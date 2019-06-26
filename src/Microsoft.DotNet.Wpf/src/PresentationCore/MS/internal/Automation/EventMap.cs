// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
//
//
// Description:
// Accessibility event map classes are used to determine if, and how many
// listeners there are for events and property changes.
//
//

using System;
using System.Collections;
using System.Security;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Automation.Peers;
using System.Windows.Threading;

using SR=MS.Internal.PresentationCore.SR;
using SRID=MS.Internal.PresentationCore.SRID;

namespace MS.Internal.Automation
{
    // Manages the event map that is used to determine if there are Automation
    // clients interested in specific events.
    internal static class EventMap
    {
        private class EventInfo
        {
            internal EventInfo()
            {
                NumberOfListeners = 1;
            }

            internal int NumberOfListeners;
        }

        // Never inline, as we don't want to unnecessarily link the automation DLL.
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private static bool IsKnownLegacyEvent(int id)
        {
            if (    id == AutomationElementIdentifiers.ToolTipOpenedEvent.Id
                ||  id == AutomationElementIdentifiers.ToolTipClosedEvent.Id
                ||  id == AutomationElementIdentifiers.MenuOpenedEvent.Id
                ||  id == AutomationElementIdentifiers.MenuClosedEvent.Id
                ||  id == AutomationElementIdentifiers.AutomationFocusChangedEvent.Id
                ||  id == InvokePatternIdentifiers.InvokedEvent.Id
                ||  id == SelectionItemPatternIdentifiers.ElementAddedToSelectionEvent.Id
                ||  id == SelectionItemPatternIdentifiers.ElementRemovedFromSelectionEvent.Id
                ||  id == SelectionItemPatternIdentifiers.ElementSelectedEvent.Id
                ||  id == SelectionPatternIdentifiers.InvalidatedEvent.Id
                ||  id == TextPatternIdentifiers.TextSelectionChangedEvent.Id
                ||  id == TextPatternIdentifiers.TextChangedEvent.Id
                ||  id == AutomationElementIdentifiers.AsyncContentLoadedEvent.Id
                ||  id == AutomationElementIdentifiers.AutomationPropertyChangedEvent.Id
                ||  id == AutomationElementIdentifiers.StructureChangedEvent.Id
                ||  id == SynchronizedInputPatternIdentifiers.InputReachedTargetEvent?.Id
                ||  id == SynchronizedInputPatternIdentifiers.InputReachedOtherElementEvent?.Id
                ||  id == SynchronizedInputPatternIdentifiers.InputDiscardedEvent?.Id)
            {
                return true;
            }

            return false;
        }

        // Never inline, as we don't want to unnecessarily link the automation DLL.
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private static bool IsKnownNewEvent(int id)
        {
            if (id == AutomationElementIdentifiers.LiveRegionChangedEvent?.Id)
            {
                return true;
            }
            return false;
        }

        // Never inline, as we don't want to unnecessarily link the automation DLL.
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private static bool IsKnownEvent(int id)
        {
            if (IsKnownLegacyEvent(id) ||
                (!AccessibilitySwitches.UseNetFx47CompatibleAccessibilityFeatures && IsKnownNewEvent(id)))
            {
                return true;
            }
            return false;
        }

