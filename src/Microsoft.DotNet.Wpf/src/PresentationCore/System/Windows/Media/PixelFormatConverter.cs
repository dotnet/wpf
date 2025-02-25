// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.ComponentModel.Design.Serialization;
using System.Diagnostics.CodeAnalysis;
using System.ComponentModel;
using System.Globalization;
using System.Reflection;

namespace System.Windows.Media
{
    /// <summary>
    /// Provides a type converter to convert from <see cref="PixelFormat"/> to <see cref="string"/> and vice versa.
    /// </summary>
    public sealed class PixelFormatConverter : TypeConverter
    {
        /// <summary>
        /// Returns whether this class can convert specific <see cref="Type"/> into <see cref="PixelFormat"/>.
        /// </summary>
        /// <param name="td">Context information used for conversion.</param>
        /// <param name="t">Type being evaluated for conversion.</param>
        /// <returns><see langword="true"/> if the given <paramref name="t"/> can be converted from, <see langword="false"/> otherwise.</returns>
        public override bool CanConvertFrom(ITypeDescriptorContext td, Type t)
        {
            // We can only handle string
            return t == typeof(string);
        }

        /// <summary>
        /// Returns whether this class can convert specified value to <see cref="string"/> or <see cref="InstanceDescriptor"/>.
        /// </summary>
        /// <param name="context">Context information used for conversion.</param>
        /// <param name="destinationType">Type being evaluated for conversion.</param>
        /// <returns><see langword="true"/> when <paramref name="destinationType"/> specified is
        /// <see cref="string"/> or <see cref="InstanceDescriptor"/>, <see langword="false"/> otherwise.</returns>
        public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType)
        {
            return destinationType == typeof(InstanceDescriptor) || destinationType == typeof(string) || base.CanConvertTo(context, destinationType);
        }

        /// <summary>
        /// Converts <paramref name="o"/> of <see cref="string"/> type to its <see cref="PixelFormat"/> represensation.
        /// </summary>
        /// <param name="value">The pixel format name to convert from.</param>
        /// <returns>A new instance of <see cref="PixelFormat"/> or <see langword="null"/> if the provided <paramref name="value"/> was <see langword="null"/>.</returns>
        [SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "Hiding method of TypeConverter.ConvertFromString(string)")]
        public new object ConvertFromString(string value)
        {
            return value is not null ? new PixelFormat(value) : null;
        }

        /// <summary>
        /// Converts <paramref name="o"/> of <see cref="string"/> type to its <see cref="PixelFormat"/> represensation.
        /// </summary>
        /// <param name="td">Context information used for conversion, ignored.</param>
        /// <param name="ci">The culture specifier to use, ignored.</param>        
        /// <param name="o">The string to convert from.</param>
        /// <returns>A new instance of <see cref="PixelFormat"/> or <see langword="null"/> if the provided <paramref name="value"/> was <see langword="null"/>.</returns>
        public override object ConvertFrom(ITypeDescriptorContext td, CultureInfo ci, object o)
        {
            return o is not null ? new PixelFormat(o as string) : null;
        }

        /// <summary>
        /// Converts a <paramref name="value"/> of <see cref="PixelFormat"/> to the specified <paramref name="destinationType"/>.
        /// </summary>
        /// <param name="context">Context information used for conversion.</param>
        /// <param name="culture">The culture specifier to use.</param>
        /// <param name="value"><see cref="PixelFormat"/> value to convert from.</param>
        /// <param name="destinationType">Type being evaluated for conversion.</param>
        /// <returns>A <see cref="string"/> or <see cref="InstanceDescriptor"/> representing the <see cref="PixelFormat"/> specified by <paramref name="value"/>.</returns>
        public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
        {
            ArgumentNullException.ThrowIfNull(destinationType);
            ArgumentNullException.ThrowIfNull(value);

            if (value is not PixelFormat pixelFormat)
                throw new ArgumentException(SR.Format(SR.General_Expected_Type, nameof(PixelFormat)));

            if (destinationType == typeof(InstanceDescriptor))
            {
                ConstructorInfo ci = typeof(PixelFormat).GetConstructor(new Type[] { typeof(string) });
                return new InstanceDescriptor(ci, new object[] { pixelFormat.ToString() });
            }
            else if (destinationType == typeof(string))
            {
                return pixelFormat.ToString();
            }

            return base.ConvertTo(context, culture, value, destinationType);
        }
    }
}
