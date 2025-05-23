// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/***************************************************************************\
*
*
*  Class for serializing a TemplateBindingExpression.
*
*
\***************************************************************************/
using System.ComponentModel;
using System.Windows.Markup;

namespace System.Windows
{


    /// <summary>
    /// Converts a template binding expression into a MarkupExtension.  This is used
    /// during serialization (the serializer native knows how to serialize an ME).
    /// </summary>
    public class TemplateBindingExpressionConverter: TypeConverter
    {
        /// <summary>
        /// Returns true for MarkupExtension
        /// </summary>
        public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType)
        {
            if (destinationType == typeof(MarkupExtension))
            {
                return true;
            }
            return base.CanConvertTo(context, destinationType);
        }

        /// <summary>
        /// Converts to a MarkupExtension
        /// </summary>
        public override object ConvertTo(ITypeDescriptorContext context, System.Globalization.CultureInfo culture, object value, Type destinationType)
        {
            if (destinationType == typeof(MarkupExtension))
            {
                TemplateBindingExpression templateBindingExpression = value as TemplateBindingExpression;
                if (templateBindingExpression == null)
                    throw new ArgumentException(SR.Format(SR.MustBeOfType, "value", "TemplateBindingExpression"));
                return templateBindingExpression.TemplateBindingExtension;
            }
            return base.ConvertTo(context, culture, value, destinationType);
        }
    }


}


