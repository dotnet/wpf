// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


namespace MS.Internal
{
    using MS.Utility;
    using MS.Win32;
    using System;
    using System.Collections.Generic;
    using System.Security;

    using PROCESS_DPI_AWARENESS = MS.Win32.NativeMethods.PROCESS_DPI_AWARENESS;

    /// <content>
    /// Contains definition of <see cref="DpiAwarenessContextHelper"/>
    /// </content>
    internal static partial class DpiUtil
    {
        /// <summary>
        /// Helper to get the DPI awareness context handle from an HWND
        /// </summary>
        /// <remarks>
        ///     .. Attempts to get the value directly from the HWND, failing which
        ///     .. attempts to get it from the PROCESS_DPI_AWARENESS
        ///             value associated with the process of the HWND, failing which
        ///     .. gets the value from the PROCESS_DPI_AWARENESS of
        ///             the currently executing process
        /// </remarks>
        private static class DpiAwarenessContextHelper
        {
            /// <summary>
            /// True if user32.dll!GetWindowDpiAwarenessContext is supported on
            /// the current platform, otherwise False
            /// </summary>
            private static bool IsGetWindowDpiAwarenessContextMethodSupported { get; set; } = true;

            /// <summary>
            /// Attempts to Get DPI Awareness context handle from the HWND directly.
            /// If that fails, falls back to the DPI awareness value of the process
            /// associated with the HWND
            /// </summary>
            /// <param name="hWnd">HWND being queried</param>
            /// <returns>DPI Awareness Context handle of <paramref name="hWnd"/></returns>
            internal static DpiAwarenessContextHandle GetDpiAwarenessContext(IntPtr hWnd)
            {
                if (IsGetWindowDpiAwarenessContextMethodSupported)
                {
                    try
                    {
                        return GetWindowDpiAwarenessContext(hWnd);
                    }
                    catch (Exception e) when (e is EntryPointNotFoundException || e is MissingMethodException || e is DllNotFoundException)
                    {
                        IsGetWindowDpiAwarenessContextMethodSupported = false;
                    }
                }

                return GetProcessDpiAwarenessContext(hWnd);
            }

            /// <summary>
            /// Gets DPI awareness context from the process associated with
            /// a window.
            /// </summary>
            /// <param name="hWnd">Handle to the window</param>
            /// <returns>DPI awareness context</returns>
            private static DpiAwarenessContextHandle GetProcessDpiAwarenessContext(IntPtr hWnd)
            {
                PROCESS_DPI_AWARENESS processDpiAwareneess =
                    ProcessDpiAwarenessHelper.GetProcessDpiAwareness(hWnd);
                return GetProcessDpiAwarenessContext(processDpiAwareneess);
            }

            /// <summary>
            /// Maps a PROCESS_DPI_AWARENESS enumeration into a well-known
            /// PROCESS_DPI_AWARENESS handle
            /// </summary>
            /// <param name="dpiAwareness">PROCESS_DPI_AWARENESS enum value</param>
            /// <returns>PROCESS_DPI_AWARENESS handle</returns>
            internal static DpiAwarenessContextHandle GetProcessDpiAwarenessContext(PROCESS_DPI_AWARENESS dpiAwareness)
            {
                switch (dpiAwareness)
                {
                    case PROCESS_DPI_AWARENESS.PROCESS_SYSTEM_DPI_AWARE:
                        return NativeMethods.DPI_AWARENESS_CONTEXT_SYSTEM_AWARE;

                    case PROCESS_DPI_AWARENESS.PROCESS_PER_MONITOR_DPI_AWARE:
                        return NativeMethods.DPI_AWARENESS_CONTEXT_PER_MONITOR_AWARE;

                    case PROCESS_DPI_AWARENESS.PROCESS_DPI_UNAWARE:
                    default:
                        return NativeMethods.DPI_AWARENESS_CONTEXT_UNAWARE;
                }
            }

            /// <summary>
            /// Gets a window's DPI awareness context using user32!GetWindowDpiAwarenessContext
            /// </summary>
            /// <param name="hWnd">Handle to the window</param>
            /// <returns>The DPI awareness context</returns>
            private static DpiAwarenessContextHandle GetWindowDpiAwarenessContext(IntPtr hWnd)
            {
                return SafeNativeMethods.GetWindowDpiAwarenessContext(hWnd);
            }
        }
    }
}
