// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// Description: Dispatch UIA events based on WinEvents

using System;
using System.Collections;
using System.Diagnostics;
using System.Globalization;
using System.Text;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Automation.Provider;
using MS.Win32;

namespace MS.Internal.AutomationProxies
{
    internal class MSAAEventDispatcher : MSAAWinEventWrap
    {
        //------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------
 
        #region Constructors

        private MSAAEventDispatcher()
            : base(NativeMethods.EVENT_OBJECT_CREATE, NativeMethods.EVENT_OBJECT_ACCELERATORCHANGE)
        { }

        #endregion Constructors


        //------------------------------------------------------
        //
        //  Internal Methods
        //
        //------------------------------------------------------
 
        #region Internal Methods

        // retrieves the global event dispatcher
        internal static MSAAEventDispatcher Dispatcher
        {
            get
            {
                // create on demand
                if (_dispatcher == null)
                {
                    _dispatcher = new MSAAEventDispatcher();
                }
                return _dispatcher;
            }
        }

        // a client is listening for an event on object(s) in the specified window.
        internal void AdviseEventAdded(IntPtr hwnd, AutomationEvent eventId, AutomationProperty[] properties)
        {
            // When you start listening for events you listen for all events and then 
            // when you get the event you filter by any hwnd that got advised.  But you have enough 
            // information when you get advised to listen only for in process where that hwnd resides.  
            // The SetWinEventHook api takes a process id, you could get the process id from the hwnd 
            // and limit the number of event you have to process. 

            // we have a two-level table structure. the top-level table maps an hwnd to another table.
            // the second-level table maps an event (or a property in the case of property-changed-event)
            // in that hwnd to a reference count of the number of clients listening for that event.

            // use a lock to update our tables in one atomic operation.
            lock (this)
            {
                // if we aren't listening for WinEvents then start listening.
                if (_hwndTable.Count == 0)
                {
                    //Debug.WriteLine("Starting listening for WinEvents.", "NativeMsaaProxy");
                    // Posting an item to the queue to start listening for WinEvents. This makes sure it is done on the proper thread
                    // Notably the same thread WinEventTracker uses, which guarantees the thread that SetWinEventHook is called on is
                    // actively pumping messages, which is required for SetWinEventHook to function properly.
                    WinEventTracker.GetCallbackQueue().PostSyncWorkItem(new QueueItem.MSAAWinEventItem(StartListening));
                }

                // if we already have a 2-nd level table for this hwnd then simply update the 2nd-level table.
                // otherwise we need to create a 2-nd level table.
                Hashtable eventTable;
                if (_hwndTable.ContainsKey(hwnd))
                {
                    eventTable = (Hashtable)(_hwndTable[hwnd]);
                }
                else
                {
                    eventTable = new Hashtable();
                    _hwndTable[hwnd] = eventTable;
                }

                // for the single event or each of possibly multiple properties increment the reference counter.
                foreach (AutomationIdentifier key in EventKeys(eventId, properties))
                {
                    eventTable[key] = eventTable.ContainsKey(key) ? (int)eventTable[key] + 1 : 1;
                }
            }
        }

