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

    [Guid("2C2C94CB-AC5F-4254-8EE9-23948309D9F0"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    [ComImport]
    internal interface IXpsOMDocument : IXpsOMPart
    {
        IXpsOMDocument Clone();

        IXpsOMDocumentStructureResource GetDocumentStructureResource();

        IXpsOMDocumentSequence GetOwner();

        IXpsOMPageReferenceCollection GetPageReferences();

        IXpsOMPrintTicketResource GetPrintTicketResource();

        IXpsOMSignatureBlockResourceCollection GetSignatureBlockResources();

        void SetDocumentStructureResource([In] IXpsOMDocumentStructureResource documentStructureResource);
        
        void SetPrintTicketResource([In] IXpsOMPrintTicketResource printTicketResource);
    }
}
