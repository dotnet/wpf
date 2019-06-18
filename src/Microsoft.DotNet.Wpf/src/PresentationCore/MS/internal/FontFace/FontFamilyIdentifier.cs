// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// 
// Description: FontFamilyIdentifier type
//
//

using System;
using System.Diagnostics;
using System.Security;
using System.Text;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.TextFormatting;

using MS.Utility;
using MS.Internal;
using MS.Internal.Shaping;
using MS.Internal.FontCache;
using MS.Internal.TextFormatting;

namespace MS.Internal.FontFace
{
    /// <summary>
    /// The FontFamilyIdentifier value type encapsulates a friendly name and base Uri
    /// and provides access to the corresponding canonical name(s) via an indexer.
    /// </summary>
    //
    // The friendly name is a string in the format passed to the FontFamily constructor and the 
    // corresponding type converter. It comprises one or more font family references separated by
    // commas (with literal commas escaped by doubling). Each font family reference comprises a
    // family name and an optional location.
    //
    // Pseudo-BNF:
    // 
    //      friendlyName        =  ESCAPED(fontFamilyReference) *( "," ESCAPED(fontFamilyReference) )
    //      fontFamilyReference =  [ location "#" ] escapedFamilyName
    //      location            =  "" | relativeUri | absoluteUri
    // 
    //      where ESCAPED(fontFamilyReference) denotes a fontFamilyReference in which comma characters (",") 
    //      have been replaced by doubled commas (",,").
    // 
    //      Both the location (if present) and the escapedFamilyName may contain hexadecimal escape
    //      sequences in the form %XX as in URI references.
    // 
    //      The location may be an empty string so "#ARIAL" is a valid font family reference;
    //      in fact it is the normalized form of "Arial".
    //
    // Canonicalization
    //
    //      Canonicalization is the process of converting a font family reference to an absolute URI
    //      which specifies the font family location, plus a fragment which specifies the family name.
    //      The family name is converted to uppercase (since family names are not case-sensitive) but 
    //      the rest of the URI is not.
    //
    //      The process for canonicalizing the entire friendly name is as follows:
    // 
    //      1.  Split the friendly name into font family references.
    //          Treat single commas (",") as delimiters, and unescape double commas (",,").
    // 
    //      2.  Convert each font family reference to a normalized form.
    //          See Util.NormalizeFontFamilyReference.
    // 
    //      3.  Canonicalize the normalized font family reference.
    //          See Util.GetCanonicalUriForFamily.
    // 
    //      This is essentially what the Canonicalize method does, with addition of caching the resulting
    //      canonical names in the TypefaceMetricsCache. To save working set, the result of canonicalization 
    //      is stored as a single null-delimited string rather than an array of strings.
    //
    //      Since canonicalization is potentially expensive, we do not always canonicalize the entire 
    //      friendly name. In this case, the indexer canonicalizes the requested font family reference 
    //      on demand. However, we still cache the result in the TypefaceMetricsCache.
    // 
    internal struct FontFamilyIdentifier
    {
        /// <summary>
        /// FontFamilyIdentifier constructor
        /// </summary>
        /// <param name="friendlyName">friendly name in the format passed to
        /// FontFamily constructor and type converter.</param>
        /// <param name="baseUri">Base Uri used to resolve the location part of a font family reference
        /// if one exists and is relative</param>
        internal FontFamilyIdentifier(string friendlyName, Uri baseUri)
        {
            _friendlyName = friendlyName;
            _baseUri = baseUri;
            _tokenCount = (friendlyName != null) ? -1 : 0;
            _canonicalReferences = null;
        }

        /// <summary>
        /// Create a FontFamilyIdentifier by concatenating two existing identifiers.
        /// </summary>
        internal FontFamilyIdentifier(FontFamilyIdentifier first, FontFamilyIdentifier second)
        {
            first.Canonicalize();
            second.Canonicalize();

            _friendlyName = null;
            _tokenCount = first._tokenCount + second._tokenCount;
            _baseUri = null;

            if (first._tokenCount == 0)
            {
                _canonicalReferences = second._canonicalReferences;
            }
            else if (second._tokenCount == 0)
            {
                _canonicalReferences = first._canonicalReferences;
            }
            else
            {
                _canonicalReferences = new CanonicalFontFamilyReference[_tokenCount];

                int i = 0;
                foreach (CanonicalFontFamilyReference family in first._canonicalReferences)
                {
                    _canonicalReferences[i++] = family;
                }
                foreach (CanonicalFontFamilyReference family in second._canonicalReferences)
                {
                    _canonicalReferences[i++] = family;
                }
            }
        }

        internal string Source
        {
            get { return _friendlyName; }
        }

        internal Uri BaseUri
        {
            get { return _baseUri; }
        }

