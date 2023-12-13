// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//---------------------------------------------------------------------------
//
//
//
//
// Description: The CachedCompositeFamily class
//
//
//
//
//---------------------------------------------------------------------------

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Security;
using System.Text;
using System.Windows;
using System.Windows.Markup;    // for XmlLanguage
using System.Windows.Media;
using System.Windows.Media.TextFormatting;
 // External Team


using MS.Utility;
using MS.Internal;
using MS.Internal.FontCache;
using MS.Internal.Shaping;
using MS.Internal.TextFormatting;

namespace MS.Internal.FontFace
{
    /// <summary>
    /// CachedCompositeFamily class represents a composite font family obtained from .CompositeFont file.
    /// </summary>
    internal class CachedCompositeFamily : IFontFamily
    {
        //------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------

        #region Constructors

        internal unsafe CachedCompositeFamily(CachedFontFamily cachedFamily)
        {
            Debug.Assert(!cachedFamily.IsNull);
            _cachedFamily = cachedFamily;
        }

        #endregion Constructors

        //------------------------------------------------------
        //
        //  Internal Methods
        //
        //------------------------------------------------------

        #region Internal Methods


        /// <summary>
        /// Get typeface metrics of the specified style
        /// </summary>
        ITypefaceMetrics IFontFamily.GetTypefaceMetrics(
            FontStyle       style,
            FontWeight      weight,
            FontStretch     stretch
            )
        {
            CachedFontFace bestFace = FindNearestTypeface(style, weight, stretch);
            if (!bestFace.IsNull)
            {
                unsafe
                {
                    return new CompositeTypefaceMetrics(
                        bestFace.CompositeFace->underlinePosition,
                        bestFace.CompositeFace->underlineThickness,
                        bestFace.CompositeFace->strikeThroughPosition,
                        bestFace.CompositeFace->strikeThroughThickness,
                        bestFace.CompositeFace->capsHeight,
                        bestFace.CompositeFace->xHeight,
                        bestFace.CompositeFace->fontStyle,
                        bestFace.CompositeFace->fontWeight,
                        bestFace.CompositeFace->fontStretch
                        );
                }
            }
            else
            {
                return new CompositeTypefaceMetrics();
            }
        }


        /// <summary>
        /// Look up device font for the typeface.
        /// </summary>
        IDeviceFont IFontFamily.GetDeviceFont(FontStyle style, FontWeight weight, FontStretch stretch)
        {
            CachedFontFace bestFace = FindExactTypeface(style, weight, stretch);
            if (!bestFace.IsNull)
            {
                unsafe
                {
                    int offsetToDeviceFont = bestFace.CompositeFace->offsetToDeviceFont;
                    if (offsetToDeviceFont != 0)
                    {
                        return new DeviceFont(
                            _cachedFamily,
                            bestFace.CheckedPointer + offsetToDeviceFont
                            );
                    }
                }
            }
            return null;
        }


        /// <summary>
        /// Find the face exactly matching the specified style, weight and stretch.
        /// Returns CachedFontFace.Null if there is no matching face.
        /// </summary>
        private CachedFontFace FindExactTypeface(FontStyle style, FontWeight weight, FontStretch stretch)
        {
            MatchingStyle target = new MatchingStyle(style, weight, stretch);

            for (int i = 0; i < _cachedFamily.NumberOfFaces; i++)
            {
                CachedFontFace currentFace = _cachedFamily.FamilyCollection.GetCachedFace(_cachedFamily, i);
                if (currentFace.MatchingStyle == target)
                {
                    return currentFace;
                }
            }

            return CachedFontFace.Null;
        }


        /// <summary>
        /// Find the face most closely matching the specified style, weight and stretch.
        /// Returns CachedFontFace.Null if there is no available face.
        /// </summary>
        private CachedFontFace FindNearestTypeface(FontStyle style, FontWeight weight, FontStretch stretch)
        {
            if (_cachedFamily.NumberOfFaces == 0)
            {
                return CachedFontFace.Null;
            }

            MatchingStyle target = new MatchingStyle(style, weight, stretch);
            CachedFontFace bestFace = _cachedFamily.FamilyCollection.GetCachedFace(_cachedFamily, 0);
            MatchingStyle bestMatch = bestFace.MatchingStyle;

            for (int i = 1; i < _cachedFamily.NumberOfFaces; i++)
            {
                CachedFontFace currentFace = _cachedFamily.FamilyCollection.GetCachedFace(_cachedFamily, i);
                MatchingStyle currentMatch = currentFace.MatchingStyle;
                if (MatchingStyle.IsBetterMatch(target, bestMatch, ref currentMatch))
                {
                    bestMatch = currentMatch;
                    bestFace = currentFace;
                }
            }

            return bestFace;
        }


