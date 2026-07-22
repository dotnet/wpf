// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace MS.Internal.Text.TextInterface.Tests;

/// <summary>
/// Tests designed to improve code coverage for TtfDelta font subsetting.
/// These tests exercise different font types and glyph ranges to trigger
/// various code paths in the TtfDelta subsetter.
/// </summary>
public class TtfDeltaCoverageTests
{
    // Font paths for different test scenarios
    private static readonly string s_simSunPath = Path.Combine(TestHelpers.FontsDirectory, "simsun.ttc");
    private static readonly string s_msGothicPath = Path.Combine(TestHelpers.FontsDirectory, "msgothic.ttc");
    private static readonly string s_segoeUIPath = Path.Combine(TestHelpers.FontsDirectory, "segoeui.ttf");
    private static readonly string s_segoeUIEmojiPath = Path.Combine(TestHelpers.FontsDirectory, "seguiemj.ttf");
    private static readonly string s_malgunPath = Path.Combine(TestHelpers.FontsDirectory, "malgun.ttf");
    private static readonly string s_gabriolaPath = Path.Combine(TestHelpers.FontsDirectory, "Gabriola.ttf");
    private static readonly string s_timesPath = Path.Combine(TestHelpers.FontsDirectory, "times.ttf");
    private static readonly string s_courierPath = Path.Combine(TestHelpers.FontsDirectory, "cour.ttf");
    private static readonly string s_tahomaPath = Path.Combine(TestHelpers.FontsDirectory, "tahoma.ttf");
    private static readonly string s_verdanaPath = Path.Combine(TestHelpers.FontsDirectory, "verdana.ttf");
    private static readonly string s_georgiaPath = Path.Combine(TestHelpers.FontsDirectory, "georgia.ttf");
    private static readonly string s_consolasPath = Path.Combine(TestHelpers.FontsDirectory, "consola.ttf");
    private static readonly string s_calibriPath = Path.Combine(TestHelpers.FontsDirectory, "calibri.ttf");
    private static readonly string s_comicSansPath = Path.Combine(TestHelpers.FontsDirectory, "comic.ttf");
    private static readonly string s_impactPath = Path.Combine(TestHelpers.FontsDirectory, "impact.ttf");
    private static readonly string s_symbolPath = Path.Combine(TestHelpers.FontsDirectory, "symbol.ttf");
    private static readonly string s_wingdingsPath = Path.Combine(TestHelpers.FontsDirectory, "wingding.ttf");
    private static readonly string s_webdingsPath = Path.Combine(TestHelpers.FontsDirectory, "webdings.ttf");
    private static readonly string s_segoeSymbolPath = Path.Combine(TestHelpers.FontsDirectory, "seguisym.ttf");

    #region TTC (TrueType Collection) Tests - Exercises directory offset handling

    /// <summary>
    /// Reads the TTC header to get the offset for a specific font index.
    /// TTC format: 'ttcf' tag (4 bytes), version (4 bytes), numFonts (4 bytes), offset[numFonts] (4 bytes each)
    /// </summary>
    private static int GetTTCFontOffset(byte[] fontData, int fontIndex)
    {
        // Check for 'ttcf' signature
        if (fontData.Length < 12)
            return 0;
        
        uint tag = (uint)((fontData[0] << 24) | (fontData[1] << 16) | (fontData[2] << 8) | fontData[3]);
        if (tag != 0x74746366) // 'ttcf'
            return 0;
        
        uint numFonts = (uint)((fontData[8] << 24) | (fontData[9] << 16) | (fontData[10] << 8) | fontData[11]);
        if (fontIndex >= numFonts)
            return 0;
        
        // Offset table starts at byte 12
        int offsetPos = 12 + (fontIndex * 4);
        if (offsetPos + 4 > fontData.Length)
            return 0;
        
        return (int)((fontData[offsetPos] << 24) | (fontData[offsetPos + 1] << 16) | 
                     (fontData[offsetPos + 2] << 8) | fontData[offsetPos + 3]);
    }

