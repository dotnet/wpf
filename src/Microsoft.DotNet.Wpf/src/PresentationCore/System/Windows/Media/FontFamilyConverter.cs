// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//+-----------------------------------------------------------------------
//
//
//
//  Contents:  FontFamilyConverter implementation
//
//  Spec:      Fonts.htm
//
//

using System;
using System.ComponentModel;
using System.ComponentModel.Design.Serialization;
using System.Diagnostics;
using System.Globalization;
using System.Windows.Navigation;
using System.Windows.Markup;

using SR=MS.Internal.PresentationCore.SR;
using SRID=MS.Internal.PresentationCore.SRID;

// Allow suppression of presharp warnings
#pragma warning disable 1634, 1691

namespace System.Windows.Media
{
    /// <summary>
    /// FontFamilyConverter - converter class for converting between the FontFamily
    /// and String types.
    /// </summary>
    public class FontFamilyConverter : TypeConverter
    {
        /// <summary>
        /// CanConvertFrom - Returns whether or not the given type can be converted to a
        /// FontFamily.
        /// </summary>
        public override bool CanConvertFrom(ITypeDescriptorContext td, Type t)
        {
            return t == typeof(string);
        }

        /// <summary>
        /// CanConvertTo - Returns whether or not this class can convert to the specified type.
        /// Conversion is possible only if the source and destination types are FontFamily and
        /// string, respectively, and the font family is not anonymous (i.e., the Source propery
        /// is not null).
        /// </summary>
        /// <param name="context">ITypeDescriptorContext</param>
        /// <param name="destinationType">Type to convert to</param>
        /// <returns>true if conversion is possible</returns>
        public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType)
        {
            if (destinationType == typeof(string))
            {
                if (context != null)
                {
                    // When serializing to XAML we want to write the FontFamily as an attribute if and
                    // only if it's a named font family.
                    FontFamily fontFamily = context.Instance as FontFamily;

                    // Suppress PRESharp warning that fontFamily can be null; apparently PRESharp
                    // doesn't understand short circuit evaluation of operator &&.
#pragma warning suppress 56506
                    return fontFamily != null && fontFamily.Source != null && fontFamily.Source.Length != 0;
                }
                else
                {
                    // Some clients call typeConverter.CanConvertTo(typeof(string)), in which case we
                    // don't have the FontFamily instance to convert. Most font families are named, and
                    // we can always give some kind of name, so return true.
                    return true;
                }
            }
            else if (destinationType == typeof(FontFamily))
            {
                return true;
            }
            return base.CanConvertTo(context, destinationType);
        }

        /// <summary>
        /// ConvertFrom - Converts the specified object to a FontFamily.
        /// </summary>
        public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo cultureInfo, object o)
        {
            if ((o != null) && (o.GetType() == typeof(string)))
            {
                string s = o as string;

                if (s == null || s.Length == 0)
                {
                    throw GetConvertFromException(s);
                }

                // Logic below is similar to TypeConverterHelper.GetUriFromUriContext,
                // except that we cannot treat font family string as a Uri,
                // and that we handle cases when context is null.

                Uri baseUri = null;

                if (context != null)
                {
                    IUriContext iuc = (IUriContext)context.GetService(typeof(IUriContext));
                    if (iuc != null)
                    {
                        if (iuc.BaseUri != null)
                        {
                            baseUri = iuc.BaseUri;

                            if (!baseUri.IsAbsoluteUri)
                            {
                                baseUri = new Uri(BaseUriHelper.BaseUri, baseUri);
                            }
                        }
                        else
                        {
                            // If we reach here, the base uri we got from IUriContext is "".
                            // Here we resolve it to application's base
                            baseUri = BaseUriHelper.BaseUri;
                        }
                    }
                }

                return new FontFamily(baseUri, s);
            }
            return base.ConvertFrom(context, cultureInfo, o); ;
        }

        /// <summary>
        /// ConvertTo - Converts the specified object to an instance of the specified type.
        /// </summary>
        public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
        {
            if (null == value)
            {
                throw new ArgumentNullException("value");
            }

            FontFamily fontFamily = value as FontFamily;
            if (fontFamily == null)
            {
                throw new ArgumentException(SR.Get(SRID.General_Expected_Type, "FontFamily"), "value");
            }

            if (null == destinationType)
            {
                throw new ArgumentNullException("destinationType");
            }

            if (destinationType == typeof(string))
            {
                if (fontFamily.Source != null)
                {
                    // Usual case: it's a named font family.
                    return fontFamily.Source;
                }
                else
                {
                    // If client calls typeConverter.CanConvertTo(typeof(string)) then we'll return
                    // true always, even though we don't have access to the FontFamily instance; so
                    // we need to be able to return some kind of family name even if Source==null.
                    string name = null;

                    CultureInfo parentCulture = null;
                    if (culture != null)
                    {
                        if (culture.Equals(CultureInfo.InvariantCulture))
                        {
                            culture = null;
                        }
                        else
                        {
                            parentCulture = culture.Parent;
                            if (parentCulture != null && 
                                (parentCulture.Equals(CultureInfo.InvariantCulture) || parentCulture == culture))
                            {
                                parentCulture = null;
                            }
                        }
                    }

                    // Try looking up the name in the FamilyNames dictionary.
                    LanguageSpecificStringDictionary names = fontFamily.FamilyNames;

                    if (culture != null && names.TryGetValue(XmlLanguage.GetLanguage(culture.IetfLanguageTag), out name))
                    {
                        // LanguageSpecificStringDictionary does not allow null string to be added.
                        Debug.Assert(name != null);
                    }
                    else if (parentCulture != null && names.TryGetValue(XmlLanguage.GetLanguage(parentCulture.IetfLanguageTag), out name))
                    {
                        // LanguageSpecificStringDictionary does not allow null string to be added.
                        Debug.Assert(name != null);
                    }
                    else if (names.TryGetValue(XmlLanguage.Empty, out name))
                    {
                        // LanguageSpecificStringDictionary does not allow null string to be added.
                        Debug.Assert(name != null);
                    }
                    else
                    {
                        // Try the first target font compatible with the culture.
                        foreach (FontFamilyMap familyMap in fontFamily.FamilyMaps)
                        {
                            if (FontFamilyMap.MatchCulture(familyMap.Language, culture))
                            {
                                name = familyMap.Target;
                                break;
                            }
                        }

                        // Use global ui as a last resort.
                        if (name == null)
                            name = FontFamily.GlobalUI;
                    }

                    return name;
                }
            }

            return base.ConvertTo(context, culture, value, destinationType);
        }
    }
}
