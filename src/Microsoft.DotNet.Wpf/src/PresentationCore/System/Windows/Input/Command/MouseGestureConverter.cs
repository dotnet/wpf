// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
//
//
// Description: MouseGestureConverter - Converts a MouseGesture string 
//              to the *Type* that the string represents 
//
//

using System;
using System.ComponentModel;    // for TypeConverter
using System.Globalization;     // for CultureInfo
using System.Reflection;
using MS.Internal;
using System.Windows;
using System.Windows.Input;
using MS.Utility;

namespace System.Windows.Input
{
    /// <summary>
    /// MouseGesture - Converter class for converting between a string and the Type of a MouseGesture
    /// </summary>
    public class MouseGestureConverter : TypeConverter
    {
        private const char MODIFIERS_DELIMITER = '+' ;
        
        ///<summary>
        /// CanConvertFrom()
        ///</summary>
        ///<param name="context">ITypeDescriptorContext</param>
        ///<param name="sourceType">type to convert from</param>
        ///<returns>true if the given type can be converted, false otherwise</returns>
        public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
        {
            // We can only handle string.
            if (sourceType == typeof(string))
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// ConvertFrom
        /// </summary>
        /// <param name="context">Parser Context</param>
        /// <param name="culture">Culture Info</param>
        /// <param name="source">MouseGesture String</param>
        /// <returns></returns>
        public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object source)
        {
            if (source is string && source != null)
            {
                string fullName = ((string)source).Trim();
                string mouseActionToken;
                string modifiersToken;

                if (fullName.Length == 0)
                    return new MouseGesture(MouseAction.None, ModifierKeys.None); ;

                // break apart LocalName and Prefix
                int Offset = fullName.LastIndexOf(MODIFIERS_DELIMITER);
                if (Offset >= 0)
                {   // modifiers exists
                    modifiersToken      = fullName.Substring(0,Offset);
                    mouseActionToken = fullName.Substring(Offset + 1);
                }
                else
                {
                    modifiersToken       = String.Empty;
                    mouseActionToken  = fullName;
                }             
                
                TypeConverter mouseActionConverter = TypeDescriptor.GetConverter(typeof(System.Windows.Input.MouseAction));
                if (null != mouseActionConverter )
                {
                    object mouseAction = mouseActionConverter.ConvertFrom(context, culture, mouseActionToken);
                    // mouseAction Converter will throw Exception, if it fails, 
                    // so we don't need to check once more for bogus 
                    // MouseAction values
                    if (mouseAction != null)
                    {
                        if (modifiersToken != String.Empty)
                        {
                            TypeConverter modifierKeysConverter = TypeDescriptor.GetConverter(typeof(System.Windows.Input.ModifierKeys));
                            if (null != modifierKeysConverter)
                            {
                                object modifierKeys = modifierKeysConverter.ConvertFrom(context, culture, modifiersToken);

                                if (modifierKeys != null && modifierKeys is ModifierKeys)
                                {
                                    return new MouseGesture((MouseAction)mouseAction, (ModifierKeys)modifierKeys);
                                }
                            }
                        }
                        else
                        {
                            return new MouseGesture((MouseAction)mouseAction);
                        }
                    }
                }
            }
            throw GetConvertFromException(source);
        }

        ///<summary>
        ///TypeConverter method override. 
        ///</summary>
        ///<param name="context">ITypeDescriptorContext</param>
        ///<param name="destinationType">Type to convert to</param>
        ///<returns>true if conversion is possible</returns>
        public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType)
        {
            // We can convert to an InstanceDescriptor or to a string.
            if (destinationType == typeof(string))
            {
                // When invoked by the serialization engine we can convert to string only for known type
                if (context != null && context.Instance != null)
                {
                    MouseGesture mouseGesture = context.Instance as MouseGesture;
                    if (mouseGesture != null)
                    {
                        return (ModifierKeysConverter.IsDefinedModifierKeys(mouseGesture.Modifiers) 
                               && MouseActionConverter.IsDefinedMouseAction(mouseGesture.MouseAction));
                    }
                 }
            }
            return false;
        }

        /// <summary>
        /// ConvertTo()
        /// </summary>
        /// <param name="context">Serialization Context</param>
        /// <param name="culture">Culture Info</param>
        /// <param name="value">MouseGesture value </param>
        /// <param name="destinationType">Type to Convert</param>
        /// <returns>string if parameter is a MouseGesture</returns>
        public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
        {
            if (destinationType == null)
                throw new ArgumentNullException("destinationType");
 
            if (destinationType == typeof(string))
            {
                if (value == null)
                    return String.Empty;

                MouseGesture mouseGesture = value as MouseGesture;
                if (mouseGesture != null)
                {
                    string strGesture = "";

                    TypeConverter modifierKeysConverter = TypeDescriptor.GetConverter(typeof(System.Windows.Input.ModifierKeys));
                    if (null != modifierKeysConverter)
                    {
                        strGesture += modifierKeysConverter.ConvertTo(context, culture, mouseGesture.Modifiers, destinationType) as string;
                        if (strGesture != String.Empty)
                        {
                            strGesture += MODIFIERS_DELIMITER ;
                        }
                    }
                    TypeConverter mouseActionConverter = TypeDescriptor.GetConverter(typeof(System.Windows.Input.MouseAction));
                    if (null != mouseActionConverter)
                    {
                        strGesture += mouseActionConverter.ConvertTo(context, culture, mouseGesture.MouseAction, destinationType) as string;
                    }                             
                    return strGesture;
                }
            }
            throw GetConvertToException(value,destinationType);
        }
    }
}


