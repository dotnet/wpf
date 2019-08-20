// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
//
//

#pragma warning disable 1634, 1691  // suppressing PreSharp warnings

using MS.Utility;
using MS.Internal;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Windows;
using MS.Internal.PresentationCore;

using SR=MS.Internal.PresentationCore.SR;
using SRID=MS.Internal.PresentationCore.SRID;

namespace System.Windows.Media.Media3D
{
    /// <summary>
    ///     A collection of Visual3D objects.
    /// </summary>
    public sealed class Visual3DCollection : IList, IList<Visual3D>
    {
        //------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------

        #region Constructors

        internal Visual3DCollection(IVisual3DContainer owner)
        {
            _owner = owner;
        }

        #endregion Constructors

        //------------------------------------------------------
        //
        //  Public Methods
        //
        //------------------------------------------------------

        #region Public Methods

        /// <summary>
        ///     Adds the value to the collection.
        /// </summary>
        public void Add(Visual3D value)
        {
            VerifyAPIForAdd(value);

            int addedPosition = InternalCount;
            _collection.Add(value);
            InvalidateEnumerators();
            
            // NOTE: The collection must be updated before notifying the Visual.
            ConnectChild(addedPosition, value);

            Debug_ICC();
        }

        private void ConnectChild(int index, Visual3D value)
        {
            value.ParentIndex = index;
            _owner.AddChild(value);
        }

        /// <summary>
        ///     Inserts the value into the list at the specified position
        /// </summary>
        public void Insert(int index, Visual3D value)
        {
            VerifyAPIForAdd(value);

            InternalInsert(index, value);
        }

        /// <summary>
        ///     Removes the value from the collection.
        /// </summary>
        public bool Remove(Visual3D value)
        {
            VerifyAPIReadWrite(value);

            if (!_collection.Contains(value))
            {
                return false;
            }

            InternalRemoveAt(value.ParentIndex);

            return true;
        }

        /// <summary>
        ///     Removes the value at the specified index.
        /// </summary>
        public void RemoveAt(int index)
        {
            if (index < 0 || index >= InternalCount)
            {
                throw new ArgumentOutOfRangeException("index");
            }

            VerifyAPIReadWrite(_collection[index]);

            InternalRemoveAt(index);
        }

        /// <summary>
        ///     Removes all IElements from the collection.
        /// </summary>
        public void Clear()
        {
            VerifyAPIReadWrite();

            // Rather than clear, we swap out the FrugalStructList because
            // we need to keep the old values around to notify the parent
            // they were removed.
            FrugalStructList<Visual3D> oldCollection = _collection;
            _collection = new FrugalStructList<Visual3D>();
            InvalidateEnumerators();

            // NOTE: The collection must be updated before notifying the Visual.
            for (int i = oldCollection.Count - 1; i >= 0; i--)
            {                
                _owner.RemoveChild(oldCollection[i]);
            }

            Debug_ICC();
        }

        /// <summary>
        ///     Copies the IElements of the collection into "array" starting at "index"
        /// </summary>
        public void CopyTo(Visual3D[] array, int index)
        {
            VerifyAPIReadOnly();

            if (array == null)
            {
                throw new ArgumentNullException("array");
            }

            // The extra "index >= array.Length" check in because even if _collection.Count
            // is 0 the index is not allowed to be equal or greater than the length
            // (from the MSDN ICollection docs)
            if (index < 0 || index >= array.Length || (index + _collection.Count) > array.Length)
            {
                throw new ArgumentOutOfRangeException("index");
            }

            _collection.CopyTo(array, index);
        }

