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

    [Guid("897C86B8-8EAF-4AE3-BDDE-56419FCF4236"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    [ComImport]
    internal interface IXpsOMDictionary
    {
        void Append([In] string key, [In] IXpsOMShareable entry);

        IXpsOMDictionary Clone();

        IXpsOMShareable GetAt([In] uint index, out string key);

        IXpsOMShareable GetByKey([In] string key, [In] IXpsOMShareable beforeEntry);

        uint GetCount();

        uint GetIndex([In] IXpsOMShareable entry);

        object GetOwner();

        void InsertAt([In] uint index, [In] string key, [In] IXpsOMShareable entry);

        void RemoveAt([In] uint index);

        void SetAt([In] uint index, [In] string key, [In] IXpsOMShareable entry);
    }
}
