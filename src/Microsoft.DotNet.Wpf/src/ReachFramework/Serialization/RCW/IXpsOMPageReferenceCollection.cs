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

    [Guid("CA16BA4D-E7B9-45C5-958B-F98022473745"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    [ComImport]
    internal interface IXpsOMPageReferenceCollection
    {
        void Append([In] IXpsOMPageReference pageReference);

        IXpsOMPageReference GetAt([In] uint index);

        uint GetCount();

        void InsertAt([In] uint index, [In] IXpsOMPageReference pageReference);

        void RemoveAt([In] uint index);

        void SetAt([In] uint index, [In] IXpsOMPageReference pageReference);
    }
}
