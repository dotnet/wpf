// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.CompilerServices;  // for MethodImplAttribute
using System.ComponentModel;            // for TypeConverter
using System.Globalization;             // for CultureInfo

namespace System.Windows.Input
{
    /// <summary>
    /// Converter class for converting between a <see langword="string"/> and <see cref="ModifierKeys"/>.
    /// </summary>
    public class ModifierKeysConverter : TypeConverter
    {
        /// <summary>
        /// Used to check whether we can convert a <see langword="string"/> into a <see cref="ModifierKeys"/>.
        /// </summary>
        ///<param name="context">ITypeDescriptorContext</param>
        ///<param name="sourceType">type to convert from</param>
        ///<returns><see langword="true"/> if the given <paramref name="sourceType"/> can be converted from, <see langword="false"/> otherwise.</returns>
        public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
        {
            // We can only handle string
            return sourceType == typeof(string);
        }

        /// <summary>
        /// Used to check whether we can convert specified value to <see langword="string"/>.
        /// </summary>
        /// <param name="context">ITypeDescriptorContext</param>
        /// <param name="destinationType">Type to convert to</param>
        /// <returns><see langword="true"/> if conversion to <see langword="string"/> is possible, <see langword="false"/> otherwise.</returns>
        public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType)
        {
            // We can convert to a string
            if (destinationType != typeof(string))
                return false;

            // When invoked by the serialization engine we can convert to string only for known type
            if (context is null || context.Instance is not ModifierKeys modifiers)
                return false;

            return IsDefinedModifierKeys(modifiers);
        }

        /// <summary>
        /// Converts <paramref name="source"/> of <see langword="string"/> type to its <see cref="ModifierKeys"/> represensation.
        /// </summary>
        /// <param name="context">Parser Context</param>
        /// <param name="culture">Culture Info</param>
        /// <param name="source">ModifierKeys String</param>
        /// <returns>A <see cref="ModifierKeys"/> representing the <see langword="string"/> specified by <paramref name="source"/>.</returns>
        public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object source)
        {
            if (source is not string stringSource)
                throw GetConvertFromException(source);

            ReadOnlySpan<char> modifiersToken = stringSource.AsSpan().Trim();

            // Empty token means there were no modifiers, exit early
            if (modifiersToken.IsEmpty)
                return ModifierKeys.None;

            // Split modifier keys by the delimiter
            ModifierKeys modifiers = ModifierKeys.None;
            foreach (Range token in modifiersToken.Split('+'))
            {
                ReadOnlySpan<char> modifier = modifiersToken[token].Trim();

                // This would be a case where we have a token like "Ctrl + " for example,
                // which itself is invalid but we choose to support this malformed behaviour.
                if (modifier.IsEmpty)
                    break;

                modifiers |= modifier switch
                {
                    _ when modifier.Equals("Ctrl", StringComparison.OrdinalIgnoreCase) => ModifierKeys.Control,
                    _ when modifier.Equals("Control", StringComparison.OrdinalIgnoreCase) => ModifierKeys.Control,
                    _ when modifier.Equals("Win", StringComparison.OrdinalIgnoreCase) => ModifierKeys.Windows,
                    _ when modifier.Equals("Windows", StringComparison.OrdinalIgnoreCase) => ModifierKeys.Windows,
                    _ when modifier.Equals("Alt", StringComparison.OrdinalIgnoreCase) => ModifierKeys.Alt,
                    _ when modifier.Equals("Shift", StringComparison.OrdinalIgnoreCase) => ModifierKeys.Shift,
                    _ => throw new NotSupportedException(SR.Format(SR.Unsupported_Modifier, modifier.ToString()))
                };
            }

            return modifiers;
        }

        /// <summary>
        /// Converts a <paramref name="value"/> of <see cref="ModifierKeys"/> to its <see langword="string"/> represensation.
        /// </summary>
        /// <param name="context">Serialization Context</param>
        /// <param name="culture">Culture Info</param>
        /// <param name="value">ModifierKeys value</param>
        /// <param name="destinationType">Type to Convert</param>
        /// <returns>A <see langword="string"/> representing the <see cref="ModifierKeys"/> specified by <paramref name="value"/>.</returns>
        public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
        {
            ArgumentNullException.ThrowIfNull(destinationType);

            // We can only convert to string
            if (destinationType != typeof(string))
                throw GetConvertToException(value, destinationType);

            // Check whether value falls within defined set
            ModifierKeys modifiers = (ModifierKeys)value;
            if (!IsDefinedModifierKeys(modifiers))
                throw new InvalidEnumArgumentException(nameof(value), (int)modifiers, typeof(ModifierKeys));

            // This is a fast path for when only a single modifier (or none) is set, which is a very common scenario.
            // Therefore we want a fast path with an allocation free return, taking advantage of interned strings.
            return modifiers switch
            {
                ModifierKeys.None => string.Empty,
                ModifierKeys.Control => "Ctrl",
                ModifierKeys.Alt => "Alt",
                ModifierKeys.Shift => "Shift",
                ModifierKeys.Windows => "Windows",
                // Since we were not able to match a single modifier alone, there must be multiple modifiers involved.
                _ => ConvertMultipleModifiers(modifiers),
            };
        }

        private static string ConvertMultipleModifiers(ModifierKeys modifiers)
        {
            // Ctrl+Alt+Windows+Shift is the maximum char length, though the composition of such value is improbable
            Span<char> modifierSpan = stackalloc char[22];
            int totalLength = 0;

            if (modifiers.HasFlag(ModifierKeys.Control))
                AppendWithDelimiter("Ctrl", ref totalLength, ref modifierSpan);

            if (modifiers.HasFlag(ModifierKeys.Alt))
                AppendWithDelimiter("Alt", ref totalLength, ref modifierSpan);

            if (modifiers.HasFlag(ModifierKeys.Windows))
                AppendWithDelimiter("Windows", ref totalLength, ref modifierSpan);

            if (modifiers.HasFlag(ModifierKeys.Shift))
                AppendWithDelimiter("Shift", ref totalLength, ref modifierSpan);

            //Helper function to concatenate modifiers
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            static void AppendWithDelimiter(string literal, ref int totalLength, ref Span<char> modifierSpan)
            {
                // If this is not the first modifier in the span, we prepend a delimiter (e.g. Ctrl -> Ctrl+Alt)
                if (totalLength > 0)
                {
                    "+".CopyTo(modifierSpan.Slice(totalLength));
                    totalLength++;
                }

                literal.CopyTo(modifierSpan.Slice(totalLength));
                totalLength += literal.Length;
            }

            return new string(modifierSpan.Slice(0, totalLength));
        }

        /// <summary>
        /// Check whether values in <paramref name="modifierKeys"/> are valid, as any number can be casted to the enum.
        /// </summary>
        /// <returns>
        /// <see langword="true"/> if the given <paramref name="modifierKeys"/> are all members of <see cref="ModifierKeys"/>, <see langword="false"/> otherwise.
        /// </returns>
        public static bool IsDefinedModifierKeys(ModifierKeys modifierKeys)
        {
            return modifierKeys == ModifierKeys.None || (((int)modifierKeys & ~(int)ModifiersAllBitsSet) == 0);
        }

        /// <summary>
        /// Specifies all bits of the <see cref="ModifierKeys"/> enum set.
        /// </summary>
        private const ModifierKeys ModifiersAllBitsSet = ModifierKeys.Windows | ModifierKeys.Shift | ModifierKeys.Alt | ModifierKeys.Control;

    }
}
