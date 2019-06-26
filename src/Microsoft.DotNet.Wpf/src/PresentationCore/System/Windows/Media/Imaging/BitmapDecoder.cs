// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
//
#pragma warning disable 1634, 1691 // Allow suppression of certain presharp messages

using System;
using System.IO;
using System.IO.Packaging;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.Design.Serialization;
using System.Collections.ObjectModel;
using System.Reflection;
using System.Security;
using System.Text.RegularExpressions;
using MS.Internal;
using MS.Internal.AppModel;
using MS.Win32.PresentationCore;
using Microsoft.Win32.SafeHandles;
using System.Diagnostics;
using System.Windows.Media;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Threading;
using System.Windows.Media.Imaging;
using MS.Internal.PresentationCore;                        // SecurityHelper
using SR=MS.Internal.PresentationCore.SR;
using SRID=MS.Internal.PresentationCore.SRID;
using System.Net;
using System.Net.Cache;
using System.Text;

namespace System.Windows.Media.Imaging
{
    #region BitmapDecoder

    /// <summary>
    /// BitmapDecoder is a container for bitmap frames.  Each bitmap frame is an BitmapFrame.
    /// Any BitmapFrame it returns are frozen
    /// be immutable.
    /// </summary>
    public abstract class BitmapDecoder : DispatcherObject
    {
        #region Constructors

        static BitmapDecoder()
        {
        }
        /// <summary>
        /// Default constructor
        /// </summary>
        protected BitmapDecoder()
        {
        }

        /// <summary>
        /// Internal constructor
        /// </summary>
        internal BitmapDecoder(bool isBuiltIn)
        {
            _isBuiltInDecoder = isBuiltIn;
        }

        /// <summary>
        /// Constructor
        /// </summary>
        internal BitmapDecoder(
            Uri bitmapUri,
            BitmapCreateOptions createOptions,
            BitmapCacheOption cacheOption,
            Guid expectedClsId
            )
        {
            Guid clsId = Guid.Empty;
            bool isOriginalWritable = false;

            if (bitmapUri == null)
            {
                throw new ArgumentNullException("bitmapUri");
            }

            if ((createOptions & BitmapCreateOptions.IgnoreImageCache) != 0)
            {
                ImagingCache.RemoveFromDecoderCache(bitmapUri);
            }

            BitmapDecoder decoder = CheckCache(bitmapUri, out clsId);
            if (decoder != null)
            {
                _decoderHandle = decoder.InternalDecoder;
            }
            else
            {
                _decoderHandle = SetupDecoderFromUriOrStream(
                    bitmapUri,
                    null,
                    cacheOption,
                    out clsId,
                    out isOriginalWritable,
                    out _uriStream,
                    out _unmanagedMemoryStream,
                    out _safeFilehandle
                    );

                if (_uriStream == null)
                {
                    GC.SuppressFinalize(this);
                }
            }

            if (clsId != expectedClsId)
            {
                throw new FileFormatException(bitmapUri, SR.Get(SRID.Image_CantDealWithUri));
            }

            _uri = bitmapUri;
            _createOptions = createOptions;
            _cacheOption = cacheOption;
            _syncObject = _decoderHandle;
            _isOriginalWritable = isOriginalWritable;
            Initialize(decoder);
        }

        /// <summary>
        /// Constructor
        /// </summary>
        internal BitmapDecoder(
            Stream bitmapStream,
            BitmapCreateOptions createOptions,
            BitmapCacheOption cacheOption,
            Guid expectedClsId
            )
        {
            Guid clsId = Guid.Empty;
            bool isOriginalWritable = false;

            if (bitmapStream == null)
            {
                throw new ArgumentNullException("bitmapStream");
            }

            _decoderHandle = SetupDecoderFromUriOrStream(
                null,
                bitmapStream,
                cacheOption,
                out clsId,
                out isOriginalWritable,
                out _uriStream,
                out _unmanagedMemoryStream,
                out _safeFilehandle
                );

            if (_uriStream == null)
            {
                GC.SuppressFinalize(this);
            }

            if (clsId != Guid.Empty && clsId != expectedClsId)
            {
                throw new FileFormatException(null, SR.Get(SRID.Image_CantDealWithStream));
            }

            _stream = bitmapStream;
            _createOptions = createOptions;
            _cacheOption = cacheOption;
            _syncObject = _decoderHandle;
            _isOriginalWritable = isOriginalWritable;
            Initialize(null);
        }

        /// <summary>
        /// Constructor
        /// </summary>
        internal BitmapDecoder(
            SafeMILHandle decoderHandle,
            BitmapDecoder decoder,
            Uri baseUri,
            Uri uri,
            Stream stream,
            BitmapCreateOptions createOptions,
            BitmapCacheOption cacheOption,
            bool insertInDecoderCache,
            bool isOriginalWritable,
            Stream uriStream,
            UnmanagedMemoryStream unmanagedMemoryStream,
            SafeFileHandle safeFilehandle
            )
        {
            _decoderHandle = decoderHandle;
            _baseUri = baseUri;
            _uri = uri;
            _stream = stream;
            _createOptions = createOptions;
            _cacheOption = cacheOption;
            _syncObject = decoderHandle;
            _shouldCacheDecoder = insertInDecoderCache;
            _isOriginalWritable = isOriginalWritable;
            _uriStream = uriStream;
            _unmanagedMemoryStream = unmanagedMemoryStream;
            _safeFilehandle = safeFilehandle;

            if (_uriStream == null)
            {
                GC.SuppressFinalize(this);
            }

            Initialize(decoder);
        }

