// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
//
//
// Description: D3DImage class
//                  An ImageSource that displays a user created D3D surface
//

using MS.Internal;
using MS.Internal.KnownBoxes;
using MS.Internal.PresentationCore;
using MS.Win32.PresentationCore;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Composition;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using System.Security;
using System.Threading;

using SR = MS.Internal.PresentationCore.SR;
using SRID = MS.Internal.PresentationCore.SRID;

namespace System.Windows.Interop
{
    /// <summary>
    ///     Specifies the acceptible resources for SetBackBuffer.
    /// </summary>
    public enum D3DResourceType
    {
        IDirect3DSurface9
    }

    public class D3DImage : ImageSource, IAppDomainShutdownListener
    {
        static D3DImage()
        {            
            IsFrontBufferAvailablePropertyKey =
                DependencyProperty.RegisterReadOnly(
                    "IsFrontBufferAvailable",
                    typeof(bool),
                    typeof(D3DImage),
                    new UIPropertyMetadata(
                        BooleanBoxes.TrueBox,
                        new PropertyChangedCallback(IsFrontBufferAvailablePropertyChanged)
                        )
                    );

            IsFrontBufferAvailableProperty = IsFrontBufferAvailablePropertyKey.DependencyProperty;
        }

        /// <summary>
        ///     Default constructor, sets DPI to 96.0
        /// </summary>
        public D3DImage() : this(96.0, 96.0)
        {
        }

        /// <summary>
        ///     DPI constructor
        /// </summary>
        public D3DImage(double dpiX, double dpiY)
        {
            
            if (dpiX < 0)
            {
                throw new ArgumentOutOfRangeException("dpiX", SR.Get(SRID.ParameterMustBeGreaterThanZero));
            }

            if (dpiY < 0)
            {
                throw new ArgumentOutOfRangeException("dpiY", SR.Get(SRID.ParameterMustBeGreaterThanZero));
            }
   
            _canWriteEvent = new ManualResetEvent(true);
            _availableCallback = Callback;
            _sendPresentDelegate = SendPresent;
            _dpiX = dpiX;
            _dpiY = dpiY;

            _listener = new WeakReference<IAppDomainShutdownListener>(this);
            AppDomainShutdownMonitor.Add(_listener);
        }

        ~D3DImage()
        {
            if (_pInteropDeviceBitmap != null)
            {
                // Stop unmanaged code from sending us messages because we're being collected
                UnsafeNativeMethods.InteropDeviceBitmap.Detach(_pInteropDeviceBitmap);
            }

            AppDomainShutdownMonitor.Remove(_listener);
        }

        /// <summary>
        ///     Sets a back buffer source for this D3DImage. See the 2nd overload for details.
        /// </summary>
        public void SetBackBuffer(D3DResourceType backBufferType, IntPtr backBuffer)
        {
            SetBackBuffer(backBufferType, backBuffer, false);
        }

