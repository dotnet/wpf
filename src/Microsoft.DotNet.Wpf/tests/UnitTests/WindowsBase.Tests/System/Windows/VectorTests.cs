// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel;
using System.Globalization;
using System.Windows.Converters;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Tests;

namespace System.Windows.Tests;

public class VectorTests
{
    [Fact]
    public void Ctor_Default()
    {
        var vector = new Vector();
        Assert.Equal(0, vector.X);
        Assert.Equal(0, vector.Y);
    }

    [Theory]
    [InlineData(double.NegativeInfinity, double.NegativeInfinity)]
    [InlineData(double.MinValue, double.MinValue)]
    [InlineData(-1, -2)]
    [InlineData(-1, 2)]
    [InlineData(1, -2)]
    [InlineData(double.NegativeZero, double.NegativeZero)]
    [InlineData(0, 0)]
    [InlineData(1, 0)]
    [InlineData(0, 2)]
    [InlineData(0.1, 0.2)]
    [InlineData(1, 2)]
    [InlineData(double.MaxValue, double.MaxValue)]
    [InlineData(double.PositiveInfinity, double.PositiveInfinity)]
    [InlineData(double.NaN, double.NaN)]
    [InlineData(1, double.NaN)]
    [InlineData(double.NaN, 2)]
    public void Ctor_Double_Double(double x, double y)
    {
        var vector = new Vector(x, y);
        Assert.Equal(x, vector.X);
        Assert.Equal(y, vector.Y);
    }

    public static IEnumerable<object[]> Length_TestData()
    {
        yield return new object[] { 0, 0, 0 };
        yield return new object[] { 1, 2, Math.Sqrt(5) };
        yield return new object[] { 1.1, 2.2, Math.Sqrt(6.05) };
        yield return new object[] { -1.1, -2.2, Math.Sqrt(6.05) };
        yield return new object[] { double.MaxValue, double.MaxValue, Math.Sqrt(double.PositiveInfinity) };
    }

    [Theory]
    [MemberData(nameof(Length_TestData))]
    public void Length_Get_ReturnsExpected(double x, double y, double expected)
    {
        var vector = new Vector(x, y);
        Assert.Equal(expected, vector.Length, precision: 5);
        Assert.Equal(Math.Sqrt(vector.LengthSquared), vector.Length, precision: 5);
    }

    [Theory]
    [MemberData(nameof(Length_TestData))]
    public void LengthSquared_Get_ReturnsExpected(double x, double y, double expected)
    {
        var vector = new Vector(x, y);
        Assert.Equal(expected * expected, vector.LengthSquared, precision: 5);
        Assert.Equal(vector.Length * vector.Length, vector.LengthSquared, precision: 5);
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
        var vector = new Vector
        {
            // Set.
            X = value
        };
        Assert.Equal(value, vector.X);

        // Set same.
        vector.X = value;
        Assert.Equal(value, vector.X);
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
        var vector = new Vector
        {
            // Set.
            Y = value
        };
        Assert.Equal(value, vector.Y);

        // Set same.
        vector.Y = value;
        Assert.Equal(value, vector.Y);
    }

    public static IEnumerable<object[]> AngleBetween_TestData()
    {
        yield return new object[] { new Vector(1, 2), new Vector(1, 2), 0 };
        yield return new object[] { new Vector(1, 2), new Vector(3, 4), -10.30485 };
        yield return new object[] { new Vector(1, 2), new Vector(-3, -4), 169.69515 };
        yield return new object[] { new Vector(1, 2), new Vector(1, 0), -63.43495 };
        yield return new object[] { new Vector(1, 2), new Vector(0, 1), 26.56505 };
        yield return new object[] { new Vector(1, 2), new Vector(0, -1), -153.43495 };
        yield return new object[] { new Vector(1, 2), new Vector(-1, 0), 116.56505 };
        yield return new object[] { new Vector(1, 2), new Vector(-1, -2), 180 };
        yield return new object[] { new Vector(1, 2), new Vector(1, -2), -126.8699 };
        yield return new object[] { new Vector(1, 2), new Vector(), 0 };
        yield return new object[] { new Vector(-1, -2), new Vector(-3, -4), -10.30485 };

        yield return new object[] { new Vector(), new Vector(), 0 };
        yield return new object[] { new Vector(), new Vector(1, 2), 0 };
    }

    [Theory]
    [MemberData(nameof(AngleBetween_TestData))]
    public void AngleBetween_Invoke_ReturnsExpected(Vector vector1, Vector vector2, double expected)
    {
        Assert.Equal(expected, Vector.AngleBetween(vector1, vector2), precision: 5);
    }

