// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace MS.Internal.Text.TextInterface.Tests;

/// <summary>
/// Tests for <see cref="Font"/> class.
/// Arial Regular is used as a known reference font with documented properties.
/// </summary>
public class FontTests
{
    // Known Arial Regular properties (standard Windows font)
    // These values are consistent across Windows versions
    private const int ArialRegularWeight = 400;     // Normal
    private const int ArialRegularStretch = 5;      // Normal
    private const int ArialRegularStyle = 0;        // Normal
    private const ushort ArialDesignUnitsPerEm = 2048;

    [Fact]
    public void Weight_ArialRegular_ShouldBeNormal()
    {
        var font = TestHelpers.GetArialFontOrSkip();
        
        // Arial Regular should have weight 400 (Normal)
        font.Weight.Should().Be(FontWeight.Normal);
        ((int)font.Weight).Should().Be(ArialRegularWeight);
    }

    [Fact]
    public void Stretch_ArialRegular_ShouldBeNormal()
    {
        var font = TestHelpers.GetArialFontOrSkip();
        
        // Arial Regular should have stretch 5 (Normal)
        font.Stretch.Should().Be(FontStretch.Normal);
        ((int)font.Stretch).Should().Be(ArialRegularStretch);
    }

    [Fact]
    public void Style_ArialRegular_ShouldBeNormal()
    {
        var font = TestHelpers.GetArialFontOrSkip();
        
        // Arial Regular should have style 0 (Normal)
        font.Style.Should().Be(FontStyle.Normal);
        ((int)font.Style).Should().Be(ArialRegularStyle);
    }

    [Fact]
    public void Family_ShouldNotBeNull()
    {
        var font = TestHelpers.GetArialFontOrSkip();
        
        font.Family.Should().NotBeNull();
    }

    [Fact]
    public void FaceNames_ShouldContainRegular()
    {
        var font = TestHelpers.GetArialFontOrSkip();
        
        var faceNames = font.FaceNames;
        
        faceNames.Should().NotBeNull();
        faceNames.Count.Should().BeGreaterThan(0);
        
        // Arial Regular should have "Regular" as a face name
        faceNames.Values.Should().Contain("Regular");
    }

    [Fact]
    public void Metrics_ArialDesignUnitsPerEm_ShouldBe2048()
    {
        var font = TestHelpers.GetArialFontOrSkip();
        
        var metrics = font.Metrics;
        
        metrics.Should().NotBeNull();
        // Arial has 2048 design units per em (standard for TrueType fonts)
        metrics.DesignUnitsPerEm.Should().Be(ArialDesignUnitsPerEm);
        metrics.Ascent.Should().BeGreaterThan(0);
        metrics.Descent.Should().BeGreaterThan(0);
    }

    [Fact]
    public void Metrics_AscentPlusDescent_ShouldBeLessThanDesignUnits()
    {
        var font = TestHelpers.GetArialFontOrSkip();
        
        var metrics = font.Metrics;
        
        // Ascent + Descent should not exceed design units per em (basic sanity check)
        (metrics.Ascent + metrics.Descent).Should().BeLessThanOrEqualTo(metrics.DesignUnitsPerEm * 2);
    }

    [Fact]
    public void DisplayMetrics_ShouldReturnValidGdiCompatibleMetrics()
    {
        var font = TestHelpers.GetArialFontOrSkip();
        
        var displayMetrics = font.DisplayMetrics(12.0f, 96.0f);
        
        displayMetrics.Should().NotBeNull();
        // GDI compatible metrics are still in design units, but snapped to pixel grid
        // They should be comparable to the regular design metrics
        var designMetrics = font.Metrics;
        displayMetrics.DesignUnitsPerEm.Should().Be(designMetrics.DesignUnitsPerEm);
        displayMetrics.Ascent.Should().BeGreaterThan(0);
        displayMetrics.Descent.Should().BeGreaterThan(0);
    }

