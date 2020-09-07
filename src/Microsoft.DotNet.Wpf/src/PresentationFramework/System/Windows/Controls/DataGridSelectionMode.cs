// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


using System;

namespace System.Windows.Controls
{
    /// <summary>
    ///     The selection modes supported by DataGrid.
    /// </summary>
    public enum DataGridSelectionMode
    {
        /// <summary>
        ///     Only one item can be selected at a time.
        /// </summary>
        Single,

        /// <summary>
        ///     Multiple items can be selected, and the input gestures will default
        ///     to the "extended" mode.
        /// </summary>
        /// <remarks>
        ///     In Extended mode, selecting multiple items requires holding down 
        ///     the SHIFT or CTRL keys to extend the selection from an anchor point.
        /// </remarks>
        Extended,
    }
}
