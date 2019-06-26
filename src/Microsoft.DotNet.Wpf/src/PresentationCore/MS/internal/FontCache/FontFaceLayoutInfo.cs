// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
//
// Description: The FontFaceLayoutInfo class
//
//

using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Security;
using System.ComponentModel;
using System.Collections;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;
using System.Runtime.InteropServices;

using MS.Win32;
using MS.Utility;
using MS.Internal;
using MS.Internal.FontFace;
using MS.Internal.Shaping;

using MS.Internal.PresentationCore;

namespace MS.Internal.FontCache
{
    [FriendAccessAllowed]
    internal sealed class FontFaceLayoutInfo
    {
        private FontTechnology _fontTechnology;
        private TypographyAvailabilities _typographyAvailabilities;
        private FontEmbeddingRight _embeddingRights;

        private bool _embeddingRightsInitialized;
        private bool _gsubInitialized;
        private bool _gposInitialized;
        private bool _gdefInitialized;
        private bool _fontTechnologyInitialized;
        private bool _typographyAvailabilitiesInitialized;

        private byte[] _gsubCache;
        private byte[] _gposCache;

        private byte[] _gsub;
        private byte[] _gpos;
        private byte[] _gdef;

        Text.TextInterface.Font _font;

        ushort _blankGlyphIndex;

        
        //------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------

        #region Constructors


        internal FontFaceLayoutInfo(Text.TextInterface.Font font)
        {
            _fontTechnologyInitialized = false;
            _typographyAvailabilitiesInitialized = false;
            _gsubInitialized            = false;
            _gposInitialized            = false;
            _gdefInitialized            = false;
            _embeddingRightsInitialized = false;
            _gsubCache = null;
            _gposCache = null;
            _gsub = null;
            _gpos = null;
            _gdef = null;

            _font = font;
            _cmap = new IntMap(_font);
            _cmap.TryGetValue(' ', out _blankGlyphIndex);
        }

        #endregion Constructors

        internal IntMap CharacterMap
        {
            get
            {
                return _cmap;
            }
        }

        internal ushort BlankGlyph
        {
            get
            {
                return _blankGlyphIndex;
            }
        }

        internal ushort DesignEmHeight
        {
            get
            {
                return _font.Metrics.DesignUnitsPerEm;
            }
        }

        private static class Os2EmbeddingFlags
        {
            public const ushort RestrictedLicense = 0x0002;
            public const ushort PreviewAndPrint = 0x0004;
            public const ushort Editable = 0x0008;

            // The font is installable if all bits in the InstallableMask are set to zero.
            public const ushort InstallableMask = RestrictedLicense | PreviewAndPrint | Editable;

            public const ushort NoSubsetting = 0x0100;
            public const ushort BitmapOnly = 0x0200;
        }

