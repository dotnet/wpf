// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
//
// Description: FamilyCollection font cache element class is responsible for
// storing the mapping between a folder and font families in it.
//
//

using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security;
using System.Windows;
using System.Windows.Markup;    // for XmlLanguage
using System.Windows.Media;

using MS.Win32;
using MS.Utility;
using MS.Internal;
using MS.Internal.FontFace;
using MS.Internal.PresentationCore;
using MS.Internal.Shaping;

// Since we disable PreSharp warnings in this file, we first need to disable warnings about unknown message numbers and unknown pragmas.
#pragma warning disable 1634, 1691

namespace MS.Internal.FontCache
{
    /// <summary>
    /// FamilyCollection font cache element class is responsible for
    /// storing the mapping between a folder and font families in it
    /// </summary>
    [FriendAccessAllowed]
    internal class FamilyCollection
    {
        //------------------------------------------------------
        //
        //  Private Fields
        //
        //------------------------------------------------------

        #region Private Fields

        private Text.TextInterface.FontCollection _fontCollection;
        private Uri                               _folderUri;
        private List<CompositeFontFamily>         _userCompositeFonts;
        private static object                     _staticLock = new object();

        #endregion Private Fields

        internal static string SxSFontsResourcePrefix { get; } = $"/{Path.GetFileNameWithoutExtension(ExternDll.PresentationCore)};component/fonts/";

        private static List<CompositeFontFamily> GetCompositeFontList(FontSourceCollection fontSourceCollection)
        {
            List<CompositeFontFamily> compositeFonts = new List<CompositeFontFamily>();

            foreach (FontSource fontSource in fontSourceCollection)
            {
                if (fontSource.IsComposite)
                {
                    CompositeFontInfo fontInfo = CompositeFontParser.LoadXml(fontSource.GetStream());
                    CompositeFontFamily compositeFamily = new CompositeFontFamily(fontInfo);
                    compositeFonts.Add(compositeFamily);
                }
            }

            return compositeFonts;
        }

        private bool UseSystemFonts
        {
            get
            {
                return (_fontCollection == DWriteFactory.SystemFontCollection);
            }
        }

        private IList<CompositeFontFamily> UserCompositeFonts
        {
            get
            {
                if (_userCompositeFonts == null)
                {
                    _userCompositeFonts = GetCompositeFontList(new FontSourceCollection(_folderUri, false, true));
                }
                return _userCompositeFonts;
            }
        }

        private static class LegacyArabicFonts
        {
            private static bool              _usePrivateFontCollectionIsInitialized = false;
            private static object            _staticLock = new object();
            private static bool              _usePrivateFontCollectionForLegacyArabicFonts;
            private static readonly string[] _legacyArabicFonts;
            private static Text.TextInterface.FontCollection _legacyArabicFontCollection;


            static LegacyArabicFonts()
            {
                _legacyArabicFonts = new string[] { "Traditional Arabic",
                                                    "Andalus",
                                                    "Simplified Arabic",
                                                    "Simplified Arabic Fixed" };
            }

            internal static Text.TextInterface.FontCollection LegacyArabicFontCollection
            {
                get
                {
                    if (_legacyArabicFontCollection == null)
                    {
                        lock (_staticLock)
                        {
                            if (_legacyArabicFontCollection == null)
                            {
                                Uri criticalSxSFontsLocation = new Uri(FamilyCollection.SxSFontsResourcePrefix);
                                _legacyArabicFontCollection = DWriteFactory.GetFontCollectionFromFolder(criticalSxSFontsLocation);
                            }
                        }
                    }
                    return _legacyArabicFontCollection;
                }
            }

            /// <summary>
            /// Checks if a given family name is one of the legacy Arabic fonts.
            /// </summary>
            /// <param name="familyName">The family name without any face info.</param>
            internal static bool IsLegacyArabicFont(string familyName)
            {
                for (int i = 0; i < _legacyArabicFonts.Length; ++i)
                {
                    if (String.Compare(familyName, _legacyArabicFonts[i], StringComparison.OrdinalIgnoreCase) == 0)
                    {
                        return true;
                    }
                }
                return false;
            }

