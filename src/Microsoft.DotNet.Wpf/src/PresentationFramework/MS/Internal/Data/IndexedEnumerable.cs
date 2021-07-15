// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// Description: offers optimistic indexer, i.e. this[int index] { get; }
//      for a collection implementing IEnumerable, assuming that after an initial request
//      to read item[N], the following indices will be a sequence for index N+1, N+2 etc.
//

using System;
using System.Collections;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Reflection;

using System.Diagnostics;
using System.Windows;
using System.Windows.Data;
using MS.Utility;

namespace MS.Internal.Data
{
    /// <summary>
    /// for a collection implementing IEnumerable this offers
    /// optimistic indexer, i.e. this[int index] { get; }
    /// and cached Count/IsEmpty properties and IndexOf method,
    /// assuming that after an initial request to read item[N],
    /// the following indices will be a sequence for index N+1, N+2 etc.
    /// </summary>
    /// <remarks>
    /// This class is NOT safe for multi-threaded use.
    /// if the source collection implements IList or ICollection, the corresponding
    /// properties/methods will be used instead of the cached versions
    /// </remarks>
    internal class IndexedEnumerable : IEnumerable, IWeakEventListener
    {
        //------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------

        /// <summary>
        /// Initialize indexer with IEnumerable.
        /// </summary>
        internal IndexedEnumerable(IEnumerable collection)
            : this(collection, null)
        {
        }

        /// <summary>
        /// Initialize indexer with IEnumerable and collectionView for purpose of filtering.
        /// </summary>
        internal IndexedEnumerable(IEnumerable collection, Predicate<object> filterCallback)
        {
            _filterCallback = filterCallback;
            SetCollection(collection);

            // observe source collection for changes to invalidate enumerators
            // for IList we can get all information directly from the source collection,
            // no need to track changes, no need to hook notification
            if (List == null)
            {
                INotifyCollectionChanged icc = collection as INotifyCollectionChanged;
                if (icc != null)
                {
                    CollectionChangedEventManager.AddHandler(icc, OnCollectionChanged);
                }
            }
        }


        //------------------------------------------------------
        //
        //  Internal Properties/Methods
        //
        //------------------------------------------------------

        /// <summary> Determines the index of a specific value in the collection. </summary>
        ///<remarks>if a FilterCallback is set, it will be reflected in the returned index</remarks>
        internal int IndexOf(object item)
        {
            // try if source collection has a IndexOf method
            int index;
            if (GetNativeIndexOf(item, out index))
            {
                return index;
            }

            // if the enumerator is still valid, then we can
            // just use the cached item.
            if (EnsureCacheCurrent())
            {
                if (item == _cachedItem)
                    return _cachedIndex;
            }

            // If item != cached item, that doesn’t mean that the enumerator
            // is `blown, it just means we have to go find the item represented by item.
            index = -1;
            // only ask for fresh enumerator if current enumerator already was moved before
            if (_cachedIndex >= 0)
            {
                // force a new enumerator
                UseNewEnumerator();
            }
            int i = 0;
            while (_enumerator.MoveNext())
            {
                if (object.Equals(_enumerator.Current, item))
                {
                    index = i;
                    break;
                }
                ++i;
            }

            // update cache if item was found
            if (index >= 0)
            {
                CacheCurrentItem(index, _enumerator.Current);
            }
            else
            {
                // item not found and moved enumerator to end -> release it
                ClearAllCaches();
                DisposeEnumerator(ref _enumerator);
            }

            return index;
        }


        ///<summary> Gets the number of elements in the collection. </summary>
        ///<remarks>if a FilterCallback is set, it will be reflected in the returned Count</remarks>
        internal int Count
        {
            get
            {
                EnsureCacheCurrent();

                // try if source collection has a Count property
                int count = 0;
                if (GetNativeCount(out count))
                {
                    return count;
                }

                // use a previously calculated Count
                if (_cachedCount >= 0)
                {
                    return _cachedCount;
                }

                // calculate and cache current Count value, using (filtered) enumerator
                count = 0;
                foreach (object unused in this)
                {
                    ++count;
                }

                _cachedCount = count;
                _cachedIsEmpty = (_cachedCount == 0);
                return count;
            }
        }

