// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel.Design.Serialization;
using System.ComponentModel;
using System.Globalization;

namespace System.Windows.Input;

public sealed class MouseActionConverterTests
{
    [Theory]
    // Supported type
    [InlineData(true, typeof(string))]
    // Unsupported types
    [InlineData(false, typeof(InstanceDescriptor))]
    [InlineData(false, typeof(MouseAction))]
    public void CanConvertFrom_ReturnsExpected(bool expected, Type sourceType)
    {
        MouseActionConverter converter = new();

        Assert.Equal(expected, converter.CanConvertFrom(null, sourceType));
    }

    [Theory]
    [MemberData(nameof(CanConvertTo_Data))]
    public void CanConvertTo_ReturnsExpected(bool expected, bool passContext, object? value, Type? destinationType)
    {
        MouseActionConverter converter = new();
        StandardContextImpl context = new() { Instance = value };

        Assert.Equal(expected, converter.CanConvertTo(passContext ? context : null, destinationType));
    }

    public static IEnumerable<object?[]> CanConvertTo_Data
    {
        get
        {
            // Supported cases
            yield return new object[] { true, true, MouseAction.None, typeof(string) };
            yield return new object[] { true, true, MouseAction.MiddleDoubleClick, typeof(string) };

            // Unsupported case (Value is above MouseAction range)
            yield return new object[] { false, true, MouseAction.MiddleDoubleClick + 1, typeof(string) };

            // Unsupported cases
            yield return new object[] { false, false, MouseAction.None, typeof(string) };
            yield return new object[] { false, false, MouseAction.MiddleDoubleClick, typeof(string) };
            yield return new object?[] { false, true, null, typeof(MouseAction) };
            yield return new object?[] { false, true, null, typeof(string) };
            yield return new object?[] { false, false, MouseAction.MiddleDoubleClick, typeof(string) };
            yield return new object?[] { false, false, null, typeof(string) };
            yield return new object?[] { false, false, null, null };

            yield return new object[] { false, true, MouseAction.MiddleDoubleClick + 1, typeof(string) };
        }
    }

    [Fact]
    public void CanConvertTo_ThrowsInvalidCastException()
    {
        MouseActionConverter converter = new();
        StandardContextImpl context = new() { Instance = 10 };

        // TODO: CanConvert* methods should not throw but the implementation is faulty
        Assert.Throws<InvalidCastException>(() => converter.CanConvertTo(context, typeof(string)));
    }

    [Theory]
    [MemberData(nameof(ConvertFrom_ReturnsExpected_Data))]
    public void ConvertFrom_ReturnsExpected(MouseAction expected, ITypeDescriptorContext context, CultureInfo? cultureInfo, string value)
    {
        MouseActionConverter converter = new();

        Assert.Equal(expected, (MouseAction)converter.ConvertFrom(context, cultureInfo, value));
    }

    public static IEnumerable<object?[]> ConvertFrom_ReturnsExpected_Data
    {
        get
        {
            // ConvertTo handles two different inputs as MouseAction.None
            yield return new object?[] { MouseAction.None, null, CultureInfo.InvariantCulture, string.Empty };
            yield return new object?[] { MouseAction.None, null, CultureInfo.InvariantCulture, "None" };

            // Supported cases (Culture must stay irrelevant)
            yield return new object?[] { MouseAction.None, null, CultureInfo.InvariantCulture, string.Empty };
            yield return new object?[] { MouseAction.LeftClick, null, new CultureInfo("ru-RU"), "LeftClick" };
            yield return new object?[] { MouseAction.RightClick, null, CultureInfo.InvariantCulture, "RightClick" };
            yield return new object?[] { MouseAction.MiddleClick, null, CultureInfo.InvariantCulture, "MiddleClick" };
            yield return new object?[] { MouseAction.WheelClick, null, new CultureInfo("no-NO"), "WheelClick" };
            yield return new object?[] { MouseAction.LeftDoubleClick, null, CultureInfo.InvariantCulture, "LeftDoubleClick" };
            yield return new object?[] { MouseAction.RightDoubleClick, null, CultureInfo.InvariantCulture, "RightDoubleClick" };
            yield return new object?[] { MouseAction.MiddleDoubleClick, null, CultureInfo.InvariantCulture, "MiddleDoubleClick" };

            // Supported cases (fuzzed via whitespace and casing)  
            yield return new object?[] { MouseAction.None, null, CultureInfo.InvariantCulture, "                " };
            yield return new object?[] { MouseAction.None, null, new CultureInfo("ru-RU"), "         NoNE          " };
            yield return new object?[] { MouseAction.LeftClick, null, CultureInfo.InvariantCulture, "   LeFTCliCK  " };
            yield return new object?[] { MouseAction.WheelClick, null, CultureInfo.InvariantCulture, "         WHEELCLICK" };
            yield return new object?[] { MouseAction.MiddleClick, null, new CultureInfo("no-NO"), "   MiDDLeCliCK   " };
            yield return new object?[] { MouseAction.LeftDoubleClick, null, CultureInfo.InvariantCulture, "    leftdoubleclick   " };
            yield return new object?[] { MouseAction.RightClick, null, CultureInfo.InvariantCulture, " rightclick" };
        }
    }

