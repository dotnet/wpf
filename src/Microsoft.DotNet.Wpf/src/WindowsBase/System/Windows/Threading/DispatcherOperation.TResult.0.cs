// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using MS.Internal;

namespace System.Windows.Threading;

/// <summary>
/// DispatcherOperation represents a delegate that has been posted to the <see cref="Dispatcher"/> queue.
/// </summary>
public class DispatcherOperation<TResult> : DispatcherOperation
{
    private TResult _result;

    internal DispatcherOperation(Dispatcher dispatcher, DispatcherPriority priority, Delegate func) : base(
            dispatcher: dispatcher,
            method: func,
            priority: priority,
            taskSource: new DispatcherOperationTaskSource<TResult>(),
            useAsyncSemantics: true)
    {
    }

    /// <summary>
    ///     Returns a Task representing the operation.
    /// </summary>
    public new Task<TResult> Task
    {
        get
        {
            // Just upcast the base Task to what it really is.
            return (Task<TResult>)((DispatcherOperation)this).Task;
        }
    }

    /// <summary>
    ///     Returns an awaiter for awaiting the completion of the operation.
    /// </summary>
    /// <remarks>
    ///     This method is intended to be used by compilers.
    /// </remarks>
    [Browsable(false), EditorBrowsable(EditorBrowsableState.Never)]
    public new TaskAwaiter<TResult> GetAwaiter()
    {
        return Task.GetAwaiter();
    }

    /// <summary>
    ///     Returns the result of the operation if it has completed.
    /// </summary>
    public new TResult Result
    {
        get
        {
            // New semantics require waiting for the operation to complete.
            //
            // Use DispatcherOperation.Wait instead of Task.Wait to handle waiting on the same thread.
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

            return _result;
        }
    }

    internal sealed override void InvokeCompletions()
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
                    // Make sure we don't box at this stage
                    ((DispatcherOperationTaskSource<TResult>)_taskSource).SetResult(_result);
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

            try
            {
                _result = InvokeDelegateCore();
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

    private protected virtual TResult InvokeDelegateCore()
    {
        Func<TResult> func = (Func<TResult>)_method;
        return func();
    }
}
