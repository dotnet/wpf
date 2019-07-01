// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        
using System;
using System.Runtime.InteropServices;
using System.Security.Permissions;
using MS.Internal;



namespace Microsoft.Test.Win32
{
    /// <summary>
    ///     The HwndSubclass class provides a managed way to subclass an existing
    ///     HWND.  This class inserts itself into the WNDPROC chain for the
    ///     window, and will call a specified delegate to process the window
    ///     messages that arrive.  The delegate has a slightly different
    ///     signature than a WNDPROC to be more specific about whether the
    ///     message was handled or not.  If the message was not handled by the
    ///     delegate, this class passes the message on down the WNDPROC chain.
    ///
    ///     To use this class properly, simply:
    ///     1) Create an instance of the HwndSubclass class and pass the delegate
    ///        to the constructor.
    ///     2) Call Attach(HWND) to subclass an existing window.
    ///     3) Call Detach(false) to unsubclass the window when you are done.
    ///
    ///     You can also just call RequestDetach() to send a message to the
    ///     window that will cause the HwndSubclass to detach itself.  This is
    ///     important if you are on a different thread, as the HwndSubclass class
    ///     is not thread safe and will be operated on by the thread that owns
    ///     the window.
    /// </summary>
    public class HwndSubclass : IDisposable
    {
        static HwndSubclass()
        {
            s_detachMessage = NativeMethods.RegisterWindowMessage("HwndSubclass.DetachMessage");

            // Go find the address of DefWindowProc.
            IntPtr hModuleUser32 = NativeMethods.GetModuleHandle(ExternDll.User32);
            IntPtr address = NativeMethods.GetProcAddress(hModuleUser32, "DefWindowProcW");

            DefWndProc = address;
        }

        /// <summary>
        ///     This HwndSubclass constructor binds the HwndSubclass object to the
        ///     specified delegate.  This delegate will be called to process
        ///     the messages that are sent or posted to the window.
        /// </summary>
        /// <param name="hook">
        ///     The delegate that will be called to process the messages that
        ///     are sent or posted to the window.
        /// </param>
        /// <returns>
        ///     Nothing.
        /// </returns>
        public HwndSubclass(HwndWrapperHook hook)
        {
            if (hook == null)
            {
                throw new ArgumentNullException("hook");
            }

            _bond = Bond.Unattached;
            _hook = new WeakReference(hook);

            // Allocate a GC handle so that we won't be collected, even if all
            // references to us get released.  This is because a component outside
            // of the managed code (ie. the window we are subclassing) still holds
            // a reference to us - just not a reference that the GC recognizes.
            _gcHandle = GCHandle.Alloc(this);
        }

        /// <summary>
        /// </summary>
        ~HwndSubclass()
        {
            // In Shutdown, the finalizer is called on LIVE OBJECTS.
            //
            // So this method can be called.  (even though we are pinned)
            // But it should not be called called after SuppressFinalize()
            // so there is no danger of double Unhook'ing.
            Dispose(false);
        }

        /// <summary>
        /// </summary>
        public virtual void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        private void Dispose(bool disposing)
        {
            UnhookWindowProc();

            _hook = null;
        }

        /// <summary>
        ///     This method subclasses the specified window, such that the
        ///     delegate specified to the constructor will be called to process
        ///     the messages that are sent or posted to this window.
        /// </summary>
        /// <param name="hwnd">
        ///     The window to subclass.
        /// </param>
        /// <returns>
        ///     An identifier that can be used to reference this instance of
        ///     the HwndSubclass class in the static RequestDetach method.
        /// </returns>
        public IntPtr Attach(IntPtr hwnd)
        {
            if (hwnd == IntPtr.Zero)
            {
                throw new ArgumentNullException("hwnd");
            }
            if (_bond != Bond.Unattached)
            {
                throw new InvalidOperationException();
            }

            NativeStructs.WndProc newWndProc = new NativeStructs.WndProc(SubclassWndProc);
            IntPtr oldWndProc = NativeMethods.GetWindowLong(hwnd, NativeConstants.GWL_WNDPROC);
            HookWindowProc(hwnd, newWndProc, oldWndProc);

            // Return the GC handle as a unique identifier of this 
            return (IntPtr)_gcHandle;
        }


