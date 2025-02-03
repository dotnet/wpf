// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
//  Contents:  BamlLocalizationDictionary and BamlLocalizationDictionaryEnumerator
//

using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace System.Windows.Markup.Localizer
{
    /// <summary>
    /// BamlLocalizationDictionaryEnumerator
    /// </summary>
    public sealed class BamlLocalizationDictionaryEnumerator : IDictionaryEnumerator
    {
        internal BamlLocalizationDictionaryEnumerator(IEnumerator enumerator)
        {
            _enumerator = enumerator;
        }

        /// <summary>
        /// move to the next entry
        /// </summary>
        public bool MoveNext()
        {
            return _enumerator.MoveNext();
        }

        /// <summary>
        /// reset 
        /// </summary>
        public void Reset()
        {
            _enumerator.Reset();
        }

        /// <summary>
        /// gets the DictionaryEntry
        /// </summary>
        public DictionaryEntry Entry
        {
            get => (DictionaryEntry)_enumerator.Current;
        }

        /// <summary>
        /// gets the key
        /// </summary>
        public BamlLocalizableResourceKey Key
        {
            get => (BamlLocalizableResourceKey)Entry.Key;
        }

        /// <summary>
        /// gets the value 
        /// </summary>
        public BamlLocalizableResource Value
        {
            get => (BamlLocalizableResource)Entry.Value;
        }

        /// <summary>
        /// return the current entry
        /// </summary>
        public DictionaryEntry Current
        {
            get => Entry;
        }

        //------------------------------------
        // Interfaces
        //------------------------------------

        /// <summary>
        /// Return the current object
        /// </summary>
        /// <value>object </value>
        object IEnumerator.Current
        {
            get => Current;
        }

        /// <summary>
        /// return the key
        /// </summary>
        /// <value>key</value>
        object IDictionaryEnumerator.Key
        {
            get => Key;
        }

        /// <summary>
        /// Value 
        /// </summary>
        /// <value>value</value>
        object IDictionaryEnumerator.Value
        {
            get => Value;
        }

        //---------------------------------------
        // Private
        //---------------------------------------
        private readonly IEnumerator _enumerator;
    }

    /// <summary>
    /// Proxy structure that will help compiler pick our enumerator for foreach.
    /// </summary>
    internal readonly struct BamlLocalizationDictEnumerator
    {
        private readonly BamlLocalizationDictionary _dictionary;

        public BamlLocalizationDictEnumerator(BamlLocalizationDictionary dictionary)
        {
            _dictionary = dictionary;
        }

        /// <summary>
        /// Method that returns the underlying generic struct-based enumerator.
        /// </summary>
        /// <returns>Struct-based enumerator for the dictionary.</returns>
        public Dictionary<BamlLocalizableResourceKey, BamlLocalizableResource>.Enumerator GetEnumerator() => _dictionary.GetStructEnumerator();

    }

    /// <summary>
    /// Enumerator that enumerates all the localizable resources in 
    /// a baml stream
    /// </summary>
    public sealed class BamlLocalizationDictionary : IDictionary
    {
        /// <summary>
        /// Constructor that creates an empty baml resource dictionary
        /// </summary>
        public BamlLocalizationDictionary()
        {
            _dictionary = new Dictionary<BamlLocalizableResourceKey, BamlLocalizableResource>();
        }

        /// <summary>
        /// Method that returns the underlying generic struct-based enumerator.
        /// </summary>
        /// <returns>Struct-based enumerator for the dictionary.</returns>
        internal Dictionary<BamlLocalizableResourceKey, BamlLocalizableResource>.Enumerator GetStructEnumerator()
        {
            return _dictionary.GetEnumerator();
        }

        /// <summary>
        /// gets value indicating whether the dictionary has fixed size
        /// </summary>
        /// <value>true for fixed size, false otherwise.</value>
        public bool IsFixedSize
        {
            get => false;
        }

        /// <summary>
        /// get value indicating whether it is readonly
        /// </summary>
        /// <value>true for readonly, false otherwise.</value>
        public bool IsReadOnly
        {
            get => false;
        }

        /// <summary>
        /// Return the key to the root element if the root element is localizable, return null otherwise
        /// </summary>
        /// <remarks>
        /// Modifications can be added to the proeprties of the root element which will have a global effect 
        /// on the UI. For example, changing CulutreInfo or FlowDirection on the root element (if applicable) 
        /// will have impact to the whole UI. 
        /// </remarks>
        public BamlLocalizableResourceKey RootElementKey
        {
            get => _rootElementKey;
        }

        /// <summary>
        /// gets the collection of keys
        /// </summary>
        /// <value>a collection of keys</value>
        public ICollection Keys
        {
            get => ((IDictionary)_dictionary).Keys;
        }

        /// <summary>
        /// gets the collection of values
        /// </summary>
        /// <value>a collection of values</value>
        public ICollection Values
        {
            get => ((IDictionary)_dictionary).Values;
        }

        /// <summary>
        /// Gets or sets a localizable resource by the key
        /// </summary>
        /// <param name="key">BamlLocalizableResourceKey key</param>
        /// <returns>BamlLocalizableResource object identified by the key</returns>
        public BamlLocalizableResource this[BamlLocalizableResourceKey key]
        {
            get
            {
                ArgumentNullException.ThrowIfNull(key);
                return _dictionary[key];
            }
            set
            {
                ArgumentNullException.ThrowIfNull(key);
                _dictionary[key] = value;
            }
        }

        /// <summary>
        /// Adds a localizable resource with the provided key
        /// </summary>
        /// <param name="key">the BamlLocalizableResourceKey key</param>
        /// <param name="value">the BamlLocalizableResource</param>
        public void Add(BamlLocalizableResourceKey key, BamlLocalizableResource value)
        {
            ArgumentNullException.ThrowIfNull(key);
            _dictionary.Add(key, value);
        }

        /// <summary>
        /// removes all the resources in the dictionary.
        /// </summary>
        public void Clear()
        {
            _dictionary.Clear();
        }

        /// <summary>
        /// removes the localizable resource with the specified key
        /// </summary>
        /// <param name="key">the key</param>
        public void Remove(BamlLocalizableResourceKey key)
        {
            _dictionary.Remove(key);
        }

        /// <summary>
        /// determines whether the dictionary contains the localizable resource 
        /// with the specified key
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public bool Contains(BamlLocalizableResourceKey key)
        {
            return _dictionary.ContainsKey(key);
        }

        /// <summary>
        /// Determines whether the dictionary contains the localizable resource with the specified <paramref name="key"/>
        /// and returns the <paramref name="value"/> if it does.
        /// </summary>
        /// <param name="key">The key to retrieve <paramref name="value"/> for.</param>
        /// <param name="value">If <see langword="true"/>, returns the value for the specified <paramref name="key"/>, otherwise <see langword="null"/>.</param>
        /// <returns></returns>
        internal bool TryGetValue(BamlLocalizableResourceKey key, [MaybeNullWhen(false)] out BamlLocalizableResource value)
        {
            return _dictionary.TryGetValue(key, out value);
        }

        /// <summary>
        /// returns an IDictionaryEnumerator for the dictionary.
        /// </summary>
        /// <returns>the enumerator for the dictionary</returns>
        public BamlLocalizationDictionaryEnumerator GetEnumerator()
        {
            return new BamlLocalizationDictionaryEnumerator(((IDictionary)_dictionary).GetEnumerator());
        }

        /// <summary>
        /// gets the number of localizable resources in the dictionary
        /// </summary>
        /// <value>number of localizable resources</value>
        public int Count
        {
            get => _dictionary.Count;
        }

        /// <summary>
        ///     Copies the dictionary's elements to a one-dimensional 
        ///     Array instance at the specified index.
        /// </summary>
        public void CopyTo(DictionaryEntry[] array, int arrayIndex)
        {
            ArgumentNullException.ThrowIfNull(array);
            ArgumentOutOfRangeException.ThrowIfNegative(arrayIndex);

            if (arrayIndex >= array.Length)
            {
                throw new ArgumentException(SR.Format(SR.Collection_CopyTo_IndexGreaterThanOrEqualToArrayLength, "arrayIndex", "array"), nameof(arrayIndex));
            }

            if (Count > (array.Length - arrayIndex))
            {
                throw new ArgumentException(SR.Format(SR.Collection_CopyTo_NumberOfElementsExceedsArrayLength, "arrayIndex", "array"));
            }

            foreach (KeyValuePair<BamlLocalizableResourceKey, BamlLocalizableResource> pair in _dictionary)
            {
                DictionaryEntry entry = new(pair.Key, pair.Value);
                array[arrayIndex++] = entry;
            }
        }

        #region interface ICollection, IEnumerable, IDictionary
        //------------------------------
        // interface functions
        //------------------------------      

        bool IDictionary.Contains(object key)
        {
            ArgumentNullException.ThrowIfNull(key);
            return ((IDictionary)_dictionary).Contains(key);
        }

        void IDictionary.Add(object key, object value)
        {
            ArgumentNullException.ThrowIfNull(key);
            ((IDictionary)_dictionary).Add(key, value);
        }

        void IDictionary.Remove(object key)
        {
            ArgumentNullException.ThrowIfNull(key);
            ((IDictionary)_dictionary).Remove(key);
        }

        object IDictionary.this[object key]
        {
            get
            {
                ArgumentNullException.ThrowIfNull(key);
                return ((IDictionary)_dictionary)[key];
            }
            set
            {
                ArgumentNullException.ThrowIfNull(key);
                ((IDictionary)_dictionary)[key] = value;
            }
        }

        IDictionaryEnumerator IDictionary.GetEnumerator() => GetEnumerator();

        void ICollection.CopyTo(Array array, int index)
        {
            if (array != null && array.Rank != 1)
            {
                throw new ArgumentException(SR.Format(SR.Collection_CopyTo_ArrayCannotBeMultidimensional), nameof(array));
            }

            CopyTo(array as DictionaryEntry[], index);
        }

        int ICollection.Count
        {
            get => Count;
        }

        object ICollection.SyncRoot
        {
            get => ((IDictionary)_dictionary).SyncRoot;
        }

        bool ICollection.IsSynchronized
        {
            get => ((IDictionary)_dictionary).IsSynchronized;
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        #endregion
        //------------------------------
        // internal functions
        //------------------------------
        internal BamlLocalizationDictionary Copy()
        {
            BamlLocalizationDictionary newDictionary = new();
            foreach (KeyValuePair<BamlLocalizableResourceKey, BamlLocalizableResource> pair in _dictionary)
            {
                BamlLocalizableResource resourceCopy = pair.Value == null ? null : new BamlLocalizableResource(pair.Value);

                newDictionary.Add(pair.Key, resourceCopy);
            }

            newDictionary._rootElementKey = _rootElementKey;

            // return the new dictionary
            return newDictionary;
        }

        internal void SetRootElementKey(BamlLocalizableResourceKey key)
        {
            _rootElementKey = key;
        }

        //------------------------------
        // private members
        //------------------------------
        private readonly Dictionary<BamlLocalizableResourceKey, BamlLocalizableResource> _dictionary;
        private BamlLocalizableResourceKey _rootElementKey;
    }
}

