// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel.Design.Serialization;
using System.ComponentModel;
using System.Globalization;

namespace System.Windows.Input.Command;

public sealed class KeyGestureConverterTests
{
    [Theory]
    // Valid type
    [InlineData(true, typeof(string))]
    // Invalid types
    [InlineData(false, typeof(Key))]
    [InlineData(false, typeof(ModifierKeys))]
    [InlineData(false, typeof(KeyGesture))]
    [InlineData(false, typeof(MouseGesture))]
    [InlineData(false, typeof(InstanceDescriptor))]
    public void CanConvertFrom_ReturnsExpected(bool expected, Type sourceType)
    {
        KeyGestureConverter converter = new();

        Assert.Equal(expected, converter.CanConvertFrom(sourceType));
    }

    [Theory]
    [MemberData(nameof(CanConvertTo_Data))]
    public void CanConvertTo_ReturnsExpected(bool expected, bool passContext, object? value, Type? destinationType)
    {
        KeyGestureConverter converter = new();
        StandardContextImpl context = new() { Instance = value };

        Assert.Equal(expected, converter.CanConvertTo(passContext ? context : null, destinationType));
    }

    public static IEnumerable<object?[]> CanConvertTo_Data
    {
        get
        {
            // Supported cases
            yield return new object[] { true, true, new KeyGesture(Key.NumLock, ModifierKeys.Control), typeof(string) };
            yield return new object[] { true, true, new KeyGesture(Key.F1, ModifierKeys.Alt, "displayString"), typeof(string) };
            yield return new object[] { true, true, new KeyGesture(Key.G, ModifierKeys.None, validateGesture: false), typeof(string) };
            yield return new object[] { true, true, new KeyGesture(Key.Insert, ModifierKeys.Control | ModifierKeys.Windows | ModifierKeys.Alt), typeof(string) };
            yield return new object[] { true, true, new KeyGesture(Key.NumLock, ModifierKeys.Control | ModifierKeys.Windows), typeof(string) };
            yield return new object[] { true, true, new KeyGesture(Key.F21, ModifierKeys.Alt | ModifierKeys.Windows, "displayString"), typeof(string) };
            yield return new object[] { true, true, new KeyGesture(Key.F8, ModifierKeys.Alt | ModifierKeys.Control, "Two Modifiers"), typeof(string) };
            yield return new object[] { true, true, new KeyGesture(Key.A, ModifierKeys.Alt | ModifierKeys.Windows | ModifierKeys.Control, "Test String"), typeof(string) };
            yield return new object[] { true, true, new KeyGesture(Key.Z, ModifierKeys.None, validateGesture: false), typeof(string) };

            // Unsupported cases (Null Context)
            yield return new object?[] { false, false, null, typeof(string) };
            // Unsupported cases (Null Context/Destination Type)
            yield return new object?[] { false, false, null, null };
            // Unsupported cases (Null Instance)
            yield return new object?[] { false, true, null, typeof(string) };
            // Unsupported cases (Null Instance/Destination Type)
            yield return new object?[] { false, true, null, null };
            // Unsupported cases (Wrong destination type)
            yield return new object?[] { false, true, new KeyGesture(Key.D1, ModifierKeys.Control), null };
            yield return new object?[] { false, true, new KeyGesture(Key.A, ModifierKeys.Alt), typeof(KeyGesture) };
            yield return new object?[] { false, true, new KeyGesture(Key.F5, ModifierKeys.Windows), typeof(MouseGesture) };
            yield return new object?[] { false, true, new KeyGesture(Key.A, ModifierKeys.Alt), typeof(Key) };
            yield return new object?[] { false, true, new KeyGesture(Key.F5, ModifierKeys.Windows), typeof(ModifierKeys) };
            // Unsupported cases (Wrong Context Instance)
            yield return new object?[] { false, true, new MouseGesture(MouseAction.LeftClick, ModifierKeys.Alt), typeof(string) };
            yield return new object?[] { false, true, MouseAction.WheelClick, typeof(string) };
            yield return new object?[] { false, true, Key.F1, typeof(string) };

            // We do not test for malformed KeyGesture as KeyGesture has to perform its own validation and shall be enforced via its own unit tests
        }
    }

