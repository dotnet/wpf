// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// Description: CompositeCollection holds the list of items that constitute the content of a ItemsControl.
//
// See specs at ItemsControl.mht
//

using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;

using System.Windows;
using System.Windows.Markup;

using MS.Internal;              // Invariant.Assert
using MS.Internal.Data;
using MS.Utility;

using System;

namespace System.Windows.Data
{
    /// <summary>
    /// CompositeCollection will contain items shaped as strings, objects, xml nodes,
    /// elements as well as other collections.
    /// A <seealso cref="System.Windows.Controls.ItemsControl"/> uses the data
    /// in the CompositeCollection to generate its content according to its ItemTemplate.
    /// </summary>

    [Localizability(LocalizationCategory.Ignore)]
    public class CompositeCollection : IList, INotifyCollectionChanged, ICollectionViewFactory, IWeakEventListener
    {
        //------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------

        #region Constructors

        /// <summary>
        /// Initializes a new instance of CompositeCollection that is empty and has default initial capacity.
        /// </summary>
        public CompositeCollection()
        {
            Initialize(new ArrayList());
        }

        /// <summary>
        /// Initializes a new instance of CompositeCollection that is empty and has specified initial capacity.
        /// </summary>
        /// <param name="capacity">The number of items that the new list is initially capable of storing</param>
        /// <remarks>
        /// Some ItemsControl implementations have better idea how many items to anticipate,
        /// capacity parameter lets them tailor the initial size.
        /// </remarks>
        public CompositeCollection(int capacity)
        {
            Initialize(new ArrayList(capacity));
        }

        #endregion Constructors


        //------------------------------------------------------
        //
        //  Public Methods
        //
        //------------------------------------------------------

        #region Public Methods
        /// <summary>
        ///     Returns an enumerator object for this CompositeCollection
        /// </summary>
        /// <returns>
        ///     Enumerator object for this CompositeCollection
        /// </returns>
        IEnumerator IEnumerable.GetEnumerator()
        {
            // Enumerator from the underlying ArrayList
            return InternalList.GetEnumerator();
        }

        /// <summary>
        ///     Makes a shallow copy of object references from this
        ///     CompositeCollection to the given target array
        /// </summary>
        /// <param name="array">
        ///     Target of the copy operation
        /// </param>
        /// <param name="index">
        ///     Zero-based index at which the copy begins
        /// </param>
        public void CopyTo(Array array, int index)
        {
            // Forward call to internal list.
            InternalList.CopyTo(array, index);
        }

        /// <summary>
        ///     Add an item to this collection.
        /// </summary>
        /// <param name="newItem">
        ///     New item to be added to collection
        /// </param>
        /// <returns>
        ///     Zero-based index where the new item is added.
        /// </returns>
        /// <exception cref="ArgumentException">
        ///     CompositeCollection can only accept CollectionContainers it doesn't already have.
        /// </exception>
        public int Add(object newItem)
        {
            CollectionContainer cc = newItem as CollectionContainer;
            if (cc != null)
            {
                AddCollectionContainer(cc);
            }

            int addedIndex = InternalList.Add(newItem);

            OnCollectionChanged(NotifyCollectionChangedAction.Add, newItem, addedIndex);
            return addedIndex;
        }

        /// <summary>
        ///     Clears the collection.  Releases the references on all items
        /// currently in the collection.
        /// </summary>
        public void Clear()
        {
            // unhook contained collections
            for (int k=0, n=InternalList.Count;  k < n;  ++k)
            {
                CollectionContainer cc = this[k] as CollectionContainer;
                if (cc != null)
                {
                    RemoveCollectionContainer(cc);
                }
            }

            InternalList.Clear();
            OnCollectionChanged(NotifyCollectionChangedAction.Reset);
        }

        /// <summary>
        ///     Checks to see if a given item is in this collection
        /// </summary>
        /// <param name="containItem">
        ///     The item whose membership in this collection is to be checked.
        /// </param>
        /// <returns>
        ///     True if the collection contains the given item
        /// </returns>
        public bool Contains(object containItem)
        {
            return InternalList.Contains(containItem);
        }

        /// <summary>
        ///     Finds the index in this collection where the given item is found.
        /// </summary>
        /// <param name="indexItem">
        ///     The item whose index in this collection is to be retrieved.
        /// </param>
        /// <returns>
        ///     Zero-based index into the collection where the given item can be
        /// found.  Otherwise, -1
        /// </returns>
        public int IndexOf(object indexItem)
        {
            return InternalList.IndexOf(indexItem);
        }

        /// <summary>
        ///     Insert an item in the collection at a given index.  All items
        /// after the given position are moved down by one.
        /// </summary>
        /// <param name="insertIndex">
        ///     The index at which to inser the item
        /// </param>
        /// <param name="insertItem">
        ///     The item reference to be added to the collection
        /// </param>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Thrown if index is out of range
        /// </exception>
        public void Insert(int insertIndex, object insertItem)
        {
            CollectionContainer cc = insertItem as CollectionContainer;
            if (cc != null)
            {
                AddCollectionContainer(cc);
            }

            // ArrayList implementation checks index and will throw out of range exception
            InternalList.Insert(insertIndex, insertItem);

            OnCollectionChanged(NotifyCollectionChangedAction.Add, insertItem, insertIndex);
        }

