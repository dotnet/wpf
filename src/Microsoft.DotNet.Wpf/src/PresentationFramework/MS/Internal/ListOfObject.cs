// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections;
using System.Collections.Generic;

namespace MS.Internal
{
    // This class is useful in situations where you have an IList that you need
    // to expose as a generic collection: IList<object>.   It simply wraps the
    // given IList.

    internal class ListOfObject : IList<object>
    {
        IList _list;
        internal ListOfObject(IList list)
        {
            if (list == null)
                throw new ArgumentNullException("list");
            _list = list;
        }

        #region IList<object> Members

        int IList<object>.IndexOf(object item)
        {
            return _list.IndexOf(item);
        }

        void IList<object>.Insert(int index, object item)
        {
            throw new NotImplementedException();
        }

        void IList<object>.RemoveAt(int index)
        {
            throw new NotImplementedException();
        }

        object IList<object>.this[int index]
        {
            get
            {
                return _list[index];
            }
            set
            {
                throw new NotImplementedException();
            }
        }

        #endregion

        #region ICollection<object> Members

        void ICollection<object>.Add(object item)
        {
            throw new NotImplementedException();
        }

        void ICollection<object>.Clear()
        {
            throw new NotImplementedException();
        }

        bool ICollection<object>.Contains(object item)
        {
            return _list.Contains(item);
        }

        void ICollection<object>.CopyTo(object[] array, int arrayIndex)
        {
            _list.CopyTo(array, arrayIndex);
        }

        int ICollection<object>.Count
        {
            get { return _list.Count; }
        }

        bool ICollection<object>.IsReadOnly
        {
            get { return true; }
        }

        bool ICollection<object>.Remove(object item)
        {
            throw new NotImplementedException();
        }

        #endregion

        #region IEnumerable<object> Members

        IEnumerator<object> IEnumerable<object>.GetEnumerator()
        {
            return new ObjectEnumerator(_list);
        }

        #endregion

        #region IEnumerable Members

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return ((IEnumerable<object>)this).GetEnumerator();
        }

        #endregion

        class ObjectEnumerator : IEnumerator<object>
        {
            IEnumerator _ie;
            public ObjectEnumerator(IList list)
            {
                _ie = list.GetEnumerator();
            }

            #region IEnumerator<object> Members

            object IEnumerator<object>.Current
            {
                get { return _ie.Current; }
            }

            #endregion

            #region IDisposable Members

            void IDisposable.Dispose()
            {
                _ie = null;
            }

            #endregion

            #region IEnumerator Members

            object IEnumerator.Current
            {
                get { return _ie.Current; }
            }

            bool IEnumerator.MoveNext()
            {
                return _ie.MoveNext();
            }

            void IEnumerator.Reset()
            {
                _ie.Reset();
            }

            #endregion
        }
    }
}

