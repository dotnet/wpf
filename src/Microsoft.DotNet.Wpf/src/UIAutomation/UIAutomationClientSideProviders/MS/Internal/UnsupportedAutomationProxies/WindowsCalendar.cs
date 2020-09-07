// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// Description: Windows Calendar Proxy

// PRESHARP: In order to avoid generating warnings about unkown message numbers and unknown pragmas.
#pragma warning disable 1634, 1691

using System;
using System.Text;
using System.Collections;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.ComponentModel;
using System.Globalization;
using System.Windows.Automation;
using System.Windows.Automation.Provider;
using System.Windows;
using System.Windows.Input;
using MS.Internal.AutomationProxies;
using MS.Win32;

namespace MS.Internal.UnsupportedAutomationProxies
{
    // Windows Calendar Proxy Class
    class WindowsCalendar: ProxyHwnd, ISelectionProvider, IScrollProvider
    {
        // ------------------------------------------------------
        //
        // Constructors
        //
        // ------------------------------------------------------

        #region Constructors

        internal WindowsCalendar (IntPtr hwnd, ProxyFragment parent, int item)
            : base(hwnd, parent, item)
        {
            _cControlType = ControlType.Calendar;

            // Poor UI design for this control. The focus should be sent the focus to 
            // the days or the next or previous month!
            _fIsKeyboardFocusable = true;

            _isVersion6 = IsVersion6();
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
            // ComCtl V6 on Vista has IAccessible impls that send winevents with childIDs
            // of:
            // CalWindow = 0
            // NextButton = 1
            // PrevButton = 2
            // TodayButton = 3
            // FirstCalendar = 4
            // ...followed by 5 for a second calendar, 6 for the 3rd, etc.
            // Their IAccessible works better with narrator than this calendar impl,
            // so return null to fall through to the MSAA proxy, and use it instead.
            // (eg. their IAccessible has good accNames - "Monday, Augut 28 2006",
            // whereas Narrator's read template is set up to only read Value, which
            // this calendar class doesn't implement. It does support SelectionItem,
            // but that would just have text of "28".)
            if (idChild != 0)
            {
                return null;
            }

            return new WindowsCalendar(hwnd, null, 0);
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
            if (SelectionPattern.Pattern == iid || ScrollPattern.Pattern == iid)
            {
                return this;
            }

            return null;
        }

        #endregion

        #region ProxyFragment Interface
        
        // Returns the next sibling element in the raw hierarchy.
        // Peripheral controls have always negative values.
        // Returns null if no next child.
        internal override ProxySimple GetNextSibling (ProxySimple child)
        {
            CalendarBits calBits = (CalendarBits)child;
            int iCalendar = calBits._iCalendar;

            switch (calBits._type)
            {
                case CalendarItem.ButtonPrev :
                    return CreateCalendarBits (CalendarItem.ButtonNext, (int) CalendarButton.ButtonItem.Next);

                case CalendarItem.ButtonNext :
                    return CreateCalendarBits (CalendarItem.Month, 0);

                case CalendarItem.Month :
                    return CreateCalendarBits (CalendarItem.Year, iCalendar);

                case CalendarItem.Year :
                    return CreateCalendarBits (CalendarItem.Dates, iCalendar);

                case CalendarItem.Dates :
                    int cCalendar = GetMonthCount();

                    return (iCalendar >= cCalendar - 1)
                        ? (HasStyle(_hwnd, NativeMethods.MCS_NOTODAY)
                            ? null
                            : CreateCalendarBits(CalendarItem.TodayLink, 0)) 
                        : CreateCalendarBits(CalendarItem.Month, iCalendar + 1);
            }
            return null;
        }

        // Returns the previous sibling element in the raw hierarchy.
        // Peripheral controls have always negative values.
        // Returns null is no previous.
        internal override ProxySimple GetPreviousSibling (ProxySimple child)
        {
            CalendarBits calBits = (CalendarBits)child;
            int iCalendar = calBits._iCalendar;

            switch (calBits._type)
            {
                case CalendarItem.ButtonPrev :
                    return null;

                case CalendarItem.ButtonNext :
                    return CreateCalendarBits (CalendarItem.ButtonPrev, (int) CalendarButton.ButtonItem.Prev);

                case CalendarItem.Month :
                    return iCalendar == 0
                               ? CreateCalendarBits (CalendarItem.ButtonNext, (int) CalendarButton.ButtonItem.Next)
                               : CreateCalendarBits (CalendarItem.Dates, iCalendar - 1);

                case CalendarItem.Year :
                    return CreateCalendarBits (CalendarItem.Month, iCalendar);

                case CalendarItem.Dates :
                    return CreateCalendarBits (CalendarItem.Year, iCalendar);

                case CalendarItem.TodayLink :
                    int cCalendar = GetMonthCount();

                    return CreateCalendarBits (CalendarItem.Dates, cCalendar - 1);
            }
            return null;
        }

        // Returns the first child element in the raw hierarchy.
        internal override ProxySimple GetFirstChild ()
        {
            return CreateCalendarBits (CalendarItem.ButtonPrev, (int) CalendarButton.ButtonItem.Prev);
        }

        // Returns the last child element in the raw hierarchy.
        internal override ProxySimple GetLastChild ()
        {
            if (!HasStyle(_hwnd, NativeMethods.MCS_NOTODAY))
            {
                return CreateCalendarBits (CalendarItem.TodayLink, 0);
            }

            int cCalendar = GetMonthCount();

            return CreateCalendarBits (CalendarItem.Dates, cCalendar - 1);
        }

        private bool GetHitTestInfo_V6(
            int xScreen, int yScreen, ref NativeMethods.MCHITTESTINFO_V6 hitTestInfo)
        {
            bool success = false;
            hitTestInfo.cbSize = (uint)Marshal.SizeOf(hitTestInfo);
            hitTestInfo.pt = new NativeMethods.Win32Point(xScreen, yScreen);

            // Convert the coordinates for the point of interest from
            // screen coordinates to window-relative coordinates.
            if (Misc.MapWindowPoints(IntPtr.Zero, _hwnd, ref hitTestInfo.pt, 1))
            {
                unsafe
                {
                    fixed (NativeMethods.MCHITTESTINFO_V6* pHitTestInfo = &hitTestInfo)
                    {
                        success =
                            XSendMessage.XSend(_hwnd, NativeMethods.MCM_HITTEST,
                                               IntPtr.Zero, new IntPtr(pHitTestInfo),
                                               (int) hitTestInfo.cbSize);
                    }
                }
            }

            return success;
        }

        private bool GetHitTestInfo(
            int xScreen, int yScreen, ref NativeMethods.MCHITTESTINFO hitTestInfo)
        {
            bool success = false;
            hitTestInfo.cbSize = (uint)Marshal.SizeOf(hitTestInfo);
            hitTestInfo.pt = new NativeMethods.Win32Point(xScreen, yScreen);

            // Convert the coordinates for the point of interest from
            // screen coordinates to window-relative coordinates.
            if (Misc.MapWindowPoints(IntPtr.Zero, _hwnd, ref hitTestInfo.pt, 1))
            {
                unsafe
                {
                    fixed (NativeMethods.MCHITTESTINFO* pHitTestInfo = &hitTestInfo)
                    {
                        success =
                            XSendMessage.XSend(_hwnd, NativeMethods.MCM_HITTEST,
                                               IntPtr.Zero, new IntPtr(pHitTestInfo),
                                               (int) hitTestInfo.cbSize);
                    }
                }
            }

            return success;
        }

        // Returns a Proxy element corresponding to the specified screen coordinates.
        internal override ProxySimple ElementProviderFromPoint (int x, int y)
        {
            // worst case scenario we return the parent.
            bool fUnknown = false;
            CalendarItem type = CalendarItem.ButtonPrev;
            int index = CalendarIndexFromPoint (x, y);

            // Allocate a local LVHITTESTINFO struct.
            NativeMethods.MCHITTESTINFO hitTestInfo = new NativeMethods.MCHITTESTINFO();
            GetHitTestInfo(x, y, ref hitTestInfo);
            switch (hitTestInfo.uHit)
            {
                case NativeMethods.MCHT_TITLEBTNPREV:
                    {
                        type = CalendarItem.ButtonPrev;
                        index = (int) CalendarButton.ButtonItem.Prev;
                        break;
                    }

                case NativeMethods.MCHT_TITLEBTNNEXT:
                    {
                        type = CalendarItem.ButtonNext;
                        index = (int) CalendarButton.ButtonItem.Next;
                        break;
                    }

                case NativeMethods.MCHT_TITLEMONTH:
                    {
                        type = CalendarItem.Month;
                        break;
                    }

                case NativeMethods.MCHT_TITLEYEAR:
                    {
                        type = CalendarItem.Year;
                        break;
                    }

                case NativeMethods.MCHT_CALENDARDAY:
                case NativeMethods.MCHT_CALENDARWEEKNUM:
                case NativeMethods.MCHT_CALENDARDATE:
                    {
                        type = CalendarItem.Dates;
                        break;
                    }

                case NativeMethods.MCHT_TODAYLINK:
                    {
                        type = CalendarItem.TodayLink;
                        break;
                    }

                default :
                    {
                        fUnknown = true;
                        break;
                    }
            }

            // If unknown lets have a try in one of the Dates element
            // Hittest fails on everything that is not one of the available month
            if (fUnknown)
            {
                for (int iMonth = 0, cMonths = GetMonthCount(); iMonth < cMonths; iMonth++)
                {
                    CalendarDates elDates = (CalendarDates) CreateCalendarBits (CalendarItem.Dates, iMonth);
                    Rect rc = elDates.BoundingRectangle;

                    if (x >= rc.Left && x <= rc.Right && y >= rc.Top && y <= rc.Bottom)
                    {
                        CalendarBits el = elDates.FromPoint (x, y, hitTestInfo.uHit);

                        if (el != null)
                            return el;
                    }
                }
            }

            // If a date, try to look into each Day
            if (type == CalendarItem.Dates)
            {
                CalendarDates elDates = (CalendarDates) CreateCalendarBits (CalendarItem.Dates, index);

                return elDates.FromPoint (x, y, hitTestInfo.uHit);
            }

            if (!fUnknown)
            {
                return CreateCalendarBits (type, index);
            }

            return base.ElementProviderFromPoint (x, y);
        }

        #endregion

        #region Scroll Pattern

        // Request to scroll Horizontally and vertically by the specified amount
        void IScrollProvider.SetScrollPercent (double horizontalPercent, double verticalPercent)
        {
            // Make sure that the control is enabled
            if (!SafeNativeMethods.IsWindowEnabled(_hwnd))
            {
                throw new ElementNotEnabledException();
            }

            if ((int)verticalPercent != (int)ScrollPattern.NoScroll)
            {
                throw new InvalidOperationException(SR.Get(SRID.OperationCannotBePerformed));
            }
            else if ((int)horizontalPercent == (int)ScrollPattern.NoScroll)
            {
                return;
            }
            else if (horizontalPercent < 0 || horizontalPercent > 100)
            {
                throw new ArgumentOutOfRangeException("horizontalPercent", SR.Get(SRID.ScrollBarOutOfRange));
            }

            int cDisplayedMonths = GetMonthCount();
            DateTime [] dateRange =
                CreateDateTimeFromSystemTime (GetRawValue (_hwnd, CalendarData.Range));
            int cMonths = (dateRange [1].Year - dateRange [0].Year) * 12
                            + (dateRange [1].Month - dateRange [0].Month) + 1;

            if (cMonths <= cDisplayedMonths)
            {
                return;
            }

            DateTime dtCur = CreateDateTimeFromSystemTime1 (GetRawValue (_hwnd, CalendarData.Title));
            int iPosCur = (dtCur.Year - dateRange [0].Year) * 12 + (dtCur.Month - dateRange [0].Month);
            int iPosNew = (int) Math.Round ((horizontalPercent / 100.0) * (cMonths - cDisplayedMonths));
            int cDeltaMonths = iPosNew > iPosCur ? iPosNew - iPosCur : iPosCur - iPosNew;

            if (cDeltaMonths == 0)
            {
                return;
            }

            int oldDeltaMonth =
                Misc.ProxySendMessageInt(
                    _hwnd, NativeMethods.MCM_SETMONTHDELTA, (IntPtr)cDeltaMonths, IntPtr.Zero);

            bool fForward = iPosNew > iPosCur;
            CalendarButton calButton =
                new CalendarButton (_hwnd, this,
                                    fForward ? CalendarButton.ButtonItem.Next : CalendarButton.ButtonItem.Prev,
                                    fForward ? CalendarItem.ButtonNext : CalendarItem.ButtonPrev);

            ((IInvokeProvider)calButton).Invoke ();
            Misc.ProxySendMessage(_hwnd, NativeMethods.MCM_SETMONTHDELTA, (IntPtr)oldDeltaMonth, IntPtr.Zero);

            return;
        }

        // Request to scroll horizontally and vertically by the specified scrolling amount
        void IScrollProvider.Scroll (ScrollAmount horizontalAmount, ScrollAmount verticalAmount)
        {
            // Make sure that the control is enabled
            if (!SafeNativeMethods.IsWindowEnabled(_hwnd))
            {
                throw new ElementNotEnabledException();
            }

            if (verticalAmount != ScrollAmount.NoAmount)
            {
                throw new InvalidOperationException(SR.Get(SRID.OperationCannotBePerformed));
            }

            if (!Scroll(horizontalAmount))
            {
                throw new InvalidOperationException(SR.Get(SRID.OperationCannotBePerformed));
            }
        }

        // Calc the position of the horizontal scroll bar thumb in the 0..100 % range
        double IScrollProvider.HorizontalScrollPercent
        {
            get
            {
                int cDisplayedMonths = GetMonthCount();
                DateTime [] dt = CreateDateTimeFromSystemTime (GetRawValue (_hwnd, CalendarData.Range));
                int cMonths = (dt [1].Year - dt [0].Year) * 12 + (dt [1].Month - dt [0].Month) + 1;
                DateTime [] dateTimeCur = CreateDateTimeFromSystemTime (GetRawValue (_hwnd, CalendarData.Title));
                int iPos = (dateTimeCur [0].Year - dt [0].Year) * 12 + (dateTimeCur [0].Month - dt [0].Month);

                return cMonths <= cDisplayedMonths
                        ? (double)ScrollPattern.NoScroll
                        : 100.0 * iPos / (cMonths - cDisplayedMonths);
            }
        }

        // Calc the position of the Vertical scroll bar thumb in the 0..100 % range
        double IScrollProvider.VerticalScrollPercent
        {
            get
            {
                return (double)ScrollPattern.NoScroll;
            }
        }

        // Percentage of the window that is visible along the horizontal axis. 
        double IScrollProvider.HorizontalViewSize
        {
            get
            {
                int cDisplayedMonths = GetMonthCount();
                DateTime [] dt = CreateDateTimeFromSystemTime (GetRawValue (_hwnd, CalendarData.Range));
                int cMonths = (dt [1].Year - dt [0].Year) * 12 + (dt [1].Month - dt [0].Month) + 1;

                return cMonths <= cDisplayedMonths ? 100.0 : 100.0 * cDisplayedMonths / cMonths;
            }
        }

        // Percentage of the window that is visible along the vertical axis. 
        double IScrollProvider.VerticalViewSize
        {
            get
            {
                return 100.0;
            }
        }

        // Can the element be horizontaly scrolled
        bool IScrollProvider.HorizontallyScrollable
        {
            get
            {
                int cDisplayedMonths = GetMonthCount();
                DateTime [] dt =
                    CreateDateTimeFromSystemTime(GetRawValue (_hwnd, CalendarData.Range));
                int cMonths = (dt [1].Year - dt [0].Year) * 12 + (dt [1].Month - dt [0].Month) + 1;
                return cMonths > cDisplayedMonths;
            }
        }

