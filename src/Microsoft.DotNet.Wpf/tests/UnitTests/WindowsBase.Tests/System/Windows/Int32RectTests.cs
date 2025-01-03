// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel;
using System.Globalization;
using System.Windows.Converters;
using System.Windows.Markup;

namespace System.Windows.Tests;

public class Int32RectTests
{
    [Fact]
    public void Ctor_Default()
    {
        var rect = new Int32Rect();
        Assert.Equal(0, rect.X);
        Assert.Equal(0, rect.Y);
        Assert.Equal(0, rect.Width);
        Assert.Equal(0, rect.Height);
        Assert.True(rect.IsEmpty);
        Assert.False(rect.HasArea);
    }

    [Theory]
    [InlineData(-1, -2, -3, -4, false, false)]
    [InlineData(0, 0, 0, 0, true, false)]
    [InlineData(1, 0, 0, 0, false, false)]
    [InlineData(-1, 0, 0, 0, false, false)]
    [InlineData(0, 1, 0, 0, false, false)]
    [InlineData(0, -1, 0, 0, false, false)]
    [InlineData(0, 0, 1, 0, false, false)]
    [InlineData(0, 0, -1, 0, false, false)]
    [InlineData(0, 0, 0, 1, false, false)]
    [InlineData(0, 0, 0, -1, false, false)]
    [InlineData(1, 2, -1, 4, false, false)]
    [InlineData(1, 2, 0, 4, false, false)]
    [InlineData(1, 2, 3, -1, false, false)]
    [InlineData(1, 2, 3, 0, false, false)]
    [InlineData(1, 2, 3, 4, false, true)]
    [InlineData(0, 0, 3, 4, false, true)]
    public void Ctor_Int_Int_Int_Int(int x, int y, int width, int height, bool expectedIsEmpty, bool expectedHasArea)
    {
        var rect = new Int32Rect(x, y, width, height);
        Assert.Equal(x, rect.X);
        Assert.Equal(y, rect.Y);
        Assert.Equal(width, rect.Width);
        Assert.Equal(height, rect.Height);
        Assert.Equal(expectedIsEmpty, rect.IsEmpty);
        Assert.Equal(expectedHasArea, rect.HasArea);
    }

