// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// Description: class which defines the delegates used to invoke the client event handlers

using System;
using System.Windows.Automation;
using System.ComponentModel;
using System.Threading;
using System.Diagnostics;

namespace MS.Internal.Automation
{
    // Temp class used to bundle focus event time along with the usual
    // focus change information. The InvokeHandler code below that dispatches
    // focus change events checks the time, and drops any events thta have
    // a timestamp that is earlier than the most recently dispatched event.
    // (This avoids race conditions from events from WinEvents and events from UIA,
    // which arrive on different threads, and which can drift during their processing,
    // since winevents get scope-checked - slow - as part of their processing before
    // being queued, whereas UIA events are just queued up. The key issue with any
    // timestamp checking is that it happens *after* the two streams of events have
    // been merged - otherwise they could just drift again. This is the case here,
    // since the InvokeHandler code is called by the callbackqueue, which services
    // both these types of events.)
    //
    // This code is "temp" - there's a work item to remove AsyncOperations, and
    // removing that should result in a cleaner solution to passing the eventTime
    // around, perhaps passing it directly to the InvokeHandlers method instead of
    // tunnelling it into a 'wrapped' AutomationFocusChangedEventArgs.
    internal class InternalAutomationFocusChangedEventArgs : AutomationFocusChangedEventArgs
    {
        internal AutomationFocusChangedEventArgs _args;
        internal uint _eventTime;

        internal InternalAutomationFocusChangedEventArgs(int idObject, int idChild, uint eventTime)
            : base(idObject, idChild)
        {
            _args = new AutomationFocusChangedEventArgs(idObject, idChild);
            _eventTime = eventTime;
        }
    };

    // This class manages dispatching events to the different types of
    // UIA event delegates
    internal static class InvokeHandlers
    {
        //------------------------------------------------------
        //
        //  Internal Methods
        //
        //------------------------------------------------------
 
        #region Internal Methods
        
        // The method that gets called from CallbackQueue's thread.  Uses Post to invoke the callback on the proper thread.
        internal static void InvokeClientHandler(Delegate clientCallback, AutomationElement srcEl, AutomationEventArgs args)
        {
            try
            {
                if (args is AutomationPropertyChangedEventArgs)
                {
                    ((AutomationPropertyChangedEventHandler)clientCallback)(srcEl, (AutomationPropertyChangedEventArgs)args);
                }
                else if (args is StructureChangedEventArgs)
                {
                    ((StructureChangedEventHandler)clientCallback)(srcEl, (StructureChangedEventArgs)args);
                }
                else if (args is InternalAutomationFocusChangedEventArgs)
                {
                    AutomationFocusChangedEventArgs realArgs = ((InternalAutomationFocusChangedEventArgs)args)._args;

                    // For focus events, check that the event is actually more recent than the last one (see note at top of file).
                    // Since the timestamps can wrap around, subtract and measure the delta instead of just comparing them.
                    // Any events that appear to have taken place within the 5 seconds before the last event we got will be ignored.
                    // (Because of wraparound, certain before- and after- time spans share the same deltas; 5000ms before has the
                    // same value as MAXUINT-5000ms after. Since we're just trying to filter out very recent event race conditions,
                    // confine this test to a small window, just the most recent 5 seconds. That means you'd have to wait a *very*
                    // long time without any focus changes before getting a false positive here.)
                    uint eventTime = ((InternalAutomationFocusChangedEventArgs)args)._eventTime;
                    if (_lastFocusEventTime != 0)
                    {
                        uint delta = _lastFocusEventTime - eventTime;
                        // Exclude events that happend before the last one, but do allow any that happened "at the same time",
                        // (delta==0) since they likely actually happened after, but within the resolution of the event timer.
                        if (delta < 5000 && delta != 0)
                        {
                            return;
                        }
                    }
                    _lastFocusEventTime = eventTime;
                    ((AutomationFocusChangedEventHandler)clientCallback)(srcEl, realArgs);
                }
                else
                {
                    ((AutomationEventHandler)clientCallback)(srcEl, args);
                }
            }
            catch (Exception e)
            {
                if (Misc.IsCriticalException(e))
                    throw;

                // Since we can't predict what exceptions an outside client might throw intentionally ignore all
            }
        }

        #endregion Internal Methods


        //------------------------------------------------------
        //
        //  Internal Fields
        //
        //------------------------------------------------------
 
        #region Internal Fields

        internal static uint _lastFocusEventTime;

        #endregion Internal Fields
    }
}
