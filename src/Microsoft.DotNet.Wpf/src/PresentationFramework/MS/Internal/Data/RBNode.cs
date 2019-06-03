// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// Description: Node in a red-black tree.
//

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Text;
using TypeConverterHelper = System.Windows.Markup.TypeConverterHelper;

namespace MS.Internal.Data
{
    internal class RBNode<T> : INotifyPropertyChanged
    {
#if LiveShapingInstrumentation
        protected static int MaxSize = 64;
        protected static int BinarySearchThreshold = 3;
#else
        protected const int MaxSize = 64;
        protected const int BinarySearchThreshold = 3;
#endif

        public RBNode()
        {
            _data = new T[MaxSize];
        }

        protected RBNode(bool b)
        {
        }

        public RBNode<T> LeftChild { get; set; }
        public RBNode<T> RightChild { get; set; }
        public RBNode<T> Parent { get; set; }
        public bool IsRed { get; set; }
        public virtual bool HasData { get { return true; } }

        int _size;
        public int Size { get { return _size; } set { _size = value; OnPropertyChanged("Size"); } }

        int _leftSize;
        public int LeftSize { get { return _leftSize; } set { _leftSize = value; OnPropertyChanged("LeftSize"); } }

        public T GetItemAt(int offset) { return _data[offset]; }
        public virtual T SetItemAt(int offset, T x) { _data[offset] = x; return x; }
        public int OffsetOf(T x) { return Array.IndexOf(_data, x); }

        internal RBNode<T> GetSuccessor()
        {
            RBNode<T> node, parent;
            if (RightChild == null)
            {   // go up
                for (node = this, parent = node.Parent; parent.RightChild == node; node = parent, parent = node.Parent)
                    ;
                return parent;
            }
            else
            {   // go down
                for (parent = RightChild, node = parent.LeftChild; node != null; parent = node, node = parent.LeftChild)
                    ;
                return parent;
            }
        }

        internal RBNode<T> GetPredecessor()
        {
            RBNode<T> node, parent;
            if (LeftChild == null)
            {   // go up
                for (node = this, parent = node.Parent; parent != null && parent.LeftChild == node; node = parent, parent = node.Parent)
                    ;
                return parent;
            }
            else
            {   // go down
                for (parent = LeftChild, node = parent.RightChild; node != null; parent = node, node = parent.RightChild)
                    ;
                return parent;
            }
        }

        protected RBFinger<T> FindIndex(int index, bool exists = true)
        {
            RBFinger<T> result;
            int delta = exists ? 1 : 0;
            if (index + delta <= LeftSize)
            {
                if (LeftChild == null)
                    result = new RBFinger<T>() { Node = this, Offset = 0, Index = 0, Found = false };
                else
                {
                    result = LeftChild.FindIndex(index, exists);
                }
            }
            else if (index < LeftSize + Size)
            {
                result = new RBFinger<T>() { Node = this, Offset = index - LeftSize, Index = index, Found = true };
            }
            else
            {
                if (RightChild == null)
                    result = new RBFinger<T>() { Node = this, Offset = Size, Index = LeftSize + Size, Found = false };
                else
                {
                    result = RightChild.FindIndex(index - LeftSize - Size, exists);
                    result.Index += LeftSize + Size;
                }
            }
            return result;
        }

        protected RBFinger<T> Find(T x, Comparison<T> comparison)
        {
            RBFinger<T> result;
            int compL = (_data != null) ? comparison(x, GetItemAt(0)) : -1;
            int compR;
            if (compL <= 0)
            {
                if (LeftChild == null)
                    result = new RBFinger<T>() { Node = this, Offset = 0, Index = 0, Found = (compL == 0) };
                else
                {
                    result = LeftChild.Find(x, comparison);
                    if (compL == 0 && !result.Found)
                        result = new RBFinger<T>() { Node = this, Offset = 0, Index = LeftSize, Found = true };
                }
            }
            else if ((compR = comparison(x, GetItemAt(Size - 1))) <= 0)
            {
                bool found;
                int offset = BinarySearch(x, 1, Size - 1, comparison, compR, out found);
                result = new RBFinger<T>() { Node = this, Offset = offset, Index = LeftSize + offset, Found = found };
            }
            else
            {
                if (RightChild == null)
                    result = new RBFinger<T>() { Node = this, Offset = Size, Index = LeftSize + Size };
                else
                {
                    result = RightChild.Find(x, comparison);
                    result.Index += LeftSize + Size;
                }
            }

            return result;
        }

