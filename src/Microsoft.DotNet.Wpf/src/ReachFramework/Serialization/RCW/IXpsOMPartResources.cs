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
    
    [Guid("F4CF7729-4864-4275-99B3-A8717163ECAF"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    [ComImport]
    internal interface IXpsOMPartResources
    {
        [MethodImpl(MethodImplOptions.InternalCall)]
        [return: MarshalAs(UnmanagedType.Interface)]
        IXpsOMFontResourceCollection GetFontResources();

        [MethodImpl(MethodImplOptions.InternalCall)]
        [return: MarshalAs(UnmanagedType.Interface)]
        IXpsOMImageResourceCollection GetImageResources();

        [MethodImpl(MethodImplOptions.InternalCall)]
        [return: MarshalAs(UnmanagedType.Interface)]
        IXpsOMColorProfileResourceCollection GetColorProfileResources();

        [MethodImpl(MethodImplOptions.InternalCall)]
        [return: MarshalAs(UnmanagedType.Interface)]
        IXpsOMRemoteDictionaryResourceCollection GetRemoteDictionaryResources();
    }
}