        /// <summary>
        /// Get family name correspondent to the first n-characters of the specified character string
        /// </summary>
        /// <param name="unicodeString">character string</param>
        /// <param name="culture">text culture info</param>
        /// <param name="digitCulture">culture used for digit subsitution or null</param>
        /// <param name="defaultSizeInEm">default size relative to em</param>
        /// <param name="cchAdvance">number of characters advanced</param>
        /// <param name="targetFamilyName">target family name</param>
        /// <param name="scaleInEm">size relative to em</param>
        /// <returns>number of character sharing the same family name and size</returns>
        /// <remarks>
        ///
        /// Null target family name returned indicates that the font family cannot find target
        /// name of the character range being advanced.
        ///
        /// Return value false indicates that the font family has no character map.
        /// It is a font face family.
        ///
        /// </remarks>
        unsafe bool IFontFamily.GetMapTargetFamilyNameAndScale(
            CharacterBufferRange    unicodeString,
            CultureInfo             culture,
            CultureInfo             digitCulture,
            double                  defaultSizeInEm,
            out int                 cchAdvance,
            out string              targetFamilyName,
            out double              scaleInEm
            )
        {
            Invariant.Assert(unicodeString.CharacterBuffer != null && unicodeString.Length > 0);
            Invariant.Assert(culture != null);

            // Get the family map. This will find the first family map that matches
            // the specified culture, an ancestor neutral culture, or "any" culture.
            FamilyCollection.CachedFamilyMap * familyMap = GetCachedFamilyMap(
                unicodeString,
                culture,
                digitCulture,
                out cchAdvance
                );

            if (familyMap == null)
            {
                targetFamilyName = null;
                scaleInEm = 1;
            }
            else
            {
                int* sizePrefix = (int*)((byte*)familyMap + familyMap->targetFamilyNameOffset);
                targetFamilyName = Util.StringCopyFromUncheckedPointer(sizePrefix + 1, *sizePrefix);
                scaleInEm = familyMap->scaleInEm;
            }

            return true;
        }

        private unsafe FamilyCollection.CachedFamilyMap *GetCachedFamilyMap(
            CharacterBufferRange    unicodeString,
            CultureInfo             culture,
            CultureInfo             digitCulture,
            out int                 cchAdvance
            )
        {
            cchAdvance = 0;
            DigitMap digitMap = new DigitMap(digitCulture);

            int lengthOfRanges;
            ushort* ranges = _cachedFamily.FamilyCollection.GetFamilyMapRanges(
                _cachedFamily.CompositeFamily,
                culture,
                out lengthOfRanges
                );
            Debug.Assert(ranges != null);

            int sizeofChar = 0;
            int ch = 0;

            cchAdvance = Classification.AdvanceWhile(unicodeString, ItemClass.JoinerClass);
            if (cchAdvance >= unicodeString.Length)
            {
                // It is rare that the run only contains joiner characters.
                // If it really happens, just map them to the initial family map.
                return _cachedFamily.FamilyCollection.GetFamilyMapOfChar(
                    _cachedFamily.CompositeFamily,
                    ranges,
                    lengthOfRanges,
                    Classification.UnicodeScalar(unicodeString, out sizeofChar)
                    );
            }


            //
            // If the run starts with combining marks, we will not be able to find base characters for them
            // within the run. These combining marks will be mapped to their best fonts as normal characters.
            //
            ch = Classification.UnicodeScalar(
                new CharacterBufferRange(unicodeString, cchAdvance, unicodeString.Length - cchAdvance),
                out sizeofChar
                );

            bool hasBaseChar = !Classification.IsCombining(ch);

            ch = digitMap[ch];
            FamilyCollection.CachedFamilyMap* familyMap =
                _cachedFamily.FamilyCollection.GetFamilyMapOfChar(_cachedFamily.CompositeFamily, ranges, lengthOfRanges, ch);

            for (cchAdvance += sizeofChar; cchAdvance < unicodeString.Length; cchAdvance += sizeofChar)
            {
                ch = Classification.UnicodeScalar(
                    new CharacterBufferRange(unicodeString, cchAdvance, unicodeString.Length - cchAdvance),
                    out sizeofChar
                    );

                if (Classification.IsJoiner(ch))
                    continue; // continue to advance if current char is a joiner

                if (!Classification.IsCombining(ch))
                {
                    hasBaseChar = true;
                }
                else if (hasBaseChar)
                {
                    continue; // continue to advance for combining mark with base char
                }

                ch = digitMap[ch];

                if (_cachedFamily.FamilyCollection.GetFamilyMapOfChar(_cachedFamily.CompositeFamily, ranges, lengthOfRanges, ch) != familyMap)
                {
                    break;
                }
            }

            return familyMap;
        }



        #endregion Internal Methods

        //------------------------------------------------------
        //
        //  Internal Properties
        //
        //------------------------------------------------------

        #region IFontFamily Properties

        /// <summary>
        /// Font family name table indexed by culture
        /// </summary>
        IDictionary<XmlLanguage, string> IFontFamily.Names
        {
            get
            {
                return _cachedFamily.Names;
            }
        }


        /// <summary>
        /// Distance from character cell top to English baseline relative to em size.
        /// </summary>
        double IFontFamily.Baseline
        {
            get
            {
                if (_cachedFamily.Baseline != 0)
                    return _cachedFamily.Baseline;
                return GetFirstFontFamily().Baseline;
            }
        }


