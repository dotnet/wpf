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

    [Guid("5CF4F5CC-3969-49B5-A70A-5550B618FE49"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    [ComImport]
    internal interface IXpsOMGradientStop
    {
        IXpsOMGradientStop Clone();

        IXpsOMColorProfileResource GetColor([ComAliasName("System.Windows.Xps.Serialization.RCW.XPS_COLOR")] out XPS_COLOR color);

        float GetOffset();

        IXpsOMGradientBrush GetOwner();

        void SetColor([In][ComAliasName("System.Windows.Xps.Serialization.RCW.XPS_COLOR")] ref XPS_COLOR color, [In] IXpsOMColorProfileResource colorProfile);

        void SetOffset([In] float offset);
    }
}
