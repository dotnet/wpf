// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.ComponentModel;
using System.ComponentModel.Design.Serialization;
using System.Globalization;
using System.Windows.Markup;
using System.Xaml.Replacements;

namespace System.Xaml.Schema
{
    internal class BuiltInValueConverter<TConverterBase> : XamlValueConverter<TConverterBase>
        where TConverterBase : class
    {
        private Func<TConverterBase> _factory;

        internal BuiltInValueConverter(Type converterType, Func<TConverterBase> factory)
            :base(converterType, null)
        {
            _factory = factory;
        }

        internal override bool IsPublic => true;

        protected override TConverterBase CreateInstance() => _factory.Invoke();
    }

    internal static class BuiltInValueConverter
    {
        private static XamlValueConverter<TypeConverter> s_String;
        private static XamlValueConverter<TypeConverter> s_Object;
        private static XamlValueConverter<TypeConverter> s_Int32;
        private static XamlValueConverter<TypeConverter> s_Int16;
        private static XamlValueConverter<TypeConverter> s_Int64;
        private static XamlValueConverter<TypeConverter> s_UInt32;
        private static XamlValueConverter<TypeConverter> s_UInt16;
        private static XamlValueConverter<TypeConverter> s_UInt64;
        private static XamlValueConverter<TypeConverter> s_Boolean;
        private static XamlValueConverter<TypeConverter> s_Double;
        private static XamlValueConverter<TypeConverter> s_Single;
        private static XamlValueConverter<TypeConverter> s_Byte;
        private static XamlValueConverter<TypeConverter> s_SByte;
        private static XamlValueConverter<TypeConverter> s_Char;
        private static XamlValueConverter<TypeConverter> s_Decimal;
        private static XamlValueConverter<TypeConverter> s_TimeSpan;
        private static XamlValueConverter<TypeConverter> s_Guid;
        private static XamlValueConverter<TypeConverter> s_Type;
        private static XamlValueConverter<TypeConverter> s_TypeList;
        private static XamlValueConverter<TypeConverter> s_DateTime;
        private static XamlValueConverter<TypeConverter> s_DateTimeOffset;
        private static XamlValueConverter<TypeConverter> s_CultureInfo;
        private static XamlValueConverter<ValueSerializer> s_StringSerializer;
        private static XamlValueConverter<TypeConverter> s_Delegate;
        private static XamlValueConverter<TypeConverter> s_Uri;

        internal static XamlValueConverter<TypeConverter> Int32
            => s_Int32 ??= new BuiltInValueConverter<TypeConverter>(typeof(Int32Converter), () => new Int32Converter());

        internal static XamlValueConverter<TypeConverter> String
            => s_String ??= new BuiltInValueConverter<TypeConverter>(typeof(StringConverter), () => new StringConverter());

        internal static XamlValueConverter<TypeConverter> Object
            => s_Object ??= new XamlValueConverter<TypeConverter>(null, XamlLanguage.Object);

        internal static XamlValueConverter<TypeConverter> Event
            => s_Delegate ??= new BuiltInValueConverter<TypeConverter>(typeof(EventConverter), () => new EventConverter());

