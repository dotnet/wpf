// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections;
using System.Security.Permissions;
using System.Text;

namespace Microsoft.Test.Globalization
{
   /// <summary>
   /// Class for automatically generating random UI-culture-aware data
   /// </summary>
    public class Generate
    {
        #region Constructor

            /// <summary>
            ///  Private Constructor
            /// </summary>
            private Generate() {}// Block class instantiation (static Methods)

        #endregion

        #region Properties

            private static Random m_rnd = new Random();
            private static char[] m_invalidEntries = new char[] { 
                                                                    (char)0xFDD0, (char)0xFDD1, (char)0xFDD2, (char)0xFDD3, (char)0xFDD4, (char)0xFDD5, (char)0xFDD6, (char)0xFDD7, (char)0xFDD8, (char)0xFDD9, (char)0xFDDA, (char)0xFDDB, (char)0xFDDC, (char)0xFDDD, (char)0xFDDE, (char)0xFDDF,
                                                                    (char)0xFDE0, (char)0xFDE1, (char)0xFDE2, (char)0xFDE3, (char)0xFDE4, (char)0xFDE5, (char)0xFDE6, (char)0xFDE7, (char)0xFDE8, (char)0xFDE9, (char)0xFDEA, (char)0xFDEB, (char)0xFDEC, (char)0xFDED, (char)0xFDEE, (char)0xFDEF,
                                                                    (char)0xFFFE,
                                                                    (char)0xFFFF
                                                                };

            private static Hashtable m_invalidHash = PopulateInvalidHash;
            private static int m_maxStringSize = 261; // Greater than MAX_PATH just for fun.

            private static Hashtable PopulateInvalidHash
            {
                get
                {
                    Hashtable retVal = new Hashtable();
                    for (int t = 0; t < m_invalidEntries.Length; t++)
                    {
                        retVal.Add(m_invalidEntries[t], null);
                    }
                    return retVal;
                }
            }

            /// <summary>
            /// Maximum string length allowed on system.
            /// </summary>
            public static int MaxStringSize
            {
                get
                {
                    return m_maxStringSize;
                }
                set
                {
                    if (value<=0)
                        throw new Exception("(Generate::MaxStringSize::'Getter') String Size must be greater than zero.");
                    m_maxStringSize = value;
                }
            }

        #endregion

        #region Public Methods

            #region GetRandomString(5 Overloads)
                /// <summary>
                /// Return a random Unicode string (UTF16).
                /// </summary>
                /// <returns>(string) a valid Unicode string (random size between 0 and 'MaxStringSize')</returns>
                public static string GetRandomString()
                {
                    int iSize = m_rnd.Next(MaxStringSize) + 1;  // Min string size is 1
                    return GetRandomString(iSize, new char[0]);
                }


                /// <summary>
                /// Return a random Unicode string (UTF16) of the specified length.
                /// </summary>
                /// <param name="length">(integer) The length of the string to be returned</param>
                /// <returns>(string) A string containing the new generated unicode char</returns>
                public static string GetRandomString(int length)
                {
                    return GetRandomString(length, new char[0]);
                }


                /// <summary>
                /// Return a Random Unicode string based on the scriptType (Hebrew, Latin ...)
                /// The string returned will be of the specified length
                /// </summary>
                /// <param name="length">Integer, the length of the string to be returned</param>
                /// <param name="scriptType">scriptTypeEnum (see scriptTypeEnum), the script type requested (Arabic, Japanese, Armenian...)</param>
                /// <returns>string, a string containing the new generated unicode char</returns>
                public static string GetRandomString(int length, ScriptTypeEnum scriptType)
                {
                    return GetRandomString(length,scriptType, new char[0]);
                }

