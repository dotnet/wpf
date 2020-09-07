// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Windows;
using System.Windows.Threading;
using MS.Internal;

namespace System.Windows.Controls
{
    /// <summary>
    ///     Internal class that holds the DataGrid's column collection.  Handles error-checking columns as they come in.
    /// </summary>
    internal class DataGridColumnCollection : ObservableCollection<DataGridColumn>
    {
        internal DataGridColumnCollection(DataGrid dataGridOwner)
        {
            Debug.Assert(dataGridOwner != null, "We should have a valid DataGrid");

            DisplayIndexMap = new List<int>(5);
            _dataGridOwner = dataGridOwner;

            RealizedColumnsBlockListForNonVirtualizedRows = null;
            RealizedColumnsDisplayIndexBlockListForNonVirtualizedRows = null;
            RebuildRealizedColumnsBlockListForNonVirtualizedRows = true;

            RealizedColumnsBlockListForVirtualizedRows = null;
            RealizedColumnsDisplayIndexBlockListForVirtualizedRows = null;
            RebuildRealizedColumnsBlockListForVirtualizedRows = true;
        }

        #region Protected Overrides

        protected override void InsertItem(int index, DataGridColumn item)
        {
            if (item == null)
            {
                throw new ArgumentNullException("item", SR.Get(SRID.DataGrid_NullColumn));
            }

            if (item.DataGridOwner != null)
            {
                throw new ArgumentException(SR.Get(SRID.DataGrid_InvalidColumnReuse, item.Header), "item");
            }

            if (DisplayIndexMapInitialized)
            {
                ValidateDisplayIndex(item, item.DisplayIndex, true);
            }

            base.InsertItem(index, item);
            item.CoerceValue(DataGridColumn.IsFrozenProperty);
        }

        protected override void SetItem(int index, DataGridColumn item)
        {
            if (item == null)
            {
                throw new ArgumentNullException("item", SR.Get(SRID.DataGrid_NullColumn));
            }

            if (index >= Count || index < 0)
            {
                throw new ArgumentOutOfRangeException("index", SR.Get(SRID.DataGrid_ColumnIndexOutOfRange, item.Header));
            }

            if (item.DataGridOwner != null && this[index] != item)
            {
                throw new ArgumentException(SR.Get(SRID.DataGrid_InvalidColumnReuse, item.Header), "item");
            }

            if (DisplayIndexMapInitialized)
            {
                ValidateDisplayIndex(item, item.DisplayIndex);
            }

            base.SetItem(index, item);
            item.CoerceValue(DataGridColumn.IsFrozenProperty);
        }

        protected override void OnCollectionChanged(NotifyCollectionChangedEventArgs e)
        {
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    if (DisplayIndexMapInitialized)
                    {
                        UpdateDisplayIndexForNewColumns(e.NewItems, e.NewStartingIndex);
                    }

                    InvalidateHasVisibleStarColumns();
                    break;

                case NotifyCollectionChangedAction.Move:
                    if (DisplayIndexMapInitialized)
                    {
                        UpdateDisplayIndexForMovedColumn(e.OldStartingIndex, e.NewStartingIndex);
                    }

                    break;

                case NotifyCollectionChangedAction.Remove:
                    if (DisplayIndexMapInitialized)
                    {
                        UpdateDisplayIndexForRemovedColumns(e.OldItems, e.OldStartingIndex);
                    }

                    ClearDisplayIndex(e.OldItems, e.NewItems);
                    InvalidateHasVisibleStarColumns();
                    break;

                case NotifyCollectionChangedAction.Replace:
                    if (DisplayIndexMapInitialized)
                    {
                        UpdateDisplayIndexForReplacedColumn(e.OldItems, e.NewItems);
                    }

                    ClearDisplayIndex(e.OldItems, e.NewItems);
                    InvalidateHasVisibleStarColumns();
                    break;

                case NotifyCollectionChangedAction.Reset:
                    // We dont ClearDisplayIndex here because we no longer have access to the old items.
                    // Instead this is handled in ClearItems.
                    if (DisplayIndexMapInitialized)
                    {
                        DisplayIndexMap.Clear();
                        DataGridOwner.UpdateColumnsOnVirtualizedCellInfoCollections(NotifyCollectionChangedAction.Reset, -1, null, -1);
                    }

                    HasVisibleStarColumns = false;
                    break;
            }

            InvalidateAverageColumnWidth();

            base.OnCollectionChanged(e);
        }

        /// <summary>
        ///     Clear's all the columns from this collection and resets DisplayIndex to its default value.
        /// </summary>
        protected override void ClearItems()
        {
            ClearDisplayIndex(this, null);

            // Clear DataGrid reference is on all columns.
            // Doing it here since CollectionChanged notification wouldn't
            // propagate the cleared columns list.
            DataGridOwner.UpdateDataGridReference(this, true);
            base.ClearItems();
        }

        #endregion

        #region Notification Propagation

        internal void NotifyPropertyChanged(DependencyObject d, string propertyName, DependencyPropertyChangedEventArgs e, DataGridNotificationTarget target)
        {
            if (DataGridHelper.ShouldNotifyColumnCollection(target))
            {
                if (e.Property == DataGridColumn.DisplayIndexProperty)
                {
                    OnColumnDisplayIndexChanged((DataGridColumn)d, (int)e.OldValue, (int)e.NewValue);
                    if (((DataGridColumn)d).IsVisible)
                    {
                        InvalidateColumnRealization(true);
                    }
                }
                else if (e.Property == DataGridColumn.WidthProperty)
                {
                    if (((DataGridColumn)d).IsVisible)
                    {
                        InvalidateColumnRealization(false);
                    }
                }
                else if (e.Property == DataGrid.FrozenColumnCountProperty)
                {
                    InvalidateColumnRealization(false);
                    OnDataGridFrozenColumnCountChanged((int)e.OldValue, (int)e.NewValue);
                }
                else if (e.Property == DataGridColumn.VisibilityProperty)
                {
                    InvalidateAverageColumnWidth();
                    InvalidateHasVisibleStarColumns();
                    InvalidateColumnWidthsComputation();
                    InvalidateColumnRealization(true);
                }
                else if (e.Property == DataGrid.EnableColumnVirtualizationProperty)
                {
                    InvalidateColumnRealization(true);
                }
                else if (e.Property == DataGrid.CellsPanelHorizontalOffsetProperty)
                {
                    OnCellsPanelHorizontalOffsetChanged(e);
                }
                else if (e.Property == DataGrid.HorizontalScrollOffsetProperty ||
                         string.Compare(propertyName, "ViewportWidth", StringComparison.Ordinal) == 0)
                {
                    InvalidateColumnRealization(false);
                }
            }

            if (DataGridHelper.ShouldNotifyColumns(target))
            {
                int count = this.Count;
                for (int i = 0; i < count; i++)
                {
                    // Passing in NotificationTarget.Columns directly to ensure the notification doesn't
                    // bounce back to the collection.
                    this[i].NotifyPropertyChanged(d, e, DataGridNotificationTarget.Columns);
                }
            }
        }

        #endregion

        #region Display Index

        /// <summary>
        ///     Returns the DataGridColumn with the given DisplayIndex
        /// </summary>
        internal DataGridColumn ColumnFromDisplayIndex(int displayIndex)
        {
            Debug.Assert(displayIndex >= 0 && displayIndex < DisplayIndexMap.Count, "displayIndex should have already been validated");
            return this[DisplayIndexMap[displayIndex]];
        }

        /// <summary>
        ///     A map of display index (key) to index in the column collection (value).  Used to quickly find a column from its display index.
        /// </summary>
        internal List<int> DisplayIndexMap
        {
            get
            {
                if (!DisplayIndexMapInitialized)
                {
                    InitializeDisplayIndexMap();
                }

                return _displayIndexMap;
            }

            private set
            {
                _displayIndexMap = value;
            }
        }

        /// <summary>
        ///     Used to guard against re-entrancy when changing the DisplayIndex of a column.
        /// </summary>
        private bool IsUpdatingDisplayIndex
        {
            get { return _isUpdatingDisplayIndex; }
            set { _isUpdatingDisplayIndex = value; }
        }

        private int CoerceDefaultDisplayIndex(DataGridColumn column)
        {
            return CoerceDefaultDisplayIndex(column, IndexOf(column));
        }

        /// <summary>
        ///     This takes a column and checks that if its DisplayIndex is the default value.  If so, it coerces
        ///     the DisplayIndex to be its location in the columns collection.
        ///     We can't do this in CoerceValue because the callback isn't called for default values.  Instead we call this
        ///     whenever a column is added or replaced in the collection or when the DisplayIndex of an existing column has changed.
        /// </summary>
        /// <param name="column">The column</param>
        /// <param name="newDisplayIndex">The DisplayIndex the column should have</param>
        /// <returns>The DisplayIndex of the column</returns>
        private int CoerceDefaultDisplayIndex(DataGridColumn column, int newDisplayIndex)
        {
            if (DataGridHelper.IsDefaultValue(column, DataGridColumn.DisplayIndexProperty))
            {
                bool isUpdating = IsUpdatingDisplayIndex;
                try
                {
                    IsUpdatingDisplayIndex = true;
                    column.DisplayIndex = newDisplayIndex;
                }
                finally
                {
                    IsUpdatingDisplayIndex = isUpdating;
                }

                return newDisplayIndex;
            }

            return column.DisplayIndex;
        }

        /// <summary>
        ///     Called when a column's display index has changed.
        /// <param name="oldDisplayIndex">the old display index of the column</param>
        /// <param name="newDisplayIndex">the new display index of the column</param>
        private void OnColumnDisplayIndexChanged(DataGridColumn column, int oldDisplayIndex, int newDisplayIndex)
        {
            int originalOldDisplayIndex = oldDisplayIndex;
            if (!_displayIndexMapInitialized)
            {
                InitializeDisplayIndexMap(column, oldDisplayIndex, out oldDisplayIndex);
            }

            // Handle ClearValue.
            if (_isClearingDisplayIndex)
            {
                // change from -1 to the new value; the OnColumnDisplayIndexChanged further down the stack (from old value to -1) will handle
                // notifying the user and updating columns.
                return;
            }

            // The DisplayIndex may have changed to the default value.
            newDisplayIndex = CoerceDefaultDisplayIndex(column);

            if (newDisplayIndex == oldDisplayIndex)
            {
                return;
            }

            // Our coerce value callback should have validated the DisplayIndex.  Fire the virtual.
            Debug.Assert(newDisplayIndex >= 0 && newDisplayIndex < Count, "The new DisplayIndex should have already been validated");

            // -1 is the default value and really means 'DisplayIndex should be the index of the column in the column collection'.
            // We immediately replace the display index without notifying anyone.
            if (originalOldDisplayIndex != -1)
            {
                DataGridOwner.OnColumnDisplayIndexChanged(new DataGridColumnEventArgs(column));
            }

            // Call our helper to walk through all other columns and adjust their display indices.
            UpdateDisplayIndexForChangedColumn(oldDisplayIndex, newDisplayIndex);
        }

