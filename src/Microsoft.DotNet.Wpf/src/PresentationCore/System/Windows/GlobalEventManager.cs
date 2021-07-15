// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections;
using System.Diagnostics;
using MS.Utility;

using SR=MS.Internal.PresentationCore.SR;
using SRID=MS.Internal.PresentationCore.SRID;

namespace System.Windows
{
    internal static class GlobalEventManager
    {
        #region Operations

        // Registers a RoutedEvent with the given details
        // NOTE: The Name must be unique within the given OwnerType
        internal static RoutedEvent RegisterRoutedEvent(
            string name,
            RoutingStrategy routingStrategy,
            Type handlerType,
            Type ownerType)
        {
            Debug.Assert(GetRoutedEventFromName(name, ownerType, false) == null, 
                                "RoutedEvent name must be unique within a given OwnerType");

            lock (Synchronized)
            {
                // Create a new RoutedEvent
                // Requires GlobalLock to access _countRoutedEvents
                RoutedEvent routedEvent = new RoutedEvent(
                    name, 
                    routingStrategy, 
                    handlerType, 
                    ownerType);
                
                // Increment the count for registered RoutedEvents
                // Requires GlobalLock to access _countRoutedEvents
                _countRoutedEvents++;

                AddOwner(routedEvent, ownerType);

                return routedEvent;
            }
        }

        // Register a Class Handler
        // NOTE: Handler Type must be the 
        // same as the one specified when 
        // registering the corresponding RoutedEvent
        internal static void RegisterClassHandler(
            Type classType,
            RoutedEvent routedEvent,
            Delegate handler,
            bool handledEventsToo)
        {
            Debug.Assert(
                typeof(UIElement).IsAssignableFrom(classType) ||
                typeof(ContentElement).IsAssignableFrom(classType) ||
                typeof(UIElement3D).IsAssignableFrom(classType), 
                "Class Handlers can be registered only for UIElement/ContentElement/UIElement3D and their sub types");
            Debug.Assert(routedEvent.IsLegalHandler(handler),
                                "Handler Type mismatch");

            ClassHandlersStore classListenersLists;
            int index;
            
            // We map the classType to a DType use DTypeMap for storage
            DependencyObjectType dType = DependencyObjectType.FromSystemTypeInternal(classType);
            
            // Get the updated EventHandlersStore for the given DType
            GetDTypedClassListeners(dType, routedEvent, out classListenersLists, out index);
            
            // Reuired to update storage
            lock (Synchronized)
            {
                // Add new routed event handler and get the updated set of handlers
                RoutedEventHandlerInfoList updatedClassListeners = 
                    classListenersLists.AddToExistingHandlers(index, handler, handledEventsToo);
                
                // Update Sub Classes
                ItemStructList<DependencyObjectType> keys = _dTypedClassListeners.ActiveDTypes;
                
                for (int i=0; i<keys.Count; i++)
                {
                    if (keys.List[i].IsSubclassOf(dType) == true)
                    {
                        classListenersLists = (ClassHandlersStore)_dTypedClassListeners[keys.List[i]];                            
                        classListenersLists.UpdateSubClassHandlers(routedEvent, updatedClassListeners);
                    }
                }
            }
        }

        // Returns a copy of the list of registered RoutedEvents
        // Returns a copy of the list so the original cannot be modified
        internal static RoutedEvent[] GetRoutedEvents()
        {
            RoutedEvent[] routedEvents;

            lock (Synchronized)
            {
                // Requires GlobalLock to access _countRoutedEvents
                routedEvents = new RoutedEvent[_countRoutedEvents];

                // Enumerate through all of the RoutedEvents in the DTypeMap
                // Requires GlobalLock to access _dTypedRoutedEventList
                ItemStructList<DependencyObjectType> keys = _dTypedRoutedEventList.ActiveDTypes;
                
                int destIndex = 0;
                for (int i=0; i<keys.Count; i++)
                {
                    FrugalObjectList<RoutedEvent> dTypedRoutedEventList = (FrugalObjectList<RoutedEvent>)_dTypedRoutedEventList[keys.List[i]];

                    for(int j = 0; j < dTypedRoutedEventList.Count; j++)
                    {
                        RoutedEvent routedEvent = dTypedRoutedEventList[j];

                        if(Array.IndexOf(routedEvents, routedEvent) < 0)
                        {
                            routedEvents[destIndex++] = routedEvent;
                        }
                    }
                }

                // Enumerate through all of the RoutedEvents in the Hashtable
                // Requires GlobalLock to access _ownerTypedRoutedEventList
                IDictionaryEnumerator htEnumerator = _ownerTypedRoutedEventList.GetEnumerator();
                
                while(htEnumerator.MoveNext() == true)
                {
                    FrugalObjectList<RoutedEvent> ownerRoutedEventList = (FrugalObjectList<RoutedEvent>)htEnumerator.Value;
                
                    for(int j = 0; j < ownerRoutedEventList.Count; j++)
                    {
                        RoutedEvent routedEvent = ownerRoutedEventList[j];
                        
                        if(Array.IndexOf(routedEvents, routedEvent) < 0)
                        {
                            routedEvents[destIndex++] = routedEvent;
                        }
                    }
                }
            }

            return routedEvents;
        }

