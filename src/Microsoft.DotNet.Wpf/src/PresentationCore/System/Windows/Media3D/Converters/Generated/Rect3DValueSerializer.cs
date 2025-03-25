// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
//
// This file was generated, please do not edit it directly.
//
// Please see MilCodeGen.html for more information.
//

using System.Windows.Media.Media3D;
using System.Windows.Markup;

using ConverterHelper = System.Windows.Markup.TypeConverterHelper;

namespace System.Windows.Media.Media3D.Converters
{
    /// <summary>
    /// Rect3DValueSerializer - ValueSerializer class for converting instances of strings to and from Rect3D instances
    /// This is used by the MarkupWriter class.
    /// </summary>
    public class Rect3DValueSerializer : ValueSerializer 
    {
        /// <summary>
        /// Returns true.
        /// </summary>
        public override bool CanConvertFromString(string value, IValueSerializerContext context)
        {
            return true;
        }

        /// <summary>
        /// Returns true if the given value can be converted into a string
        /// </summary>
        public override bool CanConvertToString(object value, IValueSerializerContext context)
        {
            // Validate the input type
            return value is Rect3D;
        }

        /// <summary>
        /// Converts a string into a Rect3D.
        /// </summary>
        public override object ConvertFromString(string value, IValueSerializerContext context)
        {
            return value is not null ? Rect3D.Parse(value) : base.ConvertFromString(value, context);
        }

        /// <summary>
        /// Converts the value into a string.
        /// </summary>
        public override string ConvertToString(object value, IValueSerializerContext context)
        {
            if (value is not Rect3D rect3D)
            {
                // Let base throw an exception.
                return base.ConvertToString(value, context);
            }

            return rect3D.ConvertToString(null, ConverterHelper.InvariantEnglishUS);
        }
    }
}
