// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
//

using System;
using System.Collections;
using System.Security;
using System.ComponentModel;
using System.ComponentModel.Design.Serialization;
using System.Reflection;
using MS.Internal;
using MS.Win32.PresentationCore;
using System.Diagnostics;
using System.Windows.Media;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Windows.Media.Imaging;

using SR=MS.Internal.PresentationCore.SR;
using SRID=MS.Internal.PresentationCore.SRID;

namespace System.Windows.Media.Imaging
{
    #region WmpBitmapEncoder

    /// <summary>
    /// Built-in Encoder for Wmp files.
    /// </summary>
    public sealed class WmpBitmapEncoder : BitmapEncoder
    {
        #region Constructors

        /// <summary>
        /// Constructor for WmpBitmapEncoder
        /// </summary>
        public WmpBitmapEncoder() :
            base(true)
        {
            _supportsPreview = false;
            _supportsGlobalThumbnail = false;
            _supportsGlobalMetadata = false;
            _supportsMultipleFrames = false;
        }

        #endregion

        #region Public Properties

        // Begin WMPhoto-Canonical Encoder Parameter Properties

        /// <summary>
        /// Set the canonical quality level for the encoding.
        /// The quality level must be between 0.0-1.0, inclusive.
        /// This property is mutually exclusive with doing lossless encoding.
        /// </summary>
        public float ImageQualityLevel
        {
            get
            {
                return _imagequalitylevel;
            }
            set
            {
                if ((value < 0.0) || (value > 1.0))
                {
                    throw new System.ArgumentOutOfRangeException("value", SR.Get(SRID.ParameterMustBeBetween, 0.0, 1.0));
                }

                _imagequalitylevel= value;
            }
        }

        /// <summary>
        /// Set the lossless property for encoding.
        /// The quality level must be true/false.
        /// This property is mutually exclusive with doing lossy encoding.
        /// </summary>
        public bool Lossless
        {
            get
            {
                return _lossless;
            }
            set
            {
                _lossless = value;
            }
        }

        /// <summary>
        /// Set a lossless rotation value of Rotation degrees.
        /// This replaces any previous lossless transformation.
        /// </summary>
        public Rotation Rotation
        {
            get
            {
                if (Rotate90)
                {
                    return Rotation.Rotate90;
                }
                else if (Rotate180)
                {
                    return Rotation.Rotate180;
                }
                else if (Rotate270)
                {
                    return Rotation.Rotate270;
                }
                else
                {
                    return Rotation.Rotate0;
                }
            }

            set
            {
                Rotate90 = false;
                Rotate180 = false;
                Rotate270 = false;

                switch(value)
                {
                    case(Rotation.Rotate0):
                        // do nothing, we reset everything above
                        // case statement is here for clearness
                        break;

                    case(Rotation.Rotate90):
                        Rotate90 = true;
                        break;

                    case(Rotation.Rotate180):
                        Rotate180 = true;
                        break;

                    case(Rotation.Rotate270):
                        Rotate270 = true;
                        break;
                }
            }
        }

        /// <summary>
        /// Set a lossless horizontal flip.
        /// This replaces any previous lossless transformation.
        /// </summary>
        public bool FlipHorizontal
        {
            get
            {
                return (Convert.ToBoolean((int)_transformation & (int)WICBitmapTransformOptions.WICBitmapTransformFlipHorizontal));
            }
            set
            {
                if (value != this.FlipHorizontal)
                {
                    if (value)
                    {
                        _transformation |= WICBitmapTransformOptions.WICBitmapTransformFlipHorizontal;
                    }
                    else
                    {
                        _transformation &= ~WICBitmapTransformOptions.WICBitmapTransformFlipHorizontal;
                    }
                }
            }
        }

