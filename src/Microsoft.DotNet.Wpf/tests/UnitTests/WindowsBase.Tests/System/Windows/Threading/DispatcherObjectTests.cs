// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading;

namespace System.Windows.Threading.Tests;

public class DispatcherObjectTests
{
    [Fact]
    public void Ctor_Default()
    {
        var obj = new SubDispatcherObject();
        Assert.NotNull(obj.Dispatcher);
        Assert.Same(obj.Dispatcher, obj.Dispatcher);
        Assert.Same(Dispatcher.CurrentDispatcher, obj.Dispatcher);
    }

    [Fact]
    public void CheckAccess_InvokeOnCurrentThread_ReturnsTrue()
    {
        var obj = new SubDispatcherObject();
        Assert.True(obj.CheckAccess());
    }

    [Fact]
    public void CheckAccess_InvokeOnDifferentThread_ReturnsFalse()
    {
        var obj = new SubDispatcherObject();
        bool? access = null;
        var thread = new Thread(() =>
        {
            access = obj.CheckAccess();
        });
        thread.Start();
        thread.Join();
        Assert.False(access);
    }

    [Fact]
    public void CheckAccess_InvokeDetachedDispatcher_ReturnsTrue()
    {
        var obj = new SubFreezable();
        obj.Freeze();
        Assert.Null(obj.Dispatcher);

        Assert.True(obj.CheckAccess());
    }

    [Fact]
    public void VerifyAccess_InvokeOnCurrentThread_Success()
    {
        var obj = new SubDispatcherObject();
        obj.VerifyAccess();
    }

    [Fact]
    public void VerifyAccess_InvokeDetachedDispatcher_Success()
    {
        var obj = new SubFreezable();
        obj.Freeze();
        Assert.Null(obj.Dispatcher);
        
        obj.VerifyAccess();
    }

    [Fact]
    public void VerifyAccess_InvokeOnDifferentThread_ThrowsInvalidOperationException()
    {
        var obj = new SubDispatcherObject();
        bool? threwInvalidOperationException = null;
        var thread = new Thread(() =>
        {
            try
            {
                obj.VerifyAccess();
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

    private class SubFreezable : Freezable
    {
        protected override Freezable CreateInstanceCore() => throw new NotImplementedException();
    }

    public class SubDispatcherObject : DispatcherObject
    {
    }
}