// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading;
using MS.Internal;

namespace System.Windows.Threading;

/// <summary>
/// DispatcherOperation represents a delegate that has been posted to the <see cref="Dispatcher"/> queue.
/// </summary>
internal sealed class DispatcherOperationLegacy : DispatcherOperation
{
    private readonly object _args;
    private readonly int _numArgs;
    private object _result;

    private DispatcherOperationLegacy(Dispatcher dispatcher, Delegate method, DispatcherPriority priority, object args, int numArgs,
        DispatcherOperationTaskSource taskSource, bool useAsyncSemantics) : base(dispatcher, method, priority, taskSource, useAsyncSemantics)
    {
        _numArgs = numArgs;
        _args = args;
    }

    internal DispatcherOperationLegacy(Dispatcher dispatcher, Delegate method, DispatcherPriority priority, object args, int numArgs) : this(
        dispatcher, method, priority, args, numArgs, new DispatcherOperationTaskSource<object>(), useAsyncSemantics: false)
    {
    }

    public override object Result
    {
        get
        {
            return _result;
        }
    }

    internal override void InvokeCompletions()
    {
        switch (_status)
        {
            case DispatcherOperationStatus.Aborted:
                _taskSource.SetCanceled();
                break;
            case DispatcherOperationStatus.Completed:
                if (_exception is not null)
                {
                    _taskSource.SetException(_exception);
                }
                else
                {
                    _taskSource.SetResult(_result);
                }
                break;

            default:
                Invariant.Assert(false, "Operation should be either Aborted or Completed!");
                break;
        }
    }

    private protected sealed override void InvokeImpl()
    {
        SynchronizationContext oldSynchronizationContext = SynchronizationContext.Current;

        try
        {
            // We are executing under the "foreign" execution context, but the SynchronizationContext
            // must be for the correct dispatcher and the correct priority (since NETFX 4.5).
            DispatcherSynchronizationContext newSynchronizationContext = DispatcherUtils.GetOrCreateContext(_dispatcher, _priority);

            SynchronizationContext.SetSynchronizationContext(newSynchronizationContext);

            // Win32 considers timers to be low priority. Avalon does not, since different timers are
            // associated with different priorities. So we promote the timers before we invoke any work items.
            _dispatcher.PromoteTimers(Environment.TickCount);

            // Invoke the delegate and route exceptions through the dispatcher events.
            _result = _dispatcher.WrappedInvoke(_method, _args, _numArgs, null);

            // Note: we do not catch exceptions, they flow out the the Dispatcher.UnhandledException handling.
        }
        finally
        {
            SynchronizationContext.SetSynchronizationContext(oldSynchronizationContext);
        }
    }
}
