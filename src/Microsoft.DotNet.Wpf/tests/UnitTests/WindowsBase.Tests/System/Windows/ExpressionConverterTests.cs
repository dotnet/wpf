// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel;
using System.ComponentModel.Design.Serialization;
using System.Globalization;

namespace System.Windows.Tests;

public class ExpressionConverterTests
{
    [Theory]
    [InlineData(null)]
    [InlineData(typeof(object))]
    [InlineData(typeof(string))]
    [InlineData(typeof(InstanceDescriptor))]
    [InlineData(typeof(Expression))]
    public void CanConvertTo_Invoke_ReturnsFalse(Type? destinationType)
    {
        var converter = new ExpressionConverter();
        Assert.False(converter.CanConvertTo(null, destinationType));
        Assert.False(converter.CanConvertTo(new CustomTypeDescriptorContext(), destinationType));
    }

    public static IEnumerable<object?[]> ConvertTo_TestData()
    {
        // TODO: this should not throw NullReferenceException
        //yield return new object?[] { null, null };
        //yield return new object?[] { string.Empty, null };
        //yield return new object?[] { "value", null };
        //yield return new object?[] { new object(), null };
        
        yield return new object?[] { null, typeof(object) };
        yield return new object?[] { string.Empty, typeof(object) };
        yield return new object?[] { "value", typeof(object) };
        yield return new object?[] { new object(), typeof(object) };
        
        yield return new object?[] { null, typeof(string) };
        yield return new object?[] { string.Empty, typeof(string) };
        yield return new object?[] { "value", typeof(string) };
        yield return new object?[] { new object(), typeof(string) };
        
        yield return new object?[] { null, typeof(Expression) };
        yield return new object?[] { string.Empty, typeof(Expression) };
        yield return new object?[] { "value", typeof(Expression) };
        yield return new object?[] { new object(), typeof(Expression) };
    }

    [Theory]
    [MemberData(nameof(ConvertTo_TestData))]
    public void ConvertTo_Invoke_ThrowsNotSupportedException(object value, Type destinationType)
    {
        var converter = new ExpressionConverter();
        Assert.Throws<NotSupportedException>(() => converter.ConvertTo(value, destinationType));
        Assert.Throws<NotSupportedException>(() => converter.ConvertTo(null, null, value, destinationType));
        Assert.Throws<NotSupportedException>(() => converter.ConvertTo(new CustomTypeDescriptorContext(), CultureInfo.InvariantCulture, value, destinationType));
    }

    public static IEnumerable<object?[]> ConvertTo_NullDestinationType_TestData()
    {
        yield return new object?[] { null };
        yield return new object?[] { string.Empty };
        yield return new object?[] { "value" };
        yield return new object?[] { new object() };
    }

    [Theory]
    [MemberData(nameof(ConvertTo_NullDestinationType_TestData))]
    public void ConvertTo_NullDestinationType_ThrowsNullReferenceException(object value)
    {
        // TODO: this should not throw NullReferenceException
        var converter = new ExpressionConverter();
        Assert.Throws<NullReferenceException>(() => converter.ConvertTo(value, null!));
        Assert.Throws<NullReferenceException>(() => converter.ConvertTo(null, null, value, null!));
        Assert.Throws<NullReferenceException>(() => converter.ConvertTo(new CustomTypeDescriptorContext(), CultureInfo.InvariantCulture, value, null!));
    }
    
    [Theory]
    [InlineData(null)]
    [InlineData(typeof(object))]
    [InlineData(typeof(string))]
    [InlineData(typeof(InstanceDescriptor))]
    [InlineData(typeof(Expression))]
    public void CanConvertFrom_Invoke_ReturnsFalse(Type? sourceType)
    {
        var converter = new ExpressionConverter();
        Assert.False(converter.CanConvertFrom(sourceType!));
        Assert.False(converter.CanConvertFrom(null, sourceType));
        Assert.False(converter.CanConvertFrom(new CustomTypeDescriptorContext(), sourceType));
    }

    public static IEnumerable<object?[]> ConvertFrom_CantConvert_TestData()
    {
        yield return new object?[] { null };
        yield return new object?[] { new object() };
    }
    
    [Theory]
    [MemberData(nameof(ConvertFrom_CantConvert_TestData))]
    public void ConvertFrom_CantConvert_ThrowsNotSupportedException(object value)
    {
        var converter = new ExpressionConverter();
        Assert.Throws<NotSupportedException>(() => converter.ConvertFrom(value));
        Assert.Throws<NotSupportedException>(() => converter.ConvertFrom(null, null, value));
        Assert.Throws<NotSupportedException>(() => converter.ConvertFrom(new CustomTypeDescriptorContext(), CultureInfo.InvariantCulture, value));
    }

    private class CustomTypeDescriptorContext : ITypeDescriptorContext
    {
        public IContainer Container => throw new NotImplementedException();

        public object Instance => throw new NotImplementedException();

        public PropertyDescriptor PropertyDescriptor => throw new NotImplementedException();

        public object? GetService(Type serviceType) => throw new NotImplementedException();

        public void OnComponentChanged() => throw new NotImplementedException();

        public bool OnComponentChanging() => throw new NotImplementedException();
    }
}
