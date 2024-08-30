// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.ComponentModel;
using System.ComponentModel.Design.Serialization;
using System.Reflection;
using System.Globalization;

namespace System.Windows.Media
{
    #region PixelFormatConverter
    //------------------------------------------------------------------------------
    // PixelFormatConverter
    //------------------------------------------------------------------------------
    /// <summary>
    /// PixelFormatConverter
    /// </summary>
    public sealed class PixelFormatConverter : TypeConverter
    {
        /// <summary>
        /// CanConvertFrom - Returns whether or not this class can convert from a given type
        /// </summary>
        public override bool CanConvertFrom(ITypeDescriptorContext td, Type t)
        {
            // We can only handle string
            return t == typeof(string);
        }

        /// <summary>
        /// TypeConverter method override.
        /// </summary>
        /// <param name="context">ITypeDescriptorContext</param>
        /// <param name="destinationType">Type to convert to</param>
        /// <returns>true if conversion is possible</returns>
        public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType)
        {
            return destinationType == typeof(InstanceDescriptor) || destinationType == typeof(string) || base.CanConvertTo(context, destinationType);
        }

        /// <summary>
        /// test
        /// </summary>
        public new object ConvertFromString(string value)
        {
            return value is not null ? new PixelFormat(value) : null;
        }

        /// <summary>
        /// test
        /// </summary>
        public override object ConvertFrom(ITypeDescriptorContext td, CultureInfo ci, object o)
        {
            return o is not null ? new PixelFormat(o as string) : null;
        }

        /// <summary>
        /// TypeConverter method implementation.
        /// </summary>
        /// <param name="context">ITypeDescriptorContext</param>
        /// <param name="culture">current culture (see CLR specs)</param>
        /// <param name="value">value to convert from</param>
        /// <param name="destinationType">Type to convert to</param>
        /// <returns>converted value</returns>
        public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
        {
            ArgumentNullException.ThrowIfNull(destinationType);
            ArgumentNullException.ThrowIfNull(value);

            if (value is not PixelFormat pixelFormat)
                throw new ArgumentException(SR.Format(SR.General_Expected_Type, nameof(PixelFormat)));

            if (destinationType == typeof(InstanceDescriptor))
            {
                ConstructorInfo ci = typeof(PixelFormat).GetConstructor(new Type[] { typeof(string) });
                return new InstanceDescriptor(ci, new object[] { pixelFormat.ToString() });
            }
            else if (destinationType == typeof(string))
            {
                return pixelFormat.ToString();
            }

            return base.ConvertTo(context, culture, value, destinationType);
        }
    }
#endregion // PixelFormatConverter
}
