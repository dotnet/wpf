// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// Description: Manages the listeners for one accessibility aid or test application.

using System.Windows;
using System.Collections;
using System.Diagnostics;
using System.Windows.Automation;
using System.Windows.Automation.Provider;
using MS.Win32;

using System;

// PRESHARP: In order to avoid generating warnings about unkown message numbers and unknown pragmas.
#pragma warning disable 1634, 1691

namespace MS.Internal.Automation
{
     // Manages the listeners for one accessibility aid or test application.  Locking
     // is used in all public methods to allow for multiple threads in a client process.
    internal static class ClientEventManager
    {
        //------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------

        #region Constructors

        // Static class, no ctor

        #endregion Constructors


        //------------------------------------------------------
        //
        //  Internal Methods
        //
        //------------------------------------------------------

        #region Internal Methods

        // AddFocusListener - Adds a focus listener for this client.
        internal static void AddFocusListener(Delegate eventCallback, EventListener l)
        {
            AddRootListener(Tracker.Focus, eventCallback, l);
        }

        // RemoveFocusListener - Removes a focus change listener from this client
        // and notifies the UIAccess server
        internal static void RemoveFocusListener(Delegate eventCallback)
        {
            RemoveRootListener(AutomationElement.AutomationFocusChangedEvent, Tracker.Focus, eventCallback);
        }

        // AddListener - Adds a listener for this client and notifies the UIA server.  All event
        // handler additions call through to this method.
        internal static void AddListener(AutomationElement rawEl, Delegate eventCallback, EventListener l)
        {
            lock (_classLock)
            {
                // If we are adding a listener then a proxy could be created as a result of an event so make sure they are loaded
                ProxyManager.LoadDefaultProxies();

                if (_listeners == null)
                {
                    // enough space for 16 AddXxxListeners (100 bytes)
                    _listeners = new ArrayList(16);
                }

                // Start the callback queue that gets us off the server's
                // UI thread when events arrive cross-proc
                CheckStartCallbackQueueing();

                //
                // The framework handles some events on behalf of providers; do those here
                //

                // If listening for BoundingRectangleProperty then may need to start listening on the
                // client-side for LocationChange WinEvent (only use *one* BoundingRectTracker instance).
                if (_winEventTrackers[(int)Tracker.BoundingRect] == null && HasProperty(AutomationElement.BoundingRectangleProperty, l.Properties))
                {
                    // There may be special cases to not map BoundingRect to WinEvent. One may be
                    // where rawEl is a non-top-level native implementation and TreeScope == Element.
                    // Another may be if rawEl is a native impl and TreeScope includes descendents then
                    // may not need to worry about BoundingRect prop change on hosted Win32 UI? I think the
                    // answer is 'yes' because BoundingRectangleProperty should fire events for the top-most
                    // element that moved in the hierarchy.  So if hosted Win32 content Rect changes w/o
                    // the host changing then it would fire this event.
                    // Note: Part of cleaning up WinEvent listeners is to move away from WinEvent handlers calling UIA client handlers
                    AddWinEventListener(Tracker.BoundingRect, new BoundingRectTracker());
                }

                // Start listening for menu event in order to raise MenuOpened/Closed events.
                if ( _winEventTrackers [(int)Tracker.MenuOpenedOrClosed] == null && (l.EventId == AutomationElement.MenuOpenedEvent || l.EventId == AutomationElement.MenuClosedEvent) )
                {
                    AddWinEventListener( Tracker.MenuOpenedOrClosed, new MenuTracker( new MenuHandler( OnMenuEvent ) ) );
                }

                // Begin watching for hwnd open/close/show/hide so can advise of what events are being listened for.
                // Only advise UI contexts of events being added if the event might be raised by a provider.
                // TopLevelWindow event is raised by UI Automation framework so no need to track new UI.
                // Are there other events like this where Advise can be skipped?
                if (_winEventTrackers[(int)Tracker.WindowShowOrOpen] == null )
                {
                    AddWinEventListener( Tracker.WindowShowOrOpen, new WindowShowOrOpenTracker( new WindowShowOrOpenHandler( OnWindowShowOrOpen ) ) );
                    AddWinEventListener( Tracker.WindowHideOrClose, new WindowHideOrCloseTracker( new WindowHideOrCloseHandler( OnWindowHideOrClose ) ) );
                }

                // If listening for WindowInteractionStateProperty then may need to start listening on the
                // client-side for ObjectStateChange WinEvent.
                if (_winEventTrackers[(int)Tracker.WindowInteractionState] == null && HasProperty(WindowPattern.WindowInteractionStateProperty, l.Properties))
                {
                    AddWinEventListener(Tracker.WindowInteractionState, new WindowInteractionStateTracker());
                }

                // If listening for WindowVisualStateProperty then may need to start listening on the
                // client-side for ObjectLocationChange WinEvent.
                if (_winEventTrackers[(int)Tracker.WindowVisualState] == null && HasProperty(WindowPattern.WindowVisualStateProperty, l.Properties))
                {
                    AddWinEventListener(Tracker.WindowVisualState, new WindowVisualStateTracker());
                }

                // Wrap and store this record on the client...
                EventListenerClientSide ec = new EventListenerClientSide(rawEl, eventCallback, l);
                _listeners.Add(ec);

                // Only advise UI contexts of events being added if the event might be raised by
                // a provider.  TopLevelWindow event is raised by UI Automation framework.
                if (ShouldAdviseProviders( l.EventId ))
                {
                    // .. then let the server know about this listener
                    ec.EventHandle = UiaCoreApi.UiaAddEvent(rawEl.RawNode, l.EventId.Id, ec.CallbackDelegate, l.TreeScope, PropertyArrayToIntArray(l.Properties), l.CacheRequest);
                }
            }
        }

