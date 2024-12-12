// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.ObjectModel;
using MS.Internal;
using System.Windows.Threading;
using System.Runtime.InteropServices;
using MS.Win32.PresentationCore;
using Windows.Win32.Foundation;

#pragma warning disable 1634, 1691  // suppressing PreSharp warnings

namespace System.Windows.Media.Imaging
{
    #region PROPBAG2

    [StructLayout(LayoutKind.Sequential, Pack=1)]
    internal struct PROPBAG2
    {
        internal UInt32 dwType;
        internal ushort vt;
        internal ushort cfType;
        internal IntPtr dwHint;

        internal IntPtr pstrName; //this is string array

        internal Guid clsid;

        internal void Init(String name)
        {
            pstrName = Marshal.StringToCoTaskMemUni(name);
        }

        internal void Clear()
        {
            Marshal.FreeCoTaskMem(pstrName);
            pstrName = IntPtr.Zero;
        }
    }

    #endregion

    #region BitmapEncoder

    /// <summary>
    /// BitmapEncoder collects a set of frames (BitmapSource's) with their associated
    /// thumbnails and saves them to a specified stream.  In addition
    /// to frame-specific thumbnails, there can also be an bitmap-wide
    /// (global) thumbnail, if the codec supports it.
    /// </summary>
    public abstract class BitmapEncoder : DispatcherObject
    {
        #region Constructors

        /// <summary>
        /// Constructor.
        /// </summary>
        protected BitmapEncoder()
        {
        }

        /// <summary>
        /// Internal Constructor.
        /// </summary>
        internal BitmapEncoder(bool isBuiltIn)
        {
            _isBuiltIn = isBuiltIn;
        }

        /// <summary>
        /// Creates a BitmapEncoder from a container format Guid
        /// </summary>
        /// <param name="containerFormat">Container format for the codec</param>
        public static BitmapEncoder Create(Guid containerFormat)
        {
            if (containerFormat == Guid.Empty)
            {
                throw new ArgumentException(
                    SR.Format(SR.Image_GuidEmpty, "containerFormat"),
                    "containerFormat"
                    );
            }
            else if (containerFormat == MILGuidData.GUID_ContainerFormatBmp)
            {
                return new BmpBitmapEncoder();
            }
            else if (containerFormat == MILGuidData.GUID_ContainerFormatGif)
            {
                return new GifBitmapEncoder();
            }
            else if (containerFormat == MILGuidData.GUID_ContainerFormatJpeg)
            {
                return new JpegBitmapEncoder();
            }
            else if (containerFormat == MILGuidData.GUID_ContainerFormatPng)
            {
                return new PngBitmapEncoder();
            }
            else if (containerFormat == MILGuidData.GUID_ContainerFormatTiff)
            {
                return new TiffBitmapEncoder();
            }
            else if (containerFormat == MILGuidData.GUID_ContainerFormatWmp)
            {
                return new WmpBitmapEncoder();
            }
            else
            {
                return new UnknownBitmapEncoder(containerFormat);
            }
        }

        #endregion

        #region Public Properties

        /// <summary>
        /// Set or get the bitmap's color profile.
        /// </summary>
        public virtual ReadOnlyCollection<ColorContext> ColorContexts
        {
            get
            {
                VerifyAccess();
                EnsureBuiltIn();
                return _readOnlycolorContexts;
            }
            set
            {
                VerifyAccess();
                EnsureBuiltIn();

                ArgumentNullException.ThrowIfNull(value);

                if (!_supportsColorContext)
                {
                    throw new InvalidOperationException(SR.Image_EncoderNoColorContext);
                }

                _readOnlycolorContexts = value;
            }
        }

        /// <summary>
        /// Set or get the bitmap's global embedded thumbnail.
        /// </summary>
        public virtual BitmapSource Thumbnail
        {
            get
            {
                VerifyAccess();
                EnsureBuiltIn();
                return _thumbnail;
            }
            set
            {
                VerifyAccess();
                EnsureBuiltIn();

                ArgumentNullException.ThrowIfNull(value);

                if (!_supportsGlobalThumbnail)
                {
                    throw new InvalidOperationException(SR.Image_EncoderNoGlobalThumbnail);
                }

                _thumbnail = value;
            }
        }

