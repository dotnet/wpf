// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// Description: List data structure, used for live shaping.
//

/*
    A collection view that does live shaping needs to support the following operations:
        1. Initialize from raw list of items
        2. Sort, according to the view's comparer
        3. Filter, according to the view's filter predicate
        4. Listen for changes to properties in the LiveShapingProperties lists
        5. Maintain a list of dirty items (whose properties have changed)
        6. Move dirty items to their correct position (making them clean)
        7. Expose the items as an IList (obeying the desired shaping)
        8. Raise CollectionChanged events to inform the view's clients

    This data structure helps do all of this.   It has the following features:

    A) A list of LiveShapingItems, one for each item in the source collection
    that pass the view's filter (if any).   This list is implemented as a
    set of LiveShapingBlocks, each holding a bounded number of LSItems, that are
    glued together as a balanced tree with order statistics.  The capacity of a block
    was chosen by experiment, to achieve a good tradeoff between low-overhead
    O(capacity) array-based operations within a block, and higher-overhead O(log N)
    operations of a balanced tree.  So it's fast for most individual changes,
    but still scales well when the data set is large.

    B) A second list, for the items that don't pass the filter.   This list is only
    needed when live filtering is enabled.   A property change can cause an item to
    move from one list to the other.

    C) Fingers into the list.  A finger knows its position.  When the list moves LSItems
    between blocks, the active fingers stick to their positions.

    D) Each LSItem listens to the relevant property-change events, and notifies the
    view.
*/

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;

using System.Windows;
using System.Windows.Data;

namespace MS.Internal.Data
{
    internal struct LivePropertyInfo
    {
        public LivePropertyInfo(string path, DependencyProperty dp)
        {
            _path = path;
            _dp = dp;
        }

        string _path;
        public string Path { get { return _path; } }

        DependencyProperty _dp;
        public DependencyProperty Property { get { return _dp; } }
    }

    [Flags]
    internal enum LiveShapingFlags
    {
        Sorting = 0x0001,
        Filtering = 0x0002,
        Grouping = 0x0004,
    }

    internal class LiveShapingList : IList
    {
        internal LiveShapingList(ICollectionViewLiveShaping view, LiveShapingFlags flags, IComparer comparer)
        {
            _view = view;
            _comparer = comparer;
            _isCustomSorting = !(comparer is SortFieldComparer);
            _dpFromPath = new DPFromPath();
            _root = new LiveShapingTree(this);

            if (comparer != null)
                _root.Comparison = CompareLiveShapingItems;

            _sortDirtyItems = new List<LiveShapingItem>();
            _filterDirtyItems = new List<LiveShapingItem>();
            _groupDirtyItems = new List<LiveShapingItem>();

            SetLiveShapingProperties(flags);
        }

        internal ICollectionViewLiveShaping View { get { return _view; } }

        internal Dictionary<string, DependencyProperty> ObservedProperties
        {
            get { return _dpFromPath; }
        }

