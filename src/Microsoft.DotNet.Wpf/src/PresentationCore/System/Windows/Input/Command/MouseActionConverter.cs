// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.ComponentModel;
using System.Globalization;

namespace System.Windows.Input
{
    /// <summary>
    /// Converter class for converting between a <see langword="string"/> and <see cref="MouseAction"/>.
    /// </summary>
    public class MouseActionConverter : TypeConverter
    {
        ///<summary>
        /// Used to check whether we can convert a <see langword="string"/> into a <see cref="MouseAction"/>.
        ///</summary>
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
            // We can convert to an InstanceDescriptor or to a string
            if (destinationType != typeof(string))
                return false;

            // When invoked by the serialization engine we can convert to string only for known type
            if (context is null || context.Instance is null)
                return false;

            // Make sure the value falls within defined set
            return IsDefinedMouseAction((MouseAction)context.Instance);
        }

        /// <summary>
        /// Converts <paramref name="source"/> of <see langword="string"/> type to its <see cref="MouseAction"/> represensation.
        /// </summary>
        /// <param name="context">Parser Context</param>
        /// <param name="culture">Culture Info</param>
        /// <param name="source">MouseAction String</param>
        /// <returns>A <see cref="MouseAction"/> representing the <see langword="string"/> specified by <paramref name="source"/>.</returns>
        public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object source)
        {
            if (source is not string mouseAction)
                throw GetConvertFromException(source);

            ReadOnlySpan<char> mouseActionToken = mouseAction.AsSpan().Trim();
            return mouseActionToken switch
            {
                _ when mouseActionToken.IsEmpty => MouseAction.None, // Special casing as produced by "ConvertTo"
                _ when mouseActionToken.Equals("None", StringComparison.OrdinalIgnoreCase) => MouseAction.None,
                _ when mouseActionToken.Equals("LeftClick", StringComparison.OrdinalIgnoreCase) => MouseAction.LeftClick,
                _ when mouseActionToken.Equals("RightClick", StringComparison.OrdinalIgnoreCase) => MouseAction.RightClick,
                _ when mouseActionToken.Equals("MiddleClick", StringComparison.OrdinalIgnoreCase) => MouseAction.MiddleClick,
                _ when mouseActionToken.Equals("WheelClick", StringComparison.OrdinalIgnoreCase) => MouseAction.WheelClick,
                _ when mouseActionToken.Equals("LeftDoubleClick", StringComparison.OrdinalIgnoreCase) => MouseAction.LeftDoubleClick,
                _ when mouseActionToken.Equals("RightDoubleClick", StringComparison.OrdinalIgnoreCase) => MouseAction.RightDoubleClick,
                _ when mouseActionToken.Equals("MiddleDoubleClick", StringComparison.OrdinalIgnoreCase) => MouseAction.MiddleDoubleClick,
                _ => throw new NotSupportedException(SR.Format(SR.Unsupported_MouseAction, mouseActionToken.ToString()))
            };
        }

        /// <summary>
        /// Converts a <paramref name="value"/> of <see cref="MouseAction"/> to its <see langword="string"/> represensation.
        /// </summary>
        /// <param name="context">Serialization Context</param>
        /// <param name="culture">Culture Info</param>
        /// <param name="value">MouseAction value </param>
        /// <param name="destinationType">Type to Convert</param>
        /// <returns>A <see langword="string"/> representing the <see cref="MouseAction"/> specified by <paramref name="value"/>.</returns>
        public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
        {
            ArgumentNullException.ThrowIfNull(destinationType);

            if (value is null || destinationType != typeof(string))
                throw GetConvertToException(value, destinationType);

            MouseAction mouseAction = (MouseAction)value;
            return mouseAction switch
            {
                MouseAction.None => string.Empty,
                MouseAction.LeftClick => "LeftClick",
                MouseAction.RightClick => "RightClick",
                MouseAction.MiddleClick => "MiddleClick",
                MouseAction.WheelClick => "WheelClick",
                MouseAction.LeftDoubleClick => "LeftDoubleClick",
                MouseAction.RightDoubleClick => "RightDoubleClick",
                MouseAction.MiddleDoubleClick => "MiddleDoubleClick",
                _ => throw new InvalidEnumArgumentException(nameof(value), (int)mouseAction, typeof(MouseAction))
            };
        }

        /// <summary>
        /// Helper function similar to <see cref="Enum.IsDefined{MouseAction}(MouseAction)"/>, just lighter and faster.
        /// </summary>
        /// <param name="mouseAction">The value to test against.</param>
        /// <returns><see langword="true"/> if <paramref name="mouseAction"/> falls in enumeration range, <see langword="false"/> otherwise.</returns>
        internal static bool IsDefinedMouseAction(MouseAction mouseAction)
        {
            return mouseAction >= MouseAction.None && mouseAction <= MouseAction.MiddleDoubleClick;
        }
    }
}
