// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Windows.Threading;
//using Microsoft.Test.Security.Wrappers;


namespace Microsoft.Test.Threading
{

    /// <summary>
    /// This class provide a wrapper to the dispatcher. The main
    /// reason for this wrapper is that a lot of the properties that
    /// we care are internal or private. So we do reflection to 
    /// get the values
    /// </summary>
    public class DispatcherQueueWrapper
    {

        /// <summary>
        /// Constructor that takes 1 dispatcher
        /// </summary>
        public DispatcherQueueWrapper(Dispatcher dispatcher)
        {
            if (dispatcher == null)
                throw new ArgumentNullException("dispatcher");

            _dispatcher = dispatcher;
        }


        /// <summary>
        /// Returns the Dispatcher queue. We cache the value so we don't need to 
        /// do reflection every time.
        /// </summary>
        internal object InternalQueue
        {
            get
            {
                if (_internalQueue == null)
                {
                    //TODO-Miguep:check changes from SecurityWrappers to regular reflection types
                    // Dispatcher._queue is a PriorityQueue field that holds
                    // the refence to the queue
                    object internalQueue = _dispatcher.GetType().InvokeMember("_queue",
                            BindingFlags.GetField | BindingFlags.NonPublic | BindingFlags.Instance,
                            null, _dispatcher, null);

                    _internalQueue = internalQueue;

                }

                return _internalQueue;
            }
        }


        /// <summary>
        /// Return the total amount of enqueued items
        /// </summary>
        public int Count
        {
            get
            {
                //TODO-Miguep:check changes from SecurityWrappers to regular reflection types
                // Dispatcher.Count is a PriorityQueue property that holds
                // the number of total elements on the _queue
                object intObj = InternalQueue.GetType().InvokeMember("_count",
                    BindingFlags.GetField | BindingFlags.Instance | BindingFlags.Public
                        | BindingFlags.IgnoreCase | BindingFlags.NonPublic,
                    null,
                    InternalQueue,
                    null);

                return (int)intObj;
            }
        }



        /// <summary>
        /// Returns the max priority at this time of the enqueued items
        /// </summary>
        public DispatcherPriority MaxPriority
        {
            get
            {
                //TODO-Miguep:check changes from SecurityWrappers to regular reflection types
                object priorityObj = InternalQueue.GetType().InvokeMember("MaxPriority",
                    BindingFlags.GetProperty | BindingFlags.Instance | BindingFlags.Public
                        | BindingFlags.IgnoreCase | BindingFlags.NonPublic,
                    null,
                    InternalQueue,
                    null);

                return (DispatcherPriority)priorityObj;
            }
        }


        /// <summary>
        /// Builds a list with all the PriorityChains available on the Dispatcher Queue
        /// </summary>
        public List<PriorityQueueChain> PriorityQueueChains
        {
            get
            {
                List<PriorityQueueChain> items = new List<PriorityQueueChain>();

                //TODO-Miguep:check changes from SecurityWrappers to regular reflection types
                // Retrieve all the Keys on the SortedDictionary. The dictionary is sorted
                // by DispatcherPriority
                object keysObj = priorityChains.GetType().InvokeMember(
                    "Keys",
                    BindingFlags.GetProperty | BindingFlags.Instance | BindingFlags.Public
                    | BindingFlags.IgnoreCase | BindingFlags.NonPublic,
                    null,
                    priorityChains,
                    null);

                IList<DispatcherPriority> priorityList = (IList<DispatcherPriority>)keysObj;

                if (priorityList == null)
                    throw new InvalidOperationException("PriorityChain list cannot be null");

                for (int i = 0; i < priorityList.Count; i++)
                {
                    string name = priorityList[i].ToString();

                    object[] arrayObj = { priorityList[i] };


                    //TODO-Miguep:check changes from SecurityWrappers to regular reflection types
                    object valuesObj = priorityChains.GetType().InvokeMember(
                     "get_Item",
                     BindingFlags.InvokeMethod | BindingFlags.Instance | BindingFlags.Public
                     | BindingFlags.IgnoreCase | BindingFlags.NonPublic,
                     null,
                     priorityChains,
                     arrayObj);

                    items.Add(new PriorityQueueChain(name, valuesObj));
                }


                return items;
            }
        }

        /// <summary>
        /// Provides a reference to the PriorityChain (SortedDictionary) that lives
        /// on the PriorityQueue (_queue) class.  
        /// </summary>
        private object priorityChains
        {
            get
            {
                if (_priorityChainsObj == null)
                {
                    //TODO-Miguep:check changes from SecurityWrappers to regular reflection types
                    // Dispatcher._queue._priorityChains is a SortedDictionary property that holds
                    // all the priorityChain available on the Queue
                    object priorityChainsObj = InternalQueue.GetType().InvokeMember("_priorityChains",
                        BindingFlags.GetField | BindingFlags.NonPublic | BindingFlags.Instance,
                        null,
                        InternalQueue,
                        null);

                    _priorityChainsObj = priorityChainsObj;
                }

                return _priorityChainsObj;
            }
        }

        object _priorityChainsObj = null;
        Dispatcher _dispatcher = null;
        object _internalQueue = null;

    }


    /// <summary>
    /// Wraps a PriorityChain Avalon internal API
    /// </summary>
    public class PriorityQueueChain
    {
        /// <summary>
        /// Constructor for the PriorityChain wrapper 
        /// </summary>
        /// <param name="priorityName">DispatcherPriority name for the chain</param>
        /// <param name="priorityChainObj">This is the real prioritychain object</param>
        internal PriorityQueueChain(string priorityName, object priorityChainObj)
        {
            if (priorityChainObj == null)
                throw new ArgumentNullException("priorityChainObj");

            _priorityChainObj = priorityChainObj;
            _priorityName = priorityName;
        }

