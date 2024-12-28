// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
//
// This file was generated, please do not edit it directly.
//
// Please see MilCodeGen.html for more information.
//

using System.Windows.Markup;

#pragma warning disable 1634, 1691  // suppressing PreSharp warnings

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
            if (!(value is CacheMode))
            {
                return false;
            }

            CacheMode instance  = (CacheMode) value;

            #pragma warning suppress 6506 // instance is obviously not null
            return instance.CanSerializeToString();
}

        /// <summary>
        /// Converts a string into a CacheMode.
        /// </summary>
        public override object ConvertFromString(string value, IValueSerializerContext context)
        {
            if (value != null)
            {
                return CacheMode.Parse(value );
            }
            else
            {
                return base.ConvertFromString( value, context );
            }
}

        /// <summary>
        /// Converts the value into a string.
        /// </summary>
        public override string ConvertToString(object value, IValueSerializerContext context)
        {
            if (value is CacheMode)
            {
                CacheMode instance = (CacheMode) value;
                // When invoked by the serialization engine we can convert to string only for some instances
                #pragma warning suppress 6506 // instance is obviously not null
                if (!instance.CanSerializeToString())
                {
                    // Let base throw an exception.
                    return base.ConvertToString(value, context);
                }


                #pragma warning suppress 6506 // instance is obviously not null
                return instance.ConvertToString(null, System.Windows.Markup.TypeConverterHelper.InvariantEnglishUS);
            }

            return base.ConvertToString(value, context);
        }
    }
}
