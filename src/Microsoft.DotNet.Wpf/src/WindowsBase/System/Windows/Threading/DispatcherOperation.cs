// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Security;
using System.Threading;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using MS.Internal.WindowsBase;
using MS.Internal;

namespace System.Windows.Threading
{
    /// <summary>
    ///     DispatcherOperation represents a delegate that has been
    ///     posted to the Dispatcher queue.
    /// </summary>
    public class DispatcherOperation
    {
        static DispatcherOperation()
        {
            _invokeInSecurityContext = new ContextCallback(InvokeInSecurityContext);
        }

        internal DispatcherOperation(
            Dispatcher dispatcher,
            Delegate method,
            DispatcherPriority priority,
            object args,
            int numArgs,
            DispatcherOperationTaskSource taskSource,
            bool useAsyncSemantics)
        {
            _dispatcher = dispatcher;
            _method = method;
            _priority = priority;
            _numArgs = numArgs;
            _args = args;

            _executionContext = CulturePreservingExecutionContext.Capture();

            _taskSource = taskSource;
            _taskSource.Initialize(this);
            
            _useAsyncSemantics = useAsyncSemantics;
        }

        internal DispatcherOperation(
            Dispatcher dispatcher,
            Delegate method,
            DispatcherPriority priority,
            object args,
            int numArgs) : this(
                dispatcher,
                method,
                priority,
                args,
                numArgs,
                new DispatcherOperationTaskSource<object>(),
                false)
        {
        }

        internal DispatcherOperation(
            Dispatcher dispatcher,
            DispatcherPriority priority,
            Action action) : this(
                dispatcher,
                action,
                priority,
                null,
                0,
                new DispatcherOperationTaskSource<object>(),
                true)
        {
        }        

        internal DispatcherOperation(
            Dispatcher dispatcher,
            DispatcherPriority priority,
            Delegate method,
            object[] args) : this(
                dispatcher,
                method,
                priority,
                args,
                -1,
                new DispatcherOperationTaskSource<object>(),
                true)
        {
        }        

        /// <summary>
        ///     Returns the Dispatcher that this operation was posted to.
        /// </summary>
        public Dispatcher Dispatcher
        {
            get
            {
                return _dispatcher;
            }
        }

        /// <summary>
        ///     Gets or sets the priority of this operation within the
        ///     Dispatcher queue.
        /// </summary>
        public DispatcherPriority Priority // NOTE: should be Priority
        {
            get 
            {
                return _priority;
            }
            
            set
            {
                Dispatcher.ValidatePriority(value, "value");
                
                if(value != _priority && _dispatcher.SetPriority(this, value))
                {
                    _priority = value;
                }
            }
        }

        /// <summary>
        ///     The status of this operation.
        /// </summary>
        public DispatcherOperationStatus Status
        {
            get
            {
                return _status;
            }
        }

        /// <summary>
        ///     Returns a Task representing the operation.
        /// </summary>
        public Task Task
        {
            get
            {
                return _taskSource.GetTask();
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
            if((_status == DispatcherOperationStatus.Pending || _status == DispatcherOperationStatus.Executing) &&
                timeout.TotalMilliseconds != 0)
            {
                if(_dispatcher.Thread == Thread.CurrentThread)
                {
                    if(_status == DispatcherOperationStatus.Executing)
                    {
                        // We are the dispatching thread, and the current operation state is
                        // executing, which means that the operation is in the middle of
                        // executing (on this thread) and is trying to wait for the execution
                        // to complete.  Unfortunately, the thread will now deadlock, so
                        // we throw an exception instead.
                        throw new InvalidOperationException(SR.Get(SRID.ThreadMayNotWaitOnOperationsAlreadyExecutingOnTheSameThread));
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

            if(_useAsyncSemantics)
            {
                if(_status == DispatcherOperationStatus.Completed ||
                   _status == DispatcherOperationStatus.Aborted)
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
                    EventHandler aborted = _aborted;
                    if (aborted != null)
                    {
                        aborted(this, EventArgs.Empty);
                    }
                }
            }

            return removed;
        }

        /// <summary>
        ///     Name of this operation.
        /// </summary>
        /// <returns>
        ///     Returns a string representation of the operation to be invoked.
        /// </returns>
        internal String Name
        {
            get
            {
                return _method.Method.DeclaringType + "." + _method.Method.Name;
            }
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
                    fixed (DispatcherPriority* pb = &this._priority)
                    {
                        addr = (long) pb;
                    }
                }
                return addr;
            }
        }
        
        /// <summary>
        ///     Returns the result of the operation if it has completed.
        /// </summary>
        public object Result 
        {
            get 
            {
                if(_useAsyncSemantics)
                {
                    // New semantics require waiting for the operation to
                    // complete.
                    //
                    // Use DispatcherOperation.Wait instead of Task.Wait to handle
                    // waiting on the same thread.
                    Wait();

                    if(_status == DispatcherOperationStatus.Completed ||
                       _status == DispatcherOperationStatus.Aborted)
                    {
                        // We know the operation has completed, and the
                        // _taskSource has been completed,  so it safe to ask
                        // the task for the Awaiter, and the awaiter for the result.
                        // We don't actually care about the result, but this gives the
                        // Task the chance to throw any captured exceptions.
                        Task.GetAwaiter().GetResult();
                    }
                }
                
                return _result;
            }
        }

        /// <summary>
        ///     An event that is raised when the operation is aborted or canceled.
        /// </summary>
        public event EventHandler Aborted
        {
            add
            {
                lock (DispatcherLock)
                {
                    _aborted = (EventHandler) Delegate.Combine(_aborted, value);
                }
            }

            remove
            {
                lock(DispatcherLock)
                {
                    _aborted = (EventHandler) Delegate.Remove(_aborted, value);
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
                    _completed = (EventHandler) Delegate.Combine(_completed, value);
                }
            }
        
            remove
            {
                lock(DispatcherLock)
                {
                    _completed = (EventHandler) Delegate.Remove(_completed, value);
                }
            }
        }
        
        // Note: this is called by the Dispatcher to actually invoke the operation.
        // Invoke --> InvokeInSecurityContext --> InvokeImpl
        internal void Invoke()
        {
            // Mark this operation as executing.
            _status = DispatcherOperationStatus.Executing;

            // Invoke the operation under the execution context that was
            // current when the operation was created.
            if(_executionContext != null)
            {
                CulturePreservingExecutionContext.Run(_executionContext, _invokeInSecurityContext, this);

                // Release any resources held by the execution context.
                _executionContext.Dispose();
                _executionContext = null;
            }
            else
            {
                // _executionContext can be null if someone called
                // ExecutionContext.SupressFlow before calling BeginInvoke/Invoke.
                // In this case we'll just call the invokation directly.
                // SupressFlow is a privileged operation, so this is not a
                // security hole.
                _invokeInSecurityContext(this);
            }

            EventHandler handler; // either completed or aborted
            lock(DispatcherLock)
            {
                if(_exception != null && _exception is OperationCanceledException)
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
                    // exception and we caught it.  There is no seperate event
                    // for this, so we raise the same Completed event for both.
                    handler = _completed;
                    _status = DispatcherOperationStatus.Completed;
                }
            }
                
            if(handler != null)
            {
                handler(this, EventArgs.Empty);
            }
        }

