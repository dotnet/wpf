// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

//
//
// This file was generated, please do not edit it directly.
//
// Please see MilCodeGen.html for more information.
//

using System.Windows;
using System.Windows.Markup;

using ConverterHelper = System.Windows.Markup.TypeConverterHelper;

namespace System.Windows.Converters
{
    /// <summary>
    /// VectorValueSerializer - ValueSerializer class for converting instances of strings to and from Vector instances
    /// This is used by the MarkupWriter class.
    /// </summary>
    public class VectorValueSerializer : ValueSerializer 
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
            return value is Vector;
        }

        /// <summary>
        /// Converts a string into a Vector.
        /// </summary>
        public override object ConvertFromString(string value, IValueSerializerContext context)
        {
            return value is not null ? Vector.Parse(value) : base.ConvertFromString(value, context);
        }

        /// <summary>
        /// Converts the value into a string.
        /// </summary>
        public override string ConvertToString(object value, IValueSerializerContext context)
        {
            if (value is not Vector vector)
            {
                // Let base throw an exception.
                return base.ConvertToString(value, context);
            }

            return vector.ConvertToString(null, ConverterHelper.InvariantEnglishUS);
        }
    }
}
