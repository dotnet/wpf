// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Windows;
using Microsoft.Test.Diagnostics;
using Microsoft.Test.Win32;

namespace Microsoft.Test.Display
{
    /// <summary>
    /// Class encapsulating Display settings information and Monitor information
    /// </summary>
    public class Monitor
    {
        #region Private data

        private static Dictionary<string, User32.MONITORINFOEX> _monitorInfo = new Dictionary<string,User32.MONITORINFOEX>();
        private static Dictionary<string, IntPtr> _monitorHandle = new Dictionary<string, IntPtr>();
        private static AdapterIdentifier adaptorIdentifier;

        private User32.DISPLAY_DEVICE _displayDevice;
        private DisplaySettings _displaySettings = null;
        private NativeStructs.RECT _area;
        private NativeStructs.RECT _workingArea;

        #endregion Private data


        #region Properties        

        /// <summary>Return the device setting resolution (Dot Per Inch)</summary>
        public static NativeStructs.POINT Dpi
        {
            get
            {
                NativeStructs.POINT retVal = new NativeStructs.POINT();
                IntPtr hDC = IntPtr.Zero;
                try
                {
                    hDC = User32.GetWindowDC(IntPtr.Zero);
                    if (hDC == IntPtr.Zero) { throw new Win32Exception(); }
                    retVal.x = Gdi32.GetDeviceCaps(hDC, Gdi32.LOGPIXELSX);
                    retVal.y = Gdi32.GetDeviceCaps(hDC, Gdi32.LOGPIXELSY);
                }
                finally
                {
                    if (hDC != IntPtr.Zero) { User32.ReleaseDC(IntPtr.Zero, hDC); }
                }
                return retVal;
            }
        }

        /// <summary>Get the device name</summary>
        public string DeviceName
        {
            get { return _displayDevice.DeviceName; }
        }

        /// <summary>Get the name of the video adapter</summary>
        public string Description
        {
            get { return _displayDevice.DeviceString; }
        }

        /// <summary>Determin if this monitor is the primary monitor</summary>
        public bool IsPrimary
        {
            get { return (_displayDevice.StateFlags & User32.DISPLAY_DEVICE_PRIMARY_DEVICE) != 0; }
        }

        /// <summary>Access the display settings</summary>
        public DisplaySettings DisplaySettings
        {
            get { return _displaySettings; }
        }

        /// <summary>Get the Monitor area</summary>
        public NativeStructs.RECT Area
        {
            get { return _area; }
        }

        /// <summary>Get the Monitor working area</summary>
        public NativeStructs.RECT WorkingArea
        {
            get { return _workingArea; }
        }

        /// <summary>
        /// Get the HMONITOR handle associated with this monitor
        /// </summary>
        public IntPtr Handle
        {
            get { return _monitorHandle[DeviceName]; }
        }

        #endregion Properties


        #region Constructors

        private Monitor(User32.DISPLAY_DEVICE displayDevice)
        {
            // Check params in debug build ?

            _displayDevice = displayDevice;
            _displaySettings = new DisplaySettings(_displayDevice.DeviceName);
        }

        #endregion Constructors


        #region Public methods

        /// <summary>
        /// Convert screen pixel to Avalon Logical pixel.
        /// </summary>
        /// <param name="dimension"></param>
        /// <param name="value"></param>
        /// <returns>logical width or height</returns>
        public static double ConvertScreenToLogical(Dimension dimension, double value)
        {
            switch (dimension)
            {
                case Dimension.Width:
                    return value * 96.0 / Dpi.x;
                case Dimension.Height:
                    return value * 96.0 / Dpi.y;
                default:
                    throw new System.ComponentModel.InvalidEnumArgumentException("dimension", (int)dimension, typeof(Dimension));
            }
        }

        /// <summary>
        /// Convert Avalon Logical pixel to screen pixel.
        /// </summary>
        /// <param name="dimension"></param>
        /// <param name="value"></param>
        /// <returns>logical width or height</returns>
        public static double ConvertLogicalToScreen(Dimension dimension, double value)
        {
            switch (dimension)
            {
                case Dimension.Width:
                    return value * Dpi.x / 96.0;
                case Dimension.Height:
                    return value * Dpi.y / 96.0;
                default:
                    throw new System.ComponentModel.InvalidEnumArgumentException("dimension", (int)dimension, typeof(Dimension));
            }
        }



