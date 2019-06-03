// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows.Data;

namespace System.Windows.Controls
{
    /// <summary>
    ///     A collection that simulates holding multiple copies of the same item.  Used as the ItemsSource for the DataGridCellsPresenter.
    ///     For our purposes this mirrors the DataGrid.Columns collection in that it has the same number of items and changes whenever 
    ///     the columns collection changes (though the items in it are obviously different; each item is the data object for a given row).  
    /// </summary>
    internal class MultipleCopiesCollection :
        IList,
        ICollection,
        IEnumerable,
        INotifyCollectionChanged,
        INotifyPropertyChanged
    {
        #region Construction

        internal MultipleCopiesCollection(object item, int count)
        {
            Debug.Assert(item != null, "item should not be null.");
            Debug.Assert(count >= 0, "count should not be negative.");

            CopiedItem = item;
            _count = count;
        }

        #endregion

        #region Item Management

        /// <summary>
        ///     Takes a collection change notifcation and causes the MultipleCopies collection to sync to the changes.  For example,
        ///     if an item was removed at a given index, the MultipleCopiesCollection also removes an item at the same index and fires
        ///     its own collection changed event.
        /// </summary>
        internal void MirrorCollectionChange(NotifyCollectionChangedEventArgs e)
        {            
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    Debug.Assert(
                        e.NewItems.Count == 1, 
                        "We're mirroring the Columns collection which is an ObservableCollection and only supports adding one item at a time");
                    Insert(e.NewStartingIndex);
                    break;

                case NotifyCollectionChangedAction.Move:
                    Debug.Assert(
                        e.NewItems.Count == 1,
                        "We're mirroring the Columns collection which is an ObservableCollection and only supports moving one item at a time");
                    Move(e.OldStartingIndex, e.NewStartingIndex);
                    break;

                case NotifyCollectionChangedAction.Remove:
                    Debug.Assert(
                        e.OldItems.Count == 1,
                        "We're mirroring the Columns collection which is an ObservableCollection and only supports removing one item at a time");
                    RemoveAt(e.OldStartingIndex);
                    break;

                case NotifyCollectionChangedAction.Replace:
                    Debug.Assert(
                        e.NewItems.Count == 1,
                        "We're mirroring the Columns collection which is an ObservableCollection and only supports replacing one item at a time");
                    OnReplace(CopiedItem, CopiedItem, e.NewStartingIndex);
                    break;

                case NotifyCollectionChangedAction.Reset:
                    Reset();
                    break;
            }            
        }

        /// <summary>
        ///     Syncs up the count with the given one.  This is used when we know we've missed a CollectionChanged event (say this 
        ///     MultipleCopiesCollection is inside a DataGridRow that was virtualized and recycled).  It attempts to resync
        ///     by adjusting the count and firing the proper property change notifications.
        /// </summary>
        /// <remarks>
        ///     This method works in concert with the DataGridCellsPresenter.  We don't know where items were removed / added, so containers
        ///     (DataGridCells) based off this collection could be stale (wrong column).  The cells presenter updates them.  We could have also 
        ///     just fired a Reset event here and not bothered with work in the cells presenter, but that would cause all cells to be regenerated. 
        ///     
        ///     Note that this method is designed to sync up to ALL collection changes that may have happened.  
        ///     The job of this method is made significantly easier by the fact that the MultipleCopiesCollection really only cares about
        ///     the count of items in the given collection (since we keep it in sync with the DataGrid Columns collection but host
        ///     a DataGridRow as the item).  This means we don't care about Move, Replace, etc.
        /// </remarks>
        internal void SyncToCount(int newCount)
        {
            int oldCount = RepeatCount;

            if (newCount != oldCount)
            {
                if (newCount > oldCount)
                {
                    // Insert at end
                    InsertRange(oldCount, newCount - oldCount);
                }
                else
                {
                    // Remove from the end
                    int numToRemove = oldCount - newCount;
                    RemoveRange(oldCount - numToRemove, numToRemove);
                }

                Debug.Assert(RepeatCount == newCount, "We should have properly updated the RepeatCount");
            }
        }

        /// <summary>
        ///     This is the item that is returned multiple times.
        /// </summary>
        internal object CopiedItem
        {
            get 
            { 
                return _item; 
            }

            set
            {
                if (value == CollectionView.NewItemPlaceholder)
                {
                    // If we populate the collection with the CollectionView's
                    // NewItemPlaceholder, it will confuse the CollectionView.
                    value = DataGrid.NewItemPlaceholder;
                }

                if (_item != value)
                {
                    object oldValue = _item;
                    _item = value;

                    OnPropertyChanged(IndexerName);

                    // Report replacing each item with the new item
                    for (int i = 0; i < _count; i++)
                    {
                        OnReplace(oldValue, _item, i);
                    }
                }
            }
        }

        /// <summary>
        ///     This is the number of times the item is to be repeated.
        /// </summary>
        private int RepeatCount
        {
            get 
            { 
                return _count; 
            }

            set
            {
                if (_count != value)
                {
                    _count = value;
                    OnPropertyChanged(CountName);
                    OnPropertyChanged(IndexerName);
                }
            }
        }
       
        private void Insert(int index)
        {
            RepeatCount++;
            OnCollectionChanged(NotifyCollectionChangedAction.Add, CopiedItem, index);
        }

        private void InsertRange(int index, int count)
        {
            // True range operations are not supported by CollectionView so we instead fire many changed events.
            for (int i = 0; i < count; i++)
            {
                Insert(index);
                index++;
            }
        }