        // Can the element be verticaly scrolled
        bool IScrollProvider.VerticallyScrollable
        {
            get
            {
                return false;
            }
        }

        #endregion Scroll Pattern

        #region Selection Pattern

        // ------------------------------------------------------
        //
        // ISelectionProvider interface implementation
        //
        // ------------------------------------------------------

        // Returns an enumerator over the current selection.
        IRawElementProviderSimple [] ISelectionProvider.GetSelection()
        {
                // Get the visible selection items
            DateTime [] dateVisible =
                CreateDateTimeFromSystemTime (GetRawValue (_hwnd, CalendarData.Title));
            DateTime [] dateSelection =
                CreateDateTimeFromSystemTime (GetRawValue (_hwnd, CalendarData.Selection));
            int count = dateSelection.Length;

            // should never happen
            if (count <= 0)
            {
                System.Diagnostics.Debug.Assert (
                    false, "No selection, the code assumes that there is always one");
                return Array.Empty<IRawElementProviderSimple >();
            }

            // Single selection
            if (count == 1)
            {
                if (dateSelection [0] >= dateVisible [0] && dateSelection [0] <= dateVisible [1])
                {
                    return BuildSelectionArray (dateSelection, 1, dateVisible [0]);
                }

                return Array.Empty<IRawElementProviderSimple >();
            }

            // Range selection, expand the dates
            TimeSpan ts = dateSelection [1] - dateSelection [0];
            int cDays = (int) ts.TotalDays + 1;
            int cItems = 0;
            DateTime [] list = new DateTime [cDays];
            DateTime dtStart = dateSelection [0];

            for (int iDay = 0; iDay < cDays; iDay++)
            {
                list [iDay] = dtStart.AddDays ((double) iDay);
                if (list [cItems] >= dateVisible [0] && list [cItems] <= dateVisible [1])
                {
                    cItems++;
                }
            }

            return BuildSelectionArray (list, cItems, dateVisible [0]);
        }

        // Returns whether the control supports multiple selection.
        bool ISelectionProvider.CanSelectMultiple
        {
            get
            {
                return IsMultiSelect (_hwnd);
            }
        }

        // Returns whether the control requires a minimum of one
        // selected element at all times.
        bool ISelectionProvider.IsSelectionRequired
        {
            get
            {
                return true;
            }
        }

        #endregion

        // ------------------------------------------------------
        //
        // Internal Types Declaration
        //
        // ------------------------------------------------------

        #region Internal Types

        // Unique identifiers for children elements
        internal enum CalendarItem
        {
            TodayLink = -3,
            ButtonPrev = -2,
            ButtonNext = -1,
            Month = 0,
            Year = 1,
            Dates = 2,
            GridItem = 3,
            Last = 3
        }

        // Type of information requested for the calendar
        internal enum CalendarData
        {
            Range,          // Min and max date
            Title,
            Selection       // Selection Range
        }

        #endregion

        // ------------------------------------------------------
        //
        // Internal Methods
        //
        // ------------------------------------------------------
        #region Internal Methods

        // Get the range values Min/Max days for this calendar instance.
        static internal unsafe NativeMethods.SYSTEMTIME[] GetRange(IntPtr hwnd)
        {
            NativeMethods.SYSTEMTIME[] systemTime = new NativeMethods.SYSTEMTIME[2];

            systemTime[0] = new NativeMethods.SYSTEMTIME();
            systemTime[1] = new NativeMethods.SYSTEMTIME();
            fixed (ushort* head = &(systemTime[0].wYear))
            {
                XSendMessage.XSend(hwnd, NativeMethods.MCM_GETRANGE,
                                   IntPtr.Zero, new IntPtr(head),
                                   2 * Marshal.SizeOf(systemTime[0]));
            }

            return systemTime;
        }

        // Sends the MCM_GETCURSEL message to to the controls hwnd
        // Returns a win32 SYSTEMTIME object
        static unsafe internal NativeMethods.SYSTEMTIME[] GetRawValue(
            IntPtr hwnd, CalendarData calendarData)
        {
            NativeMethods.SYSTEMTIME[] CurrentDateTime = new NativeMethods.SYSTEMTIME[2]
            {
                new NativeMethods.SYSTEMTIME (), new NativeMethods.SYSTEMTIME ()
            };

            fixed (ushort* lParam = &CurrentDateTime[0].wYear)
            {
                if (calendarData == CalendarData.Range)
                {
                    XSendMessage.XSend(hwnd, NativeMethods.MCM_GETRANGE,
                                       IntPtr.Zero, new IntPtr(lParam),
                                       2 * (int)Marshal.SizeOf(CurrentDateTime[0].GetType()));

                    // If max range return 2 SystemTime with all zeros
                    if (CurrentDateTime[0].wYear == 0)
                    {
                        CurrentDateTime[0].wYear = 1752;
                        CurrentDateTime[0].wMonth = 10;
                        CurrentDateTime[0].wDay = 1;
                    }

                    if (CurrentDateTime[1].wYear == 0)
                    {
                        CurrentDateTime[1].wYear = 9999;
                        CurrentDateTime[1].wMonth = 12;
                        CurrentDateTime[1].wDay = 31;
                    }
                }
                else if (calendarData == CalendarData.Title)
                {
                    XSendMessage.XSend(hwnd, NativeMethods.MCM_GETMONTHRANGE,
                                       IntPtr.Zero, new IntPtr(lParam),
                                       2 * (int)Marshal.SizeOf(CurrentDateTime[0].GetType()));
                }
                else if (calendarData == CalendarData.Selection)
                {
                    // A multiselect calendar
                    if (HasStyle(hwnd, NativeMethods.MCS_MULTISELECT))
                    {
                        XSendMessage.XSend(hwnd, NativeMethods.MCM_GETSELRANGE,
                                           IntPtr.Zero, new IntPtr(lParam),
                                           2 * (int)Marshal.SizeOf(CurrentDateTime[0].GetType()));
                    }
                    else
                    {
                        XSendMessage.XSend(hwnd, NativeMethods.MCM_GETCURSEL,
                                           IntPtr.Zero, new IntPtr(lParam),
                                           (int)Marshal.SizeOf(CurrentDateTime[0].GetType()));
                        CurrentDateTime[1] = CurrentDateTime[0];
                    }
                }
            }

            return CurrentDateTime;
        }

        static internal bool SetRawValue(IntPtr hwnd, DateTime[] aDateTime)
        {
            NativeMethods.SYSTEMTIME[] aSysTime = new NativeMethods.SYSTEMTIME[2];

            aSysTime[0] = ParseDate(aDateTime[0]);
            aSysTime[1] = ParseDate(aDateTime[1]);

            bool fMultiSelect = HasStyle(hwnd, NativeMethods.MCS_MULTISELECT);
            int iMessage = fMultiSelect ? NativeMethods.MCM_SETSELRANGE : NativeMethods.MCM_SETCURSEL;
            int cLenStruct = (fMultiSelect ? 2 : 1) * Marshal.SizeOf(aSysTime[0].GetType());

            unsafe
            {
                fixed (NativeMethods.SYSTEMTIME* lParam = &aSysTime[0])
                {
                    return XSendMessage.XSend(hwnd, iMessage, IntPtr.Zero, new IntPtr(lParam), cLenStruct);
                }
            }
        }

        // internal member method for converting SYSTEMTIME to .net frameworks DateTime object
        static internal DateTime[] CreateDateTimeFromSystemTime(NativeMethods.SYSTEMTIME[] systemTime)
        {
            DateTime[] dt = new DateTime[systemTime.Length];

            for (int i = 0; i < systemTime.Length; ++i)
            {
                dt[i] = systemTime[i].wYear == 0
                        ? DateTime.Today
                        : new DateTime(systemTime[i].wYear, systemTime[i].wMonth, systemTime[i].wDay);
            }

            return dt;
        }

        // internal member method for converting SYSTEMTIME to .net frameworks DateTime object
        static internal DateTime CreateDateTimeFromSystemTime1(NativeMethods.SYSTEMTIME[] systemTime)
        {
            return systemTime[0].wYear == 0
                    ? DateTime.Today
                    : new DateTime(systemTime[0].wYear, systemTime[0].wMonth, systemTime[0].wDay);
        }

        // internal member method for converting SYSTEMTIME to .net frameworks DateTime object
        static internal DateTime CreateDateTimeFromSystemTime1(NativeMethods.SYSTEMTIME systemTime)
        {
            return systemTime.wYear == 0
                    ? DateTime.Today
                    : new DateTime(systemTime.wYear, systemTime.wMonth, systemTime.wDay);
        }

        // internal member function for creating a new system time object from a .net datetime object
        static internal NativeMethods.SYSTEMTIME CreateSystemTimeFromDateTime(DateTime dt)
        {
            NativeMethods.SYSTEMTIME systemTime = new NativeMethods.SYSTEMTIME();

            systemTime.wYear = (ushort)dt.Year;
            systemTime.wMonth = (ushort)dt.Month;
            systemTime.wDay = (ushort)dt.Day;
            systemTime.wHour = (ushort)dt.Hour;
            systemTime.wMinute = (ushort)dt.Minute;
            systemTime.wSecond = (ushort)dt.Second;
            systemTime.wMilliseconds = (ushort)dt.Millisecond;
            return systemTime;
        }

        // determines if a window has a certain style property
        static internal bool HasStyle(IntPtr hwnd, int calStyle)
        {
            return (Misc.IsBitSet(Misc.GetWindowStyle(hwnd), calStyle));
        }

        // determines if a window has the multi-style property.
        static internal bool IsMultiSelect(IntPtr hwnd)
        {
            int cMaxSelection =
                Misc.ProxySendMessageInt(hwnd, NativeMethods.MCM_GETMAXSELCOUNT,
                                         IntPtr.Zero, IntPtr.Zero);
            return cMaxSelection > 1 && HasStyle(hwnd, NativeMethods.MCS_MULTISELECT);
        }

        // Returns the month count, or -1 if the month count is unobtainable.
        internal int GetMonthCount()
        {
            if (_isVersion6)
            {
                int monthCount =
                    Misc.ProxySendMessageInt(_hwnd, NativeMethods.MCM_GETCALENDARCOUNT,
                                             IntPtr.Zero, IntPtr.Zero);
                return monthCount;
            }
            else
            {
                // Since MCM_GETMONTHCOUNT is not available, use the older pixel-based
                // calculation.  This fails under some longhorn themes, e.g. Aero.
                NativeMethods.Win32Rect rc = new NativeMethods.Win32Rect();
                NativeMethods.Win32Rect rcMonth;
                CalcPositions(0, out rcMonth);
                if (!Misc.GetClientRectInScreenCoordinates(_hwnd, ref rc))
                {
                    return -1;
                }

                int dxMonth = rcMonth.right - rcMonth.left;
                int dyMonth = rcMonth.bottom - rcMonth.top;
                int dx = rc.right - rc.left;
                int dy = rc.bottom - rc.top;
                int cCols = 1 + (dx - dxMonth) / (dxMonth + CALBORDER);
                int cRows = 1 + (dy - dyMonth - _dyTodayLink) / (dyMonth + CALBORDER);

                if (cCols < 1)
                {
                    cCols = 1;
                }

                if (cRows < 1)
                {
                    cRows = 1;
                }

                return cCols * cRows;
            }
        }

        internal void CalcPositions(int iCalendar, out NativeMethods.Win32Rect rcMonth)
        {
            rcMonth = NativeMethods.Win32Rect.Empty;

            _fHasWeekNum = WindowsCalendar.HasStyle(_hwnd, NativeMethods.MCS_WEEKNUMBERS);
            _fHasToday = !WindowsCalendar.HasStyle(_hwnd, NativeMethods.MCS_NOTODAY);

            if (Misc.GetClientRectInScreenCoordinates(_hwnd, ref _rcCalendar))
            {
                NativeMethods.Win32Rect rcSingleMonth = new NativeMethods.Win32Rect();
                unsafe
                {
                    // Get range of position values
                    XSendMessage.XSend(_hwnd, NativeMethods.MCM_GETMINREQRECT,
                                       IntPtr.Zero, new IntPtr(&rcSingleMonth),
                                       Marshal.SizeOf(rcSingleMonth.GetType()));
                }

                // height and width of this part of the month
                int cxMonth = rcSingleMonth.right - rcSingleMonth.left;
                int cyMonth = rcSingleMonth.bottom - rcSingleMonth.top;

                // calc heights based on control number of lines
                _dyRow = _fHasToday ? (cyMonth - 3) / 10 : (cyMonth - 1) / 9;
                _dyHeader = _dyRow * 2;
                _dyWeekDay = _dyRow;
                _dyTodayLink = _dyRow + 2;

                int dx = _rcCalendar.right - _rcCalendar.left;
                int cCols = 1 + (dx - cxMonth) / (cxMonth + CALBORDER);

                if (cCols < 1)
                {
                    cCols = 1;
                }

                if (cCols > 1 && Misc.IsLayoutRTL(_hwnd))
                {
                    iCalendar = (cCols - 1) - iCalendar;
                }

                // x margin = control size - ( # of months * month size + # of month separators * separator size) / 2
                sizeMargin.cx = (dx - (cCols * cxMonth + (cCols - 1) * CALBORDER)) / 2;
                sizeMargin.cy = 0;

                int iLine = iCalendar / cCols;
                int iCol = iCalendar % cCols;

                // Set the start position for this month 
                cyMonth -= 1 + (_fHasToday ? _dyTodayLink : 0);
                rcMonth.left = _rcCalendar.left + iCol * (cxMonth + CALBORDER) + sizeMargin.cx;
                rcMonth.top = _rcCalendar.top + iLine * (cyMonth + CALBORDER + 1) + sizeMargin.cy;

                // And the widths
                rcMonth.right = rcMonth.left + cxMonth;
                rcMonth.bottom = rcMonth.top + cyMonth;
            }
        }

        internal void CalcTitleTextExtents(
            int iCalendar, NativeMethods.Win32Rect rcMonth,
            out int dxMonth, out int dxYear, out int dxSeparator)
        {
            // Selection that span multiple years only show beginning year
            DateTime[] dtTitle =
                WindowsCalendar.CreateDateTimeFromSystemTime(
                    WindowsCalendar.GetRawValue(_hwnd, WindowsCalendar.CalendarData.Title));
            DateTime dt = dtTitle[0].AddMonths(iCalendar);
            string sMonth = dt.ToString("MMMM", CultureInfo.InvariantCulture);
            string sYear = dt.ToString("yyyy", CultureInfo.InvariantCulture);
            string sSeparator = ",";

            int width = rcMonth.right - rcMonth.left;

            dxMonth = (width * 4 / 100) * sMonth.Length;
            dxYear = (width * 4 / 100) * sYear.Length;
            dxSeparator = (width * 4 / 100) * sSeparator.Length;

            _dyTitleHeaderPad = _dyRow / 2;
        }

        #endregion Internal Methods

        // ------------------------------------------------------
        //
        // Private Methods
        //
        // ------------------------------------------------------        

        #region Private Methods

        #region Scroll Interface helpers

