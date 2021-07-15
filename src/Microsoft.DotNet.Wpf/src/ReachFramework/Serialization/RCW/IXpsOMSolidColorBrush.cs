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

    [Guid("A06F9F05-3BE9-4763-98A8-094FC672E488"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    [ComImport]
    internal interface IXpsOMSolidColorBrush : IXpsOMBrush
    {
        IXpsOMSolidColorBrush Clone();

        IXpsOMColorProfileResource GetColor([ComAliasName("System.Windows.Xps.Serialization.RCW.XPS_COLOR")] out XPS_COLOR color);

        void SetColor([In][ComAliasName("System.Windows.Xps.Serialization.RCW.XPS_COLOR")] ref XPS_COLOR color, [In] IXpsOMColorProfileResource colorProfile);

    }
}