        public bool Equals(FontFamilyIdentifier other)
        {
            if (_friendlyName == other._friendlyName && _baseUri == other._baseUri)
                return true;

            int c = Count;
            if (other.Count != c)
                return false;

            if (c != 0)
            {
                Canonicalize();
                other.Canonicalize();

                for (int i = 0; i < c; ++i)
                {
                    if (!_canonicalReferences[i].Equals(other._canonicalReferences[i]))
                        return false;
                }
            }

            return true;
        }

        public override bool Equals(object obj)
        {
            return obj is FontFamilyIdentifier && Equals((FontFamilyIdentifier)obj);
        }

        public override int GetHashCode()
        {
            int hash = 1;

            if (Count != 0)
            {
                Canonicalize();

                foreach (CanonicalFontFamilyReference family in _canonicalReferences)
                {
                    hash = HashFn.HashMultiply(hash) + family.GetHashCode();
                }
            }

            return HashFn.HashScramble(hash);
        }

        internal int Count
        {
            get
            {
                // negative value represents uninitialized count
                if (_tokenCount < 0)
                {
                    _tokenCount = CountTokens(_friendlyName);
                }
                return _tokenCount;
            }
        }


        internal CanonicalFontFamilyReference this[int tokenIndex]
        {
            get
            {
                if (tokenIndex < 0 || tokenIndex >= Count)
                    throw new ArgumentOutOfRangeException("tokenIndex");

                // Have we already been canonicalized?
                if (_canonicalReferences != null)
                {
                    // We have already canonicalized. This is typically the case for longer-lived
                    // identifiers such as belong to FontFamily objects.
                    return _canonicalReferences[tokenIndex];
                }
                else
                {
                    // We have not already canonicalized. This is probably a short-lived object so
                    // it's not worthwhile to canonicalize all the names up front and cache them for
                    // later use. Just canonicalize each name as we need it.

                    // find the Nth font family reference.
                    int i, length;
                    int j = FindToken(_friendlyName, 0, out i, out length);
                    for (int k = 0; k < tokenIndex; ++k)
                    {
                        j = FindToken(_friendlyName, j, out i, out length);
                    }

                    // canonicalize just this font family reference
                    return GetCanonicalReference(i, length);
                }
            }
        }

        internal void Canonicalize()
        {
            if (_canonicalReferences != null)
                return;
                
            int count = this.Count;
            if (count == 0)
                return;

            // First look up the entire friendly name in the cache; this may enable us to
            // save working set by sharing the same array of may equal FontFamilyIdentifier.
            BasedFriendlyName hashKey = new BasedFriendlyName(_baseUri, _friendlyName);
            CanonicalFontFamilyReference[] canonicalReferences = TypefaceMetricsCache.ReadonlyLookup(hashKey) as CanonicalFontFamilyReference[];

            if (canonicalReferences == null)
            {
                // We need to construct a new array.
                canonicalReferences = new CanonicalFontFamilyReference[count];

                // Add the first canonical family reference.
                int i, length;
                int j = FindToken(_friendlyName, 0, out i, out length);
                canonicalReferences[0] = GetCanonicalReference(i, length);

                // Add subsequent family references.
                for (int k = 1; k < count; ++k)
                {
                    j = FindToken(_friendlyName, j, out i, out length);
                    canonicalReferences[k] = GetCanonicalReference(i, length);
                }

                // Add the array to the cache.
                TypefaceMetricsCache.Add(hashKey, canonicalReferences);
            }

            // for thread safety, we assign to the field only after the array is fully initialized
            _canonicalReferences = canonicalReferences;
        }

        #region Friendly name parsing methods

        private static int CountTokens(string friendlyName)
        {
            int count = 0;

            int i, length;
            int j = FindToken(friendlyName, 0, out i, out length);
            while (j >= 0)
            {
                // Limit the number of family names in a single string.
                if (++count == MaxFamilyNamePerFamilyMapTarget)
                    break;

                j = FindToken(friendlyName, j, out i, out length);
            }
            return count;
        }