        /// <summary>
        /// Returns the number of display monitors available
        /// </summary>
        /// <returns></returns>
        public static int GetDisplayCount()
        {
            return SystemMetrics.GetSystemMetric(SystemMetric.Monitors);
        }

        /// <summary>
        /// Returns true if multi-mon is supported
        /// </summary>
        /// <returns></returns>
        public static bool IsMultiMonAvailable()
        {
            return GetDisplayCount() > 1;
        }

        /// <summary>Get all monitors attached to this box and recognized by window</summary>
        public static Monitor[] GetAllEnabled()
        {
            return GetAllAttached(false);
        }

        /// <summary>Get all monitors attached to this box</summary>
        public static Monitor[] GetAllAttached(bool getDisabledMonitors)
        {
            List<Monitor> retVal = new List<Monitor>();
            bool success = true;
            uint index = 0;
            while (success)
            {
                User32.DISPLAY_DEVICE displayDevice = new User32.DISPLAY_DEVICE();
                displayDevice.cb = Marshal.SizeOf(displayDevice);
                success = User32.EnumDisplayDevices(null, index++, ref displayDevice, 0);
                if (success)
                {
                    if ((displayDevice.StateFlags & User32.DISPLAY_DEVICE_ATTACHED_TO_DESKTOP) != 0)
                    {
                        retVal.Add(new Monitor(displayDevice));
                    }
                    else if ((displayDevice.StateFlags & User32.DISPLAY_DEVICE_MIRRORING_DRIVER) == 0) 
                    {
                        if (getDisabledMonitors) 
                        {
                            // Check to see if the display isn't a mirroring device, but 
                            // has a monitor attached.  This would represent a currently
                            // inactive monitor
                            User32.DISPLAY_DEVICE monitorInfo = new User32.DISPLAY_DEVICE();
                            monitorInfo.cb = Marshal.SizeOf(displayDevice);
                            success = User32.EnumDisplayDevices(null, 0, ref monitorInfo, 0);
                            if (success) { retVal.Add(new Monitor(displayDevice)); }
                        }
                    }
                }
            }

            _monitorInfo.Clear();
            _monitorHandle.Clear();
            if ( ! User32.EnumDisplayMonitors(IntPtr.Zero, IntPtr.Zero, new User32.EnumMonitorsDelegate(EnumMonitorCallback), IntPtr.Zero))
            {
                throw new Win32Exception("EnumDisplayMonitors");
            }
            for (int t = 0; t < retVal.Count; t++)
            { 
                User32.MONITORINFOEX monitorInfo;
                if( ! _monitorInfo.TryGetValue(retVal[t].DeviceName, out monitorInfo)) {continue;}
                retVal[t]._area = monitorInfo.rcMonitor;
                retVal[t]._workingArea = monitorInfo.rcWork;
            }

            return retVal.ToArray();
        }

        /// <summary>Get the primary monitor</summary>
        public static Monitor GetPrimary()
        {
            Monitor retVal = null;
            Monitor[] monitors = GetAllEnabled();
            // Primary should always be the first one
            // but not doced so it's an internal feature that can change on next release
            for (int t = 0; t < monitors.Length; t++)
            {
                if (monitors[t].IsPrimary) 
                { 
                    retVal = monitors[t];
                    break;
                }
            }
            if (retVal == null) { throw new ApplicationException("Internal Error (No primary monitor found)"); }
            return retVal;
        }

        /// <summary>
        /// Get the Monitor hosting the window (see msnd for special cases like spanning across multiple monitors).
        /// </summary>
        /// <param name="hwnd">The HWND of the window</param>
        /// <returns>The Monitor object associate with this window</returns>
        public static Monitor MonitorFromWindow(IntPtr hwnd)
        {
            // TODO : check if handle is non Null & a Valid hwnd & Visible

            IntPtr hmonitor = User32.MonitorFromWindow(hwnd, User32.MONITOR_DEFAULTTONULL);
            Monitor[] monitors = Monitor.GetAllEnabled();
            for (int t = 0; t < monitors.Length;t++ )
            {
                if (monitors[t].Handle == hmonitor) { return monitors[t]; }
            }
            throw new InvalidOperationException("hwnd not located on any monitor");
        }


