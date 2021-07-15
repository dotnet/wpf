// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// Description: A CollectionViewGroupInternal, as created by a CollectionView according to a GroupDescription.
//
// See spec at Grouping.mht
//

using System;
using System.Collections;       // IEnumerator
using System.ComponentModel;    // PropertyChangedEventArgs, GroupDescription
using System.Diagnostics;       // Debug
using System.Windows;           // DependencyProperty.UnsetValue
using System.Windows.Data;      // CollectionViewGroup
using System.Windows.Threading; // Dispatcher

namespace MS.Internal.Data
{
    internal class CollectionViewGroupInternal : CollectionViewGroup
    {
        #region Constructors

        //------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------

        internal CollectionViewGroupInternal(object name, CollectionViewGroupInternal parent, bool isExplicit = false) : base(name)
        {
            _parentGroup = parent;
            _isExplicit = isExplicit;
        }

        #endregion Constructors

        #region Public Properties

        //------------------------------------------------------
        //
        //  Public Properties
        //
        //------------------------------------------------------

        /// <summary>
        /// Is this group at the bottom level (not further subgrouped).
        /// </summary>
        public override bool IsBottomLevel
        {
            get { return (_groupBy == null); }
        }

        #endregion  Public Properties

        #region Internal Properties

        //------------------------------------------------------
        //
        //  Internal Properties
        //
        //------------------------------------------------------

        // how this group divides into subgroups
        internal GroupDescription GroupBy
        {
            get { return _groupBy; }
            set
            {
                bool oldIsBottomLevel = IsBottomLevel;

                if (_groupBy != null)
                {
                    PropertyChangedEventManager.RemoveHandler(_groupBy, OnGroupByChanged, String.Empty);
                }

                _groupBy = value;

                if (_groupBy != null)
                {
                    PropertyChangedEventManager.AddHandler(_groupBy, OnGroupByChanged, String.Empty);
                }

                // choose a comparer based on info in the GroupDescription and the owning collection view
                _groupComparer = (_groupBy == null) ? null :
                    ListCollectionView.PrepareComparer(
                        _groupBy.CustomSort,
                        _groupBy.SortDescriptionsInternal,
                        () =>
                        {
                            for (CollectionViewGroupInternal group = this;
                                    group != null;
                                    group = group.Parent)
                            {
                                CollectionViewGroupRoot root = group as CollectionViewGroupRoot;
                                if (root != null)
                                {
                                    return root.View;
                                }
                            }
                            return null;    // this should never happen - root should always be present
                        });

                if (oldIsBottomLevel != IsBottomLevel)
                {
                    OnPropertyChanged(new PropertyChangedEventArgs("IsBottomLevel"));
                }
            }
        }

        // the number of items and groups in the subtree under this group
        internal int FullCount
        {
            get { return _fullCount; }
            set { _fullCount = value; }
        }

        // the most recent index where actvity took place
        internal int LastIndex
        {
            get { return _lastIndex; }
            set { _lastIndex = value; }
        }

        // the first item (leaf) added to this group.  If this can't be determined,
        // DependencyProperty.UnsetValue.
        internal object SeedItem
        {
            get
            {
                if (ItemCount > 0 && (GroupBy == null || GroupBy.GroupNames.Count == 0))
                {
                    // look for first item, child by child
                    for (int k = 0, n = Items.Count; k < n; ++k)
                    {
                        CollectionViewGroupInternal subgroup = Items[k] as CollectionViewGroupInternal;
                        if (subgroup == null)
                        {
                            // child is an item - return it
                            return Items[k];
                        }
                        else if (subgroup.ItemCount > 0)
                        {
                            // child is a nonempty subgroup - ask it
                            return subgroup.SeedItem;
                        }
                        // otherwise child is an empty subgroup - go to next child
                    }

                    // we shouldn't get here, but just in case...
                    return DependencyProperty.UnsetValue;
                }
                else
                {
                    // the group is empty, or it has explicit subgroups.
                    // In either case, we cannot determine the first item -
                    // it could have gone into any of the subgroups.
                    return DependencyProperty.UnsetValue;
                }
            }
        }

