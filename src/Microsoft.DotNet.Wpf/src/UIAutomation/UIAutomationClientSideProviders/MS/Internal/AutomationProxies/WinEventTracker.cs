// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// Description: Handles WinEvent notifications.

// PRESHARP: In order to avoid generating warnings about unkown message numbers and unknown pragmas.
#pragma warning disable 1634, 1691

using System;
using System.Collections;
using System.Reflection;
using System.Windows.Automation;
using System.Windows.Automation.Provider;
using System.Runtime.InteropServices;
using System.Diagnostics;
using MS.Win32;

namespace MS.Internal.AutomationProxies
{
    // Manage a list of notifications per WinEvents to send for a given set of hwnd. 
    // All members are static
    // 
    // A static arrays is  maintained with the size of the total 
    // number of known WinEvents id (around 50).
    //
    // Each entry in the array contains a list of hwnd that should receive a 
    // notification for a given EventId. The list is dynamically built on each call 
    // to AdviseEventAdded/AdviseEventRemoved.
    //
    // Notifications are processed:
    //     Convert an EventID into an index for the above array.
    //     Sequential traverse of the list of windows handles 
    //     to find a match. (for one WinEvent Id)
    //     Call the delegate associated with the hwnd to create a raw element.
    //     Call the automation code to queue a new notification for the client.
    //     
    static class WinEventTracker
    {
        #region Internal Methods

        // ------------------------------------------------------
        //
        // Internal Methods
        //
        // ------------------------------------------------------
        // Update the Array of hwnd requesting notifications
        // Param name="hwnd"
        // Param name="raiseEvents" - function to call to create a raw element
        // Param name="aEvtIdProp"
        // Param name="cProps" - Number of valid props in the array
        static internal void AddToNotificationList (IntPtr hwnd, ProxyRaiseEvents raiseEvents, EvtIdProperty[] aEvtIdProp, int cProps)
        {
            GetCallbackQueue();

            // Build the list of Event to Window List
            BuildEventsList (EventFlag.Add, hwnd, raiseEvents, aEvtIdProp, cProps);
        }


        // Get the QueueProcessor so MSAA events can use it as well
        static internal QueueProcessor GetCallbackQueue()
        {
            lock (_queueLock)
            {
                // Create the thread to process the notification if necessary
                if (_callbackQueue == null)
                {
                    _callbackQueue = new QueueProcessor();
                    _callbackQueue.StartOnThread();
                }
            }

            return _callbackQueue;
        }

        // Update the Array of hwnd requesting notifications calling the main routine
        // Param name="hwnd"
        // Param name="raiseEvents" - Callback, should be null for non system-wide events
        // Param name="aEvtIdProp"
        // Param name="cProps" - Number of valid props in the array
        static internal void RemoveToNotificationList (IntPtr hwnd, EvtIdProperty[] aEvtIdProp, ProxyRaiseEvents raiseEvents, int cProps)
        {
            // Remove the list of Event to Window List
            // NOTE: raiseEvents must be null in the case when event is not a system-wide event
            BuildEventsList (EventFlag.Remove, hwnd, raiseEvents, aEvtIdProp, cProps);
        }

        #endregion

        #region Internal Types

        // ------------------------------------------------------
        //
        // Internal Types Declaration
        //
        // ------------------------------------------------------
        // Callback into the Proxy code to create a raw element based on a WinEvent callback parameters
        internal delegate void ProxyRaiseEvents (IntPtr hwnd, int eventId, object idProp, int idObject, int idChild);

        // Association, WinEvent/Automation Property/RawBase class property
        internal struct EvtIdProperty
        {
            // Win32 Win Events Id
            internal int _evtId;

            // Automation Property or Automation Event
            internal object _idProp;

            // constructor
            internal EvtIdProperty (int evtId, object idProp)
            {
                _evtId = evtId;
                _idProp = idProp;
            }
        }

        // WinEvent Notification parameters. Used as data in a hash table, there is one of these 
        // per call to SetWineventHook.  Must be a class as a ref is send to an another thread via 
        // a messenging scheme.  
        // The array list _alHwnd catains a struct of type EventCreateParams.
        internal class EventHookParams
        {
            // List of hwnd that requested to receive notification event
            internal ArrayList _alHwnd;

            // Win32 Hook handle from SetWinEventHook.
            internal IntPtr _winEventHook;

            // Reference to the locked Procedure
            internal GCHandle _procHookHandle;