    public static IEnumerable<object[]> CrossProduct_TestData()
    {
        yield return new object[] { new Vector(1, 2), new Vector(3, 4), -2 };
        yield return new object[] { new Vector(1.1, 2.2), new Vector(3.3, 4.4), -2.42 };
        yield return new object[] { new Vector(1, 2), new Vector(-3, -4), 2 };
        yield return new object[] { new Vector(1, 2), new Vector(1, 2), 0 };
        yield return new object[] { new Vector(1, 2), new Vector(0, 0), 0 };
        yield return new object[] { new Vector(1, 2), new Vector(double.MaxValue, double.MaxValue), double.NegativeInfinity };
        yield return new object[] { new Vector(1, 2), new Vector(double.PositiveInfinity, double.PositiveInfinity), double.NaN };
        yield return new object[] { new Vector(1, 2), new Vector(double.NaN, double.NaN), double.NaN };
        yield return new object[] { new Vector(-1, -2), new Vector(-3, -4), -2 };

        yield return new object[] { new Vector(), new Vector(), 0 };
        yield return new object[] { new Vector(), new Vector(1, 2), 0 };
    }

    [Theory]
    [MemberData(nameof(CrossProduct_TestData))]
    public void CrossProduct_Invoke_ReturnsExpected(Vector vector1, Vector vector2, double expected)
    {
        Assert.Equal(expected, Vector.CrossProduct(vector1, vector2), precision: 5);
        Assert.Equal(-expected, Vector.CrossProduct(vector2, vector1), precision: 5);
    }

    public static IEnumerable<object?[]> Equals_TestData()
    {
        // Normal size.
        yield return new object?[] { new Vector(1, 2), new Vector(1, 2), true };
        yield return new object?[] { new Vector(1, 2), new Vector(2, 2), false };
        yield return new object?[] { new Vector(1, 2), new Vector(1, 3), false };
        yield return new object?[] { new Vector(1, 2), new Vector(double.NaN, 2), false };
        yield return new object?[] { new Vector(1, 2), new Vector(1, double.NaN), false };
        yield return new object?[] { new Vector(1, 2), new Vector(double.NaN, double.NaN), false };
        yield return new object?[] { new Vector(1, 2), new Vector(0, 0), false };
        yield return new object?[] { new Vector(1, 2), new Vector(), false };

        // NaN x.
        yield return new object[] { new Vector(double.NaN, 2), new Vector(double.NaN, 2), true };
        yield return new object[] { new Vector(double.NaN, 2), new Vector(1, 2), false };
        yield return new object[] { new Vector(double.NaN, 2), new Vector(double.NaN, double.NaN), false };

        // NaN y.
        yield return new object[] { new Vector(1, double.NaN), new Vector(1, double.NaN), true };
        yield return new object[] { new Vector(1, double.NaN), new Vector(1, 2), false };
        yield return new object[] { new Vector(1, double.NaN), new Vector(double.NaN, double.NaN), false };

        // NaN x & y.
        yield return new object[] { new Vector(double.NaN, double.NaN), new Vector(double.NaN, double.NaN), true };
        yield return new object[] { new Vector(double.NaN, double.NaN), new Vector(1, 2), false };
        yield return new object[] { new Vector(double.NaN, double.NaN), new Vector(double.NaN, 2), false };
        yield return new object[] { new Vector(double.NaN, double.NaN), new Vector(1, double.NaN), false };

        // Zero.
        yield return new object?[] { new Vector(0, 0), new Vector(), true };
        yield return new object?[] { new Vector(0, 0), new Vector(0, 0), true };
        yield return new object?[] { new Vector(0, 0), new Vector(1, 0), false };
        yield return new object?[] { new Vector(0, 0), new Vector(0, 1), false };

        // Default.
        yield return new object?[] { new Vector(), new Vector(), true };
        yield return new object?[] { new Vector(), new Vector(0, 0), true };
        yield return new object?[] { new Vector(), new Vector(1, 0), false };
        yield return new object?[] { new Vector(), new Vector(0, 1), false };
    }

    [Theory]
    [MemberData(nameof(Equals_TestData))]
    public void Equals_Object_ReturnsExpected(Vector vector, object o, bool expected)
    {
        Assert.Equal(expected, vector.Equals(o));
        if (o is Vector other)
        {
            Assert.Equal(expected, vector.Equals(other));
            Assert.Equal(expected, other.Equals(vector));
            Assert.Equal(expected, Vector.Equals(vector, other));
            Assert.Equal(expected, Vector.Equals(other, vector));
            Assert.Equal(expected, vector.GetHashCode().Equals(other.GetHashCode()));
        }
    }

