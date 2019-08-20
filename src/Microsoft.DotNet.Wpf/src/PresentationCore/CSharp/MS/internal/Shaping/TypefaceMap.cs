// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
//
// Description: TypefaceMap class
//
//

using System;
using System.Security;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Diagnostics;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.TextFormatting;
using MS.Utility;
using MS.Internal;
using MS.Internal.Generic;
using MS.Internal.FontCache;
using MS.Internal.FontFace;
using MS.Internal.TextFormatting;
using System.Runtime.InteropServices;

using FontFace = MS.Internal.FontFace;


namespace MS.Internal.Shaping
{
    /// <summary>
    /// TypefaceMap class. It maps characters to corresponding ShapeTypeface through font linking logic.
    /// It also caches the mapping results such that same code points doesn't need to go through all
    /// the font linking process again.
    /// </summary>
    internal class TypefaceMap
    {
        private FontFamily[]                     _fontFamilies;
        private FontStyle                        _canonicalStyle;
        private FontWeight                       _canonicalWeight;
        private FontStretch                      _canonicalStretch;
        private bool                             _nullFont;

        private IList<ScaledShapeTypeface>       _cachedScaledTypefaces = new List<ScaledShapeTypeface>(InitialScaledGlyphableTypefaceCount);
        private IDictionary<CultureInfo, IntMap> _intMaps               = new Dictionary<CultureInfo, IntMap>();

        // Constants
        private const int InitialScaledGlyphableTypefaceCount = 2;
        private const int MaxTypefaceMapDepths                = 32;

        internal TypefaceMap(
            FontFamily           fontFamily,
            FontFamily           fallbackFontFamily,
            FontStyle            canonicalStyle,
            FontWeight           canonicalWeight,
            FontStretch          canonicalStretch,
            bool                 nullFont
            )
        {
            Invariant.Assert(fontFamily != null);

            _fontFamilies = fallbackFontFamily == null ?
                 new FontFamily[] { fontFamily }
               : new FontFamily[] { fontFamily, fallbackFontFamily };

             _canonicalStyle   = canonicalStyle;
             _canonicalWeight  = canonicalWeight;
             _canonicalStretch = canonicalStretch;
             _nullFont         = nullFont;
        }

