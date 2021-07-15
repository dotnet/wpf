// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Reflection;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using MS.Internal;

namespace System.Windows.Controls
{
    /// <summary>
    ///     A base class for specifying the column definitions.
    /// </summary>
    public abstract class DataGridColumn : DependencyObject
    {
        #region Header

        /// <summary>
        ///     An object that represents the header of this column.
        /// </summary>
        public object Header
        {
            get { return (object)GetValue(HeaderProperty); }
            set { SetValue(HeaderProperty, value); }
        }

        /// <summary>
        ///     The DependencyProperty that represents the Header property.
        /// </summary>
        public static readonly DependencyProperty HeaderProperty =
            DependencyProperty.Register("Header", typeof(object), typeof(DataGridColumn), new FrameworkPropertyMetadata(null, new PropertyChangedCallback(OnNotifyColumnHeaderPropertyChanged)));

        /// <summary>
        ///     The Style for the DataGridColumnHeader
        /// </summary>
        public Style HeaderStyle
        {
            get { return (Style)GetValue(HeaderStyleProperty); }
            set { SetValue(HeaderStyleProperty, value); }
        }

        /// <summary>
        ///     The DependencyProperty that represents the HeaderStyle property.
        /// </summary>
        public static readonly DependencyProperty HeaderStyleProperty =
            DependencyProperty.Register("HeaderStyle", typeof(Style), typeof(DataGridColumn), new FrameworkPropertyMetadata(null, OnNotifyColumnHeaderPropertyChanged, OnCoerceHeaderStyle));

        private static object OnCoerceHeaderStyle(DependencyObject d, object baseValue)
        {
            var column = d as DataGridColumn;
            return DataGridHelper.GetCoercedTransferPropertyValue(
                column,
                baseValue,
                HeaderStyleProperty,
                column.DataGridOwner,
                DataGrid.ColumnHeaderStyleProperty);
        }

        /// <summary>
        ///     The string format to apply to the header.
        /// </summary>
        public string HeaderStringFormat
        {
            get { return (string)GetValue(HeaderStringFormatProperty); }
            set { SetValue(HeaderStringFormatProperty, value); }
        }

        /// <summary>
        ///     The DependencyProperty that represents the HeaderStringFormat property.
        /// </summary>
        public static readonly DependencyProperty HeaderStringFormatProperty =
            DependencyProperty.Register("HeaderStringFormat", typeof(string), typeof(DataGridColumn), new FrameworkPropertyMetadata(null, OnNotifyColumnHeaderPropertyChanged));

        /// <summary>
        ///     The template that defines the visual representation of the header.
        /// </summary>
        public DataTemplate HeaderTemplate
        {
            get { return (DataTemplate)GetValue(HeaderTemplateProperty); }
            set { SetValue(HeaderTemplateProperty, value); }
        }

        /// <summary>
        ///     The DependencyProperty that represents the HeaderTemplate property.
        /// </summary>
        public static readonly DependencyProperty HeaderTemplateProperty =
            DependencyProperty.Register("HeaderTemplate", typeof(DataTemplate), typeof(DataGridColumn), new FrameworkPropertyMetadata(null, OnNotifyColumnHeaderPropertyChanged));

        /// <summary>
        ///     DataTemplateSelector that selects which template to use for the Column Header
        /// </summary>
        public DataTemplateSelector HeaderTemplateSelector
        {
            get { return (DataTemplateSelector)GetValue(HeaderTemplateSelectorProperty); }
            set { SetValue(HeaderTemplateSelectorProperty, value); }
        }

        /// <summary>
        ///     The DependencyProperty that represents the HeaderTemplateSelector property.
        /// </summary>
        public static readonly DependencyProperty HeaderTemplateSelectorProperty =
            DependencyProperty.Register("HeaderTemplateSelector", typeof(DataTemplateSelector), typeof(DataGridColumn), new FrameworkPropertyMetadata(null, OnNotifyColumnHeaderPropertyChanged));

        #endregion

        #region Cell Container

        /// <summary>
        ///     A style to apply to the container of cells in this column.
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
            DependencyProperty.Register("CellStyle", typeof(Style), typeof(DataGridColumn), new FrameworkPropertyMetadata(null, OnNotifyCellPropertyChanged, OnCoerceCellStyle));

        private static object OnCoerceCellStyle(DependencyObject d, object baseValue)
        {
            var column = d as DataGridColumn;
            return DataGridHelper.GetCoercedTransferPropertyValue(
                column,
                baseValue,
                CellStyleProperty,
                column.DataGridOwner,
                DataGrid.CellStyleProperty);
        }

        /// <summary>
        ///     Whether cells in this column can enter edit mode.
        /// </summary>
        public bool IsReadOnly
        {
            get { return (bool)GetValue(IsReadOnlyProperty); }
            set { SetValue(IsReadOnlyProperty, value); }
        }

        /// <summary>
        ///     The DependencyProperty that represents the IsReadOnly property.
        /// </summary>
        public static readonly DependencyProperty IsReadOnlyProperty =
            DependencyProperty.Register("IsReadOnly", typeof(bool), typeof(DataGridColumn), new FrameworkPropertyMetadata(false, OnNotifyCellPropertyChanged, OnCoerceIsReadOnly));

        private static object OnCoerceIsReadOnly(DependencyObject d, object baseValue)
        {
            var column = d as DataGridColumn;
            return column.OnCoerceIsReadOnly((bool)baseValue);
        }

        /// <summary>
        ///     Subtypes can override this to force IsReadOnly to be coerced to true.
        /// </summary>
        protected virtual bool OnCoerceIsReadOnly(bool baseValue)
        {
            return (bool)DataGridHelper.GetCoercedTransferPropertyValue(
                this,
                baseValue,
                IsReadOnlyProperty,
                DataGridOwner,
                DataGrid.IsReadOnlyProperty);
        }

        #endregion

        #region Width

        /// <summary>
        ///     Specifies the width of the header and cells within this column.
        /// </summary>
        public DataGridLength Width
        {
            get { return (DataGridLength)GetValue(WidthProperty); }
            set { SetValue(WidthProperty, value); }
        }

        /// <summary>
        ///     The DependencyProperty that represents the Width property.
        /// </summary>
        public static readonly DependencyProperty WidthProperty =
            DependencyProperty.Register(
                "Width",
                typeof(DataGridLength),
                typeof(DataGridColumn),
                new FrameworkPropertyMetadata(DataGridLength.Auto, new PropertyChangedCallback(OnWidthPropertyChanged), new CoerceValueCallback(OnCoerceWidth)));

