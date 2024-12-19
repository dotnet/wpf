// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.Design.Serialization;
using System.Linq;
using System.Reflection;
using Xunit;

namespace System.Xaml.Replacements.Tests;

public class TypeUriConverterTests
{
    [Fact]
    public void LookupTypeConverter_Uri_ReturnsExpected()
    {
        // Simulate resetting the static cache with reflection.
        // This avoids the need to spawn additional processes etc.
        static void ResetCache(TypeDescriptionProvider provider)
        {
            if (provider != null)
            {
                TypeDescriptor.RemoveProvider(provider, typeof(Uri));
            }
            Type builtinType = typeof(XamlType).Assembly.GetType("System.Xaml.Schema.BuiltInValueConverter")!;
            FieldInfo uriField = builtinType.GetField("s_Uri", BindingFlags.Static | BindingFlags.NonPublic)!;
            uriField.SetValue(null, null);
        }

        var normalType = new XamlType(typeof(Uri), new XamlSchemaContext());
        Assert.IsType<UriTypeConverter>(normalType.TypeConverter.ConverterInstance);
        ResetCache(null!);

        foreach (Type converterType in new Type[] { typeof(CantConvertToStringConverter),typeof(CantConvertToUriConverter), typeof(CantConvertToInstanceDescriptorConverter), typeof(CantConvertFromStringConverter), typeof(CantConvertFromUriConverter), typeof(ThrowsNotSupportedExceptionConverter) })
        {
            TypeDescriptionProvider provider = TypeDescriptor.AddAttributes(typeof(Uri), new TypeConverterAttribute(converterType));
            var type = new XamlType(typeof(Uri), new XamlSchemaContext());
            Assert.IsNotType<UriTypeConverter>(type.TypeConverter.ConverterInstance);
            ResetCache(provider);
        }
    }

    [Theory]
    [InlineData(typeof(string), true)]
    [InlineData(typeof(Uri), true)]
    [InlineData(typeof(int), false)]
    [InlineData(typeof(InstanceDescriptor), false)]
    public void CanConvertFrom_Invoke_ReturnsExpected(Type sourceType, bool expected)
    {
        TypeConverter converter = GetTypeUriConverter();
        Assert.Equal(expected, converter.CanConvertFrom(sourceType));
    }

    [Fact]
    public void CanConvertFrom_NullSourceType_ThrowsArgumentNullException()
    {
        TypeConverter converter = GetTypeUriConverter();
        Assert.Throws<ArgumentNullException>("sourceType", () => converter.CanConvertFrom(null!));
    }

    public static IEnumerable<object[]> ConvertFrom_TestData()
    {
        yield return new object[] { "", new Uri("", UriKind.Relative) };
        yield return new object[] { "http://google.com", new Uri("http://google.com", UriKind.Absolute) };
        yield return new object[] { "/path", new Uri("/path", UriKind.Relative) };
        yield return new object[] { "c:\\dir\\file", new Uri("c:\\dir\\file", UriKind.RelativeOrAbsolute) };
        yield return new object[] { "my:scheme/path?query", new Uri("my:scheme/path?query", UriKind.RelativeOrAbsolute) };

        yield return new object[] { new Uri("", UriKind.Relative), new Uri("", UriKind.Relative) };
        yield return new object[] { new Uri("http://google.com", UriKind.Absolute), new Uri("http://google.com", UriKind.Absolute) };
        yield return new object[] { new Uri("/path", UriKind.Relative), new Uri("/path", UriKind.Relative) };
        yield return new object[] { new Uri("c:\\dir\\file", UriKind.RelativeOrAbsolute), new Uri("c:\\dir\\file", UriKind.RelativeOrAbsolute) };
        yield return new object[] { new Uri("my:scheme/path?query", UriKind.Absolute), new Uri("my:scheme/path?query", UriKind.Absolute) };
        yield return new object[] { new Uri("my:scheme/path?query", UriKind.RelativeOrAbsolute), new Uri("my:scheme/path?query", UriKind.RelativeOrAbsolute) };
    }

