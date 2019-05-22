// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

﻿
using System;
using System.Windows;
using System.Windows.Input;

namespace System.Windows.Controls
{
    /// <summary>
    ///     Provides information just before a row exits edit mode.
    /// </summary>
    public class DataGridRowEditEndingEventArgs : EventArgs
    {
        /// <summary>
        ///     Instantiates a new instance of this class.
        /// </summary>
        /// <param name="row">The row container of the cell container that is about to exit edit mode.</param>
        /// <param name="editingUnit">The editing unit that is about to leave edit mode.</param>
        public DataGridRowEditEndingEventArgs(DataGridRow row, DataGridEditAction editAction)
        {
            _dataGridRow = row;
            _editAction = editAction;
        }

        /// <summary>
        ///     When true, prevents the row from exiting edit mode.
        /// </summary>
        public bool Cancel
        {
            get { return _cancel; }
            set { _cancel = value; }
        }

        /// <summary>
        ///     The row container of the cell container that is about to exit edit mode.
        /// </summary>
        public DataGridRow Row
        {
            get { return _dataGridRow; }
        }

        /// <summary>
        ///     The edit action when leave edit mode.
        /// </summary>
        public DataGridEditAction EditAction
        {
            get { return _editAction; }
        }

        private bool _cancel;
        private DataGridRow _dataGridRow;
        private DataGridEditAction _editAction;
    }
}