    [Fact]
    public void ComputeSubset_CambriaTTC_FirstFont_ShouldWork()
    {
        Assert.SkipUnless(File.Exists(TestHelpers.CambriaPath), "Cambria TTC not found");
        var fontData = File.ReadAllBytes(TestHelpers.CambriaPath);
        var uri = new Uri(TestHelpers.CambriaPath);

        // Get the proper offset for the first font in the TTC
        int fontOffset = GetTTCFontOffset(fontData, 0);
        ushort[] glyphArray = Enumerable.Range(0, 50).Select(i => (ushort)i).ToArray();

        unsafe
        {
            fixed (byte* pFontData = fontData)
            {
                var result = TrueTypeSubsetter.ComputeSubset(pFontData, fontData.Length, uri, fontOffset, glyphArray);
                result.Should().NotBeNull();
                result.Length.Should().BeGreaterThan(0);
            }
        }
    }

    [Fact]
    public void ComputeSubset_SimSunTTC_WithCJKGlyphs_ShouldWork()
    {
        Assert.SkipUnless(File.Exists(s_simSunPath), "SimSun TTC not found");
        var fontData = File.ReadAllBytes(s_simSunPath);
        var uri = new Uri(s_simSunPath);

        // Get the proper offset for the first font in the TTC
        int fontOffset = GetTTCFontOffset(fontData, 0);
        // CJK fonts have many glyphs - request a range that exercises CMAP handling
        ushort[] glyphArray = Enumerable.Range(0, 500).Select(i => (ushort)i).ToArray();

        unsafe
        {
            fixed (byte* pFontData = fontData)
            {
                var result = TrueTypeSubsetter.ComputeSubset(pFontData, fontData.Length, uri, fontOffset, glyphArray);
                result.Should().NotBeNull();
            }
        }
    }

    [Fact]
    public void ComputeSubset_MsGothicTTC_ShouldWork()
    {
        Assert.SkipUnless(File.Exists(s_msGothicPath), "MS Gothic TTC not found");
        var fontData = File.ReadAllBytes(s_msGothicPath);
        var uri = new Uri(s_msGothicPath);

        // Get the proper offset for the first font in the TTC
        int fontOffset = GetTTCFontOffset(fontData, 0);
        ushort[] glyphArray = Enumerable.Range(0, 200).Select(i => (ushort)i).ToArray();

        unsafe
        {
            fixed (byte* pFontData = fontData)
            {
                var result = TrueTypeSubsetter.ComputeSubset(pFontData, fontData.Length, uri, fontOffset, glyphArray);
                result.Should().NotBeNull();
            }
        }
    }

    [Fact]
    public void ComputeSubset_CambriaTTC_SecondFont_ShouldWork()
    {
        Assert.SkipUnless(File.Exists(TestHelpers.CambriaPath), "Cambria TTC not found");
        var fontData = File.ReadAllBytes(TestHelpers.CambriaPath);
        var uri = new Uri(TestHelpers.CambriaPath);

        // Get the proper offset for the second font in the TTC (Cambria Math)
        int fontOffset = GetTTCFontOffset(fontData, 1);
        Assert.SkipUnless(fontOffset > 0, "Second font not found in TTC");
        ushort[] glyphArray = Enumerable.Range(0, 100).Select(i => (ushort)i).ToArray();

        unsafe
        {
            fixed (byte* pFontData = fontData)
            {
                var result = TrueTypeSubsetter.ComputeSubset(pFontData, fontData.Length, uri, fontOffset, glyphArray);
                result.Should().NotBeNull();
                result.Length.Should().BeGreaterThan(0);
            }
        }
    }

    #endregion

    #region High Glyph Index Tests - Exercises extended glyph handling

