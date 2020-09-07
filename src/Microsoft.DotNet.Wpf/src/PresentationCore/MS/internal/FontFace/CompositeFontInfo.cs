// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//+-----------------------------------------------------------------------
//
//
//
//  Contents:  Composite font info parsed from composite font file
//
//

using System;
using System.Globalization;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows;
using System.Windows.Markup;
using System.Windows.Media;

using SR=MS.Internal.PresentationCore.SR;
using SRID=MS.Internal.PresentationCore.SRID;

namespace MS.Internal.FontFace
{
    /// <summary>
    /// Composite font info
    /// </summary>
    internal sealed class CompositeFontInfo
    {
        private LanguageSpecificStringDictionary    _familyNames;
        private double                              _baseline;
        private double                              _lineSpacing;
        private FamilyTypefaceCollection            _familyTypefaces;
        private FontFamilyMapCollection             _familyMaps;
        private ushort[]                            _defaultFamilyMapRanges;
        private Dictionary<XmlLanguage, ushort[]>   _familyMapRangesByLanguage;


        private const int InitialCultureCount = 1;  // at least a familyMap for one locale available
        private const int InitialTargetFamilyCount = 1;


        /// <summary>
        /// Construct a composite font
        /// </summary>
        internal CompositeFontInfo()
        {
            _familyNames = new LanguageSpecificStringDictionary(new Dictionary<XmlLanguage,string>(InitialCultureCount));
            _familyMaps = new FontFamilyMapCollection(this);
            _defaultFamilyMapRanges = EmptyFamilyMapRanges;
        }

        /// <summary>
        /// Called by FontFamilyMapCollection when a FontFamilyMap is being added.
        /// </summary>
        internal void PrepareToAddFamilyMap(FontFamilyMap familyMap)
        {
            // Validate parameters.
            if (familyMap == null)
                throw new ArgumentNullException("familyMap");

            if (string.IsNullOrEmpty(familyMap.Target))
                throw new ArgumentException(SR.Get(SRID.FamilyMap_TargetNotSet));

            // If it's culture-specific make sure it's in the hash table.
            if (familyMap.Language != null)
            {
                if (_familyMapRangesByLanguage == null)
                {
                    _familyMapRangesByLanguage = new Dictionary<XmlLanguage, ushort[]>(InitialCultureCount);
                    _familyMapRangesByLanguage.Add(familyMap.Language, EmptyFamilyMapRanges);
                }
                else if (!_familyMapRangesByLanguage.ContainsKey(familyMap.Language))
                {
                    _familyMapRangesByLanguage.Add(familyMap.Language, EmptyFamilyMapRanges);
                }
            }
        }

        #region family map ranges (skip lists)

        /// <summary>
        /// FontFamilyMap ranges (aka. skip lists) are an optimization to speed up family map lookup.
        /// 
        /// OBSERVABLE BEHAVIOR
        /// 
        ///     The observable behavior of family map lookup should be as if we traverse the
        ///     list of family maps sequentially and return the first one that matches both
        ///     the text culture and the code point.
        ///
        ///     The language matches if the family map language is null, or is equal to the text
        ///     language, or if the family map language's "range" includes the text culture.
        ///     This logic is implemented by FontFamilyMap.MatchCulture() and
        ///     FontFamilyMap.MatchLanguage, which call XmlLanguage.RangeIncludes().
        /// 
        /// 
        /// SKIP LISTS
        /// 
        ///     Skip lists allow us to avoid doing the culture comparisons described above on 
        ///     every character lookup. Instead, we generate a skip list once the first time we
        ///     use a particular culture, and then use the skip list to determine which family
        ///     maps to look at and which to skip.
        /// 
        ///     A skip list is an array of ushort. The first array member represents the size of
        ///     the family map list and is used to determine whether the skip list is invalid 
        ///     (see Invalidating Skip Lists). The remainder of the skip list (beginning at 
        ///     at index FirstFamilyMapRange) consists of pairs of ushort values. Each pair
        ///     denotes a range of family maps in the family maps list; the first member of the
        ///     pair is the index of the first element in the range, and the second is the index
        ///     one past the last element in the range. Collectively, these ranges include all of
        ///     the family maps that should be included in the lookup for a culture, i.e., the
        ///     culture associated with the skip list.
        ///
        ///     Following is an example of a family map list and the corresponding skip lists:
        /// 
        ///     0       1       2       3       4       5       6       7       8
        ///     +-------+-------+-------+-------+-------+-------+-------+-------+
        ///     |   ja  |   ja  |   ko  |   ko  | zh-CHT| zh-CHS|  any  |  any  |
        ///     +-------+-------+-------+-------+-------+-------+-------+-------+
        /// 
        ///     "ja"     -> (0,2) (6,8)
        ///     "ko"     -> (2,4) (6,8)
        ///     "zh-CHT" -> (4,5) (6,8)
        ///     "zh-CHS" -> (5,8)
        ///     default  -> (6,8)
        /// 
        /// INVALIDATING SKIP LISTS
        /// 
        ///     A skip list becomes invalid whenever the family map list changes. To avoid
        ///     recreating skip lists every time a family map is added, skip lists are created
        ///     lazily. Skip lists are added to the _familyMapRangesByLanguage hash table as
        ///     family maps are added, but each skip list is initialized by EmptyFamilyMapRanges.
        /// 
        ///     After a skip list has been created, a may be rendered invalid by subsequent 
        ///     changes to the family map list. We have two mechanisms to detect this.
        /// 
        ///       (1)  Each skip list includes (as its first member) the size of the family
        ///            map when the skip list was created. Additions to or insertions into 
        ///            the list can therefore be detected because the sizes no longer match.
        /// 
        ///       (2)  For all other changes (removing items changing items), the FamilyMaps
        ///            list calls InvalidateFamilyMapRanges(), which setes all skip lists to
        ///            EmptyFamilyMapRanges.
        /// 
        /// </summary>

