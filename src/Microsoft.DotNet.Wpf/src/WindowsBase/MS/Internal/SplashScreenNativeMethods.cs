// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.



using System;
using System.Runtime.ConstrainedExecution;
using System.Runtime.InteropServices;
using System.Security;
using System.Windows;
using MS.Internal.WindowsBase;

namespace MS.Win32
{
    internal sealed partial class UnsafeNativeMethods
    {
        internal class WIC
        {
            #region Constants
            internal const int WINCODEC_SDK_VERSION = 0x0236;
            internal static readonly Guid WICPixelFormat32bppPBGRA = new Guid(0x6fddc324, 0x4e03, 0x4bfe, 0xb1, 0x85, 0x3d, 0x77, 0x76, 0x8d, 0xc9, 0x10);
            #endregion

            [DllImport(DllImport.WindowsCodecs, EntryPoint = "WICCreateImagingFactory_Proxy")]
            internal static extern int CreateImagingFactory(
                UInt32 SDKVersion,
                out IntPtr ppICodecFactory);

            [DllImport(DllImport.WindowsCodecs, EntryPoint = "IWICImagingFactory_CreateStream_Proxy")]
            internal static extern int /* HRESULT */ CreateStream(
                IntPtr pICodecFactory,
                out IntPtr /* IWICBitmapStream */ ppIStream);

            [DllImport(DllImport.WindowsCodecs, EntryPoint = "IWICStream_InitializeFromMemory_Proxy")]
            internal static extern int /*HRESULT*/ InitializeStreamFromMemory(
                IntPtr pIWICStream,
                IntPtr pbBuffer,
                uint cbSize);

            [DllImport(DllImport.WindowsCodecs, EntryPoint = "IWICImagingFactory_CreateDecoderFromStream_Proxy")]
            internal static extern int /*HRESULT*/ CreateDecoderFromStream(
                IntPtr pICodecFactory,
                IntPtr /* IStream */ pIStream,
                ref Guid guidVendor,
                UInt32 metadataFlags,
                out IntPtr /* IWICBitmapDecoder */ ppIDecode);

            [DllImport(DllImport.WindowsCodecs, EntryPoint = "IWICBitmapDecoder_GetFrame_Proxy")]
            internal static extern int /* HRESULT */ GetFrame(
                IntPtr /* IWICBitmapDecoder */ THIS_PTR,
                UInt32 index,
                out IntPtr /* IWICBitmapFrameDecode */ ppIFrameDecode);

            [DllImport(DllImport.WindowsCodecs, EntryPoint = "IWICImagingFactory_CreateFormatConverter_Proxy")]
            internal static extern int /* HRESULT */ CreateFormatConverter(
                IntPtr pICodecFactory,
                out IntPtr /* IWICFormatConverter */ ppFormatConverter);

            [DllImport(DllImport.WindowsCodecs, EntryPoint = "IWICFormatConverter_Initialize_Proxy")]
            internal static extern int /* HRESULT */ InitializeFormatConverter(
                IntPtr /* IWICFormatConverter */ THIS_PTR,
                IntPtr /* IWICBitmapSource */ source,
                ref Guid dstFormat,
                int dither,
                IntPtr /* IWICBitmapPalette */ bitmapPalette,
                double alphaThreshold,
                WICPaletteType paletteTranslate);

            [DllImport(DllImport.WindowsCodecs, EntryPoint = "IWICImagingFactory_CreateBitmapFlipRotator_Proxy")]
            internal static extern int /* HRESULT */ CreateBitmapFlipRotator(
                IntPtr pICodecFactory,
                out IntPtr /* IWICBitmapFlipRotator */ ppBitmapFlipRotator);
            
            [DllImport(DllImport.WindowsCodecs, EntryPoint = "IWICBitmapFlipRotator_Initialize_Proxy")]
            internal static extern int /* HRESULT */ InitializeBitmapFlipRotator(
                IntPtr /* IWICBitmapFlipRotator */ THIS_PTR,
                IntPtr /* IWICBitmapSource */ source,
                WICBitmapTransformOptions options);

            [DllImport(DllImport.WindowsCodecs, EntryPoint = "IWICBitmapSource_GetSize_Proxy")]
            internal static extern int /* HRESULT */ GetBitmapSize(
                IntPtr /* IWICBitmapSource */ THIS_PTR,
                out Int32 puiWidth,
                out Int32 puiHeight);

            [DllImport(DllImport.WindowsCodecs, EntryPoint = "IWICBitmapSource_CopyPixels_Proxy")]
            internal static extern int /* HRESULT */ CopyPixels(
                IntPtr /* IWICBitmapSource */ THIS_PTR,
                ref Int32Rect prc,
                Int32 cbStride,
                Int32 cbBufferSize,
                IntPtr /* BYTE* */ pvPixels);


            #region enums

            internal enum WICBitmapTransformOptions
            {
                WICBitmapTransformRotate0 = 0,
                WICBitmapTransformRotate90 = 0x1,
                WICBitmapTransformRotate180 = 0x2,
                WICBitmapTransformRotate270 = 0x3,
                WICBitmapTransformFlipHorizontal = 0x8,
                WICBitmapTransformFlipVertical = 0x10
            }

            internal enum WICPaletteType
            {
                WICPaletteTypeCustom = 0,
                WICPaletteTypeOptimal = 1,
                WICPaletteTypeFixedBW = 2,
                WICPaletteTypeFixedHalftone8 = 3,
                WICPaletteTypeFixedHalftone27 = 4,
                WICPaletteTypeFixedHalftone64 = 5,
                WICPaletteTypeFixedHalftone125 = 6,
                WICPaletteTypeFixedHalftone216 = 7,
                WICPaletteTypeFixedWebPalette = 7,
                WICPaletteTypeFixedHalftone252 = 8,
                WICPaletteTypeFixedHalftone256 = 9,
                WICPaletteTypeFixedGray4 = 10,
                WICPaletteTypeFixedGray16 = 11,
                WICPaletteTypeFixedGray256 = 12
            };

            #endregion
        }

        internal class HRESULT
        {
            public static void Check(int hr)
            {
                if (hr >= 0)
                {
                    return;
                }
                else
                {
                    // PresentationCore (wgx_render.cs) has a more complete system
                    // for converting hresults to exceptions for MIL and windows codecs
                    // but for splash screen we don't want to take a dependency on core.
                    Marshal.ThrowExceptionForHR(hr, (IntPtr)(-1));
                }
            }
        }
    }
}
