// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using System.Windows.Media;

namespace MS.Internal.Text.TextInterface.Tests;

/// <summary>
/// Additional tests for <see cref="TextAnalyzer"/> to improve code coverage.
/// Focuses on control character paths and edge cases not covered by existing tests.
/// </summary>
public class TextAnalyzerControlCharacterTests
{
    private static Font GetTestFont()
    {
        return TestHelpers.GetArialFontOrSkip();
    }

    private static TextAnalyzer GetTextAnalyzer()
    {
        return DWriteFactory.Instance.CreateTextAnalyzer();
    }

    private static unsafe ushort GetBlankGlyphIndex(Font font)
    {
        var fontFace = font.GetFontFace();
        try
        {
            uint spaceCodePoint = ' ';
            ushort glyphIndex = 0;
            fontFace.GetArrayOfGlyphIndices(&spaceCodePoint, 1, &glyphIndex);
            return glyphIndex;
        }
        finally
        {
            fontFace.Release();
        }
    }

    private static unsafe IList<Span>? Itemize(string text, CultureInfo? culture = null)
    {
        if (string.IsNullOrEmpty(text))
            return null;

        culture ??= CultureInfo.InvariantCulture;

        fixed (char* pText = text)
        {
            return TextAnalyzer.Itemize(
                pText,
                (uint)text.Length,
                culture,
                (Native.IDWriteFactory*)DWriteFactory.Instance.DWriteFactory,
                false,
                null,
                false,
                0,
                ClassificationUtility.Instance,
                MS.Internal.TextFormatting.UnsafeNativeMethods.CreateTextAnalysisSink,
                MS.Internal.TextFormatting.UnsafeNativeMethods.GetScriptAnalysisList,
                MS.Internal.TextFormatting.UnsafeNativeMethods.GetNumberSubstitutionList,
                MS.Internal.TextFormatting.UnsafeNativeMethods.CreateTextAnalysisSource
            );
        }
    }

    #region Control Character Glyph Tests

    [Fact]
    public unsafe void GetGlyphsAndPlacements_SoftHyphen_ShouldSucceed()
    {
        // Soft hyphen (U+00AD) is replaced with visible hyphen (U+002D) during line breaking
        // This tests the CharHyphen handling in GetBlankGlyphsForControlCharacters
        var font = GetTestFont();
        // The soft hyphen character
        string text = "word\u00ADbreak";

        var spans = Itemize(text);
        Assert.SkipUnless(spans != null && spans.Count > 0, "Itemization failed");

        var analyzer = GetTextAnalyzer();
        ushort blankGlyphIndex = GetBlankGlyphIndex(font);

        fixed (char* pText = text)
        {
            analyzer.GetGlyphsAndTheirPlacements(
                pText,
                (uint)text.Length,
                font,
                blankGlyphIndex,
                false,  // isSideways
                false,  // isRightToLeft
                CultureInfo.InvariantCulture,
                null,   // features
                null,   // featureRangeLengths
                12.0,   // fontSize
                1.0,    // scalingFactor
                1.0f,   // pixelsPerDip
                TextFormattingMode.Ideal,
                (ItemProps)spans[0].element,
                out ushort[] clusterMap,
                out ushort[] glyphIndices,
                out int[] glyphAdvances,
                out GlyphOffset[] glyphOffsets
            );

            clusterMap.Should().NotBeNull();
            glyphIndices.Should().NotBeNull();
            glyphAdvances.Should().NotBeNull();
            glyphOffsets.Should().NotBeNull();
        }
    }