        /// <summary>
        /// Set a lossless vertical flip.
        /// This replaces any previous lossless transformation.
        /// </summary>
        public bool FlipVertical
        {
            get
            {
                return (Convert.ToBoolean((int)_transformation & (int)WICBitmapTransformOptions.WICBitmapTransformFlipVertical));
            }
            set
            {
                if (value != this.FlipVertical)
                {
                    if (value)
                    {
                        _transformation |= WICBitmapTransformOptions.WICBitmapTransformFlipVertical;
                    }
                    else
                    {
                        _transformation &= ~WICBitmapTransformOptions.WICBitmapTransformFlipVertical;
                    }
                }
            }
        }

        // End WMPhoto-Canonical Encoder Parameter Properties

        // Begin WMPhoto-Specific Encoder Parameter Properties

        /// <summary>
        /// Set the flag to use wmphoto-specific encoding parameters for encoding.
        /// This property is mutually exclusive with using ImageQuality for encoding.
        /// </summary>
        public bool UseCodecOptions
        {
            get
            {
                return _usecodecoptions;
            }
            set
            {
                _usecodecoptions = value;
            }
        }

        /// <summary>
        /// Set the quality level for the encoding.
        /// The quality level must be between 1-255, inclusive.
        /// </summary>
        public byte QualityLevel
        {
            get
            {
                return _qualitylevel;
            }
            set
            {
                if ((value < 1) || (value > 255))
                {
                    throw new System.ArgumentOutOfRangeException("value", SR.Get(SRID.ParameterMustBeBetween, 1, 255));
                }

                _qualitylevel = value;
            }
        }

        /// <summary>
        /// Set the subsampling level for the encoding.
        /// The subsampling level must be between 0-3, inclusive.
        /// </summary>
        public byte SubsamplingLevel
        {
            get
            {
                return _subsamplinglevel;
            }
            set
            {
                if ((value < 0) || (value > 3))
                {
                    throw new System.ArgumentOutOfRangeException("value", SR.Get(SRID.ParameterMustBeBetween, 0, 3));
                }

                _subsamplinglevel = value;
            }
        }

        /// <summary>
        /// Set the overlap level for the encoding.
        /// The overlap level must be between 0-2, inclusive.
        /// </summary>
        public byte OverlapLevel
        {
            get
            {
                return _overlaplevel;
            }
            set
            {
                if ((value < 0) || (value > 2))
                {
                    throw new System.ArgumentOutOfRangeException("value", SR.Get(SRID.ParameterMustBeBetween, 0, 2));
                }

                _overlaplevel = value;
            }
        }

        /// <summary>
        /// Set the number of horizontal tile slices for the encoding.
        /// The number of horizontal tile slices must be between 0-4096, inclusive.
        /// </summary>
        public short HorizontalTileSlices
        {
            get
            {
                return _horizontaltileslices;
            }
            set
            {
                if ((value < 0) || (value > 4096))
                {
                    throw new System.ArgumentOutOfRangeException("value", SR.Get(SRID.ParameterMustBeBetween, 0, 4096));
                }

                _horizontaltileslices = value;
            }
        }

        /// <summary>
        /// Set the number of horizontal tice slices for the encoding.
        /// The number of horizontal tile slices must be between 0-4096, inclusive.
        /// </summary>
        public short VerticalTileSlices
        {
            get
            {
                return _verticaltileslices;
            }
            set
            {
                if ((value < 0) || (value > 4096))
                {
                    throw new System.ArgumentOutOfRangeException("value", SR.Get(SRID.ParameterMustBeBetween, 0, 4096));
                }

                _verticaltileslices = value;
            }
        }

        /// <summary>
        /// Set the flag to use frequency ordering for encoding.
        /// </summary>
        public bool FrequencyOrder
        {
            get
            {
                return _frequencyorder;
            }
            set
            {
                _frequencyorder = value;
            }
        }

        /// <summary>
        /// Set the flag to for interleaved alpha for encoding.
        /// </summary>
        public bool InterleavedAlpha
        {
            get
            {
                return _interleavedalpha;
            }
            set
            {
                _interleavedalpha = value;
            }
        }