        /// <summary>
        ///     Sets a back buffer source for this D3DImage.
        ///
        ///     If enableSoftwareFallback is true, D3DImage will enable rendering of your surface
        ///     in software (TS, etc.).
        ///     
        ///     You can only call this while locked. You only need to call this once, unless
        ///     IsFrontBufferAvailable goes from false -> true and then you MUST call this 
        ///     again. When IsFrontBufferAvailable goes to false, we release our reference
        ///     to your back buffer, except when enableSoftwareFallBack is true, in which case
        ///     you are responsible for calling SetBackBuffer again with a null value to get us 
        ///     to release the reference. In this case, you are responsible for checking your 
        ///     device for device loss when rendering.
        ///
        ///     Requirements on backBuffer by type:
        ///         IDirect3DSurface9
        ///             D3DFMT_A8R8G8B8 or D3DFMT_X8R8G8B8
        ///             D3DUSAGE_RENDERTARGET
        ///             D3DPOOL_DEFAULT
        ///             Multisampling is allowed on 9Ex only
        ///             Lockability is optional but has performance impact (see below)      
        ///
        ///     For best performance by type:
        ///         IDirect3DSurface9
        ///             Vista WDDM: non-lockable, created on IDirect3DDevice9Ex with 
        ///                         D3DDEVCAPS2_CAN_STRETCHRECT_FROM_TEXTURES and
        ///                         D3DCAPS2_CANSHARERESOURCE support.
        ///             Vista XDDM: Doesn't matter. Software copying is fastest.
        ///             non-Vista:  Lockable with GetDC support for the pixel format and 
        ///                         D3DDEVCAPS2_CAN_STRETCHRECT_FROM_TEXTURES support.
        ///
        /// </summary>
        public void SetBackBuffer(D3DResourceType backBufferType, IntPtr backBuffer, bool enableSoftwareFallback)
        {

            WritePreamble();
            
            if (_lockCount == 0)
            {
                throw new InvalidOperationException(SR.Get(SRID.Image_MustBeLocked));
            }

            // In case the user passed in something like "(D3DResourceType)-1"
            if (backBufferType != D3DResourceType.IDirect3DSurface9)
            {
                throw new ArgumentOutOfRangeException("backBufferType");
            }

            // Early-out if the current back buffer equals the new one. If the front buffer
            // is not available and software fallback is not enabled, _pUserSurfaceUnsafe 
            // will be null and this check will fail. We don't want a null backBuffer to 
            // early-out when the front buffer isn't available.
            if (backBuffer != IntPtr.Zero && backBuffer == _pUserSurfaceUnsafe)
            {
                return;
            }
            
            SafeMILHandle newBitmap = null;
            uint newPixelWidth = 0;
            uint newPixelHeight = 0;

            // Create a new CInteropDeviceBitmap. Note that a null backBuffer will result 
            // in a null _pInteropDeviceBitmap at the end
            if (backBuffer != IntPtr.Zero)
            {
                HRESULT.Check(UnsafeNativeMethods.InteropDeviceBitmap.Create(
                    backBuffer,
                    _dpiX,
                    _dpiY,
                    ++_version,
                    _availableCallback,
                    enableSoftwareFallback,
                    out newBitmap,
                    out newPixelWidth,
                    out newPixelHeight
                    ));
            }

            //
            // We need to completely disassociate with the old interop bitmap if it
            // exists because it won't be deleted until the composition thread is done 
            // with it or until the garbage collector runs.
            //
            if (_pInteropDeviceBitmap != null)
            {
                // 1. Tell the old bitmap to stop sending front buffer messages because
                //    our new back buffer may be on a different adapter. Plus, tell the
                //    bitmap to release the back buffer in case the user wants to delete
                //    it immediately.
                UnsafeNativeMethods.InteropDeviceBitmap.Detach(_pInteropDeviceBitmap);

                // 2. If we were waiting for a present, unhook from commit
                UnsubscribeFromCommittingBatch();
                
                // 3. We are no longer dirty
                _isDirty = false;

                // Note: We don't need to do anything to the event because we're under
                //       the protection of Lock
            }

            // If anything about the new surface were unacceptible, we would have recieved
            // a bad HRESULT from Create() so everything must be good
            _pInteropDeviceBitmap = newBitmap;
            _pUserSurfaceUnsafe = backBuffer;
            _pixelWidth = newPixelWidth;
            _pixelHeight = newPixelHeight;
            _isSoftwareFallbackEnabled = enableSoftwareFallback;

            // AddDirtyRect is usually what triggers Changed, but AddDirtyRect isn't allowed with
            // no back buffer so we mark for Changed here
            if (_pInteropDeviceBitmap == null)
            {
                _isChangePending = true;
            }

            RegisterForAsyncUpdateResource();
            _waitingForUpdateResourceBecauseBitmapChanged = true;

            // WritePostscript will happen at Unlock
        }


        /// <summary>
        ///     Locks the D3DImage
        ///
        ///     While locked you can call AddDirtyRect, SetBackBuffer, and Unlock. You can
        ///     also write to your back buffer. You should not write to the back buffer without
        ///     being locked.
        /// </summary>
        public void Lock()
        {
            WritePreamble();

            LockImpl(Duration.Forever);
        }

