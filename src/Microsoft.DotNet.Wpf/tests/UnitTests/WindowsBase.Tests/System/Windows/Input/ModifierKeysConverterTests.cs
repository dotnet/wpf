// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel;
using System.ComponentModel.Design.Serialization;
using System.Globalization;

namespace System.Windows.Input.Tests;

public class ModifierKeysConverterTests
{
    public static IEnumerable<object?[]> CanConvertTo_TestData()
    {
        yield return new object?[] { null, null, false };
        yield return new object?[] { null, typeof(object), false };
        yield return new object?[] { null, typeof(string), false };
        yield return new object?[] { null, typeof(InstanceDescriptor), false };
        yield return new object?[] { null, typeof(Key), false };
        yield return new object?[] { null, typeof(ModifierKeys), false };
        yield return new object?[] { new CustomTypeDescriptorContext(), null, false };
        yield return new object?[] { new CustomTypeDescriptorContext(), typeof(object), false };
        yield return new object?[] { new CustomTypeDescriptorContext(), typeof(string), false };
        yield return new object?[] { new CustomTypeDescriptorContext(), typeof(InstanceDescriptor), false };
        yield return new object?[] { new CustomTypeDescriptorContext(), typeof(Key), false };
        yield return new object?[] { new CustomTypeDescriptorContext(), typeof(ModifierKeys), false };
        yield return new object?[] { new CustomTypeDescriptorContext { Instance = new object() }, null, false };
        yield return new object?[] { new CustomTypeDescriptorContext { Instance = new object() }, typeof(object), false };
        yield return new object?[] { new CustomTypeDescriptorContext { Instance = new object() }, typeof(string), false };
        yield return new object?[] { new CustomTypeDescriptorContext { Instance = new object() }, typeof(InstanceDescriptor), false };
        yield return new object?[] { new CustomTypeDescriptorContext { Instance = new object() }, typeof(Key), false };
        yield return new object?[] { new CustomTypeDescriptorContext { Instance = new object() }, typeof(ModifierKeys), false };
        yield return new object?[] { new CustomTypeDescriptorContext { Instance = ModifierKeys.None }, null, false };
        yield return new object?[] { new CustomTypeDescriptorContext { Instance = ModifierKeys.None }, typeof(object), false };
        yield return new object?[] { new CustomTypeDescriptorContext { Instance = ModifierKeys.None }, typeof(string), true };
        yield return new object?[] { new CustomTypeDescriptorContext { Instance = ModifierKeys.None }, typeof(InstanceDescriptor), false };
        yield return new object?[] { new CustomTypeDescriptorContext { Instance = ModifierKeys.None }, typeof(Key), false };
        yield return new object?[] { new CustomTypeDescriptorContext { Instance = ModifierKeys.None }, typeof(ModifierKeys), false };
        yield return new object?[] { new CustomTypeDescriptorContext { Instance = ModifierKeys.Control }, null, false };
        yield return new object?[] { new CustomTypeDescriptorContext { Instance = ModifierKeys.Control }, typeof(object), false };
        yield return new object?[] { new CustomTypeDescriptorContext { Instance = ModifierKeys.Control }, typeof(string), true };
        yield return new object?[] { new CustomTypeDescriptorContext { Instance = ModifierKeys.Control }, typeof(InstanceDescriptor), false };
        yield return new object?[] { new CustomTypeDescriptorContext { Instance = ModifierKeys.Control }, typeof(Key), false };
        yield return new object?[] { new CustomTypeDescriptorContext { Instance = ModifierKeys.Control }, typeof(ModifierKeys), false };
        yield return new object?[] { new CustomTypeDescriptorContext { Instance = ModifierKeys.Control | ModifierKeys.Alt | ModifierKeys.Windows | ModifierKeys.Shift }, null, false };
        yield return new object?[] { new CustomTypeDescriptorContext { Instance = ModifierKeys.Control | ModifierKeys.Alt | ModifierKeys.Windows | ModifierKeys.Shift }, typeof(object), false };
        yield return new object?[] { new CustomTypeDescriptorContext { Instance = ModifierKeys.Control | ModifierKeys.Alt | ModifierKeys.Windows | ModifierKeys.Shift }, typeof(string), true };
        yield return new object?[] { new CustomTypeDescriptorContext { Instance = ModifierKeys.Control | ModifierKeys.Alt | ModifierKeys.Windows | ModifierKeys.Shift }, typeof(InstanceDescriptor), false };
        yield return new object?[] { new CustomTypeDescriptorContext { Instance = ModifierKeys.Control | ModifierKeys.Alt | ModifierKeys.Windows | ModifierKeys.Shift }, typeof(Key), false };
        yield return new object?[] { new CustomTypeDescriptorContext { Instance = ModifierKeys.Control | ModifierKeys.Alt | ModifierKeys.Windows | ModifierKeys.Shift }, typeof(ModifierKeys), false };
        yield return new object?[] { new CustomTypeDescriptorContext { Instance = (ModifierKeys)(-1) }, null, false };
        yield return new object?[] { new CustomTypeDescriptorContext { Instance = (ModifierKeys)(-1) }, typeof(object), false };
        yield return new object?[] { new CustomTypeDescriptorContext { Instance = (ModifierKeys)(-1) }, typeof(string), false };
        yield return new object?[] { new CustomTypeDescriptorContext { Instance = (ModifierKeys)(-1) }, typeof(InstanceDescriptor), false };
        yield return new object?[] { new CustomTypeDescriptorContext { Instance = (ModifierKeys)(-1) }, typeof(Key), false };
        yield return new object?[] { new CustomTypeDescriptorContext { Instance = (ModifierKeys)(-1) }, typeof(ModifierKeys), false };
        yield return new object?[] { new CustomTypeDescriptorContext { Instance = (ModifierKeys)0x10 }, null, false };
        yield return new object?[] { new CustomTypeDescriptorContext { Instance = (ModifierKeys)0x10 }, typeof(object), false };
        yield return new object?[] { new CustomTypeDescriptorContext { Instance = (ModifierKeys)0x10 }, typeof(string), false };
        yield return new object?[] { new CustomTypeDescriptorContext { Instance = (ModifierKeys)0x10 }, typeof(InstanceDescriptor), false };
        yield return new object?[] { new CustomTypeDescriptorContext { Instance = (ModifierKeys)0x10 }, typeof(Key), false };
        yield return new object?[] { new CustomTypeDescriptorContext { Instance = (ModifierKeys)0x10 }, typeof(ModifierKeys), false };
    }

