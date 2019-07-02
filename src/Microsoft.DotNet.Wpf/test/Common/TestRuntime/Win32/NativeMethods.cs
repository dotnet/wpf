// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        

using System.Runtime.InteropServices;
using System;
using System.Security.Permissions;
using System.Collections;
using System.IO;
using System.Text;
using System.Security;
using System.Threading;
using System.Windows;
using System.Windows.Interop;
using Microsoft.Test.Diagnostics;



namespace Microsoft.Test.Win32
{


    /// <summary>
    /// Definition for DLL names
    /// </summary>
    internal class ExternDll
    {
        /// <summary>
        /// 
        /// </summary>
        public const string Gdiplus = "gdiplus.dll";

        /// <summary>
        /// 
        /// </summary>
        public const string User32 = "user32.dll";

        /// <summary>
        /// 
        /// </summary>
        public const string Shfolder = "shfolder.dll";

        /// <summary>
        /// 
        /// </summary>
        public const string Imm32 = "imm32.dll";

        /// <summary>
        /// 
        /// </summary>
        public const string Advapi32 = "advapi32.dll";

        /// <summary>
        /// 
        /// </summary>
        public const string Shell32 = "shell32.dll";

        /// <summary>
        /// 
        /// </summary>
        public const string Kernel32 = "kernel32.dll";

        /// <summary>
        /// 
        /// </summary>
        public const string Comctl32 = "comctl32.dll";

        /// <summary>
        /// 
        /// </summary>
        public const string Oleaut32 = "oleaut32.dll";

        /// <summary>
        /// 
        /// </summary>
        public const string Olepro32 = "olepro32.dll";

        /// <summary>
        /// 
        /// </summary>
        public const string Ole32 = "ole32.dll";

        /// <summary>
        /// 
        /// </summary>
        public const string Gdi32 = "gdi32.dll";

        /// <summary>
        /// 
        /// </summary>
        public const string Comdlg32 = "comdlg32.dll";

        /// <summary>
        /// 
        /// </summary>
        public const string Uxtheme = "uxtheme.dll";

        /// <summary>
        /// 
        /// </summary>
        public const string Oleacc = "oleacc.dll";

        /// <summary>
        /// 
        /// </summary>
        public const string Hhctrl = "hhctrl.ocx";

        /// <summary>
        /// 
        /// </summary>
        public const string Winspool = "winspool.drv";

        /// <summary>
        /// 
        /// </summary>
        public const string Psapi = "psapi.dll";

        /// <summary>
        /// 
        /// </summary>
        public const string Ntdll = "ntdll.dll";

        /// <summary>
        /// 
        /// </summary>
        public const string Version = "version.dll";

        /// <summary>
        /// 
        /// </summary>
        public const string Vsassert = "vsassert.dll";

        /// <summary>
        /// 
        /// </summary>
        public const string Mscoree = "mscoree.dll";

        /// <summary>
        /// 
        /// </summary>
        public const string Msi = "msi.dll";

        /// <summary>
        /// 
        /// </summary>
        public const string Mqrt = "mqrt.dll";

        /// <summary>
        /// 
        /// </summary>
        public const string Activeds = "activeds.dll";

        /// <summary>
        /// 
        /// </summary>
        public const string Loadperf = "Loadperf.dll";

        /// <summary>
        /// 
        /// </summary>
        public const string DwmApi = "DwmApi.dll";

    }


    /// <summary>
    /// This is use to for place common handles
    /// </summary>
    public sealed class CommonHandles
    {
        /// <devdoc>
        ///     Handle type for accelerator tables.
        /// </devdoc>
        public static readonly int Accelerator = HandleCollector.RegisterType("Accelerator", 80, 50);

        /// <devdoc>
        ///     Handle type for files.
        /// </devdoc>
        public static readonly int Menu = HandleCollector.RegisterType("Menu", 30, 1000);

        /// <devdoc>
        ///     Handle type for files.
        /// </devdoc>
        public static readonly int Brush = HandleCollector.RegisterType("Brush", 30, 1000);

        /// <devdoc>
        ///     Handle type for HDC's that count against the Win98 limit of five DC's.  HDC's
        ///     which are not scarce, such as HDC's for bitmaps, are counted as GDIHANDLE's.
        /// </devdoc>
        public static readonly int HDC = HandleCollector.RegisterType("HDC", 100, 2);

    }


    /// <summary>
    /// Handle Collector class. I took this code from a internal avalon code
    /// </summary>
    internal sealed class HandleCollector
    {
        private static HandleType[] handleTypes = null;

        private static int handleTypeCount = 0;

        internal static event HandleChangeEventHandler HandleAdded;

        internal static event HandleChangeEventHandler HandleRemoved;

        /// <devdoc>
        ///     Adds the given handle to the handle collector.  This keeps the
        ///     handle on a "hot list" of objects that may need to be garbage
        ///     collected.
        /// </devdoc>
        internal static IntPtr Add(IntPtr handle, int type)
        {
            handleTypes[type - 1].Add(handle);
            return handle;
        }

        /// <devdoc>
        ///     Registers a new type of handle with the handle collector.
        /// </devdoc>
        internal static int RegisterType(string typeName, int expense, int initialThreshold)
        {
            lock (typeof(HandleCollector))
            {
                if (handleTypeCount == 0 || handleTypeCount == handleTypes.Length)
                {
                    HandleType[] newTypes = new HandleType[handleTypeCount + 10];

                    if (handleTypes != null)
                    {
                        Array.Copy(handleTypes, 0, newTypes, 0, handleTypeCount);
                    }

                    handleTypes = newTypes;
                }

                handleTypes[handleTypeCount++] = new HandleType(typeName, expense, initialThreshold);
                return handleTypeCount;
            }
        }

        /// <devdoc>
        ///     Removes the given handle from the handle collector.  Removing a
        ///     handle removes it from our "hot list" of objects that should be
        ///     frequently garbage collected.
        /// </devdoc>
        internal static IntPtr Remove(IntPtr handle, int type)
        {
            return handleTypes[type - 1].Remove(handle);
        }

        /// <devdoc>
        ///     Represents a specific type of handle.
        /// </devdoc>
        private class HandleType
        {
            internal readonly string name;

            private int initialThreshHold;

            private int threshHold;

            private int handleCount;

            private readonly int deltaPercent;

            /// <devdoc>
            ///     Creates a new handle type.
            /// </devdoc>
            internal HandleType(string name, int expense, int initialThreshHold)
            {
                this.name = name;
                this.initialThreshHold = initialThreshHold;
                this.threshHold = initialThreshHold;
                this.handleCount = 0;
                this.deltaPercent = 100 - expense;
            }

            /// <devdoc>
            ///     Adds a handle to this handle type for monitoring.
            /// </devdoc>
            internal void Add(IntPtr handle)
            {
                bool performCollect = false;

                lock (this)
                {
                    handleCount++;
                    performCollect = NeedCollection();
                    lock (typeof(HandleCollector))
                    {
                        if (HandleCollector.HandleAdded != null)
                        {
                            HandleCollector.HandleAdded(name, handle, GetHandleCount());
                        }
                    }

                    if (!performCollect)
                    {
                        return;
                    }
                }

                if (performCollect)
                {
#if DEBUG_HANDLECOLLECTOR
                    Debug.WriteLine("HC> Forcing garbage collect");
                    Debug.WriteLine("HC>     name        :" + name);
                    Debug.WriteLine("HC>     threshHold  :" + (threshHold).ToString());
                    Debug.WriteLine("HC>     handleCount :" + (handleCount).ToString());
                    Debug.WriteLine("HC>     deltaPercent:" + (deltaPercent).ToString());
#endif
                    GC.Collect();

                    // We just performed a GC.  If the main thread is in a tight
                    // loop there is a this will cause us to increase handles forever and prevent handle collector
                    // from doing its job.  Yield the thread here.  This won't totally cause
                    // a finalization pass but it will effectively elevate the priority
                    // of the finalizer thread just for an instant.  But how long should
                    // we sleep?  We base it on how expensive the handles are because the
                    // more expensive the handle, the more critical that it be reclaimed.
                    int sleep = (100 - deltaPercent) / 4;

                    System.Threading.Thread.Sleep(sleep);
                }
            }


            /// <devdoc>
            ///     Retrieves the outstanding handle count for this
            ///     handle type.
            /// </devdoc>
            internal int GetHandleCount()
            {
                lock (this)
                {
                    return handleCount;
                }
            }

            /// <devdoc>
            ///     Determines if this handle type needs a garbage collection pass.
            /// </devdoc>
            internal bool NeedCollection()
            {

                if (handleCount > threshHold)
                {
                    threshHold = handleCount + ((handleCount * deltaPercent) / 100);
#if DEBUG_HANDLECOLLECTOR
                    Debug.WriteLine("HC> NeedCollection: increase threshHold to " + threshHold);
#endif
                    return true;
                }

                // If handle count < threshHold, we don't
                // need to collect, but if it 10% below the next lowest threshhold we
                // will bump down a rung.  We need to choose a percentage here or else
                // we will oscillate.
                //
                int oldThreshHold = (100 * threshHold) / (100 + deltaPercent);

                if (oldThreshHold >= initialThreshHold && handleCount < (int)(oldThreshHold * .9F))
                {
#if DEBUG_HANDLECOLLECTOR
                    Debug.WriteLine("HC> NeedCollection: throttle threshhold " + threshHold + " down to " + oldThreshHold);
#endif
                    threshHold = oldThreshHold;
                }

                return false;
            }

            /// <devdoc>
            ///     Removes the given handle from our monitor list.
            /// </devdoc>
            internal IntPtr Remove(IntPtr handle)
            {
                lock (this)
                {
                    handleCount--;

                    handleCount = Math.Max(0, handleCount);

                    lock (typeof(HandleCollector))
                    {
                        if (HandleCollector.HandleRemoved != null)
                        {
                            HandleCollector.HandleRemoved(name, handle, GetHandleCount());
                        }
                    }

                    return handle;
                }
            }
        }
    }

    internal delegate void HandleChangeEventHandler(string handleType, IntPtr handleValue, int currentHandleCount);



    /// <summary>
    /// This class contains Win32 Methods. All the documentation for this methods can be found on MSDN
    /// The Constants or Structs used by these method can be found on Microsoft.Test.Win32.NaviteConstants or 
    /// Microsoft.Test.Win32.NaviteStructs
    /// </summary>
    [SuppressUnmanagedCodeSecurity()]
    public static class NativeMethods
    {

        /// <summary>
        /// 
        /// </summary>
        public static IntPtr InvalidIntPtr = ((IntPtr)((int)(-1)));

        /// <summary>
        /// 
        /// </summary>
        public static HandleRef NullHandleRef = new HandleRef(null, IntPtr.Zero);


        /// <summary>
        /// 
        /// </summary>
        public delegate bool MonitorEnumProc(IntPtr monitor, IntPtr hdc, IntPtr lprcMonitor, IntPtr lParam);


        [CLSCompliant(false)]
        public static byte HIBYTE(UInt16 w)
        {
            return (byte)((w >> 8) & 0xFF);
        }

