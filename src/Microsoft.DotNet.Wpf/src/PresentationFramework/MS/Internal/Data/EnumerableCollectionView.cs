// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// Description: Collection view over an IEnumerable.
//

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.Threading;
using System.Windows;
using System.Windows.Data;

namespace MS.Internal.Data
{
    ///<summary>
    /// Collection view over an IEnumerable.
    ///</summary>
    internal class EnumerableCollectionView : CollectionView, IItemProperties
    {
        #region Constructors

        //------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------

        // Set up a ListCollectionView over the
        // snapshot.  We will delegate all CollectionView functionality
        // to this view.
        internal EnumerableCollectionView(IEnumerable source)
            : base(source, -1)
        {
            _snapshot = new ObservableCollection<object>();

            // if the source doesn't raise collection change events, try to
            // detect changes by polling the enumerator
            _pollForChanges = !(source is INotifyCollectionChanged);

            LoadSnapshotCore(source);

            if (_snapshot.Count > 0)
            {
                SetCurrent(_snapshot[0], 0, 1);
            }
            else
            {
                SetCurrent(null, -1, 0);
            }

            _view = new ListCollectionView(_snapshot);

            INotifyCollectionChanged incc = _view as INotifyCollectionChanged;
            incc.CollectionChanged += new NotifyCollectionChangedEventHandler(_OnViewChanged);

            INotifyPropertyChanged ipc = _view as INotifyPropertyChanged;
            ipc.PropertyChanged += new PropertyChangedEventHandler(_OnPropertyChanged);

            _view.CurrentChanging += new CurrentChangingEventHandler(_OnCurrentChanging);
            _view.CurrentChanged += new EventHandler(_OnCurrentChanged);
        }

        #endregion Constructors

        //------------------------------------------------------
        //
        //  Interfaces
        //
        //------------------------------------------------------

        #region ICollectionView

        /// <summary>
        /// Culture to use during sorting.
        /// </summary>
        public override System.Globalization.CultureInfo Culture
        {
            get { return _view.Culture; }
            set { _view.Culture = value; }
        }

        /// <summary>
        /// Return true if the item belongs to this view.  No assumptions are
        /// made about the item. This method will behave similarly to IList.Contains().
        /// If the caller knows that the item belongs to the
        /// underlying collection, it is more efficient to call PassesFilter.
        /// </summary>
        public override bool Contains(object item)
        {
            EnsureSnapshot();
            return _view.Contains(item);
        }

        /// <summary>
        /// Set/get a filter callback to filter out items in collection.
        /// This property will always accept a filter, but the collection view for the
        /// underlying InnerList or ItemsSource may not actually support filtering.
        /// Please check <seealso cref="CanFilter"/>
        /// </summary>
        /// <exception cref="NotSupportedException">
        /// Collections assigned to ItemsSource may not support filtering and could throw a NotSupportedException.
        /// Use <seealso cref="CanSort"/> property to test if sorting is supported before adding
        /// to SortDescriptions.
        /// </exception>
        public override Predicate<object> Filter
        {
            get { return _view.Filter; }
            set { _view.Filter = value; }
        }

        /// <summary>
        /// Test if this ICollectionView supports filtering before assigning
        /// a filter callback to <seealso cref="Filter"/>.
        /// </summary>
        public override bool CanFilter
        {
            get { return _view.CanFilter; }
        }

        /// <summary>
        /// Set/get Sort criteria to sort items in collection.
        /// </summary>
        /// <remarks>
        /// <p>
        /// Clear a sort criteria by assigning SortDescription.Empty to this property.
        /// One or more sort criteria in form of <seealso cref="SortDescription"/>
        /// can be used, each specifying a property and direction to sort by.
        /// </p>
        /// </remarks>
        /// <exception cref="NotSupportedException">
        /// Simpler implementations do not support sorting and will throw a NotSupportedException.
        /// Use <seealso cref="CanSort"/> property to test if sorting is supported before adding
        /// to SortDescriptions.
        /// </exception>
        public override SortDescriptionCollection SortDescriptions
        {
            get { return _view.SortDescriptions; }
        }

        /// <summary>
        /// Test if this ICollectionView supports sorting before adding
        /// to <seealso cref="SortDescriptions"/>.
        /// </summary>
        public override bool CanSort
        {
            get { return _view.CanSort; }
        }

        /// <summary>
        /// Returns true if this view really supports grouping.
        /// When this returns false, the rest of the interface is ignored.
        /// </summary>
        public override bool CanGroup
        {
            get { return _view.CanGroup; }
        }

        /// <summary>
        /// The description of grouping, indexed by level.
        /// </summary>
        public override ObservableCollection<GroupDescription> GroupDescriptions
        {
            get { return _view.GroupDescriptions; }
        }