        // cheaper than (Count == 0)
        internal bool IsEmpty
        {
            get
            {
                // try if source collection has a IsEmpty property
                bool isEmpty;
                if (GetNativeIsEmpty(out isEmpty))
                {
                    return isEmpty;
                }

                if (_cachedIsEmpty.HasValue)
                {
                    return _cachedIsEmpty.Value;
                }

                // determine using (filtered) enumerator
                IEnumerator ie = GetEnumerator();
                _cachedIsEmpty = !ie.MoveNext();

                IDisposable d = ie as IDisposable;
                if (d != null)
                {
                    d.Dispose();
                }

                if (_cachedIsEmpty.Value)
                    _cachedCount = 0;
                return _cachedIsEmpty.Value;
            }
        }

        /// <summary>
        ///     Indexer property to retrieve the item at the given
        /// zero-based offset into the collection.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Thrown if index is out of range
        /// </exception>
        internal object this[int index]
        {
            get
            {
#pragma warning disable 1634 // about to use PreSharp message numbers - unknown to C#
                // try if source collection has a indexer
                object value;
                if (GetNativeItemAt(index, out value))
                {
                    return value;
                }

                if (index < 0)
                {
#pragma warning suppress 6503   // "Property get methods should not throw exceptions."
                    throw new ArgumentOutOfRangeException("index"); // validating the index argument
                }

                int moveBy = (index - _cachedIndex);
                if (moveBy < 0)
                {
                    // new index is before current position, need to reset enumerators
                    UseNewEnumerator(); // recreate a new enumerator, must not call .Reset anymore
                    moveBy = index + 1; // force at least one MoveNext
                }

                // if the enumerator is still valid, then we can
                // just use the cached value.
                if (EnsureCacheCurrent())
                {
                    if (index == _cachedIndex)
                        return _cachedItem;
                }
                else
                {
                    // there are new enumerators, caches were cleared, recalculate moveBy
                    moveBy = index + 1; // force at least one MoveNext
                }

                // position enumerator at new index:
                while ((moveBy > 0) && _enumerator.MoveNext())
                {
                    moveBy--;
                }

                // moved beyond the end of the enumerator?
                if (moveBy != 0)
                {
#pragma warning suppress 6503   // "Property get methods should not throw exceptions."
                    throw new ArgumentOutOfRangeException("index"); // validating the index argument
                }

                CacheCurrentItem(index, _enumerator.Current);
                return _cachedItem;
#pragma warning restore 1634
            }
        }

        /// <summary>
        /// The enumerable collection wrapped by this indexer.
        /// </summary>
        internal IEnumerable Enumerable
        {
            get { return _enumerable; }
        }

        /// <summary>
        /// The collection wrapped by this indexer.
        /// </summary>
        internal ICollection Collection
        {
            get { return _collection; }
        }

        /// <summary>
        /// The list wrapped by this indexer.
        /// </summary>
        internal IList List
        {
            get { return _list; }
        }

        /// <summary>
        /// The CollectionView wrapped by this indexer.
        /// </summary>
        internal CollectionView CollectionView
        {
            get { return _collectionView; }
        }


        /// <summary> Return an enumerator for the collection. </summary>
        public IEnumerator GetEnumerator()
        {
            return new FilteredEnumerator(this, Enumerable, FilterCallback);
        }