        // search for x within the index range [low, high)
        protected RBFinger<T> BoundedSearch(T x, int low, int high, Comparison<T> comparison)
        {
            RBFinger<T> result;
            int compL, compR;
            RBNode<T> leftChild = LeftChild, rightChild = RightChild;
            int left = 0, right = Size;

            // determine whether to search the left subtree
            if (high <= LeftSize)
            {   // allowed range is entirely within left subtree
                compL = -1;
            }
            else
            {
                if (low >= LeftSize)
                {   // left subtree does not intersect allowed range
                    leftChild = null;
                    left = low - LeftSize;
                }
                compL = (left < Size) ? comparison(x, GetItemAt(left)) : +1;
            }

            if (compL <= 0)
            {   // x is in the left subtree, or at the leftmost position in this node
                if (leftChild == null)
                    result = new RBFinger<T>() { Node = this, Offset = left, Index = left, Found = (compL == 0) };
                else
                {
                    result = leftChild.BoundedSearch(x, low, high, comparison);
                    if (compL == 0 && !result.Found)
                        result = new RBFinger<T>() { Node = this, Offset = 0, Index = LeftSize, Found = true };
                }
                return result;
            }

            // determine whether to search the right subtree
            if (LeftSize + Size <= low)
            {   // allowed range is entirely within right subtree
                compR = +1;
            }
            else
            {
                if (LeftSize + Size >= high)
                {   // right subtree does not intersect allowed range
                    rightChild = null;
                    right = high - LeftSize;
                }
                // by symmetry with the left case, we should write
                // compR = (right > 0) ? comparison(x, GetItemAt(right-1)) : -1;
                // but since we know Size>0 and high>LeftSize, we can abbreviate to
                compR = comparison(x, GetItemAt(right - 1));
            }

            if (compR > 0)
            {   // x is in the right subtree, or after the rightmost position in this node
                if (rightChild == null)
                    result = new RBFinger<T>() { Node = this, Offset = right, Index = LeftSize + right, Found = false };
                else
                {
                    int delta = LeftSize + Size;
                    result = rightChild.BoundedSearch(x, low - delta, high - delta, comparison);
                    result.Index += delta;
                }
                return result;
            }

            // if we get here, x is in this node in the range [left+1, right)
            bool found;
            int offset = BinarySearch(x, left + 1, right - 1, comparison, compR, out found);
            result = new RBFinger<T>() { Node = this, Offset = offset, Index = LeftSize + offset, Found = found };
            return result;
        }

        int BinarySearch(T x, int low, int high, Comparison<T> comparison, int compHigh, out bool found)
        {
            while (high - low > BinarySearchThreshold)
            {
                int mid = (high + low) / 2;
                int c = comparison(x, GetItemAt(mid));
                if (c <= 0)
                {
                    compHigh = c;
                    high = mid;
                }
                else
                    low = mid + 1;
            }

            int comp = 0;
            for (; low < high; ++low)
            {
                comp = comparison(x, GetItemAt(low));
                if (comp <= 0)
                    break;
            }
            if (low == high)
                comp = compHigh;

            found = (comp == 0);
            return low;
        }

