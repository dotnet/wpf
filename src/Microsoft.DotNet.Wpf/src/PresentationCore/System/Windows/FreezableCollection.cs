// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
//
// Description: This file contains the implementation of FreezableCollection<T>.
//     FreezableCollection<T> is an IList<T> implementation which implements
//     the requisite infrastructure for collections of DependencyObjects,
//     Freezables, and Animatables and which is itself an Animatable and a Freezable.
//

using MS.Internal;
using MS.Internal.KnownBoxes;
using MS.Internal.Collections;
using MS.Internal.PresentationCore;
using MS.Utility;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using System.Runtime.InteropServices;
using System.ComponentModel.Design.Serialization;
using System.Text;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Media.Media3D;
using System.Windows.Media.Animation;
using System.Windows.Media.Composition;
using System.Windows.Media.Imaging;
using System.Windows.Markup;
using System.Windows.Media.Converters;
using System.Security;
using SR=MS.Internal.PresentationCore.SR;
using SRID=MS.Internal.PresentationCore.SRID;
// These types are aliased to match the unamanaged names used in interop
using BOOL = System.UInt32;
using WORD = System.UInt16;
using Float = System.Single;

namespace System.Windows
{
    /// <summary>
    ///     FreezableCollection&lt;T&gt; is an IList&lt;T&gt; implementation which implements
    ///     the requisite infrastructure for collections of DependencyObjects,
    ///     Freezables, and Animatables and which is itself an Animatable and a Freezable.
    /// </summary>
    public class FreezableCollection<T>: Animatable, IList, IList<T>, INotifyCollectionChanged, INotifyPropertyChanged
        where T: DependencyObject
    {
        #region Constructors

        //------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------


        /// <summary>
        /// Initializes a new instance that is empty.
        /// </summary>
        public FreezableCollection()
        {
            _collection = new List<T>();
        }

        /// <summary>
        /// Initializes a new instance that is empty and has the specified initial capacity.
        /// </summary>
        /// <param name="capacity"> int - The number of elements that the new list is initially capable of storing. </param>
        public FreezableCollection(int capacity)
        {
            _collection = new List<T>(capacity);
        }

        /// <summary>
        /// Creates a FreezableCollection&lt;T&gt; with all of the same elements as "collection"
        /// </summary>
        public FreezableCollection(IEnumerable<T> collection)
        {
            // The WritePreamble and WritePostscript aren't technically necessary
            // in the constructor as of 1/20/05 but they are put here in case
            // their behavior changes at a later date

            WritePreamble();

            if (collection != null)
            {
                int count = GetCount(collection);

                if (count > 0)
                {
                    _collection = new List<T>(count);
                }
                else
                {
                    _collection = new List<T>();
                }

                foreach (T item in collection)
                {
                    if (item == null)
                    {
                        throw new System.ArgumentException(SR.Get(SRID.Collection_NoNull));
                    }

                    OnFreezablePropertyChanged(/* oldValue = */ null, item);

                    _collection.Add(item);
                }

                WritePostscript();
            }
            else
            {
                throw new ArgumentNullException("collection");
            }
        }

        #endregion Constructors

        //------------------------------------------------------
        //
        //  Public Methods
        //
        //------------------------------------------------------

        #region Public Methods

        /// <summary>
        ///     Shadows inherited Clone() with a strongly typed
        ///     version for convenience.
        /// </summary>
        public new FreezableCollection<T> Clone()
        {
            return (FreezableCollection<T>)base.Clone();
        }

        /// <summary>
        ///     Shadows inherited CloneCurrentValue() with a strongly typed
        ///     version for convenience.
        /// </summary>
        public new FreezableCollection<T> CloneCurrentValue()
        {
            return (FreezableCollection<T>)base.CloneCurrentValue();
        }




        #endregion Public Methods

        //------------------------------------------------------
        //
        //  Public Properties
        //
        //------------------------------------------------------


        #region IList<T>

        /// <summary>
        ///     Adds "value" to the list
        /// </summary>
        public void Add(T value)
        {
            AddHelper(value);
        }

        /// <summary>
        ///     Removes all elements from the list
        /// </summary>
        public void Clear()
        {
            CheckReentrancy();

            WritePreamble();

            for (int i = _collection.Count - 1; i >= 0; i--)
            {
                OnFreezablePropertyChanged(/* oldValue = */ _collection[i], /* newValue = */ null);
            }

            _collection.Clear();

            Debug.Assert(_collection.Count == 0);

            ++_version;
            WritePostscript();

            OnCollectionChanged(NotifyCollectionChangedAction.Reset, 0, null, 0, null);
        }

        /// <summary>
        ///     Determines if the list contains "value"
        /// </summary>
        public bool Contains(T value)
        {
            ReadPreamble();

            return _collection.Contains(value);
        }

        /// <summary>
        ///     Returns the index of "value" in the list
        /// </summary>
        public int IndexOf(T value)
        {
            ReadPreamble();

            return _collection.IndexOf(value);
        }

        /// <summary>
        ///     Inserts "value" into the list at the specified position
        /// </summary>
        public void Insert(int index, T value)
        {
            if (value == null)
            {
                throw new System.ArgumentException(SR.Get(SRID.Collection_NoNull));
            }

            CheckReentrancy();

            WritePreamble();

            OnFreezablePropertyChanged(/* oldValue = */ null, /* newValue = */ value);

            _collection.Insert(index, value);

            ++_version;
            WritePostscript();

            OnCollectionChanged(NotifyCollectionChangedAction.Add, 0, null, index, value);
        }

        /// <summary>
        ///     Removes "value" from the list
        /// </summary>
        public bool Remove(T value)
        {
            WritePreamble();

            // By design collections "succeed silently" if you attempt to remove an item
            // not in the collection.  Therefore we need to first verify the old value exists
            // before calling OnFreezablePropertyChanged.  Since we already need to locate
            // the item in the collection we keep the index and use RemoveAt(...) to do
            // the work.  (Windows OS #1016178)

            // We use the public IndexOf to guard our UIContext since OnFreezablePropertyChanged
            // is only called conditionally.  IList.IndexOf returns -1 if the value is not found.
            int index = IndexOf(value);

            if (index >= 0)
            {
                CheckReentrancy();

                T oldValue = _collection[index];

                OnFreezablePropertyChanged(oldValue, null);

                // we already have index from IndexOf so instead of using Remove -
                // which will search the collection a second time - we'll use RemoveAt
                _collection.RemoveAt(index);

                ++_version;
                WritePostscript();

                OnCollectionChanged(NotifyCollectionChangedAction.Remove, index, oldValue, 0, null);

                return true;
            }

            // Collection_Remove returns true, calls WritePostscript,
            // increments version, and does UpdateResource if it succeeds

            return false;
        }

        /// <summary>
        ///     Removes the element at the specified index
        /// </summary>
        public void RemoveAt(int index)
        {
            T oldValue = _collection[ index ];

            RemoveAtWithoutFiringPublicEvents(index);

            // RemoveAtWithoutFiringPublicEvents incremented the version

            WritePostscript();

            OnCollectionChanged(NotifyCollectionChangedAction.Remove, index, oldValue, 0, null);
        }


        /// <summary>
        ///     Removes the element at the specified index without firing
        ///     the public Changed event.
        ///     The caller - typically a public method - is responsible for calling
        ///     WritePostscript if appropriate.
        /// </summary>
        internal void RemoveAtWithoutFiringPublicEvents(int index)
        {
            CheckReentrancy();

            WritePreamble();

            T oldValue = _collection[ index ];

            OnFreezablePropertyChanged(oldValue, null);

            _collection.RemoveAt(index);


            ++_version;

            // No WritePostScript to avoid firing the Changed event.
        }


        /// <summary>
        ///     Indexer for the collection
        /// </summary>
        public T this[int index]
        {
            get
            {
                ReadPreamble();

                return _collection[index];
            }
            set
            {
                if (value == null)
                {
                    throw new System.ArgumentException(SR.Get(SRID.Collection_NoNull));
                }

                CheckReentrancy();

                WritePreamble();

                T oldValue = _collection[ index ];
                bool isChanging = !Object.ReferenceEquals(oldValue, value);

                if (isChanging)
                {
                    OnFreezablePropertyChanged(oldValue, value);

                    _collection[ index ] = value;
                }

                ++_version;
                WritePostscript();

                if (isChanging)
                {
                    OnCollectionChanged(NotifyCollectionChangedAction.Replace, index, oldValue, index, value);
                }
            }
        }

        #endregion

        #region ICollection<T>

        /// <summary>
        ///     The number of elements contained in the collection.
        /// </summary>
        public int Count
        {
            get
            {
                ReadPreamble();

                return _collection.Count;
            }
        }

        /// <summary>
        ///     Copies the elements of the collection into "array" starting at "index"
        /// </summary>
        public void CopyTo(T[] array, int index)
        {
            ReadPreamble();

            if (array == null)
            {
                throw new ArgumentNullException("array");
            }

            // This will not throw in the case that we are copying
            // from an empty collection.  This is consistent with the
            // BCL Collection implementations. (Windows 1587365)
            if (index < 0  || (index + _collection.Count) > array.Length)
            {
                throw new ArgumentOutOfRangeException("index");
            }

            _collection.CopyTo(array, index);
        }

        bool ICollection<T>.IsReadOnly
        {
            get
            {
                ReadPreamble();

                return IsFrozen;
            }
        }

        #endregion

        #region IEnumerable<T>

        /// <summary>
        /// Returns an enumerator for the collection
        /// </summary>
        public Enumerator GetEnumerator()
        {
            ReadPreamble();

            return new Enumerator(this);
        }

        IEnumerator<T> IEnumerable<T>.GetEnumerator()
        {
            return this.GetEnumerator();
        }

        #endregion

        #region IList

        bool IList.IsReadOnly
        {
            get
            {
                return ((ICollection<T>)this).IsReadOnly;
            }
        }

        bool IList.IsFixedSize
        {
            get
            {
                ReadPreamble();

                return IsFrozen;
            }
        }

        object IList.this[int index]
        {
            get
            {
                return this[index];
            }
            set
            {
                // Forwards to typed implementation
                this[index] = Cast(value);
            }
        }

        int IList.Add(object value)
        {
            // Forward to typed helper
            return AddHelper(Cast(value));
        }

        bool IList.Contains(object value)
        {
            return Contains(value as T);
        }

        int IList.IndexOf(object value)
        {
            return IndexOf(value as T);
        }

        void IList.Insert(int index, object value)
        {
            // Forward to IList<T> Insert
            Insert(index, Cast(value));
        }

        void IList.Remove(object value)
        {
            Remove(value as T);
        }

        #endregion

        #region ICollection

        void ICollection.CopyTo(Array array, int index)
        {
            ReadPreamble();

            if (array == null)
            {
                throw new ArgumentNullException("array");
            }

            // This will not throw in the case that we are copying
            // from an empty collection.  This is consistent with the
            // BCL Collection implementations. (Windows 1587365)
            if (index < 0  || (index + _collection.Count) > array.Length)
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
                throw new ArgumentException(SR.Get(SRID.Collection_BadDestArray, this.GetType().Name), e);
            }
        }

