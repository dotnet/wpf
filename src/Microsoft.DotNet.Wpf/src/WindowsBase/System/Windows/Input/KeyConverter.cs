// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.ComponentModel;
using System.Globalization;

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
            // We can only handle string
            return sourceType == typeof(string);
        }

        /// <summary>
        /// TypeConverter method override. 
        /// </summary>
        /// <param name="context">ITypeDescriptorContext</param>
        /// <param name="destinationType">Type to convert to</param>
        /// <returns>true if conversion is possible</returns>
        public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType)
        {
            // We can convert to a string
            if (destinationType != typeof(string))
                return false;

            // When invoked by the serialization engine we can convert to string only for known type
            if (context is null || context.Instance is not Key key)
                return false;

            return key >= Key.None && key <= Key.DeadCharProcessed;
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
            if (source is not string stringSource)
                throw GetConvertFromException(source);
            
            ReadOnlySpan<char> fullName = stringSource.AsSpan().Trim();
            return GetKeyFromString(fullName);      
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
            ArgumentNullException.ThrowIfNull(destinationType);

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

        private static Key GetKeyFromString(ReadOnlySpan<char> keyToken)
        {
            // If the token is empty, we presume "None" as our value but it is a success
            if (keyToken.IsEmpty)
                return Key.None;

            // In case we're dealing with a lowercase character, we uppercase it
            char firstChar = keyToken[0];
            if (firstChar >= 'a' && firstChar <= 'z')
                firstChar ^= (char)0x20;

            // If this is a single-character we're dealing with, match digits/letters
            if (keyToken.Length == 1 && char.IsLetterOrDigit(firstChar))
            {
                // Match a digit (0-9) or an ASCII letter (lower/uppercase)
                if (char.IsDigit(firstChar) && (firstChar >= '0' && firstChar <= '9'))
                    return Key.D0 + firstChar - '0';
                else if (char.IsLetter(firstChar) && (firstChar >= 'A' && firstChar <= 'Z'))
                    return Key.A + firstChar - 'A';
                else
                    throw new ArgumentException(SR.Format(SR.CannotConvertStringToType, keyToken.ToString(), typeof(Key)));
            }

            // Special path for F1-F9 (switch would take 600 B in code size for no benefit)
            char secondChar = keyToken[1];
            if (keyToken.Length == 2 && firstChar == 'F' && (secondChar > '0' && secondChar <= '9'))
                return Key.F1 + secondChar - '1';

            // It is a special key or an invalid one, we're gonna find out
            switch (keyToken.Length)
            {
                case 2:
                    if (keyToken.Equals("BS", StringComparison.OrdinalIgnoreCase))
                        return Key.Back;
                    break;
                case 3:
                    switch (firstChar)
                    {
                        case 'A':
                            if (keyToken.Equals("Alt", StringComparison.OrdinalIgnoreCase))
                                return Key.LeftAlt;
                            break;
                        case 'D':
                            if (keyToken.Equals("Del", StringComparison.OrdinalIgnoreCase))
                                return Key.Delete;
                            break;
                        case 'E':
                            if (keyToken.Equals("Esc", StringComparison.OrdinalIgnoreCase))
                                return Key.Escape;
                            break;
                        case 'F':
                            if (keyToken.Equals("F10", StringComparison.OrdinalIgnoreCase))
                                return Key.F10;
                            if (keyToken.Equals("F11", StringComparison.OrdinalIgnoreCase))
                                return Key.F11;
                            if (keyToken.Equals("F12", StringComparison.OrdinalIgnoreCase))
                                return Key.F12;
                            break;
                        case 'I':
                            if (keyToken.Equals("INS", StringComparison.OrdinalIgnoreCase))
                                return Key.Insert;
                            break;
                        case 'P':
                            if (keyToken.Equals("Pa1", StringComparison.OrdinalIgnoreCase))
                                return Key.Pa1;
                            break;
                        case 'W':
                            if (keyToken.Equals("Win", StringComparison.OrdinalIgnoreCase))
                                return Key.LWin;
                            break;
                    }
                    break;
                case 4:
                    switch (firstChar)
                    {
                        case 'A':
                            if (keyToken.Equals("Apps", StringComparison.OrdinalIgnoreCase))
                                return Key.Apps;
                            if (keyToken.Equals("Attn", StringComparison.OrdinalIgnoreCase))
                                return Key.Attn;
                            break;
                        case 'B':
                            if (keyToken.Equals("BKSP", StringComparison.OrdinalIgnoreCase))
                                return Key.Back;
                            break;
                        case 'C':
                            if (keyToken.Equals("Ctrl", StringComparison.OrdinalIgnoreCase))
                                return Key.LeftCtrl;
                            break;
                        case 'P':
                            if (keyToken.Equals("PGDN", StringComparison.OrdinalIgnoreCase))
                                return Key.PageDown;
                            if (keyToken.Equals("PGUP", StringComparison.OrdinalIgnoreCase))
                                return Key.PageUp;
                            if (keyToken.Equals("Pipe", StringComparison.OrdinalIgnoreCase))
                                return Key.OemPipe;
                            if (keyToken.Equals("Play", StringComparison.OrdinalIgnoreCase))
                                return Key.Play;
                            if (keyToken.Equals("Plus", StringComparison.OrdinalIgnoreCase))
                                return Key.OemPlus;
                            break;
                        case 'Z':
                            if (keyToken.Equals("Zoom", StringComparison.OrdinalIgnoreCase))
                                return Key.Zoom;
                            break;
                    }
                    break;
                case 5:
                    switch (firstChar)
                    {
                        case 'B':
                            if (keyToken.Equals("Break", StringComparison.OrdinalIgnoreCase))
                                return Key.Cancel;
                            break;
                        case 'C':
                            if (keyToken.Equals("Comma", StringComparison.OrdinalIgnoreCase))
                                return Key.OemComma;
                            if (keyToken.Equals("CrSel", StringComparison.OrdinalIgnoreCase))
                                return Key.CrSel;
                            break;
                        case 'E':
                            if (keyToken.Equals("Enter", StringComparison.OrdinalIgnoreCase))
                                return Key.Return;
                            if (keyToken.Equals("ExSel", StringComparison.OrdinalIgnoreCase))
                                return Key.ExSel;
                            break;
                        case 'M':
                            if (keyToken.Equals("Minus", StringComparison.OrdinalIgnoreCase))
                                return Key.OemMinus;
                            break;
                        case 'P':
                            if (keyToken.Equals("PRTSC", StringComparison.OrdinalIgnoreCase))
                                return Key.PrintScreen;
                            break;
                        case 'S':
                            if (keyToken.Equals("Shift", StringComparison.OrdinalIgnoreCase))
                                return Key.LeftShift;
                            break;
                        case 'T':
                            if (keyToken.Equals("Tilde", StringComparison.OrdinalIgnoreCase))
                                return Key.OemTilde;
                            break;
                    }
                    break;
                case 6:
                    if (keyToken.Equals("Finish", StringComparison.OrdinalIgnoreCase))
                        return Key.OemFinish;
                    if (keyToken.Equals("Period", StringComparison.OrdinalIgnoreCase))
                        return Key.OemPeriod;
                    if (keyToken.Equals("Quotes", StringComparison.OrdinalIgnoreCase))
                        return Key.OemQuotes;
                    break;
                case 7:
                    if (keyToken.Equals("Control", StringComparison.OrdinalIgnoreCase))
                        return Key.LeftCtrl;
                    if (keyToken.Equals("LeftAlt", StringComparison.OrdinalIgnoreCase))
                        return Key.LeftAlt;
                    if (keyToken.Equals("Windows", StringComparison.OrdinalIgnoreCase))
                        return Key.LWin;
                    break;
                case 8:
                    if (keyToken.Equals("EraseEof", StringComparison.OrdinalIgnoreCase))
                        return Key.EraseEof;
                    if (keyToken.Equals("LeftCtrl", StringComparison.OrdinalIgnoreCase))
                        return Key.LeftCtrl;
                    if (keyToken.Equals("Question", StringComparison.OrdinalIgnoreCase))
                        return Key.OemQuestion;
                    if (keyToken.Equals("RightAlt", StringComparison.OrdinalIgnoreCase))
                        return Key.RightAlt;
                    break;
                case 9:
                    if (keyToken.Equals("Backslash", StringComparison.OrdinalIgnoreCase))
                        return Key.OemBackslash;
                    if (keyToken.Equals("Backspace", StringComparison.OrdinalIgnoreCase))
                        return Key.Back;
                    if (keyToken.Equals("LeftShift", StringComparison.OrdinalIgnoreCase))
                        return Key.LeftShift;
                    if (keyToken.Equals("RightCtrl", StringComparison.OrdinalIgnoreCase))
                        return Key.RightCtrl;
                    if (keyToken.Equals("Semicolon", StringComparison.OrdinalIgnoreCase))
                        return Key.OemSemicolon;
                    break;
                case 10:
                    if (keyToken.Equals("RightShift", StringComparison.OrdinalIgnoreCase))
                        return Key.RightShift;
                    break;
                case 11:
                    if (keyToken.Equals("Application", StringComparison.OrdinalIgnoreCase))
                        return Key.Apps;
                    if (keyToken.Equals("LeftWindows", StringComparison.OrdinalIgnoreCase))
                        return Key.LWin;
                    break;
                case 12:
                    if (keyToken.Equals("OpenBrackets", StringComparison.OrdinalIgnoreCase))
                        return Key.OemOpenBrackets;
                    if (keyToken.Equals("RightWindows", StringComparison.OrdinalIgnoreCase))
                        return Key.RWin;
                    break;
                case 13:
                    if (keyToken.Equals("CloseBrackets", StringComparison.OrdinalIgnoreCase))
                        return Key.OemCloseBrackets;
                    break;
            }

            return Enum.Parse<Key>(keyToken, true);
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

