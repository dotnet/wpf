// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace Microsoft.Test.Text
{
    /// <summary>
    /// Collection of Unicode ranges including Scripts, Symbols, and Punctuation
    /// </summary>
    internal class UnicodeRangeDatabase
    {
        private static readonly Group [] scripts = new Group[TextUtil.NUMOFSCRIPTS];
        private static readonly Group [] symbolsAndPunctuation = new Group[TextUtil.NUMOFSYMBOLSANDPUNCTUATION];

        /// <summary>
        /// Define UnicodeRangeDatabase class, 
        /// <a href="http://www.unicode.org/charts/">Newline</a>
        /// </summary>
        public UnicodeRangeDatabase( )
        {
            if (null == scripts[0])
            {
                InitializeScripts();
            }

            if (null == symbolsAndPunctuation[0])
            {
                InitializeSymbolsAndPunctuation();
            }
        }

        /// <summary>
        /// Getter for scripts
        /// </summary>
        public Group [] Scripts { get { return scripts; } }

        /// <summary>
        /// Getter for symbols and punctuation
        /// </summary>
        public Group [] SymbolsAndPunctuation { get { return symbolsAndPunctuation; } }

        private static void InitializeScripts()
        {
            // For the 3rd parameter ids - all lower cases
            // If LCID exists, Short String description is used; otherwise, spell out the whole name in lower case.
            // If it applies to the root culture e.g. zh-cn, zh-hk, zh-tw, the root culture id is used e.g. "zh". '-' is omitted.
            // If it can be used for various culuture, "any" is used. 
            // If LCID does not exist, full spell of the culture is the id and space is omitted.
            scripts[0] = new Group(new UnicodeRange(0x0530, 0x058F), "European Scripts", "Armenian", "hy", UnicodeChart.Armenian);
            scripts[0].SubGroups = new SubGroup[1];
            scripts[0].SubGroups[0] = new SubGroup(new UnicodeRange(0xFB00, 0xFB4F), "Armenian Ligatures", "hy", UnicodeChart.ArmenianLigatures);
            
            scripts[1] = new Group(new UnicodeRange(0x2C80, 0x2CFF), "European Scripts", "Coptic", "eg", UnicodeChart.Coptic);
            scripts[1].SubGroups = new SubGroup[1];
            scripts[1].SubGroups[0] = new SubGroup(new UnicodeRange(0x0370, 0x03FF), "Coptic in Greek block", "eg,el", UnicodeChart.CopticInGreekBlock);

            scripts[2] = new Group(new UnicodeRange(0x10800, 0x1083F), "European Scripts", "Cypriot Syllabary", "cyprus", UnicodeChart.CypriotSyllabary);

            scripts[3] = new Group(new UnicodeRange(0x0400, 0x04FF), "European Scripts", "Cyrillic", "azaz,srsp,uzuz", UnicodeChart.Cyrillic);
            scripts[3].SubGroups = new SubGroup[3];
            scripts[3].SubGroups[0] = new SubGroup(new UnicodeRange(0x0500, 0x052F), "Cyrillic Supplement", "azaz,srsp,uzuz", UnicodeChart.CyrillicSupplement);
            scripts[3].SubGroups[1] = new SubGroup(new UnicodeRange(0x2DE0, 0x2DFF), "Cyrillic Extended-A", "azaz,srsp,uzuz", UnicodeChart.CyrillicExtendedA);
            scripts[3].SubGroups[2] = new SubGroup(new UnicodeRange(0xA640, 0xA69F), "Cyrillic Extended-B", "azaz,srsp,uzuz", UnicodeChart.CyrillicExtendedB);

            scripts[4] = new Group(new UnicodeRange(0x10A0, 0x10FF), "European Scripts", "Georgian", "georgia", UnicodeChart.Georgian);
            scripts[4].SubGroups = new SubGroup[1];
            scripts[4].SubGroups[0] = new SubGroup(new UnicodeRange(0x2D00, 0x2D2F), "Georgian Supplement", "georgia", UnicodeChart.GeorgianSupplement);

            scripts[5] = new Group(new UnicodeRange(0x2C00, 0x2C5F), "European Scripts", "Glagolitic", "glagolitsa", UnicodeChart.Glagolitic);
            scripts[6] = new Group(new UnicodeRange(0x10330, 0x1034F), "European Scripts", "Gothic", "de", UnicodeChart.Gothic);

            scripts[7] = new Group(new UnicodeRange(0x0370, 0x03FF), "European Scripts", "Greek", "el", UnicodeChart.Greek); 
            scripts[7].SubGroups = new SubGroup[1];
            scripts[7].SubGroups[0] = new SubGroup(new UnicodeRange(0x1F00, 0x1FFF), "Greek Extended", "el", UnicodeChart.GreekExtended);

            scripts[8] = new Group(new UnicodeRange(0x0000, 0x007F), "European Scripts", "Latin", "latin", UnicodeChart.Latin);
            scripts[8].SubGroups = new SubGroup[8];
            scripts[8].SubGroups[0] = new SubGroup(new UnicodeRange(0x0080, 0x00FF), "Latin-1 Supplement", "latin", UnicodeChart.Latin1Supplement);
            scripts[8].SubGroups[1] = new SubGroup(new UnicodeRange(0x0100, 0x017F), "Latin Extended-A", "latin", UnicodeChart.LatinExtendedA);
            scripts[8].SubGroups[2] = new SubGroup(new UnicodeRange(0x0180, 0x024F), "Latin Extended-B", "latin", UnicodeChart.LatinExtendedB);
            scripts[8].SubGroups[3] = new SubGroup(new UnicodeRange(0x2C60, 0x2C7F), "Latin Extended-C", "latin", UnicodeChart.LatinExtendedC);
            scripts[8].SubGroups[4] = new SubGroup(new UnicodeRange(0xA720, 0xA7FF), "Latin Extended-D", "latin", UnicodeChart.LatinExtendedD);
            scripts[8].SubGroups[5] = new SubGroup(new UnicodeRange(0x1E00, 0x1EFF), "Latin Extended Additional", "latin", UnicodeChart.LatinExtendedAdditional);
            scripts[8].SubGroups[6] = new SubGroup(new UnicodeRange(0xFB00, 0xFB4F), "Latin Ligatures", "latin", UnicodeChart.LatinLigatures);
            scripts[8].SubGroups[7] = new SubGroup(new UnicodeRange(0xFF00, 0xFFEF), "FullWidth Latin Letters", "latin", UnicodeChart.FullwidthLatinLetters);

            // Linear B doesn't have a range specified. Use the range of both sub groups
            scripts[9] = new Group(new UnicodeRange(0x10000, 0x100FF), "European Scripts", "Linear B", "other", UnicodeChart.LinearB);
            scripts[9].SubGroups = new SubGroup[2];
            scripts[9].SubGroups[0] = new SubGroup(new UnicodeRange(0x10000, 0x1007F), "Linear B Syllabary", "other", UnicodeChart.LinearBSyllabary);
            scripts[9].SubGroups[1] = new SubGroup(new UnicodeRange(0x10080, 0x100FF), "Linear B Ideograms", "other", UnicodeChart.LinearBIdeograms);

            scripts[10] = new Group(new UnicodeRange(0x1680, 0x169F), "European Scripts", "Ogham", "ie", UnicodeChart.Ogham);
            scripts[11] = new Group(new UnicodeRange(0x10300, 0x1032F), "European Scripts", "Old Italic", "other", UnicodeChart.OldItalic);
            scripts[12] = new Group(new UnicodeRange(0x101D0, 0x101FF), "European Scripts", "Phaistos Disc", "phaistosdisc", UnicodeChart.PhaistosDisc);
            scripts[13] = new Group(new UnicodeRange(0x16A0, 0x16FF), "European Scripts", "Runic", "de", UnicodeChart.Runic);
            scripts[14] = new Group(new UnicodeRange(0x10450, 0x1047F), "European Scripts", "Shavian", "en", UnicodeChart.Shavian);

            scripts[15] = new Group(new UnicodeRange(0x0250, 0x02AF), "Phonetic Symbols", "IPA Extensions", "latin", UnicodeChart.IpaExtensions);

            scripts[16] = new Group(new UnicodeRange(0x1D00, 0x1D7F), "Phonetic Symbols", "Phonetic Extensions", "latin", UnicodeChart.PhoneticExtensions);
            scripts[16].SubGroups = new SubGroup[1];
            scripts[16].SubGroups[0] = new SubGroup(new UnicodeRange(0x1D80, 0x1D8F), "Phonetic Extensions Supplement", "latin", UnicodeChart.PhoneticExtensionsSupplement);

            scripts[17] = new Group(new UnicodeRange(0xA700, 0xA71F), "Phonetic Symbols", "Modifier Tone Letters", "other", UnicodeChart.ModifierToneLetters);
            scripts[18] = new Group(new UnicodeRange(0x02B0, 0x02FF), "Phonetic Symbols", "Spacing Modifier Letters", "other", UnicodeChart.SpacingModifierLetters);
            scripts[19] = new Group(new UnicodeRange(0x2070, 0x209F), "Phonetic Symbols", "Superscripts and Subscripts", "any", UnicodeChart.SuperscriptsAndSubscripts);

            scripts[20] = new Group(new UnicodeRange(0x0300, 0x036F), "Combining Diacritics", "Combining Diacritical Marks", "other", UnicodeChart.CombiningDiacriticalMarks);
            scripts[20].SubGroups = new SubGroup[1];
            scripts[20].SubGroups[0] = new SubGroup(new UnicodeRange(0x1DC0, 0x1DFF), "Combining Diacritical Marks Supplement", "other", UnicodeChart.CombiningDiacriticalMarksSupplement);

            scripts[21] = new Group(new UnicodeRange(0xFE20, 0xFE2F), "Combining Diacritics", "Combining Half Marks", "other", UnicodeChart.CombiningHalfMarks);

            scripts[22] = new Group(new UnicodeRange(0xA6A0, 0xA6FF), "African Scripts", "Bamum", "cameroon", UnicodeChart.Bamum);
            scripts[23] = new Group(new UnicodeRange(0x13000, 0x1342F), "African Scripts", "Egyptian Hieroglyphs", "eg", UnicodeChart.EgyptianHieroglyphs);
            
            scripts[24] = new Group(new UnicodeRange(0x1200, 0x137F), "African Scripts", "Ethiopic", "ethiopia", UnicodeChart.Ethiopic);
            scripts[24].SubGroups = new SubGroup[2];
            scripts[24].SubGroups[0] = new SubGroup(new UnicodeRange(0x1380, 0x139F), "Ethiopic Supplement", "ethiopia", UnicodeChart.EthiopicSupplement);
            scripts[24].SubGroups[1] = new SubGroup(new UnicodeRange(0x2D80, 0x2DDF), "Ethiopic Extended", "ethiopia", UnicodeChart.EthiopicExtended);
            
            scripts[25] = new Group(new UnicodeRange(0xA700, 0xA71F), "African Scripts", "N'ko", "nko", UnicodeChart.NKo);
            scripts[26] = new Group(new UnicodeRange(0x10480, 0x104AF), "African Scripts", "Osmanya", "somalia", UnicodeChart.Osmanya);
            scripts[27] = new Group(new UnicodeRange(0x2D30, 0x2D7F), "African Scripts", "Tifinagh", "tifinagh", UnicodeChart.Tifinagh);
            scripts[28] = new Group(new UnicodeRange(0xA500, 0xA63F), "African Scripts", "Vai", "vai", UnicodeChart.Vai);

            scripts[29] = new Group(new UnicodeRange(0x0600, 0x06FF), "Middle Eastern Scripts", "Arabic", "ar", UnicodeChart.Arabic);
            scripts[29].SubGroups = new SubGroup[3];
            scripts[29].SubGroups[0] = new SubGroup(new UnicodeRange(0x0750, 0x077F), "Arabic Supplement", "ar", UnicodeChart.ArabicSupplement);
            scripts[29].SubGroups[1] = new SubGroup(new UnicodeRange(0xFB50, 0xFDFF), "Arabic Presentation Forms-A", "ar", UnicodeChart.ArabicPresentationFormsA);
            scripts[29].SubGroups[2] = new SubGroup(new UnicodeRange(0xFE70, 0xFEFF), "Arabic Presentation Forms-B", "ar", UnicodeChart.ArabicPresentationFormsB);
            
            scripts[30] = new Group(new UnicodeRange(0x10840, 0x1085F), "Middle Eastern Scripts", "Aramaic, Imperial", "he", UnicodeChart.AramaicImperial);
            scripts[31] = new Group(new UnicodeRange(0x10B00, 0x10B3F), "Middle Eastern Scripts", "Avestan", "iran", UnicodeChart.Avestan);
            scripts[32] = new Group(new UnicodeRange(0x102A0, 0x102DF), "Middle Eastern Scripts", "Carian", "carians", UnicodeChart.Carian);
            
            scripts[33] = new Group(new UnicodeRange(0x12000, 0x123FF), "Middle Eastern Scripts", "Cuneiform", "cuneiform", UnicodeChart.Cuneiform);
            scripts[33].SubGroups = new SubGroup[3];
            scripts[33].SubGroups[0] = new SubGroup(new UnicodeRange(0x12400, 0x1247F), "Cuneiform Numbers and Punctuation", "cuneiform", UnicodeChart.CuneiformNumbersAndPunctuation);
            scripts[33].SubGroups[1] = new SubGroup(new UnicodeRange(0x103A0, 0x103DF), "Old Persian", "cuneiform", UnicodeChart.OldPersian);
            scripts[33].SubGroups[2] = new SubGroup(new UnicodeRange(0x10380, 0x1039F), "Ugaritic", "cuneiform", UnicodeChart.Ugaritic);
            
            scripts[34] = new Group(new UnicodeRange(0x0590, 0x05FF), "Middle Eastern Scripts", "Hebrew", "he", UnicodeChart.Hebrew);
            scripts[34].SubGroups = new SubGroup[1];
            scripts[34].SubGroups[0] = new SubGroup(new UnicodeRange(0xFB00, 0xFB4F), "Hebrew Presentation Forms", "he", UnicodeChart.HebrewPresentationForms);
            
            scripts[35] = new Group(new UnicodeRange(0x10280, 0x1029F), "Middle Eastern Scripts", "Lycian", "lycia", UnicodeChart.Lycian);
            scripts[36] = new Group(new UnicodeRange(0x10920, 0x1093F), "Middle Eastern Scripts", "Lydian", "lycia", UnicodeChart.Lydian);
            scripts[37] = new Group(new UnicodeRange(0x10A60, 0x10A7F), "Middle Eastern Scripts", "Old South Arabian", "ar", UnicodeChart.OldSouthArabian);
            scripts[38] = new Group(new UnicodeRange(0x10B60, 0x10B7F), "Middle Eastern Scripts", "Pahlavi, Inscriptional", "iran", UnicodeChart.PahlaviInscriptional);
            scripts[39] = new Group(new UnicodeRange(0x10B40, 0x10B5FF), "Middle Eastern Scripts", "Parthian, Inscriptional", "iran", UnicodeChart.ParthianInscriptional);
            scripts[40] = new Group(new UnicodeRange(0x10900, 0x1091F), "Middle Eastern Scripts", "Phoenician", "phoenicia", UnicodeChart.Phoenician);
            scripts[41] = new Group(new UnicodeRange(0x0800, 0x083F), "Middle Eastern Scripts", "Samaritan", "samaria", UnicodeChart.Samaritan);
            scripts[42] = new Group(new UnicodeRange(0x0700, 0x074F), "Middle Eastern Scripts", "Syriac", "syriac", UnicodeChart.Syriac);

            scripts[43] = new Group(new UnicodeRange(0x1800, 0x18AF), "Central Asian Scripts", "Mongolian", "mongolia", UnicodeChart.Mongolian);
            scripts[44] = new Group(new UnicodeRange(0x10C00, 0x10C4F), "Central Asian Scripts", "Old Turkic", "oldturkic", UnicodeChart.OldTurkic);
            scripts[45] = new Group(new UnicodeRange(0xA840, 0xA87F), "Central Asian Scripts", "Phags-Pa", "zh", UnicodeChart.PhagsPa);
            scripts[46] = new Group(new UnicodeRange(0x0F00, 0x0FFF), "Central Asian Scripts", "Tibetan", "zh", UnicodeChart.Tibetan);

            scripts[47] = new Group(new UnicodeRange(0x0980, 0x09FF), "South Asian Scripts", "Bengali", "bangladesh,hi", UnicodeChart.Bengali);
            
            scripts[48] = new Group(new UnicodeRange(0x0900, 0x097F), "South Asian Scripts", "Devanagari", "hi,nepal", UnicodeChart.Devanagari); 
            scripts[48].SubGroups = new SubGroup[1];
            scripts[48].SubGroups[0] = new SubGroup(new UnicodeRange(0xA8E0, 0xA8FF), "Devanagari Extended", "hi", UnicodeChart.DevanagariExtended);
            
            scripts[49] = new Group(new UnicodeRange(0x0A80, 0x0AFF), "South Asian Scripts", "Gujarati", "hi", UnicodeChart.Gujarati);
            scripts[50] = new Group(new UnicodeRange(0x0A00, 0x0A7F), "South Asian Scripts", "Gurmukhi", "hi", UnicodeChart.Gurmukhi);
            scripts[51] = new Group(new UnicodeRange(0x11080, 0x110CF), "South Asian Scripts", "Kaithi", "hi", UnicodeChart.Kaithi);
            scripts[52] = new Group(new UnicodeRange(0x0C80, 0x0CFF), "South Asian Scripts", "Kannada", "hi", UnicodeChart.Kannada);
            scripts[53] = new Group(new UnicodeRange(0x10A00, 0x10A5F), "South Asian Scripts", "Kharoshthi", "kharoshthi", UnicodeChart.Kharoshthi);
            scripts[54] = new Group(new UnicodeRange(0x1C00, 0x1C4F), "South Asian Scripts", "Lepcha", "zh,nepal,hi", UnicodeChart.Lepcha);
            scripts[55] = new Group(new UnicodeRange(0x1900, 0x194F), "South Asian Scripts", "Limbu", "nepal", UnicodeChart.Limbu);
            scripts[56] = new Group(new UnicodeRange(0x0D00, 0x0D7F), "South Asian Scripts", "Malayalam", "hi", UnicodeChart.Malayalam);
            scripts[57] = new Group(new UnicodeRange(0xABC0, 0xABFF), "South Asian Scripts", "Meetei Mayek", "hi", UnicodeChart.MeeteiMayek);
            scripts[58] = new Group(new UnicodeRange(0x1C50, 0x1C7F), "South Asian Scripts", "Ol Chiki", "hi", UnicodeChart.OlChiki);
            scripts[59] = new Group(new UnicodeRange(0x0B00, 0x0B7F), "South Asian Scripts", "Oriya", "hi", UnicodeChart.Oriya);
            scripts[60] = new Group(new UnicodeRange(0xA880, 0xA8DF), "South Asian Scripts", "Saurashtra", "hi", UnicodeChart.Saurashtra);
            scripts[61] = new Group(new UnicodeRange(0x0D80, 0x0DFF), "South Asian Scripts", "Sinhala", "srilanka", UnicodeChart.Sinhala);
            scripts[62] = new Group(new UnicodeRange(0xA800, 0xA82F), "South Asian Scripts", "Syloti Nagri", "sylotinagri", UnicodeChart.SylotiNagri);
            scripts[63] = new Group(new UnicodeRange(0x0B80, 0x0BFF), "South Asian Scripts", "Tamil", "hi,srilanka,singapore", UnicodeChart.Tamil);
            scripts[64] = new Group(new UnicodeRange(0x0C00, 0x0C7F), "South Asian Scripts", "Telugu", "hi", UnicodeChart.Telugu);
            scripts[65] = new Group(new UnicodeRange(0x0780, 0x07BF), "South Asian Scripts", "Thaana", "maldives", UnicodeChart.Thaana);
            scripts[66] = new Group(new UnicodeRange(0x1CD0, 0x1CFF), "South Asian Scripts", "Vedic Extensions", "hi", UnicodeChart.VedicExtensions);

            scripts[67] = new Group(new UnicodeRange(0x1B00, 0x1B7F), "Southeast Asian Scripts", "Balinese", "id", UnicodeChart.Balinese);
            scripts[68] = new Group(new UnicodeRange(0x1A00, 0x1A1F), "Southeast Asian Scripts", "Buginese", "id", UnicodeChart.Buginese); 
            scripts[69] = new Group(new UnicodeRange(0xAA00, 0xAA5F), "Southeast Asian Scripts", "Cham", "vi,th,cambodia", UnicodeChart.Cham); 
            scripts[70] = new Group(new UnicodeRange(0xA980, 0xA9DF), "Southeast Asian Scripts", "Javanese", "id", UnicodeChart.Javanese); 
            scripts[71] = new Group(new UnicodeRange(0xA900, 0xA92F), "Southeast Asian Scripts", "Kayah Li", "myanmar", UnicodeChart.KayahLi);

            scripts[72] = new Group(new UnicodeRange(0x1780, 0x17FF), "Southeast Asian Scripts", "Khmer", "cambodia", UnicodeChart.Khmer); 
            scripts[72].SubGroups = new SubGroup[1];
            scripts[72].SubGroups[0] = new SubGroup(new UnicodeRange(0x17E0, 0x17FF), "Khmer Symbols", "cambodia", UnicodeChart.KhmerSymbols);

            scripts[73] = new Group(new UnicodeRange(0x0E80, 0x0EFF), "Southeast Asian Scripts", "Lao", "lao", UnicodeChart.Lao); 

            scripts[74] = new Group(new UnicodeRange(0x1000, 0x109F), "Southeast Asian Scripts", "Myanmar", "myanmar", UnicodeChart.Myanmar); 
            scripts[74].SubGroups = new SubGroup[1];
            scripts[74].SubGroups[0] = new SubGroup(new UnicodeRange(0xAA60, 0xAA7F), "Myanmar Extended-A", "myanmar", UnicodeChart.MyanmarExtendedA);

            scripts[75] = new Group(new UnicodeRange(0x1980, 0x19DF), "Southeast Asian Scripts", "New Tai Lue", "zh", UnicodeChart.NewTaiLue);
            scripts[76] = new Group(new UnicodeRange(0xA930, 0xA95F), "Southeast Asian Scripts", "Rejang", "id", UnicodeChart.Rejang); 
            scripts[77] = new Group(new UnicodeRange(0x1B80, 0x1BBF), "Southeast Asian Scripts", "Sundanese", "id", UnicodeChart.Sundanese); 
            scripts[78] = new Group(new UnicodeRange(0x1950, 0x197F), "Southeast Asian Scripts", "Tai Le", "zh", UnicodeChart.TaiLe); 
            scripts[79] = new Group(new UnicodeRange(0x1A20, 0x1AAF), "Southeast Asian Scripts", "Tai Tham", "th", UnicodeChart.TaiTham);
            scripts[80] = new Group(new UnicodeRange(0xAA80, 0xAADF), "Southeast Asian Scripts", "Tai Viet", "vi", UnicodeChart.TaiViet); 
            scripts[81] = new Group(new UnicodeRange(0x0E00, 0x0E7F), "Southeast Asian Scripts", "Thai", "th", UnicodeChart.Thai); 

            scripts[82] = new Group(new UnicodeRange(0x1740, 0x175F), "Philippine Scripts", "Buhid", "ph", UnicodeChart.Buhid);
            scripts[83] = new Group(new UnicodeRange(0x1720, 0x173F), "Philippine Scripts", "Hanunoo", "ph", UnicodeChart.Hanunoo); 
            scripts[84] = new Group(new UnicodeRange(0x1700, 0x171F), "Philippine Scripts", "Tagalog", "ph", UnicodeChart.Tagalog); 
            scripts[85] = new Group(new UnicodeRange(0x1760, 0x177F), "Philippine Scripts", "Tagbanwa", "ph", UnicodeChart.Tagbanwa); 

            scripts[86] = new Group(new UnicodeRange(0x3100, 0x312F), "East Asian Scripts", "Bopomofo", "zhtw", UnicodeChart.Bopomofo);
            scripts[86].SubGroups = new SubGroup[1];
            scripts[86].SubGroups[0] = new SubGroup(new UnicodeRange(0x31A0, 0x31BF), "Bopomofo Extended", "zhtw", UnicodeChart.BopomofoExtended);

            scripts[87] = new Group(new UnicodeRange(0x4E00, 0x9FCF), "East Asian Scripts", "CJK Unified Ideographs (Han)", "zh,ja,ko", UnicodeChart.CjkUnifiedIdeographs);
            scripts[87].SubGroups = new SubGroup[3];
            scripts[87].SubGroups[0] = new SubGroup(new UnicodeRange(0x3400, 0x4DBF), "CJK Extension-A", "zh,ja,ko", UnicodeChart.CjkExtensionA);
            scripts[87].SubGroups[1] = new SubGroup(new UnicodeRange(0x20000, 0x2A6DF), "CJK Extension B", "zh,ja,ko", UnicodeChart.CjkExtensionB);
            scripts[87].SubGroups[2] = new SubGroup(new UnicodeRange(0x2A700, 0x2B73F), "CJK Extension C", "zh,ja,ko", UnicodeChart.CjkExtensionC);

            scripts[88] = new Group(new UnicodeRange(0xF900, 0xFAFF), "East Asian Scripts", "CJK Compatibility Ideographs", "zh,ja,ko", UnicodeChart.CjkCompatibilityIdeographs);
            scripts[88].SubGroups = new SubGroup[1];
            scripts[88].SubGroups[0] = new SubGroup(new UnicodeRange(0x2F800, 0x2FA1F), "CJK Compatibility Ideographs Supplement", "zh,ja,ko", UnicodeChart.CjkCompatibilityIdeographsSupplement);

            scripts[89] = new Group(new UnicodeRange(0x2F00, 0x2FDF), "East Asian Scripts", "CJK Radicals // KangXi Radicals", "zh,ja,ko", UnicodeChart.CjkKangXiRadicals); 
            scripts[89].SubGroups = new SubGroup[3];
            scripts[89].SubGroups[0] = new SubGroup(new UnicodeRange(0x2E80, 0x2EFF), "CJK Radicals Supplement", "zh,ja,ko", UnicodeChart.CjkRadicalsSupplement);
            scripts[89].SubGroups[1] = new SubGroup(new UnicodeRange(0x2E80, 0x2EFF), "CJK Strokes", "zh,ja,ko", UnicodeChart.CjkStrokes);
            scripts[89].SubGroups[2] = new SubGroup(new UnicodeRange(0x31C0, 0x31EF), "Ideographic Description Characters", "zh,ja,ko", UnicodeChart.IdeographicDescriptionCharacters);

            scripts[90] = new Group(new UnicodeRange(0x1100, 0x11FF), "East Asian Scripts", "Hangul Jamo", "ko", UnicodeChart.HangulJamo); 
            scripts[90].SubGroups = new SubGroup[4];
            scripts[90].SubGroups[0] = new SubGroup(new UnicodeRange(0xA960, 0xA97F), "Hangul Jamo Extended-A", "ko", UnicodeChart.HangulJamoExtendedA);
            scripts[90].SubGroups[1] = new SubGroup(new UnicodeRange(0xD7B0, 0xD7FF), "Hangul Jamo Extended-B", "ko", UnicodeChart.HangulJamoExtendedB);
            scripts[90].SubGroups[2] = new SubGroup(new UnicodeRange(0x3130, 0x318F), "Hangul Compatibility Jamo", "ko", UnicodeChart.HangulCompatibilityJamo);
            scripts[90].SubGroups[3] = new SubGroup(new UnicodeRange(0xFF00, 0xFFEF), "Halfwidth Jamo", "ko", UnicodeChart.HalfwidthJamo);

            scripts[91] = new Group(new UnicodeRange(0xAC00, 0xD7AF), "East Asian Scripts", "Hangul Syllables", "ko", UnicodeChart.HangulSyllables);
            scripts[92] = new Group(new UnicodeRange(0x3040, 0x309F), "East Asian Scripts", "Hiragana", "ja", UnicodeChart.Hiragana);

            scripts[93] = new Group(new UnicodeRange(0x30A0, 0x30FF), "East Asian Scripts", "Katakana", "ja", UnicodeChart.Katakana);
            scripts[93].SubGroups = new SubGroup[2];
            scripts[93].SubGroups[0] = new SubGroup(new UnicodeRange(0x31F0, 0x31FF), "Katakana Phonetic Extensions", "ja", UnicodeChart.KatakanaPhoneticExtensions);
            scripts[93].SubGroups[1] = new SubGroup(new UnicodeRange(0xFF00, 0xFFEF), "Halfwidth Katakana", "ja", UnicodeChart.HalfwidthKatakana);

            scripts[94] = new Group(new UnicodeRange(0x3190, 0x319F), "East Asian Scripts", "Kanbun", "zh,ja", UnicodeChart.Kanbun);
            scripts[95] = new Group(new UnicodeRange(0xA4D0, 0xA4FF), "East Asian Scripts", "Lisu", "zh", UnicodeChart.Lisu);

            // Yi does not have code range defined. Use the range of both sub groups.
            scripts[96] = new Group(new UnicodeRange(0xA000, 0xA4CF), "East Asian Scripts", "Yi", "zh", UnicodeChart.Yi);
            scripts[96].SubGroups = new SubGroup[2];
            scripts[96].SubGroups[0] = new SubGroup(new UnicodeRange(0xA000, 0xA48F), "Yi Syllables", "zh", UnicodeChart.YiSyllables);
            scripts[96].SubGroups[1] = new SubGroup(new UnicodeRange(0xA490, 0xA4CF), "Yi Radicals", "zh", UnicodeChart.YiRadicals);

            scripts[97] = new Group(new UnicodeRange(0x13A0, 0x13FF), "American Scripts", "Cherokee", "us", UnicodeChart.Cherokee);
            scripts[98] = new Group(new UnicodeRange(0x10440, 0x1044F), "American Scripts", "Deseret", "us", UnicodeChart.Deseret);

            scripts[99] = new Group(new UnicodeRange(0x1400, 0x167F), "American Scripts", "Unified Canadian Aboriginal Syllabics", "ca", UnicodeChart.UnifiedCanadianAboriginalSyllabics);
            scripts[99].SubGroups = new SubGroup[1];
            scripts[99].SubGroups[0] = new SubGroup(new UnicodeRange(0x18B0, 0x18FF), "UCAS Extended", "ca", UnicodeChart.UnifiedCanadianAboriginalSyllabicsExtended);

            scripts[100] = new Group(new UnicodeRange(0xFB00, 0xFB4F), "Other", "Alphabetic Presentation Forms", "latin,he,hy", UnicodeChart.AlphabeticPresentationForms);
            scripts[101] = new Group(new UnicodeRange(0xFF00, 0xFFEF), "Other", "Halfwidth and Fullwidth Forms", "latin,ja", UnicodeChart.HalfwidthAndFullwidthForms);
            scripts[102] = new Group(new UnicodeRange(0x0000, 0x007F), "Other", "ASCII Characters", "latin", UnicodeChart.AsciiCharacters);
        }

        private static void InitializeSymbolsAndPunctuation()
        {
            symbolsAndPunctuation[0] = new Group(new UnicodeRange(0x2000, 0x206F), "Punctuation", "General Punctuation", "any", UnicodeChart.GeneralPunctuation);
            symbolsAndPunctuation[0].SubGroups = new SubGroup[4];
            symbolsAndPunctuation[0].SubGroups[0] = new SubGroup(new UnicodeRange(0x0000, 0x007F), "ASCII Punctuation", "latin", UnicodeChart.AsciiPunctuation);
            symbolsAndPunctuation[0].SubGroups[1] = new SubGroup(new UnicodeRange(0x0080, 0x00FF), "Latin-1 Punctuation", "latin", UnicodeChart.Latin1Punctuation);
            symbolsAndPunctuation[0].SubGroups[2] = new SubGroup(new UnicodeRange(0xFE50, 0xFE6F), "Small Form Variants", "any", UnicodeChart.SmallFormVariants);
            symbolsAndPunctuation[0].SubGroups[3] = new SubGroup(new UnicodeRange(0x2E00, 0x2E7F), "Supplemental Punctuation", "other", UnicodeChart.SupplementalPunctuation);

            symbolsAndPunctuation[1] = new Group(new UnicodeRange(0x3000, 0x303F), "Punctuation", "CJK Symbols and Punctuation", "zh,ja,ko", UnicodeChart.CjkSymbolsAndPunctuation);
            symbolsAndPunctuation[1].SubGroups = new SubGroup[3]; 
            symbolsAndPunctuation[1].SubGroups[0] = new SubGroup(new UnicodeRange(0xFE30, 0xFE4F), "CJK Compatibility Forms", "zh,ja,ko", UnicodeChart.CjkCompatibilityForms);
            symbolsAndPunctuation[1].SubGroups[1] = new SubGroup(new UnicodeRange(0xFF00, 0xFFEF), "Fullwidth ASCII Punctuation", "zh,ja,ko", UnicodeChart.FullwidthAsciiPunctuation);
            symbolsAndPunctuation[1].SubGroups[2] = new SubGroup(new UnicodeRange(0xFE10, 0xFE1F), "Vertical Forms", "zh,ja,ko", UnicodeChart.VerticalForms);

            symbolsAndPunctuation[2] = new Group(new UnicodeRange(0x2100, 0x214F), "Alphanumeric Symbols", "Letterlike Symbols", "any", UnicodeChart.LetterlikeSymbols);
            symbolsAndPunctuation[2].SubGroups = new SubGroup[1];
            symbolsAndPunctuation[2].SubGroups[0] = new SubGroup(new UnicodeRange(0x10190, 0x101CF), "Roman Symbols", "latin", UnicodeChart.RomanSymbols);

            symbolsAndPunctuation[3] = new Group(new UnicodeRange(0x1D400, 0x1D7FF), "Alphanumeric Symbols", "Mathematical Alphanumeric Symbols", "any", UnicodeChart.MathematicalAlphanumericSymbols);

            symbolsAndPunctuation[4] = new Group(new UnicodeRange(0x2460, 0x124FF), "Alphanumeric Symbols", "Enclosed Alphanumerics", "any", UnicodeChart.EnclosedAlphanumerics);
            symbolsAndPunctuation[4].SubGroups = new SubGroup[1]; 
            symbolsAndPunctuation[4].SubGroups[0] = new SubGroup(new UnicodeRange(0x1F100, 0x1F1FF), "Enclosed Alphanumerics Supplement", "any", UnicodeChart.EnclosedAlphanumericSupplement);

            symbolsAndPunctuation[5] = new Group(new UnicodeRange(0x3200, 0x32FF), "Alphanumeric Symbols", "Enclosed CJK Letters and Months", "zh,ja,ko", UnicodeChart.EnclosedCjkLettersAndMonths);
            symbolsAndPunctuation[5].SubGroups = new SubGroup[1]; 
            symbolsAndPunctuation[5].SubGroups[0] = new SubGroup(new UnicodeRange(0x1F200, 0x1F2FF), "Enclosed Ideographic Supplement", "zh,ja,ko", UnicodeChart.EnclosedIdeographicSupplement);

            symbolsAndPunctuation[6] = new Group(new UnicodeRange(0x3300, 0x33FF), "Alphanumeric Symbols", "CJK Compatibility", "zh,ja,ko", UnicodeChart.CjkCompatibility);
            symbolsAndPunctuation[6].SubGroups = new SubGroup[1]; 
            symbolsAndPunctuation[6].SubGroups[0] = new SubGroup(new UnicodeRange(0x2100, 0x214F), "Additional Squared Symbols", "zh,ja,ko", UnicodeChart.AdditionalSquaredSymbols);

            symbolsAndPunctuation[7] = new Group(new UnicodeRange(0x2300, 0x23FF), "Technical Symbols", "APL symbols", "any", UnicodeChart.AplSymbols);
            symbolsAndPunctuation[8] = new Group(new UnicodeRange(0x2400, 0x243F), "Technical Symbols", "Control Pictures", "any", UnicodeChart.ControlPictures);
            symbolsAndPunctuation[9] = new Group(new UnicodeRange(0x2300, 0x23FF), "Technical Symbols", "Miscellaneous  Technical", "any", UnicodeChart.MiscellaneousTechnical);
            symbolsAndPunctuation[10] = new Group(new UnicodeRange(0x2440, 0x245F), "Technical Symbols", "Optical Character Recognition (OCR)", "any", UnicodeChart.OpticalCharacterRecognition);
            symbolsAndPunctuation[11] = new Group(new UnicodeRange(0x20D0, 0x20FF), "Combining Diacritics", "Combining Diacritical Marks for Symbols", "other", UnicodeChart.CombiningDiacriticalMarksForSymbols);
            
            symbolsAndPunctuation[12] = new Group(new UnicodeRange(0x10100, 0x1013F), "Numbers and Digits", "Aegean", "el", UnicodeChart.AegeanNumbers);
            symbolsAndPunctuation[13] = new Group(new UnicodeRange(0x10140, 0x1018F), "Numbers and Digits", "Ancient Greek Numbers", "el", UnicodeChart.AncientGreekNumbers);

            symbolsAndPunctuation[14] = new Group(new UnicodeRange(0x0000, 0x007F), "Numbers and Digits", "ASCII Digits", "latin", UnicodeChart.AsciiDigits);
            symbolsAndPunctuation[14].SubGroups = new SubGroup[1]; 
            symbolsAndPunctuation[14].SubGroups[0] = new SubGroup(new UnicodeRange(0xFF00, 0xFFEF), "Fullwidth ASCII Digits", "latin,ja", UnicodeChart.FullwidthAsciiDigits);

            symbolsAndPunctuation[15] = new Group(new UnicodeRange(0xA830, 0xA83F), "Numbers and Digits", "Common Indic Number Forms", "hi", UnicodeChart.CommonIndicNumberForms);
            symbolsAndPunctuation[16] = new Group(new UnicodeRange(0x1D360, 0x1D37F), "Numbers and Digits", "Counting Rod Numerals", "other", UnicodeChart.CountingRodNumerals);
            symbolsAndPunctuation[17] = new Group(new UnicodeRange(0x12400, 0x1247F), "Numbers and Digits", "Cuneiform Numbers and Punctuation", "cuneiform", UnicodeChart.CuneiformNumbersAndPunctuation);
            symbolsAndPunctuation[18] = new Group(new UnicodeRange(0x2150, 0x218F), "Numbers and Digits", "Number Forms", "latin", UnicodeChart.NumberForms);
            symbolsAndPunctuation[19] = new Group(new UnicodeRange(0x10E60, 0x10E7F), "Numbers and Digits", "Rumi Numeral Symbols", "rumi", UnicodeChart.RumiNumeralSymbols);
            symbolsAndPunctuation[20] = new Group(new UnicodeRange(0x2070, 0x209F), "Numbers and Digits", "Super and Subscripts", "any", UnicodeChart. SuperAndSubscripts);

            symbolsAndPunctuation[21] = new Group(new UnicodeRange(0x2190, 0x21FF), "Mathematical Symbols", "Arrows", "latin", UnicodeChart.Arrows);
            symbolsAndPunctuation[21].SubGroups = new SubGroup[3]; 
            symbolsAndPunctuation[21].SubGroups[0] = new SubGroup(new UnicodeRange(0x27F0, 0x27FF), "Supplemental Arrows-A", "latin", UnicodeChart.SupplementalArrowsA);
            symbolsAndPunctuation[21].SubGroups[1] = new SubGroup(new UnicodeRange(0x2900, 0x297F), "Supplemental Arrows-B", "latin", UnicodeChart.SupplementalArrowsB);
            symbolsAndPunctuation[21].SubGroups[2] = new SubGroup(new UnicodeRange(0x2B00, 0x2BFF), "Additional Arrows", "latin", UnicodeChart.AdditionalArrows);

            symbolsAndPunctuation[22] = new Group(new UnicodeRange(0x1D400, 0x1D7FF), "Mathematical Symbols", "Mathematical Alphanumeric Symbols", "latin", UnicodeChart.MathematicalAlphanumericSymbols);
            symbolsAndPunctuation[22].SubGroups = new SubGroup[1]; 
            symbolsAndPunctuation[22].SubGroups[0] = new SubGroup(new UnicodeRange(0x2100, 0x214F), "Letterlike Symbols", "latin", UnicodeChart.LetterlikeSymbols);

            symbolsAndPunctuation[23] = new Group(new UnicodeRange(0x2200, 0x22FF), "Mathematical Symbols", "Mathematical Operators", "any", UnicodeChart.MathematicalOperators);
            symbolsAndPunctuation[23].SubGroups = new SubGroup[6];
            symbolsAndPunctuation[23].SubGroups[0] = new SubGroup(new UnicodeRange(0x0000, 0x007F), "Basic operators: Plus, Factorial,", "any", UnicodeChart.BasicOperatorsPlusFactorial);
            symbolsAndPunctuation[23].SubGroups[1] = new SubGroup(new UnicodeRange(0x0080, 0x00FF), "Division, Multiplication", "any", UnicodeChart.BasicOperatorsDivisionMultiplication);
            symbolsAndPunctuation[23].SubGroups[2] = new SubGroup(new UnicodeRange(0x2A00, 0x2AFF), "Supplemental Mathematical Operators", "any", UnicodeChart.SupplementalMathematicalOperators);
            symbolsAndPunctuation[23].SubGroups[3] = new SubGroup(new UnicodeRange(0x27C0, 0x27EF), "Miscellaneous Mathematical Symbols-A", "any", UnicodeChart.MiscellaneousMathematicalSymbolsA);
            symbolsAndPunctuation[23].SubGroups[4] = new SubGroup(new UnicodeRange(0x2980, 0x29FF), "Miscellaneous Mathematical Symbols-B", "any", UnicodeChart.MiscellaneousMathematicalSymbolsB);
            symbolsAndPunctuation[23].SubGroups[5] = new SubGroup(new UnicodeRange(0x2300, 0x23FF), "Floors and Ceilings", "any", UnicodeChart.FloorsAndCeilings);

            symbolsAndPunctuation[24] = new Group(new UnicodeRange(0x25A0, 0x25FF), "Mathematical Symbols", "Geometric Shapes", "any", UnicodeChart.GeometricShapes);
            symbolsAndPunctuation[24].SubGroups = new SubGroup[3]; 
            symbolsAndPunctuation[24].SubGroups[0] = new SubGroup(new UnicodeRange(0x2B00, 0x2BFF), "Additional Shapes", "any", UnicodeChart.AdditionalShapes);
            symbolsAndPunctuation[24].SubGroups[1] = new SubGroup(new UnicodeRange(0x2500, 0x257F), "Box Drawing", "any", UnicodeChart.BoxDrawing);
            symbolsAndPunctuation[24].SubGroups[2] = new SubGroup(new UnicodeRange(0x2580, 0x259F), "Block Elements", "any", UnicodeChart.BlockElements);

            symbolsAndPunctuation[25] = new Group(new UnicodeRange(0x10190, 0x101CF), "Other Symbols", "Ancient Symbols", "other", UnicodeChart.AncientSymbols);
            symbolsAndPunctuation[26] = new Group(new UnicodeRange(0x2800, 0x28FF), "Other Symbols", "Braille Patterns", "other", UnicodeChart.BraillePatterns);

            symbolsAndPunctuation[27] = new Group(new UnicodeRange(0x20A0, 0x20CF), "Other Symbols", "Currency Symbols", "any", UnicodeChart.CurrencySymbols);
            symbolsAndPunctuation[27].SubGroups = new SubGroup[7]; 
            symbolsAndPunctuation[27].SubGroups[0] = new SubGroup(new UnicodeRange(0x0000, 0x007F), "Dollar Sign", "any", UnicodeChart.DollarSign);
            symbolsAndPunctuation[27].SubGroups[1] = new SubGroup(new UnicodeRange(0x20A0, 0x20CF), "Euro Sign", "any", UnicodeChart.EuroSign);
            symbolsAndPunctuation[27].SubGroups[2] = new SubGroup(new UnicodeRange(0x0080, 0x00FF), "Yen, Pound and Cent", "any", UnicodeChart.YenPoundAndCent);
            symbolsAndPunctuation[27].SubGroups[3] = new SubGroup(new UnicodeRange(0xFF00, 0xFFEF), "Fullwidth Currency Symbols", "any", UnicodeChart.FullwidthCurrencySymbols);
            symbolsAndPunctuation[27].SubGroups[4] = new SubGroup(new UnicodeRange(0x2100, 0x214F), "Mark", "de", UnicodeChart.Mark);
            symbolsAndPunctuation[27].SubGroups[5] = new SubGroup(new UnicodeRange(0x20A0, 0x20CF), "Pfennig", "de", UnicodeChart.Pfennig);
            symbolsAndPunctuation[27].SubGroups[6] = new SubGroup(new UnicodeRange(0xFB50, 0xFDFF), "Rial Sign", "iran", UnicodeChart.RialSign);

            symbolsAndPunctuation[28] = new Group(new UnicodeRange(0x2700, 0x27BF), "Other Symbols", "Dingbats", "any", UnicodeChart.Dingbats);

            // Game Symbols range is not defined. Use checker/chess range.
            symbolsAndPunctuation[29] = new Group(new UnicodeRange(0x2600, 0x26FF), "Other Symbols", "Game Symbols", "other", UnicodeChart.GameSymbols);
            symbolsAndPunctuation[29].SubGroups = new SubGroup[5]; 
            symbolsAndPunctuation[29].SubGroups[0] = new SubGroup(new UnicodeRange(0x2600, 0x26FF), "Chess//Checkers", "other", UnicodeChart.ChessCheckers);
            symbolsAndPunctuation[29].SubGroups[1] = new SubGroup(new UnicodeRange(0x1F030, 0x1F09F), "Domino Tiles", "other", UnicodeChart.DominoTiles);
            symbolsAndPunctuation[29].SubGroups[2] = new SubGroup(new UnicodeRange(0x2600, 0x26FF), "Japanese Chess", "ja", UnicodeChart.JapaneseChess);
            symbolsAndPunctuation[29].SubGroups[3] = new SubGroup(new UnicodeRange(0x1F000, 0x1F02F), "Mahjong Tiles", "ja,zh", UnicodeChart.MahjongTiles);
            symbolsAndPunctuation[29].SubGroups[4] = new SubGroup(new UnicodeRange(0x2600, 0x26FF), "Card suits", "other", UnicodeChart.CardSuits);

            symbolsAndPunctuation[30] = new Group(new UnicodeRange(0x2600, 0x26FF), "Other Symbols", "Miscellaneous Symbols", "any", UnicodeChart.MiscellaneousSymbols);
            symbolsAndPunctuation[30].SubGroups = new SubGroup[1]; 
            symbolsAndPunctuation[30].SubGroups[0] = new SubGroup(new UnicodeRange(0x2B00, 0x2BFF), "Miscellaneous Symbols and Arrows", "any", UnicodeChart.MiscellaneousSymbolsAndArrows);

            symbolsAndPunctuation[31] = new Group(new UnicodeRange(0x1D100, 0x1D1FF), "Other Symbols", "Musical Symbols", "any", UnicodeChart.MusicalSymbols);
            symbolsAndPunctuation[31].SubGroups = new SubGroup[2]; 
            symbolsAndPunctuation[31].SubGroups[0] = new SubGroup(new UnicodeRange(0x1D200, 0x1D24F), "Ancient Greek Musical Notation", "el", UnicodeChart.AncientGreekMusicalNotation);
            symbolsAndPunctuation[31].SubGroups[1] = new SubGroup(new UnicodeRange(0x1D000, 0x1D0FF), "Byzantine Musical Symbols", "other", UnicodeChart.ByzantineMusicalSymbols);

            // Yijing Symbols is not defined. Use Yijing Mono-, Di- and Trigrams range.
            symbolsAndPunctuation[32] = new Group(new UnicodeRange(0x2600, 0x26FF), "Other Symbols", "Yijing Symbols", "zh", UnicodeChart.YijingSymbols);
            symbolsAndPunctuation[32].SubGroups = new SubGroup[3]; 
            symbolsAndPunctuation[32].SubGroups[0] = new SubGroup(new UnicodeRange(0x2600, 0x26FF), "Yijing Mono-, Di- and Trigrams", "zh", UnicodeChart.YijingMonoDiAndTrigrams);
            symbolsAndPunctuation[32].SubGroups[1] = new SubGroup(new UnicodeRange(0x4DC0, 0x4DFF), "Yijing Hexagram Symbols", "zh", UnicodeChart.YijingHexagramSymbols);
            symbolsAndPunctuation[32].SubGroups[2] = new SubGroup(new UnicodeRange(0x1D300, 0x1D35F), "Tai Xuan Jing Symbols", "zh", UnicodeChart.TaiXuanJingSymbols);

            // Controls is not defined. Use C0 and C1 range.
            symbolsAndPunctuation[33] = new Group(new UnicodeRange(0x0000, 0x00FF), "Specials", "Controls", "any", UnicodeChart.Controls);
            symbolsAndPunctuation[33].SubGroups = new SubGroup[4]; 
            symbolsAndPunctuation[33].SubGroups[0] = new SubGroup(new UnicodeRange(0x0000, 0x007F), "C0", "any", UnicodeChart.C0);
            symbolsAndPunctuation[33].SubGroups[1] = new SubGroup(new UnicodeRange(0x0080, 0x00FF), "C1", "any", UnicodeChart.C1);
            symbolsAndPunctuation[33].SubGroups[2] = new SubGroup(new UnicodeRange(0x2000, 0x206F), "Layout Controls", "any", UnicodeChart.LayoutControls);
            symbolsAndPunctuation[33].SubGroups[3] = new SubGroup(new UnicodeRange(0x2000, 0x206F), "Invisible Operators", "any", UnicodeChart.InvisibleOperators);

            symbolsAndPunctuation[34] = new Group(new UnicodeRange(0xFFF0, 0xFFFF), "Specials", "Specials", "any", UnicodeChart.Specials);
            symbolsAndPunctuation[35] = new Group(new UnicodeRange(0xE0000, 0xE007F), "Specials", "Tags", "any", UnicodeChart.Tags);

            symbolsAndPunctuation[36] = new Group(new UnicodeRange(0xFE00, 0xFE0F), "Specials", "Variation Selectors", "any", UnicodeChart.VariationSelectors);
            symbolsAndPunctuation[36].SubGroups = new SubGroup[1]; 
            symbolsAndPunctuation[36].SubGroups[0] = new SubGroup(new UnicodeRange(0xE0100, 0xE01EF), "Variation Selectors Supplement", "any", UnicodeChart.VariationSelectorsSupplement);

            symbolsAndPunctuation[37] = new Group(new UnicodeRange(0xE000, 0xF8FF), "Private Use", "Private Use Area", "any", UnicodeChart.PrivateUseArea);
            symbolsAndPunctuation[38] = new Group(new UnicodeRange(0xF0000, 0xFFFFD), "Private Use", "Supplementary Private Use Area-A", "any", UnicodeChart.SupplementaryPrivateUseAreaA);
            symbolsAndPunctuation[39] = new Group(new UnicodeRange(0x100000, 0x10FFFD), "Private Use", "Supplementary Private Use Area-B", "any", UnicodeChart.SupplementaryPrivateUseAreaB);
            
            symbolsAndPunctuation[40] = new Group(new UnicodeRange(0xD800, 0xDBFF), "Surrogates", "High Surrogates", "zh", UnicodeChart.HighSurrogates);
            symbolsAndPunctuation[41] = new Group(new UnicodeRange(0xDC00, 0xDFFF), "Surrogates", "Low Surrogates", "zh", UnicodeChart.LowSurrogates);
            
            symbolsAndPunctuation[42] = new Group(new UnicodeRange(0xFB50, 0xFDFF), "Noncharacters in UnicodeCharts", "Reserved range", "any", UnicodeChart.ReservedRange);

            // at end of... is not defined. Use BMP range.
            symbolsAndPunctuation[43] = new Group(new UnicodeRange(0xFFF0, 0xFFFF), "Noncharacters in UnicodeCharts", "at end of...", "any", UnicodeChart.AtEndOf);
            symbolsAndPunctuation[43].SubGroups = new SubGroup[17]; 
            symbolsAndPunctuation[43].SubGroups[0] = new SubGroup(new UnicodeRange(0xFFF0, 0xFFFF), "BMP", "any", UnicodeChart.Bmp);
            symbolsAndPunctuation[43].SubGroups[1] = new SubGroup(new UnicodeRange(0x1FF80, 0x1FFFF), "Plane 1", "any", UnicodeChart.Plane1);
            symbolsAndPunctuation[43].SubGroups[2] = new SubGroup(new UnicodeRange(0x2FF80, 0x2FFFF), "Plane 2", "any", UnicodeChart.Plane2);
            symbolsAndPunctuation[43].SubGroups[3] = new SubGroup(new UnicodeRange(0x3FF80, 0x3FFFF), "Plane 3", "any", UnicodeChart.Plane3);
            symbolsAndPunctuation[43].SubGroups[4] = new SubGroup(new UnicodeRange(0x4FF80, 0x4FFFF), "Plane 4", "any", UnicodeChart.Plane4);
            symbolsAndPunctuation[43].SubGroups[5] = new SubGroup(new UnicodeRange(0x5FF80, 0x5FFFF), "Plane 5", "any", UnicodeChart.Plane5);
            symbolsAndPunctuation[43].SubGroups[6] = new SubGroup(new UnicodeRange(0x6FF80, 0x6FFFF), "Plane 6", "any", UnicodeChart.Plane6);
            symbolsAndPunctuation[43].SubGroups[7] = new SubGroup(new UnicodeRange(0x7FF80, 0x7FFFF), "Plane 7", "any", UnicodeChart.Plane7);
            symbolsAndPunctuation[43].SubGroups[8] = new SubGroup(new UnicodeRange(0x8FF80, 0x8FFFF), "Plane 8", "any", UnicodeChart.Plane8);
            symbolsAndPunctuation[43].SubGroups[9] = new SubGroup(new UnicodeRange(0x9FF80, 0x9FFFF), "Plane 9", "any", UnicodeChart.Plane9);
            symbolsAndPunctuation[43].SubGroups[10] = new SubGroup(new UnicodeRange(0xAFF80, 0xAFFFF), "Plane 10", "any", UnicodeChart.Plane10);
            symbolsAndPunctuation[43].SubGroups[11] = new SubGroup(new UnicodeRange(0xBFF80, 0xBFFFF), "Plane 11", "any", UnicodeChart.Plane11);
            symbolsAndPunctuation[43].SubGroups[12] = new SubGroup(new UnicodeRange(0xCFF80, 0xCFFFF), "Plane 12", "any", UnicodeChart.Plane12);
            symbolsAndPunctuation[43].SubGroups[13] = new SubGroup(new UnicodeRange(0xdFF80, 0xDFFFF), "Plane 13", "any", UnicodeChart.Plane13);
            symbolsAndPunctuation[43].SubGroups[14] = new SubGroup(new UnicodeRange(0xEFF80, 0xEFFFF), "Plane 14", "any", UnicodeChart.Plane14);
            symbolsAndPunctuation[43].SubGroups[15] = new SubGroup(new UnicodeRange(0xFFF80, 0xFFFFF), "Plane 15", "any", UnicodeChart.Plane15);
            symbolsAndPunctuation[43].SubGroups[16] = new SubGroup(new UnicodeRange(0x10FF80, 0x10FFFF), "Plane 16", "any", UnicodeChart.Plane16);
        }
    }
}

