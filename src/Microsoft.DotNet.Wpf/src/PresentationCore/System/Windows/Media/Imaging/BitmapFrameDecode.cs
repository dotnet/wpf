// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
//

using System;
using System.Collections;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.Design.Serialization;
using System.Reflection;
using MS.Internal;
using MS.Internal.PresentationCore;
using System.Diagnostics;
using System.Windows.Media;
using System.Globalization;
using System.Security;
using System.Runtime.InteropServices;
using System.Windows.Media.Animation;
using System.Windows.Media.Composition;
using System.Windows.Media.Imaging;
using MS.Win32.PresentationCore;

#pragma warning disable 1634, 1691 // Allow suppression of certain presharp messages

namespace System.Windows.Media.Imaging
{
    #region BitmapFrameDecode

    /// <summary>
    /// BitmapFrameDecode abstract class
    /// </summary>
    internal sealed class BitmapFrameDecode : BitmapFrame
    {
        #region Constructors

        /// <summary>
        /// Internal constructor -- Creates new frame using specified decoder
        /// </summary>
        internal BitmapFrameDecode(
            int frameNumber,
            BitmapCreateOptions createOptions,
            BitmapCacheOption cacheOption,
            BitmapDecoder decoder
            ) : base(true)
        {
            _bitmapInit.BeginInit();
            _frameNumber = frameNumber;
            _isThumbnailCached = false;
            _isMetadataCached = false;
            _frameSource = null;

            Debug.Assert(decoder != null);
            _decoder = decoder;
            _syncObject = decoder.SyncObject;
            _createOptions = createOptions;
            _cacheOption = cacheOption;

            _bitmapInit.EndInit();

            if ((createOptions & BitmapCreateOptions.DelayCreation) != 0)
            {
                DelayCreation = true;
            }
            else
            {
                FinalizeCreation();
            }
        }

        /// <summary>
        /// Internal constructor -- Creates frame from another frame
        /// </summary>
        internal BitmapFrameDecode(
            int frameNumber,
            BitmapCreateOptions createOptions,
            BitmapCacheOption cacheOption,
            BitmapFrameDecode frameDecode
            ) : base(true)
        {
            _bitmapInit.BeginInit();
            _frameNumber = frameNumber;
            WicSourceHandle = frameDecode.WicSourceHandle;
            IsSourceCached = frameDecode.IsSourceCached;
            CreationCompleted = frameDecode.CreationCompleted;
            _frameSource = frameDecode._frameSource;
            _decoder = frameDecode.Decoder;
            _syncObject = _decoder.SyncObject;
            _createOptions = createOptions;
            _cacheOption = cacheOption;

            _thumbnail = frameDecode._thumbnail;
            _isThumbnailCached = frameDecode._isThumbnailCached;
            _metadata = frameDecode._metadata;
            _isMetadataCached = frameDecode._isMetadataCached;
            _readOnlycolorContexts = frameDecode._readOnlycolorContexts;
            _isColorContextCached = frameDecode._isColorContextCached;

            _bitmapInit.EndInit();

            if ((createOptions & BitmapCreateOptions.DelayCreation) != 0)
            {
                DelayCreation = true;
            }
            else if (!CreationCompleted)
            {
                FinalizeCreation();
            }
            else
            {
                UpdateCachedSettings();
            }
        }

        /// <summary>
        /// Internal constructor -- Creates frame thats being downloaded
        /// </summary>
        internal BitmapFrameDecode(
            int frameNumber,
            BitmapCreateOptions createOptions,
            BitmapCacheOption cacheOption,
            LateBoundBitmapDecoder decoder
            ) : base(true)
        {
            _bitmapInit.BeginInit();
            _frameNumber = frameNumber;

            byte[] pixels = new byte[4];

            BitmapSource source = BitmapSource.Create(
                1,
                1,
                96,
                96,
                PixelFormats.Pbgra32,
                null,
                pixels,
                4
                );

            WicSourceHandle = source.WicSourceHandle;

            Debug.Assert(decoder != null);
            _decoder = decoder;
            _createOptions = createOptions;
            _cacheOption = cacheOption;

            //
            // Hook the decoders download events
            //
            _decoder.DownloadCompleted += OnDownloadCompleted;
            _decoder.DownloadProgress += OnDownloadProgress;
            _decoder.DownloadFailed += OnDownloadFailed;

            _bitmapInit.EndInit();
        }

