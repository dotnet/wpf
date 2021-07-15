// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
//

using System;
using System.Collections;
using System.ComponentModel;
using System.ComponentModel.Design.Serialization;
using System.Reflection;
using MS.Internal;
using System.Diagnostics;
using System.Windows.Media;
using System.Globalization;
using System.Runtime.InteropServices;
using SR=MS.Internal.PresentationCore.SR;
using SRID=MS.Internal.PresentationCore.SRID;

namespace System.Windows.Media.Imaging
{
    #region BitmapSizeOptions

    /// <summary>
    /// Sizing options for an bitmap.  The resulting bitmap
    /// will be scaled based on these options.
    /// </summary>
    public class BitmapSizeOptions
    {
        /// <summary>
        /// Construct an BitmapSizeOptions object.  Still need to set the Width and Height Properties.
        /// </summary>
        private BitmapSizeOptions()
        {
        }

        /// <summary>
        /// Whether or not to preserve the aspect ratio of the original
        /// bitmap.  If so, then the PixelWidth and PixelHeight are
        /// maximum values for the bitmap size.  The resulting bitmap
        /// is only guaranteed to have either its width or its height
        /// match the specified values.  For example, if you want to
        /// specify the height, while preserving the aspect ratio for
        /// the width, then set the height to the desired value, and
        /// set the width to Int32.MaxValue.
        ///
        /// If we are not to preserve aspect ratio, then both the
        /// specified width and the specified height are used, and
        /// the bitmap will be stretched to fit both those values.
        /// </summary>
        public bool PreservesAspectRatio
        {
            get
            {
                return _preservesAspectRatio;
            }
        }

        /// <summary>
        /// PixelWidth of the resulting bitmap.  See description of
        /// PreserveAspectRatio for how this value is used.
        ///
        /// PixelWidth must be set to a value greater than zero to be valid.
        /// </summary>
        public int PixelWidth
        {
            get
            {
                return _pixelWidth;
            }
        }

        /// <summary>
        /// PixelHeight of the resulting bitmap.  See description of
        /// PreserveAspectRatio for how this value is used.
        ///
        /// PixelHeight must be set to a value greater than zero to be valid.
        /// </summary>
        public int PixelHeight
        {
            get
            {
                return _pixelHeight;
            }
        }

        /// <summary>
        /// Rotation to rotate the bitmap.  Only multiples of 90 are supported.
        /// </summary>
        public Rotation Rotation
        {
            get
            {
                return _rotationAngle;
            }
        }

        /// <summary>
        /// Constructs an identity BitmapSizeOptions (when passed to a TransformedBitmap, the
        /// input is the same as the output).
        /// </summary>
        public static BitmapSizeOptions FromEmptyOptions()
        {
            BitmapSizeOptions sizeOptions = new BitmapSizeOptions();

            sizeOptions._rotationAngle          = Rotation.Rotate0;
            sizeOptions._preservesAspectRatio = true;
            sizeOptions._pixelHeight         = 0;
            sizeOptions._pixelWidth          = 0;

            return sizeOptions;
        }

        /// <summary>
        /// Constructs an BitmapSizeOptions that preserves the aspect ratio and enforces a height of pixelHeight.
        /// </summary>
        /// <param name="pixelHeight">Height of the resulting Bitmap</param>
        public static BitmapSizeOptions FromHeight(int pixelHeight)
        {
            if (pixelHeight <= 0)
            {
                throw new System.ArgumentOutOfRangeException("pixelHeight", SR.Get(SRID.ParameterMustBeGreaterThanZero));
            }

            BitmapSizeOptions sizeOptions = new BitmapSizeOptions();

            sizeOptions._rotationAngle          = Rotation.Rotate0;
            sizeOptions._preservesAspectRatio = true;
            sizeOptions._pixelHeight         = pixelHeight;
            sizeOptions._pixelWidth          = 0;

            return sizeOptions;
        }

        /// <summary>
        /// Constructs an BitmapSizeOptions that preserves the aspect ratio and enforces a width of pixelWidth.
        /// </summary>
        /// <param name="pixelWidth">Width of the resulting Bitmap</param>
        public static BitmapSizeOptions FromWidth(int pixelWidth)
        {
            if (pixelWidth <= 0)
            {
                throw new System.ArgumentOutOfRangeException("pixelWidth", SR.Get(SRID.ParameterMustBeGreaterThanZero));
            }

            BitmapSizeOptions sizeOptions = new BitmapSizeOptions();

            sizeOptions._rotationAngle          = Rotation.Rotate0;
            sizeOptions._preservesAspectRatio = true;
            sizeOptions._pixelWidth          = pixelWidth;
            sizeOptions._pixelHeight         = 0;

            return sizeOptions;
        }

