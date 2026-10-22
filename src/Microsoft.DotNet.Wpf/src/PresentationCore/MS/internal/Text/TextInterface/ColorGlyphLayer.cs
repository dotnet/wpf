// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace MS.Internal.Text.TextInterface
{
    /// <summary>
    /// Represents a single color layer from a COLR v0 color glyph run decomposition.
    /// </summary>
    internal struct ColorGlyphLayer
    {
        public ushort[] GlyphIndices;
        public float[] GlyphAdvances;
        public float[] GlyphOffsets;    // Interleaved (advanceOffset, ascenderOffset) pairs; may be null.
        public float BaselineOriginX;
        public float BaselineOriginY;
        public float ColorR;
        public float ColorG;
        public float ColorB;
        public float ColorA;
        public bool UseForegroundColor;
    }
}
