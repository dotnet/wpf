// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// Description: Unsafe P/Invokes used by UIAutomation

using System.Threading;
using System;
using Accessibility;
using System.Runtime.InteropServices;
using System.Collections;
using System.IO;
using System.Text;
using Microsoft.Win32.SafeHandles;
using System.Diagnostics;
using NativeMethodsSetLastError = MS.Internal.UIAutomationClientSideProviders.NativeMethodsSetLastError;

namespace MS.Win32
{
    internal static class UnsafeNativeMethods
    {
        [DllImport(ExternDll.Gdi32)]
        internal static extern int GetDeviceCaps(IntPtr hdc, int nIndex);
        [DllImport(ExternDll.Gdi32, SetLastError = true)]
        internal static extern int GetObjectW(IntPtr hObject, int size, ref NativeMethods.LOGFONT lf);
        [DllImport(ExternDll.Gdi32)]
        internal static extern IntPtr SelectObject(IntPtr hdc, IntPtr hObject);
        [DllImport(ExternDll.Gdi32)]
        internal static extern IntPtr GetStockObject(int nIndex);

        [DllImport(ExternDll.Kernel32, SetLastError = true)]
        internal static extern bool CloseHandle(IntPtr h);
        [DllImport(ExternDll.Kernel32, SetLastError = true)]
        internal static extern IntPtr OpenProcess(int flags, bool inherit, uint dwProcessId);

        [DllImport(ExternDll.Kernel32)]
        public static extern uint GetCurrentProcessId();
        [DllImport(ExternDll.Kernel32)]
        internal static extern void GetSystemInfo(out NativeMethods.SYSTEM_INFO SystemInfo);
        [DllImport(ExternDll.Kernel32, SetLastError = true)]
        internal static extern bool IsWow64Process(MS.Internal.AutomationProxies.SafeProcessHandle hProcess, out bool Wow64Process);
        [DllImport(ExternDll.Ntdll, CharSet = CharSet.Unicode)]
        public static extern int NtQueryInformationProcess(MS.Internal.AutomationProxies.SafeProcessHandle hProcess, int query, ref ulong info, int size, int[] returnedSize);

        internal const int ProcessWow64Information = 26;

        [DllImport(ExternDll.Kernel32, ExactSpelling = true, CharSet = CharSet.Auto)]
        internal static extern IntPtr GlobalLock(IntPtr handle);
        [DllImport(ExternDll.Kernel32, ExactSpelling = true, CharSet = CharSet.Auto)]
        internal static extern bool GlobalUnlock(IntPtr handle);

        [DllImport(ExternDll.Kernel32, SetLastError = true)]
        internal static extern IntPtr VirtualAlloc(IntPtr address, UIntPtr size, int allocationType, int protect);
        [DllImport(ExternDll.Kernel32, SetLastError = true)]
        internal static extern IntPtr VirtualAllocEx(MS.Internal.AutomationProxies.SafeProcessHandle hProcess, IntPtr address, UIntPtr size, int allocationType, int protect);
        [DllImport(ExternDll.Kernel32, SetLastError = true)]
        internal static extern bool VirtualFree(IntPtr address, UIntPtr size, int freeType);
        [DllImport(ExternDll.Kernel32, SetLastError = true)]
        internal static extern bool VirtualFreeEx(MS.Internal.AutomationProxies.SafeProcessHandle hProcess, IntPtr address, UIntPtr size, int freeType);

        internal const int PAGE_NOACCESS = 0x01;
        internal const int PAGE_READWRITE = 0x04;

        internal const int MEM_COMMIT = 0x1000;
        internal const int MEM_RELEASE = 0x8000;
        internal const int MEM_FREE = 0x10000;

