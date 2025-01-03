// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Windows.Threading.Tests;

public class DispatcherPriorityAwaiterTests
{
    [Fact]
    public void Ctor_Default()
    {
        var awaiter = new DispatcherPriorityAwaiter();
        Assert.False(awaiter.IsCompleted);
    }

    [Theory]
    [InlineData(DispatcherPriority.ApplicationIdle)]
    [InlineData(DispatcherPriority.Background)]
    [InlineData(DispatcherPriority.ContextIdle)]
    [InlineData(DispatcherPriority.DataBind)]
    [InlineData(DispatcherPriority.Inactive)]
    [InlineData(DispatcherPriority.Input)]
    [InlineData(DispatcherPriority.Loaded)]
    [InlineData(DispatcherPriority.Normal)]
    [InlineData(DispatcherPriority.Render)]
    [InlineData(DispatcherPriority.Send)]
    [InlineData(DispatcherPriority.SystemIdle)]
    public void GetResult_InvokeWithDispatcher_Nop(DispatcherPriority priority)
    {
        DispatcherPriorityAwaitable awaitable;
        try
        {
            awaitable = Dispatcher.Yield(priority);
        }
        catch (InvalidOperationException)
        {
            // Yield throws if there is no dispatcher.
            return;
        }

        DispatcherPriorityAwaiter awaiter = awaitable.GetAwaiter();
        
        // Invoke.
        awaiter.GetResult();

        // Invoke again.
        awaiter.GetResult();
    }

    [Fact]
    public void GetResult_InvokeDefaultAwaitable_Nop()
    {
        var awaitable = new DispatcherPriorityAwaitable();
        DispatcherPriorityAwaiter awaiter = awaitable.GetAwaiter();

        // Invoke.
        awaiter.GetResult();

        // Invoke again.
        awaiter.GetResult();
    }

    [Fact]
    public void GetResult_InvokeDefault_Nop()
    {
        var awaiter = new DispatcherPriorityAwaiter();

        // Invoke.
        awaiter.GetResult();

        // Invoke again.
        awaiter.GetResult();
    }

    [Theory]
    [InlineData(DispatcherPriority.ApplicationIdle)]
    [InlineData(DispatcherPriority.Background)]
    [InlineData(DispatcherPriority.ContextIdle)]
    [InlineData(DispatcherPriority.DataBind)]
    [InlineData(DispatcherPriority.Inactive)]
    [InlineData(DispatcherPriority.Input)]
    [InlineData(DispatcherPriority.Loaded)]
    [InlineData(DispatcherPriority.Normal)]
    [InlineData(DispatcherPriority.Render)]
    [InlineData(DispatcherPriority.Send)]
    [InlineData(DispatcherPriority.SystemIdle)]
    public void OnCompleted_InvokeWithDispatcher_Success(DispatcherPriority priority)
    {
        DispatcherPriorityAwaitable awaitable;
        try
        {
            awaitable = Dispatcher.Yield(priority);
        }
        catch (InvalidOperationException)
        {
            // Yield throws if there is no dispatcher.
            return;
        }

        DispatcherPriorityAwaiter awaiter = awaitable.GetAwaiter();

        int callCount = 0;
        Action action = () =>
        {
            callCount++;
        };
        
        // Invoke.
        awaiter.OnCompleted(action);
        Assert.Equal(0, callCount);

        // Invoke again.
        awaiter.OnCompleted(action);
        Assert.Equal(0, callCount);
    }

    [Fact]
    public void OnCompleted_NullAction_ThrowsArgumentNullException()
    {
        DispatcherPriorityAwaitable awaitable;
        try
        {
            awaitable = Dispatcher.Yield(DispatcherPriority.Normal);
        }
        catch (InvalidOperationException)
        {
            // Yield throws if there is no dispatcher.
            return;
        }

        DispatcherPriorityAwaiter awaiter = awaitable.GetAwaiter();
        Assert.Throws<ArgumentNullException>(() => awaiter.OnCompleted(null));
    }

    [Fact]
    public void OnCompleted_InvokeDefaultAwaitable_ThrowsInvalidOperationException()
    {
        var awaitable = new DispatcherPriorityAwaitable();
        DispatcherPriorityAwaiter awaiter = awaitable.GetAwaiter();
        Assert.Throws<InvalidOperationException>(() => awaiter.OnCompleted(null));
        Assert.Throws<InvalidOperationException>(() => awaiter.OnCompleted(() => { }));
    }

    [Fact]
    public void OnCompleted_InvokeDefault_ThrowsInvalidOperationException()
    {
        var awaiter = new DispatcherPriorityAwaiter();
        Assert.Throws<InvalidOperationException>(() => awaiter.OnCompleted(null));
        Assert.Throws<InvalidOperationException>(() => awaiter.OnCompleted(() => { }));
    }
}
