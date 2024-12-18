// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel;
using System.ComponentModel.Design.Serialization;
using System.Globalization;

namespace System.Windows.Tests;

public class RectConverterTests
{
    [Theory]
    [InlineData(null, false)]
    [InlineData(typeof(object), false)]
    [InlineData(typeof(string), true)]
    [InlineData(typeof(InstanceDescriptor), false)]
    [InlineData(typeof(Rect), false)]
    public void CanConvertTo_Invoke_ReturnsExpected(Type? destinationType, bool expected)
    {
        var converter = new RectConverter();
        Assert.Equal(expected, converter.CanConvertTo(null, destinationType));
        Assert.Equal(expected, converter.CanConvertTo(new CustomTypeDescriptorContext(), destinationType));
    }
    
    [Theory]
    [MemberData(nameof(RectTests.ToString_TestData), MemberType = typeof(RectTests))]
    public void ConvertTo_InvokeRectToString_ReturnsExpected(Rect matrix, string expected)
    {
        var converter = new RectConverter();
        Assert.Equal(expected, converter.ConvertTo(matrix, typeof(string)));
        Assert.Equal(expected, converter.ConvertTo(new CustomTypeDescriptorContext(), null, matrix, typeof(string)));
        Assert.Equal(expected, converter.ConvertTo(new CustomTypeDescriptorContext(), CultureInfo.InvariantCulture, matrix, typeof(string)));
    }
    
    [Theory]
    [MemberData(nameof(RectTests.ToString_IFormatProviderCustom_TestData), MemberType = typeof(RectTests))]
    public void ConvertTo_InvokeRectToStringCustomCulture_ReturnsExpected(Rect matrix, string numberDecimalSeparator, string expected)
    {
        var culture = new CultureInfo("en-US");
        NumberFormatInfo formatInfo = culture.NumberFormat;
        formatInfo.NumberDecimalSeparator = numberDecimalSeparator;

        var converter = new RectConverter();
        Assert.Equal(expected, converter.ConvertTo(new CustomTypeDescriptorContext(), culture, matrix, typeof(string)));
    }

    [Theory]
    [InlineData(null, "")]
    [InlineData("", "")]
    [InlineData("value", "value")]
    [InlineData(1, "1")]
    public void ConvertTo_InvokeNotRectToString_ReturnsExpected(object? value, string expected)
    {
        var converter = new RectConverter();
        Assert.Equal(expected, converter.ConvertTo(value, typeof(string)));
        Assert.Equal(expected, converter.ConvertTo(new CustomTypeDescriptorContext(), null, value, typeof(string)));
        Assert.Equal(expected, converter.ConvertTo(new CustomTypeDescriptorContext(), CultureInfo.InvariantCulture, value, typeof(string)));
    }

    public static IEnumerable<object?[]> ConvertTo_CantConvert_TestData()
    {
        yield return new object?[] { null, typeof(object) };
        yield return new object?[] { string.Empty, typeof(object) };
        yield return new object?[] { "value", typeof(object) };
        yield return new object?[] { new object(), typeof(object) };
        yield return new object?[] { new Rect(), typeof(object) };
        
        yield return new object?[] { null, typeof(Rect) };
        yield return new object?[] { string.Empty, typeof(Rect) };
        yield return new object?[] { "value", typeof(Rect) };
        yield return new object?[] { new object(), typeof(Rect) };
        yield return new object?[] { new Rect(), typeof(Rect) };
    }

    [Theory]
    [MemberData(nameof(ConvertTo_CantConvert_TestData))]
    public void ConvertTo_CantConvert_ThrowsNotSupportedException(object value, Type destinationType)
    {
        var converter = new RectConverter();
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
        yield return new object?[] { new Rect() };
    }

    [Theory]
    [MemberData(nameof(ConvertTo_NullDestinationType_TestData))]
    public void ConvertTo_NullDestinationType_ThrowsArgumentNullException(object value)
    {
        var converter = new RectConverter();
        Assert.Throws<ArgumentNullException>("destinationType", () => converter.ConvertTo(value, null!));
        Assert.Throws<ArgumentNullException>("destinationType", () => converter.ConvertTo(null, null, new Rect(), null!));
        Assert.Throws<ArgumentNullException>("destinationType", () => converter.ConvertTo(new CustomTypeDescriptorContext(), CultureInfo.InvariantCulture, new Rect(), null!));
    }