        private bool Scroll(ScrollAmount Amount)
        {
            // Done
            if (Amount == ScrollAmount.NoAmount)
            {
                return true;
            }

            // Set position
            DateTime dtStart = CreateDateTimeFromSystemTime1 (GetRawValue (_hwnd, CalendarData.Title));
            DateTime [] dateRange =
                CreateDateTimeFromSystemTime (
                    GetRawValue (_hwnd, CalendarData.Range));
            int iPosCur = (dtStart.Year - dateRange [0].Year) * 12
                                + (dtStart.Month - dateRange [0].Month);

            switch (Amount)
            {
                // substract one month
                case ScrollAmount.LargeDecrement:
                case ScrollAmount.SmallDecrement:
                    {
                        // boundaryCheck
                        if (iPosCur > 0)
                        {
                            CalendarButton calButton =
                                new CalendarButton (
                                    _hwnd, this, CalendarButton.ButtonItem.Prev, CalendarItem.ButtonPrev);

                            ((IInvokeProvider) calButton).Invoke ();
                            return true;
                        }

                        break;
                    }

                // add one month
                case ScrollAmount.LargeIncrement :
                case ScrollAmount.SmallIncrement :
                    {
                        // boundaryCheck
                        int cMonths = (dateRange [1].Year - dateRange [0].Year) * 12
                                        + (dateRange [1].Month - dateRange [0].Month) + 1;

                        if (iPosCur < cMonths)
                        {
                            CalendarButton calButton =
                                new CalendarButton (
                                        _hwnd, this, CalendarButton.ButtonItem.Next, CalendarItem.ButtonNext);

                            ((IInvokeProvider) calButton).Invoke ();
                            return true;
                        }

                        break;
                    }
            }
            return true;
        }

        #endregion

        #region Selection Interface helpers

        private CalendarDates.CalendarDay[] BuildSelectionArray(
            DateTime[] adt, int cDates, DateTime dtMonth1)
        {
            if (cDates <= 0)
            {
                return null;
            }

            CalendarDates.CalendarDay [] aRaw = new CalendarDates.CalendarDay [cDates];
            for (int i = 0; i < cDates; i++)
            {
                int iCalendar = (adt [i].Year - dtMonth1.Year) * 12 + (adt [i].Month - dtMonth1.Month);
                DateTime itemDateTime =
                    CalendarDates.CalendarDay.GetDateTimeFromCalendarItem(
                        _hwnd, this, iCalendar, CalendarDates.MAX_DAYS);
                int iDeltaItem = adt[i].Day - itemDateTime.Day;
                CalendarDates calDates = new CalendarDates(_hwnd, this, iCalendar, CalendarItem.Dates);
                aRaw[i] = new CalendarDates.CalendarDay(
                                _hwnd, calDates, CalendarDates.MAX_DAYS + iDeltaItem, iCalendar);
            }

            return aRaw;
        }

        #endregion

        
        // Create Proxy objects for each piece of a calendar
        private ProxySimple CreateCalendarBits (CalendarItem type, int index)
        {
            switch (type)
            {
                case CalendarItem.ButtonPrev :
                    return new CalendarButton (_hwnd, this, CalendarButton.ButtonItem.Prev, type);

                case CalendarItem.ButtonNext :
                    return new CalendarButton (_hwnd, this, CalendarButton.ButtonItem.Next, type);

                case CalendarItem.Month :
                    return new CalendarMonth (_hwnd, this, index, type);

                case CalendarItem.Year :
                    return new CalendarYear (_hwnd, this, index, type);

                case CalendarItem.Dates :
                    return new CalendarDates (_hwnd, this, index, type);

                case CalendarItem.TodayLink :
                    return new CalendarTodayLink (_hwnd, this, (int) CalendarItem.TodayLink, type);
            }
            return null;
        }

        private bool IsVersion6()
        {
            NativeMethods.Win32Rect rect;
            NativeMethods.SYSTEMTIME stEnd;
            NativeMethods.SYSTEMTIME stStart;
            bool isVersion6 =
                GetCalendarGridInfo(NativeMethods.MCGIF_RECT,
                                    NativeMethods.MCGIP_CALENDARCONTROL,
                                    0, 0, 0, out rect, out stEnd, out stStart);
            return isVersion6;
        }

        // V6 wrapper method to get the rect and/or dates of a calendar part.
        // NOT for use with MCGIF_NAME, since this involves copying string
        // across memory boundaries, and is handled instead in
        // GetCalendarGridInfoText().
        private bool GetCalendarGridInfo(
            uint dwFlags,   // OR of NativeMethods.MCGIF_DATE, _RECT
            uint dwPart,    // one of NativeMethods.MCGIP_ values
            int iCalendar,
            int iRow,
            int iCol,
            out NativeMethods.Win32Rect rect,
            out NativeMethods.SYSTEMTIME stEnd,
            out NativeMethods.SYSTEMTIME stStart
            )
        {
            System.Diagnostics.Debug.Assert(
                (dwFlags & ~(NativeMethods.MCGIF_DATE | NativeMethods.MCGIF_RECT)) == 0,
                "GetCalendarGridInfo() should be used only to obtain Date and Rect,"
                + "dwFlags has flag bits other that MCGIF_DATE and MCGIF_RECT");

            NativeMethods.MCGRIDINFO gridInfo = new NativeMethods.MCGRIDINFO();
            gridInfo.dwFlags = dwFlags;
            gridInfo.cbSize = (uint)Marshal.SizeOf(gridInfo);
            gridInfo.dwPart = dwPart;
            gridInfo.iCalendar = iCalendar;
            gridInfo.iCol = iCol;
            gridInfo.iRow = iRow;
            bool success = GetCalendarGridInfo(_hwnd, ref gridInfo);
            rect = gridInfo.rc;
            stEnd = gridInfo.stEnd;
            stStart = gridInfo.stStart;
            return success;
        }

        // V6 wrapper method to get just the text of a calendar part.
        private string GetCalendarGridInfoText(
            uint dwPart,    // one of NativeMethods.MCGIP_ values
            int iCalendar, int iRow, int iCol)
        {
            NativeMethods.MCGRIDINFO gridInfo = new NativeMethods.MCGRIDINFO();
            gridInfo.cbSize = (uint)Marshal.SizeOf(gridInfo);
            gridInfo.dwPart = dwPart;
            gridInfo.iCalendar = iCalendar;
            gridInfo.iCol = iCol;
            gridInfo.iRow = iRow;
            gridInfo.cchName = 80;
            string cellText = GetCalendarGridInfoText(_hwnd, gridInfo);
            return cellText;
        }

        // V6 wrapper method to get just the rect of a calendar part,
        // in screen coordinates.
        private bool GetCalendarPartRect(
            int iCalendar, uint dwPart, int row, int column,
            out NativeMethods.Win32Rect rectScreen)
        {
            NativeMethods.SYSTEMTIME stEnd;
            NativeMethods.SYSTEMTIME stStart;
            bool success =
                GetCalendarGridInfo(
                        NativeMethods.MCGIF_RECT, dwPart, iCalendar, row, column,
                        out rectScreen, out stEnd, out stStart);
            // This returns client coordinates relative to the control,
            // so convert rectScreen to screen coords.
            if (success)
            {
                success = Misc.MapWindowPoints(_hwnd, IntPtr.Zero, ref rectScreen, 2);
            }
            if (!success)
            {
                rectScreen = new NativeMethods.Win32Rect();
            }
            return success;
        }

        // Try to transform an object into a SYSTEMTIME,
        // throws if unsucessful
        static private NativeMethods.SYSTEMTIME ParseDate (object val)
        {
            NativeMethods.SYSTEMTIME systemTime = new NativeMethods.SYSTEMTIME ();
            bool fValid = false;

            string valString = val as string;

            if (val is DateTime)
            {
                systemTime = CreateSystemTimeFromDateTime((DateTime)val);
                fValid = true;
            }
            // PerSharp/PreFast will flag this as warning 6507/56507:
            // Prefer 'string.IsNullOrEmpty(valString)' over checks for null and/or emptiness.
            // Null and Empty string mean different things here.
#pragma warning suppress 6507
            else if (valString != null)
            {
                systemTime = CreateSystemTimeFromDateTime(
                                System.DateTime.Parse(valString, CultureInfo.InvariantCulture));
                fValid = true;
            }

            if (!fValid)
            {
                throw new ArgumentException (
                    SR.Get(SRID.InvalidDataTypeOfParameter, " DateTime or string "), "val");
            }

            return systemTime;
        }

        private int CalendarIndexFromPoint (int x, int y)
        {
            NativeMethods.Win32Rect rcMonth;
            CalcPositions (0, out rcMonth);

            NativeMethods.Win32Rect rc = new NativeMethods.Win32Rect ();
            NativeMethods.Win32Rect rcSingleMonth = new NativeMethods.Win32Rect ();

            if (!Misc.GetClientRectInScreenCoordinates(_hwnd, ref rc))
            {
                return -1;
            }

            unsafe
            {
                // Get range of position values
                XSendMessage.XSend(
                    _hwnd, NativeMethods.MCM_GETMINREQRECT,
                    IntPtr.Zero, new IntPtr(&rcSingleMonth),
                    Marshal.SizeOf(rcSingleMonth.GetType()));
            }

            // height and width of this part of the month
            int cxMonth = rcSingleMonth.right - rcSingleMonth.left;
            int cyMonth = (rcSingleMonth.bottom - rcSingleMonth.top) - (_fHasToday ? _dyTodayLink : 0);

            int dx = rc.right - rc.left;
            int cCols = 1 + (dx - cxMonth) / (cxMonth + CALBORDER);

            if (cCols < 1)
            {
                cCols = 1;
            }

            // The calendar is horizontaly centered in its window. Figure out the offset
            // of the first calendar month. (>0 if the window is greater than the MinReqRect,
            // this is rare)
            int cxMargin = (dx - (cCols * cxMonth + (cCols - 1) * CALBORDER)) / 2;

            int iX = ((x - cxMargin) - rc.left) / (cxMonth + CALBORDER);
            int iY = (y - rc.top) / (cyMonth + CALBORDER + 1);

            int index = iY * cCols + iX;

            if (cCols > 1 && Misc.IsLayoutRTL(_hwnd))
            {
                index = (cCols - 1) - index;
            }

            return index;
        }

        // Use to retrieve MCGIF_NAME only.
        private static unsafe string GetCalendarGridInfoText(
            IntPtr hwnd, NativeMethods.MCGRIDINFO gridInfo)
        {
            Debug.Assert(
                gridInfo.dwFlags == 0,
                "gridInfo.dwFlags should be 0 when calling GetCalendarGridInfoText");
            gridInfo.dwFlags = NativeMethods.MCGIF_NAME;

            XSendMessage.ProcessorTypes localBitness;
            XSendMessage.ProcessorTypes remoteBitness;
            XSendMessage.GetProcessTypes(hwnd, out localBitness, out remoteBitness);

            if (localBitness == remoteBitness)
            {
                return XSendMessage.GetTextWithinStructure(
                            hwnd,
                            NativeMethods.MCM_GETCALENDARGRIDINFO,
                            IntPtr.Zero,
                            new IntPtr(&gridInfo),
                            Marshal.SizeOf(gridInfo.GetType()),
                            new IntPtr(&gridInfo.pszName),
                            (int)gridInfo.cchName);
            }
            else if (remoteBitness == XSendMessage.ProcessorTypes.Processor32Bit)
            {
                MCGRIDINFO_32 gridInfo32 = new MCGRIDINFO_32(gridInfo);

                return XSendMessage.GetTextWithinStructure(
                            hwnd,
                            NativeMethods.MCM_GETCALENDARGRIDINFO,
                            IntPtr.Zero,
                            new IntPtr(&gridInfo32),
                            Marshal.SizeOf(gridInfo32.GetType()),
                            new IntPtr(&gridInfo32.pszName),
                            (int)gridInfo32.cchName);
            }
            else if (remoteBitness == XSendMessage.ProcessorTypes.Processor64Bit)
            {
                MCGRIDINFO_64 gridInfo64 = new MCGRIDINFO_64(gridInfo);

                return XSendMessage.GetTextWithinStructure(
                            hwnd,
                            NativeMethods.MCM_GETCALENDARGRIDINFO,
                            IntPtr.Zero,
                            new IntPtr(&gridInfo64),
                            Marshal.SizeOf(gridInfo64.GetType()),
                            new IntPtr(&gridInfo64.pszName),
                            (int)gridInfo64.cchName);
            }

            return string.Empty;
        }

        private static unsafe bool GetCalendarGridInfo(
            IntPtr hwnd, ref NativeMethods.MCGRIDINFO gridInfo)
        {
            // Do not use this if gridInfo.dwFlags contains MCGIF_NAME;
            // use GetCalendarGridInfoText() instead.
            Debug.Assert(
                (gridInfo.dwFlags & NativeMethods.MCGIF_NAME) == 0,
                "dwFlags contains MCGIF_NAME, "
                    + "use GetCalendarGridInfoText() to retrieve the text "
                    + "of a calendar part.");

            gridInfo.dwFlags &= ~((uint)NativeMethods.MCGIF_NAME);

            XSendMessage.ProcessorTypes localBitness;
            XSendMessage.ProcessorTypes remoteBitness;
            XSendMessage.GetProcessTypes(hwnd, out localBitness, out remoteBitness);

            if (localBitness == remoteBitness)
            {
                fixed (NativeMethods.MCGRIDINFO* pGridInfo = &gridInfo)
                {
                    return XSendMessage.XSend(hwnd, NativeMethods.MCM_GETCALENDARGRIDINFO,
                                 IntPtr.Zero, new IntPtr(pGridInfo),
                                 Marshal.SizeOf(gridInfo.GetType()));
                }
            }
            else if (remoteBitness == XSendMessage.ProcessorTypes.Processor32Bit)
            {
                MCGRIDINFO_32 gridInfo32 = new MCGRIDINFO_32(gridInfo);

                bool result = XSendMessage.XSend(hwnd, NativeMethods.MCM_GETCALENDARGRIDINFO,
                                    IntPtr.Zero, new IntPtr(&gridInfo32),
                                    Marshal.SizeOf(gridInfo32.GetType()));

                if (result)
                {
                    gridInfo = (NativeMethods.MCGRIDINFO)gridInfo32;
                }

                return result;
            }
            else if (remoteBitness == XSendMessage.ProcessorTypes.Processor64Bit)
            {
                MCGRIDINFO_64 gridInfo64 = new MCGRIDINFO_64(gridInfo);

                bool result = XSendMessage.XSend(hwnd, NativeMethods.MCM_GETCALENDARGRIDINFO,
                                    IntPtr.Zero, new IntPtr(&gridInfo64),
                                    Marshal.SizeOf(gridInfo64.GetType()));

                if (result)
                {
                    gridInfo = (NativeMethods.MCGRIDINFO)gridInfo64;
                }

                return result;
            }
            return false;
        }

