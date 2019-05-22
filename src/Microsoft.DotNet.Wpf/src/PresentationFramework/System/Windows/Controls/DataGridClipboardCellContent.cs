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
    /// This structure encapsulate the cell information necessary when clipboard content is prepared
    /// </summary>
    public struct DataGridClipboardCellContent
    {
        /// <summary>
        /// Creates a new DataGridClipboardCellValue structure containing information about DataGrid cell
        /// </summary>
        /// <param name="row">DataGrid row item containing the cell</param>
        /// <param name="column">DataGridColumn containing the cell</param>
        /// <param name="value">DataGrid cell value</param>
        public DataGridClipboardCellContent(object item, DataGridColumn column, object content)
        {
            _item = item;
            _column = column;
            _content = content;
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

        /// <summary>
        /// Cell content
        /// </summary>
        public object Content
        {
            get { return _content; }
        }

        /// <summary>
        /// Field-by-field comparison to avoid reflection-based ValueType.Equals 
        /// </summary>
        /// <param name="data"/>
        /// <returns>True iff this and data are equal</returns>
        public override bool Equals(object data)
        {
            DataGridClipboardCellContent clipboardCellContent;
            if (data is DataGridClipboardCellContent)
            {
                clipboardCellContent = (DataGridClipboardCellContent)data;
                            
                return 
                    (_column == clipboardCellContent._column) &&
                    (_content == clipboardCellContent._content) &&
                    (_item == clipboardCellContent._item);
            }

            return false;
        }

        /// <summary>
        /// Return a deterministic hash code
        /// </summary>
        /// <returns>Hash value</returns>
        public override int GetHashCode()
        {
            return ((_column == null ? 0 : _column.GetHashCode()) ^
                (_content == null ? 0 : _content.GetHashCode()) ^
                (_item == null ? 0 : _item.GetHashCode()));
        } 

        /// <summary>
        /// Field-by-field comparison to avoid reflection-based ValueType.Equals 
        /// </summary>
        /// <param name="clipboardCellContent1"/>
        /// <param name="clipboardCellContent2"/>
        /// <returns>True iff clipboardCellContent1 and clipboardCellContent2 are equal</returns>
        public static bool operator ==(
            DataGridClipboardCellContent clipboardCellContent1,
            DataGridClipboardCellContent clipboardCellContent2)
        {
            return 
                (clipboardCellContent1._column == clipboardCellContent2._column) &&
                (clipboardCellContent1._content == clipboardCellContent2._content) &&
                (clipboardCellContent1._item == clipboardCellContent2._item);
        }

        /// <summary>
        /// Field-by-field comparison to avoid reflection-based ValueType.Equals 
        /// </summary>
        /// <param name="clipboardCellContent1"/>
        /// <param name="clipboardCellContent2"/>
        /// <returns>True iff clipboardCellContent1 and clipboardCellContent2 are NOT equal</returns>
        public static bool operator !=(
            DataGridClipboardCellContent clipboardCellContent1, 
            DataGridClipboardCellContent clipboardCellContent2)
        {
            return
                (clipboardCellContent1._column != clipboardCellContent2._column) ||
                (clipboardCellContent1._content != clipboardCellContent2._content) ||
                (clipboardCellContent1._item != clipboardCellContent2._item);
        }

        private object _item;
        private DataGridColumn _column;
        private object _content;
    }
}