        #endregion Properties

        #region Internal methods

        //------------------------------------------------------
        //
        //  Internal methods
        //
        //------------------------------------------------------

        internal void Add(object item)
        {
            if (_groupComparer == null)
            {
                ChangeCounts(item, +1);
                ProtectedItems.Add(item);
            }
            else
            {
                Insert(item, null, null);
            }
        }

        internal int Remove(object item, bool returnLeafIndex)
        {
            int index = -1;
            int localIndex = ProtectedItems.IndexOf(item);

            if (localIndex >= 0)
            {
                if (returnLeafIndex)
                {
                    index = LeafIndexFromItem(null, localIndex);
                }

                CollectionViewGroupInternal subGroup = item as CollectionViewGroupInternal;
                if (subGroup != null)
                {
                    subGroup.Clear();

                    // Remove from the name to group map.
                    RemoveSubgroupFromMap(subGroup);
                }

                ChangeCounts(item, -1);

                // ChangeCounts may clear this group, if it is now empty.
                // In that case, don't use localIndex - it's now out of range.
                if (ProtectedItems.Count > 0)
                {
                    ProtectedItems.RemoveAt(localIndex);
                }
            }

            return index;
        }

        internal void Clear()
        {
            // reset the counts before delving into subgroups.   The subgroup
            // changes can incur re-entrant calls to LeafAt(index) which will
            // get out-of-range exceptions, unless we fend them
            // off by ensuring that count<=index.
            FullCount = 1;
            ProtectedItemCount = 0;

            if (_groupBy != null)
            {
                // This group has subgroups.  Disconnect from GroupDescription events
                PropertyChangedEventManager.RemoveHandler(_groupBy, OnGroupByChanged, String.Empty);
                _groupBy = null;

                // recursively clear subgroups
                for (int i = 0, n = ProtectedItems.Count; i < n; ++i)
                {
                    CollectionViewGroupInternal subGroup = ProtectedItems[i] as CollectionViewGroupInternal;
                    if (subGroup != null)
                    {
                        subGroup.Clear();
                    }
                }
            }

            ProtectedItems.Clear();
            if (_nameToGroupMap != null)
            {
                _nameToGroupMap.Clear();
            }
        }

        // return the index of the given item within the list of leaves governed
        // by this group
        internal int LeafIndexOf(object item)
        {
            int leaves = 0;         // number of leaves we've passed over so far
            for (int k = 0, n = Items.Count; k < n; ++k)
            {
                CollectionViewGroupInternal subgroup = Items[k] as CollectionViewGroupInternal;
                if (subgroup != null)
                {
                    int subgroupIndex = subgroup.LeafIndexOf(item);
                    if (subgroupIndex < 0)
                    {
                        leaves += subgroup.ItemCount;       // item not in this subgroup
                    }
                    else
                    {
                        return (leaves + subgroupIndex);    // item is in this subgroup
                    }
                }
                else
                {
                    // current item is a leaf - compare it directly
                    if (System.Windows.Controls.ItemsControl.EqualsEx(item, Items[k]))
                    {
                        return leaves;
                    }
                    else
                    {
                        leaves += 1;
                    }
                }
            }

            // item not found
            return -1;
        }

        // return the index of the given item within the list of leaves governed
        // by the full group structure.  The item must be a (direct) child of this
        // group.  The caller provides the index of the item within this group,
        // if known, or -1 if not.
        internal int LeafIndexFromItem(object item, int index)
        {
            int result = 0;

            // accumulate the number of predecessors at each level
            for (CollectionViewGroupInternal group = this;
                    group != null;
                    item = group, group = group.Parent, index = -1)
            {
                // accumulate the number of predecessors at the level of item
                for (int k = 0, n = group.Items.Count; k < n; ++k)
                {
                    // if we've reached the item, move up to the next level
                    if ((index < 0 && System.Windows.Controls.ItemsControl.EqualsEx(item, group.Items[k])) ||
                        index == k)
                    {
                        break;
                    }

                    // accumulate leaf count
                    CollectionViewGroupInternal subgroup = group.Items[k] as CollectionViewGroupInternal;
                    result += (subgroup == null) ? 1 : subgroup.ItemCount;
                }
            }

            return result;
        }


