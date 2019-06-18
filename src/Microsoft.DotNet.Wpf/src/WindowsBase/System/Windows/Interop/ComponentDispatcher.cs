// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


using System;
using System.Threading;
using System.Diagnostics.CodeAnalysis;
using System.Security;
using System.Security.Permissions;
using MS.Internal;
using MS.Win32;
using MS.Internal.WindowsBase;

namespace System.Windows.Interop
{
    /// <summary>
    ///     This is the delegate used for registering with the
    ///     ThreadFilterMessage and ThreadPreprocessMessage Events.
    ///</summary>
    [SuppressMessage("Microsoft.Design", "CA1045:DoNotPassTypesByReference")]
    public delegate void ThreadMessageEventHandler(ref MSG msg, ref bool handled);

    /// <summary>
    /// This is a static class used to share control of the message pump.
    /// Whomever is pumping (i.e. calling GetMessage()) will also send
    /// the messages to RaiseThreadKeyMessage() which will dispatch them to
    /// the ThreadFilterMessage and then (if not handled) to the ThreadPreprocessMessage
    /// delegates.  That way everyone can be included in the message loop.
    /// Currently only Keyboard messages are supported.
    /// There are also Events for Idle and facilities for Thread-Modal operation.
    ///</summary>
    public static class ComponentDispatcher
    {
        static ComponentDispatcher()
        {
            _threadSlot = Thread.AllocateDataSlot();
        }

        private static ComponentDispatcherThread CurrentThreadData
        {
            get
            {
                ComponentDispatcherThread data;
                object obj = Thread.GetData(_threadSlot);
                if(null == obj)
                {
                    data = new ComponentDispatcherThread();
                    Thread.SetData(_threadSlot, data);
                }
                else
                {
                    data = (ComponentDispatcherThread) obj;
                }
                return data;
            }
        }

        // Properties

        /// <summary>
        /// Returns true if one or more components has gone modal.
        /// Although once one component is modal a 2nd shouldn't.
        ///</summary>
        /// <remarks>
        ///     Callers must have UIPermission(PermissionState.Unrestricted) to call this API.
        /// </remarks>
        /// <SecurityNote>
        ///     Critical: This is blocked off as defense in depth
        ///     PublicOk: There is a demand here
        /// </SecurityNote>
        public static bool IsThreadModal
        {
            get
            {
                SecurityHelper.DemandUnrestrictedUIPermission();
                ComponentDispatcherThread data = ComponentDispatcher.CurrentThreadData;
                return data.IsThreadModal;
            }
        }

        /// <summary>
        /// Returns "current" message.   More exactly the last MSG Raised.
        ///</summary>
        /// <remarks>
        ///     Callers must have UIPermission(PermissionState.Unrestricted) to call this API.
        /// </remarks>
        /// <SecurityNote>
        ///     Critical: This is blocked off as defense in depth
        ///     PublicOk: There is a demand here
        /// </SecurityNote>
        public static MSG CurrentKeyboardMessage
        {
            get
            {
                SecurityHelper.DemandUnrestrictedUIPermission();
                return ComponentDispatcher.CurrentThreadData.CurrentKeyboardMessage;
            }
        }

        /// <summary>
        /// Returns "current" message.   More exactly the last MSG Raised.
        ///</summary>
        /// <SecurityNote>
        ///     Critical: This is blocked off as defense in depth
        /// </SecurityNote>
        internal static MSG UnsecureCurrentKeyboardMessage
        {
            [FriendAccessAllowed] // Built into Base, used by Core or Framework.
            get
            {
                return ComponentDispatcher.CurrentThreadData.CurrentKeyboardMessage;
            }

            [FriendAccessAllowed] // Built into Base, used by Core or Framework.
            set
            {
                ComponentDispatcher.CurrentThreadData.CurrentKeyboardMessage = value;
            }
        }

        // Methods

        /// <summary>
        /// A component calls this to go modal.  Current thread wide only.
        ///</summary>
        /// <remarks>
        ///     Callers must have UIPermission(PermissionState.Unrestricted) to call this API.
        /// </remarks>
        /// <SecurityNote>
        ///     Critical: This is blocked off as defense in depth
        ///     PublicOk: There is a demand here
        /// </SecurityNote>
        public static void PushModal()
        {
            SecurityHelper.DemandUnrestrictedUIPermission();
            CriticalPushModal();
        }

