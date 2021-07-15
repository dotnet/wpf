// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
//

using System;
using System.IO;
using System.Collections;
using System.ComponentModel;
using System.ComponentModel.Design.Serialization;
using System.Reflection;
using MS.Internal;
using System.Diagnostics;
using System.Windows.Media;
using System.Globalization;
using System.Security;
using System.Net;
using System.Runtime.InteropServices;
using System.Windows.Media.Animation;
using System.Windows.Media.Composition;
using MS.Win32;
using System.IO.Packaging;
using UnsafeNativeMethods = MS.Win32.PresentationCore.UnsafeNativeMethods;
using SR = MS.Internal.PresentationCore.SR;
using SRID = MS.Internal.PresentationCore.SRID;
using MS.Internal.PresentationCore;                        // SecurityHelper

namespace System.Windows.Media.Imaging
{
    #region BitmapSource

    /// <summary>
    /// Interface for Bitmap Sources, included decoders and effects
    /// </summary>
    [Localizability(LocalizationCategory.None, Readability = Readability.Unreadable)]
    public abstract class BitmapSource : ImageSource, DUCE.IResource
    {
        #region Constructor

        /// <summary>
        /// Create a BitmapSource from an array of pixels.
        /// </summary>
        /// <param name="pixelWidth">Width of the Bitmap</param>
        /// <param name="pixelHeight">Height of the Bitmap</param>
        /// <param name="dpiX">Horizontal DPI of the Bitmap</param>
        /// <param name="dpiY">Vertical DPI of the Bitmap</param>
        /// <param name="pixelFormat">Format of the Bitmap</param>
        /// <param name="palette">Palette of the Bitmap</param>
        /// <param name="pixels">Array of pixels</param>
        /// <param name="stride">stride</param>
        public static BitmapSource Create(
            int pixelWidth,
            int pixelHeight,
            double dpiX,
            double dpiY,
            PixelFormat pixelFormat,
            Imaging.BitmapPalette palette,
            System.Array pixels,
            int stride
            )
        {
            return new CachedBitmap(
                        pixelWidth, pixelHeight,
                        dpiX, dpiY,
                        pixelFormat, palette,
                        pixels, stride);
        }


        /// <summary>
        /// Create a BitmapSource from an array of pixels in unmanaged memory.
        /// </summary>
        /// <param name="pixelWidth">Width of the Bitmap</param>
        /// <param name="pixelHeight">Height of the Bitmap</param>
        /// <param name="dpiX">Horizontal DPI of the Bitmap</param>
        /// <param name="dpiY">Vertical DPI of the Bitmap</param>
        /// <param name="pixelFormat">Format of the Bitmap</param>
        /// <param name="palette">Palette of the Bitmap</param>
        /// <param name="buffer">Pointer to the buffer in memory</param>
        /// <param name="bufferSize">Size of the buffer</param>
        /// <param name="stride">stride</param>
        /// <remarks>
        ///     Callers must have UnmanagedCode permission to call this API.
        /// </remarks>
        unsafe public static BitmapSource Create(
            int pixelWidth,
            int pixelHeight,
            double dpiX,
            double dpiY,
            PixelFormat pixelFormat,
            Imaging.BitmapPalette palette,
            IntPtr buffer,
            int bufferSize,
            int stride
            )
        {

            return new CachedBitmap(
                        pixelWidth, pixelHeight,
                        dpiX, dpiY,
                        pixelFormat, palette,
                        buffer, bufferSize, stride);
        }


        /// <summary>
        /// Constructor
        /// </summary>
        protected BitmapSource()
        {
            // Synchronize for *this* object only by default.
            _syncObject = _bitmapInit;
            _isSourceCached = false;
        }

        /// <summary>
        /// Internal Constructor
        ///
        /// useVirtuals: Should properties and methods like PixelWidth and CopyPixels use their "default" implementation.
        /// </summary>
        internal BitmapSource(bool useVirtuals)
        {
            _useVirtuals = true;
            _isSourceCached = false;

            // Synchronize for *this* object only by default.
            _syncObject = _bitmapInit;
        }

        /// <summary>
        /// Creates a copy of this object.
        /// </summary>
        /// <returns>The copy.</returns>
        public new BitmapSource Clone()
        {
            return (BitmapSource)base.Clone();
        }

        /// <summary>
        /// Shadows inherited CloneCurrentValue() with a strongly typed version for convenience.
        /// </summary>
        /// <returns>The copy.</returns>
        public new BitmapSource CloneCurrentValue()
        {
            return (BitmapSource)base.CloneCurrentValue();
        }

        #endregion Constructor

        #region Public properties and methods

        /// <summary>
        /// Native format of the bitmap's data.
        /// If the BitmapSource is directly readable, this is the format the
        /// pixels will be in when they are read.
        /// </summary>
        public virtual System.Windows.Media.PixelFormat Format
        {
            get
            {
                ReadPreamble();
                EnsureShouldUseVirtuals();
                _bitmapInit.EnsureInitializedComplete();
                CompleteDelayedCreation();

                return _format;
            }
        }

        /// <summary>
        /// Width, in pixels, of the bitmap.
        /// </summary>
        public virtual int PixelWidth
        {
            get
            {
                ReadPreamble();
                EnsureShouldUseVirtuals();
                _bitmapInit.EnsureInitializedComplete();
                CompleteDelayedCreation();

                return _pixelWidth;
            }
        }

        /// <summary>
        /// Height, in pixels, of the bitmap.
        /// </summary>
        public virtual int PixelHeight
        {
            get
            {
                ReadPreamble();
                EnsureShouldUseVirtuals();
                _bitmapInit.EnsureInitializedComplete();
                CompleteDelayedCreation();

                return _pixelHeight;
            }
        }


        /// <summary>
        /// Horizontal DPI of the bitmap.
        /// </summary>
        public virtual double DpiX
        {
            get
            {
                ReadPreamble();
                EnsureShouldUseVirtuals();
                _bitmapInit.EnsureInitializedComplete();
                CompleteDelayedCreation();

                return _dpiX;
            }
        }


        /// <summary>
        /// Vertical DPI of the bitmap.
        /// </summary>
        public virtual double DpiY
        {
            get
            {
                ReadPreamble();
                EnsureShouldUseVirtuals();
                _bitmapInit.EnsureInitializedComplete();
                CompleteDelayedCreation();

                return _dpiY;
            }
        }

        /// <summary>
        /// Retrieve and set the bitmap palette.
        /// </summary>
        public virtual Imaging.BitmapPalette Palette
        {
            get
            {
                ReadPreamble();
                EnsureShouldUseVirtuals();
                _bitmapInit.EnsureInitializedComplete();
                CompleteDelayedCreation();

                if (_palette == null)
                {
                    // update the local palette
                    if (_format.Palettized)
                    {
                        _palette = Imaging.BitmapPalette.CreateFromBitmapSource(this);
                    }
                }

                return _palette;
            }
        }

        /// <summary>
        /// Returns true if the BitmapSource is downloading content
        /// </summary>
        public virtual bool IsDownloading
        {
            get
            {
                ReadPreamble();
                return false;
            }
        }

        /// <summary>
        /// Raised when downloading content is done
        /// May not be raised for all content.
        /// </summary>
        public virtual event EventHandler DownloadCompleted
        {
            add
            {
                WritePreamble();
                _downloadEvent.AddEvent(value);
            }
            remove
            {
                WritePreamble();
                _downloadEvent.RemoveEvent(value);
            }
        }

        /// <summary>
        /// Raised when download has progressed
        /// May not be raised for all content.
        /// </summary>
        public virtual event EventHandler<DownloadProgressEventArgs> DownloadProgress
        {
            add
            {
                WritePreamble();
                _progressEvent.AddEvent(value);
            }
            remove
            {
                WritePreamble();
                _progressEvent.RemoveEvent(value);
            }
        }

