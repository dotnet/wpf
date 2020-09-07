// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/***************************************************************************\
*
*
* Description:
* HWND-based tab control proxy
*
*
\***************************************************************************/

using System;
using System.Collections;
using System.Text;
using System.Windows.Automation;
using System.Windows.Automation.Provider;
using System.Runtime.InteropServices;
using System.ComponentModel;
using System.Windows;
using System.Windows.Input;
using MS.Win32;
using NativeMethodsSetLastError = MS.Internal.UIAutomationClientSideProviders.NativeMethodsSetLastError;

namespace MS.Internal.AutomationProxies
{
    class WindowsTab: ProxyHwnd, ISelectionProvider, IScrollProvider, IRawElementProviderHwndOverride
    {
        // ------------------------------------------------------
        //
        // Constructors
        //
        // ------------------------------------------------------

        #region Constructors

        public WindowsTab (IntPtr hwnd, ProxyFragment parent, int item)
            : base( hwnd, parent, item )
        {
            // Set the strings to return properly the properties.
            _cControlType = ControlType.Tab;

            // force initialisation of this so it can be used later
            _windowsForms = WindowsFormsHelper.GetControlState (hwnd);

            // support for events
            _createOnEvent = new WinEventTracker.ProxyRaiseEvents (RaiseEvents);

            _fIsContent = IsValidControl(_hwnd);
        }

        static WindowsTab ()
        {
            _upDownEvents = new WinEventTracker.EvtIdProperty [1];
            _upDownEvents[0]._evtId = NativeMethods.EventObjectValueChange;
            _upDownEvents[0]._idProp = ScrollPattern.HorizontalScrollPercentProperty;
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
            WindowsTab wTab = new WindowsTab(hwnd, null, 0);

            return idChild == 0 ? wTab : wTab.CreateTabItem (idChild - 1);
        }

