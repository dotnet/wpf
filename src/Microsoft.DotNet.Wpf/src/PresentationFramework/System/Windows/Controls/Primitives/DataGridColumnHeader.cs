// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Automation.Peers;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Automation;

using MS.Internal;

namespace System.Windows.Controls.Primitives
{
    /// <summary>
    /// Container for the Column header object.  This is instantiated by the DataGridColumnHeadersPresenter.
    /// </summary>
    [TemplatePart(Name = "PART_LeftHeaderGripper", Type = typeof(Thumb))]
    [TemplatePart(Name = "PART_RightHeaderGripper", Type = typeof(Thumb))]
    public class DataGridColumnHeader : ButtonBase, IProvideDataGridColumn
    {
        #region Constructors

        static DataGridColumnHeader()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(DataGridColumnHeader), new FrameworkPropertyMetadata(typeof(DataGridColumnHeader)));

            ContentProperty.OverrideMetadata(typeof(DataGridColumnHeader), new FrameworkPropertyMetadata(OnNotifyPropertyChanged, OnCoerceContent));
            ContentTemplateProperty.OverrideMetadata(typeof(DataGridColumnHeader), new FrameworkPropertyMetadata(null, OnNotifyPropertyChanged, OnCoerceContentTemplate));
            ContentTemplateSelectorProperty.OverrideMetadata(typeof(DataGridColumnHeader), new FrameworkPropertyMetadata(null, OnNotifyPropertyChanged, OnCoerceContentTemplateSelector));
            ContentStringFormatProperty.OverrideMetadata(typeof(DataGridColumnHeader), new FrameworkPropertyMetadata(null, OnNotifyPropertyChanged, OnCoerceStringFormat));
            StyleProperty.OverrideMetadata(typeof(DataGridColumnHeader), new FrameworkPropertyMetadata(null, OnNotifyPropertyChanged, OnCoerceStyle));
            HeightProperty.OverrideMetadata(typeof(DataGridColumnHeader), new FrameworkPropertyMetadata(OnNotifyPropertyChanged, OnCoerceHeight));

