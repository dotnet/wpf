// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;

namespace System.Windows.Threading;

/// <summary>
/// DispatcherOperation represents a delegate that has been posted to the <see cref="Dispatcher"/> queue.
/// </summary>
internal sealed class DispatcherOperation<TArg, TResult> : DispatcherOperation<TResult>
{
    private readonly TArg _arg;

    internal DispatcherOperation(Dispatcher dispatcher, DispatcherPriority priority,
        Func<TArg, TResult> func, TArg arg) : base(dispatcher, priority, func)
    {
        _arg = arg;
    }

    private protected sealed override TResult InvokeDelegateCore()
    {
        Func<TArg, TResult> func = Unsafe.As<Func<TArg, TResult>>(_method);
        return func(_arg);
    }
}

