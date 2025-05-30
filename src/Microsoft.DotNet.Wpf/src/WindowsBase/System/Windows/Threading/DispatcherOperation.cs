// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using MS.Internal;

namespace System.Windows.Threading
{
    /// <summary>
    ///     DispatcherOperation represents a delegate that has been
    ///     posted to the Dispatcher queue.
    /// </summary>
    public abstract partial class DispatcherOperation
    {
        private CulturePreservingExecutionContext _executionContext;

        private protected readonly Dispatcher _dispatcher;
        private protected readonly Delegate _method;

        private protected readonly bool _useAsyncSemantics;

        private protected DispatcherPriority _priority;
        private protected Exception _exception;

        internal PriorityItem<DispatcherOperation> _item; // The Dispatcher sets this when it enqueues/deques the item.
        internal DispatcherOperationStatus _status; // set from Dispatcher

        private EventHandler _aborted;
        private EventHandler _completed;

        internal readonly DispatcherOperationTaskSource _taskSource; // also used from Dispatcher

        internal DispatcherOperation(Dispatcher dispatcher, Delegate method, DispatcherPriority priority,
                                     DispatcherOperationTaskSource taskSource, bool useAsyncSemantics)
        {
            _dispatcher = dispatcher;
            _method = method;
            _priority = priority;

            _executionContext = CulturePreservingExecutionContext.Capture();

            _taskSource = taskSource;
            _taskSource.Initialize(this);

            _useAsyncSemantics = useAsyncSemantics;
        }

        /// <summary>
        ///     Returns the Dispatcher that this operation was posted to.
        /// </summary>
        public Dispatcher Dispatcher => _dispatcher;

        private Lock DispatcherLock => _dispatcher._instanceLock;

        /// <summary>
        ///     The status of this operation.
        /// </summary>
        public DispatcherOperationStatus Status => _status;

        /// <summary>
        ///     Returns a Task representing the operation.
        /// </summary>
        public Task Task => _taskSource.GetTask();

        /// <summary>
        ///     Name of this operation.
        /// </summary>
        /// <returns>
        ///     Returns a string representation of the operation to be invoked.
        /// </returns>
        internal string Name => $"{_method.Method.DeclaringType}.{_method.Method.Name}";

        /// <summary>
        ///     Gets or sets the priority of this operation within the
        ///     Dispatcher queue.
        /// </summary>
        public DispatcherPriority Priority // NOTE: should be Priority
        {
            get => _priority;
            set
            {
                Dispatcher.ValidatePriority(value, "value");

                if (value != _priority && _dispatcher.SetPriority(this, value))
                {
                    _priority = value;
                }
            }
        }

        /// <summary>
        ///     Returns an awaiter for awaiting the completion of the operation.
        /// </summary>
        /// <remarks>
        ///     This method is intended to be used by compilers.
        /// </remarks>
        [Browsable(false), EditorBrowsable(EditorBrowsableState.Never)]
        public TaskAwaiter GetAwaiter()
        {
            return Task.GetAwaiter();
        }

        /// <summary>
        ///     Waits for this operation to complete.
        /// </summary>
        /// <returns>
        ///     The status of the operation.  To obtain the return value
        ///     of the invoked delegate, use the the Result property.
        /// </returns>
        public DispatcherOperationStatus Wait()
        {
            return Wait(TimeSpan.FromMilliseconds(-1));
        }

        /// <summary>
        ///     Waits for this operation to complete.
        /// </summary>
        /// <param name="timeout">
        ///     The maximum amount of time to wait.
        /// </param>
        /// <returns>
        ///     The status of the operation.  To obtain the return value
        ///     of the invoked delegate, use the the Result property.
        /// </returns>
        public DispatcherOperationStatus Wait(TimeSpan timeout)
        {
            if ((_status == DispatcherOperationStatus.Pending || _status == DispatcherOperationStatus.Executing) &&
                timeout.TotalMilliseconds != 0)
            {
                if (_dispatcher.Thread == Thread.CurrentThread)
                {
                    if (_status == DispatcherOperationStatus.Executing)
                    {
                        // We are the dispatching thread, and the current operation state is
                        // executing, which means that the operation is in the middle of
                        // executing (on this thread) and is trying to wait for the execution
                        // to complete.  Unfortunately, the thread will now deadlock, so
                        // we throw an exception instead.
                        throw new InvalidOperationException(SR.ThreadMayNotWaitOnOperationsAlreadyExecutingOnTheSameThread);
                    }

                    // We are the dispatching thread for this operation, so
                    // we can't block.  We will push a frame instead.
                    DispatcherOperationFrame frame = new DispatcherOperationFrame(this, timeout);
                    Dispatcher.PushFrame(frame);
                }
                else
                {
                    // We are some external thread, so we can just block.  Of
                    // course this means that the Dispatcher (queue)for this
                    // thread (if any) is now blocked.  The COM STA model 
                    // suggests that we should pump certain messages so that
                    // back-communication can happen.  Underneath us, the CLR
                    // will pump the STA apartment for us, and we will allow 
                    // the UI thread for a context to call
                    // Invoke(Priority.Max, ...) without going through the
                    // blocked queue.
                    DispatcherOperationEvent wait = new DispatcherOperationEvent(this, timeout);
                    wait.WaitOne();
                }
            }

            if (_useAsyncSemantics)
            {
                if (_status is DispatcherOperationStatus.Completed or DispatcherOperationStatus.Aborted)
                {
                    // We know the operation has completed, so it safe to ask
                    // the task for the Awaiter, and the awaiter for the result.
                    // We don't actually care about the result, but this gives the
                    // Task the chance to throw any captured exceptions.
                    Task.GetAwaiter().GetResult();
                }
            }

            return _status;
        }