    [Fact]
    public unsafe void GetGlyphsAndPlacements_TabCharacter_ShouldSucceed()
    {
        // Tab character (U+0009) is a control character
        var font = GetTestFont();
        string text = "Hello\tWorld";

        var spans = Itemize(text);
        Assert.SkipUnless(spans != null && spans.Count > 0, "Itemization failed");

        var analyzer = GetTextAnalyzer();
        ushort blankGlyphIndex = GetBlankGlyphIndex(font);

        fixed (char* pText = text)
        {
            analyzer.GetGlyphsAndTheirPlacements(
                pText,
                (uint)text.Length,
                font,
                blankGlyphIndex,
                false,
                false,
                CultureInfo.InvariantCulture,
                null,
                null,
                12.0,
                1.0,
                1.0f,
                TextFormattingMode.Ideal,
                (ItemProps)spans[0].element,
                out ushort[] clusterMap,
                out ushort[] glyphIndices,
                out int[] glyphAdvances,
                out GlyphOffset[] glyphOffsets
            );

            clusterMap.Should().NotBeNull();
            glyphIndices.Should().NotBeNull();
        }
    }

    [Fact]
    public unsafe void GetGlyphsAndPlacements_NewlineCharacter_ShouldSucceed()
    {
        // Newline characters are control characters
        var font = GetTestFont();
        string text = "Line1\nLine2";

        var spans = Itemize(text);
        Assert.SkipUnless(spans != null && spans.Count > 0, "Itemization failed");

        var analyzer = GetTextAnalyzer();
        ushort blankGlyphIndex = GetBlankGlyphIndex(font);

        fixed (char* pText = text)
        {
            analyzer.GetGlyphsAndTheirPlacements(
                pText,
                (uint)text.Length,
                font,
                blankGlyphIndex,
                false,
                false,
                CultureInfo.InvariantCulture,
                null,
                null,
                12.0,
                1.0,
                1.0f,
                TextFormattingMode.Ideal,
                (ItemProps)spans[0].element,
                out ushort[] clusterMap,
                out ushort[] glyphIndices,
                out int[] glyphAdvances,
                out GlyphOffset[] glyphOffsets
            );

            clusterMap.Should().NotBeNull();
            glyphIndices.Should().NotBeNull();
        }
    }

    [Fact]
    public unsafe void GetGlyphsAndPlacements_CarriageReturn_ShouldSucceed()
    {
        // Carriage return (U+000D) is a control character
        var font = GetTestFont();
        string text = "Line1\rLine2";

        var spans = Itemize(text);
        Assert.SkipUnless(spans != null && spans.Count > 0, "Itemization failed");

        var analyzer = GetTextAnalyzer();
        ushort blankGlyphIndex = GetBlankGlyphIndex(font);

        fixed (char* pText = text)
        {
            analyzer.GetGlyphsAndTheirPlacements(
                pText,
                (uint)text.Length,
                font,
                blankGlyphIndex,
                false,
                false,
                CultureInfo.InvariantCulture,
                null,
                null,
                12.0,
                1.0,
                1.0f,
                TextFormattingMode.Ideal,
                (ItemProps)spans[0].element,
                out ushort[] clusterMap,
                out ushort[] glyphIndices,
                out int[] glyphAdvances,
                out GlyphOffset[] glyphOffsets
            );

            clusterMap.Should().NotBeNull();
            glyphIndices.Should().NotBeNull();
        }
    }

    [Fact]
    public unsafe void GetGlyphsAndPlacements_CRLFSequence_ShouldSucceed()
    {
        // Windows-style line ending
        var font = GetTestFont();
        string text = "Line1\r\nLine2";

        var spans = Itemize(text);
        Assert.SkipUnless(spans != null && spans.Count > 0, "Itemization failed");

        var analyzer = GetTextAnalyzer();
        ushort blankGlyphIndex = GetBlankGlyphIndex(font);

        fixed (char* pText = text)
        {
            analyzer.GetGlyphsAndTheirPlacements(
                pText,
                (uint)text.Length,
                font,
                blankGlyphIndex,
                false,
                false,
                CultureInfo.InvariantCulture,
                null,
                null,
                12.0,
                1.0,
                1.0f,
                TextFormattingMode.Ideal,
                (ItemProps)spans[0].element,
                out ushort[] clusterMap,
                out ushort[] glyphIndices,
                out int[] glyphAdvances,
                out GlyphOffset[] glyphOffsets
            );

            clusterMap.Should().NotBeNull();
            glyphIndices.Should().NotBeNull();
        }
    }