        // Static Create method called by the event tracker system
        // WinEvents are one throwns because items exist. so it makes sense to create the item and
        // check for details afterward.
        internal static void RaiseEvents (IntPtr hwnd, int eventId, object idProp, int idObject, int idChild)
        {
            ProxySimple el = null;

            switch (idObject)
            {
                case NativeMethods.OBJID_CLIENT :
                {
                    WindowsTab wlv = new WindowsTab (hwnd, null, -1);

                    if (eventId == NativeMethods.EventObjectSelection || eventId == NativeMethods.EventObjectSelectionRemove || eventId == NativeMethods.EventObjectSelectionAdd)
                    {
                        el = wlv.CreateTabItem (idChild - 1);
                    }
                    else
                    {
                        el = wlv;
                    }

                    break;
                }

                default :
                    if ((idProp == ScrollPattern.VerticalScrollPercentProperty && idObject != NativeMethods.OBJID_VSCROLL) ||
                        (idProp == ScrollPattern.HorizontalScrollPercentProperty && idObject != NativeMethods.OBJID_HSCROLL))
                    {
                        return;
                    }

                    el = new WindowsTab(hwnd, null, -1);
                    break;
            }
            if (el != null)
            {
                el.DispatchEvents (eventId, idProp, idObject, idChild);
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
        // Param name="iid", UIAutomation Pattern
        // Returns null or pattern interface
        internal override object GetPatternProvider (AutomationPattern iid)
        {
            if (iid == SelectionPattern.Pattern)
            {
                return this;
            }

            if (iid == ScrollPattern.Pattern)
            {
                return this;
            }

            return null;
        }

        // Process all the Logical and Raw Element Properties
        internal override object GetElementProperty(AutomationProperty idProp)
        {
            if (idProp == AutomationElement.IsControlElementProperty)
            {
                return IsValidControl(_hwnd);
            }
            else if (idProp == AutomationElement.OrientationProperty)
            {
                return IsVerticalTab() ? OrientationType.Vertical : OrientationType.Horizontal;
            }
            else if (idProp == ScrollPatternIdentifiers.HorizontalScrollPercentProperty)
            {
                return ((IScrollProvider)this).HorizontalScrollPercent;
            }
            else if (idProp == ScrollPatternIdentifiers.HorizontallyScrollableProperty)
            {
                return ((IScrollProvider)this).HorizontallyScrollable;
            }
            else if (idProp == ScrollPatternIdentifiers.HorizontalViewSizeProperty)
            {
                return ((IScrollProvider)this).HorizontalViewSize;
            }

            return base.GetElementProperty(idProp);
        }

        #endregion ProxySimple Interface

        #region ProxyFragment Interface

        // Returns the next sibling element in the raw hierarchy.
        // Peripheral controls have always negative values.
        // Param name="child", the current child
        // Returns null if no next child
        internal override ProxySimple GetNextSibling (ProxySimple child)
        {
            int item = child._item;

            if (item != SpinControl)
            {
                int count = GetItemCount(_hwnd);

                // Next for an item that does not exist in the list
                if (item >= count)
                {
                    throw new ElementNotAvailableException ();
                }

                if (item + 1 < count)
                {
                    return CreateTabItem(item + 1);
                }
            }

            return null;
        }

        // Returns the previous sibling element in the raw hierarchy.
        // Peripheral controls have always negative values.
        // Param name="child", the current child
        // Returns null is no previous
        internal override ProxySimple GetPreviousSibling (ProxySimple child)
        {
            int count = GetItemCount(_hwnd);
            int item = child._item;

            if (item == SpinControl)
                item = count;

            // Next for an item that does not exist in the list
            if (item >= count)
            {
                throw new ElementNotAvailableException ();
            }

            if (item > 0 && item <= count)
            {
                return CreateTabItem(item - 1);
            }

            return null;
        }

        // Returns the first child element in the raw hierarchy.
        internal override ProxySimple GetFirstChild ()
        {
            int count = GetItemCount(_hwnd);

            if (count > 0)
            {
                return CreateTabItem(0);
            }

            return null;
        }

        // Returns the last child element in the raw hierarchy.
        internal override ProxySimple GetLastChild ()
        {
            int count = GetItemCount(_hwnd);

            if (count > 0)
            {
                return CreateTabItem(count - 1);
            }

            return null;
        }

        // Returns a Proxy element corresponding to the specified screen coordinates.
        internal override ProxySimple ElementProviderFromPoint (int x, int y)
        {
            UnsafeNativeMethods.TCHITTESTINFO hti = new UnsafeNativeMethods.TCHITTESTINFO();

            hti.pt = new NativeMethods.Win32Point (x, y);

            if (!Misc.MapWindowPoints(IntPtr.Zero, _hwnd, ref hti.pt, 1))
            {
                return null;
            }

            // updown control goes over the tabs hence the order of the check
            // We cannot let UIAutomation do the do the drilling for the updown as the spinner covers the tab
            IntPtr updownHwnd = this.GetUpDownHwnd ();

            if (updownHwnd != IntPtr.Zero && Misc.PtInWindowRect(updownHwnd, x, y))
            {
                return null;
            }

            int index;
            unsafe
            {
                index = XSendMessage.XSendGetIndex(_hwnd, NativeMethods.TCM_HITTEST, IntPtr.Zero, new IntPtr(&hti), Marshal.SizeOf(hti.GetType()));
            }

            if (index >= 0)
            {
                return CreateTabItem (index);
            }

            return null;
        }

        // Returns an item corresponding to the focused element (if there is one), or null otherwise.
        internal override ProxySimple GetFocus ()
        {
            int focusIndex = Misc.ProxySendMessageInt(_hwnd, NativeMethods.TCM_GETCURFOCUS, IntPtr.Zero, IntPtr.Zero);

            if (focusIndex >= 0 && focusIndex < GetItemCount(_hwnd))
            {
                return CreateTabItem (focusIndex);
            }
            else
            {
                return null;
            }
        }

        #endregion

        #region ProxyHwnd Interface

        internal override void AdviseEventAdded(
            AutomationEvent eventId, AutomationProperty[] aidProps)
        {
            if (eventId == AutomationElementIdentifiers.AutomationPropertyChangedEvent
                && aidProps.Length > 0
                && aidProps[0] == ScrollPatternIdentifiers.HorizontalScrollPercentProperty)
            {
                IntPtr upDownHwnd = GetUpDownHwnd();
                if (upDownHwnd != IntPtr.Zero)
                {
                    // Register for UpDown ValueChange WinEvents, which will be
                    // translated to scrolling events for the tab control.
                    WinEventTracker.AddToNotificationList(
                        upDownHwnd,
                        new WinEventTracker.ProxyRaiseEvents(UpDownControlRaiseEvents),
                        _upDownEvents, 1);
                }
            }

            base.AdviseEventAdded(eventId, aidProps);
        }

        internal override void AdviseEventRemoved(
            AutomationEvent eventId, AutomationProperty[] aidProps)
        {
            if (eventId == AutomationElementIdentifiers.AutomationPropertyChangedEvent
                && aidProps.Length > 0
                && aidProps[0] == ScrollPatternIdentifiers.HorizontalScrollPercentProperty)
            {
                IntPtr upDownHwnd = GetUpDownHwnd();
                if (upDownHwnd != IntPtr.Zero)
                {
                    WinEventTracker.RemoveToNotificationList(
                        upDownHwnd, _upDownEvents, null, 1);
                }
            }
            base.AdviseEventRemoved(eventId, aidProps);
        }

        #endregion ProxyHwnd Interface

        #region IRawElementProviderHwndOverride Interface

        //------------------------------------------------------
        //
        //  Interface IRawElementProviderHwndOverride
        //
        //------------------------------------------------------
        IRawElementProviderSimple IRawElementProviderHwndOverride.GetOverrideProviderForHwnd (IntPtr hwnd)
        {
            // return the appropriate placeholder for the given hwnd...
            // loop over all the tabs to find it.

            string sTitle = Misc.ProxyGetText(hwnd);

            // If there is no hwnd title there is no way to match to the tab item.
            if (string.IsNullOrEmpty(sTitle))
            {
                return null;
            }

            for (int i = 0, c = GetItemCount(_hwnd); i < c; i++)
            {
                if (sTitle == WindowsTabItem.GetName(_hwnd, i, true))
                {
                    return new WindowsTabChildOverrideProxy(hwnd, CreateTabItem(i), i);
                }
            }

            return null;
        }

        #endregion IRawElementProviderHwndOverride Interface

        #region Selection Pattern

        // Returns an enumerator over the current selection.
        IRawElementProviderSimple[] ISelectionProvider.GetSelection()
        {
            IRawElementProviderSimple[] selection = null;

            // If only one selection allowed, get selected item, if any, and add to list
            if (!WindowsTab.SupportMultipleSelection (_hwnd))
            {
                int selectedItem = WindowsTabItem.GetCurrentSelectedItem(_hwnd);

                if (selectedItem >= 0)
                {
                    selection = new IRawElementProviderSimple[1];
                    selection[0] = CreateTabItem(selectedItem);
                }
            }

            // If multiple selections allowed, check each tab for selected state
                else
            {
                ArrayList list = new ArrayList();
                for (ProxySimple child = GetFirstChild(); child != null; child = GetNextSibling(child))
                {
                    if (((ISelectionItemProvider) child).IsSelected)
                    {
                        list.Add (child);
                    }
                }

                int count = list.Count;
                if (count <= 0)
                {
                    return null;
                }

                selection = new IRawElementProviderSimple[count];
                for (int i = 0; i < count; i++)
                {
                    selection[i] = (ProxySimple)list[i];
                }
            }

            return selection;
        }

        // Returns whether the control supports multiple selection.
        bool ISelectionProvider.CanSelectMultiple
        {
            get
            {
                return SupportMultipleSelection (_hwnd);
            }
        }

        // Returns whether the control requires a minimum of one selected element at all times.
        bool ISelectionProvider.IsSelectionRequired
        {
            get
            {
                return !Misc.IsBitSet(WindowStyle, NativeMethods.TCS_BUTTONS);
            }
        }

        #endregion ISelectionProvider

        #region Scroll Pattern

        void IScrollProvider.Scroll (ScrollAmount horizontalAmount, ScrollAmount verticalAmount)
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

            if (verticalAmount != ScrollAmount.NoAmount)
            {
                throw new InvalidOperationException(SR.Get(SRID.OperationCannotBePerformed));
            }

            if (!Scroll(horizontalAmount))
            {
                throw new InvalidOperationException(SR.Get(SRID.OperationCannotBePerformed));
            }
        }

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

            // Get up/down control's hwnd
            IntPtr updownHwnd = this.GetUpDownHwnd ();

            if (updownHwnd == IntPtr.Zero)
            {
                throw new InvalidOperationException(SR.Get(SRID.OperationCannotBePerformed));
            }

            // Get available range
            int range = Misc.ProxySendMessageInt(updownHwnd, NativeMethods.UDM_GETRANGE, IntPtr.Zero, IntPtr.Zero);
            int minPos = NativeMethods.Util.HIWORD(range);
            int maxPos = NativeMethods.Util.LOWORD(range);

            // Calculate new position
            int newPos = (int) Math.Round ((maxPos - minPos) * horizontalPercent / 100) + minPos;

            // Set position
            Misc.ProxySendMessage(updownHwnd, NativeMethods.UDM_SETPOS, IntPtr.Zero, (IntPtr)newPos);
            Misc.ProxySendMessage(_hwnd, NativeMethods.WM_HSCROLL, (IntPtr)NativeMethods.Util.MAKELPARAM(NativeMethods.SB_THUMBPOSITION, newPos), IntPtr.Zero);
        }