        [CLSCompliant(false)]
        public static byte LOBYTE(UInt16 w)
        {
            return (byte)w;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="fBlockIt"></param>
        /// <returns></returns>
        [DllImport(ExternDll.User32, ExactSpelling = true, EntryPoint = "BlockInput", CharSet = CharSet.Auto)]
        internal static extern int IntBlockInput(int fBlockIt);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="fBlockIt"></param>
        /// <returns></returns>
        public static int BlockInput(int fBlockIt)
        {

            return IntBlockInput(fBlockIt);
        }

        [DllImport(ExternDll.DwmApi, EntryPoint = "DwmIsCompositionEnabled", PreserveSig = true, CallingConvention = CallingConvention.StdCall)]
        private static extern int PDwmIsCompositionEnabled(out bool isEnabled);

        [DllImport(ExternDll.DwmApi, EntryPoint = "DwmEnableComposition", PreserveSig = true, SetLastError = true, CallingConvention = CallingConvention.StdCall)]
        private static extern int PDwmEnableComposition(bool enableDwm);

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public static bool DwmIsCompositionEnabled()
        {
            bool retVal = false;
            int hresult = PDwmIsCompositionEnabled(out retVal);
            if (FAILED(hresult)) { throw Win32ExceptionWrapper.ExternalException("DwmIsCompositionEnabled", hresult); }
            return retVal;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="enableDwm"></param>
        public static int DwmEnableComposition(bool enableDwm)
        {
            if (System.Environment.OSVersion.Version.Major <= 5)  // Xp or below do not have DWM APIs
            {
                return -1;
            }

            if (DwmIsCompositionEnabled() == enableDwm) { return 0; }

            int hresult = PDwmEnableComposition(enableDwm);

            if (FAILED(hresult)) { throw Win32ExceptionWrapper.ExternalException("DwmEnableComposition", hresult); }
            return 0;
        }

        private static bool SUCCEEDED(int HRESULT)
        {
            return (HRESULT >= 0);
        }
        private static bool FAILED(int HRESULT)
        {
            return (HRESULT < 0);
        }

        /// <summary>
        /// CallWindowProc Win32 API
        /// </summary>
        /// <returns></returns>

        [DllImport(ExternDll.User32, EntryPoint = "CallWindowProc", CharSet = CharSet.Auto)]
        internal static extern IntPtr IntCallWindowProc(IntPtr wndProc, IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);

        /// <summary>
        /// CallWindowProc Win32 API
        /// </summary>
        /// <returns></returns>
        public static IntPtr CallWindowProc(IntPtr wndProc, IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam)
        {
            return IntCallWindowProc(wndProc, hWnd, msg, wParam, lParam);
        }


        /// <summary>Converts the client-area coordinates of a specified point to screen coordinates.</summary>
        /// <param name="hwndFrom">Handle to the window whose client area is used for the conversion.</param>
        /// <param name="pt">POINT structure that contains the client coordinates to be converted.</param>
        /// <returns>true if the function succeeds, false otherwise.</returns>
        [DllImport(ExternDll.User32, EntryPoint = "ClientToScreen", CharSet = CharSet.Auto)]
        internal static extern bool IntClientToScreen(IntPtr hwndFrom, [In, Out] ref NativeStructs.POINT pt);

        /// <summary>Converts the client-area coordinates of a specified point to screen coordinates.</summary>
        /// <param name="hwndFrom">Handle to the window whose client area is used for the conversion.</param>
        /// <param name="pt">POINT structure that contains the client coordinates to be converted.</param>
        /// <returns>true if the function succeeds, false otherwise.</returns>
        public static bool ClientToScreen(IntPtr hwndFrom, ref NativeStructs.POINT pt)
        {
            return IntClientToScreen(hwndFrom, ref pt);
        }

        [DllImport(ExternDll.User32, EntryPoint = "CreateWindowEx", CharSet = CharSet.Auto, SetLastError = true)]
        internal static extern IntPtr IntCreateWindowEx(int dwExStyle, string lpszClassName,
                                                   string lpszWindowName, int style, int x, int y, int width, int height,
                                                   IntPtr hWndParent, IntPtr hMenu, IntPtr hInst, IntPtr lpParam);

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public static IntPtr CreateWindowEx(int dwExStyle, string lpszClassName,
                                         string lpszWindowName, int style, int x, int y, int width, int height,
                                         IntPtr hWndParent, IntPtr hMenu, IntPtr hInst, IntPtr lpParam)
        {
            return IntCreateWindowEx(dwExStyle, lpszClassName,
                                         lpszWindowName, style, x, y, width, height, hWndParent, hMenu,
                                         hInst, lpParam);

        }


        /// <summary>
        /// Call Win32 Dest
        /// </summary>
        /// <param name="hWnd"></param>
        /// <returns></returns>
        [DllImport(ExternDll.User32, ExactSpelling = true, EntryPoint = "DestroyWindow", CharSet = CharSet.Auto)]
        internal static extern bool IntDestroyWindow(HandleRef hWnd);

        /// <summary>
        /// Wraps Win32 Destroy Window
        /// </summary>
        /// <param name="hWnd"></param>
        /// <returns></returns>
        public static bool DestroyWindow(HandleRef hWnd)
        {
            return IntDestroyWindow(hWnd);
        }

        [DllImport(ExternDll.Kernel32, ExactSpelling = true, EntryPoint = "GetCurrentThreadId", CharSet = System.Runtime.InteropServices.CharSet.Auto)]
        internal static extern int IntGetCurrentThreadId();

        /// <summary>
        /// Wraps Win32 GetCurrenThreadId
        /// </summary>
        public static int GetCurrentThreadId()
        {
            return IntGetCurrentThreadId();
        }

        /// <summary>
        /// Enables or Disables mouse and keyboard input to the specified window or win32 control
        /// </summary>
        /// <param name="hWnd"></param>
        /// <param name="bEnable"></param>
        /// <returns></returns>
        [DllImport(ExternDll.User32, ExactSpelling = true, EntryPoint = "EnableWindow", CharSet = CharSet.Auto)]
        internal static extern bool IntEnableWindow(
         HandleRef hWnd,     // handle to window
         bool bEnable   // enable or disable input
         );

        /// <summary>
        /// Enables or Disables mouse and keyboard input to the specified window or win32 control
        /// </summary>
        /// <param name="hWnd"></param>
        /// <param name="bEnable"></param>
        /// <returns></returns>
        public static bool EnableWindow(
         HandleRef hWnd,     // handle to window
         bool bEnable   // enable or disable input
         )
        {

            return IntEnableWindow(hWnd, bEnable);
        }

        //
        // SendInput related
        //

        /// <summary>
        /// </summary>
        public const int VK_SHIFT = 0x10;
        /// <summary>
        /// </summary>
        public const int VK_CONTROL = 0x11;
        /// <summary>
        /// </summary>
        public const int VK_MENU = 0x12;

        /// <summary>
        /// </summary>
        public const int KEYEVENTF_EXTENDEDKEY = 0x0001;
        /// <summary>
        /// </summary>
        public const int KEYEVENTF_KEYUP = 0x0002;
        /// <summary>
        /// </summary>
        public const int KEYEVENTF_UNICODE = 0x0004;
        /// <summary>
        /// </summary>
        public const int KEYEVENTF_SCANCODE = 0x0008;

        /// <summary>
        /// </summary>
        public const int MOUSEEVENTF_VIRTUALDESK = 0x4000;

        /// <summary>
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        public struct INPUT
        {
            /// <summary>
            /// </summary>
            public int type;
            /// <summary>
            /// </summary>
            public INPUTUNION union;
        };

        /// <summary>
        /// </summary>
        [StructLayout(LayoutKind.Explicit)]
        public struct INPUTUNION
        {
            /// <summary>
            /// </summary>
            [FieldOffset(0)]
            public MOUSEINPUT mouseInput;
            /// <summary>
            /// </summary>
            [FieldOffset(0)]
            public KEYBDINPUT keyboardInput;
        };

        /// <summary>
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        public struct MOUSEINPUT
        {
            /// <summary>
            /// </summary>
            public int dx;
            /// <summary>
            /// </summary>
            public int dy;
            /// <summary>
            /// </summary>
            public int mouseData;
            /// <summary>
            /// </summary>
            public int dwFlags;
            /// <summary>
            /// </summary>
            public int time;
            /// <summary>
            /// </summary>
            public IntPtr dwExtraInfo;
        };

        /// <summary>
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        public struct KEYBDINPUT
        {
            /// <summary>
            /// </summary>
            public short wVk;
            /// <summary>
            /// </summary>
            public short wScan;
            /// <summary>
            /// </summary>
            public int dwFlags;
            /// <summary>
            /// </summary>
            public int time;
            /// <summary>
            /// </summary>
            public IntPtr dwExtraInfo;
        };

        /// <summary>
        /// </summary>
        public const int INPUT_MOUSE = 0;
        /// <summary>
        /// </summary>
        public const int INPUT_KEYBOARD = 1;

        /// <summary>
        /// </summary>
        [DllImport(ExternDll.User32, SetLastError = true)]
        public static extern int SendInput(int nInputs, ref INPUT mi, int cbSize);

        /// <summary>
        /// MapVirtualKey
        /// </summary>
        /// <param name="nVirtKey">nVirtKey</param>
        /// <param name="nMapType">nMapType</param>
        /// <returns></returns>
        [DllImport(ExternDll.User32, ExactSpelling = true, EntryPoint = "MapVirtualKeyW", CharSet = CharSet.Auto)]
        internal static extern int IntMapVirtualKey(int nVirtKey, int nMapType);


        /// <summary>
        /// MapVirtualKey
        /// </summary>
        /// <param name="nVirtKey">nVirtKey</param>
        /// <param name="nMapType">nMapType</param>
        /// <returns></returns>
        public static int MapVirtualKey(int nVirtKey, int nMapType)
        {
            return IntMapVirtualKey(nVirtKey, nMapType);
        }


        /// <summary>
        /// </summary>
        [DllImport(ExternDll.User32, CharSet = CharSet.Auto)]
        public static extern int GetAsyncKeyState(int nVirtKey);

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        [DllImport(ExternDll.User32, EntryPoint = "GetKeyState", ExactSpelling = true, CharSet = CharSet.Auto)]
        internal static extern short IntGetKeyState(int keyCode);

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public static short GetKeyState(int keyCode)
        {
            return IntGetKeyState(keyCode);
        }


        //
        // Keyboard state
        //

        /// <summary>
        /// GetKeyboardState (internal)
        /// </summary>
        /// <param name="keystate">keystate</param>
        /// <returns>int</returns>
        [DllImport(ExternDll.User32, ExactSpelling = true, EntryPoint = "GetKeyboardState", CharSet = CharSet.Auto)]
        internal static extern int IntGetKeyboardState(byte[] keystate);

        /// <summary>
        /// GetKeyboardState
        /// </summary>
        /// <param name="keystate">keystate</param>
        /// <returns>int</returns>
        public static int GetKeyboardState(byte[] keystate)
        {
            return IntGetKeyboardState(keystate);
        }


        /// <summary>
        /// </summary>
        [DllImport(ExternDll.User32, ExactSpelling = true, EntryPoint = "keybd_event", CharSet = CharSet.Auto)]
        internal static extern void Keybd_event(byte vk, byte scan, int flags, int extrainfo);

        /// <summary>
        /// SetKeyboardState (internal)
        /// </summary>
        /// <param name="keystate">keystate</param>
        /// <returns>int</returns>
        [DllImport(ExternDll.User32, ExactSpelling = true, EntryPoint = "SetKeyboardState", CharSet = CharSet.Auto)]
        internal static extern int IntSetKeyboardState(byte[] keystate);

        /// <summary>
        /// SetKeyboardState
        /// </summary>
        /// <param name="keystate">keystate</param>
        /// <returns>int</returns>
        public static int SetKeyboardState(byte[] keystate)
        {
            return IntSetKeyboardState(keystate);
        }






        [DllImport(ExternDll.User32, ExactSpelling = true, EntryPoint = "GetClassInfoW", CharSet = CharSet.Auto)]
        internal static extern bool IntGetClassInfox(HandleRef hInst, string lpszClass, [In, Out] NativeStructs.WNDCLASS wc);


        /// <summary>
        /// </summary>
        public static bool GetClassInfo(HandleRef hInst, string lpszClass, NativeStructs.WNDCLASS wc)
        {
            return IntGetClassInfox(hInst, lpszClass, wc);
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="deviceName"></param>
        /// <param name="devMode"></param>
        /// <param name="hwnd"></param>
        /// <param name="dwFlags"></param>
        /// <param name="lParam"></param>
        /// <returns></returns>
        [DllImport(ExternDll.User32, EntryPoint = "ChangeDisplaySettingsEx", SetLastError = true)]
        internal static extern int IntChangeDisplaySettingsEx(string deviceName, ref NativeStructs.DEVMODE devMode, IntPtr hwnd, int dwFlags, IntPtr lParam);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="deviceName"></param>
        /// <param name="devMode"></param>
        /// <param name="hwnd"></param>
        /// <param name="dwFlags"></param>
        /// <param name="lParam"></param>
        /// <returns></returns>
        public static int ChangeDisplaySettingsEx(string deviceName, ref NativeStructs.DEVMODE devMode, IntPtr hwnd, int dwFlags, IntPtr lParam)
        {
            return IntChangeDisplaySettingsEx(deviceName, ref devMode, hwnd, dwFlags, lParam);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="deviceName"></param>
        /// <param name="iModeNum"></param>
        /// <param name="devMode"></param>
        /// <returns></returns>
        [DllImport(ExternDll.User32, SetLastError = true, CharSet = CharSet.Auto, EntryPoint = "EnumDisplaySettings")]
        internal static extern bool IntEnumDisplaySettings([MarshalAs(UnmanagedType.LPStr)]string deviceName, int iModeNum, ref NativeStructs.DEVMODE devMode);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="deviceName"></param>
        /// <param name="iModeNum"></param>
        /// <param name="devMode"></param>
        /// <returns></returns>
        public static bool EnumDisplaySettings(string deviceName, int iModeNum, ref NativeStructs.DEVMODE devMode)
        {
            return IntEnumDisplaySettings(deviceName, iModeNum, ref devMode);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="lpDevice"></param>
        /// <param name="iDevNum"></param>
        /// <param name="lpDisplayDevice"></param>
        /// <param name="dwFlags"></param>
        /// <returns></returns>
        [DllImport(ExternDll.User32, EntryPoint = "EnumDisplayDevices")]
        internal static extern bool IntEnumDisplayDevices(string lpDevice, int iDevNum, ref NativeStructs.DISPLAY_DEVICE lpDisplayDevice, int dwFlags);


        /// <summary>
        /// 
        /// </summary>
        /// <param name="lpDevice"></param>
        /// <param name="iDevNum"></param>
        /// <param name="lpDisplayDevice"></param>
        /// <param name="dwFlags"></param>
        /// <returns></returns>
        public static bool EnumDisplayDevices(string lpDevice, int iDevNum, ref NativeStructs.DISPLAY_DEVICE lpDisplayDevice, int dwFlags)
        {
            return IntEnumDisplayDevices(lpDevice, iDevNum, ref  lpDisplayDevice, dwFlags);
        }



        /// <summary>
        /// Extern for GetFocus
        /// </summary>
        /// <returns></returns>
        [DllImport(ExternDll.User32, ExactSpelling = true, EntryPoint = "GetFocus", CharSet = CharSet.Auto)]
        internal static extern IntPtr IntGetFocus();

        /// <summary>
        /// Wraps the Win32 GetFocus call
        /// </summary>
        /// <returns></returns>
        public static IntPtr GetFocus()
        {
            return IntGetFocus();
        }


        [DllImport(ExternDll.User32, ExactSpelling = true, EntryPoint = "GetClassInfoW", CharSet = CharSet.Auto)]
        internal static extern bool IntGetClassInfo(HandleRef hInst, string lpszClass, [In, Out] NativeStructs.WNDCLASS_I wc);


        /// <summary>
        /// </summary>
        public static bool GetClassInfo(HandleRef hInst, string lpszClass, NativeStructs.WNDCLASS_I wc)
        {
            return IntGetClassInfo(hInst, lpszClass, wc);
        }






        [DllImport(ExternDll.User32, ExactSpelling = true, EntryPoint = "GetClassInfoW", CharSet = CharSet.Auto)]
        internal static extern bool IntGetClassInfo(HandleRef hInst, string lpszClass, IntPtr h);

        /// <summary>
        /// </summary>
        public static bool GetClassInfo(HandleRef hInst, string lpszClass, IntPtr h)
        {
            return IntGetClassInfo(hInst, lpszClass, h);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="dwFlags"></param>
        /// <param name="lpSource"></param>
        /// <param name="dwMessageId"></param>
        /// <param name="dwLanguageId"></param>
        /// <param name="lpBuffer"></param>
        /// <param name="nSize"></param>
        /// <param name="va_list"></param>
        /// <returns></returns>
        [DllImport(ExternDll.Kernel32, EntryPoint = "FormatMessage", SetLastError = true, CharSet = CharSet.Ansi)]
        internal static extern int IntFormatMessage(int dwFlags, IntPtr lpSource, int dwMessageId, int dwLanguageId, ref StringBuilder lpBuffer, int nSize, IntPtr va_list);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="dwFlags"></param>
        /// <param name="lpSource"></param>
        /// <param name="dwMessageId"></param>
        /// <param name="dwLanguageId"></param>
        /// <param name="lpBuffer"></param>
        /// <param name="nSize"></param>
        /// <param name="va_list"></param>
        /// <returns></returns>
        public static int FormatMessage(int dwFlags, IntPtr lpSource, int dwMessageId, int dwLanguageId, ref StringBuilder lpBuffer, int nSize, IntPtr va_list)
        {
            return IntFormatMessage(dwFlags, lpSource, dwMessageId, dwLanguageId, ref lpBuffer, nSize, va_list);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="herror"></param>
        /// <returns></returns>
        public static string GetStringForErrorCode(int herror)
        {
            StringBuilder retVal = new StringBuilder();
            int dwFlags = NativeConstants.FORMAT_MESSAGE_FROM_SYSTEM |
                NativeConstants.FORMAT_MESSAGE_ALLOCATE_BUFFER |
                NativeConstants.FORMAT_MESSAGE_IGNORE_INSERTS;

            if (IntFormatMessage(dwFlags, IntPtr.Zero, herror, 0, ref retVal, 0, IntPtr.Zero) == 0)
            {
                // call failed, not a major issue, just return the error code
                return "( Unable to retrive string associated with this error code)";
            }
            return retVal.ToString();
        }



        [DllImport(ExternDll.User32, ExactSpelling = true, EntryPoint = "GetClassInfoExW", CharSet = CharSet.Auto)]
        internal static extern bool IntGetClassInfoEx(IntPtr hInst, string lpszClass, [In, Out] NativeStructs.WNDCLASSEX_I wc);

        /// <summary>
        /// </summary>
        public static bool GetClassInfoEx(IntPtr hInst, string lpszClass, NativeStructs.WNDCLASSEX_I wc)
        {
            return IntGetClassInfoEx(hInst, lpszClass, wc);
        }



        /// <summary>
        /// GetCursor (internal)
        /// </summary>
        /// <returns>IDC cursor value.</returns>
        [DllImport(ExternDll.User32, ExactSpelling = true, EntryPoint = "GetCursor", CharSet = CharSet.Auto)]
        internal static extern IntPtr IntGetCursor();

        /// <summary>
        /// GetCursor
        /// </summary>
        /// <returns>IDC cursor value.</returns>
        public static IntPtr GetCursor()
        {
            return IntGetCursor();
        }



        [DllImport(ExternDll.Gdi32, ExactSpelling = true, EntryPoint = "GetDeviceCaps", CharSet = CharSet.Auto)]
        internal static extern int IntGetDeviceCaps(HandleRef hDC, int nIndex);

        /// <summary>
        /// 
        /// </summary>
        public static int GetDeviceCaps(HandleRef hDC, int nIndex)
        {
            return IntGetDeviceCaps(hDC, nIndex);
        }

        /// <summary>Performs a bit-block transfer of the color data corresponding to
        /// a rectangle of pixels from the specified source device context into a
        /// destination device context.</summary>
        /// <param name="hdcDest">Handle to the destination device context.</param>
        /// <param name="xDest">Specifies the x-coordinate, in logical units, of the upper-left corner of the destination rectangle.</param>
        /// <param name="yDest">Specifies the y-coordinate, in logical units, of the upper-left corner of the destination rectangle.</param>
        /// <param name="cxDest">Specifies the width, in logical units, of the source and destination rectangles.</param>
        /// <param name="cyDest">Specifies the height, in logical units, of the source and the destination rectangles.</param>
        /// <param name="hdcSrc">Handle to the source device context.</param>
        /// <param name="xSrc">Specifies the x-coordinate, in logical units, of the upper-left corner of the source rectangle.</param>
        /// <param name="ySrc">Specifies the y-coordinate, in logical units, of the upper-left corner of the source rectangle.</param>
        /// <param name="dwRop">Specifies a raster-operation code.</param>
        /// <returns>true on success, false otherwise.</returns>
        [DllImport(ExternDll.Gdi32, ExactSpelling = true, EntryPoint = "BitBlt", CharSet = CharSet.Auto)]
        internal static extern bool IntBitBlt(
            IntPtr hdcDest, int xDest, int yDest, int cxDest, int cyDest,
            IntPtr hdcSrc, int xSrc, int ySrc, int dwRop);

        /// <summary>
        /// 
        /// </summary>
        public static bool SafeBitBlt(IntPtr hdcDest, int xDest, int yDest, int cxDest, int cyDest,
            IntPtr hdcSrc, int xSrc, int ySrc, int dwRop)
        {
            return IntBitBlt(hdcDest, xDest, yDest, cxDest, cyDest, hdcSrc, xSrc, ySrc, dwRop);
        }

        /// <summary>
        /// 
        /// </summary>
        [DllImport(ExternDll.User32, ExactSpelling = true, EntryPoint = "GetDC", CharSet = CharSet.Auto)]
        internal static extern IntPtr IntGetDC(HandleRef hWnd);

        /// <summary>
        /// 
        /// </summary>
        public static IntPtr GetDC(HandleRef hWnd)
        {
            return HandleCollector.Add(IntGetDC(hWnd), CommonHandles.HDC);
        }





        [DllImport(ExternDll.Kernel32, ExactSpelling = true, EntryPoint = "GetModuleHandleW", CharSet = CharSet.Auto)]
        internal static extern IntPtr IntGetModuleHandle(string modName);

        /// <summary>
        /// </summary>
        public static IntPtr GetModuleHandle(string modName)
        {
            return IntGetModuleHandle(modName);
        }



        [DllImport(ExternDll.User32, ExactSpelling = true, EntryPoint = "GetSystemMenu", CharSet = CharSet.Auto)]
        internal static extern IntPtr IntGetSystemMenu(HandleRef hWnd, bool bRevert);

        /// <summary>
        /// Get the System Menu
        /// </summary>
        public static IntPtr GetSystemMenu(HandleRef hWnd, bool bRevert)
        {
            return IntGetSystemMenu(hWnd, bRevert);
        }



        [DllImport(ExternDll.User32, ExactSpelling = true, EntryPoint = "GetMouseMovePointsEx", CharSet = CharSet.Auto, SetLastError = true)]
        internal static extern int IntGetMouseMovePointsEx(
                                        int cbSize,
                                        [In] ref NativeStructs.MOUSEMOVEPOINT pointsIn,
                                        [Out] NativeStructs.MOUSEMOVEPOINT[] pointsBufferOut,
                                        int nBufPoints,
                                        int resolution
                                   );

        /// <summary>
        /// </summary>
        public static int GetMouseMovePointsEx(
                                        int cbSize,
                                        [In] ref NativeStructs.MOUSEMOVEPOINT pointsIn,
                                        [Out] NativeStructs.MOUSEMOVEPOINT[] pointsBufferOut,
                                        int nBufPoints,
                                        int resolution
                                   )
        {
            return IntGetMouseMovePointsEx(
                cbSize,
                ref pointsIn,
                pointsBufferOut,
                nBufPoints,
                resolution);
        }




        [DllImport(ExternDll.User32, ExactSpelling = true, EntryPoint = "DefWindowProcW", CharSet = CharSet.Auto)]
        internal static extern IntPtr IntDefWindowProc(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);

        /// <summary>
        /// </summary>
        public static IntPtr DefWindowProc(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam)
        {
            return IntDefWindowProc(hWnd, msg, wParam, lParam);
        }






        /// <summary>
        /// 
        /// </summary>
        /// <param name="hwnd"></param>
        /// <param name="rc"></param>
        /// <returns></returns>
        [DllImport(ExternDll.User32, ExactSpelling = true, EntryPoint = "GetClientRect", CharSet = CharSet.Auto)]
        internal static extern bool IntGetClientRect(
                    HandleRef hwnd, [In, Out] ref NativeStructs.RECT rc);


        /// <summary>
        /// 
        /// </summary>
        /// <param name="hwnd"></param>
        /// <param name="rc"></param>
        /// <returns></returns>
        public static bool GetClientRect(HandleRef hwnd, ref NativeStructs.RECT rc)
        {
            return IntGetClientRect(hwnd, ref  rc);
        }

        /// <summary>Retrieves the dimensions of the bounding rectangle of the specified window.</summary>
        /// <param name="hwnd">Handle to the window whose dimensions are to be retrieved.</param>
        /// <param name="rc">RECT structure that receives the coordinates.</param>
        /// <returns>true if the function succeeds, false otherwise.</returns>
        /// <remarks>The dimensions are given in screen coordinates that are relative to the upper-left corner of the screen.</remarks>
        [DllImport(ExternDll.User32, ExactSpelling = true, EntryPoint = "GetWindowRect", CharSet = CharSet.Auto)]
        public static extern bool IntGetWindowRect(
            HandleRef hwnd, [In, Out] ref NativeStructs.RECT rc);


        /// <summary>
        /// 
        /// </summary>
        /// <param name="hwnd"></param>
        /// <param name="rc"></param>
        /// <returns></returns>
        public static bool GetWindowRect(HandleRef hwnd, ref NativeStructs.RECT rc)
        {
            return IntGetWindowRect(hwnd, ref  rc);
        }

        /// <summary>
        /// GetCursorPos
        /// </summary>
        /// <param name="lpPoint">lpPoint</param>
        /// <returns></returns>
        [DllImport(ExternDll.User32, ExactSpelling = true, EntryPoint = "GetCursorPos", CharSet = CharSet.Auto)]
        internal static extern bool IntGetCursorPos(ref NativeStructs.POINT lpPoint);

        /// <summary>
        /// GetCursorPos
        /// </summary>
        /// <param name="lpPoint">lpPoint</param>
        /// <returns></returns>
        public static bool GetCursorPos(ref NativeStructs.POINT lpPoint)
        {
            return IntGetCursorPos(ref lpPoint);
        }


        /// <summary>
        /// user32 function GetKeyboardLayout
        /// supplying threadID = 0 means the current thread
        /// </summary>
        /// <param name="threadID">thread id, 0 means current thread</param>
        /// <returns>return hkl for the thread</returns>
        [DllImport(ExternDll.User32, ExactSpelling = true, EntryPoint = "GetKeyboardLayout", CharSet = CharSet.Auto)]
        public static extern IntPtr IntGetKeyboardLayout(IntPtr threadID);

        /// <summary>
        /// user32 function GetKeyboardLayout
        /// supplying threadID = 0 means the current thread
        /// </summary>
        /// <param name="threadID">thread id, 0 means current thread</param>
        /// <returns>return hkl for the thread</returns>
        public static IntPtr GetKeyboardLayout(IntPtr threadID)
        {
            return IntGetKeyboardLayout(threadID);
        }


        /// <summary>
        /// 
        /// </summary>
        [DllImport(ExternDll.User32, ExactSpelling = true, EntryPoint = "GetParent", CharSet = CharSet.Auto)]
        internal static extern IntPtr IntGetParent(HandleRef hWnd);

        /// <summary>
        /// 
        /// </summary>
        public static IntPtr GetParent(HandleRef hWnd)
        {
            return IntGetParent(hWnd);
        }

        /// <summary>
        /// GetSystemMetrics
        /// </summary>
        /// <param name="nIndex">nIndex (SM constant)</param>
        /// <returns></returns>
        [DllImport(ExternDll.User32, ExactSpelling = true, EntryPoint = "GetSystemMetrics", CharSet = CharSet.Auto)]
        internal static extern int IntGetSystemMetrics(int nIndex);


        /// <summary>
        /// GetSystemMetrics
        /// </summary>
        /// <param name="nIndex">nIndex (SM constant)</param>
        /// <returns></returns>
        public static int GetSystemMetrics(int nIndex)
        {

            return IntGetSystemMetrics(nIndex);
        }


        /// <summary>
        /// GetSystemMetrics
        /// </summary>
        [DllImport(ExternDll.User32, ExactSpelling = true, EntryPoint = "GetSysColorBrush", CharSet = System.Runtime.InteropServices.CharSet.Auto)]
        internal static extern IntPtr IntGetSysColorBrush(int nIndex);

        /// <summary>
        /// GetSystemMetrics
        /// </summary>        
        public static IntPtr GetSysColorBrush(int nIndex)
        {
            return IntGetSysColorBrush(nIndex);

        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="hWnd"></param>
        /// <returns></returns>
        [DllImport(ExternDll.User32, EntryPoint = "GetWindowDC", SetLastError = true)]
        internal static extern IntPtr IntGetWindowDC(IntPtr hWnd);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="hWnd"></param>
        /// <returns></returns>
        public static IntPtr GetWindowDC(IntPtr hWnd)
        {
            return IntGetWindowDC(hWnd);
        }



        /// <summary>
        /// Enables or Disables mouse and keyboard input to the specified window or win32 control
        /// </summary>
        [DllImport("comctl32.dll", EntryPoint = "InitCommonControls", CharSet = CharSet.Auto)]
        internal static extern void IntInitCommonControls();


        /// <summary>
        /// Enables or Disables mouse and keyboard input to the specified window or win32 control
        /// </summary>
        public static void InitCommonControls()
        {
            IntInitCommonControls();
        }


        /// <summary>
        /// The IsWindowVisible function retrieves the visibility state of the specified window
        /// </summary>
        [DllImport(ExternDll.User32, ExactSpelling = true, EntryPoint = "IsWindowVisible", CharSet = CharSet.Auto)]
        internal static extern bool IntIsWindowVisible(HandleRef hwnd);


        /// <summary>
        /// The IsWindowVisible function retrieves the visibility state of the specified window
        /// </summary>
        public static bool IsWindowVisible(HandleRef hwnd)
        {
            return IntIsWindowVisible(hwnd);
        }

        /// <summary>
        /// Brings the specified window to the top of the Z order. If the
        /// window is a top-level window, it is activated. If the window
        /// is a child window, the top-level parent window associated with
        /// the child window is activated.
        /// </summary>
        /// <param name="hwnd">
        /// Handle to the window to bring to the top of the Z order.
        /// </param>
        /// <returns>Nonzero on success, zero otherwise.</returns>
        [System.Runtime.InteropServices.DllImport(ExternDll.User32)]
        internal static extern bool BringWindowToTop(IntPtr hwnd);

        /// <summary>
        /// Makes a safe call to BringWindowToTop.
        /// </summary>
        /// <param name="hwnd">
        /// Handle to the window to bring to the top of the Z order.
        /// </param>
        /// <returns>Nonzero on success, zero otherwise.</returns>
        public static bool SafeBringWindowToTop(IntPtr hwnd)
        {
            new System.Security.Permissions.SecurityPermission(
                System.Security.Permissions.PermissionState.Unrestricted)
                .Assert();
            return BringWindowToTop(hwnd);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="vk"></param>
        /// <param name="scan"></param>
        /// <param name="flags"></param>
        /// <param name="extrainfo"></param>
        [DllImport(ExternDll.User32, ExactSpelling = true, EntryPoint = "keybd_event", CharSet = CharSet.Auto)]
        internal static extern void IntKeybd_event(byte vk, byte scan, int flags, IntPtr extrainfo);


        /// <summary>
        /// 
        /// </summary>
        /// <param name="vk"></param>
        /// <param name="scan"></param>
        /// <param name="flags"></param>
        /// <param name="extrainfo"></param>
        public static void Keybd_event(byte vk, byte scan, int flags, IntPtr extrainfo)
        {
            IntKeybd_event(vk, scan, flags, extrainfo);
        }


        /// <summary>
        /// IsWindow
        /// </summary>
        [DllImport(ExternDll.User32, ExactSpelling = true, EntryPoint = "IsWindow", CharSet = CharSet.Auto)]
        internal static extern bool IntIsWindow(HandleRef hwnd);


        /// <summary>
        /// IsWindow Win32
        /// </summary>
        /// <param name="hwnd">HWND</param>
        /// <returns></returns>
        public static bool IsWindow(HandleRef hwnd)
        {
            return IntIsWindow(hwnd);
        }


        /// <summary>
        /// MapWindowsPoints
        /// </summary>
        [System.Runtime.InteropServices.DllImport(ExternDll.User32, ExactSpelling = true, EntryPoint = "MapWindowPoints", CharSet = CharSet.Auto)]
        internal static extern bool IntMapWindowPoints(IntPtr hwndFrom, IntPtr hwndTo, ref NativeStructs.RECT rc, int count);


        /// <summary>
        /// 
        /// </summary>
        public static bool MapWindowPoints(IntPtr hwndFrom, IntPtr hwndTo, ref NativeStructs.RECT rc, int count)
        {
            return IntMapWindowPoints(hwndFrom, hwndTo, ref rc, count);
        }



        /// <summary>
        /// 
        /// </summary>
        /// <param name="hwnd"></param>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="width"></param>
        /// <param name="height"></param>
        /// <param name="bRepaint"></param>
        /// <returns></returns>
        [DllImport(ExternDll.User32, ExactSpelling = true, EntryPoint = "MoveWindow", CharSet = CharSet.Auto)]
        internal static extern bool IntMoveWindow(HandleRef hwnd, int x, int y, int width, int height, bool bRepaint);


        /// <summary>
        /// 
        /// </summary>
        /// <param name="hwnd"></param>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="width"></param>
        /// <param name="height"></param>
        /// <param name="bRepaint"></param>
        /// <returns></returns>
        public static bool MoveWindow(HandleRef hwnd, int x, int y, int width, int height, bool bRepaint)
        {
            return IntMoveWindow(hwnd, x, y, width, height, bRepaint);
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="flags"></param>
        /// <param name="dx"></param>
        /// <param name="dy"></param>
        /// <param name="dwData"></param>
        /// <param name="extrainfo"></param>
        [DllImport(ExternDll.User32, ExactSpelling = true, EntryPoint = "mouse_event", CharSet = CharSet.Auto)]
        internal static extern void IntMouse_event(int flags, int dx, int dy, int dwData, IntPtr extrainfo);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="flags"></param>
        /// <param name="dx"></param>
        /// <param name="dy"></param>
        /// <param name="dwData"></param>
        /// <param name="extrainfo"></param>
        public static void Mouse_event(int flags, int dx, int dy, int dwData, IntPtr extrainfo)
        {

            IntMouse_event(flags, dx, dy, dwData, extrainfo);
        }







        /// <summary>
        /// 
        /// </summary>
        /// <param name="hWnd"></param>
        /// <param name="nMsg"></param>
        /// <param name="wParam"></param>
        /// <param name="lParam"></param>
        /// <returns></returns>
        [DllImport(ExternDll.User32, ExactSpelling = true, EntryPoint = "PostMessageW", CharSet = CharSet.Auto)]
        internal static extern bool IntPostMessage(
                HandleRef hWnd, int nMsg, IntPtr wParam, IntPtr lParam);


        /// <summary>
        /// 
        /// </summary>
        /// <param name="hWnd"></param>
        /// <param name="nMsg"></param>
        /// <param name="wParam"></param>
        /// <param name="lParam"></param>
        /// <returns></returns>
        public static bool PostMessage(
                HandleRef hWnd, int nMsg, IntPtr wParam, IntPtr lParam)
        {
            return IntPostMessage(hWnd, nMsg, wParam, lParam);
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="id"></param>
        /// <param name="nMsg"></param>
        /// <param name="wParam"></param>
        /// <param name="lParam"></param>
        /// <returns></returns>
        [DllImport(ExternDll.User32, ExactSpelling = true, EntryPoint = "PostThreadMessageW", CharSet = CharSet.Auto)]
        internal static extern bool IntPostThreadMessage(
                int id, int nMsg, IntPtr wParam, IntPtr lParam);


        /// <summary>
        /// 
        /// </summary>
        /// <param name="id"></param>
        /// <param name="nMsg"></param>
        /// <param name="wParam"></param>
        /// <param name="lParam"></param>
        /// <returns></returns>
        public static bool PostThreadMessage(
                int id, int nMsg, IntPtr wParam, IntPtr lParam)
        {
            return IntPostThreadMessage(id, nMsg, wParam, lParam);
        }




        /// <summary>
        /// 
        /// </summary>
        /// <param name="nExitCode"></param>
        [DllImport(ExternDll.User32, ExactSpelling = true, EntryPoint = "PostQuitMessage", CharSet = CharSet.Auto)]
        internal static extern void IntPostQuitMessage(
                int nExitCode);


        /// <summary>
        /// 
        /// </summary>
        /// <param name="nExitCode"></param>
        public static void PostQuitMessage(
                int nExitCode)
        {
            IntPostQuitMessage(nExitCode);
        }


        [DllImport(ExternDll.User32, ExactSpelling = true, EntryPoint = "RegisterClassExW", CharSet = CharSet.Auto, SetLastError = true)]
        internal static extern IntPtr IntRegisterClassEx(NativeStructs.WNDCLASSEX_D wc_d);

        /// <summary>
        /// </summary>
        public static IntPtr RegisterClassEx(NativeStructs.WNDCLASSEX_D wc_d)
        {
            return IntRegisterClassEx(wc_d);
        }


        /// <summary>
        /// 
        /// </summary>
        [DllImport(ExternDll.User32, ExactSpelling = true, EntryPoint = "ReleaseDC", CharSet = CharSet.Auto)]
        internal static extern int IntReleaseDC(HandleRef hWnd, HandleRef hDC);

        /// <summary>
        /// 
        /// </summary>
        public static int ReleaseDC(HandleRef hWnd, HandleRef hDC)
        {
            HandleCollector.Remove((IntPtr)hDC, CommonHandles.HDC);
            return IntReleaseDC(hWnd, hDC);
        }

        /// <summary>
        /// 
        /// </summary>
        [DllImport(ExternDll.User32, ExactSpelling = true, EntryPoint = "SetParent", CharSet = CharSet.Auto)]
        internal static extern IntPtr IntSetParent(HandleRef hWnd, HandleRef hWndParent);


        /// <summary>
        /// 
        /// </summary>
        public static IntPtr SetParent(HandleRef hWnd, HandleRef hWndParent)
        {
            return IntSetParent(hWnd, hWndParent);
        }




        /// <summary>
        /// 
        /// </summary>
        /// <param name="hWnd"></param>
        /// <param name="hWndInsertAfter"></param>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="cx"></param>
        /// <param name="cy"></param>
        /// <param name="flags"></param>
        /// <returns></returns>
        [DllImport(ExternDll.User32, ExactSpelling = true, EntryPoint = "SetWindowPos", CharSet = CharSet.Auto)]
        internal static extern bool IntSetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter,
                int x, int y, int cx, int cy, int flags);



        /// <summary>
        /// 
        /// </summary>
        /// <param name="hWnd"></param>
        /// <param name="hWndInsertAfter"></param>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="cx"></param>
        /// <param name="cy"></param>
        /// <param name="flags"></param>
        /// <returns></returns>
        public static bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter,
                int x, int y, int cx, int cy, int flags)
        {
            return IntSetWindowPos(hWnd, hWndInsertAfter, x, y, cx, cy, flags);
        }







        /// <summary>
        /// SendInput (keyboard overload)
        /// </summary>
        /// <param name="nInputs">nInputs</param>
        /// <param name="ki">ki</param>
        /// <param name="cbSize">cbSize</param>
        /// <returns></returns>
        [DllImport(ExternDll.User32, ExactSpelling = true, EntryPoint = "SendInput", CharSet = CharSet.Auto)]
        internal static extern int IntSendInput(int nInputs, ref NativeStructs.KEYBDINPUT ki, int cbSize);


        /// <summary>
        /// SendInput (keyboard overload)
        /// </summary>
        /// <param name="nInputs">nInputs</param>
        /// <param name="ki">ki</param>
        /// <param name="cbSize">cbSize</param>
        /// <returns></returns>
        public static int SendInput(int nInputs, ref NativeStructs.KEYBDINPUT ki, int cbSize)
        {

            return IntSendInput(nInputs, ref  ki, cbSize);
        }









        /// <summary>
        /// SendInput (mouse overload)
        /// </summary>
        /// <param name="nInputs">nInputs</param>
        /// <param name="mi">mi</param>
        /// <param name="cbSize">cbSize</param>
        /// <returns></returns>
        [DllImport(ExternDll.User32, ExactSpelling = true, EntryPoint = "SendInput", CharSet = CharSet.Auto)]
        internal static extern int IntSendInput(int nInputs, ref NativeStructs.MOUSEINPUT mi, int cbSize);


        /// <summary>
        /// SendInput (mouse overload)
        /// </summary>
        /// <param name="nInputs">nInputs</param>
        /// <param name="mi">mi</param>
        /// <param name="cbSize">cbSize</param>
        /// <returns></returns>
        public static int SendInput(int nInputs, ref NativeStructs.MOUSEINPUT mi, int cbSize)
        {
            return IntSendInput(nInputs, ref  mi, cbSize);
        }

        /// <summary>
        /// MsgWaitForMultipleObjectsEx
        /// </summary>
        [DllImport(ExternDll.User32, EntryPoint = "MsgWaitForMultipleObjectsEx", SetLastError = true, ExactSpelling = true, CharSet = CharSet.Auto)]
        private static extern int IntMsgWaitForMultipleObjectsEx(int nCount, IntPtr[] pHandles, int dwMilliseconds, int dwWakeMask, int dwFlags);

        /// <summary>
        /// MsgWaitForMultipleObjectsEx
        /// </summary>
        public static int MsgWaitForMultipleObjectsEx(int nCount, IntPtr[] pHandles, int dwMilliseconds, int dwWakeMask, int dwFlags)
        {
            return IntMsgWaitForMultipleObjectsEx(nCount, pHandles, dwMilliseconds, dwWakeMask, dwFlags);
        }




        /// <summary>
        /// 
        /// </summary>
        [DllImport(ExternDll.User32, ExactSpelling = true, EntryPoint = "GetMonitorInfoW", CharSet = CharSet.Auto)]
        internal static extern bool IntGetMonitorInfo(IntPtr hmonitor, [In, Out]NativeStructs.MONITORINFOEX info);

        /// <summary>
        /// 
        /// </summary>
        public static bool GetMonitorInfo(IntPtr hmonitor, NativeStructs.MONITORINFOEX info)
        {
            return IntGetMonitorInfo(hmonitor, info);
        }

        /// <summary>
        /// 
        /// </summary>
        [DllImport(ExternDll.User32, ExactSpelling = true, EntryPoint = "MonitorFromPoint", CharSet = CharSet.Auto)]
        internal static extern IntPtr IntMonitorFromPoint(int x, int y, int flags);

        /// <summary>
        /// 
        /// </summary>
        public static IntPtr MonitorFromPoint(int x, int y, int flags)
        {
            return IntMonitorFromPoint(x, y, flags);
        }


        /// <summary>
        /// 
        /// </summary>
        [DllImport(ExternDll.User32, ExactSpelling = true, EntryPoint = "MonitorFromRect", CharSet = CharSet.Auto)]
        internal static extern IntPtr IntMonitorFromRect(ref NativeStructs.RECT rect, int flags);


        /// <summary>
        /// 
        /// </summary>
        internal static IntPtr MonitorFromRect(ref NativeStructs.RECT rect, int flags)
        {
            return IntMonitorFromRect(ref rect, flags);
        }


        /// <summary>
        /// 
        /// </summary>
        [DllImport(ExternDll.User32, ExactSpelling = true, EntryPoint = "MonitorFromWindow", CharSet = CharSet.Auto)]
        internal static extern IntPtr IntMonitorFromWindow(IntPtr handle, int flags);


        /// <summary>
        /// 
        /// </summary>
        public static IntPtr MonitorFromWindow(IntPtr handle, int flags)
        {
            return IntMonitorFromWindow(handle, flags);
        }

        /// <summary>
        /// 
        /// </summary>
        [DllImport(ExternDll.User32, ExactSpelling = true, EntryPoint = "EnumDisplayMonitors", CharSet = CharSet.Auto)]
        internal static extern bool IntEnumDisplayMonitors(IntPtr hdc, ref NativeStructs.RECT rcClip, MonitorEnumProc lpfnEnum, IntPtr dwData);



        /// <summary>
        /// 
        /// </summary>
        public static bool EnumDisplayMonitors(IntPtr hdc, ref NativeStructs.RECT rcClip, MonitorEnumProc lpfnEnum, IntPtr dwData)
        {
            return IntEnumDisplayMonitors(hdc, ref rcClip, lpfnEnum, dwData);
        }


        /// <summary>
        /// 
        /// </summary>
        [DllImport(ExternDll.User32, ExactSpelling = true, EntryPoint = "EnumDisplayMonitors", CharSet = CharSet.Auto)]
        internal static extern bool IntEnumDisplayMonitors(IntPtr hdc, NativeStructs.COMRECT rcClip, MonitorEnumProc lpfnEnum, IntPtr dwData);



        /// <summary>
        /// 
        /// </summary>
        public static bool EnumDisplayMonitors(IntPtr hdc, NativeStructs.COMRECT rcClip, MonitorEnumProc lpfnEnum, IntPtr dwData)
        {
            return IntEnumDisplayMonitors(hdc, rcClip, lpfnEnum, dwData);
        }



        /// <summary>
        /// 
        /// </summary>
        /// <param name="hWnd"></param>
        /// <returns></returns>
        [DllImport(ExternDll.User32, ExactSpelling = true, EntryPoint = "SetFocus", CharSet = CharSet.Auto)]
        internal static extern IntPtr IntSetFocus(HandleRef hWnd);


        /// <summary>
        /// 
        /// </summary>
        /// <param name="hWnd"></param>
        /// <returns></returns>
        public static IntPtr SetFocus(
                HandleRef hWnd)
        {

            return IntSetFocus(hWnd);
        }


        /// <summary>
        /// Extern for OpenIcon
        /// </summary>
        [DllImport(ExternDll.User32, ExactSpelling = true, EntryPoint = "OpenIcon", CharSet = CharSet.Auto)]
        internal static extern bool IntOpenIcon(HandleRef hWnd);

        /// <summary>
        /// Wraps the Win32 OpenIcon call
        /// </summary>
        public static bool OpenIcon(HandleRef hWnd)
        {
            return IntOpenIcon(hWnd);
        }


        /// <summary>
        /// Extern for CloseWindow
        /// </summary>
        [DllImport(ExternDll.User32, ExactSpelling = true, EntryPoint = "CloseWindow", CharSet = CharSet.Auto)]
        internal static extern bool IntCloseWindow(HandleRef hWnd);

        /// <summary>
        /// Wraps the Win32 CloseWindow call
        /// </summary>
        public static bool CloseWindow(HandleRef hWnd)
        {
            return IntCloseWindow(hWnd);
        }

        /// <summary>
        /// Extern for SetActiveWindow
        /// </summary>
        [DllImport(ExternDll.User32, ExactSpelling = true, EntryPoint = "SetActiveWindow", CharSet = CharSet.Auto)]
        internal static extern bool IntSetActiveWindow(HandleRef hWnd);

        /// <summary>
        /// Wraps the Win32 SetActiveWindow call
        /// </summary>
        public static bool SetActiveWindow(HandleRef hWnd)
        {
            return IntSetActiveWindow(hWnd);
        }

        /// <summary>
        /// Extern for GetActiveWindow
        /// </summary>
        [DllImport(ExternDll.User32, ExactSpelling = true, EntryPoint = "GetActiveWindow", CharSet = CharSet.Auto)]
        internal static extern IntPtr IntGetActiveWindow();

        /// <summary>
        /// Wraps the Win32 GetActiveWindow call
        /// </summary>
        public static IntPtr GetActiveWindow()
        {
            return IntGetActiveWindow();
        }

        /// <summary>
        /// Extern for SetForegroundWindow
        /// </summary>
        /// <param name="hWnd"></param>
        /// <returns></returns>
        [DllImport(ExternDll.User32, ExactSpelling = true, EntryPoint = "SetForegroundWindow", CharSet = CharSet.Auto)]
        internal static extern bool IntSetForegroundWindow(HandleRef hWnd);

        /// <summary>
        /// Wraps the Win32 SetForegroundWindow call
        /// </summary>
        /// <param name="hWnd"></param>
        /// <returns></returns>
        public static bool SetForegroundWindow(HandleRef hWnd)
        {
            return IntSetForegroundWindow(hWnd);
        }





        /// <summary>
        /// SendMessage
        /// </summary>
        /// <param name="hWnd">hWnd</param>
        /// <param name="msg">msg</param>
        /// <param name="wParam">wParam</param>
        /// <param name="lParam">lParam</param>
        /// <returns></returns>
        [DllImport(ExternDll.User32, ExactSpelling = true, EntryPoint = "SendMessageW", CharSet = CharSet.Auto)]
        internal static extern IntPtr IntSendMessage(HandleRef hWnd, int msg, int wParam, int lParam);




        /// <summary>
        /// SendMessage
        /// </summary>
        /// <param name="hWnd">hWnd</param>
        /// <param name="msg">msg</param>
        /// <param name="wParam">wParam</param>
        /// <param name="lParam">lParam</param>
        /// <returns></returns>
        public static IntPtr SendMessage(HandleRef hWnd, int msg, IntPtr wParam, IntPtr lParam)
        {
            return IntSendMessage(hWnd, msg, wParam, lParam);
        }



        /// <summary>
        /// SendMessage
        /// </summary>
        /// <param name="hWnd">hWnd</param>
        /// <param name="msg">msg</param>
        /// <param name="wParam">wParam</param>
        /// <param name="lParam">lParam</param>
        /// <returns></returns>
        [DllImport(ExternDll.User32, ExactSpelling = true, EntryPoint = "SendMessageW", CharSet = CharSet.Auto)]
        internal static extern IntPtr IntSendMessage(HandleRef hWnd, int msg, IntPtr wParam, IntPtr lParam);




        /// <summary>
        /// SendMessage
        /// </summary>
        /// <param name="hWnd">hWnd</param>
        /// <param name="msg">msg</param>
        /// <param name="wParam">wParam</param>
        /// <param name="lParam">lParam</param>
        /// <returns></returns>
        public static IntPtr SendMessage(HandleRef hWnd, int msg, int wParam, int lParam)
        {
            return IntSendMessage(hWnd, msg, wParam, lParam);
        }

        /// <summary>
        /// SendMessage
        /// </summary>
        [DllImport(ExternDll.User32, EntryPoint = "SendMessageW", CharSet = CharSet.Auto)]
        internal static extern IntPtr IntSendMessage(
            IntPtr hWnd,
            int msg,
            IntPtr wParam,
            ref NativeStructs.TVINSERTSTRUCT tvis);

        /// <summary>
        /// SendMessage
        /// </summary>
        public static IntPtr SendMessageTreeview(IntPtr hWnd, int msg, IntPtr wParam, ref NativeStructs.TVINSERTSTRUCT tvis)
        {
            return IntSendMessage(hWnd, msg, wParam, ref tvis);
        }



        [DllImport(ExternDll.User32, ExactSpelling = true, EntryPoint = "ShowWindow", CharSet = System.Runtime.InteropServices.CharSet.Auto)]
        internal static extern bool IntShowWindow(HandleRef hWnd, int nCmdShow);

        /// <summary>
        /// GetDesktopWindow (internal)
        /// </summary>
        public static bool ShowWindow(HandleRef hWnd, int nCmdShow)
        {
            return IntShowWindow(hWnd, nCmdShow);
        }






        /// <summary>
        /// 
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        [DllImport(ExternDll.User32, ExactSpelling = true, EntryPoint = "VkKeyScanW", CharSet = CharSet.Auto)]
        internal static extern short IntVkKeyScan(char key);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public static short VkKeyScan(char key)
        {
            return IntVkKeyScan(key);
        }


        /// <summary>
        /// QueryPerformanceCounter (internal)
        /// </summary>
        /// <param name="lpPerformanceCount"></param>
        /// <returns></returns>
        [System.Runtime.InteropServices.DllImport(ExternDll.Kernel32, ExactSpelling = true, EntryPoint = "QueryPerformanceCounter", CharSet = System.Runtime.InteropServices.CharSet.Auto)]
        internal static extern bool IntQueryPerformanceCounter([In, Out] ref long lpPerformanceCount);

        /// <summary>
        /// QueryPerformanceCounter
        /// </summary>
        /// <param name="lpPerformanceCount"></param>
        /// <returns></returns>
        public static bool QueryPerformanceCounter(ref long lpPerformanceCount)
        {
            return IntQueryPerformanceCounter(ref lpPerformanceCount);
        }





        /// <summary>
        /// QueryPerformanceFrequency (internal)
        /// </summary>
        /// <param name="lpFrequency"></param>
        /// <returns></returns>
        [System.Runtime.InteropServices.DllImport(ExternDll.Kernel32, ExactSpelling = true, EntryPoint = "QueryPerformanceFrequency", CharSet = System.Runtime.InteropServices.CharSet.Auto)]
        internal static extern bool IntQueryPerformanceFrequency([In, Out] ref long lpFrequency);


        /// <summary>
        /// QueryPerformanceFrequency
        /// </summary>
        /// <param name="lpFrequency"></param>
        /// <returns></returns>
        public static bool QueryPerformanceFrequency(ref long lpFrequency)
        {
            return IntQueryPerformanceFrequency(ref lpFrequency);
        }

        /// <summary>
        /// GetMessage (internal)
        /// </summary>
        /// <param name="msg"></param>
        /// <param name="hWnd"></param>
        /// <param name="uMsgFilterMin"></param>
        /// <param name="uMsgFilterMax"></param>
        /// <returns></returns>
        [DllImport(ExternDll.User32, ExactSpelling = true, EntryPoint = "GetMessageW", CharSet = CharSet.Unicode)]
        internal static extern int IntGetMessageW([In, Out] ref MSG msg, IntPtr hWnd, int uMsgFilterMin, int uMsgFilterMax);

        /// <summary>
        /// GetMessage
        /// </summary>
        /// <param name="hWnd"></param>
        /// <param name="msg"></param>
        /// <param name="uMsgFilterMax"></param>
        /// <param name="uMsgFilterMin"></param>
        /// <returns></returns>
        public static int GetMessage(ref MSG msg, IntPtr hWnd, int uMsgFilterMin, int uMsgFilterMax)
        {
            return IntGetMessageW(ref msg, hWnd, uMsgFilterMin, uMsgFilterMax);
        }


        /// <summary>
        /// TranslateMessage (internal)
        /// </summary>
        /// <param name="msg"></param>
        /// <returns></returns>
        [DllImport(ExternDll.User32, ExactSpelling = true, EntryPoint = "TranslateMessage", CharSet = CharSet.Auto)]
        internal static extern bool IntTranslateMessage([In, Out] ref MSG msg);

        /// <summary>
        /// TranslateMessage
        /// </summary>
        /// <param name="msg"></param>
        /// <returns></returns>
        public static bool TranslateMessage(ref MSG msg)
        {
            return IntTranslateMessage(ref msg);
        }


        /// <summary>
        /// DispatchMessage (internal)
        /// </summary>
        /// <param name="msg"></param>
        /// <returns></returns>
        [DllImport(ExternDll.User32, ExactSpelling = true, EntryPoint = "DispatchMessageW", CharSet = CharSet.Auto)]
        internal static extern IntPtr IntDispatchMessage([In] ref MSG msg);

        /// <summary>
        /// DispatchMessage
        /// </summary>
        /// <param name="msg"></param>
        /// <returns></returns>
        public static IntPtr DispatchMessage(ref MSG msg)
        {
            return IntDispatchMessage(ref msg);
        }



        /// <summary>
        /// LoadCursor (internal)
        /// </summary>
        /// <param name="hInst">hInst</param>
        /// <param name="iconId">iconId</param>
        [DllImport(ExternDll.User32, ExactSpelling = true, EntryPoint = "LoadCursorW", CharSet = CharSet.Auto)]
        internal static extern IntPtr IntLoadCursor(HandleRef hInst, int iconId);

        /// <summary>
        /// LoadCursor
        /// </summary>
        /// <param name="hInst">hInst</param>
        /// <param name="iconId">iconId</param>
        public static IntPtr LoadCursor(HandleRef hInst, int iconId)
        {
            return IntLoadCursor(hInst, iconId);
        }

        /// <summary>
        /// LoadCursorFromFile (internal)
        /// </summary>
        /// <param name="lpFilename">lpFilename</param>
        [DllImport(ExternDll.User32, ExactSpelling = true, EntryPoint = "LoadCursorFromFileW", CharSet = CharSet.Auto)]
        internal static extern IntPtr IntLoadCursorFromFile(string lpFilename);

        /// <summary>
        /// LoadCursorFromFile
        /// </summary>
        /// <param name="lpFilename">lpFilename</param>
        public static IntPtr LoadCursorFromFile(string lpFilename)
        {
            return IntLoadCursorFromFile(lpFilename);
        }

        /// <summary>
        /// DestroyCursor (internal)
        /// </summary>
        /// <param name="hCurs">hCurs</param>
        [DllImport(ExternDll.User32, ExactSpelling = true, EntryPoint = "DestroyCursor", CharSet = CharSet.Auto)]
        internal static extern bool IntDestroyCursor(IntPtr hCurs);

        /// <summary>
        /// DestroyCursor
        /// </summary>
        /// <param name="hCurs">hCurs</param>
        public static bool DestroyCursor(IntPtr hCurs)
        {
            return IntDestroyCursor(hCurs);
        }


        /// <summary>
        /// Loads a new input locale identifier (formerly called the
        /// keyboard layout) into the system.
        /// </summary>
        [DllImport(ExternDll.User32, ExactSpelling = true, EntryPoint = "LoadKeyboardLayout", CharSet = CharSet.Auto)]
        internal static extern IntPtr IntLoadKeyboardLayout(string pwszKLID, int flags);

        /// <summary>
        /// Loads a new input locale identifier (formerly called the
        /// keyboard layout) into the system.
        /// </summary>
        /// <param name="pwszKLID">A string composed of the hexadecimal value of the Language Identifier (low word) and a device identifier (high word).</param>
        /// <param name="flags">Specifies how the input locale identifier is to be loaded.</param>
        /// <returns>
        /// The input locale identifier to the locale matched with the
        /// requested name. If no matching locale is available, the return
        /// value is IntPtr.Zero.
        /// </returns>
        public static IntPtr LoadKeyboardLayout(string pwszKLID, int flags)
        {
            return IntLoadKeyboardLayout(pwszKLID, flags);
        }

        /// <summary>
        /// Sets the input locale identifier (formerly called the keyboard
        /// layout handle) for the calling thread or the current process.
        /// </summary>
        [DllImport(ExternDll.User32, ExactSpelling = true, EntryPoint = "ActivateKeyboardLayout", CharSet = CharSet.Auto, SetLastError = true)]
        internal static extern IntPtr IntActivateKeyboardLayout(IntPtr hkl, int uFlags);

        /// <summary>
        /// Sets the input locale identifier (formerly called the keyboard
        /// layout handle) for the calling thread or the current process.
        /// </summary>
        /// <param name="hkl">Input locale identifier to be activated.</param>
        /// <param name="flags">Specifies how the input locale identifier is to be activated.</param>
        /// <returns>
        /// If the function succeeds, the return value is the previous input
        /// locale identifier. Otherwise, it is IntPtr.Zero.
        /// </returns>
        public static IntPtr ActivateKeyboardLayout(IntPtr hkl, int flags)
        {
            return IntActivateKeyboardLayout(hkl, flags);
        }


        /// <summary>
        /// GetDesktopWindow (internal)
        /// </summary>
        [DllImport(ExternDll.User32, ExactSpelling = true, EntryPoint = "GetDesktopWindow", CharSet = CharSet.Auto)]
        internal static extern IntPtr IntGetDesktopWindow();

        /// <summary>
        /// GetDesktopWindow
        /// </summary>
        public static IntPtr GetDesktopWindow()
        {
            return IntGetDesktopWindow();
        }

        /// <summary>
        /// FindWindowEx (internal)
        /// </summary>
        [DllImport(ExternDll.User32, ExactSpelling = true, EntryPoint = "FindWindowExW", CharSet = CharSet.Auto)]
        internal static extern IntPtr IntFindWindowEx(IntPtr hwndParent, IntPtr hwndChildAfter, string className, string wndName);

        /// <summary>
        /// FindWindowEx
        /// </summary>
        public static IntPtr FindWindowEx(IntPtr hwndParent, IntPtr hwndChildAfter, string className, string wndName)
        {
            return IntFindWindowEx(hwndParent, hwndChildAfter, className, wndName);
        }

        /// <summary>
        /// GetWindowText (internal)
        /// </summary>
        [DllImport(ExternDll.User32, ExactSpelling = true, EntryPoint = "GetWindowTextW", CharSet = CharSet.Auto)]
        internal static extern int IntGetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

        /// <summary>
        /// GetWindowText
        /// </summary>
        public static int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount)
        {
            return IntGetWindowText(hWnd, lpString, nMaxCount);
        }


        /// <summary>
        /// TrackPopupMenu (internal)
        /// </summary>
        [DllImport(ExternDll.User32, ExactSpelling = true, EntryPoint = "TrackPopupMenu", CharSet = CharSet.Auto)]
        internal static extern bool IntTrackPopupMenu(HandleRef HMENU, int uFlags, int x, int y, int nReseverd, HandleRef HWND, HandleRef prcRect);


        /// <summary>
        /// TrackPopupMenu
        /// </summary>
        public static bool TrackPopupMenu(HandleRef HMENU, int uFlags, int x, int y, int nReseverd, HandleRef HWND, HandleRef prcRect)
        {
            return IntTrackPopupMenu(HMENU, uFlags, x, y, nReseverd, HWND, prcRect);
        }



        /// <summary>
        /// TranslateAccelerator (internal)
        /// </summary>
        [DllImport(ExternDll.User32, ExactSpelling = true, EntryPoint = "TranslateAccelerator", CharSet = CharSet.Auto, SetLastError = true)]
        internal static extern int IntTranslateAccelerator(HandleRef Hwnd, HandleRef HACCEL, [In, Out] ref  MSG Msg);


        /// <summary>
        /// TranslateAccelerator
        /// </summary>
        public static int TranslateAccelerator(HandleRef Hwnd, HandleRef HACCEL, ref MSG Msg)
        {
            return IntTranslateAccelerator(Hwnd, HACCEL, ref Msg);
        }



        /// <summary>
        /// CreatePopupMenu (internal)
        /// </summary>        
        [DllImport(ExternDll.User32, ExactSpelling = true, EntryPoint = "CreatePopupMenu", CharSet = CharSet.Auto)]
        internal static extern IntPtr IntCreatePopupMenu();

        /// <summary>
        /// CreatePopupMenu
        /// </summary>
        public static IntPtr CreatePopupMenu()
        {
            return HandleCollector.Add(IntCreatePopupMenu(), CommonHandles.Menu);
        }


        /// <summary>
        /// DestroyMenu (internal)
        /// </summary>
        [DllImport(ExternDll.User32, ExactSpelling = true, EntryPoint = "DestroyMenu", CharSet = CharSet.Auto)]
        internal static extern bool IntDestroyMenu(HandleRef hMenu);


        /// <summary>
        /// DestroyMenu
        /// </summary>
        public static bool DestroyMenu(HandleRef hMenu)
        {
            HandleCollector.Remove((IntPtr)hMenu, CommonHandles.Menu);
            return IntDestroyMenu(hMenu);
        }



        /// <summary>
        /// InsertMenuItem (internal)
        /// </summary>
        [DllImport(ExternDll.User32, ExactSpelling = true, EntryPoint = "InsertMenuItem", CharSet = CharSet.Auto)]
        extern static bool IntInsertMenuItem(HandleRef hMenu, int uItem, bool fByPosition, NativeStructs.MENUITEMINFO_T lpmii);

        /// <summary>
        /// InsertMenuItem
        /// </summary>       
        public static bool InsertMenuItem(HandleRef hMenu, int uItem, bool fByPosition, NativeStructs.MENUITEMINFO_T lpmii)
        {
            return IntInsertMenuItem(hMenu, uItem, fByPosition, lpmii);
        }

        /// <summary>
        /// GetMenuItemInfo (internal)
        /// </summary>
        [DllImport(ExternDll.User32, CharSet = CharSet.Auto)]
        static extern bool GetMenuItemInfo(HandleRef hMenu, int uItem, bool fByPosition, [In, Out] NativeStructs.MENUITEMINFO_T lpmii);

        /// <summary>
        /// SetMenuItemInfo (internal)
        /// </summary>
        [DllImport(ExternDll.User32, CharSet = CharSet.Auto)]
        extern static bool SetMenuItemInfo(HandleRef hMenu, int uItem, bool fByPosition, NativeStructs.MENUITEMINFO_T lpmii);

        /// <summary>
        /// RegisterWindowMessage (internal)
        /// </summary>
        [DllImport(ExternDll.User32, ExactSpelling = true, EntryPoint = "RegisterWindowMessageW", CharSet = CharSet.Auto)]
        internal static extern int IntRegisterWindowMessage(string name);


        /// <summary>
        /// RegisterWindowMessage
        /// </summary>
        public static int RegisterWindowMessage(string name)
        {
            return IntRegisterWindowMessage(name);
        }


        /// <summary>
        /// GetWindowLong Win32 API
        /// </summary>

        [DllImport(ExternDll.User32, EntryPoint = "GetWindowLongPtrW", CharSet = CharSet.Auto)]
        internal static extern IntPtr IntGetWindowLongPtr(IntPtr hWnd, int nIndex);

        [DllImport(ExternDll.User32, EntryPoint = "GetWindowLong", CharSet = CharSet.Auto)]
        internal static extern IntPtr IntGetWindowLong(IntPtr hWnd, int nIndex);

        /// <summary>
        /// GetWindowLong Win32 API
        /// </summary>
        public static IntPtr GetWindowLong(IntPtr hWnd, int nIndex)
        {
            if (IntPtr.Size == 4)
            {
                return IntGetWindowLong(hWnd, nIndex);
            }
            else
            {
                return IntGetWindowLongPtr(hWnd, nIndex);
            }
        }


        /// <summary>
        /// GetWindowLongWndProc Win32 API
        /// </summary>

        [DllImport(ExternDll.User32, EntryPoint = "GetWindowLongPtrW", CharSet = CharSet.Auto)]
        internal static extern NativeStructs.WndProc IntGetWindowLongPtrWndProc(IntPtr hWnd, int nIndex);

        [DllImport(ExternDll.User32, EntryPoint = "GetWindowLong", CharSet = CharSet.Auto)]
        internal static extern NativeStructs.WndProc IntGetWindowLongWndProc(IntPtr hWnd, int nIndex);

        /// <summary>
        /// GetWindowLongWndProc Win32 API
        /// </summary>
        public static NativeStructs.WndProc GetWindowLongWndProc(IntPtr hWnd, int nIndex)
        {
            if (IntPtr.Size == 4)
            {
                return IntGetWindowLongWndProc(hWnd, nIndex);
            }
            else
            {
                return IntGetWindowLongPtrWndProc(hWnd, nIndex);
            }
        }


        /// <summary>
        /// SetWindowLong Win32 API
        /// </summary>

        [DllImport(ExternDll.User32, EntryPoint = "SetWindowLongPtrW", CharSet = CharSet.Auto)]
        internal static extern IntPtr IntSetWindowLongPtr(IntPtr hWnd, int nIndex, IntPtr dwNewLong);

        [DllImport(ExternDll.User32, EntryPoint = "SetWindowLong", CharSet = CharSet.Auto)]
        internal static extern IntPtr IntSetWindowLong(IntPtr hWnd, int nIndex, IntPtr dwNewLong);

        /// <summary>
        /// SetWindowLong Win32 API
        /// </summary>
        public static IntPtr SetWindowLong(IntPtr hWnd, int nIndex, IntPtr dwNewLong)
        {
            if (IntPtr.Size == 4)
            {
                return IntSetWindowLong(hWnd, nIndex, dwNewLong);
            }
            else
            {
                return IntSetWindowLongPtr(hWnd, nIndex, dwNewLong);
            }
        }


        /// <summary>
        /// SetWindowLong Win32 API
        /// </summary>

        [DllImport(ExternDll.User32, EntryPoint = "SetWindowLongPtrW", CharSet = CharSet.Auto)]
        internal static extern IntPtr IntSetWindowLongPtr(IntPtr hWnd, int nIndex, NativeStructs.WndProc dwNewLong);

        [DllImport(ExternDll.User32, EntryPoint = "SetWindowLong", CharSet = CharSet.Auto)]
        internal static extern IntPtr IntSetWindowLong(IntPtr hWnd, int nIndex, NativeStructs.WndProc dwNewLong);

        /// <summary>
        /// SetWindowLong Win32 API
        /// </summary>
        public static IntPtr SetWindowLong(IntPtr hWnd, int nIndex, NativeStructs.WndProc dwNewLong)
        {
            if (IntPtr.Size == 4)
            {
                return IntSetWindowLong(hWnd, nIndex, dwNewLong);
            }
            else
            {
                return IntSetWindowLongPtr(hWnd, nIndex, dwNewLong);
            }
        }



        /// <summary>
        /// IsWindowUnicode Win32 API
        /// </summary>
        [DllImport(ExternDll.User32, ExactSpelling = true, EntryPoint = "IsWindowUnicode", CharSet = System.Runtime.InteropServices.CharSet.Auto)]
        internal static extern bool IntIsWindowUnicode(IntPtr hWnd);

        /// <summary>
        /// IsWindowUnicode Win32 API
        /// </summary>
        public static bool IsWindowUnicode(IntPtr hWnd)
        {
            return IntIsWindowUnicode(hWnd);
        }

        /// <summary>
        /// GetProcAddress Win32 API
        /// </summary>
        [DllImport(ExternDll.Kernel32, ExactSpelling = true, EntryPoint = "GetProcAddress", CharSet = CharSet.Ansi)]
        internal static extern IntPtr IntGetProcAddress(IntPtr hModule, string lpProcName);

        /// <summary>
        /// GetProcAddress Win32 API
        /// </summary>
        public static IntPtr GetProcAddress(IntPtr hModule, string lpProcName)
        {
            return IntGetProcAddress(hModule, lpProcName);
        }


        /// <summary>
        /// Translates a character to the corresponding virtual-key code and shift state,
        /// using the input language and physical keyboard layout identified by
        /// the input locale identifier.
        /// </summary>
        /// <param name='ch'>Specifies the character to be translated into a virtual-key code.</param>
        /// <param name='hkl'>
        /// Input locale identifier used to translate the character.
        /// This parameter can be any input locale identifier previously
        /// returned by the LoadKeyboardLayout function.
        /// </param>
        /// <returns>If the function succeeds, the low-order byte of the return value
        /// contains the virtual-key code and the high-order byte contains the shift state
        /// (1=shift, 2=ctrl, 4=alt, 8=hankaku, 16 and 32 reserved).</returns>
        [DllImport(ExternDll.User32, ExactSpelling = true, EntryPoint = "VkKeyScanEx", CharSet = CharSet.Auto)]
        internal static extern short IntVkKeyScanEx(char ch, IntPtr hkl);


        /// <summary>
        /// Translates a character to the corresponding virtual-key code and shift state,
        /// using the input language and physical keyboard layout identified by
        /// the input locale identifier.
        /// </summary>
        /// <param name='ch'>Specifies the character to be translated into a virtual-key code.</param>
        /// <param name='hkl'>
        /// Input locale identifier used to translate the character.
        /// This parameter can be any input locale identifier previously
        /// returned by the LoadKeyboardLayout function.
        /// </param>
        /// <returns>If the function succeeds, the low-order byte of the return value
        /// contains the virtual-key code and the high-order byte contains the shift state
        /// (1=shift, 2=ctrl, 4=alt, 8=hankaku, 16 and 32 reserved).</returns>
        public static short VkKeyScanEx(char ch, IntPtr hkl)
        {
            return IntVkKeyScanEx(ch, hkl);
        }




        /// <summary>
        /// We have this wrapper because casting IntPtr to int may
        /// generate OverflowException when one of high 32 bits is set.
        /// </summary>
        public static int IntPtrToInt32(IntPtr intPtr)
        {
            return unchecked((int)intPtr.ToInt64());
        }


        /// <summary>
        /// SignedHIWORD
        /// </summary>
        public static int SignedHIWORD(IntPtr intPtr)
        {
            return SignedHIWORD(IntPtrToInt32(intPtr));
        }

        /// <summary>
        /// SignedHIWORD
        /// </summary>
        public static int SignedHIWORD(int n)
        {
            int i = (int)(short)((n >> 16) & 0xffff);

            return i;
        }

        /// <summary>
        /// SignedLOWORD
        /// </summary>
        public static int SignedLOWORD(IntPtr intPtr)
        {
            return SignedLOWORD(IntPtrToInt32(intPtr));
        }

        /// <summary>
        /// SignedLOWORD
        /// </summary>
        public static int SignedLOWORD(int n)
        {
            int i = (int)(short)(n & 0xFFFF);

            return i;
        }




        [DllImport(ExternDll.User32, EntryPoint = "CreateAcceleratorTableW", CharSet = CharSet.Auto)]
        private static extern IntPtr IntCreateAcceleratorTable(HandleRef pentries, int cCount);

        /// <summary>
        /// 
        /// </summary>
        public static IntPtr CreateAcceleratorTable(HandleRef pentries, int cCount)
        {
            return HandleCollector.Add(IntCreateAcceleratorTable(pentries, cCount), CommonHandles.Accelerator);
        }



        [DllImport(ExternDll.User32, ExactSpelling = true, EntryPoint = "DestroyAcceleratorTable", CharSet = CharSet.Auto)]
        internal static extern bool IntDestroyAcceleratorTable(HandleRef hAccel);

        /// <summary>
        /// 
        /// </summary>
        public static bool DestroyAcceleratorTable(HandleRef hAccel)
        {
            HandleCollector.Remove((IntPtr)hAccel, CommonHandles.Accelerator);
            return IntDestroyAcceleratorTable(hAccel);
        }


        /// <summary>
        /// 
        /// </summary>
        [DllImport(ExternDll.Comdlg32, ExactSpelling = true, EntryPoint = "GetOpenFileNameW", SetLastError = true, CharSet = CharSet.Auto)]
        internal static extern bool IntGetOpenFileName([In, Out] NativeStructs.OPENFILENAME_I ofn);


        /// <summary>
        /// 
        /// </summary>
        public static bool GetOpenFileName(NativeStructs.OPENFILENAME_I ofn)
        {
            return IntGetOpenFileName(ofn);
        }


        /// <summary>
        /// 
        /// </summary>
        [DllImport(ExternDll.Comdlg32, ExactSpelling = true, EntryPoint = "GetSaveFileNameW", SetLastError = true, CharSet = CharSet.Auto)]
        internal static extern bool IntGetSaveFileName([In, Out] NativeStructs.OPENFILENAME_I ofn);


        /// <summary>
        /// 
        /// </summary>
        public static bool GetSaveFileName(NativeStructs.OPENFILENAME_I ofn)
        {
            return IntGetSaveFileName(ofn);
        }



        /// <summary>
        /// 
        /// </summary>
        [DllImport(ExternDll.Comdlg32, ExactSpelling = true, EntryPoint = "CommDlgExtendedError", SetLastError = true, CharSet = System.Runtime.InteropServices.CharSet.Auto)]
        internal static extern int IntCommDlgExtendedError();



        /// <summary>
        /// 
        /// </summary>
        public static int CommDlgExtendedError()
        {
            return IntCommDlgExtendedError();
        }


        /// <summary>
        /// 
        /// </summary>
        [DllImport(ExternDll.Kernel32, EntryPoint = "Wow64RevertWow64FsRedirection", ExactSpelling = true, CharSet = CharSet.Auto, SetLastError = true)]
        internal static extern bool IntWow64RevertWow64FsRedirection(out IntPtr oldValue);

        /// <summary>
        /// 
        /// </summary>
        public static bool Wow64RevertWow64FsRedirection(out IntPtr oldValue)
        {
            return IntWow64RevertWow64FsRedirection(out oldValue);
        }

        [DllImport(ExternDll.Kernel32, EntryPoint = "Wow64DisableWow64FsRedirection", ExactSpelling = true, CharSet = CharSet.Auto, SetLastError = true)]
        internal static extern bool IntWow64DisableWow64FsRedirection(out IntPtr oldValue);


        /// <summary>
        /// 
        /// </summary>
        public static bool Wow64DisableWow64FsRedirection(out IntPtr oldValue)
        {
            return IntWow64DisableWow64FsRedirection(out oldValue);
        }

        [DllImport(ExternDll.Kernel32, EntryPoint = "GetShortPathNameW", ExactSpelling = true, CharSet = CharSet.Auto, SetLastError = true)]
        internal static extern int IntGetShortPathName([MarshalAs(UnmanagedType.LPTStr)]string lpszLongPath, [MarshalAs(UnmanagedType.LPTStr)]StringBuilder lpszShortPath, int cchBuffer);

        /// <summary>
        /// 
        /// </summary>
        public static int GetShortPathName(string lpszLongPath, StringBuilder lpszShortPath, int cchBuffer)
        {
            return IntGetShortPathName(lpszLongPath, lpszShortPath, cchBuffer);
        }

        [DllImport(ExternDll.Kernel32)]
        public static extern IntPtr LoadLibrary(string lpFileName);

        [DllImport(ExternDll.Kernel32, SetLastError = true)]
        public static extern bool FreeLibrary(IntPtr hModule);
    }


    /// <summary>
    /// This implementation wraps a buffer for char on Win32 world. The code was taken from Winforms
    /// </summary>
    public abstract class CharBuffer
    {


        /// <summary>
        /// 
        /// </summary>
        public static CharBuffer CreateBuffer(int size)
        {
            if (Marshal.SystemDefaultCharSize == 1)
            {
                return new AnsiCharBuffer(size);
            }
            return new UnicodeCharBuffer(size);
        }

        /// <summary>
        /// 
        /// </summary>
        public abstract IntPtr AllocCoTaskMem();

        /// <summary>
        /// 
        /// </summary>
        public abstract string GetString();

        /// <summary>
        /// 
        /// </summary>
        public abstract void PutCoTaskMem(IntPtr ptr);

        /// <summary>
        /// 
        /// </summary>
        public abstract void PutString(string s);
    }

    /// <summary>
    ///     This implementation wraps a buffer for Ansi on Win32 world. The code was taken from Winforms
    /// </summary>
    [SuppressUnmanagedCodeSecurity()]
    internal class AnsiCharBuffer : CharBuffer
    {
        /// <summary>
        /// 
        /// </summary>
        internal byte[] buffer;

        /// <summary>
        /// 
        /// </summary>        
        internal int offset;

        /// <summary>
        /// 
        /// </summary>
        public AnsiCharBuffer(int size)
        {
            buffer = new byte[size];
        }

        /// <summary>
        /// 
        /// </summary>
        public override IntPtr AllocCoTaskMem()
        {
            IntPtr result = Marshal.AllocCoTaskMem(buffer.Length);
            Marshal.Copy(buffer, 0, result, buffer.Length);
            return result;
        }


        /// <summary>
        /// 
        /// </summary>
        public override string GetString()
        {
            int i = offset;
            while (i < buffer.Length && buffer[i] != 0)
                i++;
            string result = Encoding.Default.GetString(buffer, offset, i - offset);
            if (i < buffer.Length)
                i++;
            offset = i;
            return result;
        }

        /// <summary>
        /// 
        /// </summary>
        public override void PutCoTaskMem(IntPtr ptr)
        {
            Marshal.Copy(ptr, buffer, 0, buffer.Length);
            offset = 0;
        }

        /// <summary>
        /// 
        /// </summary>
        public override void PutString(string s)
        {

            byte[] bytes = Encoding.Default.GetBytes(s);
            int count = Math.Min(bytes.Length, buffer.Length - offset);
            Array.Copy(bytes, 0, buffer, offset, count);
            offset += count;
            if (offset < buffer.Length) buffer[offset++] = 0;
        }
    }


    /// <summary>
    /// This implementation wraps a buffer for Unicode on Win32 world. The code was taken from Winforms
    /// </summary>
    [SuppressUnmanagedCodeSecurity()]
    internal class UnicodeCharBuffer : CharBuffer
    {

        /// <summary>
        /// 
        /// </summary>
        internal char[] buffer;

        /// <summary>
        /// 
        /// </summary>
        internal int offset;

        /// <summary>
        /// 
        /// </summary>
        public UnicodeCharBuffer(int size)
        {
            buffer = new char[size];
        }

        /// <summary>
        /// 
        /// </summary>
        public override IntPtr AllocCoTaskMem()
        {
            IntPtr result = Marshal.AllocCoTaskMem(buffer.Length * 2);
            Marshal.Copy(buffer, 0, result, buffer.Length);
            return result;
        }

        /// <summary>
        /// 
        /// </summary>
        public override String GetString()
        {
            int i = offset;
            while (i < buffer.Length && buffer[i] != 0) i++;
            string result = new string(buffer, offset, i - offset);
            if (i < buffer.Length) i++;
            offset = i;
            return result;
        }

        /// <summary>
        /// 
        /// </summary>
        public override void PutCoTaskMem(IntPtr ptr)
        {
            Marshal.Copy(ptr, buffer, 0, buffer.Length);
            offset = 0;
        }

        /// <summary>
        /// 
        /// </summary>
        public override void PutString(string s)
        {
            int count = Math.Min(s.Length, buffer.Length - offset);
            s.CopyTo(0, buffer, offset, count);
            offset += count;
            if (offset < buffer.Length) buffer[offset++] = (char)0;
        }
    }

}