    [Theory]
    [MemberData(nameof(CanConvertTo_TestData))]
    public void CanConvertTo_Invoke_ReturnsExpected(ITypeDescriptorContext context, Type destinationType, bool expected)
    {
        var converter = new ModifierKeysConverter();
        Assert.Equal(expected, converter.CanConvertTo(context, destinationType));
    }

    public static IEnumerable<object[]> ConvertTo_ModifierKeysToString_TestData()
    {
        yield return new object[] { ModifierKeys.None, "" };
        yield return new object[] { ModifierKeys.Alt, "Alt" };
        yield return new object[] { ModifierKeys.Control, "Ctrl" };
        yield return new object[] { ModifierKeys.Shift, "Shift" };
        yield return new object[] { ModifierKeys.Windows, "Windows" };
        
        yield return new object[] { ModifierKeys.Control | ModifierKeys.Alt, "Ctrl+Alt" };
        yield return new object[] { ModifierKeys.Control | ModifierKeys.Windows, "Ctrl+Windows" };
        yield return new object[] { ModifierKeys.Control | ModifierKeys.Shift, "Ctrl+Shift" };
        yield return new object[] { ModifierKeys.Control | ModifierKeys.Alt | ModifierKeys.Windows, "Ctrl+Alt+Windows" };
        yield return new object[] { ModifierKeys.Control | ModifierKeys.Alt | ModifierKeys.Windows | ModifierKeys.Shift, "Ctrl+Alt+Windows+Shift" };
        yield return new object[] { ModifierKeys.Control | ModifierKeys.Alt | ModifierKeys.Shift, "Ctrl+Alt+Shift" };
        yield return new object[] { ModifierKeys.Control | ModifierKeys.Windows | ModifierKeys.Shift, "Ctrl+Windows+Shift" };
        yield return new object[] { ModifierKeys.Alt | ModifierKeys.Windows, "Alt+Windows" };
        yield return new object[] { ModifierKeys.Alt | ModifierKeys.Windows | ModifierKeys.Shift, "Alt+Windows+Shift" };
        yield return new object[] { ModifierKeys.Alt | ModifierKeys.Shift, "Alt+Shift" };
        yield return new object[] { ModifierKeys.Windows | ModifierKeys.Shift, "Windows+Shift" };
    }

