// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.ComponentModel;
using System.Windows.Controls;
using System.Windows.Markup;
using System.Globalization;
using System.Text;
using MS.Internal;

namespace System.Windows.Controls
{
    internal sealed class RowDefinitionCollectionConverter : TypeConverter
    {
        #region Public Methods

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
        /// CanConvertTo - Returns whether or not this class can convert to a given type.
        /// </summary>
        /// <returns>
        /// bool - True if this converter can convert to the provided type, false if not.
        /// </returns>
        /// <param name="typeDescriptorContext"> The ITypeDescriptorContext for this call. </param>
        /// <param name="destinationType"> The Type being queried for support. </param>
        public override bool CanConvertTo(ITypeDescriptorContext typeDescriptorContext, Type destinationType)
        {
            return destinationType == typeof(string);
        }

        /// <summary>
        /// ConvertFrom - Attempt to convert to a RowDefinitionCollection from the given object.
        /// </summary>
        /// <returns>
        /// The object which was constructed.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// An ArgumentNullException is thrown if the example object is null.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// An ArgumentException is thrown if the object is not null and is not a valid type,
        /// or if the destinationType isn't one of the valid destination types.
        /// </exception>
        /// <param name="typeDescriptorContext"> The ITypeDescriptorContext for this call. </param>
        /// <param name="cultureInfo"> The CultureInfo which is respected when converting. </param>
        /// <param name="value"> The Thickness to convert. </param>
        public override object ConvertFrom(ITypeDescriptorContext typeDescriptorContext, CultureInfo cultureInfo, object value)
        {
            if (value != null && value is string input)
            {
                IProvideValueTarget ipvt = typeDescriptorContext?.GetService(typeof(IProvideValueTarget)) as IProvideValueTarget;
                Grid grid = ipvt?.TargetObject as Grid;
                if (grid != null)
                {
                    RowDefinitionCollection collection = new RowDefinitionCollection(grid); // Pass Grid instance

                    TokenizerHelper th = new TokenizerHelper(input, cultureInfo);
                    while (th.NextToken())
                    {
                        collection.Add(new RowDefinition { Height = GridLengthConverter.FromString(th.GetCurrentToken(), cultureInfo) });
                    }

                    return collection;
                }
            }
            throw GetConvertFromException(value);
        }

        /// <summary>
        /// ConvertTo - Attempt to convert a RowDefinitionCollection to the given type
        /// </summary>
        /// <returns>
        /// The object which was constructed.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// An ArgumentNullException is thrown if the example object is null.
        /// </exception>
        /// <param name="context"> The ITypeDescriptorContext for this call. </param>
        /// <param name="culture"> The CultureInfo which is respected when converting. </param>
        /// <param name="value"> The RowDefinitionCollection to convert. </param>
        /// <param name="destinationType">The type to which to convert the RowDefinitionCollection instance. </param>
        public override object ConvertTo(ITypeDescriptorContext typeDescriptorContext, CultureInfo cultureInfo, object value, Type destinationType)
        {
            ArgumentNullException.ThrowIfNull(value);
            ArgumentNullException.ThrowIfNull(destinationType);
            if (destinationType == typeof(string) && value is RowDefinitionCollection rowDefinitions)
            {
                char listSeparator = TokenizerHelper.GetNumericListSeparator(cultureInfo);
                StringBuilder sb = new StringBuilder();

                for (int i = 0; i < rowDefinitions.Count; i++)
                {
                    if (i > 0)
                    {
                        sb.Append(listSeparator);
                    }
                    sb.Append(GridLengthConverter.ToString(rowDefinitions[i].Height, cultureInfo));
                }

                return sb.ToString();
            }

            throw GetConvertToException(value, destinationType);
        }

        #endregion Public Methods
    }
}