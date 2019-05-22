// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

﻿//
// Description: Collection of TableColumn objects.
//

using System;
using System.Collections;
using System.Collections.Generic;
using MS.Internal.Documents;

namespace System.Windows.Documents
{
    /// <summary>
    /// A TableColumnCollection is an ordered collection of TableColumns.
    /// </summary>
    /// <remarks>
    /// TableColumnCollection provides public access for TableColumns
    /// reading and manipulating.
    /// </remarks>
    public sealed class TableColumnCollection : IList<TableColumn>, IList
    {
        //------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------

        #region Constructors

        internal TableColumnCollection(Table owner)
        {
            _columnCollection = new TableColumnCollectionInternal(owner);
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
            _columnCollection.CopyTo(array, index);
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
        public void CopyTo(TableColumn[] array, int index)
        {
            _columnCollection.CopyTo(array, index);
        }

        /// <summary>
        ///     <see cref="IEnumerable.GetEnumerator"/>
        /// </summary>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return _columnCollection.GetEnumerator();
        }

        /// <summary>
        ///     <see cref="IEnumerable&lt;T&gt;.GetEnumerator"/>
        /// </summary>
        IEnumerator<TableColumn> IEnumerable<TableColumn>.GetEnumerator()
        {
            return ((IEnumerable<TableColumn>)_columnCollection).GetEnumerator();
        }

        /// <summary>
        /// Appends a TableColumn to the end of the TableColumnCollection.
        /// </summary>
        /// <param name="item">The TableColumn to be added to the end of the TableColumnCollection.</param>
        /// <returns>The TableColumnCollection index at which the TableColumn has been added.</returns>
        /// <remarks>Adding a null is prohibited.</remarks>
        /// <exception cref="ArgumentNullException">
        /// If the <c>item</c> value is null.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// If the new child already has a parent.
        /// </exception>
        public void Add(TableColumn item)
        {
            _columnCollection.Add(item);
        }

        /// <summary>
        /// Removes all elements from the TableColumnCollection.
        /// </summary>
        /// <remarks>
        /// Count is set to zero. Capacity remains unchanged.
        /// To reset the capacity of the TableColumnCollection, call TrimToSize
        /// or set the Capacity property directly.
        /// </remarks>
        public void Clear()
        {
            _columnCollection.Clear();
        }

        /// <summary>
        /// Determines whether a TableColumn is in the TableColumnCollection.
        /// </summary>
        /// <param name="item">The TableColumn to locate in the TableColumnCollection.
        /// The value can be a null reference.</param>
        /// <returns>true if TableColumn is found in the TableColumnCollection;
        /// otherwise, false.</returns>
        public bool Contains(TableColumn item)
        {
            return _columnCollection.Contains(item);
        }

        /// <summary>
        /// Returns the zero-based index of the TableColumn. If the TableColumn is not
        /// in the TableColumnCollection, -1 is returned.
        /// </summary>
        /// <param name="item">The TableColumn to locate in the TableColumnCollection.</param>
        public int IndexOf(TableColumn item)
        {
            return _columnCollection.IndexOf(item);
        }

        /// <summary>
        /// Inserts a TableColumn into the TableColumnCollection at the specified index.
        /// </summary>
        /// <param name="index">The zero-based index at which value should be inserted.</param>
        /// <param name="item">The TableColumn to insert. </param>
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
        /// TableColumnCollection is increased before the new TableColumn is inserted.
        ///
        /// If index is equal to Count, TableColumn is added to the
        /// end of TableColumnCollection.
        ///
        /// The TableColumns that follow the insertion point move down to
        /// accommodate the new TableColumn. The indexes of the TableColumns that are
        /// moved are also updated.
        /// </remarks>
        public void Insert(int index, TableColumn item)
        {
            _columnCollection.Insert(index, item);
        }

        /// <summary>
        /// Removes the specified TableColumn from the TableColumnCollection.
        /// </summary>
        /// <param name="item">The TableColumn to remove from the TableColumnCollection.</param>
        /// <exception cref="ArgumentNullException">
        /// If the <c>item</c> value is null.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// If the specified TableColumn is not in this collection.
        /// </exception>
        /// <remarks>
        /// The TableColumns that follow the removed TableColumn move up to occupy
        /// the vacated spot. The indices of the TableColumns that are moved
        /// also updated.
        /// </remarks>
        public bool Remove(TableColumn item)
        {
            return _columnCollection.Remove(item);
        }

        /// <summary>
        /// Removes the TableColumn at the specified index.
        /// </summary>
        /// <param name="index">The zero-based index of the TableColumn to remove.</param>
        /// <exception cref="ArgumentOutOfRangeException">
        /// <c>index</c> is less than zero
        /// - or -
        /// <c>index</c> is equal or greater than count.
        /// </exception>
        /// <remarks>
        /// The TableColumns that follow the removed TableColumn move up to occupy
        /// the vacated spot. The indices of the TableColumns that are moved
        /// also updated.
        /// </remarks>
        public void RemoveAt(int index)
        {
            _columnCollection.RemoveAt(index);
        }


