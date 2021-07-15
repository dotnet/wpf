// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// 
//
// Description: Converter for FontSize to Double
//
using System;
using System.IO;
using System.Diagnostics;
using System.ComponentModel;

using System.ComponentModel.Design.Serialization;
using System.Reflection;                // for ConstructorInfo
using System.Globalization;             // for CultureInfo
using MS.Utility;                       // For ExceptionStringTable
using System.Windows.Media;           // for Typeface
using System.Windows.Controls;        // for ReadingPanel
using System.Windows.Documents;
using  System.Windows.Markup;
using MS.Internal;
using MS.Internal.Text;
using MS.Internal.Documents;

namespace System.Windows
{
    /// <summary>
    /// TypeConverter to convert FontSize to/from other types.
    /// Currently: string, int, float, and double are supported.
    /// </summary>
    //  Used by Parser to parse BoxUnits from markup. 
    public class FontSizeConverter : TypeConverter
    {
        /// <summary>
        /// TypeConverter method override.
        /// </summary>
        /// <param name="context">ITypeDescriptorContext</param>
        /// <param name="sourceType">Type to convert from</param>
        /// <returns>true if conversion is possible</returns>
        public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
        {
            return (sourceType == typeof(string) 
                || sourceType == typeof(int) 
                || sourceType == typeof(float) 
                || sourceType == typeof(double));
        }

        /// <summary>
        /// TypeConverter method override.
        /// </summary>
        /// <param name="context">ITypeDescriptorContext</param>
        /// <param name="destinationType">Type to convert to</param>
        /// <returns>true if conversion is possible</returns>
        public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType)
        {
            if (destinationType == typeof(InstanceDescriptor))
            {
                return true;
            }

            return base.CanConvertTo(context, destinationType);
        }

        /// <summary>
        /// TypeConverter method implementation.
        /// </summary>
        /// <param name="context">ITypeDescriptorContext</param>
        /// <param name="culture">Current culture (see CLR specs)</param>
        /// <param name="value">value to convert from</param>
        /// <returns>value that is result of conversion</returns>
        public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
        {
            if (value == null)
            {
                throw GetConvertFromException(value);
            }

            // NB: The culture passed from Avalon parser should always be the invariant culture.
            //     Do we care about usage in other circumstances?
            string text = value as string;

            if (text != null)
            {
                double       amount;

                FromString(text, culture,
                    out amount);
                return amount;
            }

            if (value is System.Int32 || value is System.Single || value is System.Double)
            {
                return (double)value;
            }

         // Can't convert, wrong type
            return null;
        }

        /// <summary>
        /// TypeConverter method implementation.
        /// </summary>
        /// <param name="context">ITypeDescriptorContext</param>
        /// <param name="culture">current culture (see CLR specs)</param>
        /// <param name="value">value to convert from</param>
        /// <param name="destinationType">Type to convert to</param>
        /// <returns>converted value</returns>
        public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
        {
            if (destinationType == null)
            {
                throw new ArgumentNullException("destinationType");
            }

            double fs = (double)value;

            if (destinationType == typeof(string))
            {
                return fs.ToString(culture);
            }

            if (destinationType == typeof(int))
            {
                return (int)fs;
            }

            if (destinationType == typeof(float))
            {
                return (float)fs;
            }

            if (destinationType == typeof(double))
            {
                return fs;
            }

            return base.ConvertTo(context, culture, value, destinationType);
        }


        ///<summary>
        /// Convert a string into an amount and a font size type that can be used to create
        /// and instance of a Fontsize, or to serialize in a more compact representation to
        /// a baml stream.
        ///</summary>
        internal static void FromString(
                string       text, 
                CultureInfo  culture,
            out double       amount)
        {
            amount = LengthConverter.FromString(text, culture);
        }
    }
}
