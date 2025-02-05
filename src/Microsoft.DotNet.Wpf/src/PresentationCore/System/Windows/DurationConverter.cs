// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel.Design.Serialization;
using System.ComponentModel;
using System.Globalization;
using System.Reflection;

namespace System.Windows
{
    /// <summary>
    /// Provides a type converter to convert from <see cref="Duration"/> to <see cref="string"/> and vice versa.
    /// </summary>
    public class DurationConverter : TypeConverter
    {
        /// <summary>
        /// Returns whether this class can convert specific <see cref="Type"/> into <see cref="Duration"/>.
        /// </summary>
        /// <param name="td">Context information used for conversion.</param>
        /// <param name="t">Type being evaluated for conversion.</param>
        /// <returns><see langword="true"/> if the given <paramref name="t"/> can be converted from, <see langword="false"/> otherwise.</returns>
        public override bool CanConvertFrom(ITypeDescriptorContext td, Type t)
        {
            return t == typeof(string);
        }

        /// <summary>
        /// Returns whether this class can convert specified value to <see cref="string"/>.
        /// </summary>
        /// <param name="context">Context information used for conversion.</param>
        /// <param name="destinationType">Type being evaluated for conversion.</param>
        /// <returns><see langword="true"/> if conversion to <see cref="string"/> is possible, <see langword="false"/> otherwise.</returns>
        public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType)
        {
            return destinationType == typeof(InstanceDescriptor) || destinationType == typeof(string);
        }

        /// <summary>
        /// Converts <paramref name="value"/> of <see cref="string"/> type to its <see cref="Duration"/> representation.
        /// </summary>
        /// <param name="td">Context information used for conversion.</param>
        /// <param name="cultureInfo">The culture specifier to use.</param>
        /// <param name="value">The string to convert from.</param>
        /// <returns>A <see cref="Duration"/> representing the <see cref="string"/> specified by <paramref name="value"/>.</returns>
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
        /// Facilities parsing from <paramref name="valueSpan"/> to <see cref="TimeSpan"/> and initializes new <see cref="Duration"/> instance.
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
        /// Converts a <paramref name="value"/> of <see cref="Duration"/> to its <see cref="string"/> representation.
        /// </summary>
        /// <param name="context">Context information used for conversion.</param>
        /// <param name="cultureInfo">The culture specifier to use, currently ignored during conversion.</param>
        /// <param name="value">Duration value to convert from.</param>
        /// <param name="destinationType">Type being evaluated for conversion.</param>
        /// <returns>A <see cref="string"/> representing the <see cref="Duration"/> specified by <paramref name="value"/>.</returns>
        public override object ConvertTo(ITypeDescriptorContext context, CultureInfo cultureInfo, object value, Type destinationType)
        {
            ArgumentNullException.ThrowIfNull(destinationType);

            // Check that we actually support the conversion, NULL value results in string.Empty, others throw
            if (value is not Duration duration || (destinationType != typeof(InstanceDescriptor) && destinationType != typeof(string)))
                return base.ConvertTo(context, cultureInfo, value, destinationType);

            // The current behaviour produces culture-invariant result in case of a Duration with HasTimeSpan,
            // whereas ConvertFrom accepts localized input, provided the correct cultureInfo was supplied.
            if (destinationType == typeof(string))
                return duration.ToStringInvariant();

            // InstanceDescriptor reflection magic
            if (duration.HasTimeSpan)
            {
                MemberInfo mi = typeof(Duration).GetConstructor(new Type[] { typeof(TimeSpan) });
                return new InstanceDescriptor(mi, new object[] { duration.TimeSpan });
            }
            else if (duration == Duration.Forever)
            {
                MemberInfo mi = typeof(Duration).GetProperty("Forever");
                return new InstanceDescriptor(mi, null);
            }
            else
            {
                Debug.Assert(duration == Duration.Automatic); // Only other legal duration type

                MemberInfo mi = typeof(Duration).GetProperty("Automatic");
                return new InstanceDescriptor(mi, null);
            }
        }
    }
}
