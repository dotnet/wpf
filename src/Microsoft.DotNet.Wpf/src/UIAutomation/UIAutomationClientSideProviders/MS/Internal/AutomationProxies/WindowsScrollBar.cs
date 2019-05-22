// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// Description: ScrollBar Proxy

using System;
using System.Collections;
using System.Text;
using System.Windows.Automation;
using System.Windows.Automation.Provider;
using System.Runtime.InteropServices;
using System.ComponentModel;
using System.Windows;
using MS.Win32;

namespace MS.Internal.AutomationProxies
{
    // Win32 Proxy Scrollbar implementation.
    // This code works for vertical and horizontal scroll bars
    // both as part of the none client area or as a stand alone
    // scroll bar control.
    class WindowsScrollBar: ProxyHwnd, IRangeValueProvider
    {
        // ------------------------------------------------------
        //
        // Constructors
        //
        // ------------------------------------------------------

        #region Constructors

        internal WindowsScrollBar (IntPtr hwnd, ProxyFragment parent, int item, int sbFlag)
            : base( hwnd, parent, item)
        {
            _sbFlag = sbFlag;

            // Never do any non client area cliping when dealing with the scroll bar and 
            // scroll bar bits
            _fNonClientAreaElement = true;

            // Control Type
            _cControlType = ControlType.ScrollBar;

            // Only Focusable if it is a stand alone scroll bar
            _fIsKeyboardFocusable = IsStandAlone();

            if (!IsStandAlone())
            {
                _sAutomationId = sbFlag == NativeMethods.SB_VERT ? "Vertical ScrollBar" : "Horizontal ScrollBar"; // This string is a non-localizable string
            }

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

            return new WindowsScrollBar(hwnd, null, idChild, NativeMethods.SB_CTL);
        }