    [Theory]
    [MemberData(nameof(ConvertTo_ModifierKeysToString_TestData))]
    public void ConvertTo_InvokeModifierKeysToString_ReturnsExpected(ModifierKeys value, string expected)
    {
        var converter = new ModifierKeysConverter();
        Assert.Equal(expected, converter.ConvertTo(value, typeof(string)));
        Assert.Equal(expected, converter.ConvertTo(new CustomTypeDescriptorContext(), null, value, typeof(string)));
        Assert.Equal(expected, converter.ConvertTo(new CustomTypeDescriptorContext(), CultureInfo.InvariantCulture, value, typeof(string)));
    }

    public static IEnumerable<object[]> ConvertTo_InvalidModifierKeys_TestData()
    {
        yield return new object[] { (ModifierKeys)int.MinValue };
        yield return new object[] { (ModifierKeys)(-1) };
        yield return new object[] { (ModifierKeys)32 };
        yield return new object[] { (ModifierKeys)int.MaxValue };
    }

    [Theory]
    [MemberData(nameof(ConvertTo_InvalidModifierKeys_TestData))]
    public void ConvertTo_InvalidModifierKeys_ThrowsInvalidEnumArgumentException(ModifierKeys value)
    {
        var converter = new ModifierKeysConverter();
        Assert.Throws<InvalidEnumArgumentException>("value", () => converter.ConvertTo(value, typeof(string)));
        Assert.Throws<InvalidEnumArgumentException>("value", () => converter.ConvertTo(new CustomTypeDescriptorContext(), null, value, typeof(string)));
        Assert.Throws<InvalidEnumArgumentException>("value", () => converter.ConvertTo(new CustomTypeDescriptorContext(), CultureInfo.InvariantCulture, value, typeof(string)));
    }

    [Theory]
    [InlineData(null)]
    // TODO: this should not throw InvalidCastException.
    //[InlineData("", "")]
    //[InlineData("value", "value")]
    public void ConvertTo_InvokeNotModifierKeysToStringNull_ThrowsNotSupportedException(object? value)
    {
        var converter = new ModifierKeysConverter();
        // TODO: should not throw NullReferenceException
        //Assert.Throws<NotSupportedException>(() => converter.ConvertTo(value, typeof(string)));
        //Assert.Throws<NotSupportedException>(() => converter.ConvertTo(new CustomTypeDescriptorContext(), null, value, typeof(string)));
        //Assert.Throws<NotSupportedException>(() => converter.ConvertTo(new CustomTypeDescriptorContext(), CultureInfo.InvariantCulture, value, typeof(string)));
        Assert.Throws<NullReferenceException>(() => converter.ConvertTo(value, typeof(string)));
        Assert.Throws<NullReferenceException>(() => converter.ConvertTo(new CustomTypeDescriptorContext(), null, value, typeof(string)));
        Assert.Throws<NullReferenceException>(() => converter.ConvertTo(new CustomTypeDescriptorContext(), CultureInfo.InvariantCulture, value, typeof(string)));
    }

    [Theory]
    [InlineData("")]
    [InlineData("value")]
    public void ConvertTo_InvokeNotModifierKeysToStringNotNull_ThrowsInvalidCastException(object value)
    {
        // TODO: this should not throw InvalidCastException.
        var converter = new ModifierKeysConverter();
        Assert.Throws<InvalidCastException>(() => converter.ConvertTo(value, typeof(string)));
        Assert.Throws<InvalidCastException>(() => converter.ConvertTo(new CustomTypeDescriptorContext(), null, value, typeof(string)));
        Assert.Throws<InvalidCastException>(() => converter.ConvertTo(new CustomTypeDescriptorContext(), CultureInfo.InvariantCulture, value, typeof(string)));
    }

