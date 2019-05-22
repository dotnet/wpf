// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// Description: This class wraps an event listener object on the client side.

using System;
using System.Diagnostics;
using System.Windows.Automation;
using System.ComponentModel;    // needed by AsyncOperation
using System.Runtime.InteropServices;
using MS.Win32;

namespace MS.Internal.Automation
{
    // This class wraps an event listener object on the client side.
    internal class EventListenerClientSide : MarshalByRefObject
    {
        //------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------

        #region Constructors

        internal EventListenerClientSide(AutomationElement elRoot, Delegate clientCallback, EventListener l)
        {
            _eventListener = l;
            _refElement = elRoot;
            // Ensure that RuntimeId is cached on elRoot so that later compares can be done w/o accessing the native element
            _refRid = elRoot.GetRuntimeId();
            _clientCallback = clientCallback;
            _callbackDelegate = new UiaCoreApi.UiaEventCallback(OnEvent);
            _gch = GCHandle.Alloc(_callbackDelegate);

            // Note: currently we don't have a well-defined lifetime for the usage of the callback, so don't
            // really know when to release the GCHandle. Really nead the other event classes to call back to
            // us when they're done.
            // For now, just leave _gch to be collected when this object itself is collected. Note that we
            // don't have a finalizer, since that is only for finalizing non-CLR entities; the GCHandle should
            // take care of itself.
        }

        #endregion Constructors

        //------------------------------------------------------
        //
        //  Internal Methods
        //
        //------------------------------------------------------

        #region Internal Methods

        // called when unhooking the event - release resources
        internal void Dispose()
        {
            _gch.Free();
        }

        // Unmanaged DLL calls back on this to notify a UIAccess client of an event.
        internal void OnEvent(IntPtr argsAddr, object[,] requestedData, string treeStructure)
        {
            AutomationEventArgs e = UiaCoreApi.GetUiaEventArgs(argsAddr);
            if (e.EventId == AutomationElement.AutomationFocusChangedEvent)
            {
                uint eventTime = SafeNativeMethods.GetTickCount();
                if (eventTime == 0) // 0 is used as a marker value, so bump up to 1 if we get it.
                    eventTime = 1;

                // There's no FocusChangedEventArgs in core, but clients expect one, so substitute if needed...
                // (otherwise the cast in InvokeHandlers will fail...)
                e = new InternalAutomationFocusChangedEventArgs(0, 0, eventTime);
            }
            UiaCoreApi.UiaCacheResponse cacheResponse = new UiaCoreApi.UiaCacheResponse(requestedData, treeStructure, _eventListener.CacheRequest);
            // Invoke the listener's callback but not on this thread.  Queuing this onto a worker thread allows
            // OnEvent to return (which allows the call on the server-side to complete) and avoids a deadlock
            // situation when the client accesses properties on the source element.
            ClientEventManager.CBQ.PostWorkItem(new CalloutQueueItem(_clientCallback, cacheResponse, e, _eventListener.CacheRequest));
        }


        // IsListeningFor - called by UIAccess client during removal of listeners. Returns
        // true if rid, eventId and clientCallback represent this listener instance.
        internal bool IsListeningFor(AutomationEvent eventId, AutomationElement el, Delegate clientCallback)
        {
            // Removing the event handler using the element RuntimeId prevents problems with dead elements
            int[] rid = null;
            try
            {
                rid = el.GetRuntimeId();
            }
            catch( ElementNotAvailableException )
            {
                // This can't be the element this instance is holding because when
                // creating this instance we caused the RuntimeId to be cached.
                return false;
            }

            if( !Misc.Compare( _refRid, rid ) )
                return false;

            if( _eventListener.EventId != eventId )
                return false;

            if (_clientCallback != clientCallback)
                return false;

            return true;
        }