        internal static void AddOwner(RoutedEvent routedEvent, Type ownerType)
        {
            // If the ownerType is a subclass of DependencyObject 
            // we map it to a DType use DTypeMap for storage else 
            // we use the more generic Hashtable.
            if ((ownerType == typeof(DependencyObject)) || ownerType.IsSubclassOf(typeof(DependencyObject)))
            {
                DependencyObjectType dType = DependencyObjectType.FromSystemTypeInternal(ownerType);
                
                // Get the ItemList of RoutedEvents for the given OwnerType
                // Requires GlobalLock to access _dTypedRoutedEventList
                object ownerRoutedEventListObj = _dTypedRoutedEventList[dType];
                FrugalObjectList<RoutedEvent> ownerRoutedEventList;
                if (ownerRoutedEventListObj == null)
                {
                    // Create an ItemList of RoutedEvents for the 
                    // given OwnerType if one does not already exist
                    ownerRoutedEventList = new FrugalObjectList<RoutedEvent>(1);
                    _dTypedRoutedEventList[dType] = ownerRoutedEventList;
                }
                else
                {
                    ownerRoutedEventList = (FrugalObjectList<RoutedEvent>)ownerRoutedEventListObj;
                }

                // Add the newly created 
                // RoutedEvent to the ItemList
                // Requires GlobalLock to access ownerRoutedEventList
                if(!ownerRoutedEventList.Contains(routedEvent))
                {
                    ownerRoutedEventList.Add(routedEvent);
                }
            }
            else
            {
                // Get the ItemList of RoutedEvents for the given OwnerType
                // Requires GlobalLock to access _ownerTypedRoutedEventList
                object ownerRoutedEventListObj = _ownerTypedRoutedEventList[ownerType];
                FrugalObjectList<RoutedEvent> ownerRoutedEventList;
                if (ownerRoutedEventListObj == null)
                {
                    // Create an ItemList of RoutedEvents for the 
                    // given OwnerType if one does not already exist
                    ownerRoutedEventList = new FrugalObjectList<RoutedEvent>(1);
                    _ownerTypedRoutedEventList[ownerType] = ownerRoutedEventList;
                }
                else
                {
                    ownerRoutedEventList = (FrugalObjectList<RoutedEvent>)ownerRoutedEventListObj;
                }
                
                // Add the newly created 
                // RoutedEvent to the ItemList
                // Requires GlobalLock to access ownerRoutedEventList
                if(!ownerRoutedEventList.Contains(routedEvent))
                {
                    ownerRoutedEventList.Add(routedEvent);
                }
            }
        }
        
        // Returns a RoutedEvents that match 
        // the ownerType input param
        // If not found returns null
        internal static RoutedEvent[] GetRoutedEventsForOwner(Type ownerType)
        {
            if ((ownerType == typeof(DependencyObject)) || ownerType.IsSubclassOf(typeof(DependencyObject)))
            {
                // Search DTypeMap
                DependencyObjectType dType = DependencyObjectType.FromSystemTypeInternal(ownerType);
                
                // Get the ItemList of RoutedEvents for the given DType
                FrugalObjectList<RoutedEvent> ownerRoutedEventList = (FrugalObjectList<RoutedEvent>)_dTypedRoutedEventList[dType];
                if (ownerRoutedEventList != null)
                {
                    return ownerRoutedEventList.ToArray();
                }
            }
            else // Search Hashtable
            {
                // Get the ItemList of RoutedEvents for the given OwnerType
                FrugalObjectList<RoutedEvent> ownerRoutedEventList = (FrugalObjectList<RoutedEvent>)_ownerTypedRoutedEventList[ownerType];
                if (ownerRoutedEventList != null)
                {
                    return ownerRoutedEventList.ToArray();
                }
            }
            
            // No match found
            return null;
        }

