// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
//

using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.Design.Serialization;
using System.Reflection;
using MS.Internal;
using MS.Win32.PresentationCore;
using System.Security;
using System.Diagnostics;
using System.Windows.Media;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Media.Animation;
using System.Windows.Media.Composition;
using SR=MS.Internal.PresentationCore.SR;
using SRID=MS.Internal.PresentationCore.SRID;
using MS.Internal.PresentationCore;                        // SecurityHelper
using System.Threading;

namespace System.Windows.Media.Imaging
{
    /// <summary>
    ///   WriteableBitmap provides an efficient, tear-free mechanism for updating
    ///   a system-memory bitmap.
    /// </summary>
    public sealed class WriteableBitmap : BitmapSource
    {
        #region Constructors

        /// <summary>
        ///     Internal constructor
        /// </summary>
        internal WriteableBitmap()
        {
        }
        
        /// <summary>
        ///     Creates a new WriteableBitmap instance initialized with the
        ///     contents of the specified BitmapSource.
        /// </summary>
        /// <param name="source">
        ///     The BitmapSource to copy.
        /// </param>
        public WriteableBitmap(
            BitmapSource source
            )
            : base(true) // Use base class virtuals
        {
            InitFromBitmapSource(source);
        }
        
        /// <summary>
        ///   Initializes a new instance of the WriteableBitmap class with
        ///   the specified parameters.
        /// </summary>
        /// <param name="pixelWidth">The desired width of the bitmap.</param>
        /// <param name="pixelHeight">The desired height of the bitmap.</param>
        /// <param name="dpiX">The horizontal dots per inch (dpi) of the bitmap.</param>
        /// <param name="dpiY">The vertical dots per inch (dpi) of the bitmap.</param>
        /// <param name="pixelFormat">The PixelFormat of the bitmap.</param>
        /// <param name="palette">The BitmapPalette of the bitmap.</param>
        public WriteableBitmap(
            int pixelWidth,
            int pixelHeight,
            double dpiX,
            double dpiY,
            PixelFormat pixelFormat,
            BitmapPalette palette
            )
            : base(true) // Use base class virtuals
        {
            BeginInit();

            //
            // Sanitize inputs
            //

            if (pixelFormat.Palettized && palette == null)
            {
                throw new InvalidOperationException(SR.Get(SRID.Image_IndexedPixelFormatRequiresPalette));
            }

            if (pixelFormat.Format == PixelFormatEnum.Extended)
            {
                // We don't support third-party pixel formats yet.
                throw new ArgumentException(SR.Get(SRID.Effect_PixelFormat), "pixelFormat");
            }

            if (pixelWidth < 0)
            {
                // Backwards Compat
                HRESULT.Check((int)WinCodecErrors.WINCODEC_ERR_VALUEOVERFLOW);
            }

            if (pixelWidth == 0)
            {
                // Backwards Compat
                HRESULT.Check(MS.Win32.NativeMethods.E_INVALIDARG);
            }

            if (pixelHeight < 0)
            {
                // Backwards Compat
                HRESULT.Check((int)WinCodecErrors.WINCODEC_ERR_VALUEOVERFLOW);
            }

            if (pixelHeight == 0)
            {
                // Backwards Compat
                HRESULT.Check(MS.Win32.NativeMethods.E_INVALIDARG);
            }

            //
            // Create and initialize a new unmanaged double buffered bitmap.
            //
            Guid formatGuid = pixelFormat.Guid;

            // This SafeMILHandle gets ignored if the pixel format is not palettized.
            SafeMILHandle internalPalette = new SafeMILHandle();
            if (pixelFormat.Palettized)
            {
                internalPalette = palette.InternalPalette;
            }

            HRESULT.Check(MILSwDoubleBufferedBitmap.Create(
                (uint) pixelWidth, // safe cast
                (uint) pixelHeight, // safe cast
                dpiX,
                dpiY,
                ref formatGuid,
                internalPalette,
                out _pDoubleBufferedBitmap
                ));

            _pDoubleBufferedBitmap.UpdateEstimatedSize(
                GetEstimatedSize(pixelWidth, pixelHeight, pixelFormat));

            // Momentarily lock to populate the BackBuffer/BackBufferStride properties.
            Lock();
            Unlock();

            EndInit();
        }

        #endregion // Constructors

        #region Public Methods

        /// <summary>
        ///   Adds a dirty region to the WriteableBitmap's back buffer.
        /// </summary>
        /// <param name="dirtyRect">
        ///   An Int32Rect structure specifying the dirty region.
        /// </param>
        /// <remarks>
        ///   This method can be called multiple times, and the areas are accumulated
        ///   in a sufficient, but not necessarily minimal, representation.  For efficiency,
        ///   only the areas that are marked as dirty are guaranteed to be copied over to
        ///   the rendering system.
        ///   AddDirtyRect can only be called while the bitmap is locked, otherwise an
        ///   InvalidOperationException will be thrown.
        /// </remarks>
        public void AddDirtyRect(Int32Rect dirtyRect)
        {
            WritePreamble();

            if (_lockCount == 0)
            {
                throw new InvalidOperationException(SR.Get(SRID.Image_MustBeLocked));
            }

            //
            // Sanitize the dirty rect.
            //
            dirtyRect.ValidateForDirtyRect("dirtyRect", _pixelWidth, _pixelHeight);
            if (dirtyRect.HasArea)
            {
                MILSwDoubleBufferedBitmap.AddDirtyRect(
                    _pDoubleBufferedBitmap,
                    ref dirtyRect);

                _hasDirtyRects = true;
            }

            // Note: we do not call WritePostscript because we do not want to
            // raise change notifications until the writeable bitmap is unlocked.
        }

        /// <summary>
        ///   Shadows inherited Copy() with a strongly typed version for convenience.
        /// </summary>
        public new WriteableBitmap Clone()
        {
            return (WriteableBitmap)base.Clone();
        }

        /// <summary>
        ///   Shadows inherited CloneCurrentValue() with a strongly typed version for convenience.
        /// </summary>
        public new WriteableBitmap CloneCurrentValue()
        {
            return (WriteableBitmap)base.CloneCurrentValue();
        }

