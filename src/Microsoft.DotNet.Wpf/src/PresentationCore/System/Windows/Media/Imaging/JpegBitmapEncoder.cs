// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
//

using System;
using System.Security;
using System.Collections;
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
    #region JpegBitmapEncoder

    /// <summary>
    /// Built-in Encoder for Jpeg files.
    /// </summary>
    public sealed class JpegBitmapEncoder : BitmapEncoder
    {
        #region Constructors

        /// <summary>
        /// Constructor for JpegBitmapEncoder
        /// </summary>
        public JpegBitmapEncoder() :
            base(true)
        {
            _supportsPreview = false;
            _supportsGlobalThumbnail = false;
            _supportsGlobalMetadata = false;
            _supportsFrameThumbnails = true;
            _supportsMultipleFrames = false;
            _supportsFrameMetadata = true;
        }

        #endregion

        #region Public Properties

        /// <summary>
        /// Set the quality level for the encoding.
        /// The quality level must be between 1-100, inclusive.
        /// This property is mutually exclusive with doing lossless encoding.
        /// </summary>
        public int QualityLevel
        {
            get
            {
                return _qualityLevel;
            }
            set
            {
                if ((value < 1) || (value > 100))
                {
                    throw new System.ArgumentOutOfRangeException("value", SR.Get(SRID.ParameterMustBeBetween, 1, 100));
                }

                _qualityLevel = value;
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

        /// <summary>
        /// Setups the encoder and other properties before encoding each frame
        /// </summary>
        internal override void SetupFrame(SafeMILHandle frameEncodeHandle, SafeMILHandle encoderOptions)
        {
            PROPBAG2 propBag = new PROPBAG2();
            PROPVARIANT propValue = new PROPVARIANT();

            // There are only two encoder options supported here:

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

            if (_qualityLevel != c_defaultQualityLevel)
            {
                try
                {
                    propBag.Init("ImageQuality");
                    propValue.Init( ((float)_qualityLevel) / 100.0f);

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

        #region Internal Abstract

        /// Need to implement this to derive from the "sealed" object
        internal override void SealObject()
        {
            throw new NotImplementedException();
        }

        #endregion

        #region Data Members

        private Guid _containerFormat = MILGuidData.GUID_ContainerFormatJpeg;

        // This happens to be the default used by the jpeg lib.
        private const int c_defaultQualityLevel = 75;
        private int _qualityLevel = c_defaultQualityLevel;

        private const WICBitmapTransformOptions c_defaultTransformation = WICBitmapTransformOptions.WICBitmapTransformRotate0;
        private WICBitmapTransformOptions _transformation = c_defaultTransformation;

        #endregion
    }

    #endregion // JpegBitmapEncoder
}

