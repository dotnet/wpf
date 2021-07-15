// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// Description: Window pattern provider interface

using System;
using System.Windows.Automation;
using System.Runtime.InteropServices;

namespace System.Windows.Automation.Provider
{
    /// <summary>
    /// Expose an element's ability to change its on-screen position or size,
    /// as well as change the visual state and close it.
    /// </summary>
    [ComVisible(true)]
    [Guid("987df77b-db06-4d77-8f8a-86a9c3bb90b9")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
#if (NO_INTERNAL_COMPILE_BUG1080665)
    internal interface IWindowProvider
#else
    public interface IWindowProvider
#endif
    {
        /// <summary>
        /// Changes the State of the window based on the Passed enum.
        /// </summary>
        /// <param name="state">The requested state of the window.</param>
        void SetVisualState( WindowVisualState state );

        /// <summary>
        /// Non-blocking call to close this non-application window.
        /// When called on a split pane, it will close the pane (thereby removing a
        /// split), it may or may not also close all other panes related to the
        /// document/content/etc. This behavior is application dependent.
        /// </summary>
        void Close();

        /// <summary>
        /// Causes the calling code to block, waiting the specified number of milliseconds, for the
        /// associated window to enter an idle state.
        /// </summary>
        /// <remarks>
        /// The implementation is dependent on the underlying application framework therefore this
        /// call may return sometime after the window is ready for user input.  The calling code
        /// should not rely on this call to understand exactly when the window has become idle.
        ///
        /// For now this method works reliably for both WinFX and Win32 Windows that are starting
        /// up.  However, if called at other times on WinFX Windows (e.g. during a long layout)
        /// WaitForInputIdle may return true before the Window is actually idle.  Additional work
        /// needs to be done to detect when WinFX Windows are idle.
        /// </remarks>
        /// <param name="milliseconds">The amount of time, in milliseconds, to wait for the
        /// associated process to become idle. The maximum is the largest possible value of a
        /// 32-bit integer, which represents infinity to the operating system
        /// </param>
        /// <returns>
        /// returns true if the window has reached the idle state and false if the timeout occurred.
        /// </returns>
        [return: MarshalAs(UnmanagedType.Bool)]
        bool WaitForInputIdle( int milliseconds );


        /// <summary>Is this window Maximizable</summary>
        bool Maximizable
        {
            [return: MarshalAs(UnmanagedType.Bool)] // Without this, only lower SHORT of BOOL*pRetVal param is updated.
            get;
        }

        /// <summary>Is this window Minimizable</summary>
        bool Minimizable
        {
            [return: MarshalAs(UnmanagedType.Bool)] // Without this, only lower SHORT of BOOL*pRetVal param is updated.
            get;
        }

        /// <summary>Is this is a modal window.</summary>
        bool IsModal
        {
            [return: MarshalAs(UnmanagedType.Bool)] // Without this, only lower SHORT of BOOL*pRetVal param is updated.
            get;
        }

        /// <summary>Is the Window Maximized, Minimized, or Normal (aka restored)</summary>
        WindowVisualState VisualState
        {
            get;
        }

        /// <summary>Is the Window Closing, ReadyForUserInteraction, BlockedByModalWindow or NotResponding.</summary>
        WindowInteractionState InteractionState
        {
            get;
        }

        /// <summary>Is this window is always on top</summary>
        bool IsTopmost
        {
            [return: MarshalAs(UnmanagedType.Bool)] // Without this, only lower SHORT of BOOL*pRetVal param is updated.
            get;
        }
    }
}