        /// <summary>
        ///     This method locks the WriteableBitmap and increments the lock count.
        /// </summary>
        /// <remarks>
        ///     By "locking" the WriteableBitmap, updates will not be sent to the rendering system until
        ///     the WriteableBitmap is fully unlocked.  This can be used to support multi-threaded scenarios.
        ///     This method blocks until the rendering system is finished processing the last frame's update.
        ///     To provide a timeout see WriteableBitmap.TryLock.
        ///     Locking the WriteableBitmap gives the caller write permission to the back buffer whose address
        ///     can be obtained via the WriteableBitmap.BackBuffer property.
        /// </remarks>
        public void Lock()
        {
            bool locked = TryLock(Duration.Forever);
            Debug.Assert(locked);
        }

        /// <summary>
        ///     This method tries to lock the WriteableBitmap for the specified
        ///     timeout and increments the lock count if successful.
        /// </summary>
        /// <param name="timeout">
        ///     The amount of time to wait while trying to acquire the lock.
        ///     To block indefinitely pass Duration.Forever.
        ///     Duration.Automatic is an invalid value.
        /// </param>
        /// <returns>Returns true if the lock is now held, false otherwise.</returns>
        public bool TryLock(Duration timeout)
        {
            WritePreamble();

            TimeSpan timeoutSpan;
            if (timeout == Duration.Automatic)
            {
                throw new ArgumentOutOfRangeException("timeout");
            }
            else if (timeout == Duration.Forever)
            {
                timeoutSpan = TimeSpan.FromMilliseconds(-1);
            }
            else
            {
                timeoutSpan = timeout.TimeSpan;
            }

            if (_lockCount == UInt32.MaxValue)
            {
                throw new InvalidOperationException(SR.Get(SRID.Image_LockCountLimit));
            }

            if (_lockCount == 0)
            {
                // Try to acquire the back buffer by the supplied timeout, if the acquire call times out, return false.
                if (!AcquireBackBuffer(timeoutSpan, true))
                {
                    return false;
                }

                Int32Rect rect = new Int32Rect(0, 0, _pixelWidth, _pixelHeight);

                HRESULT.Check(UnsafeNativeMethods.WICBitmap.Lock(
                    WicSourceHandle,
                    ref rect,
                    LockFlags.MIL_LOCK_WRITE,
                    out _pBackBufferLock
                    ));

                // If this is the first lock operation, cache the BackBuffer and
                // BackBufferStride.  These two values will never change, so we
                // don't fetch them on every lock.
                if (_backBuffer == IntPtr.Zero)
                {
                    IntPtr tempBackBufferPointer = IntPtr.Zero;
                    uint lockBufferSize = 0;
                    HRESULT.Check(UnsafeNativeMethods.WICBitmapLock.GetDataPointer(
                        _pBackBufferLock,
                        ref lockBufferSize,
                        ref tempBackBufferPointer
                        ));
                    BackBuffer = tempBackBufferPointer;

                    uint lockBufferStride = 0;
                    HRESULT.Check(UnsafeNativeMethods.WICBitmapLock.GetStride(
                        _pBackBufferLock,
                        ref lockBufferStride
                        ));
                    Invariant.Assert(lockBufferStride <= Int32.MaxValue);
                    _backBufferStride.Value = (int)lockBufferStride;
                }

                // If we were subscribed to the CommittingBatch event, unsubscribe
                // since we should not be part of the batch now that we are
                // locked.  When we unlock, we will subscribe to the
                // CommittingBatch again.
                UnsubscribeFromCommittingBatch();
            }

            _lockCount++;
            return true;
        }

        /// <summary>
        ///   This method decrements the lock count, and if it reaches zero will release the
        ///   on the back buffer and request a render pass.
        /// </summary>
        public void Unlock()
        {
            WritePreamble();

            if (_lockCount == 0)
            {
                throw new InvalidOperationException(SR.Get(SRID.Image_MustBeLocked));
            }
            Invariant.Assert(_lockCount > 0, "Lock count should never be negative!");

            _lockCount--;
            if (_lockCount == 0)
            {
                // This makes the back buffer read-only.
                _pBackBufferLock.Dispose();
                _pBackBufferLock = null;

                if (_hasDirtyRects)
                {
                    SubscribeToCommittingBatch();

                    //
                    // Notify listeners that we have changed.
                    //
                    WritePostscript();
                }
            }
        }

        /// <summary>
        ///   Updates the pixels in the specified region of the bitmap.
        /// </summary>
        /// <param name="sourceRect">The rect to copy from the input buffer.</param>
        /// <param name="sourceBuffer">The input buffer used to update the bitmap.</param>
        /// <param name="sourceBufferSize">The size of the input buffer in bytes.</param>
        /// <param name="sourceBufferStride">The stride of the input buffer in bytes.</param>
        /// <param name="destinationX">The destination x-coordinate of the left-most pixel to copy.</param>
        /// <param name="destinationY">The destination y-coordinate of the top-most pixel to copy.</param>
        public void WritePixels(
            Int32Rect sourceRect,
            IntPtr    sourceBuffer,
            int       sourceBufferSize,
            int       sourceBufferStride,
            int       destinationX,
            int       destinationY
            )
        {
            WritePreamble();

            WritePixelsImpl(sourceRect,
                            sourceBuffer,
                            sourceBufferSize,
                            sourceBufferStride, 
                            destinationX,
                            destinationY,
                            /*backwardsCompat*/ false);
        }
        
