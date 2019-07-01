// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Drawing;
using System.Runtime.InteropServices;

namespace Microsoft.Test.VisualVerification
{
    /// <summary>
    /// WIN32 RECT structure
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    internal struct RECT
    {
        internal int X;
        internal int Y;
        internal int Right;
        internal int Bottom;

        internal Rectangle ToRectangle()
        {
            return new Rectangle(X, Y, Right - X, Bottom - Y);
        }
    }

    /// <summary>
    /// RasterOperation used by GDI BitBlt and StretchBlt methods
    /// </summary>
    [FlagsAttribute]
    internal enum RasterOperationCodeEnum
    {
        /// <summary/>
        SRCCOPY = 0x00CC0020,
        /// <summary/>
        SRCPAINT = 0x00EE0086,
        /// <summary/>
        SRCAND = 0x008800C6,
        /// <summary/>
        SRCINVERT = 0x00660046,
        /// <summary/>
        SRCERASE = 0x00440328,
        /// <summary/>
        NOTSRCCOPY = 0x00330008,
        /// <summary/>
        NOTSRCERASE = 0x001100A6,
        /// <summary/>
        MERGECOPY = 0x00C000CA,
        /// <summary/>
        MERGEPAINT = 0x00BB0226,
        /// <summary/>
        PATCOPY = 0x00F00021,
        /// <summary/>
        PATPAINT = 0x00FB0A09,
        /// <summary/>
        PATINVERT = 0x005A0049,
        /// <summary/>
        DSTINVERT = 0x00550009,
        /// <summary/>
        BLACKNESS = 0x00000042,
        /// <summary/>
        WHITENESS = 0x00FF0062,
        /// <summary/>
        CAPTUREBLT = 0x40000000
    }

    /// <summary>
    /// Native methods
    /// </summary>
    internal static class NativeMethods
    {
        //wrappers cover API's for used for Visual Verification/Screen Capture        
        private const string Gdi32Dll = "GDI32.dll";
        private const string User32Dll = "User32.dll";

        #region Gdi32

        [DllImportAttribute(Gdi32Dll, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        static internal extern bool BitBlt(IntPtr hdcDest, int nXDest, int nYDest, int nWidth, int nHeight, IntPtr hdcSrc, int nXSrc, int nYSrc, Int32 RasterOpCode);

        [DllImportAttribute(Gdi32Dll, SetLastError = true)]
        static internal extern IntPtr CreateCompatibleBitmap(IntPtr hdcSrc, int width, int height);

        [DllImportAttribute(Gdi32Dll, SetLastError = true)]
        static internal extern IntPtr CreateCompatibleDC(IntPtr HDCSource);

        [DllImport(Gdi32Dll, SetLastError = true, CharSet = CharSet.Unicode)]
        static internal extern IntPtr CreateDC(string driverName, string deviceName, string reserved, IntPtr initData);

        [DllImportAttribute(Gdi32Dll, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        static internal extern bool DeleteDC(IntPtr HDC);

        [DllImportAttribute(Gdi32Dll, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        static internal extern bool DeleteObject(IntPtr hBMP);

        [DllImportAttribute(Gdi32Dll, SetLastError = true)]
        static internal extern IntPtr SelectObject(IntPtr HDC, IntPtr hgdiobj);

        #endregion

        #region User32

        [DllImportAttribute(User32Dll)]
        static internal extern IntPtr GetDC(IntPtr hWnd);

        [DllImportAttribute(User32Dll, SetLastError = true)]
        static internal extern bool GetClientRect(IntPtr HWND, out RECT rect);

        [DllImportAttribute(User32Dll, SetLastError = true)]
        static internal extern bool GetWindowRect(IntPtr HWND, out RECT rect);

        [DllImportAttribute(User32Dll)]
        static internal extern bool ReleaseDC(IntPtr HWND, IntPtr HDC);

        #endregion
    }
}