        // Calc the position of the horizontal scroll bar thumb in the 0..100 % range
        double IScrollProvider.HorizontalScrollPercent
        {
            get
            {
                double minPos, maxPos, currentPos;

                // Get up/down control's hwnd
                IntPtr updownHwnd = this.GetUpDownHwnd ();

                if (updownHwnd == IntPtr.Zero)
                {
                    return (double)ScrollPattern.NoScroll;
                }

                // Get range of position values
                int range = Misc.ProxySendMessageInt(updownHwnd, NativeMethods.UDM_GETRANGE, IntPtr.Zero, IntPtr.Zero);

                // Get current position
                int posResult = Misc.ProxySendMessageInt(updownHwnd, NativeMethods.UDM_GETPOS, IntPtr.Zero, IntPtr.Zero);

                // Calculate percentage position
                minPos = NativeMethods.Util.HIWORD(range);
                maxPos = NativeMethods.Util.LOWORD(range);
                currentPos = NativeMethods.Util.LOWORD(posResult);
                return (currentPos - minPos) / (maxPos - minPos) * 100;
            }
        }

        // Calc the position of the Vertical scroll bar thumb in the 0..100 % range
        double IScrollProvider.VerticalScrollPercent
        {
            get
            {
                // Tab controls can never be vertically scrolling
                // since vertical tab controls must have the multiline style
                return (double)ScrollPattern.NoScroll;
            }
        }

