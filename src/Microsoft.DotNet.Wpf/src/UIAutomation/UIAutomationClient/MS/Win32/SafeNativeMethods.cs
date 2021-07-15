// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// Description: Safe P/Invokes used by UIAutomation
//


using System.Runtime.InteropServices;
using System;
using System.Security;
using System.Collections;
using System.IO;
using System.Text;

namespace MS.Win32
{
    // This class *MUST* be internal for security purposes
    internal static class SafeNativeMethods
    {
        //
        // Current Process ID / Handle
        //

        [DllImport("user32.dll", ExactSpelling=true, CharSet=System.Runtime.InteropServices.CharSet.Auto)]
        public static extern int GetWindowThreadProcessId( NativeMethods.HWND hWnd, out int lpdwProcessId);

        //
        // GetSystemMetrics
        //

        public const int SM_CXMAXTRACK          = 59;
        public const int SM_CYMAXTRACK          = 60;
        public const int SM_XVIRTUALSCREEN      = 76;
        public const int SM_YVIRTUALSCREEN      = 77;
        public const int SM_CXVIRTUALSCREEN     = 78;
        public const int SM_CYVIRTUALSCREEN     = 79;
        public const int SM_SWAPBUTTON          = 23;

        public const int SM_CXHSCROLL           = 21;
        public const int SM_CYHSCROLL           = 3;


        [DllImport("user32.dll")]
        public static extern int GetSystemMetrics(int metric);



        //
        // GetGUIThreadInfo
        //

        public const int GUI_CARETBLINKING   = 0x00000001;
        public const int GUI_INMOVESIZE      = 0x00000002;
        public const int GUI_INMENUMODE      = 0x00000004;
        public const int GUI_SYSTEMMENUMODE  = 0x00000008;
        public const int GUI_POPUPMENUMODE   = 0x00000010;

        [StructLayout(LayoutKind.Sequential, CharSet=CharSet.Unicode)]
        public struct GUITHREADINFO
        {
            public int      cbSize;
            public int      dwFlags;
            public NativeMethods.HWND     hwndActive;
            public NativeMethods.HWND     hwndFocus;
            public NativeMethods.HWND     hwndCapture;
            public NativeMethods.HWND     hwndMenuOwner;
            public NativeMethods.HWND     hwndMoveSize;
            public NativeMethods.HWND     hwndCaret;
            public NativeMethods.RECT     rc;
        }

        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool GetGUIThreadInfo(int idThread, ref GUITHREADINFO guiThreadInfo);



        //
        // Window style information
        //

        public const int GWL_HINSTANCE  = -6;
        public const int GWL_ID         = -12;
        public const int GWL_STYLE      = -16;
        public const int GWL_EXSTYLE    = -20;

        public const int WS_MINIMIZE               = 0x20000000;
        public const int WS_MAXIMIZE               = 0x01000000;
        public const int WS_THICKFRAME             = 0x00040000;
        public const int WS_SYSMENU                = 0x00080000;
        public const int WS_BORDER                 = 0x00800000;
        public const int WS_DLGFRAME               = 0x00400000;
        public const int WS_CAPTION                = 0x00C00000;
        public const int WS_MINIMIZEBOX            = 0x00020000;
        public const int WS_MAXIMIZEBOX            = 0x00010000;
        public const int WS_DISABLED               = 0x08000000;
        public const int WS_CHILD                  = 0x40000000;
        public const int WS_POPUP                  = unchecked((int)0x80000000);

        public const int WS_EX_DLGMODALFRAME       = 0x00000001;
        public const int WS_EX_TOPMOST             = 0x00000008;
        public const int WS_EX_TRANSPARENT         = 0x00000020;
        public const int WS_EX_MDICHILD            = 0x00000040;
        public const int WS_EX_TOOLWINDOW          = 0x00000080;
        public const int WS_EX_APPWINDOW           = 0x00040000;
        public const int WS_EX_LAYERED             = 0x00080000;


        //
        // Window navigation
        //

        public const int GA_PARENT = 1;
        public const int GA_ROOT = 2;