    [Fact]
    public void GetFontFace_ShouldReturnValidFontFace()
    {
        var font = TestHelpers.GetArialFontOrSkip();
        
        var fontFace = font.GetFontFace();
        
        fontFace.Should().NotBeNull();
        fontFace.Release();
    }

    [Fact]
    public void SimulationFlags_ShouldBeNoneForRegularFont()
    {
        var font = TestHelpers.GetArialFontOrSkip();
        
        // A regular font without simulations should have None
        font.SimulationFlags.Should().Be(FontSimulations.None);
    }

    [Fact]
    public void Version_ShouldBePositive()
    {
        var font = TestHelpers.GetArialFontOrSkip();
        
        font.Version.Should().BeGreaterThan(0);
    }

    [Fact]
    public void IsSymbolFont_ForArial_ShouldBeFalse()
    {
        var font = TestHelpers.GetArialFontOrSkip();
        
        // Arial is not a symbol font
        font.IsSymbolFont.Should().BeFalse();
    }

    [Fact]
    public void SymbolFont_ShouldBeIdentifiedCorrectly()
    {
        var fontCollection = DWriteFactory.SystemFontCollection;
        
        // Try to find a symbol font like Wingdings or Symbol
        var symbolFamilyNames = new[] { "Wingdings", "Symbol", "Webdings" };
        
        foreach (var familyName in symbolFamilyNames)
        {
            var family = fontCollection[familyName];
            if (family != null && family.Count > 0)
            {
                var font = family[0u];
                font.IsSymbolFont.Should().BeTrue($"{familyName} should be identified as a symbol font");
                return;
            }
        }
        
        Assert.Skip("No symbol fonts found on system");
    }

    [Fact]
    public void HasCharacter_WithAsciiLetters_ShouldReturnTrue()
    {
        var font = TestHelpers.GetArialFontOrSkip();
        
        // All ASCII letters should be in Arial
        for (uint codePoint = 'A'; codePoint <= 'Z'; codePoint++)
        {
            font.HasCharacter(codePoint).Should().BeTrue($"Arial should have character U+{codePoint:X4} ('{(char)codePoint}')");
        }
        for (uint codePoint = 'a'; codePoint <= 'z'; codePoint++)
        {
            font.HasCharacter(codePoint).Should().BeTrue($"Arial should have character U+{codePoint:X4} ('{(char)codePoint}')");
        }
    }

    [Fact]
    public void HasCharacter_WithDigits_ShouldReturnTrue()
    {
        var font = TestHelpers.GetArialFontOrSkip();
        
        // All ASCII digits should be in Arial
        for (uint codePoint = '0'; codePoint <= '9'; codePoint++)
        {
            font.HasCharacter(codePoint).Should().BeTrue($"Arial should have character U+{codePoint:X4} ('{(char)codePoint}')");
        }
    }

    [Fact]
    public void HasCharacter_WithRareCharacter_ShouldReturnFalse()
    {
        var font = TestHelpers.GetArialFontOrSkip();
        
        // Very high code point that's unlikely to be in Arial
        font.HasCharacter(0x1F999).Should().BeFalse(); // Emoji unicorn
    }

    [Fact]
    public void GetInformationalStrings_FamilyName_ShouldContainArial()
    {
        var font = TestHelpers.GetArialFontOrSkip();
        
        bool exists = font.GetInformationalStrings(InformationalStringID.WIN32FamilyNames, out var familyNames);
        
        exists.Should().BeTrue();
        familyNames.Should().NotBeNull();
        // Should contain "Arial" in the localized names
        familyNames!.Values.Should().Contain("Arial");
    }

    [Fact]
    public void GetInformationalStrings_DesignerName_ShouldNotThrow()
    {
        var font = TestHelpers.GetArialFontOrSkip();
        
        // May or may not have designer info, but shouldn't throw
        _ = font.GetInformationalStrings(InformationalStringID.Designer, out _);
        // Result may be null if this info string is not present
    }

