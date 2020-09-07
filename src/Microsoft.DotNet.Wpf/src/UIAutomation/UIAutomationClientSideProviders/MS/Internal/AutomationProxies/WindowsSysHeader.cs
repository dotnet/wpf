// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// Description: Win32 SysHeader32 proxy
//

using System;
using System.Collections;
using System.Globalization;
using System.Runtime.InteropServices;
using System.ComponentModel;
using System.Text;
using System.Windows.Automation;
using System.Windows.Automation.Provider;
using System.Windows;
using System.Diagnostics;
using MS.Win32;
using NativeMethodsSetLastError = MS.Internal.UIAutomationClientSideProviders.NativeMethodsSetLastError;

namespace MS.Internal.AutomationProxies
{
    // Windows SysHeader32 proxy
    // NOTE: Since this proxy has its own HWND, it will be always discovered by UIAutomation
    // and placed where it should be
    // we MUST NEVER create a hard connection between us and header (via _parent) ourselves
    class WindowsSysHeader: ProxyHwnd
    {
        //------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------

        #region Constructors

        internal WindowsSysHeader (IntPtr hwnd)
            : base( hwnd, null, 0)
        {
            _cControlType = ControlType.Header;
            _fIsContent = false;
            _sAutomationId = "Header"; // This string is a non-localizable string

            // support for events
            _createOnEvent = new WinEventTracker.ProxyRaiseEvents (RaiseEvents);
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
            WindowsSysHeader header = new WindowsSysHeader(hwnd);

            if (idChild != 0)
            {
                return header.CreateHeaderItem(idChild - 1);
            }

            return header;
        }

        internal static void RaiseEvents (IntPtr hwnd, int eventId, object idProp, int idObject, int idChild)
        {
            if (idObject != NativeMethods.OBJID_VSCROLL && idObject != NativeMethods.OBJID_HSCROLL)
            {
                ProxyFragment header = new WindowsSysHeader(hwnd);
                AutomationProperty property = idProp as AutomationProperty;
                if (property == TablePattern.ColumnHeadersProperty || property == TablePattern.RowHeadersProperty)
                {
                    // Check if the parent is a ListView
                    IntPtr hwndParent = NativeMethodsSetLastError.GetAncestor (hwnd, NativeMethods.GA_PARENT);
                    if (hwndParent != IntPtr.Zero)
                    {
                        if (Misc.GetClassName(hwndParent).IndexOf("SysListView32", StringComparison.Ordinal) >= 0)
                        {
                            // Notify the Listview that the header Change
                            WindowsListView wlv = (WindowsListView) WindowsListView.Create (hwndParent, 0);
                            if (wlv != null)
                            {
                                wlv.DispatchEvents (eventId, idProp, idObject, idChild);
                            }
                        }
                    }
                }
                else
                {
                    if (idProp == InvokePattern.InvokedEvent)
                    {
                        ProxySimple headerItem = new HeaderItem(hwnd, header, idChild);
                        headerItem.DispatchEvents(eventId, idProp, idObject, idChild);
                    }
                    else
                    {
                        header.DispatchEvents(eventId, idProp, idObject, idChild);
                    }
                }
            }
        }

        #endregion

        //------------------------------------------------------
        //
        //  Patterns Implementation
        //
        //------------------------------------------------------

        #region ProxyFragment Interface

        // Returns the next sibling element in the raw hierarchy.
        // Peripheral controls have always negative values.
        // Returns null if no next child
        internal override ProxySimple GetNextSibling (ProxySimple child)
        {
            // Determine how many items are in the list view.
            int item = child._item;

            return item + 1 < Length ? CreateHeaderItem (item + 1) : null;
        }

        // Returns the previous sibling element in the raw hierarchy.
        // Peripheral controls have always negative values.
        // Returns null is no previous
        internal override ProxySimple GetPreviousSibling (ProxySimple child)
        {
            // If the index of the previous node would be out of range...
            int item = child._item;

            return item > 0 ? CreateHeaderItem (item - 1) : null;
        }

        // Returns the first child element in the raw hierarchy.
        internal override ProxySimple GetFirstChild ()
        {
            return Length > 0 ? CreateHeaderItem (0) : null;
        }

        // Returns the last child element in the raw hierarchy.
        internal override ProxySimple GetLastChild ()
        {
            int count = Length;

            return count > 0 ? CreateHeaderItem (count - 1) : null;
        }

