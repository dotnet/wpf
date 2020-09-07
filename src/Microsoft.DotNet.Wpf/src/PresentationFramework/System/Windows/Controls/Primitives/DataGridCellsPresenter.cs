// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using MS.Internal;

namespace System.Windows.Controls.Primitives
{
    /// <summary>
    ///     A control that will be responsible for generating cells.
    ///     This control is meant to be specified within the template of a DataGridRow.
    ///     The APIs from ItemsControl do not match up nicely with the meaning of a
    ///     row, which is why this is being factored out.
    ///
    ///     The data item for the row is added n times to the Items collection,
    ///     where n is the number of columns in the DataGrid. This is implemented
    ///     using a special collection to avoid keeping multiple references to the
    ///     same object.
    /// </summary>
    public class DataGridCellsPresenter : ItemsControl
    {
        #region Constructors

        /// <summary>
        ///     Instantiates global information.
        /// </summary>
        static DataGridCellsPresenter()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(DataGridCellsPresenter), new FrameworkPropertyMetadata(typeof(DataGridCellsPresenter)));
            ItemsPanelProperty.OverrideMetadata(typeof(DataGridCellsPresenter), new FrameworkPropertyMetadata(new ItemsPanelTemplate(new FrameworkElementFactory(typeof(DataGridCellsPanel)))));
            FocusableProperty.OverrideMetadata(typeof(DataGridCellsPresenter), new FrameworkPropertyMetadata(false));

            HeightProperty.OverrideMetadata(typeof(DataGridCellsPresenter), new FrameworkPropertyMetadata(OnNotifyHeightPropertyChanged, OnCoerceHeight));
            MinHeightProperty.OverrideMetadata(typeof(DataGridCellsPresenter), new FrameworkPropertyMetadata(OnNotifyHeightPropertyChanged, OnCoerceMinHeight));

