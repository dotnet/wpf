// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
//
// Description:
//      The VisualCollection implementation is based on the
//      CLR's Lightning ArrayList implementation.
//

using MS.Win32;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Composition;
using System.Windows.Threading;

using System;
using System.Diagnostics;
using System.Collections;
using MS.Internal;
using System.Runtime.InteropServices;

using SR=MS.Internal.PresentationCore.SR;
using SRID=MS.Internal.PresentationCore.SRID;

//------------------------------------------------------------------------------
//  - There is an exception thrown inside of ConnectChild which could render
//         the collection inconsistent.
//  - Performance: RemoveRange moves and nulls entry. It is better to null out
//    after we moved all the items.
//------------------------------------------------------------------------------


// Since we disable PreSharp warnings in this file, we first need to disable
// warnings about unknown message numbers and unknown pragmas:
#pragma warning disable 1634, 1691

namespace System.Windows.Media
{
    /// <summary>
    /// A VisualCollection is a ordered collection of Visuals.
    /// </summary>
    /// <remarks>
    /// A VisualCollection has implied context affinity. It is a violation to access
    /// the VisualCollectionfrom a different context than the owning ContainerVisual belongs
    /// to.
    /// </remarks>
    public sealed class VisualCollection : ICollection
    {
        private Visual[] _items;
        private int _size;
        private Visual _owner;

        // We reserve bit 1 to keep track of readonly state.  Bits
        // 32..2 are used for our version counter.
        //
        //              Version                RO
        // +----------------------------------+---+
        // |        bit 32..2                 | 1 |
        // +----------------------------------+---+
        //
        private uint _data;

        private const int c_defaultCapacity = 4;
        private const float c_growFactor = 1.5f;

        internal int InternalCount { get { return _size; } }

        /// <summary>
        /// Returns a reference to the internal Visual children array.
        /// </summary>
        /// <remarks>
        /// This array should never given out.
        /// It is only used for internal code
        /// to enumerate through the children.
        /// </remarks>
        internal Visual[] InternalArray { get { return _items; } }

        /// <summary>
        /// Creates a VisualCollection.
        /// </summary>
        public VisualCollection(Visual parent)
        {
            if (parent == null)
            {
                throw new ArgumentNullException(nameof(parent));
            }
            _owner = parent;
        }

        internal void VerifyAPIReadOnly()
        {
            Debug.Assert(_owner != null);
            _owner.VerifyAPIReadOnly();
        }

        internal void VerifyAPIReadOnly(Visual other)
        {
            Debug.Assert(_owner != null);
            _owner.VerifyAPIReadOnly(other);
        }

        internal void VerifyAPIReadWrite()
        {
            Debug.Assert(_owner != null);
            _owner.VerifyAPIReadWrite();
            VerifyNotReadOnly();
        }

        internal void VerifyAPIReadWrite(Visual other)
        {
            Debug.Assert(_owner != null);
            _owner.VerifyAPIReadWrite(other);
            VerifyNotReadOnly();
        }

        internal void VerifyNotReadOnly()
        {
            if (IsReadOnlyInternal)
            {
                throw new InvalidOperationException(SR.Get(SRID.VisualCollection_ReadOnly)); 
            }
        }

        /// <summary>
        /// Gets the number of elements in the collection.
        /// </summary>
        public int Count
        {
            get
            {
                VerifyAPIReadOnly();

                return InternalCount;
            }
        }

        /// <summary>
        /// True if the collection allows modifications, otherwise false.
        /// </summary>
        public bool IsReadOnly
        {
            get
            {
                VerifyAPIReadOnly();

                return IsReadOnlyInternal;
            }
        }

        /// <summary>
        /// Gets a value indicating whether access to the ICollection
        /// is synchronized (thread-safe).
        /// </summary>
        public bool IsSynchronized
        {
            get
            {
                VerifyAPIReadOnly();

                return false;
            }
        }


