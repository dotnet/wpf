// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Windows.Interop;

/// <summary>
/// This is the delegate used for registering with the
/// ThreadFilterMessage and ThreadPreprocessMessage Events.
/// </summary>
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
        get => CurrentThreadData.IsThreadModal;
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
    /// A component calls this to go modal. Current thread wide only.
    ///</summary>
    public static void PushModal()
    {
        CurrentThreadData.PushModal();
    }

    /// <summary>
    /// A component calls this to end being modal.
    ///</summary>
    public static void PopModal()
    {
        CurrentThreadData.PopModal();
    }

    /// <summary>
    /// The message loop pumper calls this when it is time to do idle processing.
    ///</summary>
    public static void RaiseIdle()
    {
        CurrentThreadData.RaiseIdle();
    }

    /// <summary>
    /// The message loop pumper calls this for every keyboard message.
    /// </summary>
    public static bool RaiseThreadMessage(ref MSG msg)
    {
        return CurrentThreadData.RaiseThreadMessage(ref msg);
    }

    // Events

    /// <summary>
    /// Components register delegates with this event to handle
    /// thread idle processing.
    ///</summary>
    public static event EventHandler ThreadIdle
    {
        add => CurrentThreadData.ThreadIdle += value;
        remove => CurrentThreadData.ThreadIdle -= value;
    }

    /// <summary>
    /// Components register delegates with this event to handle
    /// Keyboard Messages (first chance processing).
    ///</summary>
    public static event ThreadMessageEventHandler ThreadFilterMessage
    {
        add => CurrentThreadData.ThreadFilterMessage += value;
        remove => CurrentThreadData.ThreadFilterMessage -= value;
    }

    /// <summary>
    /// Components register delegates with this event to handle
    /// Keyboard Messages (second chance processing).
    ///</summary>
    public static event ThreadMessageEventHandler ThreadPreprocessMessage
    {
        add => CurrentThreadData.ThreadPreprocessMessage += value;
        remove => CurrentThreadData.ThreadPreprocessMessage -= value;
    }

    /// <summary>
    /// Adds the specified handler to the front of the invocation list
    /// of the PreprocessMessage event.
    /// <summary>
    internal static void AddThreadPreprocessMessageHandlerFirst(ThreadMessageEventHandler handler)
    {
        CurrentThreadData.AddThreadPreprocessMessageHandlerFirst(handler);
    }

    /// <summary>
    /// Removes the first occurrence of the specified handler from the
    /// invocation list of the PreprocessMessage event.
    /// <summary>
    internal static void RemoveThreadPreprocessMessageHandlerFirst(ThreadMessageEventHandler handler)
    {
        CurrentThreadData.RemoveThreadPreprocessMessageHandlerFirst(handler);
    }

    /// <summary>
    /// Components register delegates with this event to handle
    /// a component on this thread has "gone modal", when previously none were.
    ///</summary>
    public static event EventHandler EnterThreadModal
    {
        add => CurrentThreadData.EnterThreadModal += value;
        remove => CurrentThreadData.EnterThreadModal -= value;
    }

    /// <summary>
    /// Components register delegates with this event to handle
    /// all components on this thread are done being modal.
    ///</summary>
    public static event EventHandler LeaveThreadModal
    {
        add => CurrentThreadData.LeaveThreadModal += value;
        remove => CurrentThreadData.LeaveThreadModal -= value;
    }
}
