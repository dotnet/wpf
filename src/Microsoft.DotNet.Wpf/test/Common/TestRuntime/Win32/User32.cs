// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Security;

namespace Microsoft.Test.Win32
{
    internal static class WindowMessages
    {
        internal const int WM_DWMCOMPOSITIONCHANGED = 0x031E;
    }

    [SuppressUnmanagedCodeSecurity()]
    internal static class User32
    {
        private const string USER32DLL = "User32.dll";
        private const int CCHDEVICENAME = 32;

        internal enum ScreenOrientation
        {
            Angle0,
            Angle90,
            Angle180,
            Angle270
        }

        internal const int DISP_CHANGE_SUCCESSFUL   =  0;
        internal const int DISP_CHANGE_RESTART      =  1;
        internal const int DISP_CHANGE_FAILED       = -1;
        internal const int DISP_CHANGE_BADMODE      = -2;
        internal const int DISP_CHANGE_NOTUPDATED   = -3;
        internal const int DISP_CHANGE_BADFLAGS     = -4;
        internal const int DISP_CHANGE_BADPARAM     = -5;
        internal const int DISP_CHANGE_BADDUALVIEW  = -6;

        internal const int CDS_UPDATEREGISTRY   = 0x00000001;
        internal const int CDS_TEST             = 0x00000002;
        internal const int CDS_FULLSCREEN       = 0x00000004;
        internal const int CDS_GLOBAL           = 0x00000008;
        internal const int CDS_SET_PRIMARY      = 0x00000010;
        internal const int CDS_VIDEOPARAMETERS  = 0x00000020;
        internal const int CDS_RESET            = 0x40000000;
        internal const int CDS_NORESET          = 0x10000000;


        internal const uint ENUM_CURRENT_SETTINGS   = unchecked((uint)-1);
        internal const uint ENUM_REGISTRY_SETTINGS  = unchecked((uint)-2);

        internal const uint DISPLAY_DEVICE_ATTACHED_TO_DESKTOP  = 0x00000001;
        internal const uint DISPLAY_DEVICE_MULTI_DRIVER         = 0x00000002;
        internal const uint DISPLAY_DEVICE_PRIMARY_DEVICE       = 0x00000004;
        internal const uint DISPLAY_DEVICE_MIRRORING_DRIVER     = 0x00000008;
        internal const uint DISPLAY_DEVICE_VGA_COMPATIBLE       = 0x00000010;
        internal const uint DISPLAY_DEVICE_REMOVABLE            = 0x00000020;
        internal const uint DISPLAY_DEVICE_MODESPRUNED          = 0x08000000;
        internal const uint DISPLAY_DEVICE_REMOTE               = 0x04000000;
        internal const uint DISPLAY_DEVICE_DISCONNECT           = 0x02000000;
        internal const uint DISPLAY_DEVICE_TS_COMPATIBLE        = 0x00200000;

        internal const int MONITOR_DEFAULTTONULL    =   0x00000000;
        internal const int MONITOR_DEFAULTTOPRIMARY =   0x00000001;
        internal const int MONITOR_DEFAULTTONEAREST =   0x00000002;

        internal const uint WM_COMMAND = 0x0111;
        internal const int CBN_SELCHANGE = 1;