        /// <summary>
        ///     Trys to lock the D3DImage but gives up once duration expires. Returns true
        ///     if the lock was obtained. See Lock for more details.
        /// </summary>
        public bool TryLock(Duration timeout)
        {
            WritePreamble();

            if (timeout == Duration.Automatic)
            {
                throw new ArgumentOutOfRangeException("timeout");
            }

            return LockImpl(timeout);
        }

        /// <summary>
        ///     Unlocks the D3DImage
        ///
        ///     Can only be called while locked.
        ///
        ///     If you have dirtied the image with AddDirtyRect, Unlocking will trigger us to
        ///     copy the dirty regions from the back buffer to the front buffer. While this is
        ///     taking place, Lock will block. To avoid locking indefinitely, use TryLock.
        /// </summary>
        public void Unlock()
        {
            WritePreamble();
            
            if (_lockCount == 0)
            {
                throw new InvalidOperationException(SR.Get(SRID.Image_MustBeLocked));
            }

            --_lockCount;

            if (_isDirty && _lockCount == 0)
            {
                SubscribeToCommittingBatch();
            }

            if (_isChangePending)
            {
                _isChangePending = false;
                
                WritePostscript();
            }
        }

        /// <Summary>
        ///     Adds a dirty rect to the D3DImage     
        ///
        ///     When you update a part of your back buffer you must dirty the same area on
        ///     the D3DImage. After you unlock, we will copy the dirty areas to the front buffer.
        ///
        ///     Can only be called while locked.
        ///
        ///     IMPORTANT: After five dirty rects, we will union them all together. This
        ///                means you must have valid data outside of the dirty regions.
        /// </Summary>
        public void AddDirtyRect(Int32Rect dirtyRect)
        {
            WritePreamble();

            if (_lockCount == 0)
            {
                throw new InvalidOperationException(SR.Get(SRID.Image_MustBeLocked));
            }

            if (_pInteropDeviceBitmap == null)
            {
                throw new InvalidOperationException(SR.Get(SRID.D3DImage_MustHaveBackBuffer));
            }

            dirtyRect.ValidateForDirtyRect("dirtyRect", PixelWidth, PixelHeight);
            if (dirtyRect.HasArea)
            {
                // Unmanaged code will make sure that the rect is well-formed
                HRESULT.Check(UnsafeNativeMethods.InteropDeviceBitmap.AddDirtyRect(
                    dirtyRect.X, 
                    dirtyRect.Y, 
                    dirtyRect.Width, 
                    dirtyRect.Height, 
                    _pInteropDeviceBitmap
                    ));
                    
                // We're now dirty, but we won't consider it a change until Unlock
                _isDirty = true;
                _isChangePending = true;
            }
        }

        /// <Summary>
        ///     When true, a front buffer exists and back buffer updates will be copied forward.
        ///     When false, a front buffer does not exist. Your changes will not be seen.
        /// </Summary>
        public bool IsFrontBufferAvailable
        {
            get
            {
                return (bool)GetValue(IsFrontBufferAvailableProperty);
            }
        }

        public static readonly DependencyProperty IsFrontBufferAvailableProperty;

        /// <Summary>
        ///     Event that fires when IsFrontBufferAvailable changes
        ///
        ///     After a true -> false transition, you should stop updating your surface as
        ///     we have stopped processing updates.
        ///
        ///     After a false -> true transition, you MUST set a valid back buffer
        /// </Summary>
        public event DependencyPropertyChangedEventHandler IsFrontBufferAvailableChanged
        {
            add
            {
                WritePreamble();

                if (value != null)
                {
                    _isFrontBufferAvailableChangedHandlers += value;
                }
            }

            remove
            {
                WritePreamble();
    
                if (value != null)
                {
                    _isFrontBufferAvailableChangedHandlers -= value;
                }
            }
        }