        // Returns a Proxy element corresponding to the specified screen coordinates.
        internal override ProxySimple ElementProviderFromPoint (int x, int y)
        {
            NativeMethods.HDHITTESTINFO HitTestInfo = new NativeMethods.HDHITTESTINFO();

            HitTestInfo.pt = new NativeMethods.Win32Point (x, y);

            int index = -1;

            if (Misc.MapWindowPoints(IntPtr.Zero, _hwnd, ref HitTestInfo.pt, 1))
            {
                unsafe
                {
                    index = XSendMessage.XSendGetIndex(_hwnd, NativeMethods.HDM_HITTEST, IntPtr.Zero, new IntPtr(&HitTestInfo), Marshal.SizeOf(HitTestInfo.GetType()));
                }
            }

            // make sure that hit-test happened on the header item itself
            if (index != -1 && (NativeMethods.HHT_ONHEADER == (HitTestInfo.flags & 0x000F)))
            {
                return CreateHeaderItem (GetItemFromIndex (index));
            }

            return this;
        }

        // Returns an item corresponding to the focused element (if there is one), or null otherwise.
        internal override ProxySimple GetFocus ()
        {
            int item = Misc.ProxySendMessageInt(_hwnd, NativeMethods.HDM_GETFOCUSEDITEM, IntPtr.Zero, IntPtr.Zero);
            if (item < Length)
                return CreateHeaderItem (item);

            return null;
        }

        #endregion ProxyFragment Interface

        #region ProxySimple Interface

        // Process all the Logical and Raw Element Properties
        internal override object GetElementProperty(AutomationProperty idProp)
        {
            if (idProp == AutomationElement.OrientationProperty)
            {
                return Misc.IsBitSet(WindowStyle, NativeMethods.HDS_VERT) ? OrientationType.Vertical : OrientationType.Horizontal;
            }

            return base.GetElementProperty(idProp);
        }

        //Gets the localized name
        internal override string LocalizedName
        {
            get
            {
                return SR.Get(SRID.LocalizedNameWindowsSysHeader);
            }
        }

        #endregion ProxySimple Interface

        // ------------------------------------------------------
        //
        // Private Methods
        //
        // ------------------------------------------------------

        #region Private Methods

        private void GetVisibleHeaderItemRange(
            out HeaderItem firstVisibleHeaderItem, out HeaderItem lastVisibleHeaderItem)
        {
            firstVisibleHeaderItem = null;
            lastVisibleHeaderItem = null;
            for (HeaderItem headerItem = GetFirstChild() as HeaderItem;
                            headerItem != null;
                            headerItem = GetNextSibling(headerItem) as HeaderItem)
            {
                bool isOffscreen = (bool) headerItem.GetElementProperty(AutomationElement.IsOffscreenProperty);
                if (!isOffscreen)
                {
                    // Header item is visible.
                    if (firstVisibleHeaderItem == null)
                    {
                        firstVisibleHeaderItem = headerItem;
                    }
                    lastVisibleHeaderItem = headerItem;
                }
            }
        }

        // Scroll the specified headerItem horizontally into view.
        internal void ScrollIntoView(HeaderItem headerItem)
        {
            // Check if the parent is a ListView
            IntPtr hwndParent = NativeMethodsSetLastError.GetAncestor (_hwnd, NativeMethods.GA_PARENT);
            if (hwndParent != IntPtr.Zero)
            {
                if (Misc.GetClassName(hwndParent).IndexOf("SysListView32", StringComparison.Ordinal) >= 0)
                {
                    // Determine the number of pixels or columns to scroll horizontally.
                    int pixels = 0;
                    int columns = 0;

                    // Get first and last visible header items.
                    HeaderItem firstVisibleHeaderItem;
                    HeaderItem lastVisibleHeaderItem;
                    GetVisibleHeaderItemRange(out firstVisibleHeaderItem, out lastVisibleHeaderItem);
                    if (firstVisibleHeaderItem != null && firstVisibleHeaderItem._item > headerItem._item)
                    {
                        // Scroll backward.
                        pixels = (int)(headerItem.BoundingRectangle.Left - firstVisibleHeaderItem.BoundingRectangle.Left);
                        columns = headerItem._item - firstVisibleHeaderItem._item;
                    }
                    else if (lastVisibleHeaderItem != null && headerItem._item > lastVisibleHeaderItem._item)
                    {
                        // Scroll forward.
                        pixels = (int)(headerItem.BoundingRectangle.Left - lastVisibleHeaderItem.BoundingRectangle.Left);
                        columns = headerItem._item - lastVisibleHeaderItem._item;
                    }

                    int horizontalScrollAmount = 0;
                    if (WindowsListView.IsListMode(hwndParent))
                    {
                        // In list mode, LVM_SCROLL uses a column count.
                        horizontalScrollAmount = columns;
                    }
                    else if (WindowsListView.IsDetailMode(hwndParent))
                    {
                        // In details mode, LVM_SCROLL uses a pixel count.
                        horizontalScrollAmount = pixels;
                    }

                    if (horizontalScrollAmount != 0)
                    {
                        Misc.ProxySendMessage(hwndParent, NativeMethods.LVM_SCROLL, new IntPtr(horizontalScrollAmount), IntPtr.Zero);
                    }
                }
            }
        }

