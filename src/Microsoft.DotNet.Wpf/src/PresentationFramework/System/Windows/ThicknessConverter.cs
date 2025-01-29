// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// 
//
// Description: Contains the ThicknessConverter: TypeConverter for the Thickness struct.
//
//

using System.ComponentModel;
using System.ComponentModel.Design.Serialization;
using System.Runtime.CompilerServices;
using System.Globalization;
using System.Reflection;
using MS.Internal;

namespace System.Windows
{
    /// <summary>
    /// ThicknessConverter - Converter class for converting instances of other types to and from Thickness instances.
    /// </summary> 
    public class ThicknessConverter : TypeConverter
    {
        #region Public Methods

        /// <summary>
        /// CanConvertFrom - Returns whether or not this class can convert from a given type.
        /// </summary>
        /// <returns>
        /// bool - True if thie converter can convert from the provided type, false if not.
        /// </returns>
        /// <param name="typeDescriptorContext"> The ITypeDescriptorContext for this call. </param>
        /// <param name="sourceType"> The Type being queried for support. </param>
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
        /// ConvertFrom - Attempt to convert to a Thickness from the given object
        /// </summary>
        /// <returns>
        /// The Thickness which was constructed.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// An ArgumentNullException is thrown if the example object is null.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// An ArgumentException is thrown if the example object is not null and is not a valid type
        /// which can be converted to a Thickness.
        /// </exception>
        /// <param name="typeDescriptorContext"> The ITypeDescriptorContext for this call. </param>
        /// <param name="cultureInfo"> The CultureInfo which is respected when converting. </param>
        /// <param name="source"> The object to convert to a Thickness. </param>
        public override object ConvertFrom(ITypeDescriptorContext typeDescriptorContext, CultureInfo cultureInfo, object source)
        {
            if (source is null)
                throw GetConvertFromException(source);

            if (source is string sourceString)
                return FromString(sourceString, cultureInfo);
            else if (source is double sourceValue)
                return new Thickness(sourceValue);
            else
                return new Thickness(Convert.ToDouble(source, cultureInfo));         
        }

        /// <summary>
        /// ConvertTo - Attempt to convert a Thickness to the given type
        /// </summary>
        /// <returns>
        /// The object which was constructed.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// An ArgumentNullException is thrown if the example object is null.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// An ArgumentException is thrown if the object is not null and is not a Thickness,
        /// or if the destinationType isn't one of the valid destination types.
        /// </exception>
        /// <param name="typeDescriptorContext"> The ITypeDescriptorContext for this call. </param>
        /// <param name="cultureInfo"> The CultureInfo which is respected when converting. </param>
        /// <param name="value"> The Thickness to convert. </param>
        /// <param name="destinationType">The type to which to convert the Thickness instance. </param>
        public override object ConvertTo(ITypeDescriptorContext typeDescriptorContext, CultureInfo cultureInfo, object value, Type destinationType)
        {
            ArgumentNullException.ThrowIfNull(value);
            ArgumentNullException.ThrowIfNull(destinationType);

            if (value is not Thickness thickness)
                throw new ArgumentException(SR.Format(SR.UnexpectedParameterType, value.GetType(), typeof(Thickness)), nameof(value));

            if (destinationType == typeof(string))
                return ToString(thickness, cultureInfo);
            else if (destinationType == typeof(InstanceDescriptor))
            {
                ConstructorInfo ci = typeof(Thickness).GetConstructor(new Type[] { typeof(double), typeof(double), typeof(double), typeof(double) });
                return new InstanceDescriptor(ci, new object[] { thickness.Left, thickness.Top, thickness.Right, thickness.Bottom });
            }

            throw new ArgumentException(SR.Format(SR.CannotConvertType, typeof(Thickness), destinationType.FullName));
        }


        #endregion Public Methods

        //-------------------------------------------------------------------
        //
        //  Internal Methods
        //
        //-------------------------------------------------------------------

        #region Internal Methods

        /// <summary>
        /// Converts <paramref name="th"/> to its string representation using the specified <paramref name="cultureInfo"/>.
        /// </summary>
        /// <param name="th">The <see cref="Thickness"/> to convert to string.</param>
        /// <param name="cultureInfo">Culture to use when formatting doubles and choosing separator.</param>
        /// <returns>The formatted <paramref name="th"/> as string using the specified <paramref name="cultureInfo"/>.</returns>
        internal static string ToString(Thickness th, CultureInfo cultureInfo)
        {
            char listSeparator = TokenizerHelper.GetNumericListSeparator(cultureInfo);

            // Initial capacity [64] is an estimate based on a sum of:
            // 48 = 4x double (twelve digits is generous for the range of values likely)
            //  8 = 4x Unit Type string (approx two characters)
            //  3 = 3x separator characters
            //  1 = 1x scratch space for alignment

            DefaultInterpolatedStringHandler handler = new(0, 7, cultureInfo, stackalloc char[64]);
            LengthConverter.FormatLengthAsString(th.Left, ref handler);
            handler.AppendFormatted(listSeparator);

            LengthConverter.FormatLengthAsString(th.Top, ref handler);
            handler.AppendFormatted(listSeparator);

            LengthConverter.FormatLengthAsString(th.Right, ref handler);
            handler.AppendFormatted(listSeparator);

            LengthConverter.FormatLengthAsString(th.Bottom, ref handler);

            return handler.ToStringAndClear();
        }

        /// <summary>
        /// Constructs a <see cref="Thickness"/> struct out of string representation supplied by <paramref name="s"/> and the specified <paramref name="cultureInfo"/>.
        /// </summary>
        /// <param name="s">The string representation of a <see cref="Thickness"/> struct.</param>
        /// <param name="cultureInfo">The <see cref="CultureInfo"/> which was used to format this string.</param>
        /// <returns>A new instance of <see cref="Thickness"/> struct representing the data contained in <paramref name="s"/>.</returns>
        /// <exception cref="FormatException">Thrown when <paramref name="s"/> contains invalid string representation.</exception>
        internal static Thickness FromString(string s, CultureInfo cultureInfo)
        {
            TokenizerHelper th = new(s, cultureInfo);
            Span<double> lengths = stackalloc double[4];
            int i = 0;

            // Peel off each double in the delimited list.
            while (th.NextToken())
            {
                if (i >= 4) // In case we've got more than 4 doubles, we throw
                    throw new FormatException(SR.Format(SR.InvalidStringThickness, s));

                lengths[i] = LengthConverter.FromString(th.GetCurrentToken(), cultureInfo);
                i++;
            }

            // We have a reasonable interpretation for one value (all four edges),
            // two values (horizontal, vertical),
            // and four values (left, top, right, bottom).
            return i switch
            {
                1 => new Thickness(lengths[0]),
                2 => new Thickness(lengths[0], lengths[1], lengths[0], lengths[1]),
                4 => new Thickness(lengths[0], lengths[1], lengths[2], lengths[3]),
                _ => throw new FormatException(SR.Format(SR.InvalidStringThickness, s)),
            };
        }

    #endregion

    }
}