        // reset the collections of properties to observe, and their
        // corresponding DPs
        internal void SetLiveShapingProperties(LiveShapingFlags flags)
        {
            int k, n;
            string path;

            _dpFromPath.BeginReset();

            // Sorting //

            // get the properties used for comparison
            SortDescriptionCollection sdc = ((ICollectionView)View).SortDescriptions;
            n = sdc.Count;
            _compInfos = new LivePropertyInfo[n];
            for (k = 0; k < n; ++k)
            {
                path = NormalizePath(sdc[k].PropertyName);
                _compInfos[k] = new LivePropertyInfo(path, _dpFromPath.GetDP(path));
            }


            if (TestLiveShapingFlag(flags, LiveShapingFlags.Sorting))
            {
                // get the list of property paths to observe
                Collection<string> sortProperties = View.LiveSortingProperties;

                if (sortProperties.Count == 0)
                {
                    // use the sort description properties
                    _sortInfos = _compInfos;
                }
                else
                {
                    // use the explicit list of properties
                    n = sortProperties.Count;
                    _sortInfos = new LivePropertyInfo[n];
                    for (k = 0; k < n; ++k)
                    {
                        path = NormalizePath(sortProperties[k]);
                        _sortInfos[k] = new LivePropertyInfo(path, _dpFromPath.GetDP(path));
                    }
                }
            }
            else
            {
                _sortInfos = new LivePropertyInfo[0];
            }


            // Filtering //

            if (TestLiveShapingFlag(flags, LiveShapingFlags.Filtering))
            {
                // get the list of property paths to observe
                Collection<string> filterProperties = View.LiveFilteringProperties;
                n = filterProperties.Count;
                _filterInfos = new LivePropertyInfo[n];
                for (k = 0; k < n; ++k)
                {
                    path = NormalizePath(filterProperties[k]);
                    _filterInfos[k] = new LivePropertyInfo(path, _dpFromPath.GetDP(path));
                }

                _filterRoot = new LiveShapingTree(this);
            }
            else
            {
                _filterInfos = new LivePropertyInfo[0];
                _filterRoot = null;
            }


            // Grouping //

            if (TestLiveShapingFlag(flags, LiveShapingFlags.Grouping))
            {
                // get the list of property paths to observe
                Collection<string> groupingProperties = View.LiveGroupingProperties;

                if (groupingProperties.Count == 0)
                {
                    // if no explicit list, use the group description properties
                    groupingProperties = new Collection<string>();
                    ICollectionView icv = View as ICollectionView;
                    ObservableCollection<GroupDescription> groupDescriptions = (icv != null) ? icv.GroupDescriptions : null;

                    if (groupDescriptions != null)
                    {
                        foreach (GroupDescription gd in groupDescriptions)
                        {
                            PropertyGroupDescription pgd = gd as PropertyGroupDescription;
                            if (pgd != null)
                            {
                                groupingProperties.Add(pgd.PropertyName);
                            }
                        }
                    }
                }

                n = groupingProperties.Count;
                _groupInfos = new LivePropertyInfo[n];
                for (k = 0; k < n; ++k)
                {
                    path = NormalizePath(groupingProperties[k]);
                    _groupInfos[k] = new LivePropertyInfo(path, _dpFromPath.GetDP(path));
                }
            }
            else
            {
                _groupInfos = new LivePropertyInfo[0];
            }

            _dpFromPath.EndReset();
        }

        bool TestLiveShapingFlag(LiveShapingFlags flags, LiveShapingFlags flag)
        {
            return (flags & flag) != 0;
        }

        // Search for value in the slice of the list starting at index with length count,
        // using the given comparer.  The list is assumed to be sorted w.r.t. the
        // comparer.  Return the index if found, or the bit-complement
        // of the index where it would belong.
        internal int Search(int index, int count, object value)
        {
            LiveShapingItem temp = new LiveShapingItem(value, this, true, null, true);
            RBFinger<LiveShapingItem> finger = _root.BoundedSearch(temp, index, index + count);
            ClearItem(temp);

            return finger.Found ? finger.Index : ~finger.Index;
        }

        // Sort the list, using the comparer supplied at creation
        internal void Sort()
        {
            _root.Sort();
        }

        internal int CompareLiveShapingItems(LiveShapingItem x, LiveShapingItem y)
        {
#if LiveShapingInstrumentation
            ++_comparisons;
#endif

            if (x == y || System.Windows.Controls.ItemsControl.EqualsEx(x.Item, y.Item))
                return 0;

            int result = 0;

            if (!_isCustomSorting)
            {
                // intercept SortFieldComparer, and do the comparisons here.
                // The LiveShapingItems will cache the field values.
                SortFieldComparer sfc = _comparer as SortFieldComparer;
                SortDescriptionCollection sdc = ((ICollectionView)View).SortDescriptions;
                Debug.Assert(sdc.Count >= _compInfos.Length, "SortDescriptions don't match LivePropertyInfos");
                int n = _compInfos.Length;

                for (int k = 0; k < n; ++k)
                {
                    object v1 = x.GetValue(_compInfos[k].Path, _compInfos[k].Property);
                    object v2 = y.GetValue(_compInfos[k].Path, _compInfos[k].Property);

                    result = sfc.BaseComparer.Compare(v1, v2);
                    if (sdc[k].Direction == ListSortDirection.Descending)
                        result = -result;

                    if (result != 0)
                        break;
                }
            }
            else
            {
                // for custom comparers, just compare items the normal way
                result = _comparer.Compare(x.Item, y.Item);
            }

            return result;
        }

        // Move an item from one position to another
        internal void Move(int oldIndex, int newIndex)
        {
            _root.Move(oldIndex, newIndex);
        }