            /// <summary>
            /// We will use the private font collection to load some Arabic fonts
            /// only on OSes lower than Win7, since the fonts on these OSes are legacy fonts
            /// and the shaping engines that DWrite uses does not handle them properly.
            /// </summary>
            internal static bool UsePrivateFontCollectionForLegacyArabicFonts
            {
                get
                {
                    if (!_usePrivateFontCollectionIsInitialized)
                    {
                        lock (_staticLock)
                        {
                            if (!_usePrivateFontCollectionIsInitialized)
                            {
                                try
                                {
                                    OperatingSystem osInfo = Environment.OSVersion;
                                    // The version of Win7 is 6.1
                                    _usePrivateFontCollectionForLegacyArabicFonts = (osInfo.Version.Major < 6)
                                                                                 || (osInfo.Version.Major == 6
                                                                                     &&
                                                                                     osInfo.Version.Minor == 0);
                                }
                                //Environment.OSVersion was unable to obtain the system version.
                                //-or- 
                                //The obtained platform identifier is not a member of PlatformID.
                                catch (InvalidOperationException)
                                {
                                    // We do not want to bubble this exception up to the user since the user
                                    // has nothing to do about it.
                                    // Instead we will silently fallback to using the private fonts collection 
                                    // so that we guarantee that the text shows properly.
                                    _usePrivateFontCollectionForLegacyArabicFonts = true;
                                }

                                _usePrivateFontCollectionIsInitialized = true;
                            }
                        }
                    }
                    return _usePrivateFontCollectionForLegacyArabicFonts;
                }
            }
        }

        /// <summary>
        /// This class encapsulates the 4 system composite fonts.
        /// </summary>
        /// <remarks>
        /// This class has direct knowledge about the 4 composite fonts that ship with WPF.
        /// </remarks>
        private static class SystemCompositeFonts
        {
            /// The number of the system composite fonts that ship with WPF is 4.
            internal const int NumOfSystemCompositeFonts = 4;

            private static object                _systemCompositeFontsLock = new object();
            private static readonly string[]     _systemCompositeFontsNames;
            private static readonly string[]     _systemCompositeFontsFileNames;
            private static CompositeFontFamily[] _systemCompositeFonts;

            static SystemCompositeFonts()
            {
                _systemCompositeFontsNames     = new string[] { "Global User Interface", "Global Monospace", "Global Sans Serif", "Global Serif" };
                _systemCompositeFontsFileNames = new string[] { "GlobalUserInterface", "GlobalMonospace", "GlobalSansSerif", "GlobalSerif" };
                _systemCompositeFonts = new CompositeFontFamily[NumOfSystemCompositeFonts];
            }

            /// <summary>
            /// Returns the composite font to be used to fallback to a different font
            /// if one of the legacy Arabic fonts is specifed.
            /// </summary>
            internal static CompositeFontFamily GetFallbackFontForArabicLegacyFonts()
            {
                return GetCompositeFontFamilyAtIndex(1);
            }

            /// <summary>
            /// This method returns the composite font family (or null if not found) given its name
            /// </summary>
            internal static CompositeFontFamily FindFamily(string familyName)
            {
                int index = GetIndexOfFamily(familyName);
                if (index >= 0)
                {
                    return GetCompositeFontFamilyAtIndex(index);
                }
                return null;
            }

            /// <summary>
            /// This method returns the composite font with the given index after
            /// lazily allocating it if it has not been already allocated.
            /// </summary>
            internal static CompositeFontFamily GetCompositeFontFamilyAtIndex(int index)
            {
                if (_systemCompositeFonts[index] == null)
                {
                    lock (_systemCompositeFontsLock)
                    {
                        if (_systemCompositeFonts[index] == null)
                        {
                            FontSource fontSource = new FontSource(new Uri(Path.Combine(FamilyCollection.SxSFontsResourcePrefix, _systemCompositeFontsFileNames[index] + Util.CompositeFontExtension), UriKind.RelativeOrAbsolute),
                                                                   skipDemand:true,
                                                                   isComposite:true,
                                                                   isInternalCompositeFont:true);

                            CompositeFontInfo fontInfo = CompositeFontParser.LoadXml(fontSource.GetStream());
                            _systemCompositeFonts[index] = new CompositeFontFamily(fontInfo);
                        }
                    }
                }
                return _systemCompositeFonts[index];
            }

            /// <summary>
            /// This method returns the index of the system composite font in _systemCompositeFontsNames.
            /// </summary>
            private static int GetIndexOfFamily(string familyName)
            {
                for (int i = 0; i < _systemCompositeFontsNames.Length; ++i)
                {
                    if (String.Compare(_systemCompositeFontsNames[i], familyName, StringComparison.OrdinalIgnoreCase) == 0)
                    {
                        return i;
                    }
                }
                return -1;
            }
        }

        //------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------

