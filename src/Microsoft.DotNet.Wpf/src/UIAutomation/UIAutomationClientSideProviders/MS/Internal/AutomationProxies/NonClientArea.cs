// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/***************************************************************************\
*
*
* Description:
* HWND-based NonClientArea proxy
*
*
\***************************************************************************/

using System;
using System.Collections;
using System.Globalization;
using System.Text;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Automation.Provider;
using System.ComponentModel;
using MS.Win32;


namespace MS.Internal.AutomationProxies
{
    class NonClientArea: ProxyHwnd, IScrollProvider
    {
        #region Constructors

        internal NonClientArea (IntPtr hwnd)
            : base( hwnd, null, 0 )
        {
            _createOnEvent = new WinEventTracker.ProxyRaiseEvents (RaiseEvents);
        }

        #endregion

        #region ProxyHwnd Interface

        internal override void AdviseEventAdded (AutomationEvent eventId, AutomationProperty [] aidProps)
        {
            // we need to create the menu proxy so that the global event for menu will be listened for.
            // This proxy will get advise of events needed for menus
            ProxyHwnd menuProxy = CreateNonClientMenu();
            if (menuProxy == null)
            {
                // If the window does not have a menu, it at least has a system menu.
                WindowsTitleBar titleBar = (WindowsTitleBar)CreateNonClientChild(NonClientItem.TitleBar);
                if (titleBar != null)
                {
                    menuProxy = (ProxyHwnd)titleBar.CreateTitleBarChild(WindowsTitleBar._systemMenu);
                }
            }

            if (menuProxy != null)
            {
                menuProxy.AdviseEventAdded(eventId, aidProps);
            }

            base.AdviseEventAdded(eventId, aidProps);
        }

        internal override void AdviseEventRemoved (AutomationEvent eventId, AutomationProperty [] aidProps)
        {
            // we need to create the menu proxy so that the global event for menu will be listened for.
            // This proxy will get advise of events needed for menus
            ProxyHwnd menuProxy = CreateNonClientMenu();
            if (menuProxy == null)
            {
                // If the window does not have a menu, it at least has a system menu.
                WindowsTitleBar titleBar = (WindowsTitleBar)CreateNonClientChild(NonClientItem.TitleBar);
                if (titleBar != null)
                {
                    menuProxy = (ProxyHwnd)titleBar.CreateTitleBarChild(WindowsTitleBar._systemMenu);
                }
            }

            if (menuProxy != null)
            {
                menuProxy.AdviseEventRemoved(eventId, aidProps);
            }

            base.AdviseEventRemoved(eventId, aidProps);
        }


        // Picks a WinEvent to track for a UIA property
        protected override int[] PropertyToWinEvent(AutomationProperty idProp)
        {
            if (idProp == AutomationElement.IsEnabledProperty)
            {
                return new int[] { NativeMethods.EventObjectStateChange };
            }

            return base.PropertyToWinEvent(idProp);
        }


        #endregion ProxyHwnd Interface

        #region Proxy Create

        // Static Create method called by UIAutomation to create this proxy.
        // returns null if unsuccessful 
        internal static IRawElementProviderSimple Create(IntPtr hwnd, int idChild, int idObject)
        {
            return Create(hwnd, idChild);
        }

        internal static IRawElementProviderSimple Create(IntPtr hwnd, int idChild)
        {
            if (!UnsafeNativeMethods.IsWindow(hwnd))
            {
                return null;
            }

            System.Diagnostics.Debug.Assert(idChild == 0, string.Format(CultureInfo.CurrentCulture, "Invalid Child Id, idChild == {2}\n\rClassName: \"{0}\"\n\rhwnd = 0x{1:x8}", Misc.ProxyGetClassName(hwnd), hwnd.ToInt32(), idChild));

            NativeMethods.Win32Rect clientRect = new NativeMethods.Win32Rect();
            NativeMethods.Win32Rect windowRect = new NativeMethods.Win32Rect();

            try
            {
                bool hasNonClientControls =
                    // Note: GetClientRect will do MapWindowsPoints
                    Misc.GetClientRectInScreenCoordinates(hwnd, ref clientRect)
                        && Misc.GetWindowRect(hwnd, ref windowRect)
                        && !windowRect.Equals(clientRect);

                if(hasNonClientControls || WindowsFormsHelper.IsWindowsFormsControl(hwnd))
                {
                    return new NonClientArea(hwnd);
                }
                return null;
            }
            catch (ElementNotAvailableException)
            {
                return null;
            }
        }