        /// <summary>
        /// Analyzes os/2 fsType value and construct FontEmbeddingRight enum value from it.
        /// </summary>
        internal FontEmbeddingRight EmbeddingRights
        {
            get
            {
                if (!_embeddingRightsInitialized)
                {
                    // If there is no os/2 table, default to restricted font.
                    // This is the precedence that has been set by T2Embed, Word, etc.
                    // No one has complained about this because these fonts are generally lower in quality and are less likely to be embedded.
                    FontEmbeddingRight rights = FontEmbeddingRight.RestrictedLicense;

                    ushort fsType;
                    bool success;

                    MS.Internal.Text.TextInterface.FontFace fontFace = _font.GetFontFace();
                    try
                    {
                        success = fontFace.ReadFontEmbeddingRights(out fsType);
                    }
                    finally
                    {
                        fontFace.Release();
                    }

                    if (success)
                    {
                        // Start with the most restrictive flags.
                        // In case a font uses conflicting flags,
                        // expose the least restrictive combination in order to be compatible with existing applications.

                        if ((fsType & Os2EmbeddingFlags.InstallableMask) == 0)
                        {
                            // The font is installable if all bits in the InstallableMask are set to zero.
                            switch (fsType & (Os2EmbeddingFlags.NoSubsetting | Os2EmbeddingFlags.BitmapOnly))
                            {
                                case 0:
                                    rights = FontEmbeddingRight.Installable;
                                    break;
                                case Os2EmbeddingFlags.NoSubsetting:
                                    rights = FontEmbeddingRight.InstallableButNoSubsetting;
                                    break;
                                case Os2EmbeddingFlags.BitmapOnly:
                                    rights = FontEmbeddingRight.InstallableButWithBitmapsOnly;
                                    break;
                                case Os2EmbeddingFlags.NoSubsetting | Os2EmbeddingFlags.BitmapOnly:
                                    rights = FontEmbeddingRight.InstallableButNoSubsettingAndWithBitmapsOnly;
                                    break;
                            }
                        }
                        else if ((fsType & Os2EmbeddingFlags.Editable) != 0)
                        {
                            switch (fsType & (Os2EmbeddingFlags.NoSubsetting | Os2EmbeddingFlags.BitmapOnly))
                            {
                                case 0:
                                    rights = FontEmbeddingRight.Editable;
                                    break;
                                case Os2EmbeddingFlags.NoSubsetting:
                                    rights = FontEmbeddingRight.EditableButNoSubsetting;
                                    break;
                                case Os2EmbeddingFlags.BitmapOnly:
                                    rights = FontEmbeddingRight.EditableButWithBitmapsOnly;
                                    break;
                                case Os2EmbeddingFlags.NoSubsetting | Os2EmbeddingFlags.BitmapOnly:
                                    rights = FontEmbeddingRight.EditableButNoSubsettingAndWithBitmapsOnly;
                                    break;
                            }
                        }
                        else if ((fsType & Os2EmbeddingFlags.PreviewAndPrint) != 0)
                        {
                            switch (fsType & (Os2EmbeddingFlags.NoSubsetting | Os2EmbeddingFlags.BitmapOnly))
                            {
                                case 0:
                                    rights = FontEmbeddingRight.PreviewAndPrint;
                                    break;
                                case Os2EmbeddingFlags.NoSubsetting:
                                    rights = FontEmbeddingRight.PreviewAndPrintButNoSubsetting;
                                    break;
                                case Os2EmbeddingFlags.BitmapOnly:
                                    rights = FontEmbeddingRight.PreviewAndPrintButWithBitmapsOnly;
                                    break;
                                case Os2EmbeddingFlags.NoSubsetting | Os2EmbeddingFlags.BitmapOnly:
                                    rights = FontEmbeddingRight.PreviewAndPrintButNoSubsettingAndWithBitmapsOnly;
                                    break;
                            }
                        }
                        else
                        {
                            // Otherwise, the font either has Os2EmbeddingFlags.RestrictedLicense set, or
                            // it has a reserved bit 0 set, which is invalid per specification.
                            // Either way, rights should remain FontEmbeddingRight.RestrictedLicense.
                        }                        
                    }
                    _embeddingRights = rights;
                    _embeddingRightsInitialized = true;
                }
                return _embeddingRights;
            }
        }

        internal FontTechnology FontTechnology
        {
            get
            {
                if (!_fontTechnologyInitialized)
                {
                    ComputeFontTechnology();
                    _fontTechnologyInitialized = true;
                }
                return _fontTechnology;
            }
        }

        internal TypographyAvailabilities TypographyAvailabilities
        {
            get
            {
                if (!_typographyAvailabilitiesInitialized)
                {
                    ComputeTypographyAvailabilities();
                    _typographyAvailabilitiesInitialized = true;
                }
                return _typographyAvailabilities;
            }
        }

        internal ushort GlyphCount
        {
            get
            {
                ushort glyphCount;

                MS.Internal.Text.TextInterface.FontFace fontFace = _font.GetFontFace();
                try
                {
                    glyphCount = fontFace.GlyphCount;
                }
                finally
                {
                    fontFace.Release();
                }

                return glyphCount;
            }
        }

        private byte[] GetFontTable(Text.TextInterface.OpenTypeTableTag openTypeTableTag)
        {
            byte[] table;

            MS.Internal.Text.TextInterface.FontFace fontFace = _font.GetFontFace();
            try
            {
                if (!fontFace.TryGetFontTable(openTypeTableTag, out table))
                {
                    table = null;
                }
            }
            finally
            {
                fontFace.Release();
            }

            return table;
        }

        // OpenType support
        
        internal byte[] Gsub()
        {
            if (!_gsubInitialized)
            {
                _gsub = GetFontTable(Text.TextInterface.OpenTypeTableTag.TTO_GSUB);
                _gsubInitialized = true;
            }
            return _gsub;
        }
        