    public static IEnumerable<object[]> EqualityOperator_TestData()
    {
        // Normal size.
        yield return new object[] { new Vector(1, 2), new Vector(1, 2), true };
        yield return new object[] { new Vector(1, 2), new Vector(2, 2), false };
        yield return new object[] { new Vector(1, 2), new Vector(1, 3), false };
        yield return new object[] { new Vector(1, 2), new Vector(double.NaN, 2), false };
        yield return new object[] { new Vector(1, 2), new Vector(1, double.NaN), false };
        yield return new object[] { new Vector(1, 2), new Vector(double.NaN, double.NaN), false };
        yield return new object[] { new Vector(1, 2), new Vector(0, 0), false };
        yield return new object[] { new Vector(1, 2), new Vector(), false };

        // NaN x.
        yield return new object[] { new Vector(double.NaN, 2), new Vector(double.NaN, 2), false };
        yield return new object[] { new Vector(double.NaN, 2), new Vector(1, 2), false };
        yield return new object[] { new Vector(double.NaN, 2), new Vector(double.NaN, double.NaN), false };

        // NaN y.
        yield return new object[] { new Vector(1, double.NaN), new Vector(1, double.NaN), false };
        yield return new object[] { new Vector(1, double.NaN), new Vector(1, 2), false };
        yield return new object[] { new Vector(1, double.NaN), new Vector(double.NaN, double.NaN), false };

        // NaN x & y.
        yield return new object[] { new Vector(double.NaN, double.NaN), new Vector(double.NaN, double.NaN), false };
        yield return new object[] { new Vector(double.NaN, double.NaN), new Vector(1, 2), false };
        yield return new object[] { new Vector(double.NaN, double.NaN), new Vector(double.NaN, 2), false };
        yield return new object[] { new Vector(double.NaN, double.NaN), new Vector(1, double.NaN), false };

        // Zero.
        yield return new object[] { new Vector(0, 0), new Vector(), true };
        yield return new object[] { new Vector(0, 0), new Vector(0, 0), true };
        yield return new object[] { new Vector(0, 0), new Vector(1, 0), false };
        yield return new object[] { new Vector(0, 0), new Vector(0, 1), false };

        // Default.
        yield return new object[] { new Vector(), new Vector(), true };
        yield return new object[] { new Vector(), new Vector(0, 0), true };
        yield return new object[] { new Vector(), new Vector(1, 0), false };
        yield return new object[] { new Vector(), new Vector(0, 1), false };
    }

    [Theory]
    [MemberData(nameof(EqualityOperator_TestData))]
    public void EqualityOperator_Invoke_ReturnsExpected(Vector vector1, Vector vector2, bool expected)
    {
        Assert.Equal(expected, vector1 == vector2);
        Assert.Equal(expected, vector2 == vector1);
        Assert.Equal(!expected, vector1 != vector2);
        Assert.Equal(!expected, vector2 != vector1);
    }

    [Fact]
    public void GetHashCode_InvokeDefault_ReturnsEqual()
    {
        var vector = new Vector();
        Assert.Equal(0, vector.GetHashCode());
        Assert.Equal(vector.GetHashCode(), vector.GetHashCode());
    }

    [Fact]
    public void GetHashCode_InvokeNormal_ReturnsEqual()
    {
        var vector = new Vector(1, 2);
        Assert.NotEqual(0, vector.GetHashCode());
        Assert.Equal(vector.GetHashCode(), vector.GetHashCode());
    }

    [Fact]
    public void Normalize_InvokeDefault_ReturnsExpected()
    {
        var vector = new Vector();
        vector.Normalize();
        Assert.Equal(double.NaN, vector.X);
        Assert.Equal(double.NaN, vector.Y);
        Assert.Equal(double.NaN, vector.Length);
        Assert.Equal(double.NaN, vector.LengthSquared);
    }

    [Fact]
    public void Normalize_InvokeZero_ReturnsExpected()
    {
        var vector = new Vector(0, 0);
        vector.Normalize();
        Assert.Equal(double.NaN, vector.X);
        Assert.Equal(double.NaN, vector.Y);
        Assert.Equal(double.NaN, vector.Length);
        Assert.Equal(double.NaN, vector.LengthSquared);
    }

