// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// See spec at CollectionView.mht
//

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Threading;

using MS.Internal;
using MS.Internal.Data;
using MS.Utility;

namespace System.Windows.Data
{
    ///<summary>
    /// <seealso cref="ICollectionView"/> based on and associated to <seealso cref="IBindingList"/>
    /// and <seealso cref="IBindingListView"/>, namely ADO DataViews.
    ///</summary>
    public sealed class BindingListCollectionView : CollectionView, IComparer, IEditableCollectionView, ICollectionViewLiveShaping, IItemProperties
    {
        //------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------

        #region Constructors

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="list">Underlying IBindingList</param>
        public BindingListCollectionView(IBindingList list)
            : base(list)
        {
            InternalList = list;
            _blv = list as IBindingListView;
            _isDataView = SystemDataHelper.IsDataView(list);

            SubscribeToChanges();

            _group = new CollectionViewGroupRoot(this);
            _group.GroupDescriptionChanged += new EventHandler(OnGroupDescriptionChanged);
            ((INotifyCollectionChanged)_group).CollectionChanged += new NotifyCollectionChangedEventHandler(OnGroupChanged);
            ((INotifyCollectionChanged)_group.GroupDescriptions).CollectionChanged += new NotifyCollectionChangedEventHandler(OnGroupByChanged);
        }

        #endregion Constructors



        //------------------------------------------------------
        //
        //  Public Methods
        //
        //------------------------------------------------------

        #region Public Methods

        //------------------------------------------------------
        #region ICollectionView

        /// <summary>
        /// Return true if the item belongs to this view.  The item is assumed to belong to the
        /// underlying DataCollection;  this method merely takes filters into account.
        /// It is commonly used during collection-changed notifications to determine if the added/removed
        /// item requires processing.
        /// Returns true if no filter is set on collection view.
        /// </summary>
        public override bool PassesFilter(object item)
        {
            if (IsCustomFilterSet)
                return Contains(item);  // need to ask inner list, not cheap but only way to determine
            else
                return true;    // every item is contained
        }

        /// <summary>
        /// Return true if the item belongs to this view.  No assumptions are
        /// made about the item. This method will behave similarly to IList.Contains().
        /// </summary>
        public override bool Contains(object item)
        {
            VerifyRefreshNotDeferred();

            return (item == NewItemPlaceholder) ? (NewItemPlaceholderPosition != NewItemPlaceholderPosition.None)
                                                : CollectionProxy.Contains(item);
        }

        /// <summary>
        /// Move <seealso cref="CollectionView.CurrentItem"/> to the item at the given index.
        /// </summary>
        /// <param name="position">Move CurrentItem to this index</param>
        /// <returns>true if <seealso cref="CollectionView.CurrentItem"/> points to an item within the view.</returns>
        public override bool MoveCurrentToPosition(int position)
        {
            VerifyRefreshNotDeferred();

            if (position < -1 || position > InternalCount)
                throw new ArgumentOutOfRangeException("position");

            _MoveTo(position);
            return IsCurrentInView;
        }

        #endregion ICollectionView


        //------------------------------------------------------
        #region IComparer

        /// <summary> Return -, 0, or +, according to whether o1 occurs before, at, or after o2 (respectively)
        /// </summary>
        /// <param name="o1">first object</param>
        /// <param name="o2">second object</param>
        /// <remarks>
        /// Compares items by their resp. index in the IList.
        /// </remarks>
        int IComparer.Compare(object o1, object o2)
        {
            int i1 = InternalIndexOf(o1);
            int i2 = InternalIndexOf(o2);
            return (i1 - i2);
        }

        #endregion IComparer

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
            VerifyRefreshNotDeferred();

            return InternalIndexOf(item);
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
            VerifyRefreshNotDeferred();

            return InternalItemAt(index);
        }

        /// <summary>
        /// Implementation of IEnumerable.GetEnumerator().
        /// This provides a way to enumerate the members of the collection
        /// without changing the currency.
        /// </summary>
        protected override IEnumerator GetEnumerator()
        {
            VerifyRefreshNotDeferred();

            return InternalGetEnumerator();
        }

        /// <summary>
        /// Detach from the source collection.  (I.e. stop listening to the collection's
        /// events, or anything else that makes the CollectionView ineligible for
        /// garbage collection.)
        /// </summary>
        public override void DetachFromSourceCollection()
        {
            if (InternalList != null && InternalList.SupportsChangeNotification)
            {
                InternalList.ListChanged -= new ListChangedEventHandler(OnListChanged);
            }

            InternalList = null;

            base.DetachFromSourceCollection();
        }

        #endregion Public Methods


        //------------------------------------------------------
        //
        //  Public Properties
        //
        //------------------------------------------------------

        #region Public Properties

        //------------------------------------------------------
        #region ICollectionView

        /// <summary>
        /// Collection of Sort criteria to sort items in this view over the inner IBindingList.
        /// </summary>
        /// <remarks>
        /// <p>
        /// If the underlying SourceCollection only implements IBindingList,
        /// then only one sort criteria in form of a <seealso cref="SortDescription"/>
        /// can be added, specifying a property and direction to sort by.
        /// Adding more than one SortDescription will cause a InvalidOperationException.
        /// One such class is Generic BindingList
        /// </p>
        /// <p>
        /// Classes like ADO's DataView (the view around a DataTable) do implement
        /// IBindingListView which can support sorting by more than one property
        /// and also filtering <seealso cref="CustomFilter" />
        /// </p>
        /// <p>
        /// Some IBindingList implementations do not support sorting; for those this property
        /// will return an empty and immutable / read-only SortDescription collection.
        /// Attempting to modify such a collection will cause NotSupportedException.
        /// Use <seealso cref="CanSort"/> property on this CollectionView to test if sorting is supported
        /// before modifying the returned collection.
        /// </p>
        /// </remarks>
        public override SortDescriptionCollection SortDescriptions
        {
            get
            {
                if (InternalList.SupportsSorting)
                {
                    if (_sort == null)
                    {
                        bool allowAdvancedSorting = _blv != null && _blv.SupportsAdvancedSorting;
                        _sort = new BindingListSortDescriptionCollection(allowAdvancedSorting);
                        ((INotifyCollectionChanged)_sort).CollectionChanged += new NotifyCollectionChangedEventHandler(SortDescriptionsChanged);
                    }
                    return _sort;
                }
                else
                    return SortDescriptionCollection.Empty;
            }
        }

        /// <summary>
        /// Test if this ICollectionView supports sorting before adding
        /// to <seealso cref="SortDescriptions"/>.
        /// </summary>
        /// <remarks>
        /// ListCollectionView does implement an IComparer based sorting.
        /// </remarks>
        public override bool CanSort
        {
            get
            {
                return InternalList.SupportsSorting;
            }
        }

        private IComparer ActiveComparer
        {
            get { return _comparer; }
            set
            {
                _comparer = value;
            }
        }

        /// <summary>
        /// BindingListCollectionView does not support callback-based filtering.
        /// Use <seealso cref="CustomFilter" /> instead.
        /// </summary>
        public override bool CanFilter
        {
            get
            {
                return false;
            }
        }

        /// <summary>
        /// Gets or sets the filter to be used to exclude items from the collection of items returned by the data source .
        /// </summary>
        /// <remarks>
        /// Before assigning, test if this CollectionView supports custom filtering
        /// <seealso cref="CanCustomFilter"/>.
        /// The actual syntax depends on the implementer of IBindingListView. ADO's DataView is
        /// a common example, see System.Data.DataView.RowFilter for its supported
        /// filter expression syntax.
        /// </remarks>
        public string CustomFilter
        {
            get { return _customFilter; }
            set
            {
                if (!CanCustomFilter)
                    throw new NotSupportedException(SR.Get(SRID.BindingListCannotCustomFilter));
                if (IsAddingNew || IsEditingItem)
                    throw new InvalidOperationException(SR.Get(SRID.MemberNotAllowedDuringAddOrEdit, "CustomFilter"));
                if (AllowsCrossThreadChanges)
                    VerifyAccess();

                _customFilter = value;

                RefreshOrDefer();
            }
        }

        /// <summary>
        /// Test if this CollectionView supports custom filtering before assigning
        /// a filter string to <seealso cref="CustomFilter"/>.
        /// </summary>
        public bool CanCustomFilter
        {
            get
            {
                return ((_blv != null) && _blv.SupportsFiltering);
            }
        }

        /// <summary>
        /// Returns true if this view really supports grouping.
        /// When this returns false, the rest of the interface is ignored.
        /// </summary>
        public override bool CanGroup
        {
            get { return true; }
        }

        /// <summary>
        /// The description of grouping, indexed by level.
        /// </summary>
        public override ObservableCollection<GroupDescription> GroupDescriptions
        {
            get { return _group.GroupDescriptions; }
        }

        /// <summary>
        /// The top-level groups, constructed according to the descriptions
        /// given in GroupDescriptions and/or GroupBySelector.
        /// </summary>
        public override ReadOnlyObservableCollection<object> Groups
        {
            get { return (_isGrouping) ? _group.Items : null; }
        }

        #endregion ICollectionView

