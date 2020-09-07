// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
//
#pragma warning disable 1634, 1691 // Allow suppression of certain presharp messages

using System;
using System.Security;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.Design.Serialization;
using System.Reflection;
using MS.Internal;
using MS.Win32;
using System.Diagnostics;
using System.Windows.Media;
using System.Globalization;
using System.Runtime.InteropServices;

using SR=MS.Internal.PresentationCore.SR;
using SRID=MS.Internal.PresentationCore.SRID;
using UnsafeNativeMethods=MS.Win32.PresentationCore.UnsafeNativeMethods;

namespace System.Windows.Media
{
    [System.Flags]
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
                return _mask != null ? new PartialList<byte>((byte[])_mask.Clone()) : null;
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
    [TypeConverter (typeof(PixelFormatConverter))]
    [Serializable]
    public struct PixelFormat : IEquatable<PixelFormat>
    {
        internal PixelFormat(Guid guidPixelFormat)
        {
            unsafe
            {
                Guid guidWicPixelFormat = WICPixelFormatGUIDs.WICPixelFormatDontCare;
                byte * pGuidPixelFormat = (byte*) &guidPixelFormat;
                byte * pGuidBuiltIn = (byte*) &guidWicPixelFormat;

                // Compare only the first 15 bytes of the GUID.  If the first
                // 15 bytes match the WIC pixel formats, then the 16th byte
                // will be the format enum value.
                Debug.Assert(Marshal.SizeOf(typeof(Guid)) == 16);
                int compareCount = 15;
                
                bool fBuiltIn = true;
                for (int i = 0; i < compareCount; ++i)
                {
                    if (pGuidPixelFormat[i] != pGuidBuiltIn[i])
                    {
                        fBuiltIn = false;
                        break;
                    }
                }
                
                // If it looks like a built-in WIC pixel format, verify that
                // the format enum value is known to us.
                if (fBuiltIn && pGuidPixelFormat[compareCount] <= (byte)PixelFormatEnum.Cmyk32)
                {
                    _format = (PixelFormatEnum) pGuidPixelFormat[compareCount];
                }
                else
                {
                    _format = PixelFormatEnum.Extended;
                }
            }

            _flags = GetPixelFormatFlagsFromEnum(_format) | GetPixelFormatFlagsFromGuid(guidPixelFormat);
            _bitsPerPixel = GetBitsPerPixelFromEnum(_format);
            _guidFormat = new SecurityCriticalDataForSet<Guid> (guidPixelFormat);
        }

        internal PixelFormat(PixelFormatEnum format)
        {
            _format = format;

            _flags = GetPixelFormatFlagsFromEnum(format);
            _bitsPerPixel = GetBitsPerPixelFromEnum(format);
            _guidFormat = new SecurityCriticalDataForSet<Guid> (PixelFormat.GetGuidFromFormat(format));
        }

