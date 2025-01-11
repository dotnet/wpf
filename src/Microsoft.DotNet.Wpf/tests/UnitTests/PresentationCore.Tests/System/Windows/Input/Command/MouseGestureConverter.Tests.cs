// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel.Design.Serialization;
using System.ComponentModel;
using System.Globalization;

namespace System.Windows.Input.Command;

public sealed class MouseGestureConverterTests
{
    [Theory]
    // Valid type
    [InlineData(true, typeof(string))]
    // Invalid types
    [InlineData(false, typeof(MouseAction))]
    [InlineData(false, typeof(ModifierKeys))]
    [InlineData(false, typeof(KeyGesture))]
    [InlineData(false, typeof(MouseGesture))]
    [InlineData(false, typeof(InstanceDescriptor))]
    public void CanConvertFrom_ReturnsExpected(bool expected, Type sourceType)
    {
        MouseGestureConverter converter = new();

        Assert.Equal(expected, converter.CanConvertFrom(sourceType));
    }

    [Theory]
    [MemberData(nameof(CanConvertTo_Data))]
    public void CanConvertTo_ReturnsExpected(bool expected, bool passContext, object? value, Type? destinationType)
    {
        MouseGestureConverter converter = new();
        StandardContextImpl context = new() { Instance = value };

        Assert.Equal(expected, converter.CanConvertTo(passContext ? context : null, destinationType));
    }

    public static IEnumerable<object?[]> CanConvertTo_Data
    {
        get
        {
            // Supported cases
            yield return new object[] { true, true, new MouseGesture(MouseAction.None, ModifierKeys.Control), typeof(string) };
            yield return new object[] { true, true, new MouseGesture(MouseAction.None, ModifierKeys.Alt), typeof(string) };
            yield return new object[] { true, true, new MouseGesture(MouseAction.MiddleDoubleClick, ModifierKeys.Shift), typeof(string) };
            yield return new object[] { true, true, new MouseGesture(MouseAction.LeftDoubleClick, ModifierKeys.Control | ModifierKeys.Windows | ModifierKeys.Alt), typeof(string) };
            yield return new object[] { true, true, new MouseGesture(MouseAction.WheelClick, ModifierKeys.Control | ModifierKeys.Windows), typeof(string) };
            yield return new object[] { true, true, new MouseGesture(MouseAction.LeftClick, ModifierKeys.Alt | ModifierKeys.Windows), typeof(string) };
            yield return new object[] { true, true, new MouseGesture(MouseAction.RightClick, ModifierKeys.Alt | ModifierKeys.Control), typeof(string) };
            yield return new object[] { true, true, new MouseGesture(MouseAction.RightDoubleClick, ModifierKeys.Alt | ModifierKeys.Windows | ModifierKeys.Control), typeof(string) };
            yield return new object[] { true, true, new MouseGesture(MouseAction.RightDoubleClick, ModifierKeys.None), typeof(string) };

            // Unsupported cases (Null Context)
            yield return new object?[] { false, false, null, typeof(string) };
            // Unsupported cases (Null Context/Destination Type)
            yield return new object?[] { false, false, null, null };
            // Unsupported cases (Null Instance)
            yield return new object?[] { false, true, null, typeof(string) };
            // Unsupported cases (Null Instance/Destination Type)
            yield return new object?[] { false, true, null, null };
            // Unsupported cases (Wrong destination type)
            yield return new object?[] { false, true, new MouseGesture(MouseAction.None, ModifierKeys.Control), null };
            yield return new object?[] { false, true, new MouseGesture(MouseAction.WheelClick, ModifierKeys.Control | ModifierKeys.Windows), typeof(KeyGesture) };
            yield return new object?[] { false, true, new MouseGesture(MouseAction.LeftClick, ModifierKeys.Alt | ModifierKeys.Windows), typeof(MouseGesture) };
            yield return new object?[] { false, true, new MouseGesture(MouseAction.None, ModifierKeys.Control), typeof(MouseAction) };
            yield return new object?[] { false, true, new MouseGesture(MouseAction.RightDoubleClick, ModifierKeys.Alt | ModifierKeys.Windows | ModifierKeys.Control), typeof(Key) };
            yield return new object?[] { false, true, new MouseGesture(MouseAction.RightDoubleClick, ModifierKeys.None), typeof(ModifierKeys) };
            // Unsupported cases (Wrong Context Instance)
            yield return new object?[] { false, true, new KeyGesture(Key.None, ModifierKeys.Alt), typeof(string) };
            yield return new object?[] { false, true, MouseAction.WheelClick, typeof(string) };
            yield return new object?[] { false, true, Key.F1, typeof(string) };

            // We do not test for malformed MouseGesture as MouseGesture has to perform its own validation and shall be enforced via its own unit tests
        }
    }

