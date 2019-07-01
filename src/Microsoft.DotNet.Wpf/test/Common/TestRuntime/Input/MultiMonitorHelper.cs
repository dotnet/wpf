// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;
using System.Windows;
using System.Threading;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows.Threading;
using System.Collections.Generic;
using Microsoft.Test.Win32;

namespace Microsoft.Test.Input
{
    ///<summary>
    ///</summary>
    public static class MultiMonitorHelper
    {
        ///<summary>
        ///</summary>
        public static int GetDisplayCount()
        {
            return NativeMethods.GetSystemMetrics(NativeConstants.SM_CMONITORS);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public static bool IsMultiMonAvailable()
        {
            return GetDisplayCount() != 1;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public static Size GetVirtualScreenSize()
        {
            int width = NativeMethods.GetSystemMetrics(NativeConstants.SM_CXVIRTUALSCREEN);
            int height = NativeMethods.GetSystemMetrics(NativeConstants.SM_CYVIRTUALSCREEN);

            Size size = new Size(width, height);
            
            return size;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public static Point GetVirtualScreenTopLeftPoint()
        {
            double x = NativeMethods.GetSystemMetrics(NativeConstants.SM_XVIRTUALSCREEN);
            double y = NativeMethods.GetSystemMetrics(NativeConstants.SM_YVIRTUALSCREEN);

            Point point = new Point(x, y);
            return point;
        }




        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public static NativeStructs.MONITORINFOEX[] GetAllMonitors(out NativeStructs.MONITORINFOEX primaryMonitor)
        {
            NativeStructs.RECT empty = NativeStructs.RECT.Empty;
            NativeStructs.MONITORINFOEX[] monitorArray = null;            
            lock (_globalLock)
            {
                _currentMonitors = new List<NativeStructs.MONITORINFOEX>(); ;
                NativeMethods.EnumDisplayMonitors(IntPtr.Zero, null, new NativeMethods.MonitorEnumProc(MultiMonitorHelper.MonitorEnumProcCallback), IntPtr.Zero);

                if (_currentMonitors.Count <= 0)
                {
                    throw new InvalidOperationException("No Monitor found.");
                }
                monitorArray = _currentMonitors.ToArray();
                primaryMonitor = _primaryMonitor;
            }
            return monitorArray;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="monitor"></param>
        /// <param name="hdc"></param>
        /// <param name="lprcMonitor"></param>
        /// <param name="lParam"></param>
        /// <returns></returns>
        private static bool MonitorEnumProcCallback(IntPtr monitor, IntPtr hdc, IntPtr lprcMonitor, IntPtr lParam)
        {
            NativeStructs.MONITORINFOEX info = new NativeStructs.MONITORINFOEX();
            NativeMethods.GetMonitorInfo(monitor, info);
            _currentMonitors.Add(info);

            if ((info.dwFlags & NativeConstants.MONITORINFOF_PRIMARY) == NativeConstants.MONITORINFOF_PRIMARY)
            {
                _primaryMonitor = info;
            }

            return true;
        }

        static object _globalLock = new object();
        static List<NativeStructs.MONITORINFOEX> _currentMonitors = null;
        static NativeStructs.MONITORINFOEX _primaryMonitor = null;
    }
}



