// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace System.Windows.Threading
{
    internal class PriorityItem<T>
    {
        public PriorityItem(T data)
        {
            _data = data;
        }
        
        public T Data {get{return _data;}}
        public bool IsQueued { get { return _chain != null; } }

        // Note: not used
        // public DispatcherPriority Priority { get { return _chain.Priority; } } // NOTE: should be Priority

        internal PriorityItem<T> SequentialPrev {get{return _sequentialPrev;} set{_sequentialPrev=value;}}
        internal PriorityItem<T> SequentialNext {get{return _sequentialNext;} set{_sequentialNext=value;}}

        internal PriorityChain<T> Chain {get{return _chain;} set{_chain=value;}}
        internal PriorityItem<T> PriorityPrev {get{return _priorityPrev;} set{_priorityPrev=value;}}
        internal PriorityItem<T> PriorityNext {get{return _priorityNext;} set{_priorityNext=value;}}

        private T _data;
        
        private PriorityItem<T> _sequentialPrev;
        private PriorityItem<T> _sequentialNext;

        private PriorityChain<T> _chain;
        private PriorityItem<T> _priorityPrev;
        private PriorityItem<T> _priorityNext;
    }
}