        /// <summary>
        ///     This method unsubclasses this HwndSubclass object from the window
        ///     it previously subclassed. The HwndSubclass object is not thread
        ///     safe, and should thus be called only by the thread that owns
        ///     the window being unsubclassed.
        /// </summary>
        /// <param name="force">
        ///     Whether or not the unsubclassing should be forced.  Due to the
        ///     way that Win32 implements window subclassing, it is not always
        ///     possible to safely remove a window proc from the WNDPROC chain.
        ///     However, the delegate will not be called again after this
        ///     method returns.
        /// </param>
        /// <returns>
        ///     Whether or not this HwndSubclass object was actually removed from
        ///     the WNDPROC chain.
        /// </returns>
        public bool Detach(bool force)
        {
            bool detached = false;
            bool cleanup = force;

            // If we have already detached, return immediately.
            if (_bond == Bond.Detached || _bond == Bond.Unattached)
            {
                detached = true;
            }
            else
            {
                // When we detach, we simply make a note of it.
                _bond = Bond.Orphaned;

                // If we aren't going to force our removal from the window proc chain,
                // at least check to see if we can safely remove us.
                if (!force)
                {
                    NativeStructs.WndProc currentWndProc = NativeMethods.GetWindowLongWndProc(_hwndAttached, NativeConstants.GWL_WNDPROC);

                    //AvDebug.Assert(currentWndProc != null, "A window should always have a window proc.");

                    // If the current window proc is us, then we can clean up.
                    if (currentWndProc == _attachedWndProc)
                    {
                        cleanup = true;
                    }
                }

                // Cleanup if we can.
                if (cleanup)
                {
                    Dispose();
                    detached = true;
                }
            }

            return detached;
        }

        /// <summary>
        ///     This method sends a message to the window that is currently
        ///     subclassed by this instance of the HwndSubclass class, in order
        ///     to unsubclass the window.  This is important if a different
        ///     thread than the thread that owns the window wants to initiate
        ///     the unsubclassing.
        /// </summary>
        /// <param name="force">
        ///     Whether or not the unsubclassing should be forced.  Due to the
        ///     way that Win32 implements window subclassing, it is not always
        ///     possible to safely remove a window proc from the WNDPROC chain.
        ///     However, the delegate will not be called again after this
        ///     method returns.
        /// </param>
        /// <returns>
        ///     Nothing.
        /// </returns>
        public void RequestDetach(bool force)
        {
            // Let the static version do the work.
            if (_hwndAttached != IntPtr.Zero)
            {
                RequestDetach(_hwndAttached, (IntPtr)_gcHandle, force);
            }
        }

        /// <summary>
        ///     This method sends a message to the specified window in order to
        ///     cause the specified bridge to unsubclass the window.  This is
        ///     important if a different thread than the thread that owns the
        ///     window wants to initiate the unsubclassing.  The HwndSubclass
        ///     object must be identified by the value returned from Attach().
        /// </summary>
        /// <param name="hwnd">
        ///     The window to unsubclass.
        /// </param>
        /// <param name="subclass">
        ///     The identifier of the subclass to unsubclass.
        /// </param>
        /// <param name="force">
        ///     Whether or not the unsubclassing should be forced.  Due to the
        ///     way that Win32 implements window subclassing, it is not always
        ///     possible to safely remove a window proc from the WNDPROC chain.
        ///     However, the delegate will not be called again after this
        ///     method returns.
        /// </param>
        /// <returns>
        ///     Nothing.
        /// </returns>
        public static void RequestDetach(IntPtr hwnd, IntPtr subclass, bool force)
        {
            if (hwnd == IntPtr.Zero)
            {
                throw new ArgumentNullException("hwnd");
            }
            if (subclass == IntPtr.Zero)
            {
                throw new ArgumentNullException("subclass");
            }

            int iForce = force ? 1 : 0;
            NativeMethods.SendMessage(new HandleRef(null, hwnd), s_detachMessage, subclass, (IntPtr)iForce);
        }

        /// <summary>
        ///     This is the WNDPROC that gets inserted into the window's
        ///     WNDPROC chain.  It responds to various conditions that
        ///     would cause this HwndSubclass object to unsubclass the window,
        ///     and then calls the delegate specified to the HwndSubclass
        ///     constructor to process the message.  If the delegate does not
        ///     handle the message, the message is then passed on down the
        ///     WNDPROC chain for further processing.
        /// </summary>
        /// <param name="hwnd">
        ///     The window that this message was sent or posted to.
        /// </param>
        /// <param name="msg">
        ///     The message that was sent or posted.
        /// </param>
        /// <param name="wParam">
        ///     A parameter for the message that was sent or posted.
        /// </param>
        /// <param name="lParam">
        ///     A parameter for the message that was sent or posted.
        /// </param>
        /// <returns>
        ///     The value that is the result of processing the message.
        /// </returns>
        public IntPtr SubclassWndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam)
        {
            IntPtr retval = IntPtr.Zero;
            bool handled = false;

            // If we are unattached and we receive a message, then we must have
            // been used as the original window proc.  In this case, we insert
            // ourselves as if the original window proc had been DefWindowProc.
            if (_bond == Bond.Unattached)
            {
                HookWindowProc(hwnd, new NativeStructs.WndProc(SubclassWndProc), DefWndProc);
            }
            else if (_bond == Bond.Detached)
            {
                throw new InvalidOperationException();
            }

            if (msg == s_detachMessage)
            {
                // We received our special message to detach.  Make sure it is intended
                // for us by matching the bridge.
                if (wParam == (IntPtr)_gcHandle)
                {
                    retval = Detach(false) ? (IntPtr)1 : (IntPtr)0;
                    handled = true;
                }
            }
            else
            {
                HwndWrapperHook hook = _hook.Target as HwndWrapperHook;
                if (_bond == Bond.Attached && hook != null)
                {
                    retval = hook(hwnd, msg, wParam, lParam, ref handled);
                }

                // Handle WM_NCDESTROY explicitly to forcibly clean up.
                if (msg == NativeConstants.WM_NCDESTROY)
                {
                    // The fact that we received this message means that we are
                    // still in the call chain.  This is our last chance to clean
                    // up, and no other message should be received by this window
                    // proc again. It is OK to force a cleanup now.
                    Dispose();

                    // Always pass the WM_NCDESTROY message down the chain!
                    handled = false;
                }
            }

            // If our window proc didn't handle this message, pass it on down the
            // chain.
            if (!handled)
            {
                retval = CallOldWindowProc(hwnd, msg, wParam, lParam);
            }

            return retval;
        }