        /// <summary>
        ///     Called when the DisplayIndex for a single column has changed.  The other columns may have conflicting display indices, so
        ///     we walk through them and adjust.  This method does nothing if we're already updating display index as part of a larger
        ///     operation (such as add or remove).  This is both for re-entrancy and to avoid modifying the display index map as we walk over
        ///     the columns.
        /// </summary>
        private void UpdateDisplayIndexForChangedColumn(int oldDisplayIndex, int newDisplayIndex)
        {
            // The code below adjusts the DisplayIndex of other columns and shouldn't happen if this column's display index is changed
            // to account for the change in another.
            if (IsUpdatingDisplayIndex)
            {
                // Avoid re-entrancy; setting DisplayIndex on columns causes their OnDisplayIndexChanged to fire.
                return;
            }

            try
            {
                IsUpdatingDisplayIndex = true;

                Debug.Assert(oldDisplayIndex != newDisplayIndex, "A column's display index must have changed for us to call OnColumnDisplayIndexChanged");
                Debug.Assert(oldDisplayIndex >= 0 && oldDisplayIndex < Count, "The old DisplayIndex should be valid");

                // Update the display index mapping for all affected columns.
                int columnIndex = DisplayIndexMap[oldDisplayIndex];
                DisplayIndexMap.RemoveAt(oldDisplayIndex);
                DisplayIndexMap.Insert(newDisplayIndex, columnIndex);

                // Update the display index of other columns.
                if (newDisplayIndex < oldDisplayIndex)
                {
                    // DisplayIndex decreased. All columns with DisplayIndex >= newDisplayIndex and < oldDisplayIndex
                    // get their DisplayIndex incremented.
                    for (int i = newDisplayIndex + 1; i <= oldDisplayIndex; i++)
                    {
                        ColumnFromDisplayIndex(i).DisplayIndex++;
                    }
                }
                else
                {
                    // DisplayIndex increased. All columns with DisplayIndex <= newDisplayIndex and > oldDisplayIndex get their DisplayIndex decremented.
                    for (int i = oldDisplayIndex; i < newDisplayIndex; i++)
                    {
                        ColumnFromDisplayIndex(i).DisplayIndex--;
                    }
                }

                Debug_VerifyDisplayIndexMap();

                DataGridOwner.UpdateColumnsOnVirtualizedCellInfoCollections(NotifyCollectionChangedAction.Move, oldDisplayIndex, null, newDisplayIndex);
            }
            finally
            {
                IsUpdatingDisplayIndex = false;
            }
        }

        private void UpdateDisplayIndexForMovedColumn(int oldColumnIndex, int newColumnIndex)
        {
            int displayIndex = RemoveFromDisplayIndexMap(oldColumnIndex);
            InsertInDisplayIndexMap(displayIndex, newColumnIndex);
            DataGridOwner.UpdateColumnsOnVirtualizedCellInfoCollections(NotifyCollectionChangedAction.Move, oldColumnIndex, null, newColumnIndex);
        }

        /// <summary>
        ///     Sets the DisplayIndex on all newly inserted or added columns and updates the existing columns as necessary.
        /// </summary>
        private void UpdateDisplayIndexForNewColumns(IList newColumns, int startingIndex)
        {
            DataGridColumn column;
            int newDisplayIndex, columnIndex;

            Debug.Assert(
                newColumns.Count == 1,
                "This derives from ObservableCollection; it is impossible to add multiple columns at once");
            Debug.Assert(IsUpdatingDisplayIndex == false, "We don't add new columns as part of a display index update operation");

            try
            {
                IsUpdatingDisplayIndex = true;

                // Set the display index of the new columns and add them to the DisplayIndexMap
                column = (DataGridColumn)newColumns[0];
                columnIndex = startingIndex;

                newDisplayIndex = CoerceDefaultDisplayIndex(column, columnIndex);

                // Inserting the column in the map means that all columns with display index >= the new column's display index
                // were given a higher display index.  This is perfect, except that the column indices have changed due to the insert
                // in the column collection.  We need to iterate over the column indices and increment them appropriately.  We also
                // need to give each changed column a new display index.
                InsertInDisplayIndexMap(newDisplayIndex, columnIndex);

                for (int i = 0; i < DisplayIndexMap.Count; i++)
                {
                    if (i > newDisplayIndex)
                    {
                        // All columns with DisplayIndex higher than the newly inserted columns
                        // need to have their DisplayIndex adiusted.
                        column = ColumnFromDisplayIndex(i);
                        column.DisplayIndex++;
                    }
                }

                Debug_VerifyDisplayIndexMap();

                DataGridOwner.UpdateColumnsOnVirtualizedCellInfoCollections(NotifyCollectionChangedAction.Add, -1, null, newDisplayIndex);
            }
            finally
            {
                IsUpdatingDisplayIndex = false;
            }
        }

        // This method is called in first DataGrid measure call
        // It needs to populate DisplayIndexMap and validate the DisplayIndex of all columns
        internal void InitializeDisplayIndexMap()
        {
            int resultIndex = -1;
            InitializeDisplayIndexMap(null, -1, out resultIndex);
        }

        private void InitializeDisplayIndexMap(DataGridColumn changingColumn, int oldDisplayIndex, out int resultDisplayIndex)
        {
            resultDisplayIndex = oldDisplayIndex;
            if (_displayIndexMapInitialized)
            {
                return;
            }

            _displayIndexMapInitialized = true;

            Debug.Assert(DisplayIndexMap.Count == 0, "DisplayIndexMap should be empty until first measure call.");
            int columnCount = Count;
            Dictionary<int, int> assignedDisplayIndexMap = new Dictionary<int, int>(); // <DisplayIndex, ColumnIndex>

            if (changingColumn != null && oldDisplayIndex >= columnCount)
            {
                throw new ArgumentOutOfRangeException("displayIndex", oldDisplayIndex, SR.Get(SRID.DataGrid_ColumnDisplayIndexOutOfRange, changingColumn.Header));
            }

            // First loop:
            // 1. Validate all columns DisplayIndex
            // 2. Add columns with DisplayIndex!=default to the assignedDisplayIndexMap
            for (int columnIndex = 0; columnIndex < columnCount; columnIndex++)
            {
                DataGridColumn currentColumn = this[columnIndex];
                int currentColumnDisplayIndex = currentColumn.DisplayIndex;

                ValidateDisplayIndex(currentColumn, currentColumnDisplayIndex);

                if (currentColumn == changingColumn)
                {
                    currentColumnDisplayIndex = oldDisplayIndex;
                }

                if (currentColumnDisplayIndex >= 0)
                {
                    if (assignedDisplayIndexMap.ContainsKey(currentColumnDisplayIndex))
                    {
                        throw new ArgumentException(SR.Get(SRID.DataGrid_DuplicateDisplayIndex));
                    }

                    assignedDisplayIndexMap.Add(currentColumnDisplayIndex, columnIndex);
                }
            }

            // Second loop:
            // Assign DisplayIndex to the columns with default values
            int nextAvailableColumnIndex = 0;
            for (int columnIndex = 0; columnIndex < columnCount; columnIndex++)
            {
                DataGridColumn currentColumn = this[columnIndex];
                int currentColumnDisplayIndex = currentColumn.DisplayIndex;
                bool hasDefaultDisplayIndex = DataGridHelper.IsDefaultValue(currentColumn, DataGridColumn.DisplayIndexProperty);
                if (currentColumn == changingColumn)
                {
                    if (oldDisplayIndex == -1)
                    {
                        hasDefaultDisplayIndex = true;
                    }
                    currentColumnDisplayIndex = oldDisplayIndex;
                }

                if (hasDefaultDisplayIndex)
                {
                    while (assignedDisplayIndexMap.ContainsKey(nextAvailableColumnIndex))
                    {
                        nextAvailableColumnIndex++;
                    }

                    CoerceDefaultDisplayIndex(currentColumn, nextAvailableColumnIndex);
                    assignedDisplayIndexMap.Add(nextAvailableColumnIndex, columnIndex);
                    if (currentColumn == changingColumn)
                    {
                        resultDisplayIndex = nextAvailableColumnIndex;
                    }
                    nextAvailableColumnIndex++;
                }
            }

            // Third loop:
            // Copy generated assignedDisplayIndexMap into DisplayIndexMap
            for (int displayIndex = 0; displayIndex < columnCount; displayIndex++)
            {
                Debug.Assert(assignedDisplayIndexMap.ContainsKey(displayIndex));
                DisplayIndexMap.Add(assignedDisplayIndexMap[displayIndex]);
            }
        }

        /// <summary>
        ///     Updates the display index for all columns affected by the removal of a set of columns.
        /// </summary>
        private void UpdateDisplayIndexForRemovedColumns(IList oldColumns, int startingIndex)
        {
            DataGridColumn column;
            Debug.Assert(
                oldColumns.Count == 1,
                "This derives from ObservableCollection; it is impossible to remove multiple columns at once");
            Debug.Assert(IsUpdatingDisplayIndex == false, "We don't remove columns as part of a display index update operation");

            try
            {
                IsUpdatingDisplayIndex = true;
                Debug.Assert(DisplayIndexMap.Count > Count, "Columns were just removed: the display index map shouldn't have yet been updated");

                int removedDisplayIndex = RemoveFromDisplayIndexMap(startingIndex);

                // Removing the column in the map means that all columns with display index >= the new column's display index
                // were given a lower display index.  This is perfect, except that the column indices have changed due to the insert
                // in the column collection.  We need to iterate over the column indices and decrement them appropriately.  We also
                // need to give each changed column a new display index.
                for (int i = 0; i < DisplayIndexMap.Count; i++)
                {
                    if (i >= removedDisplayIndex)
                    {
                        // All columns with DisplayIndex higher than the newly deleted columns need to have their DisplayIndex adiusted
                        // (we use >= because a column will have been decremented to have the same display index as the deleted column).
                        column = ColumnFromDisplayIndex(i);
                        column.DisplayIndex--;
                    }
                }

                Debug_VerifyDisplayIndexMap();

                DataGridOwner.UpdateColumnsOnVirtualizedCellInfoCollections(NotifyCollectionChangedAction.Remove, removedDisplayIndex, (DataGridColumn)oldColumns[0], -1);
            }
            finally
            {
                IsUpdatingDisplayIndex = false;
            }
        }

        /// <summary>
        ///     Updates the display index for the column that was just replaced and adjusts the other columns if necessary
        /// </summary>
        private void UpdateDisplayIndexForReplacedColumn(IList oldColumns, IList newColumns)
        {
            if (oldColumns != null && oldColumns.Count > 0 && newColumns != null && newColumns.Count > 0)
            {
                Debug.Assert(oldColumns.Count == 1 && newColumns.Count == 1, "Multi replace isn't possible with ObservableCollection");
                DataGridColumn oldColumn = (DataGridColumn)oldColumns[0];
                DataGridColumn newColumn = (DataGridColumn)newColumns[0];

                if (oldColumn != null && newColumn != null)
                {
                    int newDisplayIndex = CoerceDefaultDisplayIndex(newColumn);

                    if (oldColumn.DisplayIndex != newDisplayIndex)
                    {
                        // Update the display index of other columns to adjust for that of the new one.
                        UpdateDisplayIndexForChangedColumn(oldColumn.DisplayIndex, newDisplayIndex);
                    }

                    DataGridOwner.UpdateColumnsOnVirtualizedCellInfoCollections(NotifyCollectionChangedAction.Replace, newDisplayIndex, oldColumn, newDisplayIndex);
                }
            }
        }