        /// <summary>
        /// Raised when download has failed
        /// May not be raised for all content.
        /// </summary>
        public virtual event EventHandler<ExceptionEventArgs> DownloadFailed
        {
            add
            {
                WritePreamble();
                _failedEvent.AddEvent(value);
            }
            remove
            {
                WritePreamble();
                _failedEvent.RemoveEvent(value);
            }
        }

        /// <summary>
        /// Raised when decoding has failed
        /// May not be raised for all content.
        /// </summary>
        public virtual event EventHandler<ExceptionEventArgs> DecodeFailed
        {
            add
            {
                WritePreamble();
                EnsureShouldUseVirtuals();
                _decodeFailedEvent.AddEvent(value);
            }
            remove
            {
                WritePreamble();
                EnsureShouldUseVirtuals();
                _decodeFailedEvent.RemoveEvent(value);
            }
        }


        /// <summary>
        /// Copy the pixel data from the bitmap into the array of pixels that
        /// has the specified stride, starting at the offset (specified in number
        /// of pixels from the beginning). An empty rect (all 0s) will copy the
        /// entire bitmap.
        /// </summary>
        /// <param name="sourceRect">Source rect to copy. Int32Rect.Empty specifies the entire rect</param>
        /// <param name="pixels">Destination array</param>
        /// <param name="stride">Stride</param>
        /// <param name="offset">Offset in the array to begin copying</param>
        public virtual void CopyPixels(Int32Rect sourceRect, Array pixels, int stride, int offset)
        {
            EnsureShouldUseVirtuals();

            // Demand Site Of origin on the URI if it passes then this  information is ok to expose
            CheckIfSiteOfOrigin();

            CriticalCopyPixels(sourceRect, pixels, stride, offset);
        }

        /// <summary>
        /// Copy the pixel data from the bitmap into the array of pixels that
        /// has the specified stride, starting at the offset (specified in number
        /// of pixels from the beginning).
        /// </summary>
        /// <param name="pixels">Destination array</param>
        /// <param name="stride">Stride</param>
        /// <param name="offset">Offset to begin at</param>
        public virtual void CopyPixels(Array pixels, int stride, int offset)
        {
            Int32Rect sourceRect = Int32Rect.Empty;
            EnsureShouldUseVirtuals();

            // Demand Site Of origin on the URI if it passes then this  information is ok to expose
            CheckIfSiteOfOrigin();

            CopyPixels(sourceRect, pixels, stride, offset);
        }

        /// <summary>
        /// Copy the pixel data from the bitmap into the array of pixels that
        /// has the specified stride, starting at the offset (specified in number
        /// of pixels from the beginning). An empty rect (all 0s) will copy the
        /// entire bitmap.
        /// </summary>
        /// <param name="sourceRect">Source rect to copy. Int32Rect.Empty specified entire Bitmap</param>
        /// <param name="buffer">Pointer to the buffer</param>
        /// <param name="bufferSize">Size of buffer</param>
        /// <param name="stride">Stride</param>
        public virtual void CopyPixels(Int32Rect sourceRect, IntPtr buffer, int bufferSize, int stride)
        {
            ReadPreamble();
            EnsureShouldUseVirtuals();
            _bitmapInit.EnsureInitializedComplete();
            CompleteDelayedCreation();

            // Demand Site Of origin on the URI if it passes then this  information is ok to expose
            CheckIfSiteOfOrigin();

            CriticalCopyPixels(sourceRect, buffer, bufferSize, stride);
        }

        /// <summary>
        /// Get the width of the bitmap in measure units (96ths of an inch).
        /// </summary>
        public override double Width
        {
            get
            {
                ReadPreamble();

                return GetWidthInternal();
            }
        }

        /// <summary>
        /// Get the width of the bitmap in measure units (96ths of an inch).
        /// </summary>
        public override double Height
        {
            get
            {
                ReadPreamble();

                return GetHeightInternal();
            }
        }

        /// <summary>
        /// Get the Metadata of the bitmap
        /// </summary>
        public override ImageMetadata Metadata
        {
            get
            {
                ReadPreamble();

                return null;
            }
        }

        #endregion

        #region Internal, Protected and Private properties and methods

        /// <summary>
        /// Helper function to calculate Width.
        /// </summary>
        private double GetWidthInternal()
        {
            return ImageSource.PixelsToDIPs(this.DpiX, this.PixelWidth);
        }

        /// <summary>
        /// Helper function to calculate Height.
        /// </summary>
        private double GetHeightInternal()
        {
            return ImageSource.PixelsToDIPs(this.DpiY, this.PixelHeight);
        }

        /// <summary>
        /// Get the Size for the bitmap
        /// </summary>
        internal override Size Size
        {
            get
            {
                ReadPreamble();

                return new Size(Math.Max(0, GetWidthInternal()),
                                Math.Max(0, GetHeightInternal()));
            }
        }

        internal bool DelayCreation
        {
            get
            {
                return _delayCreation;
            }
            set
            {
                _delayCreation = value;

                if (_delayCreation)
                {
                    CreationCompleted = false;
                }
            }
        }

        internal bool CreationCompleted
        {
            get
            {
                return _creationComplete;
            }
            set
            {
                _creationComplete = value;
            }
        }

        ///
        /// Demand that the bitmap should be created if it was delay-created.
        ///
        internal void CompleteDelayedCreation()
        {
            // Protect against multithreaded contention on delayed creation.
            if (DelayCreation)
            {
                lock (_syncObject)
                {
                    if (DelayCreation)
                    {
                        EnsureShouldUseVirtuals();

                        DelayCreation = false;

                        try
                        {
                            FinalizeCreation();
                        }
                        catch
                        {
                            DelayCreation = true;
                            throw;
                        }

                        CreationCompleted = true;
                    }
                }
            }
        }

        internal virtual void FinalizeCreation()
        {
            throw new NotImplementedException();
        }

        private void EnsureShouldUseVirtuals()
        {
            if (_useVirtuals == false)
            {
                throw new NotImplementedException();
            }
        }

        internal object SyncObject
        {
            get
            {
                Debug.Assert(_syncObject != null);
                return _syncObject;
            }
        }

        internal bool IsSourceCached
        {
            get
            {
                return _isSourceCached;
            }
            set
            {
                _isSourceCached = value;
            }
        }

        internal BitmapSourceSafeMILHandle WicSourceHandle
        {
            get
            {
                CompleteDelayedCreation();
                if (_wicSource == null || _wicSource.IsInvalid)
                {
                    ManagedBitmapSource managedBitmapSource = new ManagedBitmapSource(this);
                    _wicSource = new BitmapSourceSafeMILHandle(Marshal.GetComInterfaceForObject(
                            managedBitmapSource,
                            typeof(System.Windows.Media.Imaging.BitmapSource.IWICBitmapSource)));
                }

                return _wicSource;
            }
            set
            {
                if (value != null)
                {
                    IntPtr wicSource = IntPtr.Zero;
                    Guid _uuidWicBitmapSource = MILGuidData.IID_IWICBitmapSource;
                    HRESULT.Check(UnsafeNativeMethods.MILUnknown.QueryInterface(
                        value,
                        ref _uuidWicBitmapSource,
                        out wicSource));

                    _wicSource = new BitmapSourceSafeMILHandle(wicSource, value);
                    UpdateCachedSettings();
                }
                else
                {
                    _wicSource = null;
                }
            }
        }

        ///
        /// Update local variables from the unmanaged resource
        ///
        internal virtual void UpdateCachedSettings()
        {
            EnsureShouldUseVirtuals();

            uint pw, ph;

            lock (_syncObject)
            {
                _format = PixelFormat.GetPixelFormat(_wicSource);

                HRESULT.Check(UnsafeNativeMethods.WICBitmapSource.GetSize(
                    _wicSource,
                    out pw,
                    out ph));

                HRESULT.Check(UnsafeNativeMethods.WICBitmapSource.GetResolution(
                    _wicSource,
                    out _dpiX,
                    out _dpiY));
            }

            _pixelWidth = (int)pw;
            _pixelHeight = (int)ph;
        }

