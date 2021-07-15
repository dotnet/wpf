// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

﻿//
// Description: Collection of TableRow objects.
//

using System;
using System.Collections;
using System.Collections.Generic;
using MS.Internal.Documents;

namespace System.Windows.Documents
{
    /// <summary>
    /// A TableRowCollection is an ordered collection of TableRows.
    /// </summary>
    /// <remarks>
    /// TableRowCollection provides public access for TableRow
    /// reading and manipulating.
    /// </remarks>
    public sealed class TableRowCollection : IList<TableRow>, IList
    {
        //------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------

        #region Constructors

        internal TableRowCollection(TableRowGroup owner)
        {
            _rowCollectionInternal = new TableTextElementCollectionInternal<TableRowGroup, TableRow>(owner);
        }

        #endregion Constructors

        //------------------------------------------------------
        //
        //  Public Methods
        //
        //------------------------------------------------------

        #region Public Methods

        /// <summary>
        /// <see cref="ICollection.CopyTo"/>
        /// </summary>
        /// <exception cref="ArgumentNullException">
        /// <see cref="ICollection.CopyTo"/>
        /// </exception>
        /// <exception cref="ArgumentException">
        /// <see cref="ICollection.CopyTo"/>
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// <see cref="ICollection.CopyTo"/>
        /// </exception>
        /// <param name="array"><see cref="ICollection.CopyTo"/></param>
        /// <param name="index"><see cref="ICollection.CopyTo"/></param>
        public void CopyTo(Array array, int index)
        {
            _rowCollectionInternal.CopyTo(array, index);
        }

        /// <summary>
        /// Strongly typed version of ICollection.CopyTo.
        /// </summary>
        /// <exception cref="ArgumentNullException">
        /// <see cref="ICollection.CopyTo"/>
        /// </exception>
        /// <exception cref="ArgumentException">
        /// <see cref="ICollection.CopyTo"/>
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// <see cref="ICollection.CopyTo"/>
        /// </exception>
        /// <param name="array"><see cref="ICollection.CopyTo"/></param>
        /// <param name="index"><see cref="ICollection.CopyTo"/></param>
        /// <remarks>
        /// <see cref="ICollection.CopyTo"/>
        /// </remarks>
        public void CopyTo(TableRow[] array, int index)
        {
            _rowCollectionInternal.CopyTo(array, index);
        }

        /// <summary>
        ///     <see cref="IEnumerable.GetEnumerator"/>
        /// </summary>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return _rowCollectionInternal.GetEnumerator();
        }

        /// <summary>
        ///     <see cref="IEnumerable&lt;T&gt;.GetEnumerator"/>
        /// </summary>
        IEnumerator<TableRow> IEnumerable<TableRow>.GetEnumerator()
        {
            return ((IEnumerable<TableRow>)_rowCollectionInternal).GetEnumerator();
        }

        /// <summary>
        /// Appends a TableRow to the end of the TableRowCollection.
        /// </summary>
        /// <param name="item">The TableRow to be added to the end of the TableRowCollection.</param>
        /// <returns>The TableRowCollection index at which the TableRow has been added.</returns>
        /// <remarks>Adding a null is prohibited.</remarks>
        /// <exception cref="ArgumentNullException">
        /// If the <c>item</c> value is null.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// If the new child already has a parent.
        /// </exception>
        public void Add(TableRow item)
        {
            _rowCollectionInternal.Add(item);
        }

        /// <summary>
        /// Removes all elements from the TableRowCollection.
        /// </summary>
        /// <remarks>
        /// Count is set to zero. Capacity remains unchanged.
        /// To reset the capacity of the TableRowCollection, call TrimToSize
        /// or set the Capacity property directly.
        /// </remarks>
        public void Clear()
        {
            _rowCollectionInternal.Clear();
        }

        /// <summary>
        /// Determines whether a TableRow is in the TableRowCollection.
        /// </summary>
        /// <param name="item">The TableRow to locate in the TableRowCollection.
        /// The value can be a null reference.</param>
        /// <returns>true if TableRow is found in the TableRowCollection;
        /// otherwise, false.</returns>
        public bool Contains(TableRow item)
        {
            return _rowCollectionInternal.Contains(item);
        }

        /// <summary>
        /// Returns the zero-based index of the TableRow. If the TableRow is not
        /// in the TableRowCollection, -1 is returned.
        /// </summary>
        /// <param name="item">The TableRow to locate in the TableRowCollection.</param>
        public int IndexOf(TableRow item)
        {
            return _rowCollectionInternal.IndexOf(item);
        }

        /// <summary>
        /// Inserts a TableRow into the TableRowCollection at the specified index.
        /// </summary>
        /// <param name="index">The zero-based index at which value should be inserted.</param>
        /// <param name="item">The TableRow to insert. </param>
        /// <exception cref="ArgumentOutOfRangeException">
        /// <c>index</c>c> is less than zero.
        /// -or-
        /// <c>index</c> is greater than Count.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        /// If the <c>item</c> value is null.
        /// </exception>
        /// <remarks>
        /// If Count already equals Capacity, the capacity of the
        /// TableRowCollection is increased before the new TableRow is inserted.
        ///
        /// If index is equal to Count, TableRow is added to the
        /// end of TableRowCollection.
        ///
        /// The TableRows that follow the insertion point move down to
        /// accommodate the new TableRow. The indexes of the TableRows that are
        /// moved are also updated.
        /// </remarks>
        public void Insert(int index, TableRow item)
        {
            _rowCollectionInternal.Insert(index, item);
        }