        /// <summary>
        ///   Updates the pixels in the specified region of the bitmap.
        /// </summary>
        /// <param name="sourceRect">The rect to copy from the input buffer.</param>
        /// <param name="sourceBuffer">The input buffer used to update the bitmap.</param>
        /// <param name="sourceBufferStride">The stride of the input buffer in bytes.</param>
        /// <param name="destinationX">The destination x-coordinate of the left-most pixel to copy.</param>
        /// <param name="destinationY">The destination y-coordinate of the top-most pixel to copy.</param>
        public void WritePixels(
            Int32Rect sourceRect,
            Array     sourceBuffer,
            int       sourceBufferStride,
            int       destinationX,
            int       destinationY
            )
        {
            WritePreamble();

            int elementSize;
            int sourceBufferSize;
            Type elementType;
            ValidateArrayAndGetInfo(sourceBuffer,
                                    /*backwardsCompat*/ false,
                                    out elementSize,
                                    out sourceBufferSize,
                                    out elementType);

            // We accept arrays of arbitrary value types - but not reference types.
            if (elementType == null || !elementType.IsValueType)
            {
                throw new ArgumentException(SR.Get(SRID.Image_InvalidArrayForPixel));
            }

            // Get the address of the data in the array by pinning it.
            GCHandle arrayHandle = GCHandle.Alloc(sourceBuffer, GCHandleType.Pinned);
            try
            {
                unsafe
                {
                    IntPtr buffer = arrayHandle.AddrOfPinnedObject();
                    WritePixelsImpl(sourceRect,
                                    buffer,
                                    sourceBufferSize,
                                    sourceBufferStride,
                                    destinationX,
                                    destinationY,
                                    /*backwardsCompat*/ false);
                }
            }
            finally
            {
                arrayHandle.Free();
            }
        }

        /// <summary>
        /// Update the pixels of this Bitmap
        /// </summary>
        /// <param name="sourceRect">Area to update</param>
        /// <param name="buffer">Input buffer</param>
        /// <param name="bufferSize">Size of the buffer</param>
        /// <param name="stride">Stride</param>
        public unsafe void WritePixels(
            Int32Rect sourceRect,
            IntPtr buffer,
            int bufferSize,
            int stride
            )
        {
            WritePreamble();

            if (bufferSize < 1)
            {
                throw new ArgumentOutOfRangeException("bufferSize", SR.Get(SRID.ParameterCannotBeLessThan, 1));
            }

            if (stride < 1)
            {
                throw new ArgumentOutOfRangeException("stride", SR.Get(SRID.ParameterCannotBeLessThan, 1));
            }

            if (sourceRect.IsEmpty || sourceRect.Width <= 0 || sourceRect.Height <= 0)
            {
                return;
            }

            // Backwards-Compat:
            //
            // The "sourceRect" is actually a "destinationRect", as in it
            // refers to the location where the contents are written.
            //
            // This method presumes that the pixels are copied from the
            // the specified offset (element count) in the source buffer, and
            // that no sub-byte pixel formats are used.
            int destinationX = sourceRect.X;
            int destinationY = sourceRect.Y;
            sourceRect.X = 0;
            sourceRect.Y = 0;

            WritePixelsImpl(sourceRect, 
                            buffer,
                            bufferSize,
                            stride,
                            destinationX,
                            destinationY,
                            /*backwardsCompat*/ true);
        }

        /// <summary>
        /// Update the pixels of this Bitmap
        /// </summary>
        /// <param name="sourceRect">Area to update</param>
        /// <param name="pixels">Input buffer</param>
        /// <param name="stride">Stride</param>
        /// <param name="offset">Input buffer offset</param>
        public void WritePixels(
            Int32Rect sourceRect,
            Array pixels,
            int stride,
            int offset
            )
        {
            WritePreamble();

            if (sourceRect.IsEmpty || sourceRect.Width <= 0 || sourceRect.Height <= 0)
            {
                return;
            }

            int elementSize;
            int sourceBufferSize;
            Type elementType;
            ValidateArrayAndGetInfo(pixels,
                                    /*backwardsCompat*/ true,
                                    out elementSize,
                                    out sourceBufferSize, 
                                    out elementType);

            if (stride < 1)
            {
                throw new ArgumentOutOfRangeException("stride", SR.Get(SRID.ParameterCannotBeLessThan, 1));
            }

            if (offset < 0)
            {
                throw new ArgumentOutOfRangeException("offset", SR.Get(SRID.ParameterCannotBeLessThan, 0));
            }

            // We accept arrays of arbitrary value types - but not reference types.
            if (elementType == null || !elementType.IsValueType)
            {
                throw new ArgumentException(SR.Get(SRID.Image_InvalidArrayForPixel));
            }
            
            checked
            {
                int offsetInBytes = checked(offset * elementSize);
                if (offsetInBytes >= sourceBufferSize)
                {
                    // Backwards compat:
                    //
                    // The original code would throw an exception deeper in
                    // the code when it indexed off the end of the array.  We
                    // now check earlier (compat break) but throw the same
                    // exception.
                    throw new IndexOutOfRangeException();
                }

                // Backwards-Compat:
                //
                // The "sourceRect" is actually a "destinationRect", as in it
                // refers to the location where the contents are written.
                //
                // This method presumes that the pixels are copied from the
                // the specified offset (element count) in the source buffer, and
                // that no sub-byte pixel formats are used.  We handle the offset
                // later.
                int destinationX = sourceRect.X;
                int destinationY = sourceRect.Y;
                sourceRect.X = 0;
                sourceRect.Y = 0;

                // Get the address of the data in the array by pinning it.
                GCHandle arrayHandle = GCHandle.Alloc(pixels, GCHandleType.Pinned);
                try
                {
                    IntPtr buffer = arrayHandle.AddrOfPinnedObject();

                    checked
                    {
                        buffer = new IntPtr(((long) buffer) + (long) offsetInBytes);
                        sourceBufferSize -= offsetInBytes;
                    }

                    WritePixelsImpl(sourceRect,
                                    buffer,
                                    sourceBufferSize,
                                    stride,
                                    destinationX,
                                    destinationY,
                                    /*backwardsCompat*/ true);
                }
                finally
                {
                    arrayHandle.Free();
                }
            }
        }

        #endregion // Public Methods

        #region Protected Methods

        /// <summary>
        ///   Implementation of Freezable.CreateInstanceCore.
        /// </summary>
        /// <returns>The new Freezable.</returns>
        protected override Freezable CreateInstanceCore()
        {
            return new WriteableBitmap();
        }