        // Returns a RoutedEvents that match 
        // the name and ownerType input params
        // If not found returns null
        internal static RoutedEvent GetRoutedEventFromName(
            string name,
            Type ownerType,
            bool includeSupers)
        {
            if ((ownerType == typeof(DependencyObject)) || ownerType.IsSubclassOf(typeof(DependencyObject)))
            {
                // Search DTypeMap
                DependencyObjectType dType = DependencyObjectType.FromSystemTypeInternal(ownerType);
                
                while (dType != null)
                {
                    // Get the ItemList of RoutedEvents for the given DType
                    FrugalObjectList<RoutedEvent> ownerRoutedEventList = (FrugalObjectList<RoutedEvent>)_dTypedRoutedEventList[dType];                
                    if (ownerRoutedEventList != null)
                    {
                        // Check for RoutedEvent with matching name in the ItemList
                        for (int i=0; i<ownerRoutedEventList.Count; i++)
                        {
                            RoutedEvent routedEvent = ownerRoutedEventList[i];
                            if (routedEvent.Name.Equals(name))
                            {
                                // Return if found match
                                return routedEvent;
                            }
                        }
                    }
                
                    // If not found match yet check for BaseType if specified to do so
                    dType = includeSupers ? dType.BaseType : null;
                }
            }
            else
            {
                // Search Hashtable
                while (ownerType != null)
                {
                    // Get the ItemList of RoutedEvents for the given OwnerType
                    FrugalObjectList<RoutedEvent> ownerRoutedEventList = (FrugalObjectList<RoutedEvent>)_ownerTypedRoutedEventList[ownerType];                
                    if (ownerRoutedEventList != null)
                    {                        
                        // Check for RoutedEvent with matching name in the ItemList
                        for (int i=0; i<ownerRoutedEventList.Count; i++)
                        {
                            RoutedEvent routedEvent = ownerRoutedEventList[i];
                            if (routedEvent.Name.Equals(name))
                            {
                                // Return if found match
                                return routedEvent;
                            }
                        }
                    }
                
                    // If not found match yet check for BaseType if specified to do so
                    ownerType = includeSupers?ownerType.BaseType : null;
                }                
            }
            
            // No match found
            return null;
        }

        // Returns the list of class listeners for the given 
        // DType and RoutedEvent
        // NOTE: Returns null if no matches found
        // Helper method for GetClassListeners
        // Invoked only when trying to build the event route
        internal static RoutedEventHandlerInfoList GetDTypedClassListeners(
            DependencyObjectType dType,
            RoutedEvent routedEvent)
        {
            ClassHandlersStore classListenersLists;
            int index;
            
            // Class Forwarded
            return GetDTypedClassListeners(dType, routedEvent, out classListenersLists, out index);
        }

        // Returns the list of class listeners for the given 
        // DType and RoutedEvent
        // NOTE: Returns null if no matches found
        // Helper method for GetClassListeners
        // Invoked when trying to build the event route 
        // as well as when registering a new class handler
        internal static RoutedEventHandlerInfoList GetDTypedClassListeners(
            DependencyObjectType dType,
            RoutedEvent routedEvent,
            out ClassHandlersStore classListenersLists,
            out int index)
        {          
            // Get the ClassHandlersStore for the given DType
            classListenersLists = (ClassHandlersStore)_dTypedClassListeners[dType];
            RoutedEventHandlerInfoList handlers;
            if (classListenersLists != null)
            {
                // Get the handlers for the given DType and RoutedEvent
                index = classListenersLists.GetHandlersIndex(routedEvent);
                if (index != -1)
                {
                    handlers = classListenersLists.GetExistingHandlers(index);
                    return handlers;
                }
            }

            lock (Synchronized)
            {
                // Search the DTypeMap for the list of matching RoutedEventHandlerInfo
                handlers = GetUpdatedDTypedClassListeners(dType, routedEvent, out classListenersLists, out index);
            }        
            
            return handlers;
        }

