// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


using System;

namespace System.Windows.Controls
{
    /// <summary>
    /// Enum used to specify where we want an internal property change notification to be routed.
    /// </summary>
    [Flags]
    internal enum DataGridNotificationTarget
    {
        None                   = 0x00, // this means don't send it on; likely handle it on the same object that raised the event.
        Cells                  = 0x01,
        CellsPresenter         = 0x02,
        Columns                = 0x04,
        ColumnCollection       = 0x08,
        ColumnHeaders          = 0x10,
        ColumnHeadersPresenter = 0x20,
        DataGrid               = 0x40,
        DetailsPresenter       = 0x80,
        RefreshCellContent     = 0x100,
        RowHeaders             = 0x200,
        Rows                   = 0x400,
        All                    = 0xFFF,
    }
}