        /// <summary>
        /// Compute a list of shapeable text objects for the specified character string
        /// </summary>
        internal void GetShapeableText(
            CharacterBufferReference    characterBufferReference,
            int                         stringLength,
            TextRunProperties           textRunProperties,
            CultureInfo                 digitCulture,
            bool                        isRightToLeftParagraph,
            IList<TextShapeableSymbols> shapeableList,
            IShapeableTextCollector     collector,
            TextFormattingMode          textFormattingMode
            )
        {
            SpanVector<int> cachedScaledTypefaceIndexSpans;
            
            int ichItem = 0;

            CharacterBufferRange unicodeString = new CharacterBufferRange(
                characterBufferReference,
                stringLength
                );

            CultureInfo culture = textRunProperties.CultureInfo;
            IList<Span> spans;
            
            GCHandle gcHandle;
            IntPtr ptext = characterBufferReference.CharacterBuffer.PinAndGetCharacterPointer(characterBufferReference.OffsetToFirstChar, out gcHandle);

            // Contextual number substitution cannot be performed on the run level, since it depends 
            // on context - nearest preceding strong character. For this reason, contextual number 
            // substitutions has been already done (TextStore.CreateLSRunsUniformBidiLevel) and 
            // digitCulture has been updated to reflect culture which is dependent on the context.
            // NumberSubstitutionMethod.AsCulture method can be resolved to Context, hence it also needs to be resolved to appropriate 
            // not ambiguous method.
            // Both of those values (Context and AsCulture) are resolved to one of following: European, Traditional or NativeNational,
            // which can be safely handled by DWrite without getting context information.
            bool ignoreUserOverride;
            NumberSubstitutionMethod numberSubstitutionMethod = DigitState.GetResolvedSubstitutionMethod(textRunProperties, digitCulture, out ignoreUserOverride);

            // Itemize the text based on DWrite's text analysis for scripts and number substitution.
            unsafe
            {
                checked 
                {
                    spans = MS.Internal.Text.TextInterface.TextAnalyzer.Itemize(
                        (char*)ptext.ToPointer(),
                        (uint)stringLength,
                        culture,
                        MS.Internal.FontCache.DWriteFactory.Instance,
                        isRightToLeftParagraph,
                        digitCulture,
                        ignoreUserOverride,
                        (uint)numberSubstitutionMethod,
                        ClassificationUtility.Instance,
                        UnsafeNativeMethods.CreateTextAnalysisSink,
                        UnsafeNativeMethods.GetScriptAnalysisList,
                        UnsafeNativeMethods.GetNumberSubstitutionList,
                        UnsafeNativeMethods.CreateTextAnalysisSource
                        );
                }
}
            characterBufferReference.CharacterBuffer.UnpinCharacterPointer(gcHandle);

            SpanVector itemSpans = new SpanVector(null, new FrugalStructList<Span>((ICollection<Span>)spans));

            cachedScaledTypefaceIndexSpans = new SpanVector<int>(-1);            
            foreach(Span itemSpan in itemSpans)
            {
                MapItem(
                    new CharacterBufferRange(
                        unicodeString,
                        ichItem,
                        itemSpan.length
                        ),
                    culture,
                    itemSpan,
                    ref cachedScaledTypefaceIndexSpans,
                    ichItem
                    );

                #if DEBUG
                ValidateMapResult(
                    ichItem,
                    itemSpan.length,
                    ref cachedScaledTypefaceIndexSpans
                    );
                #endif

                ichItem += itemSpan.length;
            }


            Debug.Assert(ichItem == unicodeString.Length);

            // intersect item spans with shapeable spans to create span of shapeable runs

            int ich = 0;

            SpanRider itemSpanRider = new SpanRider(itemSpans);
            SpanRider<int> typefaceIndexSpanRider = new SpanRider<int>(cachedScaledTypefaceIndexSpans);

            while(ich < unicodeString.Length)
            {
                itemSpanRider.At(ich);
                typefaceIndexSpanRider.At(ich);

                int index = typefaceIndexSpanRider.CurrentValue;
                Debug.Assert(index >= 0);

                int cch = unicodeString.Length - ich;
                cch = Math.Min(cch, itemSpanRider.Length);
                cch = Math.Min(cch, typefaceIndexSpanRider.Length);

                ScaledShapeTypeface scaledShapeTypeface = _cachedScaledTypefaces[index];

                collector.Add(
                    shapeableList,
                    new CharacterBufferRange(
                        unicodeString,
                        ich,
                        cch
                        ),
                    textRunProperties,
                    (MS.Internal.Text.TextInterface.ItemProps)itemSpanRider.CurrentElement,
                    scaledShapeTypeface.ShapeTypeface,
                    scaledShapeTypeface.ScaleInEm,
                    scaledShapeTypeface.NullShape,
                    textFormattingMode
                    );

                ich += cch;
            }
        }


        #if DEBUG
        private unsafe void ValidateMapResult(
            int                 ichRange,
            int                 cchRange,
            ref SpanVector<int> cachedScaledTypefaceIndexSpans
            )
        {
            int ich = 0;

            SpanRider<int> typefaceIndexSpanRider = new SpanRider<int>(cachedScaledTypefaceIndexSpans);

            while(ich < cchRange)
            {
                typefaceIndexSpanRider.At(ichRange + ich);
                if((int)typefaceIndexSpanRider.CurrentValue < 0)
                {
                    Debug.Assert(false, "Invalid font face spans");
                    return;
                }

                int cch = Math.Min(cchRange - ich, typefaceIndexSpanRider.Length);
                ich += cch;
            }
        }
        #endif

