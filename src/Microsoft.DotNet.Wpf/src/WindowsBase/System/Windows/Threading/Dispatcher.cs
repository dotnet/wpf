// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;                                // Console
using System.Collections.Generic;            // List<T>
using MS.Win32;                              // win32 interop
using System.Windows.Interop;                // ComponentDispatcher & MSG
using Microsoft.Win32;                       // Registry
using System.Security;                       // CAS
using System.Diagnostics;                    // Debug
using MS.Utility;                            // EventTrace
using System.Reflection;                     // Assembly
using System.Runtime.InteropServices;        // SEHException
using MS.Internal;                           // SecurityCriticalData, TextServicesInterop
using MS.Internal.Interop;                   // WM
using MS.Internal.WindowsBase;               // SecurityHelper
using System.Threading;
using System.ComponentModel;                 // EditorBrowsableAttribute, BrowsableAttribute
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

// Disabling 1634 and 1691:
// In order to avoid generating warnings about unknown message numbers and
// unknown pragmas when compiling C# source code with the C# compiler,
// you need to disable warnings 1634 and 1691. (Presharp Documentation)
#pragma warning disable 1634, 1691

namespace System.Windows.Threading
{
    /// <summary>
    ///     Provides UI services for a thread.
    /// </summary>
    public sealed class Dispatcher
    {
        static Dispatcher()
        {
            _msgProcessQueue = UnsafeNativeMethods.RegisterWindowMessage("DispatcherProcessQueue");
            _globalLock = new object();
            _dispatchers = new List<WeakReference>();
            _possibleDispatcher = new WeakReference(null);
            _exceptionWrapper = new ExceptionWrapper();
            _exceptionWrapper.Catch += new ExceptionWrapper.CatchHandler(CatchExceptionStatic);
            _exceptionWrapper.Filter += new ExceptionWrapper.FilterHandler(ExceptionFilterStatic);
        }

        /// <summary>
        ///     Returns the Dispatcher for the calling thread.
        /// </summary>
        /// <remarks>
        ///     If there is no dispatcher available for the current thread,
        ///     and the thread allows a dispatcher to be auto-created, a new
        ///     dispatcher will be created.
        ///     <P/>
        ///     If there is no dispatcher for the current thread, and thread
        ///     does not allow one to be auto-created, an exception is thrown.
        /// </remarks>
        public static Dispatcher CurrentDispatcher
        {
            get
            {
                // Find the dispatcher for this thread.
                Dispatcher currentDispatcher = FromThread(Thread.CurrentThread);;

                // Auto-create the dispatcher if there is no dispatcher for
                // this thread (if we are allowed to).
                if(currentDispatcher == null)
                {
                    currentDispatcher = new Dispatcher();
                }

                return currentDispatcher;
            }
        }


        /// <summary>
        ///     Returns the Dispatcher for the specified thread.
        /// </summary>
        /// <remarks>
        ///     If there is no dispatcher available for the specified thread,
        ///     this method will return null.
        /// </remarks>
        public static Dispatcher FromThread(Thread thread)
        {
            lock(_globalLock)
            {
                Dispatcher dispatcher = null;

                if(thread != null)
                {
                    // Shortcut: we track one static reference to the last current
                    // dispatcher we gave out.  For single-threaded apps, this will
                    // be set all the time.  For multi-threaded apps, this will be
                    // set for periods of time during which accessing CurrentDispatcher
                    // is cheap.  When a thread switch happens, the next call to
                    // CurrentDispatcher is expensive, but then the rest are fast
                    // again.
                    dispatcher = _possibleDispatcher.Target as Dispatcher;
                    if(dispatcher == null || dispatcher.Thread != thread)
                    {
                        // The "possible" dispatcher either was null or belongs to
                        // the a different thread.
                        dispatcher = null;

                        // Spin over the list of dispatchers looking for one that belongs
                        // to this thread.  We could use TLS here, but managed TLS is very
                        // expensive, so we think it is cheaper to search our own data
                        // structure.
                        //
                        // Note: Do not cache _dispatchers.Count because we rely on it
                        // being updated if we encounter a dead weak reference.
                        for(int i = 0; i < _dispatchers.Count; i++)
                        {
                            Dispatcher d = _dispatchers[i].Target as Dispatcher;
                            if(d != null)
                            {
                                // Note: we compare the thread objects themselves to protect
                                // against threads reusing old thread IDs.
                                Thread dispatcherThread = d.Thread;
                                if(dispatcherThread == thread)
                                {
                                    dispatcher = d;

                                    // Do not exit the loop early since we are also
                                    // looking for dead references.
                                }
                            }
                            else
                            {
                                // We found a dead reference, so remove it from
                                // the list, and adjust the index so we account
                                // for it.
                                _dispatchers.RemoveAt(i);
                                i--;
                            }
                        }

                        // Stash this dispatcher as a "possible" dispatcher for the
                        // next call to FromThread.
                        if(dispatcher != null)
                        {
                            // We expect this call to be frequent so we want to
                            // avoid uneccesary allocations, such as a new
                            // WeakReference.  However, we discovered late
                            // in Dev11 that sometimes this code is called
                            // during finalization, and the existing
                            // WeakReference may throw when you try to change
                            // the Target because the GC has already discarded
                            // the handle for the WeakReference.
                            //
                            // Ideally we would re-work the code to avoid
                            // calling this method from a finalizer path.  But
                            // that is tricky: we are destroying an HWND
                            // (appropriate for a finalizer) that has a managed
                            // WndProc, which calls Dispatcher.Invoke to get
                            // under the exception filters.  Changing all of that
                            // code would be very risky at this point.
                            //
                            // There is no good API to check if running on the
                            // finalizer thread, or if the handle of the
                            // WeakReference has been reclaimed by the GC.
                            // The best we can do is check IsAlive, which will
                            // return false if either the handle has been
                            // reclaimed or the target has been collected.
                            // If that happens, we allocate a new
                            // WeakReference instance, rather than reusing the
                            // existing one.
                            if(_possibleDispatcher.IsAlive)
                            {
                                _possibleDispatcher.Target = dispatcher;
                            }
                            else
                            {
                                _possibleDispatcher = new WeakReference(dispatcher);
                            }
                        }
}
                }

                return dispatcher;
            }
        }

        /// <summary>
        /// </summary>
        public Thread Thread
        {
            get
            {
                return _dispatcherThread;
            }
        }

        /// <summary>
        ///     Checks that the calling thread has access to this object.
        /// </summary>
        /// <remarks>
        ///     Only the dispatcher thread may access DispatcherObjects.
        ///     <p/>
        ///     This method is public so that any thread can probe to
        ///     see if it has access to the DispatcherObject.
        /// </remarks>
        /// <returns>
        ///     True if the calling thread has access to this object.
        /// </returns>
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        public bool CheckAccess()
        {
            return Thread == Thread.CurrentThread;
        }

        /// <summary>
        ///     Verifies that the calling thread has access to this object.
        /// </summary>
        /// <remarks>
        ///     Only the dispatcher thread may access DispatcherObjects.
        ///     <p/>
        ///     This method is public so that derived classes can probe to
        ///     see if the calling thread has access to itself.
        /// </remarks>
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        public void VerifyAccess()
        {
            if(!CheckAccess())
            {
                // Used to inline VerifyAccess.
                [DoesNotReturn]
                [MethodImpl(MethodImplOptions.NoInlining)]
                static void ThrowVerifyAccess()
                    => throw new InvalidOperationException(SR.VerifyAccess);

                ThrowVerifyAccess();
            }
        }

        /// <summary>
        ///     Begins the process of shutting down the dispatcher.
        /// </summary>
        /// <remarks>
        ///     This API demand unrestricted UI Permission
        /// </remarks>
        public void BeginInvokeShutdown(DispatcherPriority priority) // NOTE: should be Priority
        {

            BeginInvoke(priority, new ShutdownCallback(ShutdownCallbackInternal));
        }

        /// <summary>
        ///     Begins the process of shutting down the dispatcher.
        /// </summary>
        public void InvokeShutdown()
        {

            CriticalInvokeShutdown();
        }

        [FriendAccessAllowed] //used by Application.ShutdownImpl() in PresentationFramework
        internal void CriticalInvokeShutdown()
        {
            Invoke(DispatcherPriority.Send, new ShutdownCallback(ShutdownCallbackInternal)); // NOTE: should be Priority.Max
        }

        /// <summary>
        ///     Whether or not the dispatcher is shutting down.
        /// </summary>
        public bool HasShutdownStarted
        {
            get
            {
                return _hasShutdownStarted; // Free-Thread access OK.
            }
        }

        /// <summary>
        ///     Whether or not the dispatcher has been shut down.
        /// </summary>
        public bool HasShutdownFinished
        {
            get
            {
                return _hasShutdownFinished; // Free-Thread access OK.
            }
        }

        /// <summary>
        ///     Raised when the dispatcher is shutting down.
        /// </summary>
        public event EventHandler ShutdownStarted;

        /// <summary>
        ///     Raised when the dispatcher is shut down.
        /// </summary>
        public event EventHandler ShutdownFinished;

        /// <summary>
        ///     Push the main execution frame.
        /// </summary>
        /// <remarks>
        ///     This frame will continue until the dispatcher is shut down.
        /// </remarks>
        public static void Run()
        {
            PushFrame(new DispatcherFrame());
        }

        /// <summary>
        ///     Push an execution frame.
        /// </summary>
        /// <param name="frame">
        ///     The frame for the dispatcher to process.
        /// </param>
        public static void PushFrame(DispatcherFrame frame)
        {
            if(frame == null)
            {
                throw new ArgumentNullException("frame");
            }

            Dispatcher dispatcher = Dispatcher.CurrentDispatcher;
            if(dispatcher._hasShutdownFinished) // Dispatcher thread - no lock needed for read
            {
                throw new InvalidOperationException(SR.DispatcherHasShutdown);
            }

            if(frame.Dispatcher != dispatcher)
            {
                throw new InvalidOperationException(SR.MismatchedDispatchers);
            }

            if(dispatcher._disableProcessingCount > 0)
            {
                throw new InvalidOperationException(SR.DispatcherProcessingDisabled);
            }

            dispatcher.PushFrameImpl(frame);
        }

        /// <summary>
        ///     Requests that all nested frames exit.
        /// </summary>
        public static void ExitAllFrames()
        {

            Dispatcher dispatcher = Dispatcher.CurrentDispatcher;
            if(dispatcher._frameDepth > 0)
            {
                dispatcher._exitAllFrames = true;

                // Post a message so that the message pump will wake up and
                // check our continue state.
                dispatcher.BeginInvoke(DispatcherPriority.Send, (Action) delegate {});
            }
        }

        /// <summary>
        ///     Returns an awaitable object that can be used to queue
        ///     continuations at background priority.
        /// </summary>
        public static DispatcherPriorityAwaitable Yield()
        {
            return Yield(DispatcherPriority.Background);
        }

        /// <summary>
        ///     Returns an awaitable object that can be used to queue
        ///     continuations at the specified priority.
        /// </summary>
        public static DispatcherPriorityAwaitable Yield(DispatcherPriority priority)
        {
            ValidatePriority(priority, "priority");

            Dispatcher currentDispatcher = FromThread(Thread.CurrentThread);;
            if(currentDispatcher == null)
            {
                throw new InvalidOperationException(SR.DispatcherYieldNoAvailableDispatcher);
            }

            return new DispatcherPriorityAwaitable(currentDispatcher, priority);
        }

        /// <summary>
        ///     Executes the specified delegate asynchronously on the thread that
        ///     the Dispatcher was created on.
        /// </summary>
        /// <param name="priority">
        ///     The priority that determines in what order the specified method
        ///     is invoked relative to the other pending methods in the Dispatcher.
        /// </param>
        /// <param name="method">
        ///     A delegate to a method that takes no parameters.
        /// </param>
        /// <returns>
        ///     An IAsyncResult object that represents the result of the
        ///     BeginInvoke operation.
        /// </returns>
        [Browsable(false), EditorBrowsable(EditorBrowsableState.Never)]
        public DispatcherOperation BeginInvoke(DispatcherPriority priority, Delegate method) // NOTE: should be Priority
        {
            return LegacyBeginInvokeImpl(priority, method, null, 0);
        }

