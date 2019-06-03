// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// Description: Win32 IP Common Control proxy
//


using System;
using System.Net;
using System.Text;
using System.ComponentModel;
using System.Globalization;
using System.Windows.Automation;
using System.Windows.Automation.Provider;
using System.Windows;
using MS.Win32;

namespace MS.Internal.AutomationProxies
{
    class WindowsIPAddress: ProxyHwnd, IRawElementProviderHwndOverride, IValueProvider, IGridProvider
    {
        // ------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------

        #region Constructors

        WindowsIPAddress (IntPtr hwnd, ProxyFragment parent, int item)
            : base( hwnd, parent, item )
        {
            // IP Address control itself is custom so need to also return LocalizedControlType property
            _cControlType = ControlType.Custom;
            _sType = SR.Get( SRID.LocalizedControlTypeIPAddress ); ;
            _fIsKeyboardFocusable = true;

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

            return new WindowsIPAddress(hwnd, null, 0);
        }

        internal static void RaiseEvents (IntPtr hwnd, int eventId, object idProp, int idObject, int idChild)
        {
            if (idObject != NativeMethods.OBJID_VSCROLL && idObject != NativeMethods.OBJID_HSCROLL)
            {
                ProxySimple el = new WindowsIPAddress (hwnd, null, 0);

                el.DispatchEvents (eventId, idProp, idObject, idChild);
            }
        }

        #endregion Proxy Create

        //------------------------------------------------------
        //
        //  Patterns Implementation
        //
        //------------------------------------------------------

        #region ProxySimple Interface

        // Returns a pattern interface if supported.
        internal override object GetPatternProvider (AutomationPattern iid)
        {
            return iid == ValuePattern.Pattern || iid == GridPattern.Pattern ? this : null;
        }

        #endregion

        #region ProxyFragment Interface

        // Returns a Proxy element corresponding to the specified screen coordinates.
        internal override ProxySimple ElementProviderFromPoint (int x, int y)
        {
            // let UIAutomation do the drilling
            return null;
        }

        #endregion ProxyFragment Interface

        #region IRawElementProviderHwndOverride

        IRawElementProviderSimple IRawElementProviderHwndOverride.GetOverrideProviderForHwnd (IntPtr hwnd)
        {
            // Find location of passed in hwnd under the parent
            int index = GetIndexOfChildWindow (hwnd);
            System.Diagnostics.Debug.Assert (index != -1, "GetOverrideProviderForHwnd: cannot find child hwnd index");
            return new ByteEditBoxOverride (hwnd, index);
        }

        #endregion IRawElementProviderHwndOverride

        #region Value Pattern

        // Sets the IP Address.
        void IValueProvider.SetValue (string val)
        {
            // Make sure that the control is enabled
            if (!SafeNativeMethods.IsWindowEnabled(_hwnd))
            {
                throw new ElementNotEnabledException();
            }

            IPAddress ipAddress = GetIPAddressFromString (val);

            if (ipAddress != null)
            {
                byte[] abOctet = ipAddress.GetAddressBytes();

                if (abOctet.Length == 4)
                {
                    uint ipV4 = 0;

                    for (int iPos = 0; iPos < 4; iPos++)
                    {
                        ipV4 = (ipV4 << 8) + abOctet[iPos];
                    }

                    // no return result for this message, so if it get sent it must have succeeded
                    Misc.ProxySendMessage(_hwnd, NativeMethods.IPM_SETADDRESS, IntPtr.Zero, (IntPtr)unchecked((int)ipV4));
                    return;
                }
            }

            // this was no a valid IP address
            throw new InvalidOperationException (SR.Get(SRID.OperationCannotBePerformed));
        }


        // Request to get the value that this UI element is representing as a string
        string IValueProvider.Value
        {
            get
            {
                return Misc.ProxyGetText(_hwnd, IP_ADDRESS_STRING_LENGTH);
            }
        }

        bool IValueProvider.IsReadOnly
        {
            get
            {
                return !Misc.IsEnabled(_hwnd);
            }
        }

        #endregion

        #region Grid Pattern

        // Obtain the AutomationElement at an zero based absolute position in the grid.
        // Where 0,0 is top left
        IRawElementProviderSimple IGridProvider.GetItem(int row, int column)
        {
            // NOTE: IPAddress has only 1 row
            if (row != 0)
            {
                throw new ArgumentOutOfRangeException("row", row, SR.Get(SRID.GridRowOutOfRange));
            }

            if (column < 0 || column >= OCTETCOUNT)
            {
                throw new ArgumentOutOfRangeException("column", column, SR.Get(SRID.GridColumnOutOfRange));
            }

            // Note: GridItem position is in reverse from the hwnd position
            // take this into account
            column = OCTETCOUNT - (column + 1);
            IntPtr hwndChild = GetChildWindowFromIndex(column);
            if (hwndChild != IntPtr.Zero)
            {
                return new ByteEditBoxOverride(hwndChild, column);
            }
            return null;
        }

        int IGridProvider.RowCount
        {
            get
            {
                return 1;
            }
        }

        int IGridProvider.ColumnCount
        {
            get
            {
                return OCTETCOUNT;
            }
        }

        #endregion Grid Pattern

        //------------------------------------------------------
        //
        //  Private Methods
        //
        //------------------------------------------------------

        #region Private Method

        private IPAddress GetIPAddressFromString (String strIPAddress)
        {
            String [] strIPAddresses = strIPAddress.Split (IP_ADDRESS_SEPERATOR);

            if (strIPAddresses.Length != 4)
            {
                return null;
            }

            uint ipV4 = 0;
            for (int iPos = 3; iPos >= 0; iPos--)
            {
                ipV4 = (ipV4 << 8) + byte.Parse(strIPAddresses[iPos], CultureInfo.InvariantCulture);
            }

            return new IPAddress ((long) ipV4);
        }