        /// <summary>
        /// The top-level groups, constructed according to the descriptions
        /// given in GroupDescriptions.
        /// </summary>
        public override ReadOnlyObservableCollection<object> Groups
        {
            get { return _view.Groups; }
        }

        /// <summary>
        /// Enter a Defer Cycle.
        /// Defer cycles are used to coalesce changes to the ICollectionView.
        /// </summary>
        public override IDisposable DeferRefresh()
        {
            return _view.DeferRefresh();
        }

        /// <summary> Return current item. </summary>
        public override object CurrentItem
        {
            get { return _view.CurrentItem; }
        }

        /// <summary>
        /// The ordinal position of the <seealso cref="CurrentItem"/> within the (optionally
        /// sorted and filtered) view.
        /// </summary>
        public override int CurrentPosition
        {
            get { return _view.CurrentPosition; }
        }

        /// <summary> Return true if currency is beyond the end (End-Of-File). </summary>
        public override bool IsCurrentAfterLast
        {
            get { return _view.IsCurrentAfterLast; }
        }

        /// <summary> Return true if currency is before the beginning (Beginning-Of-File). </summary>
        public override bool IsCurrentBeforeFirst
        {
            get { return _view.IsCurrentBeforeFirst; }
        }

        /// <summary> Move to the first item. </summary>
        public override bool MoveCurrentToFirst()
        {
            return _view.MoveCurrentToFirst();
        }

        /// <summary> Move to the previous item. </summary>
        public override bool MoveCurrentToPrevious()
        {
            return _view.MoveCurrentToPrevious();
        }

        /// <summary> Move to the next item. </summary>
        public override bool MoveCurrentToNext()
        {
            return _view.MoveCurrentToNext();
        }

        /// <summary> Move to the last item. </summary>
        public override bool MoveCurrentToLast()
        {
            return _view.MoveCurrentToLast();
        }

        /// <summary> Move to the given item. </summary>
        public override bool MoveCurrentTo(object item)
        {
            return _view.MoveCurrentTo(item);
        }

        /// <summary>Move CurrentItem to this index</summary>
        public override bool MoveCurrentToPosition(int position)
        {
            //
            // If the index is out of range here, I'll let the
            // _view be the one to make that determination.
            //
            return _view.MoveCurrentToPosition(position);
        }

        #endregion ICollectionView

        #region IItemProperties

        /// <summary>
        /// Returns information about the properties available on items in the
        /// underlying collection.  This information may come from a schema, from
        /// a type descriptor, from a representative item, or from some other source
        /// known to the view.
        /// </summary>
        public ReadOnlyCollection<ItemPropertyInfo> ItemProperties
        {
            get { return ((IItemProperties)_view).ItemProperties; }
        }

        #endregion IItemProperties

        //------------------------------------------------------
        //
        //  Public Properties
        //
        //------------------------------------------------------

        #region Public Properties

        /// <summary>
        /// Return the number of records (or -1, meaning "don't know").
        /// A virtualizing view should return the best estimate it can
        /// without de-virtualizing all the data.  A non-virtualizing view
        /// should return the exact count of its (filtered) data.
        /// </summary>
        public override int Count
        {
            get
            {
                EnsureSnapshot();
                return _view.Count;
            }
        }

        public override bool IsEmpty
        {
            get
            {
                EnsureSnapshot();
                return (_view != null) ? _view.IsEmpty : true;
            }
        }

        /// <summary>
        ///     Returns true if this view needs to be refreshed.
        /// </summary>
        public override bool NeedsRefresh
        {
            get { return _view.NeedsRefresh; }
        }

        #endregion Public Properties



        //------------------------------------------------------
        //
        //  Public Methods
        //
        //------------------------------------------------------

        #region Public Methods

        /// <summary> Return the index where the given item appears, or -1 if doesn't appear.
        /// </summary>
        /// <param name="item">data item</param>
        public override int IndexOf(object item)
        {
            EnsureSnapshot();
            return _view.IndexOf(item);
        }

        /// <summary>
        /// Return true if the item belongs to this view.  The item is assumed to belong to the
        /// underlying DataCollection;  this method merely takes filters into account.
        /// It is commonly used during collection-changed notifications to determine if the added/removed
        /// item requires processing.
        /// Returns true if no filter is set on collection view.
        /// </summary>
        public override bool PassesFilter(object item)
        {
            if (_view.CanFilter && _view.Filter != null)
                return _view.Filter(item);

            return true;
        }

        /// <summary>
        /// Retrieve item at the given zero-based index in this CollectionView.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Thrown if index is out of range
        /// </exception>
        public override object GetItemAt(int index)
        {
            EnsureSnapshot();
            return _view.GetItemAt(index);
        }

        #endregion Public Methods


        //------------------------------------------------------
        //
        //  Protected Methods
        //
        //------------------------------------------------------

        #region Protected Methods