        private static readonly ushort[] EmptyFamilyMapRanges = new ushort[] { 0 };
        private const int InitialFamilyMapRangesCapacity = 7; // count + 3 ranges
        internal const int FirstFamilyMapRange = 1;

        /// <summary>
        /// Called by FontFamilyMapCollection when a change occurs that renders all
        /// family map ranges potentially invalid.
        /// </summary>
        internal void InvalidateFamilyMapRanges()
        {
            _defaultFamilyMapRanges = EmptyFamilyMapRanges;

            if (_familyMapRangesByLanguage != null)
            {
                Dictionary<XmlLanguage, ushort[]> table = new Dictionary<XmlLanguage, ushort[]>(_familyMapRangesByLanguage.Count);
                foreach (XmlLanguage language in _familyMapRangesByLanguage.Keys)
                {
                    table.Add(language, EmptyFamilyMapRanges);
                }
                _familyMapRangesByLanguage = table;
            }
        }

        /// <summary>
        /// Returns information about which family maps apply to the specified culture.
        /// The return value is used by GetFamilyMapOfChar.
        /// </summary>
        internal ushort[] GetFamilyMapsOfLanguage(XmlLanguage language)
        {
            ushort[] ranges = null;

            // Look for a family map range for the specified language or one of its matching languages
            if (_familyMapRangesByLanguage != null && language != null)
            {
                foreach (XmlLanguage matchingLanguage in language.MatchingLanguages)
                {
                    // break out of loop to handle default list of ranges
                    if (matchingLanguage.IetfLanguageTag.Length == 0)
                        break;

                    if (_familyMapRangesByLanguage.TryGetValue(matchingLanguage, out ranges))
                    {
                        // Recreate the list of ranges if we've added more family maps.
                        if (!IsFamilyMapRangesValid(ranges))
                        {
                            ranges = CreateFamilyMapRanges(matchingLanguage);
                            _familyMapRangesByLanguage[matchingLanguage] = ranges;
                        }
                        return ranges;
                    }
                }
            }

            // Use the default list of ranges (containing only family maps that match
            // any culture); recreate it if we've added more family maps.
            if (!IsFamilyMapRangesValid(_defaultFamilyMapRanges))
            {
                _defaultFamilyMapRanges = CreateFamilyMapRanges(null);
            }

            return _defaultFamilyMapRanges;
        }

