// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// Description: Win32 Spinner Proxy

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
    // Proxy for a Windows NumericUpDown control with Windows Edit control, called a Spinner
    class WindowsSpinner : ProxyHwnd, IRangeValueProvider
    {
        // ------------------------------------------------------
        //
        // Constructors
        //
        // ------------------------------------------------------

        #region Constructors 

        // Contructor for WindownsSpinner class. Calls the base class constructor.
        internal WindowsSpinner(IntPtr hwndUpDown, IntPtr hwndEdit, ProxyFragment parent, int item)
            : base(hwndUpDown, parent, item)
        {
            _elEdit = new WindowsEditBox(hwndEdit, this, 0);
            _elUpDown = new WindowsUpDown(hwndUpDown, this, 0);

            // Set the strings to return properly the properties.
            _cControlType = ControlType.Spinner;

            // support for events
            _createOnEvent = new WinEventTracker.ProxyRaiseEvents(RaiseEvents);
        }

        #endregion Constructors

        #region Proxy Create 

        // Static Create method called by UIAutomation to create this proxy.
        // returns null if unsuccessful
        internal static IRawElementProviderSimple Create(IntPtr hwnd, int idChild, int idObject)
        {
            return Create(hwnd, idChild);
        }

        internal static IRawElementProviderSimple Create(IntPtr hwnd, int idChild)
        {
            // Something is wrong if idChild is not zero 
            if (idChild != 0)
            {
                System.Diagnostics.Debug.Assert (idChild == 0, "Invalid Child Id, idChild != 0");
                throw new ArgumentOutOfRangeException("idChild", idChild, SR.Get(SRID.ShouldBeZero));
            }

            IntPtr hwndBuddy;
            try
            {
                // Only use spinner class if we find a buddy that's an EDIT, otherwise return
                // null to defer to the WindowsUpDown entry in the proxy table
                hwndBuddy = Misc.ProxySendMessage(hwnd, NativeMethods.UDM_GETBUDDY, IntPtr.Zero, IntPtr.Zero);
                if (hwndBuddy == IntPtr.Zero)
                {
                    return null;
                }

                if (Misc.ProxyGetClassName(hwndBuddy).IndexOf("EDIT", StringComparison.OrdinalIgnoreCase) == -1)
                {
                    return null;
                }
            }
            catch (ElementNotAvailableException)
            {
                return null;
            }

            return new WindowsSpinner(hwnd, hwndBuddy, null, 0);
        }

        // Static Create method called by the event tracker system
        internal static void RaiseEvents(IntPtr hwnd, int eventId, object idProp, int idObject, int idChild)
        {
            if (idObject == NativeMethods.OBJID_VSCROLL || idObject == NativeMethods.OBJID_HSCROLL)
            {
                return;
            }

            // ChildId will be non-0 if the event is due to operating on the updown part of the spinner. 
            // Events on non-content parts of a control are not necessary so don't raise an event in that case.
            if ( idChild != 0 )
            {
                return;
            }

            ProxySimple ps = (ProxySimple)Create( hwnd, idChild, idObject );
            if ( ps == null )
                return;
            ps.DispatchEvents( eventId, idProp, idObject, idChild );
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
            return (iid == RangeValuePattern.Pattern) ? this : null;
        }

        // Gets the bounding rectangle for this element
        internal override Rect BoundingRectangle
        {
            get
            {
                Rect rcUpDown = _elUpDown.BoundingRectangle;
                Rect rcEdit = _elEdit.BoundingRectangle;
                return Rect.Union(rcEdit, rcUpDown);
            }
        }

        internal override object GetElementProperty(AutomationProperty idProp)
        {
            if (idProp == AutomationElement.ClickablePointProperty)
            {
                return _elEdit.GetElementProperty(idProp);
            }
            else if (idProp == AutomationElement.HasKeyboardFocusProperty)
            {
                return _elEdit.GetElementProperty(idProp);
            }

            return base.GetElementProperty(idProp);
        }

        #endregion ProxySimple Interface

        #region ProxyFragment Interface

        // Returns a Proxy element corresponding to the specified screen coordinates.
        internal override ProxySimple ElementProviderFromPoint(int x, int y)
        {
            Rect rcUpDown = _elUpDown.BoundingRectangle;
            Rect rcEdit = _elEdit.BoundingRectangle;
            Point pt = new Point(x, y);

            if (rcUpDown.Contains(pt))
            {
                return _elUpDown.ElementProviderFromPoint(x, y);
            }
            else if (rcEdit.Contains(pt))
            {
                return this;
            }

            return base.ElementProviderFromPoint(x, y);
        }

        // Returns the next sibling element in the raw hierarchy.
        // Peripheral controls have always negative values.
        // Returns null if no next child.
        internal override ProxySimple GetNextSibling(ProxySimple child)
        {
            return _elUpDown.GetNextSibling(child);
        }

        // Returns the previous sibling element in the raw hierarchy.
        // Peripheral controls have always negative values.
        // Returns null is no previous.
        internal override ProxySimple GetPreviousSibling(ProxySimple child)
        {
            return _elUpDown.GetPreviousSibling(child);
        }

        // Returns the first child element in the raw hierarchy.
        internal override ProxySimple GetFirstChild()
        {
            return _elUpDown.GetFirstChild();
        }

        // Returns the last child element in the raw hierarchy.
        internal override ProxySimple GetLastChild()
        {
            return _elUpDown.GetLastChild();
        }

        #endregion

        #region RangeValue Pattern

        // Sets a new position for the edit part of the spinner.
        void IRangeValueProvider.SetValue (double obj)
        {
            ((IRangeValueProvider)_elUpDown).SetValue(obj);
        }

        // Request to get the value that this UI element is representing in a native format
        double IRangeValueProvider.Value
        {
            get
            {
                return ((IRangeValueProvider)_elUpDown).Value;
            }
        }

        bool IRangeValueProvider.IsReadOnly
        {
            get
            {
                return (bool)((IRangeValueProvider)_elUpDown).IsReadOnly && 
                       (bool)((IValueProvider)_elEdit).IsReadOnly;
            }
        }

        double IRangeValueProvider.Maximum
        {
            get
            {
                return ((IRangeValueProvider) _elUpDown).Maximum;
            }
        }

        double IRangeValueProvider.Minimum
        {
            get
            {
                return ((IRangeValueProvider) _elUpDown).Minimum;
            }
        }

        double IRangeValueProvider.SmallChange
        {
            get
            {
                return ((IRangeValueProvider)_elUpDown).SmallChange;
            }
        }

        double IRangeValueProvider.LargeChange
        {
            get
            {
                return ((IRangeValueProvider)_elUpDown).LargeChange;
            }
        }
        #endregion RangeValuePattern

        // ------------------------------------------------------
        //
        // Internal methods                                             
        //
        // ------------------------------------------------------

        #region Internal Methods

        // if there is an UpDown control that its buddy window is the same as the given edit hwnd
        // the edit is part of the spinner.
        internal static bool IsSpinnerEdit(IntPtr hwnd)
        {
            return GetUpDownFromEdit(hwnd) != IntPtr.Zero;
        }

        // Try to find a sibling of the edit control that is an UpDown control that has the
        // edit contol as its buddy window.  If one is found return the hwnd of the UpDown control.
        internal static IntPtr GetUpDownFromEdit(IntPtr hwnd)
        {
            IntPtr hwndParent = Misc.GetParent(hwnd);

            if (hwndParent != IntPtr.Zero)
            {
                IntPtr hwndChild = Misc.GetWindow(hwndParent, NativeMethods.GW_CHILD);
                while (hwndChild != IntPtr.Zero)
                {
                    string className = Misc.ProxyGetClassName(hwndChild);
                    if (className.IndexOf("msctls_updown32", StringComparison.OrdinalIgnoreCase) != -1)
                    {
                        IntPtr hwndBuddy = Misc.ProxySendMessage(hwndChild, NativeMethods.UDM_GETBUDDY, IntPtr.Zero, IntPtr.Zero);
                        if (hwnd == hwndBuddy)
                        {
                            return hwndChild;
                        }
                    }
                    hwndChild = Misc.GetWindow(hwndChild, NativeMethods.GW_HWNDNEXT);
                }
            }

            return IntPtr.Zero;
        }

        #endregion Internal Methods

        // ------------------------------------------------------
        //
        // Private Fields                                             
        //
        // ------------------------------------------------------

        #region Private Fields

        private WindowsEditBox _elEdit;
        private WindowsUpDown _elUpDown;

        #endregion
    }
}
