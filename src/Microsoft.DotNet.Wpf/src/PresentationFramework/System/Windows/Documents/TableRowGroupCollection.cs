// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

﻿//
// Description: Collection of TableRowGroup objects.
//

using System;
using System.Collections;
using System.Collections.Generic;
using MS.Internal.Documents;

namespace System.Windows.Documents
{
    /// <summary>
    /// A TableRowGroupCollection is an ordered collection of TableRowGroups.
    /// </summary>
    /// <remarks>
    /// TableRowGroupCollection provides public access for TableRowGroups
    /// reading and manipulating.
    /// </remarks>
    public sealed class TableRowGroupCollection : IList<TableRowGroup>, IList
    {
        //------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------

        #region Constructors

        internal TableRowGroupCollection(Table owner)
        {
            _rowGroupCollectionInternal = new TableTextElementCollectionInternal<Table, TableRowGroup>(owner);
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
            _rowGroupCollectionInternal.CopyTo(array, index);
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
        public void CopyTo(TableRowGroup[] array, int index)
        {
            _rowGroupCollectionInternal.CopyTo(array, index);
        }

        /// <summary>
        ///     <see cref="IEnumerable.GetEnumerator"/>
        /// </summary>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return _rowGroupCollectionInternal.GetEnumerator();
        }

        /// <summary>
        ///     <see cref="IEnumerable&lt;T&gt;.GetEnumerator"/>
        /// </summary>
        IEnumerator<TableRowGroup> IEnumerable<TableRowGroup>.GetEnumerator()
        {
            return ((IEnumerable<TableRowGroup>)_rowGroupCollectionInternal).GetEnumerator();
        }

        /// <summary>
        /// Appends a TableRowGroup to the end of the TableRowGroupCollection.
        /// </summary>
        /// <param name="item">The TableRowGroup to be added to the end of the TableRowGroupCollection.</param>
        /// <returns>The TableRowGroupCollection index at which the TableRowGroup has been added.</returns>
        /// <remarks>Adding a null is prohibited.</remarks>
        /// <exception cref="ArgumentNullException">
        /// If the <c>item</c> value is null.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// If the new child already has a parent.
        /// </exception>
        public void Add(TableRowGroup item)
        {
            _rowGroupCollectionInternal.Add(item);
        }

        /// <summary>
        /// Removes all elements from the TableRowGroupCollection.
        /// </summary>
        /// <remarks>
        /// Count is set to zero. Capacity remains unchanged.
        /// To reset the capacity of the TableRowGroupCollection, call TrimToSize
        /// or set the Capacity property directly.
        /// </remarks>
        public void Clear()
        {
            _rowGroupCollectionInternal.Clear();
        }

        /// <summary>
        /// Determines whether a TableRowGroup is in the TableRowGroupCollection.
        /// </summary>
        /// <param name="item">The TableRowGroup to locate in the TableRowGroupCollection.
        /// The value can be a null reference.</param>
        /// <returns>true if TableRowGroup is found in the TableRowGroupCollection;
        /// otherwise, false.</returns>
        public bool Contains(TableRowGroup item)
        {
            return _rowGroupCollectionInternal.Contains(item);
        }

        /// <summary>
        /// Returns the zero-based index of the TableRowGroup. If the TableRowGroup is not
        /// in the TableRowGroupCollection, -1 is returned.
        /// </summary>
        /// <param name="item">The TableRowGroup to locate in the TableRowGroupCollection.</param>
        public int IndexOf(TableRowGroup item)
        {
            return _rowGroupCollectionInternal.IndexOf(item);
        }

        /// <summary>
        /// Inserts a TableRowGroup into the TableRowGroupCollection at the specified index.
        /// </summary>
        /// <param name="index">The zero-based index at which value should be inserted.</param>
        /// <param name="item">The TableRowGroup to insert. </param>
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
        /// TableRowGroupCollection is increased before the new TableRowGroup is inserted.
        ///
        /// If index is equal to Count, TableRowGroup is added to the
        /// end of TableRowGroupCollection.
        ///
        /// The TableRowGroups that follow the insertion point move down to
        /// accommodate the new TableRowGroup. The indexes of the TableRowGroups that are
        /// moved are also updated.
        /// </remarks>
        public void Insert(int index, TableRowGroup item)
        {
            _rowGroupCollectionInternal.Insert(index, item);
        }