        // proxy factory used to create menu bar items in response to OBJID_MENU winevents
        // This is wired up to the #nonclientmenubar pseudo-classname in the proxy table
        internal static IRawElementProviderSimple CreateMenuBarItem(IntPtr hwnd, int idChild, int idObject)
        {
            return CreateMenuBarItem(hwnd, idChild);
        }

        private static IRawElementProviderSimple CreateMenuBarItem(IntPtr hwnd, int idChild)
        {
            IntPtr menu = UnsafeNativeMethods.GetMenu(hwnd);

            if (menu == IntPtr.Zero)
            {
                return null;
            }

            NonClientArea nonClientArea = new NonClientArea( hwnd );

            WindowsMenu appMenu = new WindowsMenu(hwnd, nonClientArea, menu, WindowsMenu.MenuType.Toplevel, (int) NonClientItem.Menu);

            if (idChild == 0)
            {
                return appMenu;
            }
            else
            {
                return appMenu.CreateMenuItem(idChild - 1);
            }
        }

        // proxy factory used to create sytem menu in response to OBJID_MENU winevents
        // This is wired up to the #nonclientsysmenu pseudo-classname in the proxy table
        internal static IRawElementProviderSimple CreateSystemMenu(IntPtr hwnd, int idChild, int idObject)
        {
            return CreateSystemMenu(hwnd);
        }

        private static IRawElementProviderSimple CreateSystemMenu(IntPtr hwnd)
        {
            NonClientArea nonClientArea = new NonClientArea(hwnd);
            WindowsTitleBar titleBar = (WindowsTitleBar)nonClientArea.CreateNonClientChild(NonClientItem.TitleBar);

            return titleBar.CreateTitleBarChild(WindowsTitleBar._systemMenu);
        }
        #endregion

        #region Public Methods

        internal static void RaiseEvents(IntPtr hwnd, int eventId, object idProp, int idObject, int idChild)
        {
            switch (idObject)
            {
                case NativeMethods.OBJID_WINDOW:
                    RaiseEventsOnWindow(hwnd, eventId, idProp, idObject, idChild);
                    break;

                case NativeMethods.OBJID_HSCROLL :
                case NativeMethods.OBJID_VSCROLL :
                    RaiseEventsOnScroll(hwnd, eventId, idProp, idObject, idChild);
                    break;

                case NativeMethods.OBJID_CLIENT:
                    RaiseEventsOnClient(hwnd, eventId, idProp, idObject, idChild);
                    break;

                case NativeMethods.OBJID_SYSMENU:
                case NativeMethods.OBJID_MENU:
                    RaiseMenuEventsOnClient(hwnd, eventId, idProp, idObject, idChild);
                    break;
            }
        }

        // Returns a Proxy element corresponding to the specified screen coordinates.
        internal override ProxySimple ElementProviderFromPoint (int x, int y)
        {
            int hit = Misc.ProxySendMessageInt(_hwnd, NativeMethods.WM_NCHITTEST, IntPtr.Zero, NativeMethods.Util.MAKELPARAM(x, y));

            switch (hit)
            {
                case NativeMethods.HTHSCROLL:
                    {
                        ProxyFragment ret = CreateNonClientChild(NonClientItem.HScrollBar);
                        return ret.ElementProviderFromPoint(x, y);
                    }

                case NativeMethods.HTVSCROLL:
                    {
                        ProxyFragment ret = CreateNonClientChild(NonClientItem.VScrollBar);
                        return ret.ElementProviderFromPoint(x, y);
                    }

                case NativeMethods.HTCAPTION :
                case NativeMethods.HTMINBUTTON :
                case NativeMethods.HTMAXBUTTON :
                case NativeMethods.HTHELP :
                case NativeMethods.HTCLOSE :
                case NativeMethods.HTSYSMENU :
                    WindowsTitleBar tb = new WindowsTitleBar(_hwnd, this, 0);
                    return tb.ElementProviderFromPoint (x, y);

                case NativeMethods.HTGROWBOX:
                    return CreateNonClientChild(NonClientItem.Grip);

                case NativeMethods.HTBOTTOMRIGHT:
                    return FindGrip(x, y);

                case NativeMethods.HTBOTTOMLEFT:
                    return FindGripMirrored(x, y);

                case NativeMethods.HTMENU:
                    return FindMenus(x, y);

                case NativeMethods.HTLEFT:
                case NativeMethods.HTRIGHT:
                case NativeMethods.HTTOP:
                case NativeMethods.HTTOPLEFT:
                case NativeMethods.HTTOPRIGHT:
                case NativeMethods.HTBOTTOM:
                case NativeMethods.HTBORDER:
                    // We do not handle the borders so return null here and let the
                    // HWNDProvider handle them as the whole window.
                    return null;

                default:
                    // Leave all other cases (especially including HTCLIENT) alone...
                    return null;
            }

        }

