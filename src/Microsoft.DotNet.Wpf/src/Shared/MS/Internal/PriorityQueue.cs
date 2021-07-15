// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//-----------------------------------------------------------------------
//
//  Microsoft Windows Client Platform
// 
//
//  File:      PriorityQueue.cs
//
//  Contents:  Implementation of PriorityQueue class.
//
//  Created:   2-14-2005 Niklas Borson (niklasb)
//
//------------------------------------------------------------------------


using System;
using System.Diagnostics;
using System.Collections.Generic;

namespace MS.Internal
{
    /// <summary>
    /// PriorityQueue provides a stack-like interface, except that objects
    /// "pushed" in arbitrary order are "popped" in order of priority, i.e.,
    /// from least to greatest as defined by the specified comparer.
    /// </summary>
    /// <remarks>
    /// Push and Pop are each O(log N). Pushing N objects and them popping
    /// them all is equivalent to performing a heap sort and is O(N log N).
    /// </remarks>
    internal class PriorityQueue<T>
    {
        //
        // The _heap array represents a binary tree with the "shape" property.
        // If we number the nodes of a binary tree from left-to-right and top-
        // to-bottom as shown,
        //
        //             0
        //           /   \
        //          /     \
        //         1       2
        //       /  \     / \
        //      3    4   5   6
        //     /\    /
        //    7  8  9
        //
        // The shape property means that there are no gaps in the sequence of
        // numbered nodes, i.e., for all N > 0, if node N exists then node N-1
        // also exists. For example, the next node added to the above tree would
        // be node 10, the right child of node 4.
        //
        // Because of this constraint, we can easily represent the "tree" as an
        // array, where node number == array index, and parent/child relationships
        // can be calculated instead of maintained explicitly. For example, for
        // any node N > 0, the parent of N is at array index (N - 1) / 2.
        //
        // In addition to the above, the first _count members of the _heap array
        // compose a "heap", meaning each child node is greater than or equal to
        // its parent node; thus, the root node is always the minimum (i.e., the
        // best match for the specified style, weight, and stretch) of the nodes
        // in the heap.
        //
        // Initially _count < 0, which means we have not yet constructed the heap.
        // On the first call to MoveNext, we construct the heap by "pushing" all
        // the nodes into it. Each successive call "pops" a node off the heap
        // until the heap is empty (_count == 0), at which time we've reached the
        // end of the sequence.
        //

        #region constructors

        internal PriorityQueue(int capacity, IComparer<T> comparer)
        {
            _heap = new T[capacity > 0 ? capacity : DefaultCapacity];
            _count = 0;
            _comparer = comparer;
        }

        #endregion

        #region internal members

        /// <summary>
        /// Gets the number of items in the priority queue.
        /// </summary>
        internal int Count
        {
            get { return _count; }
        }

        /// <summary>
        /// Gets the first or topmost object in the priority queue, which is the
        /// object with the minimum value.
        /// </summary>
        internal T Top
        {
            get
            {
                Debug.Assert(_count > 0);
                if (!_isHeap)
                {
                    Heapify();
                }

                return _heap[0];
            }
        }

        /// <summary>
        /// Adds an object to the priority queue.
        /// </summary>
        internal void Push(T value)
        {
            // Increase the size of the array if necessary.
            if (_count == _heap.Length)
            {
                Array.Resize<T>(ref _heap, _count * 2);
            }

            // A common usage is to Push N items, then Pop them.  Optimize for that
            // case by treating Push as a simple append until the first Top or Pop,
            // which establishes the heap property.  After that, Push needs
            // to maintain the heap property.
            if (_isHeap)
            {
                SiftUp(_count, ref value, 0);
            }
            else
            {
                _heap[_count] = value;
            }

            _count++;
        }

        /// <summary>
        /// Removes the first node (i.e., the logical root) from the heap.
        /// </summary>
        internal void Pop()
        {
            Debug.Assert(_count != 0);
            if (!_isHeap)
            {
                Heapify();
            }

            if (_count > 0)
            {
                --_count;

                // discarding the root creates a gap at position 0.  We fill the
                // gap with the item x from the last position, after first sifting
                // the gap to a position where inserting x will maintain the
                // heap property.  This is done in two phases - SiftDown and SiftUp.
                //
                // The one-phase method found in many textbooks does 2 comparisons
                // per level, while this method does only 1.  The one-phase method
                // examines fewer levels than the two-phase method, but it does
                // more comparisons unless x ends up in the top 2/3 of the tree.
                // That accounts for only n^(2/3) items, and x is even more likely
                // to end up near the bottom since it came from the bottom in the
                // first place.  Overall, the two-phase method is noticeably better.

                T x = _heap[_count];        // lift item x out from the last position
                int index = SiftDown(0);    // sift the gap at the root down to the bottom
                SiftUp(index, ref x, 0);    // sift the gap up, and insert x in its rightful position
                _heap[_count] = default(T); // don't leak x
            }
        }

