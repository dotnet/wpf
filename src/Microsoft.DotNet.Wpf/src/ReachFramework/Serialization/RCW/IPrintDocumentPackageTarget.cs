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
    /// RCW for documenttarget.idl found in Windows SDK
    /// This is generated code with minor manual edits. 
    /// i.  Generate TLB
    ///      MIDL /TLB documenttarget.tlb documenttarget.IDL //documenttarget.IDL found in Windows SDK
    /// ii. Generate RCW in a DLL
    ///      TLBIMP documenttarget.tlb // Generates documenttarget.dll
    /// iii.Decompile the DLL and copy out the RCW by hand.
    ///      ILDASM documenttarget.dll
    /// </summary>
    
    [Guid("1B8EFEC4-3019-4C27-964E-367202156906"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    [ComImport]
    internal interface IPrintDocumentPackageTarget
    {
        [MethodImpl(MethodImplOptions.InternalCall)]
        void GetPackageTargetTypes(out uint targetCount, [MarshalAs(UnmanagedType.LPStruct)] out Guid targetTypes);

        [MethodImpl(MethodImplOptions.InternalCall)]
        void GetPackageTarget([In] ref Guid guidTargetType, [In] ref Guid riid, out IntPtr ppvTarget);

        [MethodImpl(MethodImplOptions.InternalCall)]
        void Cancel();
    }
}