        /// <summary>
        /// Internal method which sets the column's width
        /// without actual redistribution of widths among other
        /// columns
        /// </summary>
        /// <param name="width"></param>
        internal void SetWidthInternal(DataGridLength width)
        {
            bool originalValue = _ignoreRedistributionOnWidthChange;
            _ignoreRedistributionOnWidthChange = true;
            try
            {
                Width = width;
            }
            finally
            {
                _ignoreRedistributionOnWidthChange = originalValue;
            }
        }

        /// <summary>
        /// Property changed call back for Width property which notification propagation
        /// and does the redistribution of widths among other columns if needed
        /// </summary>
        /// <param name="d"></param>
        /// <param name="e"></param>
        private static void OnWidthPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            DataGridColumn column = (DataGridColumn)d;
            DataGridLength oldWidth = (DataGridLength)e.OldValue;
            DataGridLength newWidth = (DataGridLength)e.NewValue;
            DataGrid dataGrid = column.DataGridOwner;

            if (dataGrid != null &&
                !DoubleUtil.AreClose(oldWidth.DisplayValue, newWidth.DisplayValue))
            {
                dataGrid.InternalColumns.InvalidateAverageColumnWidth();
            }

            if (column._processingWidthChange)
            {
                column.CoerceValue(ActualWidthProperty);
                return;
            }

            column._processingWidthChange = true;
            if (oldWidth.IsStar != newWidth.IsStar)
            {
                column.CoerceValue(MaxWidthProperty);
            }

            try
            {
                if (dataGrid != null && (newWidth.IsStar ^ oldWidth.IsStar))
                {
                    dataGrid.InternalColumns.InvalidateHasVisibleStarColumns();
                }

                column.NotifyPropertyChanged(
                    d,
                    e,
                    DataGridNotificationTarget.ColumnCollection |
                    DataGridNotificationTarget.Columns |
                    DataGridNotificationTarget.Cells |
                    DataGridNotificationTarget.ColumnHeaders |
                    DataGridNotificationTarget.CellsPresenter |
                    DataGridNotificationTarget.ColumnHeadersPresenter |
                    DataGridNotificationTarget.DataGrid);

                if (dataGrid != null)
                {
                    if (!column._ignoreRedistributionOnWidthChange && column.IsVisible)
                    {
                        if (!newWidth.IsStar && !newWidth.IsAbsolute)
                        {
                            DataGridLength changedWidth = column.Width;
                            double displayValue = DataGridHelper.CoerceToMinMax(changedWidth.DesiredValue, column.MinWidth, column.MaxWidth);
                            column.SetWidthInternal(new DataGridLength(changedWidth.Value, changedWidth.UnitType, changedWidth.DesiredValue, displayValue));
                        }

                        dataGrid.InternalColumns.RedistributeColumnWidthsOnWidthChangeOfColumn(column, (DataGridLength)e.OldValue);
                    }
                }
            }
            finally
            {
                column._processingWidthChange = false;
            }
        }

        /// <summary>
        ///     Specifies the minimum width of the header and cells within this column.
        /// </summary>
        public double MinWidth
        {
            get { return (double)GetValue(MinWidthProperty); }
            set { SetValue(MinWidthProperty, value); }
        }

        /// <summary>
        ///     The DependencyProperty that represents the MinWidth property.
        /// </summary>
        public static readonly DependencyProperty MinWidthProperty =
            DependencyProperty.Register(
                "MinWidth",
                typeof(double),
                typeof(DataGridColumn),
                new FrameworkPropertyMetadata(20d, new PropertyChangedCallback(OnMinWidthPropertyChanged), new CoerceValueCallback(OnCoerceMinWidth)),
                new ValidateValueCallback(ValidateMinWidth));

        /// <summary>
        /// Property changed call back for MinWidth property which notification propagation
        /// and does the redistribution of widths among other columns if needed
        /// </summary>
        /// <param name="d"></param>
        /// <param name="e"></param>
        private static void OnMinWidthPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            DataGridColumn column = (DataGridColumn)d;
            DataGrid dataGrid = column.DataGridOwner;

            column.NotifyPropertyChanged(d, e, DataGridNotificationTarget.Columns);

