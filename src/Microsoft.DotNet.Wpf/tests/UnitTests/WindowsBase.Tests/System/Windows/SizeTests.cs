// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel;
using System.Globalization;
using System.Windows.Converters;
using System.Windows.Markup;

namespace System.Windows.Tests;

public class SizeTests
{
    [Fact]
    public void Ctor_Default()
    {
        var size = new Size();
        Assert.Equal(0, size.Width);
        Assert.Equal(0, size.Height);
        Assert.False(size.IsEmpty);
    }

    [Theory]
    [InlineData(double.NegativeZero, double.NegativeZero)]
    [InlineData(0, 0)]
    [InlineData(1, 0)]
    [InlineData(0, 2)]
    [InlineData(0.1, 0.2)]
    [InlineData(1, 2)]
    [InlineData(double.MaxValue, double.MaxValue)]
    [InlineData(double.PositiveInfinity, double.PositiveInfinity)]
    [InlineData(double.NaN, double.NaN)]
    [InlineData(double.NaN, 1)]
    [InlineData(1, double.NaN)]
    public void Ctor_Double_Double(double width, double height)
    {
        var size = new Size(width, height);
        Assert.Equal(width, size.Width);
        Assert.Equal(height, size.Height);
        Assert.False(size.IsEmpty);
    }

    [Theory]
    [InlineData(double.NegativeInfinity)]
    [InlineData(double.MinValue)]
    [InlineData(-1)]
    public void Ctor_NegativeWidth_ThrowsArgumentException(double width)
    {
        // TODO: should have paramName
        Assert.Throws<ArgumentException>(() => new Size(width, 0));
    }

    [Theory]
    [InlineData(double.NegativeInfinity)]
    [InlineData(double.MinValue)]
    [InlineData(-1)]
    public void Ctor_NegativeHeight_ThrowsArgumentException(double height)
    {
        // TODO: should have paramName
        Assert.Throws<ArgumentException>(() => new Size(0, height));
    }

    [Fact]
    public void Empty_Get_ReturnsExpected()
    {
        Size size = Size.Empty;
        Assert.Equal(double.NegativeInfinity, size.Width);
        Assert.Equal(double.NegativeInfinity, size.Height);
        Assert.True(size.IsEmpty);
    }

