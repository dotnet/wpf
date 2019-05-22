// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// Description: Generic implementation of the scroll pattern for
//              controls having scroll bars.
//

using System;
using System.ComponentModel;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Automation.Provider;
using MS.Win32;

namespace MS.Internal.AutomationProxies
{
    // Static class used to support the Scroll pattern for controls that have scroll bars. 
    static class WindowScroll
    {
        #region Internal Methods

        // ------------------------------------------------------
        //
        // Internal Methods
        //
        // ------------------------------------------------------

        // Request to scroll Horizontally and vertically by the specified amount
        static internal void SetScrollPercent (IntPtr hwnd, double horizontalPercent, double verticalPercent, bool forceResults)
        {
            if (!IsScrollable(hwnd))
            {
                throw new InvalidOperationException(SR.Get(SRID.OperationCannotBePerformed));
            }

            bool resultsNoCheck;
            bool isHorizontal = SetScrollPercent (hwnd, horizontalPercent, NativeMethods.SB_HORZ, out resultsNoCheck);

            // This is needed for Controls that do not return the proper return codes to WinAPI Calls
            if (!isHorizontal && (forceResults && resultsNoCheck))
            {
                isHorizontal = true;
            }

            bool isVertical = SetScrollPercent (hwnd, verticalPercent, NativeMethods.SB_VERT, out resultsNoCheck);

            // This is needed for Controls that do not return the proper return codes to WinAPI Calls
            if (!isVertical && (forceResults && resultsNoCheck))
            {
                isVertical = true;
            }

            if (isHorizontal && isVertical)
            {
                return;
            }

            throw new InvalidOperationException(SR.Get(SRID.OperationCannotBePerformed));
        }

        // Request to scroll horizontally and vertically by the specified scrolling amount
        static internal void Scroll (IntPtr hwnd, ScrollAmount HorizontalAmount, ScrollAmount VerticalAmount, bool fForceResults)
        {
            if (!IsScrollable(hwnd))
            {
                throw new InvalidOperationException(SR.Get(SRID.OperationCannotBePerformed));
            }

            bool fHz = ScrollCursor(hwnd, HorizontalAmount, NativeMethods.SB_HORZ, fForceResults);
            bool fVt = ScrollCursor (hwnd, VerticalAmount, NativeMethods.SB_VERT, fForceResults);

            if ( fHz && fVt )
                return;

            throw new InvalidOperationException(SR.Get(SRID.OperationCannotBePerformed));
        }

        // Process the Scroll Properties
        static internal object GetPropertyScroll (AutomationProperty idProp, IntPtr hwnd)
        {
            // ...handle the scroll properties...
            if (idProp == ScrollPattern.HorizontalScrollPercentProperty)
            {
                return Scrollable (hwnd, NativeMethods.SB_HORZ) ? GetScrollInfo (hwnd, NativeMethods.SB_HORZ) : ScrollPattern.NoScroll;
            }
            else if (idProp == ScrollPattern.VerticalScrollPercentProperty)
            {
                return Scrollable (hwnd, NativeMethods.SB_VERT) ? GetScrollInfo (hwnd, NativeMethods.SB_VERT) : ScrollPattern.NoScroll;
            }
            else if (idProp == ScrollPattern.HorizontalViewSizeProperty)
            {
                return Scrollable (hwnd, NativeMethods.SB_HORZ) ? ScrollViewSize (hwnd, NativeMethods.SB_HORZ) : 100.0;
            }
            else if (idProp == ScrollPattern.VerticalViewSizeProperty)
            {
                return Scrollable (hwnd, NativeMethods.SB_VERT) ? ScrollViewSize (hwnd, NativeMethods.SB_VERT) : 100.0;
            }
            else if (idProp == ScrollPattern.HorizontallyScrollableProperty)
            {
                return Scrollable (hwnd, NativeMethods.SB_HORZ);
            }
            else if (idProp == ScrollPattern.VerticallyScrollableProperty)
            {
                return Scrollable (hwnd, NativeMethods.SB_VERT);
            }

            return null;
        }

        // Finds if a control can be scrolled
        static internal bool Scrollable (IntPtr hwnd, int sbFlag)
        {
            int style = Misc.GetWindowStyle(hwnd);

            if ((sbFlag == NativeMethods.SB_HORZ && !Misc.IsBitSet(style, NativeMethods.WS_HSCROLL)) ||
                (sbFlag == NativeMethods.SB_VERT && !Misc.IsBitSet(style, NativeMethods.WS_VSCROLL)))
            {
                return false;
            }

            if (!Misc.IsEnabled(hwnd))
            {
                return false;
            }

            // Check if the scroll info shows the scroll bar as enabled.
            bool scrollBarEnabled = false;
            NativeMethods.ScrollBarInfo sbi = new NativeMethods.ScrollBarInfo();
            sbi.cbSize = Marshal.SizeOf(sbi.GetType());
            int scrollBarObjectId =
                (sbFlag == NativeMethods.SB_VERT) ? NativeMethods.OBJID_VSCROLL : NativeMethods.OBJID_HSCROLL;
            if (Misc.GetScrollBarInfo(hwnd, scrollBarObjectId, ref sbi))
            {
                scrollBarEnabled =
                    !Misc.IsBitSet(sbi.scrollBarInfo, NativeMethods.STATE_SYSTEM_UNAVAILABLE);
            }

            if (!scrollBarEnabled)
            {
                return false;
            }

            // Get scroll range
            NativeMethods.ScrollInfo si = new NativeMethods.ScrollInfo ();
            si.cbSize = Marshal.SizeOf (si.GetType ());
            si.fMask = NativeMethods.SIF_ALL;

            if (!Misc.GetScrollInfo(hwnd, sbFlag, ref si))
            {
                return false;
            }

            return (si.nMax != si.nMin && si.nPage != si.nMax - si.nMin + 1);
        }