        // Never inline, as we don't want to unnecessarily link the automation DLL.
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private static AutomationEvent GetRegisteredEventObjectHelper(AutomationEvents eventId)
        {
            AutomationEvent eventObject = null;

            switch(eventId)
            {
                case AutomationEvents.ToolTipOpened:                                        eventObject = AutomationElementIdentifiers.ToolTipOpenedEvent; break;
                case AutomationEvents.ToolTipClosed:                                        eventObject = AutomationElementIdentifiers.ToolTipClosedEvent; break;
                case AutomationEvents.MenuOpened:                                           eventObject = AutomationElementIdentifiers.MenuOpenedEvent; break;
                case AutomationEvents.MenuClosed:                                           eventObject = AutomationElementIdentifiers.MenuClosedEvent; break;
                case AutomationEvents.AutomationFocusChanged:                               eventObject = AutomationElementIdentifiers.AutomationFocusChangedEvent; break;
                case AutomationEvents.InvokePatternOnInvoked:                               eventObject = InvokePatternIdentifiers.InvokedEvent; break;
                case AutomationEvents.SelectionItemPatternOnElementAddedToSelection:        eventObject = SelectionItemPatternIdentifiers.ElementAddedToSelectionEvent; break;
                case AutomationEvents.SelectionItemPatternOnElementRemovedFromSelection:    eventObject = SelectionItemPatternIdentifiers.ElementRemovedFromSelectionEvent; break;
                case AutomationEvents.SelectionItemPatternOnElementSelected:                eventObject = SelectionItemPatternIdentifiers.ElementSelectedEvent; break;
                case AutomationEvents.SelectionPatternOnInvalidated:                        eventObject = SelectionPatternIdentifiers.InvalidatedEvent; break;
                case AutomationEvents.TextPatternOnTextSelectionChanged:                    eventObject = TextPatternIdentifiers.TextSelectionChangedEvent; break;
                case AutomationEvents.TextPatternOnTextChanged:                             eventObject = TextPatternIdentifiers.TextChangedEvent; break;
                case AutomationEvents.AsyncContentLoaded:                                   eventObject = AutomationElementIdentifiers.AsyncContentLoadedEvent; break;
                case AutomationEvents.PropertyChanged:                                      eventObject = AutomationElementIdentifiers.AutomationPropertyChangedEvent; break;
                case AutomationEvents.StructureChanged:                                     eventObject = AutomationElementIdentifiers.StructureChangedEvent; break;
                case AutomationEvents.InputReachedTarget:                                   eventObject = SynchronizedInputPatternIdentifiers.InputReachedTargetEvent; break;
                case AutomationEvents.InputReachedOtherElement:                             eventObject = SynchronizedInputPatternIdentifiers.InputReachedOtherElementEvent; break;
                case AutomationEvents.InputDiscarded:                                       eventObject = SynchronizedInputPatternIdentifiers.InputDiscardedEvent; break;
                case AutomationEvents.LiveRegionChanged:                                    eventObject = AutomationElementIdentifiers.LiveRegionChangedEvent; break;

                default:
                    throw new ArgumentException(SR.Get(SRID.Automation_InvalidEventId), "eventId");
            }

            if ((eventObject != null) && (!_eventsTable.ContainsKey(eventObject.Id)))
            {
                eventObject = null;
            }

            return (eventObject);
        }

        internal static void AddEvent(int idEvent)
        {
            //  to avoid unbound memory allocations,
            //  register only events that we recognize
            if (IsKnownEvent(idEvent))
            {
                bool firstEvent = false;
                lock (_lock)
                {
                    if (_eventsTable == null)
                    {
                        _eventsTable = new Hashtable(20, .1f);
                        firstEvent = true;
                    }

                    if (_eventsTable.ContainsKey(idEvent))
                    {
                        EventInfo info = (EventInfo)_eventsTable[idEvent];
                        info.NumberOfListeners++;
                    }
                    else
                    {
                        _eventsTable[idEvent] = new EventInfo();
                    }
                }

                // notify PresentationSources (outside the lock) when the number
                // of listeners becomes non-zero
                if (firstEvent)
                {
                    NotifySources();
                }
            }
        }

        internal static void RemoveEvent(int idEvent)
        {
            lock (_lock)
            {
                if (_eventsTable != null)
                {
                    // Decrement the count of listeners for this event
                    if (_eventsTable.ContainsKey(idEvent))
                    {
                        EventInfo info = (EventInfo)_eventsTable[idEvent];

                        // Update or remove the entry based on remaining listeners
                        info.NumberOfListeners--;
                        if (info.NumberOfListeners <= 0)
                        {
                            _eventsTable.Remove(idEvent);

                            // If no more entries exist kill the table
                            if (_eventsTable.Count == 0)
                            {
                                _eventsTable = null;
                            }
                        }
                    }
                }
            }
        }