        /// <summary>
        /// Primary Display Adaptor Description
        /// </summary>
        /// <returns></returns>
        public static string PrimaryAdaptorDescription
        {
            get
            {
                if (String.IsNullOrEmpty(adaptorIdentifier.DeviceName))
                {
                    Direct3D9 d3d9 = Direct3D9.CreateInstance();
                    adaptorIdentifier = d3d9.GetAdapterIdentifier();
                }
                return adaptorIdentifier.Description;
            }
        }

        /// <summary>
        /// Given a window object, this method will reposition and resize the window such that it
        /// would span multiple monitors. If multi-mon is not present, it will keep the window at
        /// its original size but positions it such that it is centered in the primary screen.
        /// </summary>
        /// <param name="window">The window object</param>
        public static void SetWindowPositionAndSize(Window window)
        {
            Monitor[] monitors = GetAllEnabled();

            // Get the monitor that is farthest away from the primary monitor
            Monitor primaryMonitor = GetPrimary();
            Monitor farthestMonitor = null;

            // middle of the screen for the primary monitor
            Vector midPrimaryVector = new Vector((primaryMonitor.Area.left + primaryMonitor.Area.Width / 2),
                                                 (primaryMonitor.Area.top + primaryMonitor.Area.Height / 2));
            Vector farthestVector = new Vector(0, 0); // vector from middle of the primary to middle of the farthest monitor
            double farthestDistance = 0;

            foreach (Monitor monitor in monitors)
            {
                if (monitor.IsPrimary) continue;

                // Get the distance from the center of the primary monitor to the center of this monitor
                double dx = (monitor.Area.left + monitor.Area.Width / 2) - midPrimaryVector.X;
                double dy = (monitor.Area.top + monitor.Area.Height / 2) - midPrimaryVector.Y;
                double distance = dx * dx + dy * dy;

                if (distance > farthestDistance)
                {
                    farthestDistance = distance;
                    farthestMonitor = monitor;
                    farthestVector.X = dx;
                    farthestVector.Y = dy;
                }
            }

            // Resize the window such that it would span the two monitors (if any)
            window.Height = Math.Max(Math.Abs(farthestVector.Y), window.Height);
            window.Width = Math.Max(Math.Abs(farthestVector.X), window.Width);

            window.Left = midPrimaryVector.X;
            window.Top = midPrimaryVector.Y;

            if (monitors.Length == 1)
            {
                window.Left -= window.Width * 0.5;
                window.Top -= window.Height * 0.5;
            }
            else
            {
                // Also place the window in a position that it would span the two monitors
                if (farthestVector.X < 0)
                {
                    window.Left += farthestVector.X;
                }

                if (farthestVector.Y < 0)
                {
                    window.Top += farthestVector.Y;
                }
            }
        }

        #endregion Public methods


        #region Helper functions

        private static bool EnumMonitorCallback(IntPtr hMonitor, IntPtr hdc, ref NativeStructs.RECT rect, IntPtr dwData)
        {
            User32.MONITORINFOEX monitorInfo = new User32.MONITORINFOEX();
            monitorInfo.cbSize = Marshal.SizeOf(monitorInfo);
            if (!User32.GetMonitorInfo(hMonitor, ref monitorInfo))
            {
                throw new Win32Exception();
            }
            _monitorInfo.Add(monitorInfo.szDevice, monitorInfo);
            _monitorHandle.Add(monitorInfo.szDevice, hMonitor);

            return true;
        }

        #endregion Helper functions

        #region Private Imports


