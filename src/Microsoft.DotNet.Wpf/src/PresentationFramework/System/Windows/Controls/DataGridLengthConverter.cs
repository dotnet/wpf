// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


using System;
using System.ComponentModel;
using System.ComponentModel.Design.Serialization;
using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using System.Security;
using System.Windows;
using MS.Internal;

namespace System.Windows.Controls
{
    /// <summary>
    ///     Converts instances of various types to and from DataGridLength.
    /// </summary> 
    public class DataGridLengthConverter : TypeConverter
    {
        /// <summary>
        ///     Checks whether or not this class can convert from a given type.
        /// </summary>
        public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
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
                case TypeCode.Byte:
                    return true;

                default:
                    return false;
            }
        }

        /// <summary>
        ///     Checks whether or not this class can convert to a given type.
        /// </summary>
        public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType)
        {
            return (destinationType == typeof(string)) || (destinationType == typeof(InstanceDescriptor));
        }

        /// <summary>
        ///     Attempts to convert to a DataGridLength from the given object.
        /// </summary>
        /// <param name="context">The ITypeDescriptorContext for this call.</param>
        /// <param name="culture">The CultureInfo which is respected when converting.</param>
        /// <param name="value">The object to convert to a DataGridLength.</param>
        /// <returns>The DataGridLength instance which was constructed.</returns>
        /// <exception cref="ArgumentNullException">
        ///     An ArgumentNullException is thrown if the source is null.
        /// </exception>
        /// <exception cref="ArgumentException">
        ///     An ArgumentException is thrown if the source is not null 
        ///     and is not a valid type which can be converted to a DataGridLength.
        /// </exception>
        public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
        {
            if (value != null)
            {
                string stringSource = value as string;
                if (stringSource != null)
                {
                    // Convert from string
                    return ConvertFromString(stringSource, culture);
                }
                else
                {
                    // Conversion from numeric type
                    DataGridLengthUnitType type;
                    double doubleValue = Convert.ToDouble(value, culture);

                    if (DoubleUtil.IsNaN(doubleValue))
                    {
                        // This allows for conversion from Width / Height = "Auto" 
                        doubleValue = 1.0;
                        type = DataGridLengthUnitType.Auto;
                    }
                    else
                    {
                        type = DataGridLengthUnitType.Pixel;
                    }

                    if (!Double.IsInfinity(doubleValue))
                    {
                        return new DataGridLength(doubleValue, type);
                    }
                }
            }

            // The default exception to throw in ConvertFrom
            throw GetConvertFromException(value);
        }

        /// <summary>
        ///     Attempts to convert a DataGridLength instance to the given type.
        /// </summary>
        /// <param name="context">The ITypeDescriptorContext for this call.</param>
        /// <param name="culture">The CultureInfo which is respected when converting.</param>
        /// <param name="value">The DataGridLength to convert.</param>
        /// <param name="destinationType">The type to which to convert the DataGridLength instance.</param>
        /// <returns>
        ///     The object which was constructed.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///     An ArgumentNullException is thrown if the value is null.
        /// </exception>
        /// <exception cref="ArgumentException">
        ///     An ArgumentException is thrown if the value is not null and is not a DataGridLength,
        ///     or if the destinationType isn't one of the valid destination types.
        /// </exception>
        public override object ConvertTo(
            ITypeDescriptorContext context,
            CultureInfo culture,
            object value,
            Type destinationType)
        {
            if (destinationType == null)
            {
                throw new ArgumentNullException("destinationType");
            }

            if ((value != null) && (value is DataGridLength))
            {
                DataGridLength length = (DataGridLength)value;

                if (destinationType == typeof(string))
                {
                    return ConvertToString(length, culture);
                }

                if (destinationType == typeof(InstanceDescriptor))
                {
                    ConstructorInfo ci = typeof(DataGridLength).GetConstructor(new Type[] { typeof(double), typeof(DataGridLengthUnitType) });
                    return new InstanceDescriptor(ci, new object[] { length.Value, length.UnitType });
                }
            }

            // The default exception to throw from ConvertTo
            throw GetConvertToException(value, destinationType);
        }

        /// <summary>
        ///     Converts a DataGridLength instance to a String given the CultureInfo.
        /// </summary>
        /// <param name="gl">DataGridLength instance to convert.</param>
        /// <param name="cultureInfo">The culture to use.</param>
        /// <returns>String representation of the object.</returns>
        internal static string ConvertToString(DataGridLength length, CultureInfo cultureInfo)
        {
            switch (length.UnitType)
            {
                case DataGridLengthUnitType.Auto:
                case DataGridLengthUnitType.SizeToCells:
                case DataGridLengthUnitType.SizeToHeader:
                    return length.UnitType.ToString();

                // Star has one special case when value is "1.0" in which the value can be dropped.
                case DataGridLengthUnitType.Star:
                    return DoubleUtil.IsOne(length.Value) ? "*" : Convert.ToString(length.Value, cultureInfo) + "*";

                // Print out the numeric value. "px" can be omitted.
                default:
                    return Convert.ToString(length.Value, cultureInfo);
            }
        }

        /// <summary>
        ///     Parses a DataGridLength from a string given the CultureInfo.
        /// </summary>
        /// <param name="s">String to parse from.</param>
        /// <param name="cultureInfo">Culture Info.</param>
        /// <returns>Newly created DataGridLength instance.</returns>
        /// <remarks>
        /// Formats: 
        /// "[value][unit]"
        ///     [value] is a double
        ///     [unit] is a string in DataGridLength._unitTypes connected to a DataGridLengthUnitType
        /// "[value]"
        ///     As above, but the DataGridLengthUnitType is assumed to be DataGridLengthUnitType.Pixel
        /// "[unit]"
        ///     As above, but the value is assumed to be 1.0
        ///     This is only acceptable for a subset of DataGridLengthUnitType: Auto
        /// </remarks>
        private static DataGridLength ConvertFromString(string s, CultureInfo cultureInfo)
        {
            string goodString = s.Trim().ToLowerInvariant();

            // Check if the string matches any of the descriptive unit types.
            // In these cases, there is no need to parse a value.
            for (int i = 0; i < NumDescriptiveUnits; i++)
            {
                string unitString = _unitStrings[i];
                if (goodString == unitString)
                {
                    return new DataGridLength(1.0, (DataGridLengthUnitType)i);
                }
            }

            double value = 0.0;
            DataGridLengthUnitType unit = DataGridLengthUnitType.Pixel;
            int strLen = goodString.Length;
            int strLenUnit = 0;
            double unitFactor = 1.0;

            // Check if the string contains a non-descriptive unit at the end.
            int numUnitStrings = _unitStrings.Length;
            for (int i = NumDescriptiveUnits; i < numUnitStrings; i++)
            {
                string unitString = _unitStrings[i];

                // Note: This is NOT a culture specific comparison.
                // This is by design: we want the same unit string table to work across all cultures.
                if (goodString.EndsWith(unitString, StringComparison.Ordinal))
                {
                    strLenUnit = unitString.Length;
                    unit = (DataGridLengthUnitType)i;
                    break;
                }
            }

            // Couldn't match a standard unit type, try a non-standard unit type.
            if (strLenUnit == 0)
            {
                numUnitStrings = _nonStandardUnitStrings.Length;
                for (int i = 0; i < numUnitStrings; i++)
                {
                    string unitString = _nonStandardUnitStrings[i];

                    // Note: This is NOT a culture specific comparison.
                    // This is by design: we want the same unit string table to work across all cultures.
                    if (goodString.EndsWith(unitString, StringComparison.Ordinal))
                    {
                        strLenUnit = unitString.Length;
                        unitFactor = _pixelUnitFactors[i];
                        break;
                    }
                }
            }

            // Check if there is a numerical value to parse
            if (strLen == strLenUnit)
            {
                // There is no numerical value to parse
                if (unit == DataGridLengthUnitType.Star)
                {
                    // Star's value defaults to 1. Anyone else would be 0.
                    value = 1.0;
                }
            }
            else
            {
                // Parse a numerical value
                Debug.Assert(
                    (unit == DataGridLengthUnitType.Pixel) || DoubleUtil.AreClose(unitFactor, 1.0),
                    "unitFactor should not be other than 1.0 unless the unit type is Pixel.");

                string valueString = goodString.Substring(0, strLen - strLenUnit);
                value = Convert.ToDouble(valueString, cultureInfo) * unitFactor;
            }

            return new DataGridLength(value, unit);
        }

        private static string[] _unitStrings = { "auto", "px", "sizetocells", "sizetoheader", "*" };
        private const int NumDescriptiveUnits = 3;

        // This array contains strings for unit types not included in the standard set
        private static string[] _nonStandardUnitStrings = { "in", "cm", "pt" };

        // These are conversion factors to transform other units to pixels
        private static double[] _pixelUnitFactors = 
        { 
            96.0,             // Pixels per Inch
            96.0 / 2.54,      // Pixels per Centimeter
            96.0 / 72.0,      // Pixels per Point 
        };
    }
}
