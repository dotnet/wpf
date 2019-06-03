// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Windows;
using System.Collections.ObjectModel;
using System.Diagnostics;

namespace System.Windows
{
    internal class ResourceDictionaryCollection : ObservableCollection<ResourceDictionary>
    {
        #region Constructor

        internal ResourceDictionaryCollection(ResourceDictionary owner)
        {
            Debug.Assert(owner != null, "ResourceDictionaryCollection's owner cannot be null");

            _owner = owner;
        }

        #endregion Constructor
        
        #region ProtectedMethods

        /// <summary>
        /// Called by base class Collection&lt;T&gt; when the list is being cleared;
        /// raises a CollectionChanged event to any listeners.
        /// </summary>
        protected override void ClearItems()
        {
            for (int i=0; i<Count; i++)
            {
                _owner.RemoveParentOwners(this[i]);
            }
            
            base.ClearItems();
        }

        /// <summary>
        /// Called by base class Collection&lt;T&gt; when an item is added to list;
        /// raises a CollectionChanged event to any listeners.
        /// </summary>
        protected override void InsertItem(int index, ResourceDictionary item)
        {
            if (item == null)
            {
                throw new ArgumentNullException("item");
            }

            base.InsertItem(index, item);
        }

        /// <summary>
        /// Called by base class Collection&lt;T&gt; when an item is set in list;
        /// raises a CollectionChanged event to any listeners.
        /// </summary>
        protected override void SetItem(int index, ResourceDictionary item)
        {
            if (item == null)
            {
                throw new ArgumentNullException("item");
            }

            base.SetItem(index, item);
        }

        #endregion ProtectedMethods

        #region Data

        private ResourceDictionary _owner;
        
        #endregion Data
    }
}

