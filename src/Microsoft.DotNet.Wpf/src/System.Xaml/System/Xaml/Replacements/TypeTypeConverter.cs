// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.ComponentModel;
using System.Globalization;
using System.Windows.Markup;
using System.Xaml.Schema;

namespace System.Xaml.Replacements
{
    /// <summary>
    /// TypeConverter for System.Type
    /// </summary>
    internal class TypeTypeConverter : TypeConverter
    {
        public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
            => sourceType == typeof(string);

        public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
        {
            if (context != null && value is string typeName)
            {
                IXamlTypeResolver typeResolver = GetService<IXamlTypeResolver>(context);
                if (typeResolver != null)
                {
                    return typeResolver.Resolve(typeName);
                }
            }

            return base.ConvertFrom(context, culture, value);
        }

        public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType)
            => destinationType == typeof(string);

        public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
        {
            if (context != null && value is Type type && destinationType == typeof(string))
            {
                string result = ConvertTypeToString(context, type);
                if (result != null)
                {
                    return result;
                }
            }

            return base.ConvertTo(context, culture, value, destinationType);
        }

        private static string ConvertTypeToString(ITypeDescriptorContext context, Type type)
        {
            IXamlSchemaContextProvider schemaContextProvider = GetService<IXamlSchemaContextProvider>(context);
            if (schemaContextProvider == null)
            {
                return null;
            }
            if (schemaContextProvider.SchemaContext == null)
            {
                return null;
            }

            XamlType xamlType = schemaContextProvider.SchemaContext.GetXamlType(type);
            if (xamlType == null)
            {
                return null;
            }

            return XamlTypeTypeConverter.ConvertXamlTypeToString(context, xamlType);
        }

        private static TService GetService<TService>(ITypeDescriptorContext context) where TService : class
            => context.GetService(typeof(TService)) as TService;
    }
}
