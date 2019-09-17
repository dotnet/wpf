// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
//
//
//  Contents:  Typeface 
//
//

using System;
using System.Globalization;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Security;
using System.Windows;
using System.Windows.Media.TextFormatting;
using System.Runtime.InteropServices;
using System.ComponentModel;

using MS.Internal;
using MS.Internal.FontFace;
using MS.Internal.FontCache;
using MS.Internal.Shaping;
using MS.Internal.TextFormatting;


namespace System.Windows.Media
{
    /// <summary>
    /// A Typeface is a combination of family, weight, style and stretch:
    /// </summary>
    public class Typeface
    {
        private FontFamily              _fontFamily;

        // these _style, _weight and _stretch are only used for storing what was passed into the constructor.
        // Since FontFamily may change these values when it includes a style name implicitly,
        private FontStyle               _style;
        private FontWeight              _weight;
        private FontStretch             _stretch;        

        private FontFamily              _fallbackFontFamily;

        // Cached canonical values of the typeface. 
        private CachedTypeface         _cachedTypeface;        


        /// <summary>
        /// Construct a typeface
        /// </summary>
        /// <param name="typefaceName">font typeface name</param>
        public Typeface(
            string typefaceName
            ) 
            // assume face name is family name until we get face name resolved properly.
            : this(
                new FontFamily(typefaceName),   
                FontStyles.Normal, 
                FontWeights.Normal, 
                FontStretches.Normal
                )
        {}



        /// <summary>
        /// Construct a typeface
        /// </summary>
        /// <param name="fontFamily">Font family</param>
        /// <param name="style">Font style</param>
        /// <param name="weight">Boldness of font</param>
        /// <param name="stretch">Width of characters</param>
        public Typeface(
            FontFamily      fontFamily,
            FontStyle       style,
            FontWeight      weight,
            FontStretch     stretch
            )
            : this(
                fontFamily,
                style,
                weight,
                stretch,
                FontFamily.FontFamilyGlobalUI
                )
        {}



        /// <summary>
        /// Construct a typeface
        /// </summary>
        /// <param name="fontFamily">Font family</param>
        /// <param name="style">Font style</param>
        /// <param name="weight">Boldness of font</param>
        /// <param name="stretch">Width of characters</param>
        /// <param name="fallbackFontFamily">fallback font family</param>
        public Typeface(
            FontFamily      fontFamily,
            FontStyle       style,
            FontWeight      weight,
            FontStretch     stretch,
            FontFamily      fallbackFontFamily
            )
        {
            if(fontFamily == null)
            {
                throw new ArgumentNullException("fontFamily");
            }

            _fontFamily = fontFamily;
            _style = style;
            _weight = weight;
            _stretch = stretch;
            _fallbackFontFamily = fallbackFontFamily;
        }



        /// <summary>
        /// Font family
        /// </summary>
        public FontFamily FontFamily
        {
            get { return _fontFamily; }
        }


        /// <summary>
        /// Font weight (light, bold, etc.)
        /// </summary>
        public FontWeight Weight
        {
            get { return _weight; }
        }


        /// <summary>
        /// Font style (italic, oblique)
        /// </summary>
        public FontStyle Style
        {
            get { return _style; }
        }


        /// <summary>
        /// Font Stretch (narrow, wide, etc.)
        /// </summary>
        public FontStretch Stretch
        {
            get { return _stretch; }
        }

        /// <summary>
        /// Returns true if FontStyle.Oblique is algorithmically simulated by
        /// slanting glyphs. Returns false otherwise.
        /// </summary>
        public bool IsObliqueSimulated
        {
            get
            {
                return (CachedTypeface.TypefaceMetrics.StyleSimulations & StyleSimulations.ItalicSimulation) != 0;
            }
        }

        /// <summary>
        /// Returns true if FontStyle.Bold is algorithmically simulated by
        /// thickening glyphs. Returns false otherwise.
        /// </summary>
        public bool IsBoldSimulated
        {
            get
            {
                return (CachedTypeface.TypefaceMetrics.StyleSimulations & StyleSimulations.BoldSimulation) != 0;
            }
        }