        // Restore sorted order by insertion sort, raising an event for each move
        internal void RestoreLiveSortingByInsertionSort(Action<NotifyCollectionChangedEventArgs, int, int> RaiseMoveEvent)
        {
            // the collection view suppresses some actions while we're restoring sorted order
            _isRestoringLiveSorting = true;
            _root.RestoreLiveSortingByInsertionSort(RaiseMoveEvent);
            _isRestoringLiveSorting = false;
        }

        // Add an item to the filtered list
        internal void AddFilteredItem(object item)
        {
            LiveShapingItem lsi = new LiveShapingItem(item, this, true) { FailsFilter = true };
            _filterRoot.Insert(_filterRoot.Count, lsi);
        }

        // Add an item to the filtered list
        internal void AddFilteredItem(LiveShapingItem lsi)
        {
            InitializeItem(lsi, lsi.Item, true, false);
            lsi.FailsFilter = true;
            _filterRoot.Insert(_filterRoot.Count, lsi);
        }

        // if item appears on the filtered list, set its LSI's starting index
        // to the given value.  This supports duplicate items in the original list;
        // when a property changes, all the duplicates may become un-filtered at
        // the same time, and we need to insert the copies at different places.
        internal void SetStartingIndexForFilteredItem(object item, int value)
        {
            foreach (LiveShapingItem lsi in _filterDirtyItems)
            {
                if (System.Windows.Controls.ItemsControl.EqualsEx(item, lsi.Item))
                {
                    lsi.StartingIndex = value;
                    return;
                }
            }
        }

        // Remove an item from the filtered list
        internal void RemoveFilteredItem(LiveShapingItem lsi)
        {
            _filterRoot.RemoveAt(_filterRoot.IndexOf(lsi));
            ClearItem(lsi);
        }

        // Remove an item from the filtered list
        internal void RemoveFilteredItem(object item)
        {
            LiveShapingItem lsi = _filterRoot.FindItem(item);
            if (lsi != null)
            {
                RemoveFilteredItem(lsi);
            }
        }

        // Replace an item in the filtered list
        internal void ReplaceFilteredItem(object oldItem, object newItem)
        {
            LiveShapingItem lsi = _filterRoot.FindItem(oldItem);
            if (lsi != null)
            {
                ClearItem(lsi);
                InitializeItem(lsi, newItem, true, false);
            }
        }

        // Find a given LiveShapingItem
        internal int IndexOf(LiveShapingItem lsi)
        {
            return _root.IndexOf(lsi);
        }


        // initialize a new LiveShapingItem
        internal void InitializeItem(LiveShapingItem lsi, object item, bool filtered, bool oneTime)
        {
            lsi.Item = item;

            if (!filtered)
            {
                foreach (LivePropertyInfo info in _sortInfos)
                {
                    // the item may raise a cross-thread PropertyChanged after
                    // the binding is set, but before the LSI is added to the list.
                    // If so, the LSI needs a different way to find the list,
                    // namely a placeholder block that points
                    // directly to the root.
                    lsi.Block = _root.PlaceholderBlock;
                    lsi.SetBinding(info.Path, info.Property, oneTime, true);
                }
                foreach (LivePropertyInfo info in _groupInfos)
                {
                    lsi.SetBinding(info.Path, info.Property, oneTime);
                }
            }

            foreach (LivePropertyInfo info in _filterInfos)
            {
                lsi.SetBinding(info.Path, info.Property, oneTime);
            }

            lsi.ForwardChanges = !oneTime;
        }


        // clear a LiveShapingItem
        internal void ClearItem(LiveShapingItem lsi)
        {
            lsi.ForwardChanges = false;

            foreach (DependencyProperty dp in ObservedProperties.Values)
            {
                BindingOperations.ClearBinding(lsi, dp);
            }
        }


        string NormalizePath(string path)
        {
            return String.IsNullOrEmpty(path) ? String.Empty : path;
        }


        internal void OnItemPropertyChanged(LiveShapingItem lsi, DependencyProperty dp)
        {
            if (ContainsDP(_sortInfos, dp) && !lsi.FailsFilter && !lsi.IsSortPendingClean)
            {
                lsi.IsSortDirty = true;
                lsi.IsSortPendingClean = true;
                _sortDirtyItems.Add(lsi);
                OnLiveShapingDirty();

#if DEBUG
                _root.CheckSort = false;
#endif
            }

            if (ContainsDP(_filterInfos, dp) && !lsi.IsFilterDirty)
            {
                lsi.IsFilterDirty = true;
                _filterDirtyItems.Add(lsi);
                OnLiveShapingDirty();
            }

            if (ContainsDP(_groupInfos, dp) && !lsi.FailsFilter && !lsi.IsGroupDirty)
            {
                lsi.IsGroupDirty = true;
                _groupDirtyItems.Add(lsi);
                OnLiveShapingDirty();
            }
        }