        // Find the new position of the item under the given finger.  Used in InsertionSort.
        protected RBFinger<T> LocateItem(RBFinger<T> finger, Comparison<T> comparison)
        {
            RBNode<T> startingNode = finger.Node;
            int nodeIndex = finger.Index - finger.Offset;
            T x = startingNode.GetItemAt(finger.Offset);

            // first look within the node, using standard InsertionSort loop
            for (int k = finger.Offset - 1; k >= 0; --k)
            {
                if (comparison(x, startingNode.GetItemAt(k)) >= 0)
                    return new RBFinger<T>() { Node = startingNode, Offset = k + 1, Index = nodeIndex + k + 1 };
            }

            // next locate x between a node and its left-parent
            RBNode<T> node = startingNode, parent = node.Parent;
            while (parent != null)
            {
                while (parent != null && node == parent.LeftChild)
                { node = parent; parent = node.Parent; }    // find left-parent

                if (parent == null || comparison(x, parent.GetItemAt(parent.Size - 1)) >= 0)
                    break;      // x belongs in startingNode's left subtree

                nodeIndex = nodeIndex - startingNode.LeftSize - parent.Size;
                if (comparison(x, parent.GetItemAt(0)) >= 0)
                {   // x belongs in the parent
                    bool found;
                    int offset = parent.BinarySearch(x, 1, parent.Size - 1, comparison, -1, out found);
                    return new RBFinger<T>() { Node = parent, Offset = offset, Index = nodeIndex + offset };
                }

                // advance up the tree
                startingNode = node = parent;
                parent = node.Parent;
            }

            // now we know x belongs in startingNode's left subtree, if any
            if (startingNode.LeftChild != null)
            {
                RBFinger<T> newFinger = startingNode.LeftChild.Find(x, comparison);
                if (newFinger.Offset == newFinger.Node.Size)
                    newFinger = new RBFinger<T>() { Node = newFinger.Node.GetSuccessor(), Offset = 0, Index = newFinger.Index };
                return newFinger;
            }
            else
                return new RBFinger<T>() { Node = startingNode, Offset = 0, Index = nodeIndex };
        }

        protected virtual void Copy(RBNode<T> sourceNode, int sourceOffset, RBNode<T> destNode, int destOffset, int count)
        {
            Array.Copy(sourceNode._data, sourceOffset, destNode._data, destOffset, count);
        }

        // move the item under oldFinger to newFinger (assumed to be left of oldFinger)
        protected void ReInsert(ref RBFinger<T> oldFinger, RBFinger<T> newFinger)
        {
            RBNode<T> oldNode = oldFinger.Node, newNode = newFinger.Node;
            int oldOffset = oldFinger.Offset, newOffset = newFinger.Offset;
            T x = oldNode.GetItemAt(oldFinger.Offset);

            if (oldNode == newNode)
            {   // move within a single node
                int s = oldOffset - newOffset;
                if (s != 0)
                {
                    Copy(oldNode, newOffset, oldNode, newOffset + 1, s);
                    oldNode.SetItemAt(newOffset, x);
                }
            }
            else
            {   // move from one node to an earlier node
                if (newNode.Size < MaxSize)
                {   // easy case - new node has room
                    newNode.InsertAt(newOffset, x);
                    oldNode.RemoveAt(ref oldFinger);
                }
                else
                {   // hard case - new node is full
                    RBNode<T> successor = newNode.GetSuccessor();
                    if (successor == oldNode)
                    {   // easy subcase - oldNode is next to newNode
                        T y = newNode.GetItemAt(MaxSize - 1);
                        Copy(newNode, newOffset, newNode, newOffset + 1, MaxSize - newOffset - 1);
                        newNode.SetItemAt(newOffset, x);
                        Copy(oldNode, 0, oldNode, 1, oldOffset);
                        oldNode.SetItemAt(0, y);
                    }
                    else
                    {
                        if (successor.Size < MaxSize)
                        {   // medium subcase - need to move items into successor
                            newNode.InsertAt(newOffset, x, successor);
                        }
                        else
                        {   // hard subcase - need a new node after newNode
                            RBNode<T> succsucc = successor;
                            successor = InsertNodeAfter(newNode);
                            newNode.InsertAt(newOffset, x, successor, succsucc);
                        }

                        oldNode.RemoveAt(ref oldFinger);
                    }
                }
            }
        }