        // return the item at the given index within the list of leaves governed
        // by this group
        internal object LeafAt(int index)
        {
            for (int k = 0, n = Items.Count; k < n; ++k)
            {
                CollectionViewGroupInternal subgroup = Items[k] as CollectionViewGroupInternal;
                if (subgroup != null)
                {
                    // current item is a group - either drill in, or skip over
                    if (index < subgroup.ItemCount)
                    {
                        return subgroup.LeafAt(index);
                    }
                    else
                    {
                        index -= subgroup.ItemCount;
                    }
                }
                else
                {
                    // current item is a leaf - see if we're done
                    if (index == 0)
                    {
                        return Items[k];
                    }
                    else
                    {
                        index -= 1;
                    }
                }
            }

            // the loop should have found the index.  We shouldn't get here.
            throw new ArgumentOutOfRangeException("index");
        }

        // return an enumerator over the leaves governed by this group
        internal IEnumerator GetLeafEnumerator()
        {
            return new LeafEnumerator(this);
        }

        // insert a new item or subgroup and return its index.  Seed is a
        // representative from the subgroup (or the item itself) that
        // is used to position the new item/subgroup w.r.t. the order given
        // by the comparer.  (If comparer is null, just add at the end).
        internal int Insert(object item, object seed, IComparer comparer)
        {
            // when group sorting is not declared,
            // never insert the new item/group before the explicit subgroups
            int low = 0;
            if (_groupComparer == null && GroupBy != null)
            {
                low = GroupBy.GroupNames.Count;
            }

            int index = FindIndex(item, seed, comparer, low, ProtectedItems.Count);

            // now insert the item
            ChangeCounts(item, +1);
            ProtectedItems.Insert(index, item);

            return index;
        }

        protected virtual int FindIndex(object item, object seed, IComparer comparer, int low, int high)
        {
            int index;

            if (_groupComparer == null)
            {
                // group sorting is not declared - find the position using the seed
                if (comparer != null)
                {
                    IListComparer ilc = comparer as IListComparer;
                    if (ilc != null)
                    {
                        // reset the IListComparer before each search.  This cannot be done
                        // any less frequently (e.g. in Root.AddToSubgroups), due to the
                        // possibility that the item may appear in more than one subgroup.
                        ilc.Reset();
                    }

                    for (index = low; index < high; ++index)
                    {
                        CollectionViewGroupInternal subgroup = ProtectedItems[index] as CollectionViewGroupInternal;
                        object seed1 = (subgroup != null) ? subgroup.SeedItem : ProtectedItems[index];
                        if (seed1 == DependencyProperty.UnsetValue)
                            continue;
                        if (comparer.Compare(seed, seed1) < 0)
                            break;
                    }
                }
                else
                {
                    index = high;
                }
            }
            else
            {
                // group sorting is declared - find the position using the local comparer
                for (index = low; index < high; ++index)
                {
                    if (_groupComparer.Compare(item, ProtectedItems[index]) < 0)
                        break;
                }
            }

            return index;
        }

