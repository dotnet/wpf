// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
//
//
//  Contents:  FontFamilyMapCollection
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
    /// List of FontFamilyMap objects in a FontFamily, in lookup order.
    /// </summary>
    public sealed class FontFamilyMapCollection : IList<FontFamilyMap>, SC.IList
    {
        private const int InitialCapacity = 8;
        private CompositeFontInfo _fontInfo;
        private FontFamilyMap[] _items;
        private int _count;

        internal FontFamilyMapCollection(CompositeFontInfo fontInfo)
        {
            _fontInfo = fontInfo;
            _items = null;
            _count = 0;
        }

        #region IEnumerable members

        /// <summary>
        /// Returns an enumerator for iterating through the list.
        /// </summary>
        public IEnumerator<FontFamilyMap> GetEnumerator()
        {
            return new Enumerator(_items, _count);
        }

        SC.IEnumerator SC.IEnumerable.GetEnumerator()
        {
            return new Enumerator(_items, _count);
        }

        #endregion

        #region ICollection methods

        /// <summary>
        /// Adds a FontFamilyMap to the font family.
        /// </summary>
        public void Add(FontFamilyMap item)
        {
            InsertItem(_count, item);
        }

        /// <summary>
        /// Removes all FontFamilyMap objects from the FontFamily.
        /// </summary>
        public void Clear()
        {
            ClearItems();
        }

        /// <summary>
        /// Determines whether the FontFamily contains the specified FontFamilyMap.
        /// </summary>
        public bool Contains(FontFamilyMap item)
        {
            return FindItem(item) >= 0;
        }

        /// <summary>
        /// Copies the contents of the list to the specified array.
        /// </summary>
        public void CopyTo(FontFamilyMap[] array, int index)
        {
            if (array == null)
                throw new ArgumentNullException("array");

            if (index >= array.Length)
                throw new ArgumentException(SR.Get(SRID.Collection_CopyTo_IndexGreaterThanOrEqualToArrayLength, "index", "array"));

            if (_count > array.Length - index)
                throw new ArgumentException(SR.Get(SRID.Collection_CopyTo_NumberOfElementsExceedsArrayLength, index, "array"));

            if (_count != 0)
                Array.Copy(_items, 0, array, index, _count);
        }

        void SC.ICollection.CopyTo(Array array, int index)
        {
            if (array == null)
                throw new ArgumentNullException("array");

            if (array.Rank != 1)
                throw new ArgumentException(SR.Get(SRID.Collection_CopyTo_ArrayCannotBeMultidimensional));

            Type elementType = array.GetType().GetElementType();
            if (!elementType.IsAssignableFrom(typeof(FamilyTypeface)))
                throw new ArgumentException(SR.Get(SRID.CannotConvertType, typeof(FamilyTypeface), elementType));

            if (index >= array.Length)
                throw new ArgumentException(SR.Get(SRID.Collection_CopyTo_IndexGreaterThanOrEqualToArrayLength, "index", "array"));

            if (_count > array.Length - index)
                throw new ArgumentException(SR.Get(SRID.Collection_CopyTo_NumberOfElementsExceedsArrayLength, index, "array"));

            if (_count != 0)
                Array.Copy(_items, 0, array, index, _count);
        }

        bool SC.ICollection.IsSynchronized
        {
            get { return false; }
        }

        object SC.ICollection.SyncRoot
        {
            get { return (_fontInfo != null) ? (object)_fontInfo : (object)this; }
        }

        /// <summary>
        /// Removes the specified FontFamilyMap.
        /// </summary>
        public bool Remove(FontFamilyMap item)
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
        /// Gets a value indicating whether the FontFamilyMap list can be changed.
        /// </summary>
        public bool IsReadOnly
        {
            get { return _fontInfo == null; }
        }

        #endregion

        #region IList members

        /// <summary>
        /// Gets the index of the specified FontFamilyMap.
        /// </summary>
        public int IndexOf(FontFamilyMap item)
        {
            return FindItem(item);
        }

        /// <summary>
        /// Inserts a FontFamilyMap into the list.
        /// </summary>
        public void Insert(int index, FontFamilyMap item)
        {
            InsertItem(index, item);
        }

        /// <summary>
        /// Removes the FontFamilyMap at the specified index.
        /// </summary>
        public void RemoveAt(int index)
        {
            RemoveItem(index);
        }

        /// <summary>
        /// Gets or sets the FontFamilyMap at the specified index.
        /// </summary>
        public FontFamilyMap this[int index]
        {
            get
            {
                RangeCheck(index);
                return _items[index];
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
            return FindItem(value as FontFamilyMap) >= 0;
        }

        int SC.IList.IndexOf(object value)
        {
            return FindItem(value as FontFamilyMap);
        }

        void SC.IList.Insert(int index, object item)
        {
            InsertItem(index, ConvertValue(item));
        }

        void SC.IList.Remove(object value)
        {
            VerifyChangeable();
            int i = FindItem(value as FontFamilyMap);
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
                RangeCheck(index);
                return _items[index];
            }

            set
            {
                SetItem(index, ConvertValue(value));
            }
        }

        #endregion

        #region Internal implementation

        private int InsertItem(int index, FontFamilyMap item)
        {
            if (item == null)
                throw new ArgumentNullException("item");

            VerifyChangeable();

            // Limit the number of family maps because we use ushort indexes in the skip lists.
            // To exceed this limit a user would have to have a separate family maps for almost 
            // every Unicode value, in which case (since we search sequentially) performance
            // would become a problem.
            if (_count + 1 >= ushort.MaxValue)
                throw new InvalidOperationException(SR.Get(SRID.CompositeFont_TooManyFamilyMaps));

            // Validate the index.
            if (index < 0 || index > Count)
                throw new ArgumentOutOfRangeException("index");

            // PrepareToAddFamilyMap validates the familyName and updates the internal state
            // of the CompositeFontInfo object.
            _fontInfo.PrepareToAddFamilyMap(item);

            // Make room for the new item.
            if (_items == null)
            {
                _items = new FontFamilyMap[InitialCapacity];
            }
            else if (_count == _items.Length)
            {
                FontFamilyMap[] items = new FontFamilyMap[_count * 2];
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

        private void SetItem(int index, FontFamilyMap item)
        {
            if (item == null)
                throw new ArgumentNullException("item");

            VerifyChangeable();
            RangeCheck(index);

            // PrepareToAddFamilyMap validates the familyName and updates the internal state
            // of the CompositeFontInfo object.
            _fontInfo.PrepareToAddFamilyMap(item);

            if (item.Language != _items[index].Language)
            {
                _fontInfo.InvalidateFamilyMapRanges();
            }

            _items[index] = item;
        }

        private void ClearItems()
        {
            VerifyChangeable();

            _fontInfo.InvalidateFamilyMapRanges();

            _count = 0;
            _items = null;
        }

        private void RemoveItem(int index)
        {
            VerifyChangeable();
            RangeCheck(index);

            _fontInfo.InvalidateFamilyMapRanges();

            _count--;
            for (int i = index; i < _count; ++i)
            {
                _items[i] = _items[i + 1];
            }
            _items[_count] = null;
        }

        private int FindItem(FontFamilyMap item)
        {
            if (_count != 0 && item != null)
            {
                for (int i = 0; i < _count; ++i)
                {
                    if (_items[i].Equals(item))
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
            if (_fontInfo == null)
                throw new NotSupportedException(SR.Get(SRID.General_ObjectIsReadOnly));
        }

        private FontFamilyMap ConvertValue(object obj)
        {
            if (obj == null)
                throw new ArgumentNullException("obj");

            FontFamilyMap familyMap = obj as FontFamilyMap;
            if (familyMap == null)
                throw new ArgumentException(SR.Get(SRID.CannotConvertType, obj.GetType(), typeof(FontFamilyMap)));

            return familyMap;
        }

        private class Enumerator : IEnumerator<FontFamilyMap>, SC.IEnumerator
        {
            FontFamilyMap[] _items;
            int _count;
            int _index;
            FontFamilyMap _current;

            internal Enumerator(FontFamilyMap[] items, int count)
            {
                _items = items;
                _count = count;
                _index = -1;
                _current = null;
            }

            public bool MoveNext()
            {
                if (_index < _count)
                {
                    _index++;
                    if (_index < _count)
                    {
                        _current = _items[_index];
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

            public FontFamilyMap Current
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
