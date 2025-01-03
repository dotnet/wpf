// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Windows;

public class SizeTests
{
    [Fact]
    public void Constructor_NegativeWidth_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => new Size(-1, 0));
    }

    [Fact]
    public void Constructor_NegativeHeight_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => new Size(0, -1));
    }

    [Fact]
    public void Empty_SetWidth_ThrowsInvalidOperation()
    {
        Size size = Size.Empty;
        Assert.Throws<InvalidOperationException>(() => size.Width = 0);
    }

    [Fact]
    public void Empty_SetHeight_ThrowsInvalidOperation()
    {
        Size size = Size.Empty;
        Assert.Throws<InvalidOperationException>(() => size.Height = 0);
    }

    [Fact]
    public void Height_Set_NegativeValue_ThrowsArgumentException()
    {
        Size size = new(0, 0);
        Assert.Throws<ArgumentException>(() => size.Height = -1);
    }

    [Fact]
    public void Width_Set_NegativeValue_ThrowsArgumentException()
    {
        Size size = new(0, 0);
        Assert.Throws<ArgumentException>(() => size.Width = -1);
    }
}
