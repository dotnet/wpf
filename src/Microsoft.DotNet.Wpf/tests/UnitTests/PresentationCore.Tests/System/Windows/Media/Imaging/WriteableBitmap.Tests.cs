// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace System.Windows.Media.Imaging;

[Collection("WriteableBitmapTests")]
public sealed class WriteableBitmapTests
{
    // Under 2GB back-buffer (4 channels)
    [InlineData(128, 128, 96.0, 96.0)]
    [InlineData(256, 512, 96.0, 96.0)]
    [InlineData(256, 256, 120.0, 120.0)]
    [InlineData(512, 256, 120.0, 120.0)]
    [InlineData(10_000, 10_000, 96.0, 96.0, Skip = "Disabled to reduce working set")]
    [InlineData(20_000, 20_000, 96.0, 96.0, Skip = "Disabled to reduce working set")]
    // Over 2GB back-buffer (4 channels) -- NOTE: These tests shall not be run on x86 without PAE
    [InlineData(25_000, 25_000, 96.0, 96.0, Skip = "Disabled to reduce working set")]
    [InlineData(30_000, 30_000, 96.0, 96.0, Skip = "Disabled to reduce working set")]
    [InlineData(32_000, 32_000, 96.0, 96.0, Skip = "Disabled to reduce working set")]
    [Theory]
    public void Constructor_CreationSucceeds_HasCorrectParameters(int width, int height, double dpiX, double dpiY)
    {
        WriteableBitmap writeableBitmap = new(width, height, dpiX, dpiY, PixelFormats.Pbgra32, null);

        // Assert
        Assert.Equal(width, writeableBitmap.PixelWidth);
        Assert.Equal(height, writeableBitmap.PixelHeight);

        Assert.Equal(dpiX, writeableBitmap.DpiX);
        Assert.Equal(dpiY, writeableBitmap.DpiY);

        Assert.Equal(PixelFormats.Pbgra32, writeableBitmap.Format);
    }

    // Under 2GB back-buffer (4 channels)
    [InlineData(2_000, 2_000, 96.0, 96.0)]
    [InlineData(4_000, 4_000, 120, 120)]
    [InlineData(10_000, 10_000, 96.0, 96.0, Skip = "Disabled to reduce working set")]
    [InlineData(20_000, 20_000, 96.0, 96.0, Skip = "Disabled to reduce working set")]
    // Over 2GB back-buffer (4 channels) -- NOTE: These tests shall not be run on x86 without PAE
    [InlineData(25_000, 25_000, 96.0, 96.0, Skip = "Disabled to reduce working set")]
    [InlineData(32_000, 32_000, 96.0, 96.0, Skip = "Disabled to reduce working set")]
    [Theory]
    public void WritePixels_SmallRect_Safe_Succeeds(int width, int height, double dpiX, double dpiY)
    {
        const int tileSize = 500;
        const int channels = 4;

        // Create 1000x1000 rectangle with 4 channels, fill the rectangle with teal color
        byte[] smallRect = GC.AllocateUninitializedArray<byte>(tileSize * tileSize * channels);
        MemoryMarshal.Cast<byte, uint>(smallRect.AsSpan()).Fill(0xFF00E6FF);

        WriteableBitmap writeableBitmap = new(width, height, dpiX, dpiY, PixelFormats.Pbgra32, null);

        // Top-Left
        writeableBitmap.WritePixels(new Int32Rect(0, 0, tileSize, tileSize),
                                    smallRect, tileSize * channels, 0, 0);

        // Top-Right
        writeableBitmap.WritePixels(new Int32Rect(0, 0, tileSize, tileSize),
                                    smallRect, tileSize * channels, width - tileSize, 0);

        // Middle Rect
        writeableBitmap.WritePixels(new Int32Rect(0, 0, tileSize, tileSize),
                                    smallRect, tileSize * channels, (width - tileSize) / 2, (height - tileSize) / 2);

        // Bottom-Left
        writeableBitmap.WritePixels(new Int32Rect(0, 0, tileSize, tileSize),
                                    smallRect, tileSize * channels, 0, height - tileSize);

        // Bottom-Right
        writeableBitmap.WritePixels(new Int32Rect(0, 0, tileSize, tileSize),
                                    smallRect, tileSize * channels, width - tileSize, height - tileSize);
    }

