// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// Description: Root of the RB tree used for live shaping.
//

using System;
using System.Collections;
using System.Collections.Specialized;

namespace MS.Internal.Data
{
    internal class LiveShapingTree : RBTree<LiveShapingItem>
    {
        internal LiveShapingTree(LiveShapingList list)
        {
            _list = list;
        }

        internal LiveShapingList List { get { return _list; } }

        internal LiveShapingBlock PlaceholderBlock
        {
            get
            {
                if (_placeholderBlock == null)
                {
                    _placeholderBlock = new LiveShapingBlock(false);
                    _placeholderBlock.Parent = this;
                }
                return _placeholderBlock;
            }
        }

        internal override RBNode<LiveShapingItem> NewNode()
        {
            return new LiveShapingBlock();
        }

        internal void Move(int oldIndex, int newIndex)
        {
            LiveShapingItem lsi = this[oldIndex];
            RemoveAt(oldIndex);
            Insert(newIndex, lsi);
        }

        // re-implementation of RBTree.InsertionSort, with a little extra work
        internal void RestoreLiveSortingByInsertionSort(Action<NotifyCollectionChangedEventArgs, int, int> RaiseMoveEvent)
        {
            RBFinger<LiveShapingItem> finger = FindIndex(0);
            while (finger.Node != this)
            {
                LiveShapingItem lsi = finger.Item;
                lsi.IsSortDirty = false;
                lsi.IsSortPendingClean = false;

                int oldIndex, newIndex;

                RBFinger<LiveShapingItem> fingerL = LocateItem(finger, Comparison);

                oldIndex = finger.Index;
                newIndex = fingerL.Index;

                if (oldIndex != newIndex)
                {
                    ReInsert(ref finger, fingerL);

                    RaiseMoveEvent(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Move,
                                                                        lsi.Item, oldIndex, newIndex),
                                    oldIndex, newIndex);
                }

                ++finger;
            }
        }

        internal void FindPosition(LiveShapingItem lsi, out int oldIndex, out int newIndex)
        {
            RBFinger<LiveShapingItem> oldFinger, newFinger;
            lsi.FindPosition(out oldFinger, out newFinger, Comparison);

            oldIndex = oldFinger.Index;
            newIndex = newFinger.Index;
        }

        internal void ReplaceAt(int index, object item)
        {
            RBFinger<LiveShapingItem> finger = FindIndex(index);
            LiveShapingItem lsi = finger.Item;
            lsi.Clear();
            finger.Node.SetItemAt(finger.Offset, new LiveShapingItem(item, List));
        }

        // linear search - only called when removing a filtered item
        internal LiveShapingItem FindItem(object item)
        {
            RBFinger<LiveShapingItem> finger = FindIndex(0);
            while (finger.Node != this)
            {
                if (System.Windows.Controls.ItemsControl.EqualsEx(finger.Item.Item, item))
                    return finger.Item;
                ++finger;
            }
            return null;
        }

        public override int IndexOf(LiveShapingItem lsi)
        {
            RBFinger<LiveShapingItem> finger = lsi.GetFinger();
            return finger.Found ? finger.Index : -1;
        }

        #region Debugging
#if DEBUG

        // check the allegedly restored item against its neighbors
        internal bool VerifyPosition(LiveShapingItem lsi)
        {
            bool result = true;

            RBFinger<LiveShapingItem> finger = lsi.GetFinger();

            // we deliberately test the comparison *before* checking IsSortDirty
            // to handle the case where a second thread changes a sort property
            // during the check.

            if (finger.Index > 0)
            {
                --finger;
                result = result &&
                    !(  Comparison(finger.Item, lsi) > 0 &&
                        !lsi.IsSortDirty && !finger.Item.IsSortDirty);
                ++finger;
            }

            if (finger.Index < Count-1)
            {
                ++finger;
                result = result &&
                    !(  Comparison(lsi, finger.Item) > 0 &&
                        !lsi.IsSortDirty && !finger.Item.IsSortDirty);
            }

            return result;
        }

#endif // DEBUG
        #endregion Debugging

#if LiveShapingInstrumentation

        public static void SetQuickSortThreshold(int threshold)
        {
            QuickSortThreshold = threshold;
        }

        public static void SetBinarySearchThreshold(int threshold)
        {
            BinarySearchThreshold = threshold;
        }


        public void ResetCopies()
        {
            LiveShapingBlock._copies = 0;
        }

        public void ResetAverageCopy()
        {
            LiveShapingBlock._totalCopySize = 0;
        }

        public int GetCopies()
        {
            return LiveShapingBlock._copies;
        }

        public double GetAverageCopy()
        {
            int copies = LiveShapingBlock._copies;
            return (copies > 0) ? (double)LiveShapingBlock._totalCopySize / copies : copies;
        }

#endif // LiveShapingInstrumentation

        LiveShapingList _list;      // my owner
        LiveShapingBlock _placeholderBlock; // used to handle a race condition arising in live sorting
    }
}
