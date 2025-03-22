// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// Description: Windows LinkLabel Proxy

using System;
using System.Windows.Automation;
using System.Windows.Automation.Provider;
using MS.Win32;

namespace MS.Internal.AutomationProxies
{
    // FormsLink proxy
    internal class FormsLink : ProxyHwnd, IInvokeProvider
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
            ArgumentOutOfRangeException.ThrowIfNotEqual(idChild, 0);

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
        protected override ReadOnlySpan<WinEventTracker.EvtIdProperty> EventToWinEvent(AutomationEvent idEvent)
        {
            if (idEvent == InvokePattern.InvokedEvent)
            {
                return new WinEventTracker.EvtIdProperty[1] { new(NativeMethods.EventSystemCaptureEnd, idEvent) };
            }

            return base.EventToWinEvent(idEvent);
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
                throw new InvalidOperationException(SR.OperationCannotBePerformed);
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
