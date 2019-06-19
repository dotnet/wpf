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

    [Guid("A5AB8616-5B16-4B9F-9629-89B323ED7909"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    [ComImport]
    internal interface IXpsOMGlyphsEditor
    {
        void ApplyEdits();

        uint GetBidiLevel();

        string GetDeviceFontName();

        uint GetGlyphIndexCount();

        void GetGlyphIndices([In][Out] ref uint indexCount, [ComAliasName("System.Windows.Xps.Serialization.RCW.XPS_GLYPH_INDEX")] out XPS_GLYPH_INDEX glyphIndices);

        uint GetGlyphMappingCount();

        void GetGlyphMappings([In][Out] ref uint glyphMappingCount, [ComAliasName("System.Windows.Xps.Serialization.RCW.XPS_GLYPH_MAPPING")] out XPS_GLYPH_MAPPING glyphMappings);

        int GetIsSideways();

        uint GetProhibitedCaretStopCount();

        void GetProhibitedCaretStops([In][Out] ref uint count, out uint prohibitedCaretStops);

        string GetUnicodeString();

        void SetBidiLevel([In] uint bidiLevel);

        void SetDeviceFontName([In] string deviceFontName);

        void SetGlyphIndices([In] uint indexCount, [In][ComAliasName("System.Windows.Xps.Serialization.RCW.XPS_GLYPH_INDEX")] ref XPS_GLYPH_INDEX glyphIndices);

        void SetGlyphMappings([In] uint glyphMappingCount, [In][ComAliasName("System.Windows.Xps.Serialization.RCW.XPS_GLYPH_MAPPING")] ref XPS_GLYPH_MAPPING glyphMappings);

        void SetIsSideways([In] int isSideways);

        void SetProhibitedCaretStops([In] uint count, [In] ref uint prohibitedCaretStops);

        void SetUnicodeString([In] string unicodeString);
    }
}