        internal static XamlValueConverter<TypeConverter> GetTypeConverter(Type targetType)
        {
            if (typeof(string) == targetType)
            {
                return String;
            }
            if (typeof(object) == targetType)
            {
                return Object;
            }
            if (typeof(Int32) == targetType)
            {
                return Int32;
            }
            if (typeof(Int16) == targetType)
            {
                return s_Int16 ??= new BuiltInValueConverter<TypeConverter>(typeof(Int16Converter), () => new Int16Converter());
            }
            if (typeof(Int64) == targetType)
            {
                return s_Int64 ??= new BuiltInValueConverter<TypeConverter>(typeof(Int64Converter), () => new Int64Converter());
            }
            if (typeof(UInt32) == targetType)
            {
                return s_UInt32 ??= new BuiltInValueConverter<TypeConverter>(typeof(UInt32Converter), () => new UInt32Converter());
            }
            if (typeof(UInt16) == targetType)
            {
                return s_UInt16 ??= new BuiltInValueConverter<TypeConverter>(typeof(UInt16Converter), () => new UInt16Converter());
            }
            if (typeof(UInt64) == targetType)
            {
                return s_UInt64 ??= new BuiltInValueConverter<TypeConverter>(typeof(UInt64Converter), () => new UInt64Converter());
            }
            if (typeof(Boolean) == targetType)
            {
                return s_Boolean ??= new BuiltInValueConverter<TypeConverter>(typeof(BooleanConverter), () => new BooleanConverter());
            }
            if (typeof(Double) == targetType)
            {
                return s_Double ??= new BuiltInValueConverter<TypeConverter>(typeof(DoubleConverter), () => new DoubleConverter());
            }
            if (typeof(Single) == targetType)
            {
                return s_Single ??= new BuiltInValueConverter<TypeConverter>(typeof(SingleConverter), () => new SingleConverter());
            }
            if (typeof(Byte) == targetType)
            {
                return s_Byte ??= new BuiltInValueConverter<TypeConverter>(typeof(ByteConverter), () => new ByteConverter());
            }
            if (typeof(SByte) == targetType)
            {
                return s_SByte ??= new BuiltInValueConverter<TypeConverter>(typeof(SByteConverter), () => new SByteConverter());
            }
            if (typeof(Char) == targetType)
            {
                return s_Char ??= new BuiltInValueConverter<TypeConverter>(typeof(CharConverter), () => new CharConverter());
            }
            if (typeof(Decimal) == targetType)
            {
                return s_Decimal ??= new BuiltInValueConverter<TypeConverter>(typeof(DecimalConverter), () => new DecimalConverter());
            }
            if (typeof(TimeSpan) == targetType)
            {
                return s_TimeSpan ??= new BuiltInValueConverter<TypeConverter>(typeof(TimeSpanConverter), () => new TimeSpanConverter());
            }
            if (typeof(Guid) == targetType)
            {
                return s_Guid ??= new BuiltInValueConverter<TypeConverter>(typeof(GuidConverter), () => new GuidConverter());
            }
            if (typeof(Type).IsAssignableFrom(targetType))
            {
                return s_Type ??= new BuiltInValueConverter<TypeConverter>(typeof(System.Xaml.Replacements.TypeTypeConverter), () => new System.Xaml.Replacements.TypeTypeConverter());
            }
            if (typeof(Type[]).IsAssignableFrom(targetType))
            {
                return s_TypeList ??= new BuiltInValueConverter<TypeConverter>(typeof(System.Xaml.Replacements.TypeListConverter), () => new System.Xaml.Replacements.TypeListConverter());
            }
            if (typeof(DateTime) == targetType)
            {
                return s_DateTime ??= new BuiltInValueConverter<TypeConverter>(typeof(System.Xaml.Replacements.DateTimeConverter2), () => new System.Xaml.Replacements.DateTimeConverter2());
            }
            if (typeof(DateTimeOffset) == targetType)
            {
                return s_DateTimeOffset ??= new BuiltInValueConverter<TypeConverter>(typeof(System.Xaml.Replacements.DateTimeOffsetConverter2), () => new System.Xaml.Replacements.DateTimeOffsetConverter2());
            }
            if (typeof(CultureInfo).IsAssignableFrom(targetType))
            {
                return s_CultureInfo ??= new BuiltInValueConverter<TypeConverter>(typeof(CultureInfoConverter), () => new CultureInfoConverter());
            }
            if (typeof(Delegate).IsAssignableFrom(targetType))
            {
                return s_Delegate ??= new BuiltInValueConverter<TypeConverter>(typeof(EventConverter), () => new EventConverter());
            }
            if (typeof(Uri).IsAssignableFrom(targetType))
            {
                if(s_Uri is null)
                {
                    TypeConverter stdConverter = null;
                    try
                    {
                        stdConverter = TypeDescriptor.GetConverter(typeof(Uri));
                        // The TypeConverter for Uri, if one is found, should be capable of converting from { String, Uri }
                        // and converting to { String, Uri, System.ComponentModel.Design.Serialization.InstanceDescriptor }
                        if (stdConverter == null ||
                            !stdConverter.CanConvertFrom(typeof(string)) || !stdConverter.CanConvertFrom(typeof(Uri)) ||
                            !stdConverter.CanConvertTo(typeof(string)) || !stdConverter.CanConvertTo(typeof(Uri)) || !stdConverter.CanConvertTo(typeof(InstanceDescriptor)))
                        {
                            stdConverter = null;
                        }
                    }
                    catch (NotSupportedException)
                    {
                    }

                    if (stdConverter == null)
                    {
                        s_Uri = new BuiltInValueConverter<TypeConverter>(typeof(TypeUriConverter), () => new TypeUriConverter());
                    }
                    else
                    {
                        // There is a built-in TypeConverter available. Very likely, System.UriTypeConverter, but this was not naturally
                        // discovered. this is probably due to the fact that System.Uri does not have [TypeConverterAttribute(typeof(UriConverter))]
                        // in the .NET Core codebase. 
                        // Since a default converter was discovered, just use that instead of our own (very nearly equivalent) implementation.
                        s_Uri = new BuiltInValueConverter<TypeConverter>(stdConverter.GetType(), () => TypeDescriptor.GetConverter(typeof(Uri)));
                    }
                }

                return s_Uri;
            }

            return null;
        }

        internal static XamlValueConverter<ValueSerializer> GetValueSerializer(Type targetType)
        {
            if (typeof(string) == targetType)
            {
                if (s_StringSerializer is null)
                {
                    // Once StringSerializer is TypeForwarded to S.X, this can be made more efficient
                    ValueSerializer stringSerializer = ValueSerializer.GetSerializerFor(typeof(string));
                    s_StringSerializer = new BuiltInValueConverter<ValueSerializer>(stringSerializer.GetType(), () => stringSerializer);
                }

                return s_StringSerializer;
            }

            return null;
        }
    }
}