        /// <summary>
        /// Do not allow construction
        /// </summary>
        private BitmapFrameDecode() : base(true)
        {
        }

        #endregion

        #region IUriContext

        /// <summary>
        /// Provides the base uri of the current context.
        /// </summary>
        public override Uri BaseUri
        {
            get
            {
                ReadPreamble();
                return _decoder._baseUri;
            }
            set
            {
                WritePreamble();
                // Nothing to do here.
            }
        }

        #endregion

        #region Public Properties

        /// <summary>
        /// Accesses the Thumbnail property for this BitmapFrameDecode
        /// </summary>
        public override BitmapSource Thumbnail
        {
            get
            {
                ReadPreamble();
                EnsureThumbnail();
                return _thumbnail;
            }
        }

        /// <summary>
        /// Accesses the Metadata property for this BitmapFrameDecode
        /// </summary>
        public override ImageMetadata Metadata
        {
            get
            {
                ReadPreamble();
                return InternalMetadata;
            }
        }

        /// <summary>
        /// Accesses the Decoder property for this BitmapFrameDecode
        /// </summary>
        public override BitmapDecoder Decoder
        {
            get
            {
                ReadPreamble();
                return _decoder;
            }
        }

        /// <summary>
        /// Used as a delegate in ColorContexts to get the unmanaged IWICColorContexts
        /// </summary>
        private int GetColorContexts(ref uint numContexts, IntPtr[] colorContextPtrs)
        {
            Invariant.Assert(colorContextPtrs == null || numContexts <= colorContextPtrs.Length);
            
            return UnsafeNativeMethods.WICBitmapFrameDecode.GetColorContexts(_frameSource, numContexts, colorContextPtrs, out numContexts);
        }

        /// <summary>
        /// If there is an embedded color profile, return it.
        /// Otherwise, return null.  This method does NOT create a
        /// color profile for bitmaps that don't already have one.
        /// </summary>
        /// <summary>
        /// Provides access to this encoders color profile
        /// </summary>
        ///
        public override ReadOnlyCollection<ColorContext> ColorContexts
        {
            get
            {
                ReadPreamble();
                if (!_isColorContextCached && !IsDownloading)
                {
                    EnsureSource();

                    lock (_syncObject)
                    {
                        IList<ColorContext> colorContextList = ColorContext.GetColorContextsHelper(GetColorContexts);

                        if (colorContextList != null)
                        {
                            _readOnlycolorContexts = new ReadOnlyCollection<ColorContext>(colorContextList);
                        }

                        _isColorContextCached = true;
                    }
                }

                return _readOnlycolorContexts;
}
        }

