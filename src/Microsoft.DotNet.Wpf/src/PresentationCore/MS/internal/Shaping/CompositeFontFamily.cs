// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
//
//
//  Contents:  Composite font family
//
//

using System;
using System.Globalization;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Security;
using System.Windows;
using System.Windows.Markup;    // for XmlLanguage
using System.Windows.Media;
using System.Windows.Media.TextFormatting;

using MS.Internal.FontCache;
using MS.Internal.FontFace;
using MS.Internal.TextFormatting;
using FontFamily = System.Windows.Media.FontFamily;
using SR=MS.Internal.PresentationCore.SR;
using SRID=MS.Internal.PresentationCore.SRID;

namespace MS.Internal.Shaping
{
    /// <summary>
    /// Composite font family
    /// </summary>
    internal sealed class CompositeFontFamily : IFontFamily
    {
        private readonly CompositeFontInfo   _fontInfo;

        private IFontFamily         _firstFontFamily;

        #region Constructors

        /// <summary>
        /// Construct a default composite font family
        /// </summary>
        internal CompositeFontFamily()
            : this(new CompositeFontInfo())
        {}


        /// <summary>
        /// Construct a composite font family from composite font info
        /// </summary>
        internal CompositeFontFamily(CompositeFontInfo fontInfo)
        {
            _fontInfo = fontInfo;
        }


        /// <summary>
        /// Construct a composite font family with a single target family name
        /// </summary>
        internal CompositeFontFamily(
            string      friendlyName
            ) :
            this(
                friendlyName,
                null    // firstFontFamily
                )
        {}


        /// <summary>
        /// Construct a composite font family with a single target family name
        /// after the first font family in the target family is known
        /// </summary>
        internal CompositeFontFamily(
            string          friendlyName,
            IFontFamily     firstFontFamily
            ) :
            this()
        {
            FamilyMaps.Add(
                new FontFamilyMap(
                    0, FontFamilyMap.LastUnicodeScalar,
                    null, // any language
                    friendlyName,
                    1     // scaleInEm
                    )
                );


            _firstFontFamily = firstFontFamily;
        }

        #endregion

        #region IFontFamily properties


        /// <summary>
        /// Font family name table indexed by culture
        /// </summary>
        IDictionary<XmlLanguage, string> IFontFamily.Names
        {
            get
            {
                return _fontInfo.FamilyNames;
            }
        }

        /// <summary>
        /// Distance from character cell top to English baseline relative to em size.
        /// </summary>
        public double Baseline(double emSize, double toReal, double pixelsPerDip, TextFormattingMode textFormattingMode)
        {
            if (textFormattingMode == TextFormattingMode.Ideal)
            {
                return ((IFontFamily)this).BaselineDesign * emSize;
            }
            else
            {
                // If the composite font has a pre specified Baseline then we respect it in calculating the 
                // baseline but we round it since Compatible metrics are pixel aligned.
                if (_fontInfo.Baseline != 0)
                {
                    return Math.Round(_fontInfo.Baseline * emSize);
                }
                // If the composite font has no specifed Baseline then we get the compatible font metrics of the
                // first fontfamily in the composite font.
                else
                {
                    return GetFirstFontFamily().Baseline(emSize, toReal, pixelsPerDip, textFormattingMode);             
                }
            }
}

        public void SetBaseline(double value)
        {
            _fontInfo.Baseline = value;
        }

        /// <summary>
        /// Recommended baseline-to-baseline distance for text in this font.
        /// </summary>
        public double LineSpacing(double emSize, double toReal, double pixelsPerDip, TextFormattingMode textFormattingMode)
        {            
            if (textFormattingMode == TextFormattingMode.Ideal)
            {
                return ((IFontFamily)this).LineSpacingDesign * emSize;
            }
            else
            {
                // If the composite font has a pre specified LineSpacing then we respect it in calculating the 
                // linespacing but we round it since Compatible metrics are pixel aligned.
                if (_fontInfo.LineSpacing != 0)
                {
                    return Math.Round(_fontInfo.LineSpacing * emSize);
                }
                // If the composite font has no specifed LineSpacing then we get the compatible font metrics of the
                // first fontfamily in the composite font.
                else
                {
                    return GetFirstFontFamily().LineSpacing(emSize, toReal, pixelsPerDip, textFormattingMode);
                }
            }         
        }