        /// <summary>
        ///     Aborts this operation.
        /// </summary>
        /// <returns>
        ///     False if the operation could not be aborted (because the
        ///     operation was already in  progress)
        /// </returns>
        /// <remarks>
        ///     Aborting an operation will try to remove an operation from the
        ///     Dispatcher queue so that it is never invoked.  If successful,
        ///     the associated task is also marked as canceled.
        /// </remarks>
        public bool Abort()
        {
            bool removed = false;

            if (_dispatcher != null)
            {
                removed = _dispatcher.Abort(this);

                if (removed)
                {
                    // Mark the task as canceled so that continuations will be invoked.
                    _taskSource.SetCanceled();

                    // Raise the aborted event.
                    _aborted?.Invoke(this, EventArgs.Empty);
                }
            }

            return removed;
        }

        /// <summary>
        ///     ID of this operation.
        /// </summary>
        /// <returns>
        ///     Returns a "roaming" ID. This ID changes as the object is relocated by the GC.
        ///     However ETW tools listening for events containing these "roaming" IDs will be
        ///     able to account for the movements by listening for CLR's GC ETW events, and
        ///     will therefore be able to track this identity across the lifetime of the object.
        /// </returns>
        internal long Id
        {
            get
            {
                long addr;
                unsafe
                {
                    // we need a non-readonly field of a pointer-compatible type (using _priority)
                    fixed (DispatcherPriority* pb = &_priority)
                    {
                        addr = (long)pb;
                    }
                }
                return addr;
            }
        }

        /// <summary>
        ///     Returns the result of the operation if it has completed.
        /// </summary>
        public virtual object Result { get; }

        /// <summary>
        ///     An event that is raised when the operation is aborted or canceled.
        /// </summary>
        public event EventHandler Aborted
        {
            add
            {
                lock (DispatcherLock)
                {
                    _aborted = (EventHandler)Delegate.Combine(_aborted, value);
                }
            }
            remove
            {
                lock (DispatcherLock)
                {
                    _aborted = (EventHandler)Delegate.Remove(_aborted, value);
                }
            }
        }

        /// <summary>
        ///     An event that is raised when the operation completes.
        /// </summary>
        /// <remarks>
        ///     Completed indicates that the operation was invoked and has
        ///     either completed successfully or faulted. Note that a canceled
        ///     or aborted operation is never is never considered completed.
        /// </remarks>
        public event EventHandler Completed
        {
            add
            {
                lock (DispatcherLock)
                {
                    _completed = (EventHandler)Delegate.Combine(_completed, value);
                }
            }
            remove
            {
                lock (DispatcherLock)
                {
                    _completed = (EventHandler)Delegate.Remove(_completed, value);
                }
            }
        }

        // Note: this is called by the Dispatcher to actually invoke the completions for the operation.
        internal abstract void InvokeCompletions();

        // Invoke --> InvokeImpl
        private protected abstract void InvokeImpl();

        // Note: this is called by the Dispatcher to actually invoke the operation.
        // Invoke --> InvokeImpl
        internal void Invoke()
        {
            // Mark this operation as executing.
            _status = DispatcherOperationStatus.Executing;

            // Invoke the operation under the execution context that was
            // current when the operation was created.
            if (_executionContext != null)
            {
                CulturePreservingExecutionContext.Run(_executionContext, static (state) => ((DispatcherOperation)state).InvokeImpl(), this);

                // Release any resources held by the execution context.
                _executionContext.Dispose();
                _executionContext = null;
            }
            else
            {
                // _executionContext can be null if someone called
                // ExecutionContext.SuppressFlow before calling BeginInvoke/Invoke.
                // In this case we'll just call the invocation directly.
                InvokeImpl();
            }

            EventHandler handler; // either completed or aborted
            lock (DispatcherLock)
            {
                if (_exception is OperationCanceledException)
                {
                    // A new way to abort/cancel an operation is to raise an
                    // OperationCanceledException exception.  This only works
                    // from the new APIs; the old APIs would flow the exception
                    // up through the Dispatcher.UnhandledException handling.
                    // 
                    // Note that programmatically calling
                    // DispatcherOperation.Abort sets the status and raises the
                    // Aborted event itself.
                    handler = _aborted;
                    _status = DispatcherOperationStatus.Aborted;
                }
                else
                {
                    // The operation either completed, or a new version threw an
                    // exception and we caught it.  There is no separate event
                    // for this, so we raise the same Completed event for both.
                    handler = _completed;
                    _status = DispatcherOperationStatus.Completed;
                }
            }

            handler?.Invoke(this, EventArgs.Empty);
        }
    }
}