            FocusableProperty.OverrideMetadata(typeof(DataGridColumnHeader), new FrameworkPropertyMetadata(false));
            ClipProperty.OverrideMetadata(typeof(DataGridColumnHeader), new FrameworkPropertyMetadata(null, OnCoerceClip));
            AutomationProperties.IsOffscreenBehaviorProperty.OverrideMetadata(typeof(DataGridColumnHeader), new FrameworkPropertyMetadata(IsOffscreenBehavior.FromClip));
        }

        /// <summary>
        ///     Instantiates a new instance of this class.
        /// </summary>
        public DataGridColumnHeader()
        {
            _tracker = new ContainerTracking<DataGridColumnHeader>(this);
        }

        #endregion

        #region Public API

        /// <summary>
        ///     The Column associated with this DataGridColumnHeader.
        /// </summary>
        public DataGridColumn Column
        {
            get
            {
                return _column;
            }
        }

        /// <summary>
        ///     Property that indicates the brush to use when drawing seperators between headers.
        /// </summary>
        public Brush SeparatorBrush
        {
            get { return (Brush)GetValue(SeparatorBrushProperty); }
            set { SetValue(SeparatorBrushProperty, value); }
        }

        /// <summary>
        ///     DependencyProperty for SeperatorBrush.
        /// </summary>
        public static readonly DependencyProperty SeparatorBrushProperty =
            DependencyProperty.Register("SeparatorBrush", typeof(Brush), typeof(DataGridColumnHeader), new FrameworkPropertyMetadata(null));

        /// <summary>
        ///     Property that indicates the Visibility for the header seperators.
        /// </summary>
        public Visibility SeparatorVisibility
        {
            get { return (Visibility)GetValue(SeparatorVisibilityProperty); }
            set { SetValue(SeparatorVisibilityProperty, value); }
        }

        /// <summary>
        ///     DependencyProperty for SeperatorBrush.
        /// </summary>
        public static readonly DependencyProperty SeparatorVisibilityProperty =
            DependencyProperty.Register("SeparatorVisibility", typeof(Visibility), typeof(DataGridColumnHeader), new FrameworkPropertyMetadata(Visibility.Visible));

        #endregion

        #region Column Header Generation

        /// <summary>
        /// Prepares a column header to be used.  Sets up the association between the column header and its column.
        /// </summary>
        internal void PrepareColumnHeader(object item, DataGridColumn column)
        {
            Debug.Assert(column != null, "This header must have been generated with for a particular column");
            Debug.Assert(column.Header == item, "The data item for a ColumnHeader is the Header property of a column");
            _column = column;
            TabIndex = column.DisplayIndex;

            DataGridHelper.TransferProperty(this, ContentProperty);
            DataGridHelper.TransferProperty(this, ContentTemplateProperty);
            DataGridHelper.TransferProperty(this, ContentTemplateSelectorProperty);
            DataGridHelper.TransferProperty(this, ContentStringFormatProperty);
            DataGridHelper.TransferProperty(this, StyleProperty);
            DataGridHelper.TransferProperty(this, HeightProperty);

            CoerceValue(CanUserSortProperty);
            CoerceValue(SortDirectionProperty);
            CoerceValue(IsFrozenProperty);
            CoerceValue(ClipProperty);
            CoerceValue(DisplayIndexProperty);
        }

        internal void ClearHeader()
        {
            _column = null;
        }

        /// <summary>
        ///     Used by the DataGridRowGenerator owner to send notifications to the cell container.
        /// </summary>
        internal ContainerTracking<DataGridColumnHeader> Tracker
        {
            get { return _tracker; }
        }

        #endregion

        #region Resize Gripper

        /// <summary>
        /// DependencyPropertyKey for DisplayIndex property
        /// </summary>
        private static readonly DependencyPropertyKey DisplayIndexPropertyKey =
                DependencyProperty.RegisterReadOnly(
                        "DisplayIndex",
                        typeof(int),
                        typeof(DataGridColumnHeader),
                        new FrameworkPropertyMetadata(-1, new PropertyChangedCallback(OnDisplayIndexChanged), new CoerceValueCallback(OnCoerceDisplayIndex)));

        /// <summary>
        ///     The DependencyProperty for the DisplayIndex property.
        /// </summary>
        public static readonly DependencyProperty DisplayIndexProperty = DisplayIndexPropertyKey.DependencyProperty;

        /// <summary>
        /// The property which represents the displayindex of the  column corresponding to this header
        /// </summary>
        public int DisplayIndex
        {
            get { return (int)GetValue(DisplayIndexProperty); }
        }

        /// <summary>
        /// Coercion callback for DisplayIndex property
        /// </summary>
        private static object OnCoerceDisplayIndex(DependencyObject d, object baseValue)
        {
            DataGridColumnHeader header = (DataGridColumnHeader)d;
            DataGridColumn column = header.Column;

            if (column != null)
            {
                return column.DisplayIndex;
            }

            return -1;
        }

        /// <summary>
        /// Property changed call back for DisplayIndex property.
        /// Sets the visibility of resize grippers accordingly
        /// </summary>
        /// <param name="d"></param>
        /// <param name="e"></param>
        private static void OnDisplayIndexChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            DataGridColumnHeader header = (DataGridColumnHeader)d;
            DataGridColumn column = header.Column;
            if (column != null)
            {
                DataGrid dataGrid = column.DataGridOwner;
                if (dataGrid != null)
                {
                    header.SetLeftGripperVisibility();
                    DataGridColumnHeader nextColumnHeader = dataGrid.ColumnHeaderFromDisplayIndex(header.DisplayIndex + 1);
                    if (nextColumnHeader != null)
                    {
                        nextColumnHeader.SetLeftGripperVisibility(column.CanUserResize);
                    }
                }
            }
        }

        /// <summary>
        /// Override for <see cref="System.Windows.FrameworkElement.OnApplyTemplate">FrameworkElement.OnApplyTemplate</see>
        /// </summary>
        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            HookupGripperEvents();
        }

        /// <summary>
        /// Find grippers and register drag events
        ///
        /// The default style for DataGridHeader is
        /// +-------------------------------+
        /// +---------+           +---------+
        /// + Gripper +  Header   + Gripper +
        /// +         +           +         +
        /// +---------+           +---------+
        /// +-------------------------------+
        ///
        /// The reason we have two grippers is we can't extend the right gripper to straddle the line between two
        /// headers; the header to the right would render on top of it.
        /// We resize a column by grabbing the gripper to the right; the leftmost gripper thus adjusts the width of
        /// the column to its left.
        /// </summary>
        private void HookupGripperEvents()
        {
            UnhookGripperEvents();

            _leftGripper = GetTemplateChild(LeftHeaderGripperTemplateName) as Thumb;
            _rightGripper = GetTemplateChild(RightHeaderGripperTemplateName) as Thumb;

            if (_leftGripper != null)
            {
                _leftGripper.DragStarted += new DragStartedEventHandler(OnColumnHeaderGripperDragStarted);
                _leftGripper.DragDelta += new DragDeltaEventHandler(OnColumnHeaderResize);
                _leftGripper.DragCompleted += new DragCompletedEventHandler(OnColumnHeaderGripperDragCompleted);
                _leftGripper.MouseDoubleClick += new MouseButtonEventHandler(OnGripperDoubleClicked);
                SetLeftGripperVisibility();
            }

            if (_rightGripper != null)
            {
                _rightGripper.DragStarted += new DragStartedEventHandler(OnColumnHeaderGripperDragStarted);
                _rightGripper.DragDelta += new DragDeltaEventHandler(OnColumnHeaderResize);
                _rightGripper.DragCompleted += new DragCompletedEventHandler(OnColumnHeaderGripperDragCompleted);
                _rightGripper.MouseDoubleClick += new MouseButtonEventHandler(OnGripperDoubleClicked);
                SetRightGripperVisibility();
            }
        }

        /// <summary>
        /// Clear gripper event
        /// </summary>
        private void UnhookGripperEvents()
        {
            if (_leftGripper != null)
            {
                _leftGripper.DragStarted -= new DragStartedEventHandler(OnColumnHeaderGripperDragStarted);
                _leftGripper.DragDelta -= new DragDeltaEventHandler(OnColumnHeaderResize);
                _leftGripper.DragCompleted -= new DragCompletedEventHandler(OnColumnHeaderGripperDragCompleted);
                _leftGripper.MouseDoubleClick -= new MouseButtonEventHandler(OnGripperDoubleClicked);
                _leftGripper = null;
            }

            if (_rightGripper != null)
            {
                _rightGripper.DragStarted -= new DragStartedEventHandler(OnColumnHeaderGripperDragStarted);
                _rightGripper.DragDelta -= new DragDeltaEventHandler(OnColumnHeaderResize);
                _rightGripper.DragCompleted -= new DragCompletedEventHandler(OnColumnHeaderGripperDragCompleted);
                _rightGripper.MouseDoubleClick -= new MouseButtonEventHandler(OnGripperDoubleClicked);
                _rightGripper = null;
            }
        }

        /// <summary>
        /// Returns either this header or the one before it depending on which Gripper fired the event.
        /// </summary>
        /// <param name="sender"></param>
        private DataGridColumnHeader HeaderToResize(object gripper)
        {
            return (gripper == _rightGripper) ? this : PreviousVisibleHeader;
        }

        // Save the original widths before header resize
        private void OnColumnHeaderGripperDragStarted(object sender, DragStartedEventArgs e)
        {
            DataGridColumnHeader header = HeaderToResize(sender);

            if (header != null)
            {
                if (header.Column != null)
                {
                    DataGrid dataGrid = header.Column.DataGridOwner;
                    if (dataGrid != null)
                    {
                        dataGrid.InternalColumns.OnColumnResizeStarted();
                    }
                }

                e.Handled = true;
            }
        }

        private void OnColumnHeaderResize(object sender, DragDeltaEventArgs e)
        {
            DataGridColumnHeader header = HeaderToResize(sender);
            if (header != null)
            {
                RecomputeColumnWidthsOnColumnResize(header, e.HorizontalChange);
                e.Handled = true;
            }
        }

        /// <summary>
        /// Method which recomputes the widths of columns on resize of a header
        /// </summary>
        private static void RecomputeColumnWidthsOnColumnResize(DataGridColumnHeader header, double horizontalChange)
        {
            Debug.Assert(header != null, "Header should not be null");

            DataGridColumn resizingColumn = header.Column;
            if (resizingColumn == null)
            {
                return;
            }

            DataGrid dataGrid = resizingColumn.DataGridOwner;
            if (dataGrid == null)
            {
                return;
            }

            dataGrid.InternalColumns.RecomputeColumnWidthsOnColumnResize(resizingColumn, horizontalChange, false);
        }

        private void OnColumnHeaderGripperDragCompleted(object sender, DragCompletedEventArgs e)
        {
            DataGridColumnHeader header = HeaderToResize(sender);

            if (header != null)
            {
                if (header.Column != null)
                {
                    DataGrid dataGrid = header.Column.DataGridOwner;
                    if (dataGrid != null)
                    {
                        dataGrid.InternalColumns.OnColumnResizeCompleted(e.Canceled);
                    }
                }

                e.Handled = true;
            }
        }

        private void OnGripperDoubleClicked(object sender, MouseButtonEventArgs e)
        {
            DataGridColumnHeader header = HeaderToResize(sender);

            if (header != null && header.Column != null)
            {
                // DataGridLength is a struct, so setting to Auto resets desired and display widths to 0.0.
                header.Column.Width = DataGridLength.Auto;
                e.Handled = true;
            }
        }

        private DataGridLength ColumnWidth
        {
            get { return Column != null ? Column.Width : DataGridLength.Auto; }
        }

        private double ColumnActualWidth
        {
            get { return Column != null ? Column.ActualWidth : ActualWidth; }
        }

        #endregion

        #region Property Change Notification Propagation

        /// <summary>
        ///     Notifies the Header of a property change.
        /// </summary>
        private static void OnNotifyPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((DataGridColumnHeader)d).NotifyPropertyChanged(d, e);
        }

        /// <summary>
        ///     Notification for column header-related DependencyProperty changes from the grid or from columns.
        /// </summary>
        internal void NotifyPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            DataGridColumn column = d as DataGridColumn;
            if ((column != null) && (column != Column))
            {
                // This notification does not apply to this column header
                return;
            }

            if (e.Property == DataGridColumn.WidthProperty)
            {
                DataGridHelper.OnColumnWidthChanged(this, e);
            }
            else if (e.Property == DataGridColumn.HeaderProperty || e.Property == ContentProperty)
            {
                DataGridHelper.TransferProperty(this, ContentProperty);
            }
            else if (e.Property == DataGridColumn.HeaderTemplateProperty || e.Property == ContentTemplateProperty)
            {
                DataGridHelper.TransferProperty(this, ContentTemplateProperty);
            }
            else if (e.Property == DataGridColumn.HeaderTemplateSelectorProperty || e.Property == ContentTemplateSelectorProperty)
            {
                DataGridHelper.TransferProperty(this, ContentTemplateSelectorProperty);
            }
            else if (e.Property == DataGridColumn.HeaderStringFormatProperty || e.Property == ContentStringFormatProperty)
            {
                DataGridHelper.TransferProperty(this, ContentStringFormatProperty);
            }
            else if (e.Property == DataGrid.ColumnHeaderStyleProperty || e.Property == DataGridColumn.HeaderStyleProperty || e.Property == StyleProperty)
            {
                DataGridHelper.TransferProperty(this, StyleProperty);
            }
            else if (e.Property == DataGrid.ColumnHeaderHeightProperty || e.Property == HeightProperty)
            {
                DataGridHelper.TransferProperty(this, HeightProperty);
            }
            else if (e.Property == DataGridColumn.DisplayIndexProperty)
            {
                CoerceValue(DisplayIndexProperty);
                TabIndex = column.DisplayIndex;
            }
            else if (e.Property == DataGrid.CanUserResizeColumnsProperty)
            {
                OnCanUserResizeColumnsChanged();
            }
            else if (e.Property == DataGridColumn.CanUserSortProperty)
            {
                CoerceValue(CanUserSortProperty);
            }
            else if (e.Property == DataGridColumn.SortDirectionProperty)
            {
                CoerceValue(SortDirectionProperty);
            }
            else if (e.Property == DataGridColumn.IsFrozenProperty)
            {
                CoerceValue(IsFrozenProperty);
            }
            else if (e.Property == DataGridColumn.CanUserResizeProperty)
            {
                OnCanUserResizeChanged();
            }
            else if (e.Property == DataGridColumn.VisibilityProperty)
            {
                OnColumnVisibilityChanged(e);
            }
        }

        private void OnCanUserResizeColumnsChanged()
        {
            Debug.Assert(Column != null, "column can't be null if we got a notification for this property change");
            if (Column.DataGridOwner != null)
            {
                SetLeftGripperVisibility();
                SetRightGripperVisibility();
            }
        }

        private void OnCanUserResizeChanged()
        {
            Debug.Assert(Column != null, "column can't be null if we got a notification for this property change");
            DataGrid dataGrid = Column.DataGridOwner;
            if (dataGrid != null)
            {
                SetNextHeaderLeftGripperVisibility(Column.CanUserResize);
                SetRightGripperVisibility();
            }
        }

        private void SetLeftGripperVisibility()
        {
            if (_leftGripper == null || Column == null)
            {
                return;
            }

            DataGrid dataGridOwner = Column.DataGridOwner;
            bool canPrevColumnResize = false;
            for (int index = DisplayIndex - 1; index >= 0; index--)
            {
                DataGridColumn column = dataGridOwner.ColumnFromDisplayIndex(index);
                if (column.IsVisible)
                {
                    canPrevColumnResize = column.CanUserResize;
                    break;
                }
            }
            SetLeftGripperVisibility(canPrevColumnResize);
        }

        private void SetLeftGripperVisibility(bool canPreviousColumnResize)
        {
            if (_leftGripper == null || Column == null)
            {
                return;
            }

            DataGrid dataGrid = Column.DataGridOwner;
            if (dataGrid != null && dataGrid.CanUserResizeColumns && canPreviousColumnResize)
            {
                _leftGripper.Visibility = Visibility.Visible;
            }
            else
            {
                _leftGripper.Visibility = Visibility.Collapsed;
            }
        }

        private void SetRightGripperVisibility()
        {
            if (_rightGripper == null || Column == null)
            {
                return;
            }

            DataGrid dataGrid = Column.DataGridOwner;
            if (dataGrid != null && dataGrid.CanUserResizeColumns && Column.CanUserResize)
            {
                _rightGripper.Visibility = Visibility.Visible;
            }
            else
            {
                _rightGripper.Visibility = Visibility.Collapsed;
            }
        }

        private void SetNextHeaderLeftGripperVisibility(bool canUserResize)
        {
            DataGrid dataGrid = Column.DataGridOwner;
            int columnCount = dataGrid.Columns.Count;
            for (int index = DisplayIndex + 1; index < columnCount; index++)
            {
                if (dataGrid.ColumnFromDisplayIndex(index).IsVisible)
                {
                    DataGridColumnHeader nextHeader = dataGrid.ColumnHeaderFromDisplayIndex(index);
                    if (nextHeader != null)
                    {
                        nextHeader.SetLeftGripperVisibility(canUserResize);
                    }
                    break;
                }
            }
        }

        private void OnColumnVisibilityChanged(DependencyPropertyChangedEventArgs e)
        {
            Debug.Assert(Column != null, "column can't be null if we got a notification for this property change");
            DataGrid dataGrid = Column.DataGridOwner;
            if (dataGrid != null)
            {
                bool oldIsVisible = (((Visibility)e.OldValue) == Visibility.Visible);
                bool newIsVisible = (((Visibility)e.NewValue) == Visibility.Visible);

                if (oldIsVisible != newIsVisible)
                {
                    if (newIsVisible)
                    {
                        SetLeftGripperVisibility();
                        SetRightGripperVisibility();
                        SetNextHeaderLeftGripperVisibility(Column.CanUserResize);
                    }
                    else
                    {
                        bool canPrevColumnResize = false;
                        for (int index = DisplayIndex - 1; index >= 0; index--)
                        {
                            DataGridColumn column = dataGrid.ColumnFromDisplayIndex(index);
                            if (column.IsVisible)
                            {
                                canPrevColumnResize = column.CanUserResize;
                                break;
                            }
                        }
                        SetNextHeaderLeftGripperVisibility(canPrevColumnResize);
                    }
                }
            }
        }

        #endregion

        #region Style and Template Coercion callbacks

        /// <summary>
        ///     Coerces the Content property.  We're choosing a value between Column.Header and the Content property on ColumnHeader.
        /// </summary>
        private static object OnCoerceContent(DependencyObject d, object baseValue)
        {
            var header = d as DataGridColumnHeader;

            object content = DataGridHelper.GetCoercedTransferPropertyValue(
                header,
                baseValue,
                ContentProperty,
                header.Column,
                DataGridColumn.HeaderProperty);

            // if content is a WPF element with a logical parent, disconnect it now
            // so that it can be connected to the DGColumnHeader.   This happens during
            // a theme change
            FrameworkObject fo = new FrameworkObject(content as DependencyObject);
            if (fo.Parent != null && fo.Parent != header)
            {
                fo.ChangeLogicalParent(null);
            }

            return content;
        }

        /// <summary>
        ///     Coerces the ContentTemplate property based on the templates defined on the Column.
        /// </summary>
        private static object OnCoerceContentTemplate(DependencyObject d, object baseValue)
        {
            var columnHeader = d as DataGridColumnHeader;
            return DataGridHelper.GetCoercedTransferPropertyValue(
                columnHeader,
                baseValue,
                ContentTemplateProperty,
                columnHeader.Column,
                DataGridColumn.HeaderTemplateProperty);
        }

        /// <summary>
        ///     Coerces the ContentTemplateSelector property based on the selector defined on the Column.
        /// </summary>
        private static object OnCoerceContentTemplateSelector(DependencyObject d, object baseValue)
        {
            var columnHeader = d as DataGridColumnHeader;
            return DataGridHelper.GetCoercedTransferPropertyValue(
                columnHeader,
                baseValue,
                ContentTemplateSelectorProperty,
                columnHeader.Column,
                DataGridColumn.HeaderTemplateSelectorProperty);
        }

        /// <summary>
        ///     Coerces the ContentStringFormat property based on the templates defined on the Column.
        /// </summary>
        private static object OnCoerceStringFormat(DependencyObject d, object baseValue)
        {
            var columnHeader = d as DataGridColumnHeader;
            return DataGridHelper.GetCoercedTransferPropertyValue(
                columnHeader,
                baseValue,
                ContentStringFormatProperty,
                columnHeader.Column,
                DataGridColumn.HeaderStringFormatProperty);
        }

        /// <summary>
        ///     Coerces the Style property based on the templates defined on the Column or DataGrid.
        /// </summary>
        private static object OnCoerceStyle(DependencyObject d, object baseValue)
        {
            var columnHeader = (DataGridColumnHeader)d;
            DataGridColumn column = columnHeader.Column;
            DataGrid dataGrid = null;

            // Propagate style changes to any filler column headers.
            if (column == null)
            {
                DataGridColumnHeadersPresenter presenter = columnHeader.TemplatedParent as DataGridColumnHeadersPresenter;
                if (presenter != null)
                {
                    dataGrid = presenter.ParentDataGrid;
                }
            }
            else
            {
                dataGrid = column.DataGridOwner;
            }

            return DataGridHelper.GetCoercedTransferPropertyValue(
                columnHeader,
                baseValue,
                StyleProperty,
                column,
                DataGridColumn.HeaderStyleProperty,
                dataGrid,
                DataGrid.ColumnHeaderStyleProperty);
        }

        #endregion

        #region Auto Sort

        /// <summary>
        /// DependencyPropertyKey for CanUserSort property
        /// </summary>
        private static readonly DependencyPropertyKey CanUserSortPropertyKey =
                DependencyProperty.RegisterReadOnly(
                        "CanUserSort",
                        typeof(bool),
                        typeof(DataGridColumnHeader),
                        new FrameworkPropertyMetadata(true, null, new CoerceValueCallback(OnCoerceCanUserSort)));

        /// <summary>
        ///     The DependencyProperty for the CanUserSort property.
        /// </summary>
        public static readonly DependencyProperty CanUserSortProperty = CanUserSortPropertyKey.DependencyProperty;

        /// <summary>
        ///     CanUserSort is the flag which determines if the datagrid can be sorted based on the column of this header
        /// </summary>
        public bool CanUserSort
        {
            get { return (bool)GetValue(CanUserSortProperty); }
        }

        /// <summary>
        /// DependencyPropertyKey for SortDirection property
        /// </summary>
        private static readonly DependencyPropertyKey SortDirectionPropertyKey =
                DependencyProperty.RegisterReadOnly(
                        "SortDirection",
                        typeof(Nullable<ListSortDirection>),
                        typeof(DataGridColumnHeader),
                        new FrameworkPropertyMetadata(null, OnVisualStatePropertyChanged, new CoerceValueCallback(OnCoerceSortDirection)));

        /// <summary>
        ///     The DependencyProperty for the SortDirection property.
        /// </summary>
        public static readonly DependencyProperty SortDirectionProperty = SortDirectionPropertyKey.DependencyProperty;

        /// <summary>
        /// The property for current sort direction of the column of this header
        /// </summary>
        public Nullable<ListSortDirection> SortDirection
        {
            get { return (Nullable<ListSortDirection>)GetValue(SortDirectionProperty); }
        }

        /// <summary>
        /// The override of ButtonBase.OnClick.
        /// Informs the owning datagrid to sort itself after the execution of usual button stuff
        /// </summary>
        protected override void OnClick()
        {
            if (!SuppressClickEvent)
            {
                if (AutomationPeer.ListenerExists(AutomationEvents.InvokePatternOnInvoked))
                {
                    AutomationPeer peer = UIElementAutomationPeer.CreatePeerForElement(this);
                    if (peer != null)
                    {
                        peer.RaiseAutomationEvent(AutomationEvents.InvokePatternOnInvoked);
                    }
                }

                base.OnClick();

                if (Column != null &&
                    Column.DataGridOwner != null)
                {
                    Column.DataGridOwner.PerformSort(Column);
                }
            }
        }

        /// <summary>
        /// Coercion callback for Height property.
        /// </summary>
        private static object OnCoerceHeight(DependencyObject d, object baseValue)
        {
            var columnHeader = (DataGridColumnHeader)d;
            DataGridColumn column = columnHeader.Column;
            DataGrid dataGrid = null;

            // Propagate style changes to any filler column headers.
            if (column == null)
            {
                DataGridColumnHeadersPresenter presenter = columnHeader.TemplatedParent as DataGridColumnHeadersPresenter;
                if (presenter != null)
                {
                    dataGrid = presenter.ParentDataGrid;
                }
            }
            else
            {
                dataGrid = column.DataGridOwner;
            }

            return DataGridHelper.GetCoercedTransferPropertyValue(
                columnHeader,
                baseValue,
                HeightProperty,
                dataGrid,
                DataGrid.ColumnHeaderHeightProperty);
        }

        /// <summary>
        /// Coercion callback for CanUserSort property. Checks for the value of CanUserSort on owning column
        /// and returns accordingly
        /// </summary>
        private static object OnCoerceCanUserSort(DependencyObject d, object baseValue)
        {
            DataGridColumnHeader header = (DataGridColumnHeader)d;
            DataGridColumn column = header.Column;

            if (column != null)
            {
                return column.CanUserSort;
            }

            return baseValue;
        }

        /// <summary>
        /// Coercion callback for SortDirection property
        /// </summary>
        private static object OnCoerceSortDirection(DependencyObject d, object baseValue)
        {
            DataGridColumnHeader header = (DataGridColumnHeader)d;
            DataGridColumn column = header.Column;

            if (column != null)
            {
                return column.SortDirection;
            }

            return baseValue;
        }

        #endregion

        #region Automation

        protected override System.Windows.Automation.Peers.AutomationPeer OnCreateAutomationPeer()
        {
            return new System.Windows.Automation.Peers.DataGridColumnHeaderAutomationPeer(this);
        }

        // Called from DataGridColumnHeaderAutomationPeer
        internal void Invoke()
        {
            this.OnClick();
        }

        #endregion

        #region Frozen Columns

        /// <summary>
        /// DependencyPropertyKey for IsFrozen property
        /// </summary>
        private static readonly DependencyPropertyKey IsFrozenPropertyKey =
                DependencyProperty.RegisterReadOnly(
                        "IsFrozen",
                        typeof(bool),
                        typeof(DataGridColumnHeader),
                        new FrameworkPropertyMetadata(false, null, new CoerceValueCallback(OnCoerceIsFrozen)));

        /// <summary>
        ///     The DependencyProperty for the IsFrozen property.
        /// </summary>
        public static readonly DependencyProperty IsFrozenProperty = IsFrozenPropertyKey.DependencyProperty;

        /// <summary>
        /// The property to determine if the column corresponding to this header is frozen or not
        /// </summary>
        public bool IsFrozen
        {
            get { return (bool)GetValue(IsFrozenProperty); }
        }

        /// <summary>
        /// Coercion callback for IsFrozen property
        /// </summary>
        private static object OnCoerceIsFrozen(DependencyObject d, object baseValue)
        {
            DataGridColumnHeader header = (DataGridColumnHeader)d;
            DataGridColumn column = header.Column;

            if (column != null)
            {
                return column.IsFrozen;
            }

            return baseValue;
        }

        /// <summary>
        /// Coercion call back for clip property which ensures that the header overlapping with frozen
        /// column gets clipped appropriately.
        /// </summary>
        private static object OnCoerceClip(DependencyObject d, object baseValue)
        {
            DataGridColumnHeader header = (DataGridColumnHeader)d;
            Geometry geometry = baseValue as Geometry;
            Geometry frozenGeometry = DataGridHelper.GetFrozenClipForCell(header);
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

        #region Column Reordering

        internal DataGridColumnHeadersPresenter ParentPresenter
        {
            get
            {
                if (_parentPresenter == null)
                {
                    _parentPresenter = ItemsControl.ItemsControlFromItemContainer(this) as DataGridColumnHeadersPresenter;
                }

                return _parentPresenter;
            }
        }

        /// <summary>
        /// Property which determines if click event has to raised or not. Used during column drag drop which could
        /// be mis-interpreted as a click
        /// </summary>
        internal bool SuppressClickEvent
        {
            get
            {
                return _suppressClickEvent;
            }

            set
            {
                _suppressClickEvent = value;
            }
        }

        protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
        {
            base.OnMouseLeftButtonDown(e);

            DataGridColumnHeadersPresenter parentPresenter = ParentPresenter;
            if (parentPresenter != null)
            {
                // If clickmode is hover then during the mouse move the hover events will be sent
                // all the headers in the path. To avoid that we are using a capture
                if (ClickMode == ClickMode.Hover && e.ButtonState == MouseButtonState.Pressed)
                {
                    CaptureMouse();
                }

                parentPresenter.OnHeaderMouseLeftButtonDown(e);
                e.Handled = true;
            }
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);

            DataGridColumnHeadersPresenter parentPresenter = ParentPresenter;
            if (parentPresenter != null)
            {
                parentPresenter.OnHeaderMouseMove(e);
                e.Handled = true;
            }
        }

        protected override void OnMouseLeftButtonUp(MouseButtonEventArgs e)
        {
            base.OnMouseLeftButtonUp(e);

            DataGridColumnHeadersPresenter parentPresenter = ParentPresenter;
            if (parentPresenter != null)
            {
                if (ClickMode == ClickMode.Hover && IsMouseCaptured)
                {
                    ReleaseMouseCapture();
                }

                parentPresenter.OnHeaderMouseLeftButtonUp(e);
                e.Handled = true;
            }
        }

        protected override void OnLostMouseCapture(MouseEventArgs e)
        {
            base.OnLostMouseCapture(e);

            DataGridColumnHeadersPresenter parentPresenter = ParentPresenter;
            if (parentPresenter != null)
            {
                parentPresenter.OnHeaderLostMouseCapture(e);
                e.Handled = true;
            }
        }

        /// <summary>
        ///     Style key for DataGridColumnDropSeparator
        /// </summary>
        public static ComponentResourceKey ColumnHeaderDropSeparatorStyleKey
        {
            get
            {
                return SystemResourceKey.DataGridColumnHeaderColumnHeaderDropSeparatorStyleKey;
            }
        }

        /// <summary>
        ///     Style key for DataGridColumnFloatingHeader
        /// </summary>
        public static ComponentResourceKey ColumnFloatingHeaderStyleKey
        {
            get
            {
                return SystemResourceKey.DataGridColumnHeaderColumnFloatingHeaderStyleKey;
            }
        }

        #endregion

        #region VSM

        internal override void ChangeVisualState(bool useTransitions)
        {
            // Common States
            if (IsPressed)
            {
                VisualStates.GoToState(this, useTransitions, VisualStates.StatePressed, VisualStates.StateMouseOver, VisualStates.StateNormal);
            }
            else if (IsMouseOver)
            {
                VisualStates.GoToState(this, useTransitions, VisualStates.StateMouseOver, VisualStates.StateNormal);
            }
            else
            {
                VisualStateManager.GoToState(this, VisualStates.StateNormal, useTransitions);
            }

            // Sort States
            var sortDirection = SortDirection;
            if (sortDirection != null)
            {
                if (sortDirection == ListSortDirection.Ascending)
                {
                    VisualStates.GoToState(this, useTransitions, VisualStates.StateSortAscending, VisualStates.StateUnsorted);
                }
                if (sortDirection == ListSortDirection.Descending)
                {
                    VisualStates.GoToState(this, useTransitions, VisualStates.StateSortDescending, VisualStates.StateUnsorted);
                }
            }
            else
            {
                VisualStateManager.GoToState(this, VisualStates.StateUnsorted, useTransitions);
            }

            // Don't call base.ChangeVisualState because we dont want to pick up the button's state changes.
            // This is because SL didn't declare several of the states that Button defines, and we need to be
            // compatible with them.
            ChangeValidationVisualState(useTransitions);
        }

        #endregion


        #region Helpers

        DataGridColumn IProvideDataGridColumn.Column
        {
            get
            {
                return _column;
            }
        }

        private Panel ParentPanel
        {
            get
            {
                return VisualParent as Panel;
            }
        }

        /// <summary>
        ///     Used by the resize code -- this is the header that the left gripper should be resizing.
        /// </summary>
        private DataGridColumnHeader PreviousVisibleHeader
        {
            get
            {
                // we need to be able to find the corner header too.
                DataGridColumn column = Column;
                if (column != null)
                {
                    DataGrid dataGridOwner = column.DataGridOwner;
                    if (dataGridOwner != null)
                    {
                        for (int index = DisplayIndex - 1; index >= 0; index--)
                        {
                            if (dataGridOwner.ColumnFromDisplayIndex(index).IsVisible)
                            {
                                return dataGridOwner.ColumnHeaderFromDisplayIndex(index);
                            }
                        }
                    }
                }

                return null;
            }
        }

        #endregion

        #region Data

        private DataGridColumn _column;
        private ContainerTracking<DataGridColumnHeader> _tracker;
        private DataGridColumnHeadersPresenter _parentPresenter;

        private Thumb _leftGripper, _rightGripper;
        private bool _suppressClickEvent;
        private const string LeftHeaderGripperTemplateName = "PART_LeftHeaderGripper";
        private const string RightHeaderGripperTemplateName = "PART_RightHeaderGripper";

        #endregion
    }
}
