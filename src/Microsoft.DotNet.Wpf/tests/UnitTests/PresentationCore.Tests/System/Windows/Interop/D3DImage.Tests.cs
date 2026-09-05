// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Windows.Interop;

public sealed class D3DImageTests
{
    [Fact]
    public void Constructor_NaN_Dpi_DoesNotThrow()
    {
        // NaN DPI should not throw - it passes through the "< 0" check
        // because NaN < 0 is false per IEEE 754.
        _ = new D3DImage(double.NaN, double.NaN);
    }

    [Fact]
    public void Constructor_NegativeDpiX_ThrowsArgumentOutOfRangeException()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new D3DImage(-1.0, 96.0));
    }

    [Fact]
    public void Constructor_NegativeDpiY_ThrowsArgumentOutOfRangeException()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new D3DImage(96.0, -1.0));
    }

    [Fact]
    public void Constructor_ValidDpi_DoesNotThrow()
    {
        _ = new D3DImage(96.0, 96.0);
    }
}