        /// <summary>
        /// A delegate to select the group description as a function of the
        /// parent group and its level.
        /// </summary>
        [DefaultValue(null)]
        public GroupDescriptionSelectorCallback GroupBySelector
        {
            get { return _group.GroupBySelector; }
            set
            {
                if (!CanGroup)
                    throw new NotSupportedException();
                if (IsAddingNew || IsEditingItem)
                    throw new InvalidOperationException(SR.Get(SRID.MemberNotAllowedDuringAddOrEdit, "GroupBySelector"));

                _group.GroupBySelector = value;

                RefreshOrDefer();
            }
        }

        /// <summary>
        /// Return the estimated number of records (or -1, meaning "don't know").
        /// </summary>
        public override int Count
        {
            get
            {
                VerifyRefreshNotDeferred();

                return InternalCount;
            }
        }

        /// <summary>
        /// Returns true if the resulting (filtered) view is emtpy.
        /// </summary>
        public override bool IsEmpty
        {
            get { return (NewItemPlaceholderPosition == NewItemPlaceholderPosition.None &&
                            CollectionProxy.Count == 0); }
        }

        /// <summary>
        /// Setting this to true informs the view that the list of items
        /// (after applying the sort and filter, if any) is already in the
        /// correct order for grouping.  This allows the view to use a more
        /// efficient algorithm to build the groups.
        /// </summary>
        public bool IsDataInGroupOrder
        {
            get { return _group.IsDataInGroupOrder; }
            set { _group.IsDataInGroupOrder = value; }
        }

        #endregion Public Properties

        #region IEditableCollectionView

        #region Adding new items

        /// <summary>
        /// Indicates whether to include a placeholder for a new item, and if so,
        /// where to put it.
        /// </summary>
        public NewItemPlaceholderPosition NewItemPlaceholderPosition
        {
            get { return _newItemPlaceholderPosition; }
            set
            {
                VerifyRefreshNotDeferred();

                if (value != _newItemPlaceholderPosition && IsAddingNew)
                    throw new InvalidOperationException(SR.Get(SRID.MemberNotAllowedDuringTransaction, "NewItemPlaceholderPosition", "AddNew"));

                if (value != _newItemPlaceholderPosition && _isRemoving)
                {
                    DeferAction(() => { NewItemPlaceholderPosition = value; });
                    return;
                }

                NotifyCollectionChangedEventArgs args = null;
                int oldIndex=-1, newIndex=-1;

                // we're adding, removing, or moving the placeholder.
                // Determine the appropriate events.
                switch (value)
                {
                    case NewItemPlaceholderPosition.None:
                        switch (_newItemPlaceholderPosition)
                        {
                            case NewItemPlaceholderPosition.None:
                                break;
                            case NewItemPlaceholderPosition.AtBeginning:
                                oldIndex = 0;
                                args = new NotifyCollectionChangedEventArgs(
                                                NotifyCollectionChangedAction.Remove,
                                                NewItemPlaceholder,
                                                oldIndex);
                                break;
                            case NewItemPlaceholderPosition.AtEnd:
                                oldIndex = InternalCount - 1;
                                args = new NotifyCollectionChangedEventArgs(
                                                NotifyCollectionChangedAction.Remove,
                                                NewItemPlaceholder,
                                                oldIndex);
                                break;
                        }
                        break;

                    case NewItemPlaceholderPosition.AtBeginning:
                        switch (_newItemPlaceholderPosition)
                        {
                            case NewItemPlaceholderPosition.None:
                                newIndex = 0;
                                args = new NotifyCollectionChangedEventArgs(
                                                NotifyCollectionChangedAction.Add,
                                                NewItemPlaceholder,
                                                newIndex);
                                break;
                            case NewItemPlaceholderPosition.AtBeginning:
                                break;
                            case NewItemPlaceholderPosition.AtEnd:
                                oldIndex = InternalCount - 1;
                                newIndex = 0;
                                args = new NotifyCollectionChangedEventArgs(
                                                NotifyCollectionChangedAction.Move,
                                                NewItemPlaceholder,
                                                newIndex,
                                                oldIndex);
                                break;
                        }
                        break;

                    case NewItemPlaceholderPosition.AtEnd:
                        switch (_newItemPlaceholderPosition)
                        {
                            case NewItemPlaceholderPosition.None:
                                newIndex = InternalCount;
                                args = new NotifyCollectionChangedEventArgs(
                                                NotifyCollectionChangedAction.Add,
                                                NewItemPlaceholder,
                                                newIndex);
                                break;
                            case NewItemPlaceholderPosition.AtBeginning:
                                oldIndex = 0;
                                newIndex = InternalCount - 1;
                                args = new NotifyCollectionChangedEventArgs(
                                                NotifyCollectionChangedAction.Move,
                                                NewItemPlaceholder,
                                                newIndex,
                                                oldIndex);
                                break;
                            case NewItemPlaceholderPosition.AtEnd:
                                break;
                        }
                        break;
                }

                // now make the change and raise the events
                if (args != null)
                {
                    _newItemPlaceholderPosition = value;

                    if (!_isGrouping)
                    {
                        base.OnCollectionChanged(null, args);
                    }
                    else
                    {
                        if (oldIndex >= 0)
                        {
                            int index = (oldIndex == 0) ? 0 : _group.Items.Count - 1;
                            _group.RemoveSpecialItem(index, NewItemPlaceholder, false /*loading*/);
                        }
                        if (newIndex >= 0)
                        {
                            int index = (newIndex == 0) ? 0 : _group.Items.Count;
                            _group.InsertSpecialItem(index, NewItemPlaceholder, false /*loading*/);
                        }
                    }

                    OnPropertyChanged("NewItemPlaceholderPosition");
                }
            }
        }

        /// <summary>
        /// Return true if the view supports <seealso cref="AddNew"/>.
        /// </summary>
        public bool CanAddNew
        {
            get { return !IsEditingItem && InternalList.AllowNew; }
        }

        /// <summary>
        /// Add a new item to the underlying collection.  Returns the new item.
        /// After calling AddNew and changing the new item as desired, either
        /// <seealso cref="CommitNew"/> or <seealso cref="CancelNew"/> should be
        /// called to complete the transaction.
        /// </summary>
        public object AddNew()
        {
            VerifyRefreshNotDeferred();

            if (IsEditingItem)
            {
                CommitEdit();   // implicitly close a previous EditItem
            }

            CommitNew();        // implicitly close a previous AddNew

            if (!CanAddNew)
                throw new InvalidOperationException(SR.Get(SRID.MemberNotAllowedForView, "AddNew"));

            object newItem = null;
            BindingOperations.AccessCollection(InternalList,
                () =>
                {
                    ProcessPendingChanges();

                    _newItemIndex = -2; // this is a signal that the next ItemAdded event comes from AddNew
                    newItem = InternalList.AddNew();
                },
                true);

            Debug.Assert(_newItemIndex != -2 && newItem == _newItem, "AddNew did not raise expected events");

            MoveCurrentTo(newItem);

            ISupportInitialize isi = newItem as ISupportInitialize;
            if (isi != null)
            {
                isi.BeginInit();
            }

            // DataView.AddNew calls BeginEdit on the new item, but other implementations
            // of IBL don't.  Make up for them.
            if (!IsDataView)
            {
                IEditableObject ieo = newItem as IEditableObject;
                if (ieo != null)
                {
                    ieo.BeginEdit();
                }
            }

            return newItem;
        }

        // Calling IBL.AddNew() will raise an ItemAdded event.  We handle this specially
        // to adjust the position of the new item in the view (it should be adjacent
        // to the placeholder), and cache the new item for use by the other APIs
        // related to AddNew.  This method is called from ProcessCollectionChanged.
        // The index gives the adjusted position of the newItem in the view;  this
        // differs from its position in the source collection by 1 if we've added
        // a placeholder at the beginning.
        void BeginAddNew(object newItem, int index)
        {
            Debug.Assert(_newItemIndex == -2 && _newItem == NoNewItem, "unexpected call to BeginAddNew");

            // remember the new item and its position in the underlying list
            SetNewItem(newItem);
            _newItemIndex = index;

            // adjust the position of the new item
            // (not needed when grouping, as we'll be inserting into the group structure)
            int position = index;
            if (!_isGrouping)
            {
                switch (NewItemPlaceholderPosition)
                {
                    case NewItemPlaceholderPosition.None:
                        break;
                    case NewItemPlaceholderPosition.AtBeginning:
                        -- _newItemIndex;
                        position = 1;
                        break;
                    case NewItemPlaceholderPosition.AtEnd:
                        position = InternalCount - 2;
                        break;
                }
            }

            // raise events as if the new item appeared in the adjusted position
            ProcessCollectionChanged(new NotifyCollectionChangedEventArgs(
                                            NotifyCollectionChangedAction.Add,
                                            newItem,
                                            position));
        }