        private static bool HeaderIsHidden (IntPtr hwnd)
        {
            return Misc.IsBitSet(Misc.GetWindowStyle(hwnd), NativeMethods.HDS_HIDDEN);
        }

        // Map a header item
        static private int OrderToIndex (IntPtr hwnd, int order)
        {
            return Misc.ProxySendMessageInt(hwnd, NativeMethods.HDM_ORDERTOINDEX, new IntPtr(order), IntPtr.Zero);
        }
        // retrieve count of header items
        static private int HeaderItemCount (IntPtr hwnd)
        {
            return Misc.ProxySendMessageInt(hwnd, NativeMethods.HDM_GETITEMCOUNT, IntPtr.Zero, IntPtr.Zero);
        }

        private int GetItemFromIndex (int index)
        {
            NativeMethods.HDITEM item = new NativeMethods.HDITEM();
            item.Init();
            item.mask = NativeMethods.HDI_ORDER;

            // Send the message...
            if (!XSendMessage.GetItem(_hwnd, index, ref item))
            {
                return -1;
            }

            return item.iOrder;
        }


        // Creates a header item
        private ProxySimple CreateHeaderItem (int index)
        {
            return new HeaderItem (_hwnd, this, index);
        }

        // Returns the number of elments in the Header
        private int Length
        {
            get
            {
                return HeaderItemCount (_hwnd);
            }
        }

        #endregion Private Methods

        //------------------------------------------------------
        //
        //  HeaderItem Private Class
        //
        //------------------------------------------------------

        #region HeaderItem

        internal class HeaderItem: ProxyFragment, IInvokeProvider, IExpandCollapseProvider
        {
            //------------------------------------------------------
            //
            //  Constructors
            //
            //------------------------------------------------------

            #region Constructors

            internal HeaderItem (IntPtr hwnd, ProxyFragment parent, int item)
            : base( hwnd, parent, item )
            {
                if (IsSplitButton())
                {
                    _cControlType = ControlType.SplitButton;
                }
                else
                {
                    _cControlType = ControlType.HeaderItem;
                }

                // This string is a non-localizable string.
                _sAutomationId = "HeaderItem " + item.ToString(CultureInfo.InvariantCulture);

                _fIsContent = false;
            }

            #endregion Constructors

            //------------------------------------------------------
            //
            //  Patterns Implementation
            //
            //------------------------------------------------------

            #region ProxySimple Interface

            // Returns a pattern interface if supported.
            internal override object GetPatternProvider (AutomationPattern iid)
            {
                if (RetrievePattern ())
                {
                    if (iid == InvokePattern.Pattern)
                    {
                        return (IsPushButton ()) ? this : null;
                    }
                    else if (iid == ExpandCollapsePattern.Pattern && IsSplitButton())
                    {
                        return this;
                    }
                }

                return null;
            }

            // Gets the bounding rectangle for this element
            internal override Rect BoundingRectangle
            {
                get
                {
                    // Don't need to normalize, GetItemRect returns absolute coordinates.
                    return BoundingRect().ToRect(false);
                }
            }

            // Process all the Logical and Raw Element Properties
            internal override object GetElementProperty (AutomationProperty idProp)
            {
                if (idProp == AutomationElement.AccessKeyProperty)
                {
                    return Misc.AccessKey(Text);
                }
                else  if (idProp == AutomationElement.IsOffscreenProperty)
                {
                    NativeMethods.Win32Rect itemRect = BoundingRect();
                    if (itemRect.IsEmpty)
                    {
                        return true;
                    }

                    // Need to check if this item is visible on the whole control not just its immediate parent.
                    IntPtr hwndParent = Misc.GetParent(_hwnd);
                    if (hwndParent != IntPtr.Zero)
                    {
                        NativeMethods.Win32Rect parentRect = NativeMethods.Win32Rect.Empty;
                        if (Misc.GetClientRectInScreenCoordinates(hwndParent, ref parentRect) && !parentRect.IsEmpty)
                        {
                            if (!Misc.IsItemVisible(ref parentRect, ref itemRect))
                            {
                                return true;
                            }
                        }
                    }
                }

                return base.GetElementProperty (idProp);
            }

