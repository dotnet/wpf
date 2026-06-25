// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel;
using System.Globalization;
using System.Windows.Converters;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Tests;

namespace System.Windows.Tests;

public class RectTests
{
    [Fact]
    public void Ctor_Default()
    {
        var rect = new Rect();
        Assert.Equal(0, rect.X);
        Assert.Equal(0, rect.Y);
        Assert.Equal(0, rect.Width);
        Assert.Equal(0, rect.Height);
        Assert.False(rect.IsEmpty);
    }

    public static IEnumerable<object[]> Ctor_Size_TestData()
    {
        yield return new object[] { new Size(1, 2) };
        yield return new object[] { new Size(1, 0) };
        yield return new object[] { new Size(0, 2) };
        yield return new object[] { new Size(0, 0) };
        yield return new object[] { new Size(double.NaN, double.NaN) };
        yield return new object[] { new Size(double.NaN, 2) };
        yield return new object[] { new Size(1, double.NaN) };
        yield return new object[] { new Size() };
    }

    [Theory]
    [MemberData(nameof(Ctor_Size_TestData))]
    public void Ctor_Size(Size size)
    {
        var rect = new Rect(size);
        Assert.Equal(0, rect.X);
        Assert.Equal(0, rect.Y);
        Assert.Equal(size.Width, rect.Width);
        Assert.Equal(size.Height, rect.Height);
        Assert.Equal(0, rect.Location.X);
        Assert.Equal(0, rect.Location.Y);
        Assert.Equal(size.Width, rect.Size.Width);
        Assert.Equal(size.Height, rect.Size.Height);
        Assert.Equal(0, rect.Top);
        Assert.Equal(0, rect.TopLeft.X);
        Assert.Equal(0, rect.TopLeft.Y);
        Assert.Equal(size.Width, rect.TopRight.X);
        Assert.Equal(0, rect.TopRight.Y);
        Assert.Equal(size.Height, rect.Bottom);
        Assert.Equal(0, rect.BottomLeft.X);
        Assert.Equal(size.Height, rect.BottomLeft.Y);
        Assert.Equal(size.Width, rect.BottomRight.X);
        Assert.Equal(size.Height, rect.BottomRight.Y);
        Assert.False(rect.IsEmpty);
    }

    [Fact]
    public void Ctor_SizeEmpty()
    {
        var rect = new Rect(Size.Empty);
        Assert.Equal(double.PositiveInfinity, rect.X);
        Assert.Equal(double.PositiveInfinity, rect.Y);
        Assert.Equal(double.NegativeInfinity, rect.Width);
        Assert.Equal(double.NegativeInfinity, rect.Height);
        Assert.Equal(double.PositiveInfinity, rect.Location.X);
        Assert.Equal(double.PositiveInfinity, rect.Location.Y);
        Assert.Equal(double.NegativeInfinity, rect.Size.Width);
        Assert.Equal(double.NegativeInfinity, rect.Size.Height);
        Assert.Equal(double.PositiveInfinity, rect.Top);
        Assert.Equal(double.PositiveInfinity, rect.TopLeft.X);
        Assert.Equal(double.PositiveInfinity, rect.TopLeft.Y);
        Assert.Equal(double.NegativeInfinity, rect.TopRight.X);
        Assert.Equal(double.PositiveInfinity, rect.TopRight.Y);
        Assert.Equal(double.NegativeInfinity, rect.Bottom);
        Assert.Equal(double.PositiveInfinity, rect.BottomLeft.X);
        Assert.Equal(double.NegativeInfinity, rect.BottomLeft.Y);
        Assert.Equal(double.NegativeInfinity, rect.BottomRight.X);
        Assert.Equal(double.NegativeInfinity, rect.BottomRight.Y);
        Assert.True(rect.IsEmpty);
    }

    [Theory]
    [InlineData(double.NegativeZero, double.NegativeZero, double.NegativeZero, double.NegativeZero)]
    [InlineData(double.NegativeInfinity, double.NegativeInfinity, 3, 4)]
    [InlineData(double.MinValue, double.MinValue, 3, 4)]
    [InlineData(0, 0, 0, 0)]
    [InlineData(0.1, 0.2, 0.3, 0.4)]
    [InlineData(-1, -2, 3, 4)]
    [InlineData(1, 2, 3, 4)]
    [InlineData(double.MaxValue, double.MaxValue, double.MaxValue, double.MaxValue)]
    [InlineData(double.PositiveInfinity, double.PositiveInfinity, double.PositiveInfinity, double.PositiveInfinity)]
    [InlineData(double.NaN, double.NaN, double.NaN, double.NaN)]
    public void Ctor_Double_Double_Double_Double(double x, double y, double width, double height)
    {
        var rect = new Rect(x, y, width, height);
        Assert.Equal(x, rect.X);
        Assert.Equal(y, rect.Y);
        Assert.Equal(width, rect.Width);
        Assert.Equal(height, rect.Height);
        Assert.Equal(x, rect.Location.X);
        Assert.Equal(y, rect.Location.Y);
        Assert.Equal(width, rect.Size.Width);
        Assert.Equal(height, rect.Size.Height);
        Assert.Equal(y, rect.Top);
        Assert.Equal(x, rect.TopLeft.X);
        Assert.Equal(y, rect.TopLeft.Y);
        Assert.Equal(x + width, rect.TopRight.X);
        Assert.Equal(y, rect.TopRight.Y);
        Assert.Equal(y + height, rect.Bottom);
        Assert.Equal(x, rect.BottomLeft.X);
        Assert.Equal(y + height, rect.BottomLeft.Y);
        Assert.Equal(x + width, rect.BottomRight.X);
        Assert.Equal(y + height, rect.BottomRight.Y);
        Assert.False(rect.IsEmpty);
    }

    [Theory]
    [InlineData(double.NegativeInfinity)]
    [InlineData(double.MinValue)]
    [InlineData(-1)]
    public void Ctor_NegativeWidth_ThrowsArgumentException(double width)
    {
        // TODO: should have paramName
        Assert.Throws<ArgumentException>(() => new Rect(0, 0, width, 0));
    }

    [Theory]
    [InlineData(double.NegativeInfinity)]
    [InlineData(double.MinValue)]
    [InlineData(-1)]
    public void Ctor_NegativeHeight_ThrowsArgumentException(double height)
    {
        // TODO: should have paramName
        Assert.Throws<ArgumentException>(() => new Rect(0, 0, 0, height));
    }

    public static IEnumerable<object[]> Ctor_Point_Point_TestData()
    {
        // Normal.
        yield return new object[] { new Point(1, 2), new Point(1, 2), 1, 2, 0, 0 };
        yield return new object[] { new Point(1, 2), new Point(3, 4), 1, 2, 2, 2 };
        yield return new object[] { new Point(1.5, 2.5), new Point(3.5, 4.5), 1.5, 2.5, 2, 2 };
        yield return new object[] { new Point(-1, -2), new Point(3, 4), -1, -2, 4, 6 };
        yield return new object[] { new Point(1, 2), new Point(-3, -4), -3, -4, 4, 6 };
        yield return new object[] { new Point(1, 2), new Point(0, 0), 0, 0, 1, 2 };
        yield return new object[] { new Point(1, 2), new Point(), 0, 0, 1, 2 };

        // Infinity.
        yield return new object[] { new Point(double.PositiveInfinity, double.PositiveInfinity), new Point(1, 2), 1, 2, double.PositiveInfinity, double.PositiveInfinity };
        yield return new object[] { new Point(1, 2), new Point(double.PositiveInfinity, double.PositiveInfinity), 1, 2, double.PositiveInfinity, double.PositiveInfinity };
        yield return new object[] { new Point(double.NegativeInfinity, double.NegativeInfinity), new Point(1, 2), double.NegativeInfinity, double.NegativeInfinity, double.PositiveInfinity, double.PositiveInfinity };
        yield return new object[] { new Point(1, 2), new Point(double.NegativeInfinity, double.NegativeInfinity), double.NegativeInfinity, double.NegativeInfinity, double.PositiveInfinity, double.PositiveInfinity };
        yield return new object[] { new Point(double.NegativeInfinity, double.NegativeInfinity), new Point(double.NegativeInfinity, double.NegativeInfinity), double.NegativeInfinity, double.NegativeInfinity, double.NaN, double.NaN };        

        // NaN.
        yield return new object[] { new Point(double.NaN, double.NaN), new Point(1, 2), double.NaN, double.NaN, double.NaN, double.NaN };
        yield return new object[] { new Point(1, 2), new Point(double.NaN, double.NaN), double.NaN, double.NaN, double.NaN, double.NaN };
        yield return new object[] { new Point(double.NaN, double.NaN), new Point(double.NaN, double.NaN), double.NaN, double.NaN, double.NaN, double.NaN };

        // Zero.
        yield return new object[] { new Point(0, 0), new Point(0, 0), 0, 0, 0, 0 };
        yield return new object[] { new Point(0, 0), new Point(), 0, 0, 0, 0 };
        yield return new object[] { new Point(0, 0), new Point(1, 2), 0, 0, 1, 2 };
        yield return new object[] { new Point(0, 0), new Point(-1, -2), -1, -2, 1, 2 };
        
        // Default.
        yield return new object[] { new Point(), new Point(0, 0), 0, 0, 0, 0 };
        yield return new object[] { new Point(), new Point(), 0, 0, 0, 0 };
        yield return new object[] { new Point(), new Point(1, 2), 0, 0, 1, 2 };
        yield return new object[] { new Point(), new Point(-1, -2), -1, -2, 1, 2 };
    }

    [Theory]
    [MemberData(nameof(Ctor_Point_Point_TestData))]
    public void Ctor_Point_Point(Point point1, Point point2, double expectedX, double expectedY, double expectedWidth, double expectedHeight)
    {
        var rect = new Rect(point1, point2);
        Assert.Equal(new Rect(expectedX, expectedY, expectedWidth, expectedHeight), rect);
        Assert.Equal(expectedX, rect.X);
        Assert.Equal(expectedY, rect.Y);
        Assert.Equal(expectedWidth, rect.Width);
        Assert.Equal(expectedHeight, rect.Height);
        Assert.Equal(expectedX, rect.Location.X);
        Assert.Equal(expectedY, rect.Location.Y);
        Assert.Equal(expectedWidth, rect.Size.Width);
        Assert.Equal(expectedHeight, rect.Size.Height);
        Assert.Equal(expectedY, rect.Top);
        Assert.Equal(expectedX, rect.TopLeft.X);
        Assert.Equal(expectedY, rect.TopLeft.Y);
        Assert.Equal(expectedX + expectedWidth, rect.TopRight.X);
        Assert.Equal(expectedY, rect.TopRight.Y);
        Assert.Equal(expectedY + expectedHeight, rect.Bottom);
        Assert.Equal(expectedX, rect.BottomLeft.X);
        Assert.Equal(expectedY + expectedHeight, rect.BottomLeft.Y);
        Assert.Equal(expectedX + expectedWidth, rect.BottomRight.X);
        Assert.Equal(expectedY + expectedHeight, rect.BottomRight.Y);
        Assert.False(rect.IsEmpty);
    }

    public static IEnumerable<object[]> Ctor_Point_Size_TestData()
    {
        // Normal.
        yield return new object[] { new Point(1, 2), new Size(1, 2), 1, 2, 1, 2 };
        yield return new object[] { new Point(1.25, 2.25), new Size(1.5, 2.5), 1.25, 2.25, 1.5, 2.5 };
        yield return new object[] { new Point(-1, -2), new Size(1, 2), -1, -2, 1, 2 };

        // Infinite.
        yield return new object[] { new Point(double.PositiveInfinity, double.PositiveInfinity), new Size(double.PositiveInfinity, double.PositiveInfinity), double.PositiveInfinity, double.PositiveInfinity, double.PositiveInfinity, double.PositiveInfinity };
        yield return new object[] { new Point(double.PositiveInfinity, double.PositiveInfinity), new Size(1, 2), double.PositiveInfinity, double.PositiveInfinity, 1, 2 };
        yield return new object[] { new Point(1, 2), new Size(double.PositiveInfinity, double.PositiveInfinity), 1, 2, double.PositiveInfinity, double.PositiveInfinity };
        yield return new object[] { new Point(double.NegativeInfinity, double.NegativeInfinity), new Size(double.PositiveInfinity, double.PositiveInfinity), double.NegativeInfinity, double.NegativeInfinity, double.PositiveInfinity, double.PositiveInfinity };
        yield return new object[] { new Point(double.NegativeInfinity, double.NegativeInfinity), new Size(1, 2), double.NegativeInfinity, double.NegativeInfinity, 1, 2 };

        // NaN.
        yield return new object[] { new Point(double.NaN, double.NaN), new Size(double.NaN, double.NaN), double.NaN, double.NaN, double.NaN, double.NaN };
        yield return new object[] { new Point(double.NaN, double.NaN), new Size(1, 2), double.NaN, double.NaN, 1, 2 };
        yield return new object[] { new Point(1, 2), new Size(double.NaN, double.NaN), 1, 2, double.NaN, double.NaN };

        // Zero.
        yield return new object[] { new Point(0, 0), new Size(1, 2), 0, 0, 1, 2 };
        yield return new object[] { new Point(0, 0), new Size(0, 0), 0, 0, 0, 0 };
        yield return new object[] { new Point(0, 0), new Size(), 0, 0, 0, 0 };
        
        // Default.
        yield return new object[] { new Point(), new Size(1, 2), 0, 0, 1, 2 };
        yield return new object[] { new Point(), new Size(0, 0), 0, 0, 0, 0 };
        yield return new object[] { new Point(), new Size(), 0, 0, 0, 0 };
    }

    [Theory]
    [MemberData(nameof(Ctor_Point_Size_TestData))]
    public void Ctor_Point_Size(Point point, Size size, double expectedX, double expectedY, double expectedWidth, double expectedHeight)
    {
        var rect = new Rect(point, size);
        Assert.Equal(new Rect(expectedX, expectedY, expectedWidth, expectedHeight), rect);
        Assert.Equal(expectedX, rect.X);
        Assert.Equal(expectedY, rect.Y);
        Assert.Equal(expectedWidth, rect.Width);
        Assert.Equal(expectedHeight, rect.Height);
        Assert.Equal(expectedX, rect.Location.X);
        Assert.Equal(expectedY, rect.Location.Y);
        Assert.Equal(expectedWidth, rect.Size.Width);
        Assert.Equal(expectedHeight, rect.Size.Height);
        Assert.Equal(expectedY, rect.Top);
        Assert.Equal(expectedX, rect.TopLeft.X);
        Assert.Equal(expectedY, rect.TopLeft.Y);
        Assert.Equal(expectedX + expectedWidth, rect.TopRight.X);
        Assert.Equal(expectedY, rect.TopRight.Y);
        Assert.Equal(expectedY + expectedHeight, rect.Bottom);
        Assert.Equal(expectedX, rect.BottomLeft.X);
        Assert.Equal(expectedY + expectedHeight, rect.BottomLeft.Y);
        Assert.Equal(expectedX + expectedWidth, rect.BottomRight.X);
        Assert.Equal(expectedY + expectedHeight, rect.BottomRight.Y);
        Assert.False(rect.IsEmpty);
    }

    public static IEnumerable<object[]> Ctor_Point_Size_Empty_TestData()
    {
        yield return new object[] { new Point(1, 2) };
        yield return new object[] { new Point(0, 0) };
        yield return new object[] { new Point() };
    }

    [Theory]
    [MemberData(nameof(Ctor_Point_Size_Empty_TestData))]
    public void Ctor_Point_Size_Empty(Point point)
    {
        var rect = new Rect(point, Size.Empty);
        Assert.Equal(Rect.Empty, rect);
        Assert.Equal(double.PositiveInfinity, rect.X);
        Assert.Equal(double.PositiveInfinity, rect.Y);
        Assert.Equal(double.NegativeInfinity, rect.Width);
        Assert.Equal(double.NegativeInfinity, rect.Height);
        Assert.Equal(double.PositiveInfinity, rect.Location.X);
        Assert.Equal(double.PositiveInfinity, rect.Location.Y);
        Assert.Equal(double.NegativeInfinity, rect.Size.Width);
        Assert.Equal(double.NegativeInfinity, rect.Size.Height);
        Assert.Equal(double.PositiveInfinity, rect.Top);
        Assert.Equal(double.PositiveInfinity, rect.TopLeft.X);
        Assert.Equal(double.PositiveInfinity, rect.TopLeft.Y);
        Assert.Equal(double.NegativeInfinity, rect.TopRight.X);
        Assert.Equal(double.PositiveInfinity, rect.TopRight.Y);
        Assert.Equal(double.NegativeInfinity, rect.Bottom);
        Assert.Equal(double.PositiveInfinity, rect.BottomLeft.X);
        Assert.Equal(double.NegativeInfinity, rect.BottomLeft.Y);
        Assert.Equal(double.NegativeInfinity, rect.BottomRight.X);
        Assert.Equal(double.NegativeInfinity, rect.BottomRight.Y);
        Assert.True(rect.IsEmpty);
    }

    public static IEnumerable<object[]> Ctor_Point_Vector_TestData()
    {
        // Normal.
        yield return new object[] { new Point(1, 2), new Vector(3, 4), 1, 2, 3, 4 };
        yield return new object[] { new Point(1.25, 2.25), new Vector(1.5, 2.5), 1.25, 2.25, 1.5, 2.5 };
        yield return new object[] { new Point(1.25, 2.25), new Vector(-1.5, -2.5), -0.25, -0.25, 1.5, 2.5 };
        yield return new object[] { new Point(1, 2), new Vector(0, 0), 1, 2, 0, 0 };
        yield return new object[] { new Point(1, 2), new Vector(), 1, 2, 0, 0 };

        // Infinite.
        yield return new object[] { new Point(double.PositiveInfinity, double.PositiveInfinity), new Vector(double.PositiveInfinity, double.PositiveInfinity), double.PositiveInfinity, double.PositiveInfinity, double.NaN, double.NaN };
        yield return new object[] { new Point(double.PositiveInfinity, double.PositiveInfinity), new Vector(1, 2), double.PositiveInfinity, double.PositiveInfinity, double.NaN, double.NaN };
        yield return new object[] { new Point(1, 2), new Vector(double.PositiveInfinity, double.PositiveInfinity), 1, 2, double.PositiveInfinity, double.PositiveInfinity };
        yield return new object[] { new Point(double.NegativeInfinity, double.NegativeInfinity), new Vector(1, 2), double.NegativeInfinity, double.NegativeInfinity, double.NaN, double.NaN };
        yield return new object[] { new Point(1, 2), new Vector(double.NegativeInfinity, double.NegativeInfinity), double.NegativeInfinity, double.NegativeInfinity, double.PositiveInfinity, double.PositiveInfinity };
        yield return new object[] { new Point(double.NegativeInfinity, double.NegativeInfinity), new Vector(double.NegativeInfinity, double.NegativeInfinity), double.NegativeInfinity, double.NegativeInfinity, double.NaN, double.NaN };

        // NaN.
        yield return new object[] { new Point(double.NaN, double.NaN), new Vector(double.NaN, double.NaN), double.NaN, double.NaN, double.NaN, double.NaN };
        yield return new object[] { new Point(double.NaN, double.NaN), new Vector(1, 2), double.NaN, double.NaN, double.NaN, double.NaN };
        yield return new object[] { new Point(1, 2), new Vector(double.NaN, double.NaN), double.NaN, double.NaN, double.NaN, double.NaN };

        // Zero.
        yield return new object[] { new Point(0, 0), new Vector(1, 2), 0, 0, 1, 2 };
        yield return new object[] { new Point(0, 0), new Vector(0, 0), 0, 0, 0, 0 };
        yield return new object[] { new Point(0, 0), new Vector(), 0, 0, 0, 0 };

        // Default.
        yield return new object[] { new Point(), new Vector(1, 2), 0, 0, 1, 2 };
        yield return new object[] { new Point(), new Vector(0, 0), 0, 0, 0, 0 };
        yield return new object[] { new Point(), new Vector(), 0, 0, 0, 0 };
    }

    [Theory]
    [MemberData(nameof(Ctor_Point_Vector_TestData))]
    public void Ctor_Point_Vector(Point point, Vector vector, double expectedX, double expectedY, double expectedWidth, double expectedHeight)
    {
        var rect = new Rect(point, vector);
        Assert.Equal(new Rect(expectedX, expectedY, expectedWidth, expectedHeight), rect);
        Assert.Equal(expectedX, rect.X);
        Assert.Equal(expectedY, rect.Y);
        Assert.Equal(expectedWidth, rect.Width);
        Assert.Equal(expectedHeight, rect.Height);
        Assert.Equal(expectedX, rect.Location.X);
        Assert.Equal(expectedY, rect.Location.Y);
        Assert.Equal(expectedWidth, rect.Size.Width);
        Assert.Equal(expectedHeight, rect.Size.Height);
        Assert.Equal(expectedY, rect.Top);
        Assert.Equal(expectedX, rect.TopLeft.X);
        Assert.Equal(expectedY, rect.TopLeft.Y);
        Assert.Equal(expectedX + expectedWidth, rect.TopRight.X);
        Assert.Equal(expectedY, rect.TopRight.Y);
        Assert.Equal(expectedY + expectedHeight, rect.Bottom);
        Assert.Equal(expectedX, rect.BottomLeft.X);
        Assert.Equal(expectedY + expectedHeight, rect.BottomLeft.Y);
        Assert.Equal(expectedX + expectedWidth, rect.BottomRight.X);
        Assert.Equal(expectedY + expectedHeight, rect.BottomRight.Y);
        Assert.False(rect.IsEmpty);
    }

    [Fact]
    public void Empty_Get_ReturnsExpected()
    {
        Rect rect = Rect.Empty;
        Assert.Equal(double.PositiveInfinity, rect.X);
        Assert.Equal(double.PositiveInfinity, rect.Y);
        Assert.Equal(double.NegativeInfinity, rect.Width);
        Assert.Equal(double.NegativeInfinity, rect.Height);
        Assert.Equal(double.PositiveInfinity, rect.Location.X);
        Assert.Equal(double.PositiveInfinity, rect.Location.Y);
        Assert.Equal(double.NegativeInfinity, rect.Size.Width);
        Assert.Equal(double.NegativeInfinity, rect.Size.Height);
        Assert.Equal(double.PositiveInfinity, rect.Top);
        Assert.Equal(double.PositiveInfinity, rect.TopLeft.X);
        Assert.Equal(double.PositiveInfinity, rect.TopLeft.Y);
        Assert.Equal(double.NegativeInfinity, rect.TopRight.X);
        Assert.Equal(double.PositiveInfinity, rect.TopRight.Y);
        Assert.Equal(double.NegativeInfinity, rect.Bottom);
        Assert.Equal(double.PositiveInfinity, rect.BottomLeft.X);
        Assert.Equal(double.NegativeInfinity, rect.BottomLeft.Y);
        Assert.Equal(double.NegativeInfinity, rect.BottomRight.X);
        Assert.Equal(double.NegativeInfinity, rect.BottomRight.Y);
        Assert.True(rect.IsEmpty);
    }

    public static IEnumerable<object[]> Location_Set_TestData()
    {
        yield return new object[] { new Point(1, 2) };
        yield return new object[] { new Point(1, 0) };
        yield return new object[] { new Point(0, 2) };
        yield return new object[] { new Point(0, 0) };
        yield return new object[] { new Point(double.NaN, double.NaN) };
        yield return new object[] { new Point(0, double.NaN) };
        yield return new object[] { new Point(double.NaN, 0) };
    }

    [Theory]
    [MemberData(nameof(Location_Set_TestData))]
    public void Location_Set_GetReturnsExpected(Point value)
    {
        var rect = new Rect(1, 2, 3, 4)
        {
            // Set.
            Location = value
        };
        Assert.Equal(value.X, rect.X);
        Assert.Equal(value.Y, rect.Y);
        Assert.Equal(3, rect.Width);
        Assert.Equal(4, rect.Height);
        Assert.Equal(value.X, rect.Location.X);
        Assert.Equal(value.Y, rect.Location.Y);
        Assert.Equal(3, rect.Size.Width);
        Assert.Equal(4, rect.Size.Height);
        Assert.Equal(value.Y, rect.Top);
        Assert.Equal(value.X, rect.TopLeft.X);
        Assert.Equal(value.Y, rect.TopLeft.Y);
        Assert.Equal(value.X + 3, rect.TopRight.X);
        Assert.Equal(value.Y, rect.TopRight.Y);
        Assert.Equal(value.Y + 4, rect.Bottom);
        Assert.Equal(value.X, rect.BottomLeft.X);
        Assert.Equal(value.Y + 4, rect.BottomLeft.Y);
        Assert.Equal(value.X + 3, rect.BottomRight.X);
        Assert.Equal(value.Y + 4, rect.BottomRight.Y);
        Assert.False(rect.IsEmpty);
        
        // Set same.
        rect.Location = value;
        Assert.Equal(value.X, rect.X);
        Assert.Equal(value.Y, rect.Y);
        Assert.Equal(3, rect.Width);
        Assert.Equal(4, rect.Height);
        Assert.Equal(value.X, rect.Location.X);
        Assert.Equal(value.Y, rect.Location.Y);
        Assert.Equal(3, rect.Size.Width);
        Assert.Equal(4, rect.Size.Height);
        Assert.Equal(value.Y, rect.Top);
        Assert.Equal(value.X, rect.TopLeft.X);
        Assert.Equal(value.Y, rect.TopLeft.Y);
        Assert.Equal(value.X + 3, rect.TopRight.X);
        Assert.Equal(value.Y, rect.TopRight.Y);
        Assert.Equal(value.Y + 4, rect.Bottom);
        Assert.Equal(value.X, rect.BottomLeft.X);
        Assert.Equal(value.Y + 4, rect.BottomLeft.Y);
        Assert.Equal(value.X + 3, rect.BottomRight.X);
        Assert.Equal(value.Y + 4, rect.BottomRight.Y);
        Assert.False(rect.IsEmpty);
    }