        // a client has stopped listening for an event on object(s) in the specified window.
        internal void AdviseEventRemoved(IntPtr hwnd, AutomationEvent eventId, AutomationProperty[] properties)
        {
            // note: we don't need to worry about removing entries from our tables when windows are destroyed 
            //(EVENT_OBJECT_DESTROY with OBJID_WINDOW) because UIA already watches for this event and calls AdviseEventRemoved.

            // use a lock to update our tables in one atomic operation.
            lock (this)
            {
                // we should have a 2-nd level table for this hwnd...
                if (_hwndTable.ContainsKey(hwnd))
                {
                    Hashtable eventTable = (Hashtable)(_hwndTable[hwnd]);

                    // for the single event or each of possibly multiple properties decrement the reference count.
                    foreach (AutomationIdentifier key in EventKeys(eventId, properties))
                    {
                        // we should have an entry in the 2-nd level table for this event or property...
                        if (eventTable.ContainsKey(key))
                        {
                            // decrement the reference count
                            int refcount = (int)eventTable[key] - 1;
                            Debug.Assert(refcount >= 0);

                            if (refcount > 0)
                            {
                                eventTable[key] = refcount;
                            }
                            else
                            {
                                // if the refcount has gone to zero then remove this entry from the 2-nd level table.
                                eventTable.Remove(key);

                                // if this window doesn't have any more advises then remove it's entry from the top-level table
                                if (eventTable.Count == 0)
                                {
                                    _hwndTable.Remove(hwnd);

                                    // if there are no more advises then we can stop listening for WinEvents
                                    if (_hwndTable.Count == 0)
                                    {
                                        //Debug.WriteLine("Stop listening for WinEvents.", "NativeMsaaProxy");
                                        WinEventTracker.GetCallbackQueue().PostSyncWorkItem(new QueueItem.MSAAWinEventItem(StopListening));
                                    }
                                }
                            }
                        }
                        else
                        {
                            Debug.WriteLine(string.Format(CultureInfo.CurrentCulture, "ERROR: AdviseEventRemoved called for {0} and event/property {1} without matching AdviseEventAdded.", hwnd, key), "NativeMsaaProxy");
                        }
                    }
                }
                else
                {
                    Debug.WriteLine(string.Format(CultureInfo.CurrentCulture, "ERROR: AdviseEventRemoved called for {0} without matching AdviseEventAdded.", hwnd), "NativeMsaaProxy");
                }
            }
        }

        // process a WinEvent notification.
        // this gets called *frequently*.
        internal override void WinEventProc(int eventId, IntPtr hwnd, int idObject, int idChild)
        {
            // we are only interested in the root element (OBJID_CLIENT) or one of its children.
            // if it is any other object id then abort.
            if (idObject <= NativeMethods.OBJID_WINDOW
                && idObject != NativeMethods.OBJID_CLIENT)
            {
                return;
            }

            // if there is an entry in our top-level table for this hwnd...
            if (_hwndTable.Contains(hwnd))
            {
                //Debug.WriteLine(string.Format(CultureInfo.CurrentCulture, "WinEvent {0:X} {1} {2} {3}", eventId, hwnd, idObject, idChild), "MsaaNativeProxy");

                // get the 2-nd level table of events and properties we are listening for in this window
                Hashtable eventTable = (Hashtable)_hwndTable[hwnd];

                switch (eventId)
                {
                    case NativeMethods.EVENT_OBJECT_CREATE:
                    case NativeMethods.EVENT_OBJECT_SHOW:
                    //case NativeMethods.EVENT_OBJECT_DESTROY: (see note in FireStructureChangeEvent.)
                    case NativeMethods.EVENT_OBJECT_HIDE:
                    case NativeMethods.EVENT_OBJECT_REORDER:
                        MaybeFireStructureChangeEvent(eventId, eventTable, hwnd, idObject, idChild);
                        break;

                    case NativeMethods.EVENT_OBJECT_PARENTCHANGE:
                        // Observed invoking sidebar widget property dialogs in Vista
                        MaybeFireStructureChangeEvent(eventId, eventTable, hwnd, idObject, idChild);
                        break;

                    case NativeMethods.EVENT_OBJECT_LOCATIONCHANGE:
                        // we don't need to MaybeFire this for the root (OBJID_CLIENT) element because UIA does it for hwnds.
                        MaybeFirePropertyChangeEvent(null, AutomationElement.BoundingRectangleProperty, eventTable, hwnd, idObject, idChild, false);
                        break;

                    case NativeMethods.EVENT_OBJECT_NAMECHANGE:
                        MaybeFirePropertyChangeEvent(null, AutomationElement.NameProperty, eventTable, hwnd, idObject, idChild, true);
                        break;

                    case NativeMethods.EVENT_OBJECT_SELECTION:
                        MaybeFireSelectionItemEvent(SelectionItemPattern.ElementSelectedEvent, eventTable, hwnd, idObject, idChild);
                        break;

                    case NativeMethods.EVENT_OBJECT_SELECTIONADD:
                        MaybeFireSelectionItemEvent(SelectionItemPattern.ElementAddedToSelectionEvent, eventTable, hwnd, idObject, idChild);
                        break;

                    case NativeMethods.EVENT_OBJECT_SELECTIONREMOVE:
                        MaybeFireSelectionItemEvent(SelectionItemPattern.ElementRemovedFromSelectionEvent, eventTable, hwnd, idObject, idChild);
                        break;

// 







                    case NativeMethods.EVENT_OBJECT_VALUECHANGE:
                        MaybeFirePropertyChangeEvent(ValuePattern.Pattern, ValuePattern.ValueProperty, eventTable, hwnd, idObject, idChild, false);
                        break;

                    case NativeMethods.EVENT_OBJECT_HELPCHANGE:
                        MaybeFirePropertyChangeEvent(null, AutomationElement.HelpTextProperty, eventTable, hwnd, idObject, idChild, true);
                        break;

// VisualDescriptionProperty is not yet defined. Once it is, we should implement it.
//                    case NativeMethods.EVENT_OBJECT_DESCRIPTIONCHANGE:
//                        MaybeFirePropertyChangeEvent(null, AutomationElement.VisualDescriptionProperty, eventTable, hwnd, idObject, idChild, true);
//                        break;
// Do we need to implement these?
//                   case NativeMethods.EVENT_OBJECT_SELECTIONWITHIN:
//                        break;
//                    case NativeMethods.EVENT_OBJECT_STATECHANGE:
//                        break;
                }
            }
        }

