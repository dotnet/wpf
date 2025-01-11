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
                ReadOnlySpan<char> mouseActionToken = trimmedSource.Slice(index + 1).Trim();
                ReadOnlySpan<char> modifiersToken = trimmedSource.Slice(0, index).Trim();

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

            if (destinationType != typeof(string))
                throw GetConvertToException(value, destinationType);

            // Following checks are here to match the previous behavior
            if (value is null)
                return string.Empty;

            if (value is not MouseGesture mouseGesture)
                throw GetConvertToException(value, destinationType);

            string mouseAction = (string)s_mouseActionConverter.ConvertTo(context, culture, mouseGesture.MouseAction, destinationType);

            if (mouseGesture.Modifiers is ModifierKeys.None)
                return mouseAction;

            ReadOnlySpan<char> modifierSpan = ModifierKeysConverter.ConvertMultipleModifiers(mouseGesture.Modifiers, stackalloc char[22]);

            // This will result in "Ctrl+" in case MouseAction was None but that's fine
            return string.Create(CultureInfo.InvariantCulture, stackalloc char[42], $"{modifierSpan}+{mouseAction}");
        }
    }
}


