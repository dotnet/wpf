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
using MS.Internal.PresentationCore;
using MS.Win32;
using System.Security;
using System.Diagnostics;
using System.Windows.Media;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Windows.Media.Animation;
using System.Windows.Media.Composition;
using SR=MS.Internal.PresentationCore.SR;
using SRID=MS.Internal.PresentationCore.SRID;
using System.Windows.Markup;
using System.Net.Cache;

namespace System.Windows.Media.Imaging
{
    #region BitmapImage

    /// <summary>
    /// BitmapImage provides caching functionality for a BitmapSource.
    /// </summary>
    public sealed partial class BitmapImage : Imaging.BitmapSource, ISupportInitialize, IUriContext
    {
        /// <summary>
        /// BitmapImage constructor
        /// </summary>
        public BitmapImage()
            : base(true) // Use base class virtuals
        {
        }

        /// <summary>
        /// Construct a BitmapImage with the given Uri
        /// </summary>
        /// <param name="uriSource">Uri of the source Bitmap</param>
        public BitmapImage(Uri uriSource)
            : this(uriSource, null)
        {
        }

        /// <summary>
        /// Construct a BitmapImage with the given Uri and RequestCachePolicy
        /// </summary>
        /// <param name="uriSource">Uri of the source Bitmap</param>
        /// <param name="uriCachePolicy">Optional web request cache policy</param>
        public BitmapImage(Uri uriSource, RequestCachePolicy uriCachePolicy)
            : base(true) // Use base class virtuals
        {
            if (uriSource == null)
            {
                throw new ArgumentNullException("uriSource");
            }

            BeginInit();
            UriSource = uriSource;
            UriCachePolicy = uriCachePolicy;
            EndInit();
        }
        
        #region ISupportInitialize

        /// <summary>
        /// Prepare the bitmap to accept initialize paramters.
        /// </summary>
        public void BeginInit()
        {
            WritePreamble();
            _bitmapInit.BeginInit();
        }

        /// <summary>
        /// Prevent the bitmap from accepting any further initialize paramters.
        /// </summary>
        public void EndInit()
        {
            WritePreamble();
            _bitmapInit.EndInit();

            if (UriSource == null && StreamSource == null)
            {
                throw new InvalidOperationException(SR.Get(SRID.Image_NeitherArgument, "UriSource", "StreamSource"));
            }

            // If the Uri is relative, use delay creation as the BaseUri could be set at a later point
            if (UriSource != null && !UriSource.IsAbsoluteUri && CacheOption != BitmapCacheOption.OnLoad)
            {
                DelayCreation = true;
            }

            if (!DelayCreation && !CreationCompleted)
                FinalizeCreation();
        }

        #endregion

        #region IUriContext

        /// <summary>
        /// Provides the base uri of the current context.
        /// </summary>
        public Uri BaseUri
        {
            get
            {
                ReadPreamble();
                return _baseUri;
            }
            set
            {
                WritePreamble();
                if (!CreationCompleted && _baseUri != value)
                {
                    _baseUri = value;
                    WritePostscript();
                }
            }
        }

        #endregion

        #region Properties

        /// <summary>
        /// Returns if the BitmapFrame is downloading content
        /// </summary>
        public override bool IsDownloading
        {
            get
            {
                ReadPreamble();
                return _isDownloading;
            }
        }

        /// <summary>
        /// Get the Metadata of the bitmap
        /// </summary>
        public override ImageMetadata Metadata
        {
            get
            {
                throw new System.NotSupportedException(SR.Get(SRID.Image_MetadataNotSupported));
            }
        }


        #endregion

        #region ToString

        /// <summary>
        /// Can serialze "this" to a string
        /// </summary>
        internal override bool CanSerializeToString()
        {
            ReadPreamble();

            return (
                // UriSource not null
                UriSource != null &&

                // But rest are defaults
                StreamSource == null &&
                SourceRect.IsEmpty &&
                DecodePixelWidth == 0 &&
                DecodePixelHeight == 0 &&
                Rotation == Rotation.Rotate0 &&
                CreateOptions == BitmapCreateOptions.None &&
                CacheOption == BitmapCacheOption.Default &&
                UriCachePolicy == null
               );
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

            if (UriSource != null)
            {
                if (_baseUri != null)
                {
                    return BindUriHelper.UriToString(new Uri(_baseUri, UriSource));
                }
                else
                {
                    return BindUriHelper.UriToString(UriSource);
                }
            }

            return base.ConvertToString(format, provider);
        }

