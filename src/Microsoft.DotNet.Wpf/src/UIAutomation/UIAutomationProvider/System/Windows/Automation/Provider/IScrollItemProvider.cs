// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// Description: ScrollItem pattern provider interface

using System;
using System.Windows.Automation;
using System.Runtime.InteropServices;

namespace System.Windows.Automation.Provider
{
    /// <summary>
    /// Implemented by objects in a known Scrollable context, such as ListItems, ListViewItems, TreeViewItems, and Tabs.
    /// This allows them to be scrolled into view using known API's based on the control in question.
    /// </summary>
    [ComVisible(true)]
    [Guid("2360c714-4bf1-4b26-ba65-9b21316127eb")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
#if (NO_INTERNAL_COMPILE_BUG1080665)
    internal interface IScrollItemProvider
#else
    public interface IScrollItemProvider
#endif
    {
        /// <summary>
        /// Scrolls the windows containing this automation element to make this element visible.
        /// InvalidOperationException should be thrown if item becomes unable to be scrolled. Makes
        /// no guarantees about where the item will be in the scrolled window.
        /// </summary>
        void ScrollIntoView();
    }
}
