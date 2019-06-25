// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.ComponentModel;
using System.Diagnostics;

namespace MS.Win32
{
    using Accessibility;
    using SRCS = System.Runtime.CompilerServices;
    using System.Runtime.ConstrainedExecution;
    using System.Runtime.InteropServices;
    using System;
    using System.Collections;
    using System.IO;
    using System.Text;
    using System.Security;
    using Microsoft.Win32.SafeHandles;
    using MS.Internal;
    using MS.Internal.Interop;
    using MS.Utility;

#if WINDOWS_BASE
    using MS.Internal.WindowsBase;
#elif PRESENTATION_CORE
    using MS.Internal.PresentationCore;
#elif PRESENTATIONFRAMEWORK
    using MS.Internal.PresentationFramework;
#elif DRT
    using MS.Internal.Drt;
#else
#error Attempt to use FriendAccessAllowedAttribute from an unknown assembly.
    using MS.Internal.YourAssemblyName;
#endif

    using IComDataObject = System.Runtime.InteropServices.ComTypes.IDataObject;

    [FriendAccessAllowed]
    internal partial class UnsafeNativeMethods
    {
        [DllImport(ExternDll.Kernel32, CharSet=CharSet.Unicode, SetLastError=true, EntryPoint="GetTempFileName")]
        internal static extern uint _GetTempFileName(string tmpPath, string prefix, uint uniqueIdOrZero, StringBuilder tmpFileName);

        internal static uint GetTempFileName(string tmpPath, string prefix, uint uniqueIdOrZero, StringBuilder tmpFileName)
        {
            uint result = _GetTempFileName(tmpPath, prefix, uniqueIdOrZero, tmpFileName);
            if (result == 0)
            {
                throw new Win32Exception();
            }

            return result;
        }

        [DllImport(ExternDll.Shell32, CharSet = System.Runtime.InteropServices.CharSet.Auto, BestFitMapping = false, ThrowOnUnmappableChar = true)]
        internal static extern int ExtractIconEx(
                                        string szExeFileName,
                                        int nIconIndex,
                                        out NativeMethods.IconHandle phiconLarge,
                                        out NativeMethods.IconHandle phiconSmall,
                                        int nIcons);

        [DllImport(ExternDll.User32, CharSet = System.Runtime.InteropServices.CharSet.Auto, SetLastError=true)]
        internal static extern NativeMethods.IconHandle CreateIcon(IntPtr hInstance, int nWidth, int nHeight, byte cPlanes, byte cBitsPixel, byte[] lpbANDbits, byte[] lpbXORbits);

        [DllImport(ExternDll.User32, SetLastError = true)]
        public static extern bool CreateCaret(HandleRef hwnd, NativeMethods.BitmapHandle hbitmap, int width, int height);

        [DllImport(ExternDll.User32, SetLastError = true)]
        public static extern bool ShowCaret(HandleRef hwnd);

        [DllImport(ExternDll.User32, SetLastError = true)]
        public static extern bool HideCaret(HandleRef hwnd);

        [DllImport(ExternDll.User32, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Auto)]
        public static extern bool ShowWindowAsync(HandleRef hWnd, int nCmdShow);

        [DllImport(ExternDll.User32, EntryPoint="LoadImage", CharSet = System.Runtime.InteropServices.CharSet.Auto, SetLastError = true, BestFitMapping = false, ThrowOnUnmappableChar = true)]
        internal static extern NativeMethods.IconHandle LoadImageIcon(IntPtr hinst, string stName, int nType, int cxDesired, int cyDesired, int nFlags);

        [DllImport(ExternDll.User32, EntryPoint="LoadImage", CharSet = System.Runtime.InteropServices.CharSet.Auto, SetLastError = true, BestFitMapping = false, ThrowOnUnmappableChar = true)]
        internal static extern NativeMethods.CursorHandle LoadImageCursor(IntPtr hinst, string stName, int nType, int cxDesired, int cyDesired, int nFlags);
        // uncomment this if you plan to use LoadImage to load anything other than Icons/Cursors.
        /*
                [DllImport(ExternDll.User32, CharSet = System.Runtime.InteropServices.CharSet.Auto, SetLastError = true, BestFitMapping = false, ThrowOnUnmappableChar = true)]
                internal static extern SafeHandle LoadImage(
                    IntPtr hinst, string stName, int nType, int cxDesired, int cyDesired, int nFlags);
        */
        /*
                [DllImport(ExternDll.User32, CharSet = System.Runtime.InteropServices.CharSet.Auto, SetLastError = true, BestFitMapping = false, ThrowOnUnmappableChar = true)]
                internal static extern NativeMethods.IconHandle LoadImage(
                    IntPtr hinst, string stName, int nType, int cxDesired, int cyDesired, int nFlags);
        */

        [DllImport( ExternDll.Urlmon, ExactSpelling=true)]
        internal static extern int CoInternetIsFeatureEnabled( int featureEntry , int dwFlags );

        [DllImport( ExternDll.Urlmon, ExactSpelling=true)]
        internal static extern int CoInternetSetFeatureEnabled( int featureEntry , int dwFlags, bool fEnable );

        [DllImport( ExternDll.Urlmon, ExactSpelling=true)]
        internal static extern int CoInternetIsFeatureZoneElevationEnabled(
                                                            [MarshalAs(UnmanagedType.LPWStr)] string szFromURL,
                                                            [MarshalAs(UnmanagedType.LPWStr)] string szToURL,
                                                            UnsafeNativeMethods.IInternetSecurityManager secMgr,
                                                            int dwFlags
                                                            );


        [DllImport(ExternDll.PresentationHostDll, EntryPoint = "ProcessUnhandledException")]
        internal static extern void ProcessUnhandledException_DLL([MarshalAs(UnmanagedType.BStr)] string errMsg);

        [DllImport(ExternDll.Kernel32, CharSet=CharSet.Unicode)]
        internal static extern bool GetVersionEx([In, Out] NativeMethods.OSVERSIONINFOEX ver);
        [DllImport( ExternDll.Urlmon, ExactSpelling=true)]
        internal static extern int CoInternetCreateSecurityManager(
                                                                    [MarshalAs(UnmanagedType.Interface)] object pIServiceProvider,
                                                                    [MarshalAs(UnmanagedType.Interface)] out object ppISecurityManager ,
                                                                    int dwReserved ) ;




