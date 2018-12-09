// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//---------------------------------------------------------------------------
// Description: P/Invokes for methods that need to call SetLastError(0)
//---------------------------------------------------------------------------

// The NativeMethodsSetLastError class differs between assemblies and could not actually be
//  shared, so it is duplicated across namespaces to prevent name collision.
#if WINDOWS_BASE
namespace MS.Internal.WindowsBase
#elif UIAUTOMATIONCLIENT
namespace MS.Internal.UIAutomationClient
#elif UIAUTOMATIONCLIENTSIDEPROVIDERS
namespace MS.Internal.UIAutomationClientSideProviders
#elif WINDOWSFORMSINTEGRATION
namespace MS.Internal.WinFormsIntegration
#elif UIAUTOMATIONTYPES
namespace MS.Internal.UIAutomationTypes
#elif DRT
namespace MS.Internal.Drt
#else
#error Class is being used from an unknown assembly.
#endif
{
    using System;
    using System.Diagnostics.CodeAnalysis;
    using System.IO;
    using System.Security;
    using System.Security.Permissions;
    using System.Runtime.InteropServices;
    using System.Text;
    using System.Windows;
    using MS.Win32;

    [SuppressUnmanagedCodeSecurity]
    public static class NativeMethodsSetLastError
    {
#if WINDOWSFORMSINTEGRATION     // WinFormsIntegration

        [SuppressMessage("Microsoft.Security", "CA2118:ReviewSuppressUnmanagedCodeSecurityUsage")]
        [DllImport(ExternDll.PresentationNativeDll, EntryPoint="EnableWindowWrapper", SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Auto)]
        public static extern bool EnableWindow(IntPtr hWnd, bool enable);

#elif UIAUTOMATIONCLIENT || UIAUTOMATIONCLIENTSIDEPROVIDERS   // UIAutomation

        [SuppressMessage("Microsoft.Security", "CA2118:ReviewSuppressUnmanagedCodeSecurityUsage")]
        [DllImport(ExternDll.PresentationNativeDll, EntryPoint="GetWindowLongWrapper", CharSet=CharSet.Auto, SetLastError=true)]
        public static extern Int32 GetWindowLong(IntPtr hWnd, int nIndex );

        [SuppressMessage("Microsoft.Security", "CA2118:ReviewSuppressUnmanagedCodeSecurityUsage")]
        [DllImport(ExternDll.PresentationNativeDll, EntryPoint="GetWindowLongPtrWrapper", CharSet=CharSet.Auto, SetLastError=true)]
        public static extern IntPtr GetWindowLongPtr(IntPtr hWnd, int nIndex );

        [DllImport(ExternDll.PresentationNativeDll, EntryPoint="GlobalDeleteAtomWrapper", ExactSpelling = true, SetLastError = true)]
        public static extern short GlobalDeleteAtom(short atom);

#if UIAUTOMATIONCLIENT  // UIAutomationClient

        [DllImport(ExternDll.PresentationNativeDll, EntryPoint="GetMenuBarInfoWrapper", SetLastError = true)]
        public static extern bool GetMenuBarInfo (IntPtr hwnd, int idObject, uint idItem, ref UnsafeNativeMethods.MENUBARINFO mbi);

        [DllImport(ExternDll.PresentationNativeDll, EntryPoint="GetWindowWrapper", ExactSpelling = true, SetLastError = true)]
        public static extern NativeMethods.HWND GetWindow(NativeMethods.HWND hWnd, int uCmd);

        [DllImport(ExternDll.PresentationNativeDll, EntryPoint="MapWindowPointsWrapper", SetLastError = true, ExactSpelling=true, CharSet=CharSet.Auto)]
        public static extern int MapWindowPoints(NativeMethods.HWND hWndFrom, NativeMethods.HWND hWndTo, [In, Out] ref NativeMethods.RECT rect, int cPoints);

        [DllImport(ExternDll.PresentationNativeDll, EntryPoint="MapWindowPointsWrapper", SetLastError = true, ExactSpelling=true, CharSet=CharSet.Auto)]
        public static extern int MapWindowPoints(NativeMethods.HWND hWndFrom, NativeMethods.HWND hWndTo, [In, Out] ref NativeMethods.POINT pt, int cPoints);

#elif UIAUTOMATIONCLIENTSIDEPROVIDERS   // UIAutomationClientSideProviders

        [DllImport(ExternDll.PresentationNativeDll, EntryPoint="GetAncestorWrapper", CharSet = CharSet.Auto)]
        public static extern IntPtr GetAncestor(IntPtr hwnd, int gaFlags);

        [DllImport(ExternDll.PresentationNativeDll, EntryPoint="FindWindowExWrapper", CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern IntPtr FindWindowEx(IntPtr hwndParent, IntPtr hwndChildAfter, string className, string wndName);

        [DllImport(ExternDll.PresentationNativeDll, EntryPoint="GetMenuBarInfoWrapper", SetLastError = true)]
        public static extern bool GetMenuBarInfo (IntPtr hwnd, int idObject, uint idItem, ref NativeMethods.MENUBARINFO mbi);

        [DllImport(ExternDll.PresentationNativeDll, EntryPoint="GetTextExtentPoint32Wrapper", SetLastError = true)]
        public static extern int GetTextExtentPoint32(IntPtr hdc, [MarshalAs(UnmanagedType.LPWStr)]string lpString, int cbString, out NativeMethods.SIZE lpSize);

        [DllImport(ExternDll.PresentationNativeDll, EntryPoint="GetWindowWrapper", ExactSpelling = true, SetLastError = true)]
        public static extern IntPtr GetWindow(IntPtr hWnd, int uCmd);

        [DllImport(ExternDll.PresentationNativeDll, EntryPoint = "GetWindowTextWrapper", CharSet=CharSet.Auto, BestFitMapping = false, SetLastError = true)]
        public static extern int GetWindowText(IntPtr hWnd, [Out] StringBuilder lpString, int nMaxCount);

        [DllImport(ExternDll.PresentationNativeDll, EntryPoint="MapWindowPointsWrapper", ExactSpelling = true, SetLastError = true)]
        public static extern int MapWindowPoints(IntPtr hWndFrom, IntPtr hWndTo, [In, Out] ref NativeMethods.Win32Rect rect, int cPoints);

        [DllImport(ExternDll.PresentationNativeDll, EntryPoint="MapWindowPointsWrapper", ExactSpelling = true, SetLastError = true)]
        public static extern int MapWindowPoints(IntPtr hWndFrom, IntPtr hWndTo, [In, Out] ref NativeMethods.Win32Point pt, int cPoints);

        [DllImport(ExternDll.PresentationNativeDll, EntryPoint="SetScrollPosWrapper", SetLastError = true)]
        public static extern int SetScrollPos(IntPtr hWnd, int nBar, int nPos, bool bRedraw);

#endif
#else       // Base/Core/FW + DRT

        [DllImport(ExternDll.PresentationNativeDll, EntryPoint="EnableWindowWrapper", SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Auto)]
        public static extern bool EnableWindow(HandleRef hWnd, bool enable);

        [DllImport(ExternDll.PresentationNativeDll, EntryPoint="GetAncestorWrapper", CharSet = CharSet.Auto)]
        public static extern IntPtr GetAncestor(IntPtr hwnd, int gaFlags);

        [DllImport(ExternDll.PresentationNativeDll, EntryPoint="GetKeyboardLayoutListWrapper", SetLastError = true, ExactSpelling=true, CharSet=CharSet.Auto)]
        public static extern int GetKeyboardLayoutList(int size, [Out, MarshalAs(UnmanagedType.LPArray)] IntPtr[] hkls);

        [DllImport(ExternDll.PresentationNativeDll, EntryPoint="GetParentWrapper", SetLastError = true)]
        public static extern IntPtr GetParent(HandleRef hWnd);

        [DllImport(ExternDll.PresentationNativeDll, EntryPoint="GetWindowWrapper", ExactSpelling = true, SetLastError = true)]
        public static extern IntPtr GetWindow(IntPtr hWnd, int uCmd);

        [DllImport(ExternDll.PresentationNativeDll, EntryPoint="GetWindowLongWrapper", CharSet=CharSet.Auto, SetLastError=true)]
        public static extern Int32 GetWindowLong(HandleRef hWnd, int nIndex );

        [DllImport(ExternDll.PresentationNativeDll, EntryPoint="GetWindowLongWrapper", CharSet=CharSet.Auto, SetLastError=true)]
        public static extern Int32 GetWindowLong(IntPtr hWnd, int nIndex );

        [DllImport(ExternDll.PresentationNativeDll, EntryPoint="GetWindowLongWrapper", CharSet=CharSet.Auto, SetLastError=true)]
        public static extern NativeMethods.WndProc GetWindowLongWndProc(HandleRef hWnd, int nIndex);

        [DllImport(ExternDll.PresentationNativeDll, EntryPoint="GetWindowLongPtrWrapper", CharSet=CharSet.Auto, SetLastError=true)]
        public static extern IntPtr GetWindowLongPtr(IntPtr hWnd, int nIndex);

        [DllImport(ExternDll.PresentationNativeDll, EntryPoint="GetWindowLongPtrWrapper", CharSet=CharSet.Auto, SetLastError=true)]
        public static extern IntPtr GetWindowLongPtr(HandleRef hWnd, int nIndex);

        [DllImport(ExternDll.PresentationNativeDll, EntryPoint="GetWindowLongPtrWrapper", CharSet=CharSet.Auto, SetLastError=true)]
        public static extern NativeMethods.WndProc GetWindowLongPtrWndProc(HandleRef hWnd, int nIndex);

        [DllImport(ExternDll.PresentationNativeDll, EntryPoint = "GetWindowTextWrapper", CharSet=CharSet.Auto, BestFitMapping = false, SetLastError = true)]
        public static extern int GetWindowText(HandleRef hWnd, [Out] StringBuilder lpString, int nMaxCount);

        [DllImport(ExternDll.PresentationNativeDll, EntryPoint = "GetWindowTextLengthWrapper", CharSet = System.Runtime.InteropServices.CharSet.Auto, SetLastError = true)]
        public static extern int GetWindowTextLength(HandleRef hWnd);

        [DllImport(ExternDll.PresentationNativeDll, EntryPoint="MapWindowPointsWrapper", SetLastError = true, ExactSpelling=true, CharSet=CharSet.Auto)]
        public static extern int MapWindowPoints(HandleRef hWndFrom, HandleRef hWndTo, [In, Out] ref NativeMethods.RECT rect, int cPoints);

        [DllImport(ExternDll.PresentationNativeDll, EntryPoint="SetFocusWrapper", SetLastError = true)]
        public static extern IntPtr SetFocus(HandleRef hWnd);

        [DllImport(ExternDll.PresentationNativeDll, EntryPoint="SetWindowLongWrapper", CharSet=CharSet.Auto)]
        public static extern Int32 SetWindowLong(HandleRef hWnd, int nIndex, Int32 dwNewLong);

        [DllImport(ExternDll.PresentationNativeDll, EntryPoint="SetWindowLongWrapper", CharSet=CharSet.Auto)]
        public static extern Int32 SetWindowLong(IntPtr hWnd, int nIndex, Int32 dwNewLong);

        [DllImport(ExternDll.PresentationNativeDll, EntryPoint="SetWindowLongWrapper", CharSet=CharSet.Auto, SetLastError=true)]
        public static extern Int32 SetWindowLongWndProc(HandleRef hWnd, int nIndex, NativeMethods.WndProc dwNewLong);

        [DllImport(ExternDll.PresentationNativeDll, EntryPoint="SetWindowLongPtrWrapper", CharSet=CharSet.Auto)]
        public static extern IntPtr SetWindowLongPtr(HandleRef hWnd, int nIndex, IntPtr dwNewLong);

        [DllImport(ExternDll.PresentationNativeDll, EntryPoint="SetWindowLongPtrWrapper", CharSet=CharSet.Auto)]
        public static extern IntPtr SetWindowLongPtr(IntPtr hWnd, int nIndex, IntPtr dwNewLong);

        [DllImport(ExternDll.PresentationNativeDll, EntryPoint="SetWindowLongPtrWrapper", CharSet=CharSet.Auto, SetLastError=true)]
        public static extern IntPtr SetWindowLongPtrWndProc(HandleRef hWnd, int nIndex, NativeMethods.WndProc dwNewLong);

#endif

    }
}
