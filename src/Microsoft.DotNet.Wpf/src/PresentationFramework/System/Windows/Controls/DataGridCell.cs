// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using System.Windows.Automation;

namespace System.Windows.Controls
{
    /// <summary>
    ///     A control for displaying a cell of the DataGrid.
    /// </summary>
    public class DataGridCell : ContentControl, IProvideDataGridColumn
    {
        #region Constructors

        /// <summary>
        ///     Instantiates global information.
        /// </summary>
        static DataGridCell()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(DataGridCell), new FrameworkPropertyMetadata(typeof(DataGridCell)));
            StyleProperty.OverrideMetadata(typeof(DataGridCell), new FrameworkPropertyMetadata(null, OnNotifyPropertyChanged, OnCoerceStyle));
            ClipProperty.OverrideMetadata(typeof(DataGridCell), new FrameworkPropertyMetadata(null, new CoerceValueCallback(OnCoerceClip)));
            KeyboardNavigation.TabNavigationProperty.OverrideMetadata(typeof(DataGridCell), new FrameworkPropertyMetadata(KeyboardNavigationMode.Local));
            AutomationProperties.IsOffscreenBehaviorProperty.OverrideMetadata(typeof(DataGridCell), new FrameworkPropertyMetadata(IsOffscreenBehavior.FromClip));

            // Set SnapsToDevicePixels to true so that this element can draw grid lines.  The metadata options are so that the property value doesn't inherit down the tree from here.
            SnapsToDevicePixelsProperty.OverrideMetadata(typeof(DataGridCell), new FrameworkPropertyMetadata(true, FrameworkPropertyMetadataOptions.AffectsArrange));

            EventManager.RegisterClassHandler(typeof(DataGridCell), MouseLeftButtonDownEvent, new MouseButtonEventHandler(OnAnyMouseLeftButtonDownThunk), true);

            IsMouseOverPropertyKey.OverrideMetadata(typeof(DataGridCell), new UIPropertyMetadata(new PropertyChangedCallback(OnVisualStatePropertyChanged)));

