// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// Description: ScrollBarBits Proxy
//
//              Proxy for the up, down, large increment,
//              large decrement and thumb piece of a scrollbar.
//

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
    // Proxy for the up, down, large increment, large decrement and thumb piece of a scrollbar
    class WindowsScrollBarBits: ProxySimple, IInvokeProvider
    {
        // ------------------------------------------------------
        //
        // Constructors
        //
        // ------------------------------------------------------

        #region Constructors

        // Contructor for the pieces that a acroll bar is made of.
        // Up Arrow, Down Arrow, Large Increment, Large Decrement and thmub.
        // param "hwnd", Windows handle
        // param "parent", Proxy Parent. Null if it is a root fragment
        // param "item", Proxy ID
        // param "sbFlag", CTL or (Vertical or Horizon non clent scroll bar)
        internal WindowsScrollBarBits (IntPtr hwnd, ProxyFragment parent, int item, int sbFlag)
            : base( hwnd, parent, item )
        {
            _item = (int) item;
            _sbFlag = sbFlag;
            _fIsContent = false;

            // Never do any non client area cliping when dealing with the scroll bar and 
            // scroll bar bits
            _fNonClientAreaElement = true;

            switch ((WindowsScrollBar.ScrollBarItem)_item)
            {
                case WindowsScrollBar.ScrollBarItem.UpArrow:
                    _cControlType = ControlType.Button;
                    _sAutomationId = "SmallDecrement"; // This string is a non-localizable string
                    break;

                case WindowsScrollBar.ScrollBarItem.LargeDecrement:
                    _cControlType = ControlType.Button;
                    _sAutomationId = "LargeDecrement"; // This string is a non-localizable string
                    break;

                case WindowsScrollBar.ScrollBarItem.LargeIncrement:
                    _cControlType = ControlType.Button;
                    _sAutomationId = "LargeIncrement"; // This string is a non-localizable string
                    break;

                case WindowsScrollBar.ScrollBarItem.DownArrow:
                    _cControlType = ControlType.Button;
                    _sAutomationId = "SmallIncrement"; // This string is a non-localizable string
                    break;

                case WindowsScrollBar.ScrollBarItem.Thumb:
                    _cControlType = ControlType.Thumb;
                    _sAutomationId = "Thumb"; // This string is a non-localizable string
                    _fIsKeyboardFocusable = parent._fIsKeyboardFocusable;
                    break;
            }
        }


        // Static Create method called to create this proxy from a child id.
        // the item needs to be adjusted because it is zero based and child is 1 based.
        internal static ProxySimple CreateFromChildId(IntPtr hwnd, ProxyFragment parent, int idChild, int sbFlag)
        {
            return new WindowsScrollBarBits(hwnd, parent, idChild -1, sbFlag);
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
            if (iid == InvokePattern.Pattern && (WindowsScrollBar.ScrollBarItem) _item != WindowsScrollBar.ScrollBarItem.Thumb)
            {
                return this;
            }

            return null;
        }
        
        // Process all the Logical and Raw Element Properties
        internal override object GetElementProperty (AutomationProperty idProp)
        {
            if (idProp == AutomationElement.IsControlElementProperty)
            {
                return SafeNativeMethods.IsWindowVisible(_hwnd);
            }
            else if (idProp == AutomationElement.IsEnabledProperty)
            {
                return _parent.GetElementProperty(idProp);
            }

            return base.GetElementProperty (idProp);
        }

        // Gets the bounding rectangle for this element
        internal override Rect BoundingRectangle
        {
            get
            {
                return GetBoundingRectangle (_hwnd, _parent, (WindowsScrollBar.ScrollBarItem) _item, _sbFlag);
            }
        }

        //Gets the localized name
        internal override string LocalizedName
        {
            get
            {
                return SR.Get(_asNames[_item]);
            }
        }

        #endregion ProxySimple Interface

        #region Invoke Pattern

        // Same effect as a click on the arrows or the large increment.
        void IInvokeProvider.Invoke ()
        {
            // Make sure that the control is enabled
            if (!SafeNativeMethods.IsWindowEnabled(_hwnd))
            {
                throw new ElementNotEnabledException();
            }

            if ((WindowsScrollBar.ScrollBarItem) _item == WindowsScrollBar.ScrollBarItem.Thumb)
            {
                return;
            }

            ScrollAmount amount = ScrollAmount.SmallDecrement;

            switch ((WindowsScrollBar.ScrollBarItem) _item)
            {
                case WindowsScrollBar.ScrollBarItem.UpArrow :
                    amount = ScrollAmount.SmallDecrement;
                    break;

                case WindowsScrollBar.ScrollBarItem.LargeDecrement :
                    amount = ScrollAmount.LargeDecrement;
                    break;

                case WindowsScrollBar.ScrollBarItem.LargeIncrement :
                    amount = ScrollAmount.LargeIncrement;
                    break;

                case WindowsScrollBar.ScrollBarItem.DownArrow :
                    amount = ScrollAmount.SmallIncrement;
                    break;
            }
            if (WindowsScrollBar.IsScrollBarVertical(_hwnd, _sbFlag))
            {
                Scroll(amount, NativeMethods.SBS_VERT);
            }
            else
            {
                Scroll(amount, NativeMethods.SBS_HORZ);
            }
        }

        #endregion Invoke Pattern

        // ------------------------------------------------------
        //
        // Internal Methods
        //
        // ------------------------------------------------------

        #region Internal Methods

        // Static implementation for the bounding rectangle. This is used by
        // ElementProviderFromPoint to avoid to have to create for a simple
        // boundary check
        // param "item", ID for the scrollbar bit
        // param "sbFlag", SBS_ WindowLong equivallent flag
        static internal Rect GetBoundingRectangle(IntPtr hwnd, ProxyFragment parent, WindowsScrollBar.ScrollBarItem item, int sbFlag)
        {
            NativeMethods.ScrollInfo si = new NativeMethods.ScrollInfo ();
            si.cbSize = Marshal.SizeOf (si.GetType ());
            si.fMask = NativeMethods.SIF_RANGE;

            // If the scroll bar is disabled, we cannot have a thumb and large Increment/Decrement)
            bool fDisableScrollBar = !WindowsScrollBar.IsScrollBarWithThumb (hwnd, sbFlag);
            if (fDisableScrollBar && (item == WindowsScrollBar.ScrollBarItem.LargeDecrement || item == WindowsScrollBar.ScrollBarItem.Thumb || item == WindowsScrollBar.ScrollBarItem.LargeDecrement))
            {
                return Rect.Empty;
            }

            // If fails assume that the hwnd is invalid
            if (!Misc.GetScrollInfo(hwnd, sbFlag, ref si))
            {
                return Rect.Empty;
            }

            int idObject = sbFlag == NativeMethods.SB_VERT ? NativeMethods.OBJID_VSCROLL : sbFlag == NativeMethods.SB_HORZ ? NativeMethods.OBJID_HSCROLL : NativeMethods.OBJID_CLIENT;
            NativeMethods.ScrollBarInfo sbi = new NativeMethods.ScrollBarInfo ();
            sbi.cbSize = Marshal.SizeOf (sbi.GetType ());

            if (!Misc.GetScrollBarInfo(hwnd, idObject, ref sbi))
            {
                return Rect.Empty;
            }

            if (parent != null && parent._parent != null)
            {
                //
                // Builds prior to Vista 5359 failed to correctly account for RTL scrollbar layouts.
                //
                if ((Environment.OSVersion.Version.Major < 6) && (Misc.IsLayoutRTL(parent._parent._hwnd)))
                {
                    // Right to left mirroring style
                    Rect rcParent = parent._parent.BoundingRectangle;
                    int width = sbi.rcScrollBar.right - sbi.rcScrollBar.left;

                    if (sbFlag == NativeMethods.SB_VERT)
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

            // When the scroll bar is for a listbox within a combo and it is hidden, then 
            // GetScrollBarInfo returns true but the rectangle is boggus!
            // 32 bits * 32 bits > 64 values
            //
            // Note that this test must come after the rectangle has been normalized for RTL or it will fail 
            //
            long area = (sbi.rcScrollBar.right - sbi.rcScrollBar.left) * (sbi.rcScrollBar.bottom - sbi.rcScrollBar.top);
            if (area <= 0 || area > 1000 * 1000)
            {
                // Ridiculous value assume error
                return Rect.Empty;
            }

            if(WindowsScrollBar.IsScrollBarVertical(hwnd, sbFlag))
            {
                return GetVerticalScrollbarBitBoundingRectangle(hwnd, item, sbi);
            }
            else
            {
                return GetHorizontalScrollbarBitBoundingRectangle(hwnd, item, sbi);
            }
        }

        #endregion Invoke Pattern

        // ------------------------------------------------------
        //
        // Internal Methods
        //
        // ------------------------------------------------------

        #region Internal Methods

        // Static implementation for the bounding rectangle. This is used by
        // ElementProviderFromPoint to avoid to have to create for a simple
        // boundary check
        // param "item", ID for the scrollbar bit
        // param "sbFlag", SBS_ WindowLong equivallent flag
        static internal Rect GetVerticalScrollbarBitBoundingRectangle(IntPtr hwnd, WindowsScrollBar.ScrollBarItem item, NativeMethods.ScrollBarInfo sbi)
        {
            NativeMethods.Win32Rect rc = new NativeMethods.Win32Rect(sbi.rcScrollBar.left, sbi.xyThumbTop, sbi.rcScrollBar.right, sbi.xyThumbBottom);
            if (!Misc.MapWindowPoints(hwnd, IntPtr.Zero, ref rc, 2))
            {
                return Rect.Empty;
            }

            // Vertical Scrollbar
            // Since the scrollbar position is already mapped, restore them back
            rc.left = sbi.rcScrollBar.left;
            rc.right = sbi.rcScrollBar.right;

            NativeMethods.SIZE sizeArrow;

            using (ThemePart themePart = new ThemePart(hwnd, "SCROLLBAR"))
            {
                sizeArrow = themePart.Size((int)ThemePart.SCROLLBARPARTS.SBP_ARROWBTN, 0);
            }

            // check that the 2 buttons can hold in the scroll bar
            bool fThumbVisible = sbi.rcScrollBar.bottom - sbi.rcScrollBar.top >= 5 * sizeArrow.cy / 2;
            if (sbi.rcScrollBar.bottom - sbi.rcScrollBar.top < 2 * sizeArrow.cy)
            {
                // the scroll bar is tiny, need to shrink the button
                sizeArrow.cy = (sbi.rcScrollBar.bottom - sbi.rcScrollBar.top) / 2;
            }

            switch (item)
            {
                case WindowsScrollBar.ScrollBarItem.UpArrow :
                    rc.top = sbi.rcScrollBar.top;
                    rc.bottom = sbi.rcScrollBar.top + sizeArrow.cy;
                    break;

                case WindowsScrollBar.ScrollBarItem.LargeIncrement :
                    if (fThumbVisible)
                    {
                        rc.top = rc.bottom;
                        rc.bottom = sbi.rcScrollBar.bottom - sizeArrow.cy;
                    }
                    else
                    {
                        rc.top = rc.bottom = sbi.rcScrollBar.top + sizeArrow.cy;
                    }
                    break;

                case WindowsScrollBar.ScrollBarItem.Thumb :
                    if (!fThumbVisible)
                    {
                        rc.top = rc.bottom = sbi.rcScrollBar.top + sizeArrow.cy;
                    }
                    break;

                case WindowsScrollBar.ScrollBarItem.LargeDecrement :
                    if (fThumbVisible)
                    {
                        rc.bottom = rc.top;
                        rc.top = sbi.rcScrollBar.top + sizeArrow.cy;
                    }
                    else
                    {
                        rc.top = rc.bottom = sbi.rcScrollBar.top + sizeArrow.cy;
                    }
                    break;

                case WindowsScrollBar.ScrollBarItem.DownArrow :
                    rc.top = sbi.rcScrollBar.bottom - sizeArrow.cy;
                    rc.bottom = sbi.rcScrollBar.bottom;
                    break;
            }

            // Don't need to normalize, OSVer conditional block converts to absolute coordinates.
            return rc.ToRect(false);
        }

        #endregion Invoke Pattern

        // ------------------------------------------------------
        //
        // Internal Methods
        //
        // ------------------------------------------------------

        #region Internal Methods

        // Static implementation for the bounding rectangle. This is used by
        // ElementProviderFromPoint to avoid to have to create for a simple
        // boundary check
        // param "item", ID for the scrollbar bit
        // param "sbFlag", SBS_ WindowLong equivallent flag
        static internal Rect GetHorizontalScrollbarBitBoundingRectangle(IntPtr hwnd, WindowsScrollBar.ScrollBarItem item, NativeMethods.ScrollBarInfo sbi)
        {
            // Horizontal Scrollbar
            NativeMethods.Win32Rect rc = new NativeMethods.Win32Rect(sbi.xyThumbTop, sbi.rcScrollBar.top, sbi.xyThumbBottom, sbi.rcScrollBar.bottom);
            if (!Misc.MapWindowPoints(hwnd, IntPtr.Zero, ref rc, 2))
            {
                return Rect.Empty;
            }

            // Since the scrollbar position is already mapped, restore them back
            rc.top = sbi.rcScrollBar.top;
            rc.bottom = sbi.rcScrollBar.bottom;

            NativeMethods.SIZE sizeArrow;

            using (ThemePart themePart = new ThemePart(hwnd, "SCROLLBAR"))
            {
                sizeArrow = themePart.Size((int)ThemePart.SCROLLBARPARTS.SBP_ARROWBTN, 0);
            }

            // check that the 2 buttons can hold in the scroll bar
            bool fThumbVisible = sbi.rcScrollBar.right - sbi.rcScrollBar.left >= 5 * sizeArrow.cx / 2;
            if (sbi.rcScrollBar.right - sbi.rcScrollBar.left < 2 * sizeArrow.cx)
            {
                // the scroll bar is tiny, need to shrink the button
                sizeArrow.cx = (sbi.rcScrollBar.right - sbi.rcScrollBar.left) / 2;
            }

            //
            // Builds prior to Vista 5359 failed to correctly account for RTL scrollbar layouts.
            //
            if ((Environment.OSVersion.Version.Major < 6) && (Misc.IsLayoutRTL(hwnd)))
            {
                if (item == WindowsScrollBar.ScrollBarItem.UpArrow)
                {
                    item = WindowsScrollBar.ScrollBarItem.DownArrow;
                }
                else if (item == WindowsScrollBar.ScrollBarItem.DownArrow)
                {
                    item = WindowsScrollBar.ScrollBarItem.UpArrow;
                }
                else if (item == WindowsScrollBar.ScrollBarItem.LargeIncrement)
                {
                    item = WindowsScrollBar.ScrollBarItem.LargeDecrement;
                }
                else if (item == WindowsScrollBar.ScrollBarItem.LargeDecrement)
                {
                    item = WindowsScrollBar.ScrollBarItem.LargeIncrement;
                }
            }

            switch (item)
            {
                case WindowsScrollBar.ScrollBarItem.UpArrow :
                    rc.left = sbi.rcScrollBar.left;
                    rc.right = sbi.rcScrollBar.left + sizeArrow.cx;
                    break;

                case WindowsScrollBar.ScrollBarItem.LargeIncrement :
                    if (fThumbVisible)
                    {
                        rc.left = rc.right;
                        rc.right = sbi.rcScrollBar.right - sizeArrow.cx;
                    }
                    else
                    {
                        rc.left = rc.right = sbi.rcScrollBar.left + sizeArrow.cx;
                    }
                    break;

                case WindowsScrollBar.ScrollBarItem.Thumb :
                    if (!fThumbVisible)
                    {
                        rc.left = rc.right = sbi.rcScrollBar.left + sizeArrow.cx;
                    }
                    break;

                case WindowsScrollBar.ScrollBarItem.LargeDecrement :
                    if (fThumbVisible)
                    {
                        rc.right = rc.left;
                        rc.left = sbi.rcScrollBar.left + sizeArrow.cx;
                    }
                    else
                    {
                        rc.left = rc.right = sbi.rcScrollBar.left + sizeArrow.cx;
                    }
                    break;

                case WindowsScrollBar.ScrollBarItem.DownArrow :
                    rc.left = sbi.rcScrollBar.right - sizeArrow.cx;
                    rc.right = sbi.rcScrollBar.right;
                    break;
            }

            // Don't need to normalize, OSVer conditional block converts to absolute coordinates.
            return rc.ToRect(false);
        }

        #endregion

        // ------------------------------------------------------
        //
        // Internal Types
        //
        // ------------------------------------------------------

        #region Internal Types

        internal enum ScrollBarInfo
        {
            CurrentPosition,
            MaximumPosition,
            MinimumPosition,
            PageSize,
            TrackPosition
        }

        #endregion

        // ------------------------------------------------------
        //
        // Private Methods
        //
        // ------------------------------------------------------

        #region Private Methods

        // Scroll by a given amount
        private void Scroll (ScrollAmount amount, int style)
        {
            IntPtr parentHwnd = _sbFlag == NativeMethods.SB_CTL ? Misc.GetWindowParent(_hwnd) : _hwnd;
            int wParam = 0;

            switch (amount)
            {
                case ScrollAmount.LargeDecrement :
                    wParam = NativeMethods.SB_PAGEUP;
                    break;

                case ScrollAmount.SmallDecrement :
                    wParam = NativeMethods.SB_LINEUP;
                    break;

                case ScrollAmount.LargeIncrement :
                    wParam = NativeMethods.SB_PAGEDOWN;
                    break;

                case ScrollAmount.SmallIncrement :
                    wParam = NativeMethods.SB_LINEDOWN;
                    break;
            }

            NativeMethods.ScrollInfo si = new NativeMethods.ScrollInfo ();
            si.fMask = NativeMethods.SIF_ALL;
            si.cbSize = Marshal.SizeOf (si.GetType ());

            if (!Misc.GetScrollInfo(_hwnd, _sbFlag, ref si))
            {
                return;
            }

            // If the scrollbar is at the maximum position and the user passes
            // pagedown or linedown, just return
            if ((si.nPos == si.nMax) && (wParam == NativeMethods.SB_PAGEDOWN || wParam == NativeMethods.SB_LINEDOWN))
            {
                return;
            }

            // If the scrollbar is at the minimum position and the user passes
            // pageup or lineup, just return
            if ((si.nPos == si.nMin) && (wParam == NativeMethods.SB_PAGEUP || wParam == NativeMethods.SB_LINEUP))
            {
                return;
            }

            int msg = (style == NativeMethods.SBS_HORZ) ? NativeMethods.WM_HSCROLL : NativeMethods.WM_VSCROLL;

            Misc.ProxySendMessage(parentHwnd, msg, (IntPtr)wParam, (IntPtr)(parentHwnd == _hwnd ? IntPtr.Zero : _hwnd));
        }

        #endregion

        // ------------------------------------------------------
        //
        // Private Fields
        //
        // ------------------------------------------------------

        #region Private Fields

        // Cached value for the scroll bar style
        private int _sbFlag;

        private static string [] _asNames = {
            SRID.LocalizedNameWindowsScrollBarBitsBackBySmallAmount,
            SRID.LocalizedNameWindowsScrollBarBitsBackByLargeAmount,
            SRID.LocalizedNameWindowsScrollBarBitsThumb,
            SRID.LocalizedNameWindowsScrollBarBitsForwardByLargeAmount,
            SRID.LocalizedNameWindowsScrollBarBitsForwardBySmallAmount
        };

        #endregion
    }
}
