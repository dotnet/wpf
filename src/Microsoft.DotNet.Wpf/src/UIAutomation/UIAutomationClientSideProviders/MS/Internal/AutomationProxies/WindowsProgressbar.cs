// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// Description: HWND-based ProgressBar Proxy

using System;
using System.Collections;
using System.Text;
using System.ComponentModel;
using System.Windows.Automation;
using System.Windows.Automation.Provider;
using System.Windows;
using MS.Win32;

namespace MS.Internal.AutomationProxies
{
    class WindowsProgressBar: ProxyHwnd, IRangeValueProvider
    {
       // ------------------------------------------------------
        //
        // Constructors
        //
        // ------------------------------------------------------

        #region Constructors

        WindowsProgressBar (IntPtr hwnd, ProxyFragment parent, int item)
            : base( hwnd, parent, item )
        {
            _cControlType = ControlType.ProgressBar;

            // support for events
            _createOnEvent = new WinEventTracker.ProxyRaiseEvents (RaiseEvents);
        }

        #endregion

        #region Proxy Create

        // Static Create method called by UIAutomation to create this proxy.
        // returns null if unsuccessful
        internal static IRawElementProviderSimple Create(IntPtr hwnd, int idChild, int idObject)
        {
            return Create(hwnd, idChild);
        }

        private static IRawElementProviderSimple Create(IntPtr hwnd, int idChild)
        {
            // Something is wrong if idChild is not zero 
            if (idChild != 0)
            {
                System.Diagnostics.Debug.Assert (idChild == 0, "Invalid Child Id, idChild != 0");
                throw new ArgumentOutOfRangeException("idChild", idChild, SR.Get(SRID.ShouldBeZero));
            }

            return new WindowsProgressBar(hwnd, null, 0);
        }

        // Static Create method called by the event tracker system
        // WinEvents are raise because items exist. So it makes sense to create the item and
        // check for details afterward.
        internal static void RaiseEvents (IntPtr hwnd, int eventId, object idProp, int idObject, int idChild)
        {
            if (idObject != NativeMethods.OBJID_VSCROLL && idObject != NativeMethods.OBJID_HSCROLL)
            {
                WindowsProgressBar wtv = new WindowsProgressBar (hwnd, null, 0);
                wtv.DispatchEvents (eventId, idProp, idObject, idChild);
            }
        }

        #endregion

       //------------------------------------------------------
        //
        //  Patterns Implementation
        //
        //------------------------------------------------------

        #region ProxySimple Interface
        
        // Returns a pattern interface if supported.
        internal override object GetPatternProvider (AutomationPattern iid)
        {
            return (iid == RangeValuePattern.Pattern) ? this : null;
        }

        #endregion
        
        #region RangeValue Pattern

        void IRangeValueProvider.SetValue (double val)
        {
            //This proxy is readonly
            throw new InvalidOperationException(SR.Get(SRID.ValueReadonly));
        }

        // Request to get the value that this UI element is representing in a native format
        double IRangeValueProvider.Value
        {
            get
            {
                return (double)ValuePercent;
            }
        }

        bool IRangeValueProvider.IsReadOnly
        {
            get
            {
                return true;
            }
        }

        double IRangeValueProvider.Maximum
        {
            get
            {
                return (double) 100;
            }
        }

        double IRangeValueProvider.Minimum
        {
            get
            {
                return (double) 0;
            }
        }

        double IRangeValueProvider.SmallChange
        {
            get
            {
                return Double.NaN;
            }
        }

        double IRangeValueProvider.LargeChange
        {
            get
            {
                return Double.NaN;
            }
        }

        #endregion

        //------------------------------------------------------
        //
        //  Private Methods
        //
        //------------------------------------------------------

        #region Private Methods

        #region Value pattern helper

        private int ValuePercent
        {
            get
            {
                int cur = Misc.ProxySendMessageInt(_hwnd, NativeMethods.PBM_GETPOS, IntPtr.Zero, IntPtr.Zero);
                int min = Misc.ProxySendMessageInt(_hwnd, NativeMethods.PBM_GETRANGE, new IntPtr(1), IntPtr.Zero);
                int max = Misc.ProxySendMessageInt(_hwnd, NativeMethods.PBM_GETRANGE, IntPtr.Zero, IntPtr.Zero);

                int actualCur = cur - min;
                int range = max - min;

                return ((range != 0) ? (int)((100 * actualCur + range / 2) / range) : 0);
            }
        }

        #endregion

        #endregion
    }
}
