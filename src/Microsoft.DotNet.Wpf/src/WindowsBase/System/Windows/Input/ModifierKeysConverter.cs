// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

	//
//
// Description:
//
//      ModifierKeysConverter : Converts a Modifier string to the *Type* that the string represents and vice-versa.
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
using MS.Internal.WindowsBase;

namespace System.Windows.Input
{
    /// <summary>
    /// Key Converter class for converting between a string and the Type of a Modifiers
    /// </summary>
    /// <ExternalAPI/> 
    public class ModifierKeysConverter : TypeConverter
    {
        /// <summary>
        /// CanConvertFrom()
        /// </summary>
        /// <param name="context"></param>
        /// <param name="sourceType"></param>
        /// <returns></returns>
        /// <ExternalAPI/> 
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
        /// TypeConverter method override. 
        /// </summary>
        /// <param name="context">ITypeDescriptorContext</param>
        /// <param name="destinationType">Type to convert to</param>
        /// <returns>true if conversion is possible</returns>
        public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType)
        {
            // We can convert to a string.
            if (destinationType == typeof(string))
            {
                // When invoked by the serialization engine we can convert to string only for known type
                if (context != null && context.Instance != null && 
					context.Instance is ModifierKeys)
                {
                    return (IsDefinedModifierKeys((ModifierKeys)context.Instance));
                }
            }
            return false;
        }

        /// <summary>
        /// ConvertFrom()
        /// </summary>
        /// <param name="context"></param>
        /// <param name="culture"></param>
        /// <param name="source"></param>
        /// <returns></returns>
        /// <ExternalAPI/> 
        public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object source)
        {
            if (source is string)
            {
                string modifiersToken = ((string)source).Trim();
                ModifierKeys modifiers = GetModifierKeys(modifiersToken, CultureInfo.InvariantCulture);
                return modifiers;
            }
            throw GetConvertFromException(source);
        }

        /// <summary>
        /// ConvertTo()
        /// </summary>
        /// <param name="context"></param>
        /// <param name="culture"></param>
        /// <param name="value"></param>
        /// <param name="destinationType"></param>
        /// <returns></returns>
        /// <ExternalAPI/> 
        public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
        {
            if (destinationType == null)
                throw new ArgumentNullException("destinationType");

            if (destinationType == typeof(string))
            {
                ModifierKeys modifiers = (ModifierKeys)value;

                if (!IsDefinedModifierKeys(modifiers))
                    throw new InvalidEnumArgumentException("value", (int)modifiers, typeof(ModifierKeys));
                else
                {
                    string strModifiers = "";

                    if ((modifiers & ModifierKeys.Control) == ModifierKeys.Control)
                    {
                        strModifiers += MatchModifiers(ModifierKeys.Control);
                    }

                    if ((modifiers & ModifierKeys.Alt) == ModifierKeys.Alt)
                    {
                        if (strModifiers.Length > 0) 
                            strModifiers += Modifier_Delimiter;

                        strModifiers += MatchModifiers(ModifierKeys.Alt);
                    }

                    if ((modifiers & ModifierKeys.Windows) == ModifierKeys.Windows)
                    {
                        if (strModifiers.Length > 0)
                            strModifiers += Modifier_Delimiter;

                        strModifiers += MatchModifiers(ModifierKeys.Windows); ;
                    }

                    if ((modifiers & ModifierKeys.Shift) == ModifierKeys.Shift)
                    {
                        if (strModifiers.Length > 0)
                            strModifiers += Modifier_Delimiter;

                        strModifiers += MatchModifiers(ModifierKeys.Shift); ;
                    }

                    return strModifiers;
                }
            }
            throw GetConvertToException(value,destinationType);
        }

        private ModifierKeys GetModifierKeys(string modifiersToken, CultureInfo culture)
        {
            ModifierKeys modifiers = ModifierKeys.None;
            if (modifiersToken.Length != 0)
            {
                int offset = 0;
                do
                {
                    offset = modifiersToken.IndexOf(Modifier_Delimiter);
                    string token = (offset < 0) ? modifiersToken : modifiersToken.Substring(0, offset);
                    token = token.Trim();
                    token = token.ToUpper(culture);

                    if (token == String.Empty)
                        break;

                    switch (token)
                    {
                        case "CONTROL" :
                        case "CTRL" : 
                            modifiers |= ModifierKeys.Control;
                            break;

                        case "SHIFT" : 
                            modifiers |= ModifierKeys.Shift;
                            break;

                        case "ALT":
                            modifiers |= ModifierKeys.Alt;
                            break;

                        case "WINDOWS":
                        case "WIN":
                            modifiers |= ModifierKeys.Windows;
                            break;

                        default:
                            throw new NotSupportedException(SR.Get(SRID.Unsupported_Modifier, token));
                    }

                    modifiersToken = modifiersToken.Substring(offset + 1);
                } while (offset != -1);
            }
            return modifiers;
        }

        /// <summary>
        ///     Check for Valid enum, as any int can be casted to the enum.
        /// </summary>
        public static bool IsDefinedModifierKeys(ModifierKeys modifierKeys)
        {
            return (modifierKeys == ModifierKeys.None || (((int)modifierKeys & ~((int)ModifierKeysFlag)) == 0));
        }

	    private const char Modifier_Delimiter = '+';

        private static ModifierKeys ModifierKeysFlag  =  ModifierKeys.Windows | ModifierKeys.Shift | 
                                                         ModifierKeys.Alt     | ModifierKeys.Control ;

        internal static string MatchModifiers(ModifierKeys modifierKeys)
        {
            string modifiers = String.Empty; 
            switch (modifierKeys)
            {
                case ModifierKeys.Control: modifiers="Ctrl";break;
                case ModifierKeys.Shift  : modifiers="Shift";break;
                case ModifierKeys.Alt    : modifiers="Alt";break;
                case ModifierKeys.Windows: modifiers="Windows";break;
            }
            return modifiers;
        }
    }
}
