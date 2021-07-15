// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
//
// Description:
//
//      KeyConverter : Converts a key string to the *Type* that the string represents and vice-versa
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
using System.Windows;
using System.Windows.Input;
using System.Windows.Markup;
using MS.Utility;
using MS.Internal.WindowsBase;

namespace System.Windows.Input
{
    /// <summary>
    /// Key Converter class for converting between a string and the Type of a Key
    /// </summary>
    /// <ExternalAPI/> 
    public class KeyConverter : TypeConverter
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
            // We can convert to an InstanceDescriptor or to a string.
            if (destinationType == typeof(string))
            {
                // When invoked by the serialization engine we can convert to string only for known type
                if (context != null && context.Instance != null)
                {
                    Key key = (Key)context.Instance;
                    return ((int)key >= (int)Key.None && (int)key <= (int)Key.DeadCharProcessed);
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
                string fullName = ((string)source).Trim();
                object key = GetKey(fullName, CultureInfo.InvariantCulture);
                if (key != null)
                {
                    return ((Key)key);
                }
                else
                {
                    throw new NotSupportedException(SR.Get(SRID.Unsupported_Key, fullName));
                }
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

            if (destinationType == typeof(string) && value != null)
            {
                Key key = (Key)value;
                if (key == Key.None)
                {
                    return String.Empty;
                }

                if (key >= Key.D0 && key <= Key.D9)
                {
                    return Char.ToString((char)(int)(key - Key.D0 + '0'));
                }

                if (key >= Key.A && key <= Key.Z)
                {
                    return Char.ToString((char)(int)(key - Key.A + 'A'));
                }

                String strKey = MatchKey(key, culture);
                if (strKey != null)
                {
                    return strKey;
                }
            }
            throw GetConvertToException(value, destinationType);
        }

        private object GetKey(string keyToken, CultureInfo culture)
        {
            if (keyToken.Length == 0)
            {
                return Key.None;
            }
            else
            {
                keyToken = keyToken.ToUpper(culture);
                if (keyToken.Length == 1 && Char.IsLetterOrDigit(keyToken[0]))
                {
                    if (Char.IsDigit(keyToken[0]) && (keyToken[0] >= '0' && keyToken[0] <= '9'))
                    {
                        return ((int)(Key)(Key.D0 + keyToken[0] - '0'));
                    }
                    else if (Char.IsLetter(keyToken[0]) && (keyToken[0] >= 'A' && keyToken[0] <= 'Z'))
                    {
                        return ((int)(Key)(Key.A + keyToken[0] - 'A'));
                    }
                    else
                    {
                        throw new ArgumentException(SR.Get(SRID.CannotConvertStringToType, keyToken, typeof(Key)));
                    }
                }
                else
                {
                    Key keyFound = (Key)(-1);
                    switch (keyToken)
                    {
                        case "ENTER": keyFound = Key.Return; break;
                        case "ESC": keyFound = Key.Escape; break;
                        case "PGUP": keyFound = Key.PageUp; break;
                        case "PGDN": keyFound = Key.PageDown; break;
                        case "PRTSC": keyFound = Key.PrintScreen; break;
                        case "INS": keyFound = Key.Insert; break;
                        case "DEL": keyFound = Key.Delete; break;
                        case "WINDOWS": keyFound = Key.LWin; break;
                        case "WIN": keyFound = Key.LWin; break;
                        case "LEFTWINDOWS": keyFound = Key.LWin; break;
                        case "RIGHTWINDOWS": keyFound = Key.RWin; break;
                        case "APPS": keyFound = Key.Apps; break;
                        case "APPLICATION": keyFound = Key.Apps; break;
                        case "BREAK": keyFound = Key.Cancel; break;
                        case "BACKSPACE": keyFound = Key.Back; break;
                        case "BKSP": keyFound = Key.Back; break;
                        case "BS": keyFound = Key.Back; break;
                        case "SHIFT": keyFound = Key.LeftShift; break;
                        case "LEFTSHIFT": keyFound = Key.LeftShift; break;
                        case "RIGHTSHIFT": keyFound = Key.RightShift; break;
                        case "CONTROL": keyFound = Key.LeftCtrl; break;
                        case "CTRL": keyFound = Key.LeftCtrl; break;
                        case "LEFTCTRL": keyFound = Key.LeftCtrl; break;
                        case "RIGHTCTRL": keyFound = Key.RightCtrl; break;
                        case "ALT": keyFound = Key.LeftAlt; break;
                        case "LEFTALT": keyFound = Key.LeftAlt; break;
                        case "RIGHTALT": keyFound = Key.RightAlt; break;
                        case "SEMICOLON": keyFound = Key.OemSemicolon; break;
                        case "PLUS": keyFound = Key.OemPlus; break;
                        case "COMMA": keyFound = Key.OemComma; break;
                        case "MINUS": keyFound = Key.OemMinus; break;
                        case "PERIOD": keyFound = Key.OemPeriod; break;
                        case "QUESTION": keyFound = Key.OemQuestion; break;
                        case "TILDE": keyFound = Key.OemTilde; break;
                        case "OPENBRACKETS": keyFound = Key.OemOpenBrackets; break;
                        case "PIPE": keyFound = Key.OemPipe; break;
                        case "CLOSEBRACKETS": keyFound = Key.OemCloseBrackets; break;
                        case "QUOTES": keyFound = Key.OemQuotes; break;
                        case "BACKSLASH": keyFound = Key.OemBackslash; break;
                        case "FINISH": keyFound = Key.OemFinish; break;
                        case "ATTN": keyFound = Key.Attn; break;
                        case "CRSEL": keyFound = Key.CrSel; break;
                        case "EXSEL": keyFound = Key.ExSel; break;
                        case "ERASEEOF": keyFound = Key.EraseEof; break;
                        case "PLAY": keyFound = Key.Play; break;
                        case "ZOOM": keyFound = Key.Zoom; break;
                        case "PA1": keyFound = Key.Pa1; break;
                        default: keyFound = (Key)Enum.Parse(typeof(Key), keyToken, true); break;
                    }

                    if ((int)keyFound != -1)
                    {
                        return keyFound;
                    }
                    return null;
                }
            }
        }

        private static string MatchKey(Key key, CultureInfo culture)
        {
            if (key == Key.None)
                return String.Empty;
            else
            {
                switch (key)
                {
                    case Key.Back: return "Backspace";
                    case Key.LineFeed: return "Clear";
                    case Key.Escape: return "Esc";
                }
            }
            if ((int)key >= (int)Key.None && (int)key <= (int)Key.DeadCharProcessed)
                return key.ToString();
            else
                return null;
        }
    }
}