    [Fact]
    public unsafe void GetGlyphsAndPlacements_VerticalTab_ShouldSucceed()
    {
        // Vertical tab (U+000B) is a control character
        var font = GetTestFont();
        string text = "Hello\vWorld";

        var spans = Itemize(text);
        Assert.SkipUnless(spans != null && spans.Count > 0, "Itemization failed");

        var analyzer = GetTextAnalyzer();
        ushort blankGlyphIndex = GetBlankGlyphIndex(font);

        fixed (char* pText = text)
        {
            analyzer.GetGlyphsAndTheirPlacements(
                pText,
                (uint)text.Length,
                font,
                blankGlyphIndex,
                false,
                false,
                CultureInfo.InvariantCulture,
                null,
                null,
                12.0,
                1.0,
                1.0f,
                TextFormattingMode.Ideal,
                (ItemProps)spans[0].element,
                out ushort[] clusterMap,
                out ushort[] glyphIndices,
                out int[] glyphAdvances,
                out GlyphOffset[] glyphOffsets
            );

            clusterMap.Should().NotBeNull();
            glyphIndices.Should().NotBeNull();
        }
    }

    [Fact]
    public unsafe void GetGlyphsAndPlacements_FormFeed_ShouldSucceed()
    {
        // Form feed (U+000C) is a control character
        var font = GetTestFont();
        string text = "Page1\fPage2";

        var spans = Itemize(text);
        Assert.SkipUnless(spans != null && spans.Count > 0, "Itemization failed");

        var analyzer = GetTextAnalyzer();
        ushort blankGlyphIndex = GetBlankGlyphIndex(font);

        fixed (char* pText = text)
        {
            analyzer.GetGlyphsAndTheirPlacements(
                pText,
                (uint)text.Length,
                font,
                blankGlyphIndex,
                false,
                false,
                CultureInfo.InvariantCulture,
                null,
                null,
                12.0,
                1.0,
                1.0f,
                TextFormattingMode.Ideal,
                (ItemProps)spans[0].element,
                out ushort[] clusterMap,
                out ushort[] glyphIndices,
                out int[] glyphAdvances,
                out GlyphOffset[] glyphOffsets
            );

            clusterMap.Should().NotBeNull();
            glyphIndices.Should().NotBeNull();
        }
    }

    #endregion

    #region Hyphen Character Tests (exercises GetBlankGlyphsForControlCharacters hyphen path)

    [Fact]
    public unsafe void GetGlyphsAndPlacements_HyphenCharacter_ShouldSucceed()
    {
        // The actual hyphen character (U+002D) which is CharHyphen in the code
        var font = GetTestFont();
        string text = "word-break";

        var spans = Itemize(text);
        Assert.SkipUnless(spans != null && spans.Count > 0, "Itemization failed");

        var analyzer = GetTextAnalyzer();
        ushort blankGlyphIndex = GetBlankGlyphIndex(font);

        fixed (char* pText = text)
        {
            analyzer.GetGlyphsAndTheirPlacements(
                pText,
                (uint)text.Length,
                font,
                blankGlyphIndex,
                false,
                false,
                CultureInfo.InvariantCulture,
                null,
                null,
                12.0,
                1.0,
                1.0f,
                TextFormattingMode.Ideal,
                (ItemProps)spans[0].element,
                out ushort[] clusterMap,
                out ushort[] glyphIndices,
                out int[] glyphAdvances,
                out GlyphOffset[] glyphOffsets
            );

            clusterMap.Should().NotBeNull();
            glyphIndices.Should().NotBeNull();
            // Hyphen should have a non-zero advance
            glyphAdvances.Should().Contain(a => a > 0);
        }
    }

