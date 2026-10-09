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
        /// <summary>
        /// To aid with conversion from <see cref="Key"/> to <see cref="string"/>. 
        /// </summary>
        private static readonly KeyConverter s_keyConverter = new();

        /// <summary>
        /// Returns whether or not this class can convert from a given <paramref name="sourceType"/>.
        /// </summary>
        /// <param name="context">The <see cref="ITypeDescriptorContext"/> for this call.</param>
        /// <param name="sourceType">The <see cref="Type"/> being queried for support.</param>
        /// <returns>
        /// <see langword="true"/> if the given <paramref name="sourceType"/> can be converted, <see langword="false"/> otherwise.
        /// </returns>
        public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
        {
            // We can only handle string.
            return sourceType == typeof(string);
        }

        /// <summary>
        /// Returns whether or not this class can convert to a given <paramref name="destinationType"/>.
        /// </summary>
        /// <param name="context">The <see cref="ITypeDescriptorContext"/> for this call.</param>
        /// <param name="destinationType">The <see cref="Type"/> being queried for support.</param>
        /// <returns>
        /// <see langword="true"/> if this class can convert to <paramref name="destinationType"/>, <see langword="false"/> otherwise.
        /// </returns>
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
        /// Converts <paramref name="source"/> of <see cref="string"/> type to its <see cref="KeyGesture"/> represensation.
        /// </summary>
        /// <param name="context">This parameter is ignored during the call.</param>
        /// <param name="culture">This parameter is ignored during the call.</param>
        /// <param name="source">The object to convert to a <see cref="KeyGesture"/>.</param>
        /// <returns>
        /// A new instance of <see cref="KeyGesture"/> class representing the data contained in <paramref name="source"/>.
        /// </returns>
        /// <exception cref="NotSupportedException">Thrown in case the <paramref name="source"/> was not a <see cref="string"/>.</exception>
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
        /// Attempt to convert a <see cref="KeyGesture"/> class to the <paramref name="destinationType"/>.
        /// </summary>
        /// <param name="context">This parameter is ignored during the call.</param>
        /// <param name="culture">This parameter is ignored during the call.</param>
        /// <param name="value">The object to convert to a <paramref name="destinationType"/>.</param>
        /// <param name="destinationType">The <see cref="Type"/> to convert <paramref name="value"/> to.</param>
        /// <returns>
        /// The <paramref name="value"/> formatted to its <see cref="string"/> representation.
        /// </returns>
        /// <exception cref="ArgumentNullException">Thrown in case <paramref name="destinationType"/> was <see langword="null"/>.</exception>
        /// <exception cref="NotSupportedException">
        /// Thrown in case the <paramref name="destinationType"/> was not a <see cref="string"/>
        /// or <paramref name="value"/> was not a <see cref="KeyGesture"/>.
        /// </exception>
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
            string strKey = (string)s_keyConverter.ConvertTo(context, culture, keyGesture.Key, destinationType);

            // No modifiers, just binding (possibly with with display string) -> "F5,Refresh"
            if (keyGesture.Modifiers is ModifierKeys.None)
                return string.IsNullOrEmpty(keyGesture.DisplayString) ? strKey : $"{strKey},{keyGesture.DisplayString}";

            ReadOnlySpan<char> modifierSpan = ModifierKeysConverter.ConvertMultipleModifiers(keyGesture.Modifiers, stackalloc char[22]);

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
    }
}

