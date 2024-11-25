// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


using System;
using System.Diagnostics;
using System.Windows;

namespace System.Windows.Controls
{
    /// <summary>
    ///     Defines a structure that identifies a specific cell based on row and column data.
    /// </summary>
    public struct DataGridCellInfo
    {
        /// <summary>
        ///     Identifies a cell at the column within the row for the specified item.
        /// </summary>
        /// <param name="item">The item who's row contains the cell.</param>
        /// <param name="column">The column of the cell within the row.</param>
        /// <remarks>
        ///     This constructor will not tie the DataGridCellInfo to any particular
        ///     DataGrid.
        /// </remarks>
        public DataGridCellInfo(object item, DataGridColumn column)
        {
            ArgumentNullException.ThrowIfNull(column);

            _info = new ItemsControl.ItemInfo(item);
            _column = column;
            _owner = null;
        }

        /// <summary>
        ///     Creates a structure that identifies the specific cell container.
        /// </summary>
        /// <param name="cell">
        ///     A reference to a cell.
        ///     This structure does not maintain a strong reference to the cell.
        ///     Changes to the cell will not affect this structure.
        /// </param>
        /// <remarks>
        ///     This constructor will tie the DataGridCellInfo to the specific
        ///     DataGrid that owns the cell container.
        /// </remarks>
        public DataGridCellInfo(DataGridCell cell)
        {
            ArgumentNullException.ThrowIfNull(cell);

            DataGrid owner = cell.DataGridOwner;
            _info = owner.NewItemInfo(cell.RowDataItem, cell.RowOwner);
            _column = cell.Column;
            _owner = new WeakReference(owner);
        }

        internal DataGridCellInfo(object item, DataGridColumn column, DataGrid owner)
        {
            Debug.Assert(item is not null, "item should not be null.");
            Debug.Assert(column is not null, "column should not be null.");
            Debug.Assert(owner is not null, "owner should not be null.");

            _info = owner.NewItemInfo(item);
            _column = column;
            _owner = new WeakReference(owner);
        }

        internal DataGridCellInfo(ItemsControl.ItemInfo info, DataGridColumn column, DataGrid owner)
        {
            Debug.Assert(info is not null, "item should not be null.");
            Debug.Assert(column is not null, "column should not be null.");
            Debug.Assert(owner is not null, "owner should not be null.");

            _info = info;
            _column = column;
            _owner = new WeakReference(owner);
        }

        /// <summary>
        ///     Used to create an unset DataGridCellInfo.
        /// </summary>
        internal DataGridCellInfo(object item)
        {
            Debug.Assert(item == DependencyProperty.UnsetValue, "This should only be used to make an Unset CellInfo.");
            _info = new ItemsControl.ItemInfo(item);
            _column = null;
            _owner = null;
        }

        /// <summary>
        ///     Used to create a copy of an existing DataGridCellInfo, with its own
        ///     ItemInfo (to avoid aliasing).
        /// </summary>
        internal DataGridCellInfo(DataGridCellInfo info)
        {
            _info = info._info.Clone();
            _column = info._column;
            _owner = info._owner;
        }

        /// <summary>
        ///     This is used strictly to create the partial CellInfos.
        /// </summary>
        /// <remarks>
        ///     This is being kept private so that it is explicit that the
        ///     caller expects invalid data.
        /// </remarks>
        private DataGridCellInfo(DataGrid owner, DataGridColumn column, object item)
        {
            Debug.Assert(owner is not null, "owner should not be null.");

            _info = owner.NewItemInfo(item);
            _column = column;
            _owner = new WeakReference(owner);
        }

        /// <summary>
        ///     This is used by CurrentCell if there isn't a valid CurrentItem or CurrentColumn.
        /// </summary>
        internal static DataGridCellInfo CreatePossiblyPartialCellInfo(object item, DataGridColumn column, DataGrid owner)
        {
            Debug.Assert(owner is not null, "owner should not be null.");

            if ((item is null) && (column is null))
            {
                return Unset;
            }
            else
            {
                return new DataGridCellInfo(owner, column, (item is null) ? DependencyProperty.UnsetValue : item);
            }
        }

        /// <summary>
        ///     The item whose row contains the cell.
        /// </summary>
        public object Item
        {
            get { return (_info is not null) ? _info.Item : null; }
        }

        /// <summary>
        ///     The column of the cell within the row.
        /// </summary>
        public DataGridColumn Column
        {
            get { return _column; }
        }

        /// <summary>
        ///     Whether the two objects are equal.
        /// </summary>
        public override bool Equals(object obj)
        {
            if (obj is DataGridCellInfo)
            {
                return EqualsImpl((DataGridCellInfo)obj);
            }

            return false;
        }

        /// <summary>
        ///     Returns whether the two values are equal.
        /// </summary>
        public static bool operator ==(DataGridCellInfo cell1, DataGridCellInfo cell2)
        {
            return cell1.EqualsImpl(cell2);
        }

        /// <summary>
        ///     Returns whether the two values are not equal.
        /// </summary>
        public static bool operator !=(DataGridCellInfo cell1, DataGridCellInfo cell2)
        {
            return !cell1.EqualsImpl(cell2);
        }

        internal bool EqualsImpl(DataGridCellInfo cell)
        {
            return (cell._column == _column) && (cell.Owner == Owner) && (cell._info == _info);
        }

        /// <summary>
        ///     Returns a hash code for the structure.
        /// </summary>
        public override int GetHashCode()
        {
            return ((_info is null) ? 0 : _info.GetHashCode()) ^
                   ((_column is null) ? 0 : _column.GetHashCode());
        }

        /// <summary>
        ///     Whether the structure holds valid information.
        /// </summary>
        public bool IsValid
        {
            get
            {
                return ArePropertyValuesValid
#if STRICT_VALIDATION
                    && ColumnInDataGrid && ItemInDataGrid
#endif
                    ;
            }
        }

        internal bool IsSet
        {
            get { return _column is not null || _info.Item != DependencyProperty.UnsetValue; }
        }

        internal ItemsControl.ItemInfo ItemInfo
        {
            get { return _info; }
        }

        /// <summary>
        ///     Assumes that if the owner matches, then the column and item fields are valid.
        /// </summary>
        internal bool IsValidForDataGrid(DataGrid dataGrid)
        {
            DataGrid owner = Owner;
            return (ArePropertyValuesValid && (owner == dataGrid)) || (owner is null);
        }

        private bool ArePropertyValuesValid
        {
            get { return (Item != DependencyProperty.UnsetValue) && (_column is not null); }
        }

#if STRICT_VALIDATION
        private bool ColumnInDataGrid
        {
            get
            {
                DataGrid owner = Owner;
                if (owner is not null)
                {
                    return owner.Columns.Contains(_column);
                }

                return true;
            }
        }

        private bool ItemInDataGrid
        {
            get
            {
                DataGrid owner = Owner;
                if (owner is not null)
                {
                    owner.Items.Contains(Item);
                }

                return true;
            }
        }
#endif

        /// <summary>
        ///     Used for default values.
        /// </summary>
        internal static DataGridCellInfo Unset
        {
            get { return new DataGridCellInfo(DependencyProperty.UnsetValue); }
        }

        private DataGrid Owner
        {
            get
            {
                if (_owner is not null)
                {
                    return (DataGrid)_owner.Target;
                }

                return null;
            }
        }

        private ItemsControl.ItemInfo _info;
        private DataGridColumn _column;
        private WeakReference _owner;
    }
}