        /// <summary>
        /// CriticalCopyPixels
        /// </summary>
        /// <param name="sourceRect"></param>
        /// <param name="pixels"></param>
        /// <param name="stride"></param>
        /// <param name="offset"></param>
        [FriendAccessAllowed] // Built into Core, also used by Framework.
        unsafe internal void CriticalCopyPixels(Int32Rect sourceRect, Array pixels, int stride, int offset)
        {
            ReadPreamble();
            _bitmapInit.EnsureInitializedComplete();
            CompleteDelayedCreation();

            if (pixels == null)
                throw new System.ArgumentNullException("pixels");

            if (pixels.Rank != 1)
                throw new ArgumentException(SR.Get(SRID.Collection_BadRank), "pixels");

            if (offset < 0)
            {
                HRESULT.Check((int)WinCodecErrors.WINCODEC_ERR_VALUEOVERFLOW);
            }

            int elementSize = -1;

            if (pixels is byte[])
                elementSize = 1;
            else if (pixels is short[] || pixels is ushort[])
                elementSize = 2;
            else if (pixels is int[] || pixels is uint[] || pixels is float[])
                elementSize = 4;
            else if (pixels is double[])
                elementSize = 8;

            if (elementSize == -1)
                throw new ArgumentException(SR.Get(SRID.Image_InvalidArrayForPixel));

            int destBufferSize = checked(elementSize * (pixels.Length - offset));


            if (pixels is byte[])
            {
                fixed (void* pixelArray = &((byte[])pixels)[offset])
                    CriticalCopyPixels(sourceRect, (IntPtr)pixelArray, destBufferSize, stride);
            }
            else if (pixels is short[])
            {
                fixed (void* pixelArray = &((short[])pixels)[offset])
                    CriticalCopyPixels(sourceRect, (IntPtr)pixelArray, destBufferSize, stride);
            }
            else if (pixels is ushort[])
            {
                fixed (void* pixelArray = &((ushort[])pixels)[offset])
                    CriticalCopyPixels(sourceRect, (IntPtr)pixelArray, destBufferSize, stride);
            }
            else if (pixels is int[])
            {
                fixed (void* pixelArray = &((int[])pixels)[offset])
                    CriticalCopyPixels(sourceRect, (IntPtr)pixelArray, destBufferSize, stride);
            }
            else if (pixels is uint[])
            {
                fixed (void* pixelArray = &((uint[])pixels)[offset])
                    CriticalCopyPixels(sourceRect, (IntPtr)pixelArray, destBufferSize, stride);
            }
            else if (pixels is float[])
            {
                fixed (void* pixelArray = &((float[])pixels)[offset])
                    CriticalCopyPixels(sourceRect, (IntPtr)pixelArray, destBufferSize, stride);
            }
            else if (pixels is double[])
            {
                fixed (void* pixelArray = &((double[])pixels)[offset])
                    CriticalCopyPixels(sourceRect, (IntPtr)pixelArray, destBufferSize, stride);
            }
}

        /// <summary>
        /// CriticalCopyPixels
        /// </summary>
        /// <param name="sourceRect"></param>
        /// <param name="buffer"></param>
        /// <param name="bufferSize"></param>
        /// <param name="stride"></param>
        internal void CriticalCopyPixels(Int32Rect sourceRect, IntPtr buffer, int bufferSize, int stride)
        {
            if (buffer == IntPtr.Zero)
                throw new ArgumentNullException("buffer");

            if (stride <= 0)
                throw new ArgumentOutOfRangeException("stride", SR.Get(SRID.ParameterMustBeGreaterThanZero));

            if (sourceRect.Width <= 0)
                sourceRect.Width = PixelWidth;

            if (sourceRect.Height <= 0)
                sourceRect.Height = PixelHeight;

            if (sourceRect.Width > PixelWidth)
                throw new ArgumentOutOfRangeException("sourceRect.Width", SR.Get(SRID.ParameterCannotBeGreaterThan, PixelWidth));

            if (sourceRect.Height > PixelHeight)
                throw new ArgumentOutOfRangeException("sourceRect.Height", SR.Get(SRID.ParameterCannotBeGreaterThan, PixelHeight));

            int minStride = checked(((sourceRect.Width * Format.BitsPerPixel) + 7) / 8);
            if (stride < minStride)
                throw new ArgumentOutOfRangeException("stride", SR.Get(SRID.ParameterCannotBeLessThan, minStride));

            int minRequiredDestSize = checked((stride * (sourceRect.Height - 1)) + minStride);
            if (bufferSize < minRequiredDestSize)
                throw new ArgumentOutOfRangeException("buffer", SR.Get(SRID.ParameterCannotBeLessThan, minRequiredDestSize));

            lock (_syncObject)
            {
                HRESULT.Check(UnsafeNativeMethods.WICBitmapSource.CopyPixels(
                    WicSourceHandle,
                    ref sourceRect,
                    (uint)stride,
                    (uint)bufferSize,
                    buffer
                    ));
            }
        }

        protected void CheckIfSiteOfOrigin()
        {
            string uri = null;

            // This call is inheritance demand protected. It is overridden in
            // BitmapFrameDecoder and BitmapImage
            if (CanSerializeToString())
            {
                // This call returns the URI either as an absolute URI which the user
                // passed in, in the first place or as the string "image"
                // we only allow this code to succeed in the case of Uri and if it is site of
                // origin or pack:. In all other conditions we fail
                uri = ConvertToString(null, null);
            }

        }

        /// <summary>
        /// Called when DUCE resource requires updating
        /// </summary>
        internal override void UpdateResource(DUCE.Channel channel, bool skipOnChannelCheck)
        {
            base.UpdateResource(channel, skipOnChannelCheck);

            UpdateBitmapSourceResource(channel, skipOnChannelCheck);
        }

