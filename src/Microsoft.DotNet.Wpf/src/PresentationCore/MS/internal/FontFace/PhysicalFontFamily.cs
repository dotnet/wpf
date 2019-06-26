// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
//
//
// Description: The PhysicalFontFamily class
//
//

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Security;
using System.Text;
using System.Windows;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.TextFormatting;

using MS.Internal;
using MS.Internal.FontCache;
using MS.Internal.TextFormatting;

namespace MS.Internal.FontFace
{
    /// <summary>
    /// PhysicalFontFamily class represents a font family obtained from a collection of OpenType files.
    /// </summary>
    internal sealed class PhysicalFontFamily : IFontFamily
    {
        private Text.TextInterface.FontFamily    _family;
        private IDictionary<XmlLanguage, string> _familyNames;

        // _family.FamilyNames is of type LocalizedStrings which does not support editing (Adding, Replacing, and Clearing)
        // IFontFamily.Names is passed to a LanguageSpecificStringDictionary which is exposed publicly and allows editing
        // operations. Thus to convert from IDictionary<CultureInfo, string> to IDictionary<XmlLanguage, string> we had
        // 2 approaches:
        //          - Copying the elements into a new IDictionary<XmlLanguage, string>
        //          - Implement a new class that wraps the IDictionary<CultureInfo, string> and allow
        //            editing operations
        // The second approach will eventually copy elements into a new structure that allows editing when
        // an editing operation is performed. Since this dictionary is not expected to hold a huge number of elements
        // we chose to do the copying upfront and not lazily and hence use the first approach.
        //
        private static IDictionary<XmlLanguage, string> ConvertDictionary(IDictionary<CultureInfo, string> dictionary)
        {
            Dictionary<XmlLanguage, string> convertedDictionary = new Dictionary<XmlLanguage, string>();
            foreach (KeyValuePair<CultureInfo, string> pair in dictionary)
            {
                // DevDiv.1153238 : In Windows 10, the dictionary argument to this method may contain two different entries
                // for the same language if two fonts in the same font family report two different localized family names.
                // We check for this case, and only add the first one we encounter into convertedDictionary.
                XmlLanguage language = XmlLanguage.GetLanguage(pair.Key.Name);
                if (!convertedDictionary.ContainsKey(language))
                {
                    convertedDictionary.Add(language, pair.Value);
                }
            }

            return convertedDictionary;
        }

        internal PhysicalFontFamily(Text.TextInterface.FontFamily family)
        {
            Invariant.Assert(family != null);
            _family = family;
        }


        /// <summary>
        /// Get typeface metrics of the specified typeface
        /// </summary>
        ITypefaceMetrics IFontFamily.GetTypefaceMetrics(
            FontStyle       style,
            FontWeight      weight,
            FontStretch     stretch
            )
        {
            return GetGlyphTypeface(style, weight, stretch);
        }


        /// <summary>
        /// Look up device font for the typeface.
        /// </summary>
        IDeviceFont IFontFamily.GetDeviceFont(FontStyle style, FontWeight weight, FontStretch stretch)
        {
            return null;
        }


        /// <summary>
        /// Indexer that indexes the underlying family name table via CultureInfo
        /// </summary>
        /// <value></value>
        IDictionary<XmlLanguage,string> IFontFamily.Names
        {
            get
            {
                if (_familyNames == null)
                {
                    _familyNames = ConvertDictionary(_family.FamilyNames);
                }
                return _familyNames;
            }
        }


        /// <summary>
        /// Get the matching glyph typeface of a specified style
        /// </summary>
        /// <param name="style">font style</param>
        /// <param name="weight">font weight</param>
        /// <param name="stretch">font stretch</param>
        /// <returns>matching font face</returns>
        internal GlyphTypeface GetGlyphTypeface(
            FontStyle       style,
            FontWeight      weight,
            FontStretch     stretch
            )
        {
            Text.TextInterface.Font bestMatch = _family.GetFirstMatchingFont((Text.TextInterface.FontWeight)weight.ToOpenTypeWeight(),
                                                                             (Text.TextInterface.FontStretch)stretch.ToOpenTypeStretch(),
                                                                             (Text.TextInterface.FontStyle)   style.GetStyleForInternalConstruction());
            Debug.Assert(bestMatch != null);
            return new GlyphTypeface(bestMatch);
        }