        /// <summary>
        /// Obtain a glyph typeface that corresponds to the Typeface object constructed from an OpenType font family.
        /// If the Typeface was constructed from a composite font family, returns null.
        /// </summary>
        /// <param name="glyphTypeface">GlyphTypeface object that corresponds to this Typeface, or null if the Typeface
        /// was constructed from a composite font.</param>
        /// <returns>Whether glyphTypeface is not null.</returns>
        public bool TryGetGlyphTypeface(out GlyphTypeface glyphTypeface)
        {
            glyphTypeface = CachedTypeface.TypefaceMetrics as GlyphTypeface;
            return glyphTypeface != null;
        }


        /// <summary>
        /// Fallback font family
        /// </summary>
        internal FontFamily FallbackFontFamily
        {
            get { return _fallbackFontFamily; }
        }

        /// <summary>
        /// (Western) x-height relative to em size.
        /// </summary>
        public double XHeight
        {
            get 
            { 
                return CachedTypeface.TypefaceMetrics.XHeight;
            }
        }


        /// <summary>
        /// Distance from baseline to top of English ----, relative to em size.
        /// </summary>
        public double CapsHeight
        {
            get 
            { 
                return CachedTypeface.TypefaceMetrics.CapsHeight;
            }
        }


        /// <summary>
        /// Distance from baseline to underline position
        /// </summary>
        public double UnderlinePosition
        {
            get 
            { 
                return CachedTypeface.TypefaceMetrics.UnderlinePosition;
            }
        }


        /// <summary>
        /// Underline thickness
        /// </summary>
        public double UnderlineThickness
        {
            get
            {
                return CachedTypeface.TypefaceMetrics.UnderlineThickness;
            }
        }


        /// <summary>
        /// Distance from baseline to strike-through position
        /// </summary>
        public double StrikethroughPosition
        {
            get 
            { 
                return CachedTypeface.TypefaceMetrics.StrikethroughPosition;
            }
        }


        /// <summary>
        /// strike-through thickness
        /// </summary>
        public double StrikethroughThickness
        {
            get
            {
                return CachedTypeface.TypefaceMetrics.StrikethroughThickness;
            }
        }

        /// <summary>
        /// Collection of culture-dependant face names.
        /// </summary>
        public LanguageSpecificStringDictionary FaceNames
        {
            get
            {
                return new LanguageSpecificStringDictionary(CachedTypeface.TypefaceMetrics.AdjustedFaceNames);
            }
        }

        internal double Baseline(double emSize, double toReal, double pixelsPerDip, TextFormattingMode textFormattingMode)
        {
            return CachedTypeface.FirstFontFamily.Baseline(emSize, toReal, pixelsPerDip, textFormattingMode);            
        }

        internal double LineSpacing(double emSize, double toReal, double pixelsPerDip, TextFormattingMode textFormattingMode)
        {
            return CachedTypeface.FirstFontFamily.LineSpacing(emSize, toReal, pixelsPerDip, textFormattingMode);
        }

        /// <summary>
        /// Flag indicating if the typeface is of symbol type
        /// </summary>
        internal bool Symbol
        {
            get
            {
                return CachedTypeface.TypefaceMetrics.Symbol;
            }
        }        

        internal bool NullFont
        {
            get 
            {
                return CachedTypeface.NullFont;
            }
        }

        // Tries to get a GlyphTypeface based on the Typeface properties. The 
        // return value can be null. However, if CheckFastPathNominalGlyphs
        // returns true, then one can expect this function to return a valid
        // GlyphTypeface that maps all the specified text.
        internal GlyphTypeface TryGetGlyphTypeface()
        {
            return CachedTypeface.TypefaceMetrics as GlyphTypeface;
        }

        internal FontStyle CanonicalStyle
        {
            get 
            {
                return CachedTypeface.CanonicalStyle;
            }
        }

        internal FontWeight CanonicalWeight
        {
            get 
            {
                return CachedTypeface.CanonicalWeight;
            }
        }