        /// <summary>
        /// Gets an object that can be used to synchronize access
        /// to the ICollection.
        ///
        /// ??? Figure out what we need to return here. We do have context
        /// affinity which renders this property useless.
        ///
        /// ArrayList returns "this". I am still not sure what this is
        /// used for. Check!
        /// </summary>
        public object SyncRoot
        {
            get
            {
                VerifyAPIReadOnly();

                return this;
            }
        }

        /// <summary>
        /// Copies the Visual collection to the specified array starting at the specified index.
        /// </summary>
        public void CopyTo(Array array, int index)
        {
            VerifyAPIReadOnly();

            if (array == null)
            {
                throw new ArgumentNullException(nameof(array));
            }

            if (array.Rank != 1)
            {
                throw new ArgumentException(SR.Get(SRID.Collection_BadRank));
            }

            if ((index < 0) ||
                (array.Length - index < _size))
            {
                throw new ArgumentOutOfRangeException(nameof(index));
            }

            // System.Array does not have a CopyTo method that takes a count. Therefore
            // the loop is programmed here out.
            for (int i=0; i < _size; i++)
            {
                array.SetValue(_items[i], i+index);
            }
}

        /// <summary>
        /// Copies the Visual collection to the specified array starting at the specified index.
        /// </summary>
        public void CopyTo(Visual[] array, int index)
        {
            // Remark: This is the strongly typed version of the ICollection.CopyTo method.
            // FXCop requires us to implement this method.

            VerifyAPIReadOnly();

            if (array == null)
            {
                throw new ArgumentNullException(nameof(array));
            }

            if ((index < 0) ||
                (array.Length - index < _size))
            {
                throw new ArgumentOutOfRangeException(nameof(index));
            }

            // System.Array does not have a CopyTo method that takes a count. Therefore
            // the loop is programmed here out.
            for (int i=0; i < _size; i++)
            {
                array[i+index] = _items[i];
            }
        }


        // ----------------------------------------------------------------
        // ArrayList like operations for the VisualCollection
        // ----------------------------------------------------------------


        /// <summary>
        /// Ensures that the capacity of this list is at least the given minimum
        /// value. If the currect capacity of the list is less than min, the
        /// capacity is increased to min.
        /// </summary>
        private void EnsureCapacity(int min)
        {
            if (InternalCapacity < min)
            {
                InternalCapacity = Math.Max(min, (int)(InternalCapacity * c_growFactor));
            }
        }

        /// <summary>
        /// InternalCapacity sets/gets the Capacity of the collection.
        /// </summary>
        internal int InternalCapacity
        {
            get
            {
                return _items != null ? _items.Length : 0;
            }
            set
            {
                int currentCapacity = _items != null ? _items.Length : 0;
                if (value != currentCapacity)
                {
                    if (value < _size)
                    {
                        throw new ArgumentOutOfRangeException(nameof(value), SR.Get(SRID.VisualCollection_NotEnoughCapacity));
                    }
                    if (value > 0)
                    {
                        Visual[] newItems = new Visual[value];
                        if (_size > 0)
                        {
                            Debug.Assert(_items != null);
                            Array.Copy(_items, 0, newItems, 0, _size);
                        }
                        _items = newItems;
                    }
                    else
                    {
                        Debug.Assert(value == 0, "There shouldn't be a case where value != 0.");
                        Debug.Assert(_size == 0, "Size must be 0 here.");
                        _items = null;
                    }
                }
            }
        }

        /// <summary>
        /// Gets or sets the number of elements that the VisualCollection can contain.
        /// </summary>
        /// <value>
        /// The number of elements that the VisualCollection can contain.
        /// </value>
        /// <remarks>
        /// Capacity is the number of elements that the VisualCollection is capable of storing.
        /// Count is the number of Visuals that are actually in the VisualCollection.
        ///
        /// Capacity is always greater than or equal to Count. If Count exceeds
        /// Capacity while adding elements, the capacity of the VisualCollection is increased.
        ///
        /// By default the capacity is 0.
        /// </remarks>
        /// <exception cref="ArgumentOutOfRangeException">Capacity is set to a value that is less than Count.</exception>
        public int Capacity
        {
            get
            {
                VerifyAPIReadOnly();

                return InternalCapacity;
            }
            set
            {
                VerifyAPIReadWrite();

                InternalCapacity = value;
            }
        }

