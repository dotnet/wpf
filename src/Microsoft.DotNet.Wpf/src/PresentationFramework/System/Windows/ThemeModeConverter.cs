// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
using System.Diagnostics.CodeAnalysis;


namespace System.Windows
{
    /// <summary>
    /// Converts instances of other types to and from ThemeMode instances.
    /// </summary>
    [Experimental("WPF0001")]
    public class ThemeModeConverter: TypeConverter
    {

        #region Public Methods

        /// <summary>
        /// Determines whether or not this class can convert from a given type.
        /// </summary>
        /// <param name="typeDescriptorContext">The ITypeDescriptorContext for this call.</param>
        /// <param name="sourceType">The Type being queried for support.</param>
        /// <returns>
        /// <see langword="true" /> if the converter can convert from the provided type; otherwise, <see langword="false" />.
        /// </returns>
        public override bool CanConvertFrom(ITypeDescriptorContext typeDescriptorContext, Type sourceType)
        {
           return Type.GetTypeCode(sourceType) == TypeCode.String;
        }

        /// <summary>
        /// Determines whether or not this class can convert to a given type.
        /// </summary>
        /// <param name="typeDescriptorContext"> The ITypeDescriptorContext for this call. </param>
        /// <param name="destinationType"> The Type being queried for support. </param>
        /// <returns>
        /// <see langword="true" /> if this converter can convert to the provided type; otherwise, <see langword="false" />.
        /// </returns>
        public override bool CanConvertTo(ITypeDescriptorContext typeDescriptorContext, Type destinationType) 
        {
            // We can convert to an InstanceDescriptor or to a string.
            return destinationType == typeof(InstanceDescriptor) || destinationType == typeof(string);
        }

        /// <summary>
        /// Attempts to convert to a ThemeMode from the specified object
        /// </summary>
        /// <exception cref="ArgumentNullException">
        /// The example object is <see langword="null" />.
        /// </exception>
        /// <param name="typeDescriptorContext">The ITypeDescriptorContext for this call.</param>
        /// <param name="cultureInfo">The CultureInfo which is respected when converting.</param>
        /// <param name="source">The object to convert to a ThemeMode.</param>
        /// <returns>
        /// The new ThemeMode instance.
        /// </returns>
        public override object ConvertFrom(ITypeDescriptorContext typeDescriptorContext, 
                                           CultureInfo cultureInfo, 
                                           object source)
        {
            if (source != null)
            {
                return new ThemeMode(source.ToString());
            }

            throw GetConvertFromException(source);
        }

        /// <summary>
        /// Attempts to convert a ThemeMode object to the specified type.
        /// </summary>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="value" />  is <see langword="null" />.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// <paramref name="value" /> is not a ThemeMode, or <paramref name="destinationType" /> isn't a valid destination type.
        /// </exception>
        /// <param name="typeDescriptorContext">The ITypeDescriptorContext for this call.</param>
        /// <param name="cultureInfo">The CultureInfo which is respected when converting.</param>
        /// <param name="value">The ThemeMode to convert.</param>
        /// <param name="destinationType">The type to which to convert the ThemeMode instance.</param>
        /// <returns>
        /// The newly constructed object.
        /// </returns>
        public override object ConvertTo(ITypeDescriptorContext typeDescriptorContext, 
                                         CultureInfo cultureInfo,
                                         object value,
                                         Type destinationType)
        {
            ArgumentNullException.ThrowIfNull(destinationType);

            if (value is ThemeMode themeMode)
            {
                if (destinationType == typeof(string)) 
                { 
                    return themeMode.Value;
                }
                else if (destinationType == typeof(InstanceDescriptor))
                {
                    ConstructorInfo ci = typeof(ThemeMode).GetConstructor(new Type[] { typeof(string) });
                    return new InstanceDescriptor(ci, new object[] { value });
                }
            }
            throw GetConvertToException(value, destinationType);
        }

        #endregion 
    }
}