        internal FontStretch CanonicalStretch
        {
            get 
            {
                return CachedTypeface.CanonicalStretch;
            }
        }
        
       
        /// <summary>
        /// Scan through specified character string checking for valid character 
        /// nominal glyph.
        /// </summary>
        /// <param name="charBufferRange">character buffer range</param>
        /// <param name="emSize">height of Em</param>
        /// <param name="scalingFactor">This is the factor by which we will scale up 
        /// the metrics. Typically this value to used to convert metrics from the real 
        /// space to the ideal space</param>
        /// <param name="widthMax">maximum width allowed</param>
        /// <param name="keepAWord">do not stop arbitrarily in the middle of a word</param>
        /// <param name="numberSubstitution">digits require complex shaping</param>
        /// <param name="cultureInfo">CultureInfo of the text</param>
        /// <param name="textFormattingMode">The TextFormattingMode used (Ideal vs. Display)</param>
        /// <param name="isSideways">Indicates whether to rotate glyphs.</param>
        /// <param name="breakOnTabs">Determines whether to stop checking at a tab and 
        /// break the run there</param>
        /// <param name="stringLengthFit">number of character fit in given width</param>
        /// <returns>whether the specified string can be optimized by nominal glyph lookup</returns>
        internal bool CheckFastPathNominalGlyphs(
            CharacterBufferRange    charBufferRange,
            double                  emSize,
            float                   pixelsPerDip,
            double                  scalingFactor,
            double                  widthMax,
            bool                    keepAWord,
            bool                    numberSubstitution,
            CultureInfo             cultureInfo,
            TextFormattingMode      textFormattingMode,
            bool                    isSideways,
            bool                    breakOnTabs,
            out int                 stringLengthFit
            )
        {
            stringLengthFit = 0;

            if (CachedTypeface.NullFont) return false;

            GlyphTypeface glyphTypeface = TryGetGlyphTypeface();
            
            if (glyphTypeface == null) return false;

            double totalWidth = 0;
            int i = 0;

            ushort blankGlyph = glyphTypeface.BlankGlyphIndex;
            ushort glyph = blankGlyph;

            ushort charFlagsMask = numberSubstitution ?
                (ushort)(CharacterAttributeFlags.CharacterComplex | CharacterAttributeFlags.CharacterDigit) :
                (ushort)CharacterAttributeFlags.CharacterComplex;
            ushort charFlags = 0;
            ushort charFastTextCheck = (ushort)(CharacterAttributeFlags.CharacterFastText | CharacterAttributeFlags.CharacterIdeo);

            bool symbolTypeface = glyphTypeface.Symbol;
            if (symbolTypeface)
            {
                // we don't care what code points are present if it's a non-Unicode font such as Symbol or Wingdings;
                // the code points don't have any standardized meanings, and we always want to bypass shaping
                charFlagsMask = 0;
            }

            bool ignoreWidths = widthMax == double.MaxValue;
            ushort[] glyphIndices = BufferCache.GetUShorts(charBufferRange.Length);
            MS.Internal.Text.TextInterface.GlyphMetrics[] glyphMetrics = ignoreWidths ? null : BufferCache.GetGlyphMetrics(charBufferRange.Length);

            glyphTypeface.GetGlyphMetricsOptimized(charBufferRange, 
                                                   emSize,
                                                   pixelsPerDip,
                                                   glyphIndices,
                                                   glyphMetrics,
                                                   textFormattingMode,
                                                   isSideways
                                                   );

            double designToEm = emSize / glyphTypeface.DesignEmHeight;

            //
            // This block will advance until one of:
            // 1. The end of the charBufferRange is reached
            // 2. The charFlags have some of the charFlagsMask values
            // 3. The glyph is BlankGlyph (space)
            // 4. Glyph index is 0 (unless symbol font)
            //
            // At this point totalWidth includes all of the widths including the stop character (which fits above)
            // i indexes the next character (not included in the width)
            //
            if(keepAWord)
            {
                do
                {
                    char ch = charBufferRange[i++];
                    if (ch == TextStore.CharLineFeed || ch == TextStore.CharCarriageReturn || (breakOnTabs && ch == TextStore.CharTab))
                    {
                        --i;
                        break;
                    }
                    else
                    {
                        int charClass = (int)Classification.GetUnicodeClassUTF16(ch);
                        charFlags = Classification.CharAttributeOf(charClass).Flags;
                        charFastTextCheck &= charFlags;

                        glyph = glyphIndices[i-1];
                        if (!ignoreWidths)
                        {
                            totalWidth += TextFormatterImp.RoundDip(glyphMetrics[i - 1].AdvanceWidth * designToEm, pixelsPerDip, textFormattingMode) * scalingFactor;
                        }
                    }
                } while(
                        i < charBufferRange.Length
                    &&  ((charFlags & charFlagsMask) == 0)
                    &&  (glyph != 0 || symbolTypeface)
                    &&  glyph != blankGlyph
                    );

                // i is now at a character immediately following a leading blank
            }

            //
            // This block will advance until one of:
            // 1. The end of the charBufferRange is reached
            // 2. The charFlags have some of the charFlagsMask values
            // 3. Glyph index is 0 (unless symbol font)
            // 4. totalWidth > widthMax
            //
            
            while(
                    i < charBufferRange.Length
                &&  (ignoreWidths || totalWidth <= widthMax)
                &&  ((charFlags & charFlagsMask) == 0)
                &&  (glyph != 0 || symbolTypeface)
                )
            {
                char ch = charBufferRange[i++];
                if (ch == TextStore.CharLineFeed || ch == TextStore.CharCarriageReturn || (breakOnTabs && ch == TextStore.CharTab))
                {
                    --i;
                    break;
                }
                else
                {
                    int charClass = (int)Classification.GetUnicodeClassUTF16(ch);
                    charFlags = Classification.CharAttributeOf(charClass).Flags;
                    charFastTextCheck &= charFlags;

                    glyph = glyphIndices[i-1];
                    if (!ignoreWidths)
                    {
                        totalWidth += TextFormatterImp.RoundDip(glyphMetrics[i - 1].AdvanceWidth * designToEm, pixelsPerDip, textFormattingMode) * scalingFactor;
                    }
                }
            }

            BufferCache.ReleaseUShorts(glyphIndices);
            glyphIndices = null;
            BufferCache.ReleaseGlyphMetrics(glyphMetrics);
            glyphMetrics = null;

            if (symbolTypeface)
            {
                // always optimize for non-Unicode font as we don't support shaping or typographic features;
                // we also don't fall back from non-Unicode fonts so we don't care if there are missing glyphs
                stringLengthFit = i;
                return true;
            }

            if (glyph == 0)
            {
                // character is not supported by the font
                return false;
            }

            if ((charFlags & charFlagsMask) != 0)
            {
                // complex character encountered, exclude it
                Debug.Assert(i > 0);

                if(--i <= 0)
                {
                    // first char is complex, fail the call
                    return false;
                }
            }

            stringLengthFit = i;
            TypographyAvailabilities typography = glyphTypeface.FontFaceLayoutInfo.TypographyAvailabilities;

            if ((charFastTextCheck & (byte) CharacterAttributeFlags.CharacterFastText) != 0)
            {
                // all input code points are Fast Text
                if ((typography &
                         (  TypographyAvailabilities.FastTextTypographyAvailable
                          | TypographyAvailabilities.FastTextMajorLanguageLocalizedFormAvailable
                         )
                     ) != 0
                   )
                {
                    // Considered too risky to optimize. It is either because the font
                    // has required features or the font has 'locl' lookup for major languages.
                    return false;
                }
                else if ((typography & TypographyAvailabilities.FastTextExtraLanguageLocalizedFormAvailable) != 0)
                {
                    // The font has 'locl' lookup for FastText code points for non major languages.
                    // Check whether the input is major langauge. If it is, we are still good to optimize.
                    return MajorLanguages.Contains(cultureInfo);
                }
                else
                {
                    // No FastText flags are present, safe to optimize
                    return true;
                }
            }
            else if ((charFastTextCheck & (byte) CharacterAttributeFlags.CharacterIdeo) != 0)
            {
                // The input are all ideographs, check the IdeoTypographyAvailable bit. It is safe if
                // the bit is not set.
                return ((typography & TypographyAvailabilities.IdeoTypographyAvailable) == 0);
            }
            else
            {
                // for all the rest of the cases, just check whether there is any required typography
                // present at all. If none exists, it is optimizable. We might under-optimize here but
                // it will be non-major languages.
                return ((typography & TypographyAvailabilities.Available) == 0);
            }            
        }