        // Returns an item corresponding to the focused element (if there is one), or null otherwise.
        internal override ProxySimple GetFocus()
        {
            if (WindowsMenu.IsInSystemMenuMode())
            {
                // Since in system menu mode try to find point on the SystemMenu
                WindowsTitleBar titleBar = (WindowsTitleBar)CreateNonClientChild(NonClientItem.TitleBar);
                if (titleBar != null)
                {
                    ProxyFragment systemMenu = (ProxyFragment)titleBar.CreateTitleBarChild(WindowsTitleBar._systemMenu);

                    if (systemMenu != null)
                    {
                        // need to drill down ourself, since the FragmentRoot of the System Menu Bar will
                        // be NonClient area hence UIAutomation will not drill down
                        ProxySimple proxy = systemMenu.GetFocus();
                        if (proxy != null)
                        {
                            return proxy;
                        }
                    }
                }
            }

            return base.GetFocus();
        }

        #endregion Public Methods

        internal override ProviderOptions ProviderOptions
        {
            get
            {
                return base.ProviderOptions | ProviderOptions.NonClientAreaProvider;
            }
        }


        // Returns the Run Time Id.
        internal override int [] GetRuntimeId ()
        {
            return new int [] { 1, unchecked((int)(long)_hwnd) };
        }

        // Returns a pattern interface if supported, else null.
        internal override object GetPatternProvider (AutomationPattern iid)
        {
            if (iid == ScrollPattern.Pattern && WindowScroll.HasScrollableStyle(_hwnd))
            {
                return this;
            }

            return null;
        }

        #region IRawElementProvider

        // Next Silbing: assumes none, must be overloaded by a subclass if any
        // The method is called on the parent with a reference to the child.
        // This makes the implementation a lot more clean than the UIAutomation call
        internal override ProxySimple GetNextSibling (ProxySimple child)
        {
            return ReturnNextNonClientChild (true, (NonClientItem) child._item + 1);
        }

        // Prev Silbing: assumes none, must be overloaded by a subclass if any
        // The method is called on the parent with a reference to the child.
        // This makes the implementation a lot more clean than the UIAutomation call
        internal override ProxySimple GetPreviousSibling (ProxySimple child)
        {
            return ReturnNextNonClientChild (false, (NonClientItem) child._item - 1);
        }

        // GetFirstChild: assumes none, must be overloaded by a subclass if any
        internal override ProxySimple GetFirstChild ()
        {
            return ReturnNextNonClientChild (true, (NonClientItem)((int)NonClientItem.MIN + 1));
        }

        // GetLastChild: assumes none, must be overloaded by a subclass if any
        internal override ProxySimple GetLastChild ()
        {
            return ReturnNextNonClientChild (false, (NonClientItem)((int)NonClientItem.MAX - 1));
        }

        #endregion

        #region Scroll Pattern

        // Request to scroll Horizontally and vertically by the specified amount
        void IScrollProvider.SetScrollPercent (double horizontalPercent, double verticalPercent)
        {
            WindowScroll.SetScrollPercent (_hwnd, horizontalPercent, verticalPercent, true);
        }

        // Request to scroll horizontally and vertically by the specified scrolling amount
        void IScrollProvider.Scroll (ScrollAmount horizontalAmount, ScrollAmount verticalAmount)
        {
            WindowScroll.Scroll (_hwnd, horizontalAmount, verticalAmount, true);
        }

        // Calc the position of the horizontal scroll bar thumb in the 0..100 % range
        double IScrollProvider.HorizontalScrollPercent
        {
            get
            {
                return (double) WindowScroll.GetPropertyScroll (ScrollPattern.HorizontalScrollPercentProperty, _hwnd);
            }
        }