        /// <summary>
        /// A component calls this to go modal.  Current thread wide only.
        ///</summary>
        /// <SecurityNote>
        ///     Critical: This bypasses the demand for unrestricted UIPermission.
        /// </SecurityNote>
        internal static void CriticalPushModal()
        {
            ComponentDispatcherThread data = ComponentDispatcher.CurrentThreadData;
            data.PushModal();
        }

        /// <summary>
        /// A component calls this to end being modal.
        ///</summary>
        /// <remarks>
        ///     Callers must have UIPermission(PermissionState.Unrestricted) to call this API.
        /// </remarks>
        /// <SecurityNote>
        ///     Critical: This is blocked off as defense in depth
        ///     PublicOk: There is a demand here
        /// </SecurityNote>
        public static void PopModal()
        {
            SecurityHelper.DemandUnrestrictedUIPermission();
            CriticalPopModal();
        }

        /// <summary>
        /// A component calls this to end being modal.
        ///</summary>
        /// <SecurityNote>
        ///     Critical: This bypasses the demand for unrestricted UIPermission.
        /// </SecurityNote>
        internal static void CriticalPopModal()
        {
            ComponentDispatcherThread data = ComponentDispatcher.CurrentThreadData;
            data.PopModal();
        }

        /// <summary>
        /// The message loop pumper calls this when it is time to do idle processing.
        ///</summary>
        /// <remarks>
        ///     Callers must have UIPermission(PermissionState.Unrestricted) to call this API.
        /// </remarks>
        /// <SecurityNote>
        ///     Critical: This is blocked off as defense in depth
        ///     PublicOk: There is a demand here
        /// </SecurityNote>
        [SuppressMessage("Microsoft.Design", "CA1030:UseEventsWhereAppropriate")]
        public static void RaiseIdle()
        {
            ComponentDispatcherThread data = ComponentDispatcher.CurrentThreadData;
            data.RaiseIdle();
        }

        /// <summary>
        /// The message loop pumper calls this for every keyboard message.
        /// </summary>
        /// <remarks>
        ///     Callers must have UIPermission(PermissionState.Unrestricted) to call this API.
        /// </remarks>
        /// <SecurityNote>
        ///     Critical: This is blocked off as defense in depth
        ///     PublicOk: There is a demand here
        /// </SecurityNote>
        [SuppressMessage("Microsoft.Design", "CA1030:UseEventsWhereAppropriate")]
        [SuppressMessage("Microsoft.Design", "CA1045:DoNotPassTypesByReference")]
        public static bool RaiseThreadMessage(ref MSG msg)
        {
            ComponentDispatcherThread data = ComponentDispatcher.CurrentThreadData;
            return data.RaiseThreadMessage(ref msg);
        }

        // Events

        /// <summary>
        /// Components register delegates with this event to handle
        /// thread idle processing.
        ///</summary>
        /// <remarks>
        ///     Callers must have UIPermission(PermissionState.Unrestricted) to call this API.
        /// </remarks>
        /// <SecurityNote>
        ///     Critical: This is blocked off as defense in depth
        ///     PublicOk: There is a demand here
        /// </SecurityNote>
        public static event EventHandler ThreadIdle
        {
            add {
                SecurityHelper.DemandUnrestrictedUIPermission();
                ComponentDispatcher.CurrentThreadData.ThreadIdle += value;
            }
            remove {
                SecurityHelper.DemandUnrestrictedUIPermission();
                ComponentDispatcher.CurrentThreadData.ThreadIdle -= value;
            }
        }

        /// <summary>
        /// Components register delegates with this event to handle
        /// Keyboard Messages (first chance processing).
        ///</summary>
        /// <remarks>
        ///     Callers must have UIPermission(PermissionState.Unrestricted) to call this API.
        /// </remarks>
        /// <SecurityNote>
        ///     Critical: This is blocked off as defense in depth
        ///     PublicOk: There is a demand here
        /// </SecurityNote>
        [SuppressMessage("Microsoft.Design", "CA1009:DeclareEventHandlersCorrectly")]
        public static event ThreadMessageEventHandler ThreadFilterMessage
        {
            add {
                SecurityHelper.DemandUnrestrictedUIPermission();
                ComponentDispatcher.CurrentThreadData.ThreadFilterMessage += value;
            }
            remove {
                SecurityHelper.DemandUnrestrictedUIPermission();
                ComponentDispatcher.CurrentThreadData.ThreadFilterMessage -= value;
            }
        }

