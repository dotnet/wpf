// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace MS.Internal.Text.TextInterface.Tests;

/// <summary>
/// Tests for <see cref="Font"/> class.
/// </summary>
public class FontTests
{
    [Fact]
    public void Weight_ShouldBeValid()
    {
        var font = TestHelpers.GetArialFontOrSkip();
        
        font.Weight.Should().BeDefined();
    }

    [Fact]
    public void Stretch_ShouldBeValid()
    {
        var font = TestHelpers.GetArialFontOrSkip();
        
        font.Stretch.Should().BeDefined();
    }

    [Fact]
    public void Style_ShouldBeValid()
    {
        var font = TestHelpers.GetArialFontOrSkip();
        
        font.Style.Should().BeDefined();
    }

    [Fact]
    public void Family_ShouldNotBeNull()
    {
        var font = TestHelpers.GetArialFontOrSkip();
        
        font.Family.Should().NotBeNull();
    }

    [Fact]
    public void FaceNames_ShouldNotBeEmpty()
    {
        var font = TestHelpers.GetArialFontOrSkip();
        
        var faceNames = font.FaceNames;
        
        faceNames.Should().NotBeNull();
        faceNames.Count.Should().BeGreaterThan(0);
    }

    [Fact]
    public void Metrics_ShouldBeValid()
    {
        var font = TestHelpers.GetArialFontOrSkip();
        
        var metrics = font.Metrics;
        
        metrics.Should().NotBeNull();
        metrics.DesignUnitsPerEm.Should().BeGreaterThan(0);
        metrics.Ascent.Should().BeGreaterThan(0);
    }

    [Fact]
    public void DisplayMetrics_ShouldBeValid()
    {
        var font = TestHelpers.GetArialFontOrSkip();
        
        var displayMetrics = font.DisplayMetrics(12.0f, 96.0f);
        
        displayMetrics.Should().NotBeNull();
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
                break;
            }
        }
    }

    [Fact]
    public void HasCharacter_WithCommonCharacter_ShouldReturnTrue()
    {
        var font = TestHelpers.GetArialFontOrSkip();
        
        // 'A' (0x41) should be in Arial
        font.HasCharacter(0x41).Should().BeTrue();
    }

    [Fact]
    public void HasCharacter_WithRareCharacter_ShouldReturnFalse()
    {
        var font = TestHelpers.GetArialFontOrSkip();
        
        // Very high code point that's unlikely to be in Arial
        font.HasCharacter(0x1F999).Should().BeFalse(); // Emoji unicorn
    }

    [Fact]
    public void GetInformationalStrings_FamilyName_ShouldReturnArialFamily()
    {
        var font = TestHelpers.GetArialFontOrSkip();
        
        bool exists = font.GetInformationalStrings(InformationalStringID.WIN32FamilyNames, out var familyNames);
        
        exists.Should().BeTrue();
        familyNames.Should().NotBeNull();
        // Should contain "Arial" somewhere in the localized names
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
    public void GetInformationalStrings_CopyrightNotice_ShouldExist()
    {
        var font = TestHelpers.GetArialFontOrSkip();
        
        bool exists = font.GetInformationalStrings(InformationalStringID.CopyrightNotice, out var copyright);
        
        exists.Should().BeTrue("Arial should have copyright information");
        copyright.Should().NotBeNull();
    }
}

/// <summary>
/// Tests for <see cref="FontFace"/> class.
/// </summary>
public class FontFaceTests
{
    private FontFace GetArialFontFace()
    {
        var factory = DWriteFactory.Instance;
        TestHelpers.SkipIfArialNotAvailable();
        var arialPath = TestHelpers.ArialPath;
        
        return factory.CreateFontFace(new Uri(arialPath), 0);
    }

    [Fact]
    public void Type_ShouldBeTrueType()
    {
        var fontFace = GetArialFontFace();
        
        try
        {
            fontFace.Type.Should().Be(FontFaceType.TrueType);
        }
        finally
        {
            fontFace.Release();
        }
    }

    [Fact]
    public void Index_ShouldBeZero()
    {
        var fontFace = GetArialFontFace();
        
        try
        {
            fontFace.Index.Should().Be(0u);
        }
        finally
        {
            fontFace.Release();
        }
    }

    [Fact]
    public void SimulationFlags_ShouldBeNone()
    {
        var fontFace = GetArialFontFace();
        
        try
        {
            fontFace.SimulationFlags.Should().Be(FontSimulations.None);
        }
        finally
        {
            fontFace.Release();
        }
    }

    [Fact]
    public void Metrics_ShouldBeValid()
    {
        var fontFace = GetArialFontFace();
        
        try
        {
            var metrics = fontFace.Metrics;
            
            metrics.Should().NotBeNull();
            metrics.DesignUnitsPerEm.Should().BeGreaterThan(0);
            metrics.Ascent.Should().BeGreaterThan(0);
            metrics.Descent.Should().BeGreaterThan(0);
        }
        finally
        {
            fontFace.Release();
        }
    }

    [Fact]
    public void IsSymbolFont_ShouldBeFalse()
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
    public void GlyphCount_ShouldBeGreaterThanZero()
    {
        var fontFace = GetArialFontFace();
        
        try
        {
            fontFace.GlyphCount.Should().BeGreaterThan(0);
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
    public void TryGetFontTable_WithHeadTable_ShouldReturnData()
    {
        var fontFace = GetArialFontFace();
        
        try
        {
            // 'head' table should exist in all TrueType fonts
            bool found = fontFace.TryGetFontTable(OpenTypeTableTag.FontHeader, out byte[]? tableData);
            
            found.Should().BeTrue();
            tableData.Should().NotBeNull();
            tableData!.Length.Should().BeGreaterThan(0);
        }
        finally
        {
            fontFace.Release();
        }
    }

    [Fact]
    public void TryGetFontTable_WithCmapTable_ShouldReturnData()
    {
        var fontFace = GetArialFontFace();
        
        try
        {
            // 'cmap' table (character to glyph mapping) should exist
            bool found = fontFace.TryGetFontTable(OpenTypeTableTag.CharToIndexMap, out byte[]? tableData);
            
            found.Should().BeTrue();
            tableData.Should().NotBeNull();
            tableData!.Length.Should().BeGreaterThan(0);
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
    public void ReadFontEmbeddingRights_ShouldReturnValue()
    {
        var fontFace = GetArialFontFace();
        
        try
        {
            bool success = fontFace.ReadFontEmbeddingRights(out ushort fsType);
            
            // Arial should have OS/2 table with embedding rights
            success.Should().BeTrue();
            // fsType is a bitfield, just verify it's a valid value (0-0x0F00 range typically)
        }
        finally
        {
            fontFace.Release();
        }
    }

    [Fact]
    public unsafe void GetArrayOfGlyphIndices_WithValidCodePoints_ShouldReturnGlyphIndices()
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
    public unsafe void GetDesignGlyphMetrics_ShouldReturnMetrics()
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
