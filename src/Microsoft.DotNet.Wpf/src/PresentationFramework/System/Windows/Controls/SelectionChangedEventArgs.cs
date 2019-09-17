// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections;
using System.Collections.Generic;

using System.Windows;
using System.Windows.Controls.Primitives;

namespace System.Windows.Controls
{
    /// <summary>
    /// The delegate type for handling a selection changed event
    /// </summary>
    public delegate void SelectionChangedEventHandler(
        object sender,
        SelectionChangedEventArgs e);

    /// <summary>
    /// The inputs to a selection changed event handler
    /// </summary>
    public class SelectionChangedEventArgs : RoutedEventArgs
    {
        #region Constructors

        /// <summary>
        /// The constructor for selection changed args
        /// </summary>
        /// <param name="id">The event ID for the event about to fire -- should probably be Selector.SelectionChangedEvent</param>
        /// <param name="removedItems">The items that were unselected during this event</param>
        /// <param name="addedItems">The items that were selected during this event</param>
        public SelectionChangedEventArgs(
            RoutedEvent id,
            IList removedItems,
            IList addedItems)
        {
            if (id == null)
                throw new ArgumentNullException("id");
            if (removedItems == null)
                throw new ArgumentNullException("removedItems");
            if (addedItems == null)
                throw new ArgumentNullException("addedItems");

            RoutedEvent = id;

            _removedItems = new object[removedItems.Count];
            removedItems.CopyTo(_removedItems, 0);

            _addedItems = new object[addedItems.Count];
            addedItems.CopyTo(_addedItems, 0);
        }

        internal SelectionChangedEventArgs(List<ItemsControl.ItemInfo> unselectedInfos, List<ItemsControl.ItemInfo> selectedInfos)
        {
            RoutedEvent = Selector.SelectionChangedEvent;

            _removedItems = new object[unselectedInfos.Count];
            for (int i=0; i<unselectedInfos.Count; ++i)
            {
                _removedItems[i] = unselectedInfos[i].Item;
            }

            _addedItems = new object[selectedInfos.Count];
            for (int i=0; i<selectedInfos.Count; ++i)
            {
                _addedItems[i] = selectedInfos[i].Item;
            }

            _removedInfos = unselectedInfos;
            _addedInfos = selectedInfos;
        }

        #endregion

        #region Public Properties

        /// <summary>
        /// An IList containing the items that were unselected during this event
        /// </summary>
        public IList RemovedItems
        {
            get { return _removedItems; }
        }

        /// <summary>
        /// An IList containing the items that were selected during this event
        /// </summary>
        public IList AddedItems
        {
            get { return _addedItems; }
        }

        #endregion

        #region Internal Properties

        /// <summary>
        /// A list containing the ItemInfos that were unselected during this event
        /// </summary>
        internal List<ItemsControl.ItemInfo> RemovedInfos
        {
            get { return _removedInfos; }
        }

        /// <summary>
        /// A list containing the ItemInfos that were selected during this event
        /// </summary>
        internal List<ItemsControl.ItemInfo> AddedInfos
        {
            get { return _addedInfos; }
        }

        #endregion

        #region Protected Methods

        /// <summary>
        /// This method is used to perform the proper type casting in order to
        /// call the type-safe SelectionChangedEventHandler delegate for the SelectionChangedEvent event.
        /// </summary>
        /// <param name="genericHandler">The handler to invoke.</param>
        /// <param name="genericTarget">The current object along the event's route.</param>
        protected override void InvokeEventHandler(Delegate genericHandler, object genericTarget)
        {
            SelectionChangedEventHandler handler = (SelectionChangedEventHandler)genericHandler;

            handler(genericTarget, this);
        }

        #endregion

        #region Data

        private object[] _addedItems;
        private object[] _removedItems;
        private List<ItemsControl.ItemInfo> _addedInfos;
        private List<ItemsControl.ItemInfo> _removedInfos;

        #endregion
    }
}
