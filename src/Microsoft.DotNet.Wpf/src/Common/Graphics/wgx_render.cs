// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace MS.Internal
{
    internal enum LockFlags
    {
        MIL_LOCK_READ        = 0x00000001,
        MIL_LOCK_WRITE       = 0x00000002,
    }

    internal enum WICBitmapAlphaChannelOption
    {
        WICBitmapUseAlpha              = 0,
        WICBitmapUsePremultipliedAlpha = 1,
        WICBitmapIgnoreAlpha           = 2,
    }

    internal enum WICBitmapCreateCacheOptions
    {
        WICBitmapNoCache            = 0x00000000,
        WICBitmapCacheOnDemand      = 0x00000001,
        WICBitmapCacheOnLoad        = 0x00000002,
    }

    internal enum WICBitmapEncodeCacheOption
    {
        WICBitmapEncodeCacheInMemory = 0x00000000,
        WICBitmapEncodeCacheTempFile = 0x00000001,
        WICBitmapEncodeNoCache       = 0x00000002,
    }

    internal enum WICMetadataCacheOptions
    {
        WICMetadataCacheOnDemand = 0x00000000,
        WICMetadataCacheOnLoad   = 0x00000001
    };

    internal enum WICInterpolationMode
    {
        NearestNeighbor = 0,
        Linear          = 1,
        Cubic           = 2,
        Fant            = 3
    }

    /// <summary>
    /// PixelFormatEnum represents the format of the bits of an image or surface.
    /// </summary>
    internal enum PixelFormatEnum
    {
        /// <summary>
        /// Default: (DontCare) the format is not important
        /// </summary>
        Default    = 0,

        /// <summary>
        /// Extended: the pixel format is 3rd party - we don't know anything about it.
        /// </summary>
        Extended   = Default,

        /// <summary>
        /// Indexed1: Paletted image with 2 colors.
        /// </summary>
        Indexed1    = 0x1,

        /// <summary>
        /// Indexed2: Paletted image with 4 colors.
        /// </summary>
        Indexed2    = 0x2,

        /// <summary>
        /// Indexed4: Paletted image with 16 colors.
        /// </summary>
        Indexed4    = 0x3,

        /// <summary>
        /// Indexed8: Paletted image with 256 colors.
        /// </summary>
        Indexed8    = 0x4,

        /// <summary>
        /// BlackWhite: Monochrome, 2-color image, black and white only.
        /// </summary>
        BlackWhite  = 0x5,

        /// <summary>
        /// Gray2: Image with 4 shades of gray
        /// </summary>
        Gray2       = 0x6,

        /// <summary>
        /// Gray4: Image with 16 shades of gray
        /// </summary>
        Gray4       = 0x7,

        /// <summary>
        /// Gray8: Image with 256 shades of gray
        /// </summary>
        Gray8       = 0x8,

        /// <summary>
        /// Bgr555: 16 bpp SRGB format
        /// </summary>
        Bgr555      = 0x9,

        /// <summary>
        /// Bgr565: 16 bpp SRGB format
        /// </summary>
        Bgr565      = 0xA,

        /// <summary>
        /// Gray16: 16 bpp Gray format
        /// </summary>
        Gray16 = 0xB,

        /// <summary>
        /// Bgr24: 24 bpp SRGB format
        /// </summary>
        Bgr24       = 0xC,

        /// <summary>
        /// BGR24: 24 bpp SRGB format
        /// </summary>
        Rgb24       = 0xD,

        /// <summary>
        /// Bgr32: 32 bpp SRGB format
        /// </summary>
        Bgr32       = 0xE,

        /// <summary>
        /// Bgra32: 32 bpp SRGB format
        /// </summary>
        Bgra32      = 0xF,

        /// <summary>
        /// Pbgra32: 32 bpp SRGB format
        /// </summary>
        Pbgra32     = 0x10,

        /// <summary>
        /// Gray32Float: 32 bpp Gray format, gamma is 1.0
        /// </summary>
        Gray32Float = 0x11,

        /// <summary>
        /// Bgr101010: 32 bpp Gray fixed point format
        /// </summary>
        Bgr101010 = 0x14,

        /// <summary>
        /// Rgb48: 48 bpp RGB format
        /// </summary>
        Rgb48 = 0x15,

        /// <summary>
        /// Rgba64: 64 bpp extended format; Gamma is 1.0
        /// </summary>
        Rgba64      = 0x16,

        /// <summary>
        /// Prgba64: 64 bpp extended format; Gamma is 1.0
        /// </summary>
        Prgba64     = 0x17,

        /// <summary>
        /// Rgba128Float: 128 bpp extended format; Gamma is 1.0
        /// </summary>
        Rgba128Float     = 0x19,

        /// <summary>
        /// Prgba128Float: 128 bpp extended format; Gamma is 1.0
        /// </summary>
        Prgba128Float    = 0x1A,

        /// <summary>
        /// PABGR128Float: 128 bpp extended format; Gamma is 1.0
        /// </summary>
        Rgb128Float    = 0x1B,

        /// <summary>
        /// CMYK32: 32 bpp CMYK format.
        /// </summary>
        Cmyk32      = 0x1C
    }

    internal enum DitherType
    {
        // Solid color - picks the nearest matching color with no attempt to
        // halftone or dither. May be used on an arbitrary palette.

        DitherTypeNone          = 0,
        DitherTypeSolid         = 0,

        // Ordered dithers and spiral dithers must be used with a fixed palette or
        // a fixed palette translation.

        // NOTE: DitherOrdered4x4 is unique in that it may apply to 16bpp
        // conversions also.

        DitherTypeOrdered4x4    = 1,

        DitherTypeOrdered8x8    = 2,
        DitherTypeOrdered16x16  = 3,
        DitherTypeSpiral4x4     = 4,
        DitherTypeSpiral8x8     = 5,
        DitherTypeDualSpiral4x4 = 6,
        DitherTypeDualSpiral8x8 = 7,

        // Error diffusion. May be used with any palette.

        DitherTypeErrorDiffusion = 8,
    }


    /// <summary>
    /// WICPaletteType
    /// </summary>
    internal enum WICPaletteType
    {
        /// <summary>
        /// Arbitrary custom palette provided by caller.
        /// </summary>
        WICPaletteTypeCustom           = 0,

        /// <summary>
        /// Optimal palette generated using a median-cut algorithm.
        /// </summary>
        WICPaletteTypeOptimal          = 1,

        /// <summary>
        /// Black and white palette.
        /// </summary>
        WICPaletteTypeFixedBW          = 2,

        // Symmetric halftone palettes.
        // Each of these halftone palettes will be a superset of the system palette.
        // E.g. Halftone8 will have it's 8-color on-off primaries and the 16 system
        // colors added. With duplicates removed, that leaves 16 colors.

        /// <summary>
        /// 8-color, on-off primaries
        /// </summary>
        WICPaletteTypeFixedHalftone8   = 3,

        /// <summary>
        /// 3 intensity levels of each color
        /// </summary>
        WICPaletteTypeFixedHalftone27  = 4,

        /// <summary>
        /// 4 intensity levels of each color
        /// </summary>
        WICPaletteTypeFixedHalftone64  = 5,

        /// <summary>
        /// 5 intensity levels of each color
        /// </summary>
        WICPaletteTypeFixedHalftone125 = 6,

        /// <summary>
        /// 6 intensity levels of each color
        /// </summary>
        WICPaletteTypeFixedHalftone216 = 7,

        /// <summary>
        /// convenient web palette, same as WICPaletteTypeFixedHalftone216
        /// </summary>
        WICPaletteTypeFixedWebPalette  = 7,

        // Assymetric halftone palettes.
        // These are somewhat less useful than the symmetric ones, but are
        // included for completeness. These do not include all of the system
        // colors.

        /// <summary>
        /// 6-red, 7-green, 6-blue intensities
        /// </summary>
        WICPaletteTypeFixedHalftone252 = 8,

        /// <summary>
        /// 8-red, 8-green, 4-blue intensities
        /// </summary>
        WICPaletteTypeFixedHalftone256 = 9,

        /// <summary>
        /// 4 shades of gray
        /// </summary>
        WICPaletteTypeFixedGray4 = 10,

        /// <summary>
        /// 16 shades of gray
        /// </summary>
        WICPaletteTypeFixedGray16 = 11,

        /// <summary>
        /// 256 shades of gray
        /// </summary>
        WICPaletteTypeFixedGray256 = 12
    };

    /// <summary>
    /// Transform options when doing a lossless JPEG image save
    /// </summary>
    /// <ExternalAPI Inherit="true"/>
    internal enum WICBitmapTransformOptions
    {
        /// <summary>
        /// Don't Rotate
        /// </summary>
        WICBitmapTransformRotate0                = 0,
        /// <summary>
        /// Rotate 90 degree clockwise
        /// </summary>
        WICBitmapTransformRotate90            = 0x1,
        /// <summary>
        /// Rotate 180 degree
        /// </summary>
        WICBitmapTransformRotate180           = 0x2,
        /// <summary>
        /// Rotate 270 degree clockwise
        /// </summary>
        WICBitmapTransformRotate270           = 0x3,
        /// <summary>
        /// Flip the image horizontally
        /// </summary>
        WICBitmapTransformFlipHorizontal      = 0x8,
        /// <summary>
        /// Flip the image vertically
        /// </summary>
        WICBitmapTransformFlipVertical        = 0x10
    }
}
