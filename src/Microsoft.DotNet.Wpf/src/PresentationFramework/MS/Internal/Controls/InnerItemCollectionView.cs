// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// Description: inner item collection view used by ItemsControl.
//                  This is a "cached" view which allows modifications while the view is
//                  sorted/filtered; it does not keep the view in sorted/filtered state.
//
// See specs at ItemsControl.mht
//

using System;
using System.Collections;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using MS.Internal.Data;

namespace MS.Internal.Controls
{
    internal sealed class InnerItemCollectionView : CollectionView, IList
    {
        // InnerItemCollectionView will return itself as SourceCollection (SourceCollection property is overridden);
        // shouldProcessCollectionChanged is turned off because this class will handle its own events.
        public InnerItemCollectionView(int capacity, ItemCollection itemCollection)
            : base(EmptyEnumerable.Instance, false)
        {
            // This list is cloned and diverged when Sort/Filter is applied.
            _rawList = _viewList = new ArrayList(capacity);
            _itemCollection = itemCollection;
        }

        //------------------------------------------------------
        //
        //  Public Interfaces
        //
        //------------------------------------------------------

        #region ICollectionView

        /// <summary>
        /// Collection of Sort criteria to sort items in this view over the SourceCollection.
        /// </summary>
        /// <remarks>
        /// <p>
        /// Simpler implementations do not support sorting and will return an empty
        /// and immutable / read-only SortDescription collection.
        /// Attempting to modify such a collection will cause NotSupportedException.
        /// Use <seealso cref="CanSort"/> property on CollectionView to test if sorting is supported
        /// before modifying the returned collection.
        /// </p>
        /// <p>
        /// One or more sort criteria in form of <seealso cref="SortDescription"/>
        /// can be added, each specifying a property and direction to sort by.
        /// </p>
        /// </remarks>
        public override SortDescriptionCollection SortDescriptions
        {
            get
            {
                if (_sort == null)
                    SetSortDescriptions(new SortDescriptionCollection());
                return _sort;
            }
        }

        /// <summary>
        /// Test if this ICollectionView supports sorting before adding
        /// to <seealso cref="SortDescriptions"/>.
        /// </summary>
        public override bool CanSort
        {
            get { return true; }
        }

        /// <summary>
        /// Return true if the item belongs to this view.  No assumptions are
        /// made about the item. This method will behave similarly to IList.Contains()
        /// and will do an exhaustive search through all items in the view.
        /// If the caller knows that the item belongs to the
        /// underlying collection, it is more efficient to call PassesFilter.
        /// </summary>
        public override bool Contains(object item)
        {
            return _viewList.Contains(item);
        }

        #endregion ICollectionView

        #region IList

