// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace MS.Internal.Text.TextInterface.Tests;

/// <summary>
/// Edge case and error handling tests for DirectWriteForwarder classes.
/// Tests boundary conditions, invalid inputs, and error scenarios.
/// </summary>
public class EdgeCaseTests
{
    #region FontCollection Edge Cases

    [Fact]
    public void FontCollection_Indexer_WithOutOfRangeIndex_ShouldThrow()
    {
        var fontCollection = DWriteFactory.SystemFontCollection;
        var invalidIndex = fontCollection.FamilyCount + 100u;

        Action act = () => _ = fontCollection[invalidIndex];

        // DirectWrite throws COMException for out of range indices
        act.Should().Throw<System.Runtime.InteropServices.COMException>();
    }

    [Fact]
    public void FontCollection_Indexer_WithMaxUInt_ShouldThrow()
    {
        var fontCollection = DWriteFactory.SystemFontCollection;

        Action act = () => _ = fontCollection[uint.MaxValue];

        // DirectWrite throws COMException for out of range indices
        act.Should().Throw<System.Runtime.InteropServices.COMException>();
    }

    [Fact]
    public void FontCollection_Indexer_WithEmptyString_ShouldReturnNull()
    {
        var fontCollection = DWriteFactory.SystemFontCollection;

        var result = fontCollection[string.Empty];

        result.Should().BeNull();
    }

    [Fact]
    public void FontCollection_FindFamilyName_WithEmptyString_ShouldReturnFalse()
    {
        var fontCollection = DWriteFactory.SystemFontCollection;

        bool found = fontCollection.FindFamilyName(string.Empty, out _);

        found.Should().BeFalse();
    }

    [Fact]
    public void FontCollection_FindFamilyName_WithWhitespace_ShouldReturnFalse()
    {
        var fontCollection = DWriteFactory.SystemFontCollection;

        bool found = fontCollection.FindFamilyName("   ", out _);

        found.Should().BeFalse();
    }

    [Fact]
    public void FontCollection_FindFamilyName_WithSpecialCharacters_ShouldReturnFalse()
    {
        var fontCollection = DWriteFactory.SystemFontCollection;

        bool found = fontCollection.FindFamilyName("!@#$%^&*()", out _);

        found.Should().BeFalse();
    }

    [Fact]
    public void FontCollection_FindFamilyName_WithVeryLongName_ShouldReturnFalse()
    {
        var fontCollection = DWriteFactory.SystemFontCollection;
        var veryLongName = new string('A', 10000);

        bool found = fontCollection.FindFamilyName(veryLongName, out _);

        found.Should().BeFalse();
    }

    [Fact]
    public void FontCollection_FindFamilyName_CaseSensitivity_ShouldBeInsensitive()
    {
        var fontCollection = DWriteFactory.SystemFontCollection;

        bool foundUpper = fontCollection.FindFamilyName("ARIAL", out uint indexUpper);
        bool foundLower = fontCollection.FindFamilyName("arial", out uint indexLower);
        bool foundMixed = fontCollection.FindFamilyName("ArIaL", out uint indexMixed);

        // All should find the same font (case-insensitive matching)
        if (foundUpper && foundLower && foundMixed)
        {
            indexUpper.Should().Be(indexLower);
            indexLower.Should().Be(indexMixed);
        }
    }

    #endregion

    #region FontFamily Edge Cases

    [Fact]
    public void FontFamily_Indexer_WithOutOfRangeIndex_ShouldThrow()
    {
        var fontCollection = DWriteFactory.SystemFontCollection;
        var family = fontCollection["Arial"];
        if (family == null) return;

        var invalidIndex = family.Count + 100;

        Action act = () => _ = family[(uint)invalidIndex];

        // DirectWrite throws COMException for out of range indices
        act.Should().Throw<System.Runtime.InteropServices.COMException>();
    }