        // Percentage of the window that is visible along the horizontal axis.
        // Value 0..100
        double IScrollProvider.HorizontalViewSize
        {
            get
            {
                ProxySimple firstChild = GetFirstChild ();
                ProxySimple lastChild = GetLastChild ();

                // Get rectangles
                Rect firstRect = firstChild.BoundingRectangle;
                Rect lastRect = lastChild.BoundingRectangle;
                NativeMethods.Win32Rect viewable = new NativeMethods.Win32Rect ();

                viewable.left = 0;
                if (!Misc.GetWindowRect(_hwnd, ref viewable))
                {
                    return 100.0;
                }

                // Calculate ranges
                double totalRange = (double)lastRect.Right - (double)firstRect.Left;
                double viewableRange = viewable.right - viewable.left;

                // Get the rectangle of the up/down control and adjust viewable range
                IntPtr updownHwnd = this.GetUpDownHwnd ();

                if (updownHwnd == IntPtr.Zero)
                {
                    return 100.0;
                }

                NativeMethods.Win32Rect rectW32 = new NativeMethods.Win32Rect ();

                if (!Misc.GetWindowRect(updownHwnd, ref rectW32))
                {
                    return 100.0;
                }

                viewableRange -= rectW32.right - rectW32.left;
                return viewableRange / totalRange * 100;
            }
        }

        // Percentage of the window that is visible along the vertical axis.
        // Value 0..100
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
                return IsScrollable();
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

        #endregion IScrollProvider

        // ------------------------------------------------------
        //
        // Internal Methods
        //
        // ------------------------------------------------------

        #region Internal Methods

        internal static int GetItemCount(IntPtr hwnd)
        {
            // The Display Property Dialog is doing something strange with the their tab control.  The
            // last tab is invisable. So if that is the case remove it from the count, since UIAutomation
            // can not do anything with it.
            int count = Misc.ProxySendMessageInt(hwnd, NativeMethods.TCM_GETITEMCOUNT, IntPtr.Zero, IntPtr.Zero);

            if (count > 0)
            {
                NativeMethods.Win32Rect rectW32 = NativeMethods.Win32Rect.Empty;
                bool result;
                unsafe
                {
                    result = XSendMessage.XSend(hwnd, NativeMethods.TCM_GETITEMRECT, new IntPtr(count - 1), new IntPtr(&rectW32), Marshal.SizeOf(rectW32.GetType()), XSendMessage.ErrorValue.Zero);
                }
                if (!result)
                {
                    count--;
                }
                if (rectW32.IsEmpty)
                {
                    count--;
                }
            }

            return count;
        }

        // Create a WindowsTab instance
        internal ProxyFragment CreateTabItem(int index)
        {
            return new WindowsTabItem(_hwnd, this, index, _windowsForms == WindowsFormsHelper.FormControlState.True);
        }

        internal void ScrollToItem(int index)
        {
            if (!IsScrollable())
            {
                throw new InvalidOperationException(SR.Get(SRID.OperationCannotBePerformed));
            }

            // Get up/down control's hwnd
            IntPtr updownHwnd = this.GetUpDownHwnd();

            if (updownHwnd == IntPtr.Zero)
                return;

            int range = Misc.ProxySendMessageInt(updownHwnd, NativeMethods.UDM_GETRANGE, IntPtr.Zero, IntPtr.Zero);
            int max = NativeMethods.Util.LOWORD(range);
            int newPos = index < max ? index : max;

            Misc.ProxySendMessage(updownHwnd, NativeMethods.UDM_SETPOS, IntPtr.Zero, (IntPtr)newPos);
            Misc.ProxySendMessage(_hwnd, NativeMethods.WM_HSCROLL, NativeMethods.Util.MAKELPARAM(NativeMethods.SB_THUMBPOSITION, newPos), IntPtr.Zero);
        }

        internal bool IsScrollable()
        {
            return GetUpDownHwnd(_hwnd) != IntPtr.Zero;
        }

        // if all the tab items have no name then the control is not useful
        internal static bool IsValidControl(IntPtr hwnd)
        {
            for (int i = 0, c = GetItemCount(hwnd); i < c; i++)
            {
                if (!string.IsNullOrEmpty(WindowsTabItem.GetName(hwnd, i, true)))
                {
                    return true;
                }
            }

            return false;
        }

        // Process events for the associated UpDown control, and relay
        // them to the WindowsTab control instead.
        internal static void UpDownControlRaiseEvents(
            IntPtr hwnd, int eventId, object idProp, int idObject, int idChild)
        {
            if (eventId == NativeMethods.EventObjectValueChange
                && idProp == ScrollPattern.HorizontalScrollPercentProperty)
            {
                IntPtr hwndParent = NativeMethodsSetLastError.GetAncestor(hwnd, NativeMethods.GA_PARENT);
                if (hwndParent != IntPtr.Zero
                   && Misc.ProxyGetClassName(hwndParent).Contains("SysTabControl32"))
                {
                    WindowsTab el = new WindowsTab(hwndParent, null, 0);
                    el.DispatchEvents(eventId, idProp, 0, 0);
                }
            }
        }