        /// <summary>
        ///     Removes the given item reference from the collection.  All
        /// remaining items move up by one.
        /// </summary>
        /// <param name="removeItem">
        ///     The item to be removed.
        /// </param>
        public void Remove(object removeItem)
        {
            int index = InternalList.IndexOf(removeItem);
            if (index >= 0)
            {
                // to ensure model parent is cleared and the CollectionChange notification is raised,
                // call this.RemoveAt, not the aggregated ArrayList
                this.RemoveAt(index);
            }
        }

        /// <summary>
        ///     Removes an item from the collection at the given index.  All
        /// remaining items move up by one.
        /// </summary>
        /// <param name="removeIndex">
        ///     The index at which to remove an item.
        /// </param>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Thrown if index is out of range
        /// </exception>
        public void RemoveAt(int removeIndex)
        {
            if ((0 <= removeIndex) && (removeIndex < Count))
            {
                object removedItem = this[removeIndex];

                CollectionContainer cc = removedItem as CollectionContainer;
                if (cc != null)
                {
                    RemoveCollectionContainer(cc);
                }

                InternalList.RemoveAt(removeIndex);

                OnCollectionChanged(NotifyCollectionChangedAction.Remove, removedItem, removeIndex);
            }
            else
            {
                throw new ArgumentOutOfRangeException("removeIndex",
                            SR.Get(SRID.ItemCollectionRemoveArgumentOutOfRange));
            }
        }


        /// <summary>
        /// Create a new view on this collection [Do not call directly].
        /// </summary>
        /// <remarks>
        /// Normally this method is only called by the platform's view manager,
        /// not by user code.
        /// </remarks>
        ICollectionView ICollectionViewFactory.CreateView()
        {
            return new CompositeCollectionView(this);
        }

        #endregion Public Methods


        //------------------------------------------------------
        //
        //  Public Properties
        //
        //------------------------------------------------------

        #region Public Properties

        /// <summary>
        ///     Read-only property for the number of items stored in this collection of objects
        /// </summary>
        /// <remarks>
        ///     CollectionContainers each count as 1 item.
        ///     When in ItemsSource mode, Count always equals 1.
        /// </remarks>
        public int Count
        {
            get
            {
                // Return value from the underlying ArrayList.
                return InternalList.Count;
            }
        }

        /// <summary>
        ///     Indexer property to retrieve or replace the item at the given
        /// zero-based offset into the collection.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Thrown if index is out of range
        /// </exception>
        public object this[int itemIndex]
        {
            get
            {
                // ArrayList implementation checks index and will throw out of range exception
                return InternalList[itemIndex];
            }
            set
            {
                // ArrayList implementation checks index and will throw out of range exception
                object originalItem = InternalList[itemIndex];

                // unhook the old, hook the new
                CollectionContainer cc;
                if ((cc = originalItem as CollectionContainer) != null)
                {
                    RemoveCollectionContainer(cc);
                }
                if ((cc = value as CollectionContainer) != null)
                {
                    AddCollectionContainer(cc);
                }

                // make the change
                InternalList[itemIndex] = value;

                OnCollectionChanged(NotifyCollectionChangedAction.Replace, originalItem, value, itemIndex);
            }
        }

        /// <summary>
        ///     Gets a value indicating whether access to the CompositeCollection is synchronized (thread-safe).
        /// </summary>
        bool ICollection.IsSynchronized
        {
            get
            {
                // Return value from the underlying ArrayList.
                return InternalList.IsSynchronized;
            }
        }

        /// <summary>
        ///     Returns an object to be used in thread synchronization.
        /// </summary>
        object ICollection.SyncRoot
        {
            get
            {
                // Return the SyncRoot object of the underlying ArrayList
                return InternalList.SyncRoot;
            }
        }

        /// <summary>
        ///     Gets a value indicating whether the IList has a fixed size.
        ///     An CompositeCollection can usually grow dynamically,
        ///     this call will commonly return FixedSize = False.
        ///     In ItemsSource mode, this call will return IsFixedSize = True.
        /// </summary>
        bool IList.IsFixedSize
        {
            get
            {
                return InternalList.IsFixedSize;
            }
        }

        /// <summary>
        ///     Gets a value indicating whether the IList is read-only.
        ///     An CompositeCollection is usually writable,
        ///     this call will commonly return IsReadOnly = False.
        ///     In ItemsSource mode, this call will return IsReadOnly = True.
        /// </summary>
        bool IList.IsReadOnly
        {
            get
            {
                return InternalList.IsReadOnly;
            }
        }

        #endregion Public Properties


        //------------------------------------------------------
        //
        //  Public Events
        //
        //------------------------------------------------------

        #region Public Events

        /// <summary>
        /// Occurs when the collection changes, either by adding or removing an item
        /// <see cref="INotifyCollectionChanged" />
        /// </summary>
        event NotifyCollectionChangedEventHandler INotifyCollectionChanged.CollectionChanged
        {
            add
            {
                CollectionChanged += value;
            }
            remove
            {
                CollectionChanged -= value;
            }
        }