        /// <summary>
        /// Get the matching typeface for the specified target style that also supports
        /// glyph mapping of the specified character string.
        /// </summary>
        /// <param name="style">font style</param>
        /// <param name="weight">font weight</param>
        /// <param name="stretch">font stretch</param>
        /// <param name="charString">character string</param>
        /// <param name="digitCulture">culture used for digit substitution or null</param>
        /// <param name="advance">number of characters with valid glyph mapped</param>
        /// <param name="nextValid">offset to the character mapping to a valid glyph</param>
        /// <returns>matching typeface</returns>
        internal GlyphTypeface MapGlyphTypeface(
            FontStyle               style,
            FontWeight              weight,
            FontStretch             stretch,
            CharacterBufferRange    charString,
            CultureInfo             digitCulture,
            ref int                 advance,
            ref int                 nextValid
            )
        {
            int smallestInvalid = charString.Length;

            // Add all the cached font faces to a priority queue.
            MatchingStyle targetStyle = new MatchingStyle(style, weight, stretch);

            LegacyPriorityQueue<MatchingFace> queue = new LegacyPriorityQueue<MatchingFace>(
                checked((int)_family.Count),
                new MatchingFaceComparer(targetStyle));

            foreach (Text.TextInterface.Font face in _family)
            {
                queue.Push(new MatchingFace(face));
            }

            // Remember the best style match.
            MS.Internal.Text.TextInterface.Font bestStyleTypeface = null;

            // Iterate in priority order.
            for (; queue.Count != 0; queue.Pop())
            {
                int invalid = 0;
                MS.Internal.Text.TextInterface.Font font = queue.Top.FontFace;
                int valid = MapCharacters(font, charString, digitCulture, ref invalid);
                if (valid > 0)
                {
                    if (smallestInvalid > 0 && smallestInvalid < valid)
                    {
                        // advance only to smallestInvalid because there's a better match after that
                        advance = smallestInvalid;
                        nextValid = 0;
                    }
                    else
                    {
                        advance = valid;
                        nextValid = invalid;
                    }

                    return new GlyphTypeface(font);
                }
                else
                {
                    if (invalid < smallestInvalid)
                    {
                        // keep track of the current shortest length of invalid characters,
                        smallestInvalid = invalid;
                    }

                    if (bestStyleTypeface == null)
                    {
                        bestStyleTypeface = font;
                    }
                }
            }

            // no face can map the specified character string,
            // fall back to the closest style match
            advance = 0;
            nextValid = smallestInvalid;
            Debug.Assert(bestStyleTypeface != null);
            return new GlyphTypeface(bestStyleTypeface);
        }

        /// <summary>
        /// Element type for priority queue used by MapGlyphTypeface.
        /// </summary>
        private struct MatchingFace
        {
            internal MatchingFace(Text.TextInterface.Font face)
            {
                _face = face;
                _style = new MatchingStyle(new FontStyle((int)face.Style), new FontWeight((int)face.Weight), new FontStretch((int)face.Stretch));
            }

            internal Text.TextInterface.Font FontFace
            {
                get { return _face; }
            }

            internal MatchingStyle MatchingStyle
            {
                get { return _style; }
            }

            private Text.TextInterface.Font _face;
            private MatchingStyle           _style;
        }

        /// <summary>
        /// Comparer for priority queue used by MapGlyphTypeface.
        /// </summary>
        private class MatchingFaceComparer : IComparer<MatchingFace>
        {
            internal MatchingFaceComparer(MatchingStyle targetStyle)
            {
                _targetStyle = targetStyle;
            }

            int IComparer<MatchingFace>.Compare(MatchingFace a, MatchingFace b)
            {
                return a.MatchingStyle.IsBetterMatch(_targetStyle, b.MatchingStyle) ? -1 : 1;
            }

            private MatchingStyle _targetStyle;
        }


