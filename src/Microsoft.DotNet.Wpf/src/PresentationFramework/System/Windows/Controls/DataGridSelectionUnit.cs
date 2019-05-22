// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


using System;

namespace System.Windows.Controls
{
    /// <summary>
    ///     The accepted selection units used in selection on a DataGrid.
    /// </summary>
    public enum DataGridSelectionUnit
    {
        /// <summary>
        ///     Only cells are selectable.
        ///     Clicking on a cell will select the cell.
        ///     Clicking on row or column headers does nothing.
        /// </summary>
        Cell,

        /// <summary>
        ///     Only full rows are selectable.
        ///     Clicking on row headers or on cells will select the whole row.
        /// </summary>
        FullRow,

        /// <summary>
        ///     Cells and rows are selectable.
        ///     Clicking on a cell will select the cell. Selecting all cells in the row will not select the row.
        ///     Clicking on a row header will select the row and all cells in the row.
        /// </summary>
        CellOrRowHeader
    }
}
