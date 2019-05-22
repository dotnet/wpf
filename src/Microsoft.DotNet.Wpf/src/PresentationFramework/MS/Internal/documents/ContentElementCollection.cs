// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// Description: Generic Collection of Table related objects.
//

using MS.Utility;
using System;
using System.Collections.Generic;
using System.Collections;
using System.Diagnostics;
using System.Threading;
using System.Windows;
using System.Windows.Documents;

namespace MS.Internal.Documents
{
    /// <summary>
    /// A ContentElementCollection is an ordered collection of TItems.
    /// </summary>
    /// <remarks>
    /// ContentElementCollection provides public access for TItems
    /// reading and manipulating.
    /// </remarks>
    internal abstract class ContentElementCollection<TParent, TItem> : IList<TItem>, IList
        where TParent : TextElement, IAcceptInsertion
        where TItem : FrameworkContentElement, IIndexedChild<TParent>
    {
        //------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------

        #region Constructors

        internal ContentElementCollection(TParent owner)
        {
            Debug.Assert(owner != null);
            _owner = owner;
            Items = new TItem[DefaultCapacity];
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
            if (array == null)
            {
                throw new ArgumentNullException("array");
            }
            if (array.Rank != 1)
            {
                throw new ArgumentException(SR.Get(SRID.TableCollectionRankMultiDimNotSupported));
            }
            if (index < 0)
            {
                throw new ArgumentOutOfRangeException("index", SR.Get(SRID.TableCollectionOutOfRangeNeedNonNegNum));
            }
            if (array.Length - index < Size)
            {
                throw new ArgumentException(SR.Get(SRID.TableCollectionInvalidOffLen));
            }

            Array.Copy(Items, 0, array, index, Size);
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
        public void CopyTo(TItem[] array, int index)
        {
            if (array == null)
            {
                throw new ArgumentNullException("array");
            }
            if (index < 0)
            {
                throw new ArgumentOutOfRangeException("index", SR.Get(SRID.TableCollectionOutOfRangeNeedNonNegNum));
            }
            if (array.Length - index < Size)
            {
                throw new ArgumentException(SR.Get(SRID.TableCollectionInvalidOffLen));
            }

            Array.Copy(Items, 0, array, index, Size);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }
        /// <summary>
        ///     <see cref="IEnumerable.GetEnumerator"/>
        /// </summary>
        internal IEnumerator GetEnumerator()
        {
            return (new ContentElementCollectionEnumeratorSimple(this));
        }

        /// <summary>
        ///     <see cref="IEnumerable&lt;T&gt;.GetEnumerator"/>
        /// </summary>
        IEnumerator<TItem> IEnumerable<TItem>.GetEnumerator()
        {
            return (new ContentElementCollectionEnumeratorSimple(this));
        }

        /// <summary>
        /// Appends a TItem to the end of the ContentElementCollection.
        /// </summary>
        /// <param name="item">The TItem to be added to the end of the ContentElementCollection.</param>
        /// <returns>The ContentElementCollection index at which the TItem has been added.</returns>
        /// <remarks>Adding a null is prohibited.</remarks>
        /// <exception cref="ArgumentNullException">
        /// If the <c>item</c> value is null.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// If the new child already has a parent.
        /// </exception>
        abstract public void Add(TItem item);

        /// <summary>
        /// Removes all elements from the ContentElementCollection.
        /// </summary>
        /// <remarks>
        /// Count is set to zero. Capacity remains unchanged.
        /// To reset the capacity of the ContentElementCollection, call TrimToSize
        /// or set the Capacity property directly.
        /// </remarks>
        abstract public void Clear();

        /// <summary>
        /// Determines whether a TItem is in the ContentElementCollection.
        /// </summary>
        /// <param name="item">The TItem to locate in the ContentElementCollection.
        /// The value can be a null reference.</param>
        /// <returns>true if TItem is found in the ContentElementCollection;
        /// otherwise, false.</returns>
        public bool Contains(TItem item)
        {
            if (BelongsToOwner(item))
            {
                Debug.Assert(Items[item.Index] == item);
                return (true);
            }

            return (false);
        }

        /// <summary>
        /// Returns the zero-based index of the TItem. If the TItem is not
        /// in the ContentElementCollection, -1 is returned.
        /// </summary>
        /// <param name="item">The TItem to locate in the ContentElementCollection.</param>
        public int IndexOf(TItem item)
        {
            if (BelongsToOwner(item))
            {
                return item.Index;
            }
            else
            {
                return (-1);
            }
        }

        /// <summary>
        /// Inserts a TItem into the ContentElementCollection at the specified index.
        /// </summary>
        /// <param name="index">The zero-based index at which value should be inserted.</param>
        /// <param name="item">The TItem to insert. </param>
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
        /// ContentElementCollection is increased before the new TItem is inserted.
        ///
        /// If index is equal to Count, TItem is added to the
        /// end of ContentElementCollection.
        ///
        /// The TItems that follow the insertion point move down to
        /// accommodate the new TItem. The indexes of the TItems that are
        /// moved are also updated.
        /// </remarks>
        abstract public void Insert(int index, TItem item);

        /// <summary>
        /// Removes the specified TItem from the ContentElementCollection.
        /// </summary>
        /// <param name="item">The TItem to remove from the ContentElementCollection.</param>
        /// <exception cref="ArgumentNullException">
        /// If the <c>item</c> value is null.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// If the specified TItem is not in this collection.
        /// </exception>
        /// <remarks>
        /// The TItems that follow the removed TItem move up to occupy
        /// the vacated spot. The indices of the TItems that are moved
        /// also updated.
        /// </remarks>
        abstract public bool Remove(TItem item);

        /// <summary>
        /// Removes the TItem at the specified index.
        /// </summary>
        /// <param name="index">The zero-based index of the TItem to remove.</param>
        /// <exception cref="ArgumentOutOfRangeException">
        /// <c>index</c> is less than zero
        /// - or -
        /// <c>index</c> is equal or greater than count.
        /// </exception>
        /// <remarks>
        /// The TItems that follow the removed TItem move up to occupy
        /// the vacated spot. The indices of the TItems that are moved
        /// also updated.
        /// </remarks>
        abstract public void RemoveAt(int index);



        /// <summary>
        /// Removes a range of TItems from the ContentElementCollection.
        /// </summary>
        /// <param name="index">The zero-based index of the range
        /// of TItems to remove</param>
        /// <param name="count">The number of TItems to remove.</param>
        /// <exception cref="ArgumentOutOfRangeException">
        /// <c>index</c> is less than zero.
        /// -or-
        /// <c>count</c> is less than zero.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// <c>index</c> and <c>count</c> do not denote a valid range of TItems in the ContentElementCollection.
        /// </exception>
        /// <remarks>
        /// The TItems that follow the removed TItems move up to occupy
        /// the vacated spot. The indices of the TItems that are moved are
        /// also updated.
        /// </remarks>
        abstract public void RemoveRange(int index, int count);


        /// <summary>
        /// Sets the capacity to the actual number of elements in the ContentElementCollection.
        /// </summary>
        /// <remarks>
        /// This method can be used to minimize a ContentElementCollection's memory overhead
        /// if no new elements will be added to the collection.
        ///
        /// To completely clear all elements in a ContentElementCollection, call the Clear method
        /// before calling TrimToSize.
        /// </remarks>
        public void TrimToSize()
        {
            PrivateCapacity = Size;
        }

        #endregion Public Methods

        //------------------------------------------------------
        //
        //  Private Types
        //
        //------------------------------------------------------

        #region Protected Types

        protected class ContentElementCollectionEnumeratorSimple : IEnumerator<TItem>, IEnumerator
        {
            internal ContentElementCollectionEnumeratorSimple(ContentElementCollection<TParent, TItem> collection)
            {
                Debug.Assert(collection != null);

                _collection = collection;
                _index = -1;
                Version = _collection.Version;
                _currentElement = collection;
            }

            public bool MoveNext()
            {
                if (Version != _collection.Version)
                {
                    throw new InvalidOperationException(SR.Get(SRID.EnumeratorVersionChanged));
                }

                if (_index < (_collection.Size - 1))
                {
                    _index++;
                    _currentElement = _collection[_index];
                    return (true);
                }
                else
                {
                    _currentElement = _collection;
                    _index = _collection.Size;
                    return (false);
                }
            }

            public TItem Current
            {
                get
                {
                    if (_currentElement == _collection)
                    {
                        if (_index == -1)
                        {
                            throw new InvalidOperationException(SR.Get(SRID.EnumeratorNotStarted));
                        }
                        else
                        {
                            throw new InvalidOperationException(SR.Get(SRID.EnumeratorReachedEnd));
                        }
                    }
                    return (TItem)_currentElement;
                }
            }

            /// <summary>
            ///     <see cref="IEnumerator.Current"/>
            /// </summary>
            object IEnumerator.Current
            {
                get
                {
                    return this.Current;
                }
            }


            public void Reset()
            {
                if (Version != _collection.Version)
                {
                    throw new InvalidOperationException(SR.Get(SRID.EnumeratorVersionChanged));
                }
                _currentElement = _collection;
                _index = -1;
            }

            public void Dispose()
            {
                GC.SuppressFinalize(this);
            }


            private ContentElementCollection<TParent, TItem> _collection;
            private int _index;
            protected int Version;
            private object _currentElement;
        }

        protected class DummyProxy : DependencyObject
        {
        }

        #endregion Protected Types

        //-------------------------------------------------------------------
        //
        //  IList Members
        //
        //-------------------------------------------------------------------
        #region IList Members

        int IList.Add(object value)
        {
            if (value == null)
            {
                throw new ArgumentNullException("value");
            }

            TItem item = value as TItem;

            // <CR NOTE>: This chunk is moved to the correcponding override in TableCollumnCollection, to keep same behavior
            //if (item == null)
            //{
            //    throw new ArgumentException(SR.Get(SRID.TableCollectionElementTypeExpected, typeof(TItem).Name), "value");
            //}

            this.Add(item);

            return ((IList)this).IndexOf(item);
        }

        void IList.Clear()
        {
            this.Clear();
        }

        bool IList.Contains(object value)
        {
            TItem item = value as TItem;

            if (item == null)
            {
                return false;
            }

            return this.Contains(item);
        }

        int IList.IndexOf(object value)
        {
            TItem item = value as TItem;

            if (item == null)
            {
                return -1;
            }

            return this.IndexOf(item);
        }

        void IList.Insert(int index, object value)
        {
            if (value == null)
            {
                throw new ArgumentNullException("value");
            }

            TItem newItem = value as TItem;

            if (newItem == null)
            {
                throw new ArgumentException(SR.Get(SRID.TableCollectionElementTypeExpected, typeof(TItem).Name), "value");
            }

            this.Insert(index, newItem);
        }

        bool IList.IsFixedSize
        {
            get
            {
                return false;
            }
        }

        bool IList.IsReadOnly
        {
            get
            {
                return this.IsReadOnly;
            }
        }

        void IList.Remove(object value)
        {
            TItem item = value as TItem;

            if (item == null)
            {
                return;
            }

            this.Remove(item);
        }

        void IList.RemoveAt(int index)
        {
            this.RemoveAt(index);
        }

        object IList.this[int index]
        {
            get
            {
                return this[index];
            }

            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException("value");
                }

                TItem item = value as TItem;

                if (item == null)
                {
                    throw new ArgumentException(SR.Get(SRID.TableCollectionElementTypeExpected, typeof(TItem).Name), "value");
                }

                this[index] = item;
            }
        }

