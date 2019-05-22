// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


using System;

namespace System.Windows.Controls
{
    /// <summary>
    ///     EventArgs used for events related to DataGridColumn.
    /// </summary>
    public class DataGridColumnEventArgs : EventArgs
    {
        /// <summary>
        ///     Instantiates a new instance of this class.
        /// </summary>
        public DataGridColumnEventArgs(DataGridColumn column)
        {
            _column = column;
        }

        /// <summary>
        ///     DataGridColumn that the DataGridColumnEventArgs refers to
        /// </summary>
        public DataGridColumn Column
        {
            get { return _column; }
        }

        private DataGridColumn _column;
    }
}
