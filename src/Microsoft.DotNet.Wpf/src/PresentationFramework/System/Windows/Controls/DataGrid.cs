// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security;
using System.Text;
using System.Windows.Automation;
using System.Windows.Automation.Peers;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using MS.Internal;
using MS.Internal.Data;
using MS.Internal.Telemetry.PresentationFramework;

namespace System.Windows.Controls
{
    /// <summary>
    ///     A DataGrid control that displays data in rows and columns and allows
    ///     for the entering and editing of data.
    /// </summary>
    public class DataGrid : MultiSelector
    {
        #region Constructors

        /// <summary>
        ///     Instantiates global information.
        /// </summary>
        static DataGrid()
        {
            Type ownerType = typeof(DataGrid);

            DefaultStyleKeyProperty.OverrideMetadata(ownerType, new FrameworkPropertyMetadata(typeof(DataGrid)));
            FrameworkElementFactory dataGridRowPresenterFactory = new FrameworkElementFactory(typeof(DataGridRowsPresenter));
            dataGridRowPresenterFactory.SetValue(FrameworkElement.NameProperty, ItemsPanelPartName);
            ItemsPanelProperty.OverrideMetadata(ownerType, new FrameworkPropertyMetadata(new ItemsPanelTemplate(dataGridRowPresenterFactory)));
            VirtualizingPanel.IsVirtualizingProperty.OverrideMetadata(ownerType, new FrameworkPropertyMetadata(true, null, new CoerceValueCallback(OnCoerceIsVirtualizingProperty)));
            VirtualizingPanel.VirtualizationModeProperty.OverrideMetadata(ownerType, new FrameworkPropertyMetadata(VirtualizationMode.Recycling));
            ItemContainerStyleProperty.OverrideMetadata(ownerType, new FrameworkPropertyMetadata(null, new CoerceValueCallback(OnCoerceItemContainerStyle)));
            ItemContainerStyleSelectorProperty.OverrideMetadata(ownerType, new FrameworkPropertyMetadata(null, new CoerceValueCallback(OnCoerceItemContainerStyleSelector)));
            ItemsSourceProperty.OverrideMetadata(ownerType, new FrameworkPropertyMetadata((PropertyChangedCallback)null, OnCoerceItemsSourceProperty));
            AlternationCountProperty.OverrideMetadata(ownerType, new FrameworkPropertyMetadata(0, null, new CoerceValueCallback(OnCoerceAlternationCount)));
            IsEnabledProperty.OverrideMetadata(ownerType, new FrameworkPropertyMetadata(new PropertyChangedCallback(OnIsEnabledChanged)));
            IsKeyboardFocusWithinPropertyKey.OverrideMetadata(ownerType, new FrameworkPropertyMetadata(new PropertyChangedCallback(OnIsKeyboardFocusWithinChanged)));
            IsSynchronizedWithCurrentItemProperty.OverrideMetadata(ownerType, new FrameworkPropertyMetadata(null, new CoerceValueCallback(OnCoerceIsSynchronizedWithCurrentItem)));
            IsTabStopProperty.OverrideMetadata(ownerType, new FrameworkPropertyMetadata(false));
            KeyboardNavigation.DirectionalNavigationProperty.OverrideMetadata(ownerType, new FrameworkPropertyMetadata(KeyboardNavigationMode.Contained));
            KeyboardNavigation.ControlTabNavigationProperty.OverrideMetadata(ownerType, new FrameworkPropertyMetadata(KeyboardNavigationMode.Once));

            CommandManager.RegisterClassInputBinding(ownerType, new InputBinding(BeginEditCommand, new KeyGesture(Key.F2)));
            CommandManager.RegisterClassCommandBinding(ownerType, new CommandBinding(BeginEditCommand, new ExecutedRoutedEventHandler(OnExecutedBeginEdit), new CanExecuteRoutedEventHandler(OnCanExecuteBeginEdit)));

            CommandManager.RegisterClassCommandBinding(ownerType, new CommandBinding(CommitEditCommand, new ExecutedRoutedEventHandler(OnExecutedCommitEdit), new CanExecuteRoutedEventHandler(OnCanExecuteCommitEdit)));

            CommandManager.RegisterClassInputBinding(ownerType, new InputBinding(CancelEditCommand, new KeyGesture(Key.Escape)));
            CommandManager.RegisterClassCommandBinding(ownerType, new CommandBinding(CancelEditCommand, new ExecutedRoutedEventHandler(OnExecutedCancelEdit), new CanExecuteRoutedEventHandler(OnCanExecuteCancelEdit)));

            CommandManager.RegisterClassCommandBinding(ownerType, new CommandBinding(SelectAllCommand, new ExecutedRoutedEventHandler(OnExecutedSelectAll), new CanExecuteRoutedEventHandler(OnCanExecuteSelectAll)));

            CommandManager.RegisterClassCommandBinding(ownerType, new CommandBinding(DeleteCommand, new ExecutedRoutedEventHandler(OnExecutedDelete), new CanExecuteRoutedEventHandler(OnCanExecuteDelete)));

            // Default Clipboard handling
            CommandManager.RegisterClassCommandBinding(typeof(DataGrid), new CommandBinding(ApplicationCommands.Copy, new ExecutedRoutedEventHandler(OnExecutedCopy), new CanExecuteRoutedEventHandler(OnCanExecuteCopy)));

            EventManager.RegisterClassHandler(typeof(DataGrid), MouseUpEvent, new MouseButtonEventHandler(OnAnyMouseUpThunk), true);

            ControlsTraceLogger.AddControl(TelemetryControls.DataGrid);
        }

        /// <summary>
        ///     Instantiates a new instance of this class.
        /// </summary>
        public DataGrid()
        {
            _columns = new DataGridColumnCollection(this);
            _columns.CollectionChanged += new NotifyCollectionChangedEventHandler(OnColumnsChanged);

            _rowValidationRules = new ObservableCollection<ValidationRule>();
            _rowValidationRules.CollectionChanged += new NotifyCollectionChangedEventHandler(OnRowValidationRulesChanged);

            _selectedCells = new SelectedCellsCollection(this);

            ((INotifyCollectionChanged)Items).CollectionChanged += new NotifyCollectionChangedEventHandler(OnItemsCollectionChanged);

            ((INotifyCollectionChanged)Items.SortDescriptions).CollectionChanged += new NotifyCollectionChangedEventHandler(OnItemsSortDescriptionsChanged);
            Items.GroupDescriptions.CollectionChanged += new NotifyCollectionChangedEventHandler(OnItemsGroupDescriptionsChanged);

            // Compute column widths but wait until first load
            InternalColumns.InvalidateColumnWidthsComputation();

            CellsPanelHorizontalOffsetComputationPending = false;
        }

        #endregion

        #region Columns

        /// <summary>
        ///     A collection of column definitions describing the individual
        ///     columns of each row.
        /// </summary>
        public ObservableCollection<DataGridColumn> Columns
        {
            get { return _columns; }
        }

        /// <summary>
        ///     Returns the column collection without having to upcast from ObservableCollection
        /// </summary>
        internal DataGridColumnCollection InternalColumns
        {
            get { return _columns; }
        }

        /// <summary>
        ///     A property that specifies whether the user can resize columns in the UI by dragging the column headers.
        /// </summary>
        /// <remarks>
        ///     This does not affect whether column widths can be changed programmatically via a property such as Column.Width.
        /// </remarks>
        public bool CanUserResizeColumns
        {
            get { return (bool)GetValue(CanUserResizeColumnsProperty); }
            set { SetValue(CanUserResizeColumnsProperty, value); }
        }

        /// <summary>
        ///     The DependencyProperty that represents the CanUserResizeColumns property.
        /// </summary>
        public static readonly DependencyProperty CanUserResizeColumnsProperty =
            DependencyProperty.Register("CanUserResizeColumns", typeof(bool), typeof(DataGrid), new FrameworkPropertyMetadata(true, new PropertyChangedCallback(OnNotifyColumnAndColumnHeaderPropertyChanged)));

        /// <summary>
        ///     Specifies the width of the header and cells within all the columns.
        /// </summary>
        public DataGridLength ColumnWidth
        {
            get { return (DataGridLength)GetValue(ColumnWidthProperty); }
            set { SetValue(ColumnWidthProperty, value); }
        }

        /// <summary>
        ///     The DependencyProperty that represents the ColumnWidth property.
        /// </summary>
        public static readonly DependencyProperty ColumnWidthProperty =
            DependencyProperty.Register("ColumnWidth", typeof(DataGridLength), typeof(DataGrid), new FrameworkPropertyMetadata(DataGridLength.SizeToHeader));

        /// <summary>
        ///     Specifies the minimum width of the header and cells within all columns.
        /// </summary>
        public double MinColumnWidth
        {
            get { return (double)GetValue(MinColumnWidthProperty); }
            set { SetValue(MinColumnWidthProperty, value); }
        }

        /// <summary>
        ///     The DependencyProperty that represents the MinColumnWidth property.
        /// </summary>
        public static readonly DependencyProperty MinColumnWidthProperty =
            DependencyProperty.Register(
                "MinColumnWidth",
                typeof(double),
                typeof(DataGrid),
                new FrameworkPropertyMetadata(20d, new PropertyChangedCallback(OnColumnSizeConstraintChanged)),
                new ValidateValueCallback(ValidateMinColumnWidth));

        /// <summary>
        ///     Specifies the maximum width of the header and cells within all columns.
        /// </summary>
        public double MaxColumnWidth
        {
            get { return (double)GetValue(MaxColumnWidthProperty); }
            set { SetValue(MaxColumnWidthProperty, value); }
        }

        /// <summary>
        ///     The DependencyProperty that represents the  MaxColumnWidth property.
        /// </summary>
        public static readonly DependencyProperty MaxColumnWidthProperty =
            DependencyProperty.Register(
                "MaxColumnWidth",
                typeof(double),
                typeof(DataGrid),
                new FrameworkPropertyMetadata(double.PositiveInfinity, new PropertyChangedCallback(OnColumnSizeConstraintChanged)),
                new ValidateValueCallback(ValidateMaxColumnWidth));

        private static void OnColumnSizeConstraintChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((DataGrid)d).NotifyPropertyChanged(d, e, DataGridNotificationTarget.Columns);
        }

        /// <summary>
        /// Validates that the minimum column width is an acceptable value
        /// </summary>
        private static bool ValidateMinColumnWidth(object v)
        {
            double value = (double)v;
            return !(value < 0d || DoubleUtil.IsNaN(value) || Double.IsPositiveInfinity(value));
        }

        /// <summary>
        /// Validates that the maximum column width is an acceptable value
        /// </summary>
        private static bool ValidateMaxColumnWidth(object v)
        {
            double value = (double)v;
            return !(value < 0d || DoubleUtil.IsNaN(value));
        }

        /// <summary>
        ///     Called when the Columns collection changes.
        /// </summary>
        private void OnColumnsChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            // Update the reference to this DataGrid on the affected column(s)
            // and update the SelectedCells collection.
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    UpdateDataGridReference(e.NewItems, /* clear = */ false);
                    UpdateColumnSizeConstraints(e.NewItems);
                    break;

                case NotifyCollectionChangedAction.Remove:
                    UpdateDataGridReference(e.OldItems, /* clear = */ true);
                    break;

                case NotifyCollectionChangedAction.Replace:
                    UpdateDataGridReference(e.OldItems, /* clear = */ true);
                    UpdateDataGridReference(e.NewItems, /* clear = */ false);
                    UpdateColumnSizeConstraints(e.NewItems);
                    break;

                case NotifyCollectionChangedAction.Reset:
                    // We can't clear column references on Reset: _columns has 0 items and e.OldItems is empty.
                    _selectedCells.Clear();
                    break;
            }

            // FrozenColumns rely on column DisplayIndex
            // Delay the coercion if necessary
            if (InternalColumns.DisplayIndexMapInitialized)
            {
                CoerceValue(FrozenColumnCountProperty);
            }

            bool visibleColumnsChanged = HasVisibleColumns(e.OldItems);
            visibleColumnsChanged |= HasVisibleColumns(e.NewItems);
            visibleColumnsChanged |= (e.Action == NotifyCollectionChangedAction.Reset);

            if (visibleColumnsChanged)
            {
                InternalColumns.InvalidateColumnRealization(true);
            }

            UpdateColumnsOnRows(e);

            // Recompute the column width if required, but wait until the first load
            if (visibleColumnsChanged && e.Action != NotifyCollectionChangedAction.Move)
            {
                InternalColumns.InvalidateColumnWidthsComputation();
            }
        }

        /// <summary>
        ///     Updates the reference to this DataGrid on the list of columns.
        /// </summary>
        /// <param name="list">The list of affected columns.</param>
        /// <param name="clear">Whether to add or remove the reference to this grid.</param>
        internal void UpdateDataGridReference(IList list, bool clear)
        {
            int numItems = list.Count;
            for (int i = 0; i < numItems; i++)
            {
                DataGridColumn column = (DataGridColumn)list[i];
                if (clear)
                {
                    // Set the owner to null only if the current owner is this grid
                    if (column.DataGridOwner == this)
                    {
                        column.DataGridOwner = null;
                    }
                }
                else
                {
                    // Remove the column from any old owner
                    if (column.DataGridOwner != null && column.DataGridOwner != this)
                    {
                        column.DataGridOwner.Columns.Remove(column);
                    }

                    column.DataGridOwner = this;
                }
            }
        }

        /// <summary>
        ///     Updates the transferred size constraints from DataGrid on the columns.
        /// </summary>
        /// <param name="list">The list of affected columns.</param>
        private static void UpdateColumnSizeConstraints(IList list)
        {
            var count = list.Count;
            for (var i = 0; i < count; i++)
            {
                var column = (DataGridColumn)list[i];
                column.SyncProperties();
            }
        }

        /// <summary>
        ///     Helper method which determines if the
        ///     given list has visible columns
        /// </summary>
        private static bool HasVisibleColumns(IList columns)
        {
            if (columns != null && columns.Count > 0)
            {
                foreach (DataGridColumn column in columns)
                {
                    if (column.IsVisible)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private static readonly UncommonField<int> BringColumnIntoViewRetryCountField
            = new UncommonField<int>(0);
        const int MaxBringColumnIntoViewRetries = 4;

        /// <summary>
        ///     Called from DataGridCellsPanel.BringIndexIntoView to request a
        ///     retry, to handle column width changes due to deferred data binding.
        ///     To prevent infinite loops, simply give up after a fixed number
        ///     of retries, leaving the desired column possibly out of view.
        ///     (One retry was enough to bring the column into view
        ///     in all the cases we tested - two if you include the "layout demoted"
        ///     scenario described in BringIndexIntoView.  More complex data could
        ///     need more retries.  In theory there is no upper bound, but in practice
        ///     the max declared here should suffice.)
        /// </summary>
        internal bool RetryBringColumnIntoView(bool retryRequested)
        {
            if (retryRequested)
            {
                // if the number of retries hasn't exceeded the limit,
                // update the count and allow the retry
                int retries = BringColumnIntoViewRetryCountField.GetValue(this);
                if (retries < MaxBringColumnIntoViewRetries)
                {
                    BringColumnIntoViewRetryCountField.SetValue(this, retries+1);
                    return true;
                }
            }

            // we're not going to retry
            BringColumnIntoViewRetryCountField.ClearValue(this);
            return false;
        }

        #endregion

        #region Display Index

        /// <summary>
        ///     Returns the DataGridColumn with the given DisplayIndex
        /// </summary>
        public DataGridColumn ColumnFromDisplayIndex(int displayIndex)
        {
            if (displayIndex < 0 || displayIndex >= Columns.Count)
            {
                throw new ArgumentOutOfRangeException("displayIndex", displayIndex, SR.Get(SRID.DataGrid_DisplayIndexOutOfRange));
            }

            return InternalColumns.ColumnFromDisplayIndex(displayIndex);
        }

        /// <summary>
        ///     Event that is fired when the DisplayIndex on one of the DataGrid's Columns changes.
        /// </summary>
        public event EventHandler<DataGridColumnEventArgs> ColumnDisplayIndexChanged;

        /// <summary>
        ///     Called when the DisplayIndex of a column is modified.
        /// </summary>
        /// <remarks>
        ///     A column's DisplayIndex may be modified as the result of another column's DisplayIndex changing.  This is because the
        ///     DataGrid enforces that the DisplayIndex of all Columns are unique integers from 0 to Columns.Count -1.
        /// </remarks>
        protected internal virtual void OnColumnDisplayIndexChanged(DataGridColumnEventArgs e)
        {
            if (ColumnDisplayIndexChanged != null)
            {
                ColumnDisplayIndexChanged(this, e);
            }
        }

        /// <summary>
        ///     A map of display index (key) to index in the column collection (value).
        ///     Used by the CellsPanel to quickly find a child from a column display index.
        /// </summary>
        internal List<int> DisplayIndexMap
        {
            get { return InternalColumns.DisplayIndexMap; }
        }

        /// <summary>
        ///     Throws an ArgumentOutOfRangeException if the given displayIndex is invalid.
        /// </summary>
        internal void ValidateDisplayIndex(DataGridColumn column, int displayIndex)
        {
            InternalColumns.ValidateDisplayIndex(column, displayIndex);
        }

        /// <summary>
        ///     Returns the index of a column from the given DisplayIndex
        /// </summary>
        internal int ColumnIndexFromDisplayIndex(int displayIndex)
        {
            if (displayIndex >= 0 && displayIndex < DisplayIndexMap.Count)
            {
                return DisplayIndexMap[displayIndex];
            }

            return -1;
        }

        /// <summary>
        ///     Given the DisplayIndex of a column returns the DataGridColumnHeader for that column.
        ///     Used by DataGridColumnHeader to find its previous sibling.
        /// </summary>
        /// <param name="displayIndex"></param>
        /// <returns></returns>
        internal DataGridColumnHeader ColumnHeaderFromDisplayIndex(int displayIndex)
        {
            int columnIndex = ColumnIndexFromDisplayIndex(displayIndex);

            if (columnIndex != -1)
            {
                if (ColumnHeadersPresenter != null && ColumnHeadersPresenter.ItemContainerGenerator != null)
                {
                    return (DataGridColumnHeader)ColumnHeadersPresenter.ItemContainerGenerator.ContainerFromIndex(columnIndex);
                }
            }

            return null;
        }

        #endregion

        #region Notification Propagation

        /// <summary>
        ///     Notifies each CellsPresenter about property changes.
        /// </summary>
        private static void OnNotifyCellsPresenterPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((DataGrid)d).NotifyPropertyChanged(d, e, DataGridNotificationTarget.CellsPresenter);
        }

        /// <summary>
        ///     Notifies each Column and Cell about property changes.
        /// </summary>
        private static void OnNotifyColumnAndCellPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((DataGrid)d).NotifyPropertyChanged(d, e, DataGridNotificationTarget.Columns | DataGridNotificationTarget.Cells);
        }

        /// <summary>
        ///     Notifies each Column about property changes.
        /// </summary>
        private static void OnNotifyColumnPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((DataGrid)d).NotifyPropertyChanged(d, e, DataGridNotificationTarget.Columns);
        }

        /// <summary>
        ///     Notifies the Column & Column Headers about property changes.
        /// </summary>
        private static void OnNotifyColumnAndColumnHeaderPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((DataGrid)d).NotifyPropertyChanged(d, e, DataGridNotificationTarget.Columns | DataGridNotificationTarget.ColumnHeaders);
        }

        /// <summary>
        ///     Notifies the Column Headers about property changes.
        /// </summary>
        private static void OnNotifyColumnHeaderPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((DataGrid)d).NotifyPropertyChanged(d, e, DataGridNotificationTarget.ColumnHeaders);
        }

        /// <summary>
        ///     Notifies the Row and Column Headers about property changes (used by the AlternationBackground property)
        /// </summary>
        private static void OnNotifyHeaderPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((DataGrid)d).NotifyPropertyChanged(d, e, DataGridNotificationTarget.ColumnHeaders | DataGridNotificationTarget.RowHeaders);
        }

        /// <summary>
        ///     Notifies the DataGrid and each Row about property changes.
        /// </summary>
        private static void OnNotifyDataGridAndRowPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((DataGrid)d).NotifyPropertyChanged(d, e, DataGridNotificationTarget.Rows | DataGridNotificationTarget.DataGrid);
        }

        /// <summary>
        ///     Notifies everyone who cares about GridLine property changes (Row, Cell, RowHeader, ColumnHeader)
        /// </summary>
        private static void OnNotifyGridLinePropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            // Clear out and regenerate all containers.  We do this so that we don't have to propagate this notification
            // to containers that are currently on the recycle queue -- doing so costs us perf on every scroll.  We don't
            // care about the time spent on a GridLine change since it'll be a very rare occurance.
            //
            // ItemsControl.OnItemTemplateChanged calls the internal ItemContainerGenerator.Refresh() method, which
            // clears out all containers and notifies the panel.  The fact we're passing in two null templates is ignored.
            if (e.OldValue != e.NewValue)
            {
                ((DataGrid)d).OnItemTemplateChanged(null, null);
            }
        }

        /// <summary>
        ///     Notifies each Row about property changes.
        /// </summary>
        private static void OnNotifyRowPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((DataGrid)d).NotifyPropertyChanged(d, e, DataGridNotificationTarget.Rows);
        }

        /// <summary>
        ///     Notifies the Row Headers about property changes.
        /// </summary>
        private static void OnNotifyRowHeaderPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((DataGrid)d).NotifyPropertyChanged(d, e, DataGridNotificationTarget.RowHeaders);
        }

        /// <summary>
        ///     Notifies the Row & Row Headers about property changes.
        /// </summary>
        private static void OnNotifyRowAndRowHeaderPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((DataGrid)d).NotifyPropertyChanged(d, e, DataGridNotificationTarget.Rows | DataGridNotificationTarget.RowHeaders);
        }

        /// <summary>
        ///     Notifies the Row & Details about property changes.
        /// </summary>
        private static void OnNotifyRowAndDetailsPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((DataGrid)d).NotifyPropertyChanged(d, e, DataGridNotificationTarget.Rows | DataGridNotificationTarget.DetailsPresenter);
        }

        /// <summary>
        ///     Notifies HorizontalOffset change to columns collection, cellspresenter and column headers presenter
        /// </summary>
        private static void OnNotifyHorizontalOffsetPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((DataGrid)d).NotifyPropertyChanged(d, e, DataGridNotificationTarget.ColumnCollection | DataGridNotificationTarget.CellsPresenter | DataGridNotificationTarget.ColumnHeadersPresenter);
        }

        /// <summary>
        ///     General notification for DependencyProperty changes from the grid or from columns.
        /// </summary>
        /// <remarks>
        ///     This can be called from a variety of sources, such as from column objects
        ///     or from this DataGrid itself when there is a need to notify the rows and/or
        ///     the cells in the DataGrid about a property change. Down-stream handlers
        ///     can check the source of the change using the "d" parameter.
        /// </remarks>
        internal void NotifyPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e, DataGridNotificationTarget target)
        {
            NotifyPropertyChanged(d, string.Empty, e, target);
        }

        /// <summary>
        ///     General notification for DependencyProperty changes from the grid or from columns.
        /// </summary>
        /// <remarks>
        ///     This can be called from a variety of sources, such as from column objects
        ///     or from this DataGrid itself when there is a need to notify the rows and/or
        ///     the cells in the DataGrid about a property change. Down-stream handlers
        ///     can check the source of the change using the "d" parameter.
        /// </remarks>
        internal void NotifyPropertyChanged(DependencyObject d, string propertyName, DependencyPropertyChangedEventArgs e, DataGridNotificationTarget target)
        {
            if (DataGridHelper.ShouldNotifyDataGrid(target))
            {
                if (e.Property == AlternatingRowBackgroundProperty)
                {
                    // If the alternate row background is set, the count may be coerced to 2
                    CoerceValue(AlternationCountProperty);
                }
                else if ((e.Property == DataGridColumn.VisibilityProperty) || (e.Property == DataGridColumn.WidthProperty) || (e.Property == DataGridColumn.DisplayIndexProperty))
                {
                    // DataGridCellsPanel needs to be re-measured when column visibility changes
                    // Recyclable containers may not be fully remeasured when they are brought in
                    foreach (DependencyObject container in ItemContainerGenerator.RecyclableContainers)
                    {
                        DataGridRow row = container as DataGridRow;
                        if (row != null)
                        {
                            var cellsPresenter = row.CellsPresenter;
                            if (cellsPresenter != null)
                            {
                                cellsPresenter.InvalidateDataGridCellsPanelMeasureAndArrange();
                            }
                        }
                    }
                }
            }

            // Rows, Cells, CellsPresenter, DetailsPresenter or RowHeaders
            if (DataGridHelper.ShouldNotifyRowSubtree(target))
            {
                // Notify the Rows about the property change
                ContainerTracking<DataGridRow> tracker = _rowTrackingRoot;
                while (tracker != null)
                {
                    tracker.Container.NotifyPropertyChanged(d, propertyName, e, target);
                    tracker = tracker.Next;
                }
            }

            if (DataGridHelper.ShouldNotifyColumnCollection(target) || DataGridHelper.ShouldNotifyColumns(target))
            {
                InternalColumns.NotifyPropertyChanged(d, propertyName, e, target);
            }

            if ((DataGridHelper.ShouldNotifyColumnHeadersPresenter(target) || DataGridHelper.ShouldNotifyColumnHeaders(target)) && ColumnHeadersPresenter != null)
            {
                ColumnHeadersPresenter.NotifyPropertyChanged(d, propertyName, e, target);
            }
        }

        /// <summary>
        ///     Called by DataGridColumnCollection when columns' DisplayIndex changes
        /// </summary>
        /// <param name="e"></param>
        internal void UpdateColumnsOnVirtualizedCellInfoCollections(NotifyCollectionChangedAction action, int oldDisplayIndex, DataGridColumn oldColumn, int newDisplayIndex)
        {
            using (UpdateSelectedCells())
            {
                _selectedCells.OnColumnsChanged(action, oldDisplayIndex, oldColumn, newDisplayIndex, SelectedItems);
            }
        }

        /// <summary>
        ///     Reference to the ColumnHeadersPresenter. The presenter sets this when it is created.
        /// </summary>
        internal DataGridColumnHeadersPresenter ColumnHeadersPresenter
        {
            get { return _columnHeadersPresenter; }
            set { _columnHeadersPresenter = value; }
        }

        /// <summary>
        ///     OnTemplateChanged override
        /// </summary>
        protected override void OnTemplateChanged(ControlTemplate oldTemplate, ControlTemplate newTemplate)
        {
            base.OnTemplateChanged(oldTemplate, newTemplate);

            // Our column headers presenter comes from the template.  Clear out the reference to it if the template has changed
            ColumnHeadersPresenter = null;
        }

        #endregion

        #region GridLines

        /// <summary>
        ///     GridLinesVisibility Dependency Property
        /// </summary>
        public static readonly DependencyProperty GridLinesVisibilityProperty =
            DependencyProperty.Register(
                "GridLinesVisibility",
                typeof(DataGridGridLinesVisibility),
                typeof(DataGrid),
                new FrameworkPropertyMetadata(DataGridGridLinesVisibility.All, new PropertyChangedCallback(OnNotifyGridLinePropertyChanged)));

        /// <summary>
        ///     Specifies the visibility of the DataGrid's grid lines
        /// </summary>
        public DataGridGridLinesVisibility GridLinesVisibility
        {
            get { return (DataGridGridLinesVisibility)GetValue(GridLinesVisibilityProperty); }
            set { SetValue(GridLinesVisibilityProperty, value); }
        }

        /// <summary>
        /// HorizontalGridLinesBrush Dependency Property
        /// </summary>
        public static readonly DependencyProperty HorizontalGridLinesBrushProperty =
            DependencyProperty.Register(
                "HorizontalGridLinesBrush",
                typeof(Brush),
                typeof(DataGrid),
                new FrameworkPropertyMetadata(Brushes.Black, new PropertyChangedCallback(OnNotifyGridLinePropertyChanged)));

        /// <summary>
        /// Specifies the Brush used to draw the horizontal grid lines
        /// </summary>
        public Brush HorizontalGridLinesBrush
        {
            get { return (Brush)GetValue(HorizontalGridLinesBrushProperty); }
            set { SetValue(HorizontalGridLinesBrushProperty, value); }
        }

        /// <summary>
        /// VerticalGridLinesBrush Dependency Property
        /// </summary>
        public static readonly DependencyProperty VerticalGridLinesBrushProperty =
            DependencyProperty.Register(
                "VerticalGridLinesBrush",
                typeof(Brush),
                typeof(DataGrid),
                new FrameworkPropertyMetadata(Brushes.Black, new PropertyChangedCallback(OnNotifyGridLinePropertyChanged)));

        /// <summary>
        /// Specifies the Brush used to draw the vertical grid lines
        /// </summary>
        public Brush VerticalGridLinesBrush
        {
            get { return (Brush)GetValue(VerticalGridLinesBrushProperty); }
            set { SetValue(VerticalGridLinesBrushProperty, value); }
        }

#if GridLineThickness
        /// <summary>
        /// HorizontalGridLineThickness DependencyProperty
        /// </summary>
        public static readonly DependencyProperty HorizontalGridLineThicknessProperty =
                DependencyProperty.Register("HorizontalGridLineThickness", typeof(double), typeof(DataGrid),
                                            new FrameworkPropertyMetadata(1d, new PropertyChangedCallback(OnNotifyGridLinePropertyChanged)));

        /// <summary>
        /// Specifies the thickness of the horizontal grid lines.
        /// </summary>
        public double HorizontalGridLineThickness
        {
            get { return (double)GetValue(HorizontalGridLineThicknessProperty); }
            set { SetValue(HorizontalGridLineThicknessProperty, value); }
        }

        /// <summary>
        /// VerticalGridLineThickness DependencyProperty
        /// </summary>
        public static readonly DependencyProperty VerticalGridLineThicknessProperty =
                    DependencyProperty.Register("VerticalGridLineThickness", typeof(double), typeof(DataGrid),
                                                new FrameworkPropertyMetadata(1d, new PropertyChangedCallback(OnNotifyGridLinePropertyChanged)));


        /// <summary>
        /// Specifies the thickness of the vertical grid lines.
        /// </summary>
        public double VerticalGridLineThickness
        {
            get { return (double)GetValue(VerticalGridLineThicknessProperty); }
            set { SetValue(VerticalGridLineThicknessProperty, value); }
        }

#else
        internal double HorizontalGridLineThickness
        {
            get { return 1.0; }
        }

        internal double VerticalGridLineThickness
        {
            get { return 1.0; }
        }
