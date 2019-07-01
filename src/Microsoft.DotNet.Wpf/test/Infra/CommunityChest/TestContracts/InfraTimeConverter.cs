// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.ComponentModel;
using System.Globalization;

namespace Microsoft.Test
{
    /// <summary>
    /// A converter for InfraTime
    /// HACK Motivation #1: In the case of DateTimeFormat as we need higher precision than stock 
    /// HACK Motivation #2: Due to bug 824978(Custom Separator chars not honored with custom culture)
    /// To work around #2, while achieving #1, we convert DateTime to Windows file time, in long. 
    /// </summary>
    public class InfraTimeConverter : TypeConverter
    {
        /// <summary>
        /// CanConvertFrom - Returns whether or not this class can convert from a given type.
        /// </summary>
        /// <param name="context">The ITypeDescriptorContext.</param>
        /// <param name="sourceType">The Type being queried for support.</param>
        /// <returns>bool - True if thie converter can convert from the provided type, false if not.</returns>
        public override bool CanConvertFrom(ITypeDescriptorContext context,
           Type sourceType)
        {
            if (sourceType == typeof(string)) { return true; }
            return base.CanConvertFrom(context, sourceType);
        }
        /// <summary>
        /// CanConvertTo - Returns whether or not this class can convert to a given type.
        /// </summary>
        /// <param name="context">The ITypeDescriptorContext.</param>
        /// <param name="destinationType">The Type being queried for support.</param>
        /// <returns>bool - True if this converter can convert to the provided type, false if not.</returns>
        public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType)
        {
            if (destinationType == typeof(string)) { return true; }
            return base.CanConvertTo(context, destinationType);
        }

        /// <summary>
        /// ConvertFrom - Attempt to convert to InfraTime from the given object.
        /// </summary>
        /// <param name="context">The ITypeDescriptorContext.</param>
        /// <param name="culture">The CulturfraeInfo which is respected when converting.</param>
        /// <param name="value">The object to convert to a InfraTime</param>
        /// <returns>The InfraTime constructed.</returns>
        public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
        {
            if (value is string)
            {
                long fileTime = Int64.Parse((string)value);
                DateTime dateTime = DateTime.FromFileTime(fileTime);
                InfraTime infraTime = new InfraTime(dateTime);
                
                return infraTime;
            }
            return base.ConvertFrom(context, culture, value);
        }

        /// <summary>
        /// ConvertTo : convert InfraTime to Windows file time. 
        /// </summary>
        /// <param name="context">The ITypeDescriptorContext.</param>
        /// <param name="culture">The CultureInfo which is respected when converting.</param>
        /// <param name="value">The InfraTime to convert.</param>
        /// <param name="destinationType">The type to which to convert to.</param>
        /// <returns></returns>
        public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
        {
            if (destinationType == typeof(string))
            {
                InfraTime infraTime = value as InfraTime;
                DateTime dateTime = infraTime.DateTime;
                long fileTime = dateTime.ToFileTime();
                return Convert.ToString(fileTime, NumberFormatInfo.InvariantInfo);
            }
            return base.ConvertTo(context, culture, value, destinationType);
        }
    }
}