        /// <summary>
        /// Close the stream pointing to the file after this BitmapDecoder is no longer used.
        /// It might have been closed before in Initialize if the cache option was OnLoad or
        /// on decode if the cache option was OnDemand/Default. In those cases, the finalizer
        /// would have been suppressed.
        /// </summary>
        ~BitmapDecoder()
        {
            //
            // Normally _uriStream gets GCed because this BitmapDecoder is the only thing that
            // references it, and the stream's finalizer closes it. However, in some cases, other
            // objects still have references to the _uriStream, so it won't get GCed because this
            // decoder does. One such case is getting the stream from a pack uri that points to
            // a resource. In those cases, PackagePart created the streams and is also holding on
            // to them. It won't let go unless the stream is closed, but the stream won't close
            // unless it's been let go, so the stream is leaked. Closing the stream removes the
            // leak.
            //
            // _uriStream is always created within SetupDecoderFromUriOrStream. Either it's set
            // using an out parameter, or it's set in the internal constructor that takes a
            // uriStream parameter. That internal constructor is called by the sealed subclasses
            // of BitmapDecoder (such as JpegBitmapDecoder), those subclasses are constructed in
            // CreateFromUriOrStream, which also gets uriStream from SetupDecoderFromUriOrStream.
            //
            if (_uriStream != null)
            {
                _uriStream.Close();
            }
        }

        /// <summary>
        /// Create BitmapDecoder from the uri or stream. If both are specified, the uri
        /// is chosen.
        /// </summary>
        internal static BitmapDecoder CreateFromUriOrStream(
            Uri baseUri,
            Uri uri,
            Stream stream,
            BitmapCreateOptions createOptions,
            BitmapCacheOption cacheOption,
            RequestCachePolicy uriCachePolicy,
            bool insertInDecoderCache
            )
        {
            Guid clsId = Guid.Empty;
            bool isOriginalWritable = false;
            SafeMILHandle decoderHandle = null;
            BitmapDecoder cachedDecoder = null;
            Uri finalUri = null;
            Stream uriStream = null;
            UnmanagedMemoryStream unmanagedMemoryStream = null;
            SafeFileHandle safeFilehandle = null;

            if (uri != null)
            {
                finalUri = (baseUri != null) ?
                               System.Windows.Navigation.BaseUriHelper.GetResolvedUri(baseUri, uri) :
                               uri;

                if (insertInDecoderCache)
                {
                    if ((createOptions & BitmapCreateOptions.IgnoreImageCache) != 0)
                    {
                        ImagingCache.RemoveFromDecoderCache(finalUri);
                    }

                    cachedDecoder = CheckCache(
                        finalUri,
                        out clsId
                        );
                }
            }

            // try to retrieve the cached decoder
            if (cachedDecoder != null)
            {
                decoderHandle = cachedDecoder.InternalDecoder;
            }
            else if ((finalUri != null) && (finalUri.IsAbsoluteUri) && (stream == null) &&
                     ((finalUri.Scheme == Uri.UriSchemeHttp) ||
                      (finalUri.Scheme == Uri.UriSchemeHttps)))
            {
                return new LateBoundBitmapDecoder(baseUri, uri, stream, createOptions, cacheOption, uriCachePolicy);
            }
            else if ((stream != null) && (!stream.CanSeek))
            {
                return new LateBoundBitmapDecoder(baseUri, uri, stream, createOptions, cacheOption, uriCachePolicy);
            }
            else
            {
                // Create an unmanaged decoder
                decoderHandle = BitmapDecoder.SetupDecoderFromUriOrStream(
                    finalUri,
                    stream,
                    cacheOption,
                    out clsId,
                    out isOriginalWritable,
                    out uriStream,
                    out unmanagedMemoryStream,
                    out safeFilehandle
                    );
            }

            BitmapDecoder decoder = null;

            // Find out the decoder type and wrap it appropriately and return that
            if (MILGuidData.GUID_ContainerFormatBmp == clsId)
            {
                decoder = new BmpBitmapDecoder(
                    decoderHandle,
                    cachedDecoder,
                    baseUri,
                    uri,
                    stream,
                    createOptions,
                    cacheOption,
                    insertInDecoderCache,
                    isOriginalWritable,
                    uriStream,
                    unmanagedMemoryStream,
                    safeFilehandle
                    );
            }
            else if (MILGuidData.GUID_ContainerFormatGif == clsId)
            {
                decoder = new GifBitmapDecoder(
                    decoderHandle,
                    cachedDecoder,
                    baseUri,
                    uri,
                    stream,
                    createOptions,
                    cacheOption,
                    insertInDecoderCache,
                    isOriginalWritable,
                    uriStream,
                    unmanagedMemoryStream,
                    safeFilehandle
                    );
            }
            else if (MILGuidData.GUID_ContainerFormatIco == clsId)
            {
                decoder = new IconBitmapDecoder(
                    decoderHandle,
                    cachedDecoder,
                    baseUri,
                    uri,
                    stream,
                    createOptions,
                    cacheOption,
                    insertInDecoderCache,
                    isOriginalWritable,
                    uriStream,
                    unmanagedMemoryStream,
                    safeFilehandle
                    );
            }
            else if (MILGuidData.GUID_ContainerFormatJpeg == clsId)
            {
                decoder = new JpegBitmapDecoder(
                    decoderHandle,
                    cachedDecoder,
                    baseUri,
                    uri,
                    stream,
                    createOptions,
                    cacheOption,
                    insertInDecoderCache,
                    isOriginalWritable,
                    uriStream,
                    unmanagedMemoryStream,
                    safeFilehandle
                    );
            }
            else if (MILGuidData.GUID_ContainerFormatPng == clsId)
            {
                decoder = new PngBitmapDecoder(
                    decoderHandle,
                    cachedDecoder,
                    baseUri,
                    uri,
                    stream,
                    createOptions,
                    cacheOption,
                    insertInDecoderCache,
                    isOriginalWritable,
                    uriStream,
                    unmanagedMemoryStream,
                    safeFilehandle
                    );
            }
            else if (MILGuidData.GUID_ContainerFormatTiff == clsId)
            {
                decoder = new TiffBitmapDecoder(
                    decoderHandle,
                    cachedDecoder,
                    baseUri,
                    uri,
                    stream,
                    createOptions,
                    cacheOption,
                    insertInDecoderCache,
                    isOriginalWritable,
                    uriStream,
                    unmanagedMemoryStream,
                    safeFilehandle
                    );
            }
            else if (MILGuidData.GUID_ContainerFormatWmp == clsId)
            {
                decoder = new WmpBitmapDecoder(
                    decoderHandle,
                    cachedDecoder,
                    baseUri,
                    uri,
                    stream,
                    createOptions,
                    cacheOption,
                    insertInDecoderCache,
                    isOriginalWritable,
                    uriStream,
                    unmanagedMemoryStream,
                    safeFilehandle
                    );
            }
            else
            {
                decoder = new UnknownBitmapDecoder(
                    decoderHandle,
                    cachedDecoder,
                    baseUri,
                    uri,
                    stream,
                    createOptions,
                    cacheOption,
                    insertInDecoderCache,
                    isOriginalWritable,
                    uriStream,
                    unmanagedMemoryStream,
                    safeFilehandle
                    );
            }

            return decoder;
        }