            EventManager.RegisterClassHandler(typeof(DataGridCell), LostFocusEvent, new RoutedEventHandler(OnAnyLostFocus), true);
            EventManager.RegisterClassHandler(typeof(DataGridCell), GotFocusEvent, new RoutedEventHandler(OnAnyGotFocus), true);
        }

        /// <summary>
        ///     Instantiates a new instance of this class.
        /// </summary>
        public DataGridCell()
        {
            _tracker = new ContainerTracking<DataGridCell>(this);
        }

        #endregion

        #region Automation

        protected override System.Windows.Automation.Peers.AutomationPeer OnCreateAutomationPeer()
        {
            return new System.Windows.Automation.Peers.DataGridCellAutomationPeer(this);
        }

        #endregion

        #region Cell Generation

        /// <summary>
        ///     Prepares a cell for use.
        /// </summary>
        /// <remarks>
        ///     Updates the column reference.
        ///     This overload computes the column index from the ItemContainerGenerator.
        /// </remarks>
        internal void PrepareCell(object item, ItemsControl cellsPresenter, DataGridRow ownerRow)
        {
            PrepareCell(item, ownerRow, cellsPresenter.ItemContainerGenerator.IndexFromContainer(this));
        }

        /// <summary>
        ///     Prepares a cell for use.
        /// </summary>
        /// <remarks>
        ///     Updates the column reference.
        /// </remarks>
        internal void PrepareCell(object item, DataGridRow ownerRow, int index)
        {
            Debug.Assert(_owner == null || _owner == ownerRow, "_owner should be null before PrepareCell is called or the same value as the ownerRow.");

            _owner = ownerRow;

            DataGrid dataGrid = _owner.DataGridOwner;
            if (dataGrid != null)
            {
                // The index of the container should correspond to the index of the column
                if ((index >= 0) && (index < dataGrid.Columns.Count))
                {
                    // Retrieve the column definition and pass it to the cell container
                    DataGridColumn column = dataGrid.Columns[index];
                    Column = column;
                    TabIndex = column.DisplayIndex;
                }

                if (IsEditing)
                {
                    // If IsEditing was left on and this container was recycled, reset it here.
                    // Setting this property will result in BuildVisualTree being called.
                    IsEditing = false;
                }
                else if ((Content as FrameworkElement) == null)
                {
                    // If there isn't already a visual tree, then create one.
                    BuildVisualTree();

                    if (!NeedsVisualTree)
                    {
                        Content = item;
                    }
                }

                // Update cell Selection
                bool isSelected = dataGrid.SelectedCellsInternal.Contains(this);
                SyncIsSelected(isSelected);
            }

            DataGridHelper.TransferProperty(this, StyleProperty);
            DataGridHelper.TransferProperty(this, IsReadOnlyProperty);
            CoerceValue(ClipProperty);
        }

        /// <summary>
        ///     Clears the cell of references.
        /// </summary>
        internal void ClearCell(DataGridRow ownerRow)
        {
            Debug.Assert(_owner == ownerRow, "_owner should be the same as the DataGridRow that is clearing the cell.");
            _owner = null;
        }

        /// <summary>
        ///     Used by the DataGridRowGenerator owner to send notifications to the cell container.
        /// </summary>
        internal ContainerTracking<DataGridCell> Tracker
        {
            get { return _tracker; }
        }

        #endregion

        #region Column Information

        /// <summary>
        ///     The column that defines how this cell should appear.
        /// </summary>
        public DataGridColumn Column
        {
            get { return (DataGridColumn)GetValue(ColumnProperty); }
            internal set { SetValue(ColumnPropertyKey, value); }
        }

        /// <summary>
        ///     The DependencyPropertyKey that allows writing the Column property value.
        /// </summary>
        private static readonly DependencyPropertyKey ColumnPropertyKey =
            DependencyProperty.RegisterReadOnly("Column", typeof(DataGridColumn), typeof(DataGridCell), new FrameworkPropertyMetadata(null, new PropertyChangedCallback(OnColumnChanged)));

        /// <summary>
        ///     The DependencyProperty for the Columns property.
        /// </summary>
        public static readonly DependencyProperty ColumnProperty = ColumnPropertyKey.DependencyProperty;

        /// <summary>
        ///     Called when the Column property changes.
        ///     Calls the protected virtual OnColumnChanged.
        /// </summary>
        private static void OnColumnChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            DataGridCell cell = sender as DataGridCell;
            if (cell != null)
            {
                cell.OnColumnChanged((DataGridColumn)e.OldValue, (DataGridColumn)e.NewValue);
            }
        }

        /// <summary>
        ///     Called due to the cell's column definition changing.
        ///     Not called due to changes within the current column definition.
        /// </summary>
        /// <remarks>
        ///     Coerces ContentTemplate and ContentTemplateSelector.
        /// </remarks>
        /// <param name="oldColumn">The old column definition.</param>
        /// <param name="newColumn">The new column definition.</param>
        protected virtual void OnColumnChanged(DataGridColumn oldColumn, DataGridColumn newColumn)
        {
            // We need to call BuildVisualTree after changing the column (PrepareCell does this).
            Content = null;
            DataGridHelper.TransferProperty(this, StyleProperty);
            DataGridHelper.TransferProperty(this, IsReadOnlyProperty);
        }

        #endregion

        #region Notification Propagation

        /// <summary>
        ///     Notifies the Cell of a property change.
        /// </summary>
        private static void OnNotifyPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((DataGridCell)d).NotifyPropertyChanged(d, string.Empty, e, DataGridNotificationTarget.Cells);
        }

        /// <summary>
        ///     Cancels editing the current cell & notifies the cell of a change to IsReadOnly.
        /// </summary>
        private static void OnNotifyIsReadOnlyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var cell = (DataGridCell)d;
            var dataGrid = cell.DataGridOwner;
            if ((bool)e.NewValue && dataGrid != null)
            {
                dataGrid.CancelEdit(cell);
            }

            // re-evalutate the BeginEdit command's CanExecute.
            CommandManager.InvalidateRequerySuggested();

            cell.NotifyPropertyChanged(d, string.Empty, e, DataGridNotificationTarget.Cells);
        }

        /// <summary>
        ///     General notification for DependencyProperty changes from the grid or from columns.
        /// </summary>
        internal void NotifyPropertyChanged(DependencyObject d, string propertyName, DependencyPropertyChangedEventArgs e, DataGridNotificationTarget target)
        {
            DataGridColumn column = d as DataGridColumn;
            if ((column != null) && (column != Column))
            {
                // This notification does not apply to this cell
                return;
            }

            // All the notifications which are to be handled by the cell
            if (DataGridHelper.ShouldNotifyCells(target))
            {
                if (e.Property == DataGridColumn.WidthProperty)
                {
                    DataGridHelper.OnColumnWidthChanged(this, e);
                }
                else if (e.Property == DataGrid.CellStyleProperty || e.Property == DataGridColumn.CellStyleProperty || e.Property == StyleProperty)
                {
                    DataGridHelper.TransferProperty(this, StyleProperty);
                }
                else if (e.Property == DataGrid.IsReadOnlyProperty || e.Property == DataGridColumn.IsReadOnlyProperty || e.Property == IsReadOnlyProperty)
                {
                    DataGridHelper.TransferProperty(this, IsReadOnlyProperty);
                }
                else if (e.Property == DataGridColumn.DisplayIndexProperty)
                {
                    TabIndex = column.DisplayIndex;
                }
                else if (e.Property == DataGrid.IsKeyboardFocusWithinProperty)
                {
                    UpdateVisualState();
                }
            }

            // All the notifications which needs forward to columns
            if (DataGridHelper.ShouldRefreshCellContent(target))
            {
                if (column != null && NeedsVisualTree)
                {
                    if (!string.IsNullOrEmpty(propertyName))
                    {
                        column.RefreshCellContent(this, propertyName);
                    }
                    else if (e.Property != null)
                    {
                        column.RefreshCellContent(this, e.Property.Name);
                    }
                }
            }
        }

        #endregion

        #region Style

        private static object OnCoerceStyle(DependencyObject d, object baseValue)
        {
            var cell = d as DataGridCell;
            return DataGridHelper.GetCoercedTransferPropertyValue(
                cell,
                baseValue,
                StyleProperty,
                cell.Column,
                DataGridColumn.CellStyleProperty,
                cell.DataGridOwner,
                DataGrid.CellStyleProperty);
        }

        #endregion

        #region Template

        internal override void ChangeVisualState(bool useTransitions)
        {
            if (DataGridOwner == null)
            {
                return;
            }

            // CommonStates
            if (IsMouseOver)
            {
                VisualStates.GoToState(this, useTransitions, VisualStates.StateMouseOver, VisualStates.StateNormal);
            }
            else
            {
                VisualStateManager.GoToState(this, VisualStates.StateNormal, useTransitions);
            }

            // SelectionStates
            if (IsSelected)
            {
                VisualStates.GoToState(this, useTransitions, VisualStates.StateSelected, VisualStates.StateUnselected);
            }
            else
            {
                VisualStates.GoToState(this, useTransitions, VisualStates.StateUnselected);
            }

            // FocusStates
            if (DataGridOwner.IsKeyboardFocusWithin)
            {
                VisualStates.GoToState(this, useTransitions, VisualStates.StateFocused, VisualStates.StateUnfocused);
            }
            else
            {
                VisualStateManager.GoToState(this, VisualStates.StateUnfocused, useTransitions);
            }

            // CurrentStates
            if (IsCurrent)
            {
                VisualStates.GoToState(this, useTransitions, VisualStates.StateCurrent, VisualStates.StateRegular);
            }
            else
            {
                VisualStateManager.GoToState(this, VisualStates.StateRegular, useTransitions);
            }

            // Interaction states
            if (IsEditing)
            {
                VisualStates.GoToState(this, useTransitions, VisualStates.StateEditing, VisualStates.StateDisplay);
            }
            else
            {
                VisualStateManager.GoToState(this, VisualStates.StateDisplay, useTransitions);
            }

            base.ChangeVisualState(useTransitions);
        }

        /// <summary>
        ///     Builds a column's visual tree if not using templates.
        /// </summary>
        internal void BuildVisualTree()
        {
            if (NeedsVisualTree)
            {
                var column = Column;
                if (column != null)
                {
                    // Work around a problem with BindingGroup not removing BindingExpressions.
                    var row = RowOwner;
                    if (row != null)
                    {
                        var bindingGroup = row.BindingGroup;
                        if (bindingGroup != null)
                        {
                            RemoveBindingExpressions(bindingGroup, Content as DependencyObject);
                        }
                    }

                    // Ask the column to build a visual tree
                    FrameworkElement newContent = column.BuildVisualTree(IsEditing, RowDataItem, this);

                    // Before discarding the old visual tree, disconnect all its
                    // bindings, as in ItemContainerGenerator.UnlinkContainerFromItem.
                    // This prevents aliasing that can arise in recycling mode
                    FrameworkElement oldContent = Content as FrameworkElement;
                    if (oldContent != null && oldContent != newContent)
                    {
                        ContentPresenter cp = oldContent as ContentPresenter;
                        if (cp == null)
                        {
                            oldContent.SetValue(FrameworkElement.DataContextProperty, BindingExpressionBase.DisconnectedItem);
                        }
                        else
                        {
                            // for a template column, disconnect by setting the
                            // Content, to override the binding set up in
                            // DataGridTemplateColumn.LoadTemplateContent.
                            cp.Content = BindingExpressionBase.DisconnectedItem;
                        }
                    }

                    // hook the visual tree up through the Content property.
                    Content = newContent;
                }
            }
        }

        private void RemoveBindingExpressions(BindingGroup bindingGroup, DependencyObject element)
        {
            if (element == null)
                return; // no content, hence no bindings to remove

            var bindingExpressions = bindingGroup.BindingExpressions;
            BindingExpressionBase[] bindingExpressionsCopy = new BindingExpressionBase[bindingExpressions.Count];
            bindingExpressions.CopyTo(bindingExpressionsCopy, 0);

            for (int i = 0; i < bindingExpressionsCopy.Length; i++)
            {
                // Check the binding's target element - it might have been GC'd
                // (this can happen when column-virtualization is enabled).
                // If so, fetching TargetElement will detach the binding and remove it
                // from the binding group's collection.  This side-effect is why we
                // loop through a copy of the original collection, and don't rely
                // on i to be a valid index into the original collection.
                if (DataGridHelper.BindingExpressionBelongsToElement<DataGridCell>(bindingExpressionsCopy[i], this))
                {
                    bindingExpressions.Remove(bindingExpressionsCopy[i]);
                }
            }
        }

        #endregion

        #region Editing

        /// <summary>
        ///     Whether the cell is in editing mode.
        /// </summary>
        public bool IsEditing
        {
            get { return (bool)GetValue(IsEditingProperty); }
            set { SetValue(IsEditingProperty, value); }
        }

        /// <summary>
        ///     Represents the IsEditing property.
        /// </summary>
        public static readonly DependencyProperty IsEditingProperty = DependencyProperty.Register("IsEditing", typeof(bool), typeof(DataGridCell), new FrameworkPropertyMetadata(false, new PropertyChangedCallback(OnIsEditingChanged)));

        private static void OnIsEditingChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            ((DataGridCell)sender).OnIsEditingChanged((bool)e.NewValue);
        }

        /// <summary>
        ///     Called when the value of IsEditing changes.
        /// </summary>
        /// <remarks>
        ///     Coerces the value of ContentTemplate.
        /// </remarks>
        /// <param name="isEditing">The new value of IsEditing.</param>
        protected virtual void OnIsEditingChanged(bool isEditing)
        {
            if (IsKeyboardFocusWithin && !IsKeyboardFocused)
            {
                // Keep focus on the cell when flipping modes
                Focus();
            }

            // If templates aren't being used, then a new visual tree needs to be built.
            BuildVisualTree();
            UpdateVisualState();
        }

        internal void NotifyCurrentCellContainerChanged()
        {
            UpdateVisualState();
        }

        /// <summary>
        ///     Whether the cell is the current cell.
        /// </summary>
        private bool IsCurrent
        {
            get
            {
                var row = RowOwner;
                var column = Column;
                if (row != null && column != null)
                {
                    var dataGrid = row.DataGridOwner;
                    if (dataGrid != null)
                    {
                        return dataGrid.IsCurrent(row, column);
                    }
                }

                return false;
            }
        }

        /// <summary>
        ///     Whether the cell can be placed in edit mode.
        /// </summary>
        public bool IsReadOnly
        {
            get { return (bool)GetValue(IsReadOnlyProperty); }
        }

        private static readonly DependencyPropertyKey IsReadOnlyPropertyKey =
            DependencyProperty.RegisterReadOnly("IsReadOnly", typeof(bool), typeof(DataGridCell), new FrameworkPropertyMetadata(false, OnNotifyIsReadOnlyChanged, OnCoerceIsReadOnly));

        /// <summary>
        ///     The DependencyProperty for IsReadOnly.
        /// </summary>
        public static readonly DependencyProperty IsReadOnlyProperty = IsReadOnlyPropertyKey.DependencyProperty;

        private static object OnCoerceIsReadOnly(DependencyObject d, object baseValue)
        {
            var cell = d as DataGridCell;
            var column = cell.Column;
            var dataGrid = cell.DataGridOwner;

            // We dont use the cell & 'baseValue' here because this property is read only on cell.
            // the column may coerce a default value to 'true', so we'll use it's effective value for IsReadOnly
            // as the baseValue.
            return DataGridHelper.GetCoercedTransferPropertyValue(
                column,
                column.IsReadOnly,
                DataGridColumn.IsReadOnlyProperty,
                dataGrid,
                DataGrid.IsReadOnlyProperty);
        }

        private static void OnAnyLostFocus(object sender, RoutedEventArgs e)
        {
            // Get the ancestor cell of old focused element.
            // Set DataGrid.FocusedCell to null, if the cell doesn't
            // have keyboard focus.
            DataGridCell cell = DataGridHelper.FindVisualParent<DataGridCell>(e.OriginalSource as UIElement);
            if (cell != null && cell == sender)
            {
                DataGrid owner = cell.DataGridOwner;
                if (owner != null && !cell.IsKeyboardFocusWithin && owner.FocusedCell == cell)
                {
                    owner.FocusedCell = null;
                }
            }
        }

        private static void OnAnyGotFocus(object sender, RoutedEventArgs e)
        {
            DataGridCell cell = DataGridHelper.FindVisualParent<DataGridCell>(e.OriginalSource as UIElement);
            if (cell != null && cell == sender)
            {
                DataGrid owner = cell.DataGridOwner;
                if (owner != null)
                {
                    owner.FocusedCell = cell;
                }
            }
        }

        internal void BeginEdit(RoutedEventArgs e)
        {
            Debug.Assert(!IsEditing, "Should not call BeginEdit when IsEditing is true.");

            IsEditing = true;

            DataGridColumn column = Column;
            if (column != null)
            {
                // Ask the column to store the original value
                column.BeginEdit(Content as FrameworkElement, e);
            }

            RaisePreparingCellForEdit(e);
        }

        internal void CancelEdit()
        {
            Debug.Assert(IsEditing, "Should not call CancelEdit when IsEditing is false.");

            DataGridColumn column = Column;
            if (column != null)
            {
                // Ask the column to restore the original value
                column.CancelEdit(Content as FrameworkElement);
            }

            IsEditing = false;
        }

        internal bool CommitEdit()
        {
            Debug.Assert(IsEditing, "Should not call CommitEdit when IsEditing is false.");

            bool validationPassed = true;
            DataGridColumn column = Column;
            if (column != null)
            {
                // Ask the column to access the binding and update the data source
                // If validation fails, then remain in editing mode
                validationPassed = column.CommitEdit(Content as FrameworkElement);
            }

            if (validationPassed)
            {
                IsEditing = false;
            }

            return validationPassed;
        }

        private void RaisePreparingCellForEdit(RoutedEventArgs editingEventArgs)
        {
            DataGrid dataGridOwner = DataGridOwner;
            if (dataGridOwner != null)
            {
                FrameworkElement currentEditingElement = EditingElement;
                DataGridPreparingCellForEditEventArgs preparingCellForEditEventArgs = new DataGridPreparingCellForEditEventArgs(Column, RowOwner, editingEventArgs, currentEditingElement);
                dataGridOwner.OnPreparingCellForEdit(preparingCellForEditEventArgs);
            }
        }

        internal FrameworkElement EditingElement
        {
            get
            {
                // The editing element was stored in the Content property.
                return Content as FrameworkElement;
            }
        }

        #endregion

        #region Selection

        /// <summary>
        ///     Whether the cell is selected or not.
        /// </summary>
        public bool IsSelected
        {
            get { return (bool)GetValue(IsSelectedProperty); }
            set { SetValue(IsSelectedProperty, value); }
        }

        /// <summary>
        ///     Represents the IsSelected property.
        /// </summary>
        public static readonly DependencyProperty IsSelectedProperty = DependencyProperty.Register("IsSelected", typeof(bool), typeof(DataGridCell), new FrameworkPropertyMetadata(false, new PropertyChangedCallback(OnIsSelectedChanged)));

        private static void OnIsSelectedChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            DataGridCell cell = (DataGridCell)sender;
            bool isSelected = (bool)e.NewValue;

            // There is no reason to notify the DataGrid if IsSelected's value came
            // from the DataGrid.
            if (!cell._syncingIsSelected)
            {
                DataGrid dataGrid = cell.DataGridOwner;
                if (dataGrid != null)
                {
                    // Notify the DataGrid that a cell's IsSelected property changed
                    // in case it was done programmatically instead of by the
                    // DataGrid itself.
                    dataGrid.CellIsSelectedChanged(cell, isSelected);
                }
            }

            cell.RaiseSelectionChangedEvent(isSelected);
            cell.UpdateVisualState();
        }

        /// <summary>
        ///     Used to synchronize IsSelected with the DataGrid.
        ///     Prevents unncessary notification back to the DataGrid.
        /// </summary>
        internal void SyncIsSelected(bool isSelected)
        {
            bool originalValue = _syncingIsSelected;
            _syncingIsSelected = true;
            try
            {
                IsSelected = isSelected;
            }
            finally
            {
                _syncingIsSelected = originalValue;
            }
        }

        private void RaiseSelectionChangedEvent(bool isSelected)
        {
            if (isSelected)
            {
                OnSelected(new RoutedEventArgs(SelectedEvent, this));
            }
            else
            {
                OnUnselected(new RoutedEventArgs(UnselectedEvent, this));
            }
        }

        /// <summary>
        ///     Raised when the item's IsSelected property becomes true.
        /// </summary>
        public static readonly RoutedEvent SelectedEvent = EventManager.RegisterRoutedEvent("Selected", RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(DataGridCell));

        /// <summary>
        ///     Raised when the item's IsSelected property becomes true.
        /// </summary>
        public event RoutedEventHandler Selected
        {
            add
            {
                AddHandler(SelectedEvent, value);
            }

            remove
            {
                RemoveHandler(SelectedEvent, value);
            }
        }

        /// <summary>
        ///     Called when IsSelected becomes true. Raises the Selected event.
        /// </summary>
        /// <param name="e">Empty event arguments.</param>
        protected virtual void OnSelected(RoutedEventArgs e)
        {
            RaiseEvent(e);
        }

        /// <summary>
        ///     Raised when the item's IsSelected property becomes false.
        /// </summary>
        public static readonly RoutedEvent UnselectedEvent = EventManager.RegisterRoutedEvent("Unselected", RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(DataGridCell));

        /// <summary>
        ///     Raised when the item's IsSelected property becomes false.
        /// </summary>
        public event RoutedEventHandler Unselected
        {
            add
            {
                AddHandler(UnselectedEvent, value);
            }

            remove
            {
                RemoveHandler(UnselectedEvent, value);
            }
        }

        /// <summary>
        ///     Called when IsSelected becomes false. Raises the Unselected event.
        /// </summary>
        /// <param name="e">Empty event arguments.</param>
        protected virtual void OnUnselected(RoutedEventArgs e)
        {
            RaiseEvent(e);
        }

        #endregion

        #region GridLines

        // Different parts of the DataGrid draw different pieces of the GridLines.
        // Cells draw a single line on their right side.

        /// <summary>
        ///     Measure.  This is overridden so that the cell can extend its size to account for a grid line on the right.
        /// </summary>
        protected override Size MeasureOverride(Size constraint)
        {
            // Make space for the GridLine on the right and bottom:
            // Remove space from the constraint (since it implicitly includes the GridLine's thickness),
            // call the base implementation, and add the thickness back for the returned size.
            DataGrid dataGridOwner = DataGridOwner;
            bool horizontalLinesVisible = DataGridHelper.IsGridLineVisible(dataGridOwner, /*isHorizontal = */ true);
            bool verticalLinesVisible = DataGridHelper.IsGridLineVisible(dataGridOwner, /*isHorizontal = */ false);
            double horizontalLineThickness = 0;
            double verticalLineThickness = 0;

            if (horizontalLinesVisible)
            {
                horizontalLineThickness = dataGridOwner.HorizontalGridLineThickness;
                constraint = DataGridHelper.SubtractFromSize(constraint, horizontalLineThickness, /*height = */ true);
            }
            if (verticalLinesVisible)
            {
                verticalLineThickness = dataGridOwner.VerticalGridLineThickness;
                constraint = DataGridHelper.SubtractFromSize(constraint, verticalLineThickness, /*height = */ false);
            }
            Size desiredSize = base.MeasureOverride(constraint);
            if (horizontalLinesVisible)
            {
                desiredSize.Height += horizontalLineThickness;
            }
            if (verticalLinesVisible)
            {
                desiredSize.Width += verticalLineThickness;
            }
            return desiredSize;
        }

        /// <summary>
        ///     Arrange.  This is overriden so that the cell can position its content to account for a grid line on the right.
        /// </summary>
        /// <param name="arrangeSize">Arrange size</param>
        protected override Size ArrangeOverride(Size arrangeSize)
        {
            // We don't need to adjust the Arrange position of the content.  By default it is arranged at 0,0 and we're
            // adding a line to the right and bottom.  All we have to do is compress and extend the size, just like Measure.
            DataGrid dataGridOwner = DataGridOwner;
            bool horizontalLinesVisible = DataGridHelper.IsGridLineVisible(dataGridOwner, /*isHorizontal = */ true);
            bool verticalLinesVisible = DataGridHelper.IsGridLineVisible(dataGridOwner, /*isHorizontal = */ false);
            double horizontalLineThickness = 0;
            double verticalLineThickness = 0;
            if (horizontalLinesVisible)
            {
                horizontalLineThickness = dataGridOwner.HorizontalGridLineThickness;
                arrangeSize = DataGridHelper.SubtractFromSize(arrangeSize, horizontalLineThickness, /*height = */ true);
            }
            if (verticalLinesVisible)
            {
                verticalLineThickness = dataGridOwner.VerticalGridLineThickness;
                arrangeSize = DataGridHelper.SubtractFromSize(arrangeSize, verticalLineThickness, /*height = */ false);
            }
            Size returnSize = base.ArrangeOverride(arrangeSize);
            if (horizontalLinesVisible)
            {
                returnSize.Height += horizontalLineThickness;
            }
            if (verticalLinesVisible)
            {
                returnSize.Width += verticalLineThickness;
            }
            return returnSize;
        }

        /// <summary>
        ///     OnRender.  Overriden to draw a vertical line on the right.
        /// </summary>
        /// <param name="drawingContext"></param>
        protected override void OnRender(DrawingContext drawingContext)
        {
            base.OnRender(drawingContext);
            DataGrid dataGrid = DataGridOwner;

            if (DataGridHelper.IsGridLineVisible(dataGrid, /*isHorizontal = */ false))
            {
                double thickness = DataGridOwner.VerticalGridLineThickness;
                Rect rect = new Rect(new Size(thickness, RenderSize.Height));
                rect.X = RenderSize.Width - thickness;

                drawingContext.DrawRectangle(DataGridOwner.VerticalGridLinesBrush, null, rect);
            }

            if (DataGridHelper.IsGridLineVisible(dataGrid, /*isHorizontal = */ true))
            {
                double thickness = dataGrid.HorizontalGridLineThickness;
                Rect rect = new Rect(new Size(RenderSize.Width, thickness));
                rect.Y = RenderSize.Height - thickness;

                drawingContext.DrawRectangle(dataGrid.HorizontalGridLinesBrush, null, rect);
            }
        }

        #endregion

        #region Input

        private static void OnAnyMouseLeftButtonDownThunk(object sender, MouseButtonEventArgs e)
        {
            ((DataGridCell)sender).OnAnyMouseLeftButtonDown(e);
        }

        /// <summary>
        ///     The left mouse button was pressed
        /// </summary>
        /// Consider making a protected virtual.
        private void OnAnyMouseLeftButtonDown(MouseButtonEventArgs e)
        {
            bool focusWithin = IsKeyboardFocusWithin;
            bool isCtrlKeyPressed = (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control;
            if (focusWithin && !isCtrlKeyPressed && !e.Handled && IsSelected)
            {
                // The cell is focused and there are no other special selection gestures,
                // enter edit mode.
                DataGrid dataGridOwner = DataGridOwner;
                if (dataGridOwner != null)
                {
                    // The cell was clicked, which means that other cells may
                    // need to be de-selected, let the DataGrid handle that.
                    dataGridOwner.HandleSelectionForCellInput(this, /* startDragging = */ false, /* allowsExtendSelect = */ true, /* allowsMinimalSelect = */ false);

                    if (!IsEditing && !IsReadOnly)
                    {
                        // Enter edit mode
                        dataGridOwner.BeginEdit(e);
                        e.Handled = true;
                    }

                    // Please note that unselecting rows is not really considered a
                    // handlable operation.
                    //e.Handled = true;
                }
            }
            else if (!focusWithin || !IsSelected || isCtrlKeyPressed)
            {
                if (!focusWithin)
                {
                    // The cell should receive focus on click
                    Focus();
                }

                DataGrid dataGridOwner = DataGridOwner;
                if (dataGridOwner != null)
                {
                    // Let the DataGrid process selection
                    dataGridOwner.HandleSelectionForCellInput(this, /* startDragging = */ Mouse.Captured == null, /* allowsExtendSelect = */ true, /* allowsMinimalSelect = */ true);
                }

                e.Handled = true;
            }
#if PUBLIC_ONINPUT
            else
            {
                SendInputToColumn(e);
            }
#endif
        }

        /// <summary>
        ///     Reporting text composition.
        /// </summary>
        protected override void OnTextInput(TextCompositionEventArgs e)
        {
            SendInputToColumn(e);
        }

        protected override void OnPreviewKeyDown(KeyEventArgs e)
        {
            SendInputToColumn(e);
        }

        /// <summary>
        ///     Reporting a key was pressed.
        /// </summary>
        protected override void OnKeyDown(KeyEventArgs e)
        {
            SendInputToColumn(e);
        }

#if PUBLIC_ONINPUT
        // While DataGridColumn.OnInput is internal, these methods are not needed.
        // If that method becomes exposed, then it would make sense to include a better
        // range of input events.

        /// <summary>
        ///     Reporting a key was released
        /// </summary>
        protected override void OnKeyUp(KeyEventArgs e)
        {
            SendInputToColumn(e);
        }

        /// <summary>
        ///     Reporting the mouse button was released
        /// </summary>
        protected override void OnMouseUp(MouseButtonEventArgs e)
        {
            SendInputToColumn(e);
        }
#endif

        private void SendInputToColumn(InputEventArgs e)
        {
            var column = Column;
            if (column != null)
            {
                column.OnInput(e);
            }
        }

        #endregion

        #region Frozen Columns

        /// <summary>
        /// Coercion call back for clip property which ensures that the cell overlapping with frozen
        /// column gets clipped appropriately.
        /// </summary>
        /// <param name="d"></param>
        /// <param name="baseValue"></param>
        /// <returns></returns>
        private static object OnCoerceClip(DependencyObject d, object baseValue)
        {
            DataGridCell cell = (DataGridCell)d;
            Geometry geometry = baseValue as Geometry;
            Geometry frozenGeometry = DataGridHelper.GetFrozenClipForCell(cell);
            if (frozenGeometry != null)
            {
                if (geometry == null)
                {
                    return frozenGeometry;
                }

                geometry = new CombinedGeometry(GeometryCombineMode.Intersect, geometry, frozenGeometry);
            }

            return geometry;
        }

        #endregion

        #region Helpers

        internal DataGrid DataGridOwner
        {
            get
            {
                if (_owner != null)
                {
                    DataGrid dataGridOwner = _owner.DataGridOwner;
                    if (dataGridOwner == null)
                    {
                        dataGridOwner = ItemsControl.ItemsControlFromItemContainer(_owner) as DataGrid;
                    }

                    return dataGridOwner;
                }

                return null;
            }
        }

        private Panel ParentPanel
        {
            get
            {
                return VisualParent as Panel;
            }
        }

        internal DataGridRow RowOwner
        {
            get { return _owner; }
        }

        internal object RowDataItem
        {
            get
            {
                DataGridRow row = RowOwner;
                if (row != null)
                {
                    return row.Item;
                }

                return DataContext;
            }
        }

        private DataGridCellsPresenter CellsPresenter
        {
            get
            {
                return ItemsControl.ItemsControlFromItemContainer(this) as DataGridCellsPresenter;
            }
        }

        private bool NeedsVisualTree
        {
            get
            {
                return (ContentTemplate == null) && (ContentTemplateSelector == null);
            }
        }

        #endregion

        #region Data

        private DataGridRow _owner;
        private ContainerTracking<DataGridCell> _tracker;
        private bool _syncingIsSelected;                    // Used to prevent unnecessary notifications

        #endregion
    }
}
