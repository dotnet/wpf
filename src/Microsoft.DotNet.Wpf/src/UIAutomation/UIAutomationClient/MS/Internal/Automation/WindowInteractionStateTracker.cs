// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// Description: Class used to track new UI appearing and make sure any events
// are propogated to that new UI.

using System;
using System.Globalization;
using System.Text;
using System.Windows.Automation;
using System.Diagnostics;
using MS.Win32;

namespace MS.Internal.Automation
{
    // Class used to track new UI appearing and make sure any events
    // are propogated to that new UI.
    internal class WindowInteractionStateTracker : WinEventWrap
    {
        //------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------
 
        #region Constructors

        internal WindowInteractionStateTracker()
            : base(new int[] { NativeMethods.EVENT_OBJECT_STATECHANGE })
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
            // ignore any event not pertaining directly to the window
            if (idObject != UnsafeNativeMethods.OBJID_WINDOW)
            {
                return;
            }

            // Ignore if this is a bogus hwnd (shouldn't happen)
            if (hwnd == IntPtr.Zero)
            {
                return;
            }

            OnStateChange(hwnd, idObject, idChild);
        }

        #endregion Internal Methods


        //------------------------------------------------------
        //
        //  Private Methods
        //
        //------------------------------------------------------

        #region Private Methods

        private void OnStateChange(IntPtr hwnd, int idObject, int idChild)
        {
            NativeMethods.HWND nativeHwnd = NativeMethods.HWND.Cast(hwnd);

            // Ignore windows that have been destroyed
            if (!SafeNativeMethods.IsWindow(nativeHwnd))
            {
                return;
            }

            AutomationElement rawEl = AutomationElement.FromHandle(hwnd);

            try
            {
                rawEl.GetCurrentPattern(WindowPattern.Pattern);
            }
            catch (InvalidOperationException)
            {
                // Only raise this event for elements with the WindowPattern.
                return;
            }

            Object windowInteractionState = rawEl.GetPatternPropertyValue(WindowPattern.WindowInteractionStateProperty, false);

            // if has no state value just return
            if (!(windowInteractionState is WindowInteractionState))
            {
                return;
            }

            WindowInteractionState state = (WindowInteractionState)windowInteractionState;

            // Filter... avoid duplicate events
            if (hwnd == _lastHwnd && state == _lastState)
            {
                return;
            }

            AutomationPropertyChangedEventArgs e = new AutomationPropertyChangedEventArgs(
                                        WindowPattern.WindowInteractionStateProperty,
                                        hwnd == _lastHwnd ? _lastState : WindowInteractionState.Running,
                                        state);

            ClientEventManager.RaiseEventInThisClientOnly(AutomationElement.AutomationPropertyChangedEvent, rawEl, e);

            // save the last hwnd/rect for filtering out duplicates
            _lastHwnd = hwnd;
            _lastState = state;
        }

        #endregion Private Methods


        //------------------------------------------------------
        //
        //  Private Fields
        //
        //------------------------------------------------------
 
        #region Private Fields

        private WindowInteractionState _lastState;      // keep track of last interaction state
        private IntPtr _lastHwnd;                       // and hwnd for dup checking

        #endregion Private Fields
    }
}
