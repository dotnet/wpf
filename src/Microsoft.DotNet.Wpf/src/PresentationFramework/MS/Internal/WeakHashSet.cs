// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// This file was generated, please do not edit it directly.
// 
// This file was generated using the common file located at:

namespace MS.Internal
{
    using System;
    using System.Collections;
    using System.Collections.Generic;

    /// <summary>
    ///     Helper WeakHashSet class implemented using WeakHashTable
    /// </summary>
    internal class WeakHashSet<T> : ICollection<T> where T : class
    {
        #region ICollection<T> Members

        public void Add(T item)
        {
            if (!_hashTable.ContainsKey(item))
            {
                _hashTable.SetWeak(item, null);
            }
        }

        public void Clear()
        {
            _hashTable.Clear();
        }

        public bool Contains(T item)
        {
            return _hashTable.ContainsKey(item);
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            if (arrayIndex < 0)
            {
                throw new ArgumentOutOfRangeException("arrayIndex");
            }
            if (array == null)
            {
                throw new ArgumentNullException("array");
            }

            int count = 0;
            foreach (T item in this)
            {
                count++;
            }

            if (count + arrayIndex > array.Length)
            {
                throw new ArgumentOutOfRangeException("arrayIndex");
            }

            foreach (T item in this)
            {
                array[arrayIndex++] = item;
            }
        }

        public int Count
        {
            get
            {
                return _hashTable.Count;
            }
        }

        public bool IsReadOnly
        {
            get { return false; }
        }

        public bool Remove(T item)
        {
            if (_hashTable.ContainsKey(item))
            {
                _hashTable.Remove(item);
                return true;
            }
            return false;
        }

        #endregion

        #region IEnumerable<T> Members

        public IEnumerator<T> GetEnumerator()
        {
            foreach (object key in _hashTable.Keys)
            {
                WeakHashtable.EqualityWeakReference objRef = key as WeakHashtable.EqualityWeakReference;
                if (objRef != null)
                {
                    T obj = objRef.Target as T;
                    if (obj != null)
                    {
                        yield return obj;
                    }
                }
            }
        }

        #endregion

        #region IEnumerable Members

        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }

        #endregion

        #region Private Data

        WeakHashtable _hashTable = new WeakHashtable();

        #endregion
    }
}