        private static int[] PropertyArrayToIntArray(AutomationProperty[] properties)
        {
            if (properties == null)
                return null;
            int[] propertiesAsInts = new int[properties.Length];
            for (int i = 0; i < properties.Length; i++)
            {
                propertiesAsInts[i] = properties[i].Id;
            }
            return propertiesAsInts;
        }


        // RemoveListener - Removes a listener from this client and notifies the UIAutomation server-side
        internal static void RemoveListener( AutomationEvent eventId, AutomationElement el, Delegate eventCallback )
        {
            lock( _classLock )
            {
                if( _listeners != null )
                {
                    bool boundingRectListeners = false; // if not removing BoundingRect listeners no need to do check below
                    bool menuListeners = false; // if not removing MenuOpenedOrClosed listeners no need to do check below
                    bool windowInteracationListeners = false; // if not removing WindowsIntercation listeners no need to do check below
                    bool windowVisualListeners = false; // if not removing WindowsVisual listeners no need to do check below

                    for (int i = _listeners.Count - 1; i >= 0; i--)
                    {
                        EventListenerClientSide ec = (EventListenerClientSide)_listeners[i];
                        if( ec.IsListeningFor( eventId, el, eventCallback ) )
                        {
                            EventListener l = ec.EventListener;

                            // Only advise UI contexts of events being removed if the event might be raised by
                            // a provider.  TopLevelWindow event is raised by UI Automation framework.
                            if ( ShouldAdviseProviders(eventId) )
                            {
                                // Notify the server-side that this event is no longer interesting
                                try
                                {
                                    ec.EventHandle.Dispose(); // Calls UiaCoreApi.UiaRemoveEvent
                                }
// PRESHARP: Warning - Catch statements should not have empty bodies
#pragma warning disable 6502
                                catch (ElementNotAvailableException)
                                {
                                    // the element is gone already; continue on and remove the listener
                                }
#pragma warning restore 6502
                                finally
                                {
                                    ec.Dispose();
                                }
                            }

                            // before removing, check if this delegate was listening for the below events
                            // and see if we can stop clientside WinEvent trackers.
                            if (HasProperty(AutomationElement.BoundingRectangleProperty, l.Properties))
                            {
                                boundingRectListeners = true;
                            }

                            if( eventId == AutomationElement.MenuOpenedEvent || eventId == AutomationElement.MenuClosedEvent )
                            {
                                menuListeners = true;
                            }

                            if (HasProperty(WindowPattern.WindowInteractionStateProperty, l.Properties))
                            {
                                windowInteracationListeners = true;
                            }

                            if (HasProperty(WindowPattern.WindowVisualStateProperty, l.Properties))
                            {
                                windowVisualListeners = true;
                            }

                            // delete this one
                            _listeners.RemoveAt( i );
                        }
                    }

                    // Check listeners bools to see if clientside listeners can be removed
                    if (boundingRectListeners)
                    {
                        RemovePropertyTracker(AutomationElement.BoundingRectangleProperty, Tracker.BoundingRect);
                    }

                    if (menuListeners)
                    {
                        RemoveMenuListeners();
                    }

                    if (windowInteracationListeners)
                    {
                        RemovePropertyTracker(WindowPattern.WindowInteractionStateProperty, Tracker.WindowInteractionState);
                    }

                    if (windowVisualListeners)
                    {
                        RemovePropertyTracker(WindowPattern.WindowVisualStateProperty, Tracker.WindowVisualState);
                    }

                    // See if we can cleanup completely
                    if (_listeners.Count == 0)
                    {
                        // as long as OnWindowShowOrOpen is static can just use new here and get same object instance
                        // (if there's no WindowShowOrOpen listener, this method just returns)
                        RemoveWinEventListener(Tracker.WindowShowOrOpen, new WindowShowOrOpenHandler(OnWindowShowOrOpen));
                        RemoveWinEventListener( Tracker.WindowHideOrClose, new WindowHideOrCloseHandler( OnWindowHideOrClose ) );

                        _listeners = null;
                    }
                }
            }
        }

