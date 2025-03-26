// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Windows.Media.Imaging;

public sealed class BitmapImageTests
{
    private static readonly byte[] s_png120DPI1x1 = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0x00, 0x00, 0x00, 0x0D, 0x49, 0x48, 0x44, 0x52,
                                                     0x00, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00, 0x01, 0x08, 0x02, 0x00, 0x00, 0x00, 0x90, 0x77, 0x53,
                                                     0xDE, 0x00, 0x00, 0x00, 0x01, 0x73, 0x52, 0x47, 0x42, 0x00, 0xAE, 0xCE, 0x1C, 0xE9, 0x00, 0x00,
                                                     0x00, 0x04, 0x67, 0x41, 0x4D, 0x41, 0x00, 0x00, 0xB1, 0x8F, 0x0B, 0xFC, 0x61, 0x05, 0x00, 0x00,
                                                     0x00, 0x09, 0x70, 0x48, 0x59, 0x73, 0x00, 0x00, 0x12, 0x74, 0x00, 0x00, 0x12, 0x74, 0x01, 0xDE,
                                                     0x66, 0x1F, 0x78, 0x00, 0x00, 0x00, 0x0C, 0x49, 0x44, 0x41, 0x54, 0x18, 0x57, 0x63, 0x60, 0x60,
                                                     0x60, 0x00, 0x00, 0x00, 0x04, 0x00, 0x01, 0x5C, 0xCD, 0xFF, 0x69, 0x00, 0x00, 0x00, 0x00, 0x49,
                                                     0x45, 0x4E, 0x44, 0xAE, 0x42, 0x60, 0x82];

    [WpfFact]
    public void Initialization_StreamSource_Invalid_ThrowsNotSupportedException()
    {
        using MemoryStream stream = new("invalid image"u8.ToArray());
        BitmapImage image = new BitmapImage();

        // InitializeInit

        image.BeginInit();

        image.StreamSource = stream;
        image.CacheOption = BitmapCacheOption.OnLoad;

        Assert.Throws<NotSupportedException>(image.EndInit);
    }

    [WpfFact]
    public void Initialization_StreamSource_PNG_Succeeds()
    {
        using MemoryStream stream = new(s_png120DPI1x1);

        BitmapImage image = new BitmapImage();

        // InitializeInit

        image.BeginInit();

        image.CacheOption = BitmapCacheOption.OnLoad;
        image.StreamSource = stream;

        image.EndInit();
        image.Freeze();

        // Check sources
        Assert.Null(image.UriSource);
        Assert.NotNull(image.StreamSource);

        // It is 1x1 at 120 DPI
        Assert.Equal(1, image.PixelWidth);
        Assert.Equal(1, image.PixelHeight);
    }

    [WpfFact]
    public void Initialization_UriSource_Invalid_ThrowsNotSupportedException()
    {
        string tempFile = Path.GetTempFileName();

        try
        {
            File.WriteAllBytes(tempFile, "invalid image"u8.ToArray());

            BitmapImage image = new BitmapImage();

            // InitializeInit (with OnLoad, so the file handle is closed)

            image.BeginInit();

            image.CacheOption = BitmapCacheOption.OnLoad;
            image.UriSource = new Uri(tempFile);

            Assert.Throws<NotSupportedException>(image.EndInit);
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [WpfFact]
    public void Initialization_UriSource_PNG_Succeeds()
    {
        string tempFile = Path.GetTempFileName();

        try
        {
            File.WriteAllBytes(tempFile, s_png120DPI1x1);

            BitmapImage image = new BitmapImage();

            // InitializeInit (with OnLoad, so the file handle is closed)

            image.BeginInit();

            image.CacheOption = BitmapCacheOption.OnLoad;
            image.UriSource = new Uri(tempFile);

            image.EndInit();
            image.Freeze();

            // Check sources
            Assert.NotNull(image.UriSource);
            Assert.Null(image.StreamSource);

            // It is 1x1 at 120 DPI
            Assert.Equal(1, image.PixelWidth);
            Assert.Equal(1, image.PixelHeight);
        }
        finally
        {
            File.Delete(tempFile);
        }
    }
}