        void ICollection.CopyTo(Array array, int index)
        {
            VerifyAPIReadOnly();

            if (array == null)
            {
                throw new ArgumentNullException("array");
            }

            // The extra "index >= array.Length" check in because even if _collection.Count
            // is 0 the index is not allowed to be equal or greater than the length
            // (from the MSDN ICollection docs)
            if (index < 0 || index >= array.Length || (index + _collection.Count) > array.Length)
            {
                throw new ArgumentOutOfRangeException("index");
            }

            if (array.Rank != 1)
            {
                throw new ArgumentException(SR.Get(SRID.Collection_BadRank));
            }

            // Elsewhere in the collection we throw an AE when the type is
            // bad so we do it here as well to be consistent
            try
            {
                int count = _collection.Count;
                for (int i = 0; i < count; i++)
                {
                    array.SetValue(_collection[i], index + i);
                }
            }
            catch (InvalidCastException e)
            {
                throw new ArgumentException(SR.Get(SRID.Collection_BadDestArray, "Visual3DCollection"), e);
            }
        }

        /// <summary>
        ///     Determines if the list contains "value"
        /// </summary>
        public bool Contains(Visual3D value)
        {
            VerifyAPIReadOnly(value);

            return (value != null && (value.InternalVisualParent == _owner));
        }

        /// <summary>
        ///     Returns the index of value in the list
        /// </summary>
        public int IndexOf(Visual3D value)
        {
            VerifyAPIReadOnly(value);

            if (value == null || (value.InternalVisualParent != _owner))
            {
                return -1;
            }

#pragma warning disable 56506 // Suppress presharp warning: Parameter 'value' to this public method must be validated:  A null-dereference can occur here.
            return value.ParentIndex;
#pragma warning restore 56506
        }