    [Fact]
    public void Normalize_InvokeNormal_ReturnsExpected()
    {
        var vector = new Vector(1, 2);
        vector.Normalize();
        Assert.Equal(1 / Math.Sqrt(5), vector.X, precision: 5);
        Assert.Equal(2 / Math.Sqrt(5), vector.Y, precision: 5);
        Assert.Equal(1, vector.Length, precision: 5);
        Assert.Equal(1, vector.LengthSquared, precision: 5);
    }

    [Fact]
    public void Normalize_InvokeNegative_ReturnsExpected()
    {
        var vector = new Vector(-1, -2);
        vector.Normalize();
        Assert.Equal(-1 / Math.Sqrt(5), vector.X, precision: 5);
        Assert.Equal(-2 / Math.Sqrt(5), vector.Y, precision: 5);
        Assert.Equal(1, vector.Length, precision: 5);
        Assert.Equal(1, vector.LengthSquared, precision: 5);
    }

    [Fact]
    public void Normalize_InvokeLarge_ReturnsExpected()
    {
        var vector = new Vector(double.MaxValue, double.MaxValue);
        vector.Normalize();
        Assert.Equal(0.70711, vector.X, precision: 5);
        Assert.Equal(0.70711, vector.Y, precision: 5);
        Assert.Equal(1, vector.Length, precision: 5);
        Assert.Equal(1, vector.LengthSquared, precision: 5);
    }

    public static IEnumerable<object[]> Add_Vector_TestData()
    {
        yield return new object[] { new Vector(1, 2), new Vector(3, 4), new Vector(4, 6) };
        yield return new object[] { new Vector(1, 2), new Vector(-3, -4), new Vector(-2, -2) };
        yield return new object[] { new Vector(1.1, 2.2), new Vector(3.3, 4.4), new Vector(4.4, 6.6) };
        yield return new object[] { new Vector(1, 2), new Vector(), new Vector(1, 2) };

        yield return new object[] { new Vector(), new Vector(), new Vector() };
        yield return new object[] { new Vector(), new Vector(1, 2), new Vector(1, 2) };
    }

    [Theory]
    [MemberData(nameof(Add_Vector_TestData))]
    public void Add_InvokeVector_ReturnsExpected(Vector vector1, Vector vector2, Vector expected)
    {
        Vector result = Vector.Add(vector1, vector2);
        Assert.Equal(expected.X, result.X, precision: 5);
        Assert.Equal(expected.Y, result.Y, precision: 5);
        Assert.Equal(result, Vector.Add(vector2, vector1));
    }

    [Theory]
    [MemberData(nameof(Add_Vector_TestData))]
    public void OperatorAdd_InvokeVector_ReturnsExpected(Vector vector1, Vector vector2, Vector expected)
    {
        Vector result = vector1 + vector2;
        Assert.Equal(expected.X, result.X, precision: 5);
        Assert.Equal(expected.Y, result.Y, precision: 5);
        Assert.Equal(result, vector2 + vector1);
    }

    public static IEnumerable<object[]> Add_Point_TestData()
    {
        yield return new object[] { new Vector(1, 2), new Point(3, 4), new Point(4, 6) };
        yield return new object[] { new Vector(1, 2), new Point(-3, -4), new Point(-2, -2) };
        yield return new object[] { new Vector(1.1, 2.2), new Point(3.3, 4.4), new Point(4.4, 6.6) };
        yield return new object[] { new Vector(1, 2), new Point(), new Point(1, 2) };

        yield return new object[] { new Vector(), new Point(), new Point() };
        yield return new object[] { new Vector(), new Point(1, 2), new Point(1, 2) };
    }

    [Theory]
    [MemberData(nameof(Add_Point_TestData))]
    public void Add_InvokePoint_ReturnsExpected(Vector vector, Point point, Point expected)
    {
        Point result = Vector.Add(vector, point);
        Assert.Equal(expected.X, result.X, precision: 5);
        Assert.Equal(expected.Y, result.Y, precision: 5);
    }

    [Theory]
    [MemberData(nameof(Add_Point_TestData))]
    public void OperatorAdd_InvokePoint_ReturnsExpected(Vector vector, Point point, Point expected)
    {
        Point result = vector + point;
        Assert.Equal(expected.X, result.X, precision: 5);
        Assert.Equal(expected.Y, result.Y, precision: 5);
    }

