// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
//
//
//  Contents:  LanguageSpecificStringDictionary
//
//

using System;
using System.ComponentModel;    // for TypeConverter
using System.Globalization;
using SC=System.Collections;
using System.Collections.Generic;
using System.Windows.Markup;    // for XmlLanguage and XmlLanguageConverter

using MS.Internal.PresentationCore;
using SR=MS.Internal.PresentationCore.SR;
using SRID=MS.Internal.PresentationCore.SRID;

namespace System.Windows.Media
{
    /// <summary>
    /// Collection of strings indexed by language. May be used to represent an object's
    /// name in various languages.
    /// </summary>
    public sealed class LanguageSpecificStringDictionary : IDictionary<XmlLanguage, string>, SC.IDictionary
    {
        private IDictionary<XmlLanguage, string> _innerDictionary;

        /// <summary>
        /// Creates a LanguageSpecificStringDictionary that wraps the specified dictionary.
        /// </summary>
        internal LanguageSpecificStringDictionary(IDictionary<XmlLanguage, string> innerDictionary)
        {
            _innerDictionary = innerDictionary;
        }

        #region IEnumerable members

        /// <summary>
        /// Returns an enumerator that iterates through the collection.
        /// </summary>
        [CLSCompliant(false)]
        public IEnumerator<KeyValuePair<XmlLanguage, string>> GetEnumerator()
        {
            return _innerDictionary.GetEnumerator();
        }

        SC.IEnumerator SC.IEnumerable.GetEnumerator()
        {
            return new EntryEnumerator(_innerDictionary);
        }

        SC.IDictionaryEnumerator SC.IDictionary.GetEnumerator()
        {
            return new EntryEnumerator(_innerDictionary);
        }

        /// <summary>
        /// If the dictionary contains an entry for the specified language, returns true
        /// and stores the string in the value parameter; otherwise returns false and
        /// sets value to null.
        /// </summary>
        public bool TryGetValue(XmlLanguage key, out string value)
        {
            return _innerDictionary.TryGetValue(key, out value);
        }

        #endregion

        #region ICollection members

        /// <summary>
        /// Gets the number of strings in the colection.
        /// </summary>
        public int Count
        {
            get { return _innerDictionary.Count; }
        }

        /// <summary>
        /// Gets a value indicating whether the collection is read-only.
        /// </summary>
        public bool IsReadOnly
        {
            get { return _innerDictionary.IsReadOnly; }
        }

        /// <summary>
        /// Adds a language and associated string to the collection.
        /// </summary>
        [CLSCompliant(false)]
        public void Add(KeyValuePair<XmlLanguage, string> item)
        {
            Add(item.Key, item.Value);
        }

        /// <summary>
        /// Removes all languages and strings from the collection.
        /// </summary>
        public void Clear()
        {
            _innerDictionary.Clear();
        }

        /// <summary>
        /// Determines whether the collection contains the specified language-string pair.
        /// </summary>
        [CLSCompliant(false)]
        public bool Contains(KeyValuePair<XmlLanguage, string> item)
        {
            return _innerDictionary.Contains(item);
        }

        /// <summary>
        /// Copies the contents of the collection to the specified array.
        /// </summary>
        [CLSCompliant(false)]
        public void CopyTo(KeyValuePair<XmlLanguage, string>[] array, int index)
        {
            if (array == null)
                throw new ArgumentNullException("array");

            if (index < 0)
                throw new ArgumentOutOfRangeException("index");

            if (index >= array.Length)
                throw new ArgumentException(SR.Get(SRID.Collection_CopyTo_IndexGreaterThanOrEqualToArrayLength, "index", "array"));

            if (_innerDictionary.Count > array.Length - index)
                throw new ArgumentException(SR.Get(SRID.Collection_CopyTo_NumberOfElementsExceedsArrayLength, index, "array"));

            _innerDictionary.CopyTo(array, index);
        }

        /// <summary>
        /// Removes the specified language-string pair from the collection.
        /// </summary>
        [CLSCompliant(false)]
        public bool Remove(KeyValuePair<XmlLanguage, string> item)
        {
            return _innerDictionary.Remove(item);
        }

        bool SC.ICollection.IsSynchronized
        {
            get { return false; }
        }

        object SC.ICollection.SyncRoot
        {
            get { return _innerDictionary; }
        }

