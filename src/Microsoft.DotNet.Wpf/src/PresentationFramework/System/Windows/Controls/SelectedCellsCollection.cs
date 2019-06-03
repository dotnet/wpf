// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;

namespace System.Windows.Controls
{
    /// <summary>
    ///     A collection that optimizes the storage of DataGridCellInfo.
    /// </summary>
    /// <remarks>
    ///     The collection is exposed through the DataGrid.SelectedCells property as
    ///     a generic IList.
    ///     
    ///     The collection maintains a list of DataGridCellInfo so that users of the
    ///     SelectedCells property can interact with it like a normal list.
    ///     
    ///     The collection maintains a dictionary mapping rows to columns and 
    ///     a dictionary that maps columns to rows. This allows quick retrieval
    ///     of all selected cells in a particular row or column. These are
    ///     operations that occur when select/deselecting a row or column.
    ///     
    ///     The collection implements all the parts of INotifyCollectionChanged so
    ///     that the DataGrid can be notified of changes, but does not expose the
    ///     interface so that SelectedCells can't be cast to it. This was to
    ///     reduce the test coverage and the undiscoverability of the interface.
    /// </remarks>
    internal sealed class SelectedCellsCollection : VirtualizedCellInfoCollection
    {
        #region Construction

        internal SelectedCellsCollection(DataGrid owner) : base(owner)
        {
        }

        #endregion

        #region DataGrid API

        /// <summary>
        ///     Calculates the bounding box of the cells.
        /// </summary>
        /// <returns>true if not empty, false if empty.</returns>
        internal bool GetSelectionRange(out int minColumnDisplayIndex, out int maxColumnDisplayIndex, out int minRowIndex, out int maxRowIndex) 
        {
            if (IsEmpty)
            {
                minColumnDisplayIndex = -1;
                maxColumnDisplayIndex = -1;
                minRowIndex = -1;
                maxRowIndex = -1;
                return false;
            }
            else
            {
                GetBoundingRegion(out minColumnDisplayIndex, out minRowIndex, out maxColumnDisplayIndex, out maxRowIndex);
                return true;
            }
        }

        #endregion

        #region Collection Changed Notification

        /// <summary>
        ///     Notify the owning DataGrid of changes to this collection.
        /// </summary>
        protected override void OnCollectionChanged(NotifyCollectionChangedAction action, VirtualizedCellInfoCollection oldItems, VirtualizedCellInfoCollection newItems)
        {
            Owner.OnSelectedCellsChanged(action, oldItems, newItems);
        }

        #endregion
    }
}