    // Under 2GB back-buffer (4 channels)
    [InlineData(2_000, 2_000, 96.0, 96.0)]
    [InlineData(4_000, 4_000, 120, 120)]
    [InlineData(10_000, 10_000, 96.0, 96.0, Skip = "Disabled to reduce working set")]
    [InlineData(20_000, 20_000, 96.0, 96.0, Skip = "Disabled to reduce working set")]
    // Over 2GB back-buffer (4 channels) -- NOTE: These tests shall not be run on x86 without PAE
    [InlineData(25_000, 25_000, 96.0, 96.0, Skip = "Disabled to reduce working set")]
    [InlineData(32_000, 32_000, 96.0, 96.0, Skip = "Disabled to reduce working set")]
    [Theory]
    public unsafe void WritePixels_SmallRect_Unsafe_Succeeds(int width, int height, double dpiX, double dpiY)
    {
        const int tileSize = 500;
        const int channels = 4;

        // Create 1000x1000 rectangle with 4 channels, fill the rectangle with teal color
        Span<byte> smallRect = GC.AllocateUninitializedArray<byte>(tileSize * tileSize * channels, pinned: true);
        MemoryMarshal.Cast<byte, uint>(smallRect).Fill(0xFF00E6FF);

        WriteableBitmap writeableBitmap = new(width, height, dpiX, dpiY, PixelFormats.Pbgra32, null);

        // Top-Left
        writeableBitmap.WritePixels(new Int32Rect(0, 0, tileSize, tileSize), smallRect.AsNativePointer(),
                                    smallRect.Length, tileSize * channels, 0, 0);

        // Top-Right
        writeableBitmap.WritePixels(new Int32Rect(0, 0, tileSize, tileSize), smallRect.AsNativePointer(),
                                    smallRect.Length, tileSize * channels, width - tileSize, 0);

        // Middle Rect
        writeableBitmap.WritePixels(new Int32Rect(0, 0, tileSize, tileSize), smallRect.AsNativePointer(),
                                    smallRect.Length, tileSize * channels, (width - tileSize) / 2, (height - tileSize) / 2);

        // Bottom-Left
        writeableBitmap.WritePixels(new Int32Rect(0, 0, tileSize, tileSize), smallRect.AsNativePointer(),
                                    smallRect.Length, tileSize * channels, 0, height - tileSize);

        // Bottom-Right
        writeableBitmap.WritePixels(new Int32Rect(0, 0, tileSize, tileSize), smallRect.AsNativePointer(),
                                    smallRect.Length, tileSize * channels, width - tileSize, height - tileSize);
    }

    // Under 2GB back-buffer (4 channels)
    [InlineData(512, 512, 96.0, 96.0)]
    [InlineData(4_000, 4_000, 120, 120)]
    [InlineData(10_000, 10_000, 96.0, 96.0, Skip = "Disabled to reduce working set")]
    [InlineData(20_000, 20_000, 96.0, 96.0, Skip = "Disabled to reduce working set")]
    // Over 2GB back-buffer (4 channels) -- NOTE: These tests shall not be run on x86 without PAE
    [InlineData(25_000, 25_000, 96.0, 96.0, Skip = "Disabled to reduce working set")]
    [InlineData(32_000, 32_000, 96.0, 96.0, Skip = "Disabled to reduce working set")]
    [Theory]
    public void WritePixels_FullRect_Safe_Succeeds(int width, int height, double dpiX, double dpiY)
    {
        const int channels = 4;

        // Create same-sized rectangle with 4 channels, fill the rectangle with teal color
        // NOTE: We use uint[] over byte[] to avoid Array.MaxLength limit for single-dims on 2GB+ bitmaps
        uint[] bigRect = GC.AllocateUninitializedArray<uint>(width * height);
        Array.Fill(bigRect, 0xFF00E6FF);

        WriteableBitmap writeableBitmap = new(width, height, dpiX, dpiY, PixelFormats.Pbgra32, null);

        // Paint the full rect teal
        writeableBitmap.WritePixels(new Int32Rect(0, 0, width, height), bigRect, width * channels, 0, 0);
    }

