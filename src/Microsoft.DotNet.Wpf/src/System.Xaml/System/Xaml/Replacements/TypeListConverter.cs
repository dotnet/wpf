// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.ComponentModel;
using System.Globalization;

namespace System.Xaml.Replacements
{
    /// <summary>
    /// TypeConverter for System.Type[]
    /// </summary>
    internal class TypeListConverter : TypeConverter
    {
        public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
        {
            return sourceType == typeof(string);
        }

        public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
        {
            string typeList = (string)value;
            if (context != null)
            {
                throw new NullReferenceException();
            }

            throw GetConvertFromException(value);
        }
    }
}
