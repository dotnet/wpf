// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;

namespace System.Windows.Threading;

/// <summary>
/// DispatcherOperation represents a delegate that has been posted to the <see cref="Dispatcher"/> queue.
/// </summary>
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

    private protected sealed override void InvokeDelegateCore()
    {
        Action<TArg> action = Unsafe.As<Action<TArg>>(_method);
        action(_arg);
    }
}