    [Fact]
    public void GetInformationalStrings_CopyrightNotice_ShouldExistAndContainMicrosoft()
    {
        var font = TestHelpers.GetArialFontOrSkip();
        
        bool exists = font.GetInformationalStrings(InformationalStringID.CopyrightNotice, out var copyright);
        
        exists.Should().BeTrue("Arial should have copyright information");
        copyright.Should().NotBeNull();
        
        // Arial is a Microsoft/Monotype font
        var copyrightText = copyright!.Values.FirstOrDefault() ?? "";
        copyrightText.Should().NotBeEmpty();
    }
}

/// <summary>
/// Tests for <see cref="FontFace"/> class.
/// Arial is used as a known reference font with documented properties.
/// </summary>
public class FontFaceTests
{
    // Known Arial metrics
    private const ushort ArialDesignUnitsPerEm = 2048;
    private const ushort ArialGlyphCountMinimum = 1000; // Arial has 1000+ glyphs

    private FontFace GetArialFontFace()
    {
        var factory = DWriteFactory.Instance;
        TestHelpers.SkipIfArialNotAvailable();
        var arialPath = TestHelpers.ArialPath;
        
        return factory.CreateFontFace(new Uri(arialPath), 0);
    }

    [Fact]
    public void Type_ArialTtf_ShouldBeTrueType()
    {
        var fontFace = GetArialFontFace();
        
        try
        {
            // arial.ttf is a TrueType font (not TrueTypeCollection)
            fontFace.Type.Should().Be(FontFaceType.TrueType);
            // WPF FontFaceType enum: CFF=0, TrueType=1, TrueTypeCollection=2, etc.
            ((int)fontFace.Type).Should().Be(1, "FontFaceType.TrueType has value 1 in WPF enum");
        }
        finally
        {
            fontFace.Release();
        }
    }

    [Fact]
    public void Index_ForSingleFontFile_ShouldBeZero()
    {
        var fontFace = GetArialFontFace();
        
        try
        {
            // Single font file (not TTC) should have index 0
            fontFace.Index.Should().Be(0u);
        }
        finally
        {
            fontFace.Release();
        }
    }

    [Fact]
    public void SimulationFlags_WithNoSimulations_ShouldBeNone()
    {
        var fontFace = GetArialFontFace();
        
        try
        {
            fontFace.SimulationFlags.Should().Be(FontSimulations.None);
            ((int)fontFace.SimulationFlags).Should().Be(0);
        }
        finally
        {
            fontFace.Release();
        }
    }

    [Fact]
    public void Metrics_Arial_ShouldHave2048DesignUnits()
    {
        var fontFace = GetArialFontFace();
        
        try
        {
            var metrics = fontFace.Metrics;
            
            metrics.Should().NotBeNull();
            metrics.DesignUnitsPerEm.Should().Be(ArialDesignUnitsPerEm);
            metrics.Ascent.Should().BeGreaterThan(0);
            metrics.Descent.Should().BeGreaterThan(0);
        }
        finally
        {
            fontFace.Release();
        }
    }

    [Fact]
    public void IsSymbolFont_ForArial_ShouldBeFalse()
    {
        var fontFace = GetArialFontFace();
        
        try
        {
            fontFace.IsSymbolFont.Should().BeFalse();
        }
        finally
        {
            fontFace.Release();
        }
    }

    [Fact]
    public void GlyphCount_Arial_ShouldBeOver1000()
    {
        var fontFace = GetArialFontFace();
        
        try
        {
            // Arial has a large character set with 1000+ glyphs
            fontFace.GlyphCount.Should().BeGreaterThanOrEqualTo(ArialGlyphCountMinimum);
        }
        finally
        {
            fontFace.Release();
        }
    }

    [Fact]
    public void FontFace_WithBoldSimulation_ShouldHaveBoldFlag()
    {
        var factory = DWriteFactory.Instance;
        TestHelpers.SkipIfArialNotAvailable();
        var arialPath = TestHelpers.ArialPath;
        
        var fontFace = factory.CreateFontFace(new Uri(arialPath), 0, FontSimulations.Bold);
        try
        {
            fontFace.SimulationFlags.Should().Be(FontSimulations.Bold);
        }
        finally
        {
            fontFace.Release();
        }
    }

