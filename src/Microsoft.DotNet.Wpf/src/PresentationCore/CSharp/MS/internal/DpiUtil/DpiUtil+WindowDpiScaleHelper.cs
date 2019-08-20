// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


namespace MS.Internal
{
    using MS.Win32;
    using System;
    using System.Runtime.InteropServices;
    using System.Security;

    using MONITOR_DPI_TYPE = MS.Win32.NativeMethods.MONITOR_DPI_TYPE;

    /// <content>
    /// Contains definition of <see cref="WindowDpiScaleHelper"/>
    /// </content>
    internal static partial class DpiUtil
    {
        /// <summary>
        /// Gets a window's DPI scale factor
        /// </summary>
        /// <remarks>
        /// Provides a fallback for obtaining the DPI of the monitor nearest to
        /// the window if unable to obtain the DPI of the window directly.
        /// </remarks>
        private static class WindowDpiScaleHelper
        {
            /// <summary>
            /// When true, user32!GetDpiForWindow function is available on the
            /// currently running platform. Otherwise this value is False.
            /// </summary>
            private static bool IsGetDpiForWindowFunctionEnabled { get; set; } = true;

            /// <summary>
            /// Gets a window's DPI scale factor
            /// </summary>
            /// <param name="hWnd">Handle to the window</param>
            /// <param name="fallbackToNearestMonitorHeuristic">
            /// When true, falls back to obtaining the DPI of the monitor nearest to the window if unable to obtain
            /// the DPI of the window directly.
            /// When false, there is no callback attempted and returns null on failure to obtain the window's DPI directly.
            /// </param>
            /// <returns>The window's DPI</returns>
            internal static DpiScale2 GetWindowDpi(IntPtr hWnd, bool fallbackToNearestMonitorHeuristic)
            {
                if (IsGetDpiForWindowFunctionEnabled)
                {
                    try
                    {
                        return GetDpiForWindow(hWnd);
                    }
                    catch (Exception e) when (e is EntryPointNotFoundException || e is MissingMethodException || e is DllNotFoundException)
                    {
                        IsGetDpiForWindowFunctionEnabled = false;
                    }
                    catch (Exception e) when (e is COMException)
                    {
                    }
                }

                if (fallbackToNearestMonitorHeuristic)
                {
                    try
                    {
                        return GetDpiForWindowFromNearestMonitor(hWnd);
                    }
                    catch (Exception e) when (e is COMException)
                    {
                    }
                }

                return null;
            }

            /// <summary>
            /// Gets a window's DPI by querying it directly
            /// </summary>
            /// <param name="hWnd">Handle to the window</param>
            /// <returns>The DPI of the window</returns>
            private static DpiScale2 GetDpiForWindow(IntPtr hWnd)
            {
                uint dpi = SafeNativeMethods.GetDpiForWindow(new HandleRef(IntPtr.Zero, hWnd));
                return DpiScale2.FromPixelsPerInch(dpi, dpi);
            }

            /// <summary>
            /// Gets the DPI of the monitor nearest to a window
            /// </summary>
            /// <param name="hWnd">Handle to the window</param>
            /// <returns>DPI of the monitor nearest to the window</returns>
            private static DpiScale2 GetDpiForWindowFromNearestMonitor(IntPtr hWnd)
            {
                IntPtr hMon =
                    SafeNativeMethods.MonitorFromWindow(new HandleRef(IntPtr.Zero, hWnd), NativeMethods.MONITOR_DEFAULTTONEAREST);

                uint dpiX, dpiY;
                int hr = (int)UnsafeNativeMethods.GetDpiForMonitor(new HandleRef(IntPtr.Zero, hMon), MONITOR_DPI_TYPE.MDT_EFFECTIVE_DPI, out dpiX, out dpiY);
                
                // Throw if FAILED(hr)
                Marshal.ThrowExceptionForHR(hr);

                return DpiScale2.FromPixelsPerInch(dpiX, dpiY);
            }
        }
    }
}