        // Calc the position of the Vertical scroll bar thumb in the 0..100 % range
        double IScrollProvider.VerticalScrollPercent
        {
            get
            {
                return (double)WindowScroll.GetPropertyScroll(ScrollPattern.VerticalScrollPercentProperty, _hwnd);
            }
        }

        // Percentage of the window that is visible along the horizontal axis. 
        double IScrollProvider.HorizontalViewSize
        {
            get
            {
                return (double)WindowScroll.GetPropertyScroll(ScrollPattern.HorizontalViewSizeProperty, _hwnd);
            }
        }

        // Percentage of the window that is visible along the vertical axis. 
        double IScrollProvider.VerticalViewSize
        {
            get
            {
                return (double)WindowScroll.GetPropertyScroll(ScrollPattern.VerticalViewSizeProperty, _hwnd);
            }
        }

        // Can the element be horizontaly scrolled
        bool IScrollProvider.HorizontallyScrollable
        {
            get
            {
                return (bool) WindowScroll.GetPropertyScroll (ScrollPattern.HorizontallyScrollableProperty, _hwnd);
            }
        }

        // Can the element be verticaly scrolled
        bool IScrollProvider.VerticallyScrollable
        {
            get
            {
                return (bool) WindowScroll.GetPropertyScroll (ScrollPattern.VerticallyScrollableProperty, _hwnd);
            }
        }

        #endregion

        #region Private Methods

        // Not all the non-client children exist for every window that has a non-client area.
        // This gets the next child that is actually there.
        private ProxyFragment ReturnNextNonClientChild (bool next, NonClientItem start)
        {
            ProxyFragment el;

            for (int i = (int) start; i > (int) NonClientItem.MIN && i < (int) NonClientItem.MAX; i += next ? 1 : -1)
            {
                el = CreateNonClientChild ((NonClientItem) i);
                if (el != null)
                    return el;
            }

            return null;
        }

        // The ApplicationWindow pattern provider code needs to be able to create menu bars correctly
        // and uses the NonClientArea proxy to accomplish this.

        // the scroll bars are not always peripheral so we create them with a negative number so the
        // base class can tell if they are peripheral or not.
        internal enum NonClientItem
        {
            MIN = -5,
            Grip = -4,
            HScrollBar = -3,
            VScrollBar = -2,
            TitleBar = 0,
            Menu,
            MAX,
        }

        // Create the approiate child this can return null if that child does not exist
        internal ProxyFragment CreateNonClientChild (NonClientItem item)
        {
            switch (item)
            {
                case NonClientItem.HScrollBar :
                    if (WindowsScrollBar.HasHorizontalScrollBar (_hwnd))
                    {
                        // the listview needs special handling WindowsListViewScrollBar inherits from WindowsScrollBar
                        // and overrides some of its behavoir
                        if (Misc.ProxyGetClassName(_hwnd) == "SysListView32")
                            return new WindowsListViewScrollBar (_hwnd, this, (int) item, NativeMethods.SB_HORZ);
                        else
                            return new WindowsScrollBar (_hwnd, this, (int) item, NativeMethods.SB_HORZ);
                    }
                    break;

                case NonClientItem.VScrollBar :
                    if (WindowsScrollBar.HasVerticalScrollBar (_hwnd))
                    {
                        // the listview needs special handling WindowsListViewScrollBar inherits from WindowsScrollBar
                        // and overrides some of its behavoir
                        if (Misc.ProxyGetClassName(_hwnd) == "SysListView32")
                            return new WindowsListViewScrollBar (_hwnd, this, (int) item, NativeMethods.SB_VERT);
                        else
                            return new WindowsScrollBar (_hwnd, this, (int) item, NativeMethods.SB_VERT);
                    }
                    break;

                case NonClientItem.TitleBar :
                    {
                        // Note 2 checks above will succeed for the win32popup menu, hence adding this last one
                        if (WindowsTitleBar.HasTitleBar (_hwnd))
                        {
                            return new WindowsTitleBar (_hwnd, this, (int) item);
                        }
                        break;
                    }

                case NonClientItem.Menu :
                    {
                        return CreateNonClientMenu();
                    }

                case NonClientItem.Grip :
                    {
                        int style = WindowStyle;
                        if (Misc.IsBitSet(style, NativeMethods.WS_VSCROLL) && Misc.IsBitSet(style, NativeMethods.WS_HSCROLL))
                        {
                            if (WindowsGrip.IsGripPresent(_hwnd, false))
                            {
                                return new WindowsGrip(_hwnd, this, (int)item);
                            }
                        }
                        break;
                    }

                default :
                    return null;

            }

            return null;
        }