    [Fact]
    public void ComputeSubset_WithHighGlyphIndices_ShouldWork()
    {
        Assert.SkipUnless(File.Exists(TestHelpers.ArialPath), "Arial not found");
        var fontData = File.ReadAllBytes(TestHelpers.ArialPath);
        var uri = new Uri(TestHelpers.ArialPath);

        // Request high glyph indices - exercises different code paths
        ushort[] glyphArray = [0, 100, 500, 1000, 1500, 2000];

        unsafe
        {
            fixed (byte* pFontData = fontData)
            {
                var result = TrueTypeSubsetter.ComputeSubset(pFontData, fontData.Length, uri, 0, glyphArray);
                result.Should().NotBeNull();
            }
        }
    }

    [Fact]
    public void ComputeSubset_WithSparseGlyphIndices_ShouldWork()
    {
        Assert.SkipUnless(File.Exists(TestHelpers.ArialPath), "Arial not found");
        var fontData = File.ReadAllBytes(TestHelpers.ArialPath);
        var uri = new Uri(TestHelpers.ArialPath);

        // Sparse glyph indices - exercises CMAP lookup
        ushort[] glyphArray = [0, 10, 50, 100, 200, 500, 1000, 2000, 3000];

        unsafe
        {
            fixed (byte* pFontData = fontData)
            {
                var result = TrueTypeSubsetter.ComputeSubset(pFontData, fontData.Length, uri, 0, glyphArray);
                result.Should().NotBeNull();
            }
        }
    }

    #endregion

    #region Large Glyph Count Tests - Exercises table compression and optimization

    [Fact]
    public void ComputeSubset_WithManyGlyphs_ShouldWork()
    {
        Assert.SkipUnless(File.Exists(TestHelpers.ArialPath), "Arial not found");
        var fontData = File.ReadAllBytes(TestHelpers.ArialPath);
        var uri = new Uri(TestHelpers.ArialPath);

        // Request many glyphs to exercise table processing
        ushort[] glyphArray = Enumerable.Range(0, 1000).Select(i => (ushort)i).ToArray();

        unsafe
        {
            fixed (byte* pFontData = fontData)
            {
                var result = TrueTypeSubsetter.ComputeSubset(pFontData, fontData.Length, uri, 0, glyphArray);
                result.Should().NotBeNull();
            }
        }
    }

    [Fact]
    public void ComputeSubset_WithVeryManyGlyphs_ShouldWork()
    {
        Assert.SkipUnless(File.Exists(TestHelpers.ArialPath), "Arial not found");
        var fontData = File.ReadAllBytes(TestHelpers.ArialPath);
        var uri = new Uri(TestHelpers.ArialPath);

        // Even more glyphs - may trigger ERR_WOULD_GROW path
        ushort[] glyphArray = Enumerable.Range(0, 2500).Select(i => (ushort)i).ToArray();

        unsafe
        {
            fixed (byte* pFontData = fontData)
            {
                var result = TrueTypeSubsetter.ComputeSubset(pFontData, fontData.Length, uri, 0, glyphArray);
                result.Should().NotBeNull();
            }
        }
    }

    [Fact]
    public void ComputeSubset_CJKFont_WithManyGlyphs_ShouldWork()
    {
        Assert.SkipUnless(File.Exists(s_malgunPath), "Malgun Gothic not found");
        var fontData = File.ReadAllBytes(s_malgunPath);
        var uri = new Uri(s_malgunPath);

        // Korean font with many glyphs
        ushort[] glyphArray = Enumerable.Range(0, 2000).Select(i => (ushort)i).ToArray();

        unsafe
        {
            fixed (byte* pFontData = fontData)
            {
                var result = TrueTypeSubsetter.ComputeSubset(pFontData, fontData.Length, uri, 0, glyphArray);
                result.Should().NotBeNull();
            }
        }
    }

    #endregion

    #region Different Font Type Tests - Exercises various table handling code

