// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

﻿//
// Description: Collection of Uri objects used to specify location of custom dictionaries.
//

using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using System.Windows.Markup;
using System.Windows.Navigation;
using System.Windows.Controls.Primitives;
using System.Windows.Documents;

namespace System.Windows.Controls
{
    /// <summary>
    /// Collection of <see cref="Uri"/> objects representing local file system or UNC paths.
    /// Methods that modify the collection also call into Speller callbakcs to notify about the change, so Speller
    /// could load/unload appropriate dicitonary files.
    /// </summary>
    /// <remarks>
    /// Implements IList interface with 1 restriction - does not allow double addition of same item to 
    /// the collection.
    /// For all methods (except Inser) that modify the collection behavior is to ignore add/set request,
    /// but still notify the Speller.
    /// In case of Insert an exception is thrown if the item already exists in the collection.
    /// 
    /// There is a restriction on kinds of supported Uri objects, which is enforced in <see cref="ValidateUri"/> method.
    /// Refer to this method for supported types of URIs.
    /// </remarks>
    internal class CustomDictionarySources : IList<Uri>, IList
    {
        //------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------
        #region constructors

        internal CustomDictionarySources(TextBoxBase owner)
        {
            _owner = owner;
            _uriList = new List<Uri>();
        }

        #endregion constructors

        //------------------------------------------------------
        //
        //  Interface implementations
        //
        //------------------------------------------------------
        #region Interface implementations

        #region IEnumerable<Uri> Members

        public IEnumerator<Uri> GetEnumerator()
        {
            return _uriList.GetEnumerator();
        }

        #endregion

        #region IEnumerable Members

        IEnumerator IEnumerable.GetEnumerator()
        {
            return _uriList.GetEnumerator();
        }

        #endregion
        #endregion Interface implementations


        #region IList<Uri> Members

        int IList<Uri>.IndexOf(Uri item)
        {
            return _uriList.IndexOf(item);
        }

        /// <summary>
        /// Implementation of Insert method. A restriction added by this implementation is that
        /// it will throw exception if the item was already added previously.
        /// This is to avoid ambiguity in respect to the expected position (index) of the inserted item.
        /// Caller will have to check first if item was already added.
        /// </summary>
        /// <param name="index"></param>
        /// <param name="item"></param>
        void IList<Uri>.Insert(int index, Uri item)
        {
            if (_uriList.Contains(item))
            {
                throw new ArgumentException(SR.Get(SRID.CustomDictionaryItemAlreadyExists), "item");
            }

            ValidateUri(item);
            _uriList.Insert(index, item);
            
            if (Speller != null)
            {
                Speller.OnDictionaryUriAdded(item);
            }
        }

        void IList<Uri>.RemoveAt(int index)
        {
            Uri uri = _uriList[index];
            _uriList.RemoveAt(index);
            
            if (Speller != null)
            {
                Speller.OnDictionaryUriRemoved(uri);
            }
        }

        /// <summary>
        /// Sets value at specified index.
        /// Speller is notified that value at the index is being replaced, which means
        /// current value at given offset is removed, and new value is added at the same index.
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        Uri IList<Uri>.this[int index]
        {
            get
            {
                return _uriList[index];
            }
            set
            {
                ValidateUri(value);
                Uri oldUri = _uriList[index];
                if (Speller != null)
                {
                    Speller.OnDictionaryUriRemoved(oldUri);
                }                
                _uriList[index] = value;
                if (Speller != null)
                {
                    Speller.OnDictionaryUriAdded(value);
                }
            }
        }

        #endregion

        #region ICollection<Uri> Members

        /// <summary>
        /// Adds new item to the internal collection.
        /// Duplicate items ARE NOT added, but Speller is still notified.
        /// </summary>
        /// <param name="item"></param>
        void ICollection<Uri>.Add(Uri item)
        {
            ValidateUri(item);
            if (!_uriList.Contains(item))
            {
                _uriList.Add(item);
            }

            if (Speller != null)
            {
                Speller.OnDictionaryUriAdded(item);
            }
        }

        void ICollection<Uri>.Clear()
        {
            _uriList.Clear();
            if (Speller != null)
            {
                Speller.OnDictionaryUriCollectionCleared();
            }
        }