        // move an item and return true if it really moved.
        // Also translate the indices to "leaf" coordinates
        internal bool Move(object item, IList list, ref int oldIndex, ref int newIndex)
        {
            int oldIndexLocal = -1, newIndexLocal = -1;
            int localIndex = 0;
            int n = ProtectedItems.Count;

            // the input is in "full" coordinates.  Find the corresponding local coordinates
            for (int fullIndex = 0; ; ++fullIndex)
            {
                if (fullIndex == oldIndex)
                {
                    oldIndexLocal = localIndex;
                    if (newIndexLocal >= 0)
                        break;
                    ++localIndex;
                }

                if (fullIndex == newIndex)
                {
                    newIndexLocal = localIndex;
                    if (oldIndexLocal >= 0)
                    {
                        --newIndexLocal;
                        break;
                    }
                    ++fullIndex;
                    ++oldIndex;
                }

                if (localIndex < n && System.Windows.Controls.ItemsControl.EqualsEx(ProtectedItems[localIndex], list[fullIndex]))
                    ++localIndex;
            }

            // the move may be a no-op w.r.t. this group
            if (oldIndexLocal == newIndexLocal)
                return false;

            // translate to "leaf" coordinates
            int low, high, lowLeafIndex, delta = 0;
            if (oldIndexLocal < newIndexLocal)
            {
                low = oldIndexLocal + 1;
                high = newIndexLocal + 1;
                lowLeafIndex = LeafIndexFromItem(null, oldIndexLocal);
            }
            else
            {
                low = newIndexLocal;
                high = oldIndexLocal;
                lowLeafIndex = LeafIndexFromItem(null, newIndexLocal);
            }

            for (int i = low; i < high; ++i)
            {
                CollectionViewGroupInternal subgroup = Items[i] as CollectionViewGroupInternal;
                delta += (subgroup == null) ? 1 : subgroup.ItemCount;
            }

            if (oldIndexLocal < newIndexLocal)
            {
                oldIndex = lowLeafIndex;
                newIndex = oldIndex + delta;
            }
            else
            {
                newIndex = lowLeafIndex;
                oldIndex = newIndex + delta;
            }

            // do the actual move
            ProtectedItems.Move(oldIndexLocal, newIndexLocal);
            return true;
        }

        // the group's description has changed - notify parent
        protected virtual void OnGroupByChanged()
        {
            if (Parent != null)
                Parent.OnGroupByChanged();
        }

        /// <summary>
        ///     Maps the given name with the given subgroup
        /// </summary>
        internal void AddSubgroupToMap(object nameKey, CollectionViewGroupInternal subgroup)
        {
            Debug.Assert(subgroup != null);
            if (nameKey == null)
            {
                // use null name place holder.
                nameKey = _nullGroupNameKey;
            }
            if (_nameToGroupMap == null)
            {
                _nameToGroupMap = new Hashtable();
            }
            // Add to the map. Use WeakReference to avoid memory leaks
            // in case some one calls ProtectedItems.Remove instead of
            // CollectionViewGroupInternal.Remove
            _nameToGroupMap[nameKey] = new WeakReference(subgroup);
            ScheduleMapCleanup();
        }

        /// <summary>
        ///     Removes the given subgroup from the name to group map.
        /// </summary>
        private void RemoveSubgroupFromMap(CollectionViewGroupInternal subgroup)
        {
            Debug.Assert(subgroup != null);
            if (_nameToGroupMap == null)
            {
                return;
            }
            object keyToBeRemoved = null;

            // Search for the subgroup in the map.
            foreach (object key in _nameToGroupMap.Keys)
            {
                WeakReference weakRef = _nameToGroupMap[key] as WeakReference;
                if (weakRef != null &&
                    weakRef.Target == subgroup)
                {
                    keyToBeRemoved = key;
                    break;
                }
            }
            if (keyToBeRemoved != null)
            {
                _nameToGroupMap.Remove(keyToBeRemoved);
            }
            ScheduleMapCleanup();
        }

        /// <summary>
        ///     Tries to find the subgroup for the name from the map.
        /// </summary>
        internal CollectionViewGroupInternal GetSubgroupFromMap(object nameKey)
        {
            if (_nameToGroupMap != null)
            {
                if (nameKey == null)
                {
                    // use null name place holder.
                    nameKey = _nullGroupNameKey;
                }
                // Find and return the subgroup
                WeakReference weakRef = _nameToGroupMap[nameKey] as WeakReference;
                if (weakRef != null)
                {
                    return (weakRef.Target as CollectionViewGroupInternal);
                }
            }
            return null;
        }

