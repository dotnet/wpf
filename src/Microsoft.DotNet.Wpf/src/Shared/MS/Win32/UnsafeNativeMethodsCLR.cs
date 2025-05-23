﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Accessibility;
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using System.Text;
using System.ComponentModel;
#if !DRT && !UIAUTOMATIONTYPES
using MS.Internal.Interop;
using MS.Utility;
#endif

// DRTs cannot access MS.Internal
#if !DRT && !UIAUTOMATIONTYPES
using HR = MS.Internal.Interop.HRESULT;
#endif

//The SecurityHelper class differs between assemblies and could not actually be
// shared, so it is duplicated across namespaces to prevent name collision.
#if WINDOWS_BASE
using IComDataObject = System.Runtime.InteropServices.ComTypes.IDataObject;

#elif PRESENTATION_CORE
using MS.Internal.PresentationCore;
#elif PRESENTATIONFRAMEWORK
using MS.Internal.PresentationFramework;
#elif UIAUTOMATIONTYPES
using MS.Internal.UIAutomationTypes;
using IComDataObject = System.Runtime.InteropServices.ComTypes.IDataObject;

#elif DRT
using MS.Internal.Drt;
#else
#error Attempt to use a class (duplicated across multiple namespaces) from an unknown assembly.
#endif

namespace MS.Win32
{
    internal partial class UnsafeNativeMethods
    {

        [DllImport(ExternDll.Uxtheme, CharSet = CharSet.Auto, BestFitMapping = false)]
        public static extern int GetCurrentThemeName(StringBuilder pszThemeFileName, int dwMaxNameChars, StringBuilder pszColorBuff, int dwMaxColorChars, StringBuilder pszSizeBuff, int cchMaxSizeChars);

#if !DRT && !UIAUTOMATIONTYPES
        [DllImport(ExternDll.User32, CharSet = CharSet.Auto, BestFitMapping = false)]
        public static extern WindowMessage RegisterWindowMessage(string msg);
#endif

        [DllImport(ExternDll.User32, EntryPoint = "SetWindowPos", ExactSpelling = true, CharSet = CharSet.Auto, SetLastError = true)]
        public static extern bool SetWindowPos(HandleRef hWnd, HandleRef hWndInsertAfter, int x, int y, int cx, int cy, int flags);

        [DllImport(ExternDll.User32, ExactSpelling = true, CharSet = CharSet.Auto, SetLastError = true)]
        public static extern IntPtr GetWindow(HandleRef hWnd, int uCmd);

        [DllImport(ExternDll.Shcore, CharSet = CharSet.Auto, SetLastError = true)]
        public static extern uint GetDpiForMonitor(HandleRef hMonitor, NativeMethods.MONITOR_DPI_TYPE dpiType, out uint dpiX, out uint dpiY);

        [DllImport(ExternDll.User32, EntryPoint = "IsProcessDPIAware", CharSet = CharSet.Auto, SetLastError = true)]
        internal static extern bool IsProcessDPIAware();

        [DllImport(ExternDll.Kernel32, CharSet = CharSet.Auto, SetLastError = true)]
        public static extern IntPtr OpenProcess(int dwDesiredAccess, bool fInherit, int dwProcessId);