    public static IEnumerable<object[]> Determinant_TestData()
    {
        yield return new object[] { new Vector(1, 2), new Vector(3, 4), -2 };
        yield return new object[] { new Vector(1, 2), new Vector(-3, -4), 2 };
        yield return new object[] { new Vector(1.1, 2.2), new Vector(3.3, 4.4), -2.42 };
        yield return new object[] { new Vector(1, 2), new Vector(), 0 };

        yield return new object[] { new Vector(-1, -2), new Vector(3, 4), 2 };
        yield return new object[] { new Vector(-1, -2), new Vector(-3, -4), -2 };
        yield return new object[] { new Vector(-1, -2), new Vector(), 0 };

        yield return new object[] { new Vector(), new Vector(), 0 };
        yield return new object[] { new Vector(), new Vector(1, 2), 0 };
        yield return new object[] { new Vector(), new Vector(-1, -2), 0 };
    }

    [Theory]
    [MemberData(nameof(Determinant_TestData))]
    public void Determinant_Invoke_ReturnsExpected(Vector vector1, Vector vector2, double expected)
    {
        Assert.Equal(expected, Vector.Determinant(vector1, vector2), precision: 5);
    }

    public static IEnumerable<object[]> Divide_TestData()
    {
        yield return new object[] { new Vector(1, 2), 2, new Vector(0.5, 1) };
        yield return new object[] { new Vector(1, 2), 1, new Vector(1, 2) };
        yield return new object[] { new Vector(1, 2), -2, new Vector(-0.5, -1) };
        yield return new object[] { new Vector(1.1, 2.2), 2, new Vector(0.55, 1.1) };
        yield return new object[] { new Vector(1, 2), 0, new Vector(double.PositiveInfinity, double.PositiveInfinity) };

        yield return new object[] { new Vector(-1, -2), 2, new Vector(-0.5, -1) };
        yield return new object[] { new Vector(-1, -2), 1, new Vector(-1, -2) };
        yield return new object[] { new Vector(-1, -2), -2, new Vector(0.5, 1) };

        yield return new object[] { new Vector(), 2, new Vector() };
        yield return new object[] { new Vector(), 1, new Vector() };
        yield return new object[] { new Vector(), -2, new Vector() };
        yield return new object[] { new Vector(), 0, new Vector(double.NaN, double.NaN) };
    }

    [Theory]
    [MemberData(nameof(Divide_TestData))]
    public void Divide_Invoke_ReturnsExpected(Vector vector, double scalar, Vector expected)
    {
        Vector result = Vector.Divide(vector, scalar);
        Assert.Equal(expected.X, result.X, precision: 5);
        Assert.Equal(expected.Y, result.Y, precision: 5);
    }

    [Theory]
    [MemberData(nameof(Divide_TestData))]
    public void OperatorDivide_Invoke_ReturnsExpected(Vector vector, double scalar, Vector expected)
    {
        Vector result = vector / scalar;
        Assert.Equal(expected.X, result.X, precision: 5);
        Assert.Equal(expected.Y, result.Y, precision: 5);
    }

    public static IEnumerable<object[]> Subtract_Vector_TestData()
    {
        yield return new object[] { new Vector(1, 2), new Vector(3, 4), new Vector(-2, -2) };
        yield return new object[] { new Vector(1, 2), new Vector(-3, -4), new Vector(4, 6) };
        yield return new object[] { new Vector(1.1, 2.2), new Vector(3.3, 4.4), new Vector(-2.2, -2.2) };
        yield return new object[] { new Vector(1, 2), new Vector(), new Vector(1, 2) };

        yield return new object[] { new Vector(), new Vector(1, 2), new Vector(-1, -2) };
        yield return new object[] { new Vector(), new Vector(), new Vector() };
    }

    [Theory]
    [MemberData(nameof(Subtract_Vector_TestData))]
    public void Subtract_InvokeVector_ReturnsExpected(Vector vector1, Vector vector2, Vector expected)
    {
        Vector result = Vector.Subtract(vector1, vector2);
        Assert.Equal(expected.X, result.X, precision: 5);
        Assert.Equal(expected.Y, result.Y, precision: 5);
        Assert.Equal(new Vector(-result.X, -result.Y), Vector.Subtract(vector2, vector1));
    }

    [Theory]
    [MemberData(nameof(Subtract_Vector_TestData))]
    public void OperatorSubtract_InvokeVector_ReturnsExpected(Vector vector1, Vector vector2, Vector expected)
    {
        Vector result = vector1 - vector2;
        Assert.Equal(expected.X, result.X, precision: 5);
        Assert.Equal(expected.Y, result.Y, precision: 5);
        Assert.Equal(new Vector(-result.X, -result.Y), vector2 - vector1);
    }