        #endregion Internal Methods

        // ------------------------------------------------------
        //
        // Private Methods
        //
        // ------------------------------------------------------

        #region Private Methods

        // Gets the windows handle of the UpDown control in the tab control
        // Returns the handle to the UpDown control or IntPtr.Zero if this tab control isn't scrollable.
        private IntPtr GetUpDownHwnd()
        {
            return GetUpDownHwnd(_hwnd);
        }
        private static IntPtr GetUpDownHwnd(IntPtr hwnd)
        {
            IntPtr childHwnd = Misc.GetWindow(hwnd, NativeMethods.GW_CHILD);
            string className;
            int i;

            // UpDown control is either the first or last child, so do this check twice
            for (i = 0; i < 2; i++)
            {
                if (childHwnd != IntPtr.Zero)
                {
                    className = Misc.ProxyGetClassName(childHwnd);
                    if (className.IndexOf("updown", StringComparison.Ordinal) > -1)
                    {
                        // found it
                        return childHwnd;
                    }

                    childHwnd = Misc.GetWindow(childHwnd, NativeMethods.GW_HWNDLAST);
                }
                else
                {
                    // didn't find it
                    break;
                }
            }

            return IntPtr.Zero;
        }

        private bool IsVerticalTab()
        {
            int style = WindowStyle;
            return Misc.IsBitSet(style, NativeMethods.TCS_MULTILINE) &&
                    (Misc.IsBitSet(style, NativeMethods.TCS_RIGHT) ||
                     Misc.IsBitSet(style, NativeMethods.TCS_VERTICAL));
        }

        #endregion Private Methods

        #region Scroll Helper

        private bool Scroll(ScrollAmount amount)
        {
            // Done
            if (amount == ScrollAmount.NoAmount)
            {
                return true;
            }

            // Get up/down control's hwnd
            IntPtr updownHwnd = this.GetUpDownHwnd ();

            if (updownHwnd == IntPtr.Zero)
            {
                return false;
            }

            // Do we need to send UDN_DELTAPOS notification to get permission to scroll?
            // Set position
            int newPos = Misc.ProxySendMessageInt(updownHwnd, NativeMethods.UDM_GETPOS, IntPtr.Zero, IntPtr.Zero);
            int range = Misc.ProxySendMessageInt(updownHwnd, NativeMethods.UDM_GETRANGE, IntPtr.Zero, IntPtr.Zero);
            int max = NativeMethods.Util.LOWORD(range);
            int min = NativeMethods.Util.HIWORD(range);

            if (NativeMethods.Util.HIWORD (newPos) == 0)
            {
                throw new InvalidOperationException(SR.Get(SRID.OperationCannotBePerformed));
            }

            newPos = NativeMethods.Util.LOWORD (newPos);
            switch (amount)
            {
                case ScrollAmount.LargeDecrement :
                case ScrollAmount.LargeIncrement :
                    // Not supported.
                    return false;

                case ScrollAmount.SmallDecrement :
                    newPos--;
                    break;

                case ScrollAmount.SmallIncrement :
                    newPos++;
                    break;

                default :  // should never get here
                    return false;
            }

            if (newPos < min || newPos > max)
            {
                // Attempt to scroll before beginning or past end.
                // As long as this is a supported operation (namely,
                // SmallIncrement or SmallDecrement), do nothing but
                // return success.
                return true;
            }

            // Update both the spiner and the tabs
            Misc.ProxySendMessage(updownHwnd, NativeMethods.UDM_SETPOS, IntPtr.Zero, (IntPtr)newPos);
            Misc.ProxySendMessage(_hwnd, NativeMethods.WM_HSCROLL, NativeMethods.Util.MAKELPARAM(NativeMethods.SB_THUMBPOSITION, newPos), IntPtr.Zero);

            return true;
        }

        #endregion

        #region Selection Helper

        // detect if tab-control supports multiple selection
        static internal bool SupportMultipleSelection (IntPtr hwnd)
        {
            return Misc.IsBitSet(Misc.GetWindowStyle(hwnd), (NativeMethods.TCS_BUTTONS | NativeMethods.TCS_MULTISELECT));
        }

        #endregion

        // ------------------------------------------------------
        //
        // Private Fields
        //
        // ------------------------------------------------------

        #region Private Fields

        private const int SpinControl = -2;

        // Updown specific events.
        private readonly static WinEventTracker.EvtIdProperty[] _upDownEvents;

        #endregion
    }

    // ------------------------------------------------------
    //
    // WindowsTabItem Private Class
    //
    // ------------------------------------------------------

    #region WindowsTabItem

    class WindowsTabItem : ProxyFragment, ISelectionItemProvider, IScrollItemProvider
    {
        // ------------------------------------------------------
        //
        // Constructors
        //
        // ------------------------------------------------------

        #region Constructors