        internal byte[] Gpos()
        {
            if (!_gposInitialized)
            {
                _gpos = GetFontTable(Text.TextInterface.OpenTypeTableTag.TTO_GPOS);
                _gposInitialized = true;
            }
            return _gpos;
        }

        internal byte[] Gdef()
        {
            if (!_gdefInitialized)
            {
                _gdef = GetFontTable(Text.TextInterface.OpenTypeTableTag.TTO_GDEF);
                _gdefInitialized = true;
            }
            return _gdef;
        }

        internal byte[] GetTableCache(OpenTypeTags tableTag)
        {
            switch (tableTag)
            {
                case OpenTypeTags.GSUB:
                    if (Gsub() != null)
                    {
                        return _gsubCache;
                    }
                    break;
                case OpenTypeTags.GPOS:
                    if (Gpos() != null)
                    {
                        return _gposCache;
                    }
                    break;
                default:
                    throw new NotSupportedException();
            }

            return null;
        }

        internal byte[] AllocateTableCache(OpenTypeTags tableTag, int size)
        {
            switch (tableTag)
            {
                case OpenTypeTags.GSUB:
                    {
                        _gsubCache = new byte[size];
                        return _gsubCache;
                    }
                case OpenTypeTags.GPOS:
                    {
                        _gposCache = new byte[size];
                        return _gposCache;
                    }
                default:
                    {
                        throw new NotSupportedException();
                    }
            }
        }

        private void ComputeFontTechnology()
        {
            MS.Internal.Text.TextInterface.FontFace fontFace = _font.GetFontFace();
            try
            {
                if (fontFace.Type == Text.TextInterface.FontFaceType.TrueTypeCollection)
                {
                    _fontTechnology = FontTechnology.TrueTypeCollection;
                }
                else if (fontFace.Type == Text.TextInterface.FontFaceType.CFF)
                {
                    _fontTechnology = FontTechnology.PostscriptOpenType;
                }
                else
                {
                    _fontTechnology = FontTechnology.TrueType;
                }
            }
            finally
            {
                fontFace.Release();
            }
        }