        /// <summary>
        /// Lookup characters nominal glyphs and width
        /// </summary>
        /// <param name="charBufferRange">character buffer range</param>
        /// <param name="emSize">height of Em</param>
        /// <param name="toIdeal"> scaling factor from real to ideal unit </param>
        /// <param name="nominalWidths">glyph nominal advances in ideal units</param>
        /// <returns>true for success</returns>
        /// <remarks>This function is only used in fast path, and can only be called 
        /// if CheckFastPathNominalGlyphs has previously returned true.</remarks>
        internal void GetCharacterNominalWidthsAndIdealWidth(
            CharacterBufferRange charBufferRange,
            double               emSize,
            float                pixelsPerDip,
            double               toIdeal,
            TextFormattingMode   textFormattingMode,
            bool                 isSideways,
            out int[]            nominalWidths
            )
        {
            int idealWidth;
            GetCharacterNominalWidthsAndIdealWidth(
                                charBufferRange,
                                emSize,
                                pixelsPerDip,
                                toIdeal,
                                textFormattingMode,
                                isSideways,
                                out nominalWidths,
                                out idealWidth
                                );
        }

        /// <summary>
        /// Lookup characters nominal glyphs and width
        /// </summary>
        /// <param name="charBufferRange">character buffer range</param>
        /// <param name="emSize">height of Em</param>
        /// <param name="toIdeal"> scaling factor from real to ideal unit </param>
        /// <param name="nominalWidths">glyph nominal advances in ideal units</param>
        /// <param name="idealWidth">total width in ideal units</param>
        /// <returns>true for success</returns>
        /// <remarks>This function is only used in fast path, and can only be called 
        /// if CheckFastPathNominalGlyphs has previously returned true.</remarks>
        internal void GetCharacterNominalWidthsAndIdealWidth(
            CharacterBufferRange charBufferRange,
            double               emSize,
            float                pixelsPerDip,
            double               toIdeal,
            TextFormattingMode   textFormattingMode,
            bool                 isSideways,
            out int[]            nominalWidths,
            out int              idealWidth
            )
        {
            // This function should only be called if CheckFastPathNominalGlyphs has
            // returned true so we can assume the ITypefaceMetrics is a GlyphTypeface.
            GlyphTypeface glyphTypeface = TryGetGlyphTypeface();
            Invariant.Assert(glyphTypeface != null);

            MS.Internal.Text.TextInterface.GlyphMetrics[] glyphMetrics = BufferCache.GetGlyphMetrics(charBufferRange.Length);

            glyphTypeface.GetGlyphMetricsOptimized(charBufferRange, 
                                                   emSize,
                                                   pixelsPerDip,
                                                   textFormattingMode,
                                                   isSideways,
                                                   glyphMetrics);

            nominalWidths = new int[charBufferRange.Length];
            idealWidth = 0;

            if (TextFormattingMode.Display == textFormattingMode)
            {
                double designToEm = emSize / glyphTypeface.DesignEmHeight;
                for (int i = 0; i < charBufferRange.Length; i++)
                {
                    nominalWidths[i] = (int)Math.Round(TextFormatterImp.RoundDipForDisplayMode(glyphMetrics[i].AdvanceWidth * designToEm, pixelsPerDip) * toIdeal);
                    idealWidth += nominalWidths[i];
                }
            }
            else
            {
                double designToEm = emSize * toIdeal / glyphTypeface.DesignEmHeight;
                for (int i = 0; i < charBufferRange.Length; i++)
                {
                    nominalWidths[i] = (int)Math.Round(glyphMetrics[i].AdvanceWidth * designToEm);
                    idealWidth += nominalWidths[i];
                }
            }

            BufferCache.ReleaseGlyphMetrics(glyphMetrics);
        }

