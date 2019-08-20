// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;

using MS.Utility;
using MS.Internal.PresentationCore;

using SR=MS.Internal.PresentationCore.SR;
using SRID=MS.Internal.PresentationCore.SRID;

namespace System.Windows
{
    /// <summary>
    ///     Container for the event handlers
    /// </summary>
    /// <remarks>
    ///     EventHandlersStore is a hashtable 
    ///     of handlers for a given 
    ///     EventPrivateKey or RoutedEvent
    /// </remarks>
    [FriendAccessAllowed] // Built into Core, also used by Framework.
    internal class EventHandlersStore
    {
        #region Construction
        
        /// <summary>
        ///     Constructor for EventHandlersStore
        /// </summary>
        public EventHandlersStore()
        {
            _entries = new FrugalMap();
        }

        /// <summary>
        /// Copy constructor for EventHandlersStore
        /// </summary>
        public EventHandlersStore(EventHandlersStore source)
        {
            _entries = source._entries;
        }
        
        #endregion Construction

        #region ExternalAPI

        /// <summary>
        ///     Adds a Clr event handler for the 
        ///     given EventPrivateKey to the store
        /// </summary>
        /// <param name="key">
        ///     Private key for the event
        /// </param>
        /// <param name="handler">
        ///     Event handler
        /// </param>
        public void Add(EventPrivateKey key, Delegate handler)
        {
            if (key == null)
            {
                throw new ArgumentNullException("key"); 
            }
            if (handler == null)
            {
                throw new ArgumentNullException("handler"); 
            }

            // Get the entry corresponding to the given key
            Delegate existingDelegate = (Delegate)this[key];

            if (existingDelegate == null)
            {
                _entries[key.GlobalIndex] = handler;
            }
            else
            {
                _entries[key.GlobalIndex] = Delegate.Combine(existingDelegate, handler);
            }
        }


        /// <summary>
        ///     Removes an instance of the specified 
        ///     Clr event handler for the given 
        ///     EventPrivateKey from the store
        /// </summary>
        /// <param name="key">
        ///     Private key for the event
        /// </param>
        /// <param name="handler">
        ///     Event handler
        /// </param>
        /// <remarks>
        ///     NOTE: This method does nothing if no 
        ///     matching handler instances are found 
        ///     in the store
        /// </remarks>
        public void Remove(EventPrivateKey key, Delegate handler)
        {
            if (key == null)
            {
                throw new ArgumentNullException("key"); 
            }
            if (handler == null)
            {
                throw new ArgumentNullException("handler"); 
            }

            // Get the entry corresponding to the given key
            Delegate existingDelegate = (Delegate) this[key];
            if (existingDelegate != null)
            {
                existingDelegate = Delegate.Remove(existingDelegate, handler);
                if (existingDelegate == null)
                {
                    // last handler for this event was removed -- reclaim space in
                    // underlying FrugalMap by setting value to DependencyProperty.UnsetValue
                    _entries[key.GlobalIndex] = DependencyProperty.UnsetValue;
                }
                else
                {
                    _entries[key.GlobalIndex] = existingDelegate;
                }            
            }
        }

        /// <summary>
        ///     Gets all the handlers for the given EventPrivateKey
        /// </summary>
        /// <param name="key">
        ///     Private key for the event
        /// </param>
        /// <returns>
        ///     Combined delegate or null if no match found
        /// </returns>
        /// <remarks>
        ///     This method is not exposing a security risk for the reason 
        ///     that the EventPrivateKey for the events will themselves be 
        ///     private to the declaring class. This will be enforced via fxcop rules.
        /// </remarks>
        public Delegate Get(EventPrivateKey key)
        {
            if (key == null)
            {
                throw new ArgumentNullException("key"); 
            }

            // Return the handlers corresponding to the given key
            return (Delegate)this[key];
        }
        
        #endregion ExternalAPI
        
        #region Operations

        /// <summary>
        ///     Adds a routed event handler for the given 
        ///     RoutedEvent to the store
        /// </summary>
        public void AddRoutedEventHandler(
            RoutedEvent routedEvent,
            Delegate handler,
            bool handledEventsToo)
        {
            if (routedEvent == null)
            {
                throw new ArgumentNullException("routedEvent"); 
            }
            if (handler == null)
            {
                throw new ArgumentNullException("handler"); 
            }
            if (!routedEvent.IsLegalHandler(handler))
            {
                throw new ArgumentException(SR.Get(SRID.HandlerTypeIllegal));
            }
            
            // Create a new RoutedEventHandler
            RoutedEventHandlerInfo routedEventHandlerInfo = 
                new RoutedEventHandlerInfo(handler, handledEventsToo);

            // Get the entry corresponding to the given RoutedEvent
            FrugalObjectList<RoutedEventHandlerInfo> handlers = (FrugalObjectList<RoutedEventHandlerInfo>)this[routedEvent];
            if (handlers == null)
            {
                _entries[routedEvent.GlobalIndex] = handlers = new FrugalObjectList<RoutedEventHandlerInfo>(1);
            }

            // Add the RoutedEventHandlerInfo to the list
            handlers.Add(routedEventHandlerInfo);
        }

