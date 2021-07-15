// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// Description: ItemCollection holds the list of items that constitute the content of a ItemsControl.
//
// See specs at ItemsControl.mht
//              model tree -- design.doc
//


using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;

using System.Windows;
using System.Windows.Data;  // for CollectionContainer
using System.Windows.Markup;

using MS.Utility;
using MS.Internal;
using MS.Internal.Controls;
using MS.Internal.Data;     // for IndexedEnumerable
using MS.Internal.KnownBoxes; // for BooleanBoxes
using MS.Internal.Utility;


namespace System.Windows.Controls
{
    /// <summary>
    /// ItemCollection will contain items shaped as strings, objects, xml nodes,
    /// elements, as well as other collections.  (It will not promote elements from
    /// contained collections; to "flatten" contained collections, assign a
    /// <seealso cref="System.Windows.Data.CompositeCollection"/> to
    /// the ItemsSource property on the ItemsControl.)
    /// A <seealso cref="System.Windows.Controls.ItemsControl"/> uses the data
    /// in the ItemCollection to generate its content according to its ItemTemplate.
    /// </summary>
    /// <remarks>
    /// When first created, ItemCollection is in an uninitialized state, neither
    /// ItemsSource-mode nor direct-mode.  It will hold settings like SortDescriptions and Filter
    /// until the mode is determined, then assign the settings to the active view.
    /// When uninitialized, calls to the list-modifying members will put the
    /// ItemCollection in direct mode, and setting the ItemsSource will put the
    /// ItemCollection in ItemsSource mode.
    /// </remarks>

    [Localizability(LocalizationCategory.Ignore)]
    public sealed class ItemCollection : CollectionView, IList, IEditableCollectionViewAddNewItem, ICollectionViewLiveShaping, IItemProperties, IWeakEventListener
    {
        //------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------

        #region Constructors
        // ItemCollection cannot be created standalone, it is created by ItemsControl

        /// <summary>
        /// Initializes a new instance of ItemCollection that is empty and has default initial capacity.
        /// </summary>
        /// <param name="modelParent">model parent of this item collection</param>
        /// <remarks>
        /// </remarks>
        internal ItemCollection(DependencyObject modelParent)
            : base(EmptyEnumerable.Instance, false)
        {
            _modelParent = new WeakReference(modelParent);
        }

        /// <summary>
        /// Initializes a new instance of ItemCollection that is empty and has specified initial capacity.
        /// </summary>
        /// <param name="modelParent">model parent of this item collection</param>
        /// <param name="capacity">The number of items that the new list is initially capable of storing</param>
        /// <remarks>
        /// Some ItemsControl implementations have better idea how many items to anticipate,
        /// capacity parameter lets them tailor the initial size.
        /// </remarks>
        internal ItemCollection(FrameworkElement modelParent, int capacity)
            : base(EmptyEnumerable.Instance, false)
        {
            _defaultCapacity = capacity;
            _modelParent = new WeakReference(modelParent);
        }
        #endregion Constructors


        //------------------------------------------------------
        //
        //  Public Methods
        //
        //------------------------------------------------------

        #region Public Methods

        //------------------------------------------------------
        #region ICurrentItem

        // These currency methods do not call OKToChangeCurrent() because
        // ItemCollection already picks up and forwards the CurrentChanging
        // event from the inner _collectionView.

        /// <summary>
        /// Move <seealso cref="CurrentItem"/> to the first item.
        /// </summary>
        /// <returns>true if <seealso cref="CurrentItem"/> points to an item within the view.</returns>
        public override bool MoveCurrentToFirst()
        {
            if (!EnsureCollectionView())
                return false;

            VerifyRefreshNotDeferred();

            return _collectionView.MoveCurrentToFirst();
        }

        /// <summary>
        /// Move <seealso cref="CurrentItem"/> to the next item.
        /// </summary>
        /// <returns>true if <seealso cref="CurrentItem"/> points to an item within the view.</returns>
        public override bool MoveCurrentToNext()
        {
            if (!EnsureCollectionView())
                return false;

            VerifyRefreshNotDeferred();

            return _collectionView.MoveCurrentToNext();
        }

        /// <summary>
        /// Move <seealso cref="CurrentItem"/> to the previous item.
        /// </summary>
        /// <returns>true if <seealso cref="CurrentItem"/> points to an item within the view.</returns>
        public override bool MoveCurrentToPrevious()
        {
            if (!EnsureCollectionView())
                return false;

            VerifyRefreshNotDeferred();

            return _collectionView.MoveCurrentToPrevious();
        }

        /// <summary>
        /// Move <seealso cref="CurrentItem"/> to the last item.
        /// </summary>
        /// <returns>true if <seealso cref="CurrentItem"/> points to an item within the view.</returns>
        public override bool MoveCurrentToLast()
        {
            if (!EnsureCollectionView())
                return false;

            VerifyRefreshNotDeferred();

            return _collectionView.MoveCurrentToLast();
        }

        /// <summary>
        /// Move <seealso cref="ICollectionView.CurrentItem"/> to the given item.
        /// </summary>
        /// <param name="item">Move CurrentItem to this item.</param>
        /// <returns>true if <seealso cref="CurrentItem"/> points to an item within the view.</returns>
        public override bool MoveCurrentTo(object item)
        {
            if (!EnsureCollectionView())
                return false;

            VerifyRefreshNotDeferred();

            return _collectionView.MoveCurrentTo(item);
        }

        /// <summary>
        /// Move <seealso cref="CurrentItem"/> to the item at the given index.
        /// </summary>
        /// <param name="position">Move CurrentItem to this index</param>
        /// <returns>true if <seealso cref="CurrentItem"/> points to an item within the view.</returns>
        public override bool MoveCurrentToPosition(int position)
        {
            if (!EnsureCollectionView())
                return false;

            VerifyRefreshNotDeferred();

            return _collectionView.MoveCurrentToPosition(position);
        }


        #endregion ICurrentItem

        #region IList

        /// <summary>
        ///     Returns an enumerator object for this ItemCollection
        /// </summary>
        /// <returns>
        ///     Enumerator object for this ItemCollection
        /// </returns>
        protected override IEnumerator GetEnumerator()
        {
            if (!EnsureCollectionView())
                return EmptyEnumerator.Instance;

            return ((IEnumerable)_collectionView).GetEnumerator();
        }

        /// <summary>
        ///     Add an item to this collection.
        /// </summary>
        /// <param name="newItem">
        ///     New item to be added to collection
        /// </param>
        /// <returns>
        ///     Zero-based index where the new item is added.  -1 if the item could not be added.
        /// </returns>
        /// <remarks>
        ///     To facilitate initialization of direct-mode ItemsControls with Sort and/or Filter,
        /// Add() is permitted when ItemsControl is initializing, even if a Sort or Filter has been set.
        /// </remarks>
        /// <exception cref="InvalidOperationException">
        /// trying to add an item which already has a different model/logical parent
        /// - or -
        /// trying to add an item when the ItemCollection is in ItemsSource mode.
        /// </exception>
        public int Add(object newItem)
        {
            CheckIsUsingInnerView();
            int index = _internalView.Add(newItem);
            ModelParent.SetValue(ItemsControl.HasItemsPropertyKey, BooleanBoxes.TrueBox);
            return index;
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
            // Not using CheckIsUsingInnerView() because we don't want to create internal list

            VerifyRefreshNotDeferred();

            if (IsUsingItemsSource)
            {
                throw new InvalidOperationException(SR.Get(SRID.ItemsSourceInUse));
            }

            if (_internalView != null)
            {
                _internalView.Clear();
            }
            ModelParent.ClearValue(ItemsControl.HasItemsPropertyKey);
        }

        /// <summary>
        ///     Checks to see if a given item is in this collection and in the view
        /// </summary>
        /// <param name="containItem">
        ///     The item whose membership in this collection is to be checked.
        /// </param>
        /// <returns>
        ///     True if the collection contains the given item and the item passes the active filter
        /// </returns>
        public override bool Contains(object containItem)
        {
            if (!EnsureCollectionView())
                return false;

            VerifyRefreshNotDeferred();

            return _collectionView.Contains(containItem);
        }

        /// <summary>
        ///     Makes a shallow copy of object references from this
        ///     ItemCollection to the given target array
        /// </summary>
        /// <param name="array">
        ///     Target of the copy operation
        /// </param>
        /// <param name="index">
        ///     Zero-based index at which the copy begins
        /// </param>
        public void CopyTo(Array array, int index)
        {
            if (array == null)
                throw new ArgumentNullException("array");
            if (array.Rank > 1)
                throw new ArgumentException(SR.Get(SRID.BadTargetArray), "array"); // array is multidimensional.
            if (index < 0)
                throw new ArgumentOutOfRangeException("index");

            // use the view instead of the collection, because it may have special sort/filter
            if (!EnsureCollectionView())
                return;  // there is no collection (bind returned no collection) and therefore nothing to copy

            VerifyRefreshNotDeferred();

            IndexedEnumerable.CopyTo(_collectionView, array, index);
        }

