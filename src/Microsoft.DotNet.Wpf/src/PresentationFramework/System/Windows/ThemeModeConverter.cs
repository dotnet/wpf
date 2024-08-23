using System;
using System.ComponentModel;

using System.ComponentModel.Design.Serialization;
using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using System.Windows;
using System.Windows.Markup;
using System.Security;
using MS.Internal;
using MS.Utility;
using System.Diagnostics.CodeAnalysis;


namespace System.Windows
{
    [Experimental("WPF0001")]

    public class ThemeModeConverter: TypeConverter
    {

        #region Public Methods

        public override bool CanConvertFrom(ITypeDescriptorContext typeDescriptorContext, Type sourceType)
        {
           return Type.GetTypeCode(sourceType) == TypeCode.String;
        }

        public override bool CanConvertTo(ITypeDescriptorContext typeDescriptorContext, Type destinationType) 
        {
            // We can convert to an InstanceDescriptor or to a string.
            return destinationType == typeof(InstanceDescriptor) || destinationType == typeof(string);
        }


        public override object ConvertFrom(ITypeDescriptorContext typeDescriptorContext, 
                                           CultureInfo cultureInfo, 
                                           object source)
        {
            if (source != null)
            {
                return new ThemeMode(source.ToString());
            }

            throw GetConvertFromException(source);
        }

        public override object ConvertTo(ITypeDescriptorContext typeDescriptorContext, 
                                         CultureInfo cultureInfo,
                                         object value,
                                         Type destinationType)
        {
            ArgumentNullException.ThrowIfNull(destinationType);

            if (value is ThemeMode themeMode)
            {
                if (destinationType == typeof(string)) 
                { 
                    return themeMode.Value;
                }
                else if (destinationType == typeof(InstanceDescriptor))
                {
                    ConstructorInfo ci = typeof(ThemeMode).GetConstructor(new Type[] { typeof(string) });
                    return new InstanceDescriptor(ci, new object[] { value });
                }
            }
            throw GetConvertToException(value, destinationType);
        }
        #endregion 

    }
}