        internal virtual BitmapSourceSafeMILHandle DUCECompatiblePtr
        {
            get
            {
                BitmapSourceSafeMILHandle /* IWICBitmapSource */ pIWICSource = WicSourceHandle;
                BitmapSourceSafeMILHandle /* CWICWrapperBitmap as IWICBitmapSource */ pCWICWrapperBitmap = null;

                // if we've already cached the ptr, reuse it.
                if (_convertedDUCEPtr != null && !_convertedDUCEPtr.IsInvalid)
                {
                    // already in friendly format
                    Debug.Assert(_isSourceCached);
                }
                else
                {
                    if (UsableWithoutCache)
                    {
                        #region Make sure the image is decoded on the UI thread

                        // In the case that the source is cached (ie it is already an IWICBitmap),
                        // its possible that the bitmap is a demand bitmap. The demand bitmap only
                        // copies the source bits when absolutely required (ie CopyPixels). This means
                        // that if a decode frame is attached to the demand bitmap, it may decode bits
                        // on the render thread (bad!). To prevent that, we call CopyPixels for the first
                        // pixel which will decode the entire image.
                        //
                        // Ideally, we need an implementation of IWICBitmap that can cache on a scanline
                        // basis as well can be forced to "realize" its cache when requested. Consider
                        // adding a method on IWICBitmap such as RealizeCache(WICRect *).
                        //
                        Int32Rect sourceRect = new Int32Rect(0, 0, 1, 1);
                        PixelFormat format = Format;
                        int bufferSize = (format.BitsPerPixel + 7) / 8;
                        byte[] buffer = new byte[bufferSize];

                        // If the bitmap has corrupt pixel data, we may not have detected it until now.
                        // At this point the user cannot recover gracefully, so we'll display a 1x1 image
                        // similar to what LateBoundDecoder does before it's done downloading.

                        try
                        {
                            unsafe
                            {
                                fixed (void* pixelArray = &((byte[])buffer)[0])
                                {
                                    HRESULT.Check(UnsafeNativeMethods.WICBitmapSource.CopyPixels(
                                        pIWICSource,
                                        ref sourceRect,
                                        (uint)bufferSize,
                                        (uint)bufferSize,
                                        (IntPtr)pixelArray
                                        ));
                                }
                            }
                        }
                        catch (Exception e)
                        {
                            RecoverFromDecodeFailure(e);

                            // the source will change during recovery, so we need to grab its new value
                            pIWICSource = WicSourceHandle;
                        }

                        #endregion
                    }
                    else // needs caching
                    {
                        BitmapSourceSafeMILHandle pIWicConverter = null;

                        using (FactoryMaker factoryMaker = new FactoryMaker())
                        {
                            try
                            {
                                if (!HasCompatibleFormat)
                                {
                                    #region Convert the source to a compatible format that's writable

                                    Guid destFmt = GetClosestDUCEFormat(Format, Palette).Guid;

                                    // This forces a cached system memory copy of the image in PARGB32 format.  This is
                                    // necessary to avoid format conversion in the UCE during render and accompanying
                                    // sychronization locks with UI thread during bitmap access.

                                    HRESULT.Check(UnsafeNativeMethods.WICImagingFactory.CreateFormatConverter(
                                        factoryMaker.ImagingFactoryPtr,
                                        out pIWicConverter));

                                    HRESULT.Check(UnsafeNativeMethods.WICFormatConverter.Initialize(
                                        pIWicConverter,
                                        pIWICSource,
                                        ref destFmt,
                                        DitherType.DitherTypeNone,
                                        new SafeMILHandle(IntPtr.Zero),
                                        0,
                                        WICPaletteType.WICPaletteTypeCustom));

                                    pIWICSource = pIWicConverter;

                                    #endregion
                                }

                                #region Cache the source in memory to ensure it's not decoded/converted on the render thread

                                try
                                {
                                    HRESULT.Check(UnsafeNativeMethods.WICImagingFactory.CreateBitmapFromSource(
                                            factoryMaker.ImagingFactoryPtr,
                                            pIWICSource,
                                            WICBitmapCreateCacheOptions.WICBitmapCacheOnLoad,
                                            out pIWICSource));
                                }
                                catch (Exception e)
                                {
                                    RecoverFromDecodeFailure(e);

                                    // the source will change during recovery, so we need to grab its new value
                                    pIWICSource = WicSourceHandle;
                                }

                                _isSourceCached = true;

                                #endregion
                            }
                            finally
                            {
                                if (pIWicConverter != null)
                                    pIWicConverter.Close();
                            }
                        }
                    }

                    HRESULT.Check(UnsafeNativeMethods.MilCoreApi.CreateCWICWrapperBitmap(
                            pIWICSource,
                            out pCWICWrapperBitmap));

                    UnsafeNativeMethods.MILUnknown.AddRef(pCWICWrapperBitmap);
                    _convertedDUCEPtr = new BitmapSourceSafeMILHandle(pCWICWrapperBitmap.DangerousGetHandle(), pIWICSource);
                }

                return _convertedDUCEPtr;
            }
        }

        internal override DUCE.ResourceHandle AddRefOnChannelCore(DUCE.Channel channel)
        {
            if (_duceResource.CreateOrAddRefOnChannel(this, channel, DUCE.ResourceType.TYPE_BITMAPSOURCE))
            {
                UpdateResource(channel, true /* skip "on channel" check - we already know that we're on channel */ );
            }

            return _duceResource.GetHandle(channel);
        }

        DUCE.ResourceHandle DUCE.IResource.AddRefOnChannel(DUCE.Channel channel)
        {
            // Reconsider the need for this lock when removing the MultiChannelResource.
            using (CompositionEngineLock.Acquire())
            {
                return AddRefOnChannelCore(channel);
            }
        }

        internal override int GetChannelCountCore()
        {
            return _duceResource.GetChannelCount();
        }

        int DUCE.IResource.GetChannelCount()
        {
            return GetChannelCountCore();
        }

        internal override DUCE.Channel GetChannelCore(int index)
        {
            return _duceResource.GetChannel(index);
        }

        DUCE.Channel DUCE.IResource.GetChannel(int index)
        {
            return GetChannelCore(index);
        }

        internal virtual void UpdateBitmapSourceResource(DUCE.Channel channel, bool skipOnChannelCheck)
        {
            if (_needsUpdate)
            {
                _convertedDUCEPtr = null;
                _needsUpdate = false;
            }

            // If we're told we can skip the channel check, then we must be on channel
            Debug.Assert(!skipOnChannelCheck || _duceResource.IsOnChannel(channel));

            if (skipOnChannelCheck || _duceResource.IsOnChannel(channel))
            {
                // We may end up loading in the bitmap bits so it's necessary to take the sync lock here.
                lock (_syncObject)
                {
                    channel.SendCommandBitmapSource(
                        _duceResource.GetHandle(channel),
                        DUCECompatiblePtr
                        );
                }
            }
        }

        /// <summary>
        /// Called when a failure to decode is detected.
        /// </summary>
        internal void RecoverFromDecodeFailure(Exception e)
        {
            // Set the source to an empty image in case the user doesn't respond to the failed event
            byte[] pixels = new byte[4];
            WicSourceHandle = Create(1, 1, 96, 96, PixelFormats.Pbgra32, null, pixels, 4).WicSourceHandle;
            IsSourceCached = true;

            // Let the user know that we've failed to decode so they can gracefully handle the failure.
            // Typically, the user would replace this image with a "failure" image
            OnDecodeFailed(this, new ExceptionEventArgs(e));
        }

        internal override void ReleaseOnChannelCore(DUCE.Channel channel)
        {
            Debug.Assert(_duceResource.IsOnChannel(channel));

            _duceResource.ReleaseOnChannel(channel);
        }

        void DUCE.IResource.ReleaseOnChannel(DUCE.Channel channel)
        {
            // Reconsider the need for this lock when removing the MultiChannelResource.
            using (CompositionEngineLock.Acquire())
            {
                ReleaseOnChannelCore(channel);
            }
        }

        internal override DUCE.ResourceHandle GetHandleCore(DUCE.Channel channel)
        {
            return _duceResource.GetHandle(channel);
        }

        DUCE.ResourceHandle DUCE.IResource.GetHandle(DUCE.Channel channel)
        {
            using (CompositionEngineLock.Acquire())
            {
                return GetHandleCore(channel);
            }
        }

        /// Returns the closest format that is supported by the rendering engine
        internal static PixelFormat GetClosestDUCEFormat(PixelFormat format, BitmapPalette palette)
        {
            int i = Array.IndexOf(s_supportedDUCEFormats, format);

            if (i != -1)
            {
                return s_supportedDUCEFormats[i];
            }

            int bitsPerPixel = format.InternalBitsPerPixel;

            if (bitsPerPixel == 1)
            {
                return PixelFormats.Indexed1;
            }
            else if (bitsPerPixel == 2)
            {
                return PixelFormats.Indexed2;
            }
            else if (bitsPerPixel <= 4)
            {
                return PixelFormats.Indexed4;
            }
            else if (bitsPerPixel <= 8)
            {
                return PixelFormats.Indexed8;
            }
            else if (bitsPerPixel <= 16 && format.Format != PixelFormatEnum.Gray16)     // For Gray16, one of the RGB Formats is closest
            {
                return PixelFormats.Bgr555;
            }
            else if (format.HasAlpha || BitmapPalette.DoesPaletteHaveAlpha(palette))
            {
                return PixelFormats.Pbgra32;
            }
            else
            {
                return PixelFormats.Bgr32;
            }
        }