    [Fact]
    public void Location_SetIsEmpty_ThrowsInvalidOperationException()
    {
        Rect rect = Rect.Empty;
        Assert.Throws<InvalidOperationException>(() => rect.Location = rect.Location);
        Assert.Throws<InvalidOperationException>(() => rect.Location = new Point(1, 2));
    }
    
    public static IEnumerable<object[]> Size_Set_TestData()
    {
        yield return new object[] { new Size(1, 2) };
        yield return new object[] { new Size(1, 0) };
        yield return new object[] { new Size(0, 2) };
        yield return new object[] { new Size(0, 0) };
        yield return new object[] { new Size(double.NaN, double.NaN) };
        yield return new object[] { new Size(double.NaN, 2) };
        yield return new object[] { new Size(1, double.NaN) };
        yield return new object[] { new Size() };
    }

    [Theory]
    [MemberData(nameof(Size_Set_TestData))]
    public void Size_Set_GetReturnsExpected(Size value)
    {
        var rect = new Rect(1, 2, 3, 4)
        {
            // Set.
            Size = value
        };
        Assert.Equal(1, rect.X);
        Assert.Equal(2, rect.Y);
        Assert.Equal(value.Width, rect.Width);
        Assert.Equal(value.Height, rect.Height);
        Assert.Equal(1, rect.Location.X);
        Assert.Equal(2, rect.Location.Y);
        Assert.Equal(value.Width, rect.Size.Width);
        Assert.Equal(value.Height, rect.Size.Height);
        Assert.Equal(2, rect.Top);
        Assert.Equal(1, rect.TopLeft.X);
        Assert.Equal(2, rect.TopLeft.Y);
        Assert.Equal(1 + value.Width, rect.TopRight.X);
        Assert.Equal(2, rect.TopRight.Y);
        Assert.Equal(2 + value.Height, rect.Bottom);
        Assert.Equal(1, rect.BottomLeft.X);
        Assert.Equal(2 + value.Height, rect.BottomLeft.Y);
        Assert.Equal(1 + value.Width, rect.BottomRight.X);
        Assert.Equal(2 + value.Height, rect.BottomRight.Y);
        Assert.False(rect.IsEmpty);
        
        // Set same.
        rect.Size = value;
        Assert.Equal(1, rect.X);
        Assert.Equal(2, rect.Y);
        Assert.Equal(value.Width, rect.Width);
        Assert.Equal(value.Height, rect.Height);
        Assert.Equal(1, rect.Location.X);
        Assert.Equal(2, rect.Location.Y);
        Assert.Equal(value.Width, rect.Size.Width);
        Assert.Equal(value.Height, rect.Size.Height);
        Assert.Equal(2, rect.Top);
        Assert.Equal(1, rect.TopLeft.X);
        Assert.Equal(2, rect.TopLeft.Y);
        Assert.Equal(1 + value.Width, rect.TopRight.X);
        Assert.Equal(2, rect.TopRight.Y);
        Assert.Equal(2 + value.Height, rect.Bottom);
        Assert.Equal(1, rect.BottomLeft.X);
        Assert.Equal(2 + value.Height, rect.BottomLeft.Y);
        Assert.Equal(1 + value.Width, rect.BottomRight.X);
        Assert.Equal(2 + value.Height, rect.BottomRight.Y);
        Assert.False(rect.IsEmpty);
    }

    [Fact]
    public void Size_SetEmpty_Success()
    {
        var rect = new Rect(1, 2, 3, 4)
        {
            // Set.
            Size = Size.Empty
        };
        Assert.Equal(double.PositiveInfinity, rect.X);
        Assert.Equal(double.PositiveInfinity, rect.Y);
        Assert.Equal(double.NegativeInfinity, rect.Width);
        Assert.Equal(double.NegativeInfinity, rect.Height);
        Assert.Equal(double.PositiveInfinity, rect.Location.X);
        Assert.Equal(double.PositiveInfinity, rect.Location.Y);
        Assert.Equal(double.NegativeInfinity, rect.Size.Width);
        Assert.Equal(double.NegativeInfinity, rect.Size.Height);
        Assert.Equal(double.PositiveInfinity, rect.Top);
        Assert.Equal(double.PositiveInfinity, rect.TopLeft.X);
        Assert.Equal(double.PositiveInfinity, rect.TopLeft.Y);
        Assert.Equal(double.NegativeInfinity, rect.TopRight.X);
        Assert.Equal(double.PositiveInfinity, rect.TopRight.Y);
        Assert.Equal(double.NegativeInfinity, rect.Bottom);
        Assert.Equal(double.PositiveInfinity, rect.BottomLeft.X);
        Assert.Equal(double.NegativeInfinity, rect.BottomLeft.Y);
        Assert.Equal(double.NegativeInfinity, rect.BottomRight.X);
        Assert.Equal(double.NegativeInfinity, rect.BottomRight.Y);
        Assert.True(rect.IsEmpty);
    }
    
    [Fact]
    public void Size_SetIsEmpty_ThrowsInvalidOperationException()
    {
        Rect rect = Rect.Empty;
        Assert.Throws<InvalidOperationException>(() => rect.Size = new Size(1, 2));
        
        // Setting to Size.Empty should work.
        rect.Size = rect.Size;
        Assert.Equal(double.PositiveInfinity, rect.X);
        Assert.Equal(double.PositiveInfinity, rect.Y);
        Assert.Equal(double.NegativeInfinity, rect.Width);
        Assert.Equal(double.NegativeInfinity, rect.Height);
        Assert.Equal(double.PositiveInfinity, rect.Location.X);
        Assert.Equal(double.PositiveInfinity, rect.Location.Y);
        Assert.Equal(double.NegativeInfinity, rect.Size.Width);
        Assert.Equal(double.NegativeInfinity, rect.Size.Height);
        Assert.Equal(double.PositiveInfinity, rect.Top);
        Assert.Equal(double.PositiveInfinity, rect.TopLeft.X);
        Assert.Equal(double.PositiveInfinity, rect.TopLeft.Y);
        Assert.Equal(double.NegativeInfinity, rect.TopRight.X);
        Assert.Equal(double.PositiveInfinity, rect.TopRight.Y);
        Assert.Equal(double.NegativeInfinity, rect.Bottom);
        Assert.Equal(double.PositiveInfinity, rect.BottomLeft.X);
        Assert.Equal(double.NegativeInfinity, rect.BottomLeft.Y);
        Assert.Equal(double.NegativeInfinity, rect.BottomRight.X);
        Assert.Equal(double.NegativeInfinity, rect.BottomRight.Y);
        Assert.True(rect.IsEmpty);
    }

    public static IEnumerable<object[]> X_Set_TestData()
    {
        yield return new object[] { double.NegativeInfinity };
        yield return new object[] { double.MinValue };
        yield return new object[] { -1 };
        yield return new object[] { double.NegativeZero };
        yield return new object[] { 0 };
        yield return new object[] { 0.1 };
        yield return new object[] { 1 };
        yield return new object[] { double.MaxValue };
        yield return new object[] { double.PositiveInfinity };
    }

    [Theory]
    [MemberData(nameof(X_Set_TestData))]
    public void X_Set_GetReturnsExpected(double value)
    {
        var rect = new Rect(1, 2, 3, 4)
        {
            // Set.
            X = value
        };
        Assert.Equal(value, rect.X);
        Assert.Equal(2, rect.Y);
        Assert.Equal(3, rect.Width);
        Assert.Equal(4, rect.Height);
        Assert.Equal(value, rect.Location.X);
        Assert.Equal(2, rect.Location.Y);
        Assert.Equal(3, rect.Size.Width);
        Assert.Equal(4, rect.Size.Height);
        Assert.Equal(2, rect.Top);
        Assert.Equal(value, rect.TopLeft.X);
        Assert.Equal(2, rect.TopLeft.Y);
        Assert.Equal(value + 3, rect.TopRight.X);
        Assert.Equal(2, rect.TopRight.Y);
        Assert.Equal(6, rect.Bottom);
        Assert.Equal(value, rect.BottomLeft.X);
        Assert.Equal(6, rect.BottomLeft.Y);
        Assert.Equal(value + 3, rect.BottomRight.X);
        Assert.Equal(6, rect.BottomRight.Y);
        Assert.False(rect.IsEmpty);
        
        // Set same.
        rect.X = value;
        Assert.Equal(value, rect.X);
        Assert.Equal(2, rect.Y);
        Assert.Equal(3, rect.Width);
        Assert.Equal(4, rect.Height);
        Assert.Equal(value, rect.Location.X);
        Assert.Equal(2, rect.Location.Y);
        Assert.Equal(3, rect.Size.Width);
        Assert.Equal(4, rect.Size.Height);
        Assert.Equal(2, rect.Top);
        Assert.Equal(value, rect.TopLeft.X);
        Assert.Equal(2, rect.TopLeft.Y);
        Assert.Equal(value + 3, rect.TopRight.X);
        Assert.Equal(2, rect.TopRight.Y);
        Assert.Equal(6, rect.Bottom);
        Assert.Equal(value, rect.BottomLeft.X);
        Assert.Equal(6, rect.BottomLeft.Y);
        Assert.Equal(value + 3, rect.BottomRight.X);
        Assert.Equal(6, rect.BottomRight.Y);
        Assert.False(rect.IsEmpty);
    }

    [Fact]
    public void X_SetIsEmpty_ThrowsInvalidOperationException()
    {
        Rect rect = Rect.Empty;
        Assert.Throws<InvalidOperationException>(() => rect.X = rect.X);
        Assert.Throws<InvalidOperationException>(() => rect.X = 1);
    }

    public static IEnumerable<object[]> Y_Set_TestData()
    {
        yield return new object[] { double.NegativeInfinity };
        yield return new object[] { double.MinValue };
        yield return new object[] { -1 };
        yield return new object[] { double.NegativeZero };
        yield return new object[] { 0 };
        yield return new object[] { 0.1 };
        yield return new object[] { 1 };
        yield return new object[] { double.MaxValue };
        yield return new object[] { double.PositiveInfinity };
    }

    [Theory]
    [MemberData(nameof(Y_Set_TestData))]
    public void Y_Set_GetReturnsExpected(double value)
    {
        var rect = new Rect(1, 2, 3, 4)
        {
            // Set.
            Y = value
        };
        Assert.Equal(1, rect.X);
        Assert.Equal(value, rect.Y);
        Assert.Equal(3, rect.Width);
        Assert.Equal(4, rect.Height);
        Assert.Equal(1, rect.Location.X);
        Assert.Equal(value, rect.Location.Y);
        Assert.Equal(3, rect.Size.Width);
        Assert.Equal(4, rect.Size.Height);
        Assert.Equal(value, rect.Top);
        Assert.Equal(1, rect.TopLeft.X);
        Assert.Equal(value, rect.TopLeft.Y);
        Assert.Equal(4, rect.TopRight.X);
        Assert.Equal(value, rect.TopRight.Y);
        Assert.Equal(value + 4, rect.Bottom);
        Assert.Equal(1, rect.BottomLeft.X);
        Assert.Equal(value + 4, rect.BottomLeft.Y);
        Assert.Equal(4, rect.BottomRight.X);
        Assert.Equal(value + 4, rect.BottomRight.Y);
        Assert.False(rect.IsEmpty);

        // Set same.
        rect.Y = value;
        Assert.Equal(1, rect.X);
        Assert.Equal(value, rect.Y);
        Assert.Equal(3, rect.Width);
        Assert.Equal(4, rect.Height);
        Assert.Equal(1, rect.Location.X);
        Assert.Equal(value, rect.Location.Y);
        Assert.Equal(3, rect.Size.Width);
        Assert.Equal(4, rect.Size.Height);
        Assert.Equal(value, rect.Top);
        Assert.Equal(1, rect.TopLeft.X);
        Assert.Equal(value, rect.TopLeft.Y);
        Assert.Equal(4, rect.TopRight.X);
        Assert.Equal(value, rect.TopRight.Y);
        Assert.Equal(value + 4, rect.Bottom);
        Assert.Equal(1, rect.BottomLeft.X);
        Assert.Equal(value + 4, rect.BottomLeft.Y);
        Assert.Equal(4, rect.BottomRight.X);
        Assert.Equal(value + 4, rect.BottomRight.Y);
        Assert.False(rect.IsEmpty);
    }

    [Fact]
    public void Y_SetIsEmpty_ThrowsInvalidOperationException()
    {
        Rect rect = Rect.Empty;
        Assert.Throws<InvalidOperationException>(() => rect.Y = rect.Y);
        Assert.Throws<InvalidOperationException>(() => rect.Y = 1);
    }

    public static IEnumerable<object[]> Width_Set_TestData()
    {
        yield return new object[] { double.NegativeZero };
        yield return new object[] { 0 };
        yield return new object[] { 0.2 };
        yield return new object[] { 1 };
        yield return new object[] { double.MaxValue };
        yield return new object[] { double.PositiveInfinity };
    }

    [Theory]
    [MemberData(nameof(Width_Set_TestData))]
    public void Width_Set_GetReturnsExpected(double value)
    {
        var rect = new Rect(1, 2, 3, 4)
        {
            // Set.
            Width = value
        };
        Assert.Equal(1, rect.X);
        Assert.Equal(2, rect.Y);
        Assert.Equal(value, rect.Width);
        Assert.Equal(4, rect.Height);
        Assert.Equal(1, rect.Location.X);
        Assert.Equal(2, rect.Location.Y);
        Assert.Equal(value, rect.Size.Width);
        Assert.Equal(4, rect.Size.Height);
        Assert.Equal(2, rect.Top);
        Assert.Equal(1, rect.TopLeft.X);
        Assert.Equal(2, rect.TopLeft.Y);
        Assert.Equal(value + 1, rect.TopRight.X);
        Assert.Equal(2, rect.TopRight.Y);
        Assert.Equal(6, rect.Bottom);
        Assert.Equal(1, rect.BottomLeft.X);
        Assert.Equal(6, rect.BottomLeft.Y);
        Assert.Equal(value + 1, rect.BottomRight.X);
        Assert.Equal(6, rect.BottomRight.Y);
        Assert.False(rect.IsEmpty);

        // Set same.
        rect.Width = value;
        Assert.Equal(1, rect.X);
        Assert.Equal(2, rect.Y);
        Assert.Equal(value, rect.Width);
        Assert.Equal(4, rect.Height);
        Assert.Equal(1, rect.Location.X);
        Assert.Equal(2, rect.Location.Y);
        Assert.Equal(value, rect.Size.Width);
        Assert.Equal(4, rect.Size.Height);
        Assert.Equal(2, rect.Top);
        Assert.Equal(1, rect.TopLeft.X);
        Assert.Equal(2, rect.TopLeft.Y);
        Assert.Equal(value + 1, rect.TopRight.X);
        Assert.Equal(2, rect.TopRight.Y);
        Assert.Equal(6, rect.Bottom);
        Assert.Equal(1, rect.BottomLeft.X);
        Assert.Equal(6, rect.BottomLeft.Y);
        Assert.Equal(value + 1, rect.BottomRight.X);
        Assert.Equal(6, rect.BottomRight.Y);
        Assert.False(rect.IsEmpty);
    }

    [Fact]
    public void Width_SetIsEmpty_ThrowsInvalidOperationException()
    {
        Rect rect = Rect.Empty;
        Assert.Throws<InvalidOperationException>(() => rect.Width = rect.Width);
        Assert.Throws<InvalidOperationException>(() => rect.Width = 1);
        Assert.Throws<InvalidOperationException>(() => rect.Width = -1);
    }

    [Theory]
    [InlineData(double.NegativeInfinity)]
    [InlineData(double.MinValue)]
    [InlineData(-1)]
    public void Width_SetNegative_ThrowsArgumentException(double width)
    {
        var rect = new Rect(1, 2, 3, 4);
        // TODO: should have paramName
        Assert.Throws<ArgumentException>(() => rect.Width = width);
    }

    public static IEnumerable<object[]> Height_Set_TestData()
    {
        yield return new object[] { double.NegativeZero };
        yield return new object[] { 0 };
        yield return new object[] { 0.2 };
        yield return new object[] { 1 };
        yield return new object[] { double.MaxValue };
        yield return new object[] { double.PositiveInfinity };
    }

    [Theory]
    [MemberData(nameof(Height_Set_TestData))]
    public void Height_Set_GetReturnsExpected(double value)
    {
        var rect = new Rect(1, 2, 3, 4)
        {
            // Set.
            Height = value
        };
        Assert.Equal(1, rect.X);
        Assert.Equal(2, rect.Y);
        Assert.Equal(3, rect.Width);
        Assert.Equal(value, rect.Height);
        Assert.Equal(1, rect.Location.X);
        Assert.Equal(2, rect.Location.Y);
        Assert.Equal(3, rect.Size.Width);
        Assert.Equal(value, rect.Size.Height);
        Assert.Equal(2, rect.Top);
        Assert.Equal(1, rect.TopLeft.X);
        Assert.Equal(2, rect.TopLeft.Y);
        Assert.Equal(4, rect.TopRight.X);
        Assert.Equal(2, rect.TopRight.Y);
        Assert.Equal(value + 2, rect.Bottom);
        Assert.Equal(1, rect.BottomLeft.X);
        Assert.Equal(value + 2, rect.BottomLeft.Y);
        Assert.Equal(4, rect.BottomRight.X);
        Assert.Equal(value + 2, rect.BottomRight.Y);
        Assert.False(rect.IsEmpty);

        // Set same.
        rect.Height = value;
        Assert.Equal(1, rect.X);
        Assert.Equal(2, rect.Y);
        Assert.Equal(3, rect.Width);
        Assert.Equal(value, rect.Height);
        Assert.Equal(1, rect.Location.X);
        Assert.Equal(2, rect.Location.Y);
        Assert.Equal(3, rect.Size.Width);
        Assert.Equal(value, rect.Size.Height);
        Assert.Equal(2, rect.Top);
        Assert.Equal(1, rect.TopLeft.X);
        Assert.Equal(2, rect.TopLeft.Y);
        Assert.Equal(4, rect.TopRight.X);
        Assert.Equal(2, rect.TopRight.Y);
        Assert.Equal(value + 2, rect.Bottom);
        Assert.Equal(1, rect.BottomLeft.X);
        Assert.Equal(value + 2, rect.BottomLeft.Y);
        Assert.Equal(4, rect.BottomRight.X);
        Assert.Equal(value + 2, rect.BottomRight.Y);
        Assert.False(rect.IsEmpty);
    }

    [Fact]
    public void Height_SetIsEmpty_ThrowsInvalidOperationException()
    {
        Rect rect = Rect.Empty;
        Assert.Throws<InvalidOperationException>(() => rect.Height = rect.Height);
        Assert.Throws<InvalidOperationException>(() => rect.Height = 1);
        Assert.Throws<InvalidOperationException>(() => rect.Height = -1);
    }

    [Theory]
    [InlineData(double.NegativeInfinity)]
    [InlineData(double.MinValue)]
    [InlineData(-1)]
    public void Height_SetNegative_ThrowsArgumentException(double height)
    {
        var rect = new Rect(1, 2, 3, 4);
        // TODO: should have paramName
        Assert.Throws<ArgumentException>(() => rect.Height = height);
    }