        //  Unlike GetRegisteredEvent below,
        //  HasRegisteredEvent does NOT cause automation DLLs loading
        internal static bool HasRegisteredEvent(AutomationEvents eventId)
        {
            lock (_lock)
            {
                if (_eventsTable != null && _eventsTable.Count != 0)
                {
                    return (GetRegisteredEventObjectHelper(eventId) != null);
                }
            }
            return (false);
        }

        internal static AutomationEvent GetRegisteredEvent(AutomationEvents eventId)
        {
            lock (_lock)
            {
                if (_eventsTable != null && _eventsTable.Count != 0)
                {
                    return (GetRegisteredEventObjectHelper(eventId));
                }
            }

            return (null);
        }

        internal static bool HasListeners
        {
            get { return (_eventsTable != null); }
        }

        // Most automation clients send WM_GETOBJECT messages to our hwnd(s).
        // We rely on these to add the top-level peers to the LayoutManager's
        // AutomationEvents list, so that the automation tree stays in sync with
        // the visual tree.  But some "clients" merely add WinEvent hooks to
        // intercept automation messages, and don't send WM_GETOBJECT messages.
        // This can lead to crashes (with System.Windows.Automation.ElementNotAvailableException) as follows:
        //  1. external process installs a WinEvent hook
        //  2. WPF app opens a popup.  PopupSecurityHelper.ForceMsaaToUiaBridge
        //      detects the hook, creates a peer, informs uiacore.
        //  3. uiacore registers for automation events, calling EventMap.AddEvent
        //  4. elements throughout the app (on any thread) create peers, thinking
        //      that there is interest in the relevant automation events
        //      (EventMap.HasRegisteredEvent returns true).
        //  5. after visual tree changes, the automation tree is not fixed up fully
        //  6. some automation code uses outdated information and throws
        //      an exception or makes bad decisions that lead to problems later
        // [In some cases, TreeViewItems get recycled to display different data,
        // but the corresponding TreeViewItemAutomationPeers don't always get
        // fixed up to refer to a different EventSource (TreeViewDataItemAutomationPeer).
        // A subsequent UpdatePeer (induced by changing IsEnabled) walks down
        // the wrong path, finds a stale data item peer, and throws ElementNotFoundException.]
        //
        // To mitigate this, ensure that all top-level peers get added to the
        // AutomationEvents list whenever there are any event listeners.  This
        // has two parts:
        //  a. new top-level elements (HwndSource.RootVisual) check for listeners
        //      and add themselves to the list
        //  b. when the listener count becomes non-zero, add existing top-level
        //      elements to the list
        // This strategy is much cheaper than checking something each time an
        // element creates a peer (as in (4) above), but it will create a full
        // automation tree for all windows, even those that don't have any
        // elements that check for events.  However, that's just what would
        // happen in the presence of a full automation client that sends
        // WM_GETOBJECT, so it's a cost we're already paying in the "normal" case.
        //
        // The following methods implement (b).   See <see cref="HwndSource.RootVisual"/> for (a).

        private static void NotifySources()
        {
            foreach (PresentationSource source in PresentationSource.CriticalCurrentSources)
            {
                if (!source.IsDisposed)
                {
                    source.Dispatcher.BeginInvoke(DispatcherPriority.Normal,
                                                  new DispatcherOperationCallback(NotifySource),
                                                  new object[]{source});
                }
            }
        }

        private static object NotifySource(Object args)
        {
            object[] argsArray = (object[])args;
            PresentationSource source = argsArray[0] as PresentationSource;
            if (source != null && !source.IsDisposed)
            {
                // setting the RootVisual to itself triggers the logic to
                // add to the AutomationEvents list
                source.RootVisual = source.RootVisual;
            }
            return null;
        }

        private static Hashtable _eventsTable;        // key=event id, data=listener count
        private readonly static object _lock = new object();
    }
}