        internal ProxyHwnd CreateNonClientMenu ()
        {
            // child windows don't have menus
            int style = WindowStyle;
            if (!Misc.IsBitSet(style, NativeMethods.WS_CHILD))
            {
                ProxyHwnd menuProxy = null;

                if (WindowsMenu.IsInSystemMenuMode())
                {
                    // Since in system menu mode try to find point on the SystemMenu
                    WindowsTitleBar titleBar = (WindowsTitleBar)CreateNonClientChild(NonClientItem.TitleBar);
                    if (titleBar != null)
                    {
                        menuProxy = (ProxyHwnd)titleBar.CreateTitleBarChild(WindowsTitleBar._systemMenu);
                    }
                }
                else
                {
                    IntPtr menu = UnsafeNativeMethods.GetMenu(_hwnd);
                    if (menu != IntPtr.Zero)
                    {
                        menuProxy = new WindowsMenu(_hwnd, this, menu, WindowsMenu.MenuType.Toplevel, (int)NonClientItem.Menu);
                    }
                }

                if (menuProxy != null)
                {
                    // if this menu is not visible its really not there
                    if (menuProxy.BoundingRectangle.Width != 0 && menuProxy.BoundingRectangle.Height != 0)
                    {
                        return menuProxy;
                    }
                }
            }

            return null;
        }

        private ProxyFragment FindGrip(int x,int y)
        {
            // Note that for sizeable windows, being over the size grip may
            // return in fact HTBOTTOMRIGHT for sizing purposes.
            // make sure we don't accidently include the borders
            ProxyFragment grip = CreateNonClientChild(NonClientItem.Grip);
            if (grip != null)
            {
                Rect rect = grip.BoundingRectangle;
                if (x < rect.Right && y < rect.Bottom)
                {
                    return grip;
                }
            }

            return null;
        }
        
        private ProxyFragment FindGripMirrored(int x, int y)
        {
            if (Misc.IsLayoutRTL(_hwnd))
            {
                // Right to left mirroring style

                // Note that for sizeable windows, being over the size grip may
                // return in fact HTBOTTOMLEFT for sizing purposes.
                // make sure we don't accidently include the borders
                ProxyFragment grip = CreateNonClientChild(NonClientItem.Grip);
                if (grip != null)
                {
                    Rect rect = grip.BoundingRectangle;
                    if (x > rect.Left && y < rect.Bottom)
                    {
                        return grip;
                    }
                }
            }

            return null;
        }

        private ProxySimple FindMenus(int x, int y)
        {
            if (WindowsMenu.IsInSystemMenuMode())
            {
                // Since in system menu mode try to find point on the SystemMenu
                WindowsTitleBar titleBar = (WindowsTitleBar)CreateNonClientChild(NonClientItem.TitleBar);
                if (titleBar != null)
                {
                    ProxyFragment systemMenu = (ProxyFragment)titleBar.CreateTitleBarChild(WindowsTitleBar._systemMenu);

                    if (systemMenu != null)
                    {
                        // need to drill down ourself, since the FragmentRoot of the System Menu Bar will
                        // be NonClient area hence UIAutomation will not drill down
                        ProxySimple proxy = systemMenu.ElementProviderFromPoint(x, y);
                        if (proxy != null)
                        {
                            return proxy;
                        }
                    }
                }
            }
            else
            {
                // Not in system menu mode so it may be a Popup Menu, have a go at it
                ProxyFragment menu = CreateNonClientChild(NonClientItem.Menu);
                if (menu != null)
                {
                    // need to drill down ourself, since the FragmentRoot of the MenuBar will
                    // be NonClient area hence UIAutomation will not drill down
                    ProxySimple proxy = menu.ElementProviderFromPoint(x, y);
                    if (proxy != null)
                    {
                        return proxy;
                    }

                    // We may have been on the Menu but not on a menu Item
                    return menu;
                }
            }

            return null;
        }