        /// <summary>
        /// Indexer for the VisualCollection. Gets or sets the Visual stored at the
        /// zero-based index of the VisualCollection.
        /// </summary>
        /// <remarks>This property provides the ability to access a specific Visual in the
        /// VisualCollection by using the following systax: <c>myVisualCollection[index]</c>.</remarks>
        /// <exception cref="ArgumentOutOfRangeException"><c>index</c> is less than zero -or- <c>index</c> is equal to or greater than Count.</exception>
        /// <exception cref="ArgumentException">If the new child has already a parent or if the slot a the specified index is not null.</exception>
        public Visual this[int index]
        {
            get
            {
                // We should likely skip the context checks here for performance reasons.
                //     MediaSystem.VerifyContext(_owner); The guy who gets the Visual won't be able to access the context
                //     the Visual anyway if he is in the wrong context.

                // Disable PREsharp warning about throwing exceptions in property
                // get methods

#pragma warning disable 6503
                if (index < 0 || index >= _size) throw new ArgumentOutOfRangeException(nameof(index));
                return _items[index];
#pragma warning restore 6503
            }
            set
            {
                VerifyAPIReadWrite(value);

                if (index < 0 || index >= _size) throw new ArgumentOutOfRangeException(nameof(index));

                Visual child = _items[index];

                if ((value == null) && (child != null))
                {
                    DisconnectChild(index);
                }
                else if (value != null)
                {
                    if (child != null)
                    {
                        throw new System.ArgumentException(SR.Get(SRID.VisualCollection_EntryInUse));
                    }
                    if ((value._parent != null) // Only a visual that isn't a visual parent or
                        || value.IsRootElement) // are a root node of a visual target can be set into the collection.
                    {
                        throw new System.ArgumentException(SR.Get(SRID.VisualCollection_VisualHasParent));
                    }

                    ConnectChild(index, value);
                }
            }
        }

        /// <summary>
        /// Sets the specified visual at the specified index into the child
        /// collection. It also corrects the parent.
        /// Note that the function requires that _item[index] == null and it
        /// also requires that the passed in child is not connected to another Visual.
        /// </summary>
        /// <exception cref="ArgumentException">If the new child has already a parent or if the slot a the specified index is not null.</exception>
        private void ConnectChild(int index, Visual value)
        {
            //
            // -- Approved By The Core Team --
            //
            // Do not allow foreign threads to change the tree.
            // (This is a noop if this object is not assigned to a Dispatcher.)
            //
            // We also need to ensure that the tree is homogenous with respect
            // to the dispatchers that the elements belong to.
            //
            _owner.VerifyAccess();
            value.VerifyAccess();
            
            // It is invalid to modify the children collection that we 
            // might be iterating during a property invalidation tree walk.
            if (_owner.IsVisualChildrenIterationInProgress)
            {
                throw new InvalidOperationException(SR.Get(SRID.CannotModifyVisualChildrenDuringTreeWalk));
            }

            Debug.Assert(value != null);
            Debug.Assert(_items[index] == null);
            Debug.Assert(value._parent == null);
            Debug.Assert(!value.IsRootElement);

            value._parentIndex = index;
            _items[index] = value;
            IncrementVersion();

            // Notify the Visual tree about the children changes. 
            _owner.InternalAddVisualChild(value);
        }

