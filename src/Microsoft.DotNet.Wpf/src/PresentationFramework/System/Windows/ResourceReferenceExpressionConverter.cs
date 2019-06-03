// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
//
// Description:
//   TypeConverter for a resource value expression
//
//

using System;
using System.ComponentModel;
using System.Globalization;
using System.ComponentModel.Design.Serialization;
using System.Diagnostics;

using System.Windows;

namespace System.Windows.Markup
{
    /// <summary>
    ///     TypeConverter for a resource value expression
    /// </summary>
    public class ResourceReferenceExpressionConverter : ExpressionConverter
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
            return base.CanConvertFrom(context, sourceType);
        }

        /// <summary>
        ///     TypeConverter method override.
        /// </summary>
        /// <remarks>
        ///     For avalon serialization this converter
        ///     returns true for string types only if the
        ///     target element with the resource
        ///     dictionary for the current resource
        ///     reference is also being serialized. Else
        ///     it returns false and the serialization
        ///     engine must serialize the evaluated
        ///     value of the expression.
        /// </remarks>
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
            // Validate Input Arguments
            if (destinationType == null)
            {
                throw new ArgumentNullException("destinationType");
            }

            //MarkupExtention
            if (destinationType == typeof(MarkupExtension))
                return true;

            return base.CanConvertTo(context, destinationType);
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
            return base.ConvertFrom(context, culture, value);
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
            // Validate Input Arguments
            ResourceReferenceExpression expr = value as ResourceReferenceExpression;
            if (expr == null)
            {
                throw new ArgumentException(SR.Get(SRID.MustBeOfType, "value", "ResourceReferenceExpression"));
            }
            if (destinationType == null)
            {
                throw new ArgumentNullException("destinationType");
            }

            // MarkupExtension
            if (destinationType == typeof(MarkupExtension))
            {
                return new DynamicResourceExtension(expr.ResourceKey);
            }

            return base.ConvertTo(context, culture, value, destinationType);
        }
    }
}

