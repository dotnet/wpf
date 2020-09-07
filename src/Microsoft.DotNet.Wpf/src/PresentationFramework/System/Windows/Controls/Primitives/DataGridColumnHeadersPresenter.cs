// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.



using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using MS.Internal;

namespace System.Windows.Controls.Primitives
{
    /// <summary>
    ///     A control that will be responsible for generating column headers.
    ///     This control is meant to be specified within the template of the DataGrid.
    ///
    ///     It typically isn't in the subtree of the main ScrollViewer for the DataGrid.
    ///     It thus handles scrolling the column headers horizontally.  For this to work
    ///     it needs to be able to find the ScrollViewer -- this is done by setting the
    ///     SourceScrollViewerName property.
    /// </summary>
    [TemplatePart(Name = "PART_FillerColumnHeader", Type = typeof(DataGridColumnHeader))]
    public class DataGridColumnHeadersPresenter : ItemsControl
    {
        #region Constants

        private const string ElementFillerColumnHeader = "PART_FillerColumnHeader";

        #endregion

        static DataGridColumnHeadersPresenter()
        {
            Type ownerType = typeof(DataGridColumnHeadersPresenter);

            DefaultStyleKeyProperty.OverrideMetadata(ownerType, new FrameworkPropertyMetadata(ownerType));
            FocusableProperty.OverrideMetadata(ownerType, new FrameworkPropertyMetadata(false));

            FrameworkElementFactory factory = new FrameworkElementFactory(typeof(DataGridCellsPanel));
            ItemsPanelProperty.OverrideMetadata(ownerType, new FrameworkPropertyMetadata(new ItemsPanelTemplate(factory)));

            VirtualizingPanel.IsVirtualizingProperty.OverrideMetadata(
                ownerType,
                new FrameworkPropertyMetadata(false, new PropertyChangedCallback(OnIsVirtualizingPropertyChanged), new CoerceValueCallback(OnCoerceIsVirtualizingProperty)));

            VirtualizingPanel.VirtualizationModeProperty.OverrideMetadata(
                ownerType,
                new FrameworkPropertyMetadata(VirtualizationMode.Recycling));
        }

        #region Initialization

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
            
            base.OnApplyTemplate();

            // Find the columns collection and set the ItemsSource.
            DataGrid grid = ParentDataGrid;

            if (grid != null)
            {
                ItemsSource = new DataGridColumnHeaderCollection(grid.Columns);
                grid.ColumnHeadersPresenter = this;
                DataGridHelper.TransferProperty(this, VirtualizingPanel.IsVirtualizingProperty);

                DataGridColumnHeader fillerColumnHeader = GetTemplateChild(ElementFillerColumnHeader) as DataGridColumnHeader;
                if (fillerColumnHeader != null)
                {
                    DataGridHelper.TransferProperty(fillerColumnHeader, DataGridColumnHeader.StyleProperty);
                    DataGridHelper.TransferProperty(fillerColumnHeader, DataGridColumnHeader.HeightProperty);
                }
            }
            else
            {
                ItemsSource = null;
            }
        }

        #endregion

        #region Automation

        protected override System.Windows.Automation.Peers.AutomationPeer OnCreateAutomationPeer()
        {
            return new System.Windows.Automation.Peers.DataGridColumnHeadersPresenterAutomationPeer(this);
        }

        #endregion

        #region Layout

        /// <summary>
        ///     Measure
        /// </summary>
        protected override Size MeasureOverride(Size availableSize)
        {
            Size desiredSize;
            Size childConstraint = availableSize;
            childConstraint.Width = Double.PositiveInfinity;

            desiredSize = base.MeasureOverride(childConstraint);

            Size indicatorSize;
            if (_columnHeaderDragIndicator != null && _isColumnHeaderDragging)
            {
                _columnHeaderDragIndicator.Measure(childConstraint);
                indicatorSize = _columnHeaderDragIndicator.DesiredSize;
                desiredSize.Width = Math.Max(desiredSize.Width, indicatorSize.Width);
                desiredSize.Height = Math.Max(desiredSize.Height, indicatorSize.Height);
            }

            if (_columnHeaderDropLocationIndicator != null && _isColumnHeaderDragging)
            {
                _columnHeaderDropLocationIndicator.Measure(availableSize);
                indicatorSize = _columnHeaderDropLocationIndicator.DesiredSize;
                desiredSize.Width = Math.Max(desiredSize.Width, indicatorSize.Width);
                desiredSize.Height = Math.Max(desiredSize.Height, indicatorSize.Height);
            }

            desiredSize.Width = Math.Min(availableSize.Width, desiredSize.Width);

            return desiredSize;
        }