        void SC.ICollection.CopyTo(Array array, int index)
        {
            if (array == null)
                throw new ArgumentNullException("array");

            if (index < 0)
                throw new ArgumentOutOfRangeException("index");

            if (index >= array.Length)
                throw new ArgumentException(SR.Get(SRID.Collection_CopyTo_IndexGreaterThanOrEqualToArrayLength, "index", "array"));

            if (_innerDictionary.Count > array.Length - index)
                throw new ArgumentException(SR.Get(SRID.Collection_CopyTo_NumberOfElementsExceedsArrayLength, index, "array"));

            SC.DictionaryEntry[] typedArray = array as SC.DictionaryEntry[];
            if (typedArray != null)
            {
                // it's an array of the exact type
                foreach (KeyValuePair<XmlLanguage, string> item in _innerDictionary)
                {
                    typedArray[index++] = new SC.DictionaryEntry(item.Key, item.Value);
                }
            }
            else
            {
                // it's an array of some other type, e.g., object[]; make sure it's one dimensional
                if (array.Rank != 1)
                    throw new ArgumentException(SR.Get(SRID.Collection_CopyTo_ArrayCannotBeMultidimensional));

                // make sure the element type is compatible
                Type elementType = array.GetType().GetElementType();
                if (!elementType.IsAssignableFrom(typeof(SC.DictionaryEntry)))
                    throw new ArgumentException(SR.Get(SRID.CannotConvertType, typeof(SC.DictionaryEntry), elementType));

                foreach (KeyValuePair<XmlLanguage, string> item in _innerDictionary)
                {
                    array.SetValue(new SC.DictionaryEntry(item.Key, item.Value), index++);
                }
            }
        }

        #endregion

        #region IDictionary members

        /// <summary>
        /// Adds a language and associated string to the collection.
        /// </summary>
        public void Add(XmlLanguage key, string value)
        {
            _innerDictionary.Add(key, ValidateValue(value));
        }

        /// <summary>
        /// Determines whether the collection contains the specified language.
        /// </summary>
        public bool ContainsKey(XmlLanguage key)
        {
            return _innerDictionary.ContainsKey(key);
        }

        /// <summary>
        /// Removes the specified language and associated string.
        /// </summary>
        public bool Remove(XmlLanguage key)
        {
            return _innerDictionary.Remove(key);
        }

        /// <summary>
        /// Gets or sets the string associated with the specified language.
        /// </summary>
        public string this[XmlLanguage key]
        {
            get { return _innerDictionary[key]; }
            set { _innerDictionary[key] = ValidateValue(value); }
        }

        /// <summary>
        /// Gets a collection containing the keys (languages) in the dictionary.
        /// </summary>
        [CLSCompliant(false)]
        public ICollection<XmlLanguage> Keys
        {
            get { return _innerDictionary.Keys; }
        }

        /// <summary>
        /// Gets a collection containing the values (strings) in the dictionary.
        /// </summary>
        [CLSCompliant(false)]
        public ICollection<string> Values
        {
            get { return _innerDictionary.Values; }
        }

        bool SC.IDictionary.IsFixedSize
        {
            get { return false; }
        }

        object SC.IDictionary.this[object key]
        {
            get
            {
                XmlLanguage language = TryConvertKey(key);
                if (language == null)
                    return null;

                return _innerDictionary[language];
            }

            set
            {
                _innerDictionary[ConvertKey(key)] = ConvertValue(value);
            }
        }

        SC.ICollection SC.IDictionary.Keys
        {
            get 
            {
                return new KeyCollection(_innerDictionary);
            }
        }

        SC.ICollection SC.IDictionary.Values
        {
            get 
            {
                return new ValueCollection(_innerDictionary);
            }
        }

        void SC.IDictionary.Add(object key, object value)
        {
            _innerDictionary.Add(ConvertKey(key), ConvertValue(value));
        }

        bool SC.IDictionary.Contains(object key)
        {
            XmlLanguage language = TryConvertKey(key);
            if (language == null)
                return false;

            return _innerDictionary.ContainsKey(language);
        }

        void SC.IDictionary.Remove(object key)
        {
            XmlLanguage language = TryConvertKey(key);
            if (language != null)
                _innerDictionary.Remove(language);
        }
        #endregion

        #region private members

        // make sure value is not null
        private string ValidateValue(string value)
        {
            if (value == null)
                throw new ArgumentNullException("value");

            return value;
        }

        // make sure value is a string, and throw exception on failure
        private string ConvertValue(object value)
        {
            string s = value as string;
            if (s == null)
            {
                if (value == null)
                    throw new ArgumentNullException("value");
                else
                    throw new ArgumentException(SR.Get(SRID.UnexpectedParameterType, value.GetType(), typeof(string)), "value");
            }
            return s;
        }

        // Convert a key to an XmlLanguage, and throw exception on failure
        private XmlLanguage ConvertKey(object key)
        {
            XmlLanguage language = TryConvertKey(key);
            if (language == null)
            {
                if (key == null)
                    throw new ArgumentNullException("key");
                else
                    throw new ArgumentException(SR.Get(SRID.CannotConvertType, key.GetType(), typeof(XmlLanguage)), "key");
            }
            return language;
        }