        bool ICollection<Uri>.Contains(Uri item)
        {
            return _uriList.Contains(item);
        }

        void ICollection<Uri>.CopyTo(Uri[] array, int arrayIndex)
        {
            _uriList.CopyTo(array, arrayIndex);
        }

        int ICollection<Uri>.Count
        {
            get
            {
                return _uriList.Count;
            }
        }

        bool ICollection<Uri>.IsReadOnly
        {
            get 
            {
                return ((ICollection<Uri>)_uriList).IsReadOnly;
            }
        }

        bool ICollection<Uri>.Remove(Uri item)
        {
            bool removed = _uriList.Remove(item);
            if (removed && (Speller != null))
            {
                Speller.OnDictionaryUriRemoved(item);
            }
            return removed;
        }

        #endregion

        #region IList Members

        /// <summary>
        /// See generic IList'Uri implementation notes.
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        int IList.Add(object value)
        {
            ((IList<Uri>)this).Add((Uri)value);
            return _uriList.IndexOf((Uri)value);
        }

        void IList.Clear()
        {
            ((IList<Uri>)this).Clear();
        }

        bool IList.Contains(object value)
        {
            return ((IList)_uriList).Contains(value);
        }

        int IList.IndexOf(object value)
        {
            return ((IList)_uriList).IndexOf(value);
        }

        void IList.Insert(int index, object value)
        {
            ((IList<Uri>)this).Insert(index, (Uri)value);
        }

        bool IList.IsFixedSize
        {
            get 
            {
                return ((IList)_uriList).IsFixedSize; 
            }
        }

        bool IList.IsReadOnly
        {
            get
            { 
                return ((IList)_uriList).IsReadOnly; 
            }
        }

        void IList.Remove(object value)
        {
            ((IList<Uri>)this).Remove((Uri)value);
        }

        void IList.RemoveAt(int index)
        {
            ((IList<Uri>)this).RemoveAt(index);
        }

        object IList.this[int index]
        {
            get
            {
                return _uriList[index];
            }
            set
            {
                ((IList<Uri>)this)[index] = (Uri)value;
            }
        }

        #endregion

        #region ICollection Members

        void ICollection.CopyTo(Array array, int index)
        {
            ((ICollection)_uriList).CopyTo(array, index);
        }

        int ICollection.Count
        {
            get { return ((IList<Uri>)this).Count; }
        }

        bool ICollection.IsSynchronized
        {
            get
            {
                return ((ICollection)_uriList).IsSynchronized;
            }
        }

        object ICollection.SyncRoot
        {
            get
            {
                return ((ICollection)_uriList).SyncRoot;
            }
        }

        #endregion

        //------------------------------------------------------
        //
        //  Private Properties
        //
        //------------------------------------------------------
        #region Private Properties
        private Speller Speller
        {
            get
            {
                if (_owner.TextEditor == null)
                {
                    return null;
                }
                return _owner.TextEditor.Speller;
            }
        }
        #endregion Private Properties

        //------------------------------------------------------
        //
        //  Private Methods
        //
        //------------------------------------------------------
        #region Private Methods

        /// <summary>
        /// Makes sure Uri is of supported kind. Throws exception for unsupported URI type.
        /// Supported URI types [please keep this list updated]
        /// 1. Local path
        /// 2. Relative path
        /// 3. UNC path
        /// 4. Pack URI
        /// </summary>
        /// <param name="item"></param>
        private static void ValidateUri(Uri item)
        {
            if (item == null)
            {
                throw new ArgumentException(SR.Get(SRID.CustomDictionaryNullItem));
            }
            if (item.IsAbsoluteUri)
            {
                if (!(item.IsUnc || item.IsFile || MS.Internal.IO.Packaging.PackUriHelper.IsPackUri(item)))
                {
                    throw new NotSupportedException(SR.Get(SRID.CustomDictionarySourcesUnsupportedURI));
                }
            }
        }
        #endregion Private Methods
        
        //------------------------------------------------------
        //
        //  Private Fields
        //
        //------------------------------------------------------
        #region Private Fields
        private readonly List<Uri> _uriList;
        private readonly TextBoxBase _owner;
        #endregion Private Fields



    }
}