        /// <summary>
        /// Construct a pixel format from a string that represents the format.
        /// The purpose of this method is only for deserialization of PixelFormat.
        /// The preferred way to construct a PixelFormat is with the PixelFormats class.
        /// </summary>
        /// <param name="pixelFormatString"></param>
        internal PixelFormat(string pixelFormatString)
        {
            PixelFormatEnum format = PixelFormatEnum.Default;

            if (pixelFormatString == null)
            {
                throw new System.ArgumentNullException("pixelFormatString");
            }

            string upperPixelFormatString = pixelFormatString.ToUpper(System.Globalization.CultureInfo.InvariantCulture);

            switch (upperPixelFormatString)
            {
                case "DEFAULT":
                    format = PixelFormatEnum.Default;
                    break;

                case "EXTENDED":
                    format = PixelFormatEnum.Extended;
                    break;

                case "INDEXED1":
                    format = PixelFormatEnum.Indexed1;
                    break;

                case "INDEXED2":
                    format = PixelFormatEnum.Indexed2;
                    break;

                case "INDEXED4":
                    format = PixelFormatEnum.Indexed4;
                    break;

                case "INDEXED8":
                    format = PixelFormatEnum.Indexed8;
                    break;

                case "BLACKWHITE":
                    format = PixelFormatEnum.BlackWhite;
                    break;

                case "GRAY2":
                    format = PixelFormatEnum.Gray2;
                    break;

                case "GRAY4":
                    format = PixelFormatEnum.Gray4;
                    break;

                case "GRAY8":
                    format = PixelFormatEnum.Gray8;
                    break;

                case "BGR555":
                    format = PixelFormatEnum.Bgr555;
                    break;

                case "BGR565":
                    format = PixelFormatEnum.Bgr565;
                    break;

                case "BGR24":
                    format = PixelFormatEnum.Bgr24;
                    break;

                case "RGB24":
                    format = PixelFormatEnum.Rgb24;
                    break;

                case "BGR101010":
                    format = PixelFormatEnum.Bgr101010;
                    break;

                case "BGR32":
                    format = PixelFormatEnum.Bgr32;
                    break;

                case "BGRA32":
                    format = PixelFormatEnum.Bgra32;
                    break;

                case "PBGRA32":
                    format = PixelFormatEnum.Pbgra32;
                    break;

                case "RGB48":
                    format = PixelFormatEnum.Rgb48;
                    break;

                case "RGBA64":
                    format = PixelFormatEnum.Rgba64;
                    break;

                case "PRGBA64":
                    format = PixelFormatEnum.Prgba64;
                    break;

                case "GRAY16":
                    format = PixelFormatEnum.Gray16;
                    break;

                case "GRAY32FLOAT":
                    format = PixelFormatEnum.Gray32Float;
                    break;

                case "RGB128FLOAT":
                    format = PixelFormatEnum.Rgb128Float;
                    break;

                case "RGBA128FLOAT":
                    format = PixelFormatEnum.Rgba128Float;
                    break;

                case "PRGBA128FLOAT":
                    format = PixelFormatEnum.Prgba128Float;
                    break;

                case "CMYK32":
                    format = PixelFormatEnum.Cmyk32;
                    break;

                default:
                    throw new System.ArgumentException (SR.Get(SRID.Image_BadPixelFormat, pixelFormatString),
                            "pixelFormatString");
            }

            _format = format;

            _flags = GetPixelFormatFlagsFromEnum(format);
            _bitsPerPixel = GetBitsPerPixelFromEnum(format);
            _guidFormat = new SecurityCriticalDataForSet<Guid> (PixelFormat.GetGuidFromFormat(format));
        }