        private void MapItem(
            CharacterBufferRange unicodeString,
            CultureInfo          culture,
            Span                 itemSpan,
            ref SpanVector<int>  cachedScaledTypefaceIndexSpans,
            int                  ichItem
            )
        {
            CultureInfo digitCulture = ((MS.Internal.Text.TextInterface.ItemProps)itemSpan.element).DigitCulture;

            bool isCached = GetCachedScaledTypefaceMap(
                unicodeString,
                culture,
                digitCulture,
                ref cachedScaledTypefaceIndexSpans,
                ichItem
                );

            if(!isCached)
            {
                // shapeable typeface to shape each character in the item has not been located,
                // look thru information in font family searching for the right shapeable typeface.

                SpanVector scaledTypefaceSpans = new SpanVector(null);
                int nextValid;

                // we haven't yet found a valid physical font family
                PhysicalFontFamily firstValidFamily = null;
                int firstValidLength = 0;

                if (!_nullFont)
                {
                    MapByFontFamilyList(
                        unicodeString,
                        culture,
                        digitCulture,
                        _fontFamilies,
                        ref firstValidFamily,
                        ref firstValidLength,
                        null,   // device font
                        1.0,    // default size is one em
                        0,      // recursion depth
                        scaledTypefaceSpans,
                        0,      // firstCharIndex
                        out nextValid
                        );
                }
                else
                {
                    MapUnresolvedCharacters(
                        unicodeString,
                        culture,
                        digitCulture,
                        firstValidFamily,
                        ref firstValidLength,
                        scaledTypefaceSpans,
                        0,       // firstCharIndex
                        out nextValid
                        );
                }

                CacheScaledTypefaceMap(
                    unicodeString,
                    culture,
                    digitCulture,
                    scaledTypefaceSpans,
                    ref cachedScaledTypefaceIndexSpans,
                    ichItem
                    );
            }
        }


        /// <summary>
        /// Get spans of index to the list of scaled shapeable typeface of the specified
        /// character string from the map table
        /// </summary>
        private bool GetCachedScaledTypefaceMap(
            CharacterBufferRange        unicodeString,
            CultureInfo                 culture,
            CultureInfo                 digitCulture,
            ref SpanVector<int>         cachedScaledTypefaceIndexSpans,
            int                         ichItem
            )
        {
            IntMap map;
            if (!_intMaps.TryGetValue(culture, out map))
            {
                return false;
            }

            DigitMap digitMap = new DigitMap(digitCulture);

            int ich = 0;
            while (ich < unicodeString.Length)
            {
                // Get map entry for first character.
                int sizeofChar;
                int ch = digitMap[
                    Classification.UnicodeScalar(
                        new CharacterBufferRange(unicodeString, ich, unicodeString.Length - ich),
                        out sizeofChar
                    )
                ];

                ushort firstIndex = map[ch];
                if (firstIndex == 0)
                    return false;

                // Advance past subsequent characters with the same mapping.
                int cchSpan = sizeofChar;
                for (; ich + cchSpan < unicodeString.Length; cchSpan += sizeofChar)
                {
                    ch = digitMap[
                        Classification.UnicodeScalar(
                            new CharacterBufferRange(unicodeString, ich + cchSpan, unicodeString.Length - ich - cchSpan),
                            out sizeofChar
                        )
                    ];

                    if (map[ch] != firstIndex && !Classification.IsCombining(ch) && !Classification.IsJoiner(ch))
                        break;
                }

                // map entry is stored in index+1, since 0 indicates uninitialized entry
                cachedScaledTypefaceIndexSpans.Set(ichItem + ich, cchSpan, firstIndex - 1);
                ich += cchSpan;
            }
            return true;
        }


        /// <summary>
        /// Cache index to the list of scaled shapeable typeface
        /// </summary>
        private void CacheScaledTypefaceMap(
            CharacterBufferRange        unicodeString,
            CultureInfo                 culture,
            CultureInfo                 digitCulture,
            SpanVector                  scaledTypefaceSpans,
            ref SpanVector<int>         cachedScaledTypefaceIndexSpans,
            int                         ichItem
            )
        {
            IntMap map;
            if (!_intMaps.TryGetValue(culture, out map))
            {
                map = new IntMap();
                _intMaps.Add(culture, map);
            }

            DigitMap digitMap = new DigitMap(digitCulture);

            SpanRider typefaceSpanRider = new SpanRider(scaledTypefaceSpans);

            int ich = 0;
            while(ich < unicodeString.Length)
            {
                typefaceSpanRider.At(ich);

                int cch = Math.Min(unicodeString.Length - ich, typefaceSpanRider.Length);

                int index = IndexOfScaledTypeface((ScaledShapeTypeface)typefaceSpanRider.CurrentElement);
                Debug.Assert(index >= 0, "Invalid scaled shapeable typeface index spans");

                cachedScaledTypefaceIndexSpans.Set(ichItem + ich, cch, index);

                // we keep index + 1 in the map, so that we leave map entry zero
                // to indicate uninitialized entry.
                index++;

                int sizeofChar;
                for (int c = 0; c < cch; c += sizeofChar)
                {
                    int ch = digitMap[
                        Classification.UnicodeScalar(
                            new CharacterBufferRange(unicodeString, ich + c, unicodeString.Length - ich - c),
                            out sizeofChar
                        )
                    ];

                    // only cache typeface map index for base characters
                    if(!Classification.IsCombining(ch) && !Classification.IsJoiner(ch))
                    {
                        // Dump values of local variables when the condition fails for better debuggability.
                        // We use "if" to avoid the expensive string.Format() in normal case.
                        if (map[ch] != 0 && map[ch] != index)
                        {
                            Invariant.Assert(
                                false,
                                string.Format(
                                    CultureInfo.InvariantCulture,
                                    "shapeable cache stores conflicting info, ch = {0}, map[ch] = {1}, index = {2}",
                                    ch, map[ch], index
                                )
                            );
                        }

                        map[ch] = (ushort)index;
                    }
                }

                ich += cch;
            }
        }



