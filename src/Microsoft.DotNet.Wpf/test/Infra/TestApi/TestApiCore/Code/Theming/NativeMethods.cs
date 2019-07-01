// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.InteropServices;
using System.Text;

namespace Microsoft.Test.Theming
{
    internal static class NativeMethods
    {
        #region Const data

        private const string Gdi32Dll = "GDI32.dll";
        private const string User32Dll = "User32.dll";

        public const int SW_HIDE = 0;
        public const int SW_SHOWNORMAL = 1;
        public const int SW_NORMAL = 1;
        public const int SW_SHOWMINIMIZED = 2;
        public const int SW_SHOWMAXIMIZED = 3;
        public const int SW_MAXIMIZE = 3;
        public const int SW_SHOWNOACTIVATE = 4;
        public const int SW_SHOW = 5;
        public const int SW_MINIMIZE = 6;
        public const int SW_SHOWMINNOACTIVE = 7;
        public const int SW_SHOWNA = 8;
        public const int SW_RESTORE = 9;
        public const int SW_SHOWDEFAULT = 10;
        public const int SW_FORCEMINIMIZE = 11;
        public const int SW_MAX = 11;

        #endregion Const data

        #region Methods

        [DllImport(User32Dll)]
        public static extern IntPtr GetForegroundWindow();

        [DllImport(User32Dll)]
        public static extern bool BringWindowToTop(IntPtr hWnd);

        [DllImport(User32Dll, EntryPoint = "IsWindowVisible", PreserveSig = true)]
        public static extern bool IsWindowVisible(IntPtr hWnd);

        [DllImport(User32Dll)]
        public static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        [DllImport(User32Dll)]
        public static extern void SetForegroundWindow(IntPtr hWnd);

        [DllImport(User32Dll)]
        public static extern bool CloseWindow(IntPtr hWnd);

        [DllImport(User32Dll)]
        public static extern bool DestroyWindow(IntPtr hWnd);

        [DllImport(User32Dll, EntryPoint = "GetClassName")]
        public static extern int GetClassName(IntPtr hwnd, [MarshalAs(UnmanagedType.LPStr)] StringBuilder buf, int nMaxCount);

        [DllImport(User32Dll, EntryPoint = "GetWindowText")]
        public static extern int GetWindowText(IntPtr hwnd, [MarshalAs(UnmanagedType.LPStr)] StringBuilder buf, int nMaxCount);

        [DllImport(User32Dll, EntryPoint = "EnumThreadWindows", PreserveSig = true, SetLastError = true)]
        public static extern bool EnumThreadWindows(int threadId, EnumProcCallback callback, IntPtr lParam);

        [DllImport(User32Dll, EntryPoint = "EnumChildWindows", PreserveSig = true, SetLastError = true)]
        public static extern bool EnumChildWindows(IntPtr hWndParent, EnumProcCallback callback, IntPtr lParam);

        public delegate bool EnumProcCallback(IntPtr hwnd, IntPtr lParam);

        [DllImport(User32Dll)]
        public static extern int GetWindowThreadProcessId(IntPtr hWnd, out int processId);

        #endregion Methods
    }
}