        bool ICollection.IsSynchronized
        {
            get
            {
                ReadPreamble();

                return IsFrozen || Dispatcher != null;
            }
        }

        object ICollection.SyncRoot
        {
            get
            {
                ReadPreamble();
                return this;
            }
        }
        #endregion

        #region IEnumerable

        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }

        #endregion

        #region INotifyCollectionChanged

        /// <summary>
        /// CollectionChanged event (per <see cref="INotifyCollectionChanged" />).
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
        /// <remarks>
        /// see <seealso cref="INotifyCollectionChanged"/>
        /// </remarks>
        private event NotifyCollectionChangedEventHandler CollectionChanged;

        /// <summary>
        /// Raise CollectionChanged event to any listeners.
        /// Properties/methods modifying this FreezableCollection will raise
        /// a collection changed event through this method.
        /// </summary>
        private void OnCollectionChanged(NotifyCollectionChangedEventArgs e)
        {
            if (CollectionChanged != null)
            {
                using (BlockReentrancy())
                {
                    CollectionChanged(this, e);
                }
            }
        }

        #endregion INotifyCollectionChanged

        #region INotifyPropertyChanged

        /// <summary>
        /// PropertyChanged event (per <see cref="INotifyPropertyChanged" />).
        /// </summary>
        event PropertyChangedEventHandler INotifyPropertyChanged.PropertyChanged
        {
            add
            {
                PrivatePropertyChanged += value;
            }
            remove
            {
                PrivatePropertyChanged -= value;
            }
        }

        /// <summary>
        /// PropertyChanged event (per <see cref="INotifyPropertyChanged" />).
        /// </summary>
        // We can't call this "PropertyChanged" because the base class Animatable
        // declares an internal method with that name.
        private event PropertyChangedEventHandler PrivatePropertyChanged;

        /// <summary>
        /// Raises a PropertyChanged event (per <see cref="INotifyPropertyChanged" />).
        /// </summary>
        private void OnPropertyChanged(PropertyChangedEventArgs e)
        {
            if (PrivatePropertyChanged != null)
            {
                PrivatePropertyChanged(this, e);
            }
        }

        #endregion INotifyPropertyChanged

        #region Internal Helpers

        /// <summary>
        ///     Freezable collections need to notify their contained Freezables
        ///     about the change in the InheritanceContext
        /// </summary>
        internal override void OnInheritanceContextChangedCore(EventArgs args)
        {
            base.OnInheritanceContextChangedCore(args);

            for (int i=0; i<this.Count; i++)
            {
                DependencyObject inheritanceChild = _collection[i];
                if (inheritanceChild!= null && inheritanceChild.InheritanceContext == this)
                {
                    inheritanceChild.OnInheritanceContextChanged(args);
                }
            }
        }

        #endregion

        #region Private Helpers

        private T Cast(object value)
        {
            if( value == null )
            {
                throw new System.ArgumentNullException("value");
            }

            if (!(value is T))
            {
                throw new System.ArgumentException(SR.Get(SRID.Collection_BadType, this.GetType().Name, value.GetType().Name, "T"));
            }

            return (T) value;
        }

        // Extracts the count for the given IEnumerable<T> by sniffing for the
        // ICollection and ICollection<T> interfaces.  If the count can not be
        // extract it return -1.
        private int GetCount(IEnumerable<T> enumerable)
        {
            ICollection collectionAsICollection = enumerable as ICollection;

            if (collectionAsICollection != null)
            {
                return collectionAsICollection.Count;
            }

            ICollection<T> enumerableAsICollectionT = enumerable as ICollection<T>;

            if (enumerableAsICollectionT != null)
            {
                return enumerableAsICollectionT.Count;
            }

            // We return -1 here and force the caller to decide how to handle
            // the unknown case.  In the future different collections might
            // use different estimates for unknown.  (e.g., Point3DCollections
            // tend to be 8+ while DoubleCollections are freqently <= 2, etc.)
            return -1;
        }

        // IList.Add returns int and IList<T>.Add does not. This
        // is called by both Adds and IList<T>'s just ignores the
        // integer
        private int AddHelper(T value)
        {
            CheckReentrancy();

            int index = AddWithoutFiringPublicEvents(value);

            // AddAtWithoutFiringPublicEvents incremented the version

            WritePostscript();

            // AddWithoutFiringPublicEvents returns the wrong answer,
            // which we adjust for here.  Fix this.
            OnCollectionChanged(NotifyCollectionChangedAction.Add, 0, null, index-1, value);

            return index;
        }

        internal int AddWithoutFiringPublicEvents(T value)
        {
            if (value == null)
            {
                throw new System.ArgumentException(SR.Get(SRID.Collection_NoNull));
            }
            WritePreamble();
            T newValue = value;
            OnFreezablePropertyChanged(/* oldValue = */ null, newValue);
            _collection.Add(value);


            ++_version;

            // No WritePostScript to avoid firing the Changed event.

            // Fix this - it's off by one (too large)
            return _collection.Count;
        }

        // helper to raise events after the collection has changed
        private void OnCollectionChanged(NotifyCollectionChangedAction action,
                                            int oldIndex,
                                            T oldValue,
                                            int newIndex,
                                            T newValue)
        {
            if (PrivatePropertyChanged == null && CollectionChanged == null)
                return;

            using (BlockReentrancy())
            {
                // most collection changes imply a change in the Count and indexer
                // properties
                if (PrivatePropertyChanged != null)
                {
                    if (action != NotifyCollectionChangedAction.Replace &&
                        action != NotifyCollectionChangedAction.Move)
                    {
                        OnPropertyChanged(new PropertyChangedEventArgs(CountPropertyName));
                    }
                    OnPropertyChanged(new PropertyChangedEventArgs(IndexerPropertyName));
                }

                if (CollectionChanged != null)
                {
                    NotifyCollectionChangedEventArgs args;

                    switch (action)
                    {
                        case NotifyCollectionChangedAction.Reset:
                            args = new NotifyCollectionChangedEventArgs(action);
                            break;
                        case NotifyCollectionChangedAction.Add:
                            args = new NotifyCollectionChangedEventArgs(action, newValue, newIndex);
                            break;
                        case NotifyCollectionChangedAction.Remove:
                            args = new NotifyCollectionChangedEventArgs(action, oldValue, oldIndex);
                            break;
                        case NotifyCollectionChangedAction.Replace:
                            args = new NotifyCollectionChangedEventArgs(action, newValue, oldValue, newIndex);
                            break;
                        default:
                            throw new InvalidOperationException(SR.Get(SRID.Freezable_UnexpectedChange));
                    }

                    OnCollectionChanged(args);
                }
            }
        }


        #endregion Private Helpers

        //------------------------------------------------------
        //
        //  Protected Methods
        //
        //------------------------------------------------------

        #region Protected Methods

        /// <summary>
        /// Implementation of <see cref="System.Windows.Freezable.CreateInstanceCore">Freezable.CreateInstanceCore</see>.
        /// </summary>
        /// <returns>The new Freezable.</returns>
        protected override Freezable CreateInstanceCore()
        {
            return new FreezableCollection<T>();
        }

        enum CloneCommonType
        {
            Clone,
            CloneCurrentValue,
            GetAsFrozen,
            GetCurrentValueAsFrozen
        }

        private void CloneCommon(FreezableCollection<T> source,
                                 CloneCommonType cloneType)
        {
            int count = source._collection.Count;

            _collection = new List<T>(count);

            for (int i = 0; i < count; i++)
            {
                T newValue = source._collection[i];

                Freezable itemAsFreezable = newValue as Freezable;

                if (itemAsFreezable != null)
                {
                    switch (cloneType)
                    {
                    case CloneCommonType.Clone:
                        newValue = itemAsFreezable.Clone() as T;
                        break;
                    case CloneCommonType.CloneCurrentValue:
                        newValue = itemAsFreezable.CloneCurrentValue() as T;
                        break;
                    case CloneCommonType.GetAsFrozen:
                        newValue = itemAsFreezable.GetAsFrozen() as T;
                        break;
                    case CloneCommonType.GetCurrentValueAsFrozen:
                        newValue = itemAsFreezable.GetCurrentValueAsFrozen() as T;
                        break;
                    default:
                        Invariant.Assert(false, "Invalid CloneCommonType encountered.");
                        break;
                    }

                    if (newValue == null)
                    {
                        throw new InvalidOperationException(SR.Get(SRID.Freezable_CloneInvalidType, typeof(T).Name));
                    }
                }

                OnFreezablePropertyChanged(/* oldValue = */ null, newValue);
                _collection.Add(newValue);
            }
        }

        /// <summary>
        /// Implementation of Freezable.CloneCore()
        /// </summary>
        protected override void CloneCore(Freezable source)
        {
            base.CloneCore(source);

            FreezableCollection<T> sourceFreezableCollection = (FreezableCollection<T>) source;

            CloneCommon(sourceFreezableCollection, CloneCommonType.Clone);
        }

        /// <summary>
        /// Implementation of Freezable.CloneCurrentValueCore()
        /// </summary>
        protected override void CloneCurrentValueCore(Freezable source)
        {
            base.CloneCurrentValueCore(source);

            FreezableCollection<T> sourceFreezableCollection = (FreezableCollection<T>) source;

            CloneCommon(sourceFreezableCollection, CloneCommonType.CloneCurrentValue);
        }
        /// <summary>
        /// Implementation of Freezable.GetAsFrozenCore()
        /// </summary>
        protected override void GetAsFrozenCore(Freezable source)
        {
            base.GetAsFrozenCore(source);

            FreezableCollection<T> sourceFreezableCollection = (FreezableCollection<T>) source;

            CloneCommon(sourceFreezableCollection, CloneCommonType.GetAsFrozen);
        }
        /// <summary>
        /// Implementation of Freezable.GetCurrentValueAsFrozenCore()
        /// </summary>
        protected override void GetCurrentValueAsFrozenCore(Freezable source)
        {
            base.GetCurrentValueAsFrozenCore(source);

            FreezableCollection<T> sourceFreezableCollection = (FreezableCollection<T>) source;

            CloneCommon(sourceFreezableCollection, CloneCommonType.GetCurrentValueAsFrozen);
        }

        /// <summary>
        /// Implementation of <see cref="System.Windows.Freezable.FreezeCore">Freezable.FreezeCore</see>.
        /// </summary>
        protected override bool FreezeCore(bool isChecking)
        {
            bool canFreeze = base.FreezeCore(isChecking);

            int count = _collection.Count;
            for (int i = 0; i < count && canFreeze; i++)
            {
                T item = _collection[i];
                Freezable itemAsFreezable = item as Freezable;

                if (itemAsFreezable != null)
                {
                    canFreeze &= Freezable.Freeze(itemAsFreezable, isChecking);
                }
                else
                {
                    canFreeze &= (item.Dispatcher == null);
                }
            }

            return canFreeze;
        }

        /// <summary>
        /// Disallow reentrant attempts to change this collection. E.g. a event handler
        /// of the CollectionChanged event is not allowed to make changes to this collection.
        /// </summary>
        /// <remarks>
        /// typical usage is to wrap e.g. a OnCollectionChanged call with a using() scope:
        /// <code>
        ///         using (BlockReentrancy())
        ///         {
        ///             CollectionChanged(this, new NotifyCollectionChangedEventArgs(action, item, index));
        ///         }
        /// </code>
        /// </remarks>
        private IDisposable BlockReentrancy()
        {
            _monitor.Enter();
            return _monitor;
        }

        /// <summary> Check and assert for reentrant attempts to change this collection. </summary>
        /// <exception cref="InvalidOperationException"> raised when changing the collection
        /// while another collection change is still being notified to other listeners </exception>
        private void CheckReentrancy()
        {
            if (_monitor.Busy)
            {
                throw new InvalidOperationException(SR.Get(SRID.Freezable_Reentrant));
            }
        }

        #endregion ProtectedMethods

        //------------------------------------------------------
        //
        //  Internal Fields
        //
        //------------------------------------------------------

        #region Internal Fields

        internal List<T> _collection;
        internal uint _version = 0;

        #endregion Internal Fields

        //------------------------------------------------------
        //
        //  Private Fields
        //
        //------------------------------------------------------

        #region Private Fields

        private const string CountPropertyName = "Count";

        // This must agree with Binding.IndexerName.  It is declared separately
        // here so as to avoid a dependency on PresentationFramework.dll.
        private const string IndexerPropertyName = "Item[]";

        private SimpleMonitor _monitor = new SimpleMonitor();

        #endregion Private Fields

        #region Enumerator
        /// <summary>
        /// Enumerates the items in a TCollection
        /// </summary>
        public struct Enumerator : IEnumerator, IEnumerator<T>
        {
            #region Constructor

            internal Enumerator(FreezableCollection<T> list)
            {
                Debug.Assert(list != null, "list may not be null.");

                _list = list;
                _version = list._version;
                _index = -1;
                _current = default(T);
            }

            #endregion

            #region Methods

            void IDisposable.Dispose()
            {
}

            /// <summary>
            /// Advances the enumerator to the next element of the collection.
            /// </summary>
            /// <returns>
            /// true if the enumerator was successfully advanced to the next element,
            /// false if the enumerator has passed the end of the collection.
            /// </returns>
            public bool MoveNext()
            {
                _list.ReadPreamble();

                if (_version == _list._version)
                {
                    if (_index > -2 && _index < _list._collection.Count - 1)
                    {
                        _current = _list._collection[++_index];
                        return true;
                    }
                    else
                    {
                        _index = -2; // -2 indicates "past the end"
                        return false;
                    }
                }
                else
                {
                    throw new InvalidOperationException(SR.Get(SRID.Enumerator_CollectionChanged));
                }
            }

            /// <summary>
            /// Sets the enumerator to its initial position, which is before the
            /// first element in the collection.
            /// </summary>
            public void Reset()
            {
                _list.ReadPreamble();

                if (_version == _list._version)
                {
                    _index = -1;
                }
                else
                {
                    throw new InvalidOperationException(SR.Get(SRID.Enumerator_CollectionChanged));
                }
            }

            #endregion

            #region Properties

            object IEnumerator.Current
            {
                get
                {
                    return this.Current;
                }
            }

            /// <summary>
            /// Current element
            ///
            /// The behavior of IEnumerable&lt;T>.Current is undefined
            /// before the first MoveNext and after we have walked
            /// off the end of the list. However, the IEnumerable.Current
            /// contract requires that we throw exceptions
            /// </summary>
            public T Current
            {
                get
                {
                    if (_index > -1)
                    {
                        return _current;
                    }
                    else if (_index == -1)
                    {
                        throw new InvalidOperationException(SR.Get(SRID.Enumerator_NotStarted));
                    }
                    else
                    {
                        Debug.Assert(_index == -2, "expected -2, got " + _index + "\n");
                        throw new InvalidOperationException(SR.Get(SRID.Enumerator_ReachedEnd));
                    }
                }
            }

            #endregion

            #region Data
            private T _current;
            private FreezableCollection<T> _list;
            private uint _version;
            private int _index;
            #endregion
        }

        private class SimpleMonitor : IDisposable
        {
            public void Enter()
            {
                ++ _busyCount;
            }

            public void Dispose()
            {
                -- _busyCount;
                GC.SuppressFinalize(this);
            }

            public bool Busy { get { return _busyCount > 0; } }

            int _busyCount;
        }

        #endregion
    }
}
