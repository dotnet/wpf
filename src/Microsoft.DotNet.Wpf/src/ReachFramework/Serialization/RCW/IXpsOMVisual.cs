// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security;

namespace System.Windows.Xps.Serialization.RCW
{
    /// <summary>
    /// RCW for xpsobjectmodel.idl found in Windows SDK
    /// This is generated code with minor manual edits. 
    /// i.  Generate TLB
    ///      MIDL /TLB xpsobjectmodel.tlb xpsobjectmodel.IDL //xpsobjectmodel.IDL found in Windows SDK
    /// ii. Generate RCW in a DLL
    ///      TLBIMP xpsobjectmodel.tlb // Generates xpsobjectmodel.dll
    /// iii.Decompile the DLL and copy out the RCW by hand.
    ///      ILDASM xpsobjectmodel.dll
    /// </summary>
    
    [Guid("BC3E7333-FB0B-4AF3-A819-0B4EAAD0D2FD"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    [ComImport]
    internal interface IXpsOMVisual : IXpsOMShareable
    {
        [MethodImpl(MethodImplOptions.InternalCall)]
        [return: MarshalAs(UnmanagedType.IUnknown)]
        new object GetOwner();

        [MethodImpl(MethodImplOptions.InternalCall)]
        [return: ComAliasName("MSXPS.XPS_OBJECT_TYPE")]
        new XPS_OBJECT_TYPE GetType();

        [MethodImpl(MethodImplOptions.InternalCall)]
        [return: MarshalAs(UnmanagedType.Interface)]
        IXpsOMMatrixTransform GetTransform();

        [MethodImpl(MethodImplOptions.InternalCall)]
        [return: MarshalAs(UnmanagedType.Interface)]
        IXpsOMMatrixTransform GetTransformLocal();

        [MethodImpl(MethodImplOptions.InternalCall)]
        void SetTransformLocal([MarshalAs(UnmanagedType.Interface)] [In] IXpsOMMatrixTransform matrixTransform);

        [MethodImpl(MethodImplOptions.InternalCall)]
        [return: MarshalAs(UnmanagedType.LPWStr)]
        string GetTransformLookup();

        [MethodImpl(MethodImplOptions.InternalCall)]
        void SetTransformLookup([MarshalAs(UnmanagedType.LPWStr)] [In] string key);

        [MethodImpl(MethodImplOptions.InternalCall)]
        [return: MarshalAs(UnmanagedType.Interface)]
        IXpsOMGeometry GetClipGeometry();

        [MethodImpl(MethodImplOptions.InternalCall)]
        [return: MarshalAs(UnmanagedType.Interface)]
        IXpsOMGeometry GetClipGeometryLocal();

        [MethodImpl(MethodImplOptions.InternalCall)]
        void SetClipGeometryLocal([MarshalAs(UnmanagedType.Interface)] [In] IXpsOMGeometry clipGeometry);

        [MethodImpl(MethodImplOptions.InternalCall)]
        [return: MarshalAs(UnmanagedType.LPWStr)]
        string GetClipGeometryLookup();

        [MethodImpl(MethodImplOptions.InternalCall)]
        void SetClipGeometryLookup([MarshalAs(UnmanagedType.LPWStr)] [In] string key);

        [MethodImpl(MethodImplOptions.InternalCall)]
        float GetOpacity();

        [MethodImpl(MethodImplOptions.InternalCall)]
        void SetOpacity([In] float opacity);

        [MethodImpl(MethodImplOptions.InternalCall)]
        [return: MarshalAs(UnmanagedType.Interface)]
        IXpsOMBrush GetOpacityMaskBrush();

        [MethodImpl(MethodImplOptions.InternalCall)]
        [return: MarshalAs(UnmanagedType.Interface)]
        IXpsOMBrush GetOpacityMaskBrushLocal();

        [MethodImpl(MethodImplOptions.InternalCall)]
        void SetOpacityMaskBrushLocal([MarshalAs(UnmanagedType.Interface)] [In] IXpsOMBrush opacityMaskBrush);

        [MethodImpl(MethodImplOptions.InternalCall)]
        [return: MarshalAs(UnmanagedType.LPWStr)]
        string GetOpacityMaskBrushLookup();

        [MethodImpl(MethodImplOptions.InternalCall)]
        void SetOpacityMaskBrushLookup([MarshalAs(UnmanagedType.LPWStr)] [In] string key);

        [MethodImpl(MethodImplOptions.InternalCall)]
        [return: MarshalAs(UnmanagedType.LPWStr)]
        string GetName();

        [MethodImpl(MethodImplOptions.InternalCall)]
        void SetName([MarshalAs(UnmanagedType.LPWStr)] [In] string name);

        [MethodImpl(MethodImplOptions.InternalCall)]
        int GetIsHyperlinkTarget();

        [MethodImpl(MethodImplOptions.InternalCall)]
        void SetIsHyperlinkTarget([In] int isHyperlink);

        [MethodImpl(MethodImplOptions.InternalCall)]
        [return: MarshalAs(UnmanagedType.Interface)]
        IUri GetHyperlinkNavigateUri();

        [MethodImpl(MethodImplOptions.InternalCall)]
        void SetHyperlinkNavigateUri([MarshalAs(UnmanagedType.Interface)] [In] IUri hyperlinkUri);

        [MethodImpl(MethodImplOptions.InternalCall)]
        [return: MarshalAs(UnmanagedType.LPWStr)]
        string GetLanguage();

        [MethodImpl(MethodImplOptions.InternalCall)]
        void SetLanguage([MarshalAs(UnmanagedType.LPWStr)] [In] string language);
    }
}