    [Theory]
    [MemberData(nameof(ConvertFrom_ReturnsExpected_Data))]
    public void ConvertFrom_ReturnsExpected(KeyGesture expected, ITypeDescriptorContext context, CultureInfo? cultureInfo, string value)
    {
        KeyGestureConverter converter = new();

        KeyGesture converted = (KeyGesture)converter.ConvertFrom(context, cultureInfo, value);
        Assert.Equal(expected.Key, converted.Key);
        Assert.Equal(expected.Modifiers, converted.Modifiers);
        Assert.Equal(expected.DisplayString, converted.DisplayString);
    }

    public static IEnumerable<object?[]> ConvertFrom_ReturnsExpected_Data
    {
        get
        {
            // Supported special case
            yield return new object?[] { new KeyGesture(Key.None, ModifierKeys.None, validateGesture: false), null, CultureInfo.InvariantCulture, string.Empty };

            // Supported cases (Culture must stay irrelevant, Key/ModifierKeys also do not care)
            yield return new object?[] { new KeyGesture(Key.NumLock, ModifierKeys.Control), null, CultureInfo.InvariantCulture, "Ctrl+NumLock" };
            yield return new object?[] { new KeyGesture(Key.A, ModifierKeys.Alt), null, CultureInfo.InvariantCulture, "Alt+A" };
            yield return new object?[] { new KeyGesture(Key.Back, ModifierKeys.Windows, "Massive Test"), null, CultureInfo.InvariantCulture, "Windows+Backspace,Massive Test" };
            yield return new object?[] { new KeyGesture(Key.F1, ModifierKeys.Alt, "displayString"), null, CultureInfo.InvariantCulture, "Alt+F1,displayString" };
            yield return new object?[] { new KeyGesture(Key.Insert, ModifierKeys.Control | ModifierKeys.Windows | ModifierKeys.Alt), null, new CultureInfo("de-DE"), "Ctrl+Alt+Windows+Insert", };
            yield return new object?[] { new KeyGesture(Key.Insert, ModifierKeys.Control | ModifierKeys.Windows | ModifierKeys.Alt), null, CultureInfo.InvariantCulture, "Ctrl+Alt+Windows+Insert" };
            yield return new object?[] { new KeyGesture(Key.NumLock, ModifierKeys.Control | ModifierKeys.Windows), null, CultureInfo.InvariantCulture, "Ctrl+Windows+NumLock" };
            yield return new object?[] { new KeyGesture(Key.F21, ModifierKeys.Alt | ModifierKeys.Windows, "displayString"), null, CultureInfo.InvariantCulture, "Alt+Windows+F21,displayString" };
            yield return new object?[] { new KeyGesture(Key.F21, ModifierKeys.Alt | ModifierKeys.Windows, "displayString"), null, new CultureInfo("ru-RU"), "Alt+Windows+F21,displayString" };
            yield return new object?[] { new KeyGesture(Key.F8, ModifierKeys.Alt | ModifierKeys.Control, "Two Modifiers"), null, CultureInfo.InvariantCulture, "Ctrl+Alt+F8,Two Modifiers" };
            yield return new object?[] { new KeyGesture(Key.A, ModifierKeys.Alt | ModifierKeys.Windows | ModifierKeys.Control, "Test String"), null, CultureInfo.InvariantCulture, "Ctrl+Alt+Windows+A,Test String" };

            // Supported cases (fuzzed)
            yield return new object?[] { new KeyGesture(Key.A, ModifierKeys.Alt, "Accept+Plus"), null, CultureInfo.InvariantCulture, "Alt+A,Accept+Plus" };
            yield return new object?[] { new KeyGesture(Key.NumLock, ModifierKeys.Control), null, CultureInfo.InvariantCulture, "      Ctrl     +     NumLock     " };
            yield return new object?[] { new KeyGesture(Key.A, ModifierKeys.Alt), null, CultureInfo.InvariantCulture, "Alt+A " };
            yield return new object?[] { new KeyGesture(Key.Back, ModifierKeys.Windows, "Massive Test"), null, CultureInfo.InvariantCulture, "Windows+     Backspace,        Massive Test" };
            yield return new object?[] { new KeyGesture(Key.F1, ModifierKeys.Alt, ",,,,,,,,displayString"), null, CultureInfo.InvariantCulture, "Alt+F1,,,,,,,,,displayString" };
            yield return new object?[] { new KeyGesture(Key.Insert, ModifierKeys.Control | ModifierKeys.Windows | ModifierKeys.Alt), null, new CultureInfo("de-DE"), "Ctrl+Alt+Windows+Insert   ", };
            yield return new object?[] { new KeyGesture(Key.F24, ModifierKeys.Alt | ModifierKeys.Windows, ",,,  displayString"), null, CultureInfo.InvariantCulture, "  Alt+Windows+ F24  ,,,,  displayString" };
            yield return new object?[] { new KeyGesture(Key.F8, ModifierKeys.Alt | ModifierKeys.Control, "Two,,, Modifiers"), null, CultureInfo.InvariantCulture, "Ctrl+Alt+F8,Two,,, Modifiers" };
            yield return new object?[] { new KeyGesture(Key.D8, ModifierKeys.Alt | ModifierKeys.Windows | ModifierKeys.Control, ",,   Test String,"), null, CultureInfo.InvariantCulture, "Ctrl+Alt+Windows+8   ,,,   Test String,  " };
        }
    }