    [Fact]
    public void ComputeSubset_SegoeUI_ShouldWork()
    {
        Assert.SkipUnless(File.Exists(s_segoeUIPath), "Segoe UI not found");
        var fontData = File.ReadAllBytes(s_segoeUIPath);
        var uri = new Uri(s_segoeUIPath);

        ushort[] glyphArray = Enumerable.Range(0, 300).Select(i => (ushort)i).ToArray();

        unsafe
        {
            fixed (byte* pFontData = fontData)
            {
                var result = TrueTypeSubsetter.ComputeSubset(pFontData, fontData.Length, uri, 0, glyphArray);
                result.Should().NotBeNull();
            }
        }
    }

    [Fact]
    public void ComputeSubset_Gabriola_OpenTypeFeatures_ShouldWork()
    {
        // Gabriola has many OpenType features (stylistic sets, swashes)
        Assert.SkipUnless(File.Exists(s_gabriolaPath), "Gabriola not found");
        var fontData = File.ReadAllBytes(s_gabriolaPath);
        var uri = new Uri(s_gabriolaPath);

        ushort[] glyphArray = Enumerable.Range(0, 500).Select(i => (ushort)i).ToArray();

        unsafe
        {
            fixed (byte* pFontData = fontData)
            {
                var result = TrueTypeSubsetter.ComputeSubset(pFontData, fontData.Length, uri, 0, glyphArray);
                result.Should().NotBeNull();
            }
        }
    }

    [Fact]
    public void ComputeSubset_TimesNewRoman_ShouldWork()
    {
        Assert.SkipUnless(File.Exists(s_timesPath), "Times New Roman not found");
        var fontData = File.ReadAllBytes(s_timesPath);
        var uri = new Uri(s_timesPath);

        ushort[] glyphArray = Enumerable.Range(0, 200).Select(i => (ushort)i).ToArray();

        unsafe
        {
            fixed (byte* pFontData = fontData)
            {
                var result = TrueTypeSubsetter.ComputeSubset(pFontData, fontData.Length, uri, 0, glyphArray);
                result.Should().NotBeNull();
            }
        }
    }

    [Fact]
    public void ComputeSubset_CourierNew_ShouldWork()
    {
        Assert.SkipUnless(File.Exists(s_courierPath), "Courier New not found");
        var fontData = File.ReadAllBytes(s_courierPath);
        var uri = new Uri(s_courierPath);

        ushort[] glyphArray = Enumerable.Range(0, 200).Select(i => (ushort)i).ToArray();

        unsafe
        {
            fixed (byte* pFontData = fontData)
            {
                var result = TrueTypeSubsetter.ComputeSubset(pFontData, fontData.Length, uri, 0, glyphArray);
                result.Should().NotBeNull();
            }
        }
    }

    [Fact]
    public void ComputeSubset_Tahoma_ShouldWork()
    {
        Assert.SkipUnless(File.Exists(s_tahomaPath), "Tahoma not found");
        var fontData = File.ReadAllBytes(s_tahomaPath);
        var uri = new Uri(s_tahomaPath);

        ushort[] glyphArray = Enumerable.Range(0, 300).Select(i => (ushort)i).ToArray();

        unsafe
        {
            fixed (byte* pFontData = fontData)
            {
                var result = TrueTypeSubsetter.ComputeSubset(pFontData, fontData.Length, uri, 0, glyphArray);
                result.Should().NotBeNull();
            }
        }
    }

    [Fact]
    public void ComputeSubset_Verdana_ShouldWork()
    {
        Assert.SkipUnless(File.Exists(s_verdanaPath), "Verdana not found");
        var fontData = File.ReadAllBytes(s_verdanaPath);
        var uri = new Uri(s_verdanaPath);

        ushort[] glyphArray = Enumerable.Range(0, 300).Select(i => (ushort)i).ToArray();

        unsafe
        {
            fixed (byte* pFontData = fontData)
            {
                var result = TrueTypeSubsetter.ComputeSubset(pFontData, fontData.Length, uri, 0, glyphArray);
                result.Should().NotBeNull();
            }
        }
    }