        /// <summary>
        /// Set the aplha quality level for the encoding.
        /// The number of horizontal tile slices must be between 0-255, inclusive.
        /// </summary>
        public byte AlphaQualityLevel
        {
            get
            {
                return _alphaqualitylevel;
            }
            set
            {
                if ((value < 0) || (value > 255))
                {
                    throw new System.ArgumentOutOfRangeException("value", SR.Get(SRID.ParameterMustBeBetween, 0, 255));
                }

                _alphaqualitylevel = value;
            }
        }

        /// <summary>
        /// Set the flag for Compressed domain operations during encoding.
        /// </summary>
        public bool CompressedDomainTranscode
        {
            get
            {
                return _compresseddomaintranscode;
            }
            set
            {
                _compresseddomaintranscode = value;
            }
        }

        /// <summary>
        /// Set the image data discard level for the encoding.
        /// The image data discard level must be between 0-3, inclusive.
        /// </summary>
        public byte ImageDataDiscardLevel
        {
            get
            {
                return _imagedatadiscardlevel;
            }
            set
            {
                if ((value < 0) || (value > 3))
                {
                    throw new System.ArgumentOutOfRangeException("value", SR.Get(SRID.ParameterMustBeBetween, 0, 3));
                }

                _imagedatadiscardlevel = value;
            }
        }

        /// <summary>
        /// Set the alpha data discard level for the encoding.
        /// The alpha data discard level must be between 0-3, inclusive.
        /// </summary>
        public byte AlphaDataDiscardLevel
        {
            get
            {
                return _alphadatadiscardlevel;
            }
            set
            {
                if ((value < 0) || (value > 4))
                {
                    throw new System.ArgumentOutOfRangeException("value", SR.Get(SRID.ParameterMustBeBetween, 0, 4));
                }

                _alphadatadiscardlevel = value;
            }
        }

        /// <summary>
        /// Set the flag to ignore overlap during encoding.
        /// </summary>
        public bool IgnoreOverlap
        {
            get
            {
                return _ignoreoverlap;
            }
            set
            {
                _ignoreoverlap = value;
            }
        }

        // End WMPhoto-Specific Encoder Parameter Properties