                /// <summary>
                /// Return a random Unicode string (UTF16) of the specified length, excluding the specified chars
                /// </summary>
                /// <param name="length">(integer) The length of the string to be returned</param>
                /// <param name="ExcludeChars">(char[]) An array of char that need to be exluded from the returned string</param>
                /// <returns>(string) A string containing the new generated unicode char</returns>
                public static string GetRandomString(int length, char[] ExcludeChars)
                {
                    // Check arguments
                    if (length <=0)
                        throw(new ArgumentOutOfRangeException("length MUST be greater than 0"));
                    if(ExcludeChars == null)
                        ExcludeChars = new Char[0];

                    // Convert To char[] passed-in to an HashTable.
                    System.Collections.Hashtable badCharHash = new System.Collections.Hashtable(ExcludeChars.Length);
                    for(int i = 0; i < ExcludeChars.Length; i++)
                    {
                        badCharHash.Add(ExcludeChars[i], null);
                    }

                    // Need to multiple length by 2 because of surrogates
                    StringBuilder builder = new StringBuilder(length, length * 2);
                    char stringCell;
                    int t = 0;
                    while(t++ < length)
                    {
                        // Get a random char
                        // * Don't bother with language rules 
                        // * Don't bother to check if this char can be rendered
                        // * Care about surrogates
                        // * Care about invalid chars
                        // * Check for unwanted char
                        do
                        {
                            stringCell = (char)m_rnd.Next(0xFFFF);
                        } while (badCharHash.Contains(stringCell));

                        if (stringCell >= 0xD800 && stringCell <= 0xDFFF)
                        {
                            char highSurrogate;
                            // This is a surrogate range, create a well form surrogate
                            if (stringCell >= 0xD800 && stringCell <= 0xDBFF)
                            {
                                // High surrogate
                                highSurrogate = stringCell;
                                stringCell = (char)(m_rnd.Next(0x400) + 0xDC00);
                            }
                            else
                            {
                                // Low surrogate
                                highSurrogate = (char)(m_rnd.Next(0x400) + 0xD800);
                            }
                            builder.Append(highSurrogate);
                        }
                        // Some Unicode entries are not valid characters
                        // Unvalid char are : FFFF / FFEF / FEFF and also (Unicode 3.1) range [0xFDD0-0xFDEF]
                        if (stringCell == 0xFFFE || stringCell == 0xFFFF || (stringCell >= 0xFDD0 && stringCell <= 0xFDEF) )
                        {
                            // Lazy hack ! need to be fixed.
                            t--;
                        }
                        else
                        {
                            // Valid character, add it to the string
                            builder.Append(stringCell);
                        }
                    }

                    return builder.ToString();


                }