        /// <summary>
        /// Constructs an BitmapSizeOptions that does not preserve the aspect ratio and
        /// instead uses dimensions pixelWidth x pixelHeight.
        /// </summary>
        /// <param name="pixelWidth">Width of the resulting Bitmap</param>
        /// <param name="pixelHeight">Height of the resulting Bitmap</param>
        public static BitmapSizeOptions FromWidthAndHeight(int pixelWidth, int pixelHeight)
        {
            if (pixelWidth <= 0)
            {
                throw new System.ArgumentOutOfRangeException("pixelWidth", SR.Get(SRID.ParameterMustBeGreaterThanZero));
            }

            if (pixelHeight <= 0)
            {
                throw new System.ArgumentOutOfRangeException("pixelHeight", SR.Get(SRID.ParameterMustBeGreaterThanZero));
            }

            BitmapSizeOptions sizeOptions = new BitmapSizeOptions();

            sizeOptions._rotationAngle          = Rotation.Rotate0;
            sizeOptions._preservesAspectRatio = false;
            sizeOptions._pixelWidth          = pixelWidth;
            sizeOptions._pixelHeight         = pixelHeight;

            return sizeOptions;
        }

        /// <summary>
        /// Constructs an BitmapSizeOptions that does not preserve the aspect ratio and
        /// instead uses dimensions pixelWidth x pixelHeight.
        /// </summary>
        /// <param name="rotation">Angle to rotate</param>
        public static BitmapSizeOptions FromRotation(Rotation rotation)
        {
            switch(rotation)
            {
                case Rotation.Rotate0:
                case Rotation.Rotate90:
                case Rotation.Rotate180:
                case Rotation.Rotate270:
                    break;
                default:
                    throw new ArgumentException(SR.Get(SRID.Image_SizeOptionsAngle), "rotation");
            }

            BitmapSizeOptions sizeOptions = new BitmapSizeOptions();

            sizeOptions._rotationAngle          = rotation;
            sizeOptions._preservesAspectRatio = true;
            sizeOptions._pixelWidth          = 0;
            sizeOptions._pixelHeight         = 0;

            return sizeOptions;
        }

        // Note: In this method, newWidth, newHeight are not affected by the
        // rotation angle.
        internal void GetScaledWidthAndHeight(
            uint width,
            uint height,
            out uint newWidth,
            out uint newHeight)
        {
            if (_pixelWidth == 0 && _pixelHeight != 0)
            {
                Debug.Assert(_preservesAspectRatio == true);

                newWidth = (uint)((_pixelHeight * width)/height);
                newHeight = (uint)_pixelHeight;
            }
            else if (_pixelWidth != 0 && _pixelHeight == 0)
            {
                Debug.Assert(_preservesAspectRatio == true);

                newWidth = (uint)_pixelWidth;
                newHeight = (uint)((_pixelWidth * height)/width);
            }
            else if (_pixelWidth != 0 && _pixelHeight != 0)
            {
                Debug.Assert(_preservesAspectRatio == false);

                newWidth = (uint)_pixelWidth;
                newHeight = (uint)_pixelHeight;
            }
            else
            {
                newWidth = width;
                newHeight = height;
            }
        }

        internal bool DoesScale
        {
            get
            {
                return (_pixelWidth != 0 || _pixelHeight != 0);
            }
        }

        internal WICBitmapTransformOptions WICTransformOptions
        {
            get
            {
                WICBitmapTransformOptions options = 0;

                switch (_rotationAngle)
                {
                    case Rotation.Rotate0:
                        options = WICBitmapTransformOptions.WICBitmapTransformRotate0;
                        break;
                    case Rotation.Rotate90:
                        options = WICBitmapTransformOptions.WICBitmapTransformRotate90;
                        break;
                    case Rotation.Rotate180:
                        options = WICBitmapTransformOptions.WICBitmapTransformRotate180;
                        break;
                    case Rotation.Rotate270:
                        options = WICBitmapTransformOptions.WICBitmapTransformRotate270;
                        break;
                    default:
                        Debug.Assert(false);
                        break;
                }

                return options;
            }
        }

        private bool        _preservesAspectRatio;
        private int         _pixelWidth;
        private int         _pixelHeight;
        private Rotation    _rotationAngle;
    }

    #endregion // BitmapSizeOptions
}

