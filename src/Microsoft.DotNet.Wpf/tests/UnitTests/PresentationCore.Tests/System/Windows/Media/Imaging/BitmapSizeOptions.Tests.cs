// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Windows.Media.Imaging;

public sealed class BitmapSizeOptionsTests
{
    [Fact]
    public void Init_FromEmptyOptions_ShouldReturnZeroSizes()
    {
        BitmapSizeOptions options = BitmapSizeOptions.FromEmptyOptions();

        Assert.Equal(0, options.PixelWidth);
        Assert.Equal(0, options.PixelHeight);

        // Rotation cannot be set on empty
        Assert.Equal(Rotation.Rotate0, options.Rotation);
        // AR is obviously preserved
        Assert.True(options.PreservesAspectRatio);
    }

    [Fact]
    public void Init_FromWidth_Zero_ThrowsArgumentOutOfRangeException()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => BitmapSizeOptions.FromWidth(0));
    }

    [Theory]
    [InlineData(1)]
    [InlineData(10)]
    [InlineData(50)]
    [InlineData(100)]
    [InlineData(200)]
    [InlineData(300)]
    [InlineData(400)]
    [InlineData(500)]
    [InlineData(600)]
    [InlineData(1024)]
    [InlineData(2048)]
    [InlineData(4096)]
    [InlineData(8192)]
    [InlineData(3)]
    [InlineData(7)]
    [InlineData(11)]
    [InlineData(17)]
    [InlineData(23)]
    [InlineData(37)]
    [InlineData(int.MaxValue)]
    public void Init_FromWidth_ShouldSetPixelWidth(int width)
    {
        BitmapSizeOptions options = BitmapSizeOptions.FromWidth(width);

        Assert.Equal(width, options.PixelWidth);
        Assert.Equal(0, options.PixelHeight);

        // Rotation cannot be set when specifying width/height
        Assert.Equal(Rotation.Rotate0, options.Rotation);
        // When only width/height is set, AR is preserved
        Assert.True(options.PreservesAspectRatio);
    }

    [Fact]
    public void Init_FromHeight_Zero_ThrowsArgumentOutOfRangeException()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => BitmapSizeOptions.FromHeight(0));
    }

    [Theory]
    [InlineData(1)]
    [InlineData(10)]
    [InlineData(50)]
    [InlineData(100)]
    [InlineData(200)]
    [InlineData(300)]
    [InlineData(400)]
    [InlineData(500)]
    [InlineData(600)]
    [InlineData(1024)]
    [InlineData(2048)]
    [InlineData(4096)]
    [InlineData(8192)]
    [InlineData(3)]
    [InlineData(7)]
    [InlineData(11)]
    [InlineData(17)]
    [InlineData(23)]
    [InlineData(37)]
    [InlineData(int.MaxValue)]
    public void Init_FromHeight_ShouldSetPixelHeight(int height)
    {
        BitmapSizeOptions options = BitmapSizeOptions.FromHeight(height);

        Assert.Equal(0, options.PixelWidth);
        Assert.Equal(height, options.PixelHeight);

        // Rotation cannot be set when specifying width/height
        Assert.Equal(Rotation.Rotate0, options.Rotation);
        // When only width/height is set, AR is preserved
        Assert.True(options.PreservesAspectRatio);
    }

    [Theory]
    [InlineData(0, 0)]
    [InlineData(666, 0)]
    [InlineData(0, 666)]
    public void Init_FromWidthAndHeight_ThrowsArgumentOutOfRangeException(int width, int height)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => BitmapSizeOptions.FromWidthAndHeight(width, height));
    }

    [Theory]
    [InlineData(1, 1)]
    [InlineData(10, 5)]
    [InlineData(50, 25)]
    [InlineData(100, 100)]
    [InlineData(200, 150)]
    [InlineData(300, 250)]
    [InlineData(400, 300)]
    [InlineData(500, 400)]
    [InlineData(600, 450)]
    [InlineData(1024, 768)]
    [InlineData(2048, 1536)]
    [InlineData(4096, 3072)]
    [InlineData(8192, 4096)]
    [InlineData(3, 2)]
    [InlineData(7, 4)]
    [InlineData(11, 6)]
    [InlineData(17, 8)]
    [InlineData(23, 10)]
    [InlineData(37, 12)]
    [InlineData(int.MaxValue, int.MaxValue)]
    public void Init_FromWidthAndHeight_ShouldSetBoth(int width, int height)
    {
        BitmapSizeOptions options = BitmapSizeOptions.FromWidthAndHeight(width, height);

        Assert.Equal(width, options.PixelWidth);
        Assert.Equal(height, options.PixelHeight);

        // Rotation cannot be set when specifying width/height
        Assert.Equal(Rotation.Rotate0, options.Rotation);
        // When width/height is both set, AR is not preserved
        Assert.False(options.PreservesAspectRatio);
    }

    [Theory]
    [InlineData(4)]
    [InlineData(89)]
    [InlineData(539)]
    [InlineData(int.MaxValue)]
    public void Init_FromRotation_Invalid_ThrowsArgumentException(int rotation)
    {
        Assert.Throws<ArgumentException>(() => BitmapSizeOptions.FromRotation((Rotation)rotation));
    }

    [Theory]
    [InlineData(Rotation.Rotate0)]
    [InlineData(Rotation.Rotate90)]
    [InlineData(Rotation.Rotate180)]
    [InlineData(Rotation.Rotate270)]
    public void Init_FromRotation_Valid_ShouldSetOnlyRotation(Rotation rotation)
    {
        BitmapSizeOptions options = BitmapSizeOptions.FromRotation(rotation);

        Assert.Equal(0, options.PixelWidth);
        Assert.Equal(0, options.PixelHeight);

        Assert.Equal(rotation, options.Rotation);

        Assert.True(options.PreservesAspectRatio);
    }
}
