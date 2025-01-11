// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel;    // for TypeConverter
using System.Globalization;     // for CultureInfo

namespace System.Windows.Input
{
    /// <summary>
    /// Converter class for converting between a <see cref="string"/> and <see cref="MouseGesture"/>.
    /// </summary>
    public class MouseGestureConverter : TypeConverter
    {
        /// <summary>
        /// To aid with conversion from <see cref="MouseAction"/> to <see cref="string"/>. 
        /// </summary>
        private static readonly MouseActionConverter s_mouseActionConverter = new();

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
            if (context?.Instance is not MouseGesture mouseGesture)
                return false;

            return ModifierKeysConverter.IsDefinedModifierKeys(mouseGesture.Modifiers) &&
                   MouseActionConverter.IsDefinedMouseAction(mouseGesture.MouseAction);
        }

        /// <summary>
        /// Converts <paramref name="source"/> of <see cref="string"/> type to its <see cref="MouseGesture"/> represensation.
        /// </summary>
        /// <param name="context">This parameter is ignored during the call.</param>
        /// <param name="culture">This parameter is ignored during the call.</param>
        /// <param name="source">The object to convert to a <see cref="MouseGesture"/>.</param>
        /// <returns>
        /// A new instance of <see cref="MouseGesture"/> class representing the data contained in <paramref name="source"/>.
        /// </returns>
        /// <exception cref="NotSupportedException">Thrown in case the <paramref name="source"/> was not a <see cref="string"/>.</exception>
        public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object source)
        {
            if (source is not string sourceString)
                throw GetConvertFromException(source);

            ReadOnlySpan<char> trimmedSource = sourceString.AsSpan().Trim();

            // Break apart MouseAction and ModifierKeys
            int index = trimmedSource.LastIndexOf('+');
            if (index >= 0)
            {
                ReadOnlySpan<char> mouseActionToken = trimmedSource.Slice(index + 1).Trim();
                ReadOnlySpan<char> modifiersToken = trimmedSource.Slice(0, index).Trim();

                return new MouseGesture(MouseActionConverter.ConvertFromImpl(mouseActionToken), ModifierKeysConverter.ConvertFromImpl(modifiersToken));
            }

            return new MouseGesture(MouseActionConverter.ConvertFromImpl(trimmedSource), ModifierKeys.None);
        }

        /// <summary>
        /// Attempts to convert a <see cref="MouseGesture"/> object to the <paramref name="destinationType"/>.
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
        /// or <paramref name="value"/> was not a <see cref="MouseGesture"/>.
        /// </exception>
        public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
        {
            ArgumentNullException.ThrowIfNull(destinationType);

            if (destinationType != typeof(string))
                throw GetConvertToException(value, destinationType);

            // Following checks are here to match the previous behavior
            if (value is null)
                return string.Empty;

            if (value is not MouseGesture mouseGesture)
                throw GetConvertToException(value, destinationType);

            string mouseAction = (string)s_mouseActionConverter.ConvertTo(context, culture, mouseGesture.MouseAction, destinationType);

            // This returns the converted MouseAction only
            if (mouseGesture.Modifiers is ModifierKeys.None)
                return mouseAction;

            ReadOnlySpan<char> modifierSpan = ModifierKeysConverter.ConvertMultipleModifiers(mouseGesture.Modifiers, stackalloc char[22]);

            // This will result in "Ctrl+" in case MouseAction was None but that's fine
            return string.Create(CultureInfo.InvariantCulture, stackalloc char[42], $"{modifierSpan}+{mouseAction}");
        }
    }
}
