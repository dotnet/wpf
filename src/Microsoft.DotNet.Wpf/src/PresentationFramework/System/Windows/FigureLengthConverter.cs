// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// 
//
// Description: Figure length converter implementation
//
//

using MS.Internal;
using MS.Utility;
using System.ComponentModel;
using System.Windows;
using System;
using System.Security;
using System.ComponentModel.Design.Serialization;
using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using System.Windows.Markup;

namespace System.Windows
{
    /// <summary>
    /// FigureLengthConverter - Converter class for converting 
    /// instances of other types to and from FigureLength instances.
    /// </summary> 
    public class FigureLengthConverter: TypeConverter
    {
        //-------------------------------------------------------------------
        //
        //  Public Methods
        //
        //-------------------------------------------------------------------

        #region Public Methods

        /// <summary>
        /// Checks whether or not this class can convert from a given type.
        /// </summary>
        /// <param name="typeDescriptorContext">The ITypeDescriptorContext 
        /// for this call.</param>
        /// <param name="sourceType">The Type being queried for support.</param>
        /// <returns>
        /// <c>true</c> if thie converter can convert from the provided type, 
        /// <c>false</c> otherwise.
        /// </returns>
        public override bool CanConvertFrom(
            ITypeDescriptorContext typeDescriptorContext, 
            Type sourceType)
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
        /// Returns whether this class can convert specified value to <see langword="string"/> or <see cref="InstanceDescriptor"/>.
        /// </summary>
        /// <param name="typeDescriptorContext">Context information used for conversion.</param>
        /// <param name="destinationType">Type being evaluated for conversion.</param>
        /// <returns><see langword="true"/> when <paramref name="destinationType"/> specified is
        /// <see langword="string"/> or <see cref="InstanceDescriptor"/>, <see langword="false"/> otherwise.</returns>
        public override bool CanConvertTo(ITypeDescriptorContext typeDescriptorContext, Type destinationType)
        {
            return destinationType == typeof(InstanceDescriptor) || destinationType == typeof(string);
        }

        /// <summary>
        /// Attempts to convert to a FigureLength from the given object.
        /// </summary>
        /// <param name="typeDescriptorContext">The ITypeDescriptorContext for this call.</param>
        /// <param name="cultureInfo">The CultureInfo which is respected when converting.</param>
        /// <param name="source">The object to convert to a FigureLength.</param>
        /// <returns>
        /// The FigureLength instance which was constructed.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// An ArgumentNullException is thrown if the example object is null.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// An ArgumentException is thrown if the example object is not null 
        /// and is not a valid type which can be converted to a FigureLength.
        /// </exception>
        public override object ConvertFrom(ITypeDescriptorContext typeDescriptorContext, CultureInfo cultureInfo, object source)
        {
            if (source is null)
                throw GetConvertFromException(source);

            if (source is string stringValue)
                return FromString(stringValue, cultureInfo);

            // Attempt conversion from a numeric type (FigureLength.Pixel)
            return new FigureLength(Convert.ToDouble(source, cultureInfo));
        }

        /// <summary>
        /// Attempts to convert a FigureLength instance to the given type.
        /// </summary>
        /// <param name="typeDescriptorContext">The ITypeDescriptorContext for this call.</param>
        /// <param name="cultureInfo">The CultureInfo which is respected when converting.</param>
        /// <param name="value">The FigureLength to convert.</param>
        /// <param name="destinationType">The type to which to convert the FigureLength instance.</param>
        /// <returns>
        /// The object which was constructed.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// An ArgumentNullException is thrown if the example object is null.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// An ArgumentException is thrown if the object is not null and is not a FigureLength,
        /// or if the destinationType isn't one of the valid destination types.
        /// </exception>
        public override object ConvertTo(ITypeDescriptorContext typeDescriptorContext, CultureInfo cultureInfo, object value, Type destinationType)
        {
            ArgumentNullException.ThrowIfNull(destinationType);

            if (value is not FigureLength figureLength)
                throw GetConvertToException(value, destinationType);

            if (destinationType == typeof(string))
                return ToString(in figureLength, cultureInfo);

            if (destinationType == typeof(InstanceDescriptor))
            {
                ConstructorInfo ci = typeof(FigureLength).GetConstructor(new Type[] { typeof(double), typeof(FigureUnitType) });
                return new InstanceDescriptor(ci, new object[] { figureLength.Value, figureLength.FigureUnitType });
            }

            throw GetConvertToException(value, destinationType);
        }

        #endregion Public Methods

        //-------------------------------------------------------------------
        //
        //  Internal Methods
        //
        //-------------------------------------------------------------------

        #region Internal Methods

        /// <summary>
        /// Converts a <see cref="FigureLength"/> instance to a <see langword="string"/> given the <see cref="CultureInfo"/>.
        /// </summary>
        /// <param name="length">Reference to a <see cref="FigureLength"/> instance to convert from.</param>
        /// <param name="cultureInfo">The <see cref="CultureInfo"/> which is respected during conversion.</param>
        /// <returns><see langword="string"/> representation of the <see cref="FigureLength"/>.</returns>
        internal static string ToString(ref readonly FigureLength length, CultureInfo cultureInfo) => length.FigureUnitType switch
        {
            FigureUnitType.Auto => "Auto", // Print out "Auto", value is always "1.0"
            FigureUnitType.Pixel => Convert.ToString(length.Value, cultureInfo),
            _ => ToStringWithUnitType(in length, cultureInfo)
        };

        /// <summary>
        /// Converts a <see cref="FigureLength"/> instance to a <see langword="string"/> given the <see cref="CultureInfo"/>.
        /// </summary>
        /// <param name="length">Reference to a <see cref="FigureLength"/> instance to convert from.</param>
        /// <param name="cultureInfo">The <see cref="CultureInfo"/> which is respected during conversion.</param>
        /// <returns><see langword="string"/> representation of the <see cref="FigureLength"/>.</returns>
        private static string ToStringWithUnitType(ref readonly FigureLength length, CultureInfo cultureInfo)
        {
            // 17 for digits; 3 for separator; 4 for negative sign; 5 for exponent; 3 scratch space
            Span<char> doubleSpan = stackalloc char[32];
            length.Value.TryFormat(doubleSpan, out int charsWritten, provider: cultureInfo);

            return string.Create(cultureInfo, stackalloc char[48], $"{doubleSpan.Slice(0, charsWritten)} {length.FigureUnitType}");
        }

        /// <summary>
        /// Parses a <see cref="FigureLength"/> from a <see langword="string"/> given the <see cref="CultureInfo"/>.
        /// </summary>
        /// <param name="input"><see langword="string"/> to parse from.</param>
        /// <param name="cultureInfo">The <see cref="CultureInfo"/> which is respected during conversion.</param>
        /// <returns>Newly created <see cref="FigureLength"/> instance.</returns>
        /// <remarks>
        /// Formats: 
        /// "[value][unit]"
        ///     [value] is a double
        ///     [unit] is a string in FigureLength._unitTypes connected to a FigureUnitType
        /// "[value]"
        ///     As above, but the FigureUnitType is assumed to be FigureUnitType.Pixel
        /// "[unit]"
        ///     As above, but the value is assumed to be 1.0
        ///     This is only acceptable for a subset of FigureUnitType: Auto
        /// </remarks>
        internal static FigureLength FromString(string input, CultureInfo cultureInfo)
        {
            XamlFigureLengthSerializer.FromString(input, cultureInfo, out double value, out FigureUnitType unit);

            return new FigureLength(value, unit);
        }

        #endregion Internal Methods
    }
}
