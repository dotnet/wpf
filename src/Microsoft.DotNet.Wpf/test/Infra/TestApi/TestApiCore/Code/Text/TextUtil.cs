// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Globalization;

namespace Microsoft.Test.Text
{
    /// <summary>
    /// A collection of const, enum, and struc declaration and static utility function
    /// </summary>
    internal static class TextUtil
    {
        /// <summary>
        /// Define number of scripts in Unicode
        /// <a href="http://www.unicode.org/charts/#scripts">Newline</a> 
        /// </summary>
        public static readonly int NUMOFSCRIPTS = 103;

        /// <summary>
        /// Define number of symbols and punctuation in Unicode
        /// <a href="http://www.unicode.org/charts/#symbols">Newline</a> 
        /// </summary>
        public static readonly int NUMOFSYMBOLSANDPUNCTUATION = 44;

        /// <summary>
        /// Maximum Unicode Point value
        /// <a href="http://www.unicode.org/reports/tr19/tr19-9.html">Newline</a> 
        /// </summary>
        public static readonly int MaxUnicodePoint = 0x10FFFF;

        /// <summary>
        /// Maximum number of code points defined for a string to be generated
        /// </summary>
        public static readonly int MAXNUMOFCODEPOINT = 300;

        /// <summary>
        /// Maximum number iteration used in while loop to guard from infinite loops.
        /// </summary>
        public static readonly int MAXNUMITERATION = 128;

        /// <summary>
        /// Defined ids to help identifying which country/region where the script or symbol is used. If id is defined in LCID,
        ///  LCID is used. Otherwise, spell the full name in lower case. '-' is used to omitted.
        /// <a href="http://www.unicode.org/charts/">Newline</a> 
        /// </summary>
        public enum CultureIds
        {
            /// <summary>
            /// Null - don't care
            /// </summary>
            Null = 0,

            /// <summary>
            /// Can be used in any content
            /// </summary>
            any,

            /// <summary>
            /// Arabic countries
            /// </summary>
            ar,

            /// <summary>
            /// Azerbaijani - Cyrillic
            /// </summary>
            azaz,

            /// <summary>
            /// Bangladesh
            /// </summary>
            bangladesh,

            /// <summary>
            /// Canada
            /// </summary>
            ca,

            /// <summary>
            /// Cambodia
            /// </summary>
            cambodia,

            /// <summary>
            /// Cameroon
            /// </summary>
            cameroon,

            /// <summary>
            /// Carians 
            /// </summary>
            carians,

            /// <summary>
            /// Cuneiform 
            /// </summary>
            cuneiform,

            /// <summary>
            /// Cyprus 
            /// </summary>
            cyprus,

            /// <summary>
            /// German
            /// </summary>
            de,

            /// <summary>
            /// Egypt
            /// </summary>
            eg,

            /// <summary>
            /// Greece
            /// </summary>
            el,

            /// <summary>
            /// English speaking countries
            /// </summary>
            en, 

            /// <summary>
            /// Ethiopia
            /// </summary>
            ethiopia,

            /// <summary>
            /// Georgia
            /// </summary>
            georgia,

            /// <summary>
            /// Glagolitsa 
            /// </summary>
            glagolitsa,

            /// <summary>
            /// Israel
            /// </summary>
            he,

            /// <summary>
            /// India
            /// </summary>
            hi,

            /// <summary>
            /// Armenia
            /// </summary>
            hy,

            /// <summary>
            /// Indonesia
            /// </summary>
            id,

            /// <summary>
            /// Ireland
            /// </summary>
            ie,

            /// <summary>
            /// Iran /Percian
            /// </summary>
            iran,

            /// <summary>
            /// Japan
            /// </summary>
            ja,

            /// <summary>
            /// Kharoshthi 
            /// </summary>
            kharoshthi,

            /// <summary>
            /// Korea
            /// </summary>
            ko,

            /// <summary>
            /// Lao
            /// </summary>
            lao,

            /// <summary>
            /// Latin
            /// </summary>
            latin,

            /// <summary>
            /// Lycian 
            /// </summary>
            lycia,

            /// <summary>
            /// Maldives
            /// </summary>
            maldives,

            /// <summary>
            /// Monglian
            /// </summary>
            mongolia,

            /// <summary>
            /// Myanmar
            /// </summary>
            myanmar,

            /// <summary>
            /// Nepal
            /// </summary>
            nepal,

            /// <summary>
            /// N'KO
            /// </summary>
            nko,

            /// <summary>
            /// Turkic ancient form
            /// </summary>
            oldturkic,