        /// <summary>
        /// Disconnects a child.
        /// </summary>
        private void DisconnectChild(int index)
        {
            Debug.Assert(_items[index] != null);

            Visual child = _items[index];

            //
            // -- Approved By The Core Team --
            //
            // Do not allow foreign threads to change the tree.
            // (This is a noop if this object is not assigned to a Dispatcher.)
            //
            child.VerifyAccess();
            
            Visual oldParent = VisualTreeHelper.GetContainingVisual2D(child._parent);
            int oldParentIndex = child._parentIndex;

            // It is invalid to modify the children collection that we 
            // might be iterating during a property invalidation tree walk.
            if (oldParent.IsVisualChildrenIterationInProgress)
            {
                throw new InvalidOperationException(SR.Get(SRID.CannotModifyVisualChildrenDuringTreeWalk));
            }

            _items[index] = null;

#if DEBUG
            child._parentIndex = -1;
#endif            
            IncrementVersion();

            _owner.InternalRemoveVisualChild(child);
        }

        /// <summary>
        /// Appends a Visual to the end of the VisualCollection.
        /// </summary>
        /// <param name="visual">The Visual to be added to the end of the VisualCollection.</param>
        /// <returns>The VisualCollection index at which the Visual has been added.</returns>
        /// <remarks>Adding a null is allowed.</remarks>
        /// <exception cref="ArgumentException">If the new child has already a parent.</exception>
        public int Add(Visual visual)
        {
            VerifyAPIReadWrite(visual);

            if ((visual != null) &&
                ((visual._parent != null)   // Only visuals that are not connected to another tree
                 || visual.IsRootElement))  // or a visual target can be added.
            {
                throw new System.ArgumentException(SR.Get(SRID.VisualCollection_VisualHasParent));
            }


            if ((_items == null) || (_size == _items.Length))
            {
                EnsureCapacity(_size+1);
            }
            int addedPosition = _size++;
            Debug.Assert(_items[addedPosition] == null);
            if (visual != null)
            {
                ConnectChild(addedPosition, visual);
            }
            IncrementVersion();
            return addedPosition;
        }

        /// <summary>
        /// Returns the zero-based index of the Visual. If the Visual is not
        /// in the VisualCollection -1 is returned. If null is passed to the method, the index
        /// of the first entry with null is returned. If there is no null entry -1 is returned.
        /// </summary>
        /// <param name="visual">The Visual to locate in the VisualCollection.</param>
        /// <remark>Runtime of this method is constant if the argument is not null. If the argument is
        /// null, the runtime of this method is linear in the size of the collection.
        /// </remark>
        public int IndexOf(Visual visual)
        {
            VerifyAPIReadOnly();

            if (visual == null)
            {
                // If the passed in argument is null, we find the first index with a null
                // entry and return it.
                for (int i = 0; i < _size; i++)
                {
                    if (_items[i] == null)
                    {
                        return i;
                    }
                }
                // No null entry found, return -1.
                return -1;
            }
            else if (visual._parent != _owner)
            {
                return -1;
            }
            else
            {
                return visual._parentIndex;
            }
        }

        /// <summary>
        /// Removes the specified visual from the VisualCollection.
        /// </summary>
        /// <param name="visual">The Visual to remove from the VisualCollection.</param>
        /// <remarks>
        /// The Visuals that follow the removed Visuals move up to occupy
        /// the vacated spot. The indexes of the Visuals that are moved are
        /// also updated.
        ///
        /// If visual is null then the first null entry is removed. Note that removing
        /// a null entry is linear in the size of the collection.
        /// </remarks>
        public void Remove(Visual visual)
        {
            VerifyAPIReadWrite(visual);

            InternalRemove(visual);
        }