        /// <summary>
        /// Map character supported by the typeface
        /// </summary>
        /// <remarks>
        /// Combining mark is considered part of the character that may be supported
        /// thru precomposed form or OpenType glyph substitution table.
        /// </remarks>
        private int MapCharacters(
            MS.Internal.Text.TextInterface.Font font,
            CharacterBufferRange                unicodeString,
            CultureInfo                         digitCulture,
            ref int                             nextValid
            )
        {
            DigitMap digitMap = new DigitMap(digitCulture);

            int sizeofChar = 0;
            int advance;

            // skip all the leading joiner characters. They need to be shaped with the
            // surrounding strong characters.
            advance = Classification.AdvanceWhile(unicodeString, ItemClass.JoinerClass);
            if (advance >= unicodeString.Length)
            {
                // It is rare that the run only contains joiner characters.
                // If it really happens, just return.
                return advance;
            }

            //
            // If the run starts with combining marks, we will not be able to find base characters for them
            // within the run. These combining marks will be mapped to their best fonts as normal characters.
            //
            bool hasBaseChar = false;

            // Determine how many characters we can advance, i.e., find the first invalid character.
            for (; advance < unicodeString.Length; advance += sizeofChar)
            {
                // Get the character and apply digit substitution, if any.
                int originalChar = Classification.UnicodeScalar(
                    new CharacterBufferRange(unicodeString, advance, unicodeString.Length - advance),
                    out sizeofChar
                    );

                if (Classification.IsJoiner(originalChar))
                    continue;

                if (!Classification.IsCombining(originalChar))
                {
                    hasBaseChar = true;
                }
                else if (hasBaseChar)
                {
                    // continue to advance for combining mark with base char
                    continue;
                }

                int ch = digitMap[originalChar];

                if (font.HasCharacter(checked((uint)ch)))
                    continue;

                // If ch is a substituted character, can we substitute a different character instead?
                if (ch != originalChar)
                {
                    ch = DigitMap.GetFallbackCharacter(ch);
                    if (ch != 0 && font.HasCharacter(checked((uint)ch)))
                        continue;
                }

                // If we fall through to here it's invalid.
                break;
            }

            // UnicodeScalar won't return a sizeofChar that exceeds the string length.
            Debug.Assert(advance <= unicodeString.Length);

            // Find the next valid character.
            if (advance < unicodeString.Length)
            {
                // UnicodeScalar won't return a sizeofChar that exceeds the string length.
                Debug.Assert(advance + sizeofChar <= unicodeString.Length);

                for (nextValid = advance + sizeofChar; nextValid < unicodeString.Length; nextValid += sizeofChar)
                {
                    // Get the character.
                    int originalChar = Classification.UnicodeScalar(
                        new CharacterBufferRange(unicodeString, nextValid, unicodeString.Length - nextValid),
                        out sizeofChar
                        );

                    // Apply digit substitution, if any.
                    int ch = digitMap[originalChar];

                    //
                    // Combining mark should always be shaped by the same font as the base char.
                    // If the physical font is invalid for the base char, it should also be invalid for the
                    // following combining mark so that both characters will go onto the same fallback font.
                    // - When the fallback font is physical font, the font will be valid for both characters
                    //   if and only if it is valid for the base char. Otherwise, it will be invalid for both.
                    // - When the fallback font is composite font, it maps the combining mark to the same font
                    //   as the base char such that they will eventually be resolved to the same physical font.
                    //   That means FamilyMap for the combining mark is not used when it follows a base char.
                    //
                    // The same goes for joiner. Note that "hasBaseChar" here indicates if there is an invalid base
                    // char in front.
                    if (Classification.IsJoiner(ch)
                       || (hasBaseChar && Classification.IsCombining(ch))
                       )
                       continue;

                    // If we have a glyph it's valid.
                    if (font.HasCharacter(checked((uint)ch)))
                        break;

                    // If ch is a substituted character, can we substitute a different character instead?
                    if (ch != originalChar)
                    {
                        ch = DigitMap.GetFallbackCharacter(ch);
                        if (ch != 0 && font.HasCharacter(checked((uint)ch)))
                            break;
                    }
                }
            }

            return advance;
        }


        /// <summary>
        /// Distance from character cell top to English baseline relative to em size.
        /// </summary>
        double IFontFamily.Baseline(double emSize, double toReal, double pixelsPerDip, TextFormattingMode textFormattingMode)
        {
            if (textFormattingMode == TextFormattingMode.Ideal)
            {
                return emSize * _family.Metrics.Baseline;
            }
            else
            {
                double realEmSize = emSize * toReal;
                return TextFormatterImp.RoundDipForDisplayMode(_family.DisplayMetrics((float)(realEmSize), checked((float)pixelsPerDip)).Baseline * realEmSize, pixelsPerDip) / toReal;
            }
        }

        double IFontFamily.BaselineDesign
        {
            get
            {
                return ((IFontFamily)this).Baseline(1, 1, 1, TextFormattingMode.Ideal);
            }
        }


        double IFontFamily.LineSpacingDesign
        {
            get
            {
                return ((IFontFamily)this).LineSpacing(1, 1, 1, TextFormattingMode.Ideal);
            }
        }


        /// <summary>
        /// Recommended baseline-to-baseline distance for text in this font
        /// </summary>
        double IFontFamily.LineSpacing(double emSize, double toReal, double pixelsPerDip, TextFormattingMode textFormattingMode)
        {
            if (textFormattingMode == TextFormattingMode.Ideal)
            {
                return emSize * _family.Metrics.LineSpacing;
            }
            else
            {
                double realEmSize = emSize * toReal;
                return TextFormatterImp.RoundDipForDisplayMode(_family.DisplayMetrics((float)(realEmSize), checked((float)pixelsPerDip)).LineSpacing * realEmSize, pixelsPerDip) / toReal;
            }
        }

        ICollection<Typeface> IFontFamily.GetTypefaces(FontFamilyIdentifier familyIdentifier)
        {
            return new TypefaceCollection(new FontFamily(familyIdentifier), _family);
        }


        /// <summary>
        /// Get family name correspondent to the first n-characters of the specified character string
        /// </summary>
        bool IFontFamily.GetMapTargetFamilyNameAndScale(
            CharacterBufferRange    unicodeString,
            CultureInfo             culture,
            CultureInfo             digitCulture,
            double                  defaultSizeInEm,
            out int                 cchAdvance,
            out string              targetFamilyName,
            out double              scaleInEm
            )
        {
            cchAdvance = unicodeString.Length;
            targetFamilyName = null;
            scaleInEm = defaultSizeInEm;
            return false;
        }
    }
}

