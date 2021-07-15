// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/***************************************************************************\
*
*  Class for converting a given TemplateKey to a string
*
\***************************************************************************/
using System;
using System.ComponentModel;        // for TypeConverter
using System.Globalization;               // for CultureInfo
using System.Reflection;
using MS.Utility;
using MS.Internal;
using System.Windows;
using System.ComponentModel.Design.Serialization;
using System.Windows.Documents;

#pragma warning disable 1634, 1691  // suppressing PreSharp warnings

namespace System.Windows.Markup
{
    /// <summary>
    ///     Class for converting a given TemplateKey to a string
    /// </summary>
    public sealed class TemplateKeyConverter : TypeConverter
    {
        #region Public Methods
        
        /// <summary>
        ///     CanConvertFrom()
        /// </summary>
        /// <param name="context">ITypeDescriptorContext</param>
        /// <param name="sourceType">type to convert from</param>
        /// <returns>true if the given type can be converted, flase otherwise</returns>
        public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
        {
            return false;
        }

        /// <summary>
        ///     TypeConverter method override. 
        /// </summary>
        /// <param name="context">ITypeDescriptorContext</param>
        /// <param name="destinationType">Type to convert to</param>
        /// <returns>true if conversion is possible</returns>
        public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType)
        {
            return false;
        }

        /// <summary>
        ///     ConvertFrom() -TypeConverter method override. using the givein name to return DependencyProperty
        /// </summary>
        /// <param name="context">ITypeDescriptorContext</param>
        /// <param name="culture">CultureInfo</param>
        /// <param name="source">Object to convert from</param>
        /// <returns>instance of Command</returns>
        public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object source)
        {
            throw GetConvertFromException(source);
        }

        /// <summary>
        ///     ConvertTo() - Serialization purposes, returns the string from Command.Name by adding ownerType.FullName
        /// </summary>
        /// <param name="context">ITypeDescriptorContext</param>
        /// <param name="culture">CultureInfo</param>
        /// <param name="value">the	object to convert from</param>
        /// <param name="destinationType">the type to convert to</param>
        /// <returns>string object, if the destination type is string</returns>
        public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
        {
            throw GetConvertToException(value, destinationType);
        }

        #endregion Public Methods
    }
}