    [Fact]
    public unsafe void GetGlyphsAndPlacements_MultipleHyphens_ShouldSucceed()
    {
        var font = GetTestFont();
        string text = "one-two-three-four";

        var spans = Itemize(text);
        Assert.SkipUnless(spans != null && spans.Count > 0, "Itemization failed");

        var analyzer = GetTextAnalyzer();
        ushort blankGlyphIndex = GetBlankGlyphIndex(font);

        fixed (char* pText = text)
        {
            analyzer.GetGlyphsAndTheirPlacements(
                pText,
                (uint)text.Length,
                font,
                blankGlyphIndex,
                false,
                false,
                CultureInfo.InvariantCulture,
                null,
                null,
                12.0,
                1.0,
                1.0f,
                TextFormattingMode.Ideal,
                (ItemProps)spans[0].element,
                out ushort[] clusterMap,
                out ushort[] glyphIndices,
                out int[] glyphAdvances,
                out GlyphOffset[] glyphOffsets
            );

            clusterMap.Should().NotBeNull();
            glyphIndices.Should().NotBeNull();
        }
    }

    #endregion

    #region TextFormattingMode Coverage (exercises different measuring mode paths)

    [Fact]
    public unsafe void GetGlyphsAndPlacements_DisplayMode_ShouldSucceed()
    {
        var font = GetTestFont();
        string text = "Display Mode Test";

        var spans = Itemize(text);
        Assert.SkipUnless(spans != null && spans.Count > 0, "Itemization failed");

        var analyzer = GetTextAnalyzer();
        ushort blankGlyphIndex = GetBlankGlyphIndex(font);

        fixed (char* pText = text)
        {
            analyzer.GetGlyphsAndTheirPlacements(
                pText,
                (uint)text.Length,
                font,
                blankGlyphIndex,
                false,
                false,
                CultureInfo.InvariantCulture,
                null,
                null,
                12.0,
                1.0,
                1.0f,
                TextFormattingMode.Display,  // Use Display mode
                (ItemProps)spans[0].element,
                out ushort[] clusterMap,
                out ushort[] glyphIndices,
                out int[] glyphAdvances,
                out GlyphOffset[] glyphOffsets
            );

            clusterMap.Should().NotBeNull();
            glyphIndices.Should().NotBeNull();
            glyphAdvances.Should().NotBeNull();
            glyphOffsets.Should().NotBeNull();
        }
    }

    [Fact]
    public unsafe void GetGlyphsAndPlacements_IdealMode_ShouldSucceed()
    {
        var font = GetTestFont();
        string text = "Ideal Mode Test";

        var spans = Itemize(text);
        Assert.SkipUnless(spans != null && spans.Count > 0, "Itemization failed");

        var analyzer = GetTextAnalyzer();
        ushort blankGlyphIndex = GetBlankGlyphIndex(font);

        fixed (char* pText = text)
        {
            analyzer.GetGlyphsAndTheirPlacements(
                pText,
                (uint)text.Length,
                font,
                blankGlyphIndex,
                false,
                false,
                CultureInfo.InvariantCulture,
                null,
                null,
                12.0,
                1.0,
                1.0f,
                TextFormattingMode.Ideal,
                (ItemProps)spans[0].element,
                out ushort[] clusterMap,
                out ushort[] glyphIndices,
                out int[] glyphAdvances,
                out GlyphOffset[] glyphOffsets
            );

            clusterMap.Should().NotBeNull();
            glyphIndices.Should().NotBeNull();
        }
    }

