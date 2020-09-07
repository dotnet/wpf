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

    [Guid("ED90C8AD-5C34-4D05-A1EC-0E8A9B3AD7AF"), TypeLibType(TypeLibTypeFlags.FDual | TypeLibTypeFlags.FNonExtensible | TypeLibTypeFlags.FDispatchable)]
    [ComImport]
    internal interface IPrintDocumentPackageStatusEvent
    {
        [DispId(1)]
        [MethodImpl(MethodImplOptions.InternalCall)]
        void PackageStatusUpdated([ComAliasName("PrintDocumentTargetLib.PrintDocumentPackageStatus")] [In] ref PrintDocumentPackageStatus PackageStatus);
    }
}