        /// <summary>
        /// Create correspondent hash code for the object
        /// </summary>
        /// <returns>object hash code</returns>
        public override int GetHashCode()
        {
            int hash = _fontFamily.GetHashCode();

            if (_fallbackFontFamily != null)
                hash = HashFn.HashMultiply(hash) + _fallbackFontFamily.GetHashCode();

            hash = HashFn.HashMultiply(hash) + _style.GetHashCode();
            hash = HashFn.HashMultiply(hash) + _weight.GetHashCode();
            hash = HashFn.HashMultiply(hash) + _stretch.GetHashCode();
            return HashFn.HashScramble(hash);
        }



        /// <summary>
        /// Equality check
        /// </summary>
        public override bool Equals(object o)
        {
            Typeface t = o as Typeface;
            if(t == null)
                return false;

            return  _style == t._style 
                &&  _weight == t._weight 
                &&  _stretch == t._stretch 
                &&  _fontFamily.Equals(t._fontFamily)
                &&  CompareFallbackFontFamily(t._fallbackFontFamily);
        }


        internal bool CompareFallbackFontFamily(FontFamily fallbackFontFamily)
        {
            if (fallbackFontFamily == null || _fallbackFontFamily == null)
                return fallbackFontFamily == _fallbackFontFamily;

            return _fallbackFontFamily.Equals(fallbackFontFamily);
        }