    [Fact]
    public unsafe void GetGlyphsAndPlacements_DisplayMode_WithControlChars_ShouldSucceed()
    {
        // This exercises GetGlyphPlacementsForControlCharacters with Display mode
        var font = GetTestFont();
        string text = "Hello\tWorld";

        var spans = Itemize(text);
        Assert.SkipUnless(spans != null && spans.Count > 0, "Itemization failed");

        var analyzer = GetTextAnalyzer();
        ushort blankGlyphIndex = GetBlankGlyphIndex(font);

        fixed (char* pText = text)
        {
            analyzer.GetGlyphsAndTheirPlacements(
                pText,
                (uint)text.Length,
                font,
                blankGlyphIndex,
                false,
                false,
                CultureInfo.InvariantCulture,
                null,
                null,
                12.0,
                1.0,
                1.0f,
                TextFormattingMode.Display,
                (ItemProps)spans[0].element,
                out ushort[] clusterMap,
                out ushort[] glyphIndices,
                out int[] glyphAdvances,
                out GlyphOffset[] glyphOffsets
            );

            clusterMap.Should().NotBeNull();
            glyphIndices.Should().NotBeNull();
        }
    }

    #endregion

    #region Sideways Text Coverage

    [Fact]
    public unsafe void GetGlyphsAndPlacements_Sideways_ShouldSucceed()
    {
        var font = GetTestFont();
        string text = "Sideways";

        var spans = Itemize(text);
        Assert.SkipUnless(spans != null && spans.Count > 0, "Itemization failed");

        var analyzer = GetTextAnalyzer();
        ushort blankGlyphIndex = GetBlankGlyphIndex(font);

        fixed (char* pText = text)
        {
            analyzer.GetGlyphsAndTheirPlacements(
                pText,
                (uint)text.Length,
                font,
                blankGlyphIndex,
                true,   // isSideways = true
                false,
                CultureInfo.InvariantCulture,
                null,
                null,
                12.0,
                1.0,
                1.0f,
                TextFormattingMode.Ideal,
                (ItemProps)spans[0].element,
                out ushort[] clusterMap,
                out ushort[] glyphIndices,
                out int[] glyphAdvances,
                out GlyphOffset[] glyphOffsets
            );

            clusterMap.Should().NotBeNull();
            glyphIndices.Should().NotBeNull();
        }
    }

    #endregion

    #region Different Font Sizes

    [Theory]
    [InlineData(8.0)]
    [InlineData(12.0)]
    [InlineData(24.0)]
    [InlineData(48.0)]
    [InlineData(72.0)]
    public unsafe void GetGlyphsAndPlacements_VariousFontSizes_ShouldSucceed(double fontSize)
    {
        var font = GetTestFont();
        string text = "Size Test";

        var spans = Itemize(text);
        Assert.SkipUnless(spans != null && spans.Count > 0, "Itemization failed");

        var analyzer = GetTextAnalyzer();
        ushort blankGlyphIndex = GetBlankGlyphIndex(font);

        fixed (char* pText = text)
        {
            analyzer.GetGlyphsAndTheirPlacements(
                pText,
                (uint)text.Length,
                font,
                blankGlyphIndex,
                false,
                false,
                CultureInfo.InvariantCulture,
                null,
                null,
                fontSize,
                1.0,
                1.0f,
                TextFormattingMode.Ideal,
                (ItemProps)spans[0].element,
                out ushort[] clusterMap,
                out ushort[] glyphIndices,
                out int[] glyphAdvances,
                out GlyphOffset[] glyphOffsets
            );

            clusterMap.Should().NotBeNull();
            glyphIndices.Should().NotBeNull();
            // Larger font sizes should produce larger advances
            glyphAdvances.Where(a => a > 0).Should().NotBeEmpty();
        }
    }

    #endregion

    #region Scaling Factor Coverage