    public static IEnumerable<object[]> Multiply_Double_TestData()
    {
        yield return new object[] { new Vector(1, 2), 2, new Vector(2, 4) };
        yield return new object[] { new Vector(1, 2), -2, new Vector(-2, -4) };
        yield return new object[] { new Vector(1.1, 2.2), 2, new Vector(2.2, 4.4) };
        yield return new object[] { new Vector(1, 2), 0, new Vector(0, 0) };
        yield return new object[] { new Vector(1, 2), 1, new Vector(1, 2) };
        yield return new object[] { new Vector(1, 2), double.PositiveInfinity, new Vector(double.PositiveInfinity, double.PositiveInfinity) };

        yield return new object[] { new Vector(-1, -2), 2, new Vector(-2, -4) };
        yield return new object[] { new Vector(-1, -2), -2, new Vector(2, 4) };
        yield return new object[] { new Vector(-1, -2), 0, new Vector(0, 0) };
        yield return new object[] { new Vector(-1, -2), 1, new Vector(-1, -2) };

        yield return new object[] { new Vector(), 2, new Vector() };
        yield return new object[] { new Vector(), -2, new Vector() };
        yield return new object[] { new Vector(), 0, new Vector() };
        yield return new object[] { new Vector(), 1, new Vector() };
    }

    [Theory]
    [MemberData(nameof(Multiply_Double_TestData))]
    public void Multiply_InvokeDouble_ReturnsExpected(Vector vector, double scalar, Vector expected)
    {
        Vector result = Vector.Multiply(vector, scalar);
        Assert.Equal(expected.X, result.X, precision: 5);
        Assert.Equal(expected.Y, result.Y, precision: 5);
        Assert.Equal(result, Vector.Multiply(scalar, vector));
    }

    [Theory]
    [MemberData(nameof(Multiply_Double_TestData))]
    public void OperatorMultiply_InvokeDouble_ReturnsExpected(Vector vector, double scalar, Vector expected)
    {
        Vector result = vector * scalar;
        Assert.Equal(expected.X, result.X, precision: 5);
        Assert.Equal(expected.Y, result.Y, precision: 5);
        Assert.Equal(result, scalar * vector);
    }

    [Theory]
    [MemberData(nameof(MatrixTests.Transform_Vector_TestData), MemberType = typeof(MatrixTests))]
    public void Multiply_InvokeMatrix_ReturnsExpected(Matrix matrix, Vector vector, Vector expected)
    {
        Vector result = Vector.Multiply(vector, matrix);
        Assert.Equal(expected.X, result.X, precision: 5);
        Assert.Equal(expected.Y, result.Y, precision: 5);
    }

    [Theory]
    [MemberData(nameof(MatrixTests.Transform_Vector_TestData), MemberType = typeof(MatrixTests))]
    public void OperatorMultiply_InvokeMatrix_ReturnsExpected(Matrix matrix, Vector vector, Vector expected)
    {
        Vector result = vector * matrix;
        Assert.Equal(expected.X, result.X, precision: 5);
        Assert.Equal(expected.Y, result.Y, precision: 5);
    }

    public static IEnumerable<object[]> Multiply_Vector_TestData()
    {
        yield return new object[] { new Vector(1, 2), new Vector(3, 4), 11 };
        yield return new object[] { new Vector(1, 2), new Vector(1, 2), 5 };
        yield return new object[] { new Vector(1, 2), new Vector(0, 0), 0 };
        yield return new object[] { new Vector(1, 2), new Vector(-1, -2), -5 };

        yield return new object[] { new Vector(-1, -2), new Vector(3, 4), -11 };
        yield return new object[] { new Vector(-1, -2), new Vector(-1, -2), 5 };
        yield return new object[] { new Vector(-1, -2), new Vector(0, 0), 0 };

        yield return new object[] { new Vector(), new Vector(3, 4), 0 };
        yield return new object[] { new Vector(), new Vector(), 0 };
        yield return new object[] { new Vector(), new Vector(-1, -2), 0 };
    }

    [Theory]
    [MemberData(nameof(Multiply_Vector_TestData))]
    public void Multiply_InvokeVector_ReturnsExpected(Vector vector1, Vector vector2, double expected)
    {
        double result = Vector.Multiply(vector1, vector2);
        Assert.Equal(expected, result, precision: 5);
        Assert.Equal(result, Vector.Multiply(vector1, vector2));
    }

    [Theory]
    [MemberData(nameof(Multiply_Vector_TestData))]
    public void OperatorMultiply_InvokeVector_ReturnsExpected(Vector vector1, Vector vector2, double expected)
    {
        double result = vector1 * vector2;
        Assert.Equal(expected, result, precision: 5);
        Assert.Equal(result, vector2 * vector1);
    }