        double IFontFamily.BaselineDesign
        {
            get
            {
                if (_fontInfo.Baseline == 0)
                {
                    _fontInfo.Baseline = GetFirstFontFamily().BaselineDesign;
                }
                return _fontInfo.Baseline;   
            }
        }


        double IFontFamily.LineSpacingDesign
        {
            get
            {
                if (_fontInfo.LineSpacing == 0)
                {
                    _fontInfo.LineSpacing = GetFirstFontFamily().LineSpacingDesign;
                }
                return _fontInfo.LineSpacing; 
            }
        }

        public void SetLineSpacing(double value)
        {
            _fontInfo.LineSpacing = value;
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
            if (_fontInfo.FamilyTypefaces == null &&
                _fontInfo.FamilyMaps.Count == 1 &&
                _fontInfo.FamilyMaps[0].IsSimpleFamilyMap)
            {
                // Typical e.g. "MyFont, sans-serif"
                return GetFirstFontFamily().GetTypefaceMetrics(style, weight, stretch);
            }

            return FindTypefaceMetrics(style, weight, stretch);
        }


        /// <summary>
        /// Look up device font for the typeface.
        /// </summary>
        IDeviceFont IFontFamily.GetDeviceFont(FontStyle style, FontWeight weight, FontStretch stretch)
        {
            FamilyTypeface bestFace = FindExactFamilyTypeface(style, weight, stretch);

            if (bestFace != null && bestFace.DeviceFontName != null)
                return bestFace;
            else
                return null;
        }


        /// <summary>
        /// Get family name correspondent to the first n-characters of the specified character string
        /// </summary>
        bool IFontFamily.GetMapTargetFamilyNameAndScale(
            CharacterBufferRange unicodeString,
            CultureInfo culture,
            CultureInfo digitCulture,
            double defaultSizeInEm,
            out int cchAdvance,
            out string targetFamilyName,
            out double scaleInEm
            )
        {
            Invariant.Assert(unicodeString.CharacterBuffer != null && unicodeString.Length > 0);
            Invariant.Assert(culture != null);

            // Get the family map. This will find the first family map that matches
            // the specified culture, an ancestor neutral culture, or "any" culture.
            FontFamilyMap familyMap = GetTargetFamilyMap(
                unicodeString,
                culture,
                digitCulture,
                out cchAdvance
                );

            // Return the values for the matching FontFamilyMap. If there is none this is
            // FontFamilyMap.Default which has Target == null and Scale == 1.0.
            targetFamilyName = familyMap.Target;
            scaleInEm = familyMap.Scale;

            return true;
        }

        ICollection<Typeface> IFontFamily.GetTypefaces(FontFamilyIdentifier familyIdentifier)
        {
            return new TypefaceCollection(new FontFamily(familyIdentifier), FamilyTypefaces);
        }

        #endregion

        #region collections exposed by FontFamily

        internal LanguageSpecificStringDictionary FamilyNames
        {
            get { return _fontInfo.FamilyNames; }
        }

        internal FamilyTypefaceCollection FamilyTypefaces
        {
            get { return _fontInfo.GetFamilyTypefaceList(); }
        }

        internal FontFamilyMapCollection FamilyMaps
        {
            get { return _fontInfo.FamilyMaps; }
        }

        #endregion

