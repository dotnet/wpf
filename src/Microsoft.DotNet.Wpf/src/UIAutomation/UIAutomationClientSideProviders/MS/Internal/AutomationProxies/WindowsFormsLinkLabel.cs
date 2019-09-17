// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// Description: Windows LinkLabel Proxy

// PRESHARP: In order to avoid generating warnings about unkown message numbers and unknown pragmas.
#pragma warning disable 1634, 1691

using System;
using System.Collections;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Windows.Automation;
using System.Windows.Automation.Provider;
using Accessibility;
using System.Windows;
using System.Windows.Input;
using MS.Win32;

namespace MS.Internal.AutomationProxies
{
    // FormsLink proxy
    class FormsLink : ProxyHwnd, IInvokeProvider
    {
        // ------------------------------------------------------
        //
        // Construction/destruction
        //
        // ------------------------------------------------------

        #region Constructors

        internal FormsLink (IntPtr hwnd, ProxyFragment parent, int item)
            : base( hwnd, parent, item)
        {
            // Set the strings to return properly the properties.
            _cControlType = ControlType.Hyperlink;

            // support for events
            _createOnEvent = new WinEventTracker.ProxyRaiseEvents(RaiseEvents);
        }

        #endregion Constructors

        #region Proxy Create

        // Static Create method called by UIAutomation to create this proxy.
        internal static IRawElementProviderSimple Create (IntPtr hwnd, int idChild)
        {
            // Something is wrong if idChild is not zero 
            if (idChild != 0)
            {
                System.Diagnostics.Debug.Assert (idChild == 0, "Invalid Child Id, idChild != 0");
                throw new ArgumentOutOfRangeException("idChild", idChild, SR.Get(SRID.ShouldBeZero));
            }

            return new FormsLink(hwnd, null, idChild);
        }

        // Static Create method called by the event tracker system
        internal static void RaiseEvents(IntPtr hwnd, int eventId, object idProp, int idObject, int idChild)
        {
            if (idObject != NativeMethods.OBJID_VSCROLL && idObject != NativeMethods.OBJID_HSCROLL)
            {
                ProxySimple wtv = new FormsLink(hwnd, null, idChild);
                wtv.DispatchEvents(eventId, idProp, idObject, idChild);
            }
        }

        #endregion Proxy Create

        // ------------------------------------------------------
        //
        // Patterns Implementation
        //
        // ------------------------------------------------------

        #region ProxyHwnd Interface

        // Builds a list of Win32 WinEvents to process a UIAutomation Event.
        // Param name="idEvent", UIAuotmation event
        // Param name="cEvent"out, number of winevent set in the array
        // Returns an array of Events to Set. The number of valid entries in this array pass back in cEvent
        protected override WinEventTracker.EvtIdProperty[] EventToWinEvent(AutomationEvent idEvent, out int cEvent)
        {
            if (idEvent == InvokePattern.InvokedEvent)
            {
                cEvent = 1;
                return new WinEventTracker.EvtIdProperty[1] { new WinEventTracker.EvtIdProperty(NativeMethods.EventSystemCaptureEnd, idEvent) };
            }

            return base.EventToWinEvent(idEvent, out cEvent);
        }

        #endregion ProxyHwnd Interface

        #region ProxySimple Interface

        // Returns a pattern interface if supported.
        internal override object GetPatternProvider(AutomationPattern iid)
        {
            return iid == InvokePattern.Pattern ? this : null;
        }

        // Sets the focus to this item.
        internal override bool SetFocus()
        {
            Misc.SetFocus(_hwnd);
            return true;
        }

        #endregion ProxySimple Interface

        #region Invoke Pattern

        // Same as clicking on an hyperlink
        void IInvokeProvider.Invoke()
        {
            // Check that button can be clicked.
            //
            // This state could change anytime.
            //

            // Make sure that the control is enabled
            if (!SafeNativeMethods.IsWindowEnabled(_hwnd))
            {
                throw new ElementNotEnabledException();
            }

            if (!SafeNativeMethods.IsWindowVisible(_hwnd))
            {
                throw new InvalidOperationException(SR.Get(SRID.OperationCannotBePerformed));
            }

            Misc.SetFocus(_hwnd);

            NativeMethods.Win32Point pt = new NativeMethods.Win32Point();
            if (GetClickablePoint(out pt, false))
            {
                Misc.MouseClick(pt.x, pt.y);
            }
        }

        #endregion Invoke Pattern
   }
}
