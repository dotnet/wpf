// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel;

namespace System.Windows.Threading.Tests;

public class DispatcherTimerTests
{
    [Fact]
    public void Ctor_Default()
    {
        var timer = new DispatcherTimer();
        Assert.Same(Dispatcher.CurrentDispatcher, timer.Dispatcher);
        Assert.Equal(TimeSpan.Zero, timer.Interval);
        Assert.False(timer.IsEnabled);
        Assert.Null(timer.Tag);
    }

    [Theory]
    [InlineData(DispatcherPriority.ApplicationIdle)]
    [InlineData(DispatcherPriority.Background)]
    [InlineData(DispatcherPriority.ContextIdle)]
    [InlineData(DispatcherPriority.DataBind)]
    [InlineData(DispatcherPriority.Input)]
    [InlineData(DispatcherPriority.Loaded)]
    [InlineData(DispatcherPriority.Normal)]
    [InlineData(DispatcherPriority.Render)]
    [InlineData(DispatcherPriority.Send)]
    [InlineData(DispatcherPriority.SystemIdle)]
    public void Ctor_DispatcherPriority(DispatcherPriority priority)
    {
        var timer = new DispatcherTimer(priority);
        Assert.Same(Dispatcher.CurrentDispatcher, timer.Dispatcher);
        Assert.Equal(TimeSpan.Zero, timer.Interval);
        Assert.False(timer.IsEnabled);
        Assert.Null(timer.Tag);
    }

    [Theory]
    [InlineData(DispatcherPriority.ApplicationIdle)]
    [InlineData(DispatcherPriority.Background)]
    [InlineData(DispatcherPriority.ContextIdle)]
    [InlineData(DispatcherPriority.DataBind)]
    [InlineData(DispatcherPriority.Input)]
    [InlineData(DispatcherPriority.Loaded)]
    [InlineData(DispatcherPriority.Normal)]
    [InlineData(DispatcherPriority.Render)]
    [InlineData(DispatcherPriority.Send)]
    [InlineData(DispatcherPriority.SystemIdle)]
    public void Ctor_DispatcherPriority_Dispatcher(DispatcherPriority priority)
    {
        var timer = new DispatcherTimer(priority, Dispatcher.CurrentDispatcher);
        Assert.Same(Dispatcher.CurrentDispatcher, timer.Dispatcher);
        Assert.Equal(TimeSpan.Zero, timer.Interval);
        Assert.False(timer.IsEnabled);
        Assert.Null(timer.Tag);
    }

    public static IEnumerable<object?[]> Ctor_TimeSpan_DispatcherPriority_EventHandler_Dispatcher_TestData()
    {
        yield return new object?[] { TimeSpan.FromMilliseconds(0), DispatcherPriority.ApplicationIdle };
        yield return new object?[] { TimeSpan.FromMilliseconds(0), DispatcherPriority.Background };
        yield return new object?[] { TimeSpan.FromMilliseconds(int.MaxValue), DispatcherPriority.ContextIdle };
        yield return new object?[] { TimeSpan.FromMilliseconds(0), DispatcherPriority.DataBind };
        yield return new object?[] { TimeSpan.FromMilliseconds(1), DispatcherPriority.Input };
        yield return new object?[] { TimeSpan.FromMilliseconds(2), DispatcherPriority.Loaded };
        yield return new object?[] { TimeSpan.FromMilliseconds(3), DispatcherPriority.Normal };
        yield return new object?[] { TimeSpan.FromMilliseconds(4), DispatcherPriority.Render };
        yield return new object?[] { TimeSpan.FromMilliseconds(5), DispatcherPriority.Send };
        yield return new object?[] { TimeSpan.FromMilliseconds(6), DispatcherPriority.SystemIdle };
    }

    [Theory]
    [MemberData(nameof(Ctor_TimeSpan_DispatcherPriority_EventHandler_Dispatcher_TestData))]
    public void Ctor_TimeSpan_DispatcherPriority_EventHandler_Dispatcher(TimeSpan interval, DispatcherPriority priority)
    {
        EventHandler callback = (s, e) => {};
        var timer = new DispatcherTimer(interval, priority, callback, Dispatcher.CurrentDispatcher);
        Assert.Same(Dispatcher.CurrentDispatcher, timer.Dispatcher);
        Assert.Equal(interval, timer.Interval);
        Assert.True(timer.IsEnabled);
        Assert.Null(timer.Tag);
    }

    [Theory]
    [InlineData(DispatcherPriority.Invalid)]
    [InlineData(DispatcherPriority.Invalid - 1)]
    [InlineData(DispatcherPriority.Send + 1)]
    public void Ctor_InvalidPriority_ThrowsInvalidEnumArgumentException(DispatcherPriority priority)
    {
        Assert.Throws<InvalidEnumArgumentException>("priority", () => new DispatcherTimer(priority));
        Assert.Throws<InvalidEnumArgumentException>("priority", () => new DispatcherTimer(priority, Dispatcher.CurrentDispatcher));
        Assert.Throws<InvalidEnumArgumentException>("priority", () => new DispatcherTimer(TimeSpan.Zero, priority, (s, e) => {}, Dispatcher.CurrentDispatcher));
    }