    public static IEnumerable<object[]> Contains_Point_TestData()
    {
        // Normal.
        yield return new object[] { new Rect(1, 2, 3, 4), new Point(), false };
        yield return new object[] { new Rect(1, 2, 3, 4), new Point(0, 0), false };
        yield return new object[] { new Rect(1, 2, 3, 4), new Point(1, 1), false };
        yield return new object[] { new Rect(1, 2, 3, 4), new Point(1, 2), true };
        yield return new object[] { new Rect(1, 2, 3, 4), new Point(1, 3), true };
        yield return new object[] { new Rect(1, 2, 3, 4), new Point(1, 4), true };
        yield return new object[] { new Rect(1, 2, 3, 4), new Point(1, 5), true };
        yield return new object[] { new Rect(1, 2, 3, 4), new Point(1, 6), true };
        yield return new object[] { new Rect(1, 2, 3, 4), new Point(1, 7), false };
        yield return new object[] { new Rect(1, 2, 3, 4), new Point(2, 2), true };
        yield return new object[] { new Rect(1, 2, 3, 4), new Point(2, 3), true };
        yield return new object[] { new Rect(1, 2, 3, 4), new Point(2, 4), true };
        yield return new object[] { new Rect(1, 2, 3, 4), new Point(2, 5), true };
        yield return new object[] { new Rect(1, 2, 3, 4), new Point(2, 6), true };
        yield return new object[] { new Rect(1, 2, 3, 4), new Point(2, 7), false };
        yield return new object[] { new Rect(1, 2, 3, 4), new Point(3, 1), false };
        yield return new object[] { new Rect(1, 2, 3, 4), new Point(3, 2), true };
        yield return new object[] { new Rect(1, 2, 3, 4), new Point(3, 3), true };
        yield return new object[] { new Rect(1, 2, 3, 4), new Point(3, 4), true };
        yield return new object[] { new Rect(1, 2, 3, 4), new Point(3, 5), true };
        yield return new object[] { new Rect(1, 2, 3, 4), new Point(3, 6), true };
        yield return new object[] { new Rect(1, 2, 3, 4), new Point(3, 7), false };
        yield return new object[] { new Rect(1, 2, 3, 4), new Point(4, 1), false };
        yield return new object[] { new Rect(1, 2, 3, 4), new Point(4, 2), true };
        yield return new object[] { new Rect(1, 2, 3, 4), new Point(4, 3), true };
        yield return new object[] { new Rect(1, 2, 3, 4), new Point(4, 4), true };
        yield return new object[] { new Rect(1, 2, 3, 4), new Point(4, 5), true };
        yield return new object[] { new Rect(1, 2, 3, 4), new Point(4, 6), true };
        yield return new object[] { new Rect(1, 2, 3, 4), new Point(4, 7), false };
        yield return new object[] { new Rect(1, 2, 3, 4), new Point(5, 1), false };
        yield return new object[] { new Rect(1, 2, 3, 4), new Point(5, 2), false };
        yield return new object[] { new Rect(1, 2, 3, 4), new Point(5, 3), false };
        yield return new object[] { new Rect(1, 2, 3, 4), new Point(5, 4), false };
        yield return new object[] { new Rect(1, 2, 3, 4), new Point(5, 5), false };
        yield return new object[] { new Rect(1, 2, 3, 4), new Point(5, 6), false };
        yield return new object[] { new Rect(1, 2, 3, 4), new Point(5, 7), false };

        // Empty width.
        yield return new object[] { new Rect(1, 2, 0, 4), new Point(), false };
        yield return new object[] { new Rect(1, 2, 0, 4), new Point(0, 0), false };
        yield return new object[] { new Rect(1, 2, 0, 4), new Point(0, 1), false };
        yield return new object[] { new Rect(1, 2, 0, 4), new Point(0, 2), false };
        yield return new object[] { new Rect(1, 2, 0, 4), new Point(0, 3), false };
        yield return new object[] { new Rect(1, 2, 0, 4), new Point(0, 4), false };
        yield return new object[] { new Rect(1, 2, 0, 4), new Point(0, 5), false };
        yield return new object[] { new Rect(1, 2, 0, 4), new Point(0, 6), false };
        yield return new object[] { new Rect(1, 2, 0, 4), new Point(0, 7), false };
        yield return new object[] { new Rect(1, 2, 0, 4), new Point(1, 1), false };
        yield return new object[] { new Rect(1, 2, 0, 4), new Point(1, 2), true };
        yield return new object[] { new Rect(1, 2, 0, 4), new Point(1, 3), true };
        yield return new object[] { new Rect(1, 2, 0, 4), new Point(1, 4), true };
        yield return new object[] { new Rect(1, 2, 0, 4), new Point(1, 5), true };
        yield return new object[] { new Rect(1, 2, 0, 4), new Point(1, 6), true };
        yield return new object[] { new Rect(1, 2, 0, 4), new Point(1, 6), true };
        yield return new object[] { new Rect(1, 2, 0, 4), new Point(1, 7), false };
        yield return new object[] { new Rect(1, 2, 0, 4), new Point(2, 1), false };
        yield return new object[] { new Rect(1, 2, 0, 4), new Point(2, 2), false };
        yield return new object[] { new Rect(1, 2, 0, 4), new Point(2, 3), false };
        yield return new object[] { new Rect(1, 2, 0, 4), new Point(2, 4), false };
        yield return new object[] { new Rect(1, 2, 0, 4), new Point(2, 5), false };
        yield return new object[] { new Rect(1, 2, 0, 4), new Point(2, 6), false };
        yield return new object[] { new Rect(1, 2, 0, 4), new Point(2, 6), false };
        yield return new object[] { new Rect(1, 2, 0, 4), new Point(2, 7), false };

        // Empty height.
        yield return new object[] { new Rect(1, 2, 3, 0), new Point(), false };
        yield return new object[] { new Rect(1, 2, 3, 0), new Point(0, 0), false };
        yield return new object[] { new Rect(1, 2, 3, 0), new Point(0, 1), false };
        yield return new object[] { new Rect(1, 2, 3, 0), new Point(0, 2), false };
        yield return new object[] { new Rect(1, 2, 3, 0), new Point(0, 3), false };
        yield return new object[] { new Rect(1, 2, 3, 0), new Point(0, 4), false };
        yield return new object[] { new Rect(1, 2, 3, 0), new Point(0, 5), false };
        yield return new object[] { new Rect(1, 2, 3, 0), new Point(0, 6), false };
        yield return new object[] { new Rect(1, 2, 3, 0), new Point(0, 7), false };
        yield return new object[] { new Rect(1, 2, 3, 0), new Point(1, 1), false };
        yield return new object[] { new Rect(1, 2, 3, 0), new Point(1, 2), true };
        yield return new object[] { new Rect(1, 2, 3, 0), new Point(1, 3), false };
        yield return new object[] { new Rect(1, 2, 3, 0), new Point(1, 4), false };
        yield return new object[] { new Rect(1, 2, 3, 0), new Point(1, 5), false };
        yield return new object[] { new Rect(1, 2, 3, 0), new Point(1, 6), false };
        yield return new object[] { new Rect(1, 2, 3, 0), new Point(1, 7), false };
        yield return new object[] { new Rect(1, 2, 3, 0), new Point(2, 1), false };
        yield return new object[] { new Rect(1, 2, 3, 0), new Point(2, 2), true };
        yield return new object[] { new Rect(1, 2, 3, 0), new Point(2, 3), false };
        yield return new object[] { new Rect(1, 2, 3, 0), new Point(2, 4), false };
        yield return new object[] { new Rect(1, 2, 3, 0), new Point(2, 5), false };
        yield return new object[] { new Rect(1, 2, 3, 0), new Point(2, 6), false };
        yield return new object[] { new Rect(1, 2, 3, 0), new Point(2, 7), false };
        yield return new object[] { new Rect(1, 2, 3, 0), new Point(3, 1), false };
        yield return new object[] { new Rect(1, 2, 3, 0), new Point(3, 2), true };
        yield return new object[] { new Rect(1, 2, 3, 0), new Point(3, 3), false };
        yield return new object[] { new Rect(1, 2, 3, 0), new Point(3, 4), false };
        yield return new object[] { new Rect(1, 2, 3, 0), new Point(3, 5), false };
        yield return new object[] { new Rect(1, 2, 3, 0), new Point(3, 6), false };
        yield return new object[] { new Rect(1, 2, 3, 0), new Point(3, 7), false };
        yield return new object[] { new Rect(1, 2, 3, 0), new Point(4, 1), false };
        yield return new object[] { new Rect(1, 2, 3, 0), new Point(4, 2), true };
        yield return new object[] { new Rect(1, 2, 3, 0), new Point(4, 3), false };
        yield return new object[] { new Rect(1, 2, 3, 0), new Point(4, 4), false };
        yield return new object[] { new Rect(1, 2, 3, 0), new Point(4, 5), false };
        yield return new object[] { new Rect(1, 2, 3, 0), new Point(4, 6), false };
        yield return new object[] { new Rect(1, 2, 3, 0), new Point(4, 7), false };
        yield return new object[] { new Rect(1, 2, 3, 0), new Point(5, 1), false };
        yield return new object[] { new Rect(1, 2, 3, 0), new Point(5, 2), false };
        yield return new object[] { new Rect(1, 2, 3, 0), new Point(5, 3), false };
        yield return new object[] { new Rect(1, 2, 3, 0), new Point(5, 4), false };
        yield return new object[] { new Rect(1, 2, 3, 0), new Point(5, 5), false };
        yield return new object[] { new Rect(1, 2, 3, 0), new Point(5, 6), false };
        yield return new object[] { new Rect(1, 2, 3, 0), new Point(5, 7), false };

        // Empty width & height.
        yield return new object[] { new Rect(1, 2, 0, 0), new Point(), false };
        yield return new object[] { new Rect(1, 2, 0, 0), new Point(0, 0), false };
        yield return new object[] { new Rect(1, 2, 0, 0), new Point(0, 1), false };
        yield return new object[] { new Rect(1, 2, 0, 0), new Point(0, 2), false };
        yield return new object[] { new Rect(1, 2, 0, 0), new Point(1, 1), false };
        yield return new object[] { new Rect(1, 2, 0, 0), new Point(1, 2), true };
        yield return new object[] { new Rect(1, 2, 0, 0), new Point(1, 3), false };
        yield return new object[] { new Rect(1, 2, 0, 0), new Point(2, 1), false };
        yield return new object[] { new Rect(1, 2, 0, 0), new Point(2, 2), false };
        yield return new object[] { new Rect(1, 2, 0, 0), new Point(2, 3), false };

        // Zero.
        yield return new object[] { new Rect(0, 0, 0, 0), new Point(), true };
        yield return new object[] { new Rect(0, 0, 0, 0), new Point(0, 0), true };
        yield return new object[] { new Rect(0, 0, 0, 0), new Point(1, 0), false };
        yield return new object[] { new Rect(0, 0, 0, 0), new Point(0, 1), false };

        // Default.
        yield return new object[] { new Rect(), new Point(), true };
        yield return new object[] { new Rect(), new Point(0, 0), true };
        yield return new object[] { new Rect(), new Point(1, 0), false };
        yield return new object[] { new Rect(), new Point(0, 1), false };

        // Infinite bounds.
        yield return new object[] { new Rect(double.NegativeInfinity, double.NegativeInfinity, double.PositiveInfinity, double.PositiveInfinity), new Point(0, 0), true };
        yield return new object[] { new Rect(double.NegativeInfinity, double.NegativeInfinity, double.PositiveInfinity, double.PositiveInfinity), new Point(1, 2), true };
        yield return new object[] { new Rect(double.NegativeInfinity, double.NegativeInfinity, double.PositiveInfinity, double.PositiveInfinity), new Point(double.NegativeInfinity, double.NegativeInfinity), true };
        yield return new object[] { new Rect(double.NegativeInfinity, double.NegativeInfinity, double.PositiveInfinity, double.PositiveInfinity), new Point(double.MinValue, double.MinValue), true };
        yield return new object[] { new Rect(double.NegativeInfinity, double.NegativeInfinity, double.PositiveInfinity, double.PositiveInfinity), new Point(double.MaxValue, double.MaxValue), true };
        yield return new object[] { new Rect(double.NegativeInfinity, double.NegativeInfinity, double.PositiveInfinity, double.PositiveInfinity), new Point(double.PositiveInfinity, double.PositiveInfinity), false };

        yield return new object[] { new Rect(double.PositiveInfinity, double.PositiveInfinity, double.PositiveInfinity, double.PositiveInfinity), new Point(double.PositiveInfinity, double.PositiveInfinity), false };
        yield return new object[] { new Rect(double.PositiveInfinity, double.PositiveInfinity, double.PositiveInfinity, double.PositiveInfinity), new Point(double.MaxValue, double.MaxValue), false };

        // Infinite width/height.
        yield return new object[] { new Rect(1, 2, double.PositiveInfinity, double.PositiveInfinity), new Point(0, 0), false };
        yield return new object[] { new Rect(1, 2, double.PositiveInfinity, double.PositiveInfinity), new Point(1, 2), true };
        yield return new object[] { new Rect(1, 2, double.PositiveInfinity, double.PositiveInfinity), new Point(2, 3), true };
        yield return new object[] { new Rect(1, 2, double.PositiveInfinity, double.PositiveInfinity), new Point(double.PositiveInfinity, double.PositiveInfinity), false };
        yield return new object[] { new Rect(1, 2, double.PositiveInfinity, double.PositiveInfinity), new Point(double.NegativeInfinity, double.NegativeInfinity), false };
        yield return new object[] { new Rect(1, 2, double.PositiveInfinity, double.PositiveInfinity), new Point(double.NaN, double.NaN), false };

        // NaN
        yield return new object[] { new Rect(double.NaN, 2, 3, 4), new Point(1, 2), false };
        yield return new object[] { new Rect(double.NaN, 2, 3, 4), new Point(double.NaN, 2), false };
        yield return new object[] { new Rect(1, double.NaN, 3, 4), new Point(1, 2), false };
        yield return new object[] { new Rect(1, double.NaN, 3, 4), new Point(1, double.NaN), false };
        yield return new object[] { new Rect(1, 2, double.NaN, 4), new Point(1, 2), false };
        yield return new object[] { new Rect(1, 2, double.NaN, 4), new Point(1, double.NaN), false };
        yield return new object[] { new Rect(1, 2, 3, double.NaN), new Point(1, 2), false };
        yield return new object[] { new Rect(1, 2, 3, double.NaN), new Point(1, double.NaN), false };

        // Empty.
        yield return new object[] { Rect.Empty, new Point(), false };
        yield return new object[] { Rect.Empty, new Point(0, 0), false };
        yield return new object[] { Rect.Empty, new Point(1, 2), false };
        yield return new object[] { Rect.Empty, new Point(double.PositiveInfinity, double.PositiveInfinity), false };
        yield return new object[] { Rect.Empty, new Point(double.NaN, double.NaN), false };
    }

    [Theory]
    [MemberData(nameof(Contains_Point_TestData))]
    public void Contains_InvokePoint_ReturnsExpected(Rect rect, Point point, bool expected)
    {
        Assert.Equal(expected, rect.Contains(point));
    }

    [Theory]
    [MemberData(nameof(Contains_Point_TestData))]
    public void Contains_InvokeDoubleDouble_ReturnsExpected(Rect rect, Point point, bool expected)
    {
        Assert.Equal(expected, rect.Contains(point.X, point.Y));
    }

