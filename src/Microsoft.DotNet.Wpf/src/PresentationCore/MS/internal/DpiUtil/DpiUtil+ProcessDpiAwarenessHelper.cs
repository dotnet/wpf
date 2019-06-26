// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


namespace MS.Internal
{
    using Microsoft.Win32.SafeHandles;
    using MS.Win32;
    using System;
    using System.Diagnostics;
    using System.Runtime.InteropServices;
    using System.Security;

    using PROCESS_DPI_AWARENESS = MS.Win32.NativeMethods.PROCESS_DPI_AWARENESS;

    /// <content>
    /// Contains definition of <see cref="ProcessDpiAwarenessHelper"/>
    /// </content>
    internal static partial class DpiUtil
    {
        /// <summary>
        /// Gets the PROCESS_DPI_AWARENESS enum value of the process associated
        /// with a window
        /// </summary>
        /// <remarks>
        /// If the process associated with the HWND cannot be queried for its
        /// PROCESS_DPI_AWARENESS value, then the value is obtained from the
        /// current process' DPI awareness information.
        /// </remarks>
        private static class ProcessDpiAwarenessHelper
        {
            /// <summary>
            /// If shcore.dll!GetProcessDpiAwarness function is supported in the
            /// current platform, then this value is True. Otherwise this value is False.
            /// </summary>
            private static bool IsGetProcessDpiAwarenessFunctionSupported { get; set; } = true;

            /// <summary>
            /// Gets the PROCESS_DPI_AWARENESS of the current process
            /// </summary>
            /// <returns>PROCESS_DPI_AWARENESS value</returns>
            /// <remarks>
            /// The only values returned by this method are PROCESS_SYSTEM_DPI_AWARE or
            /// PROCESS_DPI_UNAWARE
            /// </remarks>
            internal static PROCESS_DPI_AWARENESS GetLegacyProcessDpiAwareness()
            {
                return
                    UnsafeNativeMethods.IsProcessDPIAware()
                    ? PROCESS_DPI_AWARENESS.PROCESS_SYSTEM_DPI_AWARE
                    : PROCESS_DPI_AWARENESS.PROCESS_DPI_UNAWARE;
            }

            /// <summary>
            /// Gets the PROCESS_DPI_AWARENESS enum value of the process associated
            /// with a window
            /// </summary>
            /// <param name="hWnd">Handle to the window being queried</param>
            /// <returns>The PROCESS_DPI_AWARNESS value</returns>
            /// <remarks>
            /// If the process associated with the HWND cannot be queried for its
            /// PROCESS_DPI_AWARENESS value, then the value is obtained from the
            /// current process' DPI awareness information.
            /// </remarks>
            internal static PROCESS_DPI_AWARENESS GetProcessDpiAwareness(IntPtr hWnd)
            {
                if (IsGetProcessDpiAwarenessFunctionSupported)
                {
                    try
                    {
                        return GetProcessDpiAwarenessFromWindow(hWnd);
                    }
                    catch (Exception e) when (e is EntryPointNotFoundException || e is MissingMethodException || e is DllNotFoundException)
                    {
                        IsGetProcessDpiAwarenessFunctionSupported = false;
                    }
                    catch (Exception e) when (e is ArgumentException || e is UnauthorizedAccessException || e is COMException)
                    {
                    }
                }

                return GetLegacyProcessDpiAwareness();
            }

            /// <summary>
            /// Gets the PROCESS_DPI_AWARENESS of the process associated with a window
            /// </summary>
            /// <param name="hWnd">Handle of the window</param>
            /// <returns>PROCESS_DPI_AWARENESS value</returns>
            private static PROCESS_DPI_AWARENESS GetProcessDpiAwarenessFromWindow(IntPtr hWnd)
            {
                int windowThreadProcessId = 0;
                if (hWnd != IntPtr.Zero)
                {
                    UnsafeNativeMethods.GetWindowThreadProcessId(new HandleRef(null, hWnd), out windowThreadProcessId);
                }
                else
                {
                    // If a valid window is not specified, then query the current process instead of the process
                    // associated with the window
                    windowThreadProcessId = SafeNativeMethods.GetCurrentProcessId();
                }

                Debug.Assert(windowThreadProcessId != 0, "GetWindowThreadProcessId failed");

                using (var hProcess = new SafeProcessHandle(UnsafeNativeMethods.OpenProcess(NativeMethods.PROCESS_ALL_ACCESS, false, windowThreadProcessId), true))
                {
                    return SafeNativeMethods.GetProcessDpiAwareness(new HandleRef(null, hProcess.DangerousGetHandle()));
                }
            }
        }
    }
}
