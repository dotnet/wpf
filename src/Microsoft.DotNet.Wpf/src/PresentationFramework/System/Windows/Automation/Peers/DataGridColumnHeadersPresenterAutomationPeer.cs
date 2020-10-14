// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Windows.Automation.Provider;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;

namespace System.Windows.Automation.Peers
{
    /// <summary>
    /// AutomationPeer for DataGridColumnHeadersPresenter
    /// </summary>
    public sealed class DataGridColumnHeadersPresenterAutomationPeer : ItemsControlAutomationPeer, IItemContainerProvider
    {
        #region Constructors

        /// <summary>
        /// AutomationPeer for DataGridColumnHeadersPresenter
        /// </summary>
        /// <param name="owner">DataGridColumnHeadersPresenter</param>
        public DataGridColumnHeadersPresenterAutomationPeer(DataGridColumnHeadersPresenter owner)
            : base(owner)
        {
        }

        #endregion

        #region AutomationPeer Overrides

        /// <summary>
        /// Gets the control type for the element that is associated with the UI Automation peer.
        /// </summary>
        /// <returns>The control type.</returns>
        protected override AutomationControlType GetAutomationControlTypeCore()
        {
            return AutomationControlType.Header;
        }

        /// <summary>
        /// Called by GetClassName that gets a human readable name that, in addition to AutomationControlType,
        /// differentiates the control represented by this AutomationPeer.
        /// </summary>
        /// <returns>The string that contains the name.</returns>
        protected override string GetClassNameCore()
        {
            return Owner.GetType().Name;
        }

        ///<summary>
        /// Creates Children peers.
        ///
        /// GetChildrenCore and FindItemByProperty are almost straight copies of the
        /// ItemControlAutomationPeer code; however since DataGridColumHeaderPresenter
        /// returns the Column.Header's as the items some specialized code was needed to
        /// create and store peers.
        ///</summary>
        protected override List<AutomationPeer> GetChildrenCore()
        {
            List<AutomationPeer> children = null;
            ItemPeersStorage<ItemAutomationPeer> oldChildren = ItemPeers; //cache the old ones for possible reuse
            ItemPeers = new ItemPeersStorage<ItemAutomationPeer>();
            ItemsControl owner = (ItemsControl)Owner;

            if (OwningDataGrid?.Columns.Count > 0)
            {
                IList childItems = null;
                Panel itemHost = owner.ItemsHost;

                // To avoid the situation on legacy systems which may not have new unmanaged core. this check with old unmanaged core
                // ensures the older behavior as ItemContainer pattern won't be available.
                if (IsVirtualized)
                {
                    if (itemHost == null)
                        return null;

                    childItems = itemHost.Children;
                }
                else
                {
                    childItems = OwningDataGrid.Columns;
                }
                children = new List<AutomationPeer>(childItems.Count);

                foreach (object item in childItems)
                {
                    DataGridColumn dataItem;
                    if (item is DataGridColumnHeader)
                    {
                        dataItem = ((DataGridColumnHeader) item).Column;
                    }
                    else
                    {
                        dataItem = item as DataGridColumn;
                    }

                    // try to reuse old peer if it exists either in Current AT or in WeakRefStorage of Peers being sent to Client
                    ItemAutomationPeer peer = oldChildren[dataItem];
                    if (peer == null)
                    {
                        peer = GetPeerFromWeakRefStorage(dataItem);

                        // As cached peer is getting used it must be invalidated.
                        if (peer != null)
                        {
                            peer.AncestorsInvalid = false;
                            peer.ChildrenValid = false;
                        }
                    }

                    // If the peer is null or dataItem.Header has changed, create a new peer.
                    object dataItemHeader = dataItem == null ? null : dataItem.Header;
                    if (peer == null ||
                        !ItemsControl.EqualsEx(peer.Item, dataItemHeader))
                    {
                        peer = CreateItemAutomationPeer(dataItem);
                    }

                    // perform hookup so the events sourced from wrapper peer are fired as if from the data item
                    if (peer != null)
                    {
                        AutomationPeer wrapperPeer = peer.GetWrapperPeer();
                        if (wrapperPeer != null)
                        {
                            wrapperPeer.EventsSource = peer;
                        }
                    }

                    // protection from indistinguishable items - for example, 2 strings with same value
                    // this scenario does not work in ItemsControl however is not checked for.
                    if (peer != null && ItemPeers[dataItem] == null)
                    {
                        children.Add(peer);
                        ItemPeers[dataItem] = peer;
                    }
                }

                return children;
            }

            return null;
        }

