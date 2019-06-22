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

    [Guid("37D38BB6-3EE9-4110-9312-14B194163337"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    [ComImport]
    internal interface IXpsOMPath : IXpsOMVisual
    {
        IXpsOMPath Clone();

        string GetAccessibilityLongDescription();

        string GetAccessibilityShortDescription();

        IXpsOMBrush GetFillBrush();

        IXpsOMBrush GetFillBrushLocal();

        string GetFillBrushLookup();

        IXpsOMGeometry GetGeometry();

        IXpsOMGeometry GetGeometryLocal();

        string GetGeometryLookup();

        int GetSnapsToPixels();

        IXpsOMBrush GetStrokeBrush();

        IXpsOMBrush GetStrokeBrushLocal();

        string GetStrokeBrushLookup();

        XPS_DASH_CAP GetStrokeDashCap();

        IXpsOMDashCollection GetStrokeDashes();

        float GetStrokeDashOffset();

        XPS_LINE_CAP GetStrokeEndLineCap();

        XPS_LINE_JOIN GetStrokeLineJoin();

        float GetStrokeMiterLimit();

        XPS_LINE_CAP GetStrokeStartLineCap();

        float GetStrokeThickness();

        void SetAccessibilityLongDescription([In] string longDescription);

        void SetAccessibilityShortDescription([In] string shortDescription);

        void SetFillBrushLocal([In] IXpsOMBrush brush);

        void SetFillBrushLookup([In] string lookup);

        void SetGeometryLocal([In] IXpsOMGeometry geometry);

        void SetGeometryLookup([In] string lookup);

        void SetSnapsToPixels([In] int snapsToPixels);

        void SetStrokeBrushLocal([In] IXpsOMBrush brush);

        void SetStrokeBrushLookup([In] string lookup);

        void SetStrokeDashCap([In][ComAliasName("System.Windows.Xps.Serialization.RCW.XPS_DASH_CAP")] XPS_DASH_CAP strokeDashCap);

        void SetStrokeDashOffset([In] float strokeDashOffset);

        void SetStrokeEndLineCap([In][ComAliasName("System.Windows.Xps.Serialization.RCW.XPS_LINE_CAP")] XPS_LINE_CAP strokeEndLineCap);

        void SetStrokeLineJoin([In][ComAliasName("System.Windows.Xps.Serialization.RCW.XPS_LINE_JOIN")] XPS_LINE_JOIN strokeLineJoin);

        void SetStrokeMiterLimit([In] float strokeMiterLimit);

        void SetStrokeStartLineCap([In][ComAliasName("System.Windows.Xps.Serialization.RCW.XPS_LINE_CAP")] XPS_LINE_CAP strokeStartLineCap);

        void SetStrokeThickness([In] float strokeThickness);
    }
}
