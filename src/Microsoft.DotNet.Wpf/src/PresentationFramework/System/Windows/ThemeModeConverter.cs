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
    /// ThemeModeConverter - Converter class for converting instances 
    /// of other types to and from ThemeMode instances.
    /// </summary>
    [Experimental("WPF0001")]
    public class ThemeModeConverter: TypeConverter
    {

        #region Public Methods

        /// <summary>
        /// CanConvertFrom - Returns whether or not this class can convert from a given type.
        /// </summary>
        /// <param name="typeDescriptorContext">The ITypeDescriptorContext for this call.</param>
        /// <param name="sourceType">The Type being queried for support.</param>
        /// <returns>
        /// bool - True if thie converter can convert from the provided type, false if not.
        /// </returns>
        public override bool CanConvertFrom(ITypeDescriptorContext typeDescriptorContext, Type sourceType)
        {
           return Type.GetTypeCode(sourceType) == TypeCode.String;
        }

        /// <summary>
        /// CanConvertTo - Returns whether or not this class can convert to a given type.
        /// </summary>
        /// <param name="typeDescriptorContext"> The ITypeDescriptorContext for this call. </param>
        /// <param name="destinationType"> The Type being queried for support. </param>
        /// <returns>
        /// bool - True if this converter can convert to the provided type, false if not.
        /// </returns>
        public override bool CanConvertTo(ITypeDescriptorContext typeDescriptorContext, Type destinationType) 
        {
            // We can convert to an InstanceDescriptor or to a string.
            return destinationType == typeof(InstanceDescriptor) || destinationType == typeof(string);
        }

        /// <summary>
        /// ConvertFrom - Attempts to convert to a ThemeMode from the given object
        /// </summary>
        /// <exception cref="ArgumentNullException">
        /// An ArgumentNullException is thrown if the example object is null.
        /// </exception>
        /// <param name="typeDescriptorContext">The ITypeDescriptorContext for this call.</param>
        /// <param name="cultureInfo">The CultureInfo which is respected when converting.</param>
        /// <param name="source">The object to convert to a ThemeMode.</param>
        /// <returns>
        /// ThemeMode instance which was constructed.
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
        /// ConvertTo - Attempts to convert a ThemeMode to the given type
        /// </summary>
        /// <exception cref="ArgumentNullException">
        /// An ArgumentNullException is thrown if the example object is null.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// An ArgumentException is thrown if the object is not null and is not a ThemeMode,
        /// or if the destinationType isn't one of the valid destination types.
        /// </exception>
        /// <param name="typeDescriptorContext">The ITypeDescriptorContext for this call.</param>
        /// <param name="cultureInfo">The CultureInfo which is respected when converting.</param>
        /// <param name="value">The ThemeMode to convert.</param>
        /// <param name="destinationType">The type to which to convert the ThemeMode instance.</param>
        /// <returns></returns>
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