        private static void RemovePropertyTracker(AutomationProperty property, Tracker tracker)
        {
            bool foundListener = false;     // assume none
            foreach (EventListenerClientSide l in _listeners)
            {
                if (HasProperty(property, l.EventListener.Properties))
                {
                    foundListener = true;  // delegate is still interested so can't
                    break;                 // stop the WinEvent tracking
                }
            }
            if (!foundListener)
            {
                RemoveWinEventListener(tracker, null);
            }
        }

        private static void RemoveMenuListeners()
        {
            bool menuListeners = false; // assume none
            foreach (EventListenerClientSide l in _listeners)
            {
                if (l.EventListener.EventId == AutomationElement.MenuOpenedEvent || l.EventListener.EventId == AutomationElement.MenuClosedEvent)
                {
                    menuListeners = true;  // delegate is still interested so can't
                    break;                       // stop the WinEvent tracking
                }
            }
            if (!menuListeners)
            {
                // as long as OnMenuEvent is static can just use new here and get same object instance
                RemoveWinEventListener(Tracker.MenuOpenedOrClosed, new MenuHandler(OnMenuEvent));
            }
        }

        // RemoveAllListeners - Removes all listeners from this client and notifies the
        // UIAccess server
        internal static void RemoveAllListeners()
        {
            lock (_classLock)
            {
                if (_listeners == null)
                    return;

                // Stop all WinEvent tracking
                StopWinEventTracking();

                // Must remove from back to front when calling RemoveAt because ArrayList
                // elements are compressed after each RemoveAt.
                for (int i = _listeners.Count - 1; i >= 0; i--)
                {
                    EventListenerClientSide ec = (EventListenerClientSide)_listeners[i];

                    // Notify the server-side UIAccess that this event is no longer interesting
                    EventListener l = ec.EventListener;
                    // Only advise UI contexts of events being removed if the event might be raised by
                    // a provider.  TopLevelWindow event is raised by UI Automation framework.
                    if ( ShouldAdviseProviders(l.EventId) )
                    {
                        ec.EventHandle.Dispose(); // Calls RemoveEvent
                    }
                    // delete this one
                    _listeners.RemoveAt(i);
                }

                _listeners = null;
                CheckStopCallbackQueueing();
            }
        }

        #endregion Internal Methods


        //------------------------------------------------------
        //
        //  Internal Properties
        //
        //------------------------------------------------------

        #region Internal Properties

        // return the queue class instance
        internal static QueueProcessor CBQ
        {
            get
            {
                return _callbackQueue;
            }
        }

        #endregion Internal Properties


        //------------------------------------------------------
        //
        //  Private Methods
        //
        //------------------------------------------------------

        #region Private Methods

        // check queue is started, or start if necessary (always called w/in a lock)
        private static void CheckStartCallbackQueueing()
        {
            if (!_isBkgrdThreadRunning)
            {
                _isBkgrdThreadRunning = true;
               _callbackQueue = new QueueProcessor();
               _callbackQueue.StartOnThread();
            }
        }


        // check if queue is still needed, stop if necessary (always called w/in a lock)
        private static void CheckStopCallbackQueueing()
        {
            // anything to stop?
            if (!_isBkgrdThreadRunning)
                return;

            // if there are listeners then can't stop
            if (_listeners != null)
                return;

            // Are any WinEvents currently being tracked for this client?
            foreach (WinEventWrap eventWrapper in _winEventTrackers)
            {
                if (eventWrapper != null)
                {
                    return;
                }
            }

            // OK to stop the queue now
            _isBkgrdThreadRunning = false;
            _callbackQueue.PostQuit();
            // Intentionally not setting _callbackQueue null here; don't want to mess with it from this thread.
        }