    public static IEnumerable<object?[]> ConvertTo_CantConvert_TestData()
    {
        yield return new object?[] { null, typeof(object) };
        yield return new object?[] { string.Empty, typeof(object) };
        yield return new object?[] { "value", typeof(object) };
        yield return new object?[] { new object(), typeof(object) };
        yield return new object?[] { Key.None, typeof(object) };
        
        yield return new object?[] { null, typeof(Key) };
        yield return new object?[] { string.Empty, typeof(Key) };
        yield return new object?[] { "value", typeof(Key) };
        yield return new object?[] { new object(), typeof(Key) };
        yield return new object?[] { Key.None, typeof(Key) };
    }

    [Theory]
    [MemberData(nameof(ConvertTo_CantConvert_TestData))]
    public void ConvertTo_CantConvert_ThrowsNotSupportedException(object value, Type destinationType)
    {
        var converter = new ModifierKeysConverter();
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
        yield return new object?[] { ModifierKeys.None };
    }

    [Theory]
    [MemberData(nameof(ConvertTo_NullDestinationType_TestData))]
    public void ConvertTo_NullDestinationType_ThrowsArgumentNullException(object value)
    {
        var converter = new ModifierKeysConverter();
        Assert.Throws<ArgumentNullException>("destinationType", () => converter.ConvertTo(value, null!));
        Assert.Throws<ArgumentNullException>("destinationType", () => converter.ConvertTo(null, null, Key.None, null!));
        Assert.Throws<ArgumentNullException>("destinationType", () => converter.ConvertTo(new CustomTypeDescriptorContext(), CultureInfo.InvariantCulture, Key.None, null!));
    }

    [Theory]
    [InlineData(null, false)]
    [InlineData(typeof(object), false)]
    [InlineData(typeof(string), true)]
    [InlineData(typeof(InstanceDescriptor), false)]
    [InlineData(typeof(Key), false)]
    public void CanConvertFrom_Invoke_ReturnsExpected(Type? sourceType, bool expected)
    {
        var converter = new ModifierKeysConverter();
        Assert.Equal(expected, converter.CanConvertFrom(sourceType!));
        Assert.Equal(expected, converter.CanConvertFrom(null, sourceType));
        Assert.Equal(expected, converter.CanConvertFrom(new CustomTypeDescriptorContext(), sourceType));
    }

    public static IEnumerable<object[]> ConvertFrom_TestData()
    {
        yield return new object[] { "", ModifierKeys.None };
        yield return new object[] { "  ", ModifierKeys.None };
        yield return new object[] { "+", ModifierKeys.None };
        yield return new object[] { " + ", ModifierKeys.None };
        yield return new object[] { "Alt", ModifierKeys.Alt };
        yield return new object[] { "ALT", ModifierKeys.Alt };
        yield return new object[] { " Alt ", ModifierKeys.Alt };
        yield return new object[] { "Ctrl", ModifierKeys.Control };
        yield return new object[] { "Control", ModifierKeys.Control };
        yield return new object[] { "Shift", ModifierKeys.Shift };
        yield return new object[] { "Windows", ModifierKeys.Windows };
        yield return new object[] { "Win", ModifierKeys.Windows };
        
        yield return new object[] { "Ctrl+Alt", ModifierKeys.Control | ModifierKeys.Alt };
        yield return new object[] { "  Ctrl  +  Alt  ", ModifierKeys.Control | ModifierKeys.Alt };
        yield return new object[] { "Ctrl+Windows", ModifierKeys.Control | ModifierKeys.Windows };
        yield return new object[] { "Control+Win", ModifierKeys.Control | ModifierKeys.Windows };
        yield return new object[] { "Ctrl+Shift", ModifierKeys.Control | ModifierKeys.Shift };
        yield return new object[] { "Ctrl+Alt+Windows", ModifierKeys.Control | ModifierKeys.Alt | ModifierKeys.Windows };
        yield return new object[] { "Ctrl+Alt+Windows+Shift", ModifierKeys.Control | ModifierKeys.Alt | ModifierKeys.Windows | ModifierKeys.Shift };
        yield return new object[] { "Ctrl+Alt+Shift", ModifierKeys.Control | ModifierKeys.Alt | ModifierKeys.Shift };
        yield return new object[] { "Ctrl+Windows+Shift", ModifierKeys.Control | ModifierKeys.Windows | ModifierKeys.Shift };
        yield return new object[] { "Alt+Windows", ModifierKeys.Alt | ModifierKeys.Windows };
        yield return new object[] { "Alt+Windows+Shift", ModifierKeys.Alt | ModifierKeys.Windows | ModifierKeys.Shift };
        yield return new object[] { "Alt+Shift", ModifierKeys.Alt | ModifierKeys.Shift };
        yield return new object[] { "Windows+Shift", ModifierKeys.Windows | ModifierKeys.Shift };

        yield return new object[] { "Ctrl+", ModifierKeys.Control };
        yield return new object[] { "Ctrl+Windows+", ModifierKeys.Control | ModifierKeys.Windows };
        yield return new object[] { "Ctrl+Ctrl+Ctrl", ModifierKeys.Control };
    }

