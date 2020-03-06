// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        

#if RIBBON_IN_FRAMEWORK
namespace System.Windows.Controls.Ribbon
#else
namespace Microsoft.Windows.Controls.Ribbon
#endif
{
    using System;
    using System.ComponentModel;
    using System.Globalization;
    using MS.Internal;
    using System.Diagnostics;
    using System.ComponentModel.Design.Serialization;
    using System.Reflection;

    /// <summary>
    ///   A class used for converting between RibbonControlLengths and strings/numbers.
    /// </summary>
    public class RibbonControlLengthConverter : TypeConverter
    {
        #region Private Fields

        // Note: keep this array in sync with the RibbonControlLengthUnitType enum
        private static string[] _unitStrings = { "auto", "px", "items", "*" };

        //  this array contains strings for unit types that are not present in the RibbonControlLengthUnitType enum
        static private string[] _pixelUnitStrings = { "in", "cm", "pt" };
        static private double[] _pixelUnitFactors = 
        { 
            96.0,             // Pixels per Inch
            96.0 / 2.54,      // Pixels per Centimeter
            96.0 / 72.0,      // Pixels per Point
        };

        #endregion

        #region Public Methods

        /// <summary>
        ///   Checks whether or not this class can convert from a given type.
        /// </summary>
        public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
        {
            // We can only handle strings, integral and floating types.
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
        ///   Checks whether or not this class can convert to a given type.
        /// </summary>
        public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType)
        {
            return (destinationType == typeof(InstanceDescriptor) || destinationType == typeof(string));
        }

        /// <summary>
        ///   Attempts to convert to a RibbonControlLength from the given object.
        /// </summary>
        public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
        {
            if (value != null)
            {
                string stringValue = value as string;
                if (stringValue != null)
                {
                    return FromString(stringValue, culture);
                }
                else
                {
                    //  conversion from numeric type
                    double doubleValue;
                    RibbonControlLengthUnitType type;

                    doubleValue = Convert.ToDouble(value, culture);

                    if (DoubleUtil.IsNaN(doubleValue))
                    {
                        //  this allows for conversion from Width / Height = "Auto" 
                        doubleValue = 1.0;
                        type = RibbonControlLengthUnitType.Auto;
                    }
                    else
                    {
                        type = RibbonControlLengthUnitType.Pixel;
                    }

                    return new RibbonControlLength(doubleValue, type);
                }
            }

            throw GetConvertFromException(value);
        }

        /// <summary>
        ///   Attempts to convert a RibbonControlLength instance to the given type.
        /// </summary>
        public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
        {
            if (destinationType == null)
            {
                throw new ArgumentNullException("destinationType");
            }

            if (value != null && value is RibbonControlLength)
            {
                RibbonControlLength length = (RibbonControlLength)value;

                if (destinationType == typeof(string))
                {
                    return ToString(length, culture);
                }

                if (destinationType == typeof(InstanceDescriptor))
                {
                    ConstructorInfo ci = typeof(RibbonControlLength).GetConstructor(new Type[] { typeof(double), typeof(RibbonControlLengthUnitType) });
                    return new InstanceDescriptor(ci, new object[] { length.Value, length.RibbonControlLengthUnitType });
                }
            }

            throw GetConvertToException(value, destinationType);
        }

        #endregion

        #region Internal Methods

        /// <summary>
        ///   Converts a RibbonControlLength instance to a String given the CultureInfo.
        /// </summary>
        internal static string ToString(RibbonControlLength length, CultureInfo cultureInfo)
        {
            switch (length.RibbonControlLengthUnitType)
            {
                //  for Auto print out "Auto". value is always "1.0"
                case RibbonControlLengthUnitType.Auto:
                    return "Auto";
                
                case RibbonControlLengthUnitType.Item:
                    return Convert.ToString(length.Value, cultureInfo) + "items";

                //  Star has one special case when value is "1.0".
                //  in this case drop value part and print only "Star"
                case RibbonControlLengthUnitType.Star:
                    return (DoubleUtil.IsOne(length.Value) ? "*" : Convert.ToString(length.Value, cultureInfo) + "*");

                //  for Pixel print out the numeric value. "px" can be omitted.
                default:
                    return Convert.ToString(length.Value, cultureInfo);
            }
        }

        /// <summary>
        ///   Parses a RibbonControlLength from a string given the CultureInfo.
        /// </summary>
        internal static RibbonControlLength FromString(string s, CultureInfo cultureInfo)
        {
            string goodString = s.Trim().ToLowerInvariant();

            double value = 0.0;
            RibbonControlLengthUnitType unit = RibbonControlLengthUnitType.Pixel;

            int i;
            int strLen = goodString.Length;
            int strLenUnit = 0;
            double unitFactor = 1.0;

            //  this is where we would handle trailing whitespace on the input string.
            //  peel [unit] off the end of the string
            i = 0;

            if (goodString == _unitStrings[i])
            {
                strLenUnit = _unitStrings[i].Length;
                unit = (RibbonControlLengthUnitType)i;
            }
            else
            {
                for (i = 1; i < _unitStrings.Length; ++i)
                {
                    //  Note: this is NOT a culture specific comparison.
                    //  this is by design: we want the same unit string table to work across all cultures.
                    if (goodString.EndsWith(_unitStrings[i], StringComparison.Ordinal))
                    {
                        strLenUnit = _unitStrings[i].Length;
                        unit = (RibbonControlLengthUnitType)i;
                        break;
                    }
                }
            }

            //  we couldn't match a real unit from RibbonControlLengthUnitTypes.
            //  try again with a converter-only unit (a pixel equivalent).
            if (i >= _unitStrings.Length)
            {
                for (i = 0; i < _pixelUnitStrings.Length; ++i)
                {
                    //  Note: this is NOT a culture specific comparison.
                    //  this is by design: we want the same unit string table to work across all cultures.
                    if (goodString.EndsWith(_pixelUnitStrings[i], StringComparison.Ordinal))
                    {
                        strLenUnit = _pixelUnitStrings[i].Length;
                        unitFactor = _pixelUnitFactors[i];
                        break;
                    }
                }
            }

            //  this is where we would handle leading whitespace on the input string.
            //  this is also where we would handle whitespace between [value] and [unit].
            //  check if we don't have a [value].  This is acceptable for certain UnitTypes.
            if (strLen == strLenUnit && (unit == RibbonControlLengthUnitType.Auto || unit == RibbonControlLengthUnitType.Star))
            {
                value = 1;
            }
            //  we have a value to parse.
            else
            {
                Debug.Assert(unit == RibbonControlLengthUnitType.Pixel || unit == RibbonControlLengthUnitType.Item ||
                    DoubleUtil.AreClose(unitFactor, 1.0));

                string valueString = goodString.Substring(0, strLen - strLenUnit);
                value = Convert.ToDouble(valueString, cultureInfo) * unitFactor;
            }

            return new RibbonControlLength(value, unit);
        }

        #endregion
    }
}