        /// <summary> Implementation of IEnumerable.GetEnumerator().
        /// This provides a way to enumerate the members of the collection
        /// without changing the currency.
        /// </summary>
        protected override IEnumerator GetEnumerator()
        {
            EnsureSnapshot();
            return ((IEnumerable)_view).GetEnumerator();
        }

        /// Re-create the view, using any <seealso cref="SortDescriptions"/>.
        protected override void RefreshOverride()
        {
            LoadSnapshot(SourceCollection);
        }

        /// <summary>
        ///     Must be implemented by the derived classes to process a single change on the
        ///     UI thread.  The UI thread will have already been entered by now.
        /// </summary>
        /// <param name="args">
        ///     The NotifyCollectionChangedEventArgs to be processed.
        /// </param>
        protected override void ProcessCollectionChanged(NotifyCollectionChangedEventArgs args)
        {
            // ignore events received during initialization
            if (_view == null)
                return;

            // apply the change to the snapshot
            switch (args.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    if (args.NewStartingIndex < 0 || _snapshot.Count <= args.NewStartingIndex)
                    {   // append
                        for (int i = 0; i < args.NewItems.Count; ++i)
                        {
                            _snapshot.Add(args.NewItems[i]);
                        }
                    }
                    else
                    {   // insert
                        for (int i = args.NewItems.Count - 1; i >= 0; --i)
                        {
                            _snapshot.Insert(args.NewStartingIndex, args.NewItems[i]);
                        }
                    }
                    break;

                case NotifyCollectionChangedAction.Remove:
                    if (args.OldStartingIndex < 0)
                        throw new InvalidOperationException(SR.Get(SRID.RemovedItemNotFound));

                    for (int i = args.OldItems.Count - 1, index = args.OldStartingIndex + i; i >= 0; --i, --index)
                    {
                        if (!System.Windows.Controls.ItemsControl.EqualsEx(args.OldItems[i], _snapshot[index]))
                            // replace error message with a better one
                            throw new InvalidOperationException(SR.Get(SRID.AddedItemNotAtIndex, index));
                        _snapshot.RemoveAt(index);
                    }
                    break;

                case NotifyCollectionChangedAction.Replace:
                    for (int i = args.NewItems.Count - 1, index = args.NewStartingIndex + i; i >= 0; --i, --index)
                    {
                        if (!System.Windows.Controls.ItemsControl.EqualsEx(args.OldItems[i], _snapshot[index]))
                            // replace error message with a better one
                            throw new InvalidOperationException(SR.Get(SRID.AddedItemNotAtIndex, index));
                        _snapshot[index] = args.NewItems[i];
                    }
                    break;

                case NotifyCollectionChangedAction.Move:
                    if (args.NewStartingIndex < 0)
                        throw new InvalidOperationException(SR.Get(SRID.CannotMoveToUnknownPosition));

                    if (args.OldStartingIndex < args.NewStartingIndex)
                    {
                        for (int i = args.OldItems.Count - 1,
                                oldIndex = args.OldStartingIndex + i,
                                newIndex = args.NewStartingIndex + i;
                            i >= 0;
                            --i, --oldIndex, --newIndex)
                        {
                            if (!System.Windows.Controls.ItemsControl.EqualsEx(args.OldItems[i], _snapshot[oldIndex]))
                                // replace error message with a better one
                                throw new InvalidOperationException(SR.Get(SRID.AddedItemNotAtIndex, oldIndex));
                            _snapshot.Move(oldIndex, newIndex);
                        }
                    }
                    else
                    {
                        for (int i = 0,
                                oldIndex = args.OldStartingIndex + i,
                                newIndex = args.NewStartingIndex + i;
                            i < args.OldItems.Count;
                            ++i, ++oldIndex, ++newIndex)
                        {
                            if (!System.Windows.Controls.ItemsControl.EqualsEx(args.OldItems[i], _snapshot[oldIndex]))
                                // replace error message with a better one
                                throw new InvalidOperationException(SR.Get(SRID.AddedItemNotAtIndex, oldIndex));
                            _snapshot.Move(oldIndex, newIndex);
                        }
                    }

                    break;

                case NotifyCollectionChangedAction.Reset:
                    LoadSnapshot(SourceCollection);
                    break;
            }
        }

        #endregion Protected Methods


        //------------------------------------------------------
        //
        //  Private Methods
        //
        //------------------------------------------------------

        #region Private Methods

        // Load a snapshot of the contents of the IEnumerable into the
        // ObservableCollection.
        void LoadSnapshot(IEnumerable source)
        {
            // force currency off the collection (gives user a chance to save dirty information)
            OnCurrentChanging();

            // remember the values of the scalar properties, so that we can restore
            // them and raise events after reloading the data
            object oldCurrentItem = CurrentItem;
            int oldCurrentPosition = CurrentPosition;
            bool oldIsCurrentBeforeFirst = IsCurrentBeforeFirst;
            bool oldIsCurrentAfterLast = IsCurrentAfterLast;

            // reload the data
            LoadSnapshotCore(source);

            // tell listeners everything has changed
            OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));

