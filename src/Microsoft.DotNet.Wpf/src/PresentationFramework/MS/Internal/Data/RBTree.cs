// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// Description: Root of a red-black tree.
//

/*
    The Red-black tree is a popular balanced binary tree data structure for
    ordered lists, yielding O(log n) worst-case performance for standard operations
    like search, insert, and delete.  This implementation is based on Bob Sedgewick's
    Left-Leaning Red-Black Trees idea.
        formal paper:   http://www.cs.princeton.edu/~rs/talks/LLRB/LLRB.pdf
        slides:         http://www.cs.princeton.edu/~rs/talks/LLRB/RedBlack.pdf

    RB-trees are binary trees satisfying some additional constraints:
        1. Every node is colored either red or black. (Equivalently, the edge
            from the node to its parent is colored red or black.)
        2. Leaf nodes (null pointers) are black.
        3. No consecutive reds - if a node is red, its parent is black.
        4. Every leaf (null) has the same "black depth" - the number of black
            nodes on the path from the leaf to the root.
    A left-leaning tree has one additional constraint:
        5. A mixed-color family leans left.   If a node has one red child and one
            black child, the red child is the left child.

    This constraint makes the rebalancing algorithms simpler, as there are fewer
    cases to worry about.

    This implementation augments a simple textbook RB-tree in several ways.
        1. Bulk data.   Every node holds an array of data items, up to MaxSize.
            This gets the speed of array operations (indexing, bulk moves by
            memcopy, etc.) for much of the work.
        2. Order statistics.   Every node knows the number of items in its left
            subtree.  This permits O(log n) implementations of search-by-index,
            IndexOf(x), and other index-based operations.
        3. Fingers.  Short-lived pointers to a position within the data structure.
*/

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Windows;   // SR, SRID
using TypeConverterHelper = System.Windows.Markup.TypeConverterHelper;

namespace MS.Internal.Data
{
    internal class RBTree<T> : RBNode<T>, IList<T>
    {
#if LiveShapingInstrumentation
        protected static int QuickSortThreshold = 15;
#else
        const int QuickSortThreshold = 15;
#endif

        public RBTree() : base(false)
        {
            Size = MaxSize;
        }

        public override bool HasData { get { return false; } }

        public Comparison<T> Comparison
        {
            get { return _comparison; }
            set { _comparison = value; }
        }

        public RBFinger<T> BoundedSearch(T x, int low, int high)
        {
            return BoundedSearch(x, low, high, Comparison);
        }

        public void Insert(T x)
        {
            RBFinger<T> finger = Find(x, Comparison);
            Insert(finger, x, true);
        }

        void Insert(RBFinger<T> finger, T x, bool checkSort = false)
        {
#if RBTreeFlightRecorder
            SaveTree();
            int size = LeftSize;
#endif

            RBNode<T> node = finger.Node;

            if (node == this)
            {
                node = InsertNode(0);
                node.InsertAt(0, x);
            }
            else if (node.Size < MaxSize)
            {
                node.InsertAt(finger.Offset, x);
            }
            else
            {
                RBNode<T> successor = node.GetSuccessor();
                RBNode<T> succsucc = null;
                if (successor.Size >= MaxSize)
                {
                    if (successor != this)
                        succsucc = successor;
                    successor = InsertNode(finger.Index + node.Size - finger.Offset);
                }
                node.InsertAt(finger.Offset, x, successor, succsucc);
            }

            LeftChild.IsRed = false;

#if RBTreeFlightRecorder
            Verify(size + 1, checkSort);
#endif
        }

        public void Sort()
        {
            try
            {
                QuickSort();
            }
            catch (Exception e)
            {
                throw new InvalidOperationException(SR.Get(SRID.InvalidOperation_IComparerFailed), e);
            }
        }

        public void QuickSort()
        {
#if RBTreeFlightRecorder
            SaveTree();
            int size = Count;
#endif

            if (Count > 1)
            {
                RBFinger<T> low = FindIndex(0, false);
                RBFinger<T> high = FindIndex(Count, false);

                QuickSort3(low, high);
                InsertionSortImpl();
            }

#if RBTreeFlightRecorder
            Verify(size);
#endif
        }

        public void InsertionSort()
        {
#if RBTreeFlightRecorder
            SaveTree();
            int size = Count;
#endif

            if (Count > 1)
            {
                InsertionSortImpl();
            }

#if RBTreeFlightRecorder
            Verify(size);
#endif
        }