        private void InternalRemove(Visual visual)
        {
            int indexToRemove = -1;

            if (visual != null)
            {
                if (visual._parent != _owner)
                {
                    // If the Visual is not in this collection we silently return without
                    // failing. This is the same behavior that ArrayList implements. See
                    // also Windows OS 
                    return; 
                }

                Debug.Assert(visual._parent != null);

                indexToRemove = visual._parentIndex;
                DisconnectChild(indexToRemove);
            }
            else
            {
                // This is the case where visual == null. We then remove the first null
                // entry.
                for (int i = 0; i < _size; i++)
                {
                    if (_items[i] == null)
                    {
                        indexToRemove = i;
                        break;
                    }
                }
            }

            if (indexToRemove != -1)
            {
                --_size;

                for (int i = indexToRemove; i < _size; i++)
                {
                    Visual  child = _items[i+1];
                    if (child != null)
                    {
                        child._parentIndex = i;
                    }
                    _items[i] = child;
                }

                _items[_size] = null;
            }
        }

        private uint Version
        {
            get
            {
                // >> 1 because bit 1 is our read-only flag.  See comments
                // on the _data field.
                return _data >> 1;
            }
        }

        private void IncrementVersion()
        {
            // += 2 because bit 1 is our read-only flag.  Explicitly unchecked
            // because we expect this number to "roll over" after 2 billion calls.
            // See comments on _data field.
            unchecked
            {
                _data += 2;
            }
        }

        private bool IsReadOnlyInternal
        {
            get
            {
                // Bit 1 is our read-only flag.  See comments on the _data field.
                return (_data & 0x01) == 0x01;
            }
        }

        // Puts the collection into a ReadOnly state.  Viewport3DVisual does this
        // on construction to prevent the addition of 2D children.
        internal void SetReadOnly()
        {
            // Bit 1 is our read-only flag.  See comments on the _data field.
            _data |= 0x01;
        }

        /// <summary>
        /// Determines whether a visual is in the VisualCollection.
        /// </summary>
        public bool Contains(Visual visual)
        {
            VerifyAPIReadOnly(visual);

            if (visual == null)
            {
                for (int i=0; i < _size; i++)
                {
                    if (_items[i] == null)
                    {
                        return true;
                    }
                }
                return false;
            }
            else
            {
                return (visual._parent == _owner);
            }
        }

        /// <summary>
        /// Removes all elements from the VisualCollection.
        /// </summary>
        /// <remarks>
        /// Count is set to zero. Capacity remains unchanged.
        /// To reset the capacity of the VisualCollection,
        /// set the Capacity property directly.
        /// </remarks>
        public void Clear()
        {
            VerifyAPIReadWrite();

            for (int i=0; i < _size; i++)
            {
                if (_items[i] != null)
                {
                    Debug.Assert(_items[i]._parent == _owner);
                    DisconnectChild(i);
                }
                _items[i] = null;
            }
            _size = 0;
            IncrementVersion();
        }

        /// <summary>
        /// Inserts an element into the VisualCollection at the specified index.
        /// </summary>
        /// <param name="index">The zero-based index at which value should be inserted.</param>
        /// <param name="visual">The Visual to insert. </param>
        /// <exception cref="ArgumentOutOfRangeException">
        /// index is less than zero.
        ///
        /// -or-
        ///
        /// index is greater than Count.
        /// </exception>
        /// <remarks>
        /// If Count already equals Capacity, the capacity of the
        /// VisualCollection is increased before the new Visual
        /// is inserted.
        ///
        /// If index is equal to Count, value is added to the
        /// end of VisualCollection.
        ///
        /// The Visuals that follow the insertion point move down to
        /// accommodate the new Visual. The indexes of the Visuals that are
        /// moved are also updated.
        /// </remarks>
        public void Insert(int index, Visual visual)
        {
            VerifyAPIReadWrite(visual);

            if (index < 0 || index > _size)
            {
                throw new ArgumentOutOfRangeException(nameof(index));
            }

            if ((visual != null) &&
                ((visual._parent != null)   // Only visuals that are not connected to another tree
                 || visual.IsRootElement))  // or a visual target can be added.
            {
                throw new System.ArgumentException(SR.Get(SRID.VisualCollection_VisualHasParent));
            }

            if ((_items == null) || (_size == _items.Length))
            {
                EnsureCapacity(_size + 1);
            }

            for (int i = _size-1; i >= index; i--)
            {
                Visual child = _items[i];
                if (child != null)
                {
                    child._parentIndex = i+1;
                }
                _items[i+1] = child;
            }
            _items[index] = null;

            _size++;
            if (visual != null)
            {
                ConnectChild(index, visual);
            }
            // Note SetVisual that increments the version to ensure proper enumerator
            // functionality.
        }