        /// <summary>
        /// Search and find index to the list of scaled shapeable typefaces.
        /// Unfound search creates a new entry in the list.
        /// </summary>
        private int IndexOfScaledTypeface(ScaledShapeTypeface scaledTypeface)
        {
            int i;
            for(i = 0; i < _cachedScaledTypefaces.Count; i++)
            {
                if(scaledTypeface.Equals(_cachedScaledTypefaces[i]))
                    break;
            }

            if(i == _cachedScaledTypefaces.Count)
            {
                // encountering this face for the first time, add it to the list
                i = _cachedScaledTypefaces.Count;
                _cachedScaledTypefaces.Add(scaledTypeface);
            }

            return i;
        }


        /// <summary>
        /// Map characters by font family
        /// </summary>
        /// <remarks>
        /// Advance:
        ///     number of characters not mapped to missing glyph
        ///
        /// NextValid:
        ///     Offset to the nearest first character not mapped to missing glyph
        ///
        /// [Number of invalid characters following valid ones] = NextValid - Advance
        ///
        ///         A B C D E F G H x x x x x F G H I J
        ///         --------------->
        ///             Advance
        ///
        ///         ------------------------->
        ///                NextValid
        ///
        /// </remarks>
        private int MapByFontFamily(
            CharacterBufferRange            unicodeString,
            CultureInfo                     culture,
            CultureInfo                     digitCulture,
            IFontFamily                     fontFamily,
            CanonicalFontFamilyReference    canonicalFamilyReference,
            FontStyle                       canonicalStyle,
            FontWeight                      canonicalWeight, 
            FontStretch                     canonicalStretch,
            ref PhysicalFontFamily          firstValidFamily,
            ref int                         firstValidLength,
            IDeviceFont                     deviceFont,
            double                          scaleInEm,
            int                             recursionDepth,
            SpanVector                      scaledTypefaceSpans,
            int                             firstCharIndex,
            out int                         nextValid
            )
        {
            // This is the *one* place where we check for the font mapping depths of the font linking
            // process. This protects the linking process against extremely long chain of linking or
            // circular dependencies in the composite fonts.
            if (recursionDepth >= MaxTypefaceMapDepths)
            {
                // Stop the recursion. In effect, this FontFamily does not map any of the input.
                // Higher-level code must map the input text to some other FontFamily, or to the
                // "null font" if there is no valid FontFamily.
                nextValid = 0;
                return 0;
            }

            // If a device font is not already specified higher up the stack, look for a device font
            // for this font family that matches the typeface style, weight, and stretch.
            if (deviceFont == null)
            {
                deviceFont = fontFamily.GetDeviceFont(_canonicalStyle, _canonicalWeight, _canonicalStretch);
            }

            DigitMap digitMap = new DigitMap(digitCulture);

            int advance = 0;
            int cchAdvance;
            int cchNextValid;
            int ich = 0;

            nextValid = 0;

            bool terminated = false;

            while (ich < unicodeString.Length  &&  !terminated)
            {
                // Determine length of run with consistent mapping. Start by assuming we'll be able to
                // use the whole string, then reduce to the length that can be mapped consistently.
                int cchMap = unicodeString.Length - ich;

                // Determine whether the run is using a device font, and limit the run to the
                // first boundary between device/non-device font usage.
                bool useDeviceFont = false;
                if (deviceFont != null)
                {
                    // Determine whether the first run uses a device font by inspecting the first character.
                    // We do not support device fonts for codepoints >= U+10000 (aka surrogates), so we
                    // don't need to call Classification.UnicodeScalar.
                    useDeviceFont = deviceFont.ContainsCharacter(digitMap[unicodeString[ich]]);

                    // Advance as long as 'useDeviceFont' remains unchanged.
                    int i = ich + 1;
                    while (    (i < unicodeString.Length)
                           &&  (useDeviceFont == deviceFont.ContainsCharacter(digitMap[unicodeString[i]])))
                    {
                        i++;
                    }

                    cchMap = i - ich;
                }


                // Map as many characters to a family as we can up to the limit (cchMap) just determined.
                string targetFamilyName;
                double mapSizeInEm;

                bool isCompositeFontFamily = fontFamily.GetMapTargetFamilyNameAndScale(
                    new CharacterBufferRange(
                        unicodeString,
                        ich,
                        cchMap
                        ),
                    culture,
                    digitCulture,
                    scaleInEm,
                    out cchMap,
                    out targetFamilyName,
                    out mapSizeInEm
                    );

                Debug.Assert(cchMap <= unicodeString.Length - ich);

                CharacterBufferRange mappedString = new CharacterBufferRange(
                    unicodeString,
                    ich,
                    cchMap
                    );


                if (!isCompositeFontFamily)
                {
                    // not a composite font family
                    cchAdvance = MapByFontFaceFamily(
                        mappedString,
                        culture,
                        digitCulture,
                        fontFamily,
                        canonicalStyle,
                        canonicalWeight,
                        canonicalStretch,
                        ref firstValidFamily,
                        ref firstValidLength,
                        useDeviceFont ? deviceFont : null,
                        false, // nullFont
                        mapSizeInEm,
                        scaledTypefaceSpans,
                        firstCharIndex + ich,
                        false, // ignoreMissing
                        out cchNextValid
                        );
                }
                else if (!string.IsNullOrEmpty(targetFamilyName))
                {
                    // The base Uri used for resolving target family names is the Uri of the composite font.
                    Uri baseUri = (canonicalFamilyReference != null) ? canonicalFamilyReference.LocationUri : null;

                    // map to the target of the family map
                    cchAdvance = MapByFontFamilyName(
                        mappedString,
                        culture,
                        digitCulture,
                        targetFamilyName,
                        baseUri,
                        ref firstValidFamily,
                        ref firstValidLength,
                        useDeviceFont ? deviceFont : null,
                        mapSizeInEm,
                        recursionDepth + 1, // increment the depth
                        scaledTypefaceSpans,
                        firstCharIndex + ich,
                        out cchNextValid
                        );
                }
                else
                {
                    // family map lookup returned no target family
                    cchAdvance = 0;
                    cchNextValid = cchMap;
                }

                int cchValid = cchMap;
                int cchInvalid = 0;

                cchValid = cchAdvance;
                cchInvalid = cchNextValid;

                if(cchValid < cchMap)
                {
                    terminated = true;
                }

                advance += cchValid;
                nextValid = ich + cchInvalid;

                ich += cchValid;
            }

            return advance;
        }