        /// <summary>
        ///     Schedules a dispatcher operation to clean up the map
        ///     of garbage collected weak references.
        /// </summary>
        private void ScheduleMapCleanup()
        {
            if (!_mapCleanupScheduled)
            {
                _mapCleanupScheduled = true;
                Dispatcher.CurrentDispatcher.BeginInvoke(
                    (Action)delegate ()
                    {
                        _mapCleanupScheduled = false;
                        if (_nameToGroupMap != null)
                        {
                            ArrayList keysToBeRemoved = new ArrayList();
                            foreach (object key in _nameToGroupMap.Keys)
                            {
                                WeakReference weakRef = _nameToGroupMap[key] as WeakReference;
                                if (weakRef == null || !weakRef.IsAlive)
                                {
                                    keysToBeRemoved.Add(key);
                                }
                            }
                            foreach (object key in keysToBeRemoved)
                            {
                                _nameToGroupMap.Remove(key);
                            }
                        }
                    },
                    DispatcherPriority.ContextIdle);
            }
        }

        #endregion Internal methods

        #region Internal Types

        // this comparer is used to insert an item into a group in a position consistent
        // with a given IList.  It only works when used in the pattern that FindIndex
        // uses, namely first call Reset(), then call Compare(item, x) any number of
        // times with the same item (the new item) as the first argument, and a sequence
        // of x's as the second argument that appear in the IList in the same sequence.
        // This makes the total search time linear in the size of the IList.  (To give
        // the correct answer regardless of the sequence of arguments would involve
        // calling IndexOf and leads to O(N^2) total search time.)

        internal class IListComparer : IComparer
        {
            internal IListComparer(IList list)
            {
                ResetList(list);
            }

            internal void Reset()
            {
                _index = 0;
            }

            internal void ResetList(IList list)
            {
                _list = list;
                _index = 0;
            }

            public int Compare(object x, object y)
            {
                if (System.Windows.Controls.ItemsControl.EqualsEx(x, y))
                    return 0;

                // advance the index until seeing one x or y
                int n = (_list != null) ? _list.Count : 0;
                for (; _index < n; ++_index)
                {
                    object z = _list[_index];
                    if (System.Windows.Controls.ItemsControl.EqualsEx(x, z))
                    {
                        return -1;  // x occurs first, so x < y
                    }
                    else if (System.Windows.Controls.ItemsControl.EqualsEx(y, z))
                    {
                        return +1;  // y occurs first, so x > y
                    }
                }

                // if we don't see either x or y, declare x > y.
                // This has the effect of putting x at the end of the list.
                return +1;
            }

            int _index;
            IList _list;
        }

        #endregion Internal Types

        #region Private Properties

        //------------------------------------------------------
        //
        //  Private Properties
        //
        //------------------------------------------------------

        internal CollectionViewGroupInternal Parent
        {
            get { return _parentGroup; }
        }

        private bool IsExplicit
        {
            get { return _isExplicit; }
        }

        #endregion Private Properties

        #region Private methods

        //------------------------------------------------------
        //
        //  Private methods
        //
        //------------------------------------------------------

        protected void ChangeCounts(object item, int delta)
        {
            bool changeLeafCount = !(item is CollectionViewGroup);

            using (EmptyGroupRemover remover = EmptyGroupRemover.Create(isNeeded: changeLeafCount && delta < 0))
            {
                for (CollectionViewGroupInternal group = this;
                        group != null;
                        group = group._parentGroup)
                {
                    group.FullCount += delta;
                    if (changeLeafCount)
                    {
                        group.ProtectedItemCount += delta;

                        if (group.ProtectedItemCount == 0)
                        {
                            remover.RemoveEmptyGroup(group);
                        }
                    }
                }
            }

            unchecked { ++_version; }     // this invalidates enumerators
        }