        private static void RaiseMenuEventsOnClient(IntPtr hwnd, int eventId, object idProp, int idObject, int idChild)
        {
            ProxySimple el = WindowsMenu.CreateMenuItemFromEvent(hwnd, eventId, idChild, idObject);
            if (el != null)
            {
                el.DispatchEvents(eventId, idProp, idObject, idChild);
            }
        }

        private static void RaiseEventsOnClient(IntPtr hwnd, int eventId, object idProp, int idObject, int idChild)
        {
            if (Misc.GetClassName(hwnd) == "ComboLBox")
            {
                ProxySimple el = (ProxySimple)WindowsListBox.Create(hwnd, idChild);
                if (el != null)
                {
                    el.DispatchEvents(eventId, idProp, idObject, idChild);
                }
            }
        }

        private static void RaiseEventsOnScroll(IntPtr hwnd, int eventId, object idProp, int idObject, int idChild)
        {
            if ((idProp == ScrollPattern.VerticalScrollPercentProperty && idObject != NativeMethods.OBJID_VSCROLL) ||
                (idProp == ScrollPattern.HorizontalScrollPercentProperty && idObject != NativeMethods.OBJID_HSCROLL))
            {
                return;
            }

            ProxyFragment el = new NonClientArea(hwnd);
            if (el == null)
                return;

            if (idProp == InvokePattern.InvokedEvent)
            {
                NonClientItem item = idObject == NativeMethods.OBJID_HSCROLL ? NonClientItem.HScrollBar : NonClientItem.VScrollBar;
                int sbFlag = idObject == NativeMethods.OBJID_HSCROLL ? NativeMethods.SB_HORZ : NativeMethods.SB_VERT;
                ProxyFragment scrollBar = new WindowsScrollBar(hwnd, el, (int)item, sbFlag);
                if (scrollBar != null)
                {
                    ProxySimple scrollBarBit = WindowsScrollBarBits.CreateFromChildId(hwnd, scrollBar, idChild, sbFlag);
                    if (scrollBarBit != null)
                    {
                        scrollBarBit.DispatchEvents(eventId, idProp, idObject, idChild);
                    }
                }

                return;
            }

            if (eventId == NativeMethods.EventObjectStateChange && idProp == ValuePattern.IsReadOnlyProperty)
            {
                if (idChild == 0)
                {
                    // This code is never exercised. The code in User needs to change to send 
                    // EventObjectStateChange with a client ID of zero
                    //
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
                    el.DispatchEvents(eventId, idProp, idObject, idChild);
                }
                return;
            }

            if (idProp == ValuePattern.ValueProperty && eventId == NativeMethods.EVENT_OBJECT_VALUECHANGE)
            {
                NonClientItem item = idObject == NativeMethods.OBJID_HSCROLL ? NonClientItem.HScrollBar : NonClientItem.VScrollBar;
                WindowsScrollBar scrollBar = new WindowsScrollBar(hwnd, el, (int)item, idObject == NativeMethods.OBJID_HSCROLL ? NativeMethods.SB_HORZ : NativeMethods.SB_VERT);
                scrollBar.DispatchEvents(0, ValuePattern.ValueProperty, NativeMethods.OBJID_CLIENT, 0);
                return;
            }

            if (Misc.GetClassName(hwnd) == "ComboLBox")
            {
                el = (ProxyFragment)WindowsListBox.Create(hwnd, 0);
            }

            if (el != null)
            {
                el.DispatchEvents(eventId, idProp, idObject, idChild);
            }
        }

        private static void RaiseEventsOnWindow(IntPtr hwnd, int eventId, object idProp, int idObject, int idChild)
        {
            ProxyFragment elw = new NonClientArea(hwnd);

            if (eventId == NativeMethods.EventObjectNameChange)
            {
                int style = Misc.GetWindowStyle(hwnd);
                if (Misc.IsBitSet(style, NativeMethods.WS_CHILD))
                {
                    // Full control names do not change.  They are named by the static label.
                    return;
                }
                else
                {
                    // But the title bar name does so allow title bar proxy to procees the event.
                    WindowsTitleBar tb = new WindowsTitleBar(hwnd, elw, 0);
                    tb.DispatchEvents(eventId, idProp, idObject, idChild);
                }
            }

            elw.DispatchEvents(eventId, idProp, idObject, idChild);
        }

        #endregion Private Methods

        #region Private Fields

        #endregion Private Fields

    }

}
