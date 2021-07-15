// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

﻿using MS.Utility;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Documents;
using System.Diagnostics;

namespace MS.Internal.Documents
{
    /// <summary>
    /// This class is specialization of <see cref="ContentElementCollection<TParent, TItem>"/> to be used for 
    /// TableColumn collections
    /// </summary>
    internal class TableColumnCollectionInternal : ContentElementCollection<Table, TableColumn>
    {
        internal TableColumnCollectionInternal(Table owner)
            : base(owner)
        {
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
        public override void Add(TableColumn item)
        {
            Version++;

            if (item == null)
            {
                throw new ArgumentNullException("item");
            }
            if (Size == Items.Length)
            {
                EnsureCapacity(Size + 1);
            }

            int index = Size++;
            Debug.Assert(Items[index] == null);

            PrivateConnectChild(index, item);
        }

        /// <summary>
        /// Removes all elements from the ContentElementCollection.
        /// </summary>
        /// <remarks>
        /// Count is set to zero. Capacity remains unchanged.
        /// To reset the capacity of the ContentElementCollection, call TrimToSize
        /// or set the Capacity property directly.
        /// </remarks>
        public override void Clear()
        {
            Version++;

            for (int i = 0; i < Size; ++i)
            {
                Debug.Assert(BelongsToOwner(Items[i]));

                PrivateDisconnectChild(Items[i]);
                Items[i] = null;
            }
            Size = 0;
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
        public override void Insert(int index, TableColumn item)
        {
            Version++;

            if (index < 0 || index > Size)
            {
                throw new ArgumentOutOfRangeException(SR.Get(SRID.TableCollectionOutOfRange));
            }
            if (item == null)
            {
                throw new ArgumentNullException("item");
            }
            if (Size == Items.Length)
            {
                EnsureCapacity(Size + 1);
            }

            for (int i = Size - 1; i >= index; --i)
            {
                Debug.Assert(BelongsToOwner(Items[i]));

                Items[i + 1] = Items[i];
                Items[i].Index = i + 1;
            }

            Items[index] = null;

            Size++;
            PrivateConnectChild(index, item);
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
        internal override void PrivateConnectChild(int index, TableColumn item)
        {
            Debug.Assert(item != null && item.Index == -1);
            Debug.Assert(Items[index] == null);

            // If the TItem is already parented correctly through a proxy, there's no need
            // to change parentage.  Otherwise, it should be parented to Owner.
            if (item.Parent is DummyProxy)
            {
                if (LogicalTreeHelper.GetParent(item.Parent) != Owner)
                {
                    throw new System.ArgumentException(SR.Get(SRID.TableCollectionWrongProxyParent));
                }
            }
            else
            {
                if (item.Parent != null)
                {
                    throw new System.ArgumentException(SR.Get(SRID.TableCollectionInOtherCollection));
                }

                Owner.AddLogicalChild(item);
            }

            // add the item into collection's array
            Items[index] = item;
            item.Index = index;

            // notify the TItem about the change
            item.OnEnterParentTree();
        }

        /// <summary>
        /// Notifies the TItem about the event;
        /// Disconnects the item from the model tree;
        /// Sets the TItem's slot in the collection's array to null.
        /// </summary>
        internal override void PrivateDisconnectChild(TableColumn item)
        {
            Debug.Assert(item != null);

            // notify the TItem about the change
            item.OnExitParentTree();

            // remove the item from collection's array
            Items[item.Index] = null;
            item.Index = -1;

            // remove from model tree
            // If the item is parented through a proxy then the parent link of the item is 
            // not to Owner and we should not change the parentage.
            DummyProxy proxy = item.Parent as DummyProxy;
            if (proxy == null)
            {
                Owner.RemoveLogicalChild(item);
            }
        }
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
        public override bool Remove(TableColumn item)
        {
            Version++;

            if (item == null)
            {
                throw new ArgumentNullException("item");
            }
            if (!BelongsToOwner(item))
            {
                return false;
            }

            PrivateRemove(item);

            return true;
        }

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
        public override void RemoveAt(int index)
        {
            Version++;

            if (index < 0 || index >= Size)
            {
                throw new ArgumentOutOfRangeException(SR.Get(SRID.TableCollectionOutOfRange));
            }

            PrivateRemove(Items[index]);
        }


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
        public override void RemoveRange(int index, int count)
        {
            Version++;

            if (index < 0 || index >= Size)
            {
                throw new ArgumentOutOfRangeException(SR.Get(SRID.TableCollectionOutOfRange));
            }
            if (count < 0)
            {
                throw new ArgumentOutOfRangeException(SR.Get(SRID.TableCollectionCountNeedNonNegNum));
            }
            if (Size - index < count)
            {
                throw new ArgumentException(SR.Get(SRID.TableCollectionRangeOutOfRange));
            }

            if (count > 0)
            {
                for (int i = index + count - 1; i >= index; --i)
                {
                    Debug.Assert(BelongsToOwner(Items[i]));

                    PrivateDisconnectChild(Items[i]);
                }

                Size -= count;
                for (int i = index; i < Size; ++i)
                {
                    Debug.Assert(BelongsToOwner(Items[i + count]));

                    Items[i] = Items[i + count];
                    Items[i].Index = i;
                    Items[i + count] = null;
                }
            }
        }
        /// <summary>
        /// Indexer for the ContentElementCollection. Gets the TItem stored at the
        /// zero-based index of the ContentElementCollection.
        /// </summary>
        /// <remarks>This property provides the ability to access a specific TItem in the
        /// ContentElementCollection by using the following systax: <c>TItem myTItem = myContentElementCollection[index]</c>.
        /// </remarks>
        /// <exception cref="ArgumentOutOfRangeException">
        /// <c>index</c> is less than zero -or- <c>index</c> is equal to or greater than Count.
        /// </exception>
        public override TableColumn this[int index]
        {
            get
            {
                if (index < 0 || index >= Size)
                {
                    throw new ArgumentOutOfRangeException(SR.Get(SRID.TableCollectionOutOfRange));
                }
                return (Items[index]);
            }
            set
            {
                if (index < 0 || index >= Size)
                {
                    throw new ArgumentOutOfRangeException(SR.Get(SRID.TableCollectionOutOfRange));
                }

                if (value == null)
                {
                    throw new ArgumentNullException("value");
                }

                PrivateDisconnectChild(Items[index]);
                PrivateConnectChild(index, value);
            }
        }
    }
}