        #endregion

        #region private members

        // sift a gap at the given index down to the bottom of the heap,
        // return the resulting index
        private int SiftDown(int index)
        {
            // Loop invariants:
            //
            //  1.  parent is the index of a gap in the logical tree
            //  2.  leftChild is
            //      (a) the index of parent's left child if it has one, or
            //      (b) a value >= _count if parent is a leaf node
            //
            int parent = index;
            int leftChild = HeapLeftChild(parent);

            while (leftChild < _count)
            {
                int rightChild = HeapRightFromLeft(leftChild);
                int bestChild =
                    (rightChild < _count && _comparer.Compare(_heap[rightChild], _heap[leftChild]) < 0) ?
                    rightChild : leftChild;

                // Promote bestChild to fill the gap left by parent.
                _heap[parent] = _heap[bestChild];

                // Restore invariants, i.e., let parent point to the gap.
                parent = bestChild;
                leftChild = HeapLeftChild(parent);
            }

            return parent;
        }

        // sift a gap at index up until it reaches the correct position for x,
        // or reaches the given boundary.  Place x in the resulting position.
        private void SiftUp(int index, ref T x, int boundary)
        {
            while (index > boundary)
            {
                int parent = HeapParent(index);
                if (_comparer.Compare(_heap[parent], x) > 0)
                {
                    _heap[index] = _heap[parent];
                    index = parent;
                }
                else
                {
                    break;
                }
            }
            _heap[index] = x;
        }

        // Establish the heap property:  _heap[k] >= _heap[HeapParent(k)], for 0<k<_count
        // Do this "bottom up", by iterating backwards.  At each iteration, the
        // property inductively holds for k >= HeapLeftChild(i)+2;  the body of
        // the loop extends the property to the children of position i (namely
        // k=HLC(i) and k=HLC(i)+1) by lifting item x out from position i, sifting
        // the resulting gap down to the bottom, then sifting it back up (within
        // the subtree under i) until finding x's rightful position.
        //
        // Iteration i does work proportional to the height (distance to leaf)
        // of the node at position i.  Half the nodes are leaves with height 0;
        // there's nothing to do for these nodes, so we skip them by initializing
        // i to the last non-leaf position.  A quarter of the nodes have height 1,
        // an eigth have height 2, etc. so the total work is ~ 1*n/4 + 2*n/8 +
        // 3*n/16 + ... = O(n).  This is much cheaper than maintaining the
        // heap incrementally during the "Push" phase, which would cost O(n*log n).
        private void Heapify()
        {
            if (!_isHeap)
            {
                for (int i = _count/2 - 1; i >= 0; --i)
                {
                    // we use a two-phase method for the same reason Pop does
                    T x = _heap[i];
                    int index = SiftDown(i);
                    SiftUp(index, ref x, i);
                }
                _isHeap = true;
            }
        }

        /// <summary>
        /// Calculate the parent node index given a child node's index, taking advantage
        /// of the "shape" property.
        /// </summary>
        private static int HeapParent(int i)
        {
            return (i - 1) / 2;
        }

        /// <summary>
        /// Calculate the left child's index given the parent's index, taking advantage of
        /// the "shape" property. If there is no left child, the return value is >= _count.
        /// </summary>
        private static int HeapLeftChild(int i)
        {
            return (i * 2) + 1;
        }

        /// <summary>
        /// Calculate the right child's index from the left child's index, taking advantage
        /// of the "shape" property (i.e., sibling nodes are always adjacent). If there is
        /// no right child, the return value >= _count.
        /// </summary>
        private static int HeapRightFromLeft(int i)
        {
            return i + 1;
        }

        private T[] _heap;
        private int _count;
        private IComparer<T> _comparer;
        private bool _isHeap;
        private const int DefaultCapacity = 6;

        #endregion
    }
}