        /// <summary>
        ///     Executes the specified delegate asynchronously with the specified
        ///     arguments, on the thread that the Dispatcher was created on.
        /// </summary>
        /// <param name="priority">
        ///     The priority that determines in what order the specified method
        ///     is invoked relative to the other pending methods in the Dispatcher.
        /// </param>
        /// <param name="method">
        ///     A delegate to a method that takes parameters of the same number
        ///     and type that are contained in the args parameter.
        /// </param>
        /// <param name="arg">
        ///     A object to pass as an argument to the given method.
        ///     This can be null if no arguments are needed.
        /// </param>
        /// <returns>
        ///     An IAsyncResult object that represents the result of the
        ///     BeginInvoke operation.
        /// </returns>
        [Browsable(false), EditorBrowsable(EditorBrowsableState.Never)]
        public DispatcherOperation BeginInvoke(DispatcherPriority priority, Delegate method, object arg) // NOTE: should be Priority
        {
            return LegacyBeginInvokeImpl(priority, method, arg, 1);
        }

        /// <summary>
        ///     Executes the specified delegate asynchronously with the specified
        ///     arguments, on the thread that the Dispatcher was created on.
        /// </summary>
        /// <param name="priority">
        ///     The priority that determines in what order the specified method
        ///     is invoked relative to the other pending methods in the Dispatcher.
        /// </param>
        /// <param name="method">
        ///     A delegate to a method that takes parameters of the same number
        ///     and type that are contained in the args parameter.
        /// </param>
        /// <param name="arg">
        /// enh
        /// </param>
        /// <param name="args">
        ///     An array of objects to pass as arguments to the given method.
        ///     This can be null if no arguments are needed.
        /// </param>
        /// <returns>
        ///     An IAsyncResult object that represents the result of the
        ///     BeginInvoke operation.
        /// </returns>
        [Browsable(false), EditorBrowsable(EditorBrowsableState.Never)]
        public DispatcherOperation BeginInvoke(DispatcherPriority priority, Delegate method, object arg, params object[] args)
        {
            return LegacyBeginInvokeImpl(priority, method, CombineParameters(arg, args), -1);
        }

        /// <summary>
        ///     Executes the specified delegate asynchronously with the specified
        ///     arguments, on the thread that the Dispatcher was created on.
        /// </summary>
        /// <param name="method">
        ///     A delegate to a method that takes parameters of the same number
        ///     and type that are contained in the args parameter.
        /// </param>
        /// <param name="args">
        ///     An array of objects to pass as arguments to the given method.
        ///     This can be null if no arguments are needed.
        /// </param>
        /// <returns>
        ///     An IAsyncResult object that represents the result of the
        ///     BeginInvoke operation.
        /// </returns>
        public DispatcherOperation BeginInvoke(Delegate method, params object[] args)
        {
            return LegacyBeginInvokeImpl(DispatcherPriority.Normal, method, args, -1);
        }

        /// <summary>
        ///     Executes the specified delegate asynchronously with the specified
        ///     arguments, on the thread that the Dispatcher was created on.
        /// </summary>
        /// <param name="method">
        ///     A delegate to a method that takes parameters of the same number
        ///     and type that are contained in the args parameter.
        /// </param>
        /// <param name="priority">
        ///     The priority that determines in what order the specified method
        ///     is invoked relative to the other pending methods in the Dispatcher.
        /// </param>
        /// <param name="args">
        ///     An array of objects to pass as arguments to the given method.
        ///     This can be null if no arguments are needed.
        /// </param>
        /// <returns>
        ///     An IAsyncResult object that represents the result of the
        ///     BeginInvoke operation.
        /// </returns>
        public DispatcherOperation BeginInvoke(Delegate method, DispatcherPriority priority, params object[] args)
        {
            return LegacyBeginInvokeImpl(priority, method, args, -1);
        }

        /// <summary>
        ///     Executes the specified Action synchronously on the thread that
        ///     the Dispatcher was created on.
        /// </summary>
        /// <param name="callback">
        ///     An Action delegate to invoke through the dispatcher.
        /// </param>
        /// <remarks>
        ///     Note that the default priority is DispatcherPriority.Send.
        /// </remarks>
        public void Invoke(Action callback)
        {
            Invoke(callback, DispatcherPriority.Send, CancellationToken.None, TimeSpan.FromMilliseconds(-1));
        }

        /// <summary>
        ///     Executes the specified Action synchronously on the thread that
        ///     the Dispatcher was created on.
        /// </summary>
        /// <param name="callback">
        ///     An Action delegate to invoke through the dispatcher.
        /// </param>
        /// <param name="priority">
        ///     The priority that determines in what order the specified
        ///     callback is invoked relative to the other pending operations
        ///     in the Dispatcher.
        /// </param>
        public void Invoke(Action callback, DispatcherPriority priority)
        {
            Invoke(callback, priority, CancellationToken.None, TimeSpan.FromMilliseconds(-1));
        }

        /// <summary>
        ///     Executes the specified Action synchronously on the thread that
        ///     the Dispatcher was created on.
        /// </summary>
        /// <param name="callback">
        ///     An Action delegate to invoke through the dispatcher.
        /// </param>
        /// <param name="priority">
        ///     The priority that determines in what order the specified
        ///     callback is invoked relative to the other pending operations
        ///     in the Dispatcher.
        /// </param>
        /// <param name="cancellationToken">
        ///     A cancellation token that can be used to cancel the operation.
        ///     If the operation has not started, it will be aborted when the
        ///     cancellation token is canceled.  If the operation has started,
        ///     the operation can cooperate with the cancellation request.
        /// </param>
        public void Invoke(Action callback, DispatcherPriority priority, CancellationToken cancellationToken)
        {
            Invoke(callback, priority, cancellationToken, TimeSpan.FromMilliseconds(-1));
        }

        /// <summary>
        ///     Executes the specified Action synchronously on the thread that
        ///     the Dispatcher was created on.
        /// </summary>
        /// <param name="callback">
        ///     An Action delegate to invoke through the dispatcher.
        /// </param>
        /// <param name="priority">
        ///     The priority that determines in what order the specified
        ///     callback is invoked relative to the other pending operations
        ///     in the Dispatcher.
        /// </param>
        /// <param name="cancellationToken">
        ///     A cancellation token that can be used to cancel the operation.
        ///     If the operation has not started, it will be aborted when the
        ///     cancellation token is canceled.  If the operation has started,
        ///     the operation can cooperate with the cancellation request.
        /// </param>
        /// <param name="timeout">
        ///     The minimum amount of time to wait for the operation to start.
        ///     Once the operation has started, it will complete before this method
        ///     returns.
        /// </param>
        public void Invoke(Action callback, DispatcherPriority priority, CancellationToken cancellationToken, TimeSpan timeout)
        {
            if(callback == null)
            {
                throw new ArgumentNullException("callback");
            }
            ValidatePriority(priority, "priority");

            if( timeout.TotalMilliseconds < 0 &&
                timeout != TimeSpan.FromMilliseconds(-1))
            {
                throw new ArgumentOutOfRangeException("timeout");
            }

            // Fast-Path: if on the same thread, and invoking at Send priority,
            // and the cancellation token is not already canceled, then just
            // call the callback directly.
            if(!cancellationToken.IsCancellationRequested && priority == DispatcherPriority.Send && CheckAccess())
            {
                SynchronizationContext oldSynchronizationContext = SynchronizationContext.Current;

                try
                {
                    DispatcherSynchronizationContext newSynchronizationContext;
                    if(BaseCompatibilityPreferences.GetReuseDispatcherSynchronizationContextInstance())
                    {
                        newSynchronizationContext = _defaultDispatcherSynchronizationContext;
                    }
                    else
                    {
                        if(BaseCompatibilityPreferences.GetFlowDispatcherSynchronizationContextPriority())
                        {
                            newSynchronizationContext = new DispatcherSynchronizationContext(this, priority);
                        }
                        else
                        {
                            newSynchronizationContext = new DispatcherSynchronizationContext(this, DispatcherPriority.Normal);
                        }
                    }
                    SynchronizationContext.SetSynchronizationContext(newSynchronizationContext);

                    callback();
                    return;
                }
                finally
                {
                    SynchronizationContext.SetSynchronizationContext(oldSynchronizationContext);
                }
            }

            // Slow-Path: go through the queue.
            DispatcherOperation operation = new DispatcherOperation(this, priority, callback);
            InvokeImpl(operation, cancellationToken, timeout);
        }

        /// <summary>
        ///     Executes the specified Func<TResult> synchronously on the
        ///     thread that the Dispatcher was created on.
        /// </summary>
        /// <param name="callback">
        ///     A Func<TResult> delegate to invoke through the dispatcher.
        /// </param>
        /// <returns>
        ///     The return value from the delegate being invoked.
        /// </returns>
        /// <remarks>
        ///     Note that the default priority is DispatcherPriority.Send.
        /// </remarks>
        public TResult Invoke<TResult>(Func<TResult> callback)
        {
            return Invoke(callback, DispatcherPriority.Send, CancellationToken.None, TimeSpan.FromMilliseconds(-1));
        }

        /// <summary>
        ///     Executes the specified Func<TResult> synchronously on the
        ///     thread that the Dispatcher was created on.
        /// </summary>
        /// <param name="callback">
        ///     A Func<TResult> delegate to invoke through the dispatcher.
        /// </param>
        /// <param name="priority">
        ///     The priority that determines in what order the specified
        ///     callback is invoked relative to the other pending operations
        ///     in the Dispatcher.
        /// </param>
        /// <returns>
        ///     The return value from the delegate being invoked.
        /// </returns>
        public TResult Invoke<TResult>(Func<TResult> callback, DispatcherPriority priority)
        {
            return Invoke(callback, priority, CancellationToken.None, TimeSpan.FromMilliseconds(-1));
        }

        /// <summary>
        ///     Executes the specified Func<TResult> synchronously on the
        ///     thread that the Dispatcher was created on.
        /// </summary>
        /// <param name="callback">
        ///     A Func<TResult> delegate to invoke through the dispatcher.
        /// </param>
        /// <param name="priority">
        ///     The priority that determines in what order the specified
        ///     callback is invoked relative to the other pending operations
        ///     in the Dispatcher.
        /// </param>
        /// <param name="cancellationToken">
        ///     A cancellation token that can be used to cancel the operation.
        ///     If the operation has not started, it will be aborted when the
        ///     cancellation token is canceled.  If the operation has started,
        ///     the operation can cooperate with the cancellation request.
        /// </param>
        /// <returns>
        ///     The return value from the delegate being invoked.
        /// </returns>
        public TResult Invoke<TResult>(Func<TResult> callback, DispatcherPriority priority, CancellationToken cancellationToken)
        {
            return Invoke(callback, priority, cancellationToken, TimeSpan.FromMilliseconds(-1));
        }

