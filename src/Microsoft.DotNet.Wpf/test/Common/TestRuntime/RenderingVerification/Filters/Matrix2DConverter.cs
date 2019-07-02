// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        
using System;
using System.Reflection;
using System.Globalization;
using System.ComponentModel;
using System.Text.RegularExpressions;
using Microsoft.Test.RenderingVerification;

namespace Microsoft.Test.RenderingVerification.Filters
{
    /// <summary>
    /// Summary description for NormalizedColorConverter.
    /// </summary>
    [BrowsableAttribute(false)]
    public class Matrix2DConverter: ExpandableObjectConverter
    {
        /// <summary>
        /// Returns whether this converter can convert the object to the specified type
        /// </summary>
        /// <param name="context">An ITypeDescriptorContext that provides a format context. </param>
        /// <param name="destinationType">A Type that represents the type you want to convert to.</param>
        /// <returns>true if this converter can perform the conversion; otherwise, false.</returns>
        public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType)
        {
            if (destinationType == typeof(Matrix2D) || destinationType == typeof(double[]))
            {
                return true;
            }

            return base.CanConvertTo(context, destinationType);
        }
        /// <summary>
        /// Converts the given value object to the specified type, using the specified context and culture information.
        /// </summary>
        /// <param name="context">An ITypeDescriptorContext that provides a format context.</param>
        /// <param name="culture">A CultureInfo object. If a null reference (Nothing in Visual Basic) is passed, the current culture is assumed.</param>
        /// <param name="value">The Object to convert.</param>
        /// <param name="destinationType">The Type to convert the value parameter to. </param>
        /// <returns>An Object that represents the converted value.</returns>
        public override object ConvertTo(ITypeDescriptorContext context, System.Globalization.CultureInfo culture, object value, Type destinationType)
        {
            if (destinationType == typeof(string) && (value is Matrix2D || value is double[]))
            {
                return "[ " + ((Matrix2D)value).X1 + " , " + ((Matrix2D)value).Y1 + " , " + ((Matrix2D)value).X2 + " , " + ((Matrix2D)value).Y2 + " , " + ((Matrix2D)value).T1 + " , " + ((Matrix2D)value).T2 + " ]";
            }

            return base.ConvertTo(context, culture, value, destinationType);
        }
        /// <summary>
        /// Returns whether this converter can convert an object of the given type to the type of this converter, using the specified context.
        /// </summary>
        /// <param name="context">An ITypeDescriptorContext that provides a format context. </param>
        /// <param name="sourceType">A Type that represents the type you want to convert from.</param>
        /// <returns>true if this converter can perform the conversion; otherwise, false.</returns>
        public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
        {
            if (sourceType == typeof(string))
            {
                return true;
            }

            return base.CanConvertFrom(context, sourceType);
        }
        /// <summary>
        /// Converts the given object to the type of this converter, using the specified context and culture information.
        /// </summary>
        /// <param name="context">An ITypeDescriptorContext that provides a format context.</param>
        /// <param name="culture">The CultureInfo to use as the current culture.</param>
        /// <param name="value">The Object to convert.</param>
        /// <returns>An Object that represents the converted value.</returns>
        public override object ConvertFrom(ITypeDescriptorContext context, System.Globalization.CultureInfo culture, object value)
        {
            if (value is string)
            {
                try
                {
                    Matrix2D matrix = new Matrix2D();

                    if (((string)value).Trim().ToLower() != "Identity")
                    {
                        string array = Regex.Replace((string)value, @"\[\s*(.+?)\s*,\s*(.+?)\s*,\s*(.+?)\s*,\s*(.+?)\s*,\s*(.+?)\s*,\s*(.+?)\]\s*", "$1_$2_$3_$4_$5_$6", RegexOptions.IgnoreCase); 
#if CLR_VERSION_BELOW_2
                        string[] values = array.Split(new char[] { '_' });
#else
                        string[] values = array.Split(new char[] { '_' }, StringSplitOptions.RemoveEmptyEntries);
#endif
                        if (values.Length != 6)
                        {
                            throw new FormatException("Matrix param not formatted as expected");
                        }

                        matrix.X1 = double.Parse(values[0], NumberFormatInfo.InvariantInfo);
                        matrix.Y1 = double.Parse(values[1], NumberFormatInfo.InvariantInfo);
                        matrix.X2 = double.Parse(values[2], NumberFormatInfo.InvariantInfo);
                        matrix.Y2 = double.Parse(values[3], NumberFormatInfo.InvariantInfo);
                        matrix.T1 = double.Parse(values[4], NumberFormatInfo.InvariantInfo);
                        matrix.T2 = double.Parse(values[5], NumberFormatInfo.InvariantInfo);
                    }

                    return matrix;
                }
                catch
                {
                    throw new ArgumentException("Cannot convert '" + value.ToString() + "' to type 'NormalizedColor'");
                }
            }

            return base.ConvertFrom(context, culture, value);
        }
    }
}
