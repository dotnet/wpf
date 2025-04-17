// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

//
//
// This file was generated, please do not edit it directly.
//
// Please see MilCodeGen.html for more information.
//

using System.Windows.Media;
using System.Windows.Markup;

using ConverterHelper = System.Windows.Markup.TypeConverterHelper;

namespace System.Windows.Media.Converters
{
    /// <summary>
    /// CacheModeValueSerializer - ValueSerializer class for converting instances of strings to and from CacheMode instances
    /// This is used by the MarkupWriter class.
    /// </summary>
    public class CacheModeValueSerializer : ValueSerializer 
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
            // When invoked by the serialization engine we can convert to string only for some instances
            return value is CacheMode cacheMode && cacheMode.CanSerializeToString();
        }

        /// <summary>
        /// Converts a string into a CacheMode.
        /// </summary>
        public override object ConvertFromString(string value, IValueSerializerContext context)
        {
            return value is not null ? CacheMode.Parse(value) : base.ConvertFromString(value, context);
        }

        /// <summary>
        /// Converts the value into a string.
        /// </summary>
        public override string ConvertToString(object value, IValueSerializerContext context)
        {
            // When invoked by the serialization engine we can convert to string only for some instances
            if (value is not CacheMode cacheMode || !cacheMode.CanSerializeToString())
            {
                // Let base throw an exception.
                return base.ConvertToString(value, context);
            }

            return cacheMode.ConvertToString(null, ConverterHelper.InvariantEnglishUS);
        }
    }
}