    [Theory]
    [MemberData(nameof(ConvertFrom_TestData))]
    public void ConvertFrom_InvokeStringValue_ReturnsExpected(string value, ModifierKeys expected)
    {
        var converter = new ModifierKeysConverter();
        Assert.Equal(expected, converter.ConvertFrom(value));
        Assert.Equal(expected, converter.ConvertFrom(null, null, value));
        Assert.Equal(expected, converter.ConvertFrom(new CustomTypeDescriptorContext(), CultureInfo.InvariantCulture, value));
    }

    [Fact]
    public void ConvertFrom_NullValue_ThrowsNotSupportedException()
    {
        var converter = new ModifierKeysConverter();
        Assert.Throws<NotSupportedException>(() => converter.ConvertFrom(null!));
        Assert.Throws<NotSupportedException>(() => converter.ConvertFrom(null, null, null));
        Assert.Throws<NotSupportedException>(() => converter.ConvertFrom(new CustomTypeDescriptorContext(), CultureInfo.InvariantCulture, null));
    }

    public static IEnumerable<object[]> ConvertFrom_InvalidValue_TestData()
    {
        yield return new object[] { "_" };
        yield return new object[] { " _ " };
        yield return new object[] { "NOSUCHKEY" };
        yield return new object[] { " NOSUCHKEY " };
        yield return new object[] { "Control+NOSUCHKEY" };
    }

    [Theory]
    [MemberData(nameof(ConvertFrom_InvalidValue_TestData))]
    public void ConvertFrom_InvokeInvalidValue_ThrowsNotSupportedException(string value)
    {
        var converter = new ModifierKeysConverter();
        Assert.Throws<NotSupportedException>(() => converter.ConvertFrom(value));
        Assert.Throws<NotSupportedException>(() => converter.ConvertFrom(null, null, value));
        Assert.Throws<NotSupportedException>(() => converter.ConvertFrom(new CustomTypeDescriptorContext(), CultureInfo.InvariantCulture, value));
    }

    public static IEnumerable<object[]> ConvertFrom_CantConvert_TestData()
    {
        yield return new object[] { new object() };
        yield return new object[] { Key.A };
        yield return new object[] { ModifierKeys.None };
    }
    
    [Theory]
    [MemberData(nameof(ConvertFrom_CantConvert_TestData))]
    public void ConvertFrom_CantConvert_ThrowsNotSupportedException(object value)
    {
        var converter = new ModifierKeysConverter();
        Assert.Throws<NotSupportedException>(() => converter.ConvertFrom(value));
        Assert.Throws<NotSupportedException>(() => converter.ConvertFrom(null, null, value));
        Assert.Throws<NotSupportedException>(() => converter.ConvertFrom(new CustomTypeDescriptorContext(), CultureInfo.InvariantCulture, value));
    }

    private class CustomTypeDescriptorContext : ITypeDescriptorContext
    {
        public IContainer Container => throw new NotImplementedException();

        private object? _instance;

        public object Instance
        {
            get => _instance!;
            set => _instance = value;
        }

        public PropertyDescriptor PropertyDescriptor => throw new NotImplementedException();

        public object? GetService(Type serviceType) => throw new NotImplementedException();

        public void OnComponentChanged() => throw new NotImplementedException();

        public bool OnComponentChanging() => throw new NotImplementedException();
    }
}
