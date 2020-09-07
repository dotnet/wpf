// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// This file was generated, please do not edit it directly.
//
// Please see MilCodeGen.html for more information.
//
//---------------------------------------------------------------------------

using MS.Internal;
using MS.Internal.KnownBoxes;
using MS.Internal.Collections;
using MS.Internal.PresentationCore;
using MS.Utility;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using System.Runtime.InteropServices;
using System.ComponentModel.Design.Serialization;
using System.Text;
using System.Windows;
using System.Windows.Media;
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

namespace System.Windows.Media.Animation
{
    /// <summary>
    /// A collection of Timeline objects.
    /// </summary>


    public sealed partial class TimelineCollection : Animatable, IList, IList<Timeline>
    {
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
        public new TimelineCollection Clone()
        {
            return (TimelineCollection)base.Clone();
        }

        /// <summary>
        ///     Shadows inherited CloneCurrentValue() with a strongly typed
        ///     version for convenience.
        /// </summary>
        public new TimelineCollection CloneCurrentValue()
        {
            return (TimelineCollection)base.CloneCurrentValue();
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
        public void Add(Timeline value)
        {
            AddHelper(value);
        }

        /// <summary>
        ///     Removes all elements from the list
        /// </summary>
        public void Clear()
        {
            WritePreamble();

            for (int i = _collection.Count - 1; i >= 0; i--)
            {
                OnFreezablePropertyChanged(/* oldValue = */ _collection[i], /* newValue = */ null);
            }

            _collection.Clear();

            Debug.Assert(_collection.Count == 0);

            ++_version;
            WritePostscript();
        }

        /// <summary>
        ///     Determines if the list contains "value"
        /// </summary>
        public bool Contains(Timeline value)
        {
            ReadPreamble();

            return _collection.Contains(value);
        }

        /// <summary>
        ///     Returns the index of "value" in the list
        /// </summary>
        public int IndexOf(Timeline value)
        {
            ReadPreamble();

            return _collection.IndexOf(value);
        }

        /// <summary>
        ///     Inserts "value" into the list at the specified position
        /// </summary>
        public void Insert(int index, Timeline value)
        {
            if (value == null)
            {
                throw new System.ArgumentException(SR.Get(SRID.Collection_NoNull));
            }

            WritePreamble();

            OnFreezablePropertyChanged(/* oldValue = */ null, /* newValue = */ value);

            _collection.Insert(index, value);



            ++_version;
            WritePostscript();
        }

        /// <summary>
        ///     Removes "value" from the list
        /// </summary>
        public bool Remove(Timeline value)
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
                Timeline oldValue = _collection[index];

                OnFreezablePropertyChanged(oldValue, null);

                _collection.RemoveAt(index);




                ++_version;
                WritePostscript();

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
            RemoveAtWithoutFiringPublicEvents(index);

            // RemoveAtWithoutFiringPublicEvents incremented the version

            WritePostscript();
        }


        /// <summary>
        ///     Removes the element at the specified index without firing
        ///     the public Changed event.
        ///     The caller - typically a public method - is responsible for calling
        ///     WritePostscript if appropriate.
        /// </summary>
        internal void RemoveAtWithoutFiringPublicEvents(int index)
        {
            WritePreamble();

            Timeline oldValue = _collection[ index ];

            OnFreezablePropertyChanged(oldValue, null);

            _collection.RemoveAt(index);




            ++_version;

            // No WritePostScript to avoid firing the Changed event.
        }


        /// <summary>
        ///     Indexer for the collection
        /// </summary>
        public Timeline this[int index]
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

                WritePreamble();

                if (!Object.ReferenceEquals(_collection[ index ], value))
                {

                    Timeline oldValue = _collection[ index ];
                    OnFreezablePropertyChanged(oldValue, value);

                    _collection[ index ] = value;


                }


                ++_version;
                WritePostscript();
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
        public void CopyTo(Timeline[] array, int index)
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

        bool ICollection<Timeline>.IsReadOnly
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

        IEnumerator<Timeline> IEnumerable<Timeline>.GetEnumerator()
        {
            return this.GetEnumerator();
        }

        #endregion

        #region IList

