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
    using System.Collections.Specialized;
    using System.ComponentModel;
    using System.Globalization;
    using System.Text;

    /// <summary>
    ///   A class used to convert a comma separated list into 
    /// </summary>
    public class StringCollectionConverter : TypeConverter
    {
        /// <summary>
        ///   Returns whether this converter can convert from a string list to a StringCollection.
        /// </summary>
        /// <param name="context">An ITypeDescriptorContext that provides the format context.</param>
        /// <param name="sourceType">A Type that represents the type you want to convert from.</param>
        /// <returns>True if this converter can perform the conversion; otherwise, false.</returns>
        public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
        {
            return (sourceType == typeof(string)) || base.CanConvertFrom(context, sourceType);
        }

        /// <summary>
        /// Returns true if this type converter can convert to the given type.
        /// </summary>
        /// <param name="context">ITypeDescriptorContext</param>
        /// <param name="destinationType">Type to convert to</param>
        /// <returns>true if conversion is possible</returns>
        public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType)
        {
            if (destinationType == typeof(string))
            {
                return true;
            }

            return base.CanConvertTo(context, destinationType);
        }

        /// <summary>
        ///   Converts the given object to a StringCollection.
        /// </summary>
        /// <param name="context">An ITypeDescriptorContext that provides a format context.</param>
        /// <param name="culture">The CultureInfo to use as the current culture.</param>
        /// <param name="value">The object to convert.</param>
        /// <returns>A StringCollection converted from value.</returns>
        public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
        {
            string valueAsStr = value as string;
            if (valueAsStr != null)
            {
                string str = valueAsStr.Trim();
                if (str.Length == 0)
                {
                    return null;
                }

                char ch = ',';
                if (culture != null)
                {
                    ch = culture.TextInfo.ListSeparator[0];
                }
                string[] strings = str.Split(ch);
                StringCollection stringCollection = new StringCollection();
                foreach (string s in strings)
                {
                    stringCollection.Add(s);
                }

                return stringCollection;
            }
            
            return base.ConvertFrom(context, culture, value);
        }

        /// <summary>
        /// TypeConverter method implementation that converts an object of type StringCollection
        /// to an InstanceDescriptor or a string.  If the source is not a StringCollection or
        /// the destination is not a string it forwards the call to
        /// TypeConverter.ConvertTo.
        /// </summary>
        /// <exception cref="NotSupportedException">
        /// A NotSupportedException is thrown if the example object is null
        /// or if the destinationType isn't one of the valid destination types.
        /// </exception>
        /// <param name="context">ITypeDescriptorContext</param>
        /// <param name="cultureInfo"> The CultureInfo which will govern this conversion. </param>
        /// <param name="value">value to convert from</param>
        /// <param name="destinationType">Type to convert to</param>
        /// <returns>converted value</returns>
        public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
        {
            if (null == value)
            {
                throw new ArgumentNullException("value");
            }

            if (null == destinationType)
            {
                throw new ArgumentNullException("destinationType");
            }

            StringCollection stringCollectionValue = value as StringCollection;
            if (stringCollectionValue != null)
            {
                if (destinationType == typeof(string))
                {
                    char ch = ',';
                    if (culture != null)
                    {
                        ch = culture.TextInfo.ListSeparator[0];
                    }
                    StringBuilder sb = new StringBuilder();
                    int count = stringCollectionValue.Count;
                    for (int i = 0; i < count; i++)
                    {
                        if (i != 0)
                        {
                            sb.Append(ch);
                        }
                        sb.Append(stringCollectionValue[i]);
                    }
                    return sb.ToString();
                }
            }

            // Pass unhandled cases to base class
            return base.ConvertTo(context, culture, value, destinationType);
        }
    }
}