        [DllImport(ExternDll.Kernel32, SetLastError = true)]
        internal static extern bool ReadProcessMemory(MS.Internal.AutomationProxies.SafeProcessHandle hProcess, IntPtr Source, IntPtr Dest, IntPtr /*SIZE_T*/ size, out IntPtr /*SIZE_T*/ bytesRead);
        [DllImport(ExternDll.Kernel32, SetLastError = true)]
        internal static extern bool ReadProcessMemory(MS.Internal.AutomationProxies.SafeProcessHandle hProcess, IntPtr Source, MS.Internal.AutomationProxies.SafeCoTaskMem destAddress, IntPtr /*SIZE_T*/ size, out IntPtr /*SIZE_T*/ bytesRead);
        [DllImport(ExternDll.Kernel32, SetLastError = true)]
        internal static extern bool WriteProcessMemory(MS.Internal.AutomationProxies.SafeProcessHandle hProcess, IntPtr Dest, IntPtr sourceAddress, IntPtr /*SIZE_T*/ size, out IntPtr /*SIZE_T*/ bytesWritten);

        [DllImport("oleacc.dll")]
        internal static extern int AccessibleChildren(Accessibility.IAccessible paccContainer, int iChildStart, int cChildren, [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 2), In, Out] object[] rgvarChildren, out int pcObtained);
        [DllImport("oleacc.dll")]
        internal static extern int AccessibleObjectFromWindow(IntPtr hwnd, int idObject, ref Guid iid, [In, Out, MarshalAs(UnmanagedType.IUnknown)] ref object ppvObject);
        [DllImport("oleacc.dll")]
        internal static extern int ObjectFromLresult(IntPtr lResult, ref Guid iid, IntPtr wParam, [In, Out] ref IAccessible ppvObject);
        [DllImport("oleacc.dll")]
        public static extern int WindowFromAccessibleObject(IAccessible acc, ref IntPtr hwnd);


        internal static Guid IID_IUnknown = new Guid(0x00000000, 0x0000, 0x0000, 0xc0, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x46);
        internal static Guid IID_IDispatch = new Guid(0x00020400, 0x0000, 0x0000, 0xC0, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x46);
        internal static Guid IID_IAccessible = new Guid(0x618736e0, 0x3c3d, 0x11cf, 0x81, 0x0c, 0x00, 0xaa, 0x00, 0x38, 0x9b, 0x71);

        [DllImport("oleacc.dll", SetLastError=true)]
        internal static extern IntPtr GetProcessHandleFromHwnd(IntPtr hwnd);


        //
        // COM
        //
        [DllImport("ole32.dll")]
        internal static extern void ReleaseStgMedium(ref STGMEDIUM medium);

        [StructLayout(LayoutKind.Sequential)]
        internal struct FORMATETC
        {
            internal short cfFormat;
            internal short dummy;
            internal IntPtr ptd;
            internal int dwAspect;
            internal int lindex;
            internal int tymed;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        internal struct STGMEDIUM
        {
            internal int tymed;
            internal IntPtr hGlobal;
            internal IntPtr pUnkForRelease;
        }

        [DllImport("user32.dll")]
        internal static extern IntPtr SetWinEventHook(int eventMin, int eventMax, IntPtr hmodWinEventProc, NativeMethods.WinEventProcDef WinEventReentrancyFilter, uint idProcess, uint idThread, int dwFlags);

        [DllImport("user32.dll")]
        internal static extern bool UnhookWinEvent(IntPtr winEventHook);

        //
        // Atoms Functions
        //
        [DllImport(ExternDll.Kernel32, CharSet = CharSet.Unicode, SetLastError = true)]
        internal static extern short GlobalAddAtom(string atomName);

        //
        // DLLs, Processes, and Threads: Synchronization Functions
        //
        [DllImport(ExternDll.User32, ExactSpelling = true, SetLastError = true)]
        internal static extern int MsgWaitForMultipleObjects(int nCount, IntPtr[] handles, bool fWaitAll, int dwMilliseconds, int dwWakeMask);

