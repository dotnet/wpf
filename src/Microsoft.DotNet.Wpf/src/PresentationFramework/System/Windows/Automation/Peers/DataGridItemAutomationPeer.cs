// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Security;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Automation.Provider;
using System.Windows.Controls;
using System.Windows.Data;
using MS.Internal.Automation;

namespace System.Windows.Automation.Peers
{
    /// <summary>
    /// AutomationPeer for an item in a DataGrid
    /// This automation peer correspond to a row data item which may not have a visual container
    /// </summary>
    public sealed class DataGridItemAutomationPeer : ItemAutomationPeer,
        IInvokeProvider, IScrollItemProvider, ISelectionItemProvider, ISelectionProvider, IItemContainerProvider
    {
        #region Constructors

        /// <summary>
        /// AutomationPeer for an item in a DataGrid
        /// </summary>
        public DataGridItemAutomationPeer(object item, DataGridAutomationPeer dataGridPeer): base(item, dataGridPeer)
        {
            if (item == null)
            {
                throw new ArgumentNullException("item");
            }

            if (dataGridPeer == null)
            {
                throw new ArgumentNullException("dataGridPeer");
            }

            _dataGridAutomationPeer = dataGridPeer;
        }

        #endregion

        #region AutomationPeer Overrides

        /////
        protected override AutomationControlType GetAutomationControlTypeCore()
        {
            return AutomationControlType.DataItem;
        }

        ///
        protected override List<AutomationPeer> GetChildrenCore()
        {
            AutomationPeer wrapperPeer = GetWrapperPeer();
            if (wrapperPeer != null)
            {
                // We need to update children manually since wrapperPeer is not in the Automation Tree
                // When containers are recycled the visual (DataGridRow) will point to a new item. ForceEnsureChildren will just refresh children of this peer,
                // unlike UpdateSubtree which would raise property change events and recursively updates entire subtree.
                // WrapperPeer's children are the peers for DataGridRowHeader, DataGridCells and DataGridRowDetails.
                wrapperPeer.ForceEnsureChildren();
                List<AutomationPeer> children = wrapperPeer.GetChildren();
                return children;
            }

            return GetCellItemPeers();
        }

        ///
        protected override string GetClassNameCore()
        {
            AutomationPeer wrapperPeer = GetWrapperPeer();
            if (wrapperPeer != null)
            {
                return wrapperPeer.GetClassName();
            }
            else
            {
                ThrowElementNotAvailableException();
            }
            return string.Empty;
        }

        ///
        public override object GetPattern(PatternInterface patternInterface)
        {
            switch (patternInterface)
            {
                case PatternInterface.Invoke:
                    if (!this.OwningDataGrid.IsReadOnly)
                    {
                        return this;
                    }

                    break;
                case PatternInterface.ScrollItem:
                case PatternInterface.Selection:
                case PatternInterface.ItemContainer:
                    return this;
                case PatternInterface.SelectionItem:
                    if (IsRowSelectionUnit)
                    {
                        return this;
                    }

                    break;
            }

            return base.GetPattern(patternInterface);
        }


        protected override AutomationPeer GetPeerFromPointCore(Point point)
        {
            if (!IsOffscreen())
            {
                AutomationPeer rowHeaderAutomationPeer = RowHeaderAutomationPeer;
                if (rowHeaderAutomationPeer != null)
                {
                    // Check DataGridRowHeader first
                    AutomationPeer found = rowHeaderAutomationPeer.GetPeerFromPoint(point);
                    if (found != null)
                    {
                        return found;
                    }
                }
            }

            return base.GetPeerFromPointCore(point);
        }

        #endregion

        #region IItemContainerProvider
        IRawElementProviderSimple IItemContainerProvider.FindItemByProperty(IRawElementProviderSimple startAfter, int propertyId, object value)
        {
            ResetChildrenCache();
            // Checks if propertyId is valid else throws ArgumentException to notify it as invalid argument is being passed
            if (propertyId != 0)
            {
                if (!SelectorAutomationPeer.IsPropertySupportedByControlForFindItemInternal(propertyId))
                {
                    throw new ArgumentException(SR.Get(SRID.PropertyNotSupported));
                }
            }


            IList<DataGridColumn> columns = OwningDataGrid.Columns;

            if (columns != null && columns.Count > 0)
            {
                DataGridCellItemAutomationPeer startAfterItem = null;
                if (startAfter != null)
                {
                    // get the peer corresponding to this provider
                    startAfterItem = PeerFromProvider(startAfter) as DataGridCellItemAutomationPeer;
                }

                // startIndex refers to the index of the item just after startAfterItem
                int startIndex = 0;
                if (startAfterItem != null)
                {
                    if (startAfterItem.Column == null)
                    {
                        throw new InvalidOperationException(SR.Get(SRID.InavalidStartItem));
                    }

                    // To find the index of the item in items collection which occurs
                    // immidiately after startAfterItem.Item
                    startIndex = columns.IndexOf(startAfterItem.Column) + 1;
                    if (startIndex == 0 || startIndex == columns.Count)
                        return null;
                }

                if (propertyId == 0 && startIndex < columns.Count)
                {
                    return (ProviderFromPeer(GetOrCreateCellItemPeer(columns[startIndex])));
                }

                DataGridCellItemAutomationPeer currentItemPeer;
                object currentValue = null;
                for (int i = startIndex; i < columns.Count; i++)
                {
                    currentItemPeer = GetOrCreateCellItemPeer(columns[i]);
                    if (currentItemPeer == null)
                        continue;
                    try
                    {
                        currentValue = SelectorAutomationPeer.GetSupportedPropertyValueInternal(currentItemPeer, propertyId);
                    }
                    catch (Exception ex)
                    {
                        if (ex is ElementNotAvailableException)
                            continue;
                    }

                    if (value == null || currentValue == null)
                    {
                        // Accept null as value corresponding to the property if it finds an item with null as the value of corresponding property else ignore.
                        if (currentValue == null && value == null)
                            return (ProviderFromPeer(currentItemPeer));
                        else
                            continue;
                    }

                    // Match is found within the specified criterion of search
                    if (value.Equals(currentValue))
                        return (ProviderFromPeer(currentItemPeer));
                }
            }
            return null;
        }

        #endregion IItemContainerProvider

        #region IInvokeProvider

        // Invoking DataGrid item should commit the item if it is in edit mode
        // or BeginEdit if item is not in edit mode
        void IInvokeProvider.Invoke()
        {
            EnsureEnabled();
            object item = Item;

            AutomationPeer wrapperPeer = GetWrapperPeer();
            if (wrapperPeer == null)
            {
                this.OwningDataGrid.ScrollIntoView(item);
            }

            bool success = false;
            UIElement owningRow = GetWrapper();
            if (owningRow != null)
            {
                IEditableCollectionView iecv = (IEditableCollectionView)this.OwningDataGrid.Items;
                if (iecv.CurrentEditItem == item)
                {
                    success = this.OwningDataGrid.CommitEdit();
                }
                else
                {
                    if (this.OwningDataGrid.Columns.Count > 0)
                    {
                        DataGridCell cell = this.OwningDataGrid.TryFindCell(item, this.OwningDataGrid.Columns[0]);
                        if (cell != null)
                        {
                            this.OwningDataGrid.UnselectAll();
                            cell.Focus();
                            success = this.OwningDataGrid.BeginEdit();
                        }
                    }
                }
            }

            // Invoke on a NewItemPlaceholder row creates a new item.
            // BeginEdit on a NewItemPlaceholder row returns false.
            if (!success && !IsNewItemPlaceholder)
            {
                throw new InvalidOperationException(SR.Get(SRID.DataGrid_AutomationInvokeFailed));
            }
        }

        #endregion

        #region IScrollItemProvider

        void IScrollItemProvider.ScrollIntoView()
        {
            this.OwningDataGrid.ScrollIntoView(Item);
        }

        #endregion

        #region ISelectionItemProvider

        bool ISelectionItemProvider.IsSelected
        {
            get
            {
                return this.OwningDataGrid.SelectedItems.Contains(Item);
            }
        }

        IRawElementProviderSimple ISelectionItemProvider.SelectionContainer
        {
            get
            {
                return ProviderFromPeer(_dataGridAutomationPeer);
            }
        }

        void ISelectionItemProvider.AddToSelection()
        {
            if (!IsRowSelectionUnit)
            {
                throw new InvalidOperationException(SR.Get(SRID.DataGridRow_CannotSelectRowWhenCells));
            }

            // If item is already selected - do nothing
            object item = Item;
            if (this.OwningDataGrid.SelectedItems.Contains(item))
            {
                return;
            }

            EnsureEnabled();

            if (this.OwningDataGrid.SelectionMode == DataGridSelectionMode.Single &&
                this.OwningDataGrid.SelectedItems.Count > 0)
            {
                throw new InvalidOperationException();
            }

            if (this.OwningDataGrid.Items.Contains(item))
            {
                this.OwningDataGrid.SelectedItems.Add(item);
            }
        }

        void ISelectionItemProvider.RemoveFromSelection()
        {
            if (!IsRowSelectionUnit)
            {
                throw new InvalidOperationException(SR.Get(SRID.DataGridRow_CannotSelectRowWhenCells));
            }

            EnsureEnabled();

            object item = Item;
            if (this.OwningDataGrid.SelectedItems.Contains(item))
            {
                this.OwningDataGrid.SelectedItems.Remove(item);
            }
        }

        void ISelectionItemProvider.Select()
        {
            if (!IsRowSelectionUnit)
            {
                throw new InvalidOperationException(SR.Get(SRID.DataGridRow_CannotSelectRowWhenCells));
            }

            EnsureEnabled();

            this.OwningDataGrid.SelectedItem = Item;
        }

        #endregion

        #region ISelectionProvider

        bool ISelectionProvider.CanSelectMultiple
        {
            get
            {
                return this.OwningDataGrid.SelectionMode == DataGridSelectionMode.Extended;
            }
        }

        bool ISelectionProvider.IsSelectionRequired
        {
            get
            {
                return false;
            }
        }

        IRawElementProviderSimple[] ISelectionProvider.GetSelection()
        {
            DataGrid dataGrid = this.OwningDataGrid;
            if (dataGrid == null)
            {
                return null;
            }

            int rowIndex = dataGrid.Items.IndexOf(Item);

            // If row has selection
            if (rowIndex > -1 && dataGrid.SelectedCellsInternal.Intersects(rowIndex))
            {
                List<IRawElementProviderSimple> selectedProviders = new List<IRawElementProviderSimple>();

                for (int i = 0; i < this.OwningDataGrid.Columns.Count; i++)
                {
                    // cell is selected
                    if (dataGrid.SelectedCellsInternal.Contains(rowIndex, i))
                    {
                        DataGridColumn column = dataGrid.ColumnFromDisplayIndex(i);
                        DataGridCellItemAutomationPeer peer = GetOrCreateCellItemPeer(column);
                        if (peer != null)
                        {
                            selectedProviders.Add(ProviderFromPeer(peer));
                        }
                    }
                }

                if (selectedProviders.Count > 0)
                {
                    return selectedProviders.ToArray();
                }
            }

            return null;
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Realized Columns only
        /// </summary>
        /// <returns></returns>
        internal List<AutomationPeer> GetCellItemPeers()
        {
            List<AutomationPeer> children = null;
            ItemPeersStorage<DataGridCellItemAutomationPeer> newChildren = new ItemPeersStorage<DataGridCellItemAutomationPeer>();

            IList childItems = null;
            bool usingItemsHost = false;
            DataGridRow row = GetWrapper() as DataGridRow;
            if (row != null)
            {
                if (row.CellsPresenter != null)
                {
                    Panel itemHost = row.CellsPresenter.ItemsHost;
                    if (itemHost != null)
                    {
                        childItems = itemHost.Children;
                        usingItemsHost = true;
                    }
                }
            }

            if (!usingItemsHost)
            {
                childItems = OwningDataGrid.Columns;
            }

            if (childItems != null)
            {
                children = new List<AutomationPeer>(childItems.Count);
                foreach (object childItem in childItems)
                {
                    DataGridColumn column = null;
                    if (usingItemsHost)
                    {
                        column = (childItem as DataGridCell).Column;
                    }
                    else
                    {
                        column = childItem as DataGridColumn;
                    }

                    if (column != null)
                    {
                        DataGridCellItemAutomationPeer peer = GetOrCreateCellItemPeer(column,/*addParentInfo*/ false );
                        children.Add(peer);
                        newChildren[column] = peer;
                    }
                }
            }

            // Cache children for reuse
            CellItemPeers = newChildren;
            return children;
        }

        internal DataGridCellItemAutomationPeer GetOrCreateCellItemPeer(DataGridColumn column)
        {
            return GetOrCreateCellItemPeer(column, /*addParentInfo*/ true);
        }

        /// <summary>
        /// It returns the CellItemAutomationPeer if it exist corresponding to the item otherwise it creates
        /// one and adds the Handle and parent info by calling AddParentInfo.
        /// </summary>
        /// <param name="column"></param>
        /// <param name="addParentInfo">only required when creating peers for virtualized cells</param>
        /// <returns></returns>
        private DataGridCellItemAutomationPeer GetOrCreateCellItemPeer(DataGridColumn column, bool addParentInfo)
        {
            // try to reuse old peer if it exists either in Current AT or in WeakRefStorage of Peers being sent to Client
            DataGridCellItemAutomationPeer peer = CellItemPeers[column];
            if (peer == null)
            {
                peer = GetPeerFromWeakRefStorage(column);
                if (peer != null && !addParentInfo)
                {
                    // As cached peer is getting used it must be invalidated. addParentInfo check ensures that call is coming from GetChildrenCore
                    peer.AncestorsInvalid = false;
                    peer.ChildrenValid = false;
                }
            }

            if (peer == null)
            {
                peer = new DataGridCellItemAutomationPeer(Item, column);
                if (addParentInfo && peer != null)
                {
                    peer.TrySetParentInfo(this);
                }
            }

            //perform hookup so the events sourced from wrapper peer are fired as if from the data item
            AutomationPeer wrapperPeer = peer.OwningCellPeer;
            if (wrapperPeer != null)
            {
                wrapperPeer.EventsSource = peer;
            }

            return peer;
        }

        // Provides Peer if exist in Weak Reference Storage
        private DataGridCellItemAutomationPeer GetPeerFromWeakRefStorage(object column)
        {
            DataGridCellItemAutomationPeer returnPeer = null;
            WeakReference weakRefEP = WeakRefElementProxyStorage[column];
            if (weakRefEP != null)
            {
                ElementProxy provider = weakRefEP.Target as ElementProxy;
                if (provider != null)
                {
                    returnPeer = PeerFromProvider(provider as IRawElementProviderSimple) as DataGridCellItemAutomationPeer;
                    if (returnPeer == null)
                        WeakRefElementProxyStorage.Remove(column);
                }
                else
                    WeakRefElementProxyStorage.Remove(column);
            }

            return returnPeer;
        }

        // Called by DataGridCellItemAutomationPeer
        internal void AddProxyToWeakRefStorage(WeakReference wr, DataGridCellItemAutomationPeer cellItemPeer)
        {
            IList<DataGridColumn> columns = OwningDataGrid.Columns;
            if (columns != null && columns.Contains(cellItemPeer.Column))
            {
                if (GetPeerFromWeakRefStorage(cellItemPeer.Column) == null)
                    WeakRefElementProxyStorage[cellItemPeer.Column] = wr;
            }
        }

        private void EnsureEnabled()
        {
            if (!_dataGridAutomationPeer.IsEnabled())
            {
                throw new ElementNotEnabledException();
            }
        }

        #endregion

        #region Private Properties

        private bool IsRowSelectionUnit
        {
            get
            {
                return (this.OwningDataGrid != null &&
                    (this.OwningDataGrid.SelectionUnit == DataGridSelectionUnit.FullRow ||
                    this.OwningDataGrid.SelectionUnit == DataGridSelectionUnit.CellOrRowHeader));
            }
        }

        private bool IsNewItemPlaceholder
        {
            get
            {
                object item = Item;
                return (item == CollectionView.NewItemPlaceholder) || (item == DataGrid.NewItemPlaceholder);
            }
        }

        internal AutomationPeer RowHeaderAutomationPeer
        {
            get
            {
                DataGridRowAutomationPeer owningRowPeer = GetWrapperPeer() as DataGridRowAutomationPeer;
                return (owningRowPeer != null) ? owningRowPeer.RowHeaderAutomationPeer : null;
            }
        }

        private DataGrid OwningDataGrid
        {
            get
            {
                DataGridAutomationPeer gridPeer = _dataGridAutomationPeer as DataGridAutomationPeer;
                return (DataGrid)gridPeer.Owner;
            }
        }

        /// <summary>
        /// Used to cache realized peers. We donot store references to virtualized peers.
        /// </summary>
        private ItemPeersStorage<DataGridCellItemAutomationPeer> CellItemPeers
        {
            get { return _dataChildren; }

            set { _dataChildren = value; }
        }

        private ItemPeersStorage<WeakReference> WeakRefElementProxyStorage
        {
            get { return _weakRefElementProxyStorage; }
        }

        #endregion

        #region Data

        private AutomationPeer _dataGridAutomationPeer;
        private ItemPeersStorage<DataGridCellItemAutomationPeer> _dataChildren = new ItemPeersStorage<DataGridCellItemAutomationPeer>();
        private ItemPeersStorage<WeakReference> _weakRefElementProxyStorage = new ItemPeersStorage<WeakReference>();

        #endregion
    }
}
