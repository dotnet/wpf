// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.ComponentModel;
using System.ComponentModel.Design.Serialization;
using System.Globalization;
using System.Reflection;

namespace System.Xaml.Replacements
{
    /// <summary>
    /// Limited converter for string <--> System.Uri
    /// </summary>
    internal class TypeUriConverter : TypeConverter
    {
        public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
        {
            ArgumentNullException.ThrowIfNull(sourceType);

            return sourceType == typeof(string) || sourceType == typeof(Uri);
        }

        public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType)
        {
            return 
                destinationType == typeof(InstanceDescriptor) ||
                destinationType == typeof(string) ||
                destinationType == typeof(Uri);
        }

        public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
        {
            if (value is Uri uri)
            {
                UriKind uriKind = UriKind.RelativeOrAbsolute;
                if (uri.IsWellFormedOriginalString())
                {
                    uriKind = uri.IsAbsoluteUri ? UriKind.Absolute : UriKind.Relative;
                }

                if (destinationType == typeof(InstanceDescriptor))
                {
                    ConstructorInfo constructor = typeof(Uri).GetConstructor(new Type[] { typeof(string), typeof(UriKind) });
                    return  new InstanceDescriptor(constructor, new object[] { uri.OriginalString, uriKind });
                }
                else if (destinationType == typeof(string))
                {
                    return uri.OriginalString;
                }
                else if (destinationType == typeof(Uri))
                {
                    return new Uri(uri.OriginalString, uriKind);
                }
            }

            return base.ConvertTo(context, culture, value, destinationType);
        }

        public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
        {
            if (value is string uriString)
            {
                if (Uri.IsWellFormedUriString(uriString, UriKind.Absolute))
                {
                    return new Uri(uriString, UriKind.Absolute);
                }

                if (Uri.IsWellFormedUriString(uriString, UriKind.Relative))
                {
                    return new Uri(uriString, UriKind.Relative);
                }

                return new Uri(uriString, UriKind.RelativeOrAbsolute);
            }

            if (value is Uri uri)
            {
                if (uri.IsWellFormedOriginalString())
                {
                    return new Uri(uri.OriginalString, uri.IsAbsoluteUri ? UriKind.Absolute : UriKind.Relative);
                }

                return new Uri(uri.OriginalString, UriKind.RelativeOrAbsolute);
            }

            return base.ConvertFrom(context, culture, value);
        }

        public override bool IsValid(ITypeDescriptorContext context, object value)
        {
            if (value is string uriString)
            {
                return Uri.TryCreate(uriString, UriKind.RelativeOrAbsolute, out _);
            }

            return value is Uri;
        }
    }
}
