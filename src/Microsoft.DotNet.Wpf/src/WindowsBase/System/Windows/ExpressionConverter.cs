// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
//
// Description:
//   TypeConverter for a generic property value expression
//
//

using System;
using System.ComponentModel;
using System.Globalization;
using System.ComponentModel.Design.Serialization;

namespace System.Windows
{
    /// <summary>
    ///     TypeConverter for a generic property value expression
    /// </summary>
    /// <remarks>
    ///     The cole purpose of this TypeConveret is to block the 
    ///     default TypeConverter/ ToString() behavior
    /// </remarks>
    public class ExpressionConverter : TypeConverter
    {
        /// <summary>
        ///     TypeConverter method override.
        /// </summary>
        /// <param name="context">
        ///     ITypeDescriptorContext
        /// </param>
        /// <param name="sourceType">
        ///     Type to convert from
        /// </param>
        /// <returns>
        ///     true if conversion is possible
        /// </returns>
        public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
        {
            return false;
        }
    
        /// <summary>
        ///     TypeConverter method override.
        /// </summary>
        /// <param name="context">
        ///     ITypeDescriptorContext
        /// </param>
        /// <param name="destinationType">
        ///     Type to convert to
        /// </param>
        /// <returns>
        ///     true if conversion is possible
        /// </returns>
        public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType) 
        {
            return false;
        }
        
        /// <summary>
        ///     TypeConverter method implementation.
        /// </summary>
        /// <param name="context">
        ///     ITypeDescriptorContext
        /// </param>
        /// <param name="culture">
        ///     current culture (see CLR specs)
        /// </param>
        /// <param name="value">
        ///     value to convert from
        /// </param>
        /// <returns>
        ///     value that is result of conversion
        /// </returns>
        public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
        {
            throw GetConvertFromException(value);
        }

        /// <summary>
        ///     TypeConverter method implementation.
        /// </summary>
        /// <param name="context">
        ///     ITypeDescriptorContext
        /// </param>
        /// <param name="culture">
        ///     current culture (see CLR specs)
        /// </param>
        /// <param name="value">
        ///     value to convert from
        /// </param>
        /// <param name="destinationType">
        ///     Type to convert to
        /// </param>
        /// <returns>
        ///     converted value
        /// </returns>
        public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
        {
            throw GetConvertToException(value, destinationType);
        }
    }
}