        bool IList.IsReadOnly
        {
            get
            {
                return ((ICollection<Timeline>)this).IsReadOnly;
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
            return Contains(value as Timeline);
        }

        int IList.IndexOf(object value)
        {
            return IndexOf(value as Timeline);
        }

        void IList.Insert(int index, object value)
        {
            // Forward to IList<T> Insert
            Insert(index, Cast(value));
        }

        void IList.Remove(object value)
        {
            Remove(value as Timeline);
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

        #region Internal Helpers

        /// <summary>
        /// A frozen empty TimelineCollection.
        /// </summary>
        internal static TimelineCollection Empty
        {
            get
            {
                if (s_empty == null)
                {
                    TimelineCollection collection = new TimelineCollection();
                    collection.Freeze();
                    s_empty = collection;
                }

                return s_empty;
            }
        }

        /// <summary>
        /// Helper to return read only access.
        /// </summary>
        internal Timeline Internal_GetItem(int i)
        {
            return _collection[i];
        }

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

        private Timeline Cast(object value)
        {
            if( value == null )
            {
                throw new System.ArgumentNullException("value");
            }

            if (!(value is Timeline))
            {
                throw new System.ArgumentException(SR.Get(SRID.Collection_BadType, this.GetType().Name, value.GetType().Name, "Timeline"));
            }

            return (Timeline) value;
        }

        // IList.Add returns int and IList<T>.Add does not. This
        // is called by both Adds and IList<T>'s just ignores the
        // integer
        private int AddHelper(Timeline value)
        {
            int index = AddWithoutFiringPublicEvents(value);

            // AddAtWithoutFiringPublicEvents incremented the version

            WritePostscript();

            return index;
        }

        internal int AddWithoutFiringPublicEvents(Timeline value)
        {
            int index = -1;

            if (value == null)
            {
                throw new System.ArgumentException(SR.Get(SRID.Collection_NoNull));
            }
            WritePreamble();
            Timeline newValue = value;
            OnFreezablePropertyChanged(/* oldValue = */ null, newValue);
            index = _collection.Add(newValue);



            ++_version;

            // No WritePostScript to avoid firing the Changed event.

            return index;
        }



        #endregion Private Helpers

        private static TimelineCollection s_empty;


        #region Public Properties



        #endregion Public Properties

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
            return new TimelineCollection();
        }
        /// <summary>
        /// Implementation of Freezable.CloneCore()
        /// </summary>
        protected override void CloneCore(Freezable source)
        {
            TimelineCollection sourceTimelineCollection = (TimelineCollection) source;

            base.CloneCore(source);

            int count = sourceTimelineCollection._collection.Count;

            _collection = new FrugalStructList<Timeline>(count);

            for (int i = 0; i < count; i++)
            {
                Timeline newValue = (Timeline) sourceTimelineCollection._collection[i].Clone();
                OnFreezablePropertyChanged(/* oldValue = */ null, newValue);
                _collection.Add(newValue);

            }

        }
        /// <summary>
        /// Implementation of Freezable.CloneCurrentValueCore()
        /// </summary>
        protected override void CloneCurrentValueCore(Freezable source)
        {
            TimelineCollection sourceTimelineCollection = (TimelineCollection) source;

            base.CloneCurrentValueCore(source);

            int count = sourceTimelineCollection._collection.Count;

            _collection = new FrugalStructList<Timeline>(count);

            for (int i = 0; i < count; i++)
            {
                Timeline newValue = (Timeline) sourceTimelineCollection._collection[i].CloneCurrentValue();
                OnFreezablePropertyChanged(/* oldValue = */ null, newValue);
                _collection.Add(newValue);

            }

        }
        /// <summary>
        /// Implementation of Freezable.GetAsFrozenCore()
        /// </summary>
        protected override void GetAsFrozenCore(Freezable source)
        {
            TimelineCollection sourceTimelineCollection = (TimelineCollection) source;

            base.GetAsFrozenCore(source);

            int count = sourceTimelineCollection._collection.Count;

            _collection = new FrugalStructList<Timeline>(count);

            for (int i = 0; i < count; i++)
            {
                Timeline newValue = (Timeline) sourceTimelineCollection._collection[i].GetAsFrozen();
                OnFreezablePropertyChanged(/* oldValue = */ null, newValue);
                _collection.Add(newValue);

            }

        }
        /// <summary>
        /// Implementation of Freezable.GetCurrentValueAsFrozenCore()
        /// </summary>
        protected override void GetCurrentValueAsFrozenCore(Freezable source)
        {
            TimelineCollection sourceTimelineCollection = (TimelineCollection) source;

            base.GetCurrentValueAsFrozenCore(source);

            int count = sourceTimelineCollection._collection.Count;

            _collection = new FrugalStructList<Timeline>(count);

            for (int i = 0; i < count; i++)
            {
                Timeline newValue = (Timeline) sourceTimelineCollection._collection[i].GetCurrentValueAsFrozen();
                OnFreezablePropertyChanged(/* oldValue = */ null, newValue);
                _collection.Add(newValue);

            }

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
                canFreeze &= Freezable.Freeze(_collection[i], isChecking);
            }

            return canFreeze;
        }