        //----------------------------------------
        // Private method
        //----------------------------------------
        private CachedTypeface CachedTypeface
        {
            get 
            {
                if (_cachedTypeface == null)
                {
                    CachedTypeface cachedTypeface = TypefaceMetricsCache.ReadonlyLookup(this) as CachedTypeface;
                    
                    if (cachedTypeface == null)
                    {
                       cachedTypeface = ConstructCachedTypeface();
                       TypefaceMetricsCache.Add(this, cachedTypeface);
                    }

                    // For thread-safety, set the _cachedTypeface field only after we have a fully 
                    // initialized CachedTypeface object.
                    _cachedTypeface = cachedTypeface;
                }

                return _cachedTypeface;
            }
        }        

        private CachedTypeface ConstructCachedTypeface()
        {
            FontStyle canonicalStyle     = _style;
            FontWeight canonicalWeight   = _weight;
            FontStretch canonicalStretch = _stretch;

            //
            // We always call FontFamily.FindFirstFontFamilyAndFace() method to resolve the 
            // canonical styles since the implied styles in FontFamily name will override 
            // the given styles in the Typeface. But we don't always use the IFontFamily 
            // instance returned from this method because an equal instance might already be 
            // cached. 
            //
            FontFamily sourceFontFamily = FontFamily;
            
            IFontFamily firstFontFamily = sourceFontFamily.FindFirstFontFamilyAndFace(
                ref canonicalStyle,
                ref canonicalWeight,
                ref canonicalStretch
                );

            if (firstFontFamily == null)
            {
                if (FallbackFontFamily != null)
                {
                    sourceFontFamily = FallbackFontFamily;
                    firstFontFamily = sourceFontFamily.FindFirstFontFamilyAndFace(
                        ref canonicalStyle,
                        ref canonicalWeight,
                        ref canonicalStretch
                        );                  
                }

                if (firstFontFamily == null)
                {
                    sourceFontFamily = null;
                    firstFontFamily = FontFamily.LookupFontFamily(FontFamily.NullFontFamilyCanonicalName);
                }                
            }

            // If it's a named font, map all occurrences of the same name to one cached IFontFamily.
            if (sourceFontFamily != null && sourceFontFamily.Source != null)
            {
                // We lookup in the cache to see if there is cached IFontFamily instance of the source FontFamily. Otherwise, 
                // this IFontFamily value is added to the TypefaceMetrics cache. 
                IFontFamily cachedValue = TypefaceMetricsCache.ReadonlyLookup(sourceFontFamily.FamilyIdentifier) as IFontFamily;
                
                if (cachedValue != null)
                {
                    firstFontFamily = cachedValue;
                }
                else
                {
                    TypefaceMetricsCache.Add(sourceFontFamily.FamilyIdentifier, firstFontFamily);   
                }                
            }

            ITypefaceMetrics typefaceMetrics = firstFontFamily.GetTypefaceMetrics(canonicalStyle, canonicalWeight, canonicalStretch);            

            return new CachedTypeface(
                canonicalStyle,
                canonicalWeight,
                canonicalStretch,
                firstFontFamily,
                typefaceMetrics,
                sourceFontFamily == null
                );
        }
    }
}