        // called when a property change affecting lsi occurs on a foreign thread
        internal void OnItemPropertyChangedCrossThread(LiveShapingItem lsi, DependencyProperty dp)
        {
            // we only care about DPs that affect sorting, when a custom sort is in
            // effect.  In that case, we must mark the item as dirty so that it doesn't
            // participate in comparisons.
            // For all other cases, we can wait until the change arrives on the UI
            // thread.   This is even true for sorting, when using the SortFieldComparer,
            // because in that case the comparisons use the lsi's cached property values
            // which don't change until the UI thread gets the underlying item's
            // property-change.

            if (_isCustomSorting && ContainsDP(_sortInfos, dp) && !lsi.FailsFilter)
            {
                lsi.IsSortDirty = true;

#if DEBUG
                _root.CheckSort = false;
#endif
            }
        }

        internal event EventHandler LiveShapingDirty;

        void OnLiveShapingDirty()
        {
            if (LiveShapingDirty != null)
                LiveShapingDirty(this, EventArgs.Empty);
        }

        bool ContainsDP(LivePropertyInfo[] infos, DependencyProperty dp)
        {
            for (int k = 0; k < infos.Length; ++k)
            {
                if (infos[k].Property == dp ||
                    (dp == null && String.IsNullOrEmpty(infos[k].Path)))
                {
                    return true;
                }
            }

            return false;
        }

        internal void FindPosition(LiveShapingItem lsi, out int oldIndex, out int newIndex)
        {
            _root.FindPosition(lsi, out oldIndex, out newIndex);
        }

        internal List<LiveShapingItem> SortDirtyItems { get { return _sortDirtyItems; } }
        internal List<LiveShapingItem> FilterDirtyItems { get { return _filterDirtyItems; } }
        internal List<LiveShapingItem> GroupDirtyItems { get { return _groupDirtyItems; } }

        internal LiveShapingItem ItemAt(int index) { return _root[index]; }

        internal bool IsRestoringLiveSorting { get { return _isRestoringLiveSorting; } }

        #region IList Members

        public int Add(object value)
        {
            Insert(Count, value);
            return Count;
        }

        public void Clear()
        {
            ForEach((x) => { ClearItem(x); });
            _root = new LiveShapingTree(this);
        }

        public bool Contains(object value)
        {
            return (IndexOf(value) >= 0);
        }

        public int IndexOf(object value)
        {
            int result = 0;
            ForEachUntil((x) =>
               {
                   if (System.Windows.Controls.ItemsControl.EqualsEx(value, x.Item))
                       return true;
                   ++result;
                   return false;
               });
            return (result < Count) ? result : -1;
        }

        public void Insert(int index, object value)
        {
            _root.Insert(index, new LiveShapingItem(value, this));
        }

        public bool IsFixedSize
        {
            get { return false; }
        }

        public bool IsReadOnly
        {
            get { return false; }
        }

        public void Remove(object value)
        {
            int index = IndexOf(value);
            if (index >= 0)
            {
                RemoveAt(index);
            }
        }

        public void RemoveAt(int index)
        {
            LiveShapingItem lsi = _root[index];
            _root.RemoveAt(index);
            ClearItem(lsi);
            lsi.IsDeleted = true;
        }

        public object this[int index]
        {
            get
            {
                return _root[index].Item;
            }
            set
            {
                _root.ReplaceAt(index, value);
            }
        }

        #endregion

#if LiveShapingInstrumentation

        public void ResetComparisons()
        {
            _comparisons = 0;
        }

        public void ResetCopies()
        {
            _root.ResetCopies();
        }

        public void ResetAverageCopy()
        {
            _root.ResetAverageCopy();
        }

        public int GetComparisons()
        {
            return _comparisons;
        }

        public int GetCopies()
        {
            return _root.GetCopies();
        }

        public double GetAverageCopy()
        {
            return _root.GetAverageCopy();
        }

        int _comparisons;

#endif // LiveShapingInstrumentation

        #region ICollection Members

        public void CopyTo(Array array, int index)
        {
            throw new NotImplementedException();
        }

