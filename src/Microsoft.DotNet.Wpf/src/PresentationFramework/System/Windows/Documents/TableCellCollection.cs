// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

﻿//
// Description: Collection of TableCell objects.
//

using System;
using System.Collections;
using System.Collections.Generic;
using MS.Internal.Documents;

namespace System.Windows.Documents
{
    /// <summary>
    /// A TableCellCollection is an ordered collection of TableCells.
    /// </summary>
    /// <remarks>
    /// TableCellCollection provides public access for TableCells
    /// reading and manipulating.
    /// </remarks>
    public sealed class TableCellCollection : IList<TableCell>, IList
    {
        //------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------

        #region Constructors

        internal TableCellCollection(TableRow owner)
        {
            _cellCollectionInternal = new TableTextElementCollectionInternal<TableRow, TableCell>(owner);
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
            _cellCollectionInternal.CopyTo(array, index);
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
        public void CopyTo(TableCell[] array, int index)
        {
            _cellCollectionInternal.CopyTo(array, index);
        }

        /// <summary>
        ///     <see cref="IEnumerable.GetEnumerator"/>
        /// </summary>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return _cellCollectionInternal.GetEnumerator();
        }

        /// <summary>
        ///     <see cref="IEnumerable&lt;T&gt;.GetEnumerator"/>
        /// </summary>
        IEnumerator<TableCell> IEnumerable<TableCell>.GetEnumerator()
        {
            return ((IEnumerable<TableCell>)_cellCollectionInternal).GetEnumerator();
        }

        /// <summary>
        /// Appends a TableCell to the end of the TableCellCollection.
        /// </summary>
        /// <param name="item">The TableCell to be added to the end of the TableCellCollection.</param>
        /// <returns>The TableCellCollection index at which the TableCell has been added.</returns>
        /// <remarks>Adding a null is prohibited.</remarks>
        /// <exception cref="ArgumentNullException">
        /// If the <c>item</c> value is null.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// If the new child already has a parent.
        /// </exception>
        public void Add(TableCell item)
        {
            _cellCollectionInternal.Add(item);
        }

        /// <summary>
        /// Removes all elements from the TableCellCollection.
        /// </summary>
        /// <remarks>
        /// Count is set to zero. Capacity remains unchanged.
        /// To reset the capacity of the TableCellCollection, call TrimToSize
        /// or set the Capacity property directly.
        /// </remarks>
        public void Clear()
        {
            _cellCollectionInternal.Clear();
        }

        /// <summary>
        /// Determines whether a TableCell is in the TableCellCollection.
        /// </summary>
        /// <param name="item">The TableCell to locate in the TableCellCollection.
        /// The value can be a null reference.</param>
        /// <returns>true if TableCell is found in the TableCellCollection;
        /// otherwise, false.</returns>
        public bool Contains(TableCell item)
        {
            return _cellCollectionInternal.Contains(item);
        }

        /// <summary>
        /// Returns the zero-based index of the TableCell. If the TableCell is not
        /// in the TableCellCollection, -1 is returned.
        /// </summary>
        /// <param name="item">The TableCell to locate in the TableCellCollection.</param>
        public int IndexOf(TableCell item)
        {
            return _cellCollectionInternal.IndexOf(item);
        }

        /// <summary>
        /// Inserts a TableCell into the TableCellCollection at the specified index.
        /// </summary>
        /// <param name="index">The zero-based index at which value should be inserted.</param>
        /// <param name="item">The TableCell to insert. </param>
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
        /// TableCellCollection is increased before the new TableCell is inserted.
        ///
        /// If index is equal to Count, TableCell is added to the
        /// end of TableCellCollection.
        ///
        /// The TableCells that follow the insertion point move down to
        /// accommodate the new TableCell. The indexes of the TableCells that are
        /// moved are also updated.
        /// </remarks>
        public void Insert(int index, TableCell item)
        {
            _cellCollectionInternal.Insert(index, item);
        }