        /// <summary>
        /// Complete the transaction started by <seealso cref="AddNew"/>.  The new
        /// item remains in the collection, and the view's sort, filter, and grouping
        /// specifications (if any) are applied to the new item.
        /// </summary>
        public void CommitNew()
        {
            if (IsEditingItem)
                throw new InvalidOperationException(SR.Get(SRID.MemberNotAllowedDuringTransaction, "CommitNew", "EditItem"));
            VerifyRefreshNotDeferred();

            if (_newItem == NoNewItem)
                return;

            // commit the new item
            ICancelAddNew ican = InternalList as ICancelAddNew;
            IEditableObject ieo;

            BindingOperations.AccessCollection(InternalList,
                () =>
                {
                    ProcessPendingChanges();

                    if (ican != null)
                    {
                        ican.EndNew(_newItemIndex);
                    }
                    else if ((ieo = _newItem as IEditableObject) != null)
                    {
                        ieo.EndEdit();
                    }
                },
                true);

            // DataView raises events that cause us to update the view
            // correctly (including leaving AddNew mode).  BindingList<T> does not
            // raise these events.  If they haven't happened, do the work now.
            if (_newItem != NoNewItem)
            {
                int delta = (NewItemPlaceholderPosition == NewItemPlaceholderPosition.AtBeginning) ? 1 : 0;
                NotifyCollectionChangedEventArgs args = ProcessCommitNew(_newItemIndex, _newItemIndex + delta);
                if (args != null)
                {
                    base.OnCollectionChanged(InternalList, args);
                }
            }
        }

        /// <summary>
        /// Complete the transaction started by <seealso cref="AddNew"/>.  The new
        /// item is removed from the collection.
        /// </summary>
        public void CancelNew()
        {
            if (IsEditingItem)
                throw new InvalidOperationException(SR.Get(SRID.MemberNotAllowedDuringTransaction, "CancelNew", "EditItem"));
            VerifyRefreshNotDeferred();

            if (_newItem == NoNewItem)
                return;

            // cancel the AddNew
            ICancelAddNew ican = InternalList as ICancelAddNew;
            IEditableObject ieo;

            BindingOperations.AccessCollection(InternalList,
                () =>
                {
                    ProcessPendingChanges();

                    if (ican != null)
                    {
                        ican.CancelNew(_newItemIndex);
                    }
                    else if ((ieo = _newItem as IEditableObject) != null)
                    {
                        ieo.CancelEdit();
                    }
                },
                true);

            // DataView raises events that cause us to update the view
            // correctly (including leaving AddNew mode).  BindingList<T> does not
            // raise these events.  If they haven't happened, do the work now.
            if (_newItem != NoNewItem)
            {
                Debug.Assert(true);
            }
        }

        // Common functionality used by CommitNew, CancelNew, and when the
        // new item is removed by Remove or Refresh.
        object EndAddNew(bool cancel)
        {
            object newItem = _newItem;

            SetNewItem(NoNewItem);  // leave "adding-new" mode

            IEditableObject ieo = newItem as IEditableObject;
            if (ieo != null)
            {
                if (cancel)
                {
                    ieo.CancelEdit();
                }
                else
                {
                    ieo.EndEdit();
                }
            }

            ISupportInitialize isi = newItem as ISupportInitialize;
            if (isi != null)
            {
                isi.EndInit();
            }

            return newItem;
        }

        NotifyCollectionChangedEventArgs ProcessCommitNew(int fromIndex, int toIndex)
        {
            if (_isGrouping)
            {
                CommitNewForGrouping();
                return null;
            }

            // CommitNew either causes the list to raise an event, or not.
            // In either case, leave AddNew mode and raise a Move event if needed.
            switch (NewItemPlaceholderPosition)
            {
                case NewItemPlaceholderPosition.None:
                    break;
                case NewItemPlaceholderPosition.AtBeginning:
                    fromIndex = 1;
                    break;
                case NewItemPlaceholderPosition.AtEnd:
                    fromIndex = InternalCount - 2;
                    break;
            }

            object newItem = EndAddNew(false);

            NotifyCollectionChangedEventArgs result = null;
            if (fromIndex != toIndex)
            {
                result = new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Move, newItem, toIndex, fromIndex);
            }

