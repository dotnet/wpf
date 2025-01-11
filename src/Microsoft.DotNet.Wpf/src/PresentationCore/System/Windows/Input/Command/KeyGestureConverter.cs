// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel;    // for TypeConverter
using System.Globalization;     // for CultureInfo

namespace System.Windows.Input
{
    /// <summary>
    /// Converter class for converting between a <see cref="string"/> and <see cref="KeyGesture"/>.
    /// </summary>
    public class KeyGestureConverter : TypeConverter
    {
        ///<summary>
        ///CanConvertFrom()
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
        ///<returns>true if conversion	is possible</returns>
        public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType)
        {
            // We can convert to an InstanceDescriptor or to a string.
            if (destinationType != typeof(string))
                return false;

            // When invoked by the serialization engine we can convert to string only for known type
            if (context?.Instance is not KeyGesture keyGesture)
                return false;

            return ModifierKeysConverter.IsDefinedModifierKeys(keyGesture.Modifiers) && IsDefinedKey(keyGesture.Key);
        }

        /// <summary>
        /// ConvertFrom()
        /// </summary>
        /// <param name="context"></param>
        /// <param name="culture"></param>
        /// <param name="source"></param>
        /// <returns></returns>
        public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object source)
        {
            if (source is not string sourceString)
                throw GetConvertFromException(source);

            ReadOnlySpan<char> trimmedSource = sourceString.AsSpan().Trim();
            if (trimmedSource.IsEmpty)
                return new KeyGesture(Key.None);

            ReadOnlySpan<char> keyToken;
            ReadOnlySpan<char> modifiersToken;
            string displayString;

            // Break apart display string
            int index = trimmedSource.IndexOf(',');
            if (index >= 0)
            {
                displayString = trimmedSource.Slice(index + 1).Trim().ToString();
                trimmedSource = trimmedSource.Slice(0, index).Trim();
            }
            else
            {
                displayString = string.Empty;
            }

            // Break apart key and modifiers
            index = trimmedSource.LastIndexOf('+');
            if (index >= 0)
            {
                modifiersToken = trimmedSource.Slice(0, index);
                keyToken = trimmedSource.Slice(index + 1).Trim();
            }
            else
            {
                modifiersToken = ReadOnlySpan<char>.Empty;
                keyToken = trimmedSource;
            }

            return new KeyGesture(KeyConverter.GetKeyFromString(keyToken), ModifierKeysConverter.ConvertFromImpl(modifiersToken), displayString);
        }

        /// <summary>
        /// ConvertTo()
        /// </summary>
        /// <param name="context"></param>
        /// <param name="culture"></param>
        /// <param name="value"></param>
        /// <param name="destinationType"></param>
        /// <returns></returns>
        public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
        {
            ArgumentNullException.ThrowIfNull(destinationType);

            if (destinationType != typeof(string))
                throw GetConvertToException(value, destinationType);

            // Following checks are here to match the previous behavior
            if (value is null)
                return string.Empty;

            if (value is not KeyGesture keyGesture)
                throw GetConvertToException(value, destinationType);

            // If the key is None, nothing else matters
            if (keyGesture.Key is Key.None)
                return string.Empty;

            // You will only get string.Empty from KeyConverter for Key.None and we've checked that above
            string strKey = (string)keyConverter.ConvertTo(context, culture, keyGesture.Key, destinationType);

            // Prepend modifiers if there are any (TODO: ConvertMultipleModifiers is gonna crumble on no matching mods)
            ReadOnlySpan<char> modifierSpan = ModifierKeysConverter.ConvertMultipleModifiers(keyGesture.Modifiers, stackalloc char[22]);
            if (modifierSpan.IsEmpty)
            {
                // No modifiers, just binding (possibly with with display string)
                return string.IsNullOrEmpty(keyGesture.DisplayString) ? strKey : $"{strKey},{keyGesture.DisplayString}";
            }

            // Append display string if there's any, like "Ctrl+A,Description"
            if (!string.IsNullOrEmpty(keyGesture.DisplayString))
                return string.Create(CultureInfo.InvariantCulture, stackalloc char[168], $"{modifierSpan}+{strKey},{keyGesture.DisplayString}");

            // We just put together modifiers and key, like "Ctrl+A"
            return string.Create(CultureInfo.InvariantCulture, stackalloc char[50], $"{modifierSpan}+{strKey}");
        }

        /// <summary>
        /// Helper function similar to <see cref="Enum.IsDefined{Key}(Key)"/>, just lighter and faster.
        /// </summary>
        /// <param name="key">The value to test against.</param>
        /// <returns><see langword="true"/> if <paramref name="key"/> falls in enumeration range, <see langword="false"/> otherwise.</returns>
        internal static bool IsDefinedKey(Key key)
        {
            return key >= Key.None && key <= Key.OemClear;
        }

        private static KeyConverter keyConverter = new KeyConverter();

    }
}