            // Gets the localized name
            internal override string LocalizedName
            {
                get
                {
                    return Misc.StripMnemonic(Text);
                }
            }

            #endregion ProxySimple Interface

            #region Invoke Pattern

            // Same as a click on one of the header element
            void IInvokeProvider.Invoke ()
            {
                // Make sure that the control is enabled
                if (!SafeNativeMethods.IsWindowEnabled(_hwnd))
                {
                    throw new ElementNotEnabledException();
                }

                WindowsSysHeader parent = _parent as WindowsSysHeader;
                if (parent != null)
                {
                    parent.ScrollIntoView(this);
                }

                NativeMethods.Win32Point pt;

                if (!GetInvokationPoint (out pt))
                {
                    throw new InvalidOperationException(SR.Get(SRID.OperationCannotBePerformed));
                }

                IntPtr center = NativeMethods.Util.MAKELPARAM (pt.x, pt.y);

                // click
                Misc.ProxySendMessage(_hwnd, NativeMethods.WM_LBUTTONDOWN, new IntPtr(NativeMethods.MK_LBUTTON), center);
                Misc.ProxySendMessage(_hwnd, NativeMethods.WM_LBUTTONUP, IntPtr.Zero, center);
            }

            #endregion Invoke Pattern
            #region ExpandCollapse Pattern

            void IExpandCollapseProvider.Expand ()
            {
                if (!IsExpanded())
                {
                    ClickSplitButton();
                }

            }

            void IExpandCollapseProvider.Collapse ()
            {
                if (IsExpanded())
                {
                    ClickSplitButton();
                }
            }

            ExpandCollapseState IExpandCollapseProvider.ExpandCollapseState
            {
                get
                {
                    if (IsExpanded())
                    {
                        return ExpandCollapseState.Expanded;
                    }

                    return ExpandCollapseState.Collapsed;
                }
            }

            #endregion ExpandCollapse Pattern

            // This routine is only called on elements belonging to an hwnd that has the focus.
            protected override bool IsFocused ()
            {
                if (Misc.IsComctrlV6OnOsVerV6orHigher(_hwnd))
                {
                    int item = Misc.ProxySendMessageInt(_hwnd, NativeMethods.HDM_GETFOCUSEDITEM, IntPtr.Zero, IntPtr.Zero);
                    return item == _item;
                }

                return false;
            }

            //------------------------------------------------------
            //
            //  Private Methods
            //
            //------------------------------------------------------

            #region Private Methods

            private string Text
            {
                get
                {
                    // get index
                    int index = OrderToIndex (_hwnd, _item);

                    // new HDITEM to hold text
                    // maximum size of 256 characters used here arbitrarily
                    NativeMethods.HDITEM hdi = new NativeMethods.HDITEM();
                    hdi.Init();
                    hdi.mask = NativeMethods.HDI_TEXT;
                    hdi.cchTextMax = 256;

                    return XSendMessage.GetItemText(_hwnd, index, hdi);
                }
            }

            // Gets the bounding rectangle for this element
            private NativeMethods.Win32Rect BoundingRect ()
            {
                // get index
                int index = OrderToIndex (_hwnd, _item);
                NativeMethods.Win32Rect rectW32 = NativeMethods.Win32Rect.Empty;

                bool result;
                unsafe
                {
                    result = XSendMessage.XSend(_hwnd, NativeMethods.HDM_GETITEMRECT, new IntPtr(index), new IntPtr(&rectW32), Marshal.SizeOf(rectW32.GetType()), XSendMessage.ErrorValue.Zero);
                }

                if (result)
                {
                    if (!Misc.MapWindowPoints(_hwnd, IntPtr.Zero, ref rectW32, 2))
                    {
                        return NativeMethods.Win32Rect.Empty;
                    }

                    // Remove the space that is used to
                    if (!IsFilter ())
                    {
                        // From the source code for the SysHeader control.
                        // This is the divider slop area. Selecting this area with the mouse does not select
                        // the header, it perpares the header/column to be resized.
                        int cxBorder = 8 * UnsafeNativeMethods.GetSystemMetrics (NativeMethods.SM_CXBORDER);

                        if (Misc.IsLayoutRTL(_hwnd))
                        {
                            // Right to left mirroring style

                            // adjust the left margin
                            rectW32.left += cxBorder;
                            if (rectW32.left > rectW32.right)
                            {
                                rectW32.left = rectW32.right;
                            }

                            // adjust the right margin
                            if (_item > 0)
                            {
                                rectW32.right -= cxBorder;
                                if (rectW32.right < rectW32.left)
                                {
                                    rectW32.right = rectW32.left;
                                }
                            }
                        }
                        else
                        {
                            // adjust the left margin
                            if (_item > 0)
                            {
                                rectW32.left += cxBorder;
                                if (rectW32.left > rectW32.right)
                                {
                                    rectW32.left = rectW32.right;
                                }
                            }

                            // adjust the right margin
                            rectW32.right -= cxBorder;
                            if (rectW32.right < rectW32.left)
                            {
                                rectW32.right = rectW32.left;
                            }
                        }
                    }
                    return rectW32;
                }

                return NativeMethods.Win32Rect.Empty;
            }

