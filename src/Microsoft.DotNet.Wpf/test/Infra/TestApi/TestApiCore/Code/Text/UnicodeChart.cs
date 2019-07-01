// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Globalization;

namespace Microsoft.Test.Text
{
    /// <summary>
    /// Specifies a <a href="http://unicode.org/charts">Unicode character code chart</a>.
    /// </summary>
    ///
    /// <remarks>
    /// The Unicode standard defines a number of different character subsets, which are called 
    /// <b>Unicode character code charts</b> (or <b>Unicode charts</b> for short). These charts are available 
    /// on <a href="http://unicode.org/charts">http://unicode.org/charts</a>. The charts divide and categorize
    /// all symbols available in the Unicode range (0x0000 - 0x10FFFF) according to their common characteristics. 
    /// </remarks>
    public enum UnicodeChart
    {
        /// <summary>
        /// Additional Arrows Chart
        /// </summary>
        AdditionalArrows,
        /// <summary>
        /// Additional Shapes Chart
        /// </summary>
        AdditionalShapes,
        /// <summary>
        /// Additional Squared Symbols Chart
        /// </summary>
        AdditionalSquaredSymbols,
        /// <summary>
        /// Aegean Numbers Chart
        /// </summary>
        AegeanNumbers,
        /// <summary>
        /// Alphabetic Presentation Forms Chart
        /// </summary>
        AlphabeticPresentationForms,
        /// <summary>
        /// Ancient Greek Musical Notation Chart
        /// </summary>
        AncientGreekMusicalNotation,
        /// <summary>
        /// Ancient Greek Numbers Chart
        /// </summary> 
        AncientGreekNumbers,
        /// <summary>
        /// Ancient Symbols Chart
        /// </summary>
        AncientSymbols,
        /// <summary>
        /// APL symbols Chart
        /// </summary>
        AplSymbols,
        /// <summary>
        /// Arabic Chart
        /// </summary>
        Arabic,
        /// <summary>
        /// Arabic Presentation Forms-A Chart
        /// </summary>
        ArabicPresentationFormsA,
        /// <summary>
        /// Arabic Presentation Forms-B Chart
        /// </summary>
        ArabicPresentationFormsB,
        /// <summary>
        /// Arabic Supplement Chart
        /// </summary>
        ArabicSupplement,
        /// <summary>
        /// Aramaic Imperial Chart
        /// </summary>
        AramaicImperial,
        /// <summary>
        /// Armenian Chart
        /// </summary>
        Armenian,
        /// <summary>
        /// Armenian Ligatures Chart
        /// </summary>
        ArmenianLigatures,
        /// <summary>
        /// Arrows Chart
        /// </summary>
        Arrows,
        /// <summary>
        /// ASCII Characters Chart
        /// </summary>
        AsciiCharacters,
        /// <summary>
        /// ASCII Digits Chart
        /// </summary>
        AsciiDigits,
        /// <summary>
        /// ASCII Punctuation Chart
        /// </summary> 
        AsciiPunctuation,
        /// <summary>
        /// Same as BMP Chart
        /// </summary>
        AtEndOf,
        /// <summary>
        /// Avestan Chart
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704")]
        Avestan,
        /// <summary>
        /// Balinese Chart
        /// </summary>
        Balinese,
        /// <summary>
        /// Bamum Chart
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704")]
        Bamum,
        /// <summary>
        /// Basic operators Division Multiplication Chart
        /// </summary>
        BasicOperatorsDivisionMultiplication,
        /// <summary>
        /// Basic operators Plus Factorial Chart
        /// </summary>
        BasicOperatorsPlusFactorial,
        /// <summary>
        /// Bengali Chart
        /// </summary>
        Bengali,
        /// <summary>
        /// Block Elements Chart
        /// </summary>
        BlockElements,
        /// <summary>
        /// BMP Chart
        /// </summary>
        Bmp,
        /// <summary>
        /// Bopomofo Chart
        /// </summary>
        Bopomofo,
        /// <summary>
        /// Bopomofo Extended Chart
        /// </summary> 
        BopomofoExtended,
        /// <summary>
        /// Box Drawing Chart
        /// </summary>
        BoxDrawing,
        /// <summary>
        /// Braille Patterns Chart
        /// </summary>
        BraillePatterns,
        /// <summary>
        /// Buginese Chart
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704")]
        Buginese,
        /// <summary>
        /// Buhid Chart
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704")]
        Buhid,
        /// <summary>
        /// Byzantine Musical Symbols Chart
        /// </summary>
        ByzantineMusicalSymbols,
        /// <summary>
        /// C0 Chart
        /// </summary>
        C0,
        /// <summary>
        /// C1 Chart
        /// </summary>
        C1,
        /// <summary>
        /// Card suits Chart
        /// </summary>
        CardSuits,
        /// <summary>
        /// Carian Chart
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704")]
        Carian,
        /// <summary>
        /// Cham Chart
        /// </summary>
        Cham,
        /// <summary>
        /// Cherokee Chart
        /// </summary>
        Cherokee,
        /// <summary>
        /// Chess/Checkers Chart
        /// </summary>
        ChessCheckers,
        /// <summary>
        /// CJK Compatibility Chart
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704")]
        CjkCompatibility,
        /// <summary>
        /// CJK Compatibility Forms Chart
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704")]
        CjkCompatibilityForms,
        /// <summary>
        /// CJK Compatibility Ideographs Chart
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704")]
        CjkCompatibilityIdeographs,
        /// <summary>
        /// CJK Compatibility Ideographs Supplement Chart
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704")]
        CjkCompatibilityIdeographsSupplement,
        /// <summary>
        /// CJK ExtensionA Chart
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704")]
        CjkExtensionA,
        /// <summary>
        /// CJK Extension-B Chart
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704")]
        CjkExtensionB,
        /// <summary>
        /// CJK Extension-C Chart
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704")]
        CjkExtensionC,
        /// <summary>
        /// CJK Radicals / KangXi Chart
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1709")]
        CjkKangXiRadicals,
        /// <summary>
        /// CJK Radicals Supplement
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704")]
        CjkRadicalsSupplement,
        /// <summary>
        /// CJK Strokes Chart
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704")]
        CjkStrokes,
        /// <summary>
        /// CJK Symbols and Punctuation Chart
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704")]
        CjkSymbolsAndPunctuation,
        /// <summary>
        /// CJK Unified Ideographs Chart
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704")]
        CjkUnifiedIdeographs,
        /// <summary>
        /// Combining Diacritical Marks Chart
        /// </summary>   
        CombiningDiacriticalMarks,
        /// <summary>
        /// Combining Diacritical Marks for Symbols Chart
        /// </summary>   
        CombiningDiacriticalMarksForSymbols,
        /// <summary>
        /// Combining Diacritical Marks Supplement Chart
        /// </summary>
        CombiningDiacriticalMarksSupplement,
        /// <summary>
        /// Combining HalfMarks Chart
        /// </summary>
        CombiningHalfMarks,
        /// <summary>
        /// Common Indic Number Forms Chart
        /// </summary>
        CommonIndicNumberForms,
        /// <summary>
        /// Control Pictures Chart
        /// </summary>  
        ControlPictures,
        /// <summary>
        /// C0 and C1 Chart
        /// </summary>
        Controls,
        /// <summary>
        /// Coptic Chart
        /// </summary>
        Coptic,
        /// <summary>
        /// Coptic in Greek Block Chart
        /// </summary> 
        CopticInGreekBlock,
        /// <summary>
        /// Counting Rod Numerals Chart
        /// </summary> 
        CountingRodNumerals,
        /// <summary>
        /// Cuneiform Chart
        /// </summary>
        Cuneiform,
        /// <summary>
        /// Cuneiform Numbers and Punctuation Chart
        /// </summary> 
        CuneiformNumbersAndPunctuation,
        /// <summary>
        /// Currency Symbols Chart
        /// </summary>
        CurrencySymbols,
        /// <summary>
        /// Cypriot Syllabary Chart 
        /// </summary> 
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704")]
        CypriotSyllabary,
        /// <summary>
        /// Cyrillic Chart
        /// </summary>
        Cyrillic,
        /// <summary>
        /// Cyrillic Extended-A Chart
        /// </summary>
        CyrillicExtendedA,
        /// <summary>
        /// Cyrillic Extended-B Chart
        /// </summary>
        CyrillicExtendedB,
        /// <summary>
        /// Cyrillic Supplement Chart
        /// </summary>
        CyrillicSupplement,
        /// <summary>
        /// Deseret Chart 
        /// </summary>
        Deseret,
        /// <summary>
        /// Devanagari Chart
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704")]
        Devanagari,
        /// <summary>
        /// Devanagari Extended Chart
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704")]
        DevanagariExtended,
        /// <summary>
        /// Dingbats Chart
        /// </summary>
        Dingbats,
        /// <summary>
        /// Dollar Sign Chart
        /// </summary>
        DollarSign,
        /// <summary>
        /// Domino Tiles Chart
        /// </summary>
        DominoTiles,
        /// <summary>
        /// Egyptian Hieroglyphs Chart
        /// </summary>
        EgyptianHieroglyphs,
        /// <summary>
        /// Enclosed Alphanumerics Chart
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704")]
        EnclosedAlphanumerics,
        /// <summary>
        /// Enclosed Alphanumeric Supplement Chart
        /// </summary>
        EnclosedAlphanumericSupplement,
        /// <summary>
        /// Enclosed CJK Letters and Months Chart
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704")]
        EnclosedCjkLettersAndMonths,
        /// <summary>
        /// Enclosed Ideographic Supplement Chart
        /// </summary>
        EnclosedIdeographicSupplement,
        /// <summary>
        /// Ethiopic Chart
        /// </summary>
        Ethiopic,
        /// <summary>
        /// Ethiopic Extended Chart
        /// </summary> 
        EthiopicExtended,
        /// <summary>
        /// Ethiopic Supplement Chart
        /// </summary>
        EthiopicSupplement,
        /// <summary>
        /// Euro Sign Chart
        /// </summary>
        EuroSign,
        /// <summary>
        /// Floors and ceilings
        /// </summary>  
        FloorsAndCeilings,
        /// <summary>
        /// Fullwidth ASCII Digits Chart
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704")]
        FullwidthAsciiDigits,
        /// <summary>
        /// Fullwidth ASCII Punctuation Chart
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704")]
        FullwidthAsciiPunctuation,
        /// <summary>
        /// Fullwidth Currency Symbols Chart
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704")]
        FullwidthCurrencySymbols,
        /// <summary>
        /// Fullwidth Latin Letters Chart
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704")]
        FullwidthLatinLetters,
        /// <summary>
        /// Same as Chess/Checkers Chart
        /// </summary>
        GameSymbols,
        /// <summary>
        /// General Punctuation Chart
        /// </summary>
        GeneralPunctuation,
        /// <summary>
        /// Geometric Shapes Chart
        /// </summary>
        GeometricShapes,
        /// <summary>
        /// Georgian Chart
        /// </summary>
        Georgian,
        /// <summary>
        /// Georgian Supplement Chart
        /// </summary>
        GeorgianSupplement,
        /// <summary>
        /// Glagolitic Chart
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704")]
        Glagolitic,
        /// <summary>
        /// Gothic Chart
        /// </summary>
        Gothic,
        /// <summary>
        /// Greek Chart
        /// </summary> 
        Greek,
        /// <summary>
        /// Greek Extended Chart
        /// </summary> 
        GreekExtended,
        /// <summary>
        /// Gujarati Chart
        /// </summary>
        Gujarati,
        /// <summary>
        /// Gurmukhi Chart
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704")]
        Gurmukhi,
        /// <summary>
        /// Halfwidth and Fullwidth Forms Chart
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704")]
        HalfwidthAndFullwidthForms,
        /// <summary>
        /// Half width Jamo Chart
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704")]
        HalfwidthJamo,
        /// <summary>
        /// Half width Katakana Chart
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704")]
        HalfwidthKatakana,
        /// <summary>
        /// Hangul Compatibility Jamo Chart
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704")]
        HangulCompatibilityJamo,
        /// <summary>
        /// Hangul Jamo Chart
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704")]
        HangulJamo,
        /// <summary>
        /// Hangul Jamo ExtendedA Chart
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704")]
        HangulJamoExtendedA,
        /// <summary>
        /// Hangul Jamo Extended-B Chart
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704")]
        HangulJamoExtendedB,
        /// <summary>
        /// Hangul Syllables Chart
        /// </summary>
        HangulSyllables,
        /// <summary>
        /// Hanunoo Chart
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704")]
        Hanunoo,
        /// <summary>
        /// Hebrew Chart
        /// </summary>
        Hebrew,
        /// <summary> 
        /// Hebrew Presentation Forms Chart
        /// </summary>
        HebrewPresentationForms,
        /// <summary>
        /// High Surrogates Chart
        /// </summary>
        HighSurrogates,
        /// <summary>
        /// Hiragana Chart
        /// </summary>
        Hiragana,
        /// <summary>
        /// Ideographic Description Characters Chart
        /// </summary>
        IdeographicDescriptionCharacters,
        /// <summary>
        /// Invisible Operators Chart
        /// </summary>
        InvisibleOperators,
        /// <summary>
        /// IPA Extensions Chart
        /// </summary> 
        IpaExtensions,
        /// <summary>
        /// Japanese Chess Chart 
        /// </summary>
        JapaneseChess,
        /// <summary>
        /// Javanese Chart
        /// </summary>
        Javanese,
        /// <summary>
        /// Kaithi Chart
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704")]
        Kaithi,
        /// <summary>
        /// Kanbun Chart
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704")]
        Kanbun,
        /// <summary>
        /// Kannada Chart
        /// </summary>
        Kannada,
        /// <summary>
        /// Katakana Chart
        /// </summary>
        Katakana,
        /// <summary>
        /// Katakana Phonetic Extensions Chart
        /// </summary>
        KatakanaPhoneticExtensions,
        /// <summary>
        /// Kayah Li Chart
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1709")]
        KayahLi,
        /// <summary>
        /// Kharoshthi Chart
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704")]
        Kharoshthi,
        /// <summary>
        /// Khmer Chart
        /// </summary> 
        Khmer,
        /// <summary>
        /// Khmer Symbols Chart
        /// </summary>
        KhmerSymbols,
        /// <summary>
        /// Lao Chart
        /// </summary>
        Lao,
        /// <summary>
        /// Latin Chart
        /// </summary> 
        Latin,
        /// <summary>
        /// Latin-1 Punctuation Chart
        /// </summary>
        Latin1Punctuation,
        /// <summary>
        /// Latin-1 Supplement Chart
        /// </summary>
        Latin1Supplement,
        /// <summary>
        /// Latin Extended-A Chart
        /// </summary>
        LatinExtendedA,
        /// <summary>
        /// Latin Extended Additional Chart
        /// </summary> 
        LatinExtendedAdditional,
        /// <summary>
        /// Latin Extended-B Chart
        /// </summary>
        LatinExtendedB,
        /// <summary>
        /// Latin Extended-C Chart
        /// </summary>
        LatinExtendedC,
        /// <summary>
        /// Latin Extended-D Chart
        /// </summary>
        LatinExtendedD,
        /// <summary>
        /// Latin Ligatures Chart
        /// </summary>
        LatinLigatures,
        /// <summary>
        /// Layout Controls Chart
        /// </summary>
        LayoutControls,
        /// <summary>
        /// Lepcha Chart
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704")]
        Lepcha,
        /// <summary>
        /// Letterlike Symbols Chart
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704")]
        LetterlikeSymbols,
        /// <summary>
        /// Limbu Chart
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704")]
        Limbu,
        /// <summary>
        /// Linear B Syllabary and Linear B Ideograms Chart
        /// </summary>
        LinearB,
        /// <summary>
        /// Linear B Ideograms Chart
        /// </summary>
        LinearBIdeograms,
        /// <summary>
        /// Linear B Syllabary Chart
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704")]
        LinearBSyllabary,
        /// <summary>
        /// Lisu Chart
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704")]
        Lisu,
        /// <summary>
        /// Low Surrogates Chart
        /// </summary>
        LowSurrogates,
        /// <summary>
        /// Lycian Chart
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704")]
        Lycian,
        /// <summary>
        /// Lydian Chart
        /// </summary>
        Lydian,
        /// <summary>
        /// Mahjong Tiles Chart
        /// </summary>
        MahjongTiles,
        /// <summary>
        /// Malayalam Chart
        /// </summary>
        Malayalam,
        /// <summary>
        /// Mark Chart
        /// </summary>
        Mark,
        /// <summary>
        /// Mathematical Alphanumeric Symbols Chart
        /// </summary> 
        MathematicalAlphanumericSymbols,
        /// <summary>
        /// Mathematical Operators Chart
        /// </summary>
        MathematicalOperators,
        /// <summary>
        /// Meetei Mayek Chart
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704")]
        MeeteiMayek,
        /// <summary>
        /// Miscellaneous Mathematical SymbolsA Chart
        /// </summary>
        MiscellaneousMathematicalSymbolsA,
        /// <summary>
        /// Miscellaneous Mathematical SymbolsB Chart
        /// </summary>
        MiscellaneousMathematicalSymbolsB,
        /// <summary>
        /// Miscellaneous Symbols Chart
        /// </summary>
        MiscellaneousSymbols,
        /// <summary>
        /// Miscellaneous Symbols and Arrows Chart
        /// </summary>
        MiscellaneousSymbolsAndArrows,
        /// <summary>
        /// Miscellaneous Technical Chart
        /// </summary>
        MiscellaneousTechnical,
        /// <summary>
        /// Modifier Tone Letters Chart
        /// </summary>
        ModifierToneLetters,
        /// <summary>
        /// Mongolian Chart
        /// </summary>
        Mongolian,
        /// <summary>
        /// Musical Symbols Chart
        /// </summary>
        MusicalSymbols,
        /// <summary>
        /// Myanmar Chart
        /// </summary>
        Myanmar,
        /// <summary>
        /// Myanmar Extended-A Chart
        /// </summary>
        MyanmarExtendedA,
        /// <summary>
        /// New Tai Lue Chart
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704")]
        NewTaiLue,
        /// <summary>
        /// N'Ko Chart
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1709")]
        NKo,
        /// <summary>
        /// Number Forms Chart
        /// </summary>
        NumberForms,
        /// <summary>
        /// Ogham Chart
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704")]
        Ogham,
        /// <summary>
        /// Ol Chiki Chart
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1709")]
        OlChiki,
        /// <summary>
        /// Old Italic Chart
        /// </summary>
        OldItalic,
        /// <summary>
        /// Old Persian Chart
        /// </summary>
        OldPersian,
        /// <summary>
        /// Old South Arabian Chart
        /// </summary> 
        OldSouthArabian,
        /// <summary>
        /// Old Turkic Chart
        /// </summary>
        OldTurkic,
        /// <summary>
        /// Optical Character Recognition Chart
        /// </summary>
        OpticalCharacterRecognition,
        /// <summary>
        /// Oriya Chart
        /// </summary>
        Oriya,
        /// <summary>
        /// Osmanya Chart
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704")]
        Osmanya,
        /// <summary>
        /// Pahlavi Inscriptional Chart
        /// </summary>
        PahlaviInscriptional,
        /// <summary>
        /// Parthian Inscriptional Chart
        /// </summary>
        ParthianInscriptional,
        /// <summary>
        /// Pfennig Chart
        /// </summary>
        Pfennig,
        /// <summary>
        /// Phags-Pa Chart
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1709")]
        PhagsPa,
        /// <summary>
        /// Phaistos Disc Chart
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704")]
        PhaistosDisc,
        /// <summary>
        /// Phoenician Chart
        /// </summary>
        Phoenician,
        /// <summary>
        /// Phonetic Extensions Chart
        /// </summary>
        PhoneticExtensions,
        /// <summary>
        /// Phonetic Extensions Supplement Chart
        /// </summary>
        PhoneticExtensionsSupplement,
        /// <summary>
        /// Plane 1 Chart
        /// </summary>
        Plane1,
        /// <summary>
        /// Plane 10 Chart
        /// </summary>
        Plane10,
        /// <summary>
        /// Plane 11 Chart
        /// </summary>
        Plane11,
        /// <summary>
        /// Plane 12 Chart
        /// </summary>
        Plane12,
        /// <summary>
        /// Plane 13 Chart
        /// </summary>
        Plane13,
        /// <summary>
        /// Plane 14 Chart
        /// </summary>
        Plane14,
        /// <summary>
        /// Plane 15 Chart
        /// </summary>
        Plane15,
        /// <summary>
        /// Plane 16 Chart
        /// </summary>
        Plane16,
        /// <summary>
        /// Plane 2 Chart
        /// </summary>
        Plane2,
        /// <summary>
        /// Plane 3 Chart
        /// </summary>
        Plane3,
        /// <summary>
        /// Plane 4 Chart
        /// </summary>
        Plane4,
        /// <summary>
        /// Plane 5 Chart
        /// </summary>
        Plane5,
        /// <summary>
        /// Plane 6 Chart
        /// </summary>
        Plane6,
        /// <summary>
        /// Plane 7 Chart
        /// </summary>
        Plane7,
        /// <summary>
        /// Plane 8 Chart
        /// </summary>
        Plane8,
        /// <summary>
        /// Plane 9 Chart
        /// </summary>
        Plane9,
        /// <summary>
        /// Private Use Area Chart
        /// </summary>
        PrivateUseArea,
        /// <summary>
        /// Rejang Chart
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704")]
        Rejang,
        /// <summary>
        /// Reserved Range Chart
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1700")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704")]
        ReservedRange,
        /// <summary>
        /// Rial Sign Chart
        /// </summary> 
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704")]
        RialSign,
        /// <summary>
        /// Roman Symbols Chart
        /// </summary>
        RomanSymbols,
        /// <summary>
        /// Rumi Numeral Symbols Chart
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704")]
        RumiNumeralSymbols,
        /// <summary>
        /// Runic Chart
        /// </summary>
        Runic,
        /// <summary>
        /// Samaritan Chart
        /// </summary>
        Samaritan,
        /// <summary>
        /// Saurashtra Chart
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704")]
        Saurashtra,
        /// <summary>
        /// Shavian Chart
        /// </summary>
        Shavian,
        /// <summary>
        /// Sinhala Chart
        /// </summary>
        Sinhala,
        /// <summary>
        /// Small Form Variants Chart
        /// </summary>
        SmallFormVariants,
        /// <summary>
        /// Spacing Modifier Letters Chart
        /// </summary>
        SpacingModifierLetters,
        /// <summary>
        /// Specials Chart
        /// </summary>
        Specials,
        /// <summary>
        /// Sundanese Chart
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704")]
        Sundanese,
        /// <summary>
        /// Super and Subscripts Chart
        /// </summary>
        SuperAndSubscripts,
        /// <summary>
        /// Superscripts and Subscripts Chart
        /// </summary>
        SuperscriptsAndSubscripts,
        /// <summary>
        /// Supplemental Arrows-A Chart
        /// </summary> 
        SupplementalArrowsA,
        /// <summary>
        /// Supplemental Arrows-B Chart
        /// </summary>
        SupplementalArrowsB,
        /// <summary>
        /// Supplemental Mathematical Operators Chart
        /// </summary>
        SupplementalMathematicalOperators,
        /// <summary>
        /// Supplemental Punctuation Chart
        /// </summary>
        SupplementalPunctuation,
        /// <summary>
        /// Supplementary Private Use Area-A Chart
        /// </summary>
        SupplementaryPrivateUseAreaA,
        /// <summary>
        /// Supplementary Private Use Area-B Chart
        /// </summary>
        SupplementaryPrivateUseAreaB,
        /// <summary>
        /// Syloti Nagri Chart
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704")]
        SylotiNagri,
        /// <summary>
        /// Syriac Chart
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704")]
        Syriac,
        /// <summary>
        /// Tagalog Chart
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704")]
        Tagalog,
        /// <summary>
        /// Tagbanwa Chart
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704")]
        Tagbanwa,
        /// <summary>
        /// Tags Chart
        /// </summary>
        Tags,
        /// <summary>
        /// Tai Le Chart
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1709")]
        TaiLe,
        /// <summary>
        /// Tai Tham Chart
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704")]
        TaiTham,
        /// <summary>
        /// Tai Viet Chart
        /// </summary>
        TaiViet,
        /// <summary>
        /// Tai Xuan Jing Symbols Chart
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704")]
        TaiXuanJingSymbols,
        /// <summary>
        /// Tamil Chart
        /// </summary>
        Tamil,
        /// <summary>
        /// Telugu Chart
        /// </summary>
        Telugu,
        /// <summary>
        /// Thaana Chart
        /// </summary>
        Thaana,
        /// <summary>
        /// Thai Chart
        /// </summary>
        Thai,
        /// <summary>
        /// Tibetan Chart
        /// </summary>
        Tibetan,
        /// <summary>
        /// Tifinagh Chart
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704")]
        Tifinagh,
        /// <summary>
        /// Ugaritic Chart
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704")]
        Ugaritic,
        /// <summary>
        /// Unified Canadian Aboriginal Syllabics Chart
        /// </summary> 
        UnifiedCanadianAboriginalSyllabics,
        /// <summary>
        /// Unified Canadian Aboriginal Syllabics ExtendedChart
        /// </summary> 
        UnifiedCanadianAboriginalSyllabicsExtended,
        /// <summary>
        /// Vai Chart
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704")]
        Vai,
        /// <summary>
        /// Variation Selectors Chart
        /// </summary>
        VariationSelectors,
        /// <summary>
        /// Variation Selectors Supplement Chart
        /// </summary>
        VariationSelectorsSupplement,
        /// <summary>
        /// Vedic Extensions Chart
        /// </summary>
        VedicExtensions,
        /// <summary>
        /// Vertical Forms Chart
        /// </summary>
        VerticalForms,
        /// <summary>
        /// Yen Pound and Cent Chart
        /// </summary>
        YenPoundAndCent,
        /// <summary>
        /// Yi Syllables and Yi Radicals Chart
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1709")]
        Yi,
        /// <summary>
        /// Yijing Hexagram Symbols Chart
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704")]
        YijingHexagramSymbols,
        /// <summary>
        /// Yijing Mono-    Di- and Trigrams Chart
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1709")]
        YijingMonoDiAndTrigrams,
        /// <summary>
        /// Same as Yijing Mono-    Di- and Trigrams Chart
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1709")]
        YijingSymbols,
        /// <summary>
        /// Yi Radicals Chart
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1709")]
        YiRadicals,
        /// <summary>
        /// Yi Syllables Chart
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1709")]
        YiSyllables,
    }
}