    [Theory]
    [MemberData(nameof(ConvertFrom_ReturnsExpected_Data))]
    public void ConvertFrom_ReturnsExpected(MouseGesture expected, ITypeDescriptorContext context, CultureInfo? cultureInfo, string value)
    {
        MouseGestureConverter converter = new();

        MouseGesture converted = (MouseGesture)converter.ConvertFrom(context, cultureInfo, value);
        Assert.Equal(expected.Modifiers, converted.Modifiers);
        Assert.Equal(expected.MouseAction, converted.MouseAction);
    }

    public static IEnumerable<object?[]> ConvertFrom_ReturnsExpected_Data
    {
        get
        {
            // Supported cases (Culture must stay irrelevant, MouseAction/ModifierKeys also do not care)
            yield return new object?[] { new MouseGesture(MouseAction.None, ModifierKeys.None), null, CultureInfo.InvariantCulture, string.Empty };
            yield return new object?[] { new MouseGesture(MouseAction.LeftClick, ModifierKeys.None), null, new CultureInfo("ru-RU"), "LeftClick" };
            yield return new object?[] { new MouseGesture(MouseAction.None, ModifierKeys.Control), null, CultureInfo.InvariantCulture, "Ctrl+" };
            yield return new object?[] { new MouseGesture(MouseAction.LeftClick, ModifierKeys.Control), null, CultureInfo.InvariantCulture, "Ctrl+LeftClick" };
            yield return new object?[] { new MouseGesture(MouseAction.MiddleDoubleClick, ModifierKeys.Alt), null, new CultureInfo("no-NO"), "Alt+MiddleDoubleClick" };
            yield return new object?[] { new MouseGesture(MouseAction.WheelClick, ModifierKeys.Shift), null, CultureInfo.InvariantCulture, "Shift+WheelClick" };
            yield return new object?[] { new MouseGesture(MouseAction.LeftDoubleClick, ModifierKeys.Windows), null, CultureInfo.InvariantCulture, "Windows+LeftDoubleClick" };
            yield return new object?[] { new MouseGesture(MouseAction.RightClick, ModifierKeys.Control | ModifierKeys.Alt), null, CultureInfo.InvariantCulture, "Ctrl+Alt+RightClick" };
            yield return new object?[] { new MouseGesture(MouseAction.RightDoubleClick, ModifierKeys.Control | ModifierKeys.Windows | ModifierKeys.Alt), null, CultureInfo.InvariantCulture, "Ctrl+Alt+Windows+RightDoubleClick" };

            // Supported cases (fuzzed)
            yield return new object?[] { new MouseGesture(MouseAction.None, ModifierKeys.Alt), null, CultureInfo.InvariantCulture, "Alt+                " };
            yield return new object?[] { new MouseGesture(MouseAction.LeftClick, ModifierKeys.None), null, CultureInfo.InvariantCulture, "   LeftClick  " };
            yield return new object?[] { new MouseGesture(MouseAction.None, ModifierKeys.None), null, CultureInfo.InvariantCulture, "                   " };
            yield return new object?[] { new MouseGesture(MouseAction.WheelClick, ModifierKeys.Shift), null, CultureInfo.InvariantCulture, "Shift      +WheelClick" };
            yield return new object?[] { new MouseGesture(MouseAction.MiddleClick, ModifierKeys.Windows | ModifierKeys.Shift), null, new CultureInfo("no-NO"), " Shift +   Windows  +    MiddleClick   " };
            yield return new object?[] { new MouseGesture(MouseAction.LeftDoubleClick, ModifierKeys.Windows), null, CultureInfo.InvariantCulture, "Windows+       LeftDoubleClick   " };
            yield return new object?[] { new MouseGesture(MouseAction.RightClick, ModifierKeys.Control | ModifierKeys.Alt), null, CultureInfo.InvariantCulture, "Ctrl+Alt+ RightClick" };
        }
    }

    [Theory]
    [MemberData(nameof(ConvertFrom_ThrowsNotSupportedException_Data))]
    public void ConvertFrom_ThrowsNotSupportedException(CultureInfo? cultureInfo, object value)
    {
        MouseGestureConverter converter = new();

        Assert.Throws<NotSupportedException>(() => converter.ConvertFrom(null, cultureInfo, value));
    }

    public static IEnumerable<object?[]> ConvertFrom_ThrowsNotSupportedException_Data
    {
        get
        {
            // Nulls are not supported
            yield return new object?[] { CultureInfo.InvariantCulture, null };
            // Anything that isn't a string ain't supported
            yield return new object?[] { CultureInfo.InvariantCulture, new MouseGesture(MouseAction.LeftClick, ModifierKeys.Control) };
            yield return new object?[] { CultureInfo.InvariantCulture, new KeyGesture(Key.V, ModifierKeys.Control) };
            yield return new object?[] { CultureInfo.InvariantCulture, ModifierKeys.Control };
            yield return new object?[] { CultureInfo.InvariantCulture, MouseAction.LeftDoubleClick };
            yield return new object?[] { CultureInfo.InvariantCulture, Key.V };
        }
    }