        /// <summary>
        /// Setups the encoder and other properties before encoding each frame
        /// </summary>
        internal override void SetupFrame(SafeMILHandle frameEncodeHandle, SafeMILHandle encoderOptions)
        {
            PROPBAG2 propBag = new PROPBAG2();
            PROPVARIANT propValue = new PROPVARIANT();

            if (_imagequalitylevel != c_defaultImageQualityLevel)
            {
                try
                {
                    propBag.Init("ImageQuality");
                    propValue.Init((float)_imagequalitylevel);

                    HRESULT.Check(UnsafeNativeMethods.IPropertyBag2.Write(
                        encoderOptions,
                        1,
                        ref propBag,
                        ref propValue));
                }
                finally
                {
                    propBag.Clear();
                    propValue.Clear();
                }
            }

            if (_transformation != c_defaultTransformation)
            {
                try
                {
                    propBag.Init("BitmapTransform");
                    propValue.Init((byte) _transformation);

                    HRESULT.Check(UnsafeNativeMethods.IPropertyBag2.Write(
                        encoderOptions,
                        1,
                        ref propBag,
                        ref propValue));
                }
                finally
                {
                    propBag.Clear();
                    propValue.Clear();
                }
            }

            if (_lossless != c_defaultLossless)
            {
                try
                {
                    propBag.Init("Lossless");
                    propValue.Init((bool)_lossless);

                    HRESULT.Check(UnsafeNativeMethods.IPropertyBag2.Write(
                        encoderOptions,
                        1,
                        ref propBag,
                        ref propValue));
                }
                finally
                {
                    propBag.Clear();
                    propValue.Clear();
                }
            }

            if (_usecodecoptions != c_defaultUseCodecOptions)
            {
                try
                {
                    propBag.Init("UseCodecOptions");
                    propValue.Init((bool)_usecodecoptions);

                    HRESULT.Check(UnsafeNativeMethods.IPropertyBag2.Write(
                        encoderOptions,
                        1,
                        ref propBag,
                        ref propValue));
                }
                finally
                {
                    propBag.Clear();
                    propValue.Clear();
                }
            }

            if (_qualitylevel != c_defaultQualityLevel)
            {
                try
                {
                    propBag.Init("Quality");
                    propValue.Init((byte)_qualitylevel);

                    HRESULT.Check(UnsafeNativeMethods.IPropertyBag2.Write(
                        encoderOptions,
                        1,
                        ref propBag,
                        ref propValue));
                }
                finally
                {
                    propBag.Clear();
                    propValue.Clear();
                }
            }

            if (_subsamplinglevel != c_defaultSubsamplingLevel)
            {
                try
                {
                    propBag.Init("Subsampling");
                    propValue.Init((byte)_subsamplinglevel);

                    HRESULT.Check(UnsafeNativeMethods.IPropertyBag2.Write(
                        encoderOptions,
                        1,
                        ref propBag,
                        ref propValue));
                }
                finally
                {
                    propBag.Clear();
                    propValue.Clear();
                }
            }

            if (_overlaplevel != c_defaultOverlapLevel)
            {
                try
                {
                    propBag.Init("Overlap");
                    propValue.Init((byte)_overlaplevel);

                    HRESULT.Check(UnsafeNativeMethods.IPropertyBag2.Write(
                        encoderOptions,
                        1,
                        ref propBag,
                        ref propValue));
                }
                finally
                {
                    propBag.Clear();
                    propValue.Clear();
                }
            }

            if (_horizontaltileslices != c_defaultHorizontalTileSlices)
            {
                try
                {
                    propBag.Init("HorizontalTileSlices");
                    propValue.Init((ushort)_horizontaltileslices );

                    HRESULT.Check(UnsafeNativeMethods.IPropertyBag2.Write(
                        encoderOptions,
                        1,
                        ref propBag,
                        ref propValue));
                }
                finally
                {
                    propBag.Clear();
                    propValue.Clear();
                }
            }

            if (_verticaltileslices != c_defaultVerticalTileSlices)
            {
                try
                {
                    propBag.Init("VerticalTileSlices");
                    propValue.Init((ushort)_verticaltileslices );

                    HRESULT.Check(UnsafeNativeMethods.IPropertyBag2.Write(
                        encoderOptions,
                        1,
                        ref propBag,
                        ref propValue));
                }
                finally
                {
                    propBag.Clear();
                    propValue.Clear();
                }
            }

            if (_frequencyorder != c_defaultFrequencyOrder)
            {
                try
                {
                    propBag.Init("FrequencyOrder");
                    propValue.Init((bool)_frequencyorder);

                    HRESULT.Check(UnsafeNativeMethods.IPropertyBag2.Write(
                        encoderOptions,
                        1,
                        ref propBag,
                        ref propValue));
                }
                finally
                {
                    propBag.Clear();
                    propValue.Clear();
                }
            }

            if (_interleavedalpha != c_defaultInterleavedAlpha)
            {
                try
                {
                    propBag.Init("InterleavedAlpha");
                    propValue.Init((bool)_interleavedalpha);

                    HRESULT.Check(UnsafeNativeMethods.IPropertyBag2.Write(
                        encoderOptions,
                        1,
                        ref propBag,
                        ref propValue));
                }
                finally
                {
                    propBag.Clear();
                    propValue.Clear();
                }
            }

            if (_alphaqualitylevel != c_defaultAlphaQualityLevel)
            {
                try
                {
                    propBag.Init("AlphaQuality");
                    propValue.Init((byte)_alphaqualitylevel);

                    HRESULT.Check(UnsafeNativeMethods.IPropertyBag2.Write(
                        encoderOptions,
                        1,
                        ref propBag,
                        ref propValue));
                }
                finally
                {
                    propBag.Clear();
                    propValue.Clear();
                }
            }

            if (_compresseddomaintranscode  != c_defaultCompressedDomainTranscode)
            {
                try
                {
                    propBag.Init("CompressedDomainTranscode");
                    propValue.Init((bool)_compresseddomaintranscode);

                    HRESULT.Check(UnsafeNativeMethods.IPropertyBag2.Write(
                        encoderOptions,
                        1,
                        ref propBag,
                        ref propValue));
                }
                finally
                {
                    propBag.Clear();
                    propValue.Clear();
                }
            }

            if (_imagedatadiscardlevel != c_defaultImageDataDiscardLevel)
            {
                try
                {
                    propBag.Init("ImageDataDiscard");
                    propValue.Init((byte)_imagedatadiscardlevel);

                    HRESULT.Check(UnsafeNativeMethods.IPropertyBag2.Write(
                        encoderOptions,
                        1,
                        ref propBag,
                        ref propValue));
                }
                finally
                {
                    propBag.Clear();
                    propValue.Clear();
                }
            }

            if (_alphadatadiscardlevel != c_defaultAlphaDataDiscardLevel)
            {
                try
                {
                    propBag.Init("AlphaDataDiscard");
                    propValue.Init((byte)_alphadatadiscardlevel);

                    HRESULT.Check(UnsafeNativeMethods.IPropertyBag2.Write(
                        encoderOptions,
                        1,
                        ref propBag,
                        ref propValue));
                }
                finally
                {
                    propBag.Clear();
                    propValue.Clear();
                }
            }

            if (_ignoreoverlap != c_defaultIgnoreOverlap)
            {
                try
                {
                    propBag.Init("IgnoreOverlap");
                    propValue.Init((bool)_ignoreoverlap);

                    HRESULT.Check(UnsafeNativeMethods.IPropertyBag2.Write(
                        encoderOptions,
                        1,
                        ref propBag,
                        ref propValue));
                }
                finally
                {
                    propBag.Clear();
                    propValue.Clear();
                }
            }

            HRESULT.Check(UnsafeNativeMethods.WICBitmapFrameEncode.Initialize(
                frameEncodeHandle,
                encoderOptions
                ));
        }

