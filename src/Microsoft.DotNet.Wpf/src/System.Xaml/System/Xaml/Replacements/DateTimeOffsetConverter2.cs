// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.ComponentModel;
using System.ComponentModel.Design.Serialization;
using System.Globalization;
using System.Reflection;
using System.Security;

namespace System.Xaml.Replacements
{
    internal class DateTimeOffsetConverter2 : TypeConverter
    {
        public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType)
        {
            if (destinationType == typeof(string) || destinationType == typeof(InstanceDescriptor))
            {
                return true;
            }

            return base.CanConvertTo(context, destinationType);
        }

        public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
        {
            if (sourceType == typeof(string))
            {
                return true;
            }

            return base.CanConvertFrom(context, sourceType);
        }

        public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
        {
            if (value is DateTimeOffset dtOffset)
            {
                if (destinationType == typeof(string))
                {
                    return dtOffset.ToString("O", culture ?? CultureInfo.CurrentCulture);
                }
                else if (destinationType == typeof(InstanceDescriptor))
                {
                    // Use the year, month, day, hour, minute, second, millisecond, offset constructor
                    ConstructorInfo constructor = typeof(DateTimeOffset).GetConstructor(new Type[]
                    {
                        typeof(int),
                        typeof(int),
                        typeof(int),
                        typeof(int),
                        typeof(int),
                        typeof(int),
                        typeof(int),
                        typeof(TimeSpan)
                    });
                    return new InstanceDescriptor(
                        constructor,
                        new object[] {
                            dtOffset.Year,
                            dtOffset.Month,
                            dtOffset.Day,
                            dtOffset.Hour,
                            dtOffset.Minute,
                            dtOffset.Second,
                            dtOffset.Millisecond,
                            dtOffset.Offset
                        },
                        true);
                }
            }

            return base.ConvertTo(context, culture, value, destinationType);
        }

        public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
        {
            if (value is string s)
            {
                return DateTimeOffset.Parse(s.Trim(), culture ?? CultureInfo.CurrentCulture, DateTimeStyles.None);
            }

            return base.ConvertFrom(context, culture, value);
        }
    }
}
