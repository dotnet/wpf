// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel;
using System.Windows.Markup;

namespace System.Windows.Input.Tests;

public class ModifierKeysValueSerializerTests
{
    public static IEnumerable<object?[]> CanConvertToString_TestData()
    {
        yield return new object?[] { null, false };
        yield return new object?[] { string.Empty, false };
        yield return new object?[] { "value", false };
        yield return new object?[] { new object(), false };

        yield return new object?[] { ModifierKeys.None, true };
        yield return new object?[] { ModifierKeys.Control, true };
        yield return new object?[] { ModifierKeys.Control | ModifierKeys.Alt | ModifierKeys.Windows | ModifierKeys.Shift, true };
        yield return new object?[] { (ModifierKeys)(-1), false };
        yield return new object?[] { (ModifierKeys)0x10, false };
    }

    [Theory]
    [MemberData(nameof(CanConvertToString_TestData))]
    public void CanConvertToString_Invoke_ReturnsExpected(object value, bool expected)
    {
        var serializer = new ModifierKeysValueSerializer();
        Assert.Equal(expected, serializer.CanConvertToString(value, null));
        Assert.Equal(expected, serializer.CanConvertToString(value, new CustomValueSerializerContext()));
    }

    [Theory]
    [MemberData(nameof(ModifierKeysConverterTests.ConvertTo_ModifierKeysToString_TestData), MemberType = typeof(ModifierKeysConverterTests))]
    public void ConvertToString_Invoke_ReturnsExpected(ModifierKeys key, string expected)
    {
        var serializer = new ModifierKeysValueSerializer();
        Assert.Equal(expected, serializer.ConvertToString(key, null));
        Assert.Equal(expected, serializer.ConvertToString(key, new CustomValueSerializerContext()));
    }

    [Theory]
    [MemberData(nameof(ModifierKeysConverterTests.ConvertTo_InvalidModifierKeys_TestData), MemberType = typeof(ModifierKeysConverterTests))]
    public void ConvertToString_InvalidModifierKeys_ThrowsInvalidEnumArgumentException(ModifierKeys key)
    {
        var serializer = new ModifierKeysValueSerializer();
        Assert.Throws<InvalidEnumArgumentException>("value", () => serializer.ConvertToString(key, null));
        Assert.Throws<InvalidEnumArgumentException>("value", () => serializer.ConvertToString(key, new CustomValueSerializerContext()));
    }

    [Theory]
    [InlineData(null)]
    // TODO: this should not throw InvalidCastException.
    //[InlineData("", "")]
    //[InlineData("value", "value")]
    public void ConvertToString_InvokeNotModifierKeysToStringNull_ThrowsNotSupportedException(object? value)
    {
        var serializer = new ModifierKeysValueSerializer();
        // TODO: should not throw NullReferenceException
        //Assert.Throws<NotSupportedException>(() => serializer.ConvertToString(value, null));
        //Assert.Throws<NotSupportedException>(() => serializer.ConvertToString(value, new CustomValueSerializerContext()));
        Assert.Throws<NullReferenceException>(() => serializer.ConvertToString(value, null));
        Assert.Throws<NullReferenceException>(() => serializer.ConvertToString(value, new CustomValueSerializerContext()));
    }

    [Theory]
    [InlineData("")]
    [InlineData("value")]
    public void ConvertToString_InvokeNotModifierKeysNotNull_ThrowsInvalidCastException(object value)
    {
        // TODO: this should not throw InvalidCastException.
        var serializer = new ModifierKeysValueSerializer();
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
        var serializer = new ModifierKeysValueSerializer();
        Assert.True(serializer.CanConvertFromString(value, null));
        Assert.True(serializer.CanConvertFromString(value, new CustomValueSerializerContext()));
    }

    [Theory]
    [MemberData(nameof(ModifierKeysConverterTests.ConvertFrom_TestData), MemberType = typeof(ModifierKeysConverterTests))]
    public void ConvertFromString_Invoke_ReturnsExpected(string value, ModifierKeys expected)
    {
        var serializer = new ModifierKeysValueSerializer();
        Assert.Equal(expected, serializer.ConvertFromString(value, null));
        Assert.Equal(expected, serializer.ConvertFromString(value, new CustomValueSerializerContext()));
    }

    [Fact]
    public void ConvertFromString_NullValue_ThrowsNotSupportedException()
    {
        var serializer = new ModifierKeysValueSerializer();
        Assert.Throws<NotSupportedException>(() => serializer.ConvertFromString(null, null));
        Assert.Throws<NotSupportedException>(() => serializer.ConvertFromString(null, new CustomValueSerializerContext()));
    }

    [Theory]
    [MemberData(nameof(ModifierKeysConverterTests.ConvertFrom_InvalidValue_TestData), MemberType = typeof(ModifierKeysConverterTests))]
    public void ConvertFromString_InvalidValue_ThrowsNotSupportedException(string value)
    {
        var serializer = new ModifierKeysValueSerializer();
        Assert.Throws<NotSupportedException>(() => serializer.ConvertFromString(value, null));
        Assert.Throws<NotSupportedException>(() => serializer.ConvertFromString(value, new CustomValueSerializerContext()));
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