        /// <summary>
        /// Maps characters that could not be resolved to any font family either to the first
        /// valid physical font family or to the default font we use for display null glyphs.
        /// </summary>
        private int MapUnresolvedCharacters(
            CharacterBufferRange    unicodeString,
            CultureInfo             culture,
            CultureInfo             digitCulture,
            PhysicalFontFamily      firstValidFamily,
            ref int                 firstValidLength,
            SpanVector              scaledTypefaceSpans,
            int                     firstCharIndex,
            out int                 nextValid
            )
        {
            // If we have a valid font family use it. We don't set nullFont to true in this case.
            // We may end up displaying missing glyphs, but we don't need to force it.
            IFontFamily fontFamily = firstValidFamily;
            bool nullFont = false;

            if (firstValidLength <= 0)
            {
                // We didn't find any valid physical font family so use the default "Arial", and
                // set nullFont to true to ensure that we always display missing glyphs.
                fontFamily = FontFamily.LookupFontFamily(FontFamily.NullFontFamilyCanonicalName);
                Invariant.Assert(fontFamily != null);
                nullFont = true;
            }

            return MapByFontFaceFamily(
                unicodeString,
                culture,
                digitCulture,
                fontFamily,
                _canonicalStyle,
                _canonicalWeight,
                _canonicalStretch,
                ref firstValidFamily,
                ref firstValidLength,
                null, // device font
                nullFont,
                1.0,
                scaledTypefaceSpans,
                firstCharIndex,
                true, // ignore missing
                out nextValid
                );
        }