    [Fact]
    public void Ctor_InactivePriority_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>("priority", () => new DispatcherTimer(DispatcherPriority.Inactive));
        Assert.Throws<ArgumentException>("priority", () => new DispatcherTimer(DispatcherPriority.Inactive, Dispatcher.CurrentDispatcher));
        Assert.Throws<ArgumentException>("priority", () => new DispatcherTimer(TimeSpan.Zero, DispatcherPriority.Inactive, (s, e) => {}, Dispatcher.CurrentDispatcher));
    }

    [Fact]
    public void Ctor_NullDispatcher_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>("dispatcher", () => new DispatcherTimer(DispatcherPriority.Normal, null));
        Assert.Throws<ArgumentNullException>("dispatcher", () => new DispatcherTimer(TimeSpan.Zero, DispatcherPriority.Normal, (s, e) => {}, null));
    }

    public static IEnumerable<object[]> InvalidInterval_TestData()
    {
        yield return new object[] { TimeSpan.FromTicks(-1) };
        yield return new object[] { TimeSpan.FromMilliseconds((double)int.MaxValue + 1) };
        yield return new object[] { TimeSpan.MinValue };
        yield return new object[] { TimeSpan.MaxValue };
    }

    [Theory]
    [MemberData(nameof(InvalidInterval_TestData))]
    public void Ctor_InvalidInterval_ThrowsArgumentOutOfRangeException(TimeSpan interval)
    {
        Assert.Throws<ArgumentOutOfRangeException>("interval", () => new DispatcherTimer(interval, DispatcherPriority.Normal, (s, e) => {}, Dispatcher.CurrentDispatcher));
    }

    [Fact]
    public void Ctor_NullCallback_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>("callback", () => new DispatcherTimer(TimeSpan.Zero, DispatcherPriority.Normal, null, Dispatcher.CurrentDispatcher));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void IsEnabled_Set_GetReturnsExpected(bool value)
    {
        var timer = new DispatcherTimer
        {
            IsEnabled = value
        };
        Assert.Equal(value, timer.IsEnabled);

        // Set same.
        timer.IsEnabled = value;
        Assert.Equal(value, timer.IsEnabled);

        // Set different.
        timer.IsEnabled = !value;
        Assert.Equal(!value, timer.IsEnabled);
    }

    public static IEnumerable<object[]> Interval_Set_TestData()
    {
        yield return new object[] { TimeSpan.Zero };
        yield return new object[] { TimeSpan.FromMilliseconds(1) };
        yield return new object[] { TimeSpan.FromMilliseconds(int.MaxValue) };
    }

    [Theory]
    [MemberData(nameof(Interval_Set_TestData))]
    public void Interval_Set_GetReturnsExpected(TimeSpan value)
    {
        var timer = new DispatcherTimer
        {
            Interval = value
        };
        Assert.Equal(value, timer.Interval);

        // Set same.
        timer.Interval = value;
        Assert.Equal(value, timer.Interval);
    }

    [Theory]
    [MemberData(nameof(Interval_Set_TestData))]
    public void Interval_SetEnabled_GetReturnsExpected(TimeSpan value)
    {
        var timer = new DispatcherTimer
        {
            IsEnabled = true,

            // Set.
            Interval = value
        };
        Assert.Equal(value, timer.Interval);

        // Set same.
        timer.Interval = value;
        Assert.Equal(value, timer.Interval);
    }

    [Theory]
    [MemberData(nameof(InvalidInterval_TestData))]
    public void Interval_SetInvalid_ThrowsArgumentOutOfRangeException(TimeSpan value)
    {
        var timer = new DispatcherTimer();
        Assert.Throws<ArgumentOutOfRangeException>("value", () => timer.Interval = value);
    }

    public static IEnumerable<object?[]> Tag_Set_TestData()
    {
        yield return new object?[] { null };
        yield return new object?[] { new object() };
    }

    [Theory]
    [MemberData(nameof(Tag_Set_TestData))]
    public void Tag_Set_GetReturnsExpected(object? value)
    {
        var timer = new DispatcherTimer
        {
            // Set.
            Tag = value
        };
        Assert.Same(value, timer.Tag);

        // Set same.
        timer.Tag = value;
        Assert.Same(value, timer.Tag);
    }

    [Fact]
    public void Tick_AddRemove_Success()
    {
        var timer = new DispatcherTimer();

        int callCount = 0;
        EventHandler handler = (s, e) => callCount++;
        timer.Tick += handler;
        Assert.Equal(0, callCount);

        timer.Tick -= handler;
        Assert.Equal(0, callCount);

        // Remove non existent.
        timer.Tick -= handler;
        Assert.Equal(0, callCount);

        // Add null.
        timer.Tick += null;
        Assert.Equal(0, callCount);

        // Remove null.
        timer.Tick -= null;
        Assert.Equal(0, callCount);
    }

    [Fact]
    public void Start_Invoke_Success()
    {
        var timer = new DispatcherTimer();
        int callCount = 0;
        timer.Tick += (s, e) => callCount++;

        // Start.
        timer.Start();
        Assert.True(timer.IsEnabled);
        Assert.Equal(0, callCount);

        // Start again.
        timer.Start();
        Assert.True(timer.IsEnabled);
        Assert.Equal(0, callCount);
    }

    [Fact]
    public void Stop_InvokeStarted_Success()
    {
        var timer = new DispatcherTimer();
        int callCount = 0;
        timer.Tick += (s, e) => callCount++;

        // Start.
        timer.Start();
        Assert.True(timer.IsEnabled);
        Assert.Equal(0, callCount);

        // Stop.
        timer.Stop();
        Assert.False(timer.IsEnabled);
        Assert.Equal(0, callCount);

        // Stop again.
        timer.Stop();
        Assert.False(timer.IsEnabled);
        Assert.Equal(0, callCount);
    }

    [Fact]
    public void Stop_InvokeNotStarted_Success()
    {
        var timer = new DispatcherTimer();
        int callCount = 0;
        timer.Tick += (s, e) => callCount++;

        // Stop.
        timer.Stop();
        Assert.False(timer.IsEnabled);
        Assert.Equal(0, callCount);

        // Stop again.
        timer.Stop();
        Assert.False(timer.IsEnabled);
        Assert.Equal(0, callCount);
    }
}
