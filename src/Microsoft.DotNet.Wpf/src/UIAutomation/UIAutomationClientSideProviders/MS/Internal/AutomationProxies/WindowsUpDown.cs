// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// Description: Win32 Up/Down proxy

using System;
using System.Windows;
using System.Collections;
using System.ComponentModel;
using System.Windows.Automation;
using System.Windows.Automation.Provider;
using System.Text;
using System.Runtime.InteropServices;
using MS.Win32;
using NativeMethodsSetLastError = MS.Internal.UIAutomationClientSideProviders.NativeMethodsSetLastError;

namespace MS.Internal.AutomationProxies
{
    class WindowsUpDown : ProxyHwnd, IRangeValueProvider
    {
        // ------------------------------------------------------
        //
        // Constructors
        //
        // ------------------------------------------------------

        #region Constructors

        // Contructor for SpinControlProxy class. Calls the base class constructor.
        internal WindowsUpDown (IntPtr hwnd, ProxyFragment parent, int item)
            : base (hwnd, parent, item)
        {
            // Set the strings to return properly the properties.
            _cControlType = ControlType.Spinner;

            // support for events
            _createOnEvent = new WinEventTracker.ProxyRaiseEvents (RaiseEvents);

            // if spin is embedded in a tab control exclude from the content view.
            _fIsContent = !IsInsideOfTab();
        }

        #endregion

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

            return new WindowsUpDown(hwnd, null, idChild);
        }

        // Called by the event tracker system.
        internal static void RaiseEvents (IntPtr hwnd, int eventId, object idProp, int idObject, int idChild)
        {
            if(idObject == NativeMethods.OBJID_CLIENT
                && eventId == NativeMethods.EventObjectInvoke
                && idProp == InvokePattern.InvokedEvent)
            {
                RaiseInvokedEvent(hwnd, idObject, idChild);
            }
            else if (idObject != NativeMethods.OBJID_VSCROLL && idObject != NativeMethods.OBJID_HSCROLL)
            {
                WindowsUpDown wtv = new WindowsUpDown (hwnd, null, -1);
                wtv.DispatchEvents (eventId, idProp, idObject, idChild);
            }
        }

        private static void RaiseInvokedEvent(IntPtr hwnd, int idObject, int idChild)
        {
            ProxySimple button = null;
            if (idChild == 1)
            {
                WindowsUpDown wtv = new WindowsUpDown(hwnd, null, -1);
                button = wtv.CreateSpinButtonItem(SpinItem.DownArrow);
            }
            else if (idChild == 2)
            {
                WindowsUpDown wtv = new WindowsUpDown(hwnd, null, -1);
                button = wtv.CreateSpinButtonItem(SpinItem.UpArrow);
            }
            if (button != null)
            {
                button.DispatchEvents(NativeMethods.EventObjectInvoke, InvokePattern.InvokedEvent, idObject, idChild);
            }
        }

