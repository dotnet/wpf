// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


namespace MS.Internal
{
    using MS.Utility;
    using MS.Win32;
    using System;
    using System.ComponentModel;
    using System.Runtime.InteropServices;
    using System.Security;

    internal static partial class DpiUtil
    {
        /// <summary>
        /// Utility class that encapsulates DPI related information associated
        /// with an HWND
        /// </summary>
        /// <remarks>This is a <see cref="Tuple{T1, T2}"/> because it is primarily used as a <see cref="Dictionary"/> key 
        /// in <see cref="System.Windows.SystemResources"/></remarks>
        public class HwndDpiInfo : Tuple<DpiAwarenessContextValue, DpiScale2>
        {
            /// <summary>
            /// Constructor 
            /// </summary>
            /// <param name="hWnd"></param>
            /// <param name="fallbackToNearestMonitorHeuristic">Semantics of this parameter are identical to that in <see cref="DpiUtil.GetWindowDpi"/></param>
            internal HwndDpiInfo(IntPtr hWnd, bool fallbackToNearestMonitorHeuristic) : base(
                item1: (DpiAwarenessContextValue)DpiUtil.GetDpiAwarenessContext(hWnd),
                item2: DpiUtil.GetWindowDpi(hWnd, fallbackToNearestMonitorHeuristic))
            {
                ContainingMonitorScreenRect = NearestMonitorInfoFromWindow(hWnd).rcMonitor;
            }

            /// <summary>
            /// Constructor to create an instance based on arbitrary <see cref="DpiAwarenessContextValue"/>
            /// and <see cref="DpiScale2"/> information
            /// </summary>
            /// <param name="dpiAwarenessContextValue"></param>
            /// <param name="dpiScale"></param>
            internal HwndDpiInfo(DpiAwarenessContextValue dpiAwarenessContextValue, DpiScale2 dpiScale) 
                : base(dpiAwarenessContextValue, dpiScale)
            {
                // Get information from the desktop as the default. This information would still be
                // virtualized based on the DPI of the caller. 
                ContainingMonitorScreenRect = NearestMonitorInfoFromWindow(IntPtr.Zero).rcMonitor;
            }

            /// <summary>
            /// </summary>
            /// <param name="hwnd"></param>
            /// <returns></returns>
            private static NativeMethods.MONITORINFOEX NearestMonitorInfoFromWindow(IntPtr hwnd)
            {
                IntPtr hMonitor = SafeNativeMethods.MonitorFromWindow(new HandleRef(null, hwnd), NativeMethods.MONITOR_DEFAULTTONEAREST);
                if (hMonitor == IntPtr.Zero)
                {
                    throw new Win32Exception();
                }

                var monitorInfo = new NativeMethods.MONITORINFOEX();
                SafeNativeMethods.GetMonitorInfo(new HandleRef(null, hMonitor), monitorInfo);

                return monitorInfo;
            }

            /// <summary>
            /// Screen rectangle of the monitor that contains the HWND/window that initialized this
            /// object
            /// </summary>
            /// <remarks>
            /// This rectangle (the size as well as the component points) is virtualized by the OS
            /// based on the DPI of the caller. Suppose the actual rectangle is 2160 x 3840 @ 150% DPI,
            /// a SystemAware caller at 100% DPI would see 1440 x 2560, whereas a PerMonitorAware
            /// caller at 150% DPI would see 2160 x 3840
            /// </remarks>
            internal NativeMethods.RECT ContainingMonitorScreenRect { get; }

            /// <summary>
            /// <see cref="DpiAwarenessContextValue"/> of the HWND
            /// </summary>
            internal DpiAwarenessContextValue DpiAwarenessContextValue { get => Item1; }

            /// <summary>
            /// DPI Scale factor of the HWND
            /// </summary>
            internal DpiScale2 DpiScale { get => Item2; }
        }
    }
}
