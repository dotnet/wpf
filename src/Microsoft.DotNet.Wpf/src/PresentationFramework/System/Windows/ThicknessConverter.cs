// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// 
//
// Description: Contains the ThicknessConverter: TypeConverter for the Thicknessclass.
//
//

using System;
using System.ComponentModel;
using System.ComponentModel.Design.Serialization;
using System.Runtime.CompilerServices;
using System.Globalization;
using System.Reflection;
using System.Text;
using System.Windows;
using System.Security;
using MS.Internal;
using MS.Utility;

#pragma warning disable 1634, 1691  // suppressing PreSharp warnings

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
            if (    destinationType == typeof(InstanceDescriptor) 
                ||  destinationType == typeof(string))
            {
                return true;
            }
            else
            {
                return false;
            }
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
            if (source != null)
            {
                if (source is string)      { return FromString((string)source, cultureInfo); }
                else if (source is double) { return new Thickness((double)source); }
                else                       { return new Thickness(Convert.ToDouble(source, cultureInfo)); }
            }
            throw GetConvertFromException(source);
        }

        /// <summary>
        /// ConvertTo - Attempt to convert a Thickness to the given type
        /// </summary>
        /// <returns>
        /// The object which was constructoed.
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

            if (!(value is Thickness))
            {
                #pragma warning suppress 6506 // value is obviously not null
                throw new ArgumentException(SR.Format(SR.UnexpectedParameterType, value.GetType(), typeof(Thickness)), "value");
            }

            Thickness th = (Thickness)value;
            if (destinationType == typeof(string)) { return ToString(th, cultureInfo); }
            if (destinationType == typeof(InstanceDescriptor))
            {
                ConstructorInfo ci = typeof(Thickness).GetConstructor(new Type[] { typeof(double), typeof(double), typeof(double), typeof(double) });
                return new InstanceDescriptor(ci, new object[] { th.Left, th.Top, th.Right, th.Bottom });
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

        static internal string ToString(Thickness th, CultureInfo cultureInfo)
        {
            char listSeparator = TokenizerHelper.GetNumericListSeparator(cultureInfo);

            // Initial capacity [64] is an estimate based on a sum of:
            // 48 = 4x double (twelve digits is generous for the range of values likely)
            //  8 = 4x Unit Type string (approx two characters)
            //  3 = 3x separator characters
            //  1 = 1x scratch space for alignment

            DefaultInterpolatedStringHandler handler = new(0, 7, cultureInfo, stackalloc char[64]);
            FormatDoubleAsString(th.Left, ref handler);
            handler.AppendFormatted(listSeparator);

            FormatDoubleAsString(th.Top, ref handler);
            handler.AppendFormatted(listSeparator);

            FormatDoubleAsString(th.Right, ref handler);
            handler.AppendFormatted(listSeparator);

            FormatDoubleAsString(th.Bottom, ref handler);

            return handler.ToStringAndClear();
        }

        /// <summary> Holds the "Auto" string representation for <see cref="double.NaN"/> conversion. </summary>
        private static ReadOnlySpan<char> NaNValue => ['A', 'u', 't', 'o'];

        /// <summary> Format <see cref="double"/> into <see cref="string"/> using specified <see cref="CultureInfo"/>
        /// in <paramref name="handler"/>. <br /> <br />
        /// Special representation applies for <see cref="double.NaN"/> values, emitted as "Auto" string instead. </summary>
        /// <param name="value">The value to format as string.</param>
        /// <param name="handler">The handler specifying culture used for conversion.</param>
        static internal void FormatDoubleAsString(double value, ref DefaultInterpolatedStringHandler handler)
        {
            if (double.IsNaN(value))
                handler.AppendFormatted(NaNValue);
            else
                handler.AppendFormatted(value);
        }

        static internal Thickness FromString(string s, CultureInfo cultureInfo)
        {
            TokenizerHelper th = new(s, cultureInfo);
            Span<double> lengths = stackalloc double[4];
            int i = 0;

            // Peel off each double in the delimited list.
            while (th.NextToken())
            {
                if (i == 4) // In case we've got more than 4 doubles, we throw
                    throw new FormatException(SR.Format(SR.InvalidStringThickness, s));

                lengths[i++] = LengthConverter.FromString(th.GetCurrentToken(), cultureInfo);
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
