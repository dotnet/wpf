// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel;
using System.Windows.Controls;
using System.Windows.Markup;
using System.Globalization;
using System.Text;
using MS.Internal;

namespace System.Windows.Controls
{
    internal sealed class ColumnDefinitionCollectionConverter : TypeConverter
    {
        /// <summary>
        /// CanConvertFrom - Returns whether or not this class can convert from a given type.
        /// </summary>
        /// <returns>
        /// bool - True if this converter can convert from the provided type, false if not.
        /// </returns>
        /// <param name="typeDescriptorContext"> The ITypeDescriptorContext for this call. </param>
        /// <param name="sourceType"> The Type being queried for support. </param>
        public override bool CanConvertFrom(ITypeDescriptorContext typeDescriptorContext, Type sourceType)
        {
            return sourceType == typeof(string);
        }

        /// <summary>
        /// ConvertFrom - Attempt to convert to a ColumnDefinitionCollection from the given object.
        /// </summary>
        /// <returns>
        /// The object which was constructed.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// An ArgumentNullException is thrown if the example object is null.
        /// </exception>
        /// <param name="typeDescriptorContext"> The ITypeDescriptorContext for this call. </param>
        /// <param name="cultureInfo"> The CultureInfo which is respected when converting. </param>
        /// <param name="value"> The Thickness to convert. </param>
        public override object ConvertFrom(ITypeDescriptorContext typeDescriptorContext, CultureInfo cultureInfo, object value)
        {
            if (value is string input)
            {
                ColumnDefinitionCollection collection = new ColumnDefinitionCollection(); 

                TokenizerHelper th = new TokenizerHelper(input, cultureInfo);
                while (th.NextToken())
                {
                    collection.Add(new ColumnDefinition { Width = GridLengthConverter.FromString(th.GetCurrentToken(), cultureInfo) });
                }

                return collection;
            }
            
            throw GetConvertFromException(value);
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
            // Implementing this function can lead to potential data loss if the given ColumnDefinition contains MinWidth and MaxWidth.
            // Returning false to avoid serialization issues with the XAML object, as the default CanConvertTo method returns true when destinationType is string
            return false;
        }
    }
}