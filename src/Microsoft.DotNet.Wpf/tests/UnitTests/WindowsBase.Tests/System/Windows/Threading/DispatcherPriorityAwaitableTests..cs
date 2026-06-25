// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Windows.Threading.Tests;

public class DispatcherPriorityAwaitableTests
{
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
    public void GetAwaiter_InvokeWithDispatcher_ReturnsExpected(DispatcherPriority priority)
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
        Assert.False(awaiter.IsCompleted);
        Assert.Equal(awaiter, awaitable.GetAwaiter());
    }
    
    [Fact]
    public void GetAwaiter_InvokeDefault_ReturnsExpected()
    {
        var awaitable = new DispatcherPriorityAwaitable();
        DispatcherPriorityAwaiter awaiter = awaitable.GetAwaiter();
        Assert.False(awaiter.IsCompleted);
        Assert.Equal(awaiter, awaitable.GetAwaiter());
    }
}