        [ComImport, ComVisible(false), Guid("79eac9ee-baf9-11ce-8c82-00aa004ba90b"), System.Runtime.InteropServices.InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        internal interface IInternetSecurityManager
        {
            void SetSecuritySite( NativeMethods.IInternetSecurityMgrSite pSite);

            unsafe void GetSecuritySite( /* [out] */ void **ppSite);

            void MapUrlToZone(
                                [In, MarshalAs(UnmanagedType.BStr)]
                                        string pwszUrl,
                                [Out]   out int pdwZone,
                                [In]    int     dwFlags);

            unsafe void GetSecurityId(  /* [in] */ string pwszUrl,
                                /* [size_is][out] */ byte *pbSecurityId,
                                /* [out][in] */ int *pcbSecurityId,
                                /* [in] */ int dwReserved);

            unsafe void ProcessUrlAction(
                                /* [in] */ string pwszUrl,
                                /* [in] */ int dwAction,
                                /* [size_is][out] */ byte *pPolicy,
                                /* [in] */ int cbPolicy,
                                /* [in] */ byte *pContext,
                                /* [in] */ int cbContext,
                                /* [in] */ int dwFlags,
                                /* [in] */ int dwReserved);

            unsafe void QueryCustomPolicy(
                                /* [in] */ string pwszUrl,
                                /* [in] */ /*REFGUID*/ void *guidKey,
                                /* [size_is][size_is][out] */ byte **ppPolicy,
                                /* [out] */ int *pcbPolicy,
                                /* [in] */ byte *pContext,
                                /* [in] */ int cbContext,
                                /* [in] */ int dwReserved);

            unsafe void SetZoneMapping( /* [in] */ int dwZone, /* [in] */ string lpszPattern, /* [in] */ int dwFlags);

            unsafe void GetZoneMappings( /* [in] */ int dwZone, /* [out] */ /*IEnumString*/ void **ppenumString, /* [in] */ int dwFlags);
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="hMem"></param>
        /// <returns></returns>
        [DllImport(ExternDll.Kernel32, SetLastError = true), ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
        internal static extern IntPtr LocalFree(IntPtr hMem);

#if BASE_NATIVEMETHODS
        [DllImport(ExternDll.Kernel32, SetLastError = true, CharSet = CharSet.Auto, BestFitMapping = false, ThrowOnUnmappableChar = true)]
        internal unsafe static extern SafeFileHandle CreateFile(
          string lpFileName,
          uint dwDesiredAccess,
          uint dwShareMode,
          [In] NativeMethods.SECURITY_ATTRIBUTES lpSecurityAttributes,
          int dwCreationDisposition,
          int dwFlagsAndAttributes,
          IntPtr hTemplateFile);
#endif


#if BASE_NATIVEMETHODS

        [DllImport(ExternDll.User32, CharSet = CharSet.Auto)]
        internal static extern IntPtr GetMessageExtraInfo();
#endif

#if BASE_NATIVEMETHODS
        [DllImport(ExternDll.Kernel32, EntryPoint="WaitForMultipleObjectsEx", SetLastError = true, CharSet = CharSet.Auto)]
        private static extern int IntWaitForMultipleObjectsEx(int nCount, IntPtr[] pHandles, bool bWaitAll, int dwMilliseconds, bool bAlertable);

        public const int WAIT_FAILED = unchecked((int)0xFFFFFFFF);

        internal static int WaitForMultipleObjectsEx(int nCount, IntPtr[] pHandles, bool bWaitAll, int dwMilliseconds, bool bAlertable)
        {
            int result = IntWaitForMultipleObjectsEx(nCount, pHandles, bWaitAll, dwMilliseconds, bAlertable);
            if(result ==  UnsafeNativeMethods.WAIT_FAILED)
            {
                throw new Win32Exception();
            }

            return result;
        }

        [DllImport(ExternDll.User32, EntryPoint="MsgWaitForMultipleObjectsEx", SetLastError=true, ExactSpelling = true, CharSet = CharSet.Auto)]
        private static extern int IntMsgWaitForMultipleObjectsEx(int nCount, IntPtr[] pHandles, int dwMilliseconds, int dwWakeMask, int dwFlags);

        internal static int MsgWaitForMultipleObjectsEx(int nCount, IntPtr[] pHandles, int dwMilliseconds, int dwWakeMask, int dwFlags)
        {
            int result = IntMsgWaitForMultipleObjectsEx(nCount, pHandles, dwMilliseconds, dwWakeMask, dwFlags);
            if(result == -1)
            {
                throw new Win32Exception();
            }

            return result;
        }
#endif

        [DllImport(ExternDll.User32, EntryPoint="RegisterClassEx", CharSet=CharSet.Unicode, SetLastError=true, BestFitMapping=false)]
        internal static extern UInt16 IntRegisterClassEx(NativeMethods.WNDCLASSEX_D wc_d);

        internal static UInt16 RegisterClassEx(NativeMethods.WNDCLASSEX_D wc_d)
        {
            UInt16 result = IntRegisterClassEx(wc_d);
            if(result == 0)
            {
                throw new Win32Exception();
            }

            return result;
        }

        [DllImport(ExternDll.User32, EntryPoint="UnregisterClass",CharSet = CharSet.Auto, SetLastError = true, BestFitMapping=false)]
        internal static extern int IntUnregisterClass(IntPtr atomString /*lpClassName*/ , IntPtr hInstance);

        internal static void UnregisterClass(IntPtr atomString /*lpClassName*/ , IntPtr hInstance)
        {
            int result = IntUnregisterClass(atomString, hInstance);
            if (result == 0)
            {
                throw new Win32Exception();
            }
        }

#if !DRT

        [DllImport("user32.dll", EntryPoint="ChangeWindowMessageFilter", SetLastError=true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool IntChangeWindowMessageFilter(WindowMessage message, MSGFLT dwFlag);

        [DllImport("user32.dll", EntryPoint = "ChangeWindowMessageFilterEx", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool IntChangeWindowMessageFilterEx(IntPtr hwnd, WindowMessage message, MSGFLT action, [In, Out, Optional] ref CHANGEFILTERSTRUCT pChangeFilterStruct);

        // Note that processes at or below SECURITY_MANDATORY_LOW_RID are not allowed to change the message filter.
        // If those processes call this function, it will fail and generate the extended error code, ERROR_ACCESS_DENIED.
        internal static MS.Internal.Interop.HRESULT ChangeWindowMessageFilterEx(IntPtr hwnd, WindowMessage message, MSGFLT action, out MSGFLTINFO extStatus)
        {
            extStatus = MSGFLTINFO.NONE;

            // This API were added for Vista.  The Ex version was added for Windows 7.
            // If we're not on either, then this message filter isolation doesn't exist.
            if (!Utilities.IsOSVistaOrNewer)
            {
                return MS.Internal.Interop.HRESULT.S_FALSE;
            }

            // If we're on Vista rather than Win7 then we can't use the Ex version of this function.
            // The Ex version is preferred if possible because this results in process-wide modifications of the filter
            // and is deprecated as of Win7.
            if (!Utilities.IsOSWindows7OrNewer)
            {
                // Note that the Win7 MSGFLT_ALLOW/DISALLOW enum values map to the Vista MSGFLT_ADD/REMOVE
                if (!IntChangeWindowMessageFilter(message, action))
                {
                    return (MS.Internal.Interop.HRESULT)Win32Error.GetLastError();
                }
                return MS.Internal.Interop.HRESULT.S_OK;
            }

            var filterstruct = new CHANGEFILTERSTRUCT { cbSize = (uint)Marshal.SizeOf(typeof(CHANGEFILTERSTRUCT)) };
            if (!IntChangeWindowMessageFilterEx(hwnd, message, action, ref filterstruct))
            {
                return (MS.Internal.Interop.HRESULT)Win32Error.GetLastError();
            }

            extStatus = filterstruct.ExtStatus;
            return MS.Internal.Interop.HRESULT.S_OK;
        }

        [DllImport(ExternDll.Urlmon, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Ansi, BestFitMapping = false, ThrowOnUnmappableChar = true)]
        private static extern MS.Internal.Interop.HRESULT ObtainUserAgentString(int dwOption, StringBuilder userAgent, ref int length);

        internal static string ObtainUserAgentString()
        {
            int length = MS.Win32.NativeMethods.MAX_PATH;
            StringBuilder userAgentBuffer = new StringBuilder(length);
            MS.Internal.Interop.HRESULT hr = ObtainUserAgentString(0 /*reserved. must be 0*/, userAgentBuffer, ref length);

            // Installing .NET 4.0 adds two parts to the user agent string, i.e.
            // .NET4.0C and .NET4.0E, potentially causing the user agent string to overflow its
            // documented maximum length of MAX_PATH. Turns out ObtainUserAgentString can return
            // a longer string if asked to do so. Therefore we grow the string dynamically when
            // needed, accommodating for this failure condition.
            if (hr == MS.Internal.Interop.HRESULT.E_OUTOFMEMORY)
            {
                userAgentBuffer = new StringBuilder(length);
                hr = ObtainUserAgentString(0 /*reserved. must be 0*/, userAgentBuffer, ref length);
            }

            hr.ThrowIfFailed();

            return userAgentBuffer.ToString();
        }

        // note that this method exists in UnsafeNativeMethodsCLR.cs but with a different signature
        // using a HandleRef for the hWnd instead of an IntPtr, and not using an IntPtr for lParam
        [DllImport(ExternDll.User32, CharSet = CharSet.Auto)]
        internal static extern IntPtr SendMessage(IntPtr hWnd, WindowMessage msg, IntPtr wParam, IntPtr lParam);


        // note that this method exists in UnsafeNativeMethodsCLR.cs but with a different signature
        // using a HandleRef for the hWnd instead of an IntPtr, and not using an IntPtr for lParam
        [DllImport(ExternDll.User32,EntryPoint="SendMessage", CharSet = CharSet.Auto)]
        internal static extern IntPtr UnsafeSendMessage(IntPtr hWnd, WindowMessage msg, IntPtr wParam, IntPtr lParam);

        [DllImport(ExternDll.User32,EntryPoint="RegisterPowerSettingNotification")]
        unsafe internal static extern IntPtr RegisterPowerSettingNotification(IntPtr hRecipient, Guid *pGuid, int Flags);

        [DllImport(ExternDll.User32,EntryPoint="UnregisterPowerSettingNotification")]
        unsafe internal static extern IntPtr UnregisterPowerSettingNotification(IntPtr hPowerNotify);

/*
        //
        // SendMessage taking a SafeHandle for wParam. Needed by some Win32 messages. e.g. WM_PRINT
        //
        [DllImport(ExternDll.User32, CharSet = CharSet.Auto)]
        internal static extern IntPtr SendMessage(HandleRef hWnd, WindowMessage msg, SafeHandle wParam, IntPtr lParam);
*/

        // private  DllImport - that takes an IconHandle.
        [DllImport(ExternDll.User32, CharSet = CharSet.Auto, SetLastError = true)]
        internal static extern IntPtr SendMessage( HandleRef hWnd, WindowMessage msg, IntPtr wParam, NativeMethods.IconHandle iconHandle );
#endif

        [DllImport(ExternDll.Kernel32, ExactSpelling = true, CharSet = CharSet.Auto)]
        internal static extern void SetLastError(int dwErrorCode);

#if BASE_NATIVEMETHODS || CORE_NATIVEMETHODS || FRAMEWORK_NATIVEMETHODS
        /// <summary>
        /// Win32 GetLayeredWindowAttributes.
        /// </summary>
        /// <param name="hwnd"></param>
        /// <param name="pcrKey"></param>
        /// <param name="pbAlpha"></param>
        /// <param name="pdwFlags"></param>
        /// <returns></returns>
        [DllImport("user32.dll")]
        public static extern bool GetLayeredWindowAttributes(
                HandleRef hwnd, IntPtr pcrKey, IntPtr pbAlpha, IntPtr pdwFlags);
        internal sealed class SafeFileMappingHandle : SafeHandleZeroOrMinusOneIsInvalid
        {
            internal SafeFileMappingHandle(IntPtr handle) : base(false)
            {
                SetHandle(handle);
            }

            internal SafeFileMappingHandle() : base(true)
            {
            }

            public override bool IsInvalid
            {
                get
                {
                    return handle == IntPtr.Zero;
                }
            }

            protected override bool ReleaseHandle()
            {
                return CloseHandleNoThrow(new HandleRef(null, handle));
            }
        }
        internal sealed class SafeViewOfFileHandle : SafeHandleZeroOrMinusOneIsInvalid
        {
            internal SafeViewOfFileHandle() : base(true) { }

            internal unsafe void* Memory
            {
                get
                {
                    Debug.Assert(handle != IntPtr.Zero);
                    return (void*)handle;
                }
            }

            override protected bool ReleaseHandle()
            {
                return UnsafeNativeMethods.UnmapViewOfFileNoThrow(new HandleRef(null, handle));
            }
        }

        [DllImport(ExternDll.Kernel32, SetLastError = true, CharSet = CharSet.Auto, BestFitMapping = false, ThrowOnUnmappableChar = true)]
        internal unsafe static extern SafeFileMappingHandle CreateFileMapping(SafeFileHandle hFile, NativeMethods.SECURITY_ATTRIBUTES lpFileMappingAttributes, int flProtect, uint dwMaximumSizeHigh, uint dwMaximumSizeLow, string lpName);

        [DllImport(ExternDll.Kernel32, SetLastError = true)]
        internal static extern SafeViewOfFileHandle MapViewOfFileEx(SafeFileMappingHandle hFileMappingObject, int dwDesiredAccess, int dwFileOffsetHigh, int dwFileOffsetLow, IntPtr dwNumberOfBytesToMap, IntPtr lpBaseAddress);
#endif // BASE_NATIVEMETHODS


        internal static IntPtr SetWindowLong(HandleRef hWnd, int nIndex, IntPtr dwNewLong)
        {
            IntPtr result = IntPtr.Zero;

            if (IntPtr.Size == 4)
            {
                // use SetWindowLong
                Int32 tempResult = NativeMethodsSetLastError.SetWindowLong(hWnd, nIndex, NativeMethods.IntPtrToInt32(dwNewLong));
                result = new IntPtr(tempResult);
            }
            else
            {
                // use SetWindowLongPtr
                result = NativeMethodsSetLastError.SetWindowLongPtr(hWnd, nIndex, dwNewLong);
            }

            return result;
        }

        internal static IntPtr CriticalSetWindowLong(HandleRef hWnd, int nIndex, IntPtr dwNewLong)
        {
            IntPtr result = IntPtr.Zero;

            if (IntPtr.Size == 4)
            {
                // use SetWindowLong
                Int32 tempResult = NativeMethodsSetLastError.SetWindowLong(hWnd, nIndex, NativeMethods.IntPtrToInt32(dwNewLong));
                result = new IntPtr(tempResult);
            }
            else
            {
                // use SetWindowLongPtr
                result = NativeMethodsSetLastError.SetWindowLongPtr(hWnd, nIndex, dwNewLong);
            }

            return result;
        }

        internal static IntPtr CriticalSetWindowLong(HandleRef hWnd, int nIndex, NativeMethods.WndProc dwNewLong)
        {
            int errorCode;
            IntPtr retVal;

            if (IntPtr.Size == 4)
            {
                Int32 tempRetVal = NativeMethodsSetLastError.SetWindowLongWndProc(hWnd, nIndex, dwNewLong);
                errorCode = Marshal.GetLastWin32Error();
                retVal = new IntPtr(tempRetVal);
            }
            else
            {
                retVal = NativeMethodsSetLastError.SetWindowLongPtrWndProc(hWnd, nIndex, dwNewLong);
                errorCode = Marshal.GetLastWin32Error();
            }

            if (retVal == IntPtr.Zero)
            {
                if (errorCode != 0)
                {
                    throw new System.ComponentModel.Win32Exception(errorCode);
                }
            }

            return retVal;
        }

        internal static IntPtr GetWindowLongPtr(HandleRef hWnd, int nIndex )
        {
            IntPtr result = IntPtr.Zero;
            int error = 0;

            if (IntPtr.Size == 4)
            {
                // use getWindowLong
                Int32 tempResult = NativeMethodsSetLastError.GetWindowLong(hWnd, nIndex);
                error = Marshal.GetLastWin32Error();
                result = new IntPtr(tempResult);
            }
            else
            {
                // use GetWindowLongPtr
                result = NativeMethodsSetLastError.GetWindowLongPtr(hWnd, nIndex);
                error = Marshal.GetLastWin32Error();
            }

            if ((result == IntPtr.Zero) && (error != 0))
            {
                // To be consistent with out other PInvoke wrappers
                // we should "throw" here.  But we don't want to
                // introduce new "throws" w/o time to follow up on any
                // new problems that causes.
                Debug.WriteLine("GetWindowLongPtr failed.  Error = " + error);
                // throw new System.ComponentModel.Win32Exception(error);
            }

            return result;
        }

        internal static Int32 GetWindowLong(HandleRef hWnd, int nIndex )
        {
            int iResult = 0;
            IntPtr result = IntPtr.Zero;
            int error = 0;

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

        internal static NativeMethods.WndProc GetWindowLongWndProc(HandleRef hWnd)
        {
            NativeMethods.WndProc returnValue = null;
            int error = 0;

            if (IntPtr.Size == 4)
            {
                // use getWindowLong
                returnValue = NativeMethodsSetLastError.GetWindowLongWndProc(hWnd, NativeMethods.GWL_WNDPROC);
                error = Marshal.GetLastWin32Error();
            }
            else
            {
                // use GetWindowLongPtr
                returnValue = NativeMethodsSetLastError.GetWindowLongPtrWndProc(hWnd, NativeMethods.GWL_WNDPROC);
                error = Marshal.GetLastWin32Error();
            }

            if (null == returnValue)
            {
                throw new Win32Exception(error);
            }

            return returnValue;
        }

        [DllImport("winmm.dll", CharSet = CharSet.Unicode)]
        internal static extern bool PlaySound([In]string soundName, IntPtr hmod, SafeNativeMethods.PlaySoundFlags soundFlags);

        internal const uint
            INTERNET_COOKIE_THIRD_PARTY     = 0x10,
            INTERNET_COOKIE_EVALUATE_P3P    = 0x40,
            INTERNET_COOKIE_IS_RESTRICTED   = 0x200,
            COOKIE_STATE_REJECT             = 5;

        //!!! CAUTION
        // PresentationHost intercepts calls to InternetGetCookieEx & InternetSetCookieEx and delegates them
        // to the browser. It doesn't do this for InternetGetCookie & InternetSetCookie.
        // See also Application.Get/SetCookie().
        //!!!

        [DllImport(ExternDll.Wininet, SetLastError=true, ExactSpelling=true, EntryPoint="InternetGetCookieExW", CharSet=CharSet.Unicode)]
        internal static extern bool InternetGetCookieEx([In]string Url, [In]string cookieName,
            [Out] StringBuilder cookieData, [In, Out] ref UInt32 pchCookieData, uint flags, IntPtr reserved);

        [DllImport(ExternDll.Wininet, SetLastError = true, ExactSpelling = true, EntryPoint = "InternetSetCookieExW", CharSet = CharSet.Unicode)]
        internal static extern uint InternetSetCookieEx([In]string Url, [In]string CookieName, [In]string cookieData, uint flags, [In] string p3pHeader);

#if DRT_NATIVEMETHODS

        [DllImport(ExternDll.User32, ExactSpelling = true, EntryPoint = "mouse_event", CharSet = CharSet.Auto)]
        internal static extern void Mouse_event(int flags, int dx, int dy, int dwData, IntPtr extrainfo);

#endif
        /////////////////////////////
        // needed by Framework

        [DllImport(ExternDll.Kernel32, ExactSpelling = true, CharSet = CharSet.Unicode)]
        internal static extern int GetLocaleInfoW(int locale, int type, string data, int dataSize);

        [DllImport(ExternDll.Kernel32, ExactSpelling = true, SetLastError = true)]
        internal static extern int FindNLSString(int locale, uint flags, [MarshalAs(UnmanagedType.LPWStr)]string sourceString, int sourceCount, [MarshalAs(UnmanagedType.LPWStr)]string findString, int findCount, out int found);


        //[DllImport(ExternDll.Psapi, SetLastError = true, CharSet = CharSet.Auto, BestFitMapping = false)]
        //public static extern int GetModuleFileNameEx(IntPtr hProcess, IntPtr hModule, StringBuilder buffer, int length);

        //
        // OpenProcess
        //
        public const int PROCESS_VM_READ = 0x0010;
        public const int PROCESS_QUERY_INFORMATION = 0x0400;

        //[DllImport(ExternDll.Kernel32, SetLastError = true, CharSet = CharSet.Auto)]
        //public static extern IntPtr OpenProcess(int dwDesiredAccess, bool fInherit, int dwProcessId);

        [DllImport(ExternDll.User32, EntryPoint = "SetWindowText", CharSet = CharSet.Auto, SetLastError = true, BestFitMapping = false)]
        private static extern bool IntSetWindowText(HandleRef hWnd, string text);

        internal static void SetWindowText(HandleRef hWnd, string text)
        {
            if (IntSetWindowText(hWnd, text) == false)
            {
                throw new Win32Exception();
            }
        }
        [DllImport(ExternDll.User32, EntryPoint = "GetIconInfo", CharSet = CharSet.Auto, SetLastError = true)]
        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]
        private static extern bool GetIconInfoImpl(HandleRef hIcon, [Out] ICONINFO_IMPL piconinfo);

        [StructLayout(LayoutKind.Sequential)]
        internal class ICONINFO_IMPL
        {
            public bool fIcon = false;
            public int xHotspot = 0;
            public int yHotspot = 0;
            public IntPtr hbmMask = IntPtr.Zero;
            public IntPtr hbmColor = IntPtr.Zero;
        }

        // FOR REVIEW
        // note that a different-signature version of this method is defined in SafeNativeMethodsCLR.cs, but
        // this appears to be an intentional override of the functionality.  Seems odd if the real method
        // is really safe to reimplement it in an unsafe manner.  Need to review this.
        internal static void GetIconInfo(HandleRef hIcon, out NativeMethods.ICONINFO piconinfo)
        {
            bool success = false;
            int error = 0;
            piconinfo = new NativeMethods.ICONINFO();
            ICONINFO_IMPL iconInfoImpl = new ICONINFO_IMPL();

            SRCS.RuntimeHelpers.PrepareConstrainedRegions(); // Mark the following as special
            try
            {
                // Intentionally empty
            }
            finally
            {
                // This block won't be interrupted by certain runtime induced failures or thread abort
                success = GetIconInfoImpl(hIcon, iconInfoImpl);
                error = Marshal.GetLastWin32Error();

                if (success)
                {
                    piconinfo.hbmMask = NativeMethods.BitmapHandle.CreateFromHandle(iconInfoImpl.hbmMask);
                    piconinfo.hbmColor = NativeMethods.BitmapHandle.CreateFromHandle(iconInfoImpl.hbmColor);
                    piconinfo.fIcon = iconInfoImpl.fIcon;
                    piconinfo.xHotspot = iconInfoImpl.xHotspot;
                    piconinfo.yHotspot = iconInfoImpl.yHotspot;
                }
            }

            if(!success)
            {
                Debug.WriteLine("GetIconInfo failed.  Error = " + error);

                throw new Win32Exception();
            }
        }

#if never

        [DllImport(ExternDll.User32,
#if WIN64
         EntryPoint="GetClassLongPtr",
#endif
 CharSet = CharSet.Auto, SetLastError = true)
        ]
        internal static extern UInt32 GetClassLong(IntPtr hwnd, int nIndex);
#endif

        [DllImport(ExternDll.User32, EntryPoint = "GetWindowPlacement", ExactSpelling = true, CharSet = CharSet.Auto, SetLastError = true)]
        private static extern bool IntGetWindowPlacement(HandleRef hWnd, ref NativeMethods.WINDOWPLACEMENT placement);

        // note:  this method exists in UnsafeNativeMethodsCLR.cs, but that method does not have the if/throw implemntation
        internal static void GetWindowPlacement(HandleRef hWnd, ref NativeMethods.WINDOWPLACEMENT placement)
        {
            if (IntGetWindowPlacement(hWnd, ref placement) == false)
            {
                throw new Win32Exception();
            }
        }


        [DllImport(ExternDll.User32, EntryPoint = "SetWindowPlacement", ExactSpelling = true, CharSet = CharSet.Auto, SetLastError = true)]
        private static extern bool IntSetWindowPlacement(HandleRef hWnd, [In] ref NativeMethods.WINDOWPLACEMENT placement);

        // note: this method appears in UnsafeNativeMethodsCLR.cs but does not have the if/throw block
        internal static void SetWindowPlacement(HandleRef hWnd, [In] ref NativeMethods.WINDOWPLACEMENT placement)
        {
            if (IntSetWindowPlacement(hWnd, ref placement) == false)
            {
                throw new Win32Exception();
            }
        }

        //[DllImport("secur32.dll", CharSet = CharSet.Unicode, SetLastError = true, ExactSpelling = true)]
        //internal static extern bool GetUserNameExW(
        //    [In] EXTENDED_NAME_FORMAT nameFormat,
        //    [MarshalAs(UnmanagedType.LPWStr)] StringBuilder lpNameBuffer,
        //    [In, Out] ref ulong nSize);


        [DllImport(ExternDll.User32, CharSet = CharSet.Auto, BestFitMapping = false)]
        internal static extern bool SystemParametersInfo(int nAction, int nParam, [In, Out] NativeMethods.ANIMATIONINFO anim, int nUpdate);

        [DllImport(ExternDll.User32, CharSet = CharSet.Auto, BestFitMapping = false, ThrowOnUnmappableChar = true)]
        internal static extern bool SystemParametersInfo(int nAction, int nParam, [In, Out] NativeMethods.ICONMETRICS metrics, int nUpdate);


        //---------------------------------------------------------------------------
        //  SetWindowThemeAttribute()
        //                      - set attributes to control how themes are applied to
        //                        windows.
        //
        //  hwnd                - the handle of the window (cannot be NULL)
        //
        //  eAttribute           - one of the following:
        //
        //              WTA_NONCLIENT:
        //                      pvAttribute must be a WINDOWTHEMEATTRIBUTE pointer with a valid WTNCA flag
        //                      the default is all flags set to 0
        //
        //  pvAttribute             - pointer to data relevant to property being set size
        //                          is cbAttribute see each property for details.
        //
        //  cbAttribute             - size in bytes of the data pointed to by pvAttribute
        //
        //---------------------------------------------------------------------------
#if WCP_SYSTEM_THEMES_ENABLED
        [DllImport(ExternDll.Uxtheme, CharSet = CharSet.Unicode)]
        public static extern uint SetWindowThemeAttribute(HandleRef hwnd, NativeMethods.WINDOWTHEMEATTRIBUTETYPE eAttribute, [In, MarshalAs(UnmanagedType.LPStruct)] NativeMethods.WTA_OPTIONS pvAttribute, int cbAttribute);
        public static uint SetWindowThemeAttribute(HandleRef hwnd, NativeMethods.WINDOWTHEMEATTRIBUTETYPE eAttribute, NativeMethods.WTA_OPTIONS pvAttribute)
        {
            return SetWindowThemeAttribute(hwnd, eAttribute, pvAttribute, Marshal.SizeOf(typeof(NativeMethods.WTA_OPTIONS)));
        }
#endif


        //---------------------------------------------------------------------------
        // BeginPanningFeedback - Visual feedback init function related to pan gesture
        //
        //  HWND hwnd - The handle to the Target window that will receive feedback
        //
        //---------------------------------------------------------------------------
        [DllImport(ExternDll.Uxtheme, CharSet = CharSet.Unicode)]
        public static extern bool BeginPanningFeedback(HandleRef hwnd);

        //---------------------------------------------------------------------------
        // UpdatePanningFeedback : Visual feedback function related to pan gesture
        // Can Be called only after a BeginPanningFeedback call
        //
        // HWND hwnd                 - The handle to the Target window that will receive feedback
        //                             For the method to succeed this must be the same hwnd as provided in
        //                             BeginPanningFeedback
        //
        // LONG lTotalOverpanOffsetX - The Total displacement that the window has moved in the horizontal direction
        //                             since the end of scrollable region was reached. The API would move the window by the distance specified
        //                             A maximum displacement of 30 pixels is allowed
        //
        // LONG lTotalOverpanOffsetY - The Total displacement that the window has moved in the horizontal direction
        //                             since the end of scrollable
        //                             region was reached. The API would move the window by the distance specified
        //                             A maximum displacement of 30 pixels is allowed
        //
        // BOOL fInInertia           - Flag dictating whether the Application is handling a WM_GESTURE message with the
        //                             GF_INERTIA FLAG set
        //
        //   Incremental calls to UpdatePanningFeedback should make sure they always pass
        //   the sum of the increments and not just the increment themselves
        //   Eg : If the initial displacement is 10 pixels and the next displacement 10 pixels
        //        the second call would be with the parameter as 20 pixels as opposed to 10
        //   Eg : UpdatePanningFeedback(hwnd, 10, 10, TRUE)
        //
        [DllImport(ExternDll.Uxtheme, CharSet = CharSet.Unicode)]
        public static extern bool UpdatePanningFeedback(
            HandleRef hwnd,
            int lTotalOverpanOffsetX,
            int lTotalOverpanOffsetY,
            bool fInInertia);

        //---------------------------------------------------------------------------
        //
        // EndPanningFeedback :Visual feedback reset function related to pan gesture
        //   Terminates any existing animation that was in process or set up by BeginPanningFeedback and UpdatePanningFeedback
        //   The EndPanningFeedBack needs to be called Prior to calling any BeginPanningFeedBack if we have already
        //   called a BeginPanningFeedBack followed by one/ more UpdatePanningFeedback calls
        //
        //  HWND hwnd         - The handle to the Target window that will receive feedback
        //
        //  BOOL fAnimateBack - Flag to indicate whether you wish the displaced window to move back
        //                      to the original position via animation or a direct jump.
        //                      Either way, the method will try to restore the moved window.
        //                      The latter case exists for compatibility with legacy apps.
        //
        [DllImport(ExternDll.Uxtheme, CharSet = CharSet.Unicode)]
        public static extern bool EndPanningFeedback(
            HandleRef hwnd,
            bool fAnimateBack);

        /// <summary>
        ///
        /// </summary>
        [DllImport(ExternDll.Kernel32, CharSet = CharSet.Auto, SetLastError = true)]
        public static extern bool SetEvent(IntPtr hEvent);

        [DllImport(ExternDll.Kernel32, SetLastError = true, ExactSpelling = true, CharSet = CharSet.Auto)]
        public static extern int SetEvent([In] SafeWaitHandle hHandle);

        [DllImport(ExternDll.Kernel32, SetLastError = true, ExactSpelling = true, CharSet = CharSet.Auto)]
        public static extern int WaitForSingleObject([In] SafeWaitHandle hHandle, [In] int dwMilliseconds);


        //[DllImport(ExternDll.Kernel32, SetLastError = true)]
        //internal static extern int GetFileSize(SafeFileHandle hFile, ref int lpFileSizeHigh);


        //////////////////////////////////////
        // Needed by BASE
#if BASE_NATIVEMETHODS


        [DllImport(ExternDll.User32, ExactSpelling = true, CharSet = CharSet.Auto, SetLastError = true)]
        internal static extern int GetMouseMovePointsEx(
                                        uint  cbSize,
                                        [In] ref NativeMethods.MOUSEMOVEPOINT pointsIn,
                                        [Out] NativeMethods.MOUSEMOVEPOINT[] pointsBufferOut,
                                        int nBufPoints,
                                        uint resolution
                                   );

        [StructLayout(LayoutKind.Explicit)]
        internal unsafe struct ULARGE_INTEGER
        {
            [FieldOffset(0)]
            internal uint LowPart;

            [FieldOffset(4)]
            internal uint HighPart;

            [FieldOffset(0)]
            internal ulong QuadPart;
        }

        [StructLayout(LayoutKind.Explicit)]
        internal unsafe struct LARGE_INTEGER
        {
            [FieldOffset(0)]
            internal int LowPart;

            [FieldOffset(4)]
            internal int HighPart;

            [FieldOffset(0)]
            internal long QuadPart;
        }

        [DllImport(ExternDll.Kernel32, SetLastError = true)]
        internal static extern bool GetFileSizeEx(
            SafeFileHandle hFile,
            ref LARGE_INTEGER lpFileSize
            );


        /// <summary>Win32 constants</summary>
        internal static readonly IntPtr INVALID_HANDLE_VALUE = new IntPtr(-1);

        /// <summary>Win32 constants</summary>
        internal const int PAGE_NOACCESS = 0x01;
        /// <summary>Win32 constants</summary>
        internal const int PAGE_READONLY = 0x02;
        /// <summary>Win32 constants</summary>
        internal const int PAGE_READWRITE = 0x04;
        /// <summary>Win32 constants</summary>
        internal const int PAGE_WRITECOPY = 0x08;
        /// <summary>Win32 constants</summary>
        internal const int PAGE_EXECUTE = 0x10;
        /// <summary>Win32 constants</summary>
        internal const int PAGE_EXECUTE_READ = 0x20;
        /// <summary>Win32 constants</summary>
        internal const int PAGE_EXECUTE_READWRITE = 0x40;
        /// <summary>Win32 constants</summary>
        internal const int PAGE_EXECUTE_WRITECOPY = 0x80;
        /// <summary>Win32 constants</summary>
        internal const int PAGE_GUARD = 0x100;
        /// <summary>Win32 constants</summary>
        internal const int PAGE_NOCACHE = 0x200;
        /// <summary>Win32 constants</summary>
        internal const int PAGE_WRITECOMBINE = 0x400;
        /// <summary>Win32 constants</summary>
        internal const int MEM_COMMIT = 0x1000;
        /// <summary>Win32 constants</summary>
        internal const int MEM_RESERVE = 0x2000;
        /// <summary>Win32 constants</summary>
        internal const int MEM_DECOMMIT = 0x4000;
        /// <summary>Win32 constants</summary>
        internal const int MEM_RELEASE = 0x8000;
        /// <summary>Win32 constants</summary>
        internal const int MEM_FREE = 0x10000;
        /// <summary>Win32 constants</summary>
        internal const int MEM_PRIVATE = 0x20000;
        /// <summary>Win32 constants</summary>
        internal const int MEM_MAPPED = 0x40000;
        /// <summary>Win32 constants</summary>
        internal const int MEM_RESET = 0x80000;
        /// <summary>Win32 constants</summary>
        internal const int MEM_TOP_DOWN = 0x100000;
        /// <summary>Win32 constants</summary>
        internal const int MEM_WRITE_WATCH = 0x200000;
        /// <summary>Win32 constants</summary>
        internal const int MEM_PHYSICAL = 0x400000;
        /// <summary>Win32 constants</summary>
        internal const int MEM_4MB_PAGES = unchecked((int)0x80000000);
        /// <summary>Win32 constants</summary>
        internal const int SEC_FILE = 0x800000;
        /// <summary>Win32 constants</summary>
        internal const int SEC_IMAGE = 0x1000000;
        /// <summary>Win32 constants</summary>
        internal const int SEC_RESERVE = 0x4000000;
        /// <summary>Win32 constants</summary>
        internal const int SEC_COMMIT = 0x8000000;
        /// <summary>Win32 constants</summary>
        internal const int SEC_NOCACHE = 0x10000000;
        /// <summary>Win32 constants</summary>
        internal const int MEM_IMAGE = SEC_IMAGE;
        /// <summary>Win32 constants</summary>
        internal const int WRITE_WATCH_FLAG_RESET = 0x01;

        /// <summary>Win32 constants</summary>
        internal const int SECTION_ALL_ACCESS =
            STANDARD_RIGHTS_REQUIRED |
            SECTION_QUERY |
            SECTION_MAP_WRITE |
            SECTION_MAP_READ |
            SECTION_MAP_EXECUTE |
            SECTION_EXTEND_SIZE;

        /// <summary>Win32 constants</summary>
        internal const int STANDARD_RIGHTS_REQUIRED = 0x000F0000;

        /// <summary>Win32 constants</summary>
        internal const int SECTION_QUERY = 0x0001;
        /// <summary>Win32 constants</summary>
        internal const int SECTION_MAP_WRITE = 0x0002;
        /// <summary>Win32 constants</summary>
        internal const int SECTION_MAP_READ = 0x0004;
        /// <summary>Win32 constants</summary>
        internal const int SECTION_MAP_EXECUTE = 0x0008;
        /// <summary>Win32 constants</summary>
        internal const int SECTION_EXTEND_SIZE = 0x0010;

        /// <summary>Win32 constants</summary>
        internal const int FILE_MAP_COPY = SECTION_QUERY;
        /// <summary>Win32 constants</summary>
        internal const int FILE_MAP_WRITE = SECTION_MAP_WRITE;
        /// <summary>Win32 constants</summary>
        internal const int FILE_MAP_READ = SECTION_MAP_READ;
        /// <summary>Win32 constants</summary>
        internal const int FILE_MAP_ALL_ACCESS = SECTION_ALL_ACCESS;

        /// <summary>
        ///
        /// </summary>
        /// <param name="stringSecurityDescriptor"></param>
        /// <param name="stringSDRevision"></param>
        /// <param name="securityDescriptor"></param>
        /// <param name="securityDescriptorSize"></param>
        /// <returns></returns>
        [DllImport(ExternDll.Advapi32, SetLastError = true, CharSet = CharSet.Auto, BestFitMapping = false, ThrowOnUnmappableChar = true)]
        internal static extern bool ConvertStringSecurityDescriptorToSecurityDescriptor(
            string stringSecurityDescriptor,    // security descriptor string
            int stringSDRevision,                  // revision level
            ref IntPtr securityDescriptor,       // SD
            IntPtr securityDescriptorSize       // SD size
            );

        /// <summary>Win32 constants</summary>
        internal const int SDDL_REVISION_1 = 1;
        /// <summary>Win32 constants</summary>
        internal const int SDDL_REVISION = SDDL_REVISION_1;


        [DllImport(ExternDll.Kernel32, SetLastError = true, CharSet = CharSet.Auto, BestFitMapping = false, ThrowOnUnmappableChar = true)]
        internal static extern SafeFileMappingHandle OpenFileMapping(
            int dwDesiredAccess,
            bool bInheritHandle,
            string lpName
            );

        [DllImport(ExternDll.Kernel32, SetLastError = true)]
        internal static extern IntPtr VirtualAlloc(
            IntPtr lpAddress,
            UIntPtr dwSize,
            int flAllocationType,
            int flProtect
            );


        //
        // RIT WM_MOUSEQUERY structure for DWM WM_MOUSEQUERY (see HwndMouseInputSource.cs)
        //
        [StructLayout(LayoutKind.Sequential, Pack = 1)] // For DWM WM_MOUSEQUERY
        internal unsafe struct MOUSEQUERY
        {
            internal UInt32 uMsg;
            internal IntPtr wParam;
            internal IntPtr lParam;
            internal Int32 ptX;
            internal Int32 ptY;
            internal IntPtr hwnd;
        }

        [DllImport(ExternDll.Ole32, ExactSpelling = true, CharSet = CharSet.Auto)]
        public static extern int OleIsCurrentClipboard(IComDataObject pDataObj);

        [DllImport(ExternDll.Kernel32, ExactSpelling = true, CharSet = CharSet.Auto)]
        internal static extern int GetOEMCP();

#if never
        [DllImport("user32.dll", CharSet = CharSet.Auto, BestFitMapping = false, ThrowOnUnmappableChar = true)]
        internal static extern int ToUnicode(int nVirtKey, int nScanCode, byte[] keystate, StringBuilder text, int cch, int flags);
#endif

        // WinEvent fired when new Avalon UI is created
        public const int EventObjectUIFragmentCreate = 0x6FFFFFFF;


        //////////////////////////////////
        // Needed by FontCache

        [DllImport("ntdll.dll")]
        internal static extern int RtlNtStatusToDosError(int Status);

        internal static bool NtSuccess(int err)
        {
            return err >= STATUS_SUCCESS;
        }

        internal static void NtCheck(int err)
        {
            if (!NtSuccess(err))
            {
                int win32error = RtlNtStatusToDosError(err);
                throw new System.ComponentModel.Win32Exception(win32error);
            }
        }

        internal const int STATUS_SUCCESS = 0;
        internal const int STATUS_TIMEOUT = 0x00000102;
        internal const int STATUS_BUFFER_TOO_SMALL = unchecked((int)0xC0000023);

#endif // BASE_NATIVEMETHODS

        //
        // COM Helper Methods
        //

        internal static int SafeReleaseComObject(object o)
        {
            int refCount = 0;

            // Validate
            if (o != null)
            {
                if (Marshal.IsComObject(o))
                {
                    refCount = Marshal.ReleaseComObject(o);
                }
            }

            return refCount;
        }

#if WINDOWS_BASE
        [DllImport(DllImport.Wininet, EntryPoint = "GetUrlCacheConfigInfoW", SetLastError=true)]
        internal static extern bool GetUrlCacheConfigInfo(
            ref NativeMethods.InternetCacheConfigInfo pInternetCacheConfigInfo,
            ref UInt32 cbCacheConfigInfo,
            UInt32 /* DWORD */ fieldControl
            );
#endif

        [DllImport("WtsApi32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool WTSRegisterSessionNotification(IntPtr hwnd, uint dwFlags);

        [DllImport("WtsApi32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool WTSUnRegisterSessionNotification(IntPtr hwnd);

        [DllImport(ExternDll.Kernel32, SetLastError = true)]
        public static extern IntPtr GetCurrentProcess();

        public const int DUPLICATE_CLOSE_SOURCE = 1;
        public const int DUPLICATE_SAME_ACCESS = 2;

        [DllImport(ExternDll.Kernel32, SetLastError = true)]
        public static extern bool DuplicateHandle(
            IntPtr hSourceProcess,
            SafeWaitHandle hSourceHandle,
            IntPtr hTargetProcessHandle,
            out IntPtr hTargetHandle,
            uint dwDesiredAccess,
            bool fInheritHandle,
            uint dwOptions
            );

        //
        // <Windows Color System (WCS) types>
        //

        [StructLayout(LayoutKind.Sequential)]
        public unsafe struct PROFILEHEADER
        {
            public uint phSize;                  // profile size in bytes
            public uint phCMMType;               // CMM for this profile
            public uint phVersion;               // profile format version number
            public uint phClass;                 // type of profile
            public NativeMethods.ColorSpace phDataColorSpace;  // color space of data
            public uint phConnectionSpace;       // PCS
            public uint phDateTime_0;            // date profile was created
            public uint phDateTime_1;            // date profile was created
            public uint phDateTime_2;            // date profile was created
            public uint phSignature;             // magic number ("Reserved for internal use.")
            public uint phPlatform;              // primary platform
            public uint phProfileFlags;          // various bit settings
            public uint phManufacturer;          // device manufacturer
            public uint phModel;                 // device model number
            public uint phAttributes_0;          // device attributes
            public uint phAttributes_1;          // device attributes
            public uint phRenderingIntent;       // rendering intent
            public uint phIlluminant_0;          // profile illuminant
            public uint phIlluminant_1;          // profile illuminant
            public uint phIlluminant_2;          // profile illuminant
            public uint phCreator;               // profile creator
            public fixed byte phReserved[44];
        };

        [StructLayout(LayoutKind.Sequential)]
        public unsafe struct PROFILE
        {
            public NativeMethods.ProfileType dwType; // profile type

            public void*       pProfileData;         // either the filename of the profile or buffer containing profile depending upon dwtype
            public uint        cbDataSize;           // size in bytes of pProfileData
        };

        /// <summary>The IsIconic function determines whether the specified window is minimized (iconic).</summary>
        [DllImport(ExternDll.User32)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool IsIconic(IntPtr hWnd);

        public enum HookType : int
        {
            WH_JOURNALRECORD = 0,
            WH_JOURNALPLAYBACK = 1,
            WH_KEYBOARD = 2,
            WH_GETMESSAGE = 3,
            WH_CALLWNDPROC = 4,
            WH_CBT = 5,
            WH_SYSMSGFILTER = 6,
            WH_MOUSE = 7,
            WH_HARDWARE = 8,
            WH_DEBUG = 9,
            WH_SHELL = 10,
            WH_FOREGROUNDIDLE = 11,
            WH_CALLWNDPROCRET = 12,
            WH_KEYBOARD_LL = 13,
            WH_MOUSE_LL = 14,
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct MOUSEHOOKSTRUCT
        {
            public NativeMethods.POINT pt;
            public IntPtr hwnd;
            public uint   wHitTestCode;
            public IntPtr dwExtraInfo;
        }

        public delegate IntPtr HookProc(int code, IntPtr wParam, IntPtr lParam);

        public static HandleRef SetWindowsHookEx(HookType idHook, HookProc lpfn, IntPtr hMod, int dwThreadId)
        {
            IntPtr result = IntSetWindowsHookEx(idHook, lpfn, hMod, dwThreadId);
            if (result == IntPtr.Zero)
            {
                throw new Win32Exception();
            }

            return new HandleRef(lpfn, result);
        }

        [DllImport(ExternDll.User32, EntryPoint = "SetWindowsHookExW", SetLastError = true)]
        private static extern IntPtr IntSetWindowsHookEx(HookType idHook, HookProc lpfn, IntPtr hMod, int dwThreadId);

        [DllImport(ExternDll.User32, SetLastError = true)]
        public static extern bool UnhookWindowsHookEx(HandleRef hhk);

        [DllImport(ExternDll.User32, SetLastError = true)]
        public static extern IntPtr CallNextHookEx(HandleRef hhk, int nCode, IntPtr wParam, IntPtr lParam);
    }
}