        /// <summary>
        /// Removes the specified TableRow from the TableRowCollection.
        /// </summary>
        /// <param name="item">The TableRow to remove from the TableRowCollection.</param>
        /// <exception cref="ArgumentNullException">
        /// If the <c>item</c> value is null.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// If the specified TableRow is not in this collection.
        /// </exception>
        /// <remarks>
        /// The TableRows that follow the removed TableRow move up to occupy
        /// the vacated spot. The indices of the TableRows that are moved
        /// also updated.
        /// </remarks>
        public bool Remove(TableRow item)
        {
            return _rowCollectionInternal.Remove(item);
        }

        /// <summary>
        /// Removes the TableRow at the specified index.
        /// </summary>
        /// <param name="index">The zero-based index of the TableRow to remove.</param>
        /// <exception cref="ArgumentOutOfRangeException">
        /// <c>index</c> is less than zero
        /// - or -
        /// <c>index</c> is equal or greater than count.
        /// </exception>
        /// <remarks>
        /// The TableRows that follow the removed TableRow move up to occupy
        /// the vacated spot. The indices of the TableRows that are moved
        /// also updated.
        /// </remarks>
        public void RemoveAt(int index)
        {
            _rowCollectionInternal.RemoveAt(index);
        }


        /// <summary>
        /// Removes a range of TableRows from the TableRowCollection.
        /// </summary>
        /// <param name="index">The zero-based index of the range
        /// of TableRows to remove</param>
        /// <param name="count">The number of TableRows to remove.</param>
        /// <exception cref="ArgumentOutOfRangeException">
        /// <c>index</c> is less than zero.
        /// -or-
        /// <c>count</c> is less than zero.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// <c>index</c> and <c>count</c> do not denote a valid range of TableRows in the TableRowCollection.
        /// </exception>
        /// <remarks>
        /// The TableRows that follow the removed TableRows move up to occupy
        /// the vacated spot. The indices of the TableRows that are moved are
        /// also updated.
        /// </remarks>
        public void RemoveRange(int index, int count)
        {
            _rowCollectionInternal.RemoveRange(index, count);
        }

        /// <summary>
        /// Sets the capacity to the actual number of elements in the TableRowCollection.
        /// </summary>
        /// <remarks>
        /// This method can be used to minimize a TableRowCollection's memory overhead
        /// if no new elements will be added to the collection.
        ///
        /// To completely clear all elements in a TableRowCollection, call the Clear method
        /// before calling TrimToSize.
        /// </remarks>
        public void TrimToSize()
        {
            _rowCollectionInternal.TrimToSize();
        }

        #endregion Public Methods

        //-------------------------------------------------------------------
        //
        //  IList Members
        //
        //-------------------------------------------------------------------

        #region IList Members

        int IList.Add(object value)
        {
            return ((IList)_rowCollectionInternal).Add(value);
        }

        void IList.Clear()
        {
            this.Clear();
        }

        bool IList.Contains(object value)
        {
            return ((IList)_rowCollectionInternal).Contains(value);
        }

        int IList.IndexOf(object value)
        {
            return ((IList)_rowCollectionInternal).IndexOf(value);
        }

        void IList.Insert(int index, object value)
        {
            ((IList)_rowCollectionInternal).Insert(index, value);
        }

        bool IList.IsFixedSize
        {
            get
            {
                return ((IList)_rowCollectionInternal).IsFixedSize;
            }
        }

        bool IList.IsReadOnly
        {
            get
            {
                return ((IList)_rowCollectionInternal).IsReadOnly;
            }
        }

        void IList.Remove(object value)
        {
            ((IList)_rowCollectionInternal).Remove(value);
        }

        void IList.RemoveAt(int index)
        {
            ((IList)_rowCollectionInternal).RemoveAt(index);
        }

        object IList.this[int index]
        {
            get
            {
                return ((IList)_rowCollectionInternal)[index];
            }

            set
            {
                ((IList)_rowCollectionInternal)[index] = value;
            }
        }

        #endregion IList Members


        //------------------------------------------------------
        //
        //  Public Properties
        //
        //------------------------------------------------------

        #region Public Properties

        /// <summary>
        /// <see cref="ICollection.Count"/>
        /// </summary>
        public int Count
        {
            get
            {
                return _rowCollectionInternal.Count;
            }
        }

        /// <summary>
        ///     <see cref="IList.IsReadOnly"/>
        /// </summary>
        public bool IsReadOnly
        {
            get
            {
                return ((IList)_rowCollectionInternal).IsReadOnly;
            }
        }