    [Theory]
    [MemberData(nameof(ConvertFrom_TestData))]
    public void ConvertFrom_ValidObject_ReturnsExpected(object value, Uri expected)
    {
        TypeConverter converter = GetTypeUriConverter();
        Uri actual = Assert.IsType<Uri>(converter.ConvertFrom(value));
        Assert.Equal(expected, actual);
        Assert.Equal(expected.IsAbsoluteUri, actual.IsAbsoluteUri);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(null)]
    public void ConvertFrom_InvalidObject_ThrowsNotSupportedException(object? value)
    {
        TypeConverter converter = GetTypeUriConverter();
        Assert.Throws<NotSupportedException>(() => converter.ConvertFrom(value!));
    }

    [Theory]
    [InlineData(null, false)]
    [InlineData(typeof(string), true)]
    [InlineData(typeof(int), false)]
    [InlineData(typeof(InstanceDescriptor), true)]
    public void CanConvertTo_Invoke_ReturnsExpected(Type? sourceType, bool expected)
    {
        TypeConverter converter = GetTypeUriConverter();
        Assert.Equal(expected, converter.CanConvertTo(sourceType));
    }

    [Fact]
    public void ConvertTo_String_ReturnsExpected()
    {
        var uri = new Uri("http://google.com");
        TypeConverter converter = GetTypeUriConverter();
        Assert.Equal("http://google.com", converter.ConvertTo(uri, typeof(string)));
    }

    public static IEnumerable<object[]> ConvertTo_Uri_TestData()
    {        
        yield return new object[] { new Uri("", UriKind.Relative), new Uri("", UriKind.Relative) };
        yield return new object[] { new Uri("http://google.com", UriKind.Absolute), new Uri("http://google.com", UriKind.Absolute) };
        yield return new object[] { new Uri("/path", UriKind.Relative), new Uri("/path", UriKind.Relative) };
        yield return new object[] { new Uri("c:\\dir\\file", UriKind.RelativeOrAbsolute), new Uri("c:\\dir\\file", UriKind.RelativeOrAbsolute) };
        yield return new object[] { new Uri("my:scheme/path?query", UriKind.Absolute), new Uri("my:scheme/path?query", UriKind.Absolute) };
        yield return new object[] { new Uri("my:scheme/path?query", UriKind.RelativeOrAbsolute), new Uri("my:scheme/path?query", UriKind.Absolute) };
    }

    [Theory]
    [MemberData(nameof(ConvertTo_Uri_TestData))]
    public void ConvertTo_Uri_ReturnsExpected(Uri value, Uri expected)
    {
        TypeConverter converter = GetTypeUriConverter();
        Assert.Equal(expected, converter.ConvertTo(value, typeof(Uri)));
    }

    public static IEnumerable<object[]> ConvertTo_InstanceDescriptor_TestData()
    {        
        yield return new object[] { new Uri("", UriKind.Relative), UriKind.Relative };
        yield return new object[] { new Uri("http://google.com", UriKind.Absolute), UriKind.Absolute };
        yield return new object[] { new Uri("/path", UriKind.Relative), UriKind.Relative };
        yield return new object[] { new Uri("c:\\dir\\file", UriKind.RelativeOrAbsolute), UriKind.RelativeOrAbsolute };
        yield return new object[] { new Uri("my:scheme/path?query", UriKind.Absolute), UriKind.Absolute };
        yield return new object[] { new Uri("my:scheme/path?query", UriKind.RelativeOrAbsolute), UriKind.Absolute };
    }

    [Theory]
    [MemberData(nameof(ConvertTo_InstanceDescriptor_TestData))]
    public void ConvertTo_InstanceDescriptor_ReturnsExpected(Uri value, UriKind expectedKind)
    {
        TypeConverter converter = GetTypeUriConverter();
        InstanceDescriptor descriptor = Assert.IsType<InstanceDescriptor>(converter.ConvertTo(value, typeof(InstanceDescriptor)));
        ParameterInfo[] parameters = Assert.IsAssignableFrom<ConstructorInfo>(descriptor.MemberInfo).GetParameters();
        Assert.Equal(new Type[] { typeof(string), typeof(UriKind) }, parameters.Select(p => p.ParameterType));
        Assert.Equal(new object[] { value.OriginalString, expectedKind }, descriptor.Arguments);
        Assert.True(descriptor.IsComplete);
    }
    
    [Theory]
    [InlineData("notUri")]
    [InlineData(null)]
    public void ConvertTo_NotUri_ReturnsExpected(object? value)
    {
        TypeConverter converter = GetTypeUriConverter();
        Assert.Equal(value ?? string.Empty, converter.ConvertTo(value, typeof(string)));
    }
    
