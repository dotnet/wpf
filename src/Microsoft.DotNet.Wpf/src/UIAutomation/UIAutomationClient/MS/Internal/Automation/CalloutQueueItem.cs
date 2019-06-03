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
    // Worker class used to queue callbacks that came from the PAW server.  Used by
    // ClientEventManager in its event handler that recieves events from the server.
    // These events are queued in order to get them off the servers UI thread.
    internal class CalloutQueueItem : QueueItem
    {
        //------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------
 
        #region Constructors

        internal CalloutQueueItem(Delegate clientCallback, UiaCoreApi.UiaCacheResponse cacheResponse, AutomationEventArgs e, UiaCoreApi.UiaCacheRequest cacheRequest)
        {
            _clientCallback = clientCallback;
            _cacheResponse = cacheResponse;
            _e = e;
            _cacheRequest = cacheRequest;
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
            AutomationElement el = CacheHelper.BuildAutomationElementsFromResponse(_cacheRequest, _cacheResponse);
            InvokeHandlers.InvokeClientHandler(_clientCallback, el, _e);
        }

        #endregion Internal Methods


        //------------------------------------------------------
        //
        //  Private Fields
        //
        //------------------------------------------------------
 
        #region Private Fields

        internal Delegate _clientCallback;                // the client's callback delegate
        internal UiaCoreApi.UiaCacheResponse _cacheResponse;      // prefetched data (possibly including the element and properties/patterns)
        internal UiaCoreApi.UiaCacheRequest _cacheRequest; // list of items to prefetch (also used when deserializing prefetched data)
        internal AutomationEventArgs _e;               // the event args for the callback

        #endregion Private Fields
    }
}