        /// <summary>
        ///   Implementation of Freezable.CloneCore.
        /// </summary>
        protected override void CloneCore(Freezable sourceFreezable)
        {
            WriteableBitmap sourceBitmap = (WriteableBitmap) sourceFreezable;

            base.CloneCore(sourceFreezable);

            CopyCommon(sourceBitmap);
        }

        /// <summary>
        ///   Implementation of Freezable.FreezeCore.
        /// </summary>
        protected override bool FreezeCore(bool isChecking)
        {
            bool canFreeze = (_lockCount == 0) && base.FreezeCore(isChecking);

            if (canFreeze && !isChecking)
            {
                Debug.Assert(_pBackBufferLock == null);

                //
                // By entering 'frozen' mode, we convert from being a 
                // DoubleBufferedBitmap to a regular BitmapSource.
                //

                // Protect the back buffer for writing
                HRESULT.Check(MILSwDoubleBufferedBitmap.ProtectBackBuffer(_pDoubleBufferedBitmap));

                // Get the back buffer to be used as our WicSourceHandle
                AcquireBackBuffer(TimeSpan.Zero, false);
                _needsUpdate = true;
                _hasDirtyRects = false;

                // Transfer the memory pressure over to WicSourceHandle.
                WicSourceHandle.CopyMemoryPressure(_pDoubleBufferedBitmap);

                // From here on out we're going to effectively be an ordinary
                // BitmapSource.
                _actLikeSimpleBitmap = true;

                // Pull this resource off all the channels and put it back on.
                int channelCount = _duceResource.GetChannelCount();
                for (int i = 0; i < channelCount; i++)
                {
                    DUCE.IResource resource = this as DUCE.IResource;
                    DUCE.Channel channel = _duceResource.GetChannel(i);

                    //
                    // It could have been added multiple times, so release until
                    // it's no longer on a channel.
                    //
                    uint refCount = _duceResource.GetRefCountOnChannel(channel);
                    for (uint j = 0; j < refCount; j++)
                    {
                        resource.ReleaseOnChannel(channel);
                    }

                    // Put it back on the Channel, only this time it wont
                    // be a SwDoubleBufferedBitmap.
                    for (uint j = 0; j < refCount; j++)
                    {
                        resource.AddRefOnChannel(channel);
                    }
                }
                
                Debug.Assert(!_isWaitingForCommit);

                // We no longer need the SwDoubleBufferedBitmap
                _pDoubleBufferedBitmap.Dispose();
                _pDoubleBufferedBitmap = null;

                // We will no longer need to wait for this event.
                _copyCompletedEvent.Close();
                _copyCompletedEvent = null;

                // Clear out unused variables
                _committingBatchHandler = null;
                _pBackBuffer = null;
            }

            return canFreeze;
        }

        /// <summary>
        ///   Implementation of Freezable.CloneCurrentValueCore.
        /// </summary>
        protected override void CloneCurrentValueCore(Freezable sourceFreezable)
        {
            WriteableBitmap sourceBitmap = (WriteableBitmap) sourceFreezable;

            base.CloneCurrentValueCore(sourceFreezable);

            CopyCommon(sourceBitmap);
        }

        /// <summary>
        ///   Implementation of Freezable.GetAsFrozenCore.
        /// </summary>
        protected override void GetAsFrozenCore(Freezable sourceFreezable)
        {
            WriteableBitmap sourceBitmap = (WriteableBitmap)sourceFreezable;

            base.GetAsFrozenCore(sourceFreezable);

            CopyCommon(sourceBitmap);
        }

        /// <summary>
        ///   Implementation of Freezable.GetCurrentValueAsFrozenCore.
        /// </summary>
        protected override void GetCurrentValueAsFrozenCore(Freezable sourceFreezable)
        {
            WriteableBitmap sourceBitmap = (WriteableBitmap)sourceFreezable;

            base.GetCurrentValueAsFrozenCore(sourceFreezable);

            CopyCommon(sourceBitmap);
        }

        #endregion // Protected Methods

        #region Private/Internal Methods

        /// <summary>
        ///     Gets the estimated memory pressure in bytes
        /// </summary>
        private long GetEstimatedSize(int pixelWidth, int pixelHeight, PixelFormat pixelFormat)
        {
            // Dimensions of the bitmap * bytes per pixel, then multiply by 2 because
            // WriteableBitmap uses 2 buffers.
            return pixelWidth * pixelHeight * pixelFormat.InternalBitsPerPixel / 8 * 2;
        }

        /// <summary>
        ///     Initializes this WriteableBitmap with the
        ///     contents of the specified BitmapSource.
        /// </summary>
        /// <param name="source">
        ///     The BitmapSource to copy.
        /// </param>
        private void InitFromBitmapSource(
            BitmapSource source
            )
        {
            if (source == null)
            {
                throw new ArgumentNullException("source");
            }

            if (source.PixelWidth < 0)
            {
                // Backwards Compat
                HRESULT.Check((int)WinCodecErrors.WINCODEC_ERR_VALUEOVERFLOW);
            }

            if (source.PixelHeight < 0)
            {
                // Backwards Compat
                HRESULT.Check((int)WinCodecErrors.WINCODEC_ERR_VALUEOVERFLOW);
            }

            BeginInit();

            _syncObject = source.SyncObject;
            lock (_syncObject)
            {
                Guid formatGuid = source.Format.Guid;

                SafeMILHandle internalPalette = new SafeMILHandle();
                if (source.Format.Palettized)
                {
                    internalPalette = source.Palette.InternalPalette;
                }
                
                HRESULT.Check(MILSwDoubleBufferedBitmap.Create(
                    (uint)source.PixelWidth, // safe cast
                    (uint)source.PixelHeight, // safe cast
                    source.DpiX,
                    source.DpiY,
                    ref formatGuid,
                    internalPalette,
                    out _pDoubleBufferedBitmap
                    ));

                _pDoubleBufferedBitmap.UpdateEstimatedSize(
                    GetEstimatedSize(source.PixelWidth, source.PixelHeight, source.Format));

                Lock();

                Int32Rect rcFull = new Int32Rect(0, 0, _pixelWidth, _pixelHeight);
                int bufferSize = checked(_backBufferStride.Value * source.PixelHeight);
                source.CriticalCopyPixels(rcFull, _backBuffer, bufferSize, _backBufferStride.Value);
                AddDirtyRect(rcFull);

                Unlock();
            }

            EndInit();
        }

