// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.ComponentModel;
using System.ComponentModel.Design.Serialization;
using System.Globalization;
using System.Security;

namespace System.Windows.Markup
{
    /// <summary>
    /// Converter to convert StaticExtensions to InstanceDescriptors
    /// </summary>
    internal class StaticExtensionsToInstanceDescriptorsConverter : TypeConverter
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
                if (!(value is StaticExtension staticExtension))
                {
                    throw new ArgumentException(SR.Get(SRID.MustBeOfType, nameof(value), nameof(StaticExtension)));
                }

                return new InstanceDescriptor(
                    typeof(StaticExtension).GetConstructor(new Type[] { typeof(string) }),
                    new object[] { staticExtension.Member }
                );
            }

            return base.ConvertTo(context, culture, value, destinationType);
        }
    }
}
