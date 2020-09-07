// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;
using MS.Utility;


namespace System.Windows
{
    // Container for the class event handlers
    // ClassHandlersStore constitues lists of
    // RoutedEventHandlerInfo keyed on the 
    // RoutedEvent
    internal class ClassHandlersStore
    {
        #region Construction
        
        // Constructor for ClassHandlersStore
        internal ClassHandlersStore(int size)
        {
            _eventHandlersList = new ItemStructList<ClassHandlers>(size);
        }
        
        #endregion Construction

        #region Operations

        // Adds a routed event handler at the given index of the store
        // Returns updated set of handlers
        // NOTE: index must be valid, i.e. not -1
        internal RoutedEventHandlerInfoList AddToExistingHandlers(
            int index,
            Delegate handler,
            bool handledEventsToo)
        {
            Debug.Assert(index != -1, "There should exist a set of handlers for the given routedEvent");

            // Create a new RoutedEventHandler
            RoutedEventHandlerInfo routedEventHandlerInfo = 
                new RoutedEventHandlerInfo(handler, handledEventsToo);            

            // Check if we need to create a new node in the linked list
            RoutedEventHandlerInfoList handlers =  _eventHandlersList.List[index].Handlers;
            if (handlers == null || _eventHandlersList.List[index].HasSelfHandlers == false)
            {
                // Create a new node in the linked list of class 
                // handlers for this type and routed event.
                handlers = new RoutedEventHandlerInfoList();
                handlers.Handlers = new RoutedEventHandlerInfo[1];
                handlers.Handlers[0] = routedEventHandlerInfo;
                handlers.Next = _eventHandlersList.List[index].Handlers;
                _eventHandlersList.List[index].Handlers = handlers;
                _eventHandlersList.List[index].HasSelfHandlers = true;
            }
            else
            {
                // Add this handler to the existing node in the linked list 
                // of class handlers for this type and routed event.
                int length = handlers.Handlers.Length;
                RoutedEventHandlerInfo[] mergedHandlers = new RoutedEventHandlerInfo[length + 1];
                Array.Copy(handlers.Handlers, 0, mergedHandlers,  0, length);
                mergedHandlers[length] = routedEventHandlerInfo;
                handlers.Handlers = mergedHandlers;
            }            

            return handlers;
         }

        // Returns EventHandlers stored at the given index in the datastructure
        // NOTE: index must be valid, i.e. not -1
        internal RoutedEventHandlerInfoList GetExistingHandlers(int index)
        {
            Debug.Assert(index != -1, "There should exist a set of handlers for the given index");

            return _eventHandlersList.List[index].Handlers;
        }        

        // Creates reference to given handlers and RoutedEvent
        // Returns the index at which the new reference was added
        // NOTE: There should not exist a set of handlers for the 
        // given routedEvent
        internal int CreateHandlersLink(RoutedEvent routedEvent, RoutedEventHandlerInfoList handlers)
        {
            Debug.Assert(GetHandlersIndex(routedEvent) == -1, "There should not exist a set of handlers for the given routedEvent");

            ClassHandlers classHandlers = new ClassHandlers();
            classHandlers.RoutedEvent = routedEvent;
            classHandlers.Handlers = handlers;
            classHandlers.HasSelfHandlers = false;
            _eventHandlersList.Add(classHandlers);

            return _eventHandlersList.Count - 1;
        }        