        /// <summary>
        ///     Updates the pixels in the specified region of the bitmap.
        /// </summary>
        /// <param name="sourceRect">
        ///     The rect to copy from the input buffer.
        /// </param>
        /// <param name="sourceBuffer">
        ///     The input buffer used to update the bitmap.
        /// </param>
        /// <param name="sourceBufferSize">
        ///     The size of the input buffer in bytes.
        /// </param>
        /// <param name="sourceBufferStride">
        ///     The stride of the input buffer in bytes.
        /// </param>
        /// <param name="destX">
        ///     The destination x-coordinate of the left-most pixel to copy.
        /// </param>
        /// <param name="destY">
        ///     The destination y-coordinate of the top-most pixel to copy.
        /// </param>
        /// <param name="backwardsCompat">
        ///     Whether or not to preserve the old WritePixels behavior.
        /// </param>
        private void WritePixelsImpl(
            Int32Rect sourceRect,
            IntPtr    sourceBuffer,
            int       sourceBufferSize,
            int       sourceBufferStride,
            int       destinationX,
            int       destinationY,
            bool      backwardsCompat
            )
        {
            //
            // Sanitize the source rect and assure it will fit within the back buffer.
            //
            if (sourceRect.X < 0)
            {
                Debug.Assert(!backwardsCompat);
                throw new ArgumentOutOfRangeException("sourceRect", SR.Get(SRID.ParameterCannotBeNegative));
            }

            if (sourceRect.Y < 0)
            {
                Debug.Assert(!backwardsCompat);
                throw new ArgumentOutOfRangeException("sourceRect", SR.Get(SRID.ParameterCannotBeNegative));
            }

            if (sourceRect.Width < 0)
            {
                Debug.Assert(!backwardsCompat);
                throw new ArgumentOutOfRangeException("sourceRect", SR.Get(SRID.ParameterMustBeBetween, 0, _pixelWidth));
            }

            if (sourceRect.Width > _pixelWidth)
            {
                if (backwardsCompat)
                {
                    HRESULT.Check(MS.Win32.NativeMethods.E_INVALIDARG);
                }
                else
                {
                    throw new ArgumentOutOfRangeException("sourceRect", SR.Get(SRID.ParameterMustBeBetween, 0, _pixelWidth));
                }
            }

            if (sourceRect.Height < 0)
            {
                Debug.Assert(!backwardsCompat);
                throw new ArgumentOutOfRangeException("sourceRect", SR.Get(SRID.ParameterMustBeBetween, 0, _pixelHeight));
            }

            if (sourceRect.Height > _pixelHeight)
            {
                if (backwardsCompat)
                {
                    HRESULT.Check(MS.Win32.NativeMethods.E_INVALIDARG);
                }
                else
                {
                    throw new ArgumentOutOfRangeException("sourceRect", SR.Get(SRID.ParameterMustBeBetween, 0, _pixelHeight));
                }
            }

            if (destinationX < 0)
            {
                if (backwardsCompat)
                {
                    HRESULT.Check((int)WinCodecErrors.WINCODEC_ERR_VALUEOVERFLOW);
                }
                else
                {
                    throw new ArgumentOutOfRangeException("sourceRect", SR.Get(SRID.ParameterCannotBeNegative));
                }
            }
        
            if (destinationX > _pixelWidth - sourceRect.Width)
            {
                if (backwardsCompat)
                {
                    HRESULT.Check(MS.Win32.NativeMethods.E_INVALIDARG);
                }
                else
                {
                    throw new ArgumentOutOfRangeException("destinationX", SR.Get(SRID.ParameterMustBeBetween, 0, _pixelWidth - sourceRect.Width));
                }
            }

            if (destinationY < 0)
            {
                if (backwardsCompat)
                {
                    HRESULT.Check((int)WinCodecErrors.WINCODEC_ERR_VALUEOVERFLOW);
                }
                else
                {
                    throw new ArgumentOutOfRangeException("destinationY", SR.Get(SRID.ParameterMustBeBetween, 0, _pixelHeight - sourceRect.Height));
                }
            }

            if (destinationY > _pixelHeight - sourceRect.Height)
            {
                if (backwardsCompat)
                {
                    HRESULT.Check(MS.Win32.NativeMethods.E_INVALIDARG);
                }
                else
                {
                    throw new ArgumentOutOfRangeException("destinationY", SR.Get(SRID.ParameterMustBeBetween, 0, _pixelHeight - sourceRect.Height));
                }
            }

            //
            // Sanitize the other parameters.
            //
            if (sourceBuffer == IntPtr.Zero)
            {
                // Backwards Compat:
                //
                // The original code would null-ref when it was passed a null
                // buffer (IntPtr.Zero).  We choose to throw a better
                // exception.
                throw new ArgumentNullException(backwardsCompat ? "buffer" : "sourceBuffer");
            }

            if (sourceBufferStride < 1)
            {
                Debug.Assert(!backwardsCompat);
                throw new ArgumentOutOfRangeException("sourceBufferStride", SR.Get(SRID.ParameterCannotBeLessThan, 1));
            }

            if (sourceRect.Width == 0 || sourceRect.Height == 0)
            {
                Debug.Assert(!backwardsCompat);

                // Nothing to do.
                return;
            }

            checked
            {
                uint finalRowWidthInBits = (uint)((sourceRect.X + sourceRect.Width) * _format.InternalBitsPerPixel);
                uint finalRowWidthInBytes = ((finalRowWidthInBits + 7) / 8);
                uint requiredBufferSize = (uint)((sourceRect.Y + sourceRect.Height - 1) * sourceBufferStride) + finalRowWidthInBytes;
                if (sourceBufferSize < requiredBufferSize)
                {
                    if (backwardsCompat)
                    {
                        HRESULT.Check((int)WinCodecErrors.WINCODEC_ERR_INSUFFICIENTBUFFER);
                    }
                    else
                    {
                        throw new ArgumentException(SR.Get(SRID.Image_InsufficientBufferSize), "sourceBufferSize");
                    }
                }

                uint copyWidthInBits = (uint)(sourceRect.Width * _format.InternalBitsPerPixel);

                // Calculate some offsets that we'll need in a moment.
                uint sourceXbyteOffset = (uint)((sourceRect.X * _format.InternalBitsPerPixel) / 8);
                uint sourceBufferBitOffset = (uint)((sourceRect.X * _format.InternalBitsPerPixel) % 8);
                uint firstPixelByteOffet = (uint)((sourceRect.Y * sourceBufferStride) + sourceXbyteOffset);
                uint destXbyteOffset = (uint)((destinationX * _format.InternalBitsPerPixel) / 8);
                uint destBufferBitOffset = (uint)((destinationX * _format.InternalBitsPerPixel) % 8);

                Int32Rect destinationRect = sourceRect;
                destinationRect.X = destinationX;
                destinationRect.Y = destinationY;

                //
                // Copy pixel information from the user supplied buffer to the back buffer.
                //
                unsafe
                {
                    uint destOffset = (uint)(destinationY * _backBufferStride.Value) + destXbyteOffset;
                    byte* pDest = (byte*)_backBuffer.ToPointer();
                    pDest += destOffset;
                    uint outputBufferSize = _backBufferSize - destOffset;

                    byte* pSource = (byte*)sourceBuffer.ToPointer();
                    pSource += firstPixelByteOffet;
                    uint inputBufferSize = (uint)sourceBufferSize - firstPixelByteOffet;

                    Lock();

                    MILUtilities.MILCopyPixelBuffer(
                        pDest,
                        outputBufferSize,
                        (uint) _backBufferStride.Value,
                        destBufferBitOffset,
                        pSource,
                        inputBufferSize,
                        (uint) sourceBufferStride,
                        sourceBufferBitOffset,
                        (uint) sourceRect.Height,
                        copyWidthInBits);

                    AddDirtyRect(destinationRect);
                    Unlock();
                }
            }

            // Note: we do not call WritePostscript because we do not want to
            // raise change notifications until the writeable bitmap is unlocked.
            //
            // Change notifications may have already been raised in the Unlock
            // call in this method.
        }

