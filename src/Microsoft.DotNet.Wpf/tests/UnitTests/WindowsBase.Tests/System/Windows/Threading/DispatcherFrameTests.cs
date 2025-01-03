// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Windows.Threading.Tests;

public class DispatcherFrameTests
{
    [Fact]
    public void Ctor_Default()
    {
        var frame = new DispatcherFrame();
        Assert.True(frame.Continue);
        Assert.NotNull(frame.Dispatcher);
        Assert.Same(frame.Dispatcher, frame.Dispatcher);
        Assert.Same(Dispatcher.CurrentDispatcher, frame.Dispatcher);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Ctor_Bool(bool exitWhenRequested)
    {
        var frame = new DispatcherFrame(exitWhenRequested);
        Assert.True(frame.Continue);
        Assert.NotNull(frame.Dispatcher);
        Assert.Same(frame.Dispatcher, frame.Dispatcher);
        Assert.Same(Dispatcher.CurrentDispatcher, frame.Dispatcher);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Continue_Set_GetReturnsExpected(bool value)
    {
        var frame = new DispatcherFrame
        {
            // Set same.
            Continue = value
        };
        Assert.Equal(value, frame.Continue);

        // Set different.
        frame.Continue = !value;
        Assert.Equal(!value, frame.Continue);
    }
}