        // Update Sub Class Handlers with the given base class listeners
        // NOTE : Do not wastefully try to update subclass listeners when 
        // base class listeners are null
        internal void UpdateSubClassHandlers(
            RoutedEvent routedEvent,
            RoutedEventHandlerInfoList baseClassListeners)
        {
            Debug.Assert(baseClassListeners != null, "Update only when there are base class listeners to be updated");
            
            // Get the handlers index corresponding to the given RoutedEvent
            int index = GetHandlersIndex(routedEvent);
            if (index != -1)
            {
                bool hasSelfHandlers = _eventHandlersList.List[index].HasSelfHandlers;

                // Fetch the handlers for your baseType that the current node knows of
                
                RoutedEventHandlerInfoList handlers = hasSelfHandlers ? 
                    _eventHandlersList.List[index].Handlers.Next : 
                    _eventHandlersList.List[index].Handlers;

                bool needToChange = false;

                // If the current node has baseType handlers check if the baseClassListeners 
                // provided is for a super type of that baseType. If it is then you will 
                // replace the baseType handlers for the current node with the provided 
                // baseClassListeners. If the given baseClassListeners is for a sub type 
                // of the current nodes's baseType then we do not need to update the current node.
                //
                // Example: Consider the following class hierarchy A -> B -> C. 
                //
                // Now imagine that we register class handlers in the following order.
                // 1. Register class handler for A.
                // - A's linked list will be A -> NULL.
                // - B's linked list will be NULL.
                // - C's linked list will be NULL.
                // 2. Register class handler for C.
                // - A's linked list will be A -> NULL.
                // - B's linkedList will be NULL.
                // - C's linked list will be C -> A -> NULL.
                // 3. Register class handler for B.
                // - A's linked list will be A -> NULL.
                // - B's linkedList will be B -> A -> NULL.
                // - While updating C's linked list we are given B's linked list for the baseClassListers. 
                //   Now we want to check if B is a super type of A which is the current baseType that C 
                //   knows of. The contains check below determines this. Since it is we now replace C.Next 
                //   to be B. Thus we get C -> B -> A -> NULL.
                //
                // Now imagine that we register class handlers in the following order.
                // 1. Register class handler for C.
                // - A's linked list will be NULL.
                // - B's linked list will be NULL.
                // - C's linked list will be C -> NULL.
                // 2. Register class handler for B.
                // - A's linked list will be NULL.
                // - B's linkedList will be B -> NULL.
                // - While updating C's linked list we are given B's linked list for the baseClassListeners. 
                //   Since C does not know of any baseType listeners already it takes the given 
                //   baseClassListeners as is. Thus it has C -> B -> NULL
                // 3. Register class handler for A.
                // - A's linked list will be A -> NULL.
                // - B's linkedList will be B -> A -> NULL.
                // - While updating C's linked list we are given A's linked list for the baseClassListers. 
                //   Now we want to check if A is a super type of B which is the current baseType that C 
                //   knows of. The contains check below determines this. Since it isn't we do not need to 
                //   change the linked list for C. Since B's linked list has already been updated we get 
                //   C -> B - > A -> NULL.
                
                if (handlers != null)
                {
                    if (baseClassListeners.Next != null && baseClassListeners.Next.Contains(handlers))
                    {
                        needToChange = true;
                    }
                }

                // If the current node does not have any baseType handlers then if will 
                // simply use the given baseClassListeners.
                
                else
                {
                    needToChange = true;
                }

                if (needToChange)
                {
                    // If current node has self handlers then its next pointer 
                    // needs update if not the current node needs update.

                    if (hasSelfHandlers)
                    {
                        _eventHandlersList.List[index].Handlers.Next = baseClassListeners;
                    }
                    else
                    {
                        _eventHandlersList.List[index].Handlers = baseClassListeners;
                    }
                }
            }
        }        
        
        // Returns EventHandlers Index for the given RoutedEvent
        internal int GetHandlersIndex(RoutedEvent routedEvent)
        {            
            // Linear Search
            for (int i=0; i<_eventHandlersList.Count; i++)
            {
                if (_eventHandlersList.List[i].RoutedEvent == routedEvent)
                {
                    return i;
                }
            }
        
            return -1;
        }        

        #endregion Operations

        #region Data

        // Stores list of ClassHandlers keyed on RoutedEvent
        private ItemStructList<ClassHandlers> _eventHandlersList;

        #endregion Data
    }

    // Stores ClassHandlers for the given RoutedEvent
    internal struct ClassHandlers
    {
        #region Operations
        
        /// <summary>
        ///     Is the given object equals the current
        /// </summary>
        public override bool Equals(object o)
        {
            return Equals((ClassHandlers)o);
        }
        
        /// <summary>
        ///     Is the given ClassHandlers struct equals the current
        /// </summary>
        public bool Equals(ClassHandlers classHandlers)
        {
            return (
                classHandlers.RoutedEvent == this.RoutedEvent &&
                classHandlers.Handlers == this.Handlers);
        }

        /// <summary>
        ///     Serves as a hash function for a particular type, suitable for use in 
        ///     hashing algorithms and data structures like a hash table
        /// </summary>
        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        /// <summary>
        ///     Equals operator overload
        /// </summary>
        public static bool operator== (ClassHandlers classHandlers1, ClassHandlers classHandlers2)
        {
            return classHandlers1.Equals(classHandlers2);
        }

        /// <summary>
        ///     NotEquals operator overload
        /// </summary>
        public static bool operator!= (ClassHandlers classHandlers1, ClassHandlers classHandlers2)
        {
            return !classHandlers1.Equals(classHandlers2);
        }

        #endregion Operations
        
        #region Data
        
        internal RoutedEvent RoutedEvent;
        internal RoutedEventHandlerInfoList Handlers;
        internal bool HasSelfHandlers;

        #endregion Data
    }

    /// <summary>
    ///     This data-structure represents a linked list of all 
    ///     the Class Handlers for a type and its base types.
    /// </summary>
    internal class RoutedEventHandlerInfoList
    {
        internal RoutedEventHandlerInfo[] Handlers;
        internal RoutedEventHandlerInfoList Next;

        internal bool Contains(RoutedEventHandlerInfoList handlers)
        {
            RoutedEventHandlerInfoList tempHandlers = this;
            while (tempHandlers != null)
            {
                if (tempHandlers == handlers)
                {
                    return true;
                }
                
                tempHandlers = tempHandlers.Next;
            }

            return false;
        }
    }
}

