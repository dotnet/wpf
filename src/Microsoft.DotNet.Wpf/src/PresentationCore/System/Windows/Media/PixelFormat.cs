// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.CompilerServices;
using System.Collections.ObjectModel;
using System.ComponentModel;
using MS.Internal;

using UnsafeNativeMethods = MS.Win32.PresentationCore.UnsafeNativeMethods;

namespace System.Windows.Media
{
    [Flags]
    internal enum PixelFormatFlags
    {
        BitsPerPixelMask        = 0x00FF,
        BitsPerPixelUndefined   = 0,
        BitsPerPixel1           = 1,
        BitsPerPixel2           = 2,
        BitsPerPixel4           = 4,
        BitsPerPixel8           = 8,
        BitsPerPixel16          = 16,
        BitsPerPixel24          = 24,
        BitsPerPixel32          = 32,
        BitsPerPixel48          = 48,
        BitsPerPixel64          = 64,
        BitsPerPixel96          = 96,
        BitsPerPixel128         = 128,
        IsGray                  = 0x00000100,   // Grayscale only
        IsCMYK                  = 0x00000200,   // CMYK, not ARGB
        IsSRGB                  = 0x00000400,   // Gamma is approximately 2.2
        IsScRGB                 = 0x00000800,   // Gamma is 1.0
        Premultiplied           = 0x00001000,   // Premultiplied Alpha
        ChannelOrderMask        = 0x0001E000,
        ChannelOrderRGB         = 0x00002000,
        ChannelOrderBGR         = 0x00004000,
        ChannelOrderARGB        = 0x00008000,
        ChannelOrderABGR        = 0x00010000,
        Palettized              = 0x00020000,   // Pixels are indexes into a palette
        NChannelAlpha           = 0x00040000,   // N-Channel format with alpha
        IsNChannel              = 0x00080000,   // N-Channel format
    }

    #region PixelFormat

    /// <summary>
    /// Describes the bit mask and shift for a specific pixelformat
    /// </summary>
    public struct PixelFormatChannelMask
    {
        internal PixelFormatChannelMask(byte[] mask)
        {
            Debug.Assert(mask != null);
            _mask = mask;
        }

        /// <summary>
        /// The bitmask for a color channel
        /// It will never be greater then 0xffffffff
        /// </summary>
        public IList<byte> Mask
        {
            get
            {
                return _mask != null ? new ReadOnlyCollection<byte>((byte[])_mask.Clone()) : null;
            }
        }

        /// <summary>
        /// op_equality - returns whether or not the two pixel format channel masks are equal
        /// </summary>
        public static bool operator == (PixelFormatChannelMask left, PixelFormatChannelMask right)
        {
            return Equals(left, right);
        }

