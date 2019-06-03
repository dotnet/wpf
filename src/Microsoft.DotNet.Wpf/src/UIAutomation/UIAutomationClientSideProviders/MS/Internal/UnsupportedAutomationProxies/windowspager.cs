// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// Description: HWND-based Pager Proxy

using System;
using System.Collections;
using System.Text;
using System.ComponentModel;
using System.Windows.Automation;
using System.Windows.Automation.Provider;
using System.Runtime.InteropServices;
using System.Windows;
using MS.Internal.AutomationProxies;
using MS.Win32;
namespace MS.Internal.UnsupportedAutomationProxies
{
    internal class WindowsPager: ProxyHwnd, IScrollProvider, IRawElementProviderHwndOverride
    {
        // ------------------------------------------------------
        //
        // Constructors
        //
        // ------------------------------------------------------

        #region Constructors

        internal WindowsPager (IntPtr hwnd, ProxyFragment parent, int item)
            : base (hwnd, parent, item)
        {
            // Set the strings to return properly the properties.
            _sType = SR.Get(SRID.LocalizedControlTypePager);

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

            return new WindowsPager(hwnd, null, idChild);
        }

        // Static Create method called by the event tracker system
        internal static void RaiseEvents (IntPtr hwnd, int eventId, object idProp, int idObject, int idChild)
        {
            if (idObject != NativeMethods.OBJID_VSCROLL && idObject != NativeMethods.OBJID_HSCROLL)
            {
                WindowsPager wtv = (WindowsPager) Create (hwnd, 0);
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

        internal override object GetPatternProvider (AutomationPattern iid)
        {
            return iid == ScrollPattern.Pattern ? this : null;
        }

        //Gets the localized name
        internal override string LocalizedName
        {
            get
            {
                return SR.Get(SRID.LocalizedNameWindowsPager);
            }
        }

        #endregion

        #region ProxyFragment Interface

        // Returns the next sibling element in the raw hierarchy.
        // Peripheral controls have always negative values.
        // Returns null if no next child
        internal override ProxySimple GetNextSibling (ProxySimple child)
        {
            PagerItem item = (PagerItem) child._item;

            if (item == PagerItem.ChildWnd)
            {
                // Skip if not visible
                if (IsVisible(_hwnd, NativeMethods.PGB_TOPORLEFT))
                {
                    return new PagerButton (_hwnd, this, PagerItem.PrevBtn);
                }
                // fall into the NextBtn case
                item = PagerItem.PrevBtn;
            }

            if (item == PagerItem.PrevBtn)
            {
                // Skip if not visible
                if (IsVisible(_hwnd, NativeMethods.PGB_BOTTOMORRIGHT))
                {
                    return new PagerButton (_hwnd, this, PagerItem.NextBtn);
                }
            }
            return null;
        }

        // Returns the previous sibling element in the raw hierarchy.
        // Peripheral controls have always negative values.
        // Returns null is no previous
        internal override ProxySimple GetPreviousSibling (ProxySimple child)
        {
            PagerItem item = (PagerItem) child._item;

            if (item == PagerItem.NextBtn)
            {
                // Skip if not visible
                if (IsVisible(_hwnd, NativeMethods.PGB_TOPORLEFT))
                {
                    return new PagerButton (_hwnd, this, PagerItem.PrevBtn);
                }
                // fall into the PrevBtn case
                item = PagerItem.PrevBtn;
            }

            if (item == PagerItem.PrevBtn)
            {
                IntPtr hwndChild = Misc.GetWindow(_hwnd, NativeMethods.GW_CHILD);
                if (hwndChild != IntPtr.Zero)
                {
                    return new PagerChildOverrideProxy(hwndChild, this, PagerItem.ChildWnd);
                }
            }

            return null;
        }

        // Returns the first child element in the raw hierarchy.
        internal override ProxySimple GetFirstChild ()
        {
            IntPtr hwndChild = Misc.GetWindow(_hwnd, NativeMethods.GW_CHILD);

            if (hwndChild != IntPtr.Zero)
            {
                return new PagerChildOverrideProxy (hwndChild, this, PagerItem.ChildWnd);
            }
            return null;
        }

        // Returns the last child element in the raw hierarchy.
        internal override ProxySimple GetLastChild ()
        {
            // Skip if not visible
            if (IsVisible(_hwnd, NativeMethods.PGB_BOTTOMORRIGHT))
            {
                return new PagerButton (_hwnd, this, PagerItem.NextBtn);
            }

            // Skip if not visible
            if (IsVisible(_hwnd, NativeMethods.PGB_TOPORLEFT))
            {
                return new PagerButton (_hwnd, this, PagerItem.PrevBtn);
            }

            // Get the content
            IntPtr hwndChild = Misc.GetWindow(_hwnd, NativeMethods.GW_CHILD);

            if (hwndChild != IntPtr.Zero)
            {
                return new PagerChildOverrideProxy (hwndChild, this, PagerItem.ChildWnd);
            }

            return null;
        }

        // Returns a Proxy element corresponding to the specified screen coordinates.
        internal override ProxySimple ElementProviderFromPoint (int x, int y)
        {
            // prev button
            if (IsVisible(_hwnd, NativeMethods.PGB_TOPORLEFT))
            {
                NativeMethods.Win32Rect rc = PagerButton.BoundingRect(_hwnd, PagerItem.PrevBtn);
                if (Misc.PtInRect(ref rc, x, y))
                {
                    return new PagerButton (_hwnd, this, PagerItem.PrevBtn);
                }
            }

            // Next button
            if (IsVisible(_hwnd, NativeMethods.PGB_BOTTOMORRIGHT))
            {
                NativeMethods.Win32Rect rc = PagerButton.BoundingRect(_hwnd, PagerItem.NextBtn);
                if (Misc.PtInRect(ref rc, x, y))
                {
                    return new PagerButton (_hwnd, this, PagerItem.NextBtn);
                }
            }

            return null;
        }

        #endregion

        #region IRawElementProviderHwndOverride Interface 

        //------------------------------------------------------
        //
        //  Interface IRawElementProviderHwndOverride
        //
        //------------------------------------------------------
        IRawElementProviderSimple IRawElementProviderHwndOverride.GetOverrideProviderForHwnd (IntPtr hwnd)
        {
            // return the appropriate placeholder for the given hwnd...
            if (Misc.GetWindow(_hwnd, NativeMethods.GW_CHILD) == hwnd)
            {
                return new PagerChildOverrideProxy (hwnd, this, PagerItem.ChildWnd);
            }
            return null;
        }

        #endregion IRawElementProviderHwndOverride Interface 
    
        #region Scroll Pattern

        // Request to scroll Horizontally and vertically by the specified amount
        void IScrollProvider.SetScrollPercent (double horizontalPercent, double verticalPercent)
        {
            // Make sure that the control is enabled
            if (!SafeNativeMethods.IsWindowEnabled(_hwnd))
            {
                throw new ElementNotEnabledException();
            }

            if (!IsScrollable())
            {
                throw new InvalidOperationException(SR.Get(SRID.OperationCannotBePerformed));
            }

            bool fHorizontal = IsHorizontal(_hwnd);

            // Can scroll either horizontally or vertically, but not both. Sanity check
            if ((fHorizontal && (int)verticalPercent != (int)ScrollPattern.NoScroll) || 
                (!fHorizontal && (int)horizontalPercent != (int)ScrollPattern.NoScroll))
            {
                throw new InvalidOperationException(SR.Get(SRID.OperationCannotBePerformed));
            }

            if (!ScrollByPercent(fHorizontal ? horizontalPercent : verticalPercent))
            {
                throw new InvalidOperationException(SR.Get(SRID.OperationCannotBePerformed));
            }
        }

        // Request to scroll horizontally and vertically by the specified scrolling amount
        void IScrollProvider.Scroll (ScrollAmount HorizontalAmount, ScrollAmount VerticalAmount)
        {
            // Make sure that the control is enabled
            if (!SafeNativeMethods.IsWindowEnabled(_hwnd))
            {
                throw new ElementNotEnabledException();
            }

            if (!IsScrollable())
            {
                throw new InvalidOperationException(SR.Get(SRID.OperationCannotBePerformed));
            }

            bool fHorizontal = IsHorizontal(_hwnd);

            if ((fHorizontal && VerticalAmount != ScrollAmount.NoAmount) || (!fHorizontal && HorizontalAmount != ScrollAmount.NoAmount))
            {
                throw new InvalidOperationException(SR.Get(SRID.OperationCannotBePerformed));
            }

            // Fake the scroll with pushes on the buttons
            // It is risky to do a setpos since the calculation of the amount in pels to scroll
            // is dependent on the buttons being visible or not.
            switch (fHorizontal ? HorizontalAmount : VerticalAmount)
            {
                case ScrollAmount.LargeDecrement :
                case ScrollAmount.SmallDecrement :
                    if (IsVisible(_hwnd, NativeMethods.PGB_TOPORLEFT))
                    {
                        PagerButton pagerButton = new PagerButton (_hwnd, this, PagerItem.PrevBtn);
                        ((IInvokeProvider) pagerButton).Invoke ();
                        return;
                    }
                    throw new InvalidOperationException(SR.Get(SRID.OperationCannotBePerformed));

                case ScrollAmount.LargeIncrement :
                case ScrollAmount.SmallIncrement :
                    if (IsVisible(_hwnd, NativeMethods.PGB_BOTTOMORRIGHT))
                    {
                        PagerButton pagerButton = new PagerButton (_hwnd, this, PagerItem.NextBtn);
                        ((IInvokeProvider) pagerButton).Invoke ();
                        return;
                    }
                    throw new InvalidOperationException(SR.Get(SRID.OperationCannotBePerformed));

                default :
                    throw new InvalidOperationException(SR.Get(SRID.OperationCannotBePerformed));
            }
        }


        // Calc the position of the horizontal scroll bar thumb in the 0..100 % range
        double IScrollProvider.HorizontalScrollPercent
        {
            get
            {
                if (IsHorizontal (_hwnd))
                {
                    NativeMethods.Win32Rect rcChild = new NativeMethods.Win32Rect ();
                    int iPos = Misc.ProxySendMessageInt(_hwnd, NativeMethods.PGM_GETPOS, IntPtr.Zero, IntPtr.Zero);
                    int cRange = ScrollRange (ref rcChild);

                    return 100.0 * iPos / cRange;
                }

                return (double)ScrollPattern.NoScroll;
            }
        }

        // Calc the position of the vertical scroll bar thumb in the 0..100 % range
        double IScrollProvider.VerticalScrollPercent
        {
            get
            {
                if (!IsHorizontal (_hwnd))
                {
                    NativeMethods.Win32Rect rcChild = new NativeMethods.Win32Rect ();
                    int iPos = Misc.ProxySendMessageInt(_hwnd, NativeMethods.PGM_GETPOS, IntPtr.Zero, IntPtr.Zero);
                    int cRange = ScrollRange (ref rcChild);

                    return 100.0 * iPos / cRange;
                }
                return (double)ScrollPattern.NoScroll;
            }
        }

        // Percentage of the window that is visible along the horizontal axis. 
        double IScrollProvider.HorizontalViewSize
        {
            get
            {
                return IsHorizontal (_hwnd) ? RangeToVisibleRatio () : 100.0;
            }
        }

        // Percentage of the window that is visible along the vertical axis. 
        double IScrollProvider.VerticalViewSize
        {
            get
            {
                return !IsHorizontal (_hwnd) ? RangeToVisibleRatio () : 100.0;
            }
        }

        // Can the element be horizontaly scrolled
        bool IScrollProvider.HorizontallyScrollable
        {
            get
            {
                // if the child window is smaller than the pager window, set scrollable to false
                return IsHorizontal(_hwnd) && (IsVisible(_hwnd, NativeMethods.PGB_BOTTOMORRIGHT) || IsVisible(_hwnd, NativeMethods.PGB_TOPORLEFT));
            }
        }

        // Can the element be verticaly scrolled
        bool IScrollProvider.VerticallyScrollable
        {
            get
            {
                // if the child window is smaller than the pager window, set scrollable to false
                return !IsHorizontal(_hwnd) && (IsVisible(_hwnd, NativeMethods.PGB_BOTTOMORRIGHT) || IsVisible(_hwnd, NativeMethods.PGB_TOPORLEFT));
            }
        }
        #endregion Scroll Pattern

        // ------------------------------------------------------
        //
        // Private Methods
        //
        // ------------------------------------------------------        

        #region Private Methods

        private bool IsScrollable()
        {
            return IsVisible(_hwnd, NativeMethods.PGB_BOTTOMORRIGHT) || IsVisible(_hwnd, NativeMethods.PGB_TOPORLEFT);
        }

        // Scrolls the pager by a given percent from its current position.
        private bool ScrollByPercent(double scrollPercent)
        {
            // Check params
            if ((int)scrollPercent == (int)ScrollPattern.NoScroll)
            {
                return true;
            }

            if (scrollPercent < 0 || scrollPercent > 100)
            {
                throw new ArgumentOutOfRangeException(IsHorizontal(_hwnd) ? "horizontalPercent" : "verticalPercent", SR.Get(SRID.ScrollBarOutOfRange));
            }

            NativeMethods.Win32Rect rcChild = new NativeMethods.Win32Rect();
            int cRange = ScrollRange (ref rcChild);

            // indicative of an error
            if (cRange < 0)
            {
                return false;
            }

            // Do proper rounding
            int newPos = (int) (cRange * scrollPercent / 100 + 0.5);

            // Sometimes the PGM_SETPOS fails. Try 3 times when this happens
            for (int i = 0; i < 3; i++)
            {
                Misc.ProxySendMessage(_hwnd, NativeMethods.PGM_SETPOS, IntPtr.Zero, new IntPtr(newPos));
                if (Misc.ProxySendMessageInt(_hwnd, NativeMethods.PGM_GETPOS, IntPtr.Zero, IntPtr.Zero) == newPos)
                {
                    break;
                }
            }

            // PGM_SETPOS does not return a value
            return true;
        }

        // Calc in Pels the size of the child window minus the visible part of the pager
        private int ScrollRange (ref NativeMethods.Win32Rect rcChild)
        {
            // Get the child window rect
            IntPtr hwndChild = Misc.GetWindow(_hwnd, NativeMethods.GW_CHILD);
            if (hwndChild == IntPtr.Zero)
            {
                return -1;
            }

            if (!Misc.GetWindowRect(hwndChild, ref rcChild))
            {
                return -1;
            }

            NativeMethods.Win32Rect rcPager = new NativeMethods.Win32Rect ();
            if (!Misc.GetWindowRect(_hwnd, ref rcPager))
            {
                return -1;
            }
            int cRange = IsHorizontal (_hwnd) ? (int) ((rcChild.right - rcChild.left) - (rcPager.right - rcPager.left)) : (int) ((rcChild.right - rcChild.left) - (rcPager.bottom - rcPager.top));

            // add the border + 2 is hard coded in the source common control source code
            cRange += Misc.ProxySendMessageInt(_hwnd, NativeMethods.PGM_GETBORDER, IntPtr.Zero, IntPtr.Zero) + 2;
            int cButtons = IsVisible(_hwnd, NativeMethods.PGB_BOTTOMORRIGHT) ? 1 : 0;
            cButtons += IsVisible(_hwnd, NativeMethods.PGB_TOPORLEFT) ? 1 : 0;
            cRange += cButtons * Misc.ProxySendMessageInt(_hwnd, NativeMethods.PGM_GETBUTTONSIZE, IntPtr.Zero, IntPtr.Zero);

            return cRange;
        }

        // Calc the % of the visible part of the child window
        private double RangeToVisibleRatio()
        {
            // Get the child window rect
            NativeMethods.Win32Rect rcChild = new NativeMethods.Win32Rect ();
            int cRange = ScrollRange (ref rcChild);

            // error, default on the full width
            if (cRange == -1)
            {
                return 100.0;
            }

            int cWidth = rcChild.right - rcChild.left;
            return (double)(cWidth - cRange) / cWidth;
        }

        // class member for determining if a window has a certain style
        static internal bool HasStyle (IntPtr hwnd, int iStyle)
        {
            return Misc.IsBitSet(Misc.GetWindowStyle(hwnd), iStyle);
        }

        // class member for determining if the pager horizontal scroll style
        static internal bool IsHorizontal (IntPtr hwnd)
        {
            return HasStyle(hwnd, NativeMethods.PGS_HORZ);
        }        

        // retreives the PGM_GETBUTTONSTATE of a pager control hwnd
        static private int GetButtonState (IntPtr hwnd, int iButton)
        {
            return Misc.ProxySendMessageInt(hwnd, NativeMethods.PGM_GETBUTTONSTATE, IntPtr.Zero, (IntPtr)iButton);
        }

        // indicates if a given pager button is visible or not
        static internal bool IsVisible (IntPtr hwnd, int iButton)
        {
            return GetButtonState(hwnd, iButton) != NativeMethods.PGF_INVISIBLE;
        }

        #endregion

        // ------------------------------------------------------
        //
        // Private Fields and Types Declaration
        //
        // ------------------------------------------------------

        #region Private Fields

        // item indexs for children within the control
        private enum PagerItem
        {
            PrevBtn = -2,
            NextBtn = -1,
            ChildWnd = 0
        };

        #endregion

        // ------------------------------------------------------
        //
        //  PagerButton Private Class
        //
        //------------------------------------------------------

        #region PagerButton 

        // subclass of WindowsPager class
        // PagerControl button specific class the changes GetValue method
        class PagerButton: ProxySimple, IInvokeProvider
        {
            // ------------------------------------------------------
            //
            //  Constructors
            //
            //------------------------------------------------------

            #region Constructors

            internal PagerButton (IntPtr hwnd, ProxyFragment parent, PagerItem item)
            : base(hwnd, parent, (int) item)
            {
                // Set the strings to return properly the properties.
                _sType = SR.Get(SRID.LocalizedControlTypePagerButton);
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
                // This is the treeview container
                if (iid == InvokePattern.Pattern)
                {
                    return this;
                }
                return null;
            }

            // Gets the bounding rectangle for this element
            internal override Rect BoundingRectangle
            {
                get
                {
                    NativeMethods.Win32Rect rc = BoundingRect(_hwnd, (PagerItem)_item);
                    return rc.ToRect(Misc.IsControlRTL(_hwnd));
                }
            }

            //Gets the localized name
            internal override string LocalizedName
            {
                get
                {
                    if ((PagerItem)_item == WindowsPager.PagerItem.PrevBtn)
                    {
                        return SR.Get(SRID.LocalizedNameWindowsPagerButtonPrev);
                    }
                    else
                    {
                        return SR.Get(SRID.LocalizedNameWindowsPagerButtonNext);
                    }
                }
            }

            #endregion

            #region InvokeInteropPattern

            // Same as a click on one of the buttons
            void IInvokeProvider.Invoke ()
            {
                // Make sure that the control is enabled
                if (!SafeNativeMethods.IsWindowEnabled(_hwnd))
                {
                    throw new ElementNotEnabledException();
                }

                // Only if the button is visible
                if (!IsVisible(_hwnd, _item == (int)WindowsPager.PagerItem.PrevBtn ? NativeMethods.PGB_TOPORLEFT : NativeMethods.PGB_BOTTOMORRIGHT))
                {
                    throw new InvalidOperationException(SR.Get(SRID.OperationCannotBePerformed));
                }

                // rect of entire pager this is used to get the calc for the button
                NativeMethods.Win32Rect rcPager = new NativeMethods.Win32Rect ();
                if (!Misc.GetWindowRect(_hwnd, ref rcPager))
                {
                    throw new InvalidOperationException(SR.Get(SRID.OperationCannotBePerformed));
                }

                NativeMethods.Win32Rect rcButton = BoundingRect(_hwnd, (PagerItem)_item);

                // does the control have vertical scrolling buttons
                bool bHorz = WindowsPager.IsHorizontal (_hwnd);

                // Compute the metrics for both the button and the pager
                int cxButton = rcButton.right - rcButton.left;
                int cyButton = rcButton.bottom - rcButton.top;
                int x, y;

                // Calc the x and y position for the mouse
                if (_item == (int) PagerItem.PrevBtn)
                {
                    x = (bHorz ? -cxButton : cxButton) / 2;
                    y = (bHorz ? cyButton : -cyButton) / 2;
                }
                else
                {
                    int cHalfButtons = IsVisible(_hwnd, _item == (int)WindowsPager.PagerItem.PrevBtn ? NativeMethods.PGB_BOTTOMORRIGHT : NativeMethods.PGB_TOPORLEFT) ? -1 : 1;
                    x = bHorz ? rcButton.left - rcPager.left + cHalfButtons * cxButton / 2 : cxButton / 2;
                    y = bHorz ? cyButton / 2 : rcButton.top - rcPager.top + cHalfButtons * cyButton / 2;
                }

                // Fake a mouse click
                IntPtr center = NativeMethods.Util.MAKELPARAM (x, y);
                NativeMethods.Win32Rect rcChild = new NativeMethods.Win32Rect ();
                int cRange = ((WindowsPager) _parent).ScrollRange (ref rcChild);
                int iPrevPos = Misc.ProxySendMessageInt(_hwnd, NativeMethods.PGM_GETPOS, IntPtr.Zero, IntPtr.Zero);

                // Sometimes the Invoke fails. Try 3 times when this happens
                for (int i = 0; i < 3; i++)
                {
                    Misc.ProxySendMessage(_hwnd, NativeMethods.WM_LBUTTONDOWN, new IntPtr(NativeMethods.MK_LBUTTON), center);

                    // !!! The pager control will only take into account the Down/Up mouse action if the delta
                    // time between the 2 is at least the doubleClickTime / 8
                    System.Threading.Thread.Sleep (SafeNativeMethods.GetDoubleClickTime () / 4);
                    Misc.ProxySendMessage(_hwnd, NativeMethods.WM_LBUTTONUP, new IntPtr(NativeMethods.MK_LBUTTON), center);

                    // Sanity check
                    if ((iPrevPos == 0 && _item == (int) PagerItem.PrevBtn)
                    || (iPrevPos >= cRange && _item == (int) PagerItem.NextBtn)
                    || Misc.ProxySendMessageInt(_hwnd, NativeMethods.PGM_GETPOS, IntPtr.Zero, IntPtr.Zero) != iPrevPos)
                    {
                        break;
                    }
                }
            }

            #endregion

            // ------------------------------------------------------
            //
            //  Internal Methods
            //
            //------------------------------------------------------

            #region Internal Methods

            // Bounding rect of the pager control elements is calculated manually
            static internal NativeMethods.Win32Rect BoundingRect(IntPtr hwnd, PagerItem item)
            {
                // rect for object
                NativeMethods.Win32Rect itemrect = new NativeMethods.Win32Rect ();

                // size of button
                int iButtonSize = Misc.ProxySendMessageInt(hwnd, NativeMethods.PGM_GETBUTTONSIZE, IntPtr.Zero, IntPtr.Zero);

                // rect of entire pager this is used to get the calc for the button
                NativeMethods.Win32Rect pagerRect = new NativeMethods.Win32Rect ();
                if (!Misc.GetWindowRect(hwnd, ref pagerRect))
                {
                    return NativeMethods.Win32Rect.Empty;
                }

                // does the control have vertical scrolling buttons
                bool bHorz = WindowsPager.IsHorizontal(hwnd);

                if (PagerItem.PrevBtn == item)
                {
                    // Zero means invisible
                    if (!IsVisible(hwnd, NativeMethods.PGB_TOPORLEFT))
                    {
                        iButtonSize = 0;
                    }

                    itemrect.left = pagerRect.left;
                    itemrect.top = pagerRect.top;
                    if (bHorz)
                    {
                        itemrect.right = pagerRect.left + iButtonSize;
                        itemrect.bottom = pagerRect.bottom;
                    }
                    else
                    {
                        itemrect.right = pagerRect.right;
                        itemrect.bottom = pagerRect.top + iButtonSize;
                    }
                }
                else if (PagerItem.NextBtn == item)
                {
                    if (!IsVisible(hwnd, NativeMethods.PGB_BOTTOMORRIGHT))
                    {
                        iButtonSize = 0;
                    }

                    itemrect.right = pagerRect.right;
                    itemrect.bottom = pagerRect.bottom;
                    if (bHorz)
                    {
                        itemrect.top = pagerRect.top;
                        itemrect.left = pagerRect.right - iButtonSize;
                    }
                    else
                    {
                        itemrect.left = pagerRect.left;
                        itemrect.top = pagerRect.bottom - iButtonSize;
                    }

                }

                return itemrect;
            }

            #endregion
        }

        #endregion

        // ------------------------------------------------------
        //
        //  PagerChildOverrideProxy Private Class
        //
        //------------------------------------------------------

        #region PagerChildOverrideProxy

        class PagerChildOverrideProxy: ProxyHwnd
        {
            // ------------------------------------------------------
            //
            // Constructors
            //
            // ------------------------------------------------------

            #region Constructors

            // Usually the hwnd passed to the constructor is wrong. As the base class is doing 
            // nothing this does not matter. 
            // This avoid to making some extra calls to get this right.
            internal PagerChildOverrideProxy (IntPtr hwnd, ProxyFragment parent, PagerItem item)
            : base (hwnd, parent, (int) item)
            {
            }

            #endregion

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

            // Process all the Logical and Raw Element Properties
            internal override object GetElementProperty (AutomationProperty idProp)
            {
                // No property should be handled by the override proxy
                // Overrides the ProxySimple implementation.
                return null;
            }

            #endregion
        }

        #endregion
    }
}
