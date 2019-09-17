// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// Description:
// EventListener internal class holds the event data for both client and server sides

using System;
using System.Windows.Automation;

namespace MS.Internal.Automation
{
    // internal class holds the event data for both client and server sides
    internal class EventListener
    {
        //------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------
 
        #region Constructors

        // full ctor
        internal EventListener(
            AutomationEvent eventId, 
            TreeScope scope, 
            AutomationProperty [] properties,
            UiaCoreApi.UiaCacheRequest cacheRequest
            )
        {
            _eventId = eventId;
            _scope = scope;
            if (properties != null)
                _properties = (AutomationProperty[])properties.Clone();
            else
                _properties = null;
            _cacheRequest = cacheRequest;
        }

        #endregion Constructors

        //------------------------------------------------------
        //
        //  Internal Properties
        //
        //------------------------------------------------------
 
        #region Internal Properties

        // returns the id of the event this listener represents.
        internal AutomationEvent EventId
        {
            get
            {
                return _eventId;
            }
        }

        // returns the array of properties being listened to for property changed events.
        internal AutomationProperty [] Properties
        {
            get
            {
                return _properties;
            }
        }

        // return scopt of this event
        internal TreeScope TreeScope
        {
            get
            {
                return _scope;
            }
        }

        // returns the list of properties, patterns, etc that we need to prefetch
        internal UiaCoreApi.UiaCacheRequest CacheRequest
        {
            get
            {
                return _cacheRequest;
            }
        }

        #endregion Internal Properties


        //------------------------------------------------------
        //
        //  Private Fields
        //
        //------------------------------------------------------
 
        #region Private Fields

        private AutomationEvent            _eventId;    // the event this listener represents
        private TreeScope                  _scope;      // fire events based on this scope
        private AutomationProperty []      _properties; // for property change, indicates the properties we're listening for
        private UiaCoreApi.UiaCacheRequest _cacheRequest; // properties etc to prefetch

        #endregion Private Fields
    }
}