        /// <summary>
        /// Removes a range of TableColumns from the TableColumnCollection.
        /// </summary>
        /// <param name="index">The zero-based index of the range
        /// of TableColumns to remove</param>
        /// <param name="count">The number of TableColumns to remove.</param>
        /// <exception cref="ArgumentOutOfRangeException">
        /// <c>index</c> is less than zero.
        /// -or-
        /// <c>count</c> is less than zero.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// <c>index</c> and <c>count</c> do not denote a valid range of TableColumns in the TableColumnCollection.
        /// </exception>
        /// <remarks>
        /// The TableColumns that follow the removed TableColumns move up to occupy
        /// the vacated spot. The indices of the TableColumns that are moved are
        /// also updated.
        /// </remarks>
        public void RemoveRange(int index, int count)
        {
            _columnCollection.RemoveRange(index, count);
        }

        /// <summary>
        /// Sets the capacity to the actual number of elements in the TableColumnCollection.
        /// </summary>
        /// <remarks>
        /// This method can be used to minimize a TableColumnCollection's memory overhead
        /// if no new elements will be added to the collection.
        ///
        /// To completely clear all elements in a TableColumnCollection, call the Clear method
        /// before calling TrimToSize.
        /// </remarks>
        public void TrimToSize()
        {
            _columnCollection.TrimToSize();
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
            TableColumn item = value as TableColumn;

            if (item == null)
            {
                throw new ArgumentException(SR.Get(SRID.TableCollectionElementTypeExpected, typeof(TableColumn).Name), "value");
            }

            return ((IList)_columnCollection).Add(value);
        }

        void IList.Clear()
        {
            this.Clear();
        }

        bool IList.Contains(object value)
        {
            return ((IList)_columnCollection).Contains(value);
        }

        int IList.IndexOf(object value)
        {
            return ((IList)_columnCollection).IndexOf(value);
        }

        void IList.Insert(int index, object value)
        {
            ((IList)_columnCollection).Insert(index, value);
        }

        bool IList.IsFixedSize
        {
            get
            {
                return ((IList)_columnCollection).IsFixedSize;
            }
        }

        bool IList.IsReadOnly
        {
            get
            {
                return ((IList)_columnCollection).IsReadOnly;
            }
        }

        void IList.Remove(object value)
        {
            ((IList)_columnCollection).Remove(value);
        }

        void IList.RemoveAt(int index)
        {
            ((IList)_columnCollection).RemoveAt(index);
        }

        object IList.this[int index]
        {
            get
            {
                return ((IList)_columnCollection)[index];
            }

            set
            {
                ((IList)_columnCollection)[index] = value;
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
                return _columnCollection.Count;
            }
        }

        /// <summary>
        ///     <see cref="ICollection&lt;T&gt;.IsReadOnly"/>
        ///     <seealso cref="IList.IsReadOnly"/>
        /// </summary>
        public bool IsReadOnly  //  bool IList.IsReadOnly {get;}; bool ICollection<T>.IsReadOnly {get;}
        {
            get
            {
                return _columnCollection.IsReadOnly;
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
                return _columnCollection.IsSynchronized;
            }
        }

        /// <summary>
        /// <see cref="ICollection.SyncRoot"/>
        /// </summary>
        public object SyncRoot
        {
            get
            {
                return _columnCollection.SyncRoot;
            }
        }

        /// <summary>
        /// Gets or sets the number of elements that the TableColumnCollection can contain.
        /// </summary>
        /// <value>
        /// The number of elements that the TableColumnCollection can contain.
        /// </value>
        /// <remarks>
        /// Capacity is the number of elements that the TableColumnCollection is capable of storing.
        /// Count is the number of Visuals that are actually in the TableColumnCollection.
        ///
        /// Capacity is always greater than or equal to Count. If Count exceeds
        /// Capacity while adding elements, the capacity of the TableColumnCollection is increased.
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
                return _columnCollection.PrivateCapacity;
            }
            set
            {
                _columnCollection.PrivateCapacity = value;
            }
        }

        /// <summary>
        /// Indexer for the TableColumnCollection. Gets the TableColumn stored at the
        /// zero-based index of the TableColumnCollection.
        /// </summary>
        /// <remarks>This property provides the ability to access a specific TableColumn in the
        /// TableColumnCollection by using the following systax: <c>TableColumn myTableColumn = myTableColumnCollection[index]</c>.
        /// </remarks>
        /// <exception cref="ArgumentOutOfRangeException">
        /// <c>index</c> is less than zero -or- <c>index</c> is equal to or greater than Count.
        /// </exception>
        public TableColumn this[int index]
        {
            get
            {
                return _columnCollection[index];
            }
            set
            {
                _columnCollection[index] = value;
            }
        }

        #endregion Public Properties


        private TableColumnCollectionInternal _columnCollection;
    }
}
