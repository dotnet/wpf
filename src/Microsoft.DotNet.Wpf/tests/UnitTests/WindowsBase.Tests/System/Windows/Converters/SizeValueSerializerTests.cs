// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel;
using System.Windows.Markup;
using System.Windows.Tests;

namespace System.Windows.Converters.Tests;

public class SizeValueSerializerTests
{
    public static IEnumerable<object?[]> CanConvertToString_TestData()
    {
        yield return new object?[] { null, false };
        yield return new object?[] { string.Empty, false };
        yield return new object?[] { "value", false };
        yield return new object?[] { new object(), false };
        
        yield return new object?[] { new Size(), true };
    }

    [Theory]
    [MemberData(nameof(CanConvertToString_TestData))]
    public void CanConvertToString_Invoke_ReturnsExpected(object value, bool expected)
    {
        var serializer = new SizeValueSerializer();
        Assert.Equal(expected, serializer.CanConvertToString(value, null));
        Assert.Equal(expected, serializer.CanConvertToString(value, new CustomValueSerializerContext()));
    }
    
    [Theory]
    [MemberData(nameof(SizeTests.ToString_TestData), MemberType = typeof(SizeTests))]
    public void ConvertToString_Invoke_ReturnsExpected(Size matrix, string expected)
    {
        var serializer = new SizeValueSerializer();
        Assert.Equal(expected, serializer.ConvertToString(matrix, null));
        Assert.Equal(expected, serializer.ConvertToString(matrix, new CustomValueSerializerContext()));
    }

    public static IEnumerable<object?[]> ConvertToString_CantConvert_TestData()
    {
        yield return new object?[] { null };
        yield return new object?[] { string.Empty };
        yield return new object?[] { "value" };
        yield return new object?[] { new object() };
    }

    [Theory]
    [MemberData(nameof(ConvertToString_CantConvert_TestData))]
    public void ConvertToString_CantConvert_ThrowsNotSupportedException(object value)
    {
        var serializer = new SizeValueSerializer();
        Assert.Throws<NotSupportedException>(() => serializer.ConvertToString(value, null));
        Assert.Throws<NotSupportedException>(() => serializer.ConvertToString(value, new CustomValueSerializerContext()));
    }

    public static IEnumerable<object?[]> CanConvertFromString_TestData()
    {
        yield return new object?[] { null };
        yield return new object?[] { string.Empty };
        yield return new object?[] { "value" };
    }

    [Theory]
    [MemberData(nameof(CanConvertFromString_TestData))]
    public void CanConvertFromString_Invoke_ReturnsTrue(string value)
    {
        var serializer = new SizeValueSerializer();
        Assert.True(serializer.CanConvertFromString(value, null));
        Assert.True(serializer.CanConvertFromString(value, new CustomValueSerializerContext()));
    }

    [Theory]
    [MemberData(nameof(SizeTests.Parse_TestData), MemberType = typeof(SizeTests))]
    public void ConvertFromString_Invoke_ReturnsExpected(string value, Size expected)
    {
        var serializer = new SizeValueSerializer();
        Assert.Equal(expected, serializer.ConvertFromString(value, null));
        Assert.Equal(expected, serializer.ConvertFromString(value, new CustomValueSerializerContext()));
    }

    [Fact]
    public void ConvertFromString_NullValue_ThrowsNotSupportedException()
    {
        var serializer = new SizeValueSerializer();
        Assert.Throws<NotSupportedException>(() => serializer.ConvertFromString(null, null));
        Assert.Throws<NotSupportedException>(() => serializer.ConvertFromString(null, new CustomValueSerializerContext()));
    }

    [Theory]
    [MemberData(nameof(SizeTests.Parse_InvalidSource_TestData), MemberType = typeof(SizeTests))]
    public void ConvertFromString_InvalidValue_ThrowsInvalidOperationException(string value)
    {
        var serializer = new SizeValueSerializer();
        Assert.Throws<InvalidOperationException>(() => serializer.ConvertFromString(value, null));
        Assert.Throws<InvalidOperationException>(() => serializer.ConvertFromString(value, new CustomValueSerializerContext()));
    }

    [Theory]
    [MemberData(nameof(SizeTests.Parse_NotDouble_TestData), MemberType = typeof(SizeTests))]
    public void ConvertFromString_NotDouble_ThrowsFormatException(string value)
    {
        var serializer = new SizeValueSerializer();
        Assert.Throws<FormatException>(() => serializer.ConvertFromString(value, null));
        Assert.Throws<FormatException>(() => serializer.ConvertFromString(value, new CustomValueSerializerContext()));
    }

    [Theory]
    [MemberData(nameof(SizeTests.Parse_Negative_TestData), MemberType = typeof(SizeTests))]
    public void ConvertFromString_Negative_ThrowsArgumentException(string value)
    {
        var serializer = new SizeValueSerializer();
        Assert.Throws<ArgumentException>(() => serializer.ConvertFromString(value, null));
        Assert.Throws<ArgumentException>(() => serializer.ConvertFromString(value, new CustomValueSerializerContext()));
    }

    private class CustomValueSerializerContext : IValueSerializerContext
    {
        public IContainer Container => throw new NotImplementedException();

        public object Instance => throw new NotImplementedException();

        public PropertyDescriptor PropertyDescriptor => throw new NotImplementedException();

        public object? GetService(Type serviceType) => throw new NotImplementedException();

        public ValueSerializer GetValueSerializerFor(PropertyDescriptor descriptor) => throw new NotImplementedException();

        public ValueSerializer GetValueSerializerFor(Type type) => throw new NotImplementedException();

        public void OnComponentChanged() => throw new NotImplementedException();

        public bool OnComponentChanging() => throw new NotImplementedException();
    }
}