        // QuickSort, with the following techniques:
        //  1. choose pivot by median-of-3
        //  2. detect items equal to the pivot (3-way pivoting)
        //  3. recurse on smaller subfile first (limits stack depth)
        //  4. insertion-sort small subfiles, in one pass at the end
        //  5. eliminate tail recursion
        void QuickSort3(RBFinger<T> low, RBFinger<T> high)
        {
            while (high - low > QuickSortThreshold)
            {
                // the goal of 3-way pivoting is to swap items so as to divide the list
                // into three pieces:
                //  "red" (item < pivot), "green" (item == pivot), "blue" (item > pivot)
                // This is the famous "Dutch National Flag" problem, named by Dijkstra
                // in honor of his country's flag, which has three vertical stripes
                // of different colors (I don't remember the Dutch colors, so I'm using
                // red, greeen, and blue, like the bits in a pixel color.)
                //
                // The following algorithm seems to be the best in practice.  It's not
                // widely known - I reinvented it based on memories of a conversation
                // I had with someone (Tarjan, Cole, Bentley, McIlroy, Sedgewick?) long
                // ago.  I think Doug McIlroy had the idea.
                //
                // We maintain an invariant:  the region to the left of "left"
                // has three subregions containing green, red, and green items, respectively.
                // The fingers greenL, red, and left give the right-boundaries of these
                // regions, so the picture is
                //
                //  |--low    |--greenL    |--red    |--left
                //  v         v            v         v
                //  .------------------------------------------
                //  |  green  |   red      |  green  |   unknown
                //  .------------------------------------------
                //
                // Similarly, the region to the right of "right" looks like
                //
                //          |--right    |--blue    |--greenR    |--high
                //          v           v          v            v
                //     -----------------------------------------.
                //  unknown |  green    |  blue    |  green     |
                //     -----------------------------------------.
                //
                // We advance left and right toward each other, swapping items as needed
                // to preserve the invariant (the details are best expressed in the the
                // code itself).  When left and right reach each other, we swap the red
                // region with the green region to its left, and swap blue with the
                // green to its right, and we're done.

                RBFinger<T> greenL = low, red = low + 1, blue = high - 1, greenR = high;
                T x;

                // select pivot - median of 3
                RBFinger<T> mid = FindIndex((low.Index + high.Index) / 2);
                int c = Comparison(low.Item, mid.Item);
                if (c < 0)
                {
                    c = Comparison(mid.Item, blue.Item);
                    if (c < 0)
                    {   // r, g, b
                    }
                    else if (c == 0)
                    {   // r, g, g
                        greenR = blue;
                    }
                    else
                    {
                        c = Comparison(low.Item, blue.Item);
                        if (c < 0)
                        {   // r, b, g
                            Exchange(mid, blue);
                        }
                        else if (c == 0)
                        {   // g, b, g
                            Exchange(mid, blue);
                            greenL = red;
                        }
                        else
                        {   // g, b, r
                            Exchange(low, mid);
                            Exchange(low, blue);
                        }
                    }
                }
                else if (c == 0)
                {
                    c = Comparison(low.Item, blue.Item);
                    if (c < 0)
                    {   // g, g, b
                        greenL = red;
                    }
                    else if (c == 0)
                    {   // g, g, g
                        greenL = red;
                        greenR = blue;
                    }
                    else
                    {   // g, g, r
                        Exchange(low, blue);
                        greenR = blue;
                    }
                }
                else
                {
                    c = Comparison(low.Item, blue.Item);
                    if (c < 0)
                    {   // g, r, b
                        Exchange(low, mid);
                    }
                    else if (c == 0)
                    {   // g, r, g
                        Exchange(low, mid);
                        greenR = blue;
                    }
                    else
                    {
                        c = Comparison(mid.Item, blue.Item);
                        if (c < 0)
                        {   // b, r, g
                            Exchange(low, mid);
                            Exchange(mid, blue);
                        }
                        else if (c == 0)
                        {   // b, g, g
                            Exchange(low, blue);
                            greenL = red;
                        }
                        else
                        {   // b, g, r
                            Exchange(low, blue);
                        }
                    }
                }

                // Now partition the list into three pieces, using the pivot item
                x = mid.Item;
                RBFinger<T> left = red, right = blue;
                for (; ; )
                {
                    // advance 'left'
                    while (left < right)
                    {
                        c = Comparison(left.Item, x);
                        if (c < 0)
                        {   // red
                            Trade(greenL, red, left);
                            greenL += left - red;
                            red = ++left;
                        }
                        else if (c == 0)
                        {   // green
                            ++left;
                        }
                        else
                        {   // blue
                            break;
                        }
                    }

                    // advance 'right'
                    while (left < right)
                    {
                        RBFinger<T> f = right - 1;
                        c = Comparison(f.Item, x);
                        if (c < 0)
                        {   // red
                            break;
                        }
                        else if (c == 0)
                        {   // green
                            --right;
                        }
                        else
                        {   // blue
                            Trade(right, blue, greenR);
                            greenR -= blue - right;
                            blue = --right;
                        }
                    }

                    // check for termination
                    c = right - left;
                    if (c == 0)
                    {   // one or both of the loops terminated due to pointer collision.
                        // swap the outer green regions into the middle, and terminate
                        Trade(low, greenL, red); red += low - greenL;
                        Trade(blue, greenR, high); blue += high - greenR;
                        break;
                    }
                    else if (c == 1)
                    {   // this should never happen
                    }
                    else if (c == 2)
                    {   // special case - this[left] is blue, this[left+1] is red
                        // swap the outer green regions into the middle, and terminate
                        Trade(low, greenL, red); red += low - greenL + 1; Exchange(red - 1, left + 1);
                        if (red > left) ++left;
                        Trade(blue, greenR, high); blue += high - greenR - 1; Exchange(left, blue);
                        break;
                    }
                    else
                    {   // swap this[left] (blue) with this[right-1] (red) and continue
                        Exchange(left, right - 1);
                        Trade(greenL, red, left); greenL += left - red; red = ++left;
                        Trade(right, blue, greenR); greenR -= blue - right; blue = --right;
                    }
                }

                // Now sort the red and blue regions.  Sort the smaller one first by
                // recursion, then the larger one by tail-recursion.
                if (red - low < high - blue)
                {
                    QuickSort3(low, red);
                    low = blue;
                }
                else
                {
                    QuickSort3(blue, high);
                    high = red;
                }
            }
        }

