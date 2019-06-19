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
    
    [Guid("70B4A6BB-88D4-4FA8-AAF9-6D9C596FDBAD"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    [ComImport]
    internal interface IXpsOMFontResourceCollection
    {
        [MethodImpl(MethodImplOptions.InternalCall)]
        uint GetCount();

        [MethodImpl(MethodImplOptions.InternalCall)]
        [return: MarshalAs(UnmanagedType.Interface)]
        IXpsOMFontResource GetAt([In] uint index);

        [MethodImpl(MethodImplOptions.InternalCall)]
        void SetAt([In] uint index, [MarshalAs(UnmanagedType.Interface)] [In] IXpsOMFontResource value);

        [MethodImpl(MethodImplOptions.InternalCall)]
        void InsertAt([In] uint index, [MarshalAs(UnmanagedType.Interface)] [In] IXpsOMFontResource value);

        [MethodImpl(MethodImplOptions.InternalCall)]
        void Append([MarshalAs(UnmanagedType.Interface)] [In] IXpsOMFontResource value);

        [MethodImpl(MethodImplOptions.InternalCall)]
        void RemoveAt([In] uint index);

        [MethodImpl(MethodImplOptions.InternalCall)]
        [return: MarshalAs(UnmanagedType.Interface)]
        IXpsOMFontResource GetByPartName([MarshalAs(UnmanagedType.Interface)] [In] IOpcPartUri partName);
    }
}
