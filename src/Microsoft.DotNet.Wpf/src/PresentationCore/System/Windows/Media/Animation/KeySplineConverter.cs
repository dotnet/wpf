// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel.Design.Serialization;
using System.Runtime.CompilerServices;
using System.Windows.Media.Animation;
using System.ComponentModel;
using System.Globalization;
using System.Reflection;
using MS.Internal;

namespace System.Windows
{
    /// <summary>
    /// Converter class for converting instances of <see cref="KeySpline"/> to <see cref="string"/> and vice versa.
    /// </summary>
    /// <ExternalAPI/> 
    public class KeySplineConverter : TypeConverter
    {
        /// <summary>
        /// CanConvertFrom - Returns whether or not this class can convert from a given type
        /// </summary>
        /// <ExternalAPI/>
        public override bool CanConvertFrom(ITypeDescriptorContext typeDescriptor, Type destinationType)
        {
            return destinationType == typeof(string);
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
        public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo cultureInfo, object value)
        {
            if (value is not string stringValue)
                throw new NotSupportedException(SR.Converter_ConvertFromNotSupported);

            ValueTokenizerHelper tokenizer = new(stringValue, cultureInfo);

            return new KeySpline(double.Parse(tokenizer.NextTokenRequired(), cultureInfo),
                                 double.Parse(tokenizer.NextTokenRequired(), cultureInfo),
                                 double.Parse(tokenizer.NextTokenRequired(), cultureInfo),
                                 double.Parse(tokenizer.NextTokenRequired(), cultureInfo));
        }

        /// <summary>
        /// TypeConverter method implementation.
        /// </summary>
        /// <param name="context">ITypeDescriptorContext</param>
        /// <param name="cultureInfo">current culture (see CLR specs), null is a valid value</param>
        /// <param name="value">value to convert from</param>
        /// <param name="destinationType">Type to convert to</param>
        /// <returns>converted value</returns>
        /// <ExternalAPI/>
        public override object ConvertTo(ITypeDescriptorContext context, CultureInfo cultureInfo, object value, Type destinationType)
        {
            if (value is KeySpline keySpline && destinationType is not null)
            {
                if (destinationType == typeof(string))
                    return ToString(keySpline, cultureInfo);

                if (destinationType == typeof(InstanceDescriptor))
                {
                    ConstructorInfo ci = typeof(KeySpline).GetConstructor(new Type[] { typeof(double), typeof(double), typeof(double), typeof(double) });
                    return new InstanceDescriptor(ci, new object[] { keySpline.ControlPoint1.X, keySpline.ControlPoint1.Y, keySpline.ControlPoint2.X, keySpline.ControlPoint2.Y });
                }
            }

            // Pass unhandled cases to base class (which will throw exceptions for null value or destinationType)
            return base.ConvertTo(context, cultureInfo, value, destinationType);
        }

        /// <summary>
        /// Converts <paramref name="keySpline"/> to its string representation using the specified <paramref name="cultureInfo"/>.
        /// </summary>
        /// <param name="keySpline">The <see cref="KeySpline"/> to convert to string.</param>
        /// <param name="cultureInfo">Culture to use when formatting doubles and choosing separator.</param>
        /// <returns>The formatted <paramref name="keySpline"/> as string using the specified <paramref name="cultureInfo"/>.</returns>
        private static string ToString(KeySpline keySpline, CultureInfo cultureInfo)
        {
            string listSeparator = cultureInfo != null ? cultureInfo.TextInfo.ListSeparator : CultureInfo.InvariantCulture.TextInfo.ListSeparator;

            // Initial capacity [64] is an estimate based on a sum of:
            // 48 = 4x double (fourteen digits is generous for the range of values likely)
            //  3 = 3x separator characters
            //  1 = 1x scratch space for alignment
            DefaultInterpolatedStringHandler handler = new(3, 4, cultureInfo, stackalloc char[64]);
            handler.AppendFormatted(keySpline.ControlPoint1.X);
            handler.AppendLiteral(listSeparator);

            handler.AppendFormatted(keySpline.ControlPoint1.Y);
            handler.AppendLiteral(listSeparator);

            handler.AppendFormatted(keySpline.ControlPoint2.X);
            handler.AppendLiteral(listSeparator);

            handler.AppendFormatted(keySpline.ControlPoint2.Y);

            return handler.ToStringAndClear();
        }
    }
}
