// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// Description:
//      Implements the DocumentReferenceCollection as holder for a collection
//      of DocumentReference
//

namespace System.Windows.Documents
{
    using System;
    using System.Collections.Generic;
    using System.Collections;
    using System.Collections.Specialized;
    using System.Diagnostics;


    //=====================================================================
    /// <summary>
    /// DocumentReferenceCollection is an ordered collection of DocumentReference
    /// </summary>
    [CLSCompliant(false)]
    public sealed class DocumentReferenceCollection : IList, IEnumerable<DocumentReference>, INotifyCollectionChanged
    {
        //--------------------------------------------------------------------
        //
        // Connstructors
        //
        //---------------------------------------------------------------------

        #region Constructors
        internal DocumentReferenceCollection()
        {
        }
        #endregion Constructors

        //--------------------------------------------------------------------
        //
        // Public Methods
        //
        //---------------------------------------------------------------------
        #region Public Methods

        ///<summary>
        ///
        ///</summary>
        public void Add(DocumentReference item)
        {
            int count = _InternalList.Count;

            _InternalList.Add(item);

            OnCollectionChanged(NotifyCollectionChangedAction.Add, item, count);
        }

        /// <summary>
        /// Passes in document reference array to be copied 
        /// </summary>
        public void CopyTo(DocumentReference[] array, int arrayIndex)
        {
            _InternalList.CopyTo(array, arrayIndex);
        }

        int IList.Add(object value)
        {
            Add(Cast(value));

            return Count - 1;
        }

        void IList.Clear()
        {
            throw new NotSupportedException();
        }

        bool IList.Contains(object value)
        {
            return ((IList)_InternalList).Contains(value);
        }

        int IList.IndexOf(object value)
        {
            return ((IList)_InternalList).IndexOf(value);
        }

        void IList.Insert(int index, object value)
        {
            if (index != Count)
            {
                throw new ArgumentOutOfRangeException(nameof(index));
            }

            Add(Cast(value));
        }

        void IList.Remove(object value)
        {
            throw new NotSupportedException();
        }

        void IList.RemoveAt(int index)
        {
            throw new NotSupportedException();
        }

        void ICollection.CopyTo(Array array, int index)
        {
            ((ICollection)_InternalList).CopyTo(array, index);
        }

        #region IEnumerable
        /// <summary>
        /// <!-- see cref="System.Collections.Generic.IEnumerable&lt;&gt;.GetEnumerator" / -->
        /// </summary>
        public IEnumerator<DocumentReference> GetEnumerator()
        {
            return _InternalList.GetEnumerator();
        }

	    IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable<DocumentReference>)this).GetEnumerator();
        }

        #endregion IEnumerable

        #endregion Public Methods

        #region Public Properties

        /// <summary>
        /// Count of Document References in collection
        /// </summary>
        public int Count
        {
            get { return _InternalList.Count; }
        }

        /// <summary>
        /// <!-- see cref="System.Collections.Generic.IList&lt;&gt;.this" / -->
        /// </summary>
        public DocumentReference this[int index]
        {
            get
            {
                return _InternalList[index];
            }
        }

        bool IList.IsFixedSize
        {
            get { return false; }
        }

        bool IList.IsReadOnly
        {
            get { return false; }
        }

        object IList.this[int index]
        {
            get
            {
                return this[index];
            }
            set
            {
                throw new NotSupportedException();
            }
        }

        bool ICollection.IsSynchronized
        {
            get { return false; }
        }

        object ICollection.SyncRoot
        {
            get { return this; }
        }

        #endregion Public Properties

        //--------------------------------------------------------------------
        //
        // Public Events
        //
        //---------------------------------------------------------------------

        #region Public Event

        /// <summary>
        /// Occurs when the collection changes, either by adding or removing an item.
        /// </summary>
        /// <remarks>
        /// see <seealso cref="INotifyCollectionChanged" />
        /// </remarks>
        public event NotifyCollectionChangedEventHandler CollectionChanged;
        #endregion Public Event

        //--------------------------------------------------------------------
        //
        // private Properties
        //
        //---------------------------------------------------------------------

        #region Private Properties

        // Aggregated IList
        private IList<DocumentReference> _InternalList
        {
            get
            {
                if (_internalList == null)
                {
                    _internalList = new List<DocumentReference>();
                }
                return _internalList;
            }
        }
        #endregion Private Properties

        #region Private Methods

        //--------------------------------------------------------------------
        //
        // Private Methods
        //
        //---------------------------------------------------------------------

        private DocumentReference Cast(object value)
        {
            if (value == null)
            {
                throw new ArgumentNullException(nameof(value));
            }

            if (!(value is DocumentReference))
            {
                throw new ArgumentException(SR.Get(SRID.Collection_BadType, nameof(DocumentReferenceCollection), value.GetType().Name, nameof(DocumentReference)));
            }

            return (DocumentReference) value;
        }

        // fire CollectionChanged event to any listeners
        private void OnCollectionChanged(NotifyCollectionChangedAction action, object item, int index)
        {
            if (CollectionChanged != null)
            {
                NotifyCollectionChangedEventArgs args;
                args = new NotifyCollectionChangedEventArgs(action, item, index);

                CollectionChanged(this, args);
            }
        }

        #endregion Private Methods

        //--------------------------------------------------------------------
        //
        // Private Fields
        //
        //---------------------------------------------------------------------

        #region Private Fields
        private List<DocumentReference>  _internalList;
        #endregion Private Fields
    }
}