    [Fact]
    public void ComputeSubset_Georgia_ShouldWork()
    {
        Assert.SkipUnless(File.Exists(s_georgiaPath), "Georgia not found");
        var fontData = File.ReadAllBytes(s_georgiaPath);
        var uri = new Uri(s_georgiaPath);

        ushort[] glyphArray = Enumerable.Range(0, 300).Select(i => (ushort)i).ToArray();

        unsafe
        {
            fixed (byte* pFontData = fontData)
            {
                var result = TrueTypeSubsetter.ComputeSubset(pFontData, fontData.Length, uri, 0, glyphArray);
                result.Should().NotBeNull();
            }
        }
    }

    [Fact]
    public void ComputeSubset_Consolas_ShouldWork()
    {
        Assert.SkipUnless(File.Exists(s_consolasPath), "Consolas not found");
        var fontData = File.ReadAllBytes(s_consolasPath);
        var uri = new Uri(s_consolasPath);

        ushort[] glyphArray = Enumerable.Range(0, 300).Select(i => (ushort)i).ToArray();

        unsafe
        {
            fixed (byte* pFontData = fontData)
            {
                var result = TrueTypeSubsetter.ComputeSubset(pFontData, fontData.Length, uri, 0, glyphArray);
                result.Should().NotBeNull();
            }
        }
    }

    [Fact]
    public void ComputeSubset_Calibri_ShouldWork()
    {
        Assert.SkipUnless(File.Exists(s_calibriPath), "Calibri not found");
        var fontData = File.ReadAllBytes(s_calibriPath);
        var uri = new Uri(s_calibriPath);

        ushort[] glyphArray = Enumerable.Range(0, 500).Select(i => (ushort)i).ToArray();

        unsafe
        {
            fixed (byte* pFontData = fontData)
            {
                var result = TrueTypeSubsetter.ComputeSubset(pFontData, fontData.Length, uri, 0, glyphArray);
                result.Should().NotBeNull();
            }
        }
    }

    [Fact]
    public void ComputeSubset_ComicSans_ShouldWork()
    {
        Assert.SkipUnless(File.Exists(s_comicSansPath), "Comic Sans not found");
        var fontData = File.ReadAllBytes(s_comicSansPath);
        var uri = new Uri(s_comicSansPath);

        ushort[] glyphArray = Enumerable.Range(0, 200).Select(i => (ushort)i).ToArray();

        unsafe
        {
            fixed (byte* pFontData = fontData)
            {
                var result = TrueTypeSubsetter.ComputeSubset(pFontData, fontData.Length, uri, 0, glyphArray);
                result.Should().NotBeNull();
            }
        }
    }

    [Fact]
    public void ComputeSubset_Impact_ShouldWork()
    {
        Assert.SkipUnless(File.Exists(s_impactPath), "Impact not found");
        var fontData = File.ReadAllBytes(s_impactPath);
        var uri = new Uri(s_impactPath);

        ushort[] glyphArray = Enumerable.Range(0, 200).Select(i => (ushort)i).ToArray();

        unsafe
        {
            fixed (byte* pFontData = fontData)
            {
                var result = TrueTypeSubsetter.ComputeSubset(pFontData, fontData.Length, uri, 0, glyphArray);
                result.Should().NotBeNull();
            }
        }
    }

    #endregion

    #region Symbol Font Tests - Exercises symbol CMAP encoding

    [Fact]
    public void ComputeSubset_Symbol_ShouldWork()
    {
        // Symbol font uses a different CMAP encoding
        Assert.SkipUnless(File.Exists(s_symbolPath), "Symbol not found");
        var fontData = File.ReadAllBytes(s_symbolPath);
        var uri = new Uri(s_symbolPath);

        ushort[] glyphArray = Enumerable.Range(0, 100).Select(i => (ushort)i).ToArray();

        unsafe
        {
            fixed (byte* pFontData = fontData)
            {
                var result = TrueTypeSubsetter.ComputeSubset(pFontData, fontData.Length, uri, 0, glyphArray);
                result.Should().NotBeNull();
            }
        }
    }

