// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections;
using System.Windows.Threading;

using System.Windows.Media;
using System.Runtime.InteropServices;
using MS.Win32;

namespace System.Windows
{
    /// <summary>
    ///     Handler for the AutoResized event on HwndSource.
    /// </summary>
    public delegate void AutoResizedEventHandler(object sender, AutoResizedEventArgs e);

    /// <summary>
    ///     Event arguments for the AutoResized event on HwndSource.
    /// </summary>
    public class AutoResizedEventArgs : EventArgs
    {
        /// <summary>
        ///     Creates a new AutoResized event argument.
        /// </summary>
        /// <param name="size">The new size of the HwndSource.</param>
        public AutoResizedEventArgs(Size size)
        {
            _size = size;
        }

        /// <summary>
        ///     The new size of the HwndSource.
        /// </summary>
        public Size Size
        {
            get
            {
                return _size;
            }
        }

        private Size _size;
    }
}