        // Input: two regions [left, mid) and [mid, right), each of a single color
        // Output: swap so that the color on the left is now on the right, and vice-versa
        void Trade(RBFinger<T> left, RBFinger<T> mid, RBFinger<T> right)
        {
            int n = Math.Min(mid - left, right - mid);
            for (int k = 0; k < n; ++k)
            {
                --right;
                Exchange(left, right);
                ++left;
            }
        }

        void Exchange(RBFinger<T> f1, RBFinger<T> f2)
        {
            T x = f1.Item;
            f1.SetItem(f2.Item);
            f2.SetItem(x);
        }

        void InsertionSortImpl()
        {
            RBFinger<T> finger = FindIndex(1);
            while (finger.Node != this)
            {
                RBFinger<T> fingerL = LocateItem(finger, Comparison);
                ReInsert(ref finger, fingerL);
                ++finger;
            }
        }

        internal RBNode<T> InsertNode(int index)
        {
            RBNode<T> node;
            LeftChild = InsertNode(this, this, LeftChild, index, out node);
            return node;
        }

        internal void RemoveNode(int index)
        {
            LeftChild = DeleteNode(this, LeftChild, index);
            if (LeftChild != null)
                LeftChild.IsRed = false;
        }

        internal virtual RBNode<T> NewNode()
        {
            return new RBNode<T>();
        }

        internal void ForEach(Action<T> action)
        {
            foreach (T x in this)
            {
                action(x);
            }
        }

        internal void ForEachUntil(Func<T, bool> action)
        {
            foreach (T x in this)
            {
                if (action(x))
                    break;
            }
        }

        internal int IndexOf(T item, Func<T, T, bool> AreEqual)
        {
            if (Comparison != null)
            {
                RBFinger<T> finger = Find(item, Comparison);
                while (finger.Found && !AreEqual(finger.Item, item))
                {
                    ++finger;
                    finger.Found = (finger.IsValid && Comparison(finger.Item, item) == 0);
                }

                return finger.Found ? finger.Index : -1;
            }
            else
            {
                int result = 0;
                ForEachUntil((x) =>
                   {
                       if (AreEqual(x, item))
                           return true;
                       ++result;
                       return false;
                   });
                return (result < Count) ? result : -1;
            }
        }

        #region IList<T>

        public virtual int IndexOf(T item)
        {
            return IndexOf(item, (x, y) => { return System.Windows.Controls.ItemsControl.EqualsEx(x, y); });
        }