        /// <summary>
        ///     Clears the DisplayIndexProperty on each of the columns.
        /// </summary>
        private void ClearDisplayIndex(IList oldColumns, IList newColumns)
        {
            if (oldColumns != null)
            {
                try
                {
                    _isClearingDisplayIndex = true;
                    var count = oldColumns.Count;
                    for (int i = 0; i < count; i++)
                    {
                        var column = (DataGridColumn)oldColumns[i];

                        // Only clear the old column's index if its not in newColumns
                        if (newColumns != null && newColumns.Contains(column))
                        {
                            continue;
                        }

                        column.ClearValue(DataGridColumn.DisplayIndexProperty);
                    }
                }
                finally
                {
                    _isClearingDisplayIndex = false;
                }
            }
        }

        /// <summary>
        ///     Returns true if the display index is valid for the given column
        /// </summary>
        private bool IsDisplayIndexValid(DataGridColumn column, int displayIndex, bool isAdding)
        {
            // -1 is legal only as a default value
            if (displayIndex == -1 && DataGridHelper.IsDefaultValue(column, DataGridColumn.DisplayIndexProperty))
            {
                return true;
            }

            // If we're adding a column the count will soon be increased by one -- so a DisplayIndex == Count is ok.
            return displayIndex >= 0 && (isAdding ? displayIndex <= Count : displayIndex < Count);
        }

        /// <summary>
        ///     Inserts the given columnIndex in the DisplayIndexMap at the given display index.
        /// </summary>
        private void InsertInDisplayIndexMap(int newDisplayIndex, int columnIndex)
        {
            DisplayIndexMap.Insert(newDisplayIndex, columnIndex);

            for (int i = 0; i < DisplayIndexMap.Count; i++)
            {
                if (DisplayIndexMap[i] >= columnIndex && i != newDisplayIndex)
                {
                    // These are columns that are after the inserted item in the column collection; we have to adiust
                    // to account for the shifted column index.
                    DisplayIndexMap[i]++;
                }
            }
        }

        /// <summary>
        ///     Removes the given column index from the DisplayIndexMap
        /// </summary>
        private int RemoveFromDisplayIndexMap(int columnIndex)
        {
            int removedDisplayIndex = DisplayIndexMap.IndexOf(columnIndex);
            Debug.Assert(removedDisplayIndex >= 0);

            DisplayIndexMap.RemoveAt(removedDisplayIndex);

            for (int i = 0; i < DisplayIndexMap.Count; i++)
            {
                if (DisplayIndexMap[i] >= columnIndex)
                {
                    // These are columns that are after the removed item in the column collection; we have to adiust
                    // to account for the shifted column index.
                    DisplayIndexMap[i]--;
                }
            }

            return removedDisplayIndex;
        }

        /// <summary>
        ///     Throws an ArgumentOutOfRangeException if the given displayIndex is invalid for the given column.
        /// </summary>
        internal void ValidateDisplayIndex(DataGridColumn column, int displayIndex)
        {
            ValidateDisplayIndex(column, displayIndex, false);
        }

        /// <summary>
        ///     Throws an ArgumentOutOfRangeException if the given displayIndex is invalid for the given column.
        /// </summary>
        internal void ValidateDisplayIndex(DataGridColumn column, int displayIndex, bool isAdding)
        {
            if (!IsDisplayIndexValid(column, displayIndex, isAdding))
            {
                throw new ArgumentOutOfRangeException("displayIndex", displayIndex, SR.Get(SRID.DataGrid_ColumnDisplayIndexOutOfRange, column.Header));
            }
        }

        [Conditional("DEBUG")]
        private void Debug_VerifyDisplayIndexMap()
        {
            Debug.Assert(Count == DisplayIndexMap.Count, "Display Index map is of the wrong size");
            for (int i = 0; i < DisplayIndexMap.Count; i++)
            {
                Debug.Assert(DisplayIndexMap[i] >= 0 && DisplayIndexMap[i] < Count, "DisplayIndex map entry doesn't point to a valid column");
                Debug.Assert(ColumnFromDisplayIndex(i).DisplayIndex == i, "DisplayIndex map doesn't match column indices");
            }
        }

        #endregion

        #region Frozen Columns

        /// <summary>
        ///     Method which sets / resets the IsFrozen property of columns based on DataGrid's FrozenColumnCount.
        ///     It is possible that the FrozenColumnCount change could be a result of column count itself, in
        ///     which case only the columns which are in the collection at the moment are to be considered.
        /// </summary>
        /// <param name="oldFrozenCount"></param>
        /// <param name="newFrozenCount"></param>
        private void OnDataGridFrozenColumnCountChanged(int oldFrozenCount, int newFrozenCount)
        {
            if (newFrozenCount > oldFrozenCount)
            {
                int columnCount = Math.Min(newFrozenCount, Count);
                for (int i = oldFrozenCount; i < columnCount; i++)
                {
                    ColumnFromDisplayIndex(i).IsFrozen = true;
                }
            }
            else
            {
                int columnCount = Math.Min(oldFrozenCount, Count);
                for (int i = newFrozenCount; i < columnCount; i++)
                {
                    ColumnFromDisplayIndex(i).IsFrozen = false;
                }
            }
        }

        #endregion

        #region Helpers

        private DataGrid DataGridOwner
        {
            get { return _dataGridOwner; }
        }

        // Used by DataGridColumnCollection to delay the validation of DisplayIndex
        // Validation should be delayed because we in the process of adding columns we may have DisplayIndex less that current columns number
        // After all columns are generated or added in xaml we can do the validation
        internal bool DisplayIndexMapInitialized
        {
            get
            {
                return _displayIndexMapInitialized;
            }
        }

        #endregion

        #region Star Column Helper

        /// <summary>
        ///     Method which determines if there are any
        ///     star columns in datagrid except the given column and also returns perStarWidth
        /// </summary>
        private bool HasVisibleStarColumnsInternal(DataGridColumn ignoredColumn, out double perStarWidth)
        {
            bool hasStarColumns = false;
            perStarWidth = 0.0;
            foreach (DataGridColumn column in this)
            {
                if (column == ignoredColumn ||
                    !column.IsVisible)
                {
                    continue;
                }

                DataGridLength width = column.Width;
                if (width.IsStar)
                {
                    hasStarColumns = true;
                    if (!DoubleUtil.AreClose(width.Value, 0.0) &&
                        !DoubleUtil.AreClose(width.DesiredValue, 0.0))
                    {
                        perStarWidth = width.DesiredValue / width.Value;
                        break;
                    }
                }
            }

            return hasStarColumns;
        }

        /// <summary>
        ///     Method which determines if there are any
        ///     star columns in datagrid and also returns perStarWidth
        /// </summary>
        private bool HasVisibleStarColumnsInternal(out double perStarWidth)
        {
            return HasVisibleStarColumnsInternal(null, out perStarWidth);
        }

        /// <summary>
        ///     Method which determines if there are any
        ///     star columns in datagrid except the given column
        /// </summary>
        private bool HasVisibleStarColumnsInternal(DataGridColumn ignoredColumn)
        {
            double perStarWidth;
            return HasVisibleStarColumnsInternal(ignoredColumn, out perStarWidth);
        }

        /// <summary>
        ///     Property which determines if there are any star columns
        ///     in the datagrid.
        /// </summary>
        internal bool HasVisibleStarColumns
        {
            get
            {
                return _hasVisibleStarColumns;
            }
            private set
            {
                if (_hasVisibleStarColumns != value)
                {
                    _hasVisibleStarColumns = value;
                    DataGridOwner.OnHasVisibleStarColumnsChanged();
                }
            }
        }

        /// <summary>
        ///     Method which redetermines if the collection has any star columns are not.
        /// </summary>
        internal void InvalidateHasVisibleStarColumns()
        {
            HasVisibleStarColumns = HasVisibleStarColumnsInternal(null);
        }

        /// <summary>
        ///     Method which redistributes the width of star columns among them selves
        /// </summary>
        private void RecomputeStarColumnWidths()
        {
            double totalDisplaySpace = DataGridOwner.GetViewportWidthForColumns();
            double nonStarSpace = 0.0;
            foreach (DataGridColumn column in this)
            {
                DataGridLength width = column.Width;
                if (column.IsVisible && !width.IsStar)
                {
                    nonStarSpace += width.DisplayValue;
                }
            }

            if (DoubleUtil.IsNaN(nonStarSpace))
            {
                return;
            }

            ComputeStarColumnWidths(totalDisplaySpace - nonStarSpace);
        }

