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
    
    [Guid("D3E18888-F120-4FEE-8C68-35296EAE91D4"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    [ComImport]
    internal interface IXpsOMPage : IXpsOMPart
    {
        [MethodImpl(MethodImplOptions.InternalCall)]
        [return: MarshalAs(UnmanagedType.Interface)]
        new IOpcPartUri GetPartName();

        [MethodImpl(MethodImplOptions.InternalCall)]
        new void SetPartName([MarshalAs(UnmanagedType.Interface)] [In] IOpcPartUri partUri);

        [MethodImpl(MethodImplOptions.InternalCall)]
        [return: MarshalAs(UnmanagedType.Interface)]
        IXpsOMPageReference GetOwner();

        [MethodImpl(MethodImplOptions.InternalCall)]
        [return: MarshalAs(UnmanagedType.Interface)]
        IXpsOMVisualCollection GetVisuals();

        [MethodImpl(MethodImplOptions.InternalCall)]
        [return: ComAliasName("System.Windows.Xps.Serialization.RCW.XPS_SIZE")]
        XPS_SIZE GetPageDimensions();

        [MethodImpl(MethodImplOptions.InternalCall)]
        void SetPageDimensions([ComAliasName("System.Windows.Xps.Serialization.RCW.XPS_SIZE")] [In] ref XPS_SIZE pageDimensions);

        [MethodImpl(MethodImplOptions.InternalCall)]
        [return: ComAliasName("System.Windows.Xps.Serialization.RCW.XPS_RECT")]
        XPS_RECT GetContentBox();

        [MethodImpl(MethodImplOptions.InternalCall)]
        void SetContentBox([ComAliasName("System.Windows.Xps.Serialization.RCW.XPS_RECT")] [In] ref XPS_RECT contentBox);

        [MethodImpl(MethodImplOptions.InternalCall)]
        [return: ComAliasName("System.Windows.Xps.Serialization.RCW.XPS_RECT")]
        XPS_RECT GetBleedBox();

        [MethodImpl(MethodImplOptions.InternalCall)]
        void SetBleedBox([ComAliasName("System.Windows.Xps.Serialization.RCW.XPS_RECT")] [In] ref XPS_RECT bleedBox);

        [MethodImpl(MethodImplOptions.InternalCall)]
        [return: MarshalAs(UnmanagedType.LPWStr)]
        string GetLanguage();

        [MethodImpl(MethodImplOptions.InternalCall)]
        void SetLanguage([MarshalAs(UnmanagedType.LPWStr)] [In] string language);

        [MethodImpl(MethodImplOptions.InternalCall)]
        [return: MarshalAs(UnmanagedType.LPWStr)]
        string GetName();

        [MethodImpl(MethodImplOptions.InternalCall)]
        void SetName([MarshalAs(UnmanagedType.LPWStr)] [In] string name);

        [MethodImpl(MethodImplOptions.InternalCall)]
        int GetIsHyperlinkTarget();

        [MethodImpl(MethodImplOptions.InternalCall)]
        void SetIsHyperlinkTarget([In] int isHyperlinkTarget);

        [MethodImpl(MethodImplOptions.InternalCall)]
        [return: MarshalAs(UnmanagedType.Interface)]
        IXpsOMDictionary GetDictionary();

        [MethodImpl(MethodImplOptions.InternalCall)]
        [return: MarshalAs(UnmanagedType.Interface)]
        IXpsOMDictionary GetDictionaryLocal();

        [MethodImpl(MethodImplOptions.InternalCall)]
        void SetDictionaryLocal([MarshalAs(UnmanagedType.Interface)] [In] IXpsOMDictionary resourceDictionary);

        [MethodImpl(MethodImplOptions.InternalCall)]
        [return: MarshalAs(UnmanagedType.Interface)]
        IXpsOMRemoteDictionaryResource GetDictionaryResource();

        [MethodImpl(MethodImplOptions.InternalCall)]
        void SetDictionaryResource([MarshalAs(UnmanagedType.Interface)] [In] IXpsOMRemoteDictionaryResource remoteDictionaryResource);

        [MethodImpl(MethodImplOptions.InternalCall)]
        void Write([MarshalAs(UnmanagedType.Interface)] [In] ISequentialStream stream, [In] int optimizeMarkupSize);

        [MethodImpl(MethodImplOptions.InternalCall)]
        [return: MarshalAs(UnmanagedType.LPWStr)]
        string GenerateUnusedLookupKey([ComAliasName("System.Windows.Xps.Serialization.RCW.XPS_OBJECT_TYPE")] [In] XPS_OBJECT_TYPE type);

        [MethodImpl(MethodImplOptions.InternalCall)]
        [return: MarshalAs(UnmanagedType.Interface)]
        IXpsOMPage Clone();
    }
}