        public int Count
        {
            get { return _root.Count; }
        }

        public bool IsSynchronized
        {
            get { return false; }
        }

        public object SyncRoot
        {
            get { return null; }
        }

        #endregion

        #region IEnumerable Members

        public IEnumerator GetEnumerator()
        {
            return new ItemEnumerator(_root.GetEnumerator());
        }

        #endregion

        #region Private Methods

        void ForEach(Action<LiveShapingItem> action)
        {
            _root.ForEach(action);
        }

        void ForEachUntil(Func<LiveShapingItem, bool> action)
        {
            _root.ForEachUntil(action);
        }

        #endregion

        #region Debugging
#if DEBUG

        internal bool VerifyLiveSorting(LiveShapingItem lsi)
        {
            if (lsi == null)
            {   // the list should now be fully sorted again
                _root.CheckSort = true;
                return _root.Verify(_root.Count);
            }
            else
            {
                return _root.VerifyPosition(lsi);
            }
        }

#else
        internal bool VerifyLiveSorting(LiveShapingItem lsi) { return true; }
#endif // DEBUG
        #endregion Debugging

        #region Private Types

        class DPFromPath : Dictionary<String, DependencyProperty>
        {
            public void BeginReset()
            {
                _unusedKeys = new List<string>(this.Keys);
                _dpIndex = 0;
            }

            public void EndReset()
            {
                foreach (string s in _unusedKeys)
                {
                    Remove(s);
                }

                _unusedKeys = null;
            }

            public DependencyProperty GetDP(string path)
            {
                DependencyProperty dp;

                if (TryGetValue(path, out dp))
                {
                    // we've seen this path before - use the same DP
                    _unusedKeys.Remove(path);
                    return dp;
                }
                else
                {
                    // for a new path, get an unused DP
                    ICollection<DependencyProperty> usedDPs = this.Values;
                    for (; _dpIndex < s_dpList.Count; ++_dpIndex)
                    {
                        dp = s_dpList[_dpIndex];
                        if (!usedDPs.Contains(dp))
                        {
                            this[path] = dp;
                            return dp;
                        }
                    }

                    // no unused DPs available - allocate a new DP
                    lock (s_Sync)
                    {
                        dp = DependencyProperty.RegisterAttached(
                            String.Format(System.Windows.Markup.TypeConverterHelper.InvariantEnglishUS,
                                            "LiveSortingTargetProperty{0}",
                                            s_dpList.Count),
                            typeof(object),
                            typeof(LiveShapingList));

                        s_dpList.Add(dp);
                    }

                    this[path] = dp;
                    return dp;
                }
            }

            List<string> _unusedKeys;
            int _dpIndex;
        }

        class ItemEnumerator : IEnumerator
        {
            public ItemEnumerator(IEnumerator<LiveShapingItem> ie)
            {
                _ie = ie;
            }

            void IEnumerator.Reset()
            {
                _ie.Reset();
            }

            bool IEnumerator.MoveNext()
            {
                return _ie.MoveNext();
            }

            object IEnumerator.Current
            {
                get { return _ie.Current.Item; }
            }

            IEnumerator<LiveShapingItem> _ie;
        }

        #endregion

        #region Private Data

        ICollectionViewLiveShaping _view;          // my owner
        DPFromPath _dpFromPath;    // map of Path -> DP
        LivePropertyInfo[] _compInfos;     // properties for comparing
        LivePropertyInfo[] _sortInfos;     // properties for sorting
        LivePropertyInfo[] _filterInfos;   // properties for filtering
        LivePropertyInfo[] _groupInfos;    // properties for grouping
        IComparer _comparer;      // comparer - for sort/search

        LiveShapingTree _root;          // root of the balanced tree
        LiveShapingTree _filterRoot;    // root of tree for filtered items

        List<LiveShapingItem> _sortDirtyItems;    // list of items needing sorting fixup
        List<LiveShapingItem> _filterDirtyItems;  // list of items needing filtering fixup
        List<LiveShapingItem> _groupDirtyItems;   // list of items needing grouping fixup

        bool _isRestoringLiveSorting;    // true while restoring order
        bool _isCustomSorting;   // true if not using SortFieldComparer

        static List<DependencyProperty> s_dpList = new List<DependencyProperty>();
        // static list of DPs, shared by all instances of lists
        static object s_Sync = new object();  // lock for s_dpList

        #endregion
    }
}