        // StopWinEventTracking is called when we just want to quit (RemoveAllListeners)
        private static void StopWinEventTracking()
        {
            int i;
            for (i=0; i<(int)Tracker.NumEventTrackers; i++)
            {
                WinEventWrap eventWrapper = _winEventTrackers[i];
                if (eventWrapper != null)
                {
                    eventWrapper.StopListening();
                    _winEventTrackers[i] = null;
                }
            }
        }

        // Raise events for rawEl
        internal static void RaiseEventInThisClientOnly(AutomationEvent eventId, AutomationElement rawEl, AutomationEventArgs e)
        {
            // This version of RaiseEventInThisClientOnly can be called with a local (proxied) or remote (native)AutomationElement
            lock (_classLock)
            {
                if ( _listeners == null )
                    return;

                AutomationElement el = rawEl;
                foreach (EventListenerClientSide listener in _listeners)
                {
                    // Is this event a type this listener is interested in?
                    if (listener.EventListener.EventId == eventId)
                    {
                        // Did this event happen on an element this listener is interested in?
                        if (rawEl == null || listener.WithinScope( rawEl ))
                        {
                            UiaCoreApi.UiaCacheRequest cacheRequest = listener.EventListener.CacheRequest;
                            CBQ.PostWorkItem(new ClientSideQueueItem(listener.ClientCallback, el, cacheRequest, e));
                        }
                    }
                }
            }
        }

        // Raise events for the element that has RuntimeId == rid (special case for events where the element is no longer available)
        internal static void RaiseEventInThisClientOnly( AutomationEvent eventId, int [] rid, AutomationEventArgs e)
        {
            // This version of RaiseEventInThisClientOnly can be called with a local (proxied) or remote (native)AutomationElement
            lock ( _classLock )
            {
                if ( _listeners == null )
                    return;

                foreach ( EventListenerClientSide listener in _listeners )
                {
                    // Is this event a type this listener is interested in?
                    if ( listener.EventListener.EventId == eventId )
                    {
                        // Did this event happen on an element this listener is interested in?
                        if ( listener.WithinScope( rid ) )
                        {
                            CBQ.PostWorkItem(new ClientSideQueueItem(listener.ClientCallback, null, null, e));
                        }
                    }
                }
            }
        }

        // GetNewRootTracker - Returns a new WinEvent wrapped object for events that are
        // always based on the RootElement (e.g. AutomationFocusChanged or TopWindow events)
        private static WinEventWrap GetNewRootTracker(Tracker idx)
        {
            if (idx == Tracker.Focus)
            {
                return new FocusTracker();
            }

            Debug.Assert(false, "GetNewRootTracker internal error: Unexpected Tracker value!");
            return null;
        }

        // AddRootListener - Add a listener for an event whose reference is always the
        // root and scope is all elements
        private static void AddRootListener(Tracker idx, Delegate eventCallback, EventListener l)
        {
            lock ( _classLock )
            {
                // Add this listener to client-side store of listeners and give the server
                // a chance to enable accessibility for this event
                AddListener( AutomationElement.RootElement, eventCallback, l );

                // Track WinEvents
                WinEventWrap eventWrapper = _winEventTrackers[(int)idx];

                if ( eventWrapper == null )
                {
                    // First time create a WinEvent tracker and start listening
                    AddWinEventListener( idx, GetNewRootTracker( idx ) );
                }
                else
                {
                    // Subsequent times just add the callback to the existing WinEvent
                    eventWrapper.AddCallback( eventCallback );
                }
            }
        }

        // RemoveRootListener - Remove a listener for an event whose reference is always
        // the root and scope is all elements
        private static void RemoveRootListener(AutomationEvent eventId, Tracker idx, Delegate eventCallback)
        {
            lock (_classLock)
            {
                RemoveListener(eventId, AutomationElement.RootElement, eventCallback);
                RemoveWinEventListener(idx, eventCallback);
            }
        }