        [DllImport("user32.dll", CharSet=CharSet.Auto)]
        public static extern NativeMethods.HWND GetAncestor( NativeMethods.HWND hwnd, int gaFlags );

        public const int GW_HWNDFIRST    = 0;
        public const int GW_HWNDLAST     = 1;
        public const int GW_HWNDNEXT     = 2;
        public const int GW_HWNDPREV     = 3;
        public const int GW_OWNER        = 4;
        public const int GW_CHILD        = 5;

        [DllImport("user32.dll", CharSet=CharSet.Auto)]
        public static extern NativeMethods.HWND GetDesktopWindow();

        [DllImport("user32.dll")]
        public static extern bool EnumThreadWindows(int dwThreadId, EnumThreadWndProc enumThreadWndProc, IntPtr lParam);

        // the delegate passed to USER for receiving an EnumThreadWndProc
        public delegate bool EnumThreadWndProc( NativeMethods.HWND hwnd, NativeMethods.HWND lParam);


        //
        // Other window information
        //

        [DllImport("user32.dll", ExactSpelling = true, SetLastError = true)]
        public static extern bool GetClientRect( NativeMethods.HWND hwnd, out NativeMethods.RECT rc );

        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool GetWindowRect( NativeMethods.HWND hwnd, out NativeMethods.RECT rc );

        [DllImport("user32.dll", CharSet=CharSet.Auto)]
        public static extern bool IsWindow( NativeMethods.HWND hwnd );

        [DllImport("user32.dll", CharSet=CharSet.Auto)]
        public static extern bool IsWindowEnabled( NativeMethods.HWND hwnd );

        [DllImport("user32.dll")]
        public static extern bool IsWindowVisible( NativeMethods.HWND hwnd );

        [DllImport("user32.dll")]
        public static extern bool IsIconic(NativeMethods.HWND hwnd);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern int GetClassName( NativeMethods.HWND hWnd, StringBuilder classname, int nMax );

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern int RealGetWindowClass( NativeMethods.HWND hWnd, StringBuilder classname, int nMax );

        [DllImport("user32.dll", CharSet=CharSet.Auto)]
        internal extern static bool IsChild( NativeMethods.HWND parent, NativeMethods.HWND child );

        public const int DWMWA_CLOAKED = 14;

        [DllImport("DwmApi.dll")]
        internal static extern int DwmGetWindowAttribute(
            NativeMethods.HWND hwnd,
            int dwAttributeToGet, //DWMWA_* values
            ref int pvAttributeValue,
            int cbAttribute);

        //
        // Regions
        //

        [DllImport("gdi32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        internal static extern IntPtr CreateRectRgn(int left, int top, int right, int bottom);

        [DllImport("user32.dll", CharSet=CharSet.Auto)]
        internal static extern int GetWindowRgn(IntPtr hwnd, IntPtr hrgn);

        [DllImport("gdi32.dll", CharSet = CharSet.Auto)]
        internal static extern bool PtInRegion(IntPtr hrgn, int x, int y);

        internal const int COMPLEXREGION = 3;

        //
        // Atoms
        //

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern short GlobalAddAtom( string lpString );

        //
        // Module Name
        //

        [DllImport("Psapi.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern int GetModuleFileNameEx(MS.Internal.Automation.SafeProcessHandle hProcess, IntPtr hModule, StringBuilder buffer, int length);

        //
        // Multi monitor function
        //
        public const int MONITOR_DEFAULTTONULL    = 0x00000000;

        [DllImport("user32.dll", CharSet=CharSet.Auto)]
        public static extern IntPtr MonitorFromRect( ref NativeMethods.RECT rect, int dwFlags );

        //
        // Scaling transforms
        //
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        public static extern bool PhysicalToLogicalPoint(NativeMethods.HWND hwnd, ref NativeMethods.POINT pt);

        //
        // Misc
        //
        [DllImport("kernel32.dll", CharSet = CharSet.Auto)]
        public static extern uint GetTickCount();

    }
}