        static private Guid GetGuidFromFormat(PixelFormatEnum format)
        {
            switch (format)
            {
                case PixelFormatEnum.Default:
                    return WICPixelFormatGUIDs.WICPixelFormatDontCare;

                case PixelFormatEnum.Indexed1:
                    return WICPixelFormatGUIDs.WICPixelFormat1bppIndexed;

                case PixelFormatEnum.Indexed2:
                    return WICPixelFormatGUIDs.WICPixelFormat2bppIndexed;

                case PixelFormatEnum.Indexed4:
                    return WICPixelFormatGUIDs.WICPixelFormat4bppIndexed;

                case PixelFormatEnum.Indexed8:
                    return WICPixelFormatGUIDs.WICPixelFormat8bppIndexed;

                case PixelFormatEnum.BlackWhite:
                    return WICPixelFormatGUIDs.WICPixelFormatBlackWhite;

                case PixelFormatEnum.Gray2:
                    return WICPixelFormatGUIDs.WICPixelFormat2bppGray;

                case PixelFormatEnum.Gray4:
                    return WICPixelFormatGUIDs.WICPixelFormat4bppGray;

                case PixelFormatEnum.Gray8:
                    return WICPixelFormatGUIDs.WICPixelFormat8bppGray;

                case PixelFormatEnum.Bgr555:
                    return WICPixelFormatGUIDs.WICPixelFormat16bppBGR555;

                case PixelFormatEnum.Bgr565:
                    return WICPixelFormatGUIDs.WICPixelFormat16bppBGR565;

                case PixelFormatEnum.Bgr24:
                    return WICPixelFormatGUIDs.WICPixelFormat24bppBGR;

                case PixelFormatEnum.Rgb24:
                    return WICPixelFormatGUIDs.WICPixelFormat24bppRGB;

                case PixelFormatEnum.Bgr101010:
                    return WICPixelFormatGUIDs.WICPixelFormat32bppBGR101010;

                case PixelFormatEnum.Bgr32:
                    return WICPixelFormatGUIDs.WICPixelFormat32bppBGR;

                case PixelFormatEnum.Bgra32:
                    return WICPixelFormatGUIDs.WICPixelFormat32bppBGRA;

                case PixelFormatEnum.Pbgra32:
                    return WICPixelFormatGUIDs.WICPixelFormat32bppPBGRA;

                case PixelFormatEnum.Rgb48:
                    return WICPixelFormatGUIDs.WICPixelFormat48bppRGB;

                case PixelFormatEnum.Rgba64:
                    return WICPixelFormatGUIDs.WICPixelFormat64bppRGBA;

                case PixelFormatEnum.Prgba64:
                    return WICPixelFormatGUIDs.WICPixelFormat64bppPRGBA;

                case PixelFormatEnum.Gray16:
                    return WICPixelFormatGUIDs.WICPixelFormat16bppGray;

                case PixelFormatEnum.Gray32Float:
                    return WICPixelFormatGUIDs.WICPixelFormat32bppGrayFloat;

                case PixelFormatEnum.Rgb128Float:
                    return WICPixelFormatGUIDs.WICPixelFormat128bppRGBFloat;

                case PixelFormatEnum.Rgba128Float:
                    return WICPixelFormatGUIDs.WICPixelFormat128bppRGBAFloat;

                case PixelFormatEnum.Prgba128Float:
                    return WICPixelFormatGUIDs.WICPixelFormat128bppPRGBAFloat;

                case PixelFormatEnum.Cmyk32:
                    return WICPixelFormatGUIDs.WICPixelFormat32bppCMYK;
            }

            throw new System.ArgumentException (SR.Get(SRID.Image_BadPixelFormat, format), "format");
        }

        private PixelFormatFlags FormatFlags
        {
            get
            {
                return _flags;
            }
        }

        /// <summary>
        /// op_equality - returns whether or not the two pixel formats are equal
        /// </summary>
        public static bool operator == (PixelFormat left, PixelFormat right)
        {
            return (left.Guid == right.Guid);
        }

        /// <summary>
        /// op_inequality - returns whether or not the two pixel formats are not equal
        /// </summary>
        public static bool operator != (PixelFormat left, PixelFormat right)
        {
            return (left.Guid != right.Guid);
        }

        /// <summary>
        /// Equals - Returns whether or not the two pixel formats are equal
        /// </summary>
        public static bool Equals(PixelFormat left, PixelFormat right)
        {
            return (left.Guid == right.Guid);
        }

        /// <summary>
        /// Equals - Returns whether or not this is equal to the PixelFormat
        /// </summary>
        public bool Equals(PixelFormat pixelFormat)
        {
            return this == pixelFormat;
        }

        /// <summary>
        /// Equals - Returns whether or not this is equal to the Object
        /// </summary>
        public override bool Equals(Object obj)
        {
            if ((null == obj) ||
                !(obj is PixelFormat))
            {
                return false;
            }

            return this == (PixelFormat)obj;
        }

