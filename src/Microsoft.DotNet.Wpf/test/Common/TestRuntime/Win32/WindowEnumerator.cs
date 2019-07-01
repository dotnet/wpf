// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Win32;

namespace Microsoft.Test.Win32
{
    //--------------------------------------------------------------

    #region HwndInfo

    /// <summary>
    /// Structure for Storing Window Handles and corresponding Process ID's
    /// </summary>
    public struct HwndInfo
    {
        #region Public Members

        /// <summary>
        /// Hwnd
        /// </summary>
        public IntPtr hWnd;
        /// <summary>
        /// Process ID of Hwnd
        /// </summary>
        public int ProcessId;

        /// <summary>
        /// Constructor 
        /// </summary>
        /// <param name="hWnd"></param>
        public HwndInfo(IntPtr hWnd)
        {
            this.hWnd = hWnd;
            GetWindowThreadProcessId(hWnd, out ProcessId);
        }

        #endregion

        #region Private Imports

        [DllImport("User32.dll")]
        private static extern int GetWindowThreadProcessId(IntPtr hWnd, out int processId);

        #endregion
    }

    #endregion

    //--------------------------------------------------------------

    #region WindowEnumerator

    /// <summary>
    /// Class for Enumerating Window Handles
    /// </summary>
    public static class WindowEnumerator
    {

        #region Public Members

        /// <summary>
        /// Returns Top Level Windows in a Process
        /// </summary>
        /// <param name="process"></param>
        /// <returns></returns>
        public static HwndInfo[] GetTopLevelVisibleWindows(Process process)
        {
            ProcessThreadCollection threads;
            try
            {
                threads = process.Threads;
            }
            catch (InvalidOperationException)
            {
                //the process has exited
                return new HwndInfo[0];
            }
            catch (Win32Exception)
            {
                //The process has already exited or access is denied (this is probobly a whidbey bug)
                return new HwndInfo[0];
            }

            List<HwndInfo> list = new List<HwndInfo>();
            foreach (ProcessThread thread in process.Threads)
            {
                EnumThreadWindows(thread.Id, delegate(IntPtr hWnd, IntPtr lParam)
                {
                    if (IsWindowVisible(hWnd))
                        list.Add(new HwndInfo(hWnd));
                    return true;
                }, IntPtr.Zero);
            }

            return list.ToArray();
        }

        /// <summary>
        /// Returns all Visiable Windows in Process
        /// </summary>
        /// <param name="process">Process from which to get Visible Windows</param>
        /// <returns>Array of HwndInfo[]</returns>
        public static HwndInfo[] GetVisibleWindows(Process process)
        {
            List<HwndInfo> list = new List<HwndInfo>();
            HwndInfo[] windows = GetTopLevelVisibleWindows(process);
            foreach (HwndInfo window in windows)
            {
                if (IsWindowVisible(window.hWnd))
                    list.Add(new HwndInfo(window.hWnd));

                //search child windows
                EnumChildWindows(window.hWnd, delegate(IntPtr hWnd, IntPtr lParam)
                {
                    if (IsWindowVisible(hWnd))
                    {
                        list.Add(new HwndInfo(hWnd));
                    }
                    return true;
                }, IntPtr.Zero);

            }

            return list.ToArray();
        }

        /// <summary>
        /// Returns the First Window in the process with the given class name
        /// </summary>
        /// <param name="process"></param>
        /// <param name="classname"></param>
        /// <returns></returns>
        public static IntPtr FindFirstWindowWithClassName(Process process, string classname)
        {
            IntPtr foundhWnd = IntPtr.Zero;
            HwndInfo[] windows = GetTopLevelVisibleWindows(process);
            foreach (HwndInfo window in windows)
            {
                if (IsVisibleWithClassName(window.hWnd, classname))
                    return window.hWnd;

                //search child windows
                EnumChildWindows(window.hWnd, delegate(IntPtr hWnd, IntPtr lParam)
                {
                    if (IsVisibleWithClassName(hWnd, classname))
                    {
                        foundhWnd = hWnd;
                        return false;
                    }
                    return true;
                }, IntPtr.Zero);

            }

            return foundhWnd;
        }

        private static bool IsVisibleWithClassName(IntPtr hWnd, string classname)
        {
            StringBuilder sb = new StringBuilder(256);
            GetClassName(hWnd, sb, 256);
            return (IsWindowVisible(hWnd) && sb.ToString().Contains(classname));
        }

        internal static HwndInfo[] GetOutOfProcessVisibleChildWindows(IntPtr parenthWnd)
        {
            HwndInfo parentWindow = new HwndInfo(parenthWnd);
            List<HwndInfo> list = new List<HwndInfo>();
            EnumChildWindows(parenthWnd, delegate(IntPtr hWnd, IntPtr lParam)
            {
                HwndInfo childWindow = new HwndInfo(hWnd);
                if (IsWindowVisible(hWnd) && parentWindow.ProcessId != childWindow.ProcessId)
                    list.Add(childWindow);
                return true;
            }, IntPtr.Zero);

            return list.ToArray();
        }

        #endregion

        #region Private Members



        #endregion


        #region Private Unmanaged Interop

        [DllImport("user32.dll", EntryPoint = "GetClassName")]
        static extern int GetClassName(IntPtr hwnd,
                                              [MarshalAs(UnmanagedType.LPStr)] StringBuilder buf,
                                              int nMaxCount);

        [DllImport("User32.dll", EntryPoint = "EnumThreadWindows", PreserveSig = true, SetLastError = true)]
        static extern bool EnumThreadWindows(int threadId, EnumProcCallback callback, IntPtr lParam);

        [DllImport("User32.dll", EntryPoint = "EnumChildWindows", PreserveSig = true, SetLastError = true)]
        static extern bool EnumChildWindows(IntPtr hWndParent, EnumProcCallback callback, IntPtr lParam);

        [DllImport("User32.dll", EntryPoint = "IsWindowVisible", PreserveSig = true)]
        static extern bool IsWindowVisible(IntPtr hWnd);

        delegate bool EnumProcCallback(IntPtr hwnd, IntPtr lParam);

        #endregion

    }

    #endregion

}