        /// <summary>
        ///     Helper method which computes the widths of all the star columns
        /// </summary>
        private double ComputeStarColumnWidths(double availableStarSpace)
        {
            Debug.Assert(
                !DoubleUtil.IsNaN(availableStarSpace) && !Double.IsNegativeInfinity(availableStarSpace),
                "availableStarSpace is not valid");

            List<DataGridColumn> unResolvedColumns = new List<DataGridColumn>();
            List<DataGridColumn> partialResolvedColumns = new List<DataGridColumn>();
            double totalFactors = 0.0;
            double totalMinWidths = 0.0;
            double totalMaxWidths = 0.0;
            double utilizedStarSpace = 0.0;

            // Accumulate all the star columns into unResolvedColumns in the beginning
            foreach (DataGridColumn column in this)
            {
                DataGridLength width = column.Width;
                if (column.IsVisible && width.IsStar)
                {
                    unResolvedColumns.Add(column);
                    totalFactors += width.Value;
                    totalMinWidths += column.MinWidth;
                    totalMaxWidths += column.MaxWidth;
                }
            }

            if (DoubleUtil.LessThan(availableStarSpace, totalMinWidths))
            {
                availableStarSpace = totalMinWidths;
            }

            if (DoubleUtil.GreaterThan(availableStarSpace, totalMaxWidths))
            {
                availableStarSpace = totalMaxWidths;
            }

            while (unResolvedColumns.Count > 0)
            {
                double starValue = availableStarSpace / totalFactors;

                // Find all the columns whose star share is less than thier min width and move such columns
                // into partialResolvedColumns giving them atleast the minwidth and there by reducing the availableSpace and totalFactors
                for (int i = 0, count = unResolvedColumns.Count; i < count; i++)
                {
                    DataGridColumn column = unResolvedColumns[i];
                    DataGridLength width = column.Width;

                    double columnMinWidth = column.MinWidth;
                    double starColumnWidth = availableStarSpace * width.Value / totalFactors;

                    if (DoubleUtil.GreaterThan(columnMinWidth, starColumnWidth))
                    {
                        availableStarSpace = Math.Max(0.0, availableStarSpace - columnMinWidth);
                        totalFactors -= width.Value;
                        unResolvedColumns.RemoveAt(i);
                        i--;
                        count--;
                        partialResolvedColumns.Add(column);
                    }
                }

                // With the remaining space determine in any columns star share is more than maxwidth.
                // If such columns are found give them their max width and remove them from unResolvedColumns
                // there by reducing the availablespace and totalfactors. If such column is found, the remaining columns are to be recomputed
                bool iterationRequired = false;
                for (int i = 0, count = unResolvedColumns.Count; i < count; i++)
                {
                    DataGridColumn column = unResolvedColumns[i];
                    DataGridLength width = column.Width;

                    double columnMaxWidth = column.MaxWidth;
                    double starColumnWidth = availableStarSpace * width.Value / totalFactors;

                    if (DoubleUtil.LessThan(columnMaxWidth, starColumnWidth))
                    {
                        iterationRequired = true;
                        unResolvedColumns.RemoveAt(i);
                        availableStarSpace -= columnMaxWidth;
                        utilizedStarSpace += columnMaxWidth;
                        totalFactors -= width.Value;
                        column.UpdateWidthForStarColumn(columnMaxWidth, starValue * width.Value, width.Value);
                        break;
                    }
                }

                // If it was determined by the previous step that another iteration is needed
                // then move all the partialResolvedColumns back to unResolvedColumns and there by
                // restoring availablespace and totalfactors.
                // If another iteration is not needed then allocate min widths to all columns in
                // partial resolved columns and star share to all unresolved columns there by
                // ending the loop
                if (iterationRequired)
                {
                    for (int i = 0, count = partialResolvedColumns.Count; i < count; i++)
                    {
                        DataGridColumn column = partialResolvedColumns[i];

                        unResolvedColumns.Add(column);
                        availableStarSpace += column.MinWidth;
                        totalFactors += column.Width.Value;
                    }

                    partialResolvedColumns.Clear();
                }
                else
                {
                    for (int i = 0, count = partialResolvedColumns.Count; i < count; i++)
                    {
                        DataGridColumn column = partialResolvedColumns[i];
                        DataGridLength width = column.Width;
                        double columnMinWidth = column.MinWidth;
                        column.UpdateWidthForStarColumn(columnMinWidth, width.Value * starValue, width.Value);
                        utilizedStarSpace += columnMinWidth;
                    }

                    partialResolvedColumns.Clear();
                    for (int i = 0, count = unResolvedColumns.Count; i < count; i++)
                    {
                        DataGridColumn column = unResolvedColumns[i];
                        DataGridLength width = column.Width;
                        double starColumnWidth = availableStarSpace * width.Value / totalFactors;
                        column.UpdateWidthForStarColumn(starColumnWidth, width.Value * starValue, width.Value);
                        utilizedStarSpace += starColumnWidth;
                    }

                    unResolvedColumns.Clear();
                }
            }

            return utilizedStarSpace;
        }

        #endregion

        #region Column Width Computation Helper

        /// <summary>
        ///     Method which handles the column widths computation for CellsPanelHorizontalOffset change
        /// </summary>
        private void OnCellsPanelHorizontalOffsetChanged(DependencyPropertyChangedEventArgs e)
        {
            InvalidateColumnRealization(false);

            // Change in CellsPanelOffset width has an opposite effect on Column
            // width distribution. Hence widthChange is (oldvalue - newvalue)
            double totalAvailableWidth = DataGridOwner.GetViewportWidthForColumns();
            RedistributeColumnWidthsOnAvailableSpaceChange((double)e.OldValue - (double)e.NewValue, totalAvailableWidth);
        }

        /// <summary>
        ///     Helper method to invalidate the average width computation
        /// </summary>
        internal void InvalidateAverageColumnWidth()
        {
            _averageColumnWidth = null;

            // changing a column width should also invalidate the maximum desired
            // size of the row presenter
            VirtualizingStackPanel vsp = (DataGridOwner == null) ? null :
                    DataGridOwner.InternalItemsHost as VirtualizingStackPanel;
            if (vsp != null)
            {
                vsp.ResetMaximumDesiredSize();
            }
        }

        /// <summary>
        ///     Property holding the average width of columns
        /// </summary>
        internal double AverageColumnWidth
        {
            get
            {
                if (!_averageColumnWidth.HasValue)
                {
                    _averageColumnWidth = ComputeAverageColumnWidth();
                }

                return _averageColumnWidth.Value;
            }
        }

        /// <summary>
        ///     Helper method which determines the average width of all the columns
        /// </summary>
        private double ComputeAverageColumnWidth()
        {
            double eligibleDisplayValue = 0.0;
            int totalFactors = 0;
            foreach (DataGridColumn column in this)
            {
                DataGridLength width = column.Width;
                if (column.IsVisible && !DoubleUtil.IsNaN(width.DisplayValue))
                {
                    eligibleDisplayValue += width.DisplayValue;
                    totalFactors++;
                }
            }

            if (totalFactors != 0)
            {
                return eligibleDisplayValue / totalFactors;
            }

            return 0.0;
        }

        /// <summary>
        ///     Property indicating whether the column width computation opertaion is pending
        /// </summary>
        internal bool ColumnWidthsComputationPending
        {
            get
            {
                return _columnWidthsComputationPending;
            }
        }

        /// <summary>
        ///     Helper method to invalidate the column width computation
        /// </summary>
        internal void InvalidateColumnWidthsComputation()
        {
            if (_columnWidthsComputationPending)
            {
                return;
            }

            DataGridOwner.Dispatcher.BeginInvoke(new DispatcherOperationCallback(ComputeColumnWidths), DispatcherPriority.Render, this);
            _columnWidthsComputationPending = true;
        }

        /// <summary>
        ///     Helper method which computes the widths of the columns. Used as a callback
        ///     to dispatcher operation
        /// </summary>
        private object ComputeColumnWidths(object arg)
        {
            ComputeColumnWidths();
            DataGridOwner.NotifyPropertyChanged(
                DataGridOwner,
                "DelayedColumnWidthComputation",
                new DependencyPropertyChangedEventArgs(),
                DataGridNotificationTarget.CellsPresenter | DataGridNotificationTarget.ColumnHeadersPresenter);
            return null;
        }

        /// <summary>
        ///     Method which computes the widths of the columns
        /// </summary>
        private void ComputeColumnWidths()
        {
            if (HasVisibleStarColumns)
            {
                InitializeColumnDisplayValues();
                DistributeSpaceAmongColumns(DataGridOwner.GetViewportWidthForColumns());
            }
            else
            {
                ExpandAllColumnWidthsToDesiredValue();
            }

            if (RefreshAutoWidthColumns)
            {
                foreach (DataGridColumn column in this)
                {
                    if (column.Width.IsAuto)
                    {
                        // This operation resets desired and display widths to 0.0.
                        column.Width = DataGridLength.Auto;
                    }
                }

                RefreshAutoWidthColumns = false;
            }

            _columnWidthsComputationPending = false;
        }

        /// <summary>
        ///     Method which initializes the column width's diplay value to its desired value
        /// </summary>
        private void InitializeColumnDisplayValues()
        {
            foreach (DataGridColumn column in this)
            {
                if (!column.IsVisible)
                {
                    continue;
                }

                DataGridLength width = column.Width;
                if (!width.IsStar)
                {
                    double minWidth = column.MinWidth;
                    double displayValue = DataGridHelper.CoerceToMinMax(DoubleUtil.IsNaN(width.DesiredValue) ? minWidth : width.DesiredValue, minWidth, column.MaxWidth);
                    if (!DoubleUtil.AreClose(width.DisplayValue, displayValue))
                    {
                        column.SetWidthInternal(new DataGridLength(width.Value, width.UnitType, width.DesiredValue, displayValue));
                    }
                }
            }
        }

        /// <summary>
        ///     Method which redistributes the column widths based on change in MinWidth of a column
        /// </summary>
        internal void RedistributeColumnWidthsOnMinWidthChangeOfColumn(DataGridColumn changedColumn, double oldMinWidth)
        {
            if (ColumnWidthsComputationPending)
            {
                return;
            }

            DataGridLength width = changedColumn.Width;
            double minWidth = changedColumn.MinWidth;
            if (DoubleUtil.GreaterThan(minWidth, width.DisplayValue))
            {
                if (HasVisibleStarColumns)
                {
                    TakeAwayWidthFromColumns(changedColumn, minWidth - width.DisplayValue, false);
                }

                changedColumn.SetWidthInternal(new DataGridLength(width.Value, width.UnitType, width.DesiredValue, minWidth));
            }
            else if (DoubleUtil.LessThan(minWidth, oldMinWidth))
            {
                if (width.IsStar)
                {
                    if (DoubleUtil.AreClose(width.DisplayValue, oldMinWidth))
                    {
                        GiveAwayWidthToColumns(changedColumn, oldMinWidth - minWidth, true);
                    }
                }
                else if (DoubleUtil.GreaterThan(oldMinWidth, width.DesiredValue))
                {
                    double displayValue = Math.Max(width.DesiredValue, minWidth);
                    if (HasVisibleStarColumns)
                    {
                        GiveAwayWidthToColumns(changedColumn, oldMinWidth - displayValue);
                    }

                    changedColumn.SetWidthInternal(new DataGridLength(width.Value, width.UnitType, width.DesiredValue, displayValue));
                }
            }
        }

        /// <summary>
        ///     Method which redistributes the column widths based on change in MaxWidth of a column
        /// </summary>
        internal void RedistributeColumnWidthsOnMaxWidthChangeOfColumn(DataGridColumn changedColumn, double oldMaxWidth)
        {
            if (ColumnWidthsComputationPending)
            {
                return;
            }

            DataGridLength width = changedColumn.Width;
            double maxWidth = changedColumn.MaxWidth;
            if (DoubleUtil.LessThan(maxWidth, width.DisplayValue))
            {
                if (HasVisibleStarColumns)
                {
                    GiveAwayWidthToColumns(changedColumn, width.DisplayValue - maxWidth);
                }

                changedColumn.SetWidthInternal(new DataGridLength(width.Value, width.UnitType, width.DesiredValue, maxWidth));
            }
            else if (DoubleUtil.GreaterThan(maxWidth, oldMaxWidth))
            {
                if (width.IsStar)
                {
                    RecomputeStarColumnWidths();
                }
                else if (DoubleUtil.LessThan(oldMaxWidth, width.DesiredValue))
                {
                    double displayValue = Math.Min(width.DesiredValue, maxWidth);
                    if (HasVisibleStarColumns)
                    {
                        double leftOverSpace = TakeAwayWidthFromUnusedSpace(false, displayValue - oldMaxWidth);
                        leftOverSpace = TakeAwayWidthFromStarColumns(changedColumn, leftOverSpace);
                        displayValue -= leftOverSpace;
                    }

                    changedColumn.SetWidthInternal(new DataGridLength(width.Value, width.UnitType, width.DesiredValue, displayValue));
                }
            }
        }

