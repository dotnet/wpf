// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//

using System.ComponentModel;
using System.ComponentModel.Design.Serialization;
using System.Globalization;
using System.Reflection;

namespace System.Windows
{
    /// <summary>
    /// Provides a type converter to convert Duration to and from other representations.
    /// </summary>
    public class DurationConverter : TypeConverter
    {
        /// <summary>
        /// CanConvertFrom - Returns whether or not this class can convert from a given type
        /// </summary>
        /// <ExternalAPI/>
        public override bool CanConvertFrom(ITypeDescriptorContext td, Type t)
        {
            return t == typeof(string);
        }

        /// <summary>
        /// TypeConverter method override.
        /// </summary>
        /// <param name="context">ITypeDescriptorContext</param>
        /// <param name="destinationType">Type to convert to</param>
        /// <returns>true if conversion is possible</returns>
        /// <ExternalAPI/>
        public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType)
        {
            return destinationType == typeof(InstanceDescriptor) || destinationType == typeof(string);
        }

        /// <summary>
        /// ConvertFrom
        /// </summary>
        /// <ExternalAPI/>
        public override object ConvertFrom(ITypeDescriptorContext td, CultureInfo cultureInfo, object value)
        {
            // In case value is not a string, we can try to check InstanceDescriptor or we just throw NotSupportedException
            if (value is not string stringValue)
                return base.ConvertFrom(td, cultureInfo, value);

            // Sanitize the input
            ReadOnlySpan<char> valueSpan = stringValue.AsSpan().Trim();

            // In case it is not a pre-defined value, we will try to parse the TimeSpan and if it throws,
            // we will catch it as TimeSpanConverter does and rethrow with the inner exception information.
            if (valueSpan.Equals("Automatic", StringComparison.Ordinal))
                return Duration.Automatic;
            else if (valueSpan.Equals("Forever", StringComparison.Ordinal))
                return Duration.Forever;
            else
                return ParseTimeSpan(valueSpan, cultureInfo);
        }

        /// <summary>
        /// Faciliaties parsing from <paramref name="valueSpan"/> to <see cref="TimeSpan"/> and initializes new <see cref="Duration"/> instance.
        /// </summary>
        /// <param name="valueSpan">The string to convert from.</param>
        /// <param name="cultureInfo">The culture specifier to use.</param>
        /// <returns>A newly initialized <see cref="Duration"/> instance from the <paramref name="valueSpan"/> string.</returns>
        /// <remarks>This function is decoupled from the <see cref="ConvertFrom(ITypeDescriptorContext, CultureInfo, object)"/> for performance reasons.</remarks>
        /// <exception cref="FormatException">Thrown when parsing of <paramref name="valueSpan"/> to <see cref="TimeSpan"/> instance fails.</exception>
        private static Duration ParseTimeSpan(ReadOnlySpan<char> valueSpan, CultureInfo cultureInfo)
        {
            try
            {
                return new Duration(TimeSpan.Parse(valueSpan, cultureInfo));
            }
            catch (FormatException e)
            {
                throw new FormatException($"{valueSpan} is not a valid value for {nameof(TimeSpan)}.", e);
            }
        }

        /// <summary>
        /// TypeConverter method implementation.
        /// </summary>
        /// <param name="context">ITypeDescriptorContext</param>
        /// <param name="cultureInfo">current culture (see CLR specs)</param>
        /// <param name="value">value to convert from</param>
        /// <param name="destinationType">Type to convert to</param>
        /// <returns>converted value</returns>
        /// <ExternalAPI/>
        public override object ConvertTo(
            ITypeDescriptorContext context, 
            CultureInfo cultureInfo, 
            object value, 
            Type destinationType)
        {
            if (destinationType != null && value is Duration)
            {
                Duration durationValue = (Duration)value;

                if (destinationType == typeof(InstanceDescriptor))
                {
                    MemberInfo mi;

                    if (durationValue.HasTimeSpan)
                    {
                        mi = typeof(Duration).GetConstructor(new Type[] { typeof(TimeSpan) });

                        return new InstanceDescriptor(mi, new object[] { durationValue.TimeSpan });
                    }
                    else if (durationValue == Duration.Forever)
                    {
                        mi = typeof(Duration).GetProperty("Forever");

                        return new InstanceDescriptor(mi, null);
                    }
                    else
                    {
                        Debug.Assert(durationValue == Duration.Automatic);  // Only other legal duration type

                        mi = typeof(Duration).GetProperty("Automatic");

                        return new InstanceDescriptor(mi, null);
                    }
                }
                else if (destinationType == typeof(string))
                {
                    return durationValue.ToString();
                }
            }

            // Pass unhandled cases to base class (which will throw exceptions for null value or destinationType.)
            return base.ConvertTo(context, cultureInfo, value, destinationType);
        }
    }
}