        // Index or -1 (if not found)
        private int GetIndexOfChildWindow (IntPtr target)
        {
            int index = 0;
            IntPtr hwndChild = Misc.GetWindow(_hwnd, NativeMethods.GW_CHILD);
            while (hwndChild != IntPtr.Zero)
            {
                if (hwndChild == target)
                {
                    return index;
                }

                index++;
                hwndChild = Misc.GetWindow(hwndChild, NativeMethods.GW_HWNDNEXT);
            }
            return -1;
        }

        // get child window at index (0-based). IntPtr.Zero if not found
        private IntPtr GetChildWindowFromIndex (int index)
        {
            IntPtr hwndChild = Misc.GetWindow(_hwnd, NativeMethods.GW_CHILD);
            for (int i = 0; ((i < index) && (hwndChild != IntPtr.Zero)); i++)
            {
                hwndChild = Misc.GetWindow(hwndChild, NativeMethods.GW_HWNDNEXT);
            }
            return hwndChild;
        }
        
        #endregion Private Method

        //------------------------------------------------------
        //
        //  Private Fields
        //
        //------------------------------------------------------

        #region Private Fields

        private const int IP_ADDRESS_STRING_LENGTH = 16;
        private const char IP_ADDRESS_SEPERATOR = '.';
        internal const int OCTETCOUNT = 4;

        #endregion Private Fields
    }

    // ------------------------------------------------------
    //
    //  ByteEditBoxOverride Private Class
    //
    //------------------------------------------------------

    #region ByteEditBoxOverride

    // Placeholder/Extra Pattern provider for OctetEditBox
    class ByteEditBoxOverride : ProxyHwnd, IGridItemProvider, IRangeValueProvider
    {
        // ------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------

        #region Constructors

        internal ByteEditBoxOverride(IntPtr hwnd, int position) : 
                base(hwnd, null, 0)
        {
            _sType = SR.Get(SRID.LocalizedControlTypeOctet);
            _position = position;
            _sAutomationId = "Octet " + position.ToString(CultureInfo.CurrentCulture); // This string is a non-localizable string
            _fIsKeyboardFocusable = true;
        }

        #endregion Constructors

        //------------------------------------------------------
        //
        //  Patterns Implementation
        //
        //------------------------------------------------------

        #region ProxySimple Interface

        internal override ProviderOptions ProviderOptions
        {
            get
            {
                return base.ProviderOptions | ProviderOptions.OverrideProvider;
            }
        }

        // Returns a pattern interface if supported.
        internal override object GetPatternProvider(AutomationPattern iid)
        {
            if (GridItemPattern.Pattern == iid)
            {
                return this;
            }

            if (RangeValuePattern.Pattern == iid)
            {
                return this;
            }
            return null;
        }

        // Gets the localized name
        internal override string LocalizedName
        {
            get
            {
                return Misc.ProxyGetText(_hwnd);
            }
        }

        #endregion

        #region  RangeValue Pattern

        // Sets the one of the field of the IP Address.
        void IRangeValueProvider.SetValue(double val)
        {
            // Make sure that the control is enabled
            if (!SafeNativeMethods.IsWindowEnabled(_hwnd))
            {
                throw new ElementNotEnabledException();
            }

            int i = (int)val;

            // Check range
            if (i > 255)
            {
                throw new ArgumentOutOfRangeException("value", val, SR.Get(SRID.RangeValueMax));
            }
            if (i < 0)
            {
                throw new ArgumentOutOfRangeException("value", val, SR.Get(SRID.RangeValueMin));
            }

            // Set text
            Misc.ProxySendMessage(_hwnd, NativeMethods.WM_SETTEXT, IntPtr.Zero, new StringBuilder(i.ToString(CultureInfo.CurrentCulture)));
        }

        // Request to get the value that this UI element is representing in a native format
        double IRangeValueProvider.Value
        {
            get
            {
                string s = WindowsEditBox.Text(_hwnd);
                if (string.IsNullOrEmpty(s))
                {
                    return double.NaN;
                }
                return double.Parse(s, CultureInfo.CurrentCulture);
            }
        }

        bool IRangeValueProvider.IsReadOnly
        {
            get
            {
                return !SafeNativeMethods.IsWindowEnabled(_hwnd);
            }
        }

        double IRangeValueProvider.Maximum
        {
            get
            {
                return 255.0;
            }
        }

        double IRangeValueProvider.Minimum
        {
            get
            {
                return 0.0;
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

        #region GridItem Pattern

        int IGridItemProvider.Row
        {
            get
            {
                return 0;
            }
        }
        int IGridItemProvider.Column
        {
            get
            {
                // Note hwnd locations are in reverse
                // we need to ajust columns accordnigly
                return WindowsIPAddress.OCTETCOUNT - 1 - _position;
            }
        }
        int IGridItemProvider.RowSpan
        {
            get
            {
                return 1;
            }
        }
        int IGridItemProvider.ColumnSpan
        {
            get
            {
                return 1;
            }
        }
        IRawElementProviderSimple IGridItemProvider.ContainingGrid
        {
            get
            {
                return WindowsIPAddress.Create(Misc.GetParent(_hwnd), 0, 0);
            }
        }


        #endregion GridItem Pattern

        //------------------------------------------------------
        //
        //  Private Fields
        //
        //------------------------------------------------------

        #region Private Fields

        // location in the IP
        private int _position;

        #endregion Private Fields
    }

    #endregion
}