        private FontFamilyMap GetTargetFamilyMap(
            CharacterBufferRange unicodeString,
            CultureInfo culture,
            CultureInfo digitCulture,
            out int cchAdvance
            )
        {
            DigitMap digitMap = new DigitMap(digitCulture);
            ushort[] familyMaps = _fontInfo.GetFamilyMapsOfLanguage(XmlLanguage.GetLanguage(culture.IetfLanguageTag));

            int sizeofChar = 0;
            int ch = 0;

            // skip all the leading joinder characters. They need to be shaped with the 
            // surrounding strong characters.
            cchAdvance = Classification.AdvanceWhile(unicodeString, ItemClass.JoinerClass);
            
            if (cchAdvance >= unicodeString.Length)            
            {
                // It is rare that the run only contains joiner characters.
                // If it really happens, just map them to the initial family map. 
                return _fontInfo.GetFamilyMapOfChar(
                    familyMaps, 
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
            FontFamilyMap familyMap = _fontInfo.GetFamilyMapOfChar(familyMaps, ch);
                
            Invariant.Assert(familyMap != null);
            
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

                if (_fontInfo.GetFamilyMapOfChar(familyMaps, ch) != familyMap)
                    break;
            }

            return familyMap;
        }


        /// <summary>
        /// Get the first font family of the first target family name
        /// </summary>
        private IFontFamily GetFirstFontFamily()
        {
            if(_firstFontFamily == null)
            {
                if (_fontInfo.FamilyMaps.Count != 0)
                {
                    _firstFontFamily = FontFamily.FindFontFamilyFromFriendlyNameList(_fontInfo.FamilyMaps[0].Target);
                }
                else
                {
                    _firstFontFamily = FontFamily.LookupFontFamily(FontFamily.NullFontFamilyCanonicalName);
                }

                Invariant.Assert(_firstFontFamily != null);
            }

            return _firstFontFamily;
        }

        private ITypefaceMetrics FindTypefaceMetrics(
            FontStyle       style,
            FontWeight      weight,
            FontStretch     stretch
            )
        {
            FamilyTypeface bestFace = FindNearestFamilyTypeface(style, weight, stretch);

            if (bestFace == null)
                return new CompositeTypefaceMetrics();
            else
                return bestFace;
        }


        /// <summary>
        /// Find the face closest to the specified style, weight and stretch.
        /// Returns null if there is no matching face.
        /// </summary>
        private FamilyTypeface FindNearestFamilyTypeface(
            FontStyle style,
            FontWeight weight,
            FontStretch stretch
            )
        {
            if (_fontInfo.FamilyTypefaces == null || _fontInfo.FamilyTypefaces.Count == 0)
            {
                return null;
            }

            FamilyTypeface bestFace = (FamilyTypeface)_fontInfo.FamilyTypefaces[0];
            MatchingStyle bestMatch = new MatchingStyle(bestFace.Style, bestFace.Weight, bestFace.Stretch);
            MatchingStyle target = new MatchingStyle(style, weight, stretch);

            for (int i = 1; i < _fontInfo.FamilyTypefaces.Count; i++)
            {
                FamilyTypeface currentFace = (FamilyTypeface)_fontInfo.FamilyTypefaces[i];
                MatchingStyle currentMatch = new MatchingStyle(currentFace.Style, currentFace.Weight, currentFace.Stretch);
                if (MatchingStyle.IsBetterMatch(target, bestMatch, ref currentMatch))
                {
                    bestFace = currentFace;
                    bestMatch = currentMatch;
                }
            }

            return bestFace;
        }


        /// <summary>
        /// Find the face exactly matching the specified style, weight and stretch.
        /// Returns null if there is no matching face.
        /// </summary>
        private FamilyTypeface FindExactFamilyTypeface(
            FontStyle style,
            FontWeight weight,
            FontStretch stretch
            )
        {
            if (_fontInfo.FamilyTypefaces == null || _fontInfo.FamilyTypefaces.Count == 0)
            {
                return null;
            }

            MatchingStyle target = new MatchingStyle(style, weight, stretch);

            foreach (FamilyTypeface currentFace in _fontInfo.FamilyTypefaces)
            {
                MatchingStyle  currentMatch = new MatchingStyle(currentFace.Style, currentFace.Weight, currentFace.Stretch);
                if (currentMatch == target)
                {
                    return currentFace;
                }
            }

            return null;
        }
    }
}
