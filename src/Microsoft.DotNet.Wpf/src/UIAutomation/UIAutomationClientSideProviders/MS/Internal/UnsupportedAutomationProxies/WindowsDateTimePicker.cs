// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// Description: Win32 WindowsDateTimePicker

using System;
using System.Text;
using System.Windows.Automation;
using System.Windows.Automation.Provider;
using System.Runtime.InteropServices;
using System.ComponentModel;
using System.Globalization;
using System.Windows;
using MS.Internal.AutomationProxies;
using MS.Win32;

namespace MS.Internal.UnsupportedAutomationProxies
{
    class WindowsDateTimePicker: ProxyHwnd, IValueProvider, IExpandCollapseProvider
    {
        // ------------------------------------------------------
        //
        // Constructors
        //
        // ------------------------------------------------------
        
        #region Constructors

        WindowsDateTimePicker (IntPtr hwnd, ProxyFragment parent, int item)
            : base( hwnd, parent, item )
        {
            // DateTimePicker is custom so need to also return LocalizedControlType property
            _cControlType = ControlType.Custom;
            _sType = SR.Get( SRID.LocalizedControlTypeDateTimePicker );

            // support for events
            _IsDropDownType = IsDropDownType ();

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

            return new WindowsDateTimePicker(hwnd, null, idChild);
        }