        /// <summary>
        /// Create a BitmapDecoder from a Uri with the specified BitmapCreateOptions and
        /// BitmapCacheOption
        /// </summary>
        /// <param name="bitmapUri">Uri to decode</param>
        /// <param name="createOptions">Bitmap Create Options</param>
        /// <param name="cacheOption">Bitmap Caching Option</param>
        public static BitmapDecoder Create(
            Uri bitmapUri,
            BitmapCreateOptions createOptions,
            BitmapCacheOption cacheOption
            )
        {
            return Create(bitmapUri, createOptions, cacheOption, null);
        }

        /// <summary>
        /// Create a BitmapDecoder from a Uri with the specified BitmapCreateOptions and
        /// BitmapCacheOption
        /// </summary>
        /// <param name="bitmapUri">Uri to decode</param>
        /// <param name="createOptions">Bitmap Create Options</param>
        /// <param name="cacheOption">Bitmap Caching Option</param>
        /// <param name="uriCachePolicy">Optional web request cache policy</param>
        public static BitmapDecoder Create(
            Uri bitmapUri,
            BitmapCreateOptions createOptions,
            BitmapCacheOption cacheOption,
            RequestCachePolicy uriCachePolicy
            )
        {
            if (bitmapUri == null)
            {
                throw new ArgumentNullException("bitmapUri");
            }

            return CreateFromUriOrStream(
                null,
                bitmapUri,
                null,
                createOptions,
                cacheOption,
                uriCachePolicy,
                true
                );
        }

        /// <summary>
        /// Create a BitmapDecoder from a Stream with the specified BitmapCreateOptions and
        /// BitmapCacheOption
        /// </summary>
        /// <param name="bitmapStream">Stream to decode</param>
        /// <param name="createOptions">Bitmap Create Options</param>
        /// <param name="cacheOption">Bitmap Caching Option</param>
        public static BitmapDecoder Create(
            Stream bitmapStream,
            BitmapCreateOptions createOptions,
            BitmapCacheOption cacheOption
            )
        {
            if (bitmapStream == null)
            {
                throw new ArgumentNullException("bitmapStream");
            }

            return CreateFromUriOrStream(
                null,
                null,
                bitmapStream,
                createOptions,
                cacheOption,
                null,
                true
                );
        }

        #endregion

        #region Properties

        /// <summary>
        /// If there is an palette, return it.
        /// Otherwise, return null.
        /// </summary>
        public virtual BitmapPalette Palette
        {
            get
            {
                VerifyAccess();
                EnsureBuiltInDecoder();

                if (!_isPaletteCached)
                {
                    SafeMILHandle /* IWICBitmapPalette */ paletteHandle = BitmapPalette.CreateInternalPalette();

                    lock (_syncObject)
                    {
                        int hr = UnsafeNativeMethods.WICBitmapDecoder.CopyPalette(
                            _decoderHandle,
                            paletteHandle
                            );
                        if (hr != (int)WinCodecErrors.WINCODEC_ERR_PALETTEUNAVAILABLE)
                        {
                            HRESULT.Check(hr);
                            _palette = new BitmapPalette(paletteHandle);
                        }
                    }

                    _isPaletteCached = true;
                }

                return _palette;
            }
        }

        /// <summary>
        /// If there is an embedded color profile, return it.
        /// Otherwise, return null.
        /// </summary>
        public virtual ReadOnlyCollection<ColorContext> ColorContexts
        {
            get
            {
                VerifyAccess();

                return InternalColorContexts;
            }
        }

        /// <summary>
        /// If there is a global thumbnail, return it.
        /// Otherwise, return null. The returned source is frozen.
        /// </summary>
        public virtual BitmapSource Thumbnail
        {
            get
            {
                VerifyAccess();
                EnsureBuiltInDecoder();

                if (!_isThumbnailCached)
                {
                    IntPtr /* IWICBitmapSource */ thumbnail = IntPtr.Zero;

                    // Check if there is embedded thumbnail or not
                    lock (_syncObject)
                    {
                        int hr = UnsafeNativeMethods.WICBitmapDecoder.GetThumbnail(
                            _decoderHandle,
                            out thumbnail
                            );
                        if (hr != (int)WinCodecErrors.WINCODEC_ERR_CODECNOTHUMBNAIL)
                        {
                            HRESULT.Check(hr);
                        }
                    }

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
                            BitmapSource.CreateCachedBitmap(
                                null,
                                thumbHandle,
                                BitmapCreateOptions.PreservePixelFormat,
                                _cacheOption,
                                palette
                                ));
                        _thumbnail.Freeze();
                    }

                    _isThumbnailCached = true;
                }