        /// <summary>
        /// Map characters by font family name
        /// </summary>
        private int MapByFontFamilyName(
            CharacterBufferRange        unicodeString,
            CultureInfo                 culture,
            CultureInfo                 digitCulture,
            string                      familyName,
            Uri                         baseUri,
            ref PhysicalFontFamily      firstValidFamily,
            ref int                     firstValidLength,
            IDeviceFont                 deviceFont,
            double                      scaleInEm,
            int                         fontMappingDepth,
            SpanVector                  scaledTypefaceSpans,
            int                         firstCharIndex,
            out int                     nextValid
            )
        {
            if (familyName == null)
            {
                return MapUnresolvedCharacters(
                    unicodeString,
                    culture,
                    digitCulture,
                    firstValidFamily,
                    ref firstValidLength,
                    scaledTypefaceSpans,
                    firstCharIndex,
                    out nextValid
                    );
            }
            else
            {
                // Map as many characters as we can to families in the list.
                return MapByFontFamilyList(
                    unicodeString,
                    culture,
                    digitCulture,
                    new FontFamily[] { new FontFamily(baseUri, familyName) },
                    ref firstValidFamily,
                    ref firstValidLength,
                    deviceFont,
                    scaleInEm,
                    fontMappingDepth,
                    scaledTypefaceSpans,
                    firstCharIndex,
                    out nextValid
                    );
            }
        }

        /// <summary>
        /// Maps as may characters as it can (or *all* characters if recursionDepth == 0) to
        /// font families in the specified FontFamilyList.
        /// </summary>
        private int MapByFontFamilyList(
            CharacterBufferRange    unicodeString,
            CultureInfo             culture,
            CultureInfo             digitCulture,
            FontFamily[]            familyList,
            ref PhysicalFontFamily  firstValidFamily,
            ref int                 firstValidLength,
            IDeviceFont             deviceFont,
            double                  scaleInEm,
            int                     recursionDepth,
            SpanVector              scaledTypefaceSpans,
            int                     firstCharIndex,
            out int                 nextValid
            )
        {
            int advance = 0;
            int cchAdvance;
            int cchNextValid = 0;
            int ich = 0;

            nextValid = 0;

            while (ich < unicodeString.Length)
            {
                cchAdvance = MapOnceByFontFamilyList(
                    new CharacterBufferRange(
                        unicodeString,
                        ich,
                        unicodeString.Length - ich
                        ),
                    culture,
                    digitCulture,
                    familyList,
                    ref firstValidFamily,
                    ref firstValidLength,
                    deviceFont,
                    scaleInEm,
                    recursionDepth,
                    scaledTypefaceSpans,
                    firstCharIndex + ich,
                    out cchNextValid
                    );

                if (cchAdvance <= 0)
                {
                    // We could not map any characters. If this is a recursive call then it's OK to
                    // exit the loop without mapping all the characters; the caller may be able to
                    // map the text to some other font family.
                    if (recursionDepth > 0)
                        break;

                    Debug.Assert(cchNextValid > 0 && cchNextValid <= unicodeString.Length - ich);

                    // The top-level call has to map all the input.
                    cchAdvance = MapUnresolvedCharacters(
                        new CharacterBufferRange(
                            unicodeString,
                            ich,
                            cchNextValid
                            ),
                        culture,
                        digitCulture,
                        firstValidFamily,
                        ref firstValidLength,
                        scaledTypefaceSpans,
                        firstCharIndex + ich,
                        out cchNextValid
                        );

                    Debug.Assert(cchNextValid == 0);
                }

                ich += cchAdvance;
            }

            advance += ich;
            nextValid = ich + cchNextValid;

            // The top-level call must map all the input; recursive calls map only what they can.
            Debug.Assert(recursionDepth > 0 || advance == unicodeString.Length);
            return advance;
        }