        /// <Summary>
        ///     Width in pixels
        /// </Summary>

        public int PixelWidth
        {
            get
            {
                ReadPreamble();
                
                return (int)_pixelWidth;
            }
        }

        /// <Summary>
        ///     Height in pixels
        /// </Summary>
        public int PixelHeight
        {
            get
            {
                ReadPreamble();
                
                return (int)_pixelHeight;
            }
        }

        /// <summary>
        ///     Get the width of the image in measure units (96ths of an inch).
        ///
        ///     Sealed to prevent subclasses from modifying the correct behavior.
        /// </summary>
        public sealed override double Width
        {
            get 
            { 
                ReadPreamble();

                return ImageSource.PixelsToDIPs(_dpiX, (int)_pixelWidth);
            }
        }

        /// <summary>
        ///     Get the height of the image in measure units (96ths of an inch).
        ///
        ///     Sealed to prevent subclasses from modifying the correct behavior.
        /// </summary>
        public sealed override double Height
        {
            get 
            { 
                ReadPreamble();
                
                return ImageSource.PixelsToDIPs(_dpiY, (int)_pixelHeight);
            }
        }

        /// <summary>
        ///     Get the metadata associated with this image source
        ///
        ///     Sealed to prevent subclasses from modifying the correct behavior.
        /// </summary>
        public sealed override ImageMetadata Metadata
        {
            get 
            { 
                ReadPreamble();
                
                return null;
            }
        }

        /// <summary>
        ///     Shadows inherited Clone() with a strongly typed
        ///     version for convenience.
        /// </summary>
        public new D3DImage Clone()
        {
            return (D3DImage)base.Clone();
        }

        /// <summary>
        ///     Shadows inherited CloneCurrentValue() with a strongly typed
        ///     version for convenience.
        /// </summary>
        public new D3DImage CloneCurrentValue()
        {
            return (D3DImage)base.CloneCurrentValue();
        }

        /// <summary>
        /// Implementation of <see cref="System.Windows.Freezable.CreateInstanceCore">Freezable.CreateInstanceCore</see>.
        /// </summary>
        /// <returns>The new Freezable.</returns>
        protected override Freezable CreateInstanceCore()
        {
            return new D3DImage();
        }

        /// <summary>
        ///     Freezing is not allowed because the user will always have to make
        ///     changes to the object based upon IsFrontBufferAvailable. We could consider
        ///     SetBackBuffer to not count as a "change" but any thread could call it
        ///     and that would violate our synchronization assumptions.
        ///
        ///     Sealed to prevent subclasses from modifying the correct behavior.
        /// </summary>
        protected sealed override bool FreezeCore(bool isChecking)
        {
            return false;
        }

        /// <summary>
        ///     Clone overrides
        ///
        ///     Not sealed to allow subclasses to clone their private data
        /// </summary>
        protected override void CloneCore(Freezable sourceFreezable)
        {
            base.CloneCore(sourceFreezable);

            CloneCommon(sourceFreezable);
        }
  
        protected override void CloneCurrentValueCore(Freezable sourceFreezable)
        {
            base.CloneCurrentValueCore(sourceFreezable);

            CloneCommon(sourceFreezable);
        }

        protected override void GetAsFrozenCore(Freezable sourceFreezable)
        {
            base.GetAsFrozenCore(sourceFreezable);

            CloneCommon(sourceFreezable);
        }

        protected override void GetCurrentValueAsFrozenCore(Freezable sourceFreezable)
        {
            base.GetCurrentValueAsFrozenCore(sourceFreezable);

            CloneCommon(sourceFreezable);
        } 

