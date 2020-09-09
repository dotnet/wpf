// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
//  ABOUT THIS FILE:
//   -- This file contains native methods which are deemed NOT SAFE in the sense that any usage of them
//      must be carefully reviewed.   FXCop will flag callers of these for review.
//   -- These methods DO have the SuppressUnmanagedCodeSecurity attribute which means stalk walks for unmanaged
//      code will stop with the immediate caler.
//   -- Put methods in here when a stack walk is innappropriate due to performance concerns

using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using System.Runtime.ConstrainedExecution;
using System;
using MS.Internal;
using MS.Internal.PresentationCore;
using System.Security;
using System.Collections;
using System.IO;
using System.Text;
using System.Windows.Media.Composition;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows;
using MS.Win32;
using IComDataObject = System.Runtime.InteropServices.ComTypes.IDataObject;

using DllImport = MS.Internal.PresentationCore.DllImport;

#pragma warning disable 1634, 1691  // suppressing PreSharp warnings

namespace MS.Win32.PresentationCore
{
    internal static partial class UnsafeNativeMethods
    {
        internal static class MilCoreApi
        {
            [DllImport(DllImport.MilCore, EntryPoint = "MilCompositionEngine_EnterCompositionEngineLock")]
            internal static extern void EnterCompositionEngineLock();

            [DllImport(DllImport.MilCore, EntryPoint = "MilCompositionEngine_ExitCompositionEngineLock")]
            internal static extern void ExitCompositionEngineLock();

            [DllImport(DllImport.MilCore, EntryPoint = "MilCompositionEngine_EnterMediaSystemLock")]
            internal static extern void EnterMediaSystemLock();

            [DllImport(DllImport.MilCore, EntryPoint = "MilCompositionEngine_ExitMediaSystemLock")]
            internal static extern void ExitMediaSystemLock();

            [DllImport(DllImport.MilCore)]
            internal static extern int MilVersionCheck(
                uint uiCallerMilSdkVersion
             );
 
            [DllImport(DllImport.MilCore)]
            internal static extern bool WgxConnection_ShouldForceSoftwareForGraphicsStreamClient();

            [DllImport(DllImport.MilCore)]
            internal static extern int WgxConnection_Create(
            	bool requestSynchronousTransport,
				out IntPtr ppConnection);

            [DllImport(DllImport.MilCore)]
            internal static extern int WgxConnection_Disconnect(IntPtr pTranspManager);

            [DllImport(DllImport.MilCore)]
            internal extern static int /* HRESULT */ MILCreateStreamFromStreamDescriptor(ref System.Windows.Media.StreamDescriptor pSD, out IntPtr ppStream);

            [DllImport(DllImport.MilCore)]
            unsafe internal static extern void MilUtility_GetTileBrushMapping(
                D3DMATRIX* transform,
                D3DMATRIX* relativeTransform,
                Stretch stretch,
                AlignmentX alignmentX,
                AlignmentY alignmentY,
                BrushMappingMode viewPortUnits,
                BrushMappingMode viewBoxUnits,
                Rect* shapeFillBounds,
                Rect* contentBounds,
                ref Rect viewport,
                ref Rect viewbox,
                out D3DMATRIX contentToShape,
                out int brushIsEmpty
                );

            [DllImport(DllImport.MilCore)]
            internal unsafe static extern int MilUtility_PathGeometryBounds(
                MIL_PEN_DATA *pPenData,
                double *pDashArray,
                MilMatrix3x2D* pWorldMatrix,
                FillRule fillRule,
                byte* pPathData,
                UInt32 nSize,
                MilMatrix3x2D* pGeometryMatrix,
                double rTolerance,
                bool fRelative,
                bool fSkipHollows,
                MilRectD* pBounds);

            [DllImport(DllImport.MilCore)]
            internal unsafe static extern int MilUtility_PathGeometryCombine(
                MilMatrix3x2D* pMatrix,
                MilMatrix3x2D* pMatrix1,
                FillRule fillRule1,
                byte* pPathData1,
                UInt32 nSize1,
                MilMatrix3x2D* pMatrix2,
                FillRule fillRule2,
                byte* pPathData2,
                UInt32 nSize2,
                double rTolerance,
                bool fRelative,
                Delegate addFigureCallback,
                GeometryCombineMode combineMode,
                out FillRule resultFillRule);

            [DllImport(DllImport.MilCore)]
            internal unsafe static extern int MilUtility_PathGeometryWiden(
                MIL_PEN_DATA *pPenData,
                double *pDashArray,
                MilMatrix3x2D* pMatrix,
                FillRule fillRule,
                byte* pPathData,
                UInt32 nSize,
                double rTolerance,
                bool fRelative,
                Delegate addFigureCallback,
                out FillRule widenedFillRule);

            [DllImport(DllImport.MilCore)]
            internal unsafe static extern int MilUtility_PathGeometryOutline(
                MilMatrix3x2D* pMatrix,
                FillRule fillRule,
                byte* pPathData,
                UInt32 nSize,
                double rTolerance,
                bool fRelative,
                Delegate addFigureCallback,
                out FillRule outlinedFillRule);

            [DllImport(DllImport.MilCore)]
            internal unsafe static extern int MilUtility_PathGeometryFlatten(
                MilMatrix3x2D* pMatrix,
                FillRule fillRule,
                byte* pPathData,
                UInt32 nSize,
                double rTolerance,
                bool fRelative,
                Delegate addFigureCallback,
                out FillRule resultFillRule);

            [DllImport(DllImport.MilCore)]
            internal unsafe static extern int /* HRESULT */ MilGlyphCache_SetCreateGlyphBitmapsCallback(
                MulticastDelegate del
                );

            [DllImport(DllImport.MilCore)]
            internal unsafe static extern int MilGlyphCache_BeginCommandAtRenderTime(
                IntPtr pMilSlaveGlyphCacheTarget,
                byte* pbData,
                uint cbSize,
                uint cbExtra
                );

            [DllImport(DllImport.MilCore)]
            internal unsafe static extern int MilGlyphCache_AppendCommandDataAtRenderTime(
                IntPtr pMilSlaveGlyphCacheTarget,
                byte* pbData,
                uint cbSize
                );

            [DllImport(DllImport.MilCore)]
            internal unsafe static extern int MilGlyphCache_EndCommandAtRenderTime(
                IntPtr pMilSlaveGlyphCacheTarget
                );

            [DllImport(DllImport.MilCore)]
            internal unsafe static extern int MilGlyphRun_SetGeometryAtRenderTime(
                IntPtr pMilGlyphRunTarget,
                byte* pCmd,
                uint cbCmd
                );

            [DllImport(DllImport.MilCore)]
            internal unsafe static extern int MilGlyphRun_GetGlyphOutline(
                IntPtr pFontFace,
                ushort glyphIndex, 
                bool sideways, 
                double renderingEmSize,
                out byte* pPathGeometryData,
                out UInt32 pSize,
                out FillRule pFillRule
                );

            [DllImport(DllImport.MilCore)]
            internal unsafe static extern int MilGlyphRun_ReleasePathGeometryData(
                byte* pPathGeometryData
                );

            [DllImport(DllImport.MilCore, EntryPoint = "MilCreateReversePInvokeWrapper")]
            internal unsafe static extern /*HRESULT*/ int MilCreateReversePInvokeWrapper(
                IntPtr pFcn, 
                out IntPtr reversePInvokeWrapper);

            [DllImport(DllImport.MilCore, EntryPoint = "MilReleasePInvokePtrBlocking")]
            internal unsafe static extern void MilReleasePInvokePtrBlocking(
                IntPtr reversePInvokeWrapper);