    public static IEnumerable<object[]> Contains_Rect_TestData()
    {
        // Normal - from top left.
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(1, 2, 3, 5), false };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(1, 2, 3, 4), true };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(1, 2, 3, 3), true };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(1, 2, 3, 2), true };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(1, 2, 3, 1), true };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(1, 2, 3, 0), true };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(1, 2, 2, 5), false };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(1, 2, 2, 4), true };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(1, 2, 2, 3), true };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(1, 2, 2, 2), true };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(1, 2, 2, 1), true };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(1, 2, 2, 0), true };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(1, 2, 1, 5), false };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(1, 2, 1, 4), true };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(1, 2, 1, 3), true };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(1, 2, 1, 2), true };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(1, 2, 1, 1), true };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(1, 2, 1, 0), true };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(1, 2, 0, 5), false };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(1, 2, 0, 4), true };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(1, 2, 0, 3), true };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(1, 2, 0, 2), true };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(1, 2, 0, 1), true };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(1, 2, 0, 0), true };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(1, 2, 4, 4), false };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(1, 2, 3, 5), false };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(1, 2, 4, 5), false };

        // Normal - from middle left.
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(1, 3, 3, 5), false };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(1, 3, 3, 4), false };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(1, 3, 3, 3), true };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(1, 3, 3, 2), true };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(1, 3, 3, 1), true };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(1, 3, 3, 0), true };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(1, 3, 2, 5), false };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(1, 3, 2, 4), false };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(1, 3, 2, 3), true };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(1, 3, 2, 2), true };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(1, 3, 2, 1), true };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(1, 3, 2, 0), true };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(1, 3, 1, 5), false };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(1, 3, 1, 4), false };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(1, 3, 1, 3), true };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(1, 3, 1, 2), true };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(1, 3, 1, 1), true };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(1, 3, 1, 0), true };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(1, 3, 0, 5), false };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(1, 3, 0, 4), false };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(1, 3, 0, 3), true };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(1, 3, 0, 2), true };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(1, 3, 0, 1), true };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(1, 3, 0, 0), true };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(1, 3, 4, 4), false };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(1, 3, 3, 5), false };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(1, 3, 4, 5), false };

        // Normal - from bottom left.
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(1, 5, 3, 5), false };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(1, 5, 3, 4), false };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(1, 5, 3, 3), false };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(1, 5, 3, 2), false };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(1, 5, 3, 1), true };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(1, 5, 3, 0), true };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(1, 5, 2, 5), false };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(1, 5, 2, 4), false };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(1, 5, 2, 3), false };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(1, 5, 2, 2), false };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(1, 5, 2, 1), true };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(1, 5, 2, 0), true };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(1, 5, 1, 5), false };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(1, 5, 1, 4), false };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(1, 5, 1, 3), false };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(1, 5, 1, 2), false };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(1, 5, 1, 1), true };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(1, 5, 1, 0), true };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(1, 5, 0, 5), false };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(1, 5, 0, 4), false };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(1, 5, 0, 3), false };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(1, 5, 0, 2), false };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(1, 5, 0, 1), true };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(1, 5, 0, 0), true };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(1, 5, 4, 4), false };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(1, 5, 3, 5), false };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(1, 5, 4, 5), false };
        
        // Normal - from bottom middle.
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(2, 6, 3, 5), false };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(2, 6, 3, 4), false };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(2, 6, 3, 3), false };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(2, 6, 3, 2), false };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(2, 6, 3, 1), false };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(2, 6, 3, 0), false };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(2, 6, 2, 5), false };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(2, 6, 2, 4), false };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(2, 6, 2, 3), false };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(2, 6, 2, 2), false };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(2, 6, 2, 1), false };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(2, 6, 2, 0), true };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(2, 6, 1, 5), false };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(2, 6, 1, 4), false };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(2, 6, 1, 3), false };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(2, 6, 1, 2), false };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(2, 6, 1, 1), false };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(2, 6, 1, 0), true };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(2, 6, 0, 5), false };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(2, 6, 0, 4), false };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(2, 6, 0, 3), false };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(2, 6, 0, 2), false };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(2, 6, 0, 1), false };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(2, 6, 0, 0), true };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(2, 6, 4, 4), false };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(2, 6, 3, 5), false };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(2, 6, 4, 5), false };
        
        // Normal - from bottom right.
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(4, 6, 3, 5), false };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(4, 6, 3, 4), false };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(4, 6, 3, 3), false };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(4, 6, 3, 2), false };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(4, 6, 3, 1), false };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(4, 6, 3, 0), false };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(4, 6, 2, 5), false };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(4, 6, 2, 4), false };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(4, 6, 2, 3), false };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(4, 6, 2, 2), false };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(4, 6, 2, 1), false };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(4, 6, 2, 0), false };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(4, 6, 1, 5), false };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(4, 6, 1, 4), false };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(4, 6, 1, 3), false };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(4, 6, 1, 2), false };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(4, 6, 1, 1), false };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(4, 6, 1, 0), false };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(4, 6, 0, 5), false };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(4, 6, 0, 4), false };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(4, 6, 0, 3), false };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(4, 6, 0, 2), false };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(4, 6, 0, 1), false };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(4, 6, 0, 0), true };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(4, 6, 4, 4), false };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(4, 6, 3, 5), false };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(4, 6, 4, 5), false };
        
        // Normal - from middle right.
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(4, 3, 3, 5), false };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(4, 3, 3, 4), false };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(4, 3, 3, 3), false };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(4, 3, 3, 2), false };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(4, 3, 3, 1), false };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(4, 3, 3, 0), false };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(4, 3, 2, 5), false };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(4, 3, 2, 4), false };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(4, 3, 2, 3), false };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(4, 3, 2, 2), false };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(4, 3, 2, 1), false };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(4, 3, 2, 0), false };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(4, 3, 1, 5), false };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(4, 3, 1, 4), false };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(4, 3, 1, 3), false };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(4, 3, 1, 2), false };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(4, 3, 1, 1), false };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(4, 3, 1, 0), false };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(4, 3, 0, 5), false };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(4, 3, 0, 4), false };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(4, 3, 0, 3), true };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(4, 3, 0, 2), true };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(4, 3, 0, 1), true };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(4, 3, 0, 0), true };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(4, 3, 4, 4), false };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(4, 3, 3, 5), false };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(4, 3, 4, 5), false };
        
        // Normal - from top right.
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(1, 6, 3, 5), false };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(1, 6, 3, 4), false };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(1, 6, 3, 3), false };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(1, 6, 3, 2), false };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(1, 6, 3, 1), false };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(1, 6, 3, 0), true };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(1, 6, 2, 5), false };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(1, 6, 2, 4), false };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(1, 6, 2, 3), false };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(1, 6, 2, 2), false };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(1, 6, 2, 1), false };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(1, 6, 2, 0), true };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(1, 6, 1, 5), false };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(1, 6, 1, 4), false };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(1, 6, 1, 3), false };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(1, 6, 1, 2), false };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(1, 6, 1, 1), false };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(1, 6, 1, 0), true };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(1, 6, 0, 5), false };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(1, 6, 0, 4), false };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(1, 6, 0, 3), false };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(1, 6, 0, 2), false };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(1, 6, 0, 1), false };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(1, 6, 0, 0), true };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(1, 6, 4, 4), false };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(1, 6, 3, 5), false };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(1, 6, 4, 5), false };

        // Normal - from middle.
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(2, 4, 3, 5), false };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(2, 4, 3, 4), false };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(2, 4, 3, 3), false };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(2, 4, 3, 2), false };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(2, 4, 3, 1), false };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(2, 4, 3, 0), false };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(2, 4, 2, 5), false };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(2, 4, 2, 4), false };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(2, 4, 2, 3), false };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(2, 4, 2, 2), true };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(2, 4, 2, 1), true };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(2, 4, 2, 0), true };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(2, 4, 1, 5), false };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(2, 4, 1, 4), false };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(2, 4, 1, 3), false };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(2, 4, 1, 2), true };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(2, 4, 1, 1), true };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(2, 4, 1, 0), true };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(2, 4, 0, 5), false };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(2, 4, 0, 4), false };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(2, 4, 0, 3), false };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(2, 4, 0, 2), true };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(2, 4, 0, 1), true };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(2, 4, 0, 0), true };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(2, 4, 4, 4), false };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(2, 4, 3, 5), false };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(2, 4, 4, 5), false };

        // Normal - from outside top left.
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(0, 0, 2, 3), false };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(0, 0, 1, 1), false };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(0, 0, 0.5, 0.5), false };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(0, 0, 0, 1), false };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(0, 0, 1, 0), false };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(0, 0, 0, 0), false };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(0, 1, 2, 3), false };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(0, 1, 1, 1), false };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(0, 1, 0, 3), false };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(0, 1, 2, 1), false };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(0, 1, 0, 0), false };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(1, 0, 2, 3), false };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(1, 0, 1, 1), false };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(1, 0, 1, 0), false };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(1, 0, 0, 1), false };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(1, 0, 0, 0), false };

        // Normal - from outside middle left.
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(0, 4, 2, 3), false };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(0, 4, 1, 1), false };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(0, 4, 0.5, 0.5), false };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(0, 4, 0, 1), false };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(0, 4, 1, 0), false };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(0, 4, 0, 0), false };

        // Normal - from outside bottom left.
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(0, 6, 2, 3), false };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(0, 6, 1, 1), false };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(0, 6, 0.5, 0.5), false };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(0, 6, 0, 1), false };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(0, 6, 1, 0), false };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(0, 6, 0, 0), false };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(0, 7, 2, 3), false };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(0, 7, 1, 1), false };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(0, 7, 0.5, 0.5), false };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(0, 7, 0, 1), false };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(0, 7, 1, 0), false };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(0, 7, 0, 0), false };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(1, 7, 2, 3), false };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(1, 7, 1, 1), false };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(1, 7, 0.5, 0.5), false };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(1, 7, 0, 1), false };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(1, 7, 1, 0), false };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(1, 7, 0, 0), false };

        // Normal - from outside bottom middle.
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(2, 7, 2, 3), false };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(2, 7, 1, 1), false };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(2, 7, 0.5, 0.5), false };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(2, 7, 0, 1), false };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(2, 7, 0, 1), false };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(2, 7, 0, 0), false };

        // Normal - from outside bottom right.
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(4, 7, 2, 3), false };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(4, 7, 1, 1), false };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(4, 7, 0.5, 0.5), false };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(4, 7, 1, 0), false };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(4, 7, 0, 1), false };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(4, 7, 0, 0), false };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(5, 7, 2, 3), false };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(5, 7, 1, 1), false };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(5, 7, 0.5, 0.5), false };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(5, 7, 0, 1), false };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(5, 7, 1, 0), false };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(5, 7, 0, 0), false };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(5, 5, 2, 3), false };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(5, 5, 1, 1), false };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(5, 5, 0.5, 0.5), false };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(5, 5, 0, 1), false };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(5, 5, 1, 0), false };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(5, 5, 0, 0), false };

        // From outside middle right.
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(5, 3, 2, 3), false };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(5, 3, 1, 1), false };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(5, 3, 0.5, 0.5), false };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(5, 3, 0, 1), false };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(5, 3, 1, 0), false };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(5, 3, 0, 0), false };

        // From outside top right.
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(5, 1, 2, 3), false };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(5, 1, 1, 1), false };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(5, 1, 0.5, 0.5), false };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(5, 1, 0, 1), false };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(5, 1, 1, 0), false };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(5, 1, 0, 0), false };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(5, 0, 2, 3), false };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(5, 0, 1, 1), false };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(5, 0, 0.5, 0.5), false };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(5, 0, 0, 1), false };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(5, 0, 1, 0), false };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(5, 0, 0, 0), false };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(4, 0, 2, 3), false };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(4, 0, 1, 1), false };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(4, 0, 0.5, 0.5), false };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(4, 0, 0, 0), false };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(4, 0, 1, 0), false };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(4, 0, 0, 0), false };

        // Normal - empty.
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(0, 0, 0, 0), false };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(), false };
        yield return new object[] { new Rect(1, 2, 3, 4), Rect.Empty, false };

        // Empty width.
        yield return new object[] { new Rect(1, 2, 0, 4), new Rect(1, 2, 1, 5), false };
        yield return new object[] { new Rect(1, 2, 0, 4), new Rect(1, 2, 1, 4), false };
        yield return new object[] { new Rect(1, 2, 0, 4), new Rect(1, 2, 1, 3), false };
        yield return new object[] { new Rect(1, 2, 0, 4), new Rect(1, 2, 1, 2), false };
        yield return new object[] { new Rect(1, 2, 0, 4), new Rect(1, 2, 1, 1), false };
        yield return new object[] { new Rect(1, 2, 0, 4), new Rect(1, 2, 1, 0), false };
        yield return new object[] { new Rect(1, 2, 0, 4), new Rect(1, 2, 0, 5), false };
        yield return new object[] { new Rect(1, 2, 0, 4), new Rect(1, 2, 0, 4), true };
        yield return new object[] { new Rect(1, 2, 0, 4), new Rect(1, 2, 0, 3), true };
        yield return new object[] { new Rect(1, 2, 0, 4), new Rect(1, 2, 0, 2), true };
        yield return new object[] { new Rect(1, 2, 0, 4), new Rect(1, 2, 0, 1), true };
        yield return new object[] { new Rect(1, 2, 0, 4), new Rect(1, 2, 0, 0), true };
        yield return new object[] { new Rect(1, 2, 0, 4), new Rect(1, 1, 0, 0), false };
        yield return new object[] { new Rect(1, 2, 0, 4), new Rect(0, 1, 0, 0), false };
        yield return new object[] { new Rect(1, 2, 0, 4), new Rect(0, 0, 0, 1), false };
        yield return new object[] { new Rect(1, 2, 0, 4), new Rect(0, 0, 0, 0), false };
        yield return new object[] { new Rect(1, 2, 0, 4), new Rect(), false };
        yield return new object[] { new Rect(1, 2, 0, 4), Rect.Empty, false };

        // Empty height.
        yield return new object[] { new Rect(1, 2, 3, 0), new Rect(1, 2, 4, 1), false };
        yield return new object[] { new Rect(1, 2, 3, 0), new Rect(1, 2, 4, 0), false };
        yield return new object[] { new Rect(1, 2, 3, 0), new Rect(1, 2, 3, 1), false };
        yield return new object[] { new Rect(1, 2, 3, 0), new Rect(1, 2, 3, 0), true };
        yield return new object[] { new Rect(1, 2, 3, 0), new Rect(1, 2, 2, 1), false };
        yield return new object[] { new Rect(1, 2, 3, 0), new Rect(1, 2, 2, 0), true };
        yield return new object[] { new Rect(1, 2, 3, 0), new Rect(1, 2, 1, 1), false };
        yield return new object[] { new Rect(1, 2, 3, 0), new Rect(1, 2, 1, 0), true };
        yield return new object[] { new Rect(1, 2, 3, 0), new Rect(1, 2, 0, 0), true };
        yield return new object[] { new Rect(1, 2, 3, 0), new Rect(1, 0, 0, 0), false };
        yield return new object[] { new Rect(1, 2, 3, 0), new Rect(0, 1, 0, 0), false };
        yield return new object[] { new Rect(1, 2, 3, 0), new Rect(0, 0, 0, 0), false };
        yield return new object[] { new Rect(1, 2, 3, 0), new Rect(), false };
        yield return new object[] { new Rect(1, 2, 3, 0), Rect.Empty, false };
        
        // Zero.
        yield return new object[] { new Rect(0, 0, 0, 0), new Rect(), true };
        yield return new object[] { new Rect(0, 0, 0, 0), new Rect(0, 0, 0, 0), true };
        yield return new object[] { new Rect(0, 0, 0, 0), new Rect(1, 0, 0, 0), false };
        yield return new object[] { new Rect(0, 0, 0, 0), new Rect(0, 1, 0, 0), false };
        yield return new object[] { new Rect(0, 0, 0, 0), new Rect(0, 0, 1, 0), false };
        yield return new object[] { new Rect(0, 0, 0, 0), new Rect(0, 0, 0, 1), false };
        yield return new object[] { new Rect(0, 0, 0, 0), new Rect(0, 0, 1, 1), false };
        yield return new object[] { new Rect(0, 0, 0, 0), new Rect(1, 2, 3, 4), false };
        yield return new object[] { new Rect(0, 0, 0, 0), new Rect(1, 2, 3, 0), false };
        yield return new object[] { new Rect(0, 0, 0, 0), new Rect(1, 2, 0, 4), false };
        yield return new object[] { new Rect(0, 0, 0, 0), new Rect(1, 2, 0, 0), false };
        yield return new object[] { new Rect(0, 0, 0, 0), Rect.Empty, false };
        
        // Default.
        yield return new object[] { new Rect(), new Rect(), true };
        yield return new object[] { new Rect(), new Rect(0, 0, 0, 0), true };
        yield return new object[] { new Rect(), new Rect(1, 0, 0, 0), false };
        yield return new object[] { new Rect(), new Rect(0, 1, 0, 0), false };
        yield return new object[] { new Rect(), new Rect(0, 0, 1, 0), false };
        yield return new object[] { new Rect(), new Rect(0, 0, 0, 1), false };
        yield return new object[] { new Rect(), new Rect(0, 0, 1, 1), false };
        yield return new object[] { new Rect(), new Rect(1, 2, 3, 4), false };
        yield return new object[] { new Rect(), new Rect(1, 2, 3, 0), false };
        yield return new object[] { new Rect(), new Rect(1, 2, 0, 4), false };
        yield return new object[] { new Rect(), new Rect(1, 2, 0, 0), false };
        yield return new object[] { new Rect(), Rect.Empty, false };

        // Empty.
        yield return new object[] { Rect.Empty, Rect.Empty, false };
        yield return new object[] { Rect.Empty, new Rect(), false };
        yield return new object[] { Rect.Empty, new Rect(0, 0, 0, 0), false };
        yield return new object[] { Rect.Empty, new Rect(1, 0, 0, 0), false };
        yield return new object[] { Rect.Empty, new Rect(0, 1, 0, 0), false };
        yield return new object[] { Rect.Empty, new Rect(0, 0, 1, 0), false };
        yield return new object[] { Rect.Empty, new Rect(0, 0, 0, 1), false };
        yield return new object[] { Rect.Empty, new Rect(1, 2, 3, 4), false };
        yield return new object[] { Rect.Empty, new Rect(1, 2, 3, 0), false };
        yield return new object[] { Rect.Empty, new Rect(1, 2, 0, 4), false };
        yield return new object[] { Rect.Empty, new Rect(1, 2, 0, 0), false };
    }

    [Theory]
    [MemberData(nameof(Contains_Rect_TestData))]
    public void Contains_InvokeRect_ReturnsExpected(Rect rect, Rect other, bool expected)
    {
        Assert.Equal(expected, rect.Contains(other));
    }

    public static IEnumerable<object?[]> Equals_TestData()
    {
        // Normal.
        yield return new object?[] { new Rect(1, 2, 3, 4), new Rect(1, 2, 3, 4), true, true };
        yield return new object?[] { new Rect(1, 2, 3, 4), new Rect(2, 2, 3, 4), false, false };
        yield return new object?[] { new Rect(1, 2, 3, 4), new Rect(1, 3, 3, 4), false, false };
        yield return new object?[] { new Rect(1, 2, 3, 4), new Rect(1, 2, 4, 4), false, false };
        yield return new object?[] { new Rect(1, 2, 3, 4), new Rect(1, 2, 3, 5), false, false };
        yield return new object?[] { new Rect(1, 2, 3, 4), new Rect(double.NaN, 2, 3, 4), false, false };
        yield return new object?[] { new Rect(1, 2, 3, 4), new Rect(1, double.NaN, 3, 4), false, false };
        yield return new object?[] { new Rect(1, 2, 3, 4), new Rect(1, 2, double.NaN, 4), false, false };
        yield return new object?[] { new Rect(1, 2, 3, 4), new Rect(1, 2, 3, double.NaN), false, false };
        yield return new object?[] { new Rect(1, 2, 3, 4), new Rect(0, 0, 0, 0), false, false };
        yield return new object?[] { new Rect(1, 2, 3, 4), new Rect(), false, false };
        yield return new object?[] { new Rect(1, 2, 3, 4), Rect.Empty, false, false };

        // NaN x.
        yield return new object?[] { new Rect(double.NaN, 2, 3, 4), new Rect(double.NaN, 2, 3, 4), true, true };
        yield return new object?[] { new Rect(double.NaN, 2, 3, 4), new Rect(2, 2, 3, 4), false, false };
        yield return new object?[] { new Rect(double.NaN, 2, 3, 4), new Rect(double.NaN, double.NaN, 3, 4), false, false };
        yield return new object?[] { new Rect(double.NaN, 2, 3, 4), new Rect(double.NaN, 2, double.NaN, 4), false, false };
        yield return new object?[] { new Rect(double.NaN, 2, 3, 4), new Rect(double.NaN, 2, 3, double.NaN), false, false };
        
        // NaN y.
        yield return new object?[] { new Rect(1, double.NaN, 3, 4), new Rect(1, double.NaN, 3, 4), true, true };
        yield return new object?[] { new Rect(1, double.NaN, 3, 4), new Rect(1, 3, 3, 4), false, false };
        yield return new object?[] { new Rect(1, double.NaN, 3, 4), new Rect(double.NaN, double.NaN, 3, 4), false, false };
        yield return new object?[] { new Rect(1, double.NaN, 3, 4), new Rect(1, double.NaN, double.NaN, 4), false, false };
        yield return new object?[] { new Rect(1, double.NaN, 3, 4), new Rect(1, double.NaN, 3, double.NaN), false, false };

        // NaN width.
        yield return new object?[] { new Rect(1, 2, double.NaN, 4), new Rect(1, 2, double.NaN, 4), true, true };
        yield return new object?[] { new Rect(1, 2, double.NaN, 4), new Rect(1, 2, 4, 4), false, false };
        yield return new object?[] { new Rect(1, 2, double.NaN, 4), new Rect(double.NaN, 2, double.NaN, 4), false, false };
        yield return new object?[] { new Rect(1, 2, double.NaN, 4), new Rect(1, double.NaN, double.NaN, 4), false, false };
        yield return new object?[] { new Rect(1, 2, double.NaN, 4), new Rect(1, 2, double.NaN, double.NaN), false, false };

        // NaN height.
        yield return new object?[] { new Rect(1, 2, 3, double.NaN), new Rect(1, 2, 3, double.NaN), true, true };
        yield return new object?[] { new Rect(1, 2, 3, double.NaN), new Rect(1, 2, 3, 4), false, false };
        yield return new object?[] { new Rect(1, 2, 3, double.NaN), new Rect(double.NaN, 2, 3, double.NaN), false, false };
        yield return new object?[] { new Rect(1, 2, 3, double.NaN), new Rect(1, double.NaN, 3, double.NaN), false, false };
        yield return new object?[] { new Rect(1, 2, 3, double.NaN), new Rect(1, 2, double.NaN, double.NaN), false, false };

        // NaN x, y, width, height.
        yield return new object?[] { new Rect(double.NaN, double.NaN, double.NaN, double.NaN), new Rect(double.NaN, double.NaN, double.NaN, double.NaN), true, true };
        yield return new object?[] { new Rect(double.NaN, double.NaN, double.NaN, double.NaN), new Rect(1, 2, 3, 4), false, false };
        yield return new object?[] { new Rect(double.NaN, double.NaN, double.NaN, double.NaN), new Rect(1, double.NaN, double.NaN, double.NaN), false, false };
        yield return new object?[] { new Rect(double.NaN, double.NaN, double.NaN, double.NaN), new Rect(double.NaN, 2, double.NaN, double.NaN), false, false };
        yield return new object?[] { new Rect(double.NaN, double.NaN, double.NaN, double.NaN), new Rect(double.NaN, double.NaN, 3, double.NaN), false, false };
        yield return new object?[] { new Rect(double.NaN, double.NaN, double.NaN, double.NaN), new Rect(double.NaN, double.NaN, double.NaN, 4), false, false };

        // Zero.
        yield return new object?[] { new Rect(0, 0, 0, 0), new Rect(), true, true };
        yield return new object?[] { new Rect(0, 0, 0, 0), new Rect(0, 0, 0, 0), true, true };
        yield return new object?[] { new Rect(0, 0, 0, 0), new Rect(1, 0, 0, 0), false, false };
        yield return new object?[] { new Rect(0, 0, 0, 0), new Rect(0, 1, 0, 0), false, false };
        yield return new object?[] { new Rect(0, 0, 0, 0), new Rect(0, 0, 1, 0), false, false };
        yield return new object?[] { new Rect(0, 0, 0, 0), new Rect(0, 0, 0, 1), false, false };
        yield return new object?[] { new Rect(0, 0, 0, 0), Rect.Empty, false, true };
        
        // Default.
        yield return new object?[] { new Rect(), new Rect(), true, true };
        yield return new object?[] { new Rect(), new Rect(0, 0, 0, 0), true, true };
        yield return new object?[] { new Rect(), new Rect(1, 0, 0, 0), false, false };
        yield return new object?[] { new Rect(), new Rect(0, 1, 0, 0), false, false };
        yield return new object?[] { new Rect(), new Rect(0, 0, 1, 0), false, false };
        yield return new object?[] { new Rect(), new Rect(0, 0, 0, 1), false, false };
        yield return new object?[] { new Rect(), Rect.Empty, false, true };

        // Empty.
        yield return new object?[] { Rect.Empty, Rect.Empty, true, true };
        yield return new object?[] { Rect.Empty, new Rect(1, 2, 3, 4), false, false };
        yield return new object?[] { Rect.Empty, new Rect(0, 0, 0, 0), false, true };
        yield return new object?[] { Rect.Empty, new Rect(), false, true };

        // Other.
        yield return new object?[] { Rect.Empty, new object(), false, false };
        yield return new object?[] { Rect.Empty, null, false, false };
        yield return new object?[] { new Rect(), new object(), false, false };
        yield return new object?[] { new Rect(), null, false, false };
        yield return new object?[] { new Rect(0, 0, 0, 0), new object(), false, false };
        yield return new object?[] { new Rect(0, 0, 0, 0), null, false, false };
        yield return new object?[] { new Rect(1, 2, 3, 4), new object(), false, false };
        yield return new object?[] { new Rect(1, 2, 3, 4), null, false, false };
    }

    [Theory]
    [MemberData(nameof(Equals_TestData))]
    public void Equals_Object_ReturnsExpected(Rect rect, object o, bool expected, bool expectedHashCode)
    {
        Assert.Equal(expected, rect.Equals(o));
        if (o is Rect value)
        {
            Assert.Equal(expected, rect.Equals(value));
            Assert.Equal(expected, value.Equals(rect));
            Assert.Equal(expected, Rect.Equals(rect, value));
            Assert.Equal(expected, Rect.Equals(value, rect));
            Assert.Equal(expectedHashCode, rect.GetHashCode().Equals(value.GetHashCode()));
        }
    }

    public static IEnumerable<object?[]> EqualityOperator_TestData()
    {
        // Normal.
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(1, 2, 3, 4), true };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(2, 2, 3, 4), false };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(1, 3, 3, 4), false };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(1, 2, 4, 4), false };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(1, 2, 3, 5), false };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(double.NaN, 2, 3, 4), false };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(1, double.NaN, 3, 4), false };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(1, 2, double.NaN, 4), false };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(1, 2, 3, double.NaN), false };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(0, 0, 0, 0), false };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(), false };
        yield return new object[] { new Rect(1, 2, 3, 4), Rect.Empty, false };

        // NaN x.
        yield return new object[] { new Rect(double.NaN, 2, 3, 4), new Rect(double.NaN, 2, 3, 4), false };
        yield return new object[] { new Rect(double.NaN, 2, 3, 4), new Rect(2, 2, 3, 4), false };
        yield return new object[] { new Rect(double.NaN, 2, 3, 4), new Rect(double.NaN, double.NaN, 3, 4), false };
        yield return new object[] { new Rect(double.NaN, 2, 3, 4), new Rect(double.NaN, 2, double.NaN, 4), false };
        yield return new object[] { new Rect(double.NaN, 2, 3, 4), new Rect(double.NaN, 2, 3, double.NaN), false };
        
        // NaN y.
        yield return new object[] { new Rect(1, double.NaN, 3, 4), new Rect(1, double.NaN, 3, 4), false };
        yield return new object[] { new Rect(1, double.NaN, 3, 4), new Rect(1, 3, 3, 4), false };
        yield return new object[] { new Rect(1, double.NaN, 3, 4), new Rect(double.NaN, double.NaN, 3, 4), false };
        yield return new object[] { new Rect(1, double.NaN, 3, 4), new Rect(1, double.NaN, double.NaN, 4), false };
        yield return new object[] { new Rect(1, double.NaN, 3, 4), new Rect(1, double.NaN, 3, double.NaN), false };

        // NaN width.
        yield return new object[] { new Rect(1, 2, double.NaN, 4), new Rect(1, 2, double.NaN, 4), false };
        yield return new object[] { new Rect(1, 2, double.NaN, 4), new Rect(1, 2, 4, 4), false };
        yield return new object[] { new Rect(1, 2, double.NaN, 4), new Rect(double.NaN, 2, double.NaN, 4), false };
        yield return new object[] { new Rect(1, 2, double.NaN, 4), new Rect(1, double.NaN, double.NaN, 4), false };
        yield return new object[] { new Rect(1, 2, double.NaN, 4), new Rect(1, 2, double.NaN, double.NaN), false };

        // NaN height.
        yield return new object[] { new Rect(1, 2, 3, double.NaN), new Rect(1, 2, 3, double.NaN), false };
        yield return new object[] { new Rect(1, 2, 3, double.NaN), new Rect(1, 2, 3, 4), false };
        yield return new object[] { new Rect(1, 2, 3, double.NaN), new Rect(double.NaN, 2, 3, double.NaN), false };
        yield return new object[] { new Rect(1, 2, 3, double.NaN), new Rect(1, double.NaN, 3, double.NaN), false };
        yield return new object[] { new Rect(1, 2, 3, double.NaN), new Rect(1, 2, double.NaN, double.NaN), false };

        // NaN x, y, width, height.
        yield return new object[] { new Rect(double.NaN, double.NaN, double.NaN, double.NaN), new Rect(double.NaN, double.NaN, double.NaN, double.NaN), false };
        yield return new object[] { new Rect(double.NaN, double.NaN, double.NaN, double.NaN), new Rect(1, 2, 3, 4), false };
        yield return new object[] { new Rect(double.NaN, double.NaN, double.NaN, double.NaN), new Rect(1, double.NaN, double.NaN, double.NaN), false };
        yield return new object[] { new Rect(double.NaN, double.NaN, double.NaN, double.NaN), new Rect(double.NaN, 2, double.NaN, double.NaN), false };
        yield return new object[] { new Rect(double.NaN, double.NaN, double.NaN, double.NaN), new Rect(double.NaN, double.NaN, 3, double.NaN), false };
        yield return new object[] { new Rect(double.NaN, double.NaN, double.NaN, double.NaN), new Rect(double.NaN, double.NaN, double.NaN, 4), false };

        // Zero.
        yield return new object[] { new Rect(0, 0, 0, 0), new Rect(), true };
        yield return new object[] { new Rect(0, 0, 0, 0), new Rect(0, 0, 0, 0), true };
        yield return new object[] { new Rect(0, 0, 0, 0), new Rect(1, 0, 0, 0), false };
        yield return new object[] { new Rect(0, 0, 0, 0), new Rect(0, 1, 0, 0), false };
        yield return new object[] { new Rect(0, 0, 0, 0), new Rect(0, 0, 1, 0), false };
        yield return new object[] { new Rect(0, 0, 0, 0), new Rect(0, 0, 0, 1), false };
        yield return new object[] { new Rect(0, 0, 0, 0), Rect.Empty, false };
        
        // Default.
        yield return new object[] { new Rect(), new Rect(), true };
        yield return new object[] { new Rect(), new Rect(0, 0, 0, 0), true };
        yield return new object[] { new Rect(), new Rect(1, 0, 0, 0), false };
        yield return new object[] { new Rect(), new Rect(0, 1, 0, 0), false };
        yield return new object[] { new Rect(), new Rect(0, 0, 1, 0), false };
        yield return new object[] { new Rect(), new Rect(0, 0, 0, 1), false };
        yield return new object[] { new Rect(), Rect.Empty, false };

        // Empty.
        yield return new object[] { Rect.Empty, Rect.Empty, true };
        yield return new object[] { Rect.Empty, new Rect(1, 2, 3, 4), false };
        yield return new object[] { Rect.Empty, new Rect(0, 0, 0, 0), false };
        yield return new object[] { Rect.Empty, new Rect(), false };
    }

    [Theory]
    [MemberData(nameof(EqualityOperator_TestData))]
    public void EqualityOperator_Invoke_ReturnsExpected(Rect rect1, Rect rect2, bool expected)
    {
        Assert.Equal(expected, rect1 == rect2);
        Assert.Equal(expected, rect2 == rect1);
        Assert.Equal(!expected, rect1 != rect2);
        Assert.Equal(!expected, rect2 != rect1);
    }

    [Fact]
    public void GetHashCode_InvokeDefault_ReturnsEqual()
    {
        var rect = new Rect();
        Assert.Equal(0, rect.GetHashCode());
        Assert.Equal(rect.GetHashCode(), rect.GetHashCode());
    }

    [Fact]
    public void GetHashCode_InvokeEmpty_ReturnsEqual()
    {
        Rect rect = Rect.Empty;
        Assert.Equal(0, rect.GetHashCode());
        Assert.Equal(rect.GetHashCode(), rect.GetHashCode());
    }

    [Fact]
    public void GetHashCode_InvokeNormal_ReturnsEqual()
    {
        var rect = new Rect(1, 2, 3, 4);
        Assert.NotEqual(0, rect.GetHashCode());
        Assert.Equal(rect.GetHashCode(), rect.GetHashCode());
    }

    public static IEnumerable<object[]> Inflate_TestData()
    {
        // Normal.
        yield return new object[] { new Rect(1, 2, 3, 4), 1, 2, new Rect(0, 0, 5, 8) };
        yield return new object[] { new Rect(1, 2, 3, 4), 1.5, 2.5, new Rect(-0.5, -0.5, 6, 9) };
        yield return new object[] { new Rect(1, 2, 3, 4), 0.5, 0.25, new Rect(0.5,1.75,4,4.5) };
        yield return new object[] { new Rect(1, 2, 3, 4), 0, 0, new Rect(1, 2, 3, 4) };
        yield return new object[] { new Rect(1, 2, 3, 4), -1, 0, new Rect(2, 2, 1, 4) };
        yield return new object[] { new Rect(1, 2, 3, 4), -1, 2, new Rect(2, 0, 1, 8) };
        yield return new object[] { new Rect(1, 2, 3, 4), 0, -2, new Rect(1, 4, 3, 0) };
        yield return new object[] { new Rect(1, 2, 3, 4), 2, -2, new Rect(-1, 4, 7, 0) };

        // Large.
        yield return new object[] { new Rect(double.MaxValue, double.MaxValue, 3, 4), 1, 2, new Rect(double.MaxValue, double.MaxValue, 5, 8) };
        yield return new object[] { new Rect(double.MaxValue, double.MaxValue, 3, 4), 0, 0, new Rect(double.MaxValue, double.MaxValue, 3, 4) };
        yield return new object[] { new Rect(double.MaxValue, double.MaxValue, 3, 4), double.MaxValue, double.MaxValue, new Rect(0, 0, double.PositiveInfinity, double.PositiveInfinity) };
        yield return new object[] { new Rect(double.MinValue, double.MinValue, 3, 4), -1, -2, new Rect(double.MinValue, double.MinValue, 1, 0) };
        yield return new object[] { new Rect(double.MinValue, double.MinValue, 3, 4), 0, 0, new Rect(double.MinValue, double.MinValue, 3, 4) };
        yield return new object[] { new Rect(double.MinValue, double.MinValue, 3, 4), double.MinValue, double.MinValue, Rect.Empty };
        yield return new object[] { new Rect(1, 2, 3, 4), double.MaxValue, double.MaxValue, new Rect(double.MinValue, double.MinValue, double.PositiveInfinity, double.PositiveInfinity) };
        yield return new object[] { new Rect(0, 0, 3, 4), double.MaxValue, double.MaxValue, new Rect(double.MinValue, double.MinValue, double.PositiveInfinity, double.PositiveInfinity) };
        yield return new object[] { new Rect(-1, -2, 3, 4), double.MinValue, double.MinValue, Rect.Empty };
        yield return new object[] { new Rect(0, 0, 3, 4), double.MinValue, double.MinValue, Rect.Empty };

        // Infinite.
        yield return new object[] { new Rect(double.NegativeInfinity, double.NegativeInfinity, double.PositiveInfinity, double.PositiveInfinity), 0, 0, new Rect(double.NegativeInfinity, double.NegativeInfinity, double.PositiveInfinity, double.PositiveInfinity) };
        yield return new object[] { new Rect(double.NegativeInfinity, double.NegativeInfinity, double.PositiveInfinity, double.PositiveInfinity), 5, 6, new Rect(double.NegativeInfinity, double.NegativeInfinity, double.PositiveInfinity, double.PositiveInfinity) };
        yield return new object[] { new Rect(double.NegativeInfinity, double.NegativeInfinity, double.PositiveInfinity, double.PositiveInfinity), -5, -6, new Rect(double.NegativeInfinity, double.NegativeInfinity, double.PositiveInfinity, double.PositiveInfinity) };
        yield return new object[] { new Rect(double.NegativeInfinity, double.NegativeInfinity, double.PositiveInfinity, double.PositiveInfinity), double.PositiveInfinity, double.PositiveInfinity, new Rect(double.NegativeInfinity, double.NegativeInfinity, double.PositiveInfinity, double.PositiveInfinity) };
        yield return new object[] { new Rect(double.NegativeInfinity, double.NegativeInfinity, double.PositiveInfinity, double.PositiveInfinity), double.NegativeInfinity, double.NegativeInfinity, Rect.Empty };
        yield return new object[] { new Rect(1, 2, double.PositiveInfinity, double.PositiveInfinity), 0, 0, new Rect(1, 2, double.PositiveInfinity, double.PositiveInfinity) };
        yield return new object[] { new Rect(1, 2, double.PositiveInfinity, double.PositiveInfinity), 5, 6, new Rect(-4, -4, double.PositiveInfinity, double.PositiveInfinity) };
        yield return new object[] { new Rect(1, 2, double.PositiveInfinity, double.PositiveInfinity), -5, -6, new Rect(6 ,8, double.PositiveInfinity, double.PositiveInfinity) };
        yield return new object[] { new Rect(1, 2, double.PositiveInfinity, double.PositiveInfinity), double.PositiveInfinity, double.PositiveInfinity, new Rect(double.NegativeInfinity, double.NegativeInfinity, double.PositiveInfinity, double.PositiveInfinity) };
        yield return new object[] { new Rect(1, 2, double.PositiveInfinity, double.PositiveInfinity), double.NegativeInfinity, double.NegativeInfinity, Rect.Empty };

        // NaN.
        yield return new object[] { new Rect(double.NaN, double.NaN, double.NaN, double.NaN), 0, 0, Rect.Empty };
        yield return new object[] { new Rect(double.NaN, double.NaN, double.NaN, double.NaN), 5, 6, Rect.Empty };
        yield return new object[] { new Rect(double.NaN, double.NaN, double.NaN, double.NaN), -5, -6, Rect.Empty };
        yield return new object[] { new Rect(double.NaN, double.NaN, double.NaN, double.NaN), double.PositiveInfinity, double.PositiveInfinity, Rect.Empty };
        yield return new object[] { new Rect(double.NaN, double.NaN, double.NaN, double.NaN), double.NegativeInfinity, double.NegativeInfinity, Rect.Empty };
        yield return new object[] { new Rect(double.NaN, double.NaN, double.NaN, double.NaN), double.NaN, double.NaN, Rect.Empty };
        yield return new object[] { new Rect(1, 2, double.NaN, double.NaN), 0, 0, Rect.Empty };
        yield return new object[] { new Rect(1, 2, double.NaN, double.NaN), 5, 6, Rect.Empty };
        yield return new object[] { new Rect(1, 2, double.NaN, double.NaN), -5, -6, Rect.Empty };
        yield return new object[] { new Rect(1, 2, double.NaN, double.NaN), double.PositiveInfinity, double.PositiveInfinity, Rect.Empty };
        yield return new object[] { new Rect(1, 2, double.NaN, double.NaN), double.NegativeInfinity, double.NegativeInfinity, Rect.Empty };
        yield return new object[] { new Rect(1, 2, double.NaN, double.NaN), double.NaN, double.NaN, Rect.Empty };

        // Zero.
        yield return new object[] { new Rect(0, 0, 0, 0), 1, 2, new Rect(-1, -2, 2, 4) };
        yield return new object[] { new Rect(0, 0, 0, 0), -1, 0, Rect.Empty };
        yield return new object[] { new Rect(0, 0, 0, 0), 0, -2, Rect.Empty };
        yield return new object[] { new Rect(0, 0, 0, 0), -1, -2, Rect.Empty };
        yield return new object[] { new Rect(0, 0, 0, 0), 0, 0, new Rect() };

        // Default.
        yield return new object[] { new Rect(), 1, 2, new Rect(-1, -2, 2, 4) };
        yield return new object[] { new Rect(), -1, 0, Rect.Empty };
        yield return new object[] { new Rect(), 0, -2, Rect.Empty };
        yield return new object[] { new Rect(), -1, -2, Rect.Empty };
        yield return new object[] { new Rect(), 0, 0, new Rect() };
    }

    [Theory]
    [MemberData(nameof(Inflate_TestData))]
    public void Inflate_Invoke_ReturnsExpected(Rect rect, double width, double height, Rect expected)
    {
        Rect copy = rect;

        Assert.Equal(expected, Rect.Inflate(rect, width, height));
        Assert.Equal(copy, rect);

        if (width >= 0 && height >= 0)
        {
            Assert.Equal(expected, Rect.Inflate(rect, new Size(width, height)));
            Assert.Equal(copy, rect);
        }

        rect.Inflate(width, height);
        Assert.Equal(expected, rect);

        if (width >= 0 && height >= 0)
        {
            rect = copy;
            rect.Inflate(new Size(width, height));
            Assert.Equal(expected, rect);
        }
    }
    
    [Fact]
    public void Inflate_Empty_ThrowsInvalidOperationException()
    {
        Assert.Throws<InvalidOperationException>(() => Rect.Empty.Inflate(0, 0));
        Assert.Throws<InvalidOperationException>(() => Rect.Empty.Inflate(new Size(0, 0)));
        Assert.Throws<InvalidOperationException>(() => Rect.Inflate(Rect.Empty, 0, 0));
        Assert.Throws<InvalidOperationException>(() => Rect.Inflate(Rect.Empty, new Size(0, 0)));
    }

    public static IEnumerable<object[]> Intersect_Rect_TestData()
    {
        // Normal - from top left.
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(1, 2, 3, 5), new Rect(1, 2, 3, 4) };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(1, 2, 3, 4), new Rect(1, 2, 3, 4) };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(1, 2, 3, 3), new Rect(1, 2, 3, 3) };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(1, 2, 3, 2), new Rect(1, 2, 3, 2) };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(1, 2, 3, 1), new Rect(1, 2, 3, 1) };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(1, 2, 3, 0), new Rect(1, 2, 3, 0) };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(1, 2, 2, 5), new Rect(1, 2, 2, 4) };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(1, 2, 2, 4), new Rect(1, 2, 2, 4) };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(1, 2, 2, 3), new Rect(1, 2, 2, 3) };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(1, 2, 2, 2), new Rect(1, 2, 2, 2) };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(1, 2, 2, 1), new Rect(1, 2, 2, 1) };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(1, 2, 2, 0), new Rect(1, 2, 2, 0) };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(1, 2, 1, 5), new Rect(1, 2, 1, 4) };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(1, 2, 1, 4), new Rect(1, 2, 1, 4) };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(1, 2, 1, 3), new Rect(1, 2, 1, 3) };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(1, 2, 1, 2), new Rect(1, 2, 1, 2) };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(1, 2, 1, 1), new Rect(1, 2, 1, 1) };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(1, 2, 1, 0), new Rect(1, 2, 1, 0) };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(1, 2, 0, 5), new Rect(1, 2, 0, 4) };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(1, 2, 0, 4), new Rect(1, 2, 0, 4) };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(1, 2, 0, 3), new Rect(1, 2, 0, 3) };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(1, 2, 0, 2), new Rect(1, 2, 0, 2) };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(1, 2, 0, 1), new Rect(1, 2, 0, 1) };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(1, 2, 0, 0), new Rect(1, 2, 0, 0) };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(1, 2, 4, 4), new Rect(1, 2, 3, 4) };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(1, 2, 3, 5), new Rect(1, 2, 3, 4) };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(1, 2, 4, 5), new Rect(1, 2, 3, 4) };

        // Normal - from middle left.
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(1, 3, 3, 5), new Rect(1, 3, 3, 3) };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(1, 3, 3, 4), new Rect(1, 3, 3, 3) };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(1, 3, 3, 3), new Rect(1, 3, 3, 3) };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(1, 3, 3, 2), new Rect(1, 3, 3, 2) };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(1, 3, 3, 1), new Rect(1, 3, 3, 1) };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(1, 3, 3, 0), new Rect(1, 3, 3, 0) };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(1, 3, 2, 5), new Rect(1, 3, 2, 3) };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(1, 3, 2, 4), new Rect(1, 3, 2, 3) };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(1, 3, 2, 3), new Rect(1, 3, 2, 3) };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(1, 3, 2, 2), new Rect(1, 3, 2, 2) };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(1, 3, 2, 1), new Rect(1, 3, 2, 1) };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(1, 3, 2, 0), new Rect(1, 3, 2, 0) };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(1, 3, 1, 5), new Rect(1, 3, 1, 3) };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(1, 3, 1, 4), new Rect(1, 3, 1, 3) };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(1, 3, 1, 3), new Rect(1, 3, 1, 3) };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(1, 3, 1, 2), new Rect(1, 3, 1, 2) };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(1, 3, 1, 1), new Rect(1, 3, 1, 1) };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(1, 3, 1, 0), new Rect(1, 3, 1, 0) };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(1, 3, 0, 5), new Rect(1, 3, 0, 3) };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(1, 3, 0, 4), new Rect(1, 3, 0, 3) };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(1, 3, 0, 3), new Rect(1, 3, 0, 3) };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(1, 3, 0, 2), new Rect(1, 3, 0, 2) };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(1, 3, 0, 1), new Rect(1, 3, 0, 1) };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(1, 3, 0, 0), new Rect(1, 3, 0, 0) };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(1, 3, 4, 4), new Rect(1, 3, 3, 3) };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(1, 3, 3, 5), new Rect(1, 3, 3, 3) };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(1, 3, 4, 5), new Rect(1, 3, 3, 3) };

        // Normal - from bottom left.
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(1, 5, 3, 5), new Rect(1, 5, 3, 1) };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(1, 5, 3, 4), new Rect(1, 5, 3, 1) };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(1, 5, 3, 3), new Rect(1, 5, 3, 1) };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(1, 5, 3, 2), new Rect(1, 5, 3, 1) };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(1, 5, 3, 1), new Rect(1, 5, 3, 1) };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(1, 5, 3, 0), new Rect(1, 5, 3, 0) };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(1, 5, 2, 5), new Rect(1, 5, 2, 1) };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(1, 5, 2, 4), new Rect(1, 5, 2, 1) };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(1, 5, 2, 3), new Rect(1, 5, 2, 1) };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(1, 5, 2, 2), new Rect(1, 5, 2, 1) };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(1, 5, 2, 1), new Rect(1, 5, 2, 1) };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(1, 5, 2, 0), new Rect(1, 5, 2, 0) };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(1, 5, 1, 5), new Rect(1, 5, 1, 1) };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(1, 5, 1, 4), new Rect(1, 5, 1, 1) };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(1, 5, 1, 3), new Rect(1, 5, 1, 1) };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(1, 5, 1, 2), new Rect(1, 5, 1, 1) };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(1, 5, 1, 1), new Rect(1, 5, 1, 1) };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(1, 5, 1, 0), new Rect(1, 5, 1, 0) };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(1, 5, 0, 5), new Rect(1, 5, 0, 1) };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(1, 5, 0, 4), new Rect(1, 5, 0, 1) };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(1, 5, 0, 3), new Rect(1, 5, 0, 1) };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(1, 5, 0, 2), new Rect(1, 5, 0, 1) };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(1, 5, 0, 1), new Rect(1, 5, 0, 1) };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(1, 5, 0, 0), new Rect(1, 5, 0, 0) };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(1, 5, 4, 4), new Rect(1, 5, 3, 1) };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(1, 5, 3, 5), new Rect(1, 5, 3, 1) };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(1, 5, 4, 5), new Rect(1, 5, 3, 1) };

        // Normal - from bottom middle.
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(2, 6, 3, 5), new Rect(2, 6, 2, 0) };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(2, 6, 3, 4), new Rect(2, 6, 2, 0) };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(2, 6, 3, 3), new Rect(2, 6, 2, 0) };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(2, 6, 3, 2), new Rect(2, 6, 2, 0) };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(2, 6, 3, 1), new Rect(2, 6, 2, 0) };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(2, 6, 3, 0), new Rect(2, 6, 2, 0) };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(2, 6, 2, 5), new Rect(2, 6, 2, 0) };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(2, 6, 2, 4), new Rect(2, 6, 2, 0) };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(2, 6, 2, 3), new Rect(2, 6, 2, 0) };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(2, 6, 2, 2), new Rect(2, 6, 2, 0) };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(2, 6, 2, 1), new Rect(2, 6, 2, 0) };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(2, 6, 2, 0), new Rect(2, 6, 2, 0) };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(2, 6, 1, 5), new Rect(2, 6, 1, 0) };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(2, 6, 1, 4), new Rect(2, 6, 1, 0) };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(2, 6, 1, 3), new Rect(2, 6, 1, 0) };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(2, 6, 1, 2), new Rect(2, 6, 1, 0) };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(2, 6, 1, 1), new Rect(2, 6, 1, 0) };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(2, 6, 1, 0), new Rect(2, 6, 1, 0) };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(2, 6, 0, 5), new Rect(2, 6, 0, 0) };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(2, 6, 0, 4), new Rect(2, 6, 0, 0) };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(2, 6, 0, 3), new Rect(2, 6, 0, 0) };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(2, 6, 0, 2), new Rect(2, 6, 0, 0) };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(2, 6, 0, 1), new Rect(2, 6, 0, 0) };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(2, 6, 0, 0), new Rect(2, 6, 0, 0) };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(2, 6, 4, 4), new Rect(2, 6, 2, 0) };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(2, 6, 3, 5), new Rect(2, 6, 2, 0) };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(2, 6, 4, 5), new Rect(2, 6, 2, 0) };
  
        // Normal - from bottom right.
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(4, 6, 3, 5), new Rect(4, 6, 0, 0) };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(4, 6, 3, 4), new Rect(4, 6, 0, 0) };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(4, 6, 3, 3), new Rect(4, 6, 0, 0) };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(4, 6, 3, 2), new Rect(4, 6, 0, 0) };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(4, 6, 3, 1), new Rect(4, 6, 0, 0) };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(4, 6, 3, 0), new Rect(4, 6, 0, 0) };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(4, 6, 2, 5), new Rect(4, 6, 0, 0) };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(4, 6, 2, 4), new Rect(4, 6, 0, 0) };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(4, 6, 2, 3), new Rect(4, 6, 0, 0) };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(4, 6, 2, 2), new Rect(4, 6, 0, 0) };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(4, 6, 2, 1), new Rect(4, 6, 0, 0) };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(4, 6, 2, 0), new Rect(4, 6, 0, 0) };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(4, 6, 1, 5), new Rect(4, 6, 0, 0) };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(4, 6, 1, 4), new Rect(4, 6, 0, 0) };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(4, 6, 1, 3), new Rect(4, 6, 0, 0) };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(4, 6, 1, 2), new Rect(4, 6, 0, 0) };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(4, 6, 1, 1), new Rect(4, 6, 0, 0) };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(4, 6, 1, 0), new Rect(4, 6, 0, 0) };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(4, 6, 0, 5), new Rect(4, 6, 0, 0) };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(4, 6, 0, 4), new Rect(4, 6, 0, 0) };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(4, 6, 0, 3), new Rect(4, 6, 0, 0) };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(4, 6, 0, 2), new Rect(4, 6, 0, 0) };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(4, 6, 0, 1), new Rect(4, 6, 0, 0) };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(4, 6, 0, 0), new Rect(4, 6, 0, 0) };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(4, 6, 4, 4), new Rect(4, 6, 0, 0) };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(4, 6, 3, 5), new Rect(4, 6, 0, 0) };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(4, 6, 4, 5), new Rect(4, 6, 0, 0) };
        
        // Normal - from middle right.
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(4, 3, 3, 5), new Rect(4, 3, 0, 3) };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(4, 3, 3, 4), new Rect(4, 3, 0, 3) };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(4, 3, 3, 3), new Rect(4, 3, 0, 3) };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(4, 3, 3, 2), new Rect(4, 3, 0, 2) };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(4, 3, 3, 1), new Rect(4, 3, 0, 1) };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(4, 3, 3, 0), new Rect(4, 3, 0, 0) };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(4, 3, 2, 5), new Rect(4, 3, 0, 3) };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(4, 3, 2, 4), new Rect(4, 3, 0, 3) };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(4, 3, 2, 3), new Rect(4, 3, 0, 3) };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(4, 3, 2, 2), new Rect(4, 3, 0, 2) };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(4, 3, 2, 1), new Rect(4, 3, 0, 1) };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(4, 3, 2, 0), new Rect(4, 3, 0, 0) };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(4, 3, 1, 5), new Rect(4, 3, 0, 3) };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(4, 3, 1, 4), new Rect(4, 3, 0, 3) };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(4, 3, 1, 3), new Rect(4, 3, 0, 3) };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(4, 3, 1, 2), new Rect(4, 3, 0, 2) };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(4, 3, 1, 1), new Rect(4, 3, 0, 1) };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(4, 3, 1, 0), new Rect(4, 3, 0, 0) };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(4, 3, 0, 5), new Rect(4, 3, 0, 3) };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(4, 3, 0, 4), new Rect(4, 3, 0, 3) };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(4, 3, 0, 3), new Rect(4, 3, 0, 3) };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(4, 3, 0, 2), new Rect(4, 3, 0, 2) };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(4, 3, 0, 1), new Rect(4, 3, 0, 1) };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(4, 3, 0, 0), new Rect(4, 3, 0, 0) };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(4, 3, 4, 4), new Rect(4, 3, 0, 3) };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(4, 3, 3, 5), new Rect(4, 3, 0, 3) };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(4, 3, 4, 5), new Rect(4, 3, 0, 3) };
        
        // Normal - from top right.
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(1, 6, 3, 5), new Rect(1, 6, 3, 0) };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(1, 6, 3, 4), new Rect(1, 6, 3, 0) };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(1, 6, 3, 3), new Rect(1, 6, 3, 0) };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(1, 6, 3, 2), new Rect(1, 6, 3, 0) };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(1, 6, 3, 1), new Rect(1, 6, 3, 0) };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(1, 6, 3, 0), new Rect(1, 6, 3, 0) };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(1, 6, 2, 5), new Rect(1, 6, 2, 0) };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(1, 6, 2, 4), new Rect(1, 6, 2, 0) };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(1, 6, 2, 3), new Rect(1, 6, 2, 0) };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(1, 6, 2, 2), new Rect(1, 6, 2, 0) };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(1, 6, 2, 1), new Rect(1, 6, 2, 0) };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(1, 6, 2, 0), new Rect(1, 6, 2, 0) };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(1, 6, 1, 5), new Rect(1, 6, 1, 0) };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(1, 6, 1, 4), new Rect(1, 6, 1, 0) };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(1, 6, 1, 3), new Rect(1, 6, 1, 0) };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(1, 6, 1, 2), new Rect(1, 6, 1, 0) };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(1, 6, 1, 1), new Rect(1, 6, 1, 0) };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(1, 6, 1, 0), new Rect(1, 6, 1, 0) };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(1, 6, 0, 5), new Rect(1, 6, 0, 0) };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(1, 6, 0, 4), new Rect(1, 6, 0, 0) };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(1, 6, 0, 3), new Rect(1, 6, 0, 0) };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(1, 6, 0, 2), new Rect(1, 6, 0, 0) };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(1, 6, 0, 1), new Rect(1, 6, 0, 0) };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(1, 6, 0, 0), new Rect(1, 6, 0, 0) };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(1, 6, 4, 4), new Rect(1, 6, 3, 0) };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(1, 6, 3, 5), new Rect(1, 6, 3, 0) };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(1, 6, 4, 5), new Rect(1, 6, 3, 0) };

        // Normal - from middle.
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(2, 4, 3, 5), new Rect(2, 4, 2, 2) };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(2, 4, 3, 4), new Rect(2, 4, 2, 2) };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(2, 4, 3, 3), new Rect(2, 4, 2, 2) };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(2, 4, 3, 2), new Rect(2, 4, 2, 2) };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(2, 4, 3, 1), new Rect(2, 4, 2, 1) };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(2, 4, 3, 0), new Rect(2, 4, 2, 0) };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(2, 4, 2, 5), new Rect(2, 4, 2, 2) };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(2, 4, 2, 4), new Rect(2, 4, 2, 2) };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(2, 4, 2, 3), new Rect(2, 4, 2, 2) };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(2, 4, 2, 2), new Rect(2, 4, 2, 2) };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(2, 4, 2, 1), new Rect(2, 4, 2, 1) };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(2, 4, 2, 0), new Rect(2, 4, 2, 0) };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(2, 4, 1, 5), new Rect(2, 4, 1, 2) };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(2, 4, 1, 4), new Rect(2, 4, 1, 2) };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(2, 4, 1, 3), new Rect(2, 4, 1, 2) };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(2, 4, 1, 2), new Rect(2, 4, 1, 2) };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(2, 4, 1, 1), new Rect(2, 4, 1, 1) };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(2, 4, 1, 0), new Rect(2, 4, 1, 0) };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(2, 4, 0, 5), new Rect(2, 4, 0, 2) };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(2, 4, 0, 4), new Rect(2, 4, 0, 2) };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(2, 4, 0, 3), new Rect(2, 4, 0, 2) };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(2, 4, 0, 2), new Rect(2, 4, 0, 2) };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(2, 4, 0, 1), new Rect(2, 4, 0, 1) };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(2, 4, 0, 0), new Rect(2, 4, 0, 0) };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(2, 4, 4, 4), new Rect(2, 4, 2, 2) };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(2, 4, 3, 5), new Rect(2, 4, 2, 2) };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(2, 4, 4, 5), new Rect(2, 4, 2, 2) };

        // Normal - from outside top left.
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(0, 0, 2, 3), new Rect(1, 2, 1, 1) };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(0, 0, 1, 1), Rect.Empty };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(0, 0, 0.5, 0.5), Rect.Empty };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(0, 0, 0, 1), Rect.Empty };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(0, 0, 1, 0), Rect.Empty };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(0, 0, 0, 0), Rect.Empty };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(0, 1, 2, 3), new Rect(1, 2, 1, 2) };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(0, 1, 1, 1), new Rect(1, 2, 0, 0) };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(0, 1, 0, 3), Rect.Empty };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(0, 1, 2, 1), new Rect(1, 2, 1, 0) };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(0, 1, 0, 0), Rect.Empty };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(1, 0, 2, 3), new Rect(1, 2, 2, 1) };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(1, 0, 1, 1), Rect.Empty };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(1, 0, 1, 0), Rect.Empty };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(1, 0, 0, 1), Rect.Empty };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(1, 0, 0, 0), Rect.Empty };

        // Normal - from outside middle left.
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(0, 4, 2, 3), new Rect(1, 4, 1, 2) };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(0, 4, 1, 1), new Rect(1, 4, 0, 1) };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(0, 4, 0.5, 0.5), Rect.Empty };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(0, 4, 0, 1), Rect.Empty };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(0, 4, 1, 0), new Rect(1, 4, 0, 0) };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(0, 4, 0, 0), Rect.Empty };

        // Normal - from outside bottom left.
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(0, 6, 2, 3), new Rect(1, 6, 1, 0) };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(0, 6, 1, 1), new Rect(1, 6, 0, 0) };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(0, 6, 0.5, 0.5), Rect.Empty };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(0, 6, 0, 1), Rect.Empty };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(0, 6, 1, 0), new Rect(1, 6, 0, 0) };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(0, 6, 0, 0), Rect.Empty };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(0, 7, 2, 3), Rect.Empty };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(0, 7, 1, 1), Rect.Empty };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(0, 7, 0.5, 0.5), Rect.Empty };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(0, 7, 0, 1), Rect.Empty };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(0, 7, 1, 0), Rect.Empty };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(0, 7, 0, 0), Rect.Empty };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(1, 7, 2, 3), Rect.Empty };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(1, 7, 1, 1), Rect.Empty };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(1, 7, 0.5, 0.5), Rect.Empty };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(1, 7, 0, 1), Rect.Empty };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(1, 7, 1, 0), Rect.Empty };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(1, 7, 0, 0), Rect.Empty };

        // Normal - from outside bottom middle.
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(2, 7, 2, 3), Rect.Empty };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(2, 7, 1, 1), Rect.Empty };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(2, 7, 0.5, 0.5), Rect.Empty };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(2, 7, 0, 1), Rect.Empty };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(2, 7, 0, 1), Rect.Empty };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(2, 7, 0, 0), Rect.Empty };

        // Normal - from outside bottom right.
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(4, 7, 2, 3), Rect.Empty };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(4, 7, 1, 1), Rect.Empty };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(4, 7, 0.5, 0.5), Rect.Empty };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(4, 7, 1, 0), Rect.Empty };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(4, 7, 0, 1), Rect.Empty };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(4, 7, 0, 0), Rect.Empty };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(5, 7, 2, 3), Rect.Empty };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(5, 7, 1, 1), Rect.Empty };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(5, 7, 0.5, 0.5), Rect.Empty };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(5, 7, 0, 1), Rect.Empty };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(5, 7, 1, 0), Rect.Empty };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(5, 7, 0, 0), Rect.Empty };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(5, 5, 2, 3), Rect.Empty };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(5, 5, 1, 1), Rect.Empty };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(5, 5, 0.5, 0.5), Rect.Empty };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(5, 5, 0, 1), Rect.Empty };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(5, 5, 1, 0), Rect.Empty };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(5, 5, 0, 0), Rect.Empty };

        // From outside middle right.
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(5, 3, 2, 3), Rect.Empty };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(5, 3, 1, 1), Rect.Empty };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(5, 3, 0.5, 0.5), Rect.Empty };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(5, 3, 0, 1), Rect.Empty };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(5, 3, 1, 0), Rect.Empty };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(5, 3, 0, 0), Rect.Empty };

        // From outside top right.
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(5, 1, 2, 3), Rect.Empty };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(5, 1, 1, 1), Rect.Empty };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(5, 1, 0.5, 0.5), Rect.Empty };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(5, 1, 0, 1), Rect.Empty };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(5, 1, 1, 0), Rect.Empty };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(5, 1, 0, 0), Rect.Empty };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(5, 0, 2, 3), Rect.Empty };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(5, 0, 1, 1), Rect.Empty };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(5, 0, 0.5, 0.5), Rect.Empty };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(5, 0, 0, 1), Rect.Empty };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(5, 0, 1, 0), Rect.Empty };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(5, 0, 0, 0), Rect.Empty };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(4, 0, 2, 3), new Rect(4, 2, 0, 1) };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(4, 0, 1, 1), Rect.Empty };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(4, 0, 0.5, 0.5), Rect.Empty };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(4, 0, 0, 0), Rect.Empty };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(4, 0, 1, 0), Rect.Empty };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(4, 0, 0, 0), Rect.Empty };

        // Normal - empty.
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(0, 0, 0, 0), Rect.Empty };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(), Rect.Empty };
        yield return new object[] { new Rect(1, 2, 3, 4), Rect.Empty, Rect.Empty };

        // Empty width.
        yield return new object[] { new Rect(1, 2, 0, 4), new Rect(1, 2, 1, 5), new Rect(1, 2, 0, 4) };
        yield return new object[] { new Rect(1, 2, 0, 4), new Rect(1, 2, 1, 4), new Rect(1, 2, 0, 4) };
        yield return new object[] { new Rect(1, 2, 0, 4), new Rect(1, 2, 1, 3), new Rect(1, 2, 0, 3) };
        yield return new object[] { new Rect(1, 2, 0, 4), new Rect(1, 2, 1, 2), new Rect(1, 2, 0, 2) };
        yield return new object[] { new Rect(1, 2, 0, 4), new Rect(1, 2, 1, 1), new Rect(1, 2, 0, 1) };
        yield return new object[] { new Rect(1, 2, 0, 4), new Rect(1, 2, 1, 0), new Rect(1, 2, 0, 0) };
        yield return new object[] { new Rect(1, 2, 0, 4), new Rect(1, 2, 0, 5), new Rect(1, 2, 0, 4) };
        yield return new object[] { new Rect(1, 2, 0, 4), new Rect(1, 2, 0, 4), new Rect(1, 2, 0, 4) };
        yield return new object[] { new Rect(1, 2, 0, 4), new Rect(1, 2, 0, 3), new Rect(1, 2, 0, 3) };
        yield return new object[] { new Rect(1, 2, 0, 4), new Rect(1, 2, 0, 2), new Rect(1, 2, 0, 2) };
        yield return new object[] { new Rect(1, 2, 0, 4), new Rect(1, 2, 0, 1), new Rect(1, 2, 0, 1) };
        yield return new object[] { new Rect(1, 2, 0, 4), new Rect(1, 2, 0, 0), new Rect(1, 2, 0, 0) };
        yield return new object[] { new Rect(1, 2, 0, 4), new Rect(1, 1, 0, 0), Rect.Empty };
        yield return new object[] { new Rect(1, 2, 0, 4), new Rect(0, 1, 0, 0), Rect.Empty };
        yield return new object[] { new Rect(1, 2, 0, 4), new Rect(0, 0, 0, 1), Rect.Empty };
        yield return new object[] { new Rect(1, 2, 0, 4), new Rect(0, 0, 0, 0), Rect.Empty };
        yield return new object[] { new Rect(1, 2, 0, 4), new Rect(), Rect.Empty };
        yield return new object[] { new Rect(1, 2, 0, 4), Rect.Empty, Rect.Empty };

        // Empty height.
        yield return new object[] { new Rect(1, 2, 3, 0), new Rect(1, 2, 4, 1), new Rect(1, 2, 3, 0) };
        yield return new object[] { new Rect(1, 2, 3, 0), new Rect(1, 2, 4, 0), new Rect(1, 2, 3, 0) };
        yield return new object[] { new Rect(1, 2, 3, 0), new Rect(1, 2, 3, 1), new Rect(1, 2, 3, 0) };
        yield return new object[] { new Rect(1, 2, 3, 0), new Rect(1, 2, 3, 0), new Rect(1, 2, 3, 0) };
        yield return new object[] { new Rect(1, 2, 3, 0), new Rect(1, 2, 2, 1), new Rect(1, 2, 2, 0) };
        yield return new object[] { new Rect(1, 2, 3, 0), new Rect(1, 2, 2, 0), new Rect(1, 2, 2, 0) };
        yield return new object[] { new Rect(1, 2, 3, 0), new Rect(1, 2, 1, 1), new Rect(1, 2, 1, 0) };
        yield return new object[] { new Rect(1, 2, 3, 0), new Rect(1, 2, 1, 0), new Rect(1, 2, 1, 0) };
        yield return new object[] { new Rect(1, 2, 3, 0), new Rect(1, 2, 0, 0), new Rect(1, 2, 0, 0) };
        yield return new object[] { new Rect(1, 2, 3, 0), new Rect(1, 0, 0, 0), Rect.Empty };
        yield return new object[] { new Rect(1, 2, 3, 0), new Rect(0, 1, 0, 0), Rect.Empty };
        yield return new object[] { new Rect(1, 2, 3, 0), new Rect(0, 0, 0, 0), Rect.Empty };
        yield return new object[] { new Rect(1, 2, 3, 0), new Rect(), Rect.Empty };
        yield return new object[] { new Rect(1, 2, 3, 0), Rect.Empty, Rect.Empty };
        
        // Zero.
        yield return new object[] { new Rect(0, 0, 0, 0), new Rect(), new Rect(0, 0, 0, 0) };
        yield return new object[] { new Rect(0, 0, 0, 0), new Rect(0, 0, 0, 0), new Rect(0, 0, 0, 0) };
        yield return new object[] { new Rect(0, 0, 0, 0), new Rect(1, 0, 0, 0), Rect.Empty };
        yield return new object[] { new Rect(0, 0, 0, 0), new Rect(0, 1, 0, 0), Rect.Empty };
        yield return new object[] { new Rect(0, 0, 0, 0), new Rect(0, 0, 1, 0), new Rect(0, 0, 0, 0) };
        yield return new object[] { new Rect(0, 0, 0, 0), new Rect(0, 0, 0, 1), new Rect(0, 0, 0, 0) };
        yield return new object[] { new Rect(0, 0, 0, 0), new Rect(0, 0, 1, 1), new Rect(0, 0, 0, 0) };
        yield return new object[] { new Rect(0, 0, 0, 0), new Rect(1, 2, 3, 4), Rect.Empty };
        yield return new object[] { new Rect(0, 0, 0, 0), new Rect(1, 2, 3, 0), Rect.Empty };
        yield return new object[] { new Rect(0, 0, 0, 0), new Rect(1, 2, 0, 4), Rect.Empty };
        yield return new object[] { new Rect(0, 0, 0, 0), new Rect(1, 2, 0, 0), Rect.Empty };
        yield return new object[] { new Rect(0, 0, 0, 0), Rect.Empty, Rect.Empty };
        
        // Default.
        yield return new object[] { new Rect(), new Rect(), new Rect(0, 0, 0, 0) };
        yield return new object[] { new Rect(), new Rect(0, 0, 0, 0), new Rect(0, 0, 0, 0) };
        yield return new object[] { new Rect(), new Rect(1, 0, 0, 0), Rect.Empty };
        yield return new object[] { new Rect(), new Rect(0, 1, 0, 0), Rect.Empty };
        yield return new object[] { new Rect(), new Rect(0, 0, 1, 0), new Rect(0, 0, 0, 0) };
        yield return new object[] { new Rect(), new Rect(0, 0, 0, 1), new Rect(0, 0, 0, 0) };
        yield return new object[] { new Rect(), new Rect(0, 0, 1, 1), new Rect(0, 0, 0, 0) };
        yield return new object[] { new Rect(), new Rect(1, 2, 3, 4), Rect.Empty };
        yield return new object[] { new Rect(), new Rect(1, 2, 3, 0), Rect.Empty };
        yield return new object[] { new Rect(), new Rect(1, 2, 0, 4), Rect.Empty };
        yield return new object[] { new Rect(), new Rect(1, 2, 0, 0), Rect.Empty };
        yield return new object[] { new Rect(), Rect.Empty, Rect.Empty };

        // Empty.
        yield return new object[] { Rect.Empty, Rect.Empty, Rect.Empty };
        yield return new object[] { Rect.Empty, new Rect(), Rect.Empty };
        yield return new object[] { Rect.Empty, new Rect(0, 0, 0, 0), Rect.Empty };
        yield return new object[] { Rect.Empty, new Rect(1, 0, 0, 0), Rect.Empty };
        yield return new object[] { Rect.Empty, new Rect(0, 1, 0, 0), Rect.Empty };
        yield return new object[] { Rect.Empty, new Rect(0, 0, 1, 0), Rect.Empty };
        yield return new object[] { Rect.Empty, new Rect(0, 0, 0, 1), Rect.Empty };
        yield return new object[] { Rect.Empty, new Rect(1, 2, 3, 4), Rect.Empty };
        yield return new object[] { Rect.Empty, new Rect(1, 2, 3, 0), Rect.Empty };
        yield return new object[] { Rect.Empty, new Rect(1, 2, 0, 4), Rect.Empty };
        yield return new object[] { Rect.Empty, new Rect(1, 2, 0, 0), Rect.Empty };
    }

    [Theory]
    [MemberData(nameof(Intersect_Rect_TestData))]
    public void Intersect_InvokeRect_ReturnsExpected(Rect rect1, Rect rect2, Rect expected)
    {
        Rect copy1 = rect1;
        Rect copy2 = rect2;

        Assert.Equal(expected, Rect.Intersect(rect1, rect2));
        Assert.Equal(copy1, rect1);
        Assert.Equal(copy2, rect2);
        
        rect1.Intersect(rect2);
        Assert.Equal(expected, rect1);
        Assert.Equal(copy2, rect2);
    }

    public static IEnumerable<object[]> IntersectsWith_Rect_TestData()
    {
        // Normal - from top left.
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(1, 2, 3, 5), true };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(1, 2, 3, 4), true };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(1, 2, 3, 3), true };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(1, 2, 3, 2), true };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(1, 2, 3, 1), true };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(1, 2, 3, 0), true };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(1, 2, 2, 5), true };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(1, 2, 2, 4), true };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(1, 2, 2, 3), true };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(1, 2, 2, 2), true };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(1, 2, 2, 1), true };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(1, 2, 2, 0), true };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(1, 2, 1, 5), true };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(1, 2, 1, 4), true };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(1, 2, 1, 3), true };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(1, 2, 1, 2), true };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(1, 2, 1, 1), true };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(1, 2, 1, 0), true };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(1, 2, 0, 5), true };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(1, 2, 0, 4), true };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(1, 2, 0, 3), true };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(1, 2, 0, 2), true };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(1, 2, 0, 1), true };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(1, 2, 0, 0), true };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(1, 2, 4, 4), true };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(1, 2, 3, 5), true };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(1, 2, 4, 5), true };

        // Normal - from middle left.
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(1, 3, 3, 5), true };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(1, 3, 3, 4), true };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(1, 3, 3, 3), true };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(1, 3, 3, 2), true };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(1, 3, 3, 1), true };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(1, 3, 3, 0), true };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(1, 3, 2, 5), true };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(1, 3, 2, 4), true };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(1, 3, 2, 3), true };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(1, 3, 2, 2), true };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(1, 3, 2, 1), true };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(1, 3, 2, 0), true };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(1, 3, 1, 5), true };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(1, 3, 1, 4), true };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(1, 3, 1, 3), true };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(1, 3, 1, 2), true };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(1, 3, 1, 1), true };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(1, 3, 1, 0), true };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(1, 3, 0, 5), true };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(1, 3, 0, 4), true };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(1, 3, 0, 3), true };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(1, 3, 0, 2), true };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(1, 3, 0, 1), true };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(1, 3, 0, 0), true };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(1, 3, 4, 4), true };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(1, 3, 3, 5), true };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(1, 3, 4, 5), true };

        // Normal - from bottom left.
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(1, 5, 3, 5), true };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(1, 5, 3, 4), true };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(1, 5, 3, 3), true };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(1, 5, 3, 2), true };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(1, 5, 3, 1), true };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(1, 5, 3, 0), true };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(1, 5, 2, 5), true };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(1, 5, 2, 4), true };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(1, 5, 2, 3), true };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(1, 5, 2, 2), true };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(1, 5, 2, 1), true };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(1, 5, 2, 0), true };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(1, 5, 1, 5), true };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(1, 5, 1, 4), true };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(1, 5, 1, 3), true };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(1, 5, 1, 2), true };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(1, 5, 1, 1), true };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(1, 5, 1, 0), true };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(1, 5, 0, 5), true };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(1, 5, 0, 4), true };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(1, 5, 0, 3), true };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(1, 5, 0, 2), true };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(1, 5, 0, 1), true };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(1, 5, 0, 0), true };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(1, 5, 4, 4), true };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(1, 5, 3, 5), true };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(1, 5, 4, 5), true };
        
        // Normal - from bottom middle.
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(2, 6, 3, 5), true };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(2, 6, 3, 4), true };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(2, 6, 3, 3), true };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(2, 6, 3, 2), true };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(2, 6, 3, 1), true };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(2, 6, 3, 0), true };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(2, 6, 2, 5), true };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(2, 6, 2, 4), true };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(2, 6, 2, 3), true };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(2, 6, 2, 2), true };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(2, 6, 2, 1), true };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(2, 6, 2, 0), true };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(2, 6, 1, 5), true };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(2, 6, 1, 4), true };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(2, 6, 1, 3), true };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(2, 6, 1, 2), true };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(2, 6, 1, 1), true };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(2, 6, 1, 0), true };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(2, 6, 0, 5), true };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(2, 6, 0, 4), true };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(2, 6, 0, 3), true };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(2, 6, 0, 2), true };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(2, 6, 0, 1), true };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(2, 6, 0, 0), true };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(2, 6, 4, 4), true };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(2, 6, 3, 5), true };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(2, 6, 4, 5), true };
        
        // Normal - from bottom right.
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(4, 6, 3, 5), true };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(4, 6, 3, 4), true };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(4, 6, 3, 3), true };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(4, 6, 3, 2), true };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(4, 6, 3, 1), true };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(4, 6, 3, 0), true };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(4, 6, 2, 5), true };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(4, 6, 2, 4), true };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(4, 6, 2, 3), true };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(4, 6, 2, 2), true };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(4, 6, 2, 1), true };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(4, 6, 2, 0), true };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(4, 6, 1, 5), true };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(4, 6, 1, 4), true };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(4, 6, 1, 3), true };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(4, 6, 1, 2), true };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(4, 6, 1, 1), true };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(4, 6, 1, 0), true };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(4, 6, 0, 5), true };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(4, 6, 0, 4), true };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(4, 6, 0, 3), true };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(4, 6, 0, 2), true };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(4, 6, 0, 1), true };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(4, 6, 0, 0), true };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(4, 6, 4, 4), true };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(4, 6, 3, 5), true };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(4, 6, 4, 5), true };
        
        // Normal - from middle right.
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(4, 3, 3, 5), true };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(4, 3, 3, 4), true };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(4, 3, 3, 3), true };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(4, 3, 3, 2), true };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(4, 3, 3, 1), true };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(4, 3, 3, 0), true };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(4, 3, 2, 5), true };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(4, 3, 2, 4), true };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(4, 3, 2, 3), true };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(4, 3, 2, 2), true };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(4, 3, 2, 1), true };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(4, 3, 2, 0), true };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(4, 3, 1, 5), true };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(4, 3, 1, 4), true };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(4, 3, 1, 3), true };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(4, 3, 1, 2), true };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(4, 3, 1, 1), true };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(4, 3, 1, 0), true };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(4, 3, 0, 5), true };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(4, 3, 0, 4), true };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(4, 3, 0, 3), true };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(4, 3, 0, 2), true };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(4, 3, 0, 1), true };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(4, 3, 0, 0), true };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(4, 3, 4, 4), true };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(4, 3, 3, 5), true };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(4, 3, 4, 5), true };
        
        // Normal - from top right.
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(1, 6, 3, 5), true };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(1, 6, 3, 4), true };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(1, 6, 3, 3), true };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(1, 6, 3, 2), true };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(1, 6, 3, 1), true };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(1, 6, 3, 0), true };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(1, 6, 2, 5), true };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(1, 6, 2, 4), true };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(1, 6, 2, 3), true };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(1, 6, 2, 2), true };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(1, 6, 2, 1), true };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(1, 6, 2, 0), true };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(1, 6, 1, 5), true };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(1, 6, 1, 4), true };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(1, 6, 1, 3), true };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(1, 6, 1, 2), true };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(1, 6, 1, 1), true };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(1, 6, 1, 0), true };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(1, 6, 0, 5), true };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(1, 6, 0, 4), true };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(1, 6, 0, 3), true };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(1, 6, 0, 2), true };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(1, 6, 0, 1), true };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(1, 6, 0, 0), true };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(1, 6, 4, 4), true };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(1, 6, 3, 5), true };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(1, 6, 4, 5), true };

        // Normal - from middle.
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(2, 4, 3, 5), true };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(2, 4, 3, 4), true };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(2, 4, 3, 3), true };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(2, 4, 3, 2), true };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(2, 4, 3, 1), true };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(2, 4, 3, 0), true };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(2, 4, 2, 5), true };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(2, 4, 2, 4), true };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(2, 4, 2, 3), true };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(2, 4, 2, 2), true };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(2, 4, 2, 1), true };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(2, 4, 2, 0), true };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(2, 4, 1, 5), true };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(2, 4, 1, 4), true };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(2, 4, 1, 3), true };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(2, 4, 1, 2), true };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(2, 4, 1, 1), true };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(2, 4, 1, 0), true };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(2, 4, 0, 5), true };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(2, 4, 0, 4), true };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(2, 4, 0, 3), true };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(2, 4, 0, 2), true };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(2, 4, 0, 1), true };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(2, 4, 0, 0), true };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(2, 4, 4, 4), true };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(2, 4, 3, 5), true };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(2, 4, 4, 5), true };

        // Normal - from outside top left.
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(0, 0, 2, 3), true };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(0, 0, 1, 1), false };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(0, 0, 0.5, 0.5), false };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(0, 0, 0, 1), false };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(0, 0, 1, 0), false };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(0, 0, 0, 0), false };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(0, 1, 2, 3), true };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(0, 1, 1, 1), true };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(0, 1, 0, 3), false };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(0, 1, 2, 1), true };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(0, 1, 0, 0), false };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(1, 0, 2, 3), true };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(1, 0, 1, 1), false };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(1, 0, 1, 0), false };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(1, 0, 0, 1), false };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(1, 0, 0, 0), false };

        // Normal - from outside middle left.
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(0, 4, 2, 3), true };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(0, 4, 1, 1), true };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(0, 4, 0.5, 0.5), false };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(0, 4, 0, 1), false };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(0, 4, 1, 0), true };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(0, 4, 0, 0), false };

        // Normal - from outside bottom left.
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(0, 6, 2, 3), true };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(0, 6, 1, 1), true };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(0, 6, 0.5, 0.5), false };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(0, 6, 0, 1), false };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(0, 6, 1, 0), true };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(0, 6, 0, 0), false };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(0, 7, 2, 3), false };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(0, 7, 1, 1), false };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(0, 7, 0.5, 0.5), false };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(0, 7, 0, 1), false };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(0, 7, 1, 0), false };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(0, 7, 0, 0), false };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(1, 7, 2, 3), false };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(1, 7, 1, 1), false };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(1, 7, 0.5, 0.5), false };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(1, 7, 0, 1), false };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(1, 7, 1, 0), false };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(1, 7, 0, 0), false };

        // Normal - from outside bottom middle.
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(2, 7, 2, 3), false };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(2, 7, 1, 1), false };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(2, 7, 0.5, 0.5), false };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(2, 7, 0, 1), false };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(2, 7, 0, 1), false };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(2, 7, 0, 0), false };

        // Normal - from outside bottom right.
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(4, 7, 2, 3), false };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(4, 7, 1, 1), false };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(4, 7, 0.5, 0.5), false };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(4, 7, 1, 0), false };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(4, 7, 0, 1), false };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(4, 7, 0, 0), false };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(5, 7, 2, 3), false };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(5, 7, 1, 1), false };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(5, 7, 0.5, 0.5), false };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(5, 7, 0, 1), false };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(5, 7, 1, 0), false };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(5, 7, 0, 0), false };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(5, 5, 2, 3), false };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(5, 5, 1, 1), false };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(5, 5, 0.5, 0.5), false };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(5, 5, 0, 1), false };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(5, 5, 1, 0), false };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(5, 5, 0, 0), false };

        // From outside middle right.
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(5, 3, 2, 3), false };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(5, 3, 1, 1), false };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(5, 3, 0.5, 0.5), false };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(5, 3, 0, 1), false };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(5, 3, 1, 0), false };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(5, 3, 0, 0), false };

        // From outside top right.
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(5, 1, 2, 3), false };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(5, 1, 1, 1), false };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(5, 1, 0.5, 0.5), false };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(5, 1, 0, 1), false };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(5, 1, 1, 0), false };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(5, 1, 0, 0), false };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(5, 0, 2, 3), false };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(5, 0, 1, 1), false };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(5, 0, 0.5, 0.5), false };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(5, 0, 0, 1), false };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(5, 0, 1, 0), false };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(5, 0, 0, 0), false };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(4, 0, 2, 3), true };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(4, 0, 1, 1), false };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(4, 0, 0.5, 0.5), false };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(4, 0, 0, 0), false };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(4, 0, 1, 0), false };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(4, 0, 0, 0), false };

        // Normal - empty.
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(0, 0, 0, 0), false };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(), false };
        yield return new object[] { new Rect(1, 2, 3, 4), Rect.Empty, false };

        // Empty width.
        yield return new object[] { new Rect(1, 2, 0, 4), new Rect(1, 2, 1, 5), true };
        yield return new object[] { new Rect(1, 2, 0, 4), new Rect(1, 2, 1, 4), true };
        yield return new object[] { new Rect(1, 2, 0, 4), new Rect(1, 2, 1, 3), true };
        yield return new object[] { new Rect(1, 2, 0, 4), new Rect(1, 2, 1, 2), true };
        yield return new object[] { new Rect(1, 2, 0, 4), new Rect(1, 2, 1, 1), true };
        yield return new object[] { new Rect(1, 2, 0, 4), new Rect(1, 2, 1, 0), true };
        yield return new object[] { new Rect(1, 2, 0, 4), new Rect(1, 2, 0, 5), true };
        yield return new object[] { new Rect(1, 2, 0, 4), new Rect(1, 2, 0, 4), true };
        yield return new object[] { new Rect(1, 2, 0, 4), new Rect(1, 2, 0, 3), true };
        yield return new object[] { new Rect(1, 2, 0, 4), new Rect(1, 2, 0, 2), true };
        yield return new object[] { new Rect(1, 2, 0, 4), new Rect(1, 2, 0, 1), true };
        yield return new object[] { new Rect(1, 2, 0, 4), new Rect(1, 2, 0, 0), true };
        yield return new object[] { new Rect(1, 2, 0, 4), new Rect(1, 1, 0, 0), false };
        yield return new object[] { new Rect(1, 2, 0, 4), new Rect(0, 1, 0, 0), false };
        yield return new object[] { new Rect(1, 2, 0, 4), new Rect(0, 0, 0, 1), false };
        yield return new object[] { new Rect(1, 2, 0, 4), new Rect(0, 0, 0, 0), false };
        yield return new object[] { new Rect(1, 2, 0, 4), new Rect(), false };
        yield return new object[] { new Rect(1, 2, 0, 4), Rect.Empty, false };

        // Empty height.
        yield return new object[] { new Rect(1, 2, 3, 0), new Rect(1, 2, 4, 1), true };
        yield return new object[] { new Rect(1, 2, 3, 0), new Rect(1, 2, 4, 0), true };
        yield return new object[] { new Rect(1, 2, 3, 0), new Rect(1, 2, 3, 1), true };
        yield return new object[] { new Rect(1, 2, 3, 0), new Rect(1, 2, 3, 0), true };
        yield return new object[] { new Rect(1, 2, 3, 0), new Rect(1, 2, 2, 1), true };
        yield return new object[] { new Rect(1, 2, 3, 0), new Rect(1, 2, 2, 0), true };
        yield return new object[] { new Rect(1, 2, 3, 0), new Rect(1, 2, 1, 1), true };
        yield return new object[] { new Rect(1, 2, 3, 0), new Rect(1, 2, 1, 0), true };
        yield return new object[] { new Rect(1, 2, 3, 0), new Rect(1, 2, 0, 0), true };
        yield return new object[] { new Rect(1, 2, 3, 0), new Rect(1, 0, 0, 0), false };
        yield return new object[] { new Rect(1, 2, 3, 0), new Rect(0, 1, 0, 0), false };
        yield return new object[] { new Rect(1, 2, 3, 0), new Rect(0, 0, 0, 0), false };
        yield return new object[] { new Rect(1, 2, 3, 0), new Rect(), false };
        yield return new object[] { new Rect(1, 2, 3, 0), Rect.Empty, false };
        
        // Zero.
        yield return new object[] { new Rect(0, 0, 0, 0), new Rect(), true };
        yield return new object[] { new Rect(0, 0, 0, 0), new Rect(0, 0, 0, 0), true };
        yield return new object[] { new Rect(0, 0, 0, 0), new Rect(1, 0, 0, 0), false };
        yield return new object[] { new Rect(0, 0, 0, 0), new Rect(0, 1, 0, 0), false };
        yield return new object[] { new Rect(0, 0, 0, 0), new Rect(0, 0, 1, 0), true };
        yield return new object[] { new Rect(0, 0, 0, 0), new Rect(0, 0, 0, 1), true };
        yield return new object[] { new Rect(0, 0, 0, 0), new Rect(0, 0, 1, 1), true };
        yield return new object[] { new Rect(0, 0, 0, 0), new Rect(1, 2, 3, 4), false };
        yield return new object[] { new Rect(0, 0, 0, 0), new Rect(1, 2, 3, 0), false };
        yield return new object[] { new Rect(0, 0, 0, 0), new Rect(1, 2, 0, 4), false };
        yield return new object[] { new Rect(0, 0, 0, 0), new Rect(1, 2, 0, 0), false };
        yield return new object[] { new Rect(0, 0, 0, 0), Rect.Empty, false };
        
        // Default.
        yield return new object[] { new Rect(), new Rect(), true };
        yield return new object[] { new Rect(), new Rect(0, 0, 0, 0), true };
        yield return new object[] { new Rect(), new Rect(1, 0, 0, 0), false };
        yield return new object[] { new Rect(), new Rect(0, 1, 0, 0), false };
        yield return new object[] { new Rect(), new Rect(0, 0, 1, 0), true };
        yield return new object[] { new Rect(), new Rect(0, 0, 0, 1), true };
        yield return new object[] { new Rect(), new Rect(0, 0, 1, 1), true };
        yield return new object[] { new Rect(), new Rect(1, 2, 3, 4), false };
        yield return new object[] { new Rect(), new Rect(1, 2, 3, 0), false };
        yield return new object[] { new Rect(), new Rect(1, 2, 0, 4), false };
        yield return new object[] { new Rect(), new Rect(1, 2, 0, 0), false };
        yield return new object[] { new Rect(), Rect.Empty, false };

        // Empty.
        yield return new object[] { Rect.Empty, Rect.Empty, false };
        yield return new object[] { Rect.Empty, new Rect(), false };
        yield return new object[] { Rect.Empty, new Rect(0, 0, 0, 0), false };
        yield return new object[] { Rect.Empty, new Rect(1, 0, 0, 0), false };
        yield return new object[] { Rect.Empty, new Rect(0, 1, 0, 0), false };
        yield return new object[] { Rect.Empty, new Rect(0, 0, 1, 0), false };
        yield return new object[] { Rect.Empty, new Rect(0, 0, 0, 1), false };
        yield return new object[] { Rect.Empty, new Rect(1, 2, 3, 4), false };
        yield return new object[] { Rect.Empty, new Rect(1, 2, 3, 0), false };
        yield return new object[] { Rect.Empty, new Rect(1, 2, 0, 4), false };
        yield return new object[] { Rect.Empty, new Rect(1, 2, 0, 0), false };
    }

    [Theory]
    [MemberData(nameof(IntersectsWith_Rect_TestData))]
    public void IntersectsWith_InvokeRect_ReturnsExpected(Rect rect, Rect other, bool expected)
    {
        Assert.Equal(expected, rect.IntersectsWith(other));
        Assert.Equal(expected, other.IntersectsWith(rect));
    }

    public static IEnumerable<object[]> Offset_TestData()
    {
        // Normal.
        yield return new object[] { new Rect(1, 2, 3, 4), 0, 0, new Rect(1, 2, 3, 4) };
        yield return new object[] { new Rect(1, 2, 3, 4), 5, 6, new Rect(6, 8, 3, 4) };
        yield return new object[] { new Rect(1.25, 2.25, 3, 4), 5.5, 6.5, new Rect(6.75, 8.75, 3, 4) };
        yield return new object[] { new Rect(1, 2, 3, 4), 5, 0, new Rect(6, 2, 3, 4) };
        yield return new object[] { new Rect(1, 2, 3, 4), 0, 6, new Rect(1, 8, 3, 4) };
        yield return new object[] { new Rect(1, 2, 3, 4), -5, -6, new Rect(-4, -4, 3, 4) };

        // Large.
        yield return new object[] { new Rect(double.MaxValue, double.MaxValue, 3, 4), 1, 2, new Rect(double.MaxValue, double.MaxValue, 3, 4) };
        yield return new object[] { new Rect(double.MaxValue, double.MaxValue, 3, 4), 0, 0, new Rect(double.MaxValue, double.MaxValue, 3, 4) };
        yield return new object[] { new Rect(double.MaxValue, double.MaxValue, 3, 4), double.MaxValue, double.MaxValue, new Rect(double.PositiveInfinity, double.PositiveInfinity, 3, 4) };
        yield return new object[] { new Rect(double.MinValue, double.MinValue, 3, 4), -1, -2, new Rect(double.MinValue, double.MinValue, 3, 4) };
        yield return new object[] { new Rect(double.MinValue, double.MinValue, 3, 4), 0, 0, new Rect(double.MinValue, double.MinValue, 3, 4) };
        yield return new object[] { new Rect(double.MinValue, double.MinValue, 3, 4), double.MinValue, double.MinValue, new Rect(double.NegativeInfinity, double.NegativeInfinity, 3, 4) };
        yield return new object[] { new Rect(1, 2, 3, 4), double.MaxValue, double.MaxValue, new Rect(double.MaxValue, double.MaxValue, 3, 4) };
        yield return new object[] { new Rect(0, 0, 3, 4), double.MaxValue, double.MaxValue, new Rect(double.MaxValue, double.MaxValue, 3, 4) };
        yield return new object[] { new Rect(-1, -2, 3, 4), double.MinValue, double.MinValue, new Rect(double.MinValue, double.MinValue, 3, 4) };
        yield return new object[] { new Rect(0, 0, 3, 4), double.MinValue, double.MinValue, new Rect(double.MinValue, double.MinValue, 3, 4) };

        // Infinite.
        yield return new object[] { new Rect(double.NegativeInfinity, double.NegativeInfinity, double.PositiveInfinity, double.PositiveInfinity), 0, 0, new Rect(double.NegativeInfinity, double.NegativeInfinity, double.PositiveInfinity, double.PositiveInfinity) };
        yield return new object[] { new Rect(double.NegativeInfinity, double.NegativeInfinity, double.PositiveInfinity, double.PositiveInfinity), 5, 6, new Rect(double.NegativeInfinity, double.NegativeInfinity, double.PositiveInfinity, double.PositiveInfinity) };
        yield return new object[] { new Rect(double.NegativeInfinity, double.NegativeInfinity, double.PositiveInfinity, double.PositiveInfinity), -5, -6, new Rect(double.NegativeInfinity, double.NegativeInfinity, double.PositiveInfinity, double.PositiveInfinity) };
        yield return new object[] { new Rect(double.NegativeInfinity, double.NegativeInfinity, double.PositiveInfinity, double.PositiveInfinity), double.PositiveInfinity, double.PositiveInfinity, new Rect(double.NaN, double.NaN, double.PositiveInfinity, double.PositiveInfinity) };
        yield return new object[] { new Rect(double.NegativeInfinity, double.NegativeInfinity, double.PositiveInfinity, double.PositiveInfinity), double.NegativeInfinity, double.NegativeInfinity, new Rect(double.NegativeInfinity, double.NegativeInfinity, double.PositiveInfinity, double.PositiveInfinity) };
        yield return new object[] { new Rect(1, 2, double.PositiveInfinity, double.PositiveInfinity), 0, 0, new Rect(1, 2, double.PositiveInfinity, double.PositiveInfinity) };
        yield return new object[] { new Rect(1, 2, double.PositiveInfinity, double.PositiveInfinity), 5, 6, new Rect(6, 8, double.PositiveInfinity, double.PositiveInfinity) };
        yield return new object[] { new Rect(1, 2, double.PositiveInfinity, double.PositiveInfinity), -5, -6, new Rect(-4, -4, double.PositiveInfinity, double.PositiveInfinity) };
        yield return new object[] { new Rect(1, 2, double.PositiveInfinity, double.PositiveInfinity), double.PositiveInfinity, double.PositiveInfinity, new Rect(double.PositiveInfinity, double.PositiveInfinity, double.PositiveInfinity, double.PositiveInfinity) };
        yield return new object[] { new Rect(1, 2, double.PositiveInfinity, double.PositiveInfinity), double.NegativeInfinity, double.NegativeInfinity, new Rect(double.NegativeInfinity, double.NegativeInfinity, double.PositiveInfinity, double.PositiveInfinity) };

        // NaN.
        yield return new object[] { new Rect(double.NaN, double.NaN, double.NaN, double.NaN), 0, 0, new Rect(double.NaN, double.NaN, double.NaN, double.NaN) };
        yield return new object[] { new Rect(double.NaN, double.NaN, double.NaN, double.NaN), 5, 6, new Rect(double.NaN, double.NaN, double.NaN, double.NaN) };
        yield return new object[] { new Rect(double.NaN, double.NaN, double.NaN, double.NaN), -5, -6, new Rect(double.NaN, double.NaN, double.NaN, double.NaN) };
        yield return new object[] { new Rect(double.NaN, double.NaN, double.NaN, double.NaN), double.PositiveInfinity, double.PositiveInfinity, new Rect(double.NaN, double.NaN, double.NaN, double.NaN) };
        yield return new object[] { new Rect(double.NaN, double.NaN, double.NaN, double.NaN), double.NegativeInfinity, double.NegativeInfinity, new Rect(double.NaN, double.NaN, double.NaN, double.NaN) };
        yield return new object[] { new Rect(double.NaN, double.NaN, double.NaN, double.NaN), double.NaN, double.NaN, new Rect(double.NaN, double.NaN, double.NaN, double.NaN) };
        yield return new object[] { new Rect(1, 2, double.NaN, double.NaN), 0, 0, new Rect(1, 2, double.NaN, double.NaN) };
        yield return new object[] { new Rect(1, 2, double.NaN, double.NaN), 5, 6, new Rect(6, 8, double.NaN, double.NaN) };
        yield return new object[] { new Rect(1, 2, double.NaN, double.NaN), -5, -6, new Rect(-4, -4, double.NaN, double.NaN) };
        yield return new object[] { new Rect(1, 2, double.NaN, double.NaN), double.PositiveInfinity, double.PositiveInfinity, new Rect(double.PositiveInfinity, double.PositiveInfinity, double.NaN, double.NaN) };
        yield return new object[] { new Rect(1, 2, double.NaN, double.NaN), double.NegativeInfinity, double.NegativeInfinity, new Rect(double.NegativeInfinity, double.NegativeInfinity, double.NaN, double.NaN) };
        yield return new object[] { new Rect(1, 2, double.NaN, double.NaN), double.NaN, double.NaN, new Rect(double.NaN, double.NaN, double.NaN, double.NaN) };

        // Zero.
        yield return new object[] { new Rect(0, 0, 0, 0), 0, 0, new Rect(0, 0, 0, 0) };
        yield return new object[] { new Rect(0, 0, 0, 0), 1, 2, new Rect(1, 2, 0, 0) };
        yield return new object[] { new Rect(0, 0, 0, 0), -1, -2, new Rect(-1, -2, 0, 0) };
    }

    [Theory]
    [MemberData(nameof(Offset_TestData))]
    public void Offset_Invoke_ReturnsExpected(Rect rect, double offsetX, double offsetY, Rect expected)
    {
        Rect copy = rect;

        Assert.Equal(expected, Rect.Offset(rect, offsetX, offsetY));
        Assert.Equal(copy, rect);

        Assert.Equal(expected, Rect.Offset(rect, new Vector(offsetX, offsetY)));
        Assert.Equal(copy, rect);

        rect.Offset(offsetX, offsetY);
        Assert.Equal(expected, rect);

        rect = copy;
        rect.Offset(new Vector(offsetX, offsetY));
        Assert.Equal(expected, rect);
    }

    [Fact]
    public void Offset_Empty_ThrowsInvalidOperationException()
    {
        Assert.Throws<InvalidOperationException>(() => Rect.Empty.Offset(0, 0));
        Assert.Throws<InvalidOperationException>(() => Rect.Empty.Offset(new Vector(0, 0)));
        Assert.Throws<InvalidOperationException>(() => Rect.Offset(Rect.Empty, 0, 0));
        Assert.Throws<InvalidOperationException>(() => Rect.Offset(Rect.Empty, new Vector(0, 0)));
    }

    public static IEnumerable<object[]> Parse_TestData()
    {
        yield return new object[] { "Empty", Rect.Empty };
        yield return new object[] { "  Empty  ", Rect.Empty };
        yield return new object[] { "0,0,0,0", new Rect() };
        yield return new object[] { "1,2,3,4", new Rect(1, 2, 3, 4) };
        yield return new object[] { "1.1,2.2,3.3,4.4", new Rect(1.1, 2.2, 3.3, 4.4) };
        yield return new object[] { "   1   ,   2  ,  3,  4", new Rect(1, 2, 3, 4) };
        yield return new object[] { "-1,-2,3,4", new Rect(-1, -2, 3, 4) };
    }

    [Theory]
    [MemberData(nameof(Parse_TestData))]
    public void Parse_Invoke_Success(string source, Rect expected)
    {
        Rect result = Rect.Parse(source);
        Assert.Equal(expected.X, result.X, precision: 5);
        Assert.Equal(expected.Y, result.Y, precision: 5);
        Assert.Equal(expected.Width, result.Width, precision: 5);
        Assert.Equal(expected.Height, result.Height, precision: 5);
    }

    [Fact]
    public void Parse_NullSource_ThrowsInvalidOperationException()
    {
        Assert.Throws<InvalidOperationException>(() => Rect.Parse(null));
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
        Assert.Throws<InvalidOperationException>(() => Rect.Parse(source));
    }

    public static IEnumerable<object[]> Parse_NotDouble_TestData()
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
    }

    [Theory]
    [MemberData(nameof(Parse_NotDouble_TestData))]
    public void Parse_NotDouble_ThrowsFormatException(string source)
    {
        Assert.Throws<FormatException>(() => Rect.Parse(source));
    }

    public static IEnumerable<object[]> Parse_Negative_TestData()
    {
        yield return new object[] { "1,2,-3,4" };
        yield return new object[] { "1,2,3,-4" };
    }

    [Theory]
    [MemberData(nameof(Parse_Negative_TestData))]
    public void Parse_Negative_ThrowsArgumentException(string source)
    {
        Assert.Throws<ArgumentException>(() => Rect.Parse(source));
    }

    public static IEnumerable<object[]> Scale_TestData()
    {
        // Normal.
        yield return new object[] { new Rect(1, 2, 3, 4), 2, 3, new Rect(2, 6, 6, 12) };
        yield return new object[] { new Rect(1, 2, 3, 4), 2, 1, new Rect(2, 2, 6, 4) };
        yield return new object[] { new Rect(1, 2, 3, 4), 1, 3, new Rect(1, 6, 3, 12) };
        yield return new object[] { new Rect(1, 2, 3, 4), 1, 1, new Rect(1, 2, 3, 4) };
        yield return new object[] { new Rect(1, 2, 3, 4), 0.5, 0.5, new Rect(0.5, 1, 1.5, 2) };
        yield return new object[] { new Rect(1, 2, 3, 4), 1, 0, new Rect(1, 0, 3, 0) };
        yield return new object[] { new Rect(1, 2, 3, 4), 0, 1, new Rect(0, 2, 0, 4) };
        yield return new object[] { new Rect(1, 2, 3, 4), 0, 0, new Rect(0, 0, 0, 0) };
        yield return new object[] { new Rect(1, 2, 3, 4), -1, 1, new Rect(-4, 2, 3, 4) };
        yield return new object[] { new Rect(1, 2, 3, 4), 1, -1, new Rect(1, -6, 3, 4) };
        yield return new object[] { new Rect(1, 2, 3, 4), -1, -2, new Rect(-4, -12, 3, 8) };
        yield return new object[] { new Rect(1, 2, 3, 4), -2, -3, new Rect(-8, -18, 6, 12) };

        // Large.
        yield return new object[] { new Rect(double.MaxValue, double.MaxValue, 3, 4), 1, 2, new Rect(double.MaxValue, double.PositiveInfinity, 3, 8) };
        yield return new object[] { new Rect(double.MaxValue, double.MaxValue, 3, 4), 0, 0, new Rect(0, 0, 0, 0) };
        yield return new object[] { new Rect(double.MaxValue, double.MaxValue, 3, 4), double.MaxValue, double.MaxValue, new Rect(double.PositiveInfinity, double.PositiveInfinity, double.PositiveInfinity, double.PositiveInfinity) };
        yield return new object[] { new Rect(double.MinValue, double.MinValue, 3, 4), -1, -2, new Rect(double.MaxValue, double.PositiveInfinity, 3, 8) };
        yield return new object[] { new Rect(double.MinValue, double.MinValue, 3, 4), 0, 0, new Rect(0, 0, 0, 0) };
        yield return new object[] { new Rect(double.MinValue, double.MinValue, 3, 4), double.MinValue, double.MinValue, new Rect(double.NaN, double.NaN, double.PositiveInfinity, double.PositiveInfinity) };
        yield return new object[] { new Rect(1, 2, 3, 4), double.MaxValue, double.MaxValue, new Rect(double.MaxValue, double.PositiveInfinity, double.PositiveInfinity, double.PositiveInfinity) };
        yield return new object[] { new Rect(0, 0, 3, 4), double.MaxValue, double.MaxValue, new Rect(0, 0, double.PositiveInfinity, double.PositiveInfinity) };
        yield return new object[] { new Rect(-1, -2, 3, 4), double.MinValue, double.MinValue, new Rect(double.NegativeInfinity, double.NaN, double.PositiveInfinity, double.PositiveInfinity) };
        yield return new object[] { new Rect(0, 0, 3, 4), double.MinValue, double.MinValue, new Rect(double.NegativeInfinity, double.NegativeInfinity, double.PositiveInfinity, double.PositiveInfinity) };

        // Infinite.
        yield return new object[] { new Rect(double.NegativeInfinity, double.NegativeInfinity, double.PositiveInfinity, double.PositiveInfinity), 0, 0, new Rect(double.NaN, double.NaN, double.NaN, double.NaN) };
        yield return new object[] { new Rect(double.NegativeInfinity, double.NegativeInfinity, double.PositiveInfinity, double.PositiveInfinity), 5, 6, new Rect(double.NegativeInfinity, double.NegativeInfinity, double.PositiveInfinity, double.PositiveInfinity) };
        yield return new object[] { new Rect(double.NegativeInfinity, double.NegativeInfinity, double.PositiveInfinity, double.PositiveInfinity), -5, -6, new Rect(double.NaN, double.NaN, double.PositiveInfinity, double.PositiveInfinity) };
        yield return new object[] { new Rect(double.NegativeInfinity, double.NegativeInfinity, double.PositiveInfinity, double.PositiveInfinity), double.PositiveInfinity, double.PositiveInfinity, new Rect(double.NegativeInfinity, double.NegativeInfinity, double.PositiveInfinity, double.PositiveInfinity) };
        yield return new object[] { new Rect(double.NegativeInfinity, double.NegativeInfinity, double.PositiveInfinity, double.PositiveInfinity), double.NegativeInfinity, double.NegativeInfinity, new Rect(double.NaN, double.NaN, double.PositiveInfinity, double.PositiveInfinity) };
        yield return new object[] { new Rect(1, 2, double.PositiveInfinity, double.PositiveInfinity), 0, 0, new Rect(0, 0, double.NaN, double.NaN) };
        yield return new object[] { new Rect(1, 2, double.PositiveInfinity, double.PositiveInfinity), 5, 6, new Rect(5, 12, double.PositiveInfinity, double.PositiveInfinity) };
        yield return new object[] { new Rect(1, 2, double.PositiveInfinity, double.PositiveInfinity), -5, -6, new Rect(double.NegativeInfinity ,double.NegativeInfinity, double.PositiveInfinity, double.PositiveInfinity) };
        yield return new object[] { new Rect(1, 2, double.PositiveInfinity, double.PositiveInfinity), double.PositiveInfinity, double.PositiveInfinity, new Rect(double.PositiveInfinity, double.PositiveInfinity, double.PositiveInfinity, double.PositiveInfinity) };
        yield return new object[] { new Rect(1, 2, double.PositiveInfinity, double.PositiveInfinity), double.NegativeInfinity, double.NegativeInfinity, new Rect(double.NegativeInfinity,double.NegativeInfinity, double.PositiveInfinity, double.PositiveInfinity) };

        // NaN.
        yield return new object[] { new Rect(double.NaN, double.NaN, double.NaN, double.NaN), 0, 0, new Rect(double.NaN, double.NaN, double.NaN, double.NaN) };
        yield return new object[] { new Rect(double.NaN, double.NaN, double.NaN, double.NaN), 5, 6, new Rect(double.NaN, double.NaN, double.NaN, double.NaN) };
        yield return new object[] { new Rect(double.NaN, double.NaN, double.NaN, double.NaN), -5, -6, new Rect(double.NaN, double.NaN, double.NaN, double.NaN) };
        yield return new object[] { new Rect(double.NaN, double.NaN, double.NaN, double.NaN), double.PositiveInfinity, double.PositiveInfinity, new Rect(double.NaN, double.NaN, double.NaN, double.NaN) };
        yield return new object[] { new Rect(double.NaN, double.NaN, double.NaN, double.NaN), double.NegativeInfinity, double.NegativeInfinity, new Rect(double.NaN, double.NaN, double.NaN, double.NaN) };
        yield return new object[] { new Rect(double.NaN, double.NaN, double.NaN, double.NaN), double.NaN, double.NaN, new Rect(double.NaN, double.NaN, double.NaN, double.NaN) };
        yield return new object[] { new Rect(1, 2, double.NaN, double.NaN), 0, 0, new Rect(0, 0, double.NaN, double.NaN) };
        yield return new object[] { new Rect(1, 2, double.NaN, double.NaN), 5, 6, new Rect(5, 12, double.NaN, double.NaN) };
        yield return new object[] { new Rect(1, 2, double.NaN, double.NaN), -5, -6, new Rect(double.NaN, double.NaN, double.NaN, double.NaN) };
        yield return new object[] { new Rect(1, 2, double.NaN, double.NaN), double.PositiveInfinity, double.PositiveInfinity, new Rect(double.PositiveInfinity, double.PositiveInfinity, double.NaN, double.NaN) };
        yield return new object[] { new Rect(1, 2, double.NaN, double.NaN), double.NegativeInfinity, double.NegativeInfinity, new Rect(double.NaN, double.NaN, double.NaN, double.NaN) };
        yield return new object[] { new Rect(1, 2, double.NaN, double.NaN), double.NaN, double.NaN, new Rect(double.NaN, double.NaN, double.NaN, double.NaN) };

        // Zero.
        yield return new object[] { new Rect(0, 0, 0, 0), 2, 3, new Rect(0, 0, 0, 0) };
        yield return new object[] { new Rect(0, 0, 0, 0), 1, 1, new Rect(0, 0, 0, 0) };
        yield return new object[] { new Rect(0, 0, 0, 0), 0.5, 0.5, new Rect(0, 0, 0, 0) };
        yield return new object[] { new Rect(0, 0, 0, 0), 0, 0, new Rect(0, 0, 0, 0) };
        yield return new object[] { new Rect(0, 0, 0, 0), -1, -2, new Rect(0, 0, 0, 0) };

        // Default.
        yield return new object[] { new Rect(), 2, 3, new Rect(0, 0, 0, 0) };
        yield return new object[] { new Rect(), 1, 1, new Rect(0, 0, 0, 0) };
        yield return new object[] { new Rect(), 0.5, 0.5, new Rect(0, 0, 0, 0) };
        yield return new object[] { new Rect(), 0, 0, new Rect(0, 0, 0, 0) };
        yield return new object[] { new Rect(), -1, -2, new Rect(0, 0, 0, 0) };

        // Empty.
        yield return new object[] { Rect.Empty, 2, 3, Rect.Empty };
        yield return new object[] { Rect.Empty, 1, 1, Rect.Empty };
        yield return new object[] { Rect.Empty, 0.5, 0.5, Rect.Empty };
        yield return new object[] { Rect.Empty, 0, 0, Rect.Empty };
        yield return new object[] { Rect.Empty, -1, -2, Rect.Empty };
    }

    [Theory]
    [MemberData(nameof(Scale_TestData))]
    public void Scale_Invoke_ReturnsExpected(Rect rect, double scaleX, double scaleY, Rect expected)
    {
        rect.Scale(scaleX, scaleY);
        Assert.Equal(expected, rect);
    }

    public static IEnumerable<object[]> ToString_TestData()
    {
        yield return new object[] { Rect.Empty, "Empty" };
        yield return new object[] { new Rect(), "0,0,0,0" };
        yield return new object[] { new Rect(0, 0, 0, 0), "0,0,0,0" };
        yield return new object[] { new Rect(1, 2, 3, 4), "1,2,3,4" };
        yield return new object[] { new Rect(1.1, 2.2, 3.3, 4.4), "1.1,2.2,3.3,4.4" };
        yield return new object[] { new Rect(-1, -2, 3, 4), "-1,-2,3,4" };
    }

    [Theory]
    [MemberData(nameof(ToString_TestData))]
    public void ToString_Invoke_ReturnsExpected(Rect rect, string expected)
    {
        Assert.Equal(expected, rect.ToString());
    }

    [Theory]
    [MemberData(nameof(ToString_TestData))]
    public void ToString_InvokeIFormatProviderInvariantCulture_ReturnsExpected(Rect rect, string expected)
    {
        Assert.Equal(expected, rect.ToString(CultureInfo.InvariantCulture));
    }

    public static IEnumerable<object[]> ToString_IFormatProviderCustom_TestData()
    {
        yield return new object[] { Rect.Empty, "|", "Empty" };
        yield return new object[] { Rect.Empty, "|_", "Empty" };
        yield return new object[] { Rect.Empty, ",_", "Empty" };
        yield return new object[] { Rect.Empty, ",", "Empty" };
        yield return new object[] { Rect.Empty, ";", "Empty" };
        yield return new object[] { Rect.Empty, " ", "Empty" };

        yield return new object[] { new Rect(), "|", "0,0,0,0" };
        yield return new object[] { new Rect(), "|_", "0,0,0,0" };
        yield return new object[] { new Rect(), ",_", "0;0;0;0" };
        yield return new object[] { new Rect(), ",", "0;0;0;0" };
        yield return new object[] { new Rect(), ";", "0,0,0,0" };
        yield return new object[] { new Rect(), " ", "0,0,0,0" };

        yield return new object[] { new Rect(0, 0, 0, 0), "|", "0,0,0,0" };
        yield return new object[] { new Rect(0, 0, 0, 0), "|_", "0,0,0,0" };
        yield return new object[] { new Rect(0, 0, 0, 0), ",_", "0;0;0;0" };
        yield return new object[] { new Rect(0, 0, 0, 0), ",", "0;0;0;0" };
        yield return new object[] { new Rect(0, 0, 0, 0), ";", "0,0,0,0" };
        yield return new object[] { new Rect(0, 0, 0, 0), " ", "0,0,0,0" };

        yield return new object[] { new Rect(1, 2, 3, 4), "|", "1,2,3,4" };
        yield return new object[] { new Rect(1, 2, 3, 4), "|_", "1,2,3,4" };
        yield return new object[] { new Rect(1, 2, 3, 4), ",_", "1;2;3;4" };
        yield return new object[] { new Rect(1, 2, 3, 4), ",", "1;2;3;4" };
        yield return new object[] { new Rect(1, 2, 3, 4), ";", "1,2,3,4" };
        yield return new object[] { new Rect(1, 2, 3, 4), " ", "1,2,3,4" };

        yield return new object[] { new Rect(1.1, 2.2, 3.3, 4.4), "|", "1|1,2|2,3|3,4|4" };
        yield return new object[] { new Rect(1.1, 2.2, 3.3, 4.4), "|_", "1|_1,2|_2,3|_3,4|_4" };
        yield return new object[] { new Rect(1.1, 2.2, 3.3, 4.4), ",_", "1,_1;2,_2;3,_3;4,_4" };
        yield return new object[] { new Rect(1.1, 2.2, 3.3, 4.4), ",", "1,1;2,2;3,3;4,4" };
        yield return new object[] { new Rect(1.1, 2.2, 3.3, 4.4), ";", "1;1,2;2,3;3,4;4" };
        yield return new object[] { new Rect(1.1, 2.2, 3.3, 4.4), " ", "1 1,2 2,3 3,4 4" };

        yield return new object[] { new Rect(-1, -2, 3, 4), "|", "-1,-2,3,4" };
        yield return new object[] { new Rect(-1, -2, 3, 4), "|_", "-1,-2,3,4" };
        yield return new object[] { new Rect(-1, -2, 3, 4), ",_", "-1;-2;3;4" };
        yield return new object[] { new Rect(-1, -2, 3, 4), ",", "-1;-2;3;4" };
        yield return new object[] { new Rect(-1, -2, 3, 4), ";", "-1,-2,3,4" };
        yield return new object[] { new Rect(-1, -2, 3, 4), " ", "-1,-2,3,4" };
    }

    [Theory]
    [MemberData(nameof(ToString_IFormatProviderCustom_TestData))]
    public void ToString_InvokeIFormatProviderCustom_ReturnsExpected(Rect rect, string numberDecimalSeparator, string expected)
    {
        var culture = new CultureInfo("en-US");
        NumberFormatInfo formatInfo = culture.NumberFormat;
        formatInfo.NumberDecimalSeparator = numberDecimalSeparator;

        Assert.Equal(expected, rect.ToString(formatInfo));
    }

    [Theory]
    [MemberData(nameof(ToString_TestData))]
    public void ToString_InvokeIFormattableInvariantCulture_ReturnsExpected(Rect rect, string expected)
    {
        IFormattable formattable = rect;
        Assert.Equal(expected, formattable.ToString(null, null));
        Assert.Equal(expected, formattable.ToString(null, CultureInfo.InvariantCulture));
    }

    [Theory]
    [InlineData("|", "1|23,2|35,3|46,4|57")]
    [InlineData("|_", "1|_23,2|_35,3|_46,4|_57")]
    [InlineData(",_", "1,_23;2,_35;3,_46;4,_57")]
    [InlineData(",", "1,23;2,35;3,46;4,57")]
    [InlineData(";", "1;23,2;35,3;46,4;57")]
    [InlineData(" ", "1 23,2 35,3 46,4 57")]
    public void ToString_InvokeIFormattableCustomFormat_ReturnsExpected(string numberDecimalSeparator, string expected)
    {
        var rect = new Rect(1.23456, 2.34567, 3.45678, 4.56789);
        IFormattable formattable = rect;

        var culture = new CultureInfo("en-US");
        NumberFormatInfo formatInfo = culture.NumberFormat;
        formatInfo.NumberDecimalSeparator = numberDecimalSeparator;
        Assert.Equal(expected, formattable.ToString("F2", formatInfo));
    }

    public static IEnumerable<object[]> Transform_TestData()
    {
        // Identity.
        foreach (Matrix matrix in MatrixTests.IdentityMatrices())
        {
            yield return new object[] { new Rect(1, 2, 3, 4), matrix, new Rect(1, 2, 3, 4) };
        }

        // Scale.
        yield return new object[] { new Rect(1, 2, 3, 4), new Matrix(2, 0, 0, 1, 0, 0), new Rect(2, 2, 6, 4) };
        yield return new object[] { new Rect(1, 2, 3, 4), new Matrix(1, 0, 0, 2, 0, 0), new Rect(1, 4, 3, 8) };
        yield return new object[] { new Rect(1, 2, 3, 4), new Matrix(0, 0, 0, 3, 0, 0), new Rect(0, 6, 0, 12) };
        yield return new object[] { new Rect(1, 2, 3, 4), new Matrix(2, 0, 0, 0, 0, 0), new Rect(2, 0, 6, 0) };
        yield return new object[] { new Rect(1, 2, 3, 4), new Matrix(2, 0, 0, 3, 0, 0), new Rect(2, 6, 6, 12) };
        yield return new object[] { new Rect(1, 2, 3, 4), new Matrix(-2, 0, 0, -3, 0, 0), new Rect(-8, -18, 6, 12) };

        // Skew.
        yield return new object[] { new Rect(1, 2, 3, 4), new Matrix(1, 2, 0, 1, 0, 0), new Rect(1, 4, 3, 10) };
        yield return new object[] { new Rect(1, 2, 3, 4), new Matrix(1, 0, 3, 1, 0, 0), new Rect(7, 2, 15, 4) };
        yield return new object[] { new Rect(1, 2, 3, 4), new Matrix(1, 2, 3, 1, 0, 0), new Rect(7, 4, 15, 10) };

        // Translate.
        yield return new object[] { new Rect(1, 2, 3, 4), new Matrix(1, 0, 0, 1, 2, 0), new Rect(3, 2, 3, 4) };
        yield return new object[] { new Rect(1, 2, 3, 4), new Matrix(1, 0, 0, 1, 0, 3), new Rect(1, 5, 3, 4) };
        yield return new object[] { new Rect(1, 2, 3, 4), new Matrix(1, 0, 0, 1, 2, 3), new Rect(3, 5, 3, 4) };
        yield return new object[] { new Rect(1, 2, 3, 4), new Matrix(1, 0, 0, 1, -2, 0), new Rect(-1, 2, 3, 4) };
        yield return new object[] { new Rect(1, 2, 3, 4), new Matrix(1, 0, 0, 1, 0, -3), new Rect(1, -1, 3, 4) };
        yield return new object[] { new Rect(1, 2, 3, 4), new Matrix(1, 0, 0, 1, -2, -3), new Rect(-1, -1, 3, 4) };

        // Translate + Scale.
        yield return new object[] { new Rect(1, 2, 3, 4), new Matrix(2, 0, 0, 3, 4, 5), new Rect(6, 11, 6, 12) };

        // Translate + Skew.
        yield return new object[] { new Rect(1, 2, 3, 4), new Matrix(1, 2, 3, 1, 4, 5), new Rect(11, 9, 15, 10) };

        // Skew + Scale.
        yield return new object[] { new Rect(1, 2, 3, 4), new Matrix(2, 3, 4, 5, 0, 0), new Rect(10, 13, 22, 29) };

        // Complex.
        yield return new object[] { new Rect(1, 2, 3, 4), new Matrix(2, 3, 4, 5, 6, 7), new Rect(16, 20, 22, 29) };
        yield return new object[] { new Rect(1, 2, 3, 4), new Matrix(0, 0, 0, 0, 0, 0), new Rect(0, 0, 0, 0) };
        yield return new object[] { new Rect(1.1, 2.2, 2.3, 3.3), new Matrix(2, 3, 4, 5, 6, 7), new Rect(17, 21.3, 17.8, 23.4) };

        // Other cases.
        yield return new object[] { new Rect(-1, -2, 1, 2), Matrix.Identity, new Rect(-1, -2, 1, 2) };
        yield return new object[] { new Rect(-1, -2, 1, 2), new Matrix(), new Rect(-1, -2, 1, 2) };
        yield return new object[] { new Rect(-1, -2, 1, 2), new Matrix(1, 0, 0, 1, 0, 0), new Rect(-1, -2, 1, 2) };
        yield return new object[] { new Rect(-1, -2, 1, 2), new Matrix(2, 3, 4, 5, 6, 7), new Rect(-4, -6, 10, 13) };

        // Zero.
        yield return new object[] { new Rect(0, 0, 0, 0), Matrix.Identity, new Rect() };
        yield return new object[] { new Rect(0, 0, 0, 0), new Matrix(), new Rect() };
        yield return new object[] { new Rect(0, 0, 0, 0), new Matrix(1, 0, 0, 1, 0, 0), new Rect() };
        yield return new object[] { new Rect(0, 0, 0, 0), new Matrix(2, 3, 4, 5, 6, 7), new Rect(6, 7, 0, 0) };
        
        // Default.
        yield return new object[] { new Rect(), Matrix.Identity, new Rect() };
        yield return new object[] { new Rect(), new Matrix(), new Rect() };
        yield return new object[] { new Rect(), new Matrix(1, 0, 0, 1, 0, 0), new Rect() };
        yield return new object[] { new Rect(), new Matrix(2, 3, 4, 5, 6, 7), new Rect(6, 7, 0, 0) };
        
        // Empty.
        yield return new object[] { Rect.Empty, Matrix.Identity, Rect.Empty };
        yield return new object[] { Rect.Empty, new Matrix(), Rect.Empty };
        yield return new object[] { Rect.Empty, new Matrix(1, 0, 0, 1, 0, 0), Rect.Empty };
        yield return new object[] { Rect.Empty, new Matrix(2, 3, 4, 5, 6, 7), Rect.Empty };
    }

    [Theory]
    [MemberData(nameof(Transform_TestData))]
    public void Transform_Invoke_ReturnsExpected(Rect rect, Matrix matrix, Rect expected)
    {
        Rect copy = rect;

        Helpers.AssertEqualRounded(expected, Rect.Transform(rect, matrix), precision: 5);
        Assert.Equal(copy, rect);

        rect.Transform(matrix);
        Helpers.AssertEqualRounded(expected, rect, precision: 5);
    }

    public static IEnumerable<object[]> Union_Point_TestData()
    {
        // Normal.
        yield return new object[] { new Rect(1, 2, 3, 4), new Point(1, 2), new Rect(1, 2, 3, 4) };
        yield return new object[] { new Rect(1, 2, 3, 4), new Point(1, 6), new Rect(1, 2, 3, 4) };
        yield return new object[] { new Rect(1, 2, 3, 4), new Point(4, 2), new Rect(1, 2, 3, 4) };
        yield return new object[] { new Rect(1, 2, 3, 4), new Point(4, 6), new Rect(1, 2, 3, 4) };
        yield return new object[] { new Rect(1, 2, 3, 4), new Point(2, 3), new Rect(1, 2, 3, 4) };
        yield return new object[] { new Rect(1, 2, 3, 4), new Point(2, 3), new Rect(1, 2, 3, 4) };
        yield return new object[] { new Rect(1, 2, 3, 4), new Point(-1, -2), new Rect(-1, -2, 5, 8) };
        yield return new object[] { new Rect(1, 2, 3, 4), new Point(0, 0), new Rect(0, 0, 4, 6) };
        yield return new object[] { new Rect(1, 2, 3, 4), new Point(5, 7), new Rect(1, 2, 4, 5) };
        yield return new object[] { new Rect(1, 2, 3, 4), new Point(), new Rect(0, 0, 4, 6) };

        // Infinite width.
        yield return new object[] { new Rect(1, 2, double.PositiveInfinity, 4), new Point(1, 2), new Rect(1, 2, double.PositiveInfinity, 4) };
        yield return new object[] { new Rect(1, 2, double.PositiveInfinity, 4), new Point(double.MaxValue, 2), new Rect(1, 2, double.PositiveInfinity, 4) };
        yield return new object[] { new Rect(1, 2, double.PositiveInfinity, 4), new Point(double.PositiveInfinity, 2), new Rect(1, 2, double.PositiveInfinity, 4) };
        yield return new object[] { new Rect(1, 2, double.PositiveInfinity, 4), new Point(double.NaN, 2), new Rect(double.NaN, 2, double.PositiveInfinity, 4) };
        yield return new object[] { new Rect(1, 2, 3, 4), new Point(double.PositiveInfinity, 2), new Rect(1, 2, double.NaN, 4) };

        // Infinite height.
        yield return new object[] { new Rect(1, 2, 3, double.PositiveInfinity), new Point(1, 2), new Rect(1, 2, 3, double.PositiveInfinity) };
        yield return new object[] { new Rect(1, 2, 3, double.PositiveInfinity), new Point(1, double.MaxValue), new Rect(1, 2, 3, double.PositiveInfinity) };
        yield return new object[] { new Rect(1, 2, 3, double.PositiveInfinity), new Point(1, double.PositiveInfinity), new Rect(1, 2, 3, double.PositiveInfinity) };
        yield return new object[] { new Rect(1, 2, 3, double.PositiveInfinity), new Point(1, double.NaN), new Rect(1, double.NaN, 3, double.PositiveInfinity) };
        yield return new object[] { new Rect(1, 2, 3, 4), new Point(1, double.PositiveInfinity), new Rect(1, 2, 3, double.NaN) };

        // Infinite width & height.
        yield return new object[] { new Rect(1, 2, double.PositiveInfinity, double.PositiveInfinity), new Point(1, 2), new Rect(1, 2, double.PositiveInfinity, double.PositiveInfinity) };
        yield return new object[] { new Rect(1, 2, double.PositiveInfinity, double.PositiveInfinity), new Point(double.MaxValue, double.MaxValue), new Rect(1, 2, double.PositiveInfinity, double.PositiveInfinity) };
        yield return new object[] { new Rect(1, 2, double.PositiveInfinity, double.PositiveInfinity), new Point(double.PositiveInfinity, double.PositiveInfinity), new Rect(1, 2, double.PositiveInfinity, double.PositiveInfinity) };
        yield return new object[] { new Rect(1, 2, double.PositiveInfinity, double.PositiveInfinity), new Point(double.NaN, double.NaN), new Rect(double.NaN, double.NaN, double.PositiveInfinity, double.PositiveInfinity) };
        yield return new object[] { new Rect(1, 2, 3, 4), new Point(double.PositiveInfinity, double.PositiveInfinity), new Rect(1, 2, double.NaN, double.NaN) };

        // NaN width.
        yield return new object[] { new Rect(1, 2, double.NaN, 4), new Point(1, 2), new Rect(1, 2, double.NaN, 4) };
        yield return new object[] { new Rect(1, 2, double.NaN, 4), new Point(double.MaxValue, 2), new Rect(1, 2, double.NaN, 4) };
        yield return new object[] { new Rect(1, 2, double.NaN, 4), new Point(double.PositiveInfinity, 2), new Rect(1, 2, double.NaN, 4) };
        yield return new object[] { new Rect(1, 2, double.NaN, 4), new Point(double.NaN, 2), new Rect(double.NaN, 2, double.NaN, 4) };
        yield return new object[] { new Rect(1, 2, 3, 4), new Point(double.NaN, 2), new Rect(double.NaN, 2, double.NaN, 4) };

        // NaN height.
        yield return new object[] { new Rect(1, 2, 3, double.NaN), new Point(1, 2), new Rect(1, 2, 3, double.NaN) };
        yield return new object[] { new Rect(1, 2, 3, double.NaN), new Point(1, double.MaxValue), new Rect(1, 2, 3, double.NaN) };
        yield return new object[] { new Rect(1, 2, 3, double.NaN), new Point(1, double.PositiveInfinity), new Rect(1, 2, 3, double.NaN) };
        yield return new object[] { new Rect(1, 2, 3, double.NaN), new Point(1, double.NaN), new Rect(1, double.NaN, 3, double.NaN) };
        yield return new object[] { new Rect(1, 2, 3, 4), new Point(1, double.NaN), new Rect(1, double.NaN, 3, double.NaN) };

        // NaN width & height.
        yield return new object[] { new Rect(1, 2, double.NaN, double.NaN), new Point(1, 2), new Rect(1, 2, double.NaN, double.NaN) };
        yield return new object[] { new Rect(1, 2, double.NaN, double.NaN), new Point(double.MaxValue, double.MaxValue), new Rect(1, 2, double.NaN, double.NaN) };
        yield return new object[] { new Rect(1, 2, double.NaN, double.NaN), new Point(double.PositiveInfinity, double.PositiveInfinity), new Rect(1, 2, double.NaN, double.NaN) };
        yield return new object[] { new Rect(1, 2, double.NaN, double.NaN), new Point(double.NaN, double.NaN), new Rect(double.NaN, double.NaN, double.NaN, double.NaN) };
        yield return new object[] { new Rect(1, 2, 3, 4), new Point(double.NaN, double.NaN), new Rect(double.NaN, double.NaN, double.NaN, double.NaN) };

        // Zero.
        yield return new object[] { new Rect(0, 0, 0, 0), new Point(1, 2), new Rect(0, 0, 1, 2) };
        yield return new object[] { new Rect(0, 0, 0, 0), new Point(-1, -2), new Rect(-1, -2, 1, 2) };
        yield return new object[] { new Rect(0, 0, 0, 0), new Point(0, 0), new Rect(0, 0, 0, 0) };
        yield return new object[] { new Rect(0, 0, 0, 0), new Point(), new Rect(0, 0, 0, 0) };

        // Default.
        yield return new object[] { new Rect(), new Point(1, 2), new Rect(0, 0, 1, 2) };
        yield return new object[] { new Rect(), new Point(-1, -2), new Rect(-1, -2, 1, 2) };
        yield return new object[] { new Rect(), new Point(0, 0), new Rect(0, 0, 0, 0) };
        yield return new object[] { new Rect(), new Point(), new Rect(0, 0, 0, 0) };

        // Empty.
        yield return new object[] {Rect.Empty, new Point(1, 2), new Rect(1, 2, 0, 0) };
        yield return new object[] {Rect.Empty, new Point(-1, -2), new Rect(-1, -2, 0, 0) };
        yield return new object[] {Rect.Empty, new Point(0, 0), new Rect(0, 0, 0, 0) };
        yield return new object[] {Rect.Empty, new Point(), new Rect(0, 0, 0, 0) };
    }

    [Theory]
    [MemberData(nameof(Union_Point_TestData))]
    public void Union_InvokePoint_ReturnsExpected(Rect rect, Point point, Rect expected)
    {
        Rect copy = rect;

        Assert.Equal(expected, Rect.Union(rect, point));
        Assert.Equal(copy, rect);

        rect.Union(point);
        Assert.Equal(expected, rect);
    }

    public static IEnumerable<object[]> Union_Rect_TestData()
    {
        // Normal.
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(1, 2, 3, 4), new Rect(1, 2, 3, 4) };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(1, 2, 0, 4), new Rect(1, 2, 3, 4) };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(1, 2, 3, 0), new Rect(1, 2, 3, 4) };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(1, 2, 0, 0), new Rect(1, 2, 3, 4) };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(2, 3, 1, 2), new Rect(1, 2, 3, 4) };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(2, 3, 2, 3), new Rect(1, 2, 3, 4) };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(2, 3, 3, 4), new Rect(1, 2, 4, 5) };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(-1, -2, 1, 2), new Rect(-1, -2, 5, 8) };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(-1, -2, 2, 4), new Rect(-1, -2, 5, 8) };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(-1, -2, 7, 9), new Rect(-1, -2, 7, 9) };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(0, 0, 0, 0), new Rect(0, 0, 4, 6) };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(), new Rect(0, 0, 4, 6) };
        yield return new object[] { new Rect(1, 2, 3, 4), Rect.Empty, new Rect(1, 2, 3, 4) };

        // Infinite width.
        yield return new object[] { new Rect(1, 2, double.PositiveInfinity, 4), new Rect(1, 2, 3, 4), new Rect(1, 2, double.PositiveInfinity, 4) };
        yield return new object[] { new Rect(1, 2, double.PositiveInfinity, 4), new Rect(1, 2, double.MaxValue, 4), new Rect(1, 2, double.PositiveInfinity, 4) };
        yield return new object[] { new Rect(1, 2, double.PositiveInfinity, 4), new Rect(1, 2, double.PositiveInfinity, 4), new Rect(1, 2, double.PositiveInfinity, 4) };
        yield return new object[] { new Rect(1, 2, double.PositiveInfinity, 4), new Rect(1, 2, double.NaN, 4), new Rect(1, 2, double.PositiveInfinity, 4) };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(1, 2, double.PositiveInfinity, 4), new Rect(1, 2, double.PositiveInfinity, 4) };

        // Infinite height.
        yield return new object[] { new Rect(1, 2, 3, double.PositiveInfinity), new Rect(1, 2, 3, 4), new Rect(1, 2, 3, double.PositiveInfinity) };
        yield return new object[] { new Rect(1, 2, 3, double.PositiveInfinity), new Rect(1, 2, 3, double.MaxValue), new Rect(1, 2, 3, double.PositiveInfinity) };
        yield return new object[] { new Rect(1, 2, 3, double.PositiveInfinity), new Rect(1, 2, 3, double.PositiveInfinity), new Rect(1, 2, 3, double.PositiveInfinity) };
        yield return new object[] { new Rect(1, 2, 3, double.PositiveInfinity), new Rect(1, 2, 3, double.NaN), new Rect(1, 2, 3, double.PositiveInfinity) };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(1, 2, 3, double.PositiveInfinity), new Rect(1, 2, 3, double.PositiveInfinity) };

        // Infinite width & height.
        yield return new object[] { new Rect(1, 2, double.PositiveInfinity, double.PositiveInfinity), new Rect(1, 2, 3, 4), new Rect(1, 2, double.PositiveInfinity, double.PositiveInfinity) };
        yield return new object[] { new Rect(1, 2, double.PositiveInfinity, double.PositiveInfinity), new Rect(1, 2, double.MaxValue, double.MaxValue), new Rect(1, 2, double.PositiveInfinity, double.PositiveInfinity) };
        yield return new object[] { new Rect(1, 2, double.PositiveInfinity, double.PositiveInfinity), new Rect(1, 2, double.PositiveInfinity, double.PositiveInfinity), new Rect(1, 2, double.PositiveInfinity, double.PositiveInfinity) };
        yield return new object[] { new Rect(1, 2, double.PositiveInfinity, double.PositiveInfinity), new Rect(1, 2, double.NaN, double.NaN), new Rect(1, 2, double.PositiveInfinity, double.PositiveInfinity) };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(1, 2, double.PositiveInfinity, double.PositiveInfinity), new Rect(1, 2, double.PositiveInfinity, double.PositiveInfinity) };

        // NaN width.
        yield return new object[] { new Rect(1, 2, double.NaN, 4), new Rect(1, 2, 3, 4), new Rect(1, 2, double.NaN, 4) };
        yield return new object[] { new Rect(1, 2, double.NaN, 4), new Rect(1, 2, double.MaxValue, 4), new Rect(1, 2, double.NaN, 4) };
        yield return new object[] { new Rect(1, 2, double.NaN, 4), new Rect(1, 2, double.PositiveInfinity, 4), new Rect(1, 2, double.PositiveInfinity, 4) };
        yield return new object[] { new Rect(1, 2, double.NaN, 4), new Rect(1, 2, double.NaN, 4), new Rect(1, 2, double.NaN, 4) };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(1, 2, double.NaN, 4), new Rect(1, 2, double.NaN, 4) };

        // NaN height.
        yield return new object[] { new Rect(1, 2, 3, double.NaN), new Rect(1, 2, 3, 4), new Rect(1, 2, 3, double.NaN) };
        yield return new object[] { new Rect(1, 2, 3, double.NaN), new Rect(1, 2, 3, double.MaxValue), new Rect(1, 2, 3, double.NaN) };
        yield return new object[] { new Rect(1, 2, 3, double.NaN), new Rect(1, 2, 3, double.PositiveInfinity), new Rect(1, 2, 3, double.PositiveInfinity) };
        yield return new object[] { new Rect(1, 2, 3, double.NaN), new Rect(1, 2, 3, double.NaN), new Rect(1, 2, 3, double.NaN) };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(1, 2, 3, double.NaN), new Rect(1, 2, 3, double.NaN) };

        // NaN width & height.
        yield return new object[] { new Rect(1, 2, double.NaN, double.NaN), new Rect(1, 2, 3, 4), new Rect(1, 2, double.NaN, double.NaN) };
        yield return new object[] { new Rect(1, 2, double.NaN, double.NaN), new Rect(1, 2, double.MaxValue, double.MaxValue), new Rect(1, 2, double.NaN, double.NaN) };
        yield return new object[] { new Rect(1, 2, double.NaN, double.NaN), new Rect(1, 2, double.PositiveInfinity, double.PositiveInfinity), new Rect(1, 2, double.PositiveInfinity, double.PositiveInfinity) };
        yield return new object[] { new Rect(1, 2, double.NaN, double.NaN), new Rect(1, 2, double.NaN, double.NaN), new Rect(1, 2, double.NaN, double.NaN) };
        yield return new object[] { new Rect(1, 2, 3, 4), new Rect(1, 2, double.NaN, double.NaN), new Rect(1, 2, double.NaN, double.NaN) };

        // Zero.
        yield return new object[] { new Rect(0, 0, 0, 0), new Rect(1, 2, 3, 4), new Rect(0, 0, 4, 6) };
        yield return new object[] { new Rect(0, 0, 0, 0), new Rect(1, 2, 0, 4), new Rect(0, 0, 1, 6) };
        yield return new object[] { new Rect(0, 0, 0, 0), new Rect(1, 2, 3, 0), new Rect(0, 0, 4, 2) };
        yield return new object[] { new Rect(0, 0, 0, 0), new Rect(1, 0, 0, 0), new Rect(0, 0, 1, 0) };
        yield return new object[] { new Rect(0, 0, 0, 0), new Rect(0, 1, 0, 0), new Rect(0, 0, 0, 1) };
        yield return new object[] { new Rect(0, 0, 0, 0), new Rect(0, 0, 1, 0), new Rect(0, 0, 1, 0) };
        yield return new object[] { new Rect(0, 0, 0, 0), new Rect(0, 0, 0, 1), new Rect(0, 0, 0, 1) };
        yield return new object[] { new Rect(0, 0, 0, 0), new Rect(0, 0, 0, 0), new Rect(0, 0, 0, 0) };
        yield return new object[] { new Rect(0, 0, 0, 0), new Rect(), new Rect(0, 0, 0, 0) };
        yield return new object[] { new Rect(0, 0, 0, 0), Rect.Empty, new Rect(0, 0, 0, 0) };

        // Default.
        yield return new object[] { new Rect(), new Rect(1, 2, 3, 4), new Rect(0, 0, 4, 6) };
        yield return new object[] { new Rect(), new Rect(1, 2, 0, 4), new Rect(0, 0, 1, 6) };
        yield return new object[] { new Rect(), new Rect(1, 2, 3, 0), new Rect(0, 0, 4, 2) };
        yield return new object[] { new Rect(), new Rect(1, 0, 0, 0), new Rect(0, 0, 1, 0) };
        yield return new object[] { new Rect(), new Rect(0, 1, 0, 0), new Rect(0, 0, 0, 1) };
        yield return new object[] { new Rect(), new Rect(0, 0, 1, 0), new Rect(0, 0, 1, 0) };
        yield return new object[] { new Rect(), new Rect(0, 0, 0, 1), new Rect(0, 0, 0, 1) };
        yield return new object[] { new Rect(), new Rect(0, 0, 0, 0), new Rect(0, 0, 0, 0) };
        yield return new object[] { new Rect(), new Rect(), new Rect(0, 0, 0, 0) };
        yield return new object[] { new Rect(), Rect.Empty, new Rect(0, 0, 0, 0) };

        // Empty.
        yield return new object[] { Rect.Empty, new Rect(1, 2, 3, 4), new Rect(1, 2, 3, 4) };
        yield return new object[] { Rect.Empty, new Rect(1, 2, 0, 4), new Rect(1, 2, 0, 4) };
        yield return new object[] { Rect.Empty, new Rect(1, 2, 3, 0), new Rect(1, 2, 3, 0) };
        yield return new object[] { Rect.Empty, new Rect(1, 0, 0, 0), new Rect(1, 0, 0, 0) };
        yield return new object[] { Rect.Empty, new Rect(0, 1, 0, 0), new Rect(0, 1, 0, 0) };
        yield return new object[] { Rect.Empty, new Rect(0, 0, 1, 0), new Rect(0, 0, 1, 0) };
        yield return new object[] { Rect.Empty, new Rect(0, 0, 0, 1), new Rect(0, 0, 0, 1) };
        yield return new object[] { Rect.Empty, new Rect(0, 0, 0, 0), new Rect(0, 0, 0, 0) };
        yield return new object[] { Rect.Empty, new Rect(), new Rect(0, 0, 0, 0) };
        yield return new object[] { Rect.Empty, Rect.Empty, Rect.Empty };
    }

    [Theory]
    [MemberData(nameof(Union_Rect_TestData))]
    public void Union_InvokeRect_ReturnsExpected(Rect rect1, Rect rect2, Rect expected)
    {
        Rect copy1 = rect1;
        Rect copy2 = rect2;

        Assert.Equal(expected, Rect.Union(rect1, rect2));
        Assert.Equal(copy1, rect1);
        Assert.Equal(copy2, rect2);

        Assert.Equal(expected, Rect.Union(rect2, rect1));
        Assert.Equal(copy1, rect1);
        Assert.Equal(copy2, rect2);
        
        rect1.Union(rect2);
        Assert.Equal(expected, rect1);
        Assert.Equal(copy2, rect2);
    }

    [Fact]
    public void TypeConverter_Get_ReturnsExpected()
    {
        Assert.IsType<RectConverter>(TypeDescriptor.GetConverter(typeof(Rect)));
    }

    [Fact]
    public void ValueSerializer_Get_ReturnsExpected()
    {
        Assert.IsType<RectValueSerializer>(ValueSerializer.GetSerializerFor(typeof(Rect)));
    }
}
