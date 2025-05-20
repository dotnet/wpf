// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading;
using MS.Internal;

namespace System.Windows.Threading;

/// <summary>
/// DispatcherOperation represents a delegate that has been posted to the <see cref="Dispatcher"/> queue.
/// </summary>
internal sealed class DispatcherOperationAction : DispatcherOperation
{
    internal DispatcherOperationAction(Dispatcher dispatcher, DispatcherPriority priority, Action func) : base(
        dispatcher: dispatcher,
        method: func,
        priority: priority,
        taskSource: new DispatcherOperationTaskSource<object>(),
        useAsyncSemantics: true)
    {
    }

    public override object Result
    {
        get
        {
            // New semantics require waiting for the operation to
            // complete.
            //
            // Use DispatcherOperation.Wait instead of Task.Wait to handle
            // waiting on the same thread.
            Wait();

            if (_status is DispatcherOperationStatus.Completed or DispatcherOperationStatus.Aborted)
            {
                // We know the operation has completed, and the
                // _taskSource has been completed,  so it safe to ask
                // the task for the Awaiter, and the awaiter for the result.
                // We don't actually care about the result, but this gives the
                // Task the chance to throw any captured exceptions.
                Task.GetAwaiter().GetResult();
            }

            return null;
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
                if (_exception != null)
                {
                    _taskSource.SetException(_exception);
                }
                else
                {
                    _taskSource.SetResult(null);
                }
                break;

            default:
                Invariant.Assert(false, "Operation should be either Aborted or Completed!");
                break;
        }
    }

    protected override void InvokeImpl()
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

            try
            {
                Action action = (Action)_method;
                action();
            }
            catch (Exception e)
            {
                // Remember this for the later call to InvokeCompletions.
                _exception = e;
            }
        }
        finally
        {
            SynchronizationContext.SetSynchronizationContext(oldSynchronizationContext);
        }
    }
}