        /// <summary>
        ///     Executes the specified Func<TResult> synchronously on the
        ///     thread that the Dispatcher was created on.
        /// </summary>
        /// <param name="callback">
        ///     A Func<TResult> delegate to invoke through the dispatcher.
        /// </param>
        /// <param name="priority">
        ///     The priority that determines in what order the specified
        ///     callback is invoked relative to the other pending operations
        ///     in the Dispatcher.
        /// </param>
        /// <param name="cancellationToken">
        ///     A cancellation token that can be used to cancel the operation.
        ///     If the operation has not started, it will be aborted when the
        ///     cancellation token is canceled.  If the operation has started,
        ///     the operation can cooperate with the cancellation request.
        /// </param>
        /// <param name="timeout">
        ///     The minimum amount of time to wait for the operation to start.
        ///     Once the operation has started, it will complete before this method
        ///     returns.
        /// </param>
        /// <returns>
        ///     The return value from the delegate being invoked.
        /// </returns>
        public TResult Invoke<TResult>(Func<TResult> callback, DispatcherPriority priority, CancellationToken cancellationToken, TimeSpan timeout)
        {
            if(callback == null)
            {
                throw new ArgumentNullException("callback");
            }
            ValidatePriority(priority, "priority");

            if( timeout.TotalMilliseconds < 0 &&
                timeout != TimeSpan.FromMilliseconds(-1))
            {
                throw new ArgumentOutOfRangeException("timeout");
            }

            // Fast-Path: if on the same thread, and invoking at Send priority,
            // and the cancellation token is not already canceled, then just
            // call the callback directly.
            if(!cancellationToken.IsCancellationRequested && priority == DispatcherPriority.Send && CheckAccess())
            {
                SynchronizationContext oldSynchronizationContext = SynchronizationContext.Current;

                try
                {
                    DispatcherSynchronizationContext newSynchronizationContext;
                    if(BaseCompatibilityPreferences.GetReuseDispatcherSynchronizationContextInstance())
                    {
                        newSynchronizationContext = _defaultDispatcherSynchronizationContext;
                    }
                    else
                    {
                        if(BaseCompatibilityPreferences.GetFlowDispatcherSynchronizationContextPriority())
                        {
                            newSynchronizationContext = new DispatcherSynchronizationContext(this, priority);
                        }
                        else
                        {
                            newSynchronizationContext = new DispatcherSynchronizationContext(this, DispatcherPriority.Normal);
                        }
                    }
                    SynchronizationContext.SetSynchronizationContext(newSynchronizationContext);

                    return callback();
                }
                finally
                {
                    SynchronizationContext.SetSynchronizationContext(oldSynchronizationContext);
                }
            }

            // Slow-Path: go through the queue.
            DispatcherOperation<TResult> operation = new DispatcherOperation<TResult>(this, priority, callback);
            return (TResult) InvokeImpl(operation, cancellationToken, timeout);
        }

        /// <summary>
        ///     Executes the specified Action asynchronously on the thread
        ///     that the Dispatcher was created on.
        /// </summary>
        /// <param name="callback">
        ///     An Action delegate to invoke through the dispatcher.
        /// </param>
        /// <returns>
        ///     An operation representing the queued delegate to be invoked.
        /// </returns>
        /// <remarks>
        ///     Note that the default priority is DispatcherPriority.Normal.
        /// </remarks>
        public DispatcherOperation InvokeAsync(Action callback)
        {
            return InvokeAsync(callback, DispatcherPriority.Normal, CancellationToken.None);
        }

        /// <summary>
        ///     Executes the specified Action asynchronously on the thread
        ///     that the Dispatcher was created on.
        /// </summary>
        /// <param name="callback">
        ///     An Action delegate to invoke through the dispatcher.
        /// </param>
        /// <param name="priority">
        ///     The priority that determines in what order the specified
        ///     callback is invoked relative to the other pending operations
        ///     in the Dispatcher.
        /// </param>
        /// <returns>
        ///     An operation representing the queued delegate to be invoked.
        /// </returns>
        /// <returns>
        ///     An operation representing the queued delegate to be invoked.
        /// </returns>
        public DispatcherOperation InvokeAsync(Action callback, DispatcherPriority priority)
        {
            return InvokeAsync(callback, priority, CancellationToken.None);
        }

        /// <summary>
        ///     Executes the specified Action asynchronously on the thread
        ///     that the Dispatcher was created on.
        /// </summary>
        /// <param name="callback">
        ///     An Action delegate to invoke through the dispatcher.
        /// </param>
        /// <param name="priority">
        ///     The priority that determines in what order the specified
        ///     callback is invoked relative to the other pending operations
        ///     in the Dispatcher.
        /// </param>
        /// <param name="cancellationToken">
        ///     A cancellation token that can be used to cancel the operation.
        ///     If the operation has not started, it will be aborted when the
        ///     cancellation token is canceled.  If the operation has started,
        ///     the operation can cooperate with the cancellation request.
        /// </param>
        /// <returns>
        ///     An operation representing the queued delegate to be invoked.
        /// </returns>
        public DispatcherOperation InvokeAsync(Action callback, DispatcherPriority priority, CancellationToken cancellationToken)
        {
            if(callback == null)
            {
                throw new ArgumentNullException("callback");
            }
            ValidatePriority(priority, "priority");

            DispatcherOperation operation = new DispatcherOperation(this, priority, callback);
            InvokeAsyncImpl(operation, cancellationToken);

            return operation;
        }

        /// <summary>
        ///     Executes the specified Func<TResult> asynchronously on the
        ///     thread that the Dispatcher was created on.
        /// </summary>
        /// <param name="callback">
        ///     A Func<TResult> delegate to invoke through the dispatcher.
        /// </param>
        /// <returns>
        ///     An operation representing the queued delegate to be invoked.
        /// </returns>
        /// <remarks>
        ///     Note that the default priority is DispatcherPriority.Normal.
        /// </remarks>
        public DispatcherOperation<TResult> InvokeAsync<TResult>(Func<TResult> callback)
        {
            return InvokeAsync(callback, DispatcherPriority.Normal, CancellationToken.None);
        }

        /// <summary>
        ///     Executes the specified Func<TResult> asynchronously on the
        ///     thread that the Dispatcher was created on.
        /// </summary>
        /// <param name="callback">
        ///     A Func<TResult> delegate to invoke through the dispatcher.
        /// </param>
        /// <param name="priority">
        ///     The priority that determines in what order the specified
        ///     callback is invoked relative to the other pending operations
        ///     in the Dispatcher.
        /// </param>
        /// <returns>
        ///     An operation representing the queued delegate to be invoked.
        /// </returns>
        public DispatcherOperation<TResult> InvokeAsync<TResult>(Func<TResult> callback, DispatcherPriority priority)
        {
            return InvokeAsync(callback, priority, CancellationToken.None);
        }

        /// <summary>
        ///     Executes the specified Func<TResult> asynchronously on the
        ///     thread that the Dispatcher was created on.
        /// </summary>
        /// <param name="callback">
        ///     A Func<TResult> delegate to invoke through the dispatcher.
        /// </param>
        /// <param name="priority">
        ///     The priority that determines in what order the specified
        ///     callback is invoked relative to the other pending operations
        ///     in the Dispatcher.
        /// </param>
        /// <param name="cancellationToken">
        ///     A cancellation token that can be used to cancel the operation.
        ///     If the operation has not started, it will be aborted when the
        ///     cancellation token is canceled.  If the operation has started,
        ///     the operation can cooperate with the cancellation request.
        /// </param>
        /// <returns>
        ///     An operation representing the queued delegate to be invoked.
        /// </returns>
        public DispatcherOperation<TResult> InvokeAsync<TResult>(Func<TResult> callback, DispatcherPriority priority, CancellationToken cancellationToken)
        {
            if(callback == null)
            {
                throw new ArgumentNullException("callback");
            }
            ValidatePriority(priority, "priority");

            DispatcherOperation<TResult> operation = new DispatcherOperation<TResult>(this, priority, callback);
            InvokeAsyncImpl(operation, cancellationToken);

            return operation;
        }

        private DispatcherOperation LegacyBeginInvokeImpl(DispatcherPriority priority, Delegate method, object args, int numArgs)
        {
            ValidatePriority(priority, "priority");
            if(method == null)
            {
                throw new ArgumentNullException("method");
            }

            DispatcherOperation operation = new DispatcherOperation(this, method, priority, args, numArgs);
            InvokeAsyncImpl(operation, CancellationToken.None);

            return operation;
        }

        private void InvokeAsyncImpl(DispatcherOperation operation, CancellationToken cancellationToken)
        {
            DispatcherHooks hooks = null;
            bool succeeded = false;

            // Could be a non-dispatcher thread, lock to read
            lock(_instanceLock)
            {
                if (!cancellationToken.IsCancellationRequested &&
                    !_hasShutdownFinished &&
                    !Environment.HasShutdownStarted)
                {
                    // Add the operation to the work queue
                    operation._item = _queue.Enqueue(operation.Priority, operation);

                    // Make sure we will wake up to process this operation.
                    succeeded = RequestProcessing();

                    if (succeeded)
                    {
                        // Grab the hooks to use inside the lock; but we will
                        // call them below, outside of the lock.
                        hooks = _hooks;
                    }
                    else
                    {
                        // Dequeue the item since we failed to request
                        // processing for it.  Note we will mark it aborted
                        // below.
                        _queue.RemoveItem(operation._item);
                    }
                }
            }

            if (succeeded == true)
            {
                // We have enqueued the operation.  Register a callback
                // with the cancellation token to abort the operation
                // when cancellation is requested.
                if(cancellationToken.CanBeCanceled)
                {
                    CancellationTokenRegistration cancellationRegistration = cancellationToken.Register(s => ((DispatcherOperation)s).Abort(), operation);

                    // Revoke the cancellation when the operation is done.
                    operation.Aborted += (s,e) => cancellationRegistration.Dispose();
                    operation.Completed += (s,e) => cancellationRegistration.Dispose();
                }

                if(hooks != null)
                {
                    hooks.RaiseOperationPosted(this, operation);
                }

                if (EventTrace.IsEnabled(EventTrace.Keyword.KeywordDispatcher | EventTrace.Keyword.KeywordPerf, EventTrace.Level.Info))
                {
                    EventTrace.EventProvider.TraceEvent(EventTrace.Event.WClientUIContextPost, EventTrace.Keyword.KeywordDispatcher | EventTrace.Keyword.KeywordPerf, EventTrace.Level.Info, operation.Priority, operation.Name, operation.Id);
                }
            }
            else
            {
                // We failed to enqueue the operation, and the caller that
                // created the operation does not expose it before we return,
                // so it is safe to modify the operation outside of the lock.
                // Just mark the operation as aborted, which we can safely
                // return to the user.
                operation._status = DispatcherOperationStatus.Aborted;
                operation._taskSource.SetCanceled();
            }
        }

        /// <summary>
        ///     Executes the specified delegate synchronously on the thread that
        ///     the Dispatcher was created on.
        /// </summary>
        /// <param name="priority">
        ///     The priority that determines in what order the specified method
        ///     is invoked relative to the other pending methods in the Dispatcher.
        /// </param>
        /// <param name="method">
        ///     A delegate to a method that takes no parameters.
        /// </param>
        /// <returns>
        ///     The return value from the delegate being invoked, or null if
        ///     the delegate has no return value.
        /// </returns>
        [Browsable(false), EditorBrowsable(EditorBrowsableState.Never)]
        public object Invoke(DispatcherPriority priority, Delegate method)
        {
            return LegacyInvokeImpl(priority, TimeSpan.FromMilliseconds(-1), method, null, 0);
        }

        /// <summary>
        ///     Executes the specified delegate synchronously with the specified
        ///     arguments, on the thread that the Dispatcher was created on.
        /// </summary>
        /// <param name="priority">
        ///     The priority that determines in what order the specified method
        ///     is invoked relative to the other pending methods in the Dispatcher.
        /// </param>
        /// <param name="method">
        ///     A delegate to a method that takes parameters of the same number
        ///     and type that are contained in the args parameter.
        /// </param>
        /// <param name="arg">
        ///     An object to pass as an argument to the given method.
        ///     This can be null if no arguments are needed.
        /// </param>
        /// <returns>
        ///     The return value from the delegate being invoked, or null if
        ///     the delegate has no return value.
        /// </returns>
        [Browsable(false), EditorBrowsable(EditorBrowsableState.Never)]
        public object Invoke(DispatcherPriority priority, Delegate method, object arg)
        {
            return LegacyInvokeImpl(priority, TimeSpan.FromMilliseconds(-1), method, arg, 1);
        }