        internal WindowsTabItem(IntPtr hwnd, ProxyFragment parent, int item, bool fIsWinform)
            : base( hwnd, parent, item )
        {
            // Set the strings to return properly the properties.
            _cControlType = ControlType.TabItem;
            _fIsKeyboardFocusable = true;

            _fIsWinform = fIsWinform;
            _fIsContent = !string.IsNullOrEmpty(GetName(_hwnd, _item, true));
        }

        #endregion

        // ------------------------------------------------------
        //
        //  Patterns Implementation
        //
        // ------------------------------------------------------

        #region ProxySimple Interface

        // Returns a pattern interface if supported.
        internal override object GetPatternProvider(AutomationPattern iid)
        {
            if(iid == SelectionItemPattern.Pattern)
            {
                return this;
            }
            else if (iid == ScrollItemPattern.Pattern)
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
                // Don't need to normalize, BoundingRect returns absolute coordinates.
                return BoundingRect().ToRect(false);
            }
        }

        // Process all the Logical and Raw Element Properties
        internal override object GetElementProperty(AutomationProperty idProp)
        {
            if (idProp == AutomationElement.AccessKeyProperty && _windowsForms != WindowsFormsHelper.FormControlState.True)
            {
                return Misc.AccessKey(WindowsTabItem.GetItemText(_hwnd, _item));
            }
            else if (idProp == AutomationElement.IsControlElementProperty)
            {
                return !string.IsNullOrEmpty(GetName(_hwnd, _item, true));
            }

            return base.GetElementProperty(idProp);
        }

        //Gets the controls help text
        internal override string HelpText
        {
            get
            {
                IntPtr hwndToolTip = Misc.ProxySendMessage(_hwnd, NativeMethods.TCM_GETTOOLTIPS, IntPtr.Zero, IntPtr.Zero);
                return Misc.GetItemToolTipText(_hwnd, hwndToolTip, _item);
            }
        }

        //Gets the localized name
        internal override string LocalizedName
        {
            get
            {
                // If this is a winforms tab page and the AccessibleName is set, use it.
                if (WindowsFormsHelper.IsWindowsFormsControl(_hwnd, ref _windowsForms))
                {
                    string name = GetAccessibleName(_item + 1);
                    if (!string.IsNullOrEmpty(name))
                    {
                        return name;
                    }
                }

                return GetName(_hwnd, _item, _windowsForms == WindowsFormsHelper.FormControlState.True);
            }
        }

        // Sets the focus to this item.
        internal override bool SetFocus()
        {
            if (Misc.IsBitSet(WindowStyle, NativeMethods.TCS_FOCUSNEVER))
            {
                return false;
            }

            WindowsTab tab = (WindowsTab)_parent;
            ProxySimple focused = tab.GetFocus();

            if (focused == null || _item != focused._item)
            {
                Misc.ProxySendMessage(_hwnd, NativeMethods.TCM_SETCURFOCUS, new IntPtr(_item), IntPtr.Zero);
            }

            return true;
        }

        #endregion ProxySimple Interface

        #region ProxyFragment Interface

        // Returns the next sibling element in the raw hierarchy.
        // Peripheral controls have always negative values.
        // Param name="child", the current child
        // Returns null if no next child
        internal override ProxySimple GetNextSibling(ProxySimple child)
        {
            return null;
        }

        // Returns the previous sibling element in the raw hierarchy.
        // Peripheral controls have always negative values.
        // Param name="child", the current child
        // Returns null is no previous
        internal override ProxySimple GetPreviousSibling(ProxySimple child)
        {
            return null;
        }

        // Returns the first child element in the raw hierarchy.
        internal override ProxySimple GetFirstChild()
        {
            IntPtr hwndChild = GetItemHwndByIndex();
            if (hwndChild != IntPtr.Zero && SafeNativeMethods.IsWindowVisible(hwndChild))
            {
                return new WindowsTabChildOverrideProxy(hwndChild, this, _item);
            }

            return null;
        }

        // Returns the last child element in the raw hierarchy.
        internal override ProxySimple GetLastChild()
        {
            // One children at most for form or nothing for Win32 controls.
            return GetFirstChild();
        }

        #endregion ProxyFragment Interface

        #region Selection Pattern

        // Selects this element
        void ISelectionItemProvider.Select()
        {
            // Make sure that the control is enabled
            if (!SafeNativeMethods.IsWindowEnabled(_hwnd))
            {
                throw new ElementNotEnabledException();
            }

            if (!IsSelectable())
            {
                throw new InvalidOperationException(SR.Get(SRID.OperationCannotBePerformed));
            }

            if (((ISelectionItemProvider)this).IsSelected == false)
            {
                Select();
            }
        }