        /// <summary>
        /// Removes the specified TableCell from the TableCellCollection.
        /// </summary>
        /// <param name="item">The TableCell to remove from the TableCellCollection.</param>
        /// <exception cref="ArgumentNullException">
        /// If the <c>item</c> value is null.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// If the specified TableCell is not in this collection.
        /// </exception>
        /// <remarks>
        /// The TableCells that follow the removed TableCell move up to occupy
        /// the vacated spot. The indices of the TableCells that are moved
        /// also updated.
        /// </remarks>
        public bool Remove(TableCell item)
        {
            return _cellCollectionInternal.Remove(item);
        }

        /// <summary>
        /// Removes the TableCell at the specified index.
        /// </summary>
        /// <param name="index">The zero-based index of the TableCell to remove.</param>
        /// <exception cref="ArgumentOutOfRangeException">
        /// <c>index</c> is less than zero
        /// - or -
        /// <c>index</c> is equal or greater than count.
        /// </exception>
        /// <remarks>
        /// The TableCells that follow the removed TableCell move up to occupy
        /// the vacated spot. The indices of the TableCells that are moved
        /// also updated.
        /// </remarks>
        public void RemoveAt(int index)
        {
            _cellCollectionInternal.RemoveAt(index);
        }


        /// <summary>
        /// Removes a range of TableCells from the TableCellCollection.
        /// </summary>
        /// <param name="index">The zero-based index of the range
        /// of TableCells to remove</param>
        /// <param name="count">The number of TableCells to remove.</param>
        /// <exception cref="ArgumentOutOfRangeException">
        /// <c>index</c> is less than zero.
        /// -or-
        /// <c>count</c> is less than zero.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// <c>index</c> and <c>count</c> do not denote a valid range of TableCells in the TableCellCollection.
        /// </exception>
        /// <remarks>
        /// The TableCells that follow the removed TableCells move up to occupy
        /// the vacated spot. The indices of the TableCells that are moved are
        /// also updated.
        /// </remarks>
        public void RemoveRange(int index, int count)
        {
            _cellCollectionInternal.RemoveRange(index, count);
        }

        /// <summary>
        /// Sets the capacity to the actual number of elements in the TableCellCollection.
        /// </summary>
        /// <remarks>
        /// This method can be used to minimize a TableCellCollection's memory overhead
        /// if no new elements will be added to the collection.
        ///
        /// To completely clear all elements in a TableCellCollection, call the Clear method
        /// before calling TrimToSize.
        /// </remarks>
        public void TrimToSize()
        {
            _cellCollectionInternal.TrimToSize();
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
            return ((IList)_cellCollectionInternal).Add(value);
        }

        void IList.Clear()
        {
            this.Clear();
        }

        bool IList.Contains(object value)
        {
            return ((IList)_cellCollectionInternal).Contains(value);
        }

        int IList.IndexOf(object value)
        {
            return ((IList)_cellCollectionInternal).IndexOf(value);
        }

        void IList.Insert(int index, object value)
        {
            ((IList)_cellCollectionInternal).Insert(index, value);
        }

        bool IList.IsFixedSize
        {
            get
            {
                return ((IList)_cellCollectionInternal).IsFixedSize;
            }
        }

        bool IList.IsReadOnly
        {
            get
            {
                return ((IList)_cellCollectionInternal).IsReadOnly;
            }
        }

        void IList.Remove(object value)
        {
            ((IList)_cellCollectionInternal).Remove(value);
        }

        void IList.RemoveAt(int index)
        {
            ((IList)_cellCollectionInternal).RemoveAt(index);
        }

        object IList.this[int index]
        {
            get
            {
                return ((IList)_cellCollectionInternal)[index];
            }

            set
            {
                ((IList)_cellCollectionInternal)[index] = value;
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
                return _cellCollectionInternal.Count;
            }
        }

        /// <summary>
        ///     <see cref="IList.IsReadOnly"/>
        /// </summary>
        public bool IsReadOnly
        {
            get
            {
                return ((IList)_cellCollectionInternal).IsReadOnly;
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
                return ((IList)_cellCollectionInternal).IsSynchronized;
            }
        }

