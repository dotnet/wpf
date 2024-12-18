// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel;
using System.ComponentModel.Design.Serialization;
using System.Globalization;

namespace System.Windows.Input.Tests;

public class KeyConverterTests
{
    public static IEnumerable<object?[]> CanConvertTo_TestData()
    {
        yield return new object?[] { null, null, false };
        yield return new object?[] { null, typeof(object), false };
        yield return new object?[] { null, typeof(string), false };
        yield return new object?[] { null, typeof(InstanceDescriptor), false };
        yield return new object?[] { null, typeof(Key), false };
        yield return new object?[] { new CustomTypeDescriptorContext(), null, false };
        yield return new object?[] { new CustomTypeDescriptorContext(), typeof(object), false };
        yield return new object?[] { new CustomTypeDescriptorContext(), typeof(string), false };
        yield return new object?[] { new CustomTypeDescriptorContext(), typeof(InstanceDescriptor), false };
        yield return new object?[] { new CustomTypeDescriptorContext(), typeof(Key), false };
        yield return new object?[] { new CustomTypeDescriptorContext { Instance = new object() }, null, false };
        yield return new object?[] { new CustomTypeDescriptorContext { Instance = new object() }, typeof(object), false };
        // TODO: this should not throw.
        //yield return new object?[] { new CustomTypeDescriptorContext { Instance = new object() }, typeof(string), false };
        yield return new object?[] { new CustomTypeDescriptorContext { Instance = new object() }, typeof(InstanceDescriptor), false };
        yield return new object?[] { new CustomTypeDescriptorContext { Instance = new object() }, typeof(Key), false };
        yield return new object?[] { new CustomTypeDescriptorContext { Instance = Key.None }, null, false };
        yield return new object?[] { new CustomTypeDescriptorContext { Instance = Key.None }, typeof(object), false };
        yield return new object?[] { new CustomTypeDescriptorContext { Instance = Key.None }, typeof(string), true };
        yield return new object?[] { new CustomTypeDescriptorContext { Instance = Key.None }, typeof(InstanceDescriptor), false };
        yield return new object?[] { new CustomTypeDescriptorContext { Instance = Key.None }, typeof(Key), false };
        yield return new object?[] { new CustomTypeDescriptorContext { Instance = Key.Cancel }, null, false };
        yield return new object?[] { new CustomTypeDescriptorContext { Instance = Key.Cancel }, typeof(object), false };
        yield return new object?[] { new CustomTypeDescriptorContext { Instance = Key.Cancel }, typeof(string), true };
        yield return new object?[] { new CustomTypeDescriptorContext { Instance = Key.Cancel }, typeof(InstanceDescriptor), false };
        yield return new object?[] { new CustomTypeDescriptorContext { Instance = Key.Cancel }, typeof(Key), false };
        yield return new object?[] { new CustomTypeDescriptorContext { Instance = Key.A }, null, false };
        yield return new object?[] { new CustomTypeDescriptorContext { Instance = Key.A }, typeof(object), false };
        yield return new object?[] { new CustomTypeDescriptorContext { Instance = Key.A }, typeof(string), true };
        yield return new object?[] { new CustomTypeDescriptorContext { Instance = Key.A }, typeof(InstanceDescriptor), false };
        yield return new object?[] { new CustomTypeDescriptorContext { Instance = Key.A }, typeof(Key), false };
        yield return new object?[] { new CustomTypeDescriptorContext { Instance = Key.OemClear }, null, false };
        yield return new object?[] { new CustomTypeDescriptorContext { Instance = Key.OemClear }, typeof(object), false };
        yield return new object?[] { new CustomTypeDescriptorContext { Instance = Key.OemClear }, typeof(string), true };
        yield return new object?[] { new CustomTypeDescriptorContext { Instance = Key.OemClear }, typeof(InstanceDescriptor), false };
        yield return new object?[] { new CustomTypeDescriptorContext { Instance = Key.OemClear }, typeof(Key), false };
        yield return new object?[] { new CustomTypeDescriptorContext { Instance = Key.DeadCharProcessed }, null, false };
        yield return new object?[] { new CustomTypeDescriptorContext { Instance = Key.DeadCharProcessed }, typeof(object), false };
        yield return new object?[] { new CustomTypeDescriptorContext { Instance = Key.DeadCharProcessed }, typeof(string), true };
        yield return new object?[] { new CustomTypeDescriptorContext { Instance = Key.DeadCharProcessed }, typeof(InstanceDescriptor), false };
        yield return new object?[] { new CustomTypeDescriptorContext { Instance = Key.DeadCharProcessed }, typeof(Key), false };
        yield return new object?[] { new CustomTypeDescriptorContext { Instance = Key.None - 1 }, null, false };
        yield return new object?[] { new CustomTypeDescriptorContext { Instance = Key.None - 1 }, typeof(object), false };
        yield return new object?[] { new CustomTypeDescriptorContext { Instance = Key.None - 1 }, typeof(string), false };
        yield return new object?[] { new CustomTypeDescriptorContext { Instance = Key.None - 1 }, typeof(InstanceDescriptor), false };
        yield return new object?[] { new CustomTypeDescriptorContext { Instance = Key.None - 1 }, typeof(Key), false };
        yield return new object?[] { new CustomTypeDescriptorContext { Instance = Key.DeadCharProcessed + 1 }, null, false };
        yield return new object?[] { new CustomTypeDescriptorContext { Instance = Key.DeadCharProcessed + 1 }, typeof(object), false };
        yield return new object?[] { new CustomTypeDescriptorContext { Instance = Key.DeadCharProcessed + 1 }, typeof(string), false };
        yield return new object?[] { new CustomTypeDescriptorContext { Instance = Key.DeadCharProcessed + 1 }, typeof(InstanceDescriptor), false };
        yield return new object?[] { new CustomTypeDescriptorContext { Instance = Key.DeadCharProcessed + 1 }, typeof(Key), false };
    }

