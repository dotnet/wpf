// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        

namespace Microsoft.Test.Win32
{
    using System;
    using Microsoft.Test.Win32;
    using System.Runtime.InteropServices;
    using System.Security.Permissions;
    using System.Security;

    /// <summary>
    /// Wraps the GDI32.dll
    /// </summary>
    internal static class Gdi32
    {
        private const string DLLNAME = "GDI32.dll";

        public const int LOGPIXELSX = 88;
        public const int LOGPIXELSY = 90;

        [DllImport(DLLNAME, PreserveSig = true)]
        [SecuritySafeCritical]
        [SuppressUnmanagedCodeSecurity]
        [SecurityPermission(SecurityAction.Assert, UnmanagedCode = true)]
        internal static extern int GetDeviceCaps(IntPtr hdc, int nIndex);

        #region Enum
        /// <summary>
        /// The RasterOperation used by BitBlt and StretchBlt
        /// </summary>
        [FlagsAttribute]
        public enum RasterOperationCodeEnum
        {
            /// <summary>
            /// See MSDN Documentation
            /// </summary>
            SRCCOPY = 0x00CC0020,
            /// <summary>
            /// See MSDN Documentation
            /// </summary>
            SRCPAINT = 0x00EE0086,
            /// <summary>
            /// See MSDN Documentation
            /// </summary>
            SRCAND = 0x008800C6,
            /// <summary>
            /// See MSDN Documentation
            /// </summary>
            SRCINVERT = 0x00660046,
            /// <summary>
            /// See MSDN Documentation
            /// </summary>
            SRCERASE = 0x00440328,
            /// <summary>
            /// See MSDN Documentation
            /// </summary>
            NOTSRCCOPY = 0x00330008,
            /// <summary>
            /// See MSDN Documentation
            /// </summary>
            NOTSRCERASE = 0x001100A6,
            /// <summary>
            /// See MSDN Documentation
            /// </summary>
            MERGECOPY = 0x00C000CA,
            /// <summary>
            /// See MSDN Documentation
            /// </summary>
            MERGEPAINT = 0x00BB0226,
            /// <summary>
            /// See MSDN Documentation
            /// </summary>
            PATCOPY = 0x00F00021,
            /// <summary>
            /// See MSDN Documentation
            /// </summary>
            PATPAINT = 0x00FB0A09,
            /// <summary>
            /// See MSDN Documentation
            /// </summary>
            PATINVERT = 0x005A0049,
            /// <summary>
            /// See MSDN Documentation
            /// </summary>
            DSTINVERT = 0x00550009,
            /// <summary>
            /// See MSDN Documentation
            /// </summary>
            BLACKNESS = 0x00000042,
            /// <summary>
            /// See MSDN Documentation
            /// </summary>
            WHITENESS = 0x00FF0062,
            /*
                                /// <summary>
                                /// See MSDN Documentation
                                /// </summary>
                                NOMIRRORBITMAP  = (int)0x80000000,
            */
            /// <summary>
            /// See MSDN Documentation
            /// </summary>
            CAPTUREBLT = 0x40000000
        }
        #endregion Enum

        #region DLLImport APIs
        /// <summary>
        /// See MSDN Documentation
        /// </summary>
        /// <param name="hdcDest"></param>
        /// <param name="nXDest"></param>
        /// <param name="nYDest"></param>
        /// <param name="nWidth"></param>
        /// <param name="nHeight"></param>
        /// <param name="hdcSrc"></param>
        /// <param name="nXSrc"></param>
        /// <param name="nYSrc"></param>
        /// <param name="RasterOpCode"></param>
        /// <returns></returns>
        [DllImportAttribute(DLLNAME, SetLastError = true)]
        static public extern bool BitBlt(IntPtr hdcDest, int nXDest, int nYDest, int nWidth, int nHeight, IntPtr hdcSrc, int nXSrc, int nYSrc, Int32 RasterOpCode);
        /// <summary>
        /// See MSDN Documentation
        /// </summary>
        /// <param name="hdcSrc"></param>
        /// <param name="width"></param>
        /// <param name="height"></param>
        /// <returns></returns>
        [DllImportAttribute(DLLNAME, SetLastError = true)]
        static public extern IntPtr CreateCompatibleBitmap(IntPtr hdcSrc, int width, int height);
        /// <summary>
        /// See MSDN Documentation
        /// </summary>
        /// <param name="HDCSource"></param>
        /// <returns></returns>
        [DllImportAttribute(DLLNAME, SetLastError = true)]
        static public extern IntPtr CreateCompatibleDC(IntPtr HDCSource);
        /// <summary>
        /// See MSDN Documentation
        /// </summary>
        /// <param name="driverName"></param>
        /// <param name="deviceName"></param>
        /// <param name="reserved"></param>
        /// <param name="initData"></param>
        /// <returns></returns>
        [DllImportAttribute(DLLNAME, SetLastError = true)]
        static public extern IntPtr CreateDC(string driverName, string deviceName, string reserved, IntPtr initData);
        /// <summary>
        /// See MSDN Documentation
        /// </summary>
        /// <param name="HDC"></param>
        /// <returns></returns>
        [DllImportAttribute(DLLNAME, SetLastError = true)]
        static public extern bool DeleteDC(IntPtr HDC);
        /// <summary>
        /// See MSDN Documentation
        /// </summary>
        /// <param name="hBMP"></param>
        /// <returns></returns>
        [DllImportAttribute(DLLNAME, SetLastError = true)]
        static public extern bool DeleteObject(IntPtr hBMP);
        /// <summary>
        /// See MSDN Documentation
        /// </summary>
        /// <param name="hBMP"></param>
        /// <param name="bufferSize"></param>
        /// <param name="buffer"></param>
        /// <returns></returns>
        [DllImportAttribute(DLLNAME, SetLastError = true)]
        static public extern long GetBitmapBits(IntPtr hBMP, long bufferSize, ref byte[] buffer);
        /// <summary>
        /// See MSDN Documentation
        /// </summary>
        /// <param name="HWND"></param>
        /// <returns></returns>
        [DllImportAttribute(DLLNAME, SetLastError = true)]
        static public extern IntPtr GetDC(IntPtr HWND);
        /// <summary>
        /// See MSDN Documentation
        /// </summary>
        /// <param name="HDC"></param>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        [DllImportAttribute(DLLNAME, SetLastError = true)]
        static public extern int GetPixel(IntPtr HDC, int x, int y);
        /// <summary>
        /// See MSDN Documentation
        /// </summary>
        /// <param name="HWND"></param>
        /// <param name="HDC"></param>
        /// <returns></returns>
        [DllImportAttribute(DLLNAME, SetLastError = true)]
        static public extern bool ReleaseDC(IntPtr HWND, IntPtr HDC);
        /// <summary>
        /// See MSDN Documentation
        /// </summary>
        /// <param name="HDC"></param>
        /// <param name="hgdiobj"></param>
        /// <returns></returns>
        [DllImportAttribute(DLLNAME, SetLastError = true)]
        static public extern IntPtr SelectObject(IntPtr HDC, IntPtr hgdiobj);
        /// <summary>
        /// See MSDN Documentation
        /// </summary>
        /// <param name="HDC"></param>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="color"></param>
        /// <returns></returns>
        [DllImportAttribute(DLLNAME, SetLastError = true)]
        static public extern int SetPixel(IntPtr HDC, int x, int y, int color);
        #endregion DLLImport APIs
    }
}