        /// <summary>
        ///     Finds the index in this collection/view where the given item is found.
        /// </summary>
        /// <param name="item">
        ///     The item whose index in this collection/view is to be retrieved.
        /// </param>
        /// <returns>
        ///     Zero-based index into the collection/view where the given item can be
        /// found.  Otherwise, -1
        /// </returns>
        public override int IndexOf(object item)
        {
            if (!EnsureCollectionView())
                return -1;

            VerifyRefreshNotDeferred();

            return _collectionView.IndexOf(item);
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
                // only check lower bound because Count could be expensive
                if (index < 0)
                    throw new ArgumentOutOfRangeException("index");

                VerifyRefreshNotDeferred();

                if (!EnsureCollectionView())
                    throw new InvalidOperationException(SR.Get(SRID.ItemCollectionHasNoCollection));

                if (_collectionView == _internalView)
                {
                    // check upper bound here because we know it's not expensive
                    if (index >= _internalView.Count)
                        throw new ArgumentOutOfRangeException("index");
                }

                return _collectionView.GetItemAt(index);
        }



        /// <summary>
        ///     Insert an item in the collection at a given index.  All items
        /// after the given position are moved down by one.
        /// </summary>
        /// <param name="insertIndex">
        ///     The index at which to inser the item
        /// </param>
        /// <param name="insertItem">
        ///     The item reference to be added to the collection
        /// </param>
        /// <exception cref="InvalidOperationException">
        /// Thrown when trying to add an item which already has a different model/logical parent
        /// or when the ItemCollection is read-only because it is in ItemsSource mode
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Thrown if index is out of range
        /// </exception>
        public void Insert(int insertIndex, object insertItem)
        {
            CheckIsUsingInnerView();
            _internalView.Insert(insertIndex, insertItem);
            ModelParent.SetValue(ItemsControl.HasItemsPropertyKey, BooleanBoxes.TrueBox);
        }

        /// <summary>
        ///     Removes the given item reference from the collection or view.
        /// All remaining items move up by one.
        /// </summary>
        /// <exception cref="InvalidOperationException">
        /// the ItemCollection is read-only because it is in ItemsSource mode or there
        /// is a sort or filter in effect
        /// </exception>
        /// <param name="removeItem">
        ///     The item to be removed.
        /// </param>
        public void Remove(object removeItem)
        {
            CheckIsUsingInnerView();
            _internalView.Remove(removeItem);
            if (IsEmpty)
            {
                ModelParent.ClearValue(ItemsControl.HasItemsPropertyKey);
            }
        }

        /// <summary>
        ///     Removes an item from the collection or view at the given index.
        /// All remaining items move up by one.
        /// </summary>
        /// <param name="removeIndex">
        ///     The index at which to remove an item.
        /// </param>
        /// <exception cref="InvalidOperationException">
        /// the ItemCollection is read-only because it is in ItemsSource mode
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Thrown if index is out of range
        /// </exception>
        public void RemoveAt(int removeIndex)
        {
            CheckIsUsingInnerView();
            _internalView.RemoveAt(removeIndex);
            if (IsEmpty)
            {
                ModelParent.ClearValue(ItemsControl.HasItemsPropertyKey);
            }
        }

        #endregion IList

        /// <summary>
        /// Return true if the item is acceptable to the active filter, if any.
        /// It is commonly used during collection-changed notifications to
        /// determine if the added/removed item requires processing.
        /// </summary>
        /// <returns>
        /// true if the item passes the filter or if no filter is set on collection view.
        /// </returns>
        public override bool PassesFilter(object item)
        {
            if (!EnsureCollectionView())
                return true;
            return _collectionView.PassesFilter(item);
        }

        /// <summary>
        /// Re-create the view, using any <seealso cref="SortDescriptions"/> and/or <seealso cref="Filter"/>.
        /// </summary>
        protected override void RefreshOverride()
        {
            if (_collectionView != null)
            {
                if (_collectionView.NeedsRefresh)
                {
                    _collectionView.Refresh();
                }
                else
                {
                    // if the view is up to date, we only need to raise the Reset event
                    OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
                }
            }
        }

        #endregion Public Methods


        //------------------------------------------------------
        //
        //  Public Properties
        //
        //------------------------------------------------------

        #region Public Properties

        /// <summary>
        ///     Read-only property for the number of items stored in this collection of objects
        /// </summary>
        /// <remarks>
        ///     returns 0 if the ItemCollection is uninitialized or
        ///     there is no collection in ItemsSource mode
        /// </remarks>
        public override int Count
        {
            get
            {
                if (!EnsureCollectionView())
                    return 0;

                VerifyRefreshNotDeferred();

                return _collectionView.Count;
            }
        }

        /// <summary>
        /// Returns true if the resulting (filtered) view is emtpy.
        /// </summary>
        public override bool IsEmpty
        {
            get
            {
                if (!EnsureCollectionView())
                    return true;

                VerifyRefreshNotDeferred();

                return _collectionView.IsEmpty;
            }
        }

        /// <summary>
        ///     Indexer property to retrieve or replace the item at the given
        /// zero-based offset into the collection.
        /// </summary>
        /// <exception cref="InvalidOperationException">
        /// trying to set an item which already has a different model/logical parent; or,
        /// trying to set when in ItemsSource mode; or,
        /// the ItemCollection is uninitialized; or,
        /// in ItemsSource mode, the binding on ItemsSource does not provide a collection.
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Thrown if index is out of range
        /// </exception>
        public object this[int index]
        {
            get
            {
                return GetItemAt(index);
            }
            set
            {
                CheckIsUsingInnerView();

                if (index < 0 || index >= _internalView.Count)
                    throw new ArgumentOutOfRangeException("index");

                _internalView[index] = value;
            }
        }

        /// <summary>
        /// The ItemCollection's underlying collection or the user provided ItemsSource collection
        /// </summary>
        public override IEnumerable SourceCollection
        {
            get
            {
                if (IsUsingItemsSource)
                {
                    return ItemsSource;
                }
                else
                {
                    EnsureInternalView();
                    return this;
                }
            }
        }

        /// <summary>
        ///     Returns true if this view needs to be refreshed
        /// (i.e. when the view is not consistent with the current sort or filter).
        /// </summary>
        /// <returns>
        /// true when SortDescriptions or Filter is changed while refresh is deferred,
        /// or in direct-mode, when an item have been added while SortDescriptions or Filter is in place.
        /// </returns>
        public override bool NeedsRefresh
        {
            get
            {
                return (EnsureCollectionView()) ? _collectionView.NeedsRefresh : false;
            }
        }

        /// <summary>
        /// Collection of Sort criteria to sort items in ItemCollection.
        /// </summary>
        /// <remarks>
        /// <p>
        /// Sorting is supported for items in the ItemsControl.Items collection;
        /// if a collection is assigned to ItemsControl.ItemsSource, the capability to sort
        /// depends on the CollectionView for that inner collection.
        /// Simpler implementations of CollectionVIew do not support sorting and will return an empty
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
                // always hand out this ItemCollection's SortDescription collection;
                // in ItemsSource mode the inner collection view will be kept in synch with this collection
                if (MySortDescriptions == null)
                {
                    MySortDescriptions = new SortDescriptionCollection();
                    if (_collectionView != null)
                    {
                        // no need to do this under the monitor - we haven't hooked up events yet
                        CloneList(MySortDescriptions, _collectionView.SortDescriptions);
                    }

                    ((INotifyCollectionChanged)MySortDescriptions).CollectionChanged += new NotifyCollectionChangedEventHandler(SortDescriptionsChanged);
                }
                return MySortDescriptions;
            }
        }

        /// <summary>
        /// Test if this ICollectionView supports sorting before adding
        /// to <seealso cref="SortDescriptions"/>.
        /// </summary>
        public override bool CanSort
        {
            get
            {
                return (EnsureCollectionView()) ? _collectionView.CanSort : true;
            }
        }


        /// <summary>
        /// Set/get a filter callback to filter out items in collection.
        /// This property will always accept a filter, but the collection view for the
        /// underlying ItemsSource may not actually support filtering.
        /// Please check <seealso cref="CanFilter"/>
        /// </summary>
        /// <exception cref="NotSupportedException">
        /// Collections assigned to ItemsSource may not support filtering and could throw a NotSupportedException.
        /// Use <seealso cref="CanFilter"/> property to test if filtering is supported before assigning
        /// a non-null Filter value.
        /// </exception>
        public override Predicate<object> Filter
        {
            get
            {
                return (EnsureCollectionView()) ? _collectionView.Filter : MyFilter;
            }
            set
            {
                MyFilter = value;
                if (_collectionView != null)
                    _collectionView.Filter = value;
            }
        }

        /// <summary>
        /// Test if this ICollectionView supports filtering before assigning
        /// a filter callback to <seealso cref="Filter"/>.
        /// </summary>
        public override bool CanFilter
        {
            get
            {
                return (EnsureCollectionView()) ? _collectionView.CanFilter : true;
            }
        }

        /// <summary>
        /// Returns true if this view really supports grouping.
        /// When this returns false, the rest of the interface is ignored.
        /// </summary>
        public override bool CanGroup
        {
            get
            {
                return (EnsureCollectionView()) ? _collectionView.CanGroup : false;
            }
        }

        /// <summary>
        /// The description of grouping, indexed by level.
        /// </summary>
        public override ObservableCollection<GroupDescription> GroupDescriptions
        {
            get
            {
                // always hand out this ItemCollection's GroupDescription collection;
                // in ItemsSource mode the inner collection view will be kept in synch with this collection
                if (MyGroupDescriptions == null)
                {
                    MyGroupDescriptions = new ObservableCollection<GroupDescription>();
                    if (_collectionView != null)
                    {
                        // no need to do this under the monitor - we haven't hooked up events yet
                        CloneList(MyGroupDescriptions, _collectionView.GroupDescriptions);
                    }

                    ((INotifyCollectionChanged)MyGroupDescriptions).CollectionChanged += new NotifyCollectionChangedEventHandler(GroupDescriptionsChanged);
                }
                return MyGroupDescriptions;
            }
        }

        /// <summary>
        /// The top-level groups, constructed according to the descriptions
        /// given in GroupDescriptions and/or GroupBySelector.
        /// </summary>
        public override ReadOnlyObservableCollection<object> Groups
        {
            get
            {
                return (EnsureCollectionView()) ? _collectionView.Groups : null;
            }
        }

        /// <summary>
        /// Enter a Defer Cycle.
        /// Defer cycles are used to coalesce changes to the ICollectionView.
        /// </summary>
        public override IDisposable DeferRefresh()
        {
            // if already deferred (level > 0) and there is a _collectionView, there should be a _deferInnerRefresh
            Debug.Assert(_deferLevel == 0 || _collectionView == null || _deferInnerRefresh != null);

            // if not already deferred, there should NOT be a _deferInnerRefresh
            Debug.Assert(_deferLevel != 0 || _deferInnerRefresh == null);

            if (_deferLevel == 0 && _collectionView != null)
            {
                _deferInnerRefresh = _collectionView.DeferRefresh();
            }

            ++_deferLevel;  // do this after inner DeferRefresh, in case it throws

            return new DeferHelper(this);
        }

        /// <summary>
        ///     Gets a value indicating whether access to the ItemCollection is synchronized (thread-safe).
        /// </summary>
        bool ICollection.IsSynchronized
        {
            get
            {
                return false;
            }
        }