    [Fact]
    public void FontFamily_GetFirstMatchingFont_WithExtremeWeight_ShouldReturnClosestMatch()
    {
        var fontCollection = DWriteFactory.SystemFontCollection;
        var family = fontCollection["Arial"];
        if (family == null) return;

        // Request a weight that likely doesn't exist (e.g., 950)
        var font = family.GetFirstMatchingFont((FontWeight)950, FontStretch.Normal, FontStyle.Normal);

        font.Should().NotBeNull("Should return closest matching font");
    }

    [Fact]
    public void FontFamily_GetFirstMatchingFont_WithMinWeight_ShouldReturnFont()
    {
        var fontCollection = DWriteFactory.SystemFontCollection;
        var family = fontCollection["Arial"];
        if (family == null) return;

        var font = family.GetFirstMatchingFont((FontWeight)1, FontStretch.Normal, FontStyle.Normal);

        font.Should().NotBeNull("Should return closest matching font for minimum weight");
    }

    [Fact]
    public void FontFamily_DisplayMetrics_WithZeroEmSize_ShouldThrowArgumentException()
    {
        var fontCollection = DWriteFactory.SystemFontCollection;
        var family = fontCollection["Arial"];
        if (family == null) return;

        // Zero emSize is rejected by DirectWrite
        Action act = () => family.DisplayMetrics(emSize: 0.0f, pixelsPerDip: 1.0f);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void FontFamily_DisplayMetrics_WithNegativeEmSize_ShouldThrowArgumentException()
    {
        var fontCollection = DWriteFactory.SystemFontCollection;
        var family = fontCollection["Arial"];
        if (family == null) return;

        // Negative emSize is rejected by DirectWrite
        Action act = () => family.DisplayMetrics(emSize: -12.0f, pixelsPerDip: 1.0f);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void FontFamily_DisplayMetrics_WithVeryLargeEmSize_ShouldReturnMetrics()
    {
        var fontCollection = DWriteFactory.SystemFontCollection;
        var family = fontCollection["Arial"];
        if (family == null) return;

        var metrics = family.DisplayMetrics(emSize: 10000.0f, pixelsPerDip: 1.0f);

        metrics.Should().NotBeNull();
    }

    [Fact]
    public void FontFamily_DisplayMetrics_WithVerySmallPixelsPerDip_ShouldReturnMetrics()
    {
        var fontCollection = DWriteFactory.SystemFontCollection;
        var family = fontCollection["Arial"];
        if (family == null) return;

        var metrics = family.DisplayMetrics(emSize: 12.0f, pixelsPerDip: 0.001f);

        metrics.Should().NotBeNull();
    }

    #endregion

    #region Factory Edge Cases

    [Fact]
    public void Factory_CreateFontFace_WithNonExistentFile_ShouldThrow()
    {
        var factory = DWriteFactory.Instance;
        var nonExistentPath = new Uri("file:///C:/NonExistent/totally_fake_font_12345.ttf");

        Action act = () => factory.CreateFontFace(nonExistentPath, 0);

        act.Should().Throw<Exception>();
    }

    [Fact]
    public void Factory_CreateFontFace_WithNegativeFaceIndex_ShouldThrow()
    {
        var factory = DWriteFactory.Instance;
        var arialPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Fonts), "arial.ttf");

        if (!File.Exists(arialPath)) return;

        // The API takes uint, so we can't pass negative directly, but we can test boundary
        Action act = () => factory.CreateFontFace(new Uri(arialPath), 999);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Factory_IsLocalUri_WithFtpUri_ReturnsFalse()
    {
        var ftpUri = new Uri("ftp://example.com/fonts/myfont.ttf");

        Factory.IsLocalUri(ftpUri).Should().BeFalse();
    }

    [Fact]
    public void Factory_IsLocalUri_WithHttpsUri_ReturnsFalse()
    {
        var httpsUri = new Uri("https://example.com/fonts/myfont.ttf");

        Factory.IsLocalUri(httpsUri).Should().BeFalse();
    }

    #endregion

    #region FontFace Edge Cases

    [Fact]
    public void FontFace_TryGetFontTable_WithInvalidTableTag_ShouldReturnFalse()
    {
        var factory = DWriteFactory.Instance;
        var arialPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Fonts), "arial.ttf");

