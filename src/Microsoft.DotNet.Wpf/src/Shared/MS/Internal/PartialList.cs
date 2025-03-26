// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//---------------------------------------------------------------------------
//

// 
//
// Description: PartialList is used when the developer needs to pass an IList range to 
//              a function that takes generic IList interface.
// 
//
//  
//
//
//---------------------------------------------------------------------------

using System.Collections;

namespace MS.Internal
{
    /// <summary>
    /// PartialList is used when someone needs to pass an IList range to 
    /// a function that takes generic IList interface. It implemented a read-only subset of IList.
    /// </summary>
    internal class PartialList<T> : IList<T>
    {
        private IList<T>    _list;
        private int _initialIndex;
        private int _count;

        public PartialList(IList<T> list, int initialIndex, int count)
        {
            // make sure early that the caller didn't miscalculate index and count
            Debug.Assert(initialIndex >= 0 && initialIndex + count <= list.Count);

            _list = list;
            _initialIndex = initialIndex;
            _count = count;
        }

        #region IList<T> Members

        public void RemoveAt(int index)
        {
            // PartialList is read only.
            throw new NotSupportedException();
        }

        public void Insert(int index, T item)
        {
            // PartialList is read only.
            throw new NotSupportedException();
        }

        public T this[int index]
        {
            get
            {
                return _list[index + _initialIndex];
            }
            set
            {
                // PartialList is read only.
                throw new NotSupportedException();
            }
        }

        public int IndexOf(T item)
        {
            int index = _list.IndexOf(item);
            if (index == -1 || index < _initialIndex || index - _initialIndex >= _count)
                return -1;

            return index - _initialIndex;
        }

        #endregion

        #region ICollection<T> Members

        public bool IsReadOnly
        {
            get
            {
                return true;
            }
        }

        public void Clear()
        {
            // PartialList is read only.
            throw new NotSupportedException();
        }

        public void Add(T item)
        {
            // PartialList is read only.
            throw new NotSupportedException();
        }

        public bool Contains(T item)
        {
            return IndexOf(item) != -1;
        }

        public bool Remove(T item)
        {
            // PartialList is read only.
            throw new NotSupportedException();
        }

        public int Count
        {
            get
            {
                return _count;
            }
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            ArgumentOutOfRangeException.ThrowIfNegative(arrayIndex);

            for (int i = 0; i < _count; ++i)
                array[arrayIndex + i] = this[i];
        }

        #endregion

        #region IEnumerable<T> Members

        IEnumerator<T> IEnumerable<T>.GetEnumerator()
        {
            for (int i = _initialIndex; i < _initialIndex + _count; ++i)
                yield return _list[i];
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

