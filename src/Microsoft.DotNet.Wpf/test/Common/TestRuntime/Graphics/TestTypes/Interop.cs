// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.InteropServices;

using Rect = System.Drawing.Rectangle;
using Point = System.Drawing.Point;
using Microsoft.Test.Graphics.Factories;
using Microsoft.Test.Display;

using BindingFlags = System.Reflection.BindingFlags;
//TODO-Miguep: is thisneeded?
//using TrustedAssembly = Microsoft.Test.Security.Wrappers.AssemblySW;
//using TrustedType = Microsoft.Test.Security.Wrappers.TypeSW;

namespace Microsoft.Test.Graphics.TestTypes
{
    /// <summary>
    /// Access to unmanaged Dlls
    /// </summary>
    public class Interop
    {
        /// <summary>
        /// Same as LOGPIXELSX in wingdi.h
        /// </summary>
        public const int SourceCopy = 0x00CC0020;

        /// <summary>
        /// Same as LOGPIXELSX in wingdi.h
        /// </summary>
        public const int LogicalPixelsX = 88;

        /// <summary>
        /// Same as LOGPIXELSY in wingdi.h
        /// </summary>
        public const int LogicalPixelsY = 90;

        /// <summary>
        /// Same as BITSPIXEL in wingdi.h
        /// </summary>
        public const int BitsPerPixel = 12;

        /// <summary/>
        public static void MakeProcessDpiAware()
        {
            if (Const.IsVistaOrNewer)
            {
                // Processes run on Vista and above need to be made DPI aware
                Microsoft.Test.Diagnostics.SystemInformation.Current.SetProcessDpiAware();
            }
        }

        /// <summary/>
        public static IntPtr GetDC(IntPtr windowHandle)
        {
            return _GetDC(windowHandle);
        }

        /// <summary/>
        public static int ReleaseDC(IntPtr windowHandle, IntPtr hdc)
        {
            return _ReleaseDC(windowHandle, hdc);
        }

        /// <summary/>
        public static int GetDeviceCaps(IntPtr hdc, int index)
        {
            return _GetDeviceCaps(hdc, index);
        }

        internal static int GetBitsPerPixel(IntPtr hdc)
        {
            return Monitor.MonitorFromWindow(hdc).DisplaySettings.Current.BitsPerPixel;
        }

        internal static double GetDpiX(IntPtr hdc)
        {
            return Monitor.Dpi.x;
        }

        internal static double GetDpiY(IntPtr hdc)
        {
            return Monitor.Dpi.y;
        }

        #region DLL Imports

        /// <summary>Retrieves the coordinates of a window's client area.</summary>
        /// <param name="hwnd">Handle to the window whose client coordinates are to be retrieved.</param>
        /// <param name="rc">RECT structure that receives the client coordinates.</param>
        /// <returns>true if the function succeeds, false otherwise.</returns>
        [DllImport("user32.dll")]
        public static extern bool GetClientRect(IntPtr hwnd, [In, Out] ref Rect rc);

        /// <summary>Converts the point from client area co-ordinates to screen co-ordinates.</summary>
        /// <param name="hwnd">Handle to the window to which the point belows.</param>
        /// <param name="pt">The point that is to be converted. This also has the output, which is also a point.</param>
        /// <returns>true if the function succeeds, false otherwise.</returns>
        [DllImport("user32.dll")]
        public static extern bool ClientToScreen(IntPtr hwnd, [In, Out] ref Point pt);

        /// <summary>
        ///     The GetDC function retrieves a handle to a display device context (DC)
        ///     for the client area of a specified window or for the entire screen.</summary>
        /// <param name="hwnd">Handle to the window whose DC is to be retrieved.</param>
        /// <returns>A handle to the DC for the specified window's client area.</returns>
        [DllImport("User32.dll", EntryPoint = "GetDC")]
        [System.CLSCompliant(false)]
        public static extern IntPtr _GetDC(IntPtr hwnd);

        /// <summary>Releases a device context (DC), freeing it for use by other applications.</summary>
        /// <param name="hwnd">Handle to the window whose DC is to be released.</param>
        /// <param name="hdc">Handle to the DC to be released.</param>
        /// <returns>1 if the DC was released, 0 otherwise.</returns>
        [DllImport("User32.dll", EntryPoint = "ReleaseDC")]
        [System.CLSCompliant(false)]
        public static extern int _ReleaseDC(IntPtr hwnd, IntPtr hdc);

        /// <summary>
        ///     Performs a bit-block transfer of the color data corresponding to
        ///     a rectangle of pixels from the specified source device context into a
        ///     destination device context.</summary>
        /// <param name="hdcDest">Handle to the destination device context.</param>
        /// <param name="destX">Specifies the x-coordinate, in logical units, of the upper-left corner of the destination rectangle.</param>
        /// <param name="destY">Specifies the y-coordinate, in logical units, of the upper-left corner of the destination rectangle.</param>
        /// <param name="destWidth">Specifies the width, in logical units, of the source and destination rectangles.</param>
        /// <param name="destHeight">Specifies the height, in logical units, of the source and the destination rectangles.</param>
        /// <param name="hdcSrc">Handle to the source device context.</param>
        /// <param name="sourceX">Specifies the x-coordinate, in logical units, of the upper-left corner of the source rectangle.</param>
        /// <param name="sourceY">Specifies the y-coordinate, in logical units, of the upper-left corner of the source rectangle.</param>
        /// <param name="dwRop">Specifies a raster-operation code.</param>
        /// <returns>true on success, false otherwise.</returns>
        [DllImport("gdi32.dll", CharSet = CharSet.Auto)]
        public static extern bool BitBlt(IntPtr hdcDest, int destX, int destY, int destWidth, int destHeight, IntPtr hdcSrc, int sourceX, int sourceY, int dwRop);

        /// <summary>Grabs the value for a device parameter.</summary>
        /// <param name="hdc">A handle to the DC you want to query.</param>
        /// <param name="nIndex">The index of the parameter you wish to query the value of on this device.</param>
        /// <returns>The value of the parameter queried for the specified device.</returns>
        [DllImport("gdi32.dll", EntryPoint = "GetDeviceCaps")]
        [System.CLSCompliant(false)]
        public static extern int _GetDeviceCaps(IntPtr hdc, int nIndex);

        /// <summary>
        /// Allows you to get non-96 dpi results from GetDeviceCaps in Vista.
        /// This method does not exist in XP and should not be called unless it has been determined that OS running is Vista.
        /// </summary>
        [DllImport("user32.dll")]
        private static extern void SetProcessDPIAware();

        #endregion
    }
}