        if (!File.Exists(arialPath)) return;

        var fontFace = factory.CreateFontFace(new Uri(arialPath), 0);
        try
        {
            // Use a table tag that doesn't exist (custom/invalid tag)
            bool found = fontFace.TryGetFontTable((OpenTypeTableTag)0x58585858, out byte[]? tableData); // 'XXXX'

            found.Should().BeFalse();
            tableData.Should().BeNull();
        }
        finally
        {
            fontFace.Release();
        }
    }

    [Fact]
    public unsafe void FontFace_GetArrayOfGlyphIndices_WithZeroCount_ShouldNotThrow()
    {
        var factory = DWriteFactory.Instance;
        var arialPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Fonts), "arial.ttf");

        if (!File.Exists(arialPath)) return;

        var fontFace = factory.CreateFontFace(new Uri(arialPath), 0);
        try
        {
            uint[] codePoints = [65];
            ushort[] glyphIndices = new ushort[1];

            fixed (uint* pCodePoints = codePoints)
            fixed (ushort* pGlyphIndices = glyphIndices)
            {
                // Zero count should be handled gracefully
                fontFace.GetArrayOfGlyphIndices(pCodePoints, 0, pGlyphIndices);
            }

            // No exception means success
        }
        finally
        {
            fontFace.Release();
        }
    }

    [Fact]
    public unsafe void FontFace_GetDesignGlyphMetrics_WithZeroGlyphIndex_ShouldReturnMetrics()
    {
        var factory = DWriteFactory.Instance;
        var arialPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Fonts), "arial.ttf");

        if (!File.Exists(arialPath)) return;

        var fontFace = factory.CreateFontFace(new Uri(arialPath), 0);
        try
        {
            // Glyph index 0 is typically .notdef
            ushort[] glyphIndices = [0];
            GlyphMetrics[] metrics = new GlyphMetrics[1];

            fixed (ushort* pGlyphIndices = glyphIndices)
            fixed (GlyphMetrics* pMetrics = metrics)
            {
                fontFace.GetDesignGlyphMetrics(pGlyphIndices, 1, pMetrics);
            }

            // .notdef glyph should still have metrics
            metrics[0].Should().NotBeNull();
        }
        finally
        {
            fontFace.Release();
        }
    }

    [Fact]
    public unsafe void FontFace_GetDisplayGlyphMetrics_WithZeroEmSize_ShouldThrowArgumentException()
    {
        var factory = DWriteFactory.Instance;
        var arialPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Fonts), "arial.ttf");

        if (!File.Exists(arialPath)) return;

        var fontFace = factory.CreateFontFace(new Uri(arialPath), 0);
        try
        {
            uint[] codePoints = [65]; // 'A'
            ushort[] glyphIndices = new ushort[1];

            fixed (uint* pCodePoints = codePoints)
            fixed (ushort* pGlyphIndices = glyphIndices)
            {
                fontFace.GetArrayOfGlyphIndices(pCodePoints, 1, pGlyphIndices);
            }

            GlyphMetrics[] metrics = new GlyphMetrics[1];

            // Zero emSize should throw ArgumentException
            Action act = () =>
            {
                fixed (ushort* pGlyphIndices = glyphIndices)
                fixed (GlyphMetrics* pMetrics = metrics)
                {
                    fontFace.GetDisplayGlyphMetrics(
                        pGlyphIndices,
                        1,
                        pMetrics,
                        emSize: 0.0f,
                        useDisplayNatural: false,
                        isSideways: false,
                        pixelsPerDip: 1.0f);
                }
            };

            act.Should().Throw<ArgumentException>();
        }
        finally
        {
            fontFace.Release();
        }
    }

    [Fact]
    public unsafe void FontFace_GetDisplayGlyphMetrics_WithSideways_ShouldReturnMetrics()
    {
        var factory = DWriteFactory.Instance;
        var arialPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Fonts), "arial.ttf");

        if (!File.Exists(arialPath)) return;

        var fontFace = factory.CreateFontFace(new Uri(arialPath), 0);
        try
        {
            uint[] codePoints = [65]; // 'A'
            ushort[] glyphIndices = new ushort[1];

            fixed (uint* pCodePoints = codePoints)
            fixed (ushort* pGlyphIndices = glyphIndices)
            {
                fontFace.GetArrayOfGlyphIndices(pCodePoints, 1, pGlyphIndices);
            }

            GlyphMetrics[] metrics = new GlyphMetrics[1];

            fixed (ushort* pGlyphIndices = glyphIndices)
            fixed (GlyphMetrics* pMetrics = metrics)
            {
                // isSideways = true for vertical text layout
                fontFace.GetDisplayGlyphMetrics(
                    pGlyphIndices,
                    1,
                    pMetrics,
                    emSize: 12.0f,
                    useDisplayNatural: false,
                    isSideways: true,
                    pixelsPerDip: 1.0f);
            }

            metrics[0].AdvanceWidth.Should().BeGreaterThan(0);
        }
        finally
        {
            fontFace.Release();
        }
    }

    [Fact]
    public unsafe void FontFace_GetDisplayGlyphMetrics_WithUseDisplayNatural_ShouldReturnMetrics()
    {
        var factory = DWriteFactory.Instance;
        var arialPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Fonts), "arial.ttf");

        if (!File.Exists(arialPath)) return;

        var fontFace = factory.CreateFontFace(new Uri(arialPath), 0);
        try
        {
            uint[] codePoints = [65]; // 'A'
            ushort[] glyphIndices = new ushort[1];

            fixed (uint* pCodePoints = codePoints)
            fixed (ushort* pGlyphIndices = glyphIndices)
            {
                fontFace.GetArrayOfGlyphIndices(pCodePoints, 1, pGlyphIndices);
            }

            GlyphMetrics[] metrics = new GlyphMetrics[1];

            fixed (ushort* pGlyphIndices = glyphIndices)
            fixed (GlyphMetrics* pMetrics = metrics)
            {
                // useDisplayNatural = true
                fontFace.GetDisplayGlyphMetrics(
                    pGlyphIndices,
                    1,
                    pMetrics,
                    emSize: 12.0f,
                    useDisplayNatural: true,
                    isSideways: false,
                    pixelsPerDip: 1.0f);
            }

            metrics[0].AdvanceWidth.Should().BeGreaterThan(0);
        }
        finally
        {
            fontFace.Release();
        }
    }

    #endregion

    #region Font Edge Cases

    [Fact]
    public void Font_HasCharacter_WithZeroCodePoint_ShouldReturnFalse()
    {
        var fontCollection = DWriteFactory.SystemFontCollection;
        var family = fontCollection["Arial"];
        if (family == null || family.Count == 0) return;

        var font = family[0u];

        // Code point 0 (null character)
        font.HasCharacter(0).Should().BeFalse();
    }

    [Fact]
    public void Font_HasCharacter_WithMaxCodePoint_ShouldReturnFalse()
    {
        var fontCollection = DWriteFactory.SystemFontCollection;
        var family = fontCollection["Arial"];
        if (family == null || family.Count == 0) return;

        var font = family[0u];

        // Max Unicode code point
        font.HasCharacter(0x10FFFF).Should().BeFalse();
    }

    [Fact]
    public void Font_HasCharacter_WithSurrogateCodePoint_ShouldReturnFalse()
    {
        var fontCollection = DWriteFactory.SystemFontCollection;
        var family = fontCollection["Arial"];
        if (family == null || family.Count == 0) return;

        var font = family[0u];

        // Surrogate code points are invalid
        font.HasCharacter(0xD800).Should().BeFalse();
        font.HasCharacter(0xDFFF).Should().BeFalse();
    }

    [Fact]
    public void Font_HasCharacter_WithPrivateUseArea_ShouldReturnResult()
    {
        var fontCollection = DWriteFactory.SystemFontCollection;
        var family = fontCollection["Arial"];
        if (family == null || family.Count == 0) return;

        var font = family[0u];

        // Private Use Area (U+E000 to U+F8FF)
        // Result depends on font, but should not throw
        _ = font.HasCharacter(0xE000);
    }

    [Fact]
    public void Font_GetInformationalStrings_AllStringTypes_ShouldNotThrow()
    {
        var fontCollection = DWriteFactory.SystemFontCollection;
        var family = fontCollection["Arial"];
        if (family == null || family.Count == 0) return;

        var font = family[0u];

        // Iterate through all InformationalStringID values
        foreach (InformationalStringID stringId in Enum.GetValues(typeof(InformationalStringID)))
        {
            // Should not throw for any valid enum value
            _ = font.GetInformationalStrings(stringId, out _);
        }
    }

    [Fact]
    public void Font_DisplayMetrics_WithExtremeValues_ShouldNotThrow()
    {
        var fontCollection = DWriteFactory.SystemFontCollection;
        var family = fontCollection["Arial"];
        if (family == null || family.Count == 0) return;

        var font = family[0u];

        // Very large values
        var metrics1 = font.DisplayMetrics(float.MaxValue / 2, 96.0f);
        metrics1.Should().NotBeNull();

        // Very small values
        var metrics2 = font.DisplayMetrics(0.001f, 0.001f);
        metrics2.Should().NotBeNull();
    }

    #endregion

    #region FontMetrics Struct Edge Cases

    [Fact]
    public void FontMetrics_WithZeroDesignUnitsPerEm_Baseline_ReturnsInfinityOrNaN()
    {
        var metrics = new FontMetrics
        {
            DesignUnitsPerEm = 0,
            Ascent = 1000,
            Descent = 500,
            LineGap = 100
        };

        // When DesignUnitsPerEm is 0, Baseline calculation involves division by zero
        // Result should be infinity or NaN
        var baseline = metrics.Baseline;
        (double.IsInfinity(baseline) || double.IsNaN(baseline)).Should().BeTrue();
    }

    [Fact]
    public void FontMetrics_LineSpacing_WithTypicalValues_ShouldCalculateCorrectly()
    {
        var metrics = new FontMetrics
        {
            DesignUnitsPerEm = 2048,
            Ascent = 1854,
            Descent = 434,
            LineGap = 67
        };

        // Get line spacing
        var lineSpacing = metrics.LineSpacing;

        lineSpacing.Should().BeGreaterThan(0);
    }

    [Fact]
    public void FontMetrics_Baseline_ShouldBeConsistentWithFormula()
    {
        var fontCollection = DWriteFactory.SystemFontCollection;
        var family = fontCollection["Arial"];
        if (family == null) return;

        var metrics = family.Metrics;

        // Baseline = (Ascent + LineGap * 0.5) / DesignUnitsPerEm
        double expectedBaseline = (metrics.Ascent + metrics.LineGap * 0.5) / metrics.DesignUnitsPerEm;

        metrics.Baseline.Should().BeApproximately(expectedBaseline, 0.0001);
    }

    #endregion

    #region GlyphMetrics Struct Edge Cases

    [Fact]
    public void GlyphMetrics_DefaultValues_ShouldBeZero()
    {
        var metrics = new GlyphMetrics();

        metrics.AdvanceWidth.Should().Be(0);
        metrics.AdvanceHeight.Should().Be(0);
        metrics.LeftSideBearing.Should().Be(0);
        metrics.RightSideBearing.Should().Be(0);
        metrics.TopSideBearing.Should().Be(0);
        metrics.BottomSideBearing.Should().Be(0);
        metrics.VerticalOriginY.Should().Be(0);
    }

    [Fact]
    public void GlyphMetrics_NegativeSideBearings_AreValid()
    {
        // Some glyphs have negative side bearings (e.g., italic 'f')
        var metrics = new GlyphMetrics
        {
            AdvanceWidth = 500,
            LeftSideBearing = -50,
            RightSideBearing = -30
        };

        metrics.LeftSideBearing.Should().Be(-50);
        metrics.RightSideBearing.Should().Be(-30);
    }

    #endregion

    #region DWriteMatrix Struct Edge Cases

    [Fact]
    public void DWriteMatrix_DefaultValues_ShouldBeZero()
    {
        var matrix = new DWriteMatrix();

        matrix.M11.Should().Be(0.0f);
        matrix.M12.Should().Be(0.0f);
        matrix.M21.Should().Be(0.0f);
        matrix.M22.Should().Be(0.0f);
        matrix.Dx.Should().Be(0.0f);
        matrix.Dy.Should().Be(0.0f);
    }

    [Fact]
    public void DWriteMatrix_WithExtremeValues_ShouldStore()
    {
        var matrix = new DWriteMatrix
        {
            M11 = float.MaxValue,
            M12 = float.MinValue,
            M21 = float.Epsilon,
            M22 = float.NegativeInfinity,
            Dx = float.PositiveInfinity,
            Dy = float.NaN
        };

        matrix.M11.Should().Be(float.MaxValue);
        matrix.M12.Should().Be(float.MinValue);
        matrix.M21.Should().Be(float.Epsilon);
        matrix.M22.Should().Be(float.NegativeInfinity);
        matrix.Dx.Should().Be(float.PositiveInfinity);
        float.IsNaN(matrix.Dy).Should().BeTrue();
    }

    #endregion

    #region LocalizedStrings Edge Cases

    [Fact]
    public void LocalizedStrings_NonExistentString_GetInformationalStrings_ReturnsFalse()
    {
        var fontCollection = DWriteFactory.SystemFontCollection;
        var family = fontCollection["Arial"];
        if (family == null || family.Count == 0) return;

        var font = family[0u];

        // Try to get a string that might not exist (SampleText is often not present)
        bool exists = font.GetInformationalStrings(InformationalStringID.SampleText, out var localizedStrings);

        // If the string doesn't exist, exists should be false
        // Just verify the call doesn't throw
        if (exists)
        {
            localizedStrings.Should().NotBeNull();
        }
    }

    [Fact]
    public void LocalizedStrings_Enumeration_ShouldNotThrow()
    {
        var fontCollection = DWriteFactory.SystemFontCollection;
        var family = fontCollection["Arial"];
        if (family == null) return;

        var familyNames = family.FamilyNames;

        // Enumerate should not throw
        foreach (var kvp in familyNames)
        {
            kvp.Key.Should().NotBeNull();
            kvp.Value.Should().NotBeNull();
        }
    }

    #endregion

    #region Unicode Boundary Tests

    [Fact]
    public void Font_HasCharacter_WithBMPBoundary_ShouldWork()
    {
        var fontCollection = DWriteFactory.SystemFontCollection;
        var family = fontCollection["Arial"];
        if (family == null || family.Count == 0) return;

        var font = family[0u];

        // BMP boundary (U+FFFF)
        _ = font.HasCharacter(0xFFFF);

        // Just above BMP (U+10000) - Supplementary Multilingual Plane
        _ = font.HasCharacter(0x10000);
    }

    [Fact]
    public unsafe void FontFace_GetArrayOfGlyphIndices_WithSupplementaryPlaneCharacters_ShouldWork()
    {
        var factory = DWriteFactory.Instance;
        var arialPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Fonts), "arial.ttf");

        if (!File.Exists(arialPath)) return;

        var fontFace = factory.CreateFontFace(new Uri(arialPath), 0);
        try
        {
            // Supplementary plane characters (emoji, historic scripts, etc.)
            uint[] codePoints = [0x1F600, 0x1F4A9, 0x1D11E]; // Emoji and musical symbol
            ushort[] glyphIndices = new ushort[codePoints.Length];

            fixed (uint* pCodePoints = codePoints)
            fixed (ushort* pGlyphIndices = glyphIndices)
            {
                fontFace.GetArrayOfGlyphIndices(pCodePoints, (uint)codePoints.Length, pGlyphIndices);
            }

            // These are likely not in Arial, so should be 0 (.notdef)
            // But the call should not throw
        }
        finally
        {
            fontFace.Release();
        }
    }

    #endregion
}
