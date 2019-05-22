// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

﻿

namespace Standard
{
    using System;
    using System.Runtime.ConstrainedExecution;
    using System.Runtime.InteropServices;
    using System.Security;
    using System.Windows;
    using System.Windows.Threading;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;

    /// <SecurityNote> 
    ///   Critical : Should not be created by partially trusted callers because it's finalizer is critical
    ///              and does not allow partial trust callers.
    /// </SecurityNote>
    [SecurityCritical]
    internal sealed class MessageWindow : CriticalFinalizerObject
    {
        /// <SecurityNote>
        ///   Critical : Initializes critical static members
        /// <SecurityNote>
        [SecurityCritical]
        static MessageWindow()
        {
        }
        
        // Alias this to a static so the wrapper doesn't get GC'd
        /// <SecurityNote>
        ///   Critical : Delegate passed critical data (Win32 messages and parameters) used to control Win32 window behavior
        /// <SecurityNote>
        [SecurityCritical]
        private static readonly WndProc s_WndProc = new WndProc(_WndProc);
        
        /// <SecurityNote> 
        ///   Critical : Provides access to instances of critical MessageWindow type
        /// </SecurityNote>
        [SecurityCritical]
        private static readonly Dictionary<IntPtr, MessageWindow> s_windowLookup = new Dictionary<IntPtr, MessageWindow>();

        /// <SecurityNote>
        ///   Critical : Delegate passed critical data (Win32 messages and parameters) used to control Win32 window behavior
        /// <SecurityNote>
        [SecurityCritical]
        private WndProc _wndProcCallback;
        private string _className;
        private bool _isDisposed;
        Dispatcher _dispatcher;

        /// <SecurityNote>
        ///  Critical : Accesses critical Win32 window handle
        /// </SecurityNote>
        public IntPtr Handle 
        { 
            [SecurityCritical]
            get; 
            
            [SecurityCritical]
            private set; 
        }

        /// <SecurityNote>
        ///  Critical : P-Invokes to register window class and create win32 window
        /// </SecurityNote>
        [SecurityCritical]
        [SuppressMessage("Microsoft.Security", "CA2122:DoNotIndirectlyExposeMethodsWithLinkDemands")]
        public MessageWindow(CS classStyle, WS style, WS_EX exStyle, Rect location, string name, WndProc callback)
        {
            // A null callback means just use DefWindowProc.
            _wndProcCallback = callback;
            _className = "MessageWindowClass+" + Guid.NewGuid().ToString();

            var wc = new WNDCLASSEX
            {
                cbSize = Marshal.SizeOf(typeof(WNDCLASSEX)),
                style = classStyle,
                lpfnWndProc = s_WndProc,
                hInstance = NativeMethods.GetModuleHandle(null),
                hbrBackground = NativeMethods.GetStockObject(StockObject.NULL_BRUSH),
                lpszMenuName = "",
                lpszClassName = _className,
            };

            NativeMethods.RegisterClassEx(ref wc);

            GCHandle gcHandle = default(GCHandle);
            try
            {
                gcHandle = GCHandle.Alloc(this);
                IntPtr pinnedThisPtr = (IntPtr)gcHandle;

                Handle = NativeMethods.CreateWindowEx(
                    exStyle,
                    _className,
                    name,
                    style,
                    (int)location.X,
                    (int)location.Y,
                    (int)location.Width,
                    (int)location.Height,
                    IntPtr.Zero,
                    IntPtr.Zero,
                    IntPtr.Zero,
                    pinnedThisPtr);
            }
            finally
            {
                gcHandle.Free();
            }
            
            _dispatcher = Dispatcher.CurrentDispatcher;
        }

        /// <SecurityNote>
        ///   Critical : Calls critical methods
        /// <SecurityNote>
        [SecurityCritical]
        ~MessageWindow()
        {
            _Dispose(false, false);
        }

        /// <SecurityNote>
        ///   Critical : Calls critical methods
        /// <SecurityNote>
        [SecurityCritical]
        public void Release()
        {
            _Dispose(true, false);
            GC.SuppressFinalize(this);
        }

        // This isn't right if the Dispatcher has already started shutting down.
        // It will wind up leaking the class ATOM...
        /// <SecurityNote>
        ///   Critical : Calls critical methods
        /// <SecurityNote>
        [SecurityCritical]
        [SuppressMessage("Microsoft.Usage", "CA1801:ReviewUnusedParameters", MessageId = "disposing")]
        private void _Dispose(bool disposing, bool isHwndBeingDestroyed)
        {
            if (_isDisposed)
            {
                // Block against reentrancy.
                return;
            }

            _isDisposed = true;

            IntPtr hwnd = Handle;
            string className = _className;

            if (isHwndBeingDestroyed)
            {
                _dispatcher.BeginInvoke(DispatcherPriority.Normal, (DispatcherOperationCallback)_DestroyWindowCallback, new object [] { IntPtr.Zero, className });
            }
            else if (Handle != IntPtr.Zero)
            {
                if (_dispatcher.CheckAccess())
                {
                    _DestroyWindow(hwnd, className);
                }
                else
                {
                    _dispatcher.BeginInvoke(DispatcherPriority.Normal, (DispatcherOperationCallback)_DestroyWindowCallback, new object [] { hwnd, className });
                }
            }

            s_windowLookup.Remove(hwnd);

            _className = null;
            Handle = IntPtr.Zero;
        }

        /// <SecurityNote>
        ///   Critical : Calls critical methods
        /// <SecurityNote>
        [SecurityCritical]
        private object _DestroyWindowCallback(object arg)
        {
            object [] args = (object[])arg;
            _DestroyWindow((IntPtr)args[0], (string)args[1]);
            return null;
        }

        /// <SecurityNote>
        ///   Critical : Calls critical methods
        /// <SecurityNote>
        [SecurityCritical]
        [SuppressMessage("Microsoft.Usage", "CA1816:CallGCSuppressFinalizeCorrectly")]
        private static IntPtr _WndProc(IntPtr hwnd, WM msg, IntPtr wParam, IntPtr lParam)
        {
            IntPtr ret = IntPtr.Zero;
            MessageWindow hwndWrapper = null;

            if (msg == WM.CREATE)
            {
                var createStruct = (CREATESTRUCT)Marshal.PtrToStructure(lParam, typeof(CREATESTRUCT));
                GCHandle gcHandle = GCHandle.FromIntPtr(createStruct.lpCreateParams);
                hwndWrapper = (MessageWindow)gcHandle.Target;
                s_windowLookup.Add(hwnd, hwndWrapper);
            }
            else
            {
                if (!s_windowLookup.TryGetValue(hwnd, out hwndWrapper))
                {
                    return NativeMethods.DefWindowProc(hwnd, msg, wParam, lParam);
                }
            }
            Assert.IsNotNull(hwndWrapper);

            WndProc callback = hwndWrapper._wndProcCallback;
            if (callback != null)
            {
                ret = callback(hwnd, msg, wParam, lParam);
            }
            else
            {
                ret = NativeMethods.DefWindowProc(hwnd, msg, wParam, lParam);
            }

            if (msg == WM.NCDESTROY)
            {
                hwndWrapper._Dispose(true, true);
                GC.SuppressFinalize(hwndWrapper);
            }

            return ret;
        }

        /// <SecurityNote>
        ///   Critical : Calls critical methods
        /// <SecurityNote>
        [SecurityCritical]
        private static void _DestroyWindow(IntPtr hwnd, string className)
        {
            Utility.SafeDestroyWindow(ref hwnd);
            NativeMethods.UnregisterClass(className, NativeMethods.GetModuleHandle(null));
        }
    }
}