        /// <summary>
        ///     Arrange
        /// </summary>
        /// <param name="finalSize">Arrange size</param>
        protected override Size ArrangeOverride(Size finalSize)
        {
            UIElement child = (VisualTreeHelper.GetChildrenCount(this) > 0) ? VisualTreeHelper.GetChild(this, 0) as UIElement : null;

            if (child != null)
            {
                Rect childRect = new Rect(finalSize);
                DataGrid dataGrid = ParentDataGrid;
                if (dataGrid != null)
                {
                    childRect.X = -dataGrid.HorizontalScrollOffset;
                    childRect.Width = Math.Max(finalSize.Width, dataGrid.CellsPanelActualWidth);
                }

                child.Arrange(childRect);
            }

            if (_columnHeaderDragIndicator != null && _isColumnHeaderDragging)
            {
                _columnHeaderDragIndicator.Arrange(new Rect(
                    new Point(_columnHeaderDragCurrentPosition.X - _columnHeaderDragStartRelativePosition.X, 0),
                    new Size(_columnHeaderDragIndicator.Width, _columnHeaderDragIndicator.Height)));
            }

            if (_columnHeaderDropLocationIndicator != null && _isColumnHeaderDragging)
            {
                Point point = FindColumnHeaderPositionByCurrentPosition(_columnHeaderDragCurrentPosition, true);
                double dropIndicatorWidth = _columnHeaderDropLocationIndicator.Width;
                point.X -= dropIndicatorWidth * 0.5;
                _columnHeaderDropLocationIndicator.Arrange(new Rect(point, new Size(dropIndicatorWidth, _columnHeaderDropLocationIndicator.Height)));
            }

            return finalSize;
        }

        /// <summary>
        ///     Override of UIElement.GetLayoutClip().  This is a tricky way to ensure we always clip regardless of the value of ClipToBounds.
        /// </summary>
        protected override Geometry GetLayoutClip(Size layoutSlotSize)
        {
            RectangleGeometry clip = new RectangleGeometry(new Rect(RenderSize));
            clip.Freeze();
            return clip;
        }

        #endregion

        #region Column Header Generation

        /// <summary>
        ///     Instantiates an instance of a container.
        /// </summary>
        /// <returns>A new DataGridColumnHeader.</returns>
        protected override DependencyObject GetContainerForItemOverride()
        {
            return new DataGridColumnHeader();
        }

        /// <summary>
        ///     Determines if an item is its own container.
        /// </summary>
        /// <param name="item">The item to test.</param>
        /// <returns>true if the item is a DataGridColumnHeader, false otherwise.</returns>
        protected override bool IsItemItsOwnContainerOverride(object item)
        {
            return item is DataGridColumnHeader;
        }

        /// <summary>
        ///     Method which returns the result of IsItemItsOwnContainerOverride to be used internally
        /// </summary>
        internal bool IsItemItsOwnContainerInternal(object item)
        {
            return IsItemItsOwnContainerOverride(item);
        }

        /// <summary>
        ///     Prepares a new container for a given item.
        /// </summary>
        /// <remarks>We do not want to call base.PrepareContainerForItemOverride in this override because it will set local values on the header</remarks>
        /// <param name="element">The new container.</param>
        /// <param name="item">The item that the container represents.</param>
        protected override void PrepareContainerForItemOverride(DependencyObject element, object item)
        {
            DataGridColumnHeader header = element as DataGridColumnHeader;

            if (header != null)
            {
                DataGridColumn column = ColumnFromContainer(header);
                Debug.Assert(column != null, "We shouldn't have generated this column header if we don't have a column.");

                if (header.Column == null)
                {
                    // A null column means this is a fresh container.  PrepareContainer will also be called simply if the column's
                    // Header property has changed and this container needs to be given a new item.  In that case it'll already be tracked.
                    header.Tracker.Debug_AssertNotInList(_headerTrackingRoot);
                    header.Tracker.StartTracking(ref _headerTrackingRoot);
                }

                header.Tracker.Debug_AssertIsInList(_headerTrackingRoot);

                header.PrepareColumnHeader(item, column);
            }
        }

        /// <summary>
        ///     Clears a container of references.
        /// </summary>
        /// <param name="element">The container being cleared.</param>
        /// <param name="item">The data item that the container represented.</param>
        protected override void ClearContainerForItemOverride(DependencyObject element, object item)
        {
            DataGridColumnHeader header = element as DataGridColumnHeader;

            base.ClearContainerForItemOverride(element, item);

            if (header != null)
            {
                header.Tracker.StopTracking(ref _headerTrackingRoot);
                header.ClearHeader();
            }
        }

