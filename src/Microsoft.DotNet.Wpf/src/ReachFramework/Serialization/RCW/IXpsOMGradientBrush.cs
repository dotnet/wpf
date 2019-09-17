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

    [Guid("EDB59622-61A2-42C3-BACE-ACF2286C06BF"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    [ComImport]
    internal interface IXpsOMGradientBrush : IXpsOMBrush
    {
        XPS_COLOR_INTERPOLATION GetColorInterpolationMode();

        IXpsOMGradientStopCollection GetGradientStops();

        XPS_SPREAD_METHOD GetSpreadMethod();

        IXpsOMMatrixTransform GetTransform();

        IXpsOMMatrixTransform GetTransformLocal();

        string GetTransformLookup();

        void SetColorInterpolationMode([In][ComAliasName("System.Windows.Xps.Serialization.RCW.XPS_COLOR_INTERPOLATION")] XPS_COLOR_INTERPOLATION colorInterpolationMode);
        
        void SetSpreadMethod([In][ComAliasName("System.Windows.Xps.Serialization.RCW.XPS_SPREAD_METHOD")] XPS_SPREAD_METHOD spreadMethod);

        void SetTransformLocal([In] IXpsOMMatrixTransform transform);

        void SetTransformLookup([In] string key);
    }
}