        protected void RemoveAt(ref RBFinger<T> finger)
        {
            RBNode<T> node = finger.Node;
            int offset = finger.Offset;
            Copy(node, offset + 1, node, offset, node.Size - offset - 1);
            node.ChangeSize(-1);
            node.SetItemAt(node.Size, default(T));

            if (node.Size == 0)
            {
                // first move the finger to the successor node
                finger.Node = node.GetSuccessor();
                finger.Offset = 0;

                int index;
                RBTree<T> root = node.GetRootAndIndex(node, out index);
                root.RemoveNode(index);
            }

            finger.Offset -= 1;
        }

        protected RBNode<T> InsertNodeAfter(RBNode<T> node)
        {
            int index;
            RBTree<T> root = GetRootAndIndex(node, out index);
            return root.InsertNode(index + node.Size);
        }

        protected RBTree<T> GetRoot(RBNode<T> node)
        {
            for (RBNode<T> parent = node.Parent; parent != null; node = parent, parent = node.Parent)
            {
            }
            return (RBTree<T>)node;
        }

        protected RBTree<T> GetRootAndIndex(RBNode<T> node, out int index)
        {
            index = node.LeftSize;
            for (RBNode<T> parent = node.Parent; parent != null; node = parent, parent = node.Parent)
            {
                if (node == parent.RightChild)
                    index += parent.LeftSize + parent.Size;
            }
            return (RBTree<T>)node;
        }

        internal void InsertAt(int offset, T x, RBNode<T> successor = null, RBNode<T> succsucc = null)
        {
            if (Size < MaxSize)
            {
                // insert x into this.Array at offset
                Copy(this, offset, this, offset + 1, Size - offset);
                SetItemAt(offset, x);
                ChangeSize(1);
            }
            else
            {
                Debug.Assert(successor != null && successor.Size < MaxSize, "InsertAt: successor should have room");
                if (successor.Size == 0)
                {
                    if (succsucc == null)
                    {   // special case for insertion at the right - keep this node full
                        if (offset < MaxSize)
                        {
                            // move last item to successor
                            successor.InsertAt(0, GetItemAt(MaxSize - 1));
                            // insert x into this.Array at offset
                            Copy(this, offset, this, offset + 1, MaxSize - offset - 1);
                            SetItemAt(offset, x);
                        }
                        else
                        {
                            // insert x into successor
                            successor.InsertAt(0, x);
                        }
                    }
                    else
                    {   // split two full nodes into three
                        Debug.Assert(succsucc.Size == MaxSize, "InsertAt: outer nodes should be full");
                        int s = MaxSize / 3;

                        // move s items from this node into successor
                        Copy(successor, 0, successor, s, successor.Size);
                        Copy(this, MaxSize - s, successor, 0, s);

                        // move s items from succsucc into successor
                        Copy(succsucc, 0, successor, s + successor.Size, s);
                        Copy(succsucc, s, succsucc, 0, MaxSize - s);

                        if (offset <= MaxSize - s)
                        {
                            // insert into this.Array at offset
                            Copy(this, offset, this, offset + 1, MaxSize - s - offset);
                            SetItemAt(offset, x);

                            this.ChangeSize(1 - s);
                            successor.ChangeSize(s + s);
                        }
                        else
                        {
                            // insert into successor.Array at offset-(MaxSize-s)
                            Copy(successor, offset - (MaxSize - s), successor, offset - (MaxSize - s) + 1, successor.Size + s + s - (offset - (MaxSize - s)));
                            successor.SetItemAt(offset - (MaxSize - s), x);

                            this.ChangeSize(-s);
                            successor.ChangeSize(s + s + 1);
                        }
                        succsucc.ChangeSize(-s);
                    }
                }
                else
                {   // split a full node and its not-full successor into two pieces
                    int s = (Size + successor.Size + 1) / 2;

                    if (offset < s)
                    {
                        // move MaxSize-s+1 items from this node into successor
                        Copy(successor, 0, successor, MaxSize - s + 1, successor.Size);
                        Copy(this, s - 1, successor, 0, MaxSize - s + 1);

                        // insert into this.Array at offset
                        Copy(this, offset, this, offset + 1, s - 1 - offset);
                        SetItemAt(offset, x);
                    }
                    else
                    {
                        // move MaxSize-s items from this node into successor
                        Copy(successor, 0, successor, MaxSize - s, successor.Size);
                        Copy(this, s, successor, 0, MaxSize - s);

                        // insert into successor.Array at offset-s
                        Copy(successor, offset - s, successor, offset - s + 1, successor.Size + MaxSize - offset);
                        successor.SetItemAt(offset - s, x);
                    }
                    this.ChangeSize(s - MaxSize);
                    successor.ChangeSize(MaxSize - s + 1);
                }
            }
        }

