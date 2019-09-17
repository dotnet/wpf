// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

﻿using System;
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
    
    [Guid("3B0B6D38-53AD-41DA-B212-D37637A6714E"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    [ComImport]
    internal interface IXpsDocumentPackageTarget
    {
        [MethodImpl(MethodImplOptions.InternalCall)]
        [return: MarshalAs(UnmanagedType.Interface)]
        IXpsOMPackageWriter GetXpsOMPackageWriter([MarshalAs(UnmanagedType.Interface)] [In] IOpcPartUri documentSequencePartName, [MarshalAs(UnmanagedType.Interface)] [In] IOpcPartUri discardControlPartName);

        [MethodImpl(MethodImplOptions.InternalCall)]
        [return: MarshalAs(UnmanagedType.Interface)]
        IXpsOMObjectFactory GetXpsOMFactory();

        [MethodImpl(MethodImplOptions.InternalCall)]
        [return: ComAliasName("System.Windows.Xps.Serialization.RCW.XPS_DOCUMENT_TYPE")]
        XPS_DOCUMENT_TYPE GetXpsType();

    }
}