            VirtualizingPanel.IsVirtualizingProperty.OverrideMetadata(
                typeof(DataGridCellsPresenter),
                new FrameworkPropertyMetadata(false, new PropertyChangedCallback(OnIsVirtualizingPropertyChanged), new CoerceValueCallback(OnCoerceIsVirtualizingProperty)));
            VirtualizingPanel.VirtualizationModeProperty.OverrideMetadata(typeof(DataGridCellsPresenter), new FrameworkPropertyMetadata(VirtualizationMode.Recycling));
        }

        /// <summary>
        ///     Instantiates a new instance of this class.
        /// </summary>
        public DataGridCellsPresenter()
        {
        }

        #endregion

        #region Row Communication

        /// <summary>
        ///     Tells the row owner about this element.
        /// </summary>
        public override void OnApplyTemplate()
        {
            // If a new template has just been generated then 
            // be sure to clear any stale ItemsHost references
            if (InternalItemsHost != null && !this.IsAncestorOf(InternalItemsHost))
            {
                InternalItemsHost = null;
            }
            
#if BindingGroups
            if (ItemBindingGroup == null)
            {
                ItemBindingGroup = new BindingGroup();
            }
#endif

            base.OnApplyTemplate();

            DataGridRow owningRow = DataGridRowOwner;
            if (owningRow != null)
            {
                owningRow.CellsPresenter = this;
                Item = owningRow.Item;
            }

            // At the time that a Row is prepared we can't Sync because the CellsPresenter isn't created yet.
            // Doing it here ensures that the CellsPresenter is in the visual tree.
            SyncProperties(false);
        }

        /// <summary>
        ///     Update all properties that get a value from the DataGrid
        /// </summary>
        /// <remarks>
        ///     See comment on DataGridRow.SyncProperties
        /// </remarks>
        internal void SyncProperties(bool forcePrepareCells)
        {
            var dataGridOwner = DataGridOwner;
            if (dataGridOwner == null)
            {
                return;
            }

            DataGridHelper.TransferProperty(this, HeightProperty);
            DataGridHelper.TransferProperty(this, MinHeightProperty);
            DataGridHelper.TransferProperty(this, VirtualizingPanel.IsVirtualizingProperty);

            // This is a convenient way to walk through all cells and force them to call CoerceValue(StyleProperty)
            NotifyPropertyChanged(this, new DependencyPropertyChangedEventArgs(DataGrid.CellStyleProperty, null, null), DataGridNotificationTarget.Cells);

            // We may have missed an Add / Remove of a column from the grid (DataGridRow.OnColumnsChanged)
            // Sync the MultipleCopiesCollection count and update the Column on changed cells
            MultipleCopiesCollection cellItems = ItemsSource as MultipleCopiesCollection;
            if (cellItems != null)
            {
                DataGridCell cell;
                ObservableCollection<DataGridColumn> columns = dataGridOwner.Columns;
                int newColumnCount = columns.Count;
                int oldColumnCount = cellItems.Count;
                int dirtyCount = 0;
                bool measureAndArrangeInvalidated = false;

                if (newColumnCount != oldColumnCount)
                {
                    cellItems.SyncToCount(newColumnCount);

                    // Newly added or removed containers will be updated by the generator via PrepareContainer.
                    // All others may have a different column
                    dirtyCount = Math.Min(newColumnCount, oldColumnCount);
                }
                else if (forcePrepareCells)
                {
                    dirtyCount = newColumnCount;
                }

                // if the DataGridCellsPanel missed out on some column virtualization
                // activity while the row was virtualized, it needs to be measured
                DataGridCellsPanel cellsPanel = InternalItemsHost as DataGridCellsPanel;
                if (cellsPanel != null)
                {
                    if (cellsPanel.HasCorrectRealizedColumns)
                    {
                        // This operation is performed when a DataGridRow is being prepared. So if we are working 
                        // with a recycled DataGridRow we need to make sure to re-arrange it so that it picks up the 
                        // correct CellsPanelHorizontalOffset.
                        cellsPanel.InvalidateArrange();
                    }
                    else
                    {
                        InvalidateDataGridCellsPanelMeasureAndArrange();
                        measureAndArrangeInvalidated = true;
                    }
                }

                DataGridRow row = DataGridRowOwner;

                // Prepare the cells until dirtyCount is reached. Also invalidate the cells panel's measure
                // and arrange if there is a mismatch between cell.ActualWidth and Column.Width.DisplayValue
                for (int i = 0; i < dirtyCount; i++)
                {
                    cell = (DataGridCell)ItemContainerGenerator.ContainerFromIndex(i);
                    if (cell != null)
                    {
                        cell.PrepareCell(row.Item, this, row);
                        if (!measureAndArrangeInvalidated && !DoubleUtil.AreClose(cell.ActualWidth, columns[i].Width.DisplayValue))
                        {
                            InvalidateDataGridCellsPanelMeasureAndArrange();
                            measureAndArrangeInvalidated = true;
                        }
                    }
                }

                // Keep searching for the mismatch between cell.ActualWidth
                // and Column.Width.DisplayValue
                if (!measureAndArrangeInvalidated)
                {
                    for (int i = dirtyCount; i < newColumnCount; i++)
                    {
                        cell = (DataGridCell)ItemContainerGenerator.ContainerFromIndex(i);
                        if (cell != null)
                        {
                            if (!DoubleUtil.AreClose(cell.ActualWidth, columns[i].Width.DisplayValue))
                            {
                                InvalidateDataGridCellsPanelMeasureAndArrange();
                                break;
                            }
                        }
                    }
                }
            }
        }

        private static object OnCoerceHeight(DependencyObject d, object baseValue)
        {
            var cellsPresenter = d as DataGridCellsPresenter;
            return DataGridHelper.GetCoercedTransferPropertyValue(
                cellsPresenter,
                baseValue,
                HeightProperty,
                cellsPresenter.DataGridOwner,
                DataGrid.RowHeightProperty);
        }

        private static object OnCoerceMinHeight(DependencyObject d, object baseValue)
        {
            var cellsPresenter = d as DataGridCellsPresenter;
            return DataGridHelper.GetCoercedTransferPropertyValue(
                cellsPresenter,
                baseValue,
                MinHeightProperty,
                cellsPresenter.DataGridOwner,
                DataGrid.MinRowHeightProperty);
        }

        #endregion

        #region Data Item

        /// <summary>
        ///     The item that the row represents. This item is an entry in the list of items from the DataGrid.
        ///     From this item, cells are generated for each column in the DataGrid.
        /// </summary>
        public object Item
        {
            get
            {
                return _item;
            }

            internal set
            {
                if (_item != value)
                {
                    object oldItem = _item;
                    _item = value;
                    OnItemChanged(oldItem, _item);
                }
            }
        }

        /// <summary>
        ///     Called when the value of the Item property changes.
        /// </summary>
        /// <param name="oldItem">The old value of Item.</param>
        /// <param name="newItem">The new value of Item.</param>
        protected virtual void OnItemChanged(object oldItem, object newItem)
        {
            ObservableCollection<DataGridColumn> columns = Columns;

            if (columns != null)
            {
                // Either update or create a collection that will return the row's data item
                // n number of times, where n is the number of columns.
                MultipleCopiesCollection cellItems = ItemsSource as MultipleCopiesCollection;
                if (cellItems == null)
                {
                    cellItems = new MultipleCopiesCollection(newItem, columns.Count);
                    ItemsSource = cellItems;
                }
                else
                {
                    cellItems.CopiedItem = newItem;
                }
            }
        }

        #endregion

        #region Cell Container Generation

        /// <summary>
        ///     Determines if an item is its own container.
        /// </summary>
        /// <param name="item">The item to test.</param>
        /// <returns>true if the item is a DataGridCell, false otherwise.</returns>
        protected override bool IsItemItsOwnContainerOverride(object item)
        {
            return item is DataGridCell;
        }

        /// <summary>
        ///     Method which returns the result of IsItemItsOwnContainerOverride to be used internally
        /// </summary>
        internal bool IsItemItsOwnContainerInternal(object item)
        {
            return IsItemItsOwnContainerOverride(item);
        }

        /// <summary>
        ///     Instantiates an instance of a container.
        /// </summary>
        /// <returns>A new DataGridCell.</returns>
        protected override DependencyObject GetContainerForItemOverride()
        {
            return new DataGridCell();
        }

        /// <summary>
        ///     Prepares a new container for a given item.
        /// </summary>
        /// <param name="element">The new container.</param>
        /// <param name="item">The item that the container represents.</param>
        protected override void PrepareContainerForItemOverride(DependencyObject element, object item)
        {
            DataGridCell cell = (DataGridCell)element;
            DataGridRow rowOwner = DataGridRowOwner;

            if (cell.RowOwner != rowOwner)
            {
                cell.Tracker.StartTracking(ref _cellTrackingRoot);
            }

            cell.PrepareCell(item, this, rowOwner);
        }

        /// <summary>
        ///     Clears a container of references.
        /// </summary>
        /// <param name="element">The container being cleared.</param>
        /// <param name="item">The data item that the container represented.</param>
        protected override void ClearContainerForItemOverride(DependencyObject element, object item)
        {
            DataGridCell cell = (DataGridCell)element;
            DataGridRow rowOwner = DataGridRowOwner;

            if (cell.RowOwner == rowOwner)
            {
                cell.Tracker.StopTracking(ref _cellTrackingRoot);
            }

            cell.ClearCell(rowOwner);
        }

        /// <summary>
        ///     Notification from the DataGrid that the columns collection has changed.
        /// </summary>
        /// <param name="columns">The columns collection.</param>
        /// <param name="e">The event arguments from the collection's change event.</param>
        protected internal virtual void OnColumnsChanged(ObservableCollection<DataGridColumn> columns, NotifyCollectionChangedEventArgs e)
        {
            // Update the ItemsSource for the cells
            MultipleCopiesCollection cellItems = ItemsSource as MultipleCopiesCollection;
            if (cellItems != null)
            {
                cellItems.MirrorCollectionChange(e);
            }

            // For a reset event the only thing the MultipleCopiesCollection can do is set its count to 0.
            Debug.Assert(
                e.Action != NotifyCollectionChangedAction.Reset || columns.Count == 0,
                "A Reset event should only be fired for a Clear event from the columns collection");
        }

        #endregion

        #region Notification Propagation

        /// <summary>
        /// Notification of Height & MinHeight changes.
        /// </summary>
        private static void OnNotifyHeightPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((DataGridCellsPresenter)d).NotifyPropertyChanged(d, e, DataGridNotificationTarget.CellsPresenter);
        }

        /// <summary>
        ///     General notification for DependencyProperty changes from the grid or from columns.
        /// </summary>
        internal void NotifyPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e, DataGridNotificationTarget target)
        {
            NotifyPropertyChanged(d, string.Empty, e, target);
        }

        /// <summary>
        ///     General notification for DependencyProperty changes from the grid or from columns.
        /// </summary>
        internal void NotifyPropertyChanged(DependencyObject d, string propertyName, DependencyPropertyChangedEventArgs e, DataGridNotificationTarget target)
        {
            if (DataGridHelper.ShouldNotifyCellsPresenter(target))
            {
                if (e.Property == DataGridColumn.WidthProperty ||
                    e.Property == DataGridColumn.DisplayIndexProperty)
                {
                    if (((DataGridColumn)d).IsVisible)
                    {
                        InvalidateDataGridCellsPanelMeasureAndArrangeImpl((e.Property == DataGridColumn.WidthProperty)/*invalidateMeasureUptoRowsPresenter*/);
                    }
                }
                else if (e.Property == DataGrid.FrozenColumnCountProperty ||
                    e.Property == DataGridColumn.VisibilityProperty ||
                    e.Property == DataGrid.CellsPanelHorizontalOffsetProperty ||
                    e.Property == DataGrid.HorizontalScrollOffsetProperty ||
                    string.Compare(propertyName, "ViewportWidth", StringComparison.Ordinal) == 0 ||
                    string.Compare(propertyName, "DelayedColumnWidthComputation", StringComparison.Ordinal) == 0)
                {
                    InvalidateDataGridCellsPanelMeasureAndArrange();
                }
                else if (string.Compare(propertyName, "RealizedColumnsBlockListForNonVirtualizedRows", StringComparison.Ordinal) == 0)
                {
                    InvalidateDataGridCellsPanelMeasureAndArrange(/* withColumnVirtualization */ false);
                }
                else if (string.Compare(propertyName, "RealizedColumnsBlockListForVirtualizedRows", StringComparison.Ordinal) == 0)
                {
                    InvalidateDataGridCellsPanelMeasureAndArrange(/* withColumnVirtualization */ true);
                }
                else if (e.Property == DataGrid.RowHeightProperty || e.Property == HeightProperty)
                {
                    DataGridHelper.TransferProperty(this, HeightProperty);
                }
                else if (e.Property == DataGrid.MinRowHeightProperty || e.Property == MinHeightProperty)
                {
                    DataGridHelper.TransferProperty(this, MinHeightProperty);
                }
                else if (e.Property == DataGrid.EnableColumnVirtualizationProperty)
                {
                    DataGridHelper.TransferProperty(this, VirtualizingPanel.IsVirtualizingProperty);
                }
            }

            if (DataGridHelper.ShouldNotifyCells(target) ||
                DataGridHelper.ShouldRefreshCellContent(target))
            {
                ContainerTracking<DataGridCell> tracker = _cellTrackingRoot;
                while (tracker != null)
                {
                    tracker.Container.NotifyPropertyChanged(d, propertyName, e, target);
                    tracker = tracker.Next;
                }
            }
        }

        #endregion

        #region GridLines

        // Different parts of the DataGrid draw different pieces of the GridLines.
        // Rows draw a single horizontal line on the bottom.  The DataGridDetailsPresenter is the element that handles it.

        /// <summary>
        ///     Measure.  This is overridden so that the row can extend its size to account for a grid line on the bottom.
        /// </summary>
        /// <param name="availableSize"></param>
        /// <returns></returns>
        protected override Size MeasureOverride(Size availableSize)
        {
            return base.MeasureOverride(availableSize);
        }

        /// <summary>
        ///     Arrange.  This is overriden so that the row can position its content to account for a grid line on the bottom.
        /// </summary>
        /// <param name="finalSize">Arrange size</param>
        protected override Size ArrangeOverride(Size finalSize)
        {
            return base.ArrangeOverride(finalSize);
        }

        /// <summary>
        ///     OnRender.  Overriden to draw a horizontal line underneath the content.
        /// </summary>
        /// <param name="drawingContext"></param>
        protected override void OnRender(DrawingContext drawingContext)
        {
            base.OnRender(drawingContext);

            var row = DataGridRowOwner;
            if (row == null)
            {
                return;
            }

            var dataGrid = row.DataGridOwner;
            if (dataGrid == null)
            {
                return;
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

        #region Column Virtualization

        /// <summary>
        ///     Property changed callback for VirtualizingStackPanel.IsVirtualizing property
        /// </summary>
        private static void OnIsVirtualizingPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            DataGridCellsPresenter cellsPresenter = (DataGridCellsPresenter)d;
            DataGridHelper.TransferProperty(cellsPresenter, VirtualizingPanel.IsVirtualizingProperty);
            if (e.OldValue != cellsPresenter.GetValue(VirtualizingPanel.IsVirtualizingProperty))
            {
                cellsPresenter.InvalidateDataGridCellsPanelMeasureAndArrange();
            }
        }

        /// <summary>
        ///     Coercion callback for VirtualizingStackPanel.IsVirtualizing property
        /// </summary>
        private static object OnCoerceIsVirtualizingProperty(DependencyObject d, object baseValue)
        {
            var cellsPresenter = d as DataGridCellsPresenter;
            return DataGridHelper.GetCoercedTransferPropertyValue(
                cellsPresenter,
                baseValue,
                VirtualizingPanel.IsVirtualizingProperty,
                cellsPresenter.DataGridOwner,
                DataGrid.EnableColumnVirtualizationProperty);
        }

        /// <summary>
        ///     Helper method which invalidate the underlying itemshost's measure and arrange
        /// </summary>
        internal void InvalidateDataGridCellsPanelMeasureAndArrange()
        {
            InvalidateDataGridCellsPanelMeasureAndArrangeImpl(false);
        }

        /// <summary>
        ///     Helper method which invalidate the underlying itemshost's measure and arrange
        /// </summary>
        private void InvalidateDataGridCellsPanelMeasureAndArrangeImpl(bool invalidateMeasureUptoRowsPresenter)
        {
            if (_internalItemsHost != null)
            {
                _internalItemsHost.InvalidateMeasure();
                _internalItemsHost.InvalidateArrange();

                if(invalidateMeasureUptoRowsPresenter)
                {
                    DataGrid dataGrid = DataGridOwner;
                    if(dataGrid != null && dataGrid.InternalItemsHost != null)
                    {
                        Helper.InvalidateMeasureOnPath(_internalItemsHost, dataGrid.InternalItemsHost, false/*duringMeasure*/, true/*includePathEnd*/);
                    }
                }
            }
        }

        /// <summary>
        ///     Helper method which invalidate the underlying itemshost's measure and arrange
        /// </summary>
        /// <param name="withColumnVirtualization">
        ///     True to invalidate only when virtualization is on.
        ///     False to invalidate only when virtualization is off.
        /// </param>
        private void InvalidateDataGridCellsPanelMeasureAndArrange(bool withColumnVirtualization)
        {
            // Invalidates measure and arrange if the flag and the virtualization
            // are either both true or both false.
            if (withColumnVirtualization == VirtualizingPanel.GetIsVirtualizing(this))
            {
                InvalidateDataGridCellsPanelMeasureAndArrange();
            }
        }

        /// <summary>
        ///     Workaround for not being able to access the panel instance of
        ///     itemscontrol directly
        /// </summary>
        internal Panel InternalItemsHost
        {
            get { return _internalItemsHost; }
            set { _internalItemsHost = value; }
        }

        /// <summary>
        ///     Method which tries to scroll a cell for given index into the scroll view
        /// </summary>
        /// <param name="index"></param>
        internal void ScrollCellIntoView(int index)
        {
            DataGridCellsPanel itemsHost = InternalItemsHost as DataGridCellsPanel;
            if (itemsHost != null)
            {
                itemsHost.InternalBringIndexIntoView(index);
                return;
            }
        }

        #endregion

        #region Helpers

        /// <summary>
        ///     The DataGrid that owns this control
        /// </summary>
        internal DataGrid DataGridOwner
        {
            get
            {
                DataGridRow parent = DataGridRowOwner;
                if (parent != null)
                {
                    return parent.DataGridOwner;
                }

                return null;
            }
        }

        /// <summary>
        ///     The DataGridRow that owns this control.
        /// </summary>
        internal DataGridRow DataGridRowOwner
        {
            get { return DataGridHelper.FindParent<DataGridRow>(this); }
        }

        private ObservableCollection<DataGridColumn> Columns
        {
            get
            {
                DataGridRow owningRow = DataGridRowOwner;
                DataGrid owningDataGrid = (owningRow != null) ? owningRow.DataGridOwner : null;
                return (owningDataGrid != null) ? owningDataGrid.Columns : null;
            }
        }

        internal ContainerTracking<DataGridCell> CellTrackingRoot
        {
            get { return _cellTrackingRoot; }
        }

        #endregion

        #region Data

        private object _item;
        private ContainerTracking<DataGridCell> _cellTrackingRoot;    // Root of a linked list of active cell containers
        private Panel _internalItemsHost;

        #endregion
    }
}