        public void Insert(int index, T item)
        {
            VerifyIndex(index, 1);
            RBFinger<T> finger = FindIndex(index, false);
            Insert(finger, item);
        }

        public void RemoveAt(int index)
        {
            VerifyIndex(index);

            SaveTree();
            int size = LeftSize;

            RBFinger<T> finger = FindIndex(index, true);
            RemoveAt(ref finger);
            if (LeftChild != null)
                LeftChild.IsRed = false;

            Verify(size - 1);
        }

        public T this[int index]
        {
            get
            {
                VerifyIndex(index);
                RBFinger<T> finger = FindIndex(index);
                return finger.Node.GetItemAt(finger.Offset);
            }
            set
            {
                VerifyIndex(index);
                RBFinger<T> finger = FindIndex(index);
                finger.Node.SetItemAt(finger.Offset, value);
            }
        }

        public void Add(T item)
        {
            SaveTree();
            int size = LeftSize;

            RBNode<T> node = LeftChild;
            if (node == null)
            {
                node = InsertNode(0);
                node.InsertAt(0, item);
            }
            else
            {
                while (node.RightChild != null)
                    node = node.RightChild;
                if (node.Size < MaxSize)
                {
                    node.InsertAt(node.Size, item);
                }
                else
                {
                    node = InsertNode(this.LeftSize);
                    node.InsertAt(0, item);
                }
            }

            LeftChild.IsRed = false;
            Verify(size + 1, false);
        }

        public void Clear()
        {
            LeftChild = null;
            LeftSize = 0;
        }

        public bool Contains(T item)
        {
            RBFinger<T> finger = Find(item, Comparison);
            return finger.Found;
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            if (array == null)
                throw new ArgumentNullException("array");
            if (arrayIndex < 0)
                throw new ArgumentOutOfRangeException("arrayIndex");
            if (arrayIndex + Count > array.Length)
                throw new ArgumentException(SR.Get(SRID.Argument_InvalidOffLen));

            foreach (T item in this)
            {
                array[arrayIndex] = item;
                ++arrayIndex;
            }
        }

        public int Count
        {
            get { return LeftSize; }
        }

        public bool IsReadOnly
        {
            get { return false; }
        }

        public bool Remove(T item)
        {
            RBFinger<T> finger = Find(item, Comparison);
            if (finger.Found)
                RemoveAt(ref finger);
            if (LeftChild != null)
                LeftChild.IsRed = false;
            return finger.Found;
        }

        public IEnumerator<T> GetEnumerator()
        {
            RBFinger<T> finger = FindIndex(0);
            while (finger.Node != this)
            {
                yield return finger.Node.GetItemAt(finger.Offset);
                ++finger;
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            RBFinger<T> finger = FindIndex(0);
            while (finger.Node != this)
            {
                yield return finger.Node.GetItemAt(finger.Offset);
                ++finger;
            }
        }

        void VerifyIndex(int index, int delta = 0)
        {
            if (index < 0 || index >= Count + delta)
                throw new ArgumentOutOfRangeException("index");
        }

        #endregion IList<T>

        #region Debugging
#if DEBUG

        public bool CheckSort { get; set; }

        public bool Verify(int expectedSize, bool checkSort = true)
        {
            if (!CheckSort || Comparison == null)
                checkSort = false;
            int index = 0, size = 0;
            T maxItem = default(T);
            BlackHeight = -1;
            bool b = Verify(LeftChild, checkSort ? Comparison : null, 0, ref index, ref maxItem, out size) && size == LeftSize && size == expectedSize;
            return b;
        }

        void SaveTree()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(LeftSize);
            SaveTree(LeftChild, sb);
            _savedTree = sb.ToString();
        }

        public void LoadTree(string s)
        {
            if (s.StartsWith("\"", StringComparison.Ordinal)) s = s.Substring(1);
            int index = s.IndexOf('(');
            LeftSize = Int32.Parse(s.Substring(0, index), TypeConverterHelper.InvariantEnglishUS);
            s = s.Substring(index);
            this.LeftChild = LoadTree(ref s);
            this.LeftChild.Parent = this;
        }

        string _savedTree;

#else
        void Verify(int expectedSize, bool checkSort = true) { }
        void SaveTree() { }
        public void LoadTree(string s) { }
#endif // DEBUG
        #endregion Debugging

        Comparison<T> _comparison;
    }
}
