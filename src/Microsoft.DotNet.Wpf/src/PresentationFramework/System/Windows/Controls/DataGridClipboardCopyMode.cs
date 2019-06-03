// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

﻿
namespace System.Windows.Controls
{
    /// <summary>
    /// Defines modes that indicate how DataGrid content is copied to the Clipboard. 
    /// </summary>
    public enum DataGridClipboardCopyMode
    {
        /// <summary>
        /// Copying to the Clipboard is disabled.
        /// </summary>
        None,

        /// <summary>
        /// The text values of selected cells can be copied to the Clipboard. Column header is not included. 
        /// </summary>
        ExcludeHeader,

        /// <summary>
        /// The text values of selected cells can be copied to the Clipboard. Column header is included for columns that contain selected cells.  
        /// </summary>
        IncludeHeader,
    }
}