// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//

using System;
using System.ComponentModel;
using System.ComponentModel.Design.Serialization;
using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using System.Security;

namespace System.Windows.Media.Animation
{
    /// <summary>
    /// 
    /// </summary>
    public sealed class RepeatBehaviorConverter : TypeConverter
    {
        #region Data

        private static char[] _iterationCharacter = new char[] { 'x' };

        #endregion

        /// <summary>
        /// CanConvertFrom - Returns whether or not this class can convert from a given type
        /// </summary>
        /// <ExternalAPI/>
        public override bool CanConvertFrom(ITypeDescriptorContext td, Type t)
        {
            if (t == typeof(string))
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// TypeConverter method override.
        /// </summary>
        /// <param name="context">ITypeDescriptorContext</param>
        /// <param name="destinationType">Type to convert to</param>
        /// <returns>true if conversion is possible</returns>
        /// <ExternalAPI/>
        public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType)
        {
            if (   destinationType == typeof(InstanceDescriptor)
                || destinationType == typeof(string))
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// ConvertFrom
        /// </summary>
        /// <ExternalAPI/>
        public override object ConvertFrom(
            ITypeDescriptorContext td, 
            CultureInfo cultureInfo, 
            object value)
        {
            string stringValue = value as string;

            if (stringValue != null)
            {
                stringValue = stringValue.Trim();

                if (stringValue == "Forever")
                {
                    return RepeatBehavior.Forever;
                }
                else if (   stringValue.Length > 0
                         && stringValue[stringValue.Length - 1] == _iterationCharacter[0])
                {
                    string stringDoubleValue = stringValue.TrimEnd(_iterationCharacter);

                    double doubleValue = (double)TypeDescriptor.GetConverter(typeof(double)).ConvertFrom(td, cultureInfo, stringDoubleValue);

                    return new RepeatBehavior(doubleValue);
                }
            }

            // The value is not Forever or an iteration count so it's either a TimeSpan
            // or we'll let the TimeSpanConverter raise the appropriate exception.

            TimeSpan timeSpanValue = (TimeSpan)TypeDescriptor.GetConverter(typeof(TimeSpan)).ConvertFrom(td, cultureInfo, stringValue);

            return new RepeatBehavior(timeSpanValue);
        }

        /// <summary>
        /// TypeConverter method implementation.
        /// </summary>
        /// <param name="context">ITypeDescriptorContext</param>
        /// <param name="cultureInfo">current culture (see CLR specs)</param>
        /// <param name="value">value to convert from</param>
        /// <param name="destinationType">Type to convert to</param>
        /// <returns>converted value</returns>
        /// <ExternalAPI/>
        public override object ConvertTo(
            ITypeDescriptorContext context, 
            CultureInfo cultureInfo, 
            object value, 
            Type destinationType)
        {
            if (   value is RepeatBehavior
                && destinationType != null)
            {
                RepeatBehavior repeatBehavior = (RepeatBehavior)value;

                if (destinationType == typeof(InstanceDescriptor))
                {
                    MemberInfo mi;

                    if (repeatBehavior == RepeatBehavior.Forever)
                    {
                        mi = typeof(RepeatBehavior).GetProperty("Forever");

                        return new InstanceDescriptor(mi, null);
                    }
                    else if (repeatBehavior.HasCount)
                    {
                        mi = typeof(RepeatBehavior).GetConstructor(new Type[] { typeof(double) });

                        return new InstanceDescriptor(mi, new object[] { repeatBehavior.Count });
                    }
                    else if (repeatBehavior.HasDuration)
                    {
                        mi = typeof(RepeatBehavior).GetConstructor(new Type[] { typeof(TimeSpan) });

                        return new InstanceDescriptor(mi, new object[] { repeatBehavior.Duration });
                    }
                    else
                    {
                        Debug.Fail("Unknown type of RepeatBehavior passed to RepeatBehaviorConverter.");
                    }
                }
                else if (destinationType == typeof(string))
                {
                    return repeatBehavior.InternalToString(null, cultureInfo);
                }
            }

            // We can't do the conversion, let the base class raise the
            // appropriate exception.

            return base.ConvertTo(context, cultureInfo, value, destinationType);
        }
    }
}