        /// <summary>
        ///     Removes an instance of the specified 
        ///     routed event handler for the given 
        ///     RoutedEvent from the store
        /// </summary>
        /// <remarks>
        ///     NOTE: This method does nothing if no 
        ///     matching handler instances are found 
        ///     in the store
        /// </remarks>
        public void RemoveRoutedEventHandler(RoutedEvent routedEvent, Delegate handler)
        {
            if (routedEvent == null)
            {
                throw new ArgumentNullException("routedEvent"); 
            }
            if (handler == null)
            {
                throw new ArgumentNullException("handler"); 
            }
            if (!routedEvent.IsLegalHandler(handler))
            {
                throw new ArgumentException(SR.Get(SRID.HandlerTypeIllegal));
            }
            
            // Get the entry corresponding to the given RoutedEvent
            FrugalObjectList<RoutedEventHandlerInfo> handlers = (FrugalObjectList<RoutedEventHandlerInfo>)this[routedEvent];
            if (handlers != null && handlers.Count > 0)
            {
                if ((handlers.Count == 1) && (handlers[0].Handler == handler))
                {
                    // this is the only handler for this event and it's being removed
                    // reclaim space in underlying FrugalMap by setting value to
                    // DependencyProperty.UnsetValue
                    _entries[routedEvent.GlobalIndex] = DependencyProperty.UnsetValue;
                }
                else
                {
                    // When a matching instance is found remove it
                    for (int i = 0; i < handlers.Count; i++)
                    {
                        if (handlers[i].Handler == handler)
                        {
                            handlers.RemoveAt(i);
                            break;
                        }
                    }
                }
            }
        }


        /// <summary>
        ///     Determines whether the given
        ///     RoutedEvent exists in the store.
        /// </summary>
        /// <param name="routedEvent">
        ///     the RoutedEvent of the event.
        /// </param>

        public bool Contains(RoutedEvent routedEvent)
        {
            if (routedEvent == null)
            {
                throw new ArgumentNullException("routedEvent"); 
            }

            FrugalObjectList<RoutedEventHandlerInfo> handlers = (FrugalObjectList<RoutedEventHandlerInfo>)this[routedEvent];

            return handlers != null && handlers.Count != 0;
        }


        
        private static void OnEventHandlersIterationCallback(ArrayList list, int key, object value)
        {
            RoutedEvent routedEvent = GlobalEventManager.EventFromGlobalIndex(key) as RoutedEvent;
            if (routedEvent != null && ((FrugalObjectList<RoutedEventHandlerInfo>)value).Count > 0)
            {
                list.Add(routedEvent);
            }
        }

        /// <summary>
        ///     Get all the event handlers in this store for the given routed event
        /// </summary>
        public RoutedEventHandlerInfo[] GetRoutedEventHandlers(RoutedEvent routedEvent)
        {
            if (routedEvent == null)
            {
                throw new ArgumentNullException("routedEvent"); 
            }

            FrugalObjectList<RoutedEventHandlerInfo> handlers = this[routedEvent];
            if (handlers != null)
            {
                return handlers.ToArray();
            }

            return null;
        }

        // Returns Handlers for the given key
        internal FrugalObjectList<RoutedEventHandlerInfo> this[RoutedEvent key]
        {            
            get
            {
                Debug.Assert(key != null, "Search key cannot be null");

                object list = _entries[key.GlobalIndex];
                if (list == DependencyProperty.UnsetValue)
                {
                    return null;
                }
                else
                {
                    return (FrugalObjectList<RoutedEventHandlerInfo>)list;
                }
            }
        }

        internal Delegate this[EventPrivateKey key]
        {
            get
            {
                Debug.Assert(key != null, "Search key cannot be null");

                object existingDelegate = _entries[key.GlobalIndex];
                if (existingDelegate == DependencyProperty.UnsetValue)
                {
                    return null;
                }
                else
                {
                    return (Delegate)existingDelegate;
                }
            }
        }

        internal int Count
        {
            get
            {
                return _entries.Count;
            }
        }
        
        #endregion Operations

        #region Data

        // Map of EventPrivateKey/RoutedEvent to Delegate/FrugalObjectList<RoutedEventHandlerInfo> (respectively)
        private FrugalMap _entries;

        private static FrugalMapIterationCallback _iterationCallback = new FrugalMapIterationCallback(OnEventHandlersIterationCallback);
        
        #endregion Data
    }
}

