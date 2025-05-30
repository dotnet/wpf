// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;

namespace System.Windows.Threading;

/// <summary>
/// DispatcherOperation represents a delegate that has been posted to the <see cref="Dispatcher"/> queue.
/// </summary>
internal sealed class DispatcherOperation<TArg1, TArg2, TArg3, TResult> : DispatcherOperation<TResult>
{
    private readonly TArg1 _arg1;
    private readonly TArg2 _arg2;
    private readonly TArg3 _arg3;

    internal DispatcherOperation(Dispatcher dispatcher, DispatcherPriority priority, Func<TArg1, TArg2, TArg3, TResult> func,
        TArg1 arg1, TArg2 arg2, TArg3 arg3) : base(dispatcher, priority, func)
    {
        _arg1 = arg1;
        _arg2 = arg2;
        _arg3 = arg3;
    }

    protected sealed override TResult InvokeDelegateCore()
    {
        Func<TArg1, TArg2, TArg3, TResult> func = Unsafe.As<Func<TArg1, TArg2, TArg3, TResult>>(_method);
        return func(_arg1, _arg2, _arg3);
    }
}

