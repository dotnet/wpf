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

namespace System.Windows.Media
{
    #region PixelFormats

    /// <summary>
    /// PixelFormats - The collection of supported Pixel Formats
    /// </summary>
    public static class PixelFormats
    {
        /// <summary>
        /// Default: for situations when the pixel format may not be important
        /// </summary>
        public static PixelFormat Default
        {
            get
            {
                return new PixelFormat(PixelFormatEnum.Default);
            }
        }

        /// <summary>
        /// Indexed1: Paletted image with 2 colors.
        /// </summary>
        public static PixelFormat Indexed1
        {
            get
            {
                return new PixelFormat(PixelFormatEnum.Indexed1);
            }
        }

        /// <summary>
        /// Indexed2: Paletted image with 4 colors.
        /// </summary>
        public static PixelFormat Indexed2
        {
            get
            {
                return new PixelFormat(PixelFormatEnum.Indexed2);
            }
        }

        /// <summary>
        /// Indexed4: Paletted image with 16 colors.
        /// </summary>
        public static PixelFormat Indexed4
        {
            get
            {
                return new PixelFormat(PixelFormatEnum.Indexed4);
            }
        }

        /// <summary>
        /// Indexed8: Paletted image with 256 colors.
        /// </summary>
        public static PixelFormat Indexed8
        {
            get
            {
                return new PixelFormat(PixelFormatEnum.Indexed8);
            }
        }

        /// <summary>
        /// BlackWhite: Monochrome, 2-color image, black and white only.
        /// </summary>
        public static PixelFormat BlackWhite
        {
            get
            {
                return new PixelFormat(PixelFormatEnum.BlackWhite);
            }
        }

        /// <summary>
        /// Gray2: Image with 4 shades of gray
        /// </summary>
        public static PixelFormat Gray2
        {
            get
            {
                return new PixelFormat(PixelFormatEnum.Gray2);
            }
        }

        /// <summary>
        /// Gray4: Image with 16 shades of gray
        /// </summary>
        public static PixelFormat Gray4
        {
            get
            {
                return new PixelFormat(PixelFormatEnum.Gray4);
            }
        }

        /// <summary>
        /// Gray8: Image with 256 shades of gray
        /// </summary>
        public static PixelFormat Gray8
        {
            get
            {
                return new PixelFormat(PixelFormatEnum.Gray8);
            }
        }

        /// <summary>
        /// Bgr555: 16 bpp SRGB format
        /// </summary>
        public static PixelFormat Bgr555
        {
            get
            {
                return new PixelFormat(PixelFormatEnum.Bgr555);
            }
        }

        /// <summary>
        /// Bgr565: 16 bpp SRGB format
        /// </summary>
        public static PixelFormat Bgr565
        {
            get
            {
                return new PixelFormat(PixelFormatEnum.Bgr565);
            }
        }

        /// <summary>
        /// Rgb128Float: 128 bpp extended format; Gamma is 1.0
        /// </summary>
        public static PixelFormat Rgb128Float
        {
            get
            {
                return new PixelFormat(PixelFormatEnum.Rgb128Float);
            }
        }

        /// <summary>
        /// Bgr24: 24 bpp SRGB format
        /// </summary>
        public static PixelFormat Bgr24
        {
            get
            {
                return new PixelFormat(PixelFormatEnum.Bgr24);
            }
        }

        /// <summary>
        /// Rgb24: 24 bpp SRGB format
        /// </summary>
        public static PixelFormat Rgb24
        {
            get
            {
                return new PixelFormat(PixelFormatEnum.Rgb24);
            }
        }

        /// <summary>
        /// Bgr101010: 32 bpp SRGB format
        /// </summary>
        public static PixelFormat Bgr101010
        {
            get
            {
                return new PixelFormat(PixelFormatEnum.Bgr101010);
            }
        }

        /// <summary>
        /// Bgr32: 32 bpp SRGB format
        /// </summary>
        public static PixelFormat Bgr32
        {
            get
            {
                return new PixelFormat(PixelFormatEnum.Bgr32);
            }
        }

        /// <summary>
        /// Bgra32: 32 bpp SRGB format
        /// </summary>
        public static PixelFormat Bgra32
        {
            get
            {
                return new PixelFormat(PixelFormatEnum.Bgra32);
            }
        }

        /// <summary>
        /// Pbgra32: 32 bpp SRGB format
        /// </summary>
        public static PixelFormat Pbgra32
        {
            get
            {
                return new PixelFormat(PixelFormatEnum.Pbgra32);
            }
        }

        /// <summary>
        /// Rgb48: 48 bpp extended format; Gamma is 1.0
        /// </summary>
        public static PixelFormat Rgb48
        {
            get
            {
                return new PixelFormat(PixelFormatEnum.Rgb48);
            }
        }

        /// <summary>
        /// Rgba64: 64 bpp extended format; Gamma is 1.0
        /// </summary>
        public static PixelFormat Rgba64
        {
            get
            {
                return new PixelFormat(PixelFormatEnum.Rgba64);
            }
        }

        /// <summary>
        /// Prgba64: 64 bpp extended format; Gamma is 1.0
        /// </summary>
        public static PixelFormat Prgba64
        {
            get
            {
                return new PixelFormat(PixelFormatEnum.Prgba64);
            }
        }

        /// <summary>
        /// Gray16: 16 bpp Gray-scale format; Gamma is 1.0
        /// </summary>
        public static PixelFormat Gray16
        {
            get
            {
                return new PixelFormat(PixelFormatEnum.Gray16);
            }
        }

        /// <summary>
        /// Gray32Float: 32 bpp Gray-scale format; Gamma is 1.0
        /// </summary>
        public static PixelFormat Gray32Float
        {
            get
            {
                return new PixelFormat(PixelFormatEnum.Gray32Float);
            }
        }

        /// <summary>
        /// Rgba128Float: 128 bpp extended format; Gamma is 1.0
        /// </summary>
        public static PixelFormat Rgba128Float
        {
            get
            {
                return new PixelFormat(PixelFormatEnum.Rgba128Float);
            }
        }

        /// <summary>
        /// Prgba128Float: 128 bpp extended format; Gamma is 1.0
        /// </summary>
        public static PixelFormat Prgba128Float
        {
            get
            {
                return new PixelFormat(PixelFormatEnum.Prgba128Float);
            }
        }

        /// <summary>
        /// Cmyk32: 32 bpp format
        /// </summary>
        public static PixelFormat Cmyk32
        {
            get
            {
                return new PixelFormat(PixelFormatEnum.Cmyk32);
            }
        }
    }
    #endregion // PixelFormats
}