    [Theory]
    [InlineData(0.5)]
    [InlineData(1.0)]
    [InlineData(2.0)]
    [InlineData(3.0)]
    public unsafe void GetGlyphsAndPlacements_VariousScalingFactors_ShouldSucceed(double scalingFactor)
    {
        var font = GetTestFont();
        string text = "Scale Test";

        var spans = Itemize(text);
        Assert.SkipUnless(spans != null && spans.Count > 0, "Itemization failed");

        var analyzer = GetTextAnalyzer();
        ushort blankGlyphIndex = GetBlankGlyphIndex(font);

        fixed (char* pText = text)
        {
            analyzer.GetGlyphsAndTheirPlacements(
                pText,
                (uint)text.Length,
                font,
                blankGlyphIndex,
                false,
                false,
                CultureInfo.InvariantCulture,
                null,
                null,
                12.0,
                scalingFactor,
                1.0f,
                TextFormattingMode.Ideal,
                (ItemProps)spans[0].element,
                out ushort[] clusterMap,
                out ushort[] glyphIndices,
                out int[] glyphAdvances,
                out GlyphOffset[] glyphOffsets
            );

            clusterMap.Should().NotBeNull();
            glyphIndices.Should().NotBeNull();
        }
    }

    #endregion

    #region PixelsPerDip Coverage

    [Theory]
    [InlineData(1.0f)]
    [InlineData(1.25f)]
    [InlineData(1.5f)]
    [InlineData(2.0f)]
    public unsafe void GetGlyphsAndPlacements_VariousPixelsPerDip_ShouldSucceed(float pixelsPerDip)
    {
        var font = GetTestFont();
        string text = "DPI Test";

        var spans = Itemize(text);
        Assert.SkipUnless(spans != null && spans.Count > 0, "Itemization failed");

        var analyzer = GetTextAnalyzer();
        ushort blankGlyphIndex = GetBlankGlyphIndex(font);

        fixed (char* pText = text)
        {
            analyzer.GetGlyphsAndTheirPlacements(
                pText,
                (uint)text.Length,
                font,
                blankGlyphIndex,
                false,
                false,
                CultureInfo.InvariantCulture,
                null,
                null,
                12.0,
                1.0,
                pixelsPerDip,
                TextFormattingMode.Ideal,
                (ItemProps)spans[0].element,
                out ushort[] clusterMap,
                out ushort[] glyphIndices,
                out int[] glyphAdvances,
                out GlyphOffset[] glyphOffsets
            );

            clusterMap.Should().NotBeNull();
            glyphIndices.Should().NotBeNull();
        }
    }

    #endregion

    #region Zero-Width Characters

    [Fact]
    public unsafe void GetGlyphsAndPlacements_ZeroWidthSpace_ShouldSucceed()
    {
        var font = GetTestFont();
        // Zero-width space (U+200B)
        string text = "Hello\u200BWorld";

        var spans = Itemize(text);
        Assert.SkipUnless(spans != null && spans.Count > 0, "Itemization failed");

        var analyzer = GetTextAnalyzer();
        ushort blankGlyphIndex = GetBlankGlyphIndex(font);

        fixed (char* pText = text)
        {
            analyzer.GetGlyphsAndTheirPlacements(
                pText,
                (uint)text.Length,
                font,
                blankGlyphIndex,
                false,
                false,
                CultureInfo.InvariantCulture,
                null,
                null,
                12.0,
                1.0,
                1.0f,
                TextFormattingMode.Ideal,
                (ItemProps)spans[0].element,
                out ushort[] clusterMap,
                out ushort[] glyphIndices,
                out int[] glyphAdvances,
                out GlyphOffset[] glyphOffsets
            );

            clusterMap.Should().NotBeNull();
            glyphIndices.Should().NotBeNull();
        }
    }

    [Fact]
    public unsafe void GetGlyphsAndPlacements_ZeroWidthNonJoiner_ShouldSucceed()
    {
        var font = GetTestFont();
        // Zero-width non-joiner (U+200C)
        string text = "Hello\u200CWorld";

        var spans = Itemize(text);
        Assert.SkipUnless(spans != null && spans.Count > 0, "Itemization failed");

        var analyzer = GetTextAnalyzer();
        ushort blankGlyphIndex = GetBlankGlyphIndex(font);

        fixed (char* pText = text)
        {
            analyzer.GetGlyphsAndTheirPlacements(
                pText,
                (uint)text.Length,
                font,
                blankGlyphIndex,
                false,
                false,
                CultureInfo.InvariantCulture,
                null,
                null,
                12.0,
                1.0,
                1.0f,
                TextFormattingMode.Ideal,
                (ItemProps)spans[0].element,
                out ushort[] clusterMap,
                out ushort[] glyphIndices,
                out int[] glyphAdvances,
                out GlyphOffset[] glyphOffsets
            );

            clusterMap.Should().NotBeNull();
            glyphIndices.Should().NotBeNull();
        }
    }