        /// <summary>
        /// Set a lossless rotation value of 90 degrees.
        /// This replaces any previous lossless transformation.
        /// We can coexist with Flip operation
        /// </summary>
        private bool Rotate90
        {
            get
            {
                return ((Convert.ToBoolean((int)_transformation & (int)WICBitmapTransformOptions.WICBitmapTransformRotate90) && (!Rotate270)));
            }
            set
            {
                if (value != this.Rotate90)
                {
                    bool IsFlipH = FlipHorizontal;
                    bool IsFlipV = FlipVertical;
                    if (value)
                    {
                        _transformation = WICBitmapTransformOptions.WICBitmapTransformRotate90;
                    }
                    else
                    {
                        _transformation = WICBitmapTransformOptions.WICBitmapTransformRotate0;
                    }
                    FlipHorizontal = IsFlipH;
                    FlipVertical = IsFlipV;
                }
            }
        }

        /// <summary>
        /// Set a lossless rotation value of 180 degrees.
        /// This replaces any previous lossless transformation.
        /// We can coexist with Flip operation
        /// </summary>
        private bool Rotate180
        {
            get
            {
                return ((Convert.ToBoolean((int)_transformation & (int)WICBitmapTransformOptions.WICBitmapTransformRotate180) && (!Rotate270)));
            }
            set
            {
                if (value != this.Rotate180)
                {
                    bool IsFlipH = FlipHorizontal;
                    bool IsFlipV = FlipVertical;
                    if (value)
                    {
                        _transformation = WICBitmapTransformOptions.WICBitmapTransformRotate180;
                    }
                    else
                    {
                        _transformation = WICBitmapTransformOptions.WICBitmapTransformRotate0;
                    }
                    FlipHorizontal = IsFlipH;
                    FlipVertical = IsFlipV;
                }
            }
        }