        /// <summary>
        /// Equals - Returns whether or not the two pixel format channel masks are equal
        /// </summary>
        public static bool Equals(PixelFormatChannelMask left, PixelFormatChannelMask right)
        {
            int leftNumChannels  =  left._mask != null ?  left._mask.Length : 0;
            int rightNumChannels = right._mask != null ? right._mask.Length : 0;

            if (leftNumChannels != rightNumChannels)
            {
                return false;
            }

            for (int i = 0; i < leftNumChannels; ++i)
            {
                if (left._mask[i] != right._mask[i])
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// op_inequality - returns whether or not the two pixel format channel masks are not equal
        /// </summary>
        public static bool operator != (PixelFormatChannelMask left, PixelFormatChannelMask right)
        {
            return !(left == right);
        }

        /// <summary>
        /// Equals - Returns whether or not this is equal to the Object
        /// </summary>
        public override bool Equals(Object obj)
        {
            // Can't use "as" since we're looking for a value type
            return obj is PixelFormatChannelMask ? this == (PixelFormatChannelMask)obj : false;
        }

        /// <summary>
        /// GetHashCode - Returns a hash code
        /// </summary>
        public override int GetHashCode()
        {
            int hash = 0;

            if (_mask != null)
            {
                for (int i = 0, count = _mask.Length; i < count; ++i)
                {
                    hash += _mask[i] * 256 * i;
                }
            }

            return hash;
        }

        private byte[] _mask;
    }

    /// <summary>
    /// Pixel Format Definition for images and pixel-based surfaces
    /// </summary>
    [TypeConverter(typeof(PixelFormatConverter))]
    [Serializable]
    public struct PixelFormat : IEquatable<PixelFormat>
    {
        internal PixelFormat(Guid guidPixelFormat)
        {
            unsafe
            {
                Debug.Assert(Unsafe.SizeOf<Guid>() == 16);

                // Compare only the first 15 bytes of the GUID. If the first
                // 15 bytes match the WIC pixel formats, then the 16th byte
                // will be the format enum value.
                Guid guidWicPixelFormat = WICPixelFormatGUIDs.WICPixelFormatDontCare;
                ReadOnlySpan<byte> pGuidPixelFormat = new(&guidPixelFormat, 15);
                ReadOnlySpan<byte> pGuidBuiltIn = new(&guidWicPixelFormat, 15);

                // If it looks like a built-in WIC pixel format, verify that the format enum value is known to us.
                if (pGuidPixelFormat.SequenceEqual(pGuidBuiltIn) && ((byte*)&guidPixelFormat)[15] <= (byte)PixelFormatEnum.Cmyk32)
                {
                    _format = (PixelFormatEnum)((byte*)&guidPixelFormat)[15];
                }
                else
                {
                    _format = PixelFormatEnum.Extended;
                }
            }

            _flags = GetPixelFormatFlagsFromEnum(_format) | GetPixelFormatFlagsFromGuid(guidPixelFormat);
            _bitsPerPixel = GetBitsPerPixelFromEnum(_format);
            _guidFormat = guidPixelFormat;
        }

        internal PixelFormat(PixelFormatEnum format)
        {
            _format = format;

            _flags = GetPixelFormatFlagsFromEnum(format);
            _bitsPerPixel = GetBitsPerPixelFromEnum(format);
            _guidFormat = GetGuidFromFormat(format);
        }

        /// <summary>
        /// Construct a pixel format from a string that represents the format.
        /// The purpose of this method is only for deserialization of PixelFormat.
        /// The preferred way to construct a PixelFormat is with the PixelFormats class.
        /// </summary>
        /// <param name="pixelFormatString"></param>
        internal PixelFormat(string pixelFormatString)
        {
            ArgumentNullException.ThrowIfNull(pixelFormatString);

            _format = pixelFormatString switch
            {
                _ when pixelFormatString.Equals("Default", StringComparison.OrdinalIgnoreCase) => PixelFormatEnum.Default,
                _ when pixelFormatString.Equals("Extended", StringComparison.OrdinalIgnoreCase) => PixelFormatEnum.Extended,
                _ when pixelFormatString.Equals("Indexed1", StringComparison.OrdinalIgnoreCase) => PixelFormatEnum.Indexed1,
                _ when pixelFormatString.Equals("Indexed2", StringComparison.OrdinalIgnoreCase) => PixelFormatEnum.Indexed2,
                _ when pixelFormatString.Equals("Indexed4", StringComparison.OrdinalIgnoreCase) => PixelFormatEnum.Indexed4,
                _ when pixelFormatString.Equals("Indexed8", StringComparison.OrdinalIgnoreCase) => PixelFormatEnum.Indexed8,
                _ when pixelFormatString.Equals("BlackWhite", StringComparison.OrdinalIgnoreCase) => PixelFormatEnum.BlackWhite,
                _ when pixelFormatString.Equals("Gray2", StringComparison.OrdinalIgnoreCase) => PixelFormatEnum.Gray2,
                _ when pixelFormatString.Equals("Gray4", StringComparison.OrdinalIgnoreCase) => PixelFormatEnum.Gray4,
                _ when pixelFormatString.Equals("Gray8", StringComparison.OrdinalIgnoreCase) => PixelFormatEnum.Gray8,
                _ when pixelFormatString.Equals("Bgr555", StringComparison.OrdinalIgnoreCase) => PixelFormatEnum.Bgr555,
                _ when pixelFormatString.Equals("Bgr565", StringComparison.OrdinalIgnoreCase) => PixelFormatEnum.Bgr565,
                _ when pixelFormatString.Equals("Bgr24", StringComparison.OrdinalIgnoreCase) => PixelFormatEnum.Bgr24,
                _ when pixelFormatString.Equals("Rgb24", StringComparison.OrdinalIgnoreCase) => PixelFormatEnum.Rgb24,
                _ when pixelFormatString.Equals("Bgr101010", StringComparison.OrdinalIgnoreCase) => PixelFormatEnum.Bgr101010,
                _ when pixelFormatString.Equals("Bgr32", StringComparison.OrdinalIgnoreCase) => PixelFormatEnum.Bgr32,
                _ when pixelFormatString.Equals("Bgra32", StringComparison.OrdinalIgnoreCase) => PixelFormatEnum.Bgra32,
                _ when pixelFormatString.Equals("Pbgra32", StringComparison.OrdinalIgnoreCase) => PixelFormatEnum.Pbgra32,
                _ when pixelFormatString.Equals("Rgb48", StringComparison.OrdinalIgnoreCase) => PixelFormatEnum.Rgb48,
                _ when pixelFormatString.Equals("Rgba64", StringComparison.OrdinalIgnoreCase) => PixelFormatEnum.Rgba64,
                _ when pixelFormatString.Equals("Prgba64", StringComparison.OrdinalIgnoreCase) => PixelFormatEnum.Prgba64,
                _ when pixelFormatString.Equals("Gray16", StringComparison.OrdinalIgnoreCase) => PixelFormatEnum.Gray16,
                _ when pixelFormatString.Equals("Gray32Float", StringComparison.OrdinalIgnoreCase) => PixelFormatEnum.Gray32Float,
                _ when pixelFormatString.Equals("Rgb128Float", StringComparison.OrdinalIgnoreCase) => PixelFormatEnum.Rgb128Float,
                _ when pixelFormatString.Equals("Rgba128Float", StringComparison.OrdinalIgnoreCase) => PixelFormatEnum.Rgba128Float,
                _ when pixelFormatString.Equals("Prgba128Float", StringComparison.OrdinalIgnoreCase) => PixelFormatEnum.Prgba128Float,
                _ when pixelFormatString.Equals("Cmyk32", StringComparison.OrdinalIgnoreCase) => PixelFormatEnum.Cmyk32,
                _ => throw new ArgumentException(SR.Format(SR.Image_BadPixelFormat, pixelFormatString), nameof(pixelFormatString)),
            };

            _flags = GetPixelFormatFlagsFromEnum(_format);
            _bitsPerPixel = GetBitsPerPixelFromEnum(_format);
            _guidFormat = GetGuidFromFormat(_format);
        }

        private static Guid GetGuidFromFormat(PixelFormatEnum format) => format switch
        {
            PixelFormatEnum.Default => WICPixelFormatGUIDs.WICPixelFormatDontCare,
            PixelFormatEnum.Indexed1 => WICPixelFormatGUIDs.WICPixelFormat1bppIndexed,
            PixelFormatEnum.Indexed2 => WICPixelFormatGUIDs.WICPixelFormat2bppIndexed,
            PixelFormatEnum.Indexed4 => WICPixelFormatGUIDs.WICPixelFormat4bppIndexed,
            PixelFormatEnum.Indexed8 => WICPixelFormatGUIDs.WICPixelFormat8bppIndexed,
            PixelFormatEnum.BlackWhite => WICPixelFormatGUIDs.WICPixelFormatBlackWhite,
            PixelFormatEnum.Gray2 => WICPixelFormatGUIDs.WICPixelFormat2bppGray,
            PixelFormatEnum.Gray4 => WICPixelFormatGUIDs.WICPixelFormat4bppGray,
            PixelFormatEnum.Gray8 => WICPixelFormatGUIDs.WICPixelFormat8bppGray,
            PixelFormatEnum.Bgr555 => WICPixelFormatGUIDs.WICPixelFormat16bppBGR555,
            PixelFormatEnum.Bgr565 => WICPixelFormatGUIDs.WICPixelFormat16bppBGR565,
            PixelFormatEnum.Bgr24 => WICPixelFormatGUIDs.WICPixelFormat24bppBGR,
            PixelFormatEnum.Rgb24 => WICPixelFormatGUIDs.WICPixelFormat24bppRGB,
            PixelFormatEnum.Bgr101010 => WICPixelFormatGUIDs.WICPixelFormat32bppBGR101010,
            PixelFormatEnum.Bgr32 => WICPixelFormatGUIDs.WICPixelFormat32bppBGR,
            PixelFormatEnum.Bgra32 => WICPixelFormatGUIDs.WICPixelFormat32bppBGRA,
            PixelFormatEnum.Pbgra32 => WICPixelFormatGUIDs.WICPixelFormat32bppPBGRA,
            PixelFormatEnum.Rgb48 => WICPixelFormatGUIDs.WICPixelFormat48bppRGB,
            PixelFormatEnum.Rgba64 => WICPixelFormatGUIDs.WICPixelFormat64bppRGBA,
            PixelFormatEnum.Prgba64 => WICPixelFormatGUIDs.WICPixelFormat64bppPRGBA,
            PixelFormatEnum.Gray16 => WICPixelFormatGUIDs.WICPixelFormat16bppGray,
            PixelFormatEnum.Gray32Float => WICPixelFormatGUIDs.WICPixelFormat32bppGrayFloat,
            PixelFormatEnum.Rgb128Float => WICPixelFormatGUIDs.WICPixelFormat128bppRGBFloat,
            PixelFormatEnum.Rgba128Float => WICPixelFormatGUIDs.WICPixelFormat128bppRGBAFloat,
            PixelFormatEnum.Prgba128Float => WICPixelFormatGUIDs.WICPixelFormat128bppPRGBAFloat,
            PixelFormatEnum.Cmyk32 => WICPixelFormatGUIDs.WICPixelFormat32bppCMYK,
            _ => throw new ArgumentException(SR.Format(SR.Image_BadPixelFormat, format), nameof(format))
        };

        private readonly PixelFormatFlags FormatFlags
        {
            get
            {
                return _flags;
            }
        }

        /// <summary>
        /// op_equality - returns whether or not the two pixel formats are equal
        /// </summary>
        public static bool operator ==(PixelFormat left, PixelFormat right)
        {
            return left.Guid == right.Guid;
        }

        /// <summary>
        /// op_inequality - returns whether or not the two pixel formats are not equal
        /// </summary>
        public static bool operator !=(PixelFormat left, PixelFormat right)
        {
            return left.Guid != right.Guid;
        }

        /// <summary>
        /// Equals - Returns whether or not the two pixel formats are equal
        /// </summary>
        public static bool Equals(PixelFormat left, PixelFormat right)
        {
            return left.Guid == right.Guid;
        }

        /// <summary>
        /// Equals - Returns whether or not this is equal to the PixelFormat
        /// </summary>
        public readonly bool Equals(PixelFormat pixelFormat)
        {
            return this == pixelFormat;
        }

        /// <summary>
        /// Equals - Returns whether or not this is equal to the Object
        /// </summary>
        public override readonly bool Equals(object obj)
        {
            return obj is PixelFormat pixelFormat && Equals(pixelFormat);
        }

        /// <summary>
        /// GetHashCode - Returns a hash code
        /// </summary>
        public override readonly int GetHashCode()
        {
            return Guid.GetHashCode();
        }

        /// <summary>
        /// The number of bits per pixel for this format.
        /// </summary>
        public int BitsPerPixel
        {
            get
            {
                return InternalBitsPerPixel;
            }
        }

        /// <summary>
        /// The pixel format mask information for each channel.
        /// </summary>
        public readonly IList<PixelFormatChannelMask> Masks
        {
            get
            {
                IntPtr pixelFormatInfo = CreatePixelFormatInfo();
                Debug.Assert(pixelFormatInfo != IntPtr.Zero);

                PixelFormatChannelMask[] masks;

                try
                {
                    HRESULT.Check(UnsafeNativeMethods.WICPixelFormatInfo.GetChannelCount(
                        pixelFormatInfo,
                        out UInt32 channelCount
                        ));

                    Debug.Assert(channelCount >= 1);

                    masks = new PixelFormatChannelMask[channelCount];

                    unsafe
                    {
                        for (uint i = 0; i < channelCount; i++)
                        {
                            HRESULT.Check(UnsafeNativeMethods.WICPixelFormatInfo.GetChannelMask(
                                pixelFormatInfo, i, 0, null, out UInt32 cbBytes));

                            Debug.Assert(cbBytes > 0);

                            byte[] channelMask = new byte[cbBytes];

                            fixed (byte* pbChannelMask = channelMask)
                            {
                                HRESULT.Check(UnsafeNativeMethods.WICPixelFormatInfo.GetChannelMask(
                                    pixelFormatInfo, i, cbBytes, pbChannelMask, out cbBytes));

                                Debug.Assert(cbBytes == channelMask.Length);
                            }

                            masks[i] = new PixelFormatChannelMask(channelMask);
                        }
                    }
                }
                finally
                {
                    if (pixelFormatInfo != IntPtr.Zero)
                    {
                        UnsafeNativeMethods.MILUnknown.ReleaseInterface(ref pixelFormatInfo);
                    }
                }

                return new ReadOnlyCollection<PixelFormatChannelMask>(masks);
            }
        }

        internal readonly IntPtr CreatePixelFormatInfo()
        {
            IntPtr componentInfo = IntPtr.Zero;
            IntPtr pixelFormatInfo = IntPtr.Zero;

            using (FactoryMaker myFactory = new FactoryMaker())
            {
                try
                {
                    Guid guidPixelFormat = _guidFormat;

                    int hr = UnsafeNativeMethods.WICImagingFactory.CreateComponentInfo(
                        myFactory.ImagingFactoryPtr,
                        ref guidPixelFormat,
                        out componentInfo);
                    if (hr == (int)WinCodecErrors.WINCODEC_ERR_COMPONENTINITIALIZEFAILURE ||
                        hr == (int)WinCodecErrors.WINCODEC_ERR_COMPONENTNOTFOUND)
                    {
                        throw new NotSupportedException(SR.Image_NoPixelFormatFound);
                    }
                    HRESULT.Check(hr);

                    Guid guidPixelFormatInfo = MILGuidData.IID_IWICPixelFormatInfo;
                    HRESULT.Check(UnsafeNativeMethods.MILUnknown.QueryInterface(
                        componentInfo,
                        ref guidPixelFormatInfo,
                        out pixelFormatInfo));
                }
                finally
                {
                    if (componentInfo != IntPtr.Zero)
                    {
                        UnsafeNativeMethods.MILUnknown.ReleaseInterface(ref componentInfo);
                    }
                }
            }

            return pixelFormatInfo;
        }

        internal int InternalBitsPerPixel
        {
            get
            {
                if (_bitsPerPixel == 0)
                {
                    UInt32 bpp = 0;

                    IntPtr pixelFormatInfo = CreatePixelFormatInfo();
                    Debug.Assert(pixelFormatInfo != IntPtr.Zero);

                    try
                    {
                        HRESULT.Check(UnsafeNativeMethods.WICPixelFormatInfo.GetBitsPerPixel(
                            pixelFormatInfo,
                            out bpp
                            ));
                    }
                    finally
                    {
                        if (pixelFormatInfo != IntPtr.Zero)
                        {
                            UnsafeNativeMethods.MILUnknown.ReleaseInterface(ref pixelFormatInfo);
                        }
                    }

                    _bitsPerPixel = bpp;
                }

                return (int)_bitsPerPixel;
            }
        }

        internal readonly bool HasAlpha
        {
            get
            {
                return (FormatFlags & PixelFormatFlags.ChannelOrderABGR) != 0 ||
                       (FormatFlags & PixelFormatFlags.ChannelOrderARGB) != 0 ||
                       (FormatFlags & PixelFormatFlags.NChannelAlpha) != 0;
            }
        }

        internal readonly bool Palettized
        {
            get
            {
                return (FormatFlags & PixelFormatFlags.Palettized) != 0;
            }
        }

        internal readonly PixelFormatEnum Format
        {
            get
            {
                return _format;
            }
        }

        internal readonly Guid Guid
        {
            get
            {
                return _guidFormat;
            }
        }

        /// <summary>
        /// Convert a PixelFormat to a string that represents it.
        /// </summary>
        /// <returns></returns>
        public override readonly string ToString()
        {
            return _format.ToString();
        }

        internal static PixelFormat GetPixelFormat(SafeMILHandle /* IWICBitmapSource */ bitmapSource)
        {
            Guid guidPixelFormat = WICPixelFormatGUIDs.WICPixelFormatDontCare;

            HRESULT.Check(UnsafeNativeMethods.WICBitmapSource.GetPixelFormat(bitmapSource, out guidPixelFormat));

            return new PixelFormat(guidPixelFormat);
        }

        /// <summary>
        /// Convert from the internal guid to the actual PixelFormat value.
        /// </summary>
        /// <param name="pixelFormatGuid"></param>
        /// <returns></returns>
        internal static PixelFormat GetPixelFormat(Guid pixelFormatGuid)
        {
            Span<byte> guidBytes = stackalloc byte[16];
            pixelFormatGuid.TryWriteBytes(guidBytes);

            return GetPixelFormat((PixelFormatEnum)guidBytes[15]);
        }

        /// <summary>
        /// Convert from the internal enum to the actual PixelFormat value.
        /// </summary>
        /// <param name="pixelFormatEnum"></param>
        /// <returns></returns>
        internal static PixelFormat GetPixelFormat(PixelFormatEnum pixelFormatEnum) => pixelFormatEnum switch
        {
            PixelFormatEnum.Indexed1 => PixelFormats.Indexed1,
            PixelFormatEnum.Indexed2 => PixelFormats.Indexed2,
            PixelFormatEnum.Indexed4 => PixelFormats.Indexed4,
            PixelFormatEnum.Indexed8 => PixelFormats.Indexed8,
            PixelFormatEnum.BlackWhite => PixelFormats.BlackWhite,
            PixelFormatEnum.Gray2 => PixelFormats.Gray2,
            PixelFormatEnum.Gray4 => PixelFormats.Gray4,
            PixelFormatEnum.Gray8 => PixelFormats.Gray8,
            PixelFormatEnum.Bgr555 => PixelFormats.Bgr555,
            PixelFormatEnum.Bgr565 => PixelFormats.Bgr565,
            PixelFormatEnum.Bgr101010 => PixelFormats.Bgr101010,
            PixelFormatEnum.Bgr24 => PixelFormats.Bgr24,
            PixelFormatEnum.Rgb24 => PixelFormats.Rgb24,
            PixelFormatEnum.Bgr32 => PixelFormats.Bgr32,
            PixelFormatEnum.Bgra32 => PixelFormats.Bgra32,
            PixelFormatEnum.Pbgra32 => PixelFormats.Pbgra32,
            PixelFormatEnum.Rgb48 => PixelFormats.Rgb48,
            PixelFormatEnum.Rgba64 => PixelFormats.Rgba64,
            PixelFormatEnum.Prgba64 => PixelFormats.Prgba64,
            PixelFormatEnum.Gray16 => PixelFormats.Gray16,
            PixelFormatEnum.Gray32Float => PixelFormats.Gray32Float,
            PixelFormatEnum.Rgb128Float => PixelFormats.Rgb128Float,
            PixelFormatEnum.Rgba128Float => PixelFormats.Rgba128Float,
            PixelFormatEnum.Prgba128Float => PixelFormats.Prgba128Float,
            PixelFormatEnum.Cmyk32 => PixelFormats.Cmyk32,
            _ => PixelFormats.Default,
        };

        private static PixelFormatFlags GetPixelFormatFlagsFromGuid(Guid pixelFormatGuid)
        {
            PixelFormatFlags result = PixelFormatFlags.BitsPerPixelUndefined;

            if (pixelFormatGuid.CompareTo(WICPixelFormatPhotonFirst) >= 0 && pixelFormatGuid.CompareTo(WICPixelFormatPhotonLast) <= 0)
            {
                Span<byte> guidBytes = stackalloc byte[16];
                pixelFormatGuid.TryWriteBytes(guidBytes);

                result = guidBytes[15] switch
                {
                    // GUID_WICPixelFormat64bppRGBAFixedPoint
                    0x1D => PixelFormatFlags.ChannelOrderARGB | PixelFormatFlags.IsScRGB,
                    // GUID_WICPixelFormat128bppRGBAFixedPoint
                    0x1E => PixelFormatFlags.ChannelOrderARGB | PixelFormatFlags.IsScRGB,
                    // GUID_WICPixelFormat64bppCMYK
                    0x1F => PixelFormatFlags.IsCMYK,
                    // GUID_WICPixelFormat24bpp3Channels
                    0x20 => PixelFormatFlags.IsNChannel,
                    // GUID_WICPixelFormat32bpp4Channels
                    0x21 => PixelFormatFlags.IsNChannel,
                    // GUID_WICPixelFormat40bpp5Channels
                    0x22 => PixelFormatFlags.IsNChannel,
                    // GUID_WICPixelFormat48bpp6Channels
                    0x23 => PixelFormatFlags.IsNChannel,
                    // GUID_WICPixelFormat56bpp7Channels
                    0x24 => PixelFormatFlags.IsNChannel,
                    // GUID_WICPixelFormat64bpp8Channels
                    0x25 => PixelFormatFlags.IsNChannel,
                    // GUID_WICPixelFormat48bpp3Channels
                    0x26 => PixelFormatFlags.IsNChannel,
                    // GUID_WICPixelFormat64bpp4Channels
                    0x27 => PixelFormatFlags.IsNChannel,
                    // GUID_WICPixelFormat80bpp5Channels
                    0x28 => PixelFormatFlags.IsNChannel,
                    // GUID_WICPixelFormat96bpp6Channels
                    0x29 => PixelFormatFlags.IsNChannel,
                    // GUID_WICPixelFormat112bpp7Channels
                    0x2A => PixelFormatFlags.IsNChannel,
                    // GUID_WICPixelFormat128bpp8Channels
                    0x2B => PixelFormatFlags.IsNChannel,
                    // GUID_WICPixelFormat40bppCMYKAlpha
                    0x2C => PixelFormatFlags.IsCMYK | PixelFormatFlags.NChannelAlpha,
                    // GUID_WICPixelFormat80bppCMYKAlpha
                    0x2D => PixelFormatFlags.IsCMYK | PixelFormatFlags.NChannelAlpha,
                    // GUID_WICPixelFormat32bpp3ChannelsAlpha
                    0x2E => PixelFormatFlags.IsNChannel | PixelFormatFlags.NChannelAlpha,
                    // GUID_WICPixelFormat40bpp4ChannelsAlpha
                    0x2F => PixelFormatFlags.IsNChannel | PixelFormatFlags.NChannelAlpha,
                    // GUID_WICPixelFormat48bpp5ChannelsAlpha
                    0x30 => PixelFormatFlags.IsNChannel | PixelFormatFlags.NChannelAlpha,
                    // GUID_WICPixelFormat56bpp6ChannelsAlpha
                    0x31 => PixelFormatFlags.IsNChannel | PixelFormatFlags.NChannelAlpha,
                    // GUID_WICPixelFormat64bpp7ChannelsAlpha
                    0x32 => PixelFormatFlags.IsNChannel | PixelFormatFlags.NChannelAlpha,
                    // GUID_WICPixelFormat72bpp8ChannelsAlpha
                    0x33 => PixelFormatFlags.IsNChannel | PixelFormatFlags.NChannelAlpha,
                    // GUID_WICPixelFormat64bpp3ChannelsAlpha
                    0x34 => PixelFormatFlags.IsNChannel | PixelFormatFlags.NChannelAlpha,
                    // GUID_WICPixelFormat80bpp4ChannelsAlpha
                    0x35 => PixelFormatFlags.IsNChannel | PixelFormatFlags.NChannelAlpha,
                    // GUID_WICPixelFormat96bpp5ChannelsAlpha
                    0x36 => PixelFormatFlags.IsNChannel | PixelFormatFlags.NChannelAlpha,
                    // GUID_WICPixelFormat112bpp6ChannelsAlpha
                    0x37 => PixelFormatFlags.IsNChannel | PixelFormatFlags.NChannelAlpha,
                    // GUID_WICPixelFormat128bpp7ChannelsAlpha
                    0x38 => PixelFormatFlags.IsNChannel | PixelFormatFlags.NChannelAlpha,
                    // GUID_WICPixelFormat144bpp8ChannelsAlpha
                    0x39 => PixelFormatFlags.IsNChannel | PixelFormatFlags.NChannelAlpha,
                    // GUID_WICPixelFormat64bppRGBAHalf
                    0x3A => PixelFormatFlags.ChannelOrderARGB | PixelFormatFlags.IsScRGB,
                    // GUID_WICPixelFormat48bppRGBHalf
                    0x3B => PixelFormatFlags.ChannelOrderRGB | PixelFormatFlags.IsScRGB,
                    // GUID_WICPixelFormat32bppRGBE
                    0x3D => PixelFormatFlags.ChannelOrderRGB | PixelFormatFlags.IsScRGB,
                    // GUID_WICPixelFormat16bppGrayHalf
                    0x3E => PixelFormatFlags.IsGray | PixelFormatFlags.IsScRGB,
                    // GUID_WICPixelFormat32bppGrayFixedPoint
                    0x3F => PixelFormatFlags.IsGray | PixelFormatFlags.IsScRGB,
                    // GUID_WICPixelFormat64bppRGBFixedPoint
                    0x40 => PixelFormatFlags.IsScRGB | PixelFormatFlags.ChannelOrderRGB,
                    // GUID_WICPixelFormat128bppRGBFixedPoint
                    0x41 => PixelFormatFlags.IsScRGB | PixelFormatFlags.ChannelOrderRGB,
                    // GUID_WICPixelFormat64bppRGBHalf
                    0x42 => PixelFormatFlags.IsScRGB | PixelFormatFlags.ChannelOrderRGB,
                    _ => PixelFormatFlags.BitsPerPixelUndefined
                };
            }

            return result;
        }

        private static PixelFormatFlags GetPixelFormatFlagsFromEnum(PixelFormatEnum pixelFormatEnum) => pixelFormatEnum switch
        {
            PixelFormatEnum.Default => PixelFormatFlags.BitsPerPixelUndefined,
            PixelFormatEnum.Indexed1 => PixelFormatFlags.BitsPerPixel1 | PixelFormatFlags.Palettized,
            PixelFormatEnum.Indexed2 => PixelFormatFlags.BitsPerPixel2 | PixelFormatFlags.Palettized,
            PixelFormatEnum.Indexed4 => PixelFormatFlags.BitsPerPixel4 | PixelFormatFlags.Palettized,
            PixelFormatEnum.Indexed8 => PixelFormatFlags.BitsPerPixel8 | PixelFormatFlags.Palettized,
            PixelFormatEnum.BlackWhite => PixelFormatFlags.BitsPerPixel1 | PixelFormatFlags.IsGray,
            PixelFormatEnum.Gray2 => PixelFormatFlags.BitsPerPixel2 | PixelFormatFlags.IsGray,
            PixelFormatEnum.Gray4 => PixelFormatFlags.BitsPerPixel4 | PixelFormatFlags.IsGray,
            PixelFormatEnum.Gray8 => PixelFormatFlags.BitsPerPixel8 | PixelFormatFlags.IsGray,
            PixelFormatEnum.Bgr555 => PixelFormatFlags.BitsPerPixel16 | PixelFormatFlags.IsSRGB | PixelFormatFlags.ChannelOrderBGR,
            PixelFormatEnum.Bgr565 => PixelFormatFlags.BitsPerPixel16 | PixelFormatFlags.IsSRGB | PixelFormatFlags.ChannelOrderBGR,
            PixelFormatEnum.Bgr101010 => PixelFormatFlags.BitsPerPixel32 | PixelFormatFlags.IsSRGB | PixelFormatFlags.ChannelOrderBGR,
            PixelFormatEnum.Bgr24 => PixelFormatFlags.BitsPerPixel24 | PixelFormatFlags.IsSRGB | PixelFormatFlags.ChannelOrderBGR,
            PixelFormatEnum.Rgb24 => PixelFormatFlags.BitsPerPixel24 | PixelFormatFlags.IsSRGB | PixelFormatFlags.ChannelOrderRGB,
            PixelFormatEnum.Bgr32 => PixelFormatFlags.BitsPerPixel32 | PixelFormatFlags.IsSRGB | PixelFormatFlags.ChannelOrderBGR,
            PixelFormatEnum.Bgra32 => PixelFormatFlags.BitsPerPixel32 | PixelFormatFlags.IsSRGB | PixelFormatFlags.ChannelOrderABGR,
            PixelFormatEnum.Pbgra32 => PixelFormatFlags.BitsPerPixel32 | PixelFormatFlags.IsSRGB | PixelFormatFlags.Premultiplied | PixelFormatFlags.ChannelOrderABGR,
            PixelFormatEnum.Rgb48 => PixelFormatFlags.BitsPerPixel48 | PixelFormatFlags.IsSRGB | PixelFormatFlags.ChannelOrderRGB,
            PixelFormatEnum.Rgba64 => PixelFormatFlags.BitsPerPixel64 | PixelFormatFlags.IsSRGB | PixelFormatFlags.ChannelOrderARGB,
            PixelFormatEnum.Prgba64 => PixelFormatFlags.BitsPerPixel64 | PixelFormatFlags.IsSRGB | PixelFormatFlags.Premultiplied | PixelFormatFlags.ChannelOrderARGB,
            PixelFormatEnum.Gray16 => PixelFormatFlags.BitsPerPixel16 | PixelFormatFlags.IsSRGB | PixelFormatFlags.IsGray,
            PixelFormatEnum.Gray32Float => PixelFormatFlags.BitsPerPixel32 | PixelFormatFlags.IsScRGB | PixelFormatFlags.IsGray,
            PixelFormatEnum.Rgb128Float => PixelFormatFlags.BitsPerPixel128 | PixelFormatFlags.IsScRGB | PixelFormatFlags.ChannelOrderRGB,
            PixelFormatEnum.Rgba128Float => PixelFormatFlags.BitsPerPixel128 | PixelFormatFlags.IsScRGB | PixelFormatFlags.ChannelOrderARGB,
            PixelFormatEnum.Prgba128Float => PixelFormatFlags.BitsPerPixel128 | PixelFormatFlags.IsScRGB | PixelFormatFlags.Premultiplied | PixelFormatFlags.ChannelOrderARGB,
            PixelFormatEnum.Cmyk32 => PixelFormatFlags.BitsPerPixel32 | PixelFormatFlags.IsCMYK,
            // 3rd party pixel format -- we don't expose anything about it.
            _ => PixelFormatFlags.BitsPerPixelUndefined,
        };

        private static UInt32 GetBitsPerPixelFromEnum(PixelFormatEnum pixelFormatEnum) => pixelFormatEnum switch
        {
            PixelFormatEnum.Default => 0,
            PixelFormatEnum.Indexed1 => 1,
            PixelFormatEnum.Indexed2 => 2,
            PixelFormatEnum.Indexed4 => 4,
            PixelFormatEnum.Indexed8 => 8,
            PixelFormatEnum.BlackWhite => 1,
            PixelFormatEnum.Gray2 => 2,
            PixelFormatEnum.Gray4 => 4,
            PixelFormatEnum.Gray8 => 8,
            PixelFormatEnum.Bgr555 or PixelFormatEnum.Bgr565 => 16,
            PixelFormatEnum.Bgr101010 => 32,
            PixelFormatEnum.Bgr24 or PixelFormatEnum.Rgb24 => 24,
            PixelFormatEnum.Bgr32 or PixelFormatEnum.Bgra32 or PixelFormatEnum.Pbgra32 => 32,
            PixelFormatEnum.Rgb48 => 48,
            PixelFormatEnum.Rgba64 or PixelFormatEnum.Prgba64 => 64,
            PixelFormatEnum.Gray16 => 16,
            PixelFormatEnum.Gray32Float => 32,
            PixelFormatEnum.Rgb128Float or PixelFormatEnum.Rgba128Float or PixelFormatEnum.Prgba128Float => 128,
            PixelFormatEnum.Cmyk32 => 32,
            // 3rd party pixel format -- we don't expose anything about it.
            _ => 0,
        };

        [NonSerialized]
        private readonly PixelFormatFlags _flags;

        [NonSerialized]
        private readonly PixelFormatEnum _format;

        [NonSerialized]
        private UInt32 _bitsPerPixel;

        [NonSerialized]
        private readonly Guid _guidFormat;

        [NonSerialized]
        private static readonly Guid WICPixelFormatPhotonFirst = new Guid(0x6fddc324, 0x4e03, 0x4bfe, 0xb1, 0x85, 0x3d, 0x77, 0x76, 0x8d, 0xc9, 0x1d);

        [NonSerialized]
        private static readonly Guid WICPixelFormatPhotonLast  = new Guid(0x6fddc324, 0x4e03, 0x4bfe, 0xb1, 0x85, 0x3d, 0x77, 0x76, 0x8d, 0xc9, 0x42);
    }
    #endregion // PixelFormat
}