        /// Creates a IWICBitmap
        internal static BitmapSourceSafeMILHandle CreateCachedBitmap(
            BitmapFrame frame,
            BitmapSourceSafeMILHandle wicSource,
            BitmapCreateOptions createOptions,
            BitmapCacheOption cacheOption,
            BitmapPalette palette
            )
        {
            BitmapSourceSafeMILHandle wicConverter = null;
            BitmapSourceSafeMILHandle wicConvertedSource = null;

            // For NoCache, return the original
            if (cacheOption == BitmapCacheOption.None)
            {
                return wicSource;
            }

            using (FactoryMaker factoryMaker = new FactoryMaker())
            {
                IntPtr wicFactory = factoryMaker.ImagingFactoryPtr;
                bool changeFormat = false;
                PixelFormat originalFmt = PixelFormats.Pbgra32;

                WICBitmapCreateCacheOptions wicCache = WICBitmapCreateCacheOptions.WICBitmapCacheOnLoad;
                if (cacheOption == BitmapCacheOption.OnDemand)
                {
                    wicCache = WICBitmapCreateCacheOptions.WICBitmapCacheOnDemand;
                }

                originalFmt = PixelFormat.GetPixelFormat(wicSource);
                PixelFormat destFmt = originalFmt;

                // check that we need to change the format of the bitmap
                if (0 == (createOptions & BitmapCreateOptions.PreservePixelFormat))
                {
                    if (!IsCompatibleFormat(originalFmt))
                        changeFormat = true;

                    destFmt = BitmapSource.GetClosestDUCEFormat(originalFmt, palette);
                }

                if (frame != null &&
                    (createOptions & BitmapCreateOptions.IgnoreColorProfile) == 0 &&
                    frame.ColorContexts != null &&
                    frame.ColorContexts[0] != null &&
                    frame.ColorContexts[0].IsValid &&
                    !frame._isColorCorrected &&
                    PixelFormat.GetPixelFormat(wicSource).Format != PixelFormatEnum.Extended
                    )
                {
                    ColorContext destinationColorContext;

                    // We need to make sure, we can actually create the ColorContext for the destination destFmt
                    // If the destFmt is gray or scRGB, the following is not supported, so we cannot
                    // create the ColorConvertedBitmap
                    try
                    {
                        destinationColorContext = new ColorContext(destFmt);
                    }
                    catch (NotSupportedException)
                    {
                        destinationColorContext = null;
                    }

                    if (destinationColorContext != null)
                    {
                        // NOTE: Never do this for a non-MIL pixel format, because the format converter has
                        // special knowledge to deal with the profile

                        bool conversionSuccess = false;
                        bool badColorContext = false;

                        // First try if the color converter can handle the source format directly
                        // Its possible that the color converter does not support certain pixelformats, so put a try/catch here.
                        try
                        {
                            ColorConvertedBitmap colorConvertedBitmap = new ColorConvertedBitmap(
                                frame,
                                frame.ColorContexts[0],
                                destinationColorContext,
                                destFmt
                                );

                            wicSource = colorConvertedBitmap.WicSourceHandle;
                            frame._isColorCorrected = true;
                            conversionSuccess = true;
                            changeFormat = false;   // Changeformat no longer necessary, because destFmt already created
                            // by ColorConvertedBitmap
                        }
                        catch (NotSupportedException)
                        {
                        }
                        catch (FileFormatException)
                        {
                            // If the file contains a bad color context, we catch the exception here
                            // and don't bother trying the color conversion below, since color transform isn't possible
                            // with the given color context.
                            badColorContext = true;
                        }

                        if (!conversionSuccess && changeFormat && !badColorContext)
                        {   // If the conversion failed, we first use
                            // a FormatConvertedBitmap, and then Color Convert that one...
                            changeFormat = false;

                            FormatConvertedBitmap formatConvertedBitmap = new FormatConvertedBitmap(frame, destFmt, null, 0.0);

                            ColorConvertedBitmap colorConvertedBitmap = new ColorConvertedBitmap(
                                formatConvertedBitmap,
                                frame.ColorContexts[0],
                                destinationColorContext,
                                destFmt
                                );

                            wicSource = colorConvertedBitmap.WicSourceHandle;
                            frame._isColorCorrected = true;
                            Debug.Assert(destFmt == colorConvertedBitmap.Format);
                            changeFormat = false;   // Changeformat no longer necessary, because destFmt already created
                            // by ColorConvertedBitmap
                        }
                    }
                }

                if (changeFormat)
                {
                    // start up a format converter
                    Guid fmtDestFmt = destFmt.Guid;
                    HRESULT.Check(UnsafeNativeMethods.WICCodec.WICConvertBitmapSource(
                            ref fmtDestFmt,
                            wicSource,
                            out wicConverter));

                    // dump the converted contents into a bitmap
                    HRESULT.Check(UnsafeNativeMethods.WICImagingFactory.CreateBitmapFromSource(
                            wicFactory,
                            wicConverter,
                            wicCache,
                            out wicConvertedSource));
                }
                else
                {
                    // Create the unmanaged resources
                    HRESULT.Check(UnsafeNativeMethods.WICImagingFactory.CreateBitmapFromSource(
                            wicFactory,
                            wicSource,
                            wicCache,
                            out wicConvertedSource));
                }

                wicConvertedSource.CalculateSize();
            }

            return wicConvertedSource;
        }

        /// Called when decode fails
        private void OnDecodeFailed(object sender, ExceptionEventArgs e)
        {
            _decodeFailedEvent.InvokeEvents(this, e);
        }

        #region Event handlers for bitmap chains

        // When the final link in a bitmap chain's download completes, its sets a new WicSourceHandle
        // This change must propagate up the entire chain through DownloadCompleted events

        private void OnSourceDownloadCompleted(object sender, EventArgs e)
        {
            // _weakBitmapSourceEventSink might be null. If the link down the chain was cloned, then
            // this link's event listeners would be cloned as well. As a result, this BitmapSource
            // will be listening to both the original and the clone's events, so it may get the event
            // twice.
            //         TransformedBitmap --> CachedBitmap
            //                     |
            //                     | (caused by cloning the event helper on CachedBitmap)
            //                     +-------> CachedBitmap (clone)
            // So this BitmapSource will get DownloadCompleted from both the link down the chain and
            // its clone at the same time. The first event should be handled normally, whilethe 
            // second event is a duplicate and should be silently ignored.
            if (_weakBitmapSourceEventSink != null)
            {
                CleanUpWeakEventSink();

                // Need to call FinalizeCreation to create the new WicSourceHandle, but only in some 
                // circumstances. If this BitmapSource isn't done initializing, then there's no need 
                // to call FinalizeCreation since it will be called in EndInit.
                // FinalizeCreation makes use of properties on the object, and can throw if they're
                // not set properly. Use IsValidForFinalizeCreation to validate, but don't throw
                // if the validation fails.
                if (_bitmapInit.IsInitAtLeastOnce &&
                    IsValidForFinalizeCreation(/* throwIfInvalid = */ false))
                {
                    // FinalizeCreation() can throw because it usually makes pinvokes to things
                    // that return HRESULTs. Since firing the download events up the chain is
                    // new behavior in 4.0, these exceptions are breaking plus they aren't catchable
                    // by user code. This is mostly here for ColorConvertedBitmap throwing with
                    // a bad color context, but the fact that any BitmapSource could throw and
                    // it wouldn't be catchable is justification enough to eat them all for now...
                    //
                    try
                    {
                        FinalizeCreation();
                        _needsUpdate = true;
                    }
                    catch
                    {
                    }

                    _downloadEvent.InvokeEvents(this, e);
                }
            }
        }

        private void OnSourceDownloadFailed(object sender, ExceptionEventArgs e)
        {
            // _weakBitmapSourceEventSink might be null. If the link down the chain was cloned, then
            // this link's event listeners would be cloned as well. As a result, this BitmapSource
            // will be listening to both the original and the clone's events, so it may get the event
            // twice.
            //         TransformedBitmap --> CachedBitmap
            //                     |
            //                     | (caused by cloning the event helper on CachedBitmap)
            //                     +-------> CachedBitmap (clone)
            // So this BitmapSource will get DownloadFailed from both the link down the chain and
            // its clone at the same time. The first event should be handled normally, whilethe 
            // second event is a duplicate and should be silently ignored.
            if (_weakBitmapSourceEventSink != null)
            {
                CleanUpWeakEventSink();

                _failedEvent.InvokeEvents(this, e);
            }
        }

