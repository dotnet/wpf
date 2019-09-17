// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
//
//
//  Contents:  FontFamily
//

using System;
using System.Text;
using System.IO;
using System.Globalization;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows;
using System.Windows.Markup;    // for XmlLanguage
using System.ComponentModel;
using System.ComponentModel.Design;

using MS.Utility;
using MS.Internal;
using MS.Internal.FontCache;
using MS.Internal.FontFace;
using MS.Internal.Shaping;
using System.Security;

using SR = MS.Internal.PresentationCore.SR;
using SRID = MS.Internal.PresentationCore.SRID;

// Since we disable PreSharp warnings in this file, we first need to disable warnings about unknown message numbers and unknown pragmas.
#pragma warning disable 1634, 1691

namespace System.Windows.Media
{
    /// <summary>
    /// Represents a family of related fonts. Fonts in a FontFamily differ only in style,
    /// weight, or stretch.
    /// </summary>
    [TypeConverter(typeof(FontFamilyConverter))]
    [ValueSerializer(typeof(FontFamilyValueSerializer))]
    [Localizability(LocalizationCategory.Font)]
    public class FontFamily
    {
        /// <summary>
        /// Family name originally passed to by user and information derived from it.
        /// </summary>
        private FontFamilyIdentifier _familyIdentifier;

        /// <summary>
        /// The first valid font family. If no valid font family can be resolved from 
        /// the given name, this will point to a NullFontFamily object.
        /// </summary>
        private IFontFamily _firstFontFamily;

        /// <summary>
        /// Null font is the font that has metrics but logically does not support any Unicode codepoint
        /// so whatever text we throw at it would result in being mapped to missing glyph.
        /// </summary>
        internal static readonly CanonicalFontFamilyReference NullFontFamilyCanonicalName = CanonicalFontFamilyReference.Create(null, "#ARIAL");

        internal const string GlobalUI = "#GLOBAL USER INTERFACE";

        internal static FontFamily FontFamilyGlobalUI = new FontFamily(GlobalUI);

        private static volatile FamilyCollection _defaultFamilyCollection = PreCreateDefaultFamilyCollection();

        private static FontFamilyMapCollection _emptyFamilyMaps = null;


        /// <summary>
        /// Constructs FontFamily from a string.
        /// </summary>
        /// <param name="familyName">Specifies one or more comma-separated family names, each
        /// of which may be either a regular family name string (e.g., "Arial") or a URI
        /// (e.g., "file:///c:/windows/fonts/#Arial").</param>
        public FontFamily(string familyName) : this(null, familyName)
        {}

        /// <summary>
        /// Constructs FontFamily from a string and an optional base URI.
        /// </summary>
        /// <param name="baseUri">Specifies the base URI used to resolve family names, typically 
        /// the URI of the document or element that refers to the font family. Can be null.</param>
        /// <param name="familyName">Specifies one or more comma-separated family names, each
        /// of which may be either a regular family name string (e.g., "Arial") or a URI
        /// (e.g., "file:///c:/windows/fonts/#Arial").</param>
        public FontFamily(Uri baseUri, string familyName)
        {
            if (familyName == null)
                throw new ArgumentNullException("familyName");

            if (baseUri != null && !baseUri.IsAbsoluteUri)
                throw new ArgumentException(SR.Get(SRID.UriNotAbsolute), "baseUri");

            _familyIdentifier = new FontFamilyIdentifier(familyName, baseUri);
        }

        internal FontFamily(FontFamilyIdentifier familyIdentifier)
        {
            _familyIdentifier = familyIdentifier;
        }

        /// <summary>
        /// Construct an anonymous font family, i.e., a composite font that is created
        /// programatically instead of referenced by name or URI.
        /// </summary>
        public FontFamily()
        {
            _familyIdentifier = new FontFamilyIdentifier(null, null);
            _firstFontFamily = new CompositeFontFamily();
        }

        /// <summary>
        /// Collection of culture-dependant family names.
        /// </summary>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Content)]
        public LanguageSpecificStringDictionary FamilyNames
        {
            get
            {
                CompositeFontFamily compositeFont = FirstFontFamily as CompositeFontFamily;
                if (compositeFont != null)
                {
                    // Return the read/write dictionary of family names.
                    return compositeFont.FamilyNames;
                }
                else
                {
                    // Return a wrapper for the cached family's read-only dictionary.
                    return new LanguageSpecificStringDictionary(FirstFontFamily.Names);
                }
            }
        }

