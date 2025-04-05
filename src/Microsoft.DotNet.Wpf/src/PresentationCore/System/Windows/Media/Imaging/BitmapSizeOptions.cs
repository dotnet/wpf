// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using MS.Internal;

namespace System.Windows.Media.Imaging;

/// <summary>
/// Sizing options for an bitmap. The resulting bitmap will be scaled based on these options.
/// </summary>
public class BitmapSizeOptions
{
    /// <summary>
    /// Avoid construction of a plain object, use the factory methods instead.
    /// </summary>
    private BitmapSizeOptions() { }

    /// <summary>
    /// Whether or not to preserve the aspect ratio of the original bitmap.
    /// If so, then the <see cref="PixelWidth"/> and <see cref="PixelHeight"/>
    /// are both zero or at least one of them must be zero. The resulting bitmap
    /// is only guaranteed to have either its width or its height match the
    /// specified values. For example, if you want to specify the height,
    /// while preserving the aspect ratio for the width, then set the height
    /// to the desired value, and set the width to zero.
    ///
    /// If we are not to preserve aspect ratio, then both the
    /// specified width and the specified height are used, and
    /// the bitmap will be stretched to fit both those values.
    /// </summary>
    public bool PreservesAspectRatio { get; internal init; }

    /// <summary>
    /// PixelWidth of the resulting bitmap.  See description of
    /// PreserveAspectRatio for how this value is used.
    ///
    /// PixelWidth must be set to a value greater than zero to be valid.
    /// </summary>
    public int PixelWidth { get; internal init; }

    /// <summary>
    /// PixelHeight of the resulting bitmap.  See description of
    /// PreserveAspectRatio for how this value is used.
    ///
    /// PixelHeight must be set to a value greater than zero to be valid.
    /// </summary>
    public int PixelHeight { get; internal init; }

    /// <summary>
    /// Gets a value that represents the rotation angle that is applied to a bitmap.
    /// </summary>
    /// <remarks>Only increments of 90 degrees are supported.</remarks>
    public Rotation Rotation { get; internal init; }

    /// <summary>
    /// Constructs an identity <see cref="BitmapSizeOptions"/>.
    /// When passed to a TransformedBitmap, the input is the same as the output.
    /// </summary>
    /// <returns>An instance of <see cref="BitmapSizeOptions"/>.</returns>
    public static BitmapSizeOptions FromEmptyOptions()
    {
        BitmapSizeOptions sizeOptions = new BitmapSizeOptions
        {
            Rotation = Rotation.Rotate0,
            PreservesAspectRatio = true,
            PixelHeight = 0,
            PixelWidth = 0
        };

        return sizeOptions;
    }

    /// <summary>
    /// Constructs an instance of <see cref="BitmapSizeOptions"/> that preserves the aspect ratio
    /// of the source bitmap and enforces a height provided via <paramref name="pixelHeight"/>.
    /// </summary>
    /// <param name="pixelHeight">The height, in pixels, of the resulting bitmap.</param>
    /// <returns>An instance of <see cref="BitmapSizeOptions"/>.</returns>
    public static BitmapSizeOptions FromHeight(int pixelHeight)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(pixelHeight);

        BitmapSizeOptions sizeOptions = new BitmapSizeOptions
        {
            Rotation = Rotation.Rotate0,
            PreservesAspectRatio = true,
            PixelHeight = pixelHeight,
            PixelWidth = 0
        };

