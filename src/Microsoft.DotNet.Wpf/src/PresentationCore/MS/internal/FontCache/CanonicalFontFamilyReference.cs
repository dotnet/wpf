// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
//
//  Description: The CanonicalFontFamilyReference class is the internal representation of a font
//               family reference. "Canonical" in this case means it has a normalized form and
//               the location part (if any) has been resolved to an absolute URI.
//
//               See spec at References.doc
//
//

using System;
using MS.Internal;
using System.Security;

namespace MS.Internal.FontCache
{
    internal sealed class CanonicalFontFamilyReference
    {
        /// <summary>
        /// Create a CanonicalFontFamilyReference given a base URI and string.
        /// </summary>
        /// <param name="baseUri">Base URI used to resolve the location part, if it is relative.</param>
        /// <param name="normalizedString">Font family reference string, in the normalized form returned 
        /// by Util.GetNormalizedFontFamilyReference.</param>
        /// <returns>Returns a new CanonicalFontFamilyReference or CanonicalFontFamilyReference.Unresolved.</returns>
        public static CanonicalFontFamilyReference Create(Uri baseUri, string normalizedString)
        {
            string locationString;
            string escapedFamilyName;

            if (SplitFontFamilyReference(normalizedString, out locationString, out escapedFamilyName))
            {
                Uri absoluteUri = null;
                string fileName = null;
                bool resolved = false;

                if (locationString == null || Util.IsReferenceToWindowsFonts(locationString))
                {
                    // No location (e.g., "#Arial") or file-name-only location (e.g., "arial.ttf#Arial")
                    fileName = locationString;
                    resolved = true;
                }
                else
                {
                    if (Uri.TryCreate(locationString, UriKind.Absolute, out absoluteUri))
                    {
                        // Location is an absolute URI. Make sure it's a supported scheme.
                        resolved = Util.IsSupportedSchemeForAbsoluteFontFamilyUri(absoluteUri);
                    }
                    else if (baseUri != null && Util.IsEnumerableFontUriScheme(baseUri))
                    {
                        // Location is relative to the base URI.
                        resolved = Uri.TryCreate(baseUri, locationString, out absoluteUri);
                    }
                }

                if (resolved)
                {
                    string unescapeFamilyName = Uri.UnescapeDataString(escapedFamilyName);
                    if (fileName != null)
                    {
                        return new CanonicalFontFamilyReference(fileName, unescapeFamilyName);
                    }
                    else
                    {
                        return new CanonicalFontFamilyReference(absoluteUri, unescapeFamilyName);
                    }
                }
            }

            return _unresolved;
        }

        /// <summary>
        /// Represents a font family reference that could not be resolved, e.g., because of an 
        /// invalid location or unsupported scheme.
        /// </summary>
        public static CanonicalFontFamilyReference Unresolved
        {
            get { return _unresolved; }
        }

        /// <summary>
        /// Font family name. This string is not URI encoded (escaped).
        /// </summary>
        public string FamilyName
        {
            get { return _familyName; }
        }

        /// <summary>
        /// If a font family reference's location part comprises a file name only (e.g., "arial.ttf#Arial")
        /// this property is the URI-encoded file name. In this case, the implied location of the file is
        /// the default Windows Fonts folder and the LocationUri property is null. In all other cases,
        /// this property is null.
        /// </summary>
        public string EscapedFileName
        {
            get;

            private set;
        }

        /// <summary>
        /// Gets the font location if a specific location is given; otherwise returns null indicating
        /// the default Windows Fonts folder.
        /// </summary>
        public Uri LocationUri
        {
            get { return _absoluteLocationUri; }
        }

        public bool Equals(CanonicalFontFamilyReference other)
        {
            return other != null &&
                other._absoluteLocationUri == _absoluteLocationUri &&
                other.EscapedFileName == EscapedFileName &&
                other._familyName == _familyName;
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as CanonicalFontFamilyReference);
        }

        public override int GetHashCode()
        {
            if (_absoluteLocationUri == null && EscapedFileName == null)
            {
                // Typical case where no location is specified
                return _familyName.GetHashCode();
            }
            else
            {
                // Either we have a URI or a file name, never both
                int hash = (_absoluteLocationUri != null) ? 
                    _absoluteLocationUri.GetHashCode() : 
                    EscapedFileName.GetHashCode();

                // Combine the location hash with the family name hash
                hash = HashFn.HashMultiply(hash) + _familyName.GetHashCode();
                return HashFn.HashScramble(hash);
            }
        }

        private CanonicalFontFamilyReference(string escapedFileName, string familyName)
        {
            EscapedFileName = escapedFileName;
            _familyName = familyName;
        }

        private CanonicalFontFamilyReference(Uri absoluteLocationUri, string familyName)
        {
            _absoluteLocationUri = absoluteLocationUri;
            _familyName = familyName;
        }

        private static bool SplitFontFamilyReference(string normalizedString, out string locationString, out string escapedFamilyName)
        {
            int familyNameIndex;

            if (normalizedString[0] == '#')
            {
                locationString = null;
                familyNameIndex = 1;
            }
            else
            {
                int i = normalizedString.IndexOf('#');
                locationString = normalizedString.Substring(0, i);
                familyNameIndex = i + 1;
            }

            if (familyNameIndex < normalizedString.Length)
            {
                escapedFamilyName = normalizedString.Substring(familyNameIndex);
                return true;
            }
            else
            {
                escapedFamilyName = null;
                return false;
            }
        }

        private Uri     _absoluteLocationUri;
        private string  _familyName;
        private static readonly CanonicalFontFamilyReference _unresolved = new CanonicalFontFamilyReference((Uri)null, string.Empty);
    }
}