        /// <summary>
        /// Computes the typography availabilities.
        /// It checks the presence of a set of required features in the font
        /// for ranges of unicode code points and set the corresponding bits
        /// in the TypographyAvailabilities enum. TypographyAvailabilities enum is
        /// used to determind whether fast path can be used to format the input.
        /// </summary>
        private void ComputeTypographyAvailabilities()
        {
            int glyphBitsLength = (GlyphCount + 31) >> 5;
            uint[] glyphBits = BufferCache.GetUInts(glyphBitsLength);
            Array.Clear(glyphBits, 0, glyphBitsLength);

            ushort minGlyphId = 65535;
            ushort maxGlyphId = 0;

            WritingSystem[] complexScripts;
            TypographyAvailabilities typography = TypographyAvailabilities.None;

            GsubGposTables GsubGpos = new GsubGposTables(this);

            // preparing the glyph bits. When the bit is set, it means the corresponding
            // glyph needs to be checked against.
            for (int i = 0; i < fastTextRanges.Length; i++)
            {
                uint[] codepoints = fastTextRanges[i].GetFullRange();
                ushort[] glyphIndices = BufferCache.GetUShorts(codepoints.Length);

                unsafe
                {
                    fixed (uint *pCodepoints = &codepoints[0])
                    {
                        fixed (ushort *pGlyphIndices = &glyphIndices[0])
                        {
                            CharacterMap.TryGetValues(pCodepoints, checked((uint)codepoints.Length), pGlyphIndices);
                        }               
                    }
                }

                for (int j = 0; j < codepoints.Length; j++)
                {
                    ushort glyphId = glyphIndices[j];
                    if (glyphId != 0)
                    {
                        glyphBits[glyphId >> 5] |= (uint)(1 << (glyphId % 32));

                        if (glyphId > maxGlyphId) maxGlyphId = glyphId;
                        if (glyphId < minGlyphId) minGlyphId = glyphId;
                    }
                }

                BufferCache.ReleaseUShorts(glyphIndices);
            }

            //
            // Step 1: call OpenType layout engine to test presence of
            // 'locl' feature. Based on the returned writing systems, set
            // FastTextMajorLanguageLocalizedFormAvailable bit and
            // FastTextExtraLanguageLocalizedFormAvailable bit
            //
            OpenTypeLayoutResult result;
            unsafe
            {
                result = OpenTypeLayout.GetComplexLanguageList(
                             GsubGpos,
                             LoclFeature,
                             glyphBits,
                             minGlyphId,
                             maxGlyphId,
                             out complexScripts
                        );
            }

            if (result != OpenTypeLayoutResult.Success)
            {
                // The check failed. We abort and don't keep partial results that are not reliable
                _typographyAvailabilities = TypographyAvailabilities.None;
                return;
            }
            else if (complexScripts != null)
            {
                // This is the bits for localized form we would want to set
                // if both bits for localized form were set, we can end the loop earlier
                TypographyAvailabilities loclBitsTest =
                      TypographyAvailabilities.FastTextMajorLanguageLocalizedFormAvailable
                    | TypographyAvailabilities.FastTextExtraLanguageLocalizedFormAvailable;

                for (int i = 0; i < complexScripts.Length && typography != loclBitsTest; i++)
                {
                    if (MajorLanguages.Contains((ScriptTags)complexScripts[i].scriptTag, (LanguageTags)complexScripts[i].langSysTag))
                    {
                        typography |= TypographyAvailabilities.FastTextMajorLanguageLocalizedFormAvailable;
                    }
                    else
                    {
                        typography |= TypographyAvailabilities.FastTextExtraLanguageLocalizedFormAvailable;
                    }
                }
            }

            //
            // step 2: continue to find out whether there is common features availabe
            // in the font for the unicode ranges and set the FastTextTypographyAvailable bit
            //
            unsafe
            {
                result = OpenTypeLayout.GetComplexLanguageList(
                    GsubGpos,
                    RequiredTypographyFeatures,
                    glyphBits,
                    minGlyphId,
                    maxGlyphId,
                    out complexScripts
                    );
            }

            if (result != OpenTypeLayoutResult.Success)
            {
                // The check failed. We abort and don't keep partial results that are not reliable
                _typographyAvailabilities = TypographyAvailabilities.None;
                return;
            }
            else if (complexScripts != null)
            {
                typography |= TypographyAvailabilities.FastTextTypographyAvailable;
            }

            //
            // Step 3: call OpenType layout engine to find out if there is any feature present for
            // ideographs. Because there are many ideographs to check for, an alternative is to
            // check for all scripts with the required features in the font by setting all
            // glyph bits to 1, then see whether CJKIdeograph is in the returned list.
            //
            for (int i = 0; i < glyphBitsLength; i++)
            {
                glyphBits[i] = 0xFFFFFFFF;
            }

            unsafe
            {
                result = OpenTypeLayout.GetComplexLanguageList(
                             GsubGpos,
                             RequiredFeatures,
                             glyphBits,
                             minGlyphId,
                             maxGlyphId,
                             out complexScripts
                        );
            }

            if (result != OpenTypeLayoutResult.Success)
            {
                // The check failed. We abort and don't keep partial results that are not reliable
                _typographyAvailabilities = TypographyAvailabilities.None;
                return;
            }
            else if (complexScripts != null)
            {
                for (int i = 0; i < complexScripts.Length; i++)
                {
                    if (complexScripts[i].scriptTag == (uint)ScriptTags.CJKIdeographic)
                    {
                        typography |= TypographyAvailabilities.IdeoTypographyAvailable;
                    }
                    else
                    {
                        typography |= TypographyAvailabilities.Available;
                    }
                }
            }

            if (typography != TypographyAvailabilities.None)
            {
                // if any of the bits were set, set TypographyAvailabilities.Avaialble bit
                // as well to indicate some lookup is available.
                typography |= TypographyAvailabilities.Available;
            }

            _typographyAvailabilities = typography;

            // Note: we don't worry about calling ReleaseUInts in case of early out for a failure
            // above.  Releasing glyphBits is a performance optimization that is not necessary
            // for correctness, and not interesting in the rare failure case.
            BufferCache.ReleaseUInts(glyphBits);
        }
      

        #region IntMap

        /// <summary>
        /// IntMap represents mapping from UTF32 code points to glyph indices.
        /// The IDictionary part is eventually returned from public APIs and is made read-only.
        /// Internal methods are used by the font driver to create the cmap.
        /// </summary>
        internal sealed class IntMap : IDictionary<int, ushort>
        {
            private Text.TextInterface.Font     _font;
            private Dictionary<int, ushort>     _cmap;

