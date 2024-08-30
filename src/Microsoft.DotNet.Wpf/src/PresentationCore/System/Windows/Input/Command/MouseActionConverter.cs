// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
//
//
// Description: MouseActionConverter - Converts a MouseAction string 
//              to the *Type* that the string represents 
//

using System;
using System.ComponentModel;    // for TypeConverter
using System.Globalization;     // for CultureInfo
using System.Reflection;
using MS.Internal;
using System.Windows;
using System.Windows.Input;
using MS.Utility;

using SR=MS.Internal.PresentationCore.SR;

namespace System.Windows.Input
{
    /// <summary>
    /// MouseAction - Converter class for converting between a string and the Type of a MouseAction
    /// </summary>
    public class MouseActionConverter : TypeConverter
    {
        ///<summary>
        /// CanConvertFrom - Used to check whether we can convert a string into a MouseAction
        ///</summary>
        ///<param name="context">ITypeDescriptorContext</param>
        ///<param name="sourceType">type to convert from</param>
        ///<returns>true if the given type can be converted, false otherwise</returns>
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


        ///<summary>
        ///TypeConverter method override. 
        ///</summary>
        ///<param name="context">ITypeDescriptorContext</param>
        ///<param name="destinationType">Type to convert to</param>
        ///<returns>true if conversion	is possible</returns>
        public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType)
        {
            // We can convert to an InstanceDescriptor or to a string.
            if (destinationType == typeof(string))
            {
                // When invoked by the serialization engine we can convert to string only for known type
                if (context != null && context.Instance != null)
                {
                    return (MouseActionConverter.IsDefinedMouseAction((MouseAction)context.Instance));
                 }
            }
            return false;
        }

        /// <summary>
        /// ConvertFrom()
        /// </summary>
        /// <param name="context">Parser Context</param>
        /// <param name="culture">Culture Info</param>
        /// <param name="source">MouseAction String</param>
        /// <returns></returns>
        public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object source)
        {
            if (source is not string mouseAction)
                throw GetConvertFromException(source);

            ReadOnlySpan<char> mouseActionToken = mouseAction.AsSpan().Trim();
            return mouseActionToken switch
            {
                _ when mouseActionToken.IsEmpty => MouseAction.None, //Special casing as produced by "ConvertTo"
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
        /// ConvertTo()
        /// </summary>
        /// <param name="context">Serialization Context</param>
        /// <param name="culture">Culture Info</param>
        /// <param name="value">MouseAction value </param>
        /// <param name="destinationType">Type to Convert</param>
        /// <returns>string if parameter is a MouseAction</returns>
        public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
        {
            ArgumentNullException.ThrowIfNull(destinationType);

            if (value is null || destinationType != typeof(string))
                throw GetConvertToException(value, destinationType);

            return (MouseAction)value switch
            {
                MouseAction.None => string.Empty,
                MouseAction.LeftClick => "LeftClick",
                MouseAction.RightClick => "RightClick",
                MouseAction.MiddleClick => "MiddleClick",
                MouseAction.WheelClick => "WheelClick",
                MouseAction.LeftDoubleClick => "LeftDoubleClick",
                MouseAction.RightDoubleClick => "RightDoubleClick",
                MouseAction.MiddleDoubleClick => "MiddleDoubleClick",
                _ => throw new InvalidEnumArgumentException(nameof(value), (int)value, typeof(MouseAction))
            };
        }

        // Helper like Enum.IsDefined, for MouseAction.
        internal static bool IsDefinedMouseAction(MouseAction mouseAction)
        {
            return mouseAction >= MouseAction.None && mouseAction <= MouseAction.MiddleDoubleClick;
        }
    }
}
