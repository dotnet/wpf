// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Threading;
using System.Security;
using System.Security.Permissions;

namespace Microsoft.Test
{
    /// <summary>
    /// Helper class for the WPF Dispatcher. This class provides simple and 
    /// consistent wrappers for common dispatcher operations.
    /// </summary>
    /// <example>
    /// <code>
    /// 
    /// // SAMPLE USAGE #1:
    /// // Move the mouse to a certain location on the screen. Wait for a popup to appear. 
    /// // Verify that it appeared.
    /// TimeSpan defaultPopupDelay = TimeSpan.FromSeconds(2);
    /// Mouse.MoveTo(new System.Drawing.Point(100, 100));
    /// DispatcherOperations.WaitFor(defaultPopupDelay);
    /// // verify that the popup showed up.
    /// 
    /// // SAMPLE USAGE #2:
    /// // Click on a button and verify that a mouse click event handler gets called.
    /// Mouse.MoveTo(new System.Drawing.Point(100, 100));
    /// Mouse.Click(System.Windows.Input.MouseButton.Left);
    /// DispatcherOperations.WaitFor(DispatcherPriority.SystemIdle);
    /// // verify that the handler has been clicked (e.g. check a isClicked variable)
    /// </code>
    /// </example>
    public static class DispatcherOperations
    {
        #region Public Methods

        /// <summary>
        /// This method will wait until all pending DispatcherOperations of a
        /// priority higher than the specified priority have been processed.
        /// </summary>
        /// <param name="priority">The priority to wait for before continuing.</param>
        [System.Security.Permissions.PermissionSet(System.Security.Permissions.SecurityAction.Assert, Name = "FullTrust")]
        public static void WaitFor(DispatcherPriority priority)
        {
            PermissionSet permissions = new PermissionSet(PermissionState.Unrestricted);
            permissions.Demand();
            WaitFor(TimeSpan.Zero, priority);
        }

        /// <summary>
        /// This method will wait for the specified TimeSpan, allowing pending
        /// DispatcherOperations (such as UI rendering) to continue during that
        /// time. This method should be used with caution. Waiting for time is 
        /// generally discouraged, because it may have an adverse effect on the 
        /// overall run time of a test suite when the test suite has a large 
        /// number of tests.
        /// </summary>
        /// <param name="time">Amount of time to wait.</param>
        [System.Security.Permissions.PermissionSet(System.Security.Permissions.SecurityAction.Assert, Name = "FullTrust")]
        public static void WaitFor(TimeSpan time)
        {
            PermissionSet permissions = new PermissionSet(PermissionState.Unrestricted);
            permissions.Demand();
            WaitFor(time, DispatcherPriority.SystemIdle);
        }

        #endregion


        #region Private Methods

        /// <summary>
        /// This effectively enables a caller to do all pending UI work before continuing.
        /// The method will block until the desired priority has been reached, emptying the
        /// Dispatcher queue of all items with a higher priority.
        /// </summary>
        /// <param name="time">Amount of time to wait.</param>
        /// <param name="priority">The priority to wait for before continuing.</param>
        [System.Security.Permissions.PermissionSet(System.Security.Permissions.SecurityAction.Assert, Name = "FullTrust")]
        private static void WaitFor(TimeSpan time, DispatcherPriority priority)
        {
            PermissionSet permissions = new PermissionSet(PermissionState.Unrestricted);
            permissions.Demand();

            // Create a timer for the minimum wait time.
            // When the time passes, the Tick handler will be called,
            // which allows us to stop the dispatcher frame.
            DispatcherTimer timer = new DispatcherTimer(priority);
            timer.Tick += new EventHandler(OnDispatched);
            timer.Interval = time;

            // Run a dispatcher frame.
            DispatcherFrame dispatcherFrame = new DispatcherFrame(false);
            timer.Tag = dispatcherFrame;
            timer.Start();
            Dispatcher.PushFrame(dispatcherFrame);
        }

        /// <summary>
        /// Dummy SystemIdle dispatcher item.  This discontinues the current
        /// dispatcher frame so control can return to the caller of WaitFor().
        /// </summary>
        [System.Security.Permissions.PermissionSet(System.Security.Permissions.SecurityAction.Assert, Name = "FullTrust")]
        private static void OnDispatched(object sender, EventArgs args)
        {
            // Stop the timer now.
            DispatcherTimer timer = (DispatcherTimer)sender;
            timer.Tick -= new EventHandler(OnDispatched);
            timer.Stop();
            DispatcherFrame frame = (DispatcherFrame)timer.Tag;
            frame.Continue = false;
        }

        #endregion
    }
}
