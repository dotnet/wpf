// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.InteropServices;

namespace Microsoft.Test.Filtering
{
    /// <summary>
    /// A simple helper to get Monitor related information.
    /// This is similar to Desttop in TestRuntime. 
    /// </summary>
    internal class MonitorInfo
    {
        /// <summary>
        /// Return the number of monitors enabled on the system. 
        /// </summary>
        internal static int MonitorCount
        {
            get
            {
                if (monitorCount == -1)
                {
                    monitorCount = 0;
                    bool hasMoreMonitor = true;
                    uint index = 0;
                    while (hasMoreMonitor)
                    {
                        DISPLAY_DEVICE displayDevice = new DISPLAY_DEVICE();
                        displayDevice.cb = Marshal.SizeOf(displayDevice);
                        hasMoreMonitor = EnumDisplayDevices(null, index++, ref displayDevice, 0);
                        if (hasMoreMonitor && ((displayDevice.StateFlags & DISPLAY_DEVICE_ATTACHED_TO_DESKTOP) != 0))
                        {
                            monitorCount++;
                        }
                    }
                }
                return monitorCount;
            }
        }

        private static int monitorCount = -1;

        [DllImport("User32.dll", PreserveSig = true)]
        private static extern bool EnumDisplayDevices(string lpDevice, uint iDevNum, ref DISPLAY_DEVICE lpDisplayDevice, uint dwFlags);

        private const uint DISPLAY_DEVICE_ATTACHED_TO_DESKTOP = 0x00000001;

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 0)]
        private struct DISPLAY_DEVICE
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
    }
}