    [Theory]
    [MemberData(nameof(ConvertFrom_ThrowsNotSupportedException_Data))]
    public void ConvertFrom_ThrowsNotSupportedException(CultureInfo? cultureInfo, object value)
    {
        KeyGestureConverter converter = new();

        Assert.Throws<NotSupportedException>(() => converter.ConvertFrom(null, cultureInfo, value));
    }

    public static IEnumerable<object?[]> ConvertFrom_ThrowsNotSupportedException_Data
    {
        get
        {
            // This one actually comes from KeyGesture (see https://github.com/dotnet/wpf/issues/8639) [possibly TODO]
            yield return new object?[] { CultureInfo.InvariantCulture, "Z" };
            // Nulls are not supported
            yield return new object?[] { CultureInfo.InvariantCulture, null };
            // Anything that isn't a string ain't supported
            yield return new object?[] { CultureInfo.InvariantCulture, new MouseGesture(MouseAction.LeftClick, ModifierKeys.Control) };
            yield return new object?[] { CultureInfo.InvariantCulture, new KeyGesture(Key.V, ModifierKeys.Control) };
            yield return new object?[] { CultureInfo.InvariantCulture, ModifierKeys.Control };
            yield return new object?[] { CultureInfo.InvariantCulture, Key.V };
        }
    }

    [Theory]
    [MemberData(nameof(ConvertTo_ReturnsExpected_Data))]
    public void ConvertTo_ReturnsExpected(string expected, ITypeDescriptorContext context, CultureInfo? cultureInfo, object? value)
    {
        KeyGestureConverter converter = new();

        // Culture and context must not have any meaning
        Assert.Equal(expected, converter.ConvertTo(context, cultureInfo, value, typeof(string)));
    }

