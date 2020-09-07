// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// Description: CompositeCollectionView provides the flattened view of an CompositeCollection.
//
// See specs at ItemsControl.mht
//              CollectionView.mht
//

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Data;
using MS.Internal;              // Invariant.Assert
using MS.Internal.Controls;
using System.Windows.Controls;
using MS.Internal.Utility;
using MS.Utility;
using MS.Internal.Hashing.PresentationFramework;    // HashHelper

#pragma warning disable 1634, 1691  // suppressing PreSharp warnings

namespace MS.Internal.Data
{
    /// <summary>
    /// CompositeCollectionView provides the flattened view of an CompositeCollection.
    /// </summary>
    internal sealed class CompositeCollectionView : CollectionView
    {
        //------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------

        // create the new CompositeCollectionView for a CompositeCollection
        internal CompositeCollectionView(CompositeCollection collection)
            : base(collection, -1 /* don't move to first */)    // base.ctor also subscribes to CollectionChanged event of CompositeCollection
        {
            _collection = collection;
            _collection.ContainedCollectionChanged += new NotifyCollectionChangedEventHandler(OnContainedCollectionChanged);

            // Do the equivalent of MoveCurrentToFirst(), without calling virtuals
            int currentPosition = PrivateIsEmpty ? -1 : 0;
            int count = PrivateIsEmpty ? 0 : 1;
            SetCurrent(GetItem(currentPosition, out _currentPositionX, out _currentPositionY), currentPosition, count);
        }


        //------------------------------------------------------
        //
        //  Public Properties
        //
        //------------------------------------------------------

        #region Public Properties

        /// <summary>
        /// Return the estimated number of records
        /// </summary>
        /// <remarks>
        /// Count includes the number of single item in the CompositeCollection
        /// and the counts from the collection views of any sub-collections
        /// contained in <seealso cref="CollectionContainer"/>.
        /// Empty collection containers do not add to the count.
        /// </remarks>
        public override int Count
        {
            get
            {
                // _count may also be updated through CacheCount() in
                // FindItem(), GetItem(), and OnContainedCollectionChanged()

                if (_count == -1)
                {
                    _count = CountDeep(_collection.Count);
                    // no need to raise PropertyChange for cache initialization
                }
                return _count;
            }
        }

        /// <summary>
        /// Returns true if the resulting (filtered) view is emtpy.
        /// </summary>
        /// <remarks>
        /// Checks with all the collection views of any sub-collections
        /// contained in <seealso cref="CollectionContainer"/>.
        /// </remarks>
        // This is faster than calling (Count == 0) because it stops at first item found
        public override bool IsEmpty
        {
            get { return PrivateIsEmpty; }
        }

        private bool PrivateIsEmpty
        {
            get
            {
                if (_count < 0)     // if count cache is invalid
                {
                    for (int i = 0; i < _collection.Count; ++i)
                    {
                        CollectionContainer cc = _collection[i] as CollectionContainer;
                        if (cc == null || cc.ViewCount != 0)    // single item or non-empty sub-collection
                        {
                            return false;
                        }
                    }
                    CacheCount(0);  // now that we know it's empty, cache it!
                }
                return (_count == 0);
            }
        }

        /// <summary>
        /// Return true if <seealso cref="CollectionView.CurrentItem"/> is beyond the end or the collection is empty.
        /// </summary>
        public override bool IsCurrentAfterLast
        {
            get
            {
                // REVIEW: should we return true whenever collection is empty?
                // This is bug-for-bug the same as ListCollView
                return (IsEmpty || (_currentPositionX >= _collection.Count));
            }
        }

        /// <summary>
        /// Return true if <seealso cref="CollectionView.CurrentItem"/> is before the beginning or the collection is empty.
        /// </summary>
        public override bool IsCurrentBeforeFirst
        {
            get
            {
                // REVIEW: should we return true whenever collection is empty?
                // This is bug-for-bug the same as ListCollView
                return (IsEmpty || (_currentPositionX < 0));
            }
        }

        /// <summary>
        /// Indicates whether or not this ICollectionView can do any filtering.
        /// When false, set <seealso cref="CollectionView.Filter"/> will throw an exception.
        /// </summary>
        public override bool CanFilter
        {
            get
            {
                return false;
            }
        }

        #endregion Public Properties


        //------------------------------------------------------
        //
        //  Public Methods
        //
        //------------------------------------------------------

        #region Public Methods

        /// <summary>
        /// Return true if the item belongs to this view.  No assumptions are
        /// made about the item. This method will behave similarly to IList.Contains()
        /// and will do an exhaustive search through all items in the flattened view.
        /// </summary>
        public override bool Contains(object item)
        {
            return (FindItem(item, false) >= 0);
        }

        /// <summary>
        /// Return the index where the given item belongs
        /// </summary>
        /// <param name="item">data item</param>
        public override int IndexOf(object item)
        {
            return FindItem(item, false);
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
            if (index < 0)
                throw new ArgumentOutOfRangeException("index");

            int positionX, positionY;
            object item = GetItem(index, out positionX, out positionY);

            if (item == s_afterLast)
            {
                // couldn't find item at index
                item = null;
                throw new ArgumentOutOfRangeException("index");
            }
            else
            {
                return item;
            }
        }

        /// <summary>
        /// Move <seealso cref="ICollectionView.CurrentItem"/> to the given item.
        /// If the item is not found, move to BeforeFirst.
        /// </summary>
        /// <param name="item">Move Current to this item.</param>
        /// <returns>true if <seealso cref="ICollectionView.CurrentItem"/> points to an item within the view.</returns>
        public override bool MoveCurrentTo(object item)
        {
            // if already on item, don't do anything
            if (ItemsControl.EqualsEx(CurrentItem, item))
            {
                // also check that we're not fooled by a false null CurrentItem
                if (item != null || IsCurrentInView)
                    return IsCurrentInView;
            }

            if (!IsEmpty)   // when empty, don't bother looking, and currency stays at BeforeFirst.
                FindItem(item, true);
            return IsCurrentInView;
        }

