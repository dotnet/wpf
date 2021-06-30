// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Security;

namespace System.Windows.Threading
{
    /// <summary>
    ///     Additional information provided about a dispatcher.
    /// </summary>
    public sealed class DispatcherHooks
    {
        /// <summary>
        ///     An event indicating the the dispatcher has no more operations to process.
        /// </summary>
        /// <remarks>
        ///     Note that this event will be raised by the dispatcher thread when
        ///     there is no more pending work to do.
        ///     <P/>
        ///     Note also that this event could come before the last operation is
        ///     invoked, because that is when we determine that the queue is empty.
        /// </remarks>
        public event EventHandler DispatcherInactive
        {
            add
            {
                lock(_instanceLock)
                {
                    _dispatcherInactive += value;
                }
            }
            remove
            {
                lock(_instanceLock)
                {
                    _dispatcherInactive -= value;
                }
            }
        }
        
        /// <summary>
        ///     An event indicating that an operation was posted to the dispatcher.
        /// </summary>
        /// <remarks>
        ///     Typically this is due to the BeginInvoke API, but the Invoke API can
        ///     also cause this if any priority other than DispatcherPriority.Send is
        ///     specified, or if the destination dispatcher is owned by a different
        ///     thread.
        ///     <P/>
        ///     Note that any thread can post operations, so this event can be
        ///     raised by any thread.
        /// </remarks>
        public event DispatcherHookEventHandler OperationPosted
        {
            add
            {
                lock(_instanceLock)
                {
                    _operationPosted += value;
                }
            }
            remove
            {
                lock(_instanceLock)
                {
                    _operationPosted -= value;
                }
            }
        }

        /// <summary>
        ///     An event indicating that an operation is about to be invoked.
        /// </summary>
        /// <remarks>
        ///     Typically this is due to the BeginInvoke API, but the Invoke API can
        ///     also cause this if any priority other than DispatcherPriority.Send is
        ///     specified, or if the destination dispatcher is owned by a different
        ///     thread.
        ///     <P/>
        ///     Note that any thread can post operations, so this event can be
        ///     raised by any thread.
        /// </remarks>
        public event DispatcherHookEventHandler OperationStarted
        {
            add
            {
                lock(_instanceLock)
                {
                    _operationStarted += value;
                }
            }
            remove
            {
                lock(_instanceLock)
                {
                    _operationStarted -= value;
                }
            }
        }

        /// <summary>
        ///     An event indicating that an operation was completed.
        /// </summary>
        /// <remarks>
        ///     Note that with the new async model, operations can cooperate in
        ///     cancelation, and any exceptions are contained.  This event is
        ///     raised in all cases.  You need to check the status of both the
        ///     operation and the associated task to infer the final state.
        ///
        ///     Note that this event will be raised by the dispatcher thread after
        ///     the operation has completed.
        /// </remarks>
        public event DispatcherHookEventHandler OperationCompleted
        {
            add
            {
                lock(_instanceLock)
                {
                    _operationCompleted += value;
                }
            }
            remove
            {
                lock(_instanceLock)
                {
                    _operationCompleted -= value;
                }
            }
        }
        
        /// <summary>
        ///     An event indicating that the priority of an operation was changed.
        /// </summary>
        /// <remarks>
        ///     Note that any thread can change the priority of operations,
        ///     so this event can be raised by any thread.
        /// </remarks>
        public event DispatcherHookEventHandler OperationPriorityChanged
        {
            add
            {
                lock(_instanceLock)
                {
                    _operationPriorityChanged += value;
                }
            }
            remove
            {
                lock(_instanceLock)
                {
                    _operationPriorityChanged -= value;
                }
            }
        }
        
        /// <summary>
        ///     An event indicating that an operation was aborted.
        /// </summary>
        /// <remarks>
        ///     Note that any thread can abort an operation, so this event
        ///     can be raised by any thread.
        /// </remarks>
        public event DispatcherHookEventHandler OperationAborted
        {
            add
            {
                lock(_instanceLock)
                {
                    _operationAborted += value;
                }
            }
            remove
            {
                lock(_instanceLock)
                {
                    _operationAborted -= value;
                }
            }
        }
        
        // Only we can create these things.
        internal DispatcherHooks()
        {
        }
        
        internal void RaiseDispatcherInactive(Dispatcher dispatcher)
        {
            EventHandler dispatcherInactive = _dispatcherInactive;
            if(dispatcherInactive != null)
            {
                dispatcherInactive(dispatcher, EventArgs.Empty);
            }
        }

        internal void RaiseOperationPosted(Dispatcher dispatcher, DispatcherOperation operation)
        {
            DispatcherHookEventHandler operationPosted = _operationPosted;
            
            if(operationPosted != null)
            {
                operationPosted(dispatcher, new DispatcherHookEventArgs(operation));
            }
        }

        internal void RaiseOperationStarted(Dispatcher dispatcher, DispatcherOperation operation)
        {
            DispatcherHookEventHandler operationStarted = _operationStarted;
            
            if(operationStarted != null)
            {
                operationStarted(dispatcher, new DispatcherHookEventArgs(operation));
            }
        }
        
        internal void RaiseOperationCompleted(Dispatcher dispatcher, DispatcherOperation operation)
        {
            DispatcherHookEventHandler operationCompleted = _operationCompleted;

            if(operationCompleted != null)
            {
                operationCompleted(dispatcher, new DispatcherHookEventArgs(operation));
            }
        }
        
        internal void RaiseOperationPriorityChanged(Dispatcher dispatcher, DispatcherOperation operation)
        {
            DispatcherHookEventHandler operationPriorityChanged = _operationPriorityChanged;

            if(operationPriorityChanged != null)
            {
                operationPriorityChanged(dispatcher, new DispatcherHookEventArgs(operation));
            }
        }

        internal void RaiseOperationAborted(Dispatcher dispatcher, DispatcherOperation operation)
        {
            DispatcherHookEventHandler operationAborted = _operationAborted;

            if(operationAborted != null)
            {
                operationAborted(dispatcher, new DispatcherHookEventArgs(operation));
            }
        }

        private readonly object _instanceLock = new object();

        private EventHandler _dispatcherInactive;

        private DispatcherHookEventHandler _operationPosted;

        private DispatcherHookEventHandler _operationStarted;

        private DispatcherHookEventHandler _operationCompleted;

        private DispatcherHookEventHandler _operationPriorityChanged;

        private DispatcherHookEventHandler _operationAborted;
    }
}