        ///<summary>
        /// Copies all the elements of the current collection to the specified one-dimensional Array.
        ///</summary>
        internal static void CopyTo(IEnumerable collection, Array array, int index)
        {
            Invariant.Assert(collection != null, "collection is null");
            Invariant.Assert(array != null, "target array is null");
            Invariant.Assert(array.Rank == 1, "expected array of rank=1");
            Invariant.Assert(index >= 0, "index must be positive");

            ICollection ic = collection as ICollection;
            if (ic != null)
            {
                ic.CopyTo(array, index);
            }
            else
            {
                IList list = (IList)array;

                foreach (object item in collection)
                {
                    if (index < array.Length)
                    {
                        list[index] = item;
                        ++index;
                    }
                    else
                    {
                        // The number of elements in the source ICollection is greater than
                        // the available space from index to the end of the destination array.
                        throw new ArgumentException(SR.Get(SRID.CopyToNotEnoughSpace), "index");
                    }
                }
            }
        }


        internal void Invalidate()
        {
            ClearAllCaches();

            // only track changes if source collection isn't already of type IList
            if (List == null)
            {
                INotifyCollectionChanged icc = Enumerable as INotifyCollectionChanged;
                if (icc != null)
                {
                    CollectionChangedEventManager.RemoveHandler(icc, OnCollectionChanged);
                }
            }

            _enumerable = null;
            DisposeEnumerator(ref _enumerator);
            DisposeEnumerator(ref _changeTracker);
            _collection = null;
            _list = null;
            _filterCallback = null;
        }


        //------------------------------------------------------
        //
        //  Private Members
        //
        //------------------------------------------------------

        private Predicate<object> FilterCallback
        {
            get
            {
                return _filterCallback;
            }
        }

        private void CacheCurrentItem(int index, object item)
        {
            _cachedIndex = index;
            _cachedItem = item;
            _cachedVersion = _enumeratorVersion;
        }

        // checks and returns if cached values are still current
        private bool EnsureCacheCurrent()
        {
            int version = EnsureEnumerator();

            if (version != _cachedVersion)
            {
                ClearAllCaches();
                _cachedVersion = version;
            }
            bool isCacheCurrent = (version == _cachedVersion) && (_cachedIndex >= 0);

#if DEBUG
            if (isCacheCurrent)
            {
                object current = null;
                try
                {
                    current = _enumerator.Current;
                }
                catch (InvalidOperationException)
                {
                    Debug.Assert(false, "EnsureCacheCurrent: _enumerator.Current failed with InvalidOperationException");
                }
                Debug.Assert(System.Windows.Controls.ItemsControl.EqualsEx(_cachedItem, current), "EnsureCacheCurrent: _cachedItem out of sync with _enumerator.Current");
            }
#endif // DEBUG
            return isCacheCurrent;
        }


        // returns the current EnumeratorVersion to indicate
        // whether any cached values may be valid.
        private int EnsureEnumerator()
        {
            if (_enumerator == null)
            {
                UseNewEnumerator();
            }
            else
            {
                try
                {
                    _changeTracker.MoveNext();
                }
                catch (InvalidOperationException)
                {
                    // collection was changed - start over with a new enumerator
                    UseNewEnumerator();
                }
            }

            return _enumeratorVersion;
        }

        private void UseNewEnumerator()
        {
            // if _enumeratorVersion exceeds MaxValue, then it
            // will roll back to MinValue, and continue on from there.
            unchecked { ++_enumeratorVersion; }

            DisposeEnumerator(ref _changeTracker);
            _changeTracker = _enumerable.GetEnumerator();
            DisposeEnumerator(ref _enumerator);
            _enumerator = GetEnumerator();
            _cachedIndex = -1;    // will force at least one MoveNext
            _cachedItem = null;
        }

        private void InvalidateEnumerator()
        {
            // if _enumeratorVersion exceeds MaxValue, then it
            // will roll back to MinValue, and continue on from there.
            unchecked { ++_enumeratorVersion; }

            DisposeEnumerator(ref _enumerator);
            ClearAllCaches();
        }

        private void DisposeEnumerator(ref IEnumerator ie)
        {
            IDisposable d = ie as IDisposable;
            if (d != null)
            {
                d.Dispose();
            }

            ie = null;
        }

        private void ClearAllCaches()
        {
            _cachedItem = null;
            _cachedIndex = -1;
            _cachedCount = -1;
        }