    [Fact]
    public void Empty_Get_ReturnsExpected()
    {
        Int32Rect rect = Int32Rect.Empty;
        Assert.Equal(0, rect.X);
        Assert.Equal(0, rect.Y);
        Assert.Equal(0, rect.Width);
        Assert.Equal(0, rect.Height);
        Assert.True(rect.IsEmpty);
        Assert.False(rect.HasArea);
        Assert.Equal(rect, Int32Rect.Empty);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4   )]
    public void X_Set_GetReturnsExpected(int value)
    {
        var rect = new Int32Rect(1, 2, 3, 4)
        {
            // Set.
            X = value
        };
        Assert.Equal(value, rect.X);
        Assert.Equal(2, rect.Y);
        Assert.Equal(3, rect.Width);
        Assert.Equal(4, rect.Height);
        Assert.False(rect.IsEmpty);
        Assert.True(rect.HasArea);

        // Set again.
        rect.X = value;
        Assert.Equal(value, rect.X);
        Assert.Equal(2, rect.Y);
        Assert.Equal(3, rect.Width);
        Assert.Equal(4, rect.Height);
        Assert.False(rect.IsEmpty);
        Assert.True(rect.HasArea);
    }


    [Theory]
    [InlineData(-1)]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4   )]
    public void Y_Set_GetReturnsExpected(int value)
    {
        var rect = new Int32Rect(1, 2, 3, 4)
        {
            // Set.
            Y = value
        };
        Assert.Equal(1, rect.X);
        Assert.Equal(value, rect.Y);
        Assert.Equal(3, rect.Width);
        Assert.Equal(4, rect.Height);
        Assert.False(rect.IsEmpty);
        Assert.True(rect.HasArea);

        // Set again.
        rect.Y = value;
        Assert.Equal(1, rect.X);
        Assert.Equal(value, rect.Y);
        Assert.Equal(3, rect.Width);
        Assert.Equal(4, rect.Height);
        Assert.False(rect.IsEmpty);
        Assert.True(rect.HasArea);
    }


    [Theory]
    [InlineData(-1, false)]
    [InlineData(0, false)]
    [InlineData(1, true)]
    [InlineData(2, true)]
    [InlineData(3, true)]
    [InlineData(4, true)]
    public void Width_Set_GetReturnsExpected(int value, bool expectedHasArea)
    {
        var rect = new Int32Rect(1, 2, 3, 4)
        {
            // Set.
            Width = value
        };
        Assert.Equal(1, rect.X);
        Assert.Equal(2, rect.Y);
        Assert.Equal(value, rect.Width);
        Assert.Equal(4, rect.Height);
        Assert.False(rect.IsEmpty);
        Assert.Equal(expectedHasArea, rect.HasArea);

        // Set again.
        rect.Width = value;
        Assert.Equal(1, rect.X);
        Assert.Equal(2, rect.Y);
        Assert.Equal(value, rect.Width);
        Assert.Equal(4, rect.Height);
        Assert.False(rect.IsEmpty);
        Assert.Equal(expectedHasArea, rect.HasArea);
    }

    [Theory]
    [InlineData(-1, false)]
    [InlineData(0, false)]
    [InlineData(1, true)]
    [InlineData(2, true)]
    [InlineData(3, true)]
    [InlineData(4, true)]
    public void Height_Set_GetReturnsExpected(int value, bool expectedHasArea)
    {
        var rect = new Int32Rect(1, 2, 3, 4)
        {
            // Set.
            Height = value
        };
        Assert.Equal(1, rect.X);
        Assert.Equal(2, rect.Y);
        Assert.Equal(3, rect.Width);
        Assert.Equal(value, rect.Height);
        Assert.False(rect.IsEmpty);
        Assert.Equal(expectedHasArea, rect.HasArea);

        // Set again.
        rect.Height = value;
        Assert.Equal(1, rect.X);
        Assert.Equal(2, rect.Y);
        Assert.Equal(3, rect.Width);
        Assert.Equal(value, rect.Height);
        Assert.False(rect.IsEmpty);
        Assert.Equal(expectedHasArea, rect.HasArea);
    }

    public static IEnumerable<object?[]> Equals_TestData()
    {
        // Zero.
        yield return new object?[] { new Int32Rect(1, 2, 3, 4), new Int32Rect(1, 2, 3, 4), true };
        yield return new object?[] { new Int32Rect(1, 2, 3, 4), new Int32Rect(2, 2, 3, 4), false };
        yield return new object?[] { new Int32Rect(1, 2, 3, 4), new Int32Rect(1, 3, 3, 4), false };
        yield return new object?[] { new Int32Rect(1, 2, 3, 4), new Int32Rect(1, 2, 4, 4), false };
        yield return new object?[] { new Int32Rect(1, 2, 3, 4), new Int32Rect(1, 2, 3, 5), false };
        yield return new object?[] { new Int32Rect(1, 2, 3, 4), new Int32Rect(0, 0, 0, 0), false };
        yield return new object?[] { new Int32Rect(1, 2, 3, 4), new Int32Rect(), false };
        yield return new object?[] { new Int32Rect(1, 2, 3, 4), Int32Rect.Empty, false };
        yield return new object?[] { new Int32Rect(1, 2, 3, 4), new object(), false };
        yield return new object?[] { new Int32Rect(1, 2, 3, 4), null, false };

        // Zero.
        yield return new object?[] { new Int32Rect(0, 0, 0, 0), Int32Rect.Empty, true };
        yield return new object?[] { new Int32Rect(0, 0, 0, 0), new Int32Rect(), true };
        yield return new object?[] { new Int32Rect(0, 0, 0, 0), new Int32Rect(0, 0, 0, 0), true };
        yield return new object?[] { new Int32Rect(0, 0, 0, 0), new Int32Rect(1, 0, 0, 0), false };
        yield return new object?[] { new Int32Rect(0, 0, 0, 0), new Int32Rect(0, 2, 0, 0), false };
        yield return new object?[] { new Int32Rect(0, 0, 0, 0), new Int32Rect(0, 0, 3, 0), false };
        yield return new object?[] { new Int32Rect(0, 0, 0, 0), new Int32Rect(0, 0, 0, 4), false };
        yield return new object?[] { new Int32Rect(0, 0, 0, 0), new Int32Rect(1, 2, 3, 4), false };
        yield return new object?[] { new Int32Rect(0, 0, 0, 0), new object(), false };
        yield return new object?[] { new Int32Rect(0, 0, 0, 0), null, false };

        // Default.
        yield return new object?[] { new Int32Rect(), Int32Rect.Empty, true };
        yield return new object?[] { new Int32Rect(), new Int32Rect(), true };
        yield return new object?[] { new Int32Rect(), new Int32Rect(0, 0, 0, 0), true };
        yield return new object?[] { new Int32Rect(), new Int32Rect(1, 0, 0, 0), false };
        yield return new object?[] { new Int32Rect(), new Int32Rect(0, 2, 0, 0), false };
        yield return new object?[] { new Int32Rect(), new Int32Rect(0, 0, 3, 0), false };
        yield return new object?[] { new Int32Rect(), new Int32Rect(0, 0, 0, 4), false };
        yield return new object?[] { new Int32Rect(), new Int32Rect(1, 2, 3, 4), false };
        yield return new object?[] { new Int32Rect(), new object(), false };
        yield return new object?[] { new Int32Rect(), null, false };

        // Empty.
        yield return new object?[] { Int32Rect.Empty, Int32Rect.Empty, true };
        yield return new object?[] { Int32Rect.Empty, new Int32Rect(), true };
        yield return new object?[] { Int32Rect.Empty, new Int32Rect(0, 0, 0, 0), true };
        yield return new object?[] { Int32Rect.Empty, new Int32Rect(1, 0, 0, 0), false };
        yield return new object?[] { Int32Rect.Empty, new Int32Rect(0, 2, 0, 0), false };
        yield return new object?[] { Int32Rect.Empty, new Int32Rect(0, 0, 3, 0), false };
        yield return new object?[] { Int32Rect.Empty, new Int32Rect(0, 0, 0, 4), false };
        yield return new object?[] { Int32Rect.Empty, new Int32Rect(1, 2, 3, 4), false };
        yield return new object?[] { Int32Rect.Empty, new object(), false };
        yield return new object?[] { Int32Rect.Empty, null, false };
    }

    [Theory]
    [MemberData(nameof(Equals_TestData))]
    public void Equals_Invoke_ReturnsExpected(Int32Rect rect, object o, bool expected)
    {
        Assert.Equal(expected, rect.Equals(o));
        if (o is Int32Rect value)
        {
            Assert.Equal(expected, rect.Equals(value));
            Assert.Equal(expected, value.Equals(rect));
            Assert.Equal(expected, rect == value);
            Assert.Equal(expected, value == rect);
            Assert.Equal(!expected, rect != value);
            Assert.Equal(!expected, value != rect);
            Assert.Equal(expected, rect.GetHashCode().Equals(value.GetHashCode()));
        }
    }

    [Fact]
    public void GetHashCode_InvokeDefault_ReturnsEqual()
    {
        var rect = new Int32Rect();
        Assert.Equal(0, rect.GetHashCode());
        Assert.Equal(rect.GetHashCode(), rect.GetHashCode());
    }

    [Fact]
    public void GetHashCode_InvokeEmpty_ReturnsEqual()
    {
        Int32Rect rect = Int32Rect.Empty;
        Assert.Equal(0, rect.GetHashCode());
        Assert.Equal(rect.GetHashCode(), rect.GetHashCode());
    }

    [Fact]
    public void GetHashCode_InvokeNormal_ReturnsEqual()
    {
        var rect = new Int32Rect(1, 2, 3, 4);
        Assert.NotEqual(0, rect.GetHashCode());
        Assert.Equal(rect.GetHashCode(), rect.GetHashCode());
    }

    public static IEnumerable<object[]> Parse_TestData()
    {
        yield return new object[] { "Empty", Int32Rect.Empty };
        yield return new object[] { "  Empty  ", Int32Rect.Empty };
        yield return new object[] { "Empty", new Int32Rect() };
        yield return new object[] { "1,2,3,4", new Int32Rect(1, 2, 3, 4) };
        yield return new object[] { "   1   ,   2  ,  3,  4", new Int32Rect(1, 2, 3, 4) };
        yield return new object[] { "-1,-2,-3,-4", new Int32Rect(-1, -2, -3, -4) };
    }

    [Theory]
    [MemberData(nameof(Parse_TestData))]
    public void Parse_Invoke_Success(string source, Int32Rect expected)
    {
        Int32Rect result = Int32Rect.Parse(source);
        Assert.Equal(expected.X, result.X);
        Assert.Equal(expected.Y, result.Y);
        Assert.Equal(expected.Width, result.Width);
        Assert.Equal(expected.Height, result.Height);
    }

    [Fact]
    public void Parse_NullSource_ThrowsInvalidOperationException()
    {
        Assert.Throws<InvalidOperationException>(() => Int32Rect.Parse(null));
    }

    public static IEnumerable<object[]> Parse_InvalidSource_TestData()
    {
        yield return new object[] { "" };
        yield return new object[] { "  " };
        yield return new object[] { "," };
        yield return new object[] { "1" };
        yield return new object[] { "1," };
        yield return new object[] { "1,2" };
        yield return new object[] { "1,2," };
        yield return new object[] { "1,2,3" };
        yield return new object[] { "1,2,3," };
        yield return new object[] { "1,2,3,4," };
        yield return new object[] { "1,2,3,4,5" };
        yield return new object[] { "1,2,4,5,test" };
        yield return new object[] { "Empty," };
        yield return new object[] { "Empty,2,3,4" };
        yield return new object[] { "Identity," };
    }

    [Theory]
    [MemberData(nameof(Parse_InvalidSource_TestData))]
    public void Parse_InvalidSource_ThrowsInvalidOperationException(string source)
    {
        Assert.Throws<InvalidOperationException>(() => Int32Rect.Parse(source));
    }

    public static IEnumerable<object[]> Parse_NotInt32_TestData()
    {
        yield return new object[] { "Identity" };
        yield return new object[] { " Identity " };
        yield return new object[] { "Identity,2,3,4" };
        yield return new object[] { "test" };
        yield return new object[] { "test,2,3,4" };
        yield return new object[] { "1,test,3,4" };
        yield return new object[] { "1,2,test,4" };
        yield return new object[] { "1,2,3,test" };
        yield return new object[] { "1;2;3;4" };
        yield return new object[] { """1"",""2"",""3"",""4""" };
        yield return new object[] { "1.1,2,3,4" };
    }

    [Theory]
    [MemberData(nameof(Parse_NotInt32_TestData))]
    public void Parse_NotInt_ThrowsFormatException(string source)
    {
        Assert.Throws<FormatException>(() => Int32Rect.Parse(source));
    }

    public static IEnumerable<object[]> ToString_TestData()
    {
        yield return new object[] { Int32Rect.Empty, "Empty" };
        yield return new object[] { new Int32Rect(), "Empty" };
        yield return new object[] { new Int32Rect(0, 0, 0, 0), "Empty" };
        yield return new object[] { new Int32Rect(1, 2, 3, 4), "1,2,3,4" };
        yield return new object[] { new Int32Rect(-1, -2, 3, 4), "-1,-2,3,4" };
    }

    [Theory]
    [MemberData(nameof(ToString_TestData))]
    public void ToString_Invoke_ReturnsExpected(Int32Rect rect, string expected)
    {
        Assert.Equal(expected, rect.ToString());
    }

    [Theory]
    [MemberData(nameof(ToString_TestData))]
    public void ToString_InvokeIFormatProviderInvariantCulture_ReturnsExpected(Int32Rect rect, string expected)
    {
        Assert.Equal(expected, rect.ToString(CultureInfo.InvariantCulture));
    }

    public static IEnumerable<object[]> ToString_IFormatProviderCustom_TestData()
    {
        yield return new object[] { new Int32Rect(), "|", "Empty" };
        yield return new object[] { new Int32Rect(), "|_", "Empty" };
        yield return new object[] { new Int32Rect(), ",_", "Empty" };
        yield return new object[] { new Int32Rect(), ",", "Empty" };
        yield return new object[] { new Int32Rect(), ";", "Empty" };
        yield return new object[] { new Int32Rect(), " ", "Empty" };

        yield return new object[] { new Int32Rect(0, 0, 0, 0), "|", "Empty" };
        yield return new object[] { new Int32Rect(0, 0, 0, 0), "|_", "Empty" };
        yield return new object[] { new Int32Rect(0, 0, 0, 0), ",_", "Empty" };
        yield return new object[] { new Int32Rect(0, 0, 0, 0), ",", "Empty" };
        yield return new object[] { new Int32Rect(0, 0, 0, 0), ";", "Empty" };
        yield return new object[] { new Int32Rect(0, 0, 0, 0), " ", "Empty" };

        yield return new object[] { new Int32Rect(1, 2, 3, 4), "|", "1,2,3,4" };
        yield return new object[] { new Int32Rect(1, 2, 3, 4), "|_", "1,2,3,4" };
        yield return new object[] { new Int32Rect(1, 2, 3, 4), ",_", "1;2;3;4" };
        yield return new object[] { new Int32Rect(1, 2, 3, 4), ",", "1;2;3;4" };
        yield return new object[] { new Int32Rect(1, 2, 3, 4), ";", "1,2,3,4" };
        yield return new object[] { new Int32Rect(1, 2, 3, 4), " ", "1,2,3,4" };

        yield return new object[] { new Int32Rect(-1, -2, -3, -4), "|", "-1,-2,-3,-4" };
        yield return new object[] { new Int32Rect(-1, -2, -3, -4), "|_", "-1,-2,-3,-4" };
        yield return new object[] { new Int32Rect(-1, -2, -3, -4), ",_", "-1;-2;-3;-4" };
        yield return new object[] { new Int32Rect(-1, -2, -3, -4), ",", "-1;-2;-3;-4" };
        yield return new object[] { new Int32Rect(-1, -2, -3, -4), ";", "-1,-2,-3,-4" };
        yield return new object[] { new Int32Rect(-1, -2, -3, -4), " ", "-1,-2,-3,-4" };
    }

    [Theory]
    [MemberData(nameof(ToString_IFormatProviderCustom_TestData))]
    public void ToString_InvokeIFormatProviderCustom_ReturnsExpected(Int32Rect rect, string numberDecimalSeparator, string expected)
    {
        var culture = new CultureInfo("en-US");
        NumberFormatInfo formatInfo = culture.NumberFormat;
        formatInfo.NumberDecimalSeparator = numberDecimalSeparator;

        Assert.Equal(expected, rect.ToString(formatInfo));
    }

    [Theory]
    [MemberData(nameof(ToString_TestData))]
    public void ToString_InvokeIFormattableInvariantCulture_ReturnsExpected(Int32Rect rect, string expected)
    {
        IFormattable formattable = rect;
        Assert.Equal(expected, formattable.ToString(null, null));
        Assert.Equal(expected, formattable.ToString(null, CultureInfo.InvariantCulture));
    }

    [Theory]
    [InlineData("|", "1|00,2|00,3|00,4|00")]
    [InlineData("|_", "1|_00,2|_00,3|_00,4|_00")]
    [InlineData(",_", "1,_00;2,_00;3,_00;4,_00")]
    [InlineData(",", "1,00;2,00;3,00;4,00")]
    [InlineData(";", "1;00,2;00,3;00,4;00")]
    [InlineData(" ", "1 00,2 00,3 00,4 00")]
    public void ToString_InvokeIFormattableCustomFormat_ReturnsExpected(string numberDecimalSeparator, string expected)
    {
        var rect = new Int32Rect(1, 2, 3, 4);
        IFormattable formattable = rect;

        var culture = new CultureInfo("en-US");
        NumberFormatInfo formatInfo = culture.NumberFormat;
        formatInfo.NumberDecimalSeparator = numberDecimalSeparator;
        Assert.Equal(expected, formattable.ToString("F2", formatInfo));
    }

    [Fact]
    public void TypeConverter_Get_ReturnsExpected()
    {
        Assert.IsType<Int32RectConverter>(TypeDescriptor.GetConverter(typeof(Int32Rect)));
    }

    [Fact]
    public void ValueSerializer_Get_ReturnsExpected()
    {
        Assert.IsType<Int32RectValueSerializer>(ValueSerializer.GetSerializerFor(typeof(Int32Rect)));
    }
}
