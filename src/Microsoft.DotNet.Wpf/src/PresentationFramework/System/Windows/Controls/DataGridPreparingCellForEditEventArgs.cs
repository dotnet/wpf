// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


using System;
using System.Windows;
using System.Windows.Input;

namespace System.Windows.Controls
{
    /// <summary>
    ///     Provides information about a cell that has just entered edit mode.
    /// </summary>
    public class DataGridPreparingCellForEditEventArgs : EventArgs
    {
        /// <summary>
        ///     Constructs a new instance of these event arguments.
        /// </summary>
        /// <param name="column">The column of the cell that just entered edit mode.</param>
        /// <param name="row">The row container that contains the cell container that just entered edit mode.</param>
        /// <param name="editingEventArgs">The event arguments, if any, that led to the cell being placed in edit mode.</param>
        /// <param name="cell">The cell container that just entered edit mode.</param>
        /// <param name="editingElement">The editing element within the cell container.</param>
        public DataGridPreparingCellForEditEventArgs(DataGridColumn column, DataGridRow row, RoutedEventArgs editingEventArgs, FrameworkElement editingElement)
        {
            _dataGridColumn = column;
            _dataGridRow = row;
            _editingEventArgs = editingEventArgs;
            _editingElement = editingElement;
        }

        /// <summary>
        ///     The column of the cell that just entered edit mode.
        /// </summary>
        public DataGridColumn Column
        {
            get { return _dataGridColumn; }
        }

        /// <summary>
        ///     The row container that contains the cell container that just entered edit mode.
        /// </summary>
        public DataGridRow Row
        {
            get { return _dataGridRow; }
        }

        /// <summary>
        ///     The event arguments, if any, that led to the cell being placed in edit mode.
        /// </summary>
        public RoutedEventArgs EditingEventArgs
        {
            get { return _editingEventArgs; }
        }

        /// <summary>
        ///     The editing element within the cell container.
        /// </summary>
        public FrameworkElement EditingElement
        {
            get { return _editingElement; }
        }

        private DataGridColumn _dataGridColumn;
        private DataGridRow _dataGridRow;
        private RoutedEventArgs _editingEventArgs;
        private FrameworkElement _editingElement;
    }
}