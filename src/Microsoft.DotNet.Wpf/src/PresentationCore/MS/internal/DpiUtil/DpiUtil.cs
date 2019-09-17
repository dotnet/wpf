// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

﻿
namespace MS.Internal
{
    using MS.Utility;
    using System;
    using System.Security;
    using System.Windows;
    using System.Windows.Interop;

    using PROCESS_DPI_AWARENESS = MS.Win32.NativeMethods.PROCESS_DPI_AWARENESS;

    /// <summary>
    /// DPI related utilities
    /// </summary>
    internal static partial class DpiUtil
    {
        /// <summary>
        /// This is the default PPI value used in WPF and Windows
        /// </summary>
        internal const double DefaultPixelsPerInch = 96.0d;

        /// <summary>
        /// Gets the DPI awareness context handle from and HWND
        /// </summary>
        /// <param name="hWnd">Handle to the window</param>
        /// <returns>The awareness context</returns>
        /// <remarks>
        ///     .. Attempts to get the value directly from the HWND, failing which
        ///     .. attempts to get it from the PROCESS_DPI_AWARENESS
        ///             value associated with the process of the HWND, failing which
        ///     .. gets the value from the PROCESS_DPI_AWARENESS of
        ///             the currently executing process
        /// </remarks>
        internal static DpiAwarenessContextHandle GetDpiAwarenessContext(IntPtr hWnd)
        {
            return DpiAwarenessContextHelper.GetDpiAwarenessContext(hWnd);
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
            return ProcessDpiAwarenessHelper.GetProcessDpiAwareness(hWnd);
        }

        /// <summary>
        /// Equivalent to <see cref="GetProcessDpiAwareness(IntPtr)"/>
        /// </summary>
        /// <param name="hWnd">Handle to the window being queried</param>
        /// <returns>The <see cref="DpiAwarenessContextValue"/> enum corresponding to the PROCESS_DPI_AWARENESS value of the <paramref name="hWnd"/></returns>
        /// <remarks>See remarks for <see cref="GetProcessDpiAwareness(IntPtr)"/></remarks>
        internal static DpiAwarenessContextValue GetProcessDpiAwarenessContextValue(IntPtr hWnd)
        {
            var dpiAwarenessContext = ProcessDpiAwarenessHelper.GetProcessDpiAwareness(hWnd);
            return (DpiAwarenessContextValue)DpiAwarenessContextHelper.GetProcessDpiAwarenessContext(dpiAwarenessContext);
        }


        /// <summary>
        /// Gets the PROCESS_DPI_AWARENESS enum value of the current process
        /// </summary>
        /// <returns>PROCESS_DPI_AWARENESS value of the current process</returns>
        /// <remarks>
        /// The only values returned by this method are PROCESS_SYSTEM_DPI_AWARE or
        /// PROCESS_DPI_UNAWARE
        /// </remarks>
        internal static PROCESS_DPI_AWARENESS GetLegacyProcessDpiAwareness()
        {
            return ProcessDpiAwarenessHelper.GetLegacyProcessDpiAwareness();
        }

        /// <summary>
        /// Gets the System DPI
        /// </summary>
        /// <returns>The system DPI</returns>
        /// <remarks>
        /// If the system DPI cannot be obtained directly, then
        /// it falls back to obtaining the system DPI indirectly
        /// by querying through the desktop.
        /// 
        /// If querying via the desktop fails for some reason, then
        /// it returns null.
        /// </remarks>
        internal static DpiScale2 GetSystemDpi()
        {
            return SystemDpiHelper.GetSystemDpi();
        }

        /// <summary>
        /// Gets the System DPI from the values stored in the UIElement static cache
        /// </summary>
        /// <returns>The system DPI value</returns>
        internal static DpiScale2 GetSystemDpiFromUIElementCache()
        {
            return SystemDpiHelper.GetSystemDpiFromUIElementCache();
        }

        /// <summary>
        /// Updates the UIElement static cache containing System DPI value
        /// </summary>
        /// <param name="systemDpiScale">Updated system DPI scale value</param>
        internal static void UpdateUIElementCacheForSystemDpi(DpiScale2 systemDpiScale)
        {
            SystemDpiHelper.UpdateUIElementCacheForSystemDpi(systemDpiScale);
        }