    [Theory]
    // Unsupported values (data type)
    [InlineData(null)]
    [InlineData(Key.VolumeDown)]
    [InlineData(MouseAction.None)]
    // Unsupported value (bad string)
    [InlineData("BadString")]
    [InlineData("MouseZClick")]
    public void ConvertFrom_ThrowsNotSupportedException(object? value)
    {
        MouseActionConverter converter = new();

        Assert.Throws<NotSupportedException>(() => converter.ConvertFrom(null, null, value));
    }

    [Theory]
    [MemberData(nameof(ConvertTo_ReturnsExpected_Data))]
    public void ConvertTo_ReturnsExpected(string expected, ITypeDescriptorContext context, CultureInfo? cultureInfo, object? value)
    {
        MouseActionConverter converter = new();

        // Culture and context must not have any meaning
        Assert.Equal(expected, (string)converter.ConvertTo(context, cultureInfo, value, typeof(string)));
    }

    public static IEnumerable<object?[]> ConvertTo_ReturnsExpected_Data
    {
        get
        {
            // Supported cases (Culture must stay irrelevant)
            yield return new object?[] { string.Empty, null, CultureInfo.InvariantCulture, MouseAction.None };
            yield return new object?[] { "LeftClick", null, CultureInfo.InvariantCulture, MouseAction.LeftClick };
            yield return new object?[] { "RightClick", null, new CultureInfo("ru-RU"), MouseAction.RightClick };
            yield return new object?[] { "MiddleClick", null, CultureInfo.InvariantCulture, MouseAction.MiddleClick };
            yield return new object?[] { "WheelClick", null, new CultureInfo("no-NO"), MouseAction.WheelClick };
            yield return new object?[] { "LeftDoubleClick", null, CultureInfo.InvariantCulture, MouseAction.LeftDoubleClick };
            yield return new object?[] { "RightDoubleClick", null, null, MouseAction.RightDoubleClick };
            yield return new object?[] { "MiddleDoubleClick", null, null, MouseAction.MiddleDoubleClick };
        }
    }

    [Fact]
    public void ConvertTo_ThrowsArgumentNullException()
    {
        MouseActionConverter converter = new();

        Assert.Throws<ArgumentNullException>(() => converter.ConvertTo(MouseAction.None, destinationType: null!));
    }

    [Theory]
    // Unsupported value
    [InlineData(null, typeof(string))]
    // Unsupported destinationType
    [InlineData(MouseAction.None, typeof(int))]
    [InlineData(MouseAction.LeftClick, typeof(byte))]
    public void ConvertTo_ThrowsNotSupportedException(object? value, Type destinationType)
    {
        MouseActionConverter converter = new();

        Assert.Throws<NotSupportedException>(() => converter.ConvertTo(value, destinationType));
    }

    [Fact]
    public void ConvertTo_ThrowsInvalidCastException()
    {
        MouseActionConverter converter = new();

        // TODO: This should not throw InvalidCastException but NotSupportedException
        Assert.Throws<InvalidCastException>(() => converter.ConvertTo(null, null, (int)(MouseAction.MiddleDoubleClick), typeof(string)));
    }

    [Fact]
    public void ConvertTo_ThrowsInvalidEnumArgumentException()
    {
        MouseActionConverter converter = new();

        Assert.Throws<InvalidEnumArgumentException>(() => converter.ConvertTo(null, null, (MouseAction)(MouseAction.MiddleDoubleClick + 1), typeof(string)));
    }

    public sealed class StandardContextImpl : ITypeDescriptorContext
    {
        public IContainer? Container => throw new NotImplementedException();

        public object? Instance { get; set; }

        public PropertyDescriptor? PropertyDescriptor => throw new NotImplementedException();
        public object? GetService(Type serviceType) => throw new NotImplementedException();
        public void OnComponentChanged() => throw new NotImplementedException();
        public bool OnComponentChanging() => throw new NotImplementedException();
    }
}