            [DllImport(DllImport.MilCore, EntryPoint = "RenderOptions_ForceSoftwareRenderingModeForProcess")]
            internal unsafe static extern void RenderOptions_ForceSoftwareRenderingModeForProcess(
                bool fForce);

            [DllImport(DllImport.MilCore, EntryPoint = "RenderOptions_IsSoftwareRenderingForcedForProcess")]
            internal unsafe static extern bool RenderOptions_IsSoftwareRenderingForcedForProcess();            

            [DllImport(DllImport.MilCore, EntryPoint = "MilResource_CreateCWICWrapperBitmap")]
            internal unsafe static extern int /* HRESULT */ CreateCWICWrapperBitmap(
                BitmapSourceSafeMILHandle /* IWICBitmapSource */ pIWICBitmapSource,
                out BitmapSourceSafeMILHandle /* CWICWrapperBitmap as IWICBitmapSource */ pCWICWrapperBitmap);
        }

        internal static class WICComponentInfo
        {
            [DllImport(DllImport.WindowsCodecs, EntryPoint = "IWICComponentInfo_GetCLSID_Proxy")]
            internal static extern int /* HRESULT */ GetCLSID(
                System.Windows.Media.SafeMILHandle /* IWICComponentInfo */ THIS_PTR,
                out Guid pclsid);