        #region Constructors
        /// <summary>
        /// Creates a font family collection cache element from a canonical font family reference.
        /// </summary>
        /// <param name="folderUri">Absolute Uri of a folder</param>
        /// <param name="fontCollection">Collection of fonts loaded from the folderUri location</param>
        private FamilyCollection(Uri folderUri, MS.Internal.Text.TextInterface.FontCollection fontCollection)
        {
            _folderUri = folderUri;
            _fontCollection = fontCollection;
        }

        /// <summary>
        /// Creates a font family collection cache element from a canonical font family reference.
        /// </summary>
        /// <param name="folderUri">Absolute Uri of a folder</param>
        internal static FamilyCollection FromUri(Uri folderUri)
        {
            return new FamilyCollection(folderUri, DWriteFactory.GetFontCollectionFromFolder(folderUri));
        }

        /// <summary>
        /// Creates a font family collection cache element from a canonical font family reference.
        /// </summary>
        /// <param name="folderUri">Absolute Uri to the Windows Fonts folder or a file in the Windows Fonts folder.</param>
        internal static FamilyCollection FromWindowsFonts(Uri folderUri)
        {
            return new FamilyCollection(folderUri, DWriteFactory.SystemFontCollection);
        }

        #endregion Constructors


        //------------------------------------------------------
        //
        //  Internal Methods
        //
        //------------------------------------------------------

        #region Internal methods

        /// <summary>
        /// This method looks up a certain family in this collection given its name.
        /// If the name was for a specific font face then this method will return its
        /// style, weight and stretch information.
        /// </summary>
        /// <param name="familyName">The name of the family to look for.</param>
        /// <param name="fontStyle">The style if the font face in case family name contained style info.</param>
        /// <param name="fontWeight">The weight if the font face in case family name contained style info.</param>
        /// <param name="fontStretch">The stretch if the font face in case family name contained style info.</param>
        /// <returns>The font family if found.</returns>
        internal IFontFamily LookupFamily(
            string familyName,
            ref FontStyle fontStyle,
            ref FontWeight fontWeight,
            ref FontStretch fontStretch
            )
        {
            if (familyName == null || familyName.Length == 0)
                return null;

            familyName = familyName.Trim();

            // If we are referencing fonts from the system fonts, then it is cheap to lookup the 4 composite fonts
            // that ship with WPF. Also, it happens often that familyName is "Global User Interface".
            // So in this case we preceed looking into SystemComposite Fonts.
            if (UseSystemFonts)
            {
                CompositeFontFamily compositeFamily = SystemCompositeFonts.FindFamily(familyName);
                if (compositeFamily != null)
                {
                    return compositeFamily;
                }
            }

            Text.TextInterface.FontFamily fontFamilyDWrite = _fontCollection[familyName];

            // A font family was not found in DWrite's font collection.
            if (fontFamilyDWrite == null)
            {
                // Having user defined composite fonts is not very common. So we defer looking into them to looking DWrite 
                // (which is opposite to what we do for system fonts).
                if (!UseSystemFonts)
                {
                    // The family name was not found in DWrite's font collection. It may possibly be the name of a composite font
                    // since DWrite does not recognize composite fonts.
                    CompositeFontFamily compositeFamily = LookUpUserCompositeFamily(familyName);
                    if (compositeFamily != null)
                    {
                        return compositeFamily;
                    }
                }

                // The family name cannot be found. This may possibly be because the family name contains styling info.
                // For example, "Arial Bold"
                // We will strip off the styling info (one word at a time from the end) and try to find the family name.
                int indexOfSpace = -1;
                System.Text.StringBuilder potentialFaceName = new System.Text.StringBuilder();

                // Start removing off strings from the end hoping they are
                // style info so as to get down to the family name.
                do
                {
                    indexOfSpace = familyName.LastIndexOf(' ');
                    if (indexOfSpace < 0)
                    {
                        break;
                    }
                    else
                    {
                        // store the stripped off style names to look for the specific face later.
                        potentialFaceName.Insert(0, familyName.Substring(indexOfSpace));
                        familyName = familyName.Substring(0, indexOfSpace);
                    }

                    fontFamilyDWrite = _fontCollection[familyName];
} while (fontFamilyDWrite == null);


                if (fontFamilyDWrite == null)
                {
                    return null;
                }

                // If there was styling information.
                if (potentialFaceName.Length > 0)
                {
                    // The first character in the potentialFaceName will be a space so we need to strip it off.
                    Text.TextInterface.Font font = GetFontFromFamily(fontFamilyDWrite, potentialFaceName.ToString(1, potentialFaceName.Length - 1));

                    if (font != null)
                    {
                        fontStyle = new FontStyle((int)font.Style);
                        fontWeight = new FontWeight((int)font.Weight);
                        fontStretch = new FontStretch((int)font.Stretch);
                    }
                }
            }

            if (UseSystemFonts
                && LegacyArabicFonts.UsePrivateFontCollectionForLegacyArabicFonts
                // familyName will hold the family name without any face info.
                && LegacyArabicFonts.IsLegacyArabicFont(familyName))
            {
                fontFamilyDWrite = LegacyArabicFonts.LegacyArabicFontCollection[familyName];
                if (fontFamilyDWrite == null)
                {
                    return SystemCompositeFonts.GetFallbackFontForArabicLegacyFonts();
                }
            }

            return new PhysicalFontFamily(fontFamilyDWrite);
        }