        /// <summary>
        /// Removes the specified TableRowGroup from the TableRowGroupCollection.
        /// </summary>
        /// <param name="item">The TableRowGroup to remove from the TableRowGroupCollection.</param>
        /// <exception cref="ArgumentNullException">
        /// If the <c>item</c> value is null.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// If the specified TableRowGroup is not in this collection.
        /// </exception>
        /// <remarks>
        /// The TableRowGroups that follow the removed TableRowGroup move up to occupy
        /// the vacated spot. The indices of the TableRowGroups that are moved
        /// also updated.
        /// </remarks>
        public bool Remove(TableRowGroup item)
        {
            return _rowGroupCollectionInternal.Remove(item);
        }

        /// <summary>
        /// Removes the TableRowGroup at the specified index.
        /// </summary>
        /// <param name="index">The zero-based index of the TableRowGroup to remove.</param>
        /// <exception cref="ArgumentOutOfRangeException">
        /// <c>index</c> is less than zero
        /// - or -
        /// <c>index</c> is equal or greater than count.
        /// </exception>
        /// <remarks>
        /// The TableRowGroups that follow the removed TableRowGroup move up to occupy
        /// the vacated spot. The indices of the TableRowGroups that are moved
        /// also updated.
        /// </remarks>
        public void RemoveAt(int index)
        {
            _rowGroupCollectionInternal.RemoveAt(index);
        }


        /// <summary>
        /// Removes a range of TableRowGroups from the TableRowGroupCollection.
        /// </summary>
        /// <param name="index">The zero-based index of the range
        /// of TableRowGroups to remove</param>
        /// <param name="count">The number of TableRowGroups to remove.</param>
        /// <exception cref="ArgumentOutOfRangeException">
        /// <c>index</c> is less than zero.
        /// -or-
        /// <c>count</c> is less than zero.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// <c>index</c> and <c>count</c> do not denote a valid range of TableRowGroups in the TableRowGroupCollection.
        /// </exception>
        /// <remarks>
        /// The TableRowGroups that follow the removed TableRowGroups move up to occupy
        /// the vacated spot. The indices of the TableRowGroups that are moved are
        /// also updated.
        /// </remarks>
        public void RemoveRange(int index, int count)
        {
            _rowGroupCollectionInternal.RemoveRange(index, count);
        }

        /// <summary>
        /// Sets the capacity to the actual number of elements in the TableRowGroupCollection.
        /// </summary>
        /// <remarks>
        /// This method can be used to minimize a TableRowGroupCollection's memory overhead
        /// if no new elements will be added to the collection.
        ///
        /// To completely clear all elements in a TableRowGroupCollection, call the Clear method
        /// before calling TrimToSize.
        /// </remarks>
        public void TrimToSize()
        {
            _rowGroupCollectionInternal.TrimToSize();
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
            return ((IList)_rowGroupCollectionInternal).Add(value);
        }

        void IList.Clear()
        {
            this.Clear();
        }

        bool IList.Contains(object value)
        {
            return ((IList)_rowGroupCollectionInternal).Contains(value);
        }

        int IList.IndexOf(object value)
        {
            return ((IList)_rowGroupCollectionInternal).IndexOf(value);
        }

        void IList.Insert(int index, object value)
        {
            ((IList)_rowGroupCollectionInternal).Insert(index, value);
        }

        bool IList.IsFixedSize
        {
            get
            {
                return ((IList)_rowGroupCollectionInternal).IsFixedSize;
            }
        }

        bool IList.IsReadOnly
        {
            get
            {
                return ((IList)_rowGroupCollectionInternal).IsReadOnly;
            }
        }

        void IList.Remove(object value)
        {
            ((IList)_rowGroupCollectionInternal).Remove(value);
        }

        void IList.RemoveAt(int index)
        {
            ((IList)_rowGroupCollectionInternal).RemoveAt(index);
        }

        object IList.this[int index]
        {
            get
            {
                return ((IList)_rowGroupCollectionInternal)[index];
            }