        /// <Summary>
        ///     Gets a software copy of D3DImage. Called by printing, RTB, and BMEs. The
        ///     user can override this to return something else in the case of the
        ///     device lost, for example.
        /// </Summary>
        protected internal virtual BitmapSource CopyBackBuffer()
        {
            
            BitmapSource copy = null;
            
            if (_pInteropDeviceBitmap != null)
            {
                BitmapSourceSafeMILHandle pIWICBitmapSource;
                
                if (HRESULT.Succeeded(UnsafeNativeMethods.InteropDeviceBitmap.GetAsSoftwareBitmap(
                    _pInteropDeviceBitmap,
                    out pIWICBitmapSource
                    )))
                {
                    // CachedBitmap will AddRef the bitmap
                    copy = new CachedBitmap(pIWICBitmapSource);
                }
            }

            return copy;         
        }

        private void CloneCommon(Freezable sourceFreezable)
        {           
            D3DImage source = (D3DImage)sourceFreezable;

            _dpiX = source._dpiX;
            _dpiY = source._dpiY;
            
            Lock();
            // If we've lost the front buffer, _pUserSurface unsafe will be null
            SetBackBuffer(D3DResourceType.IDirect3DSurface9, source._pUserSurfaceUnsafe);
            Unlock();
        }

        private void SubscribeToCommittingBatch()
        {
            if (!_isWaitingForPresent)
            {             
                // Suppose this D3DImage is not on the main UI thread. This thread will
                // never commit a batch so we don't want to add an event handler to it
                // since it will never get removed
                MediaContext mediaContext = MediaContext.From(Dispatcher);

                if (_duceResource.IsOnChannel(mediaContext.Channel))
                {
                    mediaContext.CommittingBatch += _sendPresentDelegate;
                    _isWaitingForPresent = true;
                }
            }
        }

        private void UnsubscribeFromCommittingBatch()
        {
            if (_isWaitingForPresent)
            {
                MediaContext mediaContext = MediaContext.From(Dispatcher);
                mediaContext.CommittingBatch -= _sendPresentDelegate;
                _isWaitingForPresent = false;
            }
        }

        /// <summary>
        ///     Lock implementation shared by Lock and TryLock
        /// </summary>
        private bool LockImpl(Duration timeout)
        {
            Debug.Assert(timeout != Duration.Automatic);
            
            bool lockObtained = false;

            if (_lockCount == UInt32.MaxValue)
            {
                throw new InvalidOperationException(SR.Get(SRID.Image_LockCountLimit));
            }
            
            if (_lockCount == 0)
            {
                if (timeout == Duration.Forever)
                {
                    lockObtained = _canWriteEvent.WaitOne();
                }
                else
                {
                    lockObtained = _canWriteEvent.WaitOne(timeout.TimeSpan, false);
                }
                
                // Consider the situation: Lock(); AddDirtyRect(); Unlock(); Lock(); return;
                // The Unlock will have set us up to send a present packet but since
                // the user re-locked the buffer we shouldn't copy forward
                UnsubscribeFromCommittingBatch();
            }
            
            ++_lockCount;

            // no WritePostscript because this isn't a "change" yet

            return lockObtained;
        }

        private static readonly DependencyPropertyKey IsFrontBufferAvailablePropertyKey;

        /// <summary>
        ///     Propery changed handler for our read only IsFrontBufferAvailable DP. Calls any
        ///     user added handlers.
        /// </summary>
        private static void IsFrontBufferAvailablePropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            Debug.Assert(e.OldValue != e.NewValue);

            bool isFrontBufferAvailable = (bool)e.NewValue;
            D3DImage img = (D3DImage)d;
            
            if (!isFrontBufferAvailable)
            {
                //
                // This isn't a true "ref" to their surface, so we don't need to do this,
                // but if the user clones this D3DImage afterwards we don't want to
                // have a pontentially garbage pointer being copied
                //
                // We are not Detach()-ing from or releasing _pInteropDeviceBitmap because 
                // if we did, we would never get a notification when the front buffer became 
                // available again.
                //
                if (!img._isSoftwareFallbackEnabled)
                {
                    // If software fallback is enabled, we keep this pointer around to make sure SetBackBuffer with
                    // the same surface pointer is still a no-op. The user is responsible for calling SetBackBuffer 
                    // again (possibly with a null value) to force the release of this pointer.
                    img._pUserSurfaceUnsafe = IntPtr.Zero;
                }
            }
        
