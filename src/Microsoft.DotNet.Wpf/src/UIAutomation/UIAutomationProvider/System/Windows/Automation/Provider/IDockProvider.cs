// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// Description: Dock pattern provider interface

using System;
using System.Windows.Automation;
using System.Runtime.InteropServices;

namespace System.Windows.Automation.Provider
{
    /// <summary>
    /// Expose an element's ability to change its dock state at run time.
    /// </summary>
    [ComVisible(true)]
    [Guid("159bc72c-4ad3-485e-9637-d7052edf0146")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
#if (NO_INTERNAL_COMPILE_BUG1080665)
    internal interface IDockProvider
#else
    public interface IDockProvider
#endif
    {
        /// <summary>
        /// Moves the window to be docked at the requested location.
        /// </summary>
        void SetDockPosition( DockPosition dockPosition);

        /// <summary>Is the DockPosition Top, Left, Right, Bottom, Fill, or None?</summary>
        DockPosition DockPosition
        {
            get;
        }

    }
}
