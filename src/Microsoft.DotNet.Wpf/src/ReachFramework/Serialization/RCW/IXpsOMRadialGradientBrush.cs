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

    [Guid("75F207E5-08BF-413C-96B1-B82B4064176B"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    [ComImport]
    internal interface IXpsOMRadialGradientBrush : IXpsOMGradientBrush
    {
        IXpsOMRadialGradientBrush Clone();

        XPS_POINT GetCenter();

        XPS_POINT GetGradientOrigin();

        XPS_SIZE GetRadiiSizes();

        void SetCenter([In][ComAliasName("System.Windows.Xps.Serialization.RCW.XPS_POINT")] ref XPS_POINT center);

        void SetGradientOrigin([In][ComAliasName("System.Windows.Xps.Serialization.RCW.XPS_POINT")] ref XPS_POINT origin);

        void SetRadiiSizes([In][ComAliasName("System.Windows.Xps.Serialization.RCW.XPS_SIZE")] ref XPS_SIZE radiiSizes);
    }
}