        //
        // Menus Functions
        //
        [DllImport(ExternDll.User32)]
        public static extern IntPtr GetMenu(IntPtr hwnd);
        [DllImport(ExternDll.User32)]
        public static extern IntPtr GetSubMenu(IntPtr hmenu, int nPos);
        [DllImport(ExternDll.User32)]
        public static extern int GetMenuState(IntPtr hmenu, int uIDCheckItem, int uCheck);
        [DllImport(ExternDll.User32, CharSet = CharSet.Unicode)]
        public static extern int GetMenuString (IntPtr hMenu, int uIDItem, StringBuilder lpString, int nMaxCount, uint uFlag);
        [DllImport (ExternDll.User32, CharSet = CharSet.Unicode)]
        public static extern int GetMenuString (IntPtr hMenu, int uIDItem, IntPtr lpString, int nMaxCount, uint uFlag);
        [DllImport(ExternDll.User32, SetLastError = true)]
        public static extern int GetMenuItemCount(IntPtr hmenu);
        [DllImport(ExternDll.User32)]
        public static extern int GetMenuItemID(IntPtr hmenu, int uCheck);
        [DllImport(ExternDll.User32, CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern bool GetMenuItemInfo(IntPtr hMenu, int uItem, bool fByPosition, [In, Out] ref NativeMethods.MENUITEMINFO menuItemInfo);
        [DllImport(ExternDll.User32, SetLastError = true)]
        public static extern bool GetMenuItemRect(IntPtr hWnd, IntPtr hMenu, int uItem, out NativeMethods.Win32Rect lprcItem);
        //[DllImport (ExternDll.User32)]
        //public static extern int HiliteMenuItem (IntPtr hmenu, int uIDCheckItem, int uCheck);

        //
        // Messages and Message Queues Functions
        //
        [DllImport(ExternDll.User32, CharSet = CharSet.Unicode)]
        internal static extern IntPtr DispatchMessage([In] ref NativeMethods.MSG msg);
        [DllImport(ExternDll.User32, CharSet = CharSet.Unicode, SetLastError = true)]
        internal static extern int GetMessage([In, Out] ref NativeMethods.MSG msg, IntPtr hWnd, int uMsgFilterMin, int uMsgFilterMax);
        [DllImport(ExternDll.User32, CharSet = CharSet.Unicode)]
        internal static extern bool PeekMessage([In, Out] ref NativeMethods.MSG msg, IntPtr hwnd, int uMsgFilterMin, int uMsgFilterMax, int wRemoveMsg);
        [DllImport(ExternDll.User32, CharSet = CharSet.Unicode, SetLastError = true)]
        internal static extern bool PostMessage(IntPtr hwnd, int msg, IntPtr wparam, IntPtr lparam);
        [DllImport(ExternDll.User32, CharSet = CharSet.Unicode, SetLastError = true)]
        internal static extern IntPtr SendMessageTimeout (IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, int flags, int uTimeout, out IntPtr pResult);
        [DllImport(ExternDll.User32, CharSet = CharSet.Unicode, SetLastError = true)]
        internal static extern IntPtr SendMessageTimeout (IntPtr hwnd, int uMsg, IntPtr wParam, StringBuilder lParam, int flags, int uTimeout, out IntPtr result);
        [DllImport(ExternDll.User32, CharSet = CharSet.Unicode, SetLastError = true)]
        internal static extern IntPtr SendMessageTimeout(IntPtr hwnd, int uMsg, IntPtr wParam, ref NativeMethods.Win32Rect lParam, int flags, int uTimeout, out IntPtr result);
        [DllImport(ExternDll.User32, CharSet = CharSet.Unicode, SetLastError = true)]
        internal static extern IntPtr SendMessageTimeout(IntPtr hwnd, int uMsg, IntPtr wParam, ref UnsafeNativeMethods.TITLEBARINFOEX lParam, int flags, int uTimeout, out IntPtr result);
        [DllImport(ExternDll.User32, CharSet = CharSet.Unicode, SetLastError = true)]
        internal static extern IntPtr SendMessageTimeout(IntPtr hwnd, int uMsg, out int wParam, out int lParam, int flags, int uTimeout, out IntPtr result);
        [DllImport(ExternDll.User32, CharSet = CharSet.Unicode)]
        internal static extern bool TranslateMessage([In, Out] ref NativeMethods.MSG msg);

        //
        // Multi monitor function
        //
        internal const int MONITOR_DEFAULTTONULL = 0x00000000;

        [DllImport(ExternDll.User32, CharSet = CharSet.Unicode)]
        internal static extern IntPtr MonitorFromRect(ref NativeMethods.Win32Rect rect, int dwFlags);

        //
        // Keyboard Input Functions
        //
        [DllImport(ExternDll.User32, ExactSpelling = true)]
        internal static extern short GetAsyncKeyState(int vkey);
        [DllImport(ExternDll.User32, CharSet = CharSet.Unicode, SetLastError = true)]
        internal static extern bool RegisterHotKey(IntPtr hwnd, int atom, int fsModifiers, int vk);
        [DllImport(ExternDll.User32, SetLastError = true)]
        internal static extern int SendInput (int nInputs, ref NativeMethods.INPUT ki, int cbSize);
        [DllImport(ExternDll.User32, CharSet = CharSet.Unicode)]
        internal static extern short VkKeyScan(char key);
        [DllImport(ExternDll.User32, CharSet = CharSet.Unicode, SetLastError = true)]
        internal static extern bool UnregisterHotKey(IntPtr hwnd, int atom);

        //
        // SendInput related
        //
        public const int VK_SHIFT = 0x10;
        public const int VK_CONTROL = 0x11;
        public const int VK_MENU = 0x12;
        public const int VK_RMENU = 0xA5;
        public const int VK_RCONTROL = 0xA3;
        public const int VK_NUMLOCK = 0x90;
        public const int VK_INSERT = 0x2D;
        public const int VK_DELETE = 0x2E;
        public const int VK_HOME = 0x24;
        public const int VK_END = 0x23;
        public const int VK_PRIOR = 0x21;
        public const int VK_NEXT = 0x22;
        public const int VK_UP = 0x26;
        public const int VK_DOWN = 0x28;
        public const int VK_LEFT = 0x25;
        public const int VK_RIGHT = 0x27;
        public const int VK_APPS = 0x5D;
        public const int VK_RWIN = 0x5C;
        public const int VK_LWIN = 0x5B;

        //
        // Combo Box Functions
        //
        [DllImport(ExternDll.User32, CharSet = CharSet.Unicode, ExactSpelling = true, SetLastError = true)]
        internal static extern bool GetComboBoxInfo(IntPtr hwnd, [In, Out] ref NativeMethods.COMBOBOXINFO cbInfo);

        //
        // Cursors Functions
        //
        [DllImport(ExternDll.User32, CharSet = CharSet.Auto, ExactSpelling = true, SetLastError = true)]
        internal static extern bool GetCursorPos([In, Out] ref NativeMethods.Win32Point pt);
        [DllImport(ExternDll.User32, CharSet = CharSet.Auto, ExactSpelling = true, SetLastError = true)]
        internal static extern bool GetPhysicalCursorPos([In, Out] ref NativeMethods.Win32Point pt);

        //
        // DWM Functions
        //
        [DllImport(ExternDll.DwmAPI, BestFitMapping = false)]
        public static extern int DwmIsCompositionEnabled(out int enabled);

        //
        // Scroll Bar Functions
        //
        [DllImport(ExternDll.User32, SetLastError = true)]
        internal static extern bool GetScrollBarInfo(IntPtr hwnd, int fnBar, [In, Out] ref NativeMethods.ScrollBarInfo lpsi);
        [DllImport(ExternDll.User32, SetLastError = true)]
        internal static extern bool GetScrollInfo(IntPtr hwnd, int fnBar, [In, Out] ref NativeMethods.ScrollInfo lpsi);

        //
        // Windows Functions
        //
        [DllImport(ExternDll.User32, ExactSpelling = true, SetLastError = true)]
        internal unsafe static extern bool EnumChildWindows(IntPtr hwndParent, NativeMethods.EnumChildrenCallbackVoid lpEnumFunc, void* lParam);
        [DllImport(ExternDll.User32, ExactSpelling = true)]
        internal static extern bool EnumThreadWindows(uint threadId, EnumThreadWndProc lpEnumFunc, [In, Out] ref ENUMTOOLTIPWINDOWINFO lParam);
        [DllImport(ExternDll.User32, ExactSpelling = true)]
        internal static extern IntPtr GetDesktopWindow();
        [DllImport(ExternDll.User32, ExactSpelling = true, SetLastError = true)]
        internal static extern bool GetClientRect(IntPtr hWnd, [In, Out] ref NativeMethods.Win32Rect rect);
        [DllImport(ExternDll.User32, SetLastError = true)]
        internal static extern bool GetGUIThreadInfo(uint idThread, ref NativeMethods.GUITHREADINFO guiThreadInfo);
        [DllImport(ExternDll.User32, CharSet = CharSet.Unicode, ExactSpelling = true, SetLastError = true)]
        public static extern bool GetTitleBarInfo(IntPtr hwnd, ref TITLEBARINFO pti);
        [DllImport(ExternDll.User32, ExactSpelling = true, SetLastError = true)]
        internal static extern bool GetWindowRect(IntPtr hWnd, [In, Out] ref NativeMethods.Win32Rect rect);
        [DllImport(ExternDll.User32, ExactSpelling = true)]
        internal static extern uint GetWindowThreadProcessId(IntPtr hwnd, out uint dwProcessId);
        [DllImport(ExternDll.User32, ExactSpelling = true)]
        internal static extern bool IsWindow(IntPtr hWnd);
        [DllImport(ExternDll.User32, CharSet = CharSet.Unicode, SetLastError = true)]
        internal static extern uint RealGetWindowClass(IntPtr hwnd, StringBuilder className, uint maxCount);
        [DllImport(ExternDll.User32, ExactSpelling = true)]
        internal static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport(ExternDll.User32, ExactSpelling = true, SetLastError = true)]
        internal static extern bool GetAltTabInfo(IntPtr hwnd, int iItem, ref ALTTABINFO pati,
           StringBuilder pszItemText, uint cchItemText);

        internal struct ALTTABINFO
        {
            internal uint cbSize;
            internal int cItems;
            internal int cColumns;
            internal int cRows;
            internal int iColFocus;
            internal int iRowFocus;
            internal int cxItem;
            internal int cyItem;
            internal NativeMethods.Win32Point ptStart;
        }

        private struct POINTSTRUCT
        {
            public int x;
            public int y;

            public POINTSTRUCT(int x, int y)
            {
                this.x = x;
                this.y = y;
            }
        }

        [DllImport(ExternDll.User32, EntryPoint = "WindowFromPoint", ExactSpelling = true, CharSet = CharSet.Auto)]
        private static extern IntPtr IntWindowFromPoint(POINTSTRUCT pt);

        [DllImport(ExternDll.User32, EntryPoint = "WindowFromPhysicalPoint", ExactSpelling = true, CharSet = CharSet.Auto)]
        private static extern IntPtr IntWindowFromPhysicalPoint(POINTSTRUCT pt);

        public static IntPtr WindowFromPhysicalPoint(int x, int y)
        {
            POINTSTRUCT ps = new POINTSTRUCT(x, y);
            if (System.Environment.OSVersion.Version.Major >= 6)
                return IntWindowFromPhysicalPoint(ps);
            else
                return IntWindowFromPoint(ps);
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct TITLEBARINFO
        {
            internal int cbSize;
            internal NativeMethods.Win32Rect rcTitleBar;
            [MarshalAs(System.Runtime.InteropServices.UnmanagedType.ByValArray, SizeConst = NativeMethods.CCHILDREN_TITLEBAR + 1)]
            internal int[] rgstate;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct TITLEBARINFOEX
        {
            internal int cbSize;
            internal NativeMethods.Win32Rect rcTitleBar;
            [MarshalAs(System.Runtime.InteropServices.UnmanagedType.ByValArray, SizeConst = NativeMethods.CCHILDREN_TITLEBAR + 1)]
            internal int[] rgstate;
            [MarshalAs(System.Runtime.InteropServices.UnmanagedType.ByValArray, SizeConst = NativeMethods.CCHILDREN_TITLEBAR + 1)]
            internal NativeMethods.Win32Rect[] rgrect;
        }

        // the delegate passed to USER for receiving an EnumThreadWndProc
        internal delegate bool EnumThreadWndProc(IntPtr hwnd, ref ENUMTOOLTIPWINDOWINFO lParam);

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        internal struct ENUMTOOLTIPWINDOWINFO
        {
            internal IntPtr hwnd;
            internal int id;
            internal string name;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        internal struct ENUMCHILDWINDOWFROMRECT
        {
            internal IntPtr hwnd;
            internal NativeMethods.Win32Rect rc;
        }

        //
        // Window Class Functions
        //

        [DllImport(ExternDll.User32, CharSet = CharSet.Unicode, SetLastError = true)]
        internal static extern int GetClassName(IntPtr hwnd, StringBuilder className, int maxCount);

        // Note: GetWindowLong is used in the win32 proxy only to retrieve the style
        // The wrapper is required as the signature for the function in 32 bits and 64 bit is different.

        internal static Int32 GetWindowLong(IntPtr hWnd, int nIndex, out int error)
        {
            int iResult = 0;
            IntPtr result = IntPtr.Zero;
            error = 0;

            if (IntPtr.Size == 4)
            {
                // use GetWindowLong
                iResult = NativeMethodsSetLastError.GetWindowLong(hWnd, nIndex);
                error = Marshal.GetLastWin32Error();
                result = new IntPtr(iResult);
            }
            else
            {
                // use GetWindowLongPtr
                result = NativeMethodsSetLastError.GetWindowLongPtr(hWnd, nIndex);
                error = Marshal.GetLastWin32Error();
                iResult = NativeMethods.IntPtrToInt32(result);
            }

            if ((result == IntPtr.Zero) && (error != 0))
            {
                // To be consistent with out other PInvoke wrappers
                // we should "throw" here.  But we don't want to
                // introduce new "throws" w/o time to follow up on any
                // new problems that causes.
                Debug.WriteLine("GetWindowLong failed.  Error = " + error);
                // throw new System.ComponentModel.Win32Exception(error);
            }

            return iResult;
        }


        //
        // Window Property Functions
        //

        [DllImport(ExternDll.User32, CharSet = CharSet.Unicode)]
        internal static extern IntPtr GetProp(IntPtr hwnd, string name);

        //
        // Windows GDI: Coordinate Space and Transformation Functions
        //

        //[DllImport(ExternDll.User32, SetLastError = true)]
        //internal static extern bool ClientToScreen(IntPtr hWnd, [In, Out] ref NativeMethods.Win32Point pt);
        //[DllImport(ExternDll.User32, ExactSpelling = true, SetLastError = true)]
        //internal static extern int ScreenToClient(IntPtr hWnd, [In, Out] ref NativeMethods.Win32Point pt);

        // NOTE: Use MapWindowPoints() instead of ClientToScreen or ScreenToClient to work corrently
        // on RTL OS's

        //
        // Windows GDI: Device Context Functions
        //

        [DllImport(ExternDll.User32, ExactSpelling = true, SetLastError = true)]
        internal static extern IntPtr GetDC(IntPtr hWnd);
        [DllImport(ExternDll.User32, ExactSpelling = true)]
        internal static extern int ReleaseDC(IntPtr hWnd, IntPtr hDC);

        //
        // Windows System Information: System Information Functions
        //

        [DllImport(ExternDll.User32, ExactSpelling = true)]
        internal static extern int GetSystemMetrics(int nIndex);

        //
        // SysTabControl32 constants and strucs
        //

        [StructLayout(LayoutKind.Sequential)]
        public struct TCHITTESTINFO
        {
            public NativeMethods.Win32Point pt;
            public uint flags;
        }


        //
        // Win32 Hyperlink strucs
        //

        [StructLayout(LayoutKind.Sequential)]
        public struct LITEM
        {
            // WARNING: Layout is important (see below)
            public uint mask;

            public int iLink;
            public int state;
            public int stateMask;
            public IntPtr szID;
            public IntPtr szURL;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct LHITTESTINFO
        {
            // WARNING:  Layout is important
            public NativeMethods.Win32Point pt;

            public LITEM item;
        }
    }
}