            if (img._isFrontBufferAvailableChangedHandlers != null)
            {
                img._isFrontBufferAvailableChangedHandlers(img, e);
            }
        }

        /// <Summary>
        ///     Sends Present packet to MIL. When MIL receives the packet, it will copy the
        ///     dirty regions to the front buffer and set the event.
        /// </Summary>
        private void SendPresent(object sender, EventArgs args)
        {
            Debug.Assert(_isDirty);
            Debug.Assert(_isWaitingForPresent);
            Debug.Assert(_lockCount == 0);

            //
            // If we were waiting for present when the bitmap changed, SetBackBuffer removed
            // us from waiting for present. So if this is true then the NEW bitmap has been 
            // dirtied before it has been sent to the render thread. We need to delay the
            // present until after the update resource because the D3DImage resource is still
            // referencing the old bitmap.
            //
            if (_waitingForUpdateResourceBecauseBitmapChanged)
            {
                return;
            }

            UnsubscribeFromCommittingBatch();
            
            unsafe
            {
                DUCE.MILCMD_D3DIMAGE_PRESENT data;
                DUCE.Channel channel = sender as DUCE.Channel;
                
                Debug.Assert(_duceResource.IsOnChannel(channel));

                data.Type = MILCMD.MilCmdD3DImagePresent;
                data.Handle = _duceResource.GetHandle(channel);

                // We need to make sure the event stays alive in case we get collected before
                // the composition thread processes the packet
                IntPtr hDuplicate;
                IntPtr hCurrentProc = MS.Win32.UnsafeNativeMethods.GetCurrentProcess();
                if (!MS.Win32.UnsafeNativeMethods.DuplicateHandle(
                        hCurrentProc,
                        _canWriteEvent.SafeWaitHandle,
                        hCurrentProc,
                        out hDuplicate,
                        0,
                        false,
                        MS.Win32.UnsafeNativeMethods.DUPLICATE_SAME_ACCESS
                        ))
                {
                    throw new Win32Exception();
                }
                
                data.hEvent = (ulong)hDuplicate.ToPointer();

                // Send packed command structure

                // Note that the command is sent in its own batch (sendInSeparateBatch  == true) because this method is called under the 
                // context of the MediaContext.CommitChannel and the command needs to make it into the current set of changes which are 
                // being commited to the compositor.  If the command would not be added to a separate batch, it would go into the 
                // "future" batch which would not get submitted this time around. This leads to a dead-lock situation which occurs when 
                // the app calls Lock on the D3DImage because Lock waits on _canWriteEvent which the compositor sets when it sees the 
                // Present command. However, since the compositor does not get the Present command, it will not set the event and the 
                // UI thread will wait forever on the compositor which will cause the application to stop responding.

                channel.SendCommand(
                    (byte*)&data,
                    sizeof(DUCE.MILCMD_D3DIMAGE_PRESENT),
                    true /* sendInSeparateBatch */
                    );
            }

            _isDirty = false;

            // Block on next Lock
            _canWriteEvent.Reset();
        }

        /// <summary>
        ///     The DispatcherOperation corresponding to the composition thread callback.
        ///
        ///     If the back buffer version matches the current version, update the front
        ///     buffer status.
        /// </summary>
        private object SetIsFrontBufferAvailable(object isAvailableVersionPair)
        {
            Pair pair = (Pair)isAvailableVersionPair;
            uint version = (uint)pair.Second;

            if (version == _version)
            {
                bool isFrontBufferAvailable = (bool)pair.First;
                SetValue(IsFrontBufferAvailablePropertyKey, isFrontBufferAvailable);
            }

            // ...just because DispatcherOperationCallback requires returning an object
            return null;
        }