        // Adds this element to the selection
        void ISelectionItemProvider.AddToSelection()
        {
            // Make sure that the control is enabled
            if (!SafeNativeMethods.IsWindowEnabled(_hwnd))
            {
                throw new ElementNotEnabledException();
            }

            // If not selectable, can't add to selection
            if (!IsSelectable())
            {
                throw new InvalidOperationException(SR.Get(SRID.OperationCannotBePerformed));
            }

            // If already selected, done
            if (((ISelectionItemProvider)this).IsSelected)
            {
                return;
            }

            // If multiple selections allowed, add requested selection
            if (WindowsTab.SupportMultipleSelection(_hwnd) == true)
            {
                // Press ctrl and mouse click tab
                NativeMethods.Win32Point pt = new NativeMethods.Win32Point();
                if (GetClickablePoint(out pt, true))
                {
                    Input.SendKeyboardInput(Key.LeftCtrl, true);
                    Misc.MouseClick(pt.x, pt.y);
                    Input.SendKeyboardInput(Key.LeftCtrl, false);
                    return;
                }

                throw new InvalidOperationException(SR.Get(SRID.OperationCannotBePerformed));
            }
            // else only single selection allowed
            else
            {
                throw new InvalidOperationException(SR.Get(SRID.DoesNotSupportMultipleSelection));
            }
        }

        // Removes this element from the selection
        void ISelectionItemProvider.RemoveFromSelection()
        {
            // Make sure that the control is enabled
            if (!SafeNativeMethods.IsWindowEnabled(_hwnd))
            {
                throw new ElementNotEnabledException();
            }

            // If not selected, done
            if (!((ISelectionItemProvider)this).IsSelected)
            {
                return;
            }

            // If multiple selections allowed, unselect element
            if (WindowsTab.SupportMultipleSelection(_hwnd) == true)
            {
                NativeMethods.Win32Point pt = new NativeMethods.Win32Point();
                if (GetClickablePoint(out pt, true))
                {
                    Input.SendKeyboardInput(Key.LeftCtrl, true);
                    Misc.MouseClick(pt.x, pt.y);
                    Input.SendKeyboardInput(Key.LeftCtrl, false);
                    return;
                }

                throw new InvalidOperationException(SR.Get(SRID.OperationCannotBePerformed));
            }
            // else if button style and single select, send deselectall message
            else if (Misc.IsBitSet(WindowStyle, NativeMethods.TCS_BUTTONS))
            {
                Misc.ProxySendMessage(_hwnd, NativeMethods.TCM_DESELECTALL, IntPtr.Zero, IntPtr.Zero);
                return;
            }

            throw new InvalidOperationException(SR.Get(SRID.OperationCannotBePerformed));
        }

        // True if this element is part of the the selection
        bool ISelectionItemProvider.IsSelected
        {
            get
            {
                // If only a single selection is allowed, check which one is selected and compare
                if (!WindowsTab.SupportMultipleSelection(_hwnd))
                {
                    int selectedItem = GetCurrentSelectedItem(_hwnd);

                    return (_item == selectedItem);
                }

                // If multiple selections possible, get state information on the tab
                else
                {
                    NativeMethods.TCITEM TCItem = new NativeMethods.TCITEM();
                    TCItem.Init(NativeMethods.TCIF_STATE);

                    if (!XSendMessage.GetItem(_hwnd, _item, ref TCItem))
                    {
                        System.Diagnostics.Debug.Assert(false, "XSendMessage.GetItem() failed!");
                        return false;
                    }

                    return Misc.IsBitSet(TCItem.dwState, NativeMethods.TCIS_BUTTONPRESSED);
                }
            }
        }

        // Returns the container for this element
        IRawElementProviderSimple ISelectionItemProvider.SelectionContainer
        {
            get
            {
                System.Diagnostics.Debug.Assert(_parent is WindowsTab, "Invalid Parent for a Tab Item");
                return _parent;
            }
        }

        #endregion SelectionItem Pattern

        #region ScrollItem Pattern

        void IScrollItemProvider.ScrollIntoView()
        {
            // Make sure that the control is enabled
            if (!SafeNativeMethods.IsWindowEnabled(_hwnd))
            {
                throw new ElementNotEnabledException();
            }

            WindowsTab parent = (WindowsTab)_parent;

            if (!parent.IsScrollable())
            {
                throw new InvalidOperationException(SR.Get(SRID.OperationCannotBePerformed));
            }

            parent.ScrollToItem(_item);
        }

        #endregion ScrollItem Pattern

        // ------------------------------------------------------
        //
        // Internal Methods
        //
        // ------------------------------------------------------

        #region Internal Methods

        // Retrieve the text in a tab item
        internal static string GetName(IntPtr hwnd, int item, bool fIsWinform)
        {
            string sName = GetItemText(hwnd, item);

            // Win32 controls '&' is used as an accelerator. This is not the case for winforms !!!
            return !fIsWinform ? Misc.StripMnemonic(sName) : sName;
        }

        internal static int GetCurrentSelectedItem(IntPtr hwnd)
        {
            return Misc.ProxySendMessageInt(hwnd, NativeMethods.TCM_GETCURSEL, IntPtr.Zero, IntPtr.Zero);
        }

        #endregion Internal Methods

        //------------------------------------------------------
        //
        //  Protected Methods
        //
        //------------------------------------------------------

        #region Protected Methods

