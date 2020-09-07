// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
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

    [Guid("BC9C1B9B-D62C-49EB-AEF0-3B4E0B28EBED"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown), TypeLibType(TypeLibTypeFlags.FNonExtensible)]
    [ComImport]
    internal interface IOpcUri : IUri
    {
        IOpcPartUri CombinePartUri([In] IUri relativeUri);

        IOpcPartUri GetRelationshipsPartUri();

        IUri GetRelativeUri([In] IOpcPartUri targetPartUri);
    }
}