        /// <summary>
        /// Retrieve the name that was passed on the constructor
        /// </summary>
        /// <value></value>
        public string Name
        {
            get
            {
                return _priorityName;
            }
        }

        /// <summary>
        /// Returns the first item from the PriorityChain
        /// </summary>
        /// <value></value>
        public object Head
        {
            get
            {
                if (_head == null)
                {
                    //TODO-Miguep:check changes from SecurityWrappers to regular reflection types
                    object head = _priorityChainObj.GetType().InvokeMember("Head",
                        BindingFlags.GetProperty | BindingFlags.Instance | BindingFlags.Public
                        | BindingFlags.IgnoreCase | BindingFlags.NonPublic,
                        null,
                        _priorityChainObj,
                        null);

                    _head = head;
                }

                return _head;
            }

        }

        /// <summary>
        /// Returns the last PriorityItem from the PriorityChain
        /// </summary>
        /// <value></value>
        public object Tail
        {
            get
            {
                if (_tail == null)
                {

                    //TODO-Miguep:check changes from SecurityWrappers to regular reflection types
                    object tail = _priorityChainObj.GetType().InvokeMember("Tail",
                        BindingFlags.GetProperty | BindingFlags.Instance | BindingFlags.Public
                        | BindingFlags.IgnoreCase | BindingFlags.NonPublic,
                        null,
                        _priorityChainObj,
                        null);

                    _tail = tail;
                }

                return _tail;
            }

        }

        /// <summary>
        /// Returns all the items on the PriorityChain
        /// </summary>
        /// <value></value>
        public List<ItemInfo> Items
        {
            get
            {
                List<ItemInfo> itemList = new List<ItemInfo>();

                object head = Head;

                // if there is no items on the PriorityChain
                if (head == null)
                {
                    return itemList;
                }

                object tail = Tail;

                ItemInfo currentNode = new ItemInfo(head);

                // If there is only one item

                if (head == tail)
                {
                    itemList.Add(currentNode);

                    return itemList;
                }

                // If there are more than 1 item on the chain
                while (tail != currentNode.WrappedObject)
                {
                    itemList.Add(currentNode);

                    currentNode = new ItemInfo(currentNode.PriorityNext);
                }

                // Adding the Tail
                itemList.Add(currentNode);

                return itemList;

            }
        }

        object _head = null;
        object _tail = null;
        string _priorityName = "";
        object _priorityChainObj = null;
    }


    /// <summary>
    /// This class simulates the PriorityItem class on the Queue
    /// </summary>
    public class ItemInfo
    {
        /// <summary>
        /// Constructor that passes the PriorityItem to be wrapped
        /// </summary>
        /// <param name="itemObj"></param>
        internal ItemInfo(object itemObj)
        {
            if (itemObj == null)
                throw new ArgumentNullException("itemObj");
            _itemObj = itemObj;
        }

        /// <summary>
        /// Return the PriorityItem object that is wrapped
        /// </summary>
        /// <value></value>
        public object WrappedObject
        {
            get
            {
                return _itemObj;
            }
        }

        /// <summary>
        /// Return the Name on the DispatcherOperation.Name.  The PriorityItem.Data on the PriorityChain
        /// is always a DispatcherOperation, so we return that Name (internal property)
        /// The Name property contains the Name for the delegate stored.
        /// All data is cached for reused.
        /// </summary>
        /// <value></value>
        public string ID
        {
            get
            {
                if (_operationObj == null || _name == "")
                {
                    //TODO-Miguep:check changes from SecurityWrappers to regular reflection types
                    // Gets the Data from the PriorityItem
                    object operationObj = _itemObj.GetType().InvokeMember("Data",
                        BindingFlags.GetProperty | BindingFlags.GetProperty | BindingFlags.Instance | BindingFlags.Public
                        | BindingFlags.IgnoreCase | BindingFlags.NonPublic,
                        null,
                        _itemObj,
                        null);

                    _operationObj = operationObj;

                    // Retrieves the Name from the Data. This only is possible because all the priority
                    // items on the Avalon queues are DispatcherOperation; and dispatcherOperation has
                    // an internal property called Name
                    _name = DispatcherHelper.GetNameFromDispatcherOperation(operationObj as DispatcherOperation);
                }

                return _name;
            }
        }

        /// <summary>
        /// Returns the next PriorityItem on the PriorityChain linked list.
        /// We cached this value if we want to reuse it.
        /// </summary>
        /// <value></value>
        public object PriorityNext
        {
            get
            {
                if (_nextObj == null)
                {
                    //TODO-Miguep:check changes from SecurityWrappers to regular reflection types
                    // Gets the next item on the linked list for the PriorityChain
                    // PriorityItem.PriorityNext
                    object nextObj = _itemObj.GetType().InvokeMember("PriorityNext",
                        BindingFlags.GetProperty | BindingFlags.Instance | BindingFlags.Public
                        | BindingFlags.IgnoreCase | BindingFlags.NonPublic,
                        null,
                        _itemObj,
                        null);

                    _nextObj = nextObj;

                }

                return _nextObj;
            }
        }

        object _itemObj = null;
        object _operationObj = null;
        string _name = "";
        object _nextObj = null;

    }
}