        /// <summary>
        /// <see cref="ICollection.SyncRoot"/>
        /// </summary>
        public object SyncRoot
        {
            get
            {
                return ((IList)_cellCollectionInternal).SyncRoot;
            }
        }

        /// <summary>
        /// Gets or sets the number of elements that the TableRowGroupCollection can contain.
        /// </summary>
        /// <value>
        /// The number of elements that the TableRowGroupCollection can contain.
        /// </value>
        /// <remarks>
        /// Capacity is the number of elements that the TableRowGroupCollection is capable of storing.
        /// Count is the number of Visuals that are actually in the TableRowGroupCollection.
        ///
        /// Capacity is always greater than or equal to Count. If Count exceeds
        /// Capacity while adding elements, the capacity of the TableRowGroupCollection is increased.
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
                return _cellCollectionInternal.Capacity;
            }
            set
            {
                _cellCollectionInternal.Capacity = value;
            }
        }

        /// <summary>
        /// Indexer for the TableCellCollection. Gets the TableCell stored at the
        /// zero-based index of the TableCellCollection.
        /// </summary>
        /// <remarks>This property provides the ability to access a specific TableCell in the
        /// TableCellCollection by using the following systax: <c>TableCell myTableCell = myTableCellCollection[index]</c>.
        /// </remarks>
        /// <exception cref="ArgumentOutOfRangeException">
        /// <c>index</c> is less than zero -or- <c>index</c> is equal to or greater than Count.
        /// </exception>
        public TableCell this[int index]
        {
            get
            {
                return _cellCollectionInternal[index];
            }
            set
            {
                _cellCollectionInternal[index] = value;
            }
        }

        #endregion Public Properties

        #region Internal Methods

        /// <summary>
        /// Performs the actual work of adding the item into the array, and notifying it when it is connected
        /// </summary>
        internal void InternalAdd(TableCell item)
        {
            _cellCollectionInternal.InternalAdd(item);
        }

        /// <summary>
        /// Performs the actual work of notifying item it is leaving the array, and disconnecting it.
        /// </summary>
        internal void InternalRemove(TableCell item)
        {
            _cellCollectionInternal.InternalRemove(item);
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
            _cellCollectionInternal.EnsureCapacity(min);
        }

        /// <summary>
        /// Sets the specified TableCell at the specified index;
        /// Connects the item to the model tree;
        /// Notifies the TableCell about the event.
        /// </summary>
        /// <exception cref="ArgumentException">
        /// If the new item has already a parent or if the slot at the specified index is not null.
        /// </exception>
        /// <remarks>
        /// Note that the function requires that _item[index] == null and
        /// it also requires that the passed in item is not included into another TableCellCollection.
        /// </remarks>
        private void PrivateConnectChild(int index, TableCell item)
        {
            _cellCollectionInternal.PrivateConnectChild(index, item);
        }


        /// <summary>
        /// Removes specified TableCell from the TableCellCollection.
        /// </summary>
        /// <param name="item">TableCell to remove.</param>
        private void PrivateDisconnectChild(TableCell item)
        {
            _cellCollectionInternal.PrivateDisconnectChild(item);
        }

        // helper method: return true if the item belongs to the collection's owner
        private bool BelongsToOwner(TableCell item)
        {
            return _cellCollectionInternal.BelongsToOwner(item);
        }

        // Helper method - Searches the children collection for the index an item currently exists at -
        // NOTE - ITEM MUST BE IN TEXT TREE WHEN THIS IS CALLED.        
        private int FindInsertionIndex(TableCell item)
        {
            return _cellCollectionInternal.FindInsertionIndex(item);
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
                return _cellCollectionInternal.PrivateCapacity;
            }
            set
            {
                _cellCollectionInternal.PrivateCapacity = value;
            }
        }

        #endregion Private Properties

        private TableTextElementCollectionInternal<TableRow, TableCell> _cellCollectionInternal;
    }
}