        protected RBNode<T> InsertNode(RBTree<T> root, RBNode<T> parent, RBNode<T> node, int index, out RBNode<T> newNode)
        {
            if (node == null)
            {
                newNode = root.NewNode();
                newNode.Parent = parent;
                newNode.IsRed = true;
                return newNode;
            }

            if (index <= node.LeftSize)
            {
                node.LeftChild = InsertNode(root, node, node.LeftChild, index, out newNode);
            }
            else
            {
                Debug.Assert(index >= node.LeftSize + node.Size, "InsertNode: index should fall between nodes");
                node.RightChild = InsertNode(root, node, node.RightChild, index - node.LeftSize - node.Size, out newNode);
            }

            node = Fixup(node);

            return node;
        }

        protected void ChangeSize(int delta)
        {
            if (delta == 0) return;

            // clear slots that are no longer used
            for (int k = Size + delta; k < Size; ++k)
                _data[k] = default(T);

            Size += delta;
            RBNode<T> node, parent;
            for (node = this, parent = node.Parent; parent != null; node = parent, parent = node.Parent)
            {
                if (parent.LeftChild == node)
                    parent.LeftSize += delta;
            }
        }

        RBNode<T> Substitute(RBNode<T> node, RBNode<T> sub, RBNode<T> parent)
        {
            sub.LeftChild = node.LeftChild;
            sub.RightChild = node.RightChild;
            sub.LeftSize = node.LeftSize;
            sub.Parent = node.Parent;
            sub.IsRed = node.IsRed;

            if (sub.LeftChild != null) sub.LeftChild.Parent = sub;
            if (sub.RightChild != null) sub.RightChild.Parent = sub;
            return sub;
        }

        // invariant:  node is red, or one if its children is red
        // As we move down the tree this is preserved by calling MoveRedLeft or
        // MoveRedRight, to "borrow red-ness" from a sibling.
        protected RBNode<T> DeleteNode(RBNode<T> parent, RBNode<T> node, int index)
        {
            if (index < node.LeftSize || (index == node.LeftSize && node.Size > 0))
            {
                if (!IsNodeRed(node.LeftChild) && !IsNodeRed(node.LeftChild.LeftChild))
                    node = MoveRedLeft(node);
                node.LeftChild = DeleteNode(node, node.LeftChild, index);
            }
            else
            {
                bool deleteHere = (index == node.LeftSize);
                Debug.Assert(!deleteHere || node.Size == 0, "DeleteNode: Deleted node should be empty");

                if (IsNodeRed(node.LeftChild))
                {
                    node = node.RotateRight();
                    deleteHere = false;
                }
                if (deleteHere && node.RightChild == null)
                    return null;
                if (!IsNodeRed(node.RightChild) && !IsNodeRed(node.RightChild.LeftChild))
                {
                    RBNode<T> temp = node;
                    node = MoveRedRight(node);
                    deleteHere = deleteHere && (temp == node);
                }

                if (deleteHere)
                {
                    RBNode<T> sub;
                    node.RightChild = DeleteLeftmost(node.RightChild, out sub);
                    node = Substitute(node, sub, parent);
                }
                else
                    node.RightChild = DeleteNode(node, node.RightChild, index - node.LeftSize - node.Size);
            }

            return Fixup(node);
        }