    [Theory]
    [InlineData(double.NegativeZero)]
    [InlineData(0)]
    [InlineData(0.2)]
    [InlineData(1)]
    [InlineData(double.MaxValue)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NaN)]
    public void Width_Set_GetReturnsExpected(double value)
    {
        var size = new Size
        {
            // Set.
            Width = value
        };
        Assert.Equal(value, size.Width);

        // Set same.
        size.Width = value;
        Assert.Equal(value, size.Width);
    }

    [Theory]
    [InlineData(double.NegativeInfinity)]
    [InlineData(double.MinValue)]
    [InlineData(-1)]
    public void Width_SetNegative_ThrowsArgumentException(double value)
    {
        var size = new Size();
        // TODO: should have paramName
        Assert.Throws<ArgumentException>(() => size.Width = value);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(1)]
    public void Width_SetEmpty_ThrowsInvalidOperationException(double value)
    {
        var size = Size.Empty;
        Assert.Throws<InvalidOperationException>(() => size.Width = value);
    }

    [Theory]
    [InlineData(double.NegativeZero)]
    [InlineData(0)]
    [InlineData(0.2)]
    [InlineData(1)]
    [InlineData(double.MaxValue)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NaN)]
    public void Height_Set_GetReturnsExpected(double value)
    {
        var size = new Size
        {
            // Set.
            Height = value
        };
        Assert.Equal(value, size.Height);

        // Set same.
        size.Height = value;
        Assert.Equal(value, size.Height);
    }

    [Theory]
    [InlineData(double.NegativeInfinity)]
    [InlineData(double.MinValue)]
    [InlineData(-1)]
    public void Height_SetNegative_ThrowsArgumentException(double value)
    {
        var size = new Size();
        // TODO: should have paramName
        Assert.Throws<ArgumentException>(() => size.Height = value);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(1)]
    public void Height_SetEmpty_ThrowsInvalidOperationException(double value)
    {
        var size = Size.Empty;
        Assert.Throws<InvalidOperationException>(() => size.Height = value);
    }

    public static IEnumerable<object?[]> Equals_TestData()
    {
        // Normal size.
        yield return new object?[] { new Size(1, 2), new Size(1, 2), true, true };
        yield return new object?[] { new Size(1, 2), new Size(2, 2), false, false };
        yield return new object?[] { new Size(1, 2), new Size(1, 3), false, false };
        yield return new object?[] { new Size(1, 2), new Size(double.NaN, 2), false, false };
        yield return new object?[] { new Size(1, 2), new Size(1, double.NaN), false, false };
        yield return new object?[] { new Size(1, 2), new Size(double.NaN, double.NaN), false, false };
        yield return new object?[] { new Size(1, 2), new Size(0, 0), false, false };
        yield return new object?[] { new Size(1, 2), new Size(), false, false };
        yield return new object?[] { new Size(1, 2), Size.Empty, false, false };

        // NaN width.
        yield return new object[] { new Size(double.NaN, 2), new Size(double.NaN, 2), true, true };
        yield return new object[] { new Size(double.NaN, 2), new Size(2, 2), false, false };
        yield return new object[] { new Size(double.NaN, 2), new Size(double.NaN, double.NaN), false, false };

        // NaN height.
        yield return new object[] { new Size(1, double.NaN), new Size(1, double.NaN), true, true };
        yield return new object[] { new Size(1, double.NaN), new Size(1, 2), false, false };
        yield return new object[] { new Size(1, double.NaN), new Size(double.NaN, double.NaN), false, false };

        // NaN width & height.
        yield return new object[] { new Size(double.NaN, double.NaN), new Size(double.NaN, double.NaN), true, true };
        yield return new object[] { new Size(double.NaN, double.NaN), new Size(1, 2), false, false };
        yield return new object[] { new Size(double.NaN, double.NaN), new Size(double.NaN, 2), false, false };
        yield return new object[] { new Size(double.NaN, double.NaN), new Size(1, double.NaN), false, false };

        // Zero.
        yield return new object?[] { new Size(0, 0), new Size(), true, true };
        yield return new object?[] { new Size(0, 0), new Size(0, 0), true, true };
        yield return new object?[] { new Size(0, 0), new Size(1, 0), false, false };
        yield return new object?[] { new Size(0, 0), new Size(0, 1), false, false };
        yield return new object?[] { new Size(0, 0), Size.Empty, false, true };

        // Default.
        yield return new object?[] { new Size(), new Size(), true, true };
        yield return new object?[] { new Size(), new Size(0, 0), true, true };
        yield return new object?[] { new Size(), new Size(1, 0), false, false };
        yield return new object?[] { new Size(), new Size(0, 1), false, false };
        yield return new object?[] { new Size(), Size.Empty, false, true };

        // Empty.
        yield return new object?[] { Size.Empty, Size.Empty, true, true };
        yield return new object?[] { Size.Empty, new Size(1, 2), false, false };
        yield return new object?[] { Size.Empty, new Size(), false, true };
        yield return new object?[] { Size.Empty, new Size(0, 0), false, true };

        // Other.
        yield return new object?[] { Size.Empty, new object(), false, false };
        yield return new object?[] { Size.Empty, null, false, false };
        yield return new object?[] { new Size(), new object(), false, false };
        yield return new object?[] { new Size(), null, false, false };
        yield return new object?[] { new Size(0, 0), new object(), false, false };
        yield return new object?[] { new Size(0, 0), null, false, false };
        yield return new object?[] { new Size(1, 2), new object(), false, false };
        yield return new object?[] { new Size(1, 2), null, false, false };
    }

    [Theory]
    [MemberData(nameof(Equals_TestData))]
    public void Equals_Object_ReturnsExpected(Size size, object o, bool expected, bool expectedHashCode)
    {
        Assert.Equal(expected, size.Equals(o));
        if (o is Size value)
        {
            Assert.Equal(expected, size.Equals(value));
            Assert.Equal(expected, value.Equals(size));
            Assert.Equal(expected, Size.Equals(size, value));
            Assert.Equal(expected, Size.Equals(value, size));
            Assert.Equal(expectedHashCode, size.GetHashCode().Equals(value.GetHashCode()));
        }
    }

    public static IEnumerable<object[]> EqualityOperator_TestData()
    {
        // Normal size.
        yield return new object[] { new Size(1, 2), new Size(1, 2), true };
        yield return new object[] { new Size(1, 2), new Size(2, 2), false };
        yield return new object[] { new Size(1, 2), new Size(1, 3), false };
        yield return new object[] { new Size(1, 2), new Size(double.NaN, 2), false };
        yield return new object[] { new Size(1, 2), new Size(1, double.NaN), false };
        yield return new object[] { new Size(1, 2), new Size(double.NaN, double.NaN), false };
        yield return new object[] { new Size(1, 2), new Size(0, 0), false };
        yield return new object[] { new Size(1, 2), new Size(), false };
        yield return new object[] { new Size(1, 2), Size.Empty, false };

        // NaN width.
        yield return new object[] { new Size(double.NaN, 2), new Size(double.NaN, 2), false };
        yield return new object[] { new Size(double.NaN, 2), new Size(2, 2), false };
        yield return new object[] { new Size(double.NaN, 2), new Size(double.NaN, double.NaN), false };

        // NaN height.
        yield return new object[] { new Size(1, double.NaN), new Size(1, double.NaN), false };
        yield return new object[] { new Size(1, double.NaN), new Size(1, 2), false };
        yield return new object[] { new Size(1, double.NaN), new Size(double.NaN, double.NaN), false };

        // NaN width & height.
        yield return new object[] { new Size(double.NaN, double.NaN), new Size(double.NaN, double.NaN), false };
        yield return new object[] { new Size(double.NaN, double.NaN), new Size(1, 2), false };
        yield return new object[] { new Size(double.NaN, double.NaN), new Size(double.NaN, 2), false };
        yield return new object[] { new Size(double.NaN, double.NaN), new Size(1, double.NaN), false };

        // Zero.
        yield return new object[] { new Size(0, 0), new Size(), true };
        yield return new object[] { new Size(0, 0), new Size(0, 0), true };
        yield return new object[] { new Size(0, 0), new Size(1, 0), false };
        yield return new object[] { new Size(0, 0), new Size(0, 1), false };
        yield return new object[] { new Size(0, 0), Size.Empty, false };

        // Default.
        yield return new object[] { new Size(), new Size(), true };
        yield return new object[] { new Size(), new Size(0, 0), true };
        yield return new object[] { new Size(), new Size(1, 0), false };
        yield return new object[] { new Size(), new Size(0, 1), false };
        yield return new object[] { new Size(), Size.Empty, false };

        // Empty.
        yield return new object[] { Size.Empty, Size.Empty, true };
        yield return new object[] { Size.Empty, new Size(1, 2), false };
        yield return new object[] { Size.Empty, new Size(), false };
        yield return new object[] { Size.Empty, new Size(0, 0), false };
    }

    [Theory]
    [MemberData(nameof(EqualityOperator_TestData))]
    public void EqualityOperator_Invoke_ReturnsExpected(Size size, Size value, bool expected)
    {
        Assert.Equal(expected, size == value);
        Assert.Equal(expected, value == size);
        Assert.Equal(!expected, size != value);
        Assert.Equal(!expected, value != size);
    }

    [Fact]
    public void GetHashCode_InvokeDefault_ReturnsEqual()
    {
        var size = new Size();
        Assert.Equal(0, size.GetHashCode());
        Assert.Equal(size.GetHashCode(), size.GetHashCode());
    }

    [Fact]
    public void GetHashCode_InvokeEmpty_ReturnsEqual()
    {
        Size size = Size.Empty;
        Assert.Equal(0, size.GetHashCode());
        Assert.Equal(size.GetHashCode(), size.GetHashCode());
    }

    [Fact]
    public void GetHashCode_InvokeNormal_ReturnsEqual()
    {
        var size = new Size(1, 2);
        Assert.NotEqual(0, size.GetHashCode());
        Assert.Equal(size.GetHashCode(), size.GetHashCode());
    }

    public static IEnumerable<object[]> Parse_TestData()
    {
        yield return new object[] { "Empty", Size.Empty };
        yield return new object[] { "  Empty  ", Size.Empty };
        yield return new object[] { "0,0", new Size(0, 0) };
        yield return new object[] { "1,2", new Size(1, 2) };
        yield return new object[] { "1.1,2.2", new Size(1.1, 2.2) };
        yield return new object[] { "   1   ,   2  ", new Size(1, 2) };
    }

    [Theory]
    [MemberData(nameof(Parse_TestData))]
    public void Parse_Invoke_Success(string source, Size expected)
    {
        Size result = Size.Parse(source);
        Assert.Equal(expected.Width, result.Width, precision: 5);
        Assert.Equal(expected.Height, result.Height, precision: 5);
    }

    [Fact]
    public void Parse_NullSource_ThrowsInvalidOperationException()
    {
        Assert.Throws<InvalidOperationException>(() => Size.Parse(null));
    }

    public static IEnumerable<object[]> Parse_InvalidSource_TestData()
    {
        yield return new object[] { "" };
        yield return new object[] { "  " };
        yield return new object[] { "," };
        yield return new object[] { "1" };
        yield return new object[] { "1," };
        yield return new object[] { "1,2," };
        yield return new object[] { "1,2,3" };
        yield return new object[] { "1,2,test" };
        yield return new object[] { "Empty," };
        yield return new object[] { "Identity," };
    }

    [Theory]
    [MemberData(nameof(Parse_InvalidSource_TestData))]
    public void Parse_InvalidSource_ThrowsInvalidOperationException(string source)
    {
        Assert.Throws<InvalidOperationException>(() => Size.Parse(source));
    }

    public static IEnumerable<object[]> Parse_NotDouble_TestData()
    {
        yield return new object[] { "Identity" };
        yield return new object[] { " Identity " };
        yield return new object[] { "Identity,2" };
        yield return new object[] { "test" };
        yield return new object[] { "test,2" };
        yield return new object[] { "1,test" };
        yield return new object[] { "1,test,3" };
        yield return new object[] { "1;2" };
        yield return new object[] { "1.2.3" };
        yield return new object[] { """1"",""2""" };
    }

    [Theory]
    [MemberData(nameof(Parse_NotDouble_TestData))]
    public void Parse_NotDouble_ThrowsFormatException(string source)
    {
        Assert.Throws<FormatException>(() => Size.Parse(source));
    }

    public static IEnumerable<object[]> Parse_Negative_TestData()
    {
        yield return new object[] { "-1,2" };
        yield return new object[] { "1,-2" };
    }

    [Theory]
    [MemberData(nameof(Parse_Negative_TestData))]
    public void Parse_Negative_ThrowsArgumentException(string source)
    {
        Assert.Throws<ArgumentException>(() => Size.Parse(source));
    }

    public static IEnumerable<object[]> ToString_TestData()
    {
        yield return new object[] { Size.Empty, "Empty" };
        yield return new object[] { new Size(), "0,0" };
        yield return new object[] { new Size(0, 0), "0,0" };
        yield return new object[] { new Size(1.1, 2.2), "1.1,2.2" };
        yield return new object[] { new Size(1, 2), "1,2" };
    }

    [Theory]
    [MemberData(nameof(ToString_TestData))]
    public void ToString_Invoke_ReturnsExpected(Size size, string expected)
    {
        Assert.Equal(expected, size.ToString());
    }

    [Theory]
    [MemberData(nameof(ToString_TestData))]
    public void ToString_InvokeIFormatProviderInvariantCulture_ReturnsExpected(Size size, string expected)
    {
        Assert.Equal(expected, size.ToString(CultureInfo.InvariantCulture));
    }

    public static IEnumerable<object[]> ToString_IFormatProviderCustom_TestData()
    {
        yield return new object[] { Size.Empty, "|", "Empty" };
        yield return new object[] { Size.Empty, "|_", "Empty" };
        yield return new object[] { Size.Empty, ",_", "Empty" };
        yield return new object[] { Size.Empty, ",", "Empty" };
        yield return new object[] { Size.Empty, ";", "Empty" };
        yield return new object[] { Size.Empty, " ", "Empty" };

        yield return new object[] { new Size(), "|", "0,0" };
        yield return new object[] { new Size(), "|_", "0,0" };
        yield return new object[] { new Size(), ",_", "0;0" };
        yield return new object[] { new Size(), ",", "0;0" };
        yield return new object[] { new Size(), ";", "0,0" };
        yield return new object[] { new Size(), " ", "0,0" };

        yield return new object[] { new Size(0, 0), "|", "0,0" };
        yield return new object[] { new Size(0, 0), "|_", "0,0" };
        yield return new object[] { new Size(0, 0), ",_", "0;0" };
        yield return new object[] { new Size(0, 0), ",", "0;0" };
        yield return new object[] { new Size(0, 0), ";", "0,0" };
        yield return new object[] { new Size(0, 0), " ", "0,0" };

        yield return new object[] { new Size(1, 2), "|", "1,2" };
        yield return new object[] { new Size(1, 2), "|_", "1,2" };
        yield return new object[] { new Size(1, 2), ",_", "1;2" };
        yield return new object[] { new Size(1, 2), ",", "1;2" };
        yield return new object[] { new Size(1, 2), ";", "1,2" };
        yield return new object[] { new Size(1, 2), " ", "1,2" };
        
        yield return new object[] { new Size(1.1, 2.2), "|", "1|1,2|2" };
        yield return new object[] { new Size(1.1, 2.2), "|_", "1|_1,2|_2" };
        yield return new object[] { new Size(1.1, 2.2), ",_", "1,_1;2,_2" };
        yield return new object[] { new Size(1.1, 2.2), ",", "1,1;2,2" };
        yield return new object[] { new Size(1.1, 2.2), ";", "1;1,2;2" };
        yield return new object[] { new Size(1.1, 2.2), " ", "1 1,2 2" };
    }

    [Theory]
    [MemberData(nameof(ToString_IFormatProviderCustom_TestData))]
    public void ToString_InvokeIFormatProviderCustom_ReturnsExpected(Size size, string numberDecimalSeparator, string expected)
    {
        var culture = new CultureInfo("en-US");
        NumberFormatInfo formatInfo = culture.NumberFormat;
        formatInfo.NumberDecimalSeparator = numberDecimalSeparator;

        Assert.Equal(expected, size.ToString(formatInfo));
    }

    [Theory]
    [MemberData(nameof(ToString_TestData))]
    public void ToString_InvokeIFormattableInvariantCulture_ReturnsExpected(Size size, string expected)
    {
        IFormattable formattable = size;

        Assert.Equal(expected, formattable.ToString(null, null));
        Assert.Equal(expected, formattable.ToString(null, CultureInfo.InvariantCulture));
    }

    public static IEnumerable<object[]> ToString_IFormattableCustomFormat_TestData()
    {
        yield return new object[] { "|", "1|23,2|35" };
        yield return new object[] { "|_", "1|_23,2|_35" };
        yield return new object[] { ",_", "1,_23;2,_35" };
        yield return new object[] { ",", "1,23;2,35" };
        yield return new object[] { ";", "1;23,2;35" };
        yield return new object[] { " ", "1 23,2 35" };
    }

    [Theory]
    [MemberData(nameof(ToString_IFormattableCustomFormat_TestData))]
    public void ToString_InvokeIFormattableCustomFormat_ReturnsExpected(string numberDecimalSeparator, string expected)
    {
        var size = new Size(1.23456, 2.34567);
        IFormattable formattable = size;

        var culture = new CultureInfo("en-US");
        NumberFormatInfo formatInfo = culture.NumberFormat;
        formatInfo.NumberDecimalSeparator = numberDecimalSeparator;

        Assert.Equal(expected, formattable.ToString("F2", formatInfo));
    }

    [Fact]
    public void OperatorConvertPoint_InvokeDefault_ReturnsExpected()
    {
        Point result = (Point)new Size();
        Assert.Equal(0, result.X);
        Assert.Equal(0, result.Y);
    }

    [Fact]
    public void OperatorConvertPoint_InvokeEmpty_ReturnsExpected()
    {
        Point result = (Point)Size.Empty;
        Assert.Equal(double.NegativeInfinity, result.X);
        Assert.Equal(double.NegativeInfinity, result.Y);
    }

    [Theory]
    [InlineData(0, 0)]
    [InlineData(1, 0)]
    [InlineData(0, 1)]
    [InlineData(1, 1)]
    [InlineData(1, 2)]
    [InlineData(1.1, 2.2)]
    [InlineData(double.NaN, double.NaN)]
    [InlineData(double.NaN, 0)]
    [InlineData(0, double.NaN)]
    public void OperatorConvertPoint_InvokeNormal_ReturnsExpected(double x, double y)
    {
        Point result = (Point)new Size(x, y);
        Assert.Equal(x, result.X);
        Assert.Equal(y, result.Y);
    }

    [Fact]
    public void OperatorConvertVector_InvokeDefault_ReturnsExpected()
    {
        Vector result = (Vector)new Size();
        Assert.Equal(0, result.X);
        Assert.Equal(0, result.Y);
    }

    [Fact]
    public void OperatorConvertVector_InvokeEmpty_ReturnsExpected()
    {
        Vector result = (Vector)Size.Empty;
        Assert.Equal(double.NegativeInfinity, result.X);
        Assert.Equal(double.NegativeInfinity, result.Y);
    }

    [Theory]
    [InlineData(0, 0)]
    [InlineData(1, 0)]
    [InlineData(0, 1)]
    [InlineData(1, 1)]
    [InlineData(1, 2)]
    [InlineData(1.1, 2.2)]
    [InlineData(double.NaN, double.NaN)]
    [InlineData(double.NaN, 0)]
    [InlineData(0, double.NaN)]
    public void OperatorConvertVector_InvokeNormal_ReturnsExpected(double x, double y)
    {
        Vector result = (Vector)new Size(x, y);
        Assert.Equal(x, result.X);
        Assert.Equal(y, result.Y);
    }

    [Fact]
    public void TypeConverter_Get_ReturnsExpected()
    {
        Assert.IsType<SizeConverter>(TypeDescriptor.GetConverter(typeof(Size)));
    }

    [Fact]
    public void ValueSerializer_Get_ReturnsExpected()
    {
        Assert.IsType<SizeValueSerializer>(ValueSerializer.GetSerializerFor(typeof(Size)));
    }
}