        /// <summary>
        /// Removes the Visual at the specified index.
        /// </summary>
        /// <param name="index">The zero-based index of the visual to remove.</param>
        /// <exception cref="ArgumentOutOfRangeException">index is less than zero
        /// - or - index is equal or greater than count.</exception>
        /// <remarks>
        /// The Visuals that follow the removed Visuals move up to occupy
        /// the vacated spot. The indexes of the Visuals that are moved are
        /// also updated.
        /// </remarks>
        public void RemoveAt(int index)
        {
            VerifyAPIReadWrite();

            if (index < 0 || index >= _size)
            {
                throw new ArgumentOutOfRangeException(nameof(index));
            }

            InternalRemove(_items[index]);
        }


        /// <summary>
        /// Removes a range of Visuals from the VisualCollection.
        /// </summary>
        /// <param name="index">The zero-based index of the range
        /// of elements to remove</param>
        /// <param name="count">The number of elements to remove.</param>
        /// <exception cref="ArgumentOutOfRangeException">
        /// index is less than zero.
        /// -or-
        /// count is less than zero.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// index and count do not denote a valid range of elements in the VisualCollection.
        /// </exception>
        /// <remarks>
        /// The Visuals that follow the removed Visuals move up to occupy
        /// the vacated spot. The indexes of the Visuals that are moved are
        /// also updated.
        /// </remarks>
        public void RemoveRange(int index, int count)
        {
            VerifyAPIReadWrite();

            // Do we really need this extra check index >= _size.
            if (index < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(index));
            }
            if (count < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(count));
            }
            if (_size - index < count)
            {
                throw new ArgumentOutOfRangeException(nameof(index));
            }

