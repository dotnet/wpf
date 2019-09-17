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

    [Guid("AB8F5D8E-351B-4D33-AAED-FA56F0022931"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    [ComImport]
    internal interface IXpsOMSignatureBlockResourceCollection
    {
        void Append([In] IXpsOMSignatureBlockResource signatureBlockResource);

        IXpsOMSignatureBlockResource GetAt([In] uint index);

        IXpsOMSignatureBlockResource GetByPartName([In] IOpcPartUri partName);

        uint GetCount();

        void InsertAt([In] uint index, [In] IXpsOMSignatureBlockResource signatureBlockResource);

        void RemoveAt([In] uint index);

        void SetAt([In] uint index, [In] IXpsOMSignatureBlockResource signatureBlockResource);
    }
}