        /// <summary>
        ///     Executes the specified delegate synchronously with the specified
        ///     arguments, on the thread that the Dispatcher was created on.
        /// </summary>
        /// <param name="priority">
        ///     The priority that determines in what order the specified method
        ///     is invoked relative to the other pending methods in the Dispatcher.
        /// </param>
        /// <param name="method">
        ///     A delegate to a method that takes parameters of the same number
        ///     and type that are contained in the args parameter.
        /// </param>
        /// <param name="arg">
        /// </param>
        /// <param name="args">
        ///     An array of objects to pass as arguments to the given method.
        ///     This can be null if no arguments are needed.
        /// </param>
        /// <returns>
        ///     The return value from the delegate being invoked, or null if
        ///     the delegate has no return value.
        /// </returns>
        [Browsable(false), EditorBrowsable(EditorBrowsableState.Never)]
        public object Invoke(DispatcherPriority priority, Delegate method, object arg, params object[] args)
        {
            return LegacyInvokeImpl(priority, TimeSpan.FromMilliseconds(-1), method, CombineParameters(arg, args), -1);
        }

        /// <summary>
        ///     Executes the specified delegate synchronously on the thread that
        ///     the Dispatcher was created on.
        /// </summary>
        /// <param name="priority">
        ///     The priority that determines in what order the specified method
        ///     is invoked relative to the other pending methods in the Dispatcher.
        /// </param>
        /// <param name="timeout">
        ///     The minimum amount of time to wait for the operation to start.
        ///     Once the operation has started, it will complete before this method
        ///     returns.
        /// </param>
        /// <param name="method">
        ///     A delegate to a method that takes no parameters.
        /// </param>
        /// <returns>
        ///     The return value from the delegate being invoked, or null if
        ///     the delegate has no return value.
        /// </returns>
        [Browsable(false), EditorBrowsable(EditorBrowsableState.Never)]
        public object Invoke(DispatcherPriority priority, TimeSpan timeout, Delegate method)
        {
            return LegacyInvokeImpl(priority, timeout, method, null, 0);
        }

        /// <summary>
        ///     Executes the specified delegate synchronously with the specified
        ///     arguments, on the thread that the Dispatcher was created on.
        /// </summary>
        /// <param name="priority">
        ///     The priority that determines in what order the specified method
        ///     is invoked relative to the other pending methods in the Dispatcher.
        /// </param>
        /// <param name="timeout">
        ///     The minimum amount of time to wait for the operation to start.
        ///     Once the operation has started, it will complete before this method
        ///     returns.
        /// </param>
        /// <param name="method">
        ///     A delegate to a method that takes parameters of the same number
        ///     and type that are contained in the args parameter.
        /// </param>
        /// <param name="arg">
        ///     An object to pass as an argument to the given method.
        ///     This can be null if no arguments are needed.
        /// </param>
        /// <returns>
        ///     The return value from the delegate being invoked, or null if
        ///     the delegate has no return value.
        /// </returns>
        [Browsable(false), EditorBrowsable(EditorBrowsableState.Never)]
        public object Invoke(DispatcherPriority priority, TimeSpan timeout, Delegate method, object arg)
        {
            return LegacyInvokeImpl(priority, timeout, method, arg, 1);
        }

        /// <summary>
        ///     Executes the specified delegate synchronously with the specified
        ///     arguments, on the thread that the Dispatcher was created on.
        /// </summary>
        /// <param name="priority">
        ///     The priority that determines in what order the specified method
        ///     is invoked relative to the other pending methods in the Dispatcher.
        /// </param>
        /// <param name="timeout">
        ///     The minimum amount of time to wait for the operation to start.
        ///     Once the operation has started, it will complete before this method
        ///     returns.
        /// </param>
        /// <param name="method">
        ///     A delegate to a method that takes parameters of the same number
        ///     and type that are contained in the args parameter.
        /// </param>
        /// <param name="arg">
        /// </param>
        /// <param name="args">
        ///     An array of objects to pass as arguments to the given method.
        ///     This can be null if no arguments are needed.
        /// </param>
        /// <returns>
        ///     The return value from the delegate being invoked, or null if
        ///     the delegate has no return value.
        /// </returns>
        [Browsable(false), EditorBrowsable(EditorBrowsableState.Never)]
        public object Invoke(DispatcherPriority priority, TimeSpan timeout, Delegate method, object arg, params object[] args)
        {
            return LegacyInvokeImpl(priority, timeout, method, CombineParameters(arg, args), -1);
        }

        /// <summary>
        ///     Executes the specified delegate synchronously on the thread that
        ///     the Dispatcher was created on.
        /// </summary>
        /// <param name="method">
        ///     A delegate to a method that takes parameters of the same number
        ///     and type that are contained in the args parameter.
        /// </param>
        /// <param name="args">
        ///     An array of objects to pass as arguments to the given method.
        ///     This can be null if no arguments are needed.
        /// </param>
        /// <returns>
        ///     The return value from the delegate being invoked, or null if
        ///     the delegate has no return value.
        /// </returns>
        public object Invoke(Delegate method, params object[] args)
        {
            return LegacyInvokeImpl(DispatcherPriority.Normal, TimeSpan.FromMilliseconds(-1), method, args, -1);
        }

        /// <summary>
        ///     Executes the specified delegate synchronously on the thread that
        ///     the Dispatcher was created on.
        /// </summary>
        /// <param name="method">
        ///     A delegate to a method that takes parameters of the same number
        ///     and type that are contained in the args parameter.
        /// </param>
        /// <param name="priority">
        ///     The priority that determines in what order the specified method
        ///     is invoked relative to the other pending methods in the Dispatcher.
        /// </param>
        /// <param name="args">
        ///     An array of objects to pass as arguments to the given method.
        ///     This can be null if no arguments are needed.
        /// </param>
        /// <returns>
        ///     The return value from the delegate being invoked, or null if
        ///     the delegate has no return value.
        /// </returns>
        public object Invoke(Delegate method, DispatcherPriority priority, params object[] args)
        {
            return LegacyInvokeImpl(priority, TimeSpan.FromMilliseconds(-1), method, args, -1);
        }

        /// <summary>
        ///     Executes the specified delegate synchronously on the thread that
        ///     the Dispatcher was created on.
        /// </summary>
        /// <param name="method">
        ///     A delegate to a method that takes parameters of the same number
        ///     and type that are contained in the args parameter.
        /// </param>
        /// <param name="timeout">
        ///     The minimum amount of time to wait for the operation to start.
        ///     Once the operation has started, it will complete before this method
        ///     returns.
        /// </param>
        /// <param name="args">
        ///     An array of objects to pass as arguments to the given method.
        ///     This can be null if no arguments are needed.
        /// </param>
        /// <returns>
        ///     The return value from the delegate being invoked, or null if
        ///     the delegate has no return value.
        /// </returns>
        public object Invoke(Delegate method, TimeSpan timeout, params object[] args)
        {
            return LegacyInvokeImpl(DispatcherPriority.Normal, timeout, method, args, -1);
        }

        /// <summary>
        ///     Executes the specified delegate synchronously on the thread that
        ///     the Dispatcher was created on.
        /// </summary>
        /// <param name="method">
        ///     A delegate to a method that takes parameters of the same number
        ///     and type that are contained in the args parameter.
        /// </param>
        /// <param name="timeout">
        ///     The minimum amount of time to wait for the operation to start.
        ///     Once the operation has started, it will complete before this method
        ///     returns.
        /// </param>
        /// <param name="priority">
        ///     The priority that determines in what order the specified method
        ///     is invoked relative to the other pending methods in the Dispatcher.
        /// </param>
        /// <param name="args">
        ///     An array of objects to pass as arguments to the given method.
        ///     This can be null if no arguments are needed.
        /// </param>
        /// <returns>
        ///     The return value from the delegate being invoked, or null if
        ///     the delegate has no return value.
        /// </returns>
        public object Invoke(Delegate method, TimeSpan timeout, DispatcherPriority priority, params object[] args)
        {
            return LegacyInvokeImpl(priority, timeout, method, args, -1);
        }

        internal object LegacyInvokeImpl(DispatcherPriority priority, TimeSpan timeout, Delegate method, object args, int numArgs)
        {
            ValidatePriority(priority, "priority");
            if(priority == DispatcherPriority.Inactive)
            {
                throw new ArgumentException(SR.InvalidPriority, "priority");
            }

            if(method == null)
            {
                throw new ArgumentNullException("method");
            }

            if( timeout.TotalMilliseconds < 0 &&
                timeout != TimeSpan.FromMilliseconds(-1))
            {
                if(CheckAccess())
                {
                    // Application Compat
                    // In versions before 4.5, when invoking on the same
                    // thread, any negative timeout was effectively an
                    // infinite wait.  When invoking across threads, any
                    // negative timeout other than -1ms was an error which
                    // threw an exception internally when waiting on an event.
                    timeout = TimeSpan.FromMilliseconds(-1);
                }
                else
                {
                    throw new ArgumentOutOfRangeException("timeout");
                }
            }

            // Fast-Path: if on the same thread, and invoking at Send priority,
            // then just call the callback directly within the exception wrappers.
            if(priority == DispatcherPriority.Send && CheckAccess())
            {
                SynchronizationContext oldSynchronizationContext = SynchronizationContext.Current;

                try
                {
                    DispatcherSynchronizationContext newSynchronizationContext;
                    if(BaseCompatibilityPreferences.GetReuseDispatcherSynchronizationContextInstance())
                    {
                        newSynchronizationContext = _defaultDispatcherSynchronizationContext;
                    }
                    else
                    {
                        if(BaseCompatibilityPreferences.GetFlowDispatcherSynchronizationContextPriority())
                        {
                            newSynchronizationContext = new DispatcherSynchronizationContext(this, priority);
                        }
                        else
                        {
                            newSynchronizationContext = new DispatcherSynchronizationContext(this, DispatcherPriority.Normal);
                        }
                    }
                    SynchronizationContext.SetSynchronizationContext(newSynchronizationContext);

                    return WrappedInvoke(method, args, numArgs, null);
                }
                finally
                {
                    SynchronizationContext.SetSynchronizationContext(oldSynchronizationContext);
                }
            }

            // Slow-Path: go through the queue.
            DispatcherOperation operation = new DispatcherOperation(this, method, priority, args, numArgs);
            return InvokeImpl(operation, CancellationToken.None, timeout);
        }

        private object InvokeImpl(DispatcherOperation operation, CancellationToken cancellationToken, TimeSpan timeout)
        {
            object result = null;

            Debug.Assert(timeout.TotalMilliseconds >= 0 || timeout == TimeSpan.FromMilliseconds(-1));
            Debug.Assert(operation.Priority != DispatcherPriority.Send || !CheckAccess()); // should be handled by caller

            if(!cancellationToken.IsCancellationRequested)
            {
                // This operation must be queued since it was invoked either to
                // another thread, or at a priority other than Send.
                InvokeAsyncImpl(operation, cancellationToken);

                CancellationToken ctTimeout = CancellationToken.None;
                CancellationTokenRegistration ctTimeoutRegistration = new CancellationTokenRegistration();
                CancellationTokenSource ctsTimeout = null;

                if(timeout.TotalMilliseconds >= 0)
                {
                    // Create a CancellationTokenSource that will abort the
                    // operation after the timeout.  Note that this does not
                    // cancel the operation, just abort it if it is still pending.
                    ctsTimeout = new CancellationTokenSource(timeout);
                    ctTimeout = ctsTimeout.Token;
                    ctTimeoutRegistration = ctTimeout.Register(s => ((DispatcherOperation)s).Abort(), operation);
                }


                // We have already registered with the cancellation tokens
                // (both provided by the user, and one for the timeout) to
                // abort the operation when they are canceled.  If the
                // operation has already started when the timeout expires,
                // we still wait for it to complete.  This is different
                // than simply waiting on the operation with a timeout
                // because we are the ones queueing the dispatcher
                // operation, not the caller.  We can't leave the operation
                // in a state that it might execute if we return that it did not
                // invoke.
                try
                {
                    operation.Wait();

                    Debug.Assert(operation.Status == DispatcherOperationStatus.Completed ||
                                 operation.Status == DispatcherOperationStatus.Aborted);

                    // Old async semantics return from Wait without
                    // throwing an exception if the operation was aborted.
                    // There is no need to test the timout condition, since
                    // the old async semantics would just return the result,
                    // which would be null.

                    // This should not block because either the operation
                    // is using the old async sematics, or the operation
                    // completed successfully.
                    result = operation.Result;
                }
                catch(OperationCanceledException)
                {
                    Debug.Assert(operation.Status == DispatcherOperationStatus.Aborted);

                    // New async semantics will throw an exception if the
                    // operation was aborted.  Here we convert that
                    // exception into a timeout exception if the timeout
                    // has expired (admittedly a weak relationship
                    // assuming causality).
                    if (ctTimeout.IsCancellationRequested)
                    {
                        // The operation was canceled because of the
                        // timeout, throw a TimeoutException instead.
                        throw new TimeoutException();
                    }
                    else
                    {
                        // The operation was canceled from some other reason.
                        throw;
                    }
                }
                finally
                {
                    ctTimeoutRegistration.Dispose();
                    if (ctsTimeout != null)
                    {
                        ctsTimeout.Dispose();
                    }
                }
            }

