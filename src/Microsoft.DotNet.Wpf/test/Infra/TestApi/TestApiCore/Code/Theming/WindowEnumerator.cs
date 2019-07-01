// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Text;

namespace Microsoft.Test.Theming
{
    /// <summary>
    /// Class for Enumerating Window Handles
    /// </summary>
    internal static class WindowEnumerator
    {
        internal static HwndInfo[] GetTopLevelVisibleWindows(Process process)
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

            var list = new List<HwndInfo>();
            foreach (ProcessThread thread in process.Threads)
            {
                NativeMethods.EnumThreadWindows(
                    thread.Id, 
                    (IntPtr hWnd, IntPtr lParam) =>
                    {
                        if (NativeMethods.IsWindowVisible(hWnd))
                        {
                            list.Add(new HwndInfo(hWnd));
                        }

                        return true;
                    }, 
                    IntPtr.Zero);
            }

            return list.ToArray();
        }

        internal static HwndInfo[] GetVisibleWindows(Process process)
        {
            var list = new List<HwndInfo>();
            var windows = GetTopLevelVisibleWindows(process);
            foreach (HwndInfo window in windows)
            {
                if (NativeMethods.IsWindowVisible(window.hWnd))
                {
                    list.Add(new HwndInfo(window.hWnd));
                }

                // search child windows
                NativeMethods.EnumChildWindows(
                    window.hWnd, 
                    (IntPtr hWnd, IntPtr lParam) =>
                    {
                        if (NativeMethods.IsWindowVisible(hWnd))
                        {
                            list.Add(new HwndInfo(hWnd));
                        }

                        return true;
                    }, 
                    IntPtr.Zero);

            }

            return list.ToArray();
        }
              
        internal static IntPtr FindFirstWindowWithClassName(Process process, string classname)
        {
            var foundhWnd = IntPtr.Zero;
            var windows = GetTopLevelVisibleWindows(process);
            foreach (HwndInfo window in windows)
            {
                if (IsVisibleWithClassName(window.hWnd, classname))
                {
                    return window.hWnd;
                }

                //search child windows
                NativeMethods.EnumChildWindows(
                    window.hWnd, 
                    (IntPtr hWnd, IntPtr lParam) =>
                    {
                        if (IsVisibleWithClassName(hWnd, classname))
                        {
                            foundhWnd = hWnd;
                            return false;
                        }
                        return true;
                    }, 
                    IntPtr.Zero);

            }

            return foundhWnd;
        }

        internal static bool IsVisibleWithClassName(IntPtr hWnd, string classname)
        {
            var sb = new StringBuilder(256);
            NativeMethods.GetClassName(hWnd, sb, 256);
            return (NativeMethods.IsWindowVisible(hWnd) && sb.ToString().Contains(classname));
        }

        internal static IntPtr FindFirstWindowWithCaption(Process process, string caption)
        {
            IntPtr foundhWnd = IntPtr.Zero;
            HwndInfo[] windows = GetTopLevelVisibleWindows(process);
            foreach (HwndInfo window in windows)
            {
                if (IsVisibleWithCaption(window.hWnd, caption))
                {
                    return window.hWnd;
                }

                //search child windows
                NativeMethods.EnumChildWindows(
                    window.hWnd, 
                    (IntPtr hWnd, IntPtr lParam) =>
                    {
                        if (IsVisibleWithCaption(hWnd, caption))
                        {
                            foundhWnd = hWnd;
                            return false;
                        }

                        return true;
                    }, 
                    IntPtr.Zero);
            }

            return foundhWnd;
        }

        internal static bool IsVisibleWithCaption(IntPtr hWnd, string caption)
        {
            var sb = new StringBuilder(256);
            NativeMethods.GetWindowText(hWnd, sb, 256);
            return (NativeMethods.IsWindowVisible(hWnd) && sb.ToString().ToLower().Contains(caption.ToLower()));
        }

        internal static HwndInfo[] GetOutOfProcessVisibleChildWindows(IntPtr parenthWnd)
        {
            var parentWindow = new HwndInfo(parenthWnd);
            var list = new List<HwndInfo>();
            NativeMethods.EnumChildWindows(
                parenthWnd, 
                (IntPtr hWnd, IntPtr lParam) =>
                {
                    var childWindow = new HwndInfo(hWnd);
                    if (NativeMethods.IsWindowVisible(hWnd) && parentWindow.ProcessId != childWindow.ProcessId)
                    {
                        list.Add(childWindow);
                    }

                    return true;
                }, 
                IntPtr.Zero);

            return list.ToArray();
        }
    }
}