        /// <summary>
        /// Components register delegates with this event to handle
        /// Keyboard Messages (second chance processing).
        ///</summary>
        /// <remarks>
        ///     Callers must have UIPermission(PermissionState.Unrestricted) to call this API.
        /// </remarks>
        /// <SecurityNote>
        ///     Critical: Exposing the raw input enables tampering. (The MSG structure is passed by-ref.)
        ///     PublicOk: There is a demand here
        /// </SecurityNote>
        [SuppressMessage("Microsoft.Design", "CA1009:DeclareEventHandlersCorrectly")]
        public static event ThreadMessageEventHandler ThreadPreprocessMessage
        {
            add
            {
                ComponentDispatcher.CurrentThreadData.ThreadPreprocessMessage += value;
            }
            remove {
                ComponentDispatcher.CurrentThreadData.ThreadPreprocessMessage -= value;
            }
        }

        /// <summary>
        ///     Adds the specified handler to the front of the invocation list
        ///     of the PreprocessMessage event.
        /// <summary>
        /// <SecurityNote>
        ///     Critical: Not to expose raw input, which may be destined for a
        ///     window in another security context. Also, MSG contains a window
        ///     handle, which we don't want to expose.
        /// </SecurityNote>
        internal static void CriticalAddThreadPreprocessMessageHandlerFirst(ThreadMessageEventHandler handler)
        {
            ComponentDispatcher.CurrentThreadData.AddThreadPreprocessMessageHandlerFirst(handler);
        }

        /// <summary>
        ///     Removes the first occurance of the specified handler from the
        ///     invocation list of the PreprocessMessage event.
        /// <summary>
        /// <SecurityNote>
        ///     Critical: Not to expose raw input, which may be destined for a
        ///     window in another security context. Also, MSG contains a window
        ///     handle, which we don't want to expose.
        /// </SecurityNote>
        internal static void CriticalRemoveThreadPreprocessMessageHandlerFirst(ThreadMessageEventHandler handler)
        {
            ComponentDispatcher.CurrentThreadData.RemoveThreadPreprocessMessageHandlerFirst(handler);
        }

        /// <summary>
        /// Components register delegates with this event to handle
        /// a component on this thread has "gone modal", when previously none were.
        ///</summary>
        /// <remarks>
        ///     Callers must have UIPermission(PermissionState.Unrestricted) to call this API.
        /// </remarks>
        /// <SecurityNote>
        ///     Critical: This is blocked off as defense in depth
        ///     PublicOk: There is a demand here
        /// </SecurityNote>
        public static event EventHandler EnterThreadModal
        {
            add {
                SecurityHelper.DemandUnrestrictedUIPermission();
                ComponentDispatcher.CurrentThreadData.EnterThreadModal += value;
            }
            remove {
                SecurityHelper.DemandUnrestrictedUIPermission();
                ComponentDispatcher.CurrentThreadData.EnterThreadModal -= value;
            }
        }

        /// <summary>
        /// Components register delegates with this event to handle
        /// all components on this thread are done being modal.
        ///</summary>
        /// <remarks>
        ///     Callers must have UIPermission(PermissionState.Unrestricted) to call this API.
        /// </remarks>
        /// <SecurityNote>
        ///     Critical: This is blocked off as defense in depth
        ///     PublicOk: There is a demand here
        /// </SecurityNote>
        public static event EventHandler LeaveThreadModal
        {
            add
            {
                SecurityHelper.DemandUnrestrictedUIPermission();
                ComponentDispatcher.CurrentThreadData.LeaveThreadModal += value;
            }
            remove {
                SecurityHelper.DemandUnrestrictedUIPermission();
                ComponentDispatcher.CurrentThreadData.LeaveThreadModal -= value;
            }
        }

        // member data
        private static System.LocalDataStoreSlot _threadSlot;
    }
};