                /// <summary>
                /// Return a Random Unicode string based on the scriptType (Hebrew, Latin ...)
                /// The string returned will be of the specified length and will not include any
                /// of the specified char
                /// </summary>
                /// <param name="length">Length of random string to be returned</param>
                /// <param name="scriptType">Culture of script type requested</param>
                /// <param name="ExcludeChars">Characters to exclude from string</param>
                /// <returns>Random, culturally accurate string</returns>
                public static string GetRandomString(int length, ScriptTypeEnum scriptType, char[] ExcludeChars)
                {
                    // Check argument
                    if (length <=0)
                    {
                        throw(new ArgumentOutOfRangeException("length MUST be greater than 0"));
                    }
                    if ( ExcludeChars == null)
                    {
                        ExcludeChars = new Char[0];
                    }

                    // If user wants mixed type, forward call to the other function
                    if (scriptType == ScriptTypeEnum.Mixed)
                        return GetRandomString(length, ExcludeChars);


                    // Convert To char[] passed-in to an HashTable.
                    System.Collections.Hashtable badCharHash = new System.Collections.Hashtable(ExcludeChars.Length);
                    for(int i = 0; i < ExcludeChars.Length; i++)
                    {
                        badCharHash.Add(ExcludeChars[i], null);
                    }


                    int[] lowerBound = null;
                    int[] upperBound = null;

                    // ( Every Language implement number 0..9 : 0x0030 -> 0x0039 )

                    switch(scriptType)
                    {
                        case ScriptTypeEnum.Arabic:
                            // Arabic + Arabic Presentation  Form A & B
                            buildArray(ref lowerBound, 0x0030,0x0600,0x06F0,0xFB50,0xFBD3,0xFD50,0xFD92,0xFDF0,0xFE70,0xFE74,0xFE76);
                            buildArray(ref upperBound, 0x0039,0x06ED,0x06FE,0xFBB1,0xFD3F,0xFD8F,0xFDC7,0xFDFB,0xFE72,0xFE74,0xFEFC);
                            break;
                        case ScriptTypeEnum.Armenian:
                            // Armenian + Armenian Ligatures
                            buildArray(ref lowerBound, 0x0030,0x0531,0x0559,0x0561,0x0589,0x0FB13);
                            buildArray(ref upperBound, 0x0039,0x0556,0x055F,0x0587,0x058A,0x0FB17);
                            break;
                        case ScriptTypeEnum.Bengali:
                            buildArray(ref lowerBound, 0x0030,0x0981,0x0985,0x098F,0x0993,0x09AA,0x09B2,0x09B6,0x09BC,0x09BE,0x09C7,0x09CB,0x09D7,0x09DC,0x09DF,0x09E6);
                            buildArray(ref upperBound, 0x0039,0x0983,0x098C,0x0990,0x09A8,0x09B0,0x09B2,0x09B9,0x09BC,0x09C4,0x09C8,0x09CD,0x09D7,0x09DD,0x09E3,0x09FA);
                            break;
                        case ScriptTypeEnum.Braille:
                            buildArray(ref lowerBound, 0x0030,0x2800);
                            buildArray(ref upperBound, 0x0039,0x28FF);
                            break;
                        case ScriptTypeEnum.CanadianArboriginalSyllabics:
                            buildArray(ref lowerBound, 0x0030,0x1401);
                            buildArray(ref upperBound, 0x0039,0x1676);
                            break;
                        case ScriptTypeEnum.Cherokee:
                            buildArray(ref lowerBound, 0x0030,0x13A0);
                            buildArray(ref upperBound, 0x0039,0x13F4);
                            break;
                        case ScriptTypeEnum.ChineseSimplified:
                            buildArray(ref lowerBound, 0x0030,0x3000,0x3005,0x301d,0x3021,0x3041,0x309b,0x30a1,0x30fc,0x3105,0x3192,0x3220,0x3280,0x329f,0x32a9,0x338e,0x339c,0x33a1,0x33c4,0x33ce,0x33d1,0x33d5,0x4e00,0xe000,0xf8f5);
                            buildArray(ref upperBound, 0x0039,0x3003,0x3017,0x301e,0x3029,0x3093,0x309e,0x30f6,0x30fe,0x3129,0x319f,0x3243,0x329d,0x32a3,0x32b0,0x338f,0x339e,0x33a1,0x33c4,0x33ce,0x33d2,0x33d5,0x9fa5,0xe864,0xf8f5);
                            break;
                        case ScriptTypeEnum.ChineseTraditional:
                            // Bopomofo :0x3105->0x312C , Bopomofo Extended : 0x31A0->31B7
                            buildArrayFromResource(ref lowerBound, ref upperBound, "ChineseTraditional.txt");
                            break;
                        case ScriptTypeEnum.Cyrillic:
                            buildArray(ref lowerBound, 0x0030,0x0400,0x488,0x48C,0x4C7,0x4CB,0x4D0,0x4F8);
                            buildArray(ref upperBound, 0x0039,0x0486,0x489,0x4C4,0x4C8,0x4CC,0x4F5,0x4F9);
                            break;
                        case ScriptTypeEnum.Devanagari:
                            buildArray(ref lowerBound, 0x0030,0x0901,0x0905,0x093C,0x0950,0x0958);
                            buildArray(ref upperBound, 0x0039,0x0903,0x0939,0x094D,0x0954,0x0970);
                            break;
                        case ScriptTypeEnum.Ethiopic:
                            buildArray(ref lowerBound, 0x0030,0x1200,0x1208,0x1248,0x124A,0x1250,0x1258,0x125A,0x1260,0x1288,0x128A,0x1290,0x12B0,0x12B2,0x12B8,0x12C0,0x12C2,0x12C8,0x12D0,0x12D8,0x12F0,0x1310,0x1312,0x1318,0x1320,0x1348,0x1361);
                            buildArray(ref upperBound, 0x0039,0x1206,0x1246,0x1248,0x124D,0x1256,0x1258,0x125D,0x1286,0x1288,0x128D,0x12AE,0x12B0,0x12B5,0x12BE,0x12C0,0x12C5,0x12CE,0x12D6,0x12EE,0x130E,0x1310,0x1315,0x131E,0x1343,0x135A,0x137C);
                            break;
                        case ScriptTypeEnum.Georgian:
                            buildArray(ref lowerBound, 0x0030,0x10A0,0x10D0,0x10FB);
                            buildArray(ref upperBound, 0x0039,0x10C5,0x10F6,0x10FB);
                            break;
                        case ScriptTypeEnum.Greek:
                            // Greek + Greek Extended
                            buildArray(ref lowerBound, 0x0030,0x0374,0x037A,0x037E,0x0384,0x038C,0x038E,0x03A3,0x03D0,0x03DA, 0x1F00,0x1F18,0x1F20,0x1F48,0x1F50,0x1F59,0x1F5B,0x1F5D,0x1F5F);
                            buildArray(ref upperBound, 0x0039,0x0375,0x037A,0x037E,0x038A,0x038C,0x03A1,0x03CE,0x03D7,0x03F3, 0x1F15,0x1F1D,0x1F45,0x1F4D,0x1F57,0x1F59,0x1F5B,0x1F5D,0x1F7D);
                            break;
                        case ScriptTypeEnum.Gujarati:
                            buildArray(ref lowerBound, 0x0030,0x0A81,0x0A85,0x0A8D,0x0A8F,0x0A93,0x0AAA,0x0AB2,0x0AB5,0x0ABC,0x0AC7,0x0ACB,0x0AD0,0x0AE0,0x0AE6);
                            buildArray(ref upperBound, 0x0039,0x0A83,0x0A8B,0x0A8D,0x0A91,0x0AA8,0x0AB0,0x0AB3,0x0AB9,0x0AC5,0x0AC9,0x0ACD,0x0AD0,0x0AE0,0x0AEF);
                            break;
                        case ScriptTypeEnum.Gurmukhi:
                            buildArray(ref lowerBound, 0x0030,0x0a02,0x0a05,0x0a0f,0x0a13,0x0a2a,0x0a32,0x0a35,0x0a38,0x0a3c,0x0a3e,0x0a47,0x0a4b,0x0a59,0x0a5e,0x0a66);
                            buildArray(ref upperBound, 0x0039,0x0a02,0x0a0a,0x0a10,0x0a28,0x0a30,0x0a33,0x0a36,0x0a39,0x0a3c,0x0a42,0x0a48,0x0a4d,0x0a5c,0x0a5e,0x0a74);
                            break;
                        case ScriptTypeEnum.Hebrew:
                            // Hebrew + Hebrew Alphabetic Presentation Form
                            buildArray(ref lowerBound, 0x0030,0x0591,0x05A3,0x05BB,0x05D0,0x05F0,0x0FB1D,0x0FB38,0x0FB3E,0x0FB40,0x0FB43,0x0FB46);
                            buildArray(ref upperBound, 0x0039,0x05A1,0x05B9,0x05C4,0x05EA,0x05F4,0x0FB36,0x0FB3C,0x0FB3E,0x0FB41,0x0FB44,0x0FB4F);
                            break;
                        case ScriptTypeEnum.Japanese:
                            /*
                            // Currently contains : Kanji, Hiragana, Katakana
                            buildArray(ref lowerBound, 0x0030,0x2F00, 0x3041, 0x3099, 0x30A1);
                            buildArray(ref upperBound, 0x0039,0x2FD5, 0x3094, 0x309E, 0x30FE);
                            */
                            buildArrayFromResource(ref lowerBound, ref upperBound, "Japanese.txt");
                            break;
                        case ScriptTypeEnum.Kannada:
                            buildArray(ref lowerBound, 0x0030,0x0C82,0x0C85,0x0C8E,0x0C92,0x0CAA,0x0CB5,0x0CBE,0x0CC6,0x0CCA,0x0CD5,0x0CDE,0x0CE0,0x0CE6);
                            buildArray(ref upperBound, 0x0039,0x0C83,0x0C8C,0x0C90,0x0CA8,0x0CB3,0x0CB9,0x0CC4,0x0CC8,0x0CCD,0x0CD6,0x0CDE,0x0CE1,0x0CEF);
                            break;
                        case ScriptTypeEnum.Khmer:
                            buildArray(ref lowerBound, 0x0030,0x1780,0x17E0);
                            buildArray(ref upperBound, 0x0039,0x17DC,0x17E9);
                            break;
                        case ScriptTypeEnum.Korean:
                            /*
                            buildArray(ref lowerBound, 0x0030,0x02C7,0x02D0,0x02D8,0x02D9,0x02DA,0x02DD,  0x3000,0x3008,0x3013,   0x3131,     0x3200,0x3260,0x327F,   0xAC00,  0xF900 );
                            buildArray(ref upperBound, 0x0039,0x02C7,0x02D0,0x02D8,0x02D9,0x02DB,0x02DD,  0x3003,0x3011,0x3015,   0x318E,     0x321C,0x327B,0x327F,   0xD7A3,  0xFA0B);
                            */
                            buildArrayFromResource(ref lowerBound, ref upperBound, "Korean.txt");
                            break;
                        case ScriptTypeEnum.Lao:
                            buildArray(ref lowerBound, 0x0030,0x0E81,0x0E84,0x0E87,0x0E8A,0x0E8D,0x0E94,0x0E99,0x0EA1,0x0EA5,0x0EA7,0x0EAA,0x0EAD,0x0EBB,0x0EC0,0x0EC6,0x0EC8,0x0ED0,0x0EDC);
                            buildArray(ref upperBound, 0x0039,0x0E82,0x0E84,0x0E88,0x0E8A,0x0E8D,0x0E97,0x0E9F,0x0EA3,0x0EA5,0x0EA7,0x0EAB,0x0EB9,0x0EBD,0x0EC4,0x0EC6,0x0ECD,0x0ED9,0x0EDD);
                            break;
                        case ScriptTypeEnum.Latin:
                            // Latin basic + Latin-1 Supplement + Latin Extended-A + Latin Extended-B + IPA Extension + Spacing Modifier Letters + Combining Diacritical Marks + Latin Extended Additional + Latin Ligature + FullWidth ASCII
                            buildArray(ref lowerBound, 0x0030,0x0020,0x0222,0x0250,0x02B0,0x0300,0x0360, 0x1E00,0x1EA0, 0xFB00, 0xFF01);
                            buildArray(ref upperBound, 0x0039,0x021F,0x0233,0x02AD,0x02EE,0x034E,0x0362, 0x1E9B,0x1EF9, 0xFB06, 0xFF5E);
                            break;
                        case ScriptTypeEnum.Malayalam:
                            buildArray(ref lowerBound, 0x0030,0x0D02,0x0D05,0x0D0E,0x0D12,0x0D2A,0x0D3E,0x0D46,0x0D4A,0x0D57,0x0D60,0x0D66);
                            buildArray(ref upperBound, 0x0039,0x0D03,0x0D0C,0x0D10,0x0D28,0x0D39,0x0D43,0x0D48,0x0D4D,0x0D57,0x0D61,0x0D6F);
                            break;
                        case ScriptTypeEnum.Mongolian:
                            buildArray(ref lowerBound, 0x0030,0x1800,0x1810,0x1820,0x1880);
                            buildArray(ref upperBound, 0x0039,0x18E0,0x1819,0x1877,0x18A9);
                            break;
                        case ScriptTypeEnum.Myanmar:
                            buildArray(ref lowerBound, 0x0030,0x1000,0x1023,0x1029,0x102C,0x1036,0x1040);
                            buildArray(ref upperBound, 0x0039,0x1021,0x1027,0x102A,0x1032,0x1039,0x1059);
                            break;
                        case ScriptTypeEnum.Ogham:
                            buildArray(ref lowerBound, 0x0030,0x1680);
                            buildArray(ref upperBound, 0x0039,0x169C);
                            break;
                        case ScriptTypeEnum.Oriya:
                            buildArray(ref lowerBound, 0x0030,0x0B01,0x0B05,0x0B0F,0x0B13,0x0B2A,0x0B32,0x0B36,0x0B3C,0x0B47,0x0B4B,0x0B56,0x0B5C,0x0B5F,0x0B66);
                            buildArray(ref upperBound, 0x0039,0x0B03,0x0B0C,0x0B10,0x0B28,0x0B30,0x0B33,0x0B39,0x0B43,0x0B48,0x0B4D,0x0B57,0x0B5D,0x0B61,0x0B70);
                            break;
                        case ScriptTypeEnum.Runnic:
                            buildArray(ref lowerBound, 0x0030,0x16A0);
                            buildArray(ref upperBound, 0x0039,0x16F0);
                            break;
                        case ScriptTypeEnum.Sinhala:
                            buildArray(ref lowerBound, 0x0030,0x0D82,0x0D85,0x0D9A,0x0DB3,0x0DBD,0x0DC0,0x0DCA,0x0DCF,0x0DD6,0x0DD8,0x0DF2);
                            buildArray(ref upperBound, 0x0039,0x0D83,0x0D96,0x0DB1,0x0DBB,0x0DBD,0x0DC6,0x0DCA,0x0DD4,0x0DD6,0x0DDF,0x0DF4);
                            break;
                        case ScriptTypeEnum.Syriac:
                            buildArray(ref lowerBound, 0x0030,0x0700,0x070F,0x0730);
                            buildArray(ref upperBound, 0x0039,0x070D,0x072C,0x074A);
                            break;
                        case ScriptTypeEnum.Tamil:
                            buildArray(ref lowerBound, 0x0030,0x0B82,0x0B85,0x0B8E,0x0B92,0x0B99,0x0B9C,0x0B9E,0x0BA3,0x0BA8,0x0BAE,0x0BB7,0x0BBE,0x0BC6,0x0BCA,0x0BD7,0x0BE7);
                            buildArray(ref upperBound, 0x0039,0x0B83,0x0B8A,0x0B90,0x0B95,0x0B9A,0x0B9C,0x0B9F,0x0BA4,0x0BAA,0x0BB5,0x0BB9,0x0BC2,0x0BC8,0x0BCD,0x0BD7,0x0BF2);
                            break;
                        case ScriptTypeEnum.Telugu:
                            buildArray(ref lowerBound, 0x0030,0x0C01,0x0C05,0x0C0E,0x0C12,0x0C2A,0x0C35,0x0C3E,0x0C46,0x0C4A,0x0C55,0x0C60,0x0C66);
                            buildArray(ref upperBound, 0x0039,0x0C03,0x0C0C,0x0C10,0x0C28,0x0C33,0x0C39,0x0C44,0x0C48,0x0C4D,0x0C56,0x0C61,0x0C6F);
                            break;
                        case ScriptTypeEnum.Thaana:
                            buildArray(ref lowerBound, 0x0030,0x0780);
                            buildArray(ref upperBound, 0x0039,0x07b0);
                            break;
                        case ScriptTypeEnum.Thai:
                            buildArray(ref lowerBound, 0x0030,0x0E01,0x0E3F);
                            buildArray(ref upperBound, 0x0039,0x0E3A,0x0E5B);
                            break;
                        case ScriptTypeEnum.Tibetan:
                            buildArray(ref lowerBound, 0x0030,0x0F00,0x0F49,0x0F71,0x0F90,0x0F99,0x0FBE,0x0FCF);
                            buildArray(ref upperBound, 0x0039,0x0F47,0x0F6A,0x0F8B,0x0F97,0x0FBC,0x0FCC,0x0FCF);
                            break;
                        case ScriptTypeEnum.Yi:
                            buildArray(ref lowerBound, 0x0030,0xA000,0xA490,0xA4A4,0xA4B5,0xA4C2,0xA4C6);
                            buildArray(ref upperBound, 0x0039,0xA48C,0xA4A1,0xA4B3,0xA4C0,0xA4C4,0xA4C6);
                            break;
                            // Handle surrogate in a different way 
                        case ScriptTypeEnum.Surrogate:
                            StringBuilder surrogate = new StringBuilder(length * 2);
                            //                int t = 0;
                            //                while(t++ < length)
                            for (int t = 0; t<length; t++)
                            {
                                // 0xDBFF - 0xD800 + 1 = 1024
                                char highSur = (char)(m_rnd.Next(1024) + 0xD800);
                                char lowSur = (char)(m_rnd.Next(1024) + 0xDC00);
                                surrogate.Append(highSur);
                                surrogate.Append(lowSur);
                            }
                            return surrogate.ToString();
                        default:
                            throw(new ArgumentOutOfRangeException("(Generate.GetRandomString) This enum value is not supported"));
                    }

                    // Calculate the absolute Range
                    int Range = 0;
                    for(int t = 0; t < lowerBound.Length; t++)
                    {
                        Range += upperBound[t] - lowerBound[t] + 1;
                    }

                    StringBuilder builder = new StringBuilder(length);
                    char stringCell;
                    for (int t = 0; t < length; t++)
                    {
                        do
                        {
                            int intCell = m_rnd.Next(Range);

                            // translate abolute range into real range
                            intCell += lowerBound[0];
                            int i = 0;
                            while(intCell > upperBound[i])
                            {
                                intCell += lowerBound[i+1] - upperBound[i] - 1;
                                i++;
                            }

                            // translate int into Unicode char
                            stringCell = (char)intCell;
                        } while ( badCharHash.Contains(stringCell) );   // make sure we do not pick what the user excluded


                        builder.Append(stringCell);
                    }

                    return builder.ToString();
                }