#pragma warning disable 1634, 1691  // about to use PreSharp message numbers - unknown to C#
        /// <summary>
        ///     Returns an object to be used in thread synchronization.
        /// </summary>
        /// <exception cref="NotSupportedException">
        /// ItemCollection cannot provide a sync root for synchronization while
        /// in ItemsSource mode.  Please use the ItemsSource directly to
        /// get its sync root.
        /// </exception>
        object ICollection.SyncRoot
        {
            get
            {
                if (IsUsingItemsSource)
                {
                    // see discussion in XML comment above.
                    #pragma warning suppress 6503 // "Property get methods should not throw exceptions."
                    throw new NotSupportedException(SR.Get(SRID.ItemCollectionShouldUseInnerSyncRoot));
                }

                return _internalView.SyncRoot;
            }
        }
#pragma warning restore 1634, 1691

        /// <summary>
        ///     Gets a value indicating whether the IList has a fixed size.
        ///     An ItemCollection can usually grow dynamically,
        ///     this call will commonly return FixedSize = False.
        ///     In ItemsSource mode, this call will return IsFixedSize = True.
        /// </summary>
        bool IList.IsFixedSize
        {
            get
            {
                return IsUsingItemsSource;
            }
        }

        /// <summary>
        ///     Gets a value indicating whether the IList is read-only.
        ///     An ItemCollection is usually writable,
        ///     this call will commonly return IsReadOnly = False.
        ///     In ItemsSource mode, this call will return IsReadOnly = True.
        /// </summary>
        bool IList.IsReadOnly
        {
            get
            {
                return IsUsingItemsSource;
            }
        }

        //------------------------------------------------------
        #region ICurrentItem

        /// <summary>
        /// The ordinal position of the <seealso cref="CurrentItem"/> within the (optionally
        /// sorted and filtered) view.
        /// </summary>
        public override int CurrentPosition
        {
            get
            {
                if (!EnsureCollectionView())
                    return -1;

                VerifyRefreshNotDeferred();

                return _collectionView.CurrentPosition;
            }
        }

        /// <summary>
        /// Return current item.
        /// </summary>
        public override object CurrentItem
        {
            get
            {
                if (!EnsureCollectionView())
                    return null;

                VerifyRefreshNotDeferred();

                return _collectionView.CurrentItem;
            }
        }

        /// <summary>
        /// Return true if <seealso cref="ICollectionView.CurrentItem"/> is beyond the end (End-Of-File).
        /// </summary>
        public override bool IsCurrentAfterLast
        {
            get
            {
                if (!EnsureCollectionView())
                    return false;

                VerifyRefreshNotDeferred();

                return _collectionView.IsCurrentAfterLast;
            }
        }

        /// <summary>
        /// Return true if <seealso cref="ICollectionView.CurrentItem"/> is before the beginning (Beginning-Of-File).
        /// </summary>
        public override bool IsCurrentBeforeFirst
        {
            get
            {
                if (!EnsureCollectionView())
                    return false;

                VerifyRefreshNotDeferred();

                return _collectionView.IsCurrentBeforeFirst;
            }
        }

        #endregion ICurrentItem

        #endregion Public Properties

        #region IEditableCollectionView

        #region Adding new items

        /// <summary>
        /// Indicates whether to include a placeholder for a new item, and if so,
        /// where to put it.
        /// </summary>
        NewItemPlaceholderPosition IEditableCollectionView.NewItemPlaceholderPosition
        {
            get
            {
                IEditableCollectionView ecv = _collectionView as IEditableCollectionView;
                if (ecv != null)
                {
                    return ecv.NewItemPlaceholderPosition;
                }
                else
                {
                    return NewItemPlaceholderPosition.None;
                }
            }
            set
            {
                IEditableCollectionView ecv = _collectionView as IEditableCollectionView;
                if (ecv != null)
                {
                    ecv.NewItemPlaceholderPosition = value;
                }
                else
                {
                    throw new InvalidOperationException(SR.Get(SRID.MemberNotAllowedForView, "NewItemPlaceholderPosition"));
                }
            }
        }

        /// <summary>
        /// Return true if the view supports <seealso cref="IEditableCollectionView.AddNew"/>.
        /// </summary>
        bool    IEditableCollectionView.CanAddNew
        {
            get
            {
                IEditableCollectionView ecv = _collectionView as IEditableCollectionView;
                if (ecv != null)
                {
                    return ecv.CanAddNew;
                }
                else
                {
                    return false;
                }
            }
        }

        /// <summary>
        /// Add a new item to the underlying collection.  Returns the new item.
        /// After calling AddNew and changing the new item as desired, either
        /// <seealso cref="IEditableCollectionView.CommitNew"/> or <seealso cref="IEditableCollectionView.CancelNew"/> should be
        /// called to complete the transaction.
        /// </summary>
        object  IEditableCollectionView.AddNew()
        {
            IEditableCollectionView ecv = _collectionView as IEditableCollectionView;
            if (ecv != null)
            {
                return ecv.AddNew();
            }
            else
            {
                throw new InvalidOperationException(SR.Get(SRID.MemberNotAllowedForView, "AddNew"));
            }
        }


        /// <summary>
        /// Complete the transaction started by <seealso cref="IEditableCollectionView.AddNew"/>.  The new
        /// item remains in the collection, and the view's sort, filter, and grouping
        /// specifications (if any) are applied to the new item.
        /// </summary>
        void    IEditableCollectionView.CommitNew()
        {
            IEditableCollectionView ecv = _collectionView as IEditableCollectionView;
            if (ecv != null)
            {
                ecv.CommitNew();
            }
            else
            {
                throw new InvalidOperationException(SR.Get(SRID.MemberNotAllowedForView, "CommitNew"));
            }
        }

        /// <summary>
        /// Complete the transaction started by <seealso cref="IEditableCollectionView.AddNew"/>.  The new
        /// item is removed from the collection.
        /// </summary>
        void    IEditableCollectionView.CancelNew()
        {
            IEditableCollectionView ecv = _collectionView as IEditableCollectionView;
            if (ecv != null)
            {
                ecv.CancelNew();
            }
            else
            {
                throw new InvalidOperationException(SR.Get(SRID.MemberNotAllowedForView, "CancelNew"));
            }
        }

        /// <summary>
        /// Returns true if an </seealso cref="IEditableCollectionView.AddNew"> transaction is in progress.
        /// </summary>
        bool    IEditableCollectionView.IsAddingNew
        {
            get
            {
                IEditableCollectionView ecv = _collectionView as IEditableCollectionView;
                if (ecv != null)
                {
                    return ecv.IsAddingNew;
                }
                else
                {
                    return false;
                }
            }
        }

        /// <summary>
        /// When an </seealso cref="IEditableCollectionView.AddNew"> transaction is in progress, this property
        /// returns the new item.  Otherwise it returns null.
        /// </summary>
        object  IEditableCollectionView.CurrentAddItem
        {
            get
            {
                IEditableCollectionView ecv = _collectionView as IEditableCollectionView;
                if (ecv != null)
                {
                    return ecv.CurrentAddItem;
                }
                else
                {
                    return null;
                }
            }
        }

        #endregion Adding new items

        #region Removing items

        /// <summary>
        /// Return true if the view supports <seealso cref="IEditableCollectionView.Remove"/> and
        /// <seealso cref="RemoveAt"/>.
        /// </summary>
        bool    IEditableCollectionView.CanRemove
        {
            get
            {
                IEditableCollectionView ecv = _collectionView as IEditableCollectionView;
                if (ecv != null)
                {
                    return ecv.CanRemove;
                }
                else
                {
                    return false;
                }
            }
        }

        /// <summary>
        /// Remove the item at the given index from the underlying collection.
        /// The index is interpreted with respect to the view (not with respect to
        /// the underlying collection).
        /// </summary>
        void    IEditableCollectionView.RemoveAt(int index)
        {
            IEditableCollectionView ecv = _collectionView as IEditableCollectionView;
            if (ecv != null)
            {
                ecv.RemoveAt(index);
            }
            else
            {
                throw new InvalidOperationException(SR.Get(SRID.MemberNotAllowedForView, "RemoveAt"));
            }
        }

        /// <summary>
        /// Remove the given item from the underlying collection.
        /// </summary>
        void    IEditableCollectionView.Remove(object item)
        {
            IEditableCollectionView ecv = _collectionView as IEditableCollectionView;
            if (ecv != null)
            {
                ecv.Remove(item);
            }
            else
            {
                throw new InvalidOperationException(SR.Get(SRID.MemberNotAllowedForView, "Remove"));
            }
        }

        #endregion Removing items

        #region Transactional editing of an item

        /// <summary>
        /// Begins an editing transaction on the given item.  The transaction is
        /// completed by calling either <seealso cref="IEditableCollectionView.CommitEdit"/> or
        /// <seealso cref="IEditableCollectionView.CancelEdit"/>.  Any changes made to the item during
        /// the transaction are considered "pending", provided that the view supports
        /// the notion of "pending changes" for the given item.
        /// </summary>
        void    IEditableCollectionView.EditItem(object item)
        {
            IEditableCollectionView ecv = _collectionView as IEditableCollectionView;
            if (ecv != null)
            {
                ecv.EditItem(item);
            }
            else
            {
                throw new InvalidOperationException(SR.Get(SRID.MemberNotAllowedForView, "EditItem"));
            }
        }

        /// <summary>
        /// Complete the transaction started by <seealso cref="IEditableCollectionView.EditItem"/>.
        /// The pending changes (if any) to the item are committed.
        /// </summary>
        void    IEditableCollectionView.CommitEdit()
        {
            IEditableCollectionView ecv = _collectionView as IEditableCollectionView;
            if (ecv != null)
            {
                ecv.CommitEdit();
            }
            else
            {
                throw new InvalidOperationException(SR.Get(SRID.MemberNotAllowedForView, "CommitEdit"));
            }
        }

        /// <summary>
        /// Complete the transaction started by <seealso cref="IEditableCollectionView.EditItem"/>.
        /// The pending changes (if any) to the item are discarded.
        /// </summary>
        void    IEditableCollectionView.CancelEdit()
        {
            IEditableCollectionView ecv = _collectionView as IEditableCollectionView;
            if (ecv != null)
            {
                ecv.CancelEdit();
            }
            else
            {
                throw new InvalidOperationException(SR.Get(SRID.MemberNotAllowedForView, "CancelEdit"));
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
        bool    IEditableCollectionView.CanCancelEdit
        {
            get
            {
                IEditableCollectionView ecv = _collectionView as IEditableCollectionView;
                if (ecv != null)
                {
                    return ecv.CanCancelEdit;
                }
                else
                {
                    return false;
                }
            }
        }

        /// <summary>
        /// Returns true if an </seealso cref="IEditableCollectionView.EditItem"> transaction is in progress.
        /// </summary>
        bool    IEditableCollectionView.IsEditingItem
        {
            get
            {
                IEditableCollectionView ecv = _collectionView as IEditableCollectionView;
                if (ecv != null)
                {
                    return ecv.IsEditingItem;
                }
                else
                {
                    return false;
                }
            }
        }

        /// <summary>
        /// When an </seealso cref="IEditableCollectionView.EditItem"> transaction is in progress, this property
        /// returns the affected item.  Otherwise it returns null.
        /// </summary>
        object  IEditableCollectionView.CurrentEditItem
        {
            get
            {
                IEditableCollectionView ecv = _collectionView as IEditableCollectionView;
                if (ecv != null)
                {
                    return ecv.CurrentEditItem;
                }
                else
                {
                    return null;
                }
            }
        }

        #endregion Transactional editing of an item

        #endregion IEditableCollectionView

        #region IEditableCollectionViewAddNewItem

        /// <summary>
        /// Return true if the view supports <seealso cref="IEditableCollectionViewAddNewItem.AddNewItem"/>.
        /// </summary>
        bool    IEditableCollectionViewAddNewItem.CanAddNewItem
        {
            get
            {
                IEditableCollectionViewAddNewItem ani = _collectionView as IEditableCollectionViewAddNewItem;
                if (ani != null)
                {
                    return ani.CanAddNewItem;
                }
                else
                {
                    return false;
                }
            }
        }

        /// <summary>
        /// Add a new item to the underlying collection.  Returns the new item.
        /// After calling AddNewItem and changing the new item as desired, either
        /// <seealso cref="IEditableCollectionView.CommitNew"/> or <seealso cref="IEditableCollectionView.CancelNew"/> should be
        /// called to complete the transaction.
        /// </summary>
        object  IEditableCollectionViewAddNewItem.AddNewItem(object newItem)
        {
            IEditableCollectionViewAddNewItem ani = _collectionView as IEditableCollectionViewAddNewItem;
            if (ani != null)
            {
                return ani.AddNewItem(newItem);
            }
            else
            {
                throw new InvalidOperationException(SR.Get(SRID.MemberNotAllowedForView, "AddNewItem"));
            }
        }

        #endregion IEditableCollectionViewAddNewItem

        #region ICollectionViewLiveShaping

        ///<summary>
        /// Gets a value that indicates whether this view supports turning live sorting on or off.
        ///</summary>
        public bool CanChangeLiveSorting
        {
            get
            {
                ICollectionViewLiveShaping cvls = _collectionView as ICollectionViewLiveShaping;
                return (cvls != null) ? cvls.CanChangeLiveSorting : false;
            }
        }

        ///<summary>
        /// Gets a value that indicates whether this view supports turning live filtering on or off.
        ///</summary>
        public bool CanChangeLiveFiltering
        {
            get
            {
                ICollectionViewLiveShaping cvls = _collectionView as ICollectionViewLiveShaping;
                return (cvls != null) ? cvls.CanChangeLiveFiltering : false;
            }
        }

        ///<summary>
        /// Gets a value that indicates whether this view supports turning live grouping on or off.
        ///</summary>
        public bool CanChangeLiveGrouping
        {
            get
            {
                ICollectionViewLiveShaping cvls = _collectionView as ICollectionViewLiveShaping;
                return (cvls != null) ? cvls.CanChangeLiveGrouping : false;
            }
        }


        ///<summary>
        /// Gets or sets a value that indicates whether live sorting is enabled.
        /// The value may be null if the view does not know whether live sorting is enabled.
        /// Calling the setter when CanChangeLiveSorting is false will throw an
        /// InvalidOperationException.
        ///</summary
        public bool? IsLiveSorting
        {
            get
            {
                ICollectionViewLiveShaping cvls = _collectionView as ICollectionViewLiveShaping;
                return (cvls != null) ? cvls.IsLiveSorting : null;
            }
            set
            {
                MyIsLiveSorting = value;
                ICollectionViewLiveShaping cvls = _collectionView as ICollectionViewLiveShaping;
                if (cvls != null && cvls.CanChangeLiveSorting)
                    cvls.IsLiveSorting = value;
            }
        }

        ///<summary>
        /// Gets or sets a value that indicates whether live filtering is enabled.
        /// The value may be null if the view does not know whether live filtering is enabled.
        /// Calling the setter when CanChangeLiveFiltering is false will throw an
        /// InvalidOperationException.
        ///</summary>
        public bool? IsLiveFiltering
        {
            get
            {
                ICollectionViewLiveShaping cvls = _collectionView as ICollectionViewLiveShaping;
                return (cvls != null) ? cvls.IsLiveFiltering : null;
            }
            set
            {
                MyIsLiveFiltering = value;
                ICollectionViewLiveShaping cvls = _collectionView as ICollectionViewLiveShaping;
                if (cvls != null && cvls.CanChangeLiveFiltering)
                    cvls.IsLiveFiltering = value;
            }
        }

        ///<summary>
        /// Gets or sets a value that indicates whether live grouping is enabled.
        /// The value may be null if the view does not know whether live grouping is enabled.
        /// Calling the setter when CanChangeLiveGrouping is false will throw an
        /// InvalidOperationException.
        ///</summary>
        public bool? IsLiveGrouping
        {
            get
            {
                ICollectionViewLiveShaping cvls = _collectionView as ICollectionViewLiveShaping;
                return (cvls != null) ? cvls.IsLiveGrouping : null;
            }
            set
            {
                MyIsLiveGrouping = value;
                ICollectionViewLiveShaping cvls = _collectionView as ICollectionViewLiveShaping;
                if (cvls != null && cvls.CanChangeLiveGrouping)
                    cvls.IsLiveGrouping = value;
            }
        }


        ///<summary>
        /// Gets a collection of strings describing the properties that
        /// trigger a live-sorting recalculation.
        /// The strings use the same format as SortDescription.PropertyName.
        ///</summary>
        ///<notes>
        /// When this collection is empty, the view will use the PropertyName strings
        /// from its SortDescriptions.
        ///
        /// This collection is useful when sorting is described code supplied
        /// by the application  (e.g. ListCollectionView.CustomSort).
        /// In this case the view does not know which properties the code examines;
        /// the application should tell the view by adding the relevant properties
        /// to the LiveSortingProperties collection.
        ///</notes>
        public ObservableCollection<string> LiveSortingProperties
        {
            get
            {
                // always hand out this ItemCollection's LiveSortingProperties collection;
                // in ItemsSource mode the inner collection view will be kept in synch with this collection
                if (MyLiveSortingProperties == null)
                {
                    MyLiveSortingProperties = new ObservableCollection<string>();
                    ICollectionViewLiveShaping icvls = _collectionView as ICollectionViewLiveShaping;
                    if (icvls != null)
                    {
                        // no need to do this under the monitor - we haven't hooked up events yet
                        CloneList(MyLiveSortingProperties, icvls.LiveSortingProperties);
                    }

                    ((INotifyCollectionChanged)MyLiveSortingProperties).CollectionChanged += new NotifyCollectionChangedEventHandler(LiveSortingChanged);
                }
                return MyLiveSortingProperties;
            }
        }

        ///<summary>
        /// Gets a collection of strings describing the properties that
        /// trigger a live-filtering recalculation.
        /// The strings use the same format as SortDescription.PropertyName.
        ///</summary>
        ///<notes>
        /// Filtering is described by a Predicate.  The view does not
        /// know which properties the Predicate examines;  the application should
        /// tell the view by adding the relevant properties to the LiveFilteringProperties
        /// collection.
        ///</notes>
        public ObservableCollection<string> LiveFilteringProperties
        {
            get
            {
                // always hand out this ItemCollection's LiveFilteringProperties collection;
                // in ItemsSource mode the inner collection view will be kept in synch with this collection
                if (MyLiveFilteringProperties == null)
                {
                    MyLiveFilteringProperties = new ObservableCollection<string>();
                    ICollectionViewLiveShaping icvls = _collectionView as ICollectionViewLiveShaping;
                    if (icvls != null)
                    {
                        // no need to do this under the monitor - we haven't hooked up events yet
                        CloneList(MyLiveFilteringProperties, icvls.LiveFilteringProperties);
                    }

                    ((INotifyCollectionChanged)MyLiveFilteringProperties).CollectionChanged += new NotifyCollectionChangedEventHandler(LiveFilteringChanged);
                }
                return MyLiveFilteringProperties;
            }
        }

        ///<summary>
        /// Gets a collection of strings describing the properties that
        /// trigger a live-grouping recalculation.
        /// The strings use the same format as PropertyGroupDescription.PropertyName.
        ///</summary>
        ///<notes>
        /// When this collection is empty, the view will use the PropertyName strings
        /// from its GroupDescriptions.
        ///
        /// This collection is useful when grouping is described code supplied
        /// by the application (e.g. PropertyGroupDescription.Converter).
        /// In this case the view does not know which properties the code examines;
        /// the application should tell the view by adding the relevant properties
        /// to the LiveGroupingProperties collection.
        ///</notes>
        public ObservableCollection<string> LiveGroupingProperties
        {
            get
            {
                // always hand out this ItemCollection's LiveGroupingProperties collection;
                // in ItemsSource mode the inner collection view will be kept in synch with this collection
                if (MyLiveGroupingProperties == null)
                {
                    MyLiveGroupingProperties = new ObservableCollection<string>();
                    ICollectionViewLiveShaping icvls = _collectionView as ICollectionViewLiveShaping;
                    if (icvls != null)
                    {
                        // no need to do this under the monitor - we haven't hooked up events yet
                        CloneList(MyLiveGroupingProperties, icvls.LiveGroupingProperties);
                    }

                    ((INotifyCollectionChanged)MyLiveGroupingProperties).CollectionChanged += new NotifyCollectionChangedEventHandler(LiveGroupingChanged);
                }
                return MyLiveGroupingProperties;
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
        ReadOnlyCollection<ItemPropertyInfo>    IItemProperties.ItemProperties
        {
            get
            {
                IItemProperties iip = _collectionView as IItemProperties;
                if (iip != null)
                {
                    return iip.ItemProperties;
                }
                else
                {
                    return null;
                }
            }
        }

        #endregion IItemProperties

        //------------------------------------------------------
        //
        //  Internal API
        //
        //------------------------------------------------------

        #region Internal API

        internal DependencyObject ModelParent
        {
            get { return (DependencyObject)_modelParent.Target; }
        }

        internal FrameworkElement ModelParentFE
        {
            get { return ModelParent as FrameworkElement; }
        }

        // This puts the ItemCollection into ItemsSource mode.
        internal void SetItemsSource(IEnumerable value, Func<object, object> GetSourceItem = null)
        {
            // Allow this while refresh is deferred.

            // If we're switching from Normal mode, first make sure it's legal.
            if (!IsUsingItemsSource && (_internalView != null) && (_internalView.RawCount > 0))
            {
                throw new InvalidOperationException(SR.Get(SRID.CannotUseItemsSource));
            }

            _itemsSource = value;
            _isUsingItemsSource = true;

            SetCollectionView(CollectionViewSource.GetDefaultCollectionView(_itemsSource, ModelParent, GetSourceItem));
        }

        // This returns ItemCollection to direct mode.
        internal void ClearItemsSource()
        {
            if (IsUsingItemsSource)
            {
                // return to normal mode
                _itemsSource = null;
                _isUsingItemsSource = false;

                SetCollectionView(_internalView);   // it's ok if _internalView is null; just like uninitialized
            }
            else
            {
                // already in normal mode - no-op
            }
        }

        // Read-only property used by ItemsControl
        internal IEnumerable ItemsSource
        {
            get
            {
                return _itemsSource;
            }
        }

        internal bool IsUsingItemsSource
        {
            get
            {
                return _isUsingItemsSource;
            }
        }

        internal CollectionView CollectionView
        {
            get { return _collectionView; }
        }

        internal void BeginInit()
        {
            Debug.Assert(_isInitializing == false);
            _isInitializing = true;
            if (_collectionView != null)            // disconnect from collectionView to cut extraneous events
                UnhookCollectionView(_collectionView);
        }

        internal void EndInit()
        {
            Debug.Assert(_isInitializing == true);
            EnsureCollectionView();
            _isInitializing = false;                // now we allow collectionView to be hooked up again
            if (_collectionView != null)
            {
                HookCollectionView(_collectionView);
                Refresh();                          // apply any sort or filter for the first time
            }
        }

        internal IEnumerator LogicalChildren
        {
            get
            {
                EnsureInternalView();
                return _internalView.LogicalChildren;
            }
        }

        internal override void GetCollectionChangedSources(int level, Action<int, object, bool?, List<string>> format, List<string> sources)
        {
            format(level, this, false, sources);
            if (_collectionView != null)
            {
                _collectionView.GetCollectionChangedSources(level+1, format, sources);
            }
        }


        #endregion Internal API


        //------------------------------------------------------
        //
        //  Private Properties
        //
        //------------------------------------------------------

        #region Private Properties
        private new bool IsRefreshDeferred
        {
            get { return _deferLevel > 0; }
        }

        #endregion


        //------------------------------------------------------
        //
        //  Private Methods
        //
        //------------------------------------------------------

        #region Private Methods

        // ===== Lazy creation of InternalView =====
        // When ItemCollection is instantiated, it is uninitialized (_collectionView == null).
        // It remains so until SetItemsSource() puts it into ItemsSource mode
        // or a modifying method call such as Add() or Insert() puts it into direct mode.

        // Several ItemCollection methods check EnsureCollectionView, which returns false if
        // (_collectionView == null) and (InternalView == null), and it can mean two things:
        //   1) ItemCollection is uninitialized
        //   2) ItemsControl is in ItemsSource mode, but the ItemsSource binding returned null
        // for either of these cases, a reasonable default return value or behavior is provided.

        // EnsureCollectionView() will set _collectionView to the InternalView if the mode is correct.
        bool EnsureCollectionView()
        {
            if (_collectionView == null && !IsUsingItemsSource && _internalView != null)
            {
                // If refresh is not necessary, fake initialization so that SetCollectionView
                // doesn't raise a refresh event.
                if (_internalView.IsEmpty)
                {
                    bool wasInitializing = _isInitializing;
                    _isInitializing = true;
                    SetCollectionView(_internalView);
                    _isInitializing = wasInitializing;
                }
                else
                {
                    SetCollectionView(_internalView);
                }

                // If we're not in Begin/End Init, now's a good time to hook up listeners
                if (!_isInitializing)
                    HookCollectionView(_collectionView);
            }
            return (_collectionView != null);
        }

        void EnsureInternalView()
        {
            if (_internalView == null)
            {
                // lazy creation of the InnerItemCollectionView
                _internalView = new InnerItemCollectionView(_defaultCapacity, this);
            }
        }

        // Change the collection view in use, unhook/hook event handlers
        void SetCollectionView(CollectionView view)
        {
            if (_collectionView == view)
                return;

            if (_collectionView != null)
            {
                // Unhook events first, to avoid unnecessary refresh while it is still the active view.
                if (!_isInitializing)
                    UnhookCollectionView(_collectionView);

                if (IsRefreshDeferred)  // we've been deferring refresh on the _collectionView
                {
                    // end defer refresh on the _collectionView that we're letting go
                    _deferInnerRefresh.Dispose();
                    _deferInnerRefresh = null;
                }
            }

            bool raiseReset = false;
            _collectionView = view;
            InvalidateEnumerableWrapper();

            if (_collectionView != null)
            {
                _deferInnerRefresh = _collectionView.DeferRefresh();

                ApplySortFilterAndGroup();

                // delay event hook-up when initializing.  see BeginInit() and EndInit().
                if (!_isInitializing)
                    HookCollectionView(_collectionView);

                if (!IsRefreshDeferred)
                {
                    // make sure we get at least one refresh
                    raiseReset = !_collectionView.NeedsRefresh;

                    _deferInnerRefresh.Dispose();    // This fires refresh event that should reach ItemsControl listeners
                    _deferInnerRefresh = null;
                }
                // when refresh is deferred, we hold on to the inner DeferRefresh until EndDefer()
            }
            else    // ItemsSource binding returned null
            {
                if (!IsRefreshDeferred)
                {
                    raiseReset = true;
                }
            }

            if (raiseReset)
            {
                // notify listeners that the view is changed
                OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
            }

            // with a new view, we have new live shaping behavior
            OnPropertyChanged(new PropertyChangedEventArgs("IsLiveSorting"));
            OnPropertyChanged(new PropertyChangedEventArgs("IsLiveFiltering"));
            OnPropertyChanged(new PropertyChangedEventArgs("IsLiveGrouping"));
        }

        void ApplySortFilterAndGroup()
        {
            if (!IsShapingActive)
                return;

            // Only apply sort/filter/group if new view supports it and ItemCollection has real values
            if (_collectionView.CanSort)
            {
                // if user has added SortDescriptions to this.SortDescriptions, those settings get pushed to
                // the newly attached collection view
                // if no SortDescriptions are set on ItemCollection,
                // the inner collection view's .SortDescriptions gets copied to this.SortDescriptions
                // when switching back to direct mode and no user-set on this.SortDescriptions
                // then clear any .SortDescriptions set from previous inner collection view
                SortDescriptionCollection source = (IsSortingSet) ? MySortDescriptions : _collectionView.SortDescriptions;
                SortDescriptionCollection target = (IsSortingSet) ? _collectionView.SortDescriptions : MySortDescriptions;

                using (SortDescriptionsMonitor.Enter())
                {
                    CloneList(target, source);
                }
            }

            if (_collectionView.CanFilter && MyFilter != null)
                _collectionView.Filter = MyFilter;

            if (_collectionView.CanGroup)
            {
                // if user has added GroupDescriptions to this.GroupDescriptions, those settings get pushed to
                // the newly attached collection view
                // if no GroupDescriptions are set on ItemCollection,
                // the inner collection view's .GroupDescriptions gets copied to this.GroupDescriptions
                // when switching back to direct mode and no user-set on this.GroupDescriptions
                // then clear any .GroupDescriptions set from previous inner collection view
                ObservableCollection<GroupDescription> source = (IsGroupingSet) ? MyGroupDescriptions : _collectionView.GroupDescriptions;
                ObservableCollection<GroupDescription> target = (IsGroupingSet) ? _collectionView.GroupDescriptions : MyGroupDescriptions;

                using (GroupDescriptionsMonitor.Enter())
                {
                    CloneList(target, source);
                }
            }

            ICollectionViewLiveShaping cvls = _collectionView as ICollectionViewLiveShaping;
            if (cvls != null)
            {
                if (MyIsLiveSorting != null && cvls.CanChangeLiveSorting)
                {
                    cvls.IsLiveSorting = MyIsLiveSorting;
                }
                if (MyIsLiveFiltering != null && cvls.CanChangeLiveFiltering)
                {
                    cvls.IsLiveFiltering = MyIsLiveFiltering;
                }
                if (MyIsLiveGrouping != null && cvls.CanChangeLiveGrouping)
                {
                    cvls.IsLiveGrouping = MyIsLiveGrouping;
                }
            }
        }

        void HookCollectionView(CollectionView view)
        {
            CollectionChangedEventManager.AddHandler(view, OnViewCollectionChanged);
            CurrentChangingEventManager.AddHandler(view, OnCurrentChanging);
            CurrentChangedEventManager.AddHandler(view, OnCurrentChanged);
            PropertyChangedEventManager.AddHandler(view, OnViewPropertyChanged, String.Empty);

            SortDescriptionCollection sort = view.SortDescriptions;
            if (sort != null && sort != SortDescriptionCollection.Empty)
            {
                CollectionChangedEventManager.AddHandler(sort, OnInnerSortDescriptionsChanged);
            }

            ObservableCollection<GroupDescription> group = view.GroupDescriptions;
            if (group != null)
            {
                CollectionChangedEventManager.AddHandler(group, OnInnerGroupDescriptionsChanged);
            }

            ICollectionViewLiveShaping iclvs = view as ICollectionViewLiveShaping;
            if (iclvs != null)
            {
                ObservableCollection<string> liveSortingProperties = iclvs.LiveSortingProperties;
                if (liveSortingProperties != null)
                {
                    CollectionChangedEventManager.AddHandler(liveSortingProperties, OnInnerLiveSortingChanged);
                }

                ObservableCollection<string> liveFilteringProperties = iclvs.LiveFilteringProperties;
                if (liveFilteringProperties != null)
                {
                    CollectionChangedEventManager.AddHandler(liveFilteringProperties, OnInnerLiveFilteringChanged);
                }

                ObservableCollection<string> liveGroupingProperties = iclvs.LiveGroupingProperties;
                if (liveGroupingProperties != null)
                {
                    CollectionChangedEventManager.AddHandler(liveGroupingProperties, OnInnerLiveGroupingChanged);
                }
            }
        }

        void UnhookCollectionView(CollectionView view)
        {
            CollectionChangedEventManager.RemoveHandler(view, OnViewCollectionChanged);
            CurrentChangingEventManager.RemoveHandler(view, OnCurrentChanging);
            CurrentChangedEventManager.RemoveHandler(view, OnCurrentChanged);
            PropertyChangedEventManager.RemoveHandler(view, OnViewPropertyChanged, String.Empty);

            SortDescriptionCollection sort = view.SortDescriptions;
            if (sort != null && sort != SortDescriptionCollection.Empty)
            {
                CollectionChangedEventManager.RemoveHandler(sort, OnInnerSortDescriptionsChanged);
            }

            ObservableCollection<GroupDescription> group = view.GroupDescriptions;
            if (group != null)
            {
                CollectionChangedEventManager.RemoveHandler(group, OnInnerGroupDescriptionsChanged);
            }

            ICollectionViewLiveShaping iclvs = view as ICollectionViewLiveShaping;
            if (iclvs != null)
            {
                ObservableCollection<string> liveSortingProperties = iclvs.LiveSortingProperties;
                if (liveSortingProperties != null)
                {
                    CollectionChangedEventManager.RemoveHandler(liveSortingProperties, OnInnerLiveSortingChanged);
                }

                ObservableCollection<string> liveFilteringProperties = iclvs.LiveFilteringProperties;
                if (liveFilteringProperties != null)
                {
                    CollectionChangedEventManager.RemoveHandler(liveFilteringProperties, OnInnerLiveFilteringChanged);
                }

                ObservableCollection<string> liveGroupingProperties = iclvs.LiveGroupingProperties;
                if (liveGroupingProperties != null)
                {
                    CollectionChangedEventManager.RemoveHandler(liveGroupingProperties, OnInnerLiveGroupingChanged);
                }
            }

            // cancel any pending AddNew or EditItem transactions
            IEditableCollectionView iev = _collectionView as IEditableCollectionView;
            if (iev != null)
            {
                if (iev.IsAddingNew)
                {
                    iev.CancelNew();
                }

                if (iev.IsEditingItem)
                {
                    if (iev.CanCancelEdit)
                    {
                        iev.CancelEdit();
                    }
                    else
                    {
                        iev.CommitEdit();
                    }
                }
            }
        }

        void OnViewCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            // when the collection changes, the enumerator is no longer valid.
            // This should be detected by IndexedEnumerable, but isn't because
            // of bug in CollectionView (CollectionView's enumerators
            // do not invalidate after a collection change).
            // As a partial remedy discard the
            // enumerator here.
            //
            // Remove this line when the CollectionView bug is fixed.
            InvalidateEnumerableWrapper();

            // notify listeners on ItemsControl (like ItemContainerGenerator)
            OnCollectionChanged(e);
        }

        void OnCurrentChanged(object sender, EventArgs e)
        {
            Debug.Assert(sender == _collectionView);
            OnCurrentChanged();
        }

        void OnCurrentChanging(object sender, CurrentChangingEventArgs e)
        {
            Debug.Assert(sender == _collectionView);
            OnCurrentChanging(e);
        }

        void OnViewPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            OnPropertyChanged(e);
        }

        /// <summary>
        /// Handle events from the centralized event table
        /// </summary>
        bool IWeakEventListener.ReceiveWeakEvent(Type managerType, object sender, EventArgs e)
        {
            return false;   // this method is no longer used (but must remain, for compat)
        }

        // Before any modifying access, first call CheckIsUsingInnerView() because
        // a) InternalView is lazily created
        // b) modifying access is only allowed when the InnerView is being used
        // c) modifying access is only allowed when Refresh is not deferred
        void CheckIsUsingInnerView()
        {
            if (IsUsingItemsSource)
                throw new InvalidOperationException(SR.Get(SRID.ItemsSourceInUse));
            EnsureInternalView();
            EnsureCollectionView();
            Debug.Assert(_collectionView != null);
            VerifyRefreshNotDeferred();
        }

        void EndDefer()
        {
            --_deferLevel;

            if (_deferLevel == 0)
            {
                // if there is a _collectionView, there should be a _deferInnerRefresh
                Debug.Assert(_collectionView == null || _deferInnerRefresh != null);

                if (_deferInnerRefresh != null)
                {
                    // set _deferInnerRefresh to null before calling Dispose,
                    // in case Dispose throws an exception.
                    IDisposable deferInnerRefresh = _deferInnerRefresh;
                    _deferInnerRefresh = null;
                    deferInnerRefresh.Dispose();
                }
                else
                {
                    OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
                }
            }
        }

        // Helper to validate that we are not in the middle of a DeferRefresh
        // and throw if that is the case. The reason that this *new* version of VerifyRefreshNotDeferred
        // on ItemCollection is needed is that ItemCollection has its own *new* IsRefreshDeferred
        // which overrides IsRefreshDeferred on the base class (CollectionView), and we need to
        // be sure that we reference that member on the derived class.
        private new void VerifyRefreshNotDeferred()
        {
            #pragma warning disable 1634, 1691 // about to use PreSharp message numbers - unknown to C#
            #pragma warning disable 6503
            // If the Refresh is being deferred to change filtering or sorting of the
            // data by this CollectionView, then CollectionView will not reflect the correct
            // state of the underlying data.

            if (IsRefreshDeferred)
                throw new InvalidOperationException(SR.Get(SRID.NoCheckOrChangeWhenDeferred));

            #pragma warning restore 6503
            #pragma warning restore 1634, 1691
        }

        // SortDescription was added/removed to/from this ItemCollection.SortDescriptions, refresh CollView
        private void SortDescriptionsChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (SortDescriptionsMonitor.Busy)
                return;

            // if we have an inner collection view, keep its .SortDescriptions collection it up-to-date
            if (_collectionView != null && _collectionView.CanSort)
            {
                using (SortDescriptionsMonitor.Enter())
                {
                    SynchronizeCollections<SortDescription>(e, MySortDescriptions, _collectionView.SortDescriptions);
                }
            }

            IsSortingSet = true;       // most recent change came from ItemCollection
        }

        // SortDescription was added/removed to/from inner collectionView
        private void OnInnerSortDescriptionsChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (!IsShapingActive || SortDescriptionsMonitor.Busy)
                return;

            // keep this ItemColl.SortDescriptions in synch with inner collection view's
            using (SortDescriptionsMonitor.Enter())
            {
                SynchronizeCollections<SortDescription>(e, _collectionView.SortDescriptions, MySortDescriptions);
            }

            IsSortingSet = false;      // most recent change came from inner collection view
        }

        // GroupDescription was added/removed to/from this ItemCollection.GroupDescriptions, refresh CollView
        private void GroupDescriptionsChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (GroupDescriptionsMonitor.Busy)
                return;

            // if we have an inner collection view, keep its .SortDescriptions collection it up-to-date
            if (_collectionView != null && _collectionView.CanGroup)
            {
                using (GroupDescriptionsMonitor.Enter())
                {
                    SynchronizeCollections<GroupDescription>(e, MyGroupDescriptions, _collectionView.GroupDescriptions);
                }
            }

            IsGroupingSet = true;       // most recent change came from ItemCollection
        }

        // GroupDescription was added/removed to/from inner collectionView
        private void OnInnerGroupDescriptionsChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (!IsShapingActive || GroupDescriptionsMonitor.Busy)
                return;

            // keep this ItemColl.GroupDescriptions in synch with inner collection view's
            using (GroupDescriptionsMonitor.Enter())
            {
                SynchronizeCollections<GroupDescription>(e, _collectionView.GroupDescriptions, MyGroupDescriptions);
            }

            IsGroupingSet = false;      // most recent change came from inner collection view
        }


        // Property was added/removed to/from this ItemCollection.LiveSortingProperties, refresh CollView
        private void LiveSortingChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (LiveSortingMonitor.Busy)
                return;

            // if we have an inner collection view, keep its LiveSortingProperties collection in sync
            ICollectionViewLiveShaping icvls = _collectionView as ICollectionViewLiveShaping;
            if (icvls != null)
            {
                using (LiveSortingMonitor.Enter())
                {
                    SynchronizeCollections<string>(e, MyLiveSortingProperties, icvls.LiveSortingProperties);
                }
            }

            IsLiveSortingSet = true;       // most recent change came from ItemCollection
        }

        // Property was added/removed to/from inner collectionView's LiveSortingProperties
        private void OnInnerLiveSortingChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (!IsShapingActive || LiveSortingMonitor.Busy)
                return;

            // keep this ItemColl.LiveSortingProperties in sync with inner collection view's
            ICollectionViewLiveShaping icvls = _collectionView as ICollectionViewLiveShaping;
            if (icvls != null)
            {
                using (LiveSortingMonitor.Enter())
                {
                    SynchronizeCollections<string>(e, icvls.LiveSortingProperties, MyLiveSortingProperties);
                }
            }

            IsLiveSortingSet = false;      // most recent change came from inner collection view
        }


        // Property was added/removed to/from this ItemCollection.LiveFilteringProperties, refresh CollView
        private void LiveFilteringChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (LiveFilteringMonitor.Busy)
                return;

            // if we have an inner collection view, keep its LiveFilteringProperties collection in sync
            ICollectionViewLiveShaping icvls = _collectionView as ICollectionViewLiveShaping;
            if (icvls != null)
            {
                using (LiveFilteringMonitor.Enter())
                {
                    SynchronizeCollections<string>(e, MyLiveFilteringProperties, icvls.LiveFilteringProperties);
                }
            }

            IsLiveFilteringSet = true;       // most recent change came from ItemCollection
        }

        // Property was added/removed to/from inner collectionView's LiveFilteringProperties
        private void OnInnerLiveFilteringChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (!IsShapingActive || LiveFilteringMonitor.Busy)
                return;

            // keep this ItemColl.LiveFilteringProperties in sync with inner collection view's
            ICollectionViewLiveShaping icvls = _collectionView as ICollectionViewLiveShaping;
            if (icvls != null)
            {
                using (LiveFilteringMonitor.Enter())
                {
                    SynchronizeCollections<string>(e, icvls.LiveFilteringProperties, MyLiveFilteringProperties);
                }
            }

            IsLiveFilteringSet = false;      // most recent change came from inner collection view
        }


        // Property was added/removed to/from this ItemCollection.LiveGroupingProperties, refresh CollView
        private void LiveGroupingChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (LiveGroupingMonitor.Busy)
                return;

            // if we have an inner collection view, keep its LiveGroupingProperties collection in sync
            ICollectionViewLiveShaping icvls = _collectionView as ICollectionViewLiveShaping;
            if (icvls != null)
            {
                using (LiveGroupingMonitor.Enter())
                {
                    SynchronizeCollections<string>(e, MyLiveGroupingProperties, icvls.LiveGroupingProperties);
                }
            }

            IsLiveGroupingSet = true;       // most recent change came from ItemCollection
        }

        // Property was added/removed to/from inner collectionView's LiveGroupingProperties
        private void OnInnerLiveGroupingChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (!IsShapingActive || LiveGroupingMonitor.Busy)
                return;

            // keep this ItemColl.LiveGroupingProperties in sync with inner collection view's
            ICollectionViewLiveShaping icvls = _collectionView as ICollectionViewLiveShaping;
            if (icvls != null)
            {
                using (LiveGroupingMonitor.Enter())
                {
                    SynchronizeCollections<string>(e, icvls.LiveGroupingProperties, MyLiveGroupingProperties);
                }
            }

            IsLiveGroupingSet = false;      // most recent change came from inner collection view
        }


        // keep collections in sync
        private void SynchronizeCollections<T>(NotifyCollectionChangedEventArgs e, Collection<T> origin, Collection<T> clone)
        {
            if (clone == null)
                return;             // the clone might be lazily-created

            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    Debug.Assert(e.NewStartingIndex >= 0);
                    if (clone.Count + e.NewItems.Count != origin.Count)
                        goto case NotifyCollectionChangedAction.Reset;
                    for (int i = 0; i < e.NewItems.Count; i++)
                    {
                        clone.Insert(e.NewStartingIndex + i, (T)e.NewItems[i]);
                    }
                    break;
                case NotifyCollectionChangedAction.Remove:
                    if (clone.Count - e.OldItems.Count != origin.Count)
                        goto case NotifyCollectionChangedAction.Reset;
                    Debug.Assert(e.OldStartingIndex >= 0);
                    for (int i = 0; i < e.OldItems.Count; i++)
                    {
                        clone.RemoveAt(e.OldStartingIndex);
                    }
                    break;

                case NotifyCollectionChangedAction.Replace:
                    Debug.Assert(e.OldStartingIndex >= 0);
                    if (clone.Count != origin.Count)
                        goto case NotifyCollectionChangedAction.Reset;
                    for (int i = 0; i < e.OldItems.Count; i++)
                    {
                        clone[e.OldStartingIndex + i] = (T)e.NewItems[i];
                    }
                    break;

                case NotifyCollectionChangedAction.Move:
                    Debug.Assert(e.OldStartingIndex >= 0);
                    if (clone.Count != origin.Count)
                        goto case NotifyCollectionChangedAction.Reset;
                    if (e.NewItems.Count == 1)
                    {
                        clone.RemoveAt(e.OldStartingIndex);
                        clone.Insert(e.NewStartingIndex, (T)e.NewItems[0]);
                    }
                    else
                    {
                        for (int i = 0; i < e.OldItems.Count; i++)
                        {
                            clone.RemoveAt(e.OldStartingIndex);
                        }
                        for (int i = 0; i < e.NewItems.Count; i++)
                        {
                            clone.Insert(e.NewStartingIndex + i, (T)e.NewItems[i]);
                        }
                    }
                    break;

                // this arm also handles cases where the two collections have gotten
                // out of sync (typically because exceptions prevented a previous sync
                // from happening)
                case NotifyCollectionChangedAction.Reset:
                    CloneList(clone, origin);
                    break;

                default:
                    throw new NotSupportedException(SR.Get(SRID.UnexpectedCollectionChangeAction, e.Action));
            }
        }

        private void CloneList(IList clone, IList master)
        {
            // if either party is null, do nothing.  Allowing null lets the caller
            // avoid a lazy instantiation of the Sort/Group description collection.
            if (clone == null || master == null)
                return;

            if (clone.Count > 0)
            {
                clone.Clear();
            }

            for (int i = 0, n = master.Count; i < n; ++i)
            {
                clone.Add(master[i]);
            }
        }

        #endregion Private Methods

        #region Shaping storage

        private bool IsShapingActive
        {
            get { return _shapingStorage != null; }
        }

        private void EnsureShapingStorage()
        {
            if (!IsShapingActive)
            {
                _shapingStorage = new ShapingStorage();
            }
        }


        private SortDescriptionCollection MySortDescriptions
        {
            get { return IsShapingActive ? _shapingStorage._sort : null; }
            set { EnsureShapingStorage(); _shapingStorage._sort = value; }
        }

        private bool IsSortingSet
        {
            get { return IsShapingActive ? _shapingStorage._isSortingSet : false; }
            set
            {
                Debug.Assert(IsShapingActive, "Shaping storage not available");
                _shapingStorage._isSortingSet = value;
            }
        }

        private MonitorWrapper SortDescriptionsMonitor
        {
            get
            {
                if (_shapingStorage._sortDescriptionsMonitor == null)
                    _shapingStorage._sortDescriptionsMonitor = new MonitorWrapper();
                return _shapingStorage._sortDescriptionsMonitor;
            }
        }


        private Predicate<object> MyFilter
        {
            get { return IsShapingActive ? _shapingStorage._filter : null; }
            set { EnsureShapingStorage(); _shapingStorage._filter = value; }
        }


        private ObservableCollection<GroupDescription> MyGroupDescriptions
        {
            get { return IsShapingActive ? _shapingStorage._groupBy : null; }
            set { EnsureShapingStorage(); _shapingStorage._groupBy = value; }
        }

        private bool IsGroupingSet
        {
            get { return IsShapingActive ? _shapingStorage._isGroupingSet : false; }
            set
            {
                if (IsShapingActive)
                    _shapingStorage._isGroupingSet = value;
                else
                    Debug.Assert(!value, "Shaping storage not available");
            }
        }

        private MonitorWrapper GroupDescriptionsMonitor
        {
            get
            {
                if (_shapingStorage._groupDescriptionsMonitor == null)
                    _shapingStorage._groupDescriptionsMonitor = new MonitorWrapper();
                return _shapingStorage._groupDescriptionsMonitor;
            }
        }


        private bool? MyIsLiveSorting
        {
            get { return IsShapingActive ? _shapingStorage._isLiveSorting : null; }
            set { EnsureShapingStorage(); _shapingStorage._isLiveSorting = value; }
        }

        private ObservableCollection<string> MyLiveSortingProperties
        {
            get { return IsShapingActive ? _shapingStorage._liveSortingProperties : null; }
            set { EnsureShapingStorage(); _shapingStorage._liveSortingProperties = value; }
        }

        private bool IsLiveSortingSet
        {
            get { return IsShapingActive ? _shapingStorage._isLiveSortingSet : false; }
            set
            {
                Debug.Assert(IsShapingActive, "Shaping storage not available");
                _shapingStorage._isLiveSortingSet = value;
            }
        }

        private MonitorWrapper LiveSortingMonitor
        {
            get
            {
                if (_shapingStorage._liveSortingMonitor == null)
                    _shapingStorage._liveSortingMonitor = new MonitorWrapper();
                return _shapingStorage._liveSortingMonitor;
            }
        }


        private bool? MyIsLiveFiltering
        {
            get { return IsShapingActive ? _shapingStorage._isLiveFiltering : null; }
            set { EnsureShapingStorage(); _shapingStorage._isLiveFiltering = value; }
        }

        private ObservableCollection<string> MyLiveFilteringProperties
        {
            get { return IsShapingActive ? _shapingStorage._liveFilteringProperties : null; }
            set { EnsureShapingStorage(); _shapingStorage._liveFilteringProperties = value; }
        }

        private bool IsLiveFilteringSet
        {
            get { return IsShapingActive ? _shapingStorage._isLiveFilteringSet : false; }
            set
            {
                Debug.Assert(IsShapingActive, "Shaping storage not available");
                _shapingStorage._isLiveFilteringSet = value;
            }
        }

        private MonitorWrapper LiveFilteringMonitor
        {
            get
            {
                if (_shapingStorage._liveFilteringMonitor == null)
                    _shapingStorage._liveFilteringMonitor = new MonitorWrapper();
                return _shapingStorage._liveFilteringMonitor;
            }
        }


        private bool? MyIsLiveGrouping
        {
            get { return IsShapingActive ? _shapingStorage._isLiveGrouping : null; }
            set { EnsureShapingStorage(); _shapingStorage._isLiveGrouping = value; }
        }

        private ObservableCollection<string> MyLiveGroupingProperties
        {
            get { return IsShapingActive ? _shapingStorage._liveGroupingProperties : null; }
            set { EnsureShapingStorage(); _shapingStorage._liveGroupingProperties = value; }
        }

        private bool IsLiveGroupingSet
        {
            get { return IsShapingActive ? _shapingStorage._isLiveGroupingSet : false; }
            set
            {
                Debug.Assert(IsShapingActive, "Shaping storage not available");
                _shapingStorage._isLiveGroupingSet = value;
            }
        }

        private MonitorWrapper LiveGroupingMonitor
        {
            get
            {
                if (_shapingStorage._liveGroupingMonitor == null)
                    _shapingStorage._liveGroupingMonitor = new MonitorWrapper();
                return _shapingStorage._liveGroupingMonitor;
            }
        }

        #endregion Shaping storage

        //------------------------------------------------------
        //
        //  Private Fields
        //
        //------------------------------------------------------

        #region Private Fields

        private InnerItemCollectionView  _internalView;     // direct-mode list and view
        private IEnumerable         _itemsSource;           // ItemsControl.ItemsSource property
        private CollectionView      _collectionView;        // delegate ICollectionView
        private int                 _defaultCapacity = 16;

        private bool    _isUsingItemsSource;        // true when using ItemsSource
        private bool    _isInitializing;            // when true, ItemCollection does not listen to events of _collectionView

        private int         _deferLevel;
        private IDisposable _deferInnerRefresh;
        private ShapingStorage  _shapingStorage;

        private WeakReference       _modelParent;       // use WeakRef to avoid leaking the parent

        #endregion Private Fields


        //------------------------------------------------------
        //
        //  Private Types
        //
        //------------------------------------------------------

        #region Private Types

        // ItemCollection rarely uses shaping directly.   Make it pay-for-play
        private class ShapingStorage
        {
            public bool    _isSortingSet;       // true when user has added to this.SortDescriptions
            public bool    _isGroupingSet;      // true when user has added to this.GroupDescriptions
            public bool    _isLiveSortingSet;   // true when user has added to this.LiveSortingProperties
            public bool    _isLiveFilteringSet; // true when user has added to this.LiveFilteringProperties
            public bool    _isLiveGroupingSet;  // true when user has added to this.LiveGroupingProperties

            public SortDescriptionCollection    _sort;      // storage for SortDescriptions; will forward to _collectionView.SortDescriptions when available
            public Predicate<object>            _filter;    // storage for Filter when _collectionView is not available
            public ObservableCollection<GroupDescription> _groupBy; // storage for GroupDescriptions; will forward to _collectionView.GroupDescriptions when available

            public bool?    _isLiveSorting;     // true if live Sorting is requested
            public bool?    _isLiveFiltering;   // true if live Filtering is requested
            public bool?    _isLiveGrouping;    // true if live Grouping is requested

            public ObservableCollection<string> _liveSortingProperties; // storage for LiveSortingProperties; will forward to _collectionView.LiveSortingProperties when available
            public ObservableCollection<string> _liveFilteringProperties; // storage for LiveFilteringProperties; will forward to _collectionView.LiveFilteringProperties when available
            public ObservableCollection<string> _liveGroupingProperties; // storage for LiveGroupingProperties; will forward to _collectionView.LiveGroupingProperties when available

            public MonitorWrapper      _sortDescriptionsMonitor;
            public MonitorWrapper      _groupDescriptionsMonitor;
            public MonitorWrapper      _liveSortingMonitor;
            public MonitorWrapper      _liveFilteringMonitor;
            public MonitorWrapper      _liveGroupingMonitor;
        }

        private class DeferHelper : IDisposable
        {
            public DeferHelper(ItemCollection itemCollection)
            {
                _itemCollection = itemCollection;
            }

            public void Dispose()
            {
                if (_itemCollection != null)
                {
                    _itemCollection.EndDefer();
                    _itemCollection = null;
                }

                GC.SuppressFinalize(this);
            }

            private ItemCollection _itemCollection;
        }
        #endregion
    }
}