        /// <summary>Gets or sets the element at the specified index.</summary>
        /// <param name="index">The zero-based index of the element to get or set.</param>
        /// <returns>The element at the specified index.</returns>
        public object this[int index]
        {
            get
            {
                return GetItemAt(index);
            }
            set
            {
                // will throw an exception if item already has a model parent
                DependencyObject node = AssertPristineModelChild(value);

                bool changingCurrentItem = (CurrentPosition == index);

                // getter checks index and will throw out of range exception
                object originalItem = _viewList[index];

                // add new item into list for now, but might be rolled back if things go wrong
                _viewList[index] = value;

                int originalIndexR = -1;
                if (IsCachedMode)
                {
                    originalIndexR = _rawList.IndexOf(originalItem);
                    _rawList[originalIndexR] = value;
                }

                // try setting model parent, be prepared to rollback item from ItemCollection
                bool isAddSuccessful = true;
                if (node != null)
                {
                    isAddSuccessful = false;
                    try
                    {
                        SetModelParent(value);
                        isAddSuccessful = true;
                    }
                    finally
                    {
                        if (!isAddSuccessful)
                        {
                            // failed to set new model parent, back new item out of collection
                            // and keep old item in collection (note: its parent hasn't been cleared yet!)
                            _viewList[index] = originalItem;
                            if (originalIndexR > 0)
                            {
                                _rawList[originalIndexR] = originalItem;
                            }
                        }
                        else
                        {
                            // was able to parent new item, now cleanup old item
                            ClearModelParent(originalItem);
                        }
                    }
                }

                if (!isAddSuccessful)
                    return;

                OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Replace, value, originalItem, index));
                SetIsModified();
            }
        }

        public bool IsReadOnly
        {
            get { return false; }
        }

        public bool IsFixedSize
        {
            get { return false; }
        }

        // always adds at the end of list and view
        public int Add(object item)
        {
            // will throw an exception if item already has a model parent
            DependencyObject node = AssertPristineModelChild(item);

            // add to collection before attempting to set model parent
            int indexV = _viewList.Add(item);
            int indexR = -1;
            if (IsCachedMode)
            {
                indexR = _rawList.Add(item);
            }

            // try setting model parent, be prepared to rollback item from ItemCollection
            bool isAddSuccessful = true;
            if (node != null)
            {
                isAddSuccessful = false;
                try
                {
                    SetModelParent(item);
                    isAddSuccessful = true;
                }
                finally
                {
                    if (!isAddSuccessful)
                    {
                        // failed to set new model parent, back item out of collection
                        _viewList.RemoveAt(indexV);
                        if (indexR >= 0)
                        {
                            _rawList.RemoveAt(indexR);
                        }
                        // also roll back the parent set
                        ClearModelParent(item);
                        indexV = -1;
                    }
                }
            }

            if (!isAddSuccessful)
                return -1;

            AdjustCurrencyForAdd(indexV);
            SetIsModified();
            OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, item, indexV));
            return indexV;
        }

        /// <summary>
        ///     Clears the collection.  Releases the references on all items
        /// currently in the collection.
        /// </summary>
        /// <exception cref="InvalidOperationException">
        /// the ItemCollection is read-only because it is in ItemsSource mode
        /// </exception>
        public void Clear()
        {
            try
            {
                for (int i = _rawList.Count - 1; i >= 0; --i)
                {
                    ClearModelParent(_rawList[i]);
                }
            }
            finally
            {
                _rawList.Clear();

                // Refresh will sync the _viewList to the cleared _rawList
                RefreshOrDefer();
            }
        }

        public void Insert(int index, object item)
        {
            // will throw an exception if item already has a model parent
            DependencyObject node = AssertPristineModelChild(item);

            // add to collection before attempting to set model parent
            _viewList.Insert(index, item);
            int indexR = -1;
            if (IsCachedMode)
            {
                indexR = _rawList.Add(item);
            }

            // try setting model parent, be prepared to rollback item from ItemCollection
            bool isAddSuccessful = true;
            if (node != null)
            {
                isAddSuccessful = false;
                try
                {
                    SetModelParent(item);
                    isAddSuccessful = true;
                }
                finally
                {
                    if (!isAddSuccessful)
                    {
                        // failed to set new model parent, back item out of collection
                        _viewList.RemoveAt(index);
                        if (indexR >= 0)
                        {
                            _rawList.RemoveAt(indexR);
                        }
                        // also roll back the parent set
                        ClearModelParent(item);
                    }
                }
            }
            if (!isAddSuccessful)
                return;

            AdjustCurrencyForAdd(index);
            SetIsModified();
            OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, item, index));
        }

        public void Remove(object item)
        {
            int indexV = _viewList.IndexOf(item);
            int indexR = -1;
            if (IsCachedMode)
            {
                indexR = _rawList.IndexOf(item);
            }

            _RemoveAt(indexV, indexR, item);
        }

        public void RemoveAt(int index)
        {
            if ((0 <= index) && (index < ViewCount))
            {
                object item = this[index];
                int indexR = -1;
                if (IsCachedMode)
                {
                    indexR = _rawList.IndexOf(item);
                }
                _RemoveAt(index, indexR, item);
            }
            else
            {
                throw new ArgumentOutOfRangeException("index",
                            SR.Get(SRID.ItemCollectionRemoveArgumentOutOfRange));
            }
        }

        #endregion IList

        #region ICollection

        /// <summary>
        /// Gets a value indicating whether access to the collection is synchronized (thread-safe).
        /// </summary>
        /// <value>true if access to the collection is synchronized (thread-safe); otherwise, false.</value>
        bool ICollection.IsSynchronized
        {
            get { return false; }
        }

        /// <summary>
        /// Gets an object that can be used to synchronize access to the view
        /// </summary>
        /// <value>an object that can be used to synchronize access to the view</value>
        object ICollection.SyncRoot
        {
            get { return _rawList.SyncRoot; }
        }

        ///<summary>
        /// Copies all the elements of the current collection (view) to the specified one-dimensional Array.
        ///</summary>
        void ICollection.CopyTo(Array array, int index)
        {
            _viewList.CopyTo(array, index);
        }

        #endregion ICollection

        //------------------------------------------------------
        //
        //  Public Properties
        //
        //------------------------------------------------------

        #region Public Properties

        public override IEnumerable SourceCollection
        {
            get { return this; }
        }

        /// <summary>
        /// Return the number of records in (filtered) view
        /// </summary>
        public override int Count
        {
            get
            {
                return ViewCount;
            }
        }

        /// <summary>
        /// Returns true if the resulting (filtered) view is emtpy.
        /// </summary>
        public override bool IsEmpty
        {
            get
            {
                return ViewCount == 0;
            }
        }

        public override bool NeedsRefresh
        {
            get
            {
                return base.NeedsRefresh || _isModified;
            }
        }

        #endregion Public Properties

        //------------------------------------------------------
        //
        //  Public Methods
        //
        //------------------------------------------------------

        #region Public Methods

        /// <summary> Return the index where the given item belongs, or -1 if this index is unknown.
        /// </summary>
        /// <remarks>
        /// If this method returns an index other than -1, it must always be true that
        /// view[index-1] &lt; item &lt;= view[index], where the comparisons are done via
        /// the view's IComparer.Compare method (if any).
        /// (This method is used by a listener's (e.g. System.Windows.Controls.ItemsControl)
        /// CollectionChanged event handler to speed up its reaction to insertion and deletion of items.
        /// If IndexOf is  not implemented, a listener does a binary search using IComparer.Compare.)
        /// </remarks>
        /// <param name="item">data item</param>
        public override int IndexOf(object item)
        {
            return _viewList.IndexOf(item);
        }

        /// <summary>
        /// Retrieve item at the given zero-based index in this CollectionView.
        /// </summary>
        /// <remarks>
        /// <p>The index is evaluated with any SortDescriptions or Filter being set on this CollectionView.</p>
        /// </remarks>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Thrown if index is out of range
        /// </exception>
        public override object GetItemAt(int index)
        {
            return _viewList[index];
        }

        /// <summary>
        /// Move <seealso cref="CollectionView.CurrentItem"/> to the given item.
        /// If the item is not found, move to BeforeFirst.
        /// </summary>
        /// <param name="item">Move CurrentItem to this item.</param>
        /// <returns>true if <seealso cref="CollectionView.CurrentItem"/> points to an item within the view.</returns>
        public override bool MoveCurrentTo(object item)
        {
            // if already on item, don't do anything
            if (ItemsControl.EqualsEx(CurrentItem, item))
            {
                // also check that we're not fooled by a false null CurrentItem
                if (item != null || IsCurrentInView)
                    return IsCurrentInView;
            }

            return MoveCurrentToPosition(IndexOf(item));
        }

        /// <summary>
        /// Move <seealso cref="CollectionView.CurrentItem"/> to the item at the given index.
        /// </summary>
        /// <param name="position">Move CurrentItem to this index</param>
        /// <returns>true if <seealso cref="CollectionView.CurrentItem"/> points to an item within the view.</returns>
        public override bool MoveCurrentToPosition(int position)
        {
            if (position < -1 || position > ViewCount)
                throw new ArgumentOutOfRangeException("position");

            if (position != CurrentPosition && OKToChangeCurrent())
            {
                bool oldIsCurrentAfterLast = IsCurrentAfterLast;
                bool oldIsCurrentBeforeFirst = IsCurrentBeforeFirst;

                _MoveCurrentToPosition(position);
                OnCurrentChanged();

                if (IsCurrentAfterLast != oldIsCurrentAfterLast)
                    OnPropertyChanged(IsCurrentAfterLastPropertyName);

                if (IsCurrentBeforeFirst != oldIsCurrentBeforeFirst)
                    OnPropertyChanged(IsCurrentBeforeFirstPropertyName);

                OnPropertyChanged(CurrentPositionPropertyName);
                OnPropertyChanged(CurrentItemPropertyName);
            }

            return IsCurrentInView;
        }

        #endregion Public Methods

        //------------------------------------------------------
        //
        //  Protected Methods
        //
        //------------------------------------------------------

        #region Protected Methods

        /// <summary>
        /// Re-create the view, using any <seealso cref="CollectionView.SortDescriptions"/> and/or <seealso cref="CollectionView.Filter"/>.
        /// </summary>
        protected override void RefreshOverride()
        {
            bool wasEmpty = IsEmpty;
            object oldCurrentItem = CurrentItem;
            bool oldIsCurrentAfterLast = IsCurrentAfterLast;
            bool oldIsCurrentBeforeFirst = IsCurrentBeforeFirst;
            int oldCurrentPosition = CurrentPosition;

            // force currency off the collection (gives user a chance to save dirty information)
            OnCurrentChanging();

            if (SortDescriptions.Count > 0 || Filter != null)
            {
                // filter the view list
                if (Filter == null)
                {
                    _viewList = new ArrayList(_rawList);
                }
                else
                {
                    // optimized for footprint: initialize to size 0 and let AL amortize cost of growth
                    _viewList = new ArrayList();
                    for (int k = 0; k < _rawList.Count; ++k)
                    {
                        if (Filter(_rawList[k]))
                            _viewList.Add(_rawList[k]);
                    }
                }

                // sort the view list
                if (_sort != null && _sort.Count > 0 && ViewCount > 0)
                {
                    SortFieldComparer.SortHelper(_viewList, new SortFieldComparer(_sort, Culture));
                }
            }
            else    // no sort or filter
            {
                _viewList = _rawList;
            }

            if (IsEmpty || oldIsCurrentBeforeFirst)
            {
                _MoveCurrentToPosition(-1);
            }
            else if (oldIsCurrentAfterLast)
            {
                _MoveCurrentToPosition(ViewCount);
            }
            else if (oldCurrentItem != null) // set currency back to old current item, or first if not found
            {
                int index = _viewList.IndexOf(oldCurrentItem);
                if (index < 0)
                {
                    index = 0;
                }
                _MoveCurrentToPosition(index);
            }

            ClearIsModified();
            OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
            OnCurrentChanged();

            if (IsCurrentAfterLast != oldIsCurrentAfterLast)
                OnPropertyChanged(IsCurrentAfterLastPropertyName);

            if (IsCurrentBeforeFirst != oldIsCurrentBeforeFirst)
                OnPropertyChanged(IsCurrentBeforeFirstPropertyName);

            if (oldCurrentPosition != CurrentPosition)
                OnPropertyChanged(CurrentPositionPropertyName);

            if (oldCurrentItem != CurrentItem)
                OnPropertyChanged(CurrentItemPropertyName);
        }

        /// <summary>
        /// Returns an object that enumerates the items in this view.
        /// </summary>
        protected override IEnumerator GetEnumerator()
        {
            return _viewList.GetEnumerator();
        }

        #endregion Protected Methods

        //------------------------------------------------------
        //
        //  Internal Properties
        //
        //------------------------------------------------------

        #region Internal Properties

        internal ItemCollection ItemCollection
        {
            get { return _itemCollection; }
        }

        internal IEnumerator LogicalChildren
        {
            get
            {
                return _rawList.GetEnumerator();
            }
        }

        #endregion Internal Properties

        //------------------------------------------------------
        //
        //  Private Properties
        //
        //------------------------------------------------------

        #region Private Properties

        internal int RawCount
        {
            get { return _rawList.Count; }
        }

        private int ViewCount
        {
            get { return _viewList.Count; }
        }

        // Cached Mode is when two lists are maintained.
        private bool IsCachedMode
        {
            get { return _viewList != _rawList; }
        }

        private FrameworkElement ModelParentFE
        {
            get { return ItemCollection.ModelParentFE; }
        }

        private bool IsCurrentInView
        {
            get
            {
                return (0 <= CurrentPosition && CurrentPosition < ViewCount);
            }
        }

        #endregion Private Properties

        //------------------------------------------------------
        //
        //  Private Methods
        //
        //------------------------------------------------------

        #region Private Methods

        // called when making any modifying action on the collection that could cause a refresh to be needed.
        private void SetIsModified()
        {
            if (IsCachedMode)
                _isModified = true;
        }

        private void ClearIsModified()
        {
            _isModified = false;
        }

        private void _RemoveAt(int index, int indexR, object item)
        {
            if (index >= 0)
                _viewList.RemoveAt(index);
            if (indexR >= 0)
                _rawList.RemoveAt(indexR);

            try
            {
                // removing the model parent could throw, but we'd be left in a consistent state:
                // item is already removed from this collection when unparenting throws
                ClearModelParent(item);
            }
            finally
            {
                if (index >= 0)
                {
                    AdjustCurrencyForRemove(index);
                    //SetIsModified();  // A Remove is not affected by the view's sort and filter
                    OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, item, index));

                    // currency has to change after firing the deletion event,
                    // so event handlers have the right picture
                    if (_currentElementWasRemoved)
                    {
                        MoveCurrencyOffDeletedElement();
                    }
                }
            }
        }

        // check that item is not already parented
        // throws an exception if already parented
        DependencyObject AssertPristineModelChild(object item)
        {
            DependencyObject node = item as DependencyObject;
            if (node == null)
            {
                return null;
            }

            // refuse a child which already has a different model parent!
            // NOTE: model tree spec would allow reparenting if the parent does not change
            //  but this code will throw: this is a efficient way to catch
            //  an attempt to add the same element twice to the collection
            if (LogicalTreeHelper.GetParent(node) != null)
            {
                throw new InvalidOperationException(SR.Get(SRID.ReparentModelChildIllegal));
            }
            return node;
        }

        // NOTE: Only change the item's logical links if the host is a Visual (bug 986386)
        void SetModelParent(object item)
        {
            // to avoid the unnecessary, expensive code in AddLogicalChild, check for DO first
            if ((ModelParentFE != null) && (item is DependencyObject))
                LogicalTreeHelper.AddLogicalChild(ModelParentFE, null, item);
        }

        // if item implements IModelTree, clear model parent
        void ClearModelParent(object item)
        {
            // ClearModelParent is also called for items that are not a DependencyObject;
            // to avoid the unnecessary, expensive code in RemoveLogicalChild, check for DO first
            if ((ModelParentFE != null) && (item is DependencyObject))
                LogicalTreeHelper.RemoveLogicalChild(ModelParentFE, null, item);
        }

        // set new SortDescription collection; rehook collection change notification handler
        private void SetSortDescriptions(SortDescriptionCollection descriptions)
        {
            if (_sort != null)
            {
                ((INotifyCollectionChanged)_sort).CollectionChanged -= new NotifyCollectionChangedEventHandler(SortDescriptionsChanged);
            }

            _sort = descriptions;

            if (_sort != null)
            {
                Invariant.Assert(_sort.Count == 0, "must be empty SortDescription collection");
                ((INotifyCollectionChanged)_sort).CollectionChanged += new NotifyCollectionChangedEventHandler(SortDescriptionsChanged);
            }
        }

        // SortDescription was added/removed, refresh CollectionView
        private void SortDescriptionsChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            RefreshOrDefer();
        }

        // Just move it.  No argument check, no events, just move current to position.
        private void _MoveCurrentToPosition(int position)
        {
            if (position < 0)
            {
                SetCurrent(null, -1);
            }
            else if (position >= ViewCount)
            {
                SetCurrent(null, ViewCount);
            }
            else
            {
                SetCurrent(_viewList[position], position);
            }
        }

        // fix up CurrentPosition and CurrentItem after a collection change
        private void AdjustCurrencyForAdd(int index)
        {
            if (index < 0)
                return;

            if (ViewCount == 1)
            {
                // added first item; set current at BeforeFirst
                SetCurrent(null, -1);
            }
            else if (index <= CurrentPosition) // adjust current index if insertion is earlier
            {
                int newCurrentPosition = CurrentPosition + 1;
                if (newCurrentPosition < ViewCount)
                {
                    // CurrentItem might be out of sync if underlying list is not INCC
                    // or if this Add is the result of a Replace (Rem + Add)
                    SetCurrent(_viewList[newCurrentPosition], newCurrentPosition);
                }
                else
                {
                    SetCurrent(null, ViewCount);
                }
            }
        }

        // fix up CurrentPosition and CurrentItem after a collection change
        private void AdjustCurrencyForRemove(int index)
        {
            if (index < 0)
                return;

            // adjust current index if deletion is earlier
            if (index < CurrentPosition)
            {
                int newCurrentPosition = CurrentPosition - 1;
                SetCurrent(_viewList[newCurrentPosition], newCurrentPosition);
            }
            // move currency off the deleted element
            else if (index == CurrentPosition)
            {
                _currentElementWasRemoved = true;
            }
        }

        // set CurrentItem to the item at CurrentPosition
        private void MoveCurrencyOffDeletedElement()
        {
            int lastPosition = ViewCount - 1;   // OK if last is -1
            // if position falls beyond last position, move back to last position
            int newPosition = (CurrentPosition < lastPosition) ? CurrentPosition : lastPosition;

            // reset this before raising events to avoid problems in re-entrancy
            _currentElementWasRemoved = false;

            // ignore cancel, there's no choice in this currency change
            OnCurrentChanging();
            // update CurrentItem to match new position
            _MoveCurrentToPosition(newPosition);

            OnCurrentChanged();
        }

        /// <summary>
        /// Helper to raise a PropertyChanged event  />).
        /// </summary>
        private void OnPropertyChanged(string propertyName)
        {
            OnPropertyChanged(new PropertyChangedEventArgs(propertyName));
        }

        #endregion Private Methods

        //------------------------------------------------------
        //
        //  Private Fields
        //
        //------------------------------------------------------

        SortDescriptionCollection _sort;
        ArrayList _viewList, _rawList;
        ItemCollection _itemCollection;
        bool _isModified;
        bool _currentElementWasRemoved = false; // true if we need to MoveCurrencyOffDeletedElement
    }
}
