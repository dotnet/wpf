// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using MS.Internal.Text.TextInterface;
using MS.Internal.TextFormatting;

namespace System.Windows.Media;

/// <summary>
/// Helper that scales a raw DWrite GlyphMetrics into em space. Used when computing ink bounding box.
/// </summary>
internal readonly struct EmGlyphMetrics
{
    internal readonly double LeftSideBearing { get; }
    internal readonly double AdvanceWidth { get; }
    internal readonly double RightSideBearing { get; }
    internal readonly double TopSideBearing { get; }
    internal readonly double AdvanceHeight { get; }
    internal readonly double BottomSideBearing { get; }
    internal readonly double Baseline { get; }

    // This will result in newobj IL due to the ref pass, we want to make sure this code gets inlined due to int->double conversions via vcvtsi2sd,
    // since JIT will only use volatile SIMD registers here and that will cause unnecessary stalls (as it doesn't play "safe" zero register either)
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal EmGlyphMetrics(scoped ref readonly GlyphMetrics glyphMetrics, double designToEm, double pixelsPerDip, TextFormattingMode textFormattingMode)
    {
        if (textFormattingMode is TextFormattingMode.Display)
        {
            AdvanceWidth = TextFormatterImp.RoundDipForDisplayMode(designToEm * glyphMetrics.AdvanceWidth, pixelsPerDip);
            AdvanceHeight = TextFormatterImp.RoundDipForDisplayMode(designToEm * glyphMetrics.AdvanceHeight, pixelsPerDip);
            LeftSideBearing = TextFormatterImp.RoundDipForDisplayMode(designToEm * glyphMetrics.LeftSideBearing, pixelsPerDip);
            RightSideBearing = TextFormatterImp.RoundDipForDisplayMode(designToEm * glyphMetrics.RightSideBearing, pixelsPerDip);
            TopSideBearing = TextFormatterImp.RoundDipForDisplayMode(designToEm * glyphMetrics.TopSideBearing, pixelsPerDip);
            BottomSideBearing = TextFormatterImp.RoundDipForDisplayMode(designToEm * glyphMetrics.BottomSideBearing, pixelsPerDip);
            Baseline = TextFormatterImp.RoundDipForDisplayMode(designToEm * GlyphTypeface.BaselineHelper(in glyphMetrics), pixelsPerDip);

            // Workaround for short or narrow glyphs - see comment in AdjustAdvanceForDisplayLayout
            AdvanceWidth = AdjustAdvanceForDisplayLayout(AdvanceWidth, LeftSideBearing, RightSideBearing);
            AdvanceHeight = AdjustAdvanceForDisplayLayout(AdvanceHeight, TopSideBearing, BottomSideBearing);
        }
        else
        {
            AdvanceWidth = designToEm * glyphMetrics.AdvanceWidth;
            AdvanceHeight = designToEm * glyphMetrics.AdvanceHeight;
            LeftSideBearing = designToEm * glyphMetrics.LeftSideBearing;
            RightSideBearing = designToEm * glyphMetrics.RightSideBearing;
            TopSideBearing = designToEm * glyphMetrics.TopSideBearing;
            BottomSideBearing = designToEm * glyphMetrics.BottomSideBearing;
            Baseline = designToEm * GlyphTypeface.BaselineHelper(in glyphMetrics);
        }
    }

    private static double AdjustAdvanceForDisplayLayout(double advance, double oneSideBearing, double otherSideBearing)
    {
        // AdvanceHeight is used to compute the bounding box. In some case, eg. the dash
        // character '-', the bounding box is computed to be empty in Display
        // TextFormattingMode (because the metrics are rounded to be pixel aligned) and so the
        // dash is not rendered!
        //
        // Thus we coerce ah to be at least 1 pixel greater than tsb + bsb to gurantee that all
        // glyphs will be rendered (with non-zero bounding box).
        //
        // Note: A side effect to this is that spaces will now be processed when rendering.
        // That is, if the bounding box was empty the rendering engine will not process the
        // text for rendering. But now even spaces will be processed but will be rendered as
        // empty space.

        // This problem also applies to the width of some characters, such as '.', ':', and 'l'
        // The fix is the same: coerce AdvanceWidth to be at least
        // LeftSideBearing + RightSideBearing + 1 pixels.

        return Math.Max(advance, oneSideBearing + otherSideBearing + 1);
    }
}