    // Under 2GB back-buffer (4 channels)
    [InlineData(512, 512, 96.0, 96.0)]
    [InlineData(4_000, 4_000, 120, 120)]
    [InlineData(10_000, 10_000, 96.0, 96.0, Skip = "Disabled to reduce working set")]
    [InlineData(20_000, 20_000, 96.0, 96.0, Skip = "Disabled to reduce working set")]
    // Over 2GB back-buffer (4 channels) -- NOTE: These tests shall not be run on x86 without PAE
    [InlineData(25_000, 25_000, 96.0, 96.0, Skip = "Disabled to reduce working set")]
    [InlineData(32_000, 32_000, 96.0, 96.0, Skip = "Disabled to reduce working set")]
    [Theory]
    public unsafe void WritePixels_FullRect_Unsafe_Succeeds(int width, int height, double dpiX, double dpiY)
    {
        const int channels = 4;

        // Create same-sized rectangle with 4 channels, fill the rectangle with teal color
        // NOTE: We use uint[] over byte[] to avoid Array.MaxLength limit for single-dims on 2GB+ bitmaps
        Span<uint> bigRect = GC.AllocateUninitializedArray<uint>(width * height, pinned: true);
        bigRect.Fill(0xFF00E6FF);

        WriteableBitmap writeableBitmap = new(width, height, dpiX, dpiY, PixelFormats.Pbgra32, null);

        // Paint the full rect teal
        writeableBitmap.WritePixels(new Int32Rect(0, 0, width, height), bigRect.AsNativePointer(),
                                    bigRect.Length * channels, width * channels, 0, 0);
    }

    // Under 2GB back-buffer (4 channels)
    [InlineData(128, 128, 96.0, 96.0)]
    [InlineData(256, 512, 96.0, 96.0)]
    [InlineData(256, 256, 120.0, 120.0)]
    [InlineData(512, 256, 120.0, 120.0)]
    [InlineData(10_000, 10_000, 96.0, 96.0, Skip = "Disabled to reduce working set")]
    [InlineData(20_000, 20_000, 96.0, 96.0, Skip = "Disabled to reduce working set")]
    // Over 2GB back-buffer (4 channels) -- NOTE: These tests shall not be run on x86 without PAE
    [InlineData(25_000, 25_000, 96.0, 96.0, Skip = "Disabled to reduce working set")]
    [InlineData(32_000, 32_000, 96.0, 96.0, Skip = "Disabled to reduce working set")]
    [Theory]
    public void Clone_CopyPixels_Succeeds(int width, int height, double dpiX, double dpiY)
    {
        WriteableBitmap writeableBitmap = new(width, height, dpiX, dpiY, PixelFormats.Pbgra32, null);

        // Invoke bitmap copy
        BitmapSource bitmapSource = writeableBitmap.Clone();

        // Must succeed
        Assert.NotNull(bitmapSource);
    }
}

public static unsafe class SpanExtensions
{
    /// <summary>Retrieves the data pointer of the underlying <see cref="Span{T}"/> data reference.</summary>
    /// <param name="span">The <see cref="Span{T}"/> to retrieve a pointer to.</param>
    /// <returns>A <see cref="nuint"/> pointer of the underlying data reference.</returns>
    /// <remarks>The pointer reference is not pinned, use only on <see langword="fixed"/> buffers or <see langword="stackalloc"/> pointers.</remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static nint AsNativePointer<T>(this Span<T> span) => (nint)Unsafe.AsPointer(ref MemoryMarshal.GetReference(span));

}