        // Static create method called by the event tracker system.
        // WinEvents are raised only when a notification has been set for a
        // specific item. Create the item first and check for details afterward.
        internal static void RaiseEvents (IntPtr hwnd, int eventId, object idProp, int idObject, int idChild)
        {
            WindowsScrollBar wtv = new WindowsScrollBar (hwnd, null, -1, NativeMethods.SB_CTL);

            if (idChild == 0 && eventId == NativeMethods.EventObjectStateChange && idProp == ValuePattern.IsReadOnlyProperty)
            {
                // UIA works differently than MSAA. Events are set for elements rather than hwnd
                // Scroll bar are processed the same way whatever they are part of the non client area
                // or stand alone hwnd. OBJID_HSCROLL and OBJID_VSCROLL are mapped to OBJID_WINDOW
                // so they behave the same for NC and hwnd SB. 
                // Parameters are setup so that the dispatch will send the proper notification and 
                // recursively will send notifications to all of the children
                if (idObject == NativeMethods.OBJID_HSCROLL || idObject == NativeMethods.OBJID_VSCROLL)
                {
                    idObject = NativeMethods.OBJID_WINDOW;
                }
            }

            if (idChild == 0)
            {
                wtv.DispatchEvents (eventId, idProp, idObject, idChild);
            }
            else
            {
                // raise events for the children 
                ProxySimple scrollBarBit = WindowsScrollBarBits.CreateFromChildId(hwnd, wtv, idChild, NativeMethods.SB_CTL);
                if (scrollBarBit != null)
                {
                    scrollBarBit.DispatchEvents(eventId, idProp, idObject, idChild);
                }
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
            return (iid == RangeValuePattern.Pattern && WindowScroll.Scrollable (_hwnd, _sbFlag) && HasValuePattern (_hwnd, _sbFlag)) ? this : null;
        }

        // Gets the bounding rectangle for this element
        internal override Rect BoundingRectangle
        {
            get
            {
                //
                // If its scrollbar then get the default Win32Rect
                //
                if (_sbFlag == NativeMethods.SB_CTL)
                {
                    return base.BoundingRectangle;
                }

                NativeMethods.ScrollBarInfo sbi = new NativeMethods.ScrollBarInfo ();
                sbi.cbSize = Marshal.SizeOf (sbi.GetType ());

                int idObject = _sbFlag == NativeMethods.SB_VERT ? NativeMethods.OBJID_VSCROLL : _sbFlag == NativeMethods.SB_HORZ ? NativeMethods.OBJID_HSCROLL : NativeMethods.OBJID_CLIENT;

                if (!Misc.GetScrollBarInfo(_hwnd, idObject, ref sbi))
                {
                    return Rect.Empty;
                }

                // When the scroll bar is for a listbox within a combo and it is hidden, then 
                // GetScrollBarInfo returns true but the rectangle is boggus!
                // 32 bits * 32 bits > 64 values
                long area = (sbi.rcScrollBar.right - sbi.rcScrollBar.left) * (sbi.rcScrollBar.bottom - sbi.rcScrollBar.top);
                if (area <= 0 || area > 1000 * 1000)
                {
                    // Ridiculous value assume error
                    return Rect.Empty;
                }

                if (!IsStandAlone())
                {
                    //
                    // Builds prior to Vista 5359 failed to correctly account for RTL scrollbar layouts.
                    //
                    if ((Environment.OSVersion.Version.Major < 6) && (Misc.IsLayoutRTL(_parent._hwnd)))
                    {
                        // Right to left mirroring style
                        Rect rcParent = _parent.BoundingRectangle;
                        int width = sbi.rcScrollBar.right - sbi.rcScrollBar.left;

                        if (_sbFlag == NativeMethods.SB_VERT)
                        {
                            int offset = (int)rcParent.Right - sbi.rcScrollBar.right;
                            sbi.rcScrollBar.left = (int)rcParent.Left + offset;
                            sbi.rcScrollBar.right = sbi.rcScrollBar.left + width;
                        }
                        else
                        {
                            int offset = sbi.rcScrollBar.left - (int)rcParent.Left;
                            sbi.rcScrollBar.right = (int)rcParent.Right - offset;
                            sbi.rcScrollBar.left = sbi.rcScrollBar.right - width;
                        }
                    }
                }

                // Don't need to normalize, OSVer conditional block converts to absolute coordinates.
                return sbi.rcScrollBar.ToRect(false);
            }
        }

        // Process all the Logical and Raw Element Properties
        internal override object GetElementProperty (AutomationProperty idProp)
        {
            if (idProp == AutomationElement.IsControlElementProperty)
            {
                return SafeNativeMethods.IsWindowVisible(_hwnd);
            }
            else if (idProp == AutomationElement.IsKeyboardFocusableProperty)
            {
                // When a scroll bar is embedded into a control, treat it as a piece of the control
                // and not as a control, i.e. ignore the WS_TABSTOP style.
                if (_parent != null)
                {
                    // If it's visible and enabled it might be focusable 
                    if (SafeNativeMethods.IsWindowVisible(_hwnd) &&
                        (bool)GetElementProperty(AutomationElement.IsEnabledProperty))
                    {
                        return _fIsKeyboardFocusable;
                    }
                    else
                    {
                        return false;
                    }
                }
            }
            else if (idProp == AutomationElement.IsEnabledProperty)
            {
                // When the scroll bar is not stand-alone, _hwnd
                // is the handle of the containing window.
                return IsEnabled() && Misc.IsEnabled(_hwnd);
            }
            else if (idProp == AutomationElement.OrientationProperty)
            {
                return IsScrollBarVertical(_hwnd, _sbFlag) ? OrientationType.Vertical : OrientationType.Horizontal;
            }
            else if (idProp == AutomationElement.AutomationIdProperty)
            {
                // If this scroll bar is a sub-component of a control, do not let the base (ProxyHwnd) process the
                // AutomationIdProperty, since it may pick up the AutomationID for the whole control.
                if (!IsStandAlone())
                {
                    return _sAutomationId;
                }
            }

            return base.GetElementProperty (idProp);
        }

        // Returns the Run Time Id.
        // The default behavior in ProxySimple is to check if the element is a ProxyHwnd.
        // In that case it return a run time it the form [1,hwnd]. If a scroll bar is part
        // of the non client area, the WindowsScrollBar is a ProxyHwnd but it is not an hwnd.
        // Overide the default implementation in ProxySimple removing the check for ProxyHwnd
        internal override int[] GetRuntimeId()
        {
            if (_fSubTree)
            {
                // add the id for this level at the end of the chain
                return new int[] { AutomationInteropProvider.AppendRuntimeId, _item };
            }
            else
            {
                // UIA handles runtimeIDs for the HWND part of the object for us
                return null;
            }
        }

        //Gets the localized name
        internal override string LocalizedName
        {
            get
            {
                if (_item == -1)
                {
                    return null;
                }

                return SR.Get(
                    IsScrollBarVertical(_hwnd, _sbFlag)
                        ? SRID.LocalizedNameWindowsVerticalScrollBar
                        : SRID.LocalizedNameWindowsHorizontalScrollBar);
            }
        }

        #endregion

        #region ProxyFragment Interface

        // Returns the next sibling element in the raw hierarchy.
        // Peripheral controls have always negative values.
        // Returns null if no next child.
        internal override ProxySimple GetNextSibling (ProxySimple child)
        {
            ScrollBarItem item = (ScrollBarItem) child._item;

            if (item != ScrollBarItem.DownArrow)
            {
                // skip the Large increment/decrement if there is no thumb
                if (item == ScrollBarItem.UpArrow && !IsScrollBarWithThumb (_hwnd, _sbFlag))
                {
                    item = ScrollBarItem.DownArrow - 1;
                }
                return CreateScrollBitsItem ((ScrollBarItem) ((int) item + 1));
            }

            return null;
        }

        // Returns the previous sibling element in the raw hierarchy.
        // Peripheral controls have always negative values.
        // Returns null is no previous.
        internal override ProxySimple GetPreviousSibling (ProxySimple child)
        {
            ScrollBarItem item = (ScrollBarItem) child._item;

            if (item != ScrollBarItem.UpArrow)
            {
                // skip the Large increment/decrement if there is no thumb
                if (item == ScrollBarItem.DownArrow && !IsScrollBarWithThumb (_hwnd, _sbFlag))
                {
                    item = ScrollBarItem.UpArrow + 1;
                }
                return CreateScrollBitsItem ((ScrollBarItem) ((int) item - 1));
            }

            return null;
        }

        // Returns the first child element in the raw hierarchy.
        internal override ProxySimple GetFirstChild ()
        {
            return CreateScrollBitsItem (ScrollBarItem.UpArrow);
        }

        // Returns the last child element in the raw hierarchy.
        internal override ProxySimple GetLastChild ()
        {
            return CreateScrollBitsItem (ScrollBarItem.DownArrow);
        }

        // Returns a Proxy element corresponding to the specified screen coordinates.
        internal override ProxySimple ElementProviderFromPoint (int x, int y)
        {
            for (ScrollBarItem item = ScrollBarItem.UpArrow; (int) item <= (int) ScrollBarItem.DownArrow; item = (ScrollBarItem) ((int) item + 1))
            {
                NativeMethods.Win32Rect rc = new NativeMethods.Win32Rect(WindowsScrollBarBits.GetBoundingRectangle(_hwnd, this, item, _sbFlag));

                if (Misc.PtInRect(ref rc, x, y))
                {
                    return new WindowsScrollBarBits (_hwnd, this, (int) item, _sbFlag);
                }
            }

            return this;
        }

        #endregion

        #region RangeValue Pattern

        // Change the position of the scroll bar
        void IRangeValueProvider.SetValue (double val)
        {
            SetScrollValue((int)val);
        }

        // Request to get the value that this UI element is representing in a native format
        double IRangeValueProvider.Value
        {
            get
            {
                return (double)GetScrollValue (ScrollBarInfo.CurrentPosition);
            }
        }

        bool IRangeValueProvider.IsReadOnly
        {
            get
            {
                return !Misc.IsEnabled(_hwnd) || !HasValuePattern(_hwnd, _sbFlag);
            }
        }

        double IRangeValueProvider.Maximum
        {
            get
            {
                return (double)GetScrollValue (ScrollBarInfo.MaximumPosition);
            }
        }

        double IRangeValueProvider.Minimum
        {
            get
            {
                return (double)GetScrollValue (ScrollBarInfo.MinimumPosition);
            }
        }

        double IRangeValueProvider.SmallChange
        {
            get
            {
                return (double)GetScrollValue(ScrollBarInfo.SmallChange);
            }
        }

        double IRangeValueProvider.LargeChange
        {
            get
            {
                return (double)GetScrollValue(ScrollBarInfo.LargeChange);
            }
        }

        #endregion RangeValue Pattern

        // ------------------------------------------------------
        //
        // Internal Methods
        //
        // ------------------------------------------------------

        #region Internal Methods

        static internal bool HasVerticalScrollBar (IntPtr hwnd)
        {
            return Misc.IsBitSet(Misc.GetWindowStyle(hwnd), NativeMethods.WS_VSCROLL);
        }

        static internal bool HasHorizontalScrollBar (IntPtr hwnd)
        {
            return Misc.IsBitSet(Misc.GetWindowStyle(hwnd), NativeMethods.WS_HSCROLL);
        }

        internal static bool IsScrollBarVertical(IntPtr hwnd, int sbFlag)
        {
            if (sbFlag == NativeMethods.SB_CTL)
                return Misc.IsBitSet(Misc.GetWindowStyle(hwnd), NativeMethods.SBS_VERT);
            else
                return sbFlag == NativeMethods.SB_VERT;
        }

        // Check if a scroll bar is in a disabled state
        internal static bool IsScrollBarWithThumb (IntPtr hwnd, int sbFlag)
        {
            NativeMethods.ScrollInfo si = new NativeMethods.ScrollInfo ();
            si.cbSize = Marshal.SizeOf (si.GetType ());
            si.fMask = NativeMethods.SIF_RANGE | NativeMethods.SIF_PAGE;

            if (!Misc.GetScrollInfo(hwnd, sbFlag, ref si))
            {
                return false;
            }

            // Check for the min / max value
            if (si.nMax != si.nMin && si.nPage != si.nMax - si.nMin + 1)
            {
                // The scroll bar is enabled, check if we have a thumb
                int idObject = sbFlag == NativeMethods.SB_VERT ? NativeMethods.OBJID_VSCROLL : sbFlag == NativeMethods.SB_HORZ ? NativeMethods.OBJID_HSCROLL : NativeMethods.OBJID_CLIENT;
                NativeMethods.ScrollBarInfo sbi = new NativeMethods.ScrollBarInfo ();
                sbi.cbSize = Marshal.SizeOf (sbi.GetType ());

                // check that the 2 buttons can hold in the scroll bar
                if (Misc.GetScrollBarInfo(hwnd, idObject, ref sbi))
                {
                    // When the scroll bar is for a listbox within a combo and it is hidden, then 
                    // GetScrollBarInfo returns true but the rectangle is boggus!
                    // 32 bits * 32 bits > 64 values
                    long area = (sbi.rcScrollBar.right - sbi.rcScrollBar.left) * (sbi.rcScrollBar.bottom - sbi.rcScrollBar.top);
                    if (area > 0 && area < 1000 * 1000)
                    {
                        NativeMethods.SIZE sizeArrow;

                        using (ThemePart themePart = new ThemePart(hwnd, "SCROLLBAR"))
                        {
                            sizeArrow = themePart.Size((int)ThemePart.SCROLLBARPARTS.SBP_ARROWBTN, 0);
                        }

                        bool fThumbVisible = false;
                        if (IsScrollBarVertical(hwnd, sbFlag))
                        {
                            fThumbVisible = (sbi.rcScrollBar.bottom - sbi.rcScrollBar.top >= 5 * sizeArrow.cy / 2);
                        }
                        else
                        {
                            fThumbVisible = (sbi.rcScrollBar.right - sbi.rcScrollBar.left >= 5 * sizeArrow.cx / 2);
                        }
                        return fThumbVisible;
                    }
                }
            }
            return false;
        }

        #endregion

        //------------------------------------------------------
        //
        //  Internal Fields
        //
        //------------------------------------------------------

        #region Internal Fields

        // ------------------------------------------------------
        //
        // Internal Types Declaration
        //
        // ------------------------------------------------------
        internal enum ScrollBarItem
        {
            UpArrow = 0,
            LargeDecrement = 1,
            Thumb = 2,
            LargeIncrement = 3,
            DownArrow = 4
        }

        #endregion

        // ------------------------------------------------------
        //
        // Private Methods
        //
        // ------------------------------------------------------

        #region Private Methods

        // Create a new proxy for one of the scroll bit items
        private ProxySimple CreateScrollBitsItem (ScrollBarItem index)
        {
            // For Scrollbars as standalone controls, make sure that the buttons are not invisible. (office scroll bars)
            // Checking is done from the return value of SetScrolBarInfo. 
            NativeMethods.ScrollBarInfo sbi = new NativeMethods.ScrollBarInfo ();
            sbi.cbSize = Marshal.SizeOf (sbi.GetType ());

            if (_sbFlag != NativeMethods.SB_CTL || Misc.GetScrollBarInfo(_hwnd, NativeMethods.OBJID_CLIENT, ref sbi))
            {
                return new WindowsScrollBarBits (_hwnd, this, (int) index, _sbFlag);
            }
            return null;
        }

        private int GetScrollMaxValue(NativeMethods.ScrollInfo si)
        {
            // NOTE:
            // Proportional scrollbars have a few key values: min, max, page, and current.
            // 
            // Min and max represent the endpoints of the scrollbar; page is the side of the thumb,
            // and current is the position of the leading edge of the thumb (top for a vert scrollbar).
            // 
            // Because of this arrangment, current can't be any value between min and max, it's actually
            // confied to min...(max-page)+1. That +1 is a quirk of the scrollbar's internal logic.
            // 
            // For example, in an edit in notepad, these might be:
            // min = 0
            // max = 33 (~total lines in file)
            // current = { any value from 0 .. 12 inclusive }
            // page = 22
            // 
            // Most controls just let the scrollbar do all the proportional logic: they pass the incoming
            // values from the scroll messages (eg as a reuslt of dragging with a mouse or from UIA's SetValue)
            // straight down to the scrollbar APIs.
            // 
            // RichEdit is different: it does its own extra 'validation', and limits the current value to
            // (max-page) - without that +1.
            //
            // The end result of this is that it's not possible using UIA to scroll a richedit to the max value:
            // the richedit is exposing (implicitly through the scrollbar APIs) a max value one higher than the
            // max value that it will actually allow.
            string classname = Misc.GetClassName(_hwnd);
            if (classname.ToLower(System.Globalization.CultureInfo.InvariantCulture).Contains("richedit"))
            {
                return si.nMax - si.nPage;
            }
            else
            {
                return (si.nMax - si.nPage) + (si.nPage > 0 ? 1 : 0);
            }
        }

        private int GetScrollValue (ScrollBarInfo info)
        {
            NativeMethods.ScrollInfo si = new NativeMethods.ScrollInfo ();
            si.fMask = NativeMethods.SIF_ALL;
            si.cbSize = Marshal.SizeOf (si.GetType ());

            if (!Misc.GetScrollInfo(_hwnd, _sbFlag, ref si))
            {
                return 0;
            }

            switch (info)
            {
                case ScrollBarInfo.CurrentPosition:
                    //
                    // Builds prior to Vista 5359 failed to correctly account for RTL scrollbar layouts.
                    //
                    if ((Environment.OSVersion.Version.Major < 6) && (_sbFlag == NativeMethods.SB_HORZ) && (Misc.IsControlRTL(_parent._hwnd)))
                    {
                        return GetScrollMaxValue(si) - si.nPos;
                    }
                    return si.nPos;

                case ScrollBarInfo.MaximumPosition:
                    return GetScrollMaxValue(si);

                case ScrollBarInfo.MinimumPosition:
                    return si.nMin;

                case ScrollBarInfo.PageSize:
                    return si.nPage;

                case ScrollBarInfo.TrackPosition:
                    return si.nTrackPos;

                case ScrollBarInfo.LargeChange:
                    return si.nPage;

                case ScrollBarInfo.SmallChange:
                    return 1;
            }

            return 0;
        }

        private void SetScrollValue (int val)
        {
            // Check if the window is disabled
            if (!SafeNativeMethods.IsWindowEnabled (_hwnd))
            {
                throw new ElementNotEnabledException();
            }

            NativeMethods.ScrollInfo si = new NativeMethods.ScrollInfo ();
            si.fMask = NativeMethods.SIF_ALL;
            si.cbSize = Marshal.SizeOf (si.GetType ());

            if (!Misc.GetScrollInfo(_hwnd, _sbFlag, ref si))
            {
                throw new InvalidOperationException(SR.Get(SRID.OperationCannotBePerformed));
            }

            // No move, exit
            if (val == si.nPos)
            {
                return;
            }

            // NOTE:
            // Proportional scrollbars have a few key values: min, max, page, and current.
            // 
            // Min and max represent the endpoints of the scrollbar; page is the side of the thumb,
            // and current is the position of the leading edge of the thumb (top for a vert scrollbar).
            // 
            // Because of this arrangment, current can't be any value between min and max, it's actually
            // confied to min...(max-page)+1. That +1 is a quirk of the scrollbar's internal logic.
            // 
            // For example, in an edit in notepad, these might be:
            // min = 0
            // max = 33 (~total lines in file)
            // current = { any value from 0 .. 12 inclusive }
            // page = 22
            // 
            // Most controls just let the scrollbar do all the proportional logic: they pass the incoming
            // values from the scroll messages (eg as a reuslt of dragging with a mouse or from UIA's SetValue)
            // straight down to the scrollbar APIs.
            // 
            // RichEdit is different: it does its own extra 'validation', and limits the current value to
            // (max-page) - without that +1.
            //
            // The end result of this is that it's not possible using UIA to scroll a richedit to the max value:
            // the richedit is exposing (implicitly through the scrollbar APIs) a max value one higher than the
            // max value that it will actually allow.

            int max;
            string classname = Misc.GetClassName(_hwnd);
            if (classname.ToLower(System.Globalization.CultureInfo.InvariantCulture).Contains("richedit"))
            {
                max = si.nMax - si.nPage;
            }
            else
            {
                max = (si.nMax - si.nPage) + (si.nPage > 0 ? 1 : 0);
            }

            // Explicit comparisions are made to the MaxPercentage and MinPercentage,
            // so that we dont miss out the fractions
            if (val > max )
            {
                throw new ArgumentOutOfRangeException("value", val, SR.Get(SRID.RangeValueMax));
            }
            else if (val < si.nMin)
            {
                throw new ArgumentOutOfRangeException("value", val, SR.Get(SRID.RangeValueMin));
            }

            if (_sbFlag == NativeMethods.SB_CTL)
            {
                Misc.SetScrollPos(_hwnd, _sbFlag, val, true);
            }
            else
            {
                // Determine the msg from the style.
                int msg =
                    IsScrollBarVertical(_hwnd, _sbFlag) ? NativeMethods.WM_VSCROLL : NativeMethods.WM_HSCROLL;

                // An application is generally programmed to process either the
                // SB_THUMBTRACK or SB_THUMBPOSITION request code.
                int wParam = NativeMethods.Util.MAKELONG ((short) NativeMethods.SB_THUMBPOSITION, (short) val);
                Misc.ProxySendMessage(_hwnd, msg, (IntPtr)wParam, IntPtr.Zero);
                wParam = NativeMethods.Util.MAKELONG ((short) NativeMethods.SB_THUMBTRACK, (short) val);
                Misc.ProxySendMessage(_hwnd, msg, (IntPtr)wParam, IntPtr.Zero);
            }
        }

        // Check if a scroll bar implements the Value Pattern
        private static bool HasValuePattern (IntPtr hwnd, int sbFlag)
        {
            // The scroll bar is enabled, check if we have a thumb
            int idObject = sbFlag == NativeMethods.SB_VERT ? NativeMethods.OBJID_VSCROLL : sbFlag == NativeMethods.SB_HORZ ? NativeMethods.OBJID_HSCROLL : NativeMethods.OBJID_CLIENT;
            NativeMethods.ScrollBarInfo sbi = new NativeMethods.ScrollBarInfo ();
            sbi.cbSize = Marshal.SizeOf (sbi.GetType ());

            // Scroll bars implements the Value pattern if
            // 1) they are owner drawn (in which case GetScrollBarInfo fails)
            // 2) they have a thumb (not completely squished)
            if (Misc.GetScrollBarInfo(hwnd, idObject, ref sbi))
            {
                return IsScrollBarWithThumb (hwnd, sbFlag);
            }
            return true;
        }

        private bool IsEnabled()
        {
            NativeMethods.ScrollBarInfo sbi = new NativeMethods.ScrollBarInfo();
            sbi.cbSize = Marshal.SizeOf(sbi.GetType());

            int idObject = NativeMethods.OBJID_CLIENT;
            if (_sbFlag == NativeMethods.SB_VERT)
            {
                idObject = NativeMethods.OBJID_VSCROLL;
            }
            else if (_sbFlag == NativeMethods.SB_HORZ)
            {
                idObject = NativeMethods.OBJID_HSCROLL;
            }

            if (!Misc.GetScrollBarInfo(_hwnd, idObject, ref sbi))
            {
                return false;
            }

            return !Misc.IsBitSet(sbi.scrollBarInfo, NativeMethods.STATE_SYSTEM_UNAVAILABLE);
        }

        private bool IsStandAlone()
        {
            return _parent == null;
        }

        #endregion

        // ------------------------------------------------------
        //
        // Protected Fields
        //
        // ------------------------------------------------------

        #region Protected Fields

        // Cached value for the scroll bar style
        protected int _sbFlag = NativeMethods.SB_CTL;

        #endregion

        // ------------------------------------------------------
        //
        // Private Fields
        //
        // ------------------------------------------------------

        #region Private Fields

        private enum ScrollBarInfo
        {
            CurrentPosition,
            MaximumPosition,
            MinimumPosition,
            PageSize,
            TrackPosition,
            LargeChange,
            SmallChange
        };

        #endregion
    }
}
