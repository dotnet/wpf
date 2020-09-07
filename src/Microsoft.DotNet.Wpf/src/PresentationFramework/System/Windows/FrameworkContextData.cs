// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


using System.Windows.Threading;

using MS.Utility;

using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace System.Windows
{
    internal class FrameworkContextData
    {
        public static FrameworkContextData From(Dispatcher context)
        {
            FrameworkContextData data = (FrameworkContextData)context.Reserved2;
        
            if (data == null)
            {
                data = new FrameworkContextData();
                context.Reserved2 = data;
            }

            return data;
        }

        // Enforce never creating except via static constructor above.
        private FrameworkContextData()
        {
        }

        //
        // Property Invalidation of Inheritable Properties
        //
        // At the context level, we need to keep track of all inheritable property invalidations currently
        // in action.  The reason that there can be multiple invalidations going on at the same time is because
        // an invalidation of one property can cause an invalidation of a different property.  The result is that
        // the first invalidation *pauses* while the second invalidation is delivered to the tree.
        //
        // We keep track of these invalidations to be able to optimize a recursion of the same property
        // invalidation from an element to that element's children.  FrameworkElement.InvalidateTree will 
        // check the stack of walkers here and, if it finds a match, will conclude that a new DescendentsWalker
        // need not be spun up.  And there was much rejoicing.
        //

        public void AddWalker(object data, DescendentsWalkerBase walker)
        {
            // push a new walker on the top of the stack
            WalkerEntry walkerEntry = new WalkerEntry();
            walkerEntry.Data = data;
            walkerEntry.Walker = walker;
        
            _currentWalkers.Add(walkerEntry);
        }

        public void RemoveWalker(object data, DescendentsWalkerBase walker)
        {
            // pop the walker off the top of the stack
            int last = _currentWalkers.Count - 1;
#if DEBUG        
            WalkerEntry walkerEntry = _currentWalkers[last];
        
            Debug.Assert((walkerEntry.Data == data) && (walkerEntry.Walker == walker),
                "Inheritance DescendentsWalker tracker removal failed");
#endif        
            _currentWalkers.RemoveAt(last);
        }

        public bool WasNodeVisited(DependencyObject d, object data)
        {
            // check to see if the given property on the given object is going to be visited by the
            // DescendentsWalker on the top of the stack
            if (_currentWalkers.Count > 0)
            {
                int last = _currentWalkers.Count - 1;
            
                WalkerEntry walkerEntry = _currentWalkers[last];
            
                if (walkerEntry.Data == data)
                {
                    return walkerEntry.Walker.WasVisited(d);
                }
            }

            return false;
        }

        private struct WalkerEntry
        {
            public object Data; // either the inheritable DP being invalidated, or the AncestorChangedDelegate, or the ResourceChangedDelegate
            public DescendentsWalkerBase Walker;
        }

        private List<WalkerEntry> _currentWalkers = new List<WalkerEntry>(4);
    }
}

