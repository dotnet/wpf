// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
//
//  Description: The CultureMapper class implements static methods for mapping
//               CultureInfo objects from WPF clients to CultureInfo objects
//               that can be used internally by text formatting code.
//
//

using System;
using System.Globalization;
using System.Diagnostics;
using MS.Internal.PresentationCore;
using System.Windows.Markup;

namespace MS.Internal.TextFormatting
{
    /// <summary>
    /// Implements static methods for mapping CultureInfo objects.
    /// </summary>
    internal static class CultureMapper
    {
        /// <summary>
        /// Returns a specific culture given an arbitrary CultureInfo, which may be null, the invariant
        /// culture, or a neutral culture.
        /// </summary>
        public static CultureInfo GetSpecificCulture(CultureInfo runCulture)
        {
            // Assume default culture unless we can do better.
            CultureInfo specificCulture = TypeConverterHelper.InvariantEnglishUS;

            if (runCulture != null)
            {
                // Assign _cachedCultureMap to a local variable for thread safety. The reference assignment
                // is atomic and the CachedCultureMap class is immutable.
                CachedCultureMap cachedCultureMap = _cachedCultureMap;
                if (cachedCultureMap != null && object.ReferenceEquals(cachedCultureMap.OriginalCulture, runCulture))
                    return cachedCultureMap.SpecificCulture;

                // Unfortunately we cannot use reference comparison here because, for example, new CultureInfo("") 
                // creates an invariant culture which (being a new object) is obviously not the same instance as 
                // CultureInfo.InvariantCulture.
                if (runCulture != CultureInfo.InvariantCulture)
                {
                    if (!runCulture.IsNeutralCulture)
                    {
                        // It's already a specific culture (neither neutral nor InvariantCulture)
                        specificCulture = runCulture;
                    }
                    else
                    {
                        // Get the culture name. Note that the string expected by CreateSpecificCulture corresponds
                        // to the Name property, not IetfLanguageTag, so that's what we use.
                        string cultureName = runCulture.Name;
                        if (!string.IsNullOrEmpty(cultureName))
                        {
                            try
                            {
                                CultureInfo culture = CultureInfo.CreateSpecificCulture(cultureName);
                                specificCulture = SafeSecurityHelper.GetCultureInfoByIetfLanguageTag(culture.IetfLanguageTag);
                            }
                            catch (ArgumentException)
                            {
                                // This exception occurs if the culture name is invalid or has no corresponding specific
                                // culture. we can safely ignore the exception and fall back to TypeConverterHelper.InvariantEnglishUS.
                                specificCulture = TypeConverterHelper.InvariantEnglishUS;
                            }
                        }
                    }
                }

                // Save the mapping so the next call will be fast if we're given the same runCulture.
                // Again, the reference assignment is atomic so this is thread safe.
                _cachedCultureMap = new CachedCultureMap(runCulture, specificCulture);
            }

            return specificCulture;
        }

        private class CachedCultureMap
        {
            public CachedCultureMap(CultureInfo originalCulture, CultureInfo specificCulture)
            {
                _originalCulture = originalCulture;
                _specificCulture = specificCulture;
            }

            /// <summary>
            /// Original CultureInfo object from text formatting client; could be the invariant culture,
            /// a neutral culture, or a specific culture.
            /// </summary>
            public CultureInfo OriginalCulture
            {
                get { return _originalCulture; }
            }

            /// <summary>
            /// CultureInfo object to use for text formatting. This is guaranteed to be a specific (i.e.,
            /// neither neutral nor invariant) culture. It may be the same object as OriginalCulture if
            /// the latter is already a specific culture.
            /// </summary>
            public CultureInfo SpecificCulture
            {
                get { return _specificCulture; }
            }

            private CultureInfo _originalCulture;
            private CultureInfo _specificCulture;
        }

        private static CachedCultureMap _cachedCultureMap = null;
    }
}

