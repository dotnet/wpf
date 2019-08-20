// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
//
//
//  Contents:  FamilyTypefaceCollection
//
//

using System;
using System.Globalization;
using SC=System.Collections;
using System.Collections.Generic;
using MS.Internal.FontFace;

using SR=MS.Internal.PresentationCore.SR;
using SRID=MS.Internal.PresentationCore.SRID;

namespace System.Windows.Media
{
    /// <summary>
    /// List of FamilyTypeface objects in a FontFamily, in lookup order.
    /// </summary>
    public sealed class FamilyTypefaceCollection : IList<FamilyTypeface>, SC.IList
    {
        private const int InitialCapacity = 2;
        private ICollection<Typeface> _innerList;
        private FamilyTypeface[] _items;
        private int _count;

        /// <summary>
        /// Constructs a read-write list of FamilyTypeface objects.
        /// </summary>
        internal FamilyTypefaceCollection()
        {
            _innerList = null;
            _items = null;
            _count = 0;
        }

        /// <summary>
        /// Constructes a read-only list that wraps an ICollection.
        /// </summary>
        internal FamilyTypefaceCollection(ICollection<Typeface> innerList)
        {
            _innerList = innerList;
            _items = null;
            _count = innerList.Count;
        }

        #region IEnumerable members

        /// <summary>
        /// Returns an enumerator for iterating through the list.
        /// </summary>
        public IEnumerator<FamilyTypeface> GetEnumerator()
        {
            return new Enumerator(this);
        }

        SC.IEnumerator SC.IEnumerable.GetEnumerator()
        {
            return new Enumerator(this);
        }

        #endregion

        #region ICollection methods

        /// <summary>
        /// Adds a FamilyTypeface to the font family.
        /// </summary>
        public void Add(FamilyTypeface item)
        {
            InsertItem(_count, item);
        }

        /// <summary>
        /// Removes all FamilyTypeface objects from the FontFamily.
        /// </summary>
        public void Clear()
        {
            ClearItems();
        }

        /// <summary>
        /// Determines whether the FontFamily contains the specified FamilyTypeface.
        /// </summary>
        public bool Contains(FamilyTypeface item)
        {
            return FindItem(item) >= 0;
        }

        /// <summary>
        /// Copies the contents of the list to the specified array.
        /// </summary>
        public void CopyTo(FamilyTypeface[] array, int index)
        {
            CopyItems(array, index);
        }

        void SC.ICollection.CopyTo(Array array, int index)
        {
            CopyItems(array, index);
        }

        bool SC.ICollection.IsSynchronized
        {
            get { return false; }
        }

        object SC.ICollection.SyncRoot
        {
            get { return this; }
        }

        /// <summary>
        /// Removes the specified FamilyTypeface.
        /// </summary>
        public bool Remove(FamilyTypeface item)
        {
            VerifyChangeable();
            int i = FindItem(item);
            if (i >= 0)
            {
                RemoveAt(i);
                return true;
            }
            return false;
        }

        /// <summary>
        /// Gets the number of items in the list.
        /// </summary>
        public int Count
        {
            get { return _count; }
        }

        /// <summary>
        /// Gets a value indicating whether the FamilyTypeface list can be changed.
        /// </summary>
        public bool IsReadOnly
        {
            get { return _innerList != null; }
        }

        #endregion

        #region IList members

        /// <summary>
        /// Gets the index of the specified FamilyTypeface.
        /// </summary>
        public int IndexOf(FamilyTypeface item)
        {
            return FindItem(item);
        }

        /// <summary>
        /// Inserts a FamilyTypeface into the list.
        /// </summary>
        public void Insert(int index, FamilyTypeface item)
        {
            InsertItem(index, item);
        }

        /// <summary>
        /// Removes the FamilyTypeface at the specified index.
        /// </summary>
        public void RemoveAt(int index)
        {
            RemoveItem(index);
        }

        /// <summary>
        /// Gets or sets the FamilyTypeface at the specified index.
        /// </summary>
        public FamilyTypeface this[int index]
        {
            get
            {
                return GetItem(index);
            }

            set
            {
                SetItem(index, value);
            }
        }

        int SC.IList.Add(object value)
        {
            return InsertItem(_count, ConvertValue(value));
        }

        bool SC.IList.Contains(object value)
        {
            return FindItem(value as FamilyTypeface) >= 0;
        }

        int SC.IList.IndexOf(object value)
        {
            return FindItem(value as FamilyTypeface);
        }

        void SC.IList.Insert(int index, object item)
        {
            InsertItem(index, ConvertValue(item));
        }

        void SC.IList.Remove(object value)
        {
            VerifyChangeable();
            int i = FindItem(value as FamilyTypeface);
            if (i >= 0)
                RemoveItem(i);
        }

        bool SC.IList.IsFixedSize
        {
            get { return IsReadOnly; }
        }

        object SC.IList.this[int index]
        {
            get
            {
                return GetItem(index);
            }

            set
            {
                SetItem(index, ConvertValue(value));
            }
        }

        #endregion

        #region Internal implementation