        /// <summary>
        ///     *** WARNING ***: This runs on the composition thread!
        ///
        ///     What the composition thread calls when it wants to update D3DImage about
        ///     front buffer state. It may be called multiple times with the same value.
        ///     Since we're on a different thread we can't touch this, so queue a dispatcher
        ///     operation.
        /// </summary>
        // NOTE: Called from the render thread!We must execute the reaction on the UI thread.
        private void Callback(bool isFrontBufferAvailable, uint version)
        {
            Dispatcher.BeginInvoke(
                DispatcherPriority.Normal,
                new DispatcherOperationCallback(SetIsFrontBufferAvailable),
                new Pair(BooleanBoxes.Box(isFrontBufferAvailable), version)
                );
        }

        internal override void UpdateResource(DUCE.Channel channel, bool skipOnChannelCheck)
        {
            // If we're told we can skip the channel check, then we must be on channel
            Debug.Assert(!skipOnChannelCheck || _duceResource.IsOnChannel(channel));

            if (skipOnChannelCheck || _duceResource.IsOnChannel(channel))
            {
                base.UpdateResource(channel, skipOnChannelCheck);

                bool isSynchronous = channel.IsSynchronous;

                DUCE.MILCMD_D3DIMAGE data;
                unsafe
                {
                    data.Type = MILCMD.MilCmdD3DImage;
                    data.Handle = _duceResource.GetHandle(channel);
                    if (_pInteropDeviceBitmap != null)
                    {
                        UnsafeNativeMethods.MILUnknown.AddRef(_pInteropDeviceBitmap);
                        
                        data.pInteropDeviceBitmap = (ulong)_pInteropDeviceBitmap.DangerousGetHandle().ToPointer();
                    }
                    else
                    {
                        data.pInteropDeviceBitmap = 0;
                    }
                    
                    data.pSoftwareBitmap = 0;

                    if (isSynchronous)
                    {
                        _softwareCopy = CopyBackBuffer();
                        
                        if (_softwareCopy != null)
                        {
                            UnsafeNativeMethods.MILUnknown.AddRef(_softwareCopy.WicSourceHandle);
                            
                            data.pSoftwareBitmap = (ulong)_softwareCopy.WicSourceHandle.DangerousGetHandle().ToPointer();
                        }
                    }

                    // Send packed command structure
                    channel.SendCommand(
                        (byte*)&data,
                        sizeof(DUCE.MILCMD_D3DIMAGE),
                        false /* sendInSeparateBatch */
                        );
                }

                // Presents only happen on the async channel so don't let RTB flip this bit
                if (!isSynchronous)
                {
                    _waitingForUpdateResourceBecauseBitmapChanged = false;
                }
            }
        }


        internal override DUCE.ResourceHandle AddRefOnChannelCore(DUCE.Channel channel)
        {
            if (_duceResource.CreateOrAddRefOnChannel(this, channel, System.Windows.Media.Composition.DUCE.ResourceType.TYPE_D3DIMAGE))
            {
                AddRefOnChannelAnimations(channel);

                UpdateResource(channel, true /* skip "on channel" check - we already know that we're on channel */ );
                
                // If we are being put onto the asynchronous compositor channel in
                // a dirty state, we need to subscribe to the commit batch event.
                if (!channel.IsSynchronous && _isDirty)
                {
                    SubscribeToCommittingBatch();
                }
            }

            return _duceResource.GetHandle(channel);
        }
        internal override void ReleaseOnChannelCore(DUCE.Channel channel)
        {
            Debug.Assert(_duceResource.IsOnChannel(channel));

            if (_duceResource.ReleaseOnChannel(channel))
            {
                ReleaseOnChannelAnimations(channel);
                
                // If we are being pulled off the asynchronous compositor channel
                // while we still have a handler hooked to the commit batch event,
                // remove the handler to avoid situations where would leak the D3DImage
                if (!channel.IsSynchronous)
                {
                    UnsubscribeFromCommittingBatch();
                }
            }
        }
        