        #endregion ProtectedMethods

        //------------------------------------------------------
        //
        //  Internal Methods
        //
        //------------------------------------------------------

        #region Internal Methods









        #endregion Internal Methods

        //------------------------------------------------------
        //
        //  Internal Properties
        //
        //------------------------------------------------------

        #region Internal Properties





        #endregion Internal Properties

        //------------------------------------------------------
        //
        //  Dependency Properties
        //
        //------------------------------------------------------

        #region Dependency Properties



        #endregion Dependency Properties

        //------------------------------------------------------
        //
        //  Internal Fields
        //
        //------------------------------------------------------

        #region Internal Fields




        internal FrugalStructList<Timeline> _collection;
        internal uint _version = 0;


        #endregion Internal Fields

        #region Enumerator
        /// <summary>
        /// Enumerates the items in a TimelineCollection
        /// </summary>
        public struct Enumerator : IEnumerator, IEnumerator<Timeline>
        {
            #region Constructor

            internal Enumerator(TimelineCollection list)
            {
                Debug.Assert(list != null, "list may not be null.");

                _list = list;
                _version = list._version;
                _index = -1;
                _current = default(Timeline);
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
            public Timeline Current
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
            private Timeline _current;
            private TimelineCollection _list;
            private uint _version;
            private int _index;
            #endregion
        }
        #endregion

        #region Constructors

        //------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------


        /// <summary>
        /// Initializes a new instance that is empty.
        /// </summary>
        public TimelineCollection()
        {
            _collection = new FrugalStructList<Timeline>();
        }

        /// <summary>
        /// Initializes a new instance that is empty and has the specified initial capacity.
        /// </summary>
        /// <param name="capacity"> int - The number of elements that the new list is initially capable of storing. </param>
        public TimelineCollection(int capacity)
        {
            _collection = new FrugalStructList<Timeline>(capacity);
        }

        /// <summary>
        /// Creates a TimelineCollection with all of the same elements as collection
        /// </summary>
        public TimelineCollection(IEnumerable<Timeline> collection)
        {
            // The WritePreamble and WritePostscript aren't technically necessary
            // in the constructor as of 1/20/05 but they are put here in case
            // their behavior changes at a later date

            WritePreamble();

            if (collection != null)
            {
                bool needsItemValidation = true;
                ICollection<Timeline> icollectionOfT = collection as ICollection<Timeline>;

                if (icollectionOfT != null)
                {
                    _collection = new FrugalStructList<Timeline>(icollectionOfT);
                }
                else
                {       
                    ICollection icollection = collection as ICollection;

                    if (icollection != null) // an IC but not and IC<T>
                    {
                        _collection = new FrugalStructList<Timeline>(icollection);
                    }
                    else // not a IC or IC<T> so fall back to the slower Add
                    {
                        _collection = new FrugalStructList<Timeline>();

                        foreach (Timeline item in collection)
                        {
                            if (item == null)
                            {
                                throw new System.ArgumentException(SR.Get(SRID.Collection_NoNull));
                            }
                            Timeline newValue = item;
                            OnFreezablePropertyChanged(/* oldValue = */ null, newValue);
                            _collection.Add(newValue);

                        }

                        needsItemValidation = false;
                    }
                }

                if (needsItemValidation)
                {
                    foreach (Timeline item in collection)
                    {
                        if (item == null)
                        {
                            throw new System.ArgumentException(SR.Get(SRID.Collection_NoNull));
                        }
                        OnFreezablePropertyChanged(/* oldValue = */ null, item);

                    }
                }


                WritePostscript();
            }
            else
            {
                throw new ArgumentNullException("collection");
            }
        }

        #endregion Constructors

    }
}