        /// <summary>
        /// Occurs when the collection changes, either by adding or removing an item.
        /// </summary>
        protected event NotifyCollectionChangedEventHandler CollectionChanged;

        #endregion Public Events

        #region IWeakEventListener

        /// <summary>
        /// Handle events from the centralized event table
        /// </summary>
        bool IWeakEventListener.ReceiveWeakEvent(Type managerType, object sender, EventArgs e)
        {
            return ReceiveWeakEvent(managerType, sender, e);
        }

        /// <summary>
        /// Handle events from the centralized event table
        /// </summary>
        protected virtual bool ReceiveWeakEvent(Type managerType, object sender, EventArgs e)
        {
            return false;   // this method is no longer used (but must remain, for compat)
        }

        #endregion IWeakEventListener

        //------------------------------------------------------
        //
        //  Internal Events
        //
        //------------------------------------------------------

        #region Internal Events

        internal event NotifyCollectionChangedEventHandler ContainedCollectionChanged;

        private void OnContainedCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (ContainedCollectionChanged != null)
                ContainedCollectionChanged(sender, e);
        }

        #endregion Internal Events

        //------------------------------------------------------
        //
        //  Private Methods
        //
        //------------------------------------------------------

        #region Private Methods

        // common ctor initialization
        private void Initialize(ArrayList internalList)
        {
            _internalList = internalList;
        }

        // ArrayList that holds collection containers as well as single items
        private ArrayList InternalList
        {
            get
            {
                return _internalList;
            }
        }

        // Hook up to a newly-added CollectionContainer
        private void AddCollectionContainer(CollectionContainer cc)
        {
            if (InternalList.Contains(cc))
                throw new ArgumentException(SR.Get(SRID.CollectionContainerMustBeUniqueForComposite), "cc");

            CollectionChangedEventManager.AddHandler(cc, OnContainedCollectionChanged);

#if DEBUG
            _hasRepeatedCollectionIsValid = false;
#endif
        }

        // Unhook a newly-deleted CollectionContainer
        private void RemoveCollectionContainer(CollectionContainer cc)
        {
            CollectionChangedEventManager.RemoveHandler(cc, OnContainedCollectionChanged);

#if DEBUG
            _hasRepeatedCollectionIsValid = false;
#endif
        }

        // raise CollectionChanged event to any listeners
        void OnCollectionChanged(NotifyCollectionChangedAction action)
        {
#if DEBUG
            _hasRepeatedCollectionIsValid = false;
#endif

            if (CollectionChanged != null)
            {
                CollectionChanged(this, new NotifyCollectionChangedEventArgs(action));
            }
        }

        // raise CollectionChanged event to any listeners
        void OnCollectionChanged(NotifyCollectionChangedAction action, object item, int index)
        {
            if (CollectionChanged != null)
            {
                CollectionChanged(this, new NotifyCollectionChangedEventArgs(action, item, index));
            }
        }

        /// raise CollectionChanged event to any listeners
        void OnCollectionChanged(NotifyCollectionChangedAction action, object oldItem, object newItem, int index)
        {
            if (CollectionChanged != null)
            {
                CollectionChanged(this, new NotifyCollectionChangedEventArgs(action, newItem, oldItem, index));
            }
        }

        #endregion Private Methods


        //------------------------------------------------------
        //
        //  Private Fields
        //
        //------------------------------------------------------

        #region Private Fields

        private ArrayList               _internalList;

        #endregion Private Fields


        //------------------------------------------------------
        //
        //  Debugging Aids
        //
        //------------------------------------------------------

        #region Debugging Aids

#if DEBUG
        internal bool HasRepeatedCollection()
        {
            if (!_hasRepeatedCollectionIsValid)
            {
                _hasRepeatedCollection = FindRepeatedCollection(new ArrayList());
                _hasRepeatedCollectionIsValid = true;
            }
            return _hasRepeatedCollection;
        }

        // recursive depth-first search for repeated collection
        private bool FindRepeatedCollection(ArrayList collections)
        {
            for (int i = 0; i < Count; ++i)
            {
                CollectionContainer cc = this[i] as CollectionContainer;
                if (cc != null && cc.Collection != null)
                {
                    CompositeCollection composite = cc.Collection as CompositeCollection;
                    if (composite != null)
                    {
                        if (composite.FindRepeatedCollection(collections))
                            return true;
                    }
                    else if (collections.IndexOf(cc.Collection) > -1)
                        return true;
                    else
                        collections.Add(cc.Collection);
                }
            }
            return false;
        }

        private bool _hasRepeatedCollection = false;
        private bool _hasRepeatedCollectionIsValid = false;
#endif

        internal void GetCollectionChangedSources(int level, Action<int, object, bool?, List<string>> format, List<string> sources)
        {
            format(level, this, false, sources);
            foreach (object o in InternalList)
            {
                CollectionContainer cc = o as CollectionContainer;
                if (cc != null)
                {
                    cc.GetCollectionChangedSources(level+1, format, sources);
                }
            }
        }

        #endregion Debugging Aids
    }
}