        [StructLayoutAttribute(LayoutKind.Sequential)]
        private struct AdapterIdentifier
        {
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 512)]
            public string Driver;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 512)]
            public string Description;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 512)]
            public string DeviceName;
            public Int64 DriverVersion;
            public int DriverVersionLowPart;
            public int DriverVersionHighPart;
            public int VendorId;
            public int DeviceId;
            public int SubSysId;
            public int Revision;
            public Guid DeviceIdentifier;
            public int WHQLLevel;
        }

        [StructLayoutAttribute(LayoutKind.Sequential)]
        private struct D3DCAPS8
        {
            // TODO - ( if needed one day )
        }
        [StructLayoutAttribute(LayoutKind.Sequential)]
        private struct D3DPRESENT_PARAMETERS
        {
            // TODO - ( if needed one day )
        }

        [GuidAttribute("81bdcbca-64d4-426d-ae8d-ad0147f4275c"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface IDirect3D9
        {

            // IUnknown methods (QI, AddRef, Release) not needed since InterfaceIsIUnknown is defined

            // IDirect3D8 methods
            int RegisterSoftwareDevice(IntPtr pInitializeFunction);
            uint GetAdapterCount();
            int GetAdapterIdentifier(uint adapter, int Flags, ref AdapterIdentifier pD3DAdapterIdentifier);
            uint GetAdapterModeCount(uint adapter, int d3dFormat);
            int EnumAdapterModes(uint adapter, uint mode, object pd3dDisplayMode);
            int GetAdapterDisplayMode(uint adapter, object d3dDisplayMode);
            int CheckDeviceType(uint adapter, int d3dDeviceType, int d3dDisplayFormat, int d3dBackBufferFormat, bool Windowed);
            int CheckDeviceFormat(uint adapter, int d3dDeviceType, int d3dAdapterFormat, int Usage, int d3dResourceType, int d3dCheckFormat);
            int CheckDeviceMultiSampleType(uint adapter, int d3dDeviceType, int d3dSurfaceFormat, bool Windowed, int d3dMultiSampleType);
            int CheckDepthStencilMatch(uint adapter, int d3dDeviceType, int d3dAdapterFormat, int d3dRenderTargetFormat, int d3dDepthStencilFormat);
            int GetDeviceCaps(uint adapter, int d3dDeviceType, ref D3DCAPS8 pCaps);
            IntPtr GetAdapterMonitor(uint adapter);
            int CreateDevice(uint adapter, int d3dDeviceType, IntPtr hFocusWindow, int BehaviorFlags, ref D3DPRESENT_PARAMETERS pPresentationParameters, out object ppId3d9ReturnedDeviceInterface);

            // IDirect3D9 might have more methods, haven't check.
            // Add them if needed
        }

        // Note : Class does not explicitely implement IDirect3D9, this is on purpose.
        private class Direct3D9
        {
            [DllImport("d3d9.dll", SetLastError = true, PreserveSig = true)]
            [return: MarshalAs(UnmanagedType.IUnknown)]
            private static extern object Direct3DCreate9(int flag);


            private const int D3D_SDK_VERSION = 220;
            private const int D3DENUM_NO_WHQL_LEVEL = 0x00000002;
            private const int D3DADAPTER_DEFAULT = 0;

            private static Direct3D9 _instance = null;
            private IDirect3D9 _ID3d9 = null;

            private Direct3D9(IDirect3D9 d3d9interface)
            {
                _ID3d9 = d3d9interface;
            }
            public static Direct3D9 CreateInstance()
            {
                // Warning : not safe for multithreading, update if MT needed.
                if (_instance == null)
                {
                    _instance = new Direct3D9((IDirect3D9)Direct3DCreate9(D3D_SDK_VERSION));
                }
                return _instance;
            }

            public AdapterIdentifier GetAdapterIdentifier()
            {
                // Note : 
                // * returns info only about the Primary adapter -- Might need an overloaded Method if you care about other adapters.
                // * Do not query for WHQL level to speed up call
                AdapterIdentifier retVal = new AdapterIdentifier();
                int hr = _ID3d9.GetAdapterIdentifier(D3DADAPTER_DEFAULT, D3DENUM_NO_WHQL_LEVEL, ref retVal);
                if (FAILED(hr)) { throw new ExternalException("Call to ID3D9->GetAdapterIdentifier failed."); }
                return retVal;
            }

            // COM FAILED macro
            private bool FAILED(int hresult)
            {
                return (hresult < 0);
            }
            // COM SUCCEEDED macro
            private bool SUCCEEDED(int hresult)
            {
                return !FAILED(hresult);
            }
        }

        #endregion
    }

    /// <summary>
    /// Logical dimension.
    /// </summary>
    public enum Dimension
    {
        /// <summary>
        /// Logical Width.
        /// </summary>
        Width,
        /// <summary>
        /// Logical Height.
        /// </summary>
        Height
    };
}
