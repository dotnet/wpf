// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

//
//
// Description: MouseActionConverter - Serializes a MouseAction 


using System.ComponentModel;    // for TypeConverter
using System.Windows.Markup;

namespace System.Windows.Input
{
    /// <summary>
    /// MouseActionValueSerializer - Serializes a MouseAction
    /// </summary>
    public class MouseActionValueSerializer : ValueSerializer
    {
        /// <summary>
        /// CanConvertFromString()
        /// </summary>
        /// <param name="value"></param>
        /// <param name="context"></param>
        /// <returns></returns>
        /// <ExternalAPI/> 
        public override bool CanConvertFromString(string value, IValueSerializerContext context) 
        {
            return true;
        }

        /// <summary>
        /// CanConvertToString()
        /// </summary>
        /// <param name="value"></param>
        /// <param name="context"></param>
        /// <returns></returns>
        /// <ExternalAPI/> 
        public override bool CanConvertToString(object value, IValueSerializerContext context) 
        {
            return value is MouseAction && MouseActionConverter.IsDefinedMouseAction((MouseAction)value);
        }

        /// <summary>
        /// ConvertFromString()
        /// </summary>
        /// <param name="value"></param>
        /// <param name="context"></param>
        /// <returns></returns>
        public override object ConvertFromString(string value, IValueSerializerContext context) 
        {
            TypeConverter converter = TypeDescriptor.GetConverter(typeof(MouseAction));
            if (converter != null)
                return converter.ConvertFromString(value);
            else
                return base.ConvertFromString(value, context);
        }

        /// <summary>
        /// ConvertToString()
        /// </summary>
        /// <param name="value"></param>
        /// <param name="context"></param>
        /// <returns></returns>
        public override string ConvertToString(object value, IValueSerializerContext context) 
        {
            TypeConverter converter = TypeDescriptor.GetConverter(typeof(MouseAction));
            if (converter != null)
                return converter.ConvertToInvariantString(value);
            else
                return base.ConvertToString(value, context);
        }
    }
}
