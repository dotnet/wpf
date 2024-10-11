// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading;
using System.Diagnostics.CodeAnalysis;

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
        /// <summary>
        /// Holds a thread-specific instance of <see cref="ComponentDispatcherThread"/>.
        /// </summary>
        [ThreadStatic]
        private static ComponentDispatcherThread s_componentDispatcherThread;

        /// <summary>
        /// Retrieves or creates an instance of <see cref="ComponentDispatcherThread"/> for the current thread.
        /// </summary>
        private static ComponentDispatcherThread CurrentThreadData
        {
            get
            {
                s_componentDispatcherThread ??= new ComponentDispatcherThread();

                return s_componentDispatcherThread;
            }
        }

        // Properties

        /// <summary>
        /// Returns true if one or more components has gone modal.
        /// Although once one component is modal a 2nd shouldn't.
        ///</summary>
        public static bool IsThreadModal
        {
            get
            {
                ComponentDispatcherThread data = ComponentDispatcher.CurrentThreadData;
                return data.IsThreadModal;
            }
        }

        /// <summary>
        /// Returns "current" message. More exactly the last MSG Raised.
        ///</summary>
        public static MSG CurrentKeyboardMessage
        {
            get => CurrentThreadData.CurrentKeyboardMessage;
            internal set => CurrentThreadData.CurrentKeyboardMessage = value;
        }

        // Methods

        /// <summary>
        /// A component calls this to go modal.  Current thread wide only.
        ///</summary>
        public static void PushModal()
        {
            CriticalPushModal();
        }

        /// <summary>
        /// A component calls this to go modal.  Current thread wide only.
        ///</summary>
        internal static void CriticalPushModal()
        {
            ComponentDispatcherThread data = ComponentDispatcher.CurrentThreadData;
            data.PushModal();
        }

        /// <summary>
        /// A component calls this to end being modal.
        ///</summary>
        public static void PopModal()
        {
            CriticalPopModal();
        }

        /// <summary>
        /// A component calls this to end being modal.
        ///</summary>
        internal static void CriticalPopModal()
        {
            ComponentDispatcherThread data = ComponentDispatcher.CurrentThreadData;
            data.PopModal();
        }

        /// <summary>
        /// The message loop pumper calls this when it is time to do idle processing.
        ///</summary>
        [SuppressMessage("Microsoft.Design", "CA1030:UseEventsWhereAppropriate")]
        public static void RaiseIdle()
        {
            ComponentDispatcherThread data = ComponentDispatcher.CurrentThreadData;
            data.RaiseIdle();
        }

        /// <summary>
        /// The message loop pumper calls this for every keyboard message.
        /// </summary>
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
        public static event EventHandler ThreadIdle
        {
            add {
                ComponentDispatcher.CurrentThreadData.ThreadIdle += value;
            }
            remove {
                ComponentDispatcher.CurrentThreadData.ThreadIdle -= value;
            }
        }

        /// <summary>
        /// Components register delegates with this event to handle
        /// Keyboard Messages (first chance processing).
        ///</summary>
        public static event ThreadMessageEventHandler ThreadFilterMessage
        {
            add {
                ComponentDispatcher.CurrentThreadData.ThreadFilterMessage += value;
            }
            remove {
                ComponentDispatcher.CurrentThreadData.ThreadFilterMessage -= value;
            }
        }

        /// <summary>
        /// Components register delegates with this event to handle
        /// Keyboard Messages (second chance processing).
        ///</summary>
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
        internal static void CriticalAddThreadPreprocessMessageHandlerFirst(ThreadMessageEventHandler handler)
        {
            ComponentDispatcher.CurrentThreadData.AddThreadPreprocessMessageHandlerFirst(handler);
        }

        /// <summary>
        ///     Removes the first occurance of the specified handler from the
        ///     invocation list of the PreprocessMessage event.
        /// <summary>
        internal static void CriticalRemoveThreadPreprocessMessageHandlerFirst(ThreadMessageEventHandler handler)
        {
            ComponentDispatcher.CurrentThreadData.RemoveThreadPreprocessMessageHandlerFirst(handler);
        }

        /// <summary>
        /// Components register delegates with this event to handle
        /// a component on this thread has "gone modal", when previously none were.
        ///</summary>
        public static event EventHandler EnterThreadModal
        {
            add {
                ComponentDispatcher.CurrentThreadData.EnterThreadModal += value;
            }
            remove {
                ComponentDispatcher.CurrentThreadData.EnterThreadModal -= value;
            }
        }

        /// <summary>
        /// Components register delegates with this event to handle
        /// all components on this thread are done being modal.
        ///</summary>
        public static event EventHandler LeaveThreadModal
        {
            add
            {
                ComponentDispatcher.CurrentThreadData.LeaveThreadModal += value;
            }
            remove {
                ComponentDispatcher.CurrentThreadData.LeaveThreadModal -= value;
            }
        }
    }
};
