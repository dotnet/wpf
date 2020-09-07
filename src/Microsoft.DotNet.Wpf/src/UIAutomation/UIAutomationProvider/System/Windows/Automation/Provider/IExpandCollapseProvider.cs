// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// Description: Expand Collapse pattern provider interface

using System;
using System.Windows.Automation;
using System.Runtime.InteropServices;

namespace System.Windows.Automation.Provider
{
    /// <summary>
    /// 
    /// Exposes a control's ability to expand to display more content or
    /// collapse to hide content.
    /// Supported in conjunction with the HierarchyItem pattern on
    /// TreeView items to provide tree-like behavior, but is also relevant
    /// for individual controls that open and close.
    /// 
    /// Examples of UI that implements this includes:
    /// - TreeView items
    /// - Office's smart menus that have been collapsed
    /// - Chevrons on toolbars
    /// - Combo box
    /// - Menus
    /// - "Expandos" in the task pane of Windows Explorer (left-hand side where folder
    ///   view is often displayed).
    /// </summary>
    [ComVisible(true)]
    [Guid("d847d3a5-cab0-4a98-8c32-ecb45c59ad24")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
#if (NO_INTERNAL_COMPILE_BUG1080665)
    internal interface IExpandCollapseProvider
#else
    public interface IExpandCollapseProvider
#endif
    {
        /// <summary>
        /// Blocking method that returns after the element has been expanded.
        /// </summary>
        void Expand();

        /// <summary>
        /// Blocking method that returns after the element has been collapsed.
        /// </summary>
        void Collapse();

        ///<summary>indicates an element's current Collapsed or Expanded state</summary>
        ExpandCollapseState ExpandCollapseState
        {
            get;
        }
    }
}
