// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// Description: Class used to send BoundingRect changes for hwnds

using System;
using System.Windows;
using System.Windows.Automation;
using System.Runtime.InteropServices;
using System.ComponentModel;
using MS.Win32;

namespace MS.Internal.Automation
{
    // BoundingRectTracker - Class used to send BoundingRect changes for hwnds
    internal class BoundingRectTracker : WinEventWrap
    {
        //------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------
 
        #region Constructors

        internal BoundingRectTracker() 
            : base(new int[]{NativeMethods.EVENT_OBJECT_LOCATIONCHANGE, NativeMethods.EVENT_OBJECT_HIDE}) 
        {
            // Intentionally not setting the callback for the base WinEventWrap since the WinEventProc override
            // in this class calls RaiseEventInThisClientOnly to actually raise the event to the client.
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
            // Filter... send an event for hwnd only
            if ( hwnd == IntPtr.Zero || idObject != UnsafeNativeMethods.OBJID_WINDOW )
                return;

            switch (eventId)
            {
                case NativeMethods.EVENT_OBJECT_HIDE:           OnHide(hwnd, idObject, idChild); break;
                case NativeMethods.EVENT_OBJECT_LOCATIONCHANGE: OnLocationChange(hwnd, idObject, idChild); break;
            } 
        }

        #endregion Internal Methods


        //------------------------------------------------------
        //
        //  Private Methods
        //
        //------------------------------------------------------
 
        #region Private Methods

        private void OnHide(IntPtr hwnd, int idObject, int idChild)
        {
            // Clear last hwnd/rect variables (stop looking for dups)
            _lastHwnd = hwnd;
            _lastRect = _emptyRect;
        }

        private void OnLocationChange(IntPtr hwnd, int idObject, int idChild)
        {
            // Filter... send events for visible hwnds only
            if (!SafeNativeMethods.IsWindowVisible(NativeMethods.HWND.Cast( hwnd )))
                return;

            HandleBoundingRectChange(hwnd);
        }

        private void HandleBoundingRectChange(IntPtr hwnd)
        {
            NativeMethods.HWND nativeHwnd = NativeMethods.HWND.Cast( hwnd );
            NativeMethods.RECT rc32 = new NativeMethods.RECT(0,0,0,0);

            // if GetWindwRect fails, most likely the nativeHwnd is an invalid window, so just return.
            if (!Misc.GetWindowRect(nativeHwnd, out rc32))
            {
                return;
            }

            // Filter... avoid duplicate events
            if (hwnd == _lastHwnd && Compare( rc32, _lastRect ))
            {
                return;
            }

            AutomationElement rawEl = AutomationElement.FromHandle(hwnd);

            // Problem with Avalon combo box & menus:
            // There was a windows issue where we get two events.  One for the hwnd (LocationChange WinEvent)
            // and one for the [usually] DockPanel (Avalon BoundingRectangleProperty change). Both have the
            // same Rect value. It's unclear is this issue is still occuring and what (if anything) needs to be done.
            // Waiting for WPP redesign to investigate further how to filter out the duplicate which
            // happens to be the first (hwnd-based) event. 
            //
            AutomationPropertyChangedEventArgs e = new AutomationPropertyChangedEventArgs(
                                        AutomationElement.BoundingRectangleProperty, 
                                        Rect.Empty, 
                                        new Rect (rc32.left, rc32.top, rc32.right - rc32.left, rc32.bottom - rc32.top));

            // In the case of HWND hosted Avalon content, we will get a LocationChange WinEvent for the host
            // window when it's bounding rect changes (e.g. an Avalon window is resized) and we won't (that I've seen)
            // get a BoundingRect property change from the Avalon content.  Therefore, we need to map the WinEvent.
            // In this case, rawEl is already the remote object. It's ol to call this locally since this is called 
            // to handle a WinEvent (e.g. always called on client-side). Since rawEl may be local (proxied) or 
            // remote (native) impl then use that version of RaiseEventInThisClientOnly
            ClientEventManager.RaiseEventInThisClientOnly(AutomationElement.AutomationPropertyChangedEvent, rawEl, e);

            // save the last hwnd/rect for filtering out duplicates
            _lastHwnd = hwnd;
            _lastRect = rc32;
        }

        // Should this be in misc?
        private static bool Compare( NativeMethods.RECT rc1, NativeMethods.RECT rc2 )
        {
            return rc1.left == rc2.left
                && rc1.top == rc2.top
                && rc1.right == rc2.right
                && rc1.bottom == rc2.bottom;
        }

        #endregion Private Methods


        //------------------------------------------------------
        //
        //  Private Fields
        //
        //------------------------------------------------------
 
        #region Private Fields

        private static NativeMethods.RECT _emptyRect = new NativeMethods.RECT(0,0,0,0);

        private NativeMethods.RECT _lastRect;      // keep track of last location
        private IntPtr     _lastHwnd;      // and hwnd for dup checking

        #endregion Private Fields
    }
}