            OnCurrentChanged();

            if (IsCurrentAfterLast != oldIsCurrentAfterLast)
                OnPropertyChanged(new PropertyChangedEventArgs(IsCurrentAfterLastPropertyName));

            if (IsCurrentBeforeFirst != oldIsCurrentBeforeFirst)
                OnPropertyChanged(new PropertyChangedEventArgs(IsCurrentBeforeFirstPropertyName));

            if (oldCurrentPosition != CurrentPosition)
                OnPropertyChanged(new PropertyChangedEventArgs(CurrentPositionPropertyName));

            if (oldCurrentItem != CurrentItem)
                OnPropertyChanged(new PropertyChangedEventArgs(CurrentItemPropertyName));
        }

        void LoadSnapshotCore(IEnumerable source)
        {
            IEnumerator ie = source.GetEnumerator();

            using (IgnoreViewEvents())
            {
                _snapshot.Clear();

                while (ie.MoveNext())
                {
                    _snapshot.Add(ie.Current);
                }
            }

            // if we're tracking changes, use the new enumerator
            if (_pollForChanges)
            {
                IEnumerator temp = _trackingEnumerator;
                _trackingEnumerator = ie;
                ie = temp;
            }

            // we're done with an enumerator - dispose it
            IDisposable id = ie as IDisposable;
            if (id != null)
            {
                id.Dispose();
            }
        }

        // if the IEnumerable has changed, bring the snapshot up to date.
        // (This isn't necessary if the IEnumerable is also INotifyCollectionChanged
        // because we keep the snapshot in sync incrementally.)
        void EnsureSnapshot()
        {
            if (_pollForChanges)
            {
                try
                {
                    _trackingEnumerator.MoveNext();
                }
                catch (InvalidOperationException)
                {
                    // We need to remove the code that detects un-notified changes 
                    // This "feature" is necessarily incomplete (we cannot detect
                    // the changes when they happen, only as a side-effect of some
                    // later operation), and inconsistent (none of the other
                    // collection views does this).  Instead we should document
                    // that changing a collection without raising a notification
                    // is not supported, and let the chips fall where they may.
                    //
                    // For WPF 3.5 (SP1), use TraceData to warn the user that
                    // this scenario is not really supported.
                    if (TraceData.IsEnabled && !_warningHasBeenRaised)
                    {
                        _warningHasBeenRaised = true;
                        TraceData.TraceAndNotify(TraceEventType.Warning,
                            TraceData.CollectionChangedWithoutNotification(SourceCollection.GetType().FullName));
                    }

                    // collection was changed - start over with a new enumerator
                    LoadSnapshotCore(SourceCollection);
                }
            }
        }

        IDisposable IgnoreViewEvents()
        {
            return new IgnoreViewEventsHelper(this);
        }

        void BeginIgnoreEvents()
        {
            ++_ignoreEventsLevel;
        }

        void EndIgnoreEvents()
        {
            --_ignoreEventsLevel;
        }

        // forward events from the internal view to our own listeners

        void _OnPropertyChanged(object sender, PropertyChangedEventArgs args)
        {
            if (_ignoreEventsLevel != 0)
                return;

            OnPropertyChanged(args);
        }

        void _OnViewChanged(object sender, NotifyCollectionChangedEventArgs args)
        {
            if (_ignoreEventsLevel != 0)
                return;

            OnCollectionChanged(args);
        }

        void _OnCurrentChanging(object sender, CurrentChangingEventArgs args)
        {
            if (_ignoreEventsLevel != 0)
                return;

            OnCurrentChanging();
        }

        void _OnCurrentChanged(object sender, EventArgs args)
        {
            if (_ignoreEventsLevel != 0)
                return;

            OnCurrentChanged();
        }

        #endregion Private Methods

        #region Private Data

        //------------------------------------------------------
        //
        //  Private Fields
        //
        //------------------------------------------------------

        ListCollectionView _view;
        ObservableCollection<object> _snapshot;
        IEnumerator _trackingEnumerator;
        int _ignoreEventsLevel;
        bool _pollForChanges;
        bool _warningHasBeenRaised;

        class IgnoreViewEventsHelper : IDisposable
        {
            public IgnoreViewEventsHelper(EnumerableCollectionView parent)
            {
                _parent = parent;
                _parent.BeginIgnoreEvents();
            }

            public void Dispose()
            {
                if (_parent != null)
                {
                    _parent.EndIgnoreEvents();
                    _parent = null;
                }

                GC.SuppressFinalize(this);
            }

            EnumerableCollectionView _parent;
        }
        #endregion Private Data
    }
}