        /// <summary>
        /// Set or get the bitmap's global embedded metadata.
        /// </summary>
        public virtual BitmapMetadata Metadata
        {
            get
            {
                VerifyAccess();
                EnsureBuiltIn();
                EnsureMetadata(true);

                return _metadata;
            }
            set
            {
                VerifyAccess();
                EnsureBuiltIn();

                ArgumentNullException.ThrowIfNull(value);

                if (value.GuidFormat != ContainerFormat)
                {
                    throw new InvalidOperationException(SR.Image_MetadataNotCompatible);
                }

                if (!_supportsGlobalMetadata)
                {
                    throw new InvalidOperationException(SR.Image_EncoderNoGlobalMetadata);
                }

                _metadata = value;
            }
        }

        /// <summary>
        /// Set or get the bitmap's global preview
        /// </summary>
        public virtual BitmapSource Preview
        {
            get
            {
                VerifyAccess();
                EnsureBuiltIn();
                return _preview;
            }
            set
            {
                VerifyAccess();
                EnsureBuiltIn();

                ArgumentNullException.ThrowIfNull(value);

                if (!_supportsPreview)
                {
                    throw new InvalidOperationException(SR.Image_EncoderNoPreview);
                }

                _preview = value;
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
                EnsureBuiltIn();
                EnsureUnmanagedEncoder();

                // There should always be a codec info.
                if (_codecInfo == null)
                {
                    SafeMILHandle /* IWICBitmapEncoderInfo */ codecInfoHandle = new SafeMILHandle();

                    UnsafeNativeMethods.WICBitmapEncoder.GetEncoderInfo(
                        _encoderHandle,
                        out codecInfoHandle).ThrowOnFailureExtended();

                    _codecInfo = new BitmapCodecInfoInternal(codecInfoHandle);
                }

                return _codecInfo;
            }
        }

        /// <summary>
        /// Provides access to this bitmap's palette
        /// </summary>
        public virtual BitmapPalette Palette
        {
            get
            {
                VerifyAccess();
                EnsureBuiltIn();
                return _palette;
            }
            set
            {
                VerifyAccess();
                EnsureBuiltIn();

                ArgumentNullException.ThrowIfNull(value);

                _palette = value;
            }
        }