                return _thumbnail;
            }
        }

        /// <summary>
        /// If there is a global metadata, return it.
        /// Otherwise, return null. The returned source is frozen.
        /// </summary>
        public virtual BitmapMetadata Metadata
        {
            get
            {
                VerifyAccess();
                EnsureBuiltInDecoder();

                if (!_isMetadataCached)
                {
                    IntPtr /* IWICMetadataQueryReader */ metadata = IntPtr.Zero;

                    lock (_syncObject)
                    {
                        int hr = UnsafeNativeMethods.WICBitmapDecoder.GetMetadataQueryReader(
                            _decoderHandle,
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

                        _metadata = new BitmapMetadata(metadataHandle, true, IsMetadataFixedSize, _syncObject);
                        _metadata.Freeze();
                    }

                    _isMetadataCached = true;
                }

                return _metadata;
            }
        }

        /// <summary>
        /// The info that identifies this codec.
        /// </summary>
        public virtual BitmapCodecInfo CodecInfo
        {
            get
            {
                VerifyAccess();
                EnsureBuiltInDecoder();

                // There should always be a codec info.
                if (_codecInfo == null)
                {
                    SafeMILHandle /* IWICBitmapDecoderInfo */ codecInfoHandle =  new SafeMILHandle();

                    HRESULT.Check(UnsafeNativeMethods.WICBitmapDecoder.GetDecoderInfo(
                        _decoderHandle,
                        out codecInfoHandle
                        ));

                    _codecInfo = new BitmapCodecInfoInternal(codecInfoHandle);
                }

                return _codecInfo;
            }
        }

        /// <summary>
        /// Access to the individual frames. All BitmapFrames are frozen.
        /// </summary>
        public virtual ReadOnlyCollection<BitmapFrame> Frames
        {
            get
            {
                VerifyAccess();
                EnsureBuiltInDecoder();

                if (_frames == null)
                {
                    SetupFrames(null, null);
                }

                if (_readOnlyFrames == null)
                {
                    _readOnlyFrames = new ReadOnlyCollection<BitmapFrame>(_frames);
                }

                return _readOnlyFrames;
            }
        }

        /// <summary>
        /// If there is a global preview image, return it.
        /// Otherwise, return null. The returned source is frozen.
        /// </summary>
        public virtual BitmapSource Preview
        {
            get
            {
                VerifyAccess();
                EnsureBuiltInDecoder();

                if (!_isPreviewCached)
                {
                    IntPtr /* IWICBitmapSource */ preview = IntPtr.Zero;

                    lock (_syncObject)
                    {
                        // Check if there is embedded preview or not
                        int hr = UnsafeNativeMethods.WICBitmapDecoder.GetPreview(
                            _decoderHandle,
                            out preview
                            );
                        if (hr != (int)WinCodecErrors.WINCODEC_ERR_UNSUPPORTEDOPERATION)
                        {
                            HRESULT.Check(hr);
                        }
                    }

                    if (preview != IntPtr.Zero)
                    {
                        BitmapSourceSafeMILHandle previewHandle = new BitmapSourceSafeMILHandle(preview);
                        SafeMILHandle unmanagedPalette = BitmapPalette.CreateInternalPalette();
                        BitmapPalette palette = null;
                    
                        int hr = UnsafeNativeMethods.WICBitmapSource.CopyPalette(
                                    previewHandle,
                                    unmanagedPalette
                                    );
                        if (hr == HRESULT.S_OK)
                        {
                            palette = new BitmapPalette(unmanagedPalette);
                        }
                    
                        _preview = new UnmanagedBitmapWrapper(
                            BitmapSource.CreateCachedBitmap(
                                null,
                                previewHandle,
                                BitmapCreateOptions.PreservePixelFormat,
                                _cacheOption,
                                palette
                                ));
                        _preview.Freeze();
                    }
                    
                    _isPreviewCached = true;
                }

                return _preview;
            }
        }

        /// <summary>
        /// Returns true if the decoder is downloading content
        /// </summary>
        public virtual bool IsDownloading
        {
            get
            {
                VerifyAccess();
                EnsureBuiltInDecoder();
                return false;
            }
        }

        /// <summary>
        /// Raised when decoder has completed downloading content
        /// May not be raised for all content.
        /// </summary>
        public virtual event EventHandler DownloadCompleted
        {
            add
            {
                VerifyAccess();
                EnsureBuiltInDecoder();
                _downloadEvent.AddEvent(value);
            }
            remove
            {
                VerifyAccess();
                EnsureBuiltInDecoder();
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
                VerifyAccess();
                EnsureBuiltInDecoder();
                _progressEvent.AddEvent(value);
            }
            remove
            {
                VerifyAccess();
                EnsureBuiltInDecoder();
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
                VerifyAccess();
                EnsureBuiltInDecoder();
                _failedEvent.AddEvent(value);
            }
            remove
            {
                VerifyAccess();
                EnsureBuiltInDecoder();
                _failedEvent.RemoveEvent(value);
            }
        }

        #endregion

        #region Public Properties

        /// <summary>
        /// Create an in-place bitmap metadata writer.
        /// </summary>
        public virtual InPlaceBitmapMetadataWriter CreateInPlaceBitmapMetadataWriter()
        {
            VerifyAccess();
            EnsureBuiltInDecoder();

            CheckOriginalWritable();

            return InPlaceBitmapMetadataWriter.CreateFromDecoder(_decoderHandle, _syncObject);
        }

        #endregion

        #region ToString

        /// <summary>
        /// ToString
        /// </summary>
        public override string ToString()
        {
            VerifyAccess();

            if (!_isBuiltInDecoder)
            {
                return base.ToString();
            }

            if (_uri != null)
            {
                if (_baseUri != null)
                {
                    Uri uri = new Uri(_baseUri, _uri);
                    return BindUriHelper.UriToString(uri);
                }
                else
                {
                    return BindUriHelper.UriToString(_uri);
                }
            }
            else
            {
                return SafeSecurityHelper.IMAGE;
            }
        }

        #endregion

        #region Internal Properties

        /// <summary>
        /// Internal Decoder
        /// </summary>
        internal SafeMILHandle InternalDecoder
        {
            get
            {
                return _decoderHandle;
            }
        }

        /// <summary>
        /// Returns whether metadata is fixed size or not.
        /// </summary>
        internal virtual bool IsMetadataFixedSize
        {
            get
            {
                return false;
            }
        }

        /// <summary>
        /// Synchronization Object.  Any unmanaged PInvoke/call that requires synchronization
        /// must lock on the sync object.  This object must be internal or private so as not
        /// to be publicly lockable.
        /// </summary>
        internal object SyncObject
        {
            get
            {
                Debug.Assert(_syncObject != null);
                return _syncObject;
            }
        }

        /// <summary>
        /// Used as a delegate in InternalColorContexts to get the unmanaged IWICColorContexts
        /// </summary>
        private int GetColorContexts(ref uint numContexts, IntPtr[] colorContextPtrs)
        {
            Invariant.Assert(colorContextPtrs == null || numContexts <= colorContextPtrs.Length);
            
            return UnsafeNativeMethods.WICBitmapDecoder.GetColorContexts(_decoderHandle, numContexts, colorContextPtrs, out numContexts);
        }

        /// <summary>
        /// If there is an embedded color profile, return it.
        /// Otherwise, return null.
        /// </summary>
        internal ReadOnlyCollection<ColorContext> InternalColorContexts
        {
            get
            {
                EnsureBuiltInDecoder();

                if (!_isColorContextCached)
                {
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
        /// Checks whether the underlying source is writable (useful for in-place metadata editing)
        /// </summary>
        internal void CheckOriginalWritable()
        {
            if (!_isOriginalWritable)
            {
                throw new System.InvalidOperationException(SR.Get(SRID.Image_OriginalStreamReadOnly));
            }
        }

        #endregion

        #region Internal/Private Methods

        internal static SafeMILHandle SetupDecoderFromUriOrStream(
            Uri uri,
            Stream stream,
            BitmapCacheOption cacheOption,
            out Guid clsId,
            out bool isOriginalWritable,
            out Stream uriStream,
            out UnmanagedMemoryStream unmanagedMemoryStream,
            out SafeFileHandle safeFilehandle
            )
        {
            SafeMILHandle decoderHandle;
            IntPtr decoder = IntPtr.Zero;
            System.IO.Stream bitmapStream = null;
            string mimeType = String.Empty;
            unmanagedMemoryStream = null;
            safeFilehandle = null;
            isOriginalWritable = false;
            uriStream = null;

            if ((uri != null) && (stream != null))
            {
                // In this case we expect the Uri to be http(s)
                Debug.Assert((uri.Scheme == Uri.UriSchemeHttp) || (uri.Scheme == Uri.UriSchemeHttps));
                Debug.Assert(stream.CanSeek);
            }

            // Uri
            if (uri != null)
            {
                if (uri.IsAbsoluteUri)
                {
                    // This code path executes only for pack web requests
                    if (String.Compare(uri.Scheme, PackUriHelper.UriSchemePack, StringComparison.OrdinalIgnoreCase) == 0)
                    {
                        WebResponse response = WpfWebRequestHelper.CreateRequestAndGetResponse(uri);
                        mimeType = response.ContentType;
                        bitmapStream = response.GetResponseStream();
                        uriStream = bitmapStream;
                    }
                }

                if ((bitmapStream == null) || (bitmapStream == System.IO.Stream.Null))
                {
                    // We didn't get a stream from the pack web request, so we have
                    // to try to create one ourselves.
                    if (uri.IsAbsoluteUri)
                    {
                        // The Uri class can't tell if it is a file unless it
                        // has an absolute path.
                        int targetZone = SecurityHelper.MapUrlToZoneWrapper(uri);
                        if (targetZone == MS.Win32.NativeMethods.URLZONE_LOCAL_MACHINE)
                        {
                            if (uri.IsFile)
                            {
                                // FileStream does a demand for us, so no need to do a demand
                                bitmapStream = new System.IO.FileStream(uri.LocalPath, FileMode.Open, FileAccess.Read, FileShare.Read);
                            }
                        }
                        else // Any other zone
                        {
                            // UNC path for a file which is not http
                            if (uri.IsFile && uri.IsUnc) // for UNC
                            {
                                bitmapStream = ProcessUncFiles(uri);
                            }
                            else if (uri.Scheme == Uri.UriSchemeHttp) // for http
                            {
                                bitmapStream = ProcessHttpFiles(uri,stream);
                            }
                            else if (uri.Scheme == Uri.UriSchemeHttps) // for https
                            {
                                bitmapStream = ProcessHttpsFiles(uri,stream);
                            }
                            else
                            {
                                // The Uri is a custom Uri. Try to grab the stream from its WebResponse.
                                bitmapStream = WpfWebRequestHelper.CreateRequestAndGetResponseStream(uri);
                            }
                        }
                    }
                    else
                    {
                        #pragma warning disable 6518
                        // We don't have an absolute URI, so we don't necessarily know
                        // if it is a file, but we'll have to assume it is and try to
                        // create a stream from the original string.
                        bitmapStream = new System.IO.FileStream(uri.OriginalString, FileMode.Open, FileAccess.Read, FileShare.Read);
                        #pragma warning restore 6518
                    }

                    uriStream = bitmapStream;
                }
            }

            // We need to use the stream created from the Uri.
            if (bitmapStream != null)
            {
                stream = bitmapStream;
            }
            else
            {
                // Note whether the original stream is writable.
                isOriginalWritable = stream.CanSeek && stream.CanWrite;
            }

            // Make sure we always use a seekable stream to avoid problems with http Uris etc.
            stream = GetSeekableStream(stream);

            if (stream is UnmanagedMemoryStream)
            {
                unmanagedMemoryStream = stream as UnmanagedMemoryStream;
            }

            IntPtr comStream = IntPtr.Zero;

            if (stream is System.IO.FileStream)
            {
                System.IO.FileStream filestream = stream as System.IO.FileStream;

                try
                {
                    safeFilehandle = filestream.SafeFileHandle;
                }
                catch
                {
                    // If Filestream doesn't support SafeHandle then revert to old code path.
                    safeFilehandle = null;
                }
            }

            try
            {
                Guid vendorMicrosoft = new Guid(MILGuidData.GUID_VendorMicrosoft);
                UInt32 metadataFlags = (uint)WICMetadataCacheOptions.WICMetadataCacheOnDemand;

                if (cacheOption == BitmapCacheOption.OnLoad)
                {
                    metadataFlags = (uint)WICMetadataCacheOptions.WICMetadataCacheOnLoad;
                }

                // We have a SafeHandle.
                if (safeFilehandle != null)
                {
                    using (FactoryMaker myFactory = new FactoryMaker())
                    {
                        HRESULT.Check(UnsafeNativeMethods.WICImagingFactory.CreateDecoderFromFileHandle(
                            myFactory.ImagingFactoryPtr,
                            safeFilehandle,
                            ref vendorMicrosoft,
                            metadataFlags,
                            out decoder
                            ));
                    }
                }
                else
                {
                    comStream = BitmapDecoder.GetIStreamFromStream(ref stream);

                    using (FactoryMaker myFactory = new FactoryMaker())
                    {
                        // This does an add-ref on the comStream
                        HRESULT.Check(UnsafeNativeMethods.WICImagingFactory.CreateDecoderFromStream(
                            myFactory.ImagingFactoryPtr,
                            comStream,
                            ref vendorMicrosoft,
                            metadataFlags,
                            out decoder
                            ));
                    }
                }
                Debug.Assert(decoder != IntPtr.Zero);
                decoderHandle = new SafeMILHandle(decoder);
            }
            catch
            {
                #pragma warning disable 6500

                decoderHandle = null;
                throw;

                #pragma warning restore 6500
            }
            finally
            {
                UnsafeNativeMethods.MILUnknown.ReleaseInterface(ref comStream);
            }

            string decoderMimeTypes;
            clsId = GetCLSIDFromDecoder(decoderHandle, out decoderMimeTypes);

            return decoderHandle;
        }

        private static Stream ProcessHttpsFiles(Uri uri, Stream stream)
        {
            Stream bitmapStream = stream;
            // This is the condition where the Bitmap has already been downloaded
            // or the stream is not seekable
            // using async dowload in that case simply return the original stream
            // else you download the stream
            if (bitmapStream == null || !bitmapStream.CanSeek)
            {
                WebRequest request = null;

                request = WpfWebRequestHelper.CreateRequest(uri);

                bitmapStream = WpfWebRequestHelper.GetResponseStream(request);
            }
            return bitmapStream;
        }

        private static Stream ProcessHttpFiles(Uri uri, Stream stream)
        {
            WebRequest request = null;
            Stream bitmapStream =  stream;
            // Download only if this content is not already downloaded or stream is not seekable
            if (bitmapStream == null || !bitmapStream.CanSeek)
            {
                request = WpfWebRequestHelper.CreateRequest(uri);

                // Download only if this content is not already downloaded or stream is not seekable
                bitmapStream = WpfWebRequestHelper.GetResponseStream(request);
            }
            return bitmapStream;
        }

        private static Stream ProcessUncFiles(Uri uri)
        {


            return new System.IO.FileStream(uri.LocalPath, FileMode.Open, FileAccess.Read, FileShare.Read);
        }

        /// Returns the decoder's CLSID
        private static Guid GetCLSIDFromDecoder(SafeMILHandle decoderHandle, out string decoderMimeTypes)
        {
            Guid clsId;

            // Get the decoder info
            SafeMILHandle decoderInfo = new SafeMILHandle();
            HRESULT.Check(UnsafeNativeMethods.WICBitmapDecoder.GetDecoderInfo(
                decoderHandle,
                out decoderInfo
                ));

            // Get CLSID for the decoder
            HRESULT.Check(UnsafeNativeMethods.WICBitmapCodecInfo.GetContainerFormat(decoderInfo, out clsId));

            StringBuilder mimeTypes = null;
            UInt32 length = 0;

            // Find the length of the string needed
            HRESULT.Check(UnsafeNativeMethods.WICBitmapCodecInfo.GetMimeTypes(
                decoderInfo,
                0,
                mimeTypes,
                out length
                ));

            // get the string back
            if (length > 0)
            {
                mimeTypes = new StringBuilder((int)length);

                HRESULT.Check(UnsafeNativeMethods.WICBitmapCodecInfo.GetMimeTypes(
                    decoderInfo,
                    length,
                    mimeTypes,
                    out length
                    ));
            }

            if (mimeTypes != null)
            {
                decoderMimeTypes = mimeTypes.ToString();
            }
            else
            {
                decoderMimeTypes = String.Empty;
            }

            return clsId;
        }

        /// Return a seekable stream if the current one is not seekable
        private static System.IO.Stream GetSeekableStream(System.IO.Stream bitmapStream)
        {
            // MIL codecs require the source stream to be seekable. But if
            // the source stream is an internet stream, it is not seekable.
            // The data is probably not in the stream yet.

            if (bitmapStream.CanSeek)
            {
                return bitmapStream;
            }

            // If the source is not seekable, we have to download the
            // stream into a memory stream before we can decode it.
            // ISSUE-2002/10/03--minliu: later on, if we can make MIL
            // support progressive JPEG etc., we should take out the
            // hack here and pass the network stream (CConectStream)
            // directly to the unmanaged code

            System.IO.MemoryStream memStream =
                new System.IO.MemoryStream();

            byte[] buffer = new byte[1024];
            int read;

            // Read all the bytes and write it to a memory stream
            // This could be unbounded Going forward,
            // we either need to eliminate this code and come up
            // with a cleaner way of doing this, or prevent unbounded
            // memory creation.
            do
            {
                read = bitmapStream.Read(buffer, 0, 1024);
                if (read <= 0)
                {
                    break;
                }
                memStream.Write(buffer, 0, read);
            } while (true);

            // Reset the memory stream pointer back to the begining
            memStream.Seek(0, System.IO.SeekOrigin.Begin);

            // Use the new stream

            return memStream;
        }

        /// Check the cache to see if decoder already exists
        private static BitmapDecoder CheckCache(
            Uri uri,
            out Guid clsId
            )
        {
            clsId = Guid.Empty;
            string mimeTypes;

            if (uri != null)
            {
                WeakReference weakRef = ImagingCache.CheckDecoderCache(uri) as WeakReference;
                if (weakRef != null)
                {
                    BitmapDecoder bitmapDecoder = weakRef.Target as BitmapDecoder;
                    if ((bitmapDecoder != null) && bitmapDecoder.CheckAccess())
                    {
                        lock (bitmapDecoder.SyncObject)
                        {
                            clsId = GetCLSIDFromDecoder(bitmapDecoder.InternalDecoder, out mimeTypes);
                            return bitmapDecoder;
                        }
                    }
                    // Remove from the cache if bitmapDecoder is already been collected
                    if (bitmapDecoder == null)
                    {
                        ImagingCache.RemoveFromDecoderCache(uri);
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// Initialize the codec.
        /// </summary>
        private void Initialize(BitmapDecoder decoder)
        {
            _isBuiltInDecoder = true;

            if (decoder != null)
            {
                SetupFrames(decoder, null);
                //
                // We need to keep the strong reference to the cached decoder for a few reasons:
                //
                //    The application may release the original cached decoder and then keep a
                //    reference to this decoder only, in which case, the cache can be collected.
                //    This will cause a few undesirable results:
                //    1. The application may choose to decode the same URI again in which case
                //       we will not retrieve it from the cache even though we have a copy already
                //       decoded.
                //    2. The original cached decoder holds onto the file stream indirectly which if
                //       collected can cause bad behavior if the entire decoder is not loaded into
                //       memory.
                //
                _cachedDecoder = decoder;
            }
            else if ((_createOptions & BitmapCreateOptions.DelayCreation) == 0 && _cacheOption == BitmapCacheOption.OnLoad)
            {
                SetupFrames(null, null);

                // Its ok to close the stream since the frames are not delay created and caching is immediate
                CloseStream();
            }


            if ((_uri != null) && (decoder == null) && _shouldCacheDecoder)
            {
                // Add this decoder to the decoder cache
                ImagingCache.AddToDecoderCache(
                    (_baseUri == null) ? _uri : new Uri(_baseUri, _uri),
                    new WeakReference(this)
                    );
            }
        }

        /// <summary>
        /// Closes the stream if its non-null
        /// </summary>
        internal void CloseStream()
        {
            if (_uriStream != null)
            {
                _uriStream.Close();
                _uriStream = null;

                // The finalizer closes _uriStream, we already closed it so there's no need to run
                // the finalizer anymore.
                GC.SuppressFinalize(this);
            }
        }

        /// <summary>
        /// Sets up the frames collection.
        /// Also, ensures that the frame collection for this decoder
        /// will contain the same underlying frames as the collection
        /// passed in. This is called by the LateBoundBitmapDecoder
        /// </summary>
        internal void SetupFrames(BitmapDecoder decoder, ReadOnlyCollection<BitmapFrame> frames)
        {
            uint numFrames = 1;

            HRESULT.Check(UnsafeNativeMethods.WICBitmapDecoder.GetFrameCount(_decoderHandle, out numFrames));

            _frames = new List<BitmapFrame>((int)numFrames);

            // initialize the list of frames to null.
            // We'll fill it as it's used (pay for play).
            for (int i = 0; i < (int)numFrames; i++)
            {
                if (i > 0 && _cacheOption != BitmapCacheOption.OnLoad)
                {
                    _createOptions |= BitmapCreateOptions.DelayCreation;
                }

                BitmapFrameDecode bfd = null;

                if ((frames != null) && (frames.Count == (i + 1)))
                {
                    // If we already have a frames collection, get the BitmapFrame from it
                    bfd = frames[i] as BitmapFrameDecode;
                    bfd.UpdateDecoder(this);
                }
                else if (decoder == null)
                {
                    // All frames should be frozen.
                    bfd = new BitmapFrameDecode(
                        i,
                        _createOptions,
                        _cacheOption,
                        this
                        );
                    bfd.Freeze();
                }
                else
                {
                    // if we are creating from an existing cache, use the frames
                    // already stored in that cache
                    // All frames should be frozen.
                    bfd = new BitmapFrameDecode(
                        i,
                        _createOptions,
                        _cacheOption,
                        decoder.Frames[i] as BitmapFrameDecode
                        );
                    bfd.Freeze();
                }

                _frames.Add(bfd);
            }
        }

        /// <summary>
        /// Checks if the decoder is builtin. If not, throw exception
        /// </summary>
        private void EnsureBuiltInDecoder()
        {
            if (!_isBuiltInDecoder)
            {
                throw new NotImplementedException();
            }
        }

        /// <summary>
        /// Note, we must hold onto a reference to the managed stream
        /// as long as we are using the unmanaged stream in the decoder.
        /// This method may create a new managed stream.
        /// </summary>
        /// <param name="bitmapStream"></param>
        /// <returns></returns>
        private static IntPtr GetIStreamFromStream(ref System.IO.Stream bitmapStream)
        {
            IntPtr  comStream = IntPtr.Zero;

            // ensure the stream is seekable
            bool seekable = bitmapStream.CanSeek;

            if (bitmapStream is UnmanagedMemoryStream)
            {
                UnmanagedMemoryStream memoryStream = bitmapStream as UnmanagedMemoryStream;
                IntPtr bufferPtr = IntPtr.Zero;
                int length = 0;

                unsafe
                {
                   bufferPtr = (IntPtr) memoryStream.PositionPointer;
                   length = (int) memoryStream.Length;
                }

                if (bufferPtr != IntPtr.Zero)
                {
                    comStream = StreamAsIStream.IStreamFrom(bufferPtr, length);
                }
            }
            else
            {
                comStream = StreamAsIStream.IStreamFrom(bitmapStream);

                if (comStream == IntPtr.Zero)
                {
                    throw new System.InvalidOperationException(
                        SR.Get(SRID.Image_CantDealWithStream));
                }

                // If the stream is not seekable, we must create a
                // seekable one for the decoder.
                if (!seekable || ((!bitmapStream.CanWrite) && (bitmapStream.Length <= 1048576)))
                {
                    IntPtr memoryStream = StreamAsIStream.IStreamMemoryFrom(comStream);

                    if (memoryStream != IntPtr.Zero)
                    {
                        // we don't need the original stream anymore
                        UnsafeNativeMethods.MILUnknown.ReleaseInterface(ref comStream);
                        bitmapStream = System.IO.Stream.Null;
                        return memoryStream;
                    }
                    else if (!seekable)
                    {
                        throw new System.InvalidOperationException(
                                SR.Get(SRID.Image_CantDealWithStream));
                    }
                }
}

            if (comStream == IntPtr.Zero)
            {
                throw new System.InvalidOperationException(
                SR.Get(SRID.Image_CantDealWithStream));
            }

            return comStream;
        }


        /// Returns whether decoder can be converted to a string
        internal bool CanConvertToString()
        {
            return (_uri != null);
        }

        #endregion

        #region Internal Abstract

        /// "Seals" the object
        internal abstract void SealObject();

        #endregion

        #region Data Members

        /// check to see if implementation is internal
        private bool _isBuiltInDecoder;

        /// Internal Decoder
        private SafeMILHandle _decoderHandle;

        /// flag to see if decoder should be inserted in the cache
        private bool _shouldCacheDecoder = true;

        /// flag to see if decoder should be inserted in the cache
        private bool _isOriginalWritable = false;

        /// If the palette is already cached
        private bool _isPaletteCached;

        /// Palette
        private BitmapPalette _palette = null;

        /// If the ColorContext is already cached
        private bool _isColorContextCached = false;

        /// ColorContexts collection
        internal ReadOnlyCollection<ColorContext> _readOnlycolorContexts;

        /// If the thumbnail is already cached
        private bool _isThumbnailCached;

        /// <summary>
        /// Metadata
        /// </summary>
        private BitmapMetadata _metadata;

        /// If the metadata is already cached
        private bool _isMetadataCached;

        /// Thumbnail
        private BitmapSource _thumbnail = null;

        /// CodecInfo
        private BitmapCodecInfo _codecInfo;

        /// If the preview is already cached
        private bool _isPreviewCached;

        /// Preview
        private BitmapSource _preview = null;

        /// Frames collection
        internal List<BitmapFrame> _frames;

        /// Frames collection
        internal ReadOnlyCollection<BitmapFrame> _readOnlyFrames;

        /// Stream
        internal Stream _stream;

        /// Uri
        internal Uri _uri;

        /// Base Uri, only stored internally. Not used for serialization
        internal Uri _baseUri;

        /// Uri Stream -- this is the stream that was created from the passed in Uri
        internal Stream _uriStream;

        /// CreateOptions
        internal BitmapCreateOptions _createOptions;

        /// CacheOption
        internal BitmapCacheOption _cacheOption;

        /// Event helper for download completed event
        internal UniqueEventHelper _downloadEvent = new UniqueEventHelper();

        /// Event helper for download progress event
        internal UniqueEventHelper<DownloadProgressEventArgs> _progressEvent = new UniqueEventHelper<DownloadProgressEventArgs>();

        /// Event helper for download failed event
        internal UniqueEventHelper<ExceptionEventArgs> _failedEvent = new UniqueEventHelper<ExceptionEventArgs>();

        /// SyncObject
        private object _syncObject = new Object();

        // For UnmanagedMemoryStream we want to make sure that buffer
        // its pointing to is not getting release until decoder is alive
        private UnmanagedMemoryStream _unmanagedMemoryStream;

        private SafeFileHandle _safeFilehandle;

        private BitmapDecoder _cachedDecoder;

        #endregion
    }

    #endregion // BitmapDecoder
}