        internal override DUCE.ResourceHandle GetHandleCore(DUCE.Channel channel)
        {
            // Note that we are in a lock here already.
            return _duceResource.GetHandle(channel);
        }
        internal override int GetChannelCountCore()
        {
            // must already be in composition lock here
            return _duceResource.GetChannelCount();
        }
        internal override DUCE.Channel GetChannelCore(int index)
        {
            // Note that we are in a lock here already.
            return _duceResource.GetChannel(index);
        }

        // IAppDomainShutdownListener
        void IAppDomainShutdownListener.NotifyShutdown()
        {
            if (_pInteropDeviceBitmap != null)
            {
                // Stop unmanaged code from sending us messages because the AppDomain is going down
                UnsafeNativeMethods.InteropDeviceBitmap.Detach(_pInteropDeviceBitmap);
            }

            // The finalizer does the same thing, so it's no longer necessary
            GC.SuppressFinalize(this);
        }
        

        internal System.Windows.Media.Composition.DUCE.MultiChannelResource _duceResource = new System.Windows.Media.Composition.DUCE.MultiChannelResource();

        // User-specified DPI of their surface
        private double _dpiX;
        private double _dpiY;

        // Handle to our unmanaged interop bitmap that is the front buffer. This can be null!
        private SafeMILHandle _pInteropDeviceBitmap;

        // This will be null until the user prints, uses an RTB, or uses a BME. It's the result
        // of calling CopyBackBuffer() which could be something created by the user. We must
        // hold onto it indefinitely because we're never sure when the render thread is done
        // with it (not even ReleaseOnChannel because it queues a command)
        private BitmapSource _softwareCopy;

        // Pointer to the user's surface that IS NOT ADDREF'D (hence the unsafe). Used
        // only for cloning purposes. This can be IntPtr.Zero!
        private IntPtr _pUserSurfaceUnsafe;

        // Whether or not the user wanted software fallback for the last back buffer.
        private bool _isSoftwareFallbackEnabled;

        // Event used to synchronize between the UI and composition thread. This prevents the user
        // from writing to the back buffer while MIL is copying to the front buffer.
        private ManualResetEvent _canWriteEvent;

        // Per-instance callback from composition thread plus handlers to notify
        private UnsafeNativeMethods.InteropDeviceBitmap.FrontBufferAvailableCallback _availableCallback;
        private DependencyPropertyChangedEventHandler _isFrontBufferAvailableChangedHandlers;
        // We'll be adding and removing a lot from CommittingBatch, so create the delegate up front
        // to avoid creating many during runtime.
        private EventHandler _sendPresentDelegate;

        // WeakReference to this used to listen to AddDomain shutdown
        private WeakReference<IAppDomainShutdownListener> _listener;

        // Keeps track of how many times the user has nested Lock
        private uint _lockCount;

        // Size of the user's surface in pixels
        private uint _pixelWidth;
        private uint _pixelHeight;

        //
        // This is incremented every time a new back buffer is set and it's passed to the bitmap.
        // When we recieve a front buffer notification, we ignore it if it doesn't match the current
        // version. 
        // 
        // Of course SetBackBuffer calls detach to tell the old bitmap to stop sending messages, but
        // it is possible the old buffer queued a message right before detach happened. If the old
        // back buffer and new back buffer are on the same adapter, it's not really an issue. If they
        // are on different ones, it's a big problem because we don't want to give incorrect front
        // buffer updates. Since we have to at least send the adapter back, let's instead use a
        // version counter that will guarantee we receive no updates from the old bitmap.
        //
        // Now if the user happens to call SetBackBuffer 2^32 times exactly we'll end up back
        // and the same version and could get a bad message, but that won't break anything.
        //
        private uint _version;

        // True if we're dirty and need to send a present
        private bool _isDirty;

        // Used to prevent Unlock from registering for commit notification more than once
        private bool _isWaitingForPresent;

        // Set to true when we want to fire Changed in Unlock
        private bool _isChangePending;

        // Depending upon timing, we could commit a batch and send a present before the UpdateResource
        // for a new bitmap happens. This is used to delay the present. See SendPresent().
        private bool _waitingForUpdateResourceBecauseBitmapChanged;
    }
}