        // AddWinEventListener - add an event callback for a global listener
        private static void AddWinEventListener(Tracker idx, WinEventWrap eventWrapper)
        {
            // make sure we can queue items
            CheckStartCallbackQueueing();

            _winEventTrackers[(int)idx] = eventWrapper;
            _callbackQueue.PostSyncWorkItem(new WinEventQueueItem(eventWrapper, WinEventQueueItem.StartListening));
        }

        // RemoveWinEventListener - remove an event callback for a global listener
        private static void RemoveWinEventListener(Tracker idx, Delegate eventCallback)
        {
            WinEventWrap eventWrapper = _winEventTrackers[(int)idx];
            if (eventWrapper == null)
                return;

            bool fRemovedLastListener = eventWrapper.RemoveCallback(eventCallback);
            if (fRemovedLastListener)
            {
                _callbackQueue.PostSyncWorkItem(new WinEventQueueItem(eventWrapper, WinEventQueueItem.StopListening));
                _winEventTrackers[(int)idx] = null;

                CheckStopCallbackQueueing();
            }
        }

        // HasProperty - helper to check for a property in an AutomationProperty array
        private static bool HasProperty(AutomationProperty p, AutomationProperty [] properties)
        {
            if (properties == null)
                return false;

            foreach (AutomationProperty p1 in properties)
            {
                if (p1 == p)
                {
                    return true;
                }
            }
            return false;
        }

        // OnWindowHideOrClose - Called by the WindowHideOrCloseTracker class when UI is hidden or destroyed
        private static void OnWindowHideOrClose( IntPtr hwnd, AutomationElement rawEl, int [] runtimeId )
        {
            bool doWindowClosedEvent = false;
            bool doStructureChangedEvent = false;

            lock ( _classLock )
            {
                if (_listeners != null)
                {
                    // if an hwnd is hidden or closed remove event listeners for the window's provider
                    for (int i = 0; i < _listeners.Count; i++)
                    {
                        EventListenerClientSide ec = (EventListenerClientSide)_listeners[i];

                        EventListener l = ec.EventListener;
                        if ( l.EventId == WindowPattern.WindowClosedEvent )
                            doWindowClosedEvent = true;
                        if ( l.EventId == AutomationElement.StructureChangedEvent )
                            doStructureChangedEvent = true;

                        // Only advise UI contexts if the provider still exists
                        // (but keep looking to see if need to do a WindowClosedEvent)
                        if (rawEl == null)
                            continue;

                        // Only advise UI contexts if the provider might raise that event.
                        if (!ShouldAdviseProviders(l.EventId))
                            continue;

                        // Only advise UI contexts if the element is w/in scope of the reference element
                        if (!ec.WithinScope(rawEl))
                            continue;

                        // Notify the server-side that this event is no longer interesting
                        UiaCoreApi.UiaEventRemoveWindow(ec.EventHandle, hwnd);
                    }
                }
            }

            // Piggy-back on the listener for Windows hiding or closing to raise WindowClosed and StructureChanged events.
            // When the hwnd behind rawEl is being destroyed, it can't be determined that rawEl once had the
            // WindowPattern interface.  Therefore raise an event for any window close.
            if ( doWindowClosedEvent )
            {
                // When the hwnd is just hidden, rawEl will not be null, so can test if this would support WindowPattern
                // and throw this event away if the window doesn't support that CP
                if ( rawEl != null && !HwndProxyElementProvider.IsWindowPatternWindow( NativeMethods.HWND.Cast( hwnd ) ) )
                    return;

                // Go ahead and raise a client-side only WindowClosedEvent (if anyone is listening)
                WindowClosedEventArgs e = new WindowClosedEventArgs( runtimeId );
                RaiseEventInThisClientOnly(WindowPattern.WindowClosedEvent, runtimeId, e);
            }
            if ( doStructureChangedEvent )
            {
                // Raise an event for structure changed.  This element has essentially gone away so there isn't an
                // opportunity to do filtering here.  So, just like WindowClosed, this event will be very noisy.
                StructureChangedEventArgs e = new StructureChangedEventArgs( StructureChangeType.ChildRemoved, runtimeId );
                RaiseEventInThisClientOnly(AutomationElement.StructureChangedEvent, runtimeId, e);
            }
        }