            internal IntMap(Text.TextInterface.Font font)
            {
                _font     = font;
                _cmap     = null;
}

            private Dictionary<int, ushort> CMap
            {
                get
                {
                    if (_cmap == null)
                    {
                        lock (this)
                        {
                            if (_cmap == null)
                            {
                                _cmap = new Dictionary<int, ushort>();
                                ushort glyphIndex;
                                for (int codePoint = 0; codePoint <= FontFamilyMap.LastUnicodeScalar; ++codePoint)
                                {
                                    if (TryGetValue(codePoint, out glyphIndex))
                                    {
                                        _cmap.Add(codePoint, glyphIndex);
                                    }
                                }
                            }
                        }
                    }
                    return _cmap;
                }
            }
            #region IDictionary<int,ushort> Members
            public void Add(int key, ushort value)
            {
                throw new NotSupportedException();
            }

            public bool ContainsKey(int key)
            {
                return _font.HasCharacter(checked((uint)key));
            }

            public ICollection<int> Keys
            {
                get
                {
                    return CMap.Keys;
                }
            }

            public bool Remove(int key)
            {
                throw new NotSupportedException();
            }

            public bool TryGetValue(int key, out ushort value)
            {
                ushort localValue;
                unsafe
                {
                    uint uKey = checked((uint)key);
                    uint *pKey = &uKey;

                    MS.Internal.Text.TextInterface.FontFace fontFace = _font.GetFontFace();
                    try
                    {
                        fontFace.GetArrayOfGlyphIndices(pKey, 1, &localValue);
                    }
                    finally
                    {
                        fontFace.Release();
                    }

                    value = localValue;
                }

                // if a glyph is not present, index 0 is returned
                return (value != 0);
            }

            internal unsafe void TryGetValues(uint *pKeys, uint characterCount, ushort *pIndices)
            {
                MS.Internal.Text.TextInterface.FontFace fontFace = _font.GetFontFace();
                try
                {
                    fontFace.GetArrayOfGlyphIndices(pKeys, characterCount, pIndices);
                }
                finally
                {
                    fontFace.Release();
                }
            }

            public ICollection<ushort> Values
            {
                get
                {
                    return CMap.Values;
                }
            }

            ushort IDictionary<int, ushort>.this[int i]
            {
                get
                {
                    ushort glyphIndex;
                    if (!TryGetValue(i, out glyphIndex))
                        throw new KeyNotFoundException();
                    return glyphIndex;
                }
                set
                {
                    throw new NotSupportedException();
                }
            }

            #endregion

            #region ICollection<KeyValuePair<int,ushort>> Members

            public void Add(KeyValuePair<int, ushort> item)
            {
                throw new NotSupportedException();
            }

            public void Clear()
            {
                throw new NotSupportedException();
            }

            public bool Contains(KeyValuePair<int, ushort> item)
            {
                return ContainsKey(item.Key);
            }

            public void CopyTo(KeyValuePair<int, ushort>[] array, int arrayIndex)
            {
                if (array == null)
                {
                    throw new ArgumentNullException("array");
                }

                if (array.Rank != 1)
                {
                    throw new ArgumentException(SR.Get(SRID.Collection_BadRank));
                }

                // The extra "arrayIndex >= array.Length" check in because even if _collection.Count
                // is 0 the index is not allowed to be equal or greater than the length
                // (from the MSDN ICollection docs)
                if (arrayIndex < 0 || arrayIndex >= array.Length || (arrayIndex + Count) > array.Length)
                {
                    throw new ArgumentOutOfRangeException("arrayIndex");
                }

                foreach (KeyValuePair<int, ushort> pair in this)
                    array[arrayIndex++] = pair;
            }

            public int Count
            {
                get { return CMap.Count; }
            }

            public bool IsReadOnly
            {
                get { return true; }
            }

            public bool Remove(KeyValuePair<int, ushort> item)
            {
                throw new NotSupportedException();
            }

            #endregion

            #region IEnumerable<KeyValuePair<int,ushort>> Members

            public IEnumerator<KeyValuePair<int, ushort>> GetEnumerator()
            {
                return CMap.GetEnumerator();
            }

            #endregion

            #region IEnumerable Members

            IEnumerator IEnumerable.GetEnumerator()
            {
                return ((IEnumerable<KeyValuePair<int, ushort>>)this).GetEnumerator();
            }

            #endregion
        }