            #endregion GetRandomString

            #region GetUncheckedString (4 Overloads)

                /// <summary>
                /// Build a Unicode string (might not be a character - ie : 0x FFFF)
                /// </summary>
                /// <returns>(string) The string of Unicode entries</returns>
                public static string GetUncheckedString()
                {
                    return GetUncheckedString( (int)(m_rnd.Next(MaxStringSize) + 1) );
                }

                /// <summary>
                /// Build a Unicode string (of the specified length) ( some entry points might not be a valid charaters)
                /// </summary>
                /// <param name="stringSize">The length of the string to return</param>
                /// <returns>(string) The string of Unicode entries</returns>
                public static string GetUncheckedString(int stringSize)
                {
                    return GetUncheckedString(stringSize, 0x0,0xFFFF);
                }

                /// <summary>
                /// Build a Unicode string in the specified range (might not be a valid string - ie : unmatching hi-low surrogate)
                /// </summary>
                /// <param name="min">The lower bound (included)</param>
                /// <param name="max">The upper bound (included)</param>
                /// <returns>(string) The string of Unicode entries</returns>
                public static string GetUncheckedString(int min, int max)
                {
                    return GetUncheckedString( (int)(m_rnd.Next(MaxStringSize) + 1), min, max );
                }

                /// <summary>
                /// Build a Unicode string (of the specified length) in the specified range (might not be a valid string - ie : unmatching hi-low surrogate)
                /// </summary>
                /// <param name="stringSize">The length of the string to return</param>
                /// <param name="min">The lower bound (included)</param>
                /// <param name="max">The upper bound (included)</param>
                /// <returns>(string) The string of Unicode entries</returns>
                public static string GetUncheckedString(int stringSize, int min, int max)
                {
                    if(min > max)
                    {
                        throw new ArgumentException("'min' argument cannot be bigger than 'max' argument");
                    }
                    if (max > 0xFFFF)
                    {
                        throw new ArgumentOutOfRangeException("max", max, "argument cannot be greater than 0xFFFF (Unicode range = [0x0, 0xFFFF] )");
                    }

                    StringBuilder str = new StringBuilder((int)stringSize);
                    for(int t = 0; t < stringSize; t++)
                    {
                        str.Append((char)m_rnd.Next( (int) ((max + 1) - min)));
                    }
                    return str.ToString();
                }

