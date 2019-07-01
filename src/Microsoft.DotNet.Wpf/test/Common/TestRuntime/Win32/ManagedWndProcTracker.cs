// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        
using System;
using System.Threading; 

using System.Collections;
using System.Runtime.InteropServices;
using System.Security;
using System.Security.Permissions;

namespace Microsoft.Test.Win32
{
    /// <summary>
    /// Helper class for tracking WndProcs.
    /// </summary>
    internal static class ManagedWndProcTracker
    {
        static ManagedWndProcTracker()
        {
            // Listen for ProcessExit so we can detach ourselves when the CLR shuts down
            // and avoid unmanaged code from calling back in to managed code during shutdown.
            AppDomain.CurrentDomain.ProcessExit += new EventHandler(OnAppDomainProcessExit);
            AppDomain.CurrentDomain.DomainUnload += new EventHandler(OnAppDomainProcessExit);
        }

        internal static void TrackHwnd(IntPtr hwnd)
        {
            lock (_hwndList)
            {
                _hwndList[hwnd] = hwnd;
            }
        }

        internal static void UnhookHwnd(IntPtr hwnd, bool hookUpDefWindowProc)
        {
            lock (_hwndList)
            {
                _hwndList.Remove(hwnd);
            }

            if (hookUpDefWindowProc)
            {
                HookUpDefWindowProc(hwnd);
            }
        }

        private static void OnAppDomainProcessExit(object sender, EventArgs e)
        {
            // AppDomain is exiting -- if anyone tries to call back into managed code
            // after this point, bad things will happen.  We must remove all unmanaged
            // code references to our WndProc delegates.  USER will explode if we set the 
            // WndProc to null, so the next most reasonable thing we can do is hook up
            // the DefaultWindowProc.
            //DbgUserBreakPoint();
            lock (_hwndList)
            {
                new SecurityPermission(SecurityPermissionFlag.UnmanagedCode).Assert(); // BlessedAssert: 
                try
                {
                    foreach (DictionaryEntry entry in _hwndList)
                    {
                        HookUpDefWindowProc((IntPtr)entry.Key);
                    }
                }
                finally
                {
                    CodeAccessPermission.RevertAssert();
                }

            }
        }

        private static void HookUpDefWindowProc(IntPtr hwnd)
        {
            IntPtr result = IntPtr.Zero;

            // We've already cleaned up, return immediately.
            if (hwnd == IntPtr.Zero)
            {
                return;
            }

            IntPtr defWindowProc = GetDefWindowProcAddress(hwnd);

            if (defWindowProc != IntPtr.Zero)
            {
                try
                {
                    result = NativeMethods.SetWindowLong(hwnd, NativeConstants.GWL_WNDPROC, defWindowProc);
                }
                catch (System.ComponentModel.Win32Exception e)
                {
                    // We failed to change the window proc.  Now what?

                    if (e.NativeErrorCode != 1400) // ERROR_INVALID_WINDOW_HANDLE
                    {
                        // For debugging purposes, throw an exception so we can debug 
                        // this and know if it's possible to call SetWindowLong on 
                        // the wrong thread.
                        throw;
                    }
                }
                if (result != IntPtr.Zero)
                {
                    NativeMethods.PostMessage(new HandleRef(null, hwnd), NativeConstants.WM_CLOSE, IntPtr.Zero, IntPtr.Zero);
                }
            }
        }

        // Get the DWP for the given HWND -- returns DefWindowProcA or DefWindowProcW
        // depending on IsWindowUnicode(hwnd).
        private static IntPtr GetDefWindowProcAddress(IntPtr hwnd)
        {
            // We need to swap back in the DefWindowProc, but which one we use depends on
            // what the Unicode-ness of the window.
            if (NativeMethods.IsWindowUnicode(hwnd))
            {
                if (_cachedDefWindowProcW == IntPtr.Zero)
                {
                    _cachedDefWindowProcW = GetUser32ProcAddress("DefWindowProcW");
                }

                return _cachedDefWindowProcW;
            }
            else
            {
                if (_cachedDefWindowProcA == IntPtr.Zero)
                {
                    _cachedDefWindowProcA = GetUser32ProcAddress("DefWindowProcA");
                }

                return _cachedDefWindowProcA;
            }
        }

        private static IntPtr GetUser32ProcAddress(string export)
        {
            IntPtr hModule = NativeMethods.GetModuleHandle(ExternDll.User32);


            if (hModule != IntPtr.Zero)
            {
                return NativeMethods.GetProcAddress(hModule, export);
            }
            return IntPtr.Zero;
        }

        private static IntPtr _cachedDefWindowProcA = IntPtr.Zero;
        private static IntPtr _cachedDefWindowProcW = IntPtr.Zero;
        private static Hashtable _hwndList = new Hashtable(10);
    }
}