            /// <summary>
            /// Not classified
            /// </summary>
            other,

            /// <summary>
            /// Phillippines
            /// </summary>
            ph,

            /// <summary>
            /// Phaistos
            /// </summary>
            phaistosdisc,

            /// <summary>
            /// Phoenician 
            /// </summary>
            phoenicia,

            /// <summary>
            /// Samaritan 
            /// </summary>
            samaria,

            /// <summary>
            /// Singapore
            /// </summary>
            singapore,

            /// <summary>
            /// Somalia
            /// </summary>
            somalia,

            /// <summary>
            /// Sri Lanka
            /// </summary>
            srilanka,

            /// <summary>
            /// Serbian - Cyrillic
            /// </summary>
            srsp,

            /// <summary>
            /// Syloti
            /// </summary>
            sylotinagri,

            /// <summary>
            /// Syriac scripts
            /// </summary>
            syriac,

            /// <summary>
            /// Thailand
            /// </summary>
            th,

            /// <summary>
            /// Tifinagh
            /// </summary>
            tifinagh,

            /// <summary>
            /// US native languages
            /// </summary>
            us,

            /// <summary>
            /// Uzbek - Cyrillic
            /// </summary>
            uzuz,

            /// <summary>
            /// Vai
            /// </summary>
            vai,

            /// <summary>
            /// Vietnam
            /// </summary>
            vi,

            /// <summary>
            /// Chinese
            /// </summary>
            zh,

            /// <summary>
            /// Chinese Taiwan
            /// </summary>
            zhtw
        }

        /// <summary>
        /// Unicode character code chart types
        /// </summary>
        public enum UnicodeChartType 
        { 
            /// <summary>
            /// Unicode character code chart types is Script
            /// </summary>
            Script=1, 

            /// <summary>
            /// Unicode character code chart types is Symbol
            /// </summary>
            Symbol,

            /// <summary>
            /// Unicode character code chart types is Punctuation
            /// </summary>
            Punctuation,

            /// <summary>
            /// Unicode character code chart types is Other than three types above
            /// </summary>
            Other
        }

        /// <summary>
        /// Get a random Unicode point (points if it is Surrogate) from the given range
        /// </summary>
        public static string GetRandomCodePoint(UnicodeRange range, int iterations, int [] exclusions, int seed)
        {
            Random rand = new Random(seed);
            int codePoint = 0;
            string retStr = string.Empty;

            if (null != exclusions)
            {
                Array.Sort(exclusions);
            }
            
            for (int i=0; i < iterations; i++)
            {
                codePoint = rand.Next(range.StartOfUnicodeRange, range.EndOfUnicodeRange);
                if (null != exclusions)
                {
                    int index = Array.BinarySearch(exclusions, codePoint);
                    int ctr = 0;
                    while (index >= 0)
                    {
                        codePoint = rand.Next(range.StartOfUnicodeRange, range.EndOfUnicodeRange);
                        index = Array.BinarySearch(exclusions, codePoint);
                        ctr ++;
                        if (MAXNUMITERATION == ctr)
                        {
                            throw new ArgumentOutOfRangeException("TextUtil, " + ctr + " loop has been reached. GetRandomCodePoint may have infinite loop." + 
                                " Range " + String.Format(CultureInfo.InvariantCulture, "0x{0:X}", range.StartOfUnicodeRange) + " - " + 
                                String.Format(CultureInfo.InvariantCulture,"0x{0:X}", range.EndOfUnicodeRange) + " are likely excluded ");
                        }
                    }
                }
                
                if (codePoint > 0xFFFF)
                {
                     // In case it is surrogate
                     retStr += Convert.ToChar((codePoint - 0x10000)/0x400 + 0xD800);
                     retStr += Convert.ToChar((codePoint - 0x10000)%0x400 + 0xDC00);
                }
                else
                {
                    retStr += Convert.ToChar(codePoint);
                }
            }
            
            return retStr;
        }

        /// <summary>
        /// Convert int to string
        /// </summary>
        public static string IntToString(int codePoint)
        {
            string retStr = string.Empty;
            
            if (codePoint > 0xFFFF)
            {
                 // In case it is surrogate
                 retStr += Convert.ToChar((codePoint - 0x10000)/0x400 + 0xD800);
                 retStr += Convert.ToChar((codePoint - 0x10000)%0x400 + 0xDC00);
            }
            else
            {
                retStr += Convert.ToChar(codePoint);
            }

            return retStr;
        }
    }
}