    [Fact]
    public void ComputeSubset_Wingdings_ShouldWork()
    {
        Assert.SkipUnless(File.Exists(s_wingdingsPath), "Wingdings not found");
        var fontData = File.ReadAllBytes(s_wingdingsPath);
        var uri = new Uri(s_wingdingsPath);

        ushort[] glyphArray = Enumerable.Range(0, 150).Select(i => (ushort)i).ToArray();

        unsafe
        {
            fixed (byte* pFontData = fontData)
            {
                var result = TrueTypeSubsetter.ComputeSubset(pFontData, fontData.Length, uri, 0, glyphArray);
                result.Should().NotBeNull();
            }
        }
    }

    [Fact]
    public void ComputeSubset_Webdings_ShouldWork()
    {
        Assert.SkipUnless(File.Exists(s_webdingsPath), "Webdings not found");
        var fontData = File.ReadAllBytes(s_webdingsPath);
        var uri = new Uri(s_webdingsPath);

        ushort[] glyphArray = Enumerable.Range(0, 150).Select(i => (ushort)i).ToArray();

        unsafe
        {
            fixed (byte* pFontData = fontData)
            {
                var result = TrueTypeSubsetter.ComputeSubset(pFontData, fontData.Length, uri, 0, glyphArray);
                result.Should().NotBeNull();
            }
        }
    }

    [Fact]
    public void ComputeSubset_SegoeSymbol_ShouldWork()
    {
        // Segoe UI Symbol has extended Unicode ranges
        Assert.SkipUnless(File.Exists(s_segoeSymbolPath), "Segoe UI Symbol not found");
        var fontData = File.ReadAllBytes(s_segoeSymbolPath);
        var uri = new Uri(s_segoeSymbolPath);

        ushort[] glyphArray = Enumerable.Range(0, 500).Select(i => (ushort)i).ToArray();

        unsafe
        {
            fixed (byte* pFontData = fontData)
            {
                var result = TrueTypeSubsetter.ComputeSubset(pFontData, fontData.Length, uri, 0, glyphArray);
                result.Should().NotBeNull();
            }
        }
    }

    #endregion

    #region Emoji Font Tests - May exercise Format 12 CMAP

    [Fact]
    public void ComputeSubset_SegoeUIEmoji_ShouldWork()
    {
        // Emoji font may have Format 12 CMAP for supplementary plane
        Assert.SkipUnless(File.Exists(s_segoeUIEmojiPath), "Segoe UI Emoji not found");
        var fontData = File.ReadAllBytes(s_segoeUIEmojiPath);
        var uri = new Uri(s_segoeUIEmojiPath);

        ushort[] glyphArray = Enumerable.Range(0, 500).Select(i => (ushort)i).ToArray();

        unsafe
        {
            fixed (byte* pFontData = fontData)
            {
                var result = TrueTypeSubsetter.ComputeSubset(pFontData, fontData.Length, uri, 0, glyphArray);
                result.Should().NotBeNull();
            }
        }
    }

    [Fact]
    public void ComputeSubset_SegoeUIEmoji_WithManyGlyphs_ShouldWork()
    {
        Assert.SkipUnless(File.Exists(s_segoeUIEmojiPath), "Segoe UI Emoji not found");
        var fontData = File.ReadAllBytes(s_segoeUIEmojiPath);
        var uri = new Uri(s_segoeUIEmojiPath);

        // Emoji font has thousands of glyphs
        ushort[] glyphArray = Enumerable.Range(0, 2000).Select(i => (ushort)i).ToArray();

        unsafe
        {
            fixed (byte* pFontData = fontData)
            {
                var result = TrueTypeSubsetter.ComputeSubset(pFontData, fontData.Length, uri, 0, glyphArray);
                result.Should().NotBeNull();
            }
        }
    }