        [StructLayout(LayoutKind.Sequential, Pack = 0)]
        public struct DEVMODE
        {
            private const int CCHDEVICENAME = 0x20;
            private const int CCHFORMNAME = 0x20;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 0x20)]
            public string dmDeviceName;
            public short dmSpecVersion;
            public short dmDriverVersion;
            public short dmSize;
            public short dmDriverExtra;
            public int dmFields;
            public int dmPositionX;
            public int dmPositionY;
            public ScreenOrientation dmDisplayOrientation;
            public int dmDisplayFixedOutput;
            public short dmColor;
            public short dmDuplex;
            public short dmYResolution;
            public short dmTTOption;
            public short dmCollate;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 0x20)]
            public string dmFormName;
            public short dmLogPixels;
            public int dmBitsPerPel;
            public int dmPelsWidth;
            public int dmPelsHeight;
            public int dmDisplayFlags;
            public int dmDisplayFrequency;
            public int dmICMMethod;
            public int dmICMIntent;
            public int dmMediaType;
            public int dmDitherType;
            public int dmReserved1;
            public int dmReserved2;
            public int dmPanningWidth;
            public int dmPanningHeight;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 0)]
        internal struct DISPLAY_DEVICE
        {
            public int cb;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
            public string DeviceName;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
            public string DeviceString;
            public uint StateFlags;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
            public string DeviceID;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
            public string DeviceKey;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 0)]
        internal struct MONITORINFOEX
        {
            public int cbSize;
            public NativeStructs.RECT rcMonitor;
            public NativeStructs.RECT rcWork;
            public int dwFlags;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = CCHDEVICENAME)]
            public string szDevice;
        }

        [DllImport(USER32DLL)]
        internal static extern IntPtr GetForegroundWindow();

        [DllImportAttribute(USER32DLL, EntryPoint="GetClientRect", SetLastError=true)]
        internal static extern bool GetClientRect(IntPtr HWND, ref Rectangle rect);

        [DllImport(USER32DLL, ExactSpelling = true, EntryPoint = "GetSystemMetrics", CharSet = CharSet.Auto)]
        internal static extern int GetSystemMetrics(int nIndex);

        [DllImport(USER32DLL)]
        internal static extern void SetForegroundWindow(IntPtr hWnd);

        [DllImport(USER32DLL)]
        internal static extern bool EnumDisplaySettingsEx([MarshalAs(UnmanagedType.LPStr)] string deviceName, uint iModeNum, ref DEVMODE lpDevMode, uint dwFlags);

        [DllImport(USER32DLL, PreserveSig = true, SetLastError = true)]
        internal static extern int ChangeDisplaySettingsEx(string deviceName, ref DEVMODE devMode, IntPtr hwnd, uint dwFlags, IntPtr lParam);

        [DllImport(USER32DLL, PreserveSig = true, SetLastError = true, CharSet = CharSet.Ansi, EntryPoint = "EnumDisplaySettings")]
        internal static extern bool EnumDisplaySettings([MarshalAs(UnmanagedType.LPStr)]string deviceName, uint iModeNum, ref DEVMODE devMode);

        [DllImport(USER32DLL, PreserveSig = true)]
        internal static extern bool EnumDisplayDevices(string lpDevice, uint iDevNum, ref DISPLAY_DEVICE lpDisplayDevice, uint dwFlags);

        [DllImport(USER32DLL, PreserveSig = true, SetLastError = true)]
        internal static extern bool EnumDisplayMonitors(IntPtr hdc, IntPtr clippingRect, [MarshalAs(UnmanagedType.FunctionPtr)] EnumMonitorsDelegate lpfnEnum, IntPtr dwData);
        internal delegate bool EnumMonitorsDelegate(IntPtr hMonitor, IntPtr hdc, ref NativeStructs.RECT rect, IntPtr dwData);

        [DllImport(USER32DLL, PreserveSig = true, SetLastError = true)]
        internal static extern bool GetMonitorInfo( IntPtr hMonitor, ref MONITORINFOEX lpmi);

        [DllImport(USER32DLL, PreserveSig = true, SetLastError = true)]
        internal static extern IntPtr WindowFromPoint(NativeStructs.POINT Point);

        [DllImport(USER32DLL, PreserveSig = true, SetLastError = true)]
        internal static extern IntPtr GetWindowDC(IntPtr hWnd);

        [DllImportAttribute(USER32DLL, SetLastError = true)]
        internal static extern bool ClientToScreen(IntPtr HWND, ref Point pt);

        [DllImportAttribute(USER32DLL, SetLastError = true)]
        internal static extern bool IsWindow(IntPtr HWND);

        [DllImportAttribute(USER32DLL, SetLastError = true)]
        internal static extern bool IsWindowVisible(IntPtr HWND);

        [DllImportAttribute(USER32DLL, SetLastError = true)]
        internal static extern bool GetWindowRect(IntPtr HWND, ref Rectangle rect);

        [DllImportAttribute(USER32DLL, SetLastError = true)]
        internal static extern long GetWindowLongA(IntPtr HWND, int index);

        [DllImportAttribute(USER32DLL)]
        internal static extern IntPtr FindWindowEx(IntPtr HWNDparent, IntPtr HWNDafterChild, string className, string windowName);

        [DllImport(USER32DLL, PreserveSig = true)]
        internal static extern bool ReleaseDC(IntPtr hWnd, IntPtr hDC);

        [DllImport(USER32DLL, PreserveSig = true)]
        internal static extern IntPtr MonitorFromWindow(IntPtr hwnd, int monitorFromWindowFlags);

        [DllImport(USER32DLL)]
        internal static extern int SendMessage(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

        [DllImport(USER32DLL, EntryPoint = "SetProcessDPIAware", CharSet = CharSet.Auto, SetLastError = true)]
        internal static extern void SetProcessDpiAware();

        internal static IntPtr MakeWParam(int LoWord, int HiWord)
        {
            return new IntPtr((HiWord << 16) | (LoWord & 0xffff));
        }


    }
}
