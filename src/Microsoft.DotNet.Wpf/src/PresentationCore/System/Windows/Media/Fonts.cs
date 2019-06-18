// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// 
// Description: The Fonts class provides font enumeration APIs.
//              See spec at http://avalon/text/DesignDocsAndSpecs/Font%20Enumeration%20API.htm
// 
//
//

using System;
using System.Text;
using System.IO;
using System.Globalization;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows;

using MS.Internal;
using MS.Internal.FontCache;
using MS.Internal.FontFace;
using MS.Internal.PresentationCore;
using MS.Internal.Shaping;
using System.Security;

namespace System.Windows.Media
{
    /// <summary>
    /// The FontEmbeddingManager class provides font enumeration APIs.
    /// </summary>
    public static class Fonts
    {
        //------------------------------------------------------
        //
        //  Public Methods
        //
        //------------------------------------------------------

        #region Public Methods

        /// <summary>
        /// Enumerates font families from an arbitrary location.
        /// </summary>
        /// <param name="location">An absolute URI of a folder containing fonts or a font file.</param>
        /// <returns>Collection of FontFamily objects from the specified folder or file.</returns>
        /// <remarks>
        /// The specified location must be an absolute file URI or path, and the caller must have
        /// FileIOPermission(FileIOPermissionAccess.Read) for the location. Each resulting FontFamily
        /// object includes the specified location in its friendly name and has no base URI.
        /// </remarks>
        public static ICollection<FontFamily> GetFontFamilies(string location)
        {
            if (location == null)
                throw new ArgumentNullException("location");

            return GetFontFamilies(null, location);
        }

        /// <summary>
        /// Enumerates font families in the same folder as the specified base URI.
        /// </summary>
        /// <param name="baseUri">An absolute URI of a folder containing fonts or a resource in that folder.</param>
        /// <returns>Collection of FontFamily objects from the specified folder.</returns>
        /// <remarks>
        /// The caller must have FileIOPermission(FileIOPermissionAccess.Read) for the folder
        /// specified by baseUri. Each resulting FontFamily object has the specified Uri as its
        /// BaseUri property has a friendly name of the form "./#Family Name".
        /// </remarks>
        public static ICollection<FontFamily> GetFontFamilies(Uri baseUri)
        {
            if (baseUri == null)
                throw new ArgumentNullException("baseUri");

            return GetFontFamilies(baseUri, null);
        }


        /// <summary>
        /// Enumerates font families in the font location specified by a base URI and/or a location string.
        /// </summary>
        /// <param name="baseUri">Base URI used to determine the font location if the location parameter is not specified 
        /// or is relative, and the value of the BaseUri property of each FontFamily in the resulting collection. This
        /// parameter can be null if the location parameter specifies an absolute location.</param>
        /// <param name="location">Optional relative or absolute URI reference. The location is used (with baseUri) to 
        /// determine the font folder and is exposed as part of the Source property of each FontFamily in the resulting
        /// collection. If location is null or empty then "./" is implied, meaning same folder as the base URI.</param>
        /// <returns>Collection of FontFamily objects from the specified font location.</returns>
        /// <remarks>
        /// The caller must have FileIOPermission(FileIOPermissionAccess.Read) for the specified font folder.
        /// Each resulting FontFamily object has the specified base Uri as its BaseUri property and includes the
        /// specified location as part of the friendly name specified by the Source property.
        /// </remarks>
        public static ICollection<FontFamily> GetFontFamilies(Uri baseUri, string location)
        {
            // Both Uri parameters are optional but neither can be relative.
            if (baseUri != null && !baseUri.IsAbsoluteUri)
                throw new ArgumentException(SR.Get(SRID.UriNotAbsolute), "baseUri");

            // Determine the font location from the base URI and location string.
            Uri fontLocation;
            if (!string.IsNullOrEmpty(location) && Uri.TryCreate(location, UriKind.Absolute, out fontLocation))
            {
                // absolute location; make sure we support absolute font family references for this scheme
                if (!Util.IsSupportedSchemeForAbsoluteFontFamilyUri(fontLocation))
                    throw new ArgumentException(SR.Get(SRID.InvalidAbsoluteUriInFontFamilyName), "location");

                // make sure the absolute location is a valid URI reference rather than a Win32 path as
                // we don't support the latter in a font family reference
                location = fontLocation.GetComponents(UriComponents.AbsoluteUri, UriFormat.SafeUnescaped);
            }
            else
            {
                // relative location; we need a base URI
                if (baseUri == null)
                    throw new ArgumentNullException("baseUri", SR.Get(SRID.NullBaseUriParam, "baseUri", "location"));

                // the location part must include a path component, otherwise we'll look in windows fonts and ignore the base URI
                if (string.IsNullOrEmpty(location))
                    location = "./";
                else if (Util.IsReferenceToWindowsFonts(location))
                    location = "./" + location;

                fontLocation = new Uri(baseUri, location);
            }

            // Create the font families.
            return CreateFamilyCollection(
                fontLocation,   // fontLocation
                baseUri,        // fontFamilyBaseUri
                location        // fontFamilyLocationReference
                );
        }


