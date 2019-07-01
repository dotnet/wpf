// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace Microsoft.Test.Theming
{
    internal struct HwndInfo
    {
        public HwndInfo(IntPtr hWnd)
        {
            this.hWnd = hWnd;
            NativeMethods.GetWindowThreadProcessId(hWnd, out ProcessId);
        }

        /// <summary>
        /// Hwnd
        /// </summary>
        public IntPtr hWnd;

        /// <summary>
        /// Process ID of Hwnd
        /// </summary>
        public int ProcessId;

        
    }
}