    [Theory]
    [MemberData(nameof(CanConvertTo_TestData))]
    public void CanConvertTo_Invoke_ReturnsExpected(ITypeDescriptorContext context, Type destinationType, bool expected)
    {
        var converter = new KeyConverter();
        Assert.Equal(expected, converter.CanConvertTo(context, destinationType));
    }

    [Fact]
    public void CanConvertTo_InvokeToStringInstanceNotKey_ThrowsInvalidCastException()
    {
        // TODO: this should return false.
        var converter = new KeyConverter();
        var context = new CustomTypeDescriptorContext { Instance = new object() };
        Assert.Throws<InvalidCastException>(() => converter.CanConvertTo(context, typeof(string)));
    }

    public static IEnumerable<object[]> ConvertTo_KeyToString_TestData()
    {
        yield return new object[] { Key.None, "" };
        yield return new object[] { Key.Cancel, "Cancel" };
        yield return new object[] { Key.Back, "Backspace" };
        yield return new object[] { Key.Tab, "Tab" };
        yield return new object[] { Key.LineFeed, "Clear" };
        yield return new object[] { Key.Return, "Return" };
        yield return new object[] { Key.HanjaMode, "HanjaMode" };
        yield return new object[] { Key.Escape, "Esc" };
        yield return new object[] { Key.ImeConvert, "ImeConvert" };
        yield return new object[] { Key.Help, "Help" };
        yield return new object[] { Key.D0, "0" };
        yield return new object[] { Key.D1, "1" };
        yield return new object[] { Key.D2, "2" };
        yield return new object[] { Key.D3, "3" };
        yield return new object[] { Key.D4, "4" };
        yield return new object[] { Key.D5, "5" };
        yield return new object[] { Key.D6, "6" };
        yield return new object[] { Key.D7, "7" };
        yield return new object[] { Key.D8, "8" };
        yield return new object[] { Key.D9, "9" };
        yield return new object[] { Key.A, "A" };
        yield return new object[] { Key.B, "B" };
        yield return new object[] { Key.C, "C" };
        yield return new object[] { Key.D, "D" };
        yield return new object[] { Key.E, "E" };
        yield return new object[] { Key.F, "F" };
        yield return new object[] { Key.G, "G" };
        yield return new object[] { Key.H, "H" };
        yield return new object[] { Key.I, "I" };
        yield return new object[] { Key.J, "J" };
        yield return new object[] { Key.K, "K" };
        yield return new object[] { Key.L, "L" };
        yield return new object[] { Key.M, "M" };
        yield return new object[] { Key.N, "N" };
        yield return new object[] { Key.O, "O" };
        yield return new object[] { Key.P, "P" };
        yield return new object[] { Key.Q, "Q" };
        yield return new object[] { Key.R, "R" };
        yield return new object[] { Key.S, "S" };
        yield return new object[] { Key.T, "T" };
        yield return new object[] { Key.U, "U" };
        yield return new object[] { Key.V, "V" };
        yield return new object[] { Key.W, "W" };
        yield return new object[] { Key.X, "X" };
        yield return new object[] { Key.Y, "Y" };
        yield return new object[] { Key.Z, "Z" };
        yield return new object[] { Key.LWin, "LWin" };
        yield return new object[] { Key.DeadCharProcessed, "DeadCharProcessed" };
    }

