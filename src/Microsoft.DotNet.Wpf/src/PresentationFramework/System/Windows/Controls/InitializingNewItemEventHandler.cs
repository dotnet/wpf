// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


using System;

namespace System.Windows.Controls
{
    /// <summary>
    ///     Delegate used for the InitializingNewItem event on DataGrid.
    /// </summary>
    /// <param name="sender">The DataGrid that raised the event.</param>
    /// <param name="e">The event arguments where callbacks can access the new item.</param>
    public delegate void InitializingNewItemEventHandler(object sender, InitializingNewItemEventArgs e);
}