    #endregion

    #region Edge Case Glyph Patterns

    [Fact]
    public void ComputeSubset_SingleGlyph_ShouldWork()
    {
        Assert.SkipUnless(File.Exists(TestHelpers.ArialPath), "Arial not found");
        var fontData = File.ReadAllBytes(TestHelpers.ArialPath);
        var uri = new Uri(TestHelpers.ArialPath);

        // Single glyph - minimum subset
        ushort[] glyphArray = [0];

        unsafe
        {
            fixed (byte* pFontData = fontData)
            {
                var result = TrueTypeSubsetter.ComputeSubset(pFontData, fontData.Length, uri, 0, glyphArray);
                result.Should().NotBeNull();
            }
        }
    }

    [Fact]
    public void ComputeSubset_ConsecutiveGlyphs_ShouldWork()
    {
        Assert.SkipUnless(File.Exists(TestHelpers.ArialPath), "Arial not found");
        var fontData = File.ReadAllBytes(TestHelpers.ArialPath);
        var uri = new Uri(TestHelpers.ArialPath);

        // Consecutive glyphs - exercises sequential optimization
        ushort[] glyphArray = Enumerable.Range(100, 200).Select(i => (ushort)i).ToArray();

        unsafe
        {
            fixed (byte* pFontData = fontData)
            {
                var result = TrueTypeSubsetter.ComputeSubset(pFontData, fontData.Length, uri, 0, glyphArray);
                result.Should().NotBeNull();
            }
        }
    }

    [Fact]
    public void ComputeSubset_RandomGlyphs_ShouldWork()
    {
        Assert.SkipUnless(File.Exists(TestHelpers.ArialPath), "Arial not found");
        var fontData = File.ReadAllBytes(TestHelpers.ArialPath);
        var uri = new Uri(TestHelpers.ArialPath);

        // Random-ish pattern
        ushort[] glyphArray = [0, 5, 17, 42, 88, 123, 256, 512, 777, 1024, 1111, 1500];

        unsafe
        {
            fixed (byte* pFontData = fontData)
            {
                var result = TrueTypeSubsetter.ComputeSubset(pFontData, fontData.Length, uri, 0, glyphArray);
                result.Should().NotBeNull();
            }
        }
    }

    [Fact]
    public void ComputeSubset_BoundaryGlyphs_ShouldWork()
    {
        Assert.SkipUnless(File.Exists(TestHelpers.ArialPath), "Arial not found");
        var fontData = File.ReadAllBytes(TestHelpers.ArialPath);
        var uri = new Uri(TestHelpers.ArialPath);

        // Boundary values
        ushort[] glyphArray = [0, 1, 255, 256, 65534];

        unsafe
        {
            fixed (byte* pFontData = fontData)
            {
                var result = TrueTypeSubsetter.ComputeSubset(pFontData, fontData.Length, uri, 0, glyphArray);
                result.Should().NotBeNull();
            }
        }
    }

    [Fact]
    public void ComputeSubset_AllLowGlyphs_ShouldWork()
    {
        Assert.SkipUnless(File.Exists(TestHelpers.ArialPath), "Arial not found");
        var fontData = File.ReadAllBytes(TestHelpers.ArialPath);
        var uri = new Uri(TestHelpers.ArialPath);

        // All glyphs in low range (0-255) - exercises different table formats
        ushort[] glyphArray = Enumerable.Range(0, 256).Select(i => (ushort)i).ToArray();

        unsafe
        {
            fixed (byte* pFontData = fontData)
            {
                var result = TrueTypeSubsetter.ComputeSubset(pFontData, fontData.Length, uri, 0, glyphArray);
                result.Should().NotBeNull();
            }
        }
    }

