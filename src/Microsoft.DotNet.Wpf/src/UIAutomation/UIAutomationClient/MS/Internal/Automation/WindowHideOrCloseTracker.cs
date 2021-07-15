// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// Description: Class used to track new UI appearing and make sure any events
// are propogated to that new UI.

using System;
using System.Text;
using System.Windows.Automation;
using MS.Win32;
using System.Diagnostics;

namespace MS.Internal.Automation
{
    // WindowHideOrCloseTracker - Class used to track new UI appearing and make sure any events
    // are propogated to that new UI.
    internal delegate void WindowHideOrCloseHandler( IntPtr hwnd, AutomationElement rawEl, int[] runtimeId );

    // Class used to track new UI appearing and make sure any events
    // are propogated to that new UI.
    internal class WindowHideOrCloseTracker : WinEventWrap
    {
        //------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------
 
        #region Constructors

        internal WindowHideOrCloseTracker(WindowHideOrCloseHandler newUIHandler)
            : base(new int[]
            {NativeMethods.EVENT_OBJECT_DESTROY, NativeMethods.EVENT_OBJECT_HIDE}) 
        {
            AddCallback(newUIHandler);
        }

        #endregion Constructors


        //------------------------------------------------------
        //
        //  Internal Methods
        //
        //------------------------------------------------------
 
        #region Internal Methods

        internal override void WinEventProc(int eventId, IntPtr hwnd, int idObject, int idChild, uint eventTime)
        {
            // ignore any event not pertaining directly to the window
            if (idObject != UnsafeNativeMethods.OBJID_WINDOW)
                return;

            // Ignore if this is a bogus hwnd (shouldn't happen)
            if (hwnd == IntPtr.Zero)
                return;

            NativeMethods.HWND nativeHwnd = NativeMethods.HWND.Cast( hwnd );

            // Purposefully including windows that have been destroyed (e.g. IsWindow will return
            // false here for EVENT_OBJECT_DESTROY) because we need that notification.
            if (eventId == NativeMethods.EVENT_OBJECT_HIDE && !SafeNativeMethods.IsWindow( nativeHwnd ))
            {
                return;
            }

            int[] runtimeId;
            AutomationElement rawEl;

            if (eventId == NativeMethods.EVENT_OBJECT_DESTROY)
            {
                // If the window has been destroyed just report the RuntimeId with the event.
                runtimeId = HwndProxyElementProvider.MakeRuntimeId( nativeHwnd );
                rawEl = null;
            }
            else
            {
                // If the window is just being hidden then can create (and return as event src) a real element
                rawEl = AutomationElement.FromHandle( hwnd );
                runtimeId = rawEl.GetRuntimeId();
            }

            // Do the notify.  Note that this handler is used to notify client-side UIAutomation providers of windows
            // being destroyed or hidden.  The delegate called here is itself protected by a lock.  This delegate may
            // call out to proxies but also calls ClientEventManager.RaiseEventInThisClientOnly which properly
            // queues the actual callout to client code.
            object[] handlers = GetHandlers();
            Debug.Assert(handlers.Length <= 1, "handlers.Length");
            if ( handlers.Length > 0 )
                ( (WindowHideOrCloseHandler)handlers [0] )( hwnd, rawEl, runtimeId );
        }

        #endregion Internal Methods


        //------------------------------------------------------
        //
        //  Private Fields
        //
        //------------------------------------------------------
 
        #region Private Fields

        // no state

        #endregion Private Fields
    }
}