        // Convert a key to an XmlLanguage, and return null on failure
        private XmlLanguage TryConvertKey(object key)
        {
            XmlLanguage language = key as XmlLanguage;
            if (language != null)
                return language;

            string name = key as string;
            if (name != null)
                return XmlLanguage.GetLanguage(name);

            return null;
        }


        /// <summary>
        /// Implementation of the IDictionaryEnumerator for LanguageSpecificStringDictionary, and also the
        /// base class for the enumerators for the Keys and Values collections.
        /// </summary>
        private class EntryEnumerator : SC.IDictionaryEnumerator
        {
            protected IDictionary<XmlLanguage, string> _innerDictionary;
            protected IEnumerator<KeyValuePair<XmlLanguage, string>> _enumerator;

            internal EntryEnumerator(IDictionary<XmlLanguage, string> names)
            {
                _innerDictionary = names;
                _enumerator = names.GetEnumerator();
            }

            public bool MoveNext()
            {
                return _enumerator.MoveNext();
            }

            public void Reset()
            {
                // IDictionary<K,T> doesn't have a Reset() method so just create
                // a new enumerator.
                _enumerator = _innerDictionary.GetEnumerator();
            }

            // Current object in the sequence, which for an IDictionaryEnumerator
            // is expected to be a DictionaryEntry. This method is virtual because 
            // we override it in the derived KeyEnumerator and ValueEnumerator 
            // classes to return the Key and Value, respectively.
            public virtual object Current
            {
                get
                {
                    return Entry;
                }
            }

            private KeyValuePair<XmlLanguage, string> GetCurrentEntry()
            {
                // Get the key-value pair from the generic IDictionary.
                KeyValuePair<XmlLanguage, string> entry = _enumerator.Current;

                // If there is no current item a non-generic IEnumerator should throw an exception,
                // but a generic IEnumerator<T> is not required to. Therefore we need to check for
                // this case here by checking for a null Key.
                if (entry.Key == null)
                    throw new InvalidOperationException(SR.Get(SRID.Enumerator_VerifyContext));

                return entry;
            }

            public SC.DictionaryEntry Entry
            {
                get
                {
                    KeyValuePair<XmlLanguage, string> entry = GetCurrentEntry();
                    return new SC.DictionaryEntry(entry.Key, entry.Value);
                }
            }

            public object Key
            {
                get
                {
                    return GetCurrentEntry().Key;
                }
            }

            public object Value
            {
                get
                {
                    return GetCurrentEntry().Value;
                }
            }
        }

        /// <summary>
        /// Base class of KeyCollection and ValueCollection.
        /// </summary>
        private abstract class BaseCollection : SC.ICollection
        {
            protected IDictionary<XmlLanguage, string> _innerDictionary;

            internal BaseCollection(IDictionary<XmlLanguage, string> names)
            {
                _innerDictionary = names;
            }

            #region ICollection members
            public int Count
            {
                get { return _innerDictionary.Count; }
            }

            public void CopyTo(Array array, int index)
            {
                foreach (object obj in this)
                {
                    array.SetValue(obj, index++);
                }
            }

            public bool IsSynchronized
            {
                get { return false; }
            }

            public object SyncRoot
            {
                get { return _innerDictionary; }
            }

            public abstract SC.IEnumerator GetEnumerator();
            #endregion
        }

        /// <summary>
        /// Collection returned by LanguageSpecificStringDictionary.Keys.
        /// </summary>
        private class KeyCollection : BaseCollection
        {
            internal KeyCollection(IDictionary<XmlLanguage, string> names) : base(names)
            {
            }

            public override SC.IEnumerator GetEnumerator()
            {
                return new KeyEnumerator(_innerDictionary);
            }

            // The enumerator the Keys collection is identical to the enumerator for the
            // dictionary itself except that Current is overridden to return Key instead 
            // of Entry.
            private class KeyEnumerator : EntryEnumerator
            {
                internal KeyEnumerator(IDictionary<XmlLanguage, string> names) : base(names)
                {
                }

                public override object Current
                {
                    get
                    {
                        return base.Key;
                    }
                }
            }
        }

        /// <summary>
        /// Collection returned by LanguageSpecificStringDictionary.Values.
        /// </summary>
        private class ValueCollection : BaseCollection
        {
            internal ValueCollection(IDictionary<XmlLanguage, string> names)
                : base(names)
            {
            }

            public override SC.IEnumerator GetEnumerator()
            {
                return new ValueEnumerator(_innerDictionary);
            }

            // The enumerator the Values collection is identical to the enumerator for the
            // dictionary itself except that Current is overridden to return Value instead
            // of Entry.
            private class ValueEnumerator : EntryEnumerator
            {
                internal ValueEnumerator(IDictionary<XmlLanguage, string> names) : base(names)
                {
                }

                public override object Current
                {
                    get
                    {
                        return base.Value;
                    }
                }
            }
        }

        #endregion
    }
}
