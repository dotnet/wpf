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

    [Guid("221D1452-331E-47C6-87E9-6CCEFB9B5BA3"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    [ComImport]
    internal interface IXpsOMCanvas : IXpsOMVisual
    {
        IXpsOMCanvas Clone();

        string GetAccessibilityLongDescription();

        string GetAccessibilityShortDescription();

        IXpsOMDictionary GetDictionary();

        IXpsOMDictionary GetDictionaryLocal();

        IXpsOMRemoteDictionaryResource GetDictionaryResource();

        int GetUseAliasedEdgeMode();

        IXpsOMVisualCollection GetVisuals();

        void SetAccessibilityLongDescription([In] string longDescription);

        void SetAccessibilityShortDescription([In] string shortDescription);

        void SetDictionaryLocal([In] IXpsOMDictionary resourceDictionary);

        void SetDictionaryResource([In] IXpsOMRemoteDictionaryResource remoteDictionaryResource);

        void SetUseAliasedEdgeMode([In] int useAliasedEdgeMode);
    }
}