        private int InsertItem(int index, FamilyTypeface item)
        {
            if (item == null)
                throw new ArgumentNullException("item");

            VerifyChangeable();

            // Validate the index.
            if (index < 0 || index > Count)
                throw new ArgumentOutOfRangeException("index");

            // We can't have two items with same style, weight, stretch.
            if (FindItem(item) >= 0)
                throw new ArgumentException(SR.Get(SRID.CompositeFont_DuplicateTypeface));

            // Make room for the new item.
            if (_items == null)
            {
                _items = new FamilyTypeface[InitialCapacity];
            }
            else if (_count == _items.Length)
            {
                FamilyTypeface[] items = new FamilyTypeface[_count * 2];
                for (int i = 0; i < index; ++i)
                    items[i] = _items[i];
                for (int i = index; i < _count; ++i)
                    items[i + 1] = _items[i];
                _items = items;
            }
            else if (index < _count)
            {
                for (int i = _count - 1; i >= index; --i)
                    _items[i + 1] = _items[i];
            }

            // Add the item.
            _items[index] = item;
            _count++;

            return index;
        }

        private void InitializeItemsFromInnerList()
        {
            if (_innerList != null && _items == null)
            {
                // Create the array.
                FamilyTypeface[] items = new FamilyTypeface[_count];

                // Create a FamilyTypeface for each Typeface in the inner list.
                int i = 0;
                foreach (Typeface face in _innerList)
                {
                    items[i++] = new FamilyTypeface(face);
                }

                // For thread-safety, set _items to the fully-initialized array at the end.
                _items = items;
            }
        }

        private FamilyTypeface GetItem(int index)
        {
            RangeCheck(index);
            InitializeItemsFromInnerList();
            return _items[index];
        }

        private void SetItem(int index, FamilyTypeface item)
        {
            if (item == null)
                throw new ArgumentNullException("item");

            VerifyChangeable();
            RangeCheck(index);

            _items[index] = item;
        }

        private void ClearItems()
        {
            VerifyChangeable();

            _count = 0;
            _items = null;
        }

        private void RemoveItem(int index)
        {
            VerifyChangeable();
            RangeCheck(index);

            _count--;
            for (int i = index; i < _count; ++i)
            {
                _items[i] = _items[i + 1];
            }
            _items[_count] = null;
        }

        private int FindItem(FamilyTypeface item)
        {
            InitializeItemsFromInnerList();
            if (_count != 0 && item != null)
            {
                for (int i = 0; i < _count; ++i)
                {
                    if (GetItem(i).Equals(item))
                        return i;
                }
            }
            return -1;
        }

        private void RangeCheck(int index)
        {
            if (index < 0 || index >= _count)
                throw new ArgumentOutOfRangeException("index");
        }

        private void VerifyChangeable()
        {
            if (_innerList != null)
                throw new NotSupportedException(SR.Get(SRID.General_ObjectIsReadOnly));
        }

        private FamilyTypeface ConvertValue(object obj)
        {
            if (obj == null)
                throw new ArgumentNullException("obj");

            FamilyTypeface familyTypeface = obj as FamilyTypeface;
            if (familyTypeface == null)
                throw new ArgumentException(SR.Get(SRID.CannotConvertType, obj.GetType(), typeof(FamilyTypeface)));

            return familyTypeface;
        }

        private void CopyItems(Array array, int index)
        {
            if (array == null)
                throw new ArgumentNullException("array");

            if (array.Rank != 1)
                throw new ArgumentException(SR.Get(SRID.Collection_CopyTo_ArrayCannotBeMultidimensional));

            Type elementType = array.GetType().GetElementType();
            if (!elementType.IsAssignableFrom(typeof(FamilyTypeface)))
                throw new ArgumentException(SR.Get(SRID.CannotConvertType, typeof(FamilyTypeface[]), elementType));

            if (index >= array.Length)
                throw new ArgumentException(SR.Get(SRID.Collection_CopyTo_IndexGreaterThanOrEqualToArrayLength, "index", "array"));

            if (_count > array.Length - index)
                throw new ArgumentException(SR.Get(SRID.Collection_CopyTo_NumberOfElementsExceedsArrayLength, index, "array"));

            if (_count != 0)
            {
                InitializeItemsFromInnerList();
                Array.Copy(_items, 0, array, index, _count);
            }
        }

        private class Enumerator : IEnumerator<FamilyTypeface>, SC.IEnumerator
        {
            FamilyTypefaceCollection _list;
            int _index;
            FamilyTypeface _current;

            internal Enumerator(FamilyTypefaceCollection list)
            {
                _list = list;
                _index = -1;
                _current = null;
            }

            public bool MoveNext()
            {
                int count = _list.Count;

                if (_index < count)
                {
                    _index++;
                    if (_index < count)
                    {
                        _current = _list[_index];
                        return true;
                    }
                }
                _current = null;
                return false;
            }

            void SC.IEnumerator.Reset()
            {
                _index = -1;
            }

            public FamilyTypeface Current
            {
                get { return _current; }
            }

            object SC.IEnumerator.Current
            {
                get 
                {
                    // If there is no current item a non-generic IEnumerator should throw an exception,
                    // but a generic IEnumerator<T> is not required to.
                    if (_current == null)
                        throw new InvalidOperationException(SR.Get(SRID.Enumerator_VerifyContext));

                    return _current; 
                }
            }

            public void Dispose()
            {
            }
        }

        #endregion
    }
}