    [Theory]
    [InlineData(null, false)]
    [InlineData(typeof(object), false)]
    [InlineData(typeof(string), true)]
    [InlineData(typeof(InstanceDescriptor), true)]
    [InlineData(typeof(Rect), false)]
    public void CanConvertFrom_Invoke_ReturnsExpected(Type? sourceType, bool expected)
    {
        var converter = new RectConverter();
        Assert.Equal(expected, converter.CanConvertFrom(sourceType!));
        Assert.Equal(expected, converter.CanConvertFrom(null, sourceType));
        Assert.Equal(expected, converter.CanConvertFrom(new CustomTypeDescriptorContext(), sourceType));
    }

    [Theory]
    [MemberData(nameof(RectTests.Parse_TestData), MemberType = typeof(RectTests))]
    public void ConvertFrom_InvokeStringValue_ReturnsExpected(object value, Rect expected)
    {
        var converter = new RectConverter();
        Assert.Equal(expected, converter.ConvertFrom(value));
        Assert.Equal(expected, converter.ConvertFrom(null, null, value));
        Assert.Equal(expected, converter.ConvertFrom(new CustomTypeDescriptorContext(), CultureInfo.InvariantCulture, value));
    }

    [Fact]
    public void ConvertFrom_NullValue_ThrowsNotSupportedException()
    {
        var converter = new RectConverter();
        Assert.Throws<NotSupportedException>(() => converter.ConvertFrom(null!));
        Assert.Throws<NotSupportedException>(() => converter.ConvertFrom(null, null, null));
        Assert.Throws<NotSupportedException>(() => converter.ConvertFrom(new CustomTypeDescriptorContext(), CultureInfo.InvariantCulture, null));
    }

    [Theory]
    [MemberData(nameof(RectTests.Parse_InvalidSource_TestData), MemberType = typeof(RectTests))]
    public void ConvertFrom_InvalidStringValue_ThrowsInvalidOperationException(object value)
    {
        var converter = new RectConverter();
        Assert.Throws<InvalidOperationException>(() => converter.ConvertFrom(value));
        Assert.Throws<InvalidOperationException>(() => converter.ConvertFrom(null, null, value));
        Assert.Throws<InvalidOperationException>(() => converter.ConvertFrom(new CustomTypeDescriptorContext(), CultureInfo.InvariantCulture, value));
    }

    [Theory]
    [MemberData(nameof(RectTests.Parse_NotDouble_TestData), MemberType = typeof(RectTests))]
    public void ConvertFrom_NotDoubleStringValue_ThrowsFormatException(object value)
    {
        var converter = new RectConverter();
        Assert.Throws<FormatException>(() => converter.ConvertFrom(value));
        Assert.Throws<FormatException>(() => converter.ConvertFrom(null, null, value));
        Assert.Throws<FormatException>(() => converter.ConvertFrom(new CustomTypeDescriptorContext(), CultureInfo.InvariantCulture, value));
    }

    [Theory]
    [MemberData(nameof(RectTests.Parse_Negative_TestData), MemberType = typeof(RectTests))]
    public void ConvertFrom_Negative_ThrowsArgumentException(object value)
    {
        var converter = new RectConverter();
        Assert.Throws<ArgumentException>(() => converter.ConvertFrom(value));
        Assert.Throws<ArgumentException>(() => converter.ConvertFrom(null, null, value));
        Assert.Throws<ArgumentException>(() => converter.ConvertFrom(new CustomTypeDescriptorContext(), CultureInfo.InvariantCulture, value));
    }

    public static IEnumerable<object[]> ConvertFrom_CantConvert_TestData()
    {
        yield return new object[] { new object() };
        yield return new object[] { new Rect() };
    }
    
    [Theory]
    [MemberData(nameof(ConvertFrom_CantConvert_TestData))]
    public void ConvertFrom_CantConvert_ThrowsNotSupportedException(object value)
    {
        var converter = new RectConverter();
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
