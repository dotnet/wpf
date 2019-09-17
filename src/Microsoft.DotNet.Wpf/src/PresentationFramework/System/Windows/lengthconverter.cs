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
            if (destinationType == null)
            {
                throw new ArgumentNullException("destinationType");
            }

            if (    value != null
                &&  value is double )
            {
                double l = (double)value;
                if (destinationType == typeof(string)) 
                { 
                    if(DoubleUtil.IsNaN(l)) 
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
            string valueString = s.Trim();
            string goodString = valueString.ToLowerInvariant();
            int strLen = goodString.Length;
            int strLenUnit = 0;
            double unitFactor = 1.0;

            //Auto is represented and Double.NaN
            //properties that do not want Auto and NaN to be in their ligit values,
            //should disallow NaN in validation callbacks (same goes for negative values)
            if (goodString == "auto") return Double.NaN;

            for (int i = 0; i < PixelUnitStrings.Length; i++)
            {
                // NOTE: This is NOT a culture specific comparison.
                // This is by design: we want the same unit string table to work across all cultures.
                if (goodString.EndsWith(PixelUnitStrings[i], StringComparison.Ordinal))
                {
                    strLenUnit = PixelUnitStrings[i].Length;
                    unitFactor = PixelUnitFactors[i];
                    break;
                }
            }

            //  important to substring original non-lowered string 
            //  this allows case sensitive ToDouble below handle "NaN" and "Infinity" correctly. 
            //  this addresses windows bug 1177408
            valueString = valueString.Substring(0, strLen - strLenUnit);

            // FormatException errors thrown by Convert.ToDouble are pretty uninformative.
            // Throw a more meaningful error in this case that tells that we were attempting
            // to create a Length instance from a string.  This addresses windows bug 968884
            try
            {
                double result = Convert.ToDouble(valueString, cultureInfo) * unitFactor;
                return result;
            }
            catch (FormatException)
            {
                throw new FormatException(SR.Get(SRID.LengthFormatError, valueString));
            }
        }

        // This array contains strings for unit types 
        // These are effectively "TypeConverter only" units.
        // They are all expressable in terms of the Pixel unit type and a conversion factor.
        static private string[] PixelUnitStrings = { "px", "in", "cm", "pt" };
        static private double[] PixelUnitFactors = 
        { 
            1.0,              // Pixel itself
            96.0,             // Pixels per Inch
            96.0 / 2.54,      // Pixels per Centimeter
            96.0 / 72.0,      // Pixels per Point
        };

        static internal string ToString(double l, CultureInfo cultureInfo)
        {
            if(DoubleUtil.IsNaN(l)) return "Auto";
            return Convert.ToString(l, cultureInfo);
        }

        #endregion

    }
}