        /// <summary>
        /// Set a lossless rotation value of 270 degrees.
        /// This replaces any previous lossless transformation.
        /// We can coexist with Flip operation
        /// </summary>
        private bool Rotate270
        {
            get
            {
                return (Convert.ToBoolean(((int)_transformation & (int)WICBitmapTransformOptions.WICBitmapTransformRotate270) == (int)WICBitmapTransformOptions.WICBitmapTransformRotate270));
            }
            set
            {
                if (value != this.Rotate270)
                {
                    bool IsFlipH = FlipHorizontal;
                    bool IsFlipV = FlipVertical;
                    if (value)
                    {
                        _transformation = WICBitmapTransformOptions.WICBitmapTransformRotate270;
                    }
                    else
                    {
                        _transformation = WICBitmapTransformOptions.WICBitmapTransformRotate0;
                    }
                    FlipHorizontal = IsFlipH;
                    FlipVertical = IsFlipV;
                }
            }
        }

        #endregion

        #region Internal Properties / Methods

        /// <summary>
        /// Returns the container format for this encoder
        /// </summary>
        internal override Guid ContainerFormat
        {
            get
            {
                return _containerFormat;
            }
        }

        #endregion

        #region Internal Abstract

        /// Need to implement this to derive from the "sealed" object
        internal override void SealObject()
        {
            throw new NotImplementedException();
        }

        #endregion

        #region Data Members

        private Guid _containerFormat = MILGuidData.GUID_ContainerFormatWmp;

        private const bool c_defaultLossless = false;
        private bool _lossless = c_defaultLossless;

        private const float c_defaultImageQualityLevel= 0.9f;
        private float _imagequalitylevel= c_defaultImageQualityLevel;

        private const WICBitmapTransformOptions c_defaultTransformation = WICBitmapTransformOptions.WICBitmapTransformRotate0;
        private WICBitmapTransformOptions _transformation = c_defaultTransformation;

        private const bool c_defaultUseCodecOptions = false;
        private bool _usecodecoptions = c_defaultUseCodecOptions;

        private const byte c_defaultQualityLevel = 10;
        private byte _qualitylevel = c_defaultQualityLevel;

        private const byte c_defaultSubsamplingLevel = 3;
        private byte _subsamplinglevel = c_defaultSubsamplingLevel;

        private const byte c_defaultOverlapLevel = 1;
        private byte _overlaplevel = c_defaultOverlapLevel;

        private const short c_defaultHorizontalTileSlices = 0;
        private short _horizontaltileslices = c_defaultHorizontalTileSlices;

        private const short c_defaultVerticalTileSlices = 0;
        private short _verticaltileslices = c_defaultVerticalTileSlices;

        private const bool c_defaultFrequencyOrder = true;
        private bool _frequencyorder = c_defaultFrequencyOrder;

        private const bool c_defaultInterleavedAlpha = false;
        private bool _interleavedalpha = c_defaultInterleavedAlpha;

        private const byte c_defaultAlphaQualityLevel = 1;
        private byte _alphaqualitylevel = c_defaultAlphaQualityLevel;

        private const bool c_defaultCompressedDomainTranscode = true;
        private bool _compresseddomaintranscode = c_defaultCompressedDomainTranscode;

        private const byte c_defaultImageDataDiscardLevel = 0;
        private byte _imagedatadiscardlevel  = c_defaultImageDataDiscardLevel;

        private const byte c_defaultAlphaDataDiscardLevel = 0;
        private byte _alphadatadiscardlevel  = c_defaultAlphaDataDiscardLevel;

        private const bool c_defaultIgnoreOverlap = false;
        private bool _ignoreoverlap = c_defaultIgnoreOverlap;

        #endregion
    }

    #endregion // WmpBitmapEncoder
}


