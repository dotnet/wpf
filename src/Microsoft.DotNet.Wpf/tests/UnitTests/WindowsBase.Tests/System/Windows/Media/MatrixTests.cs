// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel;
using System.Globalization;
using System.Windows.Markup;
using System.Windows.Media.Converters;
using System.Windows.Tests;


namespace System.Windows.Media.Tests;

public class MatrixTests
{
    [Fact]
    public void Ctor_Default()
    {
        var matrix = new Matrix();

        Assert.Equal(1, matrix.M11);
        Assert.Equal(0, matrix.M12);
        Assert.Equal(0, matrix.M21);
        Assert.Equal(1, matrix.M22);
        Assert.Equal(0, matrix.OffsetX);
        Assert.Equal(0, matrix.OffsetY);
        Assert.True(matrix.HasInverse);
        Assert.True(matrix.IsIdentity);
        Assert.Equal(1, matrix.Determinant);
        Assert.Equal(Matrix.Identity, matrix);
    }

    public static IEnumerable<object[]> Ctor_MatrixElements_TestData()
    {
        // Identity.
        yield return new object[] { 1, 0, 0, 1, 0, 0, true, true, 1 };

        // Scale.
        yield return new object[] { 1, 0, 0, 2, 0, 0, true, false, 2 };
        yield return new object[] { 2, 0, 0, 1, 0, 0, true, false, 2 };
        yield return new object[] { 2, 0, 0, 3, 0, 0, true, false, 6 };

        // Skew.
        yield return new object[] { 1, 1, 0, 1, 0, 0, true, false, 1 };
        yield return new object[] { 1, 0, 2, 1, 0, 0, true, false, 1 };
        yield return new object[] { 1, 1, 2, 1, 0, 0, true, false, -1 };

        // Translate.
        yield return new object[] { 1, 0, 0, 1, 1, 0, true, false, 1 };
        yield return new object[] { 1, 0, 0, 1, 0, 2, true, false, 1 };
        yield return new object[] { 1, 0, 0, 1, 1, 2, true, false, 1 };

        // Translate + Scale.
        yield return new object[] { 2, 0, 0, 3, 4, 5, true, false, 6 };

        // Translate + Skew.
        yield return new object[] { 1, 2, 3, 1, 4, 5, true, false, -5 };

        // Skew + Scale.
        yield return new object[] { 1, 2, 3, 1, 0, 0, true, false, -5 };

        // Complex.
        yield return new object[] { 2, 3, 4, 5, 6, 7, true, false, -2 };
        yield return new object[] { -1, -2, -3, -4, -5, -6, true, false, -2 };
        yield return new object[] { 1.1, 2.2, 3.3, 4.4, 5.5, 6.6, true, false, -2.42 };

        // Zero.
        yield return new object[] { 0, 0, 0, 0, 0, 0, false, false, 0 };

        // Infinity.
        yield return new object[] { double.PositiveInfinity, 0, 0, 1, 0, 0, true, false, double.PositiveInfinity };
        yield return new object[] { 1, double.PositiveInfinity, 0, 1, 0, 0, true, false, double.NaN };
        yield return new object[] { 1, 0, double.PositiveInfinity, 1, 0, 0, true, false, double.NaN };
        yield return new object[] { 1, 0, 0, double.PositiveInfinity, 0, 0, true, false, double.PositiveInfinity };
        yield return new object[] { 1, 0, 0, 1, double.PositiveInfinity, 0, true, false, 1 };
        yield return new object[] { 1, 0, 0, 1, 0, double.PositiveInfinity, true, false, 1 };
        yield return new object[] { double.PositiveInfinity, double.PositiveInfinity, double.PositiveInfinity, double.PositiveInfinity, double.PositiveInfinity, double.PositiveInfinity, true, false, double.NaN };
        yield return new object[] { double.NegativeInfinity, 0, 0, 1, 0, 0, true, false, double.NegativeInfinity };
        yield return new object[] { 1, double.NegativeInfinity, 0, 1, 0, 0, true, false, double.NaN };
        yield return new object[] { 1, 0, double.NegativeInfinity, 1, 0, 0, true, false, double.NaN };
        yield return new object[] { 1, 0, 0, double.NegativeInfinity, 0, 0, true, false, double.NegativeInfinity };
        yield return new object[] { 1, 0, 0, 1, double.NegativeInfinity, 0, true, false, 1 };
        yield return new object[] { 1, 0, 0, 1, 0, double.NegativeInfinity, true, false, 1 };
        yield return new object[] { double.NegativeInfinity, double.NegativeInfinity, double.NegativeInfinity, double.NegativeInfinity, double.NegativeInfinity, double.NegativeInfinity, true, false, double.NaN };

        // NaN.
        yield return new object[] { double.NaN, 0, 0, 1, 0, 0, true, false, double.NaN };
        yield return new object[] { 1, double.NaN, 0, 1, 0, 0, true, false, double.NaN };
        yield return new object[] { 1, 0, double.NaN, 1, 0, 0, true, false, double.NaN };
        yield return new object[] { 1, 0, 0, double.NaN, 0, 0, true, false, double.NaN };
        yield return new object[] { 1, 0, 0, 1, double.NaN, 0, true, false, 1 };
        yield return new object[] { 1, 0, 0, 1, 0, double.NaN, true, false, 1 };
        yield return new object[] { double.NaN, double.NaN, double.NaN, double.NaN, double.NaN, double.NaN, true, false, double.NaN };
    }

    [Theory]
    [MemberData(nameof(Ctor_MatrixElements_TestData))]
    public void Ctor_MatrixElements(double m11, double m12, double m21, double m22, double offsetX, double offsetY, bool hasInverse, bool isIdentity, double expectedDeterminant)
    {
        var matrix = new Matrix(m11, m12, m21, m22, offsetX, offsetY);
        Assert.Equal(m11, matrix.M11);
        Assert.Equal(m12, matrix.M12);
        Assert.Equal(m21, matrix.M21);
        Assert.Equal(m22, matrix.M22);
        Assert.Equal(offsetX, matrix.OffsetX);
        Assert.Equal(offsetY, matrix.OffsetY);
        Assert.Equal(hasInverse, matrix.HasInverse);
        Assert.Equal(isIdentity, matrix.IsIdentity);
        Assert.Equal(expectedDeterminant, matrix.Determinant, precision: 5);
    }

    [Fact]
    public void Identity_Get_ReturnsExpected()
    {
        Matrix matrix = Matrix.Identity;
        Assert.Equal(1, matrix.M11);
        Assert.Equal(0, matrix.M12);
        Assert.Equal(0, matrix.M21);
        Assert.Equal(1, matrix.M22);
        Assert.Equal(0, matrix.OffsetX);
        Assert.Equal(0, matrix.OffsetY);
        Assert.True(matrix.HasInverse);
        Assert.True(matrix.IsIdentity);
        Assert.Equal(1, matrix.Determinant);
        Assert.Equal(Matrix.Identity, matrix);
        Assert.Equal(new Matrix(), matrix);
    }

    public static IEnumerable<object[]> Determinant_Get_TestData()
    {
        // Identity
        foreach (Matrix matrix in IdentityMatrices())
        {
            yield return new object[] { matrix, 1 };
        }

        // Scaled.
        yield return new object[] { new Matrix(2, 0, 0, 3, 0, 0), 6 };

        // Skewed.
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 0, 0), 0.625 };

        // Translated.
        yield return new object[] { new Matrix(1, 0, 0, 1, 2, 3), 1 };

        // Translated and scaled.
        yield return new object[] { new Matrix(2, 0, 0, 3, 1, 2), 6 };

        // Translated and skewed.
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 2, 3), 0.625 };

        // Skewed and scaled.
        yield return new object[] { new Matrix(2, 0.5, 0.75, 3, 0, 0), 5.625 };

        // Complex.
        yield return new object[] { new Matrix(2, 3, 4, 5, 6, 7), -2 };

        // No inverse.
        yield return new object[] { new Matrix(0, 0, 0, 0, 0, 0), 0 };
    }

    [Theory]
    [MemberData(nameof(Determinant_Get_TestData))]
    public void Determinant_Get_ReturnsExpected(Matrix matrix, double expected)
    {
        Assert.Equal(expected, matrix.Determinant, precision: 5);
    }
    public static IEnumerable<object[]> HasInverse_Get_TestData()
    {
        // Identity
        foreach (Matrix matrix in IdentityMatrices())
        {
            yield return new object[] { matrix, true };
        }

        // Scaled.
        yield return new object[] { new Matrix(2, 0, 0, 3, 0, 0), true };

        // Skewed.
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 0, 0), true };

        // Translated.
        yield return new object[] { new Matrix(1, 0, 0, 1, 2, 3), true };

        // Translated and scaled.
        yield return new object[] { new Matrix(2, 0, 0, 3, 1, 2), true };

        // Translated and skewed.
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 2, 3), true };

        // Skewed and scaled.
        yield return new object[] { new Matrix(2, 0.5, 0.75, 3, 0, 0), true };

        // Complex.
        yield return new object[] { new Matrix(2, 3, 4, 5, 6, 7), true };

        // No inverse.
        yield return new object[] { new Matrix(0, 0, 0, 0, 0, 0), false };
    }

    [Theory]
    [MemberData(nameof(HasInverse_Get_TestData))]
    public void HasInverse_Get_ReturnsExpected(Matrix matrix, bool expected)
    {
        Assert.Equal(expected, matrix.HasInverse);
    }

    [Theory]
    [MemberData(nameof(Multiply_TestData))]
    public void Append_Invoke_ReturnsExpected(Matrix matrix1, Matrix matrix2, Matrix expected)
    {
        Matrix copy2 = matrix2;

        matrix1.Append(matrix2);
        Assert.Equal(expected, matrix1);
        Assert.Equal(copy2, matrix2);
    }

    public static IEnumerable<object?[]> Equals_TestData()
    {
        // Identity.
        foreach (Matrix identity1 in IdentityMatrices())
        {
            // Identity + identity.
            foreach (Matrix identity2 in IdentityMatrices())
            {
                yield return new object?[] { identity1, identity2, true, true };
            }

            // Identity + scaled.
            yield return new object?[] { identity1, new Matrix(2, 0, 0, 1, 0, 0), false, false };
            yield return new object?[] { identity1, new Matrix(1, 0, 0, 5, 0, 0), false, false };
            yield return new object?[] { identity1, new Matrix(2, 0, 0, 5, 0, 0), false, false };

            // Identity + skewed.
            yield return new object?[] { identity1, new Matrix(1, 3, 0, 1, 0, 0), false, false };
            yield return new object?[] { identity1, new Matrix(1, 0, 4, 1, 0, 0), false, false };
            yield return new object?[] { identity1, new Matrix(1, 3, 4, 1, 0, 0), false, false };

            // Identity + translated.
            yield return new object?[] { identity1, new Matrix(1, 0, 0, 1, 6, 0), false, false };
            yield return new object?[] { identity1, new Matrix(1, 0, 0, 1, 0, 7), false, false };
            yield return new object?[] { identity1, new Matrix(1, 0, 0, 1, 6, 7), false, false };

            // Identity + translated and scaled.
            yield return new object?[] { identity1, new Matrix(2, 0, 0, 1, 6, 7), false, false };
            yield return new object?[] { identity1, new Matrix(1, 0, 0, 5, 6, 7), false, false };
            yield return new object?[] { identity1, new Matrix(2, 0, 0, 5, 6, 0), false, false };
            yield return new object?[] { identity1, new Matrix(2, 0, 0, 5, 0, 7), false, false };
            yield return new object?[] { identity1, new Matrix(2, 0, 0, 5, 6, 7), false, false };

            // Identity + translate and skewed.
            yield return new object?[] { identity1, new Matrix(1, 3, 0, 1, 6, 7), false, false };
            yield return new object?[] { identity1, new Matrix(1, 0, 4, 1, 6, 7), false, false };
            yield return new object?[] { identity1, new Matrix(1, 3, 4, 1, 6, 0), false, false };
            yield return new object?[] { identity1, new Matrix(1, 3, 4, 1, 0, 7), false, false };
            yield return new object?[] { identity1, new Matrix(1, 3, 4, 1, 6, 7), false, false };

            // Identity + skewed and scaled.
            yield return new object?[] { identity1, new Matrix(2, 3, 4, 5, 0, 0), false, false };
            yield return new object?[] { identity1, new Matrix(1, 3, 4, 5, 0, 0), false, false };
            yield return new object?[] { identity1, new Matrix(2, 3, 0, 5, 0, 0), false, false };
            yield return new object?[] { identity1, new Matrix(2, 0, 4, 5, 0, 0), false, false };
            yield return new object?[] { identity1, new Matrix(2, 3, 4, 5, 0, 0), false, false };

            // Identity + complex.
            yield return new object?[] { identity1, new Matrix(2, 3, 4, 5, 6, 7), false, false };
            yield return new object?[] { identity1, new Matrix(0, 0, 0, 0, 0, 0), false, true };
        }

        // Scaled + identity.
        foreach (Matrix identity in IdentityMatrices())
        {
            yield return new object?[] { new Matrix(2, 0, 0, 5, 0, 0), identity, false, false };
        }

        // Scaled + scaled.
        yield return new object?[] { new Matrix(2, 0, 0, 5, 0, 0), new Matrix(2, 0, 0, 1, 0, 0), false, false };
        yield return new object?[] { new Matrix(2, 0, 0, 5, 0, 0), new Matrix(1, 0, 0, 5, 0, 0), false, false };
        yield return new object?[] { new Matrix(2, 0, 0, 5, 0, 0), new Matrix(2, 0, 0, 5, 0, 0), true, true };

        // Scaled + skewed.
        yield return new object?[] { new Matrix(2, 0, 0, 5, 0, 0), new Matrix(1, 3, 0, 1, 0, 0), false, false };
        yield return new object?[] { new Matrix(2, 0, 0, 5, 0, 0), new Matrix(1, 0, 4, 1, 0, 0), false, false };
        yield return new object?[] { new Matrix(2, 0, 0, 5, 0, 0), new Matrix(1, 3, 4, 1, 0, 0), false, false };

        // Scaled + translated.
        yield return new object?[] { new Matrix(2, 0, 0, 5, 0, 0), new Matrix(1, 0, 0, 1, 6, 0), false, false };
        yield return new object?[] { new Matrix(2, 0, 0, 5, 0, 0), new Matrix(1, 0, 0, 1, 0, 7), false, false };
        yield return new object?[] { new Matrix(2, 0, 0, 5, 0, 0), new Matrix(1, 0, 0, 1, 6, 7), false, false };

        // Scaled + translated and scaled.
        yield return new object?[] { new Matrix(2, 0, 0, 5, 0, 0), new Matrix(2, 0, 0, 1, 6, 7), false, false };
        yield return new object?[] { new Matrix(2, 0, 0, 5, 0, 0), new Matrix(1, 0, 0, 5, 6, 7), false, false };
        yield return new object?[] { new Matrix(2, 0, 0, 5, 0, 0), new Matrix(2, 0, 0, 5, 0, 7), false, false };
        yield return new object?[] { new Matrix(2, 0, 0, 5, 0, 0), new Matrix(2, 0, 0, 5, 6, 0), false, false };
        yield return new object?[] { new Matrix(2, 0, 0, 5, 0, 0), new Matrix(2, 0, 0, 5, 6, 7), false, false };

        // Scaled + translated and skewed.
        yield return new object?[] { new Matrix(2, 0, 0, 5, 0, 0), new Matrix(1, 3, 0, 1, 6, 7), false, false };
        yield return new object?[] { new Matrix(2, 0, 0, 5, 0, 0), new Matrix(1, 4, 0, 1, 6, 7), false, false };
        yield return new object?[] { new Matrix(2, 0, 0, 5, 0, 0), new Matrix(1, 3, 4, 1, 0, 7), false, false };
        yield return new object?[] { new Matrix(2, 0, 0, 5, 0, 0), new Matrix(1, 3, 4, 1, 6, 0), false, false };
        yield return new object?[] { new Matrix(2, 0, 0, 5, 0, 0), new Matrix(1, 3, 4, 1, 6, 7), false, false };

        // Scaled + skewed and scaled.
        yield return new object?[] { new Matrix(2, 0, 0, 5, 0, 0), new Matrix(2, 3, 4, 1, 0, 0), false, false };
        yield return new object?[] { new Matrix(2, 0, 0, 5, 0, 0), new Matrix(1, 3, 4, 5, 0, 0), false, false };
        yield return new object?[] { new Matrix(2, 0, 0, 5, 0, 0), new Matrix(2, 3, 0, 5, 0, 0), false, false };
        yield return new object?[] { new Matrix(2, 0, 0, 5, 0, 0), new Matrix(2, 0, 4, 5, 0, 0), false, false };
        yield return new object?[] { new Matrix(2, 0, 0, 5, 0, 0), new Matrix(2, 3, 4, 5, 0, 0), false, false };

        // Scaled + complex.
        yield return new object?[] { new Matrix(2, 0, 0, 5, 0, 0), new Matrix(2, 3, 4, 5, 6, 7), false, false };
        yield return new object?[] { new Matrix(2, 0, 0, 5, 0, 0), new Matrix(0, 0, 0, 0, 0, 0), false, false };

        // Skewed + identity.
        foreach (Matrix identity in IdentityMatrices())
        {
            yield return new object?[] { new Matrix(1, 3, 4, 1, 0, 0), identity, false, false };
        }

        // Skewed + scaled.
        yield return new object?[] { new Matrix(1, 3, 4, 1, 0, 0), new Matrix(2, 0, 0, 1, 0, 0), false, false };
        yield return new object?[] { new Matrix(1, 3, 4, 1, 0, 0), new Matrix(1, 0, 0, 2, 0, 0), false, false };
        yield return new object?[] { new Matrix(1, 3, 4, 1, 0, 0), new Matrix(2, 0, 0, 5, 0, 0), false, false };

        // Skewed + skewed.
        yield return new object?[] { new Matrix(1, 3, 4, 1, 0, 0), new Matrix(1, 3, 0, 1, 0, 0), false, false };
        yield return new object?[] { new Matrix(1, 3, 4, 1, 0, 0), new Matrix(1, 0, 4, 1, 0, 0), false, false };
        yield return new object?[] { new Matrix(1, 3, 4, 1, 0, 0), new Matrix(1, 3, 4, 1, 0, 0), true, true };

        // Skewed + translated.
        yield return new object?[] { new Matrix(1, 3, 4, 1, 0, 0), new Matrix(1, 0, 0, 1, 6, 0), false, false };
        yield return new object?[] { new Matrix(1, 3, 4, 1, 0, 0), new Matrix(1, 0, 0, 1, 0, 7), false, false };
        yield return new object?[] { new Matrix(1, 3, 4, 1, 0, 0), new Matrix(1, 0, 0, 1, 6, 7), false, false };

        // Skewed + translated and scaled.
        yield return new object?[] { new Matrix(1, 3, 4, 1, 0, 0), new Matrix(2, 0, 0, 1, 6, 7), false, false };
        yield return new object?[] { new Matrix(1, 3, 4, 1, 0, 0), new Matrix(1, 0, 0, 5, 6, 7), false, false };
        yield return new object?[] { new Matrix(1, 3, 4, 1, 0, 0), new Matrix(2, 0, 0, 5, 6, 0), false, false };
        yield return new object?[] { new Matrix(1, 3, 4, 1, 0, 0), new Matrix(2, 0, 0, 5, 0, 7), false, false };
        yield return new object?[] { new Matrix(1, 3, 4, 1, 0, 0), new Matrix(2, 0, 0, 5, 6, 7), false, false };

        // Skewed + translated and skewed.
        yield return new object?[] { new Matrix(1, 3, 4, 1, 0, 0), new Matrix(1, 3, 0, 1, 6, 7), false, false };
        yield return new object?[] { new Matrix(1, 3, 4, 1, 0, 0), new Matrix(1, 0, 4, 1, 6, 7), false, false };
        yield return new object?[] { new Matrix(1, 3, 4, 1, 0, 0), new Matrix(1, 3, 4, 1, 6, 0), false, false };
        yield return new object?[] { new Matrix(1, 3, 4, 1, 0, 0), new Matrix(1, 3, 4, 1, 0, 7), false, false };
        yield return new object?[] { new Matrix(1, 3, 4, 1, 0, 0), new Matrix(1, 3, 4, 1, 6, 7), false, false };

        // Skewed + skewed and scaled.
        yield return new object?[] { new Matrix(1, 3, 4, 1, 0, 0), new Matrix(2, 3, 4, 1, 0, 0), false, false };
        yield return new object?[] { new Matrix(1, 3, 4, 1, 0, 0), new Matrix(1, 3, 4, 5, 0, 0), false, false };
        yield return new object?[] { new Matrix(1, 3, 4, 1, 0, 0), new Matrix(2, 3, 0, 5, 0, 0), false, false };
        yield return new object?[] { new Matrix(1, 3, 4, 1, 0, 0), new Matrix(2, 0, 4, 5, 0, 0), false, false };
        yield return new object?[] { new Matrix(1, 3, 4, 1, 0, 0), new Matrix(2, 3, 4, 5, 0, 0), false, false };

        // Skewed + complex.
        yield return new object?[] { new Matrix(1, 3, 4, 1, 0, 0), new Matrix(2, 3, 4, 5, 6, 7), false, false };
        yield return new object?[] { new Matrix(1, 3, 4, 1, 0, 0), new Matrix(0, 0, 0, 0, 0, 0), false, false };

        // Translated + identity.
        foreach (Matrix identity in IdentityMatrices())
        {
            yield return new object?[] { new Matrix(1, 0, 0, 1, 6, 7), identity, false, false };
        }

        // Translated + scaled.
        yield return new object?[] { new Matrix(1, 0, 0, 1, 6, 7), new Matrix(2, 0, 0, 1, 0, 0), false, false };
        yield return new object?[] { new Matrix(1, 0, 0, 1, 6, 7), new Matrix(1, 0, 0, 2, 0, 0), false, false };
        yield return new object?[] { new Matrix(1, 0, 0, 1, 6, 7), new Matrix(2, 0, 0, 5, 0, 0), false, false };

        // Translated + skewed.
        yield return new object?[] { new Matrix(1, 0, 0, 1, 6, 7), new Matrix(1, 3, 0, 1, 0, 0), false, false };
        yield return new object?[] { new Matrix(1, 0, 0, 1, 6, 7), new Matrix(1, 0, 4, 1, 0, 0), false, false };
        yield return new object?[] { new Matrix(1, 0, 0, 1, 6, 7), new Matrix(1, 3, 4, 1, 0, 0), false, false };

        // Translated + translated.
        yield return new object?[] { new Matrix(1, 0, 0, 1, 6, 7), new Matrix(1, 0, 0, 1, 6, 0), false, false };
        yield return new object?[] { new Matrix(1, 0, 0, 1, 6, 7), new Matrix(1, 0, 0, 1, 0, 7), false, false };
        yield return new object?[] { new Matrix(1, 0, 0, 1, 6, 7), new Matrix(1, 0, 0, 1, 6, 7), true, true };

        // Translated + translated and scaled.
        yield return new object?[] { new Matrix(1, 0, 0, 1, 6, 7), new Matrix(2, 0, 0, 1, 6, 7), false, false };
        yield return new object?[] { new Matrix(1, 0, 0, 1, 6, 7), new Matrix(1, 0, 0, 5, 6, 7), false, false };
        yield return new object?[] { new Matrix(1, 0, 0, 1, 6, 7), new Matrix(2, 0, 0, 5, 6, 0), false, false };
        yield return new object?[] { new Matrix(1, 0, 0, 1, 6, 7), new Matrix(2, 0, 0, 5, 0, 7), false, false };
        yield return new object?[] { new Matrix(1, 0, 0, 1, 6, 7), new Matrix(2, 0, 0, 5, 6, 7), false, false };

        // Translated + translated and skewed.
        yield return new object?[] { new Matrix(1, 0, 0, 1, 6, 7), new Matrix(1, 3, 0, 1, 6, 7), false, false };
        yield return new object?[] { new Matrix(1, 0, 0, 1, 6, 7), new Matrix(1, 0, 4, 1, 6, 7), false, false };
        yield return new object?[] { new Matrix(1, 0, 0, 1, 6, 7), new Matrix(1, 3, 4, 1, 6, 0), false, false };
        yield return new object?[] { new Matrix(1, 0, 0, 1, 6, 7), new Matrix(1, 3, 4, 1, 0, 7), false, false };
        yield return new object?[] { new Matrix(1, 0, 0, 1, 6, 7), new Matrix(1, 3, 4, 1, 6, 7), false, false };

        // Translated + skewed and scaled.
        yield return new object?[] { new Matrix(1, 0, 0, 1, 6, 7), new Matrix(2, 3, 4, 1, 0, 0), false, false };
        yield return new object?[] { new Matrix(1, 0, 0, 1, 6, 7), new Matrix(1, 3, 4, 5, 0, 0), false, false };
        yield return new object?[] { new Matrix(1, 0, 0, 1, 6, 7), new Matrix(2, 3, 0, 5, 0, 0), false, false };
        yield return new object?[] { new Matrix(1, 0, 0, 1, 6, 7), new Matrix(2, 0, 4, 5, 0, 0), false, false };
        yield return new object?[] { new Matrix(1, 0, 0, 1, 6, 7), new Matrix(2, 3, 4, 5, 0, 0), false, false };

        // Translated + complex.
        yield return new object?[] { new Matrix(1, 0, 0, 1, 6, 7), new Matrix(2, 3, 4, 5, 6, 7), false, false };
        yield return new object?[] { new Matrix(1, 0, 0, 1, 6, 7), new Matrix(0, 0, 0, 0, 0, 0), false, false };

        // Translated and scaled + identity.
        foreach (Matrix identity in IdentityMatrices())
        {
            yield return new object?[] { new Matrix(2, 0, 0, 5, 6, 7), identity, false, false };
        }

        // Translated and scaled + scaled.
        yield return new object?[] { new Matrix(2, 0, 0, 5, 6, 7), new Matrix(2, 0, 0, 1, 0, 0), false, false };
        yield return new object?[] { new Matrix(2, 0, 0, 5, 6, 7), new Matrix(1, 0, 0, 2, 0, 0), false, false };
        yield return new object?[] { new Matrix(2, 0, 0, 5, 6, 7), new Matrix(2, 0, 0, 5, 0, 0), false, false };

        // Translated and scaled + skewed.
        yield return new object?[] { new Matrix(2, 0, 0, 5, 6, 7), new Matrix(1, 3, 0, 1, 0, 0), false, false };
        yield return new object?[] { new Matrix(2, 0, 0, 5, 6, 7), new Matrix(1, 0, 4, 1, 0, 0), false, false };
        yield return new object?[] { new Matrix(2, 0, 0, 5, 6, 7), new Matrix(1, 3, 4, 1, 0, 0), false, false };

        // Translated and scaled + translated.
        yield return new object?[] { new Matrix(2, 0, 0, 5, 6, 7), new Matrix(1, 0, 0, 1, 6, 0), false, false };
        yield return new object?[] { new Matrix(2, 0, 0, 5, 6, 7), new Matrix(1, 0, 0, 1, 0, 7), false, false };
        yield return new object?[] { new Matrix(2, 0, 0, 5, 6, 7), new Matrix(1, 0, 0, 1, 6, 7), false, false };

        // Translated and scaled + translated and scaled.
        yield return new object?[] { new Matrix(2, 0, 0, 5, 6, 7), new Matrix(2, 0, 0, 1, 6, 7), false, false };
        yield return new object?[] { new Matrix(2, 0, 0, 5, 6, 7), new Matrix(1, 0, 0, 5, 6, 7), false, false };
        yield return new object?[] { new Matrix(2, 0, 0, 5, 6, 7), new Matrix(2, 0, 0, 5, 6, 0), false, false };
        yield return new object?[] { new Matrix(2, 0, 0, 5, 6, 7), new Matrix(2, 0, 0, 5, 0, 7), false, false };
        yield return new object?[] { new Matrix(2, 0, 0, 5, 6, 7), new Matrix(2, 0, 0, 5, 6, 7), true, true };

        // Translated and scaled + translated and skewed.
        yield return new object?[] { new Matrix(2, 0, 0, 5, 6, 7), new Matrix(1, 3, 0, 1, 6, 7), false, false };
        yield return new object?[] { new Matrix(2, 0, 0, 5, 6, 7), new Matrix(1, 0, 4, 1, 6, 7), false, false };
        yield return new object?[] { new Matrix(2, 0, 0, 5, 6, 7), new Matrix(1, 3, 4, 1, 6, 0), false, false };
        yield return new object?[] { new Matrix(2, 0, 0, 5, 6, 7), new Matrix(1, 3, 4, 1, 0, 7), false, false };
        yield return new object?[] { new Matrix(2, 0, 0, 5, 6, 7), new Matrix(1, 3, 4, 1, 6, 7), false, false };

        // Translated and scaled + skewed and scaled.
        yield return new object?[] { new Matrix(2, 0, 0, 5, 6, 7), new Matrix(2, 3, 4, 1, 0, 0), false, false };
        yield return new object?[] { new Matrix(2, 0, 0, 5, 6, 7), new Matrix(1, 3, 4, 5, 0, 0), false, false };
        yield return new object?[] { new Matrix(2, 0, 0, 5, 6, 7), new Matrix(2, 3, 0, 5, 0, 0), false, false };
        yield return new object?[] { new Matrix(2, 0, 0, 5, 6, 7), new Matrix(2, 0, 4, 5, 0, 0), false, false };
        yield return new object?[] { new Matrix(2, 0, 0, 5, 6, 7), new Matrix(2, 3, 4, 5, 0, 0), false, false };

        // Translated and scaled + complex.
        yield return new object?[] { new Matrix(2, 0, 0, 5, 6, 7), new Matrix(2, 3, 4, 5, 6, 7), false, false };
        yield return new object?[] { new Matrix(2, 0, 0, 5, 6, 7), new Matrix(0, 0, 0, 0, 0, 0), false, false };

        // Translated and skewed + identity.
        foreach (Matrix identity in IdentityMatrices())
        {
            yield return new object?[] { new Matrix(1, 3, 4, 1, 6, 7), identity, false, false };
        }

        // Translated and skewed + scaled.
        yield return new object?[] { new Matrix(1, 3, 4, 1, 6, 7), new Matrix(2, 0, 0, 1, 0, 0), false, false };
        yield return new object?[] { new Matrix(1, 3, 4, 1, 6, 7), new Matrix(1, 0, 0, 2, 0, 0), false, false };
        yield return new object?[] { new Matrix(1, 3, 4, 1, 6, 7), new Matrix(2, 0, 0, 5, 0, 0), false, false };

        // Translated and skewed + skewed.
        yield return new object?[] { new Matrix(1, 3, 4, 1, 6, 7), new Matrix(1, 3, 0, 1, 0, 0), false, false };
        yield return new object?[] { new Matrix(1, 3, 4, 1, 6, 7), new Matrix(1, 0, 4, 1, 0, 0), false, false };
        yield return new object?[] { new Matrix(1, 3, 4, 1, 6, 7), new Matrix(1, 3, 4, 1, 0, 0), false, false };

        // Translated and skewed + translated.
        yield return new object?[] { new Matrix(1, 3, 4, 1, 6, 7), new Matrix(1, 0, 0, 1, 6, 0), false, false };
        yield return new object?[] { new Matrix(1, 3, 4, 1, 6, 7), new Matrix(1, 0, 0, 1, 0, 7), false, false };
        yield return new object?[] { new Matrix(1, 3, 4, 1, 6, 7), new Matrix(1, 0, 0, 1, 6, 7), false, false };

        // Translated and skewed + translated and scaled.
        yield return new object?[] { new Matrix(1, 3, 4, 1, 6, 7), new Matrix(2, 0, 0, 1, 6, 7), false, false };
        yield return new object?[] { new Matrix(1, 3, 4, 1, 6, 7), new Matrix(1, 0, 0, 5, 6, 7), false, false };
        yield return new object?[] { new Matrix(1, 3, 4, 1, 6, 7), new Matrix(2, 0, 0, 5, 6, 0), false, false };
        yield return new object?[] { new Matrix(1, 3, 4, 1, 6, 7), new Matrix(2, 0, 0, 5, 0, 7), false, false };
        yield return new object?[] { new Matrix(1, 3, 4, 1, 6, 7), new Matrix(2, 0, 0, 5, 6, 7), false, false };

        // Translated and skewed + translated and skewed.
        yield return new object?[] { new Matrix(1, 3, 4, 1, 6, 7), new Matrix(1, 3, 0, 1, 6, 7), false, false };
        yield return new object?[] { new Matrix(1, 3, 4, 1, 6, 7), new Matrix(1, 0, 4, 1, 6, 7), false, false };
        yield return new object?[] { new Matrix(1, 3, 4, 1, 6, 7), new Matrix(1, 3, 4, 1, 6, 0), false, false };
        yield return new object?[] { new Matrix(1, 3, 4, 1, 6, 7), new Matrix(1, 3, 4, 1, 0, 7), false, false };
        yield return new object?[] { new Matrix(1, 3, 4, 1, 6, 7), new Matrix(1, 3, 4, 1, 6, 7), true, true };

        // Translated and skewed + skewed and scaled.
        yield return new object?[] { new Matrix(1, 3, 4, 1, 6, 7), new Matrix(2, 3, 4, 1, 0, 0), false, false };
        yield return new object?[] { new Matrix(1, 3, 4, 1, 6, 7), new Matrix(1, 3, 4, 5, 0, 0), false, false };
        yield return new object?[] { new Matrix(1, 3, 4, 1, 6, 7), new Matrix(2, 3, 0, 5, 0, 0), false, false };
        yield return new object?[] { new Matrix(1, 3, 4, 1, 6, 7), new Matrix(2, 0, 4, 5, 0, 0), false, false };
        yield return new object?[] { new Matrix(1, 3, 4, 1, 6, 7), new Matrix(2, 3, 4, 5, 0, 0), false, false };

        // Translated and skewed + complex.
        yield return new object?[] { new Matrix(1, 3, 4, 1, 6, 7), new Matrix(2, 3, 4, 5, 6, 7), false, false };
        yield return new object?[] { new Matrix(1, 3, 4, 1, 6, 7), new Matrix(0, 0, 0, 0, 0, 0), false, false };

        // Skewed and scaled + identity.
        foreach (Matrix identity in IdentityMatrices())
        {
            yield return new object?[] { new Matrix(2, 3, 4, 5, 0, 0), identity, false, false };
        }

        // Skewed and scaled + scaled.
        yield return new object?[] { new Matrix(2, 3, 4, 5, 0, 0), new Matrix(2, 0, 0, 1, 0, 0), false, false };
        yield return new object?[] { new Matrix(2, 3, 4, 5, 0, 0), new Matrix(1, 0, 0, 2, 0, 0), false, false };
        yield return new object?[] { new Matrix(2, 3, 4, 5, 0, 0), new Matrix(2, 0, 0, 5, 0, 0), false, false };

        // Skewed and scaled + skewed.
        yield return new object?[] { new Matrix(2, 3, 4, 5, 0, 0), new Matrix(1, 3, 0, 1, 0, 0), false, false };
        yield return new object?[] { new Matrix(2, 3, 4, 5, 0, 0), new Matrix(1, 0, 4, 1, 0, 0), false, false };
        yield return new object?[] { new Matrix(2, 3, 4, 5, 0, 0), new Matrix(1, 3, 4, 1, 0, 0), false, false };

        // Skewed and scaled + translated.
        yield return new object?[] { new Matrix(2, 3, 4, 5, 0, 0), new Matrix(1, 0, 0, 1, 6, 0), false, false };
        yield return new object?[] { new Matrix(2, 3, 4, 5, 0, 0), new Matrix(1, 0, 0, 1, 0, 7), false, false };
        yield return new object?[] { new Matrix(2, 3, 4, 5, 0, 0), new Matrix(1, 0, 0, 1, 6, 7), false, false };

        // Skewed and scaled + translated and scaled.
        yield return new object?[] { new Matrix(2, 3, 4, 5, 0, 0), new Matrix(2, 0, 0, 1, 6, 7), false, false };
        yield return new object?[] { new Matrix(2, 3, 4, 5, 0, 0), new Matrix(1, 0, 0, 5, 6, 7), false, false };
        yield return new object?[] { new Matrix(2, 3, 4, 5, 0, 0), new Matrix(2, 0, 0, 5, 6, 0), false, false };
        yield return new object?[] { new Matrix(2, 3, 4, 5, 0, 0), new Matrix(2, 0, 0, 5, 0, 7), false, false };
        yield return new object?[] { new Matrix(2, 3, 4, 5, 0, 0), new Matrix(2, 0, 0, 5, 6, 7), false, false };

        // Skewed and scaled + translated and skewed.
        yield return new object?[] { new Matrix(2, 3, 4, 5, 0, 0), new Matrix(1, 3, 0, 1, 6, 7), false, false };
        yield return new object?[] { new Matrix(2, 3, 4, 5, 0, 0), new Matrix(1, 0, 4, 1, 6, 7), false, false };
        yield return new object?[] { new Matrix(2, 3, 4, 5, 0, 0), new Matrix(1, 3, 4, 1, 6, 0), false, false };
        yield return new object?[] { new Matrix(2, 3, 4, 5, 0, 0), new Matrix(1, 3, 4, 1, 0, 7), false, false };
        yield return new object?[] { new Matrix(2, 3, 4, 5, 0, 0), new Matrix(1, 3, 4, 1, 6, 7), false, false };

        // Skewed and scaled + skewed and scaled.
        yield return new object?[] { new Matrix(2, 3, 4, 5, 0, 0), new Matrix(2, 3, 4, 1, 0, 0), false, false };
        yield return new object?[] { new Matrix(2, 3, 4, 5, 0, 0), new Matrix(1, 3, 4, 5, 0, 0), false, false };
        yield return new object?[] { new Matrix(2, 3, 4, 5, 0, 0), new Matrix(2, 3, 0, 5, 0, 0), false, false };
        yield return new object?[] { new Matrix(2, 3, 4, 5, 0, 0), new Matrix(2, 0, 4, 5, 0, 0), false, false };
        yield return new object?[] { new Matrix(2, 3, 4, 5, 0, 0), new Matrix(2, 3, 4, 5, 0, 0), true, true };

        // Skewed and scaled + complex.
        yield return new object?[] { new Matrix(2, 3, 4, 5, 0, 0), new Matrix(2, 3, 4, 5, 6, 7), false, false };
        yield return new object?[] { new Matrix(2, 3, 4, 5, 0, 0), new Matrix(0, 0, 0, 0, 0, 0), false, false };

        // Complex + identity.
        foreach (Matrix identity in IdentityMatrices())
        {
            yield return new object?[] { new Matrix(2, 3, 4, 5, 6, 7), identity, false, false };
        }

        // Complex + scaled.
        yield return new object?[] { new Matrix(2, 3, 4, 5, 6, 7), new Matrix(2, 0, 0, 1, 0, 0), false, false };
        yield return new object?[] { new Matrix(2, 3, 4, 5, 6, 7), new Matrix(1, 0, 0, 2, 0, 0), false, false };
        yield return new object?[] { new Matrix(2, 3, 4, 5, 6, 7), new Matrix(2, 0, 0, 5, 0, 0), false, false };

        // Complex + skewed.
        yield return new object?[] { new Matrix(2, 3, 4, 5, 6, 7), new Matrix(1, 3, 0, 1, 0, 0), false, false };
        yield return new object?[] { new Matrix(2, 3, 4, 5, 6, 7), new Matrix(1, 0, 4, 1, 0, 0), false, false };
        yield return new object?[] { new Matrix(2, 3, 4, 5, 6, 7), new Matrix(1, 3, 4, 1, 0, 0), false, false };

        // Complex + translated.
        yield return new object?[] { new Matrix(2, 3, 4, 5, 6, 7), new Matrix(1, 0, 0, 1, 6, 0), false, false };
        yield return new object?[] { new Matrix(2, 3, 4, 5, 6, 7), new Matrix(1, 0, 0, 1, 0, 7), false, false };
        yield return new object?[] { new Matrix(2, 3, 4, 5, 6, 7), new Matrix(1, 0, 0, 1, 6, 7), false, false };

        // Complex + translated and scaled.
        yield return new object?[] { new Matrix(2, 3, 4, 5, 6, 7), new Matrix(2, 0, 0, 1, 6, 7), false, false };
        yield return new object?[] { new Matrix(2, 3, 4, 5, 6, 7), new Matrix(1, 0, 0, 5, 6, 7), false, false };
        yield return new object?[] { new Matrix(2, 3, 4, 5, 6, 7), new Matrix(2, 0, 0, 5, 6, 0), false, false };
        yield return new object?[] { new Matrix(2, 3, 4, 5, 6, 7), new Matrix(2, 0, 0, 5, 0, 7), false, false };
        yield return new object?[] { new Matrix(2, 3, 4, 5, 6, 7), new Matrix(2, 0, 0, 5, 6, 7), false, false };

        // Complex + translated and skewed.
        yield return new object?[] { new Matrix(2, 3, 4, 5, 6, 7), new Matrix(1, 3, 0, 1, 6, 7), false, false };
        yield return new object?[] { new Matrix(2, 3, 4, 5, 6, 7), new Matrix(1, 0, 4, 1, 6, 7), false, false };
        yield return new object?[] { new Matrix(2, 3, 4, 5, 6, 7), new Matrix(1, 3, 4, 1, 6, 0), false, false };
        yield return new object?[] { new Matrix(2, 3, 4, 5, 6, 7), new Matrix(1, 3, 4, 1, 0, 7), false, false };
        yield return new object?[] { new Matrix(2, 3, 4, 5, 6, 7), new Matrix(1, 3, 4, 1, 6, 7), false, false };

        // Complex + skewed and scaled.
        yield return new object?[] { new Matrix(2, 3, 4, 5, 6, 7), new Matrix(2, 3, 4, 1, 0, 0), false, false };
        yield return new object?[] { new Matrix(2, 3, 4, 5, 6, 7), new Matrix(1, 3, 4, 5, 0, 0), false, false };
        yield return new object?[] { new Matrix(2, 3, 4, 5, 6, 7), new Matrix(2, 3, 0, 5, 0, 0), false, false };
        yield return new object?[] { new Matrix(2, 3, 4, 5, 6, 7), new Matrix(2, 0, 4, 5, 0, 0), false, false };
        yield return new object?[] { new Matrix(2, 3, 4, 5, 6, 7), new Matrix(2, 3, 4, 5, 0, 0), false, false };

        // Complex + complex.
        yield return new object?[] { new Matrix(2, 3, 4, 5, 6, 7), new Matrix(2, 3, 4, 5, 6, 7), true, true };
        yield return new object?[] { new Matrix(2, 3, 4, 5, 6, 7), new Matrix(0, 0, 0, 0, 0, 0), false, false };

        // Zero + identity.
        foreach (Matrix identity in IdentityMatrices())
        {
            yield return new object?[] { new Matrix(0, 0, 0, 0, 0, 0), identity, false, true };
        }

        // Zero + scaled.
        yield return new object?[] { new Matrix(0, 0, 0, 0, 0, 0), new Matrix(2, 0, 0, 1, 0, 0), false, false };
        yield return new object?[] { new Matrix(0, 0, 0, 0, 0, 0), new Matrix(1, 0, 0, 2, 0, 0), false, false };
        yield return new object?[] { new Matrix(0, 0, 0, 0, 0, 0), new Matrix(2, 0, 0, 5, 0, 0), false, false };

        // Zero + skewed.
        yield return new object?[] { new Matrix(0, 0, 0, 0, 0, 0), new Matrix(1, 3, 0, 1, 0, 0), false, false };
        yield return new object?[] { new Matrix(0, 0, 0, 0, 0, 0), new Matrix(1, 0, 4, 1, 0, 0), false, false };
        yield return new object?[] { new Matrix(0, 0, 0, 0, 0, 0), new Matrix(1, 3, 4, 1, 0, 0), false, false };

        // Zero + translated.
        yield return new object?[] { new Matrix(0, 0, 0, 0, 0, 0), new Matrix(1, 0, 0, 1, 6, 0), false, false };
        yield return new object?[] { new Matrix(0, 0, 0, 0, 0, 0), new Matrix(1, 0, 0, 1, 0, 7), false, false };
        yield return new object?[] { new Matrix(0, 0, 0, 0, 0, 0), new Matrix(1, 0, 0, 1, 6, 7), false, false };

        // Zero + translated and scaled.
        yield return new object?[] { new Matrix(0, 0, 0, 0, 0, 0), new Matrix(2, 0, 0, 1, 6, 7), false, false };
        yield return new object?[] { new Matrix(0, 0, 0, 0, 0, 0), new Matrix(1, 0, 0, 5, 6, 7), false, false };
        yield return new object?[] { new Matrix(0, 0, 0, 0, 0, 0), new Matrix(2, 0, 0, 5, 6, 0), false, false };
        yield return new object?[] { new Matrix(0, 0, 0, 0, 0, 0), new Matrix(2, 0, 0, 5, 0, 7), false, false };
        yield return new object?[] { new Matrix(0, 0, 0, 0, 0, 0), new Matrix(2, 0, 0, 5, 6, 7), false, false };

        // Zero + translated and skewed.
        yield return new object?[] { new Matrix(0, 0, 0, 0, 0, 0), new Matrix(1, 3, 0, 1, 6, 7), false, false };
        yield return new object?[] { new Matrix(0, 0, 0, 0, 0, 0), new Matrix(1, 0, 4, 1, 6, 7), false, false };
        yield return new object?[] { new Matrix(0, 0, 0, 0, 0, 0), new Matrix(1, 3, 4, 1, 6, 0), false, false };
        yield return new object?[] { new Matrix(0, 0, 0, 0, 0, 0), new Matrix(1, 3, 4, 1, 0, 7), false, false };
        yield return new object?[] { new Matrix(0, 0, 0, 0, 0, 0), new Matrix(1, 3, 4, 1, 6, 7), false, false };

        // Zero + skewed and scaled.
        yield return new object?[] { new Matrix(0, 0, 0, 0, 0, 0), new Matrix(2, 3, 4, 1, 0, 0), false, false };
        yield return new object?[] { new Matrix(0, 0, 0, 0, 0, 0), new Matrix(1, 3, 4, 5, 0, 0), false, false };
        yield return new object?[] { new Matrix(0, 0, 0, 0, 0, 0), new Matrix(2, 3, 0, 5, 0, 0), false, false };
        yield return new object?[] { new Matrix(0, 0, 0, 0, 0, 0), new Matrix(2, 0, 4, 5, 0, 0), false, false };
        yield return new object?[] { new Matrix(0, 0, 0, 0, 0, 0), new Matrix(2, 3, 4, 5, 0, 0), false, false };

        // Zero + complex.
        yield return new object?[] { new Matrix(0, 0, 0, 0, 0, 0), new Matrix(2, 3, 4, 5, 6, 7), false, false };
        yield return new object?[] { new Matrix(0, 0, 0, 0, 0, 0), new Matrix(0, 0, 0, 0, 0, 0), true, true };

        // NaN.
        yield return new object?[] { new Matrix(double.NaN, 3, 4, 5, 6, 7), new Matrix(double.NaN, 3, 4, 5, 6, 7), true, true };
        yield return new object?[] { new Matrix(double.NaN, 3, 4, 5, 6, 7), new Matrix(2, 3, 4, 5, 6, 7), false, false };
        yield return new object?[] { new Matrix(2, double.NaN, 4, 5, 6, 7), new Matrix(2, double.NaN, 4, 5, 6, 7), true, true };
        yield return new object?[] { new Matrix(2, double.NaN, 4, 5, 6, 7), new Matrix(2, 3, 4, 5, 6, 7), false, false };
        yield return new object?[] { new Matrix(2, 3, double.NaN, 5, 6, 7), new Matrix(2, 3, double.NaN, 5, 6, 7), true, true };
        yield return new object?[] { new Matrix(2, 3, double.NaN, 5, 6, 7), new Matrix(2, 3, 4, 5, 6, 7), false, false };
        yield return new object?[] { new Matrix(2, 3, 4, double.NaN, 6, 7), new Matrix(2, 3, 4, double.NaN, 6, 7), true, true };
        yield return new object?[] { new Matrix(2, 3, 4, double.NaN, 6, 7), new Matrix(2, 3, 4, 5, 6, 7), false, false };
        yield return new object?[] { new Matrix(2, 3, 4, 5, double.NaN, 7), new Matrix(2, 3, 4, 5, double.NaN, 7), true, true };
        yield return new object?[] { new Matrix(2, 3, 4, 5, double.NaN, 7), new Matrix(2, 3, 4, 5, 6, 7), false, false };
        yield return new object?[] { new Matrix(2, 3, 4, 5, 6, double.NaN), new Matrix(2, 3, 4, 5, 6, double.NaN), true, true };
        yield return new object?[] { new Matrix(2, 3, 4, 5, 6, double.NaN), new Matrix(2, 3, 4, 5, 6, 7), false, false };
        yield return new object?[] { new Matrix(double.NaN, double.NaN, double.NaN, double.NaN, double.NaN, double.NaN), new Matrix(double.NaN, double.NaN, double.NaN, double.NaN, double.NaN, double.NaN), true, true };
        yield return new object?[] { new Matrix(double.NaN, double.NaN, double.NaN, double.NaN, double.NaN, double.NaN), new Matrix(2, 3, 4, 5, 6, 7), false, false };
        yield return new object?[] { new Matrix(2, 3, 4, 5, 6, 7), new Matrix(double.NaN, 3, 4, 5, 6, 7), false, false };
        yield return new object?[] { new Matrix(2, 3, 4, 5, 6, 7), new Matrix(2, double.NaN, 4, 5, 6, 7), false, false };
        yield return new object?[] { new Matrix(2, 3, 4, 5, 6, 7), new Matrix(2, 3, double.NaN, 5, 6, 7), false, false };
        yield return new object?[] { new Matrix(2, 3, 4, 5, 6, 7), new Matrix(2, 3, 4, double.NaN, 6, 7), false, false };
        yield return new object?[] { new Matrix(2, 3, 4, 5, 6, 7), new Matrix(2, 3, 4, 5, double.NaN, 7), false, false };
        yield return new object?[] { new Matrix(2, 3, 4, 5, 6, 7), new Matrix(2, 3, 4, 5, 6, double.NaN), false, false };
        yield return new object?[] { new Matrix(2, 3, 4, 5, 6, 7), new Matrix(double.NaN, double.NaN, double.NaN, double.NaN, double.NaN, double.NaN), false, false };

        // Others.
        foreach (Matrix identity in IdentityMatrices())
        {
            yield return new object?[] { identity, new object(), false, false };
            yield return new object?[] { identity, null, false, false };
        }

        yield return new object?[] { new Matrix(2, 3, 4, 5, 6, 7), new object(), false, false };
        yield return new object?[] { new Matrix(2, 3, 4, 5, 6, 7), null, false, false };
        yield return new object?[] { new Matrix(0, 0, 0, 0, 0, 0), new object(), false, false };
        yield return new object?[] { new Matrix(0, 0, 0, 0, 0, 0), null, false, false };
    }

    [Theory]
    [MemberData(nameof(Equals_TestData))]
    public void Equals_Object_ReturnsExpected(Matrix matrix, object o, bool expected, bool expectedHashCode)
    {
        Assert.Equal(expected, matrix.Equals(o));
        if (o is Matrix value)
        {
            Assert.Equal(expected, matrix.Equals(value));
            Assert.Equal(expected, value.Equals(matrix));
            Assert.Equal(expected, Matrix.Equals(matrix, value));
            Assert.Equal(expected, Matrix.Equals(value, matrix));
            Assert.Equal(expectedHashCode, matrix.GetHashCode().Equals(value.GetHashCode()));
        }
    }

    public static IEnumerable<object[]> EqualityOperator_TestData()
    {
        // Identity.
        foreach (Matrix identity1 in IdentityMatrices())
        {
            // Identity + identity.
            foreach (Matrix identity2 in IdentityMatrices())
            {
                yield return new object[] { identity1, identity2, true };
            }

            // Identity + scaled.
            yield return new object[] { identity1, new Matrix(2, 0, 0, 1, 0, 0), false };
            yield return new object[] { identity1, new Matrix(1, 0, 0, 5, 0, 0), false };
            yield return new object[] { identity1, new Matrix(2, 0, 0, 5, 0, 0), false };

            // Identity + skewed.
            yield return new object[] { identity1, new Matrix(1, 3, 0, 1, 0, 0), false };
            yield return new object[] { identity1, new Matrix(1, 0, 4, 1, 0, 0), false };
            yield return new object[] { identity1, new Matrix(1, 3, 4, 1, 0, 0), false };

            // Identity + translated.
            yield return new object[] { identity1, new Matrix(1, 0, 0, 1, 6, 0), false };
            yield return new object[] { identity1, new Matrix(1, 0, 0, 1, 0, 7), false };
            yield return new object[] { identity1, new Matrix(1, 0, 0, 1, 6, 7), false };

            // Identity + translated and scaled.
            yield return new object[] { identity1, new Matrix(2, 0, 0, 1, 6, 7), false };
            yield return new object[] { identity1, new Matrix(1, 0, 0, 5, 6, 7), false };
            yield return new object[] { identity1, new Matrix(2, 0, 0, 5, 6, 0), false };
            yield return new object[] { identity1, new Matrix(2, 0, 0, 5, 0, 7), false };
            yield return new object[] { identity1, new Matrix(2, 0, 0, 5, 6, 7), false };

            // Identity + translate and skewed.
            yield return new object[] { identity1, new Matrix(1, 3, 0, 1, 6, 7), false };
            yield return new object[] { identity1, new Matrix(1, 0, 4, 1, 6, 7), false };
            yield return new object[] { identity1, new Matrix(1, 3, 4, 1, 6, 0), false };
            yield return new object[] { identity1, new Matrix(1, 3, 4, 1, 0, 7), false };
            yield return new object[] { identity1, new Matrix(1, 3, 4, 1, 6, 7), false };

            // Identity + skewed and scaled.
            yield return new object[] { identity1, new Matrix(2, 3, 4, 5, 0, 0), false };
            yield return new object[] { identity1, new Matrix(1, 3, 4, 5, 0, 0), false };
            yield return new object[] { identity1, new Matrix(2, 3, 0, 5, 0, 0), false };
            yield return new object[] { identity1, new Matrix(2, 0, 4, 5, 0, 0), false };
            yield return new object[] { identity1, new Matrix(2, 3, 4, 5, 0, 0), false };

            // Identity + complex.
            yield return new object[] { identity1, new Matrix(2, 3, 4, 5, 6, 7), false };
            yield return new object[] { identity1, new Matrix(0, 0, 0, 0, 0, 0), false };
        }

        // Scaled + identity.
        foreach (Matrix identity in IdentityMatrices())
        {
            yield return new object[] { new Matrix(2, 0, 0, 5, 0, 0), identity, false };
        }

        // Scaled + scaled.
        yield return new object[] { new Matrix(2, 0, 0, 5, 0, 0), new Matrix(2, 0, 0, 1, 0, 0), false };
        yield return new object[] { new Matrix(2, 0, 0, 5, 0, 0), new Matrix(1, 0, 0, 5, 0, 0), false };
        yield return new object[] { new Matrix(2, 0, 0, 5, 0, 0), new Matrix(2, 0, 0, 5, 0, 0), true };

        // Scaled + skewed.
        yield return new object[] { new Matrix(2, 0, 0, 5, 0, 0), new Matrix(1, 3, 0, 1, 0, 0), false };
        yield return new object[] { new Matrix(2, 0, 0, 5, 0, 0), new Matrix(1, 0, 4, 1, 0, 0), false };
        yield return new object[] { new Matrix(2, 0, 0, 5, 0, 0), new Matrix(1, 3, 4, 1, 0, 0), false };

        // Scaled + translated.
        yield return new object[] { new Matrix(2, 0, 0, 5, 0, 0), new Matrix(1, 0, 0, 1, 6, 0), false };
        yield return new object[] { new Matrix(2, 0, 0, 5, 0, 0), new Matrix(1, 0, 0, 1, 0, 7), false };
        yield return new object[] { new Matrix(2, 0, 0, 5, 0, 0), new Matrix(1, 0, 0, 1, 6, 7), false };

        // Scaled + translated and scaled.
        yield return new object[] { new Matrix(2, 0, 0, 5, 0, 0), new Matrix(2, 0, 0, 5, 0, 7), false };
        yield return new object[] { new Matrix(2, 0, 0, 5, 0, 0), new Matrix(1, 0, 0, 5, 6, 7), false };
        yield return new object[] { new Matrix(2, 0, 0, 5, 0, 0), new Matrix(2, 0, 0, 1, 6, 7), false };
        yield return new object[] { new Matrix(2, 0, 0, 5, 0, 0), new Matrix(2, 0, 0, 5, 6, 0), false };
        yield return new object[] { new Matrix(2, 0, 0, 5, 0, 0), new Matrix(2, 0, 0, 5, 6, 7), false };

        // Scaled + translated and skewed.
        yield return new object[] { new Matrix(2, 0, 0, 5, 0, 0), new Matrix(1, 3, 0, 1, 6, 7), false };
        yield return new object[] { new Matrix(2, 0, 0, 5, 0, 0), new Matrix(1, 4, 0, 1, 6, 7), false };
        yield return new object[] { new Matrix(2, 0, 0, 5, 0, 0), new Matrix(1, 3, 4, 1, 0, 7), false };
        yield return new object[] { new Matrix(2, 0, 0, 5, 0, 0), new Matrix(1, 3, 4, 1, 6, 0), false };
        yield return new object[] { new Matrix(2, 0, 0, 5, 0, 0), new Matrix(1, 3, 4, 1, 6, 7), false };

        // Scaled + skewed and scaled.
        yield return new object[] { new Matrix(2, 0, 0, 5, 0, 0), new Matrix(2, 3, 4, 1, 0, 0), false };
        yield return new object[] { new Matrix(2, 0, 0, 5, 0, 0), new Matrix(1, 3, 4, 5, 0, 0), false };
        yield return new object[] { new Matrix(2, 0, 0, 5, 0, 0), new Matrix(2, 3, 0, 5, 0, 0), false };
        yield return new object[] { new Matrix(2, 0, 0, 5, 0, 0), new Matrix(2, 0, 4, 5, 0, 0), false };
        yield return new object[] { new Matrix(2, 0, 0, 5, 0, 0), new Matrix(2, 3, 4, 5, 0, 0), false };

        // Scaled + complex.
        yield return new object[] { new Matrix(2, 0, 0, 5, 0, 0), new Matrix(2, 3, 4, 5, 6, 7), false };
        yield return new object[] { new Matrix(2, 0, 0, 5, 0, 0), new Matrix(0, 0, 0, 0, 0, 0), false };

        // Skewed + identity.
        foreach (Matrix identity in IdentityMatrices())
        {
            yield return new object[] { new Matrix(1, 3, 4, 1, 0, 0), identity, false };
        }

        // Skewed + scaled.
        yield return new object[] { new Matrix(1, 3, 4, 1, 0, 0), new Matrix(2, 0, 0, 1, 0, 0), false };
        yield return new object[] { new Matrix(1, 3, 4, 1, 0, 0), new Matrix(1, 0, 0, 2, 0, 0), false };
        yield return new object[] { new Matrix(1, 3, 4, 1, 0, 0), new Matrix(2, 0, 0, 5, 0, 0), false };

        // Skewed + skewed.
        yield return new object[] { new Matrix(1, 3, 4, 1, 0, 0), new Matrix(1, 3, 0, 1, 0, 0), false };
        yield return new object[] { new Matrix(1, 3, 4, 1, 0, 0), new Matrix(1, 0, 4, 1, 0, 0), false };
        yield return new object[] { new Matrix(1, 3, 4, 1, 0, 0), new Matrix(1, 3, 4, 1, 0, 0), true };

        // Skewed + translated.
        yield return new object[] { new Matrix(1, 3, 4, 1, 0, 0), new Matrix(1, 0, 0, 1, 6, 0), false };
        yield return new object[] { new Matrix(1, 3, 4, 1, 0, 0), new Matrix(1, 0, 0, 1, 0, 7), false };
        yield return new object[] { new Matrix(1, 3, 4, 1, 0, 0), new Matrix(1, 0, 0, 1, 6, 7), false };

        // Skewed + translated and scaled.
        yield return new object[] { new Matrix(1, 3, 4, 1, 0, 0), new Matrix(2, 0, 0, 1, 6, 7), false };
        yield return new object[] { new Matrix(1, 3, 4, 1, 0, 0), new Matrix(1, 0, 0, 5, 6, 7), false };
        yield return new object[] { new Matrix(1, 3, 4, 1, 0, 0), new Matrix(2, 0, 0, 5, 6, 0), false };
        yield return new object[] { new Matrix(1, 3, 4, 1, 0, 0), new Matrix(2, 0, 0, 5, 0, 7), false };
        yield return new object[] { new Matrix(1, 3, 4, 1, 0, 0), new Matrix(2, 0, 0, 5, 6, 7), false };

        // Skewed + translated and skewed.
        yield return new object[] { new Matrix(1, 3, 4, 1, 0, 0), new Matrix(1, 3, 0, 1, 6, 7), false };
        yield return new object[] { new Matrix(1, 3, 4, 1, 0, 0), new Matrix(1, 0, 4, 1, 6, 7), false };
        yield return new object[] { new Matrix(1, 3, 4, 1, 0, 0), new Matrix(1, 3, 4, 1, 6, 0), false };
        yield return new object[] { new Matrix(1, 3, 4, 1, 0, 0), new Matrix(1, 3, 4, 1, 0, 7), false };
        yield return new object[] { new Matrix(1, 3, 4, 1, 0, 0), new Matrix(1, 3, 4, 1, 6, 7), false };

        // Skewed + skewed and scaled.
        yield return new object[] { new Matrix(1, 3, 4, 1, 0, 0), new Matrix(2, 3, 4, 1, 0, 0), false };
        yield return new object[] { new Matrix(1, 3, 4, 1, 0, 0), new Matrix(1, 3, 4, 5, 0, 0), false };
        yield return new object[] { new Matrix(1, 3, 4, 1, 0, 0), new Matrix(2, 3, 0, 5, 0, 0), false };
        yield return new object[] { new Matrix(1, 3, 4, 1, 0, 0), new Matrix(2, 0, 4, 5, 0, 0), false };
        yield return new object[] { new Matrix(1, 3, 4, 1, 0, 0), new Matrix(2, 3, 4, 5, 0, 0), false };

        // Skewed + complex.
        yield return new object[] { new Matrix(1, 3, 4, 1, 0, 0), new Matrix(2, 3, 4, 5, 6, 7), false };
        yield return new object[] { new Matrix(1, 3, 4, 1, 0, 0), new Matrix(0, 0, 0, 0, 0, 0), false };

        // Translated + identity.
        foreach (Matrix identity in IdentityMatrices())
        {
            yield return new object[] { new Matrix(1, 0, 0, 1, 6, 7), identity, false };
        }

        // Translated + scaled.
        yield return new object[] { new Matrix(1, 0, 0, 1, 6, 7), new Matrix(2, 0, 0, 1, 0, 0), false };
        yield return new object[] { new Matrix(1, 0, 0, 1, 6, 7), new Matrix(1, 0, 0, 2, 0, 0), false };
        yield return new object[] { new Matrix(1, 0, 0, 1, 6, 7), new Matrix(2, 0, 0, 5, 0, 0), false };

        // Translated + skewed.
        yield return new object[] { new Matrix(1, 0, 0, 1, 6, 7), new Matrix(1, 3, 0, 1, 0, 0), false };
        yield return new object[] { new Matrix(1, 0, 0, 1, 6, 7), new Matrix(1, 0, 4, 1, 0, 0), false };
        yield return new object[] { new Matrix(1, 0, 0, 1, 6, 7), new Matrix(1, 3, 4, 1, 0, 0), false };

        // Translated + translated.
        yield return new object[] { new Matrix(1, 0, 0, 1, 6, 7), new Matrix(1, 0, 0, 1, 6, 0), false };
        yield return new object[] { new Matrix(1, 0, 0, 1, 6, 7), new Matrix(1, 0, 0, 1, 0, 7), false };
        yield return new object[] { new Matrix(1, 0, 0, 1, 6, 7), new Matrix(1, 0, 0, 1, 6, 7), true };

        // Translated + translated and scaled.
        yield return new object[] { new Matrix(1, 0, 0, 1, 6, 7), new Matrix(2, 0, 0, 1, 6, 7), false };
        yield return new object[] { new Matrix(1, 0, 0, 1, 6, 7), new Matrix(1, 0, 0, 5, 6, 7), false };
        yield return new object[] { new Matrix(1, 0, 0, 1, 6, 7), new Matrix(2, 0, 0, 5, 6, 0), false };
        yield return new object[] { new Matrix(1, 0, 0, 1, 6, 7), new Matrix(2, 0, 0, 5, 0, 7), false };
        yield return new object[] { new Matrix(1, 0, 0, 1, 6, 7), new Matrix(2, 0, 0, 5, 6, 7), false };

        // Translated + translated and skewed.
        yield return new object[] { new Matrix(1, 0, 0, 1, 6, 7), new Matrix(1, 3, 0, 1, 6, 7), false };
        yield return new object[] { new Matrix(1, 0, 0, 1, 6, 7), new Matrix(1, 0, 4, 1, 6, 7), false };
        yield return new object[] { new Matrix(1, 0, 0, 1, 6, 7), new Matrix(1, 3, 4, 1, 6, 0), false };
        yield return new object[] { new Matrix(1, 0, 0, 1, 6, 7), new Matrix(1, 3, 4, 1, 0, 7), false };
        yield return new object[] { new Matrix(1, 0, 0, 1, 6, 7), new Matrix(1, 3, 4, 1, 6, 7), false };

        // Translated + skewed and scaled.
        yield return new object[] { new Matrix(1, 0, 0, 1, 6, 7), new Matrix(2, 3, 4, 1, 0, 0), false };
        yield return new object[] { new Matrix(1, 0, 0, 1, 6, 7), new Matrix(1, 3, 4, 5, 0, 0), false };
        yield return new object[] { new Matrix(1, 0, 0, 1, 6, 7), new Matrix(2, 3, 0, 5, 0, 0), false };
        yield return new object[] { new Matrix(1, 0, 0, 1, 6, 7), new Matrix(2, 0, 4, 5, 0, 0), false };
        yield return new object[] { new Matrix(1, 0, 0, 1, 6, 7), new Matrix(2, 3, 4, 5, 0, 0), false };

        // Translated + complex.
        yield return new object[] { new Matrix(1, 0, 0, 1, 6, 7), new Matrix(2, 3, 4, 5, 6, 7), false };
        yield return new object[] { new Matrix(1, 0, 0, 1, 6, 7), new Matrix(0, 0, 0, 0, 0, 0), false };

        // Translated and scaled + identity.
        foreach (Matrix identity in IdentityMatrices())
        {
            yield return new object[] { new Matrix(2, 0, 0, 5, 6, 7), identity, false };
        }

        // Translated and scaled + scaled.
        yield return new object[] { new Matrix(2, 0, 0, 5, 6, 7), new Matrix(2, 0, 0, 1, 0, 0), false };
        yield return new object[] { new Matrix(2, 0, 0, 5, 6, 7), new Matrix(1, 0, 0, 2, 0, 0), false };
        yield return new object[] { new Matrix(2, 0, 0, 5, 6, 7), new Matrix(2, 0, 0, 5, 0, 0), false };

        // Translated and scaled + skewed.
        yield return new object[] { new Matrix(2, 0, 0, 5, 6, 7), new Matrix(1, 3, 0, 1, 0, 0), false };
        yield return new object[] { new Matrix(2, 0, 0, 5, 6, 7), new Matrix(1, 0, 4, 1, 0, 0), false };
        yield return new object[] { new Matrix(2, 0, 0, 5, 6, 7), new Matrix(1, 3, 4, 1, 0, 0), false };

        // Translated and scaled + translated.
        yield return new object[] { new Matrix(2, 0, 0, 5, 6, 7), new Matrix(1, 0, 0, 1, 6, 0), false };
        yield return new object[] { new Matrix(2, 0, 0, 5, 6, 7), new Matrix(1, 0, 0, 1, 0, 7), false };
        yield return new object[] { new Matrix(2, 0, 0, 5, 6, 7), new Matrix(1, 0, 0, 1, 6, 7), false };

        // Translated and scaled + translated and scaled.
        yield return new object[] { new Matrix(2, 0, 0, 5, 6, 7), new Matrix(2, 0, 0, 1, 6, 7), false };
        yield return new object[] { new Matrix(2, 0, 0, 5, 6, 7), new Matrix(1, 0, 0, 5, 6, 7), false };
        yield return new object[] { new Matrix(2, 0, 0, 5, 6, 7), new Matrix(2, 0, 0, 5, 6, 0), false };
        yield return new object[] { new Matrix(2, 0, 0, 5, 6, 7), new Matrix(2, 0, 0, 5, 0, 7), false };
        yield return new object[] { new Matrix(2, 0, 0, 5, 6, 7), new Matrix(2, 0, 0, 5, 6, 7), true };

        // Translated and scaled + translated and skewed.
        yield return new object[] { new Matrix(2, 0, 0, 5, 6, 7), new Matrix(1, 3, 0, 1, 6, 7), false };
        yield return new object[] { new Matrix(2, 0, 0, 5, 6, 7), new Matrix(1, 0, 4, 1, 6, 7), false };
        yield return new object[] { new Matrix(2, 0, 0, 5, 6, 7), new Matrix(1, 3, 4, 1, 6, 0), false };
        yield return new object[] { new Matrix(2, 0, 0, 5, 6, 7), new Matrix(1, 3, 4, 1, 0, 7), false };
        yield return new object[] { new Matrix(2, 0, 0, 5, 6, 7), new Matrix(1, 3, 4, 1, 6, 7), false };

        // Translated and scaled + skewed and scaled.
        yield return new object[] { new Matrix(2, 0, 0, 5, 6, 7), new Matrix(2, 3, 4, 1, 0, 0), false };
        yield return new object[] { new Matrix(2, 0, 0, 5, 6, 7), new Matrix(1, 3, 4, 5, 0, 0), false };
        yield return new object[] { new Matrix(2, 0, 0, 5, 6, 7), new Matrix(2, 3, 0, 5, 0, 0), false };
        yield return new object[] { new Matrix(2, 0, 0, 5, 6, 7), new Matrix(2, 0, 4, 5, 0, 0), false };
        yield return new object[] { new Matrix(2, 0, 0, 5, 6, 7), new Matrix(2, 3, 4, 5, 0, 0), false };

        // Translated and scaled + complex.
        yield return new object[] { new Matrix(2, 0, 0, 5, 6, 7), new Matrix(2, 3, 4, 5, 6, 7), false };
        yield return new object[] { new Matrix(2, 0, 0, 5, 6, 7), new Matrix(0, 0, 0, 0, 0, 0), false };

        // Translated and skewed + identity.
        foreach (Matrix identity in IdentityMatrices())
        {
            yield return new object[] { new Matrix(1, 3, 4, 1, 6, 7), identity, false };
        }

        // Translated and skewed + scaled.
        yield return new object[] { new Matrix(1, 3, 4, 1, 6, 7), new Matrix(2, 0, 0, 1, 0, 0), false };
        yield return new object[] { new Matrix(1, 3, 4, 1, 6, 7), new Matrix(1, 0, 0, 2, 0, 0), false };
        yield return new object[] { new Matrix(1, 3, 4, 1, 6, 7), new Matrix(2, 0, 0, 5, 0, 0), false };

        // Translated and skewed + skewed.
        yield return new object[] { new Matrix(1, 3, 4, 1, 6, 7), new Matrix(1, 3, 0, 1, 0, 0), false };
        yield return new object[] { new Matrix(1, 3, 4, 1, 6, 7), new Matrix(1, 0, 4, 1, 0, 0), false };
        yield return new object[] { new Matrix(1, 3, 4, 1, 6, 7), new Matrix(1, 3, 4, 1, 0, 0), false };

        // Translated and skewed + translated.
        yield return new object[] { new Matrix(1, 3, 4, 1, 6, 7), new Matrix(1, 0, 0, 1, 6, 0), false };
        yield return new object[] { new Matrix(1, 3, 4, 1, 6, 7), new Matrix(1, 0, 0, 1, 0, 7), false };
        yield return new object[] { new Matrix(1, 3, 4, 1, 6, 7), new Matrix(1, 0, 0, 1, 6, 7), false };

        // Translated and skewed + translated and scaled.
        yield return new object[] { new Matrix(1, 3, 4, 1, 6, 7), new Matrix(2, 0, 0, 1, 6, 7), false };
        yield return new object[] { new Matrix(1, 3, 4, 1, 6, 7), new Matrix(1, 0, 0, 5, 6, 7), false };
        yield return new object[] { new Matrix(1, 3, 4, 1, 6, 7), new Matrix(2, 0, 0, 5, 6, 0), false };
        yield return new object[] { new Matrix(1, 3, 4, 1, 6, 7), new Matrix(2, 0, 0, 5, 0, 7), false };
        yield return new object[] { new Matrix(1, 3, 4, 1, 6, 7), new Matrix(2, 0, 0, 5, 6, 7), false };

        // Translated and skewed + translated and skewed.
        yield return new object[] { new Matrix(1, 3, 4, 1, 6, 7), new Matrix(1, 3, 0, 1, 6, 7), false };
        yield return new object[] { new Matrix(1, 3, 4, 1, 6, 7), new Matrix(1, 0, 4, 1, 6, 7), false };
        yield return new object[] { new Matrix(1, 3, 4, 1, 6, 7), new Matrix(1, 3, 4, 1, 6, 0), false };
        yield return new object[] { new Matrix(1, 3, 4, 1, 6, 7), new Matrix(1, 3, 4, 1, 0, 7), false };
        yield return new object[] { new Matrix(1, 3, 4, 1, 6, 7), new Matrix(1, 3, 4, 1, 6, 7), true };

        // Translated and skewed + skewed and scaled.
        yield return new object[] { new Matrix(1, 3, 4, 1, 6, 7), new Matrix(2, 3, 4, 1, 0, 0), false };
        yield return new object[] { new Matrix(1, 3, 4, 1, 6, 7), new Matrix(1, 3, 4, 5, 0, 0), false };
        yield return new object[] { new Matrix(1, 3, 4, 1, 6, 7), new Matrix(2, 3, 0, 5, 0, 0), false };
        yield return new object[] { new Matrix(1, 3, 4, 1, 6, 7), new Matrix(2, 0, 4, 5, 0, 0), false };
        yield return new object[] { new Matrix(1, 3, 4, 1, 6, 7), new Matrix(2, 3, 4, 5, 0, 0), false };

        // Translated and skewed + complex.
        yield return new object[] { new Matrix(1, 3, 4, 1, 6, 7), new Matrix(2, 3, 4, 5, 6, 7), false };
        yield return new object[] { new Matrix(1, 3, 4, 1, 6, 7), new Matrix(0, 0, 0, 0, 0, 0), false };

        // Skewed and scaled + identity.
        foreach (Matrix identity in IdentityMatrices())
        {
            yield return new object[] { new Matrix(2, 3, 4, 5, 0, 0), identity, false };
        }

        // Skewed and scaled + scaled.
        yield return new object[] { new Matrix(2, 3, 4, 5, 0, 0), new Matrix(2, 0, 0, 1, 0, 0), false };
        yield return new object[] { new Matrix(2, 3, 4, 5, 0, 0), new Matrix(1, 0, 0, 2, 0, 0), false };
        yield return new object[] { new Matrix(2, 3, 4, 5, 0, 0), new Matrix(2, 0, 0, 5, 0, 0), false };

        // Skewed and scaled + skewed.
        yield return new object[] { new Matrix(2, 3, 4, 5, 0, 0), new Matrix(1, 3, 0, 1, 0, 0), false };
        yield return new object[] { new Matrix(2, 3, 4, 5, 0, 0), new Matrix(1, 0, 4, 1, 0, 0), false };
        yield return new object[] { new Matrix(2, 3, 4, 5, 0, 0), new Matrix(1, 3, 4, 1, 0, 0), false };

        // Skewed and scaled + translated.
        yield return new object[] { new Matrix(2, 3, 4, 5, 0, 0), new Matrix(1, 0, 0, 1, 6, 0), false };
        yield return new object[] { new Matrix(2, 3, 4, 5, 0, 0), new Matrix(1, 0, 0, 1, 0, 7), false };
        yield return new object[] { new Matrix(2, 3, 4, 5, 0, 0), new Matrix(1, 0, 0, 1, 6, 7), false };

        // Skewed and scaled + translated and scaled.
        yield return new object[] { new Matrix(2, 3, 4, 5, 0, 0), new Matrix(2, 0, 0, 1, 6, 7), false };
        yield return new object[] { new Matrix(2, 3, 4, 5, 0, 0), new Matrix(1, 0, 0, 5, 6, 7), false };
        yield return new object[] { new Matrix(2, 3, 4, 5, 0, 0), new Matrix(2, 0, 0, 5, 6, 0), false };
        yield return new object[] { new Matrix(2, 3, 4, 5, 0, 0), new Matrix(2, 0, 0, 5, 0, 7), false };
        yield return new object[] { new Matrix(2, 3, 4, 5, 0, 0), new Matrix(2, 0, 0, 5, 6, 7), false };

        // Skewed and scaled + translated and skewed.
        yield return new object[] { new Matrix(2, 3, 4, 5, 0, 0), new Matrix(1, 3, 0, 1, 6, 7), false };
        yield return new object[] { new Matrix(2, 3, 4, 5, 0, 0), new Matrix(1, 0, 4, 1, 6, 7), false };
        yield return new object[] { new Matrix(2, 3, 4, 5, 0, 0), new Matrix(1, 3, 4, 1, 6, 0), false };
        yield return new object[] { new Matrix(2, 3, 4, 5, 0, 0), new Matrix(1, 3, 4, 1, 0, 7), false };
        yield return new object[] { new Matrix(2, 3, 4, 5, 0, 0), new Matrix(1, 3, 4, 1, 6, 7), false };

        // Skewed and scaled + skewed and scaled.
        yield return new object[] { new Matrix(2, 3, 4, 5, 0, 0), new Matrix(2, 3, 4, 1, 0, 0), false };
        yield return new object[] { new Matrix(2, 3, 4, 5, 0, 0), new Matrix(1, 3, 4, 5, 0, 0), false };
        yield return new object[] { new Matrix(2, 3, 4, 5, 0, 0), new Matrix(2, 3, 0, 5, 0, 0), false };
        yield return new object[] { new Matrix(2, 3, 4, 5, 0, 0), new Matrix(2, 0, 4, 5, 0, 0), false };
        yield return new object[] { new Matrix(2, 3, 4, 5, 0, 0), new Matrix(2, 3, 4, 5, 0, 0), true };

        // Skewed and scaled + complex.
        yield return new object[] { new Matrix(2, 3, 4, 5, 0, 0), new Matrix(2, 3, 4, 5, 6, 7), false };
        yield return new object[] { new Matrix(2, 3, 4, 5, 0, 0), new Matrix(0, 0, 0, 0, 0, 0), false };

        // Complex + identity.
        foreach (Matrix identity in IdentityMatrices())
        {
            yield return new object[] { new Matrix(2, 3, 4, 5, 6, 7), identity, false };
        }

        // Complex + scaled.
        yield return new object[] { new Matrix(2, 3, 4, 5, 6, 7), new Matrix(2, 0, 0, 1, 0, 0), false };
        yield return new object[] { new Matrix(2, 3, 4, 5, 6, 7), new Matrix(1, 0, 0, 2, 0, 0), false };
        yield return new object[] { new Matrix(2, 3, 4, 5, 6, 7), new Matrix(2, 0, 0, 5, 0, 0), false };

        // Complex + skewed.
        yield return new object[] { new Matrix(2, 3, 4, 5, 6, 7), new Matrix(1, 3, 0, 1, 0, 0), false };
        yield return new object[] { new Matrix(2, 3, 4, 5, 6, 7), new Matrix(1, 0, 4, 1, 0, 0), false };
        yield return new object[] { new Matrix(2, 3, 4, 5, 6, 7), new Matrix(1, 3, 4, 1, 0, 0), false };

        // Complex + translated.
        yield return new object[] { new Matrix(2, 3, 4, 5, 6, 7), new Matrix(1, 0, 0, 1, 6, 0), false };
        yield return new object[] { new Matrix(2, 3, 4, 5, 6, 7), new Matrix(1, 0, 0, 1, 0, 7), false };
        yield return new object[] { new Matrix(2, 3, 4, 5, 6, 7), new Matrix(1, 0, 0, 1, 6, 7), false };

        // Complex + translated and scaled.
        yield return new object[] { new Matrix(2, 3, 4, 5, 6, 7), new Matrix(2, 0, 0, 1, 6, 7), false };
        yield return new object[] { new Matrix(2, 3, 4, 5, 6, 7), new Matrix(1, 0, 0, 5, 6, 7), false };
        yield return new object[] { new Matrix(2, 3, 4, 5, 6, 7), new Matrix(2, 0, 0, 5, 6, 0), false };
        yield return new object[] { new Matrix(2, 3, 4, 5, 6, 7), new Matrix(2, 0, 0, 5, 0, 7), false };
        yield return new object[] { new Matrix(2, 3, 4, 5, 6, 7), new Matrix(2, 0, 0, 5, 6, 7), false };

        // Complex + translated and skewed.
        yield return new object[] { new Matrix(2, 3, 4, 5, 6, 7), new Matrix(1, 3, 0, 1, 6, 7), false };
        yield return new object[] { new Matrix(2, 3, 4, 5, 6, 7), new Matrix(1, 0, 4, 1, 6, 7), false };
        yield return new object[] { new Matrix(2, 3, 4, 5, 6, 7), new Matrix(1, 3, 4, 1, 6, 0), false };
        yield return new object[] { new Matrix(2, 3, 4, 5, 6, 7), new Matrix(1, 3, 4, 1, 0, 7), false };
        yield return new object[] { new Matrix(2, 3, 4, 5, 6, 7), new Matrix(1, 3, 4, 1, 6, 7), false };

        // Complex + skewed and scaled.
        yield return new object[] { new Matrix(2, 3, 4, 5, 6, 7), new Matrix(2, 3, 4, 1, 0, 0), false };
        yield return new object[] { new Matrix(2, 3, 4, 5, 6, 7), new Matrix(1, 3, 4, 5, 0, 0), false };
        yield return new object[] { new Matrix(2, 3, 4, 5, 6, 7), new Matrix(2, 3, 0, 5, 0, 0), false };
        yield return new object[] { new Matrix(2, 3, 4, 5, 6, 7), new Matrix(2, 0, 4, 5, 0, 0), false };
        yield return new object[] { new Matrix(2, 3, 4, 5, 6, 7), new Matrix(2, 3, 4, 5, 0, 0), false };

        // Complex + complex.
        yield return new object[] { new Matrix(2, 3, 4, 5, 6, 7), new Matrix(2, 3, 4, 5, 6, 7), true };
        yield return new object[] { new Matrix(2, 3, 4, 5, 6, 7), new Matrix(0, 0, 0, 0, 0, 0), false };

        // Zero + identity.
        foreach (Matrix identity in IdentityMatrices())
        {
            yield return new object[] { new Matrix(0, 0, 0, 0, 0, 0), identity, false };
        }

        // Zero + scaled.
        yield return new object[] { new Matrix(0, 0, 0, 0, 0, 0), new Matrix(2, 0, 0, 1, 0, 0), false };
        yield return new object[] { new Matrix(0, 0, 0, 0, 0, 0), new Matrix(1, 0, 0, 2, 0, 0), false };
        yield return new object[] { new Matrix(0, 0, 0, 0, 0, 0), new Matrix(2, 0, 0, 5, 0, 0), false };

        // Zero + skewed.
        yield return new object[] { new Matrix(0, 0, 0, 0, 0, 0), new Matrix(1, 3, 0, 1, 0, 0), false };
        yield return new object[] { new Matrix(0, 0, 0, 0, 0, 0), new Matrix(1, 0, 4, 1, 0, 0), false };
        yield return new object[] { new Matrix(0, 0, 0, 0, 0, 0), new Matrix(1, 3, 4, 1, 0, 0), false };

        // Zero + translated.
        yield return new object[] { new Matrix(0, 0, 0, 0, 0, 0), new Matrix(1, 0, 0, 1, 6, 0), false };
        yield return new object[] { new Matrix(0, 0, 0, 0, 0, 0), new Matrix(1, 0, 0, 1, 0, 7), false };
        yield return new object[] { new Matrix(0, 0, 0, 0, 0, 0), new Matrix(1, 0, 0, 1, 6, 7), false };

        // Zero + translated and scaled.
        yield return new object[] { new Matrix(0, 0, 0, 0, 0, 0), new Matrix(2, 0, 0, 1, 6, 7), false };
        yield return new object[] { new Matrix(0, 0, 0, 0, 0, 0), new Matrix(1, 0, 0, 5, 6, 7), false };
        yield return new object[] { new Matrix(0, 0, 0, 0, 0, 0), new Matrix(2, 0, 0, 5, 6, 0), false };
        yield return new object[] { new Matrix(0, 0, 0, 0, 0, 0), new Matrix(2, 0, 0, 5, 0, 7), false };
        yield return new object[] { new Matrix(0, 0, 0, 0, 0, 0), new Matrix(2, 0, 0, 5, 6, 7), false };

        // Zero + translated and skewed.
        yield return new object[] { new Matrix(0, 0, 0, 0, 0, 0), new Matrix(1, 3, 0, 1, 6, 7), false };
        yield return new object[] { new Matrix(0, 0, 0, 0, 0, 0), new Matrix(1, 0, 4, 1, 6, 7), false };
        yield return new object[] { new Matrix(0, 0, 0, 0, 0, 0), new Matrix(1, 3, 4, 1, 6, 0), false };
        yield return new object[] { new Matrix(0, 0, 0, 0, 0, 0), new Matrix(1, 3, 4, 1, 0, 7), false };
        yield return new object[] { new Matrix(0, 0, 0, 0, 0, 0), new Matrix(1, 3, 4, 1, 6, 7), false };

        // Zero + skewed and scaled.
        yield return new object[] { new Matrix(0, 0, 0, 0, 0, 0), new Matrix(2, 3, 4, 1, 0, 0), false };
        yield return new object[] { new Matrix(0, 0, 0, 0, 0, 0), new Matrix(1, 3, 4, 5, 0, 0), false };
        yield return new object[] { new Matrix(0, 0, 0, 0, 0, 0), new Matrix(2, 3, 0, 5, 0, 0), false };
        yield return new object[] { new Matrix(0, 0, 0, 0, 0, 0), new Matrix(2, 0, 4, 5, 0, 0), false };
        yield return new object[] { new Matrix(0, 0, 0, 0, 0, 0), new Matrix(2, 3, 4, 5, 0, 0), false };

        // Zero + complex.
        yield return new object[] { new Matrix(0, 0, 0, 0, 0, 0), new Matrix(2, 3, 4, 5, 6, 7), false };
        yield return new object[] { new Matrix(0, 0, 0, 0, 0, 0), new Matrix(0, 0, 0, 0, 0, 0), true };

        // NaN.
        yield return new object[] { new Matrix(double.NaN, 3, 4, 5, 6, 7), new Matrix(double.NaN, 3, 4, 5, 6, 7), false };
        yield return new object[] { new Matrix(double.NaN, 3, 4, 5, 6, 7), new Matrix(2, 3, 4, 5, 6, 7), false };
        yield return new object[] { new Matrix(2, double.NaN, 4, 5, 6, 7), new Matrix(2, double.NaN, 4, 5, 6, 7), false };
        yield return new object[] { new Matrix(2, double.NaN, 4, 5, 6, 7), new Matrix(2, 3, 4, 5, 6, 7), false };
        yield return new object[] { new Matrix(2, 3, double.NaN, 5, 6, 7), new Matrix(2, 3, double.NaN, 5, 6, 7), false };
        yield return new object[] { new Matrix(2, 3, double.NaN, 5, 6, 7), new Matrix(2, 3, 4, 5, 6, 7), false };
        yield return new object[] { new Matrix(2, 3, 4, double.NaN, 6, 7), new Matrix(2, 3, 4, double.NaN, 6, 7), false };
        yield return new object[] { new Matrix(2, 3, 4, double.NaN, 6, 7), new Matrix(2, 3, 4, 5, 6, 7), false };
        yield return new object[] { new Matrix(2, 3, 4, 5, double.NaN, 7), new Matrix(2, 3, 4, 5, double.NaN, 7), false };
        yield return new object[] { new Matrix(2, 3, 4, 5, double.NaN, 7), new Matrix(2, 3, 4, 5, 6, 7), false };
        yield return new object[] { new Matrix(2, 3, 4, 5, 6, double.NaN), new Matrix(2, 3, 4, 5, 6, double.NaN), false };
        yield return new object[] { new Matrix(2, 3, 4, 5, 6, double.NaN), new Matrix(2, 3, 4, 5, 6, 7), false };
        yield return new object[] { new Matrix(double.NaN, double.NaN, double.NaN, double.NaN, double.NaN, double.NaN), new Matrix(double.NaN, double.NaN, double.NaN, double.NaN, double.NaN, double.NaN), false };
        yield return new object[] { new Matrix(double.NaN, double.NaN, double.NaN, double.NaN, double.NaN, double.NaN), new Matrix(2, 3, 4, 5, 6, 7), false };
        yield return new object[] { new Matrix(2, 3, 4, 5, 6, 7), new Matrix(double.NaN, 3, 4, 5, 6, 7), false };
        yield return new object[] { new Matrix(2, 3, 4, 5, 6, 7), new Matrix(2, double.NaN, 4, 5, 6, 7), false };
        yield return new object[] { new Matrix(2, 3, 4, 5, 6, 7), new Matrix(2, 3, double.NaN, 5, 6, 7), false };
        yield return new object[] { new Matrix(2, 3, 4, 5, 6, 7), new Matrix(2, 3, 4, double.NaN, 6, 7), false };
        yield return new object[] { new Matrix(2, 3, 4, 5, 6, 7), new Matrix(2, 3, 4, 5, double.NaN, 7), false };
        yield return new object[] { new Matrix(2, 3, 4, 5, 6, 7), new Matrix(2, 3, 4, 5, 6, double.NaN), false };
        yield return new object[] { new Matrix(2, 3, 4, 5, 6, 7), new Matrix(double.NaN, double.NaN, double.NaN, double.NaN, double.NaN, double.NaN), false };
    }

    [Theory]
    [MemberData(nameof(EqualityOperator_TestData))]
    public void EqualityOperator_Invoke_ReturnsExpected(Matrix matrix1, Matrix matrix2, bool expected)
    {
        Assert.Equal(expected, matrix1 == matrix2);
        Assert.Equal(expected, matrix2 == matrix1);
        Assert.Equal(expected, matrix2 == matrix1);
        Assert.Equal(!expected, matrix2 != matrix1);
    }

    public static IEnumerable<object[]> GetHashCode_Identity_TestData()
    {
        foreach (Matrix identity in IdentityMatrices())
        {
            yield return new object[] { identity };
        }
    }

    [Theory]
    [MemberData(nameof(GetHashCode_Identity_TestData))]
    public void GetHashCode_InvokeIdentity_ReturnsExpected(Matrix matrix)
    {
        Assert.Equal(0, matrix.GetHashCode());
        Assert.Equal(matrix.GetHashCode(), matrix.GetHashCode());
    }

    public static IEnumerable<object[]> GetHashCode_NotIdentity_TestData()
    {
        yield return new object[] { new Matrix(2, 0, 0, 5, 0, 0) };
        yield return new object[] { new Matrix(1, 3, 4, 1, 0, 0) };
        yield return new object[] { new Matrix(1, 0, 0, 1, 6, 7) };
        yield return new object[] { new Matrix(2, 0, 0, 5, 6, 7) };
        yield return new object[] { new Matrix(1, 3, 4, 1, 6, 7) };
        yield return new object[] { new Matrix(2, 3, 4, 5, 0, 0) };
        yield return new object[] { new Matrix(2, 3, 4, 5, 6, 7) };
    }

    [Theory]
    [MemberData(nameof(GetHashCode_NotIdentity_TestData))]
    public void GetHashCode_InvokeNotIdentity_ReturnsExpected(Matrix matrix)
    {
        Assert.NotEqual(0, matrix.GetHashCode());
        Assert.Equal(matrix.GetHashCode(), matrix.GetHashCode());
    }

    [Fact]
    public void GetHashCode_InvokeZero_ReturnsExpected()
    {
        var matrix = new Matrix(0, 0, 0, 0, 0, 0);
        Assert.Equal(0, matrix.GetHashCode());
        Assert.Equal(matrix.GetHashCode(), matrix.GetHashCode());
    }

    public static IEnumerable<object[]> Invert_TestData()
    {
        // Identity
        foreach (Matrix matrix in IdentityMatrices())
        {
            yield return new object[] { matrix, new Matrix(1, 0, 0, 1, 0, 0), 1 };
        }

        // Scaled.
        yield return new object[] { new Matrix(2, 0, 0, 3, 0, 0), new Matrix(0.5, 0, 0, 0.33333, 0, 0), 0.16667 };

        // Skewed.
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 0, 0), new Matrix(1.6, -0.8, -1.2, 1.6, 0, 0), 1.6 };

        // Translated.
        yield return new object[] { new Matrix(1, 0, 0, 1, 2, 3), new Matrix(1, 0, 0, 1, -2, -3), 1 };

        // Translated and scaled.
        yield return new object[] { new Matrix(2, 0, 0, 3, 1, 2), new Matrix(0.5, 0, 0, 0.33333, -0.5, -0.66667), 0.16667 };

        // Translated and skewed.
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 2, 3), new Matrix(1.6, -0.8, -1.2, 1.6, 0.4, -3.2), 1.6 };

        // Skewed and scaled.
        yield return new object[] { new Matrix(2, 0.5, 0.75, 3, 0, 0), new Matrix(0.53333, -0.08889, -0.13333, 0.35556, 0, 0), 0.17778 };

        // Complex.
        yield return new object[] { new Matrix(2, 3, 4, 5, 6, 7), new Matrix(-2.5, 1.5, 2, -1, 1, -2), -0.5 };
    }

    [Theory]
    [MemberData(nameof(Invert_TestData))]
    public void Invert_Invoke_Success(Matrix matrix, Matrix expected, double expectedDeterminant)
    {
        matrix.Invert();
        Helpers.AssertEqualRounded(expected, matrix);
        Assert.Equal(expected.IsIdentity, matrix.IsIdentity);
        Assert.Equal(expected.HasInverse, matrix.HasInverse);
        Assert.Equal(expectedDeterminant, matrix.Determinant, precision: 5);
    }

    [Fact]
    public void Invert_NoInverse_ThrowsInvalidOperationException()
    {
        var matrix = new Matrix(0, 0, 0, 0, 0, 0);
        Assert.Throws<InvalidOperationException>(() => matrix.Invert());
    }

    public static IEnumerable<object[]> Parse_TestData()
    {
        yield return new object[] { "Identity", Matrix.Identity };
        yield return new object[] { "  Identity  ", Matrix.Identity };
        yield return new object[] { "0,0,0,0,0,0", new Matrix(0, 0, 0, 0, 0, 0) };
        yield return new object[] { "2,3,4,5,6,7", new Matrix(2, 3, 4, 5, 6, 7) };
        yield return new object[] { "2.1,3.2,4.3,5.4,6.5,7.6", new Matrix(2.1, 3.2, 4.3, 5.4, 6.5, 7.6) };
        yield return new object[] { "   2   ,   3  ,  4,  5  , 6  , 7  ", new Matrix(2, 3, 4, 5, 6, 7) };
        yield return new object[] { "-1,-2,-3,-4,-5,-6", new Matrix(-1, -2, -3, -4, -5, -6) };
    }

    [Theory]
    [MemberData(nameof(Parse_TestData))]
    public void Parse_Invoke_Success(string source, Matrix expected)
    {
        Matrix result = Matrix.Parse(source);
        Assert.Equal(expected.M11, result.M11, precision: 5);
        Assert.Equal(expected.M12, result.M12, precision: 5);
        Assert.Equal(expected.M21, result.M21, precision: 5);
        Assert.Equal(expected.M22, result.M22, precision: 5);
        Assert.Equal(expected.OffsetX, result.OffsetX, precision: 5);
        Assert.Equal(expected.OffsetY, result.OffsetY, precision: 5);
    }

    [Fact]
    public void Parse_NullSource_ThrowsNotSupportedException()
    {
        Assert.Throws<InvalidOperationException>(() => Matrix.Parse(null));
    }

    public static IEnumerable<object?[]> Parse_InvalidSource_TestData()
    {
        yield return new object?[] { "" };
        yield return new object?[] { "  " };
        yield return new object?[] { "2" };
        yield return new object?[] { "2," };
        yield return new object?[] { "2,3" };
        yield return new object?[] { "2,3," };
        yield return new object?[] { "2,3,4" };
        yield return new object?[] { "2,3,4," };
        yield return new object?[] { "2,3,4,5," };
        yield return new object?[] { "2,3,4,5,6" };
        yield return new object?[] { "2,3,4,5,6,7,8" };
        yield return new object?[] { "2,3,4,5,6,7,test" };
        yield return new object?[] { "Empty," };
        yield return new object?[] { "Identity," };
        yield return new object?[] { "Identity,3,4,5,6,7" };
    }

    [Theory]
    [MemberData(nameof(Parse_InvalidSource_TestData))]
    public void Parse_InvalidSource_ThrowsInvalidOperationException(string source)
    {
        Assert.Throws<InvalidOperationException>(() => Matrix.Parse(source));
    }

    public static IEnumerable<object?[]> Parse_NotDouble_TestData()
    {
        yield return new object[] { "Empty" };
        yield return new object[] { " Empty " };
        yield return new object[] { "Empty,2,3,4,5,6" };
        yield return new object[] { "test" };
        yield return new object[] { "test,2,3,4,5,6" };
        yield return new object[] { "1,test,3,4,5,6" };
        yield return new object[] { "1,2,test,4,5,6" };
        yield return new object[] { "1,2,3,test,5,6" };
        yield return new object[] { "1,2,3,4,test,6" };
        yield return new object[] { "1,2,3,4,5,test" };
        yield return new object[] { "1;2;3;4;5;6" };
        yield return new object[] { """1"",""2"",""3"",""4"",""5"",""6""" };
    }

    [Theory]
    [MemberData(nameof(Parse_NotDouble_TestData))]
    public void Parse_NotDouble_ThrowsFormatException(string source)
    {
        Assert.Throws<FormatException>(() => Matrix.Parse(source));
    }

    public static IEnumerable<Matrix> IdentityMatrices()
    {
        yield return Matrix.Identity;
        yield return new Matrix();
        yield return new Matrix(1, 0, 0, 1, 0, 0);

        var identitySetIdentity = Matrix.Identity;
        identitySetIdentity.SetIdentity();
        yield return identitySetIdentity;

        var defaultSetIdentity = new Matrix();
        defaultSetIdentity.SetIdentity();
        yield return defaultSetIdentity;

        var constructedSetIdentity = new Matrix(1, 0, 0, 1, 0, 0);
        constructedSetIdentity.SetIdentity();
        yield return constructedSetIdentity;

        var scaleSetIdentity = new Matrix(2, 0, 0, 3, 0, 0);
        scaleSetIdentity.SetIdentity();
        yield return scaleSetIdentity;

        var skewSetIdentity = new Matrix(1, 2, 3, 1, 0, 0);
        skewSetIdentity.SetIdentity();
        yield return skewSetIdentity;

        var translateSetIdentity = new Matrix(1, 0, 0, 1, 1, 2);
        translateSetIdentity.SetIdentity();
        yield return translateSetIdentity;

        var translateScaleSetIdentity = new Matrix(2, 0, 0, 3, 1, 2);
        translateScaleSetIdentity.SetIdentity();
        yield return translateScaleSetIdentity;

        var translateSkewSetIdentity = new Matrix(1, 2, 3, 1, 1, 2);
        translateSkewSetIdentity.SetIdentity();
        yield return translateSkewSetIdentity;

        var skewScaleSetIdentity = new Matrix(1, 2, 3, 1, 1, 2);
        skewScaleSetIdentity.SetIdentity();
        yield return skewScaleSetIdentity;

        var complexSetIdentity = new Matrix(2, 3, 4, 5, 6, 7);
        complexSetIdentity.SetIdentity();
        yield return complexSetIdentity;

        var noInverseSetIdentity = new Matrix(0, 0, 0, 0, 0, 0);
        noInverseSetIdentity.SetIdentity();
        yield return noInverseSetIdentity;
    }

    public static IEnumerable<object[]> Multiply_TestData()
    {
        // Identity.
        foreach (Matrix identity1 in IdentityMatrices())
        {
            // Identity + identity.
            foreach (Matrix identity2 in IdentityMatrices())
            {
                yield return new object[] { identity1, identity2, identity2 };
            }

            // Identity + scaled.
            yield return new object[] { identity1, new Matrix(2, 0, 0, 1, 0, 0), new Matrix(2, 0, 0, 1, 0, 0) };
            yield return new object[] { identity1, new Matrix(1, 0, 0, 5, 0, 0), new Matrix(1, 0, 0, 5, 0, 0) };
            yield return new object[] { identity1, new Matrix(2, 0, 0, 5, 0, 0), new Matrix(2, 0, 0, 5, 0, 0) };

            // Identity + skewed.
            yield return new object[] { identity1, new Matrix(1, 3, 0, 1, 0, 0), new Matrix(1, 3, 0, 1, 0, 0) };
            yield return new object[] { identity1, new Matrix(1, 0, 4, 1, 0, 0), new Matrix(1, 0, 4, 1, 0, 0) };
            yield return new object[] { identity1, new Matrix(1, 3, 4, 1, 0, 0), new Matrix(1, 3, 4, 1, 0, 0) };

            // Identity + translated.
            yield return new object[] { identity1, new Matrix(1, 0, 0, 1, 6, 0), new Matrix(1, 0, 0, 1, 6, 0) };
            yield return new object[] { identity1, new Matrix(1, 0, 0, 1, 0, 7), new Matrix(1, 0, 0, 1, 0, 7) };
            yield return new object[] { identity1, new Matrix(1, 0, 0, 1, 6, 7), new Matrix(1, 0, 0, 1, 6, 7) };

            // Identity + translated and scaled.
            yield return new object[] { identity1, new Matrix(2, 0, 0, 1, 6, 7), new Matrix(2, 0, 0, 1, 6, 7) };
            yield return new object[] { identity1, new Matrix(1, 0, 0, 5, 6, 7), new Matrix(1, 0, 0, 5, 6, 7) };
            yield return new object[] { identity1, new Matrix(2, 0, 0, 5, 6, 0), new Matrix(2, 0, 0, 5, 6, 0) };
            yield return new object[] { identity1, new Matrix(2, 0, 0, 5, 0, 7), new Matrix(2, 0, 0, 5, 0, 7) };
            yield return new object[] { identity1, new Matrix(2, 0, 0, 5, 6, 7), new Matrix(2, 0, 0, 5, 6, 7) };

            // Identity + translate and skewed.
            yield return new object[] { identity1, new Matrix(1, 3, 0, 1, 6, 7), new Matrix(1, 3, 0, 1, 6, 7) };
            yield return new object[] { identity1, new Matrix(1, 0, 4, 1, 6, 7), new Matrix(1, 0, 4, 1, 6, 7) };
            yield return new object[] { identity1, new Matrix(1, 3, 4, 1, 6, 0), new Matrix(1, 3, 4, 1, 6, 0) };
            yield return new object[] { identity1, new Matrix(1, 3, 4, 1, 0, 7), new Matrix(1, 3, 4, 1, 0, 7) };
            yield return new object[] { identity1, new Matrix(1, 3, 4, 1, 6, 7), new Matrix(1, 3, 4, 1, 6, 7) };

            // Identity + skewed and scaled.
            yield return new object[] { identity1, new Matrix(2, 3, 4, 5, 0, 0), new Matrix(2, 3, 4, 5, 0, 0) };
            yield return new object[] { identity1, new Matrix(1, 3, 4, 5, 0, 0), new Matrix(1, 3, 4, 5, 0, 0) };
            yield return new object[] { identity1, new Matrix(2, 3, 0, 5, 0, 0), new Matrix(2, 3, 0, 5, 0, 0) };
            yield return new object[] { identity1, new Matrix(2, 0, 4, 5, 0, 0), new Matrix(2, 0, 4, 5, 0, 0) };
            yield return new object[] { identity1, new Matrix(2, 3, 4, 5, 0, 0), new Matrix(2, 3, 4, 5, 0, 0) };

            // Identity + complex.
            yield return new object[] { identity1, new Matrix(2, 3, 4, 5, 6, 7), new Matrix(2, 3, 4, 5, 6, 7) };
            yield return new object[] { identity1, new Matrix(0, 0, 0, 0, 0, 0), new Matrix(0, 0, 0, 0, 0, 0) };
        }


        // Scaled + identity.
        foreach (Matrix identity in IdentityMatrices())
        {
            yield return new object[] { new Matrix(2, 0, 0, 5, 0, 0), identity, new Matrix(2, 0, 0, 5, 0, 0) };
        }

        // Scaled + scaled.
        yield return new object[] { new Matrix(2, 0, 0, 5, 0, 0), new Matrix(2, 0, 0, 1, 0, 0), new Matrix(4, 0, 0, 5, 0, 0) };
        yield return new object[] { new Matrix(2, 0, 0, 5, 0, 0), new Matrix(1, 0, 0, 5, 0, 0), new Matrix(2, 0, 0, 25, 0, 0) };
        yield return new object[] { new Matrix(2, 0, 0, 5, 0, 0), new Matrix(2, 0, 0, 5, 0, 0), new Matrix(4, 0, 0, 25, 0, 0) };

        // Scaled + skewed.
        yield return new object[] { new Matrix(2, 0, 0, 5, 0, 0), new Matrix(1, 3, 0, 1, 0, 0), new Matrix(2, 6, 0, 5, 0, 0) };
        yield return new object[] { new Matrix(2, 0, 0, 5, 0, 0), new Matrix(1, 0, 4, 1, 0, 0), new Matrix(2, 0, 20, 5, 0, 0) };
        yield return new object[] { new Matrix(2, 0, 0, 5, 0, 0), new Matrix(1, 3, 4, 1, 0, 0), new Matrix(2, 6, 20, 5, 0, 0) };

        // Scaled + translated.
        yield return new object[] { new Matrix(2, 0, 0, 5, 0, 0), new Matrix(1, 0, 0, 1, 6, 0), new Matrix(2, 0, 0, 5, 6, 0) };
        yield return new object[] { new Matrix(2, 0, 0, 5, 0, 0), new Matrix(1, 0, 0, 1, 0, 7), new Matrix(2, 0, 0, 5, 0, 7) };
        yield return new object[] { new Matrix(2, 0, 0, 5, 0, 0), new Matrix(1, 0, 0, 1, 6, 7), new Matrix(2, 0, 0, 5, 6, 7) };

        // Scaled + translated and scaled.
        yield return new object[] { new Matrix(2, 0, 0, 5, 0, 0), new Matrix(2, 0, 0, 1, 6, 7), new Matrix(4, 0, 0, 5, 6, 7) };
        yield return new object[] { new Matrix(2, 0, 0, 5, 0, 0), new Matrix(1, 0, 0, 5, 6, 7), new Matrix(2, 0, 0, 25, 6, 7) };
        yield return new object[] { new Matrix(2, 0, 0, 5, 0, 0), new Matrix(2, 0, 0, 5, 0, 7), new Matrix(4, 0, 0, 25, 0, 7) };
        yield return new object[] { new Matrix(2, 0, 0, 5, 0, 0), new Matrix(2, 0, 0, 5, 6, 0), new Matrix(4, 0, 0, 25, 6, 0) };
        yield return new object[] { new Matrix(2, 0, 0, 5, 0, 0), new Matrix(2, 0, 0, 5, 6, 7), new Matrix(4, 0, 0, 25, 6, 7) };

        // Scaled + translated and skewed.
        yield return new object[] { new Matrix(2, 0, 0, 5, 0, 0), new Matrix(1, 3, 0, 1, 6, 7), new Matrix(2, 6, 0, 5, 6, 7) };
        yield return new object[] { new Matrix(2, 0, 0, 5, 0, 0), new Matrix(1, 4, 0, 1, 6, 7), new Matrix(2, 8, 0, 5, 6, 7) };
        yield return new object[] { new Matrix(2, 0, 0, 5, 0, 0), new Matrix(1, 3, 4, 1, 0, 7), new Matrix(2, 6, 20, 5, 0, 7) };
        yield return new object[] { new Matrix(2, 0, 0, 5, 0, 0), new Matrix(1, 3, 4, 1, 6, 0), new Matrix(2, 6, 20, 5, 6, 0) };
        yield return new object[] { new Matrix(2, 0, 0, 5, 0, 0), new Matrix(1, 3, 4, 1, 6, 7), new Matrix(2, 6, 20, 5, 6, 7) };

        // Scaled + skewed and scaled.
        yield return new object[] { new Matrix(2, 0, 0, 5, 0, 0), new Matrix(2, 3, 4, 1, 0, 0), new Matrix(4, 6, 20, 5, 0, 0) };
        yield return new object[] { new Matrix(2, 0, 0, 5, 0, 0), new Matrix(1, 3, 4, 5, 0, 0), new Matrix(2, 6, 20, 25, 0, 0) };
        yield return new object[] { new Matrix(2, 0, 0, 5, 0, 0), new Matrix(2, 3, 0, 5, 0, 0), new Matrix(4, 6, 0, 25, 0, 0) };
        yield return new object[] { new Matrix(2, 0, 0, 5, 0, 0), new Matrix(2, 0, 4, 5, 0, 0), new Matrix(4, 0, 20, 25, 0, 0) };
        yield return new object[] { new Matrix(2, 0, 0, 5, 0, 0), new Matrix(2, 3, 4, 5, 0, 0), new Matrix(4, 6, 20, 25, 0, 0) };

        // Scaled + complex.
        yield return new object[] { new Matrix(2, 0, 0, 5, 0, 0), new Matrix(2, 3, 4, 5, 6, 7), new Matrix(4, 6, 20, 25, 6, 7) };
        yield return new object[] { new Matrix(2, 0, 0, 5, 0, 0), new Matrix(0, 0, 0, 0, 0, 0), new Matrix(0, 0, 0, 0, 0, 0) };

        // Skewed + identity.
        foreach (Matrix identity in IdentityMatrices())
        {
            yield return new object[] { new Matrix(1, 3, 4, 1, 0, 0), identity, new Matrix(1, 3, 4, 1, 0, 0) };
        }

        // Skewed + scaled.
        yield return new object[] { new Matrix(1, 3, 4, 1, 0, 0), new Matrix(2, 0, 0, 1, 0, 0), new Matrix(2, 3, 8, 1, 0, 0) };
        yield return new object[] { new Matrix(1, 3, 4, 1, 0, 0), new Matrix(1, 0, 0, 5, 0, 0), new Matrix(1, 15, 4, 5, 0, 0) };
        yield return new object[] { new Matrix(1, 3, 4, 1, 0, 0), new Matrix(2, 0, 0, 5, 0, 0), new Matrix(2, 15, 8, 5, 0, 0) };

        // Skewed + skewed.
        yield return new object[] { new Matrix(1, 3, 4, 1, 0, 0), new Matrix(1, 3, 0, 1, 0, 0), new Matrix(1, 6, 4, 13, 0, 0) };
        yield return new object[] { new Matrix(1, 3, 4, 1, 0, 0), new Matrix(1, 0, 4, 1, 0, 0), new Matrix(13, 3, 8, 1, 0, 0) };
        yield return new object[] { new Matrix(1, 3, 4, 1, 0, 0), new Matrix(1, 3, 4, 1, 0, 0), new Matrix(13, 6, 8, 13, 0, 0) };

        // Skewed + translated.
        yield return new object[] { new Matrix(1, 3, 4, 1, 0, 0), new Matrix(1, 0, 0, 1, 6, 0), new Matrix(1, 3, 4, 1, 6, 0) };
        yield return new object[] { new Matrix(1, 3, 4, 1, 0, 0), new Matrix(1, 0, 0, 1, 0, 7), new Matrix(1, 3, 4, 1, 0, 7) };
        yield return new object[] { new Matrix(1, 3, 4, 1, 0, 0), new Matrix(1, 0, 0, 1, 6, 7), new Matrix(1, 3, 4, 1, 6, 7) };

        // Skewed + translated and scaled.
        yield return new object[] { new Matrix(1, 3, 4, 1, 0, 0), new Matrix(2, 0, 0, 1, 6, 7), new Matrix(2, 3, 8, 1, 6, 7) };
        yield return new object[] { new Matrix(1, 3, 4, 1, 0, 0), new Matrix(1, 0, 0, 5, 6, 7), new Matrix(1, 15, 4, 5, 6, 7) };
        yield return new object[] { new Matrix(1, 3, 4, 1, 0, 0), new Matrix(2, 0, 0, 5, 6, 0), new Matrix(2, 15, 8, 5, 6, 0) };
        yield return new object[] { new Matrix(1, 3, 4, 1, 0, 0), new Matrix(2, 0, 0, 5, 0, 7), new Matrix(2, 15, 8, 5, 0, 7) };
        yield return new object[] { new Matrix(1, 3, 4, 1, 0, 0), new Matrix(2, 0, 0, 5, 6, 7), new Matrix(2, 15, 8, 5, 6, 7) };

        // Skewed + translated and skewed.
        yield return new object[] { new Matrix(1, 3, 4, 1, 0, 0), new Matrix(1, 3, 0, 1, 6, 7), new Matrix(1, 6, 4, 13, 6, 7) };
        yield return new object[] { new Matrix(1, 3, 4, 1, 0, 0), new Matrix(1, 0, 4, 1, 6, 7), new Matrix(13, 3, 8, 1, 6, 7) };
        yield return new object[] { new Matrix(1, 3, 4, 1, 0, 0), new Matrix(1, 3, 4, 1, 6, 0), new Matrix(13, 6, 8, 13, 6, 0) };
        yield return new object[] { new Matrix(1, 3, 4, 1, 0, 0), new Matrix(1, 3, 4, 1, 0, 7), new Matrix(13, 6, 8, 13, 0, 7) };
        yield return new object[] { new Matrix(1, 3, 4, 1, 0, 0), new Matrix(1, 3, 4, 1, 6, 7), new Matrix(13, 6, 8, 13, 6, 7) };

        // Skewed + skewed and scaled.
        yield return new object[] { new Matrix(1, 3, 4, 1, 0, 0), new Matrix(2, 3, 4, 1, 0, 0), new Matrix(14, 6, 12, 13, 0, 0) };
        yield return new object[] { new Matrix(1, 3, 4, 1, 0, 0), new Matrix(1, 3, 4, 5, 0, 0), new Matrix(13, 18, 8, 17, 0, 0) };
        yield return new object[] { new Matrix(1, 3, 4, 1, 0, 0), new Matrix(2, 3, 0, 5, 0, 0), new Matrix(2, 18, 8, 17, 0, 0) };
        yield return new object[] { new Matrix(1, 3, 4, 1, 0, 0), new Matrix(2, 0, 4, 5, 0, 0), new Matrix(14, 15, 12, 5, 0, 0) };
        yield return new object[] { new Matrix(1, 3, 4, 1, 0, 0), new Matrix(2, 3, 4, 5, 0, 0), new Matrix(14, 18, 12, 17, 0, 0) };

        // Skewed + complex.
        yield return new object[] { new Matrix(1, 3, 4, 1, 0, 0), new Matrix(2, 3, 4, 5, 6, 7), new Matrix(14, 18, 12, 17, 6, 7) };
        yield return new object[] { new Matrix(1, 3, 4, 1, 0, 0), new Matrix(0, 0, 0, 0, 0, 0), new Matrix(0, 0, 0, 0, 0, 0) };

        // Translated + identity.
        foreach (Matrix identity in IdentityMatrices())
        {
            yield return new object[] { new Matrix(1, 0, 0, 1, 6, 7), identity, new Matrix(1, 0, 0, 1, 6, 7) };
        }

        // Translated + scaled.
        yield return new object[] { new Matrix(1, 0, 0, 1, 6, 7), new Matrix(2, 0, 0, 1, 0, 0), new Matrix(2, 0, 0, 1, 12, 7) };
        yield return new object[] { new Matrix(1, 0, 0, 1, 6, 7), new Matrix(1, 0, 0, 5, 0, 0), new Matrix(1, 0, 0, 5, 6, 35) };
        yield return new object[] { new Matrix(1, 0, 0, 1, 6, 7), new Matrix(2, 0, 0, 5, 0, 0), new Matrix(2, 0, 0, 5, 12, 35) };

        // Translated + skewed.
        yield return new object[] { new Matrix(1, 0, 0, 1, 6, 7), new Matrix(1, 3, 0, 1, 0, 0), new Matrix(1, 3, 0, 1, 6, 25) };
        yield return new object[] { new Matrix(1, 0, 0, 1, 6, 7), new Matrix(1, 0, 4, 1, 0, 0), new Matrix(1, 0, 4, 1, 34, 7) };
        yield return new object[] { new Matrix(1, 0, 0, 1, 6, 7), new Matrix(1, 3, 4, 1, 0, 0), new Matrix(1, 3, 4, 1, 34, 25) };

        // Translated + translated.
        yield return new object[] { new Matrix(1, 0, 0, 1, 6, 7), new Matrix(1, 0, 0, 1, 6, 0), new Matrix(1, 0, 0, 1, 12, 7) };
        yield return new object[] { new Matrix(1, 0, 0, 1, 6, 7), new Matrix(1, 0, 0, 1, 0, 7), new Matrix(1, 0, 0, 1, 6, 14) };
        yield return new object[] { new Matrix(1, 0, 0, 1, 6, 7), new Matrix(1, 0, 0, 1, 6, 7), new Matrix(1, 0, 0, 1, 12, 14) };

        // Translated + translated and scaled.
        yield return new object[] { new Matrix(1, 0, 0, 1, 6, 7), new Matrix(2, 0, 0, 1, 6, 7), new Matrix(2, 0, 0, 1, 18, 14) };
        yield return new object[] { new Matrix(1, 0, 0, 1, 6, 7), new Matrix(1, 0, 0, 5, 6, 7), new Matrix(1, 0, 0, 5, 12, 42) };
        yield return new object[] { new Matrix(1, 0, 0, 1, 6, 7), new Matrix(2, 0, 0, 5, 6, 0), new Matrix(2, 0, 0, 5, 18, 35) };
        yield return new object[] { new Matrix(1, 0, 0, 1, 6, 7), new Matrix(2, 0, 0, 5, 0, 7), new Matrix(2, 0, 0, 5, 12, 42) };
        yield return new object[] { new Matrix(1, 0, 0, 1, 6, 7), new Matrix(2, 0, 0, 5, 6, 7), new Matrix(2, 0, 0, 5, 18, 42) };

        // Translated + translated and skewed.
        yield return new object[] { new Matrix(1, 0, 0, 1, 6, 7), new Matrix(1, 3, 0, 1, 6, 7), new Matrix(1, 3, 0, 1, 12, 32) };
        yield return new object[] { new Matrix(1, 0, 0, 1, 6, 7), new Matrix(1, 0, 4, 1, 6, 7), new Matrix(1, 0, 4, 1, 40, 14) };
        yield return new object[] { new Matrix(1, 0, 0, 1, 6, 7), new Matrix(1, 3, 4, 1, 6, 0), new Matrix(1, 3, 4, 1, 40, 25) };
        yield return new object[] { new Matrix(1, 0, 0, 1, 6, 7), new Matrix(1, 3, 4, 1, 0, 7), new Matrix(1, 3, 4, 1, 34, 32) };
        yield return new object[] { new Matrix(1, 0, 0, 1, 6, 7), new Matrix(1, 3, 4, 1, 6, 7), new Matrix(1, 3, 4, 1, 40, 32) };

        // Translated + skewed and scaled.
        yield return new object[] { new Matrix(1, 0, 0, 1, 6, 7), new Matrix(2, 3, 4, 1, 0, 0), new Matrix(2, 3, 4, 1, 40, 25) };
        yield return new object[] { new Matrix(1, 0, 0, 1, 6, 7), new Matrix(1, 3, 4, 5, 0, 0), new Matrix(1, 3, 4, 5, 34, 53) };
        yield return new object[] { new Matrix(1, 0, 0, 1, 6, 7), new Matrix(2, 3, 0, 5, 0, 0), new Matrix(2, 3, 0, 5, 12, 53) };
        yield return new object[] { new Matrix(1, 0, 0, 1, 6, 7), new Matrix(2, 0, 4, 5, 0, 0), new Matrix(2, 0, 4, 5, 40, 35) };
        yield return new object[] { new Matrix(1, 0, 0, 1, 6, 7), new Matrix(2, 3, 4, 5, 0, 0), new Matrix(2, 3, 4, 5, 40, 53) };

        // Translated + complex.
        yield return new object[] { new Matrix(1, 0, 0, 1, 6, 7), new Matrix(2, 3, 4, 5, 6, 7), new Matrix(2, 3, 4, 5, 46, 60) };
        yield return new object[] { new Matrix(1, 0, 0, 1, 6, 7), new Matrix(0, 0, 0, 0, 0, 0), new Matrix(0, 0, 0, 0, 0, 0) };

        // Translated and scaled + identity.
        foreach (Matrix identity in IdentityMatrices())
        {
            yield return new object[] { new Matrix(2, 0, 0, 5, 6, 7), identity, new Matrix(2, 0, 0, 5, 6, 7) };
        }

        // Translated and scaled + scaled.
        yield return new object[] { new Matrix(2, 0, 0, 5, 6, 7), new Matrix(2, 0, 0, 1, 0, 0), new Matrix(4, 0, 0, 5, 12, 7) };
        yield return new object[] { new Matrix(2, 0, 0, 5, 6, 7), new Matrix(1, 0, 0, 5, 0, 0), new Matrix(2, 0, 0, 25, 6, 35) };
        yield return new object[] { new Matrix(2, 0, 0, 5, 6, 7), new Matrix(2, 0, 0, 5, 0, 0), new Matrix(4, 0, 0, 25, 12, 35) };

        // Translated and scaled + skewed.
        yield return new object[] { new Matrix(2, 0, 0, 5, 6, 7), new Matrix(1, 3, 0, 1, 0, 0), new Matrix(2, 6, 0, 5, 6, 25) };
        yield return new object[] { new Matrix(2, 0, 0, 5, 6, 7), new Matrix(1, 0, 4, 1, 0, 0), new Matrix(2, 0, 20, 5, 34, 7) };
        yield return new object[] { new Matrix(2, 0, 0, 5, 6, 7), new Matrix(1, 3, 4, 1, 0, 0), new Matrix(2, 6, 20, 5, 34, 25) };

        // Translated and scaled + translated.
        yield return new object[] { new Matrix(2, 0, 0, 5, 6, 7), new Matrix(1, 0, 0, 1, 6, 0), new Matrix(2, 0, 0, 5, 12, 7) };
        yield return new object[] { new Matrix(2, 0, 0, 5, 6, 7), new Matrix(1, 0, 0, 1, 0, 7), new Matrix(2, 0, 0, 5, 6, 14) };
        yield return new object[] { new Matrix(2, 0, 0, 5, 6, 7), new Matrix(1, 0, 0, 1, 6, 7), new Matrix(2, 0, 0, 5, 12, 14) };

        // Translated and scaled + translated and scaled.
        yield return new object[] { new Matrix(2, 0, 0, 5, 6, 7), new Matrix(2, 0, 0, 1, 6, 7), new Matrix(4, 0, 0, 5, 18, 14) };
        yield return new object[] { new Matrix(2, 0, 0, 5, 6, 7), new Matrix(1, 0, 0, 5, 6, 7), new Matrix(2, 0, 0, 25, 12, 42) };
        yield return new object[] { new Matrix(2, 0, 0, 5, 6, 7), new Matrix(2, 0, 0, 5, 6, 0), new Matrix(4, 0, 0, 25, 18, 35) };
        yield return new object[] { new Matrix(2, 0, 0, 5, 6, 7), new Matrix(2, 0, 0, 5, 0, 7), new Matrix(4, 0, 0, 25, 12, 42) };
        yield return new object[] { new Matrix(2, 0, 0, 5, 6, 7), new Matrix(2, 0, 0, 5, 6, 7), new Matrix(4, 0, 0, 25, 18, 42) };

        // Translated and scaled + translated and skewed.
        yield return new object[] { new Matrix(2, 0, 0, 5, 6, 7), new Matrix(1, 3, 0, 1, 6, 7), new Matrix(2, 6, 0, 5, 12, 32) };
        yield return new object[] { new Matrix(2, 0, 0, 5, 6, 7), new Matrix(1, 0, 4, 1, 6, 7), new Matrix(2, 0, 20, 5, 40, 14) };
        yield return new object[] { new Matrix(2, 0, 0, 5, 6, 7), new Matrix(1, 3, 4, 1, 6, 0), new Matrix(2, 6, 20, 5, 40, 25) };
        yield return new object[] { new Matrix(2, 0, 0, 5, 6, 7), new Matrix(1, 3, 4, 1, 0, 7), new Matrix(2, 6, 20, 5, 34, 32) };
        yield return new object[] { new Matrix(2, 0, 0, 5, 6, 7), new Matrix(1, 3, 4, 1, 6, 7), new Matrix(2, 6, 20, 5, 40, 32) };

        // Translated and scaled + skewed and scaled.
        yield return new object[] { new Matrix(2, 0, 0, 5, 6, 7), new Matrix(2, 3, 4, 1, 0, 0), new Matrix(4, 6, 20, 5, 40, 25) };
        yield return new object[] { new Matrix(2, 0, 0, 5, 6, 7), new Matrix(1, 3, 4, 5, 0, 0), new Matrix(2, 6, 20, 25, 34, 53) };
        yield return new object[] { new Matrix(2, 0, 0, 5, 6, 7), new Matrix(2, 3, 0, 5, 0, 0), new Matrix(4, 6, 0, 25, 12, 53) };
        yield return new object[] { new Matrix(2, 0, 0, 5, 6, 7), new Matrix(2, 0, 4, 5, 0, 0), new Matrix(4, 0, 20, 25, 40, 35) };
        yield return new object[] { new Matrix(2, 0, 0, 5, 6, 7), new Matrix(2, 3, 4, 5, 0, 0), new Matrix(4, 6, 20, 25, 40, 53) };

        // Translated and scaled + complex.
        yield return new object[] { new Matrix(2, 0, 0, 5, 6, 7), new Matrix(2, 3, 4, 5, 6, 7), new Matrix(4, 6, 20, 25, 46, 60) };
        yield return new object[] { new Matrix(2, 0, 0, 5, 6, 7), new Matrix(0, 0, 0, 0, 0, 0), new Matrix(0, 0, 0, 0, 0, 0) };

        // Translated and skewed + identity.
        foreach (Matrix identity in IdentityMatrices())
        {
            yield return new object[] { new Matrix(1, 3, 4, 1, 6, 7), identity, new Matrix(1, 3, 4, 1, 6, 7) };
        }

        // Translated and skewed + scaled.
        yield return new object[] { new Matrix(1, 3, 4, 1, 6, 7), new Matrix(2, 0, 0, 1, 0, 0), new Matrix(2, 3, 8, 1, 12, 7) };
        yield return new object[] { new Matrix(1, 3, 4, 1, 6, 7), new Matrix(1, 0, 0, 5, 0, 0), new Matrix(1, 15, 4, 5, 6, 35) };
        yield return new object[] { new Matrix(1, 3, 4, 1, 6, 7), new Matrix(2, 0, 0, 5, 0, 0), new Matrix(2, 15, 8, 5, 12, 35) };

        // Translated and skewed + skewed.
        yield return new object[] { new Matrix(1, 3, 4, 1, 6, 7), new Matrix(1, 3, 0, 1, 0, 0), new Matrix(1, 6, 4, 13, 6, 25) };
        yield return new object[] { new Matrix(1, 3, 4, 1, 6, 7), new Matrix(1, 0, 4, 1, 0, 0), new Matrix(13, 3, 8, 1, 34, 7) };
        yield return new object[] { new Matrix(1, 3, 4, 1, 6, 7), new Matrix(1, 3, 4, 1, 0, 0), new Matrix(13, 6, 8, 13, 34, 25) };

        // Translated and skewed + translated.
        yield return new object[] { new Matrix(1, 3, 4, 1, 6, 7), new Matrix(1, 0, 0, 1, 6, 0), new Matrix(1, 3, 4, 1, 12, 7) };
        yield return new object[] { new Matrix(1, 3, 4, 1, 6, 7), new Matrix(1, 0, 0, 1, 0, 7), new Matrix(1, 3, 4, 1, 6, 14) };
        yield return new object[] { new Matrix(1, 3, 4, 1, 6, 7), new Matrix(1, 0, 0, 1, 6, 7), new Matrix(1, 3, 4, 1, 12, 14) };

        // Translated and skewed + translated and scaled.
        yield return new object[] { new Matrix(1, 3, 4, 1, 6, 7), new Matrix(2, 0, 0, 1, 6, 7), new Matrix(2, 3, 8, 1, 18, 14) };
        yield return new object[] { new Matrix(1, 3, 4, 1, 6, 7), new Matrix(1, 0, 0, 5, 6, 7), new Matrix(1, 15, 4, 5, 12, 42) };
        yield return new object[] { new Matrix(1, 3, 4, 1, 6, 7), new Matrix(2, 0, 0, 5, 6, 0), new Matrix(2, 15, 8, 5, 18, 35) };
        yield return new object[] { new Matrix(1, 3, 4, 1, 6, 7), new Matrix(2, 0, 0, 5, 0, 7), new Matrix(2, 15, 8, 5, 12, 42) };
        yield return new object[] { new Matrix(1, 3, 4, 1, 6, 7), new Matrix(2, 0, 0, 5, 6, 7), new Matrix(2, 15, 8, 5, 18, 42) };

        // Translated and skewed + translated and skewed.
        yield return new object[] { new Matrix(1, 3, 4, 1, 6, 7), new Matrix(1, 3, 0, 1, 6, 7), new Matrix(1, 6, 4, 13, 12, 32) };
        yield return new object[] { new Matrix(1, 3, 4, 1, 6, 7), new Matrix(1, 0, 4, 1, 6, 7), new Matrix(13, 3, 8, 1, 40, 14) };
        yield return new object[] { new Matrix(1, 3, 4, 1, 6, 7), new Matrix(1, 3, 4, 1, 6, 0), new Matrix(13, 6, 8, 13, 40, 25) };
        yield return new object[] { new Matrix(1, 3, 4, 1, 6, 7), new Matrix(1, 3, 4, 1, 0, 7), new Matrix(13, 6, 8, 13, 34, 32) };
        yield return new object[] { new Matrix(1, 3, 4, 1, 6, 7), new Matrix(1, 3, 4, 1, 6, 7), new Matrix(13, 6, 8, 13, 40, 32) };

        // Translated and skewed + skewed and scaled.
        yield return new object[] { new Matrix(1, 3, 4, 1, 6, 7), new Matrix(2, 3, 4, 1, 0, 0), new Matrix(14, 6, 12, 13, 40, 25) };
        yield return new object[] { new Matrix(1, 3, 4, 1, 6, 7), new Matrix(1, 3, 4, 5, 0, 0), new Matrix(13, 18, 8, 17, 34, 53) };
        yield return new object[] { new Matrix(1, 3, 4, 1, 6, 7), new Matrix(2, 3, 0, 5, 0, 0), new Matrix(2, 18, 8, 17, 12, 53) };
        yield return new object[] { new Matrix(1, 3, 4, 1, 6, 7), new Matrix(2, 0, 4, 5, 0, 0), new Matrix(14, 15, 12, 5, 40, 35) };
        yield return new object[] { new Matrix(1, 3, 4, 1, 6, 7), new Matrix(2, 3, 4, 5, 0, 0), new Matrix(14, 18, 12, 17, 40, 53) };

        // Translated and skewed + complex.
        yield return new object[] { new Matrix(1, 3, 4, 1, 6, 7), new Matrix(2, 3, 4, 5, 6, 7), new Matrix(14, 18, 12, 17, 46, 60) };
        yield return new object[] { new Matrix(1, 3, 4, 1, 6, 7), new Matrix(0, 0, 0, 0, 0, 0), new Matrix(0, 0, 0, 0, 0, 0) };

        // Skewed and scaled + identity.
        foreach (Matrix identity in IdentityMatrices())
        {
            yield return new object[] { new Matrix(2, 3, 4, 5, 0, 0), identity, new Matrix(2, 3, 4, 5, 0, 0) };
        }

        // Skewed and scaled + scaled.
        yield return new object[] { new Matrix(2, 3, 4, 5, 0, 0), new Matrix(2, 0, 0, 1, 0, 0), new Matrix(4, 3, 8, 5, 0, 0) };
        yield return new object[] { new Matrix(2, 3, 4, 5, 0, 0), new Matrix(1, 0, 0, 5, 0, 0), new Matrix(2, 15, 4, 25, 0, 0) };
        yield return new object[] { new Matrix(2, 3, 4, 5, 0, 0), new Matrix(2, 0, 0, 5, 0, 0), new Matrix(4, 15, 8, 25, 0, 0) };

        // Skewed and scaled + skewed.
        yield return new object[] { new Matrix(2, 3, 4, 5, 0, 0), new Matrix(1, 3, 0, 1, 0, 0), new Matrix(2, 9, 4, 17, 0, 0) };
        yield return new object[] { new Matrix(2, 3, 4, 5, 0, 0), new Matrix(1, 0, 4, 1, 0, 0), new Matrix(14, 3, 24, 5, 0, 0) };
        yield return new object[] { new Matrix(2, 3, 4, 5, 0, 0), new Matrix(1, 3, 4, 1, 0, 0), new Matrix(14, 9, 24, 17, 0, 0) };

        // Skewed and scaled + translated.
        yield return new object[] { new Matrix(2, 3, 4, 5, 0, 0), new Matrix(1, 0, 0, 1, 6, 0), new Matrix(2, 3, 4, 5, 6, 0) };
        yield return new object[] { new Matrix(2, 3, 4, 5, 0, 0), new Matrix(1, 0, 0, 1, 0, 7), new Matrix(2, 3, 4, 5, 0, 7) };
        yield return new object[] { new Matrix(2, 3, 4, 5, 0, 0), new Matrix(1, 0, 0, 1, 6, 7), new Matrix(2, 3, 4, 5, 6, 7) };

        // Skewed and scaled + translated and scaled.
        yield return new object[] { new Matrix(2, 3, 4, 5, 0, 0), new Matrix(2, 0, 0, 1, 6, 7), new Matrix(4, 3, 8, 5, 6, 7) };
        yield return new object[] { new Matrix(2, 3, 4, 5, 0, 0), new Matrix(1, 0, 0, 5, 6, 7), new Matrix(2, 15, 4, 25, 6, 7) };
        yield return new object[] { new Matrix(2, 3, 4, 5, 0, 0), new Matrix(2, 0, 0, 5, 6, 0), new Matrix(4, 15, 8, 25, 6, 0) };
        yield return new object[] { new Matrix(2, 3, 4, 5, 0, 0), new Matrix(2, 0, 0, 5, 0, 7), new Matrix(4, 15, 8, 25, 0, 7) };
        yield return new object[] { new Matrix(2, 3, 4, 5, 0, 0), new Matrix(2, 0, 0, 5, 6, 7), new Matrix(4, 15, 8, 25, 6, 7) };

        // Skewed and scaled + translated and skewed.
        yield return new object[] { new Matrix(2, 3, 4, 5, 0, 0), new Matrix(1, 3, 0, 1, 6, 7), new Matrix(2, 9, 4, 17, 6, 7) };
        yield return new object[] { new Matrix(2, 3, 4, 5, 0, 0), new Matrix(1, 0, 4, 1, 6, 7), new Matrix(14, 3, 24, 5, 6, 7) };
        yield return new object[] { new Matrix(2, 3, 4, 5, 0, 0), new Matrix(1, 3, 4, 1, 6, 0), new Matrix(14, 9, 24, 17, 6, 0) };
        yield return new object[] { new Matrix(2, 3, 4, 5, 0, 0), new Matrix(1, 3, 4, 1, 0, 7), new Matrix(14, 9, 24, 17, 0, 7) };
        yield return new object[] { new Matrix(2, 3, 4, 5, 0, 0), new Matrix(1, 3, 4, 1, 6, 7), new Matrix(14, 9, 24, 17, 6, 7) };

        // Skewed and scaled + skewed and scaled.
        yield return new object[] { new Matrix(2, 3, 4, 5, 0, 0), new Matrix(2, 3, 4, 1, 0, 0), new Matrix(16, 9, 28, 17, 0, 0) };
        yield return new object[] { new Matrix(2, 3, 4, 5, 0, 0), new Matrix(1, 3, 4, 5, 0, 0), new Matrix(14, 21, 24, 37, 0, 0) };
        yield return new object[] { new Matrix(2, 3, 4, 5, 0, 0), new Matrix(2, 3, 0, 5, 0, 0), new Matrix(4, 21, 8, 37, 0, 0) };
        yield return new object[] { new Matrix(2, 3, 4, 5, 0, 0), new Matrix(2, 0, 4, 5, 0, 0), new Matrix(16, 15, 28, 25, 0, 0) };
        yield return new object[] { new Matrix(2, 3, 4, 5, 0, 0), new Matrix(2, 3, 4, 5, 0, 0), new Matrix(16, 21, 28, 37, 0, 0) };

        // Skewed and scaled + complex.
        yield return new object[] { new Matrix(2, 3, 4, 5, 0, 0), new Matrix(2, 3, 4, 5, 6, 7), new Matrix(16, 21, 28, 37, 6, 7) };
        yield return new object[] { new Matrix(2, 3, 4, 5, 0, 0), new Matrix(0, 0, 0, 0, 0, 0), new Matrix(0, 0, 0, 0, 0, 0) };

        // Complex + identity.
        foreach (Matrix identity in IdentityMatrices())
        {
            yield return new object[] { new Matrix(2, 3, 4, 5, 6, 7), identity, new Matrix(2, 3, 4, 5, 6, 7) };
        }

        // Complex + scaled.
        yield return new object[] { new Matrix(2, 3, 4, 5, 6, 7), new Matrix(2, 0, 0, 1, 0, 0), new Matrix(4, 3, 8, 5, 12, 7) };
        yield return new object[] { new Matrix(2, 3, 4, 5, 6, 7), new Matrix(1, 0, 0, 5, 0, 0), new Matrix(2, 15, 4, 25, 6, 35) };
        yield return new object[] { new Matrix(2, 3, 4, 5, 6, 7), new Matrix(2, 0, 0, 5, 0, 0), new Matrix(4, 15, 8, 25, 12, 35) };

        // Complex + skewed.
        yield return new object[] { new Matrix(2, 3, 4, 5, 6, 7), new Matrix(1, 3, 0, 1, 0, 0), new Matrix(2, 9, 4, 17, 6, 25) };
        yield return new object[] { new Matrix(2, 3, 4, 5, 6, 7), new Matrix(1, 0, 4, 1, 0, 0), new Matrix(14, 3, 24, 5, 34, 7) };
        yield return new object[] { new Matrix(2, 3, 4, 5, 6, 7), new Matrix(1, 3, 4, 1, 0, 0), new Matrix(14, 9, 24, 17, 34, 25) };

        // Complex + translated.
        yield return new object[] { new Matrix(2, 3, 4, 5, 6, 7), new Matrix(1, 0, 0, 1, 6, 0), new Matrix(2, 3, 4, 5, 12, 7) };
        yield return new object[] { new Matrix(2, 3, 4, 5, 6, 7), new Matrix(1, 0, 0, 1, 0, 7), new Matrix(2, 3, 4, 5, 6, 14) };
        yield return new object[] { new Matrix(2, 3, 4, 5, 6, 7), new Matrix(1, 0, 0, 1, 6, 7), new Matrix(2, 3, 4, 5, 12, 14) };

        // Complex + translated and scaled.
        yield return new object[] { new Matrix(2, 3, 4, 5, 6, 7), new Matrix(2, 0, 0, 1, 6, 7), new Matrix(4, 3, 8, 5, 18, 14) };
        yield return new object[] { new Matrix(2, 3, 4, 5, 6, 7), new Matrix(1, 0, 0, 5, 6, 7), new Matrix(2, 15, 4, 25, 12, 42) };
        yield return new object[] { new Matrix(2, 3, 4, 5, 6, 7), new Matrix(2, 0, 0, 5, 6, 0), new Matrix(4, 15, 8, 25, 18, 35) };
        yield return new object[] { new Matrix(2, 3, 4, 5, 6, 7), new Matrix(2, 0, 0, 5, 0, 7), new Matrix(4, 15, 8, 25, 12, 42) };
        yield return new object[] { new Matrix(2, 3, 4, 5, 6, 7), new Matrix(2, 0, 0, 5, 6, 7), new Matrix(4, 15, 8, 25, 18, 42) };

        // Complex + translated and skewed.
        yield return new object[] { new Matrix(2, 3, 4, 5, 6, 7), new Matrix(1, 3, 0, 1, 6, 7), new Matrix(2, 9, 4, 17, 12, 32) };
        yield return new object[] { new Matrix(2, 3, 4, 5, 6, 7), new Matrix(1, 0, 4, 1, 6, 7), new Matrix(14, 3, 24, 5, 40, 14) };
        yield return new object[] { new Matrix(2, 3, 4, 5, 6, 7), new Matrix(1, 3, 4, 1, 6, 0), new Matrix(14, 9, 24, 17, 40, 25) };
        yield return new object[] { new Matrix(2, 3, 4, 5, 6, 7), new Matrix(1, 3, 4, 1, 0, 7), new Matrix(14, 9, 24, 17, 34, 32) };
        yield return new object[] { new Matrix(2, 3, 4, 5, 6, 7), new Matrix(1, 3, 4, 1, 6, 7), new Matrix(14, 9, 24, 17, 40, 32) };

        // Complex + skewed and scaled.
        yield return new object[] { new Matrix(2, 3, 4, 5, 6, 7), new Matrix(2, 3, 4, 1, 0, 0), new Matrix(16, 9, 28, 17, 40, 25) };
        yield return new object[] { new Matrix(2, 3, 4, 5, 6, 7), new Matrix(1, 3, 4, 5, 0, 0), new Matrix(14, 21, 24, 37, 34, 53) };
        yield return new object[] { new Matrix(2, 3, 4, 5, 6, 7), new Matrix(2, 3, 0, 5, 0, 0), new Matrix(4, 21, 8, 37, 12, 53) };
        yield return new object[] { new Matrix(2, 3, 4, 5, 6, 7), new Matrix(2, 0, 4, 5, 0, 0), new Matrix(16, 15, 28, 25, 40, 35) };
        yield return new object[] { new Matrix(2, 3, 4, 5, 6, 7), new Matrix(2, 3, 4, 5, 0, 0), new Matrix(16, 21, 28, 37, 40, 53) };

        // Complex + complex.
        yield return new object[] { new Matrix(2, 3, 4, 5, 6, 7), new Matrix(2, 3, 4, 5, 6, 7), new Matrix(16, 21, 28, 37, 46, 60) };
        yield return new object[] { new Matrix(2, 3, 4, 5, 6, 7), new Matrix(0, 0, 0, 0, 0, 0), new Matrix(0, 0, 0, 0, 0, 0) };

        // Zero + identity.
        foreach (Matrix identity in IdentityMatrices())
        {
            yield return new object[] { new Matrix(0, 0, 0, 0, 0, 0), identity, new Matrix(0, 0, 0, 0, 0, 0) };
        }

        // Zero + scaled.
        yield return new object[] { new Matrix(0, 0, 0, 0, 0, 0), new Matrix(2, 0, 0, 1, 0, 0), new Matrix(0, 0, 0, 0, 0, 0) };
        yield return new object[] { new Matrix(0, 0, 0, 0, 0, 0), new Matrix(1, 0, 0, 5, 0, 0), new Matrix(0, 0, 0, 0, 0, 0) };
        yield return new object[] { new Matrix(0, 0, 0, 0, 0, 0), new Matrix(2, 0, 0, 5, 0, 0), new Matrix(0, 0, 0, 0, 0, 0) };

        // Zero + skewed.
        yield return new object[] { new Matrix(0, 0, 0, 0, 0, 0), new Matrix(1, 3, 0, 1, 0, 0), new Matrix(0, 0, 0, 0, 0, 0) };
        yield return new object[] { new Matrix(0, 0, 0, 0, 0, 0), new Matrix(1, 0, 4, 1, 0, 0), new Matrix(0, 0, 0, 0, 0, 0) };
        yield return new object[] { new Matrix(0, 0, 0, 0, 0, 0), new Matrix(1, 3, 4, 1, 0, 0), new Matrix(0, 0, 0, 0, 0, 0) };

        // Zero + translated.
        yield return new object[] { new Matrix(0, 0, 0, 0, 0, 0), new Matrix(1, 0, 0, 1, 6, 0), new Matrix(0, 0, 0, 0, 6, 0) };
        yield return new object[] { new Matrix(0, 0, 0, 0, 0, 0), new Matrix(1, 0, 0, 1, 0, 7), new Matrix(0, 0, 0, 0, 0, 7) };
        yield return new object[] { new Matrix(0, 0, 0, 0, 0, 0), new Matrix(1, 0, 0, 1, 6, 7), new Matrix(0, 0, 0, 0, 6, 7) };

        // Zero + translated and scaled.
        yield return new object[] { new Matrix(0, 0, 0, 0, 0, 0), new Matrix(2, 0, 0, 1, 6, 7), new Matrix(0, 0, 0, 0, 6, 7) };
        yield return new object[] { new Matrix(0, 0, 0, 0, 0, 0), new Matrix(1, 0, 0, 5, 6, 7), new Matrix(0, 0, 0, 0, 6, 7) };
        yield return new object[] { new Matrix(0, 0, 0, 0, 0, 0), new Matrix(2, 0, 0, 5, 6, 0), new Matrix(0, 0, 0, 0, 6, 0) };
        yield return new object[] { new Matrix(0, 0, 0, 0, 0, 0), new Matrix(2, 0, 0, 5, 0, 7), new Matrix(0, 0, 0, 0, 0, 7) };
        yield return new object[] { new Matrix(0, 0, 0, 0, 0, 0), new Matrix(2, 0, 0, 5, 6, 7), new Matrix(0, 0, 0, 0, 6, 7) };

        // Zero + translated and skewed.
        yield return new object[] { new Matrix(0, 0, 0, 0, 0, 0), new Matrix(1, 3, 0, 1, 6, 7), new Matrix(0, 0, 0, 0, 6, 7) };
        yield return new object[] { new Matrix(0, 0, 0, 0, 0, 0), new Matrix(1, 0, 4, 1, 6, 7), new Matrix(0, 0, 0, 0, 6, 7) };
        yield return new object[] { new Matrix(0, 0, 0, 0, 0, 0), new Matrix(1, 3, 4, 1, 6, 0), new Matrix(0, 0, 0, 0, 6, 0) };
        yield return new object[] { new Matrix(0, 0, 0, 0, 0, 0), new Matrix(1, 3, 4, 1, 0, 7), new Matrix(0, 0, 0, 0, 0, 7) };
        yield return new object[] { new Matrix(0, 0, 0, 0, 0, 0), new Matrix(1, 3, 4, 1, 6, 7), new Matrix(0, 0, 0, 0, 6, 7) };

        // Zero + skewed and scaled.
        yield return new object[] { new Matrix(0, 0, 0, 0, 0, 0), new Matrix(2, 3, 4, 1, 0, 0), new Matrix(0, 0, 0, 0, 0, 0) };
        yield return new object[] { new Matrix(0, 0, 0, 0, 0, 0), new Matrix(1, 3, 4, 5, 0, 0), new Matrix(0, 0, 0, 0, 0, 0) };
        yield return new object[] { new Matrix(0, 0, 0, 0, 0, 0), new Matrix(2, 3, 0, 5, 0, 0), new Matrix(0, 0, 0, 0, 0, 0) };
        yield return new object[] { new Matrix(0, 0, 0, 0, 0, 0), new Matrix(2, 0, 4, 5, 0, 0), new Matrix(0, 0, 0, 0, 0, 0) };
        yield return new object[] { new Matrix(0, 0, 0, 0, 0, 0), new Matrix(2, 3, 4, 5, 0, 0), new Matrix(0, 0, 0, 0, 0, 0) };

        // Zero + complex.
        yield return new object[] { new Matrix(0, 0, 0, 0, 0, 0), new Matrix(2, 3, 4, 5, 6, 7), new Matrix(0, 0, 0, 0, 6, 7) };
        yield return new object[] { new Matrix(0, 0, 0, 0, 0, 0), new Matrix(0, 0, 0, 0, 0, 0), new Matrix(0, 0, 0, 0, 0, 0) };

        // NaN.
        yield return new object[] { new Matrix(double.NaN, 3, 4, 5, 6, 7), new Matrix(double.NaN, 3, 4, 5, 6, 7), new Matrix(double.NaN, double.NaN, double.NaN, 37, double.NaN, 60) };
        yield return new object[] { new Matrix(double.NaN, 3, 4, 5, 6, 7), new Matrix(2, 3, 4, 5, 6, 7), new Matrix(double.NaN, double.NaN, 28, 37, 46, 60) };
        yield return new object[] { new Matrix(2, double.NaN, 4, 5, 6, 7), new Matrix(2, double.NaN, 4, 5, 6, 7), new Matrix(double.NaN, double.NaN, 28, double.NaN, 46, double.NaN) };
        yield return new object[] { new Matrix(2, double.NaN, 4, 5, 6, 7), new Matrix(2, 3, 4, 5, 6, 7), new Matrix(double.NaN, double.NaN, 28, 37, 46, 60) };
        yield return new object[] { new Matrix(2, 3, double.NaN, 5, 6, 7), new Matrix(2, 3, double.NaN, 5, 6, 7), new Matrix(double.NaN, 21, double.NaN, double.NaN, double.NaN, 60) };
        yield return new object[] { new Matrix(2, 3, double.NaN, 5, 6, 7), new Matrix(2, 3, 4, 5, 6, 7), new Matrix(16, 21, double.NaN, double.NaN, 46, 60) };
        yield return new object[] { new Matrix(2, 3, 4, double.NaN, 6, 7), new Matrix(2, 3, 4, double.NaN, 6, 7), new Matrix(16, double.NaN, double.NaN, double.NaN, 46, double.NaN) };
        yield return new object[] { new Matrix(2, 3, 4, double.NaN, 6, 7), new Matrix(2, 3, 4, 5, 6, 7), new Matrix(16, 21, double.NaN, double.NaN, 46, 60) };
        yield return new object[] { new Matrix(2, 3, 4, 5, double.NaN, 7), new Matrix(2, 3, 4, 5, double.NaN, 7), new Matrix(16, 21, 28, 37, double.NaN, double.NaN) };
        yield return new object[] { new Matrix(2, 3, 4, 5, double.NaN, 7), new Matrix(2, 3, 4, 5, 6, 7), new Matrix(16, 21, 28, 37, double.NaN, double.NaN) };
        yield return new object[] { new Matrix(2, 3, 4, 5, 6, double.NaN), new Matrix(2, 3, 4, 5, 6, double.NaN), new Matrix(16, 21, 28, 37, double.NaN, double.NaN) };
        yield return new object[] { new Matrix(2, 3, 4, 5, 6, double.NaN), new Matrix(2, 3, 4, 5, 6, 7), new Matrix(16, 21, 28, 37, double.NaN, double.NaN) };
        yield return new object[] { new Matrix(double.NaN, double.NaN, double.NaN, double.NaN, double.NaN, double.NaN), new Matrix(double.NaN, double.NaN, double.NaN, double.NaN, double.NaN, double.NaN), new Matrix(double.NaN, double.NaN, double.NaN, double.NaN, double.NaN, double.NaN) };
        yield return new object[] { new Matrix(double.NaN, double.NaN, double.NaN, double.NaN, double.NaN, double.NaN), new Matrix(2, 3, 4, 5, 6, 7), new Matrix(double.NaN, double.NaN, double.NaN, double.NaN, double.NaN, double.NaN) };
        yield return new object[] { new Matrix(2, 3, 4, 5, 6, 7), new Matrix(double.NaN, 3, 4, 5, 6, 7), new Matrix(double.NaN, 21, double.NaN, 37, double.NaN, 60) };
        yield return new object[] { new Matrix(2, 3, 4, 5, 6, 7), new Matrix(2, double.NaN, 4, 5, 6, 7), new Matrix(16, double.NaN, 28, double.NaN, 46, double.NaN) };
        yield return new object[] { new Matrix(2, 3, 4, 5, 6, 7), new Matrix(2, 3, double.NaN, 5, 6, 7), new Matrix(double.NaN, 21, double.NaN, 37, double.NaN, 60) };
        yield return new object[] { new Matrix(2, 3, 4, 5, 6, 7), new Matrix(2, 3, 4, double.NaN, 6, 7), new Matrix(16, double.NaN, 28, double.NaN, 46, double.NaN) };
        yield return new object[] { new Matrix(2, 3, 4, 5, 6, 7), new Matrix(2, 3, 4, 5, double.NaN, 7), new Matrix(16, 21, 28, 37, double.NaN, 60) };
        yield return new object[] { new Matrix(2, 3, 4, 5, 6, 7), new Matrix(2, 3, 4, 5, 6, double.NaN), new Matrix(16, 21, 28, 37, 46, double.NaN) };
        yield return new object[] { new Matrix(2, 3, 4, 5, 6, 7), new Matrix(double.NaN, double.NaN, double.NaN, double.NaN, double.NaN, double.NaN), new Matrix(double.NaN, double.NaN, double.NaN, double.NaN, double.NaN, double.NaN) };
    }

    [Theory]
    [MemberData(nameof(Multiply_TestData))]
    public void Multiply_Invoke_ReturnsExpected(Matrix matrix1, Matrix matrix2, Matrix expected)
    {
        Matrix copy1 = matrix1;
        Matrix copy2 = matrix2;

        Matrix result = Matrix.Multiply(matrix1, matrix2);
        Assert.Equal(expected, result);
        Assert.Equal(copy1, matrix1);
        Assert.Equal(copy2, matrix2);
    }

    [Theory]
    [MemberData(nameof(Multiply_TestData))]
    public void OperatorMultiply_Invoke_ReturnsExpected(Matrix matrix1, Matrix matrix2, Matrix expected)
    {
        Matrix copy1 = matrix1;
        Matrix copy2 = matrix2;

        Matrix result = matrix1 * matrix2;
        Assert.Equal(expected, result);
        Assert.Equal(copy1, matrix1);
        Assert.Equal(copy2, matrix2);
    }

    [Theory]
    [MemberData(nameof(Multiply_TestData))]
    public void Prepend_Invoke_ReturnsExpected(Matrix matrix1, Matrix matrix2, Matrix expected)
    {
        Matrix copy1 = matrix1;

        matrix2.Prepend(matrix1);
        Assert.Equal(expected, matrix2);
        Assert.Equal(copy1, matrix1);
    }

    public static IEnumerable<object[]> Rotate_TestData()
    {
        // Identity
        foreach (Matrix matrix in IdentityMatrices())
        {
            yield return new object[] { matrix, -360, new Matrix(1, -0, 0, 1, 0, 0), 1 };
            yield return new object[] { matrix, -270, new Matrix(-0, 1, -1, -0, 0, 0), 1 };
            yield return new object[] { matrix, -180, new Matrix(-1, -0, 0, -1, 0, 0), 1 };
            yield return new object[] { matrix, -90, new Matrix(0, -1, 1, 0, 0, 0), 1 };
            yield return new object[] { matrix, -45, new Matrix(0.70711, -0.70711, 0.70711, 0.70711, 0, 0), 1 };
            yield return new object[] { matrix, 45, new Matrix(0.70711, 0.70711, -0.70711, 0.70711, 0, 0), 1 };
            yield return new object[] { matrix, 0, new Matrix(1, 0, -0, 1, 0, 0), 1 };
            yield return new object[] { matrix, 90, new Matrix(0, 1, -1, 0, 0, 0), 1 };
            yield return new object[] { matrix, 30, new Matrix(0.86603, 0.5, -0.5, 0.86603, 0, 0), 1 };
            yield return new object[] { matrix, 180, new Matrix(-1, 0, -0, -1, 0, 0), 1 };
            yield return new object[] { matrix, 270, new Matrix(-0, -1, 1, -0, 0, 0), 1 };
            yield return new object[] { matrix, 360, new Matrix(1, 0, -0, 1, 0, 0), 1 };
            yield return new object[] { matrix, 720, new Matrix(1, 0, -0, 1, 0, 0), 1 };
            yield return new object[] { matrix, double.NaN, new Matrix(double.NaN, double.NaN, double.NaN, double.NaN, double.NaN, double.NaN), double.NaN };
        }

        // Scaled.
        yield return new object[] { new Matrix(2, 0, 0, 3, 0, 0), -360, new Matrix(2, 0, 0, 3, 0, 0), 6 };
        yield return new object[] { new Matrix(2, 0, 0, 3, 0, 0), -270, new Matrix(-0, 2, -3, -0, 0, 0), 6 };
        yield return new object[] { new Matrix(2, 0, 0, 3, 0, 0), -180, new Matrix(-2, -0, 0, -3, 0, 0), 6 };
        yield return new object[] { new Matrix(2, 0, 0, 3, 0, 0), -90, new Matrix(0, -2, 3, 0, 0, 0), 6 };
        yield return new object[] { new Matrix(2, 0, 0, 3, 0, 0), -45, new Matrix(1.41421, -1.41421, 2.12132, 2.12132, 0, 0), 6 };
        yield return new object[] { new Matrix(2, 0, 0, 3, 0, 0), 45, new Matrix(1.41421, 1.41421, -2.12132, 2.12132, 0, 0), 6 };
        yield return new object[] { new Matrix(2, 0, 0, 3, 0, 0), 0, new Matrix(2, 0, 0, 3, 0, 0), 6 };
        yield return new object[] { new Matrix(2, 0, 0, 3, 0, 0), 90, new Matrix(0, 2, -3, 0, 0, 0), 6 };
        yield return new object[] { new Matrix(2, 0, 0, 3, 0, 0), 30, new Matrix(1.73205, 1, -1.5, 2.59808, 0, 0), 6 };
        yield return new object[] { new Matrix(2, 0, 0, 3, 0, 0), 180, new Matrix(-2, 0, -0, -3, 0, 0), 6 };
        yield return new object[] { new Matrix(2, 0, 0, 3, 0, 0), 270, new Matrix(-0, -2, 3, -0, 0, 0), 6 };
        yield return new object[] { new Matrix(2, 0, 0, 3, 0, 0), 360, new Matrix(2, 0, 0, 3, 0, 0), 6 };
        yield return new object[] { new Matrix(2, 0, 0, 3, 0, 0), 720, new Matrix(2, 0, 0, 3, 0, 0), 6 };
        yield return new object[] { new Matrix(2, 0, 0, 3, 0, 0), double.NaN, new Matrix(double.NaN, double.NaN, double.NaN, double.NaN, double.NaN, double.NaN), double.NaN };

        // Skewed.
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 0, 0), -360, new Matrix(1, 0.5, 0.75, 1, 0, 0), 0.625 };
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 0, 0), -270, new Matrix(-0.5, 1, -1, 0.75, 0, 0), 0.625 };
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 0, 0), -180, new Matrix(-1, -0.5, -0.75, -1, 0, 0), 0.625 };
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 0, 0), -90, new Matrix(0.5, -1, 1, -0.75, 0, 0), 0.625 };
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 0, 0), -45, new Matrix(1.06066, -0.35355, 1.23744, 0.17678, 0, 0), 0.625 };
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 0, 0), 45, new Matrix(0.35355, 1.06066, -0.17678, 1.23744, 0, 0), 0.625 };
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 0, 0), 0, new Matrix(1, 0.5, 0.75, 1, 0, 0), 0.625 };
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 0, 0), 90, new Matrix(-0.5, 1, -1, 0.75, 0, 0), 0.625 };
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 0, 0), 30, new Matrix(0.61603, 0.93301, 0.14951, 1.24103, 0, 0), 0.625 };
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 0, 0), 180, new Matrix(-1, -0.5, -0.75, -1, 0, 0), 0.625 };
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 0, 0), 270, new Matrix(0.5, -1, 1, -0.75, 0, 0), 0.625 };
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 0, 0), 360, new Matrix(1, 0.5, 0.75, 1, 0, 0), 0.625 };
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 0, 0), 720, new Matrix(1, 0.5, 0.75, 1, 0, 0), 0.625 };
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 0, 0), double.NaN, new Matrix(double.NaN, double.NaN, double.NaN, double.NaN, double.NaN, double.NaN), double.NaN };

        // Translated.
        yield return new object[] { new Matrix(1, 0, 0, 1, 2, 3), -360, new Matrix(1, -0, 0, 1, 2, 3), 1 };
        yield return new object[] { new Matrix(1, 0, 0, 1, 2, 3), -270, new Matrix(-0, 1, -1, -0, -3, 2), 1 };
        yield return new object[] { new Matrix(1, 0, 0, 1, 2, 3), -180, new Matrix(-1, -0, 0, -1, -2, -3), 1 };
        yield return new object[] { new Matrix(1, 0, 0, 1, 2, 3), -90, new Matrix(0, -1, 1, 0, 3, -2), 1 };
        yield return new object[] { new Matrix(1, 0, 0, 1, 2, 3), -45, new Matrix(0.70711, -0.70711, 0.70711, 0.70711, 3.53553, 0.70711), 1 };
        yield return new object[] { new Matrix(1, 0, 0, 1, 2, 3), 45, new Matrix(0.70711, 0.70711, -0.70711, 0.70711, -0.70711, 3.53553), 1 };
        yield return new object[] { new Matrix(1, 0, 0, 1, 2, 3), 0, new Matrix(1, 0, -0, 1, 2, 3), 1 };
        yield return new object[] { new Matrix(1, 0, 0, 1, 2, 3), 90, new Matrix(0, 1, -1, 0, -3, 2), 1 };
        yield return new object[] { new Matrix(1, 0, 0, 1, 2, 3), 30, new Matrix(0.86603, 0.5, -0.5, 0.86603, 0.23205, 3.59808), 1 };
        yield return new object[] { new Matrix(1, 0, 0, 1, 2, 3), 180, new Matrix(-1, 0, -0, -1, -2, -3), 1 };
        yield return new object[] { new Matrix(1, 0, 0, 1, 2, 3), 270, new Matrix(-0, -1, 1, -0, 3, -2), 1 };
        yield return new object[] { new Matrix(1, 0, 0, 1, 2, 3), 360, new Matrix(1, 0, -0, 1, 2, 3), 1 };
        yield return new object[] { new Matrix(1, 0, 0, 1, 2, 3), 720, new Matrix(1, 0, -0, 1, 2, 3), 1 };
        yield return new object[] { new Matrix(1, 0, 0, 1, 2, 3), double.NaN, new Matrix(double.NaN, double.NaN, double.NaN, double.NaN, double.NaN, double.NaN), double.NaN };

        // Translated and scaled.
        yield return new object[] { new Matrix(2, 0, 0, 3, 1, 2), -360, new Matrix(2, 0, 0, 3, 1, 2), 6 };
        yield return new object[] { new Matrix(2, 0, 0, 3, 1, 2), -270, new Matrix(-0, 2, -3, -0, -2, 1), 6 };
        yield return new object[] { new Matrix(2, 0, 0, 3, 1, 2), -180, new Matrix(-2, -0, 0, -3, -1, -2), 6 };
        yield return new object[] { new Matrix(2, 0, 0, 3, 1, 2), -90, new Matrix(0, -2, 3, 0, 2, -1), 6 };
        yield return new object[] { new Matrix(2, 0, 0, 3, 1, 2), -45, new Matrix(1.41421, -1.41421, 2.12132, 2.12132, 2.12132, 0.70711), 6 };
        yield return new object[] { new Matrix(2, 0, 0, 3, 1, 2), 45, new Matrix(1.41421, 1.41421, -2.12132, 2.12132, -0.70711, 2.12132), 6 };
        yield return new object[] { new Matrix(2, 0, 0, 3, 1, 2), 0, new Matrix(2, 0, 0, 3, 1, 2), 6 };
        yield return new object[] { new Matrix(2, 0, 0, 3, 1, 2), 90, new Matrix(0, 2, -3, 0, -2, 1), 6 };
        yield return new object[] { new Matrix(2, 0, 0, 3, 1, 2), 30, new Matrix(1.73205, 1, -1.5, 2.59808, -0.13397, 2.23205), 6 };
        yield return new object[] { new Matrix(2, 0, 0, 3, 1, 2), 180, new Matrix(-2, 0, -0, -3, -1, -2), 6 };
        yield return new object[] { new Matrix(2, 0, 0, 3, 1, 2), 270, new Matrix(-0, -2, 3, -0, 2, -1), 6 };
        yield return new object[] { new Matrix(2, 0, 0, 3, 1, 2), 360, new Matrix(2, 0, 0, 3, 1, 2), 6 };
        yield return new object[] { new Matrix(2, 0, 0, 3, 1, 2), 720, new Matrix(2, 0, 0, 3, 1, 2), 6 };
        yield return new object[] { new Matrix(2, 0, 0, 3, 1, 2), double.NaN, new Matrix(double.NaN, double.NaN, double.NaN, double.NaN, double.NaN, double.NaN), double.NaN };

        // Translated and skewed.
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 2, 3), -360, new Matrix(1, 0.5, 0.75, 1, 2, 3), 0.625 };
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 2, 3), -270, new Matrix(-0.5, 1, -1, 0.75, -3, 2), 0.625 };
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 2, 3), -180, new Matrix(-1, -0.5, -0.75, -1, -2, -3), 0.625 };
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 2, 3), -90, new Matrix(0.5, -1, 1, -0.75, 3, -2), 0.625 };
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 2, 3), -45, new Matrix(1.06066, -0.35355, 1.23744, 0.17678, 3.53553, 0.70711), 0.625 };
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 2, 3), 45, new Matrix(0.35355, 1.06066, -0.17678, 1.23744, -0.70711, 3.53553), 0.625 };
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 2, 3), 0, new Matrix(1, 0.5, 0.75, 1, 2, 3), 0.625 };
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 2, 3), 90, new Matrix(-0.5, 1, -1, 0.75, -3, 2), 0.625 };
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 2, 3), 30, new Matrix(0.61603, 0.93301, 0.14951, 1.24103, 0.23205, 3.59808), 0.625 };
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 2, 3), 180, new Matrix(-1, -0.5, -0.75, -1, -2, -3), 0.625 };
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 2, 3), 270, new Matrix(0.5, -1, 1, -0.75, 3, -2), 0.625 };
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 2, 3), 360, new Matrix(1, 0.5, 0.75, 1, 2, 3), 0.625 };
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 2, 3), 720, new Matrix(1, 0.5, 0.75, 1, 2, 3), 0.625 };
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 2, 3), double.NaN, new Matrix(double.NaN, double.NaN, double.NaN, double.NaN, double.NaN, double.NaN), double.NaN };

        // Skewed and scaled.
        yield return new object[] { new Matrix(2, 0.5, 0.75, 3, 0, 0), -360, new Matrix(2, 0.5, 0.75, 3, 0, 0), 5.625 };
        yield return new object[] { new Matrix(2, 0.5, 0.75, 3, 0, 0), -270, new Matrix(-0.5, 2, -3, 0.75, 0, 0), 5.625 };
        yield return new object[] { new Matrix(2, 0.5, 0.75, 3, 0, 0), -180, new Matrix(-2, -0.5, -0.75, -3, 0, 0), 5.625 };
        yield return new object[] { new Matrix(2, 0.5, 0.75, 3, 0, 0), -90, new Matrix(0.5, -2, 3, -0.75, 0, 0), 5.625 };
        yield return new object[] { new Matrix(2, 0.5, 0.75, 3, 0, 0), -45, new Matrix(1.76777, -1.06066, 2.65165, 1.59099, 0, 0), 5.625 };
        yield return new object[] { new Matrix(2, 0.5, 0.75, 3, 0, 0), 45, new Matrix(1.06066, 1.76777, -1.59099, 2.65165, 0, 0), 5.625 };
        yield return new object[] { new Matrix(2, 0.5, 0.75, 3, 0, 0), 0, new Matrix(2, 0.5, 0.75, 3, 0, 0), 5.625 };
        yield return new object[] { new Matrix(2, 0.5, 0.75, 3, 0, 0), 90, new Matrix(-0.5, 2, -3, 0.75, 0, 0), 5.625 };
        yield return new object[] { new Matrix(2, 0.5, 0.75, 3, 0, 0), 30, new Matrix(1.48205, 1.43301, -0.85048, 2.97308, 0, 0), 5.625 };
        yield return new object[] { new Matrix(2, 0.5, 0.75, 3, 0, 0), 180, new Matrix(-2, -0.5, -0.75, -3, 0, 0), 5.625 };
        yield return new object[] { new Matrix(2, 0.5, 0.75, 3, 0, 0), 270, new Matrix(0.5, -2, 3, -0.75, 0, 0), 5.625 };
        yield return new object[] { new Matrix(2, 0.5, 0.75, 3, 0, 0), 360, new Matrix(2, 0.5, 0.75, 3, 0, 0), 5.625 };
        yield return new object[] { new Matrix(2, 0.5, 0.75, 3, 0, 0), 720, new Matrix(2, 0.5, 0.75, 3, 0, 0), 5.625 };
        yield return new object[] { new Matrix(2, 0.5, 0.75, 3, 0, 0), double.NaN, new Matrix(double.NaN, double.NaN, double.NaN, double.NaN, double.NaN, double.NaN), double.NaN };

        // Complex.
        yield return new object[] { new Matrix(2, 3, 4, 5, 6, 7), -360, new Matrix(2, 3, 4, 5, 6, 7), -2 };
        yield return new object[] { new Matrix(2, 3, 4, 5, 6, 7), -270, new Matrix(-3, 2, -5, 4, -7, 6), -2 };
        yield return new object[] { new Matrix(2, 3, 4, 5, 6, 7), -180, new Matrix(-2, -3, -4, -5, -6, -7), -2 };
        yield return new object[] { new Matrix(2, 3, 4, 5, 6, 7), -90, new Matrix(3, -2, 5, -4, 7, -6), -2 };
        yield return new object[] { new Matrix(2, 3, 4, 5, 6, 7), -45, new Matrix(3.53553, 0.70711, 6.36396, 0.70711, 9.19239, 0.70711), -2 };
        yield return new object[] { new Matrix(2, 3, 4, 5, 6, 7), 45, new Matrix(-0.70711, 3.53553, -0.70711, 6.36396, -0.70711, 9.19239), -2 };
        yield return new object[] { new Matrix(2, 3, 4, 5, 6, 7), 0, new Matrix(2, 3, 4, 5, 6, 7), -2 };
        yield return new object[] { new Matrix(2, 3, 4, 5, 6, 7), 90, new Matrix(-3, 2, -5, 4, -7, 6), -2 };
        yield return new object[] { new Matrix(2, 3, 4, 5, 6, 7), 30, new Matrix(0.23205, 3.59808, 0.9641, 6.33013, 1.69615, 9.06218), -2 };
        yield return new object[] { new Matrix(2, 3, 4, 5, 6, 7), 180, new Matrix(-2, -3, -4, -5, -6, -7), -2 };
        yield return new object[] { new Matrix(2, 3, 4, 5, 6, 7), 270, new Matrix(3, -2, 5, -4, 7, -6), -2 };
        yield return new object[] { new Matrix(2, 3, 4, 5, 6, 7), 360, new Matrix(2, 3, 4, 5, 6, 7), -2 };
        yield return new object[] { new Matrix(2, 3, 4, 5, 6, 7), 720, new Matrix(2, 3, 4, 5, 6, 7), -2 };
        yield return new object[] { new Matrix(2, 3, 4, 5, 6, 7), double.NaN, new Matrix(double.NaN, double.NaN, double.NaN, double.NaN, double.NaN, double.NaN), double.NaN };

        // No inverse.
        yield return new object[] { new Matrix(0, 0, 0, 0, 0, 0), -360, new Matrix(0, 0, 0, 0, 0, 0), 0 };
        yield return new object[] { new Matrix(0, 0, 0, 0, 0, 0), -270, new Matrix(-0, 0, -0, 0, 0, 0), -0 };
        yield return new object[] { new Matrix(0, 0, 0, 0, 0, 0), -180, new Matrix(0, -0, 0, -0, 0, 0), -0 };
        yield return new object[] { new Matrix(0, 0, 0, 0, 0, 0), -90, new Matrix(0, 0, 0, 0, 0, 0), 0 };
        yield return new object[] { new Matrix(0, 0, 0, 0, 0, 0), -45, new Matrix(0, 0, 0, 0, 0, 0), 0 };
        yield return new object[] { new Matrix(0, 0, 0, 0, 0, 0), 45, new Matrix(0, 0, 0, 0, 0, 0), 0 };
        yield return new object[] { new Matrix(0, 0, 0, 0, 0, 0), 0, new Matrix(0, 0, 0, 0, 0, 0), 0 };
        yield return new object[] { new Matrix(0, 0, 0, 0, 0, 0), 90, new Matrix(0, 0, 0, 0, 0, 0), 0 };
        yield return new object[] { new Matrix(0, 0, 0, 0, 0, 0), 30, new Matrix(0, 0, 0, 0, 0, 0), 0 };
        yield return new object[] { new Matrix(0, 0, 0, 0, 0, 0), 180, new Matrix(-0, 0, -0, 0, 0, 0), -0 };
        yield return new object[] { new Matrix(0, 0, 0, 0, 0, 0), 270, new Matrix(0, -0, 0, -0, 0, 0), -0 };
        yield return new object[] { new Matrix(0, 0, 0, 0, 0, 0), 360, new Matrix(0, 0, 0, 0, 0, 0), 0 };
        yield return new object[] { new Matrix(0, 0, 0, 0, 0, 0), 720, new Matrix(0, 0, 0, 0, 0, 0), 0 };
        yield return new object[] { new Matrix(0, 0, 0, 0, 0, 0), double.NaN, new Matrix(double.NaN, double.NaN, double.NaN, double.NaN, double.NaN, double.NaN), double.NaN };
    }

    [Theory]
    [MemberData(nameof(Rotate_TestData))]
    
    public void Rotate_Invoke_Success(Matrix matrix, double angle, Matrix expected, double expectedDeterminant)
    {
        matrix.Rotate(angle);
        Helpers.AssertEqualRounded(expected, matrix);
        Assert.Equal(expected.IsIdentity, matrix.IsIdentity);
        Assert.Equal(expected.HasInverse, matrix.HasInverse);
        Assert.Equal(expectedDeterminant, matrix.Determinant, precision: 5);
    }

    public static IEnumerable<object[]> RotatePrepend_TestData()
    {
        // Identity
        foreach (Matrix matrix in IdentityMatrices())
        {
            yield return new object[] { matrix, -360, new Matrix(1, -0, 0, 1, 0, 0), 1 };
            yield return new object[] { matrix, -270, new Matrix(-0, 1, -1, -0, 0, 0), 1 };
            yield return new object[] { matrix, -180, new Matrix(-1, -0, 0, -1, 0, 0), 1 };
            yield return new object[] { matrix, -90, new Matrix(0, -1, 1, 0, 0, 0), 1 };
            yield return new object[] { matrix, -45, new Matrix(0.70711, -0.70711, 0.70711, 0.70711, 0, 0), 1 };
            yield return new object[] { matrix, 45, new Matrix(0.70711, 0.70711, -0.70711, 0.70711, 0, 0), 1 };
            yield return new object[] { matrix, 0, new Matrix(1, 0, -0, 1, 0, 0), 1 };
            yield return new object[] { matrix, 90, new Matrix(0, 1, -1, 0, 0, 0), 1 };
            yield return new object[] { matrix, 30, new Matrix(0.86603, 0.5, -0.5, 0.86603, 0, 0), 1 };
            yield return new object[] { matrix, 180, new Matrix(-1, 0, -0, -1, 0, 0), 1 };
            yield return new object[] { matrix, 270, new Matrix(-0, -1, 1, -0, 0, 0), 1 };
            yield return new object[] { matrix, 360, new Matrix(1, 0, -0, 1, 0, 0), 1 };
            yield return new object[] { matrix, 720, new Matrix(1, 0, -0, 1, 0, 0), 1 };
            yield return new object[] { matrix, double.NaN, new Matrix(double.NaN, double.NaN, double.NaN, double.NaN, double.NaN, double.NaN), double.NaN };
        }

        // Scaled.
        yield return new object[] { new Matrix(2, 0, 0, 3, 0, 0), -360, new Matrix(2, 0, 0, 3, 0, 0), 6 };
        yield return new object[] { new Matrix(2, 0, 0, 3, 0, 0), -270, new Matrix(-0, 3, -2, -0, 0, 0), 6 };
        yield return new object[] { new Matrix(2, 0, 0, 3, 0, 0), -180, new Matrix(-2, -0, 0, -3, 0, 0), 6 };
        yield return new object[] { new Matrix(2, 0, 0, 3, 0, 0), -90, new Matrix(0, -3, 2, 0, 0, 0), 6 };
        yield return new object[] { new Matrix(2, 0, 0, 3, 0, 0), -45, new Matrix(1.41421, -2.12132, 1.41421, 2.12132, 0, 0), 6 };
        yield return new object[] { new Matrix(2, 0, 0, 3, 0, 0), 45, new Matrix(1.41421, 2.12132, -1.41421, 2.12132, 0, 0), 6 };
        yield return new object[] { new Matrix(2, 0, 0, 3, 0, 0), 0, new Matrix(2, 0, 0, 3, 0, 0), 6 };
        yield return new object[] { new Matrix(2, 0, 0, 3, 0, 0), 90, new Matrix(0, 3, -2, 0, 0, 0), 6 };
        yield return new object[] { new Matrix(2, 0, 0, 3, 0, 0), 30, new Matrix(1.73205, 1.5, -1, 2.59808, 0, 0), 6 };
        yield return new object[] { new Matrix(2, 0, 0, 3, 0, 0), 180, new Matrix(-2, 0, -0, -3, 0, 0), 6 };
        yield return new object[] { new Matrix(2, 0, 0, 3, 0, 0), 270, new Matrix(-0, -3, 2, -0, 0, 0), 6 };
        yield return new object[] { new Matrix(2, 0, 0, 3, 0, 0), 360, new Matrix(2, 0, 0, 3, 0, 0), 6 };
        yield return new object[] { new Matrix(2, 0, 0, 3, 0, 0), 720, new Matrix(2, 0, 0, 3, 0, 0), 6 };
        yield return new object[] { new Matrix(2, 0, 0, 3, 0, 0), double.NaN, new Matrix(double.NaN, double.NaN, double.NaN, double.NaN, double.NaN, double.NaN), double.NaN };

        // Skewed.
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 0, 0), -360, new Matrix(1, 0.5, 0.75, 1, 0, 0), 0.625 };
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 0, 0), -270, new Matrix(0.75, 1, -1, -0.5, 0, 0), 0.625 };
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 0, 0), -180, new Matrix(-1, -0.5, -0.75, -1, 0, 0), 0.625 };
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 0, 0), -90, new Matrix(-0.75, -1, 1, 0.5, 0, 0), 0.625 };
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 0, 0), -45, new Matrix(0.17678, -0.35355, 1.23744, 1.06066, 0, 0), 0.625 };
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 0, 0), 45, new Matrix(1.23744, 1.06066, -0.17678, 0.35355, 0, 0), 0.625 };
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 0, 0), 0, new Matrix(1, 0.5, 0.75, 1, 0, 0), 0.625 };
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 0, 0), 90, new Matrix(0.75, 1, -1, -0.5, 0, 0), 0.625 };
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 0, 0), 30, new Matrix(1.24103, 0.93301, 0.14951, 0.61603, 0, 0), 0.625 };
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 0, 0), 180, new Matrix(-1, -0.5, -0.75, -1, 0, 0), 0.625 };
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 0, 0), 270, new Matrix(-0.75, -1, 1, 0.5, 0, 0), 0.625 };
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 0, 0), 360, new Matrix(1, 0.5, 0.75, 1, 0, 0), 0.625 };
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 0, 0), 720, new Matrix(1, 0.5, 0.75, 1, 0, 0), 0.625 };
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 0, 0), double.NaN, new Matrix(double.NaN, double.NaN, double.NaN, double.NaN, double.NaN, double.NaN), double.NaN };

        // Translated.
        yield return new object[] { new Matrix(1, 0, 0, 1, 2, 3), -360, new Matrix(1, -0, 0, 1, 2, 3), 1 };
        yield return new object[] { new Matrix(1, 0, 0, 1, 2, 3), -270, new Matrix(-0, 1, -1, -0, 2, 3), 1 };
        yield return new object[] { new Matrix(1, 0, 0, 1, 2, 3), -180, new Matrix(-1, -0, 0, -1, 2, 3), 1 };
        yield return new object[] { new Matrix(1, 0, 0, 1, 2, 3), -90, new Matrix(0, -1, 1, 0, 2, 3), 1 };
        yield return new object[] { new Matrix(1, 0, 0, 1, 2, 3), -45, new Matrix(0.70711, -0.70711, 0.70711, 0.70711, 2, 3), 1 };
        yield return new object[] { new Matrix(1, 0, 0, 1, 2, 3), 45, new Matrix(0.70711, 0.70711, -0.70711, 0.70711, 2, 3), 1 };
        yield return new object[] { new Matrix(1, 0, 0, 1, 2, 3), 0, new Matrix(1, 0, -0, 1, 2, 3), 1 };
        yield return new object[] { new Matrix(1, 0, 0, 1, 2, 3), 90, new Matrix(0, 1, -1, 0, 2, 3), 1 };
        yield return new object[] { new Matrix(1, 0, 0, 1, 2, 3), 30, new Matrix(0.86603, 0.5, -0.5, 0.86603, 2, 3), 1 };
        yield return new object[] { new Matrix(1, 0, 0, 1, 2, 3), 180, new Matrix(-1, 0, -0, -1, 2, 3), 1 };
        yield return new object[] { new Matrix(1, 0, 0, 1, 2, 3), 270, new Matrix(-0, -1, 1, -0, 2, 3), 1 };
        yield return new object[] { new Matrix(1, 0, 0, 1, 2, 3), 360, new Matrix(1, 0, -0, 1, 2, 3), 1 };
        yield return new object[] { new Matrix(1, 0, 0, 1, 2, 3), 720, new Matrix(1, 0, -0, 1, 2, 3), 1 };
        yield return new object[] { new Matrix(1, 0, 0, 1, 2, 3), double.NaN, new Matrix(double.NaN, double.NaN, double.NaN, double.NaN, double.NaN, double.NaN), double.NaN };

        // Translated and scaled.
        yield return new object[] { new Matrix(2, 0, 0, 3, 1, 2), -360, new Matrix(2, 0, 0, 3, 1, 2), 6 };
        yield return new object[] { new Matrix(2, 0, 0, 3, 1, 2), -270, new Matrix(-0, 3, -2, -0, 1, 2), 6 };
        yield return new object[] { new Matrix(2, 0, 0, 3, 1, 2), -180, new Matrix(-2, -0, 0, -3, 1, 2), 6 };
        yield return new object[] { new Matrix(2, 0, 0, 3, 1, 2), -90, new Matrix(0, -3, 2, 0, 1, 2), 6 };
        yield return new object[] { new Matrix(2, 0, 0, 3, 1, 2), -45, new Matrix(1.41421, -2.12132, 1.41421, 2.12132, 1, 2), 6 };
        yield return new object[] { new Matrix(2, 0, 0, 3, 1, 2), 45, new Matrix(1.41421, 2.12132, -1.41421, 2.12132, 1, 2), 6 };
        yield return new object[] { new Matrix(2, 0, 0, 3, 1, 2), 0, new Matrix(2, 0, 0, 3, 1, 2), 6 };
        yield return new object[] { new Matrix(2, 0, 0, 3, 1, 2), 90, new Matrix(0, 3, -2, 0, 1, 2), 6 };
        yield return new object[] { new Matrix(2, 0, 0, 3, 1, 2), 30, new Matrix(1.73205, 1.5, -1, 2.59808, 1, 2), 6 };
        yield return new object[] { new Matrix(2, 0, 0, 3, 1, 2), 180, new Matrix(-2, 0, -0, -3, 1, 2), 6 };
        yield return new object[] { new Matrix(2, 0, 0, 3, 1, 2), 270, new Matrix(-0, -3, 2, -0, 1, 2), 6 };
        yield return new object[] { new Matrix(2, 0, 0, 3, 1, 2), 360, new Matrix(2, 0, 0, 3, 1, 2), 6 };
        yield return new object[] { new Matrix(2, 0, 0, 3, 1, 2), 720, new Matrix(2, 0, 0, 3, 1, 2), 6 };
        yield return new object[] { new Matrix(2, 0, 0, 3, 1, 2), double.NaN, new Matrix(double.NaN, double.NaN, double.NaN, double.NaN, double.NaN, double.NaN), double.NaN };

        // Translated and skewed.
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 2, 3), -360, new Matrix(1, 0.5, 0.75, 1, 2, 3), 0.625 };
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 2, 3), -270, new Matrix(0.75, 1, -1, -0.5, 2, 3), 0.625 };
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 2, 3), -180, new Matrix(-1, -0.5, -0.75, -1, 2, 3), 0.625 };
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 2, 3), -90, new Matrix(-0.75, -1, 1, 0.5, 2, 3), 0.625 };
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 2, 3), -45, new Matrix(0.17678, -0.35355, 1.23744, 1.06066, 2, 3), 0.625 };
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 2, 3), 45, new Matrix(1.23744, 1.06066, -0.17678, 0.35355, 2, 3), 0.625 };
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 2, 3), 0, new Matrix(1, 0.5, 0.75, 1, 2, 3), 0.625 };
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 2, 3), 90, new Matrix(0.75, 1, -1, -0.5, 2, 3), 0.625 };
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 2, 3), 30, new Matrix(1.24103, 0.93301, 0.14951, 0.61603, 2, 3), 0.625 };
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 2, 3), 180, new Matrix(-1, -0.5, -0.75, -1, 2, 3), 0.625 };
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 2, 3), 270, new Matrix(-0.75, -1, 1, 0.5, 2, 3), 0.625 };
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 2, 3), 360, new Matrix(1, 0.5, 0.75, 1, 2, 3), 0.625 };
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 2, 3), 720, new Matrix(1, 0.5, 0.75, 1, 2, 3), 0.625 };
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 2, 3), double.NaN, new Matrix(double.NaN, double.NaN, double.NaN, double.NaN, double.NaN, double.NaN), double.NaN };

        // Skewed and scaled.
        yield return new object[] { new Matrix(2, 0.5, 0.75, 3, 0, 0), -360, new Matrix(2, 0.5, 0.75, 3, 0, 0), 5.625 };
        yield return new object[] { new Matrix(2, 0.5, 0.75, 3, 0, 0), -270, new Matrix(0.75, 3, -2, -0.5, 0, 0), 5.625 };
        yield return new object[] { new Matrix(2, 0.5, 0.75, 3, 0, 0), -180, new Matrix(-2, -0.5, -0.75, -3, 0, 0), 5.625 };
        yield return new object[] { new Matrix(2, 0.5, 0.75, 3, 0, 0), -90, new Matrix(-0.75, -3, 2, 0.5, 0, 0), 5.625 };
        yield return new object[] { new Matrix(2, 0.5, 0.75, 3, 0, 0), -45, new Matrix(0.88388, -1.76777, 1.94454, 2.47487, 0, 0), 5.625 };
        yield return new object[] { new Matrix(2, 0.5, 0.75, 3, 0, 0), 45, new Matrix(1.94454, 2.47487, -0.88388, 1.76777, 0, 0), 5.625 };
        yield return new object[] { new Matrix(2, 0.5, 0.75, 3, 0, 0), 0, new Matrix(2, 0.5, 0.75, 3, 0, 0), 5.625 };
        yield return new object[] { new Matrix(2, 0.5, 0.75, 3, 0, 0), 90, new Matrix(0.75, 3, -2, -0.5, 0, 0), 5.625 };
        yield return new object[] { new Matrix(2, 0.5, 0.75, 3, 0, 0), 30, new Matrix(2.10705, 1.93301, -0.35048, 2.34808, 0, 0), 5.625 };
        yield return new object[] { new Matrix(2, 0.5, 0.75, 3, 0, 0), 180, new Matrix(-2, -0.5, -0.75, -3, 0, 0), 5.625 };
        yield return new object[] { new Matrix(2, 0.5, 0.75, 3, 0, 0), 270, new Matrix(-0.75, -3, 2, 0.5, 0, 0), 5.625 };
        yield return new object[] { new Matrix(2, 0.5, 0.75, 3, 0, 0), 360, new Matrix(2, 0.5, 0.75, 3, 0, 0), 5.625 };
        yield return new object[] { new Matrix(2, 0.5, 0.75, 3, 0, 0), 720, new Matrix(2, 0.5, 0.75, 3, 0, 0), 5.625 };
        yield return new object[] { new Matrix(2, 0.5, 0.75, 3, 0, 0), double.NaN, new Matrix(double.NaN, double.NaN, double.NaN, double.NaN, double.NaN, double.NaN), double.NaN };

        // Complex.
        yield return new object[] { new Matrix(2, 3, 4, 5, 6, 7), -360, new Matrix(2, 3, 4, 5, 6, 7), -2 };
        yield return new object[] { new Matrix(2, 3, 4, 5, 6, 7), -270, new Matrix(4, 5, -2, -3, 6, 7), -2 };
        yield return new object[] { new Matrix(2, 3, 4, 5, 6, 7), -180, new Matrix(-2, -3, -4, -5, 6, 7), -2 };
        yield return new object[] { new Matrix(2, 3, 4, 5, 6, 7), -90, new Matrix(-4, -5, 2, 3, 6, 7), -2 };
        yield return new object[] { new Matrix(2, 3, 4, 5, 6, 7), -45, new Matrix(-1.41421, -1.41421, 4.24264, 5.65685, 6, 7), -2 };
        yield return new object[] { new Matrix(2, 3, 4, 5, 6, 7), 45, new Matrix(4.24264, 5.65685, 1.41421, 1.41421, 6, 7), -2 };
        yield return new object[] { new Matrix(2, 3, 4, 5, 6, 7), 0, new Matrix(2, 3, 4, 5, 6, 7), -2 };
        yield return new object[] { new Matrix(2, 3, 4, 5, 6, 7), 90, new Matrix(4, 5, -2, -3, 6, 7), -2 };
        yield return new object[] { new Matrix(2, 3, 4, 5, 6, 7), 30, new Matrix(3.73205, 5.09808, 2.4641, 2.83013, 6, 7), -2 };
        yield return new object[] { new Matrix(2, 3, 4, 5, 6, 7), 180, new Matrix(-2, -3, -4, -5, 6, 7), -2 };
        yield return new object[] { new Matrix(2, 3, 4, 5, 6, 7), 270, new Matrix(-4, -5, 2, 3, 6, 7), -2 };
        yield return new object[] { new Matrix(2, 3, 4, 5, 6, 7), 360, new Matrix(2, 3, 4, 5, 6, 7), -2 };
        yield return new object[] { new Matrix(2, 3, 4, 5, 6, 7), 720, new Matrix(2, 3, 4, 5, 6, 7), -2 };
        yield return new object[] { new Matrix(2, 3, 4, 5, 6, 7), double.NaN, new Matrix(double.NaN, double.NaN, double.NaN, double.NaN, double.NaN, double.NaN), double.NaN };

        // No inverse.
        yield return new object[] { new Matrix(0, 0, 0, 0, 0, 0), -360, new Matrix(0, 0, 0, 0, 0, 0), 0 };
        yield return new object[] { new Matrix(0, 0, 0, 0, 0, 0), -270, new Matrix(0, 0, -0, -0, 0, 0), -0 };
        yield return new object[] { new Matrix(0, 0, 0, 0, 0, 0), -180, new Matrix(-0, -0, 0, 0, 0, 0), -0 };
        yield return new object[] { new Matrix(0, 0, 0, 0, 0, 0), -90, new Matrix(0, 0, 0, 0, 0, 0), 0 };
        yield return new object[] { new Matrix(0, 0, 0, 0, 0, 0), -45, new Matrix(0, 0, 0, 0, 0, 0), 0 };
        yield return new object[] { new Matrix(0, 0, 0, 0, 0, 0), 45, new Matrix(0, 0, 0, 0, 0, 0), 0 };
        yield return new object[] { new Matrix(0, 0, 0, 0, 0, 0), 0, new Matrix(0, 0, 0, 0, 0, 0), 0 };
        yield return new object[] { new Matrix(0, 0, 0, 0, 0, 0), 90, new Matrix(0, 0, 0, 0, 0, 0), 0 };
        yield return new object[] { new Matrix(0, 0, 0, 0, 0, 0), 30, new Matrix(0, 0, 0, 0, 0, 0), 0 };
        yield return new object[] { new Matrix(0, 0, 0, 0, 0, 0), 180, new Matrix(0, 0, -0, -0, 0, 0), -0 };
        yield return new object[] { new Matrix(0, 0, 0, 0, 0, 0), 270, new Matrix(-0, -0, 0, 0, 0, 0), -0 };
        yield return new object[] { new Matrix(0, 0, 0, 0, 0, 0), 360, new Matrix(0, 0, 0, 0, 0, 0), 0 };
        yield return new object[] { new Matrix(0, 0, 0, 0, 0, 0), 720, new Matrix(0, 0, 0, 0, 0, 0), 0 };
        yield return new object[] { new Matrix(0, 0, 0, 0, 0, 0), double.NaN, new Matrix(double.NaN, double.NaN, double.NaN, double.NaN, double.NaN, double.NaN), double.NaN };
    }

    [Theory]
    [MemberData(nameof(RotatePrepend_TestData))]
    public void RotatePrepend_Invoke_Success(Matrix matrix, double angle, Matrix expected, double expectedDeterminant)
    {
        matrix.RotatePrepend(angle);
        Helpers.AssertEqualRounded(expected, matrix);
        Assert.Equal(expected.IsIdentity, matrix.IsIdentity);
        Assert.Equal(expected.HasInverse, matrix.HasInverse);
        Assert.Equal(expectedDeterminant, matrix.Determinant, precision: 5);
    }

    public static IEnumerable<object[]> RotateAt_TestData()
    {
        // Identity
        foreach (Matrix matrix in IdentityMatrices())
        {
            yield return new object[] { matrix, -360, 0, 0, new Matrix(1, -0, 0, 1, 0, 0), 1 };
            yield return new object[] { matrix, -270, 0, 0, new Matrix(-0, 1, -1, -0, 0, 0), 1 };
            yield return new object[] { matrix, -180, 0, 0, new Matrix(-1, -0, 0, -1, 0, 0), 1 };
            yield return new object[] { matrix, -90, 0, 0, new Matrix(0, -1, 1, 0, 0, 0), 1 };
            yield return new object[] { matrix, -45, 0, 0, new Matrix(0.70711, -0.70711, 0.70711, 0.70711, 0, 0), 1 };
            yield return new object[] { matrix, 45, 0, 0, new Matrix(0.70711, 0.70711, -0.70711, 0.70711, 0, 0), 1 };
            yield return new object[] { matrix, 0, 0, 0, new Matrix(1, 0, -0, 1, 0, 0), 1 };
            yield return new object[] { matrix, 90, 0, 0, new Matrix(0, 1, -1, 0, 0, 0), 1 };
            yield return new object[] { matrix, 30, 0, 0, new Matrix(0.86603, 0.5, -0.5, 0.86603, 0, 0), 1 };
            yield return new object[] { matrix, 180, 0, 0, new Matrix(-1, 0, -0, -1, 0, 0), 1 };
            yield return new object[] { matrix, 270, 0, 0, new Matrix(-0, -1, 1, -0, 0, 0), 1 };
            yield return new object[] { matrix, 360, 0, 0, new Matrix(1, 0, -0, 1, 0, 0), 1 };
            yield return new object[] { matrix, 720, 0, 0, new Matrix(1, 0, -0, 1, 0, 0), 1 };
            yield return new object[] { matrix, double.NaN, 0, 0, new Matrix(double.NaN, double.NaN, double.NaN, double.NaN, double.NaN, double.NaN), double.NaN };
            yield return new object[] { matrix, -360, 1, 2, new Matrix(1, -0, 0, 1, 0, 0), 1 };
            yield return new object[] { matrix, -270, 1, 2, new Matrix(-0, 1, -1, -0, 3, 1), 1 };
            yield return new object[] { matrix, -180, 1, 2, new Matrix(-1, -0, 0, -1, 2, 4), 1 };
            yield return new object[] { matrix, -90, 1, 2, new Matrix(0, -1, 1, 0, -1, 3), 1 };
            yield return new object[] { matrix, -45, 1, 2, new Matrix(0.70711, -0.70711, 0.70711, 0.70711, -1.12132, 1.29289), 1 };
            yield return new object[] { matrix, 45, 1, 2, new Matrix(0.70711, 0.70711, -0.70711, 0.70711, 1.70711, -0.12132), 1 };
            yield return new object[] { matrix, 0, 1, 2, new Matrix(1, 0, -0, 1, 0, 0), 1 };
            yield return new object[] { matrix, 90, 1, 2, new Matrix(0, 1, -1, 0, 3, 1), 1 };
            yield return new object[] { matrix, 30, 1, 2, new Matrix(0.86603, 0.5, -0.5, 0.86603, 1.13396, -0.23205), 1 };
            yield return new object[] { matrix, 180, 1, 2, new Matrix(-1, 0, -0, -1, 2, 4), 1 };
            yield return new object[] { matrix, 270, 1, 2, new Matrix(-0, -1, 1, -0, -1, 3), 1 };
            yield return new object[] { matrix, 360, 1, 2, new Matrix(1, 0, -0, 1, 0, 0), 1 };
            yield return new object[] { matrix, 720, 1, 2, new Matrix(1, 0, -0, 1, 0, 0), 1 };
            yield return new object[] { matrix, double.NaN, 1, 2, new Matrix(double.NaN, double.NaN, double.NaN, double.NaN, double.NaN, double.NaN), double.NaN };
            yield return new object[] { matrix, 20, double.NaN, double.NaN, new Matrix(0.93969, 0.34202, -0.34202, 0.93969, double.NaN, double.NaN), 1 };
        }

        // Scaled.
        yield return new object[] { new Matrix(2, 0, 0, 3, 0, 0), -360, 0, 0, new Matrix(2, 0, 0, 3, 0, 0), 6 };
        yield return new object[] { new Matrix(2, 0, 0, 3, 0, 0), -270, 0, 0, new Matrix(-0, 2, -3, -0, 0, 0), 6 };
        yield return new object[] { new Matrix(2, 0, 0, 3, 0, 0), -180, 0, 0, new Matrix(-2, -0, 0, -3, 0, 0), 6 };
        yield return new object[] { new Matrix(2, 0, 0, 3, 0, 0), -90, 0, 0, new Matrix(0, -2, 3, 0, 0, 0), 6 };
        yield return new object[] { new Matrix(2, 0, 0, 3, 0, 0), -45, 0, 0, new Matrix(1.41421, -1.41421, 2.12132, 2.12132, 0, 0), 6 };
        yield return new object[] { new Matrix(2, 0, 0, 3, 0, 0), 45, 0, 0, new Matrix(1.41421, 1.41421, -2.12132, 2.12132, 0, 0), 6 };
        yield return new object[] { new Matrix(2, 0, 0, 3, 0, 0), 0, 0, 0, new Matrix(2, 0, 0, 3, 0, 0), 6 };
        yield return new object[] { new Matrix(2, 0, 0, 3, 0, 0), 90, 0, 0, new Matrix(0, 2, -3, 0, 0, 0), 6 };
        yield return new object[] { new Matrix(2, 0, 0, 3, 0, 0), 30, 0, 0, new Matrix(1.73205, 1, -1.5, 2.59808, 0, 0), 6 };
        yield return new object[] { new Matrix(2, 0, 0, 3, 0, 0), 180, 0, 0, new Matrix(-2, 0, -0, -3, 0, 0), 6 };
        yield return new object[] { new Matrix(2, 0, 0, 3, 0, 0), 270, 0, 0, new Matrix(-0, -2, 3, -0, 0, 0), 6 };
        yield return new object[] { new Matrix(2, 0, 0, 3, 0, 0), 360, 0, 0, new Matrix(2, 0, 0, 3, 0, 0), 6 };
        yield return new object[] { new Matrix(2, 0, 0, 3, 0, 0), 720, 0, 0, new Matrix(2, 0, 0, 3, 0, 0), 6 };
        yield return new object[] { new Matrix(2, 0, 0, 3, 0, 0), double.NaN, 0, 0, new Matrix(double.NaN, double.NaN, double.NaN, double.NaN, double.NaN, double.NaN), double.NaN };
        yield return new object[] { new Matrix(2, 0, 0, 3, 0, 0), -360, 1, 2, new Matrix(2, 0, 0, 3, 0, 0), 6 };
        yield return new object[] { new Matrix(2, 0, 0, 3, 0, 0), -270, 1, 2, new Matrix(-0, 2, -3, -0, 3, 1), 6 };
        yield return new object[] { new Matrix(2, 0, 0, 3, 0, 0), -180, 1, 2, new Matrix(-2, -0, 0, -3, 2, 4), 6 };
        yield return new object[] { new Matrix(2, 0, 0, 3, 0, 0), -90, 1, 2, new Matrix(0, -2, 3, 0, -1, 3), 6 };
        yield return new object[] { new Matrix(2, 0, 0, 3, 0, 0), -45, 1, 2, new Matrix(1.41421, -1.41421, 2.12132, 2.12132, -1.12132, 1.29289), 6 };
        yield return new object[] { new Matrix(2, 0, 0, 3, 0, 0), 45, 1, 2, new Matrix(1.41421, 1.41421, -2.12132, 2.12132, 1.70711, -0.12132), 6 };
        yield return new object[] { new Matrix(2, 0, 0, 3, 0, 0), 0, 1, 2, new Matrix(2, 0, 0, 3, 0, 0), 6 };
        yield return new object[] { new Matrix(2, 0, 0, 3, 0, 0), 90, 1, 2, new Matrix(0, 2, -3, 0, 3, 1), 6 };
        yield return new object[] { new Matrix(2, 0, 0, 3, 0, 0), 30, 1, 2, new Matrix(1.73205, 1, -1.5, 2.59808, 1.13396, -0.23205), 6 };
        yield return new object[] { new Matrix(2, 0, 0, 3, 0, 0), 180, 1, 2, new Matrix(-2, 0, -0, -3, 2, 4), 6 };
        yield return new object[] { new Matrix(2, 0, 0, 3, 0, 0), 270, 1, 2, new Matrix(-0, -2, 3, -0, -1, 3), 6 };
        yield return new object[] { new Matrix(2, 0, 0, 3, 0, 0), 360, 1, 2, new Matrix(2, 0, 0, 3, 0, 0), 6 };
        yield return new object[] { new Matrix(2, 0, 0, 3, 0, 0), 720, 1, 2, new Matrix(2, 0, 0, 3, 0, 0), 6 };
        yield return new object[] { new Matrix(2, 0, 0, 3, 0, 0), double.NaN, 1, 2, new Matrix(double.NaN, double.NaN, double.NaN, double.NaN, double.NaN, double.NaN), double.NaN };
        yield return new object[] { new Matrix(2, 0, 0, 3, 0, 0), 20, double.NaN, double.NaN, new Matrix(1.87939, 0.68404, -1.02606, 2.81908, double.NaN, double.NaN), 6 };

        // Skewed.
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 0, 0), -360, 0, 0, new Matrix(1, 0.5, 0.75, 1, 0, 0), 0.625 };
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 0, 0), -270, 0, 0, new Matrix(-0.5, 1, -1, 0.75, 0, 0), 0.625 };
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 0, 0), -180, 0, 0, new Matrix(-1, -0.5, -0.75, -1, 0, 0), 0.625 };
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 0, 0), -90, 0, 0, new Matrix(0.5, -1, 1, -0.75, 0, 0), 0.625 };
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 0, 0), -45, 0, 0, new Matrix(1.06066, -0.35355, 1.23744, 0.17678, 0, 0), 0.625 };
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 0, 0), 45, 0, 0, new Matrix(0.35355, 1.06066, -0.17678, 1.23744, 0, 0), 0.625 };
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 0, 0), 0, 0, 0, new Matrix(1, 0.5, 0.75, 1, 0, 0), 0.625 };
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 0, 0), 90, 0, 0, new Matrix(-0.5, 1, -1, 0.75, 0, 0), 0.625 };
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 0, 0), 30, 0, 0, new Matrix(0.61603, 0.93301, 0.14951, 1.24103, 0, 0), 0.625 };
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 0, 0), 180, 0, 0, new Matrix(-1, -0.5, -0.75, -1, 0, 0), 0.625 };
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 0, 0), 270, 0, 0, new Matrix(0.5, -1, 1, -0.75, 0, 0), 0.625 };
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 0, 0), 360, 0, 0, new Matrix(1, 0.5, 0.75, 1, 0, 0), 0.625 };
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 0, 0), 720, 0, 0, new Matrix(1, 0.5, 0.75, 1, 0, 0), 0.625 };
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 0, 0), double.NaN, 0, 0, new Matrix(double.NaN, double.NaN, double.NaN, double.NaN, double.NaN, double.NaN), double.NaN };
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 0, 0), -360, 1, 2, new Matrix(1, 0.5, 0.75, 1, 0, 0), 0.625 };
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 0, 0), -270, 1, 2, new Matrix(-0.5, 1, -1, 0.75, 3, 1), 0.625 };
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 0, 0), -180, 1, 2, new Matrix(-1, -0.5, -0.75, -1, 2, 4), 0.625 };
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 0, 0), -90, 1, 2, new Matrix(0.5, -1, 1, -0.75, -1, 3), 0.625 };
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 0, 0), -45, 1, 2, new Matrix(1.06066, -0.35355, 1.23744, 0.17678, -1.12132, 1.29289), 0.625 };
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 0, 0), 45, 1, 2, new Matrix(0.35355, 1.06066, -0.17678, 1.23744, 1.70711, -0.12132), 0.625 };
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 0, 0), 0, 1, 2, new Matrix(1, 0.5, 0.75, 1, 0, 0), 0.625 };
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 0, 0), 90, 1, 2, new Matrix(-0.5, 1, -1, 0.75, 3, 1), 0.625 };
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 0, 0), 30, 1, 2, new Matrix(0.61603, 0.93301, 0.14951, 1.24103, 1.13396, -0.23205), 0.625 };
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 0, 0), 180, 1, 2, new Matrix(-1, -0.5, -0.75, -1, 2, 4), 0.625 };
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 0, 0), 270, 1, 2, new Matrix(0.5, -1, 1, -0.75, -1, 3), 0.625 };
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 0, 0), 360, 1, 2, new Matrix(1, 0.5, 0.75, 1, 0, 0), 0.625 };
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 0, 0), 720, 1, 2, new Matrix(1, 0.5, 0.75, 1, 0, 0), 0.625 };
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 0, 0), double.NaN, 1, 2, new Matrix(double.NaN, double.NaN, double.NaN, double.NaN, double.NaN, double.NaN), double.NaN };
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 0, 0), 20, double.NaN, double.NaN, new Matrix(0.76868, 0.81187, 0.36275, 1.19621, double.NaN, double.NaN), 0.625 };

        // Translated.
        yield return new object[] { new Matrix(1, 0, 0, 1, 2, 3), -360, 0, 0, new Matrix(1, -0, 0, 1, 2, 3), 1 };
        yield return new object[] { new Matrix(1, 0, 0, 1, 2, 3), -270, 0, 0, new Matrix(-0, 1, -1, -0, -3, 2), 1 };
        yield return new object[] { new Matrix(1, 0, 0, 1, 2, 3), -180, 0, 0, new Matrix(-1, -0, 0, -1, -2, -3), 1 };
        yield return new object[] { new Matrix(1, 0, 0, 1, 2, 3), -90, 0, 0, new Matrix(0, -1, 1, 0, 3, -2), 1 };
        yield return new object[] { new Matrix(1, 0, 0, 1, 2, 3), -45, 0, 0, new Matrix(0.70711, -0.70711, 0.70711, 0.70711, 3.53553, 0.70711), 1 };
        yield return new object[] { new Matrix(1, 0, 0, 1, 2, 3), 45, 0, 0, new Matrix(0.70711, 0.70711, -0.70711, 0.70711, -0.70711, 3.53553), 1 };
        yield return new object[] { new Matrix(1, 0, 0, 1, 2, 3), 0, 0, 0, new Matrix(1, 0, -0, 1, 2, 3), 1 };
        yield return new object[] { new Matrix(1, 0, 0, 1, 2, 3), 90, 0, 0, new Matrix(0, 1, -1, 0, -3, 2), 1 };
        yield return new object[] { new Matrix(1, 0, 0, 1, 2, 3), 30, 0, 0, new Matrix(0.86603, 0.5, -0.5, 0.86603, 0.23205, 3.59808), 1 };
        yield return new object[] { new Matrix(1, 0, 0, 1, 2, 3), 180, 0, 0, new Matrix(-1, 0, -0, -1, -2, -3), 1 };
        yield return new object[] { new Matrix(1, 0, 0, 1, 2, 3), 270, 0, 0, new Matrix(-0, -1, 1, -0, 3, -2), 1 };
        yield return new object[] { new Matrix(1, 0, 0, 1, 2, 3), 360, 0, 0, new Matrix(1, 0, -0, 1, 2, 3), 1 };
        yield return new object[] { new Matrix(1, 0, 0, 1, 2, 3), 720, 0, 0, new Matrix(1, 0, -0, 1, 2, 3), 1 };
        yield return new object[] { new Matrix(1, 0, 0, 1, 2, 3), double.NaN, 0, 0, new Matrix(double.NaN, double.NaN, double.NaN, double.NaN, double.NaN, double.NaN), double.NaN };
        yield return new object[] { new Matrix(1, 0, 0, 1, 2, 3), -360, 1, 2, new Matrix(1, -0, 0, 1, 2, 3), 1 };
        yield return new object[] { new Matrix(1, 0, 0, 1, 2, 3), -270, 1, 2, new Matrix(-0, 1, -1, -0, -0, 3), 1 };
        yield return new object[] { new Matrix(1, 0, 0, 1, 2, 3), -180, 1, 2, new Matrix(-1, -0, 0, -1, 0, 1), 1 };
        yield return new object[] { new Matrix(1, 0, 0, 1, 2, 3), -90, 1, 2, new Matrix(0, -1, 1, 0, 2, 1), 1 };
        yield return new object[] { new Matrix(1, 0, 0, 1, 2, 3), -45, 1, 2, new Matrix(0.70711, -0.70711, 0.70711, 0.70711, 2.41421, 2), 1 };
        yield return new object[] { new Matrix(1, 0, 0, 1, 2, 3), 45, 1, 2, new Matrix(0.70711, 0.70711, -0.70711, 0.70711, 1, 3.41421), 1 };
        yield return new object[] { new Matrix(1, 0, 0, 1, 2, 3), 0, 1, 2, new Matrix(1, 0, -0, 1, 2, 3), 1 };
        yield return new object[] { new Matrix(1, 0, 0, 1, 2, 3), 90, 1, 2, new Matrix(0, 1, -1, 0, 0, 3), 1 };
        yield return new object[] { new Matrix(1, 0, 0, 1, 2, 3), 30, 1, 2, new Matrix(0.86603, 0.5, -0.5, 0.86603, 1.36603, 3.36603), 1 };
        yield return new object[] { new Matrix(1, 0, 0, 1, 2, 3), 180, 1, 2, new Matrix(-1, 0, -0, -1, 0, 1), 1 };
        yield return new object[] { new Matrix(1, 0, 0, 1, 2, 3), 270, 1, 2, new Matrix(-0, -1, 1, -0, 2, 1), 1 };
        yield return new object[] { new Matrix(1, 0, 0, 1, 2, 3), 360, 1, 2, new Matrix(1, 0, -0, 1, 2, 3), 1 };
        yield return new object[] { new Matrix(1, 0, 0, 1, 2, 3), 720, 1, 2, new Matrix(1, 0, -0, 1, 2, 3), 1 };
        yield return new object[] { new Matrix(1, 0, 0, 1, 2, 3), double.NaN, 1, 2, new Matrix(double.NaN, double.NaN, double.NaN, double.NaN, double.NaN, double.NaN), double.NaN };
        yield return new object[] { new Matrix(1, 0, 0, 1, 2, 3), 20, double.NaN, double.NaN, new Matrix(0.93969, 0.34202, -0.34202, 0.93969, double.NaN, double.NaN), 1 };

        // Translated and scaled.
        yield return new object[] { new Matrix(2, 0, 0, 3, 1, 2), -360, 0, 0, new Matrix(2, 0, 0, 3, 1, 2), 6 };
        yield return new object[] { new Matrix(2, 0, 0, 3, 1, 2), -270, 0, 0, new Matrix(-0, 2, -3, -0, -2, 1), 6 };
        yield return new object[] { new Matrix(2, 0, 0, 3, 1, 2), -180, 0, 0, new Matrix(-2, -0, 0, -3, -1, -2), 6 };
        yield return new object[] { new Matrix(2, 0, 0, 3, 1, 2), -90, 0, 0, new Matrix(0, -2, 3, 0, 2, -1), 6 };
        yield return new object[] { new Matrix(2, 0, 0, 3, 1, 2), -45, 0, 0, new Matrix(1.41421, -1.41421, 2.12132, 2.12132, 2.12132, 0.70711), 6 };
        yield return new object[] { new Matrix(2, 0, 0, 3, 1, 2), 45, 0, 0, new Matrix(1.41421, 1.41421, -2.12132, 2.12132, -0.70711, 2.12132), 6 };
        yield return new object[] { new Matrix(2, 0, 0, 3, 1, 2), 0, 0, 0, new Matrix(2, 0, 0, 3, 1, 2), 6 };
        yield return new object[] { new Matrix(2, 0, 0, 3, 1, 2), 90, 0, 0, new Matrix(0, 2, -3, 0, -2, 1), 6 };
        yield return new object[] { new Matrix(2, 0, 0, 3, 1, 2), 30, 0, 0, new Matrix(1.73205, 1, -1.5, 2.59808, -0.13397, 2.23205), 6 };
        yield return new object[] { new Matrix(2, 0, 0, 3, 1, 2), 180, 0, 0, new Matrix(-2, 0, -0, -3, -1, -2), 6 };
        yield return new object[] { new Matrix(2, 0, 0, 3, 1, 2), 270, 0, 0, new Matrix(-0, -2, 3, -0, 2, -1), 6 };
        yield return new object[] { new Matrix(2, 0, 0, 3, 1, 2), 360, 0, 0, new Matrix(2, 0, 0, 3, 1, 2), 6 };
        yield return new object[] { new Matrix(2, 0, 0, 3, 1, 2), 720, 0, 0, new Matrix(2, 0, 0, 3, 1, 2), 6 };
        yield return new object[] { new Matrix(2, 0, 0, 3, 1, 2), double.NaN, 0, 0, new Matrix(double.NaN, double.NaN, double.NaN, double.NaN, double.NaN, double.NaN), double.NaN };
        yield return new object[] { new Matrix(2, 0, 0, 3, 1, 2), -360, 1, 2, new Matrix(2, 0, 0, 3, 1, 2), 6 };
        yield return new object[] { new Matrix(2, 0, 0, 3, 1, 2), -270, 1, 2, new Matrix(-0, 2, -3, -0, 1, 2), 6 };
        yield return new object[] { new Matrix(2, 0, 0, 3, 1, 2), -180, 1, 2, new Matrix(-2, -0, 0, -3, 1, 2), 6 };
        yield return new object[] { new Matrix(2, 0, 0, 3, 1, 2), -90, 1, 2, new Matrix(0, -2, 3, 0, 1, 2), 6 };
        yield return new object[] { new Matrix(2, 0, 0, 3, 1, 2), -45, 1, 2, new Matrix(1.41421, -1.41421, 2.12132, 2.12132, 1, 2), 6 };
        yield return new object[] { new Matrix(2, 0, 0, 3, 1, 2), 45, 1, 2, new Matrix(1.41421, 1.41421, -2.12132, 2.12132, 1, 2), 6 };
        yield return new object[] { new Matrix(2, 0, 0, 3, 1, 2), 0, 1, 2, new Matrix(2, 0, 0, 3, 1, 2), 6 };
        yield return new object[] { new Matrix(2, 0, 0, 3, 1, 2), 90, 1, 2, new Matrix(0, 2, -3, 0, 1, 2), 6 };
        yield return new object[] { new Matrix(2, 0, 0, 3, 1, 2), 30, 1, 2, new Matrix(1.73205, 1, -1.5, 2.59808, 1, 2), 6 };
        yield return new object[] { new Matrix(2, 0, 0, 3, 1, 2), 180, 1, 2, new Matrix(-2, 0, -0, -3, 1, 2), 6 };
        yield return new object[] { new Matrix(2, 0, 0, 3, 1, 2), 270, 1, 2, new Matrix(-0, -2, 3, -0, 1, 2), 6 };
        yield return new object[] { new Matrix(2, 0, 0, 3, 1, 2), 360, 1, 2, new Matrix(2, 0, 0, 3, 1, 2), 6 };
        yield return new object[] { new Matrix(2, 0, 0, 3, 1, 2), 720, 1, 2, new Matrix(2, 0, 0, 3, 1, 2), 6 };
        yield return new object[] { new Matrix(2, 0, 0, 3, 1, 2), double.NaN, 1, 2, new Matrix(double.NaN, double.NaN, double.NaN, double.NaN, double.NaN, double.NaN), double.NaN };
        yield return new object[] { new Matrix(2, 0, 0, 3, 1, 2), 20, double.NaN, double.NaN, new Matrix(1.87939, 0.68404, -1.02606, 2.81908, double.NaN, double.NaN), 6 };

        // Translated and skewed.
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 2, 3), -360, 0, 0, new Matrix(1, 0.5, 0.75, 1, 2, 3), 0.625 };
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 2, 3), -270, 0, 0, new Matrix(-0.5, 1, -1, 0.75, -3, 2), 0.625 };
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 2, 3), -180, 0, 0, new Matrix(-1, -0.5, -0.75, -1, -2, -3), 0.625 };
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 2, 3), -90, 0, 0, new Matrix(0.5, -1, 1, -0.75, 3, -2), 0.625 };
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 2, 3), -45, 0, 0, new Matrix(1.06066, -0.35355, 1.23744, 0.17678, 3.53553, 0.70711), 0.625 };
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 2, 3), 45, 0, 0, new Matrix(0.35355, 1.06066, -0.17678, 1.23744, -0.70711, 3.53553), 0.625 };
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 2, 3), 0, 0, 0, new Matrix(1, 0.5, 0.75, 1, 2, 3), 0.625 };
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 2, 3), 90, 0, 0, new Matrix(-0.5, 1, -1, 0.75, -3, 2), 0.625 };
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 2, 3), 30, 0, 0, new Matrix(0.61603, 0.93301, 0.14951, 1.24103, 0.23205, 3.59808), 0.625 };
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 2, 3), 180, 0, 0, new Matrix(-1, -0.5, -0.75, -1, -2, -3), 0.625 };
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 2, 3), 270, 0, 0, new Matrix(0.5, -1, 1, -0.75, 3, -2), 0.625 };
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 2, 3), 360, 0, 0, new Matrix(1, 0.5, 0.75, 1, 2, 3), 0.625 };
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 2, 3), 720, 0, 0, new Matrix(1, 0.5, 0.75, 1, 2, 3), 0.625 };
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 2, 3), double.NaN, 0, 0, new Matrix(double.NaN, double.NaN, double.NaN, double.NaN, double.NaN, double.NaN), double.NaN };
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 2, 3), -360, 1, 2, new Matrix(1, 0.5, 0.75, 1, 2, 3), 0.625 };
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 2, 3), -270, 1, 2, new Matrix(-0.5, 1, -1, 0.75, -0, 3), 0.625 };
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 2, 3), -180, 1, 2, new Matrix(-1, -0.5, -0.75, -1, 0, 1), 0.625 };
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 2, 3), -90, 1, 2, new Matrix(0.5, -1, 1, -0.75, 2, 1), 0.625 };
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 2, 3), -45, 1, 2, new Matrix(1.06066, -0.35355, 1.23744, 0.17678, 2.41421, 2), 0.625 };
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 2, 3), 45, 1, 2, new Matrix(0.35355, 1.06066, -0.17678, 1.23744, 1, 3.41421), 0.625 };
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 2, 3), 0, 1, 2, new Matrix(1, 0.5, 0.75, 1, 2, 3), 0.625 };
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 2, 3), 90, 1, 2, new Matrix(-0.5, 1, -1, 0.75, 0, 3), 0.625 };
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 2, 3), 30, 1, 2, new Matrix(0.61603, 0.93301, 0.14951, 1.24103, 1.36603, 3.36603), 0.625 };
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 2, 3), 180, 1, 2, new Matrix(-1, -0.5, -0.75, -1, 0, 1), 0.625 };
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 2, 3), 270, 1, 2, new Matrix(0.5, -1, 1, -0.75, 2, 1), 0.625 };
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 2, 3), 360, 1, 2, new Matrix(1, 0.5, 0.75, 1, 2, 3), 0.625 };
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 2, 3), 720, 1, 2, new Matrix(1, 0.5, 0.75, 1, 2, 3), 0.625 };
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 2, 3), double.NaN, 1, 2, new Matrix(double.NaN, double.NaN, double.NaN, double.NaN, double.NaN, double.NaN), double.NaN };
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 2, 3), 20, double.NaN, double.NaN, new Matrix(0.76868, 0.81187, 0.36275, 1.19621, double.NaN, double.NaN), 0.625 };

        // Skewed and scaled.
        yield return new object[] { new Matrix(2, 0.5, 0.75, 3, 0, 0), -360, 0, 0, new Matrix(2, 0.5, 0.75, 3, 0, 0), 5.625 };
        yield return new object[] { new Matrix(2, 0.5, 0.75, 3, 0, 0), -270, 0, 0, new Matrix(-0.5, 2, -3, 0.75, 0, 0), 5.625 };
        yield return new object[] { new Matrix(2, 0.5, 0.75, 3, 0, 0), -180, 0, 0, new Matrix(-2, -0.5, -0.75, -3, 0, 0), 5.625 };
        yield return new object[] { new Matrix(2, 0.5, 0.75, 3, 0, 0), -90, 0, 0, new Matrix(0.5, -2, 3, -0.75, 0, 0), 5.625 };
        yield return new object[] { new Matrix(2, 0.5, 0.75, 3, 0, 0), -45, 0, 0, new Matrix(1.76777, -1.06066, 2.65165, 1.59099, 0, 0), 5.625 };
        yield return new object[] { new Matrix(2, 0.5, 0.75, 3, 0, 0), 45, 0, 0, new Matrix(1.06066, 1.76777, -1.59099, 2.65165, 0, 0), 5.625 };
        yield return new object[] { new Matrix(2, 0.5, 0.75, 3, 0, 0), 0, 0, 0, new Matrix(2, 0.5, 0.75, 3, 0, 0), 5.625 };
        yield return new object[] { new Matrix(2, 0.5, 0.75, 3, 0, 0), 90, 0, 0, new Matrix(-0.5, 2, -3, 0.75, 0, 0), 5.625 };
        yield return new object[] { new Matrix(2, 0.5, 0.75, 3, 0, 0), 30, 0, 0, new Matrix(1.48205, 1.43301, -0.85048, 2.97308, 0, 0), 5.625 };
        yield return new object[] { new Matrix(2, 0.5, 0.75, 3, 0, 0), 180, 0, 0, new Matrix(-2, -0.5, -0.75, -3, 0, 0), 5.625 };
        yield return new object[] { new Matrix(2, 0.5, 0.75, 3, 0, 0), 270, 0, 0, new Matrix(0.5, -2, 3, -0.75, 0, 0), 5.625 };
        yield return new object[] { new Matrix(2, 0.5, 0.75, 3, 0, 0), 360, 0, 0, new Matrix(2, 0.5, 0.75, 3, 0, 0), 5.625 };
        yield return new object[] { new Matrix(2, 0.5, 0.75, 3, 0, 0), 720, 0, 0, new Matrix(2, 0.5, 0.75, 3, 0, 0), 5.625 };
        yield return new object[] { new Matrix(2, 0.5, 0.75, 3, 0, 0), double.NaN, 0, 0, new Matrix(double.NaN, double.NaN, double.NaN, double.NaN, double.NaN, double.NaN), double.NaN };
        yield return new object[] { new Matrix(2, 0.5, 0.75, 3, 0, 0), -360, 1, 2, new Matrix(2, 0.5, 0.75, 3, 0, 0), 5.625 };
        yield return new object[] { new Matrix(2, 0.5, 0.75, 3, 0, 0), -270, 1, 2, new Matrix(-0.5, 2, -3, 0.75, 3, 1), 5.625 };
        yield return new object[] { new Matrix(2, 0.5, 0.75, 3, 0, 0), -180, 1, 2, new Matrix(-2, -0.5, -0.75, -3, 2, 4), 5.625 };
        yield return new object[] { new Matrix(2, 0.5, 0.75, 3, 0, 0), -90, 1, 2, new Matrix(0.5, -2, 3, -0.75, -1, 3), 5.625 };
        yield return new object[] { new Matrix(2, 0.5, 0.75, 3, 0, 0), -45, 1, 2, new Matrix(1.76777, -1.06066, 2.65165, 1.59099, -1.12132, 1.29289), 5.625 };
        yield return new object[] { new Matrix(2, 0.5, 0.75, 3, 0, 0), 45, 1, 2, new Matrix(1.06066, 1.76777, -1.59099, 2.65165, 1.70711, -0.12132), 5.625 };
        yield return new object[] { new Matrix(2, 0.5, 0.75, 3, 0, 0), 0, 1, 2, new Matrix(2, 0.5, 0.75, 3, 0, 0), 5.625 };
        yield return new object[] { new Matrix(2, 0.5, 0.75, 3, 0, 0), 90, 1, 2, new Matrix(-0.5, 2, -3, 0.75, 3, 1), 5.625 };
        yield return new object[] { new Matrix(2, 0.5, 0.75, 3, 0, 0), 30, 1, 2, new Matrix(1.48205, 1.43301, -0.85048, 2.97308, 1.13396, -0.23205), 5.625 };
        yield return new object[] { new Matrix(2, 0.5, 0.75, 3, 0, 0), 180, 1, 2, new Matrix(-2, -0.5, -0.75, -3, 2, 4), 5.625 };
        yield return new object[] { new Matrix(2, 0.5, 0.75, 3, 0, 0), 270, 1, 2, new Matrix(0.5, -2, 3, -0.75, -1, 3), 5.625 };
        yield return new object[] { new Matrix(2, 0.5, 0.75, 3, 0, 0), 360, 1, 2, new Matrix(2, 0.5, 0.75, 3, 0, 0), 5.625 };
        yield return new object[] { new Matrix(2, 0.5, 0.75, 3, 0, 0), 720, 1, 2, new Matrix(2, 0.5, 0.75, 3, 0, 0), 5.625 };
        yield return new object[] { new Matrix(2, 0.5, 0.75, 3, 0, 0), double.NaN, 1, 2, new Matrix(double.NaN, double.NaN, double.NaN, double.NaN, double.NaN, double.NaN), double.NaN };
        yield return new object[] { new Matrix(2, 0.5, 0.75, 3, 0, 0), 20, double.NaN, double.NaN, new Matrix(1.70838, 1.15389, -0.32129, 3.07559, double.NaN, double.NaN), 5.625 };

        // Complex.
        yield return new object[] { new Matrix(2, 3, 4, 5, 6, 7), -360, 0, 0, new Matrix(2, 3, 4, 5, 6, 7), -2 };
        yield return new object[] { new Matrix(2, 3, 4, 5, 6, 7), -270, 0, 0, new Matrix(-3, 2, -5, 4, -7, 6), -2 };
        yield return new object[] { new Matrix(2, 3, 4, 5, 6, 7), -180, 0, 0, new Matrix(-2, -3, -4, -5, -6, -7), -2 };
        yield return new object[] { new Matrix(2, 3, 4, 5, 6, 7), -90, 0, 0, new Matrix(3, -2, 5, -4, 7, -6), -2 };
        yield return new object[] { new Matrix(2, 3, 4, 5, 6, 7), -45, 0, 0, new Matrix(3.53553, 0.70711, 6.36396, 0.70711, 9.19239, 0.70711), -2 };
        yield return new object[] { new Matrix(2, 3, 4, 5, 6, 7), 45, 0, 0, new Matrix(-0.70711, 3.53553, -0.70711, 6.36396, -0.70711, 9.19239), -2 };
        yield return new object[] { new Matrix(2, 3, 4, 5, 6, 7), 0, 0, 0, new Matrix(2, 3, 4, 5, 6, 7), -2 };
        yield return new object[] { new Matrix(2, 3, 4, 5, 6, 7), 90, 0, 0, new Matrix(-3, 2, -5, 4, -7, 6), -2 };
        yield return new object[] { new Matrix(2, 3, 4, 5, 6, 7), 30, 0, 0, new Matrix(0.23205, 3.59808, 0.9641, 6.33013, 1.69615, 9.06218), -2 };
        yield return new object[] { new Matrix(2, 3, 4, 5, 6, 7), 180, 0, 0, new Matrix(-2, -3, -4, -5, -6, -7), -2 };
        yield return new object[] { new Matrix(2, 3, 4, 5, 6, 7), 270, 0, 0, new Matrix(3, -2, 5, -4, 7, -6), -2 };
        yield return new object[] { new Matrix(2, 3, 4, 5, 6, 7), 360, 0, 0, new Matrix(2, 3, 4, 5, 6, 7), -2 };
        yield return new object[] { new Matrix(2, 3, 4, 5, 6, 7), 720, 0, 0, new Matrix(2, 3, 4, 5, 6, 7), -2 };
        yield return new object[] { new Matrix(2, 3, 4, 5, 6, 7), double.NaN, 0, 0, new Matrix(double.NaN, double.NaN, double.NaN, double.NaN, double.NaN, double.NaN), double.NaN };
        yield return new object[] { new Matrix(2, 3, 4, 5, 6, 7), -360, 1, 2, new Matrix(2, 3, 4, 5, 6, 7), -2 };
        yield return new object[] { new Matrix(2, 3, 4, 5, 6, 7), -270, 1, 2, new Matrix(-3, 2, -5, 4, -4, 7), -2 };
        yield return new object[] { new Matrix(2, 3, 4, 5, 6, 7), -180, 1, 2, new Matrix(-2, -3, -4, -5, -4, -3), -2 };
        yield return new object[] { new Matrix(2, 3, 4, 5, 6, 7), -90, 1, 2, new Matrix(3, -2, 5, -4, 6, -3), -2 };
        yield return new object[] { new Matrix(2, 3, 4, 5, 6, 7), -45, 1, 2, new Matrix(3.53553, 0.70711, 6.36396, 0.70711, 8.07107, 2), -2 };
        yield return new object[] { new Matrix(2, 3, 4, 5, 6, 7), 45, 1, 2, new Matrix(-0.70711, 3.53553, -0.70711, 6.36396, 1, 9.07107), -2 };
        yield return new object[] { new Matrix(2, 3, 4, 5, 6, 7), 0, 1, 2, new Matrix(2, 3, 4, 5, 6, 7), -2 };
        yield return new object[] { new Matrix(2, 3, 4, 5, 6, 7), 90, 1, 2, new Matrix(-3, 2, -5, 4, -4, 7), -2 };
        yield return new object[] { new Matrix(2, 3, 4, 5, 6, 7), 30, 1, 2, new Matrix(0.23205, 3.59808, 0.9641, 6.33013, 2.83013, 8.83013), -2 };
        yield return new object[] { new Matrix(2, 3, 4, 5, 6, 7), 180, 1, 2, new Matrix(-2, -3, -4, -5, -4, -3), -2 };
        yield return new object[] { new Matrix(2, 3, 4, 5, 6, 7), 270, 1, 2, new Matrix(3, -2, 5, -4, 6, -3), -2 };
        yield return new object[] { new Matrix(2, 3, 4, 5, 6, 7), 360, 1, 2, new Matrix(2, 3, 4, 5, 6, 7), -2 };
        yield return new object[] { new Matrix(2, 3, 4, 5, 6, 7), 720, 1, 2, new Matrix(2, 3, 4, 5, 6, 7), -2 };
        yield return new object[] { new Matrix(2, 3, 4, 5, 6, 7), double.NaN, 1, 2, new Matrix(double.NaN, double.NaN, double.NaN, double.NaN, double.NaN, double.NaN), double.NaN };
        yield return new object[] { new Matrix(2, 3, 4, 5, 6, 7), 20, double.NaN, double.NaN, new Matrix(0.85332, 3.50312, 2.04867, 6.06654, double.NaN, double.NaN), -2 };

        // No inverse.
        yield return new object[] { new Matrix(0, 0, 0, 0, 0, 0), -360, 0, 0, new Matrix(0, 0, 0, 0, 0, 0), 0 };
        yield return new object[] { new Matrix(0, 0, 0, 0, 0, 0), -270, 0, 0, new Matrix(-0, 0, -0, 0, 0, 0), -0 };
        yield return new object[] { new Matrix(0, 0, 0, 0, 0, 0), -180, 0, 0, new Matrix(0, -0, 0, -0, 0, 0), -0 };
        yield return new object[] { new Matrix(0, 0, 0, 0, 0, 0), -90, 0, 0, new Matrix(0, 0, 0, 0, 0, 0), 0 };
        yield return new object[] { new Matrix(0, 0, 0, 0, 0, 0), -45, 0, 0, new Matrix(0, 0, 0, 0, 0, 0), 0 };
        yield return new object[] { new Matrix(0, 0, 0, 0, 0, 0), 45, 0, 0, new Matrix(0, 0, 0, 0, 0, 0), 0 };
        yield return new object[] { new Matrix(0, 0, 0, 0, 0, 0), 0, 0, 0, new Matrix(0, 0, 0, 0, 0, 0), 0 };
        yield return new object[] { new Matrix(0, 0, 0, 0, 0, 0), 90, 0, 0, new Matrix(0, 0, 0, 0, 0, 0), 0 };
        yield return new object[] { new Matrix(0, 0, 0, 0, 0, 0), 30, 0, 0, new Matrix(0, 0, 0, 0, 0, 0), 0 };
        yield return new object[] { new Matrix(0, 0, 0, 0, 0, 0), 180, 0, 0, new Matrix(-0, 0, -0, 0, 0, 0), -0 };
        yield return new object[] { new Matrix(0, 0, 0, 0, 0, 0), 270, 0, 0, new Matrix(0, -0, 0, -0, 0, 0), -0 };
        yield return new object[] { new Matrix(0, 0, 0, 0, 0, 0), 360, 0, 0, new Matrix(0, 0, 0, 0, 0, 0), 0 };
        yield return new object[] { new Matrix(0, 0, 0, 0, 0, 0), 720, 0, 0, new Matrix(0, 0, 0, 0, 0, 0), 0 };
        yield return new object[] { new Matrix(0, 0, 0, 0, 0, 0), double.NaN, 0, 0, new Matrix(double.NaN, double.NaN, double.NaN, double.NaN, double.NaN, double.NaN), double.NaN };
        yield return new object[] { new Matrix(0, 0, 0, 0, 0, 0), -360, 1, 2, new Matrix(0, 0, 0, 0, 0, 0), 0 };
        yield return new object[] { new Matrix(0, 0, 0, 0, 0, 0), -270, 1, 2, new Matrix(-0, 0, -0, 0, 3, 1), -0 };
        yield return new object[] { new Matrix(0, 0, 0, 0, 0, 0), -180, 1, 2, new Matrix(0, -0, 0, -0, 2, 4), -0 };
        yield return new object[] { new Matrix(0, 0, 0, 0, 0, 0), -90, 1, 2, new Matrix(0, 0, 0, 0, -1, 3), 0 };
        yield return new object[] { new Matrix(0, 0, 0, 0, 0, 0), -45, 1, 2, new Matrix(0, 0, 0, 0, -1.12132, 1.29289), 0 };
        yield return new object[] { new Matrix(0, 0, 0, 0, 0, 0), 45, 1, 2, new Matrix(0, 0, 0, 0, 1.70711, -0.12132), 0 };
        yield return new object[] { new Matrix(0, 0, 0, 0, 0, 0), 0, 1, 2, new Matrix(0, 0, 0, 0, 0, 0), 0 };
        yield return new object[] { new Matrix(0, 0, 0, 0, 0, 0), 90, 1, 2, new Matrix(0, 0, 0, 0, 3, 1), 0 };
        yield return new object[] { new Matrix(0, 0, 0, 0, 0, 0), 30, 1, 2, new Matrix(0, 0, 0, 0, 1.13396, -0.23205), 0 };
        yield return new object[] { new Matrix(0, 0, 0, 0, 0, 0), 180, 1, 2, new Matrix(-0, 0, -0, 0, 2, 4), -0 };
        yield return new object[] { new Matrix(0, 0, 0, 0, 0, 0), 270, 1, 2, new Matrix(0, -0, 0, -0, -1, 3), -0 };
        yield return new object[] { new Matrix(0, 0, 0, 0, 0, 0), 360, 1, 2, new Matrix(0, 0, 0, 0, 0, 0), 0 };
        yield return new object[] { new Matrix(0, 0, 0, 0, 0, 0), 720, 1, 2, new Matrix(0, 0, 0, 0, 0, 0), 0 };
        yield return new object[] { new Matrix(0, 0, 0, 0, 0, 0), double.NaN, 1, 2, new Matrix(double.NaN, double.NaN, double.NaN, double.NaN, double.NaN, double.NaN), double.NaN };
        yield return new object[] { new Matrix(0, 0, 0, 0, 0, 0), 20, double.NaN, double.NaN, new Matrix(0, 0, 0, 0, double.NaN, double.NaN), 0 };
    }
    
    [Theory]
    [MemberData(nameof(RotateAt_TestData))]
    public void RotateAt_Invoke_Success(Matrix matrix, double angle, double centerX, double centerY, Matrix expected, double expectedDeterminant)
    {
        matrix.RotateAt(angle, centerX, centerY);
        Helpers.AssertEqualRounded(expected, matrix);
        Assert.Equal(expected.IsIdentity, matrix.IsIdentity);
        Assert.Equal(expected.HasInverse, matrix.HasInverse);
        Assert.Equal(expectedDeterminant, matrix.Determinant, precision: 5);
    }

    public static IEnumerable<object[]> RotateAtPrepend_TestData()
    {
        // Identity
        foreach (Matrix matrix in IdentityMatrices())
        {
            yield return new object[] { matrix, -360, 0, 0, new Matrix(1, -0, 0, 1, 0, 0), 1 };
            yield return new object[] { matrix, -270, 0, 0, new Matrix(-0, 1, -1, -0, 0, 0), 1 };
            yield return new object[] { matrix, -180, 0, 0, new Matrix(-1, -0, 0, -1, 0, 0), 1 };
            yield return new object[] { matrix, -90, 0, 0, new Matrix(0, -1, 1, 0, 0, 0), 1 };
            yield return new object[] { matrix, -45, 0, 0, new Matrix(0.70711, -0.70711, 0.70711, 0.70711, 0, 0), 1 };
            yield return new object[] { matrix, 45, 0, 0, new Matrix(0.70711, 0.70711, -0.70711, 0.70711, 0, 0), 1 };
            yield return new object[] { matrix, 0, 0, 0, new Matrix(1, 0, -0, 1, 0, 0), 1 };
            yield return new object[] { matrix, 90, 0, 0, new Matrix(0, 1, -1, 0, 0, 0), 1 };
            yield return new object[] { matrix, 30, 0, 0, new Matrix(0.86603, 0.5, -0.5, 0.86603, 0, 0), 1 };
            yield return new object[] { matrix, 180, 0, 0, new Matrix(-1, 0, -0, -1, 0, 0), 1 };
            yield return new object[] { matrix, 270, 0, 0, new Matrix(-0, -1, 1, -0, 0, 0), 1 };
            yield return new object[] { matrix, 360, 0, 0, new Matrix(1, 0, -0, 1, 0, 0), 1 };
            yield return new object[] { matrix, 720, 0, 0, new Matrix(1, 0, -0, 1, 0, 0), 1 };
            yield return new object[] { matrix, double.NaN, 0, 0, new Matrix(double.NaN, double.NaN, double.NaN, double.NaN, double.NaN, double.NaN), double.NaN };
            yield return new object[] { matrix, -360, 1, 2, new Matrix(1, -0, 0, 1, 0, 0), 1 };
            yield return new object[] { matrix, -270, 1, 2, new Matrix(-0, 1, -1, -0, 3, 1), 1 };
            yield return new object[] { matrix, -180, 1, 2, new Matrix(-1, -0, 0, -1, 2, 4), 1 };
            yield return new object[] { matrix, -90, 1, 2, new Matrix(0, -1, 1, 0, -1, 3), 1 };
            yield return new object[] { matrix, -45, 1, 2, new Matrix(0.70711, -0.70711, 0.70711, 0.70711, -1.12132, 1.29289), 1 };
            yield return new object[] { matrix, 45, 1, 2, new Matrix(0.70711, 0.70711, -0.70711, 0.70711, 1.70711, -0.12132), 1 };
            yield return new object[] { matrix, 0, 1, 2, new Matrix(1, 0, -0, 1, 0, 0), 1 };
            yield return new object[] { matrix, 90, 1, 2, new Matrix(0, 1, -1, 0, 3, 1), 1 };
            yield return new object[] { matrix, 30, 1, 2, new Matrix(0.86603, 0.5, -0.5, 0.86603, 1.13396, -0.23205), 1 };
            yield return new object[] { matrix, 180, 1, 2, new Matrix(-1, 0, -0, -1, 2, 4), 1 };
            yield return new object[] { matrix, 270, 1, 2, new Matrix(-0, -1, 1, -0, -1, 3), 1 };
            yield return new object[] { matrix, 360, 1, 2, new Matrix(1, 0, -0, 1, 0, 0), 1 };
            yield return new object[] { matrix, 720, 1, 2, new Matrix(1, 0, -0, 1, 0, 0), 1 };
            yield return new object[] { matrix, double.NaN, 1, 2, new Matrix(double.NaN, double.NaN, double.NaN, double.NaN, double.NaN, double.NaN), double.NaN };
            yield return new object[] { matrix, 20, double.NaN, double.NaN, new Matrix(0.93969, 0.34202, -0.34202, 0.93969, double.NaN, double.NaN), 1 };
        }

        // Scaled.
        yield return new object[] { new Matrix(2, 0, 0, 3, 0, 0), -360, 0, 0, new Matrix(2, 0, 0, 3, 0, 0), 6 };
        yield return new object[] { new Matrix(2, 0, 0, 3, 0, 0), -270, 0, 0, new Matrix(-0, 3, -2, -0, 0, 0), 6 };
        yield return new object[] { new Matrix(2, 0, 0, 3, 0, 0), -180, 0, 0, new Matrix(-2, -0, 0, -3, 0, 0), 6 };
        yield return new object[] { new Matrix(2, 0, 0, 3, 0, 0), -90, 0, 0, new Matrix(0, -3, 2, 0, 0, 0), 6 };
        yield return new object[] { new Matrix(2, 0, 0, 3, 0, 0), -45, 0, 0, new Matrix(1.41421, -2.12132, 1.41421, 2.12132, 0, 0), 6 };
        yield return new object[] { new Matrix(2, 0, 0, 3, 0, 0), 45, 0, 0, new Matrix(1.41421, 2.12132, -1.41421, 2.12132, 0, 0), 6 };
        yield return new object[] { new Matrix(2, 0, 0, 3, 0, 0), 0, 0, 0, new Matrix(2, 0, 0, 3, 0, 0), 6 };
        yield return new object[] { new Matrix(2, 0, 0, 3, 0, 0), 90, 0, 0, new Matrix(0, 3, -2, 0, 0, 0), 6 };
        yield return new object[] { new Matrix(2, 0, 0, 3, 0, 0), 30, 0, 0, new Matrix(1.73205, 1.5, -1, 2.59808, 0, 0), 6 };
        yield return new object[] { new Matrix(2, 0, 0, 3, 0, 0), 180, 0, 0, new Matrix(-2, 0, -0, -3, 0, 0), 6 };
        yield return new object[] { new Matrix(2, 0, 0, 3, 0, 0), 270, 0, 0, new Matrix(-0, -3, 2, -0, 0, 0), 6 };
        yield return new object[] { new Matrix(2, 0, 0, 3, 0, 0), 360, 0, 0, new Matrix(2, 0, 0, 3, 0, 0), 6 };
        yield return new object[] { new Matrix(2, 0, 0, 3, 0, 0), 720, 0, 0, new Matrix(2, 0, 0, 3, 0, 0), 6 };
        yield return new object[] { new Matrix(2, 0, 0, 3, 0, 0), double.NaN, 0, 0, new Matrix(double.NaN, double.NaN, double.NaN, double.NaN, double.NaN, double.NaN), double.NaN };
        yield return new object[] { new Matrix(2, 0, 0, 3, 0, 0), -360, 1, 2, new Matrix(2, 0, 0, 3, 0, 0), 6 };
        yield return new object[] { new Matrix(2, 0, 0, 3, 0, 0), -270, 1, 2, new Matrix(-0, 3, -2, -0, 6, 3), 6 };
        yield return new object[] { new Matrix(2, 0, 0, 3, 0, 0), -180, 1, 2, new Matrix(-2, -0, 0, -3, 4, 12), 6 };
        yield return new object[] { new Matrix(2, 0, 0, 3, 0, 0), -90, 1, 2, new Matrix(0, -3, 2, 0, -2, 9), 6 };
        yield return new object[] { new Matrix(2, 0, 0, 3, 0, 0), -45, 1, 2, new Matrix(1.41421, -2.12132, 1.41421, 2.12132, -2.24264, 3.87868), 6 };
        yield return new object[] { new Matrix(2, 0, 0, 3, 0, 0), 45, 1, 2, new Matrix(1.41421, 2.12132, -1.41421, 2.12132, 3.41421, -0.36396), 6 };
        yield return new object[] { new Matrix(2, 0, 0, 3, 0, 0), 0, 1, 2, new Matrix(2, 0, 0, 3, 0, 0), 6 };
        yield return new object[] { new Matrix(2, 0, 0, 3, 0, 0), 90, 1, 2, new Matrix(0, 3, -2, 0, 6, 3), 6 };
        yield return new object[] { new Matrix(2, 0, 0, 3, 0, 0), 30, 1, 2, new Matrix(1.73205, 1.5, -1, 2.59808, 2.26795, -0.69615), 6 };
        yield return new object[] { new Matrix(2, 0, 0, 3, 0, 0), 180, 1, 2, new Matrix(-2, 0, -0, -3, 4, 12), 6 };
        yield return new object[] { new Matrix(2, 0, 0, 3, 0, 0), 270, 1, 2, new Matrix(-0, -3, 2, -0, -2, 9), 6 };
        yield return new object[] { new Matrix(2, 0, 0, 3, 0, 0), 360, 1, 2, new Matrix(2, 0, 0, 3, 0, 0), 6 };
        yield return new object[] { new Matrix(2, 0, 0, 3, 0, 0), 720, 1, 2, new Matrix(2, 0, 0, 3, 0, 0), 6 };
        yield return new object[] { new Matrix(2, 0, 0, 3, 0, 0), double.NaN, 1, 2, new Matrix(double.NaN, double.NaN, double.NaN, double.NaN, double.NaN, double.NaN), double.NaN };
        yield return new object[] { new Matrix(2, 0, 0, 3, 0, 0), 20, double.NaN, double.NaN, new Matrix(1.87939, 1.02606, -0.68404, 2.81908, double.NaN, double.NaN), 6 };

        // Skewed.
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 0, 0), -360, 0, 0, new Matrix(1, 0.5, 0.75, 1, 0, 0), 0.625 };
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 0, 0), -270, 0, 0, new Matrix(0.75, 1, -1, -0.5, 0, 0), 0.625 };
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 0, 0), -180, 0, 0, new Matrix(-1, -0.5, -0.75, -1, 0, 0), 0.625 };
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 0, 0), -90, 0, 0, new Matrix(-0.75, -1, 1, 0.5, 0, 0), 0.625 };
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 0, 0), -45, 0, 0, new Matrix(0.17678, -0.35355, 1.23744, 1.06066, 0, 0), 0.625 };
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 0, 0), 45, 0, 0, new Matrix(1.23744, 1.06066, -0.17678, 0.35355, 0, 0), 0.625 };
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 0, 0), 0, 0, 0, new Matrix(1, 0.5, 0.75, 1, 0, 0), 0.625 };
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 0, 0), 90, 0, 0, new Matrix(0.75, 1, -1, -0.5, 0, 0), 0.625 };
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 0, 0), 30, 0, 0, new Matrix(1.24103, 0.93301, 0.14951, 0.61603, 0, 0), 0.625 };
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 0, 0), 180, 0, 0, new Matrix(-1, -0.5, -0.75, -1, 0, 0), 0.625 };
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 0, 0), 270, 0, 0, new Matrix(-0.75, -1, 1, 0.5, 0, 0), 0.625 };
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 0, 0), 360, 0, 0, new Matrix(1, 0.5, 0.75, 1, 0, 0), 0.625 };
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 0, 0), 720, 0, 0, new Matrix(1, 0.5, 0.75, 1, 0, 0), 0.625 };
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 0, 0), double.NaN, 0, 0, new Matrix(double.NaN, double.NaN, double.NaN, double.NaN, double.NaN, double.NaN), double.NaN };
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 0, 0), -360, 1, 2, new Matrix(1, 0.5, 0.75, 1, 0, 0), 0.625 };
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 0, 0), -270, 1, 2, new Matrix(0.75, 1, -1, -0.5, 3.75, 2.5), 0.625 };
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 0, 0), -180, 1, 2, new Matrix(-1, -0.5, -0.75, -1, 5, 5), 0.625 };
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 0, 0), -90, 1, 2, new Matrix(-0.75, -1, 1, 0.5, 1.25, 2.5), 0.625 };
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 0, 0), -45, 1, 2, new Matrix(0.17678, -0.35355, 1.23744, 1.06066, -0.15165, 0.73223), 0.625 };
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 0, 0), 45, 1, 2, new Matrix(1.23744, 1.06066, -0.17678, 0.35355, 1.61612, 0.73223), 0.625 };
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 0, 0), 0, 1, 2, new Matrix(1, 0.5, 0.75, 1, 0, 0), 0.625 };
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 0, 0), 90, 1, 2, new Matrix(0.75, 1, -1, -0.5, 3.75, 2.5), 0.625 };
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 0, 0), 30, 1, 2, new Matrix(1.24103, 0.93301, 0.14951, 0.61603, 0.95994, 0.33494), 0.625 };
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 0, 0), 180, 1, 2, new Matrix(-1, -0.5, -0.75, -1, 5, 5), 0.625 };
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 0, 0), 270, 1, 2, new Matrix(-0.75, -1, 1, 0.5, 1.25, 2.5), 0.625 };
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 0, 0), 360, 1, 2, new Matrix(1, 0.5, 0.75, 1, 0, 0), 0.625 };
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 0, 0), 720, 1, 2, new Matrix(1, 0.5, 0.75, 1, 0, 0), 0.625 };
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 0, 0), double.NaN, 1, 2, new Matrix(double.NaN, double.NaN, double.NaN, double.NaN, double.NaN, double.NaN), double.NaN };
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 0, 0), 20, double.NaN, double.NaN, new Matrix(1.19621, 0.81187, 0.36275, 0.76868, double.NaN, double.NaN), 0.625 };

        // Translated.
        yield return new object[] { new Matrix(1, 0, 0, 1, 2, 3), -360, 0, 0, new Matrix(1, -0, 0, 1, 2, 3), 1 };
        yield return new object[] { new Matrix(1, 0, 0, 1, 2, 3), -270, 0, 0, new Matrix(-0, 1, -1, -0, 2, 3), 1 };
        yield return new object[] { new Matrix(1, 0, 0, 1, 2, 3), -180, 0, 0, new Matrix(-1, -0, 0, -1, 2, 3), 1 };
        yield return new object[] { new Matrix(1, 0, 0, 1, 2, 3), -90, 0, 0, new Matrix(0, -1, 1, 0, 2, 3), 1 };
        yield return new object[] { new Matrix(1, 0, 0, 1, 2, 3), -45, 0, 0, new Matrix(0.70711, -0.70711, 0.70711, 0.70711, 2, 3), 1 };
        yield return new object[] { new Matrix(1, 0, 0, 1, 2, 3), 45, 0, 0, new Matrix(0.70711, 0.70711, -0.70711, 0.70711, 2, 3), 1 };
        yield return new object[] { new Matrix(1, 0, 0, 1, 2, 3), 0, 0, 0, new Matrix(1, 0, -0, 1, 2, 3), 1 };
        yield return new object[] { new Matrix(1, 0, 0, 1, 2, 3), 90, 0, 0, new Matrix(0, 1, -1, 0, 2, 3), 1 };
        yield return new object[] { new Matrix(1, 0, 0, 1, 2, 3), 30, 0, 0, new Matrix(0.86603, 0.5, -0.5, 0.86603, 2, 3), 1 };
        yield return new object[] { new Matrix(1, 0, 0, 1, 2, 3), 180, 0, 0, new Matrix(-1, 0, -0, -1, 2, 3), 1 };
        yield return new object[] { new Matrix(1, 0, 0, 1, 2, 3), 270, 0, 0, new Matrix(-0, -1, 1, -0, 2, 3), 1 };
        yield return new object[] { new Matrix(1, 0, 0, 1, 2, 3), 360, 0, 0, new Matrix(1, 0, -0, 1, 2, 3), 1 };
        yield return new object[] { new Matrix(1, 0, 0, 1, 2, 3), 720, 0, 0, new Matrix(1, 0, -0, 1, 2, 3), 1 };
        yield return new object[] { new Matrix(1, 0, 0, 1, 2, 3), double.NaN, 0, 0, new Matrix(double.NaN, double.NaN, double.NaN, double.NaN, double.NaN, double.NaN), double.NaN };
        yield return new object[] { new Matrix(1, 0, 0, 1, 2, 3), -360, 1, 2, new Matrix(1, -0, 0, 1, 2, 3), 1 };
        yield return new object[] { new Matrix(1, 0, 0, 1, 2, 3), -270, 1, 2, new Matrix(-0, 1, -1, -0, 5, 4), 1 };
        yield return new object[] { new Matrix(1, 0, 0, 1, 2, 3), -180, 1, 2, new Matrix(-1, -0, 0, -1, 4, 7), 1 };
        yield return new object[] { new Matrix(1, 0, 0, 1, 2, 3), -90, 1, 2, new Matrix(0, -1, 1, 0, 1, 6), 1 };
        yield return new object[] { new Matrix(1, 0, 0, 1, 2, 3), -45, 1, 2, new Matrix(0.70711, -0.70711, 0.70711, 0.70711, 0.87868, 4.29289), 1 };
        yield return new object[] { new Matrix(1, 0, 0, 1, 2, 3), 45, 1, 2, new Matrix(0.70711, 0.70711, -0.70711, 0.70711, 3.70711, 2.87868), 1 };
        yield return new object[] { new Matrix(1, 0, 0, 1, 2, 3), 0, 1, 2, new Matrix(1, 0, -0, 1, 2, 3), 1 };
        yield return new object[] { new Matrix(1, 0, 0, 1, 2, 3), 90, 1, 2, new Matrix(0, 1, -1, 0, 5, 4), 1 };
        yield return new object[] { new Matrix(1, 0, 0, 1, 2, 3), 30, 1, 2, new Matrix(0.86603, 0.5, -0.5, 0.86603, 3.13397, 2.76795), 1 };
        yield return new object[] { new Matrix(1, 0, 0, 1, 2, 3), 180, 1, 2, new Matrix(-1, 0, -0, -1, 4, 7), 1 };
        yield return new object[] { new Matrix(1, 0, 0, 1, 2, 3), 270, 1, 2, new Matrix(-0, -1, 1, -0, 1, 6), 1 };
        yield return new object[] { new Matrix(1, 0, 0, 1, 2, 3), 360, 1, 2, new Matrix(1, 0, -0, 1, 2, 3), 1 };
        yield return new object[] { new Matrix(1, 0, 0, 1, 2, 3), 720, 1, 2, new Matrix(1, 0, -0, 1, 2, 3), 1 };
        yield return new object[] { new Matrix(1, 0, 0, 1, 2, 3), double.NaN, 1, 2, new Matrix(double.NaN, double.NaN, double.NaN, double.NaN, double.NaN, double.NaN), double.NaN };
        yield return new object[] { new Matrix(1, 0, 0, 1, 2, 3), 20, double.NaN, double.NaN, new Matrix(0.93969, 0.34202, -0.34202, 0.93969, double.NaN, double.NaN), 1 };

        // Translated and scaled.
        yield return new object[] { new Matrix(2, 0, 0, 3, 1, 2), -360, 0, 0, new Matrix(2, 0, 0, 3, 1, 2), 6 };
        yield return new object[] { new Matrix(2, 0, 0, 3, 1, 2), -270, 0, 0, new Matrix(-0, 3, -2, -0, 1, 2), 6 };
        yield return new object[] { new Matrix(2, 0, 0, 3, 1, 2), -180, 0, 0, new Matrix(-2, -0, 0, -3, 1, 2), 6 };
        yield return new object[] { new Matrix(2, 0, 0, 3, 1, 2), -90, 0, 0, new Matrix(0, -3, 2, 0, 1, 2), 6 };
        yield return new object[] { new Matrix(2, 0, 0, 3, 1, 2), -45, 0, 0, new Matrix(1.41421, -2.12132, 1.41421, 2.12132, 1, 2), 6 };
        yield return new object[] { new Matrix(2, 0, 0, 3, 1, 2), 45, 0, 0, new Matrix(1.41421, 2.12132, -1.41421, 2.12132, 1, 2), 6 };
        yield return new object[] { new Matrix(2, 0, 0, 3, 1, 2), 0, 0, 0, new Matrix(2, 0, 0, 3, 1, 2), 6 };
        yield return new object[] { new Matrix(2, 0, 0, 3, 1, 2), 90, 0, 0, new Matrix(0, 3, -2, 0, 1, 2), 6 };
        yield return new object[] { new Matrix(2, 0, 0, 3, 1, 2), 30, 0, 0, new Matrix(1.73205, 1.5, -1, 2.59808, 1, 2), 6 };
        yield return new object[] { new Matrix(2, 0, 0, 3, 1, 2), 180, 0, 0, new Matrix(-2, 0, -0, -3, 1, 2), 6 };
        yield return new object[] { new Matrix(2, 0, 0, 3, 1, 2), 270, 0, 0, new Matrix(-0, -3, 2, -0, 1, 2), 6 };
        yield return new object[] { new Matrix(2, 0, 0, 3, 1, 2), 360, 0, 0, new Matrix(2, 0, 0, 3, 1, 2), 6 };
        yield return new object[] { new Matrix(2, 0, 0, 3, 1, 2), 720, 0, 0, new Matrix(2, 0, 0, 3, 1, 2), 6 };
        yield return new object[] { new Matrix(2, 0, 0, 3, 1, 2), double.NaN, 0, 0, new Matrix(double.NaN, double.NaN, double.NaN, double.NaN, double.NaN, double.NaN), double.NaN };
        yield return new object[] { new Matrix(2, 0, 0, 3, 1, 2), -360, 1, 2, new Matrix(2, 0, 0, 3, 1, 2), 6 };
        yield return new object[] { new Matrix(2, 0, 0, 3, 1, 2), -270, 1, 2, new Matrix(-0, 3, -2, -0, 7, 5), 6 };
        yield return new object[] { new Matrix(2, 0, 0, 3, 1, 2), -180, 1, 2, new Matrix(-2, -0, 0, -3, 5, 14), 6 };
        yield return new object[] { new Matrix(2, 0, 0, 3, 1, 2), -90, 1, 2, new Matrix(0, -3, 2, 0, -1, 11), 6 };
        yield return new object[] { new Matrix(2, 0, 0, 3, 1, 2), -45, 1, 2, new Matrix(1.41421, -2.12132, 1.41421, 2.12132, -1.24264, 5.87868), 6 };
        yield return new object[] { new Matrix(2, 0, 0, 3, 1, 2), 45, 1, 2, new Matrix(1.41421, 2.12132, -1.41421, 2.12132, 4.41421, 1.63604), 6 };
        yield return new object[] { new Matrix(2, 0, 0, 3, 1, 2), 0, 1, 2, new Matrix(2, 0, 0, 3, 1, 2), 6 };
        yield return new object[] { new Matrix(2, 0, 0, 3, 1, 2), 90, 1, 2, new Matrix(0, 3, -2, 0, 7, 5), 6 };
        yield return new object[] { new Matrix(2, 0, 0, 3, 1, 2), 30, 1, 2, new Matrix(1.73205, 1.5, -1, 2.59808, 3.26795, 1.30385), 6 };
        yield return new object[] { new Matrix(2, 0, 0, 3, 1, 2), 180, 1, 2, new Matrix(-2, 0, -0, -3, 5, 14), 6 };
        yield return new object[] { new Matrix(2, 0, 0, 3, 1, 2), 270, 1, 2, new Matrix(-0, -3, 2, -0, -1, 11), 6 };
        yield return new object[] { new Matrix(2, 0, 0, 3, 1, 2), 360, 1, 2, new Matrix(2, 0, 0, 3, 1, 2), 6 };
        yield return new object[] { new Matrix(2, 0, 0, 3, 1, 2), 720, 1, 2, new Matrix(2, 0, 0, 3, 1, 2), 6 };
        yield return new object[] { new Matrix(2, 0, 0, 3, 1, 2), double.NaN, 1, 2, new Matrix(double.NaN, double.NaN, double.NaN, double.NaN, double.NaN, double.NaN), double.NaN };
        yield return new object[] { new Matrix(2, 0, 0, 3, 1, 2), 20, double.NaN, double.NaN, new Matrix(1.87939, 1.02606, -0.68404, 2.81908, double.NaN, double.NaN), 6 };

        // Translated and skewed.
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 2, 3), -360, 0, 0, new Matrix(1, 0.5, 0.75, 1, 2, 3), 0.625 };
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 2, 3), -270, 0, 0, new Matrix(0.75, 1, -1, -0.5, 2, 3), 0.625 };
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 2, 3), -180, 0, 0, new Matrix(-1, -0.5, -0.75, -1, 2, 3), 0.625 };
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 2, 3), -90, 0, 0, new Matrix(-0.75, -1, 1, 0.5, 2, 3), 0.625 };
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 2, 3), -45, 0, 0, new Matrix(0.17678, -0.35355, 1.23744, 1.06066, 2, 3), 0.625 };
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 2, 3), 45, 0, 0, new Matrix(1.23744, 1.06066, -0.17678, 0.35355, 2, 3), 0.625 };
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 2, 3), 0, 0, 0, new Matrix(1, 0.5, 0.75, 1, 2, 3), 0.625 };
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 2, 3), 90, 0, 0, new Matrix(0.75, 1, -1, -0.5, 2, 3), 0.625 };
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 2, 3), 30, 0, 0, new Matrix(1.24103, 0.93301, 0.14951, 0.61603, 2, 3), 0.625 };
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 2, 3), 180, 0, 0, new Matrix(-1, -0.5, -0.75, -1, 2, 3), 0.625 };
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 2, 3), 270, 0, 0, new Matrix(-0.75, -1, 1, 0.5, 2, 3), 0.625 };
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 2, 3), 360, 0, 0, new Matrix(1, 0.5, 0.75, 1, 2, 3), 0.625 };
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 2, 3), 720, 0, 0, new Matrix(1, 0.5, 0.75, 1, 2, 3), 0.625 };
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 2, 3), double.NaN, 0, 0, new Matrix(double.NaN, double.NaN, double.NaN, double.NaN, double.NaN, double.NaN), double.NaN };
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 2, 3), -360, 1, 2, new Matrix(1, 0.5, 0.75, 1, 2, 3), 0.625 };
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 2, 3), -270, 1, 2, new Matrix(0.75, 1, -1, -0.5, 5.75, 5.5), 0.625 };
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 2, 3), -180, 1, 2, new Matrix(-1, -0.5, -0.75, -1, 7, 8), 0.625 };
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 2, 3), -90, 1, 2, new Matrix(-0.75, -1, 1, 0.5, 3.25, 5.5), 0.625 };
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 2, 3), -45, 1, 2, new Matrix(0.17678, -0.35355, 1.23744, 1.06066, 1.84835, 3.73223), 0.625 };
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 2, 3), 45, 1, 2, new Matrix(1.23744, 1.06066, -0.17678, 0.35355, 3.61612, 3.73223), 0.625 };
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 2, 3), 0, 1, 2, new Matrix(1, 0.5, 0.75, 1, 2, 3), 0.625 };
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 2, 3), 90, 1, 2, new Matrix(0.75, 1, -1, -0.5, 5.75, 5.5), 0.625 };
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 2, 3), 30, 1, 2, new Matrix(1.24103, 0.93301, 0.14951, 0.61603, 2.95994, 3.33494), 0.625 };
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 2, 3), 180, 1, 2, new Matrix(-1, -0.5, -0.75, -1, 7, 8), 0.625 };
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 2, 3), 270, 1, 2, new Matrix(-0.75, -1, 1, 0.5, 3.25, 5.5), 0.625 };
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 2, 3), 360, 1, 2, new Matrix(1, 0.5, 0.75, 1, 2, 3), 0.625 };
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 2, 3), 720, 1, 2, new Matrix(1, 0.5, 0.75, 1, 2, 3), 0.625 };
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 2, 3), double.NaN, 1, 2, new Matrix(double.NaN, double.NaN, double.NaN, double.NaN, double.NaN, double.NaN), double.NaN };
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 2, 3), 20, double.NaN, double.NaN, new Matrix(1.19621, 0.81187, 0.36275, 0.76868, double.NaN, double.NaN), 0.625 };

        // Skewed and scaled.
        yield return new object[] { new Matrix(2, 0.5, 0.75, 3, 0, 0), -360, 0, 0, new Matrix(2, 0.5, 0.75, 3, 0, 0), 5.625 };
        yield return new object[] { new Matrix(2, 0.5, 0.75, 3, 0, 0), -270, 0, 0, new Matrix(0.75, 3, -2, -0.5, 0, 0), 5.625 };
        yield return new object[] { new Matrix(2, 0.5, 0.75, 3, 0, 0), -180, 0, 0, new Matrix(-2, -0.5, -0.75, -3, 0, 0), 5.625 };
        yield return new object[] { new Matrix(2, 0.5, 0.75, 3, 0, 0), -90, 0, 0, new Matrix(-0.75, -3, 2, 0.5, 0, 0), 5.625 };
        yield return new object[] { new Matrix(2, 0.5, 0.75, 3, 0, 0), -45, 0, 0, new Matrix(0.88388, -1.76777, 1.94454, 2.47487, 0, 0), 5.625 };
        yield return new object[] { new Matrix(2, 0.5, 0.75, 3, 0, 0), 45, 0, 0, new Matrix(1.94454, 2.47487, -0.88388, 1.76777, 0, 0), 5.625 };
        yield return new object[] { new Matrix(2, 0.5, 0.75, 3, 0, 0), 0, 0, 0, new Matrix(2, 0.5, 0.75, 3, 0, 0), 5.625 };
        yield return new object[] { new Matrix(2, 0.5, 0.75, 3, 0, 0), 90, 0, 0, new Matrix(0.75, 3, -2, -0.5, 0, 0), 5.625 };
        yield return new object[] { new Matrix(2, 0.5, 0.75, 3, 0, 0), 30, 0, 0, new Matrix(2.10705, 1.93301, -0.35048, 2.34808, 0, 0), 5.625 };
        yield return new object[] { new Matrix(2, 0.5, 0.75, 3, 0, 0), 180, 0, 0, new Matrix(-2, -0.5, -0.75, -3, 0, 0), 5.625 };
        yield return new object[] { new Matrix(2, 0.5, 0.75, 3, 0, 0), 270, 0, 0, new Matrix(-0.75, -3, 2, 0.5, 0, 0), 5.625 };
        yield return new object[] { new Matrix(2, 0.5, 0.75, 3, 0, 0), 360, 0, 0, new Matrix(2, 0.5, 0.75, 3, 0, 0), 5.625 };
        yield return new object[] { new Matrix(2, 0.5, 0.75, 3, 0, 0), 720, 0, 0, new Matrix(2, 0.5, 0.75, 3, 0, 0), 5.625 };
        yield return new object[] { new Matrix(2, 0.5, 0.75, 3, 0, 0), double.NaN, 0, 0, new Matrix(double.NaN, double.NaN, double.NaN, double.NaN, double.NaN, double.NaN), double.NaN };
        yield return new object[] { new Matrix(2, 0.5, 0.75, 3, 0, 0), -360, 1, 2, new Matrix(2, 0.5, 0.75, 3, 0, 0), 5.625 };
        yield return new object[] { new Matrix(2, 0.5, 0.75, 3, 0, 0), -270, 1, 2, new Matrix(0.75, 3, -2, -0.5, 6.75, 4.5), 5.625 };
        yield return new object[] { new Matrix(2, 0.5, 0.75, 3, 0, 0), -180, 1, 2, new Matrix(-2, -0.5, -0.75, -3, 7, 13), 5.625 };
        yield return new object[] { new Matrix(2, 0.5, 0.75, 3, 0, 0), -90, 1, 2, new Matrix(-0.75, -3, 2, 0.5, 0.25, 8.5), 5.625 };
        yield return new object[] { new Matrix(2, 0.5, 0.75, 3, 0, 0), -45, 1, 2, new Matrix(0.88388, -1.76777, 1.94454, 2.47487, -1.27297, 3.31802), 5.625 };
        yield return new object[] { new Matrix(2, 0.5, 0.75, 3, 0, 0), 45, 1, 2, new Matrix(1.94454, 2.47487, -0.88388, 1.76777, 3.32322, 0.48959), 5.625 };
        yield return new object[] { new Matrix(2, 0.5, 0.75, 3, 0, 0), 0, 1, 2, new Matrix(2, 0.5, 0.75, 3, 0, 0), 5.625 };
        yield return new object[] { new Matrix(2, 0.5, 0.75, 3, 0, 0), 90, 1, 2, new Matrix(0.75, 3, -2, -0.5, 6.75, 4.5), 5.625 };
        yield return new object[] { new Matrix(2, 0.5, 0.75, 3, 0, 0), 30, 1, 2, new Matrix(2.10705, 1.93301, -0.35048, 2.34808, 2.09391, -0.12917), 5.625 };
        yield return new object[] { new Matrix(2, 0.5, 0.75, 3, 0, 0), 180, 1, 2, new Matrix(-2, -0.5, -0.75, -3, 7, 13), 5.625 };
        yield return new object[] { new Matrix(2, 0.5, 0.75, 3, 0, 0), 270, 1, 2, new Matrix(-0.75, -3, 2, 0.5, 0.25, 8.5), 5.625 };
        yield return new object[] { new Matrix(2, 0.5, 0.75, 3, 0, 0), 360, 1, 2, new Matrix(2, 0.5, 0.75, 3, 0, 0), 5.625 };
        yield return new object[] { new Matrix(2, 0.5, 0.75, 3, 0, 0), 720, 1, 2, new Matrix(2, 0.5, 0.75, 3, 0, 0), 5.625 };
        yield return new object[] { new Matrix(2, 0.5, 0.75, 3, 0, 0), double.NaN, 1, 2, new Matrix(double.NaN, double.NaN, double.NaN, double.NaN, double.NaN, double.NaN), double.NaN };
        yield return new object[] { new Matrix(2, 0.5, 0.75, 3, 0, 0), 20, double.NaN, double.NaN, new Matrix(2.1359, 1.49591, 0.02073, 2.64807, double.NaN, double.NaN), 5.625 };

        // Complex.
        yield return new object[] { new Matrix(2, 3, 4, 5, 6, 7), -360, 0, 0, new Matrix(2, 3, 4, 5, 6, 7), -2 };
        yield return new object[] { new Matrix(2, 3, 4, 5, 6, 7), -270, 0, 0, new Matrix(4, 5, -2, -3, 6, 7), -2 };
        yield return new object[] { new Matrix(2, 3, 4, 5, 6, 7), -180, 0, 0, new Matrix(-2, -3, -4, -5, 6, 7), -2 };
        yield return new object[] { new Matrix(2, 3, 4, 5, 6, 7), -90, 0, 0, new Matrix(-4, -5, 2, 3, 6, 7), -2 };
        yield return new object[] { new Matrix(2, 3, 4, 5, 6, 7), -45, 0, 0, new Matrix(-1.41421, -1.41421, 4.24264, 5.65685, 6, 7), -2 };
        yield return new object[] { new Matrix(2, 3, 4, 5, 6, 7), 45, 0, 0, new Matrix(4.24264, 5.65685, 1.41421, 1.41421, 6, 7), -2 };
        yield return new object[] { new Matrix(2, 3, 4, 5, 6, 7), 0, 0, 0, new Matrix(2, 3, 4, 5, 6, 7), -2 };
        yield return new object[] { new Matrix(2, 3, 4, 5, 6, 7), 90, 0, 0, new Matrix(4, 5, -2, -3, 6, 7), -2 };
        yield return new object[] { new Matrix(2, 3, 4, 5, 6, 7), 30, 0, 0, new Matrix(3.73205, 5.09808, 2.4641, 2.83013, 6, 7), -2 };
        yield return new object[] { new Matrix(2, 3, 4, 5, 6, 7), 180, 0, 0, new Matrix(-2, -3, -4, -5, 6, 7), -2 };
        yield return new object[] { new Matrix(2, 3, 4, 5, 6, 7), 270, 0, 0, new Matrix(-4, -5, 2, 3, 6, 7), -2 };
        yield return new object[] { new Matrix(2, 3, 4, 5, 6, 7), 360, 0, 0, new Matrix(2, 3, 4, 5, 6, 7), -2 };
        yield return new object[] { new Matrix(2, 3, 4, 5, 6, 7), 720, 0, 0, new Matrix(2, 3, 4, 5, 6, 7), -2 };
        yield return new object[] { new Matrix(2, 3, 4, 5, 6, 7), double.NaN, 0, 0, new Matrix(double.NaN, double.NaN, double.NaN, double.NaN, double.NaN, double.NaN), double.NaN };
        yield return new object[] { new Matrix(2, 3, 4, 5, 6, 7), -360, 1, 2, new Matrix(2, 3, 4, 5, 6, 7), -2 };
        yield return new object[] { new Matrix(2, 3, 4, 5, 6, 7), -270, 1, 2, new Matrix(4, 5, -2, -3, 16, 21), -2 };
        yield return new object[] { new Matrix(2, 3, 4, 5, 6, 7), -180, 1, 2, new Matrix(-2, -3, -4, -5, 26, 33), -2 };
        yield return new object[] { new Matrix(2, 3, 4, 5, 6, 7), -90, 1, 2, new Matrix(-4, -5, 2, 3, 16, 19), -2 };
        yield return new object[] { new Matrix(2, 3, 4, 5, 6, 7), -45, 1, 2, new Matrix(-1.41421, -1.41421, 4.24264, 5.65685, 8.92892, 10.10051), -2 };
        yield return new object[] { new Matrix(2, 3, 4, 5, 6, 7), 45, 1, 2, new Matrix(4.24264, 5.65685, 1.41421, 1.41421, 8.92892, 11.51472), -2 };
        yield return new object[] { new Matrix(2, 3, 4, 5, 6, 7), 0, 1, 2, new Matrix(2, 3, 4, 5, 6, 7), -2 };
        yield return new object[] { new Matrix(2, 3, 4, 5, 6, 7), 90, 1, 2, new Matrix(4, 5, -2, -3, 16, 21), -2 };
        yield return new object[] { new Matrix(2, 3, 4, 5, 6, 7), 30, 1, 2, new Matrix(3.73205, 5.09808, 2.4641, 2.83013, 7.33975, 9.24166), -2 };
        yield return new object[] { new Matrix(2, 3, 4, 5, 6, 7), 180, 1, 2, new Matrix(-2, -3, -4, -5, 26, 33), -2 };
        yield return new object[] { new Matrix(2, 3, 4, 5, 6, 7), 270, 1, 2, new Matrix(-4, -5, 2, 3, 16, 19), -2 };
        yield return new object[] { new Matrix(2, 3, 4, 5, 6, 7), 360, 1, 2, new Matrix(2, 3, 4, 5, 6, 7), -2 };
        yield return new object[] { new Matrix(2, 3, 4, 5, 6, 7), 720, 1, 2, new Matrix(2, 3, 4, 5, 6, 7), -2 };
        yield return new object[] { new Matrix(2, 3, 4, 5, 6, 7), double.NaN, 1, 2, new Matrix(double.NaN, double.NaN, double.NaN, double.NaN, double.NaN, double.NaN), double.NaN };
        yield return new object[] { new Matrix(2, 3, 4, 5, 6, 7), 20, double.NaN, double.NaN, new Matrix(3.24747, 4.52918, 3.07473, 3.6724, double.NaN, double.NaN), -2 };

        // No inverse.
        yield return new object[] { new Matrix(0, 0, 0, 0, 0, 0), -360, 0, 0, new Matrix(0, 0, 0, 0, 0, 0), 0 };
        yield return new object[] { new Matrix(0, 0, 0, 0, 0, 0), -270, 0, 0, new Matrix(0, 0, -0, -0, 0, 0), -0 };
        yield return new object[] { new Matrix(0, 0, 0, 0, 0, 0), -180, 0, 0, new Matrix(-0, -0, 0, 0, 0, 0), -0 };
        yield return new object[] { new Matrix(0, 0, 0, 0, 0, 0), -90, 0, 0, new Matrix(0, 0, 0, 0, 0, 0), 0 };
        yield return new object[] { new Matrix(0, 0, 0, 0, 0, 0), -45, 0, 0, new Matrix(0, 0, 0, 0, 0, 0), 0 };
        yield return new object[] { new Matrix(0, 0, 0, 0, 0, 0), 45, 0, 0, new Matrix(0, 0, 0, 0, 0, 0), 0 };
        yield return new object[] { new Matrix(0, 0, 0, 0, 0, 0), 0, 0, 0, new Matrix(0, 0, 0, 0, 0, 0), 0 };
        yield return new object[] { new Matrix(0, 0, 0, 0, 0, 0), 90, 0, 0, new Matrix(0, 0, 0, 0, 0, 0), 0 };
        yield return new object[] { new Matrix(0, 0, 0, 0, 0, 0), 30, 0, 0, new Matrix(0, 0, 0, 0, 0, 0), 0 };
        yield return new object[] { new Matrix(0, 0, 0, 0, 0, 0), 180, 0, 0, new Matrix(0, 0, -0, -0, 0, 0), -0 };
        yield return new object[] { new Matrix(0, 0, 0, 0, 0, 0), 270, 0, 0, new Matrix(-0, -0, 0, 0, 0, 0), -0 };
        yield return new object[] { new Matrix(0, 0, 0, 0, 0, 0), 360, 0, 0, new Matrix(0, 0, 0, 0, 0, 0), 0 };
        yield return new object[] { new Matrix(0, 0, 0, 0, 0, 0), 720, 0, 0, new Matrix(0, 0, 0, 0, 0, 0), 0 };
        yield return new object[] { new Matrix(0, 0, 0, 0, 0, 0), double.NaN, 0, 0, new Matrix(double.NaN, double.NaN, double.NaN, double.NaN, double.NaN, double.NaN), double.NaN };
        yield return new object[] { new Matrix(0, 0, 0, 0, 0, 0), -360, 1, 2, new Matrix(0, 0, 0, 0, 0, 0), 0 };
        yield return new object[] { new Matrix(0, 0, 0, 0, 0, 0), -270, 1, 2, new Matrix(0, 0, -0, -0, 0, 0), -0 };
        yield return new object[] { new Matrix(0, 0, 0, 0, 0, 0), -180, 1, 2, new Matrix(-0, -0, 0, 0, 0, 0), -0 };
        yield return new object[] { new Matrix(0, 0, 0, 0, 0, 0), -90, 1, 2, new Matrix(0, 0, 0, 0, 0, 0), 0 };
        yield return new object[] { new Matrix(0, 0, 0, 0, 0, 0), -45, 1, 2, new Matrix(0, 0, 0, 0, 0, 0), 0 };
        yield return new object[] { new Matrix(0, 0, 0, 0, 0, 0), 45, 1, 2, new Matrix(0, 0, 0, 0, 0, 0), 0 };
        yield return new object[] { new Matrix(0, 0, 0, 0, 0, 0), 0, 1, 2, new Matrix(0, 0, 0, 0, 0, 0), 0 };
        yield return new object[] { new Matrix(0, 0, 0, 0, 0, 0), 90, 1, 2, new Matrix(0, 0, 0, 0, 0, 0), 0 };
        yield return new object[] { new Matrix(0, 0, 0, 0, 0, 0), 30, 1, 2, new Matrix(0, 0, 0, 0, 0, 0), 0 };
        yield return new object[] { new Matrix(0, 0, 0, 0, 0, 0), 180, 1, 2, new Matrix(0, 0, -0, -0, 0, 0), -0 };
        yield return new object[] { new Matrix(0, 0, 0, 0, 0, 0), 270, 1, 2, new Matrix(-0, -0, 0, 0, 0, 0), -0 };
        yield return new object[] { new Matrix(0, 0, 0, 0, 0, 0), 360, 1, 2, new Matrix(0, 0, 0, 0, 0, 0), 0 };
        yield return new object[] { new Matrix(0, 0, 0, 0, 0, 0), 720, 1, 2, new Matrix(0, 0, 0, 0, 0, 0), 0 };
        yield return new object[] { new Matrix(0, 0, 0, 0, 0, 0), double.NaN, 1, 2, new Matrix(double.NaN, double.NaN, double.NaN, double.NaN, double.NaN, double.NaN), double.NaN };
        yield return new object[] { new Matrix(0, 0, 0, 0, 0, 0), 20, double.NaN, double.NaN, new Matrix(0, 0, 0, 0, double.NaN, double.NaN), 0 };
    }
    
    [Theory]
    [MemberData(nameof(RotateAt_TestData))]
    public void RotateAtPrepend_Invoke_Success(Matrix matrix, double angle, double centerX, double centerY, Matrix expected, double expectedDeterminant)
    {
        matrix.RotateAtPrepend(angle, centerX, centerY);
        Helpers.AssertEqualRounded(expected, matrix);
        Assert.Equal(expected.IsIdentity, matrix.IsIdentity);
        Assert.Equal(expected.HasInverse, matrix.HasInverse);
        Assert.Equal(expectedDeterminant, matrix.Determinant, precision: 5);
    }


    public static IEnumerable<object[]> Scale_TestData()
    {        
        // Identity
        foreach (Matrix matrix in IdentityMatrices())
        {
            yield return new object[] { matrix, -2, -3, new Matrix(-2, 0, 0, -3, 0, 0) };
            yield return new object[] { matrix, -2, 3, new Matrix(-2, 0, 0, 3, 0, 0) };
            yield return new object[] { matrix, 2, -3, new Matrix(2, 0, 0, -3, 0, 0) };
            yield return new object[] { matrix, -1, -2, new Matrix(-1, 0, 0, -2, 0, 0) };
            yield return new object[] { matrix, -1, 0, new Matrix(-1, 0, 0, 0, 0, 0) };
            yield return new object[] { matrix, 0, -2, new Matrix(0, 0, 0, -2, 0, 0) };
            yield return new object[] { matrix, 0, 0, new Matrix(0, 0, 0, 0, 0, 0) };
            yield return new object[] { matrix, 1, 1, new Matrix(1, 0, 0, 1, 0, 0) };
            yield return new object[] { matrix, 1, 0, new Matrix(1, 0, 0, 0, 0, 0) };
            yield return new object[] { matrix, 0, 2, new Matrix(0, 0, 0, 2, 0, 0) };
            yield return new object[] { matrix, 1, 2, new Matrix(1, 0, 0, 2, 0, 0) };
            yield return new object[] { matrix, 1.1, 2.2, new Matrix(1.1, 0, 0, 2.2, 0, 0) };
            yield return new object[] { matrix, 2, 3, new Matrix(2, 0, 0, 3, 0, 0) };
            yield return new object[] { matrix, double.NaN, 3, new Matrix(double.NaN, 0, 0, 3, 0, 0) };
            yield return new object[] { matrix, 2, double.NaN, new Matrix(2, 0, 0, double.NaN, 0, 0) };
            yield return new object[] { matrix, double.NaN, double.NaN, new Matrix(double.NaN, 0, 0, double.NaN, 0, 0) };
        }

        // Scaled.
        yield return new object[] { new Matrix(2, 0, 0, 3, 0, 0), -2, -3, new Matrix(-4, 0, 0, -9, 0, 0) };
        yield return new object[] { new Matrix(2, 0, 0, 3, 0, 0), -2, 3, new Matrix(-4, 0, 0, 9, 0, 0) };
        yield return new object[] { new Matrix(2, 0, 0, 3, 0, 0), 2, -3, new Matrix(4, 0, 0, -9, 0, 0) };
        yield return new object[] { new Matrix(2, 0, 0, 3, 0, 0), -1, -2, new Matrix(-2, 0, 0, -6, 0, 0) };
        yield return new object[] { new Matrix(2, 0, 0, 3, 0, 0), -1, 0, new Matrix(-2, 0, 0, 0, 0, 0) };
        yield return new object[] { new Matrix(2, 0, 0, 3, 0, 0), 0, -2, new Matrix(0, 0, 0, -6, 0, 0) };
        yield return new object[] { new Matrix(2, 0, 0, 3, 0, 0), 0, 0, new Matrix(0, 0, 0, 0, 0, 0) };
        yield return new object[] { new Matrix(2, 0, 0, 3, 0, 0), 1, 1, new Matrix(2, 0, 0, 3, 0, 0) };
        yield return new object[] { new Matrix(2, 0, 0, 3, 0, 0), 1, 0, new Matrix(2, 0, 0, 0, 0, 0) };
        yield return new object[] { new Matrix(2, 0, 0, 3, 0, 0), 0, 2, new Matrix(0, 0, 0, 6, 0, 0) };
        yield return new object[] { new Matrix(2, 0, 0, 3, 0, 0), 1, 2, new Matrix(2, 0, 0, 6, 0, 0) };
        yield return new object[] { new Matrix(2, 0, 0, 3, 0, 0), 1.1, 2.2, new Matrix(2.2, 0, 0, 6.6, 0, 0) };
        yield return new object[] { new Matrix(2, 0, 0, 3, 0, 0), 2, 3, new Matrix(4, 0, 0, 9, 0, 0) };
        yield return new object[] { new Matrix(2, 0, 0, 3, 0, 0), double.NaN, 3, new Matrix(double.NaN, 0, 0, 9, 0, 0) };
        yield return new object[] { new Matrix(2, 0, 0, 3, 0, 0), 2, double.NaN, new Matrix(4, 0, 0, double.NaN, 0, 0) };
        yield return new object[] { new Matrix(2, 0, 0, 3, 0, 0), double.NaN, double.NaN, new Matrix(double.NaN, 0, 0, double.NaN, 0, 0) };

        // Skewed.
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 0, 0), -2, -3, new Matrix(-2, -1.5, -1.5, -3, 0, 0) };
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 0, 0), -2, 3, new Matrix(-2, 1.5, -1.5, 3, 0, 0) };
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 0, 0), 2, -3, new Matrix(2, -1.5, 1.5, -3, 0, 0) };
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 0, 0), -1, -2, new Matrix(-1, -1, -0.75, -2, 0, 0) };
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 0, 0), -1, 0, new Matrix(-1, 0, -0.75, 0, 0, 0) };
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 0, 0), 0, -2, new Matrix(0, -1, 0, -2, 0, 0) };
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 0, 0), 0, 0, new Matrix(0, 0, 0, 0, 0, 0) };
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 0, 0), 1, 1, new Matrix(1, 0.5, 0.75, 1, 0, 0) };
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 0, 0), 1, 0, new Matrix(1, 0, 0.75, 0, 0, 0) };
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 0, 0), 0, 2, new Matrix(0, 1, 0, 2, 0, 0) };
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 0, 0), 1, 2, new Matrix(1, 1, 0.75, 2, 0, 0) };
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 0, 0), 1.1, 2.2, new Matrix(1.1, 1.1, 0.825, 2.2, 0, 0) };
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 0, 0), 2, 3, new Matrix(2, 1.5, 1.5, 3, 0, 0) };
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 0, 0), double.NaN, 3, new Matrix(double.NaN, 1.5, double.NaN, 3, double.NaN, 0) };
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 0, 0), 2, double.NaN, new Matrix(2, double.NaN, 1.5, double.NaN, 0, double.NaN) };
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 0, 0), double.NaN, double.NaN, new Matrix(double.NaN, double.NaN, double.NaN, double.NaN, double.NaN, double.NaN) };

        // Translated.
        yield return new object[] { new Matrix(1, 0, 0, 1, 2, 3), -2, -3, new Matrix(-2, 0, 0, -3, -4, -9) };
        yield return new object[] { new Matrix(1, 0, 0, 1, 2, 3), -2, 3, new Matrix(-2, 0, 0, 3, -4, 9) };
        yield return new object[] { new Matrix(1, 0, 0, 1, 2, 3), 2, -3, new Matrix(2, 0, 0, -3, 4, -9) };
        yield return new object[] { new Matrix(1, 0, 0, 1, 2, 3), -1, -2, new Matrix(-1, 0, 0, -2, -2, -6) };
        yield return new object[] { new Matrix(1, 0, 0, 1, 2, 3), -1, 0, new Matrix(-1, 0, 0, 0, -2, 0) };
        yield return new object[] { new Matrix(1, 0, 0, 1, 2, 3), 0, -2, new Matrix(0, 0, 0, -2, 0, -6) };
        yield return new object[] { new Matrix(1, 0, 0, 1, 2, 3), 0, 0, new Matrix(0, 0, 0, 0, 0, 0) };
        yield return new object[] { new Matrix(1, 0, 0, 1, 2, 3), 1, 1, new Matrix(1, 0, 0, 1, 2, 3) };
        yield return new object[] { new Matrix(1, 0, 0, 1, 2, 3), 1, 0, new Matrix(1, 0, 0, 0, 2, 0) };
        yield return new object[] { new Matrix(1, 0, 0, 1, 2, 3), 0, 2, new Matrix(0, 0, 0, 2, 0, 6) };
        yield return new object[] { new Matrix(1, 0, 0, 1, 2, 3), 1, 2, new Matrix(1, 0, 0, 2, 2, 6) };
        yield return new object[] { new Matrix(1, 0, 0, 1, 2, 3), 1.1, 2.2, new Matrix(1.1, 0, 0, 2.2, 2.2, 6.6) };
        yield return new object[] { new Matrix(1, 0, 0, 1, 2, 3), 2, 3, new Matrix(2, 0, 0, 3, 4, 9) };
        yield return new object[] { new Matrix(1, 0, 0, 1, 2, 3), double.NaN, 3, new Matrix(double.NaN, 0, 0, 3, double.NaN, 9) };
        yield return new object[] { new Matrix(1, 0, 0, 1, 2, 3), 2, double.NaN, new Matrix(2, 0, 0, double.NaN, 4, double.NaN) };
        yield return new object[] { new Matrix(1, 0, 0, 1, 2, 3), double.NaN, double.NaN, new Matrix(double.NaN, 0, 0, double.NaN, double.NaN, double.NaN) };

        // Translated and scaled.
        yield return new object[] { new Matrix(2, 0, 0, 3, 1, 2), -2, -3, new Matrix(-4, 0, 0, -9, -2, -6) };
        yield return new object[] { new Matrix(2, 0, 0, 3, 1, 2), -2, 3, new Matrix(-4, 0, 0, 9, -2, 6) };
        yield return new object[] { new Matrix(2, 0, 0, 3, 1, 2), 2, -3, new Matrix(4, 0, 0, -9, 2, -6) };
        yield return new object[] { new Matrix(2, 0, 0, 3, 1, 2), -1, -2, new Matrix(-2, 0, 0, -6, -1, -4) };
        yield return new object[] { new Matrix(2, 0, 0, 3, 1, 2), -1, 0, new Matrix(-2, 0, 0, 0, -1, 0) };
        yield return new object[] { new Matrix(2, 0, 0, 3, 1, 2), 0, -2, new Matrix(0, 0, 0, -6, 0, -4) };
        yield return new object[] { new Matrix(2, 0, 0, 3, 1, 2), 0, 0, new Matrix(0, 0, 0, 0, 0, 0) };
        yield return new object[] { new Matrix(2, 0, 0, 3, 1, 2), 1, 1, new Matrix(2, 0, 0, 3, 1, 2) };
        yield return new object[] { new Matrix(2, 0, 0, 3, 1, 2), 1, 0, new Matrix(2, 0, 0, 0, 1, 0) };
        yield return new object[] { new Matrix(2, 0, 0, 3, 1, 2), 0, 2, new Matrix(0, 0, 0, 6, 0, 4) };
        yield return new object[] { new Matrix(2, 0, 0, 3, 1, 2), 1, 2, new Matrix(2, 0, 0, 6, 1, 4) };
        yield return new object[] { new Matrix(2, 0, 0, 3, 1, 2), 1.1, 2.2, new Matrix(2.2, 0, 0, 6.6, 1.1, 4.4) };
        yield return new object[] { new Matrix(2, 0, 0, 3, 1, 2), 2, 3, new Matrix(4, 0, 0, 9, 2, 6) };
        yield return new object[] { new Matrix(2, 0, 0, 3, 1, 2), double.NaN, 3, new Matrix(double.NaN, 0, 0, 9, double.NaN, 6) };
        yield return new object[] { new Matrix(2, 0, 0, 3, 1, 2), 2, double.NaN, new Matrix(4, 0, 0, double.NaN, 2, double.NaN) };
        yield return new object[] { new Matrix(2, 0, 0, 3, 1, 2), double.NaN, double.NaN, new Matrix(double.NaN, 0, 0, double.NaN, double.NaN, double.NaN) };

        // Translated and skewed.
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 2, 3), -2, -3, new Matrix(-2, -1.5, -1.5, -3, -4, -9) };
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 2, 3), -2, 3, new Matrix(-2, 1.5, -1.5, 3, -4, 9) };
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 2, 3), 2, -3, new Matrix(2, -1.5, 1.5, -3, 4, -9) };
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 2, 3), -1, -2, new Matrix(-1, -1, -0.75, -2, -2, -6) };
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 2, 3), -1, 0, new Matrix(-1, 0, -0.75, 0, -2, 0) };
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 2, 3), 0, -2, new Matrix(0, -1, 0, -2, 0, -6) };
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 2, 3), 0, 0, new Matrix(0, 0, 0, 0, 0, 0) };
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 2, 3), 1, 1, new Matrix(1, 0.5, 0.75, 1, 2, 3) };
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 2, 3), 1, 0, new Matrix(1, 0, 0.75, 0, 2, 0) };
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 2, 3), 0, 2, new Matrix(0, 1, 0, 2, 0, 6) };
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 2, 3), 1, 2, new Matrix(1, 1, 0.75, 2, 2, 6) };
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 2, 3), 1.1, 2.2, new Matrix(1.1, 1.1, 0.825, 2.2, 2.2, 6.6) };
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 2, 3), 2, 3, new Matrix(2, 1.5, 1.5, 3, 4, 9) };
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 2, 3), double.NaN, 3, new Matrix(double.NaN, 1.5, double.NaN, 3, double.NaN, 9) };
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 2, 3), 2, double.NaN, new Matrix(2, double.NaN, 1.5, double.NaN, 4, double.NaN) };
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 2, 3), double.NaN, double.NaN, new Matrix(double.NaN, double.NaN, double.NaN, double.NaN, double.NaN, double.NaN) };

        // Skewed and scaled.
        yield return new object[] { new Matrix(2, 0.5, 0.75, 3, 0, 0), -2, -3, new Matrix(-4, -1.5, -1.5, -9, 0, 0) };
        yield return new object[] { new Matrix(2, 0.5, 0.75, 3, 0, 0), -2, 3, new Matrix(-4, 1.5, -1.5, 9, 0, 0) };
        yield return new object[] { new Matrix(2, 0.5, 0.75, 3, 0, 0), 2, -3, new Matrix(4, -1.5, 1.5, -9, 0, 0) };
        yield return new object[] { new Matrix(2, 0.5, 0.75, 3, 0, 0), -1, -2, new Matrix(-2, -1, -0.75, -6, 0, 0) };
        yield return new object[] { new Matrix(2, 0.5, 0.75, 3, 0, 0), -1, 0, new Matrix(-2, 0, -0.75, 0, 0, 0) };
        yield return new object[] { new Matrix(2, 0.5, 0.75, 3, 0, 0), 0, -2, new Matrix(0, -1, 0, -6, 0, 0) };
        yield return new object[] { new Matrix(2, 0.5, 0.75, 3, 0, 0), 0, 0, new Matrix(0, 0, 0, 0, 0, 0) };
        yield return new object[] { new Matrix(2, 0.5, 0.75, 3, 0, 0), 1, 1, new Matrix(2, 0.5, 0.75, 3, 0, 0) };
        yield return new object[] { new Matrix(2, 0.5, 0.75, 3, 0, 0), 1, 0, new Matrix(2, 0, 0.75, 0, 0, 0) };
        yield return new object[] { new Matrix(2, 0.5, 0.75, 3, 0, 0), 0, 2, new Matrix(0, 1, 0, 6, 0, 0) };
        yield return new object[] { new Matrix(2, 0.5, 0.75, 3, 0, 0), 1, 2, new Matrix(2, 1, 0.75, 6, 0, 0) };
        yield return new object[] { new Matrix(2, 0.5, 0.75, 3, 0, 0), 1.1, 2.2, new Matrix(2.2, 1.1, 0.825, 6.6, 0, 0) };
        yield return new object[] { new Matrix(2, 0.5, 0.75, 3, 0, 0), 2, 3, new Matrix(4, 1.5, 1.5, 9, 0, 0) };
        yield return new object[] { new Matrix(2, 0.5, 0.75, 3, 0, 0), double.NaN, 3, new Matrix(double.NaN, 1.5, double.NaN, 9, double.NaN, 0) };
        yield return new object[] { new Matrix(2, 0.5, 0.75, 3, 0, 0), 2, double.NaN, new Matrix(4, double.NaN, 1.5, double.NaN, 0, double.NaN) };
        yield return new object[] { new Matrix(2, 0.5, 0.75, 3, 0, 0), double.NaN, double.NaN, new Matrix(double.NaN, double.NaN, double.NaN, double.NaN, double.NaN, double.NaN) };

        // Complex.
        yield return new object[] { new Matrix(2, 3, 4, 5, 6, 7), -2, -3, new Matrix(-4, -9, -8, -15, -12, -21) };
        yield return new object[] { new Matrix(2, 3, 4, 5, 6, 7), -2, 3, new Matrix(-4, 9, -8, 15, -12, 21) };
        yield return new object[] { new Matrix(2, 3, 4, 5, 6, 7), 2, -3, new Matrix(4, -9, 8, -15, 12, -21) };
        yield return new object[] { new Matrix(2, 3, 4, 5, 6, 7), -1, -2, new Matrix(-2, -6, -4, -10, -6, -14) };
        yield return new object[] { new Matrix(2, 3, 4, 5, 6, 7), -1, 0, new Matrix(-2, 0, -4, 0, -6, 0) };
        yield return new object[] { new Matrix(2, 3, 4, 5, 6, 7), 0, -2, new Matrix(0, -6, 0, -10, 0, -14) };
        yield return new object[] { new Matrix(2, 3, 4, 5, 6, 7), 0, 0, new Matrix(0, 0, 0, 0, 0, 0) };
        yield return new object[] { new Matrix(2, 3, 4, 5, 6, 7), 1, 1, new Matrix(2, 3, 4, 5, 6, 7) };
        yield return new object[] { new Matrix(2, 3, 4, 5, 6, 7), 1, 0, new Matrix(2, 0, 4, 0, 6, 0) };
        yield return new object[] { new Matrix(2, 3, 4, 5, 6, 7), 0, 2, new Matrix(0, 6, 0, 10, 0, 14) };
        yield return new object[] { new Matrix(2, 3, 4, 5, 6, 7), 1, 2, new Matrix(2, 6, 4, 10, 6, 14) };
        yield return new object[] { new Matrix(2, 3, 4, 5, 6, 7), 1.1, 2.2, new Matrix(2.2, 6.6, 4.4, 11, 6.6, 15.4) };
        yield return new object[] { new Matrix(2, 3, 4, 5, 6, 7), 2, 3, new Matrix(4, 9, 8, 15, 12, 21) };
        yield return new object[] { new Matrix(2, 3, 4, 5, 6, 7), double.NaN, 3, new Matrix(double.NaN, 9, double.NaN, 15, double.NaN, 21) };
        yield return new object[] { new Matrix(2, 3, 4, 5, 6, 7), 2, double.NaN, new Matrix(4, double.NaN, 8, double.NaN, 12, double.NaN) };
        yield return new object[] { new Matrix(2, 3, 4, 5, 6, 7), double.NaN, double.NaN, new Matrix(double.NaN, double.NaN, double.NaN, double.NaN, double.NaN, double.NaN) };

        // No inverse.
        yield return new object[] { new Matrix(0, 0, 0, 0, 0, 0), -2, -3, new Matrix(-0, 0, 0, -0, 0, 0) };
        yield return new object[] { new Matrix(0, 0, 0, 0, 0, 0), -2, 3, new Matrix(-0, 0, 0, 0, 0, 0) };
        yield return new object[] { new Matrix(0, 0, 0, 0, 0, 0), 2, -3, new Matrix(0, 0, 0, -0, 0, 0) };
        yield return new object[] { new Matrix(0, 0, 0, 0, 0, 0), -1, -2, new Matrix(-0, 0, 0, -0, 0, 0) };
        yield return new object[] { new Matrix(0, 0, 0, 0, 0, 0), -1, 0, new Matrix(-0, 0, 0, 0, 0, 0) };
        yield return new object[] { new Matrix(0, 0, 0, 0, 0, 0), 0, -2, new Matrix(0, 0, 0, -0, 0, 0) };
        yield return new object[] { new Matrix(0, 0, 0, 0, 0, 0), 0, 0, new Matrix(0, 0, 0, 0, 0, 0) };
        yield return new object[] { new Matrix(0, 0, 0, 0, 0, 0), 1, 1, new Matrix(0, 0, 0, 0, 0, 0) };
        yield return new object[] { new Matrix(0, 0, 0, 0, 0, 0), 1, 0, new Matrix(0, 0, 0, 0, 0, 0) };
        yield return new object[] { new Matrix(0, 0, 0, 0, 0, 0), 0, 2, new Matrix(0, 0, 0, 0, 0, 0) };
        yield return new object[] { new Matrix(0, 0, 0, 0, 0, 0), 1, 2, new Matrix(0, 0, 0, 0, 0, 0) };
        yield return new object[] { new Matrix(0, 0, 0, 0, 0, 0), 1.1, 2.2, new Matrix(0, 0, 0, 0, 0, 0) };
        yield return new object[] { new Matrix(0, 0, 0, 0, 0, 0), 2, 3, new Matrix(0, 0, 0, 0, 0, 0) };
        yield return new object[] { new Matrix(0, 0, 0, 0, 0, 0), double.NaN, 3, new Matrix(double.NaN, 0, 0, 0, 0, 0) };
        yield return new object[] { new Matrix(0, 0, 0, 0, 0, 0), 2, double.NaN, new Matrix(0, 0, 0, double.NaN, 0, 0) };
        yield return new object[] { new Matrix(0, 0, 0, 0, 0, 0), double.NaN, double.NaN, new Matrix(double.NaN, 0, 0, double.NaN, 0, 0) };
    }

    [Theory]
    [MemberData(nameof(Scale_TestData))]
    public void Scale_Invoke_Success(Matrix matrix, double scaleX, double scaleY, Matrix expected)
    {
        matrix.Scale(scaleX, scaleY);
        Helpers.AssertEqualRounded(expected, matrix);
        Assert.Equal(expected.IsIdentity, matrix.IsIdentity);
        Assert.Equal(expected.HasInverse, matrix.HasInverse);
        Assert.Equal(expected.Determinant, matrix.Determinant, precision: 5);
    }

    public static IEnumerable<object[]> ScalePrepend_TestData()
    {
        // Identity
        foreach (Matrix matrix in IdentityMatrices())
        {
            yield return new object[] { matrix, -2, -3, new Matrix(-2, 0, 0, -3, 0, 0) };
            yield return new object[] { matrix, -2, 3, new Matrix(-2, 0, 0, 3, 0, 0) };
            yield return new object[] { matrix, 2, -3, new Matrix(2, 0, 0, -3, 0, 0) };
            yield return new object[] { matrix, -1, -2, new Matrix(-1, 0, 0, -2, 0, 0) };
            yield return new object[] { matrix, -1, 0, new Matrix(-1, 0, 0, 0, 0, 0) };
            yield return new object[] { matrix, 0, -2, new Matrix(0, 0, 0, -2, 0, 0) };
            yield return new object[] { matrix, 0, 0, new Matrix(0, 0, 0, 0, 0, 0) };
            yield return new object[] { matrix, 1, 1, new Matrix(1, 0, 0, 1, 0, 0) };
            yield return new object[] { matrix, 1, 0, new Matrix(1, 0, 0, 0, 0, 0) };
            yield return new object[] { matrix, 0, 2, new Matrix(0, 0, 0, 2, 0, 0) };
            yield return new object[] { matrix, 1, 2, new Matrix(1, 0, 0, 2, 0, 0) };
            yield return new object[] { matrix, 1.1, 2.2, new Matrix(1.1, 0, 0, 2.2, 0, 0) };
            yield return new object[] { matrix, 2, 3, new Matrix(2, 0, 0, 3, 0, 0) };
            yield return new object[] { matrix, double.NaN, 3, new Matrix(double.NaN, 0, 0, 3, 0, 0) };
            yield return new object[] { matrix, 2, double.NaN, new Matrix(2, 0, 0, double.NaN, 0, 0) };
            yield return new object[] { matrix, double.NaN, double.NaN, new Matrix(double.NaN, 0, 0, double.NaN, 0, 0) };
        }

        // Scaled.
        yield return new object[] { new Matrix(2, 0, 0, 3, 0, 0), -2, -3, new Matrix(-4, 0, 0, -9, 0, 0) };
        yield return new object[] { new Matrix(2, 0, 0, 3, 0, 0), -2, 3, new Matrix(-4, 0, 0, 9, 0, 0) };
        yield return new object[] { new Matrix(2, 0, 0, 3, 0, 0), 2, -3, new Matrix(4, 0, 0, -9, 0, 0) };
        yield return new object[] { new Matrix(2, 0, 0, 3, 0, 0), -1, -2, new Matrix(-2, 0, 0, -6, 0, 0) };
        yield return new object[] { new Matrix(2, 0, 0, 3, 0, 0), -1, 0, new Matrix(-2, 0, 0, 0, 0, 0) };
        yield return new object[] { new Matrix(2, 0, 0, 3, 0, 0), 0, -2, new Matrix(0, 0, 0, -6, 0, 0) };
        yield return new object[] { new Matrix(2, 0, 0, 3, 0, 0), 0, 0, new Matrix(0, 0, 0, 0, 0, 0) };
        yield return new object[] { new Matrix(2, 0, 0, 3, 0, 0), 1, 1, new Matrix(2, 0, 0, 3, 0, 0) };
        yield return new object[] { new Matrix(2, 0, 0, 3, 0, 0), 1, 0, new Matrix(2, 0, 0, 0, 0, 0) };
        yield return new object[] { new Matrix(2, 0, 0, 3, 0, 0), 0, 2, new Matrix(0, 0, 0, 6, 0, 0) };
        yield return new object[] { new Matrix(2, 0, 0, 3, 0, 0), 1, 2, new Matrix(2, 0, 0, 6, 0, 0) };
        yield return new object[] { new Matrix(2, 0, 0, 3, 0, 0), 1.1, 2.2, new Matrix(2.2, 0, 0, 6.6, 0, 0) };
        yield return new object[] { new Matrix(2, 0, 0, 3, 0, 0), 2, 3, new Matrix(4, 0, 0, 9, 0, 0) };
        yield return new object[] { new Matrix(2, 0, 0, 3, 0, 0), double.NaN, 3, new Matrix(double.NaN, 0, 0, 9, 0, 0) };
        yield return new object[] { new Matrix(2, 0, 0, 3, 0, 0), 2, double.NaN, new Matrix(4, 0, 0, double.NaN, 0, 0) };
        yield return new object[] { new Matrix(2, 0, 0, 3, 0, 0), double.NaN, double.NaN, new Matrix(double.NaN, 0, 0, double.NaN, 0, 0) };

        // Skewed.
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 0, 0), -2, -3, new Matrix(-2, -1.5, -1.5, -3, 0, 0) };
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 0, 0), -2, 3, new Matrix(-2, 1.5, -1.5, 3, 0, 0) };
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 0, 0), 2, -3, new Matrix(2, -1.5, 1.5, -3, 0, 0) };
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 0, 0), -1, -2, new Matrix(-1, -1, -0.75, -2, 0, 0) };
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 0, 0), -1, 0, new Matrix(-1, 0, -0.75, 0, 0, 0) };
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 0, 0), 0, -2, new Matrix(0, -1, 0, -2, 0, 0) };
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 0, 0), 0, 0, new Matrix(0, 0, 0, 0, 0, 0) };
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 0, 0), 1, 1, new Matrix(1, 0.5, 0.75, 1, 0, 0) };
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 0, 0), 1, 0, new Matrix(1, 0, 0.75, 0, 0, 0) };
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 0, 0), 0, 2, new Matrix(0, 1, 0, 2, 0, 0) };
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 0, 0), 1, 2, new Matrix(1, 1, 0.75, 2, 0, 0) };
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 0, 0), 1.1, 2.2, new Matrix(1.1, 1.1, 0.825, 2.2, 0, 0) };
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 0, 0), 2, 3, new Matrix(2, 1.5, 1.5, 3, 0, 0) };
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 0, 0), double.NaN, 3, new Matrix(double.NaN, 1.5, double.NaN, 3, double.NaN, 0) };
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 0, 0), 2, double.NaN, new Matrix(2, double.NaN, 1.5, double.NaN, 0, double.NaN) };
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 0, 0), double.NaN, double.NaN, new Matrix(double.NaN, double.NaN, double.NaN, double.NaN, double.NaN, double.NaN) };

        // Translated.
        yield return new object[] { new Matrix(1, 0, 0, 1, 2, 3), -2, -3, new Matrix(-2, 0, 0, -3, -4, -9) };
        yield return new object[] { new Matrix(1, 0, 0, 1, 2, 3), -2, 3, new Matrix(-2, 0, 0, 3, -4, 9) };
        yield return new object[] { new Matrix(1, 0, 0, 1, 2, 3), 2, -3, new Matrix(2, 0, 0, -3, 4, -9) };
        yield return new object[] { new Matrix(1, 0, 0, 1, 2, 3), -1, -2, new Matrix(-1, 0, 0, -2, -2, -6) };
        yield return new object[] { new Matrix(1, 0, 0, 1, 2, 3), -1, 0, new Matrix(-1, 0, 0, 0, -2, 0) };
        yield return new object[] { new Matrix(1, 0, 0, 1, 2, 3), 0, -2, new Matrix(0, 0, 0, -2, 0, -6) };
        yield return new object[] { new Matrix(1, 0, 0, 1, 2, 3), 0, 0, new Matrix(0, 0, 0, 0, 0, 0) };
        yield return new object[] { new Matrix(1, 0, 0, 1, 2, 3), 1, 1, new Matrix(1, 0, 0, 1, 2, 3) };
        yield return new object[] { new Matrix(1, 0, 0, 1, 2, 3), 1, 0, new Matrix(1, 0, 0, 0, 2, 0) };
        yield return new object[] { new Matrix(1, 0, 0, 1, 2, 3), 0, 2, new Matrix(0, 0, 0, 2, 0, 6) };
        yield return new object[] { new Matrix(1, 0, 0, 1, 2, 3), 1, 2, new Matrix(1, 0, 0, 2, 2, 6) };
        yield return new object[] { new Matrix(1, 0, 0, 1, 2, 3), 1.1, 2.2, new Matrix(1.1, 0, 0, 2.2, 2.2, 6.6) };
        yield return new object[] { new Matrix(1, 0, 0, 1, 2, 3), 2, 3, new Matrix(2, 0, 0, 3, 4, 9) };
        yield return new object[] { new Matrix(1, 0, 0, 1, 2, 3), double.NaN, 3, new Matrix(double.NaN, 0, 0, 3, double.NaN, 9) };
        yield return new object[] { new Matrix(1, 0, 0, 1, 2, 3), 2, double.NaN, new Matrix(2, 0, 0, double.NaN, 4, double.NaN) };
        yield return new object[] { new Matrix(1, 0, 0, 1, 2, 3), double.NaN, double.NaN, new Matrix(double.NaN, 0, 0, double.NaN, double.NaN, double.NaN) };

        // Translated and scaled.
        yield return new object[] { new Matrix(2, 0, 0, 3, 1, 2), -2, -3, new Matrix(-4, 0, 0, -9, -2, -6) };
        yield return new object[] { new Matrix(2, 0, 0, 3, 1, 2), -2, 3, new Matrix(-4, 0, 0, 9, -2, 6) };
        yield return new object[] { new Matrix(2, 0, 0, 3, 1, 2), 2, -3, new Matrix(4, 0, 0, -9, 2, -6) };
        yield return new object[] { new Matrix(2, 0, 0, 3, 1, 2), -1, -2, new Matrix(-2, 0, 0, -6, -1, -4) };
        yield return new object[] { new Matrix(2, 0, 0, 3, 1, 2), -1, 0, new Matrix(-2, 0, 0, 0, -1, 0) };
        yield return new object[] { new Matrix(2, 0, 0, 3, 1, 2), 0, -2, new Matrix(0, 0, 0, -6, 0, -4) };
        yield return new object[] { new Matrix(2, 0, 0, 3, 1, 2), 0, 0, new Matrix(0, 0, 0, 0, 0, 0) };
        yield return new object[] { new Matrix(2, 0, 0, 3, 1, 2), 1, 1, new Matrix(2, 0, 0, 3, 1, 2) };
        yield return new object[] { new Matrix(2, 0, 0, 3, 1, 2), 1, 0, new Matrix(2, 0, 0, 0, 1, 0) };
        yield return new object[] { new Matrix(2, 0, 0, 3, 1, 2), 0, 2, new Matrix(0, 0, 0, 6, 0, 4) };
        yield return new object[] { new Matrix(2, 0, 0, 3, 1, 2), 1, 2, new Matrix(2, 0, 0, 6, 1, 4) };
        yield return new object[] { new Matrix(2, 0, 0, 3, 1, 2), 1.1, 2.2, new Matrix(2.2, 0, 0, 6.6, 1.1, 4.4) };
        yield return new object[] { new Matrix(2, 0, 0, 3, 1, 2), 2, 3, new Matrix(4, 0, 0, 9, 2, 6) };
        yield return new object[] { new Matrix(2, 0, 0, 3, 1, 2), double.NaN, 3, new Matrix(double.NaN, 0, 0, 9, double.NaN, 6) };
        yield return new object[] { new Matrix(2, 0, 0, 3, 1, 2), 2, double.NaN, new Matrix(4, 0, 0, double.NaN, 2, double.NaN) };
        yield return new object[] { new Matrix(2, 0, 0, 3, 1, 2), double.NaN, double.NaN, new Matrix(double.NaN, 0, 0, double.NaN, double.NaN, double.NaN) };

        // Translated and skewed.
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 2, 3), -2, -3, new Matrix(-2, -1.5, -1.5, -3, -4, -9) };
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 2, 3), -2, 3, new Matrix(-2, 1.5, -1.5, 3, -4, 9) };
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 2, 3), 2, -3, new Matrix(2, -1.5, 1.5, -3, 4, -9) };
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 2, 3), -1, -2, new Matrix(-1, -1, -0.75, -2, -2, -6) };
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 2, 3), -1, 0, new Matrix(-1, 0, -0.75, 0, -2, 0) };
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 2, 3), 0, -2, new Matrix(0, -1, 0, -2, 0, -6) };
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 2, 3), 0, 0, new Matrix(0, 0, 0, 0, 0, 0) };
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 2, 3), 1, 1, new Matrix(1, 0.5, 0.75, 1, 2, 3) };
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 2, 3), 1, 0, new Matrix(1, 0, 0.75, 0, 2, 0) };
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 2, 3), 0, 2, new Matrix(0, 1, 0, 2, 0, 6) };
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 2, 3), 1, 2, new Matrix(1, 1, 0.75, 2, 2, 6) };
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 2, 3), 1.1, 2.2, new Matrix(1.1, 1.1, 0.825, 2.2, 2.2, 6.6) };
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 2, 3), 2, 3, new Matrix(2, 1.5, 1.5, 3, 4, 9) };
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 2, 3), double.NaN, 3, new Matrix(double.NaN, 1.5, double.NaN, 3, double.NaN, 9) };
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 2, 3), 2, double.NaN, new Matrix(2, double.NaN, 1.5, double.NaN, 4, double.NaN) };
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 2, 3), double.NaN, double.NaN, new Matrix(double.NaN, double.NaN, double.NaN, double.NaN, double.NaN, double.NaN) };

        // Skewed and scaled.
        yield return new object[] { new Matrix(2, 0.5, 0.75, 3, 0, 0), -2, -3, new Matrix(-4, -1.5, -1.5, -9, 0, 0) };
        yield return new object[] { new Matrix(2, 0.5, 0.75, 3, 0, 0), -2, 3, new Matrix(-4, 1.5, -1.5, 9, 0, 0) };
        yield return new object[] { new Matrix(2, 0.5, 0.75, 3, 0, 0), 2, -3, new Matrix(4, -1.5, 1.5, -9, 0, 0) };
        yield return new object[] { new Matrix(2, 0.5, 0.75, 3, 0, 0), -1, -2, new Matrix(-2, -1, -0.75, -6, 0, 0) };
        yield return new object[] { new Matrix(2, 0.5, 0.75, 3, 0, 0), -1, 0, new Matrix(-2, 0, -0.75, 0, 0, 0) };
        yield return new object[] { new Matrix(2, 0.5, 0.75, 3, 0, 0), 0, -2, new Matrix(0, -1, 0, -6, 0, 0) };
        yield return new object[] { new Matrix(2, 0.5, 0.75, 3, 0, 0), 0, 0, new Matrix(0, 0, 0, 0, 0, 0) };
        yield return new object[] { new Matrix(2, 0.5, 0.75, 3, 0, 0), 1, 1, new Matrix(2, 0.5, 0.75, 3, 0, 0) };
        yield return new object[] { new Matrix(2, 0.5, 0.75, 3, 0, 0), 1, 0, new Matrix(2, 0, 0.75, 0, 0, 0) };
        yield return new object[] { new Matrix(2, 0.5, 0.75, 3, 0, 0), 0, 2, new Matrix(0, 1, 0, 6, 0, 0) };
        yield return new object[] { new Matrix(2, 0.5, 0.75, 3, 0, 0), 1, 2, new Matrix(2, 1, 0.75, 6, 0, 0) };
        yield return new object[] { new Matrix(2, 0.5, 0.75, 3, 0, 0), 1.1, 2.2, new Matrix(2.2, 1.1, 0.825, 6.6, 0, 0) };
        yield return new object[] { new Matrix(2, 0.5, 0.75, 3, 0, 0), 2, 3, new Matrix(4, 1.5, 1.5, 9, 0, 0) };
        yield return new object[] { new Matrix(2, 0.5, 0.75, 3, 0, 0), double.NaN, 3, new Matrix(double.NaN, 1.5, double.NaN, 9, double.NaN, 0) };
        yield return new object[] { new Matrix(2, 0.5, 0.75, 3, 0, 0), 2, double.NaN, new Matrix(4, double.NaN, 1.5, double.NaN, 0, double.NaN) };
        yield return new object[] { new Matrix(2, 0.5, 0.75, 3, 0, 0), double.NaN, double.NaN, new Matrix(double.NaN, double.NaN, double.NaN, double.NaN, double.NaN, double.NaN) };

        // Complex.
        yield return new object[] { new Matrix(2, 3, 4, 5, 6, 7), -2, -3, new Matrix(-4, -9, -8, -15, -12, -21) };
        yield return new object[] { new Matrix(2, 3, 4, 5, 6, 7), -2, 3, new Matrix(-4, 9, -8, 15, -12, 21) };
        yield return new object[] { new Matrix(2, 3, 4, 5, 6, 7), 2, -3, new Matrix(4, -9, 8, -15, 12, -21) };
        yield return new object[] { new Matrix(2, 3, 4, 5, 6, 7), -1, -2, new Matrix(-2, -6, -4, -10, -6, -14) };
        yield return new object[] { new Matrix(2, 3, 4, 5, 6, 7), -1, 0, new Matrix(-2, 0, -4, 0, -6, 0) };
        yield return new object[] { new Matrix(2, 3, 4, 5, 6, 7), 0, -2, new Matrix(0, -6, 0, -10, 0, -14) };
        yield return new object[] { new Matrix(2, 3, 4, 5, 6, 7), 0, 0, new Matrix(0, 0, 0, 0, 0, 0) };
        yield return new object[] { new Matrix(2, 3, 4, 5, 6, 7), 1, 1, new Matrix(2, 3, 4, 5, 6, 7) };
        yield return new object[] { new Matrix(2, 3, 4, 5, 6, 7), 1, 0, new Matrix(2, 0, 4, 0, 6, 0) };
        yield return new object[] { new Matrix(2, 3, 4, 5, 6, 7), 0, 2, new Matrix(0, 6, 0, 10, 0, 14) };
        yield return new object[] { new Matrix(2, 3, 4, 5, 6, 7), 1, 2, new Matrix(2, 6, 4, 10, 6, 14) };
        yield return new object[] { new Matrix(2, 3, 4, 5, 6, 7), 1.1, 2.2, new Matrix(2.2, 6.6, 4.4, 11, 6.6, 15.4) };
        yield return new object[] { new Matrix(2, 3, 4, 5, 6, 7), 2, 3, new Matrix(4, 9, 8, 15, 12, 21) };
        yield return new object[] { new Matrix(2, 3, 4, 5, 6, 7), double.NaN, 3, new Matrix(double.NaN, 9, double.NaN, 15, double.NaN, 21) };
        yield return new object[] { new Matrix(2, 3, 4, 5, 6, 7), 2, double.NaN, new Matrix(4, double.NaN, 8, double.NaN, 12, double.NaN) };
        yield return new object[] { new Matrix(2, 3, 4, 5, 6, 7), double.NaN, double.NaN, new Matrix(double.NaN, double.NaN, double.NaN, double.NaN, double.NaN, double.NaN) };

        // No inverse.
        yield return new object[] { new Matrix(0, 0, 0, 0, 0, 0), -2, -3, new Matrix(-0, 0, 0, -0, 0, 0) };
        yield return new object[] { new Matrix(0, 0, 0, 0, 0, 0), -2, 3, new Matrix(-0, 0, 0, 0, 0, 0) };
        yield return new object[] { new Matrix(0, 0, 0, 0, 0, 0), 2, -3, new Matrix(0, 0, 0, -0, 0, 0) };
        yield return new object[] { new Matrix(0, 0, 0, 0, 0, 0), -1, -2, new Matrix(-0, 0, 0, -0, 0, 0) };
        yield return new object[] { new Matrix(0, 0, 0, 0, 0, 0), -1, 0, new Matrix(-0, 0, 0, 0, 0, 0) };
        yield return new object[] { new Matrix(0, 0, 0, 0, 0, 0), 0, -2, new Matrix(0, 0, 0, -0, 0, 0) };
        yield return new object[] { new Matrix(0, 0, 0, 0, 0, 0), 0, 0, new Matrix(0, 0, 0, 0, 0, 0) };
        yield return new object[] { new Matrix(0, 0, 0, 0, 0, 0), 1, 1, new Matrix(0, 0, 0, 0, 0, 0) };
        yield return new object[] { new Matrix(0, 0, 0, 0, 0, 0), 1, 0, new Matrix(0, 0, 0, 0, 0, 0) };
        yield return new object[] { new Matrix(0, 0, 0, 0, 0, 0), 0, 2, new Matrix(0, 0, 0, 0, 0, 0) };
        yield return new object[] { new Matrix(0, 0, 0, 0, 0, 0), 1, 2, new Matrix(0, 0, 0, 0, 0, 0) };
        yield return new object[] { new Matrix(0, 0, 0, 0, 0, 0), 1.1, 2.2, new Matrix(0, 0, 0, 0, 0, 0) };
        yield return new object[] { new Matrix(0, 0, 0, 0, 0, 0), 2, 3, new Matrix(0, 0, 0, 0, 0, 0) };
        yield return new object[] { new Matrix(0, 0, 0, 0, 0, 0), double.NaN, 3, new Matrix(double.NaN, 0, 0, 0, 0, 0) };
        yield return new object[] { new Matrix(0, 0, 0, 0, 0, 0), 2, double.NaN, new Matrix(0, 0, 0, double.NaN, 0, 0) };
        yield return new object[] { new Matrix(0, 0, 0, 0, 0, 0), double.NaN, double.NaN, new Matrix(double.NaN, 0, 0, double.NaN, 0, 0) };
    }

    [Theory]
    [MemberData(nameof(ScalePrepend_TestData))]
    public void ScalePrepend_Invoke_Success(Matrix matrix, double scaleX, double scaleY, Matrix expected)
    {
        matrix.Scale(scaleX, scaleY);
        Helpers.AssertEqualRounded(expected, matrix);
        Assert.Equal(expected.IsIdentity, matrix.IsIdentity);
        Assert.Equal(expected.HasInverse, matrix.HasInverse);
        Assert.Equal(expected.Determinant, matrix.Determinant, precision: 5);
    }

    public static IEnumerable<object[]> ScaleAt_TestData()
    {
        // Identity
        foreach (Matrix matrix in IdentityMatrices())
        {
            yield return new object[] { matrix, -2, -3, 0, 0, new Matrix(-2, 0, 0, -3, 0, 0) };
            yield return new object[] { matrix, -2, 3, 0, 0, new Matrix(-2, 0, 0, 3, 0, 0) };
            yield return new object[] { matrix, 2, -3, 0, 0, new Matrix(2, 0, 0, -3, 0, 0) };
            yield return new object[] { matrix, -1, -2, 0, 0, new Matrix(-1, 0, 0, -2, 0, 0) };
            yield return new object[] { matrix, -1, 0, 0, 0, new Matrix(-1, 0, 0, 0, 0, 0) };
            yield return new object[] { matrix, 0, -2, 0, 0, new Matrix(0, 0, 0, -2, 0, 0) };
            yield return new object[] { matrix, 0, 0, 0, 0, new Matrix(0, 0, 0, 0, 0, 0) };
            yield return new object[] { matrix, 1, 1, 0, 0, new Matrix(1, 0, 0, 1, 0, 0) };
            yield return new object[] { matrix, 1, 0, 0, 0, new Matrix(1, 0, 0, 0, 0, 0) };
            yield return new object[] { matrix, 0, 2, 0, 0, new Matrix(0, 0, 0, 2, 0, 0) };
            yield return new object[] { matrix, 1, 2, 0, 0, new Matrix(1, 0, 0, 2, 0, 0) };
            yield return new object[] { matrix, 1.1, 2.2, 0, 0, new Matrix(1.1, 0, 0, 2.2, 0, 0) };
            yield return new object[] { matrix, 2, 3, 0, 0, new Matrix(2, 0, 0, 3, 0, 0) };
            yield return new object[] { matrix, double.NaN, 3, 0, 0, new Matrix(double.NaN, 0, 0, 3, double.NaN, 0) };
            yield return new object[] { matrix, 2, double.NaN, 0, 0, new Matrix(2, 0, 0, double.NaN, 0, double.NaN) };
            yield return new object[] { matrix, double.NaN, double.NaN, 0, 0, new Matrix(double.NaN, 0, 0, double.NaN, double.NaN, double.NaN) };
            yield return new object[] { matrix, -2, -3, 1, 2, new Matrix(-2, 0, 0, -3, 3, 8) };
            yield return new object[] { matrix, -2, 3, 1, 2, new Matrix(-2, 0, 0, 3, 3, -4) };
            yield return new object[] { matrix, 2, -3, 1, 2, new Matrix(2, 0, 0, -3, -1, 8) };
            yield return new object[] { matrix, -1, -2, 1, 2, new Matrix(-1, 0, 0, -2, 2, 6) };
            yield return new object[] { matrix, -1, 0, 1, 2, new Matrix(-1, 0, 0, 0, 2, 2) };
            yield return new object[] { matrix, 0, -2, 1, 2, new Matrix(0, 0, 0, -2, 1, 6) };
            yield return new object[] { matrix, 0, 0, 1, 2, new Matrix(0, 0, 0, 0, 1, 2) };
            yield return new object[] { matrix, 1, 1, 1, 2, new Matrix(1, 0, 0, 1, 0, 0) };
            yield return new object[] { matrix, 1, 0, 1, 2, new Matrix(1, 0, 0, 0, 0, 2) };
            yield return new object[] { matrix, 0, 2, 1, 2, new Matrix(0, 0, 0, 2, 1, -2) };
            yield return new object[] { matrix, 1, 2, 1, 2, new Matrix(1, 0, 0, 2, 0, -2) };
            yield return new object[] { matrix, 1.1, 2.2, 1, 2, new Matrix(1.1, 0, 0, 2.2, -0.1, -2.4) };
            yield return new object[] { matrix, 2, 3, 1, 2, new Matrix(2, 0, 0, 3, -1, -4) };
            yield return new object[] { matrix, double.NaN, 3, 1, 2, new Matrix(double.NaN, 0, 0, 3, double.NaN, -4) };
            yield return new object[] { matrix, 2, double.NaN, 1, 2, new Matrix(2, 0, 0, double.NaN, -1, double.NaN) };
            yield return new object[] { matrix, double.NaN, double.NaN, 1, 2, new Matrix(double.NaN, 0, 0, double.NaN, double.NaN, double.NaN) };
            yield return new object[] { matrix, 2, 3, double.NaN, 2, new Matrix(2, 0, 0, 3, double.NaN, -4) };
            yield return new object[] { matrix, 2, 3, 1, double.NaN, new Matrix(2, 0, 0, 3, -1, double.NaN) };
            yield return new object[] { matrix, 2, 3, double.NaN, double.NaN, new Matrix(2, 0, 0, 3, double.NaN, double.NaN) };
            yield return new object[] { matrix, double.NaN, double.NaN, double.NaN, double.NaN, new Matrix(double.NaN, 0, 0, double.NaN, double.NaN, double.NaN) };
        }

        // Scaled.
        yield return new object[] { new Matrix(2, 0, 0, 3, 0, 0), -2, -3, 0, 0, new Matrix(-4, 0, 0, -9, 0, 0) };
        yield return new object[] { new Matrix(2, 0, 0, 3, 0, 0), -2, 3, 0, 0, new Matrix(-4, 0, 0, 9, 0, 0) };
        yield return new object[] { new Matrix(2, 0, 0, 3, 0, 0), 2, -3, 0, 0, new Matrix(4, 0, 0, -9, 0, 0) };
        yield return new object[] { new Matrix(2, 0, 0, 3, 0, 0), -1, -2, 0, 0, new Matrix(-2, 0, 0, -6, 0, 0) };
        yield return new object[] { new Matrix(2, 0, 0, 3, 0, 0), -1, 0, 0, 0, new Matrix(-2, 0, 0, 0, 0, 0) };
        yield return new object[] { new Matrix(2, 0, 0, 3, 0, 0), 0, -2, 0, 0, new Matrix(0, 0, 0, -6, 0, 0) };
        yield return new object[] { new Matrix(2, 0, 0, 3, 0, 0), 0, 0, 0, 0, new Matrix(0, 0, 0, 0, 0, 0) };
        yield return new object[] { new Matrix(2, 0, 0, 3, 0, 0), 1, 1, 0, 0, new Matrix(2, 0, 0, 3, 0, 0) };
        yield return new object[] { new Matrix(2, 0, 0, 3, 0, 0), 1, 0, 0, 0, new Matrix(2, 0, 0, 0, 0, 0) };
        yield return new object[] { new Matrix(2, 0, 0, 3, 0, 0), 0, 2, 0, 0, new Matrix(0, 0, 0, 6, 0, 0) };
        yield return new object[] { new Matrix(2, 0, 0, 3, 0, 0), 1, 2, 0, 0, new Matrix(2, 0, 0, 6, 0, 0) };
        yield return new object[] { new Matrix(2, 0, 0, 3, 0, 0), 1.1, 2.2, 0, 0, new Matrix(2.2, 0, 0, 6.6, 0, 0) };
        yield return new object[] { new Matrix(2, 0, 0, 3, 0, 0), 2, 3, 0, 0, new Matrix(4, 0, 0, 9, 0, 0) };
        yield return new object[] { new Matrix(2, 0, 0, 3, 0, 0), double.NaN, 3, 0, 0, new Matrix(double.NaN, 0, 0, 9, double.NaN, 0) };
        yield return new object[] { new Matrix(2, 0, 0, 3, 0, 0), 2, double.NaN, 0, 0, new Matrix(4, 0, 0, double.NaN, 0, double.NaN) };
        yield return new object[] { new Matrix(2, 0, 0, 3, 0, 0), double.NaN, double.NaN, 0, 0, new Matrix(double.NaN, 0, 0, double.NaN, double.NaN, double.NaN) };
        yield return new object[] { new Matrix(2, 0, 0, 3, 0, 0), -2, -3, 1, 2, new Matrix(-4, 0, 0, -9, 3, 8) };
        yield return new object[] { new Matrix(2, 0, 0, 3, 0, 0), -2, 3, 1, 2, new Matrix(-4, 0, 0, 9, 3, -4) };
        yield return new object[] { new Matrix(2, 0, 0, 3, 0, 0), 2, -3, 1, 2, new Matrix(4, 0, 0, -9, -1, 8) };
        yield return new object[] { new Matrix(2, 0, 0, 3, 0, 0), -1, -2, 1, 2, new Matrix(-2, 0, 0, -6, 2, 6) };
        yield return new object[] { new Matrix(2, 0, 0, 3, 0, 0), -1, 0, 1, 2, new Matrix(-2, 0, 0, 0, 2, 2) };
        yield return new object[] { new Matrix(2, 0, 0, 3, 0, 0), 0, -2, 1, 2, new Matrix(0, 0, 0, -6, 1, 6) };
        yield return new object[] { new Matrix(2, 0, 0, 3, 0, 0), 0, 0, 1, 2, new Matrix(0, 0, 0, 0, 1, 2) };
        yield return new object[] { new Matrix(2, 0, 0, 3, 0, 0), 1, 1, 1, 2, new Matrix(2, 0, 0, 3, 0, 0) };
        yield return new object[] { new Matrix(2, 0, 0, 3, 0, 0), 1, 0, 1, 2, new Matrix(2, 0, 0, 0, 0, 2) };
        yield return new object[] { new Matrix(2, 0, 0, 3, 0, 0), 0, 2, 1, 2, new Matrix(0, 0, 0, 6, 1, -2) };
        yield return new object[] { new Matrix(2, 0, 0, 3, 0, 0), 1, 2, 1, 2, new Matrix(2, 0, 0, 6, 0, -2) };
        yield return new object[] { new Matrix(2, 0, 0, 3, 0, 0), 1.1, 2.2, 1, 2, new Matrix(2.2, 0, 0, 6.6, -0.1, -2.4) };
        yield return new object[] { new Matrix(2, 0, 0, 3, 0, 0), 2, 3, 1, 2, new Matrix(4, 0, 0, 9, -1, -4) };
        yield return new object[] { new Matrix(2, 0, 0, 3, 0, 0), double.NaN, 3, 1, 2, new Matrix(double.NaN, 0, 0, 9, double.NaN, -4) };
        yield return new object[] { new Matrix(2, 0, 0, 3, 0, 0), 2, double.NaN, 1, 2, new Matrix(4, 0, 0, double.NaN, -1, double.NaN) };
        yield return new object[] { new Matrix(2, 0, 0, 3, 0, 0), double.NaN, double.NaN, 1, 2, new Matrix(double.NaN, 0, 0, double.NaN, double.NaN, double.NaN) };
        yield return new object[] { new Matrix(2, 0, 0, 3, 0, 0), 2, 3, double.NaN, 2, new Matrix(4, 0, 0, 9, double.NaN, -4) };
        yield return new object[] { new Matrix(2, 0, 0, 3, 0, 0), 2, 3, 1, double.NaN, new Matrix(4, 0, 0, 9, -1, double.NaN) };
        yield return new object[] { new Matrix(2, 0, 0, 3, 0, 0), 2, 3, double.NaN, double.NaN, new Matrix(4, 0, 0, 9, double.NaN, double.NaN) };
        yield return new object[] { new Matrix(2, 0, 0, 3, 0, 0), double.NaN, double.NaN, double.NaN, double.NaN, new Matrix(double.NaN, 0, 0, double.NaN, double.NaN, double.NaN) };

        // Skewed.
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 0, 0), -2, -3, 0, 0, new Matrix(-2, -1.5, -1.5, -3, 0, 0) };
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 0, 0), -2, 3, 0, 0, new Matrix(-2, 1.5, -1.5, 3, 0, 0) };
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 0, 0), 2, -3, 0, 0, new Matrix(2, -1.5, 1.5, -3, 0, 0) };
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 0, 0), -1, -2, 0, 0, new Matrix(-1, -1, -0.75, -2, 0, 0) };
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 0, 0), -1, 0, 0, 0, new Matrix(-1, 0, -0.75, 0, 0, 0) };
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 0, 0), 0, -2, 0, 0, new Matrix(0, -1, 0, -2, 0, 0) };
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 0, 0), 0, 0, 0, 0, new Matrix(0, 0, 0, 0, 0, 0) };
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 0, 0), 1, 1, 0, 0, new Matrix(1, 0.5, 0.75, 1, 0, 0) };
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 0, 0), 1, 0, 0, 0, new Matrix(1, 0, 0.75, 0, 0, 0) };
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 0, 0), 0, 2, 0, 0, new Matrix(0, 1, 0, 2, 0, 0) };
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 0, 0), 1, 2, 0, 0, new Matrix(1, 1, 0.75, 2, 0, 0) };
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 0, 0), 1.1, 2.2, 0, 0, new Matrix(1.1, 1.1, 0.825, 2.2, 0, 0) };
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 0, 0), 2, 3, 0, 0, new Matrix(2, 1.5, 1.5, 3, 0, 0) };
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 0, 0), double.NaN, 3, 0, 0, new Matrix(double.NaN, 1.5, double.NaN, 3, double.NaN, 0) };
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 0, 0), 2, double.NaN, 0, 0, new Matrix(2, double.NaN, 1.5, double.NaN, 0, double.NaN) };
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 0, 0), double.NaN, double.NaN, 0, 0, new Matrix(double.NaN, double.NaN, double.NaN, double.NaN, double.NaN, double.NaN) };
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 0, 0), -2, -3, 1, 2, new Matrix(-2, -1.5, -1.5, -3, 3, 8) };
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 0, 0), -2, 3, 1, 2, new Matrix(-2, 1.5, -1.5, 3, 3, -4) };
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 0, 0), 2, -3, 1, 2, new Matrix(2, -1.5, 1.5, -3, -1, 8) };
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 0, 0), -1, -2, 1, 2, new Matrix(-1, -1, -0.75, -2, 2, 6) };
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 0, 0), -1, 0, 1, 2, new Matrix(-1, 0, -0.75, 0, 2, 2) };
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 0, 0), 0, -2, 1, 2, new Matrix(0, -1, 0, -2, 1, 6) };
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 0, 0), 0, 0, 1, 2, new Matrix(0, 0, 0, 0, 1, 2) };
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 0, 0), 1, 1, 1, 2, new Matrix(1, 0.5, 0.75, 1, 0, 0) };
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 0, 0), 1, 0, 1, 2, new Matrix(1, 0, 0.75, 0, 0, 2) };
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 0, 0), 0, 2, 1, 2, new Matrix(0, 1, 0, 2, 1, -2) };
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 0, 0), 1, 2, 1, 2, new Matrix(1, 1, 0.75, 2, 0, -2) };
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 0, 0), 1.1, 2.2, 1, 2, new Matrix(1.1, 1.1, 0.825, 2.2, -0.1, -2.4) };
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 0, 0), 2, 3, 1, 2, new Matrix(2, 1.5, 1.5, 3, -1, -4) };
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 0, 0), double.NaN, 3, 1, 2, new Matrix(double.NaN, 1.5, double.NaN, 3, double.NaN, -4) };
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 0, 0), 2, double.NaN, 1, 2, new Matrix(2, double.NaN, 1.5, double.NaN, -1, double.NaN) };
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 0, 0), double.NaN, double.NaN, 1, 2, new Matrix(double.NaN, double.NaN, double.NaN, double.NaN, double.NaN, double.NaN) };
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 0, 0), 2, 3, double.NaN, 2, new Matrix(2, 1.5, 1.5, 3, double.NaN, -4) };
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 0, 0), 2, 3, 1, double.NaN, new Matrix(2, 1.5, 1.5, 3, -1, double.NaN) };
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 0, 0), 2, 3, double.NaN, double.NaN, new Matrix(2, 1.5, 1.5, 3, double.NaN, double.NaN) };
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 0, 0), double.NaN, double.NaN, double.NaN, double.NaN, new Matrix(double.NaN, double.NaN, double.NaN, double.NaN, double.NaN, double.NaN) };

        // Translated.
        yield return new object[] { new Matrix(1, 0, 0, 1, 2, 3), -2, -3, 0, 0, new Matrix(-2, 0, 0, -3, -4, -9) };
        yield return new object[] { new Matrix(1, 0, 0, 1, 2, 3), -2, 3, 0, 0, new Matrix(-2, 0, 0, 3, -4, 9) };
        yield return new object[] { new Matrix(1, 0, 0, 1, 2, 3), 2, -3, 0, 0, new Matrix(2, 0, 0, -3, 4, -9) };
        yield return new object[] { new Matrix(1, 0, 0, 1, 2, 3), -1, -2, 0, 0, new Matrix(-1, 0, 0, -2, -2, -6) };
        yield return new object[] { new Matrix(1, 0, 0, 1, 2, 3), -1, 0, 0, 0, new Matrix(-1, 0, 0, 0, -2, 0) };
        yield return new object[] { new Matrix(1, 0, 0, 1, 2, 3), 0, -2, 0, 0, new Matrix(0, 0, 0, -2, 0, -6) };
        yield return new object[] { new Matrix(1, 0, 0, 1, 2, 3), 0, 0, 0, 0, new Matrix(0, 0, 0, 0, 0, 0) };
        yield return new object[] { new Matrix(1, 0, 0, 1, 2, 3), 1, 1, 0, 0, new Matrix(1, 0, 0, 1, 2, 3) };
        yield return new object[] { new Matrix(1, 0, 0, 1, 2, 3), 1, 0, 0, 0, new Matrix(1, 0, 0, 0, 2, 0) };
        yield return new object[] { new Matrix(1, 0, 0, 1, 2, 3), 0, 2, 0, 0, new Matrix(0, 0, 0, 2, 0, 6) };
        yield return new object[] { new Matrix(1, 0, 0, 1, 2, 3), 1, 2, 0, 0, new Matrix(1, 0, 0, 2, 2, 6) };
        yield return new object[] { new Matrix(1, 0, 0, 1, 2, 3), 1.1, 2.2, 0, 0, new Matrix(1.1, 0, 0, 2.2, 2.2, 6.6) };
        yield return new object[] { new Matrix(1, 0, 0, 1, 2, 3), 2, 3, 0, 0, new Matrix(2, 0, 0, 3, 4, 9) };
        yield return new object[] { new Matrix(1, 0, 0, 1, 2, 3), double.NaN, 3, 0, 0, new Matrix(double.NaN, 0, 0, 3, double.NaN, 9) };
        yield return new object[] { new Matrix(1, 0, 0, 1, 2, 3), 2, double.NaN, 0, 0, new Matrix(2, 0, 0, double.NaN, 4, double.NaN) };
        yield return new object[] { new Matrix(1, 0, 0, 1, 2, 3), double.NaN, double.NaN, 0, 0, new Matrix(double.NaN, 0, 0, double.NaN, double.NaN, double.NaN) };
        yield return new object[] { new Matrix(1, 0, 0, 1, 2, 3), -2, -3, 1, 2, new Matrix(-2, 0, 0, -3, -1, -1) };
        yield return new object[] { new Matrix(1, 0, 0, 1, 2, 3), -2, 3, 1, 2, new Matrix(-2, 0, 0, 3, -1, 5) };
        yield return new object[] { new Matrix(1, 0, 0, 1, 2, 3), 2, -3, 1, 2, new Matrix(2, 0, 0, -3, 3, -1) };
        yield return new object[] { new Matrix(1, 0, 0, 1, 2, 3), -1, -2, 1, 2, new Matrix(-1, 0, 0, -2, 0, 0) };
        yield return new object[] { new Matrix(1, 0, 0, 1, 2, 3), -1, 0, 1, 2, new Matrix(-1, 0, 0, 0, 0, 2) };
        yield return new object[] { new Matrix(1, 0, 0, 1, 2, 3), 0, -2, 1, 2, new Matrix(0, 0, 0, -2, 1, 0) };
        yield return new object[] { new Matrix(1, 0, 0, 1, 2, 3), 0, 0, 1, 2, new Matrix(0, 0, 0, 0, 1, 2) };
        yield return new object[] { new Matrix(1, 0, 0, 1, 2, 3), 1, 1, 1, 2, new Matrix(1, 0, 0, 1, 2, 3) };
        yield return new object[] { new Matrix(1, 0, 0, 1, 2, 3), 1, 0, 1, 2, new Matrix(1, 0, 0, 0, 2, 2) };
        yield return new object[] { new Matrix(1, 0, 0, 1, 2, 3), 0, 2, 1, 2, new Matrix(0, 0, 0, 2, 1, 4) };
        yield return new object[] { new Matrix(1, 0, 0, 1, 2, 3), 1, 2, 1, 2, new Matrix(1, 0, 0, 2, 2, 4) };
        yield return new object[] { new Matrix(1, 0, 0, 1, 2, 3), 1.1, 2.2, 1, 2, new Matrix(1.1, 0, 0, 2.2, 2.1, 4.2) };
        yield return new object[] { new Matrix(1, 0, 0, 1, 2, 3), 2, 3, 1, 2, new Matrix(2, 0, 0, 3, 3, 5) };
        yield return new object[] { new Matrix(1, 0, 0, 1, 2, 3), double.NaN, 3, 1, 2, new Matrix(double.NaN, 0, 0, 3, double.NaN, 5) };
        yield return new object[] { new Matrix(1, 0, 0, 1, 2, 3), 2, double.NaN, 1, 2, new Matrix(2, 0, 0, double.NaN, 3, double.NaN) };
        yield return new object[] { new Matrix(1, 0, 0, 1, 2, 3), double.NaN, double.NaN, 1, 2, new Matrix(double.NaN, 0, 0, double.NaN, double.NaN, double.NaN) };
        yield return new object[] { new Matrix(1, 0, 0, 1, 2, 3), 2, 3, double.NaN, 2, new Matrix(2, 0, 0, 3, double.NaN, 5) };
        yield return new object[] { new Matrix(1, 0, 0, 1, 2, 3), 2, 3, 1, double.NaN, new Matrix(2, 0, 0, 3, 3, double.NaN) };
        yield return new object[] { new Matrix(1, 0, 0, 1, 2, 3), 2, 3, double.NaN, double.NaN, new Matrix(2, 0, 0, 3, double.NaN, double.NaN) };
        yield return new object[] { new Matrix(1, 0, 0, 1, 2, 3), double.NaN, double.NaN, double.NaN, double.NaN, new Matrix(double.NaN, 0, 0, double.NaN, double.NaN, double.NaN) };

        // Translated and scaled.
        yield return new object[] { new Matrix(2, 0, 0, 3, 1, 2), -2, -3, 0, 0, new Matrix(-4, 0, 0, -9, -2, -6) };
        yield return new object[] { new Matrix(2, 0, 0, 3, 1, 2), -2, 3, 0, 0, new Matrix(-4, 0, 0, 9, -2, 6) };
        yield return new object[] { new Matrix(2, 0, 0, 3, 1, 2), 2, -3, 0, 0, new Matrix(4, 0, 0, -9, 2, -6) };
        yield return new object[] { new Matrix(2, 0, 0, 3, 1, 2), -1, -2, 0, 0, new Matrix(-2, 0, 0, -6, -1, -4) };
        yield return new object[] { new Matrix(2, 0, 0, 3, 1, 2), -1, 0, 0, 0, new Matrix(-2, 0, 0, 0, -1, 0) };
        yield return new object[] { new Matrix(2, 0, 0, 3, 1, 2), 0, -2, 0, 0, new Matrix(0, 0, 0, -6, 0, -4) };
        yield return new object[] { new Matrix(2, 0, 0, 3, 1, 2), 0, 0, 0, 0, new Matrix(0, 0, 0, 0, 0, 0) };
        yield return new object[] { new Matrix(2, 0, 0, 3, 1, 2), 1, 1, 0, 0, new Matrix(2, 0, 0, 3, 1, 2) };
        yield return new object[] { new Matrix(2, 0, 0, 3, 1, 2), 1, 0, 0, 0, new Matrix(2, 0, 0, 0, 1, 0) };
        yield return new object[] { new Matrix(2, 0, 0, 3, 1, 2), 0, 2, 0, 0, new Matrix(0, 0, 0, 6, 0, 4) };
        yield return new object[] { new Matrix(2, 0, 0, 3, 1, 2), 1, 2, 0, 0, new Matrix(2, 0, 0, 6, 1, 4) };
        yield return new object[] { new Matrix(2, 0, 0, 3, 1, 2), 1.1, 2.2, 0, 0, new Matrix(2.2, 0, 0, 6.6, 1.1, 4.4) };
        yield return new object[] { new Matrix(2, 0, 0, 3, 1, 2), 2, 3, 0, 0, new Matrix(4, 0, 0, 9, 2, 6) };
        yield return new object[] { new Matrix(2, 0, 0, 3, 1, 2), double.NaN, 3, 0, 0, new Matrix(double.NaN, 0, 0, 9, double.NaN, 6) };
        yield return new object[] { new Matrix(2, 0, 0, 3, 1, 2), 2, double.NaN, 0, 0, new Matrix(4, 0, 0, double.NaN, 2, double.NaN) };
        yield return new object[] { new Matrix(2, 0, 0, 3, 1, 2), double.NaN, double.NaN, 0, 0, new Matrix(double.NaN, 0, 0, double.NaN, double.NaN, double.NaN) };
        yield return new object[] { new Matrix(2, 0, 0, 3, 1, 2), -2, -3, 1, 2, new Matrix(-4, 0, 0, -9, 1, 2) };
        yield return new object[] { new Matrix(2, 0, 0, 3, 1, 2), -2, 3, 1, 2, new Matrix(-4, 0, 0, 9, 1, 2) };
        yield return new object[] { new Matrix(2, 0, 0, 3, 1, 2), 2, -3, 1, 2, new Matrix(4, 0, 0, -9, 1, 2) };
        yield return new object[] { new Matrix(2, 0, 0, 3, 1, 2), -1, -2, 1, 2, new Matrix(-2, 0, 0, -6, 1, 2) };
        yield return new object[] { new Matrix(2, 0, 0, 3, 1, 2), -1, 0, 1, 2, new Matrix(-2, 0, 0, 0, 1, 2) };
        yield return new object[] { new Matrix(2, 0, 0, 3, 1, 2), 0, -2, 1, 2, new Matrix(0, 0, 0, -6, 1, 2) };
        yield return new object[] { new Matrix(2, 0, 0, 3, 1, 2), 0, 0, 1, 2, new Matrix(0, 0, 0, 0, 1, 2) };
        yield return new object[] { new Matrix(2, 0, 0, 3, 1, 2), 1, 1, 1, 2, new Matrix(2, 0, 0, 3, 1, 2) };
        yield return new object[] { new Matrix(2, 0, 0, 3, 1, 2), 1, 0, 1, 2, new Matrix(2, 0, 0, 0, 1, 2) };
        yield return new object[] { new Matrix(2, 0, 0, 3, 1, 2), 0, 2, 1, 2, new Matrix(0, 0, 0, 6, 1, 2) };
        yield return new object[] { new Matrix(2, 0, 0, 3, 1, 2), 1, 2, 1, 2, new Matrix(2, 0, 0, 6, 1, 2) };
        yield return new object[] { new Matrix(2, 0, 0, 3, 1, 2), 1.1, 2.2, 1, 2, new Matrix(2.2, 0, 0, 6.6, 1, 2) };
        yield return new object[] { new Matrix(2, 0, 0, 3, 1, 2), 2, 3, 1, 2, new Matrix(4, 0, 0, 9, 1, 2) };
        yield return new object[] { new Matrix(2, 0, 0, 3, 1, 2), double.NaN, 3, 1, 2, new Matrix(double.NaN, 0, 0, 9, double.NaN, 2) };
        yield return new object[] { new Matrix(2, 0, 0, 3, 1, 2), 2, double.NaN, 1, 2, new Matrix(4, 0, 0, double.NaN, 1, double.NaN) };
        yield return new object[] { new Matrix(2, 0, 0, 3, 1, 2), double.NaN, double.NaN, 1, 2, new Matrix(double.NaN, 0, 0, double.NaN, double.NaN, double.NaN) };
        yield return new object[] { new Matrix(2, 0, 0, 3, 1, 2), 2, 3, double.NaN, 2, new Matrix(4, 0, 0, 9, double.NaN, 2) };
        yield return new object[] { new Matrix(2, 0, 0, 3, 1, 2), 2, 3, 1, double.NaN, new Matrix(4, 0, 0, 9, 1, double.NaN) };
        yield return new object[] { new Matrix(2, 0, 0, 3, 1, 2), 2, 3, double.NaN, double.NaN, new Matrix(4, 0, 0, 9, double.NaN, double.NaN) };
        yield return new object[] { new Matrix(2, 0, 0, 3, 1, 2), double.NaN, double.NaN, double.NaN, double.NaN, new Matrix(double.NaN, 0, 0, double.NaN, double.NaN, double.NaN) };

        // Translated and skewed.
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 2, 3), -2, -3, 0, 0, new Matrix(-2, -1.5, -1.5, -3, -4, -9) };
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 2, 3), -2, 3, 0, 0, new Matrix(-2, 1.5, -1.5, 3, -4, 9) };
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 2, 3), 2, -3, 0, 0, new Matrix(2, -1.5, 1.5, -3, 4, -9) };
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 2, 3), -1, -2, 0, 0, new Matrix(-1, -1, -0.75, -2, -2, -6) };
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 2, 3), -1, 0, 0, 0, new Matrix(-1, 0, -0.75, 0, -2, 0) };
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 2, 3), 0, -2, 0, 0, new Matrix(0, -1, 0, -2, 0, -6) };
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 2, 3), 0, 0, 0, 0, new Matrix(0, 0, 0, 0, 0, 0) };
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 2, 3), 1, 1, 0, 0, new Matrix(1, 0.5, 0.75, 1, 2, 3) };
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 2, 3), 1, 0, 0, 0, new Matrix(1, 0, 0.75, 0, 2, 0) };
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 2, 3), 0, 2, 0, 0, new Matrix(0, 1, 0, 2, 0, 6) };
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 2, 3), 1, 2, 0, 0, new Matrix(1, 1, 0.75, 2, 2, 6) };
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 2, 3), 1.1, 2.2, 0, 0, new Matrix(1.1, 1.1, 0.825, 2.2, 2.2, 6.6) };
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 2, 3), 2, 3, 0, 0, new Matrix(2, 1.5, 1.5, 3, 4, 9) };
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 2, 3), double.NaN, 3, 0, 0, new Matrix(double.NaN, 1.5, double.NaN, 3, double.NaN, 9) };
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 2, 3), 2, double.NaN, 0, 0, new Matrix(2, double.NaN, 1.5, double.NaN, 4, double.NaN) };
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 2, 3), double.NaN, double.NaN, 0, 0, new Matrix(double.NaN, double.NaN, double.NaN, double.NaN, double.NaN, double.NaN) };
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 2, 3), -2, -3, 1, 2, new Matrix(-2, -1.5, -1.5, -3, -1, -1) };
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 2, 3), -2, 3, 1, 2, new Matrix(-2, 1.5, -1.5, 3, -1, 5) };
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 2, 3), 2, -3, 1, 2, new Matrix(2, -1.5, 1.5, -3, 3, -1) };
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 2, 3), -1, -2, 1, 2, new Matrix(-1, -1, -0.75, -2, 0, 0) };
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 2, 3), -1, 0, 1, 2, new Matrix(-1, 0, -0.75, 0, 0, 2) };
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 2, 3), 0, -2, 1, 2, new Matrix(0, -1, 0, -2, 1, 0) };
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 2, 3), 0, 0, 1, 2, new Matrix(0, 0, 0, 0, 1, 2) };
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 2, 3), 1, 1, 1, 2, new Matrix(1, 0.5, 0.75, 1, 2, 3) };
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 2, 3), 1, 0, 1, 2, new Matrix(1, 0, 0.75, 0, 2, 2) };
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 2, 3), 0, 2, 1, 2, new Matrix(0, 1, 0, 2, 1, 4) };
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 2, 3), 1, 2, 1, 2, new Matrix(1, 1, 0.75, 2, 2, 4) };
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 2, 3), 1.1, 2.2, 1, 2, new Matrix(1.1, 1.1, 0.825, 2.2, 2.1, 4.2) };
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 2, 3), 2, 3, 1, 2, new Matrix(2, 1.5, 1.5, 3, 3, 5) };
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 2, 3), double.NaN, 3, 1, 2, new Matrix(double.NaN, 1.5, double.NaN, 3, double.NaN, 5) };
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 2, 3), 2, double.NaN, 1, 2, new Matrix(2, double.NaN, 1.5, double.NaN, 3, double.NaN) };
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 2, 3), double.NaN, double.NaN, 1, 2, new Matrix(double.NaN, double.NaN, double.NaN, double.NaN, double.NaN, double.NaN) };
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 2, 3), 2, 3, double.NaN, 2, new Matrix(2, 1.5, 1.5, 3, double.NaN, 5) };
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 2, 3), 2, 3, 1, double.NaN, new Matrix(2, 1.5, 1.5, 3, 3, double.NaN) };
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 2, 3), 2, 3, double.NaN, double.NaN, new Matrix(2, 1.5, 1.5, 3, double.NaN, double.NaN) };
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 2, 3), double.NaN, double.NaN, double.NaN, double.NaN, new Matrix(double.NaN, double.NaN, double.NaN, double.NaN, double.NaN, double.NaN) };

        // Skewed and scaled.
        yield return new object[] { new Matrix(2, 0.5, 0.75, 3, 0, 0), -2, -3, 0, 0, new Matrix(-4, -1.5, -1.5, -9, 0, 0) };
        yield return new object[] { new Matrix(2, 0.5, 0.75, 3, 0, 0), -2, 3, 0, 0, new Matrix(-4, 1.5, -1.5, 9, 0, 0) };
        yield return new object[] { new Matrix(2, 0.5, 0.75, 3, 0, 0), 2, -3, 0, 0, new Matrix(4, -1.5, 1.5, -9, 0, 0) };
        yield return new object[] { new Matrix(2, 0.5, 0.75, 3, 0, 0), -1, -2, 0, 0, new Matrix(-2, -1, -0.75, -6, 0, 0) };
        yield return new object[] { new Matrix(2, 0.5, 0.75, 3, 0, 0), -1, 0, 0, 0, new Matrix(-2, 0, -0.75, 0, 0, 0) };
        yield return new object[] { new Matrix(2, 0.5, 0.75, 3, 0, 0), 0, -2, 0, 0, new Matrix(0, -1, 0, -6, 0, 0) };
        yield return new object[] { new Matrix(2, 0.5, 0.75, 3, 0, 0), 0, 0, 0, 0, new Matrix(0, 0, 0, 0, 0, 0) };
        yield return new object[] { new Matrix(2, 0.5, 0.75, 3, 0, 0), 1, 1, 0, 0, new Matrix(2, 0.5, 0.75, 3, 0, 0) };
        yield return new object[] { new Matrix(2, 0.5, 0.75, 3, 0, 0), 1, 0, 0, 0, new Matrix(2, 0, 0.75, 0, 0, 0) };
        yield return new object[] { new Matrix(2, 0.5, 0.75, 3, 0, 0), 0, 2, 0, 0, new Matrix(0, 1, 0, 6, 0, 0) };
        yield return new object[] { new Matrix(2, 0.5, 0.75, 3, 0, 0), 1, 2, 0, 0, new Matrix(2, 1, 0.75, 6, 0, 0) };
        yield return new object[] { new Matrix(2, 0.5, 0.75, 3, 0, 0), 1.1, 2.2, 0, 0, new Matrix(2.2, 1.1, 0.825, 6.6, 0, 0) };
        yield return new object[] { new Matrix(2, 0.5, 0.75, 3, 0, 0), 2, 3, 0, 0, new Matrix(4, 1.5, 1.5, 9, 0, 0) };
        yield return new object[] { new Matrix(2, 0.5, 0.75, 3, 0, 0), double.NaN, 3, 0, 0, new Matrix(double.NaN, 1.5, double.NaN, 9, double.NaN, 0) };
        yield return new object[] { new Matrix(2, 0.5, 0.75, 3, 0, 0), 2, double.NaN, 0, 0, new Matrix(4, double.NaN, 1.5, double.NaN, 0, double.NaN) };
        yield return new object[] { new Matrix(2, 0.5, 0.75, 3, 0, 0), double.NaN, double.NaN, 0, 0, new Matrix(double.NaN, double.NaN, double.NaN, double.NaN, double.NaN, double.NaN) };
        yield return new object[] { new Matrix(2, 0.5, 0.75, 3, 0, 0), -2, -3, 1, 2, new Matrix(-4, -1.5, -1.5, -9, 3, 8) };
        yield return new object[] { new Matrix(2, 0.5, 0.75, 3, 0, 0), -2, 3, 1, 2, new Matrix(-4, 1.5, -1.5, 9, 3, -4) };
        yield return new object[] { new Matrix(2, 0.5, 0.75, 3, 0, 0), 2, -3, 1, 2, new Matrix(4, -1.5, 1.5, -9, -1, 8) };
        yield return new object[] { new Matrix(2, 0.5, 0.75, 3, 0, 0), -1, -2, 1, 2, new Matrix(-2, -1, -0.75, -6, 2, 6) };
        yield return new object[] { new Matrix(2, 0.5, 0.75, 3, 0, 0), -1, 0, 1, 2, new Matrix(-2, 0, -0.75, 0, 2, 2) };
        yield return new object[] { new Matrix(2, 0.5, 0.75, 3, 0, 0), 0, -2, 1, 2, new Matrix(0, -1, 0, -6, 1, 6) };
        yield return new object[] { new Matrix(2, 0.5, 0.75, 3, 0, 0), 0, 0, 1, 2, new Matrix(0, 0, 0, 0, 1, 2) };
        yield return new object[] { new Matrix(2, 0.5, 0.75, 3, 0, 0), 1, 1, 1, 2, new Matrix(2, 0.5, 0.75, 3, 0, 0) };
        yield return new object[] { new Matrix(2, 0.5, 0.75, 3, 0, 0), 1, 0, 1, 2, new Matrix(2, 0, 0.75, 0, 0, 2) };
        yield return new object[] { new Matrix(2, 0.5, 0.75, 3, 0, 0), 0, 2, 1, 2, new Matrix(0, 1, 0, 6, 1, -2) };
        yield return new object[] { new Matrix(2, 0.5, 0.75, 3, 0, 0), 1, 2, 1, 2, new Matrix(2, 1, 0.75, 6, 0, -2) };
        yield return new object[] { new Matrix(2, 0.5, 0.75, 3, 0, 0), 1.1, 2.2, 1, 2, new Matrix(2.2, 1.1, 0.825, 6.6, -0.1, -2.4) };
        yield return new object[] { new Matrix(2, 0.5, 0.75, 3, 0, 0), 2, 3, 1, 2, new Matrix(4, 1.5, 1.5, 9, -1, -4) };
        yield return new object[] { new Matrix(2, 0.5, 0.75, 3, 0, 0), double.NaN, 3, 1, 2, new Matrix(double.NaN, 1.5, double.NaN, 9, double.NaN, -4) };
        yield return new object[] { new Matrix(2, 0.5, 0.75, 3, 0, 0), 2, double.NaN, 1, 2, new Matrix(4, double.NaN, 1.5, double.NaN, -1, double.NaN) };
        yield return new object[] { new Matrix(2, 0.5, 0.75, 3, 0, 0), double.NaN, double.NaN, 1, 2, new Matrix(double.NaN, double.NaN, double.NaN, double.NaN, double.NaN, double.NaN) };
        yield return new object[] { new Matrix(2, 0.5, 0.75, 3, 0, 0), 2, 3, double.NaN, 2, new Matrix(4, 1.5, 1.5, 9, double.NaN, -4) };
        yield return new object[] { new Matrix(2, 0.5, 0.75, 3, 0, 0), 2, 3, 1, double.NaN, new Matrix(4, 1.5, 1.5, 9, -1, double.NaN) };
        yield return new object[] { new Matrix(2, 0.5, 0.75, 3, 0, 0), 2, 3, double.NaN, double.NaN, new Matrix(4, 1.5, 1.5, 9, double.NaN, double.NaN) };
        yield return new object[] { new Matrix(2, 0.5, 0.75, 3, 0, 0), double.NaN, double.NaN, double.NaN, double.NaN, new Matrix(double.NaN, double.NaN, double.NaN, double.NaN, double.NaN, double.NaN) };

        // Complex.
        yield return new object[] { new Matrix(2, 3, 4, 5, 6, 7), -2, -3, 0, 0, new Matrix(-4, -9, -8, -15, -12, -21) };
        yield return new object[] { new Matrix(2, 3, 4, 5, 6, 7), -2, 3, 0, 0, new Matrix(-4, 9, -8, 15, -12, 21) };
        yield return new object[] { new Matrix(2, 3, 4, 5, 6, 7), 2, -3, 0, 0, new Matrix(4, -9, 8, -15, 12, -21) };
        yield return new object[] { new Matrix(2, 3, 4, 5, 6, 7), -1, -2, 0, 0, new Matrix(-2, -6, -4, -10, -6, -14) };
        yield return new object[] { new Matrix(2, 3, 4, 5, 6, 7), -1, 0, 0, 0, new Matrix(-2, 0, -4, 0, -6, 0) };
        yield return new object[] { new Matrix(2, 3, 4, 5, 6, 7), 0, -2, 0, 0, new Matrix(0, -6, 0, -10, 0, -14) };
        yield return new object[] { new Matrix(2, 3, 4, 5, 6, 7), 0, 0, 0, 0, new Matrix(0, 0, 0, 0, 0, 0) };
        yield return new object[] { new Matrix(2, 3, 4, 5, 6, 7), 1, 1, 0, 0, new Matrix(2, 3, 4, 5, 6, 7) };
        yield return new object[] { new Matrix(2, 3, 4, 5, 6, 7), 1, 0, 0, 0, new Matrix(2, 0, 4, 0, 6, 0) };
        yield return new object[] { new Matrix(2, 3, 4, 5, 6, 7), 0, 2, 0, 0, new Matrix(0, 6, 0, 10, 0, 14) };
        yield return new object[] { new Matrix(2, 3, 4, 5, 6, 7), 1, 2, 0, 0, new Matrix(2, 6, 4, 10, 6, 14) };
        yield return new object[] { new Matrix(2, 3, 4, 5, 6, 7), 1.1, 2.2, 0, 0, new Matrix(2.2, 6.6, 4.4, 11, 6.6, 15.4) };
        yield return new object[] { new Matrix(2, 3, 4, 5, 6, 7), 2, 3, 0, 0, new Matrix(4, 9, 8, 15, 12, 21) };
        yield return new object[] { new Matrix(2, 3, 4, 5, 6, 7), double.NaN, 3, 0, 0, new Matrix(double.NaN, 9, double.NaN, 15, double.NaN, 21) };
        yield return new object[] { new Matrix(2, 3, 4, 5, 6, 7), 2, double.NaN, 0, 0, new Matrix(4, double.NaN, 8, double.NaN, 12, double.NaN) };
        yield return new object[] { new Matrix(2, 3, 4, 5, 6, 7), double.NaN, double.NaN, 0, 0, new Matrix(double.NaN, double.NaN, double.NaN, double.NaN, double.NaN, double.NaN) };
        yield return new object[] { new Matrix(2, 3, 4, 5, 6, 7), -2, -3, 1, 2, new Matrix(-4, -9, -8, -15, -9, -13) };
        yield return new object[] { new Matrix(2, 3, 4, 5, 6, 7), -2, 3, 1, 2, new Matrix(-4, 9, -8, 15, -9, 17) };
        yield return new object[] { new Matrix(2, 3, 4, 5, 6, 7), 2, -3, 1, 2, new Matrix(4, -9, 8, -15, 11, -13) };
        yield return new object[] { new Matrix(2, 3, 4, 5, 6, 7), -1, -2, 1, 2, new Matrix(-2, -6, -4, -10, -4, -8) };
        yield return new object[] { new Matrix(2, 3, 4, 5, 6, 7), -1, 0, 1, 2, new Matrix(-2, 0, -4, 0, -4, 2) };
        yield return new object[] { new Matrix(2, 3, 4, 5, 6, 7), 0, -2, 1, 2, new Matrix(0, -6, 0, -10, 1, -8) };
        yield return new object[] { new Matrix(2, 3, 4, 5, 6, 7), 0, 0, 1, 2, new Matrix(0, 0, 0, 0, 1, 2) };
        yield return new object[] { new Matrix(2, 3, 4, 5, 6, 7), 1, 1, 1, 2, new Matrix(2, 3, 4, 5, 6, 7) };
        yield return new object[] { new Matrix(2, 3, 4, 5, 6, 7), 1, 0, 1, 2, new Matrix(2, 0, 4, 0, 6, 2) };
        yield return new object[] { new Matrix(2, 3, 4, 5, 6, 7), 0, 2, 1, 2, new Matrix(0, 6, 0, 10, 1, 12) };
        yield return new object[] { new Matrix(2, 3, 4, 5, 6, 7), 1, 2, 1, 2, new Matrix(2, 6, 4, 10, 6, 12) };
        yield return new object[] { new Matrix(2, 3, 4, 5, 6, 7), 1.1, 2.2, 1, 2, new Matrix(2.2, 6.6, 4.4, 11, 6.5, 13) };
        yield return new object[] { new Matrix(2, 3, 4, 5, 6, 7), 2, 3, 1, 2, new Matrix(4, 9, 8, 15, 11, 17) };
        yield return new object[] { new Matrix(2, 3, 4, 5, 6, 7), double.NaN, 3, 1, 2, new Matrix(double.NaN, 9, double.NaN, 15, double.NaN, 17) };
        yield return new object[] { new Matrix(2, 3, 4, 5, 6, 7), 2, double.NaN, 1, 2, new Matrix(4, double.NaN, 8, double.NaN, 11, double.NaN) };
        yield return new object[] { new Matrix(2, 3, 4, 5, 6, 7), double.NaN, double.NaN, 1, 2, new Matrix(double.NaN, double.NaN, double.NaN, double.NaN, double.NaN, double.NaN) };
        yield return new object[] { new Matrix(2, 3, 4, 5, 6, 7), 2, 3, double.NaN, 2, new Matrix(4, 9, 8, 15, double.NaN, 17) };
        yield return new object[] { new Matrix(2, 3, 4, 5, 6, 7), 2, 3, 1, double.NaN, new Matrix(4, 9, 8, 15, 11, double.NaN) };
        yield return new object[] { new Matrix(2, 3, 4, 5, 6, 7), 2, 3, double.NaN, double.NaN, new Matrix(4, 9, 8, 15, double.NaN, double.NaN) };
        yield return new object[] { new Matrix(2, 3, 4, 5, 6, 7), double.NaN, double.NaN, double.NaN, double.NaN, new Matrix(double.NaN, double.NaN, double.NaN, double.NaN, double.NaN, double.NaN) };

        // No inverse.
        yield return new object[] { new Matrix(0, 0, 0, 0, 0, 0), -2, -3, 0, 0, new Matrix(-0, 0, 0, -0, 0, 0) };
        yield return new object[] { new Matrix(0, 0, 0, 0, 0, 0), -2, 3, 0, 0, new Matrix(-0, 0, 0, 0, 0, 0) };
        yield return new object[] { new Matrix(0, 0, 0, 0, 0, 0), 2, -3, 0, 0, new Matrix(0, 0, 0, -0, 0, 0) };
        yield return new object[] { new Matrix(0, 0, 0, 0, 0, 0), -1, -2, 0, 0, new Matrix(-0, 0, 0, -0, 0, 0) };
        yield return new object[] { new Matrix(0, 0, 0, 0, 0, 0), -1, 0, 0, 0, new Matrix(-0, 0, 0, 0, 0, 0) };
        yield return new object[] { new Matrix(0, 0, 0, 0, 0, 0), 0, -2, 0, 0, new Matrix(0, 0, 0, -0, 0, 0) };
        yield return new object[] { new Matrix(0, 0, 0, 0, 0, 0), 0, 0, 0, 0, new Matrix(0, 0, 0, 0, 0, 0) };
        yield return new object[] { new Matrix(0, 0, 0, 0, 0, 0), 1, 1, 0, 0, new Matrix(0, 0, 0, 0, 0, 0) };
        yield return new object[] { new Matrix(0, 0, 0, 0, 0, 0), 1, 0, 0, 0, new Matrix(0, 0, 0, 0, 0, 0) };
        yield return new object[] { new Matrix(0, 0, 0, 0, 0, 0), 0, 2, 0, 0, new Matrix(0, 0, 0, 0, 0, 0) };
        yield return new object[] { new Matrix(0, 0, 0, 0, 0, 0), 1, 2, 0, 0, new Matrix(0, 0, 0, 0, 0, 0) };
        yield return new object[] { new Matrix(0, 0, 0, 0, 0, 0), 1.1, 2.2, 0, 0, new Matrix(0, 0, 0, 0, 0, 0) };
        yield return new object[] { new Matrix(0, 0, 0, 0, 0, 0), 2, 3, 0, 0, new Matrix(0, 0, 0, 0, 0, 0) };
        yield return new object[] { new Matrix(0, 0, 0, 0, 0, 0), double.NaN, 3, 0, 0, new Matrix(double.NaN, 0, 0, 0, double.NaN, 0) };
        yield return new object[] { new Matrix(0, 0, 0, 0, 0, 0), 2, double.NaN, 0, 0, new Matrix(0, 0, 0, double.NaN, 0, double.NaN) };
        yield return new object[] { new Matrix(0, 0, 0, 0, 0, 0), double.NaN, double.NaN, 0, 0, new Matrix(double.NaN, 0, 0, double.NaN, double.NaN, double.NaN) };
        yield return new object[] { new Matrix(0, 0, 0, 0, 0, 0), -2, -3, 1, 2, new Matrix(-0, 0, 0, -0, 3, 8) };
        yield return new object[] { new Matrix(0, 0, 0, 0, 0, 0), -2, 3, 1, 2, new Matrix(-0, 0, 0, 0, 3, -4) };
        yield return new object[] { new Matrix(0, 0, 0, 0, 0, 0), 2, -3, 1, 2, new Matrix(0, 0, 0, -0, -1, 8) };
        yield return new object[] { new Matrix(0, 0, 0, 0, 0, 0), -1, -2, 1, 2, new Matrix(-0, 0, 0, -0, 2, 6) };
        yield return new object[] { new Matrix(0, 0, 0, 0, 0, 0), -1, 0, 1, 2, new Matrix(-0, 0, 0, 0, 2, 2) };
        yield return new object[] { new Matrix(0, 0, 0, 0, 0, 0), 0, -2, 1, 2, new Matrix(0, 0, 0, -0, 1, 6) };
        yield return new object[] { new Matrix(0, 0, 0, 0, 0, 0), 0, 0, 1, 2, new Matrix(0, 0, 0, 0, 1, 2) };
        yield return new object[] { new Matrix(0, 0, 0, 0, 0, 0), 1, 1, 1, 2, new Matrix(0, 0, 0, 0, 0, 0) };
        yield return new object[] { new Matrix(0, 0, 0, 0, 0, 0), 1, 0, 1, 2, new Matrix(0, 0, 0, 0, 0, 2) };
        yield return new object[] { new Matrix(0, 0, 0, 0, 0, 0), 0, 2, 1, 2, new Matrix(0, 0, 0, 0, 1, -2) };
        yield return new object[] { new Matrix(0, 0, 0, 0, 0, 0), 1, 2, 1, 2, new Matrix(0, 0, 0, 0, 0, -2) };
        yield return new object[] { new Matrix(0, 0, 0, 0, 0, 0), 1.1, 2.2, 1, 2, new Matrix(0, 0, 0, 0, -0.1, -2.4) };
        yield return new object[] { new Matrix(0, 0, 0, 0, 0, 0), 2, 3, 1, 2, new Matrix(0, 0, 0, 0, -1, -4) };
        yield return new object[] { new Matrix(0, 0, 0, 0, 0, 0), double.NaN, 3, 1, 2, new Matrix(double.NaN, 0, 0, 0, double.NaN, -4) };
        yield return new object[] { new Matrix(0, 0, 0, 0, 0, 0), 2, double.NaN, 1, 2, new Matrix(0, 0, 0, double.NaN, -1, double.NaN) };
        yield return new object[] { new Matrix(0, 0, 0, 0, 0, 0), double.NaN, double.NaN, 1, 2, new Matrix(double.NaN, 0, 0, double.NaN, double.NaN, double.NaN) };
        yield return new object[] { new Matrix(0, 0, 0, 0, 0, 0), 2, 3, double.NaN, 2, new Matrix(0, 0, 0, 0, double.NaN, -4) };
        yield return new object[] { new Matrix(0, 0, 0, 0, 0, 0), 2, 3, 1, double.NaN, new Matrix(0, 0, 0, 0, -1, double.NaN) };
        yield return new object[] { new Matrix(0, 0, 0, 0, 0, 0), 2, 3, double.NaN, double.NaN, new Matrix(0, 0, 0, 0, double.NaN, double.NaN) };
        yield return new object[] { new Matrix(0, 0, 0, 0, 0, 0), double.NaN, double.NaN, double.NaN, double.NaN, new Matrix(double.NaN, 0, 0, double.NaN, double.NaN, double.NaN) };
    }

    [Theory]
    [MemberData(nameof(ScaleAt_TestData))]
    public void ScaleAt_Invoke_Success(Matrix matrix, double scaleX, double scaleY, double centerX, double centerY, Matrix expected)
    {
        matrix.ScaleAt(scaleX, scaleY, centerX, centerY);
        Helpers.AssertEqualRounded(expected, matrix);
        Assert.Equal(expected.IsIdentity, matrix.IsIdentity);
        Assert.Equal(expected.HasInverse, matrix.HasInverse);
        Assert.Equal(expected.Determinant, matrix.Determinant, precision: 5);
    }

    public static IEnumerable<object[]> ScaleAtPrepend_TestData()
    {
        // Identity
        foreach (Matrix matrix in IdentityMatrices())
        {
            yield return new object[] { matrix, -2, -3, 0, 0, new Matrix(-2, 0, 0, -3, 0, 0) };
            yield return new object[] { matrix, -2, 3, 0, 0, new Matrix(-2, 0, 0, 3, 0, 0) };
            yield return new object[] { matrix, 2, -3, 0, 0, new Matrix(2, 0, 0, -3, 0, 0) };
            yield return new object[] { matrix, -1, -2, 0, 0, new Matrix(-1, 0, 0, -2, 0, 0) };
            yield return new object[] { matrix, -1, 0, 0, 0, new Matrix(-1, 0, 0, 0, 0, 0) };
            yield return new object[] { matrix, 0, -2, 0, 0, new Matrix(0, 0, 0, -2, 0, 0) };
            yield return new object[] { matrix, 0, 0, 0, 0, new Matrix(0, 0, 0, 0, 0, 0) };
            yield return new object[] { matrix, 1, 1, 0, 0, new Matrix(1, 0, 0, 1, 0, 0) };
            yield return new object[] { matrix, 1, 0, 0, 0, new Matrix(1, 0, 0, 0, 0, 0) };
            yield return new object[] { matrix, 0, 2, 0, 0, new Matrix(0, 0, 0, 2, 0, 0) };
            yield return new object[] { matrix, 1, 2, 0, 0, new Matrix(1, 0, 0, 2, 0, 0) };
            yield return new object[] { matrix, 1.1, 2.2, 0, 0, new Matrix(1.1, 0, 0, 2.2, 0, 0) };
            yield return new object[] { matrix, 2, 3, 0, 0, new Matrix(2, 0, 0, 3, 0, 0) };
            yield return new object[] { matrix, double.NaN, 3, 0, 0, new Matrix(double.NaN, 0, 0, 3, double.NaN, 0) };
            yield return new object[] { matrix, 2, double.NaN, 0, 0, new Matrix(2, 0, 0, double.NaN, 0, double.NaN) };
            yield return new object[] { matrix, double.NaN, double.NaN, 0, 0, new Matrix(double.NaN, 0, 0, double.NaN, double.NaN, double.NaN) };
            yield return new object[] { matrix, -2, -3, 1, 2, new Matrix(-2, 0, 0, -3, 3, 8) };
            yield return new object[] { matrix, -2, 3, 1, 2, new Matrix(-2, 0, 0, 3, 3, -4) };
            yield return new object[] { matrix, 2, -3, 1, 2, new Matrix(2, 0, 0, -3, -1, 8) };
            yield return new object[] { matrix, -1, -2, 1, 2, new Matrix(-1, 0, 0, -2, 2, 6) };
            yield return new object[] { matrix, -1, 0, 1, 2, new Matrix(-1, 0, 0, 0, 2, 2) };
            yield return new object[] { matrix, 0, -2, 1, 2, new Matrix(0, 0, 0, -2, 1, 6) };
            yield return new object[] { matrix, 0, 0, 1, 2, new Matrix(0, 0, 0, 0, 1, 2) };
            yield return new object[] { matrix, 1, 1, 1, 2, new Matrix(1, 0, 0, 1, 0, 0) };
            yield return new object[] { matrix, 1, 0, 1, 2, new Matrix(1, 0, 0, 0, 0, 2) };
            yield return new object[] { matrix, 0, 2, 1, 2, new Matrix(0, 0, 0, 2, 1, -2) };
            yield return new object[] { matrix, 1, 2, 1, 2, new Matrix(1, 0, 0, 2, 0, -2) };
            yield return new object[] { matrix, 1.1, 2.2, 1, 2, new Matrix(1.1, 0, 0, 2.2, -0.1, -2.4) };
            yield return new object[] { matrix, 2, 3, 1, 2, new Matrix(2, 0, 0, 3, -1, -4) };
            yield return new object[] { matrix, double.NaN, 3, 1, 2, new Matrix(double.NaN, 0, 0, 3, double.NaN, -4) };
            yield return new object[] { matrix, 2, double.NaN, 1, 2, new Matrix(2, 0, 0, double.NaN, -1, double.NaN) };
            yield return new object[] { matrix, double.NaN, double.NaN, 1, 2, new Matrix(double.NaN, 0, 0, double.NaN, double.NaN, double.NaN) };
            yield return new object[] { matrix, 2, 3, double.NaN, 2, new Matrix(2, 0, 0, 3, double.NaN, -4) };
            yield return new object[] { matrix, 2, 3, 1, double.NaN, new Matrix(2, 0, 0, 3, -1, double.NaN) };
            yield return new object[] { matrix, 2, 3, double.NaN, double.NaN, new Matrix(2, 0, 0, 3, double.NaN, double.NaN) };
            yield return new object[] { matrix, double.NaN, double.NaN, double.NaN, double.NaN, new Matrix(double.NaN, 0, 0, double.NaN, double.NaN, double.NaN) };
        }

        // Scaled.
        yield return new object[] { new Matrix(2, 0, 0, 3, 0, 0), -2, -3, 0, 0, new Matrix(-4, 0, 0, -9, 0, 0) };
        yield return new object[] { new Matrix(2, 0, 0, 3, 0, 0), -2, 3, 0, 0, new Matrix(-4, 0, 0, 9, 0, 0) };
        yield return new object[] { new Matrix(2, 0, 0, 3, 0, 0), 2, -3, 0, 0, new Matrix(4, 0, 0, -9, 0, 0) };
        yield return new object[] { new Matrix(2, 0, 0, 3, 0, 0), -1, -2, 0, 0, new Matrix(-2, 0, 0, -6, 0, 0) };
        yield return new object[] { new Matrix(2, 0, 0, 3, 0, 0), -1, 0, 0, 0, new Matrix(-2, 0, 0, 0, 0, 0) };
        yield return new object[] { new Matrix(2, 0, 0, 3, 0, 0), 0, -2, 0, 0, new Matrix(0, 0, 0, -6, 0, 0) };
        yield return new object[] { new Matrix(2, 0, 0, 3, 0, 0), 0, 0, 0, 0, new Matrix(0, 0, 0, 0, 0, 0) };
        yield return new object[] { new Matrix(2, 0, 0, 3, 0, 0), 1, 1, 0, 0, new Matrix(2, 0, 0, 3, 0, 0) };
        yield return new object[] { new Matrix(2, 0, 0, 3, 0, 0), 1, 0, 0, 0, new Matrix(2, 0, 0, 0, 0, 0) };
        yield return new object[] { new Matrix(2, 0, 0, 3, 0, 0), 0, 2, 0, 0, new Matrix(0, 0, 0, 6, 0, 0) };
        yield return new object[] { new Matrix(2, 0, 0, 3, 0, 0), 1, 2, 0, 0, new Matrix(2, 0, 0, 6, 0, 0) };
        yield return new object[] { new Matrix(2, 0, 0, 3, 0, 0), 1.1, 2.2, 0, 0, new Matrix(2.2, 0, 0, 6.6, 0, 0) };
        yield return new object[] { new Matrix(2, 0, 0, 3, 0, 0), 2, 3, 0, 0, new Matrix(4, 0, 0, 9, 0, 0) };
        yield return new object[] { new Matrix(2, 0, 0, 3, 0, 0), double.NaN, 3, 0, 0, new Matrix(double.NaN, 0, 0, 9, double.NaN, 0) };
        yield return new object[] { new Matrix(2, 0, 0, 3, 0, 0), 2, double.NaN, 0, 0, new Matrix(4, 0, 0, double.NaN, 0, double.NaN) };
        yield return new object[] { new Matrix(2, 0, 0, 3, 0, 0), double.NaN, double.NaN, 0, 0, new Matrix(double.NaN, 0, 0, double.NaN, double.NaN, double.NaN) };
        yield return new object[] { new Matrix(2, 0, 0, 3, 0, 0), -2, -3, 1, 2, new Matrix(-4, 0, 0, -9, 6, 24) };
        yield return new object[] { new Matrix(2, 0, 0, 3, 0, 0), -2, 3, 1, 2, new Matrix(-4, 0, 0, 9, 6, -12) };
        yield return new object[] { new Matrix(2, 0, 0, 3, 0, 0), 2, -3, 1, 2, new Matrix(4, 0, 0, -9, -2, 24) };
        yield return new object[] { new Matrix(2, 0, 0, 3, 0, 0), -1, -2, 1, 2, new Matrix(-2, 0, 0, -6, 4, 18) };
        yield return new object[] { new Matrix(2, 0, 0, 3, 0, 0), -1, 0, 1, 2, new Matrix(-2, 0, 0, 0, 4, 6) };
        yield return new object[] { new Matrix(2, 0, 0, 3, 0, 0), 0, -2, 1, 2, new Matrix(0, 0, 0, -6, 2, 18) };
        yield return new object[] { new Matrix(2, 0, 0, 3, 0, 0), 0, 0, 1, 2, new Matrix(0, 0, 0, 0, 2, 6) };
        yield return new object[] { new Matrix(2, 0, 0, 3, 0, 0), 1, 1, 1, 2, new Matrix(2, 0, 0, 3, 0, 0) };
        yield return new object[] { new Matrix(2, 0, 0, 3, 0, 0), 1, 0, 1, 2, new Matrix(2, 0, 0, 0, 0, 6) };
        yield return new object[] { new Matrix(2, 0, 0, 3, 0, 0), 0, 2, 1, 2, new Matrix(0, 0, 0, 6, 2, -6) };
        yield return new object[] { new Matrix(2, 0, 0, 3, 0, 0), 1, 2, 1, 2, new Matrix(2, 0, 0, 6, 0, -6) };
        yield return new object[] { new Matrix(2, 0, 0, 3, 0, 0), 1.1, 2.2, 1, 2, new Matrix(2.2, 0, 0, 6.6, -0.2, -7.2) };
        yield return new object[] { new Matrix(2, 0, 0, 3, 0, 0), 2, 3, 1, 2, new Matrix(4, 0, 0, 9, -2, -12) };
        yield return new object[] { new Matrix(2, 0, 0, 3, 0, 0), double.NaN, 3, 1, 2, new Matrix(double.NaN, 0, 0, 9, double.NaN, -12) };
        yield return new object[] { new Matrix(2, 0, 0, 3, 0, 0), 2, double.NaN, 1, 2, new Matrix(4, 0, 0, double.NaN, -2, double.NaN) };
        yield return new object[] { new Matrix(2, 0, 0, 3, 0, 0), double.NaN, double.NaN, 1, 2, new Matrix(double.NaN, 0, 0, double.NaN, double.NaN, double.NaN) };
        yield return new object[] { new Matrix(2, 0, 0, 3, 0, 0), 2, 3, double.NaN, 2, new Matrix(4, 0, 0, 9, double.NaN, -12) };
        yield return new object[] { new Matrix(2, 0, 0, 3, 0, 0), 2, 3, 1, double.NaN, new Matrix(4, 0, 0, 9, -2, double.NaN) };
        yield return new object[] { new Matrix(2, 0, 0, 3, 0, 0), 2, 3, double.NaN, double.NaN, new Matrix(4, 0, 0, 9, double.NaN, double.NaN) };
        yield return new object[] { new Matrix(2, 0, 0, 3, 0, 0), double.NaN, double.NaN, double.NaN, double.NaN, new Matrix(double.NaN, 0, 0, double.NaN, double.NaN, double.NaN) };

        // Skewed.
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 0, 0), -2, -3, 0, 0, new Matrix(-2, -1, -2.25, -3, 0, 0) };
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 0, 0), -2, 3, 0, 0, new Matrix(-2, -1, 2.25, 3, 0, 0) };
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 0, 0), 2, -3, 0, 0, new Matrix(2, 1, -2.25, -3, 0, 0) };
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 0, 0), -1, -2, 0, 0, new Matrix(-1, -0.5, -1.5, -2, 0, 0) };
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 0, 0), -1, 0, 0, 0, new Matrix(-1, -0.5, 0, 0, 0, 0) };
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 0, 0), 0, -2, 0, 0, new Matrix(0, 0, -1.5, -2, 0, 0) };
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 0, 0), 0, 0, 0, 0, new Matrix(0, 0, 0, 0, 0, 0) };
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 0, 0), 1, 1, 0, 0, new Matrix(1, 0.5, 0.75, 1, 0, 0) };
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 0, 0), 1, 0, 0, 0, new Matrix(1, 0.5, 0, 0, 0, 0) };
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 0, 0), 0, 2, 0, 0, new Matrix(0, 0, 1.5, 2, 0, 0) };
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 0, 0), 1, 2, 0, 0, new Matrix(1, 0.5, 1.5, 2, 0, 0) };
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 0, 0), 1.1, 2.2, 0, 0, new Matrix(1.1, 0.55, 1.65, 2.2, 0, 0) };
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 0, 0), 2, 3, 0, 0, new Matrix(2, 1, 2.25, 3, 0, 0) };
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 0, 0), double.NaN, 3, 0, 0, new Matrix(double.NaN, double.NaN, 2.25, 3, double.NaN, double.NaN) };
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 0, 0), 2, double.NaN, 0, 0, new Matrix(2, 1, double.NaN, double.NaN, double.NaN, double.NaN) };
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 0, 0), double.NaN, double.NaN, 0, 0, new Matrix(double.NaN, double.NaN, double.NaN, double.NaN, double.NaN, double.NaN) };
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 0, 0), -2, -3, 1, 2, new Matrix(-2, -1, -2.25, -3, 9, 9.5) };
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 0, 0), -2, 3, 1, 2, new Matrix(-2, -1, 2.25, 3, 0, -2.5) };
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 0, 0), 2, -3, 1, 2, new Matrix(2, 1, -2.25, -3, 5, 7.5) };
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 0, 0), -1, -2, 1, 2, new Matrix(-1, -0.5, -1.5, -2, 6.5, 7) };
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 0, 0), -1, 0, 1, 2, new Matrix(-1, -0.5, 0, 0, 3.5, 3) };
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 0, 0), 0, -2, 1, 2, new Matrix(0, 0, -1.5, -2, 5.5, 6.5) };
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 0, 0), 0, 0, 1, 2, new Matrix(0, 0, 0, 0, 2.5, 2.5) };
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 0, 0), 1, 1, 1, 2, new Matrix(1, 0.5, 0.75, 1, 0, 0) };
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 0, 0), 1, 0, 1, 2, new Matrix(1, 0.5, 0, 0, 1.5, 2) };
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 0, 0), 0, 2, 1, 2, new Matrix(0, 0, 1.5, 2, -0.5, -1.5) };
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 0, 0), 1, 2, 1, 2, new Matrix(1, 0.5, 1.5, 2, -1.5, -2) };
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 0, 0), 1.1, 2.2, 1, 2, new Matrix(1.1, 0.55, 1.65, 2.2, -1.9, -2.45) };
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 0, 0), 2, 3, 1, 2, new Matrix(2, 1, 2.25, 3, -4, -4.5) };
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 0, 0), double.NaN, 3, 1, 2, new Matrix(double.NaN, double.NaN, 2.25, 3, double.NaN, double.NaN) };
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 0, 0), 2, double.NaN, 1, 2, new Matrix(2, 1, double.NaN, double.NaN, double.NaN, double.NaN) };
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 0, 0), double.NaN, double.NaN, 1, 2, new Matrix(double.NaN, double.NaN, double.NaN, double.NaN, double.NaN, double.NaN) };
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 0, 0), 2, 3, double.NaN, 2, new Matrix(2, 1, 2.25, 3, double.NaN, double.NaN) };
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 0, 0), 2, 3, 1, double.NaN, new Matrix(2, 1, 2.25, 3, double.NaN, double.NaN) };
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 0, 0), 2, 3, double.NaN, double.NaN, new Matrix(2, 1, 2.25, 3, double.NaN, double.NaN) };
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 0, 0), double.NaN, double.NaN, double.NaN, double.NaN, new Matrix(double.NaN, double.NaN, double.NaN, double.NaN, double.NaN, double.NaN) };

        // Translated.
        yield return new object[] { new Matrix(1, 0, 0, 1, 2, 3), -2, -3, 0, 0, new Matrix(-2, 0, 0, -3, 2, 3) };
        yield return new object[] { new Matrix(1, 0, 0, 1, 2, 3), -2, 3, 0, 0, new Matrix(-2, 0, 0, 3, 2, 3) };
        yield return new object[] { new Matrix(1, 0, 0, 1, 2, 3), 2, -3, 0, 0, new Matrix(2, 0, 0, -3, 2, 3) };
        yield return new object[] { new Matrix(1, 0, 0, 1, 2, 3), -1, -2, 0, 0, new Matrix(-1, 0, 0, -2, 2, 3) };
        yield return new object[] { new Matrix(1, 0, 0, 1, 2, 3), -1, 0, 0, 0, new Matrix(-1, 0, 0, 0, 2, 3) };
        yield return new object[] { new Matrix(1, 0, 0, 1, 2, 3), 0, -2, 0, 0, new Matrix(0, 0, 0, -2, 2, 3) };
        yield return new object[] { new Matrix(1, 0, 0, 1, 2, 3), 0, 0, 0, 0, new Matrix(0, 0, 0, 0, 2, 3) };
        yield return new object[] { new Matrix(1, 0, 0, 1, 2, 3), 1, 1, 0, 0, new Matrix(1, 0, 0, 1, 2, 3) };
        yield return new object[] { new Matrix(1, 0, 0, 1, 2, 3), 1, 0, 0, 0, new Matrix(1, 0, 0, 0, 2, 3) };
        yield return new object[] { new Matrix(1, 0, 0, 1, 2, 3), 0, 2, 0, 0, new Matrix(0, 0, 0, 2, 2, 3) };
        yield return new object[] { new Matrix(1, 0, 0, 1, 2, 3), 1, 2, 0, 0, new Matrix(1, 0, 0, 2, 2, 3) };
        yield return new object[] { new Matrix(1, 0, 0, 1, 2, 3), 1.1, 2.2, 0, 0, new Matrix(1.1, 0, 0, 2.2, 2, 3) };
        yield return new object[] { new Matrix(1, 0, 0, 1, 2, 3), 2, 3, 0, 0, new Matrix(2, 0, 0, 3, 2, 3) };
        yield return new object[] { new Matrix(1, 0, 0, 1, 2, 3), double.NaN, 3, 0, 0, new Matrix(double.NaN, 0, 0, 3, double.NaN, 3) };
        yield return new object[] { new Matrix(1, 0, 0, 1, 2, 3), 2, double.NaN, 0, 0, new Matrix(2, 0, 0, double.NaN, 2, double.NaN) };
        yield return new object[] { new Matrix(1, 0, 0, 1, 2, 3), double.NaN, double.NaN, 0, 0, new Matrix(double.NaN, 0, 0, double.NaN, double.NaN, double.NaN) };
        yield return new object[] { new Matrix(1, 0, 0, 1, 2, 3), -2, -3, 1, 2, new Matrix(-2, 0, 0, -3, 5, 11) };
        yield return new object[] { new Matrix(1, 0, 0, 1, 2, 3), -2, 3, 1, 2, new Matrix(-2, 0, 0, 3, 5, -1) };
        yield return new object[] { new Matrix(1, 0, 0, 1, 2, 3), 2, -3, 1, 2, new Matrix(2, 0, 0, -3, 1, 11) };
        yield return new object[] { new Matrix(1, 0, 0, 1, 2, 3), -1, -2, 1, 2, new Matrix(-1, 0, 0, -2, 4, 9) };
        yield return new object[] { new Matrix(1, 0, 0, 1, 2, 3), -1, 0, 1, 2, new Matrix(-1, 0, 0, 0, 4, 5) };
        yield return new object[] { new Matrix(1, 0, 0, 1, 2, 3), 0, -2, 1, 2, new Matrix(0, 0, 0, -2, 3, 9) };
        yield return new object[] { new Matrix(1, 0, 0, 1, 2, 3), 0, 0, 1, 2, new Matrix(0, 0, 0, 0, 3, 5) };
        yield return new object[] { new Matrix(1, 0, 0, 1, 2, 3), 1, 1, 1, 2, new Matrix(1, 0, 0, 1, 2, 3) };
        yield return new object[] { new Matrix(1, 0, 0, 1, 2, 3), 1, 0, 1, 2, new Matrix(1, 0, 0, 0, 2, 5) };
        yield return new object[] { new Matrix(1, 0, 0, 1, 2, 3), 0, 2, 1, 2, new Matrix(0, 0, 0, 2, 3, 1) };
        yield return new object[] { new Matrix(1, 0, 0, 1, 2, 3), 1, 2, 1, 2, new Matrix(1, 0, 0, 2, 2, 1) };
        yield return new object[] { new Matrix(1, 0, 0, 1, 2, 3), 1.1, 2.2, 1, 2, new Matrix(1.1, 0, 0, 2.2, 1.9, 0.6) };
        yield return new object[] { new Matrix(1, 0, 0, 1, 2, 3), 2, 3, 1, 2, new Matrix(2, 0, 0, 3, 1, -1) };
        yield return new object[] { new Matrix(1, 0, 0, 1, 2, 3), double.NaN, 3, 1, 2, new Matrix(double.NaN, 0, 0, 3, double.NaN, -1) };
        yield return new object[] { new Matrix(1, 0, 0, 1, 2, 3), 2, double.NaN, 1, 2, new Matrix(2, 0, 0, double.NaN, 1, double.NaN) };
        yield return new object[] { new Matrix(1, 0, 0, 1, 2, 3), double.NaN, double.NaN, 1, 2, new Matrix(double.NaN, 0, 0, double.NaN, double.NaN, double.NaN) };
        yield return new object[] { new Matrix(1, 0, 0, 1, 2, 3), 2, 3, double.NaN, 2, new Matrix(2, 0, 0, 3, double.NaN, -1) };
        yield return new object[] { new Matrix(1, 0, 0, 1, 2, 3), 2, 3, 1, double.NaN, new Matrix(2, 0, 0, 3, 1, double.NaN) };
        yield return new object[] { new Matrix(1, 0, 0, 1, 2, 3), 2, 3, double.NaN, double.NaN, new Matrix(2, 0, 0, 3, double.NaN, double.NaN) };
        yield return new object[] { new Matrix(1, 0, 0, 1, 2, 3), double.NaN, double.NaN, double.NaN, double.NaN, new Matrix(double.NaN, 0, 0, double.NaN, double.NaN, double.NaN) };

        // Translated and scaled.
        yield return new object[] { new Matrix(2, 0, 0, 3, 1, 2), -2, -3, 0, 0, new Matrix(-4, 0, 0, -9, 1, 2) };
        yield return new object[] { new Matrix(2, 0, 0, 3, 1, 2), -2, 3, 0, 0, new Matrix(-4, 0, 0, 9, 1, 2) };
        yield return new object[] { new Matrix(2, 0, 0, 3, 1, 2), 2, -3, 0, 0, new Matrix(4, 0, 0, -9, 1, 2) };
        yield return new object[] { new Matrix(2, 0, 0, 3, 1, 2), -1, -2, 0, 0, new Matrix(-2, 0, 0, -6, 1, 2) };
        yield return new object[] { new Matrix(2, 0, 0, 3, 1, 2), -1, 0, 0, 0, new Matrix(-2, 0, 0, 0, 1, 2) };
        yield return new object[] { new Matrix(2, 0, 0, 3, 1, 2), 0, -2, 0, 0, new Matrix(0, 0, 0, -6, 1, 2) };
        yield return new object[] { new Matrix(2, 0, 0, 3, 1, 2), 0, 0, 0, 0, new Matrix(0, 0, 0, 0, 1, 2) };
        yield return new object[] { new Matrix(2, 0, 0, 3, 1, 2), 1, 1, 0, 0, new Matrix(2, 0, 0, 3, 1, 2) };
        yield return new object[] { new Matrix(2, 0, 0, 3, 1, 2), 1, 0, 0, 0, new Matrix(2, 0, 0, 0, 1, 2) };
        yield return new object[] { new Matrix(2, 0, 0, 3, 1, 2), 0, 2, 0, 0, new Matrix(0, 0, 0, 6, 1, 2) };
        yield return new object[] { new Matrix(2, 0, 0, 3, 1, 2), 1, 2, 0, 0, new Matrix(2, 0, 0, 6, 1, 2) };
        yield return new object[] { new Matrix(2, 0, 0, 3, 1, 2), 1.1, 2.2, 0, 0, new Matrix(2.2, 0, 0, 6.6, 1, 2) };
        yield return new object[] { new Matrix(2, 0, 0, 3, 1, 2), 2, 3, 0, 0, new Matrix(4, 0, 0, 9, 1, 2) };
        yield return new object[] { new Matrix(2, 0, 0, 3, 1, 2), double.NaN, 3, 0, 0, new Matrix(double.NaN, 0, 0, 9, double.NaN, 2) };
        yield return new object[] { new Matrix(2, 0, 0, 3, 1, 2), 2, double.NaN, 0, 0, new Matrix(4, 0, 0, double.NaN, 1, double.NaN) };
        yield return new object[] { new Matrix(2, 0, 0, 3, 1, 2), double.NaN, double.NaN, 0, 0, new Matrix(double.NaN, 0, 0, double.NaN, double.NaN, double.NaN) };
        yield return new object[] { new Matrix(2, 0, 0, 3, 1, 2), -2, -3, 1, 2, new Matrix(-4, 0, 0, -9, 7, 26) };
        yield return new object[] { new Matrix(2, 0, 0, 3, 1, 2), -2, 3, 1, 2, new Matrix(-4, 0, 0, 9, 7, -10) };
        yield return new object[] { new Matrix(2, 0, 0, 3, 1, 2), 2, -3, 1, 2, new Matrix(4, 0, 0, -9, -1, 26) };
        yield return new object[] { new Matrix(2, 0, 0, 3, 1, 2), -1, -2, 1, 2, new Matrix(-2, 0, 0, -6, 5, 20) };
        yield return new object[] { new Matrix(2, 0, 0, 3, 1, 2), -1, 0, 1, 2, new Matrix(-2, 0, 0, 0, 5, 8) };
        yield return new object[] { new Matrix(2, 0, 0, 3, 1, 2), 0, -2, 1, 2, new Matrix(0, 0, 0, -6, 3, 20) };
        yield return new object[] { new Matrix(2, 0, 0, 3, 1, 2), 0, 0, 1, 2, new Matrix(0, 0, 0, 0, 3, 8) };
        yield return new object[] { new Matrix(2, 0, 0, 3, 1, 2), 1, 1, 1, 2, new Matrix(2, 0, 0, 3, 1, 2) };
        yield return new object[] { new Matrix(2, 0, 0, 3, 1, 2), 1, 0, 1, 2, new Matrix(2, 0, 0, 0, 1, 8) };
        yield return new object[] { new Matrix(2, 0, 0, 3, 1, 2), 0, 2, 1, 2, new Matrix(0, 0, 0, 6, 3, -4) };
        yield return new object[] { new Matrix(2, 0, 0, 3, 1, 2), 1, 2, 1, 2, new Matrix(2, 0, 0, 6, 1, -4) };
        yield return new object[] { new Matrix(2, 0, 0, 3, 1, 2), 1.1, 2.2, 1, 2, new Matrix(2.2, 0, 0, 6.6, 0.8, -5.2) };
        yield return new object[] { new Matrix(2, 0, 0, 3, 1, 2), 2, 3, 1, 2, new Matrix(4, 0, 0, 9, -1, -10) };
        yield return new object[] { new Matrix(2, 0, 0, 3, 1, 2), double.NaN, 3, 1, 2, new Matrix(double.NaN, 0, 0, 9, double.NaN, -10) };
        yield return new object[] { new Matrix(2, 0, 0, 3, 1, 2), 2, double.NaN, 1, 2, new Matrix(4, 0, 0, double.NaN, -1, double.NaN) };
        yield return new object[] { new Matrix(2, 0, 0, 3, 1, 2), double.NaN, double.NaN, 1, 2, new Matrix(double.NaN, 0, 0, double.NaN, double.NaN, double.NaN) };
        yield return new object[] { new Matrix(2, 0, 0, 3, 1, 2), 2, 3, double.NaN, 2, new Matrix(4, 0, 0, 9, double.NaN, -10) };
        yield return new object[] { new Matrix(2, 0, 0, 3, 1, 2), 2, 3, 1, double.NaN, new Matrix(4, 0, 0, 9, -1, double.NaN) };
        yield return new object[] { new Matrix(2, 0, 0, 3, 1, 2), 2, 3, double.NaN, double.NaN, new Matrix(4, 0, 0, 9, double.NaN, double.NaN) };
        yield return new object[] { new Matrix(2, 0, 0, 3, 1, 2), double.NaN, double.NaN, double.NaN, double.NaN, new Matrix(double.NaN, 0, 0, double.NaN, double.NaN, double.NaN) };

        // Translated and skewed.
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 2, 3), -2, -3, 0, 0, new Matrix(-2, -1, -2.25, -3, 2, 3) };
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 2, 3), -2, 3, 0, 0, new Matrix(-2, -1, 2.25, 3, 2, 3) };
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 2, 3), 2, -3, 0, 0, new Matrix(2, 1, -2.25, -3, 2, 3) };
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 2, 3), -1, -2, 0, 0, new Matrix(-1, -0.5, -1.5, -2, 2, 3) };
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 2, 3), -1, 0, 0, 0, new Matrix(-1, -0.5, 0, 0, 2, 3) };
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 2, 3), 0, -2, 0, 0, new Matrix(0, 0, -1.5, -2, 2, 3) };
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 2, 3), 0, 0, 0, 0, new Matrix(0, 0, 0, 0, 2, 3) };
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 2, 3), 1, 1, 0, 0, new Matrix(1, 0.5, 0.75, 1, 2, 3) };
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 2, 3), 1, 0, 0, 0, new Matrix(1, 0.5, 0, 0, 2, 3) };
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 2, 3), 0, 2, 0, 0, new Matrix(0, 0, 1.5, 2, 2, 3) };
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 2, 3), 1, 2, 0, 0, new Matrix(1, 0.5, 1.5, 2, 2, 3) };
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 2, 3), 1.1, 2.2, 0, 0, new Matrix(1.1, 0.55, 1.65, 2.2, 2, 3) };
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 2, 3), 2, 3, 0, 0, new Matrix(2, 1, 2.25, 3, 2, 3) };
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 2, 3), double.NaN, 3, 0, 0, new Matrix(double.NaN, double.NaN, 2.25, 3, double.NaN, double.NaN) };
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 2, 3), 2, double.NaN, 0, 0, new Matrix(2, 1, double.NaN, double.NaN, double.NaN, double.NaN) };
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 2, 3), double.NaN, double.NaN, 0, 0, new Matrix(double.NaN, double.NaN, double.NaN, double.NaN, double.NaN, double.NaN) };
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 2, 3), -2, -3, 1, 2, new Matrix(-2, -1, -2.25, -3, 11, 12.5) };
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 2, 3), -2, 3, 1, 2, new Matrix(-2, -1, 2.25, 3, 2, 0.5) };
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 2, 3), 2, -3, 1, 2, new Matrix(2, 1, -2.25, -3, 7, 10.5) };
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 2, 3), -1, -2, 1, 2, new Matrix(-1, -0.5, -1.5, -2, 8.5, 10) };
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 2, 3), -1, 0, 1, 2, new Matrix(-1, -0.5, 0, 0, 5.5, 6) };
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 2, 3), 0, -2, 1, 2, new Matrix(0, 0, -1.5, -2, 7.5, 9.5) };
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 2, 3), 0, 0, 1, 2, new Matrix(0, 0, 0, 0, 4.5, 5.5) };
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 2, 3), 1, 1, 1, 2, new Matrix(1, 0.5, 0.75, 1, 2, 3) };
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 2, 3), 1, 0, 1, 2, new Matrix(1, 0.5, 0, 0, 3.5, 5) };
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 2, 3), 0, 2, 1, 2, new Matrix(0, 0, 1.5, 2, 1.5, 1.5) };
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 2, 3), 1, 2, 1, 2, new Matrix(1, 0.5, 1.5, 2, 0.5, 1) };
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 2, 3), 1.1, 2.2, 1, 2, new Matrix(1.1, 0.55, 1.65, 2.2, 0.1, 0.55) };
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 2, 3), 2, 3, 1, 2, new Matrix(2, 1, 2.25, 3, -2, -1.5) };
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 2, 3), double.NaN, 3, 1, 2, new Matrix(double.NaN, double.NaN, 2.25, 3, double.NaN, double.NaN) };
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 2, 3), 2, double.NaN, 1, 2, new Matrix(2, 1, double.NaN, double.NaN, double.NaN, double.NaN) };
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 2, 3), double.NaN, double.NaN, 1, 2, new Matrix(double.NaN, double.NaN, double.NaN, double.NaN, double.NaN, double.NaN) };
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 2, 3), 2, 3, double.NaN, 2, new Matrix(2, 1, 2.25, 3, double.NaN, double.NaN) };
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 2, 3), 2, 3, 1, double.NaN, new Matrix(2, 1, 2.25, 3, double.NaN, double.NaN) };
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 2, 3), 2, 3, double.NaN, double.NaN, new Matrix(2, 1, 2.25, 3, double.NaN, double.NaN) };
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 2, 3), double.NaN, double.NaN, double.NaN, double.NaN, new Matrix(double.NaN, double.NaN, double.NaN, double.NaN, double.NaN, double.NaN) };

        // Skewed and scaled.
        yield return new object[] { new Matrix(2, 0.5, 0.75, 3, 0, 0), -2, -3, 0, 0, new Matrix(-4, -1, -2.25, -9, 0, 0) };
        yield return new object[] { new Matrix(2, 0.5, 0.75, 3, 0, 0), -2, 3, 0, 0, new Matrix(-4, -1, 2.25, 9, 0, 0) };
        yield return new object[] { new Matrix(2, 0.5, 0.75, 3, 0, 0), 2, -3, 0, 0, new Matrix(4, 1, -2.25, -9, 0, 0) };
        yield return new object[] { new Matrix(2, 0.5, 0.75, 3, 0, 0), -1, -2, 0, 0, new Matrix(-2, -0.5, -1.5, -6, 0, 0) };
        yield return new object[] { new Matrix(2, 0.5, 0.75, 3, 0, 0), -1, 0, 0, 0, new Matrix(-2, -0.5, 0, 0, 0, 0) };
        yield return new object[] { new Matrix(2, 0.5, 0.75, 3, 0, 0), 0, -2, 0, 0, new Matrix(0, 0, -1.5, -6, 0, 0) };
        yield return new object[] { new Matrix(2, 0.5, 0.75, 3, 0, 0), 0, 0, 0, 0, new Matrix(0, 0, 0, 0, 0, 0) };
        yield return new object[] { new Matrix(2, 0.5, 0.75, 3, 0, 0), 1, 1, 0, 0, new Matrix(2, 0.5, 0.75, 3, 0, 0) };
        yield return new object[] { new Matrix(2, 0.5, 0.75, 3, 0, 0), 1, 0, 0, 0, new Matrix(2, 0.5, 0, 0, 0, 0) };
        yield return new object[] { new Matrix(2, 0.5, 0.75, 3, 0, 0), 0, 2, 0, 0, new Matrix(0, 0, 1.5, 6, 0, 0) };
        yield return new object[] { new Matrix(2, 0.5, 0.75, 3, 0, 0), 1, 2, 0, 0, new Matrix(2, 0.5, 1.5, 6, 0, 0) };
        yield return new object[] { new Matrix(2, 0.5, 0.75, 3, 0, 0), 1.1, 2.2, 0, 0, new Matrix(2.2, 0.55, 1.65, 6.6, 0, 0) };
        yield return new object[] { new Matrix(2, 0.5, 0.75, 3, 0, 0), 2, 3, 0, 0, new Matrix(4, 1, 2.25, 9, 0, 0) };
        yield return new object[] { new Matrix(2, 0.5, 0.75, 3, 0, 0), double.NaN, 3, 0, 0, new Matrix(double.NaN, double.NaN, 2.25, 9, double.NaN, double.NaN) };
        yield return new object[] { new Matrix(2, 0.5, 0.75, 3, 0, 0), 2, double.NaN, 0, 0, new Matrix(4, 1, double.NaN, double.NaN, double.NaN, double.NaN) };
        yield return new object[] { new Matrix(2, 0.5, 0.75, 3, 0, 0), double.NaN, double.NaN, 0, 0, new Matrix(double.NaN, double.NaN, double.NaN, double.NaN, double.NaN, double.NaN) };
        yield return new object[] { new Matrix(2, 0.5, 0.75, 3, 0, 0), -2, -3, 1, 2, new Matrix(-4, -1, -2.25, -9, 12, 25.5) };
        yield return new object[] { new Matrix(2, 0.5, 0.75, 3, 0, 0), -2, 3, 1, 2, new Matrix(-4, -1, 2.25, 9, 3, -10.5) };
        yield return new object[] { new Matrix(2, 0.5, 0.75, 3, 0, 0), 2, -3, 1, 2, new Matrix(4, 1, -2.25, -9, 4, 23.5) };
        yield return new object[] { new Matrix(2, 0.5, 0.75, 3, 0, 0), -1, -2, 1, 2, new Matrix(-2, -0.5, -1.5, -6, 8.5, 19) };
        yield return new object[] { new Matrix(2, 0.5, 0.75, 3, 0, 0), -1, 0, 1, 2, new Matrix(-2, -0.5, 0, 0, 5.5, 7) };
        yield return new object[] { new Matrix(2, 0.5, 0.75, 3, 0, 0), 0, -2, 1, 2, new Matrix(0, 0, -1.5, -6, 6.5, 18.5) };
        yield return new object[] { new Matrix(2, 0.5, 0.75, 3, 0, 0), 0, 0, 1, 2, new Matrix(0, 0, 0, 0, 3.5, 6.5) };
        yield return new object[] { new Matrix(2, 0.5, 0.75, 3, 0, 0), 1, 1, 1, 2, new Matrix(2, 0.5, 0.75, 3, 0, 0) };
        yield return new object[] { new Matrix(2, 0.5, 0.75, 3, 0, 0), 1, 0, 1, 2, new Matrix(2, 0.5, 0, 0, 1.5, 6) };
        yield return new object[] { new Matrix(2, 0.5, 0.75, 3, 0, 0), 0, 2, 1, 2, new Matrix(0, 0, 1.5, 6, 0.5, -5.5) };
        yield return new object[] { new Matrix(2, 0.5, 0.75, 3, 0, 0), 1, 2, 1, 2, new Matrix(2, 0.5, 1.5, 6, -1.5, -6) };
        yield return new object[] { new Matrix(2, 0.5, 0.75, 3, 0, 0), 1.1, 2.2, 1, 2, new Matrix(2.2, 0.55, 1.65, 6.6, -2, -7.25) };
        yield return new object[] { new Matrix(2, 0.5, 0.75, 3, 0, 0), 2, 3, 1, 2, new Matrix(4, 1, 2.25, 9, -5, -12.5) };
        yield return new object[] { new Matrix(2, 0.5, 0.75, 3, 0, 0), double.NaN, 3, 1, 2, new Matrix(double.NaN, double.NaN, 2.25, 9, double.NaN, double.NaN) };
        yield return new object[] { new Matrix(2, 0.5, 0.75, 3, 0, 0), 2, double.NaN, 1, 2, new Matrix(4, 1, double.NaN, double.NaN, double.NaN, double.NaN) };
        yield return new object[] { new Matrix(2, 0.5, 0.75, 3, 0, 0), double.NaN, double.NaN, 1, 2, new Matrix(double.NaN, double.NaN, double.NaN, double.NaN, double.NaN, double.NaN) };
        yield return new object[] { new Matrix(2, 0.5, 0.75, 3, 0, 0), 2, 3, double.NaN, 2, new Matrix(4, 1, 2.25, 9, double.NaN, double.NaN) };
        yield return new object[] { new Matrix(2, 0.5, 0.75, 3, 0, 0), 2, 3, 1, double.NaN, new Matrix(4, 1, 2.25, 9, double.NaN, double.NaN) };
        yield return new object[] { new Matrix(2, 0.5, 0.75, 3, 0, 0), 2, 3, double.NaN, double.NaN, new Matrix(4, 1, 2.25, 9, double.NaN, double.NaN) };
        yield return new object[] { new Matrix(2, 0.5, 0.75, 3, 0, 0), double.NaN, double.NaN, double.NaN, double.NaN, new Matrix(double.NaN, double.NaN, double.NaN, double.NaN, double.NaN, double.NaN) };

        // Complex.
        yield return new object[] { new Matrix(2, 3, 4, 5, 6, 7), -2, -3, 0, 0, new Matrix(-4, -6, -12, -15, 6, 7) };
        yield return new object[] { new Matrix(2, 3, 4, 5, 6, 7), -2, 3, 0, 0, new Matrix(-4, -6, 12, 15, 6, 7) };
        yield return new object[] { new Matrix(2, 3, 4, 5, 6, 7), 2, -3, 0, 0, new Matrix(4, 6, -12, -15, 6, 7) };
        yield return new object[] { new Matrix(2, 3, 4, 5, 6, 7), -1, -2, 0, 0, new Matrix(-2, -3, -8, -10, 6, 7) };
        yield return new object[] { new Matrix(2, 3, 4, 5, 6, 7), -1, 0, 0, 0, new Matrix(-2, -3, 0, 0, 6, 7) };
        yield return new object[] { new Matrix(2, 3, 4, 5, 6, 7), 0, -2, 0, 0, new Matrix(0, 0, -8, -10, 6, 7) };
        yield return new object[] { new Matrix(2, 3, 4, 5, 6, 7), 0, 0, 0, 0, new Matrix(0, 0, 0, 0, 6, 7) };
        yield return new object[] { new Matrix(2, 3, 4, 5, 6, 7), 1, 1, 0, 0, new Matrix(2, 3, 4, 5, 6, 7) };
        yield return new object[] { new Matrix(2, 3, 4, 5, 6, 7), 1, 0, 0, 0, new Matrix(2, 3, 0, 0, 6, 7) };
        yield return new object[] { new Matrix(2, 3, 4, 5, 6, 7), 0, 2, 0, 0, new Matrix(0, 0, 8, 10, 6, 7) };
        yield return new object[] { new Matrix(2, 3, 4, 5, 6, 7), 1, 2, 0, 0, new Matrix(2, 3, 8, 10, 6, 7) };
        yield return new object[] { new Matrix(2, 3, 4, 5, 6, 7), 1.1, 2.2, 0, 0, new Matrix(2.2, 3.3, 8.8, 11, 6, 7) };
        yield return new object[] { new Matrix(2, 3, 4, 5, 6, 7), 2, 3, 0, 0, new Matrix(4, 6, 12, 15, 6, 7) };
        yield return new object[] { new Matrix(2, 3, 4, 5, 6, 7), double.NaN, 3, 0, 0, new Matrix(double.NaN, double.NaN, 12, 15, double.NaN, double.NaN) };
        yield return new object[] { new Matrix(2, 3, 4, 5, 6, 7), 2, double.NaN, 0, 0, new Matrix(4, 6, double.NaN, double.NaN, double.NaN, double.NaN) };
        yield return new object[] { new Matrix(2, 3, 4, 5, 6, 7), double.NaN, double.NaN, 0, 0, new Matrix(double.NaN, double.NaN, double.NaN, double.NaN, double.NaN, double.NaN) };
        yield return new object[] { new Matrix(2, 3, 4, 5, 6, 7), -2, -3, 1, 2, new Matrix(-4, -6, -12, -15, 44, 56) };
        yield return new object[] { new Matrix(2, 3, 4, 5, 6, 7), -2, 3, 1, 2, new Matrix(-4, -6, 12, 15, -4, -4) };
        yield return new object[] { new Matrix(2, 3, 4, 5, 6, 7), 2, -3, 1, 2, new Matrix(4, 6, -12, -15, 36, 44) };
        yield return new object[] { new Matrix(2, 3, 4, 5, 6, 7), -1, -2, 1, 2, new Matrix(-2, -3, -8, -10, 34, 43) };
        yield return new object[] { new Matrix(2, 3, 4, 5, 6, 7), -1, 0, 1, 2, new Matrix(-2, -3, 0, 0, 18, 23) };
        yield return new object[] { new Matrix(2, 3, 4, 5, 6, 7), 0, -2, 1, 2, new Matrix(0, 0, -8, -10, 32, 40) };
        yield return new object[] { new Matrix(2, 3, 4, 5, 6, 7), 0, 0, 1, 2, new Matrix(0, 0, 0, 0, 16, 20) };
        yield return new object[] { new Matrix(2, 3, 4, 5, 6, 7), 1, 1, 1, 2, new Matrix(2, 3, 4, 5, 6, 7) };
        yield return new object[] { new Matrix(2, 3, 4, 5, 6, 7), 1, 0, 1, 2, new Matrix(2, 3, 0, 0, 14, 17) };
        yield return new object[] { new Matrix(2, 3, 4, 5, 6, 7), 0, 2, 1, 2, new Matrix(0, 0, 8, 10, 0, 0) };
        yield return new object[] { new Matrix(2, 3, 4, 5, 6, 7), 1, 2, 1, 2, new Matrix(2, 3, 8, 10, -2, -3) };
        yield return new object[] { new Matrix(2, 3, 4, 5, 6, 7), 1.1, 2.2, 1, 2, new Matrix(2.2, 3.3, 8.8, 11, -3.8, -5.3) };
        yield return new object[] { new Matrix(2, 3, 4, 5, 6, 7), 2, 3, 1, 2, new Matrix(4, 6, 12, 15, -12, -16) };
        yield return new object[] { new Matrix(2, 3, 4, 5, 6, 7), double.NaN, 3, 1, 2, new Matrix(double.NaN, double.NaN, 12, 15, double.NaN, double.NaN) };
        yield return new object[] { new Matrix(2, 3, 4, 5, 6, 7), 2, double.NaN, 1, 2, new Matrix(4, 6, double.NaN, double.NaN, double.NaN, double.NaN) };
        yield return new object[] { new Matrix(2, 3, 4, 5, 6, 7), double.NaN, double.NaN, 1, 2, new Matrix(double.NaN, double.NaN, double.NaN, double.NaN, double.NaN, double.NaN) };
        yield return new object[] { new Matrix(2, 3, 4, 5, 6, 7), 2, 3, double.NaN, 2, new Matrix(4, 6, 12, 15, double.NaN, double.NaN) };
        yield return new object[] { new Matrix(2, 3, 4, 5, 6, 7), 2, 3, 1, double.NaN, new Matrix(4, 6, 12, 15, double.NaN, double.NaN) };
        yield return new object[] { new Matrix(2, 3, 4, 5, 6, 7), 2, 3, double.NaN, double.NaN, new Matrix(4, 6, 12, 15, double.NaN, double.NaN) };
        yield return new object[] { new Matrix(2, 3, 4, 5, 6, 7), double.NaN, double.NaN, double.NaN, double.NaN, new Matrix(double.NaN, double.NaN, double.NaN, double.NaN, double.NaN, double.NaN) };

        // No inverse.
        yield return new object[] { new Matrix(0, 0, 0, 0, 0, 0), -2, -3, 0, 0, new Matrix(-0, 0, 0, -0, 0, 0) };
        yield return new object[] { new Matrix(0, 0, 0, 0, 0, 0), -2, 3, 0, 0, new Matrix(-0, 0, 0, 0, 0, 0) };
        yield return new object[] { new Matrix(0, 0, 0, 0, 0, 0), 2, -3, 0, 0, new Matrix(0, 0, 0, -0, 0, 0) };
        yield return new object[] { new Matrix(0, 0, 0, 0, 0, 0), -1, -2, 0, 0, new Matrix(-0, 0, 0, -0, 0, 0) };
        yield return new object[] { new Matrix(0, 0, 0, 0, 0, 0), -1, 0, 0, 0, new Matrix(-0, 0, 0, 0, 0, 0) };
        yield return new object[] { new Matrix(0, 0, 0, 0, 0, 0), 0, -2, 0, 0, new Matrix(0, 0, 0, -0, 0, 0) };
        yield return new object[] { new Matrix(0, 0, 0, 0, 0, 0), 0, 0, 0, 0, new Matrix(0, 0, 0, 0, 0, 0) };
        yield return new object[] { new Matrix(0, 0, 0, 0, 0, 0), 1, 1, 0, 0, new Matrix(0, 0, 0, 0, 0, 0) };
        yield return new object[] { new Matrix(0, 0, 0, 0, 0, 0), 1, 0, 0, 0, new Matrix(0, 0, 0, 0, 0, 0) };
        yield return new object[] { new Matrix(0, 0, 0, 0, 0, 0), 0, 2, 0, 0, new Matrix(0, 0, 0, 0, 0, 0) };
        yield return new object[] { new Matrix(0, 0, 0, 0, 0, 0), 1, 2, 0, 0, new Matrix(0, 0, 0, 0, 0, 0) };
        yield return new object[] { new Matrix(0, 0, 0, 0, 0, 0), 1.1, 2.2, 0, 0, new Matrix(0, 0, 0, 0, 0, 0) };
        yield return new object[] { new Matrix(0, 0, 0, 0, 0, 0), 2, 3, 0, 0, new Matrix(0, 0, 0, 0, 0, 0) };
        yield return new object[] { new Matrix(0, 0, 0, 0, 0, 0), double.NaN, 3, 0, 0, new Matrix(double.NaN, 0, 0, 0, double.NaN, 0) };
        yield return new object[] { new Matrix(0, 0, 0, 0, 0, 0), 2, double.NaN, 0, 0, new Matrix(0, 0, 0, double.NaN, 0, double.NaN) };
        yield return new object[] { new Matrix(0, 0, 0, 0, 0, 0), double.NaN, double.NaN, 0, 0, new Matrix(double.NaN, 0, 0, double.NaN, double.NaN, double.NaN) };
        yield return new object[] { new Matrix(0, 0, 0, 0, 0, 0), -2, -3, 1, 2, new Matrix(-0, 0, 0, -0, 0, 0) };
        yield return new object[] { new Matrix(0, 0, 0, 0, 0, 0), -2, 3, 1, 2, new Matrix(-0, 0, 0, 0, 0, -0) };
        yield return new object[] { new Matrix(0, 0, 0, 0, 0, 0), 2, -3, 1, 2, new Matrix(0, 0, 0, -0, -0, 0) };
        yield return new object[] { new Matrix(0, 0, 0, 0, 0, 0), -1, -2, 1, 2, new Matrix(-0, 0, 0, -0, 0, 0) };
        yield return new object[] { new Matrix(0, 0, 0, 0, 0, 0), -1, 0, 1, 2, new Matrix(-0, 0, 0, 0, 0, 0) };
        yield return new object[] { new Matrix(0, 0, 0, 0, 0, 0), 0, -2, 1, 2, new Matrix(0, 0, 0, -0, 0, 0) };
        yield return new object[] { new Matrix(0, 0, 0, 0, 0, 0), 0, 0, 1, 2, new Matrix(0, 0, 0, 0, 0, 0) };
        yield return new object[] { new Matrix(0, 0, 0, 0, 0, 0), 1, 1, 1, 2, new Matrix(0, 0, 0, 0, 0, 0) };
        yield return new object[] { new Matrix(0, 0, 0, 0, 0, 0), 1, 0, 1, 2, new Matrix(0, 0, 0, 0, 0, 0) };
        yield return new object[] { new Matrix(0, 0, 0, 0, 0, 0), 0, 2, 1, 2, new Matrix(0, 0, 0, 0, 0, -0) };
        yield return new object[] { new Matrix(0, 0, 0, 0, 0, 0), 1, 2, 1, 2, new Matrix(0, 0, 0, 0, 0, -0) };
        yield return new object[] { new Matrix(0, 0, 0, 0, 0, 0), 1.1, 2.2, 1, 2, new Matrix(0, 0, 0, 0, -0, -0) };
        yield return new object[] { new Matrix(0, 0, 0, 0, 0, 0), 2, 3, 1, 2, new Matrix(0, 0, 0, 0, -0, -0) };
        yield return new object[] { new Matrix(0, 0, 0, 0, 0, 0), double.NaN, 3, 1, 2, new Matrix(double.NaN, 0, 0, 0, double.NaN, -0) };
        yield return new object[] { new Matrix(0, 0, 0, 0, 0, 0), 2, double.NaN, 1, 2, new Matrix(0, 0, 0, double.NaN, -0, double.NaN) };
        yield return new object[] { new Matrix(0, 0, 0, 0, 0, 0), double.NaN, double.NaN, 1, 2, new Matrix(double.NaN, 0, 0, double.NaN, double.NaN, double.NaN) };
        yield return new object[] { new Matrix(0, 0, 0, 0, 0, 0), 2, 3, double.NaN, 2, new Matrix(0, 0, 0, 0, double.NaN, -0) };
        yield return new object[] { new Matrix(0, 0, 0, 0, 0, 0), 2, 3, 1, double.NaN, new Matrix(0, 0, 0, 0, -0, double.NaN) };
        yield return new object[] { new Matrix(0, 0, 0, 0, 0, 0), 2, 3, double.NaN, double.NaN, new Matrix(0, 0, 0, 0, double.NaN, double.NaN) };
        yield return new object[] { new Matrix(0, 0, 0, 0, 0, 0), double.NaN, double.NaN, double.NaN, double.NaN, new Matrix(double.NaN, 0, 0, double.NaN, double.NaN, double.NaN) };
    }
    
    [Theory]
    [MemberData(nameof(ScaleAtPrepend_TestData))]
    public void ScaleAtPrepend_Invoke_Success(Matrix matrix, double scaleX, double scaleY, double centerX, double centerY, Matrix expected)
    {
        matrix.ScaleAtPrepend(scaleX, scaleY, centerX, centerY);
        Helpers.AssertEqualRounded(expected, matrix);
        Assert.Equal(expected.IsIdentity, matrix.IsIdentity);
        Assert.Equal(expected.HasInverse, matrix.HasInverse);
        Assert.Equal(expected.Determinant, matrix.Determinant, precision: 5);
    }

    public static IEnumerable<object[]> SetIdentity_TestData()
    {
        // Identity
        foreach (Matrix matrix in IdentityMatrices())
        {
            yield return new object[] { matrix };
        }

        // Scaled.
        yield return new object[] { new Matrix(2, 0, 0, 3, 0, 0) };

        // Skewed.
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 0, 0) };

        // Translated.
        yield return new object[] { new Matrix(1, 0, 0, 1, 2, 3) };

        // Translated and scaled.
        yield return new object[] { new Matrix(2, 0, 0, 3, 1, 2) };

        // Translated and skewed.
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 2, 3) };

        // Skewed and scaled.
        yield return new object[] { new Matrix(2, 0.5, 0.75, 3, 0, 0) };

        // Complex.
        yield return new object[] { new Matrix(2, 3, 4, 5, 6, 7) };

        // No inverse.
        yield return new object[] { new Matrix(0, 0, 0, 0, 0, 0) };
    }

    [Theory]
    [MemberData(nameof(SetIdentity_TestData))]
    public void SetIdentity_Invoke_Success(Matrix matrix)
    {
        matrix.SetIdentity();
        Assert.Equal(Matrix.Identity, matrix);
        Assert.Equal(1, matrix.M11);
        Assert.Equal(0, matrix.M12);
        Assert.Equal(0, matrix.M21);
        Assert.Equal(1, matrix.M22);
        Assert.Equal(0, matrix.OffsetX);
        Assert.Equal(0, matrix.OffsetY);
        Assert.True(matrix.HasInverse);
        Assert.True(matrix.IsIdentity);
        Assert.Equal(1, matrix.Determinant);
        Assert.Equal(new Matrix(), matrix);
    }

    public static IEnumerable<object[]> Skew_TestData()
    {
        // Identity
        foreach (Matrix matrix in IdentityMatrices())
        {
            yield return new object[] { matrix, -90, -180, new Matrix(1, 0, -16331239353195370, 1, 0, 0), false, true, 3 };
            yield return new object[] { matrix, -90, 180, new Matrix(1, -0, -16331239353195370, 1, 0, 0), false, true, -1 };
            yield return new object[] { matrix, 90, -180, new Matrix(1, 0, 16331239353195370, 1, 0, 0), false, true, -1 };
            yield return new object[] { matrix, -90, 0, new Matrix(1, 0, -16331239353195370, 1, 0, 0), false, true, 1 };
            yield return new object[] { matrix, 0, -180, new Matrix(1, 0, 0, 1, 0, 0), false, true, 1 };
            yield return new object[] { matrix, 0, 0, new Matrix(1, 0, 0, 1, 0, 0), true, true, 1 };
            yield return new object[] { matrix, 90, 180, new Matrix(1, -0, 16331239353195370, 1, 0, 0), false, true, 3 };
            yield return new object[] { matrix, 90, 0, new Matrix(1, 0, 16331239353195370, 1, 0, 0), false, true, 1 };
            yield return new object[] { matrix, 180, 0, new Matrix(1, 0, -0, 1, 0, 0), false, true, 1 };
            yield return new object[] { matrix, 0, 180, new Matrix(1, -0, 0, 1, 0, 0), false, true, 1 };
            yield return new object[] { matrix, 180, 180, new Matrix(1, -0, -0, 1, 0, 0), false, true, 1 };
            yield return new object[] { matrix, 270, 270, new Matrix(1, 5443746451065123, 5443746451065123, 1, 0, 0), false, true, -2.963437542348412E+31 };
            yield return new object[] { matrix, 180, 270, new Matrix(1, 5443746451065123, -0, 1, 0, 0), false, true, 1.66667 };
            yield return new object[] { matrix, 270, 270, new Matrix(1, 5443746451065123, 5443746451065123, 1, 0, 0), false, true, -2.963437542348412E+31 };
            yield return new object[] { matrix, -20, -30, new Matrix(1, -0.57735, -0.36397, 1, 0, 0), false, true, 0.78986 };
            yield return new object[] { matrix, 20, 30, new Matrix(1, 0.57735, 0.36397, 1, 0, 0), false, true, 0.78986 };
            yield return new object[] { matrix, -380, -390, new Matrix(1, -0.57735, -0.36397, 1, 0, 0), false, true, 0.78986 };
            yield return new object[] { matrix, 380, 390, new Matrix(1, 0.57735, 0.36397, 1, 0, 0), false, true, 0.78986 };
            yield return new object[] { matrix, -740, -750, new Matrix(1, -0.57735, -0.36397, 1, 0, 0), false, true, 0.78986 };
            yield return new object[] { matrix, 740, 750, new Matrix(1, 0.57735, 0.36397, 1, 0, 0), false, true, 0.78986 };
            yield return new object[] { matrix, 35, 0, new Matrix(1, 0, 0.70021, 1, 0, 0), false, true, 1 };
            yield return new object[] { matrix, 0, 35, new Matrix(1, 0.70021, 0, 1, 0, 0), false, true, 1 };
            yield return new object[] { matrix, -720, -720, new Matrix(1, -0, -0, 1, 0, 0), true, true, 1 };
            yield return new object[] { matrix, -360, -360, new Matrix(1, -0, -0, 1, 0, 0), true, true, 1 };
            yield return new object[] { matrix, 360, 360, new Matrix(1, 0, 0, 1, 0, 0), true, true, 1 };
            yield return new object[] { matrix, 720, 720, new Matrix(1, 0, 0, 1, 0, 0), true, true, 1 };
            yield return new object[] { matrix, 720, 720, new Matrix(1, 0, 0, 1, 0, 0), true, true, 1 };
            yield return new object[] { matrix, 20, double.NaN, new Matrix(1, double.NaN, 0.36397, 1, 0, 0), false, true, double.NaN };
            yield return new object[] { matrix, double.NaN, 30, new Matrix(1, 0.57735, double.NaN, 1, 0, 0), false, true, double.NaN };
            yield return new object[] { matrix, double.NaN, double.NaN, new Matrix(1, double.NaN, double.NaN, 1, 0, 0), false, true, double.NaN };
        }

        // Scaled.
        yield return new object[] { new Matrix(2, 0, 0, 3, 0, 0), -90, -180, new Matrix(2, 0, -48993718059586110, 3, 0, 0), false, true, 18 };
        yield return new object[] { new Matrix(2, 0, 0, 3, 0, 0), -90, 180, new Matrix(2, -0, -48993718059586110, 3, 0, 0), false, true, -6 };
        yield return new object[] { new Matrix(2, 0, 0, 3, 0, 0), 90, -180, new Matrix(2, 0, 48993718059586110, 3, 0, 0), false, true, -6 };
        yield return new object[] { new Matrix(2, 0, 0, 3, 0, 0), -90, 0, new Matrix(2, 0, -48993718059586110, 3, 0, 0), false, true, 6 };
        yield return new object[] { new Matrix(2, 0, 0, 3, 0, 0), 0, -180, new Matrix(2, 0, 0, 3, 0, 0), false, true, 6 };
        yield return new object[] { new Matrix(2, 0, 0, 3, 0, 0), 0, 0, new Matrix(2, 0, 0, 3, 0, 0), false, true, 6 };
        yield return new object[] { new Matrix(2, 0, 0, 3, 0, 0), 90, 180, new Matrix(2, -0, 48993718059586110, 3, 0, 0), false, true, 18 };
        yield return new object[] { new Matrix(2, 0, 0, 3, 0, 0), 90, 0, new Matrix(2, 0, 48993718059586110, 3, 0, 0), false, true, 6 };
        yield return new object[] { new Matrix(2, 0, 0, 3, 0, 0), 180, 0, new Matrix(2, 0, -0, 3, 0, 0), false, true, 6 };
        yield return new object[] { new Matrix(2, 0, 0, 3, 0, 0), 0, 180, new Matrix(2, -0, 0, 3, 0, 0), false, true, 6 };
        yield return new object[] { new Matrix(2, 0, 0, 3, 0, 0), 180, 180, new Matrix(2, -0, -0, 3, 0, 0), false, true, 6 };
        yield return new object[] { new Matrix(2, 0, 0, 3, 0, 0), 270, 270, new Matrix(2, 10887492902130246, 16331239353195366, 3, 0, 0), false, true, -1.7780625254090473E+32 };
        yield return new object[] { new Matrix(2, 0, 0, 3, 0, 0), 180, 270, new Matrix(2, 10887492902130246, -0, 3, 0, 0), false, true, 10 };
        yield return new object[] { new Matrix(2, 0, 0, 3, 0, 0), 270, 270, new Matrix(2, 10887492902130246, 16331239353195366, 3, 0, 0), false, true, -1.7780625254090473E+32 };
        yield return new object[] { new Matrix(2, 0, 0, 3, 0, 0), -20, -30, new Matrix(2, -1.1547, -1.09191, 3, 0, 0), false, true, 4.73917 };
        yield return new object[] { new Matrix(2, 0, 0, 3, 0, 0), 20, 30, new Matrix(2, 1.1547, 1.09191, 3, 0, 0), false, true, 4.73917 };
        yield return new object[] { new Matrix(2, 0, 0, 3, 0, 0), -380, -390, new Matrix(2, -1.1547, -1.09191, 3, 0, 0), false, true, 4.73917 };
        yield return new object[] { new Matrix(2, 0, 0, 3, 0, 0), 380, 390, new Matrix(2, 1.1547, 1.09191, 3, 0, 0), false, true, 4.73917 };
        yield return new object[] { new Matrix(2, 0, 0, 3, 0, 0), -740, -750, new Matrix(2, -1.1547, -1.09191, 3, 0, 0), false, true, 4.73917 };
        yield return new object[] { new Matrix(2, 0, 0, 3, 0, 0), 740, 750, new Matrix(2, 1.1547, 1.09191, 3, 0, 0), false, true, 4.73917 };
        yield return new object[] { new Matrix(2, 0, 0, 3, 0, 0), 35, 0, new Matrix(2, 0, 2.10062, 3, 0, 0), false, true, 6 };
        yield return new object[] { new Matrix(2, 0, 0, 3, 0, 0), 0, 35, new Matrix(2, 1.40042, 0, 3, 0, 0), false, true, 6 };
        yield return new object[] { new Matrix(2, 0, 0, 3, 0, 0), -720, -720, new Matrix(2, 0, 0, 3, 0, 0), false, true, 6 };
        yield return new object[] { new Matrix(2, 0, 0, 3, 0, 0), -360, -360, new Matrix(2, 0, 0, 3, 0, 0), false, true, 6 };
        yield return new object[] { new Matrix(2, 0, 0, 3, 0, 0), 360, 360, new Matrix(2, 0, 0, 3, 0, 0), false, true, 6 };
        yield return new object[] { new Matrix(2, 0, 0, 3, 0, 0), 720, 720, new Matrix(2, 0, 0, 3, 0, 0), false, true, 6 };
        yield return new object[] { new Matrix(2, 0, 0, 3, 0, 0), 720, 720, new Matrix(2, 0, 0, 3, 0, 0), false, true, 6 };
        yield return new object[] { new Matrix(2, 0, 0, 3, 0, 0), 20, double.NaN, new Matrix(2, double.NaN, 1.09191, double.NaN, 0, double.NaN), false, true, double.NaN };
        yield return new object[] { new Matrix(2, 0, 0, 3, 0, 0), double.NaN, 30, new Matrix(double.NaN, 1.1547, double.NaN, 3, double.NaN, 0), false, true, double.NaN };
        yield return new object[] { new Matrix(2, 0, 0, 3, 0, 0), double.NaN, double.NaN, new Matrix(double.NaN, double.NaN, double.NaN, double.NaN, double.NaN, double.NaN), false, true, double.NaN };

        // Skewed.
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 0, 0), -90, -180, new Matrix(-8165619676597683, 0.5, -16331239353195370, 1, 0, 0), false, true, 3 };
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 0, 0), -90, 180, new Matrix(-8165619676597683, 0.5, -16331239353195370, 1, 0, 0), false, false, 0 };
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 0, 0), 90, -180, new Matrix(8165619676597686, 0.5, 16331239353195370, 1, 0, 0), false, true, -1 };
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 0, 0), -90, 0, new Matrix(-8165619676597683, 0.5, -16331239353195370, 1, 0, 0), false, true, 1 };
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 0, 0), 0, -180, new Matrix(1, 0.5, 0.75, 1, 0, 0), false, true, 0.625 };
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 0, 0), 0, 0, new Matrix(1, 0.5, 0.75, 1, 0, 0), false, true, 0.625 };
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 0, 0), 90, 180, new Matrix(8165619676597686, 0.5, 16331239353195370, 1, 0, 0), false, true, 2 };
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 0, 0), 90, 0, new Matrix(8165619676597686, 0.5, 16331239353195370, 1, 0, 0), false, true, 1 };
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 0, 0), 180, 0, new Matrix(1, 0.5, 0.75, 1, 0, 0), false, true, 0.625 };
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 0, 0), 0, 180, new Matrix(1, 0.5, 0.75, 1, 0, 0), false, true, 0.625 };
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 0, 0), 180, 180, new Matrix(1, 0.5, 0.75, 1, 0, 0), false, true, 0.625 };
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 0, 0), 270, 270, new Matrix(2721873225532562.5, 5443746451065124, 5443746451065124, 4082809838298843, 0, 0), false, true, -1.8521484639677582E+31 };
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 0, 0), 180, 270, new Matrix(1, 5443746451065124, 0.75, 4082809838298843, 0, 0), false, false, 0 };
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 0, 0), 270, 270, new Matrix(2721873225532562.5, 5443746451065124, 5443746451065124, 4082809838298843, 0, 0), false, true, -1.8521484639677582E+31 };
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 0, 0), -20, -30, new Matrix(0.81801, -0.07735, 0.38603, 0.56699, 0, 0), false, true, 0.49366 };
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 0, 0), 20, 30, new Matrix(1.18199, 1.07735, 1.11396, 1.43301, 0, 0), false, true, 0.49366 };
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 0, 0), -380, -390, new Matrix(0.81801, -0.07735, 0.38603, 0.56699, 0, 0), false, true, 0.49366 };
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 0, 0), 380, 390, new Matrix(1.18199, 1.07735, 1.11396, 1.43301, 0, 0), false, true, 0.49366 };
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 0, 0), -740, -750, new Matrix(0.81801, -0.07735, 0.38603, 0.56699, 0, 0), false, true, 0.49366 };
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 0, 0), 740, 750, new Matrix(1.18199, 1.07735, 1.11396, 1.43301, 0, 0), false, true, 0.49366 };
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 0, 0), 35, 0, new Matrix(1.3501, 0.5, 1.45021, 1, 0, 0), false, true, 0.625 };
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 0, 0), 0, 35, new Matrix(1, 1.20021, 0.75, 1.52516, 0, 0), false, true, 0.625 };
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 0, 0), -720, -720, new Matrix(1, 0.5, 0.75, 1, 0, 0), false, true, 0.625 };
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 0, 0), -360, -360, new Matrix(1, 0.5, 0.75, 1, 0, 0), false, true, 0.625 };
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 0, 0), 360, 360, new Matrix(1, 0.5, 0.75, 1, 0, 0), false, true, 0.625 };
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 0, 0), 720, 720, new Matrix(1, 0.5, 0.75, 1, 0, 0), false, true, 0.625 };
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 0, 0), 720, 720, new Matrix(1, 0.5, 0.75, 1, 0, 0), false, true, 0.625 };
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 0, 0), 20, double.NaN, new Matrix(1.18199, double.NaN, 1.11396, double.NaN, 0, double.NaN), false, true, double.NaN };
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 0, 0), double.NaN, 30, new Matrix(double.NaN, 1.07735, double.NaN, 1.43301, double.NaN, 0), false, true, double.NaN };
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 0, 0), double.NaN, double.NaN, new Matrix(double.NaN, double.NaN, double.NaN, double.NaN, double.NaN, double.NaN), false, true, double.NaN };

        // Translated.
        yield return new object[] { new Matrix(1, 0, 0, 1, 2, 3), -90, -180, new Matrix(1, 0, -16331239353195370, 1, -48993718059586110, 3), false, true, 3 };
        yield return new object[] { new Matrix(1, 0, 0, 1, 2, 3), -90, 180, new Matrix(1, -0, -16331239353195370, 1, -48993718059586110, 3), false, true, -1 };
        yield return new object[] { new Matrix(1, 0, 0, 1, 2, 3), 90, -180, new Matrix(1, 0, 16331239353195370, 1, 48993718059586110, 3), false, true, -1 };
        yield return new object[] { new Matrix(1, 0, 0, 1, 2, 3), -90, 0, new Matrix(1, 0, -16331239353195370, 1, -48993718059586110, 3), false, true, 1 };
        yield return new object[] { new Matrix(1, 0, 0, 1, 2, 3), 0, -180, new Matrix(1, 0, 0, 1, 2, 3), false, true, 1 };
        yield return new object[] { new Matrix(1, 0, 0, 1, 2, 3), 0, 0, new Matrix(1, 0, 0, 1, 2, 3), false, true, 1 };
        yield return new object[] { new Matrix(1, 0, 0, 1, 2, 3), 90, 180, new Matrix(1, -0, 16331239353195370, 1, 48993718059586110, 3), false, true, 3 };
        yield return new object[] { new Matrix(1, 0, 0, 1, 2, 3), 90, 0, new Matrix(1, 0, 16331239353195370, 1, 48993718059586110, 3), false, true, 1 };
        yield return new object[] { new Matrix(1, 0, 0, 1, 2, 3), 180, 0, new Matrix(1, 0, -0, 1, 2, 3), false, true, 1 };
        yield return new object[] { new Matrix(1, 0, 0, 1, 2, 3), 0, 180, new Matrix(1, -0, 0, 1, 2, 3), false, true, 1 };
        yield return new object[] { new Matrix(1, 0, 0, 1, 2, 3), 180, 180, new Matrix(1, -0, -0, 1, 2, 3), false, true, 1 };
        yield return new object[] { new Matrix(1, 0, 0, 1, 2, 3), 270, 270, new Matrix(1, 5443746451065123, 5443746451065123, 1, 16331239353195370, 10887492902130248), false, true, -2.963437542348412E+31 };
        yield return new object[] { new Matrix(1, 0, 0, 1, 2, 3), 180, 270, new Matrix(1, 5443746451065123, -0, 1, 2, 10887492902130248), false, true, 1.66667 };
        yield return new object[] { new Matrix(1, 0, 0, 1, 2, 3), 270, 270, new Matrix(1, 5443746451065123, 5443746451065123, 1, 16331239353195370, 10887492902130248), false, true, -2.963437542348412E+31 };
        yield return new object[] { new Matrix(1, 0, 0, 1, 2, 3), -20, -30, new Matrix(1, -0.57735, -0.36397, 1, 0.90809, 1.8453), false, true, 0.78986 };
        yield return new object[] { new Matrix(1, 0, 0, 1, 2, 3), 20, 30, new Matrix(1, 0.57735, 0.36397, 1, 3.09191, 4.1547), false, true, 0.78986 };
        yield return new object[] { new Matrix(1, 0, 0, 1, 2, 3), -380, -390, new Matrix(1, -0.57735, -0.36397, 1, 0.90809, 1.8453), false, true, 0.78986 };
        yield return new object[] { new Matrix(1, 0, 0, 1, 2, 3), 380, 390, new Matrix(1, 0.57735, 0.36397, 1, 3.09191, 4.1547), false, true, 0.78986 };
        yield return new object[] { new Matrix(1, 0, 0, 1, 2, 3), -740, -750, new Matrix(1, -0.57735, -0.36397, 1, 0.90809, 1.8453), false, true, 0.78986 };
        yield return new object[] { new Matrix(1, 0, 0, 1, 2, 3), 740, 750, new Matrix(1, 0.57735, 0.36397, 1, 3.09191, 4.1547), false, true, 0.78986 };
        yield return new object[] { new Matrix(1, 0, 0, 1, 2, 3), 35, 0, new Matrix(1, 0, 0.70021, 1, 4.10062, 3), false, true, 1 };
        yield return new object[] { new Matrix(1, 0, 0, 1, 2, 3), 0, 35, new Matrix(1, 0.70021, 0, 1, 2, 4.40042), false, true, 1 };
        yield return new object[] { new Matrix(1, 0, 0, 1, 2, 3), -720, -720, new Matrix(1, -0, -0, 1, 2, 3), false, true, 1 };
        yield return new object[] { new Matrix(1, 0, 0, 1, 2, 3), -360, -360, new Matrix(1, -0, -0, 1, 2, 3), false, true, 1 };
        yield return new object[] { new Matrix(1, 0, 0, 1, 2, 3), 360, 360, new Matrix(1, 0, 0, 1, 2, 3), false, true, 1 };
        yield return new object[] { new Matrix(1, 0, 0, 1, 2, 3), 720, 720, new Matrix(1, 0, 0, 1, 2, 3), false, true, 1 };
        yield return new object[] { new Matrix(1, 0, 0, 1, 2, 3), 720, 720, new Matrix(1, 0, 0, 1, 2, 3), false, true, 1 };
        yield return new object[] { new Matrix(1, 0, 0, 1, 2, 3), 20, double.NaN, new Matrix(1, double.NaN, 0.36397, 1, 3.09191, double.NaN), false, true, double.NaN };
        yield return new object[] { new Matrix(1, 0, 0, 1, 2, 3), double.NaN, 30, new Matrix(1, 0.57735, double.NaN, 1, double.NaN, 4.1547), false, true, double.NaN };
        yield return new object[] { new Matrix(1, 0, 0, 1, 2, 3), double.NaN, double.NaN, new Matrix(1, double.NaN, double.NaN, 1, double.NaN, double.NaN), false, true, double.NaN };

        // Translated and scaled.
        yield return new object[] { new Matrix(2, 0, 0, 3, 1, 2), -90, -180, new Matrix(2, 0, -48993718059586110, 3, -32662478706390740, 2), false, true, 18 };
        yield return new object[] { new Matrix(2, 0, 0, 3, 1, 2), -90, 180, new Matrix(2, -0, -48993718059586110, 3, -32662478706390740, 2), false, true, -6 };
        yield return new object[] { new Matrix(2, 0, 0, 3, 1, 2), 90, -180, new Matrix(2, 0, 48993718059586110, 3, 32662478706390740, 2), false, true, -6 };
        yield return new object[] { new Matrix(2, 0, 0, 3, 1, 2), -90, 0, new Matrix(2, 0, -48993718059586110, 3, -32662478706390740, 2), false, true, 6 };
        yield return new object[] { new Matrix(2, 0, 0, 3, 1, 2), 0, -180, new Matrix(2, 0, 0, 3, 1, 2), false, true, 6 };
        yield return new object[] { new Matrix(2, 0, 0, 3, 1, 2), 0, 0, new Matrix(2, 0, 0, 3, 1, 2), false, true, 6 };
        yield return new object[] { new Matrix(2, 0, 0, 3, 1, 2), 90, 180, new Matrix(2, -0, 48993718059586110, 3, 32662478706390740, 2), false, true, 18 };
        yield return new object[] { new Matrix(2, 0, 0, 3, 1, 2), 90, 0, new Matrix(2, 0, 48993718059586110, 3, 32662478706390740, 2), false, true, 6 };
        yield return new object[] { new Matrix(2, 0, 0, 3, 1, 2), 180, 0, new Matrix(2, 0, -0, 3, 1, 2), false, true, 6 };
        yield return new object[] { new Matrix(2, 0, 0, 3, 1, 2), 0, 180, new Matrix(2, -0, 0, 3, 1, 2), false, true, 6 };
        yield return new object[] { new Matrix(2, 0, 0, 3, 1, 2), 180, 180, new Matrix(2, -0, -0, 3, 1, 2), false, true, 6 };
        yield return new object[] { new Matrix(2, 0, 0, 3, 1, 2), 270, 270, new Matrix(2, 10887492902130246, 16331239353195366, 3, 10887492902130248, 5443746451065125), false, true, -1.7780625254090473E+32 };
        yield return new object[] { new Matrix(2, 0, 0, 3, 1, 2), 180, 270, new Matrix(2, 10887492902130246, -0, 3, 1, 5443746451065125), false, true, 10 };
        yield return new object[] { new Matrix(2, 0, 0, 3, 1, 2), 270, 270, new Matrix(2, 10887492902130246, 16331239353195366, 3, 10887492902130248, 5443746451065125), false, true, -1.7780625254090473E+32 };
        yield return new object[] { new Matrix(2, 0, 0, 3, 1, 2), -20, -30, new Matrix(2, -1.1547, -1.09191, 3, 0.27206, 1.42265), false, true, 4.73917 };
        yield return new object[] { new Matrix(2, 0, 0, 3, 1, 2), 20, 30, new Matrix(2, 1.1547, 1.09191, 3, 1.72794, 2.57735), false, true, 4.73917 };
        yield return new object[] { new Matrix(2, 0, 0, 3, 1, 2), -380, -390, new Matrix(2, -1.1547, -1.09191, 3, 0.27206, 1.42265), false, true, 4.73917 };
        yield return new object[] { new Matrix(2, 0, 0, 3, 1, 2), 380, 390, new Matrix(2, 1.1547, 1.09191, 3, 1.72794, 2.57735), false, true, 4.73917 };
        yield return new object[] { new Matrix(2, 0, 0, 3, 1, 2), -740, -750, new Matrix(2, -1.1547, -1.09191, 3, 0.27206, 1.42265), false, true, 4.73917 };
        yield return new object[] { new Matrix(2, 0, 0, 3, 1, 2), 740, 750, new Matrix(2, 1.1547, 1.09191, 3, 1.72794, 2.57735), false, true, 4.73917 };
        yield return new object[] { new Matrix(2, 0, 0, 3, 1, 2), 35, 0, new Matrix(2, 0, 2.10062, 3, 2.40042, 2), false, true, 6 };
        yield return new object[] { new Matrix(2, 0, 0, 3, 1, 2), 0, 35, new Matrix(2, 1.40042, 0, 3, 1, 2.70021), false, true, 6 };
        yield return new object[] { new Matrix(2, 0, 0, 3, 1, 2), -720, -720, new Matrix(2, 0, 0, 3, 1, 2), false, true, 6 };
        yield return new object[] { new Matrix(2, 0, 0, 3, 1, 2), -360, -360, new Matrix(2, 0, 0, 3, 1, 2), false, true, 6 };
        yield return new object[] { new Matrix(2, 0, 0, 3, 1, 2), 360, 360, new Matrix(2, 0, 0, 3, 1, 2), false, true, 6 };
        yield return new object[] { new Matrix(2, 0, 0, 3, 1, 2), 720, 720, new Matrix(2, 0, 0, 3, 1, 2), false, true, 6 };
        yield return new object[] { new Matrix(2, 0, 0, 3, 1, 2), 720, 720, new Matrix(2, 0, 0, 3, 1, 2), false, true, 6 };
        yield return new object[] { new Matrix(2, 0, 0, 3, 1, 2), 20, double.NaN, new Matrix(2, double.NaN, 1.09191, double.NaN, 1.72794, double.NaN), false, true, double.NaN };
        yield return new object[] { new Matrix(2, 0, 0, 3, 1, 2), double.NaN, 30, new Matrix(double.NaN, 1.1547, double.NaN, 3, double.NaN, 2.57735), false, true, double.NaN };
        yield return new object[] { new Matrix(2, 0, 0, 3, 1, 2), double.NaN, double.NaN, new Matrix(double.NaN, double.NaN, double.NaN, double.NaN, double.NaN, double.NaN), false, true, double.NaN };

        // Translated and skewed.
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 2, 3), -90, -180, new Matrix(-8165619676597683, 0.5, -16331239353195370, 1, -48993718059586110, 3), false, true, 3 };
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 2, 3), -90, 180, new Matrix(-8165619676597683, 0.5, -16331239353195370, 1, -48993718059586110, 3), false, false, 0 };
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 2, 3), 90, -180, new Matrix(8165619676597686, 0.5, 16331239353195370, 1, 48993718059586110, 3), false, true, -1 };
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 2, 3), -90, 0, new Matrix(-8165619676597683, 0.5, -16331239353195370, 1, -48993718059586110, 3), false, true, 1 };
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 2, 3), 0, -180, new Matrix(1, 0.5, 0.75, 1, 2, 3), false, true, 0.625 };
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 2, 3), 0, 0, new Matrix(1, 0.5, 0.75, 1, 2, 3), false, true, 0.625 };
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 2, 3), 90, 180, new Matrix(8165619676597686, 0.5, 16331239353195370, 1, 48993718059586110, 3), false, true, 2 };
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 2, 3), 90, 0, new Matrix(8165619676597686, 0.5, 16331239353195370, 1, 48993718059586110, 3), false, true, 1 };
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 2, 3), 180, 0, new Matrix(1, 0.5, 0.75, 1, 2, 3), false, true, 0.625 };
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 2, 3), 0, 180, new Matrix(1, 0.5, 0.75, 1, 2, 3), false, true, 0.625 };
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 2, 3), 180, 180, new Matrix(1, 0.5, 0.75, 1, 2, 3), false, true, 0.625 };
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 2, 3), 270, 270, new Matrix(2721873225532562.5, 5443746451065124, 5443746451065124, 4082809838298843, 16331239353195370, 10887492902130248), false, true, -1.8521484639677582E+31 };
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 2, 3), 180, 270, new Matrix(1, 5443746451065124, 0.75, 4082809838298843, 2, 10887492902130248), false, false, 0 };
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 2, 3), 270, 270, new Matrix(2721873225532562.5, 5443746451065124, 5443746451065124, 4082809838298843, 16331239353195370, 10887492902130248), false, true, -1.8521484639677582E+31 };
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 2, 3), -20, -30, new Matrix(0.81801, -0.07735, 0.38603, 0.56699, 0.90809, 1.8453), false, true, 0.49366 };
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 2, 3), 20, 30, new Matrix(1.18199, 1.07735, 1.11396, 1.43301, 3.09191, 4.1547), false, true, 0.49366 };
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 2, 3), -380, -390, new Matrix(0.81801, -0.07735, 0.38603, 0.56699, 0.90809, 1.8453), false, true, 0.49366 };
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 2, 3), 380, 390, new Matrix(1.18199, 1.07735, 1.11396, 1.43301, 3.09191, 4.1547), false, true, 0.49366 };
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 2, 3), -740, -750, new Matrix(0.81801, -0.07735, 0.38603, 0.56699, 0.90809, 1.8453), false, true, 0.49366 };
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 2, 3), 740, 750, new Matrix(1.18199, 1.07735, 1.11396, 1.43301, 3.09191, 4.1547), false, true, 0.49366 };
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 2, 3), 35, 0, new Matrix(1.3501, 0.5, 1.45021, 1, 4.10062, 3), false, true, 0.625 };
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 2, 3), 0, 35, new Matrix(1, 1.20021, 0.75, 1.52516, 2, 4.40042), false, true, 0.625 };
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 2, 3), -720, -720, new Matrix(1, 0.5, 0.75, 1, 2, 3), false, true, 0.625 };
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 2, 3), -360, -360, new Matrix(1, 0.5, 0.75, 1, 2, 3), false, true, 0.625 };
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 2, 3), 360, 360, new Matrix(1, 0.5, 0.75, 1, 2, 3), false, true, 0.625 };
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 2, 3), 720, 720, new Matrix(1, 0.5, 0.75, 1, 2, 3), false, true, 0.625 };
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 2, 3), 720, 720, new Matrix(1, 0.5, 0.75, 1, 2, 3), false, true, 0.625 };
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 2, 3), 20, double.NaN, new Matrix(1.18199, double.NaN, 1.11396, double.NaN, 3.09191, double.NaN), false, true, double.NaN };
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 2, 3), double.NaN, 30, new Matrix(double.NaN, 1.07735, double.NaN, 1.43301, double.NaN, 4.1547), false, true, double.NaN };
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 2, 3), double.NaN, double.NaN, new Matrix(double.NaN, double.NaN, double.NaN, double.NaN, double.NaN, double.NaN), false, true, double.NaN };

        // Skewed and scaled.
        yield return new object[] { new Matrix(2, 0.5, 0.75, 3, 0, 0), -90, -180, new Matrix(-8165619676597683, 0.5, -48993718059586110, 3, 0, 0), false, true, 20 };
        yield return new object[] { new Matrix(2, 0.5, 0.75, 3, 0, 0), -90, 180, new Matrix(-8165619676597683, 0.5, -48993718059586110, 3, 0, 0), false, true, -4 };
        yield return new object[] { new Matrix(2, 0.5, 0.75, 3, 0, 0), 90, -180, new Matrix(8165619676597687, 0.5, 48993718059586110, 3, 0, 0), false, true, -8 };
        yield return new object[] { new Matrix(2, 0.5, 0.75, 3, 0, 0), -90, 0, new Matrix(-8165619676597683, 0.5, -48993718059586110, 3, 0, 0), false, true, 8 };
        yield return new object[] { new Matrix(2, 0.5, 0.75, 3, 0, 0), 0, -180, new Matrix(2, 0.5, 0.75, 3, 0, 0), false, true, 5.625 };
        yield return new object[] { new Matrix(2, 0.5, 0.75, 3, 0, 0), 0, 0, new Matrix(2, 0.5, 0.75, 3, 0, 0), false, true, 5.625 };
        yield return new object[] { new Matrix(2, 0.5, 0.75, 3, 0, 0), 90, 180, new Matrix(8165619676597687, 0.5, 48993718059586110, 3, 0, 0), false, true, 16 };
        yield return new object[] { new Matrix(2, 0.5, 0.75, 3, 0, 0), 90, 0, new Matrix(8165619676597687, 0.5, 48993718059586110, 3, 0, 0), false, true, 4 };
        yield return new object[] { new Matrix(2, 0.5, 0.75, 3, 0, 0), 180, 0, new Matrix(2, 0.5, 0.75, 3, 0, 0), false, true, 5.625 };
        yield return new object[] { new Matrix(2, 0.5, 0.75, 3, 0, 0), 0, 180, new Matrix(2, 0.5, 0.75, 3, 0, 0), false, true, 5.625 };
        yield return new object[] { new Matrix(2, 0.5, 0.75, 3, 0, 0), 180, 180, new Matrix(2, 0.5, 0.75, 3, 0, 0), false, true, 5.625 };
        yield return new object[] { new Matrix(2, 0.5, 0.75, 3, 0, 0), 270, 270, new Matrix(2721873225532563.5, 10887492902130246, 16331239353195366, 4082809838298845, 0, 0), false, true, -1.6669336175709816E+32 };
        yield return new object[] { new Matrix(2, 0.5, 0.75, 3, 0, 0), 180, 270, new Matrix(2, 10887492902130246, 0.75, 4082809838298845, 0, 0), false, true, 9 };
        yield return new object[] { new Matrix(2, 0.5, 0.75, 3, 0, 0), 270, 270, new Matrix(2721873225532563.5, 10887492902130246, 16331239353195366, 4082809838298845, 0, 0), false, true, -1.6669336175709816E+32 };
        yield return new object[] { new Matrix(2, 0.5, 0.75, 3, 0, 0), -20, -30, new Matrix(1.81801, -0.65469, -0.34191, 2.56699, 0, 0), false, true, 4.44297 };
        yield return new object[] { new Matrix(2, 0.5, 0.75, 3, 0, 0), 20, 30, new Matrix(2.18199, 1.6547, 1.84191, 3.43301, 0, 0), false, true, 4.44297 };
        yield return new object[] { new Matrix(2, 0.5, 0.75, 3, 0, 0), -380, -390, new Matrix(1.81801, -0.65469, -0.34191, 2.56699, 0, 0), false, true, 4.44297 };
        yield return new object[] { new Matrix(2, 0.5, 0.75, 3, 0, 0), 380, 390, new Matrix(2.18199, 1.6547, 1.84191, 3.43301, 0, 0), false, true, 4.44297 };
        yield return new object[] { new Matrix(2, 0.5, 0.75, 3, 0, 0), -740, -750, new Matrix(1.81801, -0.65469, -0.34191, 2.56699, 0, 0), false, true, 4.44297 };
        yield return new object[] { new Matrix(2, 0.5, 0.75, 3, 0, 0), 740, 750, new Matrix(2.18199, 1.6547, 1.84191, 3.43301, 0, 0), false, true, 4.44297 };
        yield return new object[] { new Matrix(2, 0.5, 0.75, 3, 0, 0), 35, 0, new Matrix(2.3501, 0.5, 2.85062, 3, 0, 0), false, true, 5.625 };
        yield return new object[] { new Matrix(2, 0.5, 0.75, 3, 0, 0), 0, 35, new Matrix(2, 1.90042, 0.75, 3.52516, 0, 0), false, true, 5.625 };
        yield return new object[] { new Matrix(2, 0.5, 0.75, 3, 0, 0), -720, -720, new Matrix(2, 0.5, 0.75, 3, 0, 0), false, true, 5.625 };
        yield return new object[] { new Matrix(2, 0.5, 0.75, 3, 0, 0), -360, -360, new Matrix(2, 0.5, 0.75, 3, 0, 0), false, true, 5.625 };
        yield return new object[] { new Matrix(2, 0.5, 0.75, 3, 0, 0), 360, 360, new Matrix(2, 0.5, 0.75, 3, 0, 0), false, true, 5.625 };
        yield return new object[] { new Matrix(2, 0.5, 0.75, 3, 0, 0), 720, 720, new Matrix(2, 0.5, 0.75, 3, 0, 0), false, true, 5.625 };
        yield return new object[] { new Matrix(2, 0.5, 0.75, 3, 0, 0), 720, 720, new Matrix(2, 0.5, 0.75, 3, 0, 0), false, true, 5.625 };
        yield return new object[] { new Matrix(2, 0.5, 0.75, 3, 0, 0), 20, double.NaN, new Matrix(2.18199, double.NaN, 1.84191, double.NaN, 0, double.NaN), false, true, double.NaN };
        yield return new object[] { new Matrix(2, 0.5, 0.75, 3, 0, 0), double.NaN, 30, new Matrix(double.NaN, 1.6547, double.NaN, 3.43301, double.NaN, 0), false, true, double.NaN };
        yield return new object[] { new Matrix(2, 0.5, 0.75, 3, 0, 0), double.NaN, double.NaN, new Matrix(double.NaN, double.NaN, double.NaN, double.NaN, double.NaN, double.NaN), false, true, double.NaN };

        // Complex.
        yield return new object[] { new Matrix(2, 3, 4, 5, 6, 7), -90, -180, new Matrix(-48993718059586110, 3, -81656196765976850, 5, -1.1431867547236758E+17, 7), false, false, 0 };
        yield return new object[] { new Matrix(2, 3, 4, 5, 6, 7), -90, 180, new Matrix(-48993718059586110, 3, -81656196765976850, 5, -1.1431867547236758E+17, 7), false, true, -32 };
        yield return new object[] { new Matrix(2, 3, 4, 5, 6, 7), 90, -180, new Matrix(48993718059586110, 3, 81656196765976850, 5, 1.1431867547236758E+17, 7), false, false, 0 };
        yield return new object[] { new Matrix(2, 3, 4, 5, 6, 7), -90, 0, new Matrix(-48993718059586110, 3, -81656196765976850, 5, -1.1431867547236758E+17, 7), false, false, 0 };
        yield return new object[] { new Matrix(2, 3, 4, 5, 6, 7), 0, -180, new Matrix(2, 3, 4, 5, 6, 7), false, true, -2 };
        yield return new object[] { new Matrix(2, 3, 4, 5, 6, 7), 0, 0, new Matrix(2, 3, 4, 5, 6, 7), false, true, -2 };
        yield return new object[] { new Matrix(2, 3, 4, 5, 6, 7), 90, 180, new Matrix(48993718059586110, 3, 81656196765976850, 5, 1.1431867547236758E+17, 7), false, true, 32 };
        yield return new object[] { new Matrix(2, 3, 4, 5, 6, 7), 90, 0, new Matrix(48993718059586110, 3, 81656196765976850, 5, 1.1431867547236758E+17, 7), false, false, 0 };
        yield return new object[] { new Matrix(2, 3, 4, 5, 6, 7), 180, 0, new Matrix(2, 3, 4, 5, 6, 7), false, true, -2 };
        yield return new object[] { new Matrix(2, 3, 4, 5, 6, 7), 0, 180, new Matrix(2, 3, 4, 5, 6, 7), false, true, -2 };
        yield return new object[] { new Matrix(2, 3, 4, 5, 6, 7), 180, 180, new Matrix(2, 3, 4, 5, 6, 7), false, true, -2 };
        yield return new object[] { new Matrix(2, 3, 4, 5, 6, 7), 270, 270, new Matrix(16331239353195370, 10887492902130248, 27218732255325624, 21774985804260496, 38106225157455870, 32662478706390744), false, true, 5.926875084696823E+31 };
        yield return new object[] { new Matrix(2, 3, 4, 5, 6, 7), 180, 270, new Matrix(2, 10887492902130248, 4, 21774985804260496, 6, 32662478706390744), false, false, 0 };
        yield return new object[] { new Matrix(2, 3, 4, 5, 6, 7), 270, 270, new Matrix(16331239353195370, 10887492902130248, 27218732255325624, 21774985804260496, 38106225157455870, 32662478706390744), false, true, 5.926875084696823E+31 };
        yield return new object[] { new Matrix(2, 3, 4, 5, 6, 7), -20, -30, new Matrix(0.90809, 1.8453, 2.18014, 2.6906, 3.45221, 3.5359), false, true, -1.57972 };
        yield return new object[] { new Matrix(2, 3, 4, 5, 6, 7), 20, 30, new Matrix(3.09191, 4.1547, 5.81985, 7.3094, 8.54779, 10.4641), false, true, -1.57972 };
        yield return new object[] { new Matrix(2, 3, 4, 5, 6, 7), -380, -390, new Matrix(0.90809, 1.8453, 2.18014, 2.6906, 3.45221, 3.5359), false, true, -1.57972 };
        yield return new object[] { new Matrix(2, 3, 4, 5, 6, 7), 380, 390, new Matrix(3.09191, 4.1547, 5.81985, 7.3094, 8.54779, 10.4641), false, true, -1.57972 };
        yield return new object[] { new Matrix(2, 3, 4, 5, 6, 7), -740, -750, new Matrix(0.90809, 1.8453, 2.18014, 2.6906, 3.45221, 3.5359), false, true, -1.57972 };
        yield return new object[] { new Matrix(2, 3, 4, 5, 6, 7), 740, 750, new Matrix(3.09191, 4.1547, 5.81985, 7.3094, 8.54779, 10.4641), false, true, -1.57972 };
        yield return new object[] { new Matrix(2, 3, 4, 5, 6, 7), 35, 0, new Matrix(4.10062, 3, 7.50104, 5, 10.90145, 7), false, true, -2 };
        yield return new object[] { new Matrix(2, 3, 4, 5, 6, 7), 0, 35, new Matrix(2, 4.40042, 4, 7.80083, 6, 11.20125), false, true, -2 };
        yield return new object[] { new Matrix(2, 3, 4, 5, 6, 7), -720, -720, new Matrix(2, 3, 4, 5, 6, 7), false, true, -2 };
        yield return new object[] { new Matrix(2, 3, 4, 5, 6, 7), -360, -360, new Matrix(2, 3, 4, 5, 6, 7), false, true, -2 };
        yield return new object[] { new Matrix(2, 3, 4, 5, 6, 7), 360, 360, new Matrix(2, 3, 4, 5, 6, 7), false, true, -2 };
        yield return new object[] { new Matrix(2, 3, 4, 5, 6, 7), 720, 720, new Matrix(2, 3, 4, 5, 6, 7), false, true, -2 };
        yield return new object[] { new Matrix(2, 3, 4, 5, 6, 7), 720, 720, new Matrix(2, 3, 4, 5, 6, 7), false, true, -2 };
        yield return new object[] { new Matrix(2, 3, 4, 5, 6, 7), 20, double.NaN, new Matrix(3.09191, double.NaN, 5.81985, double.NaN, 8.54779, double.NaN), false, true, double.NaN };
        yield return new object[] { new Matrix(2, 3, 4, 5, 6, 7), double.NaN, 30, new Matrix(double.NaN, 4.1547, double.NaN, 7.3094, double.NaN, 10.4641), false, true, double.NaN };
        yield return new object[] { new Matrix(2, 3, 4, 5, 6, 7), double.NaN, double.NaN, new Matrix(double.NaN, double.NaN, double.NaN, double.NaN, double.NaN, double.NaN), false, true, double.NaN };

        // No inverse.
        yield return new object[] { new Matrix(0, 0, 0, 0, 0, 0), -90, -180, new Matrix(0, 0, 0, 0, 0, 0), false, false, 0 };
        yield return new object[] { new Matrix(0, 0, 0, 0, 0, 0), -90, 180, new Matrix(0, 0, 0, 0, 0, 0), false, false, 0 };
        yield return new object[] { new Matrix(0, 0, 0, 0, 0, 0), 90, -180, new Matrix(0, 0, 0, 0, 0, 0), false, false, 0 };
        yield return new object[] { new Matrix(0, 0, 0, 0, 0, 0), -90, 0, new Matrix(0, 0, 0, 0, 0, 0), false, false, 0 };
        yield return new object[] { new Matrix(0, 0, 0, 0, 0, 0), 0, -180, new Matrix(0, 0, 0, 0, 0, 0), false, false, 0 };
        yield return new object[] { new Matrix(0, 0, 0, 0, 0, 0), 0, 0, new Matrix(0, 0, 0, 0, 0, 0), false, false, 0 };
        yield return new object[] { new Matrix(0, 0, 0, 0, 0, 0), 90, 180, new Matrix(0, 0, 0, 0, 0, 0), false, false, 0 };
        yield return new object[] { new Matrix(0, 0, 0, 0, 0, 0), 90, 0, new Matrix(0, 0, 0, 0, 0, 0), false, false, 0 };
        yield return new object[] { new Matrix(0, 0, 0, 0, 0, 0), 180, 0, new Matrix(0, 0, 0, 0, 0, 0), false, false, 0 };
        yield return new object[] { new Matrix(0, 0, 0, 0, 0, 0), 0, 180, new Matrix(0, 0, 0, 0, 0, 0), false, false, 0 };
        yield return new object[] { new Matrix(0, 0, 0, 0, 0, 0), 180, 180, new Matrix(0, 0, 0, 0, 0, 0), false, false, 0 };
        yield return new object[] { new Matrix(0, 0, 0, 0, 0, 0), 270, 270, new Matrix(0, 0, 0, 0, 0, 0), false, false, 0 };
        yield return new object[] { new Matrix(0, 0, 0, 0, 0, 0), 180, 270, new Matrix(0, 0, 0, 0, 0, 0), false, false, 0 };
        yield return new object[] { new Matrix(0, 0, 0, 0, 0, 0), 270, 270, new Matrix(0, 0, 0, 0, 0, 0), false, false, 0 };
        yield return new object[] { new Matrix(0, 0, 0, 0, 0, 0), -20, -30, new Matrix(0, 0, 0, 0, 0, 0), false, false, 0 };
        yield return new object[] { new Matrix(0, 0, 0, 0, 0, 0), 20, 30, new Matrix(0, 0, 0, 0, 0, 0), false, false, 0 };
        yield return new object[] { new Matrix(0, 0, 0, 0, 0, 0), -380, -390, new Matrix(0, 0, 0, 0, 0, 0), false, false, 0 };
        yield return new object[] { new Matrix(0, 0, 0, 0, 0, 0), 380, 390, new Matrix(0, 0, 0, 0, 0, 0), false, false, 0 };
        yield return new object[] { new Matrix(0, 0, 0, 0, 0, 0), -740, -750, new Matrix(0, 0, 0, 0, 0, 0), false, false, 0 };
        yield return new object[] { new Matrix(0, 0, 0, 0, 0, 0), 740, 750, new Matrix(0, 0, 0, 0, 0, 0), false, false, 0 };
        yield return new object[] { new Matrix(0, 0, 0, 0, 0, 0), 35, 0, new Matrix(0, 0, 0, 0, 0, 0), false, false, 0 };
        yield return new object[] { new Matrix(0, 0, 0, 0, 0, 0), 0, 35, new Matrix(0, 0, 0, 0, 0, 0), false, false, 0 };
        yield return new object[] { new Matrix(0, 0, 0, 0, 0, 0), -720, -720, new Matrix(0, 0, 0, 0, 0, 0), false, false, 0 };
        yield return new object[] { new Matrix(0, 0, 0, 0, 0, 0), -360, -360, new Matrix(0, 0, 0, 0, 0, 0), false, false, 0 };
        yield return new object[] { new Matrix(0, 0, 0, 0, 0, 0), 360, 360, new Matrix(0, 0, 0, 0, 0, 0), false, false, 0 };
        yield return new object[] { new Matrix(0, 0, 0, 0, 0, 0), 720, 720, new Matrix(0, 0, 0, 0, 0, 0), false, false, 0 };
        yield return new object[] { new Matrix(0, 0, 0, 0, 0, 0), 720, 720, new Matrix(0, 0, 0, 0, 0, 0), false, false, 0 };
        yield return new object[] { new Matrix(0, 0, 0, 0, 0, 0), 20, double.NaN, new Matrix(0, double.NaN, 0, double.NaN, 0, double.NaN), false, true, double.NaN };
        yield return new object[] { new Matrix(0, 0, 0, 0, 0, 0), double.NaN, 30, new Matrix(double.NaN, 0, double.NaN, 0, double.NaN, 0), false, true, double.NaN };
        yield return new object[] { new Matrix(0, 0, 0, 0, 0, 0), double.NaN, double.NaN, new Matrix(double.NaN, double.NaN, double.NaN, double.NaN, double.NaN, double.NaN), false, true, double.NaN };

    }

    [Theory]
    [MemberData(nameof(Skew_TestData))]
    public void Skew_Invoke_Success(Matrix matrix, double skewX, double skewY, Matrix expected, bool expectedIsIdentity, bool expectedHasInverse, double expectedDeterminant)
    {
        matrix.Skew(skewX, skewY);
        Helpers.AssertEqualRounded(expected, matrix);
        Assert.Equal(expectedIsIdentity, matrix.IsIdentity);
        Assert.Equal(expectedHasInverse, matrix.HasInverse);
        Assert.Equal(expectedDeterminant, matrix.Determinant, precision: 5);
    }

    public static IEnumerable<object[]> SkewPrepend_TestData()
    {
        // Identity
        foreach (Matrix matrix in IdentityMatrices())
        {
            yield return new object[] { matrix, -90, -180, new Matrix(1, 0, -16331239353195370, 1, 0, 0), false, true, 3 };
            yield return new object[] { matrix, -90, 180, new Matrix(1, -0, -16331239353195370, 1, 0, 0), false, true, -1 };
            yield return new object[] { matrix, 90, -180, new Matrix(1, 0, 16331239353195370, 1, 0, 0), false, true, -1 };
            yield return new object[] { matrix, -90, 0, new Matrix(1, 0, -16331239353195370, 1, 0, 0), false, true, 1 };
            yield return new object[] { matrix, 0, -180, new Matrix(1, 0, 0, 1, 0, 0), false, true, 1 };
            yield return new object[] { matrix, 0, 0, new Matrix(1, 0, 0, 1, 0, 0), true, true, 1 };
            yield return new object[] { matrix, 90, 180, new Matrix(1, -0, 16331239353195370, 1, 0, 0), false, true, 3 };
            yield return new object[] { matrix, 90, 0, new Matrix(1, 0, 16331239353195370, 1, 0, 0), false, true, 1 };
            yield return new object[] { matrix, 180, 0, new Matrix(1, 0, -0, 1, 0, 0), false, true, 1 };
            yield return new object[] { matrix, 0, 180, new Matrix(1, -0, 0, 1, 0, 0), false, true, 1 };
            yield return new object[] { matrix, 180, 180, new Matrix(1, -0, -0, 1, 0, 0), false, true, 1 };
            yield return new object[] { matrix, 270, 270, new Matrix(1, 5443746451065123, 5443746451065123, 1, 0, 0), false, true, -2.963437542348412E+31 };
            yield return new object[] { matrix, 180, 270, new Matrix(1, 5443746451065123, -0, 1, 0, 0), false, true, 1.66667 };
            yield return new object[] { matrix, 270, 270, new Matrix(1, 5443746451065123, 5443746451065123, 1, 0, 0), false, true, -2.963437542348412E+31 };
            yield return new object[] { matrix, -20, -30, new Matrix(1, -0.57735, -0.36397, 1, 0, 0), false, true, 0.78986 };
            yield return new object[] { matrix, 20, 30, new Matrix(1, 0.57735, 0.36397, 1, 0, 0), false, true, 0.78986 };
            yield return new object[] { matrix, -380, -390, new Matrix(1, -0.57735, -0.36397, 1, 0, 0), false, true, 0.78986 };
            yield return new object[] { matrix, 380, 390, new Matrix(1, 0.57735, 0.36397, 1, 0, 0), false, true, 0.78986 };
            yield return new object[] { matrix, -740, -750, new Matrix(1, -0.57735, -0.36397, 1, 0, 0), false, true, 0.78986 };
            yield return new object[] { matrix, 740, 750, new Matrix(1, 0.57735, 0.36397, 1, 0, 0), false, true, 0.78986 };
            yield return new object[] { matrix, 35, 0, new Matrix(1, 0, 0.70021, 1, 0, 0), false, true, 1 };
            yield return new object[] { matrix, 0, 35, new Matrix(1, 0.70021, 0, 1, 0, 0), false, true, 1 };
            yield return new object[] { matrix, -720, -720, new Matrix(1, -0, -0, 1, 0, 0), true, true, 1 };
            yield return new object[] { matrix, -360, -360, new Matrix(1, -0, -0, 1, 0, 0), true, true, 1 };
            yield return new object[] { matrix, 360, 360, new Matrix(1, 0, 0, 1, 0, 0), true, true, 1 };
            yield return new object[] { matrix, 720, 720, new Matrix(1, 0, 0, 1, 0, 0), true, true, 1 };
            yield return new object[] { matrix, 720, 720, new Matrix(1, 0, 0, 1, 0, 0), true, true, 1 };
            yield return new object[] { matrix, 20, double.NaN, new Matrix(1, double.NaN, 0.36397, 1, 0, 0), false, true, double.NaN };
            yield return new object[] { matrix, double.NaN, 30, new Matrix(1, 0.57735, double.NaN, 1, 0, 0), false, true, double.NaN };
            yield return new object[] { matrix, double.NaN, double.NaN, new Matrix(1, double.NaN, double.NaN, 1, 0, 0), false, true, double.NaN };
        }

        // Scaled.
        yield return new object[] { new Matrix(2, 0, 0, 3, 0, 0), -90, -180, new Matrix(2, 0, -32662478706390740, 3, 0, 0), false, true, 18 };
        yield return new object[] { new Matrix(2, 0, 0, 3, 0, 0), -90, 180, new Matrix(2, -0, -32662478706390740, 3, 0, 0), false, true, -6 };
        yield return new object[] { new Matrix(2, 0, 0, 3, 0, 0), 90, -180, new Matrix(2, 0, 32662478706390740, 3, 0, 0), false, true, -6 };
        yield return new object[] { new Matrix(2, 0, 0, 3, 0, 0), -90, 0, new Matrix(2, 0, -32662478706390740, 3, 0, 0), false, true, 6 };
        yield return new object[] { new Matrix(2, 0, 0, 3, 0, 0), 0, -180, new Matrix(2, 0, 0, 3, 0, 0), false, true, 6 };
        yield return new object[] { new Matrix(2, 0, 0, 3, 0, 0), 0, 0, new Matrix(2, 0, 0, 3, 0, 0), false, true, 6 };
        yield return new object[] { new Matrix(2, 0, 0, 3, 0, 0), 90, 180, new Matrix(2, -0, 32662478706390740, 3, 0, 0), false, true, 18 };
        yield return new object[] { new Matrix(2, 0, 0, 3, 0, 0), 90, 0, new Matrix(2, 0, 32662478706390740, 3, 0, 0), false, true, 6 };
        yield return new object[] { new Matrix(2, 0, 0, 3, 0, 0), 180, 0, new Matrix(2, 0, -0, 3, 0, 0), false, true, 6 };
        yield return new object[] { new Matrix(2, 0, 0, 3, 0, 0), 0, 180, new Matrix(2, -0, 0, 3, 0, 0), false, true, 6 };
        yield return new object[] { new Matrix(2, 0, 0, 3, 0, 0), 180, 180, new Matrix(2, -0, -0, 3, 0, 0), false, true, 6 };
        yield return new object[] { new Matrix(2, 0, 0, 3, 0, 0), 270, 270, new Matrix(2, 16331239353195366, 10887492902130246, 3, 0, 0), false, true, -1.7780625254090473E+32 };
        yield return new object[] { new Matrix(2, 0, 0, 3, 0, 0), 180, 270, new Matrix(2, 16331239353195366, -0, 3, 0, 0), false, true, 10 };
        yield return new object[] { new Matrix(2, 0, 0, 3, 0, 0), 270, 270, new Matrix(2, 16331239353195366, 10887492902130246, 3, 0, 0), false, true, -1.7780625254090473E+32 };
        yield return new object[] { new Matrix(2, 0, 0, 3, 0, 0), -20, -30, new Matrix(2, -1.73205, -0.72794, 3, 0, 0), false, true, 4.73917 };
        yield return new object[] { new Matrix(2, 0, 0, 3, 0, 0), 20, 30, new Matrix(2, 1.73205, 0.72794, 3, 0, 0), false, true, 4.73917 };
        yield return new object[] { new Matrix(2, 0, 0, 3, 0, 0), -380, -390, new Matrix(2, -1.73205, -0.72794, 3, 0, 0), false, true, 4.73917 };
        yield return new object[] { new Matrix(2, 0, 0, 3, 0, 0), 380, 390, new Matrix(2, 1.73205, 0.72794, 3, 0, 0), false, true, 4.73917 };
        yield return new object[] { new Matrix(2, 0, 0, 3, 0, 0), -740, -750, new Matrix(2, -1.73205, -0.72794, 3, 0, 0), false, true, 4.73917 };
        yield return new object[] { new Matrix(2, 0, 0, 3, 0, 0), 740, 750, new Matrix(2, 1.73205, 0.72794, 3, 0, 0), false, true, 4.73917 };
        yield return new object[] { new Matrix(2, 0, 0, 3, 0, 0), 35, 0, new Matrix(2, 0, 1.40042, 3, 0, 0), false, true, 6 };
        yield return new object[] { new Matrix(2, 0, 0, 3, 0, 0), 0, 35, new Matrix(2, 2.10062, 0, 3, 0, 0), false, true, 6 };
        yield return new object[] { new Matrix(2, 0, 0, 3, 0, 0), -720, -720, new Matrix(2, 0, 0, 3, 0, 0), false, true, 6 };
        yield return new object[] { new Matrix(2, 0, 0, 3, 0, 0), -360, -360, new Matrix(2, 0, 0, 3, 0, 0), false, true, 6 };
        yield return new object[] { new Matrix(2, 0, 0, 3, 0, 0), 360, 360, new Matrix(2, 0, 0, 3, 0, 0), false, true, 6 };
        yield return new object[] { new Matrix(2, 0, 0, 3, 0, 0), 720, 720, new Matrix(2, 0, 0, 3, 0, 0), false, true, 6 };
        yield return new object[] { new Matrix(2, 0, 0, 3, 0, 0), 720, 720, new Matrix(2, 0, 0, 3, 0, 0), false, true, 6 };
        yield return new object[] { new Matrix(2, 0, 0, 3, 0, 0), 20, double.NaN, new Matrix(double.NaN, double.NaN, 0.72794, 3, 0, 0), false, true, double.NaN };
        yield return new object[] { new Matrix(2, 0, 0, 3, 0, 0), double.NaN, 30, new Matrix(2, 1.73205, double.NaN, double.NaN, 0, 0), false, true, double.NaN };
        yield return new object[] { new Matrix(2, 0, 0, 3, 0, 0), double.NaN, double.NaN, new Matrix(double.NaN, double.NaN, double.NaN, double.NaN, 0, 0), false, true, double.NaN };

        // Skewed.
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 0, 0), -90, -180, new Matrix(1, 0.5, -16331239353195370, -8165619676597683, 0, 0), false, true, 3 };
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 0, 0), -90, 180, new Matrix(1, 0.5, -16331239353195370, -8165619676597683, 0, 0), false, false, 0 };
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 0, 0), 90, -180, new Matrix(1, 0.5, 16331239353195370, 8165619676597686, 0, 0), false, true, -1 };
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 0, 0), -90, 0, new Matrix(1, 0.5, -16331239353195370, -8165619676597683, 0, 0), false, true, 1 };
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 0, 0), 0, -180, new Matrix(1, 0.5, 0.75, 1, 0, 0), false, true, 0.625 };
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 0, 0), 0, 0, new Matrix(1, 0.5, 0.75, 1, 0, 0), false, true, 0.625 };
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 0, 0), 90, 180, new Matrix(1, 0.5, 16331239353195370, 8165619676597686, 0, 0), false, true, 2 };
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 0, 0), 90, 0, new Matrix(1, 0.5, 16331239353195370, 8165619676597686, 0, 0), false, true, 1 };
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 0, 0), 180, 0, new Matrix(1, 0.5, 0.75, 1, 0, 0), false, true, 0.625 };
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 0, 0), 0, 180, new Matrix(1, 0.5, 0.75, 1, 0, 0), false, true, 0.625 };
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 0, 0), 180, 180, new Matrix(1, 0.5, 0.75, 1, 0, 0), false, true, 0.625 };
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 0, 0), 270, 270, new Matrix(4082809838298843, 5443746451065124, 5443746451065124, 2721873225532562.5, 0, 0), false, true, -1.8521484639677582E+31 };
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 0, 0), 180, 270, new Matrix(4082809838298843, 5443746451065124, 0.75, 1, 0, 0), false, false, 0 };
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 0, 0), 270, 270, new Matrix(4082809838298843, 5443746451065124, 5443746451065124, 2721873225532562.5, 0, 0), false, true, -1.8521484639677582E+31 };
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 0, 0), -20, -30, new Matrix(0.56699, -0.07735, 0.38603, 0.81801, 0, 0), false, true, 0.49366 };
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 0, 0), 20, 30, new Matrix(1.43301, 1.07735, 1.11396, 1.18199, 0, 0), false, true, 0.49366 };
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 0, 0), -380, -390, new Matrix(0.56699, -0.07735, 0.38603, 0.81801, 0, 0), false, true, 0.49366 };
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 0, 0), 380, 390, new Matrix(1.43301, 1.07735, 1.11396, 1.18199, 0, 0), false, true, 0.49366 };
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 0, 0), -740, -750, new Matrix(0.56699, -0.07735, 0.38603, 0.81801, 0, 0), false, true, 0.49366 };
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 0, 0), 740, 750, new Matrix(1.43301, 1.07735, 1.11396, 1.18199, 0, 0), false, true, 0.49366 };
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 0, 0), 35, 0, new Matrix(1, 0.5, 1.45021, 1.3501, 0, 0), false, true, 0.625 };
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 0, 0), 0, 35, new Matrix(1.52516, 1.20021, 0.75, 1, 0, 0), false, true, 0.625 };
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 0, 0), -720, -720, new Matrix(1, 0.5, 0.75, 1, 0, 0), false, true, 0.625 };
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 0, 0), -360, -360, new Matrix(1, 0.5, 0.75, 1, 0, 0), false, true, 0.625 };
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 0, 0), 360, 360, new Matrix(1, 0.5, 0.75, 1, 0, 0), false, true, 0.625 };
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 0, 0), 720, 720, new Matrix(1, 0.5, 0.75, 1, 0, 0), false, true, 0.625 };
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 0, 0), 720, 720, new Matrix(1, 0.5, 0.75, 1, 0, 0), false, true, 0.625 };
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 0, 0), 20, double.NaN, new Matrix(double.NaN, double.NaN, 1.11396, 1.18199, 0, 0), false, true, double.NaN };
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 0, 0), double.NaN, 30, new Matrix(1.43301, 1.07735, double.NaN, double.NaN, 0, 0), false, true, double.NaN };
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 0, 0), double.NaN, double.NaN, new Matrix(double.NaN, double.NaN, double.NaN, double.NaN, 0, 0), false, true, double.NaN };

        // Translated.
        yield return new object[] { new Matrix(1, 0, 0, 1, 2, 3), -90, -180, new Matrix(1, 0, -16331239353195370, 1, 2, 3), false, true, 3 };
        yield return new object[] { new Matrix(1, 0, 0, 1, 2, 3), -90, 180, new Matrix(1, -0, -16331239353195370, 1, 2, 3), false, true, -1 };
        yield return new object[] { new Matrix(1, 0, 0, 1, 2, 3), 90, -180, new Matrix(1, 0, 16331239353195370, 1, 2, 3), false, true, -1 };
        yield return new object[] { new Matrix(1, 0, 0, 1, 2, 3), -90, 0, new Matrix(1, 0, -16331239353195370, 1, 2, 3), false, true, 1 };
        yield return new object[] { new Matrix(1, 0, 0, 1, 2, 3), 0, -180, new Matrix(1, 0, 0, 1, 2, 3), false, true, 1 };
        yield return new object[] { new Matrix(1, 0, 0, 1, 2, 3), 0, 0, new Matrix(1, 0, 0, 1, 2, 3), false, true, 1 };
        yield return new object[] { new Matrix(1, 0, 0, 1, 2, 3), 90, 180, new Matrix(1, -0, 16331239353195370, 1, 2, 3), false, true, 3 };
        yield return new object[] { new Matrix(1, 0, 0, 1, 2, 3), 90, 0, new Matrix(1, 0, 16331239353195370, 1, 2, 3), false, true, 1 };
        yield return new object[] { new Matrix(1, 0, 0, 1, 2, 3), 180, 0, new Matrix(1, 0, -0, 1, 2, 3), false, true, 1 };
        yield return new object[] { new Matrix(1, 0, 0, 1, 2, 3), 0, 180, new Matrix(1, -0, 0, 1, 2, 3), false, true, 1 };
        yield return new object[] { new Matrix(1, 0, 0, 1, 2, 3), 180, 180, new Matrix(1, -0, -0, 1, 2, 3), false, true, 1 };
        yield return new object[] { new Matrix(1, 0, 0, 1, 2, 3), 270, 270, new Matrix(1, 5443746451065123, 5443746451065123, 1, 2, 3), false, true, -2.963437542348412E+31 };
        yield return new object[] { new Matrix(1, 0, 0, 1, 2, 3), 180, 270, new Matrix(1, 5443746451065123, -0, 1, 2, 3), false, true, 1.66667 };
        yield return new object[] { new Matrix(1, 0, 0, 1, 2, 3), 270, 270, new Matrix(1, 5443746451065123, 5443746451065123, 1, 2, 3), false, true, -2.963437542348412E+31 };
        yield return new object[] { new Matrix(1, 0, 0, 1, 2, 3), -20, -30, new Matrix(1, -0.57735, -0.36397, 1, 2, 3), false, true, 0.78986 };
        yield return new object[] { new Matrix(1, 0, 0, 1, 2, 3), 20, 30, new Matrix(1, 0.57735, 0.36397, 1, 2, 3), false, true, 0.78986 };
        yield return new object[] { new Matrix(1, 0, 0, 1, 2, 3), -380, -390, new Matrix(1, -0.57735, -0.36397, 1, 2, 3), false, true, 0.78986 };
        yield return new object[] { new Matrix(1, 0, 0, 1, 2, 3), 380, 390, new Matrix(1, 0.57735, 0.36397, 1, 2, 3), false, true, 0.78986 };
        yield return new object[] { new Matrix(1, 0, 0, 1, 2, 3), -740, -750, new Matrix(1, -0.57735, -0.36397, 1, 2, 3), false, true, 0.78986 };
        yield return new object[] { new Matrix(1, 0, 0, 1, 2, 3), 740, 750, new Matrix(1, 0.57735, 0.36397, 1, 2, 3), false, true, 0.78986 };
        yield return new object[] { new Matrix(1, 0, 0, 1, 2, 3), 35, 0, new Matrix(1, 0, 0.70021, 1, 2, 3), false, true, 1 };
        yield return new object[] { new Matrix(1, 0, 0, 1, 2, 3), 0, 35, new Matrix(1, 0.70021, 0, 1, 2, 3), false, true, 1 };
        yield return new object[] { new Matrix(1, 0, 0, 1, 2, 3), -720, -720, new Matrix(1, -0, -0, 1, 2, 3), false, true, 1 };
        yield return new object[] { new Matrix(1, 0, 0, 1, 2, 3), -360, -360, new Matrix(1, -0, -0, 1, 2, 3), false, true, 1 };
        yield return new object[] { new Matrix(1, 0, 0, 1, 2, 3), 360, 360, new Matrix(1, 0, 0, 1, 2, 3), false, true, 1 };
        yield return new object[] { new Matrix(1, 0, 0, 1, 2, 3), 720, 720, new Matrix(1, 0, 0, 1, 2, 3), false, true, 1 };
        yield return new object[] { new Matrix(1, 0, 0, 1, 2, 3), 720, 720, new Matrix(1, 0, 0, 1, 2, 3), false, true, 1 };
        yield return new object[] { new Matrix(1, 0, 0, 1, 2, 3), 20, double.NaN, new Matrix(1, double.NaN, 0.36397, 1, 2, 3), false, true, double.NaN };
        yield return new object[] { new Matrix(1, 0, 0, 1, 2, 3), double.NaN, 30, new Matrix(1, 0.57735, double.NaN, 1, 2, 3), false, true, double.NaN };
        yield return new object[] { new Matrix(1, 0, 0, 1, 2, 3), double.NaN, double.NaN, new Matrix(1, double.NaN, double.NaN, 1, 2, 3), false, true, double.NaN };

        // Translated and scaled.
        yield return new object[] { new Matrix(2, 0, 0, 3, 1, 2), -90, -180, new Matrix(2, 0, -32662478706390740, 3, 1, 2), false, true, 18 };
        yield return new object[] { new Matrix(2, 0, 0, 3, 1, 2), -90, 180, new Matrix(2, -0, -32662478706390740, 3, 1, 2), false, true, -6 };
        yield return new object[] { new Matrix(2, 0, 0, 3, 1, 2), 90, -180, new Matrix(2, 0, 32662478706390740, 3, 1, 2), false, true, -6 };
        yield return new object[] { new Matrix(2, 0, 0, 3, 1, 2), -90, 0, new Matrix(2, 0, -32662478706390740, 3, 1, 2), false, true, 6 };
        yield return new object[] { new Matrix(2, 0, 0, 3, 1, 2), 0, -180, new Matrix(2, 0, 0, 3, 1, 2), false, true, 6 };
        yield return new object[] { new Matrix(2, 0, 0, 3, 1, 2), 0, 0, new Matrix(2, 0, 0, 3, 1, 2), false, true, 6 };
        yield return new object[] { new Matrix(2, 0, 0, 3, 1, 2), 90, 180, new Matrix(2, -0, 32662478706390740, 3, 1, 2), false, true, 18 };
        yield return new object[] { new Matrix(2, 0, 0, 3, 1, 2), 90, 0, new Matrix(2, 0, 32662478706390740, 3, 1, 2), false, true, 6 };
        yield return new object[] { new Matrix(2, 0, 0, 3, 1, 2), 180, 0, new Matrix(2, 0, -0, 3, 1, 2), false, true, 6 };
        yield return new object[] { new Matrix(2, 0, 0, 3, 1, 2), 0, 180, new Matrix(2, -0, 0, 3, 1, 2), false, true, 6 };
        yield return new object[] { new Matrix(2, 0, 0, 3, 1, 2), 180, 180, new Matrix(2, -0, -0, 3, 1, 2), false, true, 6 };
        yield return new object[] { new Matrix(2, 0, 0, 3, 1, 2), 270, 270, new Matrix(2, 16331239353195366, 10887492902130246, 3, 1, 2), false, true, -1.7780625254090473E+32 };
        yield return new object[] { new Matrix(2, 0, 0, 3, 1, 2), 180, 270, new Matrix(2, 16331239353195366, -0, 3, 1, 2), false, true, 10 };
        yield return new object[] { new Matrix(2, 0, 0, 3, 1, 2), 270, 270, new Matrix(2, 16331239353195366, 10887492902130246, 3, 1, 2), false, true, -1.7780625254090473E+32 };
        yield return new object[] { new Matrix(2, 0, 0, 3, 1, 2), -20, -30, new Matrix(2, -1.73205, -0.72794, 3, 1, 2), false, true, 4.73917 };
        yield return new object[] { new Matrix(2, 0, 0, 3, 1, 2), 20, 30, new Matrix(2, 1.73205, 0.72794, 3, 1, 2), false, true, 4.73917 };
        yield return new object[] { new Matrix(2, 0, 0, 3, 1, 2), -380, -390, new Matrix(2, -1.73205, -0.72794, 3, 1, 2), false, true, 4.73917 };
        yield return new object[] { new Matrix(2, 0, 0, 3, 1, 2), 380, 390, new Matrix(2, 1.73205, 0.72794, 3, 1, 2), false, true, 4.73917 };
        yield return new object[] { new Matrix(2, 0, 0, 3, 1, 2), -740, -750, new Matrix(2, -1.73205, -0.72794, 3, 1, 2), false, true, 4.73917 };
        yield return new object[] { new Matrix(2, 0, 0, 3, 1, 2), 740, 750, new Matrix(2, 1.73205, 0.72794, 3, 1, 2), false, true, 4.73917 };
        yield return new object[] { new Matrix(2, 0, 0, 3, 1, 2), 35, 0, new Matrix(2, 0, 1.40042, 3, 1, 2), false, true, 6 };
        yield return new object[] { new Matrix(2, 0, 0, 3, 1, 2), 0, 35, new Matrix(2, 2.10062, 0, 3, 1, 2), false, true, 6 };
        yield return new object[] { new Matrix(2, 0, 0, 3, 1, 2), -720, -720, new Matrix(2, 0, 0, 3, 1, 2), false, true, 6 };
        yield return new object[] { new Matrix(2, 0, 0, 3, 1, 2), -360, -360, new Matrix(2, 0, 0, 3, 1, 2), false, true, 6 };
        yield return new object[] { new Matrix(2, 0, 0, 3, 1, 2), 360, 360, new Matrix(2, 0, 0, 3, 1, 2), false, true, 6 };
        yield return new object[] { new Matrix(2, 0, 0, 3, 1, 2), 720, 720, new Matrix(2, 0, 0, 3, 1, 2), false, true, 6 };
        yield return new object[] { new Matrix(2, 0, 0, 3, 1, 2), 720, 720, new Matrix(2, 0, 0, 3, 1, 2), false, true, 6 };
        yield return new object[] { new Matrix(2, 0, 0, 3, 1, 2), 20, double.NaN, new Matrix(double.NaN, double.NaN, 0.72794, 3, 1, 2), false, true, double.NaN };
        yield return new object[] { new Matrix(2, 0, 0, 3, 1, 2), double.NaN, 30, new Matrix(2, 1.73205, double.NaN, double.NaN, 1, 2), false, true, double.NaN };
        yield return new object[] { new Matrix(2, 0, 0, 3, 1, 2), double.NaN, double.NaN, new Matrix(double.NaN, double.NaN, double.NaN, double.NaN, 1, 2), false, true, double.NaN };

        // Translated and skewed.
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 2, 3), -90, -180, new Matrix(1, 0.5, -16331239353195370, -8165619676597683, 2, 3), false, true, 3 };
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 2, 3), -90, 180, new Matrix(1, 0.5, -16331239353195370, -8165619676597683, 2, 3), false, false, 0 };
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 2, 3), 90, -180, new Matrix(1, 0.5, 16331239353195370, 8165619676597686, 2, 3), false, true, -1 };
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 2, 3), -90, 0, new Matrix(1, 0.5, -16331239353195370, -8165619676597683, 2, 3), false, true, 1 };
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 2, 3), 0, -180, new Matrix(1, 0.5, 0.75, 1, 2, 3), false, true, 0.625 };
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 2, 3), 0, 0, new Matrix(1, 0.5, 0.75, 1, 2, 3), false, true, 0.625 };
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 2, 3), 90, 180, new Matrix(1, 0.5, 16331239353195370, 8165619676597686, 2, 3), false, true, 2 };
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 2, 3), 90, 0, new Matrix(1, 0.5, 16331239353195370, 8165619676597686, 2, 3), false, true, 1 };
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 2, 3), 180, 0, new Matrix(1, 0.5, 0.75, 1, 2, 3), false, true, 0.625 };
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 2, 3), 0, 180, new Matrix(1, 0.5, 0.75, 1, 2, 3), false, true, 0.625 };
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 2, 3), 180, 180, new Matrix(1, 0.5, 0.75, 1, 2, 3), false, true, 0.625 };
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 2, 3), 270, 270, new Matrix(4082809838298843, 5443746451065124, 5443746451065124, 2721873225532562.5, 2, 3), false, true, -1.8521484639677582E+31 };
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 2, 3), 180, 270, new Matrix(4082809838298843, 5443746451065124, 0.75, 1, 2, 3), false, false, 0 };
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 2, 3), 270, 270, new Matrix(4082809838298843, 5443746451065124, 5443746451065124, 2721873225532562.5, 2, 3), false, true, -1.8521484639677582E+31 };
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 2, 3), -20, -30, new Matrix(0.56699, -0.07735, 0.38603, 0.81801, 2, 3), false, true, 0.49366 };
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 2, 3), 20, 30, new Matrix(1.43301, 1.07735, 1.11396, 1.18199, 2, 3), false, true, 0.49366 };
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 2, 3), -380, -390, new Matrix(0.56699, -0.07735, 0.38603, 0.81801, 2, 3), false, true, 0.49366 };
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 2, 3), 380, 390, new Matrix(1.43301, 1.07735, 1.11396, 1.18199, 2, 3), false, true, 0.49366 };
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 2, 3), -740, -750, new Matrix(0.56699, -0.07735, 0.38603, 0.81801, 2, 3), false, true, 0.49366 };
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 2, 3), 740, 750, new Matrix(1.43301, 1.07735, 1.11396, 1.18199, 2, 3), false, true, 0.49366 };
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 2, 3), 35, 0, new Matrix(1, 0.5, 1.45021, 1.3501, 2, 3), false, true, 0.625 };
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 2, 3), 0, 35, new Matrix(1.52516, 1.20021, 0.75, 1, 2, 3), false, true, 0.625 };
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 2, 3), -720, -720, new Matrix(1, 0.5, 0.75, 1, 2, 3), false, true, 0.625 };
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 2, 3), -360, -360, new Matrix(1, 0.5, 0.75, 1, 2, 3), false, true, 0.625 };
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 2, 3), 360, 360, new Matrix(1, 0.5, 0.75, 1, 2, 3), false, true, 0.625 };
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 2, 3), 720, 720, new Matrix(1, 0.5, 0.75, 1, 2, 3), false, true, 0.625 };
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 2, 3), 720, 720, new Matrix(1, 0.5, 0.75, 1, 2, 3), false, true, 0.625 };
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 2, 3), 20, double.NaN, new Matrix(double.NaN, double.NaN, 1.11396, 1.18199, 2, 3), false, true, double.NaN };
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 2, 3), double.NaN, 30, new Matrix(1.43301, 1.07735, double.NaN, double.NaN, 2, 3), false, true, double.NaN };
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 2, 3), double.NaN, double.NaN, new Matrix(double.NaN, double.NaN, double.NaN, double.NaN, 2, 3), false, true, double.NaN };

        // Skewed and scaled.
        yield return new object[] { new Matrix(2, 0.5, 0.75, 3, 0, 0), -90, -180, new Matrix(2, 0.5, -32662478706390740, -8165619676597682, 0, 0), false, true, 16 };
        yield return new object[] { new Matrix(2, 0.5, 0.75, 3, 0, 0), -90, 180, new Matrix(2, 0.5, -32662478706390740, -8165619676597682, 0, 0), false, true, -6 };
        yield return new object[] { new Matrix(2, 0.5, 0.75, 3, 0, 0), 90, -180, new Matrix(2, 0.5, 32662478706390740, 8165619676597687, 0, 0), false, true, -4 };
        yield return new object[] { new Matrix(2, 0.5, 0.75, 3, 0, 0), -90, 0, new Matrix(2, 0.5, -32662478706390740, -8165619676597682, 0, 0), false, true, 6 };
        yield return new object[] { new Matrix(2, 0.5, 0.75, 3, 0, 0), 0, -180, new Matrix(2, 0.5, 0.75, 3, 0, 0), false, true, 5.625 };
        yield return new object[] { new Matrix(2, 0.5, 0.75, 3, 0, 0), 0, 0, new Matrix(2, 0.5, 0.75, 3, 0, 0), false, true, 5.625 };
        yield return new object[] { new Matrix(2, 0.5, 0.75, 3, 0, 0), 90, 180, new Matrix(2, 0.5, 32662478706390740, 8165619676597687, 0, 0), false, true, 18 };
        yield return new object[] { new Matrix(2, 0.5, 0.75, 3, 0, 0), 90, 0, new Matrix(2, 0.5, 32662478706390740, 8165619676597687, 0, 0), false, true, 6 };
        yield return new object[] { new Matrix(2, 0.5, 0.75, 3, 0, 0), 180, 0, new Matrix(2, 0.5, 0.75, 3, 0, 0), false, true, 5.625 };
        yield return new object[] { new Matrix(2, 0.5, 0.75, 3, 0, 0), 0, 180, new Matrix(2, 0.5, 0.75, 3, 0, 0), false, true, 5.625 };
        yield return new object[] { new Matrix(2, 0.5, 0.75, 3, 0, 0), 180, 180, new Matrix(2, 0.5, 0.75, 3, 0, 0), false, true, 5.625 };
        yield return new object[] { new Matrix(2, 0.5, 0.75, 3, 0, 0), 270, 270, new Matrix(4082809838298843.5, 16331239353195366, 10887492902130246, 2721873225532564.5, 0, 0), false, true, -1.6669336175709816E+32 };
        yield return new object[] { new Matrix(2, 0.5, 0.75, 3, 0, 0), 180, 270, new Matrix(4082809838298843.5, 16331239353195366, 0.75, 3, 0, 0), false, true, 10 };
        yield return new object[] { new Matrix(2, 0.5, 0.75, 3, 0, 0), 270, 270, new Matrix(4082809838298843.5, 16331239353195366, 10887492902130246, 2721873225532564.5, 0, 0), false, true, -1.6669336175709816E+32 };
        yield return new object[] { new Matrix(2, 0.5, 0.75, 3, 0, 0), -20, -30, new Matrix(1.56699, -1.23205, 0.02206, 2.81801, 0, 0), false, true, 4.44297 };
        yield return new object[] { new Matrix(2, 0.5, 0.75, 3, 0, 0), 20, 30, new Matrix(2.43301, 2.23205, 1.47794, 3.18199, 0, 0), false, true, 4.44297 };
        yield return new object[] { new Matrix(2, 0.5, 0.75, 3, 0, 0), -380, -390, new Matrix(1.56699, -1.23205, 0.02206, 2.81801, 0, 0), false, true, 4.44297 };
        yield return new object[] { new Matrix(2, 0.5, 0.75, 3, 0, 0), 380, 390, new Matrix(2.43301, 2.23205, 1.47794, 3.18199, 0, 0), false, true, 4.44297 };
        yield return new object[] { new Matrix(2, 0.5, 0.75, 3, 0, 0), -740, -750, new Matrix(1.56699, -1.23205, 0.02206, 2.81801, 0, 0), false, true, 4.44297 };
        yield return new object[] { new Matrix(2, 0.5, 0.75, 3, 0, 0), 740, 750, new Matrix(2.43301, 2.23205, 1.47794, 3.18199, 0, 0), false, true, 4.44297 };
        yield return new object[] { new Matrix(2, 0.5, 0.75, 3, 0, 0), 35, 0, new Matrix(2, 0.5, 2.15042, 3.3501, 0, 0), false, true, 5.625 };
        yield return new object[] { new Matrix(2, 0.5, 0.75, 3, 0, 0), 0, 35, new Matrix(2.52516, 2.60062, 0.75, 3, 0, 0), false, true, 5.625 };
        yield return new object[] { new Matrix(2, 0.5, 0.75, 3, 0, 0), -720, -720, new Matrix(2, 0.5, 0.75, 3, 0, 0), false, true, 5.625 };
        yield return new object[] { new Matrix(2, 0.5, 0.75, 3, 0, 0), -360, -360, new Matrix(2, 0.5, 0.75, 3, 0, 0), false, true, 5.625 };
        yield return new object[] { new Matrix(2, 0.5, 0.75, 3, 0, 0), 360, 360, new Matrix(2, 0.5, 0.75, 3, 0, 0), false, true, 5.625 };
        yield return new object[] { new Matrix(2, 0.5, 0.75, 3, 0, 0), 720, 720, new Matrix(2, 0.5, 0.75, 3, 0, 0), false, true, 5.625 };
        yield return new object[] { new Matrix(2, 0.5, 0.75, 3, 0, 0), 720, 720, new Matrix(2, 0.5, 0.75, 3, 0, 0), false, true, 5.625 };
        yield return new object[] { new Matrix(2, 0.5, 0.75, 3, 0, 0), 20, double.NaN, new Matrix(double.NaN, double.NaN, 1.47794, 3.18199, 0, 0), false, true, double.NaN };
        yield return new object[] { new Matrix(2, 0.5, 0.75, 3, 0, 0), double.NaN, 30, new Matrix(2.43301, 2.23205, double.NaN, double.NaN, 0, 0), false, true, double.NaN };
        yield return new object[] { new Matrix(2, 0.5, 0.75, 3, 0, 0), double.NaN, double.NaN, new Matrix(double.NaN, double.NaN, double.NaN, double.NaN, 0, 0), false, true, double.NaN };

        // Complex.
        yield return new object[] { new Matrix(2, 3, 4, 5, 6, 7), -90, -180, new Matrix(2, 3, -32662478706390732, -48993718059586104, 6, 7), false, false, 0 };
        yield return new object[] { new Matrix(2, 3, 4, 5, 6, 7), -90, 180, new Matrix(2, 3, -32662478706390732, -48993718059586104, 6, 7), false, false, 0 };
        yield return new object[] { new Matrix(2, 3, 4, 5, 6, 7), 90, -180, new Matrix(2, 3, 32662478706390744, 48993718059586120, 6, 7), false, true, 16 };
        yield return new object[] { new Matrix(2, 3, 4, 5, 6, 7), -90, 0, new Matrix(2, 3, -32662478706390732, -48993718059586104, 6, 7), false, false, 0 };
        yield return new object[] { new Matrix(2, 3, 4, 5, 6, 7), 0, -180, new Matrix(2, 3, 4, 5, 6, 7), false, true, -2 };
        yield return new object[] { new Matrix(2, 3, 4, 5, 6, 7), 0, 0, new Matrix(2, 3, 4, 5, 6, 7), false, true, -2 };
        yield return new object[] { new Matrix(2, 3, 4, 5, 6, 7), 90, 180, new Matrix(2, 3, 32662478706390744, 48993718059586120, 6, 7), false, false, 0 };
        yield return new object[] { new Matrix(2, 3, 4, 5, 6, 7), 90, 0, new Matrix(2, 3, 32662478706390744, 48993718059586120, 6, 7), false, true, 16 };
        yield return new object[] { new Matrix(2, 3, 4, 5, 6, 7), 180, 0, new Matrix(2, 3, 4, 5, 6, 7), false, true, -2 };
        yield return new object[] { new Matrix(2, 3, 4, 5, 6, 7), 0, 180, new Matrix(2, 3, 4, 5, 6, 7), false, true, -2 };
        yield return new object[] { new Matrix(2, 3, 4, 5, 6, 7), 180, 180, new Matrix(2, 3, 4, 5, 6, 7), false, true, -2 };
        yield return new object[] { new Matrix(2, 3, 4, 5, 6, 7), 270, 270, new Matrix(21774985804260496, 27218732255325624, 10887492902130250, 16331239353195372, 6, 7), false, true, 5.926875084696823E+31 };
        yield return new object[] { new Matrix(2, 3, 4, 5, 6, 7), 180, 270, new Matrix(21774985804260496, 27218732255325624, 4, 5, 6, 7), false, true, 16 };
        yield return new object[] { new Matrix(2, 3, 4, 5, 6, 7), 270, 270, new Matrix(21774985804260496, 27218732255325624, 10887492902130250, 16331239353195372, 6, 7), false, true, 5.926875084696823E+31 };
        yield return new object[] { new Matrix(2, 3, 4, 5, 6, 7), -20, -30, new Matrix(-0.3094, 0.11325, 3.27206, 3.90809, 6, 7), false, true, -1.57972 };
        yield return new object[] { new Matrix(2, 3, 4, 5, 6, 7), 20, 30, new Matrix(4.3094, 5.88675, 4.72794, 6.09191, 6, 7), false, true, -1.57972 };
        yield return new object[] { new Matrix(2, 3, 4, 5, 6, 7), -380, -390, new Matrix(-0.3094, 0.11325, 3.27206, 3.90809, 6, 7), false, true, -1.57972 };
        yield return new object[] { new Matrix(2, 3, 4, 5, 6, 7), 380, 390, new Matrix(4.3094, 5.88675, 4.72794, 6.09191, 6, 7), false, true, -1.57972 };
        yield return new object[] { new Matrix(2, 3, 4, 5, 6, 7), -740, -750, new Matrix(-0.3094, 0.11325, 3.27206, 3.90809, 6, 7), false, true, -1.57972 };
        yield return new object[] { new Matrix(2, 3, 4, 5, 6, 7), 740, 750, new Matrix(4.3094, 5.88675, 4.72794, 6.09191, 6, 7), false, true, -1.57972 };
        yield return new object[] { new Matrix(2, 3, 4, 5, 6, 7), 35, 0, new Matrix(2, 3, 5.40042, 7.10062, 6, 7), false, true, -2 };
        yield return new object[] { new Matrix(2, 3, 4, 5, 6, 7), 0, 35, new Matrix(4.80083, 6.50104, 4, 5, 6, 7), false, true, -2 };
        yield return new object[] { new Matrix(2, 3, 4, 5, 6, 7), -720, -720, new Matrix(2, 3, 4, 5, 6, 7), false, true, -2 };
        yield return new object[] { new Matrix(2, 3, 4, 5, 6, 7), -360, -360, new Matrix(2, 3, 4, 5, 6, 7), false, true, -2 };
        yield return new object[] { new Matrix(2, 3, 4, 5, 6, 7), 360, 360, new Matrix(2, 3, 4, 5, 6, 7), false, true, -2 };
        yield return new object[] { new Matrix(2, 3, 4, 5, 6, 7), 720, 720, new Matrix(2, 3, 4, 5, 6, 7), false, true, -2 };
        yield return new object[] { new Matrix(2, 3, 4, 5, 6, 7), 720, 720, new Matrix(2, 3, 4, 5, 6, 7), false, true, -2 };
        yield return new object[] { new Matrix(2, 3, 4, 5, 6, 7), 20, double.NaN, new Matrix(double.NaN, double.NaN, 4.72794, 6.09191, 6, 7), false, true, double.NaN };
        yield return new object[] { new Matrix(2, 3, 4, 5, 6, 7), double.NaN, 30, new Matrix(4.3094, 5.88675, double.NaN, double.NaN, 6, 7), false, true, double.NaN };
        yield return new object[] { new Matrix(2, 3, 4, 5, 6, 7), double.NaN, double.NaN, new Matrix(double.NaN, double.NaN, double.NaN, double.NaN, 6, 7), false, true, double.NaN };

        // No inverse.
        yield return new object[] { new Matrix(0, 0, 0, 0, 0, 0), -90, -180, new Matrix(0, 0, 0, 0, 0, 0), false, false, 0 };
        yield return new object[] { new Matrix(0, 0, 0, 0, 0, 0), -90, 180, new Matrix(0, 0, 0, 0, 0, 0), false, false, 0 };
        yield return new object[] { new Matrix(0, 0, 0, 0, 0, 0), 90, -180, new Matrix(0, 0, 0, 0, 0, 0), false, false, 0 };
        yield return new object[] { new Matrix(0, 0, 0, 0, 0, 0), -90, 0, new Matrix(0, 0, 0, 0, 0, 0), false, false, 0 };
        yield return new object[] { new Matrix(0, 0, 0, 0, 0, 0), 0, -180, new Matrix(0, 0, 0, 0, 0, 0), false, false, 0 };
        yield return new object[] { new Matrix(0, 0, 0, 0, 0, 0), 0, 0, new Matrix(0, 0, 0, 0, 0, 0), false, false, 0 };
        yield return new object[] { new Matrix(0, 0, 0, 0, 0, 0), 90, 180, new Matrix(0, 0, 0, 0, 0, 0), false, false, 0 };
        yield return new object[] { new Matrix(0, 0, 0, 0, 0, 0), 90, 0, new Matrix(0, 0, 0, 0, 0, 0), false, false, 0 };
        yield return new object[] { new Matrix(0, 0, 0, 0, 0, 0), 180, 0, new Matrix(0, 0, 0, 0, 0, 0), false, false, 0 };
        yield return new object[] { new Matrix(0, 0, 0, 0, 0, 0), 0, 180, new Matrix(0, 0, 0, 0, 0, 0), false, false, 0 };
        yield return new object[] { new Matrix(0, 0, 0, 0, 0, 0), 180, 180, new Matrix(0, 0, 0, 0, 0, 0), false, false, 0 };
        yield return new object[] { new Matrix(0, 0, 0, 0, 0, 0), 270, 270, new Matrix(0, 0, 0, 0, 0, 0), false, false, 0 };
        yield return new object[] { new Matrix(0, 0, 0, 0, 0, 0), 180, 270, new Matrix(0, 0, 0, 0, 0, 0), false, false, 0 };
        yield return new object[] { new Matrix(0, 0, 0, 0, 0, 0), 270, 270, new Matrix(0, 0, 0, 0, 0, 0), false, false, 0 };
        yield return new object[] { new Matrix(0, 0, 0, 0, 0, 0), -20, -30, new Matrix(0, 0, 0, 0, 0, 0), false, false, 0 };
        yield return new object[] { new Matrix(0, 0, 0, 0, 0, 0), 20, 30, new Matrix(0, 0, 0, 0, 0, 0), false, false, 0 };
        yield return new object[] { new Matrix(0, 0, 0, 0, 0, 0), -380, -390, new Matrix(0, 0, 0, 0, 0, 0), false, false, 0 };
        yield return new object[] { new Matrix(0, 0, 0, 0, 0, 0), 380, 390, new Matrix(0, 0, 0, 0, 0, 0), false, false, 0 };
        yield return new object[] { new Matrix(0, 0, 0, 0, 0, 0), -740, -750, new Matrix(0, 0, 0, 0, 0, 0), false, false, 0 };
        yield return new object[] { new Matrix(0, 0, 0, 0, 0, 0), 740, 750, new Matrix(0, 0, 0, 0, 0, 0), false, false, 0 };
        yield return new object[] { new Matrix(0, 0, 0, 0, 0, 0), 35, 0, new Matrix(0, 0, 0, 0, 0, 0), false, false, 0 };
        yield return new object[] { new Matrix(0, 0, 0, 0, 0, 0), 0, 35, new Matrix(0, 0, 0, 0, 0, 0), false, false, 0 };
        yield return new object[] { new Matrix(0, 0, 0, 0, 0, 0), -720, -720, new Matrix(0, 0, 0, 0, 0, 0), false, false, 0 };
        yield return new object[] { new Matrix(0, 0, 0, 0, 0, 0), -360, -360, new Matrix(0, 0, 0, 0, 0, 0), false, false, 0 };
        yield return new object[] { new Matrix(0, 0, 0, 0, 0, 0), 360, 360, new Matrix(0, 0, 0, 0, 0, 0), false, false, 0 };
        yield return new object[] { new Matrix(0, 0, 0, 0, 0, 0), 720, 720, new Matrix(0, 0, 0, 0, 0, 0), false, false, 0 };
        yield return new object[] { new Matrix(0, 0, 0, 0, 0, 0), 720, 720, new Matrix(0, 0, 0, 0, 0, 0), false, false, 0 };
        yield return new object[] { new Matrix(0, 0, 0, 0, 0, 0), 20, double.NaN, new Matrix(double.NaN, double.NaN, 0, 0, 0, 0), false, true, double.NaN };
        yield return new object[] { new Matrix(0, 0, 0, 0, 0, 0), double.NaN, 30, new Matrix(0, 0, double.NaN, double.NaN, 0, 0), false, true, double.NaN };
        yield return new object[] { new Matrix(0, 0, 0, 0, 0, 0), double.NaN, double.NaN, new Matrix(double.NaN, double.NaN, double.NaN, double.NaN, 0, 0), false, true, double.NaN };
    }
    
    [Theory]
    [MemberData(nameof(SkewPrepend_TestData))]
    public void SkewPrepend_Invoke_Success(Matrix matrix, double skewX, double skewY, Matrix expected, bool expectedIsIdentity, bool expectedHasInverse, double expectedDeterminant)
    {
        matrix.SkewPrepend(skewX, skewY);
        Helpers.AssertEqualRounded(expected, matrix);
        Assert.Equal(expectedIsIdentity, matrix.IsIdentity);
        Assert.Equal(expectedHasInverse, matrix.HasInverse);
        Assert.Equal(expectedDeterminant, matrix.Determinant, precision: 5);
    }

    public static IEnumerable<object[]> ToString_TestData()
    {
        foreach (Matrix matrix in IdentityMatrices())
        {
            yield return new object[] { matrix, "Identity" };
        }

        yield return new object[] { new Matrix(0, 0, 0, 0, 0, 0), "0,0,0,0,0,0" };
        yield return new object[] { new Matrix(2, 3, 4, 5, 6, 7), "2,3,4,5,6,7" };
        yield return new object[] { new Matrix(2.2, 3.3, 4.4, 5.5, 6.6, 7.7), "2.2,3.3,4.4,5.5,6.6,7.7" };
        yield return new object[] { new Matrix(-1, -2, -3, -4, -5, -6), "-1,-2,-3,-4,-5,-6" };
    }

    [Theory]
    [MemberData(nameof(ToString_TestData))]
    public void ToString_Invoke_ReturnsExpected(Matrix matrix, string expected)
    {
        Assert.Equal(expected, matrix.ToString());
    }

    [Theory]
    [MemberData(nameof(ToString_TestData))]
    public void ToString_InvokeIFormatProviderInvariantCulture_ReturnsExpected(Matrix matrix, string expected)
    {
        Assert.Equal(expected, matrix.ToString(CultureInfo.InvariantCulture));
    }

    public static IEnumerable<object[]> ToString_IFormatProviderCustom_TestData()
    {
        foreach (Matrix matrix in IdentityMatrices())
        {
            yield return new object[] { matrix, "|", "Identity" };
            yield return new object[] { matrix, "|_", "Identity" };
            yield return new object[] { matrix, ",_", "Identity" };
            yield return new object[] { matrix, ",", "Identity" };
            yield return new object[] { matrix, ";", "Identity" };
            yield return new object[] { matrix, " ", "Identity" };
        }

        yield return new object[] { new Matrix(0, 0, 0, 0, 0, 0), "|", "0,0,0,0,0,0"  };
        yield return new object[] { new Matrix(0, 0, 0, 0, 0, 0), "|_", "0,0,0,0,0,0" };
        yield return new object[] { new Matrix(0, 0, 0, 0, 0, 0), ",_", "0;0;0;0;0;0" };
        yield return new object[] { new Matrix(0, 0, 0, 0, 0, 0), ",", "0;0;0;0;0;0" };
        yield return new object[] { new Matrix(0, 0, 0, 0, 0, 0), ";", "0,0,0,0,0,0" };
        yield return new object[] { new Matrix(0, 0, 0, 0, 0, 0), " ", "0,0,0,0,0,0" };
        
        yield return new object[] { new Matrix(2, 3, 4, 5, 6, 7), "|", "2,3,4,5,6,7" };
        yield return new object[] { new Matrix(2, 3, 4, 5, 6, 7), "|_", "2,3,4,5,6,7" };
        yield return new object[] { new Matrix(2, 3, 4, 5, 6, 7), ",_", "2;3;4;5;6;7" };
        yield return new object[] { new Matrix(2, 3, 4, 5, 6, 7), ",", "2;3;4;5;6;7" };
        yield return new object[] { new Matrix(2, 3, 4, 5, 6,  7), ";", "2,3,4,5,6,7" };
        yield return new object[] { new Matrix(2, 3, 4, 5, 6,  7), " ", "2,3,4,5,6,7" };

        yield return new object[] { new Matrix(2.2, 3.3, 4.4, 5.5, 6.6, 7.7), "|", "2|2,3|3,4|4,5|5,6|6,7|7" };
        yield return new object[] { new Matrix(2.2, 3.3, 4.4, 5.5, 6.6, 7.7), "|_", "2|_2,3|_3,4|_4,5|_5,6|_6,7|_7" };
        yield return new object[] { new Matrix(2.2, 3.3, 4.4, 5.5, 6.6, 7.7), ",_", "2,_2;3,_3;4,_4;5,_5;6,_6;7,_7" };
        yield return new object[] { new Matrix(2.2, 3.3, 4.4, 5.5, 6.6, 7.7), ",", "2,2;3,3;4,4;5,5;6,6;7,7" };
        yield return new object[] { new Matrix(2.2, 3.3, 4.4, 5.5, 6.6, 7.7), ";", "2;2,3;3,4;4,5;5,6;6,7;7" };
        yield return new object[] { new Matrix(2.2, 3.3, 4.4, 5.5, 6.6, 7.7), " ", "2 2,3 3,4 4,5 5,6 6,7 7" };

        yield return new object[] { new Matrix(-1, -2, -3, -4, -5, -6), "|", "-1,-2,-3,-4,-5,-6" };
        yield return new object[] { new Matrix(-1, -2, -3, -4, -5, -6), "|_", "-1,-2,-3,-4,-5,-6" };
        yield return new object[] { new Matrix(-1, -2, -3, -4, -5, -6), ",_", "-1;-2;-3;-4;-5;-6" };
        yield return new object[] { new Matrix(-1, -2, -3, -4, -5, -6), ",", "-1;-2;-3;-4;-5;-6" };
        yield return new object[] { new Matrix(-1, -2, -3, -4, -5, -6), ";", "-1,-2,-3,-4,-5,-6" };
        yield return new object[] { new Matrix(-1, -2, -3, -4, -5, -6), " ", "-1,-2,-3,-4,-5,-6" };
    }

    [Theory]
    [MemberData(nameof(ToString_IFormatProviderCustom_TestData))]
    public void ToString_InvokeIFormatProviderCustom_ReturnsExpected(Matrix matrix, string numberDecimalSeparator, string expected)
    {
        var culture = new CultureInfo("en-US");
        NumberFormatInfo formatInfo = culture.NumberFormat;
        formatInfo.NumberDecimalSeparator = numberDecimalSeparator;

        Assert.Equal(expected, matrix.ToString(formatInfo));
    }

    [Theory]
    [MemberData(nameof(ToString_TestData))]
    public void ToString_InvokeIFormattableInvariantCulture_ReturnsExpected(Matrix matrix, string expected)
    {
        IFormattable formattable = matrix;

        Assert.Equal(expected, formattable.ToString(null, null));
        Assert.Equal(expected, formattable.ToString(null, CultureInfo.InvariantCulture));
    }

    public static IEnumerable<object[]> ToString_IFormattableCustomFormat_TestData()
    {
        yield return new object[] { "|", "1|23,2|35,3|00,4|00,5|00,6|00" };
        yield return new object[] { "|_", "1|_23,2|_35,3|_00,4|_00,5|_00,6|_00" };
        yield return new object[] { ",_", "1,_23;2,_35;3,_00;4,_00;5,_00;6,_00" };
        yield return new object[] { ",", "1,23;2,35;3,00;4,00;5,00;6,00" };
        yield return new object[] { ";", "1;23,2;35,3;00,4;00,5;00,6;00" };
        yield return new object[] { " ", "1 23,2 35,3 00,4 00,5 00,6 00" };
    }

    [Theory]
    [MemberData(nameof(ToString_IFormattableCustomFormat_TestData))]
    public void ToString_InvokeIFormattableCustomFormat_ReturnsExpected(string numberDecimalSeparator, string expected)
    {
        var matrix = new Matrix(1.23456, 2.34567, 3, 4, 5, 6);
        IFormattable formattable = matrix;

        var culture = new CultureInfo("en-US");
        NumberFormatInfo formatInfo = culture.NumberFormat;
        formatInfo.NumberDecimalSeparator = numberDecimalSeparator;

        Assert.Equal(expected, formattable.ToString("F2", formatInfo));
    }

    public static IEnumerable<object[]> Transform_Point_TestData()
    {
        // Identity.
        foreach (Matrix matrix in MatrixTests.IdentityMatrices())
        {
            yield return new object[] { matrix, new Point(1, 2), new Point(1, 2) };
        }

        // Scale.
        yield return new object[] { new Matrix(2, 0, 0, 1, 0, 0), new Point(1, 2), new Point(2, 2) };
        yield return new object[] { new Matrix(1, 0, 0, 2, 0, 0), new Point(1, 2), new Point(1, 4) };
        yield return new object[] { new Matrix(0, 0, 0, 3, 0, 0), new Point(1, 2), new Point(0, 6) };
        yield return new object[] { new Matrix(2, 0, 0, 0, 0, 0), new Point(1, 2), new Point(2, 0) };
        yield return new object[] { new Matrix(0, 0, 0, 0, 0, 0), new Point(1, 2), new Point(0, 0) };
        yield return new object[] { new Matrix(2, 0, 0, 3, 0, 0), new Point(1, 2), new Point(2, 6) };
        yield return new object[] { new Matrix(-2, 0, 0, 3, 0, 0), new Point(1, 2), new Point(-2, 6) };
        yield return new object[] { new Matrix(2, 0, 0, -3, 0, 0), new Point(1, 2), new Point(2, -6) };
        yield return new object[] { new Matrix(-2, 0, 0, -3, 0, 0), new Point(1, 2), new Point(-2, -6) };

        // Skew.
        yield return new object[] { new Matrix(1, 2, 0, 1, 0, 0), new Point(1, 2), new Point(1, 4) };
        yield return new object[] { new Matrix(1, 0, 3, 1, 0, 0), new Point(1, 2), new Point(7, 2) };
        yield return new object[] { new Matrix(1, 2, 3, 1, 0, 0), new Point(1, 2), new Point(7, 4) };

        // Translate.
        yield return new object[] { new Matrix(1, 0, 0, 1, 2, 0), new Point(1, 2), new Point(3, 2) };
        yield return new object[] { new Matrix(1, 0, 0, 1, 0, 3), new Point(1, 2), new Point(1, 5) };
        yield return new object[] { new Matrix(1, 0, 0, 1, 2, 3), new Point(1, 2), new Point(3, 5) };
        yield return new object[] { new Matrix(1, 0, 0, 1, -2, 0), new Point(1, 2), new Point(-1, 2) };
        yield return new object[] { new Matrix(1, 0, 0, 1, 0, -3), new Point(1, 2), new Point(1, -1) };
        yield return new object[] { new Matrix(1, 0, 0, 1, -2, -3), new Point(1, 2), new Point(-1, -1) };

        // Translate + Scale.
        yield return new object[] { new Matrix(2, 0, 0, 3, 4, 5), new Point(1, 2), new Point(6, 11) };

        // Translate + Skew.
        yield return new object[] { new Matrix(1, 2, 3, 1, 4, 5), new Point(1, 2), new Point(11, 9) };

        // Skew + Scale.
        yield return new object[] { new Matrix(2, 3, 4, 5, 0, 0), new Point(1, 2), new Point(10, 13) };

        // Complex.
        yield return new object[] { new Matrix(2, 3, 4, 5, 6, 7), new Point(1, 2), new Point(16, 20) };
        yield return new object[] { new Matrix(0, 0, 0, 0, 0, 0), new Point(1, 2), new Point(0, 0) };
        yield return new object[] { new Matrix(2, 3, 4, 5, 6, 7), new Point(1.1, 2.2), new Point(17, 21.3) };

        // Other cases.
        yield return new object[] { Matrix.Identity, new Point(-1, -2), new Point(-1, -2) };
        yield return new object[] { new Matrix(), new Point(-1, -2), new Point(-1, -2) };
        yield return new object[] { new Matrix(1, 0, 0, 1, 0, 0), new Point(-1, -2), new Point(-1, -2) };
        yield return new object[] { new Matrix(2, 3, 4, 5, 6, 7), new Point(-1, -2), new Point(-4, -6) };

        yield return new object[] { Matrix.Identity, new Point(), new Point() };
        yield return new object[] { new Matrix(), new Point(), new Point() };
        yield return new object[] { new Matrix(1, 0, 0, 1, 0, 0), new Point(), new Point() };
        yield return new object[] { new Matrix(2, 3, 4, 5, 6, 7), new Point(), new Point(6, 7) };
    }

    [Theory]
    [MemberData(nameof(Transform_Point_TestData))]
    public void Transform_InvokePoint_ReturnsExpected(Matrix matrix, Point point, Point expected)
    {
        Assert.Equal(expected, matrix.Transform(point));
    }

    public static IEnumerable<object?[]> Transform_PointArray_TestData()
    {
        foreach (object[] testData in Transform_Point_TestData())
        {
            yield return new object[] { (Matrix)testData[0], new Point[] { (Point)testData[1] }, new Point[] { (Point)testData[2] } };
        }

        // Other cases.
        yield return new object[] { new Matrix(2, 3, 4, 5, 6, 7), Array.Empty<Point>(), Array.Empty<Point>() };
        yield return new object[] { new Matrix(2, 3, 4, 5, 6, 7), new Point[] { new Point(1, 2), new Point(2, 3) }, new Point[] { new Point(16, 20), new Point(22, 28) } };
        yield return new object?[] { new Matrix(2, 3, 4, 5, 6, 7), null, null };
    }

    [Theory]
    [MemberData(nameof(Transform_PointArray_TestData))]
    public void Transform_InvokePointArray_ReturnsExpected(Matrix matrix, Point[] points, Point[] expected)
    {
        matrix.Transform(points);
        Assert.Equal(expected, points);
    }

    public static IEnumerable<object[]> Transform_Vector_TestData()
    {
        // Identity.
        foreach (Matrix matrix in MatrixTests.IdentityMatrices())
        {
            yield return new object[] { matrix, new Vector(1, 2), new Vector(1, 2) };
        }

        // Scale.
        yield return new object[] { new Matrix(2, 0, 0, 1, 0, 0), new Vector(1, 2), new Vector(2, 2) };
        yield return new object[] { new Matrix(1, 0, 0, 2, 0, 0), new Vector(1, 2), new Vector(1, 4) };
        yield return new object[] { new Matrix(2, 0, 0, 3, 0, 0), new Vector(1, 2), new Vector(2, 6) };
        yield return new object[] { new Matrix(0, 0, 0, 3, 0, 0), new Vector(1, 2), new Vector(0, 6) };
        yield return new object[] { new Matrix(2, 0, 0, 0, 0, 0), new Vector(1, 2), new Vector(2, 0) };
        yield return new object[] { new Matrix(0, 0, 0, 0, 0, 0), new Vector(1, 2), new Vector(0, 0) };
        yield return new object[] { new Matrix(-2, 0, 0, 3, 0, 0), new Vector(1, 2), new Vector(-2, 6) };
        yield return new object[] { new Matrix(2, 0, 0, -3, 0, 0), new Vector(1, 2), new Vector(2, -6) };
        yield return new object[] { new Matrix(-2, 0, 0, -3, 0, 0), new Vector(1, 2), new Vector(-2, -6) };

        // Skew.
        yield return new object[] { new Matrix(1, 2, 0, 1, 0, 0), new Vector(1, 2), new Vector(1, 4) };
        yield return new object[] { new Matrix(1, 0, 3, 1, 0, 0), new Vector(1, 2), new Vector(7, 2) };
        yield return new object[] { new Matrix(1, 2, 3, 1, 0, 0), new Vector(1, 2), new Vector(7, 4) };

        // Translate.
        yield return new object[] { new Matrix(1, 0, 0, 1, 2, 0), new Vector(1, 2), new Vector(1, 2) };
        yield return new object[] { new Matrix(1, 0, 0, 1, 0, 3), new Vector(1, 2), new Vector(1, 2) };
        yield return new object[] { new Matrix(1, 0, 0, 1, 2, 3), new Vector(1, 2), new Vector(1, 2) };
        yield return new object[] { new Matrix(1, 0, 0, 1, -2, 0), new Vector(1, 2), new Vector(1, 2) };
        yield return new object[] { new Matrix(1, 0, 0, 1, 0, -3), new Vector(1, 2), new Vector(1, 2) };
        yield return new object[] { new Matrix(1, 0, 0, 1, -2, -3), new Vector(1, 2), new Vector(1, 2) };

        // Translate + Scale.
        yield return new object[] { new Matrix(2, 0, 0, 3, 4, 5), new Vector(1, 2), new Vector(2, 6) };

        // Translate + Skew.
        yield return new object[] { new Matrix(1, 2, 3, 1, 4, 5), new Vector(1, 2), new Vector(7, 4) };

        // Skew + Scale.
        yield return new object[] { new Matrix(2, 3, 4, 5, 0, 0), new Vector(1, 2), new Vector(10, 13) };

        // Complex.
        yield return new object[] { new Matrix(2, 3, 4, 5, 6, 7), new Vector(1, 2), new Vector(10, 13) };
        yield return new object[] { new Matrix(0, 0, 0, 0, 0, 0), new Vector(1, 2), new Vector(0, 0) };
        yield return new object[] { new Matrix(2, 3, 4, 5, 6, 7), new Vector(1.1, 2.2), new Vector(11, 14.3) };

        // Other cases.
        yield return new object[] { Matrix.Identity, new Vector(-1, -2), new Vector(-1, -2) };
        yield return new object[] { new Matrix(), new Vector(-1, -2), new Vector(-1, -2) };
        yield return new object[] { new Matrix(1, 0, 0, 1, 0, 0), new Vector(-1, -2), new Vector(-1, -2) };
        yield return new object[] { new Matrix(2, 3, 4, 5, 6, 7), new Vector(-1, -2), new Vector(-10, -13) };

        // Zero.
        yield return new object[] { Matrix.Identity, new Vector(0, 0), new Vector() };
        yield return new object[] { new Matrix(), new Vector(0, 0), new Vector() };
        yield return new object[] { new Matrix(1, 0, 0, 1, 0, 0), new Vector(0, 0), new Vector() };
        yield return new object[] { new Matrix(2, 3, 4, 5, 6, 7), new Vector(0, 0), new Vector() };

        // Default.
        yield return new object[] { Matrix.Identity, new Vector(), new Vector() };
        yield return new object[] { new Matrix(), new Vector(), new Vector() };
        yield return new object[] { new Matrix(1, 0, 0, 1, 0, 0), new Vector(), new Vector() };
        yield return new object[] { new Matrix(2, 3, 4, 5, 6, 7), new Vector(), new Vector() };
    }

    [Theory]
    [MemberData(nameof(Transform_Vector_TestData))]
    public void Transform_InvokeVector_ReturnsExpected(Matrix matrix, Vector vector, Vector expected)
    {
        Assert.Equal(expected, matrix.Transform(vector));
    }

    public static IEnumerable<object?[]> Transform_VectorArray_TestData()
    {
        foreach (object[] testData in Transform_Vector_TestData())
        {
            yield return new object[] { (Matrix)testData[0], new Vector[] { (Vector)testData[1] }, new Vector[] { (Vector)testData[2] } };
        }

        // Other cases.
        yield return new object[] { new Matrix(2, 3, 4, 5, 6, 7), Array.Empty<Vector>(), Array.Empty<Vector>() };
        yield return new object[] { new Matrix(2, 3, 4, 5, 6, 7), new Vector[] { new Vector(1, 2), new Vector(2, 3) }, new Vector[] { new Vector(10, 13), new Vector(16, 21) } };
        yield return new object?[] { new Matrix(2, 3, 4, 5, 6, 7), null, null };
    }

    [Theory]
    [MemberData(nameof(Transform_VectorArray_TestData))]
    public void Transform_InvokeVectorArray_ReturnsExpected(Matrix matrix, Vector[] vectors, Vector[] expected)
    {
        matrix.Transform(vectors);
        Assert.Equal(expected, vectors);
    }

    public static IEnumerable<object[]> Translate_TestData()
    {        // Identity
        foreach (Matrix matrix in IdentityMatrices())
        {
            yield return new object[] { matrix, -1, -2, new Matrix(1, 0, 0, 1, -1, -2) };
            yield return new object[] { matrix, -1, 0, new Matrix(1, 0, 0, 1, -1, 0) };
            yield return new object[] { matrix, 0, -2, new Matrix(1, 0, 0, 1, 0, -2) };
            yield return new object[] { matrix, 0, 0, new Matrix(1, 0, 0, 1, 0, 0) };
            yield return new object[] { matrix, 1, 0, new Matrix(1, 0, 0, 1, 1, 0) };
            yield return new object[] { matrix, 0, 2, new Matrix(1, 0, 0, 1, 0, 2) };
            yield return new object[] { matrix, 1, 2, new Matrix(1, 0, 0, 1, 1, 2) };
            yield return new object[] { matrix, 1.1, 2.2, new Matrix(1, 0, 0, 1, 1.1, 2.2) };
            yield return new object[] { matrix, -2, -3, new Matrix(1, 0, 0, 1, -2, -3) };
            yield return new object[] { matrix, double.NaN, 2, new Matrix(1, 0, 0, 1, double.NaN, 2) };
            yield return new object[] { matrix, 1, double.NaN, new Matrix(1, 0, 0, 1, 1, double.NaN) };
            yield return new object[] { matrix, double.NaN, double.NaN, new Matrix(1, 0, 0, 1, double.NaN, double.NaN) };
        }

        // Scaled.
        yield return new object[] { new Matrix(2, 0, 0, 3, 0, 0), -1, -2, new Matrix(2, 0, 0, 3, -1, -2) };
        yield return new object[] { new Matrix(2, 0, 0, 3, 0, 0), -1, 0, new Matrix(2, 0, 0, 3, -1, 0) };
        yield return new object[] { new Matrix(2, 0, 0, 3, 0, 0), 0, -2, new Matrix(2, 0, 0, 3, 0, -2) };
        yield return new object[] { new Matrix(2, 0, 0, 3, 0, 0), 0, 0, new Matrix(2, 0, 0, 3, 0, 0) };
        yield return new object[] { new Matrix(2, 0, 0, 3, 0, 0), 1, 0, new Matrix(2, 0, 0, 3, 1, 0) };
        yield return new object[] { new Matrix(2, 0, 0, 3, 0, 0), 0, 2, new Matrix(2, 0, 0, 3, 0, 2) };
        yield return new object[] { new Matrix(2, 0, 0, 3, 0, 0), 1, 2, new Matrix(2, 0, 0, 3, 1, 2) };
        yield return new object[] { new Matrix(2, 0, 0, 3, 0, 0), 1.1, 2.2, new Matrix(2, 0, 0, 3, 1.1, 2.2) };
        yield return new object[] { new Matrix(2, 0, 0, 3, 0, 0), -2, -3, new Matrix(2, 0, 0, 3, -2, -3) };
        yield return new object[] { new Matrix(2, 0, 0, 3, 0, 0), double.NaN, 2, new Matrix(2, 0, 0, 3, double.NaN, 2) };
        yield return new object[] { new Matrix(2, 0, 0, 3, 0, 0), 1, double.NaN, new Matrix(2, 0, 0, 3, 1, double.NaN) };
        yield return new object[] { new Matrix(2, 0, 0, 3, 0, 0), double.NaN, double.NaN, new Matrix(2, 0, 0, 3, double.NaN, double.NaN) };

        // Skewed.
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 0, 0), -1, -2, new Matrix(1, 0.5, 0.75, 1, -1, -2) };
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 0, 0), -1, 0, new Matrix(1, 0.5, 0.75, 1, -1, 0) };
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 0, 0), 0, -2, new Matrix(1, 0.5, 0.75, 1, 0, -2) };
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 0, 0), 0, 0, new Matrix(1, 0.5, 0.75, 1, 0, 0) };
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 0, 0), 1, 0, new Matrix(1, 0.5, 0.75, 1, 1, 0) };
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 0, 0), 0, 2, new Matrix(1, 0.5, 0.75, 1, 0, 2) };
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 0, 0), 1, 2, new Matrix(1, 0.5, 0.75, 1, 1, 2) };
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 0, 0), 1.1, 2.2, new Matrix(1, 0.5, 0.75, 1, 1.1, 2.2) };
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 0, 0), -2, -3, new Matrix(1, 0.5, 0.75, 1, -2, -3) };
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 0, 0), double.NaN, 2, new Matrix(1, 0.5, 0.75, 1, double.NaN, 2) };
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 0, 0), 1, double.NaN, new Matrix(1, 0.5, 0.75, 1, 1, double.NaN) };
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 0, 0), double.NaN, double.NaN, new Matrix(1, 0.5, 0.75, 1, double.NaN, double.NaN) };

        // Translated.
        yield return new object[] { new Matrix(1, 0, 0, 1, 2, 3), -1, -2, new Matrix(1, 0, 0, 1, 1, 1) };
        yield return new object[] { new Matrix(1, 0, 0, 1, 2, 3), -1, 0, new Matrix(1, 0, 0, 1, 1, 3) };
        yield return new object[] { new Matrix(1, 0, 0, 1, 2, 3), 0, -2, new Matrix(1, 0, 0, 1, 2, 1) };
        yield return new object[] { new Matrix(1, 0, 0, 1, 2, 3), 0, 0, new Matrix(1, 0, 0, 1, 2, 3) };
        yield return new object[] { new Matrix(1, 0, 0, 1, 2, 3), 1, 0, new Matrix(1, 0, 0, 1, 3, 3) };
        yield return new object[] { new Matrix(1, 0, 0, 1, 2, 3), 0, 2, new Matrix(1, 0, 0, 1, 2, 5) };
        yield return new object[] { new Matrix(1, 0, 0, 1, 2, 3), 1, 2, new Matrix(1, 0, 0, 1, 3, 5) };
        yield return new object[] { new Matrix(1, 0, 0, 1, 2, 3), 1.1, 2.2, new Matrix(1, 0, 0, 1, 3.1, 5.2) };
        yield return new object[] { new Matrix(1, 0, 0, 1, 2, 3), -2, -3, new Matrix(1, 0, 0, 1, 0, 0) };
        yield return new object[] { new Matrix(1, 0, 0, 1, 2, 3), double.NaN, 2, new Matrix(1, 0, 0, 1, double.NaN, 5) };
        yield return new object[] { new Matrix(1, 0, 0, 1, 2, 3), 1, double.NaN, new Matrix(1, 0, 0, 1, 3, double.NaN) };
        yield return new object[] { new Matrix(1, 0, 0, 1, 2, 3), double.NaN, double.NaN, new Matrix(1, 0, 0, 1, double.NaN, double.NaN) };

        // Translated and scaled.
        yield return new object[] { new Matrix(2, 0, 0, 3, 1, 2), -1, -2, new Matrix(2, 0, 0, 3, 0, 0) };
        yield return new object[] { new Matrix(2, 0, 0, 3, 1, 2), -1, 0, new Matrix(2, 0, 0, 3, 0, 2) };
        yield return new object[] { new Matrix(2, 0, 0, 3, 1, 2), 0, -2, new Matrix(2, 0, 0, 3, 1, 0) };
        yield return new object[] { new Matrix(2, 0, 0, 3, 1, 2), 0, 0, new Matrix(2, 0, 0, 3, 1, 2) };
        yield return new object[] { new Matrix(2, 0, 0, 3, 1, 2), 1, 0, new Matrix(2, 0, 0, 3, 2, 2) };
        yield return new object[] { new Matrix(2, 0, 0, 3, 1, 2), 0, 2, new Matrix(2, 0, 0, 3, 1, 4) };
        yield return new object[] { new Matrix(2, 0, 0, 3, 1, 2), 1, 2, new Matrix(2, 0, 0, 3, 2, 4) };
        yield return new object[] { new Matrix(2, 0, 0, 3, 1, 2), 1.1, 2.2, new Matrix(2, 0, 0, 3, 2.1, 4.2) };
        yield return new object[] { new Matrix(2, 0, 0, 3, 1, 2), -2, -3, new Matrix(2, 0, 0, 3, -1, -1) };
        yield return new object[] { new Matrix(2, 0, 0, 3, 1, 2), double.NaN, 2, new Matrix(2, 0, 0, 3, double.NaN, 4) };
        yield return new object[] { new Matrix(2, 0, 0, 3, 1, 2), 1, double.NaN, new Matrix(2, 0, 0, 3, 2, double.NaN) };
        yield return new object[] { new Matrix(2, 0, 0, 3, 1, 2), double.NaN, double.NaN, new Matrix(2, 0, 0, 3, double.NaN, double.NaN) };

        // Translated and skewed.
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 2, 3), -1, -2, new Matrix(1, 0.5, 0.75, 1, 1, 1) };
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 2, 3), -1, 0, new Matrix(1, 0.5, 0.75, 1, 1, 3) };
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 2, 3), 0, -2, new Matrix(1, 0.5, 0.75, 1, 2, 1) };
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 2, 3), 0, 0, new Matrix(1, 0.5, 0.75, 1, 2, 3) };
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 2, 3), 1, 0, new Matrix(1, 0.5, 0.75, 1, 3, 3) };
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 2, 3), 0, 2, new Matrix(1, 0.5, 0.75, 1, 2, 5) };
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 2, 3), 1, 2, new Matrix(1, 0.5, 0.75, 1, 3, 5) };
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 2, 3), 1.1, 2.2, new Matrix(1, 0.5, 0.75, 1, 3.1, 5.2) };
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 2, 3), -2, -3, new Matrix(1, 0.5, 0.75, 1, 0, 0) };
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 2, 3), double.NaN, 2, new Matrix(1, 0.5, 0.75, 1, double.NaN, 5) };
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 2, 3), 1, double.NaN, new Matrix(1, 0.5, 0.75, 1, 3, double.NaN) };
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 2, 3), double.NaN, double.NaN, new Matrix(1, 0.5, 0.75, 1, double.NaN, double.NaN) };

        // Skewed and scaled.
        yield return new object[] { new Matrix(2, 0.5, 0.75, 3, 0, 0), -1, -2, new Matrix(2, 0.5, 0.75, 3, -1, -2) };
        yield return new object[] { new Matrix(2, 0.5, 0.75, 3, 0, 0), -1, 0, new Matrix(2, 0.5, 0.75, 3, -1, 0) };
        yield return new object[] { new Matrix(2, 0.5, 0.75, 3, 0, 0), 0, -2, new Matrix(2, 0.5, 0.75, 3, 0, -2) };
        yield return new object[] { new Matrix(2, 0.5, 0.75, 3, 0, 0), 0, 0, new Matrix(2, 0.5, 0.75, 3, 0, 0) };
        yield return new object[] { new Matrix(2, 0.5, 0.75, 3, 0, 0), 1, 0, new Matrix(2, 0.5, 0.75, 3, 1, 0) };
        yield return new object[] { new Matrix(2, 0.5, 0.75, 3, 0, 0), 0, 2, new Matrix(2, 0.5, 0.75, 3, 0, 2) };
        yield return new object[] { new Matrix(2, 0.5, 0.75, 3, 0, 0), 1, 2, new Matrix(2, 0.5, 0.75, 3, 1, 2) };
        yield return new object[] { new Matrix(2, 0.5, 0.75, 3, 0, 0), 1.1, 2.2, new Matrix(2, 0.5, 0.75, 3, 1.1, 2.2) };
        yield return new object[] { new Matrix(2, 0.5, 0.75, 3, 0, 0), -2, -3, new Matrix(2, 0.5, 0.75, 3, -2, -3) };
        yield return new object[] { new Matrix(2, 0.5, 0.75, 3, 0, 0), double.NaN, 2, new Matrix(2, 0.5, 0.75, 3, double.NaN, 2) };
        yield return new object[] { new Matrix(2, 0.5, 0.75, 3, 0, 0), 1, double.NaN, new Matrix(2, 0.5, 0.75, 3, 1, double.NaN) };
        yield return new object[] { new Matrix(2, 0.5, 0.75, 3, 0, 0), double.NaN, double.NaN, new Matrix(2, 0.5, 0.75, 3, double.NaN, double.NaN) };

        // Complex.
        yield return new object[] { new Matrix(2, 3, 4, 5, 6, 7), -1, -2, new Matrix(2, 3, 4, 5, 5, 5) };
        yield return new object[] { new Matrix(2, 3, 4, 5, 6, 7), -1, 0, new Matrix(2, 3, 4, 5, 5, 7) };
        yield return new object[] { new Matrix(2, 3, 4, 5, 6, 7), 0, -2, new Matrix(2, 3, 4, 5, 6, 5) };
        yield return new object[] { new Matrix(2, 3, 4, 5, 6, 7), 0, 0, new Matrix(2, 3, 4, 5, 6, 7) };
        yield return new object[] { new Matrix(2, 3, 4, 5, 6, 7), 1, 0, new Matrix(2, 3, 4, 5, 7, 7) };
        yield return new object[] { new Matrix(2, 3, 4, 5, 6, 7), 0, 2, new Matrix(2, 3, 4, 5, 6, 9) };
        yield return new object[] { new Matrix(2, 3, 4, 5, 6, 7), 1, 2, new Matrix(2, 3, 4, 5, 7, 9) };
        yield return new object[] { new Matrix(2, 3, 4, 5, 6, 7), 1.1, 2.2, new Matrix(2, 3, 4, 5, 7.1, 9.2) };
        yield return new object[] { new Matrix(2, 3, 4, 5, 6, 7), -2, -3, new Matrix(2, 3, 4, 5, 4, 4) };
        yield return new object[] { new Matrix(2, 3, 4, 5, 6, 7), double.NaN, 2, new Matrix(2, 3, 4, 5, double.NaN, 9) };
        yield return new object[] { new Matrix(2, 3, 4, 5, 6, 7), 1, double.NaN, new Matrix(2, 3, 4, 5, 7, double.NaN) };
        yield return new object[] { new Matrix(2, 3, 4, 5, 6, 7), double.NaN, double.NaN, new Matrix(2, 3, 4, 5, double.NaN, double.NaN) };

        // No inverse.
        yield return new object[] { new Matrix(0, 0, 0, 0, 0, 0), -1, -2, new Matrix(0, 0, 0, 0, -1, -2) };
        yield return new object[] { new Matrix(0, 0, 0, 0, 0, 0), -1, 0, new Matrix(0, 0, 0, 0, -1, 0) };
        yield return new object[] { new Matrix(0, 0, 0, 0, 0, 0), 0, -2, new Matrix(0, 0, 0, 0, 0, -2) };
        yield return new object[] { new Matrix(0, 0, 0, 0, 0, 0), 0, 0, new Matrix(0, 0, 0, 0, 0, 0) };
        yield return new object[] { new Matrix(0, 0, 0, 0, 0, 0), 1, 0, new Matrix(0, 0, 0, 0, 1, 0) };
        yield return new object[] { new Matrix(0, 0, 0, 0, 0, 0), 0, 2, new Matrix(0, 0, 0, 0, 0, 2) };
        yield return new object[] { new Matrix(0, 0, 0, 0, 0, 0), 1, 2, new Matrix(0, 0, 0, 0, 1, 2) };
        yield return new object[] { new Matrix(0, 0, 0, 0, 0, 0), 1.1, 2.2, new Matrix(0, 0, 0, 0, 1.1, 2.2) };
        yield return new object[] { new Matrix(0, 0, 0, 0, 0, 0), -2, -3, new Matrix(0, 0, 0, 0, -2, -3) };
        yield return new object[] { new Matrix(0, 0, 0, 0, 0, 0), double.NaN, 2, new Matrix(0, 0, 0, 0, double.NaN, 2) };
        yield return new object[] { new Matrix(0, 0, 0, 0, 0, 0), 1, double.NaN, new Matrix(0, 0, 0, 0, 1, double.NaN) };
        yield return new object[] { new Matrix(0, 0, 0, 0, 0, 0), double.NaN, double.NaN, new Matrix(0, 0, 0, 0, double.NaN, double.NaN) };
    }

    [Theory]
    [MemberData(nameof(Translate_TestData))]
    public void Translate_Invoke_Success(Matrix matrix, double offsetX, double offsetY, Matrix expected)
    {
        matrix.Translate(offsetX, offsetY);
        Helpers.AssertEqualRounded(expected, matrix);
        Assert.Equal(expected.IsIdentity, matrix.IsIdentity);
        Assert.Equal(expected.HasInverse, matrix.HasInverse);
        Assert.Equal(expected.Determinant, matrix.Determinant, precision: 5);
    }

    public static IEnumerable<object[]> TranslatePrepend_TestData()
    {        
        // Identity
        foreach (Matrix matrix in IdentityMatrices())
        {
            yield return new object[] { matrix, -1, -2, new Matrix(1, 0, 0, 1, -1, -2) };
            yield return new object[] { matrix, -1, 0, new Matrix(1, 0, 0, 1, -1, 0) };
            yield return new object[] { matrix, 0, -2, new Matrix(1, 0, 0, 1, 0, -2) };
            yield return new object[] { matrix, 0, 0, new Matrix(1, 0, 0, 1, 0, 0) };
            yield return new object[] { matrix, 1, 0, new Matrix(1, 0, 0, 1, 1, 0) };
            yield return new object[] { matrix, 0, 2, new Matrix(1, 0, 0, 1, 0, 2) };
            yield return new object[] { matrix, 1, 2, new Matrix(1, 0, 0, 1, 1, 2) };
            yield return new object[] { matrix, 1.1, 2.2, new Matrix(1, 0, 0, 1, 1.1, 2.2) };
            yield return new object[] { matrix, -2, -3, new Matrix(1, 0, 0, 1, -2, -3) };
            yield return new object[] { matrix, double.NaN, 2, new Matrix(1, 0, 0, 1, double.NaN, 2) };
            yield return new object[] { matrix, 1, double.NaN, new Matrix(1, 0, 0, 1, 1, double.NaN) };
            yield return new object[] { matrix, double.NaN, double.NaN, new Matrix(1, 0, 0, 1, double.NaN, double.NaN) };
        }

        // Scaled.
        yield return new object[] { new Matrix(2, 0, 0, 3, 0, 0), -1, -2, new Matrix(2, 0, 0, 3, -2, -6) };
        yield return new object[] { new Matrix(2, 0, 0, 3, 0, 0), -1, 0, new Matrix(2, 0, 0, 3, -2, 0) };
        yield return new object[] { new Matrix(2, 0, 0, 3, 0, 0), 0, -2, new Matrix(2, 0, 0, 3, 0, -6) };
        yield return new object[] { new Matrix(2, 0, 0, 3, 0, 0), 0, 0, new Matrix(2, 0, 0, 3, 0, 0) };
        yield return new object[] { new Matrix(2, 0, 0, 3, 0, 0), 1, 0, new Matrix(2, 0, 0, 3, 2, 0) };
        yield return new object[] { new Matrix(2, 0, 0, 3, 0, 0), 0, 2, new Matrix(2, 0, 0, 3, 0, 6) };
        yield return new object[] { new Matrix(2, 0, 0, 3, 0, 0), 1, 2, new Matrix(2, 0, 0, 3, 2, 6) };
        yield return new object[] { new Matrix(2, 0, 0, 3, 0, 0), 1.1, 2.2, new Matrix(2, 0, 0, 3, 2.2, 6.6) };
        yield return new object[] { new Matrix(2, 0, 0, 3, 0, 0), -2, -3, new Matrix(2, 0, 0, 3, -4, -9) };
        yield return new object[] { new Matrix(2, 0, 0, 3, 0, 0), double.NaN, 2, new Matrix(2, 0, 0, 3, double.NaN, double.NaN) };
        yield return new object[] { new Matrix(2, 0, 0, 3, 0, 0), 1, double.NaN, new Matrix(2, 0, 0, 3, double.NaN, double.NaN) };
        yield return new object[] { new Matrix(2, 0, 0, 3, 0, 0), double.NaN, double.NaN, new Matrix(2, 0, 0, 3, double.NaN, double.NaN) };

        // Skewed.
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 0, 0), -1, -2, new Matrix(1, 0.5, 0.75, 1, -2.5, -2.5) };
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 0, 0), -1, 0, new Matrix(1, 0.5, 0.75, 1, -1, -0.5) };
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 0, 0), 0, -2, new Matrix(1, 0.5, 0.75, 1, -1.5, -2) };
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 0, 0), 0, 0, new Matrix(1, 0.5, 0.75, 1, 0, 0) };
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 0, 0), 1, 0, new Matrix(1, 0.5, 0.75, 1, 1, 0.5) };
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 0, 0), 0, 2, new Matrix(1, 0.5, 0.75, 1, 1.5, 2) };
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 0, 0), 1, 2, new Matrix(1, 0.5, 0.75, 1, 2.5, 2.5) };
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 0, 0), 1.1, 2.2, new Matrix(1, 0.5, 0.75, 1, 2.75, 2.75) };
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 0, 0), -2, -3, new Matrix(1, 0.5, 0.75, 1, -4.25, -4) };
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 0, 0), double.NaN, 2, new Matrix(1, 0.5, 0.75, 1, double.NaN, double.NaN) };
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 0, 0), 1, double.NaN, new Matrix(1, 0.5, 0.75, 1, double.NaN, double.NaN) };
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 0, 0), double.NaN, double.NaN, new Matrix(1, 0.5, 0.75, 1, double.NaN, double.NaN) };

        // Translated.
        yield return new object[] { new Matrix(1, 0, 0, 1, 2, 3), -1, -2, new Matrix(1, 0, 0, 1, 1, 1) };
        yield return new object[] { new Matrix(1, 0, 0, 1, 2, 3), -1, 0, new Matrix(1, 0, 0, 1, 1, 3) };
        yield return new object[] { new Matrix(1, 0, 0, 1, 2, 3), 0, -2, new Matrix(1, 0, 0, 1, 2, 1) };
        yield return new object[] { new Matrix(1, 0, 0, 1, 2, 3), 0, 0, new Matrix(1, 0, 0, 1, 2, 3) };
        yield return new object[] { new Matrix(1, 0, 0, 1, 2, 3), 1, 0, new Matrix(1, 0, 0, 1, 3, 3) };
        yield return new object[] { new Matrix(1, 0, 0, 1, 2, 3), 0, 2, new Matrix(1, 0, 0, 1, 2, 5) };
        yield return new object[] { new Matrix(1, 0, 0, 1, 2, 3), 1, 2, new Matrix(1, 0, 0, 1, 3, 5) };
        yield return new object[] { new Matrix(1, 0, 0, 1, 2, 3), 1.1, 2.2, new Matrix(1, 0, 0, 1, 3.1, 5.2) };
        yield return new object[] { new Matrix(1, 0, 0, 1, 2, 3), -2, -3, new Matrix(1, 0, 0, 1, 0, 0) };
        yield return new object[] { new Matrix(1, 0, 0, 1, 2, 3), double.NaN, 2, new Matrix(1, 0, 0, 1, double.NaN, 5) };
        yield return new object[] { new Matrix(1, 0, 0, 1, 2, 3), 1, double.NaN, new Matrix(1, 0, 0, 1, 3, double.NaN) };
        yield return new object[] { new Matrix(1, 0, 0, 1, 2, 3), double.NaN, double.NaN, new Matrix(1, 0, 0, 1, double.NaN, double.NaN) };

        // Translated and scaled.
        yield return new object[] { new Matrix(2, 0, 0, 3, 1, 2), -1, -2, new Matrix(2, 0, 0, 3, -1, -4) };
        yield return new object[] { new Matrix(2, 0, 0, 3, 1, 2), -1, 0, new Matrix(2, 0, 0, 3, -1, 2) };
        yield return new object[] { new Matrix(2, 0, 0, 3, 1, 2), 0, -2, new Matrix(2, 0, 0, 3, 1, -4) };
        yield return new object[] { new Matrix(2, 0, 0, 3, 1, 2), 0, 0, new Matrix(2, 0, 0, 3, 1, 2) };
        yield return new object[] { new Matrix(2, 0, 0, 3, 1, 2), 1, 0, new Matrix(2, 0, 0, 3, 3, 2) };
        yield return new object[] { new Matrix(2, 0, 0, 3, 1, 2), 0, 2, new Matrix(2, 0, 0, 3, 1, 8) };
        yield return new object[] { new Matrix(2, 0, 0, 3, 1, 2), 1, 2, new Matrix(2, 0, 0, 3, 3, 8) };
        yield return new object[] { new Matrix(2, 0, 0, 3, 1, 2), 1.1, 2.2, new Matrix(2, 0, 0, 3, 3.2, 8.6) };
        yield return new object[] { new Matrix(2, 0, 0, 3, 1, 2), -2, -3, new Matrix(2, 0, 0, 3, -3, -7) };
        yield return new object[] { new Matrix(2, 0, 0, 3, 1, 2), double.NaN, 2, new Matrix(2, 0, 0, 3, double.NaN, double.NaN) };
        yield return new object[] { new Matrix(2, 0, 0, 3, 1, 2), 1, double.NaN, new Matrix(2, 0, 0, 3, double.NaN, double.NaN) };
        yield return new object[] { new Matrix(2, 0, 0, 3, 1, 2), double.NaN, double.NaN, new Matrix(2, 0, 0, 3, double.NaN, double.NaN) };

        // Translated and skewed.
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 2, 3), -1, -2, new Matrix(1, 0.5, 0.75, 1, -0.5, 0.5) };
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 2, 3), -1, 0, new Matrix(1, 0.5, 0.75, 1, 1, 2.5) };
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 2, 3), 0, -2, new Matrix(1, 0.5, 0.75, 1, 0.5, 1) };
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 2, 3), 0, 0, new Matrix(1, 0.5, 0.75, 1, 2, 3) };
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 2, 3), 1, 0, new Matrix(1, 0.5, 0.75, 1, 3, 3.5) };
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 2, 3), 0, 2, new Matrix(1, 0.5, 0.75, 1, 3.5, 5) };
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 2, 3), 1, 2, new Matrix(1, 0.5, 0.75, 1, 4.5, 5.5) };
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 2, 3), 1.1, 2.2, new Matrix(1, 0.5, 0.75, 1, 4.75, 5.75) };
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 2, 3), -2, -3, new Matrix(1, 0.5, 0.75, 1, -2.25, -1) };
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 2, 3), double.NaN, 2, new Matrix(1, 0.5, 0.75, 1, double.NaN, double.NaN) };
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 2, 3), 1, double.NaN, new Matrix(1, 0.5, 0.75, 1, double.NaN, double.NaN) };
        yield return new object[] { new Matrix(1, 0.5, 0.75, 1, 2, 3), double.NaN, double.NaN, new Matrix(1, 0.5, 0.75, 1, double.NaN, double.NaN) };

        // Skewed and scaled.
        yield return new object[] { new Matrix(2, 0.5, 0.75, 3, 0, 0), -1, -2, new Matrix(2, 0.5, 0.75, 3, -3.5, -6.5) };
        yield return new object[] { new Matrix(2, 0.5, 0.75, 3, 0, 0), -1, 0, new Matrix(2, 0.5, 0.75, 3, -2, -0.5) };
        yield return new object[] { new Matrix(2, 0.5, 0.75, 3, 0, 0), 0, -2, new Matrix(2, 0.5, 0.75, 3, -1.5, -6) };
        yield return new object[] { new Matrix(2, 0.5, 0.75, 3, 0, 0), 0, 0, new Matrix(2, 0.5, 0.75, 3, 0, 0) };
        yield return new object[] { new Matrix(2, 0.5, 0.75, 3, 0, 0), 1, 0, new Matrix(2, 0.5, 0.75, 3, 2, 0.5) };
        yield return new object[] { new Matrix(2, 0.5, 0.75, 3, 0, 0), 0, 2, new Matrix(2, 0.5, 0.75, 3, 1.5, 6) };
        yield return new object[] { new Matrix(2, 0.5, 0.75, 3, 0, 0), 1, 2, new Matrix(2, 0.5, 0.75, 3, 3.5, 6.5) };
        yield return new object[] { new Matrix(2, 0.5, 0.75, 3, 0, 0), 1.1, 2.2, new Matrix(2, 0.5, 0.75, 3, 3.85, 7.15) };
        yield return new object[] { new Matrix(2, 0.5, 0.75, 3, 0, 0), -2, -3, new Matrix(2, 0.5, 0.75, 3, -6.25, -10) };
        yield return new object[] { new Matrix(2, 0.5, 0.75, 3, 0, 0), double.NaN, 2, new Matrix(2, 0.5, 0.75, 3, double.NaN, double.NaN) };
        yield return new object[] { new Matrix(2, 0.5, 0.75, 3, 0, 0), 1, double.NaN, new Matrix(2, 0.5, 0.75, 3, double.NaN, double.NaN) };
        yield return new object[] { new Matrix(2, 0.5, 0.75, 3, 0, 0), double.NaN, double.NaN, new Matrix(2, 0.5, 0.75, 3, double.NaN, double.NaN) };

        // Complex.
        yield return new object[] { new Matrix(2, 3, 4, 5, 6, 7), -1, -2, new Matrix(2, 3, 4, 5, -4, -6) };
        yield return new object[] { new Matrix(2, 3, 4, 5, 6, 7), -1, 0, new Matrix(2, 3, 4, 5, 4, 4) };
        yield return new object[] { new Matrix(2, 3, 4, 5, 6, 7), 0, -2, new Matrix(2, 3, 4, 5, -2, -3) };
        yield return new object[] { new Matrix(2, 3, 4, 5, 6, 7), 0, 0, new Matrix(2, 3, 4, 5, 6, 7) };
        yield return new object[] { new Matrix(2, 3, 4, 5, 6, 7), 1, 0, new Matrix(2, 3, 4, 5, 8, 10) };
        yield return new object[] { new Matrix(2, 3, 4, 5, 6, 7), 0, 2, new Matrix(2, 3, 4, 5, 14, 17) };
        yield return new object[] { new Matrix(2, 3, 4, 5, 6, 7), 1, 2, new Matrix(2, 3, 4, 5, 16, 20) };
        yield return new object[] { new Matrix(2, 3, 4, 5, 6, 7), 1.1, 2.2, new Matrix(2, 3, 4, 5, 17, 21.3) };
        yield return new object[] { new Matrix(2, 3, 4, 5, 6, 7), -2, -3, new Matrix(2, 3, 4, 5, -10, -14) };
        yield return new object[] { new Matrix(2, 3, 4, 5, 6, 7), double.NaN, 2, new Matrix(2, 3, 4, 5, double.NaN, double.NaN) };
        yield return new object[] { new Matrix(2, 3, 4, 5, 6, 7), 1, double.NaN, new Matrix(2, 3, 4, 5, double.NaN, double.NaN) };
        yield return new object[] { new Matrix(2, 3, 4, 5, 6, 7), double.NaN, double.NaN, new Matrix(2, 3, 4, 5, double.NaN, double.NaN) };

        // No inverse.
        yield return new object[] { new Matrix(0, 0, 0, 0, 0, 0), -1, -2, new Matrix(0, 0, 0, 0, 0, 0) };
        yield return new object[] { new Matrix(0, 0, 0, 0, 0, 0), -1, 0, new Matrix(0, 0, 0, 0, 0, 0) };
        yield return new object[] { new Matrix(0, 0, 0, 0, 0, 0), 0, -2, new Matrix(0, 0, 0, 0, 0, 0) };
        yield return new object[] { new Matrix(0, 0, 0, 0, 0, 0), 0, 0, new Matrix(0, 0, 0, 0, 0, 0) };
        yield return new object[] { new Matrix(0, 0, 0, 0, 0, 0), 1, 0, new Matrix(0, 0, 0, 0, 0, 0) };
        yield return new object[] { new Matrix(0, 0, 0, 0, 0, 0), 0, 2, new Matrix(0, 0, 0, 0, 0, 0) };
        yield return new object[] { new Matrix(0, 0, 0, 0, 0, 0), 1, 2, new Matrix(0, 0, 0, 0, 0, 0) };
        yield return new object[] { new Matrix(0, 0, 0, 0, 0, 0), 1.1, 2.2, new Matrix(0, 0, 0, 0, 0, 0) };
        yield return new object[] { new Matrix(0, 0, 0, 0, 0, 0), -2, -3, new Matrix(0, 0, 0, 0, 0, 0) };
        yield return new object[] { new Matrix(0, 0, 0, 0, 0, 0), double.NaN, 2, new Matrix(0, 0, 0, 0, double.NaN, double.NaN) };
        yield return new object[] { new Matrix(0, 0, 0, 0, 0, 0), 1, double.NaN, new Matrix(0, 0, 0, 0, double.NaN, double.NaN) };
        yield return new object[] { new Matrix(0, 0, 0, 0, 0, 0), double.NaN, double.NaN, new Matrix(0, 0, 0, 0, double.NaN, double.NaN) };
    }

    [Theory]
    [MemberData(nameof(TranslatePrepend_TestData))]
    public void TranslatePrepend_Invoke_Success(Matrix matrix, double offsetX, double offsetY, Matrix expected)
    {
        matrix.TranslatePrepend(offsetX, offsetY);
        Helpers.AssertEqualRounded(expected, matrix);
        Assert.Equal(expected.IsIdentity, matrix.IsIdentity);
        Assert.Equal(expected.HasInverse, matrix.HasInverse);
        Assert.Equal(expected.Determinant, matrix.Determinant, precision: 5);
    }

    public static IEnumerable<object[]> Scale_SetIdentity_TestData()
    {
        foreach (Matrix matrix in IdentityMatrices())
        {
            yield return new object[] { matrix, double.NegativeInfinity, false };
            yield return new object[] { matrix, double.MinValue, false };
            yield return new object[] { matrix, -1, false };
            yield return new object[] { matrix, double.NegativeZero, false };
            yield return new object[] { matrix, 0, false };
            yield return new object[] { matrix, 1, true };
            yield return new object[] { matrix, double.MaxValue, false };
            yield return new object[] { matrix, double.PositiveInfinity, false };
            yield return new object[] { matrix, double.NaN, false };
        }
    }

    [Theory]
    [MemberData(nameof(Scale_SetIdentity_TestData))]
    public void M11_SetIdentity_GetReturnsExpected(Matrix matrix, double value, bool expectedIsIdentity)
    {
        // Set.
        matrix.M11 = value;
        Assert.Equal(value, matrix.M11);
        Assert.Equal(0, matrix.M12);
        Assert.Equal(0, matrix.M21);
        Assert.Equal(1, matrix.M22);
        Assert.Equal(0, matrix.OffsetX);
        Assert.Equal(0, matrix.OffsetY);
        Assert.Equal(expectedIsIdentity, matrix.IsIdentity);

        // Set again.
        matrix.M11 = value;
        Assert.Equal(value, matrix.M11);
        Assert.Equal(0, matrix.M12);
        Assert.Equal(0, matrix.M21);
        Assert.Equal(1, matrix.M22);
        Assert.Equal(0, matrix.OffsetX);
        Assert.Equal(0, matrix.OffsetY);
        Assert.Equal(expectedIsIdentity, matrix.IsIdentity);
    }

    public static IEnumerable<object[]> Scale_SetToIdentity_TestData()
    {
        yield return new object[] { double.NegativeInfinity, false };
        yield return new object[] { double.MinValue, false };
        yield return new object[] { -1, false };
        yield return new object[] { double.NegativeZero, false };
        yield return new object[] { 0, false };
        yield return new object[] { 1, true };
        yield return new object[] { double.MaxValue, false };
        yield return new object[] { double.PositiveInfinity, false };
        yield return new object[] { double.NaN, false };
    }

    [Theory]
    [MemberData(nameof(Scale_SetToIdentity_TestData))]
    public void M11_SetScaleToIdentity_GetReturnsExpected(double value, bool expectedIsIdentity)
    {
        var matrix = new Matrix(2, 0, 0, 1, 0, 0)
        {
            // Set.
            M11 = value
        };
        Assert.Equal(value, matrix.M11);
        Assert.Equal(0, matrix.M12);
        Assert.Equal(0, matrix.M21);
        Assert.Equal(1, matrix.M22);
        Assert.Equal(0, matrix.OffsetX);
        Assert.Equal(0, matrix.OffsetY);
        Assert.Equal(expectedIsIdentity, matrix.IsIdentity);

        // Set again.
        matrix.M11 = value;
        Assert.Equal(value, matrix.M11);
        Assert.Equal(0, matrix.M12);
        Assert.Equal(0, matrix.M21);
        Assert.Equal(1, matrix.M22);
        Assert.Equal(0, matrix.OffsetX);
        Assert.Equal(0, matrix.OffsetY);
        Assert.Equal(expectedIsIdentity, matrix.IsIdentity);
    }

    public static IEnumerable<object[]> MatrixElement_Set_TestData()
    {
        yield return new object[] { double.NegativeInfinity };
        yield return new object[] { double.MinValue };
        yield return new object[] { -1 };
        yield return new object[] { double.NegativeZero };
        yield return new object[] { 0 };
        yield return new object[] { 1 };
        yield return new object[] { double.MaxValue };
        yield return new object[] { double.PositiveInfinity };
        yield return new object[] { double.NaN };
    }

    [Theory]
    [MemberData(nameof(MatrixElement_Set_TestData))]
    public void M11_Set_GetReturnsExpected(double value)
    {
        var matrix = new Matrix(2, 3, 4, 5, 6, 7)
        {
            // Set.
            M11 = value
        };
        Assert.Equal(value, matrix.M11);
        Assert.Equal(3, matrix.M12);
        Assert.Equal(4, matrix.M21);
        Assert.Equal(5, matrix.M22);
        Assert.Equal(6, matrix.OffsetX);
        Assert.Equal(7, matrix.OffsetY);
        Assert.False(matrix.IsIdentity);

        // Set again.
        matrix.M11 = value;
        Assert.Equal(value, matrix.M11);
        Assert.Equal(3, matrix.M12);
        Assert.Equal(4, matrix.M21);
        Assert.Equal(5, matrix.M22);
        Assert.Equal(6, matrix.OffsetX);
        Assert.Equal(7, matrix.OffsetY);
        Assert.False(matrix.IsIdentity);
    }

    public static IEnumerable<object[]> NonScale_SetIdentity_TestData()
    {
        foreach (Matrix matrix in IdentityMatrices())
        {
            yield return new object[] { matrix, double.NegativeInfinity, false };
            yield return new object[] { matrix, double.MinValue, false };
            yield return new object[] { matrix, -1, false };
            yield return new object[] { matrix, double.NegativeZero, true };
            yield return new object[] { matrix, 0, true };
            yield return new object[] { matrix, 1, false };
            yield return new object[] { matrix, double.MaxValue, false };
            yield return new object[] { matrix, double.PositiveInfinity, false };
            yield return new object[] { matrix, double.NaN, false };
        }
    }

    [Theory]
    [MemberData(nameof(NonScale_SetIdentity_TestData))]
    public void M12_SetIdentity_GetReturnsExpected(Matrix matrix, double value, bool expectedIsIdentity)
    {
        // Set.
        matrix.M12 = value;
        Assert.Equal(1, matrix.M11);
        Assert.Equal(value, matrix.M12);
        Assert.Equal(0, matrix.M21);
        Assert.Equal(1, matrix.M22);
        Assert.Equal(0, matrix.OffsetX);
        Assert.Equal(0, matrix.OffsetY);
        Assert.Equal(expectedIsIdentity, matrix.IsIdentity);

        // Set again.
        matrix.M12 = value;
        Assert.Equal(1, matrix.M11);
        Assert.Equal(value, matrix.M12);
        Assert.Equal(0, matrix.M21);
        Assert.Equal(1, matrix.M22);
        Assert.Equal(0, matrix.OffsetX);
        Assert.Equal(0, matrix.OffsetY);
        Assert.Equal(expectedIsIdentity, matrix.IsIdentity);
    }

    public static IEnumerable<object[]> NonScale_SetToIdentity_TestData()
    {
        yield return new object[] { double.NegativeInfinity, false };
        yield return new object[] { double.MinValue, false };
        yield return new object[] { -1, false };
        yield return new object[] { double.NegativeZero, true };
        yield return new object[] { 0, true };
        yield return new object[] { 1, false };
        yield return new object[] { double.MaxValue, false };
        yield return new object[] { double.PositiveInfinity, false };
        yield return new object[] { double.NaN, false };
    }

    [Theory]
    [MemberData(nameof(NonScale_SetToIdentity_TestData))]
    public void M12_SetScaleToIdentity_GetReturnsExpected(double value, bool expectedIsIdentity)
    {
        var matrix = new Matrix(1, 1, 0, 1, 0, 0)
        {
            // Set.
            M12 = value
        };
        Assert.Equal(1, matrix.M11);
        Assert.Equal(value, matrix.M12);
        Assert.Equal(0, matrix.M21);
        Assert.Equal(1, matrix.M22);
        Assert.Equal(0, matrix.OffsetX);
        Assert.Equal(0, matrix.OffsetY);
        Assert.Equal(expectedIsIdentity, matrix.IsIdentity);

        // Set again.
        matrix.M12 = value;
        Assert.Equal(1, matrix.M11);
        Assert.Equal(value, matrix.M12);
        Assert.Equal(0, matrix.M21);
        Assert.Equal(1, matrix.M22);
        Assert.Equal(0, matrix.OffsetX);
        Assert.Equal(0, matrix.OffsetY);
        Assert.Equal(expectedIsIdentity, matrix.IsIdentity);
    }

    [Theory]
    [MemberData(nameof(MatrixElement_Set_TestData))]
    public void M12_Set_GetReturnsExpected(double value)
    {
        var matrix = new Matrix(2, 3, 4, 5, 6, 7)
        {
            // Set.
            M12 = value
        };
        Assert.Equal(2, matrix.M11);
        Assert.Equal(value, matrix.M12);
        Assert.Equal(4, matrix.M21);
        Assert.Equal(5, matrix.M22);
        Assert.Equal(6, matrix.OffsetX);
        Assert.Equal(7, matrix.OffsetY);
        Assert.False(matrix.IsIdentity);

        // Set again.
        matrix.M12 = value;
        Assert.Equal(2, matrix.M11);
        Assert.Equal(value, matrix.M12);
        Assert.Equal(4, matrix.M21);
        Assert.Equal(5, matrix.M22);
        Assert.Equal(6, matrix.OffsetX);
        Assert.Equal(7, matrix.OffsetY);
        Assert.False(matrix.IsIdentity);
    }

    [Theory]
    [MemberData(nameof(NonScale_SetIdentity_TestData))]
    public void M21_SetIdentity_GetReturnsExpected(Matrix matrix, double value, bool expectedIsIdentity)
    {
        // Set.
        matrix.M21 = value;
        Assert.Equal(1, matrix.M11);
        Assert.Equal(0, matrix.M12);
        Assert.Equal(value, matrix.M21);
        Assert.Equal(1, matrix.M22);
        Assert.Equal(0, matrix.OffsetX);
        Assert.Equal(0, matrix.OffsetY);
        Assert.Equal(expectedIsIdentity, matrix.IsIdentity);

        // Set again.
        matrix.M21 = value;
        Assert.Equal(1, matrix.M11);
        Assert.Equal(0, matrix.M12);
        Assert.Equal(value, matrix.M21);
        Assert.Equal(1, matrix.M22);
        Assert.Equal(0, matrix.OffsetX);
        Assert.Equal(0, matrix.OffsetY);
        Assert.Equal(expectedIsIdentity, matrix.IsIdentity);
    }

    [Theory]
    [MemberData(nameof(NonScale_SetToIdentity_TestData))]
    public void M21_SetScaleToIdentity_GetReturnsExpected(double value, bool expectedIsIdentity)
    {
        var matrix = new Matrix(1, 0, 1, 1, 0, 0)
        {
            // Set.
            M21 = value
        };
        Assert.Equal(1, matrix.M11);
        Assert.Equal(0, matrix.M12);
        Assert.Equal(value, matrix.M21);
        Assert.Equal(1, matrix.M22);
        Assert.Equal(0, matrix.OffsetX);
        Assert.Equal(0, matrix.OffsetY);
        Assert.Equal(expectedIsIdentity, matrix.IsIdentity);

        // Set again.
        matrix.M21 = value;
        Assert.Equal(1, matrix.M11);
        Assert.Equal(0, matrix.M12);
        Assert.Equal(value, matrix.M21);
        Assert.Equal(1, matrix.M22);
        Assert.Equal(0, matrix.OffsetX);
        Assert.Equal(0, matrix.OffsetY);
        Assert.Equal(expectedIsIdentity, matrix.IsIdentity);
    }

    [Theory]
    [MemberData(nameof(MatrixElement_Set_TestData))]
    public void M21_Set_GetReturnsExpected(double value)
    {
        var matrix = new Matrix(2, 3, 4, 5, 6, 7)
        {
            // Set.
            M21 = value
        };
        Assert.Equal(2, matrix.M11);
        Assert.Equal(3, matrix.M12);
        Assert.Equal(value, matrix.M21);
        Assert.Equal(5, matrix.M22);
        Assert.Equal(6, matrix.OffsetX);
        Assert.Equal(7, matrix.OffsetY);
        Assert.False(matrix.IsIdentity);

        // Set again.
        matrix.M21 = value;
        Assert.Equal(2, matrix.M11);
        Assert.Equal(3, matrix.M12);
        Assert.Equal(value, matrix.M21);
        Assert.Equal(5, matrix.M22);
        Assert.Equal(6, matrix.OffsetX);
        Assert.Equal(7, matrix.OffsetY);
        Assert.False(matrix.IsIdentity);
    }

    [Theory]
    [MemberData(nameof(Scale_SetIdentity_TestData))]
    public void M22_SetIdentity_GetReturnsExpected(Matrix matrix, double value, bool expectedIsIdentity)
    {
        // Set.
        matrix.M22 = value;
        Assert.Equal(1, matrix.M11);
        Assert.Equal(0, matrix.M12);
        Assert.Equal(0, matrix.M21);
        Assert.Equal(value, matrix.M22);
        Assert.Equal(0, matrix.OffsetX);
        Assert.Equal(0, matrix.OffsetY);
        Assert.Equal(expectedIsIdentity, matrix.IsIdentity);

        // Set again.
        matrix.M22 = value;
        Assert.Equal(1, matrix.M11);
        Assert.Equal(0, matrix.M12);
        Assert.Equal(0, matrix.M21);
        Assert.Equal(value, matrix.M22);
        Assert.Equal(0, matrix.OffsetX);
        Assert.Equal(0, matrix.OffsetY);
        Assert.Equal(expectedIsIdentity, matrix.IsIdentity);
    }

    [Theory]
    [MemberData(nameof(Scale_SetToIdentity_TestData))]
    public void M22_SetScaleToIdentity_GetReturnsExpected(double value, bool expectedIsIdentity)
    {
        var matrix = new Matrix(1, 0, 0, 2, 0, 0)
        {
            // Set.
            M22 = value
        };
        Assert.Equal(1, matrix.M11);
        Assert.Equal(0, matrix.M12);
        Assert.Equal(0, matrix.M21);
        Assert.Equal(value, matrix.M22);
        Assert.Equal(0, matrix.OffsetX);
        Assert.Equal(0, matrix.OffsetY);
        Assert.Equal(expectedIsIdentity, matrix.IsIdentity);

        // Set again.
        matrix.M22 = value;
        Assert.Equal(1, matrix.M11);
        Assert.Equal(0, matrix.M12);
        Assert.Equal(0, matrix.M21);
        Assert.Equal(value, matrix.M22);
        Assert.Equal(0, matrix.OffsetX);
        Assert.Equal(0, matrix.OffsetY);
        Assert.Equal(expectedIsIdentity, matrix.IsIdentity);
    }

    [Theory]
    [MemberData(nameof(MatrixElement_Set_TestData))]
    public void M22_Set_GetReturnsExpected(double value)
    {
        var matrix = new Matrix(2, 3, 4, 5, 6, 7)
        {
            // Set.
            M22 = value
        };
        Assert.Equal(2, matrix.M11);
        Assert.Equal(3, matrix.M12);
        Assert.Equal(4, matrix.M21);
        Assert.Equal(value, matrix.M22);
        Assert.Equal(6, matrix.OffsetX);
        Assert.Equal(7, matrix.OffsetY);
        Assert.False(matrix.IsIdentity);

        // Set again.
        matrix.M22 = value;
        Assert.Equal(2, matrix.M11);
        Assert.Equal(3, matrix.M12);
        Assert.Equal(4, matrix.M21);
        Assert.Equal(value, matrix.M22);
        Assert.Equal(6, matrix.OffsetX);
        Assert.Equal(7, matrix.OffsetY);
        Assert.False(matrix.IsIdentity);
    }

    [Theory]
    [MemberData(nameof(NonScale_SetIdentity_TestData))]
    public void OffsetX_SetIdentity_GetReturnsExpected(Matrix matrix, double value, bool expectedIsIdentity)
    {
        // Set.
        matrix.OffsetX = value;
        Assert.Equal(1, matrix.M11);
        Assert.Equal(0, matrix.M12);
        Assert.Equal(0, matrix.M21);
        Assert.Equal(1, matrix.M22);
        Assert.Equal(value, matrix.OffsetX);
        Assert.Equal(0, matrix.OffsetY);
        Assert.Equal(expectedIsIdentity, matrix.IsIdentity);

        // Set again.
        matrix.OffsetX = value;
        Assert.Equal(1, matrix.M11);
        Assert.Equal(0, matrix.M12);
        Assert.Equal(0, matrix.M21);
        Assert.Equal(1, matrix.M22);
        Assert.Equal(value, matrix.OffsetX);
        Assert.Equal(0, matrix.OffsetY);
        Assert.Equal(expectedIsIdentity, matrix.IsIdentity);
    }

    [Theory]
    [MemberData(nameof(NonScale_SetToIdentity_TestData))]
    public void OffsetX_SetScaleToIdentity_GetReturnsExpected(double value, bool expectedIsIdentity)
    {
        var matrix = new Matrix(1, 0, 0, 1, 1, 0)
        {
            // Set.
            OffsetX = value
        };
        Assert.Equal(1, matrix.M11);
        Assert.Equal(0, matrix.M12);
        Assert.Equal(0, matrix.M21);
        Assert.Equal(1, matrix.M22);
        Assert.Equal(value, matrix.OffsetX);
        Assert.Equal(0, matrix.OffsetY);
        Assert.Equal(expectedIsIdentity, matrix.IsIdentity);

        // Set again.
        matrix.OffsetX = value;
        Assert.Equal(1, matrix.M11);
        Assert.Equal(0, matrix.M12);
        Assert.Equal(0, matrix.M21);
        Assert.Equal(1, matrix.M22);
        Assert.Equal(value, matrix.OffsetX);
        Assert.Equal(0, matrix.OffsetY);
        Assert.Equal(expectedIsIdentity, matrix.IsIdentity);
    }

    [Theory]
    [MemberData(nameof(MatrixElement_Set_TestData))]
    public void OffsetX_Set_GetReturnsExpected(double value)
    {
        var matrix = new Matrix(2, 3, 4, 5, 6, 7)
        {
            // Set.
            OffsetX = value
        };
        Assert.Equal(2, matrix.M11);
        Assert.Equal(3, matrix.M12);
        Assert.Equal(4, matrix.M21);
        Assert.Equal(5, matrix.M22);
        Assert.Equal(value, matrix.OffsetX);
        Assert.Equal(7, matrix.OffsetY);
        Assert.False(matrix.IsIdentity);

        // Set again.
        matrix.OffsetX = value;
        Assert.Equal(2, matrix.M11);
        Assert.Equal(3, matrix.M12);
        Assert.Equal(4, matrix.M21);
        Assert.Equal(5, matrix.M22);
        Assert.Equal(value, matrix.OffsetX);
        Assert.Equal(7, matrix.OffsetY);
        Assert.False(matrix.IsIdentity);
    }

    [Theory]
    [MemberData(nameof(NonScale_SetIdentity_TestData))]
    public void OffsetY_SetIdentity_GetReturnsExpected(Matrix matrix, double value, bool expectedIsIdentity)
    {
        // Set.
        matrix.OffsetY = value;
        Assert.Equal(1, matrix.M11);
        Assert.Equal(0, matrix.M12);
        Assert.Equal(0, matrix.M21);
        Assert.Equal(1, matrix.M22);
        Assert.Equal(0, matrix.OffsetX);
        Assert.Equal(value, matrix.OffsetY);
        Assert.Equal(expectedIsIdentity, matrix.IsIdentity);

        // Set again.
        matrix.OffsetY = value;
        Assert.Equal(1, matrix.M11);
        Assert.Equal(0, matrix.M12);
        Assert.Equal(0, matrix.M21);
        Assert.Equal(1, matrix.M22);
        Assert.Equal(0, matrix.OffsetX);
        Assert.Equal(value, matrix.OffsetY);
        Assert.Equal(expectedIsIdentity, matrix.IsIdentity);
    }

    [Theory]
    [MemberData(nameof(NonScale_SetToIdentity_TestData))]
    public void OffsetY_SetScaleToIdentity_GetReturnsExpected(double value, bool expectedIsIdentity)
    {
        var matrix = new Matrix(1, 0, 0, 1, 0, 1)
        {
            // Set.
            OffsetY = value
        };
        Assert.Equal(1, matrix.M11);
        Assert.Equal(0, matrix.M12);
        Assert.Equal(0, matrix.M21);
        Assert.Equal(1, matrix.M22);
        Assert.Equal(0, matrix.OffsetX);
        Assert.Equal(value, matrix.OffsetY);
        Assert.Equal(expectedIsIdentity, matrix.IsIdentity);

        // Set again.
        matrix.OffsetY = value;
        Assert.Equal(1, matrix.M11);
        Assert.Equal(0, matrix.M12);
        Assert.Equal(0, matrix.M21);
        Assert.Equal(1, matrix.M22);
        Assert.Equal(0, matrix.OffsetX);
        Assert.Equal(value, matrix.OffsetY);
        Assert.Equal(expectedIsIdentity, matrix.IsIdentity);
    }

    [Theory]
    [MemberData(nameof(MatrixElement_Set_TestData))]
    public void OffsetY_Set_GetReturnsExpected(double value)
    {
        var matrix = new Matrix(2, 3, 4, 5, 6, 7)
        {
            // Set.
            OffsetY = value
        };
        Assert.Equal(2, matrix.M11);
        Assert.Equal(3, matrix.M12);
        Assert.Equal(4, matrix.M21);
        Assert.Equal(5, matrix.M22);
        Assert.Equal(6, matrix.OffsetX);
        Assert.Equal(value, matrix.OffsetY);
        Assert.False(matrix.IsIdentity);

        // Set again.
        matrix.OffsetY = value;
        Assert.Equal(2, matrix.M11);
        Assert.Equal(3, matrix.M12);
        Assert.Equal(4, matrix.M21);
        Assert.Equal(5, matrix.M22);
        Assert.Equal(6, matrix.OffsetX);
        Assert.Equal(value, matrix.OffsetY);
        Assert.False(matrix.IsIdentity);
    }

    [Fact]
    public void TypeConverter_Get_ReturnsExpected()
    {
        Assert.IsType<MatrixConverter>(TypeDescriptor.GetConverter(typeof(Matrix)));
    }

    [Fact]
    public void ValueSerializer_Get_ReturnsExpected()
    {
        Assert.IsType<MatrixValueSerializer>(ValueSerializer.GetSerializerFor(typeof(Matrix)));
    }
}