            [DllImport(DllImport.WindowsCodecs, EntryPoint = "IWICComponentInfo_GetAuthor_Proxy")]
            internal static extern int /* HRESULT */ GetAuthor(
                System.Windows.Media.SafeMILHandle /* IWICComponentInfo */ THIS_PTR,
                UInt32 cchAuthor,
                [Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder wzAuthor,
                out UInt32 pcchActual);

            [DllImport(DllImport.WindowsCodecs, EntryPoint = "IWICComponentInfo_GetVersion_Proxy")]
            internal static extern int /* HRESULT */ GetVersion(
                System.Windows.Media.SafeMILHandle /* IWICComponentInfo */ THIS_PTR,
                UInt32 cchVersion,
                [Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder wzVersion,
                out UInt32 pcchActual);

            [DllImport(DllImport.WindowsCodecs, EntryPoint = "IWICComponentInfo_GetSpecVersion_Proxy")]
            internal static extern int /* HRESULT */ GetSpecVersion(
                System.Windows.Media.SafeMILHandle /* IWICComponentInfo */ THIS_PTR,
                UInt32 cchSpecVersion,
                [Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder wzSpecVersion,
                out UInt32 pcchActual);

            [DllImport(DllImport.WindowsCodecs, EntryPoint = "IWICComponentInfo_GetFriendlyName_Proxy")]
            internal static extern int /* HRESULT */ GetFriendlyName(
                System.Windows.Media.SafeMILHandle /* IWICComponentInfo */ THIS_PTR,
                UInt32 cchFriendlyName,
                [Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder wzFriendlyName,
                out UInt32 pcchActual);
        }

        internal static class WICBitmapCodecInfo
        {
            [DllImport(DllImport.WindowsCodecs, EntryPoint = "IWICBitmapCodecInfo_GetContainerFormat_Proxy")]
            internal static extern int /* HRESULT */ GetContainerFormat(
                System.Windows.Media.SafeMILHandle /* IWICBitmapCodecInfo */ THIS_PTR,
                out Guid pguidContainerFormat);

            [DllImport(DllImport.WindowsCodecs, EntryPoint = "IWICBitmapCodecInfo_GetDeviceManufacturer_Proxy")]
            internal static extern int /* HRESULT */ GetDeviceManufacturer(
                System.Windows.Media.SafeMILHandle /* IWICBitmapCodecInfo */ THIS_PTR,
                UInt32 cchDeviceManufacturer,
                [Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder wzDeviceManufacturer,
                out UInt32 pcchActual
                );

            [DllImport(DllImport.WindowsCodecs, EntryPoint = "IWICBitmapCodecInfo_GetDeviceModels_Proxy")]
            internal static extern int /* HRESULT */ GetDeviceModels(
                System.Windows.Media.SafeMILHandle /* IWICBitmapCodecInfo */ THIS_PTR,
                UInt32 cchDeviceModels,
                [Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder wzDeviceModels,
                out UInt32 pcchActual
                );

            [DllImport(DllImport.WindowsCodecs, EntryPoint = "IWICBitmapCodecInfo_GetMimeTypes_Proxy")]
            internal static extern int /* HRESULT */ GetMimeTypes(
                System.Windows.Media.SafeMILHandle /* IWICBitmapCodecInfo */ THIS_PTR,
                UInt32 cchMimeTypes,
                [Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder wzMimeTypes,
                out UInt32 pcchActual
                );

            [DllImport(DllImport.WindowsCodecs, EntryPoint = "IWICBitmapCodecInfo_GetFileExtensions_Proxy")]
            internal static extern int /* HRESULT */ GetFileExtensions(
                System.Windows.Media.SafeMILHandle /* IWICBitmapCodecInfo */ THIS_PTR,
                UInt32 cchFileExtensions,
                [Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder wzFileExtensions,
                out UInt32 pcchActual
                );

            [DllImport(DllImport.WindowsCodecs, EntryPoint = "IWICBitmapCodecInfo_DoesSupportAnimation_Proxy")]
            internal static extern int /* HRESULT */ DoesSupportAnimation(
                System.Windows.Media.SafeMILHandle /* IWICBitmapCodecInfo */ THIS_PTR,
                out bool pfSupportAnimation
                );

            [DllImport(DllImport.WindowsCodecs, EntryPoint = "IWICBitmapCodecInfo_DoesSupportLossless_Proxy")]
            internal static extern int /* HRESULT */ DoesSupportLossless(
                System.Windows.Media.SafeMILHandle /* IWICBitmapCodecInfo */ THIS_PTR,
                out bool pfSupportLossless
                );

            [DllImport(DllImport.WindowsCodecs, EntryPoint = "IWICBitmapCodecInfo_DoesSupportMultiframe_Proxy")]
            internal static extern int /* HRESULT */ DoesSupportMultiframe(
                System.Windows.Media.SafeMILHandle /* IWICBitmapCodecInfo */ THIS_PTR,
                out bool pfSupportMultiframe
                );
}

        internal static class WICMetadataQueryReader
        {
            [DllImport(DllImport.WindowsCodecs, EntryPoint = "IWICMetadataQueryReader_GetContainerFormat_Proxy")]
            internal static extern int /* HRESULT */ GetContainerFormat(
                System.Windows.Media.SafeMILHandle /* IWICMetadataQueryReader */ THIS_PTR,
                out Guid pguidContainerFormat);

            [DllImport(DllImport.WindowsCodecs, EntryPoint = "IWICMetadataQueryReader_GetLocation_Proxy")]
            internal static extern int /* HRESULT */ GetLocation(
                System.Windows.Media.SafeMILHandle /* IWICMetadataQueryReader */ THIS_PTR,
                UInt32 cchLocation,
                [Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder wzNamespace,
                out UInt32 pcchActual
                );

            [DllImport(DllImport.WindowsCodecs, EntryPoint = "IWICMetadataQueryReader_GetMetadataByName_Proxy")]
            internal static extern int /* HRESULT */ GetMetadataByName(
                System.Windows.Media.SafeMILHandle /* IWICMetadataQueryReader */ THIS_PTR,
                [MarshalAs(UnmanagedType.LPWStr)] String wzName,
                ref System.Windows.Media.Imaging.PROPVARIANT propValue
                );

            [DllImport(DllImport.WindowsCodecs, EntryPoint = "IWICMetadataQueryReader_GetMetadataByName_Proxy")]
            internal static extern int /* HRESULT */ ContainsMetadataByName(
                System.Windows.Media.SafeMILHandle /* IWICMetadataQueryReader */ THIS_PTR,
                [MarshalAs(UnmanagedType.LPWStr)] String wzName,
                IntPtr propVar
                );

            [DllImport(DllImport.WindowsCodecs, EntryPoint = "IWICMetadataQueryReader_GetEnumerator_Proxy")]
            internal static extern int /* HRESULT */ GetEnumerator(
                System.Windows.Media.SafeMILHandle /* IWICMetadataQueryReader */ THIS_PTR,
                out System.Windows.Media.SafeMILHandle /* IEnumString */ enumString
                );
        }

        internal static class WICMetadataQueryWriter
        {
            [DllImport(DllImport.WindowsCodecs, EntryPoint = "IWICMetadataQueryWriter_SetMetadataByName_Proxy")]
            internal static extern int /* HRESULT */ SetMetadataByName(
                System.Windows.Media.SafeMILHandle /* IWICMetadataQueryWriter */ THIS_PTR,
                [MarshalAs(UnmanagedType.LPWStr)] String wzName,
                ref System.Windows.Media.Imaging.PROPVARIANT propValue
                );

            [DllImport(DllImport.WindowsCodecs, EntryPoint = "IWICMetadataQueryWriter_RemoveMetadataByName_Proxy")]
            internal static extern int /* HRESULT */ RemoveMetadataByName(
                System.Windows.Media.SafeMILHandle /* IWICMetadataQueryWriter */ THIS_PTR,
                [MarshalAs(UnmanagedType.LPWStr)] String wzName
                );
        }

        internal static class WICFastMetadataEncoder
        {
            [DllImport(DllImport.WindowsCodecs, EntryPoint = "IWICFastMetadataEncoder_Commit_Proxy")]
            internal static extern int /* HRESULT */ Commit(
                System.Windows.Media.SafeMILHandle /* IWICFastMetadataEncoder */ THIS_PTR
                );

            [DllImport(DllImport.WindowsCodecs, EntryPoint = "IWICFastMetadataEncoder_GetMetadataQueryWriter_Proxy")]
            internal static extern int /* HRESULT */ GetMetadataQueryWriter(
                System.Windows.Media.SafeMILHandle /* IWICFastMetadataEncoder */ THIS_PTR,
                out SafeMILHandle /* IWICMetadataQueryWriter */ ppIQueryWriter
                );
        }

        internal static class EnumString
        {
            [DllImport(DllImport.WindowsCodecs, EntryPoint = "IEnumString_Next_WIC_Proxy")]
            internal static extern int /* HRESULT */ Next(
                System.Windows.Media.SafeMILHandle /* IEnumString */ THIS_PTR,
                Int32 celt,
                ref IntPtr rgElt,
                ref Int32 pceltFetched
                );

            [DllImport(DllImport.WindowsCodecs, EntryPoint = "IEnumString_Reset_WIC_Proxy")]
            internal static extern int /* HRESULT */ Reset(
                System.Windows.Media.SafeMILHandle /* IEnumString */ THIS_PTR
                );
        }

        internal static class IPropertyBag2
        {
            [DllImport(DllImport.WindowsCodecs, EntryPoint = "IPropertyBag2_Write_Proxy")]
            internal static extern int /* HRESULT */ Write(
                System.Windows.Media.SafeMILHandle /* IPropertyBag2 */ THIS_PTR,
                UInt32 cProperties,
                ref System.Windows.Media.Imaging.PROPBAG2 propBag,
                ref System.Windows.Media.Imaging.PROPVARIANT propValue
                );
        }

        internal static class WICBitmapSource
        {
            [DllImport(DllImport.WindowsCodecs, EntryPoint = "IWICBitmapSource_GetSize_Proxy")]
            internal static extern int /* HRESULT */ GetSize(
                System.Windows.Media.SafeMILHandle /* IWICBitmapSource */ THIS_PTR,
                out UInt32 puiWidth,
                out UInt32 puiHeight);

            [DllImport(DllImport.WindowsCodecs, EntryPoint = "IWICBitmapSource_GetPixelFormat_Proxy")]
            internal static extern int /* HRESULT */ GetPixelFormat(
                System.Windows.Media.SafeMILHandle /* IWICBitmapSource */ THIS_PTR,
                out Guid pPixelFormatEnum);

            [DllImport(DllImport.WindowsCodecs, EntryPoint = "IWICBitmapSource_GetResolution_Proxy")]
            internal static extern int /* HRESULT */ GetResolution(
                System.Windows.Media.SafeMILHandle /* IWICBitmapSource */ THIS_PTR,
                out double pDpiX,
                out double pDpiY);

            [DllImport(DllImport.WindowsCodecs, EntryPoint = "IWICBitmapSource_CopyPalette_Proxy")]
            internal static extern int /* HRESULT */ CopyPalette(
                System.Windows.Media.SafeMILHandle /* IWICBitmapSource */ THIS_PTR,
                System.Windows.Media.SafeMILHandle /* IMILPalette */ pIPalette);

            [DllImport(DllImport.WindowsCodecs, EntryPoint = "IWICBitmapSource_CopyPixels_Proxy")]
            internal static extern int /* HRESULT */ CopyPixels(
                SafeMILHandle /* IWICBitmapSource */ THIS_PTR,
                ref Int32Rect prc,
                UInt32 cbStride,
                UInt32 cbBufferSize,
                IntPtr /* BYTE* */ pvPixels);
        }

        internal static class WICBitmapDecoder
        {
            [DllImport(DllImport.WindowsCodecs, EntryPoint = "IWICBitmapDecoder_GetDecoderInfo_Proxy")]
            internal static extern int /* HRESULT */ GetDecoderInfo(
                SafeMILHandle THIS_PTR,
                out SafeMILHandle /* IWICBitmapDecoderInfo */ ppIDecoderInfo);

            [DllImport(DllImport.WindowsCodecs, EntryPoint = "IWICBitmapDecoder_CopyPalette_Proxy")]
            internal static extern int /* HRESULT */ CopyPalette(
                SafeMILHandle /* IWICBitmapDecoder */ THIS_PTR,
                SafeMILHandle /* IMILPalette */ pIPalette);

            [DllImport(DllImport.WindowsCodecs, EntryPoint = "IWICBitmapDecoder_GetPreview_Proxy")]
            internal static extern int /* HRESULT */ GetPreview(
                SafeMILHandle THIS_PTR,
                out IntPtr /* IWICBitmapSource */ ppIBitmapSource
                );

            [DllImport(DllImport.WindowsCodecs, EntryPoint = "IWICBitmapDecoder_GetColorContexts_Proxy")]
            internal static extern int /* HRESULT */ GetColorContexts(
                SafeMILHandle THIS_PTR,
                uint count,
                IntPtr[] /* IWICColorContext */ ppIColorContext,
                out uint pActualCount
                );

            [DllImport(DllImport.WindowsCodecs, EntryPoint = "IWICBitmapDecoder_GetThumbnail_Proxy")]
            internal static extern int /* HRESULT */ GetThumbnail(
                SafeMILHandle /* IWICBitmapDecoder */ THIS_PTR,
                out IntPtr /* IWICBitmapSource */ ppIThumbnail
                );

            [DllImport(DllImport.WindowsCodecs, EntryPoint = "IWICBitmapDecoder_GetMetadataQueryReader_Proxy")]
            internal static extern int /* HRESULT */ GetMetadataQueryReader(
                SafeMILHandle /* IWICBitmapDecoder */ THIS_PTR,
                out IntPtr /* IWICMetadataQueryReader */ ppIQueryReader
                );

            [DllImport(DllImport.WindowsCodecs, EntryPoint = "IWICBitmapDecoder_GetFrameCount_Proxy")]
            internal static extern int /* HRESULT */ GetFrameCount(
                SafeMILHandle THIS_PTR,
                out uint pFrameCount
                );

            [DllImport(DllImport.WindowsCodecs, EntryPoint = "IWICBitmapDecoder_GetFrame_Proxy")]
            internal static extern int /* HRESULT */ GetFrame(
                SafeMILHandle /* IWICBitmapDecoder */ THIS_PTR,
                UInt32 index,
                out IntPtr /* IWICBitmapFrameDecode */ ppIFrameDecode
                );
        }

        internal static class WICBitmapFrameDecode
        {
            [DllImport(DllImport.WindowsCodecs, EntryPoint = "IWICBitmapFrameDecode_GetThumbnail_Proxy")]
            internal static extern int /* HRESULT */ GetThumbnail(
                SafeMILHandle /* IWICBitmapFrameDecode */ THIS_PTR,
                out IntPtr /* IWICBitmap */ ppIThumbnail
                );

            [DllImport(DllImport.WindowsCodecs, EntryPoint = "IWICBitmapFrameDecode_GetMetadataQueryReader_Proxy")]
            internal static extern int /* HRESULT */ GetMetadataQueryReader(
                SafeMILHandle /* IWICBitmapFrameDecode */ THIS_PTR,
                out IntPtr /* IWICMetadataQueryReader */ ppIQueryReader
                );

            [DllImport(DllImport.WindowsCodecs, EntryPoint = "IWICBitmapFrameDecode_GetColorContexts_Proxy")]
            internal static extern int /* HRESULT */ GetColorContexts(
                SafeMILHandle /* IWICBitmapFrameDecode */ THIS_PTR,
                uint count,
                IntPtr[] /* IWICColorContext */ ppIColorContext,
                out uint pActualCount
                );
}

        internal static class MILUnknown
        {
            [DllImport(DllImport.MilCore, EntryPoint = "MILAddRef")]
            internal static extern UInt32 AddRef(SafeMILHandle pIUnkown);

            [DllImport(DllImport.MilCore, EntryPoint = "MILAddRef")]
            internal static extern UInt32 AddRef(SafeReversePInvokeWrapper pIUnknown);

            #pragma warning disable SYSLIB0004 // The Constrained Execution Region (CER) feature is not supported.  
            [DllImport(DllImport.MilCore, EntryPoint = "MILRelease"), ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
            #pragma warning restore SYSLIB0004 // The Constrained Execution Region (CER) feature is not supported.  

            internal static extern int Release(IntPtr pIUnkown);

            internal static void ReleaseInterface(ref IntPtr ptr)
            {
                if (ptr != IntPtr.Zero)
                {
                    #pragma warning suppress 6031 // Return value ignored on purpose.
                    UnsafeNativeMethods.MILUnknown.Release(ptr);
                    ptr = IntPtr.Zero;
                }
            }

            [DllImport(DllImport.MilCore, EntryPoint = "MILQueryInterface")]
            internal static extern int /* HRESULT */ QueryInterface(
                IntPtr pIUnknown,
                ref Guid guid,
                out IntPtr ppvObject);

            [DllImport(DllImport.MilCore, EntryPoint = "MILQueryInterface")]
            internal static extern int /* HRESULT */ QueryInterface(
                SafeMILHandle pIUnknown,
                ref Guid guid,
                out IntPtr ppvObject);
        }

        internal static class WICStream
        {
            [DllImport(DllImport.WindowsCodecs, EntryPoint = "IWICStream_InitializeFromIStream_Proxy")]
            internal static extern int /*HRESULT*/ InitializeFromIStream(
                IntPtr pIWICStream,
                IntPtr pIStream);

            [DllImport(DllImport.WindowsCodecs, EntryPoint = "IWICStream_InitializeFromMemory_Proxy")]
            internal static extern int /*HRESULT*/ InitializeFromMemory(
                IntPtr pIWICStream,
                IntPtr pbBuffer,
                uint cbSize);
        }

        internal static class WindowsCodecApi
        {
            [DllImport(DllImport.WindowsCodecs, EntryPoint = "WICCreateBitmapFromSection")]
            internal static extern int /*HRESULT*/ CreateBitmapFromSection(
                UInt32 width,
                UInt32 height,
                ref Guid pixelFormatGuid,
                IntPtr hSection,
                UInt32 stride,
                UInt32 offset,
                out BitmapSourceSafeMILHandle /* IWICBitmap */ ppIBitmap);
        }

        internal static class WICBitmapFrameEncode
        {
            [DllImport(DllImport.WindowsCodecs, EntryPoint="IWICBitmapFrameEncode_Initialize_Proxy")]
            internal static extern int /* HRESULT */ Initialize(SafeMILHandle /* IWICBitmapFrameEncode*  */ THIS_PTR,
                SafeMILHandle /* IPropertyBag2* */ pIEncoderOptions);

            [DllImport(DllImport.WindowsCodecs, EntryPoint="IWICBitmapFrameEncode_Commit_Proxy")]
            internal static extern int /* HRESULT */ Commit(SafeMILHandle /* IWICBitmapFrameEncode*  */ THIS_PTR);

            [DllImport(DllImport.WindowsCodecs, EntryPoint="IWICBitmapFrameEncode_SetSize_Proxy")]
            internal static extern int /* HRESULT */ SetSize(SafeMILHandle /* IWICBitmapFrameEncode*  */ THIS_PTR,
                int width,
                int height);

           [DllImport(DllImport.WindowsCodecs, EntryPoint="IWICBitmapFrameEncode_SetResolution_Proxy")]
            internal static extern int /* HRESULT */ SetResolution(SafeMILHandle /* IWICBitmapFrameEncode*  */ THIS_PTR,
                double dpiX,
                double dpiY);

            [DllImport(DllImport.WindowsCodecs, EntryPoint="IWICBitmapFrameEncode_WriteSource_Proxy")]
            internal static extern int /* HRESULT */ WriteSource(SafeMILHandle /* IWICBitmapFrameEncode*  */ THIS_PTR,
                SafeMILHandle /* IWICBitmapSource* */ pIBitmapSource,
                ref Int32Rect /* MILRect* */ r);

            [DllImport(DllImport.WindowsCodecs, EntryPoint = "IWICBitmapFrameEncode_SetThumbnail_Proxy")]
            internal static extern int /* HRESULT */ SetThumbnail(SafeMILHandle /* IWICBitmapFrameEncode*  */ THIS_PTR,
                System.Windows.Media.SafeMILHandle /* IWICBitmapSource* */ pIThumbnail);

            [DllImport(DllImport.WindowsCodecs, EntryPoint = "IWICBitmapFrameEncode_GetMetadataQueryWriter_Proxy")]
            internal static extern int /* HRESULT */ GetMetadataQueryWriter(
                SafeMILHandle /* IWICBitmapFrameEncode */ THIS_PTR,
                out SafeMILHandle /* IWICMetadataQueryWriter */ ppIQueryWriter
                );

            [DllImport(DllImport.WindowsCodecs, EntryPoint = "IWICBitmapFrameEncode_SetColorContexts_Proxy")]
            internal static extern int /* HRESULT */ SetColorContexts(SafeMILHandle /* IWICBitmapEncoder*  */ THIS_PTR,
                    uint nIndex,
                    IntPtr[] /* IWICColorContext */ ppIColorContext
                    );
}

        internal static class WICBitmapEncoder
        {
            [DllImport(DllImport.WindowsCodecs, EntryPoint="IWICBitmapEncoder_Initialize_Proxy")]
            internal static extern int /* HRESULT */ Initialize(SafeMILHandle /* IWICBitmapEncoder* */ THIS_PTR,
                IntPtr /* IStream */ pStream,
                WICBitmapEncodeCacheOption option);

            [DllImport(DllImport.WindowsCodecs, EntryPoint="IWICBitmapEncoder_GetEncoderInfo_Proxy")]
            internal static extern int /* HRESULT */ GetEncoderInfo(SafeMILHandle /* IWICBitmapEncoder* */ THIS_PTR,
                out SafeMILHandle /* IWICBitmapEncoderInfo ** */ ppIEncoderInfo
                );

            [DllImport(DllImport.WindowsCodecs, EntryPoint="IWICBitmapEncoder_CreateNewFrame_Proxy")]
            internal static extern int /* HRESULT */ CreateNewFrame(SafeMILHandle /* IWICBitmapEncoder* */ THIS_PTR,
                out SafeMILHandle /* IWICBitmapFrameEncode ** */ ppIFramEncode,
                out SafeMILHandle /* IPropertyBag2 ** */ ppIEncoderOptions
            );

            [DllImport(DllImport.WindowsCodecs, EntryPoint="IWICBitmapEncoder_SetThumbnail_Proxy")]
            internal static extern int /* HRESULT */ SetThumbnail(SafeMILHandle /* IWICBitmapEncoder*  */ THIS_PTR,
                System.Windows.Media.SafeMILHandle /* IWICBitmapSource* */ pIThumbnail);

            [DllImport(DllImport.WindowsCodecs, EntryPoint="IWICBitmapEncoder_SetPalette_Proxy")]
            internal static extern int /* HRESULT */ SetPalette(SafeMILHandle /* IWICBitmapEncoder*  */ THIS_PTR,
                System.Windows.Media.SafeMILHandle /* IWICPalette* */ pIPalette);

            [DllImport(DllImport.WindowsCodecs, EntryPoint = "IWICBitmapEncoder_GetMetadataQueryWriter_Proxy")]
            internal static extern int /* HRESULT */ GetMetadataQueryWriter(
                SafeMILHandle /* IWICBitmapEncoder */ THIS_PTR,
                out SafeMILHandle /* IWICMetadataQueryWriter */ ppIQueryWriter
                );

            [DllImport(DllImport.WindowsCodecs, EntryPoint="IWICBitmapEncoder_Commit_Proxy")]
            internal static extern int /* HRESULT */ Commit(SafeMILHandle /* IWICBitmapEncoder* */ THIS_PTR);
        }

        internal static class WICPalette
        {
            [DllImport(DllImport.WindowsCodecs, EntryPoint="IWICPalette_InitializePredefined_Proxy")]
            internal static extern int /* HRESULT */ InitializePredefined(System.Windows.Media.SafeMILHandle /* IWICPalette */ THIS_PTR,
                WICPaletteType ePaletteType,
                bool fAddTransparentColor);

            [DllImport(DllImport.WindowsCodecs, EntryPoint="IWICPalette_InitializeCustom_Proxy")]
            internal static extern int /* HRESULT */ InitializeCustom(System.Windows.Media.SafeMILHandle /* IWICPalette */ THIS_PTR,
                IntPtr /* MILColor* */ pColors,
                int colorCount);

            [DllImport(DllImport.WindowsCodecs, EntryPoint="IWICPalette_InitializeFromBitmap_Proxy")]
            internal static extern int /* HRESULT */ InitializeFromBitmap(System.Windows.Media.SafeMILHandle /* IWICPalette */ THIS_PTR,
                System.Windows.Media.SafeMILHandle /* IWICBitmapSource* */ pISurface,
                int colorCount,
                bool fAddTransparentColor);

            [DllImport(DllImport.WindowsCodecs, EntryPoint="IWICPalette_InitializeFromPalette_Proxy")]
            internal static extern int /* HRESULT */ InitializeFromPalette(IntPtr /* IWICPalette */ THIS_PTR,
                System.Windows.Media.SafeMILHandle /* IWICPalette */ pIWICPalette);

            [DllImport(DllImport.WindowsCodecs, EntryPoint="IWICPalette_GetType_Proxy")]
            internal static extern int /* HRESULT */ GetType(System.Windows.Media.SafeMILHandle /* IWICPalette */ THIS_PTR,
                out WICPaletteType pePaletteType);

            [DllImport(DllImport.WindowsCodecs, EntryPoint="IWICPalette_GetColorCount_Proxy")]
            internal static extern int /* HRESULT */ GetColorCount(System.Windows.Media.SafeMILHandle /* IWICPalette */ THIS_PTR,
                out int pColorCount);

            [DllImport(DllImport.WindowsCodecs, EntryPoint="IWICPalette_GetColors_Proxy")]
            internal static extern int /* HRESULT */ GetColors(System.Windows.Media.SafeMILHandle /* IWICPalette */ THIS_PTR,
                int colorCount,
                IntPtr /* MILColor* */ pColors,
                out int pcActualCount);

            [DllImport(DllImport.WindowsCodecs, EntryPoint="IWICPalette_HasAlpha_Proxy")]
            internal static extern int /* HRESULT */ HasAlpha(System.Windows.Media.SafeMILHandle /* IWICPalette */ THIS_PTR,
                   out bool pfHasAlpha);
        }

        internal static class WICImagingFactory
        {
            [DllImport(DllImport.WindowsCodecs, EntryPoint = "IWICImagingFactory_CreateDecoderFromStream_Proxy")]
            internal static extern int /*HRESULT*/ CreateDecoderFromStream(
                IntPtr pICodecFactory,
                IntPtr /* IStream */ pIStream,
                ref Guid guidVendor,
                UInt32 metadataFlags,
                out IntPtr /* IWICBitmapDecoder */ ppIDecode);

            [DllImport(DllImport.WindowsCodecs, EntryPoint = "IWICImagingFactory_CreateDecoderFromFileHandle_Proxy")]
            internal static extern int /*HRESULT*/ CreateDecoderFromFileHandle(
                IntPtr pICodecFactory,
                Microsoft.Win32.SafeHandles.SafeFileHandle  /*ULONG_PTR*/ hFileHandle,
                ref Guid guidVendor,
                UInt32 metadataFlags,
                out IntPtr /* IWICBitmapDecoder */ ppIDecode);

            [DllImport(DllImport.WindowsCodecs, EntryPoint = "IWICImagingFactory_CreateComponentInfo_Proxy")]
            internal static extern int /*HRESULT*/ CreateComponentInfo(
                IntPtr pICodecFactory,
                ref Guid clsidComponent,
                out IntPtr /* IWICComponentInfo */ ppIComponentInfo);

            [DllImport(DllImport.WindowsCodecs, EntryPoint = "IWICImagingFactory_CreatePalette_Proxy")]
            internal static extern int /*HRESULT*/ CreatePalette(
                IntPtr pICodecFactory,
                out SafeMILHandle /* IWICPalette */ ppIPalette);

            [DllImport(DllImport.WindowsCodecs, EntryPoint = "IWICImagingFactory_CreateFormatConverter_Proxy")]
            internal static extern int /* HRESULT */ CreateFormatConverter(
                IntPtr pICodecFactory,
                out BitmapSourceSafeMILHandle /* IWICFormatConverter */ ppFormatConverter);

            [DllImport(DllImport.WindowsCodecs, EntryPoint = "IWICImagingFactory_CreateBitmapScaler_Proxy")]
            internal static extern int /* HRESULT */ CreateBitmapScaler(
                IntPtr pICodecFactory,
                out BitmapSourceSafeMILHandle /* IWICBitmapScaler */ ppBitmapScaler);

            [DllImport(DllImport.WindowsCodecs, EntryPoint = "IWICImagingFactory_CreateBitmapClipper_Proxy")]
            internal static extern int /* HRESULT */ CreateBitmapClipper(
                IntPtr pICodecFactory,
                out BitmapSourceSafeMILHandle /* IWICBitmapClipper */ ppBitmapClipper);

            [DllImport(DllImport.WindowsCodecs, EntryPoint = "IWICImagingFactory_CreateBitmapFlipRotator_Proxy")]
            internal static extern int /* HRESULT */ CreateBitmapFlipRotator(
                IntPtr pICodecFactory,
                out BitmapSourceSafeMILHandle /* IWICBitmapFlipRotator */ ppBitmapFlipRotator);

            [DllImport(DllImport.WindowsCodecs, EntryPoint = "IWICImagingFactory_CreateStream_Proxy")]
            internal static extern int /* HRESULT */ CreateStream(
                IntPtr pICodecFactory,
                out IntPtr /* IWICBitmapStream */ ppIStream);

            [DllImport(DllImport.WindowsCodecs, EntryPoint = "IWICImagingFactory_CreateEncoder_Proxy")]
            internal static extern int /* HRESULT */ CreateEncoder(
                IntPtr pICodecFactory,
                ref Guid guidContainerFormat,
                ref Guid guidVendor,
                out SafeMILHandle /* IUnknown** */ ppICodec);

            [DllImport(DllImport.WindowsCodecs, EntryPoint = "IWICImagingFactory_CreateBitmapFromSource_Proxy")]
            internal static extern int /*HRESULT*/ CreateBitmapFromSource(
                IntPtr THIS_PTR,
                SafeMILHandle /* IWICBitmapSource */ pIBitmapSource,
                WICBitmapCreateCacheOptions options,
                out BitmapSourceSafeMILHandle /* IWICBitmap */ ppIBitmap);

            [DllImport(DllImport.WindowsCodecs, EntryPoint = "IWICImagingFactory_CreateBitmapFromMemory_Proxy")]
            internal static extern int /*HRESULT*/ CreateBitmapFromMemory(
                IntPtr THIS_PTR,
                UInt32 width,
                UInt32 height,
                ref Guid pixelFormatGuid,
                UInt32 stride,
                UInt32 cbBufferSize,
                IntPtr /* BYTE* */ pvPixels,
                out BitmapSourceSafeMILHandle /* IWICBitmap */ ppIBitmap);

            [DllImport(DllImport.WindowsCodecs, EntryPoint = "IWICImagingFactory_CreateBitmap_Proxy")]
            internal static extern int /*HRESULT*/ CreateBitmap(
                IntPtr THIS_PTR,
                UInt32 width,
                UInt32 height,
                ref Guid pixelFormatGuid,
                WICBitmapCreateCacheOptions options,
                out BitmapSourceSafeMILHandle /* IWICBitmap */ ppIBitmap);

            [DllImport(DllImport.WindowsCodecs, EntryPoint = "IWICImagingFactory_CreateBitmapFromHBITMAP_Proxy")]
            internal static extern int /*HRESULT*/ CreateBitmapFromHBITMAP(
                IntPtr THIS_PTR,
                IntPtr hBitmap,
                IntPtr hPalette,
                WICBitmapAlphaChannelOption options,
                out BitmapSourceSafeMILHandle /* IWICBitmap */ ppIBitmap);

            [DllImport(DllImport.WindowsCodecs, EntryPoint = "IWICImagingFactory_CreateBitmapFromHICON_Proxy")]
            internal static extern int /*HRESULT*/ CreateBitmapFromHICON(
                IntPtr THIS_PTR,
                IntPtr hIcon,
                out BitmapSourceSafeMILHandle /* IWICBitmap */ ppIBitmap);

            [DllImport(DllImport.WindowsCodecs, EntryPoint = "IWICImagingFactory_CreateFastMetadataEncoderFromDecoder_Proxy")]
            internal static extern int /*HRESULT*/ CreateFastMetadataEncoderFromDecoder(
                IntPtr THIS_PTR,
                SafeMILHandle /* IWICBitmapDecoder */ pIDecoder,
                out SafeMILHandle /* IWICFastMetadataEncoder */ ppIFME);

            [DllImport(DllImport.WindowsCodecs, EntryPoint = "IWICImagingFactory_CreateFastMetadataEncoderFromFrameDecode_Proxy")]
            internal static extern int /*HRESULT*/ CreateFastMetadataEncoderFromFrameDecode(
                IntPtr THIS_PTR,
                BitmapSourceSafeMILHandle /* IWICBitmapFrameDecode */ pIFrameDecode,
                out SafeMILHandle /* IWICFastMetadataEncoder */ ppIBitmap);

            [DllImport(DllImport.WindowsCodecs, EntryPoint = "IWICImagingFactory_CreateQueryWriter_Proxy")]
            internal static extern int /*HRESULT*/ CreateQueryWriter(
                IntPtr THIS_PTR,
                ref Guid metadataFormat,
                ref Guid guidVendor,
                out IntPtr /* IWICMetadataQueryWriter */ queryWriter
            );

            [DllImport(DllImport.WindowsCodecs, EntryPoint = "IWICImagingFactory_CreateQueryWriterFromReader_Proxy")]
            internal static extern int /*HRESULT*/ CreateQueryWriterFromReader(
                IntPtr THIS_PTR,
                SafeMILHandle /* IWICMetadataQueryReader */ queryReader,
                ref Guid guidVendor,
                out IntPtr /* IWICMetadataQueryWriter */ queryWriter
            );
        }

        internal static class WICComponentFactory
        {
            [DllImport(DllImport.WindowsCodecs, EntryPoint = "IWICComponentFactory_CreateMetadataWriterFromReader_Proxy")]
            internal static extern int /*HRESULT*/ CreateMetadataWriterFromReader(
                IntPtr pICodecFactory,
                SafeMILHandle pIMetadataReader,
                ref Guid guidVendor,
                out IntPtr metadataWriter
            );

            [DllImport(DllImport.WindowsCodecs, EntryPoint = "IWICComponentFactory_CreateQueryWriterFromBlockWriter_Proxy")]
            internal static extern int /*HRESULT*/ CreateQueryWriterFromBlockWriter(
                IntPtr pICodecFactory,
                IntPtr pIBlockWriter,
                ref IntPtr ppIQueryWriter
            );
        }

        internal static class WICMetadataBlockReader
        {
            [DllImport(DllImport.WindowsCodecs, EntryPoint = "IWICMetadataBlockReader_GetCount_Proxy")]
            internal static extern int /*HRESULT*/ GetCount(
                IntPtr pIBlockReader,
                out UInt32 count
            );

            [DllImport(DllImport.WindowsCodecs, EntryPoint = "IWICMetadataBlockReader_GetReaderByIndex_Proxy")]
            internal static extern int /*HRESULT*/ GetReaderByIndex(
                IntPtr pIBlockReader,
                UInt32 index,
                out SafeMILHandle /* IWICMetadataReader* */ pIMetadataReader
            );
        }

        internal static class WICPixelFormatInfo
        {
            [DllImport(DllImport.WindowsCodecs, EntryPoint = "IWICPixelFormatInfo_GetBitsPerPixel_Proxy")]
            internal static extern int /*HRESULT*/ GetBitsPerPixel(
                IntPtr /* IWICPixelFormatInfo */ pIPixelFormatInfo,
                out UInt32 uiBitsPerPixel
            );

            [DllImport(DllImport.WindowsCodecs, EntryPoint = "IWICPixelFormatInfo_GetChannelCount_Proxy")]
            internal static extern int /*HRESULT*/ GetChannelCount(
                IntPtr /* IWICPixelFormatInfo */ pIPixelFormatInfo,
                out UInt32 uiChannelCount
            );

            [DllImport(DllImport.WindowsCodecs, EntryPoint = "IWICPixelFormatInfo_GetChannelMask_Proxy")]
            internal unsafe static extern int /*HRESULT*/ GetChannelMask(
                IntPtr /* IWICPixelFormatInfo */ pIPixelFormatInfo,
                UInt32 uiChannelIndex,
                UInt32 cbMaskBuffer,
                byte *pbMaskBuffer,
                out UInt32 cbActual
            );
        }

        internal static class WICBitmapClipper
        {
            [DllImport(DllImport.WindowsCodecs, EntryPoint = "IWICBitmapClipper_Initialize_Proxy")]
            internal static extern int /* HRESULT */ Initialize(
                System.Windows.Media.SafeMILHandle /* IWICBitmapClipper */ THIS_PTR,
                System.Windows.Media.SafeMILHandle /* IWICBitmapSource */ source,
                ref Int32Rect prc);
        }

        internal static class WICBitmapFlipRotator
        {
            [DllImport(DllImport.WindowsCodecs, EntryPoint = "IWICBitmapFlipRotator_Initialize_Proxy")]
            internal static extern int /* HRESULT */ Initialize(
                System.Windows.Media.SafeMILHandle /* IWICBitmapFlipRotator */ THIS_PTR,
                System.Windows.Media.SafeMILHandle /* IWICBitmapSource */ source,
                WICBitmapTransformOptions options);
        }

        internal static class WICBitmapScaler
        {
            [DllImport(DllImport.WindowsCodecs, EntryPoint = "IWICBitmapScaler_Initialize_Proxy")]
            internal static extern int /* HRESULT */ Initialize(
                System.Windows.Media.SafeMILHandle /* IWICBitmapScaler */ THIS_PTR,
                System.Windows.Media.SafeMILHandle /* IWICBitmapSource */ source,
                uint width,
                uint height,
                WICInterpolationMode mode);
        }

        internal static class WICFormatConverter
        {
            [DllImport(DllImport.WindowsCodecs, EntryPoint = "IWICFormatConverter_Initialize_Proxy")]
            internal static extern int /* HRESULT */ Initialize(
                System.Windows.Media.SafeMILHandle /* IWICFormatConverter */ THIS_PTR,
                System.Windows.Media.SafeMILHandle /* IWICBitmapSource */ source,
                ref Guid dstFormat,
                DitherType dither,
                System.Windows.Media.SafeMILHandle /* IWICBitmapPalette */ bitmapPalette,
                double alphaThreshold,
                WICPaletteType paletteTranslate
                );
        }
        internal static class IWICColorContext
        {
            internal enum WICColorContextType : uint 
            {
                WICColorContextUninitialized  = 0,
                WICColorContextProfile        = 1,
                WICColorContextExifColorSpace = 2
            };
            
            [DllImport(DllImport.WindowsCodecs, EntryPoint = "IWICColorContext_InitializeFromMemory_Proxy")]
            internal static extern int /* HRESULT */ InitializeFromMemory(
                SafeMILHandle THIS_PTR,
                byte[] pbBuffer,
                uint cbBufferSize
                );

            // We import the following functions from MilCore because WindowsCodecs does not have 
            // them built-in
            [DllImport(DllImport.MilCore, EntryPoint = "IWICColorContext_GetProfileBytes_Proxy")]
            internal static extern int /* HRESULT */ GetProfileBytes(
                SafeMILHandle THIS_PTR,
                uint cbBuffer,
                /* inout */ byte[] pbBuffer,
                out uint pcbActual
                );

            [DllImport(DllImport.MilCore, EntryPoint = "IWICColorContext_GetType_Proxy")]
            internal static extern int /* HRESULT */ GetType(
                SafeMILHandle THIS_PTR,
                out WICColorContextType pType
                );

            [DllImport(DllImport.MilCore, EntryPoint = "IWICColorContext_GetExifColorSpace_Proxy")]
            internal static extern int /* HRESULT */ GetExifColorSpace(
                SafeMILHandle THIS_PTR,
                out uint pValue
                );
        }

        internal static class WICColorTransform
        {
            [DllImport(DllImport.WindowsCodecsExt, EntryPoint = "IWICColorTransform_Initialize_Proxy")]
            internal static extern int /* HRESULT */ Initialize(
                System.Windows.Media.SafeMILHandle /* IWICColorTransform */ THIS_PTR,
                System.Windows.Media.SafeMILHandle /* IWICBitmapSource */ source,
                System.Windows.Media.SafeMILHandle /* IWICColorContext */ pIContextSource,
                System.Windows.Media.SafeMILHandle /* IWICColorContext */ pIContextDest,
                ref Guid pixelFmtDest
                );
        }

        internal static class WICBitmap
        {
            [DllImport(DllImport.WindowsCodecs, EntryPoint = "IWICBitmap_Lock_Proxy")]
            internal static extern int /* HRESULT */ Lock(
                System.Windows.Media.SafeMILHandle /* IWICBitmap */ THIS_PTR,
                ref Int32Rect prcLock,
                LockFlags flags,
                out SafeMILHandle /* IWICBitmapLock* */ ppILock);

            [DllImport(DllImport.WindowsCodecs, EntryPoint = "IWICBitmap_SetResolution_Proxy")]
            internal static extern int /* HRESULT */ SetResolution(
                System.Windows.Media.SafeMILHandle /* IWICBitmap */ THIS_PTR,
                double dpiX,
                double dpiY);

            [DllImport(DllImport.WindowsCodecs, EntryPoint = "IWICBitmap_SetPalette_Proxy")]
            internal static extern int /* HRESULT */ SetPalette(
                System.Windows.Media.SafeMILHandle /* IWICBitmap */ THIS_PTR,
                System.Windows.Media.SafeMILHandle /* IMILPalette */ pIPalette);
        }

        internal static class WICBitmapLock
        {
            [DllImport(DllImport.WindowsCodecs, EntryPoint = "IWICBitmapLock_GetStride_Proxy")]
            internal static extern int /* HRESULT */ GetStride(
                SafeMILHandle /* IWICBitmapLock */ pILock,
                ref uint pcbStride
                );

            [DllImport(DllImport.WindowsCodecs, EntryPoint = "IWICBitmapLock_GetDataPointer_STA_Proxy")]
            internal static extern int /* HRESULT */ GetDataPointer(
                SafeMILHandle /* IWICBitmapLock */ pILock,
                ref uint pcbBufferSize,
                ref IntPtr ppbData
                );
        }

        internal static class WICCodec
        {
            // When updating this value be sure to update milrender.h's version
            // Reserve the number by editing and resubmitting SDKVersion.txt
            internal const int WINCODEC_SDK_VERSION = 0x0236;

            [DllImport(DllImport.WindowsCodecs, EntryPoint = "WICCreateImagingFactory_Proxy")]
            internal static extern int CreateImagingFactory(
                UInt32 SDKVersion,
                out IntPtr ppICodecFactory
                );

            [DllImport(DllImport.WindowsCodecs, EntryPoint = "WICConvertBitmapSource")]
            internal static extern int /* HRESULT */ WICConvertBitmapSource(
                ref Guid dstPixelFormatGuid,
                SafeMILHandle /* IWICBitmapSource */ pISrc,
                out BitmapSourceSafeMILHandle /* IWICBitmapSource* */ ppIDst);

            [DllImport(DllImport.WindowsCodecs, EntryPoint = "WICSetEncoderFormat_Proxy")]
            internal static extern int /* HRESULT */ WICSetEncoderFormat(
                SafeMILHandle /* IWICBitmapSource */ pSourceIn,
                SafeMILHandle /* IMILPalette */ pIPalette,
                SafeMILHandle /* IWICBitmapFrameEncode*  */ pIFrameEncode,
                out SafeMILHandle /* IWICBitmapSource**  */ ppSourceOut);

            [DllImport(DllImport.WindowsCodecs, EntryPoint = "WICMapGuidToShortName")]//CASRemoval:
            internal static extern int /* HRESULT */ WICMapGuidToShortName(
                ref Guid guid,
                uint cchName,
                [Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder wzName,
                ref uint pcchActual);

            [DllImport(DllImport.WindowsCodecs, EntryPoint = "WICMapShortNameToGuid")]//CASRemoval:
            internal static extern int /* HRESULT */ WICMapShortNameToGuid(
                [MarshalAs(UnmanagedType.LPWStr)] String wzName,
                ref Guid guid);

            [DllImport(DllImport.WindowsCodecsExt, EntryPoint = "WICCreateColorTransform_Proxy")]
            internal static extern int /* HRESULT */ CreateColorTransform(
                out BitmapSourceSafeMILHandle  /* IWICColorTransform */ ppWICColorTransform);

            [DllImport(DllImport.WindowsCodecs, EntryPoint = "WICCreateColorContext_Proxy")]
            internal static extern int /* HRESULT */ CreateColorContext(
                IntPtr pICodecFactory,
                out System.Windows.Media.SafeMILHandle /* IWICColorContext */ ppColorContext);

            [DllImport("ole32.dll")]
            internal static extern int /* HRESULT */ CoInitialize(
                    IntPtr reserved);

            [DllImport("ole32.dll")]
            internal static extern void CoUninitialize();
}

        internal static class Mscms
        {
            [DllImport(DllImport.Mscms, EntryPoint = "CreateMultiProfileTransform")]
            internal static extern ColorTransformHandle /* HTRANSFORM */ CreateMultiProfileTransform(IntPtr[] /* PHPROFILE */ pahProfiles, UInt32 nProfiles, UInt32[] padwIntent, UInt32 nIntents, UInt32 dwFlags, UInt32 indexPreferredCMM);

            [DllImport(DllImport.Mscms, EntryPoint = "DeleteColorTransform", SetLastError = true)]
            internal static extern bool DeleteColorTransform(IntPtr /* HTRANSFORM */ hColorTransform);

            [DllImport(DllImport.Mscms, EntryPoint = "TranslateColors")]
            internal static extern int /* HRESULT */ TranslateColors(ColorTransformHandle /* HTRANSFORM */ hColorTransform, IntPtr paInputColors, UInt32 nColors, UInt32 ctInput, IntPtr paOutputColors, UInt32 ctOutput);

            [DllImport(DllImport.Mscms, EntryPoint = "OpenColorProfile")]
            internal static extern SafeProfileHandle /* HANDLE */ OpenColorProfile(ref MS.Win32.UnsafeNativeMethods.PROFILE pProfile, UInt32 dwDesiredAccess, UInt32 dwShareMode, UInt32 dwCreationMode);

            [DllImport(DllImport.Mscms, EntryPoint = "CloseColorProfile", SetLastError = true)]
            internal static extern bool CloseColorProfile(IntPtr /* HANDLE */ phProfile);

            [DllImport(DllImport.Mscms, EntryPoint = "GetColorProfileHeader", SetLastError = true)]
            internal static extern bool GetColorProfileHeader(SafeProfileHandle /* HANDLE */ phProfile, out MS.Win32.UnsafeNativeMethods.PROFILEHEADER pHeader);

            [DllImport(DllImport.Mscms, CharSet = CharSet.Auto, BestFitMapping = false)]
            internal static extern int /* HRESULT */ GetColorDirectory(IntPtr pMachineName, StringBuilder pBuffer, out uint pdwSize);

            [DllImport(DllImport.Mscms, CharSet = CharSet.Auto, BestFitMapping = false)]
            internal static extern int /* HRESULT */ GetStandardColorSpaceProfile(IntPtr pMachineName, uint dwProfileID, StringBuilder pProfileName, out uint pdwSize);

            [DllImport(DllImport.Mscms, EntryPoint = "GetColorProfileFromHandle", SetLastError = true)]
            internal static extern bool GetColorProfileFromHandle(SafeProfileHandle /* HANDLE */ hProfile, byte[] pBuffer, ref uint pdwSize);
        }

        internal static class MILFactory2
        {
            [DllImport(DllImport.MilCore, EntryPoint = "MILCreateFactory")]
            internal static extern int CreateFactory(
                out IntPtr ppIFactory,
                UInt32 SDKVersion
                );

            [DllImport(DllImport.MilCore, EntryPoint = "MILFactoryCreateMediaPlayer")]
            internal static extern int /*HRESULT*/ CreateMediaPlayer(
                IntPtr THIS_PTR,
                SafeMILHandle /* CEventProxy */ pEventProxy,
                bool canOpenAllMedia,
                out SafeMediaHandle /* IMILMedia */ ppMedia);

            [DllImport(DllImport.MilCore, EntryPoint = "MILFactoryCreateBitmapRenderTarget")]
            internal static extern int /* HRESULT */ CreateBitmapRenderTarget(
                IntPtr THIS_PTR,
                UInt32 width,
                UInt32 height,
                PixelFormatEnum pixelFormatEnum,
                float dpiX,
                float dpiY,
                MILRTInitializationFlags dwFlags,
                out SafeMILHandle /* IMILRenderTargetBitmap */ ppIRenderTargetBitmap);

            [DllImport(DllImport.MilCore, EntryPoint = "MILFactoryCreateSWRenderTargetForBitmap")]
            internal static extern int /* HRESULT */ CreateBitmapRenderTargetForBitmap(
                IntPtr THIS_PTR,
                BitmapSourceSafeMILHandle /* IWICBitmap */ pIBitmap,
                out SafeMILHandle /* IMILRenderTargetBitmap */ ppIRenderTargetBitmap);
        }

        internal static class InteropDeviceBitmap
        {
            internal delegate void FrontBufferAvailableCallback(bool lost, uint version);
            
            [DllImport(DllImport.MilCore, EntryPoint = "InteropDeviceBitmap_Create")]
            internal static extern int Create(
                IntPtr d3dResource,
                double dpiX,
                double dpiY,
                uint version,
                FrontBufferAvailableCallback pfnCallback,
                bool isSoftwareFallbackEnabled,
                out SafeMILHandle ppInteropDeviceBitmap,
                out uint pixelWidth,
                out uint pixelHeight
                );

            [DllImport(DllImport.MilCore, EntryPoint = "InteropDeviceBitmap_Detach")]
            internal static extern void Detach(
                SafeMILHandle pInteropDeviceBitmap
                );

            [DllImport(DllImport.MilCore, EntryPoint = "InteropDeviceBitmap_AddDirtyRect")]
            internal static extern int AddDirtyRect(

                int x, 
                int y, 
                int w, 
                int h,
                SafeMILHandle pInteropDeviceBitmap
                );

            [DllImport(DllImport.MilCore, EntryPoint = "InteropDeviceBitmap_GetAsSoftwareBitmap")]
            internal static extern int GetAsSoftwareBitmap(SafeMILHandle pInteropDeviceBitmap, out BitmapSourceSafeMILHandle pIWICBitmapSource);
        }
    }
}

