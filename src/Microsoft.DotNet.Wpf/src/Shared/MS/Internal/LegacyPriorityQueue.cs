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


// !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
//
// Do not use this class; it has a serious bug.
//
// A correct implementation is available in PriorityQueue.cs.  That implementation
// also includes a performance optimization for the frequent case where the client
// does all the Push operations before doing any Top or Pop operations.
//
// .Net Shiproom insists on keeping the old (buggy) code for existing uses,
// citing concerns about compatibility.  And they won't allow the new (fixed and
// perf-improved) code into the build if it's not being used.  If you have a
// new use for PriorityQueue, add the new class to the build and use it instead
// of this class.
//
// The bug is that the output is out of order, in the sense that Top can return
// an item that is not the lowest-priority of all items in the queue.  This only
// happens for sufficiently large inputs, and only for some sequences of operations.
// The smallest example involves 6 items;  when Pushed in a certain order, a
// sequence of 3 Pops can leave the queue in a state where Top returns an item
// that is not the lowest-priority of the 3 remaining items.
//
// In terms of current use, the bug would arise when trying to find a physical
// font to display a particular character.  Font files are assigned priority
// based on how closely they match the desired size, weight, and stretch.
// If there are (at least) 6 candidate font files, discovered in a certain order,
// and if the first 3 do not contain a glyph for the character, WPF could choose
// a font file that is not the best match among the remaining candidates.
// In other words, WPF might display the character using a glyph from a
// less-than-best physical font, when the best candidate fonts don't match.
// (We have no reports of this actually happening, possibly because it might escape
// notice by all but the most font-obsessed users.)

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
    internal class LegacyPriorityQueue<T>
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

        internal LegacyPriorityQueue(int capacity, IComparer<T> comparer)
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
                T[] temp = new T[_count * 2];
                for (int i = 0; i < _count; ++i)
                {
                    temp[i] = _heap[i];
                }
                _heap = temp;
            }

            // Loop invariant:
            //
            //  1.  index is a gap where we might insert the new node; initially
            //      it's the end of the array (bottom-right of the logical tree).
            //
            int index = _count;
            while (index > 0)
            {
                int parentIndex = HeapParent(index);
                if (_comparer.Compare(value, _heap[parentIndex]) < 0)
                {
                    // value is a better match than the parent node so exchange
                    // places to preserve the "heap" property.
                    _heap[index] = _heap[parentIndex];
                    index = parentIndex;
                }
                else
                {
                    // we can insert here.
                    break;
                }
            }

            _heap[index] = value;
            _count++;
        }

        /// <summary>
        /// Removes the first node (i.e., the logical root) from the heap.
        /// </summary>
        internal void Pop()
        {
            Debug.Assert(_count != 0);

            if (_count > 1)
            {
                // Loop invariants:
                //
                //  1.  parent is the index of a gap in the logical tree
                //  2.  leftChild is
                //      (a) the index of parent's left child if it has one, or
                //      (b) a value >= _count if parent is a leaf node
                //
                int parent = 0;
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

                // Fill the last gap by moving the last (i.e., bottom-rightmost) node.
                _heap[parent] = _heap[_count - 1];
            }

            _count--;
        }

        #endregion

        #region private members

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
        private const int DefaultCapacity = 6;

        #endregion
    }
}