        /// <summary>
        /// <see cref="ICollection.IsSynchronized"/>
        /// <remarks>
        /// Always returns false.
        /// </remarks>
        /// </summary>
        public bool IsSynchronized
        {
            get
            {
                return ((IList)_rowCollectionInternal).IsSynchronized;
            }
        }

        /// <summary>
        /// <see cref="ICollection.SyncRoot"/>
        /// </summary>
        public object SyncRoot
        {
            get
            {
                return ((IList)_rowCollectionInternal).SyncRoot;
            }
        }

        /// <summary>
        /// Gets or sets the number of elements that the TableRowCollection can contain.
        /// </summary>
        /// <value>
        /// The number of elements that the TableRowCollection can contain.
        /// </value>
        /// <remarks>
        /// Capacity is the number of elements that the TableRowCollection is capable of storing.
        /// Count is the number of Visuals that are actually in the TableRowCollection.
        ///
        /// Capacity is always greater than or equal to Count. If Count exceeds
        /// Capacity while adding elements, the capacity of the TableRowCollection is increased.
        ///
        /// By default the capacity is 8.
        /// </remarks>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Capacity is set to a value that is less than Count.
        /// </exception>
        /// <ExternalAPI/>
        public int Capacity
        {
            get
            {
                return _rowCollectionInternal.Capacity;
            }
            set
            {
                _rowCollectionInternal.Capacity = value;
            }
        }

        /// <summary>
        /// Indexer for the TableRowCollection. Gets the TableRow stored at the
        /// zero-based index of the TableRowCollection.
        /// </summary>
        /// <remarks>This property provides the ability to access a specific TableRow in the
        /// TableRowCollection by using the following systax: <c>TableRow myTableRow = myTableRowCollection[index]</c>.
        /// </remarks>
        /// <exception cref="ArgumentOutOfRangeException">
        /// <c>index</c> is less than zero -or- <c>index</c> is equal to or greater than Count.
        /// </exception>
        public TableRow this[int index]
        {
            get
            {
                return _rowCollectionInternal[index];
            }
            set
            {
                _rowCollectionInternal[index] = value;
            }
        }

        #endregion Public Properties

        #region Internal Methods

        /// <summary>
        /// Performs the actual work of adding the item into the array, and notifying it when it is connected
        /// </summary>
        internal void InternalAdd(TableRow item)
        {
            _rowCollectionInternal.InternalAdd(item);
        }

        /// <summary>
        /// Performs the actual work of notifying item it is leaving the array, and disconnecting it.
        /// </summary>
        internal void InternalRemove(TableRow item)
        {
            _rowCollectionInternal.InternalRemove(item);
        }

        #endregion Internal Methods

        //------------------------------------------------------
        //
        //  Private Methods
        //
        //------------------------------------------------------

        #region Private Methods

        /// <summary>
        /// Ensures that the capacity of this list is at least the given minimum
        /// value. If the currect capacity of the list is less than min, the
        /// capacity is increased to min.
        /// </summary>
        private void EnsureCapacity(int min)
        {
            _rowCollectionInternal.EnsureCapacity(min);
        }

        /// <summary>
        /// Sets the specified TableRow at the specified index;
        /// Connects the item to the model tree;
        /// Notifies the TableRow about the event.
        /// </summary>
        /// <exception cref="ArgumentException">
        /// If the new item has already a parent or if the slot at the specified index is not null.
        /// </exception>
        /// <remarks>
        /// Note that the function requires that _item[index] == null and
        /// it also requires that the passed in item is not included into another TableRowCollection.
        /// </remarks>
        private void PrivateConnectChild(int index, TableRow item)
        {
            _rowCollectionInternal.PrivateConnectChild(index, item);
        }


        /// <summary>
        /// Removes specified TableRow from the TableRowCollection.
        /// </summary>
        /// <param name="item">TableRow to remove.</param>
        private void PrivateDisconnectChild(TableRow item)
        {
            _rowCollectionInternal.PrivateDisconnectChild(item);
        }

        // helper method: return true if the item belongs to the collection's owner
        private bool BelongsToOwner(TableRow item)
        {
            return _rowCollectionInternal.BelongsToOwner(item);
        }

        // Helper method - Searches the children collection for the index an item currently exists at -
        // NOTE - ITEM MUST BE IN TEXT TREE WHEN THIS IS CALLED.        
        private int FindInsertionIndex(TableRow item)
        {
            return _rowCollectionInternal.FindInsertionIndex(item);
        }


        #endregion Private Methods

        //------------------------------------------------------
        //
        //  Private Properties
        //
        //------------------------------------------------------

        #region Private Properties

        /// <summary>
        /// PrivateCapacity sets/gets the Capacity of the collection.
        /// </summary>
        private int PrivateCapacity
        {
            get
            {
                return _rowCollectionInternal.PrivateCapacity;
            }
            set
            {
                _rowCollectionInternal.PrivateCapacity = value;
            }
        }

        #endregion Private Properties

        private TableTextElementCollectionInternal<TableRowGroup, TableRow> _rowCollectionInternal;
    }
}