            // the procces that we are getting events for
            internal uint _process;

            // index in the array of winevents
            internal int _evtId;
        }

        // Function call to either the Start or Stop Listener
        internal delegate void StartStopDelegate (ref EventHookParams hp);

        #endregion

        #region Private Methods

        // ------------------------------------------------------
        //
        // Private Methods
        //
        // ------------------------------------------------------

        // Enables the notification of WinEvents.
        // This function must be called once for each WinEvent to track.
        private static void StartListening (ref EventHookParams hp)
        {
            NativeMethods.WinEventProcDef proc = new NativeMethods.WinEventProcDef (WinEventProc);

            uint processId = hp._process;
            // The console window is special.  It does not raise its own WinEvents.  The CSRSS raises the
            // WinEvents for console windows.  When calling SetWinEventHook use the CSRSS process id instead of the
            // console windows process id, so that we receive the WinEvents for the console windows.
            if (IsConsoleProcess((int)processId))
            {
                try
                {
                    processId = CSRSSProcessId;
                }
                catch (Exception e)
                {
                    if (Misc.IsCriticalException(e))
                    {
                        throw;
                    }

                    processId = hp._process;
                }
            }

            hp._procHookHandle = GCHandle.Alloc (proc);
            hp._winEventHook = Misc.SetWinEventHook(hp._evtId, hp._evtId, IntPtr.Zero, proc, processId, 0, NativeMethods.WINEVENT_OUTOFCONTEXT);
        }

        // Disable the notification of WinEvents.
        // This function must be called once for each WinEvent to track.
        private static void StopListening (ref EventHookParams hp)
        {
            // remove the hook

            Misc.UnhookWinEvent(hp._winEventHook);
            hp._procHookHandle.Free ();
        }