        #endregion

        //------------------------------------------------------
        //
        //  Private Fields
        //
        //------------------------------------------------------

        #region Private Fields
        private IntMap _cmap;

        // 'locl' feature which is language sensitive
        private static readonly uint[] LoclFeature = new uint[]
        {
           (uint)OpenTypeTags.locl
        };

        // common features for fast text
        // They are insensitive to languages
        private static readonly uint[] RequiredTypographyFeatures = new uint[]
        {
           (uint)OpenTypeTags.ccmp,
           (uint)OpenTypeTags.rlig,
           (uint)OpenTypeTags.liga,
           (uint)OpenTypeTags.clig,
           (uint)OpenTypeTags.calt,
           (uint)OpenTypeTags.kern,
           (uint)OpenTypeTags.mark,
           (uint)OpenTypeTags.mkmk
        };

        // All required features
        private static readonly uint[] RequiredFeatures = new uint[]
        {
           (uint)OpenTypeTags.locl,
           (uint)OpenTypeTags.ccmp,
           (uint)OpenTypeTags.rlig,
           (uint)OpenTypeTags.liga,
           (uint)OpenTypeTags.clig,
           (uint)OpenTypeTags.calt,
           (uint)OpenTypeTags.kern,
           (uint)OpenTypeTags.mark,
           (uint)OpenTypeTags.mkmk
        };

        private static readonly UnicodeRange[] fastTextRanges = new UnicodeRange[]
        {
            new UnicodeRange(0x20  , 0x7e  ),    // basic latin
            new UnicodeRange(0xA1  , 0xFF  ),    // latin-1 supplement,
            new UnicodeRange(0x0100, 0x17F ),    // latin extended-A
            new UnicodeRange(0x0180, 0x024F),    // latin extended-B
            new UnicodeRange(0x1E00, 0x1EFF),    // latin extended additional (Vietnamese precomposed)
            new UnicodeRange(0x3040, 0x3098),    // hiragana
            new UnicodeRange(0x309B, 0x309F),    // hiragana
            new UnicodeRange(0x30A0, 0x30FF)     // kana
        };

        #endregion Private Fields
    }

    /// <summary>
    /// An implementation of IOpenTypeFont which only provides GSUB and GPOS tables
    /// It is used by OTLS API to determine the optimizable script.
    /// </summary>
    /// <remarks>
    /// OTLS API always accepts IOpenTypeFont as input parameter. To be consistent, we
    /// implement this IOpenTypeFont just for OpenTypeLayout.GetComplexLanguangeList(..) method.
    /// </remarks>
    internal sealed class GsubGposTables : IOpenTypeFont
    {
        internal GsubGposTables(FontFaceLayoutInfo layout)
        {
            _layout = layout;
            _gsubTable = new FontTable(_layout.Gsub());
            _gposTable = new FontTable(_layout.Gpos());
}

        /// <summary>
        /// Returns font table data
        /// </summary>
        public FontTable GetFontTable(OpenTypeTags TableTag)
        {
            switch (TableTag)
            {
                case OpenTypeTags.GSUB:
                    {
                        return _gsubTable;
                    }
                case OpenTypeTags.GPOS:
                    {
                        return _gposTable;
                    }
                default:
                    {
                        throw new NotSupportedException();
                    }
            }
        }

        /// <summary>
        /// Returns glyph coordinate
        /// </summary>
        public LayoutOffset GetGlyphPointCoord(ushort Glyph, ushort PointIndex)
        {
            throw new NotSupportedException();
        }

        /// <summary>
        /// Returns cache for layout table. If cache not found, return null Checked pointer
        /// </summary>
        public byte[] GetTableCache(OpenTypeTags tableTag)
        {
            return _layout.GetTableCache(tableTag);
        }

        /// <summary>
        /// Allocate space for layout table cache.
        /// </summary>
        public byte[] AllocateTableCache(OpenTypeTags tableTag, int size)
        {
            return _layout.AllocateTableCache(tableTag, size);
        }

        private FontTable _gsubTable;
        private FontTable _gposTable;
        private FontFaceLayoutInfo _layout;
    }

    /// <summary>
    /// A unicode range identified by a pair of first and last unicode code point
    /// </summary>
    internal struct UnicodeRange
    {
        internal UnicodeRange(int first, int last)
        {
            firstChar = first;
            lastChar = last;
        }