        // ----------------------------------------------------------------------------
        //
        // Explicit 32-bit and 64-bit versions of the control structs
        //
        // These use explicit 'remote type' fields instead of IntPtr,
        // to ensure the correct size and sign extension when compiled as either
        // 32-bit or 64-code. "for_alignment" fields are also added to the 64-bit
        // versions where necessary to obtain correct alignment.
        //
        // ----------------------------------------------------------------------------

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private struct MCGRIDINFO_32
        {
            internal uint cbSize;
            internal uint dwPart;
            internal uint dwFlags;
            internal int iCalendar;
            internal int iRow;
            internal int iCol;
            internal NativeMethods.SYSTEMTIME stStart;
            internal NativeMethods.SYSTEMTIME stEnd;
            internal NativeMethods.Win32Rect rc;
            internal int pszName;
            internal uint cchName;

            // This constructor should only be called when MCGRIDINFO is a 64 bit structure
            internal MCGRIDINFO_32(NativeMethods.MCGRIDINFO gridInfo)
            {
                cbSize = gridInfo.cbSize;
                dwPart = gridInfo.dwPart;
                dwFlags = gridInfo.dwFlags;
                iCalendar = gridInfo.iCalendar;
                iRow = gridInfo.iRow;
                iCol = gridInfo.iCol;
                stStart = gridInfo.stStart;
                stEnd = gridInfo.stEnd;
                rc = gridInfo.rc;
                pszName = 0;
                cchName = gridInfo.cchName;
            }

            // This operator should only be called when MCGRIDINFO is a 64 bit structure.
            static public explicit operator NativeMethods.MCGRIDINFO(MCGRIDINFO_32 gridInfo)
            {
                NativeMethods.MCGRIDINFO nativeGridInfo = new NativeMethods.MCGRIDINFO();

                nativeGridInfo.cbSize = gridInfo.cbSize;
                nativeGridInfo.dwPart = gridInfo.dwPart;
                nativeGridInfo.dwFlags = gridInfo.dwFlags;
                nativeGridInfo.iCalendar = gridInfo.iCalendar;
                nativeGridInfo.iRow = gridInfo.iRow;
                nativeGridInfo.iCol = gridInfo.iCol;
                nativeGridInfo.stStart = gridInfo.stStart;
                nativeGridInfo.stEnd = gridInfo.stEnd;
                nativeGridInfo.rc = gridInfo.rc;
                nativeGridInfo.pszName = IntPtr.Zero;
                nativeGridInfo.cchName = gridInfo.cchName;

                return nativeGridInfo;
            }
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        private struct MCGRIDINFO_64
        {
            internal uint cbSize;
            internal uint dwPart;
            internal uint dwFlags;
            internal int iCalendar;
            internal int iRow;
            internal int iCol;
            internal NativeMethods.SYSTEMTIME stStart;
            internal NativeMethods.SYSTEMTIME stEnd;
            internal NativeMethods.Win32Rect rc;
            internal long pszName;
            internal uint cchName;

            // This constructor should only be called when MCGRIDINFO is a 32 bit structure.
            internal MCGRIDINFO_64(NativeMethods.MCGRIDINFO gridInfo)
            {
                cbSize = gridInfo.cbSize;
                dwPart = gridInfo.dwPart;
                dwFlags = gridInfo.dwFlags;
                iCalendar = gridInfo.iCalendar;
                iRow = gridInfo.iRow;
                iCol = gridInfo.iCol;
                stStart = gridInfo.stStart;
                stEnd = gridInfo.stEnd;
                rc = gridInfo.rc;
                pszName = 0;
                cchName = gridInfo.cchName;
            }

            // This operator should only be called when MCGRIDINFO is a 32 bit structure.
            static public explicit operator NativeMethods.MCGRIDINFO(MCGRIDINFO_64 gridInfo)
            {
                NativeMethods.MCGRIDINFO nativeGridInfo = new NativeMethods.MCGRIDINFO();

                nativeGridInfo.cbSize = gridInfo.cbSize;
                nativeGridInfo.dwPart = gridInfo.dwPart;
                nativeGridInfo.dwFlags = gridInfo.dwFlags;
                nativeGridInfo.iCalendar = gridInfo.iCalendar;
                nativeGridInfo.iRow = gridInfo.iRow;
                nativeGridInfo.iCol = gridInfo.iCol;
                nativeGridInfo.stStart = gridInfo.stStart;
                nativeGridInfo.stEnd = gridInfo.stEnd;
                nativeGridInfo.rc = gridInfo.rc;
                nativeGridInfo.pszName = IntPtr.Zero;
                nativeGridInfo.cchName = gridInfo.cchName;

                return nativeGridInfo;
            }
        }

        #endregion

        // ------------------------------------------------------
        //
        // Private Fields
        //
        // ------------------------------------------------------

        #region Private Fields

        private bool _isVersion6;

        // All the variables below are only available during the calculation
        // of the bounding rectangle.
        private bool _fHasWeekNum;

        private bool _fHasToday;

        // rect of entire calendar
        private NativeMethods.Win32Rect _rcCalendar = new NativeMethods.Win32Rect ();

        private int _dyRow;

        // calc heights 
        private int _dyHeader;
        private int _dyWeekDay;
        private int _dyTodayLink = -1;
        private int _dyTitleHeaderPad;

        // If the Calendar window is larger than the calendar month,
        // the calendars are centered. This is the left margin for the first month.
        private NativeMethods.SIZE sizeMargin;

        // Windows hard coded constants.
        private const int CALBORDER = 6;

        #endregion

        // ------------------------------------------------------
        //
        // Calendar Items Controls
        //
        // ------------------------------------------------------

        #region Calendar Items Controls

        // ------------------------------------------------------
        //
        // Calendar Items Base Class
        //
        // ------------------------------------------------------

        #region Calendar Items Base Class

        // Child class of ProxyFragment, used as a base class for
        // calendar child items.  Each child item belongs to a
        // specific month calendar within the control.
        internal class CalendarBits: ProxyFragment
        {
            // ------------------------------------------------------
            //
            //  Constructors
            //
            //------------------------------------------------------

            #region Constructors

            internal CalendarBits (IntPtr hwnd, ProxyFragment parent,
                        int item, WindowsCalendar.CalendarItem type, int iCalendar)
                : base (hwnd, parent, item)
            {
                _type = type;
                _iCalendar = iCalendar;
            }

            #endregion

            //------------------------------------------------------
            //
            //  Patterns Implementation
            //
            //------------------------------------------------------

            #region ProxySimple Interface

            // Gets the bounding rectangle for this element
            internal override Rect BoundingRectangle
            {
                get
                {
                    WindowsCalendar winCalendar =
                        (WindowsCalendar) (_parent is WindowsCalendar ? _parent : _parent._parent);
                    winCalendar.CalcPositions (_iCalendar, out _rcMonth);
                    return BoundingRect().ToRect(Misc.IsControlRTL(_hwnd));
                }
            }

            #endregion

            //------------------------------------------------------
            //
            //  Internal Fields
            //
            //------------------------------------------------------

            #region Internal Fields

            // Known type by the calendar parent
            internal WindowsCalendar.CalendarItem _type;

            // A whole calendar consist of one or more month calendars.
            // This represents the 0-based index of a month calendar.
            internal int _iCalendar;

            #endregion

            //------------------------------------------------------
            //
            //  Protected Methods
            //
            //------------------------------------------------------

            #region Protected Methods

            #region ProxySimple Helper

            virtual protected NativeMethods.Win32Rect BoundingRect()
            {
                return NativeMethods.Win32Rect.Empty;
            }

            #endregion

            #region Invoke Helper

            protected void Invoke()
            {
                // Make sure that the control is enabled
                if (!SafeNativeMethods.IsWindowEnabled(_hwnd))
                {
                    throw new ElementNotEnabledException();
                }

                NativeMethods.Win32Point pt;
                if (GetClickablePoint(out pt, false))
                {
                    // Mouse method is used here because following methods fail:
                    //     -WM_MOUSEACTIVATE + WM_LBUTTONDOWN + WM_LBUTTONUP messages don't work with all
                    //     -WM_KEYDOWN + WM_KEYUP messages for space bar
                    //     -SendKeyboardInput for space bar
                    Misc.MouseClick(pt.x, pt.y);
                }
            }

            #endregion

            #endregion Protected Methods

            //------------------------------------------------------
            //
            //  Protected Fields
            //
            //------------------------------------------------------

            #region Protected Fields

            // The containing month calendar rectangle, in screen
            // coordinates.
            protected NativeMethods.Win32Rect _rcMonth = NativeMethods.Win32Rect.Empty;

            #endregion
        }
        #endregion
    
        // ------------------------------------------------------
        //
        //  CalendarButton Private Class
        //
        //------------------------------------------------------

        #region CalendarButton 

        // Child class of ProxyFragment that represents a calendar button
        class CalendarButton: CalendarBits, IInvokeProvider
        {
            // ------------------------------------------------------
            //
            //  Constructors
            //
            //------------------------------------------------------

            #region Constructors

            internal CalendarButton (IntPtr hwnd, ProxyFragment parent, ButtonItem item, WindowsCalendar.CalendarItem type)
            : base (hwnd, parent, (int) item, type, 0)
            {
                _cControlType = ControlType.Button;
                _sAutomationId = _item == (int)ButtonItem.Prev ? "SmallDecrement" : "SmallIncrement"; // This string is a non-localizable string
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
                return iid == InvokePattern.Pattern ? this : base.GetPatternProvider (iid);
            }

            internal override string LocalizedName
            {
                get
                {
                    if (_item == (int)ButtonItem.Prev)
                    {
                        return SR.Get(SRID.LocalizedNameWindowsCalendarButtonPrev);
                    }
                    else
                    {
                        return SR.Get(SRID.LocalizedNameWindowsCalendarButtonNext);
                    }
                }
            }

            #endregion

            #region Invoke Pattern

            // Same effect as a click. The action is implemented in each sub item.
            void IInvokeProvider.Invoke ()
            {
                Invoke();
            }

            #endregion

            // ------------------------------------------------------
            //
            // Internal Types Declaration
            //
            // ------------------------------------------------------

            #region Internal Types

            internal enum ButtonItem
            {
                Prev = -2,
                Next = -1
            }

            #endregion

            // ------------------------------------------------------
            //
            // Protected Methods
            //
            // ------------------------------------------------------

            #region Protected Methods

            // Returns the bounding rectangle of the control.
            protected override NativeMethods.Win32Rect BoundingRect()
            {
                WindowsCalendar winCalendar = (WindowsCalendar)_parent;
                if (winCalendar._isVersion6)
                {
                    uint dwPart;
                    switch (_item)
                    {
                        case (int)WindowsCalendar.CalendarItem.ButtonPrev:
                            dwPart = NativeMethods.MCGIP_PREV;
                            break;

                        case (int)WindowsCalendar.CalendarItem.ButtonNext:
                            dwPart = NativeMethods.MCGIP_NEXT;
                            break;

                        default:
                            return new NativeMethods.Win32Rect();
                    }

                    NativeMethods.Win32Rect rect;
                    winCalendar.GetCalendarPartRect(_iCalendar, dwPart, -1, -1, out rect);
                    return rect;
                }
                else
                {
                    // system metrics variables for determining ui element specific settings
                    int cxBorder = UnsafeNativeMethods.GetSystemMetrics(NativeMethods.SM_CXBORDER);
                    int cyHScroll = UnsafeNativeMethods.GetSystemMetrics(NativeMethods.SM_CYHSCROLL);

                    // arrow size definitions.  this information was taken from the calendar control source code
                    int DX_CALARROW = (cyHScroll * 4 / 3);
                    int DY_CALARROW = cyHScroll;
                    int DX_ARROWMARGIN = (5 * cxBorder);
                    NativeMethods.Win32Rect rcButton = new NativeMethods.Win32Rect();

                    rcButton.top = winCalendar._rcCalendar.top + (winCalendar._dyHeader - DY_CALARROW) / 2 + winCalendar.sizeMargin.cy;
                    rcButton.bottom = rcButton.top + DY_CALARROW;
                    switch (_item)
                    {
                        case (int)WindowsCalendar.CalendarItem.ButtonPrev:
                            if (!Misc.IsLayoutRTL(_hwnd))
                            {
                                rcButton.left = winCalendar._rcCalendar.left + winCalendar.sizeMargin.cx + DX_ARROWMARGIN;
                                rcButton.right = rcButton.left + DX_CALARROW;
                            }
                            else
                            {
                                rcButton.right = winCalendar._rcCalendar.right - (winCalendar.sizeMargin.cx + DX_ARROWMARGIN + 1);
                                rcButton.left = rcButton.right - DX_CALARROW;
                            }
                            return new NativeMethods.Win32Rect(rcButton.left, rcButton.top, rcButton.right, rcButton.bottom);

                        case (int)WindowsCalendar.CalendarItem.ButtonNext:
                            if (!Misc.IsLayoutRTL(_hwnd))
                            {
                                rcButton.right = winCalendar._rcCalendar.right - (winCalendar.sizeMargin.cx + DX_ARROWMARGIN + 1);
                                rcButton.left = rcButton.right - DX_CALARROW;
                            }
                            else
                            {
                                rcButton.left = winCalendar._rcCalendar.left + winCalendar.sizeMargin.cx + DX_ARROWMARGIN;
                                rcButton.right = rcButton.left + DX_CALARROW;
                            }
                            return new NativeMethods.Win32Rect(rcButton.left, rcButton.top, rcButton.right, rcButton.bottom);

                        default:
                            return base.BoundingRect(); //angle;
                    }
                }
            }

            #endregion
        }

        #endregion CalendarButton 

        // ------------------------------------------------------
        //
        //  CalendarYear Private Class
        //
        //------------------------------------------------------

        #region CalendarYear 

        // child class of ProxyFragment that represents the calendar year spin control
        class CalendarYear: CalendarBits, IInvokeProvider, IRangeValueProvider
        {

            // ------------------------------------------------------
            //
            //  Constructors
            //
            //------------------------------------------------------

            #region Constructors

            internal CalendarYear (
                    IntPtr hwnd, ProxyFragment parent, int item, WindowsCalendar.CalendarItem type)
            : base (hwnd, parent,
                    (int) WindowsCalendar.CalendarItem.Year
                        + item * (int) WindowsCalendar.CalendarItem.Last,
                    type, item)
            {
                _sType = SR.Get(SRID.LocalizedControlTypeCalendarYear);
                _sAutomationId = "Calendar.Year " + item.ToString(CultureInfo.CurrentCulture); // This string is a non-localizable string
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
                return iid == RangeValuePattern.Pattern || iid == InvokePattern.Pattern
                        ? this : base.GetPatternProvider (iid);
            }

            internal override string LocalizedName
            {
                get
                {
                    // Selection that span multiple years only show beginning year
                    DateTime dt =
                        WindowsCalendar.CreateDateTimeFromSystemTime1(
                            WindowsCalendar.GetRawValue(_hwnd, WindowsCalendar.CalendarData.Title));

                    dt = dt.AddMonths(_iCalendar);
                    return dt.ToString("yyyy", CultureInfo.CurrentCulture);
                }
            }

            #endregion

            #region RangeValue Pattern

            void IRangeValueProvider.SetValue (double val)
            {
                // It's Read Only...
                throw new InvalidOperationException(SR.Get(SRID.ValueReadonly));
            }

            bool IRangeValueProvider.IsReadOnly
            {
                get
                {
                    return true;
                }
            }

            double IRangeValueProvider.Value
            {
                get
                {
                    // Selection that span multiple years only show beginning year
                    DateTime [] dt =
                        WindowsCalendar.CreateDateTimeFromSystemTime (
                            WindowsCalendar.GetRawValue (
                                _hwnd, WindowsCalendar.CalendarData.Title));

                    return (double) (dt [0].Year + ((dt [0].Month - 1) + _iCalendar) / 12);
                }
            }

            double IRangeValueProvider.Maximum
            {
                get
                {
                    return GetPropertyRangeValue (RangeValuePattern.MaximumProperty, _hwnd);
                }
            }

            double IRangeValueProvider.Minimum
            {
                get
                {
                    return GetPropertyRangeValue (RangeValuePattern.MinimumProperty, _hwnd);
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

            #endregion Range Pattern

            #region Invoke Pattern

            // Same effect as a click. The action is implemented is each sub item.
            void IInvokeProvider.Invoke ()
            {
                Invoke();
            }

            #endregion Invoke Pattern

            //------------------------------------------------------
            //
            //  Protected Methods
            //
            //------------------------------------------------------

            #region Protected Methods

            #region Proxy Simple Helper

            protected override NativeMethods.Win32Rect BoundingRect()
            {
                WindowsCalendar winCalendar = (WindowsCalendar)_parent;

                if (winCalendar._isVersion6)
                {
                    // Return an empty rectangle since the calculation
                    // below is incorrect in the presence of e.g. the Aero
                    // theme on longhorn, and there is no way to obtain
                    // this rectangle directly from the calendar control.
                    return new NativeMethods.Win32Rect();
                }
                else
                {
                    int dxMonth, dxSeparator, dxYear;

                    winCalendar.CalcTitleTextExtents(
                        _iCalendar, _rcMonth, out dxMonth, out dxYear, out dxSeparator);

                    int width = _rcMonth.right - _rcMonth.left;
                    int dxHeaderPadding = (width - (dxMonth + dxSeparator + dxYear)) / 2;

                    int left = _rcMonth.left + dxHeaderPadding + dxMonth + dxSeparator;
                    int top = _rcMonth.top + winCalendar._dyTitleHeaderPad;
                    int right = left + dxYear;
                    int bottom = top + winCalendar._dyWeekDay;

                    return new NativeMethods.Win32Rect(left, top, right, bottom);
                }
            }

            #endregion

            #region Value Helper

            // This method sets a property value for all the listed supported properties.
            private static double GetPropertyRangeValue (AutomationProperty idProp, IntPtr hwnd)
            {
                NativeMethods.SYSTEMTIME [] systemTime = WindowsCalendar.GetRange (hwnd);

                if (idProp == RangeValuePattern.MinimumProperty || idProp == RangeValuePattern.MaximumProperty)
                {
                    int index = idProp == RangeValuePattern.MinimumProperty ? 0 : 1;

                    if (0 == systemTime [index].wYear
                            && 0 == systemTime [index].wMonth
                            && 0 == systemTime [index].wDayOfWeek
                            && 0 == systemTime [index].wDay
                            && 0 == systemTime [index].wHour
                            && 0 == systemTime [index].wMinute
                            && 0 == systemTime [index].wSecond
                            && 0 == systemTime [index].wMilliseconds)
                    {
                        // no limit was set for this control.  Set the limit to min of DateTime
                        if (idProp == RangeValuePattern.MinimumProperty)
                        {
                            return (double)1752;
                        }
                        else
                        {
                            return (double)9999;
                        }
                    }
                    else
                    {
                        return (double) systemTime [index].wYear;
                    }
                }
                return Double.NaN;
            }

            #endregion

            #endregion
        }

        #endregion CalendarYear subclass

        // ------------------------------------------------------
        //
        //  CalendarMonth Private Class
        //
        //------------------------------------------------------

        #region CalendarMonth sub class
    
        // child class of ProxyFragment that represents the calendar month
        // text area in the month header.
        // NOTE: this is a menu item, accessible by left-clicking on the
        // month text.
        class CalendarMonth : CalendarBits, IInvokeProvider, IExpandCollapseProvider
        {
            // ------------------------------------------------------
            //
            //  Constructors
            //
            //------------------------------------------------------

            #region Constructors

            internal CalendarMonth (
                    IntPtr hwnd, ProxyFragment parent, int item, WindowsCalendar.CalendarItem type)
            : base (hwnd, parent,
                    (int) WindowsCalendar.CalendarItem.Month
                            + item * (int) WindowsCalendar.CalendarItem.Last,
                    type, item)
            {
                _sType = SR.Get(SRID.LocalizedControlTypeCalendarMonth);
                _sAutomationId = "Calendar.Month " + item.ToString(CultureInfo.CurrentCulture); // This string is a non-localizable string
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
                return iid == InvokePattern.Pattern || iid == ExpandCollapsePattern.Pattern
                        ? this : base.GetPatternProvider(iid);
            }

            internal override string LocalizedName
            {
                get
                {
                    // Selection that span multiple years only show beginning year.
                    DateTime dt =
                        WindowsCalendar.CreateDateTimeFromSystemTime1(
                            WindowsCalendar.GetRawValue(_hwnd, WindowsCalendar.CalendarData.Title));

                    dt = dt.AddMonths(_iCalendar);
                    return dt.ToString("MMMM", CultureInfo.CurrentCulture);
                }
            }

            #endregion ProxySimple

            #region Invoke Pattern

            // Same effect as a click. The action is implemented is each sub item.
            void IInvokeProvider.Invoke ()
            {
                Invoke();
            }

            #endregion

            #region ExpandCollapse Pattern

            // Show all Children
            void IExpandCollapseProvider.Expand()
            {
                // Make sure that the control is enabled
                if (!SafeNativeMethods.IsWindowEnabled(_hwnd))
                {
                    throw new ElementNotEnabledException();
                }

                if (InCalendarMenuMode())
                {
                    // month menu is already visible
                    return;
                }

                if (MakeReadyForInput())
                {
                    ((IInvokeProvider)this).Invoke();
                }
            }

            // Hide all Children
            void IExpandCollapseProvider.Collapse()
            {
                // Make sure that the control is enabled
                if (!SafeNativeMethods.IsWindowEnabled(_hwnd))
                {
                    throw new ElementNotEnabledException();
                }

                if (!InCalendarMenuMode())
                {
                    // month menu is already collapsed
                    return;
                }

                if (MakeReadyForInput())
                {
                    ClearCalendarMenuMode();
                }
            }

            // Indicates an elements current Collapsed or Expanded state
            ExpandCollapseState IExpandCollapseProvider.ExpandCollapseState
            {
                get
                {
                    return (InCalendarMenuMode()) ? ExpandCollapseState.Expanded : ExpandCollapseState.Collapsed;
                }
            }

            #endregion ExpandCollapse Pattern

            //------------------------------------------------------
            //
            //  Protected Methods
            //
            //------------------------------------------------------

            #region Protected Methods

            #region Proxy Simple Helper

            protected override NativeMethods.Win32Rect BoundingRect()
            {
                WindowsCalendar winCalendar = (WindowsCalendar)_parent; 
                if (winCalendar._isVersion6)
                {
                    // Return an empty rectangle since the calculation
                    // below is incorrect in the presence of e.g. the Aero
                    // theme on longhorn, and there is no way to obtain
                    // this rectangle directly from the calendar control.
                    return new NativeMethods.Win32Rect();
                }
                else
                {
                    int dxMonth, dxSeparator, dxYear;

                    winCalendar.CalcTitleTextExtents(
                        _iCalendar, _rcMonth, out dxMonth, out dxYear, out dxSeparator);

                    int width = _rcMonth.right - _rcMonth.left;
                    int dxHeaderPadding = (width - (dxMonth + dxSeparator + dxYear)) / 2;

                    int left = _rcMonth.left + dxHeaderPadding;
                    int top = _rcMonth.top + winCalendar._dyTitleHeaderPad;
                    int right = left + dxMonth;
                    int bottom = top + winCalendar._dyWeekDay;

                    return new NativeMethods.Win32Rect(left, top, right, bottom);
                }
            }

            #endregion

            #region ExpandCollapse Helper
            // Checks to see if the process owning the hwnd is currently in menu mode
            // and takes steps to exit menu mode if it is
            protected void ClearCalendarMenuMode()
            {
                // Check if we're in menu mode with helper method.
                if (InCalendarMenuMode())
                {
                    // If we are, send an alt keypress to escape
                    Input.SendKeyboardInput(Key.LeftAlt, true);
                    Input.SendKeyboardInput(Key.LeftAlt, false);

                    // Wait for a few milliseconds for this operation to be completed
                    long dwTicks = (long)Environment.TickCount;

                    // Wait until the action has been completed
                    while (InCalendarMenuMode()
                            && ((long)Environment.TickCount - dwTicks) < Misc.MenuTimeOut)
                    {
                        // Sleep the shortest amount of time possible while
                        // still guaranteeing that some sleep occurs.
                        System.Threading.Thread.Sleep(1);
                    }
                }
            }

            // Detect if we're in the menu mode.
            private bool InCalendarMenuMode()
            {
                NativeMethods.GUITHREADINFO gui;
                uint processId;
                uint threadId = Misc.GetWindowThreadProcessId(_hwnd, out processId);
                return (Misc.ProxyGetGUIThreadInfo(threadId, out gui)
                            && (Misc.IsBitSet(gui.dwFlags, NativeMethods.GUI_POPUPMENUMODE)));
            }

            private bool MakeReadyForInput()
            {
                if (!SafeNativeMethods.IsWindowVisible(_hwnd))
                {
                    throw new ElementNotAvailableException();
                }

                NativeMethods.GUITHREADINFO gui;

                if (Misc.ProxyGetGUIThreadInfo(0, out gui) && _hwnd == gui.hwndActive)
                {
                    return true;
                }

                // try to set focus
                return Misc.SetFocus(_hwnd);
            }

            #endregion ExpandCollapse Helper

            #endregion
        }

        #endregion CalendarMonth sub class

        // ------------------------------------------------------
        //
        //  CalendarTodayLink Private Class
        //
        //------------------------------------------------------

        #region CalendarTodayLink 

        // Represents the "Today" row at the bottom of the calendar.
        internal class CalendarTodayLink: CalendarBits, IInvokeProvider
        {

            // ------------------------------------------------------
            //
            //  Constructors
            //
            //------------------------------------------------------

            #region Constructors

            internal CalendarTodayLink (
                        IntPtr hwnd, ProxyFragment parent, int item,
                        WindowsCalendar.CalendarItem type)
                : base (hwnd, parent, item, type, 0)
            {
                _sType = SR.Get(SRID.LocalizedControlTypeCalendarTodayButton);
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
                return iid == InvokePattern.Pattern ? this : base.GetPatternProvider (iid);
            }

            internal override string LocalizedName
            {
                get
                {
                    return SR.Get(SRID.LocalizedNameWindowsCalendarTodayLink);
                }
            }

            #endregion

            #region Invoke Pattern

            // Same effect as a click. The action is implemented is each sub item.
            void IInvokeProvider.Invoke ()
            {
                Invoke();
            }

            #endregion

            // ------------------------------------------------------
            //
            // Protected Methods
            //
            // ------------------------------------------------------

            #region Protected Methods

            #region Proxy Simple Helper

            protected override NativeMethods.Win32Rect BoundingRect()
            {
                WindowsCalendar winCalendar = (WindowsCalendar)_parent;
                if (winCalendar._isVersion6)
                {
                    NativeMethods.Win32Rect rect;
                    winCalendar.GetCalendarPartRect(_iCalendar, NativeMethods.MCGIP_FOOTER, -1, -1, out rect);
                    return rect;
                }
                else
                {
                    // size of button
                    NativeMethods.Win32Rect rcToday = winCalendar._rcCalendar;
                    rcToday.top = rcToday.bottom - winCalendar._dyTodayLink;
                    return new NativeMethods.Win32Rect(_rcMonth.left, rcToday.top, rcToday.right, rcToday.bottom);
                }
            }

            #endregion

            #endregion
        }

        #endregion CalendarTodayLink 

#if NoTodayCircle

        // ------------------------------------------------------
        //
        //  CalendarTodayCircle Private Class
        //
        //------------------------------------------------------

        #region CalendarTodayCircle 
    // child class of ProxyFragment that represents the calendar today circle item
    class CalendarTodayCircle: CalendarBits
    {
        internal CalendarTodayCircle(IntPtr hwnd, ProxyFragment parent, int item, WindowsCalendar.CalendarItem type)
            : base (hwnd, parent, item, type, item)
        {
            _sType = SR.Get(SRID.LocalizedControlTypeCalendarTodayCircle);
        }
    }
        #endregion CalendarTodayCircle 
    
#endif

        // ------------------------------------------------------
        //
        //  CalendarDates Private Class
        //
        //------------------------------------------------------

        #region CalendarDates 

        // Represents the area of the day-grid for a single calendar month,
        // including the actual dates of a month as well as the
        // week numbers (column headers) and day-of-week names (row headers).
        class CalendarDates : CalendarBits, IGridProvider, ITableProvider
        {
            // ------------------------------------------------------
            //
            //  Constructors
            //
            //------------------------------------------------------

            #region Constructors

            internal CalendarDates (
                IntPtr hwnd, ProxyFragment parent, int item, WindowsCalendar.CalendarItem type)
            : base (hwnd, parent,
                    (int) WindowsCalendar.CalendarItem.Dates + item * (int) WindowsCalendar.CalendarItem.Last,
                    type, item)
            {
                _cControlType = ControlType.List;
                _sType = SR.Get(SRID.LocalizedControlTypeCalendarDays);
                _sAutomationId = "Calendar.Days " + item.ToString(CultureInfo.CurrentCulture); // This string is a non-localizable string
                _winCalendar = (WindowsCalendar) _parent;
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
                return (iid == GridPattern.Pattern || iid == TablePattern.Pattern)
                        ? this : base.GetPatternProvider (iid);
            }

            internal override string LocalizedName
            {
                get
                {
                    return SR.Get(SRID.LocalizedNameWindowsCalendarDates);
                }
            }

            #endregion

            #region ProxyFragment Interface

            // Returns the next sibling element in the raw hierarchy.
            // Peripheral controls have always negative values.
            // Returns null if no next child
            internal override ProxySimple GetNextSibling (ProxySimple child)
            {
                int item = child._item + 1;
                ProxySimple nextSibling = null;

                if (HasWeekNumbers)
                {
                    while (item < FIRST_WEEKHEADER && nextSibling == null)
                    {
                        nextSibling = CreateCalendarWeekNum(item++);
                    }
                }

                while (item < 0 && nextSibling == null)
                {
                    nextSibling = CreateCalendarWeekHeader(item);
                }

                while (item < TOTAL_CELLS && nextSibling == null)
                {
                    nextSibling = CreateCalendarDay(item++);
                }

                return nextSibling;
            }

            // Returns the previous sibling element in the raw hierarchy.
            // Peripheral controls always have negative index values.
            // Returns null if no previous sibling exists.
            internal override ProxySimple GetPreviousSibling (ProxySimple child)
            {
                int item = child._item - 1;
                ProxySimple previousSibling = null;

                while (item >= 0 && previousSibling == null)
                {
                    previousSibling = CreateCalendarDay(item--);
                }

                while (item >= FIRST_WEEKHEADER && previousSibling == null)
                {
                    previousSibling = CreateCalendarWeekHeader(item--);
                }

                if (HasWeekNumbers)
                {
                    while (item >= FIRST_WEEKNUM && previousSibling == null)
                    {
                        previousSibling = CreateCalendarWeekNum(item--);
                    }
                }

                return previousSibling;
            }

            // Returns the first child element in the raw hierarchy.
            internal override ProxySimple GetFirstChild ()
            {
                if (HasWeekNumbers)
                {
                    return new CalendarWeekNum (_hwnd, this, FIRST_WEEKNUM, _iCalendar);
                }
                else
                {
                    return new CalendarWeekHeader (_hwnd, this, FIRST_WEEKHEADER, _iCalendar);
                }
            }

            // Returns the last child element in the raw hierarchy.
            internal override ProxySimple GetLastChild ()
            {
                ProxySimple lastChild = null;
                int dayIndex = TOTAL_CELLS - 1;
                if (_winCalendar._isVersion6)
                {
                    while (dayIndex >= 0 && lastChild == null)
                    {
                        lastChild = CreateCalendarDay(dayIndex--);
                    }
                }
                else
                {
                    lastChild = new CalendarDay(_hwnd, this, TOTAL_CELLS - 1, _iCalendar);
                }
                return lastChild;
            }

            #endregion

            #region Grid Pattern

            // Obtain the AutomationElement at an zero based absolute position in the grid.  
            // Where 0,0 is top left
            IRawElementProviderSimple IGridProvider.GetItem(int row, int column)
            {
                if (row < 0 || row >= MAX_WEEKS)
                {
                    throw new ArgumentOutOfRangeException("row", row, SR.Get(SRID.GridRowOutOfRange));
                }

                if (column < 0 || column >= MAX_DAYS)
                {
                    throw new ArgumentOutOfRangeException("column", column, SR.Get(SRID.GridColumnOutOfRange));
                }

                int dayIndex = CalendarDay.DayIndexFromRowColumn(row, column);
                return CreateCalendarDay(dayIndex);
            }

            // Number of Rows for the grid
            int IGridProvider.RowCount
            {
                get
                {
                    return MAX_WEEKS;
                }
            }

            // Number of Columns for the grid
            int IGridProvider.ColumnCount
            {
                get
                {
                    return MAX_DAYS;
                }
            }

            #endregion Grid Pattern

            #region Table Pattern

            // Collection of all Row Headers associated with the Table.
            IRawElementProviderSimple [] ITableProvider.GetRowHeaders ()
            {
                if (HasWeekNumbers)
                {
                    // Since the length may vary (5-row months can occur in 
                    // a multi-month calendar), build an ArrayList first, then
                    // build a fixed array.
                    ArrayList weekNumList = new ArrayList();
                    int count = 0;
                    for (int item = 0; item < MAX_WEEKS; item++)
                    {
                        CalendarWeekNum calendarWeekNum =
                            CreateCalendarWeekNum(item + FIRST_WEEKNUM);
                        if (calendarWeekNum != null)
                        {
                            count++;
                            weekNumList.Add(calendarWeekNum);
                        }
                    }

                    CalendarWeekNum[] aWeekNum = new CalendarWeekNum[count];
                    int arrayIndex = 0;
                    foreach (CalendarWeekNum calendarWeekNum in weekNumList)
                    {
                        if (calendarWeekNum != null)
                        {
                            aWeekNum[arrayIndex++] = calendarWeekNum;
                        }
                    }
                    return aWeekNum;
                }
                return null;
            }

            // Collection of all Column Headers associated with the Table.
            IRawElementProviderSimple [] ITableProvider.GetColumnHeaders ()
            {
                CalendarWeekHeader [] aWeekHeader = new CalendarWeekHeader [MAX_DAYS];
                for (int item = 0; item < MAX_DAYS; item++)
                {
                    aWeekHeader [item] = CreateCalendarWeekHeader(item + FIRST_WEEKHEADER);
                }
                return aWeekHeader;
            }

            // Describe the best way to present the information within this table. 
            RowOrColumnMajor ITableProvider.RowOrColumnMajor
            {
                get
                {
                    return RowOrColumnMajor.RowMajor;
                }
            }

            #endregion Table Pattern

            //------------------------------------------------------
            //
            //  Internal Fields
            //
            //------------------------------------------------------

            #region Internal Fields

            // total number of cells
            internal const int MAX_DAYS = 7;

            internal const int MAX_WEEKS = 6;
            internal const int MAX_WEEKSINYEAR = 52;

            #endregion

            //------------------------------------------------------
            //
            //  Protected Methods
            //
            //------------------------------------------------------

            #region Protected Methods

            #region Proxy Simple Helper

            // gets the bounding rectangle for the current ui element
            protected override NativeMethods.Win32Rect BoundingRect()
            {
                if (_winCalendar._isVersion6)
                {
                    NativeMethods.Win32Rect rectScreen;
                    _winCalendar.GetCalendarPartRect(
                        _iCalendar, NativeMethods.MCGIP_CALENDARBODY, 0, 0, out rectScreen);
                    return rectScreen;
                }
                else
                {
                    return new NativeMethods.Win32Rect(
                                _rcMonth.left, _rcMonth.top + _winCalendar._dyHeader,
                                _rcMonth.right, _rcMonth.bottom);
                }
            }

            #endregion // Proxy Simple Helper

            #region Private Methods

            private bool HasWeekNumbers
            {
                get
                {
                    return WindowsCalendar.HasStyle(_hwnd, NativeMethods.MCS_WEEKNUMBERS);
                }
            }

            private CalendarDay CreateCalendarDay(int dayIndex)
            {
                CalendarDay calendarDay = null;
                if (_winCalendar._isVersion6)
                {
                    // In version 6, we can tell which days are present.
                    int row = CalendarDay.RowOfDayIndex(dayIndex);
                    int col = CalendarDay.ColumnOfDayIndex(dayIndex);
                    string text =
                        _winCalendar.GetCalendarGridInfoText(
                            NativeMethods.MCGIP_CALENDARCELL, _iCalendar, row, col);
                    if (!string.IsNullOrEmpty(text))
                    {
                        // The day is present on the calendar, so create
                        // the requested day proxy.
                        calendarDay = new CalendarDay(_hwnd, this, dayIndex, _iCalendar);
                    }
                }
                else
                {
                    calendarDay = new CalendarDay(_hwnd, this, dayIndex, _iCalendar);
                }
                return calendarDay;
            }

            private CalendarWeekNum CreateCalendarWeekNum(int weekNumIndex)
            {
                CalendarWeekNum calendarWeekNum = null;
                if (_winCalendar._isVersion6)
                {
                    // In version 6, we can tell which week numbers are present.
                    int row = weekNumIndex - FIRST_WEEKNUM;
                    string text =
                        _winCalendar.GetCalendarGridInfoText(
                            NativeMethods.MCGIP_CALENDARCELL, _iCalendar, row, -1);
                    if (!string.IsNullOrEmpty(text))
                    {
                        // The week is present on the calendar, so create
                        // the requested week proxy.
                        calendarWeekNum = new CalendarWeekNum(_hwnd, this, weekNumIndex, _iCalendar);
                    }
                }
                else
                {
                    calendarWeekNum = new CalendarWeekNum(_hwnd, this, weekNumIndex, _iCalendar);
                }
                return calendarWeekNum;
            }

            private CalendarWeekHeader CreateCalendarWeekHeader(int weekHeaderIndex)
            {
                CalendarWeekHeader calendarWeekHeader = null;
                if (_winCalendar._isVersion6)
                {
                    // In version 6, we can tell which week headers are present.
                    int col = weekHeaderIndex - FIRST_WEEKHEADER;
                    string text =
                        _winCalendar.GetCalendarGridInfoText(
                            NativeMethods.MCGIP_CALENDARCELL, _iCalendar, -1, col);
                    if (!string.IsNullOrEmpty(text))
                    {
                        // The day-of-week is present on the calendar, so create
                        // the requested day-of-week proxy.
                        calendarWeekHeader = new CalendarWeekHeader(_hwnd, this, weekHeaderIndex, _iCalendar);
                    }
                }
                else
                {
                    calendarWeekHeader = new CalendarWeekHeader(_hwnd, this, weekHeaderIndex, _iCalendar);
                }
                return calendarWeekHeader;
            }

            #endregion // Private Methods

            #region Proxy Fragment Helper

            private CalendarBits FromPoint_V6(int x, int y, uint hitLocationFlags)
            {
                CalendarBits calendarBits = null;
                NativeMethods.MCHITTESTINFO_V6 hitTestInfo = new NativeMethods.MCHITTESTINFO_V6();
                if (_winCalendar.GetHitTestInfo_V6(x, y, ref hitTestInfo))
                {
                    switch (hitLocationFlags)
                    {
                        case NativeMethods.MCHT_CALENDARDAY:
                            if (hitTestInfo.iRow == -1 && hitTestInfo.iCol >= 0)
                            {
                                calendarBits =
                                    CreateCalendarWeekHeader(FIRST_WEEKHEADER + hitTestInfo.iCol);
                            }
                            break;

                        case NativeMethods.MCHT_CALENDARWEEKNUM:
                            if (hitTestInfo.iCol == -1 && hitTestInfo.iRow >= 0)
                            {
                                calendarBits =
                                    CreateCalendarWeekNum(FIRST_WEEKNUM + hitTestInfo.iRow);
                            }
                            break;

                        case NativeMethods.MCHT_CALENDARDATE:
                            if (hitTestInfo.iCol >= 0 && hitTestInfo.iRow >= 0)
                            {
                                int dayIndex =
                                    CalendarDay.DayIndexFromRowColumn(
                                                    hitTestInfo.iRow, hitTestInfo.iCol);
                                calendarBits = CreateCalendarDay(dayIndex);
                            }
                            break;
                    }
                }
                if (calendarBits == null)
                {
                    calendarBits = (CalendarBits)this;
                }
                return calendarBits;
            }

            internal CalendarBits FromPoint (int x, int y, uint hitLocationFlags)
            {
                if (_winCalendar._isVersion6)
                {
                    return FromPoint_V6(x, y, hitLocationFlags);
                }
                else
                {
                    _winCalendar.CalcPositions(_iCalendar, out _rcMonth);

                    NativeMethods.Win32Rect rc = BoundingRect();
                    int dxRow = (rc.right - rc.left) / (_winCalendar._fHasWeekNum ? MAX_DAYS + 1 : MAX_DAYS);

                    switch (hitLocationFlags)
                    {
                        case NativeMethods.MCHT_CALENDARDAY:
                            {
                                // Shift into the coordinate system of the Dates rectangle
                                if (Misc.IsLayoutRTL(_hwnd))
                                {
                                    x = rc.right - x;
                                }
                                else
                                {
                                    x -= rc.left;
                                }
                                y -= rc.top;

                                int iCol = x / dxRow - (_winCalendar._fHasWeekNum ? 1 : 0);
                                int iLine = y / _winCalendar._dyRow;

                                return x < 0 || iCol >= MAX_DAYS || iLine < 0 || iLine >= 1
                                    ? (CalendarBits) this
                                    : new CalendarWeekHeader(_hwnd, this, FIRST_WEEKHEADER + iCol, _iCalendar);
                            }

                        case NativeMethods.MCHT_CALENDARWEEKNUM:
                            {
                                // Shift into the coordinate system of the Dates rectangle
                                if (Misc.IsLayoutRTL(_hwnd))
                                {
                                    x = rc.right - x;
                                }
                                else
                                {
                                    x -= rc.left;
                                }
                                y -= rc.top + _winCalendar._dyWeekDay;

                                int iCol = x / dxRow;
                                int iLine = y / _winCalendar._dyRow;

                                return x < 0 || iCol >= 1 || iLine < 0 || iLine >= MAX_WEEKS
                                    ? (CalendarBits)this
                                    : new CalendarWeekNum(_hwnd, this, FIRST_WEEKNUM + iLine, _iCalendar);
                            }

                        case NativeMethods.MCHT_CALENDARDATE:
                        default:
                            {
                                // Shift into the coordinate system of the Dates rectangle
                                if (Misc.IsLayoutRTL(_hwnd))
                                {
                                    x = rc.right - x;
                                }
                                else
                                {
                                    x -= rc.left;
                                }
                                y -= rc.top + _winCalendar._dyWeekDay;

                                int iCol = x / dxRow - (_winCalendar._fHasWeekNum ? 1 : 0);
                                int iLine = y / _winCalendar._dyRow;
                                int iDay = CalendarDay.DayIndexFromRowColumn(iLine, iCol);
                                return iDay < 0 || iDay > TOTAL_CELLS || x < 0 || iLine < 0
                                                || iCol >= MAX_DAYS || iLine >= MAX_WEEKS
                                    ? (CalendarBits)this
                                    : new CalendarDay(_hwnd, this, iDay, _iCalendar);
                            }
                    }
                }
            }

            #endregion // Proxy Fragment Helper

            #endregion // Protected Methods

            //------------------------------------------------------
            //
            //  Protected Fields
            //
            //------------------------------------------------------

            #region Protected Fields

            protected enum DatesItem
            {
                Day,
                WeekHeader,
                WeekNumber
            }

            // The items ids ranging from 
            //      FIRST_WEEKNUM .. FIRST_WEEKHEADER -> contains WeekNumbers
            //      FIRST_WEEKHEADER .. 0 -> contains Week Headers
            //      0 .. MAX_DAYS * MAX_WEEK days
            //  Weeks number are optional the others aren't 
            private const int TOTAL_CELLS = MAX_DAYS * MAX_WEEKS;

            private const int FIRST_WEEKNUM = -(MAX_WEEKS + MAX_DAYS);
            private const int FIRST_WEEKHEADER = -MAX_DAYS;

            #endregion

            #region Private Fields
            WindowsCalendar _winCalendar;
            #endregion Private Fields

            #region CalendarGridItem

            // ------------------------------------------------------
            //
            //  CalendarGridItem Private Class
            //
            //------------------------------------------------------

            // Represents a cell within a calendar month.
            // This can be a day of the month, a week number (row header)
            // or a week header (column header).
            internal class CalendarGridItem: CalendarBits
            {

                // ------------------------------------------------------
                //
                //  Constructors
                //
                //------------------------------------------------------

                #region Constructors

                internal CalendarGridItem (IntPtr hwnd, ProxyFragment parent, int item, int iCalendar)
                : base (hwnd, parent, item, WindowsCalendar.CalendarItem.GridItem, iCalendar)
                {
                    _winCalendar = (WindowsCalendar) _parent._parent;
                }

                #endregion

                //------------------------------------------------------
                //
                //  Protected Fields
                //
                //------------------------------------------------------

                #region Protected Fields

                // Reference to the windows calendar
                protected WindowsCalendar _winCalendar;

                #endregion

            }

            #endregion

            // ------------------------------------------------------
            //
            //  CalendarWeekNum Private Class
            //
            //------------------------------------------------------

            #region CalendarWeekNum

            // Child class of ProxySimple that represents the number of
            // a week within the year. Week numbers are only present
            // if the MCS_WEEKNUMBERS style is present on the control.
            class CalendarWeekNum: CalendarGridItem
            {

                // ------------------------------------------------------
                //
                //  Constructors
                //
                //------------------------------------------------------

                #region Constructors

                internal CalendarWeekNum (IntPtr hwnd, ProxyFragment parent, int item, int iCalendar)
                : base (hwnd, parent, item, iCalendar)
                {
                    _cControlType = ControlType.HeaderItem;
                    _sType = SR.Get(SRID.LocalizedControlTypeCalendarWeekNumber);
                    _sAutomationId = "Calendar.WeekNumber " + item.ToString(CultureInfo.CurrentCulture); // This string is a non-localizable string
                }

                #endregion

                // ------------------------------------------------------
                //
                // Patterns Implementation
                //
                // ------------------------------------------------------

                #region ProxySimple Interface

                internal override string LocalizedName
                {
                    get
                    {
                        if (_winCalendar._isVersion6)
                        {
                            int row = RowOfItemIndex(_item);
                            return _winCalendar.GetCalendarGridInfoText(
                                        NativeMethods.MCGIP_CALENDARCELL, _iCalendar, row, -1);
                        }
                        else
                        {
                            // Have a go at the date in the second week, first day of the week.
                            _winCalendar.CalcPositions(_iCalendar, out _rcMonth);

                            int cxCell = (_rcMonth.right - _rcMonth.left)
                                                / (_winCalendar._fHasWeekNum
                                                        ? CalendarDates.MAX_DAYS + 1 : CalendarDates.MAX_DAYS);
                            int cyCell = _winCalendar._dyRow;
                            int iY = 2;
                            int top = _rcMonth.top + _winCalendar._dyHeader + iY * cyCell;

                            // Use the Hittest mechanism to figure out the day of the month
                            NativeMethods.MCHITTESTINFO hitTestInfo = new NativeMethods.MCHITTESTINFO();

                            // Set the point of interest.
                            if (Misc.IsLayoutRTL(_hwnd))
                            {
                                hitTestInfo.pt =
                                    new NativeMethods.Win32Point(_rcMonth.right - 2 * cxCell / 2, top + cyCell / 2);
                            }
                            else
                            {
                                hitTestInfo.pt =
                                    new NativeMethods.Win32Point(_rcMonth.left + 2 * cxCell / 2, top + cyCell / 2);
                            }
                            hitTestInfo.cbSize = (uint)Marshal.SizeOf(hitTestInfo);

                            // Convert the coordinates for the point of interest from
                            // screen coordinates to window-relative coordinates.
                            if (!Misc.MapWindowPoints(IntPtr.Zero, _hwnd, ref hitTestInfo.pt, 1))
                            {
                                return "";
                            }
                            unsafe
                            {
                                XSendMessage.XSend(_hwnd, NativeMethods.MCM_HITTEST,
                                                   IntPtr.Zero, new IntPtr(&hitTestInfo),
                                                   Marshal.SizeOf(hitTestInfo.GetType()));
                            }
                            if (hitTestInfo.uHit == NativeMethods.MCHT_CALENDARDATE)
                            {
                                // Get the first day of the week
                                int iDayWeek =
                                    Misc.ProxySendMessageInt(
                                        _hwnd, NativeMethods.MCM_GETFIRSTDAYOFWEEK, IntPtr.Zero, IntPtr.Zero);
                                // Strip the high bits, which can contain 0x00010000.
                                iDayWeek &= 0x0000ffff;

                                // Map the day into the real week number,
                                // Add an extra one as the first day of the
                                // second week was return by the previous call
                                int cDeltaWeek = _item - (FIRST_WEEKNUM + 1);
                                int iWeek2 = GetWeekNumber(hitTestInfo.st, iDayWeek, 0);
                                int iWeek = iWeek2 + cDeltaWeek;

                                // Adjust the week on a calendar year change
                                if (iWeek < 1)
                                    iWeek += MAX_WEEKSINYEAR;

                                if (iWeek > MAX_WEEKSINYEAR)
                                    iWeek -= MAX_WEEKSINYEAR;

                                return iWeek.ToString(CultureInfo.CurrentCulture);
                            }

                            return "";
                        }
                    }
                }

                #endregion

                //------------------------------------------------------
                //
                //  Protected Methods
                //
                //------------------------------------------------------

                #region Protected Methods

                #region ProxySimple Helper

                protected override NativeMethods.Win32Rect BoundingRect()
                {
                    if (_winCalendar._isVersion6)
                    {
                        int row = RowOfItemIndex(_item);
                        NativeMethods.Win32Rect rect;
                        _winCalendar.GetCalendarPartRect(
                            _iCalendar, NativeMethods.MCGIP_CALENDARCELL, row, -1, out rect);
                        return rect;
                    }
                    else
                    {
                        int cxCell = (_rcMonth.right - _rcMonth.left)
                                        / (_winCalendar._fHasWeekNum
                                                ? CalendarDates.MAX_DAYS + 1 : CalendarDates.MAX_DAYS);
                        int cyCell = _winCalendar._dyRow;
                        int iY = RowOfItemIndex(_item) + 1; // +1 for week column headers
                        int top = _rcMonth.top + _winCalendar._dyHeader + iY * cyCell;

                        if (Misc.IsLayoutRTL(_hwnd))
                        {
                            return new NativeMethods.Win32Rect(
                                _rcMonth.right - cxCell + 2, top, _rcMonth.right, top + cyCell);
                        }
                        else
                        {
                            return new NativeMethods.Win32Rect(
                                _rcMonth.left, top, _rcMonth.left + cxCell, top + cyCell);
                        }
                    }
                }

                #endregion ProxySimple Helper

                #endregion Protected Methods

                #region Internal Methods
                internal static int RowOfItemIndex(int item)
                {
                    return (item - FIRST_WEEKNUM);
                }
                #endregion Internal Methods

                //------------------------------------------------------
                //
                //  Private Methods
                //
                //------------------------------------------------------

                #region Private Methods

                #region Week Number Calculation

                // taken from scdttime.h
                private int GetStartDowForMonth (int yr, int mo)
                {
                    int dow;

                    // we want monday = 0, sunday = 6
                    // dow = 6 + (yr - 1) + ((yr - 1) >> 2);
                    dow = 5 + (yr - 1) + ((yr - 1) >> 2);
                    if (yr > 1752)
                    {
                        dow += ((yr - 1) - 1600) / 400 - ((yr - 1) - 1700) / 100 - 11;
                    }
                    else if (yr == 1752 && mo > 9)
                    {
                        dow -= 11;
                    }

                    dow += _mpcdymoAccum [mo - 1];
                    if (mo > 2 && (yr & 03) == 0 && (yr <= 1750 || yr % 100 != 0 || yr % 400 == 0))
                    {
                        dow++;
                    }

                    dow %= 7;
                    return dow;
                }

                // Calculate the number of days between two dates as expressed in YMD's.
                private int DaysBetweenDates (
                        NativeMethods.SYSTEMTIME pstStart, NativeMethods.SYSTEMTIME pstEnd)
                {
                    int cday;
                    UInt16 yr;

                    // Calculate number of days between the start month/day and the
                    // end month/day as if they were in the same year - since cday
                    // is unsigned, cday could be really large if the end month/day
                    // is before the start month.day.
                    // This will be cleared up when we account for the days between
                    // the years.
                    cday = _mpcdymoAccum [pstEnd.wMonth - 1]
                                - _mpcdymoAccum [pstStart.wMonth - 1] + pstEnd.wDay - pstStart.wDay;
                    yr = pstStart.wYear;

                    // Check to see if the start year is before the end year,
                    // and if the end month is after February and
                    // if the end year is a leap year, then add an extra day
                    // for to account for Feb. 29 in the end year.
                    if ((yr < pstEnd.wYear || pstStart.wMonth <= 2)
                            && pstEnd.wMonth > 2
                            && (pstEnd.wYear & 03) == 0
                            && (pstEnd.wYear <= 1750 || pstEnd.wYear % 100 != 0 || pstEnd.wYear % 400 == 0))
                    {
                        cday++;
                    }

                    // Now account for the leap years in between the start and end dates
                    // as well as accounting for the days in each year.
                    if (yr < pstEnd.wYear)
                    {
                        // If the start date is before march and the start year is
                        // a leap year then add an extra day to account for Feb. 29.
                        if (pstStart.wMonth <= 2 && (yr & 03) == 0 && (yr <= 1750 || yr % 100 != 0 || yr % 400 == 0))
                        {
                            cday++;
                        }

                        // Account for the days in each year (disregarding leap years).
                        cday += 365;
                        yr++;

                        // Keep on accounting for the days in each year including leap
                        // years until we reach the end year.
                        while (yr < pstEnd.wYear)
                        {
                            cday += 365;
                            if ((yr & 03) == 0 && (yr <= 1750 || yr % 100 != 0 || yr % 400 == 0))
                            {
                                cday++;
                            }

                            yr++;
                        }
                    }

                    return (cday);
                }

                // Calculates week number in which a given date occurs, based on a 
                // specified start-day of week.  Adjusts based on how a calendar would 
                // show this week (ie. week 53 is probably week 1 on the calendar).
                private int GetWeekNumber (NativeMethods.SYSTEMTIME pst, int dowFirst, int woyFirst)
                {
                    int day, ddow, ddowT, nweek;
                    NativeMethods.SYSTEMTIME st = new NativeMethods.SYSTEMTIME ();

                    st.wYear = pst.wYear;
                    st.wMonth = 1;
                    st.wDay = 1;
                    ddow = GetStartDowForMonth (st.wYear, st.wMonth) - dowFirst;
                    if (ddow < 0)
                    {
                        ddow += 7;
                    }

                    if (pst.wMonth == 1 && pst.wDay < 8 - (ushort) ddow)
                    {
                        nweek = 0;
                    }
                    else
                    {
                        if (ddow != 0)
                        {
                            st.wDay = (ushort) (8 - ddow);
                        }

                        nweek = (DaysBetweenDates (st, pst) / 7) + 1;
                    }

                    if ((ddow != 0) && (ddow <= 3))
                    {
                        nweek++;
                    }

                    // adjust if necessary for calendar
                    if (nweek == 0)
                    {
                        if (ddow == 0)
                        {
                            return (1);
                        }

                        // check what week Dec 31 is on
                        st.wYear--;
                        st.wMonth = 12;
                        st.wDay = 31;
                        return (GetWeekNumber (st, dowFirst, woyFirst));
                    }
                    else if (nweek >= 52)
                    {
                        ddowT = (GetStartDowForMonth (pst.wYear, pst.wMonth) + pst.wDay - 1 + 7 - dowFirst) % 7;
                        day = pst.wDay + (7 - ddowT);
                        if (day > 31 + 4)
                        {
                            nweek = 1;
                        }
                    }

                    return nweek;
                }

                #endregion

                #endregion

                //------------------------------------------------------
                //
                //  Private Fields
                //
                //------------------------------------------------------

                #region Private Fields

                // list to get month, year calculations correct.
                private int [] _mpcdymoAccum = new int [13]
                {
                    0, 31, 59, 90, 120, 151, 181, 212, 243, 273, 304, 334, 365
                };

                #endregion

            }

            #endregion CalendarWeekNum

            // ------------------------------------------------------
            //
            //  CalendarWeekHeader Private Class
            //
            //------------------------------------------------------

            #region CalendarWeekHeader 

            // child class of ProxyFragment that represents the calendar date item
            // NOTE: this would be the place to implement Grid and Box for date
            internal class CalendarWeekHeader: CalendarGridItem
            {

                // ------------------------------------------------------
                //
                //  Constructors
                //
                //------------------------------------------------------

                #region Constructors

                internal CalendarWeekHeader (IntPtr hwnd, ProxyFragment parent, int item, int iCalendar)
                : base (hwnd, parent, item, iCalendar)
                {
                    _cControlType = ControlType.HeaderItem;
                    _sType = SR.Get(SRID.LocalizedControlTypeCalendarDayOfWeek);
                    int adjustedItem = item + 8;
                    _sAutomationId = "Calendar.DayOfWeek " + adjustedItem.ToString(CultureInfo.CurrentCulture); // This string is a non-localizable string
                }

                #endregion

                // ------------------------------------------------------
                //
                // Patterns Implementation
                //
                // ------------------------------------------------------

                #region ProxySimple Interface

                internal override string LocalizedName
                {
                    get
                    {
                        if (_winCalendar._isVersion6)
                        {
                            int col = ColumnOfItemIndex(_item);
                            return _winCalendar.GetCalendarGridInfoText(
                                        NativeMethods.MCGIP_CALENDARCELL, _iCalendar, -1, col);
                        }
                        else
                        {
                            _winCalendar.CalcPositions(_iCalendar, out _rcMonth);

                            int cxCell = (_rcMonth.right - _rcMonth.left)
                                            / (_winCalendar._fHasWeekNum
                                                    ? CalendarDates.MAX_DAYS + 1 : CalendarDates.MAX_DAYS);
                            int cyCell = _winCalendar._dyRow;
                            int iX = ColumnOfItemIndex(_item);
                            int left;
                            if (Misc.IsLayoutRTL(_hwnd))
                            {
                                left = (_rcMonth.right - cxCell) - (iX * cxCell) - (_winCalendar._fHasWeekNum ? cxCell : 0);
                            }
                            else
                            {
                                left = _rcMonth.left + iX * cxCell + (_winCalendar._fHasWeekNum ? cxCell : 0);
                            }

                            // Go down by 2 cells as this one will always be valid
                            int top = _rcMonth.top + _winCalendar._dyHeader + 2 * cyCell;

                            // Use the Hittest mechanism to figure out the day of the month
                            NativeMethods.MCHITTESTINFO hitTestInfo = new NativeMethods.MCHITTESTINFO();

                            // Set the point of interest.
                            hitTestInfo.pt = new NativeMethods.Win32Point(left + cxCell / 2, top + cyCell / 2);
                            hitTestInfo.cbSize = (uint)Marshal.SizeOf(hitTestInfo);

                            // Convert the coordinates for the point of interest from
                            // screen coordinates to window-relative coordinates.
                            if (!Misc.MapWindowPoints(IntPtr.Zero, _hwnd, ref hitTestInfo.pt, 1))
                            {
                                return "";
                            }
                            unsafe
                            {
                                XSendMessage.XSend(_hwnd, NativeMethods.MCM_HITTEST,
                                    IntPtr.Zero, new IntPtr(&hitTestInfo), Marshal.SizeOf(hitTestInfo.GetType()));
                            }

                            DateTime dt = WindowsCalendar.CreateDateTimeFromSystemTime1(hitTestInfo.st);

                            return dt.ToString("dddd", CultureInfo.CurrentCulture);
                        }
                    }
                }
                #endregion

                //------------------------------------------------------
                //
                //  Protected Methods
                //
                //------------------------------------------------------

                #region Protected Methods

                #region ProxySimple Helper

                protected override NativeMethods.Win32Rect BoundingRect()
                {
                    if (_winCalendar._isVersion6)
                    {
                        int column = ColumnOfItemIndex(_item);
                        NativeMethods.Win32Rect rect;
                        _winCalendar.GetCalendarPartRect(
                            _iCalendar, NativeMethods.MCGIP_CALENDARCELL, -1, column, out rect);
                        return rect;
                    }
                    else
                    {
                        int cxCell = (_rcMonth.right - _rcMonth.left)
                                            / (_winCalendar._fHasWeekNum
                                                    ? CalendarDates.MAX_DAYS + 1 : CalendarDates.MAX_DAYS);
                        int cyCell = _winCalendar._dyRow;
                        int iX = ColumnOfItemIndex(_item);

                        int left;
                        if (Misc.IsLayoutRTL(_hwnd))
                        {
                            left = (_rcMonth.right - cxCell) - (iX * cxCell) - (_winCalendar._fHasWeekNum ? cxCell : 0);
                        }
                        else
                        {
                            left = _rcMonth.left + iX * cxCell + (_winCalendar._fHasWeekNum ? cxCell : 0);
                        }
                        int top = _rcMonth.top + _winCalendar._dyHeader;

                        return new NativeMethods.Win32Rect(left, top, left + cxCell, top + cyCell);
                    }
                }

                #endregion ProxySimple Helper

                #endregion Protected Methods

                //------------------------------------------------------
                //
                //  Internal Methods
                //
                //------------------------------------------------------

                #region Internal Methods
                internal static int ColumnOfItemIndex(int item)
                {
                    return (item - FIRST_WEEKHEADER);
                }
                #endregion Internal Methods
            }

            #endregion CalendarWeekHeader 

            // ------------------------------------------------------
            //
            //  CalendarDay Private Class
            //
            //------------------------------------------------------

            #region CalendarDay 

            // Child class of ProxyFragment that represents an actual day on the calendar.
            internal class CalendarDay :
                CalendarGridItem, IInvokeProvider, IGridItemProvider,
                ITableItemProvider, ISelectionItemProvider
            {

                // ------------------------------------------------------
                //
                //  Constructors
                //
                //------------------------------------------------------

                #region Constructors

                internal CalendarDay (IntPtr hwnd, ProxyFragment parent, int item, int iCalendar)
                    : base (hwnd, parent, item, iCalendar)
                {
                    _winCalendar = (WindowsCalendar) parent._parent;
                    _cControlType = ControlType.ListItem;
                    _sType = SR.Get(SRID.LocalizedControlTypeCalendarDay);
                    _sAutomationId = GetAutomationId();

                    _fIsContent = (GetDateTime() != DateTime.MinValue);
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
                    return
                        iid == InvokePattern.Pattern
                            || iid == GridItemPattern.Pattern
                            || (iid == SelectionItemPattern.Pattern
                                    && GetDateTime() != DateTime.MinValue)
                            || iid == TableItemPattern.Pattern
                        ? this : base.GetPatternProvider(iid);
                }

                // Process all the Logical and Raw Element Properties
                internal override object GetElementProperty (AutomationProperty idProp)
                {
                    if(idProp == AutomationElement.IsControlElementProperty)
                    {
                        return (GetDateTime() != DateTime.MinValue);
                    }

                    return base.GetElementProperty(idProp);
                }

                internal override string LocalizedName
                {
                    get
                    {
                        DateTime itemDateTime = GetDateTime();
                        return (itemDateTime != DateTime.MinValue)
                            ? itemDateTime.Day.ToString(CultureInfo.CurrentCulture) : null;
                    }
                }

                #endregion

                #region Invoke Pattern

                // Same effect as a click. The action is implemented is each sub item.
                void IInvokeProvider.Invoke ()
                {
                    Invoke();
                }

                #endregion

                #region Grid Pattern

                int IGridItemProvider.Row
                {
                    get
                    {
                        return RowOfDayIndex(_item);
                    }
                }

                int IGridItemProvider.Column
                {
                    get
                    {
                        return ColumnOfDayIndex(_item);
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
                        return _parent;
                    }
                }

                #endregion Grid Pattern

                #region Table Pattern

                IRawElementProviderSimple [] ITableItemProvider.GetRowHeaderItems ()
                {
                    if (WindowsCalendar.HasStyle(_hwnd, NativeMethods.MCS_WEEKNUMBERS))
                    {
                        IRawElementProviderSimple [] aRawElement = new IRawElementProviderSimple [1];
                        int weekIndex = RowOfDayIndex(_item) + FIRST_WEEKNUM;
                        aRawElement [0] =
                            new CalendarWeekNum (_hwnd, _parent, weekIndex, _iCalendar);
                        return aRawElement;
                    }
                    return null;
                }

                IRawElementProviderSimple [] ITableItemProvider.GetColumnHeaderItems ()
                {
                    IRawElementProviderSimple [] aRawElement = new IRawElementProviderSimple [1];
                    int dayOfWeekIndex = ColumnOfDayIndex(_item) + FIRST_WEEKHEADER;
                    aRawElement [0] =
                        new CalendarWeekHeader (_hwnd, _parent, dayOfWeekIndex, _iCalendar);
                    return aRawElement;
                }

                #endregion Table Pattern

                #region SelectionItem Pattern

                // Selects this element
                void ISelectionItemProvider.Select ()
                {
                    // Make sure that the control is enabled
                    if (!SafeNativeMethods.IsWindowEnabled(_hwnd))
                    {
                        throw new ElementNotEnabledException();
                    }

                    // Get the DateTime for the current cell.
                    DateTime itemDateTime = GetDateTime();

                    // Could be a cell without a date.
                    if (itemDateTime == DateTime.MinValue)
                    {
                        throw new InvalidOperationException(SR.Get(SRID.OperationCannotBePerformed));
                    }

                    DateTime [] dateSelection = new DateTime [2];

                    dateSelection[0] = dateSelection[1] = itemDateTime;

                    // do the selection
                    if (!WindowsCalendar.SetRawValue(_hwnd, dateSelection))
                    {
                        throw new InvalidOperationException(SR.Get(SRID.OperationCannotBePerformed));
                    }
                }

                // Adds this element to the selection
                void ISelectionItemProvider.AddToSelection ()
                {
                    // Make sure that the control is enabled
                    if (!SafeNativeMethods.IsWindowEnabled(_hwnd))
                    {
                        throw new ElementNotEnabledException();
                    }

                    ISelectionItemProvider selProvider = (ISelectionItemProvider) this;

                    // check if item already selected
                    if (selProvider.IsSelected)
                    {
                        return;
                    }

                    // if does not support multiple selection
                    // and other element(not us) is already selected return false
                    if (!WindowsCalendar.IsMultiSelect(_hwnd))
                    {
                        throw new InvalidOperationException(SR.Get(SRID.DoesNotSupportMultipleSelection));
                    }

                    DateTime [] dateSelection =
                        WindowsCalendar.CreateDateTimeFromSystemTime (
                            WindowsCalendar.GetRawValue (_hwnd, WindowsCalendar.CalendarData.Selection));
                    DateTime itemDateTime = GetDateTime();

                    // Valid grid item but no date int the cell, before beginning or end of the week
                    if (itemDateTime == DateTime.MinValue)
                    {
                        throw new InvalidOperationException(SR.Get(SRID.OperationCannotBePerformed));
                    }

                    // The day can only be added if it 
                    TimeSpan ts = dateSelection[0] - itemDateTime;

                    if (ts.Days == 1)
                    {
                        dateSelection [0] = dateSelection [0].AddDays (-1);
                    }
                    else if ((ts = (itemDateTime - dateSelection[1])).Days == 1)
                    {
                        dateSelection [1] = dateSelection [1].AddDays (1);
                    }
                    else
                    {
                        // Probably should throw
                        // cannot add
                        throw new InvalidOperationException(SR.Get(SRID.OperationCannotBePerformed));
                    }

                    // check if there is enough space to add an element
                    int cSelCount = Misc.ProxySendMessageInt(
                                        _hwnd, NativeMethods.MCM_GETMAXSELCOUNT, IntPtr.Zero, IntPtr.Zero);
                    if ((dateSelection [1] - dateSelection [0]).Days >= cSelCount)
                    {
                        throw new InvalidOperationException(SR.Get(SRID.OperationCannotBePerformed));
                    }

                    // do the selection
                    if (!WindowsCalendar.SetRawValue(_hwnd, dateSelection))
                    {
                        throw new InvalidOperationException(SR.Get(SRID.OperationCannotBePerformed));
                    }
                }

                // Removes this element from the selection
                void ISelectionItemProvider.RemoveFromSelection ()
                {
                    // Make sure that the control is enabled
                    if (!SafeNativeMethods.IsWindowEnabled(_hwnd))
                    {
                        throw new ElementNotEnabledException();
                    }

                    ISelectionItemProvider selProvider = (ISelectionItemProvider) this;

                    // check if item already selected
                    if (!selProvider.IsSelected)
                    {
                        return;
                    }

                    if (!WindowsCalendar.IsMultiSelect (_hwnd))
                    {
                        throw new InvalidOperationException(SR.Get(SRID.DoesNotSupportMultipleSelection));
                    }

                    DateTime [] dateSelection =
                        WindowsCalendar.CreateDateTimeFromSystemTime (
                            WindowsCalendar.GetRawValue (
                                _hwnd, WindowsCalendar.CalendarData.Selection));

                    DateTime itemDateTime = GetDateTime();

                    if (itemDateTime == DateTime.MinValue)
                    {
                        throw new InvalidOperationException(SR.Get(SRID.OperationCannotBePerformed));
                    }

                    TimeSpan ts = itemDateTime - dateSelection[0];

                    //If both date times in the dateSelection are same, then only one date is
                    //currently selected. We cannot remove the selection if its only the selected one.
                    if ((dateSelection[1] - dateSelection[0]).Days == 0)
                    {
                        throw new InvalidOperationException(SR.Get(SRID.OperationCannotBePerformed));
                    }
                    else if (ts.Days == 0)
                    {
                        dateSelection [0] = dateSelection [0].AddDays (1);
                    }
                    else if ((ts = (dateSelection[1] - itemDateTime)).Days == 0)
                    {
                        dateSelection [1] = dateSelection [1].AddDays (-1);
                    }
                    else
                    {
                        // Check if the day is next to any other day
                        throw new InvalidOperationException(SR.Get(SRID.OperationCannotBePerformed));
                    }

                    // do the selection
                    if (!WindowsCalendar.SetRawValue(_hwnd, dateSelection))
                    {
                        throw new InvalidOperationException(SR.Get(SRID.OperationCannotBePerformed));
                    }
                }

                // True if this element is part of the the selection
                bool ISelectionItemProvider.IsSelected
                {
                    get
                    {
                        DateTime [] dateSelection =
                            WindowsCalendar.CreateDateTimeFromSystemTime (
                                WindowsCalendar.GetRawValue (
                                    _hwnd, WindowsCalendar.CalendarData.Selection));

                        DateTime itemDateTime = GetDateTime();

                        // Cell without a date
                        if (itemDateTime == DateTime.MinValue)
                        {
                            return false;
                        }

                        DateTime dtCur = itemDateTime;

                        return dtCur >= dateSelection [0] && dtCur <= dateSelection [1];
                    }
                }

                // Returns the container for this element
                IRawElementProviderSimple ISelectionItemProvider.SelectionContainer
                {
                    get
                    {
                        System.Diagnostics.Debug.Assert (
                            _parent._parent is WindowsCalendar, "Invalid parent for a calendar item.");
                        return _winCalendar;
                    }
                }

                #endregion SelectionItem Pattern

                //------------------------------------------------------
                //
                //  Internal Methods
                //
                //------------------------------------------------------
                #region Internal Methods
                internal static int DayIndexFromRowColumn(int row, int column)
                {
                    return (row * CalendarDates.MAX_DAYS + column);
                }
                internal static int RowOfDayIndex(int dayIndex)
                {
                    return (dayIndex / CalendarDates.MAX_DAYS);
                }
                internal static int ColumnOfDayIndex(int dayIndex)
                {
                    return (dayIndex % CalendarDates.MAX_DAYS);
                }
                #endregion // Internal Methods

                //------------------------------------------------------
                //
                //  Protected Methods
                //
                //------------------------------------------------------

                #region Protected Methods

                #region Proxy Simple Helper

                // Used only for versions of MonthCal prior to V6.
                protected static NativeMethods.Win32Rect BoundingRect(
                    IntPtr hwnd, WindowsCalendar winCalendar, NativeMethods.Win32Rect rcMonth, int item)
                {
                    int cxCell = (rcMonth.right - rcMonth.left)
                                    / (winCalendar._fHasWeekNum ? CalendarDates.MAX_DAYS + 1 : CalendarDates.MAX_DAYS);
                    int cyCell = winCalendar._dyRow;
                    int iX = CalendarDay.ColumnOfDayIndex(item);
                    int iY = CalendarDay.RowOfDayIndex(item);

                    //int left = rcMonth.left + iX * cxCell + (winCalendar._fHasWeekNum ? cxCell : 0);
                    int left;
                    if (Misc.IsLayoutRTL(hwnd))
                    {
                        left = (rcMonth.right - cxCell) - (iX * cxCell) - (winCalendar._fHasWeekNum ? cxCell : 0);
                    }
                    else
                    {
                        left = rcMonth.left + iX * cxCell + (winCalendar._fHasWeekNum ? cxCell : 0);
                    }

                    int top = rcMonth.top + winCalendar._dyHeader + winCalendar._dyWeekDay + iY * cyCell;
                    return new NativeMethods.Win32Rect(left, top, left + cxCell, top + cyCell);
                }

                protected override NativeMethods.Win32Rect BoundingRect()
                {
                    if (_winCalendar._isVersion6)
                    {
                        int row = RowOfDayIndex(_item);
                        int column = ColumnOfDayIndex(_item);
                        NativeMethods.Win32Rect rect;
                        _winCalendar.GetCalendarPartRect(
                            _iCalendar, NativeMethods.MCGIP_CALENDARCELL, row, column, out rect);
                        return rect;
                    }
                    else
                    {
                        return BoundingRect(_hwnd, _winCalendar, _rcMonth, _item);
                    }
                }

                #endregion

                #region Value Helper

                internal static DateTime GetDateTimeFromCalendarItem(
                    IntPtr hwnd, WindowsCalendar winCalendar, int iCalendar, int item)
                {
                    if (winCalendar._isVersion6)
                    {
                        DateTime itemDateTime = DateTime.MinValue;
                        if (item >= 0)
                        {
                            int iRow = RowOfDayIndex(item);
                            int iCol = ColumnOfDayIndex(item);
                            NativeMethods.Win32Rect rect;
                            NativeMethods.SYSTEMTIME stEnd;
                            NativeMethods.SYSTEMTIME stStart;
                            if (winCalendar.GetCalendarGridInfo(
                                                NativeMethods.MCGIF_DATE,
                                                NativeMethods.MCGIP_CALENDARCELL,
                                                iCalendar,
                                                iRow,
                                                iCol,
                                                out rect,
                                                out stEnd,
                                                out stStart))
                            {
                                itemDateTime = WindowsCalendar.CreateDateTimeFromSystemTime1(stStart);
                            }
                        }
                        return itemDateTime;
                    }
                    else
                    {
                        int hitLocationFlags;
                        return GetDateTimeFromCalendarItem(hwnd, winCalendar, iCalendar, item, out hitLocationFlags);
                    }
                }

                internal static DateTime GetDateTimeFromCalendarItem(
                    IntPtr hwnd, WindowsCalendar winCalendar, int iCalendar, int item, out int hitLocationFlags)
                {
                    hitLocationFlags = 0;

                    // Use the Hittest mechanism to figure out the day of the month
                    NativeMethods.Win32Rect rcMonth;

                    winCalendar.CalcPositions (iCalendar, out rcMonth);

                    NativeMethods.Win32Rect rc = BoundingRect(hwnd, winCalendar, rcMonth, item);

                    NativeMethods.MCHITTESTINFO hitTestInfo = new NativeMethods.MCHITTESTINFO();

                    // Set the point of interest.
                    hitTestInfo.pt = new NativeMethods.Win32Point((rc.right + rc.left) / 2, (rc.bottom + rc.top) / 2);
                    hitTestInfo.cbSize = (uint) Marshal.SizeOf (hitTestInfo);

                    // Convert the coordinates for the point of interest from
                    // screen coordinates to window-relative coordinates.
                    if (!Misc.MapWindowPoints(IntPtr.Zero, hwnd, ref hitTestInfo.pt, 1))
                    {
                        return DateTime.MinValue;
                    }
                    unsafe
                    {
                        XSendMessage.XSend(hwnd, NativeMethods.MCM_HITTEST,
                                           IntPtr.Zero, new IntPtr(&hitTestInfo),
                                           Marshal.SizeOf(hitTestInfo.GetType()));
                    }
                    hitLocationFlags = (int)hitTestInfo.uHit;
                    return
                        hitLocationFlags == NativeMethods.MCHT_CALENDARDATE
                            || hitLocationFlags == (NativeMethods.MCHT_PREV | NativeMethods.MCHT_CALENDARDATE)
                            || hitLocationFlags == (NativeMethods.MCHT_NEXT | NativeMethods.MCHT_CALENDARDATE)
                        ? WindowsCalendar.CreateDateTimeFromSystemTime1(hitTestInfo.st) : DateTime.MinValue;
                }

                #endregion

                #endregion

                //------------------------------------------------------
                //
                //  Private Methods
                //
                //------------------------------------------------------

                #region Private Methods

                private DateTime GetDateTime()
                {
                    return GetDateTimeFromCalendarItem(_hwnd, _winCalendar, _iCalendar, _item);
                }

                // This AutomationId is a non-localizable string
                private string GetAutomationId()
                {
                    int hitLocationFlags;
                    DateTime itemDateTime =
                        GetDateTimeFromCalendarItem(_hwnd, _winCalendar, _iCalendar, _item, out hitLocationFlags);
                    if (itemDateTime == DateTime.MinValue)
                    {
                        return string.Empty;
                    }

                    if (Misc.IsBitSet(hitLocationFlags, NativeMethods.MCHT_PREV))
                    {
                        return "Calendar.Previous.Day " + itemDateTime.Day.ToString(CultureInfo.CurrentCulture); // This string is a non-localizable string
                    }
                    else if (Misc.IsBitSet(hitLocationFlags, NativeMethods.MCHT_NEXT))
                    {
                        return "Calendar.Next.Day " + itemDateTime.Day.ToString(CultureInfo.CurrentCulture); // This string is a non-localizable string
                    }
                    else
                    {
                        return "Calendar.Day " + itemDateTime.Day.ToString(CultureInfo.CurrentCulture); // This string is a non-localizable string
                    }
                }

                #endregion

            }

            #endregion CalendarDay 
        }

        #endregion CalendarDates 

        #endregion
    }
}

