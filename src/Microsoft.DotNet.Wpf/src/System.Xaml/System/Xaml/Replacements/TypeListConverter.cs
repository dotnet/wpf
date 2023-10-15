// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.ComponentModel;
using System.Globalization;
using System.Windows.Markup;

namespace System.Xaml.Replacements
{
    /// <summary>
    /// TypeConverter for System.Type[]
    /// </summary>
    internal class TypeListConverter : TypeConverter
    {
        private static readonly TypeTypeConverter s_typeTypeConverter = new TypeTypeConverter();

        public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
            => sourceType == typeof(string);

        public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
        {
            if (context != null && value is string typeList)
            {
                string[] tl = StringHelpers.SplitTypeList(typeList);
                Type[] types = new Type[tl.Length];
                for (int i = 0; i < tl.Length; i++)
                {
                    types[i] = (Type)s_typeTypeConverter.ConvertFrom(context, TypeConverterHelper.InvariantEnglishUS, tl[i]);
                }

                return types;
            }

            return base.ConvertFrom(context, culture, value);
        }
    }

    internal static class StringHelpers
    {
        public static string[] SplitTypeList(string typeList) => Array.Empty<string>();
    }
}
