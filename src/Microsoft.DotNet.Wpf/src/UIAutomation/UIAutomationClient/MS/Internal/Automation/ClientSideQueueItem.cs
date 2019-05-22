// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// Description: Class to create a queue on its own thread.

using System.Windows.Automation;
using System.Windows.Automation.Provider;
using System;
using System.Collections;
using System.ComponentModel;
using MS.Internal.Automation;
using MS.Win32;

namespace MS.Internal.Automation
{
    // Worker class used to queue events that originated on the client side (e.g.
    // used by focus and top-level window tracking to queue WinEvent information).
    internal class ClientSideQueueItem : QueueItem
    {
        //------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------
 
        #region Constructors

        internal ClientSideQueueItem(Delegate clientCallback, AutomationElement srcEl, UiaCoreApi.UiaCacheRequest request, AutomationEventArgs e)
        {
            _clientCallback = clientCallback;
            _srcEl = srcEl;
            _request = request;
            _e = e;
        } 

        #endregion Constructors


        //------------------------------------------------------
        //
        //  Internal Methods
        //
        //------------------------------------------------------
 
        #region Internal Methods

        internal override void Process()
        {
            // Grab properties for cache request here...
            AutomationElement src;
            if (_srcEl == null)
            {
                src = null;
            }
            else
            {
                UiaCoreApi.UiaCacheResponse response = UiaCoreApi.UiaGetUpdatedCache(_srcEl.RawNode, _request, UiaCoreApi.NormalizeState.View, null);
                src = CacheHelper.BuildAutomationElementsFromResponse(_request, response);
            }

            // We need to find out why this situation should occur at (aside from a window closed event) and
            // handle the cause.
            if (!(src == null && _e.EventId == AutomationElement.AutomationFocusChangedEvent))
                InvokeHandlers.InvokeClientHandler(_clientCallback, src, _e);
        }

        #endregion Internal Methods


        //------------------------------------------------------
        //
        //  Private Fields
        //
        //------------------------------------------------------
 
        #region Private Fields

        private Delegate _clientCallback;       // a client callback delegate
        private AutomationElement _srcEl;     // the source element
        private UiaCoreApi.UiaCacheRequest _request; // shopping list for prefetch
        private AutomationEventArgs _e;       // the event args for the callback

        #endregion Private Fields
    }
}