        [DllImport(ExternDll.User32, EntryPoint = "EnableNonClientDpiScaling", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern bool EnableNonClientDpiScaling(HandleRef hWnd);

        [DllImport(ExternDll.User32, SetLastError = true, CharSet = CharSet.Auto, BestFitMapping = false)]
        public static extern int GetClassName(HandleRef hwnd, StringBuilder lpClassName, int nMaxCount);

        [DllImport(ExternDll.User32, SetLastError = true, CharSet = CharSet.Auto, BestFitMapping = false)]
        public static extern int MessageBox(HandleRef hWnd, string text, string caption, int type);

        [DllImport(ExternDll.Uxtheme, CharSet = CharSet.Auto, BestFitMapping = false, EntryPoint = "SetWindowTheme")]
        public static extern int CriticalSetWindowTheme(HandleRef hWnd, string subAppName, string subIdList);

        [DllImport(ExternDll.Gdi32, SetLastError = true, ExactSpelling = true, EntryPoint = "CreateCompatibleBitmap", CharSet = CharSet.Auto)]
        public static extern IntPtr CriticalCreateCompatibleBitmap(HandleRef hDC, int width, int height);

        [DllImport(ExternDll.Gdi32, EntryPoint = "GetStockObject", SetLastError = true, CharSet = CharSet.Auto)]
        public static extern IntPtr CriticalGetStockObject(int stockObject);

        [DllImport(ExternDll.User32, EntryPoint = "FillRect", SetLastError = true, CharSet = CharSet.Auto)]
        public static extern int CriticalFillRect(IntPtr hdc, ref NativeMethods.RECT rcFill, IntPtr brush);

        [DllImport(ExternDll.Gdi32, SetLastError = true, ExactSpelling = true, CharSet = CharSet.Auto)]
        public static extern int GetBitmapBits(HandleRef hbmp, int cbBuffer, byte[] lpvBits);

        [DllImport(ExternDll.User32, ExactSpelling = true, CharSet = CharSet.Auto)]
        public static extern bool ShowWindow(HandleRef hWnd, int nCmdShow);

        public static void DeleteObject(HandleRef hObject)
        {
            HandleCollector.Remove((IntPtr)hObject, NativeMethods.CommonHandles.GDI);

            if (!IntDeleteObject(hObject))
            {
                throw new Win32Exception();
            }
        }

        [DllImport(ExternDll.Gdi32, SetLastError = true, ExactSpelling = true, EntryPoint = "DeleteObject", CharSet = CharSet.Auto)]
        public static extern bool IntDeleteObject(HandleRef hObject);

        [DllImport(ExternDll.Gdi32, EntryPoint = "SelectObject", SetLastError = true, ExactSpelling = true, CharSet = CharSet.Auto)]
        public static extern IntPtr CriticalSelectObject(HandleRef hdc, IntPtr obj);

        [DllImport(ExternDll.User32, EntryPoint = "PrintWindow", SetLastError = true, ExactSpelling = true, CharSet = CharSet.Auto)]
        public static extern bool CriticalPrintWindow(HandleRef hWnd, HandleRef hDC, int flags);

        [DllImport(ExternDll.User32, EntryPoint = "RedrawWindow", ExactSpelling = true, CharSet = CharSet.Auto)]
        public static extern bool CriticalRedrawWindow(HandleRef hWnd, IntPtr lprcUpdate, IntPtr hrgnUpdate, int flags);

        [DllImport(ExternDll.Shell32, CharSet = CharSet.Auto, BestFitMapping = false)]
        public static extern IntPtr ShellExecute(HandleRef hwnd, string lpOperation, string lpFile, string lpParameters, string lpDirectory, int nShowCmd);

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        internal class ShellExecuteInfo
        {
            public int cbSize;
            public ShellExecuteFlags fMask;
            public IntPtr hwnd;
            public string lpVerb;
            public string lpFile;
            public string lpParameters;
            public string lpDirectory;
            public int nShow;
            public IntPtr hInstApp;
            public IntPtr lpIDList;
            public string lpClass;
            public IntPtr hkeyClass;
            public int dwHotKey;
            public IntPtr hIcon;
            public IntPtr hProcess;
        }

        [Flags]
        internal enum ShellExecuteFlags
        {
            SEE_MASK_CLASSNAME = 0x00000001,
            SEE_MASK_CLASSKEY = 0x00000003,
            SEE_MASK_NOCLOSEPROCESS = 0x00000040,
            SEE_MASK_FLAG_DDEWAIT = 0x00000100,
            SEE_MASK_DOENVSUBST = 0x00000200,
            SEE_MASK_FLAG_NO_UI = 0x00000400,
            SEE_MASK_UNICODE = 0x00004000,
            SEE_MASK_NO_CONSOLE = 0x00008000,
            SEE_MASK_ASYNCOK = 0x00100000,
            SEE_MASK_HMONITOR = 0x00200000,
            SEE_MASK_NOZONECHECKS = 0x00800000,
            SEE_MASK_NOQUERYCLASSSTORE = 0x01000000,
            SEE_MASK_WAITFORINPUTIDLE = 0x02000000
        };

        [DllImport(ExternDll.Shell32, CharSet = CharSet.Unicode, SetLastError = true)]
        internal static extern bool ShellExecuteEx([In, Out] ShellExecuteInfo lpExecInfo);

        public const int MB_PRECOMPOSED = 0x00000001;
        public const int MB_USEGLYPHCHARS = 0x00000004;

        [DllImport(ExternDll.Kernel32, ExactSpelling = true, CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern unsafe int MultiByteToWideChar(int CodePage, int dwFlags, byte* lpMultiByteStr, int cchMultiByte, char* lpWideCharStr, int cchWideChar);

#if DRT_NATIVEMETHODS
        [DllImport(ExternDll.User32, ExactSpelling=true, EntryPoint="keybd_event", CharSet=CharSet.Auto)]
        public static extern void Keybd_event(byte vk, byte scan, int flags, IntPtr extrainfo);
#endif

#if !DRT && !UIAUTOMATIONTYPES
        [DllImport(ExternDll.Kernel32, EntryPoint = "GetModuleFileName", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern int IntGetModuleFileName(HandleRef hModule, StringBuilder buffer, int length);

        internal static string GetModuleFileName(HandleRef hModule)
        {
            // .Net is currently far behind Windows with regard to supporting paths longer than MAX_PATH.
            // At one point it was tested trying to load UNC paths longer than MAX_PATH and mscorlib threw
            // FileIOExceptions before WPF was even on the stack.
            // All the same, we still want to have this grow-and-retry logic because the CLR can be hosted
            // in a native application.  Callers bothering to use this rather than Assembly based reflection
            // are likely doing so because of (at least the potential for) the returned name referring to a
            // native module.
            StringBuilder buffer = new StringBuilder(Win32Constant.MAX_PATH);
            while (true)
            {
                int size = IntGetModuleFileName(hModule, buffer, buffer.Capacity);
                if (size == 0)
                {
                    throw new Win32Exception();
                }

                // GetModuleFileName returns nSize when it's truncated but does NOT set the last error.
                // MSDN documentation says this has changed in Windows 2000+.
                if (size == buffer.Capacity)
                {
                    // Enlarge the buffer and try again.
                    buffer.EnsureCapacity(buffer.Capacity * 2);
                    continue;
                }

                return buffer.ToString();
            }
        }
#endif


#if BASE_NATIVEMETHODS
        [DllImport(ExternDll.User32, ExactSpelling = true, CharSet = CharSet.Auto)]
        public static extern bool TranslateMessage([In, Out] ref System.Windows.Interop.MSG msg);


        [DllImport(ExternDll.User32, CharSet = CharSet.Auto)]
        public static extern IntPtr DispatchMessage([In] ref System.Windows.Interop.MSG msg);
#endif

        [DllImport("oleacc.dll")]
        internal static extern int ObjectFromLresult(IntPtr lResult, ref Guid iid, IntPtr wParam, [In, Out] ref IAccessible ppvObject);

        [DllImport("user32.dll")]
        internal static extern bool IsWinEventHookInstalled(int winevent);

        [DllImport(ExternDll.Ole32, EntryPoint = "OleInitialize")]
        private static extern int IntOleInitialize(IntPtr val);

        public static int OleInitialize()
        {
            return IntOleInitialize(IntPtr.Zero);
        }

        [DllImport(ExternDll.User32, ExactSpelling = true, CharSet = CharSet.Auto)]
        public static extern bool EnumThreadWindows(int dwThreadId, NativeMethods.EnumThreadWindowsCallback lpfn, HandleRef lParam);

        [DllImport(ExternDll.Ole32, ExactSpelling = true, CharSet = CharSet.Auto, SetLastError = true)]
        public static extern int OleUninitialize();

        [DllImport(ExternDll.Kernel32, EntryPoint = "CloseHandle", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern bool IntCloseHandle(HandleRef handle);

        public static bool CloseHandleNoThrow(HandleRef handle)
        {
            HandleCollector.Remove((IntPtr)handle, NativeMethods.CommonHandles.Kernel);

            bool result = IntCloseHandle(handle);
            int error = Marshal.GetLastWin32Error();

            if (!result)
            {
                Debug.WriteLine("CloseHandle failed.  Error = " + error);
            }

            return result;
        }

#if BASE_NATIVEMETHODS
        [DllImport(ExternDll.Gdi32, SetLastError = true, EntryPoint = "CreateCompatibleDC", CharSet = CharSet.Auto)]
        public static extern IntPtr CriticalCreateCompatibleDC(HandleRef hDC);
#endif

        [DllImport(ExternDll.Kernel32, EntryPoint = "UnmapViewOfFile", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern bool IntUnmapViewOfFile(HandleRef pvBaseAddress);

        public static bool UnmapViewOfFileNoThrow(HandleRef pvBaseAddress)
        {
            HandleCollector.Remove((IntPtr)pvBaseAddress, NativeMethods.CommonHandles.Kernel);

            bool result = IntUnmapViewOfFile(pvBaseAddress);
            int error = Marshal.GetLastWin32Error();

            if (!result)
            {
                Debug.WriteLine("UnmapViewOfFile failed.  Error = " + error);
            }

            return result;
        }


        public static bool EnableWindow(HandleRef hWnd, bool enable)
        {
            bool result = NativeMethodsSetLastError.EnableWindow(hWnd, enable);
            if (!result)
            {
                int win32Err = Marshal.GetLastWin32Error();
                if (win32Err != 0)
                {
                    throw new Win32Exception(win32Err);
                }
            }

            return result;
        }

        public static bool EnableWindowNoThrow(HandleRef hWnd, bool enable)
        {
            // This method is not throwing because the caller don't want to fail after calling this.
            // If the window was not previously disabled, the return value is zero, else it is non-zero.
            return NativeMethodsSetLastError.EnableWindow(hWnd, enable);
        }

        // GetObject stuff
        [DllImport(ExternDll.Gdi32, SetLastError = true, CharSet = CharSet.Auto)]
        public static extern int GetObject(HandleRef hObject, int nSize, [In, Out] NativeMethods.BITMAP bm);

        [DllImport(ExternDll.User32, ExactSpelling = true, CharSet = CharSet.Auto)]
        public static extern IntPtr GetFocus();

        [DllImport(ExternDll.User32, EntryPoint = "GetCursorPos", ExactSpelling = true, CharSet = CharSet.Auto, SetLastError = true)]
        private static extern bool IntGetCursorPos(ref NativeMethods.POINT pt);

        internal static bool GetCursorPos(ref NativeMethods.POINT pt)
        {
            bool returnValue = IntGetCursorPos(ref pt);
            if (!returnValue)
            {
                throw new Win32Exception();
            }
            return returnValue;
        }

        [DllImport(ExternDll.User32, EntryPoint = "GetCursorPos", ExactSpelling = true, CharSet = CharSet.Auto)]
        private static extern bool IntTryGetCursorPos(ref NativeMethods.POINT pt);

        internal static bool TryGetCursorPos(ref NativeMethods.POINT pt)
        {
            bool returnValue = IntTryGetCursorPos(ref pt);

            // Sometimes Win32 will fail this call, such as if you are
            // not running in the interactive desktop.  For example,
            // a secure screen saver may be running.
            if (!returnValue)
            {
                Debug.WriteLine("GetCursorPos failed!");

                pt.x = 0;
                pt.y = 0;
            }
            return returnValue;
        }

#if BASE_NATIVEMETHODS || CORE_NATIVEMETHODS || FRAMEWORK_NATIVEMETHODS
        [DllImport(ExternDll.User32, ExactSpelling = true, CharSet = CharSet.Auto)]
        public static extern int GetWindowThreadProcessId(HandleRef hWnd, out int lpdwProcessId);

        [DllImport(ExternDll.User32, ExactSpelling = true, CharSet = CharSet.Auto)]
        public static extern short GetKeyState(int keyCode);

        [DllImport(ExternDll.Ole32, ExactSpelling = true, CharSet = CharSet.Auto, PreserveSig = false)]
        public static extern void DoDragDrop(IComDataObject dataObject, IOleDropSource dropSource, int allowedEffects, int[] finalEffect);

        [DllImport(ExternDll.User32, ExactSpelling = true, CharSet = CharSet.Auto)]
        public static extern bool InvalidateRect(HandleRef hWnd, IntPtr rect, bool erase);
#endif


        internal static int GetWindowText(HandleRef hWnd, [Out] StringBuilder lpString, int nMaxCount)
        {
            int returnValue = NativeMethodsSetLastError.GetWindowText(hWnd, lpString, nMaxCount);
            if (returnValue == 0)
            {
                int win32Err = Marshal.GetLastWin32Error();
                if (win32Err != 0)
                {
                    throw new Win32Exception(win32Err);
                }
            }
            return returnValue;
        }

        internal static int GetWindowTextLength(HandleRef hWnd)
        {
            int returnValue = NativeMethodsSetLastError.GetWindowTextLength(hWnd);
            if (returnValue == 0)
            {
                int win32Err = Marshal.GetLastWin32Error();
                if (win32Err != 0)
                {
                    throw new Win32Exception(win32Err);
                }
            }
            return returnValue;
        }

#if BASE_NATIVEMETHODS || CORE_NATIVEMETHODS || FRAMEWORK_NATIVEMETHODS
        [DllImport(ExternDll.Imm32, CharSet = CharSet.Auto)]
        public static extern bool ImmSetConversionStatus(HandleRef hIMC, int conversion, int sentence);

        [DllImport(ExternDll.Imm32, CharSet = CharSet.Auto)]
        public static extern bool ImmGetConversionStatus(HandleRef hIMC, ref int conversion, ref int sentence);

        [DllImport(ExternDll.Imm32, CharSet = CharSet.Auto)]
        public static extern IntPtr ImmGetContext(HandleRef hWnd);

        [DllImport(ExternDll.Imm32, CharSet = CharSet.Auto)]
        public static extern bool ImmReleaseContext(HandleRef hWnd, HandleRef hIMC);

        [DllImport(ExternDll.Imm32, CharSet = CharSet.Auto)]
        public static extern IntPtr ImmAssociateContext(HandleRef hWnd, HandleRef hIMC);

        [DllImport(ExternDll.Imm32, CharSet = CharSet.Auto)]
        public static extern bool ImmSetOpenStatus(HandleRef hIMC, bool open);

        [DllImport(ExternDll.Imm32, CharSet = CharSet.Auto)]
        public static extern bool ImmGetOpenStatus(HandleRef hIMC);

        [DllImport(ExternDll.Imm32, CharSet = CharSet.Auto)]
        public static extern bool ImmNotifyIME(HandleRef hIMC, int dwAction, int dwIndex, int dwValue);

        [DllImport(ExternDll.Imm32, CharSet = CharSet.Auto)]
        public static extern int ImmGetProperty(HandleRef hkl, int flags);

        // ImmGetCompositionString for result and composition strings
        [DllImport(ExternDll.Imm32, CharSet = CharSet.Auto)]
        public static extern int ImmGetCompositionString(HandleRef hIMC, int dwIndex, char[] lpBuf, int dwBufLen);

        // ImmGetCompositionString for display attributes
        [DllImport(ExternDll.Imm32, CharSet = CharSet.Auto)]
        public static extern int ImmGetCompositionString(HandleRef hIMC, int dwIndex, byte[] lpBuf, int dwBufLen);

        // ImmGetCompositionString for clause information
        [DllImport(ExternDll.Imm32, CharSet = CharSet.Auto)]
        public static extern int ImmGetCompositionString(HandleRef hIMC, int dwIndex, int[] lpBuf, int dwBufLen);

        // ImmGetCompositionString for query information
        [DllImport(ExternDll.Imm32, CharSet = CharSet.Auto)]
        public static extern int ImmGetCompositionString(HandleRef hIMC, int dwIndex, IntPtr lpBuf, int dwBufLen);

        [DllImport(ExternDll.Imm32, CharSet = CharSet.Auto)]
        public static extern int ImmConfigureIME(HandleRef hkl, HandleRef hwnd, int dwData, IntPtr pvoid);

        [DllImport(ExternDll.Imm32, CharSet = CharSet.Auto)]
        public static extern int ImmConfigureIME(HandleRef hkl, HandleRef hwnd, int dwData, [In] ref NativeMethods.REGISTERWORD registerWord);

        [DllImport(ExternDll.Imm32, CharSet = CharSet.Auto)]
        public static extern int ImmSetCompositionWindow(HandleRef hIMC, [In, Out] ref NativeMethods.COMPOSITIONFORM compform);

        [DllImport(ExternDll.Imm32, CharSet = CharSet.Auto)]
        public static extern int ImmSetCandidateWindow(HandleRef hIMC, [In, Out] ref NativeMethods.CANDIDATEFORM candform);

        [DllImport(ExternDll.Imm32, CharSet = CharSet.Auto)]
        public static extern IntPtr ImmGetDefaultIMEWnd(HandleRef hwnd);
#endif

        internal static IntPtr SetFocus(HandleRef hWnd)
        {
            IntPtr result = IntPtr.Zero;

            if (!TrySetFocus(hWnd, ref result))
            {
                throw new Win32Exception();
            }

            return result;
        }

        internal static bool TrySetFocus(HandleRef hWnd)
        {
            IntPtr result = IntPtr.Zero;
            return TrySetFocus(hWnd, ref result);
        }

        internal static bool TrySetFocus(HandleRef hWnd, ref IntPtr result)
        {
            result = NativeMethodsSetLastError.SetFocus(hWnd);
            int errorCode = Marshal.GetLastWin32Error();

            if (result == IntPtr.Zero && errorCode != 0)
            {
                return false;
            }

            return true;
        }

        internal static IntPtr GetParent(HandleRef hWnd)
        {
            IntPtr retVal = NativeMethodsSetLastError.GetParent(hWnd);
            int errorCode = Marshal.GetLastWin32Error();

            if (retVal == IntPtr.Zero && errorCode != 0)
            {
                throw new Win32Exception(errorCode);
            }

            return retVal;
        }

        [DllImport(ExternDll.User32, ExactSpelling = true, CharSet = CharSet.Auto)]
        public static extern IntPtr GetAncestor(HandleRef hWnd, int flags);

        [DllImport(ExternDll.User32, SetLastError = true, ExactSpelling = true, CharSet = CharSet.Auto)]
        public static extern bool IsChild(HandleRef hWndParent, HandleRef hwnd);

        [DllImport(ExternDll.User32, ExactSpelling = true, CharSet = CharSet.Auto)]
        public static extern IntPtr SetParent(HandleRef hWnd, HandleRef hWndParent);

        [DllImport(ExternDll.Kernel32, EntryPoint = "GetModuleHandle", CharSet = CharSet.Auto, BestFitMapping = false, ThrowOnUnmappableChar = true, SetLastError = true)]
        private static extern IntPtr IntGetModuleHandle(string modName);
        internal static IntPtr GetModuleHandle(string modName)
        {
            IntPtr retVal = IntGetModuleHandle(modName);

            if (retVal == IntPtr.Zero)
            {
                throw new Win32Exception();
            }

            return retVal;
        }


        [DllImport(ExternDll.User32, CharSet = CharSet.Auto)]
        public static extern IntPtr CallWindowProc(IntPtr wndProc, IntPtr hWnd, int msg,
                                                IntPtr wParam, IntPtr lParam);

        [DllImport(ExternDll.Kernel32, SetLastError = true, EntryPoint = "GetProcAddress", CharSet = CharSet.Ansi, BestFitMapping = false)]
        public static extern IntPtr IntGetProcAddress(HandleRef hModule, string lpProcName);

        public static IntPtr GetProcAddress(HandleRef hModule, string lpProcName)
        {
            IntPtr result = IntGetProcAddress(hModule, lpProcName);
            if (result == IntPtr.Zero)
            {
                throw new Win32Exception();
            }

            return result;
        }

        // GetProcAddress Note : The lpProcName parameter can identify the DLL function by specifying an ordinal value associated
        // with the function in the EXPORTS statement. GetProcAddress verifies that the specified ordinal is in
        // the range 1 through the highest ordinal value exported in the .def file. The function then uses the
        // ordinal as an index to read the function's address from a function table. If the .def file does not number
        // the functions consecutively from 1 to N (where N is the number of exported functions), an error can
        // occur where GetProcAddress returns an invalid, non-NULL address, even though there is no function with the specified ordinal.

        [DllImport(ExternDll.Kernel32, EntryPoint = "GetProcAddress", CharSet = CharSet.Ansi, BestFitMapping = false)]
        public static extern IntPtr GetProcAddressNoThrow(HandleRef hModule, string lpProcName);

        [DllImport(ExternDll.Kernel32, CharSet = CharSet.Unicode)]
        public static extern IntPtr LoadLibrary(string lpFileName);

        [Flags]
        internal enum LoadLibraryFlags : uint
        {
            None = 0x00000000,
            /// <summary>
            /// If this value is used, and the executable module is a DLL, the system does 
            /// not call DllMain for process and thread initialization and termination. 
            /// Also, the system does not load additional executable modules that are 
            /// referenced by the specified module.
            /// </summary>
            /// <remarks>
            /// Do not use this value; it is provided only for backward compatibility. 
            /// If you are planning to access only data or resources in the DLL, use 
            /// <see cref="LOAD_LIBRARY_AS_DATAFILE_EXCLUSIVE"/> or 
            /// <see cref="LOAD_LIBRARY_AS_DATAFILE_EXCLUSIVE"/> or <see cref="LOAD_LIBRARY_AS_IMAGE_RESOURCE"/>
            /// or both. Otherwise, load the library as a DLL or executable module 
            /// using the <see cref="LoadLibrary(string)"/>function.
            /// </remarks>
            DONT_RESOLVE_DLL_REFERENCES = 0x00000001,
            /// <summary>
            /// If this value is used, the system does not check AppLocker rules or apply 
            /// Software Restriction Policies for the DLL. This action applies only to the 
            /// DLL being loaded and not to its dependencies. This value is recommended 
            /// for use in setup programs that must run extracted DLLs during installation.
            /// </summary>
            /// <remarks>
            /// Windows Server 2008 R2 and Windows 7:  
            ///     On systems with KB2532445 installed, the 
            ///     caller must be running as "LocalSystem" or "TrustedInstaller"; otherwise the 
            ///     system ignores this flag. For more information, see "You can circumvent AppLocker 
            ///     rules by using an Office macro on a computer that is running Windows 7 or 
            ///     Windows Server 2008 R2" in the Help and Support Knowledge Base 
            ///     at <see cref="http://support.microsoft.com/kb/2532445."/>
            /// 
            /// Windows Server 2008, Windows Vista, Windows Server 2003 and Windows XP:  
            ///     AppLocker was introduced in Windows 7 and Windows Server 2008 R2.
            /// </remarks>
            LOAD_IGNORE_CODE_AUTHZ_LEVEL = 0x00000010,
            /// <summary>
            /// If this value is used, the system maps the file into the calling process's 
            /// virtual address space as if it were a data file. Nothing is done to execute 
            /// or prepare to execute the mapped file. Therefore, you cannot call functions 
            /// like GetModuleFileName, GetModuleHandle or GetProcAddress with this DLL. 
            /// Using this value causes writes to read-only memory to raise an access violation. 
            /// Use this flag when you want to load a DLL only to extract messages or resources 
            /// from it.This value can be used with <see cref="LOAD_LIBRARY_AS_IMAGE_RESOURCE"/>.
            /// </summary>
            LOAD_LIBRARY_AS_DATAFILE = 0x00000002,
            /// <summary>
            /// Similar to LOAD_LIBRARY_AS_DATAFILE, except that the DLL file is opened with 
            /// exclusive write access for the calling process. Other processes cannot open 
            /// the DLL file for write access while it is in use. However, the DLL can 
            /// still be opened by other processes. This value can be used with 
            /// <see cref="LOAD_LIBRARY_AS_IMAGE_RESOURCE"/>. 
            /// </summary>
            /// <remarks>
            /// Windows Server 2003 and Windows XP:  This value is not supported until 
            /// Windows Vista.
            /// </remarks>
            LOAD_LIBRARY_AS_DATAFILE_EXCLUSIVE = 0x00000040,
            /// <summary>
            /// If this value is used, the system maps the file into the process's virtual 
            /// address space as an image file. However, the loader does not load the static 
            /// imports or perform the other usual initialization steps. Use this flag when 
            /// you want to load a DLL only to extract messages or resources from it. Unless 
            /// the application depends on the file having the in-memory layout of an image, 
            /// this value should be used with either <see cref="LOAD_LIBRARY_AS_DATAFILE_EXCLUSIVE "/> or 
            /// <see cref="LOAD_LIBRARY_AS_DATAFILE"/>.
            /// </summary>
            /// <remarks>
            /// Windows Server 2003 and Windows XP:  This value is not supported until Windows Vista.
            /// </remarks>
            LOAD_LIBRARY_AS_IMAGE_RESOURCE = 0x00000020,
            /// <summary>
            /// If this value is used, the application's installation directory is searched for the 
            /// DLL and its dependencies. Directories in the standard search path are not searched. 
            /// This value cannot be combined with <see cref="LOAD_WITH_ALTERED_SEARCH_PATH"/>.
            /// </summary>
            /// <remarks>
            /// Windows 7, Windows Server 2008 R2, Windows Vista and Windows Server 2008:  
            ///     This value requires KB2533623 to be installed.
            /// Windows Server 2003 and Windows XP:  
            ///     This value is not supported.
            /// </remarks>
            LOAD_LIBRARY_SEARCH_APPLICATION_DIR = 0x00000200,
            /// <summary>
            /// This value is a combination of <see cref="LOAD_LIBRARY_SEARCH_APPLICATION_DIR"/>, 
            /// <see cref="LOAD_LIBRARY_SEARCH_SYSTEM32"/>, and <see cref="LOAD_LIBRARY_SEARCH_USER_DIRS"/>. 
            /// Directories in the standard search path are not searched. This value cannot be combined with 
            /// <see cref="LOAD_WITH_ALTERED_SEARCH_PATH"/>. This value represents the recommended maximum number 
            /// of directories an application should include in its DLL search path.
            /// </summary>
            /// <remarks>
            /// Windows 7, Windows Server 2008 R2, Windows Vista and Windows Server 2008:  
            ///     This value requires KB2533623 to be installed. 
            /// 
            /// Windows Server 2003 and Windows XP:  
            ///     This value is not supported.
            /// </remarks>
            LOAD_LIBRARY_SEARCH_DEFAULT_DIRS = 0x00001000,
            /// <summary>
            /// If this value is used, the directory that contains the DLL is temporarily added to 
            /// the beginning of the list of directories that are searched for the DLL's dependencies. 
            /// Directories in the standard search path are not searched.
            /// 
            /// The lpFileName parameter must specify a fully qualified path. This value cannot be 
            /// combined with <see cref="LOAD_WITH_ALTERED_SEARCH_PATH"/>. 
            /// 
            /// For example, if Lib2.dll is a dependency of C:\Dir1\Lib1.dll, loading Lib1.dll with 
            /// this value causes the system to search for Lib2.dll only in C:\Dir1. To search for 
            /// Lib2.dll in C:\Dir1 and all of the directories in the DLL search path, combine this 
            /// value with <see cref="LOAD_LIBRARY_DEFAULT_DIRS"/>.
            /// </summary>
            /// <remarks>
            /// Windows 7, Windows Server 2008 R2, Windows Vista and Windows Server 2008:  
            ///     This value requires KB2533623 to be installed.
            /// 
            /// Windows Server 2003 and Windows XP:  
            ///     This value is not supported.
            /// </remarks>
            LOAD_LIBRARY_SEARCH_DLL_LOAD_DIR = 0x00000100,
            /// <summary>
            /// If this value is used, %windows%\system32 is searched for the DLL and its dependencies. 
            /// Directories in the standard search path are not searched. This value cannot be 
            /// combined with <see cref="LOAD_WITH_ALTERED_SEARCH_PATH"/>
            /// </summary>
            /// <remarks>
            /// Windows 7, Windows Server 2008 R2, Windows Vista and Windows Server 2008:  
            ///     This value requires KB2533623 to be installed.
            /// Windows Server 2003 and Windows XP:  
            ///     This value is not supported.
            /// </remarks>
            LOAD_LIBRARY_SEARCH_SYSTEM32 = 0x00000800,
            /// <summary>
            /// If this value is used, directories added using the AddDllDirectory or the SetDllDirectory 
            /// function are searched for the DLL and its dependencies. If more than one directory has been added, 
            /// the order in which the directories are searched is unspecified. Directories in the 
            /// standard search path are not searched. This value cannot be combined with 
            /// <see cref="LOAD_WITH_ALTERED_SEARCH_PATH"/>
            /// </summary>
            /// <remarks>
            /// Windows 7, Windows Server 2008 R2, Windows Vista and Windows Server 2008:  
            ///     This value requires KB2533623 to be installed.
            /// Windows Server 2003 and Windows XP:  
            ///     This value is not supported.
            /// </remarks>
            LOAD_LIBRARY_SEARCH_USER_DIRS = 0x00000400,
            /// <summary>
            /// If this value is used and lpFileName specifies an absolute path, the system uses the alternate 
            /// file search strategy discussed in the Remarks section to find associated executable modules that 
            /// the specified module causes to be loaded. If this value is used and lpFileName specifies a 
            /// relative path, the behavior is undefined. If this value is not used, or if lpFileName does not specify a path, 
            /// the system uses the standard search strategy discussed in the Remarks section to find associated 
            /// executable modules that the specified module causes to be loaded.This value cannot be combined with 
            /// any LOAD_LIBRARY_SEARCH flag.
            /// </summary>
            LOAD_WITH_ALTERED_SEARCH_PATH = 0x00000008
        }

        /// <summary>
        /// Do not use this - instead use <see cref="LoadLibraryHelper.SecureLoadLibrary"/>
        /// </summary>
        [Obsolete("Use LoadLibraryHelper.SafeLoadLibraryEx instead")]
        [DllImport(ExternDll.Kernel32, CallingConvention = CallingConvention.Winapi, CharSet = CharSet.Unicode, SetLastError = true)]
        internal static extern IntPtr LoadLibraryEx([In][MarshalAs(UnmanagedType.LPTStr)] string lpFileName, IntPtr hFile, [In] LoadLibraryFlags dwFlags);

        [Flags]
        internal enum GetModuleHandleFlags : uint
        {
            None = 0x00000000,
            /// <summary>
            /// The lpModuleName parameter in <see cref="GetModuleHandleEx"/> is an address 
            /// in the module.
            /// </summary>
            GET_MODULE_HANDLE_EX_FLAG_FROM_ADDRESS = 0x00000004,
            /// <summary>
            /// The module stays loaded until the process is terminated, no matter how many times 
            /// FreeLibrary is called.
            /// This option cannot be used with <see cref="GET_MODULE_HANDLE_EX_FLAG_UNCHANGED_REFCOUNT"/>.
            /// </summary>
            GET_MODULE_HANDLE_EX_FLAG_PIN = 0x00000001,
            /// <summary>
            /// The reference count for the module is not incremented. This option is equivalent to the 
            /// behavior of GetModuleHandle. Do not pass the retrieved module handle to the FreeLibrary 
            /// function; doing so can cause the DLL to be unmapped prematurely.
            /// This option cannot be used with <see cref="GET_MODULE_HANDLE_EX_FLAG_PIN"/>.
            /// </summary>
            GET_MODULE_HANDLE_EX_FLAG_UNCHANGED_REFCOUNT = 0x00000002
        }

        [DllImport(ExternDll.Kernel32, CallingConvention = CallingConvention.Winapi, CharSet = CharSet.Unicode, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool GetModuleHandleEx(
            [In] GetModuleHandleFlags dwFlags,
            [In][Optional][MarshalAs(UnmanagedType.LPTStr)] string lpModuleName,
            [Out] out IntPtr hModule);

        [DllImport(ExternDll.Kernel32, CallingConvention = CallingConvention.Winapi, CharSet = CharSet.Unicode, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool FreeLibrary([In] IntPtr hModule);

#if !DRT && !UIAUTOMATIONTYPES
        [DllImport(ExternDll.User32)]
        public static extern int GetSystemMetrics(SM nIndex);
#endif

        [DllImport(ExternDll.User32, SetLastError = true, CharSet = CharSet.Auto, BestFitMapping = false)]
        public static extern bool SystemParametersInfo(int nAction, int nParam, ref NativeMethods.RECT rc, int nUpdate);

        [DllImport(ExternDll.User32, SetLastError = true, CharSet = CharSet.Auto, BestFitMapping = false)]
        public static extern bool SystemParametersInfo(int nAction, int nParam, ref int value, int ignore);

        [DllImport(ExternDll.User32, SetLastError = true, CharSet = CharSet.Auto, BestFitMapping = false)]
        public static extern bool SystemParametersInfo(int nAction, int nParam, ref bool value, int ignore);

        [DllImport(ExternDll.User32, SetLastError = true, CharSet = CharSet.Auto, BestFitMapping = false)]
        public static extern bool SystemParametersInfo(int nAction, int nParam, ref NativeMethods.HIGHCONTRAST_I rc, int nUpdate);

        [DllImport(ExternDll.User32, SetLastError = true, CharSet = CharSet.Auto, BestFitMapping = false)]
        public static extern bool SystemParametersInfo(int nAction, int nParam, [In, Out] NativeMethods.NONCLIENTMETRICS metrics, int nUpdate);

        [DllImport(ExternDll.Kernel32, CharSet = CharSet.Auto, ExactSpelling = true)]
        public static extern bool GetSystemPowerStatus(ref NativeMethods.SYSTEM_POWER_STATUS systemPowerStatus);

        [DllImport(ExternDll.User32, EntryPoint = "ClientToScreen", SetLastError = true, ExactSpelling = true, CharSet = CharSet.Auto)]
        private static extern int IntClientToScreen(HandleRef hWnd, ref NativeMethods.POINT pt);

        public static void ClientToScreen(HandleRef hWnd, ref NativeMethods.POINT pt)
        {
            if (IntClientToScreen(hWnd, ref pt) == 0)
            {
                throw new Win32Exception();
            }
        }

        [DllImport(ExternDll.User32, ExactSpelling = true, CharSet = CharSet.Auto)]
        public static extern IntPtr GetDesktopWindow();

        [DllImport(ExternDll.User32, ExactSpelling = true, CharSet = CharSet.Auto)]
        public static extern IntPtr GetForegroundWindow();

        [DllImport(ExternDll.Ole32, ExactSpelling = true, CharSet = CharSet.Auto)]
        public static extern int RegisterDragDrop(HandleRef hwnd, IOleDropTarget target);

        [DllImport(ExternDll.Ole32, ExactSpelling = true, CharSet = CharSet.Auto)]
        public static extern int RevokeDragDrop(HandleRef hwnd);

#if !DRT && !UIAUTOMATIONTYPES
        [DllImport(ExternDll.User32, CharSet = CharSet.Auto)]
        public static extern bool PeekMessage([In, Out] ref System.Windows.Interop.MSG msg, HandleRef hwnd, WindowMessage msgMin, WindowMessage msgMax, int remove);

#if BASE_NATIVEMETHODS
        [DllImport(ExternDll.User32, BestFitMapping = false, CharSet = CharSet.Auto)]
        public static extern bool SetProp(HandleRef hWnd, string propName, HandleRef data);

#endif

        [DllImport(ExternDll.User32, EntryPoint = "PostMessage", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern bool IntPostMessage(HandleRef hwnd, WindowMessage msg, IntPtr wparam, IntPtr lparam);

        internal static void PostMessage(HandleRef hwnd, WindowMessage msg, IntPtr wparam, IntPtr lparam)
        {
            if (!IntPostMessage(hwnd, msg, wparam, lparam))
            {
                throw new Win32Exception();
            }
        }

        [DllImport(ExternDll.User32, EntryPoint = "PostMessage", CharSet = CharSet.Auto)]
        internal static extern bool TryPostMessage(HandleRef hwnd, WindowMessage msg, IntPtr wparam, IntPtr lparam);
#endif
#if BASE_NATIVEMETHODS || CORE_NATIVEMETHODS
        [DllImport(ExternDll.User32, ExactSpelling = true, CharSet = CharSet.Auto)]
        public static extern void NotifyWinEvent(int winEvent, HandleRef hwnd, int objType, int objID);
#endif
        [DllImport(ExternDll.User32, ExactSpelling = true, EntryPoint = "BeginPaint", CharSet = CharSet.Auto)]
        private static extern IntPtr IntBeginPaint(HandleRef hWnd, [In, Out] ref NativeMethods.PAINTSTRUCT lpPaint);

        public static IntPtr BeginPaint(HandleRef hWnd, [In, Out, MarshalAs(UnmanagedType.LPStruct)] ref NativeMethods.PAINTSTRUCT lpPaint)
        {
            return HandleCollector.Add(IntBeginPaint(hWnd, ref lpPaint), NativeMethods.CommonHandles.HDC);
        }

        [DllImport(ExternDll.User32, ExactSpelling = true, EntryPoint = "EndPaint", CharSet = CharSet.Auto)]
        private static extern bool IntEndPaint(HandleRef hWnd, ref NativeMethods.PAINTSTRUCT lpPaint);
        public static bool EndPaint(HandleRef hWnd, [In, MarshalAs(UnmanagedType.LPStruct)] ref NativeMethods.PAINTSTRUCT lpPaint)
        {
            HandleCollector.Remove(lpPaint.hdc, NativeMethods.CommonHandles.HDC);
            return IntEndPaint(hWnd, ref lpPaint);
        }

        [DllImport(ExternDll.User32, SetLastError = true, ExactSpelling = true, EntryPoint = "GetDC", CharSet = CharSet.Auto)]
        private static extern IntPtr IntGetDC(HandleRef hWnd);
        public static IntPtr GetDC(HandleRef hWnd)
        {
            IntPtr hDc = IntGetDC(hWnd);
            if (hDc == IntPtr.Zero)
            {
                throw new Win32Exception();
            }

            return HandleCollector.Add(hDc, NativeMethods.CommonHandles.HDC);
        }

        [DllImport(ExternDll.User32, ExactSpelling = true, EntryPoint = "ReleaseDC", CharSet = CharSet.Auto)]
        private static extern int IntReleaseDC(HandleRef hWnd, HandleRef hDC);
        public static int ReleaseDC(HandleRef hWnd, HandleRef hDC)
        {
            HandleCollector.Remove((IntPtr)hDC, NativeMethods.CommonHandles.HDC);
            return IntReleaseDC(hWnd, hDC);
        }


        [DllImport(ExternDll.Gdi32, SetLastError = true, ExactSpelling = true, CharSet = CharSet.Auto)]
        public static extern int GetDeviceCaps(HandleRef hDC, int nIndex);

        [DllImport(ExternDll.User32, ExactSpelling = true, CharSet = CharSet.Auto)]
        public static extern IntPtr GetActiveWindow();

        [DllImport(ExternDll.User32, ExactSpelling = true, CharSet = CharSet.Auto)]
        public static extern bool SetForegroundWindow(HandleRef hWnd);

        [return: MarshalAs(UnmanagedType.Bool)]
        [DllImport(ExternDll.User32, ExactSpelling = true, CharSet = CharSet.Auto, SetLastError = true)]
        public static extern unsafe bool UpdateLayeredWindow(IntPtr hwnd, IntPtr hdcDst, NativeMethods.POINT* pptDst, NativeMethods.POINT* pSizeDst, IntPtr hdcSrc, NativeMethods.POINT* pptSrc, int crKey, ref NativeMethods.BLENDFUNCTION pBlend, int dwFlags);

        [DllImport(ExternDll.User32, SetLastError = true)]
        public static extern IntPtr SetActiveWindow(HandleRef hWnd);

        //Refactor shared native methods so that parser dependency
        // is in separate file. 
#if PBTCOMPILER
        [DllImport(ExternDll.User32, ExactSpelling=true, CharSet=CharSet.Auto)]
        public static extern IntPtr SetCursor(HandleRef hcursor);
#endif


        [DllImport(ExternDll.User32, ExactSpelling = true, EntryPoint = "DestroyCursor", CharSet = CharSet.Auto)]
        private static extern bool IntDestroyCursor(IntPtr hCurs);

        public static bool DestroyCursor(IntPtr hCurs)
        {
            return IntDestroyCursor(hCurs);
        }

        [DllImport(ExternDll.User32, EntryPoint = "DestroyIcon", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern bool IntDestroyIcon(IntPtr hIcon);

        public static bool DestroyIcon(IntPtr hIcon)
        {
            bool result = IntDestroyIcon(hIcon);
            int error = Marshal.GetLastWin32Error();

            if (!result)
            {
                // To be consistent with out other PInvoke wrappers
                // we should "throw" here.  But we don't want to
                // introduce new "throws" w/o time to follow up on any
                // new problems that causes.
                Debug.WriteLine("DestroyIcon failed.  Error = " + error);
                //throw new Win32Exception();
            }

            return result;
        }

        [DllImport(ExternDll.Gdi32, EntryPoint = "DeleteObject", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern bool IntDeleteObject(IntPtr hObject);

        public static bool DeleteObject(IntPtr hObject)
        {
            bool result = IntDeleteObject(hObject);
            int error = Marshal.GetLastWin32Error();

            if (!result)
            {
                // To be consistent with out other PInvoke wrappers
                // we should "throw" here.  But we don't want to
                // introduce new "throws" w/o time to follow up on any
                // new problems that causes.
                Debug.WriteLine("DeleteObject failed.  Error = " + error);
                //throw new Win32Exception();
            }

            return result;
        }

#if BASE_NATIVEMETHODS || CORE_NATIVEMETHODS || FRAMEWORK_NATIVEMETHODS
        [DllImport(ExternDll.Gdi32, SetLastError = true, ExactSpelling = true, CharSet = CharSet.Auto, EntryPoint = "CreateDIBSection")]
        private static extern NativeMethods.BitmapHandle PrivateCreateDIBSection(HandleRef hdc, ref NativeMethods.BITMAPINFO bitmapInfo, int iUsage, ref IntPtr ppvBits, SafeFileMappingHandle hSection, int dwOffset);
        internal static NativeMethods.BitmapHandle CreateDIBSection(HandleRef hdc, ref NativeMethods.BITMAPINFO bitmapInfo, int iUsage, ref IntPtr ppvBits, SafeFileMappingHandle hSection, int dwOffset)
        {
            if (hSection == null)
            {
                // PInvoke marshalling does not handle null SafeHandle, we must pass an IntPtr.Zero backed SafeHandle
                hSection = new SafeFileMappingHandle(IntPtr.Zero);
            }

            NativeMethods.BitmapHandle hBitmap = PrivateCreateDIBSection(hdc, ref bitmapInfo, iUsage, ref ppvBits, hSection, dwOffset);
            int error = Marshal.GetLastWin32Error();

            if (hBitmap.IsInvalid)
            {
                Debug.WriteLine("CreateDIBSection failed. Error = " + error);
            }

            return hBitmap;
        }
#endif

        [DllImport(ExternDll.Gdi32, SetLastError = true, ExactSpelling = true, CharSet = CharSet.Auto, EntryPoint = "CreateBitmap")]
        private static extern NativeMethods.BitmapHandle PrivateCreateBitmap(int width, int height, int planes, int bitsPerPixel, byte[] lpvBits);
        internal static NativeMethods.BitmapHandle CreateBitmap(int width, int height, int planes, int bitsPerPixel, byte[] lpvBits)
        {
            NativeMethods.BitmapHandle hBitmap = PrivateCreateBitmap(width, height, planes, bitsPerPixel, lpvBits);
            int error = Marshal.GetLastWin32Error();

            if (hBitmap.IsInvalid)
            {
                Debug.WriteLine("CreateBitmap failed. Error = " + error);
            }

            return hBitmap;
        }

        [DllImport(ExternDll.User32, SetLastError = true, ExactSpelling = true, CharSet = CharSet.Auto, EntryPoint = "CreateIconIndirect")]
        private static extern NativeMethods.IconHandle PrivateCreateIconIndirect([In, MarshalAs(UnmanagedType.LPStruct)] NativeMethods.ICONINFO iconInfo);
        internal static NativeMethods.IconHandle CreateIconIndirect([In, MarshalAs(UnmanagedType.LPStruct)] NativeMethods.ICONINFO iconInfo)
        {
            NativeMethods.IconHandle hIcon = PrivateCreateIconIndirect(iconInfo);
            int error = Marshal.GetLastWin32Error();

            if (hIcon.IsInvalid)
            {
                Debug.WriteLine("CreateIconIndirect failed. Error = " + error);
            }

            return hIcon;
        }

        [DllImport(ExternDll.User32, ExactSpelling = true, CharSet = CharSet.Auto)]
        public static extern bool IsWindow(HandleRef hWnd);

#if BASE_NATIVEMETHODS
        [DllImport(ExternDll.Gdi32, SetLastError = true, ExactSpelling = true, EntryPoint = "DeleteDC", CharSet = CharSet.Auto)]
        private static extern bool IntCriticalDeleteDC(HandleRef hDC);

        public static void CriticalDeleteDC(HandleRef hDC)
        {
            HandleCollector.Remove((IntPtr)hDC, NativeMethods.CommonHandles.HDC);
            if (!IntCriticalDeleteDC(hDC))
            {
                throw new Win32Exception();
            }
        }
#endif


#if BASE_NATIVEMETHODS

        [DllImport(ExternDll.User32, SetLastError = true, EntryPoint = "GetMessageW", ExactSpelling = true, CharSet = CharSet.Unicode)]
        private static extern int IntGetMessageW([In, Out] ref System.Windows.Interop.MSG msg, HandleRef hWnd, int uMsgFilterMin, int uMsgFilterMax);
        public static bool GetMessageW([In, Out] ref System.Windows.Interop.MSG msg, HandleRef hWnd, int uMsgFilterMin, int uMsgFilterMax)
        {
            bool boolResult = false;

            int result = IntGetMessageW(ref msg, hWnd, uMsgFilterMin, uMsgFilterMax);
            if (result == -1)
            {
                throw new Win32Exception();
            }
            else if (result == 0)
            {
                boolResult = false;
            }
            else
            {
                boolResult = true;
            }

            return boolResult;
        }

#endif

#if BASE_NATIVEMETHODS

        [DllImport(ExternDll.User32, EntryPoint = "WindowFromPoint", ExactSpelling = true, CharSet = CharSet.Auto)]
        private static extern IntPtr IntWindowFromPoint(POINT pt);

        public static IntPtr WindowFromPoint(int x, int y)
        {
            POINT ps = new POINT(x, y);
            return IntWindowFromPoint(ps);
        }
#endif

        [DllImport(ExternDll.User32, EntryPoint = "CreateWindowEx", CharSet = CharSet.Auto, BestFitMapping = false, SetLastError = true)]
        public static extern IntPtr IntCreateWindowEx(int dwExStyle, string lpszClassName,
                                                   string lpszWindowName, int style, int x, int y, int width, int height,
                                                   HandleRef hWndParent, HandleRef hMenu, HandleRef hInst, [MarshalAs(UnmanagedType.AsAny)] object pvParam);

        public static IntPtr CreateWindowEx(int dwExStyle, string lpszClassName,
                                         string lpszWindowName, int style, int x, int y, int width, int height,
                                         HandleRef hWndParent, HandleRef hMenu, HandleRef hInst, [MarshalAs(UnmanagedType.AsAny)] object pvParam)
        {
            IntPtr retVal = IntCreateWindowEx(dwExStyle, lpszClassName,
                                         lpszWindowName, style, x, y, width, height, hWndParent, hMenu,
                                         hInst, pvParam);
            if (retVal == IntPtr.Zero)
            {
                throw new Win32Exception();
            }
            return retVal;
        }

        [DllImport(ExternDll.User32, SetLastError = true, EntryPoint = "DestroyWindow", CharSet = CharSet.Auto)]
        public static extern bool IntDestroyWindow(HandleRef hWnd);

        public static void DestroyWindow(HandleRef hWnd)
        {
            if (!IntDestroyWindow(hWnd))
            {
                throw new Win32Exception();
            }
        }
        [DllImport(ExternDll.User32)]
        internal static extern IntPtr SetWinEventHook(int eventMin, int eventMax, IntPtr hmodWinEventProc, NativeMethods.WinEventProcDef WinEventReentrancyFilter, uint idProcess, uint idThread, int dwFlags);

        [DllImport(ExternDll.User32)]
        internal static extern bool UnhookWinEvent(IntPtr winEventHook);

        [ComImport(), Guid("00000122-0000-0000-C000-000000000046"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        public interface IOleDropTarget
        {
            [PreserveSig]
            int OleDragEnter(
                [In, MarshalAs(UnmanagedType.Interface)]
                object pDataObj,
                [In, MarshalAs(UnmanagedType.U4)]
                int grfKeyState,
                [In, MarshalAs(UnmanagedType.U8)]
                long pt,
                [In, Out]
                ref int pdwEffect);

            [PreserveSig]
            int OleDragOver(
                [In, MarshalAs(UnmanagedType.U4)]
                int grfKeyState,
                [In, MarshalAs(UnmanagedType.U8)]
                long pt,
                [In, Out]
                ref int pdwEffect);

            [PreserveSig]
            int OleDragLeave();

            [PreserveSig]
            int OleDrop(
                [In, MarshalAs(UnmanagedType.Interface)]
                object pDataObj,
                [In, MarshalAs(UnmanagedType.U4)]
                int grfKeyState,
                [In, MarshalAs(UnmanagedType.U8)]
                long pt,
                [In, Out]
                ref int pdwEffect);
        }

        [ComImport(), Guid("00000121-0000-0000-C000-000000000046"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        public interface IOleDropSource
        {
            [PreserveSig]
            int OleQueryContinueDrag(
                int fEscapePressed,
                [In, MarshalAs(UnmanagedType.U4)]
                int grfKeyState);

            [PreserveSig]
            int OleGiveFeedback(
                [In, MarshalAs(UnmanagedType.U4)]
                int dwEffect);
        }

        [
        ComImport(),
        Guid("B196B289-BAB4-101A-B69C-00AA00341D07"),
        InterfaceType(ComInterfaceType.InterfaceIsIUnknown)
        ]
        public interface IOleControlSite
        {
            [PreserveSig]
            int OnControlInfoChanged();

            [PreserveSig]
            int LockInPlaceActive(int fLock);

            [PreserveSig]
            int GetExtendedControl(
                [Out, MarshalAs(UnmanagedType.IDispatch)]
                out object ppDisp);

            [PreserveSig]
            int TransformCoords(
                ref NativeMethods.POINT pPtlHimetric,
                ref NativeMethods.POINTF pPtfContainer,
                [In, MarshalAs(UnmanagedType.U4)]
                int dwFlags);

            [PreserveSig]
            int TranslateAccelerator(
                [In]
                ref System.Windows.Interop.MSG pMsg,
                [In, MarshalAs(UnmanagedType.U4)]
                int grfModifiers);

            [PreserveSig]
            int OnFocus(int fGotFocus);

            [PreserveSig]
            int ShowPropertyFrame();
        }

        [ComImport(), Guid("00000118-0000-0000-C000-000000000046"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        public interface IOleClientSite
        {
            [PreserveSig]
            int SaveObject();

            [PreserveSig]
            int GetMoniker(
                [In, MarshalAs(UnmanagedType.U4)]
                int dwAssign,
                [In, MarshalAs(UnmanagedType.U4)]
                int dwWhichMoniker,
                [Out, MarshalAs(UnmanagedType.Interface)]
                out object moniker);

            [PreserveSig]
            int GetContainer(out IOleContainer container);

            [PreserveSig]
            int ShowObject();

            [PreserveSig]
            int OnShowWindow(int fShow);

            [PreserveSig]
            int RequestNewObjectLayout();
        }

        [ComImport(), Guid("00000119-0000-0000-C000-000000000046"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        public interface IOleInPlaceSite
        {
            IntPtr GetWindow();

            [PreserveSig]
            int ContextSensitiveHelp(int fEnterMode);

            [PreserveSig]
            int CanInPlaceActivate();

            [PreserveSig]
            int OnInPlaceActivate();

            [PreserveSig]
            int OnUIActivate();

            [PreserveSig]
            int GetWindowContext(
                [Out, MarshalAs(UnmanagedType.Interface)]
                out IOleInPlaceFrame ppFrame,
                [Out, MarshalAs(UnmanagedType.Interface)]
                out IOleInPlaceUIWindow ppDoc,
                [Out]
                NativeMethods.COMRECT lprcPosRect,
                [Out]
                NativeMethods.COMRECT lprcClipRect,
                [In, Out]
                NativeMethods.OLEINPLACEFRAMEINFO lpFrameInfo);

            [PreserveSig]
            int Scroll(
                NativeMethods.SIZE scrollExtant);

            [PreserveSig]
            int OnUIDeactivate(
                int fUndoable);

            [PreserveSig]
            int OnInPlaceDeactivate();

            [PreserveSig]
            int DiscardUndoState();

            [PreserveSig]
            int DeactivateAndUndo();

            [PreserveSig]
            int OnPosRectChange(
                [In]
                NativeMethods.COMRECT lprcPosRect);
        }

        [ComImport(), Guid("9BFBBC02-EFF1-101A-84ED-00AA00341D07"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        public interface IPropertyNotifySink
        {
            void OnChanged(int dispID);

            [PreserveSig]
            int OnRequestEdit(int dispID);
        }

        [ComImport(), Guid("00000100-0000-0000-C000-000000000046"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        public interface IEnumUnknown
        {
            [PreserveSig]
            int Next(
                [In, MarshalAs(UnmanagedType.U4)]
                int celt,
                [Out]
                IntPtr rgelt,
                IntPtr pceltFetched);

            [PreserveSig]
            int Skip(
                [In, MarshalAs(UnmanagedType.U4)]
                int celt);

            void Reset();

            void Clone(
                [Out]
                out IEnumUnknown ppenum);
        }

        [ComImport(), Guid("0000011B-0000-0000-C000-000000000046"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        public interface IOleContainer
        {
            [PreserveSig]
            int ParseDisplayName(
                [In, MarshalAs(UnmanagedType.Interface)]
                object pbc,
                [In, MarshalAs(UnmanagedType.BStr)]
                string pszDisplayName,
                [Out, MarshalAs(UnmanagedType.LPArray)]
                int[] pchEaten,
                [Out, MarshalAs(UnmanagedType.LPArray)]
                object[] ppmkOut);

            [PreserveSig]
            int EnumObjects(
                [In, MarshalAs(UnmanagedType.U4)]
                int grfFlags,
                [Out]
                out IEnumUnknown ppenum);

            [PreserveSig]
            int LockContainer(
                bool fLock);
        }

        [ComImport(), Guid("00000116-0000-0000-C000-000000000046"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        public interface IOleInPlaceFrame
        {
            IntPtr GetWindow();

            [PreserveSig]
            int ContextSensitiveHelp(int fEnterMode);

            [PreserveSig]
            int GetBorder(
                [Out]
                NativeMethods.COMRECT lprectBorder);

            [PreserveSig]
            int RequestBorderSpace(
                [In]
                NativeMethods.COMRECT pborderwidths);

            [PreserveSig]
            int SetBorderSpace(
                [In]
                NativeMethods.COMRECT pborderwidths);

            [PreserveSig]
            int SetActiveObject(
                [In, MarshalAs(UnmanagedType.Interface)]
                IOleInPlaceActiveObject pActiveObject,
                [In, MarshalAs(UnmanagedType.LPWStr)]
                string pszObjName);

            [PreserveSig]
            int InsertMenus(
                [In]
                IntPtr hmenuShared,
                [In, Out]
                NativeMethods.tagOleMenuGroupWidths lpMenuWidths);

            [PreserveSig]
            int SetMenu(
                [In]
                IntPtr hmenuShared,
                [In]
                IntPtr holemenu,
                [In]
                IntPtr hwndActiveObject);

            [PreserveSig]
            int RemoveMenus(
                [In]
                IntPtr hmenuShared);

            [PreserveSig]
            int SetStatusText(
                [In, MarshalAs(UnmanagedType.LPWStr)]
                string pszStatusText);

            [PreserveSig]
            int EnableModeless(
                bool fEnable);

            [PreserveSig]
            int TranslateAccelerator(
                [In]
                ref System.Windows.Interop.MSG lpmsg,
                [In, MarshalAs(UnmanagedType.U2)]
                short wID);
        }

        //IMPORTANT: Do not try to optimize perf here by changing the enum size to byte
        //instead of int since this is used in COM Interop for browser hosting scenarios
        // Enum for OLECMDIDs used by IOleCommandTarget in browser hosted scenarios
        // Imported from the published header - docobj.h, If you need to support more
        // than these OLECMDS, add it from that header file
        public enum OLECMDID
        {
            OLECMDID_SAVE = 3,
            OLECMDID_SAVEAS = 4,
            OLECMDID_PRINT = 6,
            OLECMDID_PRINTPREVIEW = 7,
            OLECMDID_PAGESETUP = 8,
            OLECMDID_PROPERTIES = 10,
            OLECMDID_CUT = 11,
            OLECMDID_COPY = 12,
            OLECMDID_PASTE = 13,
            OLECMDID_SELECTALL = 17,
            OLECMDID_REFRESH = 22,
            OLECMDID_STOP = 23,
        }

        public enum OLECMDEXECOPT
        {
            OLECMDEXECOPT_DODEFAULT = 0,
            OLECMDEXECOPT_PROMPTUSER = 1,
            OLECMDEXECOPT_DONTPROMPTUSER = 2,
            OLECMDEXECOPT_SHOWHELP = 3
        }

        // OLECMDID Flags used by IOleCommandTarget to specify status of commands in browser hosted scenarios
        // Imported from the published header - docobj.h
        public enum OLECMDF
        {
            /// <summary>
            /// The command is supported by this object
            /// </summary>
            OLECMDF_SUPPORTED = 0x1,
            /// <summary>
            /// The command is available and enabled
            /// </summary>
            OLECMDF_ENABLED = 0x2,
            /// <summary>
            /// The command is an on-off toggle and is currently on
            /// </summary>
            OLECMDF_LATCHED = 0x4,
            /// <summary>
            /// Reserved for future use
            /// </summary>
            OLECMDF_NINCHED = 0x8,
            /// <summary>
            /// Command is invisible
            /// </summary>
            OLECMDF_INVISIBLE = 0x10,
            /// <summary>
            /// Command should not be displayed in the context menu
            /// </summary>
            OLECMDF_DEFHIDEONCTXTMENU = 0x20
        }

        [ComImport(), Guid("00000115-0000-0000-C000-000000000046"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        public interface IOleInPlaceUIWindow
        {
            IntPtr GetWindow();

            [PreserveSig]
            int ContextSensitiveHelp(
                   int fEnterMode);

            [PreserveSig]
            int GetBorder(
                   [Out]
                    NativeMethods.RECT lprectBorder);

            [PreserveSig]
            int RequestBorderSpace(
                   [In]
                    NativeMethods.RECT pborderwidths);

            [PreserveSig]
            int SetBorderSpace(
                   [In]
                    NativeMethods.RECT pborderwidths);

            void SetActiveObject(
                   [In, MarshalAs(UnmanagedType.Interface)]
                    IOleInPlaceActiveObject pActiveObject,
                   [In, MarshalAs(UnmanagedType.LPWStr)]
                    string pszObjName);
        }

        [ComImport(),
        Guid("00000117-0000-0000-C000-000000000046"),
        InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        public interface IOleInPlaceActiveObject
        {
            [PreserveSig]
            int GetWindow(out IntPtr hwnd);

            void ContextSensitiveHelp(
                    int fEnterMode);

            [PreserveSig]
            int TranslateAccelerator(
                   [In]
                    ref System.Windows.Interop.MSG lpmsg);

            void OnFrameWindowActivate(
                   int fActivate);

            void OnDocWindowActivate(
                   int fActivate);

            void ResizeBorder(
                   [In]
                    NativeMethods.RECT prcBorder,
                   [In]
                    IOleInPlaceUIWindow pUIWindow,
                   bool fFrameWindow);

            void EnableModeless(
                   int fEnable);
        }

        [ComImport(), Guid("00000114-0000-0000-C000-000000000046"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        public interface IOleWindow
        {
            [PreserveSig]
            int GetWindow([Out] out IntPtr hwnd);
            void ContextSensitiveHelp(int fEnterMode);
        }

        [ComImport(),
        Guid("00000113-0000-0000-C000-000000000046"),
        InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        public interface IOleInPlaceObject
        {
            [PreserveSig]
            int GetWindow([Out] out IntPtr hwnd);
            void ContextSensitiveHelp(int fEnterMode);
            void InPlaceDeactivate();
            [PreserveSig]
            int UIDeactivate();
            void SetObjectRects(
                [In] NativeMethods.COMRECT lprcPosRect,
                [In] NativeMethods.COMRECT lprcClipRect);

            void ReactivateAndUndo();
        }

        [ComImport(),
        Guid("00000112-0000-0000-C000-000000000046"),
        InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        public interface IOleObject
        {
            [PreserveSig]
            int SetClientSite(
                [In, MarshalAs(UnmanagedType.Interface)]
                IOleClientSite pClientSite);

            IOleClientSite GetClientSite();

            [PreserveSig]
            int SetHostNames(
                [In, MarshalAs(UnmanagedType.LPWStr)]
                string szContainerApp,
                [In, MarshalAs(UnmanagedType.LPWStr)]
                string szContainerObj);

            [PreserveSig]
            int Close(int dwSaveOption);

            [PreserveSig]
            int SetMoniker(
                   [In, MarshalAs(UnmanagedType.U4)]
                     int dwWhichMoniker,
                   [In, MarshalAs(UnmanagedType.Interface)]
                     object pmk);

            [PreserveSig]
            int GetMoniker(
                  [In, MarshalAs(UnmanagedType.U4)]
                     int dwAssign,
                  [In, MarshalAs(UnmanagedType.U4)]
                     int dwWhichMoniker,
                  [Out, MarshalAs(UnmanagedType.Interface)]
                     out object moniker);

            [PreserveSig]
            int InitFromData(
                   [In, MarshalAs(UnmanagedType.Interface)]
                     IComDataObject pDataObject,

                    int fCreation,
                   [In, MarshalAs(UnmanagedType.U4)]
                     int dwReserved);

            [PreserveSig]
            int GetClipboardData(
                   [In, MarshalAs(UnmanagedType.U4)]
                     int dwReserved,
                    out IComDataObject data);

            [PreserveSig]
            int DoVerb(

                    int iVerb,
                   [In]
                     IntPtr lpmsg,
                   [In, MarshalAs(UnmanagedType.Interface)]
                      IOleClientSite pActiveSite,

                    int lindex,

                    IntPtr hwndParent,
                   [In]
                     NativeMethods.COMRECT lprcPosRect);

            [PreserveSig]
            int EnumVerbs(out IEnumOLEVERB e);

            [PreserveSig]
            int OleUpdate();

            [PreserveSig]
            int IsUpToDate();

            [PreserveSig]
            int GetUserClassID(
                   [In, Out]
                      ref Guid pClsid);

            [PreserveSig]
            int GetUserType(
                   [In, MarshalAs(UnmanagedType.U4)]
                     int dwFormOfType,
                   [Out, MarshalAs(UnmanagedType.LPWStr)]
                     out string userType);

            [PreserveSig]
            int SetExtent(
                   [In, MarshalAs(UnmanagedType.U4)]
                     int dwDrawAspect,
                   [In]
                     NativeMethods.SIZE pSizel);

            [PreserveSig]
            int GetExtent(
                   [In, MarshalAs(UnmanagedType.U4)]
                     int dwDrawAspect,
                   [Out]
                     NativeMethods.SIZE pSizel);

            [PreserveSig]
            int Advise(
                    IAdviseSink pAdvSink,
                    out int cookie);

            [PreserveSig]
            int Unadvise(
                   [In, MarshalAs(UnmanagedType.U4)]
                     int dwConnection);

            [PreserveSig]
            int EnumAdvise(out IEnumSTATDATA e);

            [PreserveSig]
            int GetMiscStatus(
                   [In, MarshalAs(UnmanagedType.U4)]
                     int dwAspect,
                    out int misc);

            [PreserveSig]
            int SetColorScheme(
                   [In]
                      NativeMethods.tagLOGPALETTE pLogpal);
        }

        [ComImport(), Guid("1C2056CC-5EF4-101B-8BC8-00AA003E3B29"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        public interface IOleInPlaceObjectWindowless
        {
            [PreserveSig]
            int SetClientSite(
                   [In, MarshalAs(UnmanagedType.Interface)]
                      IOleClientSite pClientSite);

            [PreserveSig]
            int GetClientSite(out IOleClientSite site);

            [PreserveSig]
            int SetHostNames(
                   [In, MarshalAs(UnmanagedType.LPWStr)]
                      string szContainerApp,
                   [In, MarshalAs(UnmanagedType.LPWStr)]
                      string szContainerObj);

            [PreserveSig]
            int Close(

                    int dwSaveOption);

            [PreserveSig]
            int SetMoniker(
                   [In, MarshalAs(UnmanagedType.U4)]
                     int dwWhichMoniker,
                   [In, MarshalAs(UnmanagedType.Interface)]
                     object pmk);

            [PreserveSig]
            int GetMoniker(
                  [In, MarshalAs(UnmanagedType.U4)]
                     int dwAssign,
                  [In, MarshalAs(UnmanagedType.U4)]
                     int dwWhichMoniker,
                  [Out, MarshalAs(UnmanagedType.Interface)]
                     out object moniker);

            [PreserveSig]
            int InitFromData(
                   [In, MarshalAs(UnmanagedType.Interface)]
                     IComDataObject pDataObject,

                    int fCreation,
                   [In, MarshalAs(UnmanagedType.U4)]
                     int dwReserved);

            [PreserveSig]
            int GetClipboardData(
                   [In, MarshalAs(UnmanagedType.U4)]
                     int dwReserved,
                    out IComDataObject data);

            [PreserveSig]
            int DoVerb(

                    int iVerb,
                   [In]
                     IntPtr lpmsg,
                   [In, MarshalAs(UnmanagedType.Interface)]
                      IOleClientSite pActiveSite,

                    int lindex,

                    IntPtr hwndParent,
                   [In]
                     NativeMethods.RECT lprcPosRect);

            [PreserveSig]
            int EnumVerbs(out IEnumOLEVERB e);

            [PreserveSig]
            int OleUpdate();

            [PreserveSig]
            int IsUpToDate();

            [PreserveSig]
            int GetUserClassID(
                   [In, Out]
                      ref Guid pClsid);

            [PreserveSig]
            int GetUserType(
                   [In, MarshalAs(UnmanagedType.U4)]
                     int dwFormOfType,
                   [Out, MarshalAs(UnmanagedType.LPWStr)]
                     out string userType);

            [PreserveSig]
            int SetExtent(
                   [In, MarshalAs(UnmanagedType.U4)]
                     int dwDrawAspect,
                   [In]
                     NativeMethods.SIZE pSizel);

            [PreserveSig]
            int GetExtent(
                   [In, MarshalAs(UnmanagedType.U4)]
                     int dwDrawAspect,
                   [Out]
                     NativeMethods.SIZE pSizel);

            [PreserveSig]
            int Advise(
                   [In, MarshalAs(UnmanagedType.Interface)]
                     IAdviseSink pAdvSink,
                    out int cookie);

            [PreserveSig]
            int Unadvise(
                   [In, MarshalAs(UnmanagedType.U4)]
                     int dwConnection);

            [PreserveSig]
            int EnumAdvise(out IEnumSTATDATA e);

            [PreserveSig]
            int GetMiscStatus(
                   [In, MarshalAs(UnmanagedType.U4)]
                     int dwAspect,
                    out int misc);

            [PreserveSig]
            int SetColorScheme(
                   [In]
                      NativeMethods.tagLOGPALETTE pLogpal);

            [PreserveSig]
            int OnWindowMessage(
               [In, MarshalAs(UnmanagedType.U4)] int msg,
               [In, MarshalAs(UnmanagedType.U4)] int wParam,
               [In, MarshalAs(UnmanagedType.U4)] int lParam,
               [Out, MarshalAs(UnmanagedType.U4)] int plResult);

            [PreserveSig]
            int GetDropTarget(
               [Out, MarshalAs(UnmanagedType.Interface)] object ppDropTarget);
        };

        [ComImport(),
            Guid("B196B286-BAB4-101A-B69C-00AA00341D07"),
            InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        public interface IConnectionPoint
        {
            [PreserveSig]
            int GetConnectionInterface(out Guid iid);

            [PreserveSig]
            int GetConnectionPointContainer(
                [MarshalAs(UnmanagedType.Interface)]
                ref IConnectionPointContainer pContainer);

            [PreserveSig]
            int Advise(
                [In, MarshalAs(UnmanagedType.Interface)] object pUnkSink,
                ref int cookie);

            [PreserveSig]
            int Unadvise(
                     int cookie);

            [PreserveSig]
            int EnumConnections(out object pEnum);
        }

        [ComImport(), Guid("00020404-0000-0000-C000-000000000046"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        public interface IEnumVariant
        {
            [PreserveSig]
            int Next(
                    [In, MarshalAs(UnmanagedType.U4)]
                     int celt,
                    [In, Out]
                     IntPtr rgvar,
                    [Out, MarshalAs(UnmanagedType.LPArray)]
                     int[] pceltFetched);

            void Skip(
                   [In, MarshalAs(UnmanagedType.U4)]
                     int celt);

            void Reset();

            void Clone(
                   [Out, MarshalAs(UnmanagedType.LPArray)]
                       IEnumVariant[] ppenum);
        }

        [ComImport(), Guid("00000104-0000-0000-C000-000000000046"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        public interface IEnumOLEVERB
        {
            [PreserveSig]
            int Next(
                [MarshalAs(UnmanagedType.U4)]
                int celt,
                [Out]
                NativeMethods.tagOLEVERB rgelt,
                [Out, MarshalAs(UnmanagedType.LPArray)]
                int[] pceltFetched);

            [PreserveSig]
            int Skip(
                [In, MarshalAs(UnmanagedType.U4)]
                int celt);

            void Reset();

            void Clone(out IEnumOLEVERB ppenum);
        }

        [ComImport(),
        Guid("B196B284-BAB4-101A-B69C-00AA00341D07"),
        InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        public interface IConnectionPointContainer
        {
            [return: MarshalAs(UnmanagedType.Interface)]
            object EnumConnectionPoints();

            [PreserveSig]
            int FindConnectionPoint([In] ref Guid guid, [Out, MarshalAs(UnmanagedType.Interface)] out IConnectionPoint ppCP);
        }

        [ComImport(), Guid("B196B285-BAB4-101A-B69C-00AA00341D07"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        public interface IEnumConnectionPoints
        {
            [PreserveSig]
            int Next(int cConnections, out IConnectionPoint pCp, out int pcFetched);

            [PreserveSig]
            int Skip(int cSkip);

            void Reset();

            IEnumConnectionPoints Clone();
        }

#if !DRT && !UIAUTOMATIONTYPES
        [ComImport(), Guid("00020400-0000-0000-C000-000000000046"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        public interface IDispatch
        {
            #region <KeepInSync With="IDispatchEx">

            int GetTypeInfoCount();

            [return: MarshalAs(UnmanagedType.Interface)]
            ITypeInfo GetTypeInfo(
                    [In, MarshalAs(UnmanagedType.U4)]
                 int iTInfo,
                    [In, MarshalAs(UnmanagedType.U4)]
                 int lcid);

            [PreserveSig]
            HR GetIDsOfNames(
                   [In]
                 ref Guid riid,
                   [In, MarshalAs(UnmanagedType.LPArray)]
                 string[] rgszNames,
                   [In, MarshalAs(UnmanagedType.U4)]
                 int cNames,
                   [In, MarshalAs(UnmanagedType.U4)]
                 int lcid,
                   [Out, MarshalAs(UnmanagedType.LPArray)]
                 int[] rgDispId);


            [PreserveSig]
            HR Invoke(

                    int dispIdMember,
                   [In]
                 ref Guid riid,
                   [In, MarshalAs(UnmanagedType.U4)]
                 int lcid,
                   [In, MarshalAs(UnmanagedType.U4)]
                 int dwFlags,
                   [Out, In]
                  NativeMethods.DISPPARAMS pDispParams,
                   [Out]
                  out object pVarResult,
                   [Out, In]
                  NativeMethods.EXCEPINFO pExcepInfo,
                   [Out, MarshalAs(UnmanagedType.LPArray)]
                  IntPtr [] pArgErr);

            #endregion
        }

        [ComImport(), Guid("A6EF9860-C720-11D0-9337-00A0C90DCAA9"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        public interface IDispatchEx : IDispatch
        {
            #region <KeepInSync With="IDispatch">

            new int GetTypeInfoCount();

            [return: MarshalAs(UnmanagedType.Interface)]
            new ITypeInfo GetTypeInfo(
                    [In, MarshalAs(UnmanagedType.U4)]
                 int iTInfo,
                    [In, MarshalAs(UnmanagedType.U4)]
                 int lcid);

            [PreserveSig]
            new HR GetIDsOfNames(
                   [In]
                 ref Guid riid,
                   [In, MarshalAs(UnmanagedType.LPArray)]
                 string[] rgszNames,
                   [In, MarshalAs(UnmanagedType.U4)]
                 int cNames,
                   [In, MarshalAs(UnmanagedType.U4)]
                 int lcid,
                   [Out, MarshalAs(UnmanagedType.LPArray)]
                 int[] rgDispId);


            [PreserveSig]
            new HR Invoke(
                    int dispIdMember,
                   [In]
                 ref Guid riid,
                   [In, MarshalAs(UnmanagedType.U4)]
                 int lcid,
                   [In, MarshalAs(UnmanagedType.U4)]
                 int dwFlags,
                   [Out, In]
                  NativeMethods.DISPPARAMS pDispParams,
                   [Out]
                  out object pVarResult,
                   [Out, In]
                  NativeMethods.EXCEPINFO pExcepInfo,
                   [Out, MarshalAs(UnmanagedType.LPArray)]
                  IntPtr [] pArgErr);

            #endregion

            [PreserveSig]
            HR GetDispID(
                string name,
                int nameProperties,
                [Out] out int dispId);

            [PreserveSig]
            HR InvokeEx(
                int dispId,
                [MarshalAs(UnmanagedType.U4)] int lcid,
                [MarshalAs(UnmanagedType.U4)] int flags,
                [In, Out] NativeMethods.DISPPARAMS dispParams,
                [Out] out object result,
                /* COM interop caveat: Declaring the following just as Out seems to cause
                   garbage being handed out for the native buffer (it's out anyway). Upon
                   returning from the COM call, CLR copies back to the managed object but
                   chokes on the garbage string pointers trying to do memcpy, causing AV.*/
                [In, Out] NativeMethods.EXCEPINFO exceptionInfo,
                IServiceProvider serviceProvider);

            void DeleteMemberByName(string name, int flags);

            void DeleteMemberByDispID(int dispId);

            int GetMemberProperties(int dispId, int propFlags);

            string GetMemberName(int dispId);

            int GetNextDispID(int enumFlags, int dispId);

            [return: MarshalAs(UnmanagedType.IUnknown)]
            object GetNameSpaceParent();
        }

        [ComImport(), Guid("6D5140C1-7436-11CE-8034-00AA006009FA"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        public interface IServiceProvider
        {
            [return: MarshalAs(UnmanagedType.IUnknown)]
            object QueryService(ref Guid service, ref Guid riid);
        }

#endif

        #region WebBrowser Related Definitions
        [ComImport(), Guid("D30C1661-CDAF-11d0-8A3E-00C04FC9E26E"),
        TypeLibType(TypeLibTypeFlags.FHidden | TypeLibTypeFlags.FDual | TypeLibTypeFlags.FOleAutomation)]
        public interface IWebBrowser2
        {
            //
            // IWebBrowser members

            [DispId(100)]
            void GoBack();

            [DispId(101)]
            void GoForward();

            [DispId(102)]
            void GoHome();
            [DispId(103)]
            void GoSearch();
            [DispId(104)]
            void Navigate([In] string Url, [In] ref object flags,
              [In] ref object targetFrameName, [In] ref object postData,
              [In] ref object headers);

            [DispId(-550)]
            void Refresh();

            [DispId(105)]
            void Refresh2([In] ref object level);

            [DispId(106)]
            void Stop();
            [DispId(200)]
            object Application { [return: MarshalAs(UnmanagedType.IDispatch)] get; }
            [DispId(201)]
            object Parent { [return: MarshalAs(UnmanagedType.IDispatch)] get; }
            [DispId(202)]
            object Container { [return: MarshalAs(UnmanagedType.IDispatch)] get; }

            [DispId(203)]
            object Document
            {
                [return: MarshalAs(UnmanagedType.IDispatch)]
                get;
            }

            [DispId(204)]
            bool TopLevelContainer { get; }
            [DispId(205)]
            string Type { get; }
            [DispId(206)]
            int Left { get; set; }
            [DispId(207)]
            int Top { get; set; }
            [DispId(208)]
            int Width { get; set; }
            [DispId(209)]
            int Height { get; set; }
            [DispId(210)]
            string LocationName { get; }

            [DispId(211)]
            string LocationURL
            {
                get;
            }

            [DispId(212)]
            bool Busy { get; }
            //
            // IWebBrowserApp members
            [DispId(300)]
            void Quit();
            [DispId(301)]
            void ClientToWindow([Out] out int pcx, [Out] out int pcy);
            [DispId(302)]
            void PutProperty([In] string property, [In] object vtValue);
            [DispId(303)]
            object GetProperty([In] string property);
            [DispId(0)]
            string Name { get; }
            [DispId(-515)]
            int HWND { get; }
            [DispId(400)]
            string FullName { get; }
            [DispId(401)]
            string Path { get; }
            [DispId(402)]
            bool Visible { get; set; }
            [DispId(403)]
            bool StatusBar { get; set; }
            [DispId(404)]
            string StatusText { get; set; }
            [DispId(405)]
            int ToolBar { get; set; }
            [DispId(406)]
            bool MenuBar { get; set; }
            [DispId(407)]
            bool FullScreen { get; set; }

            //
            // IWebBrowser2 members
            [DispId(500)]
            void Navigate2([In] ref object URL, [In] ref object flags,
              [In] ref object targetFrameName, [In] ref object postData,
              [In] ref object headers);

            [DispId(501)]
            OLECMDF QueryStatusWB([In] int cmdID);
            [DispId(502)]
            void ExecWB([In] OLECMDID cmdID,
                [In] OLECMDEXECOPT cmdexecopt,
                ref object pvaIn,
                IntPtr pvaOut);
            [DispId(503)]
            void ShowBrowserBar([In] ref object pvaClsid, [In] ref object pvarShow,
                [In] ref object pvarSize);
            [DispId(-525)]
            NativeMethods.WebBrowserReadyState ReadyState { get; }
            [DispId(550)]
            bool Offline { get; set; }
            [DispId(551)]
            bool Silent { get; set; }
            [DispId(552)]
            bool RegisterAsBrowser { get; set; }
            [DispId(553)]
            bool RegisterAsDropTarget { get; set; }
            [DispId(554)]
            bool TheaterMode { get; set; }
            [DispId(555)]
            bool AddressBar { get; set; }
            [DispId(556)]
            bool Resizable { get; set; }
        }

        [ComImport(), Guid("34A715A0-6587-11D0-924A-0020AFC7AC4D"),
        InterfaceType(ComInterfaceType.InterfaceIsIDispatch),
        TypeLibType(TypeLibTypeFlags.FHidden)]
        public interface DWebBrowserEvents2
        {
            [DispId(102)]
            void StatusTextChange([In] string text);
            [DispId(108)]
            void ProgressChange([In] int progress, [In] int progressMax);
            [DispId(105)]
            void CommandStateChange([In] long command, [In] bool enable);
            [DispId(106)]
            void DownloadBegin();
            [DispId(104)]
            void DownloadComplete();
            [DispId(113)]
            void TitleChange([In] string text);
            [DispId(112)]
            void PropertyChange([In] string szProperty);
            [DispId(225)]
            void PrintTemplateInstantiation([In, MarshalAs(UnmanagedType.IDispatch)] object pDisp);
            [DispId(226)]
            void PrintTemplateTeardown([In, MarshalAs(UnmanagedType.IDispatch)] object pDisp);
            [DispId(227)]
            void UpdatePageStatus([In, MarshalAs(UnmanagedType.IDispatch)] object pDisp,
                [In] ref object nPage, [In] ref object fDone);
            [DispId(250)]
            void BeforeNavigate2([In, MarshalAs(UnmanagedType.IDispatch)] object pDisp,
                   [In] ref object URL, [In] ref object flags,
                   [In] ref object targetFrameName, [In] ref object postData,
                   [In] ref object headers, [In, Out] ref bool cancel);
            [DispId(251)]
            void NewWindow2([In, Out, MarshalAs(UnmanagedType.IDispatch)] ref object pDisp,
                  [In, Out] ref bool cancel);
            [DispId(252)]
            void NavigateComplete2([In, MarshalAs(UnmanagedType.IDispatch)] object pDisp,
                  [In] ref object URL);
            [DispId(259)]
            void DocumentComplete([In, MarshalAs(UnmanagedType.IDispatch)] object pDisp,
                  [In] ref object URL);
            [DispId(253)]
            void OnQuit();
            [DispId(254)]
            void OnVisible([In] bool visible);
            [DispId(255)]
            void OnToolBar([In] bool toolBar);
            [DispId(256)]
            void OnMenuBar([In] bool menuBar);
            [DispId(257)]
            void OnStatusBar([In] bool statusBar);
            [DispId(258)]
            void OnFullScreen([In] bool fullScreen);
            [DispId(260)]
            void OnTheaterMode([In] bool theaterMode);
            [DispId(262)]
            void WindowSetResizable([In] bool resizable);
            [DispId(264)]
            void WindowSetLeft([In] int left);
            [DispId(265)]
            void WindowSetTop([In] int top);
            [DispId(266)]
            void WindowSetWidth([In] int width);
            [DispId(267)]
            void WindowSetHeight([In] int height);
            [DispId(263)]
            void WindowClosing([In] bool isChildWindow, [In, Out] ref bool cancel);
            [DispId(268)]
            void ClientToHostWindow([In, Out] ref long cx, [In, Out] ref long cy);
            [DispId(269)]
            void SetSecureLockIcon([In] int secureLockIcon);
            [DispId(270)]
            void FileDownload([In, Out] ref bool ActiveDocument, [In, Out] ref bool cancel);
            [DispId(271)]
            void NavigateError([In, MarshalAs(UnmanagedType.IDispatch)] object pDisp,
                [In] ref object URL, [In] ref object frame, [In] ref object statusCode, [In, Out] ref bool cancel);
            [DispId(272)]
            void PrivacyImpactedStateChange([In] bool bImpacted);
            [DispId(282)] // IE 7+
            void SetPhishingFilterStatus(uint phishingFilterStatus);
            [DispId(283)] // IE 7+
            void WindowStateChanged(uint dwFlags, uint dwValidFlagsMask);
        }


        // Used to control the webbrowser appearance and provide DTE to script via window.external
        [ComImport(), Guid("BD3F23C0-D43E-11CF-893B-00AA00BDCE1A"),
        InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        internal interface IDocHostUIHandler
        {
            [return: MarshalAs(UnmanagedType.I4)]
            [PreserveSig]
            int ShowContextMenu(
                [In, MarshalAs(UnmanagedType.U4)]
                int dwID,
                ref NativeMethods.POINT pt,
                [In, MarshalAs(UnmanagedType.Interface)]
                object pcmdtReserved,
                [In, MarshalAs(UnmanagedType.Interface)]
                object pdispReserved);

            [return: MarshalAs(UnmanagedType.I4)]
            [PreserveSig]
            int GetHostInfo(
                [In, Out]
                NativeMethods.DOCHOSTUIINFO info);

            [return: MarshalAs(UnmanagedType.I4)]
            [PreserveSig]
            int ShowUI(
                [In, MarshalAs(UnmanagedType.I4)]
                int dwID,
                [In]
                IOleInPlaceActiveObject activeObject,
                [In]
                NativeMethods.IOleCommandTarget commandTarget,
                [In]
                IOleInPlaceFrame frame,
                [In]
                IOleInPlaceUIWindow doc);

            [return: MarshalAs(UnmanagedType.I4)]
            [PreserveSig]
            int HideUI();

            [return: MarshalAs(UnmanagedType.I4)]
            [PreserveSig]
            int UpdateUI();

            [return: MarshalAs(UnmanagedType.I4)]
            [PreserveSig]
            int EnableModeless(
                [In, MarshalAs(UnmanagedType.Bool)]
                bool fEnable);

            [return: MarshalAs(UnmanagedType.I4)]
            [PreserveSig]
            int OnDocWindowActivate(
                [In, MarshalAs(UnmanagedType.Bool)]
                bool fActivate);

            [return: MarshalAs(UnmanagedType.I4)]
            [PreserveSig]
            int OnFrameWindowActivate(
                [In, MarshalAs(UnmanagedType.Bool)]
                bool fActivate);

            [return: MarshalAs(UnmanagedType.I4)]
            [PreserveSig]
            int ResizeBorder(
                [In]
                NativeMethods.COMRECT rect,
                [In]
                IOleInPlaceUIWindow doc,
                bool fFrameWindow);

            [return: MarshalAs(UnmanagedType.I4)]
            [PreserveSig]
            int TranslateAccelerator(
                [In]
                ref System.Windows.Interop.MSG msg,
                [In]
                ref Guid group,
                [In, MarshalAs(UnmanagedType.I4)]
                int nCmdID);

            [return: MarshalAs(UnmanagedType.I4)]
            [PreserveSig]
            int GetOptionKeyPath(
                [Out, MarshalAs(UnmanagedType.LPArray)]
                String[] pbstrKey,
                [In, MarshalAs(UnmanagedType.U4)]
                int dw);

            [return: MarshalAs(UnmanagedType.I4)]
            [PreserveSig]
            int GetDropTarget(
                [In, MarshalAs(UnmanagedType.Interface)]
                IOleDropTarget pDropTarget,
                [Out, MarshalAs(UnmanagedType.Interface)]
                out IOleDropTarget ppDropTarget);

            [return: MarshalAs(UnmanagedType.I4)]
            [PreserveSig]
            int GetExternal(
                [Out, MarshalAs(UnmanagedType.IDispatch)]
                out object ppDispatch);

            [return: MarshalAs(UnmanagedType.I4)]
            [PreserveSig]
            int TranslateUrl(
                [In, MarshalAs(UnmanagedType.U4)]
                int dwTranslate,
                [In, MarshalAs(UnmanagedType.LPWStr)]
                string strURLIn,
                [Out, MarshalAs(UnmanagedType.LPWStr)]
                out string pstrURLOut);

            [return: MarshalAs(UnmanagedType.I4)]
            [PreserveSig]
            int FilterDataObject(
                IComDataObject pDO,
                out IComDataObject ppDORet);
        }

        [ComImport, Guid("3050F21F-98B5-11CF-BB82-00AA00BDCE0B"), InterfaceType(ComInterfaceType.InterfaceIsDual)]
        internal interface IHTMLElementCollection
        {
            string toString();
            void SetLength(int p);
            int GetLength();
            [return: MarshalAs(UnmanagedType.Interface)]
            object Get_newEnum();
            [return: MarshalAs(UnmanagedType.IDispatch)]
            object Item(object idOrName, object index);
            [return: MarshalAs(UnmanagedType.Interface)]
            object Tags(object tagName);
        };

        [ComImport, Guid("626FC520-A41E-11CF-A731-00A0C9082637"), InterfaceType(ComInterfaceType.InterfaceIsDual)]
        internal interface IHTMLDocument
        {
            [return: MarshalAs(UnmanagedType.IDispatch)]
            object GetScript();
        }

        [ComImport, Guid("332C4425-26CB-11D0-B483-00C04FD90119"), InterfaceType(ComInterfaceType.InterfaceIsDual)]
        internal interface IHTMLDocument2 : IHTMLDocument
        {
            #region IHTMLDocument - base interface
            [return: MarshalAs(UnmanagedType.Interface)]
            new object GetScript();
            #endregion
            IHTMLElementCollection GetAll();
            [return: MarshalAs(UnmanagedType.Interface)]
            /*IHTMLElement*/
            object GetBody();
            [return: MarshalAs(UnmanagedType.Interface)]
            /*IHTMLElement*/
            object GetActiveElement();
            IHTMLElementCollection GetImages();
            IHTMLElementCollection GetApplets();
            IHTMLElementCollection GetLinks();
            IHTMLElementCollection GetForms();
            IHTMLElementCollection GetAnchors();
            void SetTitle(string p);
            string GetTitle();
            IHTMLElementCollection GetScripts();
            void SetDesignMode(string p);
            string GetDesignMode();
            [return: MarshalAs(UnmanagedType.Interface)]
            object GetSelection();
            string GetReadyState();
            [return: MarshalAs(UnmanagedType.Interface)]
            object GetFrames();
            IHTMLElementCollection GetEmbeds();
            IHTMLElementCollection GetPlugins();
            void SetAlinkColor(object c);
            object GetAlinkColor();
            void SetBgColor(object c);
            object GetBgColor();
            void SetFgColor(object c);
            object GetFgColor();
            void SetLinkColor(object c);
            object GetLinkColor();
            void SetVlinkColor(object c);
            object GetVlinkColor();
            string GetReferrer();
            IHTMLLocation GetLocation();
            string GetLastModified();
            void SetUrl(string p);
            string GetUrl();
            void SetDomain(string p);
            string GetDomain();
            void SetCookie(string p);
            string GetCookie();
            void SetExpando(bool p);
            bool GetExpando();
            void SetCharset(string p);
            string GetCharset();
            void SetDefaultCharset(string p);
            string GetDefaultCharset();
            string GetMimeType();
            string GetFileSize();
            string GetFileCreatedDate();
            string GetFileModifiedDate();
            string GetFileUpdatedDate();
            string GetSecurity();
            string GetProtocol();
            string GetNameProp();
            int Write([In, MarshalAs(UnmanagedType.SafeArray)] object[] psarray);
            int WriteLine([In, MarshalAs(UnmanagedType.SafeArray)] object[] psarray);
            [return: MarshalAs(UnmanagedType.Interface)]
            object Open(string mimeExtension, object name, object features, object replace);
            void Close();
            void Clear();
            bool QueryCommandSupported(string cmdID);
            bool QueryCommandEnabled(string cmdID);
            bool QueryCommandState(string cmdID);
            bool QueryCommandIndeterm(string cmdID);
            string QueryCommandText(string cmdID);
            object QueryCommandValue(string cmdID);
            bool ExecCommand(string cmdID, bool showUI, object value);
            bool ExecCommandShowHelp(string cmdID);
            [return: MarshalAs(UnmanagedType.Interface)]
            /*IHTMLElement*/
            object CreateElement(string eTag);
            void SetOnhelp(object p);
            object GetOnhelp();
            void SetOnclick(object p);
            object GetOnclick();
            void SetOndblclick(object p);
            object GetOndblclick();
            void SetOnkeyup(object p);
            object GetOnkeyup();
            void SetOnkeydown(object p);
            object GetOnkeydown();
            void SetOnkeypress(object p);
            object GetOnkeypress();
            void SetOnmouseup(object p);
            object GetOnmouseup();
            void SetOnmousedown(object p);
            object GetOnmousedown();
            void SetOnmousemove(object p);
            object GetOnmousemove();
            void SetOnmouseout(object p);
            object GetOnmouseout();
            void SetOnmouseover(object p);
            object GetOnmouseover();
            void SetOnreadystatechange(object p);
            object GetOnreadystatechange();
            void SetOnafterupdate(object p);
            object GetOnafterupdate();
            void SetOnrowexit(object p);
            object GetOnrowexit();
            void SetOnrowenter(object p);
            object GetOnrowenter();
            void SetOndragstart(object p);
            object GetOndragstart();
            void SetOnselectstart(object p);
            object GetOnselectstart();
            [return: MarshalAs(UnmanagedType.Interface)]
            /*IHTMLElement*/
            object ElementFromPoint(int x, int y);
            [return: MarshalAs(UnmanagedType.Interface)]
            /*IHTMLWindow2*/
            object GetParentWindow();
            [return: MarshalAs(UnmanagedType.Interface)]
            object GetStyleSheets();
            void SetOnbeforeupdate(object p);
            object GetOnbeforeupdate();
            void SetOnerrorupdate(object p);
            object GetOnerrorupdate();
            string toString();
            [return: MarshalAs(UnmanagedType.Interface)]
            object CreateStyleSheet(string bstrHref, int lIndex);
        };

        [ComImport, InterfaceType(ComInterfaceType.InterfaceIsDual), Guid("163BB1E0-6E00-11CF-837A-48DC04C10000")]
        internal interface IHTMLLocation
        {
            void SetHref(string p);
            string GetHref();
            void SetProtocol(string p);
            string GetProtocol();
            void SetHost(string p);
            string GetHost();
            void SetHostname(string p);
            string GetHostname();
            void SetPort(string p);
            string GetPort();
            void SetPathname(string p);
            string GetPathname();
            void SetSearch(string p);
            string GetSearch();
            void SetHash(string p);
            string GetHash();
            void Reload(bool flag);
            void Replace(string bstr);
            void Assign(string bstr);
        };

        internal static class ArrayToVARIANTHelper
        {
            static ArrayToVARIANTHelper()
            {
                VariantSize = (int)Marshal.OffsetOf(typeof(FindSizeOfVariant), "b");
            }

            // Convert a object[] into an array of VARIANT, allocated with CoTask allocators.
            public static unsafe IntPtr ArrayToVARIANTVector(object[] args)
            {
                IntPtr mem = IntPtr.Zero;
                int i = 0;
                try
                {
                    checked
                    {
                        int len = args.Length;
                        mem = Marshal.AllocCoTaskMem(len * VariantSize);
                        byte* a = (byte*)(void*)mem;
                        for (i = 0; i < len; ++i)
                        {
                            Marshal.GetNativeVariantForObject(args[i], (IntPtr)(a + VariantSize * i));
                        }
                    }
                }
                catch
                {
                    if (mem != IntPtr.Zero)
                    {
                        FreeVARIANTVector(mem, i);
                    }
                    throw;
                }
                return mem;
            }

            // Free a Variant array created with the above function
            /// <param name="mem">The allocated memory to be freed.</param>
            /// <param name="len">The length of the Variant vector to be cleared.</param>
            public static unsafe void FreeVARIANTVector(IntPtr mem, int len)
            {
                int hr = NativeMethods.S_OK;
                byte* a = (byte*)(void*)mem;

                for (int i = 0; i < len; ++i)
                {
                    int hrcurrent = NativeMethods.S_OK;
                    checked
                    {
                        hrcurrent = VariantClear((IntPtr)(a + VariantSize * i));
                    }

                    // save the first error and throw after we finish all VariantClear.
                    if (NativeMethods.Succeeded(hr) && NativeMethods.Failed(hrcurrent))
                    {
                        hr = hrcurrent;
                    }
                }
                Marshal.FreeCoTaskMem(mem);

                if (NativeMethods.Failed(hr))
                {
                    Marshal.ThrowExceptionForHR(hr);
                }
            }

            [StructLayout(LayoutKind.Sequential, Pack = 1)]
            private struct FindSizeOfVariant
            {
                [MarshalAs(UnmanagedType.Struct)]
                public object var;
                public byte b;
            }

            private static readonly int VariantSize;
        }

        [DllImport(ExternDll.Oleaut32, PreserveSig = true)]
        private static extern int VariantClear(IntPtr pObject);

        [ComImport(), Guid("7FD52380-4E07-101B-AE2D-08002B2EC713"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        internal interface IPersistStreamInit
        {
            void GetClassID(
                   [Out]
                  out Guid pClassID);

            [PreserveSig]
            int IsDirty();

            void Load(
                   [In, MarshalAs(UnmanagedType.Interface)]
                  System.Runtime.InteropServices.ComTypes.IStream pstm);

            void Save(
                [In, MarshalAs(UnmanagedType.Interface)] object pstm,
                [In, MarshalAs(UnmanagedType.Bool)] bool fClearDirty);

            void GetSizeMax(
                   [Out, MarshalAs(UnmanagedType.LPArray)]
                 long pcbSize);

            void InitNew();
        }
        #endregion WebBrowser Related Definitions

        [DllImport(ExternDll.User32, SetLastError = true, CharSet = CharSet.Auto)]
        public static extern uint GetRawInputDeviceList(
                                                [In, Out] NativeMethods.RAWINPUTDEVICELIST[] ridl,
                                                [In, Out] ref uint numDevices,
                                                uint sizeInBytes);

        [DllImport(ExternDll.User32, SetLastError = true, CharSet = CharSet.Auto)]
        public static extern uint GetRawInputDeviceInfo(
                                                IntPtr hDevice,
                                                uint command,
                                                [In] ref NativeMethods.RID_DEVICE_INFO ridInfo,
                                                ref uint sizeInBytes);

#if !DRT && !UIAUTOMATIONTYPES

        /// <summary>
        /// Set the DPI awareness for the current thread to the provided value.
        /// </summary>
        /// <param name="dpiContext">
        /// The new DPI_AWARENESS_CONTEXT for the current thread. This context includes the DPI_AWARENESS value.
        /// </param>
        /// <returns>
        /// The old DPI_AWARENESS_CONTEXT for the thread. If the dpiContext is invalid, the thread will not
        /// be updated and the return value will be NULL. You can use this value to restore the old DPI_AWARENESS_CONTEXT
        /// after overriding it with a predefined value.
        /// </returns>
        /// <remarks>
        /// Use this API to change the DPI_AWARENESS_CONTEXT for the thread from the default value for the app.
        /// 
        /// Minimum supported client: Windows 10, version 1607 (RS1)
        /// </remarks>
        [DllImport(ExternDll.User32, CallingConvention = CallingConvention.Winapi)]
        internal static extern DpiAwarenessContextHandle SetThreadDpiAwarenessContext(DpiAwarenessContextHandle dpiContext);
#endif

        /// <summary>
        /// The EnumDisplayMonitors function enumerates display monitors (including invisible pseudo-monitors
        /// associated with the mirroring drivers) that intersect a region formed by the intersection of a specified
        /// clipping rectangle and the visible region of a device context. EnumDisplayMonitors calls an
        /// application-defined MonitorEnumProc callback function once for each monitor that is enumerated.
        /// Note that GetSystemMetrics (SM_CMONITORS) counts only the display monitors.
        /// </summary>
        /// <param name="hdc">
        /// A handle to a display device context that defines the visible region of interest.
        /// 
        /// If this parameter is NULL, the hdcMonitor parameter passed to the callback function
        /// will be NULL, and the visible region of interest is the virtual screen that encompasses all
        /// the displays on the desktop.
        /// </param>
        /// <param name="lprcClip">
        /// A pointer to a RECT structure that specifies a clipping rectangle. The region of interest
        /// is the intersection of the clipping rectangle with the visible region specified by hdc.
        /// 
        /// If hdc is non-NULL, the coordinates of the clipping rectangle are relative to the origin
        /// of the hdc.If hdc is NULL, the coordinates are virtual-screen coordinates.
        /// 
        /// This parameter can be NULL if you don't want to clip the region specified by hdc.
        /// </param>
        /// <param name="lpfnEnum">A pointer to a MonitorEnumProc application-defined callback function.</param>
        /// <param name="lParam">Application-defined data that EnumDisplayMonitors passes directly to the
        /// MonitorEnumProc function.</param>
        /// <returns>
        /// If the function succeeds, the return value is true, otherwise the
        /// return value is false.
        /// </returns>
        /// <remarks>
        /// There are two reasons to call the EnumDisplayMonitors function:
        /// 
        ///     You want to draw optimally into a device context that spans several display monitors, and the monitors have different color formats.
        ///     You want to obtain a handle and position rectangle for one or more display monitors.
        /// 
        /// To determine whether all the display monitors in a system share the same color format, call GetSystemMetrics (SM_SAMEDISPLAYFORMAT).
        /// 
        /// You do not need to use the EnumDisplayMonitors function when a window spans display monitors that have different color formats.
        /// You can continue to paint under the assumption that the entire screen has the color properties of the primary monitor.Your windows will
        /// look fine.EnumDisplayMonitors just lets you make them look better.
        /// 
        /// Setting the hdc parameter to NULL lets you use the EnumDisplayMonitors function to obtain a handle and position rectangle for
        /// one or more display monitors.The following table shows how the four combinations of NULL and non-NULLhdc and lprcClip values affect
        /// the behavior of the EnumDisplayMonitors function.
        /// 
        /// +-----------------------------------------------------------------------------------+----------+--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------+
        /// |                                        hdc                                        | lprcRect |                                                                          EnumDisplayMonitors behavior                                                                          |
        /// +-----------------------------------------------------------------------------------+----------+--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------+
        /// | NULL                                                                              | NULL     | Enumerates all display monitors.                                                                                                                                               |
        /// | The callback function receives a NULL HDC.                                        |          |                                                                                                                                                                                |
        /// | NULL                                                                              | non-NULL | Enumerates all display monitors that intersect the clipping rectangle.Use virtual screen coordinates for the clipping rectangle.                                               |
        /// | The callback function receives a NULL HDC.                                        |          |                                                                                                                                                                                |
        /// | non-NULL                                                                          | NULL     | Enumerates all display monitors that intersect the visible region of the device context.                                                                                       |
        /// | The callback function receives a handle to a DC for the specific display monitor. |          |                                                                                                                                                                                |
        /// | non-NULL                                                                          | non-NULL | Enumerates all display monitors that intersect the visible region of the device context and the clipping rectangle.Use device context coordinates for the clipping rectangle.  |
        /// | The callback function receives a handle to a DC for the specific display monitor. |          |                                                                                                                                                                                |
        /// +-----------------------------------------------------------------------------------+----------+--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------+
        /// </remarks>
        [DllImport(ExternDll.User32, CallingConvention = CallingConvention.Winapi)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool EnumDisplayMonitors(
            IntPtr hdc,
            IntPtr lprcClip,
            NativeMethods.MonitorEnumProc lpfnEnum,
            IntPtr lParam);
    }
}