    [Theory]
    [InlineData(typeof(int))]
    public void ConvertTo_InvalidType_ThrowsNotSupportedException(Type destinationType)
    {
        TypeConverter converter = GetTypeUriConverter();
        Assert.Throws<NotSupportedException>(() => converter.ConvertTo(new Uri("http://google.com"), destinationType));
    }

    [Fact]
    public void ConvertTo_NullDestinationType_ThrowsArgumentNullException()
    {
        TypeConverter converter = GetTypeUriConverter();
        Assert.Throws<ArgumentNullException>("destinationType", () => converter.ConvertTo(new Uri("http://google.com"), null!));
    }

    public static IEnumerable<object?[]> IsValid_TestData()
    {
        yield return new object?[] { null, false };
        yield return new object?[] { "", true };
        yield return new object?[] { "http://google.com", true };
        yield return new object?[] { "/", true };
        yield return new object?[] { "path", true };
        yield return new object?[] { new Uri("http://google.com", UriKind.Absolute), true };
        yield return new object?[] { new Uri("/path", UriKind.RelativeOrAbsolute), true };
        yield return new object?[] { new Uri("path", UriKind.RelativeOrAbsolute), true };
    }

    [Theory]
    [MemberData(nameof(IsValid_TestData))]
    public void IsValid_Invoke_ReturnsExpected(object value, bool expected)
    {
        TypeConverter converter = GetTypeUriConverter();
        Assert.Equal(expected, converter.IsValid(value));
    }


    private static TypeConverter GetTypeUriConverter()
    {
        // The converter for type Uri is cached in a static field.
        // 
        Type type = typeof(XamlType).Assembly.GetType("System.Xaml.Replacements.TypeUriConverter")!;
        return (TypeConverter)Activator.CreateInstance(type)!;
    }

    public class CantConvertToStringConverter : TypeConverter
    {
        public override bool CanConvertTo(ITypeDescriptorContext? context, Type? destinationType)
        {
            return destinationType == typeof(Uri) || destinationType == typeof(InstanceDescriptor);
        }
        
        public override bool CanConvertFrom(ITypeDescriptorContext? context, Type sourceType)
        {
            return sourceType == typeof(string) || sourceType == typeof(Uri);
        }
    }

    public class CantConvertToUriConverter : TypeConverter
    {
        public override bool CanConvertTo(ITypeDescriptorContext? context, Type? destinationType)
        {
            return destinationType == typeof(string) || destinationType == typeof(InstanceDescriptor);
        }
        
        public override bool CanConvertFrom(ITypeDescriptorContext? context, Type sourceType)
        {
            return sourceType == typeof(string) || sourceType == typeof(Uri);
        }
    }

    public class CantConvertToInstanceDescriptorConverter : TypeConverter
    {
        public override bool CanConvertTo(ITypeDescriptorContext? context, Type? destinationType)
        {
            return destinationType == typeof(string) || destinationType == typeof(Uri);
        }
        
        public override bool CanConvertFrom(ITypeDescriptorContext? context, Type sourceType)
        {
            return sourceType == typeof(string) || sourceType == typeof(Uri);
        }
    }

    public class CantConvertFromStringConverter : TypeConverter
    {
        public override bool CanConvertTo(ITypeDescriptorContext? context, Type? destinationType)
        {
            return destinationType == typeof(string) || destinationType == typeof(Uri);
        }
        
        public override bool CanConvertFrom(ITypeDescriptorContext? context, Type sourceType)
        {
            return sourceType == typeof(Uri);
        }
    }

    public class CantConvertFromUriConverter : TypeConverter
    {
        public override bool CanConvertTo(ITypeDescriptorContext? context, Type? destinationType)
        {
            return destinationType == typeof(string) || destinationType == typeof(Uri);
        }
        
        public override bool CanConvertFrom(ITypeDescriptorContext? context, Type sourceType)
        {
            return sourceType == typeof(string);
        }
    }

    public class ThrowsNotSupportedExceptionConverter : TypeConverter
    {
        public override bool CanConvertTo(ITypeDescriptorContext? context, Type? destinationType)
        {
            throw new NotSupportedException();
        }
        
        public override bool CanConvertFrom(ITypeDescriptorContext? context, Type sourceType)
        {
            return sourceType == typeof(string) || sourceType == typeof(Uri);
        }
    }
}