        /// <summary>
        /// Move <seealso cref="ICollectionView.CurrentItem"/> to the first item.
        /// </summary>
        /// <returns>true if <seealso cref="ICollectionView.CurrentItem"/> points to an item within the view.</returns>
        public override bool MoveCurrentToFirst()
        {
            if (IsEmpty)
                return false;
            return _MoveTo(0);
        }

        /// <summary>
        /// Move <seealso cref="ICollectionView.CurrentItem"/> to the last item.
        /// </summary>
        /// <returns>true if <seealso cref="ICollectionView.CurrentItem"/> points to an item within the view.</returns>
        public override bool MoveCurrentToLast()
        {
            bool oldIsCurrentAfterLast = IsCurrentAfterLast;
            bool oldIsCurrentBeforeFirst = IsCurrentBeforeFirst;

            int newPositionX, newPositionY;
            int lastPosition = Count - 1;
            object lastItem = GetLastItem(out newPositionX, out newPositionY);  // searches backwards

            if (((CurrentPosition != lastPosition) || (CurrentItem != lastItem))
                && OKToChangeCurrent())
            {
                _currentPositionX = newPositionX;
                _currentPositionY = newPositionY;
                SetCurrent(lastItem, lastPosition);
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

        /// <summary>
        /// Move <seealso cref="ICollectionView.CurrentItem"/> to the next item.
        /// </summary>
        /// <returns>true if <seealso cref="ICollectionView.CurrentItem"/> points to an item within the view.</returns>
        public override bool MoveCurrentToNext()
        {
            if (IsCurrentAfterLast)
                return false;
            return _MoveTo(CurrentPosition + 1);
        }

        /// <summary>
        /// Move <seealso cref="ICollectionView.CurrentItem"/> to the previous item.
        /// </summary>
        /// <returns>true if <seealso cref="ICollectionView.CurrentItem"/> points to an item within the view.</returns>
        public override bool MoveCurrentToPrevious()
        {
            if (IsCurrentBeforeFirst)
                return false;
            return _MoveTo(CurrentPosition - 1);
        }

        /// <summary>
        /// Move <seealso cref="CollectionView.CurrentItem"/> to the item at the given index.
        /// </summary>
        /// <param name="position">Move CurrentItem to this index</param>
        /// <returns>true if <seealso cref="CollectionView.CurrentItem"/> points to an item within the view.</returns>
        /// <exception cref="ArgumentOutOfRangeException">
        /// position is less than Before-First (-1) or greater than After-Last (Count)
        /// </exception>
        public override bool MoveCurrentToPosition(int position)
        {
            if (position < -1)
                throw new ArgumentOutOfRangeException("position");

            int newPositionX, newPositionY;
            object item = GetItem(position, out newPositionX, out newPositionY);

            if (position != CurrentPosition || item != CurrentItem)
            {
                if (item == s_afterLast)
                {
                    item = null;
                    // check upper-bound only after GetItem() to avoid unnecessary pre-counting
                    if (position > Count)
                    {
                        throw new ArgumentOutOfRangeException("position");
                    }
                }

                if (OKToChangeCurrent())
                {
                    bool oldIsCurrentAfterLast = IsCurrentAfterLast;
                    bool oldIsCurrentBeforeFirst = IsCurrentBeforeFirst;

                    _currentPositionX = newPositionX;
                    _currentPositionY = newPositionY;
                    SetCurrent(item, position);
                    OnCurrentChanged();

                    if (IsCurrentAfterLast != oldIsCurrentAfterLast)
                        OnPropertyChanged(IsCurrentAfterLastPropertyName);

                    if (IsCurrentBeforeFirst != oldIsCurrentBeforeFirst)
                        OnPropertyChanged(IsCurrentBeforeFirstPropertyName);

                    OnPropertyChanged(CurrentPositionPropertyName);
                    OnPropertyChanged(CurrentItemPropertyName);
                }
            }
            return IsCurrentInView;
        }

        /// <summary>
        /// Re-create the view over the associated CompositeCollection
        /// </summary>
        /// <remarks>
        /// Since CompositeCollectionView does not support sorting and filtering,
        /// this will simply raise a Reset event to <seealso cref="INotifyCollectionChanged.CollectionChanged"/> listeners.
        /// </remarks>
        protected override void RefreshOverride()
        {
            ++_version;

            // tell listeners everything has changed
            OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
        }

        #endregion Public Methods


        //------------------------------------------------------
        //
        //  Protected Methods
        //
        //------------------------------------------------------

        #region Protected Methods

        /// <summary>
        /// Implementation of IEnumerable.GetEnumerator().
        /// This provides a way to enumerate the members of the collection
        /// without changing the currency.
        /// </summary>
        protected override IEnumerator GetEnumerator()
        {
            return new FlatteningEnumerator(_collection, this);
        }

        /// <summary>
        /// Handle CollectionChange events from CompositeCollection ("ground level").
        /// i.e. Add/Remove of single items or CollectionContainers, or Refresh
        /// </summary>
        protected override void ProcessCollectionChanged(NotifyCollectionChangedEventArgs args)
        {
            ValidateCollectionChangedEventArgs(args);

            bool moveCurrencyOffDeletedElement = false;

            switch (args.Action)
            {
                case NotifyCollectionChangedAction.Add:
                case NotifyCollectionChangedAction.Remove:
                    {
                        // get the affected item (it can be a single first-level item or a CollectionContainer)
                        object item = null;
                        int startingIndex = -1;
                        if (args.Action == NotifyCollectionChangedAction.Add)
                        {
                            item = args.NewItems[0];
                            startingIndex = args.NewStartingIndex;
                        }
                        else
                        {
                            item = args.OldItems[0];
                            startingIndex = args.OldStartingIndex;
                        }
                        Debug.Assert(startingIndex >= 0, "Source composite collection failed to supply an index");
                        int index = startingIndex;

                        if (_traceLog != null)
                            _traceLog.Add("ProcessCollectionChanged  action = {0}  item = {1}",
                                        args.Action, TraceLog.IdFor(item));

                        CollectionContainer cc = item as CollectionContainer;
                        if (cc == null) // if a single item was added/removed
                        {
                            // translate the index into one that makes sense for the flat view
                            for (int k = index - 1; k >= 0; --k)
                            {
                                cc = _collection[k] as CollectionContainer;
                                if (cc != null)
                                {
                                    // count members of cc's view but not the cc itself
                                    index += cc.ViewCount - 1;
                                }
                            }
                            if (args.Action == NotifyCollectionChangedAction.Add)
                            {
                                if (_count >= 0)
                                    ++_count;
                                UpdateCurrencyAfterAdd(index, args.NewStartingIndex, true);
                            }
                            else if (args.Action == NotifyCollectionChangedAction.Remove)
                            {
                                if (_count >= 0)
                                    --_count;
                                UpdateCurrencyAfterRemove(index, args.OldStartingIndex, true);
                            }

                            args = new NotifyCollectionChangedEventArgs(args.Action, item, index);
                        }
                        else // else a whole collection container was added/removed
                        {
                            if (args.Action == NotifyCollectionChangedAction.Add)
                            {
                                if (_count >= 0)
                                    _count += cc.ViewCount;
                            }
                            else
                            {
                                // We only handle Add and Remove.  Make sure it's not some other action.
                                Debug.Assert(args.Action == NotifyCollectionChangedAction.Remove);
                                if (_count >= 0)
                                    _count -= cc.ViewCount;
                            }

                            if (startingIndex <= _currentPositionX)
                            {
                                if (args.Action == NotifyCollectionChangedAction.Add)
                                {
                                    ++_currentPositionX;
                                    SetCurrentPositionFromXY(_currentPositionX, _currentPositionY);
                                }
                                else
                                {
                                    // We only handle Add and Remove.  Make sure it's not some other action.
                                    Invariant.Assert(args.Action == NotifyCollectionChangedAction.Remove);
                                    if (startingIndex == _currentPositionX)
                                    {
                                        moveCurrencyOffDeletedElement = true;
                                    }
                                    else // (args.StartingIndex < _currentPositionX)
                                    {
                                        --_currentPositionX;
                                        SetCurrentPositionFromXY(_currentPositionX, _currentPositionY);
                                    }
                                }
                            }

                            // force refresh for all listeners
                            args = new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset);
                        }
                    }
                    break;

                case NotifyCollectionChangedAction.Replace:
                    {
                        CollectionContainer newCollectionContainer = args.NewItems[0] as CollectionContainer;
                        CollectionContainer oldCollectionContainer = args.OldItems[0] as CollectionContainer;
                        int startingIndex = args.OldStartingIndex;

                        if (newCollectionContainer == null && oldCollectionContainer == null) // if a single item was added/removed
                        {
                            // translate the index into one that makes sense for the flat view
                            for (int k = startingIndex - 1; k >= 0; --k)
                            {
                                CollectionContainer cc = _collection[k] as CollectionContainer;
                                if (cc != null)
                                {
                                    // count members of cc's view but not the cc itself
                                    startingIndex += cc.ViewCount - 1;
                                }
                            }

                            if (startingIndex == CurrentPosition)
                                moveCurrencyOffDeletedElement = true;

                            args = new NotifyCollectionChangedEventArgs(args.Action, args.NewItems, args.OldItems, startingIndex);
                        }
                        else // else a whole collection container was replaced
                        {
                            if (_count >= 0)
                            {
                                _count -= oldCollectionContainer == null ? 1 : oldCollectionContainer.ViewCount;
                                _count += newCollectionContainer == null ? 1 : newCollectionContainer.ViewCount;
                            }

                            if (startingIndex < _currentPositionX)
                            {
                                SetCurrentPositionFromXY(_currentPositionX, _currentPositionY);
                            }
                            else if (startingIndex == _currentPositionX)
                            {
                                moveCurrencyOffDeletedElement = true;
                            }

                            // force refresh for all listeners
                            args = new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset);
                        }
                    }
                    break;

                case NotifyCollectionChangedAction.Move:
                    {
                        CollectionContainer oldCollectionContainer = args.OldItems[0] as CollectionContainer;
                        int oldStartingIndex = args.OldStartingIndex;
                        int newStartingIndex = args.NewStartingIndex;

                        if (oldCollectionContainer == null) // if a single item was added/removed
                        {
                            // no change to count for a move operation.

                            // translate the index into one that makes sense for the flat view
                            for (int k = oldStartingIndex - 1; k >= 0; --k)
                            {
                                CollectionContainer cc = _collection[k] as CollectionContainer;
                                if (cc != null)
                                {
                                    // count members of cc's view but not the cc itself
                                    oldStartingIndex += cc.ViewCount - 1;
                                }
                            }

                            // translate the index into one that makes sense for the flat view
                            for (int k = newStartingIndex - 1; k >= 0; --k)
                            {
                                CollectionContainer cc = _collection[k] as CollectionContainer;
                                if (cc != null)
                                {
                                    // count members of cc's view but not the cc itself
                                    newStartingIndex += cc.ViewCount - 1;
                                }
                            }

                            // if the entire move happened before or after the CurrentPosition, then
                            // there needn't be a change to currency.
                            if (oldStartingIndex == CurrentPosition)
                            {
                                moveCurrencyOffDeletedElement = true;
                            }
                            else if (newStartingIndex <= CurrentPosition && oldStartingIndex > CurrentPosition)
                            {
                                UpdateCurrencyAfterAdd(newStartingIndex, args.NewStartingIndex, true);
                            }
                            else if (oldStartingIndex < CurrentPosition && newStartingIndex >= CurrentPosition)
                            {
                                UpdateCurrencyAfterRemove(oldStartingIndex, args.OldStartingIndex, true);
                            }

                            args = new NotifyCollectionChangedEventArgs(args.Action, args.OldItems, newStartingIndex, oldStartingIndex);
                        }
                        else // else a whole collection container was moved
                        {
                            // no change to count for a move operation.

                            // if the entire move happened before or after the CurrentPosition, then
                            // there needn't be a change to currency.
                            if (oldStartingIndex == _currentPositionX)
                            {
                                moveCurrencyOffDeletedElement = true;
                            }
                            else if (newStartingIndex <= _currentPositionX && oldStartingIndex > _currentPositionX)
                            {
                                ++_currentPositionX;
                                SetCurrentPositionFromXY(_currentPositionX, _currentPositionY);
                            }
                            else if (oldStartingIndex < _currentPositionX && newStartingIndex >= _currentPositionX)
                            {
                                --_currentPositionX;
                                SetCurrentPositionFromXY(_currentPositionX, _currentPositionY);
                            }

                            // force refresh for all listeners
                            args = new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset);
                        }
                    }
                    break;


                case NotifyCollectionChangedAction.Reset:
                    {
                        if (_traceLog != null)
                            _traceLog.Add("ProcessCollectionChanged  action = {0}", args.Action);

                        if (_collection.Count != 0)
                        {
                            //
                            //  This is to verify that a Reset event is raised IFF the CompositionCollection
                            //  was cleared.  To fully implement a Reset event otherwise can prove to be
                            //  quite complex.  For example, you must unhook the listeners to each of the
                            //  CollectionContainers that are no longer in the collection, hook up the new
                            //  ones, and figure out how to restore the currency to the correct item in
                            //  the correct sub-collection, or to BeforeFirst or AfterLast.
                            //

                            throw new InvalidOperationException(SR.Get(SRID.CompositeCollectionResetOnlyOnClear));
                        }

                        _count = 0; // OnCollectionChanged(arg) below will raise PropChange for Count
                        if (_currentPositionX >= 0) // if current item was in view
                        {
                            OnCurrentChanging();
                            SetCurrentBeforeFirst();
                            OnCurrentChanged();

                            OnPropertyChanged(IsCurrentBeforeFirstPropertyName);
                            OnPropertyChanged(CurrentPositionPropertyName);
                            OnPropertyChanged(CurrentItemPropertyName);
                        }
                    }
                    break;

                default:
                    throw new NotSupportedException(SR.Get(SRID.UnexpectedCollectionChangeAction, args.Action));
            }