        /// <summary>
        /// Enumerates typefaces from an arbitrary location.
        /// </summary>
        /// <param name="location">An absolute URI of a folder containing fonts or a font file.</param>
        /// <returns>Collection of Typeface objects from the specified folder or file.</returns>
        /// <remarks>
        /// The specified location must be an absolute file URI or path, and the caller must have
        /// FileIOPermission(FileIOPermissionAccess.Read) for the location. The FontFamily of each
        /// resulting Typeface object includes the specified location in its friendly name and has
        /// no base URI.
        /// </remarks>
        public static ICollection<Typeface> GetTypefaces(string location)
        {
            if (location == null)
                throw new ArgumentNullException("location");

            return new TypefaceCollection(GetFontFamilies(null, location));
        }

        /// <summary>
        /// Enumerates typefaces in the same folder as the specified base URI.
        /// </summary>
        /// <param name="baseUri">An absolute URI of a folder containing fonts or a resource in that folder.</param>
        /// <returns>Collection of Typeface objects from the specified folder.</returns>
        /// <remarks>
        /// The caller must have FileIOPermission(FileIOPermissionAccess.Read) for the folder
        /// specified by baseUri. The FontFamily of each resulting Typeface object has the specified 
        /// Uri as its BaseUri property has a friendly name of the form "./#Family Name".
        /// </remarks>
        public static ICollection<Typeface> GetTypefaces(Uri baseUri)
        {
            if (baseUri == null)
                throw new ArgumentNullException("baseUri");
            
            return new TypefaceCollection(GetFontFamilies(baseUri, null));
        }

        /// <summary>
        /// Enumerates typefaces in the font location specified by a base URI and/or a location string.
        /// </summary>
        /// <param name="baseUri">Base URI used to determine the font location if the location parameter is not specified 
        /// or is relative, and the value of the BaseUri property of each FontFamily in the resulting collection. This
        /// parameter can be null if the location parameter specifies an absolute location.</param>
        /// <param name="location">Optional relative or absolute URI reference. The location is used (with baseUri) to 
        /// determine the font folder and is exposed as part of the Source property of each FontFamily in the resulting
        /// collection. If location is null or empty then "./" is implied, meaning same folder as the base URI.</param>
        /// <returns>Collection of Typeface objects from the specified font location.</returns>
        /// <remarks>
        /// The caller must have FileIOPermission(FileIOPermissionAccess.Read) for the specified font folder.
        /// Each resulting FontFamily object has the specified base Uri as its BaseUri property and includes the
        /// specified location as part of the friendly name specified by the Source property.
        /// </remarks>
        public static ICollection<Typeface> GetTypefaces(Uri baseUri, string location)
        {
            return new TypefaceCollection(GetFontFamilies(baseUri, location));
        }

        #endregion Public Methods

        //------------------------------------------------------
        //
        //  Public Properties
        //
        //------------------------------------------------------

        #region Public Properties

        /// <summary>
        /// Font families from the default system font location.
        /// </summary>
        /// <value>Collection of FontFamily objects from the default system font location.</value>
        public static ICollection<FontFamily> SystemFontFamilies
        {
            get
            {
                return _defaultFontCollection;
            }
        }

        /// <summary>
        /// Type faces from the default system font location.
        /// </summary>
        /// <value>Collection of Typeface objects from the default system font location.</value>
        public static ICollection<Typeface> SystemTypefaces
        {
            get
            {
                return new TypefaceCollection(_defaultFontCollection);
            }
        }