        /// <summary>
        ///     Method which redistributes the column widths based on change in Width of a column
        /// </summary>
        internal void RedistributeColumnWidthsOnWidthChangeOfColumn(DataGridColumn changedColumn, DataGridLength oldWidth)
        {
            if (ColumnWidthsComputationPending)
            {
                return;
            }

            DataGridLength width = changedColumn.Width;
            bool hasStarColumns = HasVisibleStarColumns;
            if (oldWidth.IsStar && !width.IsStar && !hasStarColumns)
            {
                ExpandAllColumnWidthsToDesiredValue();
            }
            else if (width.IsStar && !oldWidth.IsStar)
            {
                if (!HasVisibleStarColumnsInternal(changedColumn))
                {
                    ComputeColumnWidths();
                }
                else
                {
                    double minWidth = changedColumn.MinWidth;
                    double leftOverSpace = GiveAwayWidthToNonStarColumns(null, oldWidth.DisplayValue - minWidth);
                    changedColumn.SetWidthInternal(new DataGridLength(width.Value, width.UnitType, width.DesiredValue, minWidth + leftOverSpace));
                    RecomputeStarColumnWidths();
                }
            }
            else if (width.IsStar && oldWidth.IsStar)
            {
                RecomputeStarColumnWidths();
            }
            else if (hasStarColumns)
            {
                RedistributeColumnWidthsOnNonStarWidthChange(
                    changedColumn,
                    oldWidth);
            }
        }

        /// <summary>
        ///     Method which redistributes the column widths based on change in available space of a column
        /// </summary>
        internal void RedistributeColumnWidthsOnAvailableSpaceChange(double availableSpaceChange, double newTotalAvailableSpace)
        {
            if (!ColumnWidthsComputationPending && HasVisibleStarColumns)
            {
                if (DoubleUtil.GreaterThan(availableSpaceChange, 0.0))
                {
                    GiveAwayWidthToColumns(null, availableSpaceChange);
                }
                else if (DoubleUtil.LessThan(availableSpaceChange, 0.0))
                {
                    TakeAwayWidthFromColumns(null, Math.Abs(availableSpaceChange), false, newTotalAvailableSpace);
                }
            }
        }

        /// <summary>
        ///     Method which expands the display values of widths of all columns to
        ///     their desired values. Usually used when the last star column's width
        ///     is changed to non-star
        /// </summary>
        private void ExpandAllColumnWidthsToDesiredValue()
        {
            foreach (DataGridColumn column in this)
            {
                if (!column.IsVisible)
                {
                    continue;
                }

                DataGridLength width = column.Width;
                double maxWidth = column.MaxWidth;
                if (DoubleUtil.GreaterThan(width.DesiredValue, width.DisplayValue) &&
                    !DoubleUtil.AreClose(width.DisplayValue, maxWidth))
                {
                    column.SetWidthInternal(new DataGridLength(width.Value, width.UnitType, width.DesiredValue, Math.Min(width.DesiredValue, maxWidth)));
                }
            }
        }

        /// <summary>
        ///     Method which redistributes widths of columns on change of a column's width
        ///     when datagrid itself has star columns, but neither the oldwidth or the newwidth
        ///     of changed column is star.
        /// </summary>
        private void RedistributeColumnWidthsOnNonStarWidthChange(DataGridColumn changedColumn, DataGridLength oldWidth)
        {
            DataGridLength width = changedColumn.Width;
            if (DoubleUtil.GreaterThan(width.DesiredValue, oldWidth.DisplayValue))
            {
                double nonRetrievableSpace = TakeAwayWidthFromColumns(changedColumn, width.DesiredValue - oldWidth.DisplayValue, changedColumn != null);
                if (DoubleUtil.GreaterThan(nonRetrievableSpace, 0.0))
                {
                    changedColumn.SetWidthInternal(new DataGridLength(
                        width.Value,
                        width.UnitType,
                        width.DesiredValue,
                        Math.Max(width.DisplayValue - nonRetrievableSpace, changedColumn.MinWidth)));
                }
            }
            else if (DoubleUtil.LessThan(width.DesiredValue, oldWidth.DisplayValue))
            {
                double newDesiredValue = DataGridHelper.CoerceToMinMax(width.DesiredValue, changedColumn.MinWidth, changedColumn.MaxWidth);
                GiveAwayWidthToColumns(changedColumn, oldWidth.DisplayValue - newDesiredValue);
            }
        }

        /// <summary>
        ///     Method which distributes a given amount of width among all the columns
        /// </summary>
        private void DistributeSpaceAmongColumns(double availableSpace)
        {
            double sumOfMinWidths = 0.0;
            double sumOfMaxWidths = 0.0;
            double sumOfStarMinWidths = 0.0;
            foreach (DataGridColumn column in this)
            {
                if (!column.IsVisible)
                {
                    continue;
                }

                sumOfMinWidths += column.MinWidth;
                sumOfMaxWidths += column.MaxWidth;
                if (column.Width.IsStar)
                {
                    sumOfStarMinWidths += column.MinWidth;
                }
            }

            if (DoubleUtil.LessThan(availableSpace, sumOfMinWidths))
            {
                availableSpace = sumOfMinWidths;
            }

            if (DoubleUtil.GreaterThan(availableSpace, sumOfMaxWidths))
            {
                availableSpace = sumOfMaxWidths;
            }

            double nonStarSpaceLeftOver = DistributeSpaceAmongNonStarColumns(availableSpace - sumOfStarMinWidths);

            ComputeStarColumnWidths(sumOfStarMinWidths + nonStarSpaceLeftOver);
        }

        /// <summary>
        ///     Helper method which distributes a given amount of width among all non star columns
        /// </summary>
        private double DistributeSpaceAmongNonStarColumns(double availableSpace)
        {
            double requiredSpace = 0.0;
            foreach (DataGridColumn column in this)
            {
                DataGridLength width = column.Width;
                if (!column.IsVisible ||
                    width.IsStar)
                {
                    continue;
                }

                requiredSpace += width.DisplayValue;
            }

            if (DoubleUtil.LessThan(availableSpace, requiredSpace))
            {
                double spaceDeficit = requiredSpace - availableSpace;
                TakeAwayWidthFromNonStarColumns(null, spaceDeficit);
            }

            return Math.Max(availableSpace - requiredSpace, 0.0);
        }

        #endregion

        #region Column Resizing Helper

        /// <summary>
        ///     Method which is called when user resize of column starts
        /// </summary>
        internal void OnColumnResizeStarted()
        {
            _originalWidthsForResize = new Dictionary<DataGridColumn, DataGridLength>();
            foreach (DataGridColumn column in this)
            {
                _originalWidthsForResize[column] = column.Width;
            }
        }

        /// <summary>
        ///     Method which is called when user resize of column ends
        /// </summary>
        internal void OnColumnResizeCompleted(bool cancel)
        {
            if (cancel && _originalWidthsForResize != null)
            {
                foreach (DataGridColumn column in this)
                {
                    if (_originalWidthsForResize.ContainsKey(column))
                    {
                        column.Width = _originalWidthsForResize[column];
                    }
                }
            }

            _originalWidthsForResize = null;
        }

        /// <summary>
        ///     Method which recomputes the widths of columns on resize of column
        /// </summary>
        internal void RecomputeColumnWidthsOnColumnResize(DataGridColumn resizingColumn, double horizontalChange, bool retainAuto)
        {
            DataGridLength resizingColumnWidth = resizingColumn.Width;
            double expectedRezingColumnWidth = resizingColumnWidth.DisplayValue + horizontalChange;

            if (DoubleUtil.LessThan(expectedRezingColumnWidth, resizingColumn.MinWidth))
            {
                horizontalChange = resizingColumn.MinWidth - resizingColumnWidth.DisplayValue;
            }
            else if (DoubleUtil.GreaterThan(expectedRezingColumnWidth, resizingColumn.MaxWidth))
            {
                horizontalChange = resizingColumn.MaxWidth - resizingColumnWidth.DisplayValue;
            }

            int resizingColumnIndex = resizingColumn.DisplayIndex;

            if (DoubleUtil.GreaterThan(horizontalChange, 0.0))
            {
                RecomputeColumnWidthsOnColumnPositiveResize(horizontalChange, resizingColumnIndex, retainAuto);
            }
            else if (DoubleUtil.LessThan(horizontalChange, 0.0))
            {
                RecomputeColumnWidthsOnColumnNegativeResize(-horizontalChange, resizingColumnIndex, retainAuto);
            }
        }

        /// <summary>
        ///     Method which computes widths of columns on positive resize of a column
        /// </summary>
        private void RecomputeColumnWidthsOnColumnPositiveResize(
            double horizontalChange,
            int resizingColumnIndex,
            bool retainAuto)
        {
            double perStarWidth = 0.0;
            if (HasVisibleStarColumnsInternal(out perStarWidth))
            {
                // reuse unused space
                horizontalChange = TakeAwayUnusedSpaceOnColumnPositiveResize(horizontalChange, resizingColumnIndex, retainAuto);

                // reducing columns to the right which are greater than the desired size
                horizontalChange = RecomputeNonStarColumnWidthsOnColumnPositiveResize(horizontalChange, resizingColumnIndex, retainAuto, true);

                // reducing star columns to right
                horizontalChange = RecomputeStarColumnWidthsOnColumnPositiveResize(horizontalChange, resizingColumnIndex, perStarWidth, retainAuto);

                // reducing columns to the right which are greater than the min size
                horizontalChange = RecomputeNonStarColumnWidthsOnColumnPositiveResize(horizontalChange, resizingColumnIndex, retainAuto, false);
            }
            else
            {
                DataGridColumn column = ColumnFromDisplayIndex(resizingColumnIndex);
                SetResizedColumnWidth(column, horizontalChange, retainAuto);
            }
        }

        /// <summary>
        ///     Method which resizes the widths of star columns on positive resize of a column
        /// </summary>
        private double RecomputeStarColumnWidthsOnColumnPositiveResize(
            double horizontalChange,
            int resizingColumnIndex,
            double perStarWidth,
            bool retainAuto)
        {
            while (DoubleUtil.GreaterThan(horizontalChange, 0.0))
            {
                double minPerStarExcessRatio = Double.PositiveInfinity;
                double rightStarFactors = GetStarFactorsForPositiveResize(resizingColumnIndex + 1, out minPerStarExcessRatio);

                if (DoubleUtil.GreaterThan(rightStarFactors, 0.0))
                {
                    horizontalChange = ReallocateStarValuesForPositiveResize(
                        resizingColumnIndex,
                        horizontalChange,
                        minPerStarExcessRatio,
                        rightStarFactors,
                        perStarWidth,
                        retainAuto);

                    if (DoubleUtil.AreClose(horizontalChange, 0.0))
                    {
                        break;
                    }
                }
                else
                {
                    break;
                }
            }

            return horizontalChange;
        }

        private static bool CanColumnParticipateInResize(DataGridColumn column)
        {
            return column.IsVisible && column.CanUserResize;
        }

