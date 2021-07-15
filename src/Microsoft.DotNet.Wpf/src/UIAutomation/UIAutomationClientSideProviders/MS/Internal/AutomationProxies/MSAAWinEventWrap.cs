// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// Description: Lightweight class to wrap Win32 WinEvents.
//
// THIS IS VIRTUALLY AN IDENTICAL COPY TO A FILE OF THE SAME NAME IN UIAUTOMATION!
// ANY CHANGES MADE IN THIS FILE SHOUDL BE PROPAGATED TO THE OTHER FILE.

using System;
using System.Collections;
using System.Runtime.InteropServices;
using MS.Win32;

namespace MS.Internal.AutomationProxies
{
    // Lightweight class to wrap Win32 WinEvents.  Users of this class would
    // inherit from MSAAWinEventWrap do the following:
    //   1. Call the base constructor with a params array of event identifiers
    //   2. Override WinEventProc to provide an implementation.
    internal class MSAAWinEventWrap
    {
        //------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------
 
        #region Constructors

        // ctor that takes a range of events
        internal MSAAWinEventWrap(int eventMin, int eventMax)
        {
            _eventMin = eventMin;
            _eventMax = eventMax;
            _hHooks = new IntPtr[1];
            Init();
        }

        ~MSAAWinEventWrap()
        {
            Clear();
        }

        #endregion Constructors


        //------------------------------------------------------
        //
        //  Internal Methods
        //
        //------------------------------------------------------
 
        #region Internal Methods

        internal virtual void WinEventProc(int eventId, IntPtr hwnd, int idObject, int idChild)
        {
            // override this to provide an implementation
        }

        internal void Clear()
        {
            StopListening();
            lock(this)
            {
                _clientCallbacks.Clear ();
            }
            if (_gchThis.IsAllocated)
            {
                _gchThis.Free();
            }
        }

        internal void StartListening()
        {
            _fBusy = true;

            {
                // in a single hook, listen for a range of WinEvent types
                _hHooks[0] = Misc.SetWinEventHook(_eventMin, _eventMax, IntPtr.Zero, _winEventProc, 0, 0, _fFlags);
                if (_hHooks[0] == IntPtr.Zero)
                {
                    StopListening();
                }
            }
            _fBusy = false;
        }

        internal void StopListening()
        {
            // ASSUMPTION: Before StopListening is called, all callback delegates have been removed
            // so that any events received while hooks are being removed become noops (since there's
            // no handlers for them to call).
            _fBusy = true;

            for (int i=0;i<_hHooks.Length;i++)
            {
                if (_hHooks[i] != IntPtr.Zero)
                {
                    Misc.UnhookWinEvent(_hHooks[i]);
                    _hHooks[i] = IntPtr.Zero;
                }
            }
            if (_qEvents != null)
            {
                _qEvents.Clear();
            }
            _fBusy = false;
        }

        #endregion Internal Methods


        //------------------------------------------------------
        //
        //  Private Methods
        //
        //------------------------------------------------------
 
        #region Private Methods

        // queue winevents so that the get processed in the order we receive them. If we just
        // dispatch them as we get them, we'll end up getting some _while_ we are processing others,
        // and end up completing those events out of order, making the event order appear backwards.
        // This code checks whether we are currently processing an event, and if so, queues it so that
        // we process it when we're done with the current event.
        private void WinEventReentrancyFilter(int winEventHook, int eventId, IntPtr hwnd, int idObject, int idChild, int eventThread, uint eventTime)
        {
            if (_fBusy)
            {
                _qEvents.Enqueue(new WinEvent(eventId, hwnd, idObject, idChild));
            }
            else
            {
                _fBusy = true;
                try
                {
                    WinEventProc( eventId, hwnd, idObject, idChild ); // deliver this event
                }
                catch (Exception e)
                {
                    if (Misc.IsCriticalException(e))
                    {
                        throw;
                    }

                    // ignore exceptions for now since we've no way to let clients add exception handlers
                }

                while (_qEvents.Count > 0)
                {
                    WinEvent winEvent = (WinEvent)_qEvents.Dequeue(); // process queued events
                    try
                    {
                        WinEventProc(winEvent._eventId, winEvent._hwnd, winEvent._idObject, winEvent._idChild);
                    }
                    catch (Exception e)
                    {
                        if (Misc.IsCriticalException(e))
                        {
                            throw;
                        }

                        // ignore exceptions for now since we've no way to let clients add exception handlers
                    }
                }
                _fBusy = false;
            }
        }

        private void Init()
        {
            // Keep the garbage collector from moving things around
            _winEventProc = new NativeMethods.WinEventProcDef(WinEventReentrancyFilter);
            _gchThis = GCHandle.Alloc(_winEventProc);

            _clientCallbacks = new ArrayList(2);
            _qEvents = new Queue(16, (float)2.0); // (initial cap, growth factor)
            _fFlags = NativeMethods.WINEVENT_OUTOFCONTEXT;
        }

        #endregion Private Methods

        #region Internal Types

        // ------------------------------------------------------
        //
        // Internal Types Declaration
        //
        // ------------------------------------------------------
        // Function call to either the Start or Stop Listener
        internal delegate void StartStopDelegate();

        #endregion Internal Types

        //------------------------------------------------------
        //
        //  Private Fields
        //
        //------------------------------------------------------
 
        #region Private Fields

        private class WinEvent
        {
            internal WinEvent(int eventId, IntPtr hwnd, int idObject, int idChild)
            {
                _eventId = eventId;
                _hwnd = hwnd;
                _idObject = idObject;
                _idChild = idChild;
            }
            public int _eventId;
            public IntPtr _hwnd;
            public int _idObject;
            public int _idChild;
        }
 
        private Queue _qEvents;                // Queue of events waiting to be processed
        private int _eventMin;                 // minimum WinEvent type in range
        private int _eventMax;                 // maximium WinEventType in range
        private IntPtr [] _hHooks;             // the returned handles(s) from SetWinEventHook
        private bool _fBusy;                   // Flag indicating if we're busy processing
        private int _fFlags;                   // SetWinEventHook flags
        private GCHandle _gchThis;             // GCHandle to keep GCs from moving this callback
        private NativeMethods.WinEventProcDef _winEventProc; // the callback handed to USER for WinEvents
        protected ArrayList _clientCallbacks;  // the client callback interface objects

        #endregion Private Fields
    }
}
