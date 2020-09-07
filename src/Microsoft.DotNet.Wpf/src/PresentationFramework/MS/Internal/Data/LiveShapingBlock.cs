// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// Description: A component of the list data structure used for live shaping.
//

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;

using System.Windows;
using System.Windows.Data;

namespace MS.Internal.Data
{
    internal class LiveShapingBlock : RBNode<LiveShapingItem>
    {
        internal LiveShapingBlock() : base() { }
        internal LiveShapingBlock(bool b) : base(b) { }
        LiveShapingBlock ParentBlock { get { return Parent as LiveShapingBlock; } }
        LiveShapingBlock LeftChildBlock { get { return (LiveShapingBlock)LeftChild; } }
        LiveShapingBlock RightChildBlock { get { return (LiveShapingBlock)RightChild; } }

        internal LiveShapingList List
        {
            get { return ((LiveShapingTree)GetRoot(this)).List; }
        }

        public override LiveShapingItem SetItemAt(int offset, LiveShapingItem lsi)
        {
            base.SetItemAt(offset, lsi);
            if (lsi != null)
                lsi.Block = this;
            return lsi;
        }

        protected override void Copy(RBNode<LiveShapingItem> sourceNode, int sourceOffset, RBNode<LiveShapingItem> destNode, int destOffset, int count)
        {
#if LiveShapingInstrumentation
            ++_copies;
            _totalCopySize += count;
#endif

            base.Copy(sourceNode, sourceOffset, destNode, destOffset, count);

            if (sourceNode != destNode)
            {
                LiveShapingBlock destBlock = (LiveShapingBlock)destNode;

                for (int k = 0; k < count; ++k, ++destOffset)
                {
                    destNode.GetItemAt(destOffset).Block = destBlock;
                }
            }
        }

        internal RBFinger<LiveShapingItem> GetFinger(LiveShapingItem lsi)
        {
            int baseIndex;
            int offset = OffsetOf(lsi);
            GetRootAndIndex(this, out baseIndex);
            return new RBFinger<LiveShapingItem>() { Node = this, Offset = offset, Index = baseIndex + offset, Found = true };
        }