        private void Move(int oldIndex, int newIndex)
        {
            OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Move, CopiedItem, newIndex, oldIndex));
        }

        private void RemoveAt(int index)
        {
            Debug.Assert((index >= 0) && (index < RepeatCount), "Index out of range");
      
            RepeatCount--;
            OnCollectionChanged(NotifyCollectionChangedAction.Remove, CopiedItem, index);  
        }

        private void RemoveRange(int index, int count)
        {
            // True range operations are not supported by CollectionView so we instead fire many changed events.
            for (int i = 0; i < count; i++)
            {
                RemoveAt(index);
            }
        }

        private void OnReplace(object oldItem, object newItem, int index)
        {
            OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Replace, newItem, oldItem, index));
        }

        private void Reset()
        {
            RepeatCount = 0;
            OnCollectionReset();
        }

        #endregion

        #region IList Members

        public int Add(object value)
        {
            throw new NotSupportedException(SR.Get(SRID.DataGrid_ReadonlyCellsItemsSource));
        }

        public void Clear()
        {
            throw new NotSupportedException(SR.Get(SRID.DataGrid_ReadonlyCellsItemsSource));
        }

        public bool Contains(object value)
        {
            if (value == null)
            {
                throw new ArgumentNullException("value");
            }

            return _item == value;
        }

        public int IndexOf(object value)
        {
            if (value == null)
            {
                throw new ArgumentNullException("value");
            }

            return (_item == value) ? 0 : -1;
        }

        public void Insert(int index, object value)
        {
            throw new NotSupportedException(SR.Get(SRID.DataGrid_ReadonlyCellsItemsSource));
        }

        public bool IsFixedSize
        {
            get { return false; }
        }

        public bool IsReadOnly
        {
            get { return true; }
        }

        public void Remove(object value)
        {
            throw new NotSupportedException(SR.Get(SRID.DataGrid_ReadonlyCellsItemsSource));
        }

        void IList.RemoveAt(int index)
        {
            throw new NotSupportedException(SR.Get(SRID.DataGrid_ReadonlyCellsItemsSource));
        }

        public object this[int index]
        {
            get
            {
                if ((index >= 0) && (index < RepeatCount))
                {
                    Debug.Assert(_item != null, "_item should be non-null.");
                    return _item;
                }
                else
                {
                    throw new ArgumentOutOfRangeException("index");
                }
            }

            set
            {
                throw new InvalidOperationException();
            }
        }

        #endregion

        #region ICollection Members

        public void CopyTo(Array array, int index)
        {
            throw new NotSupportedException();
        }

        public int Count
        {
            get { return RepeatCount; }
        }

        public bool IsSynchronized
        {
            get { return false; }
        }

        public object SyncRoot
        {
            get { return this; }
        }

        #endregion

        #region IEnumerable Members

        public IEnumerator GetEnumerator()
        {
            return new MultipleCopiesCollectionEnumerator(this);
        }

        private class MultipleCopiesCollectionEnumerator : IEnumerator
        {
            public MultipleCopiesCollectionEnumerator(MultipleCopiesCollection collection)
            {
                _collection = collection;
                _item = _collection.CopiedItem;
                _count = _collection.RepeatCount;
                _current = -1;
            }

            #region IEnumerator Members

            object IEnumerator.Current
            {
                get
                {
                    if (_current >= 0)
                    {
                        if (_current < _count)
                        {
                            return _item;
                        }
                        else
                        {
                            throw new InvalidOperationException();
                        }
                    }
                    else
                    {
                        return null;
                    }
                }
            }

            bool IEnumerator.MoveNext()
            {
                if (IsCollectionUnchanged)
                {
                    int newIndex = _current + 1;
                    if (newIndex < _count)
                    {
                        _current = newIndex;
                        return true;
                    }

                    return false;
                }
                else
                {
                    throw new InvalidOperationException();
                }
            }

            void IEnumerator.Reset()
            {
                if (IsCollectionUnchanged)
                {
                    _current = -1;
                }
                else
                {
                    throw new InvalidOperationException();
                }
            }

            private bool IsCollectionUnchanged
            {
                get
                {
                    return (_collection.RepeatCount == _count) && (_collection.CopiedItem == _item);
                }
            }

            #endregion

            #region Data

            private object _item;
            private int _count;
            private int _current;
            private MultipleCopiesCollection _collection;

            #endregion
        }

        #endregion

        #region INotifyCollectionChanged Members

        public event NotifyCollectionChangedEventHandler CollectionChanged;

        /// <summary>
        ///     Helper to raise a CollectionChanged event when an item is added or removed.
        /// </summary>
        private void OnCollectionChanged(NotifyCollectionChangedAction action, object item, int index)
        {
            OnCollectionChanged(new NotifyCollectionChangedEventArgs(action, item, index));
        }

        /// <summary>
        ///     Helper to raise a CollectionChanged event when the collection is cleared.
        /// </summary>
        private void OnCollectionReset()
        {
            OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
        }

        private void OnCollectionChanged(NotifyCollectionChangedEventArgs e)
        {
            if (CollectionChanged != null)
            {
                CollectionChanged(this, e);
            }
        }

        #endregion

        #region INotifyPropertyChanged Members

        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        ///     Helper to raise a PropertyChanged event.
        /// </summary>
        private void OnPropertyChanged(string propertyName)
        {
            OnPropertyChanged(new PropertyChangedEventArgs(propertyName));
        }

        private void OnPropertyChanged(PropertyChangedEventArgs e)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, e);
            }
        }

        #endregion

        #region Data

        private object _item; // consider WeakReference
        private int _count;

        private const string CountName = "Count";
        private const string IndexerName = "Item[]";

        #endregion
    }
}