            ++_version;
            OnCollectionChanged(args);

            if (moveCurrencyOffDeletedElement)
            {
                _currentPositionY = 0;
                MoveCurrencyOffDeletedElement();
            }

#if DEBUG
            VerifyCurrencyIsConsistent();
#endif
        }

        #endregion Protected Methods


        //------------------------------------------------------
        //
        //  Internal Methods
        //
        //------------------------------------------------------

        #region Internal Methods

        // collection changed event as received from the container (VIEW) of a sub-collection
        internal void OnContainedCollectionChanged(object sender, NotifyCollectionChangedEventArgs args)
        {
            ValidateCollectionChangedEventArgs(args);

            // Count must be invalidated instead of updated, because there might be
            // several change events raised from the same sub-collection change
            // (i.e. when the underlying collection is the basis of multiple collContainers.)
            _count = -1;

            int flatOldIndex = args.OldStartingIndex;
            int flatNewIndex = args.NewStartingIndex;

            // translate the index into one that makes sense for the flat view
            int x;
            int indexModifier = 0;
            for (x = 0; x < _collection.Count; ++x)
            {
                CollectionContainer cc = _collection[x] as CollectionContainer;
                if (cc != null)
                {
                    if (sender == cc)
                    {
                        break;
                    }

                    indexModifier += cc.ViewCount;
                }
                else // single item
                {
                    ++indexModifier;
                }
            }
            // if we didn't know the index to start with, we still don't
            if (args.OldStartingIndex >= 0)
                flatOldIndex += indexModifier;
            if (args.NewStartingIndex >= 0)
                flatNewIndex += indexModifier;

            if (x >= _collection.Count)
            {
                if (_traceLog != null)
                {
                    _traceLog.Add("Received ContainerCollectionChange from unknown sender {0}  action = {1} old item = {2}, new item = {3}",
                                    TraceLog.IdFor(sender), args.Action, TraceLog.IdFor(args.OldItems[0]), TraceLog.IdFor(args.NewItems[0]));
                    _traceLog.Add("Unhook CollectionChanged event handler from unknown sender.");
                }

                // Bonus: since we've spent the time looking through the whole list,
                // cache the count if we didn't have it!
                CacheCount(indexModifier);
                return;
            }

            switch (args.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    TraceContainerCollectionChange(sender, args.Action, null, args.NewItems[0]);

                    if (flatNewIndex < 0)
                    {
                        flatNewIndex = DeduceFlatIndexForAdd((CollectionContainer)sender, x);
                    }

                    UpdateCurrencyAfterAdd(flatNewIndex, x, false);
                    args = new NotifyCollectionChangedEventArgs(args.Action, args.NewItems[0], flatNewIndex);
                    break;

                case NotifyCollectionChangedAction.Remove:
                    TraceContainerCollectionChange(sender, args.Action, args.OldItems[0], null);

                    if (flatOldIndex < 0)
                    {
                        flatOldIndex = DeduceFlatIndexForRemove((CollectionContainer)sender, x, args.OldItems[0]);
                    }

                    UpdateCurrencyAfterRemove(flatOldIndex, x, false);
                    args = new NotifyCollectionChangedEventArgs(args.Action, args.OldItems[0], flatOldIndex);
                    break;

                case NotifyCollectionChangedAction.Replace:
                    TraceContainerCollectionChange(sender, args.Action, args.OldItems[0], args.NewItems[0]);

                    if (flatOldIndex == CurrentPosition)
                        MoveCurrencyOffDeletedElement();
                    args = new NotifyCollectionChangedEventArgs(args.Action, args.NewItems[0], args.OldItems[0], flatOldIndex);
                    break;

                case NotifyCollectionChangedAction.Move:
                    TraceContainerCollectionChange(sender, args.Action, args.OldItems[0], args.NewItems[0]);

                    if (flatOldIndex < 0)
                    {
                        flatOldIndex = DeduceFlatIndexForRemove((CollectionContainer)sender, x, args.NewItems[0]);
                    }

                    if (flatNewIndex < 0)
                    {
                        flatNewIndex = DeduceFlatIndexForAdd((CollectionContainer)sender, x);
                    }

                    UpdateCurrencyAfterMove(flatOldIndex, flatNewIndex, x, false);
                    args = new NotifyCollectionChangedEventArgs(args.Action, args.OldItems[0], flatNewIndex, flatOldIndex);
                    break;

                case NotifyCollectionChangedAction.Reset:
                    {
                        if (_traceLog != null)
                            _traceLog.Add("ContainerCollectionChange from {0}  action = {1}",
                                            TraceLog.IdFor(sender), args.Action);

                        UpdateCurrencyAfterRefresh(sender);
                    }
                    break;

                default:
                    throw new NotSupportedException(SR.Get(SRID.UnexpectedCollectionChangeAction, args.Action));
            }

            ++_version;
            OnCollectionChanged(args);
        }