        RBNode<T> DeleteLeftmost(RBNode<T> node, out RBNode<T> leftmost)
        {
            if (node.LeftChild == null)
            {
                leftmost = node;
                return null;
            }

            if (!IsNodeRed(node.LeftChild) && !IsNodeRed(node.LeftChild.LeftChild))
                node = MoveRedLeft(node);

            node.LeftChild = DeleteLeftmost(node.LeftChild, out leftmost);
            node.LeftSize -= leftmost.Size;
            return Fixup(node);
        }

        bool IsNodeRed(RBNode<T> node)
        {
            return node != null && node.IsRed;
        }

        RBNode<T> RotateLeft()
        {
            RBNode<T> node = this.RightChild;
            node.LeftSize += this.LeftSize + this.Size;
            node.IsRed = this.IsRed;
            node.Parent = this.Parent;
            this.RightChild = node.LeftChild;
            if (this.RightChild != null) this.RightChild.Parent = this;
            node.LeftChild = this;
            this.IsRed = true;
            this.Parent = node;
            return node;
        }

        RBNode<T> RotateRight()
        {
            RBNode<T> node = this.LeftChild;
            this.LeftSize -= node.LeftSize + node.Size;
            node.IsRed = this.IsRed;
            node.Parent = this.Parent;
            this.LeftChild = node.RightChild;
            if (this.LeftChild != null) this.LeftChild.Parent = this;
            node.RightChild = this;
            this.IsRed = true;
            this.Parent = node;
            return node;
        }

        void ColorFlip()
        {
            this.IsRed = !this.IsRed;
            LeftChild.IsRed = !LeftChild.IsRed;
            RightChild.IsRed = !RightChild.IsRed;
        }

        RBNode<T> Fixup(RBNode<T> node)
        {
            if (!IsNodeRed(node.LeftChild) && IsNodeRed(node.RightChild))
                node = node.RotateLeft();
            if (IsNodeRed(node.LeftChild) && IsNodeRed(node.LeftChild.LeftChild))
                node = node.RotateRight();
            if (IsNodeRed(node.LeftChild) && IsNodeRed(node.RightChild))
                node.ColorFlip();
            return node;
        }

        RBNode<T> MoveRedRight(RBNode<T> node)
        {
            node.ColorFlip();
            if (IsNodeRed(node.LeftChild.LeftChild))
            {
                node = node.RotateRight();
                node.ColorFlip();
            }
            return node;
        }

        RBNode<T> MoveRedLeft(RBNode<T> node)
        {
            node.ColorFlip();
            if (IsNodeRed(node.RightChild.LeftChild))
            {
                node.RightChild = node.RightChild.RotateRight();
                node = node.RotateLeft();
                node.ColorFlip();
            }
            return node;
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string name)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(name));
        }

        T[] _data;

        #region Debugging
