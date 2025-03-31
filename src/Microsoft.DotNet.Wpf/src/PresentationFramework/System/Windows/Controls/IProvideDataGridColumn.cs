// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Windows.Controls
{
    /// <summary>
    ///     Interface to abstract away the difference between a DataGridCell and a DataGridColumnHeader
    /// </summary>
    internal interface IProvideDataGridColumn
    {
        DataGridColumn Column
        {
            get;
        }
    }
}