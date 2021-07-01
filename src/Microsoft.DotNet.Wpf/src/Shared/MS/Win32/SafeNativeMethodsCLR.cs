// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace MS.Win32
{
    using MS.Utility;
    using System.Runtime.InteropServices;
    using System.Runtime.InteropServices.ComTypes;
    using System;
    using System.Security;
    using System.Collections;
    using System.IO;
    using System.Text;
    using System.ComponentModel;


    // The SecurityHelper class differs between assemblies and could not actually be
    //  shared, so it is duplicated across namespaces to prevent name collision.
#if WINDOWS_BASE
    using MS.Internal.WindowsBase;
#elif PRESENTATION_CORE
    using MS.Internal.PresentationCore;
#elif PRESENTATIONFRAMEWORK
    using MS.Internal.PresentationFramework;
#elif DRT
    using MS.Internal.Drt;
#elif UIAUTOMATIONTYPES
    using MS.Internal.UIAutomationTypes;
#else
#error Attempt to use a class (duplicated across multiple namespaces) from an unknown assembly.
#endif

    using IComDataObject = System.Runtime.InteropServices.ComTypes.IDataObject;

    internal static partial class SafeNativeMethods
    {
        public static int GetMessagePos()
        {
            return SafeNativeMethodsPrivate.GetMessagePos();
        }

        public static  IntPtr GetKeyboardLayout(int dwLayout)
        {
            return SafeNativeMethodsPrivate.GetKeyboardLayout(dwLayout);
        }

        public static  IntPtr ActivateKeyboardLayout(HandleRef hkl, int uFlags)
        {
            return SafeNativeMethodsPrivate.ActivateKeyboardLayout(hkl, uFlags);
        }

#if BASE_NATIVEMETHODS
        public static int GetKeyboardLayoutList(int size, [Out, MarshalAs(UnmanagedType.LPArray)] IntPtr[] hkls)
        {
            int result = NativeMethodsSetLastError.GetKeyboardLayoutList(size, hkls);
            if(result == 0)
            {
                int win32Err = Marshal.GetLastWin32Error();
                if (win32Err != 0)
                {
                    throw new Win32Exception(win32Err);
                }
            }

            return result;
        }
#endif


        internal static void GetMonitorInfo(HandleRef hmonitor, [In, Out]NativeMethods.MONITORINFOEX info)
        {
            if (SafeNativeMethodsPrivate.IntGetMonitorInfo(hmonitor, info) == false)
            {
                throw new Win32Exception();
            }
        }


        public static  IntPtr MonitorFromPoint(NativeMethods.POINT pt, int flags)
        {
            return SafeNativeMethodsPrivate.MonitorFromPoint(pt,flags);
        }


        public static  IntPtr MonitorFromRect(ref NativeMethods.RECT rect, int flags)
        {
            return  SafeNativeMethodsPrivate.MonitorFromRect(ref rect,flags);
        }


        public static  IntPtr MonitorFromWindow(HandleRef handle, int flags)
       {
        return SafeNativeMethodsPrivate.MonitorFromWindow(handle, flags);
        }

#if BASE_NATIVEMETHODS

        public static NativeMethods.CursorHandle LoadCursor(HandleRef hInst, IntPtr iconId)
        {
            NativeMethods.CursorHandle cursorHandle = SafeNativeMethodsPrivate.LoadCursor(hInst, iconId);
            if(cursorHandle == null || cursorHandle.IsInvalid)
            {
                throw new Win32Exception();
            }

            return cursorHandle;
        }

#endif

        public static IntPtr GetCursor()
        {
            return SafeNativeMethodsPrivate.GetCursor();
        }

        public static int ShowCursor(bool show)
        {
            return SafeNativeMethodsPrivate.ShowCursor(show);
        }

        internal static bool AdjustWindowRectEx(ref NativeMethods.RECT lpRect, int dwStyle, bool bMenu, int dwExStyle)
        {
            bool returnValue = SafeNativeMethodsPrivate.IntAdjustWindowRectEx(ref lpRect, dwStyle, bMenu, dwExStyle);
            if (returnValue == false)
            {
                throw new Win32Exception();
            }
            return returnValue;
        }


        internal static void GetClientRect(HandleRef hWnd, [In, Out] ref NativeMethods.RECT rect)
        {
            if(!SafeNativeMethodsPrivate.IntGetClientRect(hWnd, ref rect))
            {
                throw new Win32Exception();
            }
        }

        /// <summary>
        /// Alternative version of GetClientRect
        /// </summary>
        internal static NativeMethods.RECT GetClientRect(HandleRef hWnd)
        {
            var clientRect = default(NativeMethods.RECT);
            SafeNativeMethods.GetClientRect(hWnd, ref clientRect);
            return clientRect;
        }

        internal static void GetWindowRect(HandleRef hWnd, [In, Out] ref NativeMethods.RECT rect)
        {
            if(!SafeNativeMethodsPrivate.IntGetWindowRect(hWnd, ref rect))
            {
                throw new Win32Exception();
            }
        }

        public static int GetDoubleClickTime()
        {
            return SafeNativeMethodsPrivate.GetDoubleClickTime();
        }

        public static bool IsWindowEnabled(HandleRef hWnd)
        {
            return SafeNativeMethodsPrivate.IsWindowEnabled(hWnd);
        }

        public static bool IsWindowVisible(HandleRef hWnd)
        {
            return SafeNativeMethodsPrivate.IsWindowVisible(hWnd);
        }

        internal static bool ReleaseCapture()
        {
            bool returnValue = SafeNativeMethodsPrivate.IntReleaseCapture();

            if (returnValue == false)
            {
                throw new Win32Exception();
            }
            return returnValue;
        }


#if BASE_NATIVEMETHODS
        public static bool TrackMouseEvent(NativeMethods.TRACKMOUSEEVENT tme)
        {
            bool retVal = SafeNativeMethodsPrivate.TrackMouseEvent(tme);
            int win32Err = Marshal.GetLastWin32Error(); // Dance around FxCop
            if(!retVal && win32Err != 0)
            {
                throw new System.ComponentModel.Win32Exception(win32Err);
            }
            return retVal;
        }


        // Note: this overload has no return value.  If we need an overload that
        // returns the timer ID, then we'll need to add one.
        public static void SetTimer(HandleRef hWnd, int nIDEvent, int uElapse)
        {
            if(SafeNativeMethodsPrivate.SetTimer(hWnd, nIDEvent, uElapse, null) == IntPtr.Zero)
            {
                throw new Win32Exception();
            }
        }

        // Note: this returns true or false for success.  We still don't have an overload
        // that returns the timer ID.
        public static bool TrySetTimer(HandleRef hWnd, int nIDEvent, int uElapse)
        {
            if(SafeNativeMethodsPrivate.TrySetTimer(hWnd, nIDEvent, uElapse, null) == IntPtr.Zero)
            {
                return false;
            }

            return true;
        }
#endif

        public static bool KillTimer(HandleRef hwnd, int idEvent)
        {
            return (SafeNativeMethodsPrivate.KillTimer(hwnd,idEvent));
        }


#if FRAMEWORK_NATIVEMETHODS || CORE_NATIVEMETHODS || BASE_NATIVEMETHODS
        public static int GetTickCount()
        {
            return SafeNativeMethodsPrivate.GetTickCount();
        }
#endif

#if BASE_NATIVEMETHODS
        public static int MessageBeep(int uType)
        {
            return SafeNativeMethodsPrivate.MessageBeep(uType);
        }
#endif

        public static bool IsWindowUnicode(HandleRef hWnd)
        {
        return (SafeNativeMethodsPrivate.IsWindowUnicode(hWnd));
        }


#if BASE_NATIVEMETHODS
        public static IntPtr SetCursor(HandleRef hcursor)
        {
            return SafeNativeMethodsPrivate.SetCursor(hcursor);
        }

        public static IntPtr SetCursor(SafeHandle hcursor)
        {
            return SafeNativeMethodsPrivate.SetCursor(hcursor);
        }
#endif

        // not used by compiler - don't include.

        public static void ScreenToClient(HandleRef hWnd, ref NativeMethods.POINT pt)
        {
            if(SafeNativeMethodsPrivate.IntScreenToClient(hWnd, ref pt) == 0)
            {
                throw new Win32Exception();
            }
        }

        public static int GetCurrentProcessId()
        {
            return SafeNativeMethodsPrivate.GetCurrentProcessId();
        }


        public static int GetCurrentThreadId()
        {
            return SafeNativeMethodsPrivate.GetCurrentThreadId();
        }

        /// <summary>
        /// Returns the ID of the session under which the current process is running
        /// </summary>
        /// <returns>
        /// The session id upon success, null on failure
        /// </returns>
        public static int? GetCurrentSessionId()
        {
            int? result = null;

            int sessionId;
            if (SafeNativeMethodsPrivate.ProcessIdToSessionId(
                GetCurrentProcessId(), out sessionId))
            {
                result = sessionId;
            }

            return result;
        }

        public static IntPtr GetCapture()
        {
            return SafeNativeMethodsPrivate.GetCapture();
}
#if BASE_NATIVEMETHODS
        public static IntPtr SetCapture(HandleRef hwnd)
        {
            return SafeNativeMethodsPrivate.SetCapture(hwnd);
        }

        internal static int MapVirtualKey(int nVirtKey, int nMapType)
        {
            return SafeNativeMethodsPrivate.MapVirtualKey(nVirtKey,nMapType);
        }
#endif

        /// <summary>
        /// Identifies whether the given workstation session ID has a WTSConnectState value
        /// of WTSActive, or not.
        /// </summary>
        /// <param name="SessionId">
        /// The ID of the workstation session to query. If this is null,
        /// then this will default to WTS_CURRENT_SESSION. Note that the ID of the 
        /// current session will not be queried explicitly. 
        /// </param>
        /// <param name="defaultResult">
        /// The default result to return if this method is unable to identify the connection 
        /// state of the given session ID.
        /// </param>
        /// <returns>
        /// True if the connection state for <paramref name="SessionId"/> is WTSActive; 
        /// false otherwise
        /// <paramref name="defaultResult"/> is returned if WTSQuerySessionInformation 
        /// fails.
        /// </returns>
        public static bool IsCurrentSessionConnectStateWTSActive(int? SessionId = null, bool defaultResult = true)
        {
            IntPtr buffer = IntPtr.Zero;
            int bytesReturned;

            int sessionId = SessionId.HasValue ? SessionId.Value : NativeMethods.WTS_CURRENT_SESSION;
            bool currentSessionConnectState = defaultResult;

            try
            {
                if (SafeNativeMethodsPrivate.WTSQuerySessionInformation(
                    NativeMethods.WTS_CURRENT_SERVER_HANDLE, 
                    sessionId, 
                    NativeMethods.WTS_INFO_CLASS.WTSConnectState, 
                    out buffer, out bytesReturned) && (bytesReturned >= sizeof(int)))
                {
                    var data = Marshal.ReadInt32(buffer);
                    if (Enum.IsDefined(typeof(NativeMethods.WTS_CONNECTSTATE_CLASS), data))
                    {
                        var connectState = (NativeMethods.WTS_CONNECTSTATE_CLASS)data;
                        currentSessionConnectState = (connectState == NativeMethods.WTS_CONNECTSTATE_CLASS.WTSActive);
                    }
                }
            }
            finally
            {
                try
                {
                    if (buffer != IntPtr.Zero)
                    {
                        SafeNativeMethodsPrivate.WTSFreeMemory(buffer);
                    }
                }
                catch (Exception e) when (e is Win32Exception || e is SEHException)
                {
                    // We will do nothing and return defaultResult
                    //
                    // Note that we don't want to catch and ignore SystemException types
                    // like AV, OOM etc. 
                }
            }

            return currentSessionConnectState;
        }

        /// <summary>
        /// Retrieves the dots per inch (dpi) awareness of the specified process
        /// </summary>
        /// <param name="hProcess">[in]
        /// Handle of the process that is being queried. If this parameter
        /// is null, the current process is queried. 
        /// </param>
        /// <returns>The <see cref="NativeMethods.PROCESS_DPI_AWARENESS"/> of the specified process</returns>
        /// <exception cref="ArgumentException">The handle <paramref name="hProcess"/> is not valid</exception>
        /// <exception cref="UnauthorizedAccessException">The application does not have sufficient priviliges</exception>
        /// <exception cref="COMException">
        /// The call to Win32 GetProcessDpiAwareness function failed with some other error. 
        /// The error code in the exception object will contain the corresponding HRESULT
        /// </exception>
        /// <remarks>
        ///     - See remarks for <see cref="SafeNativeMethodsPrivate.GetProcessDpiAwareness(HandleRef, out IntPtr)"/>
        ///     - Minimum supported client: Windows 8.1
        /// </remarks>
        internal static NativeMethods.PROCESS_DPI_AWARENESS GetProcessDpiAwareness(HandleRef hProcess)
        {
            var ptrProcessDpiAwareness = IntPtr.Zero;
            var hr = (int)SafeNativeMethodsPrivate.GetProcessDpiAwareness(hProcess, out ptrProcessDpiAwareness);

            if(hr != NativeMethods.S_OK)
            {
                Marshal.ThrowExceptionForHR(hr);
            }

            return (NativeMethods.PROCESS_DPI_AWARENESS)NativeMethods.IntPtrToInt32(ptrProcessDpiAwareness);
        }

#if !DRT && !UIAUTOMATIONTYPES

        /// <summary>
        /// Returns the DPI_AWARENESS_CONTEXT associated with a window
        /// </summary>
        /// <param name="hwnd">The window to query</param>
        /// <returns>
        /// The DPI_AWARENESS_CONTEXT for the provided window. If the 
        /// window is not valid, the return value is NULL</returns>
        /// <remarks>
        /// The return value of GetWindowDpiAwarenessContext is not affected by the DPI_AWARENESS
        /// of the current thread. It only indicates the context of the window specified by the hwnd input parameter.
        /// 
        /// Minimum supported client: Windows 10, version 1607 (RS1)
        /// </remarks>
        internal static DpiAwarenessContextHandle GetWindowDpiAwarenessContext(IntPtr hwnd)
        {
            return SafeNativeMethodsPrivate.GetWindowDpiAwarenessContext(hwnd);
        }

#endif

        /// <summary>
        /// Determines whether two DPI_AWARENESS_CONTEXT values are identical
        /// </summary>
        /// <param name="dpiContextA">The first value to compare</param>
        /// <param name="dpiContextB">The second value to compare</param>
        /// <returns>Returns TRUE if the values are equal, otherwise FALSE</returns>
        /// <remarks>
        /// A DPI_AWARENESS_CONTEXT contains multiple pieces of information. 
        /// For example, it includes both the current and the inherited DPI_AWARENESS values. 
        /// AreDpiAwarenessContextsEqual ignores informational flags and determines if the 
        /// values are equal. You can't use a direct bitwise comparison because of these 
        /// informational flags.
        /// 
        /// Minimum supported client: Windows 10, version 1607 (RS1)
        /// 
        /// Note: Do NOT change this method signature to take DpiAwarenessContextHandle arguments.
        /// This method is used internally by DpiAwarenessContextHandle.
        /// </remarks>
        internal static bool AreDpiAwarenessContextsEqual(IntPtr dpiContextA, IntPtr dpiContextB)
        {
            return SafeNativeMethodsPrivate.AreDpiAwarenessContextsEqual(dpiContextA, dpiContextB);
        }

        /// <summary>
        /// Returns the dots per inch (dpi) value for the associated window.
        /// </summary>
        /// <param name="hwnd">The window you want to get information about.</param>
        /// <returns>The DPI for the window which depends on the <see cref="NativeMethods.DPI_AWARENESS"/> of the window. An invalid <paramref name="hwnd"/> value will result in a return value of 0.</returns>
        /// <remarks>
        /// The following table indicates the return value of GetDpiForWindow based on the <see cref="NativeMethods.DPI_AWARENESS"/> of the provided <paramref name="hwnd"/>.
        /// +---------------------------------+-----------------------------------------------------+
        /// |          DPI_AWARENESS          |                    Return value                     |
        /// +---------------------------------+-----------------------------------------------------+
        /// | DPI_AWARENESS_UNAWARE           | 96                                                  |
        /// | DPI_AWARENESS_SYSTEM_AWARE      | The system DPI.                                     |
        /// | DPI_AWARENESS_PER_MONITOR_AWARE | The DPI of the monitor where the window is located. |
        /// +---------------------------------+-----------------------------------------------------+
        /// 
        /// Minimum supported client: Windows 10, version 1607 (RS1)
        /// </remarks>
        internal static uint GetDpiForWindow(HandleRef hwnd)
        {
            return SafeNativeMethodsPrivate.GetDpiForWindow(hwnd);
        }


        /// <summary>
        /// Returns the system DPI.
        /// </summary>
        /// <returns>The system DPI value</returns>
        /// <remarks>
        /// The return value will be dependent based upon the calling context. If the current thread has a 
        /// DPI_AWARENESS (see <see cref="NativeMethods.DPI_AWARENESS"/>) value of DPI_AWARENESS_UNAWARE(<see cref="NativeMethods.DPI_AWARENESS.DPI_AWARENESS_UNAWARE"/>),
        /// the return value will be 96. that is because the current context always assumes a DPI of 96. For any other
        /// DPI_AWARENESS value, the return value will be the actual system DPI.
        /// 
        /// You should not cache the system DPI, but should use <see cref="GetDpiForSystem"/> whenever you need
        /// the system DPI value. This is so that Unaware and System Aware thread contexts get the correct system DPI's. 
        /// The System DPI for all other thread contexts (System Aware, and Per Monitor Aware) are fixed/constant at
        /// the time of process creation, and will not change. 
        /// 
        /// Minimum supported client: Windows 10, version 1607 (RS1)
        /// </remarks>
        internal static uint GetDpiForSystem()
        {
            return SafeNativeMethodsPrivate.GetDpiForSystem();
        }

        /// <summary>
        /// Returns the DPI_HOSTING_BEHAVIOR of the specified window.
        /// </summary>
        /// <param name="hWnd">The handle for the window to examine.</param>
        /// <returns>The DPI_HOSTING_BEHAVIOR of the specified window.</returns>
        /// <remarks>
        /// This API allows you to examine the hosting behavior of a window after it has been created.
        /// A window's hosting behavior is the hosting behavior of the thread in which the window was created,
        /// as set by a call to SetThreadDpiHostingBehavior. This is a permanent value and cannot be changed
        /// after the window is created, even if the thread's hosting behavior is changed.
        /// 
        /// Minimum supported client: Windows 10, version 1803 (RS4)
        /// </remarks>
        internal static NativeMethods.DPI_HOSTING_BEHAVIOR GetWindowDpiHostingBehavior(IntPtr hWnd)
        {
            return SafeNativeMethodsPrivate.GetWindowDpiHostingBehavior(hWnd);
        }

        /// <summary>
        /// Retrieves the DPI_HOSTING_BEHAVIOR from the current thread.
        /// </summary>
        /// <returns>The DPI_HOSTING_BEHAVIOR of the current thread.</returns>
        /// <remarks>
        /// This API returns the hosting behavior set by an earlier call of SetThreadDpiHostingBehavior,
        /// or DPI_HOSTING_BEHAVIOR_DEFAULT if no earlier call has been made.
        /// 
        /// Minimum supported client: Windows 10, version 1803 (RS4)
        /// </remarks>
        internal static NativeMethods.DPI_HOSTING_BEHAVIOR GetThreadDpiHostingBehavior()
        {
            return SafeNativeMethodsPrivate.GetThreadDpiHostingBehavior();
        }

        /// <summary>
        /// Calculates the required size of the window rectangle, based on the desired size of the
        /// client rectangle and the provided DPI. This window rectangle can then be passed to the CreateWindowEx
        /// function to create a window with a client area of the desired size.
        /// </summary>
        /// <param name="lpRect">
        /// A pointer to a RECT structure that contains the coordinates of the top-left and bottom-right
        /// corners of the desired client area. When the function returns, the structure contains the coordinates
        /// of the top-left and bottom-right corners of the window to accommodate the desired client area.
        /// </param>
        /// <param name="dwStyle">
        /// The Window Style of the window whose required size is to be calculated. Note that
        /// you cannot specify the WS_OVERLAPPED style.
        /// </param>
        /// <param name="bMenu">Indicates whether the window has a menu.</param>
        /// <param name="dwExStyle">The Extended Window Style of the window whose required size is to be calculated.</param>
        /// <param name="dpi">The DPI to use for scaling.</param>
        /// <returns>
        /// If the function succeeds, the return value is true.
        /// If the function fails, the return value is false.
        /// To get extended error information, call GetLastError
        /// </returns>
        /// <remarks>
        /// Minimum supported client: Windows 10, version 1607 (RS1)
        /// </remarks>
        internal static bool AdjustWindowRectExForDpi(
            ref NativeMethods.RECT lpRect,
            int dwStyle,
            bool bMenu,
            int dwExStyle,
            int dpi)
        {
            return 
                SafeNativeMethodsPrivate.AdjustWindowRectExForDpi(
                    ref lpRect, 
                    dwStyle, 
                    bMenu, 
                    dwExStyle, 
                    dpi);
        }

        /// <summary>
        /// Converts a point in a window from logical coordinates into physical coordinates, regardless of
        /// the dots per inch (dpi) awareness of the caller. For more information about DPI awareness
        /// levels, see PROCESS_DPI_AWARENESS.
        /// </summary>
        /// <param name="hWnd">A handle to the window whose transform is used for the conversion.</param>
        /// <param name="lpPoint">A pointer to a POINT structure that specifies the logical
        /// coordinates to be converted. The new physical coordinates are copied into this
        /// structure if the function succeeds.</param>
        /// <returns>
        /// Returns true if successful, or false otherwise.
        /// </returns>
        /// <remarks>
        /// Minimum supported client: Windows 8.1
        /// </remarks>
        internal static bool LogicalToPhysicalPointForPerMonitorDPI(
            HandleRef hWnd, 
            ref NativeMethods.POINT lpPoint)
        {
            return SafeNativeMethodsPrivate.LogicalToPhysicalPointForPerMonitorDPI(hWnd, ref lpPoint);
        }

        /// <summary>
        /// Converts a point in a window from logical coordinates into physical coordinates,
        /// regardless of the dots per inch (dpi) awareness of the caller. For more information about DPI
        /// awareness levels, see PROCESS_DPI_AWARENESS.
        /// </summary>
        /// <param name="hWnd">A handle to the window whose transform is used for the conversion.</param>
        /// <param name="lpPoint">A pointer to a POINT structure that specifies the physical/screen coordinates to be converted.
        /// The new logical coordinates are copied into this structure if the function succeeds.</param>
        /// <returns>
        /// Returns true if successful, or false otherwise.
        /// </returns>
        /// <remarks>
        /// Minimum supported client: Windows 8.1
        /// </remarks>
        internal static bool PhysicalToLogicalPointForPerMonitorDPI(
            HandleRef hWnd, 
            ref NativeMethods.POINT lpPoint)
        {
            return SafeNativeMethodsPrivate.PhysicalToLogicalPointForPerMonitorDPI(hWnd, ref lpPoint);
        }

        private partial class SafeNativeMethodsPrivate
        {
            [DllImport(ExternDll.Kernel32, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Auto)]
            public static extern int GetCurrentProcessId();

            [DllImport(ExternDll.Kernel32, ExactSpelling = true, CharSet = CharSet.Auto)]
            [return:MarshalAs(UnmanagedType.Bool)]
            public static extern bool ProcessIdToSessionId([In]int dwProcessId, [Out]out int pSessionId);

            [DllImport(ExternDll.Kernel32, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Auto)]
            public static extern int GetCurrentThreadId();

            [DllImport(ExternDll.User32, ExactSpelling = true, CharSet = CharSet.Auto)]
            public static extern IntPtr GetCapture();

            [DllImport(ExternDll.User32, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Auto)]
            public static extern bool IsWindowVisible(HandleRef hWnd);

            [DllImport(ExternDll.User32, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Auto)]
            public static extern int GetMessagePos();

            [DllImport(ExternDll.User32, EntryPoint = "ReleaseCapture", ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Auto, SetLastError = true)]
            public static extern bool IntReleaseCapture();

            [DllImport(ExternDll.User32, EntryPoint = "GetWindowRect", ExactSpelling = true, CharSet = CharSet.Auto, SetLastError = true)]
            public static extern bool IntGetWindowRect(HandleRef hWnd, [In, Out] ref NativeMethods.RECT rect);

            [DllImport(ExternDll.User32, EntryPoint = "GetClientRect", ExactSpelling = true, CharSet = CharSet.Auto, SetLastError = true)]
            public static extern bool IntGetClientRect(HandleRef hWnd, [In, Out] ref NativeMethods.RECT rect);

            [DllImport(ExternDll.User32, EntryPoint = "AdjustWindowRectEx", ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Auto, SetLastError = true)]
            public static extern bool IntAdjustWindowRectEx(ref NativeMethods.RECT lpRect, int dwStyle, bool bMenu, int dwExStyle);

            [DllImport(ExternDll.User32, ExactSpelling=true)]
            public static extern IntPtr MonitorFromRect(ref NativeMethods.RECT rect, int flags);

            [DllImport(ExternDll.User32, ExactSpelling = true)]
            public static extern IntPtr MonitorFromPoint(NativeMethods.POINT pt, int flags);

            [DllImport(ExternDll.User32, ExactSpelling=true, CharSet=CharSet.Auto)]
            public static extern IntPtr ActivateKeyboardLayout(HandleRef hkl, int uFlags);

            [DllImport(ExternDll.User32, ExactSpelling=true, CharSet=CharSet.Auto)]
            public static extern IntPtr GetKeyboardLayout(int dwLayout);

            [DllImport(ExternDll.User32, SetLastError = true, ExactSpelling=true, CharSet=System.Runtime.InteropServices.CharSet.Auto)]
            public static extern IntPtr SetTimer(HandleRef hWnd, int nIDEvent, int uElapse, NativeMethods.TimerProc lpTimerFunc);

            [DllImport(ExternDll.User32, EntryPoint="SetTimer", CharSet = System.Runtime.InteropServices.CharSet.Auto)]
            public static extern IntPtr TrySetTimer(HandleRef hWnd, int nIDEvent, int uElapse, NativeMethods.TimerProc lpTimerFunc);

            [DllImport(ExternDll.User32, ExactSpelling=true, CharSet=System.Runtime.InteropServices.CharSet.Auto)]
            public static extern bool KillTimer(HandleRef hwnd, int idEvent);

            [DllImport(ExternDll.User32, ExactSpelling=true, CharSet=System.Runtime.InteropServices.CharSet.Auto)]
            public static extern bool IsWindowUnicode(HandleRef hWnd);

            [DllImport(ExternDll.User32, ExactSpelling=true, CharSet=CharSet.Auto)]
            public static extern int GetDoubleClickTime();

            [DllImport(ExternDll.User32, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Auto)]
            public static extern bool IsWindowEnabled(HandleRef hWnd);

            [DllImport(ExternDll.User32, ExactSpelling=true, CharSet=System.Runtime.InteropServices.CharSet.Auto)]
            public static extern IntPtr GetCursor();

            [DllImport(ExternDll.User32, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Auto)]
            public static extern int ShowCursor(bool show);

            [DllImport(ExternDll.User32, EntryPoint = "GetMonitorInfo", CharSet = CharSet.Auto, SetLastError = true)]
            public static extern bool IntGetMonitorInfo(HandleRef hmonitor, [In, Out]NativeMethods.MONITORINFOEX info);

            [DllImport(ExternDll.User32, SetLastError = true, ExactSpelling = true)]
            public static extern IntPtr MonitorFromWindow(HandleRef handle, int flags);

#if BASE_NATIVEMETHODS
            [DllImport(ExternDll.User32, CharSet = CharSet.Auto)]
            internal static extern int MapVirtualKey(int nVirtKey, int nMapType);

            [DllImport(ExternDll.User32, ExactSpelling = true, CharSet = CharSet.Auto)]
            public static extern IntPtr SetCapture(HandleRef hwnd);

            [DllImport(ExternDll.User32, ExactSpelling = true, CharSet = CharSet.Auto)]
            public static extern IntPtr SetCursor(HandleRef hcursor);

            [DllImport(ExternDll.User32, ExactSpelling = true, CharSet = CharSet.Auto)]
            public static extern IntPtr SetCursor(SafeHandle hcursor);

            [DllImport(ExternDll.User32, ExactSpelling=true, SetLastError=true)]
            public static extern bool TrackMouseEvent(NativeMethods.TRACKMOUSEEVENT tme);

            [DllImport(ExternDll.User32, CharSet=System.Runtime.InteropServices.CharSet.Auto, SetLastError = true)]
            public static extern NativeMethods.CursorHandle LoadCursor(HandleRef hInst, IntPtr iconId);

#endif

#if BASE_NATIVEMETHODS || CORE_NATIVEMETHODS || FRAMEWORK_NATIVEMETHODS
            [DllImport(ExternDll.Kernel32, ExactSpelling=true, CharSet=System.Runtime.InteropServices.CharSet.Auto)]
            public static extern int GetTickCount();

#endif

            [DllImport(ExternDll.User32, EntryPoint="ScreenToClient", SetLastError=true, ExactSpelling=true, CharSet=CharSet.Auto)]
            public static extern int IntScreenToClient(HandleRef hWnd, ref NativeMethods.POINT pt);

#if BASE_NATIVEMETHODS
            [DllImport(ExternDll.User32)]
            public static extern int MessageBeep(int uType);
#endif
            [DllImport(ExternDll.WtsApi32, SetLastError = true, EntryPoint = "WTSQuerySessionInformation", CharSet = CharSet.Auto)]
            [return: MarshalAs(UnmanagedType.Bool)]
            public static extern bool WTSQuerySessionInformation(
                [In]IntPtr hServer, 
                [In] int SessionId, 
                [In]NativeMethods.WTS_INFO_CLASS WTSInfoClass, 
                [Out]out IntPtr ppBuffer, [Out]out int BytesReturned);

            [DllImport(ExternDll.WtsApi32, EntryPoint = "WTSFreeMemory", CharSet = CharSet.Auto)]
            public static extern bool WTSFreeMemory([In]IntPtr pMemory);

            /// <summary>
            /// Retrieves the dots per inch (dpi) awareness of the specified process
            /// </summary>
            /// <param name="hProcess"> [in]
            /// Handle of the process that is being queried. If this parameter
            /// is NULL, the current process is queried
            /// </param>
            /// <param name="awareness"> [out]
            /// The DPI awareness of the specified process. Possible values are 
            /// from the PROCESS_DPI_AWARENESS enumeration. See <see cref="PROCESS_DPI_AWARENESS"/>.
            /// </param>
            /// <returns>
            /// This function returns one of the following values.
            /// 
            /// |---------------------------------------------------|
            /// |   Return Code     |       Description             |
            /// |---------------------------------------------------|
            /// |   S_OK            |   The function successfully   |
            /// |                   |   retrieved the DPI awareness |
            /// |                   |   of the specified process    |
            /// |---------------------------------------------------|
            /// |   E_INVALIDARG    |   The handle or pointer passed|
            /// |                   |   in is not valid             |
            /// ----------------------------------------------------|
            /// |   E_ACCESSDENIED  |   The application does not    |
            /// |                   |   have sufficient priviliges.  |
            /// ----------------------------------------------------|
            /// 
            /// </returns>
            /// <remarks>
            /// - This function is identical to the following code:
            ///     <code>GetAwarenessFromDpiAwarenessContext(GetThreadDpiAwarenessContext());</code>
            /// - This function is supported on Windows 8.1 onwards. 
            /// </remarks>
            [DllImport(ExternDll.Shcore, CallingConvention = CallingConvention.Winapi)]
            internal static extern uint GetProcessDpiAwareness([In] HandleRef hProcess, out IntPtr awareness);

            /// <summary>
            /// Calculates the required size of the window rectangle, based on the desired size of the
            /// client rectangle and the provided DPI. This window rectangle can then be passed to the CreateWindowEx
            /// function to create a window with a client area of the desired size.
            /// </summary>
            /// <param name="lpRect">
            /// A pointer to a RECT structure that contains the coordinates of the top-left and bottom-right
            /// corners of the desired client area. When the function returns, the structure contains the coordinates
            /// of the top-left and bottom-right corners of the window to accommodate the desired client area.
            /// </param>
            /// <param name="dwStyle">
            /// The Window Style of the window whose required size is to be calculated. Note that
            /// you cannot specify the WS_OVERLAPPED style.
            /// </param>
            /// <param name="bMenu">Indicates whether the window has a menu.</param>
            /// <param name="dwExStyle">The Extended Window Style of the window whose required size is to be calculated.</param>
            /// <param name="dpi">The DPI to use for scaling.</param>
            /// <returns>
            /// If the function succeeds, the return value is true.
            /// If the function fails, the return value is false.
            /// To get extended error information, call GetLastError
            /// </returns>
            /// <remarks>
            /// Minimum supported client: Windows 10, version 1607 (RS1)
            /// </remarks>
            [DllImport(ExternDll.User32, CallingConvention = CallingConvention.Winapi, SetLastError = true)]
            [return: MarshalAs(UnmanagedType.Bool)]
            internal static extern bool AdjustWindowRectExForDpi(
                [In] [Out] ref NativeMethods.RECT lpRect,
                [In] int dwStyle,
                [In] [MarshalAs(UnmanagedType.Bool)] bool bMenu,
                [In] int dwExStyle,
                [In] int dpi);

            /// <summary>
            /// Converts a point in a window from logical coordinates into physical coordinates,
            /// regardless of the dots per inch (dpi) awareness of the caller. For more information about DPI
            /// awareness levels, see PROCESS_DPI_AWARENESS.
            /// </summary>
            /// <param name="hWnd">A handle to the window whose transform is used for the conversion.</param>
            /// <param name="lpPoint">A pointer to a POINT structure that specifies the physical/screen coordinates to be converted.
            /// The new logical coordinates are copied into this structure if the function succeeds.</param>
            /// <returns>
            /// Returns true if successful, or false otherwise.
            /// </returns>
            /// <remarks>
            /// Minimum supported client: Windows 8.1
            /// </remarks>
            [DllImport(ExternDll.User32, CallingConvention = CallingConvention.Winapi)]
            [return: MarshalAs(UnmanagedType.Bool)]
            internal static extern bool PhysicalToLogicalPointForPerMonitorDPI(
                [In] HandleRef hWnd,
                ref NativeMethods.POINT lpPoint);

            /// <summary>
            /// Converts a point in a window from logical coordinates into physical coordinates, regardless of
            /// the dots per inch (dpi) awareness of the caller. For more information about DPI awareness
            /// levels, see PROCESS_DPI_AWARENESS.
            /// </summary>
            /// <param name="hWnd">A handle to the window whose transform is used for the conversion.</param>
            /// <param name="lpPoint">A pointer to a POINT structure that specifies the logical
            /// coordinates to be converted. The new physical coordinates are copied into this
            /// structure if the function succeeds.</param>
            /// <returns>
            /// Returns true if successful, or false otherwise.
            /// </returns>
            /// <remarks>
            /// Minimum supported client: Windows 8.1
            /// </remarks>
            [DllImport(ExternDll.User32, CallingConvention = CallingConvention.Winapi)]
            [return: MarshalAs(UnmanagedType.Bool)]
            internal static extern bool LogicalToPhysicalPointForPerMonitorDPI(
                [In] HandleRef hWnd,
                ref NativeMethods.POINT lpPoint);

#if !DRT && !UIAUTOMATIONTYPES

            /// <summary>
            /// Returns the DPI_AWARENESS_CONTEXT associated with a window
            /// </summary>
            /// <param name="hwnd">The window to query</param>
            /// <returns>
            /// The DPI_AWARENESS_CONTEXT for the provided window. If the 
            /// window is not valid, the return value is NULL</returns>
            /// <remarks>
            /// The return value of GetWindowDpiAwarenessContext is not affected by the DPI_AWARENESS
            /// of the current thread. It only indicates the context of the window specified by the hwnd input parameter.
            /// 
            /// Minimum supported client: Windows 10, version 1607 (RS1)
            /// </remarks>
            [DllImport(ExternDll.User32, CallingConvention = CallingConvention.Winapi)]
            internal static extern DpiAwarenessContextHandle GetWindowDpiAwarenessContext([In] IntPtr hwnd);

#endif
            /// <summary>
            /// Determines whether two DPI_AWARENESS_CONTEXT values are identical
            /// </summary>
            /// <param name="dpiContextA">The first value to compare</param>
            /// <param name="dpiContextB">The second value to compare</param>
            /// <returns>Returns TRUE if the values are equal, otherwise FALSE</returns>
            /// <remarks>
            /// A DPI_AWARENESS_CONTEXT contains multiple pieces of information. 
            /// For example, it includes both the current and the inherited DPI_AWARENESS values. 
            /// AreDpiAwarenessContextsEqual ignores informational flags and determines if the 
            /// values are equal. You can't use a direct bitwise comparison because of these 
            /// informational flags.
            /// 
            /// Minimum supported client: Windows 10, version 1607 (RS1)
            /// 
            /// Note: Do NOT change this method signature to take DpiAwarenessContextHandle arguments.
            /// This method is used internally by DpiAwarenessContextHandle.
            /// </remarks>
            [DllImport(ExternDll.User32, CallingConvention = CallingConvention.Winapi, SetLastError = true)]
            [return: MarshalAs(UnmanagedType.Bool)]
            internal static extern bool AreDpiAwarenessContextsEqual([In] IntPtr dpiContextA, [In] IntPtr dpiContextB);

            /// <summary>
            /// Returns the dots per inch (dpi) value for the associated window.
            /// </summary>
            /// <param name="hwnd">The window you want to get information about.</param>
            /// <returns>The DPI for the window which depends on the <see cref="NativeMethods.DPI_AWARENESS"/> of the window. An invalid <paramref name="hwnd"/> value will result in a return value of 0.</returns>
            /// <remarks>
            /// The following table indicates the return value of GetDpiForWindow based on the <see cref="NativeMethods.DPI_AWARENESS"/> of the provided <paramref name="hwnd"/>.
            /// +---------------------------------+-----------------------------------------------------+
            /// |          DPI_AWARENESS          |                    Return value                     |
            /// +---------------------------------+-----------------------------------------------------+
            /// | DPI_AWARENESS_UNAWARE           | 96                                                  |
            /// | DPI_AWARENESS_SYSTEM_AWARE      | The system DPI.                                     |
            /// | DPI_AWARENESS_PER_MONITOR_AWARE | The DPI of the monitor where the window is located. |
            /// +---------------------------------+-----------------------------------------------------+
            /// 
            /// Minimum supported client: Windows 10, version 1607 (RS1)
            /// </remarks>
            [DllImport(ExternDll.User32, CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Winapi)]
            internal static extern uint GetDpiForWindow([In] HandleRef hwnd);

            /// <summary>
            /// Returns the system DPI.
            /// </summary>
            /// <returns>The system DPI value</returns>
            /// <remarks>
            /// The return value will be dependent based upon the calling context. If the current thread has a 
            /// DPI_AWARENESS (see <see cref="NativeMethods.DPI_AWARENESS"/>) value of DPI_AWARENESS_UNAWARE(<see cref="NativeMethods.DPI_AWARENESS.DPI_AWARENESS_UNAWARE"/>),
            /// the return value will be 96. that is because the current context always assumes a DPI of 96. For any other
            /// DPI_AWARENESS value, the return value will be the actual system DPI.
            /// 
            /// You should not cache the system DPI, but should use <see cref="GetDpiForSystem"/> whenever you need
            /// the system DPI value. This is so that Unaware and System Aware thread contexts get the correct system DPI's. 
            /// The System DPI for all other thread contexts (System Aware, and Per Monitor Aware) are fixed/constant at
            /// the time of process creation, and will not change. 
            /// 
            /// Minimum supported client: Windows 10, version 1607 (RS1)
            /// </remarks>
            [DllImport(ExternDll.User32, CallingConvention = CallingConvention.Winapi)]
            internal static extern uint GetDpiForSystem();

            /// <summary>
            /// Returns the DPI_HOSTING_BEHAVIOR of the specified window.
            /// </summary>
            /// <param name="hWnd">The handle for the window to examine.</param>
            /// <returns>The DPI_HOSTING_BEHAVIOR of the specified window.</returns>
            /// <remarks>
            /// This API allows you to examine the hosting behavior of a window after it has been created.
            /// A window's hosting behavior is the hosting behavior of the thread in which the window was created,
            /// as set by a call to SetThreadDpiHostingBehavior. This is a permanent value and cannot be changed
            /// after the window is created, even if the thread's hosting behavior is changed.
            /// 
            /// Minimum supported client: Windows 10, version 1803 (RS4)
            /// </remarks>
            [DllImport(ExternDll.User32, CallingConvention = CallingConvention.Winapi)]
            internal static extern NativeMethods.DPI_HOSTING_BEHAVIOR GetWindowDpiHostingBehavior(IntPtr hWnd);

            /// <summary>
            /// Retrieves the DPI_HOSTING_BEHAVIOR from the current thread.
            /// </summary>
            /// <returns>The DPI_HOSTING_BEHAVIOR of the current thread.</returns>
            /// <remarks>
            /// This API returns the hosting behavior set by an earlier call of SetThreadDpiHostingBehavior,
            /// or DPI_HOSTING_BEHAVIOR_DEFAULT if no earlier call has been made.
            /// 
            /// Minimum supported client: Windows 10, version 1803 (RS4)
            /// </remarks>
            [DllImport(ExternDll.User32, CallingConvention = CallingConvention.Winapi)]
            internal static extern NativeMethods.DPI_HOSTING_BEHAVIOR GetThreadDpiHostingBehavior();
        }
    }
}