        private void OnSourceDownloadProgress(object sender, DownloadProgressEventArgs e)
        {
            _progressEvent.InvokeEvents(this, e);
        }

        private void CleanUpWeakEventSink()
        {
            // Situation:
            // +------+ --a--> +-------------------+ --c--> +------------------+
            // | this |        | (weak event sink) |        | nextBitmapSource |
            // +------+ <--b-- +-------------------+ <--d-- +------------------+
            //     |                                                  ^
            //     +---------------------e----------------------------+
            // a is the _weakBitmapSourceEventSink reference
            // b is a WeakReference from _weakBitmapSourceEventSink back to this
            // c is the WeakBitmapSourceEventSink.EventSource property
            // d is the implicit reference due to _weakBitmapSourceEventSink attaching a handler
            //   to nextBitmapSource
            // e is the Source property on TransformedBitmap, etc
            // nextBitmapSource fired DownloadCompleted/Failed, so we're detaching the event 
            // handlers and cleaning up _weakBitmapSourceEventSink

            // Remove link c, link d
            // Remove the reference from the weak event sink
            // This implicitly removes link d as well (the EventSource setter detaches from the
            // old event source)
            // Note: this is NOT redundant, even if you detach explicitly somewhere else
            // If nextBitmapSource ever gets cloned, then the cloned nextBitmapSource will
            // contain the same delegates as nextBitmapSource, which includes the weak
            // event sink. So although we'll be cleaning up link a, the cloned
            // nextBitmapSource will still have a strong reference back to the weak
            // event sink
            _weakBitmapSourceEventSink.EventSource = null;

            // Remove link a
            _weakBitmapSourceEventSink = null;

            // Link b is a WeakReference and will not cause leaks

            // After:
            // +------+        +-------------------+        +------------------+
            // | this |        | (weak event sink) |        | nextBitmapSource |
            // +------+ <--b-- +-------------------+        +------------------+
            //     |                                                  ^
            //     +---------------------e----------------------------+
            // The weak event sink can be collected, unless a cloned nextBitmapSource is 
            // still referencing it.
            // this can be collected if it's not being explicitly referenced
            // nextBitmapSource can be collected if it/this isn't being explicitly referenced
        }

        // Needs to be internal, called by subclasses to attach handlers when their Source changes
        internal void RegisterDownloadEventSource(BitmapSource eventSource)
        {
            if (_weakBitmapSourceEventSink == null)
            {
                _weakBitmapSourceEventSink = new WeakBitmapSourceEventSink(this);
            }
            _weakBitmapSourceEventSink.EventSource = eventSource;
        }

        // Needs to be internal, called by BitmapImage to detach handlers from the dummy downloading
        // BitmapFrameDecode's chain
        internal void UnregisterDownloadEventSource()
        {
            if (_weakBitmapSourceEventSink != null)
            {
                CleanUpWeakEventSink();
            }
        }

        // Sometimes FinalizeCreation can't be called (e.g. when a ColorConvertedBitmap has no 
        // SourceColorContext or no DestinationColorContext). EndInit needs to make these 
        // checks before it calls FinalizeCreation, but so does OnSourceDownloadCompleted. 
        // Therefore, the validation has been factored out into this method.
        // If the validation fails in EndInit, an exception should be thrown. If the validation
        // fails in OnSourceDownloadCompleted, there should be no exception. Therefore, the
        // throwIfInvalid flag is provided.
        internal virtual bool IsValidForFinalizeCreation(bool throwIfInvalid)
        {
            return true;
        }

        #endregion

        //
        // Workaround for a behavior change caused by a bug fix
        //
        // According to the old implementation, CopyCommon will clone the download events as
        // well (_asdfEvent = sourceBitmap._asdfEvent.Clone();). The problem with this is the
        // _asdfEvent.Clone() will also clone all the delegates currently attached to _asdfEvent,
        // so anyone listening to the original would be implicitly listening to the clone as well.
        //
        // The bug was that BitmapFrameDecode's clone was broken if it was made while the original
        // BFD was still downloading. The clone never does anything, so it doesn't update to show
        // the image after download completes and doesn't fire any events. So the net effect is
        // that anyone attached to the original BFD would still see only 1 event get fired despite
        // implicitly listening to the clone as well.
        //
        // The problem comes in when the BFD clone got fixed. It'll now update to show the image,
        // but it will also start firing events. So people listening to the original BFD will now
        // see a behavior change (used to get download only once, but will now get download from
        // the fixed clone as well). This flag is introduced as a workaround. When it's true,
        // CopyCommon works as it did before: delegates get copied. When it's false, delegates do
        // not get copied. BitmapFrameDecode will override this to return false in order to
        // simulate the old BFD behavior when clones didn't fire and the listeners on the original
        // only got the event once.
        //
        internal virtual bool ShouldCloneEventDelegates
        {
            get { return true; }
        }

        #endregion

        #region Animatable and Freezable

