// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//+-----------------------------------------------------------------------
//
//
//
//  Contents:  FontFamilyValueSerializer implementation
//
//

using System.Windows.Markup;

namespace System.Windows.Media
{
    /// <summary>
    /// Serializer for a FontFamily
    /// </summary>
    public class FontFamilyValueSerializer: ValueSerializer 
    {
        /// <summary>
        /// Returns true. FontFamilyValueSerializer can always convert from a string.
        /// </summary>
        public override bool CanConvertFromString(string value, IValueSerializerContext context)
        {
            return true;
        }

        /// <summary>
        /// Creates a FontFamily from a string
        /// </summary>
        public override object ConvertFromString(string value, IValueSerializerContext context)
        {
            if (string.IsNullOrEmpty(value))
            {
                throw GetConvertFromException(value);
            }
            return new FontFamily(value);
        }

        /// <summary>
        /// Returns true if the FontFamily is a named font family.
        /// </summary>
        public override bool CanConvertToString(object value, IValueSerializerContext context)
        {
            FontFamily fontFamily = value as FontFamily;

            return fontFamily != null && fontFamily.Source != null && fontFamily.Source.Length != 0;           
        }

        /// <summary>
        /// Converts a font family to a string.
        /// </summary>
        public override string ConvertToString(object value, IValueSerializerContext context)
        {
            FontFamily fontFamily = value as FontFamily;
            if (fontFamily == null || fontFamily.Source == null)
                throw GetConvertToException(value, typeof(string));
            return fontFamily.Source;
        }
    }
}