    [Theory]
    [MemberData(nameof(ConvertTo_KeyToString_TestData))]
    public void ConvertTo_InvokeKeyToString_ReturnsExpected(Key value, string expected)
    {
        var converter = new KeyConverter();
        Assert.Equal(expected, converter.ConvertTo(value, typeof(string)));
        Assert.Equal(expected, converter.ConvertTo(new CustomTypeDescriptorContext(), null, value, typeof(string)));
        Assert.Equal(expected, converter.ConvertTo(new CustomTypeDescriptorContext(), CultureInfo.InvariantCulture, value, typeof(string)));
    }
    
    [Theory]
    [InlineData((Key)int.MinValue)]
    [InlineData((Key)(-1))]
    [InlineData(Key.DeadCharProcessed + 1)]
    [InlineData((Key)int.MaxValue)]
    public void ConvertTo_InvalidKey_ThrowsNotSupportedException(Key value)
    {
        var converter = new KeyConverter();
        Assert.Throws<NotSupportedException>(() => converter.ConvertTo(value, typeof(string)));
        Assert.Throws<NotSupportedException>(() => converter.ConvertTo(new CustomTypeDescriptorContext(), null, value, typeof(string)));
        Assert.Throws<NotSupportedException>(() => converter.ConvertTo(new CustomTypeDescriptorContext(), CultureInfo.InvariantCulture, value, typeof(string)));
    }

    [Theory]
    [InlineData(null)]
    // TODO: this should not throw InvalidCastException.
    //[InlineData("", "")]
    //[InlineData("value", "value")]
    public void ConvertTo_InvokeNotKeyToStringNull_ThrowsNotSupportedException(object? value)
    {
        var converter = new KeyConverter();
        Assert.Throws<NotSupportedException>(() => converter.ConvertTo(value, typeof(string)));
        Assert.Throws<NotSupportedException>(() => converter.ConvertTo(new CustomTypeDescriptorContext(), null, value, typeof(string)));
        Assert.Throws<NotSupportedException>(() => converter.ConvertTo(new CustomTypeDescriptorContext(), CultureInfo.InvariantCulture, value, typeof(string)));
    }

    [Theory]
    [InlineData("")]
    [InlineData("value")]
    public void ConvertTo_InvokeNotKeyToStringNotNull_ThrowsInvalidCastException(object value)
    {
        // TODO: this should not throw InvalidCastException.
        var converter = new KeyConverter();
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
        var converter = new KeyConverter();
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
        yield return new object?[] { Key.None };
    }

    [Theory]
    [MemberData(nameof(ConvertTo_NullDestinationType_TestData))]
    public void ConvertTo_NullDestinationType_ThrowsArgumentNullException(object value)
    {
        var converter = new KeyConverter();
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
        var converter = new KeyConverter();
        Assert.Equal(expected, converter.CanConvertFrom(sourceType!));
        Assert.Equal(expected, converter.CanConvertFrom(null, sourceType));
        Assert.Equal(expected, converter.CanConvertFrom(new CustomTypeDescriptorContext(), sourceType));
    }

