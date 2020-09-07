// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections;

namespace System.Windows.Threading
{
    internal class PriorityChain<T>
    {
        public PriorityChain(DispatcherPriority priority) // NOTE: should be Priority
        {
            _priority = priority;
        }

        public DispatcherPriority Priority {get{return _priority;} set{_priority = value;}} // NOTE: should be Priority
        public int Count {get{return _count;} set{_count=value;}}
        public PriorityItem<T> Head {get{return _head;} set{_head=value;}}
        public PriorityItem<T> Tail {get{return _tail;} set{_tail=value;}}

        private PriorityItem<T> _head;
        private PriorityItem<T> _tail;
        private DispatcherPriority _priority;
        private int _count;
    }
}