        // determine whether the items have reliable hash codes
        internal override bool HasReliableHashCodes()
        {
            // sample an item from each contained collection (bug 1738297)
            for (int k = 0, n = _collection.Count; k < n; ++k)
            {
                CollectionContainer cc = _collection[k] as CollectionContainer;

                if (cc != null)
                {
                    CollectionView cv = cc.View as CollectionView;
                    if (cv != null && !cv.HasReliableHashCodes())
                    {
                        return false;
                    }
                }
                else
                {
                    if (!HashHelper.HasReliableHashCode(_collection[k]))
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        internal override void GetCollectionChangedSources(int level, Action<int, object, bool?, List<string>> format, List<string> sources)
        {
            format(level, this, false, sources);
            if (_collection != null)
            {
                _collection.GetCollectionChangedSources(level + 1, format, sources);
            }
        }

        #endregion


        //------------------------------------------------------
        //
        //  Private Properties
        //
        //------------------------------------------------------

        #region Private Properties

        private bool IsCurrentInView
        {
            get
            {
                return (0 <= _currentPositionX && _currentPositionX < _collection.Count);
            }
        }

        #endregion


        //------------------------------------------------------
        //
        //  Private Methods
        //
        //------------------------------------------------------

        #region Private Methods

        // if item does not exist in the collection, -1 is returned.
        // if changeCurrent, move current to result index (even for -1); this is cancelable.
        private int FindItem(object item, bool changeCurrent)
        {
            int positionX = 0;
            int positionY = 0;
            int index = 0;
            for (; positionX < _collection.Count; ++positionX)
            {
                CollectionContainer cc = _collection[positionX] as CollectionContainer;

                if (cc == null) // flat item
                {
                    if (ItemsControl.EqualsEx(_collection[positionX], item))
                    {
                        break;
                    }
                    ++index;
                }
                else    // CollContainer
                {
                    positionY = cc.ViewIndexOf(item);
                    if (positionY >= 0)
                    {
                        index += positionY;
                        break;
                    }
                    positionY = 0;
                    index += cc.ViewCount;  // flattened index
                }
            }
            if (positionX >= _collection.Count)
            {
                // Bonus: since we've spent the time looking through the whole list,
                // cache the count if we didn't have it!
                CacheCount(index);
                index = -1;

                // if caller wanted to changeCurrent, we'll move to BeforeFirst
                item = null;
                positionX = -1;
                positionY = 0;
            }
            if (changeCurrent)
            {
                if ((CurrentPosition != index) && OKToChangeCurrent())
                {
                    object oldCurrentItem = CurrentItem;
                    int oldCurrentPosition = CurrentPosition;
                    bool oldIsCurrentAfterLast = IsCurrentAfterLast;
                    bool oldIsCurrentBeforeFirst = IsCurrentBeforeFirst;

                    SetCurrent(item, index);
                    _currentPositionX = positionX;
                    _currentPositionY = positionY;
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
            }
            return index;
        }

        // if flatIndex is -1, null is returned
        // if flatIndex is greater than Count, s_afterLast is returned
        private object GetItem(int flatIndex, out int positionX, out int positionY)
        {
            positionY = 0;

            if (flatIndex == -1)
            {
                positionX = -1;
                return null;
            }

            if (_count >= 0 && flatIndex >= _count)
            {
                positionX = _collection.Count;
                return s_afterLast;
            }

            int searchIndex = 0;

            for (int i = 0; i < _collection.Count; ++i)
            {
                CollectionContainer cc = _collection[i] as CollectionContainer;

                if (cc == null)                 // flat item
                {
                    if (searchIndex == flatIndex)
                    {
                        positionX = i;
                        return _collection[i];
                    }

                    ++searchIndex;
                }
                else if (cc.Collection != null) // CollContainer
                {
                    // see if flatIndex falls within this collection:
                    int localIndex = flatIndex - searchIndex;
                    int count = cc.ViewCount;

                    if (localIndex < count)
                    {
                        positionX = i;
                        positionY = localIndex;
                        return cc.ViewItem(localIndex);
                    }
                    else
                    {
                        // try next
                        searchIndex += count;
                    }
                }
            }
            // Bonus: since we've spent the time looking through the whole list,
            // cache the count if we didn't have it!
            CacheCount(searchIndex);

            positionX = _collection.Count;
            return s_afterLast;
        }

        // Beginning at the specified (positionX, positionY),
        // look for an item to set as CurrentItem.
        // ALWAYS set _currentPositionX and _currentPositionY.
        private object GetNextItemFromXY(int positionX, int positionY)
        {
            Invariant.Assert(positionY >= 0);

            object item = null;
            for (; positionX < _collection.Count; ++positionX)
            {
                CollectionContainer cc = _collection[positionX] as CollectionContainer;
                if (cc == null)
                {
                    item = _collection[positionX];
                    positionY = 0;
                    break;
                }
                else if (positionY < cc.ViewCount)
                {
                    item = cc.ViewItem(positionY);
                    break;
                }
                else
                {
                    // after the initial positionX, forget the old Y value
                    positionY = 0;
                }
            }
            if (positionX < _collection.Count)
            {
                _currentPositionX = positionX;
                _currentPositionY = positionY;
            }
            else
            {
                _currentPositionX = _collection.Count;
                _currentPositionY = 0;
            }
            return item;
        }

        // Count items in CompositeCollection, including those in sub-collections, from 0 to end.
        // If end is _collection.Count, this returns count of all items in collection.
        private int CountDeep(int end)
        {
            if (Invariant.Strict)
                Invariant.Assert(end <= _collection.Count);

            int count = 0;
            for (int i = 0; i < end; ++i)
            {
                CollectionContainer cc = _collection[i] as CollectionContainer;

                if (cc == null) // flat item
                {
                    ++count;
                }
                else
                {
                    count += cc.ViewCount;
                }
            }
            return count;
        }

        private void CacheCount(int count)
        {
            // count cache may be wrong if underlying collection doesn't notify;
            // also, don't count initial cache as a change.
            bool countChanged = (_count != count && _count >= 0);

            _count = count;

            if (countChanged)
            {
                OnPropertyChanged(CountPropertyName);
            }
        }

        // Move to a given index.
        // This current-changing operation can be cancelled and it should not throw exceptions.
        // if the proposed index is after the end, current is set to AfterLast
        private bool _MoveTo(int proposed)
        {
            int newPositionX, newPositionY;
            object newCurrentItem = GetItem(proposed, out newPositionX, out newPositionY);

            if (proposed != CurrentPosition || newCurrentItem != CurrentItem)
            {
                // if we know the count, proposed should be in range.
                Invariant.Assert(_count < 0 || proposed <= _count);

                if (OKToChangeCurrent())
                {
                    object oldCurrentItem = CurrentItem;
                    int oldCurrentPosition = CurrentPosition;
                    bool oldIsCurrentAfterLast = IsCurrentAfterLast;
                    bool oldIsCurrentBeforeFirst = IsCurrentBeforeFirst;

                    _currentPositionX = newPositionX;
                    _currentPositionY = newPositionY;

                    if (newCurrentItem == s_afterLast)
                    {
                        SetCurrent(null, Count);   // Count has been cached from GetItem()
                    }
                    else
                    {
                        SetCurrent(newCurrentItem, proposed);
                    }
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
            }
            return IsCurrentInView;
        }

        private int DeduceFlatIndexForAdd(CollectionContainer sender, int x)
        {
            // sender didn't provide an index, but we need to know at least
            // whether the new item comes before or after CurrentPosition
            int flatIndex;

            if (_currentPositionX > x)
            {
                flatIndex = 0;
            }
            else if (_currentPositionX < x)
            {
                flatIndex = CurrentPosition + 1;
            }
            else
            {
                object item = ((CollectionContainer)sender).ViewItem(_currentPositionY);
                if (ItemsControl.EqualsEx(CurrentItem, item))
                {
                    flatIndex = CurrentPosition + 1;
                }
                else
                {
                    flatIndex = 0;
                }
            }

            return flatIndex;
        }

        private int DeduceFlatIndexForRemove(CollectionContainer sender, int x, object item)
        {
            // sender didn't provide an index, but we need to know at least
            // whether the removed item comes before or after CurrentPosition
            int flatIndex;

            if (_currentPositionX > x)
            {
                flatIndex = 0;
            }
            else if (_currentPositionX < x)
            {
                flatIndex = CurrentPosition + 1;
            }
            else
            {
                if (ItemsControl.EqualsEx(item, CurrentItem))
                {
                    flatIndex = CurrentPosition;
                }
                else
                {
                    object item2 = ((CollectionContainer)sender).ViewItem(_currentPositionY);
                    if (ItemsControl.EqualsEx(item, item2))
                    {
                        flatIndex = CurrentPosition + 1;
                    }
                    else
                    {
                        flatIndex = 0;
                    }
                }
            }

            return flatIndex;
        }

        // Update currency fields after a single item had been added.
        private void UpdateCurrencyAfterAdd(int flatIndex, int positionX, bool isCompositeItem)
        {
            if (flatIndex < 0)
                return;

            if (flatIndex <= CurrentPosition)
            {
                int newCurrentPosition = CurrentPosition + 1;
                if (isCompositeItem)           // if the add was a single item of CompositeCollection
                {
                    ++_currentPositionX;
                }
                else if (positionX == _currentPositionX) // else if it was in the current sub-collection
                {
                    ++_currentPositionY;
                }
                // else it was in a subcollection prior to the current collection,
                // in which case we don't need to adjust X-Y.

                // CurrentItem needs to be updated because we get notified of replace as Remove+Add
                // but that's not what really happened in the underlying collection
                SetCurrent(GetNextItemFromXY(_currentPositionX, _currentPositionY), newCurrentPosition);
            }

#if DEBUG
            VerifyCurrencyIsConsistent();
#endif
        }

        // Update currency fields after a single item had been removed.
        private void UpdateCurrencyAfterRemove(int flatIndex, int positionX, bool isCompositeItem)
        {
            if (flatIndex < 0)
                return;

            if (flatIndex < CurrentPosition)
            {
                SetCurrent(CurrentItem, CurrentPosition - 1);
                if (isCompositeItem)            // if the remove was a single item of CompositeCollection
                {
                    --_currentPositionX;
                }
                else if (positionX == _currentPositionX) // else if it was in the current sub-collection
                {
                    --_currentPositionY;
                }
                // else it was in a subcollection prior to the current collection,
                // in which case we don't need to adjust X-Y.
            }
            else if (flatIndex == CurrentPosition) // current item was removed
            {
                MoveCurrencyOffDeletedElement();
            }

#if DEBUG
            VerifyCurrencyIsConsistent();
#endif
        }

        // fix up CurrentPosition and CurrentItem after a collection change
        private void UpdateCurrencyAfterMove(int oldIndex, int newIndex, int positionX, bool isCompositeItem)
        {
            // if entire move was before or after current item, then there
            // is nothing that needs to be done.
            if ((oldIndex < CurrentPosition && newIndex < CurrentPosition)
                || (oldIndex > CurrentPosition && newIndex > CurrentPosition))
                return;

            if (newIndex <= CurrentPosition)
                UpdateCurrencyAfterAdd(newIndex, positionX, isCompositeItem);

            if (oldIndex <= CurrentPosition)
                UpdateCurrencyAfterRemove(oldIndex, positionX, isCompositeItem);
        }

        // Update currency fields after a sub-collection had been refreshed.
        private void UpdateCurrencyAfterRefresh(object refreshedObject)
        {
            Invariant.Assert(refreshedObject is CollectionContainer);

            object oldCurrentItem = CurrentItem;
            int oldCurrentPosition = CurrentPosition;
            bool oldIsCurrentAfterLast = IsCurrentAfterLast;
            bool oldIsCurrentBeforeFirst = IsCurrentBeforeFirst;

            if (IsCurrentInView && refreshedObject == _collection[_currentPositionX])
            {
                CollectionContainer cc = refreshedObject as CollectionContainer;
                if (cc.ViewCount == 0) // if the collection was emptied out
                {
                    // it's just as though the collection was deleted
                    _currentPositionY = 0;  // 0 is AfterLast for an empty collection
                    MoveCurrencyOffDeletedElement();
                }
                else // else try to restore currency to the old current item
                {
                    int positionY = cc.ViewIndexOf(CurrentItem);
                    if (positionY >= 0)
                    {
                        _currentPositionY = positionY;
                        SetCurrentPositionFromXY(_currentPositionX, _currentPositionY);
                    }
                    else // else we give up and move to BeforeFirst
                    {
                        OnCurrentChanging();
                        SetCurrentBeforeFirst();
                        OnCurrentChanged();
                    }
                }
            }
            else
            {
                // Recalculate CurrentPosition if the refreshed collection was positioned before current collection
                for (int i = 0; i < _currentPositionX; ++i)
                {
                    if (_collection[i] == refreshedObject)
                    {
                        SetCurrentPositionFromXY(_currentPositionX, _currentPositionY);
                        break;
                    }
                }
            }

            if (IsCurrentAfterLast != oldIsCurrentAfterLast)
                OnPropertyChanged(IsCurrentAfterLastPropertyName);

            if (IsCurrentBeforeFirst != oldIsCurrentBeforeFirst)
                OnPropertyChanged(IsCurrentBeforeFirstPropertyName);

            if (oldCurrentPosition != CurrentPosition)
                OnPropertyChanged(CurrentPositionPropertyName);

            if (oldCurrentItem != CurrentItem)
                OnPropertyChanged(CurrentItemPropertyName);

#if DEBUG
            VerifyCurrencyIsConsistent();
#endif
        }

        private void MoveCurrencyOffDeletedElement()
        {
            int oldCurrentPosition = CurrentPosition;

            // We fire current changing, ignoring cancelation - there's no choice.
            OnCurrentChanging();

            // find the next item to be current
            object newCurrentItem = GetNextItemFromXY(_currentPositionX, _currentPositionY);
            // if next item could not be found, go to the last item instead
            if (_currentPositionX >= _collection.Count)
            {
                newCurrentItem = GetLastItem(out _currentPositionX, out _currentPositionY);
                SetCurrent(newCurrentItem, Count - 1);
            }
            else
            {
                // recalculate position because the removed element could have been a collection
                SetCurrentPositionFromXY(_currentPositionX, _currentPositionY);
                SetCurrent(newCurrentItem, CurrentPosition);
            }
            OnCurrentChanged();

            OnPropertyChanged(CountPropertyName);
            OnPropertyChanged(CurrentItemPropertyName);

            if (IsCurrentAfterLast)
                OnPropertyChanged(IsCurrentAfterLastPropertyName);

            if (IsCurrentBeforeFirst)
                OnPropertyChanged(IsCurrentBeforeFirstPropertyName);

            if (CurrentPosition != oldCurrentPosition)
                OnPropertyChanged(CurrentPositionPropertyName);
        }

        // note: if collection is empty, item and position returned is BeforeFirst
        private object GetLastItem(out int positionX, out int positionY)
        {
            object lastItem = null;
            positionX = -1;
            positionY = 0;

            if (_count != 0)    // unknown or HasItems
            {
                // seek backwards
                positionX = _collection.Count - 1;
                for (; positionX >= 0; --positionX)
                {
                    CollectionContainer cc = _collection[positionX] as CollectionContainer;
                    if (cc == null)
                    {
                        lastItem = _collection[positionX];
                        break;
                    }
                    else if (cc.ViewCount > 0)
                    {
                        positionY = cc.ViewCount - 1;
                        lastItem = cc.ViewItem(positionY);
                        break;
                    }
                }
                if (positionX < 0)  // no items? remember zero count
                {
                    CacheCount(0);
                }
            }
            return lastItem;
        }

        private void SetCurrentBeforeFirst()
        {
            _currentPositionX = -1;
            _currentPositionY = 0;
            SetCurrent(null, -1);
        }

        private void SetCurrentPositionFromXY(int x, int y)
        {
            if (IsCurrentBeforeFirst)
                SetCurrent(null, -1);
            else if (IsCurrentAfterLast)
                SetCurrent(null, Count);
            else
                SetCurrent(CurrentItem, CountDeep(x) + y);
        }


        // this method is here just to avoid the compiler error
        // error CS0649: Warning as Error: Field '..._traceLog' is never assigned to, and will always have its default value null
        void InitializeTraceLog()
        {
            _traceLog = new TraceLog(20);
        }

        private void TraceContainerCollectionChange(object sender, NotifyCollectionChangedAction action, object oldItem, object newItem)
        {
            if (_traceLog != null)
                _traceLog.Add("ContainerCollectionChange from {0}  action = {1} oldItem = {2} newItem = {3}",
                                TraceLog.IdFor(sender), action, TraceLog.IdFor(oldItem), TraceLog.IdFor(newItem));
        }

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


        #endregion Private Methods


        //------------------------------------------------------
        //
        //  Private Types
        //
        //------------------------------------------------------

        #region Private Types

        /// <summary>
        /// IEnumerator implementation that does flattened forward-only enumeration over CompositeCollectionView.
        /// </summary>
        private class FlatteningEnumerator : IEnumerator, IDisposable
        {
            internal FlatteningEnumerator(CompositeCollection collection, CompositeCollectionView view)
            {
                Invariant.Assert(collection != null && view != null);
                _collection = collection;
                _view = view;
                _version = view._version;
                Reset();
            }

            public bool MoveNext()
            {
                CheckVersion();
                bool isCurrentInView = true;

                while (true)
                {
                    // advance within a collection container
                    if (_containerEnumerator != null)
                    {
                        if (_containerEnumerator.MoveNext())
                        {
                            _current = _containerEnumerator.Current;
                            break;
                        }
                        // when we reach the end of a container, prepare to move on
                        DisposeContainerEnumerator();
                    }

                    // move to the next item
                    if (++_index < _collection.Count)
                    {
                        object item = _collection[_index];
                        CollectionContainer cc = item as CollectionContainer;

                        // item is a container,  move into it
                        if (cc != null)
                        {
                            IEnumerable ie = cc.View;   // View is null when Collection is null
                            _containerEnumerator = (ie != null) ? ie.GetEnumerator() : null;
                            continue;
                        }

                        // plain item
                        _current = item;
                        break;
                    }
                    else
                    {
                        // no more items
                        _current = null;
                        _done = true;
                        isCurrentInView = false;
                        break;
                    }
                }
                return isCurrentInView;
            }

            public object Current
            {
                get
                {
                    // the spec for ICollectionView.CurrentItem says:
                    // InvalidOperationException: The enumerator is positioned before the first element of the collection or after the last element.
                    if (_index < 0)
                    {
#pragma warning suppress 6503 // ICollectionView.CurrentItem is documented to throw this exception
                        throw new InvalidOperationException(SR.Get(SRID.EnumeratorNotStarted));
                    }
                    if (_done)
                    {
#pragma warning suppress 6503 // ICollectionView.CurrentItem is documented to throw this exception
                        throw new InvalidOperationException(SR.Get(SRID.EnumeratorReachedEnd));
                    }

                    return _current;
                }
            }

            public void Reset()
            {
                CheckVersion();
                _index = -1;
                _current = null;
                DisposeContainerEnumerator();
                _done = false;
            }

            public void Dispose()
            {
                DisposeContainerEnumerator();
            }

            private void DisposeContainerEnumerator()
            {
                IDisposable d = _containerEnumerator as IDisposable;
                if (d != null)
                {
                    d.Dispose();
                }

                _containerEnumerator = null;
            }

            private void CheckVersion()
            {
                // note: there's a very unlikely possibility that the
                // version number wraps around back to the same number
                if (_isInvalidated || (_isInvalidated = (_version != _view._version)))
                    throw new InvalidOperationException(SR.Get(SRID.EnumeratorVersionChanged));
            }

            private CompositeCollection _collection;
            private CompositeCollectionView _view;
            private int _index;
            private object _current;
            private IEnumerator _containerEnumerator;
            private bool _done;
            private bool _isInvalidated = false;
            private int _version;
        }

        #endregion Private Types


        //------------------------------------------------------
        //
        //  Private Fields
        //
        //------------------------------------------------------

        #region Private Fields

        TraceLog _traceLog;
        CompositeCollection _collection;

        int _count = -1;
        int _version = 0;

        // Using X-Y coordinates to track current position in the composite collection:
        // X is the index in the first-level collection, whose members are items and subcollections
        // Y is the index into the subcollection, if any.  0, if not.
        int _currentPositionX = -1;
        int _currentPositionY = 0;

        private static readonly object s_afterLast = new Object();

        #endregion Private Fields


        //------------------------------------------------------
        //
        //  Debugging Aids
        //
        //------------------------------------------------------

        #region Debugging Aids

#if DEBUG

        // Verify that Currency is consistent.
        // However, we have to make an exception for cases when a collection is
        // being used multiple times inside a CompositeCollection.
        private void VerifyCurrencyIsConsistent()
        {
            if (IsCurrentInView)
            {
                int x, y;
                if (!ItemsControl.EqualsEx(CurrentItem, GetItem(CurrentPosition, out x, out y)) && !_collection.HasRepeatedCollection())
                    Debug.Assert(false, "CurrentItem is not consistent with CurrentPosition");
            }
            else
            {
                if ((CurrentItem != null) && !_collection.HasRepeatedCollection())
                    Debug.Assert(false, "CurrentItem is not consistent with CurrentPosition");
            }
        }

#endif

        #endregion Debugging Aids
    }
}