        // Helper method for GetDTypedClassListeners
        // Returns updated list of class listeners for the given 
        // DType and RoutedEvent
        // NOTE: Returns null if no matches found
        // Invoked when trying to build the event route 
        // as well as when registering a new class handler
        private static RoutedEventHandlerInfoList GetUpdatedDTypedClassListeners(
            DependencyObjectType dType,
            RoutedEvent routedEvent,
            out ClassHandlersStore classListenersLists,
            out int index)
        {
            // Get the ClassHandlersStore for the given DType
            classListenersLists = (ClassHandlersStore)_dTypedClassListeners[dType];
            RoutedEventHandlerInfoList handlers;
            if (classListenersLists != null)
            {
                // Get the handlers for the given DType and RoutedEvent
                index = classListenersLists.GetHandlersIndex(routedEvent);
                if (index != -1)
                {
                    handlers = classListenersLists.GetExistingHandlers(index);
                    return handlers;
                }
            }

            // Since matching handlers were not found at this level 
            // browse base classes to check for registered class handlers
            DependencyObjectType tempDType = dType;
            ClassHandlersStore tempClassListenersLists = null;
            RoutedEventHandlerInfoList tempHandlers = null;
            int tempIndex = -1;
            while (tempIndex == -1 && tempDType.Id != _dependencyObjectType.Id)
            {
                tempDType = tempDType.BaseType;
                tempClassListenersLists = (ClassHandlersStore)_dTypedClassListeners[tempDType];
                if (tempClassListenersLists != null)
                {
                    // Get the handlers for the DType and RoutedEvent
                    tempIndex = tempClassListenersLists.GetHandlersIndex(routedEvent);
                    if (tempIndex != -1)
                    {
                        tempHandlers = tempClassListenersLists.GetExistingHandlers(tempIndex);
                    }
                }
            }
        
            if (classListenersLists == null)
            {
                if (dType.SystemType == typeof(UIElement) || dType.SystemType == typeof(ContentElement))
                {
                    classListenersLists = new ClassHandlersStore(80); // Based on the number of class handlers for these classes
                }
                else
                {
                    classListenersLists = new ClassHandlersStore(1);
                }

                _dTypedClassListeners[dType] = classListenersLists;
            }

            index = classListenersLists.CreateHandlersLink(routedEvent, tempHandlers);
            
            return tempHandlers;
        }

        #endregion Operations

        #region Global Index for RoutedEvent and EventPrivateKey

        internal static int GetNextAvailableGlobalIndex(object value)
        {
            int index;
            lock (Synchronized)
            {
                // Prevent GlobalIndex from overflow. RoutedEvents are meant to be static members and are to be registered 
                // only via static constructors. However there is no cheap way of ensuring this, without having to do a stack walk. Hence 
                // concievably people could register RoutedEvents via instance methods and therefore cause the GlobalIndex to 
                // overflow. This check will explicitly catch this error, instead of silently malfuntioning.
                if (_globalIndexToEventMap.Count >= Int32.MaxValue)
                {
                    throw new InvalidOperationException(SR.Get(SRID.TooManyRoutedEvents));
                }

                index = _globalIndexToEventMap.Add(value);
            }
            return index;
        }

        // Must be called from within a lock of GlobalEventManager.Synchronized
        internal static object EventFromGlobalIndex(int globalIndex)
        {
            return _globalIndexToEventMap[globalIndex];
        }

        // must be used within a lock of GlobalEventManager.Synchronized
        private static ArrayList _globalIndexToEventMap = new ArrayList(100); // figure out what this number is in a typical scenario

        #endregion

        #region Data

        // This is an efficient  Hashtable of ItemLists keyed on DType
        // Each ItemList holds the registered RoutedEvents for that OwnerType
        private static DTypeMap _dTypedRoutedEventList = new DTypeMap(10); // Initialization sizes based on typical MSN scenario
        
        // This is a Hashtable of ItemLists keyed on OwnerType
        // Each ItemList holds the registered RoutedEvents for that OwnerType
        private static Hashtable _ownerTypedRoutedEventList = new Hashtable(10); // Initialization sizes based on typical MSN scenario

        // This member keeps a count of the total number of Routed Events registered so far
        // The member also serves as the internally used ComputedEventIndex that indexes
        // EventListenersListss that store class handler information for a class type       
        private static int _countRoutedEvents = 0;

        // This is an efficient Hashtable of ItemLists keyed on DType
        // Each ItemList holds the registered RoutedEvent class handlers for that ClassType
        private static DTypeMap _dTypedClassListeners = new DTypeMap(100); // Initialization sizes based on typical Expression Blend startup scenario

        // This is the cached value for the DType of DependencyObject
        private static DependencyObjectType _dependencyObjectType = DependencyObjectType.FromSystemTypeInternal(typeof(DependencyObject));

        internal static object Synchronized = new object();

        #endregion Data
    }
}