            set
            {
                ((IList)_rowGroupCollectionInternal)[index] = value;
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
                return _rowGroupCollectionInternal.Count;
            }
        }

        /// <summary>
        ///     <see cref="IList.IsReadOnly"/>
        /// </summary>
        public bool IsReadOnly
        {
            get
            {
                return _rowGroupCollectionInternal.IsReadOnly;
            }
        }

        /// <summary>
        /// <see cref="ICollection.IsSynchronized"/>
        /// <remarks>
        /// </remarks>
        /// </summary>
        public bool IsSynchronized
        {
            get
            {
                return _rowGroupCollectionInternal.IsSynchronized;
            }
        }

        /// <summary>
        /// <see cref="ICollection.SyncRoot"/>
        /// </summary>
        public object SyncRoot
        {
            get
            {
                return _rowGroupCollectionInternal.SyncRoot;
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
                return _rowGroupCollectionInternal.Capacity;
            }
            set
            {
                _rowGroupCollectionInternal.Capacity = value;
            }
        }

        /// <summary>
        /// Indexer for the TableRowGroupCollection. Gets the TableRowGroup stored at the
        /// zero-based index of the TableRowGroupCollection.
        /// </summary>
        /// <remarks>This property provides the ability to access a specific TableRowGroup in the
        /// TableRowGroupCollection by using the following systax: <c>TableRowGroup myTableRowGroup = myTableRowGroupCollection[index]</c>.
        /// </remarks>
        /// <exception cref="ArgumentOutOfRangeException">
        /// <c>index</c> is less than zero -or- <c>index</c> is equal to or greater than Count.
        /// </exception>
        public TableRowGroup this[int index]
        {
            get
            {
                return _rowGroupCollectionInternal[index];
            }
            set
            {
                _rowGroupCollectionInternal[index] = value;
            }
        }

        #endregion Public Properties

        #region Internal Methods

        /// <summary>
        /// Performs the actual work of adding the item into the array, and notifying it when it is connected
        /// </summary>
        internal void InternalAdd(TableRowGroup item)
        {
            _rowGroupCollectionInternal.InternalAdd(item);
        }

        /// <summary>
        /// Performs the actual work of notifying item it is leaving the array, and disconnecting it.
        /// </summary>
        internal void InternalRemove(TableRowGroup item)
        {
            _rowGroupCollectionInternal.InternalRemove(item);
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
            _rowGroupCollectionInternal.EnsureCapacity(min);
        }

        /// <summary>
        /// Sets the specified TableRowGroup at the specified index;
        /// Connects the item to the model tree;
        /// Notifies the TableRowGroup about the event.
        /// </summary>
        /// <exception cref="ArgumentException">
        /// If the new item has already a parent or if the slot at the specified index is not null.
        /// </exception>
        /// <remarks>
        /// Note that the function requires that _item[index] == null and
        /// it also requires that the passed in item is not included into another TableRowGroupCollection.
        /// </remarks>
        private void PrivateConnectChild(int index, TableRowGroup item)
        {
            _rowGroupCollectionInternal.PrivateConnectChild(index, item);
        }


        /// <summary>
        /// Removes specified TableRowGroup from the TableRowGroupCollection.
        /// </summary>
        /// <param name="item">TableRowGroup to remove.</param>
        private void PrivateDisconnectChild(TableRowGroup item)
        {
            _rowGroupCollectionInternal.PrivateDisconnectChild(item);
        }

        // helper method: return true if the item belongs to the collection's owner
        private bool BelongsToOwner(TableRowGroup item)
        {
            return _rowGroupCollectionInternal.BelongsToOwner(item);
        }

        // Helper method - Searches the children collection for the index an item currently exists at -
        // NOTE - ITEM MUST BE IN TEXT TREE WHEN THIS IS CALLED.        
        private int FindInsertionIndex(TableRowGroup item)
        {
            return _rowGroupCollectionInternal.FindInsertionIndex(item);
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
                return _rowGroupCollectionInternal.PrivateCapacity;
            }
            set
            {
                _rowGroupCollectionInternal.PrivateCapacity = value;
            }
        }

        #endregion Private Properties

        private TableTextElementCollectionInternal<Table, TableRowGroup> _rowGroupCollectionInternal;
    }
}
