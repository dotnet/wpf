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

namespace System.Windows
{
    public readonly struct ThemeMode : IEquatable<ThemeMode>
    {
        public static ThemeMode None => new ThemeMode();
        public static ThemeMode Light => new ThemeMode("Light");
        public static ThemeMode Dark => new ThemeMode("Dark");
        public static ThemeMode System => new ThemeMode("System");


        public string Value => _value ?? "None";

        public ThemeMode(string value) => _value = value;
        
        public bool Equals(ThemeMode other)
        {
            return string.Equals(_value, other._value, StringComparison.Ordinal);
        }

        public override bool Equals(object obj)
        {
            return obj is ThemeMode other && Equals(other);
        }

        public override int GetHashCode()
        {
            return _value != null ? StringComparer.Ordinal.GetHashCode(_value) : 0;
        }

        public static bool operator ==(ThemeMode left, ThemeMode right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(ThemeMode left, ThemeMode right)
        {
            return !left.Equals(right);
        }

        public override string ToString()
        {
            return Value;
        }

        private readonly string _value;
    }


    internal class ThemeModeConverter: TypeConverter
    {

        #region Public Methods

        public override bool CanConvertFrom(ITypeDescriptorContext typeDescriptorContext, Type sourceType)
        {
           return Type.GetTypeCode(sourceType) == TypeCode.String;
        }

        public override bool CanConvertTo(ITypeDescriptorContext typeDescriptorContext, Type destinationType) 
        {
            // We can convert to an InstanceDescriptor or to a string.
            if (destinationType == typeof(InstanceDescriptor) ||
                destinationType == typeof(string)) 
            {
                return true;
            }
            else
            {
                return false;
            }
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

            if (    value != null
                &&  value is ThemeMode )
            {
                if (destinationType == typeof(string)) 
                { 
                    return ((ThemeMode)value).Value;
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
