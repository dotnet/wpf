// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        
using System;
using System.Security;
using System.Security.Permissions;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;
using System.Globalization;

namespace Microsoft.Test.Win32
{

    ///<summary>
    ///</summary> 
    public delegate IntPtr HwndWrapperHook(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled);

    ///<summary>
    ///</summary>    
    public class HwndWrapper :  IDisposable
    {
        ///<summary>
        ///</summary>
        static HwndWrapper()
        {
            s_msgGCMemory = NativeMethods.RegisterWindowMessage("HwndWrapper.GetGCMemMessage");
            int win32Error = Marshal.GetLastWin32Error(); // Dance around FxCop.
            if (s_msgGCMemory == 0 && win32Error != 0)
            {
                throw new System.ComponentModel.Win32Exception(win32Error);
            }
        }

        ///<summary>
        ///</summary>
        [SecurityCritical]
        public HwndWrapper(
            int classStyle,
            int style,
            int exStyle,
            int x,
            int y,
            int width,
            int height,
            string name,
            IntPtr parent,
            HwndWrapperHook[] hooks)
        {
            // First, add the set of hooks.  This allows the hooks to receive the
            // messages sent to the window very early in the process.
            if (hooks != null)
            {
                for (int i = 0, iEnd = hooks.Length; i < iEnd; i++)
                {
                    if (null != hooks[i])
                        AddHook(hooks[i]);
                }
            }


            _wndProc = new HwndWrapperHook(WndProc);

            // We create the HwndSubclass object so that we can use its
            // window proc directly.  We will not be "subclassing" the
            // window we create.
            HwndSubclass hwndSubclass = new HwndSubclass(_wndProc);

            // Register a unique window class for this instance.
            Random r = new Random(unchecked((int)DateTime.Now.Ticks));
            NativeStructs.WNDCLASSEX_D wc_d = new NativeStructs.WNDCLASSEX_D();
            NativeStructs.WNDCLASSEX_I wc_i = new NativeStructs.WNDCLASSEX_I();

            IntPtr hInstance = NativeMethods.GetModuleHandle(null);
            IntPtr atom;
            string className = "";
            do
            {
                // Create a suitable unique class name.
                atom = IntPtr.Zero;

                // The class name is a concat of AppName, ThreadName, and RandomNumber.
                // Register will fail if the string gets over 255 in length.
                // So limit each part to a reasonable amount.
                string appName;
                if (null != AppDomain.CurrentDomain.FriendlyName && 128 <= AppDomain.CurrentDomain.FriendlyName.Length)
                    appName = AppDomain.CurrentDomain.FriendlyName.Substring(0, 128);
                else
                    appName = AppDomain.CurrentDomain.FriendlyName;

                string threadName;
                if (null != Thread.CurrentThread.Name && 64 <= Thread.CurrentThread.Name.Length)
                    threadName = Thread.CurrentThread.Name.Substring(0, 64);
                else
                    threadName = Thread.CurrentThread.Name;

                className = String.Format(CultureInfo.InvariantCulture, "HwndWrapper[{0};{1};{2}]", appName, threadName, r.Next().ToString(System.Globalization.NumberFormatInfo.InvariantInfo));

                // Make sure the class name hasn't been used already.
                if (!NativeMethods.GetClassInfoEx(hInstance,
                                                       className,
                                                       wc_i))
                {
                    wc_d.cbSize = Marshal.SizeOf(typeof(NativeStructs.WNDCLASSEX_D));
                    wc_d.style = classStyle;
                    wc_d.lpfnWndProc = new NativeStructs.WndProc(hwndSubclass.SubclassWndProc);
                    wc_d.cbClsExtra = 0;
                    wc_d.cbWndExtra = 0;
                    wc_d.hInstance = hInstance;
                    wc_d.hIcon = IntPtr.Zero;
                    wc_d.hCursor = IntPtr.Zero;
                    wc_d.hbrBackground = IntPtr.Zero;
                    wc_d.lpszMenuName = "";
                    wc_d.lpszClassName = className;
                    wc_d.hIconSm = IntPtr.Zero;

                    // Register the unique class for this instance.
                    //
                    // Note that it might still be possible that a class with
                    // this name will be registered between our previous 
                    // check and this call to RegisterClassEx(), so we also
                    // must catch the exception that might be thrown.
                    //
                    // Note that under stress we saw an exception that we 
                    // think was due to this call to RegisterClassEx.  We had
                    // a try/catch block around this call, but it was deemed
                    // to be an FxCop violation, so I removed it.
                    atom = NativeMethods.RegisterClassEx(wc_d);

                    // Clean up potentially bogus extra bits in the return value.
                    //atom = (IntPtr)((uint)(atom) & 0xffff);
                }
            } while (atom == IntPtr.Zero);

            // call CreateWindow
            _isInCreateWindow = true;
            _handle = NativeMethods.CreateWindowEx(exStyle,
                                                         className,
                                                         name,
                                                         style,
                                                         x,
                                                         y,
                                                         width,
                                                         height,
                                                         parent,
                                                         IntPtr.Zero,
                                                         IntPtr.Zero,
                                                         IntPtr.Zero);
            int Win32Err = Marshal.GetLastWin32Error(); // Dance around FxCop
            _isInCreateWindow = false;
            if (_handle == IntPtr.Zero)
            {
                // Because the HwndSubclass is pinned, but the HWND creation failed,
                // we need to manually clean it up.
                hwndSubclass.Dispose();

                throw new System.ComponentModel.Win32Exception(Win32Err);
            }
        }