        #endregion

        private void ClonePrequel(BitmapImage otherBitmapImage)
        {
            BeginInit();
            _isDownloading = otherBitmapImage._isDownloading;
            _decoder = otherBitmapImage._decoder;
            _baseUri = otherBitmapImage._baseUri;
        }

        private void ClonePostscript(BitmapImage otherBitmapImage)
        {
            //
            // If the previous BitmapImage is downloading, we need to listen for those
            // events as our state will change based on when the download is done.
            //
            // NOTE -- This cannot happen in ClonePrequel as CopyCommon on BitmapSource
            //         is called after ClonePrequel which clones the event handlers,
            //         which will cause a reference onto itself and cause a Stack Overflow.
            //
            if (_isDownloading)
            {
                Debug.Assert(_decoder != null);
                _decoder.DownloadProgress += OnDownloadProgress;
                _decoder.DownloadCompleted += OnDownloadCompleted;
                _decoder.DownloadFailed += OnDownloadFailed;
            }

            EndInit();
        }

        /// Check the cache for an existing BitmapImage
        private BitmapImage CheckCache(Uri uri)
        {
            if (uri != null)
            {
                WeakReference bitmapWeakReference = ImagingCache.CheckImageCache(uri) as WeakReference;

                if (bitmapWeakReference != null)
                {
                    BitmapImage bitmapImage = (bitmapWeakReference.Target as BitmapImage);

                    // See if this bitmap was already in the image cache.
                    if (bitmapImage != null)
                    {
                        return bitmapImage;
                    }
                    else
                    {
                        ImagingCache.RemoveFromImageCache(uri);
                    }
                }
            }

            return null;
        }

        /// Insert BitmapImage in cache
        private void InsertInCache(Uri uri)
        {
            if (uri != null)
            {
                //
                // Keeping the image bits alive in the cache can bloat working set by 40-50mb in
                // common scenarios.  So, keep weak refereances in the cache so that we don't keep
                // bits around indefinitely.
                //
                // We currently get cache benefits if:
                //
                //    1. We have multiple references to the same bitmap on the same page
                //    2. A bitmap is referenced before a GC and the object is still alive
                //    3. The user explicit holds onto the bitmap object and keeps the cache alive
                //
                // Note that this cache is the in-memory cache for bitmap loads from disk.  If we miss,
                // we will likely hit the disk cache, so cache misses can be reasonable.  Downloads off the
                // network are cached to disk at another level and are unaffected by the weak references.
                //

                ImagingCache.AddToImageCache(uri, new WeakReference(this));
            }
        }