    public static IEnumerable<object?[]> ConvertTo_ReturnsExpected_Data
    {
        get
        {
            // Supported null value case that returns string.Empty
            yield return new object?[] { string.Empty, null, CultureInfo.InvariantCulture, null };

            // Supported special cases
            yield return new object?[] { string.Empty, null, CultureInfo.InvariantCulture, new KeyGesture(Key.None, ModifierKeys.None, validateGesture: false) };
            yield return new object?[] { string.Empty, null, CultureInfo.InvariantCulture, new KeyGesture(Key.None, ModifierKeys.Control, validateGesture: false) };
            yield return new object?[] { string.Empty, null, new CultureInfo("de-DE"), new KeyGesture(Key.None, ModifierKeys.Windows, validateGesture: false) };
            yield return new object?[] { string.Empty, null, new CultureInfo("ru-RU"), new KeyGesture(Key.None, ModifierKeys.Alt, validateGesture: false) };

            // Supported cases (Culture must stay irrelevant, Key/ModifierKeys also do not care)
            yield return new object?[] { "Z", null, CultureInfo.InvariantCulture, new KeyGesture(Key.Z, ModifierKeys.None, validateGesture: false) };
            yield return new object?[] { "Ctrl+NumLock", null, CultureInfo.InvariantCulture, new KeyGesture(Key.NumLock, ModifierKeys.Control) };
            yield return new object?[] { "Alt+A", null, CultureInfo.InvariantCulture, new KeyGesture(Key.A, ModifierKeys.Alt) };
            yield return new object?[] { "Windows+Backspace,Massive Test", null, CultureInfo.InvariantCulture, new KeyGesture(Key.Back, ModifierKeys.Windows, "Massive Test") };
            yield return new object?[] { "Alt+F1,displayString", null, CultureInfo.InvariantCulture, new KeyGesture(Key.F1, ModifierKeys.Alt, "displayString") };
            yield return new object?[] { "Ctrl+Alt+Windows+Insert", null, new CultureInfo("de-DE"), new KeyGesture(Key.Insert, ModifierKeys.Control | ModifierKeys.Windows | ModifierKeys.Alt) };
            yield return new object?[] { "Ctrl+Alt+Windows+Insert", null, CultureInfo.InvariantCulture, new KeyGesture(Key.Insert, ModifierKeys.Control | ModifierKeys.Windows | ModifierKeys.Alt) };
            yield return new object?[] { "Ctrl+Windows+NumLock", null, CultureInfo.InvariantCulture, new KeyGesture(Key.NumLock, ModifierKeys.Control | ModifierKeys.Windows) };
            yield return new object?[] { "Alt+Windows+F21,displayString", null, CultureInfo.InvariantCulture, new KeyGesture(Key.F21, ModifierKeys.Alt | ModifierKeys.Windows, "displayString") };
            yield return new object?[] { "Alt+Windows+F21,displayString", null, new CultureInfo("ru-RU"), new KeyGesture(Key.F21, ModifierKeys.Alt | ModifierKeys.Windows, "displayString") };
            yield return new object?[] { "Ctrl+Alt+F8,Two Modifiers", null, CultureInfo.InvariantCulture, new KeyGesture(Key.F8, ModifierKeys.Alt | ModifierKeys.Control, "Two Modifiers") };
            yield return new object?[] { "Ctrl+Alt+Windows+A,Test String", null, CultureInfo.InvariantCulture, new KeyGesture(Key.A, ModifierKeys.Alt | ModifierKeys.Windows | ModifierKeys.Control, "Test String") };
        }
    }

    [Fact]
    public void ConvertTo_ThrowsArgumentNullException()
    {
        KeyGestureConverter converter = new();

        Assert.Throws<ArgumentNullException>(() => converter.ConvertTo(null, CultureInfo.InvariantCulture, new KeyGesture(Key.C, ModifierKeys.Control), null));
    }

    [Theory]
    [MemberData(nameof(ConvertTo_ThrowsNotSupportedException_Data))]
    public void ConvertTo_ThrowsNotSupportedException(object? value, Type? destinationType)
    {
        KeyGestureConverter converter = new();

        Assert.Throws<NotSupportedException>(() => converter.ConvertTo(null, CultureInfo.InvariantCulture, value, destinationType));
    }

    public static IEnumerable<object?[]> ConvertTo_ThrowsNotSupportedException_Data
    {
        get
        {
            // Wrong destination types
            yield return new object?[] { new KeyGesture(Key.V, ModifierKeys.Control), typeof(MouseGesture) };
            yield return new object?[] { new KeyGesture(Key.V, ModifierKeys.Control), typeof(KeyGesture) };
            yield return new object?[] { new KeyGesture(Key.V, ModifierKeys.Control), typeof(Key) };
            yield return new object?[] { new KeyGesture(Key.V, ModifierKeys.Control), typeof(ModifierKeys) };
            // Wrong value types
            yield return new object?[] { new MouseGesture(MouseAction.LeftClick, ModifierKeys.Control), typeof(string) };
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