        /// <summary>
        /// Gets the first FontFamilyMap that matches the specified Unicode scalar value.
        /// </summary>
        /// <param name="familyMapRanges">Return value of GetFamilyMapsOfCulture.</param>
        /// <param name="ch">Character to map.</param>
        /// <returns>FontFamilyMap or null.</returns>
        internal FontFamilyMap GetFamilyMapOfChar(ushort[] familyMapRanges, int ch)
        {
            Debug.Assert(IsFamilyMapRangesValid(familyMapRanges));

            // Iterate over the ushort pairs in the skip list.
            for (int i = FirstFamilyMapRange; i < familyMapRanges.Length; i += 2)
            {
                // Each pair specifies a range in the family map list.
                int begin = familyMapRanges[i];
                int end = familyMapRanges[i + 1];
                Debug.Assert(begin < end && end <= _familyMaps.Count);

                // Iterate over the family maps in the specified range.
                for (int j = begin; j < end; ++j)
                {
                    FontFamilyMap familyMap = _familyMaps[j];
                    Invariant.Assert(familyMap != null);
                    if (familyMap.InRange(ch))
                        return familyMap;
                }
            }

            return FontFamilyMap.Default;
        }

        private bool IsFamilyMapRangesValid(ushort[] familyMapRanges)
        {
            return familyMapRanges[0] == _familyMaps.Count;
        }

        private ushort[] CreateFamilyMapRanges(XmlLanguage language)
        {
            // We could use an ArrayList, but a ushort[] is not much more code
            // and requires many fewer boxed objects.
            ushort[] ranges = new ushort[InitialFamilyMapRangesCapacity];
            ranges[0] = (ushort)_familyMaps.Count;
            int count = 1;

            Debug.Assert(count == FirstFamilyMapRange);

            for (int i = 0; i < _familyMaps.Count; ++i)
            {
                if (FontFamilyMap.MatchLanguage(_familyMaps[i].Language, language))
                {
                    // grow ranges if necessary.
                    if (count + 2 > ranges.Length)
                    {
                        ushort[] temp = new ushort[ranges.Length * 2 - FirstFamilyMapRange];
                        ranges.CopyTo(temp, 0);
                        ranges = temp;
                    }

                    // beginning of range
                    ranges[count++] = (ushort)i;

                    ++i;
                    while (i < _familyMaps.Count && FontFamilyMap.MatchLanguage(_familyMaps[i].Language, language))
                    {
                        ++i;
                    }

                    // end of range, i.e., last index + 1
                    ranges[count++] = (ushort)i;
                }
            }

            // reallocate ranges to the exact size required
            if (count < ranges.Length)
            {
                ushort[] temp = new ushort[count];
                Array.Copy(ranges, temp, count);
                ranges = temp;
            }

            return ranges;
        }

        #endregion

        /// <summary>
        /// List of typefaces; can be null.
        /// </summary>
        internal FamilyTypefaceCollection FamilyTypefaces
        {
            get { return _familyTypefaces; }
        }

        /// <summary>
        /// Gets the list of family typefaces, creating it if necessary.
        /// </summary>
        internal FamilyTypefaceCollection GetFamilyTypefaceList()
        {
            if (_familyTypefaces == null)
                _familyTypefaces = new FamilyTypefaceCollection();

            return _familyTypefaces;
        }

        /// <summary>
        /// Distance from character cell top to English baseline relative to em size. 
        /// </summary>
        internal double Baseline 
        { 
            get { return _baseline; }
            set 
            {
                CompositeFontParser.VerifyNonNegativeMultiplierOfEm("Baseline", ref value);
                _baseline = value;
            }
        }


        /// <summary>
        /// Additional line spacing after Height relative to em size
        /// </summary>
        internal double LineSpacing
        {
            get { return _lineSpacing; }
            set
            {
                CompositeFontParser.VerifyPositiveMultiplierOfEm("LineSpacing", ref value);
                _lineSpacing = value;
            }
        }


        /// <summary>
        /// Dictionary of names by culture.
        /// </summary>
        internal LanguageSpecificStringDictionary FamilyNames
        {
            get { return _familyNames; }
        }


        /// <summary>
        /// List of family maps.
        /// </summary>
        internal FontFamilyMapCollection FamilyMaps
        {
            get { return _familyMaps; }
        }


        /// <summary>
        /// Collection of cultures associated with family maps; can be null
        /// if all family maps are culture-independent.
        /// </summary>
        internal ICollection<XmlLanguage> FamilyMapLanguages
        {
            get
            {
                if (_familyMapRangesByLanguage != null)
                    return _familyMapRangesByLanguage.Keys;
                else
                    return null;
            }
        }
    }
}
