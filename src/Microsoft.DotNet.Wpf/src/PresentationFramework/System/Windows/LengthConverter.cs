// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// 
//
// Description: Contains the LengthConverter: TypeConverter for the Length class.
//
//

using System;
using System.ComponentModel;

using System.ComponentModel.Design.Serialization;
using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using System.Windows;
using System.Windows.Markup;
using System.Security;
using MS.Internal;
using MS.Utility;

namespace System.Windows
{

    /// <summary>
    /// LengthConverter - Converter class for converting instances of other types to and from double representing length.
    /// </summary> 
    public class LengthConverter: TypeConverter
    {
        //-------------------------------------------------------------------
        //
        //  Public Methods
        //
        //-------------------------------------------------------------------

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
            if (destinationType == typeof(InstanceDescriptor) ||
                destinationType == typeof(string)) 
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// ConvertFrom - Attempt to convert to a length from the given object
        /// </summary>
        /// <returns>
        /// The double representing the size in 1/96th of an inch.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// An ArgumentNullException is thrown if the example object is null.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// An ArgumentException is thrown if the example object is not null and is not a valid type
        /// which can be converted to a double.
        /// </exception>
        /// <param name="typeDescriptorContext"> The ITypeDescriptorContext for this call. </param>
        /// <param name="cultureInfo"> The CultureInfo which is respected when converting. </param>
        /// <param name="source"> The object to convert to a double. </param>
        public override object ConvertFrom(ITypeDescriptorContext typeDescriptorContext, 
                                           CultureInfo cultureInfo, 
                                           object source)
        {
            if (source != null)
            {
                if (source is string) { return FromString((string)source, cultureInfo); }
                else                  { return (double)(Convert.ToDouble(source, cultureInfo)); }
            }

            throw GetConvertFromException(source);
        }

        /// <summary>
        /// ConvertTo - Attempt to convert a double to the given type
        /// </summary>
        /// <returns>
        /// The object which was constructed.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// An ArgumentNullException is thrown if the example object is null.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// An ArgumentException is thrown if the object is not null,
        /// or if the destinationType isn't one of the valid destination types.
        /// </exception>
        /// <param name="typeDescriptorContext"> The ITypeDescriptorContext for this call. </param>
        /// <param name="cultureInfo"> The CultureInfo which is respected when converting. </param>
        /// <param name="value"> The double to convert. </param>
        /// <param name="destinationType">The type to which to convert the double. </param>
        public override object ConvertTo(ITypeDescriptorContext typeDescriptorContext, 
                                         CultureInfo cultureInfo,
                                         object value,
                                         Type destinationType)
        {
            ArgumentNullException.ThrowIfNull(destinationType);

            if (    value != null
                &&  value is double )
            {
                double l = (double)value;
                if (destinationType == typeof(string)) 
                { 
                    if(double.IsNaN(l)) 
                        return "Auto";
                    else 
                        return Convert.ToString(l, cultureInfo); 
                }
                else if (destinationType == typeof(InstanceDescriptor))
                {
                    ConstructorInfo ci = typeof(double).GetConstructor(new Type[] { typeof(double) });
                    return new InstanceDescriptor(ci, new object[] { l });
                }
            }
            throw GetConvertToException(value, destinationType);
        }
        #endregion 

        //-------------------------------------------------------------------
        //
        //  Internal Methods
        //
        //-------------------------------------------------------------------

        #region Internal Methods


        // Parse a Length from a string given the CultureInfo.
        // Formats: 
        //"[value][unit]"
        //   [value] is a double
        //   [unit] is a string specifying the unit, like 'in' or 'px', or nothing (means pixels)
        // NOTE - This code is called from FontSizeConverter, so changes will affect both.
        static internal double FromString(string s, CultureInfo cultureInfo)
        {
            ReadOnlySpan<char> valueSpan = s.AsSpan().Trim();
            double unitFactor = 1.0;

            //Auto is represented and Double.NaN
            //properties that do not want Auto and NaN to be in their ligit values,
            //should disallow NaN in validation callbacks (same goes for negative values)
            if (valueSpan.Equals("auto", StringComparison.OrdinalIgnoreCase))
                return Double.NaN;

            PixelUnit pixelUnit;
            if (PixelUnit.TryParsePixel(valueSpan, out pixelUnit)
                || PixelUnit.TryParsePixelPerInch(valueSpan, out pixelUnit)
                || PixelUnit.TryParsePixelPerCentimeter(valueSpan, out pixelUnit)
                || PixelUnit.TryParsePixelPerPoint(valueSpan, out pixelUnit))
            {
                valueSpan = valueSpan.Slice(0, valueSpan.Length - pixelUnit.Name.Length);
                unitFactor = pixelUnit.Factor;
            }

            if (valueSpan.IsEmpty)
                return 0;

            return ParseDouble(valueSpan, cultureInfo) * unitFactor;
        }

        private static double ParseDouble(ReadOnlySpan<char> span, CultureInfo cultureInfo)
        {
            // FormatException errors thrown by double.Parse are pretty uninformative.
            // Throw a more meaningful error in this case that tells that we were attempting
            // to create a Length instance from a string.  This addresses windows bug 968884
            try
            {
                return double.Parse(span, cultureInfo);
            }
            catch (FormatException)
            {
                throw new FormatException(SR.Format(SR.LengthFormatError, span.ToString()));
            }
        }


        #endregion

    }
}