        // This routine is only called on elements belonging to an hwnd that has the focus.
        protected override bool IsFocused()
        {
            return Misc.ProxySendMessageInt(_hwnd, NativeMethods.TCM_GETCURFOCUS, IntPtr.Zero, IntPtr.Zero) == _item;
        }

        #endregion Protected Methods

        // ------------------------------------------------------
        //
        // Private Methods
        //
        // ------------------------------------------------------

        #region Private Methods

        private unsafe NativeMethods.Win32Rect BoundingRect()
        {
            NativeMethods.Win32Rect rectW32 = new NativeMethods.Win32Rect();

            if (!XSendMessage.XSend(_hwnd, NativeMethods.TCM_GETITEMRECT, new IntPtr(_item), new IntPtr(&rectW32), Marshal.SizeOf(rectW32.GetType()), XSendMessage.ErrorValue.Zero))
            {
                return NativeMethods.Win32Rect.Empty;
            }

            return Misc.MapWindowPoints(_hwnd, IntPtr.Zero, ref rectW32, 2) ? rectW32 : NativeMethods.Win32Rect.Empty;
        }

        private bool IsSelectable()
        {
            return (SafeNativeMethods.IsWindowEnabled(_hwnd) && SafeNativeMethods.IsWindowVisible(_hwnd));
        }

        // Press a tab
        private void Select()
        {
            if (Misc.IsBitSet(WindowStyle, (NativeMethods.TCS_BUTTONS | NativeMethods.TCS_FOCUSNEVER)))
            {
                // The TCM_SETCURFOCUS message cannot be used with TCS_FOCUSNEVER
                // use a convulated way faking a mouse action

                NativeMethods.Win32Point pt = new NativeMethods.Win32Point();
                if (GetClickablePoint(out pt, true))
                {
                    // Convert screen coordinates to client coordinates.
                    if (Misc.MapWindowPoints(IntPtr.Zero, _hwnd, ref pt, 1))
                    {
                        Misc.PostMessage(_hwnd, NativeMethods.WM_LBUTTONDOWN, (IntPtr)NativeMethods.MK_LBUTTON, NativeMethods.Util.MAKELPARAM(pt.x, pt.y));
                        Misc.PostMessage(_hwnd, NativeMethods.WM_LBUTTONUP, (IntPtr)NativeMethods.MK_LBUTTON, NativeMethods.Util.MAKELPARAM(pt.x, pt.y));
                    }
                }
            }
            else
            {
                Misc.ProxySendMessage(_hwnd, NativeMethods.TCM_SETCURFOCUS, new IntPtr(_item), IntPtr.Zero);
            }

        }

        // Gets the windows handle of an individual tab in a Windows Forms control
        private IntPtr GetItemHwndByIndex()
        {
            // On Win32 Tab controls the table page is parented by the dialog box not the
            // Tab control.
            IntPtr hwndParent = _hwnd;
            if (!_fIsWinform)
            {
                hwndParent = Misc.GetParent(hwndParent);
            }

            if (hwndParent != IntPtr.Zero)
            {
                // Get the tab name and match it with the window title of one of the children
                string sName = WindowsTabItem.GetName(_hwnd, _item, true);

                // If there is no tab name there is no way to match to one of the childrens window title.
                if (!string.IsNullOrEmpty(sName))
                {
                    return Misc.FindWindowEx(hwndParent, IntPtr.Zero, null, sName);
                }
            }

            return IntPtr.Zero;
        }


        private static string GetItemText(IntPtr hwnd, int itemIndex)
        {
            NativeMethods.TCITEM tcitem = new NativeMethods.TCITEM();
            tcitem.Init();

            tcitem.mask = NativeMethods.TCIF_TEXT;
            tcitem.cchTextMax = Misc.MaxLengthNameProperty;

            return XSendMessage.GetItemText(hwnd, itemIndex, tcitem);
        }

        #endregion Private Methods

        // ------------------------------------------------------
        //
        // Private Fields
        //
        // ------------------------------------------------------

        #region Private Fields

        // Cached value for a winform
        bool _fIsWinform;

        #endregion Private Fields
    }

    #endregion

    // ------------------------------------------------------
    //
    //  WindowsTabChildOverrideProxy Class
    //
    //------------------------------------------------------

    #region WindowsTabChildOverrideProxy

    class WindowsTabChildOverrideProxy : ProxyHwnd
    {
        // ------------------------------------------------------
        //
        // Constructors
        //
        // ------------------------------------------------------

        #region Constructors

        // Constructor
        // Usually the hwnd passed to the constructor is wrong. As the base class is doing
        // nothing this does not matter.
        // This avoid to making some extra calls to get this right.
        internal WindowsTabChildOverrideProxy(IntPtr hwnd, ProxyFragment parent, int item)
            : base (hwnd, parent, item)
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
        internal override object GetElementProperty(AutomationProperty idProp)
        {
            if (idProp == AutomationElement.IsControlElementProperty)
            {
                return false;
            }
            // Overrides the ProxySimple implementation to remove and default
            // property handling
            // This proxy is about tree rearranging, it does not do any
            // property overrride.
            return null;
        }

        #endregion
    }

    #endregion
}
