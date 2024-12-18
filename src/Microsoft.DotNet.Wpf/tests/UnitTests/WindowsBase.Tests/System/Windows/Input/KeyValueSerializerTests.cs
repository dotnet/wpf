// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel;
using System.Windows.Markup;

namespace System.Windows.Input.Tests;

public class KeyValueSerializerTests
{
    public static IEnumerable<object?[]> CanConvertToString_TestData()
    {
        yield return new object?[] { null, false };
        yield return new object?[] { string.Empty, false };
        yield return new object?[] { "value", false };
        yield return new object?[] { new object(), false };
        yield return new object?[] { ModifierKeys.None, false };

        yield return new object?[] { Key.None, true };
        yield return new object?[] { Key.Cancel, true };
        yield return new object?[] { Key.A, true };
        yield return new object?[] { Key.Pa1, true };
        yield return new object?[] { Key.OemClear, true };
        yield return new object?[] { Key.DeadCharProcessed, false };
        yield return new object?[] { Key.None - 1, false };
        yield return new object?[] { Key.DeadCharProcessed + 1, false };
    }

    [Theory]
    [MemberData(nameof(CanConvertToString_TestData))]
    public void CanConvertToString_Invoke_ReturnsExpected(object value, bool expected)
    {
        var serializer = new KeyValueSerializer();
        Assert.Equal(expected, serializer.CanConvertToString(value, null));
        Assert.Equal(expected, serializer.CanConvertToString(value, new CustomValueSerializerContext()));
    }

    [Theory]
    [MemberData(nameof(KeyConverterTests.ConvertTo_KeyToString_TestData), MemberType = typeof(KeyConverterTests))]
    public void ConvertToString_Invoke_ReturnsExpected(Key key, string expected)
    {
        var serializer = new KeyValueSerializer();
        Assert.Equal(expected, serializer.ConvertToString(key, null));
        Assert.Equal(expected, serializer.ConvertToString(key, new CustomValueSerializerContext()));
    }

    [Theory]
    [InlineData((Key)int.MinValue)]
    [InlineData((Key)(-1))]
    [InlineData(Key.DeadCharProcessed + 1)]
    [InlineData((Key)int.MaxValue)]
    public void ConvertToString_InvalidKey_ThrowsNotSupportedException(Key key)
    {
        var serializer = new KeyValueSerializer();
        Assert.Throws<NotSupportedException>(() => serializer.ConvertToString(key, null));
        Assert.Throws<NotSupportedException>(() => serializer.ConvertToString(key, new CustomValueSerializerContext()));
    }

    [Theory]
    [InlineData(null)]
    // TODO: this should not throw InvalidCastException.
    //[InlineData("", "")]
    //[InlineData("value", "value")]
    public void ConvertToString_InvokeNotKeyToStringNull_ThrowsNotSupportedException(object? value)
    {
        var serializer = new KeyValueSerializer();
        Assert.Throws<NotSupportedException>(() => serializer.ConvertToString(value, null));
        Assert.Throws<NotSupportedException>(() => serializer.ConvertToString(value, new CustomValueSerializerContext()));
    }

    [Theory]
    [InlineData("")]
    [InlineData("value")]
    public void ConvertToString_InvokeNotKeyNotNull_ThrowsInvalidCastException(object value)
    {
        // TODO: this should not throw InvalidCastException.
        var serializer = new KeyValueSerializer();
        Assert.Throws<InvalidCastException>(() => serializer.ConvertToString(value, null));
        Assert.Throws<InvalidCastException>(() => serializer.ConvertToString(value, new CustomValueSerializerContext()));
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
        var serializer = new KeyValueSerializer();
        Assert.True(serializer.CanConvertFromString(value, null));
        Assert.True(serializer.CanConvertFromString(value, new CustomValueSerializerContext()));
    }

    [Theory]
    [MemberData(nameof(KeyConverterTests.ConvertFrom_TestData), MemberType = typeof(KeyConverterTests))]
    public void ConvertFromString_Invoke_ReturnsExpected(string value, Key expected)
    {
        var serializer = new KeyValueSerializer();
        Assert.Equal(expected, serializer.ConvertFromString(value, null));
        Assert.Equal(expected, serializer.ConvertFromString(value, new CustomValueSerializerContext()));
    }

    [Fact]
    public void ConvertFromString_NullValue_ThrowsNotSupportedException()
    {
        var serializer = new KeyValueSerializer();
        Assert.Throws<NotSupportedException>(() => serializer.ConvertFromString(null, null));
        Assert.Throws<NotSupportedException>(() => serializer.ConvertFromString(null, new CustomValueSerializerContext()));
    }

    [Theory]
    [MemberData(nameof(KeyConverterTests.ConvertFrom_InvalidValue_TestData), MemberType = typeof(KeyConverterTests))]
    public void ConvertFromString_InvalidValue_ThrowsInvalidOperationException(string value)
    {
        var serializer = new KeyValueSerializer();
        // TODO: add paramName.
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
