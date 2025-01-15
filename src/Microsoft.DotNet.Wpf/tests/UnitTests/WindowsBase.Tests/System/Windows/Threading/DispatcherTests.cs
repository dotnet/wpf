// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel;
using System.Threading;

namespace System.Windows.Threading.Tests;

public class DispatcherTests
{
    [WpfFact]
    public void CurrentDispatcher_Get_ReturnsExpected()
    {
        Dispatcher dispatcher = Dispatcher.CurrentDispatcher;
        Assert.NotNull(dispatcher);
        Assert.Same(dispatcher, Dispatcher.CurrentDispatcher);
        Assert.False(dispatcher.HasShutdownFinished);
        Assert.False(dispatcher.HasShutdownStarted);
        Assert.NotNull(dispatcher.Hooks);
        Assert.Same(dispatcher.Hooks, dispatcher.Hooks);
        Assert.Same(Thread.CurrentThread, dispatcher.Thread);
    }

    [WpfFact]
    public void PushFrame_NullFrame_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>("frame", () => Dispatcher.PushFrame(null!));
    }

    [WpfFact]
    public void FromThread_CurrentThread_ReturnsExpected()
    {
        Dispatcher dispatcher = Dispatcher.FromThread(Thread.CurrentThread);
        Assert.NotNull(dispatcher);
        Assert.Same(dispatcher, Dispatcher.CurrentDispatcher);
        Assert.False(dispatcher.HasShutdownFinished);
        Assert.False(dispatcher.HasShutdownStarted);
        Assert.NotNull(dispatcher.Hooks);
        Assert.Same(dispatcher.Hooks, dispatcher.Hooks);
        Assert.Same(Thread.CurrentThread, dispatcher.Thread);
    }

    [WpfFact]
    public void FromThread_NoSuchThread_ReturnsNull()
    {
        var thread = new Thread(() => { });
        Assert.Null(Dispatcher.FromThread(thread));
    }

    [WpfFact]
    public void FromThread_NullThread_ReturnsNull()
    {
        Assert.Null(Dispatcher.FromThread(null));
    }

    [WpfTheory]
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
    public void ValidatePriority_InvokeValidPriority_Nop(DispatcherPriority priority)
    {
        Dispatcher.ValidatePriority(priority, "priority");
        Dispatcher.ValidatePriority(priority, string.Empty);
        Dispatcher.ValidatePriority(priority, null);
    }

    [WpfTheory]
    [InlineData(DispatcherPriority.Invalid)]
    [InlineData(DispatcherPriority.Invalid - 1)]
    [InlineData(DispatcherPriority.Send + 1)]
    public void ValidatePriority_InvokeInvalidPriority_ThrowsInvalidEnumArgumentException(DispatcherPriority priority)
    {
        Assert.Throws<InvalidEnumArgumentException>("priority", () => Dispatcher.ValidatePriority(priority, "priority"));
        Assert.Throws<InvalidEnumArgumentException>("", () => Dispatcher.ValidatePriority(priority, ""));
        Assert.Throws<InvalidEnumArgumentException>(() => Dispatcher.ValidatePriority(priority, null));
    }

    [WpfTheory]
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
    public void Yield_Invoke_ReturnsExpected(DispatcherPriority priority)
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

        Assert.Equal(awaitable, Dispatcher.Yield(priority));
    }

    [WpfTheory]
    [InlineData(DispatcherPriority.Invalid)]
    [InlineData(DispatcherPriority.Invalid - 1)]
    [InlineData(DispatcherPriority.Send + 1)]
    public void Yield_InvokeInvalidPriority_ThrowsInvalidEnumArgumentException(DispatcherPriority priority)
    {
        Assert.Throws<InvalidEnumArgumentException>("priority", () => Dispatcher.Yield(priority));
    }

    [WpfFact]
    public void BeginInvoke_InvokeDelegateObjectArray_Success()
    {
        Dispatcher dispatcher = Dispatcher.CurrentDispatcher;

        Delegate action = new Action(() => { });
        DispatcherOperation operation = dispatcher.BeginInvoke(action, Array.Empty<object>());
        Assert.NotNull(operation);
        Assert.Same(dispatcher, operation.Dispatcher);
        Assert.Equal(DispatcherPriority.Normal, operation.Priority);
        Assert.Equal(DispatcherOperationStatus.Pending, operation.Status);
        Assert.NotNull(operation.Task);
    }

    [WpfTheory]
    [InlineData(DispatcherPriority.Invalid)]
    [InlineData(DispatcherPriority.Invalid - 1)]
    [InlineData(DispatcherPriority.Send + 1)]
    public void BeginInvoke_InvokeInvalidPriority_ThrowsInvalidEnumArgumentException(DispatcherPriority priority)
    {
        Dispatcher dispatcher = Dispatcher.CurrentDispatcher;
        Assert.Throws<InvalidEnumArgumentException>("priority", () => dispatcher.BeginInvoke(priority, (Action)(() => { })));
        Assert.Throws<InvalidEnumArgumentException>("priority", () => dispatcher.BeginInvoke(priority, (Action<object>)((arg) => { }), new object()));
        Assert.Throws<InvalidEnumArgumentException>("priority", () => dispatcher.BeginInvoke(priority, (Action<object, object>)((arg1, arg2) => { }), new object(), new object[] { new object() }));
        Assert.Throws<InvalidEnumArgumentException>("priority", () => dispatcher.BeginInvoke(priority, (Action<object, object>)((arg1, arg2) => { }), new object[] { new object(), new object() }));
    }

    [WpfFact]
    public void CheckAccess_InvokeOnCurrentThread_ReturnsTrue()
    {
        Dispatcher dispatcher = Dispatcher.CurrentDispatcher;
        Assert.True(dispatcher.CheckAccess());
    }

    [WpfFact]
    public void CheckAccess_InvokeOnDifferentThread_ReturnsFalse()
    {
        Dispatcher dispatcher = Dispatcher.CurrentDispatcher;
        bool? access = null;
        var thread = new Thread(() =>
        {
            access = dispatcher.CheckAccess();
        });
        thread.Start();
        thread.Join();
        Assert.False(access);
    }

    [WpfFact]
    public void VerifyAccess_InvokeOnCurrentThread_Success()
    {
        Dispatcher dispatcher = Dispatcher.CurrentDispatcher;
        dispatcher.VerifyAccess();
    }

    [WpfFact]
    public void VerifyAccess_InvokeOnDifferentThread_ThrowsInvalidOperationException()
    {
        Dispatcher dispatcher = Dispatcher.CurrentDispatcher;
        bool? threwInvalidOperationException = null;
        var thread = new Thread(() =>
        {
            try
            {
                dispatcher.VerifyAccess();
                threwInvalidOperationException = false;
            }
            catch (InvalidOperationException)
            {
                threwInvalidOperationException = true;
            }
            catch
            {
                threwInvalidOperationException = false;
            }
        });
        thread.Start();
        thread.Join();
        Assert.True(threwInvalidOperationException);
    }
}