        private object DispatcherCallbackOperation(object o)
        {
            //extract parameters
            object[] args = (object[])o;
            IntPtr hwnd = (IntPtr)args[0];
            Int32 msg = (Int32)args[1];
            IntPtr wParam = (IntPtr)args[2];
            IntPtr lParam = (IntPtr)args[3];
            bool handled = false;
            IntPtr retval = IntPtr.Zero;
            // make the call
            HwndWrapperHook hook = _hook.Target as HwndWrapperHook;
            if (_bond == Bond.Attached && hook != null)
            {
                retval = hook(hwnd, msg, wParam, lParam, ref handled);
            }
            return (new object[] { retval, handled });
        }
        /// <summary>
        ///     This method lets the user call the old WNDPROC, i.e
        ///     the next WNDPROC in the chain directly.
        /// </summary>
        /// <param name="hwnd">
        ///     The window that this message was sent or posted to.
        /// </param>
        /// <param name="msg">
        ///     The message that was sent or posted.
        /// </param>
        /// <param name="wParam">
        ///     A parameter for the message that was sent or posted.
        /// </param>
        /// <param name="lParam">
        ///     A parameter for the message that was sent or posted.
        /// </param>
        /// <returns>
        ///     The value that is the result of processing the message.
        /// </returns>
        IntPtr CallOldWindowProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam)
        {
            return NativeMethods.CallWindowProc(_oldWndProc, hwnd, msg, wParam, lParam);
        }

        private void HookWindowProc(IntPtr hwnd, NativeStructs.WndProc newWndProc, IntPtr oldWndProc)
        {
            _hwndAttached = hwnd;
            _hwndHandleRef = new HandleRef(null, _hwndAttached);
            _bond = Bond.Attached;

            _attachedWndProc = newWndProc;
            _oldWndProc = oldWndProc;
            IntPtr oldWndProc2 = NativeMethods.SetWindowLong(_hwndAttached, NativeConstants.GWL_WNDPROC, _attachedWndProc);

            // Track this window so that we can rip out the managed window proc
            // when the CLR shuts down.
            ManagedWndProcTracker.TrackHwnd(_hwndAttached);
        }

        // This method should only be called from Dispose. Otherwise 
        // assumptions about the disposing/finalize state could be violated.
        private void UnhookWindowProc()
        {
            if (_bond == Bond.Attached || _bond == Bond.Orphaned)
            {
                ManagedWndProcTracker.UnhookHwnd(_hwndAttached, false /* don't hook up defWindowProc */);
                try
                {
                    NativeMethods.SetWindowLong(_hwndHandleRef.Handle, NativeConstants.GWL_WNDPROC, _oldWndProc);
                }
                catch (System.ComponentModel.Win32Exception e)
                {
                    if (e.NativeErrorCode != 1400) // ERROR_INVALID_WINDOW_HANDLE
                    {
                        throw;
                    }
                }
            }
            _bond = Bond.Detached;

            _oldWndProc = IntPtr.Zero;
            _attachedWndProc = null;
            _hwndAttached = IntPtr.Zero;
            _hwndHandleRef = new HandleRef(null, IntPtr.Zero);

            // un-Pin this object.
            // Note: the GC is free to collect this object at anytime
            // after we have freed this handle - that is, once all
            // other managed references go away.

            //AvDebug.Assert(_gcHandle.IsAllocated, "External GC handle has not been allocated.");

            if (null != _gcHandle)
                _gcHandle.Free();
        }

        // Message to cause a detach.  WPARAM=IntPtr returned from Attach().  LPARAM=force cleanup or not.
        private static int s_detachMessage;

        private enum Bond
        {
            Unattached,
            Attached,
            Detached,
            Orphaned
        }

        private static IntPtr DefWndProc;

        private IntPtr _hwndAttached;
        private HandleRef _hwndHandleRef;
        private NativeStructs.WndProc _attachedWndProc;
        private IntPtr _oldWndProc;
        private Bond _bond;
        private GCHandle _gcHandle;
        private WeakReference _hook;
    };
}