#if DEBUG

        protected bool Verify(RBNode<T> node, Comparison<T> comparison, int blackDepth, ref int index, ref T maxItem, out int size)
        {
            bool result = true;

            if (node == null)
            {
                if (BlackHeight < 0) BlackHeight = blackDepth;
                size = 0;

                if (blackDepth != BlackHeight)
                    result = false;   // not black-balanced

                return result;
            }

            if (!node.IsRed)
                ++blackDepth;

            result = Verify(node.LeftChild, comparison, blackDepth, ref index, ref maxItem, out size);

            if (node.Size <= 0)
                result = false;     // too few items

            if (node.Size > MaxSize)
                result = false;     // too many items

            if (!IsNodeRed(node.LeftChild) && IsNodeRed(node.RightChild))
                result = false;     // not left-leaning

            if (node.IsRed && (IsNodeRed(node.LeftChild) || IsNodeRed(node.RightChild)))
                result = false;     // consecutive reds

            if (size != node.LeftSize)
                result = false;     // LeftSize is wrong

            if (node.Parent.LeftChild != node && node != node.Parent.RightChild)
                result = false;     // Parent is wrong

            if (comparison != null)
            {
                if (index > 0 && comparison(maxItem, node.GetItemAt(0)) > 0)
                    result = false;     // first item is out of order

                for (int k = 1; k < node.Size; ++k)
                {
                    if (comparison(node.GetItemAt(k-1), node.GetItemAt(k)) > 0)
                        result = false; // k-th item is out of order
                }
            }

            for (int j=node.Size; j<MaxSize; ++j)
            {
                if (!System.Windows.Controls.ItemsControl.EqualsEx(node.GetItemAt(j), default(T)))
                    result = false;     // someone didn't clean up the array
            }

            size += node.Size;
            ++index;
            maxItem = node.GetItemAt(node.Size - 1);

            int rightSize;
            result = Verify(node.RightChild, comparison, blackDepth, ref index, ref maxItem, out rightSize) && result;

            size += rightSize;

            return result;
        }

        protected void SaveTree(RBNode<T> node, StringBuilder sb)
        {
            if (node == null)
                sb.Append("()");
            else
            {
                sb.Append("(");
                sb.Append(node.IsRed ? 'T' : 'F');
                sb.Append(node.LeftSize);
                sb.Append(",");
                sb.Append(node.Size);
                for (int k = 0; k < node.Size; ++k)
                {
                    sb.Append(",");
                    sb.Append(AsInt(node.GetItemAt(k)));
                }
                SaveTree(node.LeftChild, sb);
                SaveTree(node.RightChild, sb);
                sb.Append(")");
            }
        }

        int AsInt(object x)
        {
            return (x is int) ? (int)x : 0;
        }

        T AsT(object x)
        {
            return (x is T) ? (T)x : default(T);
        }

        protected RBNode<T> LoadTree(ref string s)
        {
            if (s.StartsWith("()", StringComparison.Ordinal))
            {
                s = s.Substring(2);
                return null;
            }

            int index;

            RBNode<T> node = new RBNode<T>();
            s = s.Substring(1);             // skip '('

            node.IsRed = (s[0] == 'T');     // read IsRed
            s = s.Substring(1);

            index = s.IndexOf(',');         // read LeftSize
            node.LeftSize = Int32.Parse(s.Substring(0, index), TypeConverterHelper.InvariantEnglishUS);
            s = s.Substring(index + 1);

            index = s.IndexOf(',');         // read Size
            node.Size = Int32.Parse(s.Substring(0, index), TypeConverterHelper.InvariantEnglishUS);
            s = s.Substring(index+1);

            for (int k = 0; k < node.Size-1; ++k) // read data
            {
                index = s.IndexOf(',');
                node.SetItemAt(k, AsT(Int32.Parse(s.Substring(0, index), TypeConverterHelper.InvariantEnglishUS)));
                s = s.Substring(index+1);
            }
            index = s.IndexOf('(');
            node.SetItemAt(node.Size - 1, AsT(Int32.Parse(s.Substring(0, index), TypeConverterHelper.InvariantEnglishUS)));
            s = s.Substring(index);

            node.LeftChild = LoadTree(ref s);   // read subtrees
            node.RightChild = LoadTree(ref s);
            if (node.LeftChild != null) node.LeftChild.Parent = node;
            if (node.RightChild != null) node.RightChild.Parent = node;

            s = s.Substring(1);             // skip ')'

            return node;
        }

        static protected int BlackHeight { get; set; }

#endif // DEBUG
        #endregion Debugging
    }
}