        /// <summary>
        /// List of FamilyTypeface objects.
        /// </summary>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Content)]
        public FamilyTypefaceCollection FamilyTypefaces
        {
            get
            {
                CompositeFontFamily compositeFont = FirstFontFamily as CompositeFontFamily;
                if (compositeFont != null)
                {
                    // Return the read/write list of typefaces for the font.
                    return compositeFont.FamilyTypefaces;
                }
                else
                {
                    // Return a wrapper for the read-only collection of typefaces.
                    return new FamilyTypefaceCollection(FirstFontFamily.GetTypefaces(_familyIdentifier));
                }
            }
        }

        /// <summary>
        /// Collection of FontFamilyMap objects for an anonymous font family. For named font
        /// families, this property returns an empty, read-only list.
        /// </summary>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Content)]
        public FontFamilyMapCollection FamilyMaps
        {
            get
            {
                CompositeFontFamily compositeFont = FirstFontFamily as CompositeFontFamily;
                if (compositeFont != null)
                {
                    // Read the read/write list of family maps for the font.
                    return compositeFont.FamilyMaps;
                }
                else
                {
                    // Return an empty, read-only collection of FamilyMaps.
                    if (_emptyFamilyMaps == null)
                    {
                        _emptyFamilyMaps = new FontFamilyMapCollection(null);
                    }
                    return _emptyFamilyMaps;
                }
            }
        }

        /// <summary>
        /// Family names and/or URIs used to construct the font family.
        /// </summary>
        public string Source
        {
            get { return _familyIdentifier.Source; }
        }

        /// <summary>
        /// Base URI used to resolve family names, typically the URI of the document or element 
        /// that refers to the font family.
        /// </summary>
        /// <remarks>
        /// Family names are interpreted first relative to the base URI (if not null) and then
        /// relative to the default folder for installed fonts.
        /// </remarks>
        public Uri BaseUri
        {
            get { return _familyIdentifier.BaseUri; }
        }

        /// <summary>
        /// Return Source if there is one or String.Empty for unnamed
        /// font family.
        /// </summary>
        public override string ToString()
        {
            string source = _familyIdentifier.Source;
            return source != null ? source : string.Empty;
        }


        internal FontFamilyIdentifier FamilyIdentifier
        {
            get { return _familyIdentifier; }
        }


        /// <summary>
        /// Distance from character cell top to English baseline relative to em size. 
        /// </summary>
        public double Baseline
        {
            get
            {
                return FirstFontFamily.BaselineDesign;
            }

            set
            {
                VerifyMutable().SetBaseline(value);
            }
        }


        /// <summary>
        /// Recommended baseline-to-baseline distance for the text in this font relative to em size.
        /// </summary>
        public double LineSpacing
        {
            get
            {
                return FirstFontFamily.LineSpacingDesign;
            }

            set
            {
                VerifyMutable().SetLineSpacing(value);
            }
        }

        internal double GetLineSpacingForDisplayMode(double emSize, double pixelsPerDip)
        {
            return FirstFontFamily.LineSpacing(emSize, 1, pixelsPerDip, TextFormattingMode.Display);
        }

        /// <summary>
        /// Font families from the default system font location.
        /// </summary>
        /// <value>Collection of FontFamly objects from the default system font location.</value>
        [CLSCompliant(false)]
        public ICollection<Typeface> GetTypefaces()
        {
            return FirstFontFamily.GetTypefaces(_familyIdentifier);
        }


        /// <summary>
        /// Create correspondent hash code for the object
        /// </summary>
        /// <returns>object hash code</returns>
        public override int GetHashCode()
        {
            if (_familyIdentifier.Source != null)
            {
                // named font family: hash based on canonical name
                return _familyIdentifier.GetHashCode();
            }
            else
            {
                // unnamed family: hash is based on object identity
                return base.GetHashCode();
            }
        }