        return sizeOptions;
    }

    /// <summary>
    /// Constructs an instance of <see cref="BitmapSizeOptions"/> that preserves the aspect ratio
    /// of the source bitmap and enforces a width provided via <paramref name="pixelWidth"/>.
    /// </summary>
    /// <param name="pixelWidth">The width, in pixels, of the resulting bitmap.</param>
    public static BitmapSizeOptions FromWidth(int pixelWidth)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(pixelWidth);

        BitmapSizeOptions sizeOptions = new BitmapSizeOptions
        {
            Rotation = Rotation.Rotate0,
            PreservesAspectRatio = true,
            PixelWidth = pixelWidth,
            PixelHeight = 0
        };

        return sizeOptions;
    }

    /// <summary>
    /// Constructs an instance of <see cref="BitmapSizeOptions"/> that does not preserve the aspect ratio and
    /// instead uses the specified dimensions <paramref name="pixelWidth"/> x <paramref name="pixelHeight"/>.
    /// </summary>
    /// <param name="pixelWidth">Width of the resulting Bitmap</param>
    /// <param name="pixelHeight">Height of the resulting Bitmap</param>
    /// <returns>An instance of <see cref="BitmapSizeOptions"/>.</returns>
    public static BitmapSizeOptions FromWidthAndHeight(int pixelWidth, int pixelHeight)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(pixelWidth);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(pixelHeight);

        BitmapSizeOptions sizeOptions = new BitmapSizeOptions
        {
            Rotation = Rotation.Rotate0,
            PreservesAspectRatio = false,
            PixelWidth = pixelWidth,
            PixelHeight = pixelHeight
        };

        return sizeOptions;
    }

    /// <summary>
    /// Initializes an instance of <see cref="BitmapSizeOptions"/> that preserves the aspect ratio of the
    /// source bitmap and specifies an initial <see cref="Imaging.Rotation"/> to apply.
    /// </summary>
    /// <param name="rotation">The initial rotation value to apply. Only 90 degree increments are supported.</param>
    /// <returns>An instance of <see cref="BitmapSizeOptions"/>.</returns>
    public static BitmapSizeOptions FromRotation(Rotation rotation)
    {
        if (rotation is not (Rotation.Rotate0 or Rotation.Rotate90 or Rotation.Rotate180 or Rotation.Rotate270))
            throw new ArgumentException(SR.Image_SizeOptionsAngle, nameof(rotation));

        BitmapSizeOptions sizeOptions = new BitmapSizeOptions
        {
            Rotation = rotation,
            PreservesAspectRatio = true,
            PixelWidth = 0,
            PixelHeight = 0
        };

        return sizeOptions;
    }

    /// <summary>
    /// Note: In this method, newWidth, newHeight are not affected by the rotation angle.
    /// </summary>
    internal void GetScaledWidthAndHeight(uint width, uint height, out uint newWidth, out uint newHeight)
    {
        if (PixelWidth == 0 && PixelHeight != 0)
        {
            Debug.Assert(PreservesAspectRatio);

            newWidth = (uint)((PixelHeight * width) / height);
            newHeight = (uint)PixelHeight;
        }
        else if (PixelWidth != 0 && PixelHeight == 0)
        {
            Debug.Assert(PreservesAspectRatio);

            newWidth = (uint)PixelWidth;
            newHeight = (uint)((PixelWidth * height) / width);
        }
        else if (PixelWidth != 0 && PixelHeight != 0)
        {
            Debug.Assert(!PreservesAspectRatio);

            newWidth = (uint)PixelWidth;
            newHeight = (uint)PixelHeight;
        }
        else
        {
            newWidth = width;
            newHeight = height;
        }
    }

    /// <summary>
    /// Determines whether <see cref="PixelWidth"/> or <see cref="PixelHeight"/> are set to non-zero value.
    /// </summary>
    internal bool DoesScale
    {
        get => PixelWidth != 0 || PixelHeight != 0;
    }

    /// <summary>
    /// Converts the <see cref="Rotation"/> value to corresponding <see cref="WICBitmapTransformOptions"/>.
    /// </summary>
    internal WICBitmapTransformOptions WICTransformOptions
    {
        get
        {
            switch (Rotation)
            {
                case Rotation.Rotate0:
                    return WICBitmapTransformOptions.WICBitmapTransformRotate0;
                case Rotation.Rotate90:
                    return WICBitmapTransformOptions.WICBitmapTransformRotate90;
                case Rotation.Rotate180:
                    return WICBitmapTransformOptions.WICBitmapTransformRotate180;
                case Rotation.Rotate270:
                    return WICBitmapTransformOptions.WICBitmapTransformRotate270;
                default:
                    Debug.Assert(false);

                    // Fallback to default
                    return WICBitmapTransformOptions.WICBitmapTransformRotate0;
            }
        }
    }
}