        private DataGridColumn ColumnFromContainer(DataGridColumnHeader container)
        {
            Debug.Assert(HeaderCollection != null, "This is a helper method for preparing and clearing a container; if it's called we must have a valid ItemSource");

            int index = ItemContainerGenerator.IndexFromContainer(container);
            return HeaderCollection.ColumnFromIndex(index);
        }

        #endregion

        #region Notification Propagation

        /// <summary>
        ///     General notification for DependencyProperty changes from the grid.
        /// </summary>
        internal void NotifyPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e, DataGridNotificationTarget target)
        {
            NotifyPropertyChanged(d, string.Empty, e, target);
        }

        /// <summary>
        ///     Notification for column header-related DependencyProperty changes from the grid or from columns.
        /// </summary>
        internal void NotifyPropertyChanged(DependencyObject d, string propertyName, DependencyPropertyChangedEventArgs e, DataGridNotificationTarget target)
        {
            DataGridColumn column = d as DataGridColumn;
            if (DataGridHelper.ShouldNotifyColumnHeadersPresenter(target))
            {
                if (e.Property == DataGridColumn.WidthProperty ||
                    e.Property == DataGridColumn.DisplayIndexProperty)
                {
                    if (column.IsVisible)
                    {
                        InvalidateDataGridCellsPanelMeasureAndArrange();
                    }
                }
                else if (e.Property == DataGrid.FrozenColumnCountProperty ||
                    e.Property == DataGridColumn.VisibilityProperty ||
                    e.Property == DataGrid.CellsPanelHorizontalOffsetProperty ||
                    string.Compare(propertyName, "ViewportWidth", StringComparison.Ordinal) == 0 ||
                    string.Compare(propertyName, "DelayedColumnWidthComputation", StringComparison.Ordinal) == 0)
                {
                    InvalidateDataGridCellsPanelMeasureAndArrange();
                }
                else if (e.Property == DataGrid.HorizontalScrollOffsetProperty)
                {
                    InvalidateArrange();
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
                else if (e.Property == DataGrid.CellsPanelActualWidthProperty)
                {
                    InvalidateArrange();
                }
                else if (e.Property == DataGrid.EnableColumnVirtualizationProperty)
                {
                    DataGridHelper.TransferProperty(this, VirtualizingPanel.IsVirtualizingProperty);
                }
            }

            if (DataGridHelper.ShouldNotifyColumnHeaders(target))
            {
                if (e.Property == DataGridColumn.HeaderProperty)
                {
                    if (HeaderCollection != null)
                    {
                        HeaderCollection.NotifyHeaderPropertyChanged(column, e);
                    }
                }
                else
                {
                    // Notify the DataGridColumnHeader objects about property changes
                    ContainerTracking<DataGridColumnHeader> tracker = _headerTrackingRoot;

                    while (tracker != null)
                    {
                        tracker.Container.NotifyPropertyChanged(d, e);
                        tracker = tracker.Next;
                    }

                    // Handle Style & Height change notification for PART_FillerColumnHeader.
                    if (d is DataGrid &&
                        (e.Property == DataGrid.ColumnHeaderStyleProperty || e.Property == DataGrid.ColumnHeaderHeightProperty) )
                    {
                        DataGridColumnHeader fillerColumnHeader = GetTemplateChild(ElementFillerColumnHeader) as DataGridColumnHeader;
                        if (fillerColumnHeader != null)
                        {
                            fillerColumnHeader.NotifyPropertyChanged(d, e);
                        }
                    }
                }
            }
        }

        #endregion

        #region Column Virtualization

        /// <summary>
        ///     Property changed callback for VirtualizingStackPanel.IsVirtualizing property
        /// </summary>
        private static void OnIsVirtualizingPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            DataGridColumnHeadersPresenter headersPresenter = (DataGridColumnHeadersPresenter)d;
            DataGridHelper.TransferProperty(headersPresenter, VirtualizingPanel.IsVirtualizingProperty);
            if (e.OldValue != headersPresenter.GetValue(VirtualizingPanel.IsVirtualizingProperty))
            {
                headersPresenter.InvalidateDataGridCellsPanelMeasureAndArrange();
            }
        }

        /// <summary>
        ///     Coercion callback for VirtualizingStackPanel.IsVirtualizing property
        /// </summary>
        private static object OnCoerceIsVirtualizingProperty(DependencyObject d, object baseValue)
        {
            var headersPresenter = d as DataGridColumnHeadersPresenter;
            return DataGridHelper.GetCoercedTransferPropertyValue(
                headersPresenter,
                baseValue,
                VirtualizingPanel.IsVirtualizingProperty,
                headersPresenter.ParentDataGrid,
                DataGrid.EnableColumnVirtualizationProperty);
        }

        /// <summary>
        ///     Helper method which invalidate the underlying itemshost's measure and arrange
        /// </summary>
        private void InvalidateDataGridCellsPanelMeasureAndArrange()
        {
            if (_internalItemsHost != null)
            {
                _internalItemsHost.InvalidateMeasure();
                _internalItemsHost.InvalidateArrange();
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

        #endregion

        #region Column Reordering

        /// <summary>
        ///     Override of VisualChildrenCount which accomodates the indicators as visual children
        /// </summary>
        protected override int VisualChildrenCount
        {
            get
            {
                int visualChildrenCount = base.VisualChildrenCount;
                if (_columnHeaderDragIndicator != null)
                {
                    visualChildrenCount++;
                }

                if (_columnHeaderDropLocationIndicator != null)
                {
                    visualChildrenCount++;
                }

                return visualChildrenCount;
            }
        }

        /// <summary>
        ///     Override of GetVisualChild which accomodates the indicators as visual children
        /// </summary>
        protected override Visual GetVisualChild(int index)
        {
            int visualChildrenCount = base.VisualChildrenCount;
            if (index == visualChildrenCount)
            {
                if (_columnHeaderDragIndicator != null)
                {
                    return _columnHeaderDragIndicator;
                }
                else if (_columnHeaderDropLocationIndicator != null)
                {
                    return _columnHeaderDropLocationIndicator;
                }
            }

            if (index == visualChildrenCount + 1)
            {
                if (_columnHeaderDragIndicator != null && _columnHeaderDropLocationIndicator != null)
                {
                    return _columnHeaderDropLocationIndicator;
                }
            }

            return base.GetVisualChild(index);
        }

        /// <summary>
        ///     Gets called on mouse left button down of child header, and ensures preparation for column header drag
        /// </summary>
        internal void OnHeaderMouseLeftButtonDown(MouseButtonEventArgs e)
        {
            if (ParentDataGrid == null)
            {
                return;
            }

            if (_columnHeaderDragIndicator != null)
            {
                RemoveVisualChild(_columnHeaderDragIndicator);
                _columnHeaderDragIndicator = null;
            }

            if (_columnHeaderDropLocationIndicator != null)
            {
                RemoveVisualChild(_columnHeaderDropLocationIndicator);
                _columnHeaderDropLocationIndicator = null;
            }

            Point mousePosition = e.GetPosition(this);
            DataGridColumnHeader header = FindColumnHeaderByPosition(mousePosition);

            if (header != null)
            {
                DataGridColumn column = header.Column;

                if (ParentDataGrid.CanUserReorderColumns && column.CanUserReorder)
                {
                    PrepareColumnHeaderDrag(header, e.GetPosition(this), e.GetPosition(header));
                }
            }
            else
            {
                _isColumnHeaderDragging = false;
                _prepareColumnHeaderDragging = false;
                _draggingSrcColumnHeader = null;
                InvalidateArrange();
            }
        }

        /// <summary>
        ///     Gets called on mouse move of child header, and ensures column header drag
        /// </summary>
        internal void OnHeaderMouseMove(MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                if (_prepareColumnHeaderDragging)
                {
                    _columnHeaderDragCurrentPosition = e.GetPosition(this);

                    if (!_isColumnHeaderDragging)
                    {
                        if (CheckStartColumnHeaderDrag(_columnHeaderDragCurrentPosition, _columnHeaderDragStartPosition))
                        {
                            StartColumnHeaderDrag();
                        }
                    }
                    else
                    {
                        bool shouldDisplayDragIndicator = IsMousePositionValidForColumnDrag(2.0);
                        Visibility dragIndicatorVisibility = shouldDisplayDragIndicator ? Visibility.Visible : Visibility.Collapsed;

                        if (_columnHeaderDragIndicator != null)
                        {
                            _columnHeaderDragIndicator.Visibility = dragIndicatorVisibility;
                        }

                        if (_columnHeaderDropLocationIndicator != null)
                        {
                            _columnHeaderDropLocationIndicator.Visibility = dragIndicatorVisibility;
                        }

                        InvalidateArrange();

                        DragDeltaEventArgs dragDeltaEventArgs = new DragDeltaEventArgs(
                            _columnHeaderDragCurrentPosition.X - _columnHeaderDragStartPosition.X,
                            _columnHeaderDragCurrentPosition.Y - _columnHeaderDragStartPosition.Y);

                        _columnHeaderDragStartPosition = _columnHeaderDragCurrentPosition;
                        ParentDataGrid.OnColumnHeaderDragDelta(dragDeltaEventArgs);
                    }
                }
            }
        }

        /// <summary>
        ///     Gets called on mouse left button up of child header, and ensures reordering of columns on successful completion of drag
        /// </summary>
        internal void OnHeaderMouseLeftButtonUp(MouseButtonEventArgs e)
        {
            if (_isColumnHeaderDragging)
            {
                _columnHeaderDragCurrentPosition = e.GetPosition(this);
                FinishColumnHeaderDrag(false);
            }
            else
            {
                ClearColumnHeaderDragInfo();
            }
        }

        /// <summary>
        ///     Gets called on mouse lost capture of child header and ensures that when capture gets lost
        ///     the drag ends in appropriate state. In this case it restore the drag state to
        ///     the start of the operation by finishing the drag with cancel flag
        /// </summary>
        internal void OnHeaderLostMouseCapture(MouseEventArgs e)
        {
            if (_isColumnHeaderDragging &&
                Mouse.LeftButton == MouseButtonState.Pressed)
            {
                FinishColumnHeaderDrag(true);
            }
        }

        /// <summary>
        ///     Helper method which clears the header drag state
        /// </summary>
        private void ClearColumnHeaderDragInfo()
        {
            _isColumnHeaderDragging = false;
            _prepareColumnHeaderDragging = false;
            _draggingSrcColumnHeader = null;
            if (_columnHeaderDragIndicator != null)
            {
                RemoveVisualChild(_columnHeaderDragIndicator);
                _columnHeaderDragIndicator = null;
            }

            if (_columnHeaderDropLocationIndicator != null)
            {
                RemoveVisualChild(_columnHeaderDropLocationIndicator);
                _columnHeaderDropLocationIndicator = null;
            }
        }

        /// <summary>
        ///     Method which prepares the state for the start of column header drag
        /// </summary>
        private void PrepareColumnHeaderDrag(DataGridColumnHeader header, Point pos, Point relativePos)
        {
            _prepareColumnHeaderDragging = true;
            _isColumnHeaderDragging = false;
            _draggingSrcColumnHeader = header;
            _columnHeaderDragStartPosition = pos;
            _columnHeaderDragStartRelativePosition = relativePos;
        }

        /// <summary>
        ///     Method which checks if mouse move is sufficient to start the drag
        /// </summary>
        private static bool CheckStartColumnHeaderDrag(Point currentPos, Point originalPos)
        {
            return DoubleUtil.GreaterThan(Math.Abs(currentPos.X - originalPos.X), SystemParameters.MinimumHorizontalDragDistance);
        }

        /// <summary>
        ///     Method which checks during and after the drag if the position is valid for the drop
        /// </summary>
        private bool IsMousePositionValidForColumnDrag(double dragFactor)
        {
            int nearestDisplayIndex = -1;
            return IsMousePositionValidForColumnDrag(dragFactor, out nearestDisplayIndex);
        }

        /// <summary>
        ///     Method which checks during and after the drag if the position is valid for the drop and returns the drop display index
        /// </summary>
        private bool IsMousePositionValidForColumnDrag(double dragFactor, out int nearestDisplayIndex)
        {
            nearestDisplayIndex = -1;
            bool isDraggingColumnFrozen = false;
            if (_draggingSrcColumnHeader.Column != null)
            {
                isDraggingColumnFrozen = _draggingSrcColumnHeader.Column.IsFrozen;
            }

            int frozenCount = 0;
            if (ParentDataGrid != null)
            {
                frozenCount = ParentDataGrid.FrozenColumnCount;
            }

            nearestDisplayIndex = FindDisplayIndexByPosition(_columnHeaderDragCurrentPosition, true);
            if (isDraggingColumnFrozen && nearestDisplayIndex >= frozenCount)
            {
                return false;
            }

            if (!isDraggingColumnFrozen && nearestDisplayIndex < frozenCount)
            {
                return false;
            }

            double height = 0.0;

            if (_columnHeaderDragIndicator == null)
            {
                height = _draggingSrcColumnHeader.RenderSize.Height;
            }
            else
            {
                height = Math.Max(_draggingSrcColumnHeader.RenderSize.Height, _columnHeaderDragIndicator.Height);
            }

            return DoubleUtil.LessThanOrClose(-height * dragFactor, _columnHeaderDragCurrentPosition.Y) &&
                   DoubleUtil.LessThanOrClose(_columnHeaderDragCurrentPosition.Y, height * (dragFactor + 1));
        }

        /// <summary>
        ///     Method which start the column header drag. Includes raising events and creating default ghosts
        /// </summary>
        private void StartColumnHeaderDrag()
        {
            Debug.Assert(ParentDataGrid != null, "ParentDataGrid is null");

            _columnHeaderDragStartPosition = _columnHeaderDragCurrentPosition;
            DragStartedEventArgs dragStartedEventArgs = new DragStartedEventArgs(_columnHeaderDragStartPosition.X, _columnHeaderDragStartPosition.Y);
            ParentDataGrid.OnColumnHeaderDragStarted(dragStartedEventArgs);

            DataGridColumnReorderingEventArgs reorderingEventArgs = new DataGridColumnReorderingEventArgs(_draggingSrcColumnHeader.Column);

            _columnHeaderDragIndicator = CreateColumnHeaderDragIndicator();
            _columnHeaderDropLocationIndicator = CreateColumnHeaderDropIndicator();

            reorderingEventArgs.DragIndicator = _columnHeaderDragIndicator;
            reorderingEventArgs.DropLocationIndicator = _columnHeaderDropLocationIndicator;
            ParentDataGrid.OnColumnReordering(reorderingEventArgs);

            if (!reorderingEventArgs.Cancel)
            {
                _isColumnHeaderDragging = true;
                _columnHeaderDragIndicator = reorderingEventArgs.DragIndicator;
                _columnHeaderDropLocationIndicator = reorderingEventArgs.DropLocationIndicator;

                if (_columnHeaderDragIndicator != null)
                {
                    SetDefaultsOnDragIndicator();
                    AddVisualChild(_columnHeaderDragIndicator);
                }

                if (_columnHeaderDropLocationIndicator != null)
                {
                    SetDefaultsOnDropIndicator();
                    AddVisualChild(_columnHeaderDropLocationIndicator);
                }

                _draggingSrcColumnHeader.SuppressClickEvent = true;
                InvalidateMeasure();
            }
            else
            {
                FinishColumnHeaderDrag(true);
            }
        }

        /// <summary>
        ///     Method which returns a default control for column header drag indicator
        /// </summary>
        private Control CreateColumnHeaderDragIndicator()
        {
            Debug.Assert(_draggingSrcColumnHeader != null, "Dragging header is null");

            DataGridColumnFloatingHeader floatingHeader = new DataGridColumnFloatingHeader();
            floatingHeader.ReferenceHeader = _draggingSrcColumnHeader;
            return floatingHeader;
        }

        /// <summary>
        ///     Method which set the default values on drag indicator
        /// </summary>
        private void SetDefaultsOnDragIndicator()
        {
            Debug.Assert(_columnHeaderDragIndicator != null, "Drag indicator is null");
            Debug.Assert(_draggingSrcColumnHeader != null, "Dragging header is null");
            DataGridColumn column = _draggingSrcColumnHeader.Column;
            Style style = null;
            if (column != null)
            {
                style = column.DragIndicatorStyle;
            }

            _columnHeaderDragIndicator.Style = style;
            _columnHeaderDragIndicator.CoerceValue(WidthProperty);
            _columnHeaderDragIndicator.CoerceValue(HeightProperty);
        }

        /// <summary>
        ///     Method which returns the default control for the column header drop indicator
        /// </summary>
        private Control CreateColumnHeaderDropIndicator()
        {
            Debug.Assert(_draggingSrcColumnHeader != null, "Dragging header is null");

            DataGridColumnDropSeparator indicator = new DataGridColumnDropSeparator();
            indicator.ReferenceHeader = _draggingSrcColumnHeader;
            return indicator;
        }

        /// <summary>
        ///     Method which sets the default values on drop indicator
        /// </summary>
        private void SetDefaultsOnDropIndicator()
        {
            Debug.Assert(_columnHeaderDropLocationIndicator != null, "Drag indicator is null");
            Debug.Assert(_draggingSrcColumnHeader != null, "Dragging header is null");
            Style style = null;
            if (ParentDataGrid != null)
            {
                style = ParentDataGrid.DropLocationIndicatorStyle;
            }

            _columnHeaderDropLocationIndicator.Style = style;
            _columnHeaderDropLocationIndicator.CoerceValue(WidthProperty);
            _columnHeaderDropLocationIndicator.CoerceValue(HeightProperty);
        }

        /// <summary>
        ///     Method which completes the column header drag. Includes raising of events and changing column display index if needed.
        /// </summary>
        private void FinishColumnHeaderDrag(bool isCancel)
        {
            Debug.Assert(ParentDataGrid != null, "ParentDataGrid is null");
            _prepareColumnHeaderDragging = false;
            _isColumnHeaderDragging = false;

            _draggingSrcColumnHeader.SuppressClickEvent = false;

            if (_columnHeaderDragIndicator != null)
            {
                _columnHeaderDragIndicator.Visibility = Visibility.Collapsed;
                DataGridColumnFloatingHeader floatingHeader = _columnHeaderDragIndicator as DataGridColumnFloatingHeader;
                if (floatingHeader != null)
                {
                    floatingHeader.ClearHeader();
                }

                RemoveVisualChild(_columnHeaderDragIndicator);
            }

            if (_columnHeaderDropLocationIndicator != null)
            {
                _columnHeaderDropLocationIndicator.Visibility = Visibility.Collapsed;
                DataGridColumnDropSeparator separator = _columnHeaderDropLocationIndicator as DataGridColumnDropSeparator;
                if (separator != null)
                {
                    separator.ReferenceHeader = null;
                }

                RemoveVisualChild(_columnHeaderDropLocationIndicator);
            }

            DragCompletedEventArgs dragCompletedEventArgs = new DragCompletedEventArgs(
                _columnHeaderDragCurrentPosition.X - _columnHeaderDragStartPosition.X,
                _columnHeaderDragCurrentPosition.Y - _columnHeaderDragStartPosition.Y,
                isCancel);

            ParentDataGrid.OnColumnHeaderDragCompleted(dragCompletedEventArgs);
            _draggingSrcColumnHeader.InvalidateArrange();

            if (!isCancel)
            {
                int newDisplayIndex = -1;
                bool dragEndPositionValid = IsMousePositionValidForColumnDrag(
                    2.0,
                    out newDisplayIndex);

                DataGridColumn column = _draggingSrcColumnHeader.Column;
                if (column != null && dragEndPositionValid && newDisplayIndex != column.DisplayIndex)
                {
                    column.DisplayIndex = newDisplayIndex;

                    DataGridColumnEventArgs columnEventArgs = new DataGridColumnEventArgs(_draggingSrcColumnHeader.Column);
                    ParentDataGrid.OnColumnReordered(columnEventArgs);
                }
            }

            _draggingSrcColumnHeader = null;
            _columnHeaderDragIndicator = null;
            _columnHeaderDropLocationIndicator = null;
        }

        /// <summary>
        ///     Helper method to determine the display index based on the given position
        /// </summary>
        private int FindDisplayIndexByPosition(Point startPos, bool findNearestColumn)
        {
            Point headerPos;
            int displayIndex;
            DataGridColumnHeader header;
            FindDisplayIndexAndHeaderPosition(startPos, findNearestColumn, out displayIndex, out headerPos, out header);
            return displayIndex;
        }

        /// <summary>
        ///     Helper method to determine the column header based on the given position
        /// </summary>
        private DataGridColumnHeader FindColumnHeaderByPosition(Point startPos)
        {
            Point headerPos;
            int displayIndex;
            DataGridColumnHeader header;
            FindDisplayIndexAndHeaderPosition(startPos, false, out displayIndex, out headerPos, out header);
            return header;
        }

        /// <summary>
        ///     Helper method to determine the position of drop indicator based on the given mouse position
        /// </summary>
        private Point FindColumnHeaderPositionByCurrentPosition(Point startPos, bool findNearestColumn)
        {
            Point headerPos;
            int displayIndex;
            DataGridColumnHeader header;
            FindDisplayIndexAndHeaderPosition(startPos, findNearestColumn, out displayIndex, out headerPos, out header);
            return headerPos;
        }

        /// <summary>
        ///     Helper method which estimates the column width
        /// </summary>
        private static double GetColumnEstimatedWidth(DataGridColumn column, double averageColumnWidth)
        {
            double columnEstimatedWidth = column.Width.DisplayValue;
            if (DoubleUtil.IsNaN(columnEstimatedWidth))
            {
                columnEstimatedWidth = Math.Max(averageColumnWidth, column.MinWidth);
                columnEstimatedWidth = Math.Min(columnEstimatedWidth, column.MaxWidth);
            }

            return columnEstimatedWidth;
        }

        /// <summary>
        ///     Helper method to find display index, header and header start position based on given mouse position
        /// </summary>
        private void FindDisplayIndexAndHeaderPosition(Point startPos, bool findNearestColumn, out int displayIndex, out Point headerPos, out DataGridColumnHeader header)
        {
            Debug.Assert(ParentDataGrid != null, "ParentDataGrid is null");

            Point originPoint = new Point(0, 0);
            headerPos = originPoint;
            displayIndex = -1;
            header = null;

            if (startPos.X < 0.0)
            {
                if (findNearestColumn)
                {
                    displayIndex = 0;
                }

                return;
            }

            double headerStartX = 0.0;
            double headerEndX = 0.0;
            int i = 0;
            DataGrid dataGrid = ParentDataGrid;
            double averageColumnWidth = dataGrid.InternalColumns.AverageColumnWidth;
            bool firstVisibleNonFrozenColumnHandled = false;
            for (i = 0; i < dataGrid.Columns.Count; i++)
            {
                displayIndex++;
                DataGridColumnHeader currentHeader = dataGrid.ColumnHeaderFromDisplayIndex(i);
                if (currentHeader == null)
                {
                    DataGridColumn column = dataGrid.ColumnFromDisplayIndex(i);
                    if (!column.IsVisible)
                    {
                        continue;
                    }
                    else
                    {
                        headerStartX = headerEndX;
                        if (i >= dataGrid.FrozenColumnCount &&
                            !firstVisibleNonFrozenColumnHandled)
                        {
                            headerStartX -= dataGrid.HorizontalScrollOffset;
                            firstVisibleNonFrozenColumnHandled = true;
                        }

                        headerEndX = headerStartX + GetColumnEstimatedWidth(column, averageColumnWidth);
                    }
                }
                else
                {
                    GeneralTransform transform = currentHeader.TransformToAncestor(this);
                    headerStartX = transform.Transform(originPoint).X;
                    headerEndX = headerStartX + currentHeader.RenderSize.Width;
                }

                if (DoubleUtil.LessThanOrClose(startPos.X, headerStartX))
                {
                    break;
                }

                if (DoubleUtil.GreaterThanOrClose(startPos.X, headerStartX) &&
                    DoubleUtil.LessThanOrClose(startPos.X, headerEndX))
                {
                    if (findNearestColumn)
                    {
                        double headerMidX = (headerStartX + headerEndX) * 0.5;
                        if (DoubleUtil.GreaterThanOrClose(startPos.X, headerMidX))
                        {
                            headerStartX = headerEndX;
                            displayIndex++;
                        }

                        if (_draggingSrcColumnHeader != null && _draggingSrcColumnHeader.Column != null && _draggingSrcColumnHeader.Column.DisplayIndex < displayIndex)
                        {
                            displayIndex--;
                        }
                    }
                    else
                    {
                        header = currentHeader;
                    }

                    break;
                }
            }

            if (i == dataGrid.Columns.Count)
            {
                displayIndex = dataGrid.Columns.Count - 1;
                headerStartX = headerEndX;
            }

            headerPos.X = headerStartX;
            return;
        }

        #endregion

        #region Helpers

        private DataGridColumnHeaderCollection HeaderCollection
        {
            get
            {
                return ItemsSource as DataGridColumnHeaderCollection;
            }
        }

        internal DataGrid ParentDataGrid
        {
            get
            {
                if (_parentDataGrid == null)
                {
                    _parentDataGrid = DataGridHelper.FindParent<DataGrid>(this);
                }

                return _parentDataGrid;
            }
        }

        internal ContainerTracking<DataGridColumnHeader> HeaderTrackingRoot
        {
            get
            {
                return _headerTrackingRoot;
            }
        }

        #endregion

        #region Data

        private ContainerTracking<DataGridColumnHeader> _headerTrackingRoot;

        private DataGrid _parentDataGrid = null;

        private bool _prepareColumnHeaderDragging;
        private bool _isColumnHeaderDragging;
        private DataGridColumnHeader _draggingSrcColumnHeader;
        private Point _columnHeaderDragStartPosition;
        private Point _columnHeaderDragStartRelativePosition;
        private Point _columnHeaderDragCurrentPosition;
        private Control _columnHeaderDropLocationIndicator;
        private Control _columnHeaderDragIndicator;

        private Panel _internalItemsHost;

        #endregion
    }
}
