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

    [Guid("0FC2328D-D722-4A54-B2EC-BE90218A789E"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    [ComImport]
    internal interface IXpsOMTileBrush : IXpsOMBrush
    {

        XPS_TILE_MODE GetTileMode();

        IXpsOMMatrixTransform GetTransform();

        IXpsOMMatrixTransform GetTransformLocal();

        string GetTransformLookup();
        
        XPS_RECT GetViewbox();

        XPS_RECT GetViewport();

        void SetTileMode([In][ComAliasName("System.Windows.Xps.Serialization.RCW.XPS_TILE_MODE")] XPS_TILE_MODE tileMode);

        void SetTransformLocal([In] IXpsOMMatrixTransform transform);

        void SetTransformLookup([In] string key);

        void SetViewbox([In][ComAliasName("System.Windows.Xps.Serialization.RCW.XPS_RECT")] ref XPS_RECT viewbox);

        void SetViewport([In][ComAliasName("System.Windows.Xps.Serialization.RCW.XPS_RECT")] ref XPS_RECT viewport);
    }
}
