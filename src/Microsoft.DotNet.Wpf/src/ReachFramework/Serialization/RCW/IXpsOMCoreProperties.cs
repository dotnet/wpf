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

    [Guid("3340FE8F-4027-4AA1-8F5F-D35AE45FE597"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    [ComImport]
    internal interface IXpsOMCoreProperties : IXpsOMPart
    {
        IXpsOMCoreProperties Clone();

        string GetCategory();

        string GetContentStatus();

        string GetContentType();

        _SYSTEMTIME GetCreated();

        string GetCreator();

        string GetDescription();

        string GetIdentifier();

        string GetKeywords();

        string GetLanguage();

        string GetLastModifiedBy();

        _SYSTEMTIME GetLastPrinted();

        _SYSTEMTIME GetModified();

        IXpsOMPackage GetOwner();

        string GetRevision();

        string GetSubject();

        string GetTitle();

        string GetVersion();

        void SetCategory([In] string category);

        void SetContentStatus([In] string contentStatus);

        void SetContentType([In] string contentType);

        void SetCreated([In] ref _SYSTEMTIME created);

        void SetCreator([In] string creator);

        void SetDescription([In] string description);

        void SetIdentifier([In] string identifier);

        void SetKeywords([In] string keywords);

        void SetLanguage([In] string language);

        void SetLastModifiedBy([In] string lastModifiedBy);

        void SetLastPrinted([In] ref _SYSTEMTIME lastPrinted);

        void SetModified([In] ref _SYSTEMTIME modified);

        void SetRevision([In] string revision);

        void SetSubject([In] string subject);

        void SetTitle([In] string title);

        void SetVersion([In] string version);
    }
}
