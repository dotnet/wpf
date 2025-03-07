// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel;
using System.Globalization;
using System.Windows.Converters;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Tests;

namespace System.Windows.Tests;

public class PointTests
{
    [Fact]
    public void Ctor_Default()
    {
        var point = new Point();
        Assert.Equal(0, point.X);
        Assert.Equal(0, point.Y);
    }

    [Theory]
    [InlineData(double.NegativeInfinity, double.NegativeInfinity)]
    [InlineData(double.MinValue, double.MinValue)]
    [InlineData(-1, -2)]
    [InlineData(0, 0)]
    [InlineData(1, 0)]
    [InlineData(0, 2)]
    [InlineData(0.1, 0.2)]
    [InlineData(double.NegativeZero, double.NegativeZero)]
    [InlineData(1, 2)]
    [InlineData(double.MaxValue, double.MaxValue)]
    [InlineData(double.PositiveInfinity, double.PositiveInfinity)]
    [InlineData(double.NaN, double.NaN)]
    [InlineData(double.NaN, 1)]
    [InlineData(1, double.NaN)]
    public void Ctor_Double_Double(double x, double y)
    {
        var point = new Point(x, y);
        Assert.Equal(x, point.X);
        Assert.Equal(y, point.Y);
    }

    [Theory]
    [InlineData(double.NegativeInfinity)]
    [InlineData(double.MinValue)]
    [InlineData(-1)]
    [InlineData(double.NegativeZero)]
    [InlineData(0)]
    [InlineData(0.2)]
    [InlineData(1)]
    [InlineData(double.MaxValue)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NaN)]
    public void X_Set_GetReturnsExpected(double value)
    {
        var point = new Point
        {
            // Set.
            X = value
        };
        Assert.Equal(value, point.X);

        // Set same.
        point.X = value;
        Assert.Equal(value, point.X);
    }

    [Theory]
    [InlineData(double.NegativeInfinity)]
    [InlineData(double.MinValue)]
    [InlineData(-1)]
    [InlineData(double.NegativeZero)]
    [InlineData(0)]
    [InlineData(0.2)]
    [InlineData(1)]
    [InlineData(double.MaxValue)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NaN)]
    public void Y_Set_GetReturnsExpected(double value)
    {
        var point = new Point
        {
            // Set.
            Y = value
        };
        Assert.Equal(value, point.Y);

        // Set same.
        point.Y = value;
        Assert.Equal(value, point.Y);
    }

    public static IEnumerable<object[]> Add_TestData()
    {
        yield return new object[] { new Point(1, 2), 1, 2, new Point(2, 4) };
        yield return new object[] { new Point(1, 2), 0.1, 0.2, new Point(1.1, 2.2) };
        yield return new object[] { new Point(1.2, 2.3), 0.1, 0.2, new Point(1.3, 2.5) };
        yield return new object[] { new Point(1, 2), 0, 0, new Point(1, 2) };
        yield return new object[] { new Point(1, 2), -1, -2, new Point(0, 0) };

        yield return new object[] { new Point(1.2, 2.3), double.NegativeInfinity, double.NegativeInfinity, new Point(double.NegativeInfinity, double.NegativeInfinity) };
        yield return new object[] { new Point(1.2, 2.3), double.NegativeInfinity, 0.2, new Point(double.NegativeInfinity, 2.5) };
        yield return new object[] { new Point(1.2, 2.3), 0.1, double.NegativeInfinity, new Point(1.3, double.NegativeInfinity) };

        yield return new object[] { new Point(-1.2, -2.3), double.MinValue, double.MinValue, new Point(double.MinValue, double.MinValue) };
        yield return new object[] { new Point(-1.2, -2.3), double.MinValue, 0.2, new Point(double.MinValue, -2.1) };
        yield return new object[] { new Point(-1.2, -2.3), 0.1, double.MinValue, new Point(-1.1, double.MinValue) };

        yield return new object[] { new Point(1.2, 2.3), double.MaxValue, double.MaxValue, new Point(double.MaxValue, double.MaxValue) };
        yield return new object[] { new Point(1.2, 2.3), double.MaxValue, 0.2, new Point(double.MaxValue, 2.5) };
        yield return new object[] { new Point(1.2, 2.3), 0.1, double.MaxValue, new Point(1.3, double.MaxValue) };

        yield return new object[] { new Point(1.2, 2.3), double.PositiveInfinity, double.PositiveInfinity, new Point(double.PositiveInfinity, double.PositiveInfinity) };
        yield return new object[] { new Point(1.2, 2.3), double.PositiveInfinity, 0.2, new Point(double.PositiveInfinity, 2.5) };
        yield return new object[] { new Point(1.2, 2.3), 0.1, double.PositiveInfinity, new Point(1.3, double.PositiveInfinity) };

        yield return new object[] { new Point(1.2, 2.3), double.NaN, double.NaN, new Point(double.NaN, double.NaN) };
        yield return new object[] { new Point(1.2, 2.3), double.NaN, 0.2, new Point(double.NaN, 2.5) };
        yield return new object[] { new Point(1.2, 2.3), 0.1, double.NaN, new Point(1.3, double.NaN) };

        yield return new object[] { new Point(), 1, 2, new Point(1, 2) };
        yield return new object[] { new Point(), 0, 0, new Point() };
        yield return new object[] { new Point(), -1, -2, new Point(-1, -2) };
    }

    [Theory]
    [MemberData(nameof(Add_TestData))]
    public void Add_InvokeVector_ReturnsExpected(Point point, double x, double y, Point expected)
    {
        Vector vector = new Vector(x, y);
        Point result = Point.Add(point, vector);
        Assert.Equal(expected.X, result.X, precision: 5);
        Assert.Equal(expected.Y, result.Y, precision: 5);
    }

    [Theory]
    [MemberData(nameof(Add_TestData))]
    public void OperatorAdd_InvokeVector_ReturnsExpected(Point point, double x, double y, Point expected)
    {
        Vector vector = new Vector(x, y);
        Point result = point + vector;
        Assert.Equal(expected.X, result.X, precision: 5);
        Assert.Equal(expected.Y, result.Y, precision: 5);
    }

    public static IEnumerable<object?[]> Equals_TestData()
    {
        // Normal size.
        yield return new object?[] { new Point(1, 2), new Point(1, 2), true };
        yield return new object?[] { new Point(1, 2), new Point(2, 2), false };
        yield return new object?[] { new Point(1, 2), new Point(1, 3), false };
        yield return new object?[] { new Point(1, 2), new Point(double.NaN, 2), false };
        yield return new object?[] { new Point(1, 2), new Point(1, double.NaN), false };
        yield return new object?[] { new Point(1, 2), new Point(double.NaN, double.NaN), false };
        yield return new object?[] { new Point(1, 2), new Point(0, 0), false };
        yield return new object?[] { new Point(1, 2), new Point(), false };

        // NaN x.
        yield return new object[] { new Point(double.NaN, 2), new Point(double.NaN, 2), true };
        yield return new object[] { new Point(double.NaN, 2), new Point(2, 2), false };
        yield return new object[] { new Point(double.NaN, 2), new Point(double.NaN, double.NaN), false };

        // NaN y.
        yield return new object[] { new Point(1, double.NaN), new Point(1, double.NaN), true };
        yield return new object[] { new Point(1, double.NaN), new Point(1, 2), false };
        yield return new object[] { new Point(1, double.NaN), new Point(double.NaN, double.NaN), false };

        // NaN x & y.
        yield return new object[] { new Point(double.NaN, double.NaN), new Point(double.NaN, double.NaN), true };
        yield return new object[] { new Point(double.NaN, double.NaN), new Point(1, 2), false };
        yield return new object[] { new Point(double.NaN, double.NaN), new Point(double.NaN, 2), false };
        yield return new object[] { new Point(double.NaN, double.NaN), new Point(1, double.NaN), false };

        // Zero.
        yield return new object?[] { new Point(0, 0), new Point(), true };
        yield return new object?[] { new Point(0, 0), new Point(0, 0), true };
        yield return new object?[] { new Point(0, 0), new Point(1, 0), false };
        yield return new object?[] { new Point(0, 0), new Point(0, 1), false };

        // Default.
        yield return new object?[] { new Point(), new Point(), true };
        yield return new object?[] { new Point(), new Point(0, 0), true };
        yield return new object?[] { new Point(), new Point(1, 0), false };
        yield return new object?[] { new Point(), new Point(0, 1), false };
    }

    [Theory]
    [MemberData(nameof(Equals_TestData))]
    public void Equals_Object_ReturnsExpected(Point point, object o, bool expected)
    {
        Assert.Equal(expected, point.Equals(o));
        if (o is Point other)
        {
            Assert.Equal(expected, point.Equals(other));
            Assert.Equal(expected, other.Equals(point));
            Assert.Equal(expected, Point.Equals(point, other));
            Assert.Equal(expected, Point.Equals(other, point));
            Assert.Equal(expected, point.GetHashCode().Equals(other.GetHashCode()));
        }
    }

    public static IEnumerable<object[]> EqualityOperator_TestData()
    {
        // Normal size.
        yield return new object[] { new Point(1, 2), new Point(1, 2), true };
        yield return new object[] { new Point(1, 2), new Point(2, 2), false };
        yield return new object[] { new Point(1, 2), new Point(1, 3), false };
        yield return new object[] { new Point(1, 2), new Point(double.NaN, 2), false };
        yield return new object[] { new Point(1, 2), new Point(1, double.NaN), false };
        yield return new object[] { new Point(1, 2), new Point(double.NaN, double.NaN), false };
        yield return new object[] { new Point(1, 2), new Point(0, 0), false };
        yield return new object[] { new Point(1, 2), new Point(), false };

        // NaN x.
        yield return new object[] { new Point(double.NaN, 2), new Point(double.NaN, 2), false };
        yield return new object[] { new Point(double.NaN, 2), new Point(2, 2), false };
        yield return new object[] { new Point(double.NaN, 2), new Point(double.NaN, double.NaN), false };

        // NaN y.
        yield return new object[] { new Point(1, double.NaN), new Point(1, double.NaN), false };
        yield return new object[] { new Point(1, double.NaN), new Point(1, 2), false };
        yield return new object[] { new Point(1, double.NaN), new Point(double.NaN, double.NaN), false };

        // NaN x & y.
        yield return new object[] { new Point(double.NaN, double.NaN), new Point(double.NaN, double.NaN), false };
        yield return new object[] { new Point(double.NaN, double.NaN), new Point(1, 2), false };
        yield return new object[] { new Point(double.NaN, double.NaN), new Point(double.NaN, 2), false };
        yield return new object[] { new Point(double.NaN, double.NaN), new Point(1, double.NaN), false };

        // Zero.
        yield return new object[] { new Point(0, 0), new Point(), true };
        yield return new object[] { new Point(0, 0), new Point(0, 0), true };
        yield return new object[] { new Point(0, 0), new Point(1, 0), false };
        yield return new object[] { new Point(0, 0), new Point(0, 1), false };

        // Default.
        yield return new object[] { new Point(), new Point(), true };
        yield return new object[] { new Point(), new Point(0, 0), true };
        yield return new object[] { new Point(), new Point(1, 0), false };
        yield return new object[] { new Point(), new Point(0, 1), false };
    }

    [Theory]
    [MemberData(nameof(EqualityOperator_TestData))]
    public void EqualityOperator_Invoke_ReturnsExpected(Point point1, Point point2, bool expected)
    {
        Assert.Equal(expected, point1 == point2);
        Assert.Equal(expected, point2 == point1);
        Assert.Equal(!expected, point1 != point2);
        Assert.Equal(!expected, point2 != point1);
    }

    [Fact]
    public void GetHashCode_InvokeDefault_ReturnsEqual()
    {
        var point = new Point(0, 0);
        Assert.Equal(0, point.GetHashCode());
        Assert.Equal(point.GetHashCode(), point.GetHashCode());
    }

    [Fact]
    public void GetHashCode_InvokeNormal_ReturnsEqual()
    {
        var point = new Point(1, 2);
        Assert.NotEqual(0, point.GetHashCode());
        Assert.Equal(point.GetHashCode(), point.GetHashCode());
    }

    [Theory]
    [MemberData(nameof(MatrixTests.Transform_Point_TestData), MemberType = typeof(MatrixTests))]
    public void Multiply_InvokeMatrix_ReturnsExpected(Matrix matrix, Point point, Point expected)
    {
        Point result = Point.Multiply(point, matrix);
        Assert.Equal(expected.X, result.X, precision: 5);
        Assert.Equal(expected.Y, result.Y, precision: 5);
    }

    [Theory]
    [MemberData(nameof(MatrixTests.Transform_Point_TestData), MemberType = typeof(MatrixTests))]
    public void OperatorMultiply_InvokeMatrix_ReturnsExpected(Matrix matrix, Point point, Point expected)
    {
        Point result = point * matrix;
        Assert.Equal(expected.X, result.X, precision: 5);
        Assert.Equal(expected.Y, result.Y, precision: 5);
    }

    [Theory]
    [MemberData(nameof(Add_TestData))]
    public void Offset_Invoke_Success(Point point, double x, double y, Point expected)
    {
        point.Offset(x, y);
        Assert.Equal(expected.X, point.X, precision: 5);
        Assert.Equal(expected.Y, point.Y, precision: 5);
    }

    public static IEnumerable<object[]> Subtract_TestData()
    {
        yield return new object[] { new Point(1, 2), 1, 2, new Point(0, 0) };
        yield return new object[] { new Point(1, 2), 0.1, 0.2, new Point(0.9, 1.8) };
        yield return new object[] { new Point(1, 2), 0, 0, new Point(1, 2) };
        yield return new object[] { new Point(1, 2), -1, -2, new Point(2, 4) };

        yield return new object[] { new Point(-1, -2), 1, 2, new Point(-2, -4) };
        yield return new object[] { new Point(-1, -2), 0.1, 0.2, new Point(-1.1, -2.2) };
        yield return new object[] { new Point(-1, -2), 0, 0, new Point(-1, -2) };
        yield return new object[] { new Point(-1, -2), -1, -2, new Point(0, 0) };

        yield return new object[] { new Point(), 1, 2, new Point(-1, -2) };
        yield return new object[] { new Point(), 0, 0, new Point() };
        yield return new object[] { new Point(), -1, -2, new Point(1, 2) };
    }

    [Theory]
    [MemberData(nameof(Subtract_TestData))]
    public void Subtract_InvokePoint_ReturnsExpected(Point point, double x, double y, Point expected)
    {
        var other = new Point(x, y);
        Vector result = Point.Subtract(point, other);
        Assert.Equal(expected.X, result.X, precision: 5);
        Assert.Equal(expected.Y, result.Y, precision: 5);
        Assert.Equal(new Vector(-result.X, -result.Y), Point.Subtract(other, point));
    }

    [Theory]
    [MemberData(nameof(Subtract_TestData))]
    public void OperatorSubtract_InvokePoint_ReturnsExpected(Point point, double x, double y, Point expected)
    {
        var other = new Point(x, y);
        Vector result = point - other;
        Assert.Equal(expected.X, result.X, precision: 5);
        Assert.Equal(expected.Y, result.Y, precision: 5);
        Assert.Equal(new Vector(-result.X, -result.Y), other - point);
    }

    [Theory]
    [MemberData(nameof(Subtract_TestData))]
    public void Subtract_InvokeVector_ReturnsExpected(Point point, double x, double y, Point expected)
    {
        Vector vector = new Vector(x, y);
        Point result = Point.Subtract(point, vector);
        Assert.Equal(expected.X, result.X, precision: 5);
        Assert.Equal(expected.Y, result.Y, precision: 5);
    }

    [Theory]
    [MemberData(nameof(Subtract_TestData))]
    public void OperatorSubtract_InvokeVector_ReturnsExpected(Point point, double x, double y, Point expected)
    {
        Vector vector = new Vector(x, y);
        Point result = point - vector;
        Assert.Equal(expected.X, result.X, precision: 5);
        Assert.Equal(expected.Y, result.Y, precision: 5);
    }

    public static IEnumerable<object[]> Parse_TestData()
    {
        yield return new object[] { "0,0", new Point(0, 0) };
        yield return new object[] { "1,2", new Point(1, 2) };
        yield return new object[] { "1.1,2.2", new Point(1.1, 2.2) };
        yield return new object[] { "   1   ,   2  ", new Point(1, 2) };
        yield return new object[] { "-1,-2", new Point(-1, -2) };
    }

    [Theory]
    [MemberData(nameof(Parse_TestData))]
    public void Parse_Invoke_Success(string source, Point expected)
    {
        Point result = Point.Parse(source);
        Assert.Equal(expected.X, result.X, precision: 5);
        Assert.Equal(expected.Y, result.Y, precision: 5);
    }

    [Fact]
    public void Parse_NullSource_ThrowsInvalidOperationException()
    {
        Assert.Throws<InvalidOperationException>(() => Point.Parse(null));
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
        Assert.Throws<InvalidOperationException>(() => Point.Parse(source));
    }

    public static IEnumerable<object[]> Parse_NotDouble_TestData()
    {
        yield return new object[] { "Empty" };
        yield return new object[] { " Empty " };
        yield return new object[] { "Empty,2" };
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
        Assert.Throws<FormatException>(() => Point.Parse(source));
    }

    public static IEnumerable<object[]> ToString_TestData()
    {
        yield return new object[] { new Point(), "0,0" };
        yield return new object[] { new Point(0, 0), "0,0" };
        yield return new object[] { new Point(1, 2), "1,2" };
        yield return new object[] { new Point(1.1, 2.2), "1.1,2.2" };
        yield return new object[] { new Point(-1, -2), "-1,-2" };
    }

    [Theory]
    [MemberData(nameof(ToString_TestData))]
    public void ToString_Invoke_ReturnsExpected(Point point, string expected)
    {
        Assert.Equal(expected, point.ToString());
    }

    [Theory]
    [MemberData(nameof(ToString_TestData))]
    public void ToString_InvokeIFormatProviderInvariantCulture_ReturnsExpected(Point point, string expected)
    {
        Assert.Equal(expected, point.ToString(CultureInfo.InvariantCulture));
    }

    public static IEnumerable<object[]> ToString_IFormatProviderCustom_TestData()
    {
        yield return new object[] { new Point(), "|", "0,0" };
        yield return new object[] { new Point(), "|_", "0,0" };
        yield return new object[] { new Point(), ",_", "0;0" };
        yield return new object[] { new Point(), ",", "0;0" };
        yield return new object[] { new Point(), ";", "0,0" };
        yield return new object[] { new Point(), " ", "0,0" };

        yield return new object[] { new Point(0, 0), "|", "0,0" };
        yield return new object[] { new Point(0, 0), "|_", "0,0" };
        yield return new object[] { new Point(0, 0), ",_", "0;0" };
        yield return new object[] { new Point(0, 0), ",", "0;0" };
        yield return new object[] { new Point(0, 0), ";", "0,0" };
        yield return new object[] { new Point(0, 0), " ", "0,0" };

        yield return new object[] { new Point(1, 2), "|", "1,2" };
        yield return new object[] { new Point(1, 2), "|_", "1,2" };
        yield return new object[] { new Point(1, 2), ",_", "1;2" };
        yield return new object[] { new Point(1, 2), ",", "1;2" };
        yield return new object[] { new Point(1, 2), ";", "1,2" };
        yield return new object[] { new Point(1, 2), " ", "1,2" };
        
        yield return new object[] { new Point(1.1, 2.2), "|", "1|1,2|2" };
        yield return new object[] { new Point(1.1, 2.2), "|_", "1|_1,2|_2" };
        yield return new object[] { new Point(1.1, 2.2), ",_", "1,_1;2,_2" };
        yield return new object[] { new Point(1.1, 2.2), ",", "1,1;2,2" };
        yield return new object[] { new Point(1.1, 2.2), ";", "1;1,2;2" };
        yield return new object[] { new Point(1.1, 2.2), " ", "1 1,2 2" };

        yield return new object[] { new Point(-1, -2), "|", "-1,-2" };
        yield return new object[] { new Point(-1, -2), "|_", "-1,-2" };
        yield return new object[] { new Point(-1, -2), ",_", "-1;-2" };
        yield return new object[] { new Point(-1, -2), ",", "-1;-2" };
        yield return new object[] { new Point(-1, -2), ";", "-1,-2" };
        yield return new object[] { new Point(-1, -2), " ", "-1,-2" };
    }

    [Theory]
    [MemberData(nameof(ToString_IFormatProviderCustom_TestData))]
    public void ToString_InvokeIFormatProviderCustom_ReturnsExpected(Point point, string numberDecimalSeparator, string expected)
    {
        var culture = new CultureInfo("en-US");
        NumberFormatInfo formatInfo = culture.NumberFormat;
        formatInfo.NumberDecimalSeparator = numberDecimalSeparator;

        Assert.Equal(expected, point.ToString(formatInfo));
    }

    [Theory]
    [MemberData(nameof(ToString_TestData))]
    public void ToString_InvokeIFormattableInvariantCulture_ReturnsExpected(Point point, string expected)
    {
        IFormattable formattable = point;

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
        var point = new Point(1.23456, 2.34567);
        IFormattable formattable = point;

        var culture = new CultureInfo("en-US");
        NumberFormatInfo formatInfo = culture.NumberFormat;
        formatInfo.NumberDecimalSeparator = numberDecimalSeparator;

        Assert.Equal(expected, formattable.ToString("F2", formatInfo));
    }

    [Fact]
    public void OperatorConvertVector_InvokeEmpty_ReturnsExpected()
    {
        Vector result = (Vector)new Point();
        Assert.Equal(0, result.X);
        Assert.Equal(0, result.Y);
    }

    [Theory]
    [InlineData(1, 2)]
    [InlineData(1.1, 2.2)]
    [InlineData(-1, -2)]
    public void OperatorConvertVector_InvokeNormal_ReturnsExpected(double x, double y)
    {
        Vector result = (Vector)new Point(x, y);
        Assert.Equal(x, result.X);
        Assert.Equal(y, result.Y);
    }

    [Fact]
    public void OperatorConvertSize_InvokeEmpty_ReturnsExpected()
    {
        Size result = (Size)new Point();
        Assert.Equal(0, result.Width);
        Assert.Equal(0, result.Height);
    }

    [Theory]
    [InlineData(1, 2, 1, 2)]
    [InlineData(1.1, 2.2, 1.1, 2.2)]
    [InlineData(-1, -2, 1, 2)]
    public void OperatorConvertSize_InvokeNormal_ReturnsExpected(double x, double y, double expectedWidth, double expectedHeight)
    {
        Size result = (Size)new Point(x, y);
        Assert.Equal(expectedWidth, result.Width);
        Assert.Equal(expectedHeight, result.Height);
    }

    [Fact]
    public void TypeConverter_Get_ReturnsExpected()
    {
        Assert.IsType<PointConverter>(TypeDescriptor.GetConverter(typeof(Point)));
    }
    
    [Fact]
    public void ValueSerializer_Get_ReturnsExpected()
    {
        Assert.IsType<PointValueSerializer>(ValueSerializer.GetSerializerFor(typeof(Point)));
    }
}