        // OnWindowShowOrOpen - Called by the WindowShowOrOpenTracker class when UI is shown or created
        private static void OnWindowShowOrOpen( IntPtr hwnd, AutomationElement rawEl )
        {
            bool doWindowOpenedEvent = false;
            bool doStructureChangedEvent = false;

            lock ( _classLock )
            {
                if (_listeners != null)
                {
                    // if rawEl is w/in the scope of any listeners then register for events in the new UI
                    for (int i = 0; i < _listeners.Count; i++)
                    {
                        EventListenerClientSide ec = (EventListenerClientSide)_listeners[i];

                        EventListener l = ec.EventListener;
                        if ( l.EventId == WindowPattern.WindowOpenedEvent )
                            doWindowOpenedEvent = true;
                        if ( l.EventId == AutomationElement.StructureChangedEvent )
                            doStructureChangedEvent = true;

                        // Only advise UI contexts if the provider might raise that event.
                        if (!ShouldAdviseProviders( l.EventId ))
                            continue;

                        // Only advise UI contexts if the element is w/in scope of the reference element
                        if (!ec.WithinScope( rawEl ))
                            continue;

                        // Notify the server side
                        UiaCoreApi.UiaEventAddWindow(ec.EventHandle, hwnd);
                    }
                }
            }

            // Piggy-back on the listener for Windows hiding or closing to raise WindowClosed and StructureChanged events.
            if ( doWindowOpenedEvent )
            {
                if ( HwndProxyElementProvider.IsWindowPatternWindow( NativeMethods.HWND.Cast( hwnd ) ) )
                {
                    // Go ahead and raise a client-side only WindowOpenedEvent (if anyone is listening)
                    AutomationEventArgs e = new AutomationEventArgs( WindowPattern.WindowOpenedEvent );
                    RaiseEventInThisClientOnly( WindowPattern.WindowOpenedEvent, rawEl, e);
                }
            }
            if ( doStructureChangedEvent )
            {
                // Filter on the control elements.  Otherwise, this is extremely noisy.  Consider not filtering if there is feedback.
                //ControlType ct = (ControlType)rawEl.GetPropertyValue( AutomationElement.ControlTypeProperty );
                //if ( ct != null )
                {
                    // Last,raise an event for structure changed
                    StructureChangedEventArgs e = new StructureChangedEventArgs( StructureChangeType.ChildAdded, rawEl.GetRuntimeId() );
                    RaiseEventInThisClientOnly(AutomationElement.StructureChangedEvent, rawEl, e);
                }
            }
        }

        // OnMenuEvent - Called by MenuTracker class
        private static void OnMenuEvent( AutomationElement rawEl, bool menuHasOpened )
        {
            AutomationEvent eventId = menuHasOpened ? AutomationElement.MenuOpenedEvent : AutomationElement.MenuClosedEvent;
            AutomationEventArgs e = new AutomationEventArgs( eventId );

            RaiseEventInThisClientOnly(eventId, rawEl, e);
        }


        private static bool ShouldAdviseProviders( AutomationEvent eventId )
        {
            foreach (AutomationEvent ev in _doNotShouldAdviseProviders)
            {
                if (ev == eventId)
                    return false;
            }

            return true;
        }

        #endregion Private Methods


        //------------------------------------------------------
        //
        //  Private Fields
        //
        //------------------------------------------------------

        #region Private Fields

        // Indices into ClientEventManager._winEventTrackers.  How wrapped WinEvents are added
        // and removed is common code; using an array makes it easier to add, remove and do
        // cleanup on client exit.
        private enum Tracker
        {
            Focus = 0,
            WindowShowOrOpen,
            WindowHideOrClose,
            BoundingRect,
            MenuOpenedOrClosed,
            WindowInteractionState,
            WindowVisualState,
            // insert additional indices here...
            NumEventTrackers,
        }

        // If WindowPattern was exposed by the proxies then the WindowPattern code here
        // should change to remove WindowPattern events from those UIAutomation client-side raises.
        private static AutomationEvent[] _doNotShouldAdviseProviders = new AutomationEvent[] {
            WindowPattern.WindowOpenedEvent, WindowPattern.WindowClosedEvent
        };

        private static WinEventWrap [] _winEventTrackers = new WinEventWrap[(int)Tracker.NumEventTrackers];

        private static QueueProcessor _callbackQueue;      // callbacks are queued on this class to avoid deadlocks
        private static bool _isBkgrdThreadRunning = false; // is there a background thread for queueing and recieving WinEvents?
        private static ArrayList _listeners;               // data representing events the client is listening for
        private static object _classLock = new object();   // use lock object vs typeof(class) for perf reasons

        #endregion Private Fields
    }
}