        ///<summary>
        /// Find Childrend Peers based on Automation Properties.
        /// Used to enable virtualization with automation.
        ///
        /// GetChildrenCore and FindItemByProperty are almost straight copies of the
        /// ItemControlAutomationPeer code; however since DataGridColumHeaderPresenter
        /// returns the Column.Header's as the items some specialized code was needed to
        /// create and store peers.
        ///</summary>
        IRawElementProviderSimple IItemContainerProvider.FindItemByProperty(IRawElementProviderSimple startAfter, int propertyId, object value)
        {
            ResetChildrenCache();
            // Checks if propertyId is valid else throws ArgumentException to notify it as invalid argument is being passed
            if (propertyId != 0)
            {
                if (!IsPropertySupportedByControlForFindItem(propertyId))
                {
                    throw new ArgumentException(SR.Get(SRID.PropertyNotSupported));
                }
            }

            ItemsControl owner = (ItemsControl)Owner;

            IList items = null;
            if (owner != null)
                items = OwningDataGrid.Columns;

            if (items != null && items.Count > 0)
            {
                DataGridColumnHeaderItemAutomationPeer startAfterItem = null;
                if (startAfter != null)
                {
                    // get the peer corresponding to this provider
                    startAfterItem = PeerFromProvider(startAfter) as DataGridColumnHeaderItemAutomationPeer;
                    if (startAfterItem == null)
                        return null;
                }

                // startIndex refers to the index of the item just after startAfterItem
                int startIndex = 0;
                if (startAfterItem != null)
                {
                    if (startAfterItem.Item == null)
                    {
                        throw new InvalidOperationException(SR.Get(SRID.InavalidStartItem));
                    }

                    // To find the index of the column items collection which occurs
                    // immidiately after startAfterItem.Item
                    startIndex = items.IndexOf(startAfterItem.Column) + 1;
                    if (startIndex == 0 || startIndex == items.Count)
                        return null;
                }

                if (propertyId == 0)
                {
                    for (int i = startIndex; i < items.Count; i++)
                    {
                        // This is to handle the case of when dataItems are just plain strings and have duplicates,
                        // only the first occurence of duplicate Items will be returned. It has also been used couple more times below.
                        if (items.IndexOf(items[i]) != i)
                            continue;
                        return (ProviderFromPeer(FindOrCreateItemAutomationPeer(items[i])));
                    }
                }

                ItemAutomationPeer currentItemPeer;
                object currentValue = null;
                for (int i = startIndex; i < items.Count; i++)
                {
                    currentItemPeer = FindOrCreateItemAutomationPeer(items[i]);
                    if (currentItemPeer == null)
                        continue;
                    try
                    {
                        currentValue = GetSupportedPropertyValue(currentItemPeer, propertyId);
                    }
                    catch (Exception ex)
                    {
                        if (ex is ElementNotAvailableException)
                            continue;
                    }

                    if (value == null || currentValue == null)
                    {
                        // Accept null as value corresponding to the property if it finds an item with null as the value of corresponding property else ignore.
                        if (currentValue == null && value == null && items.IndexOf(items[i]) == i)
                            return (ProviderFromPeer(currentItemPeer));
                        else
                            continue;
                    }

                    // Match is found within the specified criterion of search
                    if (value.Equals(currentValue) && items.IndexOf(items[i]) == i)
                        return (ProviderFromPeer(currentItemPeer));
                }
            }
            return null;
        }



        // AutomationControlType.Header must return IsContentElement false.
        // See http://msdn.microsoft.com/en-us/library/ms753110.aspx
        protected override bool IsContentElementCore()
        {
            return false;
        }

        protected override ItemAutomationPeer CreateItemAutomationPeer(object column)
        {
            DataGridColumn dataGridColumn = column as DataGridColumn;
            if (column != null)
            {
                // Pass in the column and the Header in so that ItemsContainerGenerator will give a container
                // when ItemAutomationPeer.GetWrapper is called.
                return new DataGridColumnHeaderItemAutomationPeer(dataGridColumn.Header, dataGridColumn, this) as ItemAutomationPeer;
            }
            return null;
        }

        #endregion

        #region Private Methods

        private DataGrid OwningDataGrid
        {
            get
            {
                return ((DataGridColumnHeadersPresenter)Owner).ParentDataGrid;
            }
        }
        #endregion
    }
}
