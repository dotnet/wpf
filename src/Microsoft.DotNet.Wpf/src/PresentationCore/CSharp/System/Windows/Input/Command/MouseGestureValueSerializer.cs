// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
//
// Description: MouseGestureValueSerializer - Serializes a MouseGesture 
//

using System;
using System.ComponentModel;    // for TypeConverter
using System.Globalization;     // for CultureInfo
using System.Reflection;
using System.Windows;
using System.Windows.Input;
using System.Windows.Markup;

namespace System.Windows.Input
{
    /// <summary>
    /// MouseGesture - Converter class for converting between a string and the Type of a MouseGesture
    /// </summary>
    public class MouseGestureValueSerializer : ValueSerializer
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
            bool result = false;
            MouseGesture mouseGesture = value as MouseGesture;

            if(mouseGesture != null)
            {
                if(ModifierKeysConverter.IsDefinedModifierKeys(mouseGesture.Modifiers)
                   && MouseActionConverter.IsDefinedMouseAction(mouseGesture.MouseAction))
                {
                    result = true;
                }
}
            
            return result;
        }
        
        /// <summary>
        /// ConvertFromString()
        /// </summary>
        /// <param name="value"></param>
        /// <param name="context"></param>
        /// <returns></returns>
        public override object ConvertFromString(string value, IValueSerializerContext context) 
        {
            TypeConverter converter = TypeDescriptor.GetConverter(typeof(MouseGesture));
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
            TypeConverter converter = TypeDescriptor.GetConverter(typeof(MouseGesture));
            if (converter != null)
                return converter.ConvertToInvariantString(value);
            else
                return base.ConvertToString(value, context);
        }
    }
}
