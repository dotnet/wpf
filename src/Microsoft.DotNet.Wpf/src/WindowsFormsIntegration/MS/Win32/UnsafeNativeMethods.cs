// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        
using System;
using System.Runtime.ConstrainedExecution;
using System.Runtime.InteropServices;
using System.Security;
using System.Runtime.Versioning;

namespace MS.Win32
{
    internal static class UnsafeNativeMethods
    {
        [DllImport(ExternDll.User32, ExactSpelling = true, CharSet = CharSet.Auto)]
        [ResourceExposure(ResourceScope.None)]
        public static extern bool IsChild(IntPtr hWndParent, IntPtr hwnd);

        [DllImport(ExternDll.User32, ExactSpelling = true, CharSet = CharSet.Auto)]
        [ResourceExposure(ResourceScope.None)]
        public static extern IntPtr GetFocus();

        [DllImport(ExternDll.User32, ExactSpelling = true, CharSet = CharSet.Auto)]
        [ResourceExposure(ResourceScope.None)]
        internal static extern IntPtr SetParent(IntPtr hWnd, IntPtr hWndParent);

        [DllImport(ExternDll.User32, ExactSpelling = true, CharSet = CharSet.Auto)] [return: MarshalAs(UnmanagedType.Bool)]
        [ResourceExposure(ResourceScope.None)]
        public static extern bool TranslateMessage([In, Out] ref System.Windows.Interop.MSG msg);

        [DllImport(ExternDll.User32, CharSet = CharSet.Auto)]
        [ResourceExposure(ResourceScope.None)]
        public static extern IntPtr DispatchMessage([In] ref System.Windows.Interop.MSG msg);

        [DllImport(ExternDll.User32, CharSet = CharSet.Auto, SetLastError = true)]
        [ResourceExposure(ResourceScope.None)]
        internal static extern IntPtr SendMessage(HandleRef hWnd, int msg, IntPtr wParam, IntPtr lParam);

        [DllImport(ExternDll.Gdi32, ExactSpelling = true, CharSet = CharSet.Auto)]
        [ResourceExposure(ResourceScope.None)]
        public static extern int GetDeviceCaps(DCSafeHandle hDC, int nIndex);

        [DllImport(ExternDll.Gdi32, EntryPoint = "CreateDC", CharSet = CharSet.Auto)]
        [ResourceExposure(ResourceScope.Process)]
        private static extern DCSafeHandle IntCreateDC(string lpszDriver, string lpszDeviceName, string lpszOutput, IntPtr devMode);

        [ResourceExposure(ResourceScope.Process)]
        [ResourceConsumption(ResourceScope.Process)]
        public static DCSafeHandle CreateDC(string lpszDriver)
        {
            return IntCreateDC(lpszDriver, null, null, IntPtr.Zero);
        }

        [DllImport(ExternDll.Gdi32, ExactSpelling = true, CharSet = CharSet.Auto)]
        #pragma warning disable SYSLIB0004 // The Constrained Execution Region (CER) feature is not supported.  
        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
        #pragma warning restore SYSLIB0004 // The Constrained Execution Region (CER) feature is not supported.  
        [ResourceExposure(ResourceScope.None)]
        public static extern bool DeleteDC(IntPtr hDC);
    }
}