        //
        // In order to get the glyph indices of all of the fast ranges efficiently,
        // it's necessary for us to pass a full array to TextInterface components 
        // containing all the codepoints we want glyph indices for.
        // Generate such an array here on demand
        //
        internal uint[] GetFullRange()
        {
            int smaller = Math.Min(lastChar, firstChar);
            int larger = Math.Max(lastChar, firstChar);
            int arrayLength = larger - smaller + 1;
            
            uint[] unicodeArray = new uint[arrayLength];
            for (int i = 0; i < arrayLength; i++)
            {
                unicodeArray[i] = checked((uint)(smaller + i));
            }

            return unicodeArray;
        }

        internal int firstChar;
        internal int lastChar;
    }

    /// <summary>
    /// Major language targetted for optimization
    /// </summary>
    internal static class MajorLanguages
    {
        /// <summary>
        /// check if input script and langSys is considered a major language.
        /// </summary>
        /// <returns> true if it is a major language </returns>
        internal static bool Contains(ScriptTags script, LanguageTags langSys)
        {
            for (int i = 0; i < majorLanguages.Length; i++)
            {
                if (script == majorLanguages[i].Script &&
                    (langSys == LanguageTags.Default || langSys == majorLanguages[i].LangSys))
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// check if input culture is considered a major language.
        /// </summary>
        /// <returns> true if it is a major language </returns>
        internal static bool Contains(CultureInfo culture)
        {
            if (culture == null) return false;

            // explicitly check for InvariantCulture. We don't need to check for its parent.
            if (culture == CultureInfo.InvariantCulture) return true;

            for (int i = 0; i < majorLanguages.Length; i++)
            {
                if (majorLanguages[i].Culture.Equals(culture)
                   || majorLanguages[i].Culture.Equals(culture.Parent)
                   )
                {
                    return true;
                }
            }
            return false;
        }

        // major languages
        private static readonly MajorLanguageDesc[] majorLanguages = new MajorLanguageDesc[]
            {
                new MajorLanguageDesc(new CultureInfo("en"), ScriptTags.Latin,          LanguageTags.English),  // English neutral culture
                new MajorLanguageDesc(new CultureInfo("de"), ScriptTags.Latin,          LanguageTags.German),   // German neutral culture
                new MajorLanguageDesc(new CultureInfo("ja"), ScriptTags.CJKIdeographic, LanguageTags.Japanese),  // Japanese neutral culture
                new MajorLanguageDesc(new CultureInfo("ja"), ScriptTags.Hiragana,       LanguageTags.Japanese)  // Japanese neutral culture
            };

        private struct MajorLanguageDesc
        {
            internal MajorLanguageDesc(CultureInfo culture, ScriptTags script, LanguageTags langSys)
            {
                Culture = culture;
                Script = script;
                LangSys = langSys;
            }

            internal readonly CultureInfo Culture;
            internal readonly ScriptTags Script;
            internal readonly LanguageTags LangSys;
        }
    }


    /// <summary>
    /// An enum flag indicating the availabilities of various open type
    /// look ups.
    /// </summary>
    /// <remarks>
    /// The enum is used to determine whether fast path is applicable.
    /// Ideo     refers to Ideographs
    /// FastText refers to Other optimizable text
    /// We keep a minimum set of flags here to allow us reliably optimize
    /// the most common inputs. It is not to prevent under-optimization for
    /// all cases.
    /// </remarks>
    [Flags]
    internal enum TypographyAvailabilities
    {
        /// <summary>
        /// No required OpenType typography features is available
        /// </summary>
        None = 0,

        /// <summary>
        /// There are some lookup available for required typography
        /// features
        /// </summary>
        Available = 1,

        /// <summary>
        /// There are some lookup available for required typography features
        /// for Ideographic script.
        /// </summary>
        IdeoTypographyAvailable = 2,

        /// <summary>
        /// There are lookup available for required typography features
        /// for fast text
        /// </summary>
        FastTextTypographyAvailable = 4,

        /// <summary>
        /// There are localized form available for major Ui lanaguages for fast text
        /// </summary>
        /// <seealso> MajorLanguages class</seealso>
        FastTextMajorLanguageLocalizedFormAvailable = 8,

        /// <summary>
        /// There are localized form for non major Ui language available for fast text
        /// </summary>
        FastTextExtraLanguageLocalizedFormAvailable = 16,
    }
}