            #endregion GetUncheckedString

            #region GetInvalidString (4 overloads)

                /// <summary>
                /// Get an invalid string by returning non characters Entry point (and might return invalid Surrogate pair)
                /// </summary>
                /// <returns>(string) Return an invalid string</returns>
                public static string GetInvalidString()
                {
                    return GetInvalidString((int)(m_rnd.Next(MaxStringSize) + 1));
                }

                /// <summary>
                /// Get an invalid string by returning non characters Entry point (and might return invalid Surrogate pair)
                /// </summary>
                /// <param name="stringSize">(int) The size of the string to return </param>
                /// <returns>(string) Return an invalid string</returns>
                public static string GetInvalidString(int stringSize)
                {
                    double percent = m_rnd.Next(100) / 100.0;
                    if (percent == 0)
                    {
                        // cannot be zero percent (will throw an exception) default to 1%
                        percent = 0.01;
                    }

                    return GetInvalidString(stringSize, percent);
                }

                /// <summary>
                /// Get an invalid string by returning non characters Entry point (and might return invalid Surrogate pair)
                /// </summary>
                /// <param name="percentInvalid">(double) The percentage of characters in the string to be invalid (must be bigger than 0.0 and less or equal to 1.0)</param>
                /// <returns>(string) Return an invalid string</returns>
                public static string GetInvalidString(double percentInvalid)
                {
                    return GetInvalidString((int)(m_rnd.Next(MaxStringSize) + 1), percentInvalid);
                }