        /// <summary>
        /// Returns if the BitmapFrame is downloading content
        /// </summary>
        public override bool IsDownloading
        {
            get
            {
                ReadPreamble();
                return Decoder.IsDownloading;
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Create an in-place bitmap metadata writer.
        /// </summary>
        public override InPlaceBitmapMetadataWriter CreateInPlaceBitmapMetadataWriter()
        {
            ReadPreamble();

            if (_decoder != null)
            {
                _decoder.CheckOriginalWritable();
            }

            // Demand Site Of Origin on the URI before usage of metadata.
            CheckIfSiteOfOrigin();

            EnsureSource();

            return InPlaceBitmapMetadataWriter.CreateFromFrameDecode(_frameSource, _syncObject);
        }

        #endregion

        #region ToString

        /// <summary>
        /// Can serialze "this" to a string
        /// </summary>
        internal override bool CanSerializeToString()
        {
            ReadPreamble();

            return _decoder.CanConvertToString();
        }

        /// <summary>
        /// Creates a string representation of this object based on the format string
        /// and IFormatProvider passed in.
        /// If the provider is null, the CurrentCulture is used.
        /// See the documentation for IFormattable for more information.
        /// </summary>
        /// <returns>
        /// A string representation of this object.
        /// </returns>
        internal override string ConvertToString(string format, IFormatProvider provider)
        {
            ReadPreamble();

            if (_decoder != null)
            {
                return _decoder.ToString();
            }

            return base.ConvertToString(format, provider);
        }

        #endregion

        #region Freezable

        /// <summary>
        /// Implementation of <see cref="System.Windows.Freezable.CreateInstanceCore">Freezable.CreateInstanceCore</see>.
        /// </summary>
        /// <returns>The new Freezable.</returns>
        protected override Freezable CreateInstanceCore()
        {
            return new BitmapFrameDecode();
        }

        /// <summary>
        /// Copy the fields not covered by DPs.  This is used by
        /// CloneCore(), CloneCurrentValueCore(), GetAsFrozenCore() and
        /// GetCurrentValueAsFrozenCore().
        /// </summary>
        private void CopyCommon(BitmapFrameDecode sourceBitmapFrameDecode)
        {
            _bitmapInit.BeginInit();
            _frameNumber = sourceBitmapFrameDecode._frameNumber;
            _isThumbnailCached = sourceBitmapFrameDecode._isThumbnailCached;
            _isMetadataCached = sourceBitmapFrameDecode._isMetadataCached;
            _isColorContextCached = sourceBitmapFrameDecode._isColorContextCached;
            _frameSource = sourceBitmapFrameDecode._frameSource;
            _thumbnail = sourceBitmapFrameDecode._thumbnail;
            _metadata = sourceBitmapFrameDecode.InternalMetadata;
            _readOnlycolorContexts = sourceBitmapFrameDecode._readOnlycolorContexts;

            _decoder = sourceBitmapFrameDecode._decoder;
            if (_decoder != null && _decoder.IsDownloading)
            {
                // UpdateDecoder must be called when download completes and the real decoder
                // is created. Normally _decoder will call UpdateDecoder, but in this case the
                // decoder will not know about the cloned BitmapFrameDecode and will only call
                // UpdateDecoder on the original. The clone will need to listen to the original
                // BitmapFrameDecode for DownloadCompleted, then call UpdateDecoder on itself.
                // The weak event sink hooks up handlers on DownloadCompleted, DownloadFailed,
                // and DownloadProgress
                _weakBitmapFrameDecodeEventSink =
                    new WeakBitmapFrameDecodeEventSink(this, sourceBitmapFrameDecode);
            }

            _syncObject = _decoder.SyncObject;
            _createOptions = sourceBitmapFrameDecode._createOptions;
            _cacheOption = sourceBitmapFrameDecode._cacheOption;
            _bitmapInit.EndInit();
        }

        /// <summary>
        /// Implementation of <see cref="System.Windows.Freezable.CloneCore(Freezable)">Freezable.CloneCore</see>.
        /// </summary>
        protected override void CloneCore(Freezable sourceFreezable)
        {
            BitmapFrameDecode sourceBitmapFrameDecode = (BitmapFrameDecode)sourceFreezable;
            base.CloneCore(sourceFreezable);

            CopyCommon(sourceBitmapFrameDecode);
        }

        /// <summary>
        /// Implementation of <see cref="System.Windows.Freezable.CloneCurrentValueCore(Freezable)">Freezable.CloneCurrentValueCore</see>.
        /// </summary>
        protected override void CloneCurrentValueCore(Freezable sourceFreezable)
        {
            BitmapFrameDecode sourceBitmapFrameDecode = (BitmapFrameDecode)sourceFreezable;
            base.CloneCurrentValueCore(sourceFreezable);

            CopyCommon(sourceBitmapFrameDecode);
        }


        /// <summary>
        /// Implementation of <see cref="System.Windows.Freezable.GetAsFrozenCore(Freezable)">Freezable.GetAsFrozenCore</see>.
        /// </summary>
        protected override void GetAsFrozenCore(Freezable sourceFreezable)
        {
            BitmapFrameDecode sourceBitmapFrameDecode = (BitmapFrameDecode)sourceFreezable;
            base.GetAsFrozenCore(sourceFreezable);

            CopyCommon(sourceBitmapFrameDecode);
        }


        /// <summary>
        /// Implementation of <see cref="System.Windows.Freezable.GetCurrentValueAsFrozenCore(Freezable)">Freezable.GetCurrentValueAsFrozenCore</see>.
        /// </summary>
        protected override void GetCurrentValueAsFrozenCore(Freezable sourceFreezable)
        {
            BitmapFrameDecode sourceBitmapFrameDecode = (BitmapFrameDecode)sourceFreezable;
            base.GetCurrentValueAsFrozenCore(sourceFreezable);

            CopyCommon(sourceBitmapFrameDecode);
        }

        #endregion

        #region Internal Properties / Methods

        /// <summary>
        /// Updates the internal decoder -- usually happens with a LateBoundBitmapDecoder
        /// </summary>
        internal void UpdateDecoder(BitmapDecoder decoder)
        {
            Debug.Assert(_decoder != null);
            _decoder = decoder;
            _syncObject = decoder.SyncObject;
            WicSourceHandle = null;
            _needsUpdate = true;
            FinalizeCreation();

            // Trigger a update of the UCE resource
            RegisterForAsyncUpdateResource();
        }

        /// <summary>
        /// Create the unmanaged resources
        /// </summary>
        internal override void FinalizeCreation()
        {
            EnsureSource();

            // Set the WicSourceHandle and Update the cached settings so that we
            // can query the source for information such as the palette which we need
            // to determine if we need to format convert or not.
            WicSourceHandle = _frameSource;
            UpdateCachedSettings();

            lock (_syncObject)
            {
                WicSourceHandle = CreateCachedBitmap(this, _frameSource, _createOptions, _cacheOption, Palette);
            }

            IsSourceCached = (_cacheOption != BitmapCacheOption.None);
            CreationCompleted = true;
            UpdateCachedSettings();
            EnsureThumbnail();
        }

        //
        // Workaround for a change caused by a bug fix, CopyCommon checks this property when
        // copying the delegates attached to events.
        // Default implementation in BitmapSource, see comments in BitmapSource.cs for details.
        //
        internal override bool ShouldCloneEventDelegates
        {
            get { return false; }
        }

        #endregion

        #region Private Methods

        /// Fired when the decoder download has completed
        private void OnDownloadCompleted(object sender, EventArgs e)
        {
            //
            // The sender should be the LateBoundDecoder that we hooked when it was _decoder.
            //
            LateBoundBitmapDecoder decoder = (LateBoundBitmapDecoder)sender;

            //
            // Unhook the decoders download events
            //
            decoder.DownloadCompleted -= OnDownloadCompleted;
            decoder.DownloadProgress -= OnDownloadProgress;
            decoder.DownloadFailed -= OnDownloadFailed;
            
            FireChanged();
            _downloadEvent.InvokeEvents(this, null);
        }

        /// Called when download progress is made
        private void OnDownloadProgress(object sender, DownloadProgressEventArgs e)
        {
            _progressEvent.InvokeEvents(this, e);
        }

        /// Called when download fails
        private void OnDownloadFailed(object sender, ExceptionEventArgs e)
        {
            //
            // The sender should be the LateBoundDecoder that we hooked when it was _decoder.
            //
            LateBoundBitmapDecoder decoder = (LateBoundBitmapDecoder)sender;

            //
            // Unhook the decoders download events
            //
            decoder.DownloadCompleted -= OnDownloadCompleted;
            decoder.DownloadProgress -= OnDownloadProgress;
            decoder.DownloadFailed -= OnDownloadFailed;
            
            _failedEvent.InvokeEvents(this, e);
        }

        /// Fired when the original's decoder's download has completed
        private void OnOriginalDownloadCompleted(BitmapFrameDecode original, EventArgs e)
        {
            CleanUpWeakEventSink();

            // Update the underlying decoder to match the original's
            // We already have a _decoder from the cloning, but it's referencing the
            // LateBoundBitmapDecoder. When download completes, LateBoundBitmapDecoder calls
            // EnsureDecoder to make the _realDecoder (something like JpegBitmapDecoder),
            // then calls SetupFrames on the _realDecoder, which calls BitmapFrameDecode's
            // UpdateDecoder to set _decoder to the JpegBitmapDecoder. So the original's _decoder
            // changes after download completes, and the clone should change as well.
            UpdateDecoder(original.Decoder);

            FireChanged();
            _downloadEvent.InvokeEvents(this, e);
        }

        /// Called when the original's decoder's download fails
        private void OnOriginalDownloadFailed(ExceptionEventArgs e)
        {
            CleanUpWeakEventSink();

            _failedEvent.InvokeEvents(this, e);
        }

        private void CleanUpWeakEventSink()
        {
            // Unhook the decoders download events
            _weakBitmapFrameDecodeEventSink.DetachSourceDownloadHandlers();
            _weakBitmapFrameDecodeEventSink = null;
        }

        /// <summary>
        /// Ensure that the thumbnail is created/cached
        /// </summary>
        private void EnsureThumbnail()
        {
            if (_isThumbnailCached || IsDownloading)
            {
                return;
            }
            else
            {
                EnsureSource();

                IntPtr /* IWICBitmapSource */ thumbnail = IntPtr.Zero;

                lock (_syncObject)
                {
                    // Check if there is embedded thumbnail or not
                    int hr = UnsafeNativeMethods.WICBitmapFrameDecode.GetThumbnail(
                        _frameSource,
                        out thumbnail
                        );

                    if (hr != (int)WinCodecErrors.WINCODEC_ERR_CODECNOTHUMBNAIL)
                    {
                        HRESULT.Check(hr);
                    }
                }

                _isThumbnailCached = true;

                if (thumbnail != IntPtr.Zero)
                {
                    BitmapSourceSafeMILHandle thumbHandle = new BitmapSourceSafeMILHandle(thumbnail);
                    SafeMILHandle unmanagedPalette = BitmapPalette.CreateInternalPalette();
                    BitmapPalette palette = null;

                    int hr = UnsafeNativeMethods.WICBitmapSource.CopyPalette(
                                thumbHandle,
                                unmanagedPalette
                                );
                    if (hr == HRESULT.S_OK)
                    {
                        palette = new BitmapPalette(unmanagedPalette);
                    }

                    _thumbnail = new UnmanagedBitmapWrapper(
                        CreateCachedBitmap(
                            null,
                            thumbHandle,
                            BitmapCreateOptions.PreservePixelFormat,
                            _cacheOption,
                            palette
                            ));
                    _thumbnail.Freeze();
                }
            }
        }

        /// <summary>
        /// Returns cached metadata and creates BitmapMetadata if it does not exist.
        /// This code will demand site of origin permissions.
        /// </summary>
        internal override BitmapMetadata InternalMetadata
        {
            get
            {
                // Demand Site Of Origin on the URI before usage of metadata.
                CheckIfSiteOfOrigin();

                if (!_isMetadataCached && !IsDownloading)
                {
                    EnsureSource();

                    IntPtr /* IWICMetadataQueryReader */ metadata = IntPtr.Zero;

                    lock (_syncObject)
                    {
                        // Check if there is embedded metadata or not
                        int hr = UnsafeNativeMethods.WICBitmapFrameDecode.GetMetadataQueryReader(
                            _frameSource,
                            out metadata
                            );

                        if (hr != (int)WinCodecErrors.WINCODEC_ERR_UNSUPPORTEDOPERATION)
                        {
                            HRESULT.Check(hr);
                        }
                    }

                    if (metadata != IntPtr.Zero)
                    {
                        SafeMILHandle metadataHandle = new SafeMILHandle(metadata);

                        _metadata = new BitmapMetadata(metadataHandle, true, _decoder != null ? _decoder.IsMetadataFixedSize : false, _syncObject);
                        _metadata.Freeze();
                    }

                    _isMetadataCached = true;
                }

                return _metadata;
            }
            set
            {
                throw new System.NotImplementedException();
            }
        }

        /// <summary>
        /// Ensure that a BitmapSource is created
        /// </summary>
        private void EnsureSource()
        {
            if (_frameSource == null)
            {
                if (_decoder == null)
                {
                    HRESULT.Check((int)WinCodecErrors.WINCODEC_ERR_NOTINITIALIZED);
                }

                //
                // Its possible that the frame was originally created with a network URI
                // and DelayCreation was enabled. In this case, the decoder may not yet
                // exist even though the download is complete. The code below creates a
                // decoder if one does not exist.
                //
                if (_decoder.InternalDecoder == null)
                {
                    Debug.Assert(_decoder is LateBoundBitmapDecoder);
                    Debug.Assert(IsDownloading == false);

                    _decoder = ((LateBoundBitmapDecoder)_decoder).Decoder;
                    _syncObject = _decoder.SyncObject;

                    Debug.Assert(_decoder.InternalDecoder != null);
                }

                IntPtr frameDecode = IntPtr.Zero;

                Debug.Assert(_syncObject != null);
                lock (_syncObject)
                {
                    HRESULT.Check(UnsafeNativeMethods.WICBitmapDecoder.GetFrame(
                        _decoder.InternalDecoder,
                        (uint)_frameNumber,
                        out frameDecode
                        ));

                    _frameSource = new BitmapSourceSafeMILHandle(frameDecode);
                    _frameSource.CalculateSize();
                }
            }
        }

        #endregion

        #region Data Members

        /// IWICBitmapFrameDecode source
        private BitmapSourceSafeMILHandle _frameSource = null;

        /// Frame number
        private int _frameNumber;

        /// Is the thumbnail cached
        private bool _isThumbnailCached;

        /// Is the metadata cached
        private bool _isMetadataCached;

        /// If the ColorContext is already cached
        private bool _isColorContextCached = false;

        /// CreateOptions for this Frame
        private BitmapCreateOptions _createOptions;

        /// CacheOption for this Frame
        private BitmapCacheOption _cacheOption;

        /// Decoder
        private BitmapDecoder _decoder;

        #endregion

        #region WeakBitmapSourceEvents

        // Used to propagate DownloadCompleted events for cloned BitmapFrameDecodes that are still downloading
        private WeakBitmapFrameDecodeEventSink _weakBitmapFrameDecodeEventSink;

        private class WeakBitmapFrameDecodeEventSink : WeakReference
        {
            public WeakBitmapFrameDecodeEventSink(BitmapFrameDecode cloned, BitmapFrameDecode original)
                : base(cloned)
            {
                _original = original;

                if (!_original.IsFrozen)
                {
                    _original.DownloadCompleted += OnSourceDownloadCompleted;
                    _original.DownloadFailed += OnSourceDownloadFailed;
                    _original.DownloadProgress += OnSourceDownloadProgress;
                }
            }

            public void OnSourceDownloadCompleted(object sender, EventArgs e)
            {
                BitmapFrameDecode clone = this.Target as BitmapFrameDecode;
                if (null != clone)
                {
                    clone.OnOriginalDownloadCompleted(_original, e);
                }
                else
                {
                    DetachSourceDownloadHandlers();
                }
            }

            public void OnSourceDownloadFailed(object sender, ExceptionEventArgs e)
            {
                BitmapFrameDecode clone = this.Target as BitmapFrameDecode;
                if (null != clone)
                {
                    clone.OnOriginalDownloadFailed(e);
                }
                else
                {
                    DetachSourceDownloadHandlers();
                }
            }

            public void OnSourceDownloadProgress(object sender, DownloadProgressEventArgs e)
            {
                BitmapFrameDecode clone = this.Target as BitmapFrameDecode;
                if (null != clone)
                {
                    clone.OnDownloadProgress(sender, e);
                }
                else
                {
                    DetachSourceDownloadHandlers();
                }
            }

            public void DetachSourceDownloadHandlers()
            {
                if (!_original.IsFrozen)
                {
                    _original.DownloadCompleted -= OnSourceDownloadCompleted;
                    _original.DownloadFailed -= OnSourceDownloadFailed;
                    _original.DownloadProgress -= OnSourceDownloadProgress;
                }
            }

            private BitmapFrameDecode _original;
        }

        #endregion WeakBitmapSourceEvents
    }

    #endregion // BitmapFrameDecode
}