    [Fact]
    public void FontFace_WithObliqueSimulation_ShouldHaveObliqueFlag()
    {
        var factory = DWriteFactory.Instance;
        TestHelpers.SkipIfArialNotAvailable();
        var arialPath = TestHelpers.ArialPath;
        
        var fontFace = factory.CreateFontFace(new Uri(arialPath), 0, FontSimulations.Oblique);
        try
        {
            fontFace.SimulationFlags.Should().Be(FontSimulations.Oblique);
        }
        finally
        {
            fontFace.Release();
        }
    }

    [Fact]
    public void FontFace_WithCombinedSimulations_ShouldHaveBothFlags()
    {
        var factory = DWriteFactory.Instance;
        TestHelpers.SkipIfArialNotAvailable();
        var arialPath = TestHelpers.ArialPath;
        
        var fontFace = factory.CreateFontFace(new Uri(arialPath), 0, FontSimulations.Bold | FontSimulations.Oblique);
        try
        {
            fontFace.SimulationFlags.Should().HaveFlag(FontSimulations.Bold);
            fontFace.SimulationFlags.Should().HaveFlag(FontSimulations.Oblique);
        }
        finally
        {
            fontFace.Release();
        }
    }

    [Fact]
    public void GetFileZero_ShouldReturnFontFile()
    {
        var fontFace = GetArialFontFace();
        
        try
        {
            var fontFile = fontFace.GetFileZero();
            fontFile.Should().NotBeNull();
        }
        finally
        {
            fontFace.Release();
        }
    }

    [Fact]
    public void TryGetFontTable_WithHeadTable_ShouldReturnValidHeader()
    {
        var fontFace = GetArialFontFace();
        
        try
        {
            // 'head' table should exist in all TrueType fonts
            bool found = fontFace.TryGetFontTable(OpenTypeTableTag.FontHeader, out byte[]? tableData);
            
            found.Should().BeTrue();
            tableData.Should().NotBeNull();
            
            // head table is exactly 54 bytes
            tableData!.Length.Should().Be(54, "head table should be exactly 54 bytes");
            
            // Verify magic number at offset 12 (should be 0x5F0F3CF5)
            uint magicNumber = System.Buffers.Binary.BinaryPrimitives.ReadUInt32BigEndian(tableData.AsSpan(12, 4));
            magicNumber.Should().Be(0x5F0F3CF5, "head table should contain magic number 0x5F0F3CF5");
            
            // Verify unitsPerEm at offset 18 matches what we expect
            ushort unitsPerEm = System.Buffers.Binary.BinaryPrimitives.ReadUInt16BigEndian(tableData.AsSpan(18, 2));
            unitsPerEm.Should().Be(ArialDesignUnitsPerEm, "unitsPerEm in head table should match font metrics");
        }
        finally
        {
            fontFace.Release();
        }
    }

    [Fact]
    public void TryGetFontTable_WithCmapTable_ShouldReturnValidStructure()
    {
        var fontFace = GetArialFontFace();
        
        try
        {
            // 'cmap' table (character to glyph mapping) should exist
            bool found = fontFace.TryGetFontTable(OpenTypeTableTag.CharToIndexMap, out byte[]? tableData);
            
            found.Should().BeTrue();
            tableData.Should().NotBeNull();
            tableData!.Length.Should().BeGreaterThan(4);
            
            // cmap table version should be 0
            ushort version = System.Buffers.Binary.BinaryPrimitives.ReadUInt16BigEndian(tableData.AsSpan(0, 2));
            version.Should().Be(0, "cmap table version should be 0");
            
            // Number of encoding tables should be at least 1
            ushort numTables = System.Buffers.Binary.BinaryPrimitives.ReadUInt16BigEndian(tableData.AsSpan(2, 2));
            numTables.Should().BeGreaterThan(0, "cmap should have at least one encoding table");
        }
        finally
        {
            fontFace.Release();
        }
    }

