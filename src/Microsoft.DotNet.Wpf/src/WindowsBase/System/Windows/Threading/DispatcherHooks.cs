// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Security;
using System.Security.Permissions;

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
        ///     Callers must have UIPermission(PermissionState.Unrestricted) to call this API.
        /// </remarks>
        /// <SecurityNote>
        ///     Critical: accesses _dispatcherInactive
        ///     TreatAsSafe: link-demands
        /// </SecurityNote>
        public event EventHandler DispatcherInactive
        {
            [SecurityCritical]
            [UIPermissionAttribute(SecurityAction.LinkDemand,Unrestricted=true)]                                    
            add
            {
                lock(_instanceLock)
                {
                    _dispatcherInactive += value;
                }
            }
            [SecurityCritical]
            [UIPermissionAttribute(SecurityAction.LinkDemand,Unrestricted=true)]                                    
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
        ///     Callers must have UIPermission(PermissionState.Unrestricted) to call this API.
        /// </remarks>
        /// <SecurityNote>
        ///     Critical: accesses _operationPosted
        ///     TreatAsSafe: link-demands
        /// </SecurityNote>
        public event DispatcherHookEventHandler OperationPosted
        {
            [SecurityCritical]
            [UIPermissionAttribute(SecurityAction.LinkDemand,Unrestricted=true)]                                    
            add
            {
                lock(_instanceLock)
                {
                    _operationPosted += value;
                }
            }
            [SecurityCritical]
            [UIPermissionAttribute(SecurityAction.LinkDemand,Unrestricted=true)]                                    
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
        ///     Callers must have UIPermission(PermissionState.Unrestricted) to call this API.
        /// </remarks>
        /// <SecurityNote>
        ///     Critical: accesses _operationPosted
        ///     TreatAsSafe: link-demands
        /// </SecurityNote>
        public event DispatcherHookEventHandler OperationStarted
        {
            [SecurityCritical]
            [UIPermissionAttribute(SecurityAction.LinkDemand,Unrestricted=true)]                                    
            add
            {
                lock(_instanceLock)
                {
                    _operationStarted += value;
                }
            }
            [SecurityCritical]
            [UIPermissionAttribute(SecurityAction.LinkDemand,Unrestricted=true)]                                    
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
        ///     Callers must have UIPermission(PermissionState.Unrestricted) to call this API.
        /// </remarks>
        /// <SecurityNote>
        ///     Critical: accesses _operationCompleted
        ///     TreatAsSafe: link-demands
        /// </SecurityNote>
        public event DispatcherHookEventHandler OperationCompleted
        {
            [SecurityCritical]
            [UIPermissionAttribute(SecurityAction.LinkDemand,Unrestricted=true)]                                    
            add
            {
                lock(_instanceLock)
                {
                    _operationCompleted += value;
                }
            }
            [SecurityCritical]
            [UIPermissionAttribute(SecurityAction.LinkDemand,Unrestricted=true)]                                    
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
        ///     Callers must have UIPermission(PermissionState.Unrestricted) to call this API.
        /// </remarks>
        /// <SecurityNote>
        ///     Critical: accesses _operationPriorityChanged
        ///     TreatAsSafe: link-demands
        /// </SecurityNote>
        public event DispatcherHookEventHandler OperationPriorityChanged
        {
            [SecurityCritical]
            [UIPermissionAttribute(SecurityAction.LinkDemand,Unrestricted=true)]                                    
            add
            {
                lock(_instanceLock)
                {
                    _operationPriorityChanged += value;
                }
            }
            [SecurityCritical]
            [UIPermissionAttribute(SecurityAction.LinkDemand,Unrestricted=true)]                                    
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
        ///     Callers must have UIPermission(PermissionState.Unrestricted) to call this API.
        /// </remarks>
        /// <SecurityNote>
        ///     Critical: accesses _operationAborted
        ///     TreatAsSafe: link-demands
        /// </SecurityNote>
        public event DispatcherHookEventHandler OperationAborted
        {
            [SecurityCritical]
            [UIPermissionAttribute(SecurityAction.LinkDemand,Unrestricted=true)]                                    
            add
            {
                lock(_instanceLock)
                {
                    _operationAborted += value;
                }
            }
            [SecurityCritical]
            [UIPermissionAttribute(SecurityAction.LinkDemand,Unrestricted=true)]                                    
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
        
        /// <SecurityNote>
        ///     Critical: accesses _operationAborted
        ///     TreatAsSafe: no exposure
        /// </SecurityNote>
        [SecurityCritical]
        internal void RaiseDispatcherInactive(Dispatcher dispatcher)
        {
            EventHandler dispatcherInactive = _dispatcherInactive;
            if(dispatcherInactive != null)
            {
                dispatcherInactive(dispatcher, EventArgs.Empty);
            }
        }

        /// <SecurityNote>
        ///     Critical: accesses _operationPosted
        ///     TreatAsSafe: no exposure
        /// </SecurityNote>
        [SecurityCritical]
        internal void RaiseOperationPosted(Dispatcher dispatcher, DispatcherOperation operation)
        {
            DispatcherHookEventHandler operationPosted = _operationPosted;
            
            if(operationPosted != null)
            {
                operationPosted(dispatcher, new DispatcherHookEventArgs(operation));
            }
        }

        /// <SecurityNote>
        ///     Critical: accesses _operationStarted
        ///     TreatAsSafe: no exposure
        /// </SecurityNote>
        [SecurityCritical]
        internal void RaiseOperationStarted(Dispatcher dispatcher, DispatcherOperation operation)
        {
            DispatcherHookEventHandler operationStarted = _operationStarted;
            
            if(operationStarted != null)
            {
                operationStarted(dispatcher, new DispatcherHookEventArgs(operation));
            }
        }
        
        /// <SecurityNote>
        ///     Critical: accesses _operationCompleted
        ///     TreatAsSafe: no exposure
        /// </SecurityNote>
        [SecurityCritical]
        internal void RaiseOperationCompleted(Dispatcher dispatcher, DispatcherOperation operation)
        {
            DispatcherHookEventHandler operationCompleted = _operationCompleted;

            if(operationCompleted != null)
            {
                operationCompleted(dispatcher, new DispatcherHookEventArgs(operation));
            }
        }
        
        /// <SecurityNote>
        ///     Critical: accesses _operationPriorityChanged
        ///     TreatAsSafe: no exposure
        /// </SecurityNote>
        [SecurityCritical]
        internal void RaiseOperationPriorityChanged(Dispatcher dispatcher, DispatcherOperation operation)
        {
            DispatcherHookEventHandler operationPriorityChanged = _operationPriorityChanged;

            if(operationPriorityChanged != null)
            {
                operationPriorityChanged(dispatcher, new DispatcherHookEventArgs(operation));
            }
        }

        /// <SecurityNote>
        ///     Critical: accesses _operationAborted
        ///     TreatAsSafe: no exposure
        /// </SecurityNote>
        [SecurityCritical]
        internal void RaiseOperationAborted(Dispatcher dispatcher, DispatcherOperation operation)
        {
            DispatcherHookEventHandler operationAborted = _operationAborted;

            if(operationAborted != null)
            {
                operationAborted(dispatcher, new DispatcherHookEventArgs(operation));
            }
        }

        private object _instanceLock = new object();

        /// <SecurityNote>
        ///     Do not expose to partially trusted code.
        /// </SecurityNote>
        [SecurityCritical]
        private EventHandler _dispatcherInactive;

        /// <SecurityNote>
        ///     Do not expose to partially trusted code.
        /// </SecurityNote>
        [SecurityCritical]
        private DispatcherHookEventHandler _operationPosted;

        /// <SecurityNote>
        ///     Do not expose to partially trusted code.
        /// </SecurityNote>
        [SecurityCritical]
        private DispatcherHookEventHandler _operationStarted;

        /// <SecurityNote>
        ///     Do not expose to partially trusted code.
        /// </SecurityNote>
        [SecurityCritical]
        private DispatcherHookEventHandler _operationCompleted;

        /// <SecurityNote>
        ///     Do not expose to partially trusted code.
        /// </SecurityNote>
        [SecurityCritical]
        private DispatcherHookEventHandler _operationPriorityChanged;

        /// <SecurityNote>
        ///     Do not expose to partially trusted code.
        /// </SecurityNote>
        [SecurityCritical]
        private DispatcherHookEventHandler _operationAborted;
    }
}
