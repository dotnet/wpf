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

using System.ComponentModel;    // for TypeConverter
using System.Globalization;     // for CultureInfo

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
            return sourceType == typeof(string);
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
            if (destinationType != typeof(string))
                return false;

            // When invoked by the serialization engine we can convert to string only for known type
            if (context?.Instance is not MouseGesture mouseGesture)
                return false;

            return ModifierKeysConverter.IsDefinedModifierKeys(mouseGesture.Modifiers) &&
                   MouseActionConverter.IsDefinedMouseAction(mouseGesture.MouseAction);
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
            if (source is not string sourceString)
                throw GetConvertFromException(source);

            ReadOnlySpan<char> trimmedSource = sourceString.AsSpan().Trim();

            // Break apart MouseAction and ModifierKeys
            int index = trimmedSource.LastIndexOf('+');
            if (index >= 0)
            {
                ReadOnlySpan<char> mouseActionToken = trimmedSource.Slice(index + 1).TrimStart();
                ReadOnlySpan<char> modifiersToken = trimmedSource.Slice(0, index).TrimEnd();

                return new MouseGesture(MouseActionConverter.ConvertFromImpl(mouseActionToken), ModifierKeysConverter.ConvertFromImpl(modifiersToken));
            }

            return new MouseGesture(MouseActionConverter.ConvertFromImpl(trimmedSource), ModifierKeys.None);
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
            ArgumentNullException.ThrowIfNull(destinationType);

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