        #endregion Public Properties

        //------------------------------------------------------
        //
        //  Private Methods
        //
        //------------------------------------------------------

        #region Private Methods


        /// <summary>
        /// This method enumerates the font families in the specified font location and returns the resulting 
        /// collection of FontFamily objects.
        /// </summary>
        /// <param name="fontLocation">Absolute URI of file or folder containing font data</param>
        /// <param name="fontFamilyBaseUri">Optional base URI, exposed as the BaseUri property of each FontFamily.</param>
        /// <param name="fontFamilyLocationReference">Optional location reference, exposed as part of the Source
        /// property of each FontFamily.</param>
        private static ICollection<FontFamily> CreateFamilyCollection(
            Uri     fontLocation,
            Uri     fontFamilyBaseUri,
            string  fontFamilyLocationReference
            )
        {
            // Use reference comparison to determine the critical isWindowsFonts value. We want this
            // to be true ONLY if we're called internally to enumerate the default family collection.
            // See the SecurityNote for the FamilyCollection constructor.
            FamilyCollection familyCollection = 
                object.ReferenceEquals(fontLocation, Util.WindowsFontsUriObject) ?
                    FamilyCollection.FromWindowsFonts(fontLocation) : 
                    FamilyCollection.FromUri(fontLocation);

            FontFamily[] fontFamilyList = familyCollection.GetFontFamilies(fontFamilyBaseUri, fontFamilyLocationReference);
            
            return Array.AsReadOnly<FontFamily>(fontFamilyList);
        }


        /// <summary>
        /// Creates a collection of font families in the Windows Fonts folder.
        /// </summary>
        /// <remarks>
        /// This method is used to initialized the static _defaultFontCollection field. By having this
        /// safe wrapper for CreateFamilyCollection we avoid having to create a static initializer and
        /// declare it critical.
        /// </remarks>
        private static ICollection<FontFamily> CreateDefaultFamilyCollection()
        {
            return CreateFamilyCollection(
                Util.WindowsFontsUriObject, // fontLocation
                null,                       // fontFamilyBaseUri,
                null                        // fontFamilyLocationReference
                );
        }

        #endregion Private Methods

        //------------------------------------------------------
        //
        //  Private Classes
        //
        //------------------------------------------------------

        #region Private Classes

        private struct TypefaceCollection : ICollection<Typeface>
        {
            private IEnumerable<FontFamily> _families;

            public TypefaceCollection(IEnumerable<FontFamily> families)
            {
                _families = families;
            }

            #region ICollection<Typeface> Members

            public void Add(Typeface item)
            {
                throw new NotSupportedException();
            }

            public void Clear()
            {
                throw new NotSupportedException();
            }

            public bool Contains(Typeface item)
            {
                foreach (Typeface t in this)
                {
                    if (t.Equals(item))
                        return true;
                }
                return false;
            }

            public void CopyTo(Typeface[] array, int arrayIndex)
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

                foreach (Typeface t in this)
                {
                    array[arrayIndex++] = t;
                }
            }

            public int Count
            {
                get
                {
                    int count = 0;
                    foreach (Typeface t in this)
                    {
                        ++count;
                    }
                    return count;
                }
            }

            public bool IsReadOnly
            {
                get
                {
                    return true;
                }
            }

            public bool Remove(Typeface item)
            {
                throw new NotSupportedException();
            }

            #endregion

            #region IEnumerable<Typeface> Members

            public IEnumerator<Typeface> GetEnumerator()
            {
                foreach (FontFamily family in _families)
                {
                    foreach (Typeface typeface in family.GetTypefaces())
                    {
                        yield return typeface;
                    }
                }
            }

            #endregion

            #region IEnumerable Members

            System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
            {
                return ((IEnumerable<Typeface>)this).GetEnumerator();
            }

            #endregion
        }

        #endregion Private Classes

        //------------------------------------------------------
        //
        //  Private Fields
        //
        //------------------------------------------------------

        #region Private Fields

        private static readonly ICollection<FontFamily> _defaultFontCollection = CreateDefaultFamilyCollection();

        #endregion Private Fields
    }
}