            // this method detects if header item
            // is in the state when it can be asked for the pattern
            private bool RetrievePattern ()
            {
                if (!SafeNativeMethods.IsWindowEnabled (_hwnd) || !SafeNativeMethods.IsWindowVisible (_hwnd) || HeaderIsHidden (_hwnd))
                {
                    return false;
                }

                return true;
            }

            // header item looks and behaives like a push button
            private bool IsPushButton ()
            {
                return (Misc.IsBitSet(WindowStyle, NativeMethods.HDS_BUTTONS));
            }

            // header item looks and behaives like a push button
            private bool IsFilter ()
            {
                return (Misc.IsBitSet(WindowStyle, NativeMethods.HDS_FILTERBAR));
            }

            // retrieve a point which will invoke the
            // headeritem
            private bool GetInvokationPoint (out NativeMethods.Win32Point pt)
            {
                if (!GetClickablePoint(out pt, false))
                {
                    //If there is no clickable point, there is no use of calling MapWindowPoints
                    return false;
                }

                // Map to client
                return Misc.MapWindowPoints(IntPtr.Zero, _hwnd, ref pt, 1);
            }

            // This is new with v6 comctrl on Vista
            private bool IsSplitButton ()
            {
                NativeMethods.HDITEM item = new NativeMethods.HDITEM();
                item.Init();
                item.mask = NativeMethods.HDI_FORMAT;

                // Send the message...
                if (XSendMessage.GetItem(_hwnd, _item, ref item))
                {
                    if ((item.fmt & NativeMethods.HDF_SPLITBUTTON) != 0)
                    {
                        return true;
                    }
                }

                return false;
            }

            // This is new with v6 comctrl on Vista
            private bool IsItemFocused ()
            {
                int item = Misc.ProxySendMessageInt(_hwnd, NativeMethods.HDM_GETFOCUSEDITEM, IntPtr.Zero, IntPtr.Zero);
                if (item == _item)
                {
                    return true;
                }

                return false;
            }



            // This is new with v6 comctrl on Vista
            private void ClickSplitButton ()
            {
                // Make sure that the control is enabled
                if (!SafeNativeMethods.IsWindowEnabled(_hwnd))
                {
                    throw new ElementNotEnabledException();
                }

                WindowsSysHeader parent = _parent as WindowsSysHeader;
                if (parent != null)
                {
                    parent.ScrollIntoView(this);
                }


                Rect rect = XSendMessage.GetItemRect(_hwnd, NativeMethods.HDM_GETITEMDROPDOWNRECT, _item);
                NativeMethods.Win32Rect rectW32 = new NativeMethods.Win32Rect(rect);
                IntPtr center = NativeMethods.Util.MAKELPARAM (rectW32.left + ((rectW32.right - rectW32.left) / 2), rectW32.top + ((rectW32.bottom - rectW32.top) / 2));

                // click
                // Header item's split button's DoDefaultAction 'click' is broken
                // the DoDefaultAction code sends a the WM_LBUTTONDOWN message just like below but does not do anything.
                Misc.ProxySendMessage(_hwnd, NativeMethods.WM_LBUTTONDOWN, new IntPtr(NativeMethods.MK_LBUTTON), center);
                Misc.ProxySendMessage(_hwnd, NativeMethods.WM_LBUTTONUP, IntPtr.Zero, center);
            }

            #endregion Private Methods

            private bool IsExpanded()
            {
                // if the header has focus and the headitem has focus and we can find the dropdown window
                // then it must be accociated the this item because only on of these can exist at a time.
                if (Misc.GetFocusedWindow().Equals(_hwnd))
                {
                    // if this window does not exist it can't be expaned because this is the dropdown window.
                    IntPtr hwndDropDown = Misc.FindWindowEx(IntPtr.Zero, IntPtr.Zero, "DropDown", null);
                    if (hwndDropDown != IntPtr.Zero)
                    {
                        if (IsItemFocused())
                        {
                            return true;
                        }
                    }
                }

                return false;
            }
            #endregion


        }
    }
}