    [Fact]
    public void Negate_InvokeEmpty_Success()
    {
        var vector = new Vector();
        vector.Negate();
        Assert.Equal(0, vector.X);
        Assert.Equal(0, vector.Y);
    }

    [Fact]
    public void Negate_InvokeNormal_Success()
    {
        var vector = new Vector(1, 2);
        vector.Negate();
        Assert.Equal(-1, vector.X);
        Assert.Equal(-2, vector.Y);
    }

    [Fact]
    public void OperatorNegate_InvokeEmpty_Success()
    {
        var vector = new Vector();
        Vector result = -vector;
        Assert.Equal(0, result.X);
        Assert.Equal(0, result.Y);
    }

    [Fact]
    public void OperatorNegate_InvokeNormal_Success()
    {
        var vector = new Vector(1, 2);
        Vector result = -vector;
        Assert.Equal(-1, result.X);
        Assert.Equal(-2, result.Y);
    }

    public static IEnumerable<object[]> Parse_TestData()
    {
        yield return new object[] { "0,0", new Vector(0, 0) };
        yield return new object[] { "1,2", new Vector(1, 2) };
        yield return new object[] { "1.1,2.2", new Vector(1.1, 2.2) };
        yield return new object[] { "   1   ,   2  ", new Vector(1, 2) };
        yield return new object[] { "-1,-2", new Vector(-1, -2) };
    }

    [Theory]
    [MemberData(nameof(Parse_TestData))]
    public void Parse_Invoke_Success(string source, Vector expected)
    {
        Vector result = Vector.Parse(source);
        Assert.Equal(expected.X, result.X, precision: 5);
        Assert.Equal(expected.Y, result.Y, precision: 5);
    }

    [Fact]
    public void Parse_NullSource_ThrowsInvalidOperationException()
    {
        Assert.Throws<InvalidOperationException>(() => Vector.Parse(null));
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
        Assert.Throws<InvalidOperationException>(() => Vector.Parse(source));
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
        Assert.Throws<FormatException>(() => Vector.Parse(source));
    }

    public static IEnumerable<object[]> ToString_TestData()
    {
        yield return new object[] { new Vector(), "0,0" };
        yield return new object[] { new Vector(0, 0), "0,0" };
        yield return new object[] { new Vector(1, 2), "1,2" };
        yield return new object[] { new Vector(1.1, 2.2), "1.1,2.2" };
        yield return new object[] { new Vector(-1, -2), "-1,-2" };
    }

    [Theory]
    [MemberData(nameof(ToString_TestData))]
    public void ToString_Invoke_ReturnsExpected(Vector vector, string expected)
    {
        Assert.Equal(expected, vector.ToString());
    }

    [Theory]
    [MemberData(nameof(ToString_TestData))]
    public void ToString_InvokeInvariantCulture_ReturnsExpected(Vector vector, string expected)
    {
        Assert.Equal(expected, vector.ToString(CultureInfo.InvariantCulture));
    }

    public static IEnumerable<object[]> ToString_IFormatProviderCustom_TestData()
    {
        yield return new object[] { new Vector(), "|", "0,0" };
        yield return new object[] { new Vector(), "|_", "0,0" };
        yield return new object[] { new Vector(), ",_", "0;0" };
        yield return new object[] { new Vector(), ",", "0;0" };
        yield return new object[] { new Vector(), ";", "0,0" };
        yield return new object[] { new Vector(), " ", "0,0" };

        yield return new object[] { new Vector(0, 0), "|", "0,0" };
        yield return new object[] { new Vector(0, 0), "|_", "0,0" };
        yield return new object[] { new Vector(0, 0), ",_", "0;0" };
        yield return new object[] { new Vector(0, 0), ",", "0;0" };
        yield return new object[] { new Vector(0, 0), ";", "0,0" };
        yield return new object[] { new Vector(0, 0), " ", "0,0" };

        yield return new object[] { new Vector(1, 2), "|", "1,2" };
        yield return new object[] { new Vector(1, 2), "|_", "1,2" };
        yield return new object[] { new Vector(1, 2), ",_", "1;2" };
        yield return new object[] { new Vector(1, 2), ",", "1;2" };
        yield return new object[] { new Vector(1, 2), ";", "1,2" };
        yield return new object[] { new Vector(1, 2), " ", "1,2" };
        
        yield return new object[] { new Vector(1.1, 2.2), "|", "1|1,2|2" };
        yield return new object[] { new Vector(1.1, 2.2), "|_", "1|_1,2|_2" };
        yield return new object[] { new Vector(1.1, 2.2), ",_", "1,_1;2,_2" };
        yield return new object[] { new Vector(1.1, 2.2), ",", "1,1;2,2" };
        yield return new object[] { new Vector(1.1, 2.2), ";", "1;1,2;2" };
        yield return new object[] { new Vector(1.1, 2.2), " ", "1 1,2 2" };

        yield return new object[] { new Vector(-1, -2), "|", "-1,-2" };
        yield return new object[] { new Vector(-1, -2), "|_", "-1,-2" };
        yield return new object[] { new Vector(-1, -2), ",_", "-1;-2" };
        yield return new object[] { new Vector(-1, -2), ",", "-1;-2" };
        yield return new object[] { new Vector(-1, -2), ";", "-1,-2" };
        yield return new object[] { new Vector(-1, -2), " ", "-1,-2" };
    }