            return result;
        }

        void CommitNewForGrouping()
        {
            // for grouping we cannot pretend that the new item moves to a different position,
            // since it may actually appear in several new positions (belonging to several groups).
            // Instead, we remove the item from its temporary position, then add it to the groups
            // as if it had just been added to the underlying collection.
            int index;
            switch (NewItemPlaceholderPosition)
            {
                case NewItemPlaceholderPosition.None:
                default:
                    index = _group.Items.Count - 1;
                    break;
                case NewItemPlaceholderPosition.AtBeginning:
                    index = 1;
                    break;
                case NewItemPlaceholderPosition.AtEnd:
                    index = _group.Items.Count - 2;
                    break;
            }

            // End the AddNew transaction
            object newItem = EndAddNew(false);

            // remove item from its temporary position
            _group.RemoveSpecialItem(index, newItem, false /*loading*/);

            // add it to the groups
            AddItemToGroups(newItem);
        }

        /// <summary>
        /// Returns true if an </seealso cref="AddNew"> transaction is in progress.
        /// </summary>
        public bool IsAddingNew
        {
            get { return (_newItem != NoNewItem); }
        }

        /// <summary>
        /// When an </seealso cref="AddNew"> transaction is in progress, this property
        /// returns the new item.  Otherwise it returns null.
        /// </summary>
        public object CurrentAddItem
        {
            get { return IsAddingNew ? _newItem : null; }
        }

        void SetNewItem(object item)
        {
            if (!System.Windows.Controls.ItemsControl.EqualsEx(item, _newItem))
            {
                _newItem = item;

                OnPropertyChanged("CurrentAddItem");
                OnPropertyChanged("IsAddingNew");
                OnPropertyChanged("CanRemove");
            }
        }

        #endregion Adding new items

        #region Removing items

        /// <summary>
        /// Return true if the view supports <seealso cref="Remove"/> and
        /// <seealso cref="RemoveAt"/>.
        /// </summary>
        public bool CanRemove
        {
            get { return !IsEditingItem && !IsAddingNew && InternalList.AllowRemove; }
        }

        /// <summary>
        /// Remove the item at the given index from the underlying collection.
        /// The index is interpreted with respect to the view (not with respect to
        /// the underlying collection).
        /// </summary>
        public void RemoveAt(int index)
        {
            if (IsEditingItem || IsAddingNew)
                throw new InvalidOperationException(SR.Get(SRID.MemberNotAllowedDuringAddOrEdit, "RemoveAt"));
            VerifyRefreshNotDeferred();

            RemoveImpl(GetItemAt(index), index);
        }

        /// <summary>
        /// Remove the given item from the underlying collection.
        /// </summary>
        public void Remove(object item)
        {
            if (IsEditingItem || IsAddingNew)
                throw new InvalidOperationException(SR.Get(SRID.MemberNotAllowedDuringAddOrEdit, "Remove"));
            VerifyRefreshNotDeferred();

            int index = InternalIndexOf(item);
            if (index >= 0)
            {
                RemoveImpl(item, index);
            }
        }

        void RemoveImpl(object item, int index)
        {
            if (item == CollectionView.NewItemPlaceholder)
                throw new InvalidOperationException(SR.Get(SRID.RemovingPlaceholder));

            BindingOperations.AccessCollection(InternalList,
                () =>
                {
                    ProcessPendingChanges();

                    // the pending changes may have moved (or even removed) the
                    // item.   Verify the index.
                    if (index >= InternalList.Count || !System.Windows.Controls.ItemsControl.EqualsEx(item, GetItemAt(index)))
                    {
                        index = InternalList.IndexOf(item);
                        if (index < 0)
                            return;
                    }

                    // convert the index from "view-relative" to "list-relative"
                    if (_isGrouping)
                    {
                        index = InternalList.IndexOf(item);
                    }
                    else
                    {
                        int delta = (NewItemPlaceholderPosition == NewItemPlaceholderPosition.AtBeginning) ? 1 : 0;
                        index = index - delta;
                    }

                    // remove the item from the list
                    try
                    {
                        _isRemoving = true;
                        InternalList.RemoveAt(index);
                    }
                    finally
                    {
                        _isRemoving = false;
                        DoDeferredActions();
                    }
                },
                true);
        }

        #endregion Removing items

        #region Transactional editing of an item

        /// <summary>
        /// Begins an editing transaction on the given item.  The transaction is
        /// completed by calling either <seealso cref="CommitEdit"/> or
        /// <seealso cref="CancelEdit"/>.  Any changes made to the item during
        /// the transaction are considered "pending", provided that the view supports
        /// the notion of "pending changes" for the given item.
        /// </summary>
        public void EditItem(object item)
        {
            VerifyRefreshNotDeferred();

            if (item == NewItemPlaceholder)
                throw new ArgumentException(SR.Get(SRID.CannotEditPlaceholder), "item");

            if (IsAddingNew)
            {
                if (System.Windows.Controls.ItemsControl.EqualsEx(item, _newItem))
                    return;     // EditItem(newItem) is a no-op

                CommitNew();    // implicitly close a previous AddNew
            }

            CommitEdit();   // implicitly close a previous EditItem transaction

            SetEditItem(item);

            IEditableObject ieo = item as IEditableObject;
            if (ieo != null)
            {
                ieo.BeginEdit();
            }
        }

        /// <summary>
        /// Complete the transaction started by <seealso cref="EditItem"/>.
        /// The pending changes (if any) to the item are committed.
        /// </summary>
        public void CommitEdit()
        {
            if (IsAddingNew)
                throw new InvalidOperationException(SR.Get(SRID.MemberNotAllowedDuringTransaction, "CommitEdit", "AddNew"));
            VerifyRefreshNotDeferred();

            if (_editItem == null)
                return;

            IEditableObject ieo = _editItem as IEditableObject;
            object editItem = _editItem;
            SetEditItem(null);

            if (ieo != null)
            {
                BindingOperations.AccessCollection(InternalList,
                    () =>
                    {
                        ProcessPendingChanges();
                        ieo.EndEdit();
                    },
                    true);
            }

            // editing may change the item's group names (and we can't tell whether
            // it really did).  The best we can do is remove the item and re-insert
            // it.
            if (_isGrouping)
            {
                RemoveItemFromGroups(editItem);
                AddItemToGroups(editItem);
                return;
            }
        }

        /// <summary>
        /// Complete the transaction started by <seealso cref="EditItem"/>.
        /// The pending changes (if any) to the item are discarded.
        /// </summary>
        public void CancelEdit()
        {
            if (IsAddingNew)
                throw new InvalidOperationException(SR.Get(SRID.MemberNotAllowedDuringTransaction, "CancelEdit", "AddNew"));
            VerifyRefreshNotDeferred();

            if (_editItem == null)
                return;

            IEditableObject ieo = _editItem as IEditableObject;
            SetEditItem(null);

            if (ieo != null)
            {
                ieo.CancelEdit();
            }
            else
                throw new InvalidOperationException(SR.Get(SRID.CancelEditNotSupported));
        }

        private void ImplicitlyCancelEdit()
        {
            IEditableObject ieo = _editItem as IEditableObject;
            SetEditItem(null);

            if (ieo != null)
            {
                ieo.CancelEdit();
            }
        }

        /// <summary>
        /// Returns true if the view supports the notion of "pending changes" on the
        /// current edit item.  This may vary, depending on the view and the particular
        /// item.  For example, a view might return true if the current edit item
        /// implements <seealso cref="IEditableObject"/>, or if the view has special
        /// knowledge about the item that it can use to support rollback of pending
        /// changes.
        /// </summary>
        public bool CanCancelEdit
        {
            get { return (_editItem is IEditableObject); }
        }

        /// <summary>
        /// Returns true if an </seealso cref="EditItem"> transaction is in progress.
        /// </summary>
        public bool IsEditingItem
        {
            get { return (_editItem != null); }
        }

        /// <summary>
        /// When an </seealso cref="EditItem"> transaction is in progress, this property
        /// returns the affected item.  Otherwise it returns null.
        /// </summary>
        public object CurrentEditItem
        {
            get { return _editItem; }
        }

        void SetEditItem(object item)
        {
            if (!System.Windows.Controls.ItemsControl.EqualsEx(item, _editItem))
            {
                _editItem = item;

                OnPropertyChanged("CurrentEditItem");
                OnPropertyChanged("IsEditingItem");
                OnPropertyChanged("CanCancelEdit");
                OnPropertyChanged("CanAddNew");
                OnPropertyChanged("CanRemove");
            }
        }

        #endregion Transactional editing of an item

        #endregion IEditableCollectionView

        #region ICollectionViewLiveShaping

        ///<summary>
        /// Gets a value that indicates whether this view supports turning live sorting on or off.
        ///</summary>
        public bool CanChangeLiveSorting
        { get { return false; } }

        ///<summary>
        /// Gets a value that indicates whether this view supports turning live filtering on or off.
        ///</summary>
        public bool CanChangeLiveFiltering
        { get { return false; } }

        ///<summary>
        /// Gets a value that indicates whether this view supports turning live grouping on or off.
        ///</summary>
        public bool CanChangeLiveGrouping
        { get { return true; } }


        ///<summary>
        /// Gets or sets a value that indicates whether live sorting is enabled.
        /// The value may be null if the view does not know whether live sorting is enabled.
        /// Calling the setter when CanChangeLiveSorting is false will throw an
        /// InvalidOperationException.
        ///</summary
        public bool? IsLiveSorting
        {
            get { return IsDataView ? (bool?)true : (bool?)null; }
            set { throw new InvalidOperationException(SR.Get(SRID.CannotChangeLiveShaping, "IsLiveSorting", "CanChangeLiveSorting")); }
        }

        ///<summary>
        /// Gets or sets a value that indicates whether live filtering is enabled.
        /// The value may be null if the view does not know whether live filtering is enabled.
        /// Calling the setter when CanChangeLiveFiltering is false will throw an
        /// InvalidOperationException.
        ///</summary>
        public bool? IsLiveFiltering
        {
            get { return IsDataView ? (bool?)true : (bool?)null; }
            set { throw new InvalidOperationException(SR.Get(SRID.CannotChangeLiveShaping, "IsLiveFiltering", "CanChangeLiveFiltering")); }
        }

        ///<summary>
        /// Gets or sets a value that indicates whether live grouping is enabled.
        /// The value may be null if the view does not know whether live grouping is enabled.
        /// Calling the setter when CanChangeLiveGrouping is false will throw an
        /// InvalidOperationException.
        ///</summary>
        public bool? IsLiveGrouping
        {
            get { return _isLiveGrouping; }
            set
            {
                if (value == null)
                    throw new ArgumentNullException("value");


                if (value != _isLiveGrouping)
                {
                    _isLiveGrouping = value;
                    RefreshOrDefer();

                    OnPropertyChanged("IsLiveGrouping");
                }
            }
        }

        ///<summary>
        /// Gets a collection of strings describing the properties that
        /// trigger a live-sorting recalculation.
        /// The strings use the same format as SortDescription.PropertyName.
        ///</summary>
        ///<notes>
        /// When the underlying view implements ICollectionViewLiveShaping,
        /// this collection is used to set the underlying view's LiveSortingProperties.
        /// When this collection is empty, the view will use the PropertyName strings
        /// from its SortDescriptions.
        ///</notes>
        public ObservableCollection<string> LiveSortingProperties
        {
            get
            {
                if (_liveSortingProperties == null)
                {
                    _liveSortingProperties = new ObservableCollection<string>();
                }
                return _liveSortingProperties;
            }
        }

        ///<summary>
        /// Gets a collection of strings describing the properties that
        /// trigger a live-filtering recalculation.
        /// The strings use the same format as SortDescription.PropertyName.
        ///</summary>
        ///<notes>
        /// When the underlying view implements ICollectionViewLiveShaping,
        /// this collection is used to set the underlying view's LiveFilteringProperties.
        ///</notes>
        public ObservableCollection<string> LiveFilteringProperties
        {
            get
            {
                if (_liveFilteringProperties == null)
                {
                    _liveFilteringProperties = new ObservableCollection<string>();
                }
                return _liveFilteringProperties;
            }
        }

        ///<summary>
        /// Gets a collection of strings describing the properties that
        /// trigger a live-grouping recalculation.
        /// The strings use the same format as PropertyGroupDescription.PropertyName.
        ///</summary>
        ///<notes>
        /// When the underlying view implements ICollectionViewLiveShaping,
        /// this collection is used to set the underlying view's LiveGroupingProperties.
        ///</notes>
        public ObservableCollection<string> LiveGroupingProperties
        {
            get
            {
                if (_liveGroupingProperties == null)
                {
                    _liveGroupingProperties = new ObservableCollection<string>();
                    _liveGroupingProperties.CollectionChanged += new NotifyCollectionChangedEventHandler(OnLivePropertyListChanged);
                }
                return _liveGroupingProperties;
            }
        }

        void OnLivePropertyListChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (IsLiveGrouping == true)
            {
                RefreshOrDefer();
            }
        }

        #endregion ICollectionViewLiveShaping


        #region IItemProperties

        /// <summary>
        /// Returns information about the properties available on items in the
        /// underlying collection.  This information may come from a schema, from
        /// a type descriptor, from a representative item, or from some other source
        /// known to the view.
        /// </summary>
        public ReadOnlyCollection<ItemPropertyInfo> ItemProperties
        {
            get { return GetItemProperties(); }
        }

        #endregion IItemProperties

        //------------------------------------------------------
        //
        //  Protected Methods
        //
        //------------------------------------------------------

        #region Protected Methods

        /// <summary>
        /// Re-create the view over the associated IList
        /// </summary>
        /// <remarks>
        /// Any sorting and filtering will take effect during Refresh.
        /// </remarks>
        protected override void RefreshOverride()
        {
            object oldCurrentItem = CurrentItem;
            int oldCurrentPosition = IsEmpty ? 0 : CurrentPosition;
            bool oldIsCurrentAfterLast = IsCurrentAfterLast;
            bool oldIsCurrentBeforeFirst = IsCurrentBeforeFirst;

            // force currency off the collection (gives user a chance to save dirty information)
            OnCurrentChanging();

            // changing filter and sorting will cause the inner IBindingList(View) to
            // raise refresh action; ignore those until done setting filter/sort
            _ignoreInnerRefresh = true;

            // IBindingListView can support filtering
            if (IsCustomFilterSet || _isFiltered)
            {
                BindingOperations.AccessCollection(InternalList,
                    () =>
                    {
                        if (IsCustomFilterSet)
                        {
                            _isFiltered = true;
                            _blv.Filter = _customFilter;
                        }
                        else if (_isFiltered)
                        {
                            // app has cleared filter
                            _isFiltered = false;
                            _blv.RemoveFilter();
                        }
                    },
                    true);
            }

            if ((_sort != null) && (_sort.Count > 0) && (CollectionProxy != null) && (CollectionProxy.Count > 0))
            {
                // convert Avalon SortDescription collection to .Net
                // (i.e. string property names become PropertyDescriptors)
                ListSortDescriptionCollection sorts = ConvertSortDescriptionCollection(_sort);

                if (sorts.Count > 0)
                {
                    _isSorted = true;
                    BindingOperations.AccessCollection(InternalList,
                        () =>
                        {
                            if (_blv == null)
                                InternalList.ApplySort(sorts[0].PropertyDescriptor, sorts[0].SortDirection);
                            else
                                _blv.ApplySort(sorts);
                        },
                        true);
                }
                ActiveComparer = new SortFieldComparer(_sort, Culture);
            }
            else if (_isSorted)
            {
                // undo any previous sorting
                _isSorted = false;
                BindingOperations.AccessCollection(InternalList,
                    () =>
                    {
                        InternalList.RemoveSort();
                    },
                    true);
                ActiveComparer = null;
            }

            InitializeGrouping();

            // refresh cached list with any changes
            PrepareCachedList();

            PrepareGroups();

            // reset currency
            if (oldIsCurrentBeforeFirst || IsEmpty)
            {
                SetCurrent(null, -1);
            }
            else if (oldIsCurrentAfterLast)
            {
                SetCurrent(null, InternalCount);
            }
            else
            {
                // oldCurrentItem may be null

                // if there are duplicates, use the position of the first matching item
                //ISSUE windows#868101 DataRowView.IndexOf(oldCurrentItem) returns wrong index, wrong current item gets restored
                int newPosition = InternalIndexOf(oldCurrentItem);

                if (newPosition < 0)
                {
                    // oldCurrentItem not found: move to first item
                    object newItem;
                    newPosition = (NewItemPlaceholderPosition == NewItemPlaceholderPosition.AtBeginning) ?
                                1 : 0;
                    if (newPosition < InternalCount && (newItem = InternalItemAt(newPosition)) != NewItemPlaceholder)
                    {
                        SetCurrent(newItem, newPosition);
                    }
                    else
                    {
                        SetCurrent(null, -1);
                    }
                }
                else
                {
                    SetCurrent(oldCurrentItem, newPosition);
                }
            }

            _ignoreInnerRefresh = false;

            // tell listeners everything has changed
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

        protected override void OnAllowsCrossThreadChangesChanged()
        {
            PrepareCachedList();
        }

        void PrepareCachedList()
        {
            if (AllowsCrossThreadChanges)
            {
                BindingOperations.AccessCollection(InternalList,
                    () =>
                    {
                        RebuildLists();
                    },
                    false);
            }
            else
            {
                RebuildListsCore();
            }
        }

        // this must be called under read-access protection to InternalList
        void RebuildLists()
        {
            lock(SyncRoot)
            {
                ClearPendingChanges();
                RebuildListsCore();
            }
        }

        void RebuildListsCore()
        {
            _cachedList = new ArrayList(InternalList);
            LiveShapingList lsList = _shadowList as LiveShapingList;

            if (lsList != null)
                lsList.LiveShapingDirty -= new EventHandler(OnLiveShapingDirty);

            if (_isGrouping && IsLiveGrouping == true)
            {
                _shadowList = lsList = new LiveShapingList(this, GetLiveShapingFlags(), ActiveComparer);

                foreach (object item in InternalList)
                {
                    lsList.Add(item);
                }

                lsList.LiveShapingDirty += new EventHandler(OnLiveShapingDirty);
            }
            else if (AllowsCrossThreadChanges)
            {
                _shadowList = new ArrayList(InternalList);
            }
            else
            {
                _shadowList = null;
            }
        }

        /// <summary>
        ///     Obsolete.   Retained for compatibility.
        ///     Use OnAllowsCrossThreadChangesChanged instead.
        /// </summary>
        /// <param name="args">
        ///     The NotifyCollectionChangedEventArgs that is added to the change log
        /// </param>
        [Obsolete("Replaced by OnAllowsCrossThreadChangesChanged")]
        protected override void OnBeginChangeLogging(NotifyCollectionChangedEventArgs args)
        {
        }


        /// <summary>
        ///     Must be implemented by the derived classes to process a single change on the
        ///     UIContext.  The UIContext will have allready been entered by now.
        /// </summary>
        /// <param name="args">
        ///     The NotifyCollectionChangedEventArgs to be processed.
        /// </param>
        protected override void ProcessCollectionChanged(NotifyCollectionChangedEventArgs args)
        {
            bool shouldRaiseEvent = false;

            ValidateCollectionChangedEventArgs(args);

            int originalCurrentPosition = CurrentPosition;
            int oldCurrentPosition = CurrentPosition;
            object oldCurrentItem = CurrentItem;
            bool oldIsCurrentAfterLast = IsCurrentAfterLast;
            bool oldIsCurrentBeforeFirst = IsCurrentBeforeFirst;
            bool moveCurrency = false;

            switch (args.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    if (_newItemIndex == -2)
                    {
                        // The ItemAdded event came from AddNew.
                        BeginAddNew(args.NewItems[0], args.NewStartingIndex);
                        return;
                    }
                    else if (_isGrouping)
                        AddItemToGroups(args.NewItems[0]);
                    else
                    {
                        AdjustCurrencyForAdd(args.NewStartingIndex);
                        shouldRaiseEvent = true;
                    }
                    break;

                case NotifyCollectionChangedAction.Remove:
                    if (_isGrouping)
                        RemoveItemFromGroups(args.OldItems[0]);
                    else
                    {
                        moveCurrency = AdjustCurrencyForRemove(args.OldStartingIndex);
                        shouldRaiseEvent = true;
                    }
                    break;

                case NotifyCollectionChangedAction.Replace:
                    if (_isGrouping)
                    {
                        RemoveItemFromGroups(args.OldItems[0]);
                        AddItemToGroups(args.NewItems[0]);
                    }
                    else
                    {
                        moveCurrency = AdjustCurrencyForReplace(args.NewStartingIndex);
                        shouldRaiseEvent = true;
                    }
                    break;

                case NotifyCollectionChangedAction.Move:
                    if (!_isGrouping)
                    {
                        AdjustCurrencyForMove(args.OldStartingIndex, args.NewStartingIndex);
                        shouldRaiseEvent = true;
                    }
                    else
                    {
                        _group.MoveWithinSubgroups(args.OldItems[0], null, InternalList, args.OldStartingIndex, args.NewStartingIndex);
                    }
                    break;

                case NotifyCollectionChangedAction.Reset:
                    if (_isGrouping)
                        RefreshOrDefer();
                    else
                        shouldRaiseEvent = true;
                    break;

                default:
                    throw new NotSupportedException(SR.Get(SRID.UnexpectedCollectionChangeAction, args.Action));
            }

            if (AllowsCrossThreadChanges)
            {
                AdjustShadowCopy(args);
            }


            // remember whether scalar properties of the view have changed.
            // They may change again during the collection change event, so we
            // need to do the test before raising that event.
            bool afterLastHasChanged = (IsCurrentAfterLast != oldIsCurrentAfterLast);
            bool beforeFirstHasChanged = (IsCurrentBeforeFirst != oldIsCurrentBeforeFirst);
            bool currentPositionHasChanged = (CurrentPosition != oldCurrentPosition);
            bool currentItemHasChanged = (CurrentItem != oldCurrentItem);

            // take a new snapshot of the scalar properties, so that we can detect
            // changes made during the collection change event
            oldIsCurrentAfterLast = IsCurrentAfterLast;
            oldIsCurrentBeforeFirst = IsCurrentBeforeFirst;
            oldCurrentPosition = CurrentPosition;
            oldCurrentItem = CurrentItem;

            if (shouldRaiseEvent)
            {
                OnCollectionChanged(args);

                // Any scalar properties that changed don't need a further notification,
                // but do need a new snapshot
                if (IsCurrentAfterLast != oldIsCurrentAfterLast)
                {
                    afterLastHasChanged = false;
                    oldIsCurrentAfterLast = IsCurrentAfterLast;
                }
                if (IsCurrentBeforeFirst != oldIsCurrentBeforeFirst)
                {
                    beforeFirstHasChanged = false;
                    oldIsCurrentBeforeFirst = IsCurrentBeforeFirst;
                }
                if (CurrentPosition != oldCurrentPosition)
                {
                    currentPositionHasChanged = false;
                    oldCurrentPosition = CurrentPosition;
                }
                if (CurrentItem != oldCurrentItem)
                {
                    currentItemHasChanged = false;
                    oldCurrentItem = CurrentItem;
                }
            }

            // currency has to change after firing the deletion event,
            // so event handlers have the right picture
            if (moveCurrency)
            {
                MoveCurrencyOffDeletedElement(originalCurrentPosition);

                // changes to the scalar properties need notification
                afterLastHasChanged = afterLastHasChanged || (IsCurrentAfterLast != oldIsCurrentAfterLast);
                beforeFirstHasChanged = beforeFirstHasChanged || (IsCurrentBeforeFirst != oldIsCurrentBeforeFirst);
                currentPositionHasChanged = currentPositionHasChanged || (CurrentPosition != oldCurrentPosition);
                currentItemHasChanged = currentItemHasChanged || (CurrentItem != oldCurrentItem);
            }

            // notify that the properties have changed.  We may end up doing
            // double notification for properties that change during the collection
            // change event, but that's not harmful.  Detecting the double change
            // is more trouble than it's worth.
            if (afterLastHasChanged)
                OnPropertyChanged(IsCurrentAfterLastPropertyName);

            if (beforeFirstHasChanged)
                OnPropertyChanged(IsCurrentBeforeFirstPropertyName);

            if (currentPositionHasChanged)
                OnPropertyChanged(CurrentPositionPropertyName);

            if (currentItemHasChanged)
                OnPropertyChanged(CurrentItemPropertyName);
        }

        #endregion Protected Methods

        //------------------------------------------------------
        //
        //  Private Properties
        //
        //------------------------------------------------------

        #region Private Properties

        /// <summary>
        /// Protected accessor to private count.
        /// </summary>
        private int InternalCount
        {
            get
            {
                if (_isGrouping)
                    return _group.ItemCount;

                return ((NewItemPlaceholderPosition == NewItemPlaceholderPosition.None) ? 0 : 1) +
                        CollectionProxy.Count;
            }
        }

        private bool IsDataView
        {
            get { return _isDataView; }
        }

        #endregion Private Properties

        //------------------------------------------------------
        //
        //  Private Methods
        //
        //------------------------------------------------------

        #region Private Methods

        /// <summary>
        /// Return index of item in the internal list.
        /// </summary>
        private int InternalIndexOf(object item)
        {
            if (_isGrouping)
            {
                return _group.LeafIndexOf(item);
            }

            if (item == NewItemPlaceholder)
            {
                switch (NewItemPlaceholderPosition)
                {
                    case NewItemPlaceholderPosition.None:
                        return -1;

                    case NewItemPlaceholderPosition.AtBeginning:
                        return 0;

                    case NewItemPlaceholderPosition.AtEnd:
                        return InternalCount - 1;
                }
            }
            else if (IsAddingNew && System.Windows.Controls.ItemsControl.EqualsEx(item, _newItem))
            {
                switch (NewItemPlaceholderPosition)
                {
                    case NewItemPlaceholderPosition.None:
                        break;

                    case NewItemPlaceholderPosition.AtBeginning:
                        return 1;

                    case NewItemPlaceholderPosition.AtEnd:
                        return InternalCount - 2;
                }
            }

            int index = CollectionProxy.IndexOf(item);

            // When you delete the last item from the list,
            // ADO returns a bad value.  Item will be "invalid", in the
            // sense that it is not connected to a table.  But IndexOf(item)
            // returns 10, even though there are only 10 entries in the list.
            // Looks like they're just returning item.Index without checking
            // anything.  So we have to do the checking for them.
            if (index >= CollectionProxy.Count)
            {
                index = -1;
            }

            if (NewItemPlaceholderPosition == NewItemPlaceholderPosition.AtBeginning && index >= 0)
            {
                index += IsAddingNew ? 2 : 1;
            }

            return index;
        }

        /// <summary>
        /// Return item at the given index in the internal list.
        /// </summary>
        private object InternalItemAt(int index)
        {
            if (_isGrouping)
            {
                return _group.LeafAt(index);
            }

            switch (NewItemPlaceholderPosition)
            {
                case NewItemPlaceholderPosition.None:
                    break;

                case NewItemPlaceholderPosition.AtBeginning:
                    if (index == 0)
                        return NewItemPlaceholder;
                    --index;

                    if (IsAddingNew)
                    {
                        if (index == 0)
                            return _newItem;
                        if (index <= _newItemIndex+1)
                            -- index;
                    }
                    break;

                case NewItemPlaceholderPosition.AtEnd:
                    if (index == InternalCount - 1)
                        return NewItemPlaceholder;
                    if (IsAddingNew && index == InternalCount-2)
                        return _newItem;
                    break;
            }

            return CollectionProxy[index];
        }

        /// <summary>
        /// Return true if internal list contains the item.
        /// </summary>
        private bool InternalContains(object item)
        {
            if (item == NewItemPlaceholder)
                return (NewItemPlaceholderPosition != NewItemPlaceholderPosition.None);

            return (!_isGrouping) ? CollectionProxy.Contains(item) : (_group.LeafIndexOf(item) >= 0);
        }

        /// <summary>
        /// Return an enumerator for the internal list.
        /// </summary>
        private IEnumerator InternalGetEnumerator()
        {
            if (!_isGrouping)
            {
                return new PlaceholderAwareEnumerator(this, CollectionProxy.GetEnumerator(), NewItemPlaceholderPosition, _newItem);
            }
            else
            {
                return _group.GetLeafEnumerator();
            }
        }

        // Adjust the ShadowCopy so that it accurately reflects the state of the
        // Data Collection immediately after the CollectionChangeEvent
        private void AdjustShadowCopy(NotifyCollectionChangedEventArgs e)
        {
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    _shadowList.Insert(e.NewStartingIndex, e.NewItems[0]);
                    break;
                case NotifyCollectionChangedAction.Remove:
                    _shadowList.RemoveAt(e.OldStartingIndex);
                    break;
                case NotifyCollectionChangedAction.Replace:
                    _shadowList[e.OldStartingIndex] = e.NewItems[0];
                    break;
                case NotifyCollectionChangedAction.Move:
                    _shadowList.Move(e.OldStartingIndex, e.NewStartingIndex);
                    break;
            }
        }

        // true if CurrentPosition points to item within view
        private bool IsCurrentInView
        {
            get { return (0 <= CurrentPosition && CurrentPosition < InternalCount); }
        }

        // move to a given index
        private void _MoveTo (int proposed)
        {
            if (proposed == CurrentPosition || IsEmpty)
                return;

            object proposedCurrentItem = (0 <= proposed && proposed < InternalCount) ? GetItemAt(proposed) : null;

            if (proposedCurrentItem == NewItemPlaceholder)
                return;         // ignore moves to the placeholder

            if (OKToChangeCurrent())
            {
                bool oldIsCurrentAfterLast = IsCurrentAfterLast;
                bool oldIsCurrentBeforeFirst = IsCurrentBeforeFirst;

                SetCurrent(proposedCurrentItem, proposed);

                OnCurrentChanged();

                // notify that the properties have changed.
                if (IsCurrentAfterLast != oldIsCurrentAfterLast)
                    OnPropertyChanged(IsCurrentAfterLastPropertyName);

                if (IsCurrentBeforeFirst != oldIsCurrentBeforeFirst)
                    OnPropertyChanged(IsCurrentBeforeFirstPropertyName);

                OnPropertyChanged(CurrentPositionPropertyName);
                OnPropertyChanged(CurrentItemPropertyName);
            }
        }

        // subscribe to change notifications
        private void SubscribeToChanges ()
        {
            if (InternalList.SupportsChangeNotification)
            {
                BindingOperations.AccessCollection(InternalList,
                    () =>
                    {
                        InternalList.ListChanged += new ListChangedEventHandler(OnListChanged);
                        RebuildLists();
                    },
                    false);
            }
        }

        // IBindingList has changed
        // At this point we may not have entered the UIContext, but
        // the call to base.OnCollectionChanged will marshall the change over
        private void OnListChanged(object sender, ListChangedEventArgs args)
        {
            if (_ignoreInnerRefresh && (args.ListChangedType == ListChangedType.Reset))
                return;

            NotifyCollectionChangedEventArgs forwardedArgs = null;
            object item = null;
            int delta = _isGrouping ? 0 : (NewItemPlaceholderPosition == NewItemPlaceholderPosition.AtBeginning) ? 1 : 0;
            int index = args.NewIndex;

            switch (args.ListChangedType)
            {
            case ListChangedType.ItemAdded:
                // Some implementations of IBindingList raise an extra ItemAdded event
                // when the new item (from a previous call to AddNew) is "committed".
                // [The IBindingList documentation suggests that all implementations
                // should do this, but only DataView seems to obey this rather
                // bizarre requirement.]  We will ignore these extra events, unless
                // they arise from a commit that we initiated.  There's
                // no way to detect them from the event args;  we do it the same
                // way WinForms.DataGridView does - by comparing counts.
                if (InternalList.Count == _cachedList.Count)
                {
                    if (IsAddingNew && index == _newItemIndex)
                    {
                        Debug.Assert(_newItem == InternalList[index], "unexpected item while committing AddNew");
                        forwardedArgs = ProcessCommitNew(index + delta, index + delta);
                    }
                }
                else
                {
                    // normal ItemAdded event
                    item = InternalList[index];
                    forwardedArgs = new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, item, index + delta);
                    _cachedList.Insert(index, item);
                    if (InternalList.Count != _cachedList.Count)
                        throw new InvalidOperationException(SR.Get(SRID.InconsistentBindingList, InternalList, args.ListChangedType));
                    if (index <= _newItemIndex)
                    {
                        ++ _newItemIndex;
                    }
                }
                break;

            case ListChangedType.ItemDeleted:
                item = _cachedList[index];
                _cachedList.RemoveAt(index);
                if (InternalList.Count != _cachedList.Count)
                    throw new InvalidOperationException(SR.Get(SRID.InconsistentBindingList, InternalList, args.ListChangedType));
                if (index < _newItemIndex)
                {
                    -- _newItemIndex;
                }

                // implicitly cancel AddNew and/or EditItem transactions if the relevant item is removed
                if (item == CurrentEditItem)
                {
                    ImplicitlyCancelEdit();
                }
                if (item == CurrentAddItem)
                {
                    EndAddNew(true);

                    switch (NewItemPlaceholderPosition)
                    {
                        case NewItemPlaceholderPosition.AtBeginning:
                            index = 0;
                            break;
                        case NewItemPlaceholderPosition.AtEnd:
                            index = InternalCount - 1;
                            break;
                    }
                }

                forwardedArgs = new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, item, index + delta);
                break;

            case ListChangedType.ItemMoved:
                if (IsAddingNew && args.OldIndex == _newItemIndex)
                {
                    // ItemMoved applied to the new item.  We assume this is the result
                    // of committing a new item when a sort is in effect - the item
                    // moves to its sorted position.  There's no way to verify this assumption.
                    item = _newItem;
                    Debug.Assert(item == InternalList[index], "unexpected item while committing AddNew");
                    forwardedArgs = ProcessCommitNew(args.OldIndex, index + delta);
                }
                else
                {
                    // normal ItemMoved event
                    item = InternalList[index];
                    forwardedArgs = new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Move, item, index+delta, args.OldIndex+delta);
                    if (args.OldIndex < _newItemIndex && _newItemIndex < args.NewIndex)
                    {
                        -- _newItemIndex;
                    }
                    else if (args.NewIndex <= _newItemIndex && _newItemIndex < args.OldIndex)
                    {
                        ++ _newItemIndex;
                    }
                }

                _cachedList.RemoveAt(args.OldIndex);
                _cachedList.Insert(args.NewIndex, item);
                if (InternalList.Count != _cachedList.Count)
                    throw new InvalidOperationException(SR.Get(SRID.InconsistentBindingList, InternalList, args.ListChangedType));
                break;

            case ListChangedType.ItemChanged:
                // if there is no PropertyDescriptor, then ItemChanged refers to a Replace event
                // (IBindingList indexer set) and not a property change of an item
                if (args.PropertyDescriptor == null)
                {
                    item = InternalList[index];
                    var oldItem = _cachedList[index];
                    _cachedList[index] = item;
                    forwardedArgs = new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Replace, item, oldItem, index);
                    break;
                }
                
                // here ItemChange refers to a property change
                if (!_itemsRaisePropertyChanged.HasValue)
                {
                    // check whether individual items raise PropertyChanged events
                    // (DataRowView does)
                    item = InternalList[args.NewIndex];
                    _itemsRaisePropertyChanged = (item is INotifyPropertyChanged);
                }

                // if items raise PropertyChanged, we can ignore ItemChanged;
                // otherwise, treat it like a Reset
                if (!_itemsRaisePropertyChanged.Value)
                {
                    goto case ListChangedType.Reset;
                }
                break;

            case ListChangedType.Reset:
            // treat all other changes like Reset
            case ListChangedType.PropertyDescriptorAdded:
            case ListChangedType.PropertyDescriptorChanged:
            case ListChangedType.PropertyDescriptorDeleted:
                // implicitly cancel EditItem transactions
                if (IsEditingItem)
                {
                    ImplicitlyCancelEdit();
                }

                // adjust AddNew transactions, depending on whether the new item
                // survived the Reset
                if (IsAddingNew)
                {
                    _newItemIndex = InternalList.IndexOf(_newItem);
                    if (_newItemIndex < 0)
                    {
                        EndAddNew(true);
                    }
                }

                RefreshOrDefer();
                break;
            }

            if (forwardedArgs != null)
            {
                base.OnCollectionChanged(sender, forwardedArgs);
            }
        }

        // fix up CurrentPosition and CurrentItem after a collection change
        private void AdjustCurrencyForAdd(int index)
        {
            if (InternalCount == 1)
            {
                // added first item; set current at BeforeFirst
                SetCurrent(null, -1);
            }
            else if (index <= CurrentPosition)  // adjust current index if insertion is earlier
            {
                int newPosition = CurrentPosition + 1;
                if (newPosition < InternalCount)
                {
                    // CurrentItem might be out of sync if underlying list is not INCC
                    // or if this Add is the result of a Replace (Rem + Add)
                    SetCurrent(GetItemAt(newPosition), newPosition);
                }
                else
                {
                    SetCurrent(null, InternalCount);
                }
            }
        }

        // fix up CurrentPosition and CurrentItem after a collection change
        // return true if the current item was removed
        private bool AdjustCurrencyForRemove(int index)
        {
            bool result = (index == CurrentPosition);

            // adjust current index if deletion is earlier
            if (index < CurrentPosition)
            {
                SetCurrent(CurrentItem, CurrentPosition - 1);
            }

            return result;
        }

        // fix up CurrentPosition and CurrentItem after a collection change
        private void AdjustCurrencyForMove(int oldIndex, int newIndex)
        {
            if (oldIndex == CurrentPosition)
            {
                // moving the current item - currency moves with the item (bug 1942184)
                SetCurrent(GetItemAt(newIndex), newIndex);
            }
            else if (oldIndex < CurrentPosition && CurrentPosition <= newIndex)
            {
                // moving an item from before current position to after -
                // current item shifts back one position
                SetCurrent(CurrentItem, CurrentPosition - 1);
            }
            else if (newIndex <= CurrentPosition && CurrentPosition < oldIndex)
            {
                // moving an item from after current position to before -
                // current item shifts ahead one position
                SetCurrent(CurrentItem, CurrentPosition + 1);
            }
            // else no change necessary
        }

        // fix up CurrentPosition and CurrentItem after a collection change
        // return true if the current item was replaced
        private bool AdjustCurrencyForReplace(int index)
        {
            bool result = (index == CurrentPosition);

            if (result)
            {
                SetCurrent(GetItemAt(index), index);
            }

            return result;
        }

        private void MoveCurrencyOffDeletedElement(int oldCurrentPosition)
        {
            int lastPosition = InternalCount - 1;   // OK if last is -1
            // if position falls beyond last position, move back to last position
            int newPosition = (oldCurrentPosition < lastPosition) ? oldCurrentPosition : lastPosition;

            OnCurrentChanging();

            if (newPosition < 0)
                SetCurrent(null, newPosition);
            else
                SetCurrent(InternalItemAt(newPosition), newPosition);

            OnCurrentChanged();
        }

        private IList CollectionProxy
        {
            get
            {
                if (_shadowList != null)
                    return _shadowList;
                else
                    return InternalList;
            }
        }
        /// <summary>
        /// Accessor to private _internalList field.
        /// </summary>
        private IBindingList InternalList
        {
            get { return _internalList; }
            set { _internalList = value; }
        }

        private bool IsCustomFilterSet
        {
            get { return ((_blv != null) && !String.IsNullOrEmpty(_customFilter)); }
        }

        // can the group name(s) for an item change after we've grouped the item?
        private bool CanGroupNamesChange
        {
            // There's no way we can deduce this - the app has to tell us.
            // If this is true, removing a grouped item is quite difficult.
            // We cannot rely on its group names to tell us which group we inserted
            // it into (they may have been different at insertion time), so we
            // have to do a linear search.
            get { return true; }
        }

        // SortDescription was added/removed, refresh CollView
        private void SortDescriptionsChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (IsAddingNew || IsEditingItem)
                throw new InvalidOperationException(SR.Get(SRID.MemberNotAllowedDuringAddOrEdit, "Sorting"));

            RefreshOrDefer();
        }

        // convert from Avalon SortDescriptions to the corresponding .NET collection
        private ListSortDescriptionCollection ConvertSortDescriptionCollection(SortDescriptionCollection sorts)
        {
            PropertyDescriptorCollection pdc;
            ITypedList itl;
            Type itemType;

            if ((itl = InternalList as ITypedList) != null)
            {
                pdc = itl.GetItemProperties(null);
            }
            else if ((itemType = GetItemType(true)) != null)
            {
                pdc = TypeDescriptor.GetProperties(itemType);
            }
            else
            {
                pdc = null;
            }

            if ((pdc == null) || (pdc.Count == 0))
                throw new ArgumentException(SR.Get(SRID.CannotDetermineSortByPropertiesForCollection));

            ListSortDescription[] sortDescriptions = new ListSortDescription[sorts.Count];
            for (int i = 0; i < sorts.Count; i++)
            {
                PropertyDescriptor dd = pdc.Find(sorts[i].PropertyName, true);
                if (dd == null)
                {
                    string typeName = itl.GetListName(null);
                    throw new ArgumentException(SR.Get(SRID.PropertyToSortByNotFoundOnType, typeName, sorts[i].PropertyName));
                }
                ListSortDescription sd = new ListSortDescription(dd, sorts[i].Direction);
                sortDescriptions[i] = sd;
            }

            return new ListSortDescriptionCollection(sortDescriptions);
        }

        #region Grouping

        // initialization for grouping that should happen before preparing the local array
        void InitializeGrouping()
        {
            // discard old groups
            _group.Clear();

            // initialize the synthetic top level group
            _group.Initialize();

            _isGrouping = (_group.GroupBy != null);
        }


        // divide the data items into groups
        void PrepareGroups()
        {
            if (!_isGrouping)
                return;

            IList list = CollectionProxy;

            // reset the grouping comparer
            IComparer comparer = ActiveComparer;
            if (comparer != null)
            {
                _group.ActiveComparer = comparer;
            }
            else
            {
                CollectionViewGroupInternal.IListComparer ilc = _group.ActiveComparer as CollectionViewGroupInternal.IListComparer;
                if (ilc != null)
                {
                    ilc.ResetList(list);
                }
                else
                {
                    _group.ActiveComparer = new CollectionViewGroupInternal.IListComparer(list);
                }
            }

            // loop through the sorted/filtered list of items, dividing them
            // into groups (with special cases for placeholder and new item)
            if (NewItemPlaceholderPosition == NewItemPlaceholderPosition.AtBeginning)
            {
                _group.InsertSpecialItem(0, NewItemPlaceholder, true /*loading*/);
                if (IsAddingNew)
                {
                    _group.InsertSpecialItem(1, _newItem, true /*loading*/);
                }
            }

            bool isLiveGrouping = (IsLiveGrouping == true);
            LiveShapingList lsList = list as LiveShapingList;

            for (int k=0, n=list.Count;  k<n;  ++k)
            {
                object item = list[k];
                LiveShapingItem lsi = isLiveGrouping ? lsList.ItemAt(k) : null;

                if (!IsAddingNew || !System.Windows.Controls.ItemsControl.EqualsEx(_newItem, item))
                {
                    _group.AddToSubgroups(item, lsi, true /*loading*/);
                }
            }

            if (IsAddingNew && NewItemPlaceholderPosition != NewItemPlaceholderPosition.AtBeginning)
            {
                _group.InsertSpecialItem(_group.Items.Count, _newItem, true /*loading*/);
            }
            if (NewItemPlaceholderPosition == NewItemPlaceholderPosition.AtEnd)
            {
                _group.InsertSpecialItem(_group.Items.Count, NewItemPlaceholder, true /*loading*/);
            }
        }

        // For the Group to report collection changed
        void OnGroupChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == NotifyCollectionChangedAction.Add)
            {
                AdjustCurrencyForAdd(e.NewStartingIndex);
            }
            else if (e.Action == NotifyCollectionChangedAction.Remove)
            {
                AdjustCurrencyForRemove(e.OldStartingIndex);
            }
            OnCollectionChanged(e);
        }

        // The GroupDescriptions collection changed
        void OnGroupByChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (IsAddingNew || IsEditingItem)
                throw new InvalidOperationException(SR.Get(SRID.MemberNotAllowedDuringAddOrEdit, "Grouping"));

            // This is a huge change.  Just refresh the view.
            RefreshOrDefer();
        }

        // A group description for one of the subgroups changed
        void OnGroupDescriptionChanged(object sender, EventArgs e)
        {
            if (IsAddingNew || IsEditingItem)
                throw new InvalidOperationException(SR.Get(SRID.MemberNotAllowedDuringAddOrEdit, "Grouping"));

            // This is a huge change.  Just refresh the view.
            RefreshOrDefer();
        }

        // An item was inserted into the collection.  Update the groups.
        void AddItemToGroups(object item)
        {
            if (IsAddingNew && item == _newItem)
            {
                int index;
                switch (NewItemPlaceholderPosition)
                {
                    case NewItemPlaceholderPosition.None:
                    default:
                        index = _group.Items.Count;
                        break;
                    case NewItemPlaceholderPosition.AtBeginning:
                        index = 1;
                        break;
                    case NewItemPlaceholderPosition.AtEnd:
                        index = _group.Items.Count - 1;
                        break;
                }

                _group.InsertSpecialItem(index, item, false /*loading*/);
            }
            else
            {
                _group.AddToSubgroups(item, null, false /*loading*/);
            }
        }

        // An item was removed from the collection.  Update the groups.
        void RemoveItemFromGroups(object item)
        {
            if (CanGroupNamesChange || _group.RemoveFromSubgroups(item))
            {
                // the item didn't appear where we expected it to.
                _group.RemoveItemFromSubgroupsByExhaustiveSearch(item);
            }
        }

        #endregion Grouping

        #region Live Shaping

        LiveShapingFlags GetLiveShapingFlags()
        {
            LiveShapingFlags result = 0;

            if (IsLiveGrouping == true)
                result = result | LiveShapingFlags.Grouping;

            return result;
        }

        internal void RestoreLiveShaping()
        {
            LiveShapingList list = CollectionProxy as LiveShapingList;
            if (list == null)
                return;

            // restore grouping
            if (_isGrouping)
            {
                List<AbandonedGroupItem> deleteList = new List<AbandonedGroupItem>();
                foreach (LiveShapingItem lsi in list.GroupDirtyItems)
                {
                    if (!lsi.IsDeleted)
                    {
                        _group.RestoreGrouping(lsi, deleteList);
                        lsi.IsGroupDirty = false;
                    }
                }

                _group.DeleteAbandonedGroupItems(deleteList);
            }

            list.GroupDirtyItems.Clear();

            IsLiveShapingDirty = false;
        }

        internal bool IsLiveShapingDirty
        {
            get { return _isLiveShapingDirty; }
            set
            {
                if (value == _isLiveShapingDirty)
                    return;

                _isLiveShapingDirty = value;
                if (value)
                {
                    Dispatcher.BeginInvoke(DispatcherPriority.DataBind, (Action)RestoreLiveShaping);
                }
            }
        }

        void OnLiveShapingDirty(object sender, EventArgs e)
        {
            IsLiveShapingDirty = true;
        }

        #endregion Live Shaping


        private void ValidateCollectionChangedEventArgs(NotifyCollectionChangedEventArgs e)
        {
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    if (e.NewItems.Count != 1)
                        throw new NotSupportedException(SR.Get(SRID.RangeActionsNotSupported));
                    break;

                case NotifyCollectionChangedAction.Remove:
                    if (e.OldItems.Count != 1)
                        throw new NotSupportedException(SR.Get(SRID.RangeActionsNotSupported));
                    break;

                case NotifyCollectionChangedAction.Replace:
                    if (e.NewItems.Count != 1 || e.OldItems.Count != 1)
                        throw new NotSupportedException(SR.Get(SRID.RangeActionsNotSupported));
                    break;

                case NotifyCollectionChangedAction.Move:
                    if (e.NewItems.Count != 1)
                        throw new NotSupportedException(SR.Get(SRID.RangeActionsNotSupported));
                    if (e.NewStartingIndex < 0)
                        throw new InvalidOperationException(SR.Get(SRID.CannotMoveToUnknownPosition));
                    break;

                case NotifyCollectionChangedAction.Reset:
                    break;

                default:
                    throw new NotSupportedException(SR.Get(SRID.UnexpectedCollectionChangeAction, e.Action));
            }
        }

        /// <summary>
        /// Helper to raise a PropertyChanged event  />).
        /// </summary>
        private void OnPropertyChanged(string propertyName)
        {
            OnPropertyChanged(new PropertyChangedEventArgs(propertyName));
        }

        #region Deferred work

        // defer work until the current activity completes
        private void DeferAction(Action action)
        {
            if (_deferredActions == null)
            {
                _deferredActions = new List<Action>();
            }
            _deferredActions.Add(action);
        }

        // perform the deferred work, if any
        private void DoDeferredActions()
        {
            if (_deferredActions != null)
            {
                List<Action> deferredActions = _deferredActions;
                _deferredActions = null;

                foreach(Action action in deferredActions)
                {
                    action();
                }
            }
        }

        #endregion Deferred work


        #endregion Private Methods

        private class BindingListSortDescriptionCollection : SortDescriptionCollection
        {
            internal BindingListSortDescriptionCollection(bool allowMultipleDescriptions)
            {
                _allowMultipleDescriptions = allowMultipleDescriptions;
            }

            /// <summary>
            /// called by base class ObservableCollection&lt;T&gt; when an item is added to list;
            /// </summary>
            protected override void InsertItem(int index, SortDescription item)
            {
                if (!_allowMultipleDescriptions && (this.Count > 0))
                {
                    throw new InvalidOperationException(SR.Get(SRID.BindingListCanOnlySortByOneProperty));
                }
                base.InsertItem(index, item);
            }

            private bool    _allowMultipleDescriptions;
        }

        //------------------------------------------------------
        //
        //  Private Fields
        //
        //------------------------------------------------------

        #region Private Fields

        private IBindingList        _internalList;
        private CollectionViewGroupRoot _group;
        private bool                _isGrouping;
        private IBindingListView    _blv;
        private BindingListSortDescriptionCollection _sort;
        private IList               _shadowList;
        private bool                _isSorted;
        private IComparer           _comparer;
        private string              _customFilter;
        private bool                _isFiltered;
        private bool                _ignoreInnerRefresh;
        private bool?               _itemsRaisePropertyChanged;
        private bool                _isDataView;
        private object              _newItem = NoNewItem;
        private object              _editItem;
        private int                 _newItemIndex;  // position of _newItem in the source collection
        private NewItemPlaceholderPosition _newItemPlaceholderPosition;
        private List<Action>        _deferredActions;
        bool                        _isRemoving;
        private bool?               _isLiveGrouping = false;
        private bool                _isLiveShapingDirty;
        private ObservableCollection<string>    _liveSortingProperties;
        private ObservableCollection<string>    _liveFilteringProperties;
        private ObservableCollection<string>    _liveGroupingProperties;

        // to handle ItemRemoved directly, we need to remember the items -
        // IBL's event args tell us the index, not the item itself
        private IList               _cachedList;
        #endregion Private Fields

    }
}