        /// <summary>
        ///   Try to acquire the back buffer of our unmanaged double buffered bitmap in the specified timeout.
        /// </summary>
        /// <param name="timeout">
        ///   The time to wait while trying to acquire the lock.
        /// </param>
        /// <param name="waitForCopy">
        ///   Should we try to wait for the copy completed event?
        /// </param>        
        /// <returns>Returns true if the back buffer was acquired before the timeout expired.</returns>
        private bool AcquireBackBuffer(TimeSpan timeout, bool waitForCopy)
        {
            bool backBufferAcquired = false;

            //
            // Only get the back buffer from the unmanaged double buffered bitmap if this is our
            // first time being called since the last successful call to OnCommittingBatch.
            // OnCommittingBatch sets _pBackBuffer to null.
            //
            if (_pBackBuffer == null)
            {
                bool shouldGetBackBuffer = true;
                
                if (waitForCopy)
                {
                    // If we have committed a copy-forward command, we need to wait
                    // for the render thread to finish the copy before we can use
                    // the back buffer.
                    shouldGetBackBuffer = _copyCompletedEvent.WaitOne(timeout, false);
                }
                
                if (shouldGetBackBuffer)
                {
                    MILSwDoubleBufferedBitmap.GetBackBuffer(
                        _pDoubleBufferedBitmap,
                        out _pBackBuffer,
                        out _backBufferSize);

                    _syncObject = WicSourceHandle = _pBackBuffer;
                    backBufferAcquired = true;
                }
            }
            else
            {
                backBufferAcquired = true;
            }

            return backBufferAcquired;
        }

        /// <summary>
        ///   Common implementation for CloneCore(), CloneCurrentValueCore(),
        ///   GetAsFrozenCore(), and GetCurrentValueAsFrozenCore().
        /// </summary>
        /// <param name="sourceBitmap">The WriteableBitmap to copy from.</param>
        private void CopyCommon(WriteableBitmap sourceBitmap)
        {
            // Avoid Animatable requesting resource updates for invalidations
            // that occur during construction.
            Animatable_IsResourceInvalidationNecessary = false;
            _actLikeSimpleBitmap = false;

            // Create a SwDoubleBufferedBitmap and copy the sourceBitmap into it.
            InitFromBitmapSource(sourceBitmap);

            // The next invalidation will cause Animatable to register an
            // UpdateResource callback.
            Animatable_IsResourceInvalidationNecessary = true;
        }


        // ISupportInitialize

        /// <summary>
        ///   Prepare the bitmap to accept initialize paramters.
        /// </summary>
        private void BeginInit()
        {
            _bitmapInit.BeginInit();
        }

        /// <summary>
        ///   Prepare the bitmap to accept initialize paramters.
        /// </summary>
        private void EndInit()
        {
            _bitmapInit.EndInit();

            FinalizeCreation();
        }

        /// <summary>
        ///   Create the unmanaged resources.
        /// </summary>
        internal override void FinalizeCreation()
        {
            IsSourceCached = true;
            CreationCompleted = true;
            UpdateCachedSettings();
        }