        #endregion Internal Methods

        //------------------------------------------------------
        //
        //  Private Methods
        //
        //------------------------------------------------------

        #region Private Methods

        // turns a single event or an array of properties into an array of automation ids so either case
        // can be treated uniformly.
        private AutomationIdentifier[] EventKeys(AutomationEvent eventId, AutomationProperty[] properties)
        {
            return eventId == AutomationElement.AutomationPropertyChangedEvent ? properties : new AutomationIdentifier[] { eventId };
        }

        // returns true if (idObject, idChild) indicates the client object (OBJID_CLIENT).
        private static bool IsClientObject(int idObject, int idChild)
        {
            return idObject == NativeMethods.OBJID_CLIENT && idChild == NativeMethods.CHILD_SELF;
        }

        // fire the element seleceted event if there is a client listening for it.
        private void MaybeFireSelectionItemEvent(AutomationEvent eventId, Hashtable eventTable, IntPtr hwnd, int idObject, int idChild)
        {
            // if the 2-nd level table contains an entry for this property
            if (eventTable.ContainsKey(eventId))
            {
                // create a provider associated with this event and check whether the provider supports the selection item pattern.
                MsaaNativeProvider provider = (MsaaNativeProvider)MsaaNativeProvider.Create(hwnd, idChild, idObject);
                if (provider != null && provider.IsPatternSupported(SelectionItemPattern.Pattern))
                {
                    // fire the event
                    AutomationEventArgs eventArgs = new AutomationEventArgs(eventId);
                    //Debug.WriteLine(string.Format(CultureInfo.CurrentCulture, "Firing {0} for {1}", eventId, hwnd), "NativeMsaaProxy");
                    AutomationInteropProvider.RaiseAutomationEvent(eventId, provider, eventArgs);
                }
            }
        }

        // fire the property change event if there is a client listening for it.
        // pattern is the pattern that the property belongs to. the provider is tested to ensure it supports that pattern.
        private void MaybeFirePropertyChangeEvent(AutomationPattern pattern, AutomationProperty property, Hashtable eventTable, IntPtr hwnd, int idObject, int idChild, bool clientToo)
        {
            // if the 2-nd level table contains an entry for this property and the root element should be included (or not)
            if (eventTable.ContainsKey(property) && (clientToo || !IsClientObject(idObject, idChild)))
            {
                // create a provider associated with this event and check whether it supports the pattern, if specified.
                MsaaNativeProvider provider = (MsaaNativeProvider)MsaaNativeProvider.Create(hwnd, idChild, idObject);
                if (provider != null && (pattern==null || provider.IsPatternSupported(pattern)))
                {
                    // get the new property value from the provider.
                    object newValue = ((IRawElementProviderSimple)provider).GetPropertyValue(property.Id);

                    // fire the event
                    AutomationPropertyChangedEventArgs eventArgs = new AutomationPropertyChangedEventArgs(property, null, newValue);
                    //Debug.WriteLine(string.Format(CultureInfo.CurrentCulture, "Firing {0} change event for {1}", property, hwnd), "NativeMsaaProxy");
                    AutomationInteropProvider.RaiseAutomationPropertyChangedEvent(provider, eventArgs);
                }
            }
        }