            return result;
        }

        /// <summary>
        ///     Disable the event processing of the dispatcher.
        /// </summary>
        /// <remarks>
        ///     This is an advanced method intended to elliminate the chance of
        ///     unrelated reentrancy.  The effect of disabling processing is:
        ///     1) CLR locks will not pump messages internally.
        ///     2) No one is allowed to push a frame.
        ///     3) No message processing is permitted.
        /// </remarks>
        public DispatcherProcessingDisabled DisableProcessing()
        {
            VerifyAccess();

            // Turn off processing.
            _disableProcessingCount++;

            DispatcherProcessingDisabled dpd = new DispatcherProcessingDisabled();
            dpd._dispatcher = this;
            return dpd;
        }

/*
        /// <summary>
        ///     Reports the range of priorities that are considered
        ///     as foreground priorities.
        /// </summary>
        /// <remarks>
        ///     A foreground priority is processed before input.
        /// </remarks>
        public static PriorityRange ForegroundPriorityRange
        {
            get
            {
                return _foregroundPriorityRange;
            }
        }

        /// <summary>
        ///     Reports the range of priorities that are considered
        ///     as background priorities.
        /// </summary>
        /// <remarks>
        ///     A background priority is processed after input.
        /// </remarks>
        public static PriorityRange BackgroundPriorityRange
        {
            get
            {
                return _backgroundPriorityRange;
            }
        }

        /// <summary>
        ///     Reports the range of priorities that are considered
        ///     as idle priorities.
        /// </summary>
        /// <remarks>
        ///     An idle priority is processed periodically after background
        ///     priorities have been processed.
        /// </remarks>
        public static PriorityRange IdlePriorityRange
        {
            get
            {
                return _idlePriorityRange;
            }
        }

        /// <summary>
        ///     Represents a convenient foreground priority.
        /// </summary>
        /// <remarks>
        ///     A foreground priority is processed before input.  In general
        ///     you should define your own foreground priority to allow for
        ///     more fine-grained ordering of queued items.
        /// </remarks>
        public static Priority ForegroundPriority
        {
            get
            {
                return _foregroundPriority;
            }
        }

        /// <summary>
        ///     Represents a convenient background priority.
        /// </summary>
        /// <remarks>
        ///     A background priority is processed after input.  In general you
        ///     should define your own background priority to allow for more
        ///     fine-grained ordering of queued items.
        /// </remarks>
        public static Priority BackgroundPriority
        {
            get
            {
                return _backgroundPriority;
            }
        }

        /// <summary>
        ///     Represents a convenient idle priority.
        /// </summary>
        /// <remarks>
        ///     An idle priority is processed periodically after background
        ///     priorities have been processed.  In general you should define
        ///     your own idle priority to allow for more fine-grained ordering
        ///     of queued items.
        /// </remarks>
        public static Priority IdlePriority
        {
            get
            {
                return _idlePriority;
            }
        }
*/

        /// <summary>
        ///     Validates that a priority is suitable for use by the dispatcher.
        /// </summary>
        /// <param name="priority">
        ///     The priority to validate.
        /// </param>
        /// <param name="parameterName">
        ///     The name if the argument to report in the ArgumentException
        ///     that is raised if the priority is not suitable for use by
        ///     the dispatcher.
        /// </param>
        public static void ValidatePriority(DispatcherPriority priority, string parameterName) // NOTE: should be Priority
        {
            // First make sure the Priority is valid.
            // Priority.ValidatePriority(priority, paramName);

            // Second, make sure the priority is in a range recognized by
            // the dispatcher.
            if(!_foregroundPriorityRange.Contains(priority) &&
               !_backgroundPriorityRange.Contains(priority) &&
               !_idlePriorityRange.Contains(priority) &&
               DispatcherPriority.Inactive != priority)  // NOTE: should be Priority.Min
            {
                // If we move to a Priority class, this exception will have to change too.
                throw new System.ComponentModel.InvalidEnumArgumentException(parameterName, (int)priority, typeof(DispatcherPriority));
            }
        }

