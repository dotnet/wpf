// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
//
//
//  Contents:  Implements a converter to an instance descriptor for 
//             TemplateBindingExtension
//
//

using System;
using System.ComponentModel;
using System.ComponentModel.Design.Serialization;
using System.Globalization;
using System.Windows;
using System.Security;

namespace System.Windows
{
    /// <summary>
    /// Type converter to inform the serialization system how to construct a TemplateBindingExtension from
    /// an instance. It reports that Property should be used as the first parameter to the constructor.
    /// </summary>
    public class TemplateBindingExtensionConverter : TypeConverter 
    {
        /// <summary>
        /// Returns true if converting to an InstanceDescriptor
        /// </summary>
        /// <param name="context"></param>
        /// <param name="destinationType"></param>
        /// <returns></returns>
        public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType)
        {
            if (destinationType == typeof(InstanceDescriptor))
            {
                return true;
            }
            return base.CanConvertTo(context, destinationType);
        }

        /// <summary>
        /// Converts to an InstanceDescriptor
        /// </summary>
        public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
        {
            if (destinationType == typeof(InstanceDescriptor))
            {
                if(value == null)
                    throw new ArgumentNullException("value");

                TemplateBindingExtension templateBinding = value as TemplateBindingExtension;

                if(templateBinding == null)
                    throw new ArgumentException(SR.Get(SRID.MustBeOfType, "value", "TemplateBindingExtension"), "value");

                return new InstanceDescriptor(typeof(TemplateBindingExtension).GetConstructor(new Type[] { typeof(DependencyProperty) }),
                    new object[] { templateBinding.Property });
            }
            return base.ConvertTo(context, culture, value, destinationType);
        }

    }
}