        /// <summary>
        /// Equality check
        /// </summary>
        public override bool Equals(object o)
        {
            FontFamily f = o as FontFamily;
            if (f == null)
            {
                // different types or o == null
                return false;
            }
            else if (_familyIdentifier.Source != null)
            {
                // named font family; compare canonical names
                return _familyIdentifier.Equals(f._familyIdentifier);
            }
            else
            {
                // unnamed font families are equal only if they're the same instance
                return base.Equals(o);
            }
        }


        /// <summary>
        /// Verifies that the FontFamily can be changed and returns a CompositeFontFamily
        /// </summary>
        private CompositeFontFamily VerifyMutable()
        {
            CompositeFontFamily mutableFamily = _firstFontFamily as CompositeFontFamily;

            if (mutableFamily == null)
            {
                throw new NotSupportedException(SR.Get(SRID.FontFamily_ReadOnly));
            }

            return mutableFamily;
        }
     

        /// <summary>
        /// First font family
        /// </summary>
        internal IFontFamily FirstFontFamily
        {
            get
            {
                IFontFamily family = _firstFontFamily;

                if (family == null)
                {
                    // Call Canonicalize() directly so it won't just be called on the boxed object.
                    _familyIdentifier.Canonicalize();

                    // Look up first font family from cache. If not found, construct a new one.
                    family = TypefaceMetricsCache.ReadonlyLookup(FamilyIdentifier) as IFontFamily;

                    if (family == null)
                    {
                        FontStyle style     = FontStyles.Normal;
                        FontWeight weight   = FontWeights.Normal;
                        FontStretch stretch = FontStretches.Normal;
                        family = FindFirstFontFamilyAndFace(ref style, ref weight, ref stretch);

                        if (family == null)
                        {
                            // fall back to null font
                            family = LookupFontFamily(NullFontFamilyCanonicalName);
                            Invariant.Assert(family != null);
                        }

                        TypefaceMetricsCache.Add(FamilyIdentifier, family);
                    }

                    _firstFontFamily = family;
                }

                return family;               
            }
        }
        

        #region Resolving family name to font family

        /// <summary>
        /// Scan the friendly name string finding the first valid font family
        /// </summary>
        internal static IFontFamily FindFontFamilyFromFriendlyNameList(string friendlyNameList)
        {
            IFontFamily firstFontFamily = null;

            // Split limits the number of tokens in a family name.
            FontFamilyIdentifier identifier = new FontFamilyIdentifier(friendlyNameList, null);
            for (int i = 0, c = identifier.Count; firstFontFamily == null && i < c; i++)
            {
                firstFontFamily = LookupFontFamily(identifier[i]);
            }

            if (firstFontFamily == null)
            {
                // cannot find first font family, assume null font for first font family
                firstFontFamily = LookupFontFamily(NullFontFamilyCanonicalName);

                // null font family should always exist
                Invariant.Assert(firstFontFamily != null);
}

            return firstFontFamily;
        }


        /// <summary>
        /// Create font family from canonical family and ensure at least a 
        /// fallback family is created if the specified name cannot be resolved.
        /// </summary>
        internal static IFontFamily SafeLookupFontFamily(
            CanonicalFontFamilyReference canonicalName,
            out bool                     nullFont
            )
        {
            nullFont = false;

            IFontFamily fontFamily = LookupFontFamily(canonicalName);

            if(fontFamily == null)
            {
                nullFont = true;
                fontFamily = LookupFontFamily(NullFontFamilyCanonicalName);
                Invariant.Assert(fontFamily != null, "Unable to create null font family");
            }
            
            return fontFamily;
        }


        /// <summary>
        /// Look up font family from canonical name
        /// </summary>
        /// <param name="canonicalName">font family canonical name</param>
        internal static IFontFamily LookupFontFamily(CanonicalFontFamilyReference canonicalName)
        {
            FontStyle style     = FontStyles.Normal;
            FontWeight weight   = FontWeights.Normal;
            FontStretch stretch = FontStretches.Normal;

            return LookupFontFamilyAndFace(canonicalName, ref style, ref weight, ref stretch);
        }

        #endregion


        #region Resolving face name into font family and implied face

        /// <summary>
        /// Precreates family collection for Windows Fonts folder, so that we don't have to repeat lookup
        /// every time for it.
        /// </summary>
        /// <returns></returns>
        private static FamilyCollection PreCreateDefaultFamilyCollection()
        {
            FamilyCollection familyCollection = FamilyCollection.FromWindowsFonts(Util.WindowsFontsUriObject);
            return familyCollection;
        }