        // find the current and desired position of the given item (called while restoring live sorting)
        // The hurdle here is that items marked IsSortDirty should be ignored - their sort property
        // has changed, so they are possibly not in the right place.
        internal void FindPosition(LiveShapingItem item, out RBFinger<LiveShapingItem> oldFinger, out RBFinger<LiveShapingItem> newFinger, Comparison<LiveShapingItem> comparison)
        {
            // within this block, find the first and last sort-clean items, the given item
            // itself, and the nearest sort-clean items on either side of the given item
            int size = Size;
            int index;
            int first = -1, last, left = -1, right;
            LiveShapingItem lsi;

            for (index = 0; index < size; ++index)
            {   // linear search finds the item, the first clean item, and the left-nearest
                lsi = GetItemAt(index);
                if (item == lsi)
                    break;

                if (!lsi.IsSortDirty)
                {
                    left = index;
                    if (first < 0)
                        first = index;
                }
            }

            Debug.Assert(index < size, "FindPosition called with item not in its block");

            for (right = index + 1; right < size; ++right)
            {   // continue the linear search to find the right-nearest
                lsi = GetItemAt(right);
                if (!lsi.IsSortDirty)
                    break;
            }

            last = right;
            for (int k = size - 1; k > last; --k)
            {   // reverse linear search to find the last clean item
                lsi = GetItemAt(k);
                if (!lsi.IsSortDirty)
                    last = k;
            }

            // now we know enough to create the oldFinger
            int baseIndex;
            GetRootAndIndex(this, out baseIndex);
            oldFinger = new RBFinger<LiveShapingItem>() { Node = this, Offset = index, Index = baseIndex + index, Found = true };

            // find the newFinger
            // The current picture is:
            //      first       left    index       right       last
            // |----v-----------v-------v-----------v-----------v-----------|
            // |****|           |*******|***********|           |***********|
            // |------------------------------------------------------------|
            // Items under the pointers are clean (unless the pointers fall off the
            // end).  The shaded regions are dirty, the unshaded regions can be
            // a mixture of clean and dirty.  Any of the regions might be empty,
            // as the pointers can coincide.

            LiveShapingItem leftItem = (left >= 0) ? GetItemAt(left) : null;
            LiveShapingItem rightItem = (right < size) ? GetItemAt(right) : null;
            int cL = 0, cR = 0; // result of comparisons

            if (leftItem != null && (cL = comparison(item, leftItem)) < 0)
            {
                if (first != left)
                    cL = comparison(item, GetItemAt(first));
                if (cL >= 0)
                {   // item belongs between first and left
                    newFinger = LocalSearch(item, first + 1, left, comparison);
                }
                else
                {   // item belongs to the left of this block
                    newFinger = SearchLeft(item, first, comparison);
                }
            }
            else if (rightItem != null && (cR = comparison(item, rightItem)) > 0)
            {
                if (last != right)
                    cR = comparison(item, GetItemAt(last));
                if (cR <= 0)
                {   // item belongs between right and last
                    newFinger = LocalSearch(item, right + 1, last, comparison);
                }
                else
                {   // item belongs to the right of this block
                    newFinger = SearchRight(item, last + 1, comparison);
                }
            }
            else if (leftItem != null)  // hence item >= leftItem
            {
                if (rightItem != null)  // hence item <= rightItem
                {   // item is already in a good position
                    newFinger = oldFinger;
                }
                else
                {   // item belongs to the right of this block
                    newFinger = SearchRight(item, index, comparison);
                }
            }
            else // leftItem == null
            {
                if (rightItem != null)  // hence item <= rightItem
                {
                    newFinger = SearchLeft(item, index, comparison);
                }
                else
                {   // no other clean items in this block - item could belong anywhere
                    newFinger = SearchLeft(item, index, comparison);
                    if (newFinger.Node == this)
                    {   // couldn't find it for sure on the left - look right
                        newFinger = SearchRight(item, index, comparison);
                    }
                }
            }
        }

        // binary search that ignores dirty items
        RBFinger<LiveShapingItem> LocalSearch(LiveShapingItem item, int left, int right, Comparison<LiveShapingItem> comparison)
        {
            int k;
            while (right - left > BinarySearchThreshold)
            {
                int mid = (left + right) / 2;
                for (k = mid; k >= left; --k)
                {
                    if (!GetItemAt(k).IsSortDirty)
                        break;
                }

                if (k < left || comparison(GetItemAt(k), item) <= 0)
                {
                    left = mid + 1;
                }
                else
                {
                    right = k;
                }
            }

            for (k = left; k < right; ++k)
            {
                if (!GetItemAt(k).IsSortDirty && comparison(item, GetItemAt(k)) <= 0)
                    break;
            }

            int index;
            GetRootAndIndex(this, out index);
            return new RBFinger<LiveShapingItem>() { Node = this, Offset = k, Index = index + k };
        }


