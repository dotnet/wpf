// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Text;

namespace Microsoft.Test.Text
{
    /// <summary>
    /// TextNormalization property
    /// </summary>
    internal class TextNormalizationProperty : IStringProperty
    {
        /// <summary>
        /// Dictionary to store code points corresponding to culture.
        /// </summary>
        private Dictionary<string, int[]> textNormalizationPropertyDictionary = new Dictionary<string, int[]>();

        private List<UnicodeRangeProperty> textNormalizationRangeList = new List<UnicodeRangeProperty>(); 

        private int [] codePointsWithDifferentNormalizationForms;

        /// <summary>
        /// Define minimum code point needed to be a text normalization string
        /// </summary>
        public static readonly int MINNUMOFCODEPOINT = 1;

        /// <summary>
        /// Define SurrogatePairDictionary class
        /// <a href="http://www.unicode.org/reports/tr15/">Newline</a>
        /// <a href="http://www.unicode.org/charts/normalization/">Newline</a>
        /// </summary>
        public TextNormalizationProperty(UnicodeRangeDatabase unicodeDb, Collection<UnicodeRange> expectedRanges)
        {
            bool isValid = false;

            foreach (UnicodeRange range in expectedRanges)
            {
                if (RangePropertyCollector.BuildPropertyDataList(
                    unicodeDb,
                    range,
                    textNormalizationRangeList,
                    "Latin",
                    GroupAttributes.Name))
                {
                    isValid = true;
                }
                if (RangePropertyCollector.BuildPropertyDataList(
                    unicodeDb,
                    range,
                    textNormalizationRangeList,
                    "CJK Unified Ideographs (Han)",
                    GroupAttributes.Name))
                {
                    isValid = true;
                }
                if (RangePropertyCollector.BuildPropertyDataList(
                    unicodeDb,
                    range,
                    textNormalizationRangeList,
                    "CJK Compatibility Ideographs",
                    GroupAttributes.Name))
                {
                    isValid = true;
                }
                if (RangePropertyCollector.BuildPropertyDataList(
                    unicodeDb,
                    range,
                    textNormalizationRangeList,
                    "Katakana",
                    GroupAttributes.Name))
                {
                    isValid = true;
                }
                if (RangePropertyCollector.BuildPropertyDataList(
                    unicodeDb,
                    range,
                    textNormalizationRangeList,
                    "Hangul Jamo",
                    GroupAttributes.Name))
                {
                    isValid = true;
                }
                if (RangePropertyCollector.BuildPropertyDataList(
                    unicodeDb,
                    range,
                    textNormalizationRangeList,
                    "Hangul Syllables",
                    GroupAttributes.Name))
                {
                    isValid = true;
                }
                if (RangePropertyCollector.BuildPropertyDataList(
                    unicodeDb, 
                    range,
                    textNormalizationRangeList,
                    "Arabic",
                    GroupAttributes.Name))
                {
                    isValid = true;
                }
                if (RangePropertyCollector.BuildPropertyDataList(
                    unicodeDb,
                    range,
                    textNormalizationRangeList,
                    "Greek",
                    GroupAttributes.Name))
                {
                    isValid = true;
                }
            }

            
            if (InitializeTextNormalizationPropertyDictionary(expectedRanges))
            {
                isValid = true;
            }
            
            if (!isValid)
            {
                throw new ArgumentOutOfRangeException("expectedRanges", "TextNormalizationProperty, " + 
                    "code points for text normalization ranges are beyond expected range. " + "Refert to Latin,  CJK Unified Ideographs (Han) " +
                    "CJK Compatibility Ideographs, Katakana, Hangul Jamo, Hangul Syllables, Arabic, and  Greek ranges.");
            }
        }