        /// <summary>
        /// Recommended baseline-to-baseline distance for text in this font
        /// </summary>
        double IFontFamily.LineSpacing
        {
            get
            {
                if (_cachedFamily.LineSpacing != 0)
                    return _cachedFamily.LineSpacing;
                return GetFirstFontFamily().LineSpacing;
            }
        }

        ICollection<Typeface> IFontFamily.GetTypefaces(FontFamilyIdentifier familyIdentifier)
        {
            return new TypefaceCollection(new FontFamily(familyIdentifier), _cachedFamily);
        }

        #endregion Internal Properties

        //------------------------------------------------------
        //
        //  Private Methods
        //
        //------------------------------------------------------

        #region Private Methods

        /// <summary>
        /// Get the first font family of the first target family name
        /// </summary>
        private IFontFamily GetFirstFontFamily()
        {
            if(_firstFontFamily == null)
            {
                _firstFontFamily = FontFamily.FindFontFamilyFromFriendlyNameList(GetFirstTargetFamilyName());
                Debug.Assert(_firstFontFamily != null);
            }
            return _firstFontFamily;
        }

        private string GetFirstTargetFamilyName()
        {
            unsafe
            {
                return Util.StringAndLengthCopyFromCheckedPointer(
                    _cachedFamily.CheckedPointer + _cachedFamily.CompositeFamily->OffsetToTargetFamilyNameStrings
                    );
            }
        }

        #endregion Private Methods

        #region Private Classes

        private class DeviceFont : IDeviceFont
        {
            internal DeviceFont(CachedFontFamily cachedFamily, CheckedPointer deviceFont)
            {
                unsafe
                {
                    _cachedFamily = cachedFamily;
                    _deviceFont = (FamilyCollection.CachedDeviceFont*)deviceFont.Probe(0, sizeof(FamilyCollection.CachedDeviceFont));
                    _sizeInBytes = deviceFont.Size;
                }
            }

            string IDeviceFont.Name
            {
                get
                {
                    return Util.StringAndLengthCopyFromCheckedPointer(
                        CheckedPointer + FamilyCollection.CachedDeviceFont.OffsetToLengthPrefixedName
                        );
                }
            }

            bool IDeviceFont.ContainsCharacter(int unicodeScalar)
            {
                unsafe
                {
                    return LookupMetrics(unicodeScalar) != null;
                }
            }

            unsafe void IDeviceFont.GetAdvanceWidths(
                char*   characterString,
                int     characterLength,
                double  emSize,
                int*    pAdvances
            )
            {
                unsafe
                {
                    for (int i = 0; i < characterLength; ++i)
                    {
                        FamilyCollection.CachedCharacterMetrics* metrics = LookupMetrics(characterString[i]);
                        if (metrics != null)
                        {
                            // Side bearings are included in the advance width but are not used as offsets for glyph positioning.
                            pAdvances[i] = Math.Max(0, (int)((metrics->blackBoxWidth + metrics->leftSideBearing + metrics->rightSideBearing) * emSize));
                        }
                        else
                        {
                            pAdvances[i] = 0;
                        }
                    }
                }
            }

            private unsafe FamilyCollection.CachedCharacterMetrics* LookupMetrics(int unicodeScalar)
            {
                if (unicodeScalar >= 0 && unicodeScalar <= FontFamilyMap.LastUnicodeScalar)
                {
                    int pageTableOffset = _deviceFont->OffsetToCharacterMap;

                    int* pageTable = (int*)CheckedPointer.Probe(
                        pageTableOffset,
                        CharacterMetricsDictionary.PageCount * sizeof(int)
                        );

                    int i = pageTable[unicodeScalar >> CharacterMetricsDictionary.PageShift];
                    if (i != 0)
                    {
                        int* page = (int*)CheckedPointer.Probe(
                            pageTableOffset + (i * sizeof(int)),
                            CharacterMetricsDictionary.PageSize * sizeof(int)
                            );

                        int offset = page[unicodeScalar & CharacterMetricsDictionary.PageMask];
                        if (offset != 0)
                        {
                            return (FamilyCollection.CachedCharacterMetrics*)CheckedPointer.Probe(
                                offset, 
                                sizeof(FamilyCollection.CachedCharacterMetrics)
                                );
                        }
                    }
                }
                return null;
            }

            private CheckedPointer CheckedPointer
            {
                get
                {
                    unsafe
                    {
                        return new CheckedPointer(_deviceFont, _sizeInBytes);
                    }
                }
            }

            private CachedFontFamily _cachedFamily;

            private unsafe FamilyCollection.CachedDeviceFont* _deviceFont;

            /// <summary>
            /// Critical - used for bounds checking via CheckedPointer
            /// </summary>
            private int _sizeInBytes;
        }

        #endregion

        //------------------------------------------------------
        //
        //  Private Fields
        //
        //------------------------------------------------------

        #region Private Fields

        private CachedFontFamily    _cachedFamily;

        private IFontFamily         _firstFontFamily;

        #endregion Private Fields
    }
}