        /// <summary>
        ///     Method which returns the total of star factors of the columns which could be resized on positive resize of a column
        /// </summary>
        private double GetStarFactorsForPositiveResize(int startIndex, out double minPerStarExcessRatio)
        {
            minPerStarExcessRatio = Double.PositiveInfinity;
            double rightStarFactors = 0.0;
            for (int i = startIndex, count = Count; i < count; i++)
            {
                DataGridColumn column = ColumnFromDisplayIndex(i);
                if (!CanColumnParticipateInResize(column))
                {
                    continue;
                }

                DataGridLength width = column.Width;
                if (width.IsStar && !DoubleUtil.AreClose(width.Value, 0.0))
                {
                    if (DoubleUtil.GreaterThan(width.DisplayValue, column.MinWidth))
                    {
                        rightStarFactors += width.Value;
                        double excessRatio = (width.DisplayValue - column.MinWidth) / width.Value;
                        if (DoubleUtil.LessThan(excessRatio, minPerStarExcessRatio))
                        {
                            minPerStarExcessRatio = excessRatio;
                        }
                    }
                }
            }

            return rightStarFactors;
        }

        /// <summary>
        ///     Method which reallocated the star factors of star columns on
        ///     positive resize of a column
        /// </summary>
        private double ReallocateStarValuesForPositiveResize(
            int startIndex,
            double horizontalChange,
            double perStarExcessRatio,
            double totalStarFactors,
            double perStarWidth,
            bool retainAuto)
        {
            double changePerStar = 0.0;
            double horizontalChangeForIteration = 0.0;
            if (DoubleUtil.LessThan(horizontalChange, perStarExcessRatio * totalStarFactors))
            {
                changePerStar = horizontalChange / totalStarFactors;
                horizontalChangeForIteration = horizontalChange;
                horizontalChange = 0.0;
            }
            else
            {
                changePerStar = perStarExcessRatio;
                horizontalChangeForIteration = changePerStar * totalStarFactors;
                horizontalChange -= horizontalChangeForIteration;
            }

            for (int i = startIndex, count = Count; i < count; i++)
            {
                DataGridColumn column = ColumnFromDisplayIndex(i);
                DataGridLength width = column.Width;
                if (i == startIndex)
                {
                    SetResizedColumnWidth(column, horizontalChangeForIteration, retainAuto);
                }
                else if (column.Width.IsStar && CanColumnParticipateInResize(column) && DoubleUtil.GreaterThan(width.DisplayValue, column.MinWidth))
                {
                    double columnDesiredWidth = width.DisplayValue - (width.Value * changePerStar);
                    column.UpdateWidthForStarColumn(Math.Max(columnDesiredWidth, column.MinWidth), columnDesiredWidth, columnDesiredWidth / perStarWidth);
                }
            }

            return horizontalChange;
        }

        /// <summary>
        ///     Method which recomputes widths of non star columns on positive resize of a column
        /// </summary>
        private double RecomputeNonStarColumnWidthsOnColumnPositiveResize(
            double horizontalChange,
            int resizingColumnIndex,
            bool retainAuto,
            bool onlyShrinkToDesiredWidth)
        {
            if (DoubleUtil.GreaterThan(horizontalChange, 0.0))
            {
                double totalExcessWidth = 0.0;
                bool iterationNeeded = true;
                for (int i = Count - 1; iterationNeeded && i > resizingColumnIndex; i--)
                {
                    DataGridColumn column = ColumnFromDisplayIndex(i);
                    if (!CanColumnParticipateInResize(column))
                    {
                        continue;
                    }

                    DataGridLength width = column.Width;
                    double columnExcessWidth = onlyShrinkToDesiredWidth ? width.DisplayValue - Math.Max(width.DesiredValue, column.MinWidth) : width.DisplayValue - column.MinWidth;

                    if (!width.IsStar &&
                        DoubleUtil.GreaterThan(columnExcessWidth, 0.0))
                    {
                        if (DoubleUtil.GreaterThanOrClose(totalExcessWidth + columnExcessWidth, horizontalChange))
                        {
                            columnExcessWidth = horizontalChange - totalExcessWidth;
                            iterationNeeded = false;
                        }

                        column.SetWidthInternal(new DataGridLength(width.Value, width.UnitType, width.DesiredValue, width.DisplayValue - columnExcessWidth));
                        totalExcessWidth += columnExcessWidth;
                    }
                }

                if (DoubleUtil.GreaterThan(totalExcessWidth, 0.0))
                {
                    DataGridColumn column = ColumnFromDisplayIndex(resizingColumnIndex);
                    SetResizedColumnWidth(column, totalExcessWidth, retainAuto);
                    horizontalChange -= totalExcessWidth;
                }
            }

            return horizontalChange;
        }

        /// <summary>
        ///     Method which recomputes the widths of columns on negative resize of a column
        /// </summary>
        private void RecomputeColumnWidthsOnColumnNegativeResize(
            double horizontalChange,
            int resizingColumnIndex,
            bool retainAuto)
        {
            double perStarWidth = 0.0;
            if (HasVisibleStarColumnsInternal(out perStarWidth))
            {
                // increasing columns to the right which are less than the desired size
                horizontalChange = RecomputeNonStarColumnWidthsOnColumnNegativeResize(horizontalChange, resizingColumnIndex, retainAuto, false);

                // increasing star columns to the right
                horizontalChange = RecomputeStarColumnWidthsOnColumnNegativeResize(horizontalChange, resizingColumnIndex, perStarWidth, retainAuto);

                // increasing columns to the right which are less than the maximum size
                horizontalChange = RecomputeNonStarColumnWidthsOnColumnNegativeResize(horizontalChange, resizingColumnIndex, retainAuto, true);

                if (DoubleUtil.GreaterThan(horizontalChange, 0.0))
                {
                    DataGridColumn resizingColumn = ColumnFromDisplayIndex(resizingColumnIndex);
                    if (!resizingColumn.Width.IsStar)
                    {
                        SetResizedColumnWidth(resizingColumn, -horizontalChange, retainAuto);
                    }
                }
            }
            else
            {
                DataGridColumn column = ColumnFromDisplayIndex(resizingColumnIndex);
                SetResizedColumnWidth(column, -horizontalChange, retainAuto);
            }
        }

        /// <summary>
        ///     Method which recomputes widths of non star columns on negative resize of a column
        /// </summary>
        private double RecomputeNonStarColumnWidthsOnColumnNegativeResize(
            double horizontalChange,
            int resizingColumnIndex,
            bool retainAuto,
            bool expandBeyondDesiredWidth)
        {
            if (DoubleUtil.GreaterThan(horizontalChange, 0.0))
            {
                double totalLagWidth = 0.0;
                bool iterationNeeded = true;
                for (int i = resizingColumnIndex + 1, count = Count; iterationNeeded && i < count; i++)
                {
                    DataGridColumn column = ColumnFromDisplayIndex(i);
                    if (!CanColumnParticipateInResize(column))
                    {
                        continue;
                    }

                    DataGridLength width = column.Width;
                    double maxColumnResizeWidth = expandBeyondDesiredWidth ? column.MaxWidth : Math.Min(width.DesiredValue, column.MaxWidth);
                    if (!width.IsStar &&
                        DoubleUtil.LessThan(width.DisplayValue, maxColumnResizeWidth))
                    {
                        double columnLagWidth = maxColumnResizeWidth - width.DisplayValue;
                        if (DoubleUtil.GreaterThanOrClose(totalLagWidth + columnLagWidth, horizontalChange))
                        {
                            columnLagWidth = horizontalChange - totalLagWidth;
                            iterationNeeded = false;
                        }

                        column.SetWidthInternal(new DataGridLength(width.Value, width.UnitType, width.DesiredValue, width.DisplayValue + columnLagWidth));
                        totalLagWidth += columnLagWidth;
                    }
                }

                if (DoubleUtil.GreaterThan(totalLagWidth, 0.0))
                {
                    DataGridColumn column = ColumnFromDisplayIndex(resizingColumnIndex);
                    SetResizedColumnWidth(column, -totalLagWidth, retainAuto);
                    horizontalChange -= totalLagWidth;
                }
            }

            return horizontalChange;
        }

        /// <summary>
        ///     Method which recomputes widths on star columns on negative resize of a column
        /// </summary>
        private double RecomputeStarColumnWidthsOnColumnNegativeResize(
            double horizontalChange,
            int resizingColumnIndex,
            double perStarWidth,
            bool retainAuto)
        {
            while (DoubleUtil.GreaterThan(horizontalChange, 0.0))
            {
                double minPerStarLagRatio = Double.PositiveInfinity;
                double rightStarFactors = GetStarFactorsForNegativeResize(resizingColumnIndex + 1, out minPerStarLagRatio);

                if (DoubleUtil.GreaterThan(rightStarFactors, 0.0))
                {
                    horizontalChange = ReallocateStarValuesForNegativeResize(
                        resizingColumnIndex,
                        horizontalChange,
                        minPerStarLagRatio,
                        rightStarFactors,
                        perStarWidth,
                        retainAuto);

                    if (DoubleUtil.AreClose(horizontalChange, 0.0))
                    {
                        break;
                    }
                }
                else
                {
                    break;
                }
            }

            return horizontalChange;
        }

        /// <summary>
        ///     Method which returns the total star factors of columns which resize of negative resize of a column
        /// </summary>
        private double GetStarFactorsForNegativeResize(int startIndex, out double minPerStarLagRatio)
        {
            minPerStarLagRatio = Double.PositiveInfinity;
            double rightStarFactors = 0.0;
            for (int i = startIndex, count = Count; i < count; i++)
            {
                DataGridColumn column = ColumnFromDisplayIndex(i);
                if (!CanColumnParticipateInResize(column))
                {
                    continue;
                }

                DataGridLength width = column.Width;
                if (width.IsStar && !DoubleUtil.AreClose(width.Value, 0.0))
                {
                    if (DoubleUtil.LessThan(width.DisplayValue, column.MaxWidth))
                    {
                        rightStarFactors += width.Value;
                        double lagRatio = (column.MaxWidth - width.DisplayValue) / width.Value;
                        if (DoubleUtil.LessThan(lagRatio, minPerStarLagRatio))
                        {
                            minPerStarLagRatio = lagRatio;
                        }
                    }
                }
            }

            return rightStarFactors;
        }