        // Static Create method called by the event tracker system
        internal static void RaiseEvents (IntPtr hwnd, int eventId, object idProp, int idObject, int idChild)
        {
            if (idObject != NativeMethods.OBJID_VSCROLL && idObject != NativeMethods.OBJID_HSCROLL)
            {
                WindowsDateTimePicker wdt = new WindowsDateTimePicker (hwnd, null, 0);

                if (eventId == NativeMethods.EventObjectNameChange)
                {
                    if (idProp == ValuePattern.ValueProperty && idObject == NativeMethods.OBJID_WINDOW)
                    {
                        // The dispatch method expects EventObjectValueChange for the Value change property
                        eventId = NativeMethods.EventObjectValueChange;
                        idObject = NativeMethods.OBJID_CLIENT;
                    }
                    else
                    {
                        // The name of the control should never change
                        return;
                    }
                }

                wdt.DispatchEvents (eventId, idProp, idObject, idChild);
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
            return iid == ValuePattern.Pattern || (iid == ExpandCollapsePattern.Pattern && _IsDropDownType) ? this : null;
        }

        // Gets the bounding rectangle for this element
        internal override Rect BoundingRectangle
        {
            get
            {
                IntPtr hwndCalendar;
                NativeMethods.Win32Rect rcDateTime = new NativeMethods.Win32Rect ();

                if (!Misc.GetWindowRect(_hwnd, ref rcDateTime))
                {
                    return Rect.Empty;
                }
                if (IsCalendarPopupVisible (_hwnd, out hwndCalendar))
                {
                    NativeMethods.Win32Rect rcCalendar = new NativeMethods.Win32Rect ();

                    if (Misc.GetWindowRect(hwndCalendar, ref rcCalendar))
                    {
                        Misc.UnionRect(out rcDateTime, ref rcDateTime, ref rcCalendar);
                    }
                }

                return new Rect (rcDateTime.left, rcDateTime.top, rcDateTime.right - rcDateTime.left, rcDateTime.bottom - rcDateTime.top);
            }
        }

        #endregion

        #region ProxyFragment Interface

        // Returns the next sibling element in the raw hierarchy.
        // Peripheral controls have always negative values.
        // Returns null if no next child
        internal override ProxySimple GetNextSibling (ProxySimple child)
        {
            DateTimeItem item = (DateTimeItem) child._item;
            IntPtr hwndCalendar;

            if (item == DateTimeItem.Button && IsCalendarPopupVisible (_hwnd, out hwndCalendar))
            {
                return new WindowsCalendar (hwndCalendar, this, (int) DateTimeItem.Calendar);
            }

            return null;
        }

        // Returns the previous sibling element in the raw hierarchy.
        // Peripheral controls have always negative values.
        // Returns null is no previous
        internal override ProxySimple GetPreviousSibling (ProxySimple child)
        {
            DateTimeItem item = (DateTimeItem) child._item;

            if (item == DateTimeItem.Calendar)
            {
                return new DateTimeButton (_hwnd, this, (int) DateTimeItem.Button);
            }

            return null;
        }

        // Returns the first child element in the raw hierarchy.
        internal override ProxySimple GetFirstChild ()
        {
            return _IsDropDownType ? new DateTimeButton (_hwnd, this, (int) DateTimeItem.Button) : null;
        }

        // Returns the last child element in the raw hierarchy.
        internal override ProxySimple GetLastChild ()
        {
            IntPtr hwndCalendar;

            if (IsCalendarPopupVisible (_hwnd, out hwndCalendar))
            {
                return new WindowsCalendar (hwndCalendar, this, (int) DateTimeItem.Calendar);
            }

            if (_IsDropDownType)
            {
                return new DateTimeButton (_hwnd, this, (int) DateTimeItem.Button);
            }

            return null;
        }

        // Returns a Proxy element corresponding to the specified screen coordinates.
        internal override ProxySimple ElementProviderFromPoint (int x, int y)
        {
            if (_IsDropDownType)
            {
                NativeMethods.Win32Rect rc = DateTimeButton.BoundingRect (_hwnd);
                if (Misc.PtInRect(ref rc, x, y))
                {
                    return new DateTimeButton (_hwnd, this, (int) DateTimeItem.Button);
                }
            }

            return null;
        }

        #endregion

        #region Value Pattern

        // Sets the text in the edit part of the Combo
        void IValueProvider.SetValue (string val)
        {
            // Make sure that the control is enabled
            if (!SafeNativeMethods.IsWindowEnabled(_hwnd))
            {
                throw new ElementNotEnabledException();
            }

            if (!SetValue(DateTime.Parse(val, CultureInfo.CurrentCulture)))
            {
                throw new InvalidOperationException(SR.Get(SRID.OperationCannotBePerformed));
            }
        }

        // Request to set the value that this UI element is representing as a string
        string IValueProvider.Value
        {
            get
            {
                int cLen = Misc.ProxySendMessageInt(_hwnd, NativeMethods.WM_GETTEXTLENGTH, IntPtr.Zero, IntPtr.Zero);
                if (cLen > 0)
                {
                    cLen++;
                    StringBuilder sb = new StringBuilder (cLen);

                    Misc.ProxySendMessage(_hwnd, NativeMethods.WM_GETTEXT, new IntPtr(cLen), sb);

                    return sb.ToString ();
                }

                return "";
            }
        }

        bool IValueProvider.IsReadOnly
        {
            get
            {
                return false;
            }
        }

        #endregion

        #region ExpandCollapse Pattern

        void IExpandCollapseProvider.Expand ()
        {
            // Make sure that the control is enabled
            if (!SafeNativeMethods.IsWindowEnabled(_hwnd))
            {
                throw new ElementNotEnabledException();
            }

            // check if item can be expanded
            switch (GetItemState (_hwnd))
            {
                case ExpandCollapseState.LeafNode :
                    {
                        throw new InvalidOperationException(SR.Get(SRID.OperationCannotBePerformed));
                    }

                case ExpandCollapseState.Expanded :
                    {
                        return;
                    }

                case ExpandCollapseState.Collapsed :
                    {
                        DateTimeButton btn = new DateTimeButton (_hwnd, this, (int) DateTimeItem.Button);
                        ((IInvokeProvider) btn).Invoke ();
                        return;
                    }
            }

            throw new InvalidOperationException(SR.Get(SRID.OperationCannotBePerformed));
        }

        void IExpandCollapseProvider.Collapse ()
        {
            // Make sure that the control is enabled
            if (!SafeNativeMethods.IsWindowEnabled(_hwnd))
            {
                throw new ElementNotEnabledException();
            }

            // check if item can be collapsed
            switch (GetItemState (_hwnd))
            {
                case ExpandCollapseState.LeafNode :
                    {
                        throw new InvalidOperationException(SR.Get(SRID.OperationCannotBePerformed));
                    }

                case ExpandCollapseState.Expanded :
                    {
                        DateTimeButton btn = new DateTimeButton (_hwnd, this, (int) DateTimeItem.Button);
                        ((IInvokeProvider) btn).Invoke ();
                        return;
                    }

                case ExpandCollapseState.Collapsed :
                    {
                        return;
                    }
            }

            throw new InvalidOperationException(SR.Get(SRID.OperationCannotBePerformed));
        }

        ExpandCollapseState IExpandCollapseProvider.ExpandCollapseState
        {
            get
            {
                return GetItemState (_hwnd);
            }
        }

        #endregion ExpandCollapse Pattern

        // ------------------------------------------------------
        //
        // Protected Methods
        //
        // ------------------------------------------------------

        #region Protected Methods

        // Picks a WinEvent to track for a UIA property
        protected override int [] PropertyToWinEvent (AutomationProperty idProp)
        {
            if (idProp == ValuePattern.ValueProperty)
            {
                return new int [] { NativeMethods.EventObjectNameChange };
            }
            return base.PropertyToWinEvent (idProp);
        }

        #endregion

        // ------------------------------------------------------
        //
        // Private methods
        //
        // ------------------------------------------------------

        #region Private Methods

        private static NativeMethods.SYSTEMTIME ConvertDateTimeToSystemTime (DateTime dateTime)
        {
            NativeMethods.SYSTEMTIME systemTime = new NativeMethods.SYSTEMTIME ();

            systemTime.wYear = (ushort) dateTime.Year;
            systemTime.wMonth = (ushort) dateTime.Month;
            systemTime.wDay = (ushort) dateTime.Day;
            systemTime.wHour = (ushort) dateTime.Hour;
            systemTime.wMinute = (ushort) dateTime.Minute;
            systemTime.wSecond = (ushort) dateTime.Second;
            systemTime.wMilliseconds = (ushort) dateTime.Millisecond;
            return systemTime;
        }

        private bool IsUpDownType ()
        {
            return (Misc.IsBitSet(WindowStyle, NativeMethods.DTS_UPDOWN));
        }

        private bool IsDropDownType ()
        {
            return (!IsUpDownType ());
        }

        unsafe private bool SetValue (DateTime DateTimeObject)
        {
            NativeMethods.SYSTEMTIME NewDateTime = ConvertDateTimeToSystemTime (DateTimeObject);

            return (XSendMessage.XSend(_hwnd, NativeMethods.DTM_SETSYSTEMTIME, IntPtr.Zero, new IntPtr(&NewDateTime), Marshal.SizeOf(NewDateTime.GetType())));
        }

        internal static bool IsCalendarPopupVisible (IntPtr hwnd, out IntPtr hwndCalendar)
        {
            hwndCalendar = Misc.ProxySendMessage(hwnd, NativeMethods.DTM_GETMONTHCAL, IntPtr.Zero, IntPtr.Zero);
            return hwndCalendar != IntPtr.Zero;
        }

        // generic way to retrieve item's state
        private static ExpandCollapseState GetItemState (IntPtr hwnd)
        {
            IntPtr hwndCalendar;
            return IsCalendarPopupVisible (hwnd, out hwndCalendar) ? ExpandCollapseState.Expanded : ExpandCollapseState.Collapsed;
        }

        #endregion

        // ------------------------------------------------------
        //
        // Private Fields
        //
        // ------------------------------------------------------

        #region Private Fields

        private bool _IsDropDownType;

        private const int MaxDateTimeLength = 0x100;

        private enum DateTimeItem: int
        {
            Button = -1,
            Calendar = 0
        }

        #endregion

        //------------------------------------------------------
        //
        //  DateTimeButton Private Class
        //
        //------------------------------------------------------

        #region DateTimeButton

        // Class implementation for the WindowsDateTimeButtonProxy.
        class DateTimeButton: ProxySimple, IInvokeProvider
        {

            // ------------------------------------------------------
            //
            //  Constructors
            //
            //------------------------------------------------------

            #region Constructors

            internal DateTimeButton (IntPtr hwnd, ProxyFragment parent, int item)
            : base( hwnd, parent, item)
            {
                _cControlType = ControlType.Button;
                _sAutomationId = "DropDown"; // This string is a non-localizable string
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
                    return BoundingRect(_hwnd).ToRect(Misc.IsControlRTL(_hwnd));
                }
            }

            //Gets the localized name
            internal override string LocalizedName
            {
                get
                {
                    return SR.Get(SRID.LocalizedNameWindowsDateTimeButton);
                }
            }

            #endregion

            #region Invoke Pattern

            // Same a a click on the Date Time drop down button
            void IInvokeProvider.Invoke ()
            {
                // Make sure that the control is enabled
                if (!SafeNativeMethods.IsWindowEnabled(_hwnd))
                {
                    throw new ElementNotEnabledException();
                }

                IntPtr hwndCalendar;

                if (!WindowsDateTimePicker.IsCalendarPopupVisible (_hwnd, out hwndCalendar))
                {
                    Misc.PostMessage(_hwnd, NativeMethods.WM_KEYDOWN, (IntPtr)NativeMethods.VK_F4, IntPtr.Zero);
                    Misc.PostMessage(_hwnd, NativeMethods.WM_KEYUP, (IntPtr)NativeMethods.VK_F4, IntPtr.Zero);

                    // Wait for the window to come up; 10 tries
                    for (int i = 0; i < 10 && !WindowsDateTimePicker.IsCalendarPopupVisible (_hwnd, out hwndCalendar); i++)
                    {
                        System.Threading.Thread.Sleep (0);
                    }
                }
                else
                {
                    // A post message must be used at the processing of the WM_KEYDOWN is achieve in a PeekMessageLoop
                    Misc.PostMessage(hwndCalendar, NativeMethods.WM_KEYDOWN, (IntPtr)NativeMethods.VK_ESCAPE, new IntPtr(0x10001));

                    // Wait for the window to go away; 10 tries
                    for (int i = 0; i < 10 && UnsafeNativeMethods.IsWindow (hwndCalendar); i++)
                    {
                        System.Threading.Thread.Sleep (0);
                    }
                }
            }

            #endregion

            //------------------------------------------------------
            //
            //  Internal Methods
            //
            //------------------------------------------------------

            #region Internal Methods

            // Returns the bounding rectangle.
            internal static NativeMethods.Win32Rect BoundingRect (IntPtr hwnd)
            {
                NativeMethods.Win32Rect rcDateTime = new NativeMethods.Win32Rect ();

                if (!Misc.GetClientRectInScreenCoordinates(hwnd, ref rcDateTime))
                {
                    return NativeMethods.Win32Rect.Empty;
                }

                if (Misc.IsLayoutRTL(hwnd))
                {
                    rcDateTime.right = rcDateTime.left + UnsafeNativeMethods.GetSystemMetrics(NativeMethods.SM_CXVSCROLL);
                }
                else
                {
                    rcDateTime.left = rcDateTime.right - UnsafeNativeMethods.GetSystemMetrics(NativeMethods.SM_CXVSCROLL);
                }
                return rcDateTime;
            }

            #endregion
        }

        #endregion
    }
}