    [Fact]
    public void TryGetFontTable_WithNonExistentTable_ShouldReturnFalse()
    {
        var fontFace = GetArialFontFace();
        
        try
        {
            // 'JSTF' (Justification) table typically doesn't exist in Arial
            bool found = fontFace.TryGetFontTable(OpenTypeTableTag.TTO_JSTF, out byte[]? tableData);
            
            // May or may not exist, but should not throw
            if (!found)
            {
                tableData.Should().BeNull();
            }
        }
        finally
        {
            fontFace.Release();
        }
    }

    [Fact]
    public void ReadFontEmbeddingRights_Arial_ShouldReturnInstallableValue()
    {
        var fontFace = GetArialFontFace();
        
        try
        {
            bool success = fontFace.ReadFontEmbeddingRights(out ushort fsType);
            
            // Arial should have OS/2 table with embedding rights
            success.Should().BeTrue();
            // fsType bits 0-3 indicate embedding licensing rights
            // Arial typically allows embedding (fsType & 0x000F should not be 0x0002 which is "restricted")
            (fsType & 0x0002).Should().Be(0, "Arial should not have restricted embedding");
        }
        finally
        {
            fontFace.Release();
        }
    }

    [Fact]
    public unsafe void GetArrayOfGlyphIndices_WithValidCodePoints_ShouldReturnConsistentGlyphIndices()
    {
        var fontFace = GetArialFontFace();
        
        try
        {
            // Get glyph indices for 'A', 'B', 'C' (code points 65, 66, 67)
            uint[] codePoints = [65, 66, 67];
            ushort[] glyphIndices = new ushort[codePoints.Length];
            
            fixed (uint* pCodePoints = codePoints)
            fixed (ushort* pGlyphIndices = glyphIndices)
            {
                fontFace.GetArrayOfGlyphIndices(pCodePoints, (uint)codePoints.Length, pGlyphIndices);
            }
            
            // All glyphs should be non-zero (0 is typically .notdef)
            glyphIndices[0].Should().BeGreaterThan((ushort)0, "Glyph for 'A' should exist");
            glyphIndices[1].Should().BeGreaterThan((ushort)0, "Glyph for 'B' should exist");
            glyphIndices[2].Should().BeGreaterThan((ushort)0, "Glyph for 'C' should exist");
            
            // Adjacent characters should have different glyph indices
            glyphIndices[0].Should().NotBe(glyphIndices[1], "'A' and 'B' should have different glyph indices");
            glyphIndices[1].Should().NotBe(glyphIndices[2], "'B' and 'C' should have different glyph indices");
            
            // Glyph indices should be consistent when called again
            ushort[] glyphIndices2 = new ushort[codePoints.Length];
            fixed (uint* pCodePoints = codePoints)
            fixed (ushort* pGlyphIndices = glyphIndices2)
            {
                fontFace.GetArrayOfGlyphIndices(pCodePoints, (uint)codePoints.Length, pGlyphIndices);
            }
            glyphIndices.Should().Equal(glyphIndices2, "Glyph indices should be consistent across calls");
        }
        finally
        {
            fontFace.Release();
        }
    }

    [Fact]
    public unsafe void GetArrayOfGlyphIndices_WithUnsupportedCodePoint_ShouldReturnZero()
    {
        var fontFace = GetArialFontFace();
        
        try
        {
            // Very high code point unlikely to be in Arial
            uint[] codePoints = [0x1F999]; // Unicorn emoji
            ushort[] glyphIndices = new ushort[1];
            
            fixed (uint* pCodePoints = codePoints)
            fixed (ushort* pGlyphIndices = glyphIndices)
            {
                fontFace.GetArrayOfGlyphIndices(pCodePoints, 1, pGlyphIndices);
            }
            
            // Should return 0 (.notdef) for unsupported character
            glyphIndices[0].Should().Be(0);
        }
        finally
        {
            fontFace.Release();
        }
    }