        /// <summary>
        ///     Method which reallocates star factors of columns on negative resize of a column
        /// </summary>
        private double ReallocateStarValuesForNegativeResize(
            int startIndex,
            double horizontalChange,
            double perStarLagRatio,
            double totalStarFactors,
            double perStarWidth,
            bool retainAuto)
        {
            double changePerStar = 0.0;
            double horizontalChangeForIteration = 0.0;
            if (DoubleUtil.LessThan(horizontalChange, perStarLagRatio * totalStarFactors))
            {
                changePerStar = horizontalChange / totalStarFactors;
                horizontalChangeForIteration = horizontalChange;
                horizontalChange = 0.0;
            }
            else
            {
                changePerStar = perStarLagRatio;
                horizontalChangeForIteration = changePerStar * totalStarFactors;
                horizontalChange -= horizontalChangeForIteration;
            }

            for (int i = startIndex, count = Count; i < count; i++)
            {
                DataGridColumn column = ColumnFromDisplayIndex(i);
                DataGridLength width = column.Width;
                if (i == startIndex)
                {
                    SetResizedColumnWidth(column, -horizontalChangeForIteration, retainAuto);
                }
                else if (column.Width.IsStar && CanColumnParticipateInResize(column) && DoubleUtil.LessThan(width.DisplayValue, column.MaxWidth))
                {
                    double columnDesiredWidth = width.DisplayValue + (width.Value * changePerStar);
                    column.UpdateWidthForStarColumn(Math.Min(columnDesiredWidth, column.MaxWidth), columnDesiredWidth, columnDesiredWidth / perStarWidth);
                }
            }

            return horizontalChange;
        }

        /// <summary>
        ///     Helper method which sets the width of the column which is currently getting resized
        /// </summary>
        private static void SetResizedColumnWidth(DataGridColumn column, double widthDelta, bool retainAuto)
        {
            DataGridLength width = column.Width;
            double columnDisplayWidth = DataGridHelper.CoerceToMinMax(width.DisplayValue + widthDelta, column.MinWidth, column.MaxWidth);

            if (width.IsStar)
            {
                double starValue = width.DesiredValue / width.Value;
                column.UpdateWidthForStarColumn(columnDisplayWidth, columnDisplayWidth, columnDisplayWidth / starValue);
            }
            else if (!width.IsAbsolute && retainAuto)
            {
                column.SetWidthInternal(new DataGridLength(width.Value, width.UnitType, width.DesiredValue, columnDisplayWidth));
            }
            else
            {
                column.SetWidthInternal(new DataGridLength(columnDisplayWidth, DataGridLengthUnitType.Pixel, columnDisplayWidth, columnDisplayWidth));
            }
        }

        #endregion

        #region Width Give Away Methods

        /// <summary>
        ///     Method which tries to give away the given amount of width
        ///     among all the columns except the ignored column
        /// </summary>
        /// <param name="ignoredColumn">The column which is giving away the width</param>
        /// <param name="giveAwayWidth">The amount of giveaway width</param>
        private double GiveAwayWidthToColumns(DataGridColumn ignoredColumn, double giveAwayWidth)
        {
            return GiveAwayWidthToColumns(ignoredColumn, giveAwayWidth, false);
        }

        /// <summary>
        ///     Method which tries to give away the given amount of width
        ///     among all the columns except the ignored column
        /// </summary>
        /// <param name="ignoredColumn">The column which is giving away the width</param>
        /// <param name="giveAwayWidth">The amount of giveaway width</param>
        private double GiveAwayWidthToColumns(DataGridColumn ignoredColumn, double giveAwayWidth, bool recomputeStars)
        {
            double originalGiveAwayWidth = giveAwayWidth;
            giveAwayWidth = GiveAwayWidthToScrollViewerExcess(giveAwayWidth, /*includedInColumnsWidth*/ ignoredColumn != null);
            giveAwayWidth = GiveAwayWidthToNonStarColumns(ignoredColumn, giveAwayWidth);

            if (DoubleUtil.GreaterThan(giveAwayWidth, 0.0) || recomputeStars)
            {
                double sumOfStarDisplayWidths = 0.0;
                double sumOfStarMaxWidths = 0.0;
                bool giveAwayWidthIncluded = false;
                foreach (DataGridColumn column in this)
                {
                    DataGridLength width = column.Width;
                    if (width.IsStar && column.IsVisible)
                    {
                        if (column == ignoredColumn)
                        {
                            giveAwayWidthIncluded = true;
                        }

                        sumOfStarDisplayWidths += width.DisplayValue;
                        sumOfStarMaxWidths += column.MaxWidth;
                    }
                }

                double expectedStarSpace = sumOfStarDisplayWidths;
                if (!giveAwayWidthIncluded)
                {
                    expectedStarSpace += giveAwayWidth;
                }
                else if (!DoubleUtil.AreClose(originalGiveAwayWidth, giveAwayWidth))
                {
                    expectedStarSpace -= (originalGiveAwayWidth - giveAwayWidth);
                }

                double usedStarSpace = ComputeStarColumnWidths(Math.Min(expectedStarSpace, sumOfStarMaxWidths));
                giveAwayWidth = Math.Max(usedStarSpace - expectedStarSpace, 0.0);
            }

            return giveAwayWidth;
        }

        /// <summary>
        ///     Method which tries to give away the given amount of width
        ///     among all non star columns except the ignored column
        /// </summary>
        private double GiveAwayWidthToNonStarColumns(DataGridColumn ignoredColumn, double giveAwayWidth)
        {
            while (DoubleUtil.GreaterThan(giveAwayWidth, 0.0))
            {
                int countOfParticipatingColumns = 0;
                double minLagWidth = FindMinimumLaggingWidthOfNonStarColumns(
                    ignoredColumn,
                    out countOfParticipatingColumns);

                if (countOfParticipatingColumns == 0)
                {
                    break;
                }

                double minTotalLagWidth = minLagWidth * countOfParticipatingColumns;
                if (DoubleUtil.GreaterThanOrClose(minTotalLagWidth, giveAwayWidth))
                {
                    minLagWidth = giveAwayWidth / countOfParticipatingColumns;
                    giveAwayWidth = 0.0;
                }
                else
                {
                    giveAwayWidth -= minTotalLagWidth;
                }

                GiveAwayWidthToEveryNonStarColumn(ignoredColumn, minLagWidth);
            }

            return giveAwayWidth;
        }

        /// <summary>
        ///     Helper method which finds the minimum non-zero difference between displayvalue and desiredvalue
        ///     among all non star columns
        /// </summary>
        private double FindMinimumLaggingWidthOfNonStarColumns(
            DataGridColumn ignoredColumn,
            out int countOfParticipatingColumns)
        {
            double minLagWidth = Double.PositiveInfinity;
            countOfParticipatingColumns = 0;
            foreach (DataGridColumn column in this)
            {
                if (ignoredColumn == column ||
                    !column.IsVisible)
                {
                    continue;
                }

                DataGridLength width = column.Width;
                if (width.IsStar)
                {
                    continue;
                }

                double columnMaxWidth = column.MaxWidth;
                if (DoubleUtil.LessThan(width.DisplayValue, width.DesiredValue) &&
                    !DoubleUtil.AreClose(width.DisplayValue, columnMaxWidth))
                {
                    countOfParticipatingColumns++;
                    double lagWidth = Math.Min(width.DesiredValue, columnMaxWidth) - width.DisplayValue;
                    if (DoubleUtil.LessThan(lagWidth, minLagWidth))
                    {
                        minLagWidth = lagWidth;
                    }
                }
            }

            return minLagWidth;
        }

        /// <summary>
        ///     Helper method which gives away the given amount of width to
        ///     every non star column whose display value is less than its desired value
        /// </summary>
        private void GiveAwayWidthToEveryNonStarColumn(DataGridColumn ignoredColumn, double perColumnGiveAwayWidth)
        {
            foreach (DataGridColumn column in this)
            {
                if (ignoredColumn == column ||
                    !column.IsVisible)
                {
                    continue;
                }

                DataGridLength width = column.Width;
                if (width.IsStar)
                {
                    continue;
                }

                if (DoubleUtil.LessThan(width.DisplayValue, Math.Min(width.DesiredValue, column.MaxWidth)))
                {
                    column.SetWidthInternal(new DataGridLength(width.Value, width.UnitType, width.DesiredValue, width.DisplayValue + perColumnGiveAwayWidth));
                }
            }
        }

        /// <summary>
        ///     Helper method which gives away width to scroll viewer
        ///     if its extent width is greater than viewport width
        /// </summary>
        private double GiveAwayWidthToScrollViewerExcess(double giveAwayWidth, bool includedInColumnsWidth)
        {
            double totalSpace = DataGridOwner.GetViewportWidthForColumns();
            double usedSpace = 0.0;
            foreach (DataGridColumn column in this)
            {
                if (column.IsVisible)
                {
                    usedSpace += column.Width.DisplayValue;
                }
            }

            if (includedInColumnsWidth)
            {
                if (DoubleUtil.GreaterThan(usedSpace, totalSpace))
                {
                    double contributingSpace = usedSpace - totalSpace;
                    giveAwayWidth -= Math.Min(contributingSpace, giveAwayWidth);
                }
            }
            else
            {
                // If the giveAwayWidth is not included in columns, then the new
                // giveAwayWidth should be derived the total available space and used space
                giveAwayWidth = Math.Min(giveAwayWidth, Math.Max(0d, totalSpace - usedSpace));
            }

            return giveAwayWidth;
        }

        #endregion

        #region Width Take Away Methods

        /// <summary>
        ///     Method which tries to get the unused column space when another column tries to positive resize
        /// </summary>
        private double TakeAwayUnusedSpaceOnColumnPositiveResize(double horizontalChange, int resizingColumnIndex, bool retainAuto)
        {
            double spaceNeeded = TakeAwayWidthFromUnusedSpace(false, horizontalChange);
            if (DoubleUtil.LessThan(spaceNeeded, horizontalChange))
            {
                DataGridColumn resizingColumn = ColumnFromDisplayIndex(resizingColumnIndex);
                SetResizedColumnWidth(resizingColumn, horizontalChange - spaceNeeded, retainAuto);
            }

            return spaceNeeded;
        }

        /// <summary>
        ///     Helper method which tries to take away width from unused space
        /// </summary>
        private double TakeAwayWidthFromUnusedSpace(bool spaceAlreadyUtilized, double takeAwayWidth, double totalAvailableWidth)
        {
            double usedSpace = 0.0;
            foreach (DataGridColumn column in this)
            {
                if (column.IsVisible)
                {
                    usedSpace += column.Width.DisplayValue;
                }
            }

            if (spaceAlreadyUtilized)
            {
                if (DoubleUtil.GreaterThanOrClose(totalAvailableWidth, usedSpace))
                {
                    return 0.0;
                }
                else
                {
                    return Math.Min(usedSpace - totalAvailableWidth, takeAwayWidth);
                }
            }
            else
            {
                double unusedSpace = totalAvailableWidth - usedSpace;
                if (DoubleUtil.GreaterThan(unusedSpace, 0.0))
                {
                    takeAwayWidth = Math.Max(0.0, takeAwayWidth - unusedSpace);
                }

                return takeAwayWidth;
            }
        }

        /// <summary>
        ///     Helper method which tries to take away width from unused space
        /// </summary>
        private double TakeAwayWidthFromUnusedSpace(bool spaceAlreadyUtilized, double takeAwayWidth)
        {
            double totalAvailableWidth = DataGridOwner.GetViewportWidthForColumns();
            if (DoubleUtil.GreaterThan(totalAvailableWidth, 0.0))
            {
                return TakeAwayWidthFromUnusedSpace(spaceAlreadyUtilized, takeAwayWidth, totalAvailableWidth);
            }

            return takeAwayWidth;
        }