    public static IEnumerable<object[]> ConvertFrom_TestData()
    {
        yield return new object[] { "", Key.None };
        yield return new object[] { "  ", Key.None };
        yield return new object[] { "0", Key.D0 };
        yield return new object[] { " 0 ", Key.D0 };
        yield return new object[] { "1", Key.D1 };
        yield return new object[] { "2", Key.D2 };
        yield return new object[] { "3", Key.D3 };
        yield return new object[] { "4", Key.D4 };
        yield return new object[] { "5", Key.D5 };
        yield return new object[] { "6", Key.D6 };
        yield return new object[] { "7", Key.D7 };
        yield return new object[] { "8", Key.D8 };
        yield return new object[] { "9", Key.D9 };
        yield return new object[] { "A", Key.A };
        yield return new object[] { "B", Key.B };
        yield return new object[] { "C", Key.C };
        yield return new object[] { "D", Key.D };
        yield return new object[] { "E", Key.E };
        yield return new object[] { "F", Key.F };
        yield return new object[] { "G", Key.G };
        yield return new object[] { "H", Key.H };
        yield return new object[] { "I", Key.I };
        yield return new object[] { "J", Key.J };
        yield return new object[] { "K", Key.K };
        yield return new object[] { "L", Key.L };
        yield return new object[] { "M", Key.M };
        yield return new object[] { "N", Key.N };
        yield return new object[] { "O", Key.O };
        yield return new object[] { "P", Key.P };
        yield return new object[] { "Q", Key.Q };
        yield return new object[] { "R", Key.R };
        yield return new object[] { "S", Key.S };
        yield return new object[] { "T", Key.T };
        yield return new object[] { "U", Key.U };
        yield return new object[] { "V", Key.V };
        yield return new object[] { "W", Key.W };
        yield return new object[] { "X", Key.X };
        yield return new object[] { "Y", Key.Y };
        yield return new object[] { "Z", Key.Z };
        yield return new object[] { "a", Key.A };
        yield return new object[] { "b", Key.B };
        yield return new object[] { "c", Key.C };
        yield return new object[] { "d", Key.D };
        yield return new object[] { "e", Key.E };
        yield return new object[] { "f", Key.F };
        yield return new object[] { "g", Key.G };
        yield return new object[] { "h", Key.H };
        yield return new object[] { "i", Key.I };
        yield return new object[] { "j", Key.J };
        yield return new object[] { "k", Key.K };
        yield return new object[] { "l", Key.L };
        yield return new object[] { "m", Key.M };
        yield return new object[] { "n", Key.N };
        yield return new object[] { "o", Key.O };
        yield return new object[] { "p", Key.P };
        yield return new object[] { "q", Key.Q };
        yield return new object[] { "r", Key.R };
        yield return new object[] { "s", Key.S };
        yield return new object[] { "t", Key.T };
        yield return new object[] { "u", Key.U };
        yield return new object[] { "v", Key.V };
        yield return new object[] { "w", Key.W };
        yield return new object[] { "x", Key.X };
        yield return new object[] { "y", Key.Y };
        yield return new object[] { "z", Key.Z };
        yield return new object[] { "ENTER", Key.Return };
        yield return new object[] { " ENTER ", Key.Return };
        yield return new object[] { " eNtEr ", Key.Return };
        yield return new object[] { "ESC", Key.Escape };
        yield return new object[] { "PGUP", Key.PageUp };
        yield return new object[] { "PGDN", Key.PageDown };
        yield return new object[] { "PRTSC", Key.PrintScreen };
        yield return new object[] { "INS", Key.Insert };
        yield return new object[] { "DEL", Key.Delete };
        yield return new object[] { "WINDOWS", Key.LWin };
        yield return new object[] { "WIN", Key.LWin };
        yield return new object[] { "LEFTWINDOWS", Key.LWin };
        yield return new object[] { "RIGHTWINDOWS", Key.RWin };
        yield return new object[] { "APPS", Key.Apps };
        yield return new object[] { "APPLICATION", Key.Apps };
        yield return new object[] { "BREAK", Key.Cancel };
        yield return new object[] { "BACKSPACE", Key.Back };
        yield return new object[] { "BKSP", Key.Back };
        yield return new object[] { "BS", Key.Back };
        yield return new object[] { "SHIFT", Key.LeftShift };
        yield return new object[] { "LEFTSHIFT", Key.LeftShift };
        yield return new object[] { "RIGHTSHIFT", Key.RightShift };
        yield return new object[] { "CONTROL", Key.LeftCtrl };
        yield return new object[] { "CTRL", Key.LeftCtrl };
        yield return new object[] { "LEFTCTRL", Key.LeftCtrl };
        yield return new object[] { "RIGHTCTRL", Key.RightCtrl };
        yield return new object[] { "ALT", Key.LeftAlt };
        yield return new object[] { "LEFTALT", Key.LeftAlt };
        yield return new object[] { "RIGHTALT", Key.RightAlt };
        yield return new object[] { "SEMICOLON", Key.OemSemicolon };
        yield return new object[] { "PLUS", Key.OemPlus };
        yield return new object[] { "COMMA", Key.OemComma };
        yield return new object[] { "MINUS", Key.OemMinus };
        yield return new object[] { "PERIOD", Key.OemPeriod };
        yield return new object[] { "QUESTION", Key.OemQuestion };
        yield return new object[] { "TILDE", Key.OemTilde };
        yield return new object[] { "OPENBRACKETS", Key.OemOpenBrackets };
        yield return new object[] { "PIPE", Key.OemPipe };
        yield return new object[] { "CLOSEBRACKETS", Key.OemCloseBrackets };
        yield return new object[] { "QUOTES", Key.OemQuotes };
        yield return new object[] { "BACKSLASH", Key.OemBackslash };
        yield return new object[] { "FINISH", Key.OemFinish };
        yield return new object[] { "ATTN", Key.Attn };
        yield return new object[] { "CRSEL", Key.CrSel };
        yield return new object[] { "EXSEL", Key.ExSel };
        yield return new object[] { "ERASEEOF", Key.EraseEof };
        yield return new object[] { "PLAY", Key.Play };
        yield return new object[] { "ZOOM", Key.Zoom };
        yield return new object[] { "PA1", Key.Pa1 };
        
        yield return new object[] { "Cancel", Key.Cancel };
        yield return new object[] { "CANCEL", Key.Cancel };
        yield return new object[] { " CANCEL ", Key.Cancel };
    }

