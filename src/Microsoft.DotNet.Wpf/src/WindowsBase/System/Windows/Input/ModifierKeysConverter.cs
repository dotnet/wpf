// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.ComponentModel;
using System.Globalization;

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
            if (context is null || context.Instance is not ModifierKeys modifiers)
                return false;

            return IsDefinedModifierKeys(modifiers);           
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

            // We can only convert to string
            if (destinationType != typeof(string))
                throw GetConvertToException(value, destinationType);

            // Check whether value falls within defined set
            ModifierKeys modifiers = (ModifierKeys)value;
            if (!IsDefinedModifierKeys(modifiers))
                throw new InvalidEnumArgumentException(nameof(value), (int)modifiers, typeof(ModifierKeys));

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
        ///     Check for Valid enum, as any int can be casted to the enum.
        /// </summary>
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