        /// <summary>
        /// Called by the base Freezable class to make this object
        /// frozen.
        /// </summary>
        protected override bool FreezeCore(bool isChecking)
        {
            if (!base.FreezeCore(isChecking))
            {
                return false;
            }

            if (IsDownloading)
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Copy the fields not covered by DPs.  This is used by
        /// CloneCore(), CloneCurrentValueCore(), GetAsFrozenCore() and
        /// GetCurrentValueAsFrozenCore().
        /// </summary>
        private void CopyCommon(BitmapSource sourceBitmap)
        {
            _useVirtuals = sourceBitmap._useVirtuals;
            _delayCreation = sourceBitmap.DelayCreation;
            _creationComplete = sourceBitmap.CreationCompleted;
            WicSourceHandle = sourceBitmap.WicSourceHandle; // always do this near the top
            _syncObject = sourceBitmap.SyncObject;
            IsSourceCached = sourceBitmap.IsSourceCached;

            //
            // Decide on whether to stick with the old behavior of cloning delegates attached to
            // the events or not depending on the ShouldCloneEventDelegates property. It always
            // returns true, except in BitmapFrameDecode where it's overridden to return false.
            // See the comments for ShouldCloneEventDelegates for more details.
            //
            if (ShouldCloneEventDelegates)
            {
                if (sourceBitmap._downloadEvent != null)
                {
                    _downloadEvent = sourceBitmap._downloadEvent.Clone();
                }

                if (sourceBitmap._progressEvent != null)
                {
                    _progressEvent = sourceBitmap._progressEvent.Clone();
                }

                if (sourceBitmap._failedEvent != null)
                {
                    _failedEvent = sourceBitmap._failedEvent.Clone();
                }

                if (sourceBitmap._decodeFailedEvent != null)
                {
                    _decodeFailedEvent = sourceBitmap._decodeFailedEvent.Clone();
                }
            }
            //
            // else do nothing
            // the events are already created (they're initialized in the field declarations)
            //

            _format = sourceBitmap.Format;
            _pixelWidth = sourceBitmap.PixelWidth;
            _pixelHeight = sourceBitmap.PixelHeight;
            _dpiX = sourceBitmap.DpiX;
            _dpiY = sourceBitmap.DpiY;
            _palette = sourceBitmap.Palette;

            //
            // If a BitmapSource is part of a chain, then it will have handlers attached 
            // to the BitmapSource down the chain, X. When X gets cloned, the delegates 
            // in its events get cloned as well, so now the original BitmapSource will have 
            // handlers attached both to X and the X's clone. Here, have the BitmapSource 
            // detach from X's clone.
            // Note that since DependencyProperties are cloned first and since Source is a 
            // DependencyProperty, the clone's Source would already be set at this point, 
            // and the SourcePropertyChangedHook would have created a _weakBitmapSourceEventSink
            // object.
            //
            if (_weakBitmapSourceEventSink != null &&
                sourceBitmap._weakBitmapSourceEventSink != null)
            {
                sourceBitmap._weakBitmapSourceEventSink.DetachSourceDownloadHandlers(
                    _weakBitmapSourceEventSink.EventSource);
            }
        }

        /// <summary>
        /// Implementation of <see cref="System.Windows.Freezable.CloneCore(Freezable)">Freezable.CloneCore</see>.
        /// </summary>
        protected override void CloneCore(Freezable sourceFreezable)
        {
            BitmapSource sourceBitmap = (BitmapSource)sourceFreezable;
            base.CloneCore(sourceFreezable);

            CopyCommon(sourceBitmap);
        }

        /// <summary>
        /// Implementation of <see cref="System.Windows.Freezable.CloneCurrentValueCore(Freezable)">Freezable.CloneCurrentValueCore</see>.
        /// </summary>
        protected override void CloneCurrentValueCore(Freezable sourceFreezable)
        {
            BitmapSource sourceBitmap = (BitmapSource)sourceFreezable;
            base.CloneCurrentValueCore(sourceFreezable);

            CopyCommon(sourceBitmap);
        }

        /// <summary>
        /// Implementation of <see cref="System.Windows.Freezable.GetAsFrozenCore(Freezable)">Freezable.GetAsFrozenCore</see>.
        /// </summary>
        protected override void GetAsFrozenCore(Freezable sourceFreezable)
        {
            BitmapSource sourceBitmap = (BitmapSource)sourceFreezable;
            base.GetAsFrozenCore(sourceFreezable);

            CopyCommon(sourceBitmap);
        }

        /// <summary>
        /// Implementation of <see cref="System.Windows.Freezable.GetCurrentValueAsFrozenCore(Freezable)">Freezable.GetCurrentValueAsFrozenCore</see>.
        /// </summary>
        protected override void GetCurrentValueAsFrozenCore(Freezable sourceFreezable)
        {
            BitmapSource sourceBitmap = (BitmapSource)sourceFreezable;
            base.GetCurrentValueAsFrozenCore(sourceFreezable);

            CopyCommon(sourceBitmap);
        }

        #endregion

        #region Data Members

        private bool _delayCreation = false;
        private bool _creationComplete = false;
        private bool _useVirtuals = false;
        internal BitmapInitialize _bitmapInit = new BitmapInitialize();

        internal BitmapSourceSafeMILHandle _wicSource = null;

        internal BitmapSourceSafeMILHandle _convertedDUCEPtr;

        internal object _syncObject;
        internal bool _isSourceCached;

        // Setting this to true causes us to throw away the old DUCECompatiblePtr which contains
        // a cache of the bitmap in video memory. We'll create a new DUCECompatiblePtr and
        // eventually send the bitmap to video memory again.
        internal bool _needsUpdate;

        internal bool _isColorCorrected;
        internal UniqueEventHelper _downloadEvent = new UniqueEventHelper();
        internal UniqueEventHelper<DownloadProgressEventArgs> _progressEvent = new UniqueEventHelper<DownloadProgressEventArgs>();
        internal UniqueEventHelper<ExceptionEventArgs> _failedEvent = new UniqueEventHelper<ExceptionEventArgs>();
        internal UniqueEventHelper<ExceptionEventArgs> _decodeFailedEvent = new UniqueEventHelper<ExceptionEventArgs>();

        // cached properties. should always reflect the unmanaged copy
        internal PixelFormat _format = PixelFormats.Default;
        internal int _pixelWidth = 0;
        internal int _pixelHeight = 0;
        internal double _dpiX = 96.0;
        internal double _dpiY = 96.0;
        internal BitmapPalette _palette = null;

        /// Duce resource
        internal DUCE.MultiChannelResource _duceResource = new DUCE.MultiChannelResource();

        // Whether or not the _wicSource handle must be cached on the UI Thread
        // before being passed to the render thread.
        internal bool UsableWithoutCache
        {
            get
            {
                return HasCompatibleFormat && _isSourceCached;
            }
        }

        // Whether or not the _wicSource has a pixel format that is compatible
        // with the render thread.
        internal bool HasCompatibleFormat
        {
            get
            {
                return IsCompatibleFormat(Format);
            }
        }

        // Whether or not the specified format is compatible with the render
        // thread.
        internal static bool IsCompatibleFormat(PixelFormat format)
        {
            return (Array.IndexOf(s_supportedDUCEFormats, format) != -1);
        }


        /// List of supported DUCE formats
        /// NOTE: Please add formats in increasing bpp order
        private readonly static PixelFormat[] s_supportedDUCEFormats =
            new PixelFormat[13]
            {
                PixelFormats.Indexed1,
                PixelFormats.BlackWhite,
                PixelFormats.Indexed2,
                PixelFormats.Gray2,
                PixelFormats.Indexed4,
                PixelFormats.Gray4,
                PixelFormats.Indexed8,
                PixelFormats.Gray8,
                PixelFormats.Bgr555,
                PixelFormats.Bgr565,
                PixelFormats.Bgr32,
                PixelFormats.Bgra32,
                PixelFormats.Pbgra32
            };

        // For propagating events in bitmap chains
        private WeakBitmapSourceEventSink _weakBitmapSourceEventSink = null;

        #endregion

        #region Interface definitions

        //*************************************************************
        //
        //  IWICBitmapSource
        //
        //*************************************************************

        // Guid: IID_IWICBitmapSource
        [ComImport(), Guid("00000120-a8f2-4877-ba0a-fd2b6645fb94"), InterfaceTypeAttribute(ComInterfaceType.InterfaceIsIUnknown)]
        internal interface IWICBitmapSource
        {
            [PreserveSig]
            int GetSize(
                out int puiWidth,
                out int puiHeight
            );

            [PreserveSig]
            int GetPixelFormat(
                out Guid guidFormat
            );

            [PreserveSig]
            int GetResolution(
                out double pDpiX,
                out double pDpiY
            );

            [PreserveSig]
            int GetPalette(
                IntPtr /* IWICPalette */ pIPalette
            );

            // SizeParamIndex says which parameter specifies the size of the array,
            // which we are saying is cbStride.
            [PreserveSig]
            int CopyPixels(
                IntPtr prc,
                int cbStride,
                int cbPixels,
                IntPtr pvPixels
            );
        }

        #endregion

        #region Managed Bitmap Source

        //*************************************************************
        //
        //  ManagedBitmapSource
        //
        //*************************************************************

        [ClassInterface(ClassInterfaceType.None)]
        internal class ManagedBitmapSource : IWICBitmapSource
        {
            // When someone derives from BitmapSource, we create an instance
            // of the ManagedBitmapSource wrapping the instance to implement
            // the actual IWicBitmapSource interface for MIL to use.  We use
            // COM interop (Marshal.GetComInterfaceForObject) to cast the
            // ManagedBitmapSource to the COM interface.  This pins the
            // ManagedBitmapSource behind the reference-counted COM interface,
            // which is then stored in a BitmapSourceSafeMILHandle, which is
            // held by the original BitmapSource instance.  The pinned
            // ManagedBitmapSource also holds a reference to the orginal
            // BitmapSource, thus a cycle is created that cannot be broken by
            // the GC.
            //
            // It looks like this:
            //
            // CustomBitmapSource --> 
            //   BitmapSourceSafeMILHandle (ref counted) -->
            //     ManagedBitmapSource -->
            //       CustomBitmapSource
            //
            // To avoid this cycle, we hold a weak reference  to the original
            // BitmapSource.
            private WeakReference<BitmapSource> _bitmapSource;

            public ManagedBitmapSource(BitmapSource bitmapSource)
            {
                if (bitmapSource == null)
                {
                    throw new System.ArgumentNullException("bitmapSource");
                }
                _bitmapSource = new WeakReference<BitmapSource>(bitmapSource);
            }

            int IWICBitmapSource.GetSize(out int puiWidth, out int puiHeight)
            {
                BitmapSource bitmapSource;
                if(_bitmapSource.TryGetTarget(out bitmapSource))
                {
                    puiWidth = bitmapSource.PixelWidth;
                    puiHeight = bitmapSource.PixelHeight;
                    return NativeMethods.S_OK;
                }
                else
                {
                    puiWidth = 0;
                    puiHeight = 0;
                    return NativeMethods.E_FAIL;
                }
            }

            int IWICBitmapSource.GetPixelFormat(out Guid guidFormat)
            {
                BitmapSource bitmapSource;
                if(_bitmapSource.TryGetTarget(out bitmapSource))
                {
                    guidFormat = bitmapSource.Format.Guid;
                    return NativeMethods.S_OK;
                }
                else
                {
                    guidFormat = Guid.Empty;
                    return NativeMethods.E_FAIL;
                }
            }

            int IWICBitmapSource.GetResolution(out double pDpiX, out double pDpiY)
            {
                BitmapSource bitmapSource;
                if(_bitmapSource.TryGetTarget(out bitmapSource))
                {
                    pDpiX = bitmapSource.DpiX;
                    pDpiY = bitmapSource.DpiY;
                    return NativeMethods.S_OK;
                }
                else
                {
                    pDpiX = 0.0;
                    pDpiY = 0.0;
                    return NativeMethods.E_FAIL;
                }
            }

            int IWICBitmapSource.GetPalette(IntPtr /* IWICPalette */ pIPalette)
            {
                BitmapSource bitmapSource;
                if(_bitmapSource.TryGetTarget(out bitmapSource))
                {
                    BitmapPalette palette = bitmapSource.Palette;
                    if ((palette == null) || (palette.InternalPalette == null) || palette.InternalPalette.IsInvalid)
                    {
                        return (int)WinCodecErrors.WINCODEC_ERR_PALETTEUNAVAILABLE;
                    }
                    
                    HRESULT.Check(UnsafeNativeMethods.WICPalette.InitializeFromPalette(pIPalette, palette.InternalPalette));
                    return NativeMethods.S_OK;
                }
                else
                {
                    return NativeMethods.E_FAIL;
                }
            }

            int IWICBitmapSource.CopyPixels(IntPtr prc, int cbStride, int cbPixels, IntPtr pvPixels)
            {
                if (cbStride < 0)
                {
                    return NativeMethods.E_INVALIDARG;
                }

                if (pvPixels == IntPtr.Zero)
                {
                    return NativeMethods.E_INVALIDARG;
                }

                BitmapSource bitmapSource;
                if(_bitmapSource.TryGetTarget(out bitmapSource))
                {
                    Int32Rect rc;
                    
                    if (prc == IntPtr.Zero)
                    {
                        rc = new Int32Rect(0, 0, bitmapSource.PixelWidth, bitmapSource.PixelHeight);
                    }
                    else
                    {
                        rc = (Int32Rect)Marshal.PtrToStructure(prc, typeof(Int32Rect));
                    }
                    
                    int rectHeight, rectWidth;
                    
                    rectHeight = rc.Height;
                    rectWidth = rc.Width;
                    
                    if (rc.Width < 1 || rc.Height < 1)
                    {
                        return NativeMethods.E_INVALIDARG;
                    }
                    
                    // assuming cbStride can't be negative, but that prc.Height can
                    PixelFormat pfStruct = bitmapSource.Format;
                    
                    if (pfStruct.Format == PixelFormatEnum.Default ||
                        pfStruct.Format == PixelFormatEnum.Extended)
                    {
                        return (int)(WinCodecErrors.WINCODEC_ERR_UNSUPPORTEDPIXELFORMAT);
                    }
                    
                    
                    int rectRowSize = checked((rectWidth * pfStruct.InternalBitsPerPixel + 7) / 8);
                    
                    if (cbPixels < checked((rectHeight - 1) * cbStride + rectRowSize))
                    {
                        return (int)(WinCodecErrors.WINCODEC_ERR_INSUFFICIENTBUFFER);
                    }
                    
                    // Need to marshal
                    int arraySize = checked(rectHeight * rectRowSize);
                    byte[] managedArray = new byte[arraySize];
                    
                    // perform the copy
                    bitmapSource.CopyPixels(rc, managedArray, rectRowSize, 0);
                    
                    {
                        // transfer the contents of the relevant rect from the managed array to pvPixels
                        long rowPtr = pvPixels.ToInt64();
                        for (int y = 0; y < rectHeight; y++)
                        {
                            Marshal.Copy(managedArray, y * rectRowSize, new IntPtr(rowPtr), rectRowSize);
                            rowPtr += cbStride;
                        }
                    }
                    
                    return NativeMethods.S_OK;
                }
                else
                {
                    return NativeMethods.E_FAIL;
                }
            }
        }

        #endregion

        #region WeakBitmapSourceEventSink

        // Used to propagate DownloadCompleted events for bitmap chains

        // The simple way is to just have this BitmapSource add handlers to the chained 
        // BitmapSource's DownloadCompleted/DownloadFailed event. But that creates 
        // an implicit strong reference from the child back to this. Since multiple chains can 
        // contain the child, this BitmapSource can prevent BitmapSources in another chain 
        // from being collected.
        // (A) <-----------------> (B)
        // (C) <--> (D) <--> (E) <--^
        // A existing prevents C's entire chain from being collected

        // The weak event sink prevents a strong reference from the chained BitmapSource
        // back to this one.
        // (A) --> (weak) <--> (B)

        private class WeakBitmapSourceEventSink : WeakReference
        {
            public WeakBitmapSourceEventSink(BitmapSource bitmapSource)
                : base(bitmapSource)
            {
            }

            public void OnSourceDownloadCompleted(object sender, EventArgs e)
            {
                BitmapSource bitmapSource = this.Target as BitmapSource;
                if (null != bitmapSource)
                {
                    bitmapSource.OnSourceDownloadCompleted(bitmapSource, e);
                }
                else
                {
                    DetachSourceDownloadHandlers(EventSource);
                }
            }

            public void OnSourceDownloadFailed(object sender, ExceptionEventArgs e)
            {
                BitmapSource bitmapSource = this.Target as BitmapSource;
                if (null != bitmapSource)
                {
                    bitmapSource.OnSourceDownloadFailed(bitmapSource, e);
                }
                else
                {
                    DetachSourceDownloadHandlers(EventSource);
                }
            }

            public void OnSourceDownloadProgress(object sender, DownloadProgressEventArgs e)
            {
                BitmapSource bitmapSource = this.Target as BitmapSource;
                if (null != bitmapSource)
                {
                    bitmapSource.OnSourceDownloadProgress(bitmapSource, e);
                }
                else
                {
                    DetachSourceDownloadHandlers(EventSource);
                }
            }

            public void DetachSourceDownloadHandlers(BitmapSource source)
            {
                if (!source.IsFrozen)
                {
                    source.DownloadCompleted -= OnSourceDownloadCompleted;
                    source.DownloadFailed -= OnSourceDownloadFailed;
                    source.DownloadProgress -= OnSourceDownloadProgress;
                }
            }

            public void AttachSourceDownloadHandlers()
            {
                if (!_eventSource.IsFrozen)
                {
                    _eventSource.DownloadCompleted += OnSourceDownloadCompleted;
                    _eventSource.DownloadFailed += OnSourceDownloadFailed;
                    _eventSource.DownloadProgress += OnSourceDownloadProgress;
                }
            }

            private BitmapSource _eventSource;
            public BitmapSource EventSource
            {
                get
                {
                    return _eventSource;
                }
                set
                {
                    if (_eventSource != null)
                    {
                        DetachSourceDownloadHandlers(_eventSource);
                    }

                    _eventSource = value;

                    if (_eventSource != null)
                    {
                        AttachSourceDownloadHandlers();
                    }
                }
            }
        }

        #endregion
    }

    #endregion // BitmapSource
}

