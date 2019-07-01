// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Interop;

namespace Microsoft.Test.VisualVerification
{
    /// <summary>
    /// WPF type centric helper, on top of the general purpose Snapshot.
    /// </summary>
    public static class SnapshotHelper
    {
        /// <summary>
        /// Creates a Snapshot instance from a Wpf Window.
        /// </summary>
        /// <param name="window">The Wpf Window, identifying the window to capture from.</param>
        /// <param name="windowSnapshotMode">Determines if window border region should captured as part of Snapshot.</param>
        /// <returns>A Snapshot instance of the pixels captured.</returns>
        public static Snapshot SnapshotFromWindow(Visual window, WindowSnapshotMode windowSnapshotMode)
        {
            Snapshot result;

            HwndSource source = (HwndSource)PresentationSource.FromVisual(window);
            if (source == null)
            {
                throw new InvalidOperationException("The specified Window is not being rendered.");
            }

            IntPtr windowHandle = source.Handle;

            result = Snapshot.FromWindow(windowHandle, windowSnapshotMode);

            return result;
        }
    }
}
