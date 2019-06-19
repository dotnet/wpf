// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
//  Microsoft Windows Client Platform
//
//
//  Contents:  Converter to convert TypeExtensions to InstanceDescriptors

//  Created:   04/28/2005 Microsoft
//

using System.ComponentModel;
using System.ComponentModel.Design.Serialization;
using System.Globalization;
using System.Security;

namespace System.Windows.Markup
{
    class TypeExtensionConverter : TypeConverter
    {
        public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType)
        {
            if (destinationType == typeof(InstanceDescriptor))
            {
                return true;
            }
            return base.CanConvertTo(context, destinationType);
        }

        public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
        {
            if (destinationType == typeof(InstanceDescriptor))
            {
                TypeExtension typeExtension = value as TypeExtension;
                if (typeExtension == null)
                {
                    throw new ArgumentException(SR.Get(SRID.MustBeOfType, "value", "TypeExtension")); 
                }
                return new InstanceDescriptor(typeof(TypeExtension).GetConstructor(new Type[] { typeof(Type) }),
                                              new object[] { typeExtension.Type });
            }
            return base.ConvertTo(context, culture, value, destinationType);
        }
    }
}
