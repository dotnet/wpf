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

    [Guid("819B3199-0A5A-4B64-BEC7-A9E17E780DE2"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    [ComImport]
    internal interface IXpsOMGlyphs : IXpsOMVisual
    {
        IXpsOMGlyphs Clone();

        uint GetBidiLevel();

        string GetDeviceFontName();

        IXpsOMBrush GetFillBrush();

        IXpsOMBrush GetFillBrushLocal();

        string GetFillBrushLookup();

        short GetFontFaceIndex();

        float GetFontRenderingEmSize();

        IXpsOMFontResource GetFontResource();

        uint GetGlyphIndexCount();

        void GetGlyphIndices([In][Out] ref uint indexCount, [In][Out][ComAliasName("System.Windows.Xps.Serialization.RCW.XPS_GLYPH_INDEX")] ref XPS_GLYPH_INDEX glyphIndices);

        uint GetGlyphMappingCount();

        void GetGlyphMappings([In][Out] ref uint glyphMappingCount, [In][Out][ComAliasName("System.Windows.Xps.Serialization.RCW.XPS_GLYPH_MAPPING")] ref XPS_GLYPH_MAPPING glyphMappings);

        IXpsOMGlyphsEditor GetGlyphsEditor();

        int GetIsSideways();

        XPS_POINT GetOrigin();

        uint GetProhibitedCaretStopCount();

        void GetProhibitedCaretStops([In][Out] ref uint prohibitedCaretStopCount, out uint prohibitedCaretStops);

        XPS_STYLE_SIMULATION GetStyleSimulations();

        string GetUnicodeString();

        void SetFillBrushLocal([In] IXpsOMBrush fillBrush);

        void SetFillBrushLookup([In] string key);

        void SetFontFaceIndex([In] short fontFaceIndex);

        void SetFontRenderingEmSize([In] float fontRenderingEmSize);

        void SetFontResource([In] IXpsOMFontResource fontResource);

        void SetOrigin([In][ComAliasName("System.Windows.Xps.Serialization.RCW.XPS_POINT")] ref XPS_POINT origin);

        void SetStyleSimulations([In][ComAliasName("System.Windows.Xps.Serialization.RCW.XPS_STYLE_SIMULATION")] XPS_STYLE_SIMULATION styleSimulations);
    }
}