        ///<summary>
        ///</summary>
        ~HwndWrapper()
        {
            Dispose(false);
        }

        ///<summary>
        ///</summary>            
        public virtual void Dispose()
        {
//            VerifyAccess();

            Dispose(true);
            GC.SuppressFinalize(this);
        }

        // internal Dispose(bool)
        private void Dispose(bool disposing)
        {
            if (_isDisposed)
            {
                return;
            }

            if (disposing)
            {
                // Notify listeners that we are being disposed.
                if (Disposed != null)
                {
                    Disposed(this, EventArgs.Empty);
                }
            }

            // We are now considered disposed.
            _isDisposed = true;

            if (_handle != IntPtr.Zero && !_isHwndDestroyed)
            {
                NativeMethods.DestroyWindow(new HandleRef(null, _handle));
            }
        }

        ///<summary>
        ///</summary>
        public IntPtr Handle { get { return _handle; } }

        ///<summary>
        ///</summary>
        public event EventHandler Disposed;

        ///<summary>
        ///</summary>
        [SecurityCritical]
        public void AddHook(HwndWrapperHook hook)
        {
            //VerifyAccess();
            if (_hooks == null)
            {
                _hooks = new WeakReferenceList();
            }

            _hooks.Add(hook);
        }


        ///<summary>
        ///</summary>
        public void RemoveHook(HwndWrapperHook hook)
        {
            //VerifyAccess();
            if (_hooks != null)
            {
                _hooks.Remove(hook);
            }
        }

        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            // The default result for messages we handle is 0.
            IntPtr result = IntPtr.Zero;

            // Call all of the hooks
            if (_hooks != null)
            {
                foreach (HwndWrapperHook hook in _hooks)
                {
                    result = hook(hwnd, msg, wParam, lParam, ref handled);

                    CheckForCreateWindowFailure(result, handled);

                    if (handled)
                    {
                        break;
                    }
                }
            }

            // We must handle certain messages - no matter if the helpers handled them or not!
            if (msg == NativeConstants.WM_DESTROY)
            {
                // Window is going away, make sure we cleaned up properly.
                _isHwndDestroyed = true;
                Dispose();

                // We want the default window proc to process this message as
                // well, so we mark it as unhandled.
                handled = false;
            }
            else if (msg == NativeConstants.WM_NCDESTROY)
            {
                // Preserve the hwnd value until the last possible moment.
                _handle = IntPtr.Zero;
                handled = false;
            }
            else if (msg == s_msgGCMemory)
            {
                // This is a special message we respond to by forcing a GC Collect.  This
                // is used by test apps and such.
                long lHeap = GC.GetTotalMemory(wParam == (IntPtr)1 ? true : false);
                result = (IntPtr)lHeap;
                handled = true;
            }

            CheckForCreateWindowFailure(result, true);

            // return our result
            return result;
        }

        private void CheckForCreateWindowFailure(IntPtr result, bool handled)
        {
            if (!_isInCreateWindow)
                return;

            if (IntPtr.Zero != result)
            {
                System.Diagnostics.Debug.WriteLine("Non-zero WndProc result=" + result);
                if (handled)
                {
                    if (System.Diagnostics.Debugger.IsAttached)
                        System.Diagnostics.Debugger.Break();
                    else
                        throw new InvalidOperationException();
                }
            }
        }

        private IntPtr _handle;
        private WeakReferenceList _hooks;
        private HwndWrapperHook _wndProc;

        private bool _isDisposed;
        private bool _isHwndDestroyed = false;

        private bool _isInCreateWindow = false;     // debugging variable (temporary)

        // Message to cause a dispose.  We need this to ensure we destroy the window on the right thread.
        private static int s_msgGCMemory;
    } // class RawWindow
}