        /* The next two methods - SearchLeft and SearchRight - are called when the item
            might belong to a different node.  I.e. it compares below the first clean item
            in the current node (or there is no first clean item), or it compares above
            the last clean item (or there is no last clean item).   Here's the picture for
            the "left" case.
                                                    Legend
                    [-----]                     +       starting node
                   /       \                    [----]  left-ancestor - needs search
                  *         0                   0       right-ancestor - doesn't need search
                           /                    *       unexplored left subtree
                       [-----]
                      /       \
                     *         +
                              /
                             *
            To find where the item belongs, we first walk up the tree pausing at
            each left-ancestor (so we visit them right-to-left).  If it has no clean
            items, we learn nothing - instead we add its left subtree * to a list
            of unexplored subtrees that we may search later.  More likely, it has clean
            items, and comparisons will tell us whether the item belongs to the ancestor
            itself, to its left, or to its right.  This constrains the search:

                itself  the item belongs to the ancestor - use binary search to find where
                left    the item can't belong to the unexplored trees - discard them
                right   the item can't belong any farther left - stop the upward tree walk

            When the upward walk stops without finding a home for the item, we start
            searching the unexplored subtrees, right-to-left.  To search a subtree,
            we first look at its root.  As above, the bad case is when the root has
            no clean items;  in that case we need to search both its subtrees, so we push
            them on a stack (the stack holds the unexplored roots in right-to-left
            order).   Here's a picture:

                    Stack                       *           Stack
                                               / \            R
                      *     ==> Pop *   ==>   L   R  ==>      L
                      x                                       x

            With better luck, the subtree root will have clean items, and comparisons
            will tell us where the item belongs with respect to the node:

                itself  use binary search to find where
                left    push the node's left child onto the stack
                right   push the node's right child onto the stack

            Throughout this search, we keep track of the rightmost node the item
            could belong to (the "found node").  Initially this is the original
            starting node, but it changes as we move left from a subtree root.
            If the stack empties without finding a home, we put the item into
            the "found node", to the left of its leftmost clean item.

            The "right" search is symmetric.  We explore right-ancestors, followed
            by unexplored subtrees in left-to-right order.
        */

        // find the item in the tree to my left.  If it's larger than the nearest clean
        // neighbor, return a finger to the given offset within this node
        RBFinger<LiveShapingItem> SearchLeft(LiveShapingItem item, int offset, Comparison<LiveShapingItem> comparison)
        {
            LiveShapingBlock foundBlock = this;

            List<LiveShapingBlock> list = new List<LiveShapingBlock>();  // subtrees to explore
            list.Add(LeftChildBlock);

            // phase 1, walk up the tree looking for the item in each left-parent
            LiveShapingBlock block, parent;
            int first, last, size;
            for (block = this, parent = block.ParentBlock; parent != null; block = parent, parent = block.ParentBlock)
            {
                if (parent.RightChildBlock == block)
                {
                    parent.GetFirstAndLastCleanItems(out first, out last, out size);

                    // find where the search item belongs relative to the clean items
                    if (first >= size)
                    {   // no clean items - add the left subtree for later exploration
                        list.Add(parent.LeftChildBlock);
                    }
                    else if (comparison(item, parent.GetItemAt(last)) > 0)
                    {   // item belongs to the right of parent - no need to climb higher
                        break;
                    }
                    else if (comparison(item, parent.GetItemAt(first)) >= 0)
                    {   // item belongs in the parent
                        return parent.LocalSearch(item, first + 1, last, comparison);
                    }
                    else
                    {   // item belongs to the left of parent, thus also left of unexplored subtrees
                        list.Clear();
                        list.Add(parent.LeftChildBlock);
                        foundBlock = parent;
                        offset = first;
                    }
                }
            }

            // phase 2, item wasn't found in a left-parent, so it belongs in one of the
            // unexplored subtrees.   Search them right-to-left, hoping item didn't move far.
            Stack<LiveShapingBlock> stack = new Stack<LiveShapingBlock>(list);
            while (stack.Count > 0)
            {
                block = stack.Pop();
                if (block == null)
                    continue;

                block.GetFirstAndLastCleanItems(out first, out last, out size);

                if (first >= size)
                {   // no clean items - explore the subtrees
                    stack.Push(block.LeftChildBlock);
                    stack.Push(block.RightChildBlock);
                }
                else if (comparison(item, block.GetItemAt(last)) > 0)
                {   // item belongs to the right
                    stack.Clear();
                    stack.Push(block.RightChildBlock);
                }
                else if (comparison(item, block.GetItemAt(first)) >= 0)
                {   // item belongs in the current block
                    return block.LocalSearch(item, first + 1, last, comparison);
                }
                else
                {   // item belongs to the left of the block, or possibly in it
                    foundBlock = block;
                    offset = first;
                    stack.Push(block.LeftChildBlock);
                }
            }

            // phase 3, item wasn't found within any block, so put it at the
            // edge of the "found" block
            int baseIndex;
            GetRootAndIndex(foundBlock, out baseIndex);
            return new RBFinger<LiveShapingItem>() { Node = foundBlock, Offset = offset, Index = baseIndex + offset };
        }

