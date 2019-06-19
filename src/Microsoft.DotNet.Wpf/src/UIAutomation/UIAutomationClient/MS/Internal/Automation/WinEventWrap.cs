// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// Description: Lightweight class to wrap Win32 WinEvents.

// PRESHARP: In order to avoid generating warnings about unkown message numbers and unknown pragmas.
#pragma warning disable 1634, 1691

using System;
using System.Security;
using System.Collections;
using System.Runtime.InteropServices;
using System.ComponentModel;
using System.Diagnostics;
using MS.Win32;

namespace MS.Internal.Automation
{
    // Lightweight class to wrap Win32 WinEvents.  Users of this class would
    // inherit from WinEventWrap do the following:
    //   1. Call the base constructor with a params array of event identifiers
    //   2. Override WinEventProc to provide an implementation.
    internal class WinEventWrap
    {
        //------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------
 
        #region Constructors

        // ctor that takes an array of events
        internal WinEventWrap(int [] eventIds) 
        { 
            Debug.Assert(eventIds != null && eventIds.Length > 0, "eventIds is invalid");
            _eventIds = (int [])eventIds.Clone();
            _hHooks = new IntPtr[_eventIds.Length];
            Init();
        }

        ~WinEventWrap()
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

        internal virtual void WinEventProc(int eventId, IntPtr hwnd, int idObject, int idChild, uint eventTime)
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

        internal void AddCallback(object clientCallback)
        {
            lock (this)
            {
                _clientCallbacks.Add(clientCallback);
            }
        }

        internal bool RemoveCallback(object clientCallback)
        {
            if (clientCallback == null)
                return true;    // temp until cleanup of WinEvent code is complete

            bool listIsEmpty = true;
            lock (this)
            {
                if (_clientCallbacks.Count == 0)
                    return true;

                // remove a specific callback
                _clientCallbacks.Remove(clientCallback);
                listIsEmpty = (_clientCallbacks.Count == 0);
            }

            return listIsEmpty;
        }

        // install WinEvent hook and start getting the callback.
        internal void StartListening()
        {
            _fBusy = true;

            int i = 0;
            foreach (int eventId in _eventIds)
            {
                // There is no indication in the Windows SDK documentation that SetWinEventHook()
                // will set an error to be retrieved with GetLastError, so set the pragma to ignore
                // the PERSHARP warning.
#pragma warning suppress 6523
                _hHooks[i] = UnsafeNativeMethods.SetWinEventHook(eventId, eventId, IntPtr.Zero, _winEventProc, 0, 0, _fFlags);
                if (_hHooks[i] == IntPtr.Zero)
                {
                    StopListening();
                    throw new Win32Exception();
                }
                i++;
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
                    // There is no indication in the Windows SDK documentation that UnhookWinEvent()
                    // will set an error to be retrieved with GetLastError, so set the pragma to ignore
                    // the PERSHARP warning.
#pragma warning suppress 6523
                    UnsafeNativeMethods.UnhookWinEvent(_hHooks[i]);
                    _hHooks[i] = IntPtr.Zero;
                }
            }
            if (_qEvents != null)
            {
                _qEvents.Clear();
            }
            _fBusy = false;
        }

        // Handlers may make a call to another process so don't want to lock around code that protects _clientCallbacks.  
        // Instead, grab the callbacks w/in a lock then call them outside of the lock.  This technique has potential for
        // error if (for instance) handler A could remove both itself and handler B but for now don't need to worry
        // about handlers getting out of sync with the ones in the master array because the callbacks are defined w/in
        // this code and the handler for new UI doesn't remove itself (or anyone else).
        internal object[] GetHandlers()
        {
            lock (this)
            {
                object[] handlers = new object[_clientCallbacks.Count];
                for (int i = 0; i < _clientCallbacks.Count; i++)
                    handlers[i] = _clientCallbacks[i];

                return handlers;
            }
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
            if ( _fBusy )
            {
                _qEvents.Enqueue(new WinEvent(eventId, hwnd, idObject, idChild, eventTime));
            }
            else
            {
                _fBusy = true;
                try
                {
                    PreWinEventProc(eventId, hwnd, idObject, idChild, eventTime); // deliver this event
                }
                catch( Exception e )
                {
                    if( Misc.IsCriticalException( e ) )
                        throw;

                    // ignore exceptions for now since we've no way to let clients add exception handlers
                }

                while (_qEvents.Count > 0)
                {
                    WinEvent e = (WinEvent)_qEvents.Dequeue(); // process queued events
                    try
                    {
                        PreWinEventProc(e._eventId, e._hwnd, e._idObject, e._idChild, e._eventTime);
                    }
                    catch( Exception ex )
                    {
                        if( Misc.IsCriticalException( ex ) )
                            throw;

                        // ignore exceptions for now since we've no way to let clients add exception handlers
                    }
                }
                _fBusy = false;
            }
        }

        private void PreWinEventProc(int eventId, IntPtr hwnd, int idObject, int idChild, uint eventTime)
        {
            // Ignore events from the UIA->MSAA bridge: these are recognizable as having
            // >0 idObject, and the target HWND having a UIA impl.
            if (idObject > 0)
            {
                if (UiaCoreApi.UiaHasServerSideProvider(hwnd))
                {
                    // Bridge event - ignore it.
                    return;
                }
            }

            // 0 is used as a marker value elsewhere, so bump up to 1
            if(eventTime == 0)
            {
                eventTime = 1;
            }
            WinEventProc(eventId, hwnd, idObject, idChild, eventTime);
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


        //------------------------------------------------------
        //
        //  Private Fields
        //
        //------------------------------------------------------
 
        #region Private Fields

        private class WinEvent
        {
            internal WinEvent(int eventId, IntPtr hwnd, int idObject, int idChild, uint eventTime)
            {
                _eventId = eventId;
                _hwnd = hwnd;
                _idObject = idObject;
                _idChild = idChild;
                _eventTime = eventTime;
            }
            public int _eventId;
            public IntPtr _hwnd;
            public int _idObject;
            public int _idChild;
            public uint _eventTime;
        }
 
        private Queue _qEvents;                // Queue of events waiting to be processed
        private int [] _eventIds;              // the WinEvent(s) this instance is handling
        private IntPtr [] _hHooks;             // the returned handles(s) from SetWinEventHook
        private bool _fBusy;                   // Flag indicating if we're busy processing
        private int _fFlags;                   // SetWinEventHook flags
        private GCHandle _gchThis;             // GCHandle to keep GCs from moving this callback
        private NativeMethods.WinEventProcDef _winEventProc; // the callback handed to USER for WinEvents
        protected ArrayList _clientCallbacks;  // the client callback interface objects

        #endregion Private Fields
    }
}