            if (dataGrid != null && column.IsVisible)
            {
                dataGrid.InternalColumns.RedistributeColumnWidthsOnMinWidthChangeOfColumn(column, (double)e.OldValue);
            }
        }

        /// <summary>
        ///     Specifies the maximum width of the header and cells within this column.
        /// </summary>
        public double MaxWidth
        {
            get { return (double)GetValue(MaxWidthProperty); }
            set { SetValue(MaxWidthProperty, value); }
        }

        /// <summary>
        ///     The DependencyProperty that represents the MaxWidth property.
        /// </summary>
        public static readonly DependencyProperty MaxWidthProperty =
            DependencyProperty.Register(
                "MaxWidth",
                typeof(double),
                typeof(DataGridColumn),
                new FrameworkPropertyMetadata(double.PositiveInfinity, new PropertyChangedCallback(OnMaxWidthPropertyChanged), new CoerceValueCallback(OnCoerceMaxWidth)),
                new ValidateValueCallback(ValidateMaxWidth));

        /// <summary>
        /// Property changed call back for MaxWidth property which notification propagation
        /// and does the redistribution of widths among other columns if needed
        /// </summary>
        /// <param name="d"></param>
        /// <param name="e"></param>
        private static void OnMaxWidthPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            DataGridColumn column = (DataGridColumn)d;
            DataGrid dataGrid = column.DataGridOwner;

            column.NotifyPropertyChanged(d, e, DataGridNotificationTarget.Columns);

            if (dataGrid != null && column.IsVisible)
            {
                dataGrid.InternalColumns.RedistributeColumnWidthsOnMaxWidthChangeOfColumn(column, (double)e.OldValue);
            }
        }

        /// <summary>
        ///     Helper method which coerces the DesiredValue or DisplayValue
        ///     of the width.
        /// </summary>
        private static double CoerceDesiredOrDisplayWidthValue(double widthValue, double memberValue, DataGridLengthUnitType type)
        {
            if (DoubleUtil.IsNaN(memberValue))
            {
                if (type == DataGridLengthUnitType.Pixel)
                {
                    memberValue = widthValue;
                }
                else if (type == DataGridLengthUnitType.Auto ||
                    type == DataGridLengthUnitType.SizeToCells ||
                    type == DataGridLengthUnitType.SizeToHeader)
                {
                    memberValue = 0d;
                }
            }
            return memberValue;
        }

        /// <summary>
        ///     Coerces the WidthProperty based on the DataGrid transferred property rules
        /// </summary>
        private static object OnCoerceWidth(DependencyObject d, object baseValue)
        {
            var column = d as DataGridColumn;
            DataGridLength width = (DataGridLength)DataGridHelper.GetCoercedTransferPropertyValue(
                column,
                baseValue,
                WidthProperty,
                column.DataGridOwner,
                DataGrid.ColumnWidthProperty);

            double newDesiredValue = CoerceDesiredOrDisplayWidthValue(width.Value, width.DesiredValue, width.UnitType);
            double newDisplayValue = CoerceDesiredOrDisplayWidthValue(width.Value, width.DisplayValue, width.UnitType);
            newDisplayValue = (DoubleUtil.IsNaN(newDisplayValue) ? newDisplayValue : DataGridHelper.CoerceToMinMax(newDisplayValue, column.MinWidth, column.MaxWidth));
            if (DoubleUtil.IsNaN(newDisplayValue) || DoubleUtil.AreClose(newDisplayValue, width.DisplayValue))
            {
                return width;
            }

            return new DataGridLength(
                width.Value,
                width.UnitType,
                newDesiredValue,
                newDisplayValue);
        }

        /// <summary>
        ///     Coerces the MinWidthProperty based on the DataGrid transferred property rules
        /// </summary>
        private static object OnCoerceMinWidth(DependencyObject d, object baseValue)
        {
            var column = d as DataGridColumn;
            return DataGridHelper.GetCoercedTransferPropertyValue(
                column,
                baseValue,
                MinWidthProperty,
                column.DataGridOwner,
                DataGrid.MinColumnWidthProperty);
        }

        /// <summary>
        ///     Coerces the MaxWidthProperty based on the DataGrid transferred property rules
        /// </summary>
        private static object OnCoerceMaxWidth(DependencyObject d, object baseValue)
        {
            var column = d as DataGridColumn;
            double transferValue =  (double)DataGridHelper.GetCoercedTransferPropertyValue(
                column,
                baseValue,
                MaxWidthProperty,
                column.DataGridOwner,
                DataGrid.MaxColumnWidthProperty);

            // Coerce the Max Width to 10k pixels if infinity on a star column
            if (double.IsPositiveInfinity(transferValue) &&
                column.Width.IsStar)
            {
                return _starMaxWidth;
            }

            return transferValue;
        }

        /// <summary>
        ///     Validates that the minimum width is an acceptable value
        /// </summary>
        private static bool ValidateMinWidth(object v)
        {
            double value = (double)v;
            return !(value < 0d || DoubleUtil.IsNaN(value) || Double.IsPositiveInfinity(value));
        }

        /// <summary>
        ///     Validates that the maximum width is an acceptable value
        /// </summary>
        private static bool ValidateMaxWidth(object v)
        {
            double value = (double)v;
            return !(value < 0d || DoubleUtil.IsNaN(value));
        }

        /// <summary>
        ///      This is the width that cells and headers should use in Arrange.
        /// </summary>
        public double ActualWidth
        {
            get { return (double)GetValue(ActualWidthProperty); }
            private set { SetValue(ActualWidthPropertyKey, value); }
        }

        private static readonly DependencyPropertyKey ActualWidthPropertyKey =
            DependencyProperty.RegisterReadOnly("ActualWidth", typeof(double), typeof(DataGridColumn), new FrameworkPropertyMetadata(0.0, null, new CoerceValueCallback(OnCoerceActualWidth)));

        public static readonly DependencyProperty ActualWidthProperty = ActualWidthPropertyKey.DependencyProperty;

        private static object OnCoerceActualWidth(DependencyObject d, object baseValue)
        {
            DataGridColumn column = ((DataGridColumn)d);
            double actualWidth = (double)baseValue;
            double minWidth = column.MinWidth;
            double maxWidth = column.MaxWidth;

            // If the width is an absolute pixel value, then ActualWidth should be that value
            DataGridLength width = column.Width;
            if (width.IsAbsolute)
            {
                actualWidth = width.DisplayValue;
            }

            if (actualWidth < minWidth)
            {
                actualWidth = minWidth;
            }
            else if (actualWidth > maxWidth)
            {
                actualWidth = maxWidth;
            }

            return actualWidth;
        }

        /// <summary>
        ///     Retrieve the proper measure constraint for cells.
        /// </summary>
        /// <param name="isHeader">Whether a header constraint or a normal cell constraint is requested.</param>
        /// <returns>The value to use as the width when creating a measure constraint.</returns>
        internal double GetConstraintWidth(bool isHeader)
        {
            DataGridLength width = Width;
            if (!DoubleUtil.IsNaN(width.DisplayValue))
            {
                return width.DisplayValue;
            }

            if (width.IsAbsolute ||
                width.IsStar ||
                (width.IsSizeToCells && isHeader) ||
                (width.IsSizeToHeader && !isHeader))
            {
                // In these cases, the cell's desired size does not matter.
                // Use the column's current width as the constraint.
                return ActualWidth;
            }
            else
            {
                // The element gets to size to content.
                return Double.PositiveInfinity;
            }
        }

        /// <summary>
        ///     Notifies the column of a cell's desired width.
        ///     Updates the actual width if necessary
        /// </summary>
        /// <param name="isHeader">Whether the cell is a header or not.</param>
        /// <param name="pixelWidth">The desired size of the cell.</param>
        internal void UpdateDesiredWidthForAutoColumn(bool isHeader, double pixelWidth)
        {
            DataGridLength width = Width;
            double minWidth = MinWidth;
            double maxWidth = MaxWidth;
            double displayWidth = DataGridHelper.CoerceToMinMax(pixelWidth, minWidth, maxWidth);

            if (width.IsAuto ||
                (width.IsSizeToCells && !isHeader) ||
                (width.IsSizeToHeader && isHeader))
            {
                if (DoubleUtil.IsNaN(width.DesiredValue) ||
                    DoubleUtil.LessThan(width.DesiredValue, pixelWidth))
                {
                    if (DoubleUtil.IsNaN(width.DisplayValue))
                    {
                        SetWidthInternal(new DataGridLength(width.Value, width.UnitType, pixelWidth, displayWidth));
                    }
                    else
                    {
                        double originalDesiredValue = DataGridHelper.CoerceToMinMax(width.DesiredValue, minWidth, maxWidth);
                        SetWidthInternal(new DataGridLength(width.Value, width.UnitType, pixelWidth, width.DisplayValue));
                        if (DoubleUtil.AreClose(originalDesiredValue, width.DisplayValue))
                        {
                            DataGridOwner.InternalColumns.RecomputeColumnWidthsOnColumnResize(this, pixelWidth - width.DisplayValue, true);
                        }
                    }

                    width = Width;
                }

                if (DoubleUtil.IsNaN(width.DisplayValue))
                {
                    if (ActualWidth < displayWidth)
                    {
                        ActualWidth = displayWidth;
                    }
                }
                else if (!DoubleUtil.AreClose(ActualWidth, width.DisplayValue))
                {
                    ActualWidth = width.DisplayValue;
                }
            }
        }

        /// <summary>
        ///     Notifies the column that Width="*" columns have a new actual width.
        /// </summary>
        internal void UpdateWidthForStarColumn(double displayWidth, double desiredWidth, double starValue)
        {
            Debug.Assert(Width.IsStar);
            DataGridLength width = Width;

            if (!DoubleUtil.AreClose(displayWidth, width.DisplayValue) ||
                !DoubleUtil.AreClose(desiredWidth, width.DesiredValue) ||
                !DoubleUtil.AreClose(width.Value, starValue))
            {
                SetWidthInternal(new DataGridLength(starValue, width.UnitType, desiredWidth, displayWidth));
                ActualWidth = displayWidth;
            }
        }

        #endregion

        #region Visual Tree Generation

        /// <summary>
        ///     Retrieves the visual tree that was generated for a particular row and column.
        /// </summary>
        /// <param name="dataItem">The row that corresponds to the desired cell.</param>
        /// <returns>The element if found, null otherwise.</returns>
        public FrameworkElement GetCellContent(object dataItem)
        {
            if (dataItem == null)
            {
                throw new ArgumentNullException("dataItem");
            }

            if (_dataGridOwner != null)
            {
                DataGridRow row = _dataGridOwner.ItemContainerGenerator.ContainerFromItem(dataItem) as DataGridRow;
                if (row != null)
                {
                    return GetCellContent(row);
                }
            }

            return null;
        }

        /// <summary>
        ///     Retrieves the visual tree that was generated for a particular row and column.
        /// </summary>
        /// <param name="dataGridRow">The row that corresponds to the desired cell.</param>
        /// <returns>The element if found, null otherwise.</returns>
        public FrameworkElement GetCellContent(DataGridRow dataGridRow)
        {
            if (dataGridRow == null)
            {
                throw new ArgumentNullException("dataGridRow");
            }

            if (_dataGridOwner != null)
            {
                int columnIndex = _dataGridOwner.Columns.IndexOf(this);
                if (columnIndex >= 0)
                {
                    DataGridCell cell = dataGridRow.TryGetCell(columnIndex);
                    if (cell != null)
                    {
                        return cell.Content as FrameworkElement;
                    }
                }
            }

            return null;
        }

        /// <summary>
        ///     Creates the visual tree that will become the content of a cell.
        /// </summary>
        /// <param name="isEditing">Whether the editing version is being requested.</param>
        /// <param name="dataItem">The data item for the cell.</param>
        /// <param name="cell">The cell container that will receive the tree.</param>
        internal FrameworkElement BuildVisualTree(bool isEditing, object dataItem, DataGridCell cell)
        {
            if (isEditing)
            {
                return GenerateEditingElement(cell, dataItem);
            }
            else
            {
                return GenerateElement(cell, dataItem);
            }
        }

        /// <summary>
        ///     Creates the visual tree that will become the content of a cell.
        /// </summary>
        protected abstract FrameworkElement GenerateElement(DataGridCell cell, object dataItem);

        /// <summary>
        ///     Creates the visual tree that will become the content of a cell.
        /// </summary>
        protected abstract FrameworkElement GenerateEditingElement(DataGridCell cell, object dataItem);

        #endregion

        #region Editing

        /// <summary>
        ///     Called when a cell has just switched to edit mode.
        /// </summary>
        /// <param name="editingElement">A reference to element returned by GenerateEditingElement.</param>
        /// <returns>The unedited value of the cell.</returns>
        protected virtual object PrepareCellForEdit(FrameworkElement editingElement, RoutedEventArgs editingEventArgs)
        {
            return null;
        }

        /// <summary>
        ///     Called when a cell's value is to be restored to its original value,
        ///     just before it exits edit mode.
        /// </summary>
        /// <param name="editingElement">A reference to element returned by GenerateEditingElement.</param>
        /// <param name="uneditedValue">The original, unedited value of the cell.</param>
        protected virtual void CancelCellEdit(FrameworkElement editingElement, object uneditedValue)
        {
            DataGridHelper.UpdateTarget(editingElement);
        }

        /// <summary>
        ///     Called when a cell's value is to be committed, just before it exits edit mode.
        /// </summary>
        /// <param name="editingElement">A reference to element returned by GenerateEditingElement.</param>
        /// <returns>false if there is a validation error. true otherwise.</returns>
        protected virtual bool CommitCellEdit(FrameworkElement editingElement)
        {
            return DataGridHelper.ValidateWithoutUpdate(editingElement);
        }

        internal void BeginEdit(FrameworkElement editingElement, RoutedEventArgs e)
        {
            // This call is to ensure that the tree and its bindings have resolved
            // before we proceed to code that relies on the tree being ready.
            if (editingElement != null)
            {
                editingElement.UpdateLayout();

                object originalValue = PrepareCellForEdit(editingElement, e);
                SetOriginalValue(editingElement, originalValue);
            }
        }

        internal void CancelEdit(FrameworkElement editingElement)
        {
            if (editingElement != null)
            {
                CancelCellEdit(editingElement, GetOriginalValue(editingElement));
                ClearOriginalValue(editingElement);
            }
        }

        internal bool CommitEdit(FrameworkElement editingElement)
        {
            if (editingElement != null)
            {
                if (CommitCellEdit(editingElement))
                {
                    // Validation passed
                    ClearOriginalValue(editingElement);
                    return true;
                }
                else
                {
                    // Validation failed. This cell will remain in edit mode.
                    return false;
                }
            }

            return true;
        }

        private static object GetOriginalValue(DependencyObject obj)
        {
            return (object)obj.GetValue(OriginalValueProperty);
        }

        private static void SetOriginalValue(DependencyObject obj, object value)
        {
            obj.SetValue(OriginalValueProperty, value);
        }

        private static void ClearOriginalValue(DependencyObject obj)
        {
            obj.ClearValue(OriginalValueProperty);
        }

        private static readonly DependencyProperty OriginalValueProperty =
            DependencyProperty.RegisterAttached("OriginalValue", typeof(object), typeof(DataGridColumn), new FrameworkPropertyMetadata(null));

        #endregion

        #region Owner Communication

        /// <summary>
        ///     Notifies the DataGrid and the Cells about property changes.
        /// </summary>
        internal static void OnNotifyCellPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((DataGridColumn)d).NotifyPropertyChanged(d, e, DataGridNotificationTarget.Columns | DataGridNotificationTarget.Cells);
        }

        /// <summary>
        ///     Notifies the DataGrid and the Column Headers about property changes.
        /// </summary>
        private static void OnNotifyColumnHeaderPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((DataGridColumn)d).NotifyPropertyChanged(d, e, DataGridNotificationTarget.Columns | DataGridNotificationTarget.ColumnHeaders);
        }

        /// <summary>
        ///     Notifies parts that respond to changes in the column.
        /// </summary>
        private static void OnNotifyColumnPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((DataGridColumn)d).NotifyPropertyChanged(d, e, DataGridNotificationTarget.Columns);
        }

        /// <summary>
        ///   General notification for DependencyProperty changes from the grid and/or column.
        /// </summary>
        internal void NotifyPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e, DataGridNotificationTarget target)
        {
            if (DataGridHelper.ShouldNotifyColumns(target))
            {
                // Remove columns target since we're handling it.  If we're targeting multiple targets it may also need to get
                // sent to the DataGrid.
                target &= ~DataGridNotificationTarget.Columns;

                if (e.Property == DataGrid.MaxColumnWidthProperty || e.Property == MaxWidthProperty)
                {
                    DataGridHelper.TransferProperty(this, MaxWidthProperty);
                }
                else if (e.Property == DataGrid.MinColumnWidthProperty || e.Property == MinWidthProperty)
                {
                    DataGridHelper.TransferProperty(this, MinWidthProperty);
                }
                else if (e.Property == DataGrid.ColumnWidthProperty || e.Property == WidthProperty)
                {
                    DataGridHelper.TransferProperty(this, WidthProperty);
                }
                else if (e.Property == DataGrid.ColumnHeaderStyleProperty || e.Property == HeaderStyleProperty)
                {
                    DataGridHelper.TransferProperty(this, HeaderStyleProperty);
                }
                else if (e.Property == DataGrid.CellStyleProperty || e.Property == CellStyleProperty)
                {
                    DataGridHelper.TransferProperty(this, CellStyleProperty);
                }
                else if (e.Property == DataGrid.IsReadOnlyProperty || e.Property == IsReadOnlyProperty)
                {
                    DataGridHelper.TransferProperty(this, IsReadOnlyProperty);
                }
                else if (e.Property == DataGrid.DragIndicatorStyleProperty || e.Property == DragIndicatorStyleProperty)
                {
                    DataGridHelper.TransferProperty(this, DragIndicatorStyleProperty);
                }
                else if (e.Property == DisplayIndexProperty)
                {
                    CoerceValue(IsFrozenProperty);
                }
                else if (e.Property == DataGrid.CanUserSortColumnsProperty)
                {
                    DataGridHelper.TransferProperty(this, CanUserSortProperty);
                }
                else if (e.Property == DataGrid.CanUserResizeColumnsProperty || e.Property == CanUserResizeProperty)
                {
                    DataGridHelper.TransferProperty(this, CanUserResizeProperty);
                }
                else if (e.Property == DataGrid.CanUserReorderColumnsProperty || e.Property == CanUserReorderProperty)
                {
                    DataGridHelper.TransferProperty(this, CanUserReorderProperty);
                }

                if (e.Property == WidthProperty || e.Property == MinWidthProperty || e.Property == MaxWidthProperty)
                {
                    CoerceValue(ActualWidthProperty);
                }
            }

            if (target != DataGridNotificationTarget.None)
            {
                // Everything else gets sent to the DataGrid so it can propogate back down
                // to the targets that need notification.
                DataGridColumn column = (DataGridColumn)d;
                DataGrid dataGridOwner = column.DataGridOwner;
                if (dataGridOwner != null)
                {
                    dataGridOwner.NotifyPropertyChanged(d, e, target);
                }
            }
        }

        /// <summary>
        /// Method which propogates the property changed notification to datagrid
        /// </summary>
        /// <param name="propertyName"></param>
        protected void NotifyPropertyChanged(string propertyName)
        {
            if (DataGridOwner != null)
            {
                DataGridOwner.NotifyPropertyChanged(this, propertyName, new DependencyPropertyChangedEventArgs(), DataGridNotificationTarget.RefreshCellContent);
            }
        }

        /// <summary>
        /// Method used as property changed callback for properties which need RefreshCellContent to be called
        /// </summary>
        /// <param name="d"></param>
        /// <param name="e"></param>
        internal static void NotifyPropertyChangeForRefreshContent(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            Debug.Assert(d is DataGridColumn, "d should be a DataGridColumn");

            ((DataGridColumn)d).NotifyPropertyChanged(e.Property.Name);
        }

        /// <summary>
        /// Method which updates the cell for property changes
        /// </summary>
        /// <param name="element"></param>
        /// <param name="propertyName"></param>
        protected internal virtual void RefreshCellContent(FrameworkElement element, string propertyName)
        {
        }

        /// <summary>
        ///     Ensures that any properties that may be influenced by a change to the DataGrid are syncronized.
        /// </summary>
        internal void SyncProperties()
        {
            DataGridHelper.TransferProperty(this, MinWidthProperty);
            DataGridHelper.TransferProperty(this, MaxWidthProperty);
            DataGridHelper.TransferProperty(this, WidthProperty);
            DataGridHelper.TransferProperty(this, HeaderStyleProperty);
            DataGridHelper.TransferProperty(this, CellStyleProperty);
            DataGridHelper.TransferProperty(this, IsReadOnlyProperty);
            DataGridHelper.TransferProperty(this, DragIndicatorStyleProperty);
            DataGridHelper.TransferProperty(this, CanUserSortProperty);
            DataGridHelper.TransferProperty(this, CanUserReorderProperty);
            DataGridHelper.TransferProperty(this, CanUserResizeProperty);
        }

        /// <summary>
        ///     The owning DataGrid control.
        /// </summary>
        protected internal DataGrid DataGridOwner
        {
            get { return _dataGridOwner; }
            internal set { _dataGridOwner = value; }
        }

        #endregion

        #region Display Index

        /// <summary>
        ///     Specifies the display index of this column.
        /// </summary>
        /// <remarks>
        ///     A lower display index means a column will appear first (to the left) of columns with a higher display index.
        ///     Allowable values are from 0 to num columns - 1. (-1 is legal only as the default value and is modified to something else
        ///     when the column is added to a DataGrid's column collection). DataGrid enforces that no two columns have the same display index;
        ///     changing the display index of a column will cause the index of other columns to adjust as well.
        /// </remarks>
        public int DisplayIndex
        {
            get { return (int)GetValue(DisplayIndexProperty); }
            set { SetValue(DisplayIndexProperty, value); }
        }

        /// <summary>
        ///     The DependencyProperty that represents the Width property.
        /// </summary>
        public static readonly DependencyProperty DisplayIndexProperty =
            DependencyProperty.Register(
                "DisplayIndex",
                typeof(int),
                typeof(DataGridColumn),
                new FrameworkPropertyMetadata(-1, new PropertyChangedCallback(DisplayIndexChanged), new CoerceValueCallback(OnCoerceDisplayIndex)));

        /// <summary>
        ///     We use the coersion callback to validate that the DisplayIndex of a column is between 0 and DataGrid.Columns.Count
        ///     The default value is -1; this value is only legal as the default or when the Column is not attached to a DataGrid.
        /// </summary>
        private static object OnCoerceDisplayIndex(DependencyObject d, object baseValue)
        {
            DataGridColumn column = (DataGridColumn)d;

            if (column.DataGridOwner != null)
            {
                column.DataGridOwner.ValidateDisplayIndex(column, (int)baseValue);
            }

            return baseValue;
        }

        /// <summary>
        ///     Notifies the DataGrid that the display index for this column changed.
        /// </summary>
        private static void DisplayIndexChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            // Cells and ColumnHeaders invalidate Arrange; ColumnCollection handles modifying the DisplayIndex of other columns.
            ((DataGridColumn)d).NotifyPropertyChanged(
                d,
                e,
                DataGridNotificationTarget.DataGrid |
                DataGridNotificationTarget.Columns |
                DataGridNotificationTarget.ColumnCollection |
                DataGridNotificationTarget.Cells |
                DataGridNotificationTarget.ColumnHeaders |
                DataGridNotificationTarget.CellsPresenter |
                DataGridNotificationTarget.ColumnHeadersPresenter);
        }

        #endregion

        #region Auto Sorting

        /// <summary>
        /// Dependency property for SortMemberPath
        /// </summary>
        public static readonly DependencyProperty SortMemberPathProperty =
            DependencyProperty.Register(
                "SortMemberPath",
                typeof(string),
                typeof(DataGridColumn),
                new FrameworkPropertyMetadata(String.Empty));

        /// <summary>
        /// The property which the determines the member to be sorted upon when sorted on this column
        /// </summary>
        public string SortMemberPath
        {
            get { return (string)GetValue(SortMemberPathProperty); }
            set { SetValue(SortMemberPathProperty, value); }
        }

        /// <summary>
        /// Dependecy property for CanUserSort
        /// </summary>
        public static readonly DependencyProperty CanUserSortProperty =
            DependencyProperty.Register(
                "CanUserSort",
                typeof(bool),
                typeof(DataGridColumn),
                new FrameworkPropertyMetadata(true, new PropertyChangedCallback(OnCanUserSortPropertyChanged), new CoerceValueCallback(OnCoerceCanUserSort)));

        /// <summary>
        /// The property which determines whether the datagrid can be sorted upon this column or not
        /// </summary>
        public bool CanUserSort
        {
            get { return (bool)GetValue(CanUserSortProperty); }
            set { SetValue(CanUserSortProperty, value); }
        }

        /// <summary>
        /// The Coercion callback for CanUserSort property. Checks if datagrid.Items can sort and
        /// returns the value accordingly.
        /// </summary>
        /// <param name="d"></param>
        /// <param name="baseValue"></param>
        /// <returns></returns>
        internal static object OnCoerceCanUserSort(DependencyObject d, object baseValue)
        {
            var column = d as DataGridColumn;

            bool basePropertyHasModifiers;
            BaseValueSourceInternal baseValueSource = column.GetValueSource(CanUserSortProperty, /*metadata*/ null, out basePropertyHasModifiers);

            if (column.DataGridOwner != null)
            {
                bool parentPropertyHasModifiers;
                BaseValueSourceInternal parentValueSource = column.DataGridOwner.GetValueSource(DataGrid.CanUserSortColumnsProperty, /*metadata*/ null, out parentPropertyHasModifiers);
                if (parentValueSource == baseValueSource && !basePropertyHasModifiers && parentPropertyHasModifiers)
                {
                    return column.DataGridOwner.GetValue(DataGrid.CanUserSortColumnsProperty);
                }
            }

            return DataGridHelper.GetCoercedTransferPropertyValue(
                column,
                baseValue,
                CanUserSortProperty,
                column.DataGridOwner,
                DataGrid.CanUserSortColumnsProperty);
        }

        /// <summary>
        /// Property changed callback for CanUserSort property
        /// </summary>
        /// <param name="d"></param>
        /// <param name="e"></param>
        private static void OnCanUserSortPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            // To prevent re-entrancy
            if (!DataGridHelper.IsPropertyTransferEnabled(d, CanUserSortProperty))
            {
                // Coerce value from parent DataGrid
                DataGridHelper.TransferProperty(d, CanUserSortProperty);
            }
            ((DataGridColumn)d).NotifyPropertyChanged(d, e, DataGridNotificationTarget.ColumnHeaders);
        }


        /// <summary>
        /// Dependency property for SortDirection
        /// </summary>
        public static readonly DependencyProperty SortDirectionProperty =
            DependencyProperty.Register(
                "SortDirection",
                typeof(Nullable<ListSortDirection>),
                typeof(DataGridColumn),
                new FrameworkPropertyMetadata(null, new PropertyChangedCallback(OnNotifySortPropertyChanged)));

        /// <summary>
        /// The property for current sort direction of the column
        /// </summary>
        public Nullable<ListSortDirection> SortDirection
        {
            get { return (Nullable<ListSortDirection>)GetValue(SortDirectionProperty); }
            set { SetValue(SortDirectionProperty, value); }
        }

        /// <summary>
        /// Property changed callback for SortMemberPath and SortDirection properties
        /// </summary>
        /// <param name="d"></param>
        /// <param name="e"></param>
        private static void OnNotifySortPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((DataGridColumn)d).NotifyPropertyChanged(d, e, DataGridNotificationTarget.ColumnHeaders);
        }

        #endregion

        #region Auto Generation

        private static readonly DependencyPropertyKey IsAutoGeneratedPropertyKey =
            DependencyProperty.RegisterReadOnly(
                "IsAutoGenerated",
                typeof(bool),
                typeof(DataGridColumn),
                new FrameworkPropertyMetadata(false));

        /// <summary>
        /// The DependencyProperty for the IsAutoGenerated Property
        /// </summary>
        public static readonly DependencyProperty IsAutoGeneratedProperty = IsAutoGeneratedPropertyKey.DependencyProperty;

        /// <summary>
        /// This property determines whether the column is autogenerate or not.
        /// </summary>
        public bool IsAutoGenerated
        {
            get { return (bool)GetValue(IsAutoGeneratedProperty); }
            internal set { SetValue(IsAutoGeneratedPropertyKey, value); }
        }

        /// <summary>
        /// Helper Method which creates a default DataGridColumn object for the specified property type.
        /// </summary>
        /// <param name="itemProperty"></param>
        /// <returns></returns>
        internal static DataGridColumn CreateDefaultColumn(ItemPropertyInfo itemProperty)
        {
            Debug.Assert(itemProperty != null && itemProperty.PropertyType != null, "itemProperty and/or its PropertyType member cannot be null");

            DataGridColumn dataGridColumn = null;
            DataGridComboBoxColumn comboBoxColumn = null;
            Type propertyType = itemProperty.PropertyType;

            // determine the type of column to be created and create one
            if (propertyType.IsEnum)
            {
                comboBoxColumn = new DataGridComboBoxColumn();
                comboBoxColumn.ItemsSource = Enum.GetValues(propertyType);
                dataGridColumn = comboBoxColumn;
            }
            else if (typeof(string).IsAssignableFrom(propertyType))
            {
                dataGridColumn = new DataGridTextColumn();
            }
            else if (typeof(bool).IsAssignableFrom(propertyType))
            {
                dataGridColumn = new DataGridCheckBoxColumn();
            }
            else if (typeof(Uri).IsAssignableFrom(propertyType))
            {
                dataGridColumn = new DataGridHyperlinkColumn();
            }
            else
            {
                dataGridColumn = new DataGridTextColumn();
            }

            // determine if the datagrid can sort on the column or not
            if (!typeof(IComparable).IsAssignableFrom(propertyType))
            {
                dataGridColumn.CanUserSort = false;
            }

            dataGridColumn.Header = itemProperty.Name;

            // Set the data field binding for such created columns and
            // choose the BindingMode based on editability of the property.
            DataGridBoundColumn boundColumn = dataGridColumn as DataGridBoundColumn;
            if (boundColumn != null || comboBoxColumn != null)
            {
                Binding binding = new Binding(itemProperty.Name);
                if (comboBoxColumn != null)
                {
                    comboBoxColumn.SelectedItemBinding = binding;
                }
                else
                {
                    boundColumn.Binding = binding;
                }

                PropertyDescriptor pd = itemProperty.Descriptor as PropertyDescriptor;
                if (pd != null)
                {
                    if (pd.IsReadOnly)
                    {
                        binding.Mode = BindingMode.OneWay;
                        dataGridColumn.IsReadOnly = true;
                    }
                }
                else
                {
                    PropertyInfo pi = itemProperty.Descriptor as PropertyInfo;
                    if (pi != null)
                    {
                        if (!pi.CanWrite)
                        {
                            binding.Mode = BindingMode.OneWay;
                            dataGridColumn.IsReadOnly = true;
                        }
                    }
                }
            }

            return dataGridColumn;
        }

        #endregion

        #region Frozen Columns

        /// <summary>
        /// Dependency Property Key for IsFrozen property
        /// </summary>
        private static readonly DependencyPropertyKey IsFrozenPropertyKey =
            DependencyProperty.RegisterReadOnly(
                "IsFrozen",
                typeof(bool),
                typeof(DataGridColumn),
                new FrameworkPropertyMetadata(false, new PropertyChangedCallback(OnNotifyFrozenPropertyChanged), new CoerceValueCallback(OnCoerceIsFrozen)));

        /// <summary>
        /// The DependencyProperty for the IsFrozen Property
        /// </summary>
        public static readonly DependencyProperty IsFrozenProperty = IsFrozenPropertyKey.DependencyProperty;

        /// <summary>
        /// This property determines whether the column is frozen or not.
        /// </summary>
        public bool IsFrozen
        {
            get { return (bool)GetValue(IsFrozenProperty); }
            internal set { SetValue(IsFrozenPropertyKey, value); }
        }

        /// <summary>
        /// Property changed callback for IsFrozen property
        /// </summary>
        /// <param name="d"></param>
        /// <param name="e"></param>
        private static void OnNotifyFrozenPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((DataGridColumn)d).NotifyPropertyChanged(d, e, DataGridNotificationTarget.ColumnHeaders);
        }

        /// <summary>
        /// Coercion call back for IsFrozenProperty. Ensures that IsFrozen is set as per the
        /// DataGrid's FrozenColumnCount property.
        /// </summary>
        /// <param name="d"></param>
        /// <param name="baseValue"></param>
        /// <returns></returns>
        private static object OnCoerceIsFrozen(DependencyObject d, object baseValue)
        {
            DataGridColumn column = (DataGridColumn)d;
            DataGrid dataGrid = column.DataGridOwner;
            if (dataGrid != null)
            {
                if (column.DisplayIndex < dataGrid.FrozenColumnCount)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }

            return baseValue;
        }

        #endregion

        #region Column Reordering

        /// <summary>
        ///     The DependencyProperty that represents the CanUserReorder property.
        /// </summary>
        public static readonly DependencyProperty CanUserReorderProperty =
            DependencyProperty.Register("CanUserReorder", typeof(bool), typeof(DataGridColumn), new FrameworkPropertyMetadata(true, new PropertyChangedCallback(OnNotifyColumnPropertyChanged), new CoerceValueCallback(OnCoerceCanUserReorder)));

        /// <summary>
        /// The property which determines if column header can be dragged or not
        /// </summary>
        public bool CanUserReorder
        {
            get { return (bool)GetValue(CanUserReorderProperty); }
            set { SetValue(CanUserReorderProperty, value); }
        }

        private static object OnCoerceCanUserReorder(DependencyObject d, object baseValue)
        {
            var column = d as DataGridColumn;
            return DataGridHelper.GetCoercedTransferPropertyValue(
                column,
                baseValue,
                CanUserReorderProperty,
                column.DataGridOwner,
                DataGrid.CanUserReorderColumnsProperty);
        }

        /// <summary>
        ///     The DependencyProperty that represents the DragIndicatorStyle property.
        /// </summary>
        public static readonly DependencyProperty DragIndicatorStyleProperty =
            DependencyProperty.Register("DragIndicatorStyle", typeof(Style), typeof(DataGridColumn), new FrameworkPropertyMetadata(null, OnNotifyColumnPropertyChanged, OnCoerceDragIndicatorStyle));

        /// <summary>
        /// The style property which would be applied on the column header drag indicator.
        /// </summary>
        public Style DragIndicatorStyle
        {
            get { return (Style)GetValue(DragIndicatorStyleProperty); }
            set { SetValue(DragIndicatorStyleProperty, value); }
        }

        private static object OnCoerceDragIndicatorStyle(DependencyObject d, object baseValue)
        {
            var column = d as DataGridColumn;
            return DataGridHelper.GetCoercedTransferPropertyValue(
                column,
                baseValue,
                DragIndicatorStyleProperty,
                column.DataGridOwner,
                DataGrid.DragIndicatorStyleProperty);
        }

        #endregion

        #region Clipboard Copy/Paste

        /// <summary>
        ///     The binding that will be used to get or set cell content for the clipboard
        /// </summary>
        public virtual BindingBase ClipboardContentBinding
        {
            get
            {
                return _clipboardContentBinding;
            }

            set
            {
                _clipboardContentBinding = value;
            }
        }

        /// <summary>
        /// This method is called for each selected cell in each selected cell to retrieve the default cell content.
        /// Default cell content is calculated using ClipboardContentBinding.
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        public virtual object OnCopyingCellClipboardContent(object item)
        {
            object cellValue = DataGridOwner.GetCellClipboardValue(item, this);

            // Raise the event to give a chance for external listeners to modify the cell content
            if (CopyingCellClipboardContent != null)
            {
                DataGridCellClipboardEventArgs args = new DataGridCellClipboardEventArgs(item, this, cellValue);
                CopyingCellClipboardContent(this, args);
                cellValue = args.Content;
            }

            return cellValue;
        }

        /// We don't provide default Paste but this public method is exposed to help custom implementation of Paste
        /// <summary>
        /// This method stores the cellContent into the item object using ClipboardContentBinding.
        /// </summary>
        /// <param name="item"></param>
        /// <param name="cellContent"></param>
        public virtual void OnPastingCellClipboardContent(object item, object cellContent)
        {
            BindingBase binding = ClipboardContentBinding;
            if (binding != null)
            {
                // Raise the event to give a chance for external listeners to modify the cell content
                // before it gets stored into the cell
                if (PastingCellClipboardContent != null)
                {
                    DataGridCellClipboardEventArgs args = new DataGridCellClipboardEventArgs(item, this, cellContent);
                    PastingCellClipboardContent(this, args);
                    cellContent = args.Content;
                }

                // Event handlers can cancel Paste of a cell by setting its content to null
                if (cellContent != null)
                {
                    DataGridOwner.SetCellClipboardValue(item, this, cellContent);
                }
            }
        }

        /// <summary>
        /// The event is raised for each selected cell after the cell clipboard content is prepared.
        /// Event handlers can modify the cell content before it gets stored into the clipboard.
        /// </summary>
        public event EventHandler<DataGridCellClipboardEventArgs> CopyingCellClipboardContent;

        /// <summary>
        /// The event is raised for each selected cell before the clipboard content is transfered to the cell.
        /// Event handlers can modify the clipboard content before it gets stored into the cell content.
        /// </summary>
        public event EventHandler<DataGridCellClipboardEventArgs> PastingCellClipboardContent;

        #endregion

        #region Special Input

        // Consider making a protected virtual.
        // If made public, look for PUBLIC_ONINPUT (in DataGridCell) and enable.
        internal virtual void OnInput(InputEventArgs e)
        {
        }

        internal void BeginEdit(InputEventArgs e, bool handled)
        {
            var owner = DataGridOwner;
            if (owner != null)
            {
                if (owner.BeginEdit(e))
                {
                    e.Handled |= handled;
                }
            }
        }

        #endregion

        #region Column Resizing

        /// <summary>
        /// Dependency property for CanUserResize
        /// </summary>
        public static readonly DependencyProperty CanUserResizeProperty =
            DependencyProperty.Register("CanUserResize", typeof(bool), typeof(DataGridColumn), new FrameworkPropertyMetadata(true, new PropertyChangedCallback(OnNotifyColumnHeaderPropertyChanged), new CoerceValueCallback(OnCoerceCanUserResize)));

        /// <summary>
        /// Property which indicates if an end user can resize the column or not
        /// </summary>
        public bool CanUserResize
        {
            get { return (bool)GetValue(CanUserResizeProperty); }
            set { SetValue(CanUserResizeProperty, value); }
        }

        private static object OnCoerceCanUserResize(DependencyObject d, object baseValue)
        {
            var column = d as DataGridColumn;
            return DataGridHelper.GetCoercedTransferPropertyValue(
                column,
                baseValue,
                CanUserResizeProperty,
                column.DataGridOwner,
                DataGrid.CanUserResizeColumnsProperty);
        }

        #endregion

        #region Hidden Columns

        /// <summary>
        ///     Dependency property for Visibility
        /// </summary>
        public static readonly DependencyProperty VisibilityProperty =
            DependencyProperty.Register(
                "Visibility",
                typeof(Visibility),
                typeof(DataGridColumn),
                new FrameworkPropertyMetadata(Visibility.Visible, new PropertyChangedCallback(OnVisibilityPropertyChanged)));

        /// <summary>
        ///     The property which determines if the column is visible or not.
        /// </summary>
        public Visibility Visibility
        {
            get { return (Visibility)GetValue(VisibilityProperty); }
            set { SetValue(VisibilityProperty, value); }
        }

        /// <summary>
        ///     Property changed callback for Visibility property
        /// </summary>
        private static void OnVisibilityPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs eventArgs)
        {
            Visibility oldVisibility = (Visibility)eventArgs.OldValue;
            Visibility newVisibility = (Visibility)eventArgs.NewValue;

            if (oldVisibility != Visibility.Visible && newVisibility != Visibility.Visible)
            {
                return;
            }

            ((DataGridColumn)d).NotifyPropertyChanged(
                d,
                eventArgs,
                DataGridNotificationTarget.CellsPresenter | DataGridNotificationTarget.ColumnHeadersPresenter | DataGridNotificationTarget.ColumnCollection | DataGridNotificationTarget.DataGrid | DataGridNotificationTarget.ColumnHeaders);
        }

        /// <summary>
        ///     Helper IsVisible property
        /// </summary>
        internal bool IsVisible
        {
            get
            {
                return Visibility == Visibility.Visible;
            }
        }

        #endregion

        #region Data

        private DataGrid _dataGridOwner = null;                     // This property is updated by DataGrid when the column is added to the DataGrid.Columns collection
        private BindingBase _clipboardContentBinding;               // Storage for ClipboardContentBinding
        private bool _ignoreRedistributionOnWidthChange = false;    // Flag which indicates to ignore recomputation of column widths on width change of column
        private bool _processingWidthChange = false;                // Flag which indicates that execution of width change callback to avoid recursions.
        private const double _starMaxWidth = 10000d;                // Max Width constant for star columns

        #endregion
    }
}
