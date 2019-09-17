// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading;
using System.Diagnostics;
using System.ComponentModel;
using System.Security;                       // CAS
using System.Runtime.ConstrainedExecution;
using System.Windows;                        // BaseCompatibilityPreferences

namespace System.Windows.Threading
{
    /// <summary>
    ///     SynchronizationContext subclass used by the Dispatcher.
    /// </summary>
    public sealed class DispatcherSynchronizationContext : SynchronizationContext
    {
        /// <summary>
        ///     Constructs a new instance of the DispatcherSynchroniazationContext
        ///     using the current Dispatcher and normal post priority.
        /// </summary>
        public DispatcherSynchronizationContext(): this(Dispatcher.CurrentDispatcher, DispatcherPriority.Normal)
        {
        }

        /// <summary>
        ///     Constructs a new instance of the DispatcherSynchroniazationContext
        ///     using the specified Dispatcher and normal post priority.
        /// </summary>
        public DispatcherSynchronizationContext(Dispatcher dispatcher): this(dispatcher, DispatcherPriority.Normal)
        {
        }

        /// <summary>
        ///     Constructs a new instance of the DispatcherSynchroniazationContext
        ///     using the specified Dispatcher and the specified post priority.
        /// </summary>
        public DispatcherSynchronizationContext(Dispatcher dispatcher, DispatcherPriority priority)
        {
            if(dispatcher == null)
            {
                throw new ArgumentNullException("dispatcher");
            }
            
            Dispatcher.ValidatePriority(priority, "priority");
            
            _dispatcher = dispatcher;
            _priority = priority;

            // Tell the CLR to call us when blocking.
            SetWaitNotificationRequired();        
        }
        

        /// <summary>
        ///     Synchronously invoke the callback in the SynchronizationContext.
        /// </summary>
        public override void Send(SendOrPostCallback d, Object state)
        {
            // Call the Invoke overload that preserves the behavior of passing
            // exceptions to Dispatcher.UnhandledException.  
            if(BaseCompatibilityPreferences.GetInlineDispatcherSynchronizationContextSend() && _dispatcher.CheckAccess())
            {
                // Same-thread, use send priority to avoid any reentrancy.
                _dispatcher.Invoke(DispatcherPriority.Send, d, state);
            }
            else
            {
                // Cross-thread, use the cached priority.
                _dispatcher.Invoke(_priority, d, state);
            }
        }

        /// <summary>
        ///     Asynchronously invoke the callback in the SynchronizationContext.
        /// </summary>
        public override void Post(SendOrPostCallback d, Object state)
        {
            // Call BeginInvoke with the cached priority.  Note that BeginInvoke
            // preserves the behavior of passing exceptions to
            // Dispatcher.UnhandledException unlike InvokeAsync.  This is
            // desireable because there is no way to await the call to Post, so
            // exceptions are hard to observe.
            _dispatcher.BeginInvoke(_priority, d, state);
        }

        /// <summary>
        ///     Wait for a set of handles.
        /// </summary>
        [PrePrepareMethod]
        public override int Wait(IntPtr[] waitHandles, bool waitAll, int millisecondsTimeout)
        {
            if(_dispatcher._disableProcessingCount > 0)
            {
                // Call into native code directly in order to avoid the default
                // CLR locking behavior which pumps messages under contention.
                // Even though they try to pump only the COM messages, any
                // messages that have been SENT to the window are also
                // dispatched.  This can lead to unpredictable reentrancy.
                return MS.Win32.UnsafeNativeMethods.WaitForMultipleObjectsEx(waitHandles.Length, waitHandles, waitAll, millisecondsTimeout, false);
            }
            else
            {
                return SynchronizationContext.WaitHelper(waitHandles, waitAll, millisecondsTimeout);
            }
        }

        /// <summary>
        ///     Create a copy of this SynchronizationContext.
        /// </summary>
        public override SynchronizationContext CreateCopy()
        {
            DispatcherSynchronizationContext copy;
            
            if(BaseCompatibilityPreferences.GetReuseDispatcherSynchronizationContextInstance())
            {
                copy = this;
            }
            else
            {
                if(BaseCompatibilityPreferences.GetFlowDispatcherSynchronizationContextPriority())
                {
                    copy = new DispatcherSynchronizationContext(_dispatcher, _priority);
                }
                else
                {
                    copy = new DispatcherSynchronizationContext(_dispatcher, DispatcherPriority.Normal);
                }
            }

            return copy;
        }


        internal Dispatcher _dispatcher;
        private DispatcherPriority _priority;
    }
}