        /// <summary>
        ///     Get the size of the specified array and of the elements in it.
        /// </summary>
        /// <param name="sourceBuffer">
        ///     The array to get info about.
        /// </param>
        /// <param name="elementSize">
        ///     On output, will contain the size of the elements in the array.
        /// </param>
        /// <param name="sourceBufferSize">
        ///     On output, will contain the size of the array.
        /// </param>
        private void ValidateArrayAndGetInfo(Array sourceBuffer,
                                                       bool backwardsCompat,
                                                       out int elementSize,
                                                       out int sourceBufferSize,
                                                       out Type elementType)
        {
            //
            // Assure that a valid pixels Array was provided.
            //
            if (sourceBuffer == null)
            {
                throw new ArgumentNullException(backwardsCompat ? "pixels" : "sourceBuffer");
            }

            if (sourceBuffer.Rank == 1)
            {
                if (sourceBuffer.GetLength(0) <= 0)
                {
                    if (backwardsCompat)
                    {
                        elementSize = 1;
                        sourceBufferSize = 0;
                        elementType = null;
                    }
                    else
                    {
                        throw new ArgumentException(SR.Get(SRID.Image_InsufficientBuffer), "sourceBuffer");
                    }
                }
                else
                {
                    checked
                    {
                        object exemplar = sourceBuffer.GetValue(0);
                        elementSize = Marshal.SizeOf(exemplar);
                        sourceBufferSize = sourceBuffer.GetLength(0) * elementSize;
                        elementType = exemplar.GetType();
                    }
                }
}
            else if (sourceBuffer.Rank == 2)
            {
                if (sourceBuffer.GetLength(0) <= 0 || sourceBuffer.GetLength(1) <= 0)
                {
                    if (backwardsCompat)
                    {
                        elementSize = 1;
                        sourceBufferSize = 0;
                        elementType = null;
                    }
                    else
                    {
                        throw new ArgumentException(SR.Get(SRID.Image_InsufficientBuffer), "sourceBuffer");
                    }
                }
                else
                {
                    checked
                    {
                        object exemplar = sourceBuffer.GetValue(0,0);
                        elementSize = Marshal.SizeOf(exemplar);
                        sourceBufferSize = sourceBuffer.GetLength(0) * sourceBuffer.GetLength(1) * elementSize;
                        elementType = exemplar.GetType();
                    }
                }
            }
            else
            {
                throw new ArgumentException(SR.Get(SRID.Collection_BadRank), backwardsCompat ? "pixels" : "sourceBuffer");
            }
        }

        /// <summary>
        ///     Adds a reference to our DUCE resource on <paramref name="channel"/>.
        /// </summary>
        /// <param name="channel">
        ///     The channel we want to AddRef on.
        /// </param>
        /// <returns>
        ///     The handle to our DoubleBufferedBitmap or BitmapSource handle.
        /// </returns>
        /// <remarks>
        ///     We override this method because we use a different resource
        ///     type than our base class does.  This probably suggests that the
        ///     base class should not presume the resource type, but it
        ///     currently does.  The base class uses TYPE_BITMAPSOURCE
        ///     resources, and we use TYPE_DOUBLEBUFFEREDBITMAP resources.
        /// </remarks>
        internal override DUCE.ResourceHandle AddRefOnChannelCore(DUCE.Channel channel)
        {
            //
            // If we're in BitmapSource mode, then just defer to the BitmapSource
            // implementation.
            //
            if (_actLikeSimpleBitmap)
            {
                return base.AddRefOnChannelCore(channel);
            }

            if (_duceResource.CreateOrAddRefOnChannel(this, channel, DUCE.ResourceType.TYPE_DOUBLEBUFFEREDBITMAP))
            {
                // This is the first AddRef on this channel...

                // If we are being put onto the asynchronous compositor channel in
                // a dirty state, we need to subscribe to the CommittingBatch event.
                if (!channel.IsSynchronous && _hasDirtyRects)
                {
                    SubscribeToCommittingBatch();
                }

                AddRefOnChannelAnimations(channel);

                // The first time our resource is created on a channel, we need
                // to update it.  We can skip "on channel" check since we
                // already know that the resource is on channel.
                UpdateResource(channel, true);
            }

            return _duceResource.GetHandle(channel);
        }

        internal override void ReleaseOnChannelCore(DUCE.Channel channel)
        {
            Debug.Assert(_duceResource.IsOnChannel(channel));

            if (_duceResource.ReleaseOnChannel(channel))
            {
                // This is the last release from this channel...

                // If we are being pulled off the asynchronous compositor channel
                // then unsubscribe from the CommittingBatch event.
                if (!channel.IsSynchronous)
                {
                    UnsubscribeFromCommittingBatch();
                }

                ReleaseOnChannelAnimations(channel);
            }
        }

        /// <summary>
        ///   Updates the double-buffered bitmap DUCE resource with a pointer to our acutal object.
        /// </summary>
        /// <param name="channel">The channel to update the resource on.</param>
        /// <param name="skipOnChannelCheck">
        ///   If this is true, we know we are on channel and don't need to explicitly check.
        /// </param>
        internal override void UpdateBitmapSourceResource(DUCE.Channel channel, bool skipOnChannelCheck)
        {
            //
            // If we're in BitmapSource mode, then just defer to the BitmapSource
            // implementation.
            //
            if (_actLikeSimpleBitmap)
            {
                base.UpdateBitmapSourceResource(channel, skipOnChannelCheck);
                return;
            }
            
            // We override this method because we use a different resource type
            // than our base class does.  This probably suggests that the base
            // class should not presume the resource type, but it currently
            // does.  The base class uses TYPE_BITMAPSOURCE resources, and we
            // use TYPE_DOUBLEBUFFEREDBITMAP resources.

            // If we're told we can skip the channel check, then we must be on channel
            Debug.Assert(!skipOnChannelCheck || _duceResource.IsOnChannel(channel));

            if (skipOnChannelCheck || _duceResource.IsOnChannel(channel))
            {
                DUCE.MILCMD_DOUBLEBUFFEREDBITMAP command;
                command.Type = MILCMD.MilCmdDoubleBufferedBitmap;
                command.Handle = _duceResource.GetHandle(channel);
                unsafe
                {
                    command.SwDoubleBufferedBitmap = (UInt64) _pDoubleBufferedBitmap.DangerousGetHandle().ToPointer();
                }
                command.UseBackBuffer = channel.IsSynchronous ? 1u : 0u;

                //
                // We need to ensure that this object stays alive while traveling over the channel
                // so we'll AddRef it here, and simply take over the reference on the other side.
                //
                UnsafeNativeMethods.MILUnknown.AddRef(_pDoubleBufferedBitmap);
                
                unsafe
                {
                    channel.SendCommand(
                        (byte*)&command,
                        sizeof(DUCE.MILCMD_DOUBLEBUFFEREDBITMAP),
                        false /* sendInSeparateBatch */
                        );
                }
            }
        }

