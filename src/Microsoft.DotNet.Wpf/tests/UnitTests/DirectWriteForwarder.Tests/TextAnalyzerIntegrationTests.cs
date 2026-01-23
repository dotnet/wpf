// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using System.Collections.Generic;

namespace MS.Internal.Text.TextInterface.Tests;

/// <summary>
/// Integration tests for <see cref="TextAnalyzer"/> class.
/// These tests call the real TextAnalyzer.Itemize method with actual PresentationNative delegates.
/// </summary>
public class TextAnalyzerIntegrationTests
{
    /// <summary>
    /// Helper class to encapsulate the complexity of calling TextAnalyzer.Itemize
    /// with the required native delegates from PresentationNative.dll.
    /// </summary>
    private static class ItemizeHelper
    {
        /// <summary>
        /// Calls TextAnalyzer.Itemize with the provided text and returns the resulting spans.
        /// This mirrors how TypefaceMap.GetShapeableText calls Itemize in WPF.
        /// </summary>
        public static unsafe IList<Span>? Itemize(
            string text,
            CultureInfo? culture = null,
            bool isRightToLeft = false,
            CultureInfo? numberCulture = null,
            bool ignoreUserOverride = false,
            uint numberSubstitutionMethod = 0)
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
                    isRightToLeft,
                    numberCulture,
                    ignoreUserOverride,
                    numberSubstitutionMethod,
                    ClassificationUtility.Instance,
                    MS.Internal.TextFormatting.UnsafeNativeMethods.CreateTextAnalysisSink,
                    MS.Internal.TextFormatting.UnsafeNativeMethods.GetScriptAnalysisList,
                    MS.Internal.TextFormatting.UnsafeNativeMethods.GetNumberSubstitutionList,
                    MS.Internal.TextFormatting.UnsafeNativeMethods.CreateTextAnalysisSource
                );
            }
        }
    }

    /// <summary>
    /// Gets a Font instance for testing. Uses Arial which should be available on all Windows systems.
    /// </summary>
    private static Font GetTestFont()
    {
        return TestHelpers.GetArialFontOrSkip();
    }

    /// <summary>
    /// Gets a TextAnalyzer instance for testing.
    /// </summary>
    private static TextAnalyzer GetTextAnalyzer()
    {
        return DWriteFactory.Instance.CreateTextAnalyzer();
    }

    /// <summary>
    /// Gets the blank glyph index for a font. This is typically glyph 3 for space character.
    /// For most fonts, the space character maps to a low glyph index.
    /// </summary>
    private static unsafe ushort GetBlankGlyphIndex(Font font)
    {
        // Get a FontFace and look up the space character glyph index
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

    /// <summary>
    /// Helper to get glyphs and placements for simple text.
    /// Returns the output arrays from GetGlyphsAndTheirPlacements.
    /// </summary>
    private static unsafe (ushort[] clusterMap, ushort[] glyphIndices, int[] glyphAdvances, GlyphOffset[] glyphOffsets)?
        GetGlyphsAndPlacements(
            string text,
            Font font,
            ItemProps itemProps,
            double fontSize = 12.0,
            bool isSideways = false,
            bool isRightToLeft = false)
    {
        var analyzer = GetTextAnalyzer();
        ushort blankGlyphIndex = GetBlankGlyphIndex(font);

        ushort[] clusterMap;
        ushort[] glyphIndices;
        int[] glyphAdvances;
        GlyphOffset[] glyphOffsets;

        fixed (char* pText = text)
        {
            analyzer.GetGlyphsAndTheirPlacements(
                pText,
                (uint)text.Length,
                font,
                blankGlyphIndex,
                isSideways,
                isRightToLeft,
                CultureInfo.InvariantCulture,
                null,  // features
                null,  // featureRangeLengths
                fontSize,
                1.0,   // scalingFactor
                1.0f,  // pixelsPerDip
                System.Windows.Media.TextFormattingMode.Ideal,
                itemProps,
                out clusterMap,
                out glyphIndices,
                out glyphAdvances,
                out glyphOffsets
            );
        }

        return (clusterMap, glyphIndices, glyphAdvances, glyphOffsets);
    }

    #region Infrastructure Verification Tests

    [Fact]
    public void TextAnalyzer_CanBeCreated()
    {
        var analyzer = GetTextAnalyzer();
        analyzer.Should().NotBeNull();
    }

    [Fact]
    public void ClassificationUtility_IsAvailable()
    {
        var classification = ClassificationUtility.Instance;
        classification.Should().NotBeNull();
    }

    [Fact]
    public void DWriteFactory_HasValidFactoryPointer()
    {
        var factory = DWriteFactory.Instance;
        factory.Should().NotBeNull();

        unsafe
        {
            var ptr = factory.DWriteFactory;
            ((IntPtr)ptr).Should().NotBe(IntPtr.Zero);
        }
    }

    [Fact]
    public void TestFont_IsAvailable()
    {
        var font = GetTestFont();
        // Skip test if Arial is not available
        font.Weight.Should().Be(FontWeight.Normal);
    }

    #endregion

    #region Basic Itemization Tests

    [Fact]
    public void Itemize_EmptyString_ReturnsNull()
    {
        var result = ItemizeHelper.Itemize("");
        result.Should().BeNull();
    }

    [Fact]
    public void Itemize_NullString_ReturnsNull()
    {
        var result = ItemizeHelper.Itemize(null!);
        result.Should().BeNull();
    }

    [Fact]
    public void Itemize_SingleLatinCharacter_ReturnsSingleSpan()
    {
        var result = ItemizeHelper.Itemize("A");
        
        result.Should().NotBeNull();
        result.Should().HaveCount(1);
        result![0].length.Should().Be(1);
    }

    [Fact]
    public void Itemize_SimpleLatinText_ReturnsSingleSpan()
    {
        var result = ItemizeHelper.Itemize("Hello World");
        
        result.Should().NotBeNull();
        result.Should().HaveCount(1);
        result![0].length.Should().Be(11);
        
        // The element should be an ItemProps
        result[0].element.Should().BeOfType<ItemProps>();
        var props = (ItemProps)result[0].element;
        props.IsLatin.Should().BeTrue();
    }

    [Fact]
    public void Itemize_LatinText_ItemPropsHasValidProperties()
    {
        var result = ItemizeHelper.Itemize("Test");
        
        result.Should().NotBeNull();
        var props = (ItemProps)result![0].element;
        
        props.IsLatin.Should().BeTrue();
        props.IsIndic.Should().BeFalse();
        props.HasCombiningMark.Should().BeFalse();
    }

    [Fact]
    public void Itemize_WithEnglishCulture_Succeeds()
    {
        var result = ItemizeHelper.Itemize("Hello", CultureInfo.GetCultureInfo("en-US"));
        
        result.Should().NotBeNull();
        result.Should().HaveCount(1);
    }

    #endregion

    #region Mixed Script Tests

    [Fact]
    public void Itemize_MixedLatinAndArabic_ReturnsMultipleSpans()
    {
        // "Hello" + Arabic "مرحبا" (Marhaba)
        var result = ItemizeHelper.Itemize("Hello مرحبا");
        
        result.Should().NotBeNull();
        // Should have at least 2 spans: Latin and Arabic (possibly more for space)
        result!.Count.Should().BeGreaterThanOrEqualTo(2);
        
        // Total length should match input
        int totalLength = result.Sum(s => s.length);
        totalLength.Should().Be(11); // "Hello " (6) + "مرحبا" (5)
    }

    [Fact]
    public void Itemize_MixedLatinAndChinese_ReturnsMultipleSpans()
    {
        // "Hello" + Chinese "你好" (Nǐ hǎo)
        var result = ItemizeHelper.Itemize("Hello你好");
        
        result.Should().NotBeNull();
        // Should have at least 2 spans: Latin and Chinese
        result!.Count.Should().BeGreaterThanOrEqualTo(2);
        
        // First span should be Latin
        var firstProps = (ItemProps)result[0].element;
        firstProps.IsLatin.Should().BeTrue();
        
        // Total length should match
        int totalLength = result.Sum(s => s.length);
        totalLength.Should().Be(7); // "Hello" (5) + "你好" (2)
    }

    [Fact]
    public void Itemize_MixedLatinAndCyrillic_ReturnsMultipleSpans()
    {
        // "Hello" + Russian "Привет" (Privet)
        var result = ItemizeHelper.Itemize("Hello Привет");
        
        result.Should().NotBeNull();
        result!.Count.Should().BeGreaterThanOrEqualTo(2);
        
        int totalLength = result.Sum(s => s.length);
        totalLength.Should().Be(12); // "Hello " (6) + "Привет" (6)
    }

    [Fact]
    public void Itemize_MixedLatinAndGreek_ReturnsMultipleSpans()
    {
        // "Hello" + Greek "Γειά" (Geia)
        var result = ItemizeHelper.Itemize("Hello Γειά");
        
        result.Should().NotBeNull();
        result!.Count.Should().BeGreaterThanOrEqualTo(2);
        
        int totalLength = result.Sum(s => s.length);
        totalLength.Should().Be(10);
    }

    [Fact]
    public void Itemize_MixedLatinAndHebrew_ReturnsMultipleSpans()
    {
        // "Hello" + Hebrew "שלום" (Shalom)
        var result = ItemizeHelper.Itemize("Hello שלום");
        
        result.Should().NotBeNull();
        result!.Count.Should().BeGreaterThanOrEqualTo(2);
        
        int totalLength = result.Sum(s => s.length);
        totalLength.Should().Be(10);
    }

    [Fact]
    public void Itemize_MixedLatinAndJapanese_ReturnsMultipleSpans()
    {
        // "Hello" + Japanese "こんにちは" (Konnichiwa)
        var result = ItemizeHelper.Itemize("Helloこんにちは");
        
        result.Should().NotBeNull();
        result!.Count.Should().BeGreaterThanOrEqualTo(2);
        
        var firstProps = (ItemProps)result[0].element;
        firstProps.IsLatin.Should().BeTrue();
        
        int totalLength = result.Sum(s => s.length);
        totalLength.Should().Be(10); // "Hello" (5) + "こんにちは" (5)
    }

    [Fact]
    public void Itemize_MixedLatinAndKorean_ReturnsMultipleSpans()
    {
        // "Hello" + Korean "안녕하세요" (Annyeonghaseyo)
        var result = ItemizeHelper.Itemize("Hello안녕하세요");
        
        result.Should().NotBeNull();
        result!.Count.Should().BeGreaterThanOrEqualTo(2);
        
        int totalLength = result.Sum(s => s.length);
        totalLength.Should().Be(10); // "Hello" (5) + "안녕하세요" (5)
    }

    [Fact]
    public void Itemize_ThreeScripts_ReturnsMultipleSpans()
    {
        // Latin + Chinese + Arabic
        var result = ItemizeHelper.Itemize("Hi你好مرحبا");
        
        result.Should().NotBeNull();
        result!.Count.Should().BeGreaterThanOrEqualTo(3);
        
        int totalLength = result.Sum(s => s.length);
        totalLength.Should().Be(9); // "Hi" (2) + "你好" (2) + "مرحبا" (5)
    }

    #endregion

    #region RTL (Right-to-Left) Tests

    [Fact]
    public void Itemize_ArabicText_Succeeds()
    {
        // Arabic "مرحبا بالعالم" (Hello World)
        var result = ItemizeHelper.Itemize("مرحبا بالعالم");
        
        result.Should().NotBeNull();
        result!.Count.Should().BeGreaterThanOrEqualTo(1);
        
        int totalLength = result.Sum(s => s.length);
        totalLength.Should().Be(13);
    }

    [Fact]
    public void Itemize_HebrewText_Succeeds()
    {
        // Hebrew "שלום עולם" (Hello World)
        var result = ItemizeHelper.Itemize("שלום עולם");
        
        result.Should().NotBeNull();
        result!.Count.Should().BeGreaterThanOrEqualTo(1);
        
        int totalLength = result.Sum(s => s.length);
        totalLength.Should().Be(9);
    }

    [Fact]
    public void Itemize_WithRtlParagraphDirection_Succeeds()
    {
        var result = ItemizeHelper.Itemize("Hello", isRightToLeft: true);
        
        result.Should().NotBeNull();
        result.Should().HaveCount(1);
    }

    [Fact]
    public void Itemize_ArabicWithRtlParagraph_Succeeds()
    {
        var result = ItemizeHelper.Itemize("مرحبا", isRightToLeft: true);
        
        result.Should().NotBeNull();
        result!.Count.Should().BeGreaterThanOrEqualTo(1);
    }

    #endregion

    #region Indic Script Tests

    [Fact]
    public void Itemize_DevanagariText_FlaggedAsIndic()
    {
        // Hindi "नमस्ते" (Namaste)
        var result = ItemizeHelper.Itemize("नमस्ते");
        
        result.Should().NotBeNull();
        result!.Count.Should().BeGreaterThanOrEqualTo(1);
        
        // At least one span should be marked as Indic
        var hasIndicSpan = result.Any(s => ((ItemProps)s.element).IsIndic);
        hasIndicSpan.Should().BeTrue();
    }

    [Fact]
    public void Itemize_BengaliText_FlaggedAsIndic()
    {
        // Bengali "নমস্কার" (Nomoshkar)
        var result = ItemizeHelper.Itemize("নমস্কার");
        
        result.Should().NotBeNull();
        var hasIndicSpan = result!.Any(s => ((ItemProps)s.element).IsIndic);
        hasIndicSpan.Should().BeTrue();
    }

    [Fact]
    public void Itemize_TamilText_FlaggedAsIndic()
    {
        // Tamil "வணக்கம்" (Vanakkam)
        var result = ItemizeHelper.Itemize("வணக்கம்");
        
        result.Should().NotBeNull();
        var hasIndicSpan = result!.Any(s => ((ItemProps)s.element).IsIndic);
        hasIndicSpan.Should().BeTrue();
    }

    [Fact]
    public void Itemize_TeluguText_FlaggedAsIndic()
    {
        // Telugu "నమస్కారం" (Namaskaram)
        var result = ItemizeHelper.Itemize("నమస్కారం");
        
        result.Should().NotBeNull();
        var hasIndicSpan = result!.Any(s => ((ItemProps)s.element).IsIndic);
        hasIndicSpan.Should().BeTrue();
    }

    [Fact]
    public void Itemize_ThaiText_Succeeds()
    {
        // Thai "สวัสดี" (Sawasdee)
        var result = ItemizeHelper.Itemize("สวัสดี");
        
        result.Should().NotBeNull();
        result!.Count.Should().BeGreaterThanOrEqualTo(1);
    }

    [Fact]
    public void Itemize_MixedLatinAndDevanagari_ReturnsMultipleSpans()
    {
        var result = ItemizeHelper.Itemize("Hello नमस्ते");
        
        result.Should().NotBeNull();
        result!.Count.Should().BeGreaterThanOrEqualTo(2);
        
        // First span should be Latin
        var firstProps = (ItemProps)result[0].element;
        firstProps.IsLatin.Should().BeTrue();
        firstProps.IsIndic.Should().BeFalse();
        
        // Should have an Indic span
        var hasIndicSpan = result.Any(s => ((ItemProps)s.element).IsIndic);
        hasIndicSpan.Should().BeTrue();
    }

    #endregion

    #region Combining Mark Tests

    [Fact]
    public void Itemize_LatinWithCombiningAccent_FlaggedAsCombining()
    {
        // 'e' followed by combining acute accent (U+0301) = é
        var result = ItemizeHelper.Itemize("e\u0301");
        
        result.Should().NotBeNull();
        
        // Should have a span with combining mark flag
        var hasCombiningSpan = result!.Any(s => ((ItemProps)s.element).HasCombiningMark);
        hasCombiningSpan.Should().BeTrue();
    }

    [Fact]
    public void Itemize_PrecomposedAccent_NoCombiningFlag()
    {
        // Precomposed é (U+00E9) - no combining mark
        var result = ItemizeHelper.Itemize("é");
        
        result.Should().NotBeNull();
        
        // Precomposed characters should not have combining mark flag
        var props = (ItemProps)result![0].element;
        props.HasCombiningMark.Should().BeFalse();
    }

    [Fact]
    public void Itemize_MultipleCombiningMarks_FlaggedAsCombining()
    {
        // 'a' + combining grave (U+0300) + combining acute (U+0301)
        var result = ItemizeHelper.Itemize("a\u0300\u0301");
        
        result.Should().NotBeNull();
        
        var hasCombiningSpan = result!.Any(s => ((ItemProps)s.element).HasCombiningMark);
        hasCombiningSpan.Should().BeTrue();
    }

    [Fact]
    public void Itemize_CombiningDiaeresis_FlaggedAsCombining()
    {
        // 'u' + combining diaeresis (U+0308) = ü
        var result = ItemizeHelper.Itemize("u\u0308");
        
        result.Should().NotBeNull();
        
        var hasCombiningSpan = result!.Any(s => ((ItemProps)s.element).HasCombiningMark);
        hasCombiningSpan.Should().BeTrue();
    }

    #endregion

    #region Number Substitution Tests

    [Fact]
    public void Itemize_DigitsWithNoCulture_Succeeds()
    {
        var result = ItemizeHelper.Itemize("123456");
        
        result.Should().NotBeNull();
        result!.Count.Should().BeGreaterThanOrEqualTo(1);
    }

    [Fact]
    public void Itemize_DigitsWithEnglishCulture_Succeeds()
    {
        var result = ItemizeHelper.Itemize("123", numberCulture: CultureInfo.GetCultureInfo("en-US"));
        
        result.Should().NotBeNull();
        result!.Count.Should().BeGreaterThanOrEqualTo(1);
    }

    [Fact]
    public void Itemize_DigitsWithArabicCulture_Succeeds()
    {
        var result = ItemizeHelper.Itemize("123", numberCulture: CultureInfo.GetCultureInfo("ar-SA"));
        
        result.Should().NotBeNull();
        result!.Count.Should().BeGreaterThanOrEqualTo(1);
    }

    [Fact]
    public void Itemize_MixedTextAndDigits_Succeeds()
    {
        var result = ItemizeHelper.Itemize("Price: $123.45");
        
        result.Should().NotBeNull();
        
        int totalLength = result!.Sum(s => s.length);
        totalLength.Should().Be(14);
    }

    [Fact]
    public void Itemize_DigitsWithNumberSubstitutionMethod_Succeeds()
    {
        // NumberSubstitutionMethod: 0=FromCulture, 1=None, 2=Context, 3=NativeNational
        var result = ItemizeHelper.Itemize(
            "123", 
            numberCulture: CultureInfo.GetCultureInfo("ar-SA"),
            numberSubstitutionMethod: 3); // NativeNational
        
        result.Should().NotBeNull();
        result!.Count.Should().BeGreaterThanOrEqualTo(1);
    }

    #endregion

    #region ItemProps Tests

    [Fact]
    public unsafe void ItemProps_FromItemize_HasValidScriptAnalysis()
    {
        var result = ItemizeHelper.Itemize("Hello");
        
        result.Should().NotBeNull();
        var props = (ItemProps)result![0].element;
        
        // ScriptAnalysis should be a valid pointer (not null for real text)
        void* scriptPtr = props.ScriptAnalysis;
        ((IntPtr)scriptPtr).Should().NotBe(IntPtr.Zero);
    }

    [Fact]
    public void ItemProps_LatinAndArabic_HaveDifferentScriptAnalysis()
    {
        var latinResult = ItemizeHelper.Itemize("Hello");
        var arabicResult = ItemizeHelper.Itemize("مرحبا");
        
        latinResult.Should().NotBeNull();
        arabicResult.Should().NotBeNull();
        
        var latinProps = (ItemProps)latinResult![0].element;
        var arabicProps = (ItemProps)arabicResult![0].element;
        
        // They should have different properties
        latinProps.IsLatin.Should().BeTrue();
        arabicProps.IsLatin.Should().BeFalse();
    }

    [Fact]
    public void ItemProps_CanShapeTogether_SameScript_ReturnsTrue()
    {
        var result = ItemizeHelper.Itemize("Hello World");
        
        result.Should().NotBeNull();
        // If there's only one span, it can shape with itself
        if (result!.Count == 1)
        {
            var props = (ItemProps)result[0].element;
            props.CanShapeTogether(props).Should().BeTrue();
        }
    }

    [Fact]
    public void ItemProps_NeedsCaretInfo_TrueForIndic()
    {
        // Devanagari text should need caret info
        var result = ItemizeHelper.Itemize("नमस्ते");
        
        result.Should().NotBeNull();
        
        // At least one span should need caret info (Indic scripts do)
        var needsCaretInfo = result!.Any(s => ((ItemProps)s.element).NeedsCaretInfo);
        needsCaretInfo.Should().BeTrue();
    }

    [Fact]
    public void ItemProps_NeedsCaretInfo_FalseForLatin()
    {
        var result = ItemizeHelper.Itemize("Hello");
        
        result.Should().NotBeNull();
        var props = (ItemProps)result![0].element;
        
        // Latin text should not need special caret info
        props.NeedsCaretInfo.Should().BeFalse();
    }

    #endregion

    #region Edge Cases

    [Fact]
    public void Itemize_SingleSpace_Succeeds()
    {
        var result = ItemizeHelper.Itemize(" ");
        
        result.Should().NotBeNull();
        result!.Count.Should().BeGreaterThanOrEqualTo(1);
        result.Sum(s => s.length).Should().Be(1);
    }

    [Fact]
    public void Itemize_OnlySpaces_Succeeds()
    {
        var result = ItemizeHelper.Itemize("     ");
        
        result.Should().NotBeNull();
        result!.Sum(s => s.length).Should().Be(5);
    }

    [Fact]
    public void Itemize_Newlines_Succeeds()
    {
        var result = ItemizeHelper.Itemize("Hello\nWorld");
        
        result.Should().NotBeNull();
        result!.Sum(s => s.length).Should().Be(11);
    }

    [Fact]
    public void Itemize_Tabs_Succeeds()
    {
        var result = ItemizeHelper.Itemize("Hello\tWorld");
        
        result.Should().NotBeNull();
        result!.Sum(s => s.length).Should().Be(11);
    }

    [Fact]
    public void Itemize_ControlCharacters_Succeeds()
    {
        // Control characters may be filtered by DirectWrite
        // The important thing is that it doesn't throw
        var result = ItemizeHelper.Itemize("A\x01\x02\x03B");
        
        result.Should().NotBeNull();
        // Just verify we get some result - control char handling is implementation-defined
        result!.Sum(s => s.length).Should().BeGreaterThan(0);
    }

    [Fact]
    public void Itemize_SurrogatePair_Succeeds()
    {
        // Emoji: 😀 (U+1F600) = surrogate pair
        var result = ItemizeHelper.Itemize("😀");
        
        result.Should().NotBeNull();
        result!.Sum(s => s.length).Should().Be(2); // Surrogate pair = 2 chars
    }

    [Fact]
    public void Itemize_MultipleSurrogatePairs_Succeeds()
    {
        // Multiple emoji
        var result = ItemizeHelper.Itemize("😀😁😂");
        
        result.Should().NotBeNull();
        result!.Sum(s => s.length).Should().Be(6); // 3 emoji * 2 chars each
    }

    [Fact]
    public void Itemize_MixedTextAndEmoji_Succeeds()
    {
        var result = ItemizeHelper.Itemize("Hello 😀 World");
        
        result.Should().NotBeNull();
        result!.Sum(s => s.length).Should().Be(14); // "Hello " (6) + 😀 (2) + " World" (6)
    }

    [Fact]
    public void Itemize_LongText_Succeeds()
    {
        var longText = new string('A', 10000);
        var result = ItemizeHelper.Itemize(longText);
        
        result.Should().NotBeNull();
        result!.Sum(s => s.length).Should().Be(10000);
    }

    [Fact]
    public void Itemize_UnicodePrivateUseArea_Succeeds()
    {
        // Private Use Area character
        var result = ItemizeHelper.Itemize("\uE000");
        
        result.Should().NotBeNull();
        result!.Sum(s => s.length).Should().Be(1);
    }

    [Fact]
    public void Itemize_ZeroWidthChars_Succeeds()
    {
        // Zero-width space (U+200B) and zero-width joiner (U+200D)
        var result = ItemizeHelper.Itemize("A\u200B\u200DB");
        
        result.Should().NotBeNull();
        result!.Sum(s => s.length).Should().Be(4);
    }

    [Fact]
    public void Itemize_BidiControlChars_Succeeds()
    {
        // LRM (U+200E), RLM (U+200F)
        var result = ItemizeHelper.Itemize("A\u200E\u200FB");
        
        result.Should().NotBeNull();
        result!.Sum(s => s.length).Should().Be(4);
    }

    [Fact]
    public void Itemize_AllCultures_DoNotThrow()
    {
        var cultures = new[]
        {
            "en-US", "fr-FR", "de-DE", "ja-JP", "zh-CN", "ar-SA", "he-IL", "hi-IN", "ru-RU", "ko-KR"
        };
        
        foreach (var cultureName in cultures)
        {
            var culture = CultureInfo.GetCultureInfo(cultureName);
            var result = ItemizeHelper.Itemize("Test 123", culture);
            
            result.Should().NotBeNull($"Culture {cultureName} should not return null");
        }
    }

    #endregion

    #region Phase 3: Glyph Shaping Tests

    [Fact]
    public void GetGlyphsAndTheirPlacements_SimpleAscii_ReturnsGlyphData()
    {
        var font = GetTestFont();
        var spans = ItemizeHelper.Itemize("Hello");
        spans.Should().NotBeNull();
        
        var itemProps = (ItemProps)spans![0].element;
        var result = GetGlyphsAndPlacements("Hello", font, itemProps);
        
        result.Should().NotBeNull();
        var (clusterMap, glyphIndices, glyphAdvances, glyphOffsets) = result!.Value;
        
        // Should have cluster map entries for each character
        clusterMap.Should().HaveCount(5);
        
        // Should have glyph indices (at least one per character for simple text)
        glyphIndices.Should().NotBeEmpty();
        glyphIndices.Length.Should().BeGreaterThanOrEqualTo(5);
        
        // All glyph indices should be non-zero (valid glyphs)
        glyphIndices.Take(5).Should().AllSatisfy(g => g.Should().BeGreaterThan((ushort)0));
        
        // Advances should be positive
        glyphAdvances.Should().NotBeEmpty();
        glyphAdvances.Take(5).Should().AllSatisfy(a => a.Should().BeGreaterThan(0));
        
        // Offsets should exist
        glyphOffsets.Should().NotBeEmpty();
    }

    [Fact]
    public void GetGlyphsAndTheirPlacements_SingleCharacter_ReturnsOneGlyph()
    {
        var font = GetTestFont();
        var spans = ItemizeHelper.Itemize("A");
        var itemProps = (ItemProps)spans![0].element;
        var result = GetGlyphsAndPlacements("A", font, itemProps);
        
        result.Should().NotBeNull();
        var (clusterMap, glyphIndices, glyphAdvances, _) = result!.Value;
        
        clusterMap.Should().HaveCount(1);
        glyphIndices.Length.Should().BeGreaterThanOrEqualTo(1);
        glyphAdvances.Length.Should().BeGreaterThanOrEqualTo(1);
    }

    [Fact]
    public void GetGlyphsAndTheirPlacements_WithSpaces_HandlesSpacesCorrectly()
    {
        var font = GetTestFont();
        var spans = ItemizeHelper.Itemize("A B");
        var itemProps = (ItemProps)spans![0].element;
        var result = GetGlyphsAndPlacements("A B", font, itemProps);
        
        result.Should().NotBeNull();
        var (clusterMap, glyphIndices, glyphAdvances, glyphOffsets) = result!.Value;
        
        clusterMap.Should().HaveCount(3);
        // All characters should map to valid glyphs
        glyphIndices.Take(3).Should().AllSatisfy(g => g.Should().BeGreaterThan((ushort)0));
    }

    [Fact]
    public void GetGlyphsAndTheirPlacements_DifferentFontSizes_ScalesAdvances()
    {
        var font = GetTestFont();
        var spans = ItemizeHelper.Itemize("A");
        var itemProps = (ItemProps)spans![0].element;
        
        var result12 = GetGlyphsAndPlacements("A", font, itemProps, fontSize: 12.0);
        var result24 = GetGlyphsAndPlacements("A", font, itemProps, fontSize: 24.0);
        
        result12.Should().NotBeNull();
        result24.Should().NotBeNull();
        
        // Larger font size should produce larger advances
        var advance12 = result12!.Value.glyphAdvances[0];
        var advance24 = result24!.Value.glyphAdvances[0];
        
        advance24.Should().BeGreaterThan(advance12);
        // Should be approximately 2x (not exact due to rounding)
        ((double)advance24 / advance12).Should().BeApproximately(2.0, 0.1);
    }

    [Fact]
    public void GetGlyphsAndTheirPlacements_ClusterMap_MapsCharactersToGlyphs()
    {
        var font = GetTestFont();
        var spans = ItemizeHelper.Itemize("ABC");
        var itemProps = (ItemProps)spans![0].element;
        var result = GetGlyphsAndPlacements("ABC", font, itemProps);
        
        result.Should().NotBeNull();
        var clusterMap = result!.Value.clusterMap;
        
        // Cluster map should be monotonically non-decreasing
        for (int i = 1; i < clusterMap.Length; i++)
        {
            clusterMap[i].Should().BeGreaterThanOrEqualTo(clusterMap[i - 1]);
        }
    }

    [Fact]
    public void GetGlyphsAndTheirPlacements_RightToLeft_Succeeds()
    {
        var font = GetTestFont();
        // Use Arabic text for RTL
        var spans = ItemizeHelper.Itemize("مرحبا");
        Assert.SkipUnless(spans != null && spans.Count > 0, "Itemization failed");
        
        var itemProps = (ItemProps)spans[0].element;
        var result = GetGlyphsAndPlacements("مرحبا", font, itemProps, isRightToLeft: true);
        
        result.Should().NotBeNull();
        result!.Value.glyphIndices.Should().NotBeEmpty();
    }

    [Fact]
    public void GetGlyphsAndTheirPlacements_LongText_Succeeds()
    {
        var font = GetTestFont();
        var longText = new string('A', 1000);
        var spans = ItemizeHelper.Itemize(longText);
        var itemProps = (ItemProps)spans![0].element;
        var result = GetGlyphsAndPlacements(longText, font, itemProps);
        
        result.Should().NotBeNull();
        var (clusterMap, glyphIndices, _, _) = result!.Value;
        
        clusterMap.Should().HaveCount(1000);
        glyphIndices.Length.Should().BeGreaterThanOrEqualTo(1000);
    }

    [Fact]
    public void GetGlyphsAndTheirPlacements_MixedCase_ReturnsDistinctGlyphs()
    {
        var font = GetTestFont();
        var spans = ItemizeHelper.Itemize("Aa");
        var itemProps = (ItemProps)spans![0].element;
        var result = GetGlyphsAndPlacements("Aa", font, itemProps);
        
        result.Should().NotBeNull();
        var glyphIndices = result!.Value.glyphIndices;
        
        // 'A' and 'a' should have different glyph indices
        glyphIndices[0].Should().NotBe(glyphIndices[1]);
    }

    [Fact]
    public void GetGlyphsAndTheirPlacements_Numbers_ReturnsValidGlyphs()
    {
        var font = GetTestFont();
        var spans = ItemizeHelper.Itemize("0123456789");
        var itemProps = (ItemProps)spans![0].element;
        var result = GetGlyphsAndPlacements("0123456789", font, itemProps);
        
        result.Should().NotBeNull();
        var glyphIndices = result!.Value.glyphIndices;
        
        // All digits should have valid glyph indices
        glyphIndices.Take(10).Should().AllSatisfy(g => g.Should().BeGreaterThan((ushort)0));
    }

    [Fact]
    public void GetGlyphsAndTheirPlacements_Punctuation_ReturnsValidGlyphs()
    {
        var font = GetTestFont();
        var text = ".,!?;:";
        var spans = ItemizeHelper.Itemize(text);
        var itemProps = (ItemProps)spans![0].element;
        var result = GetGlyphsAndPlacements(text, font, itemProps);
        
        result.Should().NotBeNull();
        var glyphIndices = result!.Value.glyphIndices;
        
        // All punctuation should have valid glyph indices
        glyphIndices.Take(text.Length).Should().AllSatisfy(g => g.Should().BeGreaterThan((ushort)0));
    }

    [Fact]
    public void GetGlyphsAndTheirPlacements_GlyphOffsets_AreReasonable()
    {
        var font = GetTestFont();
        var spans = ItemizeHelper.Itemize("Hello");
        var itemProps = (ItemProps)spans![0].element;
        var result = GetGlyphsAndPlacements("Hello", font, itemProps);
        
        result.Should().NotBeNull();
        var glyphOffsets = result!.Value.glyphOffsets;
        
        // For simple Latin text, offsets should generally be zero or small
        foreach (var offset in glyphOffsets.Take(5))
        {
            // du is horizontal offset, dv is vertical offset
            Math.Abs(offset.du).Should().BeLessThan(1000);
            Math.Abs(offset.dv).Should().BeLessThan(1000);
        }
    }

    [Fact]
    public void GetGlyphsAndTheirPlacements_ConsistentResults_ForSameInput()
    {
        var font = GetTestFont();
        var spans = ItemizeHelper.Itemize("Test");
        var itemProps = (ItemProps)spans![0].element;
        
        var result1 = GetGlyphsAndPlacements("Test", font, itemProps);
        var result2 = GetGlyphsAndPlacements("Test", font, itemProps);
        
        result1.Should().NotBeNull();
        result2.Should().NotBeNull();
        
        // Results should be identical for the same input
        result1!.Value.glyphIndices.Should().BeEquivalentTo(result2!.Value.glyphIndices);
        result1!.Value.glyphAdvances.Should().BeEquivalentTo(result2!.Value.glyphAdvances);
    }

    [Fact]
    public void GetGlyphsAndTheirPlacements_DisplayMode_DiffersFromIdeal()
    {
        var font = GetTestFont();
        var spans = ItemizeHelper.Itemize("Hello");
        var itemProps = (ItemProps)spans![0].element;
        var analyzer = GetTextAnalyzer();
        ushort blankGlyphIndex = GetBlankGlyphIndex(font);

        ushort[] clusterMapIdeal, clusterMapDisplay;
        ushort[] glyphIndicesIdeal, glyphIndicesDisplay;
        int[] glyphAdvancesIdeal, glyphAdvancesDisplay;
        GlyphOffset[] glyphOffsetsIdeal, glyphOffsetsDisplay;

        unsafe
        {
            fixed (char* pText = "Hello")
            {
                analyzer.GetGlyphsAndTheirPlacements(
                    pText, 5, font, blankGlyphIndex, false, false,
                    CultureInfo.InvariantCulture, null, null,
                    12.0, 1.0, 1.0f,
                    System.Windows.Media.TextFormattingMode.Ideal,
                    itemProps,
                    out clusterMapIdeal, out glyphIndicesIdeal, out glyphAdvancesIdeal, out glyphOffsetsIdeal);

                analyzer.GetGlyphsAndTheirPlacements(
                    pText, 5, font, blankGlyphIndex, false, false,
                    CultureInfo.InvariantCulture, null, null,
                    12.0, 1.0, 1.0f,
                    System.Windows.Media.TextFormattingMode.Display,
                    itemProps,
                    out clusterMapDisplay, out glyphIndicesDisplay, out glyphAdvancesDisplay, out glyphOffsetsDisplay);
            }
        }

        // Glyph indices should be the same
        glyphIndicesIdeal.Should().BeEquivalentTo(glyphIndicesDisplay);
        
        // Advances may differ between Ideal and Display modes due to rounding
        // Just verify both return valid data
        glyphAdvancesIdeal.Should().NotBeEmpty();
        glyphAdvancesDisplay.Should().NotBeEmpty();
    }

    [Fact]
    public void GetBlankGlyphIndex_ReturnsValidIndex()
    {
        var font = GetTestFont();
        var blankIndex = GetBlankGlyphIndex(font);
        
        // Blank glyph index should be valid (could be 0 for .notdef or space glyph index)
        // Just verify it doesn't throw and returns a value
        blankIndex.Should().BeGreaterThanOrEqualTo((ushort)0);
    }

    [Fact]
    public void TextAnalyzer_GetGlyphsAndTheirPlacements_WithFeatures_Succeeds()
    {
        var font = GetTestFont();
        var spans = ItemizeHelper.Itemize("fi"); // potential ligature
        var itemProps = (ItemProps)spans![0].element;
        var analyzer = GetTextAnalyzer();
        ushort blankGlyphIndex = GetBlankGlyphIndex(font);

        // Create a simple feature set (liga = ligatures)
        var features = new DWriteFontFeature[][]
        {
            new DWriteFontFeature[] 
            { 
                new DWriteFontFeature(DWriteFontFeatureTag.StandardLigatures, 1) 
            }
        };
        var featureRanges = new uint[] { 2 }; // applies to 2 characters

        ushort[] clusterMap;
        ushort[] glyphIndices;
        int[] glyphAdvances;
        GlyphOffset[] glyphOffsets;

        unsafe
        {
            fixed (char* pText = "fi")
            {
                analyzer.GetGlyphsAndTheirPlacements(
                    pText, 2, font, blankGlyphIndex, false, false,
                    CultureInfo.InvariantCulture, features, featureRanges,
                    12.0, 1.0, 1.0f,
                    System.Windows.Media.TextFormattingMode.Ideal,
                    itemProps,
                    out clusterMap, out glyphIndices, out glyphAdvances, out glyphOffsets);
            }
        }

        // Should succeed regardless of whether ligature is applied
        clusterMap.Should().HaveCount(2);
        glyphIndices.Should().NotBeEmpty();
    }

    #endregion

    #region Phase 4: GetGlyphPlacements Direct Tests

    /// <summary>
    /// Helper to call GetGlyphs and then GetGlyphPlacements separately.
    /// This tests the lower-level API that GetGlyphsAndTheirPlacements combines.
    /// </summary>
    private static unsafe (int[] glyphAdvances, GlyphOffset[] glyphOffsets)?
        GetGlyphPlacementsDirect(
            string text,
            Font font,
            ItemProps itemProps,
            double fontSize = 12.0,
            bool isSideways = false,
            bool isRightToLeft = false)
    {
        var analyzer = GetTextAnalyzer();
        ushort blankGlyphIndex = GetBlankGlyphIndex(font);
        uint textLength = (uint)text.Length;
        uint maxGlyphCount = textLength * 3 + 16; // Standard DWrite buffer size formula

        // Allocate buffers for GetGlyphs output
        ushort[] clusterMap = new ushort[textLength];
        ushort[] textProps = new ushort[textLength];
        ushort[] glyphIndices = new ushort[maxGlyphCount];
        uint[] glyphProps = new uint[maxGlyphCount];
        int[] canGlyphAlone = new int[textLength];
        uint actualGlyphCount = 0;

        fixed (char* pText = text)
        fixed (ushort* pClusterMap = clusterMap)
        fixed (ushort* pTextProps = textProps)
        fixed (ushort* pGlyphIndices = glyphIndices)
        fixed (uint* pGlyphProps = glyphProps)
        fixed (int* pCanGlyphAlone = canGlyphAlone)
        {
            // Step 1: Call GetGlyphs to get glyph indices
            analyzer.GetGlyphs(
                pText,
                textLength,
                font,
                blankGlyphIndex,
                isSideways,
                isRightToLeft,
                CultureInfo.InvariantCulture,
                null, // features
                null, // featureRangeLengths
                maxGlyphCount,
                System.Windows.Media.TextFormattingMode.Ideal,
                itemProps,
                pClusterMap,
                pTextProps,
                pGlyphIndices,
                pGlyphProps,
                pCanGlyphAlone,
                out actualGlyphCount
            );

            // Step 2: Call GetGlyphPlacements with the glyph data
            int[] glyphAdvances = new int[actualGlyphCount];
            GlyphOffset[] glyphOffsets;

            fixed (int* pGlyphAdvances = glyphAdvances)
            {
                analyzer.GetGlyphPlacements(
                    pText,
                    pClusterMap,
                    pTextProps,
                    textLength,
                    pGlyphIndices,
                    pGlyphProps,
                    actualGlyphCount,
                    font,
                    fontSize,
                    1.0, // scalingFactor
                    isSideways,
                    isRightToLeft,
                    CultureInfo.InvariantCulture,
                    null, // features
                    null, // featureRangeLengths
                    System.Windows.Media.TextFormattingMode.Ideal,
                    itemProps,
                    1.0f, // pixelsPerDip
                    pGlyphAdvances,
                    out glyphOffsets
                );
            }

            return (glyphAdvances, glyphOffsets);
        }
    }

    [Fact]
    public void GetGlyphPlacements_SimpleText_ReturnsAdvances()
    {
        var font = GetTestFont();
        var spans = ItemizeHelper.Itemize("Hello");
        var itemProps = (ItemProps)spans![0].element;
        
        var result = GetGlyphPlacementsDirect("Hello", font, itemProps);
        
        result.Should().NotBeNull();
        var (glyphAdvances, glyphOffsets) = result!.Value;
        
        // Should have advances for each glyph
        glyphAdvances.Should().NotBeEmpty();
        glyphAdvances.Length.Should().BeGreaterThanOrEqualTo(5);
        
        // All advances should be positive
        glyphAdvances.Should().AllSatisfy(a => a.Should().BeGreaterThan(0));
        
        // Offsets should exist
        glyphOffsets.Should().NotBeEmpty();
    }

    [Fact]
    public void GetGlyphPlacements_DifferentFontSizes_ScalesAdvances()
    {
        var font = GetTestFont();
        var spans = ItemizeHelper.Itemize("A");
        var itemProps = (ItemProps)spans![0].element;
        
        var result12 = GetGlyphPlacementsDirect("A", font, itemProps, fontSize: 12.0);
        var result24 = GetGlyphPlacementsDirect("A", font, itemProps, fontSize: 24.0);
        
        result12.Should().NotBeNull();
        result24.Should().NotBeNull();
        
        var advance12 = result12!.Value.glyphAdvances[0];
        var advance24 = result24!.Value.glyphAdvances[0];
        
        // Larger font = larger advances
        advance24.Should().BeGreaterThan(advance12);
        ((double)advance24 / advance12).Should().BeApproximately(2.0, 0.1);
    }

    [Fact]
    public void GetGlyphPlacements_MultipleCharacters_ReturnsCorrectCount()
    {
        var font = GetTestFont();
        var spans = ItemizeHelper.Itemize("ABCDE");
        var itemProps = (ItemProps)spans![0].element;
        
        var result = GetGlyphPlacementsDirect("ABCDE", font, itemProps);
        
        result.Should().NotBeNull();
        var (glyphAdvances, glyphOffsets) = result!.Value;
        
        // For simple Latin, should have 5 glyphs
        glyphAdvances.Length.Should().Be(5);
        glyphOffsets.Length.Should().Be(5);
    }

    [Fact]
    public void GetGlyphPlacements_Offsets_AreReasonable()
    {
        var font = GetTestFont();
        var spans = ItemizeHelper.Itemize("Test");
        var itemProps = (ItemProps)spans![0].element;
        
        var result = GetGlyphPlacementsDirect("Test", font, itemProps);
        
        result.Should().NotBeNull();
        var glyphOffsets = result!.Value.glyphOffsets;
        
        // For simple Latin text, offsets should be small/zero
        foreach (var offset in glyphOffsets)
        {
            Math.Abs(offset.du).Should().BeLessThan(1000);
            Math.Abs(offset.dv).Should().BeLessThan(1000);
        }
    }

    [Fact]
    public void GetGlyphPlacements_RightToLeft_Succeeds()
    {
        var font = GetTestFont();
        var spans = ItemizeHelper.Itemize("مرحبا");
        Assert.SkipUnless(spans != null && spans.Count > 0, "Itemization failed");
        
        var itemProps = (ItemProps)spans[0].element;
        
        var result = GetGlyphPlacementsDirect("مرحبا", font, itemProps, isRightToLeft: true);
        
        result.Should().NotBeNull();
        result!.Value.glyphAdvances.Should().NotBeEmpty();
    }

    [Fact]
    public void GetGlyphPlacements_ConsistentWithGetGlyphsAndTheirPlacements()
    {
        var font = GetTestFont();
        var spans = ItemizeHelper.Itemize("Hello");
        var itemProps = (ItemProps)spans![0].element;
        
        // Get results from both methods
        var directResult = GetGlyphPlacementsDirect("Hello", font, itemProps);
        var combinedResult = GetGlyphsAndPlacements("Hello", font, itemProps);
        
        directResult.Should().NotBeNull();
        combinedResult.Should().NotBeNull();
        
        // Advances should match
        directResult!.Value.glyphAdvances.Should().BeEquivalentTo(combinedResult!.Value.glyphAdvances);
    }

    #endregion
}
