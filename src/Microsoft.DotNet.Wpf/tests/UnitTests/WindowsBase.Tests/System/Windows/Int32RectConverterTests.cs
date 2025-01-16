// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel;
using System.ComponentModel.Design.Serialization;
using System.Globalization;

namespace System.Windows.Tests;

public class Int32RectConverterTests
{
    [Theory]
    [InlineData(null, false)]
    [InlineData(typeof(object), false)]
    [InlineData(typeof(string), true)]
    [InlineData(typeof(InstanceDescriptor), false)]
    [InlineData(typeof(Int32Rect), false)]
    public void CanConvertTo_Invoke_ReturnsExpected(Type? destinationType, bool expected)
    {
        var converter = new Int32RectConverter();
        Assert.Equal(expected, converter.CanConvertTo(null, destinationType));
        Assert.Equal(expected, converter.CanConvertTo(new CustomTypeDescriptorContext(), destinationType));
    }
    
    [Theory]
    [MemberData(nameof(Int32RectTests.ToString_TestData), MemberType = typeof(Int32RectTests))]
    public void ConvertTo_InvokeInt32RectToString_ReturnsExpected(Int32Rect matrix, string expected)
    {
        var converter = new Int32RectConverter();
        Assert.Equal(expected, converter.ConvertTo(matrix, typeof(string)));
        Assert.Equal(expected, converter.ConvertTo(new CustomTypeDescriptorContext(), null, matrix, typeof(string)));
        Assert.Equal(expected, converter.ConvertTo(new CustomTypeDescriptorContext(), CultureInfo.InvariantCulture, matrix, typeof(string)));
    }
    
    [Theory]
    [MemberData(nameof(Int32RectTests.ToString_IFormatProviderCustom_TestData), MemberType = typeof(Int32RectTests))]
    public void ConvertTo_InvokeInt32RectToStringCustomCulture_ReturnsExpected(Int32Rect matrix, string numberDecimalSeparator, string expected)
    {
        var culture = new CultureInfo("en-US");
        NumberFormatInfo formatInfo = culture.NumberFormat;
        formatInfo.NumberDecimalSeparator = numberDecimalSeparator;

        var converter = new Int32RectConverter();
        Assert.Equal(expected, converter.ConvertTo(new CustomTypeDescriptorContext(), culture, matrix, typeof(string)));
    }

    [Theory]
    [InlineData(null, "")]
    [InlineData("", "")]
    [InlineData("value", "value")]
    [InlineData(1, "1")]
    public void ConvertTo_InvokeNotInt32RectToString_ReturnsExpected(object? value, string expected)
    {
        var converter = new Int32RectConverter();
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
        yield return new object?[] { new Int32Rect(), typeof(object) };
        
        yield return new object?[] { null, typeof(Int32Rect) };
        yield return new object?[] { string.Empty, typeof(Int32Rect) };
        yield return new object?[] { "value", typeof(Int32Rect) };
        yield return new object?[] { new object(), typeof(Int32Rect) };
        yield return new object?[] { new Int32Rect(), typeof(Int32Rect) };
    }

    [Theory]
    [MemberData(nameof(ConvertTo_CantConvert_TestData))]
    public void ConvertTo_CantConvert_ThrowsNotSupportedException(object value, Type destinationType)
    {
        var converter = new Int32RectConverter();
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
        yield return new object?[] { new Int32Rect() };
    }

    [Theory]
    [MemberData(nameof(ConvertTo_NullDestinationType_TestData))]
    public void ConvertTo_NullDestinationType_ThrowsArgumentNullException(object value)
    {
        var converter = new Int32RectConverter();
        Assert.Throws<ArgumentNullException>("destinationType", () => converter.ConvertTo(value, null!));
        Assert.Throws<ArgumentNullException>("destinationType", () => converter.ConvertTo(null, null, new Int32Rect(), null!));
        Assert.Throws<ArgumentNullException>("destinationType", () => converter.ConvertTo(new CustomTypeDescriptorContext(), CultureInfo.InvariantCulture, new Int32Rect(), null!));
    }

    [Theory]
    [InlineData(null, false)]
    [InlineData(typeof(object), false)]
    [InlineData(typeof(string), true)]
    [InlineData(typeof(InstanceDescriptor), true)]
    [InlineData(typeof(Int32Rect), false)]
    public void CanConvertFrom_Invoke_ReturnsExpected(Type? sourceType, bool expected)
    {
        var converter = new Int32RectConverter();
        Assert.Equal(expected, converter.CanConvertFrom(sourceType!));
        Assert.Equal(expected, converter.CanConvertFrom(null, sourceType));
        Assert.Equal(expected, converter.CanConvertFrom(new CustomTypeDescriptorContext(), sourceType));
    }

    [Theory]
    [MemberData(nameof(Int32RectTests.Parse_TestData), MemberType = typeof(Int32RectTests))]
    public void ConvertFrom_InvokeStringValue_ReturnsExpected(object value, Int32Rect expected)
    {
        var converter = new Int32RectConverter();
        Assert.Equal(expected, converter.ConvertFrom(value));
        Assert.Equal(expected, converter.ConvertFrom(null, null, value));
        Assert.Equal(expected, converter.ConvertFrom(new CustomTypeDescriptorContext(), CultureInfo.InvariantCulture, value));
    }

    [Fact]
    public void ConvertFrom_NullValue_ThrowsNotSupportedException()
    {
        var converter = new Int32RectConverter();
        Assert.Throws<NotSupportedException>(() => converter.ConvertFrom(null!));
        Assert.Throws<NotSupportedException>(() => converter.ConvertFrom(null, null, null));
        Assert.Throws<NotSupportedException>(() => converter.ConvertFrom(new CustomTypeDescriptorContext(), CultureInfo.InvariantCulture, null));
    }

    [Theory]
    [MemberData(nameof(Int32RectTests.Parse_InvalidSource_TestData), MemberType = typeof(Int32RectTests))]
    public void ConvertFrom_InvalidStringValue_ThrowsInvalidOperationException(object value)
    {
        var converter = new Int32RectConverter();
        Assert.Throws<InvalidOperationException>(() => converter.ConvertFrom(value));
        Assert.Throws<InvalidOperationException>(() => converter.ConvertFrom(null, null, value));
        Assert.Throws<InvalidOperationException>(() => converter.ConvertFrom(new CustomTypeDescriptorContext(), CultureInfo.InvariantCulture, value));
    }

    [Theory]
    [MemberData(nameof(Int32RectTests.Parse_NotInt32_TestData), MemberType = typeof(Int32RectTests))]
    public void ConvertFrom_NotInt_ThrowsFormatException(object value)
    {
        var converter = new Int32RectConverter();
        Assert.Throws<FormatException>(() => converter.ConvertFrom(value));
        Assert.Throws<FormatException>(() => converter.ConvertFrom(null, null, value));
        Assert.Throws<FormatException>(() => converter.ConvertFrom(new CustomTypeDescriptorContext(), CultureInfo.InvariantCulture, value));
    }

    public static IEnumerable<object[]> ConvertFrom_CantConvert_TestData()
    {
        yield return new object[] { new object() };
        yield return new object[] { new Int32Rect() };
    }
    
    [Theory]
    [MemberData(nameof(ConvertFrom_CantConvert_TestData))]
    public void ConvertFrom_CantConvert_ThrowsNotSupportedException(object value)
    {
        var converter = new Int32RectConverter();
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