        // find the item in the tree to my right.  If it's smaller than the nearest clean
        // neighbor, return a finger to the given offset within this node
        RBFinger<LiveShapingItem> SearchRight(LiveShapingItem item, int offset, Comparison<LiveShapingItem> comparison)
        {
            LiveShapingBlock foundBlock = this;

            List<LiveShapingBlock> list = new List<LiveShapingBlock>();  // subtrees to explore
            list.Add(RightChildBlock);

            // phase 1, walk up the tree looking for the item in each right-parent
            LiveShapingBlock block, parent;
            int first, last, size;
            for (block = this, parent = block.ParentBlock; parent != null; block = parent, parent = block.ParentBlock)
            {
                if (parent.LeftChildBlock == block)
                {
                    parent.GetFirstAndLastCleanItems(out first, out last, out size);

                    // find where the search item belongs relative to the clean items
                    if (first >= size)
                    {   // no clean items - add the right subtree for later exploration
                        list.Add(parent.RightChildBlock);
                    }
                    else if (comparison(item, parent.GetItemAt(first)) < 0)
                    {   // item belongs to the left of parent - no need to climb higher
                        break;
                    }
                    else if (comparison(item, parent.GetItemAt(last)) <= 0)
                    {   // item belongs in the parent
                        return parent.LocalSearch(item, first + 1, last, comparison);
                    }
                    else
                    {   // item belongs to the right of parent, thus also right of unexplored subtrees
                        list.Clear();
                        list.Add(parent.RightChildBlock);
                        foundBlock = parent;
                        offset = last + 1;
                    }
                }
            }

            // phase 2, item wasn't found in a right-parent, so it belongs in one of the
            // unexplored subtrees.   Search them left-to-right, hoping item didn't move far.
            Stack<LiveShapingBlock> stack = new Stack<LiveShapingBlock>(list);
            while (stack.Count > 0)
            {
                block = stack.Pop();
                if (block == null)
                    continue;

                block.GetFirstAndLastCleanItems(out first, out last, out size);

                if (first >= size)
                {   // no clean items - explore the subtrees
                    stack.Push(block.RightChildBlock);
                    stack.Push(block.LeftChildBlock);
                }
                else if (comparison(item, block.GetItemAt(first)) < 0)
                {   // item belongs to the left
                    stack.Clear();
                    stack.Push(block.LeftChildBlock);
                }
                else if (comparison(item, block.GetItemAt(last)) <= 0)
                {   // item belongs in the current block
                    return block.LocalSearch(item, first + 1, last, comparison);
                }
                else
                {   // item belongs to the right of the block, or possibly in it
                    foundBlock = block;
                    offset = last + 1;
                    stack.Push(block.RightChildBlock);
                }
            }

            // phase 3, item wasn't found within any block, so put it at the
            // edge of the "found" block
            int baseIndex;
            GetRootAndIndex(foundBlock, out baseIndex);
            return new RBFinger<LiveShapingItem>() { Node = foundBlock, Offset = offset, Index = baseIndex + offset };
        }

        void GetFirstAndLastCleanItems(out int first, out int last, out int size)
        {
            size = Size;
            for (first = 0; first < size; ++first)
            {
                if (!GetItemAt(first).IsSortDirty)
                    break;
            }
            for (last = size - 1; last > first; --last)
            {
                if (!GetItemAt(last).IsSortDirty)
                    break;
            }
        }

#if LiveShapingInstrumentation

        static internal void SetNodeSize(int nodeSize)
        {
            MaxSize = nodeSize;
        }

        static internal int _copies, _totalCopySize;
#endif // LiveShapingInstrumentation
    }
}
