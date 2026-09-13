// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;

namespace MS.Internal.Interop.DWrite
{
    [StructLayout(LayoutKind.Sequential)]
    internal struct DWRITE_GLYPH_OFFSET
    {
        public float advanceOffset;
        public float ascenderOffset;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal unsafe struct DWRITE_GLYPH_RUN
    {
        public void* fontFace;          // IDWriteFontFace*
        public float fontEmSize;
        public uint glyphCount;
        public ushort* glyphIndices;
        public float* glyphAdvances;
        public DWRITE_GLYPH_OFFSET* glyphOffsets;
        public int isSideways;          // BOOL (4 bytes)
        public uint bidiLevel;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct DWRITE_COLOR_F
    {
        public float r;
        public float g;
        public float b;
        public float a;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal unsafe struct DWRITE_COLOR_GLYPH_RUN
    {
        public DWRITE_GLYPH_RUN glyphRun;
        public void* glyphRunDescription;   // DWRITE_GLYPH_RUN_DESCRIPTION* (nullable)
        public float baselineOriginX;
        public float baselineOriginY;
        public DWRITE_COLOR_F runColor;
        public ushort paletteIndex;
    }
}
