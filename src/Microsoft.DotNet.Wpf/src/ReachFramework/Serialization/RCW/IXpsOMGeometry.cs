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

    [Guid("64FCF3D7-4D58-44BA-AD73-A13AF6492072"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    [ComImport]
    internal interface IXpsOMGeometry : IXpsOMShareable
    {
        IXpsOMGeometry Clone();

        IXpsOMGeometryFigureCollection GetFigures();

        XPS_FILL_RULE GetFillRule();

        IXpsOMMatrixTransform GetTransform();

        IXpsOMMatrixTransform GetTransformLocal();

        string GetTransformLookup();

        void SetFillRule([In][ComAliasName("System.Windows.Xps.Serialization.RCW.XPS_FILL_RULE")] XPS_FILL_RULE fillRule);

        void SetTransformLocal([In] IXpsOMMatrixTransform transform);

        void SetTransformLookup([In] string lookup);
    }
}