        // WithinScope - returns true if el is within the scope of this listener.
        internal bool WithinScope(AutomationElement el)
        {
            // Quick look: If want all elements then no compare is necessary
            if ((_eventListener.TreeScope & TreeScope.Subtree) == TreeScope.Subtree &&
                 Misc.Compare(_refRid, AutomationElement.RootElement.GetRuntimeId()))
            {
                return true;
            }


            // If our weak reference is still alive, then get it
            AutomationElement elThis = AutomationElement;
            if (elThis == null)
            {
                return false;   // reference is no longer alive
            }

            // Quick look: If they want this element
            if ((_eventListener.TreeScope & TreeScope.Element) != 0 && Misc.Compare(el, elThis))
            {
                return true;
            }

            AutomationElement elParent;

            // Quick look (sort of): If they want to include children
            if (((_eventListener.TreeScope & TreeScope.Children) != 0 || (_eventListener.TreeScope & TreeScope.Descendants) != 0))
            {
                elParent = TreeWalker.RawViewWalker.GetParent(el);
                if (elParent != null && Misc.Compare(elParent, elThis))
                    return true;
            }

            // Quick look (sort of): If they want to include the parent
            if (((_eventListener.TreeScope & TreeScope.Parent) != 0 || (_eventListener.TreeScope & TreeScope.Ancestors) != 0))
            {
                elParent = TreeWalker.RawViewWalker.GetParent(elThis);
                if (elParent != null && Misc.Compare(elParent, el))
                    return true;
            }

            // More work if they want to include any descendents of this element
            if ((_eventListener.TreeScope & TreeScope.Descendants) != 0 && IsChildOf(elThis, el))
            {
                return true;
            }

            // More work if they want to include any anscestors of this element
            if ((_eventListener.TreeScope & TreeScope.Ancestors) != 0 && IsChildOf(el, elThis))
            {
                return true;
            }

            return false;
        }

        // WithinScope - returns true if rid is the RuntimeId of this listener or listening for all elements.
        internal bool WithinScope( int [] rid )
        {
            // Quick look: If want all elements then no compare is necessary
            if ((_eventListener.TreeScope & TreeScope.Subtree) == TreeScope.Subtree &&
                 Misc.Compare(_refRid, AutomationElement.RootElement.GetRuntimeId()))
            {
                return true;
            }

            // Can only determine if ref element is the element using RuntimeId;
            // can't determine other relationships.
            if ( ( _eventListener.TreeScope & TreeScope.Element ) == 0 )
            {
                return false;
            }

            // Quick look: If they want this element but use our ref RuntimeId
            // since the weak reference may be gone.
            if ( Misc.Compare( rid, _refRid ) )
            {
                return true;
            }

            // rid is not the ref element
            return false;
        }
        #endregion Internal Methods


        //------------------------------------------------------
        //
        //  Internal Properties
        //
        //------------------------------------------------------

        #region Internal Properties

        internal EventListener EventListener
        {
            get
            {
                return _eventListener;
            }
        }

        internal Delegate ClientCallback
        {
            get
            {
                return _clientCallback;
            }
        }

        internal AutomationElement AutomationElement
        {
            get
            {
                return _refElement;
            }
        }

        internal UiaCoreApi.UiaEventCallback CallbackDelegate
        {
            get
            {
                return _callbackDelegate;
            }
        }

        internal SafeEventHandle EventHandle
        {
            get
            {
                return _eventHandle;
            }

            set
            {
                _eventHandle = value;
            }
        }
        #endregion Internal Properties


        //------------------------------------------------------
        //
        //  Private Methods
        //
        //------------------------------------------------------

        #region Private Methods

        // return true if el is a child of elPossibleParent
        private bool IsChildOf(AutomationElement elPossibleParent, AutomationElement el)
        {
            // Do the work [slower] using the proxies
            if( ! Misc.Compare( el, elPossibleParent ) )
            {
                AutomationElement elPossibleChild = TreeWalker.RawViewWalker.GetParent(el);
                while( elPossibleChild != null )
                {
                    if( Misc.Compare( elPossibleChild, elPossibleParent ) )
                    {
                        return true;
                    }
                    elPossibleChild = TreeWalker.RawViewWalker.GetParent(elPossibleChild);
                }
            }
            return false;
        }

        #endregion Private Methods


        //------------------------------------------------------
        //
        //  Private Fields
        //
        //------------------------------------------------------

        #region Private Fields

        private EventListener _eventListener;
        private AutomationElement _refElement;
        private int [] _refRid;
        private Delegate _clientCallback;
        private UiaCoreApi.UiaEventCallback _callbackDelegate;
        private GCHandle _gch; // GCHandle to keep GCs from moving the callback
        private SafeEventHandle _eventHandle;

        #endregion Private Fields
    }
}