#endif

        #endregion

        #region Row Generation

        /// <summary>
        ///     Determines if an item is its own container.
        /// </summary>
        /// <param name="item">The item to test.</param>
        /// <returns>true if the item is a DataGridRow, false otherwise.</returns>
        protected override bool IsItemItsOwnContainerOverride(object item)
        {
            return item is DataGridRow;
        }

        /// <summary>
        ///     Instantiates an instance of a container.
        /// </summary>
        /// <returns>A new DataGridRow.</returns>
        protected override DependencyObject GetContainerForItemOverride()
        {
            return new DataGridRow();
        }

        /// <summary>
        ///     Prepares a new container for a given item.
        /// </summary>
        /// <param name="element">The new container.</param>
        /// <param name="item">The item that the container represents.</param>
        protected override void PrepareContainerForItemOverride(DependencyObject element, object item)
        {
            base.PrepareContainerForItemOverride(element, item);

            DataGridRow row = (DataGridRow)element;
            if (row.DataGridOwner != this)
            {
                row.Tracker.StartTracking(ref _rowTrackingRoot);
                if (item == CollectionView.NewItemPlaceholder ||
                    (IsAddingNewItem && item == EditableItems.CurrentAddItem))
                {
                    row.IsNewItem = true;
                }
                else
                {
                    row.ClearValue(DataGridRow.IsNewItemPropertyKey);
                }
                EnsureInternalScrollControls();
                EnqueueNewItemMarginComputation();
            }

            row.PrepareRow(item, this);
            OnLoadingRow(new DataGridRowEventArgs(row));
        }

        /// <summary>
        ///     Clears a container of references.
        /// </summary>
        /// <param name="element">The container being cleared.</param>
        /// <param name="item">The data item that the container represented.</param>
        protected override void ClearContainerForItemOverride(DependencyObject element, object item)
        {
            base.ClearContainerForItemOverride(element, item);

            DataGridRow row = (DataGridRow)element;
            if (row.DataGridOwner == this)
            {
                row.Tracker.StopTracking(ref _rowTrackingRoot);
                row.ClearValue(DataGridRow.IsNewItemPropertyKey);
                EnqueueNewItemMarginComputation();
            }

            OnUnloadingRow(new DataGridRowEventArgs(row));
            row.ClearRow(this);
        }

        /// <summary>
        ///     Propagates the collection changed notification on Columns down to
        ///     each active DataGridRow.
        /// </summary>
        /// <param name="e">The event arguments from the original collection changed event.</param>
        private void UpdateColumnsOnRows(NotifyCollectionChangedEventArgs e)
        {
            ContainerTracking<DataGridRow> tracker = _rowTrackingRoot;
            while (tracker != null)
            {
                tracker.Container.OnColumnsChanged(_columns, e);
                tracker = tracker.Next;
            }
        }

        /// <summary>
        ///     Equivalent of ItemContainerStyle.
        /// </summary>
        /// <remarks>
        ///     If this property has a non-null value, it will override the value
        ///     of ItemContainerStyle.
        /// </remarks>
        public Style RowStyle
        {
            get { return (Style)GetValue(RowStyleProperty); }
            set { SetValue(RowStyleProperty, value); }
        }

        /// <summary>
        ///     DependencyProperty for the RowStyle property.
        /// </summary>
        public static readonly DependencyProperty RowStyleProperty =
            DependencyProperty.Register("RowStyle", typeof(Style), typeof(DataGrid), new FrameworkPropertyMetadata(null, new PropertyChangedCallback(OnRowStyleChanged)));

        private static void OnRowStyleChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            d.CoerceValue(ItemContainerStyleProperty);
        }

        private static object OnCoerceItemContainerStyle(DependencyObject d, object baseValue)
        {
            if (!DataGridHelper.IsDefaultValue(d, DataGrid.RowStyleProperty))
            {
                return d.GetValue(DataGrid.RowStyleProperty);
            }

            return baseValue;
        }

        /// <summary>
        /// Template used to visually indicate an error in row Validation.
        /// </summary>
        public ControlTemplate RowValidationErrorTemplate
        {
            get { return (ControlTemplate)GetValue(RowValidationErrorTemplateProperty); }
            set { SetValue(RowValidationErrorTemplateProperty, value); }
        }

        /// <summary>
        ///     DependencyProperty for the RowValidationErrorTemplate property.
        /// </summary>
        public static readonly DependencyProperty RowValidationErrorTemplateProperty =
            DependencyProperty.Register("RowValidationErrorTemplate", typeof(ControlTemplate), typeof(DataGrid), new FrameworkPropertyMetadata(null, new PropertyChangedCallback(OnNotifyRowPropertyChanged)));

        /// <summary>
        ///     Validation rules that are run on each DataGridRow.  If DataGrid.ItemBindingGroup is used, RowValidationRules is ignored.
        /// </summary>
        public ObservableCollection<ValidationRule> RowValidationRules
        {
            get { return _rowValidationRules; }
        }

        /// <summary>
        ///     Called when the Columns collection changes.
        /// </summary>
        private void OnRowValidationRulesChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            EnsureItemBindingGroup();

            // only update the ItemBindingGroup if it's not user created.
            if (_defaultBindingGroup != null)
            {
                if (object.ReferenceEquals(ItemBindingGroup, _defaultBindingGroup))
                {
                    switch (e.Action)
                    {
                        case NotifyCollectionChangedAction.Add:
                            foreach (ValidationRule rule in e.NewItems)
                            {
                                _defaultBindingGroup.ValidationRules.Add(rule);
                            }

                            break;

                        case NotifyCollectionChangedAction.Remove:
                            foreach (ValidationRule rule in e.OldItems)
                            {
                                _defaultBindingGroup.ValidationRules.Remove(rule);
                            }

                            break;

                        case NotifyCollectionChangedAction.Replace:
                            foreach (ValidationRule rule in e.OldItems)
                            {
                                _defaultBindingGroup.ValidationRules.Remove(rule);
                            }

                            foreach (ValidationRule rule in e.NewItems)
                            {
                                _defaultBindingGroup.ValidationRules.Add(rule);
                            }

                            break;

                        case NotifyCollectionChangedAction.Reset:
                            _defaultBindingGroup.ValidationRules.Clear();
                            break;
                    }
                }
                else
                {
                    _defaultBindingGroup = null;
                }
            }
        }

        /// <summary>
        ///     Equivalent of ItemContainerStyleSelector.
        /// </summary>
        /// <remarks>
        ///     If this property has a non-null value, it will override the value
        ///     of ItemContainerStyleSelector.
        /// </remarks>
        public StyleSelector RowStyleSelector
        {
            get { return (StyleSelector)GetValue(RowStyleSelectorProperty); }
            set { SetValue(RowStyleSelectorProperty, value); }
        }

        /// <summary>
        ///     DependencyProperty for the RowStyleSelector property.
        /// </summary>
        public static readonly DependencyProperty RowStyleSelectorProperty =
            DependencyProperty.Register("RowStyleSelector", typeof(StyleSelector), typeof(DataGrid), new FrameworkPropertyMetadata(null, new PropertyChangedCallback(OnRowStyleSelectorChanged)));

        private static void OnRowStyleSelectorChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            d.CoerceValue(ItemContainerStyleSelectorProperty);
        }

        private static object OnCoerceItemContainerStyleSelector(DependencyObject d, object baseValue)
        {
            if (!DataGridHelper.IsDefaultValue(d, DataGrid.RowStyleSelectorProperty))
            {
                return d.GetValue(DataGrid.RowStyleSelectorProperty);
            }

            return baseValue;
        }

        private static object OnCoerceIsSynchronizedWithCurrentItem(DependencyObject d, object baseValue)
        {
            DataGrid dataGrid = (DataGrid)d;
            if (dataGrid.SelectionUnit == DataGridSelectionUnit.Cell)
            {
                // IsSynchronizedWithCurrentItem makes IsSelected=true on the current row.
                // When SelectionUnit is Cell, we should not allow row selection.
                return false;
            }

            return baseValue;
        }

        /// <summary>
        ///     The default row background brush.
        /// </summary>
        public Brush RowBackground
        {
            get { return (Brush)GetValue(RowBackgroundProperty); }
            set { SetValue(RowBackgroundProperty, value); }
        }

        /// <summary>
        ///     DependencyProperty for RowBackground.
        /// </summary>
        public static readonly DependencyProperty RowBackgroundProperty =
            DependencyProperty.Register("RowBackground", typeof(Brush), typeof(DataGrid), new FrameworkPropertyMetadata(null, new PropertyChangedCallback(OnNotifyRowPropertyChanged)));

        /// <summary>
        ///     The default row background brush for use on every other row.
        /// </summary>
        /// <remarks>
        ///     Setting this property to a non-null value will coerce AlternationCount to 2.
        /// </remarks>
        public Brush AlternatingRowBackground
        {
            get { return (Brush)GetValue(AlternatingRowBackgroundProperty); }
            set { SetValue(AlternatingRowBackgroundProperty, value); }
        }

        /// <summary>
        ///     DependencyProperty for AlternatingRowBackground.
        /// </summary>
        public static readonly DependencyProperty AlternatingRowBackgroundProperty =
            DependencyProperty.Register("AlternatingRowBackground", typeof(Brush), typeof(DataGrid), new FrameworkPropertyMetadata(null, new PropertyChangedCallback(OnNotifyDataGridAndRowPropertyChanged)));

        private static object OnCoerceAlternationCount(DependencyObject d, object baseValue)
        {
            // Only check AlternatingRowBackground if the value isn't already set
            // to something that can use it.
            if (((int)baseValue) < 2)
            {
                DataGrid dataGrid = (DataGrid)d;
                if (dataGrid.AlternatingRowBackground != null)
                {
                    // There is an alternate background, coerce to 2.
                    return 2;
                }
            }

            return baseValue;
        }

        /// <summary>
        ///     The default height of a row.
        /// </summary>
        public double RowHeight
        {
            get { return (double)GetValue(RowHeightProperty); }
            set { SetValue(RowHeightProperty, value); }
        }

        /// <summary>
        ///     The DependencyProperty for RowHeight.
        /// </summary>
        public static readonly DependencyProperty RowHeightProperty =
            DependencyProperty.Register("RowHeight", typeof(double), typeof(DataGrid), new FrameworkPropertyMetadata(double.NaN, new PropertyChangedCallback(OnNotifyCellsPresenterPropertyChanged)));

        /// <summary>
        ///     The default minimum height of a row.
        /// </summary>
        public double MinRowHeight
        {
            get { return (double)GetValue(MinRowHeightProperty); }
            set { SetValue(MinRowHeightProperty, value); }
        }

        /// <summary>
        ///     The DependencyProperty for MinRowHeight.
        /// </summary>
        public static readonly DependencyProperty MinRowHeightProperty =
            DependencyProperty.Register("MinRowHeight", typeof(double), typeof(DataGrid), new FrameworkPropertyMetadata(0.0, new PropertyChangedCallback(OnNotifyCellsPresenterPropertyChanged)));

        /// <summary>
        ///     The NewItemPlaceholder row uses this to set its visibility while it's preparing.
        /// </summary>
        internal Visibility PlaceholderVisibility
        {
            get
            {
                return _placeholderVisibility;
            }
        }

        /// <summary>
        ///     Event that is fired just before a row is prepared.
        /// </summary>
        public event EventHandler<DataGridRowEventArgs> LoadingRow;

        /// <summary>
        ///     Event that is fired just before a row is cleared.
        /// </summary>
        public event EventHandler<DataGridRowEventArgs> UnloadingRow;

        /// <summary>
        ///     Invokes the LoadingRow event
        /// </summary>
        protected virtual void OnLoadingRow(DataGridRowEventArgs e)
        {
            if (LoadingRow != null)
            {
                LoadingRow(this, e);
            }

            var row = e.Row;
            if (row.DetailsVisibility == Visibility.Visible && row.DetailsPresenter != null)
            {
                // Invoke LoadingRowDetails, but only after the details template is expanded (so DetailsElement will be available).
                Dispatcher.CurrentDispatcher.BeginInvoke(new DispatcherOperationCallback(DelayedOnLoadingRowDetails), DispatcherPriority.Loaded, row);
            }
        }

        internal static object DelayedOnLoadingRowDetails(object arg)
        {
            var row = (DataGridRow)arg;
            var dataGrid = row.DataGridOwner;

            if (dataGrid != null)
            {
                dataGrid.OnLoadingRowDetailsWrapper(row);
            }

            return null;
        }

        /// <summary>
        ///     Invokes the UnloadingRow event
        /// </summary>
        protected virtual void OnUnloadingRow(DataGridRowEventArgs e)
        {
            if (UnloadingRow != null)
            {
                UnloadingRow(this, e);
            }

            var row = e.Row;
            OnUnloadingRowDetailsWrapper(row);
        }

        #endregion

        #region Row/Column Headers

        /// <summary>
        ///     The default width of a row header.
        /// </summary>
        public double RowHeaderWidth
        {
            get { return (double)GetValue(RowHeaderWidthProperty); }
            set { SetValue(RowHeaderWidthProperty, value); }
        }

        /// <summary>
        ///     The DependencyProperty for RowHeaderWidth.
        /// </summary>
        public static readonly DependencyProperty RowHeaderWidthProperty =
            DependencyProperty.Register("RowHeaderWidth", typeof(double), typeof(DataGrid), new FrameworkPropertyMetadata(double.NaN, new PropertyChangedCallback(OnNotifyRowHeaderWidthPropertyChanged)));

        /// <summary>
        ///     The actual width of row headers used for binding.  This is computed from the measure of all the visible row headers.
        /// </summary>
        public double RowHeaderActualWidth
        {
            get { return (double)GetValue(RowHeaderActualWidthProperty); }
            internal set { SetValue(RowHeaderActualWidthPropertyKey, value); }
        }

        /// <summary>
        ///     The DependencyPropertyKey for RowHeaderActualWidth.
        /// </summary>
        private static readonly DependencyPropertyKey RowHeaderActualWidthPropertyKey =
            DependencyProperty.RegisterReadOnly("RowHeaderActualWidth", typeof(double), typeof(DataGrid), new FrameworkPropertyMetadata(0.0, new PropertyChangedCallback(OnNotifyRowHeaderPropertyChanged)));

        /// <summary>
        ///     The DependencyProperty for RowHeaderActualWidth.
        /// </summary>
        public static readonly DependencyProperty RowHeaderActualWidthProperty = RowHeaderActualWidthPropertyKey.DependencyProperty;

        /// <summary>
        ///     The default height of a column header.
        /// </summary>
        public double ColumnHeaderHeight
        {
            get { return (double)GetValue(ColumnHeaderHeightProperty); }
            set { SetValue(ColumnHeaderHeightProperty, value); }
        }

        /// <summary>
        ///     The DependencyProperty for ColumnHeaderHeight.
        /// </summary>
        public static readonly DependencyProperty ColumnHeaderHeightProperty =
            DependencyProperty.Register("ColumnHeaderHeight", typeof(double), typeof(DataGrid), new FrameworkPropertyMetadata(double.NaN, OnNotifyColumnHeaderPropertyChanged));

        /// <summary>
        ///     A property that specifies the visibility of the column & row headers.
        /// </summary>
        public DataGridHeadersVisibility HeadersVisibility
        {
            get { return (DataGridHeadersVisibility)GetValue(HeadersVisibilityProperty); }
            set { SetValue(HeadersVisibilityProperty, value); }
        }

        /// <summary>
        ///     The DependencyProperty that represents the HeadersVisibility property.
        /// </summary>
        public static readonly DependencyProperty HeadersVisibilityProperty =
            DependencyProperty.Register("HeadersVisibility", typeof(DataGridHeadersVisibility), typeof(DataGrid), new FrameworkPropertyMetadata(DataGridHeadersVisibility.All));

        /// <summary>
        ///     Updates RowHeaderActualWidth to reflect changes to RowHeaderWidth
        /// </summary>
        private static void OnNotifyRowHeaderWidthPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var dataGrid = ((DataGrid)d);
            var newValue = (double)e.NewValue;

            if (!DoubleUtil.IsNaN(newValue))
            {
                dataGrid.RowHeaderActualWidth = newValue;
            }
            else
            {
                // If we're entering Auto mode we need to reset the RowHeaderActualWidth
                // because the previous explicit value may have been bigger than the Auto width.
                dataGrid.RowHeaderActualWidth = 0.0;
            }

            OnNotifyRowHeaderPropertyChanged(d, e);
        }

        /// <summary>
        /// Resets the RowHeaderActualWidth to 0.0 if in Auto mode
        /// </summary>
        private void ResetRowHeaderActualWidth()
        {
            if (DoubleUtil.IsNaN(RowHeaderWidth))
            {
                RowHeaderActualWidth = 0.0;
            }
        }

        #endregion

        #region Item Associated Properties

        /// <summary>
        ///     Sets the specified item's DetailsVisibility.
        /// </summary>
        /// <remarks>
        ///     This is useful when a DataGridRow may not currently exists to set DetailsVisibility on.
        /// </remarks>
        /// <param name="item">The item that will have its DetailsVisibility set.</param>
        /// <param name="detailsVisibility">The Visibility that the item's details should get.</param>
        public void SetDetailsVisibilityForItem(object item, Visibility detailsVisibility)
        {
            _itemAttachedStorage.SetValue(item, DataGridRow.DetailsVisibilityProperty, detailsVisibility);

            var row = (DataGridRow)ItemContainerGenerator.ContainerFromItem(item);
            if (row != null)
            {
                row.DetailsVisibility = detailsVisibility;
            }
        }

        /// <summary>
        ///     Returns the current DetailsVisibility for an item that's in the DataGrid.
        /// </summary>
        /// <param name="item">The item who's DetailsVisibility you would like to get</param>
        /// <returns>The DetailsVisibility associated with the specified item.</returns>
        public Visibility GetDetailsVisibilityForItem(object item)
        {
            object detailsVisibility;
            if (_itemAttachedStorage.TryGetValue(item, DataGridRow.DetailsVisibilityProperty, out detailsVisibility))
            {
                return (Visibility)detailsVisibility;
            }

            var row = (DataGridRow)ItemContainerGenerator.ContainerFromItem(item);
            if (row != null)
            {
                return row.DetailsVisibility;
            }

            // If we dont have a row, we can infer it's Visibility from the current RowDetailsVisibilityMode
            switch (RowDetailsVisibilityMode)
            {
                case DataGridRowDetailsVisibilityMode.VisibleWhenSelected:
                    return SelectedItems.Contains(item) ? Visibility.Visible : Visibility.Collapsed;
                case DataGridRowDetailsVisibilityMode.Visible:
                    return Visibility.Visible;
                default:
                    return Visibility.Collapsed;
            }
        }

        /// <summary>
        ///     Clears the DetailsVisibility for the specified item
        /// </summary>
        /// <param name="item">The item to clear the DetailsVisibility on.</param>
        public void ClearDetailsVisibilityForItem(object item)
        {
            _itemAttachedStorage.ClearValue(item, DataGridRow.DetailsVisibilityProperty);

            var row = (DataGridRow)ItemContainerGenerator.ContainerFromItem(item);
            if (row != null)
            {
                row.ClearValue(DataGridRow.DetailsVisibilityProperty);
            }
        }

        internal DataGridItemAttachedStorage ItemAttachedStorage
        {
            get { return _itemAttachedStorage; }
        }

        /// <summary>
        ///     Determines whether the selection change caused by keyboard input should select a full row (or full rows).
        /// </summary>
        private bool ShouldSelectRowHeader
        {
            get
            {
                return _selectionAnchor != null && SelectedItems.Contains(_selectionAnchor.Value.Item) &&
                       SelectionUnit == DataGridSelectionUnit.CellOrRowHeader &&
                       (Keyboard.Modifiers & ModifierKeys.Shift) == ModifierKeys.Shift;
            }
        }

        #endregion

        #region Style Properties

        /// <summary>
        ///     A style to apply to all cells in the DataGrid.
        /// </summary>
        public Style CellStyle
        {
            get { return (Style)GetValue(CellStyleProperty); }
            set { SetValue(CellStyleProperty, value); }
        }

        /// <summary>
        ///     The DependencyProperty that represents the CellStyle property.
        /// </summary>
        public static readonly DependencyProperty CellStyleProperty =
            DependencyProperty.Register("CellStyle", typeof(Style), typeof(DataGrid), new FrameworkPropertyMetadata(null, new PropertyChangedCallback(OnNotifyColumnAndCellPropertyChanged)));

        /// <summary>
        ///     A style to apply to all column headers in the DataGrid
        /// </summary>
        public Style ColumnHeaderStyle
        {
            get { return (Style)GetValue(ColumnHeaderStyleProperty); }
            set { SetValue(ColumnHeaderStyleProperty, value); }
        }

        /// <summary>
        ///     The DependencyProperty that represents the ColumnHeaderStyle property.
        /// </summary>
        public static readonly DependencyProperty ColumnHeaderStyleProperty =
            DependencyProperty.Register("ColumnHeaderStyle", typeof(Style), typeof(DataGrid), new FrameworkPropertyMetadata(null, new PropertyChangedCallback(OnNotifyColumnAndColumnHeaderPropertyChanged)));

        /// <summary>
        ///     A style to apply to all row headers in the DataGrid
        /// </summary>
        public Style RowHeaderStyle
        {
            get { return (Style)GetValue(RowHeaderStyleProperty); }
            set { SetValue(RowHeaderStyleProperty, value); }
        }

        /// <summary>
        ///     The DependencyProperty that represents the RowHeaderStyle property.
        /// </summary>
        public static readonly DependencyProperty RowHeaderStyleProperty =
            DependencyProperty.Register("RowHeaderStyle", typeof(Style), typeof(DataGrid), new FrameworkPropertyMetadata(null, new PropertyChangedCallback(OnNotifyRowAndRowHeaderPropertyChanged)));

        /// <summary>
        ///     The object representing the Row Header template.
        /// </summary>
        public DataTemplate RowHeaderTemplate
        {
            get { return (DataTemplate)GetValue(RowHeaderTemplateProperty); }
            set { SetValue(RowHeaderTemplateProperty, value); }
        }

        /// <summary>
        ///     The DependencyProperty for the RowHeaderTemplate property.
        /// </summary>
        public static readonly DependencyProperty RowHeaderTemplateProperty =
            DependencyProperty.Register("RowHeaderTemplate", typeof(DataTemplate), typeof(DataGrid), new FrameworkPropertyMetadata(null, new PropertyChangedCallback(OnNotifyRowAndRowHeaderPropertyChanged)));

        /// <summary>
        ///     The object representing the Row Header template selector.
        /// </summary>
        public DataTemplateSelector RowHeaderTemplateSelector
        {
            get { return (DataTemplateSelector)GetValue(RowHeaderTemplateSelectorProperty); }
            set { SetValue(RowHeaderTemplateSelectorProperty, value); }
        }

        /// <summary>
        ///     The DependencyProperty for the RowHeaderTemplateSelector property.
        /// </summary>
        public static readonly DependencyProperty RowHeaderTemplateSelectorProperty =
            DependencyProperty.Register("RowHeaderTemplateSelector", typeof(DataTemplateSelector), typeof(DataGrid), new FrameworkPropertyMetadata(null, new PropertyChangedCallback(OnNotifyRowAndRowHeaderPropertyChanged)));

        /// <summary>
        ///     The default style references this brush to create a thicker border
        ///     around the focused cell.
        /// </summary>
        public static ComponentResourceKey FocusBorderBrushKey
        {
            get { return SystemResourceKey.DataGridFocusBorderBrushKey; }
        }

        /// <summary>
        ///     A converter which converts DataGridHeadersVisibility to VisibilityConverter based on a ConverterParameter.
        /// </summary>
        /// <remarks>
        ///     This can be used in the DataGrid's template to control which parts of the DataGrid are visible for a given DataGridHeadersVisibility.
        /// </remarks>
        public static IValueConverter HeadersVisibilityConverter
        {
            get
            {
                // This is delay created in case the template doesn't use it.
                if (_headersVisibilityConverter == null)
                {
                    _headersVisibilityConverter = new DataGridHeadersVisibilityToVisibilityConverter();
                }

                return _headersVisibilityConverter;
            }
        }

        /// <summary>
        ///     A converter which converts bool to SelectiveScrollingOrientation based on a ConverterParameter.
        /// </summary>
        /// <remarks>
        ///     This can be used in the DataGrid's template to control how the RowDetails selectively scroll based on a bool.
        /// </remarks>
        public static IValueConverter RowDetailsScrollingConverter
        {
            get
            {
                // This is delay created in case the template doesn't use it.
                if (_rowDetailsScrollingConverter == null)
                {
                    _rowDetailsScrollingConverter = new BooleanToSelectiveScrollingOrientationConverter();
                }

                return _rowDetailsScrollingConverter;
            }
        }

        #endregion

        #region Scrolling

        /// <summary>
        ///     Defines the behavior that determines the visibility of horizontal ScrollBars.
        /// </summary>
        public ScrollBarVisibility HorizontalScrollBarVisibility
        {
            get { return (ScrollBarVisibility)GetValue(HorizontalScrollBarVisibilityProperty); }
            set { SetValue(HorizontalScrollBarVisibilityProperty, value); }
        }

        /// <summary>
        ///     The DependencyProperty for the HorizontalScrollBarVisibility property.
        /// </summary>
        public static readonly DependencyProperty HorizontalScrollBarVisibilityProperty = ScrollViewer.HorizontalScrollBarVisibilityProperty.AddOwner(typeof(DataGrid), new FrameworkPropertyMetadata(ScrollBarVisibility.Auto));

        /// <summary>
        ///     Defines the behavior that determines the visibility of vertical ScrollBars.
        /// </summary>
        public ScrollBarVisibility VerticalScrollBarVisibility
        {
            get { return (ScrollBarVisibility)GetValue(VerticalScrollBarVisibilityProperty); }
            set { SetValue(VerticalScrollBarVisibilityProperty, value); }
        }

        /// <summary>
        ///     The DependencyProperty for the HorizontalScrollBarVisibility property.
        /// </summary>
        public static readonly DependencyProperty VerticalScrollBarVisibilityProperty = ScrollViewer.VerticalScrollBarVisibilityProperty.AddOwner(typeof(DataGrid), new FrameworkPropertyMetadata(ScrollBarVisibility.Auto));

        /// <summary>
        ///     Scrolls a row into view.
        /// </summary>
        /// <param name="item">The data item of the row to bring into view.</param>
        public void ScrollIntoView(object item)
        {
            if (item == null)
            {
                throw new ArgumentNullException("item");
            }

            ScrollIntoView(NewItemInfo(item));
        }

        internal void ScrollIntoView(ItemInfo info)
        {
            if (ItemContainerGenerator.Status == GeneratorStatus.ContainersGenerated)
            {
                OnBringItemIntoView(info);
            }
            else
            {
                // The items aren't generated, try at a later time
                Dispatcher.BeginInvoke(DispatcherPriority.Loaded, new DispatcherOperationCallback(OnBringItemIntoView), info);
            }
        }

        /// <summary>
        ///     Scrolls a cell into view.
        /// If column is null then only vertical scroll is performed.
        /// If row is null then only horizontal scroll is performed.
        /// </summary>
        /// <param name="item">The data item row that contains the cell.</param>
        /// <param name="column">The cell's column.</param>
        public void ScrollIntoView(object item, DataGridColumn column)
        {
            ItemInfo info = (item == null) ? null : NewItemInfo(item);
            ScrollIntoView(info, column);
        }

        private void ScrollIntoView(ItemInfo info, DataGridColumn column)
        {
            if (column == null)
            {
                ScrollIntoView(info);
                return;
            }

            if (!column.IsVisible)
            {
                return;
            }

            if (ItemContainerGenerator.Status == GeneratorStatus.ContainersGenerated)
            {
                // Scroll by column only
                if (info == null)
                {
                    ScrollColumnIntoView(column);
                }
                else
                {
                    ScrollCellIntoView(info, column);
                }
            }
            else
            {
                // The items aren't generated, try at a later time
                Dispatcher.BeginInvoke(DispatcherPriority.Loaded, new DispatcherOperationCallback(OnScrollIntoView), new object[] { info, column });
            }
        }

        /// <summary>
        ///     Previous call to ScrollIntoView found that the generator had not finished
        ///     generating cells. This is the callback at Loaded priority when hopefully
        ///     that has occured.
        /// </summary>
        private object OnScrollIntoView(object arg)
        {
            object[] arguments = arg as object[];
            if (arguments != null)
            {
                if (arguments[0] != null)
                {
                    ScrollCellIntoView((ItemInfo)arguments[0], (DataGridColumn)arguments[1]);
                }
                else
                {
                    ScrollColumnIntoView((DataGridColumn)arguments[1]);
                }
            }
            else
            {
                OnBringItemIntoView((ItemInfo)arg);
            }

            return null;
        }

        private void ScrollColumnIntoView(DataGridColumn column)
        {
            if (_rowTrackingRoot != null)
            {
                DataGridRow row = _rowTrackingRoot.Container;
                if (row != null)
                {
                    int columnIndex = _columns.IndexOf(column);
                    row.ScrollCellIntoView(columnIndex);
                }
            }
        }

        // TODO: Consider making a protected virtual so that sub-classes can customize the behavior
        private void ScrollCellIntoView(ItemInfo info, DataGridColumn column)
        {
            Debug.Assert(info != null, "info is null.");
            Debug.Assert(column != null, "column is null.");

            if (!column.IsVisible)
            {
                return;
            }

            // Devirtualize the concerned row if it is not already
            DataGridRow row = ContainerFromItemInfo(info) as DataGridRow;
            if (row == null)
            {
                OnBringItemIntoView(info);
                UpdateLayout();
                row = ContainerFromItemInfo(info) as DataGridRow;
            }
            else
            {
                // Ensures that row is brought into viewport
                row.BringIntoView();

                // There happens to be a subtle implication of calling BringIntoView
                // on the row but then not flushing the ScrollViewer's command queue,
                // which is that any subsequent MakeVisible call gets coalesced over
                // this one. Eg. In cases like Ctrl+Home/Key.Up/Key.Down there is
                // a synchronous call to give keyboard focus to the cell which causes
                // a MakeVisible to be enqueued for the cell. This essentially eats up
                // the previous MakeVisible on the row and thus the row never entirely
                // comes into view. Some margin between the cell and the row bounds
                // is clipped off screen. This issue was discovered building the
                // DrtContainerVirtualization suite for DRTControls where we accurately
                // verify Arrange positions.
                UpdateLayout();
            }

            // Use the row to scroll cell into view.
            if (row != null)
            {
                int columnIndex = _columns.IndexOf(column);
                row.ScrollCellIntoView(columnIndex);
            }
        }

        /// <summary>
        ///     Called when IsMouseCaptured changes on this element.
        /// </summary>
        protected override void OnIsMouseCapturedChanged(DependencyPropertyChangedEventArgs e)
        {
            if (!IsMouseCaptured)
            {
                // When capture is lost, stop auto-scrolling
                StopAutoScroll();
            }

            base.OnIsMouseCapturedChanged(e);
        }

        /// <summary>
        ///     Begins a timer that will periodically scroll and select.
        /// </summary>
        private void StartAutoScroll()
        {
            if (_autoScrollTimer == null)
            {
                _hasAutoScrolled = false;

                // Same priority as ListBox. Currently choosing SystemIdle over ApplicationIdle since the layout
                // manger will do some work (sometimes) at ApplicationIdle.
                _autoScrollTimer = new DispatcherTimer(DispatcherPriority.SystemIdle);
                _autoScrollTimer.Interval = AutoScrollTimeout;
                _autoScrollTimer.Tick += new EventHandler(OnAutoScrollTimeout);
                _autoScrollTimer.Start();
            }
        }

        /// <summary>
        ///     Stops the timer that controls auto-scrolling.
        /// </summary>
        private void StopAutoScroll()
        {
            if (_autoScrollTimer != null)
            {
                _autoScrollTimer.Stop();
                _autoScrollTimer = null;
                _hasAutoScrolled = false;
            }
        }

        /// <summary>
        ///     The callback when the auto-scroll timer ticks.
        /// </summary>
        private void OnAutoScrollTimeout(object sender, EventArgs e)
        {
            if (Mouse.LeftButton == MouseButtonState.Pressed)
            {
                DoAutoScroll();
            }
            else
            {
                StopAutoScroll();
            }
        }

        /// <summary>
        ///     Based on the mouse position relative to the rows and cells,
        ///     scrolls and selects rows and/or cells.
        /// </summary>
        /// <returns>true if a scroll and select was attempted. false otherwise.</returns>
        private new bool DoAutoScroll()
        {
            Debug.Assert(_isDraggingSelection, "DoAutoScroll should only be called when dragging selection.");

            RelativeMousePositions position = RelativeMousePosition;
            if (position != RelativeMousePositions.Over)
            {
                // Get the cell that is nearest the mouse position and is
                // not being clipped by the ScrollViewer.
                DataGridCell cell = GetCellNearMouse();
                if (cell != null)
                {
                    DataGridColumn column = cell.Column;
                    ItemInfo info = ItemInfoFromContainer(cell.RowOwner);

                    // Based on the position of the mouse relative to the field
                    // of cells, choose the cell that is towards the mouse.
                    // Note: This assumes a grid layout.
                    if (IsMouseToLeft(position))
                    {
                        int columnIndex = column.DisplayIndex;
                        if (columnIndex > 0)
                        {
                            column = ColumnFromDisplayIndex(columnIndex - 1);
                        }
                    }
                    else if (IsMouseToRight(position))
                    {
                        int columnIndex = column.DisplayIndex;
                        if (columnIndex < (_columns.Count - 1))
                        {
                            column = ColumnFromDisplayIndex(columnIndex + 1);
                        }
                    }

                    if (IsMouseAbove(position))
                    {
                        int rowIndex = info.Index;
                        if (rowIndex > 0)
                        {
                            info = ItemInfoFromIndex(rowIndex - 1);
                        }
                    }
                    else if (IsMouseBelow(position))
                    {
                        int rowIndex = info.Index;
                        if (rowIndex < (Items.Count - 1))
                        {
                            info = ItemInfoFromIndex(rowIndex + 1);
                        }
                    }

                    if (_isRowDragging)
                    {
                        // Perform a row header drag-select
                        OnBringItemIntoView(info);
                        DataGridRow row = (DataGridRow)ItemContainerGenerator.ContainerFromIndex(info.Index);
                        if (row != null)
                        {
                            _hasAutoScrolled = true;
                            HandleSelectionForRowHeaderAndDetailsInput(row, /* startDragging = */ false);
                            SetCurrentItem(info.Item);
                            return true;
                        }
                    }
                    else
                    {
                        // Perform a cell drag-select
                        ScrollCellIntoView(info, column);
                        cell = TryFindCell(info, column);
                        if (cell != null)
                        {
                            _hasAutoScrolled = true;
                            HandleSelectionForCellInput(cell, /* startDragging = */ false, /* allowsExtendSelect = */ true, /* allowsMinimalSelect = */ true);
                            cell.Focus();
                            return true;
                        }
                    }
                }
            }

            return false;
        }

        /// <summary>
        ///     Prevents the ScrollViewer from handling keyboard input.
        /// </summary>
        protected internal override bool HandlesScrolling
        {
            get { return true; }
        }

        /// <summary>
        ///     Workaround for not having access to ItemsControl.ItemsHost.
        /// </summary>
        internal Panel InternalItemsHost
        {
            get { return _internalItemsHost; }
            set
            {
                if (_internalItemsHost != value)
                {
                    _internalItemsHost = value;
                    if (_internalItemsHost != null)
                    {
                        DetermineItemsHostStarBehavior();
                        EnsureInternalScrollControls();
                    }
                }
            }
        }

        /// <summary>
        ///     Workaround for not having access to ItemsControl.ScrollHost.
        /// </summary>
        internal ScrollViewer InternalScrollHost
        {
            get
            {
                EnsureInternalScrollControls();
                return _internalScrollHost;
            }
        }

        /// <summary>
        ///     Workaround for not having access to ScrollContentPresenter
        /// </summary>
        internal ScrollContentPresenter InternalScrollContentPresenter
        {
            get
            {
                EnsureInternalScrollControls();
                return _internalScrollContentPresenter;
            }
        }

        private void DetermineItemsHostStarBehavior()
        {
            VirtualizingStackPanel panel = _internalItemsHost as VirtualizingStackPanel;
            if (panel != null)
            {
                panel.IgnoreMaxDesiredSize = InternalColumns.HasVisibleStarColumns;
            }
        }

        /// <summary>
        ///     Helper method which ensures the initialization of scroll controls.
        /// </summary>
        private void EnsureInternalScrollControls()
        {
            if (_internalScrollContentPresenter == null)
            {
                if (_internalItemsHost != null)
                {
                    _internalScrollContentPresenter = DataGridHelper.FindVisualParent<ScrollContentPresenter>(_internalItemsHost);
                }
                else if (_rowTrackingRoot != null)
                {
                    DataGridRow row = _rowTrackingRoot.Container;
                    _internalScrollContentPresenter = DataGridHelper.FindVisualParent<ScrollContentPresenter>(row);
                }
                if (_internalScrollContentPresenter != null)
                {
                    _internalScrollContentPresenter.SizeChanged += new SizeChangedEventHandler(OnInternalScrollContentPresenterSizeChanged);
                }
            }

            if (_internalScrollHost == null)
            {
                if (_internalItemsHost != null)
                {
                    _internalScrollHost = DataGridHelper.FindVisualParent<ScrollViewer>(_internalItemsHost);
                }
                else if (_rowTrackingRoot != null)
                {
                    DataGridRow row = _rowTrackingRoot.Container;
                    _internalScrollHost = DataGridHelper.FindVisualParent<ScrollViewer>(row);
                }
                if (_internalScrollHost != null)
                {
                    Binding horizontalOffsetBinding = new Binding("ContentHorizontalOffset");
                    horizontalOffsetBinding.Source = _internalScrollHost;
                    SetBinding(HorizontalScrollOffsetProperty, horizontalOffsetBinding);
                }
            }
        }

        /// <summary>
        ///     Helper method which cleans up the internal scroll controls.
        /// </summary>
        private void CleanUpInternalScrollControls()
        {
            BindingOperations.ClearBinding(this, HorizontalScrollOffsetProperty);
            _internalScrollHost = null;
            if (_internalScrollContentPresenter != null)
            {
                _internalScrollContentPresenter.SizeChanged -= new SizeChangedEventHandler(OnInternalScrollContentPresenterSizeChanged);
                _internalScrollContentPresenter = null;
            }
        }

        /// <summary>
        ///     Size changed handler for InteralScrollContentPresenter.
        /// </summary>
        private void OnInternalScrollContentPresenterSizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (_internalScrollContentPresenter != null &&
                !_internalScrollContentPresenter.CanContentScroll)
            {
                OnViewportSizeChanged(e.PreviousSize, e.NewSize);
            }
        }

        /// <summary>
        ///     Helper method which enqueues a viewport width change
        ///     request to Dispatcher if needed.
        /// </summary>
        internal void OnViewportSizeChanged(Size oldSize, Size newSize)
        {
            if (!InternalColumns.ColumnWidthsComputationPending)
            {
                double widthChange = newSize.Width - oldSize.Width;
                if (!DoubleUtil.AreClose(widthChange, 0.0))
                {
                    _finalViewportWidth = newSize.Width;
                    if (!_viewportWidthChangeNotificationPending)
                    {
                        _originalViewportWidth = oldSize.Width;
                        Dispatcher.BeginInvoke(new DispatcherOperationCallback(OnDelayedViewportWidthChanged), DispatcherPriority.Loaded, this);
                        _viewportWidthChangeNotificationPending = true;
                    }
                }
            }
        }

        /// <summary>
        ///     Dispatcher callback method for Viewport width change
        ///     which propagates the notification if needed.
        /// </summary>
        private object OnDelayedViewportWidthChanged(object args)
        {
            if (!_viewportWidthChangeNotificationPending)
            {
                return null;
            }

            double widthChange = _finalViewportWidth - _originalViewportWidth;
            if (!DoubleUtil.AreClose(widthChange, 0.0))
            {
                NotifyPropertyChanged(this,
                    "ViewportWidth",
                    new DependencyPropertyChangedEventArgs(),
                    DataGridNotificationTarget.CellsPresenter | DataGridNotificationTarget.ColumnHeadersPresenter | DataGridNotificationTarget.ColumnCollection);

                double totalAvailableWidth = _finalViewportWidth;
                totalAvailableWidth -= CellsPanelHorizontalOffset;
                InternalColumns.RedistributeColumnWidthsOnAvailableSpaceChange(widthChange, totalAvailableWidth);
            }
            _viewportWidthChangeNotificationPending = false;
            return null;
        }

        internal void OnHasVisibleStarColumnsChanged()
        {
            DetermineItemsHostStarBehavior();
        }

        /// <summary>
        ///     Dependency property which would be bound to ContentHorizontalOffset
        ///     property of the ScrollViewer.
        /// </summary>
        internal static readonly DependencyProperty HorizontalScrollOffsetProperty =
            DependencyProperty.Register(
                "HorizontalScrollOffset",
                typeof(double),
                typeof(DataGrid),
                new FrameworkPropertyMetadata(0d, OnNotifyHorizontalOffsetPropertyChanged));

        /// <summary>
        ///     The HorizontalOffset of the scroll viewer
        /// </summary>
        internal double HorizontalScrollOffset
        {
            get
            {
                return (double)GetValue(HorizontalScrollOffsetProperty);
            }
        }

        #endregion

        #region Editing Commands

        /// <summary>
        ///     The command to fire and allow to route to the DataGrid in order to indicate that the
        ///     current cell or row should begin editing.
        /// </summary>
        public static readonly RoutedCommand BeginEditCommand = new RoutedCommand("BeginEdit", typeof(DataGrid));

        /// <summary>
        ///     The command to fire and allow to route to the DataGrid in order to indicate that the
        ///     current cell or row should commit any pending changes and exit edit mode.
        /// </summary>
        public static readonly RoutedCommand CommitEditCommand = new RoutedCommand("CommitEdit", typeof(DataGrid));

        /// <summary>
        ///     The command to fire and allow to route to the DataGrid in order to indicate that the
        ///     current cell or row should purge any pending changes and revert to the state it was
        ///     in before BeginEdit.
        /// </summary>
        public static readonly RoutedCommand CancelEditCommand = new RoutedCommand("CancelEdit", typeof(DataGrid));

        /// <summary>
        ///     A command that, when invoked, will delete the current row.
        /// </summary>
        public static RoutedUICommand DeleteCommand
        {
            get
            {
                return ApplicationCommands.Delete;
            }
        }

        private static void OnCanExecuteBeginEdit(object sender, CanExecuteRoutedEventArgs e)
        {
            ((DataGrid)sender).OnCanExecuteBeginEdit(e);
        }

        private static void OnExecutedBeginEdit(object sender, ExecutedRoutedEventArgs e)
        {
            ((DataGrid)sender).OnExecutedBeginEdit(e);
        }

        /// <summary>
        ///     Invoked to determine if the BeginEdit command can be executed.
        /// </summary>
        protected virtual void OnCanExecuteBeginEdit(CanExecuteRoutedEventArgs e)
        {
            bool canExecute = !IsReadOnly && (CurrentCellContainer != null) && !IsEditingCurrentCell && !IsCurrentCellReadOnly && !HasCellValidationError;

            if (canExecute && HasRowValidationError)
            {
                DataGridCell cellContainer = GetEventCellOrCurrentCell(e);
                if (cellContainer != null)
                {
                    object rowItem = cellContainer.RowDataItem;

                    // When there is a validation error, only allow editing on that row
                    canExecute = IsAddingOrEditingRowItem(rowItem);
                }
                else
                {
                    // Don't allow entering edit mode when there is a pending validation error
                    canExecute = false;
                }
            }

            if (canExecute)
            {
                e.CanExecute = true;
                e.Handled = true;
            }
            else
            {
                e.ContinueRouting = true;
            }
        }

        /// <summary>
        ///     Invoked when the BeginEdit command is executed.
        /// </summary>
        protected virtual void OnExecutedBeginEdit(ExecutedRoutedEventArgs e)
        {
            DataGridCell cell = CurrentCellContainer;
            if ((cell != null) && !cell.IsReadOnly && !cell.IsEditing)
            {
                bool addedPlaceholder = false;
                bool deselectedPlaceholder = false;
                bool reselectPlaceholderCells = false;
                List<int> columnIndexRanges = null;
                int newItemIndex = -1;
                object newItem = null;
                bool placeholderAtBeginning = (EditableItems.NewItemPlaceholderPosition == NewItemPlaceholderPosition.AtBeginning);

                if (IsNewItemPlaceholder(cell.RowDataItem))
                {
                    // If editing the new item placeholder, then create a new item and edit that instead.
                    if (SelectedItems.Contains(CollectionView.NewItemPlaceholder))
                    {
                        // Unselect the NewItemPlaceholder and select the new row
                        UnselectItem(NewItemInfo(CollectionView.NewItemPlaceholder));
                        deselectedPlaceholder = true;
                    }
                    else
                    {
                        // Cells will automatically unselect when the new item placeholder is removed, but we
                        // should reselect them on the new item.
                        newItemIndex = ItemContainerGenerator.IndexFromContainer(cell.RowOwner);
                        reselectPlaceholderCells = ((newItemIndex >= 0) && _selectedCells.Intersects(newItemIndex, out columnIndexRanges));
                    }

                    newItem = AddNewItem();
                    SetCurrentCellToNewItem(newItem);   // Puts focus on the added row
                    cell = CurrentCellContainer;
                    if (CurrentCellContainer == null)
                    {
                        // CurrentCellContainer becomes null if focus moves out of the datagrid
                        // Calling UpdateLayout instantiates the CurrentCellContainer
                        UpdateLayout();
                        cell = CurrentCellContainer;
                        if ((cell != null) && !cell.IsKeyboardFocusWithin)
                        {
                            cell.Focus();
                        }
                    }

                    if (deselectedPlaceholder)
                    {
                        // Re-select the new item if the placeholder was selected before
                        SelectItem(NewItemInfo(newItem));
                    }
                    else if (reselectPlaceholderCells)
                    {
                        // Re-select placeholder cells if they were selected before
                        using (UpdateSelectedCells())
                        {
                            int rowIndex = newItemIndex;

                            // When the placeholder is at the beginning, we don't hide it, so those cells need to be unselected.
                            // The cells to select are also now one row below.
                            if (placeholderAtBeginning)
                            {
                                _selectedCells.RemoveRegion(newItemIndex, 0, 1, Columns.Count);
                                rowIndex++;
                            }

                            for (int i = 0, count = columnIndexRanges.Count; i < count; i += 2)
                            {
                                _selectedCells.AddRegion(rowIndex, columnIndexRanges[i], 1, columnIndexRanges[i + 1]);
                            }
                        }
                    }

                    addedPlaceholder = true;
                }

                RoutedEventArgs editingEventArgs = e.Parameter as RoutedEventArgs;
                DataGridBeginningEditEventArgs beginningEditEventArgs = null;

                if (cell != null)
                {
                    // Give the callback an opportunity to cancel edit mode
                    beginningEditEventArgs = new DataGridBeginningEditEventArgs(cell.Column, cell.RowOwner, editingEventArgs);
                    OnBeginningEdit(beginningEditEventArgs);
                }

                if ((cell == null) || beginningEditEventArgs.Cancel)
                {
                    // If CurrentCellContainer is null then cancel editing
                    if (deselectedPlaceholder)
                    {
                        // If the new item placeholder was deselected and the new item was selected,
                        // de-select the new item. Selecting the new item placeholder comes at the end.
                        // This is to accomodate the scenario where the new item placeholder only appears
                        // when not editing a new item.
                        UnselectItem(NewItemInfo(newItem));
                    }
                    else if (reselectPlaceholderCells && placeholderAtBeginning)
                    {
                        // When the placeholder is at the beginning, we need to unselect the added item cells.
                        _selectedCells.RemoveRegion(newItemIndex + 1, 0, 1, Columns.Count);
                    }

                    if (addedPlaceholder)
                    {
                        // The edit was canceled, cancel the new item
                        CancelRowItem();

                        // Display the new item placeholder again
                        UpdateNewItemPlaceholder(/* isAddingNewItem = */ false);

                        // Put focus back on the placeholder
                        SetCurrentItemToPlaceholder();
                    }

                    if (deselectedPlaceholder)
                    {
                        // If the new item placeholder was deselected, then select it again.
                        SelectItem(NewItemInfo(CollectionView.NewItemPlaceholder));
                    }
                    else if (reselectPlaceholderCells)
                    {
                        for (int i = 0, count = columnIndexRanges.Count; i < count; i += 2)
                        {
                            _selectedCells.AddRegion(newItemIndex, columnIndexRanges[i], 1, columnIndexRanges[i + 1]);
                        }
                    }
                }
                else
                {
                    if (!addedPlaceholder && !IsEditingRowItem)
                    {
                        EditRowItem(cell.RowDataItem);

                        var bindingGroup = cell.RowOwner.BindingGroup;
                        if (bindingGroup != null)
                        {
                            bindingGroup.BeginEdit();
                        }

                        _editingRowInfo = ItemInfoFromContainer(cell.RowOwner);
                    }

                    cell.BeginEdit(editingEventArgs);
                    cell.RowOwner.IsEditing = true;

                    // Create a CellAutomationValueHolder object that has a binding to the content of the CurrentCell being  edited.
                    // This is required to raise PropertyChanged AutomationEvent when cell is being edited manually.
                    EnsureCellAutomationValueHolder(cell);
                }
            }

            // CancelEdit and CommitEdit rely on IsAddingNewItem and IsEditingRowItem
            CommandManager.InvalidateRequerySuggested();

            e.Handled = true;
        }

        private static void OnCanExecuteCommitEdit(object sender, CanExecuteRoutedEventArgs e)
        {
            ((DataGrid)sender).OnCanExecuteCommitEdit(e);
        }

        private static void OnExecutedCommitEdit(object sender, ExecutedRoutedEventArgs e)
        {
            ((DataGrid)sender).OnExecutedCommitEdit(e);
        }

        private DataGridCell GetEventCellOrCurrentCell(RoutedEventArgs e)
        {
            // If the command routed through a cell, then use that cell. Otherwise, use the current cell.
            UIElement source = e.OriginalSource as UIElement;
            return ((source == this) || (source == null)) ? CurrentCellContainer : DataGridHelper.FindVisualParent<DataGridCell>(source);
        }

        private bool CanEndEdit(CanExecuteRoutedEventArgs e, bool commit)
        {
            DataGridCell cellContainer = GetEventCellOrCurrentCell(e);
            if (cellContainer == null)
            {
                // If there is no cell, then nothing can be determined. So, no edit could end.
                return false;
            }

            DataGridEditingUnit editingUnit = GetEditingUnit(e.Parameter);
            IEditableCollectionView editableItems = EditableItems;
            object rowItem = cellContainer.RowDataItem;

            // Check that there is an appropriate pending add or edit.
            // - If any cell is in edit mode
            // - OR If the editing unit is row AND one of:
            //   - There is a pending add OR
            //   - There is a pending edit
            return cellContainer.IsEditing ||
                   (!HasCellValidationError &&
                   IsAddingOrEditingRowItem(editingUnit, rowItem));
        }

        /// <summary>
        ///     Invoked to determine if the CommitEdit command can be executed.
        /// </summary>
        protected virtual void OnCanExecuteCommitEdit(CanExecuteRoutedEventArgs e)
        {
            if (CanEndEdit(e, /* commit = */ true))
            {
                e.CanExecute = true;
                e.Handled = true;
            }
            else
            {
                e.ContinueRouting = true;
            }
        }

        /// <summary>
        ///     Invoked when the CommitEdit command is executed.
        /// </summary>
        protected virtual void OnExecutedCommitEdit(ExecutedRoutedEventArgs e)
        {
            DataGridCell cell = CurrentCellContainer;
            bool validationPassed = true;
            if (cell != null)
            {
                DataGridEditingUnit editingUnit = GetEditingUnit(e.Parameter);

                bool eventCanceled = false;
                if (cell.IsEditing)
                {
                    DataGridCellEditEndingEventArgs cellEditEndingEventArgs = new DataGridCellEditEndingEventArgs(cell.Column, cell.RowOwner, cell.EditingElement, DataGridEditAction.Commit);
                    OnCellEditEnding(cellEditEndingEventArgs);

                    eventCanceled = cellEditEndingEventArgs.Cancel;
                    if (!eventCanceled)
                    {
                        validationPassed = cell.CommitEdit();
                        HasCellValidationError = !validationPassed;
                        UpdateCellAutomationValueHolder(cell);
                    }
                }

                // Consider commiting the row if:
                // 1. Validation passed on the cell or no cell was in edit mode.
                // 2. A cell in edit mode didn't have it's ending edit event canceled.
                // 3. The row is being edited or added and being targeted directly.
                if (validationPassed &&
                    !eventCanceled &&
                    IsAddingOrEditingRowItem(editingUnit, cell.RowDataItem))
                {
                    DataGridRowEditEndingEventArgs rowEditEndingEventArgs = new DataGridRowEditEndingEventArgs(cell.RowOwner, DataGridEditAction.Commit);
                    OnRowEditEnding(rowEditEndingEventArgs);

                    if (!rowEditEndingEventArgs.Cancel)
                    {
                        var bindingGroup = cell.RowOwner.BindingGroup;
                        if (bindingGroup != null)
                        {
                            // CommitEdit will invoke the bindingGroup's ValidationRule's, so we need to make sure that all of the BindingExpressions
                            // have already registered with the BindingGroup.  Synchronously flushing the Dispatcher to DataBind priority lets us ensure this.
                            // Had we used BeginInvoke instead, IsEditing would not reflect the correct value.
                            Dispatcher.Invoke(new DispatcherOperationCallback(DoNothing), DispatcherPriority.DataBind, bindingGroup);
                            validationPassed = bindingGroup.CommitEdit();
                        }

                        HasRowValidationError = !validationPassed;
                        if (validationPassed)
                        {
                            CommitRowItem();
                        }
                    }
                }

                if (validationPassed)
                {
                    // Update the state of row editing
                    UpdateRowEditing(cell);

                    if (!cell.RowOwner.IsEditing)
                    {
                        ReleaseCellAutomationValueHolders();
                    }
                }

                // CancelEdit and CommitEdit rely on IsAddingNewItem and IsEditingRowItem
                CommandManager.InvalidateRequerySuggested();
            }

            e.Handled = true;
        }

        /// <summary>
        /// This is a helper method used to flush the dispatcher down to DataBind priority so that the bindingGroup will be ready for CommitEdit.
        /// </summary>
        /// <param name="arg"></param>
        /// <returns></returns>
        private static object DoNothing(object arg)
        {
            return null;
        }

        private DataGridEditingUnit GetEditingUnit(object parameter)
        {
            // If the parameter contains a DataGridEditingUnit, then use it.
            // Otherwise, choose Cell if a cell is currently being edited, or Row if not.
            return ((parameter != null) && (parameter is DataGridEditingUnit)) ?
                    (DataGridEditingUnit)parameter :
                    IsEditingCurrentCell ? DataGridEditingUnit.Cell : DataGridEditingUnit.Row;
        }

        /// <summary>
        ///     Raised just before row editing is ended.
        ///     Gives handlers the opportunity to cancel the operation.
        /// </summary>
        public event EventHandler<DataGridRowEditEndingEventArgs> RowEditEnding;

        /// <summary>
        ///     Called just before row editing is ended.
        ///     Gives subclasses the opportunity to cancel the operation.
        /// </summary>
        protected virtual void OnRowEditEnding(DataGridRowEditEndingEventArgs e)
        {
            if (RowEditEnding != null)
            {
                RowEditEnding(this, e);
            }

            if (AutomationPeer.ListenerExists(AutomationEvents.InvokePatternOnInvoked))
            {
                DataGridAutomationPeer peer = DataGridAutomationPeer.FromElement(this) as DataGridAutomationPeer;
                if (peer != null)
                {
                    peer.RaiseAutomationRowInvokeEvents(e.Row);
                }
            }
        }

        /// <summary>
        ///     Raised just before cell editing is ended.
        ///     Gives handlers the opportunity to cancel the operation.
        /// </summary>
        public event EventHandler<DataGridCellEditEndingEventArgs> CellEditEnding;

        /// <summary>
        ///     Called just before cell editing is ended.
        ///     Gives subclasses the opportunity to cancel the operation.
        /// </summary>
        protected virtual void OnCellEditEnding(DataGridCellEditEndingEventArgs e)
        {
            if (CellEditEnding != null)
            {
                CellEditEnding(this, e);
            }

            if (AutomationPeer.ListenerExists(AutomationEvents.InvokePatternOnInvoked))
            {
                DataGridAutomationPeer peer = DataGridAutomationPeer.FromElement(this) as DataGridAutomationPeer;
                if (peer != null)
                {
                    peer.RaiseAutomationCellInvokeEvents(e.Column, e.Row);
                }
            }
        }

        private static void OnCanExecuteCancelEdit(object sender, CanExecuteRoutedEventArgs e)
        {
            ((DataGrid)sender).OnCanExecuteCancelEdit(e);
        }

        private static void OnExecutedCancelEdit(object sender, ExecutedRoutedEventArgs e)
        {
            ((DataGrid)sender).OnExecutedCancelEdit(e);
        }

        /// <summary>
        ///     Invoked to determine if the CancelEdit command can be executed.
        /// </summary>
        protected virtual void OnCanExecuteCancelEdit(CanExecuteRoutedEventArgs e)
        {
            if (CanEndEdit(e, /* commit = */ false))
            {
                e.CanExecute = true;
                e.Handled = true;
            }
            else
            {
                e.ContinueRouting = true;
            }
        }

        /// <summary>
        ///     Invoked when the CancelEdit command is executed.
        /// </summary>
        protected virtual void OnExecutedCancelEdit(ExecutedRoutedEventArgs e)
        {
            DataGridCell cell = CurrentCellContainer;
            if (cell != null)
            {
                DataGridEditingUnit editingUnit = GetEditingUnit(e.Parameter);

                bool eventCanceled = false;
                if (cell.IsEditing)
                {
                    DataGridCellEditEndingEventArgs cellEditEndingEventArgs = new DataGridCellEditEndingEventArgs(cell.Column, cell.RowOwner, cell.EditingElement, DataGridEditAction.Cancel);
                    OnCellEditEnding(cellEditEndingEventArgs);

                    eventCanceled = cellEditEndingEventArgs.Cancel;
                    if (!eventCanceled)
                    {
                        cell.CancelEdit();
                        HasCellValidationError = false;
                        UpdateCellAutomationValueHolder(cell);
                    }
                }

                if (!eventCanceled &&
                    IsAddingOrEditingRowItem(editingUnit, cell.RowDataItem))
                {
                    bool cancelAllowed = true;

                    DataGridRowEditEndingEventArgs rowEditEndingEventArgs = new DataGridRowEditEndingEventArgs(cell.RowOwner, DataGridEditAction.Cancel);
                    OnRowEditEnding(rowEditEndingEventArgs);
                    cancelAllowed = !rowEditEndingEventArgs.Cancel;

                    if (cancelAllowed)
                    {
                        var bindingGroup = cell.RowOwner.BindingGroup;
                        if (bindingGroup != null)
                        {
                            bindingGroup.CancelEdit();
                        }

                        CancelRowItem();
                    }
                }

                // Update the state of row editing
                UpdateRowEditing(cell);

                if (!cell.RowOwner.IsEditing)
                {
                    // Allow the user to cancel the row and avoid being locked to that row.
                    // If the row is still not valid, it means that the source data is already
                    // invalid, and that is OK.
                    HasRowValidationError = false;

                    ReleaseCellAutomationValueHolders();
                }

                // CancelEdit and CommitEdit rely on IsAddingNewItem and IsEditingRowItem
                CommandManager.InvalidateRequerySuggested();
            }

            e.Handled = true;
        }

        private static void OnCanExecuteDelete(object sender, CanExecuteRoutedEventArgs e)
        {
            ((DataGrid)sender).OnCanExecuteDelete(e);
        }

        private static void OnExecutedDelete(object sender, ExecutedRoutedEventArgs e)
        {
            ((DataGrid)sender).OnExecutedDelete(e);
        }

        /// <summary>
        ///     Invoked to determine if the Delete command can be executed.
        /// </summary>
        protected virtual void OnCanExecuteDelete(CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = CanUserDeleteRows &&  // User is allowed to delete
                (DataItemsSelected > 0) &&       // There is a selection
                ((_currentCellContainer == null) || !_currentCellContainer.IsEditing); // Not editing a cell
            e.Handled = true;
        }

        /// <summary>
        ///     Invoked when the Delete command is executed.
        /// </summary>
        protected virtual void OnExecutedDelete(ExecutedRoutedEventArgs e)
        {
            if (DataItemsSelected > 0)
            {
                bool shouldDelete = false;
                bool isEditingRowItem = IsEditingRowItem;
                if (isEditingRowItem || IsAddingNewItem)
                {
                    // If editing or adding a row, cancel that edit.
                    if (CancelEdit(DataGridEditingUnit.Row) && isEditingRowItem)
                    {
                        // If adding, we're done. If editing, then an actual delete
                        // needs to happen.
                        shouldDelete = true;
                    }
                }
                else
                {
                    // There is no pending edit, just delete.
                    shouldDelete = true;
                }

                if (shouldDelete)
                {
                    // Normally, the current item will be within the selection,
                    // determine a new item to select once the items are removed.
                    int numSelected = SelectedItems.Count;
                    int indexToSelect = -1;
                    ItemInfo currentInfo = CurrentInfo;

                    // The current item is in the selection
                    if (SelectedItems.Contains(currentInfo.Item))
                    {
                        // Choose the smaller index between the anchor and the current item
                        // as the index to select after the items are removed.
                        indexToSelect = currentInfo.Index;
                        if (_selectionAnchor != null)
                        {
                            int anchorIndex = _selectionAnchor.Value.ItemInfo.Index;
                            if ((anchorIndex >= 0) && (anchorIndex < indexToSelect))
                            {
                                indexToSelect = anchorIndex;
                            }
                        }

                        indexToSelect = Math.Min(Items.Count - numSelected - 1, indexToSelect);
                    }

                    // Save off the selected items. The selected items are going to be cleared
                    // first as a performance optimization. When items are removed, they are checked
                    // against the selected items to be removed from that collection. This can be slow
                    // since each item could cause a linear search of the selected items collection.
                    // Since it is known that all of the selected items are going to be deleted, they
                    // can safely be unselected.
                    ArrayList itemsToRemove = new ArrayList(SelectedItems);

                    using (UpdateSelectedCells())
                    {
                        bool alreadyUpdating = IsUpdatingSelectedItems;
                        if (!alreadyUpdating)
                        {
                            BeginUpdateSelectedItems();
                        }

                        try
                        {
                            // Pre-emptively clear the selection lists
                            _selectedCells.ClearFullRows(SelectedItems);
                            SelectedItems.Clear();
                        }
                        finally
                        {
                            if (!alreadyUpdating)
                            {
                                EndUpdateSelectedItems();
                            }
                        }
                    }

                    // We are not going to defer the rest of the selection change due to existing
                    // Selector behavior. When an item is removed from the ItemsSource, the Selector
                    // will immediately remove it from SelectedItems. In this process, it starts a
                    // defer, which asserts because this code would have already started a defer.

                    // Remove the items that are selected
                    for (int i = 0; i < numSelected; i++)
                    {
                        object itemToRemove = itemsToRemove[i];
                        if (itemToRemove != CollectionView.NewItemPlaceholder)
                        {
                            EditableItems.Remove(itemToRemove);
                        }
                    }

                    // Select a new item
                    if (indexToSelect >= 0)
                    {
                        object itemToSelect = Items[indexToSelect];

                        // This should focus the row and bring it into view.
                        SetCurrentItem(itemToSelect);

                        // Since the current cell should be in view, there should be a container
                        DataGridCell cell = CurrentCellContainer;
                        if (cell != null)
                        {
                            _selectionAnchor = null;
                            HandleSelectionForCellInput(cell, /* startDragging = */ false, /* allowsExtendSelect = */ false, /* allowsMinimalSelect = */ false);
                        }
                    }
                }
            }

            e.Handled = true;
        }

        // set the CurrentCell to the newly-added item, using an accurate ItemInfo
        private void SetCurrentCellToNewItem(object newItem)
        {
            ItemInfo info = null;
            int index;

            // usually we know where to find the new item
            switch (EditableItems.NewItemPlaceholderPosition)
            {
                case NewItemPlaceholderPosition.AtEnd:
                    index = Items.Count - 2;
                    if (index >= 0 && ItemsControl.EqualsEx(newItem, Items[index]))
                    {
                        info = ItemInfoFromIndex(index);
                    }
                break;

                case NewItemPlaceholderPosition.AtBeginning:
                    index = 1;
                    if (index < Items.Count && ItemsControl.EqualsEx(newItem, Items[index]))
                    {
                        info = ItemInfoFromIndex(index);
                    }
                break;
            }

            // but if it's not where we expect, find it the hard way
            if (info == null)
            {
                info = ItemInfoFromIndex(Items.IndexOf(newItem));
            }

            // set CurrentCell to the corresponding cell info
            DataGridCellInfo currentCell = CurrentCell;
            currentCell = (info != null) ? new DataGridCellInfo(info, currentCell.Column, this)
                                        : DataGridCellInfo.CreatePossiblyPartialCellInfo(newItem, currentCell.Column, this);
            SetCurrentValueInternal(CurrentCellProperty, currentCell);
        }

        #endregion

        #region Editing

        /// <summary>
        ///     Whether the DataGrid's rows and cells can be placed in edit mode.
        /// </summary>
        public bool IsReadOnly
        {
            get { return (bool)GetValue(IsReadOnlyProperty); }
            set { SetValue(IsReadOnlyProperty, value); }
        }

        /// <summary>
        ///     The DependencyProperty for IsReadOnly.
        /// </summary>
        public static readonly DependencyProperty IsReadOnlyProperty =
            DependencyProperty.Register("IsReadOnly", typeof(bool), typeof(DataGrid), new FrameworkPropertyMetadata(false, new PropertyChangedCallback(OnIsReadOnlyChanged)));

        private static void OnIsReadOnlyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if ((bool)e.NewValue)
            {
                // When going from R/W to R/O, cancel any current edits
                ((DataGrid)d).CancelAnyEdit();
            }

            // re-evalutate the BeginEdit command's CanExecute.
            CommandManager.InvalidateRequerySuggested();

            d.CoerceValue(CanUserAddRowsProperty);
            d.CoerceValue(CanUserDeleteRowsProperty);

            // Affects the IsReadOnly property on cells
            OnNotifyColumnAndCellPropertyChanged(d, e);
        }

        /// <summary>
        ///     The object (or row) that, if not in edit mode, can be edited.
        /// </summary>
        /// <remarks>
        ///     This is the data item for the row that either has or contains focus.
        /// </remarks>
        public object CurrentItem
        {
            get { return (object)GetValue(CurrentItemProperty); }
            set { SetValue(CurrentItemProperty, value); }
        }

        /// <summary>
        ///     The DependencyProperty for CurrentItem.
        /// </summary>
        public static readonly DependencyProperty CurrentItemProperty =
            DependencyProperty.Register("CurrentItem", typeof(object), typeof(DataGrid), new FrameworkPropertyMetadata(null, new PropertyChangedCallback(OnCurrentItemChanged)));

        private static void OnCurrentItemChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            DataGrid dataGrid = (DataGrid)d;
            DataGridCellInfo currentCell = dataGrid.CurrentCell;
            object newItem = e.NewValue;

            if (currentCell.Item != newItem)
            {
                // Update the CurrentCell structure with the new item
                dataGrid.SetCurrentValueInternal(CurrentCellProperty, DataGridCellInfo.CreatePossiblyPartialCellInfo(newItem, currentCell.Column, dataGrid));
            }

            // the RowHeaders need to know when the current item changes so they can update their Visual State.
            OnNotifyRowHeaderPropertyChanged(d, e);
        }

        private void SetCurrentItem(object item)
        {
            // don't set UnsetValue - it's equivalent to ClearValue, which removes data-binding
            if (item == DependencyProperty.UnsetValue)
                item = null;

            SetCurrentValueInternal(CurrentItemProperty, item);
        }

        /// <summary>
        ///     The column of the CurrentItem (row) that corresponds with the current cell.
        /// </summary>
        /// <remarks>
        ///     null indicates that a cell does not have focus. The row may still have focus.
        /// </remarks>
        public DataGridColumn CurrentColumn
        {
            get { return (DataGridColumn)GetValue(CurrentColumnProperty); }
            set { SetValue(CurrentColumnProperty, value); }
        }

        /// <summary>
        ///     The DependencyProperty for CurrentColumn.
        /// </summary>
        public static readonly DependencyProperty CurrentColumnProperty =
            DependencyProperty.Register("CurrentColumn", typeof(DataGridColumn), typeof(DataGrid), new FrameworkPropertyMetadata(null, new PropertyChangedCallback(OnCurrentColumnChanged)));

        private static void OnCurrentColumnChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            DataGrid dataGrid = (DataGrid)d;
            DataGridCellInfo currentCell = dataGrid.CurrentCell;
            DataGridColumn newColumn = (DataGridColumn)e.NewValue;

            if (currentCell.Column != newColumn)
            {
                // Update the CurrentCell structure with the new column
                dataGrid.SetCurrentValueInternal(CurrentCellProperty, DataGridCellInfo.CreatePossiblyPartialCellInfo(currentCell.Item, newColumn, dataGrid));
            }
        }

        /// <summary>
        ///     The cell that, if not in edit mode, can be edited.
        /// </summary>
        /// <remarks>
        ///     The value returned is a structure that provides enough information to describe
        ///     the cell. It is neither an actual reference to the cell container nor the value
        ///     displayed in a given cell.
        /// </remarks>
        public DataGridCellInfo CurrentCell
        {
            get { return (DataGridCellInfo)GetValue(CurrentCellProperty); }
            set { SetValue(CurrentCellProperty, value); }
        }

        /// <summary>
        ///     The DependencyProperty for CurrentCell.
        /// </summary>
        public static readonly DependencyProperty CurrentCellProperty =
            DependencyProperty.Register("CurrentCell", typeof(DataGridCellInfo), typeof(DataGrid), new FrameworkPropertyMetadata(DataGridCellInfo.Unset, new PropertyChangedCallback(OnCurrentCellChanged)));

        private static void OnCurrentCellChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            DataGrid dataGrid = (DataGrid)d;
            DataGridCellInfo oldCell = (DataGridCellInfo)e.OldValue;
            DataGridCellInfo currentCell = (DataGridCellInfo)e.NewValue;

            if (dataGrid.CurrentItem != currentCell.Item)
            {
                dataGrid.SetCurrentItem(currentCell.Item);
            }

            if (dataGrid.CurrentColumn != currentCell.Column)
            {
                dataGrid.SetCurrentValueInternal(CurrentColumnProperty, currentCell.Column);
            }

            if (dataGrid._currentCellContainer != null)
            {
                // _currentCellContainer should still be the old container and not the new one.
                // If _currentCellContainer were null, then it should mean that no BeginEdit was called
                // so, we shouldn't be missing any EndEdits.
                if ((dataGrid.IsAddingNewItem || dataGrid.IsEditingRowItem) && (oldCell.Item != currentCell.Item))
                {
                    // There is a row edit pending and the current cell changed to another row.
                    // Commit the row, which also commits the cell.
                    dataGrid.EndEdit(CommitEditCommand, dataGrid._currentCellContainer, DataGridEditingUnit.Row, /* exitEditingMode = */ true);
                }
                else if (dataGrid._currentCellContainer.IsEditing)
                {
                    // Only the cell needs to commit.
                    dataGrid.EndEdit(CommitEditCommand, dataGrid._currentCellContainer, DataGridEditingUnit.Cell, /* exitEditingMode = */ true);
                }
            }

            var oldCellContainer = dataGrid._currentCellContainer;
            dataGrid._currentCellContainer = null;

            if (currentCell.IsValid && dataGrid.IsKeyboardFocusWithin)
            {
                // If CurrentCell was set by the user and not through a focus change,
                // then focus must be updated, but only when the DataGrid already
                // has focus.
                DataGridCell cell = dataGrid._pendingCurrentCellContainer;
                if (cell == null)
                {
                    cell = dataGrid.CurrentCellContainer;
                    if (cell == null)
                    {
                        // The cell might be virtualized. Try to devirtualize by scrolling.
                        dataGrid.ScrollCellIntoView(currentCell.ItemInfo, currentCell.Column);
                        cell = dataGrid.CurrentCellContainer;
                    }
                }

                if ((cell != null))
                {
                    if (!cell.IsKeyboardFocusWithin)
                    {
                        cell.Focus();
                    }

                    if (oldCellContainer != cell)
                    {
                        if (oldCellContainer != null)
                        {
                            oldCellContainer.NotifyCurrentCellContainerChanged();
                        }

                        cell.NotifyCurrentCellContainerChanged();
                    }
                }
                else if  (oldCellContainer != null)
                {
                    oldCellContainer.NotifyCurrentCellContainerChanged();
                }
            }

            dataGrid.OnCurrentCellChanged(EventArgs.Empty);
        }

        /// <summary>
        ///     An event to notify that the value of CurrentCell changed.
        /// </summary>
        public event EventHandler<EventArgs> CurrentCellChanged;

        /// <summary>
        ///     Called when the value of CurrentCell changes.
        /// </summary>
        /// <param name="e">Empty event arguments.</param>
        protected virtual void OnCurrentCellChanged(EventArgs e)
        {
            if (CurrentCellChanged != null)
            {
                CurrentCellChanged(this, e);
            }
        }

        private void UpdateCurrentCell(DataGridCell cell, bool isFocusWithinCell)
        {
            if (isFocusWithinCell)
            {
                // Focus is within the cell, make it the current cell.
                CurrentCellContainer = cell;
            }
            else if (!IsKeyboardFocusWithin)
            {
                // Focus moved outside the DataGrid, so clear out the current cell.
                CurrentCellContainer = null;
            }

            // Focus is within the DataGrid but not within this particular cell.
            // Assume that focus is moving to another cell, and that cell will update
            // the current cell.
        }

        internal DataGridCell CurrentCellContainer
        {
            get
            {
                if (_currentCellContainer == null)
                {
                    DataGridCellInfo currentCell = CurrentCell;
                    if (currentCell.IsValid)
                    {
                        _currentCellContainer = TryFindCell(currentCell);
                    }
                }

                return _currentCellContainer;
            }

            set
            {
                if ((_currentCellContainer != value) &&
                    ((value == null) || (value != _pendingCurrentCellContainer)))
                {
                    // Setting CurrentCell might cause some re-entrancy due to focus changes.
                    // We need to detect this without actually changing the value until after
                    // setting CurrentCell.
                    _pendingCurrentCellContainer = value;

                    // _currentCellContainer must remain intact while changing CurrentCell
                    // so that the previous edit can be committed.
                    if (value == null)
                    {
                        SetCurrentValueInternal(CurrentCellProperty, DataGridCellInfo.Unset);   // ClearValue
                    }
                    else
                    {
                        SetCurrentValueInternal(CurrentCellProperty, new DataGridCellInfo(value));
                    }

                    _pendingCurrentCellContainer = null;
                    _currentCellContainer = value;
                    CommandManager.InvalidateRequerySuggested();
                }
            }
        }

        private bool IsEditingCurrentCell
        {
            get
            {
                DataGridCell cell = CurrentCellContainer;
                if (cell != null)
                {
                    return cell.IsEditing;
                }

                return false;
            }
        }

        private bool IsCurrentCellReadOnly
        {
            get
            {
                DataGridCell cell = CurrentCellContainer;
                if (cell != null)
                {
                    return cell.IsReadOnly;
                }

                return false;
            }
        }

        internal ItemInfo CurrentInfo
        {
            get { return LeaseItemInfo(CurrentCell.ItemInfo); }
        }

        internal bool IsCurrent(DataGridRow row, DataGridColumn column=null)
        {
            DataGridCellInfo currentCell = CurrentCell;
            if (currentCell.ItemInfo == null)
            {
                // It's possible for the user to create a DGCellInfo with ItemInfo==null,
                // by calling the implicit default ctor.  It's too late
                // to define an explicit default ctor, so just test for it here and
                // treat it as Unset;
                currentCell = DataGridCellInfo.Unset;
            }
            DependencyObject currentContainer = currentCell.ItemInfo.Container;
            int currentIndex = currentCell.ItemInfo.Index;
            return  (column == null || column == currentCell.Column) &&             // columns match
                    (   (currentContainer != null && currentContainer == row) ||    // rows match (the easy way)
                        (   ItemsControl.EqualsEx(CurrentItem, row.Item) &&         // rows match (the hard way)
                            (currentIndex < 0 || currentIndex == ItemContainerGenerator.IndexFromContainer(row))
                        )
                    );
        }

        /// <summary>
        ///     Called just before a cell will change to edit mode
        ///     to allow handlers to prevent the cell from entering edit mode.
        /// </summary>
        public event EventHandler<DataGridBeginningEditEventArgs> BeginningEdit;

        /// <summary>
        ///     Called just before a cell will change to edit mode
        ///     to all subclasses to prevent the cell from entering edit mode.
        /// </summary>
        /// <remarks>
        ///     Default implementation raises the BeginningEdit event.
        /// </remarks>
        protected virtual void OnBeginningEdit(DataGridBeginningEditEventArgs e)
        {
            if (BeginningEdit != null)
            {
                BeginningEdit(this, e);
            }

            if (AutomationPeer.ListenerExists(AutomationEvents.InvokePatternOnInvoked))
            {
                DataGridAutomationPeer peer = DataGridAutomationPeer.FromElement(this) as DataGridAutomationPeer;
                if (peer != null)
                {
                    peer.RaiseAutomationCellInvokeEvents(e.Column, e.Row);
                }
            }
        }

        /// <summary>
        ///     Called after a cell has changed to editing mode to allow
        ///     handlers to modify the contents of the cell.
        /// </summary>
        public event EventHandler<DataGridPreparingCellForEditEventArgs> PreparingCellForEdit;

        /// <summary>
        ///     Called after a cell has changed to editing mode to allow
        ///     subclasses to modify the contents of the cell.
        /// </summary>
        /// <remarks>
        ///     Default implementation raises the PreparingCellForEdit event.
        ///     This method is invoked from DataGridCell (instead of DataGrid) once it has entered edit mode.
        /// </remarks>
        protected internal virtual void OnPreparingCellForEdit(DataGridPreparingCellForEditEventArgs e)
        {
            if (PreparingCellForEdit != null)
            {
                PreparingCellForEdit(this, e);
            }
        }

        /// <summary>
        ///     Raises the BeginEdit command, which will place the current cell or row into
        ///     edit mode.
        /// </summary>
        /// <remarks>
        ///     If the command is enabled, this will lead to the BeginningEdit and PreparingCellForEdit
        ///     overrides and events.
        /// </remarks>
        /// <returns>true if the current cell or row enters edit mode, false otherwise.</returns>
        public bool BeginEdit()
        {
            return BeginEdit(/* editingEventArgs = */ null);
        }

        /// <summary>
        ///     Raises the BeginEdit command, which will place the current cell or row into
        ///     edit mode.
        /// </summary>
        /// <remarks>
        ///     If the command is enabled, this will lead to the BeginningEdit and PreparingCellForEdit
        ///     overrides and events.
        /// </remarks>
        /// <param name="editingEventArgs">The event arguments, if any, that led to BeginEdit being called. May be null.</param>
        /// <returns>true if the current cell or row enters edit mode, false otherwise.</returns>
        public bool BeginEdit(RoutedEventArgs editingEventArgs)
        {
            if (!IsReadOnly)
            {
                DataGridCell cellContainer = CurrentCellContainer;
                if (cellContainer != null)
                {
                    if (!cellContainer.IsEditing &&
                        BeginEditCommand.CanExecute(editingEventArgs, cellContainer))
                    {
                        BeginEditCommand.Execute(editingEventArgs, cellContainer);

                        // When editing the NewItemPlaceHolder row the place holder row gets
                        // replaced with a real row and thus causes a new cellContainer to be
                        // generated. So we should be checking the new cellContainer to decide
                        // if this operation succeeded. 

                        cellContainer = CurrentCellContainer;

                        // the BeginEditCommand may move focus off this DataGrid,
                        // which sets CurrentCellContainer to null.  In that case,
                        // return false. 
                        if (cellContainer == null)
                        {
                            return false;
                        }
                    }

                    return cellContainer.IsEditing;
                }
            }

            return false;
        }

        /// <summary>
        ///     Raises the CancelEdit command.
        ///     If a cell is currently in edit mode, cancels the cell edit, but leaves any row edits alone.
        ///     If a cell is not in edit mode, then cancels any pending row edits.
        /// </summary>
        /// <returns>true if the current cell or row exits edit mode, false otherwise.</returns>
        public bool CancelEdit()
        {
            if (IsEditingCurrentCell)
            {
                return CancelEdit(DataGridEditingUnit.Cell);
            }
            else if (IsEditingRowItem || IsAddingNewItem)
            {
                return CancelEdit(DataGridEditingUnit.Row);
            }

            return true; // No one is in edit mode
        }

        /// <summary>
        ///     Raises the CancelEdit command.
        ///     If a cell is currently in edit mode, cancels the cell edit, but leaves any row edits alone.
        /// </summary>
        /// <returns>true if the cell exits edit mode, false otherwise.</returns>
        internal bool CancelEdit(DataGridCell cell)
        {
            DataGridCell currentCell = CurrentCellContainer;
            if (currentCell != null && currentCell == cell && currentCell.IsEditing)
            {
                return CancelEdit(DataGridEditingUnit.Cell);
            }

            return true;
        }

        /// <summary>
        ///     Raises the CancelEdit command.
        ///     Reverts any pending editing changes to the desired editing unit and exits edit mode.
        /// </summary>
        /// <param name="editingUnit">Whether to cancel edit mode of the current cell or current row.</param>
        /// <returns>true if the current cell or row exits edit mode, false otherwise.</returns>
        public bool CancelEdit(DataGridEditingUnit editingUnit)
        {
            return EndEdit(CancelEditCommand, CurrentCellContainer, editingUnit, true);
        }

        private void CancelAnyEdit()
        {
            if (IsAddingNewItem || IsEditingRowItem)
            {
                // There is a row edit in progress, cancel it, which will also cancel the cell edit.
                CancelEdit(DataGridEditingUnit.Row);
            }
            else if (IsEditingCurrentCell)
            {
                // Cancel the current cell edit.
                CancelEdit(DataGridEditingUnit.Cell);
            }
        }

        /// <summary>
        ///     Raises the CommitEdit command.
        ///     If a cell is currently being edited, commits any pending changes to the cell, but
        ///     leaves any pending changes to the row. This should mean that changes are propagated
        ///     from the editing environment to the pending row.
        ///     If a cell is not currently being edited, then commits any pending rows.
        /// </summary>
        /// <returns>true if the current cell or row exits edit mode, false otherwise.</returns>
        public bool CommitEdit()
        {
            if (IsEditingCurrentCell)
            {
                return CommitEdit(DataGridEditingUnit.Cell, true);
            }
            else if (IsEditingRowItem || IsAddingNewItem)
            {
                return CommitEdit(DataGridEditingUnit.Row, true);
            }

            return true; // No one is in edit mode
        }

        /// <summary>
        ///     Raises the CommitEdit command.
        ///     Commits any pending changes for the given editing unit and exits edit mode.
        /// </summary>
        /// <param name="editingUnit">Whether to commit changes for the current cell or current row.</param>
        /// <param name="exitEditingMode">Whether to exit edit mode.</param>
        /// <returns>true if the current cell or row exits edit mode, false otherwise.</returns>
        public bool CommitEdit(DataGridEditingUnit editingUnit, bool exitEditingMode)
        {
            return EndEdit(CommitEditCommand, CurrentCellContainer, editingUnit, exitEditingMode);
        }

        private bool CommitAnyEdit()
        {
            if (IsAddingNewItem || IsEditingRowItem)
            {
                // There is a row edit in progress, commit it, which will also commit the cell edit.
                return CommitEdit(DataGridEditingUnit.Row, /* exitEditingMode = */ true);
            }
            else if (IsEditingCurrentCell)
            {
                // Commit the current cell edit.
                return CommitEdit(DataGridEditingUnit.Cell, /* exitEditingMode = */ true);
            }

            return true;
        }

        private bool EndEdit(RoutedCommand command, DataGridCell cellContainer, DataGridEditingUnit editingUnit, bool exitEditMode)
        {
            bool cellLeftEditingMode = true;
            bool rowLeftEditingMode = true;

            if (cellContainer != null)
            {
                if (command.CanExecute(editingUnit, cellContainer))
                {
                    command.Execute(editingUnit, cellContainer);
                }

                cellLeftEditingMode = !cellContainer.IsEditing;
                rowLeftEditingMode = !IsEditingRowItem && !IsAddingNewItem;
            }

            if (!exitEditMode)
            {
                if (editingUnit == DataGridEditingUnit.Cell)
                {
                    if (cellContainer != null)
                    {
                        if (cellLeftEditingMode)
                        {
                            return BeginEdit(null);
                        }
                    }
                    else
                    {
                        // A cell was not placed in edit mode
                        return false;
                    }
                }
                else
                {
                    if (rowLeftEditingMode)
                    {
                        object rowItem = cellContainer.RowDataItem;
                        if (rowItem != null)
                        {
                            EditRowItem(rowItem);
                            return IsEditingRowItem;
                        }
                    }

                    // A row item was not placed in edit mode
                    return false;
                }
            }

            return cellLeftEditingMode && ((editingUnit == DataGridEditingUnit.Cell) || rowLeftEditingMode);
        }

        private bool HasCellValidationError
        {
            get
            {
                return _hasCellValidationError;
            }

            set
            {
                if (_hasCellValidationError != value)
                {
                    _hasCellValidationError = value;

                    // BeginEdit's CanExecute status relies on this flag
                    CommandManager.InvalidateRequerySuggested();
                }
            }
        }

        private bool HasRowValidationError
        {
            get
            {
                return _hasRowValidationError;
            }

            set
            {
                if (_hasRowValidationError != value)
                {
                    _hasRowValidationError = value;

                    // BeginEdit's CanExecute status relies on this flag
                    CommandManager.InvalidateRequerySuggested();
                }
            }
        }

        /// <summary>
        ///     Cell in DataGrid which has logical focus
        /// </summary>
        internal DataGridCell FocusedCell
        {
            get
            {
                return _focusedCell;
            }
            set
            {
                if (_focusedCell != value)
                {
                    if (_focusedCell != null)
                    {
                        UpdateCurrentCell(_focusedCell, false);
                    }
                    _focusedCell = value;
                    if (_focusedCell != null)
                    {
                        UpdateCurrentCell(_focusedCell, true);
                    }
                }
            }
        }

        #endregion

        #region Row Editing

        /// <summary>
        ///     Whether the end-user can add new rows to the ItemsSource.
        /// </summary>
        public bool CanUserAddRows
        {
            get { return (bool)GetValue(CanUserAddRowsProperty); }
            set { SetValue(CanUserAddRowsProperty, value); }
        }

        /// <summary>
        ///     DependencyProperty for CanUserAddRows.
        /// </summary>
        public static readonly DependencyProperty CanUserAddRowsProperty =
            DependencyProperty.Register("CanUserAddRows", typeof(bool), typeof(DataGrid), new FrameworkPropertyMetadata(true, new PropertyChangedCallback(OnCanUserAddRowsChanged), new CoerceValueCallback(OnCoerceCanUserAddRows)));

        private static void OnCanUserAddRowsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((DataGrid)d).UpdateNewItemPlaceholder(/* isAddingNewItem = */ false);
        }

        private static object OnCoerceCanUserAddRows(DependencyObject d, object baseValue)
        {
            return OnCoerceCanUserAddOrDeleteRows((DataGrid)d, (bool)baseValue, /* canUserAddRowsProperty = */ true);
        }

        private static bool OnCoerceCanUserAddOrDeleteRows(DataGrid dataGrid, bool baseValue, bool canUserAddRowsProperty)
        {
            // Only when the base value is true do we need to validate that the user
            // can actually add or delete rows.
            if (baseValue)
            {
                if (dataGrid.IsReadOnly || !dataGrid.IsEnabled)
                {
                    // Read-only/disabled DataGrids cannot be modified.
                    return false;
                }
                else
                {
                    if ((canUserAddRowsProperty && !dataGrid.EditableItems.CanAddNew) ||
                        (!canUserAddRowsProperty && !dataGrid.EditableItems.CanRemove))
                    {
                        // The collection view does not allow the add or delete action
                        return false;
                    }
                }
            }

            return baseValue;
        }

        /// <summary>
        ///     Whether the end-user can delete rows from the ItemsSource.
        /// </summary>
        public bool CanUserDeleteRows
        {
            get { return (bool)GetValue(CanUserDeleteRowsProperty); }
            set { SetValue(CanUserDeleteRowsProperty, value); }
        }

        /// <summary>
        ///     DependencyProperty for CanUserDeleteRows.
        /// </summary>
        public static readonly DependencyProperty CanUserDeleteRowsProperty =
            DependencyProperty.Register("CanUserDeleteRows", typeof(bool), typeof(DataGrid), new FrameworkPropertyMetadata(true, new PropertyChangedCallback(OnCanUserDeleteRowsChanged), new CoerceValueCallback(OnCoerceCanUserDeleteRows)));

        private static void OnCanUserDeleteRowsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            // The Delete command needs to have CanExecute run
            CommandManager.InvalidateRequerySuggested();
        }

        private static object OnCoerceCanUserDeleteRows(DependencyObject d, object baseValue)
        {
            return OnCoerceCanUserAddOrDeleteRows((DataGrid)d, (bool)baseValue, /* canUserAddRowsProperty = */ false);
        }

        /// <summary>
        ///     An event that is raised before a new item is created so that
        ///     developers can participate in the construction of the new item.
        /// </summary>
        public event EventHandler<AddingNewItemEventArgs> AddingNewItem;

        /// <summary>
        ///     A method that is called before a new item is created so that
        ///     overrides can participate in the construction of the new item.
        /// </summary>
        /// <remarks>
        ///     The default implementation raises the AddingNewItem event.
        /// </remarks>
        /// <param name="e">Event arguments that provide access to the new item.</param>
        protected virtual void OnAddingNewItem(AddingNewItemEventArgs e)
        {
            if (AddingNewItem != null)
            {
                AddingNewItem(this, e);
            }
        }

        /// <summary>
        ///     An event that is raised when a new item is created so that
        ///     developers can initialize the item with custom default values.
        /// </summary>
        public event InitializingNewItemEventHandler InitializingNewItem;

        /// <summary>
        ///     A method that is called when a new item is created so that
        ///     overrides can initialize the item with custom default values.
        /// </summary>
        /// <remarks>
        ///     The default implementation raises the InitializingNewItem event.
        /// </remarks>
        /// <param name="e">Event arguments that provide access to the new item.</param>
        protected virtual void OnInitializingNewItem(InitializingNewItemEventArgs e)
        {
            if (InitializingNewItem != null)
            {
                InitializingNewItem(this, e);
            }
        }

        private object AddNewItem()
        {
            Debug.Assert(CanUserAddRows, "AddNewItem called when the end-user cannot add new rows.");
            Debug.Assert(!IsAddingNewItem, "AddNewItem called when a pending add is taking place.");

            // Hide the placeholder
            UpdateNewItemPlaceholder(/* isAddingNewItem = */ true);

            // Create the new item (with app's help, or not)
            object newItem = null;
            IEditableCollectionViewAddNewItem ani = (IEditableCollectionViewAddNewItem)Items;

            if (ani.CanAddNewItem)
            {
                AddingNewItemEventArgs e = new AddingNewItemEventArgs();
                OnAddingNewItem(e);
                newItem = e.NewItem;
            }

            newItem = (newItem != null) ? ani.AddNewItem(newItem) : EditableItems.AddNew();

            // initialize the new item
            if (newItem != null)
            {
                OnInitializingNewItem(new InitializingNewItemEventArgs(newItem));
            }

            // CancelEdit and CommitEdit rely on IsAddingNewItem
            CommandManager.InvalidateRequerySuggested();

            return newItem;
        }

        private void EditRowItem(object rowItem)
        {
            EditableItems.EditItem(rowItem);

            // CancelEdit and CommitEdit rely on IsEditingRowItem
            CommandManager.InvalidateRequerySuggested();
        }

        private void CommitRowItem()
        {
            // This case illustrates a minor side-effect of communicating with IEditableObject through two different means
            // - BindingGroup
            // - IEditableCollectionView
            // The sequence of operations is as below.
            // IEditableCollectionView.BeginEdit
            // BindingGroup.BeginEdit
            // BindingGroup.CommitEdit
            // IEditableCollectionView.CommitEdit
            // After having commited the NewItem row the first time it is possible that the IsAddingNewItem is false
            // during the second call to CommitEdit. Hence we cannot quite make this assertion here.

            // Debug.Assert(IsEditingRowItem || IsAddingNewItem, "CommitRowItem was called when a row was not being edited or added.");
            if (IsEditingRowItem)
            {
                EditableItems.CommitEdit();
            }
            else
            {
                EditableItems.CommitNew();

                // Show the placeholder again
                UpdateNewItemPlaceholder(/* isAddingNewItem = */ false);
            }
        }

        private void CancelRowItem()
        {
            // This case illustrates a minor side-effect of communicating with IEditableObject through two different means
            // - BindingGroup
            // - IEditableCollectionView
            // The sequence of operations is as below.
            // IEditableCollectionView.BeginEdit
            // BindingGroup.BeginEdit
            // IEditableCollectionView.CancelEdit
            // BindingGroup.CancelEdit
            // After having cancelled the NewItem row the first time it is possible that the IsAddingNewItem is false
            // during the second call to CancelEdit. Hence we cannot quite make this assertion here.

            // Debug.Assert(IsEditingRowItem || IsAddingNewItem, "CancelRowItem was called when a row was not being edited or added.");
            if (IsEditingRowItem)
            {
                if (EditableItems.CanCancelEdit)
                {
                    EditableItems.CancelEdit();
                }
                else
                {
                    // we haven't changed the data item, so this merely exits edit-mode
                    EditableItems.CommitEdit();
                }
            }
            else
            {
                object currentAddItem = EditableItems.CurrentAddItem;
                bool wasCurrent = currentAddItem == CurrentItem;
                bool wasSelected = SelectedItems.Contains(currentAddItem);
                bool reselectPlaceholderCells = false;
                List<int> columnIndexRanges = null;
                int newItemIndex = -1;

                if (wasSelected)
                {
                    // Unselect the item that was being added
                    UnselectItem(NewItemInfo(currentAddItem));
                }
                else
                {
                    // Cells will automatically unselect when the new item is removed, but we
                    // should reselect them on the placeholder.
                    newItemIndex = Items.IndexOf(currentAddItem);
                    reselectPlaceholderCells = ((newItemIndex >= 0) && _selectedCells.Intersects(newItemIndex, out columnIndexRanges));
                }

                // Cancel the add and remove it from the collection
                EditableItems.CancelNew();

                // Show the placeholder again
                UpdateNewItemPlaceholder(/* isAddingNewItem = */ false);

                if (wasCurrent)
                {
                    // Focus the placeholder if the new item had focus
                    SetCurrentItem(CollectionView.NewItemPlaceholder);
                }

                if (wasSelected)
                {
                    // Re-select the placeholder if it was selected before
                    SelectItem(NewItemInfo(CollectionView.NewItemPlaceholder));
                }
                else if (reselectPlaceholderCells)
                {
                    // Re-select placeholder cells if they were selected before
                    using (UpdateSelectedCells())
                    {
                        int rowIndex = newItemIndex;
                        bool placeholderAtBeginning = (EditableItems.NewItemPlaceholderPosition == NewItemPlaceholderPosition.AtBeginning);

                        // When the placeholder is at the beginning, we need to unselect the cells
                        // in the added row and move those back to the previous row.
                        if (placeholderAtBeginning)
                        {
                            _selectedCells.RemoveRegion(newItemIndex, 0, 1, Columns.Count);
                            rowIndex--;
                        }

                        for (int i = 0, count = columnIndexRanges.Count; i < count; i += 2)
                        {
                            _selectedCells.AddRegion(rowIndex, columnIndexRanges[i], 1, columnIndexRanges[i + 1]);
                        }
                    }
                }
            }
        }

        private void UpdateRowEditing(DataGridCell cell)
        {
            object rowDataItem = cell.RowDataItem;

            // If the row is not in edit/add mode, then clear its IsEditing flag.
            if (!IsAddingOrEditingRowItem(rowDataItem))
            {
                cell.RowOwner.IsEditing = false;
                _editingRowInfo = null;
            }
        }

        private IEditableCollectionView EditableItems
        {
            get { return (IEditableCollectionView)Items; }
        }

        private bool IsAddingNewItem
        {
            get { return EditableItems.IsAddingNew; }
        }

        private bool IsEditingRowItem
        {
            get { return EditableItems.IsEditingItem; }
        }

        private bool IsAddingOrEditingRowItem(object item)
        {
            return IsEditingItem(item) ||
                (IsAddingNewItem && (EditableItems.CurrentAddItem == item));
        }

        private bool IsAddingOrEditingRowItem(DataGridEditingUnit editingUnit, object item)
        {
            return (editingUnit == DataGridEditingUnit.Row) &&
                    IsAddingOrEditingRowItem(item);
        }

        private bool IsEditingItem(object item)
        {
            return IsEditingRowItem && (EditableItems.CurrentEditItem == item);
        }

        private void UpdateNewItemPlaceholder(bool isAddingNewItem)
        {
            var editableItems = EditableItems;
            bool canUserAddRows = CanUserAddRows;

            if (DataGridHelper.IsDefaultValue(this, CanUserAddRowsProperty))
            {
                canUserAddRows = OnCoerceCanUserAddOrDeleteRows(this, canUserAddRows, true);
            }

            if (!isAddingNewItem)
            {
                if (canUserAddRows)
                {
                    // NewItemPlaceholderPosition isn't a DP but we want to default to AtEnd instead of None (can only be done
                    // when canUserAddRows becomes true).  This may override the users intent to make it None, however
                    // they can work around this by resetting it to None after making a change which results in canUserAddRows
                    // becoming true.
                    if (editableItems.NewItemPlaceholderPosition == NewItemPlaceholderPosition.None)
                    {
                        editableItems.NewItemPlaceholderPosition = NewItemPlaceholderPosition.AtEnd;
                    }

                    _placeholderVisibility = Visibility.Visible;
                }
                else
                {
                    if (editableItems.NewItemPlaceholderPosition != NewItemPlaceholderPosition.None)
                    {
                        editableItems.NewItemPlaceholderPosition = NewItemPlaceholderPosition.None;
                    }

                    _placeholderVisibility = Visibility.Collapsed;
                }
            }
            else
            {
                // During a row add, hide the placeholder
                _placeholderVisibility = Visibility.Collapsed;
            }

            // Make sure the newItemPlaceholderRow reflects the correct visiblity
            DataGridRow newItemPlaceholderRow = (DataGridRow)ItemContainerGenerator.ContainerFromItem(CollectionView.NewItemPlaceholder);
            if (newItemPlaceholderRow != null)
            {
                newItemPlaceholderRow.CoerceValue(VisibilityProperty);
            }
        }

        private void SetCurrentItemToPlaceholder()
        {
            NewItemPlaceholderPosition position = EditableItems.NewItemPlaceholderPosition;
            if (position == NewItemPlaceholderPosition.AtEnd)
            {
                int itemCount = Items.Count;
                if (itemCount > 0)
                {
                    SetCurrentItem(Items[itemCount - 1]);
                }
            }
            else if (position == NewItemPlaceholderPosition.AtBeginning)
            {
                if (Items.Count > 0)
                {
                    SetCurrentItem(Items[0]);
                }
            }
        }

        private int DataItemsCount
        {
            get
            {
                int itemsCount = Items.Count;

                // Subtract one if there is a new item placeholder
                if (HasNewItemPlaceholder)
                {
                    itemsCount--;
                }

                return itemsCount;
            }
        }

        private int DataItemsSelected
        {
            get
            {
                int itemsSelected = SelectedItems.Count;

                if (HasNewItemPlaceholder && SelectedItems.Contains(CollectionView.NewItemPlaceholder))
                {
                    itemsSelected--;
                }

                return itemsSelected;
            }
        }

        private bool HasNewItemPlaceholder
        {
            get
            {
                IEditableCollectionView editableItems = EditableItems;
                return editableItems.NewItemPlaceholderPosition != NewItemPlaceholderPosition.None;
            }
        }

        private bool IsNewItemPlaceholder(object item)
        {
            return (item == CollectionView.NewItemPlaceholder) || (item == DataGrid.NewItemPlaceholder);
        }

        #endregion

        #region Row Details

        /// <summary>
        ///     Determines which visibility mode the Row's details use.
        /// </summary>
        public DataGridRowDetailsVisibilityMode RowDetailsVisibilityMode
        {
            get { return (DataGridRowDetailsVisibilityMode)GetValue(RowDetailsVisibilityModeProperty); }
            set { SetValue(RowDetailsVisibilityModeProperty, value); }
        }

        /// <summary>
        ///     DependencyProperty for RowDetailsVisibilityMode.
        /// </summary>
        public static readonly DependencyProperty RowDetailsVisibilityModeProperty =
            DependencyProperty.Register("RowDetailsVisibilityMode", typeof(DataGridRowDetailsVisibilityMode), typeof(DataGrid), new FrameworkPropertyMetadata(DataGridRowDetailsVisibilityMode.VisibleWhenSelected, OnNotifyRowAndDetailsPropertyChanged));

        /// <summary>
        ///     Controls if the row details scroll.
        /// </summary>
        public bool AreRowDetailsFrozen
        {
            get { return (bool)GetValue(AreRowDetailsFrozenProperty); }
            set { SetValue(AreRowDetailsFrozenProperty, value); }
        }

        /// <summary>
        ///     DependencyProperty for AreRowDetailsFrozen.
        /// </summary>
        public static readonly DependencyProperty AreRowDetailsFrozenProperty =
            DependencyProperty.Register("AreRowDetailsFrozen", typeof(bool), typeof(DataGrid), new FrameworkPropertyMetadata(false));

        /// <summary>
        ///     Template used for the Row details.
        /// </summary>
        public DataTemplate RowDetailsTemplate
        {
            get { return (DataTemplate)GetValue(RowDetailsTemplateProperty); }
            set { SetValue(RowDetailsTemplateProperty, value); }
        }

        /// <summary>
        ///     DependencyProperty for RowDetailsTemplate.
        /// </summary>
        public static readonly DependencyProperty RowDetailsTemplateProperty =
            DependencyProperty.Register("RowDetailsTemplate", typeof(DataTemplate), typeof(DataGrid), new FrameworkPropertyMetadata(null, OnNotifyRowAndDetailsPropertyChanged));

        /// <summary>
        ///     TemplateSelector used for the Row details
        /// </summary>
        public DataTemplateSelector RowDetailsTemplateSelector
        {
            get { return (DataTemplateSelector)GetValue(RowDetailsTemplateSelectorProperty); }
            set { SetValue(RowDetailsTemplateSelectorProperty, value); }
        }

        /// <summary>
        ///     DependencyProperty for RowDetailsTemplateSelector.
        /// </summary>
        public static readonly DependencyProperty RowDetailsTemplateSelectorProperty =
            DependencyProperty.Register("RowDetailsTemplateSelector", typeof(DataTemplateSelector), typeof(DataGrid), new FrameworkPropertyMetadata(null, OnNotifyRowAndDetailsPropertyChanged));

        /// <summary>
        ///     Event that is fired just before the details of a Row is shown
        /// </summary>
        public event EventHandler<DataGridRowDetailsEventArgs> LoadingRowDetails;

        /// <summary>
        ///     Event that is fired just before the details of a Row is hidden
        /// </summary>
        public event EventHandler<DataGridRowDetailsEventArgs> UnloadingRowDetails;

        /// <summary>
        ///     Event that is fired when the visibility of a Rows details changes.
        /// </summary>
        public event EventHandler<DataGridRowDetailsEventArgs> RowDetailsVisibilityChanged;


        internal void OnLoadingRowDetailsWrapper(DataGridRow row)
        {
            if (row != null &&
                row.DetailsLoaded == false &&
                row.DetailsVisibility == Visibility.Visible &&
                row.DetailsPresenter != null)
            {
                DataGridRowDetailsEventArgs e = new DataGridRowDetailsEventArgs(row, row.DetailsPresenter.DetailsElement);
                OnLoadingRowDetails(e);
                row.DetailsLoaded = true;
            }
        }

        internal void OnUnloadingRowDetailsWrapper(DataGridRow row)
        {
            if (row != null &&
                row.DetailsLoaded == true &&
                row.DetailsPresenter != null)
            {
                DataGridRowDetailsEventArgs e = new DataGridRowDetailsEventArgs(row, row.DetailsPresenter.DetailsElement);
                OnUnloadingRowDetails(e);
                row.DetailsLoaded = false;
            }
        }

        /// <summary>
        ///     Invokes the LoadingRowDetails event
        /// </summary>
        protected virtual void OnLoadingRowDetails(DataGridRowDetailsEventArgs e)
        {
            if (LoadingRowDetails != null)
            {
                LoadingRowDetails(this, e);
            }
        }

        /// <summary>
        ///     Invokes the UnloadingRowDetails event
        /// </summary>
        protected virtual void OnUnloadingRowDetails(DataGridRowDetailsEventArgs e)
        {
            if (UnloadingRowDetails != null)
            {
                UnloadingRowDetails(this, e);
            }
        }

        /// <summary>
        ///     Invokes the RowDetailsVisibilityChanged event
        /// </summary>
        protected internal virtual void OnRowDetailsVisibilityChanged(DataGridRowDetailsEventArgs e)
        {
            if (RowDetailsVisibilityChanged != null)
            {
                RowDetailsVisibilityChanged(this, e);
            }

            var row = e.Row;

            // LoadingRowDetails only needs to be called when row.DetailsVisibility == Visibility.Visible.
            // OnLoadingRowDetailsWrapper already makes this check, so we omit it here.
            //
            // No need to used DelayedOnLoadingRowDetails because OnRowDetailsVisibilityChanged isn't called until after the
            // template is expanded.
            OnLoadingRowDetailsWrapper(row);
        }

        #endregion

        #region Row Resizing

        /// <summary>
        ///     A property that specifies whether the user can resize rows in the UI by dragging the row headers.
        /// </summary>
        public bool CanUserResizeRows
        {
            get { return (bool)GetValue(CanUserResizeRowsProperty); }
            set { SetValue(CanUserResizeRowsProperty, value); }
        }

        /// <summary>
        ///     The DependencyProperty that represents the CanUserResizeColumns property.
        /// </summary>
        public static readonly DependencyProperty CanUserResizeRowsProperty =
            DependencyProperty.Register("CanUserResizeRows", typeof(bool), typeof(DataGrid), new FrameworkPropertyMetadata(true, new PropertyChangedCallback(OnNotifyRowHeaderPropertyChanged)));

        #endregion

        #region Row Margin

        /// <summary>
        ///     Margin for the new item row.
        /// </summary>
        public Thickness NewItemMargin
        {
            get { return (Thickness)GetValue(NewItemMarginProperty); }
            private set { SetValue(NewItemMarginPropertyKey, value); }
        }

        /// <summary>
        ///     Using a DependencyProperty as the backing store for NewItemMargin.  This enables animation, styling, binding, etc...
        /// </summary>
        private static readonly DependencyPropertyKey NewItemMarginPropertyKey =
            DependencyProperty.RegisterReadOnly("NewItemMargin",
                typeof(Thickness),
                typeof(DataGrid),
                new FrameworkPropertyMetadata(new Thickness(0)));

        /// <summary>
        ///     DependencyProperty for NewItemMargin property
        /// </summary>
        public static readonly DependencyProperty NewItemMarginProperty =
            NewItemMarginPropertyKey.DependencyProperty;

        private void EnqueueNewItemMarginComputation()
        {
            if (!_newItemMarginComputationPending)
            {
                _newItemMarginComputationPending = true;
                Dispatcher.BeginInvoke((Action)delegate()
                {
                    double marginLeft = 0;
                    if (IsGrouping && InternalScrollHost != null)
                    {
                        ContainerTracking<DataGridRow> tracker = _rowTrackingRoot;
                        while (tracker != null)
                        {
                            DataGridRow row = tracker.Container;
                            if (!row.IsNewItem)
                            {
                                // If grouping is enabled and the current row is grouped,
                                // then use this to row to determine its left coordinate.
                                GeneralTransform transform = row.TransformToAncestor(InternalScrollHost);
                                if (transform != null)
                                {
                                    marginLeft = transform.Transform(new Point()).X;
                                }
                                break;
                            }
                            tracker = tracker.Next;
                        }
                    }
                    NewItemMargin = new Thickness(marginLeft, 0, 0, 0);
                    _newItemMarginComputationPending = false;
                },
                DispatcherPriority.Input);
            }
        }

        internal override void OnIsGroupingChanged(DependencyPropertyChangedEventArgs e)
        {
            base.OnIsGroupingChanged(e);
            EnqueueNewItemMarginComputation();
        }

        #endregion

        #region Selection

        internal SelectedItemCollection SelectedItemCollection
        {
            get { return (SelectedItemCollection)SelectedItems; }
        }

        /// <summary>
        ///     The currently selected cells.
        /// </summary>
        public IList<DataGridCellInfo> SelectedCells
        {
            get { return _selectedCells; }
        }

        internal SelectedCellsCollection SelectedCellsInternal
        {
            get { return _selectedCells; }
        }

        /// <summary>
        ///     Event that fires when the SelectedCells collection changes.
        /// </summary>
        public event SelectedCellsChangedEventHandler SelectedCellsChanged;

        /// <summary>
        ///     Direct notification from the SelectedCells collection of a change.
        /// </summary>
        internal void OnSelectedCellsChanged(NotifyCollectionChangedAction action, VirtualizedCellInfoCollection oldItems, VirtualizedCellInfoCollection newItems)
        {
            DataGridSelectionMode selectionMode = SelectionMode;
            DataGridSelectionUnit selectionUnit = SelectionUnit;
            if (!IsUpdatingSelectedCells && (selectionUnit == DataGridSelectionUnit.FullRow))
            {
                throw new InvalidOperationException(SR.Get(SRID.DataGrid_CannotSelectCell));
            }

            // Update the pending list of changes
            if (oldItems != null)
            {
                // When IsUpdatingSelectedCells is true, there may have been cells
                // added to _pendingSelectedCells that are now being removed.
                // These cells should be removed from _pendingSelectedCells and
                // not added to _pendingUnselectedCells.
                if (_pendingSelectedCells != null)
                {
                    VirtualizedCellInfoCollection.Xor(_pendingSelectedCells, oldItems);
                }

                if (_pendingUnselectedCells == null)
                {
                    _pendingUnselectedCells = oldItems;
                }
                else
                {
                    _pendingUnselectedCells.Union(oldItems);
                }
            }

            if (newItems != null)
            {
                // When IsUpdatingSelectedCells is true, there may have been cells
                // added to _pendingUnselectedCells that are now being removed.
                // These cells should be removed from _pendingUnselectedCells and
                // not added to _pendingSelectedCells.
                if (_pendingUnselectedCells != null)
                {
                    VirtualizedCellInfoCollection.Xor(_pendingUnselectedCells, newItems);
                }

                if (_pendingSelectedCells == null)
                {
                    _pendingSelectedCells = newItems;
                }
                else
                {
                    _pendingSelectedCells.Union(newItems);
                }
            }

            // Not deferring change notifications
            if (!IsUpdatingSelectedCells)
            {
                // This is most likely the case when SelectedCells was updated by
                // the application. In this case, some fix-up is required, and
                // the public event needs to fire.

                // This will fire the event on dispose
                using (UpdateSelectedCells())
                {
                    if ((selectionMode == DataGridSelectionMode.Single) && // Single select mode
                        (action == NotifyCollectionChangedAction.Add) && // An item was added
                        (_selectedCells.Count > 1)) // There is more than one selected cell
                    {
                        // When in single selection mode and there is more than one selected
                        // cell, remove all cells but the new cell.
                        _selectedCells.RemoveAllButOne(newItems[0]);
                    }
                    else if ((action == NotifyCollectionChangedAction.Remove) &&
                             (oldItems != null) &&
                             (selectionUnit == DataGridSelectionUnit.CellOrRowHeader))
                    {
                        // If removed cells belong to rows that are selected, then the row
                        // needs to be unselected (other selected cells may remain selected).
                        bool alreadyUpdating = IsUpdatingSelectedItems;
                        if (!alreadyUpdating)
                        {
                            BeginUpdateSelectedItems();
                        }

                        try
                        {
                            object lastRowItem = null;
                            foreach (DataGridCellInfo cellInfo in oldItems)
                            {
                                // First ensure that we haven't already checked the item
                                object rowItem = cellInfo.Item;
                                if (rowItem != lastRowItem)
                                {
                                    lastRowItem = rowItem;

                                    if (SelectedItems.Contains(rowItem))
                                    {
                                        // Remove the item
                                        SelectedItems.Remove(rowItem);
                                    }
                                }
                            }
                        }
                        finally
                        {
                            if (!alreadyUpdating)
                            {
                                EndUpdateSelectedItems();
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        ///     Fires the public change event when there are pending cell changes.
        /// </summary>
        private void NotifySelectedCellsChanged()
        {
            if (((_pendingSelectedCells != null) && (_pendingSelectedCells.Count > 0)) ||
                ((_pendingUnselectedCells != null) && (_pendingUnselectedCells.Count > 0)))
            {
                // Create the new event args
                SelectedCellsChangedEventArgs e = new SelectedCellsChangedEventArgs(this, _pendingSelectedCells, _pendingUnselectedCells);

                // Calculate the previous and current selection counts to determine if commands need invalidating
                int currentSelectionCount = _selectedCells.Count;
                int unselectedCellCount = (_pendingUnselectedCells != null) ? _pendingUnselectedCells.Count : 0;
                int selectedCellCount = (_pendingSelectedCells != null) ? _pendingSelectedCells.Count : 0;
                int previousSelectionCount = currentSelectionCount - selectedCellCount + unselectedCellCount;

                // Clear the pending lists
                _pendingSelectedCells = null;
                _pendingUnselectedCells = null;

                // Fire the public event
                OnSelectedCellsChanged(e);

                // If old or new selection is empty - invalidate Copy command
                if ((previousSelectionCount == 0) || (currentSelectionCount == 0))
                {
                    // The Copy command needs to have CanExecute run
                    CommandManager.InvalidateRequerySuggested();
                }
            }
        }

        /// <summary>
        ///     Called when there are changes to the SelectedCells collection.
        /// </summary>
        /// <param name="e">Event arguments that indicate which cells were added or removed.</param>
        /// <remarks>
        ///     Base implementation fires the public SelectedCellsChanged event.
        /// </remarks>
        protected virtual void OnSelectedCellsChanged(SelectedCellsChangedEventArgs e)
        {
            if (SelectedCellsChanged != null)
            {
                SelectedCellsChanged(this, e);
            }

            // Raise automation events
            if (AutomationPeer.ListenerExists(AutomationEvents.SelectionItemPatternOnElementSelected) ||
                AutomationPeer.ListenerExists(AutomationEvents.SelectionItemPatternOnElementAddedToSelection) ||
                AutomationPeer.ListenerExists(AutomationEvents.SelectionItemPatternOnElementRemovedFromSelection))
            {
                DataGridAutomationPeer peer = DataGridAutomationPeer.FromElement(this) as DataGridAutomationPeer;
                if (peer != null)
                {
                    peer.RaiseAutomationCellSelectedEvent(e);
                }
            }
        }

        /// <summary>
        ///     A command that, when invoked, will select all items in the DataGrid.
        /// </summary>
        public static RoutedUICommand SelectAllCommand
        {
            get
            {
                return ApplicationCommands.SelectAll;
            }
        }

        private static void OnCanExecuteSelectAll(object sender, CanExecuteRoutedEventArgs e)
        {
            DataGrid dataGrid = (DataGrid)sender;
            e.CanExecute = (dataGrid.SelectionMode == DataGridSelectionMode.Extended) && dataGrid.IsEnabled;
            e.Handled = true;
        }

        private static void OnExecutedSelectAll(object sender, ExecutedRoutedEventArgs e)
        {
            DataGrid dataGrid = (DataGrid)sender;
            if (dataGrid.SelectionUnit == DataGridSelectionUnit.Cell)
            {
                dataGrid.SelectAllCells();
            }
            else
            {
                dataGrid.SelectAll();
            }

            e.Handled = true;
        }

        /// <summary>
        ///     Called from the public SelectAll API.
        /// </summary>
        internal override void SelectAllImpl()
        {
            int numItems = Items.Count;
            int numColumns = _columns.Count;
            if ((numColumns > 0) && (numItems > 0))
            {
                using (UpdateSelectedCells())
                {
                    // Selecting the cells first is an optimization.
                    _selectedCells.AddRegion(0, 0, numItems, numColumns);
                    base.SelectAllImpl();
                }
            }
        }

        internal void SelectOnlyThisCell(DataGridCellInfo currentCellInfo)
        {
            using (UpdateSelectedCells())
            {
                _selectedCells.Clear();
                _selectedCells.Add(currentCellInfo);
            }
        }

        /// <summary>
        ///     Selects all cells.
        /// </summary>
        public void SelectAllCells()
        {
            if (SelectionUnit == DataGridSelectionUnit.FullRow)
            {
                SelectAll();
            }
            else
            {
                int numItems = Items.Count;
                int numColumns = _columns.Count;

                if ((numItems > 0) && (numColumns > 0))
                {
                    using (UpdateSelectedCells())
                    {
                        if (_selectedCells.Count > 0)
                        {
                            _selectedCells.Clear();
                        }

                        _selectedCells.AddRegion(0, 0, numItems, numColumns);
                    }
                }
            }
        }

        /// <summary>
        ///     Unselects all cells.
        /// </summary>
        public void UnselectAllCells()
        {
            using (UpdateSelectedCells())
            {
                // Unselect all of the cells
                _selectedCells.Clear();

                if (SelectionUnit != DataGridSelectionUnit.Cell)
                {
                    // Unselect all the items
                    UnselectAll();
                }
            }
        }

        /// <summary>
        ///     Defines the selection behavior.
        /// </summary>
        /// <remarks>
        ///     The SelectionMode and the SelectionUnit properties together define
        ///     the selection behavior for the DataGrid.
        /// </remarks>
        public DataGridSelectionMode SelectionMode
        {
            get { return (DataGridSelectionMode)GetValue(SelectionModeProperty); }
            set { SetValue(SelectionModeProperty, value); }
        }

        /// <summary>
        ///     The DependencyProperty for the SelectionMode property.
        /// </summary>
        public static readonly DependencyProperty SelectionModeProperty =
            DependencyProperty.Register("SelectionMode", typeof(DataGridSelectionMode), typeof(DataGrid), new FrameworkPropertyMetadata(DataGridSelectionMode.Extended, new PropertyChangedCallback(OnSelectionModeChanged)));

        private static void OnSelectionModeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            DataGrid dataGrid = (DataGrid)d;
            DataGridSelectionMode newSelectionMode = (DataGridSelectionMode)e.NewValue;
            bool changingToSingleMode = newSelectionMode == DataGridSelectionMode.Single;
            DataGridSelectionUnit selectionUnit = dataGrid.SelectionUnit;

            if (changingToSingleMode && (selectionUnit == DataGridSelectionUnit.Cell))
            {
                // Setting CanSelectMultipleItems affects SelectedItems, but DataGrid
                // needs to modify SelectedCells manually.
                using (dataGrid.UpdateSelectedCells())
                {
                    dataGrid._selectedCells.RemoveAllButOne();
                }
            }

            // Update whether multiple items can be selected. Setting this property
            // will remove items when going from multiple to single mode.
            dataGrid.CanSelectMultipleItems = (newSelectionMode != DataGridSelectionMode.Single);

            if (changingToSingleMode && (selectionUnit == DataGridSelectionUnit.CellOrRowHeader))
            {
                // In CellOrRowHeader, wait until after CanSelectMultipleItems is done removing items.
                if (dataGrid.SelectedItems.Count > 0)
                {
                    // If there is a selected item, then de-select all cells except for that one row.
                    using (dataGrid.UpdateSelectedCells())
                    {
                        dataGrid._selectedCells.RemoveAllButOneRow(dataGrid.InternalSelectedInfo.Index);
                    }
                }
                else
                {
                    // If there is no selected item, then de-select all cells except for one.
                    using (dataGrid.UpdateSelectedCells())
                    {
                        dataGrid._selectedCells.RemoveAllButOne();
                    }
                }
            }
        }

        /// <summary>
        ///     Defines the selection behavior.
        /// </summary>
        /// <remarks>
        ///     The SelectionMode and the SelectionUnit properties together define
        ///     the selection behavior for the DataGrid.
        /// </remarks>
        public DataGridSelectionUnit SelectionUnit
        {
            get { return (DataGridSelectionUnit)GetValue(SelectionUnitProperty); }
            set { SetValue(SelectionUnitProperty, value); }
        }

        /// <summary>
        ///     The DependencyProperty for the SelectionUnit property.
        /// </summary>
        public static readonly DependencyProperty SelectionUnitProperty =
            DependencyProperty.Register("SelectionUnit", typeof(DataGridSelectionUnit), typeof(DataGrid), new FrameworkPropertyMetadata(DataGridSelectionUnit.FullRow, new PropertyChangedCallback(OnSelectionUnitChanged)));

        private static void OnSelectionUnitChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            DataGrid dataGrid = (DataGrid)d;
            DataGridSelectionUnit oldUnit = (DataGridSelectionUnit)e.OldValue;

            // Full wipe on unit change
            if (oldUnit != DataGridSelectionUnit.Cell)
            {
                dataGrid.UnselectAll();
            }

            if (oldUnit != DataGridSelectionUnit.FullRow)
            {
                using (dataGrid.UpdateSelectedCells())
                {
                    dataGrid._selectedCells.Clear();
                }
            }

            dataGrid.CoerceValue(IsSynchronizedWithCurrentItemProperty);
        }

        /// <summary>
        ///     Called when SelectedItems changes.
        /// </summary>
        protected override void OnSelectionChanged(SelectionChangedEventArgs e)
        {
            if (!IsUpdatingSelectedCells)
            {
                using (UpdateSelectedCells())
                {
                    // Remove cells of rows that were deselected
                    int count = e.RemovedInfos.Count;
                    for (int i = 0; i < count; i++)
                    {
                        ItemInfo rowInfo = e.RemovedInfos[i];
                        UpdateSelectionOfCellsInRow(rowInfo, /* isSelected = */ false);
                    }

                    // Add cells of rows that were selected
                    count = e.AddedInfos.Count;
                    for (int i = 0; i < count; i++)
                    {
                        ItemInfo rowInfo = e.AddedInfos[i];
                        UpdateSelectionOfCellsInRow(rowInfo, /* isSelected = */ true);
                    }
                }
            }

            // Delete depends on the selection state
            CommandManager.InvalidateRequerySuggested();

            // Raise automation events
            if (AutomationPeer.ListenerExists(AutomationEvents.SelectionItemPatternOnElementSelected) ||
                AutomationPeer.ListenerExists(AutomationEvents.SelectionItemPatternOnElementAddedToSelection) ||
                AutomationPeer.ListenerExists(AutomationEvents.SelectionItemPatternOnElementRemovedFromSelection))
            {
                DataGridAutomationPeer peer = DataGridAutomationPeer.FromElement(this) as DataGridAutomationPeer;
                if (peer != null)
                {
                    peer.RaiseAutomationSelectionEvents(e);
                }
            }

            base.OnSelectionChanged(e);
        }

        private void UpdateIsSelected()
        {
            UpdateIsSelected(_pendingUnselectedCells, /* isSelected = */ false);
            UpdateIsSelected(_pendingSelectedCells, /* isSelected = */ true);
        }

        /// <summary>
        ///     Updates the IsSelected property on cells due to a change in SelectedCells.
        /// </summary>
        private void UpdateIsSelected(VirtualizedCellInfoCollection cells, bool isSelected)
        {
            if (cells != null)
            {
                int numCells = cells.Count;
                if (numCells > 0)
                {
                    // Determine if it would be better to iterate through all the visible cells
                    // instead of through the update list.
                    bool useTracker = false;

                    // For "small" updates it's simpler to just go through the cells, get the container,
                    // and update IsSelected. For "large" updates, it's faster to go through the visible
                    // cells, see if they're in the collection, and then update IsSelected.
                    // Determining small vs. large is going to be done using a magic number.
                    // 750 is close to the number of visible cells Excel shows by default on a 1280x1024 monitor.
                    if (numCells > 750)
                    {
                        int numTracker = 0;
                        int numColumns = _columns.Count;

                        ContainerTracking<DataGridRow> rowTracker = _rowTrackingRoot;
                        while (rowTracker != null)
                        {
                            numTracker += numColumns;
                            if (numTracker >= numCells)
                            {
                                // There are more cells visible than being updated
                                break;
                            }

                            rowTracker = rowTracker.Next;
                        }

                        useTracker = (numCells > numTracker);
                    }

                    if (useTracker)
                    {
                        ContainerTracking<DataGridRow> rowTracker = _rowTrackingRoot;
                        while (rowTracker != null)
                        {
                            DataGridRow row = rowTracker.Container;
                            DataGridCellsPresenter cellsPresenter = row.CellsPresenter;
                            if (cellsPresenter != null)
                            {
                                ContainerTracking<DataGridCell> cellTracker = cellsPresenter.CellTrackingRoot;
                                while (cellTracker != null)
                                {
                                    DataGridCell cell = cellTracker.Container;
                                    DataGridCellInfo cellInfo = new DataGridCellInfo(cell);
                                    if (cells.Contains(cellInfo))
                                    {
                                        cell.SyncIsSelected(isSelected);
                                    }

                                    cellTracker = cellTracker.Next;
                                }
                            }

                            rowTracker = rowTracker.Next;
                        }
                    }
                    else
                    {
                        foreach (DataGridCellInfo cellInfo in cells)
                        {
                            DataGridCell cell = TryFindCell(cellInfo);
                            if (cell != null)
                            {
                                cell.SyncIsSelected(isSelected);
                            }
                        }
                    }
                }
            }
        }

        private void UpdateSelectionOfCellsInRow(ItemInfo rowInfo, bool isSelected)
        {
            int columnCount = _columns.Count;
            if (columnCount > 0)
            {
                if (!isSelected && _pendingInfos != null)
                {
                    // deselecting an item - remove it from the pending list
                    // regardless of whether its index is known
                    _pendingInfos.Remove(rowInfo);
                }

                int rowIndex = rowInfo.Index;
                if (rowIndex >= 0)
                {
                    if (isSelected)
                    {
                        _selectedCells.AddRegion(rowIndex, 0, 1, columnCount);
                    }
                    else
                    {
                        _selectedCells.RemoveRegion(rowIndex, 0, 1, columnCount);
                    }
                }
                else
                {
                    // the index isn't known yet.  Mark the ItemInfo as pending
                    if (isSelected)
                    {
                        EnsurePendingInfos();
                        _pendingInfos.Add(rowInfo);
                    }
                }
            }
        }

        private void EnsurePendingInfos()
        {
            if (_pendingInfos == null)
            {
                _pendingInfos = new List<ItemInfo>();
            }
        }

        /// <summary>
        ///     Notification that a particular cell's IsSelected property changed.
        /// </summary>
        internal void CellIsSelectedChanged(DataGridCell cell, bool isSelected)
        {
            if (!IsUpdatingSelectedCells)
            {
                DataGridCellInfo cellInfo = new DataGridCellInfo(cell);
                if (isSelected)
                {
                    _selectedCells.AddValidatedCell(cellInfo);
                }
                else if (_selectedCells.Contains(cellInfo))
                {
                    _selectedCells.Remove(cellInfo);
                }
            }
        }

        /// <summary>
        ///     There was general input that means that selection should occur on
        ///     the given cell.
        /// </summary>
        /// <param name="cell">The target cell.</param>
        /// <param name="startDragging">Whether the input also indicated that dragging should start.</param>
        internal void HandleSelectionForCellInput(DataGridCell cell, bool startDragging, bool allowsExtendSelect, bool allowsMinimalSelect)
        {
            DataGridSelectionUnit selectionUnit = SelectionUnit;

            // If the mode is None, then no selection will occur
            if (selectionUnit == DataGridSelectionUnit.FullRow)
            {
                // In FullRow mode, items are selected
                MakeFullRowSelection(ItemInfoFromContainer(cell.RowOwner), allowsExtendSelect, allowsMinimalSelect);
            }
            else
            {
                // In the other modes, cells can be individually selected
                MakeCellSelection(new DataGridCellInfo(cell), allowsExtendSelect, allowsMinimalSelect);
            }

            if (startDragging)
            {
                BeginDragging();
            }
        }

        /// <summary>
        ///     There was general input on a row header that indicated that
        ///     selection should occur on the given row.
        /// </summary>
        /// <param name="row">The target row.</param>
        /// <param name="startDragging">Whether the input also indicated that dragging should start.</param>
        internal void HandleSelectionForRowHeaderAndDetailsInput(DataGridRow row, bool startDragging)
        {
            ItemInfo rowInfo = ItemInfoFromContainer(row);

            // When not dragging, move focus to the first cell
            if (!_isDraggingSelection && (_columns.Count > 0))
            {
                if (!IsKeyboardFocusWithin)
                {
                    // In order for CurrentCell to move focus, the
                    // DataGrid needs to be focused.
                    Focus();
                }

                DataGridCellInfo currentCell = CurrentCell;
                if (currentCell.ItemInfo != rowInfo)
                {
                    // Change the CurrentCell if the row is different
                    SetCurrentValueInternal(CurrentCellProperty, new DataGridCellInfo(rowInfo, ColumnFromDisplayIndex(0), this));
                }
                else
                {
                    if (_currentCellContainer != null && _currentCellContainer.IsEditing)
                    {
                        // End the pending edit even for the same row
                        EndEdit(CommitEditCommand, _currentCellContainer, DataGridEditingUnit.Cell, /* exitEditingMode = */ true);
                    }
                }
            }

            // Select a row when the mode is not None and the unit allows selecting rows
            if (CanSelectRows)
            {
                MakeFullRowSelection(rowInfo, /* allowsExtendSelect = */ true, /* allowsMinimalSelect = */ true);

                if (startDragging)
                {
                    BeginRowDragging();
                }
            }
        }

        private void BeginRowDragging()
        {
            BeginDragging();
            _isRowDragging = true;
        }

        private void BeginDragging()
        {
            if (Mouse.Capture(this, CaptureMode.SubTree))
            {
                _isDraggingSelection = true;
                _dragPoint = Mouse.GetPosition(this);
            }
        }

        private void EndDragging()
        {
            StopAutoScroll();
            if (Mouse.Captured == this)
            {
                ReleaseMouseCapture();
            }

            _isDraggingSelection = false;
            _isRowDragging = false;
        }

        /// <summary>
        ///     Processes selection for a row.
        ///     Depending on the current keyboard state, this may mean
        ///     - Selecting the row
        ///     - Deselecting the row
        ///     - Deselecting other rows
        ///     - Extending selection to the row
        /// </summary>
        /// <remarks>
        ///     ADO.Net has a bug (#524977) where if the row is in edit mode
        ///     and atleast one of the cells are edited and committed without
        ///     commiting the row itself, DataView.IndexOf for that row returns -1
        ///     and DataView.Contains returns false. The Workaround to this problem
        ///     is to try to use the previously computed row index if the operations
        ///     are in the same row scope.
        /// </remarks>
        private void MakeFullRowSelection(ItemInfo info, bool allowsExtendSelect, bool allowsMinimalSelect)
        {
            bool extendSelection = allowsExtendSelect && ShouldExtendSelection;

            // minimalModify means that previous selections should not be cleared
            // or that the particular item should be toggled.
            bool minimalModify = allowsMinimalSelect && ShouldMinimallyModifySelection;

            using (UpdateSelectedCells())
            {
                bool alreadyUpdating = IsUpdatingSelectedItems;
                if (!alreadyUpdating)
                {
                    BeginUpdateSelectedItems();
                }

                try
                {
                    if (extendSelection)
                    {
                        // Extend selection from the anchor to the item
                        int numColumns = _columns.Count;
                        if (numColumns > 0)
                        {
                            int startIndex = _selectionAnchor.Value.ItemInfo.Index;
                            int endIndex = info.Index;
                            if (startIndex > endIndex)
                            {
                                // Ensure that startIndex is before endIndex
                                int temp = startIndex;
                                startIndex = endIndex;
                                endIndex = temp;
                            }

                            if ((startIndex >= 0) && (endIndex >= 0))
                            {
                                int numItemsSelected = _selectedItems.Count;

                                if (!minimalModify)
                                {
                                    bool clearedCells = false;

                                    // Unselect items not within the selection range
                                    for (int index = 0; index < numItemsSelected; index++)
                                    {
                                        ItemInfo itemInfo = _selectedItems[index];
                                        int itemIndex = itemInfo.Index;

                                        if ((itemIndex < startIndex) || (endIndex < itemIndex))
                                        {
                                            // Selector has been signaled to delay updating the
                                            // collection until we have finished the entire update.
                                            // The item will actually remain in the collection
                                            // until EndUpdateSelectedItems.
                                            SelectionChange.Unselect(itemInfo);

                                            if (!clearedCells)
                                            {
                                                // We only want to clear if something is actually being removed.
                                                _selectedCells.Clear();
                                                clearedCells = true;
                                            }
                                        }
                                    }
                                }
                                else
                                {
                                    // If we hold Control key - unselect only the previous drag selection (between CurrentCell and endIndex)
                                    int currentCellIndex = CurrentCell.ItemInfo.Index;
                                    int removeRangeStartIndex = -1;
                                    int removeRangeEndIndex = -1;
                                    if (currentCellIndex < startIndex)
                                    {
                                        removeRangeStartIndex = currentCellIndex;
                                        removeRangeEndIndex = startIndex - 1;
                                    }
                                    else if (currentCellIndex > endIndex)
                                    {
                                        removeRangeStartIndex = endIndex + 1;
                                        removeRangeEndIndex = currentCellIndex;
                                    }

                                    if (removeRangeStartIndex >= 0 && removeRangeEndIndex >= 0)
                                    {
                                        for (int index = 0; index < numItemsSelected; index++)
                                        {
                                            ItemInfo itemInfo = _selectedItems[index];
                                            int itemIndex = itemInfo.Index;

                                            if ((removeRangeStartIndex <= itemIndex) && (itemIndex <= removeRangeEndIndex))
                                            {
                                                // Selector has been signaled to delay updating the
                                                // collection until we have finished the entire update.
                                                // The item will actually remain in the collection
                                                // until EndUpdateSelectedItems.
                                                SelectionChange.Unselect(itemInfo);
                                            }
                                        }

                                        _selectedCells.RemoveRegion(removeRangeStartIndex, 0, removeRangeEndIndex - removeRangeStartIndex + 1, Columns.Count);
                                    }
                                }

                                // Select the children in the selection range
                                IEnumerator enumerator = ((IEnumerable)Items).GetEnumerator();
                                for (int index = 0; index <= endIndex; index++)
                                {
                                    if (!enumerator.MoveNext())
                                    {
                                        // In case the enumerator ends unexpectedly
                                        break;
                                    }

                                    if (index >= startIndex)
                                    {
                                        SelectionChange.Select(ItemInfoFromIndex(index), true);
                                    }
                                }

                                IDisposable d = enumerator as IDisposable;
                                if (d != null)
                                {
                                    d.Dispose();
                                }

                                _selectedCells.AddRegion(startIndex, 0, endIndex - startIndex + 1, _columns.Count);
                            }
                        }
                    }
                    else
                    {
                        if (minimalModify && _selectedItems.Contains(info))
                        {
                            // Unselect the one item
                            UnselectItem(info);
                        }
                        else
                        {
                            if (!minimalModify || !CanSelectMultipleItems)
                            {
                                // Unselect the other items
                                if (_selectedCells.Count > 0)
                                {
                                    // Pre-emptively clear the SelectedCells collection, which is O(1),
                                    // instead of waiting for the selection change notification to clear
                                    // SelectedCells row by row, which is O(n).
                                    _selectedCells.Clear();
                                }

                                if (SelectedItems.Count > 0)
                                {
                                    SelectedItems.Clear();
                                }
                            }

                            if (_editingRowInfo == info)
                            {
                                // ADO.Net bug workaround, see remarks.
                                int numColumns = _columns.Count;
                                if (numColumns > 0)
                                {
                                    _selectedCells.AddRegion(_editingRowInfo.Index, 0, 1, numColumns);
                                }

                                SelectItem(info, false);
                            }
                            else
                            {
                                // Select the item
                                SelectItem(info);
                            }
                        }

                        _selectionAnchor = new DataGridCellInfo(info.Clone(), ColumnFromDisplayIndex(0), this);
                    }
                }
                finally
                {
                    if (!alreadyUpdating)
                    {
                        EndUpdateSelectedItems();
                    }
                }
            }
        }

        /// <summary>
        ///     Process selection on a cell.
        ///     Depending on the current keyboard state, this may mean
        ///     - Selecting the cell
        ///     - Deselecting the cell
        ///     - Deselecting other cells
        ///     - Extending selection to the cell
        /// </summary>
        /// <remarks>
        ///     ADO.Net has a bug (#524977) where if the row is in edit mode
        ///     and atleast one of the cells are edited and committed without
        ///     commiting the row itself, DataView.IndexOf for that row returns -1
        ///     and DataView.Contains returns false. The Workaround to this problem
        ///     is to try to use the previously computed row index if the operations
        ///     are in the same row scope.
        /// </remarks>
        private void MakeCellSelection(DataGridCellInfo cellInfo, bool allowsExtendSelect, bool allowsMinimalSelect)
        {
            bool extendSelection = allowsExtendSelect && ShouldExtendSelection;

            // minimalModify means that previous selections should not be cleared
            // or that the particular item should be toggled.
            bool minimalModify = allowsMinimalSelect && ShouldMinimallyModifySelection;

            using (UpdateSelectedCells())
            {
                int cellInfoColumnIndex = cellInfo.Column.DisplayIndex;
                if (extendSelection)
                {
                    // Extend selection from the anchor to the cell
                    ItemCollection items = Items;

                    int startIndex = _selectionAnchor.Value.ItemInfo.Index;
                    int endIndex = cellInfo.ItemInfo.Index;

                    DataGridColumn anchorColumn = _selectionAnchor.Value.Column;
                    int startColumnIndex = anchorColumn.DisplayIndex;
                    int endColumnIndex = cellInfoColumnIndex;

                    if ((startIndex >= 0) && (endIndex >= 0) &&
                        (startColumnIndex >= 0) && (endColumnIndex >= 0))
                    {
                        int newRowCount = Math.Abs(endIndex - startIndex) + 1;
                        int newColumnCount = Math.Abs(endColumnIndex - startColumnIndex) + 1;

                        if (!minimalModify)
                        {
                            // When extending cell selection, clear out any selected items
                            if (SelectedItems.Count > 0)
                            {
                                UnselectAll();
                            }

                            _selectedCells.Clear();
                        }
                        else
                        {
                            // Remove the previously selected region
                            int currentCellIndex = CurrentCell.ItemInfo.Index;
                            int currentCellColumnIndex = CurrentCell.Column.DisplayIndex;

                            int previousStartIndex = Math.Min(startIndex, currentCellIndex);
                            int previousRowCount = Math.Abs(currentCellIndex - startIndex) + 1;
                            int previousStartColumnIndex = Math.Min(startColumnIndex, currentCellColumnIndex);
                            int previousColumnCount = Math.Abs(currentCellColumnIndex - startColumnIndex) + 1;

                            _selectedCells.RemoveRegion(previousStartIndex, previousStartColumnIndex, previousRowCount, previousColumnCount);

                            if (SelectionUnit == DataGridSelectionUnit.CellOrRowHeader)
                            {
                                int removeRowStartIndex = previousStartIndex;
                                int removeRowEndIndex = previousStartIndex + previousRowCount - 1;

                                if (previousColumnCount <= newColumnCount)
                                {
                                    // When no columns were removed, we can check fewer rows
                                    if (previousRowCount > newRowCount)
                                    {
                                        // One or more rows were removed, so only check those rows
                                        int removeCount = previousRowCount - newRowCount;
                                        removeRowStartIndex = (previousStartIndex == currentCellIndex) ? currentCellIndex : currentCellIndex - removeCount + 1;
                                        removeRowEndIndex = removeRowStartIndex + removeCount - 1;
                                    }
                                    else
                                    {
                                        // No rows were removed, so don't check anything
                                        removeRowEndIndex = removeRowStartIndex - 1;
                                    }
                                }

                                // For cells that were removed, check if their row is selected
                                for (int i = removeRowStartIndex; i <= removeRowEndIndex; i++)
                                {
                                    object item = Items[i];
                                    if (SelectedItems.Contains(item))
                                    {
                                        // When a cell in a row is unselected, unselect the row too
                                        SelectedItems.Remove(item);
                                    }
                                }
                            }
                        }

                        // Select the cells in rows within the selection range
                        _selectedCells.AddRegion(Math.Min(startIndex, endIndex), Math.Min(startColumnIndex, endColumnIndex), newRowCount, newColumnCount);
                    }
                }
                else
                {
                    bool selectedCellsContainsCellInfo = _selectedCells.Contains(cellInfo);
                    bool singleRowOperation = (_editingRowInfo != null && _editingRowInfo.Index == cellInfo.ItemInfo.Index);
                    if (!selectedCellsContainsCellInfo &&
                        singleRowOperation)
                    {
                        // ADO.Net bug workaround, see remarks.
                        selectedCellsContainsCellInfo = _selectedCells.Contains(_editingRowInfo.Index, cellInfoColumnIndex);
                    }

                    if (minimalModify && selectedCellsContainsCellInfo)
                    {
                        // Unselect the one cell
                        if (singleRowOperation)
                        {
                            // ADO.Net bug workaround, see remarks.
                            _selectedCells.RemoveRegion(_editingRowInfo.Index, cellInfoColumnIndex, 1, 1);
                        }
                        else
                        {
                            _selectedCells.Remove(cellInfo);
                        }

                        if ((SelectionUnit == DataGridSelectionUnit.CellOrRowHeader) &&
                            SelectedItems.Contains(cellInfo.Item))
                        {
                            // When a cell in a row is unselected, unselect the row too
                            SelectedItems.Remove(cellInfo.Item);
                        }
                    }
                    else
                    {
                        if (!minimalModify || !CanSelectMultipleItems)
                        {
                            // Unselect any items
                            if (SelectedItems.Count > 0)
                            {
                                UnselectAll();
                            }

                            // Unselect all the other cells
                            _selectedCells.Clear();
                        }

                        if (singleRowOperation)
                        {
                            // ADO.Net bug workaround, see remarks.
                            _selectedCells.AddRegion(_editingRowInfo.Index, cellInfoColumnIndex, 1, 1);
                        }
                        else
                        {
                            // Select the cell
                            _selectedCells.AddValidatedCell(cellInfo);
                        }
                    }

                    _selectionAnchor = new DataGridCellInfo(cellInfo);
                }
            }
        }

        private void SelectItem(ItemInfo info)
        {
            SelectItem(info, true);
        }

        private void SelectItem(ItemInfo info, bool selectCells)
        {
            if (selectCells)
            {
                using (UpdateSelectedCells())
                {
                    int itemIndex = info.Index;
                    int numColumns = _columns.Count;
                    if ((itemIndex >= 0) && (numColumns > 0))
                    {
                        _selectedCells.AddRegion(itemIndex, 0, 1, numColumns);
                    }
                }
            }

            UpdateSelectedItems(info, /* Add = */ true);
        }

        private void UnselectItem(ItemInfo info)
        {
            using (UpdateSelectedCells())
            {
                int itemIndex = info.Index;
                int numColumns = _columns.Count;
                if ((itemIndex >= 0) && (numColumns > 0))
                {
                    _selectedCells.RemoveRegion(itemIndex, 0, 1, numColumns);
                }
            }

            UpdateSelectedItems(info, /*Add = */ false);
        }

        /// <summary>
        ///     Adds or Removes from SelectedItems when deferred selection is not handled by the caller.
        /// </summary>
        private void UpdateSelectedItems(ItemInfo info, bool add)
        {
            bool updatingSelectedItems = IsUpdatingSelectedItems;
            if (!updatingSelectedItems)
            {
                BeginUpdateSelectedItems();
            }

            try
            {
                if (add)
                {
                    SelectedItemCollection.Add(info.Clone());
                }
                else
                {
                    SelectedItemCollection.Remove(info);
                }
            }
            finally
            {
                if (!updatingSelectedItems)
                {
                    EndUpdateSelectedItems();
                }
            }
        }

        /// <summary>
        ///     When changing SelectedCells, do:
        ///     using (UpdateSelectedCells())
        ///     {
        ///         ...
        ///     }
        /// </summary>
        private IDisposable UpdateSelectedCells()
        {
            return new ChangingSelectedCellsHelper(this);
        }

        private void BeginUpdateSelectedCells()
        {
            Debug.Assert(!IsUpdatingSelectedCells);
            _updatingSelectedCells = true;
        }

        private void EndUpdateSelectedCells()
        {
            Debug.Assert(IsUpdatingSelectedCells);

            UpdateIsSelected();
            _updatingSelectedCells = false;

            NotifySelectedCellsChanged();
        }

        private bool IsUpdatingSelectedCells
        {
            get { return _updatingSelectedCells; }
        }

        /// <summary>
        ///     Handles tracking defered selection change notifications for selected cells.
        /// </summary>
        private class ChangingSelectedCellsHelper : IDisposable
        {
            internal ChangingSelectedCellsHelper(DataGrid dataGrid)
            {
                _dataGrid = dataGrid;
                _wasUpdatingSelectedCells = _dataGrid.IsUpdatingSelectedCells;
                if (!_wasUpdatingSelectedCells)
                {
                    _dataGrid.BeginUpdateSelectedCells();
                }
            }

            public void Dispose()
            {
                GC.SuppressFinalize(this);
                if (!_wasUpdatingSelectedCells)
                {
                    _dataGrid.EndUpdateSelectedCells();
                }
            }

            private DataGrid _dataGrid;
            private bool _wasUpdatingSelectedCells;
        }

        /// <summary>
        ///     SHIFT is down or performing a drag selection.
        ///     Multiple items can be selected.
        ///     There is a selection anchor.
        /// </summary>
        private bool ShouldExtendSelection
        {
            get
            {
                return CanSelectMultipleItems && (_selectionAnchor != null) &&
                    (_isDraggingSelection || ((Keyboard.Modifiers & ModifierKeys.Shift) == ModifierKeys.Shift));
            }
        }

        /// <summary>
        ///     CTRL is down.
        ///     Previous selection should not be cleared, or a selected item should be toggled.
        /// </summary>
        private static bool ShouldMinimallyModifySelection
        {
            get
            {
                return (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control;
            }
        }

        private bool CanSelectRows
        {
            get
            {
                switch (SelectionUnit)
                {
                    case DataGridSelectionUnit.FullRow:
                    case DataGridSelectionUnit.CellOrRowHeader:
                        return true;

                    case DataGridSelectionUnit.Cell:
                        return false;
                }

                Debug.Fail("Unknown SelectionUnit encountered.");
                return false;
            }
        }

        private void OnItemsCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            _currentCellContainer = null;
            List<Tuple<int,int>> ranges = null;

            using (UpdateSelectedCells())
            {
                if (e.Action == NotifyCollectionChangedAction.Reset)
                {
                    ranges = new List<Tuple<int,int>>();
                    LocateSelectedItems(ranges);
                }

                // Send the change notification to the selected cells collection
                _selectedCells.OnItemsCollectionChanged(e, ranges);
            }

            if (e.Action == NotifyCollectionChangedAction.Remove || e.Action == NotifyCollectionChangedAction.Replace)
            {
                foreach (object item in e.OldItems)
                {
                    _itemAttachedStorage.ClearItem(item);
                }
            }
            else if (e.Action == NotifyCollectionChangedAction.Reset)
            {
                _itemAttachedStorage.Clear();
            }
        }

        #endregion

        #region Input

        private static void OnIsEnabledChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            d.CoerceValue(CanUserAddRowsProperty);
            d.CoerceValue(CanUserDeleteRowsProperty);

            // Many commands use IsEnabled to determine if they are enabled or not
            CommandManager.InvalidateRequerySuggested();

            ((DataGrid)d).UpdateVisualState();
        }

        /// <summary>
        /// Cells need to update their visual state when this property changes.
        /// </summary>
        /// <param name="d"></param>
        /// <param name="e"></param>
        private static void OnIsKeyboardFocusWithinChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((DataGrid)d).NotifyPropertyChanged(d, e, DataGridNotificationTarget.Rows | DataGridNotificationTarget.RowHeaders | DataGridNotificationTarget.Cells);
        }

        /// <summary>
        ///     Called when a TextInput event is received.
        /// </summary>
        /// <param name="e"></param>
        protected override void OnTextInput(TextCompositionEventArgs e)
        {
            base.OnTextInput(e);

            // Only handle text from ourselves or an item container
            if (!e.Handled && !String.IsNullOrEmpty(e.Text) && IsTextSearchEnabled)
            {
                bool shouldProcess = (e.OriginalSource == this);
                if (!shouldProcess)
                {
                    ItemsControl itemsControl = ItemsControlFromItemContainer(e.OriginalSource as DependencyObject);
                    shouldProcess = (itemsControl == this);
                    if (!shouldProcess)
                    {
                        DataGridCellsPresenter cellsPresenter = itemsControl as DataGridCellsPresenter;
                        if (cellsPresenter != null)
                        {
                            shouldProcess = (cellsPresenter.DataGridOwner == this);
                        }
                    }
                }

                if (shouldProcess)
                {
                    TextSearch instance = TextSearch.EnsureInstance(this);

                    if (instance != null)
                    {
                        instance.DoSearch(e.Text);
                        // Note: we always want to handle the event to denote that we
                        // actually did something.  We wouldn't want an AccessKey
                        // to get invoked just because there wasn't a match here.
                        e.Handled = true;
                    }
                }
            }
        }

        /// <summary>
        ///     Called when an item is being focused
        /// </summary>
        internal override bool FocusItem(ItemInfo info, ItemNavigateArgs itemNavigateArgs)
        {
            object item = info.Item;
            bool returnValue = false;
            if (item != null)
            {
                DataGridColumn column = CurrentColumn;
                if (column == null)
                {
                    SetCurrentItem(item);
                }
                else
                {
                    DataGridCell cell = TryFindCell(info, column);
                    if (cell != null)
                    {
                        cell.Focus();
                        if (ShouldSelectRowHeader)
                        {
                            HandleSelectionForRowHeaderAndDetailsInput(cell.RowOwner, /* startDragging = */ false);
                        }
                        else
                        {
                            HandleSelectionForCellInput(cell, /* startDragging = */ false, /* allowsExtendSelect = */ false, /* allowsMinimalSelect = */ false);
                        }
                    }
                }
            }
            if (itemNavigateArgs.DeviceUsed is KeyboardDevice)
            {
                KeyboardNavigation.ShowFocusVisual();
            }
            return returnValue;
        }

        /// <summary>
        ///     Called when a keyboard key is pressed.
        /// </summary>
        protected override void OnKeyDown(KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.Tab:
                    OnTabKeyDown(e);
                    break;

                case Key.Enter:
                    OnEnterKeyDown(e);
                    break;

                case Key.Left:
                case Key.Right:
                case Key.Up:
                case Key.Down:
                    OnArrowKeyDown(e);
                    break;

                case Key.Home:
                case Key.End:
                    OnHomeOrEndKeyDown(e);
                    break;

                case Key.PageUp:
                case Key.PageDown:
                    OnPageUpOrDownKeyDown(e);
                    break;
            }

            if (!e.Handled)
            {
                base.OnKeyDown(e);
            }
        }

        private static FocusNavigationDirection KeyToTraversalDirection(Key key)
        {
            switch (key)
            {
                case Key.Left:
                    return FocusNavigationDirection.Left;

                case Key.Right:
                    return FocusNavigationDirection.Right;

                case Key.Up:
                    return FocusNavigationDirection.Up;

                case Key.Down:
                default:
                    return FocusNavigationDirection.Down;
            }
        }

        /// <summary>
        ///     Helper method which handles the arrow key down
        /// </summary>
        /// <remarks>
        ///     ADO.Net has a bug (#524977) where if the row is in edit mode
        ///     and atleast one of the cells are edited and committed without
        ///     commiting the row itself, DataView.IndexOf for that row returns -1
        ///     and DataView.Contains returns false. The Workaround to this problem
        ///     is to try to use the previously computed row index if the operations
        ///     are in the same row scope.
        /// </remarks>
        private void OnArrowKeyDown(KeyEventArgs e)
        {
            DataGridCell currentCellContainer = CurrentCellContainer;
            if (currentCellContainer != null)
            {
                e.Handled = true;
                bool wasEditing = currentCellContainer.IsEditing;
                KeyboardNavigation keyboardNavigation = KeyboardNavigation.Current;

                UIElement startElement = Keyboard.FocusedElement as UIElement;
                ContentElement startContentElement = (startElement == null) ? Keyboard.FocusedElement as ContentElement : null;
                if ((startElement != null) || (startContentElement != null))
                {
                    bool navigateFromCellContainer = e.OriginalSource == currentCellContainer;
                    if (navigateFromCellContainer)
                    {
                        KeyboardNavigationMode keyboardNavigationMode = KeyboardNavigation.GetDirectionalNavigation(this);
                        if (keyboardNavigationMode == KeyboardNavigationMode.Once)
                        {
                            // KeyboardNavigation will move the focus out of the DataGrid
                            DependencyObject nextFocusTarget = this.PredictFocus(KeyToTraversalDirection(e.Key));
                            if (nextFocusTarget != null && !keyboardNavigation.IsAncestorOfEx(this, nextFocusTarget))
                            {
                                Keyboard.Focus(nextFocusTarget as IInputElement);
                            }

                            return;
                        }

                        int currentDisplayIndex = this.CurrentColumn.DisplayIndex;
                        ItemInfo currentInfo = CurrentInfo;
                        int currentRowIndex = currentInfo.Index;

                        int nextDisplayIndex = currentDisplayIndex;
                        int nextRowIndex = currentRowIndex;
                        bool controlModifier = ((e.KeyboardDevice.Modifiers & ModifierKeys.Control) == ModifierKeys.Control);

                        // if we're at a boundary, try using default navigation - it can
                        // go to an element in a header/footer, rather than in another row.
                        if (!controlModifier && (e.Key == Key.Up || e.Key == Key.Down))
                        {
                            bool tryDefaultNavigation = false;

                            if (currentInfo.Item == CollectionView.NewItemPlaceholder)
                            {
                                tryDefaultNavigation = true;
                            }
                            else if (IsGrouping)
                            {
                                GroupItem currentGroupItem = DataGridHelper.FindVisualParent<GroupItem>(currentCellContainer);
                                if (currentGroupItem != null)
                                {
                                    CollectionViewGroupInternal cvg = ItemContainerGenerator.ItemFromContainer(currentGroupItem) as CollectionViewGroupInternal;
                                    if (cvg != null && cvg.Items.Count > 0)
                                    {
                                        // Try default navigation if current item is first or last item of a group.
                                        if ((e.Key == Key.Up && ItemsControl.EqualsEx(cvg.Items[0], currentInfo.Item)) ||
                                            (e.Key == Key.Down && ItemsControl.EqualsEx(cvg.Items[cvg.Items.Count - 1], currentInfo.Item)))
                                        {
                                            // there might be duplicate items, so double-check the index
                                            // (this is mildly expensive, which is why we put it off until necessary)
                                            int indexOfGroupBoundary = cvg.LeafIndexFromItem(null, 0);
                                            if (e.Key == Key.Down)
                                            {
                                                indexOfGroupBoundary += cvg.ItemCount - 1;
                                            }
                                            if (currentRowIndex == indexOfGroupBoundary)
                                            {
                                                tryDefaultNavigation = true;
                                            }
                                        }
                                    }
                                }
                            }
                            else
                            {
                                if ((e.Key == Key.Up && currentRowIndex == 0) ||
                                    (e.Key == Key.Down && currentRowIndex == Items.Count - 1))
                                {
                                    tryDefaultNavigation = true;
                                }
                            }

                            if (tryDefaultNavigation)
                            {
                                if (TryDefaultNavigation(e, currentInfo))
                                    return;
                            }
                        }

                        // Reverse the navigation in RTL flow direction
                        Key rtlKey = e.Key;
                        if (this.FlowDirection == FlowDirection.RightToLeft)
                        {
                            if (rtlKey == Key.Left)
                            {
                                rtlKey = Key.Right;
                            }
                            else if (rtlKey == Key.Right)
                            {
                                rtlKey = Key.Left;
                            }
                        }

                        switch (rtlKey)
                        {
                            case Key.Left:
                                if (controlModifier)
                                {
                                    nextDisplayIndex = InternalColumns.FirstVisibleDisplayIndex;
                                }
                                else
                                {
                                    nextDisplayIndex--;
                                    while (nextDisplayIndex >= 0)
                                    {
                                        DataGridColumn column = ColumnFromDisplayIndex(nextDisplayIndex);
                                        if (column.IsVisible)
                                        {
                                            break;
                                        }

                                        nextDisplayIndex--;
                                    }

                                    if (nextDisplayIndex < 0)
                                    {
                                        if (keyboardNavigationMode == KeyboardNavigationMode.Cycle)
                                        {
                                            nextDisplayIndex = InternalColumns.LastVisibleDisplayIndex;
                                        }
                                        else if (keyboardNavigationMode == KeyboardNavigationMode.Contained)
                                        {
                                            DependencyObject nextFocusTarget = keyboardNavigation.PredictFocusedElement(currentCellContainer, KeyToTraversalDirection(rtlKey),
                                                treeViewNavigation:false, considerDescendants:false);
                                            if (nextFocusTarget != null && keyboardNavigation.IsAncestorOfEx(this, nextFocusTarget))
                                            {
                                                Keyboard.Focus(nextFocusTarget as IInputElement);
                                            }
                                            return;
                                        }
                                        else // Continue, Local, None - move focus out of the datagrid
                                        {
                                            MoveFocus(new TraversalRequest(e.Key == Key.Left ? FocusNavigationDirection.Left : FocusNavigationDirection.Right));
                                            return;
                                        }
                                    }
                                }

                                break;

                            case Key.Right:
                                if (controlModifier)
                                {
                                    nextDisplayIndex = Math.Max(0, InternalColumns.LastVisibleDisplayIndex);
                                }
                                else
                                {
                                    nextDisplayIndex++;
                                    int columnCount = Columns.Count;
                                    while (nextDisplayIndex < columnCount)
                                    {
                                        DataGridColumn column = ColumnFromDisplayIndex(nextDisplayIndex);
                                        if (column.IsVisible)
                                        {
                                            break;
                                        }

                                        nextDisplayIndex++;
                                    }

                                    if (nextDisplayIndex >= Columns.Count)
                                    {
                                        if (keyboardNavigationMode == KeyboardNavigationMode.Cycle)
                                        {
                                            nextDisplayIndex = InternalColumns.FirstVisibleDisplayIndex;
                                        }
                                        else if (keyboardNavigationMode == KeyboardNavigationMode.Contained)
                                        {
                                            DependencyObject nextFocusTarget = keyboardNavigation.PredictFocusedElement(currentCellContainer, KeyToTraversalDirection(rtlKey),
                                                treeViewNavigation:false, considerDescendants:false);
                                            if (nextFocusTarget != null && keyboardNavigation.IsAncestorOfEx(this, nextFocusTarget))
                                            {
                                                Keyboard.Focus(nextFocusTarget as IInputElement);
                                            }
                                            return;
                                        }
                                        else // Continue, Local, None - move focus out of the datagrid
                                        {
                                            MoveFocus(new TraversalRequest(e.Key == Key.Left ? FocusNavigationDirection.Left : FocusNavigationDirection.Right));
                                            return;
                                        }
                                    }
                                }

                                break;

                            case Key.Up:
                                if (controlModifier)
                                {
                                    nextRowIndex = 0;
                                }
                                else
                                {
                                    nextRowIndex--;
                                    if (nextRowIndex < 0)
                                    {
                                        if (keyboardNavigationMode == KeyboardNavigationMode.Cycle)
                                        {
                                            nextRowIndex = Items.Count - 1;
                                        }
                                        else if (keyboardNavigationMode == KeyboardNavigationMode.Contained)
                                        {
                                            DependencyObject nextFocusTarget = keyboardNavigation.PredictFocusedElement(currentCellContainer, KeyToTraversalDirection(rtlKey),
                                                treeViewNavigation:false, considerDescendants:false);
                                            if (nextFocusTarget != null && keyboardNavigation.IsAncestorOfEx(this, nextFocusTarget))
                                            {
                                                Keyboard.Focus(nextFocusTarget as IInputElement);
                                            }
                                            return;
                                        }
                                        else // Continue, Local, None - move focus out of the datagrid
                                        {
                                            MoveFocus(new TraversalRequest(FocusNavigationDirection.Up));
                                            return;
                                        }
                                    }
                                }

                                break;

                            case Key.Down:
                            default:
                                if (controlModifier)
                                {
                                    nextRowIndex = Math.Max(0, Items.Count - 1);
                                }
                                else
                                {
                                    nextRowIndex++;
                                    if (nextRowIndex >= Items.Count)
                                    {
                                        if (keyboardNavigationMode == KeyboardNavigationMode.Cycle)
                                        {
                                            nextRowIndex = 0;
                                        }
                                        else if (keyboardNavigationMode == KeyboardNavigationMode.Contained)
                                        {
                                            DependencyObject nextFocusTarget = keyboardNavigation.PredictFocusedElement(currentCellContainer, KeyToTraversalDirection(rtlKey),
                                                treeViewNavigation:false, considerDescendants:false);
                                            if (nextFocusTarget != null && keyboardNavigation.IsAncestorOfEx(this, nextFocusTarget))
                                            {
                                                Keyboard.Focus(nextFocusTarget as IInputElement);
                                            }
                                            return;
                                        }
                                        else // Continue, Local, None - move focus out of the datagrid
                                        {
                                            MoveFocus(new TraversalRequest(FocusNavigationDirection.Down));
                                            return;
                                        }
                                    }
                                }

                                break;
                        }

                        DataGridColumn nextColumn = ColumnFromDisplayIndex(nextDisplayIndex);
                        ItemInfo nextInfo = ItemInfoFromIndex(nextRowIndex);
                        ScrollCellIntoView(nextInfo, nextColumn);
                        DataGridCell nextCellContainer = TryFindCell(nextInfo, nextColumn);

                        if (nextCellContainer == null || nextCellContainer == currentCellContainer || !nextCellContainer.Focus())
                        {
                            return;
                        }
                    }

                    else
                    {
                        if (TryDefaultNavigation(e, null))
                            return;
                    }

                    // Attempt to move focus
                    TraversalRequest request = new TraversalRequest(KeyToTraversalDirection(e.Key));
                    if (navigateFromCellContainer ||
                        ((startElement != null) && startElement.MoveFocus(request)) ||
                        ((startContentElement != null) && startContentElement.MoveFocus(request)))
                    {
                        SelectAndEditOnFocusMove(e, currentCellContainer, wasEditing, /* allowsExtendSelect = */ true, /* ignoreControlKey = */ true);
                    }
                }
            }
        }

        private bool TryDefaultNavigation(KeyEventArgs e, ItemInfo currentInfo)
        {
            FrameworkElement focusContainer;
            FrameworkElement startingElement = Keyboard.FocusedElement as FrameworkElement;

            if (startingElement != null && ItemsHost.IsAncestorOf(startingElement))
            {
                PrepareNavigateByLine(currentInfo,
                    startingElement,
                    (e.Key == Key.Up ? FocusNavigationDirection.Up : FocusNavigationDirection.Down),
                    new ItemNavigateArgs(e.KeyboardDevice, Keyboard.Modifiers),
                    out focusContainer);

                if (focusContainer != null)
                {
                    DataGridRow focusRow = DataGridHelper.FindVisualParent<DataGridRow>(focusContainer);
                    if (focusRow == null ||
                        focusRow.DataGridOwner != this)
                    {
                        focusContainer.Focus();
                        return true;
                    }
                }
            }

            return false;
        }

        /// <summary>
        ///     Called when the tab key is pressed to perform focus navigation.
        /// </summary>
        private void OnTabKeyDown(KeyEventArgs e)
        {
            // When the end-user uses the keyboard to tab to another cell while the current cell
            // is in edit-mode, then the next cell should enter edit mode in addition to gaining
            // focus. There is no way to detect this from the focus change events, so the cell
            // is going to handle the complete operation manually.
            // The standard focus change method is being called here, so even if focus moves
            // to something other than a cell, focus should land on the element that it would
            // have landed on anyway.
            DataGridCell currentCellContainer = CurrentCellContainer;
            if (currentCellContainer != null)
            {
                bool wasEditing = currentCellContainer.IsEditing;
                bool previous = ((e.KeyboardDevice.Modifiers & ModifierKeys.Shift) == ModifierKeys.Shift);

                // Start navigation from the current focus to allow moveing focus on other focusable elements inside the cell
                UIElement startElement = Keyboard.FocusedElement as UIElement;
                ContentElement startContentElement = (startElement == null) ? Keyboard.FocusedElement as ContentElement : null;
                if ((startElement != null) || (startContentElement != null))
                {
                    e.Handled = true;

                    FocusNavigationDirection direction = previous ? FocusNavigationDirection.Previous : FocusNavigationDirection.Next;
                    TraversalRequest request = new TraversalRequest(direction);

                    // Move focus to the the next or previous tab stop.
                    if (((startElement != null) && startElement.MoveFocus(request)) ||
                        ((startContentElement != null) && startContentElement.MoveFocus(request)))
                    {
                        // If focus moved to the cell while in edit mode - keep navigating to the previous cell
                        if (wasEditing && previous && Keyboard.FocusedElement == currentCellContainer)
                        {
                            currentCellContainer.MoveFocus(request);
                        }

                        // In case of grouping if a row level commit happened due to
                        // the previous focus change, the container of the row gets
                        // removed from the visual tree by the CollectionView,
                        // but we still hang on to a cell of that row, which will be used
                        // by the call to SelectAndEditOnFocusMove. Hence re-establishing the
                        // focus appropriately in such cases.
                        if (IsGrouping && wasEditing)
                        {
                            DataGridCell newCell = GetCellForSelectAndEditOnFocusMove();

                            if (newCell != null &&
                                newCell.RowDataItem == currentCellContainer.RowDataItem)
                            {
                                DataGridCell realNewCell = TryFindCell(newCell.RowDataItem, newCell.Column);

                                // Forcing an UpdateLayout since the generation of the new row
                                // container which was removed earlier is done in measure.
                                if (realNewCell == null)
                                {
                                    UpdateLayout();
                                    realNewCell = TryFindCell(newCell.RowDataItem, newCell.Column);
                                }
                                if (realNewCell != null && realNewCell != newCell)
                                {
                                    realNewCell.Focus();
                                }
                            }
                        }

                        // When doing TAB and SHIFT+TAB focus movement, don't confuse the selection
                        // code, which also relies on SHIFT to know whether to extend selection or not.
                        SelectAndEditOnFocusMove(e, currentCellContainer, wasEditing, /* allowsExtendSelect = */ false, /* ignoreControlKey = */ true);
                    }
                }
            }
        }

        private void OnEnterKeyDown(KeyEventArgs e)
        {
            DataGridCell currentCellContainer = CurrentCellContainer;
            if ((currentCellContainer != null) && (_columns.Count > 0))
            {
                e.Handled = true;

                DataGridColumn column = currentCellContainer.Column;

                // Commit any current edit
                if (CommitAnyEdit() && ((e.KeyboardDevice.Modifiers & ModifierKeys.Control) == 0))
                {
                    bool shiftModifier = ((e.KeyboardDevice.Modifiers & ModifierKeys.Shift) == ModifierKeys.Shift);

                    // Go to the next row, keeping the column the same
                    int numItems = Items.Count;
                    int index = CurrentInfo.Index;
                    if (index < 0)
                    {
                        index = Items.IndexOf(CurrentItem);
                    }
                    index = Math.Max(0, Math.Min(numItems - 1, index + (shiftModifier ? -1 : 1)));

                    if (index < numItems)
                    {
                        ItemInfo rowInfo = ItemInfoFromIndex(index);
                        ScrollIntoView(rowInfo, column);

                        if (!ItemsControl.EqualsEx(CurrentCell.Item, rowInfo.Item))
                        {
                            // Focus the new cell
                            SetCurrentValueInternal(CurrentCellProperty, new DataGridCellInfo(rowInfo, column, this));

                            // Will never edit on ENTER, so just say that the old cell wasn't in edit mode
                            SelectAndEditOnFocusMove(e, currentCellContainer, /* wasEditing = */ false, /* allowsExtendSelect = */ false, /* ignoreControlKey = */ true);
                        }
                        else
                        {
                            // When the new item jumped to the bottom, CurrentCell doesn't actually change,
                            // but there is a new container.
                            currentCellContainer = CurrentCellContainer;
                            if (currentCellContainer != null)
                            {
                                currentCellContainer.Focus();
                            }
                        }
                    }
                }
            }
        }

        private DataGridCell GetCellForSelectAndEditOnFocusMove()
        {
            DataGridCell newCell = Keyboard.FocusedElement as DataGridCell;

            // If focus has moved within DataGridCell use CurrentCellContainer
            if (newCell == null && CurrentCellContainer != null && CurrentCellContainer.IsKeyboardFocusWithin)
            {
                newCell = CurrentCellContainer;
            }

            return newCell;
        }

        private void SelectAndEditOnFocusMove(KeyEventArgs e, DataGridCell oldCell, bool wasEditing, bool allowsExtendSelect, bool ignoreControlKey)
        {
            DataGridCell newCell = GetCellForSelectAndEditOnFocusMove();

            if ((newCell != null) && (newCell.DataGridOwner == this))
            {
                if (ignoreControlKey || ((e.KeyboardDevice.Modifiers & ModifierKeys.Control) == 0))
                {
                    if (ShouldSelectRowHeader && allowsExtendSelect)
                    {
                        HandleSelectionForRowHeaderAndDetailsInput(newCell.RowOwner, /* startDragging = */ false);
                    }
                    else
                    {
                        HandleSelectionForCellInput(newCell, /* startDragging = */ false, allowsExtendSelect, /* allowsMinimalSelect = */ false);
                    }
                }

                // If focus moved to a new cell within the same row that didn't
                // decide on its own to enter edit mode, put it in edit mode.
                if (wasEditing && !newCell.IsEditing && (oldCell.RowDataItem == newCell.RowDataItem))
                {
                    BeginEdit(e);
                }
            }
        }

        private void OnHomeOrEndKeyDown(KeyEventArgs e)
        {
            if ((_columns.Count > 0) && (Items.Count > 0))
            {
                e.Handled = true;

                bool homeKey = (e.Key == Key.Home);
                bool controlModifier = ((e.KeyboardDevice.Modifiers & ModifierKeys.Control) == ModifierKeys.Control);

                if (controlModifier)
                {
                    ScrollViewer scrollHost = InternalScrollHost;
                    if (scrollHost != null)
                    {
                        // This operation is particularly relevant when the items in the DataGrid are
                        // grouped. We want the Group Headers to be brought into view when Ctrl+Home
                        // is pressed.

                        if (homeKey)
                        {
                            scrollHost.ScrollToHome();
                        }
                        else
                        {
                            scrollHost.ScrollToEnd();
                        }
                    }
                }

                // Go to the first or last cell
                ItemInfo info = controlModifier ? ItemInfoFromIndex(homeKey ? 0 : Items.Count - 1) : CurrentInfo;
                DataGridColumn column = ColumnFromDisplayIndex(homeKey ? InternalColumns.FirstVisibleDisplayIndex : InternalColumns.LastVisibleDisplayIndex);

                ScrollCellIntoView(info, column);

                DataGridCell cell = TryFindCell(info, column);
                if (cell != null)
                {
                    cell.Focus();
                    if (ShouldSelectRowHeader)
                    {
                        HandleSelectionForRowHeaderAndDetailsInput(cell.RowOwner, /* startDragging = */ false);
                    }
                    else
                    {
                        HandleSelectionForCellInput(cell, /* startDragging = */ false, /* allowsExtendSelect = */ true, /* allowsMinimalSelect = */ false);
                    }
                }
            }
        }

        private void OnPageUpOrDownKeyDown(KeyEventArgs e)
        {
            // This code relies on DataGridRowsPresenter since ScrollHost relies
            // on InternalItemsHost, which relies on DataGridRowsPresenter.
            // Additionally, it relies on ViewportHeight being in logical units
            // instead of pixels.
            ScrollViewer scrollHost = InternalScrollHost;
            if (scrollHost != null)
            {
                e.Handled = true;

                ItemInfo currentInfo = CurrentInfo;

                if (VirtualizingPanel.GetScrollUnit(this) == ScrollUnit.Item && !IsGrouping)
                {
                    int rowIndex = currentInfo.Index;
                    if (rowIndex >= 0)
                    {
                        // Predict the page up/page down item based on the viewport height, which
                        // should be in logical units.
                        // This is not going to work well when the rows have different heights, but
                        // it is the best estimate we have at the moment.
                        int jumpDistance = Math.Max(1, (int)scrollHost.ViewportHeight - 1);
                        int targetIndex = (e.Key == Key.PageUp) ? rowIndex - jumpDistance : rowIndex + jumpDistance;
                        targetIndex = Math.Max(0, Math.Min(targetIndex, Items.Count - 1));

                        // Scroll the target row into view, keeping the current column
                        ItemInfo targetInfo = ItemInfoFromIndex(targetIndex);
                        DataGridColumn currentColumn = CurrentColumn;

                        if (currentColumn == null)
                        {
                            OnBringItemIntoView(targetInfo);
                            SetCurrentItem(targetInfo.Item);
                        }
                        else
                        {
                            ScrollCellIntoView(targetInfo, currentColumn);
                            DataGridCell cell = TryFindCell(targetInfo, currentColumn);
                            if (cell != null)
                            {
                                cell.Focus();
                                if (ShouldSelectRowHeader)
                                {
                                    HandleSelectionForRowHeaderAndDetailsInput(cell.RowOwner, /* startDragging = */ false);
                                }
                                else
                                {
                                    HandleSelectionForCellInput(cell, /* startDragging = */ false, /* allowsExtendSelect = */ true, /* allowsMinimalSelect = */ false);
                                }
                            }
                        }
                    }
                }
                else
                {
                    FrameworkElement targetElement;
                    FocusNavigationDirection direction = e.Key == Key.PageUp ? FocusNavigationDirection.Up : FocusNavigationDirection.Down;
                    ItemInfo startingInfo = currentInfo;
                    FrameworkElement startingElement = null;
                    if (IsGrouping)
                    {
                        startingElement = Keyboard.FocusedElement as FrameworkElement;
                        if (startingElement != null)
                        {
                            startingInfo = null;
                            DataGridRow startingRow = startingElement as DataGridRow;
                            if (startingRow == null)
                            {
                                startingRow = DataGridHelper.FindVisualParent<DataGridRow>(startingElement);
                            }

                            if (startingRow != null)
                            {
                                DataGrid focusedDataGrid = ItemsControl.ItemsControlFromItemContainer(startingRow) as DataGrid;
                                if (focusedDataGrid == this)
                                {
                                    startingInfo = ItemInfoFromContainer(startingRow);
                                }
                            }
                        }
                    }
                    PrepareToNavigateByPage(startingInfo, startingElement, direction, new ItemNavigateArgs(Keyboard.PrimaryDevice, Keyboard.Modifiers), out targetElement);

                    DataGridRow targetRowElement = targetElement as DataGridRow;
                    if (targetRowElement == null)
                    {
                        targetRowElement = DataGridHelper.FindVisualParent<DataGridRow>(targetElement);
                    }

                    if (targetRowElement != null)
                    {
                        // Scroll the target row into view, keeping the current column
                        ItemInfo targetInfo = ItemInfoFromContainer(targetRowElement);
                        DataGridColumn currentColumn = CurrentColumn;

                        if (currentColumn == null)
                        {
                            SetCurrentItem(targetInfo.Item);
                        }
                        else
                        {
                            DataGridCell cell = TryFindCell(targetInfo, currentColumn);
                            if (cell != null)
                            {
                                cell.Focus();
                                if (ShouldSelectRowHeader)
                                {
                                    HandleSelectionForRowHeaderAndDetailsInput(cell.RowOwner, /* startDragging = */ false);
                                }
                                else
                                {
                                    HandleSelectionForCellInput(cell, /* startDragging = */ false, /* allowsExtendSelect = */ true, /* allowsMinimalSelect = */ false);
                                }
                            }
                        }
                    }
                    else if (targetElement != null)
                    {
                        targetElement.Focus();
                    }
                }
            }
        }

        /// <summary>
        ///     Continues a drag selection.
        /// </summary>
        protected override void OnMouseMove(MouseEventArgs e)
        {
            if (_isDraggingSelection)
            {
                if (e.LeftButton == MouseButtonState.Pressed)
                {
                    // Check that the mouse has moved relative to the DataGrid.
                    // This check prevents the case where a row is partially visible
                    // at the bottom. If this row is clicked, then it will be scrolled
                    // into view and away from the mouse. The mouse will then appear
                    // (according to these messages) as if it moved over a new cell, and
                    // could invoke a drag, but the actual mouse position relative to
                    // the DataGrid hasn't changed.
                    Point currentMousePosition = Mouse.GetPosition(this);
                    if (!DoubleUtil.AreClose(currentMousePosition, _dragPoint))
                    {
                        _dragPoint = currentMousePosition;

                        RelativeMousePositions position = RelativeMousePosition;
                        if (position == RelativeMousePositions.Over)
                        {
                            // The mouse is within the field of cells and rows, use the actual
                            // elements to determine changes to selection.
                            if (_isRowDragging)
                            {
                                DataGridRow row = MouseOverRow;
                                if ((row != null) && (row.Item != CurrentItem))
                                {
                                    // Continue a row header drag to the given row
                                    HandleSelectionForRowHeaderAndDetailsInput(row, /* startDragging = */ false);
                                    SetCurrentItem(row.Item);
                                    e.Handled = true;
                                }
                            }
                            else
                            {
                                DataGridCell cell = MouseOverCell;
                                if (cell == null)
                                {
                                    DataGridRow row = MouseOverRow;
                                    if (row != null)
                                    {
                                        // The mouse is over a row but not necessarily a cell,
                                        // such as over a header or details section. Find the
                                        // nearest cell and use that.
                                        cell = GetCellNearMouse();
                                    }
                                }

                                if ((cell != null) && (cell != CurrentCellContainer))
                                {
                                    HandleSelectionForCellInput(cell, /* startDragging = */ false, /* allowsExtendSelect = */ true, /* allowsMinimalSelect = */ true);
                                    cell.Focus();
                                    e.Handled = true;
                                }
                            }
                        }
                        else
                        {
                            // The mouse is outside of the field of cells and rows.
                            if (_isRowDragging && IsMouseToLeftOrRightOnly(position))
                            {
                                // Figure out which row the mouse is in-line with and select it
                                DataGridRow row = GetRowNearMouse();
                                if ((row != null) && (row.Item != CurrentItem))
                                {
                                    // The mouse is directly to the left or right of the row
                                    HandleSelectionForRowHeaderAndDetailsInput(row, /* startDragging = */ false);
                                    SetCurrentItem(row.Item);
                                    e.Handled = true;
                                }
                            }
                            else if (_hasAutoScrolled)
                            {
                                // The mouse is outside the grid, and we've started auto-scrolling.
                                // The user has moved the mouse and would like a quick update.
                                if (DoAutoScroll())
                                {
                                    e.Handled = true;
                                }
                            }
                            else
                            {
                                // Ensure that the auto-scroll timer has started
                                StartAutoScroll();
                            }
                        }
                    }
                }
                else
                {
                    // The mouse button is up, end the drag operation
                    EndDragging();
                }
            }
        }

        private static void OnAnyMouseUpThunk(object sender, MouseButtonEventArgs e)
        {
            ((DataGrid)sender).OnAnyMouseUp(e);
        }

        /// <summary>
        ///     Ends a drag selection.
        /// </summary>
        private void OnAnyMouseUp(MouseButtonEventArgs e)
        {
            EndDragging();
        }

        /// <summary>
        ///     When a ContextMenu opens on a cell that isn't selected, it should
        ///     become selected.
        /// </summary>
        protected override void OnContextMenuOpening(ContextMenuEventArgs e)
        {
            //
            // We do not want to change selection when the DataGrid is disabled.
            //
            if (!IsEnabled)
            {
                return;
            }

            DataGridCell cell = null;
            DataGridRowHeader rowHeader = null;
            UIElement sourceElement = e.OriginalSource as UIElement;
            while (sourceElement != null)
            {
                cell = sourceElement as DataGridCell;
                if (cell != null)
                {
                    break;
                }

                rowHeader = sourceElement as DataGridRowHeader;
                if (rowHeader != null)
                {
                    break;
                }

                sourceElement = VisualTreeHelper.GetParent(sourceElement) as UIElement;
            }

            if ((cell != null) && !cell.IsSelected && !cell.IsKeyboardFocusWithin)
            {
                cell.Focus();
                HandleSelectionForCellInput(cell, /* startDragging = */ false, /* allowsExtendSelect = */ true, /* allowsMinimalSelect = */ true);
            }

            if (rowHeader != null)
            {
                DataGridRow parentRow = rowHeader.ParentRow;
                if (parentRow != null && !parentRow.IsSelected)
                {
                    HandleSelectionForRowHeaderAndDetailsInput(parentRow, /* startDragging = */ false);
                }
            }
        }

        /// <summary>
        ///     Finds the row that contains the mouse's Y coordinate.
        /// </summary>
        /// <remarks>
        ///     Relies on InternalItemsHost.
        ///     Meant to be used when the mouse is outside the DataGrid.
        /// </remarks>
        private DataGridRow GetRowNearMouse()
        {
            Debug.Assert(RelativeMousePosition != RelativeMousePositions.Over, "The mouse is not supposed to be over the DataGrid.");

            Panel itemsHost = InternalItemsHost;
            if (itemsHost != null)
            {
                bool isGrouping = IsGrouping;

                // Iterate from the end to the beginning since it is more common
                // to drag toward the end.
                for (int i = (isGrouping ? Items.Count - 1 : itemsHost.Children.Count - 1); i >= 0; i--)
                {
                    DataGridRow row = null;

                    if (isGrouping)
                    {
                        // If Grouping is enabled, Children of itemsHost are not always DataGridRows
                        row = ItemContainerGenerator.ContainerFromIndex(i) as DataGridRow;
                    }
                    else
                    {
                        row = itemsHost.Children[i] as DataGridRow;
                    }

                    if (row != null)
                    {
                        Point pt = Mouse.GetPosition(row);
                        Rect rowBounds = new Rect(new Point(), row.RenderSize);
                        if ((pt.Y >= rowBounds.Top) && (pt.Y <= rowBounds.Bottom))
                        {
                            // The mouse cursor's Y position is within the Y bounds of the row
                            return row;
                        }
                    }
                }
            }

            return null;
        }

        /// <summary>
        ///     Finds the cell that is nearest to the mouse.
        /// </summary>
        /// <remarks>
        ///     Relies on InternalItemsHost.
        /// </remarks>
        private DataGridCell GetCellNearMouse()
        {
            Panel itemsHost = InternalItemsHost;
            if (itemsHost != null)
            {
                Rect itemsHostBounds = new Rect(new Point(), itemsHost.RenderSize);
                double closestDistance = Double.PositiveInfinity;
                DataGridCell closestCell = null;
                bool isMouseInCorner = IsMouseInCorner(RelativeMousePosition);
                bool isGrouping = IsGrouping;

                // Iterate from the end to the beginning since it is more common
                // to drag toward the end.
                for (int i = (isGrouping ? Items.Count - 1 : itemsHost.Children.Count - 1); i >= 0; i--)
                {
                    DataGridRow row = null;

                    if (isGrouping)
                    {
                        // If Grouping is enabled, Children of itemsHost are not always DataGridRows
                        row = ItemContainerGenerator.ContainerFromIndex(i) as DataGridRow;
                    }
                    else
                    {
                        row = itemsHost.Children[i] as DataGridRow;
                    }

                    if (row != null)
                    {
                        DataGridCellsPresenter cellsPresenter = row.CellsPresenter;
                        if (cellsPresenter != null)
                        {
                            // Go through all of the instantiated cells and find the closest cell
                            ContainerTracking<DataGridCell> cellTracker = cellsPresenter.CellTrackingRoot;
                            while (cellTracker != null)
                            {
                                DataGridCell cell = cellTracker.Container;

                                double cellDistance;
                                if (CalculateCellDistance(cell, row, itemsHost, itemsHostBounds, isMouseInCorner, out cellDistance))
                                {
                                    if ((closestCell == null) || (cellDistance < closestDistance))
                                    {
                                        // This cell's distance is less, so make it the closest cell
                                        closestDistance = cellDistance;
                                        closestCell = cell;
                                    }
                                }

                                cellTracker = cellTracker.Next;
                            }

                            // Check if the header is close
                            DataGridRowHeader rowHeader = row.RowHeader;
                            if (rowHeader != null)
                            {
                                double cellDistance;
                                if (CalculateCellDistance(rowHeader, row, itemsHost, itemsHostBounds, isMouseInCorner, out cellDistance))
                                {
                                    if ((closestCell == null) || (cellDistance < closestDistance))
                                    {
                                        // If the header is the closest, then use the first cell from the row
                                        DataGridCell cell = row.TryGetCell(DisplayIndexMap[0]);
                                        if (cell != null)
                                        {
                                            closestDistance = cellDistance;
                                            closestCell = cell;
                                        }
                                    }
                                }
                            }
                        }
                    }
                }

                return closestCell;
            }

            return null;
        }

        /// <summary>
        ///     Determines if a cell meets the criteria for being chosen. If it does, it
        ///     calculates its a "distance" that can be compared to other cells.
        /// </summary>
        /// <param name="distance">
        ///     A value that represents the distance between the mouse and the cell.
        ///     This is not necessarily an accurate pixel number in some cases.
        /// </param>
        /// <returns>
        ///     true if the cell can be a drag target. false otherwise.
        /// </returns>
        private static bool CalculateCellDistance(FrameworkElement cell, DataGridRow rowOwner, Panel itemsHost, Rect itemsHostBounds, bool isMouseInCorner, out double distance)
        {
            GeneralTransform transform = cell.TransformToAncestor(itemsHost);
            Rect cellBounds = new Rect(new Point(), cell.RenderSize);

            // Limit to only cells that are entirely visible
            if (itemsHostBounds.Contains(transform.TransformBounds(cellBounds)))
            {
                Point pt = Mouse.GetPosition(cell);
                if (isMouseInCorner)
                {
                    // When the mouse is in the corner, go by distance from center of the cell
                    Vector v = new Vector(pt.X - (cellBounds.Width * 0.5), pt.Y - (cellBounds.Height * 0.5));
                    distance = v.Length;
                    return true;
                }
                else
                {
                    Point rowPt = Mouse.GetPosition(rowOwner);
                    Rect rowBounds = new Rect(new Point(), rowOwner.RenderSize);

                    // The mouse should overlap a row or column
                    if ((pt.X >= cellBounds.Left) && (pt.X <= cellBounds.Right))
                    {
                        // The mouse is within a column
                        if ((rowPt.Y >= rowBounds.Top) && (rowPt.Y <= rowBounds.Bottom))
                        {
                            // Mouse is within the cell
                            distance = 0.0;
                        }
                        else
                        {
                            // Mouse is outside but is within a columns horizontal bounds
                            distance = Math.Abs(pt.Y - cellBounds.Top);
                        }

                        return true;
                    }
                    else if ((rowPt.Y >= rowBounds.Top) && (rowPt.Y <= rowBounds.Bottom))
                    {
                        // Mouse is outside but is within a row's vertical bounds
                        distance = Math.Abs(pt.X - cellBounds.Left);
                        return true;
                    }
                }
            }

            distance = Double.PositiveInfinity;
            return false;
        }

        /// <summary>
        ///     The row that the mouse is over.
        /// </summary>
        private DataGridRow MouseOverRow
        {
            get
            {
                UIElement element = Mouse.DirectlyOver as UIElement;
                DataGridRow row = null;

                while (element != null)
                {
                    row = DataGridHelper.FindVisualParent<DataGridRow>(element);
                    if (row == null || row.DataGridOwner == this)
                        break;

                    element = VisualTreeHelper.GetParent(row) as UIElement;
                }

                return row;
            }
        }

        // The cell that the mouse is over.
        private DataGridCell MouseOverCell
        {
            get
            {
                UIElement element = Mouse.DirectlyOver as UIElement;
                DataGridCell cell = null;

                while (element != null)
                {
                    cell = DataGridHelper.FindVisualParent<DataGridCell>(element);
                    if (cell == null || cell.DataGridOwner == this)
                        break;

                    element = VisualTreeHelper.GetParent(cell) as UIElement;
                }

                return cell;
            }
        }

        /// <summary>
        ///     The mouse position relative to the ItemsHost.
        /// </summary>
        /// <remarks>
        ///     Relies on InternalItemsHost.
        /// </remarks>
        private RelativeMousePositions RelativeMousePosition
        {
            get
            {
                RelativeMousePositions position = RelativeMousePositions.Over;

                Panel itemsHost = InternalItemsHost;
                if (itemsHost != null)
                {
                    Point pt = Mouse.GetPosition(itemsHost);
                    Rect bounds = new Rect(new Point(), itemsHost.RenderSize);

                    if (pt.X < bounds.Left)
                    {
                        position |= RelativeMousePositions.Left;
                    }
                    else if (pt.X > bounds.Right)
                    {
                        position |= RelativeMousePositions.Right;
                    }

                    if (pt.Y < bounds.Top)
                    {
                        position |= RelativeMousePositions.Above;
                    }
                    else if (pt.Y > bounds.Bottom)
                    {
                        position |= RelativeMousePositions.Below;
                    }
                }

                return position;
            }
        }

        private static bool IsMouseToLeft(RelativeMousePositions position)
        {
            return (position & RelativeMousePositions.Left) == RelativeMousePositions.Left;
        }

        private static bool IsMouseToRight(RelativeMousePositions position)
        {
            return (position & RelativeMousePositions.Right) == RelativeMousePositions.Right;
        }

        private static bool IsMouseAbove(RelativeMousePositions position)
        {
            return (position & RelativeMousePositions.Above) == RelativeMousePositions.Above;
        }

        private static bool IsMouseBelow(RelativeMousePositions position)
        {
            return (position & RelativeMousePositions.Below) == RelativeMousePositions.Below;
        }

        private static bool IsMouseToLeftOrRightOnly(RelativeMousePositions position)
        {
            return (position == RelativeMousePositions.Left) || (position == RelativeMousePositions.Right);
        }

        private static bool IsMouseInCorner(RelativeMousePositions position)
        {
            return (position != RelativeMousePositions.Over) &&
                (position != RelativeMousePositions.Above) &&
                (position != RelativeMousePositions.Below) &&
                (position != RelativeMousePositions.Left) &&
                (position != RelativeMousePositions.Right);
        }

        [Flags]
        private enum RelativeMousePositions
        {
            Over    = 0x00,
            Above   = 0x01,
            Below   = 0x02,
            Left    = 0x04,
            Right   = 0x08,
        }

        #endregion

        #region Automation

        protected override System.Windows.Automation.Peers.AutomationPeer OnCreateAutomationPeer()
        {
            return new System.Windows.Automation.Peers.DataGridAutomationPeer(this);
        }

        private CellAutomationValueHolder GetCellAutomationValueHolder(object item, DataGridColumn column)
        {
            CellAutomationValueHolder cellAutomationValueHolder;

            if (_editingRowInfo == null || !ItemsControl.EqualsEx(item, _editingRowInfo.Item) ||
                !_editingCellAutomationValueHolders.TryGetValue(column, out cellAutomationValueHolder))
            {
                DataGridCell cell = TryFindCell(item, column);
                cellAutomationValueHolder =
                    (cell != null) ? new CellAutomationValueHolder(cell)
                                   : new CellAutomationValueHolder(item, column);
            }

            return cellAutomationValueHolder;
        }

        internal string GetCellAutomationValue(object item, DataGridColumn column)
        {
            CellAutomationValueHolder cellAutomationValueHolder = GetCellAutomationValueHolder(item, column);
            return cellAutomationValueHolder.Value;
        }

        internal object GetCellClipboardValue(object item, DataGridColumn column)
        {
            CellAutomationValueHolder cellAutomationValueHolder = GetCellAutomationValueHolder(item, column);
            return cellAutomationValueHolder.GetClipboardValue();
        }

        internal void SetCellAutomationValue(object item, DataGridColumn column, string value)
        {
            SetCellValue(item, column, value, false /*clipboard*/);
        }

        internal void SetCellClipboardValue(object item, DataGridColumn column, object value)
        {
            SetCellValue(item, column, value, true /*clipboard*/);
        }

        private void SetCellValue(object item, DataGridColumn column, object value, bool clipboard)
        {
            // Put focus on the cell
            CurrentCellContainer = TryFindCell(item, column);
            if (CurrentCellContainer == null)
            {
                // If current cell has been virtualized away - scroll it into view
                ScrollCellIntoView(NewItemInfo(item), column);
                CurrentCellContainer = TryFindCell(item, column);
            }

            if (CurrentCellContainer == null)
            {
                return;
            }

            // Check if trying to edit cell while previously edited cell has an error.
            // BeginEdit will fail if there is a validation error.
            if (BeginEdit())
            {
                CellAutomationValueHolder holder;
                if (_editingCellAutomationValueHolders.TryGetValue(column, out holder))
                {
                    holder.SetValue(this, value, clipboard);   // calls CommitEdit
                }
                else
                {
                    // if there's no automation value-holder, we can't honor the SetValue request,
                    // so just cancel the edit
                    CancelEdit();
                }
            }
        }

        // create an automation value-holder for the given cell
        private void EnsureCellAutomationValueHolder(DataGridCell cell)
        {
            if (!_editingCellAutomationValueHolders.ContainsKey(cell.Column))
            {
                _editingCellAutomationValueHolders.Add(cell.Column, new CellAutomationValueHolder(cell));
            }
        }

        // update the automation/clipboard value for the given cell
        private void UpdateCellAutomationValueHolder(DataGridCell cell)
        {
            CellAutomationValueHolder holder;
            if (_editingCellAutomationValueHolders.TryGetValue(cell.Column, out holder))
            {
                holder.TrackValue();
            }
        }

        // release the automation value-holders, after notifying them one final time.
        // This is called at CommitEdit and CancelEdit time.
        private void ReleaseCellAutomationValueHolders()
        {
            foreach (KeyValuePair<DataGridColumn, CellAutomationValueHolder> kvp in _editingCellAutomationValueHolders)
            {
                kvp.Value.TrackValue();
            }

            _editingCellAutomationValueHolders.Clear();
        }

        #region CellAutomationHelper

        internal class CellAutomationValueHolder
        {
            // Track the value of an actual cell
            public CellAutomationValueHolder(DataGridCell cell)
            {
                _cell = cell;
                Initialize(cell.RowDataItem, cell.Column);
            }

            // Track the value of an (item,column) that has no corresponding cell.
            // This is used only to get the value from a virtualized item.
            public CellAutomationValueHolder(object item, DataGridColumn column)
            {
                Initialize(item, column);
            }

            private void Initialize(object item, DataGridColumn column)
            {
                _item = item;
                _column = column;
                _value = GetValue();
            }

            public string Value
            {
                get { return _value; }
            }

            // called by DataGrid when the value has potentially changed
            public void TrackValue()
            {
                string newValue = GetValue();

                if (newValue != _value)
                {
                    if (AutomationPeer.ListenerExists(AutomationEvents.PropertyChanged))
                    {
                        DataGridColumn column = (_cell != null) ? _cell.Column : _column;
                        DataGridAutomationPeer peer = DataGridAutomationPeer.FromElement(column.DataGridOwner) as DataGridAutomationPeer;
                        if (peer != null)
                        {
                            object item = (_cell != null) ? _cell.DataContext : _item;
                            DataGridItemAutomationPeer dataGridItemAutomationPeer = peer.FindOrCreateItemAutomationPeer(item) as DataGridItemAutomationPeer;
                            if (dataGridItemAutomationPeer != null)
                            {
                                DataGridCellItemAutomationPeer cellPeer = dataGridItemAutomationPeer.GetOrCreateCellItemPeer(column);
                                if (cellPeer != null)
                                {
                                    cellPeer.RaisePropertyChangedEvent(ValuePatternIdentifiers.ValueProperty, _value, newValue);
                                }
                            }
                        }
                    }

                    _value = newValue;
                }
            }

            // get the current value
            private string GetValue()
            {
                string value;
                if (_column.ClipboardContentBinding == null)
                {
                    value = null;
                }
                else if (_inSetValue)
                {
                    // when setting the value from automation, there's a re-entrant call:
                    //      SetValue->CommitEdit->TrackValue->GetValue.
                    // Inside this re-entrant call, there's already a binding in place,
                    // and it has the value we want.
                    value = (string)_cell.GetValue(CellContentProperty);
                }
                else
                {
                    FrameworkElement target;
                    if (_cell != null)
                    {
                        // Bind the CellContent property of the cell itself.
                        // The binding will participate in the row's BindingGroup, and
                        // pick up a proposed value (if any).
                        target = _cell;
                    }
                    else
                    {
                        // lacking a cell, bind a dummy element directly to the data item
                        target = new FrameworkElement();
                        target.DataContext = _item;
                    }

                    BindingOperations.SetBinding(target, CellContentProperty, _column.ClipboardContentBinding);
                    value = (string)target.GetValue(CellContentProperty);
                    BindingOperations.ClearBinding(target, CellContentProperty);
                }

                return value;
            }

            // get the current clipboard value.  This is similar to the "content" value,
            // except that it has type Object (rather than String).  We don't track
            // the clipboard value, since no one is interested in changes.  Instead,
            // we recompute it every time it's requested.
            public object GetClipboardValue()
            {
                object value;
                if (_column.ClipboardContentBinding == null)
                {
                    value = null;
                }
                else
                {
                    FrameworkElement target;
                    if (_cell != null)
                    {
                        // Bind the CellClipboard property of the cell itself.
                        // The binding will participate in the row's BindingGroup, and
                        // pick up a proposed value (if any).
                        target = _cell;
                    }
                    else
                    {
                        // lacking a cell, bind a dummy element directly to the data item
                        target = new FrameworkElement();
                        target.DataContext = _item;
                    }

                    BindingOperations.SetBinding(target, CellClipboardProperty, _column.ClipboardContentBinding);
                    value = target.GetValue(CellClipboardProperty);
                    BindingOperations.ClearBinding(target, CellClipboardProperty);
                }

                return value;
            }

            // set the value (used when setting value via automation)
            public void SetValue(DataGrid dataGrid, object value, bool clipboard)
            {
                if (_column.ClipboardContentBinding == null)
                    return;

                _inSetValue = true;

                // add a two-way binding (it joins the BindingGroup)
                DependencyProperty dp = clipboard ? CellClipboardProperty : CellContentProperty;
                BindingBase binding = _column.ClipboardContentBinding.Clone(BindingMode.TwoWay);
                BindingOperations.SetBinding(_cell, dp, binding);

                // set the new value
                _cell.SetValue(dp, value);

                // do a cell-level commit - this will validate the new value
                dataGrid.CommitEdit();

                // whether valid or not, remove the binding.  The binding group will
                // remember the proposed value
                BindingOperations.ClearBinding(_cell, dp);

                _inSetValue = false;
            }

            private static DependencyProperty CellContentProperty = DependencyProperty.RegisterAttached("CellContent", typeof(string), typeof(CellAutomationValueHolder));
            private static DependencyProperty CellClipboardProperty = DependencyProperty.RegisterAttached("CellClipboard", typeof(object), typeof(CellAutomationValueHolder));

            # region Data

            private DataGridCell _cell;
            private DataGridColumn _column;
            private object _item;
            private string _value;
            private bool _inSetValue;

            #endregion
        }

        #endregion

        #endregion

        #region Cell Info

        internal DataGridCell TryFindCell(DataGridCellInfo info)
        {
            // Does not de-virtualize cells
            return TryFindCell(LeaseItemInfo(info.ItemInfo), info.Column);
        }

        internal DataGridCell TryFindCell(ItemInfo info, DataGridColumn column)
        {
            // Does not de-virtualize cells
            DataGridRow row = (DataGridRow)info.Container;
            int columnIndex = _columns.IndexOf(column);
            if ((row != null) && (columnIndex >= 0))
            {
                return row.TryGetCell(columnIndex);
            }

            return null;
        }

        internal DataGridCell TryFindCell(object item, DataGridColumn column)
        {
            // Does not de-virtualize cells
            DataGridRow row = (DataGridRow)ItemContainerGenerator.ContainerFromItem(item);
            int columnIndex = _columns.IndexOf(column);
            if ((row != null) && (columnIndex >= 0))
            {
                return row.TryGetCell(columnIndex);
            }

            return null;
        }

        #endregion

        #region Auto Sort

        /// <summary>
        /// Dependecy property for CanUserSortColumns Property
        /// </summary>
        public static readonly DependencyProperty CanUserSortColumnsProperty =
            DependencyProperty.Register(
                "CanUserSortColumns",
                typeof(bool),
                typeof(DataGrid),
                new FrameworkPropertyMetadata(true, new PropertyChangedCallback(OnCanUserSortColumnsPropertyChanged), new CoerceValueCallback(OnCoerceCanUserSortColumns)));

        /// <summary>
        /// The property which determines whether the datagrid can be sorted by
        /// cells in the columns or not
        /// </summary>
        public bool CanUserSortColumns
        {
            get { return (bool)GetValue(CanUserSortColumnsProperty); }
            set { SetValue(CanUserSortColumnsProperty, value); }
        }

        private static object OnCoerceCanUserSortColumns(DependencyObject d, object baseValue)
        {
            DataGrid dataGrid = (DataGrid)d;
            if( DataGridHelper.IsPropertyTransferEnabled(dataGrid, CanUserSortColumnsProperty) &&
                DataGridHelper.IsDefaultValue(dataGrid, CanUserSortColumnsProperty) &&
                dataGrid.Items.CanSort == false)
            {
                return false;
            }
            return baseValue;
        }

        private static void OnCanUserSortColumnsPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            DataGrid dataGrid = (DataGrid)d;
            DataGridHelper.TransferProperty(dataGrid, CanUserSortColumnsProperty);
            OnNotifyColumnPropertyChanged(d, e);
        }

        public event DataGridSortingEventHandler Sorting;

        /// <summary>
        /// Protected method which raises the sorting event and does default sort
        /// </summary>
        /// <param name="eventArgs"></param>
        protected virtual void OnSorting(DataGridSortingEventArgs eventArgs)
        {
            eventArgs.Handled = false;
            if (Sorting != null)
            {
                Sorting(this, eventArgs);
            }

            if (!eventArgs.Handled)
            {
                DefaultSort(
                    eventArgs.Column,
                    /* clearExistinSortDescriptions */
                    (Keyboard.Modifiers & ModifierKeys.Shift) != ModifierKeys.Shift);
            }
        }

        /// <summary>
        /// Method to perform sorting on datagrid
        /// </summary>
        /// <param name="sortColumn"></param>
        internal void PerformSort(DataGridColumn sortColumn)
        {
            Debug.Assert(sortColumn != null, "column should not be null");

            if (!CanUserSortColumns || !sortColumn.CanUserSort)
            {
                return;
            }

            if (CommitAnyEdit())
            {
                PrepareForSort(sortColumn);

                DataGridSortingEventArgs eventArgs = new DataGridSortingEventArgs(sortColumn);
                OnSorting(eventArgs);

                if (Items.NeedsRefresh)
                {
                    try
                    {
                        Items.Refresh();
                    }
                    catch (InvalidOperationException invalidOperationException)
                    {
                        Items.SortDescriptions.Clear();
                        throw new InvalidOperationException(SR.Get(SRID.DataGrid_ProbableInvalidSortDescription), invalidOperationException);
                    }
                }
            }
        }

        /// <summary>
        /// Clears the sort directions for all the columns except the column to be sorted upon
        /// </summary>
        /// <param name="sortColumn"></param>
        private void PrepareForSort(DataGridColumn sortColumn)
        {
            if ((Keyboard.Modifiers & ModifierKeys.Shift) == ModifierKeys.Shift)
            {
                return;
            }

            if (Columns != null)
            {
                foreach (DataGridColumn column in Columns)
                {
                    if (column != sortColumn)
                    {
                        column.SortDirection = null;
                    }
                }
            }
        }

        /// <summary>
        /// Determines the sort direction and sort property name and adds a sort
        /// description to the Items>SortDescriptions Collection. Clears all the
        /// existing sort descriptions.
        /// </summary>
        /// <param name="column"></param>
        /// <param name="clearExistingSortDescriptions"></param>
        private void DefaultSort(DataGridColumn column, bool clearExistingSortDescriptions)
        {
            ListSortDirection sortDirection = ListSortDirection.Ascending;
            Nullable<ListSortDirection> currentSortDirection = column.SortDirection;
            if (currentSortDirection.HasValue &&
                currentSortDirection.Value == ListSortDirection.Ascending)
            {
                sortDirection = ListSortDirection.Descending;
            }

            string sortPropertyName = column.SortMemberPath;
            if (!string.IsNullOrEmpty(sortPropertyName))
            {
                try
                {
                    using (Items.DeferRefresh())
                    {
                        int descriptorIndex = -1;
                        if (clearExistingSortDescriptions)
                        {
                            // clear the sortdesriptions collection
                            Items.SortDescriptions.Clear();
                        }
                        else
                        {
                            // get the index of existing descriptor to replace it
                            for (int i = 0; i < Items.SortDescriptions.Count; i++)
                            {
                                if (string.Compare(Items.SortDescriptions[i].PropertyName, sortPropertyName, StringComparison.Ordinal) == 0 &&
                                    (GroupingSortDescriptionIndices == null ||
                                    !GroupingSortDescriptionIndices.Contains(i)))
                                {
                                    descriptorIndex = i;
                                    break;
                                }
                            }
                        }

                        SortDescription sortDescription = new SortDescription(sortPropertyName, sortDirection);
                        if (descriptorIndex >= 0)
                        {
                            Items.SortDescriptions[descriptorIndex] = sortDescription;
                        }
                        else
                        {
                            Items.SortDescriptions.Add(sortDescription);
                        }

                        if (clearExistingSortDescriptions || !_sortingStarted)
                        {
                            RegenerateGroupingSortDescriptions();
                            _sortingStarted = true;
                        }
                    }
                    column.SortDirection = sortDirection;
                }
                catch (InvalidOperationException invalidOperationException)
                {
                    TraceData.TraceAndNotify(TraceEventType.Error,
                                    TraceData.CannotSort(sortPropertyName),
                                    invalidOperationException);
                    Items.SortDescriptions.Clear();
                }
            }
        }

        /// <summary>
        /// List which holds all the indices of SortDescriptions which were
        /// added for the sake of GroupDescriptions
        /// </summary>
        private List<int> GroupingSortDescriptionIndices
        {
            get
            {
                return _groupingSortDescriptionIndices;
            }

            set
            {
                _groupingSortDescriptionIndices = value;
            }
        }

        /// <summary>
        /// SortDescription collection changed listener. Ensures that GroupingSortDescriptionIndices
        /// is in sync with SortDescriptions.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnItemsSortDescriptionsChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (_ignoreSortDescriptionsChange || GroupingSortDescriptionIndices == null)
            {
                return;
            }

            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    Debug.Assert(e.NewItems.Count == 1, "SortDescriptionCollection should handle one element at a time");
                    for (int i = 0, count = GroupingSortDescriptionIndices.Count; i < count; i++)
                    {
                        if (GroupingSortDescriptionIndices[i] >= e.NewStartingIndex)
                        {
                            GroupingSortDescriptionIndices[i]++;
                        }
                    }

                    break;
                case NotifyCollectionChangedAction.Remove:
                    Debug.Assert(e.OldItems.Count == 1, "SortDescriptionCollection should handle one element at a time");
                    for (int i = 0, count = GroupingSortDescriptionIndices.Count; i < count; i++)
                    {
                        if (GroupingSortDescriptionIndices[i] > e.OldStartingIndex)
                        {
                            GroupingSortDescriptionIndices[i]--;
                        }
                        else if (GroupingSortDescriptionIndices[i] == e.OldStartingIndex)
                        {
                            GroupingSortDescriptionIndices.RemoveAt(i);
                            i--;
                            count--;
                        }
                    }

                    break;
                case NotifyCollectionChangedAction.Move:
                    // SortDescriptionCollection doesnt support move, atleast as an atomic operation. Hence Do nothing.
                    break;
                case NotifyCollectionChangedAction.Replace:
                    Debug.Assert(e.OldItems.Count == 1 && e.NewItems.Count == 1, "SortDescriptionCollection should handle one element at a time");
                    GroupingSortDescriptionIndices.Remove(e.OldStartingIndex);
                    break;
                case NotifyCollectionChangedAction.Reset:
                    GroupingSortDescriptionIndices.Clear();
                    break;
            }
        }

        /// <summary>
        /// Method to remove all the SortDescriptions which were added based on GroupDescriptions
        /// </summary>
        private void RemoveGroupingSortDescriptions()
        {
            if (GroupingSortDescriptionIndices == null)
            {
                return;
            }

            bool originalIgnoreSortDescriptionChanges = _ignoreSortDescriptionsChange;
            _ignoreSortDescriptionsChange = true;
            try
            {
                for (int i = 0, count = GroupingSortDescriptionIndices.Count; i < count; i++)
                {
                    Items.SortDescriptions.RemoveAt(GroupingSortDescriptionIndices[i] - i);
                }

                GroupingSortDescriptionIndices.Clear();
            }
            finally
            {
                _ignoreSortDescriptionsChange = originalIgnoreSortDescriptionChanges;
            }
        }

        /// <summary>
        /// Helper method which determines if one can create a SortDescription out of
        /// a GroupDescription.
        /// </summary>
        /// <param name="propertyGroupDescription"></param>
        /// <returns></returns>
        private static bool CanConvertToSortDescription(PropertyGroupDescription propertyGroupDescription)
        {
            if (propertyGroupDescription != null &&
                propertyGroupDescription.Converter == null &&
                propertyGroupDescription.StringComparison == StringComparison.Ordinal)
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Method to add SortDescriptions based on GroupDescriptions.
        /// Only PropertGroupDescriptions with no ValueConverter and with
        /// Oridinal comparison are considered suitable.
        /// </summary>
        private void AddGroupingSortDescriptions()
        {
            bool originalIgnoreSortDescriptionChanges = _ignoreSortDescriptionsChange;
            _ignoreSortDescriptionsChange = true;
            try
            {
                int insertIndex = 0;
                foreach (GroupDescription groupDescription in Items.GroupDescriptions)
                {
                    PropertyGroupDescription propertyGroupDescription = groupDescription as PropertyGroupDescription;
                    if (CanConvertToSortDescription(propertyGroupDescription))
                    {
                        SortDescription sortDescription = new SortDescription(propertyGroupDescription.PropertyName, ListSortDirection.Ascending);
                        Items.SortDescriptions.Insert(insertIndex, sortDescription);
                        if (GroupingSortDescriptionIndices == null)
                        {
                            GroupingSortDescriptionIndices = new List<int>();
                        }

                        GroupingSortDescriptionIndices.Add(insertIndex++);
                    }
                }
            }
            finally
            {
                _ignoreSortDescriptionsChange = originalIgnoreSortDescriptionChanges;
            }
        }

        /// <summary>
        /// Method to regenrated the SortDescriptions based on the GroupDescriptions
        /// </summary>
        private void RegenerateGroupingSortDescriptions()
        {
            RemoveGroupingSortDescriptions();
            AddGroupingSortDescriptions();
        }

        /// <summary>
        /// CollectionChanged listener for GroupDescriptions of DataGrid.
        /// Regenerates Grouping based sort descriptions is required.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnItemsGroupDescriptionsChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            EnqueueNewItemMarginComputation();
            if (!_sortingStarted)
            {
                return;
            }

            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    Debug.Assert(e.NewItems.Count == 1, "GroupDescriptionCollection should handle one element at a time");
                    if (CanConvertToSortDescription(e.NewItems[0] as PropertyGroupDescription))
                    {
                        RegenerateGroupingSortDescriptions();
                    }

                    break;
                case NotifyCollectionChangedAction.Remove:
                    Debug.Assert(e.OldItems.Count == 1, "GroupDescriptionCollection should handle one element at a time");
                    if (CanConvertToSortDescription(e.OldItems[0] as PropertyGroupDescription))
                    {
                        RegenerateGroupingSortDescriptions();
                    }

                    break;
                case NotifyCollectionChangedAction.Move:
                    // Do Nothing
                    break;
                case NotifyCollectionChangedAction.Replace:
                    Debug.Assert(e.OldItems.Count == 1 && e.NewItems.Count == 1, "GroupDescriptionCollection should handle one element at a time");
                    if (CanConvertToSortDescription(e.OldItems[0] as PropertyGroupDescription) ||
                        CanConvertToSortDescription(e.NewItems[0] as PropertyGroupDescription))
                    {
                        RegenerateGroupingSortDescriptions();
                    }

                    break;
                case NotifyCollectionChangedAction.Reset:
                    RemoveGroupingSortDescriptions();
                    break;
            }
        }

        #endregion

        #region Column Auto Generation

        /// <summary>
        /// This event will be raised whenever auto generation of columns gets completed
        /// </summary>
        public event EventHandler AutoGeneratedColumns;

        /// <summary>
        /// This event will be raised for each column getting auto generated
        /// </summary>
        public event EventHandler<DataGridAutoGeneratingColumnEventArgs> AutoGeneratingColumn;

        /// <summary>
        ///     The DependencyProperty that represents the AutoGenerateColumns property.
        /// </summary>
        public static readonly DependencyProperty AutoGenerateColumnsProperty =
            DependencyProperty.Register("AutoGenerateColumns", typeof(bool), typeof(DataGrid), new FrameworkPropertyMetadata(true, new PropertyChangedCallback(OnAutoGenerateColumnsPropertyChanged)));

        /// <summary>
        /// The property which determines whether the columns are to be auto generated or not.
        /// Setting of the property actually generates or deletes columns.
        /// </summary>
        public bool AutoGenerateColumns
        {
            get { return (bool)GetValue(AutoGenerateColumnsProperty); }
            set { SetValue(AutoGenerateColumnsProperty, value); }
        }

        /// <summary>
        /// The polumorphic method which raises the AutoGeneratedColumns event
        /// </summary>
        /// <param name="e"></param>
        protected virtual void OnAutoGeneratedColumns(EventArgs e)
        {
            if (AutoGeneratedColumns != null)
            {
                AutoGeneratedColumns(this, e);
            }
        }

        /// <summary>
        /// The polymorphic method which raises the AutoGeneratingColumn event
        /// </summary>
        /// <param name="e"></param>
        protected virtual void OnAutoGeneratingColumn(DataGridAutoGeneratingColumnEventArgs e)
        {
            if (AutoGeneratingColumn != null)
            {
                AutoGeneratingColumn(this, e);
            }
        }

        /// <summary>
        ///     Determines the desired size of the control given a constraint.
        /// </summary>
        /// <remarks>
        ///     On the first measure:
        ///     - Performs auto-generation of columns if needed.
        ///     - Coerces CanUserAddRows and CanUserDeleteRows.
        ///     - Updates the NewItemPlaceholder.
        /// </remarks>
        /// <param name="availableSize">The available space.</param>
        /// <returns>The desired size of the control.</returns>
        protected override Size MeasureOverride(Size availableSize)
        {
            if (_measureNeverInvoked)
            {
                _measureNeverInvoked = false;
                if (AutoGenerateColumns)
                {
                    AddAutoColumns();
                }

                InternalColumns.InitializeDisplayIndexMap();

                // FrozenColumns rely on column DisplayIndex
                CoerceValue(FrozenColumnCountProperty);

                // These properties rely on a variety of properties. This is necessary since
                // our default (true) is actually incorrect initially (when ItemsSource is null).
                // So, we delay to this point, in case ItemsSource is never set, to coerce them
                // to their correct values. If ItemsSource did change, then they will have their
                // correct values already and this is extra work.
                CoerceValue(CanUserAddRowsProperty);
                CoerceValue(CanUserDeleteRowsProperty);

                // We need to call this in case CanUserAddRows has remained true (the default value)
                // since startup and no one has set the placeholder position.
                UpdateNewItemPlaceholder(/* isAddingNewItem = */ false);

                // always use an ItemBindingGroup
                EnsureItemBindingGroup();

                // always turn on SharesProposedValues
                ItemBindingGroup.SharesProposedValues = true;
            }
            else if (DeferAutoGeneration && AutoGenerateColumns)
            {
                // Try to generate auto columns if it was deferred earlier.
                AddAutoColumns();
            }

            return base.MeasureOverride(availableSize);
        }

        // Set the ItemBindingGroup property, if the user hasn't done so already
        private void EnsureItemBindingGroup()
        {
            if (ItemBindingGroup == null)
            {
                _defaultBindingGroup = new BindingGroup();
                SetCurrentValue(ItemBindingGroupProperty, _defaultBindingGroup);
            }
        }

        /// <summary>
        ///     Helper method to clear SortDescriptions and all related
        ///     member when ItemsSource changes
        /// </summary>
        private void ClearSortDescriptionsOnItemsSourceChange()
        {
            Items.SortDescriptions.Clear();
            _sortingStarted = false;
            List<int> groupingSortDescriptionIndices = GroupingSortDescriptionIndices;
            if (groupingSortDescriptionIndices != null)
            {
                groupingSortDescriptionIndices.Clear();
            }
            foreach (DataGridColumn column in Columns)
            {
                column.SortDirection = null;
            }
        }

        /// <summary>
        ///     Coercion callback for ItemsSource property
        /// </summary>
        /// <remarks>
        ///     SortDescriptions and GroupDescriptions are supposed to be
        ///     cleared in PropertyChangedCallback or OnItemsSourceChanged
        ///     virtual. But it seems that the SortDescriptions are applied
        ///     to the new CollectionView due to new ItemsSource in
        ///     PropertyChangedCallback of base class (which would execute
        ///     before PropertyChangedCallback of this class) and before calling
        ///     OnItemsSourceChanged virtual. Hence handling it in Coercion callback.
        /// </remarks>
        private static object OnCoerceItemsSourceProperty(DependencyObject d, object baseValue)
        {
            DataGrid dataGrid = (DataGrid)d;
            if (baseValue != dataGrid._cachedItemsSource && dataGrid._cachedItemsSource != null)
            {
                dataGrid.ClearSortDescriptionsOnItemsSourceChange();
            }

            return baseValue;
        }

        /// <summary>
        /// The polymorphic method which gets called whenever the ItemsSource gets changed.
        /// We regenerate columns if required when ItemsSource gets changed.
        /// </summary>
        /// <param name="oldValue"></param>
        /// <param name="newValue"></param>
        protected override void OnItemsSourceChanged(IEnumerable oldValue, IEnumerable newValue)
        {
            base.OnItemsSourceChanged(oldValue, newValue);

            // ItemsControl calls a ClearValue on ItemsSource property
            // whenever it is set to null. So Coercion is not called
            // in such case. Hence clearing the SortDescriptions and
            // GroupDescriptions here when new value is null.
            if (newValue == null)
            {
                ClearSortDescriptionsOnItemsSourceChange();
            }

            _cachedItemsSource = newValue;

            using (UpdateSelectedCells())
            {
                // Selector will try to maintain the previous row selection.
                // Keep SelectedCells in sync.
                List<Tuple<int,int>> ranges = new List<Tuple<int, int>>();
                LocateSelectedItems(ranges);
                _selectedCells.RestoreOnlyFullRows(ranges);
            }

            if (AutoGenerateColumns == true)
            {
                RegenerateAutoColumns();
            }

            InternalColumns.RefreshAutoWidthColumns = true;
            InternalColumns.InvalidateColumnWidthsComputation();

            CoerceValue(CanUserAddRowsProperty);
            CoerceValue(CanUserDeleteRowsProperty);
            DataGridHelper.TransferProperty(this, CanUserSortColumnsProperty);

            ResetRowHeaderActualWidth();

            UpdateNewItemPlaceholder(/* isAddingNewItem = */ false);

            HasCellValidationError = false;
            HasRowValidationError = false;
        }

        /// <summary>
        ///     The flag which determines whether the columns generation is deferred
        /// </summary>
        private bool DeferAutoGeneration
        {
            get;
            set;
        }

        /// <summary>
        ///     Performs column auto generation and updates validation flags
        ///     on items change.
        /// </summary>
        /// <param name="e"></param>
        protected override void OnItemsChanged(NotifyCollectionChangedEventArgs e)
        {
            base.OnItemsChanged(e);

            if (e.Action == NotifyCollectionChangedAction.Add)
            {
                if (DeferAutoGeneration)
                {
                    // Add Auto columns only if it was deferred earlier
                    AddAutoColumns();
                }
            }
            else if ((e.Action == NotifyCollectionChangedAction.Remove) || (e.Action == NotifyCollectionChangedAction.Replace))
            {
                if (HasRowValidationError || HasCellValidationError)
                {
                    foreach (object item in e.OldItems)
                    {
                        if (IsAddingOrEditingRowItem(item))
                        {
                            HasRowValidationError = false;
                            HasCellValidationError = false;
                            break;
                        }
                    }
                }
            }
            else if (e.Action == NotifyCollectionChangedAction.Reset)
            {
                ResetRowHeaderActualWidth();
                HasRowValidationError = false;
                HasCellValidationError = false;
            }
        }

        /// <summary>
        ///     Adjust ItemInfos when the Items property changes.
        /// </summary>
        internal override void AdjustItemInfoOverride(NotifyCollectionChangedEventArgs e)
        {
            List<ItemInfo> list = new List<ItemInfo>();

            if (_selectionAnchor != null)
                list.Add(_selectionAnchor.Value.ItemInfo);

            if (_editingRowInfo != null)
                list.Add(_editingRowInfo);

            if (CellInfoNeedsAdjusting(CurrentCell))
                list.Add(CurrentCell.ItemInfo);

            AdjustItemInfos(e, list);
            base.AdjustItemInfoOverride(e);
        }

        /// <summary>
        ///     Adjust ItemInfos when the generator finishes.
        /// </summary>
        internal override void AdjustItemInfosAfterGeneratorChangeOverride()
        {
            List<ItemInfo> list = new List<ItemInfo>();

            if (_selectionAnchor != null)
                list.Add(_selectionAnchor.Value.ItemInfo);

            if (_editingRowInfo != null)
                list.Add(_editingRowInfo);

            if (CellInfoNeedsAdjusting(CurrentCell))
                list.Add(CurrentCell.ItemInfo);

            AdjustItemInfosAfterGeneratorChange(list, claimUniqueContainer:false);
            base.AdjustItemInfosAfterGeneratorChangeOverride();
            AdjustPendingInfos();
        }

        private static bool CellInfoNeedsAdjusting(DataGridCellInfo cellInfo)
        {
            ItemsControl.ItemInfo info = cellInfo.ItemInfo;
            return (info != null) && (info.Index != -1);
        }

        // If pending items now have a known index, adjust selected cells
        private void AdjustPendingInfos()
        {
            int columnCount;
            if (_pendingInfos != null && _pendingInfos.Count > 0 && (columnCount = _columns.Count) > 0)
            {
                using (UpdateSelectedCells())
                {
                    for (int i=_pendingInfos.Count - 1; i>=0; --i)
                    {
                        ItemInfo info = _pendingInfos[i];
                        if (info.Index >= 0)
                        {
                            _pendingInfos.RemoveAt(i);
                            _selectedCells.AddRegion(info.Index, 0, 1, columnCount);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Method which generated auto columns and adds to the data grid.
        /// </summary>
        private void AddAutoColumns()
        {
            ReadOnlyCollection<ItemPropertyInfo> itemProperties = ((IItemProperties)Items).ItemProperties;
            if (itemProperties == null && DataItemsCount == 0)
            {
                // do deferred generation
                DeferAutoGeneration = true;
            }
            else if (!_measureNeverInvoked)
            {
                DataGrid.GenerateColumns(
                    itemProperties,
                    this,
                    null);

                DeferAutoGeneration = false;

                OnAutoGeneratedColumns(EventArgs.Empty);
            }
        }

        /// <summary>
        /// Method which deletes all the auto generated columns.
        /// </summary>
        private void DeleteAutoColumns()
        {
            if (!DeferAutoGeneration && !_measureNeverInvoked)
            {
                for (int columnIndex = Columns.Count - 1; columnIndex >= 0; --columnIndex)
                {
                    if (Columns[columnIndex].IsAutoGenerated)
                    {
                        Columns.RemoveAt(columnIndex);
                    }
                }
            }
            else
            {
                DeferAutoGeneration = false;
            }
        }

        /// <summary>
        /// Method which regenerates the columns for the datagrid
        /// </summary>
        private void RegenerateAutoColumns()
        {
            DeleteAutoColumns();
            AddAutoColumns();
        }

        /// <summary>
        /// Helper method which generates columns for a given IItemProperties
        /// </summary>
        /// <param name="iItemProperties"></param>
        /// <returns></returns>
        public static Collection<DataGridColumn> GenerateColumns(IItemProperties itemProperties)
        {
            if (itemProperties == null)
            {
                throw new ArgumentNullException("itemProperties");
            }

            Collection<DataGridColumn> columnCollection = new Collection<DataGridColumn>();
            DataGrid.GenerateColumns(
                itemProperties.ItemProperties,
                null,
                columnCollection);
            return columnCollection;
        }

        /// <summary>
        /// Helper method which generates columns for a given IItemProperties and adds
        /// them either to a datagrid or to a collection of columns as specified by the flag.
        /// </summary>
        /// <param name="itemProperties"></param>
        /// <param name="dataGrid"></param>
        /// <param name="columnCollection"></param>
        private static void GenerateColumns(
            ReadOnlyCollection<ItemPropertyInfo> itemProperties,
            DataGrid dataGrid,
            Collection<DataGridColumn> columnCollection)
        {
            Debug.Assert(dataGrid != null || columnCollection != null, "Both dataGrid and columnCollection cannot not be null at the same time");

            if (itemProperties != null &&
                itemProperties.Count > 0)
            {
                foreach (ItemPropertyInfo itemProperty in itemProperties)
                {
                    DataGridColumn dataGridColumn = DataGridColumn.CreateDefaultColumn(itemProperty);

                    if (dataGrid != null)
                    {
                        // AutoGeneratingColumn event is raised before generating and adding column to datagrid
                        // and the column returned by the event handler is used instead of the original column.
                        DataGridAutoGeneratingColumnEventArgs eventArgs = new DataGridAutoGeneratingColumnEventArgs(dataGridColumn, itemProperty);
                        dataGrid.OnAutoGeneratingColumn(eventArgs);

                        if (!eventArgs.Cancel && eventArgs.Column != null)
                        {
                            eventArgs.Column.IsAutoGenerated = true;
                            dataGrid.Columns.Add(eventArgs.Column);
                        }
                    }
                    else
                    {
                        columnCollection.Add(dataGridColumn);
                    }
                }
            }
        }

        /// <summary>
        /// The event listener which listens to the change in the AutoGenerateColumns flag
        /// </summary>
        /// <param name="d"></param>
        /// <param name="e"></param>
        private static void OnAutoGenerateColumnsPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            bool newValue = (bool)e.NewValue;

            DataGrid dataGrid = (DataGrid)d;

            if (newValue)
            {
                dataGrid.AddAutoColumns();
            }
            else
            {
                dataGrid.DeleteAutoColumns();
            }
        }

        #endregion

        #region Frozen Columns

        /// <summary>
        /// Dependency Property fro FrozenColumnCount Property
        /// </summary>
        public static readonly DependencyProperty FrozenColumnCountProperty =
            DependencyProperty.Register(
                "FrozenColumnCount",
                typeof(int),
                typeof(DataGrid),
                new FrameworkPropertyMetadata(0, new PropertyChangedCallback(OnFrozenColumnCountPropertyChanged), new CoerceValueCallback(OnCoerceFrozenColumnCount)),
                new ValidateValueCallback(ValidateFrozenColumnCount));

        /// <summary>
        /// Property which determines the number of columns which are frozen from the beginning in order of display
        /// </summary>
        public int FrozenColumnCount
        {
            get { return (int)GetValue(FrozenColumnCountProperty); }
            set { SetValue(FrozenColumnCountProperty, value); }
        }

        /// <summary>
        /// Coercion call back for FrozenColumnCount property, which ensures that it is never more that column count
        /// </summary>
        /// <param name="d"></param>
        /// <param name="baseValue"></param>
        /// <returns></returns>
        private static object OnCoerceFrozenColumnCount(DependencyObject d, object baseValue)
        {
            DataGrid dataGrid = (DataGrid)d;
            int frozenColumnCount = (int)baseValue;

            if (frozenColumnCount > dataGrid.Columns.Count)
            {
                return dataGrid.Columns.Count;
            }

            return baseValue;
        }

        /// <summary>
        /// Property changed callback fro FrozenColumnCount
        /// </summary>
        /// <param name="d"></param>
        /// <param name="e"></param>
        private static void OnFrozenColumnCountPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((DataGrid)d).NotifyPropertyChanged(d, e, DataGridNotificationTarget.ColumnCollection | DataGridNotificationTarget.ColumnHeadersPresenter | DataGridNotificationTarget.CellsPresenter);
        }

        /// <summary>
        /// Validation call back for frozen column count
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        private static bool ValidateFrozenColumnCount(object value)
        {
            int frozenCount = (int)value;
            return frozenCount >= 0;
        }

        /// <summary>
        /// Dependency Property key for NonFrozenColumnsViewportHorizontalOffset Property
        /// </summary>
        private static readonly DependencyPropertyKey NonFrozenColumnsViewportHorizontalOffsetPropertyKey =
                DependencyProperty.RegisterReadOnly(
                        "NonFrozenColumnsViewportHorizontalOffset",
                        typeof(double),
                        typeof(DataGrid),
                        new FrameworkPropertyMetadata(0.0));

        /// <summary>
        /// Dependency property for NonFrozenColumnsViewportHorizontalOffset Property
        /// </summary>
        public static readonly DependencyProperty NonFrozenColumnsViewportHorizontalOffsetProperty = NonFrozenColumnsViewportHorizontalOffsetPropertyKey.DependencyProperty;

        /// <summary>
        /// Property which gets/sets the start x coordinate of non frozen columns in view port
        /// </summary>
        public double NonFrozenColumnsViewportHorizontalOffset
        {
            get
            {
                return (double)GetValue(NonFrozenColumnsViewportHorizontalOffsetProperty);
            }

            internal set
            {
                SetValue(NonFrozenColumnsViewportHorizontalOffsetPropertyKey, value);
            }
        }

        /// <summary>
        /// Override of OnApplyTemplate which clear the scroll host member
        /// </summary>
        public override void OnApplyTemplate()
        {
            // If a new template has just been generated then
            // be sure to clear any stale ItemsHost references
            if (InternalItemsHost != null && !this.IsAncestorOf(InternalItemsHost))
            {
                InternalItemsHost = null;
            }

            CleanUpInternalScrollControls();
            base.OnApplyTemplate();
        }

        #endregion

        #region Container Virtualization

        /// <summary>
        ///     Property which determines if row virtualization is enabled or disabled
        /// </summary>
        public bool EnableRowVirtualization
        {
            get { return (bool)GetValue(EnableRowVirtualizationProperty); }
            set { SetValue(EnableRowVirtualizationProperty, value); }
        }

        /// <summary>
        ///     Dependency property for EnableRowVirtualization
        /// </summary>
        public static readonly DependencyProperty EnableRowVirtualizationProperty = DependencyProperty.Register(
            "EnableRowVirtualization",
            typeof(bool),
            typeof(DataGrid),
            new FrameworkPropertyMetadata(true, new PropertyChangedCallback(OnEnableRowVirtualizationChanged)));

        /// <summary>
        ///     Property changed callback for EnableRowVirtualization.
        ///     Keeps VirtualizingPanel.IsVirtualizingProperty in sync.
        /// </summary>
        private static void OnEnableRowVirtualizationChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            DataGrid dataGrid = (DataGrid)d;
            dataGrid.CoerceValue(VirtualizingPanel.IsVirtualizingProperty);

            Panel itemsHost = dataGrid.InternalItemsHost;
            if (itemsHost != null)
            {
                itemsHost.InvalidateMeasure();
                itemsHost.InvalidateArrange();
            }
        }

        /// <summary>
        ///     Coercion callback for VirtualizingPanel.IsVirtualizingProperty
        /// </summary>
        private static object OnCoerceIsVirtualizingProperty(DependencyObject d, object baseValue)
        {
            if (!DataGridHelper.IsDefaultValue(d, DataGrid.EnableRowVirtualizationProperty))
            {
                return d.GetValue(DataGrid.EnableRowVirtualizationProperty);
            }

            return baseValue;
        }

        /// <summary>
        ///     Property which determines if column virtualization is enabled or disabled
        /// </summary>
        public bool EnableColumnVirtualization
        {
            get { return (bool)GetValue(EnableColumnVirtualizationProperty); }
            set { SetValue(EnableColumnVirtualizationProperty, value); }
        }

        /// <summary>
        ///     Dependency property for EnableColumnVirtualization
        /// </summary>
        public static readonly DependencyProperty EnableColumnVirtualizationProperty = DependencyProperty.Register(
            "EnableColumnVirtualization",
            typeof(bool),
            typeof(DataGrid),
            new FrameworkPropertyMetadata(false, new PropertyChangedCallback(OnEnableColumnVirtualizationChanged)));

        /// <summary>
        ///     Property changed callback for EnableColumnVirtualization.
        ///     Gets VirtualizingPanel.IsVirtualizingProperty for cells presenter and
        ///     headers presenter in sync.
        /// </summary>
        private static void OnEnableColumnVirtualizationChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((DataGrid)d).NotifyPropertyChanged(d, e, DataGridNotificationTarget.CellsPresenter | DataGridNotificationTarget.ColumnHeadersPresenter | DataGridNotificationTarget.ColumnCollection);
        }

        #endregion

        #region Column Reordering

        /// <summary>
        /// Dependency Property for CanUserReorderColumns Property
        /// </summary>
        public static readonly DependencyProperty CanUserReorderColumnsProperty =
            DependencyProperty.Register("CanUserReorderColumns", typeof(bool), typeof(DataGrid), new FrameworkPropertyMetadata(true, new PropertyChangedCallback(OnNotifyColumnPropertyChanged)));

        /// <summary>
        /// The property which determines if an end user can re-order columns or not.
        /// </summary>
        public bool CanUserReorderColumns
        {
            get { return (bool)GetValue(CanUserReorderColumnsProperty); }
            set { SetValue(CanUserReorderColumnsProperty, value); }
        }

        /// <summary>
        /// Dependency Property for DragIndicatorStyle property
        /// </summary>
        public static readonly DependencyProperty DragIndicatorStyleProperty =
            DependencyProperty.Register("DragIndicatorStyle", typeof(Style), typeof(DataGrid), new FrameworkPropertyMetadata(null, OnNotifyColumnPropertyChanged));

        /// <summary>
        /// The style property which would be applied on the column header drag indicator
        /// </summary>
        public Style DragIndicatorStyle
        {
            get { return (Style)GetValue(DragIndicatorStyleProperty); }
            set { SetValue(DragIndicatorStyleProperty, value); }
        }

        /// <summary>
        /// Dependency Property for DropLocationIndicatorStyle property
        /// </summary>
        public static readonly DependencyProperty DropLocationIndicatorStyleProperty =
            DependencyProperty.Register("DropLocationIndicatorStyle", typeof(Style), typeof(DataGrid), new FrameworkPropertyMetadata(null));

        /// <summary>
        /// The style property which would be applied on the column header drop location indicator.
        /// </summary>
        public Style DropLocationIndicatorStyle
        {
            get { return (Style)GetValue(DropLocationIndicatorStyleProperty); }
            set { SetValue(DropLocationIndicatorStyleProperty, value); }
        }

        public event EventHandler<DataGridColumnReorderingEventArgs> ColumnReordering;

        public event EventHandler<DragStartedEventArgs> ColumnHeaderDragStarted;

        public event EventHandler<DragDeltaEventArgs> ColumnHeaderDragDelta;

        public event EventHandler<DragCompletedEventArgs> ColumnHeaderDragCompleted;

        public event EventHandler<DataGridColumnEventArgs> ColumnReordered;

        protected internal virtual void OnColumnHeaderDragStarted(DragStartedEventArgs e)
        {
            if (ColumnHeaderDragStarted != null)
            {
                ColumnHeaderDragStarted(this, e);
            }
        }

        protected internal virtual void OnColumnReordering(DataGridColumnReorderingEventArgs e)
        {
            if (ColumnReordering != null)
            {
                ColumnReordering(this, e);
            }
        }

        protected internal virtual void OnColumnHeaderDragDelta(DragDeltaEventArgs e)
        {
            if (ColumnHeaderDragDelta != null)
            {
                ColumnHeaderDragDelta(this, e);
            }
        }

        protected internal virtual void OnColumnHeaderDragCompleted(DragCompletedEventArgs e)
        {
            if (ColumnHeaderDragCompleted != null)
            {
                ColumnHeaderDragCompleted(this, e);
            }
        }

        protected internal virtual void OnColumnReordered(DataGridColumnEventArgs e)
        {
            if (ColumnReordered != null)
            {
                ColumnReordered(this, e);
            }
        }

        #endregion

        #region Clipboard Copy

        /// <summary>
        ///     The DependencyProperty that represents the ClipboardCopyMode property.
        /// </summary>
        public static readonly DependencyProperty ClipboardCopyModeProperty =
            DependencyProperty.Register("ClipboardCopyMode", typeof(DataGridClipboardCopyMode), typeof(DataGrid), new FrameworkPropertyMetadata(DataGridClipboardCopyMode.ExcludeHeader, new PropertyChangedCallback(OnClipboardCopyModeChanged)));

        private static void OnClipboardCopyModeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            // The Copy command needs to have CanExecute run
            CommandManager.InvalidateRequerySuggested();
        }

        /// <summary>
        /// The property which determines how DataGrid content is copied to the Clipboard.
        /// </summary>
        public DataGridClipboardCopyMode ClipboardCopyMode
        {
            get { return (DataGridClipboardCopyMode)GetValue(ClipboardCopyModeProperty); }
            set { SetValue(ClipboardCopyModeProperty, value); }
        }

        private static void OnCanExecuteCopy(object target, CanExecuteRoutedEventArgs args)
        {
            ((DataGrid)target).OnCanExecuteCopy(args);
        }

        /// <summary>
        /// This virtual method is called when ApplicationCommands.Copy command query its state.
        /// </summary>
        /// <param name="args"></param>
        protected virtual void OnCanExecuteCopy(CanExecuteRoutedEventArgs args)
        {
            args.CanExecute = ClipboardCopyMode != DataGridClipboardCopyMode.None && _selectedCells.Count > 0;
            args.Handled = true;
        }

        private static void OnExecutedCopy(object target, ExecutedRoutedEventArgs args)
        {
            ((DataGrid)target).OnExecutedCopy(args);
        }

        /// <summary>
        /// This virtual method is called when ApplicationCommands.Copy command is executed.
        /// </summary>
        /// <param name="args"></param>
        protected virtual void OnExecutedCopy(ExecutedRoutedEventArgs args)
        {
            if (ClipboardCopyMode == DataGridClipboardCopyMode.None)
            {
                throw new NotSupportedException(SR.Get(SRID.ClipboardCopyMode_Disabled));
            }

            args.Handled = true;

            // Supported default formats: Html, Text, UnicodeText and CSV
            Collection<string> formats = new Collection<string>(new string[] { DataFormats.Html, DataFormats.Text, DataFormats.UnicodeText, DataFormats.CommaSeparatedValue });
            Dictionary<string, StringBuilder> dataGridStringBuilders = new Dictionary<string, StringBuilder>(formats.Count);
            foreach (string format in formats)
            {
                dataGridStringBuilders[format] = new StringBuilder();
            }

            int minRowIndex;
            int maxRowIndex;
            int minColumnDisplayIndex;
            int maxColumnDisplayIndex;

            // Get the bounding box of the selected cells
            if (_selectedCells.GetSelectionRange(out minColumnDisplayIndex, out maxColumnDisplayIndex, out minRowIndex, out maxRowIndex))
            {
                // Add column headers if enabled
                if (ClipboardCopyMode == DataGridClipboardCopyMode.IncludeHeader)
                {
                    DataGridRowClipboardEventArgs preparingRowClipboardContentEventArgs = new DataGridRowClipboardEventArgs(null, minColumnDisplayIndex, maxColumnDisplayIndex, true /*IsColumnHeadersRow*/);
                    OnCopyingRowClipboardContent(preparingRowClipboardContentEventArgs);

                    foreach (string format in formats)
                    {
                        dataGridStringBuilders[format].Append(preparingRowClipboardContentEventArgs.FormatClipboardCellValues(format));
                    }
                }

                // Add each selected row
                for (int i = minRowIndex; i <= maxRowIndex; i++)
                {
                    object row = Items[i];

                    // Row has a selecion
                    if (_selectedCells.Intersects(i))
                    {
                        DataGridRowClipboardEventArgs preparingRowClipboardContentEventArgs = new DataGridRowClipboardEventArgs(row, minColumnDisplayIndex, maxColumnDisplayIndex, false /*IsColumnHeadersRow*/, i);
                        OnCopyingRowClipboardContent(preparingRowClipboardContentEventArgs);

                        foreach (string format in formats)
                        {
                            dataGridStringBuilders[format].Append(preparingRowClipboardContentEventArgs.FormatClipboardCellValues(format));
                        }
                    }
                }
            }

            DataGridClipboardHelper.GetClipboardContentForHtml(dataGridStringBuilders[DataFormats.Html]);

            DataObject dataObject = new DataObject();


            foreach (string format in formats)
            {
                dataObject.SetData(format, dataGridStringBuilders[format].ToString(), false /*autoConvert*/);
            }

            try
            {
                Clipboard.CriticalSetDataObject(dataObject, true /* Copy */);
            }
            catch (ExternalException)
            {
                // Clipboard failed to set the data object - fail silently.
                return;
            }
        }

        /// <summary>
        /// This method is called to prepare the clipboard content for each selected row.
        /// If ClipboardCopyMode is set to ClipboardCopyMode, then it is also called to prepare the column headers
        /// </summary>
        /// <param name="args">Contains the necessary information for generating the row clipboard content.</param>
        protected virtual void OnCopyingRowClipboardContent(DataGridRowClipboardEventArgs args)
        {
            if (args.IsColumnHeadersRow)
            {
                for (int i = args.StartColumnDisplayIndex; i <= args.EndColumnDisplayIndex; i++)
                {
                    DataGridColumn column = ColumnFromDisplayIndex(i);
                    if (!column.IsVisible)
                    {
                        continue;
                    }

                    args.ClipboardRowContent.Add(new DataGridClipboardCellContent(args.Item, column, column.Header));
                }
            }
            else
            {
                int rowIndex = args.RowIndexHint;
                if (rowIndex < 0)
                {
                    rowIndex = Items.IndexOf(args.Item);
                }

                // If row has selection
                if (_selectedCells.Intersects(rowIndex))
                {
                    for (int i = args.StartColumnDisplayIndex; i <= args.EndColumnDisplayIndex; i++)
                    {
                        DataGridColumn column = ColumnFromDisplayIndex(i);
                        if (!column.IsVisible)
                        {
                            continue;
                        }

                        object cellValue = null;

                        // Get cell value only if the cell is selected - otherwise leave it null
                        if (_selectedCells.Contains(rowIndex, i))
                        {
                            cellValue = column.OnCopyingCellClipboardContent(args.Item);
                        }

                        args.ClipboardRowContent.Add(new DataGridClipboardCellContent(args.Item, column, cellValue));
                    }
                }
            }

            // Raise the event to give a chance to external listeners to modify row clipboard content (e.ClipboardRow)
            if (CopyingRowClipboardContent != null)
            {
                CopyingRowClipboardContent(this, args);
            }
        }

        /// <summary>
        /// This event is raised by OnCopyingRowClipboardContent method after the default row content is prepared.
        /// Event listeners can modify or add to the row clipboard content
        /// </summary>
        public event EventHandler<DataGridRowClipboardEventArgs> CopyingRowClipboardContent;
        #endregion

        #region Cells Panel Width

        /// <summary>
        /// Dependency Property for CellsPanelActualWidth property
        /// </summary>
        internal static readonly DependencyProperty CellsPanelActualWidthProperty =
            DependencyProperty.Register(
                        "CellsPanelActualWidth",
                        typeof(double),
                        typeof(DataGrid),
                        new FrameworkPropertyMetadata(0.0, new PropertyChangedCallback(CellsPanelActualWidthChanged)));

        /// <summary>
        /// The property which represents the actual width of the cells panel,
        /// to be used by headers presenter
        /// </summary>
        internal double CellsPanelActualWidth
        {
            get
            {
                return (double)GetValue(CellsPanelActualWidthProperty);
            }

            set
            {
                SetValue(CellsPanelActualWidthProperty, value);
            }
        }

        /// <summary>
        /// Property changed callback for CellsPanelActualWidth property
        /// </summary>
        /// <param name="d"></param>
        /// <param name="e"></param>
        private static void CellsPanelActualWidthChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            double oldValue = (double)e.OldValue;
            double newValue = (double)e.NewValue;
            if (!DoubleUtil.AreClose(oldValue, newValue))
            {
                ((DataGrid)d).NotifyPropertyChanged(d, e, DataGridNotificationTarget.ColumnHeadersPresenter);
            }
        }

        #endregion

        #region Column Width Computations

        /// <summary>
        ///     Dependency Property Key for CellsPanelHorizontalOffset property
        /// </summary>
        private static readonly DependencyPropertyKey CellsPanelHorizontalOffsetPropertyKey =
            DependencyProperty.RegisterReadOnly(
                "CellsPanelHorizontalOffset",
                typeof(double),
                typeof(DataGrid),
                new FrameworkPropertyMetadata(0d, OnNotifyHorizontalOffsetPropertyChanged));

        /// <summary>
        ///     Dependency Property for CellsPanelHorizontalOffset
        /// </summary>
        public static readonly DependencyProperty CellsPanelHorizontalOffsetProperty = CellsPanelHorizontalOffsetPropertyKey.DependencyProperty;

        /// <summary>
        ///     Property which caches the cells panel horizontal offset
        /// </summary>
        public double CellsPanelHorizontalOffset
        {
            get { return (double)GetValue(CellsPanelHorizontalOffsetProperty); }
            private set { SetValue(CellsPanelHorizontalOffsetPropertyKey, value); }
        }

        /// <summary>
        ///     Property which indicates whether a request to
        ///     invalidate CellsPanelOffset is already in queue or not.
        /// </summary>
        private bool CellsPanelHorizontalOffsetComputationPending
        {
            get;
            set;
        }

        /// <summary>
        ///     Helper method which queue a request to dispatcher to
        ///     invalidate the cellspanel offset if not already queued
        /// </summary>
        internal void QueueInvalidateCellsPanelHorizontalOffset()
        {
            if (!CellsPanelHorizontalOffsetComputationPending)
            {
                Dispatcher.BeginInvoke(new DispatcherOperationCallback(InvalidateCellsPanelHorizontalOffset), DispatcherPriority.Loaded, this);
                CellsPanelHorizontalOffsetComputationPending = true;
            }
        }

        /// <summary>
        ///     Dispatcher call back method which recomputes the CellsPanelOffset
        /// </summary>
        private object InvalidateCellsPanelHorizontalOffset(object args)
        {
            if (!CellsPanelHorizontalOffsetComputationPending)
            {
                return null;
            }

            IProvideDataGridColumn cell = GetAnyCellOrColumnHeader();
            if (cell != null)
            {
                CellsPanelHorizontalOffset = DataGridHelper.GetParentCellsPanelHorizontalOffset(cell);
            }
            else if (!Double.IsNaN(RowHeaderWidth))
            {
                CellsPanelHorizontalOffset = RowHeaderWidth;
            }
            else
            {
                CellsPanelHorizontalOffset = 0d;
            }

            CellsPanelHorizontalOffsetComputationPending = false;
            return null;
        }


        /// <summary>
        /// Helper method which return any one of the cells or column headers
        /// </summary>
        /// <returns></returns>
        internal IProvideDataGridColumn GetAnyCellOrColumnHeader()
        {
            // Find the best try at a visible cell from a visible row.
            if (_rowTrackingRoot != null)
            {
                ContainerTracking<DataGridRow> rowTracker = _rowTrackingRoot;
                while (rowTracker != null)
                {
                    if (rowTracker.Container.IsVisible)
                    {
                        DataGridCellsPresenter cellsPresenter = rowTracker.Container.CellsPresenter;
                        if (cellsPresenter != null)
                        {
                            ContainerTracking<DataGridCell> cellTracker = cellsPresenter.CellTrackingRoot;
                            while (cellTracker != null)
                            {
                                if (cellTracker.Container.IsVisible)
                                {
                                    return cellTracker.Container;
                                }
                                cellTracker = cellTracker.Next;
                            }
                        }
                    }
                    rowTracker = rowTracker.Next;
                }
            }

            // If the row that we found earlier is not a good choice try a column header.
            // If no good column header is found the fall back will be the cell.
            if (ColumnHeadersPresenter != null)
            {
                ContainerTracking<DataGridColumnHeader> headerTracker = ColumnHeadersPresenter.HeaderTrackingRoot;
                while (headerTracker != null)
                {
                    if (headerTracker.Container.IsVisible)
                    {
                        return headerTracker.Container;
                    }
                    headerTracker = headerTracker.Next;
                }
            }

            return null;
        }

        /// <summary>
        /// Helper method which returns the width of the viewport which is available for the columns to render
        /// </summary>
        /// <returns></returns>
        internal double GetViewportWidthForColumns()
        {
            if (InternalScrollHost == null)
            {
                return 0.0;
            }

            double totalAvailableWidth = InternalScrollHost.ViewportWidth;
            totalAvailableWidth -= CellsPanelHorizontalOffset;
            return totalAvailableWidth;
        }

        #endregion

        #region Visual States

        internal override void ChangeVisualState(bool useTransitions)
        {
            if (!IsEnabled)
            {
                VisualStates.GoToState(this, useTransitions, VisualStates.StateDisabled, VisualStates.StateNormal);
            }
            else
            {
                VisualStates.GoToState(this, useTransitions, VisualStates.StateNormal);
            }

            base.ChangeVisualState(useTransitions);
        }

        #endregion

        #region Helpers

        // Consider making this public.
        // Used as an alternate data item to CollectionView.NewItemPlaceholder so that
        // CellsPresenter's ItemContainerGenerator does not get confused.
        internal static object NewItemPlaceholder
        {
            get { return _newItemPlaceholder; }
        }

        #endregion

        #region Data

        private static IValueConverter _headersVisibilityConverter;         // Used to convert DataGridHeadersVisibility to Visibility in styles
        private static IValueConverter _rowDetailsScrollingConverter;       // Used to convert boolean (DataGrid.RowDetailsAreFrozen) into a SelectiveScrollingMode
        private static object _newItemPlaceholder = new NamedObject("DataGrid.NewItemPlaceholder"); // Used as an alternate data item to CollectionView.NewItemPlaceholder

        private DataGridColumnCollection _columns;                          // Stores the columns
        private ContainerTracking<DataGridRow> _rowTrackingRoot;            // Root of a linked list of active row containers
        private DataGridColumnHeadersPresenter _columnHeadersPresenter;     // headers presenter for sending down notifications
        private DataGridCell _currentCellContainer;                         // Reference to the cell container corresponding to CurrentCell (use CurrentCellContainer property instead)
        private DataGridCell _pendingCurrentCellContainer;                  // Reference to the cell container that will become the current cell
        private SelectedCellsCollection _selectedCells;                     // Stores the selected cells
        private List<ItemInfo> _pendingInfos;                               // Selected items whose index is not yet known
        private Nullable<DataGridCellInfo> _selectionAnchor;                // For doing extended selection
        private bool _isDraggingSelection;                                  // Whether a drag select is being performed
        private bool _isRowDragging;                                        // Whether a drag select is being done on rows
        private Panel _internalItemsHost;                                   // Workaround for not having access to ItemsHost
        private ScrollViewer _internalScrollHost;                           // Scroll viewer of the datagrid
        private ScrollContentPresenter _internalScrollContentPresenter = null; // Scroll Content Presenter of DataGrid's ScrollViewer
        private DispatcherTimer _autoScrollTimer;                           // Timer to tick auto-scroll
        private bool _hasAutoScrolled;                                      // Whether an auto-scroll has occurred since starting the tick
        private VirtualizedCellInfoCollection _pendingSelectedCells;        // Cells that were selected that haven't gone through SelectedCellsChanged
        private VirtualizedCellInfoCollection _pendingUnselectedCells;      // Cells that were unselected that haven't gone through SelectedCellsChanged
        private bool _measureNeverInvoked = true;                           // Flag used to track if measure was invoked atleast once. Particularly used for AutoGeneration.
        private bool _updatingSelectedCells = false;                        // Whether to defer notifying that SelectedCells changed.
        private Visibility _placeholderVisibility = Visibility.Collapsed;   // The visibility used for the Placeholder container.  It may not exist at all times, so it's stored on the DG.
        private Point _dragPoint;                                           // Used to detect if a drag actually occurred
        private List<int> _groupingSortDescriptionIndices = null;           // List to hold the indices of SortDescriptions added for the sake of GroupDescriptions.
        private bool _ignoreSortDescriptionsChange = false;                 // Flag used to neglect the SortDescriptionCollection changes in the CollectionChanged listener.
        private bool _sortingStarted = false;                               // Flag used to track if Sorting ever started or not.
        private ObservableCollection<ValidationRule> _rowValidationRules;   // Stores the row ValidationRule's
        private BindingGroup _defaultBindingGroup;                          // Cached copy of the BindingGroup created for row validation...so we dont stomp on user set ItemBindingGroup
        private ItemInfo _editingRowInfo = null;                            // Current editing row info
        private bool _hasCellValidationError;                               // An unsuccessful cell commit occurred
        private bool _hasRowValidationError;                                // An unsuccessful row commit occurred
        private IEnumerable _cachedItemsSource = null;                      // Reference to the ItemsSource instance, used to clear SortDescriptions on ItemsSource change
        private DataGridItemAttachedStorage _itemAttachedStorage = new DataGridItemAttachedStorage(); // Holds data about the items that's need for row virtualization
        private bool _viewportWidthChangeNotificationPending = false;       // Flag to indicate if a viewport width change reuest is already queued.
        private double _originalViewportWidth = 0.0;                        // Holds the original viewport width between multiple viewport width changes
        private double _finalViewportWidth = 0.0;                           // Holds the final viewport width between multiple viewport width changes
        private Dictionary<DataGridColumn, CellAutomationValueHolder> _editingCellAutomationValueHolders
                    = new Dictionary<DataGridColumn, CellAutomationValueHolder>(); // Holds the content of edited cells. Required for raising Automation events.
        private DataGridCell _focusedCell = null;                           // Holds the cell which has logical focus.
        private bool _newItemMarginComputationPending = false;           // Flag to indicate if row margin computation request is pending

        #endregion

        #region Constants
        private const string ItemsPanelPartName = "PART_RowsPresenter";
        #endregion
    }
}