        void OnGroupByChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            OnGroupByChanged();
        }

        #endregion Private methods

        #region Private fields

        //------------------------------------------------------
        //
        //  Private fields
        //
        //------------------------------------------------------

        GroupDescription _groupBy;
        CollectionViewGroupInternal _parentGroup;
        IComparer _groupComparer;
        int _fullCount = 1;
        int _lastIndex;
        int _version;       // for detecting stale enumerators
        Hashtable _nameToGroupMap; // To cache the mapping between name and subgroup
        bool _mapCleanupScheduled = false;
        bool _isExplicit;
        static NamedObject _nullGroupNameKey = new NamedObject("NullGroupNameKey");

        #endregion Private fields

        #region Private classes

        //------------------------------------------------------
        //
        //  Private classes
        //
        //------------------------------------------------------

        private class LeafEnumerator : IEnumerator
        {
            public LeafEnumerator(CollectionViewGroupInternal group)
            {
                _group = group;
                DoReset();  // don't call virtual Reset in ctor
            }

            void IEnumerator.Reset()
            {
                DoReset();
            }

            void DoReset()
            {
                _version = _group._version;
                _index = -1;
                _subEnum = null;
            }

            bool IEnumerator.MoveNext()
            {
                // check for invalidated enumerator
                if (_group._version != _version)
                    throw new InvalidOperationException();

                // move forward to the next leaf
                while (_subEnum == null || !_subEnum.MoveNext())
                {
                    // done with the current top-level item.  Move to the next one.
                    ++_index;
                    if (_index >= _group.Items.Count)
                        return false;

                    CollectionViewGroupInternal subgroup = _group.Items[_index] as CollectionViewGroupInternal;
                    if (subgroup == null)
                    {
                        // current item is a leaf - it's the new Current
                        _current = _group.Items[_index];
                        _subEnum = null;
                        return true;
                    }
                    else
                    {
                        // current item is a subgroup - get its enumerator
                        _subEnum = subgroup.GetLeafEnumerator();
                    }
                }

                // the loop terminates only when we have a subgroup enumerator
                // positioned at the new Current item
                _current = _subEnum.Current;
                return true;
            }

            object IEnumerator.Current
            {
                get
                {
                    if (_index < 0 || _index >= _group.Items.Count)
                        throw new InvalidOperationException();
                    return _current;
                }
            }

            CollectionViewGroupInternal _group; // parent group
            int _version;   // parent group's version at ctor
            int _index;     // current index into Items
            IEnumerator _subEnum;   // enumerator over current subgroup
            object _current;   // current item
        }

        // When removing a leaf item, ChangeCounts removes groups that become empty.
        // It's important to propagate the changed item count all the way up
        // to the root before actually removing the empty groups, as the removal
        // raises an event whose handlers could call back into the group tree -
        // if the counts are inconsistent, crashes can occur.
        // This class remembers the empty groups, and removes them after the
        // counts have all been changed.
        private class EmptyGroupRemover : IDisposable
        {
            public static EmptyGroupRemover Create(bool isNeeded)
            {
                return isNeeded ? new EmptyGroupRemover() : null;
            }

            public void RemoveEmptyGroup(CollectionViewGroupInternal group)
            {
                if (_toRemove == null)
                {
                    _toRemove = new System.Collections.Generic.List<CollectionViewGroupInternal>();
                }
                _toRemove.Add(group);
            }

            public void Dispose()
            {
                if (_toRemove != null)
                {
                    foreach (CollectionViewGroupInternal group in _toRemove)
                    {
                        CollectionViewGroupInternal parent = group.Parent;

                        if (parent != null)
                        {
                            // remove the subgroup unless it is one of the explicit groups
                            if (!group.IsExplicit)
                            {
                                parent.Remove(group, false);
                            }
                        }
                    }
                }
            }

            System.Collections.Generic.List<CollectionViewGroupInternal> _toRemove;
        }

        #endregion Private classes
    }
}
