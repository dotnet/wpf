// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Windows.Interop.Tests;

public class MSGTests
{
    [Fact]
    public void Ctor_Default()
    {
        var msg = new MSG();
        Assert.Equal(IntPtr.Zero, msg.hwnd);
        Assert.Equal(0, msg.message);
        Assert.Equal(IntPtr.Zero, msg.wParam);
        Assert.Equal(IntPtr.Zero, msg.lParam);
        Assert.Equal(0, msg.time);
        Assert.Equal(0, msg.pt_x);
        Assert.Equal(0, msg.pt_y);
    }

    public static IEnumerable<object[]> IntPtr_TestData()
    {
        yield return new object[] { (IntPtr)(-1) };
        yield return new object[] { IntPtr.Zero };
        yield return new object[] { (IntPtr)1 };
    }

    [Theory]
    [MemberData(nameof(IntPtr_TestData))]
    public void hwnd_Set_GetReturnsExpected(IntPtr value)
    {
        var msg = new MSG
        {
            // Set.
            hwnd = value
        };
        Assert.Equal(value, msg.hwnd);

        // Set same.
        msg.hwnd = value;
        Assert.Equal(value, msg.hwnd);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(0)]
    [InlineData(1)]
    public void message_Set_GetReturnsExpected(int value)
    {
        var msg = new MSG
        {
            // Set.
            message = value
        };
        Assert.Equal(value, msg.message);

        // Set same.
        msg.message = value;
        Assert.Equal(value, msg.message);
    }

    [Theory]
    [MemberData(nameof(IntPtr_TestData))]
    public void wParam_Set_GetReturnsExpected(IntPtr value)
    {
        var msg = new MSG
        {
            // Set.
            wParam = value
        };
        Assert.Equal(value, msg.wParam);

        // Set same.
        msg.wParam = value;
        Assert.Equal(value, msg.wParam);
    }

    [Theory]
    [MemberData(nameof(IntPtr_TestData))]
    public void lParam_Set_GetReturnsExpected(IntPtr value)
    {
        var msg = new MSG
        {
            // Set.
            lParam = value
        };
        Assert.Equal(value, msg.lParam);

        // Set same.
        msg.lParam = value;
        Assert.Equal(value, msg.lParam);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(0)]
    [InlineData(1)]
    public void time_Set_GetReturnsExpected(int value)
    {
        var msg = new MSG
        {
            // Set.
            time = value
        };
        Assert.Equal(value, msg.time);

        // Set same.
        msg.time = value;
        Assert.Equal(value, msg.time);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(0)]
    [InlineData(1)]
    public void pt_x_Set_GetReturnsExpected(int value)
    {
        var msg = new MSG
        {
            // Set.
            pt_x = value
        };
        Assert.Equal(value, msg.pt_x);

        // Set same.
        msg.pt_x = value;
        Assert.Equal(value, msg.pt_x);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(0)]
    [InlineData(1)]
    public void pt_y_Set_GetReturnsExpected(int value)
    {
        var msg = new MSG
        {
            // Set.
            pt_y = value
        };
        Assert.Equal(value, msg.pt_y);

        // Set same.
        msg.pt_y = value;
        Assert.Equal(value, msg.pt_y);
    }
}