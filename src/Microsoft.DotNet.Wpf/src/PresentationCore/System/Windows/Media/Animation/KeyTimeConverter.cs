// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//

using System;
using System.ComponentModel;
using System.ComponentModel.Design.Serialization;
using System.Globalization;
using System.Reflection;
using System.Windows.Media.Animation;
using System.Security;

namespace System.Windows
{
    /// <summary>
    /// 
    /// </summary>
    public class KeyTimeConverter : TypeConverter
    {
        #region Data

        private static char[] _percentCharacter = new char[] { '%' };

        #endregion

        /// <summary>
        /// Returns whether or not this class can convert from a given type
        /// to an instance of a KeyTime.
        /// </summary>
        public override bool CanConvertFrom(
            ITypeDescriptorContext typeDescriptorContext, 
            Type type)
        {
            if (type == typeof(string))
            {
                return true;
            }
            else
            {
                return base.CanConvertFrom(
                    typeDescriptorContext,
                    type);
            }
        }

        /// <summary>
        /// Returns whether or not this class can convert from an instance of a
        /// KeyTime to a given type.
        /// </summary>
        public override bool CanConvertTo(
            ITypeDescriptorContext typeDescriptorContext,
            Type type)
        {
            if (   type == typeof(InstanceDescriptor)
                || type == typeof(string))
            {
                return true;
            }
            else
            {
                return base.CanConvertTo(
                    typeDescriptorContext,
                    type);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public override object ConvertFrom(
            ITypeDescriptorContext typeDescriptorContext, 
            CultureInfo cultureInfo, 
            object value)
        {
            string stringValue = value as string;

            if (stringValue != null)
            {
                stringValue = stringValue.Trim();

                if (stringValue == "Uniform")
                {
                    return KeyTime.Uniform;
                }
                else if (stringValue == "Paced")
                {
                    return KeyTime.Paced;
                }
                else if (stringValue[stringValue.Length - 1] == _percentCharacter[0])
                {
                    stringValue = stringValue.TrimEnd(_percentCharacter);

                    double doubleValue = (double)TypeDescriptor.GetConverter(
                        typeof(double)).ConvertFrom(
                            typeDescriptorContext,
                            cultureInfo,
                            stringValue);

                    if (doubleValue == 0.0)
                    {
                        return KeyTime.FromPercent(0.0);
                    }
                    else if (doubleValue == 100.0)
                    {
                        return KeyTime.FromPercent(1.0);
                    }
                    else
                    {
                        return KeyTime.FromPercent(doubleValue / 100.0);
                    }
                }
                else
                {
                    TimeSpan timeSpanValue = (TimeSpan)TypeDescriptor.GetConverter(
                        typeof(TimeSpan)).ConvertFrom(
                            typeDescriptorContext,
                            cultureInfo,
                            stringValue);

                    return KeyTime.FromTimeSpan(timeSpanValue);
                }
            }

            return base.ConvertFrom(
                typeDescriptorContext,
                cultureInfo,
                value);
        }

        /// <summary>
        /// 
        /// </summary>
        public override object ConvertTo(
            ITypeDescriptorContext typeDescriptorContext, 
            CultureInfo cultureInfo, 
            object value, 
            Type destinationType)
        {
            if (   value != null
                && value is KeyTime)
            {
                KeyTime keyTime = (KeyTime)value;

                if (destinationType == typeof(InstanceDescriptor))
                {
                    MemberInfo mi;

                    switch (keyTime.Type)
                    {
                        case KeyTimeType.Percent:

                            mi = typeof(KeyTime).GetMethod("FromPercent", new Type[] { typeof(double) });

                            return new InstanceDescriptor(mi, new object[] { keyTime.Percent });

                        case KeyTimeType.TimeSpan:

                            mi = typeof(KeyTime).GetMethod("FromTimeSpan", new Type[] { typeof(TimeSpan) });

                            return new InstanceDescriptor(mi, new object[] { keyTime.TimeSpan });

                        case KeyTimeType.Uniform:

                            mi = typeof(KeyTime).GetProperty("Uniform");

                            return new InstanceDescriptor(mi, null);

                        case KeyTimeType.Paced:

                            mi = typeof(KeyTime).GetProperty("Paced");

                            return new InstanceDescriptor(mi, null);
                    }
                }
                else if (destinationType == typeof(String))
                {
                    switch (keyTime.Type)
                    {
                        case KeyTimeType.Uniform:

                            return "Uniform";

                        case KeyTimeType.Paced:

                            return "Paced";

                        case KeyTimeType.Percent:

                            string returnValue = (string)TypeDescriptor.GetConverter(
                                typeof(Double)).ConvertTo(
                                    typeDescriptorContext,
                                    cultureInfo,
                                    keyTime.Percent * 100.0,
                                    destinationType);

                            return returnValue + _percentCharacter[0].ToString();

                        case KeyTimeType.TimeSpan:

                            return TypeDescriptor.GetConverter(
                                typeof(TimeSpan)).ConvertTo(
                                    typeDescriptorContext,
                                    cultureInfo,
                                    keyTime.TimeSpan,
                                    destinationType);
                    }
                }
            }

            return base.ConvertTo(
                typeDescriptorContext,
                cultureInfo, 
                value, 
                destinationType);
        }
    }
}
