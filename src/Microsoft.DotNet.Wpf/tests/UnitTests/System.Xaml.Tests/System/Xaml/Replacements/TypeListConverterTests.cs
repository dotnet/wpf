// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.ComponentModel;
using System.ComponentModel.Design.Serialization;
using Xunit;

namespace System.Xaml.Replacements.Tests;

public class TypeListConverterTests
{
    [Theory]
    [InlineData(null, false)]
    [InlineData(typeof(string), true)]
    [InlineData(typeof(int), false)]
    [InlineData(typeof(InstanceDescriptor), false)]
    public void CanConvertFrom_Invoke_ReturnsExpected(Type? sourceType, bool expected)
    {
        var type = new XamlType(typeof(Type[]), new XamlSchemaContext());
        TypeConverter converter = type.TypeConverter.ConverterInstance;
        Assert.Equal(expected, converter.CanConvertFrom(sourceType!));
    }

    [Fact]
    public void ConvertFrom_String_ReturnsEmpty()
    {
        var type = new XamlType(typeof(Type[]), new XamlSchemaContext());
        TypeConverter converter = type.TypeConverter.ConverterInstance;
        Assert.Empty(Assert.IsType<Type[]>(converter.ConvertFrom(new CustomTypeDescriptorContext(), null, "value")));
        Assert.Throws<NotSupportedException>(() => converter.ConvertFrom("value"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData(1)]
    public void ConvertFrom_InvalidObject_ThrowsNotSupportedException(object? value)
    {
        var type = new XamlType(typeof(Type[]), new XamlSchemaContext());
        TypeConverter converter = type.TypeConverter.ConverterInstance;
        Assert.Throws<NotSupportedException>(() => converter.ConvertFrom(value!));
        Assert.Throws<NotSupportedException>(() => converter.ConvertFrom(new CustomTypeDescriptorContext(), null, value!));
    }

    [Theory]
    [InlineData(null, false)]
    [InlineData(typeof(string), true)]
    [InlineData(typeof(int), false)]
    [InlineData(typeof(InstanceDescriptor), false)]
    public void CanConvertTo_Invoke_ReturnsExpected(Type? sourceType, bool expected)
    {
        var type = new XamlType(typeof(Type[]), new XamlSchemaContext());
        TypeConverter converter = type.TypeConverter.ConverterInstance;
        Assert.Equal(expected, converter.CanConvertTo(sourceType));
    }
    
    [Theory]
    [InlineData("notTypeList")]
    [InlineData(null)]
    public void ConvertTo_NotTypeArray_ReturnsExpected(object? value)
    {
        var type = new XamlType(typeof(Type[]), new XamlSchemaContext());
        TypeConverter converter = type.TypeConverter.ConverterInstance;
        Assert.Equal(value ?? string.Empty, converter.ConvertTo(value, typeof(string)));
    }
    
    [Theory]
    [InlineData(typeof(int))]
    public void ConvertTo_InvalidType_ThrowsNotSupportedException(Type destinationType)
    {
        var type = new XamlType(typeof(Type[]), new XamlSchemaContext());
        TypeConverter converter = type.TypeConverter.ConverterInstance;
        Assert.Throws<NotSupportedException>(() => converter.ConvertTo(Array.Empty<Type>(), destinationType));
    }

    [Fact]
    public void ConvertTo_NullDestinationType_ThrowsArgumentNullException()
    {
        var type = new XamlType(typeof(Type[]), new XamlSchemaContext());
        TypeConverter converter = type.TypeConverter.ConverterInstance;
        Assert.Throws<ArgumentNullException>("destinationType", () => converter.ConvertTo(Array.Empty<Type>(), null!));
    }
    
    private class CustomTypeDescriptorContext : ITypeDescriptorContext
    {
        public IContainer Container => throw new NotImplementedException();

        public object Instance => throw new NotImplementedException();

        public PropertyDescriptor PropertyDescriptor => throw new NotImplementedException();

        public Func<Type, object?>? GetServiceAction { get; set; }

        public object? GetService(Type serviceType) => throw new NotImplementedException();

        public void OnComponentChanged() => throw new NotImplementedException();

        public bool OnComponentChanging() => throw new NotImplementedException();
    }
}
