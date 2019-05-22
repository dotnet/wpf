// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
//
//
//  Contents:  CharacterMetricsDictionary
//
//

using System;
using SC=System.Collections;
using System.Collections.Generic;

using SR=MS.Internal.PresentationCore.SR;
using SRID=MS.Internal.PresentationCore.SRID;

// Allow suppression of presharp warnings
#pragma warning disable 1634, 1691

namespace System.Windows.Media
{
    /// <summary>
    /// Dictionary of character metrics for a device font indexed by Unicode scalar value.
    /// </summary>
    public sealed class CharacterMetricsDictionary : IDictionary<int, CharacterMetrics>, SC.IDictionary
    {
        /// <summary>
        /// Constructs an empty CharacterMetricsDictionary object.
        /// </summary>
        internal CharacterMetricsDictionary()
        {
        }

        #region IEnumerable members

        /// <summary>
        /// Returns an enumerator that iterates through the collection.
        /// </summary>
        [CLSCompliant(false)]
        public IEnumerator<KeyValuePair<int, CharacterMetrics>> GetEnumerator()
        {
            return new Enumerator(this);
        }

        SC.IEnumerator SC.IEnumerable.GetEnumerator()
        {
            return new Enumerator(this);
        }

        SC.IDictionaryEnumerator SC.IDictionary.GetEnumerator()
        {
            return new Enumerator(this);
        }

        /// <summary>
        /// If the dictionary contains an entry for the specified character code, returns true
        /// and stores the CharacterMetrics in the value parameter; otherwise returns false and
        /// sets value to null.
        /// </summary>
        public bool TryGetValue(int key, out CharacterMetrics value)
        {
            value = GetValue(key);
            return value != null;
        }

        #endregion

        #region ICollection members

        /// <summary>
        /// Gets the number of objects in the collection.
        /// </summary>
        public int Count
        {
            get
            {
                if (_count == 0)
                {
                    _count = CountValues();
                }
                return _count;
            }
        }

        /// <summary>
        /// Gets a value indicating whether the collection is read-only.
        /// </summary>
        public bool IsReadOnly
        {
            get { return false; }
        }

        /// <summary>
        /// Adds a character code and associated CharacterMetrics to the collection.
        /// </summary>
        [CLSCompliant(false)]
        public void Add(KeyValuePair<int, CharacterMetrics> item)
        {
            SetValue(
                item.Key,
                item.Value,
                true // failIfExists
                );
        }

        /// <summary>
        /// Removes all objects from the collection.
        /// </summary>
        public void Clear()
        {
            _count = 0;
            _pageTable = null;
        }

        /// <summary>
        /// Determines whether the collection contains the specified characterCode-CharacterMetrics pair.
        /// </summary>
        [CLSCompliant(false)]
        public bool Contains(KeyValuePair<int, CharacterMetrics> item)
        {
            // Suppress PRESharp warning that item.Value can be null; apparently PRESharp
            // doesn't understand short circuit evaluation of operator &&.
#pragma warning suppress 56506
            return item.Value != null && item.Value.Equals(GetValue(item.Key));
        }

