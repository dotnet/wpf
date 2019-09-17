// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// 
//
// Description: The PartialArray struct is used when the developer needs to pass a CLR array range to 
//              a function that takes generic IList interface. For cases when the whole array needs to be passed,
//              CLR array already implements IList.
// 
//
//

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using SR = MS.Internal.PresentationCore.SR;
using SRID = MS.Internal.PresentationCore.SRID;

namespace MS.Internal
{
    /// <summary>
    /// The PartialArray struct is used when someone needs to pass a CLR array range to 
    /// a function that takes generic IList interface. For cases when the whole array needs to be passed,
    /// CLR array already implements IList.
    /// </summary>
    internal struct PartialArray<T> : IList<T>
    {
        private T[] _array;
        private int _initialIndex;
        private int _count;

        public PartialArray(T[] array, int initialIndex, int count)
        {
            // make sure early that the caller didn't miscalculate index and count
            Debug.Assert(initialIndex >= 0 && initialIndex + count <= array.Length);

            _array = array;
            _initialIndex = initialIndex;
            _count = count;
        }

        /// <summary>
        /// Convenience helper for passing the whole array.
        /// </summary>
        /// <param name="array"></param>
        public PartialArray(T[] array) : this(array, 0, array.Length)
        {}

        #region IList<T> Members

        public bool IsReadOnly
        {
            get
            {
                return false;
            }
        }

        public bool Contains(T item)
        {
            return IndexOf(item) >= 0;
        }

        public bool IsFixedSize
        {
            get
            {
                return true;
            }
        }

        public bool Remove(T item)
        {
            throw new NotSupportedException(SR.Get(SRID.CollectionIsFixedSize));                           
        }

        public void RemoveAt(int index)
        {
            throw new NotSupportedException(SR.Get(SRID.CollectionIsFixedSize));                           
        }

        public void Clear()
        {
            throw new NotSupportedException();                           
        }

        public void Add(T item)
        {
            throw new NotSupportedException(SR.Get(SRID.CollectionIsFixedSize));                           
        }

        public void Insert(int index, T item)
        {
            throw new NotSupportedException(SR.Get(SRID.CollectionIsFixedSize));                           
        }

        public T this[int index]
        {
            get
            {
                return _array[index + _initialIndex];
            }
            set
            {
                _array[index + _initialIndex] = value;
            }
        }

        public int IndexOf(T item)
        {
            int index = Array.IndexOf<T>(_array, item, _initialIndex, _count);
            if (index >= 0)
            {
                return index - _initialIndex;
            }
            else
            {
                return -1;
            }
        }

        #endregion

        #region ICollection<T> Members

        public int Count
        {
            get
            {
                return _count;
            }
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            // parameter validations
            if (array == null)
            {
                throw new ArgumentNullException("array");
            }

            if (array.Rank != 1)
            {
                throw new ArgumentException(
                    SR.Get(SRID.Collection_CopyTo_ArrayCannotBeMultidimensional), 
                    "array");                
            }

            if (arrayIndex < 0)
            {
                throw new ArgumentOutOfRangeException("arrayIndex");
            }

            if (arrayIndex >= array.Length)
            {
                throw new ArgumentException(
                    SR.Get(
                        SRID.Collection_CopyTo_IndexGreaterThanOrEqualToArrayLength, 
                        "arrayIndex", 
                        "array"),
                        "arrayIndex");
            }

            if ((array.Length - Count - arrayIndex) < 0)
            {
                throw new ArgumentException(
                    SR.Get(
                        SRID.Collection_CopyTo_NumberOfElementsExceedsArrayLength,
                        "arrayIndex",
                        "array"));
            }           
            

            // do the copying here
            for (int i = 0; i < Count; i++)
            {
                array[arrayIndex + i] = this[i];
            }
        }

        #endregion

        #region IEnumerable<T> Members

        IEnumerator<T> IEnumerable<T>.GetEnumerator()
        {
            for (int i = 0; i < Count; i++)
            {
                yield return this[i];
            }
        }

        #endregion

        #region IEnumerable Members

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable<T>)this).GetEnumerator();
        }

        #endregion
    }
}

