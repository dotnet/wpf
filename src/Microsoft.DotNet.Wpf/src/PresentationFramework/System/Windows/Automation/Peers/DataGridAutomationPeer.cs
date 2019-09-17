// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows.Automation;
using System.Windows.Automation.Provider;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;

namespace System.Windows.Automation.Peers
{
    /// <summary>
    /// AutomationPeer for DataGrid
    /// Supports Grid, Table, Selection and Scroll patters
    /// </summary>
    public sealed class DataGridAutomationPeer : ItemsControlAutomationPeer,
        IGridProvider, ISelectionProvider, ITableProvider
    {
        #region Constructors

        /// <summary>
        /// Default contructor
        /// </summary>
        /// <param name="owner">DataGrid</param>
        public DataGridAutomationPeer(DataGrid owner)
            : base(owner)
        {
            if (owner == null)
            {
                throw new ArgumentNullException("owner");
            }
        }

        #endregion

        #region AutomationPeer Overrides

        /// <summary>
        /// Gets the control type for the element that is associated with the UI Automation peer.
        /// </summary>
        /// <returns>The control type.</returns>
        protected override AutomationControlType GetAutomationControlTypeCore()
        {
            return AutomationControlType.DataGrid;
        }

        protected override List<AutomationPeer> GetChildrenCore()
        {
            List<AutomationPeer> children = base.GetChildrenCore();
            DataGridColumnHeadersPresenter columnsHeaderPresenter = this.OwningDataGrid.ColumnHeadersPresenter;

            // Add ColumnsHeaderPresenter if it is visible
            if (columnsHeaderPresenter != null && columnsHeaderPresenter.IsVisible)
            {
                AutomationPeer columnsHeaderPresenterPeer = FrameworkElementAutomationPeer.CreatePeerForElement(columnsHeaderPresenter);
                if (columnsHeaderPresenterPeer != null)
                {
                    if (children == null)
                    {
                        children = new List<AutomationPeer>(1);
                    }
                    children.Insert(0, columnsHeaderPresenterPeer);
                }
            }

            return children;
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

        /// <summary>
        /// Gets the control pattern that is associated with the specified System.Windows.Automation.Peers.PatternInterface.
        /// </summary>
        /// <param name="patternInterface">A value from the System.Windows.Automation.Peers.PatternInterface enumeration.</param>
        /// <returns>The object that supports the specified pattern, or null if unsupported.</returns>
        public override object GetPattern(PatternInterface patternInterface)
        {
            switch (patternInterface)
            {
                case PatternInterface.Grid:
                case PatternInterface.Selection:
                case PatternInterface.Table:
                    return this;
                case PatternInterface.Scroll:
                    {
                        ScrollViewer scrollViewer = this.OwningDataGrid.InternalScrollHost;
                        if (scrollViewer != null)
                        {
                            AutomationPeer scrollPeer = UIElementAutomationPeer.CreatePeerForElement(scrollViewer);
                            IScrollProvider scrollProvider = scrollPeer as IScrollProvider;
                            if (scrollPeer != null && scrollProvider != null)
                            {
                                scrollPeer.EventsSource = this;
                                return scrollProvider;
                            }
                        }

                        break;
                    }
            }

            return base.GetPattern(patternInterface);
        }

        override protected ItemAutomationPeer CreateItemAutomationPeer(object item)
        {
            return new DataGridItemAutomationPeer(item, this);
        }

        internal override bool IsPropertySupportedByControlForFindItem(int id)
        {
            return SelectorAutomationPeer.IsPropertySupportedByControlForFindItemInternal(id);
        }

        internal override object GetSupportedPropertyValue(ItemAutomationPeer itemPeer, int propertyId)
        {
            return SelectorAutomationPeer.GetSupportedPropertyValueInternal(itemPeer, propertyId);
        }

        #endregion

        #region IGridProvider

        int IGridProvider.ColumnCount
        {
            get
            {
                return this.OwningDataGrid.Columns.Count;
            }
        }

        int IGridProvider.RowCount
        {
            get
            {
                return this.OwningDataGrid.Items.Count;
            }
        }

        IRawElementProviderSimple IGridProvider.GetItem(int row, int column)
        {
            if (row >= 0 && row < this.OwningDataGrid.Items.Count &&
                column >= 0 && column < this.OwningDataGrid.Columns.Count)
            {
                object item = this.OwningDataGrid.Items[row];
                DataGridColumn dataGridColumn = this.OwningDataGrid.Columns[column];
                this.OwningDataGrid.ScrollIntoView(item, dataGridColumn);
                this.OwningDataGrid.UpdateLayout();

                DataGridItemAutomationPeer itemPeer = this.FindOrCreateItemAutomationPeer(item) as DataGridItemAutomationPeer;
                if (itemPeer != null)
                {
                    DataGridCellItemAutomationPeer cellItemPeer = itemPeer.GetOrCreateCellItemPeer(dataGridColumn);
                    if (cellItemPeer != null)
                    {
                        return ProviderFromPeer(cellItemPeer);
                    }
                }
            }

            return null;
        }

        #endregion

        #region ISelectionProvider

        IRawElementProviderSimple[] ISelectionProvider.GetSelection()
        {
            List<IRawElementProviderSimple> selectedProviders = new List<IRawElementProviderSimple>();

            switch (this.OwningDataGrid.SelectionUnit)
            {
                case DataGridSelectionUnit.Cell:
                    {
                        AddSelectedCells(selectedProviders);
                        break;
                    }

                case DataGridSelectionUnit.FullRow:
                    {
                        AddSelectedRows(selectedProviders);
                        break;
                    }

                case DataGridSelectionUnit.CellOrRowHeader:
                    {
                        AddSelectedRows(selectedProviders);
                        AddSelectedCells(selectedProviders);
                        break;
                    }

                default:
                    {
                        Debug.Assert(false);
                        break;
                    }
            }

            return selectedProviders.ToArray();
        }

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

        #endregion

        #region ITableProvider

        RowOrColumnMajor ITableProvider.RowOrColumnMajor
        {
            get
            {
                return RowOrColumnMajor.RowMajor;
            }
        }

        IRawElementProviderSimple[] ITableProvider.GetColumnHeaders()
        {
            if ((this.OwningDataGrid.HeadersVisibility & DataGridHeadersVisibility.Column) == DataGridHeadersVisibility.Column)
            {
                List<IRawElementProviderSimple> providers = new List<IRawElementProviderSimple>();
                DataGridColumnHeadersPresenter dataGridColumnHeadersPresenter = this.OwningDataGrid.ColumnHeadersPresenter;
                if (dataGridColumnHeadersPresenter != null)
                {
                    DataGridColumnHeadersPresenterAutomationPeer dataGridColumnHeadersPresenterPeer = dataGridColumnHeadersPresenter.GetAutomationPeer()  as DataGridColumnHeadersPresenterAutomationPeer;
                    if (dataGridColumnHeadersPresenterPeer != null)
                    {
                        for (int i = 0; i < this.OwningDataGrid.Columns.Count; i++)
                        {
                            AutomationPeer peer = dataGridColumnHeadersPresenterPeer.FindOrCreateItemAutomationPeer(this.OwningDataGrid.Columns[i]);
                            if (peer != null)
                            {
                                providers.Add(ProviderFromPeer(peer));
                            }
                        }

                        if (providers.Count > 0)
                        {
                            return providers.ToArray();
                        }
                    }
                }
            }

            return null;
        }

        // Row virtualization does not allow us to enumerate all the row headers because
        // Their visual and bindings cannot be predicted before the row is devirtualized
        // This method will return only the list of devirtualized rows headers.
        IRawElementProviderSimple[] ITableProvider.GetRowHeaders()
        {
            if ((this.OwningDataGrid.HeadersVisibility & DataGridHeadersVisibility.Row) == DataGridHeadersVisibility.Row)
            {
                List<IRawElementProviderSimple> providers = new List<IRawElementProviderSimple>();

                foreach (object item in this.OwningDataGrid.Items)
                {
                    DataGridItemAutomationPeer dataGridItemAutomationPeer = FindOrCreateItemAutomationPeer(item) as DataGridItemAutomationPeer;
                    AutomationPeer rowHeaderAutomationPeer = dataGridItemAutomationPeer.RowHeaderAutomationPeer;
                    if (rowHeaderAutomationPeer != null)
                    {
                        providers.Add(ProviderFromPeer(rowHeaderAutomationPeer));
                    }
                }

                if (providers.Count > 0)
                {
                    return providers.ToArray();
                }
            }

            return null;
        }

        #endregion

        #region Private Helpers

        private DataGrid OwningDataGrid
        {
            get
            {
                return (DataGrid)Owner;
            }
        }

        // Private helper returning the automation peer coresponding to cellInfo
        // Cell can be virtualized
        private DataGridCellItemAutomationPeer GetCellItemPeer(DataGridCellInfo cellInfo)
        {
            if (cellInfo.IsValid)
            {
                DataGridItemAutomationPeer dataGridItemAutomationPeer = FindOrCreateItemAutomationPeer(cellInfo.Item) as DataGridItemAutomationPeer;
                if (dataGridItemAutomationPeer != null)
                {
                    return dataGridItemAutomationPeer.GetOrCreateCellItemPeer(cellInfo.Column);
                }
            }

            return null;
        }

        // This method is called from DataGrid.OnSelectedCellsChanged
        // Raises the selection events when Cell selection changes
        internal void RaiseAutomationCellSelectedEvent(SelectedCellsChangedEventArgs e)
        {
            // If the result of an AddToSelection or RemoveFromSelection is a single selected cell,
            // then all we raise is the ElementSelectedEvent for single item
            if (AutomationPeer.ListenerExists(AutomationEvents.SelectionItemPatternOnElementSelected) &&
                this.OwningDataGrid.SelectedCells.Count == 1 && e.AddedCells.Count == 1)
            {
                DataGridCellItemAutomationPeer cellPeer = GetCellItemPeer(e.AddedCells[0]);
                if (cellPeer != null)
                {
                    cellPeer.RaiseAutomationEvent(AutomationEvents.SelectionItemPatternOnElementSelected);
                }
            }
            else
            {
                int i;
                if (AutomationPeer.ListenerExists(AutomationEvents.SelectionItemPatternOnElementAddedToSelection))
                {
                    for (i = 0; i < e.AddedCells.Count; i++)
                    {
                        DataGridCellItemAutomationPeer cellPeer = GetCellItemPeer(e.AddedCells[i]);
                        if (cellPeer != null)
                        {
                            cellPeer.RaiseAutomationEvent(AutomationEvents.SelectionItemPatternOnElementAddedToSelection);
                        }
                    }
                }

                if (AutomationPeer.ListenerExists(AutomationEvents.SelectionItemPatternOnElementRemovedFromSelection))
                {
                    for (i = 0; i < e.RemovedCells.Count; i++)
                    {
                        DataGridCellItemAutomationPeer cellPeer = GetCellItemPeer(e.RemovedCells[i]);
                        if (cellPeer != null)
                        {
                            cellPeer.RaiseAutomationEvent(AutomationEvents.SelectionItemPatternOnElementRemovedFromSelection);
                        }
                    }
                }
            }
        }

        // This method is called from DataGrid.OnBeginningEdit/OnCommittingEdit/OnCancelingEdit
        // Raises Invoked event when row begin/cancel/commit edit
        internal void RaiseAutomationRowInvokeEvents(DataGridRow row)
        {
            DataGridItemAutomationPeer dataGridItemAutomationPeer = FindOrCreateItemAutomationPeer(row.Item) as DataGridItemAutomationPeer;
            if (dataGridItemAutomationPeer != null)
            {
                dataGridItemAutomationPeer.RaiseAutomationEvent(AutomationEvents.InvokePatternOnInvoked);
            }
        }

        // This method is called from DataGrid.OnBeginningEdit/OnCommittingEdit/OnCancelingEdit
        // Raises Invoked event when cell begin/cancel/commit edit
        internal void RaiseAutomationCellInvokeEvents(DataGridColumn column, DataGridRow row)
        {
            DataGridItemAutomationPeer dataGridItemAutomationPeer = FindOrCreateItemAutomationPeer(row.Item) as DataGridItemAutomationPeer;
            if (dataGridItemAutomationPeer != null)
            {
                DataGridCellItemAutomationPeer cellPeer = dataGridItemAutomationPeer.GetOrCreateCellItemPeer(column);
                if (cellPeer != null)
                {
                    cellPeer.RaiseAutomationEvent(AutomationEvents.InvokePatternOnInvoked);
                }
            }
        }

        // This method is called from DataGrid.OnSelectionChanged
        // Raises the selection events when Items selection changes
        internal void RaiseAutomationSelectionEvents(SelectionChangedEventArgs e)
        {
            int numSelected = this.OwningDataGrid.SelectedItems.Count;
            int numAdded = e.AddedItems.Count;

            // If the result of an AddToSelection or RemoveFromSelection is a single selected item,
            // then all we raise is the ElementSelectedEvent for single item
            if (AutomationPeer.ListenerExists(AutomationEvents.SelectionItemPatternOnElementSelected) &&
                numSelected == 1 && numAdded == 1)
            {
                ItemAutomationPeer peer = FindOrCreateItemAutomationPeer(this.OwningDataGrid.SelectedItem);
                if (peer != null)
                {
                    peer.RaiseAutomationEvent(AutomationEvents.SelectionItemPatternOnElementSelected);
                }
            }
            else
            {
                int i;
                if (AutomationPeer.ListenerExists(AutomationEvents.SelectionItemPatternOnElementAddedToSelection))
                {
                    for (i = 0; i < e.AddedItems.Count; i++)
                    {
                        ItemAutomationPeer peer = FindOrCreateItemAutomationPeer(e.AddedItems[i]);
                        if (peer != null)
                        {
                            peer.RaiseAutomationEvent(AutomationEvents.SelectionItemPatternOnElementAddedToSelection);
                        }
                    }
                }

                if (AutomationPeer.ListenerExists(AutomationEvents.SelectionItemPatternOnElementRemovedFromSelection))
                {
                    for (i = 0; i < e.RemovedItems.Count; i++)
                    {
                        ItemAutomationPeer peer = FindOrCreateItemAutomationPeer(e.RemovedItems[i]);
                        if (peer != null)
                        {
                            peer.RaiseAutomationEvent(AutomationEvents.SelectionItemPatternOnElementRemovedFromSelection);
                        }
                    }
                }
            }
        }

        private void AddSelectedCells(List<IRawElementProviderSimple> cellProviders)
        {
            if (cellProviders == null)
            {
                throw new ArgumentNullException("cellProviders");
            }

            // Add selected cells to selection
            if (this.OwningDataGrid.SelectedCells != null)
            {
                foreach (DataGridCellInfo cellInfo in this.OwningDataGrid.SelectedCells)
                {
                    DataGridItemAutomationPeer itemPeer = this.FindOrCreateItemAutomationPeer(cellInfo.Item) as DataGridItemAutomationPeer;
                    if (itemPeer != null)
                    {
                        IRawElementProviderSimple provider = ProviderFromPeer(itemPeer.GetOrCreateCellItemPeer(cellInfo.Column));
                        if (provider != null)
                        {
                            cellProviders.Add(provider);
                        }
                    }
                }
            }
        }

        private void AddSelectedRows(List<IRawElementProviderSimple> itemProviders)
        {
            if (itemProviders == null)
            {
                throw new ArgumentNullException("itemProviders");
            }

            // Add selected items to selection
            if (this.OwningDataGrid.SelectedItems != null)
            {
                foreach (object item in this.OwningDataGrid.SelectedItems)
                {
                    IRawElementProviderSimple provider = ProviderFromPeer(FindOrCreateItemAutomationPeer(item));
                    if (provider != null)
                    {
                        itemProviders.Add(provider);
                    }
                }
            }
        }

        #endregion
    }
}