    [Fact]
    public unsafe void GetGlyphsAndPlacements_ZeroWidthJoiner_ShouldSucceed()
    {
        var font = GetTestFont();
        // Zero-width joiner (U+200D)
        string text = "Hello\u200DWorld";

        var spans = Itemize(text);
        Assert.SkipUnless(spans != null && spans.Count > 0, "Itemization failed");

        var analyzer = GetTextAnalyzer();
        ushort blankGlyphIndex = GetBlankGlyphIndex(font);

        fixed (char* pText = text)
        {
            analyzer.GetGlyphsAndTheirPlacements(
                pText,
                (uint)text.Length,
                font,
                blankGlyphIndex,
                false,
                false,
                CultureInfo.InvariantCulture,
                null,
                null,
                12.0,
                1.0,
                1.0f,
                TextFormattingMode.Ideal,
                (ItemProps)spans[0].element,
                out ushort[] clusterMap,
                out ushort[] glyphIndices,
                out int[] glyphAdvances,
                out GlyphOffset[] glyphOffsets
            );

            clusterMap.Should().NotBeNull();
            glyphIndices.Should().NotBeNull();
        }
    }

    #endregion

    #region Bidi Control Characters

    [Fact]
    public unsafe void GetGlyphsAndPlacements_LeftToRightMark_ShouldSucceed()
    {
        var font = GetTestFont();
        // Left-to-right mark (U+200E)
        string text = "Hello\u200EWorld";

        var spans = Itemize(text);
        Assert.SkipUnless(spans != null && spans.Count > 0, "Itemization failed");

        var analyzer = GetTextAnalyzer();
        ushort blankGlyphIndex = GetBlankGlyphIndex(font);

        fixed (char* pText = text)
        {
            analyzer.GetGlyphsAndTheirPlacements(
                pText,
                (uint)text.Length,
                font,
                blankGlyphIndex,
                false,
                false,
                CultureInfo.InvariantCulture,
                null,
                null,
                12.0,
                1.0,
                1.0f,
                TextFormattingMode.Ideal,
                (ItemProps)spans[0].element,
                out ushort[] clusterMap,
                out ushort[] glyphIndices,
                out int[] glyphAdvances,
                out GlyphOffset[] glyphOffsets
            );

            clusterMap.Should().NotBeNull();
            glyphIndices.Should().NotBeNull();
        }
    }

    [Fact]
    public unsafe void GetGlyphsAndPlacements_RightToLeftMark_ShouldSucceed()
    {
        var font = GetTestFont();
        // Right-to-left mark (U+200F)
        string text = "Hello\u200FWorld";

        var spans = Itemize(text);
        Assert.SkipUnless(spans != null && spans.Count > 0, "Itemization failed");

        var analyzer = GetTextAnalyzer();
        ushort blankGlyphIndex = GetBlankGlyphIndex(font);

        fixed (char* pText = text)
        {
            analyzer.GetGlyphsAndTheirPlacements(
                pText,
                (uint)text.Length,
                font,
                blankGlyphIndex,
                false,
                false,
                CultureInfo.InvariantCulture,
                null,
                null,
                12.0,
                1.0,
                1.0f,
                TextFormattingMode.Ideal,
                (ItemProps)spans[0].element,
                out ushort[] clusterMap,
                out ushort[] glyphIndices,
                out int[] glyphAdvances,
                out GlyphOffset[] glyphOffsets
            );

            clusterMap.Should().NotBeNull();
            glyphIndices.Should().NotBeNull();
        }
    }

    #endregion
}