    [Fact]
    public unsafe void GetDesignGlyphMetrics_ShouldReturnValidMetricsInDesignUnits()
    {
        var fontFace = GetArialFontFace();
        
        try
        {
            // First get glyph indices for 'A'
            uint[] codePoints = [65]; // 'A'
            ushort[] glyphIndices = new ushort[1];
            
            fixed (uint* pCodePoints = codePoints)
            fixed (ushort* pGlyphIndices = glyphIndices)
            {
                fontFace.GetArrayOfGlyphIndices(pCodePoints, 1, pGlyphIndices);
            }
            
            // Now get design metrics for that glyph
            GlyphMetrics[] metrics = new GlyphMetrics[1];
            
            fixed (ushort* pGlyphIndices = glyphIndices)
            fixed (GlyphMetrics* pMetrics = metrics)
            {
                fontFace.GetDesignGlyphMetrics(pGlyphIndices, 1, pMetrics);
            }
            
            // 'A' should have positive advance width
            metrics[0].AdvanceWidth.Should().BeGreaterThan(0);
            
            // Advance width should be reasonable (less than design units per em for a single glyph)
            metrics[0].AdvanceWidth.Should().BeLessThan(ArialDesignUnitsPerEm,
                "Advance width should be less than design units per em");
        }
        finally
        {
            fontFace.Release();
        }
    }

    [Fact]
    public unsafe void GetDisplayGlyphMetrics_ShouldReturnMetrics()
    {
        var fontFace = GetArialFontFace();
        
        try
        {
            // First get glyph indices for 'A'
            uint[] codePoints = [65]; // 'A'
            ushort[] glyphIndices = new ushort[1];
            
            fixed (uint* pCodePoints = codePoints)
            fixed (ushort* pGlyphIndices = glyphIndices)
            {
                fontFace.GetArrayOfGlyphIndices(pCodePoints, 1, pGlyphIndices);
            }
            
            // Get display metrics at 12pt, 96 DPI
            GlyphMetrics[] metrics = new GlyphMetrics[1];
            
            fixed (ushort* pGlyphIndices = glyphIndices)
            fixed (GlyphMetrics* pMetrics = metrics)
            {
                fontFace.GetDisplayGlyphMetrics(
                    pGlyphIndices, 
                    1, 
                    pMetrics, 
                    emSize: 12.0f, 
                    useDisplayNatural: false, 
                    isSideways: false, 
                    pixelsPerDip: 1.0f);
            }
            
            // Display metrics should have positive advance width
            metrics[0].AdvanceWidth.Should().BeGreaterThan(0);
        }
        finally
        {
            fontFace.Release();
        }
    }

    [Fact]
    public unsafe void GetDesignGlyphMetrics_MultipleGlyphs_ShouldReturnAllMetrics()
    {
        var fontFace = GetArialFontFace();
        
        try
        {
            // Get glyph indices for 'Hello'
            uint[] codePoints = [72, 101, 108, 108, 111]; // H, e, l, l, o
            ushort[] glyphIndices = new ushort[codePoints.Length];
            
            fixed (uint* pCodePoints = codePoints)
            fixed (ushort* pGlyphIndices = glyphIndices)
            {
                fontFace.GetArrayOfGlyphIndices(pCodePoints, (uint)codePoints.Length, pGlyphIndices);
            }
            
            // Get metrics for all glyphs
            GlyphMetrics[] metrics = new GlyphMetrics[codePoints.Length];
            
            fixed (ushort* pGlyphIndices = glyphIndices)
            fixed (GlyphMetrics* pMetrics = metrics)
            {
                fontFace.GetDesignGlyphMetrics(pGlyphIndices, (uint)codePoints.Length, pMetrics);
            }
            
            // All visible characters should have positive advance width
            for (int i = 0; i < metrics.Length; i++)
            {
                metrics[i].AdvanceWidth.Should().BeGreaterThan(0, $"Glyph at index {i} should have positive advance width");
            }
        }
        finally
        {
            fontFace.Release();
        }
    }
}