        ///
        /// Create the unmanaged resources
        ///
        internal override void FinalizeCreation()
        {
            _bitmapInit.EnsureInitializedComplete();
            Uri uri = UriSource;
            if (_baseUri != null)
                uri = new Uri(_baseUri, UriSource);

            if ((CreateOptions & BitmapCreateOptions.IgnoreImageCache) != 0)
            {
                ImagingCache.RemoveFromImageCache(uri);
            }

            BitmapImage bitmapImage = CheckCache(uri);

            if (bitmapImage != null &&
                bitmapImage.CheckAccess() &&
                bitmapImage.SourceRect.Equals(SourceRect) &&
                bitmapImage.DecodePixelWidth == DecodePixelWidth &&
                bitmapImage.DecodePixelHeight == DecodePixelHeight &&
                bitmapImage.Rotation == Rotation &&
                (bitmapImage.CreateOptions & BitmapCreateOptions.IgnoreColorProfile) ==
                (CreateOptions & BitmapCreateOptions.IgnoreColorProfile)
               )
            {
                _syncObject = bitmapImage.SyncObject;
                lock (_syncObject)
                {
                    WicSourceHandle = bitmapImage.WicSourceHandle;
                    IsSourceCached = bitmapImage.IsSourceCached;
                    _convertedDUCEPtr = bitmapImage._convertedDUCEPtr;

                    //
                    // We nee d to keep the strong reference to the cached image for a few reasons:
                    //
                    //    The application may release the original cached image and then keep a
                    //    reference to this image only, in which case, the cache can be collected.
                    //    This will cause a few undesirable results:
                    //    1. The application may choose to decode the same URI again in which case
                    //       we will not retrieve it from the cache even though we have a copy already
                    //       decoded.
                    //    2. The original cached image holds onto the file stream indirectly which if
                    //       collected can cause bad behavior if the entire image is not loaded into
                    //       memory.
                    //
                    _cachedBitmapImage = bitmapImage;
                }
                UpdateCachedSettings();
                return;
            }

            BitmapDecoder decoder = null;
            if (_decoder == null)
            {
                // Note: We do not want to insert in the cache if there is a chance that
                //       the decode pixel width/height may cause the decoder LOD to change
                decoder = BitmapDecoder.CreateFromUriOrStream(
                    _baseUri,
                    UriSource,
                    StreamSource,
                    CreateOptions & ~BitmapCreateOptions.DelayCreation,
                    BitmapCacheOption.None, // do not cache the frames since we will do that here
                    _uriCachePolicy,
                    false
                    );

                if (decoder.IsDownloading)
                {
                    _isDownloading = true;
                    _decoder = decoder;
                    decoder.DownloadProgress += OnDownloadProgress;
                    decoder.DownloadCompleted += OnDownloadCompleted;
                    decoder.DownloadFailed += OnDownloadFailed;
                }
                else
                {
                    Debug.Assert(decoder.SyncObject != null);
                }
            }
            else
            {
                // We already had a decoder, meaning we were downloading
                Debug.Assert(!_decoder.IsDownloading);
                decoder = _decoder;
                _decoder = null;
            }

            if (decoder.Frames.Count == 0)
            {
                throw new System.ArgumentException(SR.Get(SRID.Image_NoDecodeFrames));
            }

            BitmapFrame frame = decoder.Frames[0];
            BitmapSource source = frame;

            Int32Rect sourceRect = SourceRect;

            if (sourceRect.X == 0 && sourceRect.Y == 0 &&
                sourceRect.Width == source.PixelWidth &&
                sourceRect.Height == source.PixelHeight)
            {
                sourceRect = Int32Rect.Empty;
            }

            if (!sourceRect.IsEmpty)
            {
                CroppedBitmap croppedSource = new CroppedBitmap();
                croppedSource.BeginInit();
                croppedSource.Source = source;
                croppedSource.SourceRect = sourceRect;
                croppedSource.EndInit();

                source = croppedSource;
                if (_isDownloading)
                {
                    // Unregister the download events because this is a dummy image. See comment below.
                    source.UnregisterDownloadEventSource();
                }
            }

            int finalWidth = DecodePixelWidth;
            int finalHeight = DecodePixelHeight;

            if (finalWidth == 0 && finalHeight == 0)
            {
                finalWidth = source.PixelWidth;
                finalHeight = source.PixelHeight;
            }
            else if (finalWidth == 0)
            {
                finalWidth = (source.PixelWidth * finalHeight) / source.PixelHeight;
            }
            else if (finalHeight == 0)
            {
                finalHeight = (source.PixelHeight * finalWidth) / source.PixelWidth;
            }

            if (finalWidth != source.PixelWidth || finalHeight != source.PixelHeight ||
                Rotation != Rotation.Rotate0)
            {
                TransformedBitmap transformedSource = new TransformedBitmap();
                transformedSource.BeginInit();
                transformedSource.Source = source;

                TransformGroup transformGroup = new TransformGroup();

                if (finalWidth != source.PixelWidth || finalHeight != source.PixelHeight)
                {
                    int oldWidth = source.PixelWidth;
                    int oldHeight = source.PixelHeight;

                    Debug.Assert(oldWidth > 0 && oldHeight > 0);

                    transformGroup.Children.Add(
                        new ScaleTransform(
                            (1.0*finalWidth)/ oldWidth,
                            (1.0*finalHeight)/oldHeight));
                }

                if (Rotation != Rotation.Rotate0)
                {
                    double rotation = 0.0;

                    switch (Rotation)
                    {
                        case Rotation.Rotate0:
                            rotation = 0.0;
                            break;
                        case Rotation.Rotate90:
                            rotation = 90.0;
                            break;
                        case Rotation.Rotate180:
                            rotation = 180.0;
                            break;
                        case Rotation.Rotate270:
                            rotation = 270.0;
                            break;
                        default:
                            Debug.Assert(false);
                            break;
                    }

                    transformGroup.Children.Add(new RotateTransform(rotation));
                }

                transformedSource.Transform = transformGroup;

                transformedSource.EndInit();

                source = transformedSource;
                if (_isDownloading)
                {
                    //
                    // If we're currently downloading, then the BitmapFrameDecode isn't actually
                    // the image, it's just a 1x1 placeholder. The chain we're currently building
                    // will be replaced with another chain once download completes, so there's no
                    // need to have this chain handle DownloadCompleted.
                    //
                    // Having this chain handle DownloadCompleted is actually a bad thing. Because
                    // the dummy is just 1x1, the TransformedBitmap we're building here will have
                    // a large scaling factor (to scale the image from 1x1 up to whatever
                    // DecodePixelWidth/Height specifies). When the TransformedBitmap receives
                    // DownloadCompleted from the BFD, it will call into WIC to create a new
                    // bitmap scaler using the same large scaling factor, which can produce a huge
                    // bitmap (since the BFD is now no longer 1x1). This problem is made worse if
                    // this BitmapImage has BitmapCacheOption.OnLoad, since that will put a
                    // CachedBitmap after the TransformedBitmap. When DownloadCompleted propagates
                    // from the TransformedBitmap down to the CachedBitmap, the CachedBitmap will
                    // call CreateBitmapFromSource using the TransformedBitmap, which calls
                    // CopyPixels on the huge TransformedBitmap. We want to avoid chewing up the
                    // CPU and the memory, so we unregister the download event handlers here.
                    //
                    source.UnregisterDownloadEventSource();
                }
            }

            //
            // If the original image has a color profile and IgnoreColorProfile is not one of the create options,
            // apply the profile so bits are color-corrected.
            //
            if ((CreateOptions & BitmapCreateOptions.IgnoreColorProfile) == 0 &&
                frame.ColorContexts != null &&
                frame.ColorContexts[0] != null &&
                frame.ColorContexts[0].IsValid && 
                source.Format.Format != PixelFormatEnum.Extended
                )
            {
                // NOTE: Never do this for a non-MIL pixel format, because the format converter has
                // special knowledge to deal with the profile

                PixelFormat duceFormat = BitmapSource.GetClosestDUCEFormat(source.Format, source.Palette);
                bool changeFormat = (source.Format != duceFormat);
                ColorContext destinationColorContext;

                // We need to make sure, we can actually create the ColorContext for the destination duceFormat
                // If the duceFormat is gray or scRGB, the following is not supported, so we cannot
                // create the ColorConvertedBitmap
                try
                {
                    destinationColorContext= new ColorContext(duceFormat);
                }
                catch (NotSupportedException)
                {
                    destinationColorContext = null;
                }

                if (destinationColorContext != null)
                {
                    bool conversionSuccess = false;
                    bool badColorContext = false;

                    // First try if the color converter can handle the source format directly
                    // Its possible that the color converter does not support certain pixelformats, so put a try/catch here.
                    try
                    {
                        ColorConvertedBitmap colorConvertedBitmap = new ColorConvertedBitmap(
                            source,
                            frame.ColorContexts[0],
                            destinationColorContext,
                            duceFormat
                            );

                        source = colorConvertedBitmap;
                        if (_isDownloading)
                        {
                            // Unregister the download events because this is a dummy image. See comment above.
                            source.UnregisterDownloadEventSource();
                        }
                        conversionSuccess = true;
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

                    if (!conversionSuccess && !badColorContext && changeFormat)
                    {   // If the conversion failed, we first use
                        // a FormatConvertedBitmap, and then Color Convert that one...
                        FormatConvertedBitmap formatConvertedBitmap = new FormatConvertedBitmap(source, duceFormat, source.Palette, 0.0);

                        ColorConvertedBitmap colorConvertedBitmap = new ColorConvertedBitmap(
                            formatConvertedBitmap,
                            frame.ColorContexts[0],
                            destinationColorContext,
                            duceFormat
                            );

                        source = colorConvertedBitmap;
                        if (_isDownloading)
                        {
                            // Unregister the download events because this is a dummy image. See comment above.
                            source.UnregisterDownloadEventSource();
                        }
                    }
                }
            }

            if (CacheOption != BitmapCacheOption.None)
            {
                try
                {
                    // The bitmaps bits could be corrupt, and this will cause an exception if the CachedBitmap forces a decode.
                    CachedBitmap cachedSource = new CachedBitmap(source, CreateOptions & ~BitmapCreateOptions.DelayCreation, CacheOption);
                    source = cachedSource;
                    if (_isDownloading)
                    {
                        // Unregister the download events because this is a dummy image. See comment above.
                        source.UnregisterDownloadEventSource();
                    }
                }
                catch (Exception e)
                {
                    RecoverFromDecodeFailure(e);
                    CreationCompleted = true; // we're bailing out because the decode failed
                    return;
                }
            }

            // If CacheOption == OnLoad, no need to keep the stream around
            if (decoder != null && CacheOption == BitmapCacheOption.OnLoad)
            {
                decoder.CloseStream();
            }
            else if (CacheOption != BitmapCacheOption.OnLoad)
            {
                //ensure that we don't GC the source
                _finalSource = source;
            }

            WicSourceHandle = source.WicSourceHandle;
            IsSourceCached = source.IsSourceCached;

            CreationCompleted = true;
            UpdateCachedSettings();

            // Only insert in the imaging cache if download is complete
            if (!IsDownloading)
            {
                InsertInCache(uri);
            }
        }

        private void UriCachePolicyPropertyChangedHook(DependencyPropertyChangedEventArgs e)
        {
            if (!e.IsASubPropertyChange)
            {
                _uriCachePolicy = e.NewValue as RequestCachePolicy;
            }
        }

        private void UriSourcePropertyChangedHook(DependencyPropertyChangedEventArgs e)
        {
            if (!e.IsASubPropertyChange)
            {
                _uriSource = e.NewValue as Uri;
            }
        }

        private void StreamSourcePropertyChangedHook(DependencyPropertyChangedEventArgs e)
        {
            if (!e.IsASubPropertyChange)
            {
                _streamSource = e.NewValue as Stream;
            }
        }

        private void DecodePixelWidthPropertyChangedHook(DependencyPropertyChangedEventArgs e)
        {
            if (!e.IsASubPropertyChange)
            {
                _decodePixelWidth = (Int32)e.NewValue;
            }
        }

        private void DecodePixelHeightPropertyChangedHook(DependencyPropertyChangedEventArgs e)
        {
            if (!e.IsASubPropertyChange)
            {
                _decodePixelHeight = (Int32)e.NewValue;
            }
        }

        private void RotationPropertyChangedHook(DependencyPropertyChangedEventArgs e)
        {
            if (!e.IsASubPropertyChange)
            {
                _rotation = (Rotation)e.NewValue;
            }
        }

        private void SourceRectPropertyChangedHook(DependencyPropertyChangedEventArgs e)
        {
            if (!e.IsASubPropertyChange)
            {
                _sourceRect = (Int32Rect)e.NewValue;
            }
        }

        private void CreateOptionsPropertyChangedHook(DependencyPropertyChangedEventArgs e)
        {
            BitmapCreateOptions options = (BitmapCreateOptions)e.NewValue;
            _createOptions = options;
            DelayCreation = ((options & BitmapCreateOptions.DelayCreation) != 0);
        }

        private void CacheOptionPropertyChangedHook(DependencyPropertyChangedEventArgs e)
        {
            if (!e.IsASubPropertyChange)
            {
                _cacheOption = (BitmapCacheOption)e.NewValue;
            }
        }

        /// <summary>
        ///     Coerce UriCachePolicy
        /// </summary>
        private static object CoerceUriCachePolicy(DependencyObject d, object value)
        {
            BitmapImage image = (BitmapImage)d;
            if (!image._bitmapInit.IsInInit)
            {
                return image._uriCachePolicy;
            }
            else
            {
                return value;
            }
        }

        /// <summary>
        ///     Coerce UriSource
        /// </summary>
        private static object CoerceUriSource(DependencyObject d, object value)
        {
            BitmapImage image = (BitmapImage)d;
            if (!image._bitmapInit.IsInInit)
            {
                return image._uriSource;
            }
            else
            {
                return value;
            }
        }

        /// <summary>
        ///     Coerce StreamSource
        /// </summary>
        private static object CoerceStreamSource(DependencyObject d, object value)
        {
            BitmapImage image = (BitmapImage)d;
            if (!image._bitmapInit.IsInInit)
            {
                return image._streamSource;
            }
            else
            {
                return value;
            }
        }

        /// <summary>
        ///     Coerce DecodePixelWidth
        /// </summary>
        private static object CoerceDecodePixelWidth(DependencyObject d, object value)
        {
            BitmapImage image = (BitmapImage)d;
            if (!image._bitmapInit.IsInInit)
            {
                return image._decodePixelWidth;
            }
            else
            {
                return value;
            }
        }

        /// <summary>
        ///     Coerce DecodePixelHeight
        /// </summary>
        private static object CoerceDecodePixelHeight(DependencyObject d, object value)
        {
            BitmapImage image = (BitmapImage)d;
            if (!image._bitmapInit.IsInInit)
            {
                return image._decodePixelHeight;
            }
            else
            {
                return value;
            }
        }

        /// <summary>
        ///     Coerce Rotation
        /// </summary>
        private static object CoerceRotation(DependencyObject d, object value)
        {
            BitmapImage image = (BitmapImage)d;
            if (!image._bitmapInit.IsInInit)
            {
                return image._rotation;
            }
            else
            {
                return value;
            }
        }

        /// <summary>
        ///     Coerce SourceRect
        /// </summary>
        private static object CoerceSourceRect(DependencyObject d, object value)
        {
            BitmapImage image = (BitmapImage)d;
            if (!image._bitmapInit.IsInInit)
            {
                return image._sourceRect;
            }
            else
            {
                return value;
            }
        }

        /// <summary>
        ///     Coerce CreateOptions
        /// </summary>
        private static object CoerceCreateOptions(DependencyObject d, object value)
        {
            BitmapImage image = (BitmapImage)d;
            if (!image._bitmapInit.IsInInit)
            {
                return image._createOptions;
            }
            else
            {
                return value;
            }
        }

        /// <summary>
        ///     Coerce CacheOption
        /// </summary>
        private static object CoerceCacheOption(DependencyObject d, object value)
        {
            BitmapImage image = (BitmapImage)d;
            if (!image._bitmapInit.IsInInit)
            {
                return image._cacheOption;
            }
            else
            {
                return value;
            }
        }

        /// Called when decoder finishes download
        private void OnDownloadCompleted(object sender, EventArgs e)
        {
            _isDownloading = false;

            //
            // Unhook the decoders download events
            //
            _decoder.DownloadProgress -= OnDownloadProgress;
            _decoder.DownloadCompleted -= OnDownloadCompleted;
            _decoder.DownloadFailed -= OnDownloadFailed;
            
            if ((CreateOptions & BitmapCreateOptions.DelayCreation) != 0)
            {
                DelayCreation = true;
            }
            else
            {
                FinalizeCreation();

                // Trigger a update of the UCE resource
                _needsUpdate = true;
                RegisterForAsyncUpdateResource();
                FireChanged();
            }

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
            _isDownloading = false;

            //
            // Unhook the decoders download events
            //
            _decoder.DownloadProgress -= OnDownloadProgress;
            _decoder.DownloadCompleted -= OnDownloadCompleted;
            _decoder.DownloadFailed -= OnDownloadFailed;
            
            _failedEvent.InvokeEvents(this, e);
        }

        #region Data Members

        // Base Uri from IUriContext
        private Uri _baseUri;

        /// Is downloading content
        private bool _isDownloading;

        /// Bitmap Decoder
        private BitmapDecoder _decoder;

        private RequestCachePolicy _uriCachePolicy;

        private Uri _uriSource;

        private Stream _streamSource;

        private Int32 _decodePixelWidth;

        private Int32 _decodePixelHeight;

        private Rotation _rotation;

        private Int32Rect _sourceRect;

        private BitmapCreateOptions _createOptions;

        private BitmapCacheOption _cacheOption;

        // used to ensure the source isn't GC'd
        private BitmapSource _finalSource;

        private BitmapImage _cachedBitmapImage;

        #endregion
    }

    #endregion // BitmapImage
}