        static internal bool HasScrollableStyle(IntPtr hwnd)
        {
            int style = Misc.GetWindowStyle(hwnd);

            bool hasScrollableStyle = Misc.IsBitSet(style, NativeMethods.WS_HSCROLL) || Misc.IsBitSet(style, NativeMethods.WS_VSCROLL);

            string className = Misc.ProxyGetClassName(hwnd);
            if (className.StartsWith("RichEdit", StringComparison.OrdinalIgnoreCase) ||
                className.StartsWith("WindowForms10.RichEdit", StringComparison.OrdinalIgnoreCase) ||
                string.Compare(className, "Edit", StringComparison.OrdinalIgnoreCase) == 0)
            {
                hasScrollableStyle = Misc.IsBitSet(style, NativeMethods.ES_MULTILINE);
            }

            return hasScrollableStyle;
        }

        // Finds if a control can be scrolled
        static internal bool IsScrollable(IntPtr hwnd)
        {
            return Scrollable(hwnd, NativeMethods.SB_HORZ) || Scrollable(hwnd, NativeMethods.SB_VERT);
        }

        #endregion

        #region Private Methods

        // ------------------------------------------------------
        //
        // Private Methods
        //
        // ------------------------------------------------------

        // Retrieve the scrollbar position in the [0..100]% range
        static private double GetScrollInfo(IntPtr hwnd, int sbFlag)
        {
            // check if there is a scrollbar 
            NativeMethods.ScrollInfo si = new NativeMethods.ScrollInfo ();

            si.fMask = NativeMethods.SIF_ALL;
            si.cbSize = Marshal.SizeOf (si.GetType ());

            if (Misc.GetScrollInfo(hwnd, sbFlag, ref si))
            {
                if (si.nMax != si.nMin && si.nPage != si.nMax - si.nMin + 1)
                {
                    int delta;

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

                    string classname = Misc.GetClassName(hwnd);
                    if (classname.ToLower(System.Globalization.CultureInfo.InvariantCulture).Contains("richedit"))
                    {
                        delta = (si.nPage > 0) ? si.nPage : 0;
                    }
                    else
                    {
                        delta = (si.nPage > 0) ? si.nPage - 1 : 0;
                    }

                    return 100.0 * (si.nPos - si.nMin) / ((si.nMax - delta) - si.nMin);
                }
            }
            return (double)ScrollPattern.NoScroll;
        }

        // View Size
        static private double ScrollViewSize(IntPtr hwnd, int sbFlag)
        {
            // Get scroll range and page size
            NativeMethods.ScrollInfo si = new NativeMethods.ScrollInfo ();
            si.cbSize = Marshal.SizeOf (si.GetType ());
            si.fMask = NativeMethods.SIF_RANGE | NativeMethods.SIF_PAGE;

            if (!Misc.GetScrollInfo(hwnd, sbFlag, ref si) || (si.nMax == si.nMin))
            {
                return 100.0;
            }
            else
            {
                // "+1" because nPage can be 0 to nMax-nMin+1
                int nPage = si.nPage > 0 ? si.nPage : 1;

                return (100.0 * nPage) / (si.nMax + 1 - si.nMin);
            }
        }