        /// <summary>
        /// Find the first valid IFontFamily, if any, for this FontFamily and sets the style, weight,
        /// and stretch to valies implied by the font family (e.g., "Arial Bold" implies FontWeight.Bold).
        /// </summary>
        internal IFontFamily FindFirstFontFamilyAndFace(
            ref FontStyle   style,
            ref FontWeight  weight,
            ref FontStretch stretch
            )
        {
            if (_familyIdentifier.Source == null)
            {
                Invariant.Assert(_firstFontFamily != null, "Unnamed FontFamily should have a non-null first font family");
                return _firstFontFamily;
            }

            IFontFamily firstFontFamily = null;
            
            _familyIdentifier.Canonicalize();

            for (int i = 0, c = _familyIdentifier.Count; firstFontFamily == null && i < c; ++i)
            {
                firstFontFamily = LookupFontFamilyAndFace(
                    _familyIdentifier[i],
                    ref style,
                    ref weight,
                    ref stretch);
            }

            return firstFontFamily;
        }


        /// <summary>
        /// Lookup font family from canonical name.
        /// </summary>
        /// <param name="canonicalFamilyReference">font face canonical name</param>
        /// <param name="style">FontStyle implied by the font family.</param>
        /// <param name="weight">FontWeight implied by the font family.</param>
        /// <param name="stretch">FontStretch implied by the font family.</param>
        /// <returns>The font family object.</returns>
        internal static IFontFamily LookupFontFamilyAndFace(
            CanonicalFontFamilyReference canonicalFamilyReference,
            ref FontStyle                style,
            ref FontWeight               weight,
            ref FontStretch              stretch
            )
        {
            if (canonicalFamilyReference == null || object.ReferenceEquals(canonicalFamilyReference, CanonicalFontFamilyReference.Unresolved))
            {
                // no canonical name, e.g., because the friendly name was an empty string
                // or could not be canonicalized
                return null;
            }

            try
            {
                FamilyCollection familyCollection;

                if (canonicalFamilyReference.LocationUri == null && canonicalFamilyReference.EscapedFileName == null)
                {
                    // No explicit location; use the default family collection.
                    familyCollection = _defaultFamilyCollection;
                }
                else if (canonicalFamilyReference.LocationUri != null)
                {
                    // Look in the location specified by the font family reference.
                    familyCollection = FamilyCollection.FromUri(canonicalFamilyReference.LocationUri);
                }
                else // canonicalFamilyReference.EscapedFileName != null
                {
                    // Look in the specified file in the Windows Fonts folder
                    // Note: CanonicalFamilyReference.EscapedFileName is safe to combine with Util.WindowsFontsUriObject because CanonicalFamilyReference guarantees that it will be a simple filename
                    // without relative path or directory components.
                    Uri locationUri = new Uri(Util.WindowsFontsUriObject, canonicalFamilyReference.EscapedFileName);
                    familyCollection = FamilyCollection.FromWindowsFonts(locationUri);
                }

                IFontFamily fontFamily = familyCollection.LookupFamily(
                    canonicalFamilyReference.FamilyName,
                    ref style,
                    ref weight,
                    ref stretch
                );
                return fontFamily;
            }
            // The method returns null in case of malformed/non-existent fonts and we fall back to the next font.
            // Therefore, we can disable PreSharp warning about empty catch bodies.
#pragma warning disable 6502
            catch (FileFormatException)
            {
                // malformed font file
            }
            catch (IOException)
            {
                // canonical name points to a place that doesn't exist or can't be read for some reason
            }
            catch (UnauthorizedAccessException)
            {
                // canonical name points to a place caller doesn't have permission to access
            }
            catch (ArgumentException)
            {
                // canonical name points to a valid Uri that doesn't point to a well formed
                // OS local path
            }
            catch (NotSupportedException)
            {
                // canonical name points to a Uri that specifies an unregistered scheme
            }
            catch (UriFormatException)
            {
                // canonical name points to a malformed Uri
            }
#pragma warning restore 6502
            // we want to fall back to the default fallback font instead of crashing
            return null;
        }

        #endregion
    }
}
