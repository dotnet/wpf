// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Windows.Win32.UI.WindowsAndMessaging;
using System.Windows.Media.Imaging;
using Windows.Win32.Graphics.Gdi;
using Windows.Win32;
using MS.Internal;

namespace System.Windows.Interop;

public sealed class InteropBitmapTests
{
    [InlineData(128, 128)]
    [InlineData(64, 64)]
    [InlineData(32, 32)]
    [InlineData(16, 16)]
    [WpfTheory]
    public void Init_FromHBitmap_EmptyRect_EmptySizeOptions_Succeeds(int width, int height)
    {
        Bitmap gdiBitmap = new(width, height);
        nint hBitmap = gdiBitmap.GetHbitmap();

        Assert.NotEqual(nint.Zero, hBitmap);

        // Same constructors but Imaging is public
        BitmapSource bitmapSource = Imaging.CreateBitmapSourceFromHBitmap(hBitmap, nint.Zero, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
        BitmapSource bitmapSourceInternal = new InteropBitmap(hBitmap, nint.Zero, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions(), WICBitmapAlphaChannelOption.WICBitmapUseAlpha);

        Assert.NotNull(bitmapSource);
        Assert.NotNull(bitmapSourceInternal);

        Assert.Equal(width, bitmapSource.PixelWidth);
        Assert.Equal(height, bitmapSource.PixelHeight);
        Assert.Equal(width, bitmapSourceInternal.PixelWidth);
        Assert.Equal(height, bitmapSourceInternal.PixelHeight);

        // Interop Bitmap is always created at 96 DPI
        Assert.Equal(bitmapSource.Width, bitmapSource.PixelWidth);
        Assert.Equal(bitmapSource.Height, bitmapSource.PixelHeight);
        Assert.Equal(bitmapSourceInternal.Width, bitmapSourceInternal.PixelWidth);
        Assert.Equal(bitmapSourceInternal.Height, bitmapSourceInternal.PixelHeight);

        // Free
        Assert.True(PInvoke.DeleteObject((HGDIOBJ)hBitmap));
        gdiBitmap.Dispose();
    }

    [InlineData(128, 128)]
    [InlineData(64, 64)]
    [InlineData(32, 32)]
    [InlineData(16, 16)]
    [WpfTheory]
    public void Init_FromHBitmap_SourceRect_EmptySizeOptions_Succeeds(int width, int height)
    {
        // We use half the size for the sourceRect to make this easy
        Int32Rect sourceRect = new(0, 0, width / 2, height / 2);
        Bitmap gdiBitmap = new(width, height);
        nint hBitmap = gdiBitmap.GetHbitmap();

        Assert.NotEqual(nint.Zero, hBitmap);

        // Same constructors but Imaging is public
        BitmapSource bitmapSource = Imaging.CreateBitmapSourceFromHBitmap(hBitmap, nint.Zero, sourceRect, BitmapSizeOptions.FromEmptyOptions());
        BitmapSource bitmapSourceInternal = new InteropBitmap(hBitmap, nint.Zero, sourceRect, BitmapSizeOptions.FromEmptyOptions(), WICBitmapAlphaChannelOption.WICBitmapUseAlpha);

        Assert.NotNull(bitmapSource);
        Assert.NotNull(bitmapSourceInternal);

        Assert.Equal(sourceRect.Width, bitmapSource.PixelWidth);
        Assert.Equal(sourceRect.Height, bitmapSource.PixelHeight);
        Assert.Equal(sourceRect.Width, bitmapSourceInternal.PixelWidth);
        Assert.Equal(sourceRect.Height, bitmapSourceInternal.PixelHeight);

        // Interop Bitmap is always created at 96 DPI
        Assert.Equal(bitmapSource.Width, bitmapSource.PixelWidth);
        Assert.Equal(bitmapSource.Height, bitmapSource.PixelHeight);
        Assert.Equal(bitmapSourceInternal.Width, bitmapSourceInternal.PixelWidth);
        Assert.Equal(bitmapSourceInternal.Height, bitmapSourceInternal.PixelHeight);

        // Free
        Assert.True(PInvoke.DeleteObject((HGDIOBJ)hBitmap));
        gdiBitmap.Dispose();
    }

    [InlineData(128, 128)]
    [InlineData(64, 64)]
    [InlineData(32, 32)]
    [InlineData(16, 16)]
    [WpfTheory]
    public void Init_FromHIcon_EmptyRect_EmptySizeOptions_Succeeds(int width, int height)
    {
        Bitmap gdiBitmap = new(width, height);
        nint hIcon = gdiBitmap.GetHicon();

        Assert.NotEqual(nint.Zero, hIcon);

        // Same constructors but Imaging is public
        BitmapSource bitmapSource = Imaging.CreateBitmapSourceFromHIcon(hIcon, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
        BitmapSource bitmapSourceInternal = new InteropBitmap(hIcon, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());

        Assert.NotNull(bitmapSource);
        Assert.NotNull(bitmapSourceInternal);

        Assert.Equal(width, bitmapSource.PixelWidth);
        Assert.Equal(height, bitmapSource.PixelHeight);
        Assert.Equal(width, bitmapSourceInternal.PixelWidth);
        Assert.Equal(height, bitmapSourceInternal.PixelHeight);

        // Interop Bitmap is always created at 96 DPI
        Assert.Equal(bitmapSource.Width, bitmapSource.PixelWidth);
        Assert.Equal(bitmapSource.Height, bitmapSource.PixelHeight);
        Assert.Equal(bitmapSourceInternal.Width, bitmapSourceInternal.PixelWidth);
        Assert.Equal(bitmapSourceInternal.Height, bitmapSourceInternal.PixelHeight);

        // Free
        Assert.True(PInvoke.DestroyIcon((HICON)hIcon));
        gdiBitmap.Dispose();
    }

    [InlineData(128, 128)]
    [InlineData(64, 64)]
    [InlineData(32, 32)]
    [InlineData(16, 16)]
    [WpfTheory]
    public void Init_FromHIcon_SourceRect_EmptySizeOptions_Succeeds(int width, int height)
    {
        // We use half the size for the sourceRect to make this easy
        Int32Rect sourceRect = new(0, 0, width / 2, height / 2);
        Bitmap gdiBitmap = new(width, height);
        nint hIcon = gdiBitmap.GetHicon();

        Assert.NotEqual(nint.Zero, hIcon);

        // Same constructors but Imaging is public
        BitmapSource bitmapSource = Imaging.CreateBitmapSourceFromHIcon(hIcon, sourceRect, BitmapSizeOptions.FromEmptyOptions());
        BitmapSource bitmapSourceInternal = new InteropBitmap(hIcon, sourceRect, BitmapSizeOptions.FromEmptyOptions());

        Assert.NotNull(bitmapSource);
        Assert.NotNull(bitmapSourceInternal);

        Assert.Equal(sourceRect.Width, bitmapSource.PixelWidth);
        Assert.Equal(sourceRect.Height, bitmapSource.PixelHeight);
        Assert.Equal(sourceRect.Width, bitmapSourceInternal.PixelWidth);
        Assert.Equal(sourceRect.Height, bitmapSourceInternal.PixelHeight);

        // Interop Bitmap is always created at 96 DPI
        Assert.Equal(bitmapSource.Width, bitmapSource.PixelWidth);
        Assert.Equal(bitmapSource.Height, bitmapSource.PixelHeight);
        Assert.Equal(bitmapSourceInternal.Width, bitmapSourceInternal.PixelWidth);
        Assert.Equal(bitmapSourceInternal.Height, bitmapSourceInternal.PixelHeight);

        // Free
        Assert.True(PInvoke.DestroyIcon((HICON)hIcon));
        gdiBitmap.Dispose();
    }

    [MemberData(nameof(Init_EmptyRect_BitmapSizeOptions_Data))]
    [WpfTheory]
    public void Init_FromHBitmap_EmptyRect_BitmapSizeOptions(int sourceWidth, int sourceHeight, int expectedWidth, int expectedHeight, BitmapSizeOptions? bitmapSizeOptions)
    {
        Bitmap gdiBitmap = new(sourceWidth, sourceHeight);
        nint hBitmap = gdiBitmap.GetHbitmap();

        Assert.NotEqual(nint.Zero, hBitmap);

        // Same constructors but Imaging is public
        BitmapSource bitmapSource = Imaging.CreateBitmapSourceFromHBitmap(hBitmap, nint.Zero, Int32Rect.Empty, bitmapSizeOptions);
        BitmapSource bitmapSourceInternal = new InteropBitmap(hBitmap, nint.Zero, Int32Rect.Empty, bitmapSizeOptions, WICBitmapAlphaChannelOption.WICBitmapUseAlpha);

        Assert.NotNull(bitmapSource);
        Assert.NotNull(bitmapSourceInternal);

        Assert.Equal(expectedWidth, bitmapSource.PixelWidth);
        Assert.Equal(expectedHeight, bitmapSource.PixelHeight);
        Assert.Equal(expectedWidth, bitmapSourceInternal.PixelWidth);
        Assert.Equal(expectedHeight, bitmapSourceInternal.PixelHeight);

        // Interop Bitmap is always created at 96 DPI
        Assert.Equal(bitmapSource.Width, bitmapSource.PixelWidth);
        Assert.Equal(bitmapSource.Height, bitmapSource.PixelHeight);
        Assert.Equal(bitmapSourceInternal.Width, bitmapSourceInternal.PixelWidth);
        Assert.Equal(bitmapSourceInternal.Height, bitmapSourceInternal.PixelHeight);

        // Free
        Assert.True(PInvoke.DeleteObject((HGDIOBJ)hBitmap));
        gdiBitmap.Dispose();
    }

    [MemberData(nameof(Init_EmptyRect_BitmapSizeOptions_Data))]
    [WpfTheory]
    public void Init_FromHIcon_EmptyRect_BitmapSizeOptions(int sourceWidth, int sourceHeight, int expectedWidth, int expectedHeight, BitmapSizeOptions? bitmapSizeOptions)
    {
        Bitmap gdiBitmap = new(sourceWidth, sourceHeight);
        nint hIcon = gdiBitmap.GetHicon();

        Assert.NotEqual(nint.Zero, hIcon);

        // Same constructors but Imaging is public
        BitmapSource bitmapSource = Imaging.CreateBitmapSourceFromHIcon(hIcon, Int32Rect.Empty, bitmapSizeOptions);
        BitmapSource bitmapSourceInternal = new InteropBitmap(hIcon, Int32Rect.Empty, bitmapSizeOptions);

        Assert.NotNull(bitmapSource);
        Assert.NotNull(bitmapSourceInternal);

        Assert.Equal(expectedWidth, bitmapSource.PixelWidth);
        Assert.Equal(expectedHeight, bitmapSource.PixelHeight);
        Assert.Equal(expectedWidth, bitmapSourceInternal.PixelWidth);
        Assert.Equal(expectedHeight, bitmapSourceInternal.PixelHeight);

        // Interop Bitmap is always created at 96 DPI
        Assert.Equal(bitmapSource.Width, bitmapSource.PixelWidth);
        Assert.Equal(bitmapSource.Height, bitmapSource.PixelHeight);
        Assert.Equal(bitmapSourceInternal.Width, bitmapSourceInternal.PixelWidth);
        Assert.Equal(bitmapSourceInternal.Height, bitmapSourceInternal.PixelHeight);

        // Free
        Assert.True(PInvoke.DestroyIcon((HICON)hIcon));
        gdiBitmap.Dispose();
    }

    public static IEnumerable<object?[]> Init_EmptyRect_BitmapSizeOptions_Data
    {
        get
        {
            // null options (keeps AR) - no scale
            yield return new object?[] { 128, 128, 128, 128, null };
            yield return new object?[] { 64, 64, 64, 64, null };
            yield return new object?[] { 32, 32, 32, 32, null };
            yield return new object?[] { 16, 16, 16, 16, null };

            // empty options (keeps AR) - no scale
            yield return new object?[] { 128, 128, 128, 128, BitmapSizeOptions.FromEmptyOptions() };
            yield return new object?[] { 64, 64, 64, 64, BitmapSizeOptions.FromEmptyOptions() };
            yield return new object?[] { 32, 32, 32, 32, BitmapSizeOptions.FromEmptyOptions() };
            yield return new object?[] { 16, 16, 16, 16, BitmapSizeOptions.FromEmptyOptions() };

            // width (keeps AR) - scale down
            yield return new object?[] { 128, 128, 64, 64, BitmapSizeOptions.FromWidth(64) };
            yield return new object?[] { 64, 64, 32, 32, BitmapSizeOptions.FromWidth(32) };
            yield return new object?[] { 32, 32, 16, 16, BitmapSizeOptions.FromWidth(16) };
            yield return new object?[] { 16, 16, 8, 8, BitmapSizeOptions.FromWidth(8) };

            // height (keeps AR) - scale down
            yield return new object?[] { 128, 128, 64, 64, BitmapSizeOptions.FromHeight(64) };
            yield return new object?[] { 64, 64, 32, 32, BitmapSizeOptions.FromHeight(32) };
            yield return new object?[] { 32, 32, 16, 16, BitmapSizeOptions.FromHeight(16) };
            yield return new object?[] { 16, 16, 8, 8, BitmapSizeOptions.FromHeight(8) };

            // width + height (keeps AR) - scale down
            yield return new object?[] { 128, 128, 64, 64, BitmapSizeOptions.FromWidthAndHeight(64, 64) };
            yield return new object?[] { 64, 64, 32, 32, BitmapSizeOptions.FromWidthAndHeight(32, 32) };
            yield return new object?[] { 32, 32, 16, 16, BitmapSizeOptions.FromWidthAndHeight(16, 16) };
            yield return new object?[] { 16, 16, 8, 8, BitmapSizeOptions.FromWidthAndHeight(8, 8) };

            // width (keeps AR) - scale up
            yield return new object?[] { 128, 128, 256, 256, BitmapSizeOptions.FromWidth(256) };
            yield return new object?[] { 64, 64, 128, 128, BitmapSizeOptions.FromWidth(128) };
            yield return new object?[] { 32, 32, 64, 64, BitmapSizeOptions.FromWidth(64) };
            yield return new object?[] { 16, 16, 32, 32, BitmapSizeOptions.FromWidth(32) };

            // height (keeps AR) - scale up
            yield return new object?[] { 128, 128, 256, 256, BitmapSizeOptions.FromHeight(256) };
            yield return new object?[] { 64, 64, 128, 128, BitmapSizeOptions.FromHeight(128) };
            yield return new object?[] { 32, 32, 64, 64, BitmapSizeOptions.FromHeight(64) };
            yield return new object?[] { 16, 16, 32, 32, BitmapSizeOptions.FromHeight(32) };

            // width + height (keeps AR) - scale up
            yield return new object?[] { 128, 128, 256, 256, BitmapSizeOptions.FromWidthAndHeight(256, 256) };
            yield return new object?[] { 64, 64, 128, 128, BitmapSizeOptions.FromWidthAndHeight(128, 128) };
            yield return new object?[] { 32, 32, 64, 64, BitmapSizeOptions.FromWidthAndHeight(64, 64) };
            yield return new object?[] { 16, 16, 32, 32, BitmapSizeOptions.FromWidthAndHeight(32, 32) };

            // width (keeps AR) - scale up
            yield return new object?[] { 111, 128, 256, 295, BitmapSizeOptions.FromWidth(256) };
            yield return new object?[] { 64, 34, 128, 68, BitmapSizeOptions.FromWidth(128) };
            yield return new object?[] { 17, 32, 64, 120, BitmapSizeOptions.FromWidth(64) };
            yield return new object?[] { 16, 3, 32, 6, BitmapSizeOptions.FromWidth(32) };

            // height (keeps AR) - scale up
            yield return new object?[] { 36, 88, 104, 256, BitmapSizeOptions.FromHeight(256) };
            yield return new object?[] { 34, 22, 197, 128, BitmapSizeOptions.FromHeight(128) };
            yield return new object?[] { 55, 28, 125, 64, BitmapSizeOptions.FromHeight(64) };
            yield return new object?[] { 4, 6, 21, 32, BitmapSizeOptions.FromHeight(32) };

            // width + height (forces AR) - scale up
            yield return new object?[] { 128, 64, 256, 256, BitmapSizeOptions.FromWidthAndHeight(256, 256) };
            yield return new object?[] { 64, 96, 128, 128, BitmapSizeOptions.FromWidthAndHeight(128, 128) };
            yield return new object?[] { 32, 64, 64, 64, BitmapSizeOptions.FromWidthAndHeight(64, 64) };
            yield return new object?[] { 128, 16, 32, 32, BitmapSizeOptions.FromWidthAndHeight(32, 32) };

            // width + height (forces AR) - non-proportional
            yield return new object?[] { 128, 64, 600, 800, BitmapSizeOptions.FromWidthAndHeight(600, 800) };
            yield return new object?[] { 64, 96, 800, 600, BitmapSizeOptions.FromWidthAndHeight(800, 600) };
            yield return new object?[] { 32, 64, 128, 128, BitmapSizeOptions.FromWidthAndHeight(128, 128) };
            yield return new object?[] { 128, 16, 256, 256, BitmapSizeOptions.FromWidthAndHeight(256, 256) };
        }
    }

    [MemberData(nameof(Init_SourceRect_BitmapSizeOptions_Data))]
    [WpfTheory]
    public void Init_FromHBitmap_SourceRect_BitmapSizeOptions(int sourceWidth, int sourceHeight, int expectedWidth, int expectedHeight,
                                                              Int32Rect sourceClipRect, BitmapSizeOptions? bitmapSizeOptions)
    {
        Bitmap gdiBitmap = new(sourceWidth, sourceHeight);
        nint hBitmap = gdiBitmap.GetHbitmap();

        Assert.NotEqual(nint.Zero, hBitmap);

        // Same constructors but Imaging is public
        BitmapSource bitmapSource = Imaging.CreateBitmapSourceFromHBitmap(hBitmap, nint.Zero, sourceClipRect, bitmapSizeOptions);
        BitmapSource bitmapSourceInternal = new InteropBitmap(hBitmap, nint.Zero, sourceClipRect, bitmapSizeOptions, WICBitmapAlphaChannelOption.WICBitmapUseAlpha);

        Assert.NotNull(bitmapSource);
        Assert.NotNull(bitmapSourceInternal);

        Assert.Equal(expectedWidth, bitmapSource.PixelWidth);
        Assert.Equal(expectedHeight, bitmapSource.PixelHeight);
        Assert.Equal(expectedWidth, bitmapSourceInternal.PixelWidth);
        Assert.Equal(expectedHeight, bitmapSourceInternal.PixelHeight);

        // Interop Bitmap is always created at 96 DPI
        Assert.Equal(bitmapSource.Width, bitmapSource.PixelWidth);
        Assert.Equal(bitmapSource.Height, bitmapSource.PixelHeight);
        Assert.Equal(bitmapSourceInternal.Width, bitmapSourceInternal.PixelWidth);
        Assert.Equal(bitmapSourceInternal.Height, bitmapSourceInternal.PixelHeight);

        // Free
        Assert.True(PInvoke.DeleteObject((HGDIOBJ)hBitmap));
        gdiBitmap.Dispose();
    }

    [MemberData(nameof(Init_SourceRect_BitmapSizeOptions_Data))]
    [WpfTheory]
    public void Init_FromHIcon_SourceRect_BitmapSizeOptions(int sourceWidth, int sourceHeight, int expectedWidth, int expectedHeight,
                                                            Int32Rect sourceClipRect, BitmapSizeOptions? bitmapSizeOptions)
    {
        Bitmap gdiBitmap = new(sourceWidth, sourceHeight);
        nint hIcon = gdiBitmap.GetHicon();

        Assert.NotEqual(nint.Zero, hIcon);

        // Same constructors but Imaging is public
        BitmapSource bitmapSource = Imaging.CreateBitmapSourceFromHIcon(hIcon, sourceClipRect, bitmapSizeOptions);
        BitmapSource bitmapSourceInternal = new InteropBitmap(hIcon, sourceClipRect, bitmapSizeOptions);

        Assert.NotNull(bitmapSource);
        Assert.NotNull(bitmapSourceInternal);

        Assert.Equal(expectedWidth, bitmapSource.PixelWidth);
        Assert.Equal(expectedHeight, bitmapSource.PixelHeight);
        Assert.Equal(expectedWidth, bitmapSourceInternal.PixelWidth);
        Assert.Equal(expectedHeight, bitmapSourceInternal.PixelHeight);

        // Interop Bitmap is always created at 96 DPI
        Assert.Equal(bitmapSource.Width, bitmapSource.PixelWidth);
        Assert.Equal(bitmapSource.Height, bitmapSource.PixelHeight);
        Assert.Equal(bitmapSourceInternal.Width, bitmapSourceInternal.PixelWidth);
        Assert.Equal(bitmapSourceInternal.Height, bitmapSourceInternal.PixelHeight);

        // Free
        Assert.True(PInvoke.DestroyIcon((HICON)hIcon));
        gdiBitmap.Dispose();
    }

    public static IEnumerable<object?[]> Init_SourceRect_BitmapSizeOptions_Data
    {
        get
        {
            // null options (keeps AR) - no scale
            yield return new object?[] { 128, 128, 64, 64, new Int32Rect(0, 0, 64, 64), null };
            yield return new object?[] { 64, 64, 64, 64, new Int32Rect(0, 0, 64, 64), null };
            yield return new object?[] { 32, 32, 16, 16, new Int32Rect(0, 0, 16, 16), null };
            yield return new object?[] { 16, 16, 4, 4, new Int32Rect(0, 0, 4, 4), null };

            // empty options (keeps AR) - no scale
            yield return new object?[] { 128, 128, 64, 64, new Int32Rect(0, 0, 64, 64), BitmapSizeOptions.FromEmptyOptions() };
            yield return new object?[] { 64, 64, 64, 64, new Int32Rect(0, 0, 64, 64), BitmapSizeOptions.FromEmptyOptions() };
            yield return new object?[] { 32, 32, 16, 16, new Int32Rect(0, 0, 16, 16), BitmapSizeOptions.FromEmptyOptions() };
            yield return new object?[] { 16, 16, 4, 4, new Int32Rect(0, 0, 4, 4), BitmapSizeOptions.FromEmptyOptions() };

            // width (keeps AR) - scale down
            yield return new object?[] { 128, 128, 64, 64, new Int32Rect(0, 0, 64, 64), BitmapSizeOptions.FromWidth(64) };
            yield return new object?[] { 64, 64, 32, 32, new Int32Rect(0, 0, 32, 32), BitmapSizeOptions.FromWidth(32) };
            yield return new object?[] { 32, 32, 16, 16, new Int32Rect(0, 0, 8, 8), BitmapSizeOptions.FromWidth(16) };
            yield return new object?[] { 16, 16, 8, 8, new Int32Rect(0, 0, 4, 4), BitmapSizeOptions.FromWidth(8) };

            // height (keeps AR) - scale down
            yield return new object?[] { 128, 128, 64, 64, new Int32Rect(0, 0, 64, 64), BitmapSizeOptions.FromHeight(64) };
            yield return new object?[] { 64, 64, 32, 32, new Int32Rect(0, 0, 32, 32), BitmapSizeOptions.FromHeight(32) };
            yield return new object?[] { 32, 32, 16, 16, new Int32Rect(0, 0, 8, 8), BitmapSizeOptions.FromHeight(16) };
            yield return new object?[] { 16, 16, 8, 8, new Int32Rect(0, 0, 4, 4), BitmapSizeOptions.FromHeight(8) };

            // width + height (keeps AR) - scale down
            yield return new object?[] { 128, 128, 64, 64, new Int32Rect(0, 0, 64, 64), BitmapSizeOptions.FromWidthAndHeight(64, 64) };
            yield return new object?[] { 64, 64, 32, 32, new Int32Rect(0, 0, 32, 32), BitmapSizeOptions.FromWidthAndHeight(32, 32) };
            yield return new object?[] { 32, 32, 16, 16, new Int32Rect(0, 0, 8, 8), BitmapSizeOptions.FromWidthAndHeight(16, 16) };
            yield return new object?[] { 16, 16, 8, 8, new Int32Rect(0, 0, 4, 4), BitmapSizeOptions.FromWidthAndHeight(8, 8) };

            // width (keeps AR) - scale up
            yield return new object?[] { 128, 128, 256, 256, new Int32Rect(0, 0, 64, 64), BitmapSizeOptions.FromWidth(256) };
            yield return new object?[] { 64, 64, 128, 128, new Int32Rect(0, 0, 32, 32), BitmapSizeOptions.FromWidth(128) };
            yield return new object?[] { 32, 32, 64, 64, new Int32Rect(0, 0, 8, 8), BitmapSizeOptions.FromWidth(64) };
            yield return new object?[] { 16, 16, 32, 32, new Int32Rect(0, 0, 4, 4), BitmapSizeOptions.FromWidth(32) };

            // height (keeps AR) - scale up
            yield return new object?[] { 128, 128, 256, 256, new Int32Rect(0, 0, 64, 64), BitmapSizeOptions.FromHeight(256) };
            yield return new object?[] { 64, 64, 128, 128, new Int32Rect(0, 0, 32, 32), BitmapSizeOptions.FromHeight(128) };
            yield return new object?[] { 32, 32, 64, 64, new Int32Rect(0, 0, 8, 8), BitmapSizeOptions.FromHeight(64) };
            yield return new object?[] { 16, 16, 32, 32, new Int32Rect(0, 0, 4, 4), BitmapSizeOptions.FromHeight(32) };

            // width + height (keeps AR) - scale up
            yield return new object?[] { 128, 128, 256, 256, new Int32Rect(0, 0, 64, 64), BitmapSizeOptions.FromWidthAndHeight(256, 256) };
            yield return new object?[] { 64, 64, 128, 128, new Int32Rect(0, 0, 32, 32), BitmapSizeOptions.FromWidthAndHeight(128, 128) };
            yield return new object?[] { 32, 32, 64, 64, new Int32Rect(0, 0, 4, 8), BitmapSizeOptions.FromWidthAndHeight(64, 64) };
            yield return new object?[] { 16, 16, 32, 32, new Int32Rect(0, 0, 8, 4), BitmapSizeOptions.FromWidthAndHeight(32, 32) };

            // width (keeps AR) - scale up
            yield return new object?[] { 111, 128, 256, 128, new Int32Rect(0, 0, 64, 32), BitmapSizeOptions.FromWidth(256) };
            yield return new object?[] { 64, 34, 128, 68, new Int32Rect(0, 0, 64, 34), BitmapSizeOptions.FromWidth(128) };
            yield return new object?[] { 17, 32, 64, 120, new Int32Rect(0, 0, 17, 32), BitmapSizeOptions.FromWidth(64) };
            yield return new object?[] { 16, 3, 32, 16, new Int32Rect(8, 0, 2, 1), BitmapSizeOptions.FromWidth(32) };

            // height (keeps AR) - scale up
            yield return new object?[] { 36, 88, 512, 256, new Int32Rect(4, 50, 32, 16), BitmapSizeOptions.FromHeight(256) };
            yield return new object?[] { 34, 22, 197, 128, new Int32Rect(0, 0, 34, 22), BitmapSizeOptions.FromHeight(128) };
            yield return new object?[] { 55, 28, 128, 64, new Int32Rect(10, 0, 32, 16), BitmapSizeOptions.FromHeight(64) };
            yield return new object?[] { 4, 6, 16, 32, new Int32Rect(1, 1, 2, 4), BitmapSizeOptions.FromHeight(32) };

            // width + height (forces AR) - scale up
            yield return new object?[] { 128, 64, 256, 256, new Int32Rect(0, 0, 64, 32), BitmapSizeOptions.FromWidthAndHeight(256, 256) };
            yield return new object?[] { 64, 96, 128, 128, new Int32Rect(0, 0, 64, 96), BitmapSizeOptions.FromWidthAndHeight(128, 128) };
            yield return new object?[] { 32, 64, 64, 64, new Int32Rect(16, 32, 16, 16), BitmapSizeOptions.FromWidthAndHeight(64, 64) };
            yield return new object?[] { 128, 16, 32, 32, new Int32Rect(64, 0, 32, 16), BitmapSizeOptions.FromWidthAndHeight(32, 32) };

            // width + height (forces AR) - non-proportional
            yield return new object?[] { 128, 64, 600, 800, new Int32Rect(0, 0, 64, 32), BitmapSizeOptions.FromWidthAndHeight(600, 800) };
            yield return new object?[] { 64, 96, 800, 600, new Int32Rect(0, 0, 32, 32), BitmapSizeOptions.FromWidthAndHeight(800, 600) };
            yield return new object?[] { 32, 64, 128, 128, new Int32Rect(0, 0, 16, 16), BitmapSizeOptions.FromWidthAndHeight(128, 128) };
            yield return new object?[] { 128, 16, 256, 256, new Int32Rect(0, 0, 16, 16), BitmapSizeOptions.FromWidthAndHeight(256, 256) };
        }
    }
}