        // Request to scroll a control horizontally or vertically by a specified amount.
        static private bool SetScrollPercent(IntPtr hwnd, double fScrollPos, int sbFlag, out bool forceResults)
        {
            forceResults = false;
            // Check param
            if ((int)fScrollPos == (int)ScrollPattern.NoScroll)
            {
                return true;
            }

            if (!Scrollable(hwnd, sbFlag))
            {
                return false;
            }

            if (fScrollPos < 0 || fScrollPos > 100)
            {
                throw new ArgumentOutOfRangeException(sbFlag == NativeMethods.SB_HORZ ? "horizontalPercent" : "verticalPercent", SR.Get(SRID.ScrollBarOutOfRange));
            }

            // Get Max & min                    
            NativeMethods.ScrollInfo si = new NativeMethods.ScrollInfo ();
            si.fMask = NativeMethods.SIF_ALL;
            si.cbSize = Marshal.SizeOf(si.GetType ());

            // if no scroll bar return false
            // on Win 6.0 success is false
            // on other system check through the scroll info is a scroll bar is there
            if (!Misc.GetScrollInfo(hwnd, sbFlag, ref si) ||
                !((si.nMax != si.nMin && si.nPage != si.nMax - si.nMin + 1)))
            {
                return false;
            }

            // Set position
            int delta = (si.nPage > 0) ? si.nPage - 1 : 0;

            int newPos = (int) Math.Round (((si.nMax - delta) - si.nMin) * fScrollPos / 100.0 + si.nMin);

            // No move, exit
            if (newPos == si.nPos)
            {
                return true;
            }
            si.nPos = newPos;

            forceResults = true;

            int message = sbFlag == NativeMethods.SB_HORZ ? NativeMethods.WM_HSCROLL : NativeMethods.WM_VSCROLL;

            int wParam = NativeMethods.Util.MAKELONG(NativeMethods.SB_THUMBPOSITION, si.nPos);
            bool fRet = Misc.ProxySendMessageInt(hwnd, message, (IntPtr)wParam, IntPtr.Zero) == 0;

            if (fRet && Misc.GetScrollInfo(hwnd, sbFlag, ref si) && si.nPos != newPos)
            {
                // WinForms treeview has some problems.  The first is that the SendMessage with WM_HSCROLL/WM_VSCROLL
                // with SB_THUMBPOSITION is not moving the scroll position. The second problem is that SetScrollInfo()
                // lose the theming for the scroll bars and it really does not move the scroll position.  The
                // scrollbars change but it does not scroll the treeview control.

                int prevPos = newPos;
                ScrollAmount prevAmount = si.nPos > newPos ? ScrollAmount.SmallDecrement : ScrollAmount.SmallIncrement;
                do
                {
                    ScrollAmount amount = si.nPos > newPos ? ScrollAmount.SmallDecrement : ScrollAmount.SmallIncrement;

                    // If we were moving in one direction and overshoot, break to prevent getting into infant loop.
                    // If ScrollCursor() can not set the new position, also break to prevent infant loop.
                    if (prevAmount != amount || prevPos == si.nPos)
                    {
                        break;
                    }
                    prevPos = si.nPos;
                    fRet = ScrollCursor(hwnd, amount, sbFlag, forceResults);
                } while (fRet && Misc.GetScrollInfo(hwnd, sbFlag, ref si) && si.nPos != newPos);
            }

            return fRet;
        }

        // Scroll control by a given amount
        static private bool ScrollCursor(IntPtr hwnd, ScrollAmount amount, int sbFlag, bool fForceResults)
        {
            // Check Param
            if (amount == ScrollAmount.NoAmount)
            {
                return true;
            }

            if (!Scrollable(hwnd, sbFlag))
            {
                return false;
            }

            // Get Max & min
            NativeMethods.ScrollInfo si = new NativeMethods.ScrollInfo ();
            si.fMask = NativeMethods.SIF_ALL;
            si.cbSize = Marshal.SizeOf (si.GetType ());

            // if no scroll bar return false
            // on Win 6.0 success is false
            // on other system check through the scroll info is a scroll bar is there
            if ((!Misc.GetScrollInfo(hwnd, sbFlag, ref si) ||
                !((si.nMax != si.nMin && si.nPage != si.nMax - si.nMin + 1))))
            {
                return false;
            }

            // Get Action to perform
            int nAction;

            if (sbFlag == NativeMethods.SB_HORZ)
            {
                switch (amount)
                {
                    case ScrollAmount.SmallDecrement :
                        nAction = NativeMethods.SB_LINELEFT;
                        break;

                    case ScrollAmount.LargeDecrement :
                        nAction = NativeMethods.SB_PAGELEFT;
                        break;

                    case ScrollAmount.SmallIncrement :
                        nAction = NativeMethods.SB_LINERIGHT;
                        break;

                    case ScrollAmount.LargeIncrement :
                        nAction = NativeMethods.SB_PAGERIGHT;
                        break;

                    default :
                        return false;
                }
            }
            else
            {
                switch (amount)
                {
                    case ScrollAmount.SmallDecrement :
                        nAction = NativeMethods.SB_LINEUP;
                        break;

                    case ScrollAmount.LargeDecrement :
                        nAction = NativeMethods.SB_PAGEUP;
                        break;

                    case ScrollAmount.SmallIncrement :
                        nAction = NativeMethods.SB_LINEDOWN;
                        break;

                    case ScrollAmount.LargeIncrement :
                        nAction = NativeMethods.SB_PAGEDOWN;
                        break;

                    default :
                        return false;
                }
            }

            // Set position
            int wParam = NativeMethods.Util.MAKELONG (nAction, 0);
            int message = sbFlag == NativeMethods.SB_HORZ ? NativeMethods.WM_HSCROLL : NativeMethods.WM_VSCROLL;
            int result = Misc.ProxySendMessageInt(hwnd, message, (IntPtr)wParam, IntPtr.Zero);

            return result == 0 || fForceResults;
        }

    #endregion
    }
}
