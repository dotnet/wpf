// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel.Design.Serialization;
using System.ComponentModel;
using System.Globalization;
using System.Reflection;
using MS.Internal;

namespace System.Windows
{
    /// <summary>
    /// Converter class for converting instances of other types to and from <see cref="CornerRadius"/> instances.
    /// </summary> 
    public class CornerRadiusConverter : TypeConverter
    {
        #region Public Methods

        /// <summary>
        /// CanConvertFrom - Returns whether or not this class can convert from a given type.
        /// </summary>
        /// <returns>
        /// <see langword="true"/> if the given <paramref name="sourceType"/> can be converted from, <see langword="false"/> otherwise.
        /// </returns>
        /// <param name="typeDescriptorContext">The <see cref="ITypeDescriptorContext"/> for this call.</param>
        /// <param name="sourceType">The <see cref="Type"/> being queried for support.</param>
        public override bool CanConvertFrom(ITypeDescriptorContext typeDescriptorContext, Type sourceType)
        {
            // We can only handle strings, integral and floating types
            TypeCode tc = Type.GetTypeCode(sourceType);
            switch (tc)
            {
                case TypeCode.String:
                case TypeCode.Decimal:
                case TypeCode.Single:
                case TypeCode.Double:
                case TypeCode.Int16:
                case TypeCode.Int32:
                case TypeCode.Int64:
                case TypeCode.UInt16:
                case TypeCode.UInt32:
                case TypeCode.UInt64:
                    return true;

                default:
                    return false;
            }
        }

        /// <summary>
        /// CanConvertTo - Returns whether or not this class can convert to a given type.
        /// </summary>
        /// <returns>
        /// bool - True if this converter can convert to the provided type, false if not.
        /// </returns>
        /// <param name="typeDescriptorContext"> The ITypeDescriptorContext for this call. </param>
        /// <param name="destinationType"> The Type being queried for support. </param>
        public override bool CanConvertTo(ITypeDescriptorContext typeDescriptorContext, Type destinationType)
        {
            // We can convert to an InstanceDescriptor or to a string.
            return destinationType == typeof(InstanceDescriptor) || destinationType == typeof(string);
        }

        /// <summary>
        /// ConvertFrom - Attempt to convert to a CornerRadius from the given object
        /// </summary>
        /// <returns>
        /// The CornerRadius which was constructed.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// An ArgumentNullException is thrown if the example object is null.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// An ArgumentException is thrown if the example object is not null and is not a valid type
        /// which can be converted to a CornerRadius.
        /// </exception>
        /// <param name="typeDescriptorContext"> The ITypeDescriptorContext for this call. </param>
        /// <param name="cultureInfo"> The CultureInfo which is respected when converting. </param>
        /// <param name="source"> The object to convert to a CornerRadius. </param>
        public override object ConvertFrom(ITypeDescriptorContext typeDescriptorContext, CultureInfo cultureInfo, object source)
        {
            if (source is null)
                throw GetConvertFromException(source);

            if (source is string stringValue)
                return FromString(stringValue, cultureInfo);

            // Conversion from a numeric type
            return new CornerRadius(Convert.ToDouble(source, cultureInfo));
        }

        /// <summary>
        /// ConvertTo - Attempt to convert a CornerRadius to the given type
        /// </summary>
        /// <returns>
        /// The object which was constructoed.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// An ArgumentNullException is thrown if the example object is null.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// An ArgumentException is thrown if the object is not null and is not a CornerRadius,
        /// or if the destinationType isn't one of the valid destination types.
        /// </exception>
        /// <param name="typeDescriptorContext"> The ITypeDescriptorContext for this call. </param>
        /// <param name="cultureInfo"> The CultureInfo which is respected when converting. </param>
        /// <param name="value"> The CornerRadius to convert. </param>
        /// <param name="destinationType">The type to which to convert the CornerRadius instance. </param>
        public override object ConvertTo(ITypeDescriptorContext typeDescriptorContext, CultureInfo cultureInfo, object value, Type destinationType)
        {
            ArgumentNullException.ThrowIfNull(value);
            ArgumentNullException.ThrowIfNull(destinationType);

            if (value is not CornerRadius cornerRadius)
                throw new ArgumentException(SR.Format(SR.UnexpectedParameterType, value.GetType(), typeof(CornerRadius)), nameof(value));

            if (destinationType == typeof(string))
                return ToString(cornerRadius, cultureInfo);

            if (destinationType == typeof(InstanceDescriptor))
            {
                ConstructorInfo ci = typeof(CornerRadius).GetConstructor(new Type[] { typeof(double), typeof(double), typeof(double), typeof(double) });
                return new InstanceDescriptor(ci, new object[] { cornerRadius.TopLeft, cornerRadius.TopRight, cornerRadius.BottomRight, cornerRadius.BottomLeft });
            }

            throw new ArgumentException(SR.Format(SR.CannotConvertType, typeof(CornerRadius), destinationType.FullName));
        }

        #endregion Public Methods

        //-------------------------------------------------------------------
        //
        //  Internal Methods
        //
        //-------------------------------------------------------------------

        #region Internal Methods

        internal static string ToString(CornerRadius cr, CultureInfo cultureInfo)
        {
            char listSeparator = TokenizerHelper.GetNumericListSeparator(cultureInfo);

            return string.Create(cultureInfo, stackalloc char[64], $"{cr.TopLeft}{listSeparator}{cr.TopRight}{listSeparator}{cr.BottomRight}{listSeparator}{cr.BottomLeft}");
        }

        /// <summary>
        /// Parses a <see cref="CornerRadius"/> from a <see cref="string"/> given the <see cref="CultureInfo"/>.
        /// </summary>
        /// <param name="input"><see cref="string"/> to parse from.</param>
        /// <param name="cultureInfo">The <see cref="CultureInfo"/> that is respected during parsing.</param>
        /// <returns>A new instance of <see cref="CornerRadius"/>.</returns>
        internal static CornerRadius FromString(string input, CultureInfo cultureInfo)
        {
            ValueTokenizerHelper tokenizer = new(input, cultureInfo);
            Span<double> radii = stackalloc double[4];
            int i = 0;

            // Peel off each Length in the delimited list.
            while (tokenizer.NextToken())
            {
                if (i >= 4)
                    throw new FormatException(SR.Format(SR.InvalidStringCornerRadius, input));

                radii[i] = double.Parse(tokenizer.GetCurrentToken(), cultureInfo);
                i++;
            }

            // We have a reasonable interpreation for one value (all four edges)
            // and four values (left, top, right, bottom).
            return i switch
            {
                1 => new CornerRadius(radii[0]),
                4 => new CornerRadius(radii[0], radii[1], radii[2], radii[3]),
                _ => throw new FormatException(SR.Format(SR.InvalidStringCornerRadius, input)),
            };
        }
        #endregion
    }
}
