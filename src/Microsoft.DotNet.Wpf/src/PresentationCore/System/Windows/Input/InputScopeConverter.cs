// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
//
// Description: class for input scope type-converter
//
// Please refer to the design specfication http://avalon/Cicero/Specifications/Stylable%20InputScope.mht
// 
//

using System;
using System.Collections.Generic;
using System.Windows.Input;
using System.ComponentModel;
using System.Globalization;
using System.ComponentModel.Design.Serialization;


namespace System.Windows.Input
{
    ///<summary>
    /// type-converter which performs type conversions for inputscope
    ///</summary>
    /// <speclink>http://avalon/Cicero/Specifications/Stylable%20InputScope.mht</speclink>
    public class InputScopeConverter : TypeConverter
    {
        ///<summary>
        /// Returns whether this converter can convert an object of one type to InputScope type
        /// InputScopeConverter only supports string type to convert from
        ///</summary>
        ///<param name="context">
        /// The conversion context.
        ///</param>
        ///<param name="sourceType">
        /// The type to convert from.
        ///</param>
        ///<returns>
        /// True if conversion is possible, false otherwise.
        ///</returns>
        public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
        {
            // We can only handle string.
            if (sourceType == typeof(string))
            {
                return true;
            }
            return false;
        }

        ///<summary>
        /// Returns whether this converter can convert the object to the specified type. 
        /// InputScopeConverter only supports string type to convert to
        ///</summary>
        ///<param name="context">
        /// The conversion context.
        ///</param>
        ///<param name="destinationType">
        /// The type to convert to.
        ///</param>
        ///<returns>
        /// True if conversion is possible, false otherwise.
        ///</returns>

         public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType)
        {
#if false
            if (typeof(string) == destinationType)
            {
                if (null == context)
                {
                        return true;
                }
                if (null != context.Instance && context.Instance is DependencyObject)
                {
                    InputScope inputscope = (InputScope)((DependencyObject)context.Instance).GetValue(InputMethod.InputScopeProperty);
                    if (inputscope != null && inputscope.Names.Count == 1)
                    {
                        return true;
                    }
                }
            }
#endif
            return false;
        }

        ///<summary>
        /// Converts the given value to InputScope type
        ///</summary>
        /// <param name="context">
        /// The conversion context.
        /// </param>
        /// <param name="culture">
        /// The current culture that applies to the conversion.
        /// </param>
        /// <param name="source">
        /// The source object to convert from.
        /// </param>
        /// <returns>
        /// InputScope object with a specified scope name, otherwise InputScope with Default scope.
        /// </returns>

        public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object source)
        {
            string stringSource = source as string;
            InputScopeNameValue sn = InputScopeNameValue.Default;
            InputScope inputScope;

            if (null != stringSource)
            {
                stringSource = stringSource.Trim();

                if (-1 != stringSource.LastIndexOf('.'))
                    stringSource = stringSource.Substring(stringSource.LastIndexOf('.')+1);
                    
                if (!stringSource.Equals(String.Empty))
                {
                    sn = (InputScopeNameValue)Enum.Parse(typeof(InputScopeNameValue), stringSource);
                }
            }
            
            inputScope = new InputScope();
            inputScope.Names.Add(new InputScopeName(sn));
            return inputScope;    
        }

#if true
        ///<summary>
        /// Converts the given value as InputScope object to the specified type. 
        /// This converter only supports string type as a type to convert to.
        ///</summary>
        /// <param name="context">
        /// The conversion context.
        /// </param>
        /// <param name="culture">
        /// The current culture that applies to the conversion.
        /// </param>
        /// <param name="value">
        /// The value to convert.
        /// </param>
        /// <param name="destinationType">
        /// The type to convert to.
        /// </param>
        /// <returns>
        /// A new object of the specified type (string) converted from the given InputScope object.
        /// </returns>
        public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
        {
            InputScope inputScope = value as InputScope;
            if (null != destinationType && null != inputScope)
            {
                if (destinationType == typeof(string))
                {
                    return Enum.GetName(typeof(InputScopeNameValue), ((InputScopeName)inputScope.Names[0]).NameValue);
                }
            }
            return base.ConvertTo(context, culture, value, destinationType);
        }
#endif
    }
}