                /// <summary>
                /// Get an invalid string by returning non characters Entry point (and might return invalid Surrogate pair)
                /// </summary>
                /// <param name="stringSize">(int) The size of the string to return </param>
                /// <param name="percentInvalid">(double) The percentage of characters in the string to be invalid (must be bigger than 0.0 and less or equal to 1.0)</param>
                /// <returns>(string) Return an invalid string</returns>
                public static string GetInvalidString(int stringSize, double percentInvalid)
                {

                    // check params
                    if (percentInvalid <= 0.0 || percentInvalid > 1.0)
                    {
                        throw new ArgumentOutOfRangeException("percentInvalid", percentInvalid, "percentage must be between 0.0 (excluded) and 1.0 (included)");
                    }

                    /*
                    System.Collections.Hashtable invalidHash = new System.Collections.Hashtable(m_invalidPoints.Length);
                    for(int t = 0; t < m_invalidPoints.Length; t++)
                    {
                        invalidHash.Add(m_invalidPoints[t], null);
                    }
                    */


                    // Compute the number of char to be invalid
                    int invalidCharSize =  (int)(stringSize * percentInvalid);
                    if (invalidCharSize == 0)
                    {
                        invalidCharSize = 1;
                    }


                    // build a list of invalid and valid item
/*
                    char[] invalidEntries = new char[m_invalidHash.Keys.Count];
                    m_invalidHash.Keys.CopyTo(invalidEntries,0);
*/
                    System.Collections.ArrayList list = new System.Collections.ArrayList((int)stringSize);
                    for(int t = 0 ; t < invalidCharSize; t++)
                    {
//                        list.Add(invalidEntries[ m_rnd.Next(invalidEntries.Length)]);
                        list.Add(m_invalidEntries[m_rnd.Next(m_invalidHash.Keys.Count)]);
                    }
                    for(int t = 0; t < (int)stringSize - invalidCharSize; t++)
                    {
                        char entryPoint; 
                        do 
                        {
                            entryPoint = (char)m_rnd.Next(0x10000); 
                        } while(m_invalidHash.Contains(entryPoint));
                        list.Add(entryPoint);
                    }
                    
                    // Arrange the string to be random
                    int listSize = list.Count;
                    StringBuilder sb = new StringBuilder(listSize);
                    for (int i = 0; i < listSize; i++)
                    {
                        int index = m_rnd.Next(list.Count);
                        sb.Append((char)list[index]);
                        list.Remove(list[index]);
                    }
                    return sb.ToString();
                }

