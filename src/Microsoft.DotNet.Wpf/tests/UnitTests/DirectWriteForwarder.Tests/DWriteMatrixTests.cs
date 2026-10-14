// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace MS.Internal.Text.TextInterface.Tests;

/// <summary>
/// Tests for <see cref="DWriteMatrix"/> value struct.
/// </summary>
public class DWriteMatrixTests
{
    [Fact]
    public void DefaultValues_ShouldBeZero()
    {
        var matrix = new DWriteMatrix();

        matrix.M11.Should().Be(0);
        matrix.M12.Should().Be(0);
        matrix.M21.Should().Be(0);
        matrix.M22.Should().Be(0);
        matrix.Dx.Should().Be(0);
        matrix.Dy.Should().Be(0);
    }

    [Fact]
    public void IdentityMatrix_Values()
    {
        // An identity matrix should have M11=1, M22=1, all others=0
        var identity = new DWriteMatrix
        {
            M11 = 1.0f,
            M12 = 0.0f,
            M21 = 0.0f,
            M22 = 1.0f,
            Dx = 0.0f,
            Dy = 0.0f
        };

        identity.M11.Should().Be(1.0f);
        identity.M12.Should().Be(0.0f);
        identity.M21.Should().Be(0.0f);
        identity.M22.Should().Be(1.0f);
        identity.Dx.Should().Be(0.0f);
        identity.Dy.Should().Be(0.0f);
    }

    [Fact]
    public void TranslationMatrix_Values()
    {
        // A translation matrix should have M11=1, M22=1, Dx and Dy set to translation values
        var translation = new DWriteMatrix
        {
            M11 = 1.0f,
            M12 = 0.0f,
            M21 = 0.0f,
            M22 = 1.0f,
            Dx = 100.0f,
            Dy = 50.0f
        };

        translation.Dx.Should().Be(100.0f);
        translation.Dy.Should().Be(50.0f);
    }

    [Fact]
    public void ScaleMatrix_Values()
    {
        // A scale matrix has M11 and M22 set to scale factors
        var scale = new DWriteMatrix
        {
            M11 = 2.0f,
            M12 = 0.0f,
            M21 = 0.0f,
            M22 = 0.5f,
            Dx = 0.0f,
            Dy = 0.0f
        };

        scale.M11.Should().Be(2.0f);
        scale.M22.Should().Be(0.5f);
    }

    [Fact]
    public void RotationMatrix_90Degrees()
    {
        // 90-degree rotation: M11=cos(90)=0, M12=sin(90)=1, M21=-sin(90)=-1, M22=cos(90)=0
        var rotation90 = new DWriteMatrix
        {
            M11 = 0.0f,
            M12 = 1.0f,
            M21 = -1.0f,
            M22 = 0.0f,
            Dx = 0.0f,
            Dy = 0.0f
        };

        rotation90.M11.Should().BeApproximately(0.0f, 0.0001f);
        rotation90.M12.Should().BeApproximately(1.0f, 0.0001f);
        rotation90.M21.Should().BeApproximately(-1.0f, 0.0001f);
        rotation90.M22.Should().BeApproximately(0.0f, 0.0001f);
    }

    [Theory]
    [InlineData(1.0f, 0.0f, 0.0f, 1.0f, 0.0f, 0.0f)]      // Identity
    [InlineData(2.0f, 0.0f, 0.0f, 2.0f, 0.0f, 0.0f)]      // Uniform scale 2x
    [InlineData(1.0f, 0.0f, 0.0f, 1.0f, 10.0f, 20.0f)]    // Translation
    [InlineData(-1.0f, 0.0f, 0.0f, 1.0f, 0.0f, 0.0f)]     // Horizontal flip
    [InlineData(1.0f, 0.0f, 0.0f, -1.0f, 0.0f, 0.0f)]     // Vertical flip
    public void AllFields_CanBeSetAndRetrieved(float m11, float m12, float m21, float m22, float dx, float dy)
    {
        var matrix = new DWriteMatrix
        {
            M11 = m11,
            M12 = m12,
            M21 = m21,
            M22 = m22,
            Dx = dx,
            Dy = dy
        };

        matrix.M11.Should().Be(m11);
        matrix.M12.Should().Be(m12);
        matrix.M21.Should().Be(m21);
        matrix.M22.Should().Be(m22);
        matrix.Dx.Should().Be(dx);
        matrix.Dy.Should().Be(dy);
    }

    [Fact]
    public void ShearMatrix_Values()
    {
        // A shear/skew matrix modifies M12 and M21
        var shear = new DWriteMatrix
        {
            M11 = 1.0f,
            M12 = 0.5f,   // Horizontal shear
            M21 = 0.25f,  // Vertical shear
            M22 = 1.0f,
            Dx = 0.0f,
            Dy = 0.0f
        };

        shear.M12.Should().Be(0.5f);
        shear.M21.Should().Be(0.25f);
    }
}
