// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
//
// This file was generated, please do not edit it directly.
//
// Please see MilCodeGen.html for more information.
//

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

namespace System.Windows.Media
{
    /// <summary>
    /// A collection of doubles.
    /// </summary>

    [TypeConverter(typeof(DoubleCollectionConverter))]
    [ValueSerializer(typeof(DoubleCollectionValueSerializer))] // Used by MarkupWriter
    public sealed partial class DoubleCollection : Freezable, IFormattable, IList, IList<double>
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
        public new DoubleCollection Clone()
        {
            return (DoubleCollection)base.Clone();
        }

        /// <summary>
        ///     Shadows inherited CloneCurrentValue() with a strongly typed
        ///     version for convenience.
        /// </summary>
        public new DoubleCollection CloneCurrentValue()
        {
            return (DoubleCollection)base.CloneCurrentValue();
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
        public void Add(double value)
        {
            AddHelper(value);
        }

        /// <summary>
        ///     Removes all elements from the list
        /// </summary>
        public void Clear()
        {
            WritePreamble();

            _collection.Clear();

            ++_version;
            WritePostscript();
        }

        /// <summary>
        ///     Determines if the list contains "value"
        /// </summary>
        public bool Contains(double value)
        {
            ReadPreamble();

            return _collection.Contains(value);
        }

        /// <summary>
        ///     Returns the index of "value" in the list
        /// </summary>
        public int IndexOf(double value)
        {
            ReadPreamble();

            return _collection.IndexOf(value);
        }

        /// <summary>
        ///     Inserts "value" into the list at the specified position
        /// </summary>
        public void Insert(int index, double value)
        {
            WritePreamble();
            _collection.Insert(index, value);


            ++_version;
            WritePostscript();
        }

        /// <summary>
        ///     Removes "value" from the list
        /// </summary>
        public bool Remove(double value)
        {
            WritePreamble();
            int index = IndexOf(value);
            if (index >= 0)
            {
                // we already have index from IndexOf so instead of using Remove,
                // which will search the collection a second time, we'll use RemoveAt
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
            _collection.RemoveAt(index);


            ++_version;

            // No WritePostScript to avoid firing the Changed event.
        }


        /// <summary>
        ///     Indexer for the collection
        /// </summary>
        public double this[int index]
        {
            get
            {
                ReadPreamble();

                return _collection[index];
            }
            set
            {
                WritePreamble();
                _collection[ index ] = value;


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
        public void CopyTo(double[] array, int index)
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

        bool ICollection<double>.IsReadOnly
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

        IEnumerator<double> IEnumerable<double>.GetEnumerator()
        {
            return this.GetEnumerator();
        }

        #endregion

        #region IList

        bool IList.IsReadOnly
        {
            get
            {
                return ((ICollection<double>)this).IsReadOnly;
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
            if (value is double)
            {
                return Contains((double)value);
            }

            return false;
        }

        int IList.IndexOf(object value)
        {
            if (value is double)
            {
                return IndexOf((double)value);
            }

            return -1;
        }

        void IList.Insert(int index, object value)
        {
            // Forward to IList<T> Insert
            Insert(index, Cast(value));
        }

        void IList.Remove(object value)
        {
            if (value is double)
            {
                Remove((double)value);
            }
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
        /// A frozen empty DoubleCollection.
        /// </summary>
        internal static DoubleCollection Empty
        {
            get
            {
                if (s_empty == null)
                {
                    DoubleCollection collection = new DoubleCollection();
                    collection.Freeze();
                    s_empty = collection;
                }

                return s_empty;
            }
        }

        /// <summary>
        /// Helper to return read only access.
        /// </summary>
        internal double Internal_GetItem(int i)
        {
            return _collection[i];
        }



        #endregion

        #region Private Helpers

        private double Cast(object value)
        {
            if( value == null )
            {
                throw new System.ArgumentNullException("value");
            }

            if (!(value is double))
            {
                throw new System.ArgumentException(SR.Get(SRID.Collection_BadType, this.GetType().Name, value.GetType().Name, "double"));
            }

            return (double) value;
        }

        // IList.Add returns int and IList<T>.Add does not. This
        // is called by both Adds and IList<T>'s just ignores the
        // integer
        private int AddHelper(double value)
        {
            int index = AddWithoutFiringPublicEvents(value);

            // AddAtWithoutFiringPublicEvents incremented the version

            WritePostscript();

            return index;
        }

        internal int AddWithoutFiringPublicEvents(double value)
        {
            int index = -1;


            WritePreamble();
            index = _collection.Add(value);


            ++_version;

            // No WritePostScript to avoid firing the Changed event.

            return index;
        }



        #endregion Private Helpers

        private static DoubleCollection s_empty;


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
            return new DoubleCollection();
        }
        /// <summary>
        /// Implementation of Freezable.CloneCore()
        /// </summary>
        protected override void CloneCore(Freezable source)
        {
            DoubleCollection sourceDoubleCollection = (DoubleCollection) source;

            base.CloneCore(source);

            int count = sourceDoubleCollection._collection.Count;

            _collection = new FrugalStructList<double>(count);

            for (int i = 0; i < count; i++)
            {
                _collection.Add(sourceDoubleCollection._collection[i]);
            }
}
        /// <summary>
        /// Implementation of Freezable.CloneCurrentValueCore()
        /// </summary>
        protected override void CloneCurrentValueCore(Freezable source)
        {
            DoubleCollection sourceDoubleCollection = (DoubleCollection) source;

            base.CloneCurrentValueCore(source);

            int count = sourceDoubleCollection._collection.Count;

            _collection = new FrugalStructList<double>(count);

            for (int i = 0; i < count; i++)
            {
                _collection.Add(sourceDoubleCollection._collection[i]);
            }
}
        /// <summary>
        /// Implementation of Freezable.GetAsFrozenCore()
        /// </summary>
        protected override void GetAsFrozenCore(Freezable source)
        {
            DoubleCollection sourceDoubleCollection = (DoubleCollection) source;

            base.GetAsFrozenCore(source);

            int count = sourceDoubleCollection._collection.Count;

            _collection = new FrugalStructList<double>(count);

            for (int i = 0; i < count; i++)
            {
                _collection.Add(sourceDoubleCollection._collection[i]);
            }
}
        /// <summary>
        /// Implementation of Freezable.GetCurrentValueAsFrozenCore()
        /// </summary>
        protected override void GetCurrentValueAsFrozenCore(Freezable source)
        {
            DoubleCollection sourceDoubleCollection = (DoubleCollection) source;

            base.GetCurrentValueAsFrozenCore(source);

            int count = sourceDoubleCollection._collection.Count;

            _collection = new FrugalStructList<double>(count);

            for (int i = 0; i < count; i++)
            {
                _collection.Add(sourceDoubleCollection._collection[i]);
            }
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


        /// <summary>
        /// Creates a string representation of this object based on the current culture.
        /// </summary>
        /// <returns>
        /// A string representation of this object.
        /// </returns>
        public override string ToString()
        {
            ReadPreamble();
            // Delegate to the internal method which implements all ToString calls.
            return ConvertToString(null /* format string */, null /* format provider */);
        }

        /// <summary>
        /// Creates a string representation of this object based on the IFormatProvider
        /// passed in.  If the provider is null, the CurrentCulture is used.
        /// </summary>
        /// <returns>
        /// A string representation of this object.
        /// </returns>
        public string ToString(IFormatProvider provider)
        {
            ReadPreamble();
            // Delegate to the internal method which implements all ToString calls.
            return ConvertToString(null /* format string */, provider);
        }

        /// <summary>
        /// Creates a string representation of this object based on the format string
        /// and IFormatProvider passed in.
        /// If the provider is null, the CurrentCulture is used.
        /// See the documentation for IFormattable for more information.
        /// </summary>
        /// <returns>
        /// A string representation of this object.
        /// </returns>
        string IFormattable.ToString(string format, IFormatProvider provider)
        {
            ReadPreamble();
            // Delegate to the internal method which implements all ToString calls.
            return ConvertToString(format, provider);
        }

        /// <summary>
        /// Creates a string representation of this object based on the format string
        /// and IFormatProvider passed in.
        /// If the provider is null, the CurrentCulture is used.
        /// See the documentation for IFormattable for more information.
        /// </summary>
        /// <returns>
        /// A string representation of this object.
        /// </returns>
        internal string ConvertToString(string format, IFormatProvider provider)
        {
            if (_collection.Count == 0)
            {
                return String.Empty;
            }

            StringBuilder str = new StringBuilder();

            // Consider using this separator
            // Helper to get the numeric list separator for a given culture.
            // char separator = MS.Internal.TokenizerHelper.GetNumericListSeparator(provider);

            for (int i=0; i<_collection.Count; i++)
            {
                str.AppendFormat(
                    provider,
                    "{0:" + format + "}",
                    _collection[i]);

                if (i != _collection.Count-1)
                {
                    str.Append(" ");
                }
            }

            return str.ToString();
        }
        /// <summary>
        /// Parse - returns an instance converted from the provided string
        /// using the current culture
        /// <param name="source"> string with DoubleCollection data </param>
        /// </summary>
        public static DoubleCollection Parse(string source)
        {
            IFormatProvider formatProvider = System.Windows.Markup.TypeConverterHelper.InvariantEnglishUS;

            TokenizerHelper th = new TokenizerHelper(source, formatProvider);
            DoubleCollection resource = new DoubleCollection();

            double value;

            while (th.NextToken())
            {
                value = Convert.ToDouble(th.GetCurrentToken(), formatProvider);

                resource.Add(value);
            }

            return resource;
        }

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




        internal FrugalStructList<double> _collection;
        internal uint _version = 0;


        #endregion Internal Fields

        #region Enumerator
        /// <summary>
        /// Enumerates the items in a doubleCollection
        /// </summary>
        public struct Enumerator : IEnumerator, IEnumerator<double>
        {
            #region Constructor

            internal Enumerator(DoubleCollection list)
            {
                Debug.Assert(list != null, "list may not be null.");

                _list = list;
                _version = list._version;
                _index = -1;
                _current = default(double);
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
            public double Current
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
            private double _current;
            private DoubleCollection _list;
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
        public DoubleCollection()
        {
            _collection = new FrugalStructList<double>();
        }

        /// <summary>
        /// Initializes a new instance that is empty and has the specified initial capacity.
        /// </summary>
        /// <param name="capacity"> int - The number of elements that the new list is initially capable of storing. </param>
        public DoubleCollection(int capacity)
        {
            _collection = new FrugalStructList<double>(capacity);
        }

        /// <summary>
        /// Creates a DoubleCollection with all of the same elements as collection
        /// </summary>
        public DoubleCollection(IEnumerable<double> collection)
        {
            // The WritePreamble and WritePostscript aren't technically necessary
            // in the constructor as of 1/20/05 but they are put here in case
            // their behavior changes at a later date

            WritePreamble();

            if (collection != null)
            {
                ICollection<double> icollectionOfT = collection as ICollection<double>;

                if (icollectionOfT != null)
                {
                    _collection = new FrugalStructList<double>(icollectionOfT);
                }
                else
                {       
                    ICollection icollection = collection as ICollection;

                    if (icollection != null) // an IC but not and IC<T>
                    {
                        _collection = new FrugalStructList<double>(icollection);
                    }
                    else // not a IC or IC<T> so fall back to the slower Add
                    {
                        _collection = new FrugalStructList<double>();

                        foreach (double item in collection)
                        {
                            _collection.Add(item);
                        }
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
