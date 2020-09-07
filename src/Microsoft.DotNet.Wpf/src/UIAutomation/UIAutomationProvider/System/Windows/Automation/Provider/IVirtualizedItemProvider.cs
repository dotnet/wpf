// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// Description: Virtualized Item pattern provider interface

using System;
using System.Windows.Automation;
using System.Runtime.InteropServices;

namespace System.Windows.Automation.Provider
{
    /// <summary>
    /// Exposes an item's ability to convert itself into realized state from virtualized state.
    /// Realized state is a state when the item has Visual associated with it. It must be implemented
    /// by items(e.g. ListBoxItem) whose containers(e.g. ListBox) can support Virtualization.
    /// 
    /// Examples of Item types that implements this includes:
    /// - ListBoxItem
    /// - ListViewItem
    /// - ComboBoxItem
    /// - TabItem
    /// </summary>

    [ComVisible(true)]
    [Guid("cb98b665-2d35-4fac-ad35-f3c60d0c0b8b")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
#if (NO_INTERNAL_COMPILE_BUG1080665)
    internal interface IVirtualizedItemProvider
#else
    public interface IVirtualizedItemProvider
#endif
    {
        /// <summary>
        /// Request that a placeholder element make itself fully available. Blocks
        /// until element is available, which could take time.
        /// Parent control may scroll as a side effect if the container needs to
        /// bring the item into view in order to devirtualize it.
        /// </summary>      
        void Realize();

    }
}