        // Callback function for WinEvents
        // 
        // Notifications are processed that way:
        //     Convert an EventID into an index for the above array.
        //     Sequential traverse of the list of windows handles for a give EventID 
        //     to find a match.
        //     Call the delegate associated with the hwnd to create a raw element.
        //     Call the automation code to queue a new notification for the client.
        private static void WinEventProc (int winEventHook, int eventId, IntPtr hwnd, int idObject, int idChild, int eventThread, uint eventTime)
        {
            if (hwnd == IntPtr.Zero)
            {
                // filter out non-hwnd events - eg. listening for hide/show also gets us mouse pointer (OBJID_CURSOR)
                // hide/show events (eg. when mouse is hidden as text is being entered) that have NULL hwnd...
                return;
            }

            try
            {
                int evt = Array.BinarySearch(_eventIdToIndex, eventId);
                if (evt < 0)
                    return; // negative means this event is unknown so ignore it

                // All operations in the list of events and windows handle must be atomic
                lock (_ahp)
                {
                    EventHookParams hookParams = null;

                    // Don't use the Misc.GetWindowThreadProcessId() helper since that throws; some events we want even
                    // though the hwnd is no longer valid (e.g. menu item events).
                    uint processId;
                    // Disabling the PreSharp error since GetWindowThreadProcessId doesn't use SetLastError().
    #pragma warning suppress 6523
                    if (UnsafeNativeMethods.GetWindowThreadProcessId(hwnd, out processId) != 0)
                    {
                        // Find the EventHookParams.  
                        // _ahp is an array of Hashtables where each Hashtable corrasponds to one event.
                        // Get the correct Hashtable using the index evt.  Then lookup the EventHookParams
                        // in the hash table, using the process id.
                        hookParams = (EventHookParams)_ahp[evt][processId];
                    }

                    // Sanity check
                    if (hookParams != null && hookParams._alHwnd != null)
                    {
                        ArrayList eventCreateParams = hookParams._alHwnd;

                        // Loop for all the registered hwnd listeners for this event
                        for (int index = eventCreateParams.Count - 1; index >= 0; index--)
                        {
                            EventCreateParams ecp = (EventCreateParams)eventCreateParams[index];

                            // if hwnd of the event matches the registered hwnd -OR- this is a global event (all hwnds)
                            // -AND- the hwnd is still valid have the proxies raise appropriate events.
                            if (ecp._hwnd == hwnd || ecp._hwnd == IntPtr.Zero)
                            {
                                // If this event isn't on a menu element that has just been invoked, throw away the event if the hwnd is gone
                                if (!((idObject == NativeMethods.OBJID_MENU || idObject == NativeMethods.OBJID_SYSMENU) && eventId == NativeMethods.EventObjectInvoke) &&
                                    !UnsafeNativeMethods.IsWindow(hwnd))
                                {
                                    continue;
                                }

                                try
                                {
                                    // Call the proxy code to create a raw element. null is valid as a result
                                    // The proxy must fill in the parameters for the AutomationPropertyChangedEventArgs
                                    ecp._raiseEventFromRawElement(hwnd, eventId, ecp._idProp, idObject, idChild);
                                }
                                catch (ElementNotAvailableException)
                                {
                                    // the element has gone away from the time the event happened and now
                                    // So continue the loop and allow the other proxies a chance to raise the event.
                                    continue;
                                }
                                catch (Exception e)
                                {
                                    // If we get here there is a problem in a proxy that needs fixing.
                                    Debug.Assert(false, "Exception raising event " + eventId + " for prop " + ecp._idProp + " on hwnd " + hwnd + "\n" + e.Message);

                                    if (Misc.IsCriticalException(e))
                                    {
                                        throw;
                                    }

                                    // Do not break the loop for one mis-behaving proxy.
                                    continue;
                                }
                            }
                        }
                    }

                    // handle global events.  These are usually for things that do not yet exist like show events
                    // where the hwnd is not there until it is shown.  So we need to raise these event all the time.
                    // Office command bars use this.
                    hookParams = (EventHookParams)_ahp[evt][_globalEventKey];
                    if (hookParams != null && hookParams._alHwnd != null)
                    {
                        ArrayList eventCreateParams = hookParams._alHwnd;

                        // Loop for all the registered hwnd listeners for this event
                        for (int index = eventCreateParams.Count - 1; index >= 0; index--)
                        {
                            EventCreateParams ecp = (EventCreateParams)eventCreateParams[index];

                            // We have global event
                            if ((ecp._hwnd == IntPtr.Zero))
                            {
                                try
                                {
                                    // Call the proxy code to create a raw element. null is valid as a result
                                    // The proxy must fill in the parameters for the AutomationPropertyChangedEventArgs
                                    ecp._raiseEventFromRawElement(hwnd, eventId, ecp._idProp, idObject, idChild);
                                }
                                catch (ElementNotAvailableException)
                                {
                                    // the element has gone away from the time the event happened and now
                                    // So continue the loop and allow the other proxies a chance to raise the event.
                                    continue;
                                }
                                catch (Exception e)
                                {
                                    // If we get here there is a problem in a proxy that needs fixing.
                                    Debug.Assert(false, "Exception raising event " + eventId + " for prop " + ecp._idProp + " on hwnd " + hwnd + "\n" + e.Message);

                                    if (Misc.IsCriticalException(e))
                                    {
                                        throw;
                                    }

                                    // Do not break the loop for one mis-behaving proxy.
                                    continue;
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                if (Misc.IsCriticalException(e)) 
                    throw; 
                // ignore non-critical errors from external code
            }
        }

        // Update the Array of hwnd requesting notifications
        //      eFlag - Add or Remove
        //      hwnd
        //      raiseEvents - function to call to create a raw element
        //      aEvtIdProp - Array of Tupples WinEvent and Automation properties
        //      cProps  - Number of valid props in the array
        private static void BuildEventsList (EventFlag eFlag, IntPtr hwnd, ProxyRaiseEvents raiseEvents, EvtIdProperty[] aEvtIdProp, int cProps)
        {
            // All operations in the list of events and windows handle must be atomic
            lock (_ahp)
            {
                for (int i = 0; i < cProps; i++)
                {
                    EvtIdProperty evtIdProp = aEvtIdProp[i];

                    // Map a property into a WinEventHookProperty
                    int evt = Array.BinarySearch (_eventIdToIndex, evtIdProp._evtId);

                    // add the window to the list
                    if (evt >= 0)
                    {
                        EventHookParams hookParams = null;
                        
                        uint processId;

                        if (hwnd == IntPtr.Zero)
                        {
                            // if its a global event use this well known key to the hash
                            processId = _globalEventKey;
                        }
                        else
                        {
                            if (Misc.GetWindowThreadProcessId(hwnd, out processId) == 0)
                            {
                                processId = _globalEventKey;
                            }
                        }

                        // If never seens this EventId before. Create the notification object
                        if (_ahp[evt] == null)
                        {
                            // create the hash table the key is the process id
                            _ahp[evt] = new Hashtable(10);
                        }

                        // Find the EventHookParams.  
                        // _ahp is an array of Hashtables where each Hashtable corrasponds to one event.
                        // Get the correct Hashtable using the index evt.  Then lookup the EventHookParams
                        // in the hash table, using the process id.
                        hookParams = (EventHookParams)_ahp[evt][processId];

                        // If there is not an entry for the event for the specified process then create one.
                        if (hookParams == null)
                        {
                            hookParams = new EventHookParams();
                            hookParams._process = processId;
                            _ahp[evt].Add(processId, hookParams);
                        }

                        ArrayList eventCreateParams = hookParams._alHwnd;

                        if (eFlag == EventFlag.Add)
                        {
                            if (eventCreateParams == null)
                            {
                                // empty array, create the hwnd arraylist
                                hookParams._evtId = evtIdProp._evtId;
                                eventCreateParams = hookParams._alHwnd = new ArrayList (16);
                            }

                            // Check if the event for that window already exist.
                            // Discard it as no dups are allowed
                            for (int index = eventCreateParams.Count - 1; index >= 0; index--)
                            {
                                EventCreateParams ecp = (EventCreateParams)eventCreateParams[index];

                                // Code below will discard duplicates:
                                // Proxy cannot subscribe same hwnd to the same event more than once
                                // However proxy can be globaly registered to be always notified of some event, in order to
                                // do this proxy will send IntPtr.Zero as hwnd. Please notice that a given Proxy can be globaly registered 
                                // to some EVENT_XXX only once. This will be ensured via delegate comparison.
                                if ( (hwnd == IntPtr.Zero || ecp._hwnd == hwnd) && 
                                     ecp._idProp == evtIdProp._idProp && 
                                     ecp._raiseEventFromRawElement == raiseEvents)
                                {
                                    return;
                                }
                            }

                            // Set the WinEventHook if first time around
                            if (eventCreateParams.Count == 0)
                            {
                                _callbackQueue.PostSyncWorkItem (new QueueItem.WinEventItem (ref hookParams, _startDelegate));
                            }
                            
                            // add the event into the list
                            // Called after the Post does not matter because of the lock
                            eventCreateParams.Add (new EventCreateParams (hwnd, evtIdProp._idProp, raiseEvents));
                        }
                        else
                        {
                            if ( eventCreateParams == null )
                                return;
                            
                            // Remove a notification
                            // Go through the list of window to find the one
                            for (int index = eventCreateParams.Count - 1; index >= 0; index--)
                            {
                                EventCreateParams ecp = (EventCreateParams)eventCreateParams[index];

                                // Detect if caller should be removed from notification list
                                bool remove = false;

                                if (raiseEvents == null)
                                {
                                    // Not a global wide events                                    
                                    remove = (ecp._hwnd == hwnd && ecp._idProp == evtIdProp._idProp);
                                }
                                else
                                {
                                    // Global events
                                    Debug.Assert (hwnd == IntPtr.Zero, @"BuildEventsList: event is global but hwnd is not null");
                                    remove = (ecp._hwnd == hwnd && ecp._raiseEventFromRawElement == raiseEvents);
                                }

                                if (remove)
                                {
                                    eventCreateParams.RemoveAt (index);

                                    // if empty then stop listening for this event arg
                                    if (eventCreateParams.Count == 0)
                                    {
                                        _callbackQueue.PostSyncWorkItem (new QueueItem.WinEventItem (ref hookParams, _stopDelegate));
                                        _ahp[evt].Remove(processId);
                                    }
                                    break;
                                }
                            }
                        }
                    }
                }
            }
        }

        private static bool IsConsoleProcess(int processId)
        {
            try
            {
                return Misc.ProxyGetClassName(Process.GetProcessById(processId).MainWindowHandle).Equals("ConsoleWindowClass");
            }
            catch (Exception e)
            {
                if (Misc.IsCriticalException(e))
                {
                    throw;
                }

                return false;
            }
        }

        private static uint CSRSSProcessId
        {
            get
            {
                if (_CSRSSProcessId == 0)
                {
                    Process[] localByName = Process.GetProcessesByName("csrss");
                    if (localByName[0] != null)
                    {
                        _CSRSSProcessId = (uint)localByName[0].Id;
                    }
                }

                return _CSRSSProcessId;
            }
        }

        #endregion

        #region Private Fields

        // ------------------------------------------------------
        //
        // Private Fields
        //
        // ------------------------------------------------------
        // Flag
        private enum EventFlag
        {
            Add,
            Remove
        }

        // Maps WinEvents ID to indices in _ahp 
        private static readonly int[] _eventIdToIndex = new int[] {
            NativeMethods.EventSystemSound,
            NativeMethods.EventSystemAlert,
            NativeMethods.EventSystemForeground,
            NativeMethods.EventSystemMenuStart,
            NativeMethods.EventSystemMenuEnd,
            NativeMethods.EventSystemMenuPopupStart,
            NativeMethods.EventSystemMenuPopupEnd,
            NativeMethods.EventSystemCaptureStart,
            NativeMethods.EventSystemCaptureEnd,
            NativeMethods.EventSystemMoveSizeStart,
            NativeMethods.EventSystemMoveSizeEnd,
            NativeMethods.EventSystemContextHelpStart,
            NativeMethods.EventSystemContextHelpEnd,
            NativeMethods.EventSystemDragDropStart,
            NativeMethods.EventSystemDragDropEnd,
            NativeMethods.EventSystemDialogStart,
            NativeMethods.EventSystemDialogEnd,
            NativeMethods.EventSystemScrollingStart,
            NativeMethods.EventSystemScrollingEnd,
            NativeMethods.EventSystemSwitchEnd,
            NativeMethods.EventSystemMinimizeStart,
            NativeMethods.EventSystemMinimizeEnd,
            NativeMethods.EventSystemPaint,
            NativeMethods.EventConsoleCaret,
            NativeMethods.EventConsoleUpdateRegion,
            NativeMethods.EventConsoleUpdateSimple,
            NativeMethods.EventConsoleUpdateScroll,
            NativeMethods.EventConsoleLayout,
            NativeMethods.EventConsoleStartApplication,
            NativeMethods.EventConsoleEndApplication,
            NativeMethods.EventObjectCreate,
            NativeMethods.EventObjectDestroy,
            NativeMethods.EventObjectShow,
            NativeMethods.EventObjectHide,
            NativeMethods.EventObjectReorder,
            NativeMethods.EventObjectFocus,
            NativeMethods.EventObjectSelection,
            NativeMethods.EventObjectSelectionAdd,
            NativeMethods.EventObjectSelectionRemove,
            NativeMethods.EventObjectSelectionWithin,
            NativeMethods.EventObjectStateChange,
            NativeMethods.EventObjectLocationChange,
            NativeMethods.EventObjectNameChange,
            NativeMethods.EventObjectDescriptionChange,
            NativeMethods.EventObjectValueChange,
            NativeMethods.EventObjectParentChange,
            NativeMethods.EventObjectHelpChange,
            NativeMethods.EventObjectDefactionChange,
            NativeMethods.EventObjectAcceleratorChange,
            NativeMethods.EventObjectInvoke,
            NativeMethods.EventObjectTextSelectionChanged
        };

        // WinEventHooks must be processed in the same thread that created them.
        // Use a seperate thread to manage the hooks
        private static QueueProcessor _callbackQueue = null;
        private static object _queueLock = new object();

        // static: Array of Hashtables, one per WinEvent Id. 
        // Each Hashtable contains EventHookParams classes the key is the process id.
        // Each element in the array list is a struct EventCreateParams that contains 
        // the hwnd and the other parameters needed to call the Proxy and then 
        // the client notification
        private static Hashtable[] _ahp = new Hashtable[_eventIdToIndex.Length];

        private static uint _globalEventKey = 0;
        
        // Parameters needed to send a notification to a client
        struct EventCreateParams
        {
            // hwnd requesting a notification
            internal IntPtr _hwnd;

            // Automation Property or Automation Event
            internal object _idProp;

            // delegate to call to Create a RawElementProvider from a hwnd
            internal ProxyRaiseEvents _raiseEventFromRawElement;

            internal EventCreateParams (IntPtr hwnd, object idProp, ProxyRaiseEvents raiseEvents)
            {
                _hwnd = hwnd;
                _idProp = idProp;
                _raiseEventFromRawElement = raiseEvents;
            }
        }

        // function to start and stop WinEvents
        private static StartStopDelegate _startDelegate = new StartStopDelegate (StartListening);

        private static StartStopDelegate _stopDelegate = new StartStopDelegate (StopListening);

        private static uint _CSRSSProcessId = 0;

    #endregion
    }
}