    [Theory]
    [MemberData(nameof(ConvertFrom_TestData))]
    public void ConvertFrom_InvokeStringValue_ReturnsExpected(string value, Key expected)
    {
        var converter = new KeyConverter();
        Assert.Equal(expected, converter.ConvertFrom(value));
        Assert.Equal(expected, converter.ConvertFrom(null, null, value));
        Assert.Equal(expected, converter.ConvertFrom(new CustomTypeDescriptorContext(), CultureInfo.InvariantCulture, value));
    }

    [Fact]
    public void ConvertFrom_NullValue_ThrowsNotSupportedException()
    {
        var converter = new KeyConverter();
        Assert.Throws<NotSupportedException>(() => converter.ConvertFrom(null!));
        Assert.Throws<NotSupportedException>(() => converter.ConvertFrom(null, null, null));
        Assert.Throws<NotSupportedException>(() => converter.ConvertFrom(new CustomTypeDescriptorContext(), CultureInfo.InvariantCulture, null));
    }

    public static IEnumerable<object[]> ConvertFrom_InvalidValue_TestData()
    {
        yield return new object[] { "_" };
        yield return new object[] { " _ " };
        yield return new object[] { "\u0663" };
        yield return new object[] { " \u0663 " };
        yield return new object[] { "\u0409" };
        yield return new object[] { " \u0409 " };
        yield return new object[] { "NOSUCHKEY" };
        yield return new object[] { " NOSUCHKEY " };
    }

    [Theory]
    [MemberData(nameof(ConvertFrom_InvalidValue_TestData))]
    public void ConvertFrom_InvokeInvalidValue_ThrowsArgumentException(string value)
    {
        var converter = new KeyConverter();
        // TODO: add paramName.
        Assert.Throws<ArgumentException>(() => converter.ConvertFrom(value));
        Assert.Throws<ArgumentException>(() => converter.ConvertFrom(null, null, value));
        Assert.Throws<ArgumentException>(() => converter.ConvertFrom(new CustomTypeDescriptorContext(), CultureInfo.InvariantCulture, value));
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
        var converter = new KeyConverter();
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