        /// <summary>
        /// Access to the individual frames.
        /// </summary>
        public virtual IList<BitmapFrame> Frames
        {
            get
            {
                VerifyAccess();
                EnsureBuiltIn();
                if (_frames == null)
                {
                    _frames = new List<BitmapFrame>(0);
                }

                return _frames;
            }
            set
            {
                VerifyAccess();
                EnsureBuiltIn();

                ArgumentNullException.ThrowIfNull(value);

                _frames = value;
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Save (encode) the bitmap to the specified stream.
        /// </summary>
        /// <param name="stream">Stream to save into</param>
        public virtual void Save(System.IO.Stream stream)
        {
            VerifyAccess();
            EnsureBuiltIn();
            EnsureUnmanagedEncoder();

            // No-op to get rid of build error
            if (_encodeState == EncodeState.None)
            {
            }

            if (_hasSaved)
            {
                throw new InvalidOperationException(SR.Image_OnlyOneSave);
            }

            if (_frames == null)
            {
                throw new System.NotSupportedException(SR.Format(SR.Image_NoFrames, null));
            }

            int count = _frames.Count;
            if (count <= 0)
            {
                throw new System.NotSupportedException(SR.Format(SR.Image_NoFrames, null));
            }

            IntPtr comStream = IntPtr.Zero;
            SafeMILHandle encoderHandle = _encoderHandle;

            try
            {
                comStream = StreamAsIStream.IStreamFrom(stream);

                // does this addref the stream?
                UnsafeNativeMethods.WICBitmapEncoder.Initialize(
                    encoderHandle,
                    comStream,
                    WICBitmapEncodeCacheOption.WICBitmapEncodeNoCache).ThrowOnFailureExtended();

                // Helpful for debugging stress and remote dumps
                _encodeState = EncodeState.EncoderInitialized;

                // Save global thumbnail if any.
                if (_thumbnail != null)
                {
                    Debug.Assert(_supportsGlobalThumbnail);
                    SafeMILHandle thumbnailBitmapSource = _thumbnail.WicSourceHandle;

                    lock (_thumbnail.SyncObject)
                    {
                        UnsafeNativeMethods.WICBitmapEncoder.SetThumbnail(
                            encoderHandle,
                            thumbnailBitmapSource).ThrowOnFailureExtended();

                        // Helpful for debugging stress and remote dumps
                        _encodeState = EncodeState.EncoderThumbnailSet;
                    }
                }

                // Save global palette if any.
                if (_palette != null && _palette.Colors.Count > 0)
                {
                    SafeMILHandle paletteHandle = _palette.InternalPalette;

                    UnsafeNativeMethods.WICBitmapEncoder.SetPalette(
                        encoderHandle,
                        paletteHandle).ThrowOnFailureExtended();

                    // Helpful for debugging stress and remote dumps
                    _encodeState = EncodeState.EncoderPaletteSet;
                }

                // Save global metadata if any.
                if (_metadata != null && _metadata.GuidFormat == ContainerFormat)
                {
                    Debug.Assert(_supportsGlobalMetadata);

                    EnsureMetadata(false);

                    if (_metadata.InternalMetadataHandle != _metadataHandle)
                    {
                        PROPVARIANT propVar = new PROPVARIANT();

                        try
                        {
                            propVar.Init(_metadata);

                            lock (_metadata.SyncObject)
                            {
                                UnsafeNativeMethods.WICMetadataQueryWriter.SetMetadataByName(
                                    _metadataHandle,
                                    "/",
                                    ref propVar).ThrowOnFailureExtended();
                            }
                        }
                        finally
                        {
                            propVar.Clear();
                        }
                    }
                }

                for (int i = 0; i < count; i++)
                {
                    SafeMILHandle frameEncodeHandle = new SafeMILHandle();
                    SafeMILHandle encoderOptions = new SafeMILHandle();
                    UnsafeNativeMethods.WICBitmapEncoder.CreateNewFrame(
                        encoderHandle,
                        out frameEncodeHandle,
                        out encoderOptions).ThrowOnFailureExtended();

                    // Helpful for debugging stress and remote dumps
                    _encodeState = EncodeState.EncoderCreatedNewFrame;
                    _frameHandles.Add(frameEncodeHandle);

                    SaveFrame(frameEncodeHandle, encoderOptions, _frames[i]);

                    // If multiple frames are not supported, break out
                    if (!_supportsMultipleFrames)
                    {
                        break;
                    }
                }

                // Now let the encoder know we are done encoding the file.
                UnsafeNativeMethods.WICBitmapEncoder.Commit(encoderHandle).ThrowOnFailureExtended();

                // Helpful for debugging stress and remote dumps
                _encodeState = EncodeState.EncoderCommitted;
            }
            finally
            {
                UnsafeNativeMethods.MILUnknown.ReleaseInterface(ref comStream);
            }

            _hasSaved = true;
        }

        #endregion

        #region Internal Properties / Methods

        /// <summary>
        /// Returns the container format for this encoder
        /// </summary>
        internal virtual Guid ContainerFormat
        {
            get
            {
                return Guid.Empty;
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
        /// Setups the encoder and other properties before encoding each frame
        /// </summary>
        internal virtual void SetupFrame(SafeMILHandle frameEncodeHandle, SafeMILHandle encoderOptions)
        {
            throw new NotImplementedException();
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Checks to see if encoder is built in.
        /// </summary>
        private void EnsureBuiltIn()
        {
            if (!_isBuiltIn)
            {
                throw new NotImplementedException();
            }
        }

        /// <summary>
        /// Checks to see if encoder has built-in metadata.
        /// </summary>
        private void EnsureMetadata(bool createBitmapMetadata)
        {
            if (!_supportsGlobalMetadata)
            {
                return;
            }

            if (_metadataHandle == null)
            {
                SafeMILHandle /* IWICMetadataQueryWriter */ metadataHandle = new SafeMILHandle();

                HRESULT result = UnsafeNativeMethods.WICBitmapEncoder.GetMetadataQueryWriter(
                    _encoderHandle,
                    out metadataHandle
                    );

                if (result == HRESULT.WINCODEC_ERR_UNSUPPORTEDOPERATION)
                {
                    _supportsGlobalMetadata = false;
                    return;
                }

                result.ThrowOnFailureExtended();
                _metadataHandle = metadataHandle;
            }

            if (createBitmapMetadata &&
                _metadata == null &&
                _metadataHandle != null)
            {
                _metadata = new BitmapMetadata(_metadataHandle, false, IsMetadataFixedSize, _metadataHandle);
            }
        }

        /// <summary>
        /// Creates the unmanaged encoder object
        /// </summary>
        private void EnsureUnmanagedEncoder()
        {
            if (_encoderHandle == null)
            {
                using (FactoryMaker myFactory = new FactoryMaker())
                {
                    SafeMILHandle encoderHandle = null;

                    Guid vendorMicrosoft = new Guid(MILGuidData.GUID_VendorMicrosoft);
                    Guid containerFormat = ContainerFormat;

                    UnsafeNativeMethods.WICImagingFactory.CreateEncoder(
                        myFactory.ImagingFactoryPtr,
                        ref containerFormat,
                        ref vendorMicrosoft,
                        out encoderHandle).ThrowOnFailureExtended();

                    _encoderHandle = encoderHandle;
                }
            }
        }

        /// <summary>
        /// Save the frame
        /// </summary>
        private void SaveFrame(SafeMILHandle frameEncodeHandle, SafeMILHandle encoderOptions, BitmapFrame frame)
        {
            SetupFrame(frameEncodeHandle, encoderOptions);

            // Helpful for debugging stress and remote dumps
            _encodeState = EncodeState.FrameEncodeInitialized;

            // Set the size
            UnsafeNativeMethods.WICBitmapFrameEncode.SetSize(
                frameEncodeHandle,
                frame.PixelWidth,
                frame.PixelHeight).ThrowOnFailureExtended();

            // Helpful for debugging stress and remote dumps
            _encodeState = EncodeState.FrameEncodeSizeSet;

            // Set the resolution
            double dpiX = frame.DpiX;
            double dpiY = frame.DpiY;

            if (dpiX <= 0)
            {
                dpiX = 96;
            }
            if (dpiY <= 0)
            {
                dpiY = 96;
            }

            UnsafeNativeMethods.WICBitmapFrameEncode.SetResolution(
                frameEncodeHandle,
                dpiX,
                dpiY).ThrowOnFailureExtended();

            // Helpful for debugging stress and remote dumps
            _encodeState = EncodeState.FrameEncodeResolutionSet;

            if (_supportsFrameThumbnails)
            {
                // Set the thumbnail.
                BitmapSource thumbnail = frame.Thumbnail;

                if (thumbnail != null)
                {
                    SafeMILHandle thumbnailHandle = thumbnail.WicSourceHandle;

                    lock (thumbnail.SyncObject)
                    {
                        UnsafeNativeMethods.WICBitmapFrameEncode.SetThumbnail(
                            frameEncodeHandle,
                            thumbnailHandle).ThrowOnFailureExtended();

                        // Helpful for debugging stress and remote dumps
                        _encodeState = EncodeState.FrameEncodeThumbnailSet;
                    }
                }
            }

            // if the source has been color corrected, we want to use a corresponding color profile
            if (frame._isColorCorrected)
            {
                ColorContext colorContext = new ColorContext(frame.Format);
                IntPtr[] colorContextPtrs = new IntPtr[1] { colorContext.ColorContextHandle.DangerousGetHandle() };

                int hr = UnsafeNativeMethods.WICBitmapFrameEncode.SetColorContexts(
                    frameEncodeHandle,
                    1,
                    colorContextPtrs
                    );

                // It's possible that some encoders may not support color contexts so don't check hr
                if (hr == HRESULT.S_OK)
                {
                    // Helpful for debugging stress and remote dumps
                    _encodeState = EncodeState.FrameEncodeColorContextsSet;
                }
            }
            // if the caller has explicitly provided color contexts, add them to the encoder
            else
            {
                IList<ColorContext> colorContexts = frame.ColorContexts;
                if (colorContexts != null && colorContexts.Count > 0)
                {
                    int count = colorContexts.Count;

                    // Marshal can't convert SafeMILHandle[] so we must
                    {
                        IntPtr[] colorContextPtrs = new IntPtr[count];
                        for (int i = 0; i < count; ++i)
                        {
                            colorContextPtrs[i] = colorContexts[i].ColorContextHandle.DangerousGetHandle();
                        }

                        int hr = UnsafeNativeMethods.WICBitmapFrameEncode.SetColorContexts(
                            frameEncodeHandle,
                            (uint)count,
                            colorContextPtrs
                            );

                        // It's possible that some encoders may not support color contexts so don't check hr
                        if (hr == HRESULT.S_OK)
                        {
                            // Helpful for debugging stress and remote dumps
                            _encodeState = EncodeState.FrameEncodeColorContextsSet;
                        }
                    }
                }
            }

            // Set the pixel format and palette

            lock (frame.SyncObject)
            {
                SafeMILHandle outSourceHandle = new SafeMILHandle();
                SafeMILHandle bitmapSourceHandle = frame.WicSourceHandle;
                SafeMILHandle paletteHandle = new SafeMILHandle();

                // Set the pixel format and palette of the bitmap.
                // This could (but hopefully won't) introduce a format converter.
                UnsafeNativeMethods.WICCodec.WICSetEncoderFormat(
                    bitmapSourceHandle,
                    paletteHandle,
                    frameEncodeHandle,
                    out outSourceHandle).ThrowOnFailureExtended();

                // Helpful for debugging stress and remote dumps
                _encodeState = EncodeState.FrameEncodeFormatSet;
                _writeSourceHandles.Add(outSourceHandle);

                // Set the metadata
                if (_supportsFrameMetadata)
                {
                    BitmapMetadata metadata = frame.Metadata as BitmapMetadata;

                    // If the frame has metadata associated with a different container format, then we ignore it.
                    if (metadata != null && metadata.GuidFormat == ContainerFormat)
                    {
                        SafeMILHandle /* IWICMetadataQueryWriter */ metadataHandle = new SafeMILHandle();

                        UnsafeNativeMethods.WICBitmapFrameEncode.GetMetadataQueryWriter(
                            frameEncodeHandle,
                            out metadataHandle).ThrowOnFailureExtended();

                        PROPVARIANT propVar = new PROPVARIANT();

                        try
                        {
                            propVar.Init(metadata);

                            lock (metadata.SyncObject)
                            {
                                UnsafeNativeMethods.WICMetadataQueryWriter.SetMetadataByName(
                                    metadataHandle,
                                    "/",
                                    ref propVar).ThrowOnFailureExtended();

                                // Helpful for debugging stress and remote dumps
                                _encodeState = EncodeState.FrameEncodeMetadataSet;
                            }
                        }
                        finally
                        {
                            propVar.Clear();
                        }
                    }
                }

                Int32Rect r = new Int32Rect();
                UnsafeNativeMethods.WICBitmapFrameEncode.WriteSource(
                    frameEncodeHandle,
                    outSourceHandle,
                    ref r).ThrowOnFailureExtended();

                // Helpful for debugging stress and remote dumps
                _encodeState = EncodeState.FrameEncodeSourceWritten;

                UnsafeNativeMethods.WICBitmapFrameEncode.Commit(frameEncodeHandle).ThrowOnFailureExtended();

                // Helpful for debugging stress and remote dumps
                _encodeState = EncodeState.FrameEncodeCommitted;
            }
        }

        #endregion

        #region Internal Abstract

        /// "Seals" the object
        internal abstract void SealObject();

        #endregion

        #region Data Members

        /// does encoder support a preview?
        internal bool _supportsPreview = true;

        /// does encoder support a global thumbnail?
        internal bool _supportsGlobalThumbnail = true;

        /// does encoder support a global metadata?
        internal bool _supportsGlobalMetadata = true;

        /// does encoder support per frame thumbnails?
        internal bool _supportsFrameThumbnails = true;

        /// does encoder support per frame thumbnails?
        internal bool _supportsFrameMetadata = true;

        /// does encoder support multiple frames?
        internal bool _supportsMultipleFrames = false;

        /// does encoder support color context?
        internal bool _supportsColorContext = false;

        /// is it a built in encoder
        private bool _isBuiltIn;

        /// Internal WIC encoder handle
        private SafeMILHandle _encoderHandle;

        /// metadata
        private BitmapMetadata _metadata;

        /// Internal WIC metadata handle
        private SafeMILHandle _metadataHandle;

        /// colorcontext
        private ReadOnlyCollection<ColorContext> _readOnlycolorContexts;

        /// codecinfo
        private BitmapCodecInfoInternal _codecInfo;

        /// thumbnail
        private BitmapSource _thumbnail;

        /// preview
        private BitmapSource _preview;

        /// palette
        private BitmapPalette _palette;

        /// frames
        private IList<BitmapFrame> _frames;

        /// true if Save has been called.
        private bool _hasSaved;

        /// The below data members have been added for stress or remote dump debugging purposes
        /// By the time we get an exception in managed code, we loose all context of what was
        /// on the stack and our locals are gone. The below will cache some critcal locals and state
        /// so they can be retrieved during debugging.
        private IList<SafeMILHandle> _frameHandles = new List<SafeMILHandle>(0);
        private IList<SafeMILHandle> _writeSourceHandles = new List<SafeMILHandle>(0);
        private enum EncodeState
        {
            None = 0,
            EncoderInitialized = 1,
            EncoderThumbnailSet = 2,
            EncoderPaletteSet = 3,
            EncoderCreatedNewFrame = 4,
            FrameEncodeInitialized = 5,
            FrameEncodeSizeSet = 6,
            FrameEncodeResolutionSet = 7,
            FrameEncodeThumbnailSet = 8,
            FrameEncodeMetadataSet = 9,
            FrameEncodeFormatSet = 10,
            FrameEncodeSourceWritten = 11,
            FrameEncodeCommitted = 12,
            EncoderCommitted = 13,
            FrameEncodeColorContextsSet = 14,
        };
        private EncodeState _encodeState;

        #endregion
    }

    #endregion // BitmapEncoder
}

