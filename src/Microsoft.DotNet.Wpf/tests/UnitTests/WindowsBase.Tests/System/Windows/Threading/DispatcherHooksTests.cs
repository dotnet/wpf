// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Windows.Threading.Tests;

public class DispatcherHooksTests
{
    // TODO
    // - actually raising events

    [Fact]
    public void DispatcherInactive_AddRemove_Success()
    {
        Dispatcher dispatcher = Dispatcher.CurrentDispatcher;
        DispatcherHooks hooks = dispatcher.Hooks;
        int callCount = 0;
        EventHandler handler = (s, e) => callCount++;
        hooks.DispatcherInactive += handler;
        Assert.Equal(0, callCount);

        hooks.DispatcherInactive -= handler;
        Assert.Equal(0, callCount);

        // Remove non existent.
        hooks.DispatcherInactive -= handler;
        Assert.Equal(0, callCount);

        // Add null.
        hooks.DispatcherInactive += null;
        Assert.Equal(0, callCount);

        // Remove null.
        hooks.DispatcherInactive -= null;
        Assert.Equal(0, callCount);
    }

    [Fact]
    public void OperationPosted_AddRemove_Success()
    {
        Dispatcher dispatcher = Dispatcher.CurrentDispatcher;
        DispatcherHooks hooks = dispatcher.Hooks;
        int callCount = 0;
        DispatcherHookEventHandler handler = (s, e) => callCount++;
        hooks.OperationPosted += handler;
        Assert.Equal(0, callCount);

        hooks.OperationPosted -= handler;
        Assert.Equal(0, callCount);

        // Remove non existent.
        hooks.OperationPosted -= handler;
        Assert.Equal(0, callCount);

        // Add null.
        hooks.OperationPosted += null;
        Assert.Equal(0, callCount);

        // Remove null.
        hooks.OperationPosted -= null;
        Assert.Equal(0, callCount);
    }

    [Fact]
    public void OperationAborted_AddRemove_Success()
    {
        Dispatcher dispatcher = Dispatcher.CurrentDispatcher;
        DispatcherHooks hooks = dispatcher.Hooks;
        int callCount = 0;
        DispatcherHookEventHandler handler = (s, e) => callCount++;
        hooks.OperationAborted += handler;
        Assert.Equal(0, callCount);

        hooks.OperationAborted -= handler;
        Assert.Equal(0, callCount);

        // Remove non existent.
        hooks.OperationAborted -= handler;
        Assert.Equal(0, callCount);

        // Add null.
        hooks.OperationAborted += null;
        Assert.Equal(0, callCount);

        // Remove null.
        hooks.OperationAborted -= null;
        Assert.Equal(0, callCount);
    }

    [Fact]
    public void OperationCompleted_AddRemove_Success()
    {
        Dispatcher dispatcher = Dispatcher.CurrentDispatcher;
        DispatcherHooks hooks = dispatcher.Hooks;
        int callCount = 0;
        DispatcherHookEventHandler handler = (s, e) => callCount++;
        hooks.OperationCompleted += handler;
        Assert.Equal(0, callCount);

        hooks.OperationCompleted -= handler;
        Assert.Equal(0, callCount);

        // Remove non existent.
        hooks.OperationCompleted -= handler;
        Assert.Equal(0, callCount);

        // Add null.
        hooks.OperationCompleted += null;
        Assert.Equal(0, callCount);

        // Remove null.
        hooks.OperationCompleted -= null;
        Assert.Equal(0, callCount);
    }

    [Fact]
    public void OperationPriorityChanged_AddRemove_Success()
    {
        Dispatcher dispatcher = Dispatcher.CurrentDispatcher;
        DispatcherHooks hooks = dispatcher.Hooks;
        int callCount = 0;
        DispatcherHookEventHandler handler = (s, e) => callCount++;
        hooks.OperationPriorityChanged += handler;
        Assert.Equal(0, callCount);

        hooks.OperationPriorityChanged -= handler;
        Assert.Equal(0, callCount);

        // Remove non existent.
        hooks.OperationPriorityChanged -= handler;
        Assert.Equal(0, callCount);

        // Add null.
        hooks.OperationPriorityChanged += null;
        Assert.Equal(0, callCount);

        // Remove null.
        hooks.OperationPriorityChanged -= null;
        Assert.Equal(0, callCount);
    }

    [Fact]
    public void OperationStarted_AddRemove_Success()
    {
        Dispatcher dispatcher = Dispatcher.CurrentDispatcher;
        DispatcherHooks hooks = dispatcher.Hooks;
        int callCount = 0;
        DispatcherHookEventHandler handler = (s, e) => callCount++;
        hooks.OperationStarted += handler;
        Assert.Equal(0, callCount);

        hooks.OperationStarted -= handler;
        Assert.Equal(0, callCount);

        // Remove non existent.
        hooks.OperationStarted -= handler;
        Assert.Equal(0, callCount);

        // Add null.
        hooks.OperationStarted += null;
        Assert.Equal(0, callCount);

        // Remove null.
        hooks.OperationStarted -= null;
        Assert.Equal(0, callCount);
    }
}