    #endregion

    #region Glyph Range Pattern Tests

    [Fact]
    public void ComputeSubset_LatinExtended_ShouldWork()
    {
        Assert.SkipUnless(File.Exists(TestHelpers.ArialPath), "Arial not found");
        var fontData = File.ReadAllBytes(TestHelpers.ArialPath);
        var uri = new Uri(TestHelpers.ArialPath);

        // Latin Extended range (glyphs for accented characters)
        ushort[] glyphArray = Enumerable.Range(0, 400).Select(i => (ushort)i).ToArray();

        unsafe
        {
            fixed (byte* pFontData = fontData)
            {
                var result = TrueTypeSubsetter.ComputeSubset(pFontData, fontData.Length, uri, 0, glyphArray);
                result.Should().NotBeNull();
            }
        }
    }

    [Fact]
    public void ComputeSubset_GreekAndCyrillic_ShouldWork()
    {
        Assert.SkipUnless(File.Exists(TestHelpers.ArialPath), "Arial not found");
        var fontData = File.ReadAllBytes(TestHelpers.ArialPath);
        var uri = new Uri(TestHelpers.ArialPath);

        // Greek and Cyrillic ranges
        ushort[] glyphArray = Enumerable.Range(300, 400).Select(i => (ushort)i).ToArray();

        unsafe
        {
            fixed (byte* pFontData = fontData)
            {
                var result = TrueTypeSubsetter.ComputeSubset(pFontData, fontData.Length, uri, 0, glyphArray);
                result.Should().NotBeNull();
            }
        }
    }

    #endregion

    #region Output Validation Tests

    [Fact]
    public void ComputeSubset_OutputHasValidStructure()
    {
        Assert.SkipUnless(File.Exists(TestHelpers.ArialPath), "Arial not found");
        var fontData = File.ReadAllBytes(TestHelpers.ArialPath);
        var uri = new Uri(TestHelpers.ArialPath);

        ushort[] glyphArray = Enumerable.Range(0, 100).Select(i => (ushort)i).ToArray();

        unsafe
        {
            fixed (byte* pFontData = fontData)
            {
                var result = TrueTypeSubsetter.ComputeSubset(pFontData, fontData.Length, uri, 0, glyphArray);
                
                result.Should().NotBeNull();
                result.Length.Should().BeGreaterThan(12); // Minimum TrueType header size
                
                // Verify offset table structure
                // numTables is at offset 4-5
                ushort numTables = (ushort)((result[4] << 8) | result[5]);
                numTables.Should().BeGreaterThan(0).And.BeLessThan(100);
            }
        }
    }

    [Fact]
    public void ComputeSubset_DifferentFonts_ProduceDifferentOutput()
    {
        Assert.SkipUnless(File.Exists(TestHelpers.ArialPath), "Arial not found");
        Assert.SkipUnless(File.Exists(s_timesPath), "Times not found");
        
        var arialData = File.ReadAllBytes(TestHelpers.ArialPath);
        var timesData = File.ReadAllBytes(s_timesPath);
        var arialUri = new Uri(TestHelpers.ArialPath);
        var timesUri = new Uri(s_timesPath);

        ushort[] glyphArray = Enumerable.Range(0, 50).Select(i => (ushort)i).ToArray();

        unsafe
        {
            fixed (byte* pArial = arialData)
            fixed (byte* pTimes = timesData)
            {
                var arialResult = TrueTypeSubsetter.ComputeSubset(pArial, arialData.Length, arialUri, 0, glyphArray);
                var timesResult = TrueTypeSubsetter.ComputeSubset(pTimes, timesData.Length, timesUri, 0, glyphArray);
                
                arialResult.Should().NotBeNull();
                timesResult.Should().NotBeNull();
                
                // Results should be different
                arialResult.SequenceEqual(timesResult).Should().BeFalse();
            }
        }
    }

    #endregion
}