            if (count > 0)
            {
                for (int i = index; i < index + count; i++)
                {
                    if (_items[i] != null)
                    {
                        DisconnectChild(i);
                        _items[i] = null;
                    }
                }

                _size -= count;
                for (int i = index; i < _size; i++)
                {
                    Visual child = _items[i + count];
                    if (child != null)
                    {
                        child._parentIndex = i;
                    }
                    _items[i] = child;
                    _items[i + count] = null;
                }
                IncrementVersion(); // Incrementing version number here to be consistent with the ArrayList
                            // implementation.
            }
        }

        /// <summary>
        /// Moves a child inside this collection to right before the given sibling.  Avoids unparenting / reparenting costs.
        /// This is a dangerous internal method as it moves children positions without notifying any external code.
        /// If the given sibling is null it moves the item to the end of the collection.
        /// </summary>
        /// <param name="visual"></param>
        /// <param name="destination"></param>
        internal void Move(Visual visual, Visual destination)
        {
            int newIndex;
            int oldIndex;

            Invariant.Assert(visual != null, "we don't support moving a null visual");

            if (visual._parent == _owner)
            {
                oldIndex = visual._parentIndex;
                newIndex = destination != null ? destination._parentIndex : _size;

                Debug.Assert(visual._parent != null);
                Debug.Assert(destination == null || destination._parent == visual._parent);
                Debug.Assert(newIndex >= 0 && newIndex <= _size, "New index is invalid");

                if (oldIndex != newIndex)
                {
                    if (oldIndex < newIndex)
                    {
                        // move items left to right
                        // source Visual will get the index of one before the destination Visual
                        newIndex--;

                        for (int i = oldIndex; i < newIndex; i++)
                        {
                            Visual child = _items[i + 1];
                            if (child != null)
                            {
                                child._parentIndex = i;
                            }
                            _items[i] = child;
                        }
                    }
                    else
                    {
                        // move items right to left
                        // source visual will get the index of the destination Visual, which will in turn
                        // be pushed to the right.

                        for (int i = oldIndex; i > newIndex; i--)
                        {
                            Visual child = _items[i - 1];
                            if (child != null)
                            {
                                child._parentIndex = i;
                            }
                            _items[i] = child;
                        }
                    }

                    visual._parentIndex = newIndex;
                    _items[newIndex] = visual;
                }
            }

            return;
        }


        // ----------------------------------------------------------------
        // IEnumerable Interface
        // ----------------------------------------------------------------


        /// <summary>
        /// Returns an enumerator that can iterate through the VisualCollection.
        /// </summary>
        /// <returns>Enumerator that enumerates the VisualCollection in order.</returns>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        /// <summary>
        /// Returns an enumerator that can iterate through the VisualCollection.
        /// </summary>
        /// <returns>Enumerator that enumerates the VisualCollection in order.</returns>
        public Enumerator GetEnumerator()
        {
            VerifyAPIReadOnly();

            return new Enumerator(this);
        }

        /// <summary>
        /// This is a simple VisualCollection enumerator that is based on
        /// the ArrayListEnumeratorSimple that is used for ArrayLists.
        ///
        /// The following comment is from the CLR people:
        ///   For a straightforward enumeration of the entire ArrayList,
        ///   this is faster, because it's smaller.  Benchmarks showed
        ///   this.
        /// </summary>
        public struct Enumerator : IEnumerator
        {
            private VisualCollection _collection;
            private int _index; // -1 means not started. -2 means that it reached the end.
            private uint _version;
            private Visual _currentElement;

            internal Enumerator(VisualCollection collection)
            {
                _collection = collection;
                _index = -1; // not started.
                _version = _collection.Version;
                _currentElement = null;
            }

            /// <summary>
            /// Advances the enumerator to the next element of the collection.
            /// </summary>
            public bool MoveNext()
            {
                _collection.VerifyAPIReadOnly();

                if (_version == _collection.Version)
                {
                    if ((_index > -2) && (_index < (_collection.InternalCount - 1)))
                    {
                        _index++;
                        _currentElement = _collection[_index];
                        return true;
                    }
                    else
                    {
                        _currentElement = null;
                        _index = -2; // -2 <=> reached the end.
                        return false;
                    }
                }
                else
                {
                    throw new InvalidOperationException(SR.Get(SRID.Enumerator_CollectionChanged));
                }
            }

            /// <summary>
            /// Gets the current Visual.
            /// </summary>
            object IEnumerator.Current
            {
                get
                {
                    return this.Current;
                }
            }

            /// <summary>
            /// Gets the current Visual.
            /// </summary>
            public Visual Current
            {
                get
                {
                    // Disable PREsharp warning about throwing exceptions in property
                    // get methods

#pragma warning disable 6503

                    if (_index < 0)
                    {
                        if (_index == -1)
                        {
                            // Not started.
                            throw new InvalidOperationException(SR.Get(SRID.Enumerator_NotStarted));
                        }
                        else
                        {
                            // Reached the end.
                            Debug.Assert(_index == -2);
                            throw new InvalidOperationException(SR.Get(SRID.Enumerator_ReachedEnd));
                        }
                    }
                    return _currentElement;

#pragma warning restore 6503
                }
            }


            /// <summary>
            /// Sets the enumerator to its initial position, which is before the first element in the collection.
            /// </summary>
            public void Reset()
            {
                _collection.VerifyAPIReadOnly();

                if (_version != _collection.Version)
                    throw new InvalidOperationException(SR.Get(SRID.Enumerator_CollectionChanged));
                _index = -1; // not started.
            }
        }
     }
}