        /// <summary>
        /// Dictionary to store code points corresponding to culture.
        /// </summary>
        private bool InitializeTextNormalizationPropertyDictionary(Collection<UnicodeRange> expectedRanges)
        {
            int [] othersymbols = {0xFFE4, 0x21CD, 0xFFE8, 0xFFED, 0xFFEE, 0x3036, 0x1D15E, 0x1D15F, 0x1D160, 0x1D161, 0x1D162,
                0x1D163, 0x1D164, 0x1D1BB, 0x1D1BD, 0x1D1BF, 0x1D1BC, 0x1D1BE, 0x1D1C0};
            textNormalizationPropertyDictionary.Add("othersymbols", othersymbols);

            int [] modifiersymbols = {0x00B4, 0x0384, 0x1FFD, 0x02DC, 0x00AF, 0xFFE3, 0x02D8, 0x02D9, 0x00A8, 0x1FED, 0x0385, 
                0x1FEE, 0x1FC1, 0x02DA, 0x02DD, 0x1FBD, 0x1FBF, 0x1FCD, 0x1FCE, 0x1FCF, 0x1FFE, 0x1FDD, 0x1FDE, 0x1FDF, 0x00B8,
                0x02DB, 0x1FC0, 0x309B, 0x309C, 0xFF3E, 0x1FEF, 0xFF40};
            textNormalizationPropertyDictionary.Add("modifiersymbols", modifiersymbols);

            int [] currencysymbols = {0xFE69, 0xFF04, 0xFFE0, 0xFFE1, 0xFFE5, 0xFFE6};
            textNormalizationPropertyDictionary.Add("currencysymbols", currencysymbols);

            int [] mathsymbols = {0x207A, 0x208A, 0xFB29, 0xFE62, 0xFF0B, 0x2A74, 0xFE64, 0xFF1C, 0x226E, 0x207C, 0x208C, 0xFE66,
                0xFF1D, 0x2A75, 0x2A76, 0x2260, 0xFE65, 0xFF1E, 0x226F, 0xFF5C, 0xFF5E, 0xFFE2, 0xFFE9, 0x219A, 0xFFEA, 0xFFEB,
                0x219B, 0xFFEC, 0x21AE, 0x21CF, 0x21CE, 0x1D6DB, 0x1D715, 0x1D74F, 0x1D789, 0x1D7C3, 0x2204, 0x1D6C1, 0x1D6FB,
                0x1D735, 0x1D76F, 0x1D7A9, 0x2209, 0x220C, 0x2140, 0x207B, 0x208B, 0x2224, 0x2226, 0x222C, 0x222D, 0x2A0C, 0x222F,
                0x2230, 0x2241, 0x2244, 0x2247, 0x2249, 0x226D, 0x2262, 0x2270, 0x2271, 0x2274, 0x2275, 0x2278, 0x2279, 0x2280, 
                0x2281, 0x22E0, 0x22E1, 0x2284, 0x2285, 0x2288, 0x2289, 0x22E2, 0x22E3, 0x22AC, 0x22AD, 0x22AE, 0x22AF, 0x22EA,
                0x22EB, 0x22EC, 0x22ED, 0x2ADC};
            textNormalizationPropertyDictionary.Add("mathsymbols", mathsymbols);

            int [] modifierletter = {0x037A, 0x0374, 0xFF9E, 0xFF9F, 0xFF70};
            textNormalizationPropertyDictionary.Add("modifierletter", modifierletter);

            int [] otherletter  = {0xFE70, 0xFE72, 0xFC5E, 0xFE74, 0xFC5F, 0xFE76, 0xFC60, 0xFE78, 0xFC61, 0xFE7A, 0xFC62, 0xFE7C,
                0xFC63, 0xFE7E, 0xFE71, 0xFE77, 0xFCF2, 0xFE79, 0xFCF3, 0xFE7B, 0xFCF4, 0xFE7D, 0xFE7F};
            textNormalizationPropertyDictionary.Add("otherletter", otherletter);

            int [] nonspacingmark  = {0x0340, 0x0341, 0x0344, 0x0343};
            textNormalizationPropertyDictionary.Add("nonspacingmark", nonspacingmark);

            int [] spaceseparator   = {0x00A0, 0x2000, 0x2001, 0x2002, 0x2003, 0x2004, 0x2005, 0x2006, 0x2007, 0x2008, 0x2009, 0x200A,
                0x202F, 0x205F, 0x3000};
            textNormalizationPropertyDictionary.Add("spaceseparator", spaceseparator);

            int [] decimalnumber = {0xFF10, 0x1D7CE, 0x1D7D8, 0x1D7E2, 0x1D7EC, 0x1D7F6, 0xFF11, 0x1D7CF, 0x1D7D9, 0x1D7E3, 0x1D7ED, 
                0x200A, 0x1D7F7, 0xFF12, 0x1D7D0, 0x1D7DA, 0x1D7E4, 0x1D7EE, 0x1D7F8, 0xFF13, 0x1D7D1, 0x1D7DB, 0x1D7E5, 0x1D7EF,
                0x1D7F9,0xFF14, 0x1D7D2, 0x1D7DC, 0x1D7E6, 0x1D7F0, 0x1D7FA, 0xFF15, 0x1D7D3, 0x1D7DD, 0x1D7E7, 0x1D7F1, 0x1D7FB,
                0xFF16, 0x1D7D4, 0x1D7DE, 0x1D7E8, 0x1D7F2, 0x1D7FC, 0xFF17, 0x1D7D5, 0x1D7DF, 0x1D7E9, 0x1D7F3, 0x1D7FD, 0xFF18,
                0x1D7D6, 0x1D7E0, 0x1D7EA, 0x1D7F4, 0x1D7FE, 0xFF19, 0x1D7D7, 0x1D7E1, 0x1D7EB, 0x1D7F5, 0x1D7FF};
            textNormalizationPropertyDictionary.Add("decimalnumber", decimalnumber);

            int [] othernumber = {0x2474, 0x247D, 0x247E, 0x247F, 0x2480, 0x2481, 0x2482, 0x2483, 0x2484, 0x2485, 0x2486, 0x2475, 0x2487, 0x2476, 
                0x2477, 0x2478, 0x2479, 0x247A, 0x247B, 0x247C, 0x2070, 0x2080, 0x24EA, 0x1F101, 0x1F100, 0x2189,0x00B9, 0x2081, 0x2460, 0x1F102,
                0x2488, 0x2469, 0x2491, 0x246A, 0x2492, 0x246B, 0x2493, 0x246C, 0x2494, 0x246D, 0x2495, 0x246E, 0x2496, 0x246F, 0x2497, 0x2470, 
                0x2498, 0x2499, 0x2472, 0x249A, 0x215F, 0x2152, 0x00BD, 0x2153, 0x00BC, 0x2155, 0x2159, 0x2150, 0x215B, 0x2151, 0x00B2, 0x2082, 
                0x2461, 0x1F103, 0x2489, 0x2473, 0x249B, 0x3251, 0x3252, 0x3253, 0x3254, 0x3255, 0x3256, 0x3257, 0x3258, 0x3259, 0x2154, 0x2156,
                0x00B3, 0x2083, 0x2462, 0x1F104, 0x248A, 0x325A, 0x325B, 0x325C, 0x325D, 0x325E, 0x325F, 0x32B1, 0x32B2, 0x32B3, 0x32B4, 0x00BE, 
                0x2157, 0x215C, 0x2074, 0x2084, 0x2463, 0x1F105, 0x248B, 0x32B5, 0x32B6, 0x32B7, 0x32B8, 0x32B9, 0x32BA, 0x32BB, 0x32BC, 0x32BD, 
                0x32BE, 0x2158, 0x2075, 0x2085, 0x2464, 0x1F106, 0x248C, 0x32BF, 0x215A, 0x215D, 0x2076, 0x2086, 0x2465, 0x1F107, 0x248D, 0x2077,
                0x2087, 0x2466, 0x1F108, 0x248E, 0x215E, 0x2078, 0x2088, 0x2467, 0x1F109, 0x248F, 0x2079, 0x2089, 0x2468, 0x1F10A, 0x2490};
            textNormalizationPropertyDictionary.Add("othernumber", othernumber);

             int [] kaithi = {0x1109A, 0x1109C, 0x110AB};
             textNormalizationPropertyDictionary.Add("kaithi", kaithi);

             int [] balinese = {0x1B06, 0x1B08, 0x1B0A, 0x1B0C, 0x1B0E, 0x1B12, 0x1B3B, 0x1B3D, 0x1B40, 0x1B41, 0x1B43};
             textNormalizationPropertyDictionary.Add("balinese", balinese);

             int [] tifinagh = {0x2D6F};
             textNormalizationPropertyDictionary.Add("tifinagh", tifinagh);

             int [] hiragana = {0x3094, 0x304C, 0x304E, 0x3050, 0x3052, 0x3054, 0x3056, 0x3058, 0x305A, 0x305C, 0x305E, 0x3060, 0x3062, 0x3065,
                0x3067, 0x3069, 0x3070, 0x3071, 0x3073, 0x3074, 0x3076, 0x3077, 0x3079, 0x307A, 0x1F200, 0x307C, 0x307D, 0x309F, 0x309E};
             textNormalizationPropertyDictionary.Add("hiragana", hiragana);

             int [] georgian = {0x10FC};
             textNormalizationPropertyDictionary.Add("georgian", georgian);

             int [] myanmar = {0x1026};
             textNormalizationPropertyDictionary.Add("myanmar", myanmar);

             int [] tibetan = {0x0F0C, 0x0F69, 0x0F43, 0x0F4D, 0x0F52, 0x0F57, 0x0F5C, 0x0F73, 0x0F75, 0x0F81, 0x0FB9, 0x0F93, 0x0F9D, 0x0FA2, 0x0FA7, 
                0x0FAC, 0x0F77, 0x0F76, 0x0F79, 0x0F78};
             textNormalizationPropertyDictionary.Add("tibetan", tibetan);

             int [] lao = {0x0EDC, 0x0EDD, 0x0EB3};
             textNormalizationPropertyDictionary.Add("lao", lao);

             int [] th = {0x0E33};
             textNormalizationPropertyDictionary.Add("th", th);

             int [] sinhala = {0x0DDA, 0x0DDC, 0x0DDD, 0x0DDE};
             textNormalizationPropertyDictionary.Add("sinhala", sinhala);

             int [] malayalam = {0x0D4A, 0x0D4C, 0x0D4B};
             textNormalizationPropertyDictionary.Add("malayalam", malayalam);

             int [] kannada = {0x0CC0, 0x0CCA, 0x0CCB, 0x0CC7, 0x0CC8};
             textNormalizationPropertyDictionary.Add("kannada", kannada);

             int [] telugu = {0x0C48};
             textNormalizationPropertyDictionary.Add("telugu", telugu);

             int [] ta = {0x0B94, 0x0BCA, 0x0BCC, 0x0BCB};
             textNormalizationPropertyDictionary.Add("ta", ta);

             int [] oriya = {0x0B5C, 0x0B5D, 0x0B4B, 0x0B48, 0x0B4C};
             textNormalizationPropertyDictionary.Add("oriya", oriya);

             int [] gurmukhi = {0x0A59, 0x0A5A, 0x0A5B, 0x0A5E, 0x0A33, 0x0A36};
             textNormalizationPropertyDictionary.Add("gurmukhi", gurmukhi);

             int [] bengali = {0x09DC, 0x09DD, 0x09DF, 0x09CB, 0x09CC};
             textNormalizationPropertyDictionary.Add("bengali", bengali);

             int [] devanagari = {0x0958, 0x0959, 0x095A, 0x095B, 0x095C, 0x095D, 0x0929, 0x095E, 0x095E, 0x0931, 0x0934};
             textNormalizationPropertyDictionary.Add("devanagari", devanagari);

             int [] he = {0x2135, 0xFB21, 0xFB2E, 0xFB2F, 0xFB30, 0xFB4F, 0x2136, 0xFB31, 0xFB4C, 0x2137, 0xFB32, 0x2138, 0xFB22, 0xFB33, 0xFB23, 0xFB34,
                0xFB4B, 0xFB35, 0xFB36, 0xFB38, 0xFB1D, 0xFB39, 0xFB3A, 0xFB24, 0xFB3B, 0xFB4D, 0xFB25, 0xFB3C, 0xFB26, 0xFB3E, 0xFB40, 0xFB41, 0xFB20,
                0xFB43, 0xFB44, 0xFB4E, 0xFB46, 0xFB47, 0xFB27, 0xFB48, 0xFB49, 0xFB2C, 0xFB2D, 0xFB2D, 0xFB2B, 0xFB28, 0xFB4A, 0xFB1F};
             textNormalizationPropertyDictionary.Add("he", he);

             int [] hy = {0x0587, 0xFB14, 0xFB15, 0xFB17, 0xFB13, 0xFB16};
             textNormalizationPropertyDictionary.Add("hy", hy);

             int [] cyrillic = {0x04D0, 0x04D1, 0x04D2, 0x04D3, 0x0403, 0x0453, 0x0400, 0x0450, 0x04D6, 0x04D7, 0x0401, 0x0451, 0x04C1, 0x04C2, 0x04DC,
                0x04DD, 0x04DE, 0x04DF, 0x040D, 0x045D, 0x04E2, 0x04E3, 0x0419, 0x0439, 0x04E4, 0x04E5, 0x040C, 0x045C, 0x1D78, 0x04E6, 0x04E7, 0x04EE,
                0xFB20, 0x04EF, 0x040E, 0x045E, 0x04F0, 0x04F1, 0x04F2, 0x04F3, 0x04F4, 0x04F5, 0x04F6, 0x04F7, 0x04F8, 0x04F9, 0x04EC, 0x04ED, 0x0407,
                0x0457, 0x0476, 0x0477, 0x04DA, 0x04DB, 0x04EA, 0x04EB};
             textNormalizationPropertyDictionary.Add("cyrillic", cyrillic);

             int i = 0;
             bool isValid = false;
             codePointsWithDifferentNormalizationForms = new int [othersymbols.Length + modifiersymbols.Length + currencysymbols.Length + mathsymbols.Length +
                modifierletter.Length + otherletter.Length + nonspacingmark.Length + spaceseparator.Length + decimalnumber.Length + othernumber.Length + 
                kaithi.Length + balinese.Length + tifinagh.Length + hiragana.Length + georgian.Length + myanmar.Length + tibetan.Length + lao.Length +
                th.Length + sinhala.Length + malayalam.Length + kannada.Length + telugu.Length + ta.Length + oriya.Length  + gurmukhi.Length + bengali.Length
                + devanagari.Length + he.Length + hy.Length + cyrillic.Length];
             
             Dictionary<string, int[]>.ValueCollection valueColl = textNormalizationPropertyDictionary.Values;
             foreach (int [] values in valueColl)
             {
                foreach (int codePoint in values)
                {
                    foreach (UnicodeRange range in expectedRanges)
                    {
                         if (codePoint >= range.StartOfUnicodeRange && codePoint <= range.EndOfUnicodeRange)
                         {
                             codePointsWithDifferentNormalizationForms[i++] = codePoint;
                             isValid = true;
                         }
                    }
                }
             }
             Array.Resize(ref codePointsWithDifferentNormalizationForms, i);
             Array.Sort(codePointsWithDifferentNormalizationForms);
             return isValid;
        }

        /// <summary>
        /// Check if code point is in the property range
        /// </summary>
        public bool IsInPropertyRange(int codePoint)
        {
            bool isIn = false;
            foreach (UnicodeRangeProperty prop in textNormalizationRangeList)
            {
                if (codePoint >= prop.Range.StartOfUnicodeRange && codePoint <= prop.Range.EndOfUnicodeRange)
                {
                    isIn = true;
                    break;
                }
            }

            return isIn;
        }

        /// <summary>
        /// Get random normalizeable code points
        /// </summary>
        public string GetRandomCodePoints(int numOfProperty, int seed)
        {
            if (numOfProperty < 1)
            {
                throw new ArgumentOutOfRangeException(
                    "TextNormalizationProperty, numOfProperty, " + numOfProperty + " cannot be less than one.");
            }

            string textNormalizationStr = string.Empty;
            string numStr = string.Empty;
            Random rand = new Random(seed);
            for (int i= 0; i < numOfProperty; i++)
            {
                int index = rand.Next(0, codePointsWithDifferentNormalizationForms.Length);
                textNormalizationStr += TextUtil.IntToString(codePointsWithDifferentNormalizationForms[index]);
            }

            return textNormalizationStr;
        }
    }
}




