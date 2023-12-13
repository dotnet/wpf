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

    [Guid("A39EE748-6A27-4817-A6F2-13914BEF5890"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    [ComImport]
    internal interface IUri
    {
        void GetAbsoluteUri(out string pbstrAbsoluteUri);

        void GetAuthority(out string pbstrAuthority);

        void GetDisplayUri(out string pbstrDisplayString);

        void GetDomain(out string pbstrDomain);

        void GetExtension(out string pbstrExtension);

        void GetFragment(out string pbstrFragment);

        void GetHost(out string pbstrHost);

        void GetHostType(out uint pdwHostType);

        void GetPassword(out string pbstrPassword);

        void GetPath(out string pbstrPath);

        void GetPathAndQuery(out string pbstrPathAndQuery);

        void GetPort(out uint pdwPort);

        void GetProperties(out uint pdwFlags);

        void GetPropertyBSTR([In][ComAliasName("System.Windows.Xps.Serialization.RCW.Uri_PROPERTY")] Uri_PROPERTY uriProp, out string pbstrProperty, [In] uint dwFlags);

        void GetPropertyDWORD([In][ComAliasName("System.Windows.Xps.Serialization.RCW.Uri_PROPERTY")] Uri_PROPERTY uriProp, out uint pdwProperty, [In] uint dwFlags);

        void GetPropertyLength([In][ComAliasName("System.Windows.Xps.Serialization.RCW.Uri_PROPERTY")] Uri_PROPERTY uriProp, out uint pcchProperty, [In] uint dwFlags);

        void GetQuery(out string pbstrQuery);

        void GetRawUri(out string pbstrRawUri);

        void GetScheme(out uint pdwScheme);

        void GetSchemeName(out string pbstrSchemeName);

        void GetUserInfo(out string pbstrUserInfo);

        void GetUserName(out string pbstrUserName);

        void GetZone(out uint pdwZone);

        void HasProperty([In][ComAliasName("System.Windows.Xps.Serialization.RCW.Uri_PROPERTY")] Uri_PROPERTY uriProp, out int pfHasProperty);

        void IsEqual([In] IUri pUri, out int pfEqual);
    }
}