        /// <summary>
        /// Gets a window's DPI scale factor
        /// </summary>
        /// <param name="hWnd">Handle to the window</param>
        /// <param name="fallbackToNearestMonitorHeuristic">
        /// When true, falls back to obtaining the DPI of the monitor nearest to the window if unable to obtain
        /// the DPI of the window directly.
        /// When false, there is no fallback attempted and returns null on failure to obtain the window's DPI directly.
        /// </param>
        /// <returns>The window's DPI</returns>
        internal static DpiScale2 GetWindowDpi(IntPtr hWnd, bool fallbackToNearestMonitorHeuristic)
        {
            return WindowDpiScaleHelper.GetWindowDpi(hWnd, fallbackToNearestMonitorHeuristic);
        }

        /// <summary>
        /// Obtains extended DPI related information about an HWND - specifically, the <see cref="DpiAwarenessContextValue"/>,
        /// <see cref="DpiScale2"/>, and the screen-rectangle of the monitor where the <paramref name="hWnd"/> resides.
        /// </summary>
        /// <param name="hWnd">The handle to the HWND</param>
        /// <param name="fallbackToNearestMonitorHeuristic">The semantics of this parameter are identical to that in <see cref="GetWindowDpi(IntPtr, bool)"/></param>
        /// <returns>Extended DPI information</returns>
        internal static HwndDpiInfo GetExtendedDpiInfoForWindow(IntPtr hWnd, bool fallbackToNearestMonitorHeuristic)
        {
            return new HwndDpiInfo(hWnd, fallbackToNearestMonitorHeuristic);
        }

        /// <summary>
        /// Obtains extended DPI related information about an HWND - specifically, the <see cref="DpiAwarenessContextValue"/>,
        /// <see cref="DpiScale2"/>, and the screen-rectangle of the monitor where the <paramref name="hWnd"/> resides.
        /// </summary>
        /// <param name="hWnd">The handle to the HWND</param>
        /// <returns>Extended DPI information</returns>
        /// <remarks>If the window DPI could not be obtained directly, then it uses the DPI of the nearest
        /// monitor as a fall-back value</remarks>
        internal static HwndDpiInfo GetExtendedDpiInfoForWindow(IntPtr hWnd)
        {
            return GetExtendedDpiInfoForWindow(hWnd, true);
        }

        /// <summary>
        /// Helper to modify the DPI_AWARENESS_CONTEXT of the current thread.
        /// </summary>
        /// <param name="dpiAwarenessContext">DPI_AWARENESS_CONTEXT to set the thread to</param>
        /// <returns>
        /// An <see cref="IDisposable"/> that defines a scope during which the DPI_AWARENESS_CONTEXT of the current thread is
        /// modified to the requested value.
        /// </returns>
        internal static IDisposable WithDpiAwarenessContext(DpiAwarenessContextValue dpiAwarenessContext)
        {
            return new DpiAwarenessScope(dpiAwarenessContext);
        }

        /// <summary>
        /// flag1 and flag2 correspond to MonitorDpiScaleIndex1 and MonitorDpiScaleIndex2 of VisualFlags respectively
        /// UpdateDpiScales sets flag1 and flag2 based on the correct index of the DPI in the static DPI array stored in
        /// UIElement class and inserts an entry into the array if necessary.
        /// </summary>
        internal static DpiFlags UpdateDpiScalesAndGetIndex(double pixelsPerInchX, double pixelsPerInchY)
        {
            lock (UIElement.DpiLock)
            {
                bool dpiScaleFlag1, dpiScaleFlag2;
                int index = 0;
                int sizeOfList = UIElement.DpiScaleXValues.Count;
                for (index = 0; index < sizeOfList; index++)
                {
                    // We have found a match.
                    if (UIElement.DpiScaleXValues[index] == pixelsPerInchX / DpiUtil.DefaultPixelsPerInch &&
                        UIElement.DpiScaleYValues[index] == pixelsPerInchY / DpiUtil.DefaultPixelsPerInch)
                    {
                        break;
                    }
                }

                // Didn't find a match, add to the end of the list
                if (index == sizeOfList)
                {
                    UIElement.DpiScaleXValues.Add(pixelsPerInchX / DpiUtil.DefaultPixelsPerInch);
                    UIElement.DpiScaleYValues.Add(pixelsPerInchY / DpiUtil.DefaultPixelsPerInch);
                }

                // Since we only have 2 bits to spare in VisualFlags, we use the first 3 possible
                // values to directly map the index to the array. If the index is >= 3, we set both flags
                // to true, and store the actual index in an UncommonField on the Visual

                if (index < 3)
                {
                    dpiScaleFlag1 = (index & 0x1) != 0;
                    dpiScaleFlag2 = (index & 0x2) != 0;
                }
                else
                {
                    dpiScaleFlag1 = dpiScaleFlag2 = true;
                }

                return new DpiFlags(dpiScaleFlag1, dpiScaleFlag2, index);
            }
        }
    }
}