        // fire the structure change event if there is a client listening for it.
        private void MaybeFireStructureChangeEvent(int eventId, Hashtable eventTable, IntPtr hwnd, int idObject, int idChild)
        {
            // if the 2-nd level table contains an entry for this event and element is not the root 
            // (the host hwnd provider takes care of structure changed events for the root.)
            if (eventTable.ContainsKey(AutomationElement.StructureChangedEvent) && !IsClientObject(idObject, idChild))
            {
                // Note: src element for ChildRemoved cases needs to be the parent, but the runtime id is the child!

                // the type of structure changed event that we will fire depends on which event we are receiving.
                // the type then determines the src element -- either the parent or the child -- and the runtime id
                // -- either the parent or the child -- for the event arguments.
                IRawElementProviderFragment srcElement;
                int[] runtimeId = null;
                StructureChangeType type = StructureChangeType.ChildAdded; // Actual value is assigned below; any value will do here, to init the var
                switch(eventId)
                {
                    case NativeMethods.EVENT_OBJECT_CREATE:
                    case NativeMethods.EVENT_OBJECT_SHOW:
                        // src element is child. runtime id is child.
                        srcElement = (IRawElementProviderFragment)MsaaNativeProvider.Create(hwnd, idChild, idObject);
                        if(srcElement != null)
                        {
                            runtimeId = srcElement.GetRuntimeId();
                            type = StructureChangeType.ChildAdded;
                        }
                        break;

                    //case NativeMethods.EVENT_OBJECT_DESTROY:
                        // src element is parent. runtime id is child.

                        // There is nothing we can do in this case. Since the object is destroyed we can't instantiate
                        // it in order to get its runtime ID. Even if it had a non-zero child id such that we could
                        // instantiate its parent we still couldn't determine the runtime ID of the child that was destroyed.
                        // There's also no guarantee that an EVENT_OBJECT_DESTROY will have a corresponding EVENT_OBJECT_CREATE
                        // and even if it did it might use a different object id so we can't cache the information either. 
                        // (Trident for example uses a cyclic counter to generate object ids so the object id can vary for 
                        // the same object from one event to the next!)

                    case NativeMethods.EVENT_OBJECT_HIDE:
                        // src element is parent. runtime id is child.
                        srcElement = (IRawElementProviderFragment)MsaaNativeProvider.Create(hwnd, idChild, idObject);
                        if(srcElement != null)
                        {
                            runtimeId = srcElement.GetRuntimeId();
                            srcElement = (IRawElementProviderFragment)srcElement.Navigate(NavigateDirection.Parent);
                            type = StructureChangeType.ChildRemoved;
                        }
                        break;

                    default:
                        Debug.Assert(eventId == NativeMethods.EVENT_OBJECT_REORDER);
                        // src element is parent. runtime id is parent.
                        srcElement = (IRawElementProviderFragment)MsaaNativeProvider.Create(hwnd, idChild, idObject);
                        if(srcElement != null)
                        {
                            runtimeId = srcElement.GetRuntimeId();
                            type = StructureChangeType.ChildrenReordered;
                        }
                        break;
                }

                if (srcElement != null)
                {
                    // fire the event
                    StructureChangedEventArgs eventArgs = new StructureChangedEventArgs(type, runtimeId);
                    //Debug.WriteLine(string.Format(CultureInfo.CurrentCulture, "Firing structure change event for {0}", hwnd), "NativeMsaaProxy");
                    AutomationInteropProvider.RaiseStructureChangedEvent(srcElement, eventArgs);
                }
            }
        }

        #endregion Private Methods

        //------------------------------------------------------
        //
        //  Private Fields
        //
        //------------------------------------------------------
 
        #region Private Fields

        // the top-level of our two-level table structure. see the comments in AdviseEventsAdded for details.
        private Hashtable _hwndTable = new Hashtable();

        // table mapping hwnds to browser event listeners
        private Hashtable _browserTable = new Hashtable();

        private static MSAAEventDispatcher _dispatcher; // The global singleton event dispatcher. Access via Dispatcher property.

        #endregion Private Fields
    }
}