    [Theory]
    [MemberData(nameof(ToString_IFormatProviderCustom_TestData))]
    public void ToString_InvokeIFormatProviderCustom_ReturnsExpected(Vector vector, string numberDecimalSeparator, string expected)
    {
        var culture = new CultureInfo("en-US");
        NumberFormatInfo formatInfo = culture.NumberFormat;
        formatInfo.NumberDecimalSeparator = numberDecimalSeparator;

        Assert.Equal(expected, vector.ToString(formatInfo));
    }

    [Theory]
    [MemberData(nameof(ToString_TestData))]
    public void ToString_InvokeIFormattableInvariantCulture_ReturnsExpected(Vector vector, string expected)
    {
        IFormattable formattable = vector;

        Assert.Equal(expected, formattable.ToString(null, null));
        Assert.Equal(expected, formattable.ToString(null, CultureInfo.InvariantCulture));
    }

    [Theory]
    [InlineData("|", "1|23,2|35")]
    [InlineData("|_", "1|_23,2|_35")]
    [InlineData(",_", "1,_23;2,_35")]
    [InlineData(",", "1,23;2,35")]
    [InlineData(";", "1;23,2;35")]
    [InlineData(" ", "1 23,2 35")]
    public void ToString_InvokeIFormattableCustomFormat_ReturnsExpected(string numberDecimalSeparator, string expected)
    {
        var vector = new Vector(1.23456, 2.34567);
        IFormattable formattable = vector;

        var culture = new CultureInfo("en-US");
        NumberFormatInfo formatInfo = culture.NumberFormat;
        formatInfo.NumberDecimalSeparator = numberDecimalSeparator;

        Assert.Equal(expected, formattable.ToString("F2", formatInfo));
    }

    [Fact]
    public void OperatorConvertPoint_InvokeEmpty_ReturnsExpected()
    {
        Point result = (Point)new Vector();
        Assert.Equal(0, result.X);
        Assert.Equal(0, result.Y);
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
    [InlineData(-1, -2)]
    public void OperatorConvertPoint_InvokeNormal_ReturnsExpected(double x, double y)
    {
        Point result = (Point)new Vector(x, y);
        Assert.Equal(x, result.X);
        Assert.Equal(y, result.Y);
    }

    [Fact]
    public void OperatorConvertSize_InvokeEmpty_ReturnsExpected()
    {
        Size result = (Size)new Vector();
        Assert.Equal(0, result.Width);
        Assert.Equal(0, result.Height);
    }

    [Theory]
    [InlineData(0, 0, 0, 0)]
    [InlineData(1, 0, 1, 0)]
    [InlineData(0, 1, 0, 1)]
    [InlineData(1, 1, 1, 1)]
    [InlineData(1, 2, 1, 2)]
    [InlineData(1.1, 2.2, 1.1, 2.2)]
    [InlineData(double.NaN, double.NaN, double.NaN, double.NaN)]
    [InlineData(double.NaN, 0, double.NaN, 0)]
    [InlineData(0, double.NaN, 0, double.NaN)]
    [InlineData(-1, -2, 1, 2)]
    public void OperatorConvertSize_InvokeNormal_ReturnsExpected(double x, double y, double expectedWidth, double expectedHeight)
    {
        Size result = (Size)new Vector(x, y);
        Assert.Equal(expectedWidth, result.Width);
        Assert.Equal(expectedHeight, result.Height);
    }

    [Fact]
    public void TypeConverter_Get_ReturnsExpected()
    {
        Assert.IsType<VectorConverter>(TypeDescriptor.GetConverter(typeof(Vector)));
    }
    
    [Fact]
    public void ValueSerializer_Get_ReturnsExpected()
    {
        Assert.IsType<VectorValueSerializer>(ValueSerializer.GetSerializerFor(typeof(Vector)));
    }
}