        /// <summary>
        /// Copies the contents of the collection to the specified array.
        /// </summary>
        [CLSCompliant(false)]
        public void CopyTo(KeyValuePair<int, CharacterMetrics>[] array, int index)
        {
            if (array == null)
                throw new ArgumentNullException("array");

            if (index < 0)
                throw new ArgumentOutOfRangeException("index");

            if (index >= array.Length)
                throw new ArgumentException(SR.Get(SRID.Collection_CopyTo_IndexGreaterThanOrEqualToArrayLength, "index", "array"));

            CharacterMetrics[][] pageTable = _pageTable;
            if (pageTable != null)
            {
                int k = index;

                for (int i = 0; i < pageTable.Length; ++i)
                {
                    CharacterMetrics[] page = pageTable[i];
                    if (page != null)
                    {
                        for (int j = 0; j < page.Length; ++j)
                        {
                            CharacterMetrics metrics = page[j];
                            if (metrics != null)
                            {
                                if (k >= array.Length)
                                    throw new ArgumentException(SR.Get(SRID.Collection_CopyTo_NumberOfElementsExceedsArrayLength, index, "array"));

                                array[k++] = new KeyValuePair<int, CharacterMetrics>(
                                    (i << PageShift) | j,
                                    metrics
                                    );
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Removes the specified characterCode-CharacterMetrics pair from the collection.
        /// </summary>
        [CLSCompliant(false)]
        public bool Remove(KeyValuePair<int, CharacterMetrics> item)
        {
            return item.Value != null && RemoveValue(item.Key, item.Value);
        }

        bool SC.ICollection.IsSynchronized
        {
            get { return false; }
        }

        object SC.ICollection.SyncRoot
        {
            get { return this; }
        }

        void SC.ICollection.CopyTo(Array array, int index)
        {
            if (array == null)
                throw new ArgumentNullException("array");

            if (index < 0)
                throw new ArgumentOutOfRangeException("index");

            if (index >= array.Length)
                throw new ArgumentException(SR.Get(SRID.Collection_CopyTo_IndexGreaterThanOrEqualToArrayLength, "index", "array"));

            if (Count > array.Length - index)
                throw new ArgumentException(SR.Get(SRID.Collection_CopyTo_NumberOfElementsExceedsArrayLength, index, "array"));

            SC.DictionaryEntry[] typedArray = array as SC.DictionaryEntry[];
            if (typedArray != null)
            {
                // it's an array of the exact type
                foreach (KeyValuePair<int, CharacterMetrics> item in this)
                {
                    typedArray[index++] = new SC.DictionaryEntry(item.Key, item.Value);
                }
            }
            else
            {
                // it's an array of some other type, e.g., object[]; make sure it's one dimensional
                if (array.Rank != 1)
                    throw new ArgumentException(SR.Get(SRID.Collection_CopyTo_ArrayCannotBeMultidimensional));

                // make sure the element type is compatible
                Type elementType = array.GetType().GetElementType();
                if (!elementType.IsAssignableFrom(typeof(SC.DictionaryEntry)))
                    throw new ArgumentException(SR.Get(SRID.CannotConvertType, typeof(SC.DictionaryEntry), elementType));

                foreach (KeyValuePair<int, CharacterMetrics> item in this)
                {
                    array.SetValue(new SC.DictionaryEntry(item.Key, item.Value), index++);
                }
            }
        }

        #endregion

        #region IDictionary members

        /// <summary>
        /// Adds a character code and associated CharacterMetrics to the collection.
        /// </summary>
        public void Add(int key, CharacterMetrics value)
        {
            SetValue(key, value, /* failIfExists = */ true);
        }

        /// <summary>
        /// Determines whether the collection contains the specified character code.
        /// </summary>
        public bool ContainsKey(int key)
        {
            return GetValue(key) != null;
        }

        /// <summary>
        /// Removes the specified character code and associated CharacterMetrics.
        /// </summary>
        public bool Remove(int key)
        {
            return RemoveValue(key, null);
        }

        /// <summary>
        /// Gets or sets the CharacterMetrics associated with the specified character code.
        /// </summary>
        public CharacterMetrics this[int key]
        {
            get { return GetValue(key); }
            set { SetValue(key, value, /* failIfExists = */ false); }
        }

        /// <summary>
        /// Gets a collection containing the keys (character codes) in the dictionary.
        /// </summary>
        [CLSCompliant(false)]
        public ICollection<int> Keys
        {
            get { return GetKeys(); }
        }

        /// <summary>
        /// Gets a collection containing the values (strings) in the dictionary.
        /// </summary>
        [CLSCompliant(false)]
        public ICollection<CharacterMetrics> Values
        {
            get { return GetValues(); }
        }

        bool SC.IDictionary.IsFixedSize
        {
            get { return false; }
        }

        object SC.IDictionary.this[object key]
        {
            get
            {
                return (key is int) ? GetValue((int)key) : null;
            }

            set
            {
                SetValue(ConvertKey(key), ConvertValue(value), /* failIfExists = */ false);
            }
        }

        SC.ICollection SC.IDictionary.Keys
        {
            get { return GetKeys(); }
        }

        SC.ICollection SC.IDictionary.Values
        {
            get { return GetValues(); }
        }

        void SC.IDictionary.Add(object key, object value)
        {
            SetValue(ConvertKey(key), ConvertValue(value), /* failIfExists = */ false);
        }

        bool SC.IDictionary.Contains(object key)
        {
            return key is int && GetValue((int)key) != null;
        }

        void SC.IDictionary.Remove(object key)
        {
            if (key is int)
            {
                RemoveValue((int)key, null);
            }
        }
        #endregion

        #region Internal representation

        internal const int LastDeviceFontCharacterCode = 0xFFFF;

        internal const int PageShift = 8;
        internal const int PageSize = 1 << PageShift;
        internal const int PageMask = PageSize - 1;
        internal const int PageCount = (LastDeviceFontCharacterCode + 1 + (PageSize - 1)) / PageSize;

        private CharacterMetrics[][] _pageTable = null;
        private int _count = 0;

        internal CharacterMetrics[] GetPage(int i)
        {
            return (_pageTable != null) ? _pageTable[i] : null;
        }

        private CharacterMetrics[] GetPageFromUnicodeScalar(int unicodeScalar)
        {
            int i = unicodeScalar >> PageShift;

            CharacterMetrics[] page;

            if (_pageTable != null)
            {
                page = _pageTable[i];
                if (page == null)
                {
                    _pageTable[i] = page = new CharacterMetrics[PageSize];
                }
            }
            else
            {
                _pageTable = new CharacterMetrics[PageCount][];
                _pageTable[i] = page = new CharacterMetrics[PageSize];
            }

            return page;
        }

        private void SetValue(int key, CharacterMetrics value, bool failIfExists)
        {
            if (key < 0 || key > LastDeviceFontCharacterCode)
                throw new ArgumentOutOfRangeException(SR.Get(SRID.CodePointOutOfRange, key));

            if (value == null)
                throw new ArgumentNullException("value");

            CharacterMetrics[] page = GetPageFromUnicodeScalar(key);
            int i = key & PageMask;

            if (failIfExists && page[i] != null)
                throw new ArgumentException(SR.Get(SRID.CollectionDuplicateKey, key));

            page[i] = value;
            _count = 0;
        }

        internal CharacterMetrics GetValue(int key)
        {
            CharacterMetrics metrics = null;

            if (key >= 0 && key <= FontFamilyMap.LastUnicodeScalar && _pageTable != null)
            {
                CharacterMetrics[] page = _pageTable[key >> PageShift];
                if (page != null)
                    metrics = page[key & PageMask];
            }

            return metrics;
        }

        private bool RemoveValue(int key, CharacterMetrics value)
        {
            if (key >= 0 && key <= FontFamilyMap.LastUnicodeScalar && _pageTable != null)
            {
                CharacterMetrics[] page = _pageTable[key >> PageShift];
                if (page != null)
                {
                    int i = key & PageMask;
                    CharacterMetrics metrics = page[i];

                    if (metrics != null && (value == null || metrics.Equals(value)))
                    {
                        page[i] = null;
                        _count = 0;
                        return true;
                    }
                }
            }
            return false;
        }

        private CharacterMetrics GetNextValue(ref int unicodeScalar)
        {
            CharacterMetrics[][] pageTable = _pageTable;
            if (pageTable != null)
            {
                int j = (unicodeScalar + 1) & PageMask;

                for (int i = (unicodeScalar + 1) >> PageShift; i < PageCount; ++i)
                {
                    CharacterMetrics[] page = pageTable[i];
                    if (page != null)
                    {
                        for (; j < PageSize; ++j)
                        {
                            CharacterMetrics metrics = page[j];
                            if (metrics != null)
                            {
                                unicodeScalar = (i << PageShift) | j;
                                return metrics;
                            }
                        }

                        j = 0;
                    }
                }
            }

            unicodeScalar = int.MaxValue;
            return null;
        }

        private int CountValues()
        {
            int c = 0;

            CharacterMetrics[][] pageTable = _pageTable;
            if (pageTable != null)
            {
                for (int i = 0; i < pageTable.Length; ++i)
                {
                    CharacterMetrics[] page = pageTable[i];
                    if (page != null)
                    {
                        for (int j = 0; j < page.Length; ++j)
                        {
                            if (page[j] != null)
                                ++c;
                        }
                    }
                }
            }

            return c;
        }

        private int[] GetKeys()
        {
            int[] result = new int[Count];
            int i = 0;
            foreach (KeyValuePair<int, CharacterMetrics> pair in this)
            {
                result[i++] = pair.Key;
            }
            return result;
        }

        private CharacterMetrics[] GetValues()
        {
            CharacterMetrics[] result = new CharacterMetrics[Count];
            int i = 0;
            foreach (KeyValuePair<int, CharacterMetrics> pair in this)
            {
                result[i++] = pair.Value;
            }
            return result;
        }

        internal static int ConvertKey(object key)
        {
            if (key == null)
                throw new ArgumentNullException("key");

            int value;

            string s = key as string;
            if (s != null)
            {
                int i = 0;
                if (!FontFamilyMap.ParseHexNumber(s, ref i, out value) || i < s.Length)
                    throw new ArgumentException(SR.Get(SRID.CannotConvertStringToType, s, "int"), "key");
            }
            else if (key is int)
            {
                value = (int)key;
            }
            else
            {
                throw new ArgumentException(SR.Get(SRID.CannotConvertType, key.GetType(), "int"), "key");
            }

            if (value < 0 || value > FontFamilyMap.LastUnicodeScalar)
                throw new ArgumentException(SR.Get(SRID.CodePointOutOfRange, value), "key");

            return value;
        }

        private CharacterMetrics ConvertValue(object value)
        {
            CharacterMetrics metrics = value as CharacterMetrics;
            if (metrics != null)
                return metrics;

            if (value != null)
                throw new ArgumentException(SR.Get(SRID.CannotConvertType, typeof(CharacterMetrics), value.GetType()));
            else
                throw new ArgumentNullException("value");
        }

        private struct Enumerator : SC.IDictionaryEnumerator, IEnumerator<KeyValuePair<int, CharacterMetrics>>
        {
            private CharacterMetricsDictionary _dictionary;
            private int _unicodeScalar;
            private CharacterMetrics _value;

            internal Enumerator(CharacterMetricsDictionary dictionary)
            {
                _dictionary = dictionary;
                _unicodeScalar = -1;
                _value = null;
            }

            void IDisposable.Dispose()
            {
            }

            public bool MoveNext()
            {
                if (_unicodeScalar < int.MaxValue)
                {
                    _value = _dictionary.GetNextValue(ref _unicodeScalar);
                }
                return _value != null;
            }

            void SC.IEnumerator.Reset()
            {
                _unicodeScalar = -1;
            }

            // Current object in the sequence, which for an IDictionaryEnumerator
            // is expected to be a DictionaryEntry.
            object SC.IEnumerator.Current
            {
                get
                {
                    KeyValuePair<int, CharacterMetrics> entry = GetCurrentEntry();
                    return new SC.DictionaryEntry(entry.Key, entry.Value);
                }
            }

            // Current property for generic enumerator.
            public KeyValuePair<int, CharacterMetrics> Current
            {
                get
                {
                    return new KeyValuePair<int, CharacterMetrics>(_unicodeScalar, _value);
                }
            }

            private KeyValuePair<int, CharacterMetrics> GetCurrentEntry()
            {
                if (_value != null)
                    return new KeyValuePair<int, CharacterMetrics>(_unicodeScalar, _value);
                else
                    throw new InvalidOperationException(SR.Get(SRID.Enumerator_VerifyContext));
            }

            SC.DictionaryEntry SC.IDictionaryEnumerator.Entry
            {
                get
                {
                    KeyValuePair<int, CharacterMetrics> entry = GetCurrentEntry();
                    return new SC.DictionaryEntry(entry.Key, entry.Value);
                }
            }

            object SC.IDictionaryEnumerator.Key
            {
                get
                {
                    return GetCurrentEntry().Key;
                }
            }

            object SC.IDictionaryEnumerator.Value
            {
                get
                {
                    return GetCurrentEntry().Value;
                }
            }
        }

        #endregion
    }
}