        /// <summary>
        /// Scans the specified friendly name starting at the specified index and gets the index and
        /// length of the first token it finds.
        /// </summary>
        /// <param name="friendlyName">friendly name containing zero or more comma-delimited tokens</param>
        /// <param name="i">character index to scan from</param>
        /// <param name="tokenIndex">receives the index of the token (or zero if none)</param>
        /// <param name="tokenLength">receives the length of the token (or zero if none)</param>
        /// <returns>If a token was found, the return value is a positive integer specifying where to begin 
        /// scanning for the next token; if no token was found, the return value is -1.</returns>
        private static int FindToken(string friendlyName, int i, out int tokenIndex, out int tokenLength)
        {
            int length = friendlyName.Length;
            while (i < length)
            {
                // skip leading whitespace
                while (i < length && char.IsWhiteSpace(friendlyName[i]))
                    ++i;

                int begin = i;

                // find delimiter or end of string
                while (i < length)
                {
                    if (friendlyName[i] == FamilyNameDelimiter)
                    {
                        if (i + 1 < length && friendlyName[i + 1] == FamilyNameDelimiter)
                        {
                            // Don't treat double commas as the family name delimiter.
                            i += 2;
                        }
                        else
                        {
                            break; // single comma delimiter
                        }
                    }
                    else if (friendlyName[i] == '\0')
                    {
                        break; // might as well treat null as a delimiter too
                    }
                    else
                    {
                        ++i;
                    }
                }

                // exclude trailing whitespace
                int end = i;
                while (end > begin && char.IsWhiteSpace(friendlyName[end - 1]))
                    --end;

                // make sure it's not an empty string
                if (begin < end)
                {
                    tokenIndex = begin;
                    tokenLength = end - begin;
                    return i + 1;
                }

                // continue after delimiter
                ++i;
            }

            // no token
            tokenIndex = length;
            tokenLength = 0;
            return -1;
        }

        private CanonicalFontFamilyReference GetCanonicalReference(int startIndex, int length)
        {
            string normalizedString = Util.GetNormalizedFontFamilyReference(_friendlyName, startIndex, length);

            // For caching normalized names, we use a different type of key which does not compare equal
            // to the keys we used for caching friendly names.
            BasedNormalizedName hashKey = new BasedNormalizedName(_baseUri, normalizedString);

            // Look up the normalized string and base URI in the cache?
            CanonicalFontFamilyReference canonicalReference = TypefaceMetricsCache.ReadonlyLookup(hashKey) as CanonicalFontFamilyReference;

            // Do we already have a cached font family reference?
            if (canonicalReference == null)
            {
                // Not in cache. Construct a new font family reference.
                canonicalReference = CanonicalFontFamilyReference.Create(_baseUri, normalizedString);

                // Add it to the cache.
                TypefaceMetricsCache.Add(hashKey, canonicalReference);
            }

            return canonicalReference;         
        }

        #endregion

        #region BasedFriendlyName and BasedNormalizedName

        /// <summary>
        /// BasedFriendlyName represents a friendly name with an associated URI or use as a hash key.
        /// </summary>
        private sealed class BasedFriendlyName : BasedName
        {
            public BasedFriendlyName(Uri baseUri, string name)
                : base(baseUri, name)
            {
            }

            public override int GetHashCode()
            {
                // Specify a different seed than BasedNormalizedName
                return InternalGetHashCode(1);
            }

            public override bool Equals(object obj)
            {
                // Only compare equal to other BasedFriendlyName objects
                return InternalEquals(obj as BasedFriendlyName);
            }
        }

        /// <summary>
        /// BasedNormalizedName represents a normalized name with an associated URI or use as a hash key.
        /// </summary>
        private sealed class BasedNormalizedName : BasedName
        {
            public BasedNormalizedName(Uri baseUri, string name)
                : base(baseUri, name)
            {
            }

            public override int GetHashCode()
            {
                // Specify a different seed than BasedFriendlyName
                return InternalGetHashCode(int.MaxValue);
            }

            public override bool Equals(object obj)
            {
                // Only compare equal to other BasedNormalizedName objects
                return InternalEquals(obj as BasedNormalizedName);
            }
        }

        /// <summary>
        /// BasedName implements shared functionality of BasedFriendlyName and BasedNormalizedName. 
        /// The reason for the two derived classes (and for not just using Pair) is that we don't
        /// want these two different types of keys to compare equal.
        /// </summary>
        private abstract class BasedName
        {
            private Uri _baseUri;
            private string _name;

            protected BasedName(Uri baseUri, string name)
            {
                _baseUri = baseUri;
                _name = name;
            }

            public abstract override int GetHashCode();
            public abstract override bool Equals(object obj);

            protected int InternalGetHashCode(int seed)
            {
                int hash = seed;

                if (_baseUri != null)
                    hash += HashFn.HashMultiply(_baseUri.GetHashCode());

                if (_name != null)
                    hash = HashFn.HashMultiply(hash) + _name.GetHashCode();

                return HashFn.HashScramble(hash);
            }

            protected bool InternalEquals(BasedName other)
            {
                return other != null &&
                    other._baseUri == _baseUri &&
                    other._name == _name;
            }
        }
        #endregion

        private string _friendlyName;
        private Uri _baseUri;
        private int _tokenCount;
        private CanonicalFontFamilyReference[] _canonicalReferences;

        internal const char FamilyNameDelimiter = ',';
        internal const int  MaxFamilyNamePerFamilyMapTarget = 32;
    }
}