        /// <summary>
        ///     Checks that the calling thread has access to this object.
        /// </summary>
        /// <remarks>
        ///     Only the dispatcher thread may access DispatcherObjects.
        ///     <p/>
        ///     This method is public so that any thread can probe to
        ///     see if it has access to the DispatcherObject.
        /// </remarks>
        /// <returns>
        ///     True if the calling thread has access to this object.
        /// </returns>
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Advanced)]
        public DispatcherHooks Hooks
        {
            get
            {
                DispatcherHooks hooks = null;

                lock(_instanceLock)
                {
                    if(_hooks == null)
                    {
                        _hooks = new DispatcherHooks();
                    }

                    hooks = _hooks;
                }

                return hooks;
            }
        }

        /// <summary>
        ///     Occurs when an untrapped thread exception is thrown.
        /// </summary>
        /// <remarks>
        ///     Raised during the filter stage for an exception raised during
        ///     execution of a delegate via Invoke or BeginInvoke.
        ///     <P/>
        ///     The callstack is not unwound at this time (first-chance exception).
        ///     <P/>
        ///     Listeners to this event must be written with care to avoid
        ///     creating secondary exceptions and to catch any that occur.
        ///     It is recommended to avoid allocating memory or doing any
        ///     heavylifting if possible.
        /// </remarks>
        public event DispatcherUnhandledExceptionFilterEventHandler UnhandledExceptionFilter
        {
            add
            {
                _unhandledExceptionFilter += value;
            }
            remove
            {
                _unhandledExceptionFilter -= value;
            }
        }

        /// <summary>
        ///     Occurs when an untrapped thread exception is thrown.
        /// </summary>
        /// <remarks>
        ///     Raised when an exception was caught that was raised during
        ///     execution of a delegate via Invoke or BeginInvoke.
        ///     <P/>
        ///     A handler can mark the exception as handled which will prevent
        ///     the internal "final" exception handler from being called.
        ///     <P/>
        ///     Listeners to this event must be written with care to avoid
        ///     creating secondary exceptions and to catch any that occur.
        ///     It is recommended to avoid allocating memory or doing any
        ///     heavylifting if possible.
        /// </remarks>
        public event DispatcherUnhandledExceptionEventHandler UnhandledException;

        /// <summary>
        ///     Reserved Dispatcher member
        /// </summary>
        internal object Reserved0
        {
            [FriendAccessAllowed] // Built into Base, used by Core or Framework.
            get { return _reserved0; }

            [FriendAccessAllowed] // Built into Base, used by Core or Framework.
            set { _reserved0 = value; }
        }

        /// <summary>
        ///     Reserved Dispatcher member
        /// </summary>
        internal object Reserved1
        {
            [FriendAccessAllowed] // Built into Base, used by Core or Framework.
            get { return _reserved1; }

            [FriendAccessAllowed] // Built into Base, used by Core or Framework.
            set { _reserved1 = value; }
        }

        /// <summary>
        ///     Reserved Dispatcher member
        /// </summary>
        internal object Reserved2
        {
            [FriendAccessAllowed] // Built into Base, used by Core or Framework.
            get { return _reserved2; }

            [FriendAccessAllowed] // Built into Base, used by Core or Framework.
            set { _reserved2 = value; }
        }

        /// <summary>
        ///     Reserved Dispatcher member
        /// </summary>
        internal object Reserved3
        {
            [FriendAccessAllowed] // Built into Base, used by Core or Framework.
            get { return _reserved3; }

            [FriendAccessAllowed] // Built into Base, used by Core or Framework.
            set { _reserved3 = value; }
        }

        /// <summary>
        ///     Reserved Dispatcher member
        /// </summary>
        internal object Reserved4
        {
            [FriendAccessAllowed] // Built into Base, used by Core or Framework.
            get { return _reserved4; }

            [FriendAccessAllowed] // Built into Base, used by Core or Framework.
            set { _reserved4 = value; }
        }

        /// <summary>
        ///     Reserved Dispatcher member for PtsCache
        /// </summary>
        internal object PtsCache
        {
            // This gets multiplexed with the log for "request processing" failures.
            // See OnRequestProcessingFailure.
            [FriendAccessAllowed] // Built into Base, used by Core or Framework.
            get
            {
                if (!_hasRequestProcessingFailed)
                    return _reservedPtsCache;
                Tuple<Object, List<String>> tuple = _reservedPtsCache as Tuple<Object, List<String>>;
                if (tuple == null)
                    return _reservedPtsCache;
                else
                    return tuple.Item1;
            }

            [FriendAccessAllowed] // Built into Base, used by Core or Framework.
            set
            {
                if (!_hasRequestProcessingFailed)
                    _reservedPtsCache = value;
                else
                {
                    Tuple<Object, List<String>> tuple = _reservedPtsCache as Tuple<Object, List<String>>;
                    List<String> list = (tuple != null) ? tuple.Item2 : new List<String>();
                    _reservedPtsCache = new Tuple<Object, List<String>>(value, list);
                }
            }
        }

        internal object InputMethod
        {
            [FriendAccessAllowed] // Built into Base, used by Core or Framework.
            get { return _reservedInputMethod; }

            [FriendAccessAllowed] // Built into Base, used by Core or Framework.
            set { _reservedInputMethod = value; }
        }

        internal object InputManager
        {
            [FriendAccessAllowed] // Built into Base, used by Core or Framework.
            get { return _reservedInputManager; }

            [FriendAccessAllowed] // Built into Base, used by Core or Framework.
            set { _reservedInputManager = value; }
        }

        private Dispatcher()
        {
            _queue = new PriorityQueue<DispatcherOperation>();

            _tlsDispatcher = this; // use TLS for ownership only
            _dispatcherThread = Thread.CurrentThread;

            // Add ourselves to the map of dispatchers to threads.
            lock(_globalLock)
            {
                _dispatchers.Add(new WeakReference(this));
            }

            _unhandledExceptionEventArgs = new DispatcherUnhandledExceptionEventArgs(this);
            _exceptionFilterEventArgs = new DispatcherUnhandledExceptionFilterEventArgs(this);

            _defaultDispatcherSynchronizationContext = new DispatcherSynchronizationContext(this);

            // Create the message-only window we use to receive messages
            // that tell us to process the queue.
            MessageOnlyHwndWrapper window = new MessageOnlyHwndWrapper();
            _window = new SecurityCriticalData<MessageOnlyHwndWrapper>( window );

            _hook = new HwndWrapperHook(WndProcHook);
            _window.Value.AddHook(_hook);

            // Verify that the accessibility switches are set prior to any major UI code running.
            AccessibilitySwitches.VerifySwitches(this);
        }

        // creates a "sentinel" dispatcher.  It doesn't do anything, and should never
        // be called except for CheckAccess and VerifyAccess (which fail).
        // See DispatcherObject.MakeSentinel() for more.
        // [The 'isSentinel' parameter is ignored - it only serves to distinguish
        // this ctor from others.]
        internal Dispatcher(bool isSentinel)
        {
            Debug.Assert(isSentinel, "this ctor is only for creating a 'sentinel' dispatcher");

            // set thread so that CheckAccess and VerifyAccess fail
            _dispatcherThread = null;

            // set other members so that incoming calls (which shouldn't happen)
            // do as little as possible
            _startingShutdown = true;
            _hasShutdownStarted = true;
            _hasShutdownFinished = true;
        }

        private void StartShutdownImpl()
        {
            if(!_startingShutdown)
            {
                // We only need this to prevent reentrancy if the ShutdownStarted event
                // tries to shut down again.
                _startingShutdown = true;

                // Call the ShutdownStarted event before we actually mark ourselves
                // as shutting down.  This is so the handlers can actaully do work
                // when they get this event without throwing exceptions.
                if(ShutdownStarted != null)
                {
                    ShutdownStarted(this, EventArgs.Empty);
                }

                _hasShutdownStarted = true;

                // Because we may have to defer the actual shutting-down until
                // later, we need to remember the execution context we started
                // the shutdown from.
                CulturePreservingExecutionContext shutdownExecutionContext = CulturePreservingExecutionContext.Capture();
                _shutdownExecutionContext = new SecurityCriticalDataClass<CulturePreservingExecutionContext>(shutdownExecutionContext);

                // Tell Win32 to exit the message loop for this thread.
                //
                // This call to PostQuitMessage is commented out because PostQuitMessage
                // not only shuts down the message pump associated with the Dispatcher, but also
                // shuts down any process that might be hosting WPF content (like IE).
                // UnsafeNativeMethods.PostQuitMessage(0);
                if(_frameDepth > 0)
                {
                    // If there are any frames running, we have to wait for them
                    // to unwind before we can safely destroy the dispatcher.
                }
                else
                {
                    // The current thread is not spinning inside of the Dispatcher,
                    // so we can go ahead and destroy it.
                    ShutdownImpl();
                }
            }
        }

        private void ShutdownImpl()
        {
            if(!_hasShutdownFinished) // Dispatcher thread - no lock needed for read
            {
                if(_shutdownExecutionContext != null && _shutdownExecutionContext.Value != null)
                {
                    // Continue using the execution context that was active when the shutdown
                    // was initiated.
                    CulturePreservingExecutionContext.Run(_shutdownExecutionContext.Value, new ContextCallback(ShutdownImplInSecurityContext), null);
                }
                else
                {
                    // It is possible to be called from WM_DESTROY, in which case no one has begun
                    // the shutdown process, so there is no execution context to use.
                    ShutdownImplInSecurityContext(null);
                }

                _shutdownExecutionContext = null;
            }
        }

        private void ShutdownImplInSecurityContext(Object state)
        {
            // Call the ShutdownFinished event before we actually mark ourselves
            // as shut down.  This is so the handlers can actaully do work
            // when they get this event without throwing exceptions.
            if(ShutdownFinished != null)
            {
                ShutdownFinished(this, EventArgs.Empty);
            }

            // Destroy the message-only window we use to process Win32 messages
            //
            // Note: we need to do this BEFORE we actually mark the dispatcher
            // as shutdown.  This is because the window will need the dispatcher
            // to execute the window proc.
            MessageOnlyHwndWrapper window = null;
            lock(_instanceLock)
            {
                window = _window.Value;
                _window = new SecurityCriticalData<MessageOnlyHwndWrapper>(null);
            }
            window.Dispose();

            // Mark this dispatcher as shut down.  Attempts to BeginInvoke
            // or Invoke will result in an exception.
            lock(_instanceLock)
            {
                _hasShutdownFinished = true; // Dispatcher thread - lock to write
            }

            // Now that the queue is off-line, abort all pending operations,
            // including inactive ones.
            DispatcherOperation operation = null;
            do
            {
                lock(_instanceLock)
                {
                    if(_queue.MaxPriority != DispatcherPriority.Invalid)
                    {
                        operation = _queue.Peek();
                    }
                    else
                    {
                        operation = null;
                    }
                }

                if(operation != null)
                {
                    operation.Abort();
                }
            } while(operation != null);

            // clear out the fields that could be holding onto large graphs of objects.
            lock(_instanceLock)
            {
                // We should not need the queue any more.
                _queue = null;

                // We should not need the timers any more.
                _timers = null;

                // Clear out the reserved fields.
                _reserved0 = null;
                _reserved1 = null;
                _reserved2 = null;
                _reserved3 = null;
                _reserved4 = null;
                // _reservedPtsCache = null; // PTS needs this in a finalizer... the PTS code should not assume access to this in their finalizer.
                _reservedInputMethod = null;
                _reservedInputManager = null;
            }

            // Note: the Dispatcher is still held in TLS.  This maintains the 1-1 relationship
            // between the thread and the Dispatcher.  However the dispatcher is basically
            // dead - it has been marked as _hasShutdownFinished, and most operations are
            // now illegal.
        }

        // Returns whether or not the priority was set.
        internal bool SetPriority(DispatcherOperation operation, DispatcherPriority priority) // NOTE: should be Priority
        {
            bool notify = false;
            DispatcherHooks hooks = null;

            lock(_instanceLock)
            {
                if(_queue != null && operation._item.IsQueued)
                {
                    _queue.ChangeItemPriority(operation._item, priority);
                    notify = true;

                    if(notify)
                    {
                        // Make sure we will wake up to process this operation.
                        RequestProcessing();

                        hooks = _hooks;
                    }
                }
            }

            if (notify)
            {
                if(hooks != null)
                {
                    hooks.RaiseOperationPriorityChanged(this, operation);
                }

                if (EventTrace.IsEnabled(EventTrace.Keyword.KeywordDispatcher | EventTrace.Keyword.KeywordPerf, EventTrace.Level.Info))
                {
                    EventTrace.EventProvider.TraceEvent(EventTrace.Event.WClientUIContextPromote, EventTrace.Keyword.KeywordDispatcher | EventTrace.Keyword.KeywordPerf, EventTrace.Level.Info, priority, operation.Name, operation.Id);
                }
            }

            return notify;
        }

        // Returns whether or not the operation was removed.
        internal bool Abort(DispatcherOperation operation)
        {
            bool notify = false;
            DispatcherHooks hooks = null;

            lock(_instanceLock)
            {
                if(_queue != null && operation._item.IsQueued)
                {
                    _queue.RemoveItem(operation._item);
                    operation._status = DispatcherOperationStatus.Aborted;
                    notify = true;

                    hooks = _hooks;
                }
            }

            if (notify)
            {
                if(hooks != null)
                {
                    hooks.RaiseOperationAborted(this, operation);
                }

                if (EventTrace.IsEnabled(EventTrace.Keyword.KeywordDispatcher | EventTrace.Keyword.KeywordPerf, EventTrace.Level.Info))
                {
                    EventTrace.EventProvider.TraceEvent(EventTrace.Event.WClientUIContextAbort, EventTrace.Keyword.KeywordDispatcher | EventTrace.Keyword.KeywordPerf, EventTrace.Level.Info, operation.Priority, operation.Name, operation.Id);
                }
}

            return notify;
        }

        private void ProcessQueue()
        {
            DispatcherPriority maxPriority = DispatcherPriority.Invalid; // NOTE: should be Priority.Invalid
            DispatcherOperation op = null;
            DispatcherHooks hooks = null;

            //
            // Dequeue the next operation if appropriate.
            lock(_instanceLock)
            {
                _postedProcessingType = PROCESS_NONE;

                // We can only do background processing if there is
                // no input in the Win32 queue.
                bool backgroundProcessingOK = !IsInputPending();

                maxPriority = _queue.MaxPriority;

                if(maxPriority != DispatcherPriority.Invalid &&  // Nothing. NOTE: should be Priority.Invalid
                   maxPriority != DispatcherPriority.Inactive)   // Not processed. // NOTE: should be Priority.Min
                {
                    if(_foregroundPriorityRange.Contains(maxPriority) || backgroundProcessingOK)
                    {
                         op = _queue.Dequeue();
                         hooks = _hooks;
                    }
                }

                // Hm... we are grabbing this here... but it could change while we are invoking
                // the operation...  maybe we should move this code to after the invoke?
                maxPriority = _queue.MaxPriority;

                // If there is more to do, request processing for it.
                RequestProcessing();
            }

            if(op != null)
            {
                bool eventlogged = false;

                if (EventTrace.IsEnabled(EventTrace.Keyword.KeywordDispatcher | EventTrace.Keyword.KeywordPerf, EventTrace.Level.Info))
                {
                    EventTrace.EventProvider.TraceEvent(EventTrace.Event.WClientUIContextDispatchBegin, EventTrace.Keyword.KeywordDispatcher | EventTrace.Keyword.KeywordPerf, EventTrace.Level.Info, op.Priority, op.Name, op.Id);
                    eventlogged = true;
                }

                if(hooks != null)
                {
                    hooks.RaiseOperationStarted(this, op);
                }

                op.Invoke();

                if(hooks != null)
                {
                    hooks.RaiseOperationCompleted(this, op);
                }

                if (eventlogged)
                {
                    EventTrace.EventProvider.TraceEvent(EventTrace.Event.WClientUIContextDispatchEnd, EventTrace.Keyword.KeywordDispatcher | EventTrace.Keyword.KeywordPerf, EventTrace.Level.Info);

                    if (_idlePriorityRange.Contains(maxPriority))
                    {
                        EventTrace.EventProvider.TraceEvent(EventTrace.Event.WClientUIContextIdle, EventTrace.Keyword.KeywordDispatcher | EventTrace.Keyword.KeywordPerf, EventTrace.Level.Info);
                    }
                }

                // All done, ready for reentrancy in case the completions are inlined.
                op.InvokeCompletions();
}
        }

        internal delegate void ShutdownCallback();

        private void ShutdownCallbackInternal()
        {
            StartShutdownImpl();
        }

        private void PushFrameImpl(DispatcherFrame frame)
        {
            SynchronizationContext oldSyncContext = null;
            SynchronizationContext newSyncContext = null;
            MSG msg = new MSG();

            _frameDepth++;
            try
            {
                // Change the CLR SynchronizationContext to be compatable with our Dispatcher.
                oldSyncContext = SynchronizationContext.Current;
                newSyncContext = new DispatcherSynchronizationContext(this);
                SynchronizationContext.SetSynchronizationContext(newSyncContext);

                try
                {
                    while(frame.Continue)
                    {
                        if (!GetMessage(ref msg, IntPtr.Zero, 0, 0))
                            break;

                        TranslateAndDispatchMessage(ref msg);
                    }

                    // If this was the last frame to exit after a quit, we
                    // can now dispose the dispatcher.
                    if(_frameDepth == 1)
                    {
                        if(_hasShutdownStarted)
                        {
                            ShutdownImpl();
                        }
                    }
                }
                finally
                {
                    // Restore the old SynchronizationContext.
                    SynchronizationContext.SetSynchronizationContext(oldSyncContext);
                }
            }
            finally
            {
                _frameDepth--;
                if(_frameDepth == 0)
                {
                    // We have exited all frames.
                    _exitAllFrames = false;
                }
            }
        }


        private bool GetMessage(ref MSG msg, IntPtr hwnd, int minMessage, int maxMessage)
        {
            // If Any TextServices for Cicero is not installed GetMessagePump() returns null.
            // If TextServices are there, we can get ITfMessagePump and have to use it instead of
            // Win32 GetMessage().
            bool result;
            UnsafeNativeMethods.ITfMessagePump messagePump = GetMessagePump();
            try
            {
                if (messagePump == null)
                {
                    // We have foreground items to process.
                    // By posting a message, Win32 will service us fairly promptly.
                    result = UnsafeNativeMethods.GetMessageW(ref msg,
                                                             new HandleRef(this, hwnd),
                                                             minMessage,
                                                             maxMessage);
                }
                else
                {
                    int intResult;

                    messagePump.GetMessageW(
                        ref msg,
                        hwnd,
                        minMessage,
                        maxMessage,
                        out intResult);

                    if (intResult == -1)
                    {
                        throw new Win32Exception();
                    }
                    else if (intResult == 0)
                    {
                        result = false;
                    }
                    else
                    {
                        result = true;
                    }
                }
            }
            finally
            {
                if (messagePump != null) Marshal.ReleaseComObject(messagePump);
            }

            return result;
        }

        //  Get ITfMessagePump interface from Cicero.
        private UnsafeNativeMethods.ITfMessagePump GetMessagePump()
        {
            UnsafeNativeMethods.ITfMessagePump messagePump = null;

            if (_isTSFMessagePumpEnabled)
            {
                // If the current thread is not STA, Cicero just does not work.
                // Probably this Dispatcher is running for worker thread.
                if (Thread.CurrentThread.GetApartmentState() == ApartmentState.STA)
                {
                    // If there is no text services, we don't have to use ITfMessagePump.
                    if (TextServicesLoader.ServicesInstalled)
                    {
                        UnsafeNativeMethods.ITfThreadMgr threadManager;
                        threadManager = TextServicesLoader.Load();

                        // ThreadManager does not exist. No MessagePump yet.
                        if (threadManager != null)
                        {
                            // QI ITfMessagePump.
                            messagePump = threadManager as UnsafeNativeMethods.ITfMessagePump;
                        }
                    }
                }
            }

            return messagePump;
        }

        /// <summary>
        /// Enables/disables ITfMessagePump handshake with Text Services Framework.
        /// </summary>
        /// <remarks>
        /// PresentationCore's TextServicesManager sets this property false when
        /// no WPF element has focus.  This is important to ensure that native
        /// controls receive unfiltered input.
        /// </remarks>
        [FriendAccessAllowed] // Used by TextServicesManager in PresentationCore.
        internal bool IsTSFMessagePumpEnabled
        {
            set
            {
                _isTSFMessagePumpEnabled = value;
            }
        }

        private void TranslateAndDispatchMessage(ref MSG msg)
        {
            bool handled = false;

            handled = ComponentDispatcher.RaiseThreadMessage(ref msg);

            if(!handled)
            {
                UnsafeNativeMethods.TranslateMessage(ref msg);
                UnsafeNativeMethods.DispatchMessage(ref msg);
            }
        }

        private IntPtr WndProcHook(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            WindowMessage message = (WindowMessage)msg;
            if(_disableProcessingCount > 0)
            {
                throw new InvalidOperationException(SR.DispatcherProcessingDisabledButStillPumping);
            }

            if(message == WindowMessage.WM_DESTROY)
            {
                if(!_hasShutdownStarted && !_hasShutdownFinished) // Dispatcher thread - no lock needed for read
                {
                    // Aack!  We are being torn down rudely!  Try to
                    // shut the dispatcher down as nicely as we can.
                    ShutdownImpl();
                }
            }
            else if(message == _msgProcessQueue)
            {
                ProcessQueue();
            }
            else if(message == WindowMessage.WM_TIMER && (int) wParam == TIMERID_BACKGROUND)
            {
                // This timer is just used to process background operations.
                // Stop the timer so that it doesn't fire again.
                SafeNativeMethods.KillTimer(new HandleRef(this, hwnd), TIMERID_BACKGROUND);

                ProcessQueue();
            }
            else if(message == WindowMessage.WM_TIMER && (int) wParam == TIMERID_TIMERS)
            {
                // We want 1-shot only timers.  So stop the timer
                // that just fired.
                KillWin32Timer();

                PromoteTimers(Environment.TickCount);
            }

            // We are about to return to the OS.  If there is nothing left
            // to do in the queue, then we will effectively go to sleep.
            // This is the condition that means Idle.
            DispatcherHooks hooks = null;
            bool idle = false;

            lock(_instanceLock)
            {
                idle = (_postedProcessingType < PROCESS_BACKGROUND);
                if (idle)
                {
                    hooks = _hooks;
                }
            }

            if (idle)
            {
                if(hooks != null)
                {
                    hooks.RaiseDispatcherInactive(this);
                }

                ComponentDispatcher.RaiseIdle();
            }

            return IntPtr.Zero ;
        }

        private bool IsInputPending()
        {
            int retVal = 0;

            // We need to know if there is any pending input in the Win32
            // queue because we want to only process Avalon "background"
            // items after Win32 input has been processed.
            //
            // Win32 provides the GetQueueStatus API -- but it has a major
            // drawback: it only counts "new" input.  This means that
            // sometimes it could return false, even if there really is input
            // that needs to be processed.  This results in very hard to
            // find bugs.
            //
            // Luckily, Win32 also provides the MsgWaitForMultipleObjectsEx
            // API.  While more awkward to use, this API can return queue
            // status information even if the input is "old".  The various
            // flags we use are:
            //
            // QS_INPUT
            // This represents any pending input - such as mouse moves, or
            // key presses.  It also includes the new GenericInput messages.
            //
            // QS_EVENT
            // This is actually a private flag that represents the various
            // events that can be queued in Win32.  Some of these events
            // can cause input, but Win32 doesn't include them in the
            // QS_INPUT flag.  An example is WM_MOUSELEAVE.
            //
            // QS_POSTMESSAGE
            // If there is already a message in the queue, we need to process
            // it before we can process input.
            //
            // MWMO_INPUTAVAILABLE
            // This flag indicates that any input (new or old) is to be
            // reported.
            //
            retVal = UnsafeNativeMethods.MsgWaitForMultipleObjectsEx(0, null, 0,
                                                                     NativeMethods.QS_INPUT |
                                                                     NativeMethods.QS_EVENT |
                                                                     NativeMethods.QS_POSTMESSAGE,
                                                                     NativeMethods.MWMO_INPUTAVAILABLE);

            return retVal == 0;
        }


        private bool RequestProcessing()
        {
            return CriticalRequestProcessing(false);
        }

        internal bool CriticalRequestProcessing(bool force)
        {
            bool succeeded = true;

            // This method is called from within the instance lock.  So we
            // can reliably check the _window field without worrying about
            // it being changed out from underneath us during shutdown.
            if (IsWindowNull())
                return false;

            DispatcherPriority priority = _queue.MaxPriority;

            if (priority != DispatcherPriority.Invalid &&
                priority != DispatcherPriority.Inactive)
            {
                // If forcing the processing request, we will discard any
                // existing request (timer or message) and request again.
                if (force)
                {
                    if (_postedProcessingType == PROCESS_BACKGROUND)
                    {
                        SafeNativeMethods.KillTimer(new HandleRef(this, _window.Value.Handle), TIMERID_BACKGROUND);
                    }
                    else if (_postedProcessingType == PROCESS_FOREGROUND)
                    {
                        // Preserve the thread's current "extra message info"
                        // (PeekMessage overwrites it).
                        IntPtr extraInformation = UnsafeNativeMethods.GetMessageExtraInfo();

                        MSG msg = new MSG();
                        UnsafeNativeMethods.PeekMessage(ref msg, new HandleRef(this, _window.Value.Handle), _msgProcessQueue, _msgProcessQueue, NativeMethods.PM_REMOVE);

                        UnsafeNativeMethods.SetMessageExtraInfo(extraInformation);
                    }
                    _postedProcessingType = PROCESS_NONE;
                }

                if (_foregroundPriorityRange.Contains(priority))
                {
                    succeeded = RequestForegroundProcessing();
                }
                else
                {
                    succeeded = RequestBackgroundProcessing();
                }
            }

            return succeeded;
        }

        private bool IsWindowNull()
        {
           if(_window.Value == null)
            {
                return true;
            }
            return false;
        }

        private bool RequestForegroundProcessing()
        {
            if(_postedProcessingType < PROCESS_FOREGROUND)
            {
                // If we have already set a timer to do background processing,
                // make sure we stop it before posting a message for foreground
                // processing.
                if(_postedProcessingType == PROCESS_BACKGROUND)
                {
                    SafeNativeMethods.KillTimer(new HandleRef(this, _window.Value.Handle), TIMERID_BACKGROUND);
                }

                _postedProcessingType = PROCESS_FOREGROUND;

                // We have foreground items to process.
                // By posting a message, Win32 will service us fairly promptly.
                bool succeeded = UnsafeNativeMethods.TryPostMessage(new HandleRef(this, _window.Value.Handle), _msgProcessQueue, IntPtr.Zero, IntPtr.Zero);
                if (!succeeded)
                {
                    OnRequestProcessingFailure("TryPostMessage");
                }
                return succeeded;
            }

            return true;
        }

        private bool RequestBackgroundProcessing()
        {
            bool succeeded = true;

            if(_postedProcessingType < PROCESS_BACKGROUND)
            {
                // If there is Win32 input pending, we can't do any background
                // processing until it is done.  We use a short timer to
                // get processing time after the input.
                if(IsInputPending())
                {
                    _postedProcessingType = PROCESS_BACKGROUND;

                    succeeded = SafeNativeMethods.TrySetTimer(new HandleRef(this, _window.Value.Handle), TIMERID_BACKGROUND, DELTA_BACKGROUND);
                    if (!succeeded)
                    {
                        OnRequestProcessingFailure("TrySetTimer");
                    }
                }
                else
                {
                    succeeded = RequestForegroundProcessing();
                }
            }

            return succeeded;
        }

        // Request{Foreground|Background}Processing can encounter failures from an
        // underlying OS method.  We cannot recover from these failures - they are
        // typically due to the application flooding the message queue, or starving
        // the dispatcher's message pump until the queue floods, and thus outside
        // the control of WPF.  WPF ignores the failures, but that can leave the
        // dispatcher in a non-responsive state, waiting for a message that will
        // never arrive.  It's difficult or impossible to determine whether
        // this has happened from a crash dump.
        //
        // To help this diagnosis, we now record the fact that the failure has
        // happened in the member
        //      _hasRequestProcessingFailed
        // When this bool is true, you can delve into
        //      _reservedPtsCache
        // to find a list of strings with some more rudimentary diagnostic information.
        // Unfortunately, we cannot tell why the message queue is full or who has
        // filled it - that knowledge is only available to the kernel.
        private void OnRequestProcessingFailure(string methodName)
        {
            if (!_hasRequestProcessingFailed)
            {
                // initialize the failure log, multiplexed under _reservedPtsCache.
                // (this member was chosen after instrumented tests revealed it
                // usually has the least activity of any of the existing fields)
                _reservedPtsCache = new Tuple<Object, List<String>>(_reservedPtsCache, new List<String>());
                _hasRequestProcessingFailed = true;
            }

            // add a new entry to the failure log
            Tuple<Object, List<String>> tuple = _reservedPtsCache as Tuple<Object, List<String>>;
            if (tuple != null)
            {
                List<String> list = tuple.Item2;
                list.Add(String.Format(System.Globalization.CultureInfo.InvariantCulture,
                    "{0:O} {1} failed", DateTime.Now, methodName));

                // keep the list from growing too large
                // (although usually it will have only one entry)
                if (list.Count > 1000)
                {
                    // keep the earliest and latest failures
                    list.RemoveRange(100, list.Count-200);
                    // acknowledge the gap
                    list.Insert(100, "... entries removed to conserve memory ...");
                }
            }

            // handle the failure, according to app's preference
            switch (BaseCompatibilityPreferences.HandleDispatcherRequestProcessingFailure)
            {
                case BaseCompatibilityPreferences.HandleDispatcherRequestProcessingFailureOptions.Continue:
                    break;
                case BaseCompatibilityPreferences.HandleDispatcherRequestProcessingFailureOptions.Throw:
                    throw new InvalidOperationException(SR.DispatcherRequestProcessingFailed);
                case BaseCompatibilityPreferences.HandleDispatcherRequestProcessingFailureOptions.Reset:
                    _postedProcessingType = PROCESS_NONE;
                    break;
            }
        }

        internal void PromoteTimers(int currentTimeInTicks)
        {
            try
            {
                List<DispatcherTimer> timers = null;
                long timersVersion = 0;

                lock(_instanceLock)
                {
                    if(!_hasShutdownFinished) // Could be a non-dispatcher thread, lock to read
                    {
                        if(_dueTimeFound && _dueTimeInTicks - currentTimeInTicks <= 0)
                        {
                            timers = _timers;
                            timersVersion = _timersVersion;
                        }
                    }
                }

                if(timers != null)
                {
                    DispatcherTimer timer = null;
                    int iTimer = 0;

                    do
                    {
                        lock(_instanceLock)
                        {
                            timer = null;

                            // If the timers collection changed while we are in the middle of
                            // looking for timers, start over.
                            if(timersVersion != _timersVersion)
                            {
                                timersVersion = _timersVersion;
                                iTimer = 0;
                            }

                            while(iTimer < _timers.Count)
                            {
                                // WARNING: this is vulnerable to wrapping
                                if(timers[iTimer]._dueTimeInTicks - currentTimeInTicks <= 0)
                                {
                                    // Remove this timer from our list.
                                    // Do not increment the index.
                                    timer = timers[iTimer];
                                    timers.RemoveAt(iTimer);
                                    break;
                                }
                                else
                                {
                                    iTimer++;
                                }
                            }
                        }

                        // Now that we are outside of the lock, promote the timer.
                        if(timer != null)
                        {
                            timer.Promote();
                        }
                    } while(timer != null);
}
            }
            finally
            {
                UpdateWin32Timer();
            }
        }

        internal void AddTimer(DispatcherTimer timer)
        {
            lock(_instanceLock)
            {
                if(!_hasShutdownFinished) // Could be a non-dispatcher thread, lock to read
                {
                    _timers.Add(timer);
                    _timersVersion++;
                }
            }
            UpdateWin32Timer();
        }

        internal void RemoveTimer(DispatcherTimer timer)
        {
            lock(_instanceLock)
            {
                if(!_hasShutdownFinished) // Could be a non-dispatcher thread, lock to read
                {
                    _timers.Remove(timer);
                    _timersVersion++;
                }
            }
            UpdateWin32Timer();
        }

        internal void UpdateWin32Timer() // Called from DispatcherTimer
        {
            if(CheckAccess())
            {
                UpdateWin32TimerFromDispatcherThread(null);
            }
            else
            {
                BeginInvoke(DispatcherPriority.Send,
                            new DispatcherOperationCallback(UpdateWin32TimerFromDispatcherThread),
                            null);
            }
        }

        private object UpdateWin32TimerFromDispatcherThread(object unused)
        {
            lock(_instanceLock)
            {
                if(!_hasShutdownFinished) // Dispatcher thread, does not technically need the lock to read
                {
                    bool oldDueTimeFound = _dueTimeFound;
                    int oldDueTimeInTicks = _dueTimeInTicks;
                    _dueTimeFound = false;
                    _dueTimeInTicks = 0;

                    if(_timers.Count > 0)
                    {
                        // We could do better if we sorted the list of timers.
                        for(int i = 0; i < _timers.Count; i++)
                        {
                            DispatcherTimer timer = _timers[i];

                            if(!_dueTimeFound || timer._dueTimeInTicks - _dueTimeInTicks < 0)
                            {
                                _dueTimeFound = true;
                                _dueTimeInTicks = timer._dueTimeInTicks;
                            }
                        }
                    }

                    if(_dueTimeFound)
                    {
                        if(!_isWin32TimerSet || !oldDueTimeFound || (oldDueTimeInTicks != _dueTimeInTicks))
                        {
                            SetWin32Timer(_dueTimeInTicks);
                        }
                    }
                    else if(oldDueTimeFound)
                    {
                        KillWin32Timer();
                    }
                }
            }

            return null;
        }

        private void SetWin32Timer(int dueTimeInTicks)
        {
            if(!IsWindowNull())
            {
                int delta = dueTimeInTicks - Environment.TickCount;
                if(delta < 1)
                {
                    delta = 1;
                }

                // We are being called on the dispatcher thread so we can rely on
                // _window.Value being non-null without taking the instance lock.

                SafeNativeMethods.SetTimer(
                    new HandleRef(this, _window.Value.Handle),
                    TIMERID_TIMERS,
                    delta);

                _isWin32TimerSet = true;
            }
        }

        private void KillWin32Timer()
        {
            if(!IsWindowNull())
            {
                // We are being called on the dispatcher thread so we can rely on
                // _window.Value being non-null without taking the instance lock.

                SafeNativeMethods.KillTimer(
                    new HandleRef(this, _window.Value.Handle),
                    TIMERID_TIMERS);

                _isWin32TimerSet = false;
            }
        }

        // Exception filter returns true if exception should be caught.
        private static bool ExceptionFilterStatic(object source, Exception e)
        {
            Dispatcher d = (Dispatcher)source;
            return d.ExceptionFilter(e);
        }

        private bool ExceptionFilter(Exception e)
        {
            // see whether this dispatcher has already seen the exception.
            // This can happen when the dispatcher is re-entered via
            // PushFrame (or similar).
            if (!e.Data.Contains(ExceptionDataKey))
            {
                // first time we've seen this exception - add data to the exception
                e.Data.Add(ExceptionDataKey, null);
            }
            else
            {
                // we've seen this exception before - don't catch it
                return false;
            }

            // By default, Request catch if there's anyone signed up to catch it;
            bool requestCatch = HasUnhandledExceptionHandler;

            // The app can hook up an ExceptionFilter to avoid catching it.
            // ExceptionFilter will run REGARDLESS of whether there are exception handlers.
            if (_unhandledExceptionFilter != null)
            {
                // The default requestCatch value that is passed in the args
                // should be returned unchanged if filters don't set them explicitly.
                _exceptionFilterEventArgs.Initialize(e, requestCatch);
                bool bSuccess = false;
                try
                {
                    _unhandledExceptionFilter(this, _exceptionFilterEventArgs);
                    bSuccess = true;
                }
                finally
                {
                    if (bSuccess)
                    {
                        requestCatch = _exceptionFilterEventArgs.RequestCatch;
                    }

                    // For bSuccess is false,
                    // To be in line with default behavior of structured exception handling,
                    // we would want to set requestCatch to false, however, this means one
                    // poorly programmed filter will knock out all dispatcher exception handling.
                    // If an exception filter fails, we run with whatever value is set thus far.
                }
            }

            return requestCatch;
        }

        // This returns false when caller should rethrow the exception.
        // true means Exception is "handled" and things just continue on.
        private static bool CatchExceptionStatic(object source, Exception e)
        {
            Dispatcher dispatcher = (Dispatcher)source;
            return dispatcher.CatchException(e);
        }

        // The exception filter called for catching an unhandled exception.
        private bool CatchException(Exception e)
        {
            bool handled = false;

            if (UnhandledException != null)
            {
                _unhandledExceptionEventArgs.Initialize(e, false);

                bool bSuccess = false;
                try
                {
                    UnhandledException(this, _unhandledExceptionEventArgs);
                    handled = _unhandledExceptionEventArgs.Handled;
                    bSuccess = true;
                }
                finally
                {
                    if (!bSuccess)
                        handled = false;
                }
}

            return(handled);
        }

        // This is called by DRT (via reflection) to see if there is a UnhandledException handler.
        private bool HasUnhandledExceptionHandler
        {
            get { return (UnhandledException != null); }
        }

        [FriendAccessAllowed] //also used by ResourceReferenceExpression in PresentationFramework
        internal object WrappedInvoke(Delegate callback, object args, int numArgs, Delegate catchHandler)
        {
            return _exceptionWrapper.TryCatchWhen(this, callback, args, numArgs, catchHandler);
        }

        private object[] CombineParameters(object arg, object[] args)
        {
            object[] parameters = new object[1 + (args == null ? 1 : args.Length)];
            parameters[0] = arg;
            if (args != null)
            {
                Array.Copy(args, 0, parameters, 1, args.Length);
            }
            else
            {
                parameters[1] = null;
            }

            return parameters;
        }

        private const int PROCESS_NONE = 0;
        private const int PROCESS_BACKGROUND = 1;
        private const int PROCESS_FOREGROUND = 2;

        private const int TIMERID_BACKGROUND = 1;
        private const int TIMERID_TIMERS = 2;
        private const int DELTA_BACKGROUND = 1;

        private static List<WeakReference> _dispatchers;
        private static WeakReference _possibleDispatcher;
        private static readonly object _globalLock;

        [ThreadStatic]
        private static Dispatcher _tlsDispatcher;      // use TLS for ownership only

        private Thread _dispatcherThread;

        private int _frameDepth;
        internal bool _exitAllFrames;       // used from DispatcherFrame
        private bool _startingShutdown;
        internal bool _hasShutdownStarted;  // used from DispatcherFrame
        private SecurityCriticalDataClass<CulturePreservingExecutionContext> _shutdownExecutionContext;

        internal int _disableProcessingCount; // read by DispatcherSynchronizationContext, decremented by DispatcherProcessingDisabled

        //private static Priority _foregroundBackgroundBorderPriority = new Priority(Priority.Min, Priority.Max, "Dispatcher.ForegroundBackgroundBorder");
        //private static Priority _backgroundIdleBorderPriority = new Priority(Priority.Min, _foregroundBackgroundBorderPriority, "Dispatcher.BackgroundIdleBorder");

        //private static Priority _foregroundPriority = new Priority(_foregroundBackgroundBorderPriority, Priority.Max, "Dispatcher.Foreground");
        //private static Priority _backgroundPriority = new Priority(_backgroundIdleBorderPriority, _foregroundBackgroundBorderPriority, "Dispatcher.Background");
        //private static Priority _idlePriority = new Priority(Priority.Min, _backgroundIdleBorderPriority, "Dispatcher.Idle");

        //private static PriorityRange _foregroundPriorityRange = new PriorityRange(_foregroundBackgroundBorderPriority, false, Priority.Max, true);
        //private static PriorityRange _backgroundPriorityRange = new PriorityRange(_backgroundIdleBorderPriority, false, _foregroundBackgroundBorderPriority, false);
        //private static PriorityRange _idlePriorityRange = new PriorityRange(Priority.Min, false, _backgroundIdleBorderPriority, false);

        private static PriorityRange _foregroundPriorityRange = new PriorityRange(DispatcherPriority.Loaded, true, DispatcherPriority.Send, true);
        private static PriorityRange _backgroundPriorityRange = new PriorityRange(DispatcherPriority.Background, true, DispatcherPriority.Input, true);
        private static PriorityRange _idlePriorityRange = new PriorityRange(DispatcherPriority.SystemIdle, true, DispatcherPriority.ContextIdle, true);

        private SecurityCriticalData<MessageOnlyHwndWrapper> _window;

        private HwndWrapperHook _hook;

        private int _postedProcessingType;
        private static WindowMessage _msgProcessQueue;

        private static ExceptionWrapper _exceptionWrapper;
        private static readonly object ExceptionDataKey = new object();

        // Preallocated arguments for exception handling.
        // This helps avoid allocations in the handler code, a potential
        // source of secondary exceptions (i.e. in Out-Of-Memory cases).
        private DispatcherUnhandledExceptionEventArgs _unhandledExceptionEventArgs;

        private DispatcherUnhandledExceptionFilterEventHandler _unhandledExceptionFilter;
        private DispatcherUnhandledExceptionFilterEventArgs _exceptionFilterEventArgs;

        private object _reserved0;
        private object _reserved1;
        private object _reserved2;
        private object _reserved3;
        private object _reserved4;
        private object _reservedPtsCache;
        private object _reservedInputMethod;
        private object _reservedInputManager;

        internal DispatcherSynchronizationContext _defaultDispatcherSynchronizationContext;

        internal object _instanceLock = new object(); // Also used by DispatcherOperation
        private PriorityQueue<DispatcherOperation> _queue;
        private List<DispatcherTimer> _timers = new List<DispatcherTimer>();
        private long _timersVersion;
        private bool _dueTimeFound;
        private int _dueTimeInTicks;
        private bool _isWin32TimerSet;

        // This can be read from any thread, but only written by the dispatcher thread.
        // Dispatcher Thread - lock _instanceLock only on write
        // Non-Dispatcher Threads - lock _instanceLock on read
        private bool _hasShutdownFinished;

        // Enables/disables ITfMessagePump handshake with Text Services Framework.
        private bool _isTSFMessagePumpEnabled;

        // For diagnosing situations where dispatcher stops responding due to failure
        // of TryPostMessage or TrySetTimer.  If this is true (in a crash dump, say)
        // delve into _reservedPtsCache to find more about the failure(s).
        private bool _hasRequestProcessingFailed;

        private DispatcherHooks _hooks;
    }
}

