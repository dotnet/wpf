// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Windows.Threading.Tests;

public class DispatcherHookEventArgsTests
{
    [Fact]
    public void Ctor_DispatcherOperation()
    {
        Dispatcher dispatcher = Dispatcher.CurrentDispatcher;
        DispatcherOperation operation = dispatcher.BeginInvoke(() => {});

        var args = new DispatcherHookEventArgs(operation);
        Assert.Same(dispatcher, args.Dispatcher);
        Assert.Same(operation, args.Operation);
    }

    [Fact]
    public void Ctor_NullOperation()
    {
        var args = new DispatcherHookEventArgs(null);
        Assert.Null(args.Dispatcher);
        Assert.Null(args.Operation);
    }
}
