// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.ComponentModel;
using System.Windows.Controls;
using System.Windows.Markup;
using System.Globalization;
using MS.Internal;

#pragma warning disable 1634, 1691  // suppressing PreSharp warnings

namespace System.Windows.Controls
{
    internal class RowDefinitionCollectionConverter : TypeConverter
    {
        #region Public Methods

        /// <summary>
        /// CanConvertFrom - Returns whether or not this class can convert from a given type.
        /// </summary>
        /// <returns>
        /// bool - True if thie converter can convert from the provided type, false if not.
        /// </returns>
        /// <param name="context"> The ITypeDescriptorContext for this call. </param>
        /// <param name="sourceType"> The Type being queried for support. </param>
        public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
        {
            return sourceType == typeof(string) || base.CanConvertFrom(context, sourceType);
        }

        /// <summary>
        /// CanConvertTo - Returns whether or not this class can convert to a given type.
        /// </summary>
        /// <returns>
        /// bool - True if this converter can convert to the provided type, false if not.
        /// </returns>
        /// <param name="context"> The ITypeDescriptorContext for this call. </param>
        /// <param name="destinationType"> The Type being queried for support. </param>
        public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType)
        {
            return destinationType == typeof(string) || base.CanConvertTo(context, destinationType);
        }

        /// <summary>
        /// ConvertFrom - Attempt to convert to a RowDefinitionCollection from the given object.
        /// </summary>
        /// <returns>
        /// The object which was constructoed.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// An ArgumentNullException is thrown if the example object is null.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// An ArgumentException is thrown if the object is not null and is not a valid type,
        /// or if the destinationType isn't one of the valid destination types.
        /// </exception>
        /// <param name="context"> The ITypeDescriptorContext for this call. </param>
        /// <param name="culture"> The CultureInfo which is respected when converting. </param>
        /// <param name="value"> The Thickness to convert. </param>
        public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
        {
            if(value != null && value is string input){
                IProvideValueTarget ipvt = context?.GetService(typeof(IProvideValueTarget)) as IProvideValueTarget;
                Grid grid = ipvt?.TargetObject as Grid;
                if(grid != null)
                {
                    var collection = new RowDefinitionCollection(grid); // Pass Grid instance
                    var converter = new GridLengthConverter();

                    TokenizerHelper th = new TokenizerHelper(input, culture);
                    while(th.NextToken())
                    {
                        collection.Add(new RowDefinition { Height = (GridLength)converter.ConvertFromString(th.GetCurrentToken()) });
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
        /// The object which was constructoed.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// An ArgumentNullException is thrown if the example object is null.
        /// </exception>
        /// <param name="context"> The ITypeDescriptorContext for this call. </param>
        /// <param name="culture"> The CultureInfo which is respected when converting. </param>
        /// <param name="value"> The RowDefintionCollection to convert. </param>
        /// <param name="destinationType">The type to which to convert the RowDefintionCollection instance. </param>
        public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
        {
            ArgumentNullException.ThrowIfNull(value);
            ArgumentNullException.ThrowIfNull(destinationType);
            if (destinationType == typeof(string) && value is RowDefinitionCollection RowDefinitions)
            {
                var parts = new string[RowDefinitions.Count];

                for (int i = 0; i < RowDefinitions.Count; i++)
                {
                    parts[i] = RowDefinitions[i].Height.ToString();
                }

                return string.Join(",", parts);
            }

            return base.ConvertTo(context, culture, value, destinationType);
        }

        #endregion Public Methods
    }
}