        // Note: this is called by the Dispatcher to actually invoke the completions for the operation.
        internal void InvokeCompletions()
        {
            switch(_status)
            {
                case DispatcherOperationStatus.Aborted:
                    _taskSource.SetCanceled();
                    break;

                case DispatcherOperationStatus.Completed:
                    if(_exception != null)
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
        

        // Invoke --> InvokeInSecurityContext --> InvokeImpl
        private static void InvokeInSecurityContext(Object state)
        {
            DispatcherOperation operation = (DispatcherOperation) state;
            operation.InvokeImpl();
        }

        // Invoke --> InvokeInSecurityContext --> InvokeImpl
        private void InvokeImpl()
        {
            SynchronizationContext oldSynchronizationContext = SynchronizationContext.Current;

            try
            {
                // We are executing under the "foreign" execution context, but the
                // SynchronizationContext must be for the correct dispatcher and
                // priority.
                DispatcherSynchronizationContext newSynchronizationContext;
                if(BaseCompatibilityPreferences.GetReuseDispatcherSynchronizationContextInstance())
                {
                    newSynchronizationContext = Dispatcher._defaultDispatcherSynchronizationContext;
                }
                else
                {
                    if(BaseCompatibilityPreferences.GetFlowDispatcherSynchronizationContextPriority())
                    {
                        newSynchronizationContext = new DispatcherSynchronizationContext(_dispatcher, _priority);
                    }
                    else
                    {
                        newSynchronizationContext = new DispatcherSynchronizationContext(_dispatcher, DispatcherPriority.Normal);
                    }
                }
                SynchronizationContext.SetSynchronizationContext(newSynchronizationContext);


                // Win32 considers timers to be low priority.  Avalon does not, since different timers
                // are associated with different priorities.  So we promote the timers before we
                // invoke any work items.
                _dispatcher.PromoteTimers(Environment.TickCount);
                
                if(_useAsyncSemantics)
                {
                    try
                    {
                        _result = InvokeDelegateCore();
                    }
                    catch(Exception e)
                    {
                        // Remember this for the later call to InvokeCompletions.
                        _exception = e;
                    }
                }
                else
                {
                    // Invoke the delegate and route exceptions through the dispatcher events.
                    _result = _dispatcher.WrappedInvoke(_method, _args, _numArgs, null);

                    // Note: we do not catch exceptions, they flow out the the Dispatcher.UnhandledException handling.
                }
            }
            finally
            {
                SynchronizationContext.SetSynchronizationContext(oldSynchronizationContext);
            }
        }

        protected virtual object InvokeDelegateCore()
        {
            Action action = (Action) _method;
            action();
            return null;
        }

        private class DispatcherOperationFrame : DispatcherFrame
        {
            // Note: we pass "exitWhenRequested=false" to the base
            // DispatcherFrame construsctor because we do not want to exit
            // this frame if the dispatcher is shutting down. This is
            // because we may need to invoke operations during the shutdown process.
            public DispatcherOperationFrame(DispatcherOperation op, TimeSpan timeout) : base(false)
            {
                _operation = op;
                
                // We will exit this frame once the operation is completed or aborted.
                _operation.Aborted += new EventHandler(OnCompletedOrAborted);
                _operation.Completed += new EventHandler(OnCompletedOrAborted);

                // We will exit the frame if the operation is not completed within
                // the requested timeout.
                if(timeout.TotalMilliseconds > 0)
                {
                    _waitTimer = new Timer(new TimerCallback(OnTimeout),
                                           null,
                                           timeout,
                                           TimeSpan.FromMilliseconds(-1));
                }

                // Some other thread could have aborted the operation while we were
                // setting up the handlers.  We check the state again and mark the
                // frame as "should not continue" if this happened.
                if(_operation._status != DispatcherOperationStatus.Pending)
                {
                    Exit();
                }
}
            
            private void OnCompletedOrAborted(object sender, EventArgs e)
            {
                Exit();
            }
            
            private void OnTimeout(object arg)
            {
                Exit();
            }

            private void Exit()
            {
                Continue = false;

                if(_waitTimer != null)
                {
                    _waitTimer.Dispose();
                }

                _operation.Aborted -= new EventHandler(OnCompletedOrAborted);
                _operation.Completed -= new EventHandler(OnCompletedOrAborted);
            }

            private DispatcherOperation _operation;
            private Timer _waitTimer;
        }
        
        private class DispatcherOperationEvent
        {
            public DispatcherOperationEvent(DispatcherOperation op, TimeSpan timeout)
            {
                _operation = op;
                _timeout = timeout;
                _event = new ManualResetEvent(false);
                _eventClosed = false;
                
                lock(DispatcherLock)
                {
                    // We will set our event once the operation is completed or aborted.
                    _operation.Aborted += new EventHandler(OnCompletedOrAborted);
                    _operation.Completed += new EventHandler(OnCompletedOrAborted);

                    // Since some other thread is dispatching this operation, it could
                    // have been dispatched while we were setting up the handlers.
                    // We check the state again and set the event ourselves if this
                    // happened.
                    if(_operation._status != DispatcherOperationStatus.Pending && _operation._status != DispatcherOperationStatus.Executing)
                    {
                        _event.Set();
                    }
                }
            }
            
            private void OnCompletedOrAborted(object sender, EventArgs e)
            {
                lock(DispatcherLock)
                {
                    if(!_eventClosed)
                    {
                        _event.Set();
                    }
                }
            }

            public void WaitOne()
            {
                _event.WaitOne(_timeout, false);

                lock(DispatcherLock)
                {
                    if(!_eventClosed)
                    {
                        // Cleanup the events.
                        _operation.Aborted -= new EventHandler(OnCompletedOrAborted);
                        _operation.Completed -= new EventHandler(OnCompletedOrAborted);

                        // Close the event immediately instead of waiting for a GC
                        // because the Dispatcher is a a high-activity component and
                        // we could run out of events.
                        _event.Close();

                        _eventClosed = true;
                    }
                }
            }

            private object DispatcherLock
            {
                get { return _operation.DispatcherLock; }
            }
            
            private DispatcherOperation _operation;
            private TimeSpan _timeout;            
            private ManualResetEvent _event;
            private bool _eventClosed;
        }

        private object DispatcherLock
        {
            get { return _dispatcher._instanceLock; }
        }
        
        private CulturePreservingExecutionContext _executionContext;
        private static readonly ContextCallback _invokeInSecurityContext;
        
        private readonly Dispatcher _dispatcher;
        private DispatcherPriority _priority;
        internal readonly Delegate _method;
        private readonly object _args;
        private readonly int _numArgs;
        
        internal DispatcherOperationStatus _status; // set from Dispatcher
        private object _result;
        private Exception _exception;

        internal PriorityItem<DispatcherOperation> _item; // The Dispatcher sets this when it enques/deques the item.
        
        EventHandler _aborted;
        EventHandler _completed;

        internal readonly DispatcherOperationTaskSource _taskSource; // also used from Dispatcher
        private readonly bool _useAsyncSemantics;
    }

    /// <summary>
    ///     DispatcherOperation represents a delegate that has been
    ///     posted to the Dispatcher queue.
    /// </summary>
    public class DispatcherOperation<TResult> : DispatcherOperation
    {
        internal DispatcherOperation(
            Dispatcher dispatcher,
            DispatcherPriority priority,
            Func<TResult> func) : base(
                dispatcher,
                func,
                priority,
                null,
                0,
                new DispatcherOperationTaskSource<TResult>(),
                true)
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
                return (TResult) ((DispatcherOperation)this).Result;
            }
        }

        protected override object InvokeDelegateCore()
        {
            Func<TResult> func = (Func<TResult>) _method;
            return func();
        }
}

    /// <summary>
    ///     A convenient delegate to use for dispatcher operations.
    /// </summary>
    public delegate object DispatcherOperationCallback(object arg);
}