        /// <summary>
        ///     Method which tries to take away the given amount of width from columns
        ///     except the ignored column
        /// </summary>
        private double TakeAwayWidthFromColumns(DataGridColumn ignoredColumn, double takeAwayWidth, bool widthAlreadyUtilized)
        {
            double totalAvailableWidth = DataGridOwner.GetViewportWidthForColumns();
            return TakeAwayWidthFromColumns(ignoredColumn, takeAwayWidth, widthAlreadyUtilized, totalAvailableWidth);
        }

        /// <summary>
        ///     Method which tries to take away the given amount of width from columns
        ///     except the ignored column
        /// </summary>
        private double TakeAwayWidthFromColumns(DataGridColumn ignoredColumn, double takeAwayWidth, bool widthAlreadyUtilized, double totalAvailableWidth)
        {
            takeAwayWidth = TakeAwayWidthFromUnusedSpace(widthAlreadyUtilized, takeAwayWidth, totalAvailableWidth);

            takeAwayWidth = TakeAwayWidthFromStarColumns(ignoredColumn, takeAwayWidth);

            takeAwayWidth = TakeAwayWidthFromNonStarColumns(ignoredColumn, takeAwayWidth);
            return takeAwayWidth;
        }

        /// <summary>
        ///     Method which tries to take away the given amount of width form
        ///     the star columns
        /// </summary>
        private double TakeAwayWidthFromStarColumns(DataGridColumn ignoredColumn, double takeAwayWidth)
        {
            if (DoubleUtil.GreaterThan(takeAwayWidth, 0.0))
            {
                double sumOfStarDisplayWidths = 0.0;
                double sumOfStarMinWidths = 0.0;
                foreach (DataGridColumn column in this)
                {
                    DataGridLength width = column.Width;
                    if (width.IsStar && column.IsVisible)
                    {
                        if (column == ignoredColumn)
                        {
                            sumOfStarDisplayWidths += takeAwayWidth;
                        }

                        sumOfStarDisplayWidths += width.DisplayValue;
                        sumOfStarMinWidths += column.MinWidth;
                    }
                }

                double expectedStarSpace = sumOfStarDisplayWidths - takeAwayWidth;
                double usedStarSpace = ComputeStarColumnWidths(Math.Max(expectedStarSpace, sumOfStarMinWidths));
                takeAwayWidth = Math.Max(usedStarSpace - expectedStarSpace, 0.0);
            }

            return takeAwayWidth;
        }

        /// <summary>
        ///     Method which tries to take away the given amount of width
        ///     among all non star columns except the ignored column
        /// </summary>
        private double TakeAwayWidthFromNonStarColumns(DataGridColumn ignoredColumn, double takeAwayWidth)
        {
            while (DoubleUtil.GreaterThan(takeAwayWidth, 0.0))
            {
                int countOfParticipatingColumns = 0;
                double minExcessWidth = FindMinimumExcessWidthOfNonStarColumns(
                    ignoredColumn,
                    out countOfParticipatingColumns);

                if (countOfParticipatingColumns == 0)
                {
                    break;
                }

                double minTotalExcessWidth = minExcessWidth * countOfParticipatingColumns;
                if (DoubleUtil.GreaterThanOrClose(minTotalExcessWidth, takeAwayWidth))
                {
                    minExcessWidth = takeAwayWidth / countOfParticipatingColumns;
                    takeAwayWidth = 0.0;
                }
                else
                {
                    takeAwayWidth -= minTotalExcessWidth;
                }

                TakeAwayWidthFromEveryNonStarColumn(ignoredColumn, minExcessWidth);
            }

            return takeAwayWidth;
        }

        /// <summary>
        ///     Helper method which finds the minimum non-zero difference between displayvalue and minwidth
        ///     among all non star columns
        /// </summary>
        private double FindMinimumExcessWidthOfNonStarColumns(
            DataGridColumn ignoredColumn,
            out int countOfParticipatingColumns)
        {
            double minExcessWidth = Double.PositiveInfinity;
            countOfParticipatingColumns = 0;
            foreach (DataGridColumn column in this)
            {
                if (ignoredColumn == column ||
                    !column.IsVisible)
                {
                    continue;
                }

                DataGridLength width = column.Width;
                if (width.IsStar)
                {
                    continue;
                }

                double minWidth = column.MinWidth;
                if (DoubleUtil.GreaterThan(width.DisplayValue, minWidth))
                {
                    countOfParticipatingColumns++;
                    double excessWidth = width.DisplayValue - minWidth;
                    if (DoubleUtil.LessThan(excessWidth, minExcessWidth))
                    {
                        minExcessWidth = excessWidth;
                    }
                }
            }

            return minExcessWidth;
        }

        /// <summary>
        ///     Helper method which takes away the given amount of width from
        ///     every non star column whose display value is greater than its minwidth
        /// </summary>
        private void TakeAwayWidthFromEveryNonStarColumn(
            DataGridColumn ignoredColumn,
            double perColumnTakeAwayWidth)
        {
            foreach (DataGridColumn column in this)
            {
                if (ignoredColumn == column ||
                    !column.IsVisible)
                {
                    continue;
                }

                DataGridLength width = column.Width;
                if (width.IsStar)
                {
                    continue;
                }

                if (DoubleUtil.GreaterThan(width.DisplayValue, column.MinWidth))
                {
                    column.SetWidthInternal(new DataGridLength(width.Value, width.UnitType, width.DesiredValue, width.DisplayValue - perColumnTakeAwayWidth));
                }
            }
        }

        #endregion

        #region Column Virtualization

        /// <summary>
        ///     Property which indicates that the RealizedColumnsBlockList
        ///     is dirty and needs to be rebuilt for non-column virtualized rows
        /// </summary>
        internal bool RebuildRealizedColumnsBlockListForNonVirtualizedRows
        {
            get; set;
        }

        /// <summary>
        ///     List of realized column index blocks for non-column virtualized rows
        /// </summary>
        internal List<RealizedColumnsBlock> RealizedColumnsBlockListForNonVirtualizedRows
        {
            get
            {
                return _realizedColumnsBlockListForNonVirtualizedRows;
            }

            set
            {
                _realizedColumnsBlockListForNonVirtualizedRows = value;

                // Notify other rows and column header row to
                // remeasure their child panel's in order to be
                // in sync with latest column realization computations
                DataGrid dataGrid = DataGridOwner;
                dataGrid.NotifyPropertyChanged(
                    dataGrid,
                    "RealizedColumnsBlockListForNonVirtualizedRows",
                    new DependencyPropertyChangedEventArgs(),
                    DataGridNotificationTarget.CellsPresenter | DataGridNotificationTarget.ColumnHeadersPresenter);
            }
        }

        /// <summary>
        ///     List of realized column display index blocks for non-column virtualized rows
        /// </summary>
        internal List<RealizedColumnsBlock> RealizedColumnsDisplayIndexBlockListForNonVirtualizedRows
        {
            get; set;
        }

        /// <summary>
        ///     Property which indicates that the RealizedColumnsBlockList
        ///     is dirty and needs to be rebuilt for column virtualized rows
        /// </summary>
        internal bool RebuildRealizedColumnsBlockListForVirtualizedRows
        {
            get; set;
        }

        /// <summary>
        ///     List of realized column index blocks for column virtualized rows
        /// </summary>
        internal List<RealizedColumnsBlock> RealizedColumnsBlockListForVirtualizedRows
        {
            get
            {
                return _realizedColumnsBlockListForVirtualizedRows;
            }

            set
            {
                _realizedColumnsBlockListForVirtualizedRows = value;

                // Notify other rows and column header row to
                // remeasure their child panel's in order to be
                // in sync with latest column realization computations
                DataGrid dataGrid = DataGridOwner;
                dataGrid.NotifyPropertyChanged(
                    dataGrid,
                    "RealizedColumnsBlockListForVirtualizedRows",
                    new DependencyPropertyChangedEventArgs(),
                    DataGridNotificationTarget.CellsPresenter | DataGridNotificationTarget.ColumnHeadersPresenter);
            }
        }

        /// <summary>
        ///     List of realized column display index blocks for column virtualized rows
        /// </summary>
        internal List<RealizedColumnsBlock> RealizedColumnsDisplayIndexBlockListForVirtualizedRows
        {
            get; set;
        }

        /// <summary>
        ///     Called when properties which affect the realized columns namely
        ///     Column Width, FrozenColumnCount, DisplayIndex etc. are changed.
        /// </summary>
        internal void InvalidateColumnRealization(bool invalidateForNonVirtualizedRows)
        {
            RebuildRealizedColumnsBlockListForVirtualizedRows = true;
            if (invalidateForNonVirtualizedRows)
            {
                RebuildRealizedColumnsBlockListForNonVirtualizedRows = true;
            }
        }

        #endregion

        #region Hidden Columns

        /// <summary>
        ///     Helper property to return the display index of first visible column
        /// </summary>
        internal int FirstVisibleDisplayIndex
        {
            get
            {
                for (int i = 0, count = this.Count; i < count; i++)
                {
                    DataGridColumn column = ColumnFromDisplayIndex(i);
                    if (column.IsVisible)
                    {
                        return i;
                    }
                }

                return -1;
            }
        }

        /// <summary>
        ///     Helper property to return the display index of last visible column
        /// </summary>
        internal int LastVisibleDisplayIndex
        {
            get
            {
                for (int i = this.Count - 1; i >= 0; i--)
                {
                    DataGridColumn column = ColumnFromDisplayIndex(i);
                    if (column.IsVisible)
                    {
                        return i;
                    }
                }

                return -1;
            }
        }

        /// <summary>
        ///     Flag set when an operation has invalidated auto-width columns so they are no longer expected to be their desired width.
        /// </summary>
        internal bool RefreshAutoWidthColumns { get; set; }

        #endregion

        #region Data

        private DataGrid _dataGridOwner;
        private bool _isUpdatingDisplayIndex;     // true if we're in the middle of updating the display index of each column.
        private List<int> _displayIndexMap;            // maps a DisplayIndex to an index in the _columns collection.
        private bool _displayIndexMapInitialized; // Flag is used to delay the validation of DisplayIndex until the first measure
        private bool _isClearingDisplayIndex; // Flag indicating that we're currently clearing the display index.  We should not coerce default display index's during this time.
        private bool _columnWidthsComputationPending; // Flag indicating whether the columns width computaion operation is pending
        private Dictionary<DataGridColumn, DataGridLength> _originalWidthsForResize; // Dictionary to hold the original widths of columns for resize operation
        private double? _averageColumnWidth = null;       // average width of all visible columns
        private List<RealizedColumnsBlock> _realizedColumnsBlockListForNonVirtualizedRows = null; // Realized columns for non-virtualized rows
        private List<RealizedColumnsBlock> _realizedColumnsBlockListForVirtualizedRows = null; // Realized columns for virtualized rows
        private bool _hasVisibleStarColumns = false;

        #endregion
    }
}