        private void SetCollection(IEnumerable collection)
        {
            Invariant.Assert(collection != null);
            _enumerable = collection;
            _collection = collection as ICollection;
            _list = collection as IList;
            _collectionView = collection as CollectionView;

            // try finding Count, IndexOf and indexer members via reflection
            if ((List == null) && (CollectionView == null))
            {
                Type srcType = collection.GetType();
                // try reflection for IndexOf(object)
                MethodInfo mi = srcType.GetMethod("IndexOf", new Type[] { typeof(object) });
                if ((mi != null) && (mi.ReturnType == typeof(int)))
                {
                    _reflectedIndexOf = mi;
                }

                // find matching indexer
                MemberInfo[] defaultMembers = srcType.GetDefaultMembers();
                for (int i = 0; i <= defaultMembers.Length - 1; i++)
                {
                    PropertyInfo pi = defaultMembers[i] as PropertyInfo;
                    if (pi != null)
                    {
                        ParameterInfo[] indexerParameters = pi.GetIndexParameters();
                        if (indexerParameters.Length == 1)
                        {
                            if (indexerParameters[0].ParameterType.IsAssignableFrom(typeof(int)))
                            {
                                _reflectedItemAt = pi;
                                break;
                            }
                        }
                    }
                }

                if (Collection == null)
                {
                    // try reflection for Count property
                    PropertyInfo pi = srcType.GetProperty("Count", typeof(int));
                    if (pi != null)
                    {
                        _reflectedCount = pi;
                    }
                }
            }
        }

        // to avoid slower calculation walking a IEnumerable,
        // try retreiving the requested value from source collection
        // if it implements ICollection, IList or CollectionView
        private bool GetNativeCount(out int value)
        {
            bool isNativeValue = false;
            value = -1;
            if (Collection != null)
            {
                value = Collection.Count;
                isNativeValue = true;
            }
            else if (CollectionView != null)
            {
                value = CollectionView.Count;
                isNativeValue = true;
            }
            else if (_reflectedCount != null)
            {
                try
                {
                    value = (int)_reflectedCount.GetValue(Enumerable, null);
                    isNativeValue = true;
                }
                catch (MethodAccessException)
                {
                    // revert to walking the IEnumerable
                    // under partial trust, some properties are not accessible even though they are public
                    // see bug 1415832
                    _reflectedCount = null;
                    isNativeValue = false;
                }
            }
            return isNativeValue;
        }

        private bool GetNativeIsEmpty(out bool isEmpty)
        {
            bool isNativeValue = false;
            isEmpty = true;
            if (Collection != null)
            {
                isEmpty = (Collection.Count == 0);
                isNativeValue = true;
            }
            else if (CollectionView != null)
            {
                isEmpty = CollectionView.IsEmpty;
                isNativeValue = true;
            }
            else if (_reflectedCount != null)
            {
                try
                {
                    isEmpty = ((int)_reflectedCount.GetValue(Enumerable, null) == 0);
                    isNativeValue = true;
                }
                catch (MethodAccessException)
                {
                    // revert to walking the IEnumerable
                    // under partial trust, some properties are not accessible even though they are public
                    // see bug 1415832
                    _reflectedCount = null;
                    isNativeValue = false;
                }
            }
            return isNativeValue;
        }

        private bool GetNativeIndexOf(object item, out int value)
        {
            bool isNativeValue = false;
            value = -1;
            if ((List != null) && (FilterCallback == null))
            {
                value = List.IndexOf(item);
                isNativeValue = true;
            }
            else if (CollectionView != null)
            {
                value = CollectionView.IndexOf(item);
                isNativeValue = true;
            }
            else if (_reflectedIndexOf != null)
            {
                try
                {
                    value = (int)_reflectedIndexOf.Invoke(Enumerable, new object[] { item });
                    isNativeValue = true;
                }
                catch (MethodAccessException)
                {
                    // revert to walking the IEnumerable
                    // under partial trust, some properties are not accessible even though they are public
                    // see bug 1415832
                    _reflectedIndexOf = null;
                    isNativeValue = false;
                }
            }
            return isNativeValue;
        }

