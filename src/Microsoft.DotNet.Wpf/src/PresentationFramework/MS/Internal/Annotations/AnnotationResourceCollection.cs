// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
//
// Description: Subclass of AnnotationObservableCollection<T> which has slightly different
//              eventing behavior for ClearItems and SetItem methods.  This class
//              is used specifically for AnnotationResources.
//

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows.Annotations;
using System.Windows.Data;

namespace MS.Internal.Annotations
{
    /// <summary>
    ///     Subclass of AnnotationObservableCollection which has slightly different
    ///     eventing behavior for ClearItems and SetItem methods.  This class
    ///     is used specifically for AnnotationResources.
    /// </summary>
    internal sealed class AnnotationResourceCollection : AnnotationObservableCollection<AnnotationResource>
    {
        //------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------

        #region Constructors

        /// <summary>
        /// Initializes a new instance of AnnotationResourceCollection that is empty and has default initial capacity.
        /// </summary>
        public AnnotationResourceCollection() : base()
        {
        }


        #endregion Constructors

        //------------------------------------------------------
        //
        //  Public Events
        //
        //------------------------------------------------------

        #region Public Events

        /// <summary>
        ///    Event fired when an item in the collection changes (fires a PropertyChanged event).
        /// </summary>
        public event PropertyChangedEventHandler ItemChanged;

        #endregion Public Events

        //------------------------------------------------------
        //
        //  Protected Methods
        //
        //------------------------------------------------------

        #region Protected Methods

        /// <summary>
        ///    Override this method and provide a different set of events
        ///    when items are cleared from the collection.  Specifically,
        ///    fire a Remove event for each item in the collection.
        /// </summary>
        protected override void ProtectedClearItems()
        {
            // We want to fire for each item in the list
            List<AnnotationResource> list = new List<AnnotationResource>(this);
            Items.Clear();  // directly clear Collection<T> inner Items collection
            OnPropertyChanged(CountString);
            OnPropertyChanged(IndexerName);
            OnCollectionCleared(list);
        }

        /// <summary>
        ///     Override this method and provide a different set of events
        ///     when an item is set on a given index in this collection.
        ///     Specifically, fire a both a Remove and Add event (as the
        ///     grand-parent class ObservableCollection does).
        /// </summary>
        /// <param name="index">index of item to set</param>
        /// <param name="item">item to set at that index</param>
        protected override void ProtectedSetItem(int index, AnnotationResource item)
        {
            // Use the standard built in events (one for item removed and one for item added)
            ObservableCollectionSetItem(index, item);  // Calls raw ObservableCollection method
        }

        #endregion Protected Methods


        //------------------------------------------------------
        //
        //  Private Methods
        //
        //------------------------------------------------------

        #region Private Methods

        // fire an event for each item removed from the collection
        void OnCollectionCleared(IEnumerable<AnnotationResource> list)
        {
            foreach (object item in list)
            {
                OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, item, 0));
            }
        }

        private void OnPropertyChanged(string propertyName)
        {
            OnPropertyChanged(new PropertyChangedEventArgs(propertyName));
        }

        // raise CollectionChanged event to any listeners
        protected override void OnItemPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (ItemChanged != null)
            {
                ItemChanged(sender, e);
            }
        }

        #endregion Private Methods
    }
}