        /// <summary>
        /// GetHashCode - Returns a hash code
        /// </summary>
        public override int GetHashCode()
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
        public IList<PixelFormatChannelMask> Masks
        {
            get
            {
                IntPtr pixelFormatInfo = CreatePixelFormatInfo();
                Debug.Assert(pixelFormatInfo != IntPtr.Zero);

                UInt32 channelCount = 0;
                PixelFormatChannelMask[] masks = null;
                UInt32 cbBytes = 0;

                try
                {
                    HRESULT.Check(UnsafeNativeMethods.WICPixelFormatInfo.GetChannelCount(
                        pixelFormatInfo,
                        out channelCount
                        ));

                    Debug.Assert(channelCount >= 1);

                    masks = new PixelFormatChannelMask[channelCount];

                    unsafe
                    {
                        for (uint i = 0; i < channelCount; i++)
                        {
                            HRESULT.Check(UnsafeNativeMethods.WICPixelFormatInfo.GetChannelMask(
                                pixelFormatInfo, i, 0, null, out cbBytes));

                            Debug.Assert(cbBytes > 0);

                            byte[] channelMask = new byte[cbBytes];

                            fixed (byte *pbChannelMask = channelMask)
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

                return new PartialList<PixelFormatChannelMask>(masks);
            }
        }

        internal IntPtr CreatePixelFormatInfo()
        {
            IntPtr componentInfo = IntPtr.Zero;
            IntPtr pixelFormatInfo = IntPtr.Zero;

            using (FactoryMaker myFactory = new FactoryMaker())
            {
                try
                {
                    Guid guidPixelFormat = this.Guid;

                    int hr = UnsafeNativeMethods.WICImagingFactory.CreateComponentInfo(
                        myFactory.ImagingFactoryPtr,
                        ref guidPixelFormat,
                        out componentInfo);
                    if (hr == (int)WinCodecErrors.WINCODEC_ERR_COMPONENTINITIALIZEFAILURE ||
                        hr == (int)WinCodecErrors.WINCODEC_ERR_COMPONENTNOTFOUND)
                    {
                        throw new System.NotSupportedException(SR.Get(SRID.Image_NoPixelFormatFound));
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

                return (int) _bitsPerPixel;
            }
        }

        internal bool HasAlpha
        {
            get
            {
                return ((FormatFlags & PixelFormatFlags.ChannelOrderABGR) != 0 ||
                            (FormatFlags & PixelFormatFlags.ChannelOrderARGB) != 0 ||
                            (FormatFlags & PixelFormatFlags.NChannelAlpha) != 0);
            }
        }

        internal bool Palettized
        {
            get
            {
                return ((FormatFlags & PixelFormatFlags.Palettized) != 0);
            }
        }

        internal PixelFormatEnum Format
        {
            get
            {
                return _format;
            }
        }

        internal Guid Guid
        {
            get
            {
                return _guidFormat.Value;
            }
        }

        /// <summary>
        /// Convert a PixelFormat to a string that represents it.
        /// </summary>
        /// <returns></returns>
        public override string ToString ()
        {
            return _format.ToString();
        }

        internal static PixelFormat GetPixelFormat (
            SafeMILHandle /* IWICBitmapSource */ bitmapSource
            )
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
            byte[] guidBytes = pixelFormatGuid.ToByteArray();
            return GetPixelFormat( (PixelFormatEnum)guidBytes[guidBytes.Length-1] );
        }

        /// <summary>
        /// Convert from the internal enum to the actual PixelFormat value.
        /// </summary>
        /// <param name="pixelFormatEnum"></param>
        /// <returns></returns>
        internal static PixelFormat GetPixelFormat(PixelFormatEnum pixelFormatEnum)
        {
            switch (pixelFormatEnum)
            {
                case PixelFormatEnum.Indexed1:
                    return PixelFormats.Indexed1;

                case PixelFormatEnum.Indexed2:
                    return PixelFormats.Indexed2;

                case PixelFormatEnum.Indexed4:
                    return PixelFormats.Indexed4;

                case PixelFormatEnum.Indexed8:
                    return PixelFormats.Indexed8;

                case PixelFormatEnum.BlackWhite:
                    return PixelFormats.BlackWhite;

                case PixelFormatEnum.Gray2:
                    return PixelFormats.Gray2;

                case PixelFormatEnum.Gray4:
                    return PixelFormats.Gray4;

                case PixelFormatEnum.Gray8:
                    return PixelFormats.Gray8;

                case PixelFormatEnum.Bgr555:
                    return PixelFormats.Bgr555;

                case PixelFormatEnum.Bgr565:
                    return PixelFormats.Bgr565;

                case PixelFormatEnum.Bgr101010:
                    return PixelFormats.Bgr101010;

                case PixelFormatEnum.Bgr24:
                    return PixelFormats.Bgr24;

                case PixelFormatEnum.Rgb24:
                    return PixelFormats.Rgb24;

                case PixelFormatEnum.Bgr32:
                    return PixelFormats.Bgr32;

                case PixelFormatEnum.Bgra32:
                    return PixelFormats.Bgra32;

                case PixelFormatEnum.Pbgra32:
                    return PixelFormats.Pbgra32;

                case PixelFormatEnum.Rgb48:
                    return PixelFormats.Rgb48;

                case PixelFormatEnum.Rgba64:
                    return PixelFormats.Rgba64;

                case PixelFormatEnum.Prgba64:
                    return PixelFormats.Prgba64;

                case PixelFormatEnum.Gray16:
                    return PixelFormats.Gray16;

                case PixelFormatEnum.Gray32Float:
                    return PixelFormats.Gray32Float;

                case PixelFormatEnum.Rgb128Float:
                    return PixelFormats.Rgb128Float;

                case PixelFormatEnum.Rgba128Float:
                    return PixelFormats.Rgba128Float;

                case PixelFormatEnum.Prgba128Float:
                    return PixelFormats.Prgba128Float;

                case PixelFormatEnum.Cmyk32:
                    return PixelFormats.Cmyk32;
            }

            return PixelFormats.Default;
        }

        static private PixelFormatFlags GetPixelFormatFlagsFromGuid(Guid pixelFormatGuid)
        {
            PixelFormatFlags result = PixelFormatFlags.BitsPerPixelUndefined;

            if (pixelFormatGuid.CompareTo(WICPixelFormatPhotonFirst) >= 0 &&
                            pixelFormatGuid.CompareTo(WICPixelFormatPhotonLast) <= 0)
            {
                byte[] b = pixelFormatGuid.ToByteArray();

                switch (b[15])
                {
                    case 0x1D:  // GUID_WICPixelFormat64bppRGBAFixedPoint
                        result = PixelFormatFlags.ChannelOrderARGB | PixelFormatFlags.IsScRGB;
                        break;
                    case 0x1E:  // GUID_WICPixelFormat128bppRGBAFixedPoint
                        result = PixelFormatFlags.ChannelOrderARGB | PixelFormatFlags.IsScRGB;
                        break;
                    case 0x1F:  // GUID_WICPixelFormat64bppCMYK
                        result = PixelFormatFlags.IsCMYK;
                        break;
                    case 0x20:  // GUID_WICPixelFormat24bpp3Channels
                        result = PixelFormatFlags.IsNChannel;
                        break;
                    case 0x21:  // GUID_WICPixelFormat32bpp4Channels
                        result = PixelFormatFlags.IsNChannel;
                        break;
                    case 0x22:  // GUID_WICPixelFormat40bpp5Channels
                        result = PixelFormatFlags.IsNChannel;
                        break;
                    case 0x23:  // GUID_WICPixelFormat48bpp6Channels
                        result = PixelFormatFlags.IsNChannel;
                        break;
                    case 0x24:  // GUID_WICPixelFormat56bpp7Channels
                        result = PixelFormatFlags.IsNChannel;
                        break;
                    case 0x25:  // GUID_WICPixelFormat64bpp8Channels
                        result = PixelFormatFlags.IsNChannel;
                        break;
                    case 0x26:  // GUID_WICPixelFormat48bpp3Channels
                        result = PixelFormatFlags.IsNChannel;
                        break;
                    case 0x27:  // GUID_WICPixelFormat64bpp4Channels
                        result = PixelFormatFlags.IsNChannel;
                        break;
                    case 0x28:  // GUID_WICPixelFormat80bpp5Channels
                        result = PixelFormatFlags.IsNChannel;
                        break;
                    case 0x29:  // GUID_WICPixelFormat96bpp6Channels
                        result = PixelFormatFlags.IsNChannel;
                        break;
                    case 0x2A:  // GUID_WICPixelFormat112bpp7Channels
                        result = PixelFormatFlags.IsNChannel;
                        break;
                    case 0x2B:  // GUID_WICPixelFormat128bpp8Channels
                        result = PixelFormatFlags.IsNChannel;
                        break;
                    case 0x2C:  // GUID_WICPixelFormat40bppCMYKAlpha
                        result = PixelFormatFlags.IsCMYK | PixelFormatFlags.NChannelAlpha;
                        break;
                    case 0x2D:  // GUID_WICPixelFormat80bppCMYKAlpha
                        result = PixelFormatFlags.IsCMYK | PixelFormatFlags.NChannelAlpha;
                        break;
                    case 0x2E:  // GUID_WICPixelFormat32bpp3ChannelsAlpha
                        result = PixelFormatFlags.IsNChannel | PixelFormatFlags.NChannelAlpha;
                        break;
                    case 0x2F:  // GUID_WICPixelFormat40bpp4ChannelsAlpha
                        result = PixelFormatFlags.IsNChannel | PixelFormatFlags.NChannelAlpha;
                        break;
                    case 0x30:  // GUID_WICPixelFormat48bpp5ChannelsAlpha
                        result = PixelFormatFlags.IsNChannel | PixelFormatFlags.NChannelAlpha;
                        break;
                    case 0x31:  // GUID_WICPixelFormat56bpp6ChannelsAlpha
                        result = PixelFormatFlags.IsNChannel | PixelFormatFlags.NChannelAlpha;
                        break;
                    case 0x32:  // GUID_WICPixelFormat64bpp7ChannelsAlpha
                        result = PixelFormatFlags.IsNChannel | PixelFormatFlags.NChannelAlpha;
                        break;
                    case 0x33:  // GUID_WICPixelFormat72bpp8ChannelsAlpha
                        result = PixelFormatFlags.IsNChannel | PixelFormatFlags.NChannelAlpha;
                        break;
                    case 0x34:  // GUID_WICPixelFormat64bpp3ChannelsAlpha
                        result = PixelFormatFlags.IsNChannel | PixelFormatFlags.NChannelAlpha;
                        break;
                    case 0x35:  // GUID_WICPixelFormat80bpp4ChannelsAlpha
                        result = PixelFormatFlags.IsNChannel | PixelFormatFlags.NChannelAlpha;
                        break;
                    case 0x36:  // GUID_WICPixelFormat96bpp5ChannelsAlpha
                        result = PixelFormatFlags.IsNChannel | PixelFormatFlags.NChannelAlpha;
                        break;
                    case 0x37:  // GUID_WICPixelFormat112bpp6ChannelsAlpha
                        result = PixelFormatFlags.IsNChannel | PixelFormatFlags.NChannelAlpha;
                        break;
                    case 0x38:  // GUID_WICPixelFormat128bpp7ChannelsAlpha
                        result = PixelFormatFlags.IsNChannel | PixelFormatFlags.NChannelAlpha;
                        break;
                    case 0x39:  // GUID_WICPixelFormat144bpp8ChannelsAlpha
                        result = PixelFormatFlags.IsNChannel | PixelFormatFlags.NChannelAlpha;
                        break;
                    case 0x3A:  // GUID_WICPixelFormat64bppRGBAHalf
                        result = PixelFormatFlags.ChannelOrderARGB | PixelFormatFlags.IsScRGB;
                        break;
                    case 0x3B:  // GUID_WICPixelFormat48bppRGBHalf
                        result = PixelFormatFlags.ChannelOrderRGB | PixelFormatFlags.IsScRGB;
                        break;
                    case 0x3D:  // GUID_WICPixelFormat32bppRGBE
                        result = PixelFormatFlags.ChannelOrderRGB | PixelFormatFlags.IsScRGB;
                        break;
                    case 0x3E:  // GUID_WICPixelFormat16bppGrayHalf
                        result = PixelFormatFlags.IsGray | PixelFormatFlags.IsScRGB;
                        break;
                    case 0x3F:  // GUID_WICPixelFormat32bppGrayFixedPoint
                        result = PixelFormatFlags.IsGray | PixelFormatFlags.IsScRGB;
                        break;
                    case 0x40:  // GUID_WICPixelFormat64bppRGBFixedPoint
                        result = PixelFormatFlags.IsScRGB | PixelFormatFlags.ChannelOrderRGB;
                        break;
                    case 0x41:  // GUID_WICPixelFormat128bppRGBFixedPoint
                        result = PixelFormatFlags.IsScRGB | PixelFormatFlags.ChannelOrderRGB;
                        break;
                    case 0x42:  // GUID_WICPixelFormat64bppRGBHalf
                        result = PixelFormatFlags.IsScRGB | PixelFormatFlags.ChannelOrderRGB;
                        break;
                }
            }

            return result;
        }

        static private PixelFormatFlags GetPixelFormatFlagsFromEnum(PixelFormatEnum pixelFormatEnum)
        {
            switch (pixelFormatEnum)
            {
                case PixelFormatEnum.Default:
                    return PixelFormatFlags.BitsPerPixelUndefined;

                case PixelFormatEnum.Indexed1:
                    return PixelFormatFlags.BitsPerPixel1 | PixelFormatFlags.Palettized;

                case PixelFormatEnum.Indexed2:
                    return PixelFormatFlags.BitsPerPixel2 | PixelFormatFlags.Palettized;

                case PixelFormatEnum.Indexed4:
                    return PixelFormatFlags.BitsPerPixel4 | PixelFormatFlags.Palettized;

                case PixelFormatEnum.Indexed8:
                    return PixelFormatFlags.BitsPerPixel8 | PixelFormatFlags.Palettized;

                case PixelFormatEnum.BlackWhite:
                    return PixelFormatFlags.BitsPerPixel1 | PixelFormatFlags.IsGray;

                case PixelFormatEnum.Gray2:
                    return PixelFormatFlags.BitsPerPixel2 | PixelFormatFlags.IsGray;

                case PixelFormatEnum.Gray4:
                    return PixelFormatFlags.BitsPerPixel4 | PixelFormatFlags.IsGray;

                case PixelFormatEnum.Gray8:
                    return PixelFormatFlags.BitsPerPixel8 | PixelFormatFlags.IsGray;

                case PixelFormatEnum.Bgr555:
                    return PixelFormatFlags.BitsPerPixel16 | PixelFormatFlags.IsSRGB | PixelFormatFlags.ChannelOrderBGR;

                case PixelFormatEnum.Bgr565:
                    return PixelFormatFlags.BitsPerPixel16 | PixelFormatFlags.IsSRGB | PixelFormatFlags.ChannelOrderBGR;

                case PixelFormatEnum.Bgr101010:
                    return PixelFormatFlags.BitsPerPixel32 | PixelFormatFlags.IsSRGB | PixelFormatFlags.ChannelOrderBGR;

                case PixelFormatEnum.Bgr24:
                    return PixelFormatFlags.BitsPerPixel24 | PixelFormatFlags.IsSRGB | PixelFormatFlags.ChannelOrderBGR;

                case PixelFormatEnum.Rgb24:
                    return PixelFormatFlags.BitsPerPixel24 | PixelFormatFlags.IsSRGB | PixelFormatFlags.ChannelOrderRGB;

                case PixelFormatEnum.Bgr32:
                    return PixelFormatFlags.BitsPerPixel32 | PixelFormatFlags.IsSRGB | PixelFormatFlags.ChannelOrderBGR;

                case PixelFormatEnum.Bgra32:
                    return PixelFormatFlags.BitsPerPixel32 | PixelFormatFlags.IsSRGB | PixelFormatFlags.ChannelOrderABGR;

                case PixelFormatEnum.Pbgra32:
                    return PixelFormatFlags.BitsPerPixel32 | PixelFormatFlags.IsSRGB | PixelFormatFlags.Premultiplied | PixelFormatFlags.ChannelOrderABGR;

                case PixelFormatEnum.Rgb48:
                    return PixelFormatFlags.BitsPerPixel48 | PixelFormatFlags.IsSRGB | PixelFormatFlags.ChannelOrderRGB;

                case PixelFormatEnum.Rgba64:
                    return PixelFormatFlags.BitsPerPixel64 | PixelFormatFlags.IsSRGB | PixelFormatFlags.ChannelOrderARGB;

                case PixelFormatEnum.Prgba64:
                    return PixelFormatFlags.BitsPerPixel64 | PixelFormatFlags.IsSRGB | PixelFormatFlags.Premultiplied | PixelFormatFlags.ChannelOrderARGB;

                case PixelFormatEnum.Gray16:
                    return PixelFormatFlags.BitsPerPixel16 | PixelFormatFlags.IsSRGB | PixelFormatFlags.IsGray;

                case PixelFormatEnum.Gray32Float:
                    return PixelFormatFlags.BitsPerPixel32 | PixelFormatFlags.IsScRGB | PixelFormatFlags.IsGray;

                case PixelFormatEnum.Rgb128Float:
                    return PixelFormatFlags.BitsPerPixel128 | PixelFormatFlags.IsScRGB | PixelFormatFlags.ChannelOrderRGB;

                case PixelFormatEnum.Rgba128Float:
                    return PixelFormatFlags.BitsPerPixel128 | PixelFormatFlags.IsScRGB | PixelFormatFlags.ChannelOrderARGB;

                case PixelFormatEnum.Prgba128Float:
                    return PixelFormatFlags.BitsPerPixel128 | PixelFormatFlags.IsScRGB | PixelFormatFlags.Premultiplied | PixelFormatFlags.ChannelOrderARGB;

                case PixelFormatEnum.Cmyk32:
                    return PixelFormatFlags.BitsPerPixel32 | PixelFormatFlags.IsCMYK;
            }

            // 3rd party pixel format -- we don't expose anything about it.
            return PixelFormatFlags.BitsPerPixelUndefined;
        }

        static private UInt32 GetBitsPerPixelFromEnum(PixelFormatEnum pixelFormatEnum)
        {
            switch (pixelFormatEnum)
            {
                case PixelFormatEnum.Default:
                    return 0;

                case PixelFormatEnum.Indexed1:
                    return 1;

                case PixelFormatEnum.Indexed2:
                    return 2;

                case PixelFormatEnum.Indexed4:
                    return 4;

                case PixelFormatEnum.Indexed8:
                    return 8;

                case PixelFormatEnum.BlackWhite:
                    return 1;

                case PixelFormatEnum.Gray2:
                    return 2;

                case PixelFormatEnum.Gray4:
                    return 4;

                case PixelFormatEnum.Gray8:
                    return 8;

                case PixelFormatEnum.Bgr555:
                case PixelFormatEnum.Bgr565:
                    return 16;

                case PixelFormatEnum.Bgr101010:
                    return 32;

                case PixelFormatEnum.Bgr24:
                case PixelFormatEnum.Rgb24:
                    return 24;

                case PixelFormatEnum.Bgr32:
                case PixelFormatEnum.Bgra32:
                case PixelFormatEnum.Pbgra32:
                    return 32;

                case PixelFormatEnum.Rgb48:
                    return 48;

                case PixelFormatEnum.Rgba64:
                case PixelFormatEnum.Prgba64:
                    return 64;

                case PixelFormatEnum.Gray16:
                    return 16;

                case PixelFormatEnum.Gray32Float:
                    return 32;

                case PixelFormatEnum.Rgb128Float:
                case PixelFormatEnum.Rgba128Float:
                case PixelFormatEnum.Prgba128Float:
                    return 128;

                case PixelFormatEnum.Cmyk32:
                    return 32;
            }

            // 3rd party pixel format -- we don't expose anything about it.
            return 0;
        }

        [NonSerialized]
        private PixelFormatFlags _flags;

        [NonSerialized]
        private PixelFormatEnum _format;

        [NonSerialized]
        private UInt32 _bitsPerPixel;

        [NonSerialized]
        private SecurityCriticalDataForSet<Guid> _guidFormat;

        [NonSerialized]
        private static readonly Guid WICPixelFormatPhotonFirst = new Guid(0x6fddc324, 0x4e03, 0x4bfe, 0xb1, 0x85, 0x3d, 0x77, 0x76, 0x8d, 0xc9, 0x1d);

        [NonSerialized]
        private static readonly Guid WICPixelFormatPhotonLast  = new Guid(0x6fddc324, 0x4e03, 0x4bfe, 0xb1, 0x85, 0x3d, 0x77, 0x76, 0x8d, 0xc9, 0x42);
    }
    #endregion // PixelFormat
}