        /// <summary>
        /// Maps characters to one of the font families in the specified FontFamilyList. This
        /// function differs from MapByFontFamilyList in that it returns as soon as at least
        /// one character is mapped; it does not keep going until it cannot map any more text.
        /// </summary>
        private int MapOnceByFontFamilyList(
            CharacterBufferRange                unicodeString,
            CultureInfo                         culture,
            CultureInfo                         digitCulture,
            FontFamily[]                        familyList,
            ref PhysicalFontFamily              firstValidFamily,
            ref int                             firstValidLength,
            IDeviceFont                         deviceFont,
            double                              scaleInEm,
            int                                 recursionDepth,
            SpanVector                          scaledTypefaceSpans,
            int                                 firstCharIndex,
            out int                             nextValid
            )
        {
            Invariant.Assert(familyList != null);

            int advance = 0;
            nextValid = 0;
            CharacterBufferRange mapString = unicodeString;
            FontStyle canonicalStyle = _canonicalStyle;
            FontWeight canonicalWeight = _canonicalWeight;
            FontStretch canonicalStretch = _canonicalStretch;

            // Note: FontFamilyIdentifier limits the number of family names in a single string. We
            // don't want to also limit the number of iterations here because if Typeface.FontFamily
            // has the maximum number of tokens, this should not prevent us from falling back to the
            // FallbackFontFamily (PS # 1148305).

            // Outer loop to loop over the list of FontFamily.
            for (int i = 0; i < familyList.Length; i++)
            {
                // grab the font family identifier and initialize the
                // target family based on whether it is a named font.
                FontFamilyIdentifier fontFamilyIdentifier = familyList[i].FamilyIdentifier;

                CanonicalFontFamilyReference canonicalFamilyReference = null;
                IFontFamily targetFamily;

                if (fontFamilyIdentifier.Count != 0)
                {
                    // Look up font family and face, in the case of multiple canonical families the weight/style/stretch
                    // may not match the typeface map's, since it is created w/ the first canonical family.
                    canonicalFamilyReference = fontFamilyIdentifier[0];
                    targetFamily = FontFamily.LookupFontFamilyAndFace(canonicalFamilyReference, ref canonicalStyle, ref canonicalWeight, ref canonicalStretch);
                }
                else
                {
                    targetFamily = familyList[i].FirstFontFamily;
                }

                int familyNameIndex = 0;

                // Inner loop to loop over all name tokens of a FontFamily.
                for (;;)
                {
                    if (targetFamily != null)
                    {
                        advance = MapByFontFamily(
                            mapString,
                            culture,
                            digitCulture,
                            targetFamily,
                            canonicalFamilyReference,
                            canonicalStyle,
                            canonicalWeight, 
                            canonicalStretch,
                            ref firstValidFamily,
                            ref firstValidLength,
                            deviceFont,
                            scaleInEm,
                            recursionDepth,
                            scaledTypefaceSpans,
                            firstCharIndex,
                            out nextValid
                            );

                        if (nextValid < mapString.Length)
                        {
                            // only strings before the smallest invalid needs to be mapped since
                            // string beyond smallest invalid can already be mapped to a higher priority font.
                            mapString = new CharacterBufferRange(
                                unicodeString.CharacterBuffer,
                                unicodeString.OffsetToFirstChar,
                                nextValid
                                );
                        }

                        if (advance > 0)
                        {
                            // found the family that shapes this string. We terminate both the
                            // inner and outer loops.
                            i = familyList.Length;
                            break;
                        }
                    }
                    else
                    {
                        // By definition null target does not map any of the input.
                        nextValid = mapString.Length;
                    }

                    if (++familyNameIndex < fontFamilyIdentifier.Count)
                    {
                        // Get the next canonical family name and target family.
                        canonicalFamilyReference = fontFamilyIdentifier[familyNameIndex];
                        targetFamily = FontFamily.LookupFontFamilyAndFace(canonicalFamilyReference, ref canonicalStyle, ref canonicalWeight, ref canonicalStretch);
                    }
                    else
                    {
                        // Unnamed FontFamily or no more family names in this FontFamily.
                        break;
                    }
                }
            }

            nextValid = mapString.Length;
            return advance;
        }