        // Creates a list item RawElementBase Item
        private ProxySimple CreateSpinButtonItem (SpinItem item)
        {
            return new SpinButtonItem(_hwnd, IsSpinnerElement()? _parent : this, (int)item);
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

        internal override object GetElementProperty(AutomationProperty idProp)
        {
            if (idProp == AutomationElement.IsControlElementProperty)
            {
                // Hide spin portion in the logical tree
                // in the case when it is embedded inside of a winforms spinner
                if (WindowsFormsHelper.IsWindowsFormsControl(_hwnd) && IsWinformUpdown(_hwnd))
                {
                    return false;
                }
            }

            return base.GetElementProperty(idProp);
        }

        //Gets the localized name
        internal override string LocalizedName
        {
            get
            {
                return SR.Get(SRID.LocalizedNameWindowsUpDown);
            }
        }

        #endregion

        #region ProxyFragment Interface

        // Returns the next sibling element in the raw hierarchy.
        // Peripheral controls have always negative values.
        // Returns null if no next child
        internal override ProxySimple GetNextSibling (ProxySimple child)
        {
            // Determine how many items are in the list view.
            if (child._item == (int)SpinItem.DownArrow)
            {
                return CreateSpinButtonItem (SpinItem.UpArrow);
            }

            return null;
        }

        // Returns the previous sibling element in the raw hierarchy.
        // Peripheral controls have always negative values.
        // Returns null is no previous child.
        internal override ProxySimple GetPreviousSibling (ProxySimple child)
        {
            if (child._item == (int)SpinItem.UpArrow)
            {
                return CreateSpinButtonItem (SpinItem.DownArrow);
            }

            return null;
        }

        // Returns the first child element in the raw hierarchy.
        internal override ProxySimple GetFirstChild ()
        {
            return CreateSpinButtonItem (SpinItem.DownArrow);
        }

        // Returns the last child element in the raw hierarchy.
        internal override ProxySimple GetLastChild ()
        {
            return CreateSpinButtonItem (SpinItem.UpArrow);
        }

        // Returns a Proxy element corresponding to the specified screen coordinates.
        internal override ProxySimple ElementProviderFromPoint (int x, int y)
        {
            for (SpinItem item = SpinItem.DownArrow; item <= SpinItem.UpArrow; item++)
            {
                NativeMethods.Win32Rect rc = new NativeMethods.Win32Rect (SpinButtonItem.GetBoundingRectangle (_hwnd, item));

                if (Misc.PtInRect(ref rc, x, y))
                {
                    return CreateSpinButtonItem (item);
                }
            }

            return this;
        }

        #endregion

        #region RangeValue Pattern

        // Change the position of the Up/Down
        void IRangeValueProvider.SetValue (double val)
        {
            // Make sure that the control is enabled
            if (!SafeNativeMethods.IsWindowEnabled (_hwnd))
            {
                throw new ElementNotEnabledException();
            }

            if (double.IsNaN(val))
            {
                throw new ArgumentException(SR.Get(SRID.InvalidParameter));
            }

            if (val > Max)
            {
                throw new ArgumentOutOfRangeException("value", val, SR.Get(SRID.RangeValueMax));
            }
            else if (val < Min)
            {
                throw new ArgumentOutOfRangeException("value", val, SR.Get(SRID.RangeValueMin));
            }

            short newPos = Convert.ToInt16(val);
            Misc.ProxySendMessage(_hwnd, NativeMethods.UDM_SETPOS, IntPtr.Zero, NativeMethods.Util.MAKELPARAM(newPos, 0));

            // Scroll the buddy
            Misc.ProxySendMessage(HwndBuddy(_hwnd), NativeMethods.WM_HSCROLL, NativeMethods.Util.MAKELPARAM(NativeMethods.SB_THUMBPOSITION, newPos), IntPtr.Zero);
        }

        // Request to get the value that this UI element is representing in a native format
        double IRangeValueProvider.Value
        {
            get
            {
                return Pos;
            }
        }

        bool IRangeValueProvider.IsReadOnly
        {
            get
            {
                return false;
            }
        }

        double IRangeValueProvider.Maximum
        {
            get
            {
                return Max;
            }
        }

        double IRangeValueProvider.Minimum
        {
            get
            {
                return Min;
            }
        }

        double IRangeValueProvider.SmallChange
        {
            get
            {
                return 1.0;
            }
        }

        double IRangeValueProvider.LargeChange
        {
            get
            {
                return Double.NaN;
            }
        }
        #endregion RangeValuePattern

        //------------------------------------------------------
        //
        //  Internal Methods
        //
        //------------------------------------------------------

        #region Internal Methods

        internal bool IsInsideOfTab()
        {
            IntPtr hwndParent = NativeMethodsSetLastError.GetAncestor(_hwnd, NativeMethods.GA_PARENT);

            if (hwndParent != IntPtr.Zero)
            {
                // Test for tab control
                return Misc.ProxyGetClassName(hwndParent).Contains("SysTabControl32");
            }

            return false;
        }

        // Method that verifies if window or one of its intermediate children (in terms of IAccessible tree) is a Spinner
        internal static bool IsWinformUpdown (IntPtr hwnd)
        {
            Accessible acc = null;
            int hr = Accessible.AccessibleObjectFromWindow(hwnd, NativeMethods.OBJID_CLIENT, ref acc);

            // Verify the role
            return hr == NativeMethods.S_OK && acc != null ? acc.Role == AccessibleRole.SpinButton : false;
        }

        #endregion

        //------------------------------------------------------
        //
        //  Internal Fields
        //
        //------------------------------------------------------

        #region Internal Fields

        internal enum SpinItem
        {
            DownArrow = 0,
            UpArrow = 1,
        }

        #endregion

        //------------------------------------------------------
        //
        //  Private Methods
        //
        //------------------------------------------------------

        #region Private Methods

        private double Pos
        {
            get
            {
                int pos = Misc.ProxySendMessageInt(_hwnd, NativeMethods.UDM_GETPOS, IntPtr.Zero, IntPtr.Zero);

                // From the doc, If successful, the high-order word is set to zero and the
                // low-order word is set to the control's current position. If
                // an error occurs, the high-order word is set to a nonzero value.
                // However as often the high word is set to 1 but the value is ok, ignore the
                // error code and just return the pos.
                return (double)NativeMethods.Util.LOWORD(pos);
            }
        }

        private static IntPtr HwndBuddy(IntPtr hwnd)
        {
            IntPtr hwndBuddy = Misc.ProxySendMessage(hwnd, NativeMethods.UDM_GETBUDDY, IntPtr.Zero, IntPtr.Zero);

            // if no buddy window, then all notifications are sent to the parent
            if (hwndBuddy == IntPtr.Zero)
            {
                hwndBuddy = Misc.GetParent(hwnd);
            }
            return hwndBuddy;
        }

        private bool IsSpinnerElement()
        {
            // If this is a Spinner UpDown Control, the buddy window should be a control with
            // the class of EDIT.
            IntPtr hwndBuddy = HwndBuddy(_hwnd);
            return hwndBuddy != IntPtr.Zero && Misc.ProxyGetClassName(hwndBuddy).IndexOf("EDIT", StringComparison.OrdinalIgnoreCase) != -1;
        }

        private double Max
        {
            get
            {
                // The low-order word is the maximum position for the control, and the
                // high-order word is the minimum position.
                int range = Misc.ProxySendMessageInt(_hwnd, NativeMethods.UDM_GETRANGE, IntPtr.Zero, IntPtr.Zero);
                int min = NativeMethods.Util.HIWORD(range);
                int max = NativeMethods.Util.LOWORD(range);

                return (double)(max > min ? max : min);
            }
        }

        private double Min
        {
            get
            {
                // The low-order word is the maximum position for the control, and the
                // high-order word is the minimum position.
                int range = Misc.ProxySendMessageInt(_hwnd, NativeMethods.UDM_GETRANGE, IntPtr.Zero, IntPtr.Zero);
                int min = NativeMethods.Util.HIWORD(range);
                int max = NativeMethods.Util.LOWORD(range);

                return (double)(max > min ? min : max);
            }
        }

        #endregion

        //------------------------------------------------------
        //
        //  SpinButtonItem Private Class
        //
        //------------------------------------------------------

        #region SpinButtonItem

        class SpinButtonItem: ProxySimple, IInvokeProvider
        {
            //------------------------------------------------------
            //
            //  Constructors
            //
            //------------------------------------------------------

            #region Constructors

            // Contructor for SpinControlProxy class. Calls the base class constructor.
            internal SpinButtonItem (IntPtr hwnd, ProxyFragment parent, int item)
                : base(hwnd, parent, item)
            {
                // Set the strings to return properly the properties.
                _fIsContent = false;

                _cControlType = ControlType.Button;

                WindowsUpDown upDownParent = parent as WindowsUpDown;
                if (upDownParent != null)
                {
                    _isInsideOfTab = upDownParent.IsInsideOfTab();
                }

                // The buttons are swapped on a tab control compared to the spinner.
                if (_isInsideOfTab)
                {
                    item = 1 - item;
                }

                _sAutomationId = _asAutomationId[item];
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
                return iid == InvokePattern.Pattern ? this : null;
            }

            // Gets the bounding rectangle for this element
            internal override Rect BoundingRectangle
            {
                get
                {
                    return GetBoundingRectangle(_hwnd, (WindowsUpDown.SpinItem)_item);
                }
            }

            // Process all the Logical and Raw Element Properties
            internal override object GetElementProperty(AutomationProperty idProp)
            {
                if (idProp == AutomationElement.IsControlElementProperty)
                {
                    IntPtr hwndTabParent = GetTabParent();
                    if (hwndTabParent != IntPtr.Zero)
                    {
                        return WindowsTab.IsValidControl(hwndTabParent);
                    }
                }

                return base.GetElementProperty(idProp);
            }

            //Gets the localized name
            internal override string LocalizedName
            {
                get
                {
                    int item = _item;

                    // The buttons are swapped on a tab control compared to the spinner.
                    if (_isInsideOfTab)
                    {
                        item = 1 - item;
                    }

                    return SR.Get(_asNames[item]);
                }
            }

            #endregion ProxySimple Interface

            #region Invoke Pattern

            // Same as a click on one of the button Up or Down
            void IInvokeProvider.Invoke ()
            {
                // Make sure that the control is enabled
                if (!SafeNativeMethods.IsWindowEnabled(_hwnd))
                {
                    throw new ElementNotEnabledException();
                }

                // NOTE: The GetBoundingRectangel() will swap the buttons to retrieve
                // the correct rectangle based on the WS_EX_LAYOUTRTL bit.  But the
                // SendMessages WM_LBUTTONDOWN and WM_LBUTTONUP also swaps the buttons
                // on the WS_EX_LAYOUTRTL bit.  So need to send the center point of
                // button before the swap to get the SendMessage to apply it to the
                // correct button.

                int item = _item;
                // If the control is horizontal and the WS_EX_LAYOUTRTL is set need to
                // swap the button order
                if (IsHorizontal(_hwnd) && Misc.IsLayoutRTL(_hwnd))
                {
                    item = 1 - item;
                }

                // does the control have vertical scrolling buttons
                Rect rc = GetBoundingRectangle(_hwnd, (WindowsUpDown.SpinItem)item);
                NativeMethods.Win32Rect updownRect = new NativeMethods.Win32Rect();

                if (!Misc.GetWindowRect(_hwnd, ref updownRect))
                {
                    throw new InvalidOperationException(SR.Get(SRID.OperationCannotBePerformed));
                }

                int x = (int) rc.Left - updownRect.left + (int) rc.Width / 2;
                int y = (int) rc.Top - updownRect.top + (int) rc.Height / 2;
                IntPtr center = NativeMethods.Util.MAKELPARAM (x, y);

                // the message does not seems to operate, fake a mouse action instead
                Misc.ProxySendMessage(_hwnd, NativeMethods.WM_LBUTTONDOWN, (IntPtr)NativeMethods.MK_LBUTTON, center);
                Misc.ProxySendMessage(_hwnd, NativeMethods.WM_LBUTTONUP, (IntPtr)NativeMethods.MK_LBUTTON, center);
            }

            #endregion Invoke Pattern

            //------------------------------------------------------
            //
            //  Internal Methods
            //
            //------------------------------------------------------

            #region Internal Methods

            static internal Rect GetBoundingRectangle(IntPtr hwnd, WindowsUpDown.SpinItem item)
            {
                NativeMethods.Win32Rect updownRect = new NativeMethods.Win32Rect();

                if (!Misc.GetWindowRect(hwnd, ref updownRect))
                {
                    return Rect.Empty;
                }

                bool fHorz = IsHorizontal(hwnd);

                // If the control is horizontal and the WS_EX_LAYOUTRTL is set need to
                // swap the button order
                if (fHorz && Misc.IsLayoutRTL(hwnd))
                {
                    item = item == SpinItem.DownArrow ? SpinItem.UpArrow : SpinItem.DownArrow;
                }

                switch (item)
                {
                    case WindowsUpDown.SpinItem.DownArrow:
                        if (fHorz)
                        {
                            int width = (updownRect.right - updownRect.left);
                            updownRect.right = updownRect.left + width / 2;
                        }
                        else
                        {
                            int height = (updownRect.bottom - updownRect.top);
                            updownRect.bottom = updownRect.top + height / 2;
                        }
                        // Don't need to normalize, GetWindowRect returns screen coordinates.
                        return updownRect.ToRect(false);

                    case WindowsUpDown.SpinItem.UpArrow:
                        if (fHorz)
                        {
                            int width = (updownRect.right - updownRect.left);
                            updownRect.left = updownRect.left + width / 2;
                        }
                        else
                        {
                            int height = (updownRect.bottom - updownRect.top);
                            updownRect.top = updownRect.top + height / 2;
                        }
                        // Don't need to normalize, GetWindowRect returns screen coordinates.
                        return updownRect.ToRect(false);
                }

                return Rect.Empty;
            }

            #endregion

            // ------------------------------------------------------
            //
            //  Private Methods
            //
            //------------------------------------------------------

            #region Private Methods

            private IntPtr GetTabParent()
            {
                IntPtr hwndParent = NativeMethodsSetLastError.GetAncestor(_hwnd, NativeMethods.GA_PARENT);

                if (hwndParent != IntPtr.Zero)
                {
                    // Test for tab control
                    hwndParent = Misc.ProxyGetClassName(hwndParent).Contains("SysTabControl32") ? hwndParent : IntPtr.Zero;
                }

                return hwndParent;
            }

            private static bool IsHorizontal(IntPtr hwnd)
            {
                return Misc.IsBitSet(Misc.GetWindowStyle(hwnd), NativeMethods.UDS_HORZ);
            }

            #endregion

            // ------------------------------------------------------
            //
            // Private Fields
            //
            // ------------------------------------------------------

            #region Private Fields

            private bool _isInsideOfTab;

            private static string [] _asNames = {
                SRID.LocalizedNameWindowsSpinButtonItemForward,
                SRID.LocalizedNameWindowsSpinButtonItemBackward
            };

            private static string[] _asAutomationId = new string[] {
                "SmallIncrement", "SmallDecrement"  // This string is a non-localizable string
            };

            #endregion
        }

        #endregion
    }
}
