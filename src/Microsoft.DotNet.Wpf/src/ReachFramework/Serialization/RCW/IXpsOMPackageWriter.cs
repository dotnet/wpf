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
    
    [Guid("4E2AA182-A443-42C6-B41B-4F8E9DE73FF9")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    [ComImport]
    internal interface IXpsOMPackageWriter
    {
        [MethodImpl(MethodImplOptions.InternalCall)]
        void StartNewDocument([MarshalAs(UnmanagedType.Interface)] [In] IOpcPartUri documentPartName, [MarshalAs(UnmanagedType.Interface)] [In] IXpsOMPrintTicketResource documentPrintTicket, [MarshalAs(UnmanagedType.Interface)] [In] IXpsOMDocumentStructureResource documentStructure, [MarshalAs(UnmanagedType.Interface)] [In] IXpsOMSignatureBlockResourceCollection signatureBlockResources, [MarshalAs(UnmanagedType.Interface)] [In] IXpsOMPartUriCollection restrictedFonts);

        [MethodImpl(MethodImplOptions.InternalCall)]
        void AddPage([MarshalAs(UnmanagedType.Interface)] [In] IXpsOMPage page, [ComAliasName("MSXPS.XPS_SIZE")] [In] ref XPS_SIZE advisoryPageDimensions, [MarshalAs(UnmanagedType.Interface)] [In] IXpsOMPartUriCollection discardableResourceParts, [MarshalAs(UnmanagedType.Interface)] [In] IXpsOMStoryFragmentsResource storyFragments, [MarshalAs(UnmanagedType.Interface)] [In] IXpsOMPrintTicketResource pagePrintTicket, [MarshalAs(UnmanagedType.Interface)] [In] IXpsOMImageResource pageThumbnail);

        [MethodImpl(MethodImplOptions.InternalCall)]
        void AddResource([MarshalAs(UnmanagedType.Interface)] [In] IXpsOMResource resource);

        [MethodImpl(MethodImplOptions.InternalCall)]
        void Close();

        [MethodImpl(MethodImplOptions.InternalCall)]
        int isClosed();
    }
}
