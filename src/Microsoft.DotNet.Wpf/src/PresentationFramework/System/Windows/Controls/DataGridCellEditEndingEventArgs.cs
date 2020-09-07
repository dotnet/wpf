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
    ///     Provides information just before a cell exits edit mode.
    /// </summary>
    public class DataGridCellEditEndingEventArgs : EventArgs
    {
        /// <summary>
        ///     Instantiates a new instance of this class.
        /// </summary>
        /// <param name="column">The column of the cell that is about to exit edit mode.</param>
        /// <param name="row">The row container of the cell container that is about to exit edit mode.</param>
        /// <param name="editingElement">The editing element within the cell.</param>
        /// <param name="editingUnit">The editing unit that is about to leave edit mode.</param>
        public DataGridCellEditEndingEventArgs(DataGridColumn column, DataGridRow row, FrameworkElement editingElement, DataGridEditAction editAction)
        {
            _dataGridColumn = column;
            _dataGridRow = row;
            _editingElement = editingElement;
            _editAction = editAction;
        }

        /// <summary>
        ///     When true, prevents the cell from exiting edit mode.
        /// </summary>
        public bool Cancel
        {
            get { return _cancel; }
            set { _cancel = value; }
        }

        /// <summary>
        ///     The column of the cell that is about to exit edit mode.
        /// </summary>
        public DataGridColumn Column
        {
            get { return _dataGridColumn; }
        }

        /// <summary>
        ///     The row container of the cell container that is about to exit edit mode.
        /// </summary>
        public DataGridRow Row
        {
            get { return _dataGridRow; }
        }

        /// <summary>
        ///     The editing element within the cell. 
        /// </summary>
        public FrameworkElement EditingElement
        {
            get { return _editingElement; }
        }

        /// <summary>
        ///     The edit action when leave edit mode.
        /// </summary>
        public DataGridEditAction EditAction
        {
            get { return _editAction; }
        }

        private bool _cancel;
        private DataGridColumn _dataGridColumn;
        private DataGridRow _dataGridRow;
        private FrameworkElement _editingElement;
        private DataGridEditAction _editAction;
    }
}