        /// <summary>
        ///     Returns an Enumerator for the collection.
        /// </summary>
        public Enumerator GetEnumerator()
        {
            VerifyAPIReadOnly();

            return new Enumerator(this);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        IEnumerator<Visual3D> IEnumerable<Visual3D>.GetEnumerator()
        {
            return GetEnumerator();
        }

        #endregion Public Methods

        //------------------------------------------------------
        //
        //  Public Properties
        //
        //------------------------------------------------------

        #region Public Properties

        /// <summary>
        ///     Returns the IElement at the given index in the collection.
        /// </summary>
        public Visual3D this[int index]
        {
            get
            {
                VerifyAPIReadOnly();

                return InternalGetItem(index);
            }
            set
            {
                if (index < 0 || index >= InternalCount)
                {
                    throw new ArgumentOutOfRangeException("index");
                }

                VerifyAPIForAdd(value);

                InternalRemoveAt(index);
                InternalInsert(index, value);
            }
        }

        /// <summary>
        ///     The number of IElements contained in the collection.
        /// </summary>
        public int Count
        {
            get
            {
                VerifyAPIReadOnly();

                return InternalCount;
            }
        }

        bool ICollection.IsSynchronized
        {
            get
            {
                VerifyAPIReadOnly();

                // True because we force single thread access via VerifyAccess()
                return true;
            }
        }

        object ICollection.SyncRoot
        {
            get
            {
                VerifyAPIReadOnly();

                return _owner;
            }
        }

        bool ICollection<Visual3D>.IsReadOnly
        {
            get
            {
                VerifyAPIReadOnly();

                return false;
            }
        }

        #region IList Members

        /// <summary>
        /// Adds an element to the Visual3DCollection
        /// </summary>
        int IList.Add(object value)
        {
            Add(Cast(value));
            return InternalCount - 1;
        }

        /// <summary>
        /// Determines whether an element is in the Visual3DCollection.
        /// </summary>
        bool IList.Contains(object value)
        {
            return Contains(value as Visual3D);
        }

        /// <summary>
        /// Returns the index of the element in the Visual3DCollection
        /// </summary>
        int IList.IndexOf(object value)
        {
            return IndexOf(value as Visual3D);
        }

        /// <summary>
        /// Inserts an element into the Visual3DCollection
        /// </summary>
        void IList.Insert(int index, object value)
        {
            Insert(index, Cast(value));
        }

        /// <summary>
        /// </summary>
        bool IList.IsFixedSize
        {
            get { return false; }
        }

        /// <summary>
        /// </summary>
        bool IList.IsReadOnly
        {
            get { return false; }
        }

        /// <summary>
        /// Removes an element from the Visual3DCollection
        /// </summary>
        void IList.Remove(object value)
        {
            Remove(value as Visual3D);
        }

        /// <summary>
        /// For more details, see <see cref="System.Windows.Media.VisualCollection" />
        /// </summary>
        object IList.this[int index]
        {
            get
            {
                return this[index];
            }
            set
            {
                this[index] = Cast(value);
            }
        }

        #endregion

        #endregion Public Properties

        //------------------------------------------------------
        //
        //  Public Events
        //
        //------------------------------------------------------

        //------------------------------------------------------
        //
        //  Internal Methods
        //
        //------------------------------------------------------

        #region Internal Methods

        internal Visual3D InternalGetItem(int index)
        {
            return _collection[index];
        }       

        #endregion Internal Methods

        //------------------------------------------------------
        //
        //  Internal Properties
        //
        //------------------------------------------------------

        #region Internal Properties

        internal int InternalCount
        {
            get { return _collection.Count; }
        }

        #endregion Internal Properties

        //------------------------------------------------------
        //
        //  Private Methods
        //
        //------------------------------------------------------

        #region Private Methods

        private void VerifyAPIReadOnly()
        {
            Debug_ICC();
            _owner.VerifyAPIReadOnly();
        }

        private void VerifyAPIReadOnly(Visual3D other)
        {
            Debug_ICC();
            _owner.VerifyAPIReadOnly(other);
        }

        private void VerifyAPIReadWrite()
        {
            Debug_ICC();
            _owner.VerifyAPIReadWrite();
        }

        private void VerifyAPIReadWrite(Visual3D other)
        {
            Debug_ICC();
            _owner.VerifyAPIReadWrite(other);
        }

        private Visual3D Cast(object value)
        {
            if( value == null )
            {
                throw new System.ArgumentNullException("value");
            }
            
            if (!(value is Visual3D))
            {
                throw new System.ArgumentException(SR.Get(SRID.Collection_BadType, this.GetType().Name, value.GetType().Name, "Visual3D"));
            }

            return (Visual3D) value;
        }

        private void VerifyAPIForAdd(Visual3D value)
        {
            if (value == null)
            {
                throw new System.ArgumentException(SR.Get(SRID.Collection_NoNull));
            }

            VerifyAPIReadWrite(value);

            if (value.InternalVisualParent != null)
            {
                throw new System.ArgumentException(SR.Get(SRID.VisualCollection_VisualHasParent));
            }
        }

        private void InternalInsert(int index, Visual3D value)
        {            
            _collection.Insert(index, value);

            // Update ParentIndex value on each Visual3D.  Run through them
            // and increment.  Note that this means that Inserting/Removal from a 
            // Visual3DCollection can be O(n^2) if done in the wrong order.
            for (int i = index + 1, count = InternalCount; i < count; i++)
            {
                Debug.Assert(InternalGetItem(i).ParentIndex == i - 1,
                    "ParentIndex has been corrupted.");
                
                InternalGetItem(i).ParentIndex = i;
            }
            
            InvalidateEnumerators();
            
            // NOTE: The collection must be updated before notifying the Visual.
            ConnectChild(index, value);

            Debug_ICC();
        }

        private void InternalRemoveAt(int index)
        {
            Visual3D value = _collection[index];

            _collection.RemoveAt(index);
            // Update ParentIndices after the modified index are now invalid.  Run through them
            // and decrement.  Note that this means that Inserting/Removal from a 
            // Visual3DCollection can be O(n^2) if done in the wrong order.
            for (int i = index; i < InternalCount; i++)
            {
                Debug.Assert(InternalGetItem(i).ParentIndex == i + 1,
                    "ParentIndex has been corrupted.");
                
                InternalGetItem(i).ParentIndex = i;
            }
            
            InvalidateEnumerators();
            
            // NOTE: The collection must be updated before notifying the Visual.
            _owner.RemoveChild(value);

            Debug_ICC();
        }

        // Each member which modifies the collection should call this method to
        // invalidate any enumerators which might have been handed out.
        private void InvalidateEnumerators()
        {
            _version++;
        }

        #endregion Private Methods

        //------------------------------------------------------
        //
        //  DEBUG
        //
        //------------------------------------------------------

        #region DEBUG

        [Conditional("DEBUG")]
        private void Debug_ICC()
        {
            Debug.Assert(_owner != null, "How did an Visual3DCollection get constructed without an owner?");

            Dictionary<Visual3D, string> duplicates = new Dictionary<Visual3D, string>();

            for (int i = 0; i < _collection.Count; i++)
            {
                Visual3D visual = _collection[i];

                Debug.Assert(!duplicates.ContainsKey(visual), "How did the visual get re-inserted?");
                duplicates.Add(visual, String.Empty);

                Debug.Assert(visual.InternalVisualParent == _owner, "Why isn't our child's parent pointer the same as the collection owner?");
                Debug.Assert(visual.ParentIndex == i,
                    String.Format(
                        CultureInfo.InvariantCulture,
                        "Child's ParentIndex does not match the child's actual position in the collection. Expected='{0}' Actual='{1}'",
                        i,
                        visual.ParentIndex));

                // If the Visual3D is being added to the collection via a resource reference
                // its inheritance context will be the owner of the ResourceDictionary in which
                // it was declared.  (Creating a 3D scene by loading loose xaml containing resources
                // causes asserts to fail in Visual3DCollection.Debug_ICC())
                //
                // Debug.Assert(visual.InheritanceContext == _inheritanceContext,
                //    "How did a Visual3D get inserted without updating it's InheritanceContext?");
            }
        }

        #endregion DEBUG

        //------------------------------------------------------
        //
        //  Private Fields
        //
        //------------------------------------------------------

        #region Private Fields

        private IVisual3DContainer _owner = null;       
        private FrugalStructList<Visual3D> _collection = new FrugalStructList<Visual3D>();
        private int _version = 0;

        #endregion Private Fields

        //------------------------------------------------------
        //
        //  Enumerator
        //
        //------------------------------------------------------

        #region Enumerator

        /// <summary>
        ///     VisualCollection Enumerator.
        /// </summary>
        public struct Enumerator : IEnumerator<Visual3D>, IEnumerator
        {
            #region Constructors

            internal Enumerator(Visual3DCollection list)
            {
                Debug.Assert(list != null, "list may not be null.");

                _list = list;
                _index = -1;
                _version = _list._version;
            }

            #endregion Constructors

            #region Public Methods

            /// <summary>
            ///     Advances the enumerator to the next IElement of the collection.
            /// </summary>
            public bool MoveNext()
            {
                if (_list._version != _version)
                {
                    throw new InvalidOperationException(SR.Get(SRID.Enumerator_CollectionChanged));
                }

                int count = _list.Count;

                if (_index < count)
                {
                    _index++;
                }

                return _index < count;
            }

            /// <summary>
            ///     Resets the enumerator to its initial position.
            /// </summary>
            public void Reset()
            {
                if (_list._version != _version)
                {
                    throw new InvalidOperationException(SR.Get(SRID.Enumerator_CollectionChanged));
                }

                _index = -1;
            }

            void IDisposable.Dispose()
            {
                // Do nothing - Required by the IEnumeable contract.
            }

            #endregion Public Methods

            #region Public Properties

            object IEnumerator.Current
            {
                get
                {
                    return this.Current;
                }
            }

            /// <summary>
            ///     Returns the current IElement.
            /// </summary>
            public Visual3D Current
            {
#pragma warning disable 1634, 1691
#pragma warning disable 6503
                get
                {
                    if ((_index < 0) || (_index >= _list.Count))
                    {
                        throw new InvalidOperationException(SR.Get(SRID.Enumerator_VerifyContext));
                    }

                    return _list[_index];
                }
#pragma warning restore 6503
#pragma warning restore 1634, 1691
            }

            #endregion Public Methods

            #region Private Fields

            private Visual3DCollection _list;
            private int _index;
            private int _version;

            #endregion Private Fields
        }

        #endregion Enumerator
    }
}

