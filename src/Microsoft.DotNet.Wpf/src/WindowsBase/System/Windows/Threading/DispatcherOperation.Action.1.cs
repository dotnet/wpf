// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Windows.Threading;

internal sealed class DispatcherOperationAction<TArg> : DispatcherOperationAction
{
    private readonly TArg _arg;

    internal DispatcherOperationAction(Dispatcher dispatcher, DispatcherPriority priority, Action<TArg> method, TArg arg) : base(
    dispatcher: dispatcher,
    method: method,
    priority: priority)
    {
        _arg = arg;
    }

    protected sealed override void InvokeDelegateCore()
    {
        Action<TArg> action = (Action<TArg>)_method;
        action(_arg);
    }
}
