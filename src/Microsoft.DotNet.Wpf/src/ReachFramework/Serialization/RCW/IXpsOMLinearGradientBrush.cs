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

    [Guid("005E279F-C30D-40FF-93EC-1950D3C528DB"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    [ComImport]
    internal interface IXpsOMLinearGradientBrush : IXpsOMGradientBrush
    {
        IXpsOMLinearGradientBrush Clone();

        XPS_POINT GetEndPoint();

        XPS_POINT GetStartPoint();

        void SetEndPoint([In][ComAliasName("System.Windows.Xps.Serialization.RCW.XPS_POINT")] ref XPS_POINT endPoint);

        void SetStartPoint([In][ComAliasName("System.Windows.Xps.Serialization.RCW.XPS_POINT")] ref XPS_POINT startPoint);
    }
}