        private void SubscribeToCommittingBatch()
        {
            // Only subscribe the the CommittingBatch event if we are on-channel.
            if (!_isWaitingForCommit)
            {
                MediaContext mediaContext = MediaContext.From(Dispatcher);
                if (_duceResource.IsOnChannel(mediaContext.Channel))
                {
                    mediaContext.CommittingBatch += CommittingBatchHandler;
                    _isWaitingForCommit = true;
                }
            }
        }

        private void UnsubscribeFromCommittingBatch()
        {
            if (_isWaitingForCommit)
            {
                MediaContext mediaContext = MediaContext.From(Dispatcher);
                mediaContext.CommittingBatch -= CommittingBatchHandler;
                _isWaitingForCommit = false;
            }
        }

        /// <summary>
        ///   Send a packet on the DUCE.Channel telling our double-buffered bitmap resource
        ///   to copy forward dirty regions from the back buffer to the front buffer.
        /// </summary>
        /// <remarks>
        ///   For the packet to be sent, the user must have added a dirty region to the
        ///   WriteableBitmap and there must be no outstanding locks.
        /// </remarks>
        private void OnCommittingBatch(object sender, EventArgs args)
        {
            Debug.Assert(_isWaitingForCommit);  // How else are we here?
            UnsubscribeFromCommittingBatch();

            Debug.Assert(_lockCount == 0);  // How else are we here?
            Debug.Assert(_hasDirtyRects);  // How else are we here?

            // Before using the back buffer again, we need to know when
            // the rendering thread has completed the copy. By setting
            // our back buffer pointer to null, we'll have to re-acquire
            // it the next time, which will wait for the copy to complete.
            _copyCompletedEvent.Reset();
            _pBackBuffer = null;

            DUCE.Channel channel = sender as DUCE.Channel;
            Debug.Assert(_duceResource.IsOnChannel(channel));  // How else are we here?

            // We are going to pass an event in the command packet we send to
            // the composition thread.  We need to make sure the event stays 
            // alive in case we get collected before the composition thread
            // processes the packet.  We do this by duplicating the event
            // handle, and the composition thread will close the handle after
            // signalling it.
            IntPtr hDuplicate;
            IntPtr hCurrentProc = MS.Win32.UnsafeNativeMethods.GetCurrentProcess();
            if (!MS.Win32.UnsafeNativeMethods.DuplicateHandle(
                    hCurrentProc,
                    _copyCompletedEvent.SafeWaitHandle,
                    hCurrentProc,
                    out hDuplicate,
                    0,
                    false,
                    MS.Win32.UnsafeNativeMethods.DUPLICATE_SAME_ACCESS
                    ))
            {
                throw new Win32Exception();
            }

            DUCE.MILCMD_DOUBLEBUFFEREDBITMAP_COPYFORWARD command;
            command.Type = MILCMD.MilCmdDoubleBufferedBitmapCopyForward;
            command.Handle = _duceResource.GetHandle(channel);
            command.CopyCompletedEvent = (UInt64) hDuplicate.ToInt64();

            // Note that the batch is closed after the sendcommand because this method is called under the 
            // context of the MediaContext.CommitChannel and the command needs to make it into the current set of changes which are 
            // being commited to the compositor.  If the batch is not closed, it would go into the 
            // "future" batch which would not get submitted this time around. This leads to a dead-lock situation which occurs when 
            // the app calls Lock on the WriteableBitmap because Lock waits on _copyCompletedEvent which the compositor sets when it sees the 
            // Present command. However, since the compositor does not get the Present command, it will not set the event and the 
            // UI thread will wait forever on the compositor which will cause the application to stop responding.
            // Another option is to send the command in its own batch (instead of closing the batch). This doesn't work in all cases 
            // because the command for creating the resource handle (AddRefOnChannelCore) or the command for initializing the resource (UpdateBitmapSourceResource)
            // could be in the "future" batch thus crashing the CopyForward operation in this batch.


            unsafe
            {
                channel.SendCommand(
                    (byte*)&command,
                    sizeof(DUCE.MILCMD_DOUBLEBUFFEREDBITMAP_COPYFORWARD));
                channel.CloseBatch();
            }

            // We are committing the batch to the asynchronous compositor,
            // which will copy the rects forward.  The copy will complete
            // before we can access the back buffer again.  So, we consider
            // ourselves clean.
            _hasDirtyRects = false;
        }

        #endregion

        #region Properties

        /// <summary>
        ///   Read-only data pointer to the back buffer.
        /// </summary>
        public IntPtr BackBuffer
        {
            get
            {
                ReadPreamble();

                return _backBuffer;
            }

            private set
            {
                _backBuffer = value;
            }
        }

        private IntPtr _backBuffer;

        private uint _backBufferSize;

        /// <summary>
        ///   Read-only stride of the back buffer.
        /// </summary>
        public int BackBufferStride
        {
            get
            {
                ReadPreamble();

                return _backBufferStride.Value;
            }
        }

        private SecurityCriticalDataForSet<int> _backBufferStride;

        #endregion // Properties

        #region Fields

        private SafeMILHandle _pDoubleBufferedBitmap;   // CSwDoubleBufferedBitmap

        private SafeMILHandle _pBackBufferLock;         // IWICBitmapLock

        private BitmapSourceSafeMILHandle _pBackBuffer; // IWICBitmap

        private uint _lockCount = 0;

        // Flags whether the user has added a dirty rect since the last CopyForward packet was sent.
        private bool _hasDirtyRects = true;

        // Flags whether a MediaContext.CommittingBatch handler has already been added.
        private bool _isWaitingForCommit = false;   

        private ManualResetEvent _copyCompletedEvent = new ManualResetEvent(true);

        private EventHandler CommittingBatchHandler
        {
            get
            {
                if (_committingBatchHandler == null)
                {
                    _committingBatchHandler = OnCommittingBatch;
                }

                return _committingBatchHandler;
            }
        }
        private EventHandler _committingBatchHandler; // = OnCommittingBatch (CS0236)

        private bool _actLikeSimpleBitmap = false;

        #endregion // Fields
    }
}