    [Theory]
    [MemberData(nameof(ConvertTo_ReturnsExpected_Data))]
    public void ConvertTo_ReturnsExpected(string expected, ITypeDescriptorContext context, CultureInfo? cultureInfo, object? value)
    {
        MouseGestureConverter converter = new();

        // Culture and context must not have any meaning
        Assert.Equal(expected, converter.ConvertTo(context, cultureInfo, value, typeof(string)));
    }

    public static IEnumerable<object?[]> ConvertTo_ReturnsExpected_Data
    {
        get
        {
            // Supported null value case that returns string.Empty
            yield return new object?[] { string.Empty, null, CultureInfo.InvariantCulture, null };

            // Supported cases (Culture must stay irrelevant, MouseAction/ModifierKeys also do not care)
            yield return new object?[] { string.Empty, null, CultureInfo.InvariantCulture, new MouseGesture(MouseAction.None, ModifierKeys.None) };
            yield return new object?[] { "Alt+", null, CultureInfo.InvariantCulture, new MouseGesture(MouseAction.None, ModifierKeys.Alt) };
            yield return new object?[] { "Windows+", null, new CultureInfo("de-DE"), new MouseGesture(MouseAction.None, ModifierKeys.Windows) };
            yield return new object?[] { "Shift+", null, new CultureInfo("ru-RU"), new MouseGesture(MouseAction.None, ModifierKeys.Shift) };
            yield return new object?[] { "LeftClick", null, CultureInfo.InvariantCulture, new MouseGesture(MouseAction.LeftClick) };
            yield return new object?[] { "Ctrl+LeftClick", null, CultureInfo.InvariantCulture, new MouseGesture(MouseAction.LeftClick, ModifierKeys.Control) };
            yield return new object?[] { "Alt+RightClick", null, CultureInfo.InvariantCulture, new MouseGesture(MouseAction.RightClick, ModifierKeys.Alt) };
            yield return new object?[] { "Windows+WheelClick", null, CultureInfo.InvariantCulture, new MouseGesture(MouseAction.WheelClick, ModifierKeys.Windows) };
            yield return new object?[] { "Alt+RightDoubleClick", null, CultureInfo.InvariantCulture, new MouseGesture(MouseAction.RightDoubleClick, ModifierKeys.Alt) };
            yield return new object?[] { "Ctrl+Alt+Windows+WheelClick", null, new CultureInfo("de-DE"), new MouseGesture(MouseAction.WheelClick, ModifierKeys.Control | ModifierKeys.Windows | ModifierKeys.Alt) };
            yield return new object?[] { "Alt+Windows+MiddleDoubleClick", null, new CultureInfo("ru-RU"), new MouseGesture(MouseAction.MiddleDoubleClick, ModifierKeys.Alt | ModifierKeys.Windows) };
            yield return new object?[] { "Ctrl+Alt+Windows+MiddleClick", null, CultureInfo.InvariantCulture, new MouseGesture(MouseAction.MiddleClick, ModifierKeys.Alt | ModifierKeys.Windows | ModifierKeys.Control) };
        }
    }

    [Fact]
    public void ConvertTo_ThrowsArgumentNullException()
    {
        MouseGestureConverter converter = new();

        Assert.Throws<ArgumentNullException>(() => converter.ConvertTo(null, CultureInfo.InvariantCulture, new MouseGesture(MouseAction.LeftClick, ModifierKeys.Control), null));
    }

    [Theory]
    [MemberData(nameof(ConvertTo_ThrowsNotSupportedException_Data))]
    public void ConvertTo_ThrowsNotSupportedException(object? value, Type? destinationType)
    {
        MouseGestureConverter converter = new();

        Assert.Throws<NotSupportedException>(() => converter.ConvertTo(null, CultureInfo.InvariantCulture, value, destinationType));
    }

    public static IEnumerable<object?[]> ConvertTo_ThrowsNotSupportedException_Data
    {
        get
        {
            // Wrong destination types
            yield return new object?[] { new MouseGesture(MouseAction.LeftClick, ModifierKeys.Control), typeof(MouseGesture) };
            yield return new object?[] { new MouseGesture(MouseAction.LeftClick, ModifierKeys.Control), typeof(KeyGesture) };
            yield return new object?[] { new MouseGesture(MouseAction.LeftClick, ModifierKeys.Control), typeof(MouseAction) };
            yield return new object?[] { new MouseGesture(MouseAction.LeftClick, ModifierKeys.Control), typeof(Key) };
            yield return new object?[] { new MouseGesture(MouseAction.WheelClick, ModifierKeys.Control), typeof(ModifierKeys) };
            // Wrong value types
            yield return new object?[] { new KeyGesture(Key.V, ModifierKeys.Control), typeof(string) };
            yield return new object?[] { MouseAction.MiddleDoubleClick, typeof(string) };
            yield return new object?[] { ModifierKeys.Control, typeof(string) };
            yield return new object?[] { Key.V, typeof(string) };
        }
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