            #endregion GetInvalidString

        #endregion Public Methods

        #region Helper functions (private)
            /// <summary>
            /// (Internal use only, Do not use) Build an array from a array passed as params.
            /// </summary>
            /// <param name="bound">(ref int[]) The array to populate</param>
            /// <param name="list">(params int[]) The entries to be copied in the array</param>
            private static void buildArray(ref int[] bound, params int[] list) 
            {
                bound = new int[list.Length];
                for(int t = 0; t< list.Length; t++)
                {
                    bound[t] = list[t];
                }
            }

            /// <summary>
            /// (Internal use only, Do not use) Build an array from a specific resource
            /// </summary>
            /// <param name="lowerBound">(ref int[]) A reference to the Array containing the lower bounds</param>
            /// <param name="upperBound">(ref int[]) A reference to the Array containing the upper bounds</param>
            /// <param name="resourceName">The name of the resource from where to grab info</param>
            private static void buildArrayFromResource(ref int[]lowerBound, ref int[]upperBound,string resourceName)
            {
                FileIOPermission fp = new FileIOPermission(PermissionState.Unrestricted);
                fp.Assert();
                System.Reflection.Assembly assembly = System.Reflection.Assembly.GetExecutingAssembly();  //GetEntryAssembly();
                System.IO.Stream stream = null;
                System.IO.StreamReader streamReader = null;
                try
                {
                    stream = assembly.GetManifestResourceStream(resourceName);

                    // compute the number of lines (1 line = 15 char)
                    // BUGBUG : comment and empty lines will corrupt this (not 15 char)
                    // !!! Might crash the App, need to be fixed !!!
                        // FIX will be : Use a ArrayList instead
                    long size = stream.Length / 15;
                    lowerBound = new int[size];
                    upperBound = new int[size];
                    streamReader = new System.IO.StreamReader(stream);

                    for(int t = 0, index = 0; t < size; t++)
                    {
                    string line = streamReader.ReadLine();
                    // ignore empty lines and comments
                    if(line == null)
                        continue;
                    if(line.Trim()[0] == ';')
                        continue;
                    string[] limits = line.Split(',');
                    lowerBound[index] = Convert.ToInt32(limits[0],16);
                    upperBound[index] = Convert.ToInt32(limits[1],16);
                    index++;
                    }
                }
                catch(Exception exp)
                {
                    throw exp;
                }
                finally
                {
                    if(stream != null)
                    {
                        stream.Close();
                    }
                    if(streamReader != null)
                    {
                        streamReader.Close();
                    }
                   System.Security.CodeAccessPermission.RevertAssert();
                }
            }

        #endregion
   }
}
