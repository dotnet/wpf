// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
//
// Description:
//
//      ModifierKeysValueSerializer : Serializes a Modifier to and from a string.
//
// Features:
//
//
//
// 

using System;
using System.ComponentModel;    // for TypeConverter
using System.Globalization;     // for CultureInfo
using System.Reflection;
using MS.Internal;
using System.Windows;
using System.Windows.Input;
using System.Windows.Markup;
using MS.Utility;

namespace System.Windows.Input
{
    /// <summary>
    /// Key Converter class for converting between a string and the Type of a Modifiers
    /// </summary>
    /// <ExternalAPI/> 
    public class ModifierKeysValueSerializer : ValueSerializer
    {
        /// <summary>
        /// CanConvertFromString()
        /// </summary>
        /// <param name="value"></param>
        /// <param name="context"></param>
        /// <returns></returns>
        /// <ExternalAPI/> 
        public override bool CanConvertFromString(string value, IValueSerializerContext context) 
        {
            return true;
        }

        /// <summary>
        /// CanConvertToString()
        /// </summary>
        /// <param name="value"></param>
        /// <param name="context"></param>
        /// <returns></returns>
        /// <ExternalAPI/> 
        public override bool CanConvertToString(object value, IValueSerializerContext context) 
        {
            return (value is ModifierKeys) && ModifierKeysConverter.IsDefinedModifierKeys((ModifierKeys)value);
        }

        /// <summary>
        /// ConvertFromString()
        /// </summary>
        /// <param name="value"></param>
        /// <param name="context"></param>
        /// <returns></returns>
        public override object ConvertFromString(string value, IValueSerializerContext context) 
        {
            TypeConverter converter = TypeDescriptor.GetConverter(typeof(ModifierKeys));
            if (converter != null)
                return converter.ConvertFromString(value);
            else
                return base.ConvertFromString(value, context);
        }

        /// <summary>
        /// ConvertToString()
        /// </summary>
        /// <param name="value"></param>
        /// <param name="context"></param>
        /// <returns></returns>
        public override string ConvertToString(object value, IValueSerializerContext context) 
        {
            TypeConverter converter = TypeDescriptor.GetConverter(typeof(ModifierKeys));
            if (converter != null)
                return converter.ConvertToInvariantString(value);
            else
                return base.ConvertToString(value, context);
        }
    }
}