        private bool GetNativeItemAt(int index, out object value)
        {
            bool isNativeValue = false;
            value = null;
            if (List != null)
            {
                value = List[index];
                isNativeValue = true;
            }
            else if (CollectionView != null)
            {
                value = CollectionView.GetItemAt(index);
                isNativeValue = true;
            }
            else if (_reflectedItemAt != null)
            {
                try
                {
                    value = _reflectedItemAt.GetValue(Enumerable, new object[] { index });
                    isNativeValue = true;
                }
                catch (MethodAccessException)
                {
                    // revert to walking the IEnumerable
                    // under partial trust, some properties are not accessible even though they are public
                    // see bug 1415832
                    _reflectedItemAt = null;
                    isNativeValue = false;
                }
            }
            return isNativeValue;
        }

        //------------------------------------------------------
        //
        //  Event handlers
        //
        //------------------------------------------------------

        /// <summary>
        /// Handle events from the centralized event table
        /// </summary>
        bool IWeakEventListener.ReceiveWeakEvent(Type managerType, object sender, EventArgs e)
        {
            return ReceiveWeakEvent(managerType, sender, e);
        }

        /// <summary>
        /// Handle events from the centralized event table
        /// </summary>
        protected virtual bool ReceiveWeakEvent(Type managerType, object sender, EventArgs e)
        {
            return false;   // this method is no longer used (but must remain, for compat)
        }

        void OnCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            InvalidateEnumerator();
        }

        //------------------------------------------------------
        //
        //  Private Members
        //
        //------------------------------------------------------

        private IEnumerable _enumerable;
        private IEnumerator _enumerator;
        private IEnumerator _changeTracker;
        private ICollection _collection;
        private IList _list;
        private CollectionView _collectionView;

        private int _enumeratorVersion;
        private object _cachedItem;
        private int _cachedIndex = -1;
        private int _cachedVersion = -1;
        private int _cachedCount = -1;
        private bool? _cachedIsEmpty;

        private PropertyInfo _reflectedCount;
        private PropertyInfo _reflectedItemAt;
        private MethodInfo _reflectedIndexOf;

        private Predicate<object> _filterCallback;


        //------------------------------------------------------
        //
        //  Private Types
        //
        //------------------------------------------------------
        private class FilteredEnumerator : IEnumerator, IDisposable
        {
            public FilteredEnumerator(IndexedEnumerable indexedEnumerable, IEnumerable enumerable, Predicate<object> filterCallback)
            {
                _enumerable = enumerable;
                _enumerator = _enumerable.GetEnumerator();
                _filterCallback = filterCallback;
                _indexedEnumerable = indexedEnumerable;
            }

            void IEnumerator.Reset()
            {
                if (_indexedEnumerable._enumerable == null)
                    throw new InvalidOperationException(SR.Get(SRID.EnumeratorVersionChanged));

                Dispose();
                _enumerator = _enumerable.GetEnumerator();
            }

            bool IEnumerator.MoveNext()
            {
                bool returnValue;

                if (_indexedEnumerable._enumerable == null)
                    throw new InvalidOperationException(SR.Get(SRID.EnumeratorVersionChanged));

                if (_filterCallback == null)
                {
                    returnValue = _enumerator.MoveNext();
                }
                else
                {
                    while ((returnValue = _enumerator.MoveNext()) && !_filterCallback(_enumerator.Current));
                }

                return returnValue;
            }

            object IEnumerator.Current
            {
                get
                {
                    return _enumerator.Current;
                }
            }

            public void Dispose()
            {
                IDisposable d = _enumerator as IDisposable;
                if (d != null)
                {
                    d.Dispose();
                }
                _enumerator = null;
            }

            IEnumerable _enumerable;
            IEnumerator _enumerator;
            IndexedEnumerable _indexedEnumerable;
            Predicate<object> _filterCallback;
        }
    }
}

