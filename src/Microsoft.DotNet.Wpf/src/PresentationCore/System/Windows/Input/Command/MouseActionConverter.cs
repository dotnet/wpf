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
using SRID=MS.Internal.PresentationCore.SRID;

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
            if (source != null && source is string)
            {
                string mouseActionToken = ((string)source).Trim();
                mouseActionToken = mouseActionToken.ToUpper(CultureInfo.InvariantCulture);
                if (mouseActionToken == String.Empty)
                    return MouseAction.None;
                
                MouseAction mouseAction = MouseAction.None;
                switch (mouseActionToken)
                {
                    case "LEFTCLICK"        : mouseAction = MouseAction.LeftClick; break;
                    case "RIGHTCLICK"       : mouseAction = MouseAction.RightClick; break;
                    case "MIDDLECLICK"      : mouseAction = MouseAction.MiddleClick; break;
                    case "WHEELCLICK"       : mouseAction = MouseAction.WheelClick; break;
                    case "LEFTDOUBLECLICK"  : mouseAction = MouseAction.LeftDoubleClick; break;
                    case "RIGHTDOUBLECLICK" : mouseAction = MouseAction.RightDoubleClick; break;
                    case "MIDDLEDOUBLECLICK": mouseAction = MouseAction.MiddleDoubleClick; break;
                    default :
                        throw new NotSupportedException(SR.Get(SRID.Unsupported_MouseAction, mouseActionToken));
                }
                return mouseAction;
            }
            throw GetConvertFromException(source);
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
            if (destinationType == null)
                throw new ArgumentNullException("destinationType");

            if (destinationType == typeof(string) && value != null)
            {
                MouseAction mouseActionValue  = (MouseAction)value ;
                if (MouseActionConverter.IsDefinedMouseAction(mouseActionValue))
                {
                    string mouseAction = null;
                    switch (mouseActionValue)
                    {
                        case MouseAction.None             : mouseAction=String.Empty; break;
                        case MouseAction.LeftClick        : mouseAction="LeftClick"; break;
                        case MouseAction.RightClick       : mouseAction="RightClick"; break;
                        case MouseAction.MiddleClick      : mouseAction="MiddleClick"; break;
                        case MouseAction.WheelClick       : mouseAction="WheelClick"; break;
                        case MouseAction.LeftDoubleClick  : mouseAction="LeftDoubleClick"; break;
                        case MouseAction.RightDoubleClick : mouseAction="RightDoubleClick"; break;
                        case MouseAction.MiddleDoubleClick: mouseAction="MiddleDoubleClick"; break;
                    }
                    if (mouseAction != null)
                        return mouseAction;
                }
                throw new InvalidEnumArgumentException("value", (int)mouseActionValue, typeof(MouseAction));
            }
            throw GetConvertToException(value,destinationType);
        }

        // Helper like Enum.IsDefined,  for MouseAction.
        internal static bool IsDefinedMouseAction(MouseAction mouseAction)
        {
            return (mouseAction >= MouseAction.None && mouseAction <= MouseAction.MiddleDoubleClick);
        }
    }
}
