// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// 
// Description: High level glyphing cache
//
//

using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Windows.Media;
using System.Windows.Media.TextFormatting;

using MS.Internal;
using MS.Internal.TextFormatting;

namespace MS.Internal.Shaping
{
    /// <summary>
    /// GlyphingCache stores the runtime state of shaping engines and the mapping between Unicode scalar
    /// value and the physical font being used to display it. This class is not thread-safe. The client is 
    /// responsible for synchronizing access from multiple threads. It is generally recommended that the client 
    /// manages a single instance of this class per text formatting session. 
    ///
    /// It currently only caches one key-value pair:
    /// o Typeface - TypefaceMap
    ///
    /// This pair is cached in SizeLimitedCache which implement LRU algorithm through 
    /// a linked list. When cache is full, the least used entry in the cache will be replaced 
    /// by the latest entry. 
    /// </summary>
    internal class GlyphingCache
    {
        private SizeLimitedCache<Typeface, TypefaceMap>  _sizeLimitedCache;
    
        internal GlyphingCache(int capacity)
        {
            _sizeLimitedCache  = new SizeLimitedCache<Typeface, TypefaceMap>(capacity);
        }

        internal void GetShapeableText(
            Typeface                    typeface,
            CharacterBufferReference    characterBufferReference,
            int                         stringLength,
            TextRunProperties           textRunProperties,
            CultureInfo                 digitCulture,
            bool                        isRightToLeftParagraph,
            IList<TextShapeableSymbols> shapeableList,
            IShapeableTextCollector     collector,
            TextFormattingMode              textFormattingMode
            )
        {
            if (!typeface.Symbol)
            {
                Lookup(typeface).GetShapeableText(
                    characterBufferReference,
                    stringLength,
                    textRunProperties,
                    digitCulture,
                    isRightToLeftParagraph,
                    shapeableList,
                    collector,
                    textFormattingMode
                    );
            }
            else
            {
                // It's a non-Unicode ("symbol") font, where code points have non-standard meanings. We
                // therefore want to bypass the usual itemization and font linking. Instead, just map
                // everything to the default script and first GlyphTypeface.

                ShapeTypeface shapeTypeface = new ShapeTypeface(
                    typeface.TryGetGlyphTypeface(),
                    null // device font
                    );

                collector.Add(
                    shapeableList,
                    new CharacterBufferRange(characterBufferReference, stringLength),
                    textRunProperties,
                    new MS.Internal.Text.TextInterface.ItemProps(),
                    shapeTypeface,
                    1.0,   // scale in Em
                    false,  // null shape
                    textFormattingMode
                    );
            }
        }

        /// <summary>
        /// Look up the font mapping data for a typeface.
        /// </summary>
        private TypefaceMap Lookup(Typeface key)
        {
            TypefaceMap typefaceMap = _sizeLimitedCache.Get(key);
            if (typefaceMap == null)
            {                
                typefaceMap = new TypefaceMap(
                    key.FontFamily, 
                    key.FallbackFontFamily,
                    key.CanonicalStyle,
                    key.CanonicalWeight,
                    key.CanonicalStretch,
                    key.NullFont
                    );
                
                _sizeLimitedCache.Add(
                    key, 
                    typefaceMap, 
                    false   // is not permanent in the cache.
                    );
            }   
            
            return typefaceMap;
        }  
    }
}