        #endregion IList Members


        //------------------------------------------------------
        //
        //  Public Properties
        //
        //------------------------------------------------------

        #region Public Properties

        public abstract TItem this[int index]
        {
            get;
            set;
        }

        /// <summary>
        /// <see cref="ICollection.Count"/>
        /// </summary>
        public int Count
        {
            get
            {
                return (Size);
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
                return false;
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
                return (false);
            }
        }

        /// <summary>
        /// <see cref="ICollection.SyncRoot"/>
        /// </summary>
        public object SyncRoot
        {
            get
            {
                // Need to figure out what is the correct behavior.
                //       current code won't do what's expected.
                return (this);
            }
        }

        /// <summary>
        /// Gets or sets the number of elements that the ContentElementCollection can contain.
        /// </summary>
        /// <value>
        /// The number of elements that the ContentElementCollection can contain.
        /// </value>
        /// <remarks>
        /// Capacity is the number of elements that the ContentElementCollection is capable of storing.
        /// Count is the number of Visuals that are actually in the ContentElementCollection.
        ///
        /// Capacity is always greater than or equal to Count. If Count exceeds
        /// Capacity while adding elements, the capacity of the ContentElementCollection is increased.
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
                return PrivateCapacity;
            }
            set
            {
                PrivateCapacity = value;
            }
        }
        public TParent Owner
        {
            get { return _owner; }
        }

        #endregion Public Properties


        #region Protected Properties

        protected TItem[] Items
        {
            get { return _items; }
            private set { _items = value; }
        }

        protected int Size
        {
            get { return _size; }
            set { _size = value; }
        }

        protected int Version
        {
            get { return _version; }
            set { _version = value; }
        }

        protected int DefaultCapacity
        {
            get { return c_defaultCapacity; }
        }
        #endregion Protected Properties

        //------------------------------------------------------
        //
        //  Private Methods
        //
        //------------------------------------------------------

        #region Internal Methods


        /// <summary>
        /// Ensures that the capacity of this list is at least the given minimum
        /// value. If the currect capacity of the list is less than min, the
        /// capacity is increased to min.
        /// </summary>
        internal void EnsureCapacity(int min)
        {
            if (PrivateCapacity < min)
            {
                PrivateCapacity = Math.Max(min, PrivateCapacity * 2);
            }
        }

        /// <summary>
        /// Sets the specified TItem at the specified index;
        /// Connects the item to the model tree;
        /// Notifies the TItem about the event.
        /// </summary>
        /// <exception cref="ArgumentException">
        /// If the new item has already a parent or if the slot at the specified index is not null.
        /// </exception>
        /// <remarks>
        /// Note that the function requires that _item[index] == null and
        /// it also requires that the passed in item is not included into another ContentElementCollection.
        /// </remarks>
        abstract internal void PrivateConnectChild(int index, TItem item);


        /// <summary>
        /// Notifies the TItem about the event;
        /// Disconnects the item from the model tree;
        /// Sets the TItem's slot in the collection's array to null.
        /// </summary>
        abstract internal void PrivateDisconnectChild(TItem item);


        /// <summary>
        /// Removes specified TItem from the ContentElementCollection.
        /// </summary>
        /// <param name="item">TItem to remove.</param>
        internal void PrivateRemove(TItem item)
        {
            Debug.Assert(BelongsToOwner(item) && Items[item.Index] == item);

            int index = item.Index;

            PrivateDisconnectChild(item);

            --Size;

            for (int i = index; i < Size; ++i)
            {
                Debug.Assert(BelongsToOwner(Items[i + 1]));

                Items[i] = Items[i + 1];
                Items[i].Index = i;
            }

            Items[Size] = null;
        }

        // helper method: return true if the item belongs to the collection's owner
        internal bool BelongsToOwner(TItem item)
        {
            if (item == null)
                return false;

            DependencyObject node = item.Parent;
            if (node is DummyProxy)
            {
                node = LogicalTreeHelper.GetParent(node);
            }

            return (node == Owner);
        }

        #endregion Internal Methods

        //------------------------------------------------------
        //
        //  Private Properties
        //
        //------------------------------------------------------

        #region Internal Properties

        /// <summary>
        /// PrivateCapacity sets/gets the Capacity of the collection.
        /// </summary>
        internal int PrivateCapacity
        {
            get
            {
                return (Items.Length);
            }
            set
            {
                if (value != Items.Length)
                {
                    if (value < Size)
                    {
                        throw new ArgumentOutOfRangeException(SR.Get(SRID.TableCollectionNotEnoughCapacity));
                    }

                    if (value > 0)
                    {
                        TItem[] newItems = new TItem[value];
                        if (Size > 0)
                        {
                            Array.Copy(Items, 0, newItems, 0, Size);
                        }
                        Items = newItems;
                    }
                    else
                    {
                        Items = new TItem[DefaultCapacity];
                    }
                }
            }
        }

        #endregion Internal Properties

        //------------------------------------------------------
        //
        //  Protected Fields
        //
        //------------------------------------------------------

        #region Protected Fields
        private readonly TParent _owner;      //  owner of the collection
        private TItem[] _items;              //  storage of items
        private int _size;                          //  size of the collection
        private int _version;                       //  version tracks updates in the collection
        protected const int c_defaultCapacity = 8;    //  default capacity of the collection
        #endregion Protected Fields

    }
}