        private CompositeFontFamily LookUpUserCompositeFamily(string familyName)
        {
            if (UserCompositeFonts != null)
            {
                foreach (CompositeFontFamily compositeFamily in UserCompositeFonts)
                {
                    foreach (KeyValuePair<XmlLanguage, string> localizedFamilyName in compositeFamily.FamilyNames)
                    {
                        if (String.Compare(localizedFamilyName.Value, familyName, StringComparison.OrdinalIgnoreCase) == 0)
                        {
                            return compositeFamily;
                        }
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// Given a DWrite font family, look into it for the given face.
        /// </summary>
        /// <param name="fontFamily">The font family to look in.</param>
        /// <param name="faceName">The face to look for.</param>
        /// <returns>The font face if found and null if nothing was found.</returns>
        private static Text.TextInterface.Font GetFontFromFamily(Text.TextInterface.FontFamily fontFamily, string faceName)
        {
            faceName = faceName.ToUpper(CultureInfo.InvariantCulture);

            // The search that DWrite supports is a linear search.
            // Look at every font face.
            foreach (Text.TextInterface.Font font in fontFamily)
            {
                // and at every locale name this font face has.
                foreach (KeyValuePair<CultureInfo, string> name in font.FaceNames)
                {
                    string currentFontName = name.Value.ToUpper(CultureInfo.InvariantCulture);
                    if (currentFontName == faceName)
                    {
                        return font;
                    }
                }
            }

            // This dictionary is used to store the faces (indexed by their names).
            // This dictionary will be used in case the exact string "faceName" was not found,
            // thus we will start again removing words (separated by ' ') from its end and looking
            // for the resulting faceName in that dictionary. So this dictionary is 
            // used to speed the search.
            Dictionary<string, Text.TextInterface.Font> faces = new Dictionary<string, Text.TextInterface.Font>();

            //We could have merged this loop with the one above. However this will degrade the performance 
            //of the scenario where the user entered  a correct face name (which is the common scenario).
            //Thus we adopt a pay for play approach, meaning that only whenever the face name does not
            //exactly correspond to an actual face name we will incure this overhead.
            foreach (Text.TextInterface.Font font in fontFamily)
            {
                foreach (KeyValuePair<CultureInfo, string> name in font.FaceNames)
                {
                    string currentFontName = name.Value.ToUpper(CultureInfo.InvariantCulture);
                    if (!faces.ContainsKey(currentFontName))
                    {
                        faces.Add(currentFontName, font);
                    }
                }
            }

            // An exact match was not found and so we will start looking for the best match.
            Text.TextInterface.Font matchingFont = null;
            int indexOfSpace = faceName.LastIndexOf(' ');

            while (indexOfSpace > 0)
            {
                faceName = faceName.Substring(0, indexOfSpace);
                if (faces.TryGetValue(faceName, out matchingFont))
                {
                    return matchingFont;
                }

                indexOfSpace = faceName.LastIndexOf(' ');
            }

            // No match was found.
            return null;
        }

        private struct FamilyEnumerator : IEnumerator<Text.TextInterface.FontFamily>, IEnumerable<Text.TextInterface.FontFamily>
        {
            private uint _familyCount;
            private Text.TextInterface.FontCollection _fontCollection;
            private bool _firstEnumeration;
            private uint _currentFamily;

            internal FamilyEnumerator(Text.TextInterface.FontCollection fontCollection)
            {
                _fontCollection = fontCollection;
                _currentFamily = 0;
                _firstEnumeration = true;
                _familyCount = fontCollection.FamilyCount;
}

            #region IEnumerator<Text.TextInterface.FontFamily> Members

            public bool MoveNext()
            {
                if (_firstEnumeration)
                {
                    _firstEnumeration = false;
                }
                else
                {
                    ++_currentFamily;
                }
                if (_currentFamily >= _familyCount)
                {
                    // prevent cycling
                    _currentFamily = _familyCount;
                    return false;
                }
                return true;
            }

            Text.TextInterface.FontFamily IEnumerator<Text.TextInterface.FontFamily>.Current
            {
                get
                {
                    if (_currentFamily < 0 || _currentFamily >= _familyCount)
                    {
                        throw new InvalidOperationException();
                    }

                    return _fontCollection[_currentFamily];
                }
            }

            #endregion

            #region IEnumerator Members

            object IEnumerator.Current
            {
                get
                {
                    return ((IEnumerator<Text.TextInterface.FontFamily>)this).Current;
                }
            }

            public void Reset()
            {
                _currentFamily = 0;
                _firstEnumeration = true;
            }

            #endregion


            #region IDisposable Members

            public void Dispose() { }

            #endregion

            #region IEnumerable<Text.TextInterface.FontFamily> Members

            IEnumerator<Text.TextInterface.FontFamily> IEnumerable<Text.TextInterface.FontFamily>.GetEnumerator()
            {
                return this as IEnumerator<Text.TextInterface.FontFamily>;
            }

            #endregion

            #region IEnumerable Members

            IEnumerator IEnumerable.GetEnumerator()
            {
                return ((IEnumerable<Text.TextInterface.FontFamily>)this).GetEnumerator();
            }

            #endregion
        }

        private IEnumerable<Text.TextInterface.FontFamily> GetPhysicalFontFamilies()
        {
            return new FamilyEnumerator(this._fontCollection);
        }

        internal FontFamily[] GetFontFamilies(Uri fontFamilyBaseUri, string fontFamilyLocationReference)
        {
            FontFamily[] fontFamilyList = new FontFamily[FamilyCount];
            int i = 0;
            foreach (MS.Internal.Text.TextInterface.FontFamily family in GetPhysicalFontFamilies())
            {
                string fontFamilyReference = Util.ConvertFamilyNameAndLocationToFontFamilyReference(
                    family.OrdinalName,
                    fontFamilyLocationReference
                    );

                string friendlyName = Util.ConvertFontFamilyReferenceToFriendlyName(fontFamilyReference);

                fontFamilyList[i++] = new FontFamily(fontFamilyBaseUri, friendlyName);
            }

            FontFamily fontFamily;
            if (UseSystemFonts)
            {
                for (int j = 0; j < SystemCompositeFonts.NumOfSystemCompositeFonts; ++j)
                {
                    fontFamily = CreateFontFamily(SystemCompositeFonts.GetCompositeFontFamilyAtIndex(j), fontFamilyBaseUri, fontFamilyLocationReference);
                    if (fontFamily != null)
                    {
                        fontFamilyList[i++] = fontFamily;
                    }
                }
            }
            else
            {
                foreach (CompositeFontFamily compositeFontFamily in UserCompositeFonts)
                {
                    fontFamily = CreateFontFamily(compositeFontFamily, fontFamilyBaseUri, fontFamilyLocationReference);
                    if (fontFamily != null)
                    {
                        fontFamilyList[i++] = fontFamily;
                    }
                }
            }

            Debug.Assert(i == FamilyCount);

            return fontFamilyList;
        }

        private FontFamily CreateFontFamily(CompositeFontFamily compositeFontFamily, Uri fontFamilyBaseUri, string fontFamilyLocationReference)
        {
            IFontFamily fontFamily = (IFontFamily)compositeFontFamily;
            IEnumerator<string> familyNames = fontFamily.Names.Values.GetEnumerator();
            if (familyNames.MoveNext())
            {
                string ordinalName = familyNames.Current;
                string fontFamilyReference = Util.ConvertFamilyNameAndLocationToFontFamilyReference(
                ordinalName,
                fontFamilyLocationReference
                );

                string friendlyName = Util.ConvertFontFamilyReferenceToFriendlyName(fontFamilyReference);

                return new FontFamily(fontFamilyBaseUri, friendlyName);
            }
            return null;
        }

        internal uint FamilyCount
        {
            get
            {
                return _fontCollection.FamilyCount + (UseSystemFonts ? SystemCompositeFonts.NumOfSystemCompositeFonts : checked((uint)UserCompositeFonts.Count));
            }
        }

        #endregion Internal methods
    }
}
