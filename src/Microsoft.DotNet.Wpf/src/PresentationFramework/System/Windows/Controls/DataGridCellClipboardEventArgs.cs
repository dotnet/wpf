// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

﻿
using System;
using System.Collections.Generic;
using System.Text;

namespace System.Windows.Controls
{
    /// <summary>
    /// This class encapsulates a cell information necessary for CopyingCellClipboardContent and PastingCellClipboardContent events
    /// </summary>
    public class DataGridCellClipboardEventArgs : EventArgs
    {
        /// <summary>
        /// Construct DataGridCellClipboardEventArgs object
        /// </summary>
        /// <param name="item"></param>
        /// <param name="column"></param>
        /// <param name="content"></param>
        public DataGridCellClipboardEventArgs(object item, DataGridColumn column, object content)
        {
            _item = item;
            _column = column;
            _content = content;
        }

        /// <summary>
        /// Content of the cell to be set or get from clipboard
        /// </summary>
        public object Content
        {
            get { return _content; }
            set { _content = value; }
        }

        /// <summary>
        /// DataGrid row item containing the cell
        /// </summary>
        public object Item
        {
            get { return _item; }
        }

        /// <summary>
        /// DataGridColumn containing the cell
        /// </summary>
        public DataGridColumn Column
        {
            get { return _column; }
        }

        private object _content;
        private object _item;
        private DataGridColumn _column;
    }
}
