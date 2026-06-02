// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Windows.Media;

/// <summary>
/// Tests for COLR v0 color emoji rendering support:
///   - GlyphTypeface.IsColorFont detection via the COLR table
///   - GlyphRun.TryBuildColorGlyphDrawing decomposition into color layers
///   - Correct sRGB color interpretation (not linear/scRGB)
///
/// Requires Segoe UI Emoji (standard on Windows 10+).
/// </summary>
public class ColorEmojiTests
{
    private const string SegoeUIEmojiPath = @"C:\Windows\Fonts\seguiemj.ttf";
    private const string SegoeUIPath = @"C:\Windows\Fonts\segoeui.ttf";

    private static bool HasSegoeUIEmoji => File.Exists(SegoeUIEmojiPath);
    private static bool HasSegoeUI => File.Exists(SegoeUIPath);

    [Fact]
    [Trait("Category", "ColorEmoji")]
    public void IsColorFont_SegoeUIEmoji_ReturnsTrue()
    {
        if (!HasSegoeUIEmoji) return;

        var typeface = new GlyphTypeface(new Uri(SegoeUIEmojiPath));
        Assert.True(typeface.IsColorFont);
    }

    [Fact]
    [Trait("Category", "ColorEmoji")]
    public void IsColorFont_SegoeUI_ReturnsFalse()
    {
        if (!HasSegoeUI) return;

        var typeface = new GlyphTypeface(new Uri(SegoeUIPath));
        Assert.False(typeface.IsColorFont);
    }

    [Fact]
    [Trait("Category", "ColorEmoji")]
    public void TryBuildColorGlyphDrawing_GrinningFace_ReturnsMultiLayerDrawingGroup()
    {
        if (!HasSegoeUIEmoji) return;

        var typeface = new GlyphTypeface(new Uri(SegoeUIEmojiPath));

        // U+1F600 Grinning Face
        if (!typeface.CharacterToGlyphMap.TryGetValue(0x1F600, out ushort glyphIndex)) return;

        var glyphRun = new GlyphRun(
            typeface,
            bidiLevel: 0,
            isSideways: false,
            renderingEmSize: 20.0,
            pixelsPerDip: 96.0f,
            glyphIndices: new ushort[] { glyphIndex },
            baselineOrigin: new Point(0, 20),
            advanceWidths: new double[] { 20.0 },
            glyphOffsets: null,
            characters: null,
            deviceFontName: null,
            clusterMap: null,
            caretStops: null,
            language: null);

        Drawing drawing = glyphRun.TryBuildColorGlyphDrawing(Brushes.Black);

        Assert.NotNull(drawing);
        Assert.IsType<DrawingGroup>(drawing);

        var group = (DrawingGroup)drawing;
        Assert.True(group.Children.Count > 1,
            $"Expected multiple color layers, got {group.Children.Count}");
    }

    [Fact]
    [Trait("Category", "ColorEmoji")]
    public void TryBuildColorGlyphDrawing_NonColorFont_ReturnsNull()
    {
        if (!HasSegoeUI) return;

        var typeface = new GlyphTypeface(new Uri(SegoeUIPath));

        if (!typeface.CharacterToGlyphMap.TryGetValue((int)'A', out ushort glyphIndex)) return;

        var glyphRun = new GlyphRun(
            typeface,
            bidiLevel: 0,
            isSideways: false,
            renderingEmSize: 20.0,
            pixelsPerDip: 96.0f,
            glyphIndices: new ushort[] { glyphIndex },
            baselineOrigin: new Point(0, 20),
            advanceWidths: new double[] { 20.0 },
            glyphOffsets: null,
            characters: null,
            deviceFontName: null,
            clusterMap: null,
            caretStops: null,
            language: null);

        Drawing drawing = glyphRun.TryBuildColorGlyphDrawing(Brushes.Black);

        Assert.Null(drawing);
    }

    [Fact]
    [Trait("Category", "ColorEmoji")]
    public void TryBuildColorGlyphDrawing_LayerBrushesHaveVibrantColors()
    {
        if (!HasSegoeUIEmoji)
            return;

        var typeface = new GlyphTypeface(new Uri(SegoeUIEmojiPath));

        var result = typeface.CharacterToGlyphMap.TryGetValue(0x1F600, out ushort glyphIndex);
        if (!result)
            return;

        var glyphRun = new GlyphRun(
            typeface,
            bidiLevel: 0,
            isSideways: false,
            renderingEmSize: 20.0,
            pixelsPerDip: 96.0f,
            glyphIndices: new ushort[] { glyphIndex },
            baselineOrigin: new Point(0, 20),
            advanceWidths: new double[] { 20.0 },
            glyphOffsets: null,
            characters: null,
            deviceFontName: null,
            clusterMap: null,
            caretStops: null,
            language: null);

        Drawing drawing = glyphRun.TryBuildColorGlyphDrawing(Brushes.Black);
        Assert.NotNull(drawing);
        var group = (DrawingGroup)drawing;

        // The grinning face emoji has vibrant warm layers (orange/yellow in sRGB).
        // If colors were wrongly interpreted as linear (scRGB via FromScRgb), the sRGB
        // byte values would be much lower, producing washed-out colors.
        // We look for any layer with a warm, saturated color (R > 200, B < 100).
        bool foundVibrantWarm = false;
        foreach (Drawing child in group.Children)
        {
            if (child is GeometryDrawing gd && gd.Brush is SolidColorBrush scb)
            {
                Color c = scb.Color;
                if (c.R > 200 && c.B < 100)
                {
                    foundVibrantWarm = true;
                    break;
                }
            }
        }

        Assert.True(foundVibrantWarm,
            "Expected at least one layer with a vibrant warm color (sRGB R>200, B<100). " +
            "If colors appear washed out, FromScRgb may have been used instead of FromArgb.");
    }
}
