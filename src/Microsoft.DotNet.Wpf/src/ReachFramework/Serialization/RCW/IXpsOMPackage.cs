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

    [Guid("18C3DF65-81E1-4674-91DC-FC452F5A416F"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    [ComImport]
    internal interface IXpsOMPackage
    {
        IXpsOMCoreProperties GetCoreProperties();

        IOpcPartUri GetDiscardControlPartName();

        IXpsOMDocumentSequence GetDocumentSequence();

        IXpsOMImageResource GetThumbnailResource();

        void SetCoreProperties([In] IXpsOMCoreProperties coreProperties);

        void SetDiscardControlPartName([In] IOpcPartUri discardControlPartUri);

        void SetDocumentSequence([In] IXpsOMDocumentSequence documentSequence);

        void SetThumbnailResource([In] IXpsOMImageResource imageResource);

        void WriteToFile([In] string fileName, [In] ref _SECURITY_ATTRIBUTES securityAttributes, [In] uint flagsAndAttributes, [In] int optimizeMarkupSize);

        void WriteToStream([In] ISequentialStream stream, [In] int optimizeMarkupSize);
    }
}
