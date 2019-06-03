// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Automation.Provider;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;


namespace System.Windows.Automation.Peers
{
    /// <summary>
    /// AutomationPeer for a cell item in a DataGridRow.
    /// Cell may not have a visual container if it is scrolled out of view.
    /// </summary>
    public sealed class DataGridCellItemAutomationPeer : AutomationPeer,
        IGridItemProvider, ITableItemProvider, IInvokeProvider, IScrollItemProvider, ISelectionItemProvider, IValueProvider, IVirtualizedItemProvider
    {
        #region Constructors

        /// <summary>
        /// AutomationPeer for an item in a DataGrid
        /// </summary>
        public DataGridCellItemAutomationPeer(object item, DataGridColumn dataGridColumn) : base()
        {
            if (item == null)
            {
                throw new ArgumentNullException("item");
            }

            if (dataGridColumn == null)
            {
                throw new ArgumentNullException("dataGridColumn");
            }

            _item = new WeakReference(item);
            _column = dataGridColumn;
        }

        #endregion

        #region AutomationPeer Overrides

        ///
        protected override string GetAcceleratorKeyCore()
        {
            AutomationPeer wrapperPeer = OwningCellPeer;
            if (wrapperPeer != null)
                return wrapperPeer.GetAcceleratorKey();
            else
                ThrowElementNotAvailableException();

            return string.Empty;
        }

        ///
        protected override string GetAccessKeyCore()
        {
            AutomationPeer wrapperPeer = OwningCellPeer;
            if (wrapperPeer != null)
                return wrapperPeer.GetAccessKey();
            else
                ThrowElementNotAvailableException();

            return string.Empty;
        }

        ///
        protected override AutomationControlType GetAutomationControlTypeCore()
        {
            return AutomationControlType.Custom;
        }

        ///
        protected override string GetAutomationIdCore()
        {
            AutomationPeer wrapperPeer = OwningCellPeer;
            if (wrapperPeer != null)
                return wrapperPeer.GetAutomationId();
            else
                ThrowElementNotAvailableException();

            return string.Empty;
        }

        ///
        protected override Rect GetBoundingRectangleCore()
        {
            AutomationPeer wrapperPeer = OwningCellPeer;
            if (wrapperPeer != null)
            {
                return wrapperPeer.GetBoundingRectangle();
            }
            else
                ThrowElementNotAvailableException();

            return new Rect();
        }

        ///
        protected override List<AutomationPeer> GetChildrenCore()
        {
            AutomationPeer wrapperPeer = OwningCellPeer;
            if (wrapperPeer != null)
            {
                // We need to manually update children here since the wrapperPeer is not in the automation tree.
                // When containers are recycled the visual (DataGridCell) will point to a new item. ForceEnsureChildren will just refresh children of this peer,
                // unlike UpdateSubtree which would raise property change events and recursively updates entire subtree.
                // WrapperPeer's children need to be updated when switching from Editing mode to Non-editing mode and back.
                wrapperPeer.ForceEnsureChildren();
                List<AutomationPeer> children = wrapperPeer.GetChildren();
                return children;
            }

            return null;
        }

        ///
        protected override string GetClassNameCore()
        {
            AutomationPeer wrapperPeer = OwningCellPeer;
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
        protected override Point GetClickablePointCore()
        {
            AutomationPeer wrapperPeer = OwningCellPeer;
            if (wrapperPeer != null)
                return wrapperPeer.GetClickablePoint();
            else
                ThrowElementNotAvailableException();

            return new Point(double.NaN, double.NaN);
        }

        ///
        protected override string GetHelpTextCore()
        {
            AutomationPeer wrapperPeer = OwningCellPeer;
            if (wrapperPeer != null)
                return wrapperPeer.GetHelpText();
            else
                ThrowElementNotAvailableException();

            return string.Empty;
        }

        ///
        protected override string GetItemStatusCore()
        {
            AutomationPeer wrapperPeer = OwningCellPeer;
            if (wrapperPeer != null)
                return wrapperPeer.GetItemStatus();
            else
                ThrowElementNotAvailableException();

            return string.Empty;
        }

        ///
        protected override string GetItemTypeCore()
        {
            AutomationPeer wrapperPeer = OwningCellPeer;
            if (wrapperPeer != null)
                return wrapperPeer.GetItemType();
            else
                ThrowElementNotAvailableException();

            return string.Empty;
        }

        ///
        protected override AutomationPeer GetLabeledByCore()
        {
            AutomationPeer wrapperPeer = OwningCellPeer;
            if (wrapperPeer != null)
                return wrapperPeer.GetLabeledBy();
            else
                ThrowElementNotAvailableException();

            return null;
        }

        ///
        protected override string GetLocalizedControlTypeCore()
        {
            if (!AccessibilitySwitches.UseNetFx47CompatibleAccessibilityFeatures)
            {
                return SR.Get(SRID.DataGridCellItemAutomationPeer_LocalizedControlType);
            }
            else
            {
                return base.GetLocalizedControlTypeCore();
            }
        }

        override protected AutomationLiveSetting GetLiveSettingCore()
        {
            AutomationPeer wrapperPeer = OwningCellPeer;
            AutomationLiveSetting liveSetting = AutomationLiveSetting.Off;

            if (wrapperPeer != null)
            {
                liveSetting = wrapperPeer.GetLiveSetting();
            }
            else
            {
                ThrowElementNotAvailableException();
            }

            return liveSetting;
        }

        ///
        protected override string GetNameCore()
        {
            AutomationPeer wrapperPeer = OwningCellPeer;
            string name = null;

            if (wrapperPeer != null)
                name = wrapperPeer.GetName();

            if (string.IsNullOrEmpty(name))
            {
                name = SR.Get(SRID.DataGridCellItemAutomationPeer_NameCoreFormat, Item, _column.DisplayIndex);
            }

            return name;
        }

        ///
        protected override AutomationOrientation GetOrientationCore()
        {
            AutomationPeer wrapperPeer = OwningCellPeer;
            if (wrapperPeer != null)
                return wrapperPeer.GetOrientation();
            else
                ThrowElementNotAvailableException();

            return AutomationOrientation.None;
        }


        ///
        public override object GetPattern(PatternInterface patternInterface)
        {
            switch (patternInterface)
            {
                case PatternInterface.Invoke:
                    if (!this.OwningDataGrid.IsReadOnly && !_column.IsReadOnly)
                    {
                        return this;
                    }
                    break;
                case PatternInterface.Value:
                    // Value Pattern is not supported for NewItemPlaceholder row.
                    if (!IsNewItemPlaceholder)
                    {
                        return this;
                    }

                    break;
                case PatternInterface.SelectionItem:
                    if (IsCellSelectionUnit)
                    {
                        return this;
                    }

                    break;
                case PatternInterface.ScrollItem:
                case PatternInterface.GridItem:
                case PatternInterface.TableItem:
                    return this;
                case PatternInterface.VirtualizedItem:
                    if (VirtualizedItemPatternIdentifiers.Pattern != null)
                    {
                        if (OwningCellPeer == null)
                            return this;
                        else
                        {
                            // If the Item is in Automation Tree we consider it Realized and need not return VirtualizedItem pattern.
                            if (OwningItemPeer != null && !IsItemInAutomationTree())
                            {
                                return this;
                            }

                            // DataGridItemPeer could be virtualized
                            if (OwningItemPeer == null)
                                return this;
                        }
                    }
                    break;
            }


            return null;
        }

        /// <summary>
        /// Gets the position of this DataGridCellItem within a set.
        /// </summary>
        /// <remarks>
        /// Forwards the call to the wrapperPeer.
        /// </remarks>
        /// <returns>
        /// The PositionInSet property value from the wrapper peer
        /// </returns>
        protected override int GetPositionInSetCore()
        {
            AutomationPeer wrapperPeer = OwningCellPeer;
            int position = AutomationProperties.AutomationPositionInSetDefault;

            if (wrapperPeer != null)
            {
                position = wrapperPeer.GetPositionInSet();
            }
            else
            {
                ThrowElementNotAvailableException();
            }

            return position;
        }

        /// <summary>
        /// Gets the size of a set that contains this DataGridCellItem.
        /// </summary>
        /// <remarks>
        /// Forwards the call to the wrapperPeer.
        /// </remarks>
        /// <returns>
        /// The SizeOfSet property value from the wrapper peer
        /// </returns>
        protected override int GetSizeOfSetCore()
        {
            AutomationPeer wrapperPeer = OwningCellPeer;
            int size = AutomationProperties.AutomationSizeOfSetDefault;

            if (wrapperPeer != null)
            {
                size = wrapperPeer.GetSizeOfSet();
            }
            else
            {
                ThrowElementNotAvailableException();
            }

            return size;
        }

        override internal Rect GetVisibleBoundingRectCore()
        {
            AutomationPeer wrapperPeer = OwningCellPeer;
            if (wrapperPeer != null)
            {
                return wrapperPeer.GetVisibleBoundingRectCore();
            }
            return GetBoundingRectangle();
        }

        ///
        protected override bool HasKeyboardFocusCore()
        {
            AutomationPeer wrapperPeer = OwningCellPeer;
            if (wrapperPeer != null)
                return wrapperPeer.HasKeyboardFocus();
            else
                ThrowElementNotAvailableException();

            return false;
        }

        ///
        protected override bool IsContentElementCore()
        {
            AutomationPeer wrapperPeer = OwningCellPeer;
            if (wrapperPeer != null)
                return wrapperPeer.IsContentElement();

            return true;
        }

        ///
        protected override bool IsControlElementCore()
        {
            AutomationPeer wrapperPeer = OwningCellPeer;
            if (wrapperPeer != null)
                return wrapperPeer.IsControlElement();

            return true;
        }

        ///
        protected override bool IsEnabledCore()
        {
            AutomationPeer wrapperPeer = OwningCellPeer;
            if (wrapperPeer != null)
                return wrapperPeer.IsEnabled();
            else
                ThrowElementNotAvailableException();

            return true;
        }

        ///
        protected override bool IsKeyboardFocusableCore()
        {
            AutomationPeer wrapperPeer = OwningCellPeer;
            if (wrapperPeer != null)
                return wrapperPeer.IsKeyboardFocusable();
            else
                ThrowElementNotAvailableException();

            return false;
        }

        ///
        protected override bool IsOffscreenCore()
        {
            AutomationPeer wrapperPeer = OwningCellPeer;
            if (wrapperPeer != null)
                return wrapperPeer.IsOffscreen();
            else
                ThrowElementNotAvailableException();

            return true;
        }


        ///
        protected override bool IsPasswordCore()
        {
            AutomationPeer wrapperPeer = OwningCellPeer;
            if (wrapperPeer != null)
                return wrapperPeer.IsPassword();
            else
                ThrowElementNotAvailableException();

            return false;
        }

        ///
        protected override bool IsRequiredForFormCore()
        {
            AutomationPeer wrapperPeer = OwningCellPeer;
            if (wrapperPeer != null)
                return wrapperPeer.IsRequiredForForm();
            else
                ThrowElementNotAvailableException();

            return false;
        }

        ///
        protected override void SetFocusCore()
        {
            AutomationPeer wrapperPeer = OwningCellPeer;
            if (wrapperPeer != null)
                wrapperPeer.SetFocus();
            else
                ThrowElementNotAvailableException();
        }

        override internal bool IsDataItemAutomationPeer()
        {
            return true;
        }

        override internal void AddToParentProxyWeakRefCache()
        {
            DataGridItemAutomationPeer owningItemPeer = this.OwningItemPeer;
            if (owningItemPeer != null)
            {
                owningItemPeer.AddProxyToWeakRefStorage(this.ElementProxyWeakReference, this);
            }
        }


        #endregion

        #region IGridItemProvider

        int IGridItemProvider.Column
        {
            get
            {
                return this.OwningDataGrid.Columns.IndexOf(this._column);
            }
        }

        int IGridItemProvider.ColumnSpan
        {
            get
            {
                return 1;
            }
        }

        IRawElementProviderSimple IGridItemProvider.ContainingGrid
        {
            get
            {
                return this.ContainingGrid;
            }
        }

        int IGridItemProvider.Row
        {
            get
            {
                return this.OwningDataGrid.Items.IndexOf(this.Item);
            }
        }

        int IGridItemProvider.RowSpan
        {
            get
            {
                return 1;
            }
        }

        #endregion

        #region ITableItemProvider

        IRawElementProviderSimple[] ITableItemProvider.GetColumnHeaderItems()
        {
            if (this.OwningDataGrid != null &&
                (this.OwningDataGrid.HeadersVisibility & DataGridHeadersVisibility.Column) == DataGridHeadersVisibility.Column &&
                this.OwningDataGrid.ColumnHeadersPresenter != null)
            {
                DataGridColumnHeadersPresenterAutomationPeer columnHeadersPresenterPeer = UIElementAutomationPeer.CreatePeerForElement(this.OwningDataGrid.ColumnHeadersPresenter) as DataGridColumnHeadersPresenterAutomationPeer;
                if (columnHeadersPresenterPeer != null)
                {
                    AutomationPeer dataGridColumnHeaderPeer = columnHeadersPresenterPeer.FindOrCreateItemAutomationPeer(_column);
                    if (dataGridColumnHeaderPeer != null)
                    {
                        List<IRawElementProviderSimple> providers = new List<IRawElementProviderSimple>(1);
                        providers.Add(ProviderFromPeer(dataGridColumnHeaderPeer));
                        return providers.ToArray();
                    }
                }
            }

            return null;
        }

        IRawElementProviderSimple[] ITableItemProvider.GetRowHeaderItems()
        {
            if (this.OwningDataGrid != null &&
                (this.OwningDataGrid.HeadersVisibility & DataGridHeadersVisibility.Row) == DataGridHeadersVisibility.Row)
            {
                DataGridAutomationPeer dataGridAutomationPeer = UIElementAutomationPeer.CreatePeerForElement(this.OwningDataGrid) as DataGridAutomationPeer;
                DataGridItemAutomationPeer dataGridItemAutomationPeer = dataGridAutomationPeer.FindOrCreateItemAutomationPeer(Item) as DataGridItemAutomationPeer;
                if (dataGridItemAutomationPeer != null)
                {
                    AutomationPeer rowHeaderAutomationPeer = dataGridItemAutomationPeer.RowHeaderAutomationPeer;
                    if (rowHeaderAutomationPeer != null)
                    {
                        List<IRawElementProviderSimple> providers = new List<IRawElementProviderSimple>(1);
                        providers.Add(ProviderFromPeer(rowHeaderAutomationPeer));
                        return providers.ToArray();
                    }
                }
            }

            return null;
        }

        #endregion

        #region IInvokeProvider

        void IInvokeProvider.Invoke()
        {
            if (this.OwningDataGrid.IsReadOnly || _column.IsReadOnly)
            {
                return;
            }

            EnsureEnabled();

            bool success = false;

            // If the current cell is virtualized - scroll into view
            if (this.OwningCell == null)
            {
                this.OwningDataGrid.ScrollIntoView(Item, _column);
            }

            // Put current cell into edit mode
            DataGridCell cell = this.OwningCell;
            if (cell != null)
            {
                if (!cell.IsEditing)
                {
                    // the automation core usually gives the cell focus before calling
                    // Invoke, but this may not happen if the cell is virtualized
                    if (!cell.IsKeyboardFocusWithin)
                    {
                        cell.Focus();
                    }

                    // other cells may need to be de-selected, let the DataGrid handle that.
                    this.OwningDataGrid.HandleSelectionForCellInput(cell, /* startDragging = */ false, /* allowsExtendSelect = */ false, /* allowsMinimalSelect = */ false);

                    // the cell is now the datagrid's "current" cell, so BeginEdit will put
                    // it into edit mode
                    success = this.OwningDataGrid.BeginEdit();
                }
                else
                {
                    success = true;
                }
            }

            // Invoke on a NewItemPlaceholder row creates a new item.
            // BeginEdit on a NewItemPlaceholder row returns false.
            if (!success  && !IsNewItemPlaceholder)
            {
                throw new InvalidOperationException(SR.Get(SRID.DataGrid_AutomationInvokeFailed));
            }
        }

        #endregion

        #region IScrollItemProvider

        void IScrollItemProvider.ScrollIntoView()
        {
            this.OwningDataGrid.ScrollIntoView(Item, _column);
        }

        #endregion

        #region ISelectionItemProvider

        bool ISelectionItemProvider.IsSelected
        {
            get
            {
                return this.OwningDataGrid.SelectedCellsInternal.Contains(new DataGridCellInfo(Item, _column));
            }
        }

        IRawElementProviderSimple ISelectionItemProvider.SelectionContainer
        {
            get
            {
                return this.ContainingGrid;
            }
        }

        void ISelectionItemProvider.AddToSelection()
        {
            if (!IsCellSelectionUnit)
            {
                throw new InvalidOperationException(SR.Get(SRID.DataGrid_CannotSelectCell));
            }

            // If item is already selected - do nothing
            DataGridCellInfo currentCellInfo = new DataGridCellInfo(Item, _column);
            if (this.OwningDataGrid.SelectedCellsInternal.Contains(currentCellInfo))
            {
                return;
            }

            EnsureEnabled();

            if (this.OwningDataGrid.SelectionMode == DataGridSelectionMode.Single &&
                this.OwningDataGrid.SelectedCells.Count > 0)
            {
                throw new InvalidOperationException();
            }

            this.OwningDataGrid.SelectedCellsInternal.Add(currentCellInfo);
        }

        void ISelectionItemProvider.RemoveFromSelection()
        {
            if (!IsCellSelectionUnit)
            {
                throw new InvalidOperationException(SR.Get(SRID.DataGrid_CannotSelectCell));
            }

            EnsureEnabled();

            DataGridCellInfo currentCellInfo = new DataGridCellInfo(Item, _column);
            if (this.OwningDataGrid.SelectedCellsInternal.Contains(currentCellInfo))
            {
                this.OwningDataGrid.SelectedCellsInternal.Remove(currentCellInfo);
            }
        }

        void ISelectionItemProvider.Select()
        {
            if (!IsCellSelectionUnit)
            {
                throw new InvalidOperationException(SR.Get(SRID.DataGrid_CannotSelectCell));
            }

            EnsureEnabled();

            DataGridCellInfo currentCellInfo = new DataGridCellInfo(Item, _column);
            this.OwningDataGrid.SelectOnlyThisCell(currentCellInfo);
        }

        #endregion

        #region IValueProvider

        bool IValueProvider.IsReadOnly
        {
            get
            {
                return _column.IsReadOnly;
            }
        }

        void IValueProvider.SetValue(string value)
        {
            if (_column.IsReadOnly)
            {
                throw new InvalidOperationException(SR.Get(SRID.DataGrid_ColumnIsReadOnly));
            }
            if (this.OwningDataGrid != null)
            {
                OwningDataGrid.SetCellAutomationValue(Item, _column, value);
            }
        }

        string IValueProvider.Value
        {
            get
            {
                if (this.OwningDataGrid != null)
                {
                    return OwningDataGrid.GetCellAutomationValue(Item, _column);
                }
                else
                {
                    return null;
                }
            }
        }

        #endregion

        #region IVirtualizedItemProvider
        void IVirtualizedItemProvider.Realize()
        {
            OwningDataGrid.ScrollIntoView(Item, _column);
        }

        #endregion

        #region Private Methods

        private void EnsureEnabled()
        {
            if (!OwningDataGrid.IsEnabled)
            {
                throw new ElementNotEnabledException();
            }
        }

        /// <summary>
        private void ThrowElementNotAvailableException()
        {
            // To avoid the situation on legacy systems which may not have new unmanaged core. this check with old unmanaged core
            // avoids throwing exception and provide older behavior returning default values for items which are virtualized rather than throwing exception.
            if (VirtualizedItemPatternIdentifiers.Pattern != null && !IsItemInAutomationTree())
                throw new ElementNotAvailableException(SR.Get(SRID.VirtualizedElement));
        }

        private bool IsItemInAutomationTree()
        {
            AutomationPeer parent = this.GetParent();
            if (this.Index != -1 && parent != null && parent.Children != null && this.Index < parent.Children.Count && parent.Children[this.Index] == this)
                return true;
            else return false;
        }

        #endregion

        #region Private Properties

        private bool IsCellSelectionUnit
        {
            get
            {
                return (this.OwningDataGrid != null && (this.OwningDataGrid.SelectionUnit == DataGridSelectionUnit.Cell ||
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

        private DataGrid OwningDataGrid
        {
            get
            {
                return _column.DataGridOwner;
            }
        }

        // This may be null if the cell is virtualized
        private DataGridCell OwningCell
        {
            get
            {
                DataGrid dataGrid = this.OwningDataGrid;
                return (dataGrid != null) ? dataGrid.TryFindCell(Item, _column) : null;
            }
        }

        internal DataGridCellAutomationPeer OwningCellPeer
        {
            get
            {
                DataGridCellAutomationPeer cellPeer = null;
                DataGridCell cell = this.OwningCell;
                if (cell != null)
                {
                    cellPeer = FrameworkElementAutomationPeer.CreatePeerForElement(cell) as DataGridCellAutomationPeer;
                    cellPeer.EventsSource = this;
                }

                return cellPeer;
            }
        }

        private IRawElementProviderSimple ContainingGrid
        {
            get
            {
                AutomationPeer peer = UIElementAutomationPeer.CreatePeerForElement(OwningDataGrid);
                if (peer != null)
                {
                    return ProviderFromPeer(peer);
                }

                return null;
            }
        }

        internal DataGridColumn Column
        {
            get
            {
                return _column;
            }
        }

        internal object Item
        {
            get {  return (_item == null) ? null : _item.Target; }
        }

        private DataGridItemAutomationPeer OwningItemPeer
        {
            get
            {
                if (OwningDataGrid != null)
                {
                    DataGridAutomationPeer dataGridPeer = FrameworkElementAutomationPeer.CreatePeerForElement(OwningDataGrid) as DataGridAutomationPeer;
                    if (dataGridPeer != null)
                    {
                        return dataGridPeer.GetExistingPeerByItem(Item, /*checkInWeakRefStorage*/ true) as DataGridItemAutomationPeer;
                    }
                }
                return null;
            }
        }

        #endregion

        ///
        internal override bool AncestorsInvalid
        {
            get { return base.AncestorsInvalid; }
            set
            {
                base.AncestorsInvalid = value;
                if (value)
                    return;
                AutomationPeer wrapperPeer = OwningCellPeer;
                if (wrapperPeer != null)
                {
                    wrapperPeer.AncestorsInvalid = false;
                }
            }
        }

        #region Data

        private WeakReference _item;
        private DataGridColumn _column;

        #endregion
    }
}