        /// <summary>
        /// Map characters by font face family
        /// </summary>
        private int MapByFontFaceFamily(
            CharacterBufferRange    unicodeString,
            CultureInfo             culture,
            CultureInfo             digitCulture,
            IFontFamily             fontFamily,
            FontStyle               canonicalStyle,
            FontWeight              canonicalWeight,
            FontStretch             canonicalStretch,
            ref PhysicalFontFamily  firstValidFamily,
            ref int                 firstValidLength,
            IDeviceFont             deviceFont,
            bool                    nullFont,
            double                  scaleInEm,
            SpanVector              scaledTypefaceSpans,
            int                     firstCharIndex,
            bool                    ignoreMissing,
            out int                 nextValid
            )
        {
            Invariant.Assert(fontFamily != null);

            PhysicalFontFamily fontFaceFamily = fontFamily as PhysicalFontFamily;
            Invariant.Assert(fontFaceFamily != null);

            int advance = unicodeString.Length;
            nextValid = 0;

            GlyphTypeface glyphTypeface = null;

            if(ignoreMissing)
            {
                glyphTypeface = fontFaceFamily.GetGlyphTypeface(canonicalStyle, canonicalWeight, canonicalStretch);
            }
            else if(nullFont)
            {
                glyphTypeface = fontFaceFamily.GetGlyphTypeface(canonicalStyle, canonicalWeight, canonicalStretch);

                advance = 0; // by definition, null font always yields missing glyphs for whatever codepoint
                nextValid = unicodeString.Length;
            }
            else
            {
                glyphTypeface = fontFaceFamily.MapGlyphTypeface(
                    canonicalStyle,
                    canonicalWeight,
                    canonicalStretch,
                    unicodeString,
                    digitCulture,
                    ref advance,
                    ref nextValid
                    );
            }

            Invariant.Assert(glyphTypeface != null);

            int cch = unicodeString.Length;
            if(!ignoreMissing && advance > 0)
            {
                cch = advance;
            }

            // Do we need to set firstValidFamily?
            if (firstValidLength <= 0)
            {
                // Either firstValidFamily hasn't been set, or has "expired" (see below). The first valid
                // family is the first existing physical font in the font linking chain. We want to remember 
                // it so we can use it to map any unresolved characters.
                firstValidFamily = fontFaceFamily;

                // Set the "expiration date" for firstValidFamily. We know that this is the first physical
                // font for the specified character range, but after that family map lookup may result in
                // a different first physical family.
                firstValidLength = unicodeString.Length;
            }

            // Each time we advance we near the expiration date for firstValidFamily.
            firstValidLength -= advance;


            Debug.Assert(cch > 0);
            scaledTypefaceSpans.SetValue(
                firstCharIndex,
                cch,
                new ScaledShapeTypeface(
                    glyphTypeface,
                    deviceFont,
                    scaleInEm,
                    nullFont
                    )
                );

            return advance;
        }

        #region IntMap

        /// <summary>
        /// Sparse table
        /// </summary>
        internal class IntMap
        {
            private const byte NumberOfPlanes = 17;

            /// <summary>
            /// Creates an empty IntMap.
            /// </summary>
            public IntMap()
            {
                _planes = new Plane[NumberOfPlanes];
                for (int i = 0; i < NumberOfPlanes; ++i)
                    _planes[i] = EmptyPlane;
            }

            private void CreatePlane(int i)
            {
                Invariant.Assert(i < NumberOfPlanes);
                if(_planes[i] == EmptyPlane)
                {
                    Plane plane = new Plane();
                    _planes[i] = plane;
                }
            }

            /// <summary>
            /// i is Unicode character value
            /// get looks up glyph index corresponding to it.
            /// put inserts glyph index corresponding to it.
            /// </summary>
            public ushort this[int i]
            {
                get
                {
                    return _planes[i>>16][i>>8 & 0xff][i & 0xff];
                }
                set
                {
                    CreatePlane(i>>16);

                    _planes[i>>16].CreatePage(i>>8 & 0xff, this);

                    _planes[i>>16][i>>8 & 0xff][i & 0xff] = value;
                }
            }

            private Plane[]                 _planes;
            private static Plane EmptyPlane = new Plane();
        };


        internal class Plane
        {
            public Plane()
            {
                _data = new Page[256];
                for (int i = 0; i < 256; ++i)
                    _data[i] = EmptyPage;
            }

            public Page this[int index]
            {
                get
                {
                    return _data[index];
                }
                set
                {
                    _data[index] = value;
                }
            }

            internal void CreatePage(int i, IntMap intMap)
            {
                if(this[i] == Plane.EmptyPage)
                {
                    Page page = new Page();
                    this[i] = page;
                }
            }

            private Page[]      _data;

            private static Page EmptyPage = new Page();
        };


        internal class Page
        {
            public Page()
            {
                _data = new ushort[256];
            }

            public ushort this[int index]
            {
                get
                {
                    return _data[index];
                }
                set
                {
                    _data[index] = value;
                }
            }

            private ushort[] _data;
        };

        #endregion
    }
}
