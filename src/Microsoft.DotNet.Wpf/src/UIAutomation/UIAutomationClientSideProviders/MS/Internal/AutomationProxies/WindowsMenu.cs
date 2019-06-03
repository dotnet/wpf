// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// Description: HWND-based Menu Proxy

// PRESHARP: In order to avoid generating warnings about unkown message numbers and unknown pragmas.
#pragma warning disable 1634, 1691

using System;
using System.Collections;
using System.Globalization;
using System.Text;
using System.ComponentModel;
using Accessibility;
using System.Windows.Automation.Provider;
using System.Windows.Automation;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using MS.Win32;

// Is it worth it to make WindowsMenu a protected base class
// and have a specific menu classes (e.g. MenuBar) that derives from it?
// The whole menu code is a bit complicated and hard to understand.

namespace MS.Internal.AutomationProxies
{
    // Win32 menu proxy
    class WindowsMenu: ProxyHwnd
    {
        // ------------------------------------------------------
        //
        // Constructors
        //
        // ------------------------------------------------------

        #region Constructors

        internal WindowsMenu(IntPtr hwnd, ProxyFragment parent, IntPtr hmenu, MenuType type, int item)
            : base (hwnd, parent, item)
        {
            _type = type;
            _fNonClientAreaElement = true;
            _fIsContent = false;

            FixMDIMenuType();

            // (Vista Explorer FolerBandModuleInner menu items don't raise InvokedEvent).  During events, this ctor is
            // called with parent = null and type = MenuType.Menu.  I think it should be parent != null and type = MenuType.Toplevel
            // since it is acting like a top-level menu.  Need to work through this with the other core team members, though.  Start
            // here to investigate.
            //Console.WriteLine("WindowsMenu {0} is {1} parent is {2}", hwnd.ToInt32(), (int)_type, (parent != null)?parent._hwnd.ToInt32() : 0);

            // According to spec:
            switch (_type)
            {
                case MenuType.Toplevel :
                    {
                        _cControlType = ControlType.MenuBar;
                        _sAutomationId = "MenuBar"; // This string is a non-localizable string
                    }
                    break;

                case MenuType.System :
                    {
                        _cControlType = ControlType.MenuBar;
                        _sAutomationId = "SystemMenuBar"; // This string is a non-localizable string

                        // Cache this menu, so change use during the Collapse Property Change Event.
                        if (!_expandedMenus.Contains(IntPtr.Zero))
                        {
                            _expandedMenus[IntPtr.Zero] = new MenuParentInfo(hwnd, item, type);
                        }
                    }
                    break;


                case MenuType.SystemPopup :
                    {
                        // Re-parent properly a system popup to the system menu
                        _parent = GetSystemPopupParent();
                        _cControlType = ControlType.Menu;
                        _sAutomationId = "MenuPopup"; // This string is a non-localizable string

                        // Cache the parent menu, so change use during the Collapse Property Change Event.
                        if (!_expandedMenus.Contains(hwnd))
                        {
                            _expandedMenus[hwnd] = new MenuParentInfo(_parent._hwnd, _parent._item, ((MenuItem)_parent)._menuType);
                        }
                    }
                    break;

                case MenuType.Submenu :
                    {
                        // Re-parent properly a popup menu to the proper parent
                        _parent = GetHierarchyParent(hwnd);
                        _cControlType = ControlType.Menu;

                        // Cache the parent menu, so change use during the Collapse Property Change Event.
                        if (!_expandedMenus.Contains(hwnd))
                        {
                            _expandedMenus[hwnd] = new MenuParentInfo(_parent._hwnd, _parent._item, ((MenuItem)_parent)._menuType);
                        }
                    }
                    break;

                default :
                    {
                        _cControlType = ControlType.Menu;
                    }
                    break;
            }

            _hmenu = hmenu;
        }

        internal static ProxySimple CreateMenuItemFromEvent(IntPtr hwndMenu, int eventId, int idChild, int idObject)
        {
            // This assert is valid because idObject is a non-volatile enumeration or constant
            System.Diagnostics.Debug.Assert(idObject == NativeMethods.OBJID_MENU || idObject == NativeMethods.OBJID_SYSMENU, "Unexpected idObject");

            if (eventId == NativeMethods.EventObjectInvoke)
            {
                // When a menu item is invoked the containing hwnd is destroyed and calling methods on hwndMenu result in an exception
                // (Target element corresponds to UI that is no longer available. For example, the parent window has closed.) so
                // at this point the only thing we can know about this menu item is whether it was an item on the system menu or
                // a menu bar and an identifier for the menu item.
                MenuParentInfo parentInfo = (MenuParentInfo)_expandedMenus[hwndMenu];
                if (parentInfo != null)
                {
                    return new DestroyedMenuItem(hwndMenu, idChild, parentInfo._hwndParent);
                }
            }

            return null;
        }

        // Handle the case of an MDI child frame system menu,
        // which would otherwise be treated as a normal submenu.
        private void FixMDIMenuType()
        {
            if (_type == MenuType.Submenu && GetHierarchyParent(_hwnd) == null && GetSystemPopupParent() != null)
            {
                _type = MenuType.SystemPopup;
            }
        }

        #endregion

        #region Proxy Create

        // Static Create method called by UIAutomation to create this proxy.
        // null if unsuccessful
        internal static IRawElementProviderSimple Create(IntPtr hwnd, int idChild, int idObject)
        {
            return Create(hwnd, idChild);
        }

        private static IRawElementProviderSimple Create(IntPtr hwnd, int idChild)
        {
            MenuType type = MenuType.Toplevel;
            IntPtr hmenu;

            try
            {
                hmenu = UnsafeNativeMethods.GetMenu(hwnd);

                if (hmenu == IntPtr.Zero)
                {
                    hmenu = HmenuFromHwnd(hwnd);
                    if (hmenu == IntPtr.Zero)
                    {
                        // bail
                        return null;
                    }
                }
                // get type of the submenu
                type = GetSubMenuType(hwnd, hmenu);
            }
            catch (ElementNotAvailableException)
            {
                return null;
            }

            WindowsMenu windowsMenu = new WindowsMenu(hwnd, null, hmenu, type, 0);
            if (idChild == 0)
            {
                return windowsMenu;
            }
            else
            {
                return windowsMenu.CreateMenuItem(idChild - 1);
            }
        }

        // Special method responsible for creating a System MenuBar
        // The type will be automatically set to the System
        internal static WindowsMenu CreateSystemMenu (IntPtr hwnd, ProxyFragment parent)
        {
            IntPtr hSysMenu = GetSystemMenuHandle(hwnd);
            if (hSysMenu != IntPtr.Zero)
            {
                return new WindowsMenu(hwnd, parent, hSysMenu, MenuType.System, 1);
            }

            return null;
        }

        // Called by UIA when we're in menu mode
        // Note that we have to drill all the way down to an item here, UIA expects us
        // to return the lowest focused item.
        internal static IRawElementProviderSimple CreateFocusedMenuItem(IntPtr hwnd, int idChild, int idObject)
        {
            NativeMethods.GUITHREADINFO gui;
            if (!Misc.ProxyGetGUIThreadInfo(0, out gui))
            {
                return null;
            }

            // The following code assumes that regardless of the type of menu mode
            // we're in, there's only one chain of cascaded menus present - you never
            // have two unrelated sets of menus present at a time.
            // So, we can look for *any* visible popup menu window (which could get us
            // half-way into a cascade chain, or at the start or end),
            // then follow the chain (by looking for selected items) to the lowest
            // item.
            // If no visible menu popup present, then need to special case
            // based on menu mode (eg. sys vs menubar vs popup).

            // first, check that we're really in menu mode (all menu mode flags
            // also include this...
            if (!Misc.IsBitSet(gui.dwFlags, NativeMethods.GUI_INMENUMODE))
                return null;

            IntPtr hwndPopup = IntPtr.Zero;
            for (; ; )
            {
                hwndPopup = Misc.FindWindowEx(IntPtr.Zero, hwndPopup, WindowsMenu.MenuClassName, null);
                if (hwndPopup == IntPtr.Zero)
                    break;
                if (!SafeNativeMethods.IsWindowVisible(hwndPopup))
                    continue;

                // No other tests needed - it's menu-class and visible, use it...
                break;
            }

            if (hwndPopup != IntPtr.Zero)
            {
                // Now drill down to lowest item...
                for (; ; )
                {
                    // Get hmenu of popup...
                    IntPtr hmenu = HmenuFromHwnd(hwndPopup);
                    if (hmenu == IntPtr.Zero)
                        return null;

                    // Check for item focused in the popup...
                    int i = GetHighlightedMenuItem(hmenu);
                    if (i == -1)
                    {
                        // No selected item - could be the menu itself (eg. context menus
                        // sometimes appear with no item having focus)
                        return Create(hwndPopup, 0);
                    }

                    // Got an item - it could be a sub-menu or a leaf node item...
                    IntPtr hSubMenu = UnsafeNativeMethods.GetSubMenu(hmenu, i);
                    if (hSubMenu == IntPtr.Zero)
                    {
                        // actual menu item...
                        return Create(hwndPopup, i + 1);
                    }

                    // Check to see if there's a popup open for that sub hmenu...
                    IntPtr hwndSubMenuPopup = GetPopupHwndForHMenu(hSubMenu);
                    if (hwndSubMenuPopup == IntPtr.Zero)
                    {
                        // No popup present - so just means that a popup item is selected,
                        // but not popped out...
                        return Create(hwndPopup, i + 1);
                    }

                    // Popup is present - so reassign the loop variable, and drill into it...
                    hwndPopup = hwndSubMenuPopup;
                }
            }

            // No popup present, so must be a special case...
            //
            // For any of these, the item could be a menu itself or an item in a menu -
            // most reliable approach is to pick an appropriate starting point (eg. sysmenu or menubar),
            // and follow the trail of focused items.
            if (Misc.IsBitSet(gui.dwFlags, NativeMethods.GUI_SYSTEMMENUMODE))
            {
                // Sys menu mode but no popup, means it must be the sys menu button (on the non-client area)

                // Can't create a sys menu area directly - have to create nonclientarea then titlebar first...
                NonClientArea nonClient = (NonClientArea)NonClientArea.Create(gui.hwndActive, 0);
                WindowsTitleBar titlebar = new WindowsTitleBar(gui.hwndActive, nonClient, 0);
                return CreateSystemMenu(gui.hwndActive, titlebar);
            }
            else if (Misc.IsBitSet(gui.dwFlags, NativeMethods.GUI_POPUPMENUMODE))
            {
                // Popup/context menu mode but no popup - shouldn't happen, unless it's just
                // disappeared.
                return null;
            }
            else if (Misc.IsBitSet(gui.dwFlags, NativeMethods.GUI_INMENUMODE))
            {
                // Menu/menubar mode, but no menu present - so could be item on menu bar
                // or menu bar itself...
                IntPtr hmenu = UnsafeNativeMethods.GetMenu(gui.hwndActive);
                int i = GetHighlightedMenuItem(hmenu);
                if (i == -1)
                {
                    // No selected item - so its the menubar itself...
                    // Need to create this via NonClient proxy...
                    return NonClientArea.CreateMenuBarItem(gui.hwndActive, 0, 0/*ignored by CreateMenuBarItem*/);
                }
                else
                {
                    // selected item on menubar...
                    return NonClientArea.CreateMenuBarItem(gui.hwndActive, i + 1, 0/*ignored by CreateMenuBarItem*/);
                }
            }
            return null;
        }

        #endregion

        //------------------------------------------------------
        //
        //  Patterns Implementation
        //
        //------------------------------------------------------

        #region ProxySimple Interface

        //Gets the runtime id
        internal override int[] GetRuntimeId()
        {
            return new int[] { 1, unchecked((int)(long)_hwnd), unchecked((int)(long)_hmenu) };
        }

        // Returns a pattern interface if supported.
        internal override object GetPatternProvider (AutomationPattern iid)
        {
            return null;
        }

        // Process all the Element Properties
        internal override object GetElementProperty(AutomationProperty idProp)
        {
            if (idProp == AutomationElement.NameProperty)
            {
                // NOTE: It does not look like Winforms support the AccessibleName for standard menus.
                // If MenuStrips are going to be supported by this proxy this code will need to be removed.
                return LocalizedName;
            }

            return base.GetElementProperty(idProp);
        }

        // Gets the bounding rectangle for this element
        internal override Rect BoundingRectangle
        {
            get
            {
                return GetBoundingRectangle();
            }
        }

        //Gets the localized name
        internal override string LocalizedName
        {
            get
            {
                if (_parent != null && _type == MenuType.Submenu)
                {
                    return _parent.LocalizedName;
                }
                else
                {
                    return SR.Get(GetLocalizedNameFromType());
                }
            }
        }
        #endregion

        #region ProxyFragment Interface

        // Returns the next sibling element in the raw hierarchy.
        // Peripheral controls have always negative values.
        // Returns null if no next child
        internal override ProxySimple GetNextSibling (ProxySimple child)
        {
            int item = child._item;

            return item + 1 < Count ? CreateMenuItem (item + 1) : null;
        }

        // Returns the previous sibling element in the raw hierarchy.
        // Peripheral controls have always negative values.
        // Returns null is no previous
        internal override ProxySimple GetPreviousSibling (ProxySimple child)
        {
            int item = child._item;

            return item > 0 && (item - 1) < Count ? CreateMenuItem (item - 1) : null;
        }

        // Returns the first child element in the raw hierarchy.
        internal override ProxySimple GetFirstChild ()
        {
            return Count > 0 ? CreateMenuItem (0) : null;
        }

        // Returns the last child element in the raw hierarchy.
        internal override ProxySimple GetLastChild ()
        {
            int count = Count;

            return count > 0 ? CreateMenuItem (count - 1) : null;
        }

        // Returns a Proxy element corresponding to the specified screen coordinates.
        internal override ProxySimple ElementProviderFromPoint (int x, int y)
        {
            for (int item = 0, count = Count; item < count; item++)
            {
                // The point might be in this menu item or in one of the popup Menu Item
                ProxySimple menuItem = CreateMenuItem(item).ElementProviderFromPoint(x, y);
                if (menuItem != null)
                {
                    return menuItem;
                }
            }

            return null;
        }

        // Returns an item corresponding to the focused element (if there is one), or null otherwise.
        internal override ProxySimple GetFocus()
        {
            int i = GetHighlightedMenuItem(_hmenu);
            if (i == -1)
                return null;
            else
                return CreateMenuItem(i);
        }

        #endregion ProxyFragment Interface

        #region ProxyHwnd Interface

        internal override void AdviseEventAdded (AutomationEvent eventId, AutomationProperty [] aidProps)
        {
            // NOTE: Menu supports SYSTEM wide events (SYS_MENUPOPUPSTART and SYS_MENUPOPUPEND)
            // Each proxy will register for the system-wide event only once (this is ensured by the event-framework (see BuildEventsList in WinEventTracker.cs)
            if (MenuRelatedEvent(eventId, aidProps))
            {
                // Sytem wide event register with hwnd == IntPtr.Zero
                WinEventTracker.AddToNotificationList(IntPtr.Zero, new WinEventTracker.ProxyRaiseEvents(MenuEvents), _menuEvents, _menuEvents.Length);

                // Keep counter of how many requests came so we will know when to remove ourselves from the notification list
                // We need a counter since system wide events are not based on the hwnd.
                _eventListeners++;
            }
        }

        internal override void AdviseEventRemoved (AutomationEvent eventId, AutomationProperty [] aidProps)
        {
            // NOTE: Menu supports SYSTEM wide events (SYS_MENUPOPUPSTART and SYS_MENUPOPUPEND)
            // Each proxy will register for the system-wide event only once (this is ensured by the event-framework (see BuildEventsList in WinEventTracker.cs)
            // We'll keep an internal counter on how many times user asked us to sign up for event
            // and when counter will get to zero we will remove ourselve from notification list
            // In the case when counter is not 0 we will do nothing but UIAutomation will ensure that LE that asked to be removed from being notified
            // of the event will not be getting anymore notification
            if (_eventListeners > 0)
            {
                --_eventListeners;
                if (_eventListeners == 0 && MenuRelatedEvent (eventId, aidProps))
                {
                    WinEventTracker.RemoveToNotificationList (IntPtr.Zero, _menuEvents, new WinEventTracker.ProxyRaiseEvents (MenuEvents), _menuEvents.Length);
                }
            }
        }

        #endregion ProxyHwnd Interface

        //------------------------------------------------------
        //
        //  Internal Methods
        //
        //------------------------------------------------------

        #region Internal Methods

        internal MenuItem CreateMenuItem (int index)
        {
            return new MenuItem(_hwnd, this, index, _hmenu, _type);
        }

        // wrapper for GetMenuBarInfo
        internal static bool GetMenuBarInfo(IntPtr hwnd, int idObject, uint idItem, out NativeMethods.MENUBARINFO mbi)
        {
            mbi = new NativeMethods.MENUBARINFO();
            mbi.cbSize = Marshal.SizeOf(mbi.GetType());
            bool result = Misc.GetMenuBarInfo(hwnd, idObject, idItem, ref mbi);

#if _NEED_DEBUG_OUTPUT
            StringBuilder sb = new StringBuilder(1024);
            sb.Append("MENUBARINFO\n\r");
            sb.Append("{\n\r");
            sb.AppendFormat("\tcbSize = {0},\n\r", mbi.cbSize);
            sb.AppendFormat("\trcBar = ({0}, {1}, {2}, {3}),\n\r", mbi.rcBar.left, mbi.rcBar.top, mbi.rcBar.right, mbi.rcBar.bottom);
            sb.AppendFormat("\thMenu = 0x{0:x8},\n\r", (mbi.hMenu).ToInt32());
            sb.AppendFormat("\thwndMenu = 0x{0:x8},\n\r", (mbi.hwndMenu).ToInt32());
            sb.AppendFormat("\tfocusFlags = 0x{0:x8},\n\r", mbi.focusFlags);
            sb.Append("}\n\r");
            System.Diagnostics.Debug.WriteLine(sb.ToString());
#endif

            return result;
        }

        // Return menuItem object which is a hierarchical parent of the given menu (specified via hwnd)
        // return NULL in the case when menu does not have a parent (e.g. Context menu)
        // NOTE: this method should not be called for the menuItem that lives on the System or Menubar
        internal static MenuItem GetHierarchyParent(IntPtr hwnd)
        {
            int ownerMenuItemPos = -1;
            IntPtr menuParent = IntPtr.Zero;
            IntPtr hwndParent = IntPtr.Zero;

            IntPtr menu = HmenuFromHwnd(hwnd);
            if (menu == IntPtr.Zero)
            {
                throw new ElementNotAvailableException();
            }
            MenuType currentType = GetSubMenuType(hwnd, menu);
            MenuType parentType = MenuType.Toplevel;

            if (currentType == MenuType.Submenu)
            {
                if (!GetSubMenuParent(hwnd, out menuParent, out hwndParent, out ownerMenuItemPos, out parentType))
                {
                    return null;
                }

                ProxyFragment parent = null;
                if (parentType == MenuType.Toplevel)
                {
                    // Top Level Menu.
                    // We need to have the parenthood defined in the same way as if it was done
                    // from the non client area code.

                    NonClientArea nonClientArea = new NonClientArea(hwndParent);
                    parent = nonClientArea.CreateNonClientChild(NonClientArea.NonClientItem.Menu);
                }
                else
                {
                    parent = new WindowsMenu(hwndParent, null, menuParent, parentType, ownerMenuItemPos);
                }

                return new MenuItem(hwndParent, parent, ownerMenuItemPos, menuParent, parentType);
            }

            return null;
        }

        // Return menuItem object which is a hierarchical parent of the given menu (specified via hwnd)
        // return NULL in the case when menu does not have a parent (e.g. Context menu)
        // NOTE: this method should not be called for the menuItem that lives on the System or Menubar
        private static bool GetSubMenuParent (IntPtr hwndMenu, out IntPtr menuParent, out IntPtr hwndParent, out int ownerMenuItemPos, out MenuType parentType)
        {
            ownerMenuItemPos = -1;
            hwndParent = IntPtr.Zero;
            menuParent = IntPtr.Zero;

            parentType = MenuType.Toplevel;
            IntPtr hMenu = HmenuFromHwnd (hwndMenu);
            if (hMenu == IntPtr.Zero)
            {
                return false;
            }

            // this covers rest of cases
            // Parent lives on menuBar or parent lives on another submenu
            // or parent lives on the context or parent lives on system popup menu ...
            // Start from menuBar
            NativeMethods.GUITHREADINFO gui;

            if (!Misc.ProxyGetGUIThreadInfo(0, out gui))
            {
                return false;
            }

            IntPtr hMenuPossibleParent = UnsafeNativeMethods.GetMenu (gui.hwndActive);

            if (hMenuPossibleParent != IntPtr.Zero)
            {
                int menuItem = GetMenuItemParent (hMenuPossibleParent, hMenu);

                if (menuItem != -1)
                {
                    ownerMenuItemPos = menuItem;
                    menuParent = hMenuPossibleParent;
                    hwndParent = gui.hwndActive;
                    parentType = MenuType.Toplevel;
                    return true;
                }
            }

            // got here: menuItem does not live on the menubar
            // Menu on which menuItem lives located after us in the hwnd-hierarchy
            for (IntPtr hwndPossibleParent = Misc.FindWindowEx(IntPtr.Zero, hwndMenu, WindowsMenu.MenuClassName, null);
                 hwndPossibleParent != IntPtr.Zero;
                 hwndPossibleParent = Misc.FindWindowEx(IntPtr.Zero, hwndPossibleParent, WindowsMenu.MenuClassName, null))
            {
                if (SafeNativeMethods.IsWindowVisible (hwndPossibleParent))
                {
                    hMenuPossibleParent = HmenuFromHwnd (hwndPossibleParent);
                    if (hMenuPossibleParent != IntPtr.Zero)
                    {
                        int menuItem = GetMenuItemParent (hMenuPossibleParent, hMenu);

                        if (menuItem != -1)
                        {
                            ownerMenuItemPos = menuItem;
                            menuParent = hMenuPossibleParent;
                            hwndParent = hwndPossibleParent;
                            parentType = GetSubMenuType (hwndParent, menuParent);
                            return true;
                        }
                    }
                }
            }
            return false;
        }

        // Find visible hwnd that contains the submenu
        internal static IntPtr WindowFromSubmenu (IntPtr submenu)
        {
            for (IntPtr hwndSubMenu = Misc.FindWindowEx(IntPtr.Zero, IntPtr.Zero, MenuClassName, null);
                 hwndSubMenu != IntPtr.Zero;
                 hwndSubMenu = Misc.FindWindowEx(IntPtr.Zero, hwndSubMenu, MenuClassName, null))
            {
                if (SafeNativeMethods.IsWindowVisible (hwndSubMenu))
                {
                    IntPtr submenuCandidate = HmenuFromHwnd (hwndSubMenu);

                    if (submenuCandidate == submenu)
                    {
                        return hwndSubMenu;
                    }
                }
            }

            return IntPtr.Zero;
        }

        // Get type of the submenu
        // DO NOT call this method for System and MenuBar
        internal static MenuType GetSubMenuType (IntPtr hwnd, IntPtr hMenu)
        {
            if (IsSystemPopupMenu(hMenu))
            {
                return MenuType.SystemPopup;
            }

            if (IntPtr.Zero != Misc.GetWindow(hwnd, NativeMethods.GW_OWNER))
            {
                return MenuType.Submenu;
            }

            return MenuType.Context;
        }

        // This method returns the index of menuItem that lives on hmenuPossibleParent
        // and that is possible responsible for showing hmenuChild
        // If such menuItem is not found return -1
        private static int GetMenuItemParent (IntPtr hmenuPossibleParent, IntPtr hmenuChild)
        {
            int count = Misc.GetMenuItemCount(hmenuPossibleParent);

            for (int i = 0; i < count; i++)
            {
                // for speed reasons do not try to filter out menuItem that are not of type SubMenu
                if (UnsafeNativeMethods.GetSubMenu (hmenuPossibleParent, i) == hmenuChild)
                {
                    return i;
                }
            }

            return -1;
        }

        internal int Count
        {
            get
            {
                // The GetMenuItemCount API return always 1 for the system popup event
                // when it is not there. Work around to deal with it.
                if (_type == MenuType.System)
                {
                    return 1;
                }

                return Misc.GetMenuItemCount(_hmenu);
            }
        }

        // Retrieve hMenu represented by passed in hwnd
        // NOTE: do not call for menubar or system menubar
        internal static IntPtr HmenuFromHwnd (IntPtr hwnd)
        {
            if (SafeNativeMethods.IsWindowVisible (hwnd))
            {
                return Misc.ProxySendMessage(hwnd, NativeMethods.MN_GETHMENU, IntPtr.Zero, IntPtr.Zero);
            }

            return IntPtr.Zero;
        }

        // check if passed in menu handle represents windows system menu
        internal static bool IsInSystemMenuMode()
        {
            try
            {
                NativeMethods.GUITHREADINFO gui;

                if (Misc.ProxyGetGUIThreadInfo(0, out gui))
                {
                    return Misc.IsBitSet(gui.dwFlags, NativeMethods.GUI_SYSTEMMENUMODE);
                }

                return false;
            }
            catch (ElementNotAvailableException)
            {
                return false;
            }
        }

        internal static IntPtr GetSystemMenuHandle(IntPtr hwnd)
        {
            NativeMethods.MENUBARINFO mbi;

            if (GetMenuBarInfo(hwnd, NativeMethods.OBJID_SYSMENU, 0, out mbi) && mbi.hMenu != IntPtr.Zero)
            {
                return mbi.hMenu;
            }

            return IntPtr.Zero;
        }

        #endregion

        //------------------------------------------------------
        //
        //  Internal Fields
        //
        //------------------------------------------------------

        #region Internal Fields

        // Defines the menu item types
        internal enum MenuType : int
        {
            Submenu = 0, //  submenu
            System, //  menubar for the system (This mb has one control only)
            Toplevel, // top-level menu (a.k.a - menubar)
            Context, // context menu (a.k.a shortcut) - brough by right-click on client area or specific object
            SystemPopup, // pop-up that gets displayed when user "invokes" system menu bar's control
        }

        // Win32 Menu class name
        internal const string MenuClassName = "#32768";

        // Time out for showing and disappearance of menus
        internal const int TimeOut = 100;

        #endregion

        //------------------------------------------------------
        //
        //  Private Methods
        //
        //------------------------------------------------------

        #region Private Methods

        private Rect GetBoundingRectangle()
        {
            switch (_type)
            {
                case MenuType.System:
                    {
                        // This is to take in count for a bug in GetMenuBarInfo().  It does not calculate the
                        // rcBar correctly when the WS_EX_LAYOUTRTL extended style is set. GetMenuBarInfo()
                        // assumes SYSMENU is always on the left of the title bar.
                        NativeMethods.MENUBARINFO mbi;
                        if (!GetMenuBarInfo(_hwnd, NativeMethods.OBJID_SYSMENU, 0, out mbi))
                        {
                            throw new ElementNotAvailableException();
                        }

                        int leftEdge = mbi.rcBar.left;
                        int buttonWidth = mbi.rcBar.right - mbi.rcBar.left;
                        int buttonHeight = mbi.rcBar.bottom - mbi.rcBar.top;

                        //
                        // Builds prior to Vista 5359 failed to correctly account for RTL menu layouts.
                        //
                        if ((Environment.OSVersion.Version.Major < 6) && (Misc.IsLayoutRTL(_hwnd)))
                        {
                            // Get the bounding rectangle of the whole title bar to be able to calculate
                            // the true left boundary in this extend style.
                            UnsafeNativeMethods.TITLEBARINFO ti;
                            if (!Misc.ProxyGetTitleBarInfo(_hwnd, out ti))
                            {
                                throw new ElementNotAvailableException();
                            }

                            leftEdge = ti.rcTitleBar.right - buttonWidth;
                        }
                        return new Rect(leftEdge, mbi.rcBar.top, buttonWidth, buttonHeight);
                    }

                case MenuType.Toplevel:
                    {
                        NativeMethods.MENUBARINFO mbi;
                        if (GetMenuBarInfo(_hwnd, NativeMethods.OBJID_MENU, 0, out mbi))
                        {
                            return mbi.rcBar.ToRect(false);
                        }

                        throw new ElementNotAvailableException();
                    }

                default:
                    return base.BoundingRectangle;
            }
        }

        //Returns the SRID of the resource name string
        private string GetLocalizedNameFromType()
        {
            switch (_type)
            {
                case MenuType.Toplevel:
                    return SRID.LocalizedNameWindowsMenuBar;

                case MenuType.System:
                    return SRID.LocalizedNameWindowsSystemMenuBar;

                case MenuType.SystemPopup:
                    return SRID.LocalizedNameWindowsMenu;

                case MenuType.Submenu:
                    return SRID.LocalizedNameWindowsMenu;

                default:
                    return SRID.LocalizedNameWindowsMenu;
            }
        }

        // get a handle to the system menu
        // note - passed in hwnd should be apps hwnd
        //  Returns the system HMENU for the given HWND.
        //
        //  Can't use the Win32 API GetSystemMenu, since that modifies the system
        //  HMENU for the window.
        private static IntPtr GetSystemPopupMenu (IntPtr hwnd)
        {
            NativeMethods.MENUBARINFO mbi;

            // Only the 2 bits are valid in the focusFlags field. Strip the other ones as sometimes
            // they are set to none zero value.
            if (GetMenuBarInfo(hwnd, NativeMethods.OBJID_SYSMENU, 0, out mbi) &&
                mbi.hMenu != IntPtr.Zero &&
                Misc.IsBitSet(mbi.focusFlags, 3))
            {
                return UnsafeNativeMethods.GetSubMenu(mbi.hMenu, 0);
            }

            return IntPtr.Zero;
        }

        // check if passed in menu handle represents windows system menu
        private static bool IsSystemPopupMenu (IntPtr hmenu)
        {
            if (hmenu == IntPtr.Zero)
            {
                return false;
            }

            NativeMethods.GUITHREADINFO gui;

            if (Misc.ProxyGetGUIThreadInfo(0, out gui))
            {
                if (hmenu == GetSystemPopupMenu(gui.hwndActive))
                {
                    return true;
                }
            }

            return false;
        }

        // detect if hwnd corresponds to the submenu
        private static bool IsWindowSubMenu (IntPtr hwnd)
        {
            return (String.Compare(Misc.ProxyGetClassName(hwnd), WindowsMenu.MenuClassName, StringComparison.OrdinalIgnoreCase) == 0);
        }


        private static int GetHighlightedMenuItem(IntPtr hmenu)
        {
            int count = Misc.GetMenuItemCount(hmenu);
            for (int i = 0; i < count; i++)
            {
                int state = UnsafeNativeMethods.GetMenuState(hmenu, i, NativeMethods.MF_BYPOSITION);
                if (Misc.IsBitSet(state, NativeMethods.MF_HILITE))
                {
                    return i;
                }
            }
            return -1;
        }

        private static IntPtr GetPopupHwndForHMenu(IntPtr hmenu)
        {
            IntPtr hwndPopup = IntPtr.Zero;
            for (; ; )
            {
                hwndPopup = Misc.FindWindowEx(IntPtr.Zero, hwndPopup, WindowsMenu.MenuClassName, null);
                if (hwndPopup == IntPtr.Zero)
                    break;
                if (!SafeNativeMethods.IsWindowVisible(hwndPopup))
                    continue;

                // Get the hMenu, and see if it matches...
                IntPtr hmenuTest = HmenuFromHwnd(hwndPopup);
                if (hmenuTest == hmenu)
                    return hwndPopup;
            }
            return IntPtr.Zero;
        }

        // Handle menu specific events
        private static void MenuEvents (IntPtr hwnd, int eventId, object idProp, int idObject, int idChild)
        {
            MenuItem parent = null;

            if (eventId == NativeMethods.EventSystemMenuPopupEnd)
            {
                // EVENT_SYSTEM_MENUPOPUPEND comes with the hwnd for the menu that got closed and hwnd is not valid anymore,
                // hence we cannot use it to obtain any information
                // Solution: MENUPOPUPEND cannot happen without MENUPOPUPSTART, and we will be notified about MENUPOPUPSTART.
                // During the processing of MENUPOPUPSTART we will cache the parent(a.k.a menuItem) of the MENU that got "started"
                // When processing MENUPOPUPEND we will refer to the cached info in order to retrieve the valid-cached parent
                // of the menu that got ended
                // Update 3/9/2005:
                // Cacheing the parent durning the processing of MENUPOPUPSTART is not a option sometimes.  If the menu is already
                // expanded before registring for the ExpandCollapsePropertyChange event is one example.  To solve this cache the
                // parent during the ctor if it has not already been cached.
                MenuParentInfo parentInfo = null;

                while (_expandedMenus.Contains(hwnd) && parent == null)
                {
                    // get the parent info
                    parentInfo = (MenuParentInfo) _expandedMenus [hwnd];

                    // remove object from the hashtable (since menu was collapsed)
                    _expandedMenus.Remove (hwnd);
                    if (SafeNativeMethods.IsWindowVisible (parentInfo._hwndParent))
                    {
                        WindowsMenu menu = null;

                        if (parentInfo._type == MenuType.System)
                        {
                            menu = CreateSystemMenu (parentInfo._hwndParent, null);
                        }
                        else
                        {
                            menu = (WindowsMenu) WindowsMenu.Create (parentInfo._hwndParent, 0);
                        }

                        if (menu != null)
                        {
                            parent = (MenuItem) menu.CreateMenuItem (parentInfo._menuItem);
                        }
                    }
                    else
                    {
                        // NOTE: EVENT_SYSTEM_MENUPOPUPSTART comes one after another
                        // at the same time we can have many EVENT_SYSTEM_MENUPOPUPEND happen at the same time
                        // (e.g. Hierachy of submenus got removed after app lost focus), we will unroll the
                        // hierarchical chain (which would be in the hastable) and raise the AutomationPropertyChangedEvent
                        // only for the first visible parent. It is perfectly possible that there would be no
                        // visible parent (e.g. Hierarchy was rooted at the menuItem that lived on the Context menu)
                        // in this case there would be no event raised.
                        hwnd = parentInfo._hwndParent;
                    }
                }

                // If could not find cached parent information, check to see if the system menu has been
                // cached.  If so use the system menu as the parent.
                if (parent == null && _expandedMenus.Contains(IntPtr.Zero))
                {
                    // get the parent info
                    parentInfo = (MenuParentInfo)_expandedMenus[IntPtr.Zero];

                    WindowsMenu menu = null;
                    if (parentInfo._type == MenuType.System)
                    {
                        menu = CreateSystemMenu(parentInfo._hwndParent, null);
                    }
                    if (menu != null)
                    {
                        parent = (MenuItem)menu.CreateMenuItem(0);
                    }
                }
            }
            else
            {
                if (!IsWindowSubMenu(hwnd))
                {
                    // Event does not belong to win32 menu
                    return;
                }

                if (IsInSystemMenuMode())
                {
                    parent = (MenuItem)GetSystemPopupParent();
                }
                else
                {
                    parent = GetHierarchyParent(hwnd);
                }
            }

            // Raise an event
            if (parent != null)
            {
                object propertyValue = ((IExpandCollapseProvider)parent).ExpandCollapseState;

                if (propertyValue != null && propertyValue != AutomationElement.NotSupported)
                {
                    AutomationInteropProvider.RaiseAutomationPropertyChangedEvent(parent, new AutomationPropertyChangedEventArgs((AutomationProperty)idProp, null, propertyValue));
                }
            }
        }

        // Detect if requested event corresponds to the events supported by menu
        private static bool MenuRelatedEvent (AutomationEvent eventId, AutomationProperty [] aidProps)
        {
            // MenuItem supports ExpandCollapseState property change event only
            if (eventId != AutomationElement.AutomationPropertyChangedEvent)
            {
                return false;
            }

            int count = aidProps.Length;

            for (int i = 0; i < count; i++)
            {
                if (aidProps[i] == ExpandCollapsePattern.ExpandCollapseStateProperty)
                {
                    return true;
                }
            }

            return false;
        }

        // Return menuItem object which is a hierarchical parent of the given menu (specified via hwnd)
        // return NULL in the case when menu does not have a parent (e.g. Context menu)
        // NOTE: this method should not be called for the menuItem that lives on the System or Menubar
        private static ProxyFragment GetSystemPopupParent ()
        {
            // Parent lives on menuBar or parent lives on another submenu
            // or parent lives on the context or parent lives on system popup menu ...
            // Start from menuBar
            NativeMethods.GUITHREADINFO gui;

            if (!Misc.ProxyGetGUIThreadInfo(0, out gui))
            {
                return null;
            }

            NonClientArea nonClientArea = new NonClientArea (gui.hwndActive);
            WindowsTitleBar titleBar = (WindowsTitleBar) nonClientArea.CreateNonClientChild (NonClientArea.NonClientItem.TitleBar);
            ProxyFragment systemMenu = (ProxyFragment) titleBar.CreateTitleBarChild (WindowsTitleBar._systemMenu);

            return (ProxyFragment) systemMenu.GetFirstChild ();
        }

        #endregion

        //------------------------------------------------------
        //
        //  Private Fields
        //
        //------------------------------------------------------

        #region Private Fields

        private IntPtr _hmenu;

        private MenuType _type;

        // Menu-specific events
        private readonly static WinEventTracker.EvtIdProperty [] _menuEvents = new WinEventTracker.EvtIdProperty [] {
                new WinEventTracker.EvtIdProperty(NativeMethods.EventSystemMenuPopupStart, ExpandCollapsePattern.ExpandCollapseStateProperty),
                new WinEventTracker.EvtIdProperty(NativeMethods.EventSystemMenuPopupEnd, ExpandCollapsePattern.ExpandCollapseStateProperty),
                new WinEventTracker.EvtIdProperty(NativeMethods.EventObjectInvoke, InvokePattern.InvokedEvent)
            };

        private static Hashtable _expandedMenus = new Hashtable(5);

        // counter of elements interested in menu-related events
        private static int _eventListeners = 0;

        #endregion

        // ------------------------------------------------------
        //
        //  MenuParentInfo Private Class
        //
        //------------------------------------------------------

        #region MenuParentInfo

        private class MenuParentInfo
        {
            internal IntPtr _hwndParent; // menu's hwnd on which menuItem lives
            internal int _menuItem; // menuItem index
            internal MenuType _type; // type of the menu on which menuItem lives

            internal MenuParentInfo (IntPtr hwndParent, int menuItem, MenuType type)
            {
                _hwndParent = hwndParent;
                _menuItem = menuItem;
                _type = type;
            }
        }

        #endregion

        // ------------------------------------------------------
        //
        //  MenuItem Internal Class
        //
        //------------------------------------------------------

        #region MenuItem

        // Class implementation for the WindowsMenuItemProxy.
        internal class MenuItem : ProxyFragment, IInvokeProvider, IExpandCollapseProvider, ISelectionItemProvider, IToggleProvider
        {
            // ------------------------------------------------------
            //
            // Constructors
            //
            // ------------------------------------------------------

            #region Constructors

            internal MenuItem (IntPtr hwnd, ProxyFragment parent, int item, IntPtr hmenu, WindowsMenu.MenuType type)
            : base(hwnd, parent, item)
            {
                _fNonClientAreaElement = true;
                _menuType = type;
                _hmenu = hmenu;
                _type = GetMenuItemType ();

                if (_type == MenuItemType.Spacer)
                {
                    _cControlType = ControlType.Separator;
                    _sAutomationId = "Separator " + (_item + 1).ToString(CultureInfo.InvariantCulture); // This string is a non-localizable string
                    _fIsContent = false;
                }
                else
                {
                    _cControlType = ControlType.MenuItem;
                    GetItemId(ref _sAutomationId);
                }
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
                    // Use ProviderOwnsSetFocus to take complete control of set focus,
                    // don't use UIA's default behavior, which would call SetFocus on,
                    // the HWND and cause it to collapse.
                    return base.ProviderOptions | ProviderOptions.ProviderOwnsSetFocus;
                }
            }

            // Returns a pattern interface if supported.
            internal override object GetPatternProvider (AutomationPattern iid)
            {
                // if item is !visible or item is Spacer than none of the patterns are supported
                if (!SafeNativeMethods.IsWindowVisible (_hwnd) || _type == MenuItemType.Spacer)
                {
                    return null;
                }

                if (iid == ExpandCollapsePattern.Pattern && _type == MenuItemType.SubMenu)
                {
                    return this;
                }
                else if (iid == InvokePattern.Pattern && _type == MenuItemType.Command)
                {
                    return this;
                }
                else if (iid == SelectionItemPattern.Pattern && _type == MenuItemType.Command && IsRadioCheck())
                {
                    return this;
                }
                else if (iid == TogglePattern.Pattern && _type == MenuItemType.Command && IsChecked() && !IsRadioCheck())
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
                    if (_menuType == WindowsMenu.MenuType.System)
                    {
                        NativeMethods.MENUBARINFO mbi;

                        if (GetMenuBarInfo(_hwnd, NativeMethods.OBJID_SYSMENU, 0, out mbi))
                        {
                            int leftEdge = mbi.rcBar.left;
                            int buttonWidth = mbi.rcBar.right - mbi.rcBar.left;
                            int buttonHeight = mbi.rcBar.bottom - mbi.rcBar.top;

                            //
                            // Builds prior to Vista 5359 failed to correctly account for RTL menu layouts.
                            //
                            if ((Environment.OSVersion.Version.Major < 6) && (Misc.IsLayoutRTL(_hwnd)))
                            {
                                // Get the bounding rectangle of the whole title bar to be able to calculate
                                // the true left boundary in this extend style.
                                UnsafeNativeMethods.TITLEBARINFO ti;
                                if (!Misc.ProxyGetTitleBarInfo(_hwnd, out ti))
                                {
// Suppress Property get methods should not throw exceptions for internal getter
#pragma warning suppress 6503
                                    throw new ElementNotAvailableException();
                                }

                                leftEdge = ti.rcTitleBar.right - buttonWidth;
                            }
                            return new Rect(leftEdge, mbi.rcBar.top, buttonWidth, buttonHeight);
                        }
                    }
                    else
                    {
                        NativeMethods.Win32Rect rc;
                        if (Misc.GetMenuItemRect(_hwnd, _hmenu, _item, out rc))
                        {
                            return rc.ToRect(false);
                        }
                    }

                    return Rect.Empty;
                }
            }

            //Gets the localized name
            internal override string LocalizedName
            {
                get
                {
                    // It does not look like Winforms support the AccessibleName for standard menus or there items.
                    // If MenuStrips are going to be supported by this proxy, will need to added code to check
                    // if this is winforms menu item and use the AccessibleName if it is set.

                    if (_menuType == WindowsMenu.MenuType.System)
                    {
                        return SR.Get(SRID.LocalizedNameWindowsSystemMenuItem);
                    }

                    string menuRawText = Text;

                    if (string.IsNullOrEmpty(menuRawText))
                    {
                        return null;
                    }

                    // Get the full string with the & removed
                    menuRawText = Misc.StripMnemonic(menuRawText);

                    // A tab, '\t', is usually used to separate the menu string from the accelerator.  If a
                    // tab is found assume that the menu string is before the tab.
                    int pos = menuRawText.IndexOf('\t');
                    if (pos > 0)
                    {
                        return menuRawText.Substring(0, pos);
                    }

                    // Try to remove the Ctrl or Alt, etc at the end of the string if there
                    // Be caution modifying this code, it must miror the code for AcceleratorKeyProperty

                    // Try to look for a combination Ctrl or Alt + something
                    string keyCtrl = SR.Get(SRID.KeyCtrl);
                    string keyControl = SR.Get(SRID.KeyControl);
                    string keyAlt = SR.Get(SRID.KeyAlt);
                    string keyShift = SR.Get(SRID.KeyShift);
                    string keyWin = SR.Get(SRID.KeyWinKey);

                    string menuText = menuRawText.ToLower(CultureInfo.InvariantCulture);
                    string accelerator;

                    if ((accelerator = AccelatorKeyCtrl(keyCtrl.ToLower(CultureInfo.InvariantCulture), keyCtrl + " + ", menuText, menuRawText, out pos)) != null ||
                        (accelerator = AccelatorKeyCtrl(keyControl.ToLower(CultureInfo.InvariantCulture), keyCtrl + " + ", menuText, menuRawText, out pos)) != null ||
                        (accelerator = AccelatorKeyCtrl(keyAlt.ToLower(CultureInfo.InvariantCulture), keyAlt + " + ", menuText, menuRawText, out pos)) != null ||
                        (accelerator = AccelatorKeyCtrl(keyShift.ToLower(CultureInfo.InvariantCulture), keyShift + " + ", menuText, menuRawText, out pos)) != null ||
                        (accelerator = AccelatorKeyCtrl(keyWin.ToLower(CultureInfo.InvariantCulture), keyWin + " + ", menuText, menuRawText, out pos)) != null)
                    {
                        return menuRawText.Substring(0, SkipMenuSpaceChar(menuText, pos));
                    }

                    // Try to look for a Fxx
                    accelerator = AccelatorFxx(menuText);
                    if (!string.IsNullOrEmpty(accelerator))
                    {
                        pos = menuText.LastIndexOf(accelerator, StringComparison.OrdinalIgnoreCase);
                        if (pos >= 0)
                        {
                            return menuRawText.Substring(0, SkipMenuSpaceChar(menuText, pos));
                        }
                        else
                        {
                            // Wrong logic, we should be able to find the Fxx combination we just built
                            System.Diagnostics.Debug.Assert(false, "Cannot find back the accelerator in the menu!");
                            return menuRawText;
                        }
                    }

                    // Look for a bunch of Predefined keyword
                    string[] keywordsAccelerators = GetKeywordsAccelerators();

                    for (int i = 0; i < keywordsAccelerators.Length; i++)
                    {
                        pos = menuText.LastIndexOf(keywordsAccelerators[i], StringComparison.OrdinalIgnoreCase);
                        if (pos > 0 && pos + keywordsAccelerators[i].Length == menuText.Length && (menuText[pos - 1] == '\a' || menuText[pos - 1] == '\t'))
                        {
                            return menuRawText.Substring(0, SkipMenuSpaceChar(menuText, pos));
                        }
                    }

                    return menuRawText;
                }
            }

            // Process all the Element Properties
            internal override object GetElementProperty (AutomationProperty idProp)
            {
                if (idProp == AutomationElement.AcceleratorKeyProperty)
                {
                    string acceleratorKey = AcceleratorKey;
                    if (!string.IsNullOrEmpty(acceleratorKey))
                        return AcceleratorKey;
                }
                else if (idProp == AutomationElement.IsEnabledProperty)
                {
                    // If an element in the parent chain is disabled there is not point in checking the menu
                    if (!Misc.IsEnabled(_hwnd))
                        return false;

                    return IsEnabledMenu;
                }
                else if (idProp == AutomationElement.AccessKeyProperty)
                {
                    return GetAccessKey();
                }
                else if (idProp == AutomationElement.IsKeyboardFocusableProperty)
                {
                    return _type != MenuItemType.Spacer;
                }
                else if (idProp == AutomationElement.HasKeyboardFocusProperty)
                {
                    // The check for the focused window fails!!!
                    return IsFocused ();
                }
                else if (idProp == AutomationElement.IsOffscreenProperty)
                {
                    // Even if the menu item that is the parent to a popup menu is off the screen
                    // the popup menu will be displayed on the screen, so must override the
                    // default action of checking the parent.
                    Rect itemRect = BoundingRectangle;

                    if (itemRect.IsEmpty)
                    {
                        return true;
                    }

                    // if this element is not on any monitor than it is off the screen.
                    NativeMethods.Win32Rect itemWin32Rect = new NativeMethods.Win32Rect(itemRect);
                    return UnsafeNativeMethods.MonitorFromRect(ref itemWin32Rect, UnsafeNativeMethods.MONITOR_DEFAULTTONULL) == IntPtr.Zero;
                }

                return base.GetElementProperty (idProp);
            }

            // Sets the focus to this item.
            // Note: parent should be focused at the time of call
            internal override bool SetFocus()
            {
                if (!SafeNativeMethods.IsWindowVisible(_hwnd) || _type == MenuItemType.Spacer)
                {
                    return false;
                }

                // Occasionally this fails due to timing issues.
                // To massively lower the probability of this occurring we try it several times.
                for (int i = 0; i < 4; i++)
                {
                    // Put ourselves in the menu mode (if already in the menu mode
                    // cancel out from it and get back to it)
                    if ((_menuType == MenuType.Toplevel || _menuType == MenuType.System))
                    {
                        if (Misc.InMenuMode())
                        {
                            IntPtr hwndFocus = Misc.GetFocusedWindow();

                            // Detects if the Menu is expanded (popup)
                            // If this is the case, get entirely out of the Menu mode to get rid of the popup
                            if (hwndFocus != IntPtr.Zero && hwndFocus != _hwnd)
                            {
                                SendAltKey();
                                WaitForPopupMenu(false);
                                System.Threading.Thread.Sleep(1);
                            }
                        }

                        // Set the focus and the menu bar if it is not already the case
                        if (!Misc.InMenuMode() && !MenuMode(true))
                        {
                            return false;
                        }
                    }
                    else
                    {
                        if (!Misc.InMenuMode())
                            return false;
                    }
                        // It is possible to use easily the hot key because they do
                        // both selection and expand. For now, limit ourself
                        // to use moves keyboard in the menus
#if future

                // If menuItem has a hotkey, use it
                char hotKey;

                // This scheme does not work for the MFC. It terminates the application
                // Further more an accelerator might be defined twice and this is not handled
                // Comment this code out and let the default behavior go through testing.
                if (_menuType != MenuType.Toplevel && '\0' != (hotKey = HotKeyCheckNoDups))
                {
                    short convert = UnsafeNativeMethods.VkKeyScan (hotKey);

                    if (-1 != convert)
                    {
                        byte vk = (byte) (convert & 0xff);

                        // NOTE: Do not worry if we are already in the menu mode
                        // sending Alt will take care of everything in this case
                        Input.SendKeyboardInputVK (UnsafeNativeMethods.VK_MENU, true);
                        Input.SendKeyboardInputVK (vk, true);
                        Input.SendKeyboardInputVK (vk, false);
                        Input.SendKeyboardInputVK (UnsafeNativeMethods.VK_MENU, false);
                    }
                }
                else
#endif
                    {
                        // No Accelerator - use the keyboard to move to a given element
                        if (!KeyBoardNavigate())
                        {
                            System.Threading.Thread.Sleep(100);
                            continue;
                        }
                    }

                    // Make sure that the our hwnd has the focus
                    // Wait for a few millisecond for this operation to be completed
                    UInt32 dwTicks = SafeNativeMethods.GetTickCount();
                    UInt32 dwDelta = 0;

                    // Wait until the action has been completed
                    while (!Misc.IsBitSet(UnsafeNativeMethods.GetMenuState(_hmenu, _item, NativeMethods.MF_BYPOSITION), NativeMethods.MF_HILITE) &&
                           (dwDelta = SubtractTicks(SafeNativeMethods.GetTickCount(), dwTicks)) <= WindowsMenu.TimeOut)
                    {
                        System.Threading.Thread.Sleep(1);
                    }

                    if (dwDelta <= WindowsMenu.TimeOut)
                        return true;

                    System.Threading.Thread.Sleep(100);
                }

                return false;
            }

            internal override string GetAccessKey()
            {
                MenuType type = ((WindowsMenu)_parent)._type;

                switch (type)
                {
                    case MenuType.System:
                        {
                            return SR.Get(SRID.KeyAlt) + " + " + SR.Get(SRID.KeySpace);
                        }
                    case MenuType.Submenu:
                    case MenuType.SystemPopup:
                        {
                            string text = Text;
                            return string.IsNullOrEmpty(text) ? null : SubMenuAccessKey(text);
                        }
                    default:
                        return Misc.AccessKey(Text);
                }
            }

            internal static string SubMenuAccessKey(string s)
            {
                // Get the index of the shortcut
                int iPosShortCut = s.IndexOf('&');

                // Did we found an & or is it at the end of the string
                if (iPosShortCut < 0 || iPosShortCut + 1 >= s.Length)
                {
                    return null;
                }

                // Build the result string
                return s[iPosShortCut + 1].ToString();
            }

            #endregion

            #region ProxyFragment Interface

            // Returns the next sibling element in the raw hierarchy.
            // Peripheral controls have always negative values.
            // Returns null if no next child
            internal override ProxySimple GetNextSibling (ProxySimple child)
            {
                // If the parent is a MenuItem then there is one child
                // a popup Menu. A Popup Menu cannot have siblings
                if (child is WindowsMenu)
                {
                    return null;
                }

                // A child is a Menu Item
                return ((MenuItem) child).PreviousMenuItem;
            }

            // Returns the previous sibling element in the raw hierarchy.
            // Peripheral controls have always negative values.
            // Returns null is no previous
            internal override ProxySimple GetPreviousSibling (ProxySimple child)
            {
                // If the parent is a MenuItem then there is one child
                // a popup Menu. A Popup Menu cannot have siblings
                if (child is WindowsMenu)
                {
                    return null;
                }

                // A child is a Menu Item
                return ((MenuItem) child).NextMenuItem;
            }

            // Returns the first child element in the raw hierarchy.
            internal override ProxySimple GetFirstChild ()
            {
                IntPtr submenu = _menuType == MenuType.System ? _hmenu : UnsafeNativeMethods.GetSubMenu (_hmenu, _item);
                if (submenu == IntPtr.Zero)
                {
                    return null;
                }

                IntPtr hwndSubmenu = WindowsMenu.WindowFromSubmenu (submenu);

                if (hwndSubmenu != IntPtr.Zero)
                {
                    WindowsMenu.MenuType type = WindowsMenu.GetSubMenuType (hwndSubmenu, submenu);
                    return new WindowsMenu (hwndSubmenu, null, submenu, type, 0);
                }
                return null;
            }

            // Returns the last child element in the raw hierarchy.
            internal override ProxySimple GetLastChild ()
            {
                IntPtr submenu = _menuType == MenuType.System ? _hmenu : UnsafeNativeMethods.GetSubMenu (_hmenu, _item);
                if (submenu == IntPtr.Zero)
                {
                    return null;
                }

                IntPtr hwndSubmenu = WindowsMenu.WindowFromSubmenu(submenu);

                if (hwndSubmenu != IntPtr.Zero)
                {
                    WindowsMenu.MenuType type = WindowsMenu.GetSubMenuType (hwndSubmenu, submenu);
                    return new WindowsMenu (hwndSubmenu, null, submenu, type, 0);
                }
                return null;
            }

            // Returns a Proxy element corresponding to the specified screen coordinates.
            internal override ProxySimple ElementProviderFromPoint (int x, int y)
            {
                // Hit test on all the descendent
                for (MenuItem menuCur = FirstOrLastChild (true); menuCur != null; menuCur = menuCur.NextMenuItem)
                {
                    ProxySimple menuItem = menuCur.ElementProviderFromPoint(x, y);

                    if (menuItem != null)
                    {
                        return menuItem;
                    }
                }

                Rect rc = BoundingRectangle;
                if (Misc.PtInRect(ref rc, x, y))
                {
                    return this;
                }

                return null;
            }

            #endregion ProxyFragment Interface

            #region Invoke Pattern

            // Select the desired menuItem.  This will expand a menu item as well as
            // invoke a menu item.  The enter key is used here to make sure in the
            // case where we are not expanding that the menu get dissmissed properly.
            void IInvokeProvider.Invoke ()
            {
                // Make sure that the control is enabled
                if (!IsEnabledMenu)
                {
                    throw new ElementNotEnabledException();
                }

                SetFocus();

                // sending enter key to the currently selected item
                ExpandViaEnter();
                WaitForMenuMode(false);
            }

            #endregion

            #region ExpandCollapse Pattern

            // Show all Children
            void IExpandCollapseProvider.Expand ()
            {
                if (!IsEnabledMenu)
                {
                    throw new ElementNotEnabledException();
                }

                if (!IsSubmenuCollapsed ())
                {
                    return;
                }

                // For some menus we need to have a right to input
                if (ReadyForInput ())
                {
                    // sytem menu item has its own way to expand
                    // this should preceed all other expand calls
                    if (IsSystemMenuItem())
                    {
                        if (ExpandCollapseSystem(true))
                            return;

                        throw new InvalidOperationException(SR.Get(SRID.OperationCannotBePerformed));
                    }

                    // top level
                    if (_menuType == WindowsMenu.MenuType.Toplevel)
                    {
                        if (ExpandTopLevelMenu())
                            return;

                        throw new InvalidOperationException(SR.Get(SRID.OperationCannotBePerformed));
                    }

                    // submenu
                    if( ExpandSubmenu ())
                        return;
                }

                throw new InvalidOperationException(SR.Get(SRID.OperationCannotBePerformed));
            }

            // Hide all Children
            void IExpandCollapseProvider.Collapse()
            {
                if (!IsEnabledMenu)
                {
                    throw new ElementNotEnabledException();
                }

                if (IsSubmenuCollapsed ())
                {
                    return;
                }

                // For some menus we need to have a right to input
                if (ReadyForInput ())
                {
                    // sytem menu item has its own way to expand
                    // this should be preceed all other collapse calls
                    if (IsSystemMenuItem ())
                    {
                        if (ExpandCollapseSystem(false))
                            return;

                        throw new InvalidOperationException(SR.Get(SRID.OperationCannotBePerformed));
                    }

                    // top-level
                    if (_menuType == WindowsMenu.MenuType.Toplevel)
                    {
                        if (CollapseTopLevelMenu())
                            return;

                        throw new InvalidOperationException(SR.Get(SRID.OperationCannotBePerformed));
                    }

                    // submenu
                    if (CollapseSubmenu())
                        return;
                }

                // probably should throw
                throw new InvalidOperationException(SR.Get(SRID.OperationCannotBePerformed));
            }

            // Indicates an elements current Collapsed or Expanded state
            ExpandCollapseState IExpandCollapseProvider.ExpandCollapseState
            {
                get
                {
                    return (IsSubmenuCollapsed()) ? ExpandCollapseState.Collapsed : ExpandCollapseState.Expanded;
                }
            }

            #endregion ExpandCollapse Pattern

            #region SelectionItem Pattern

            // Selects this element
            void ISelectionItemProvider.Select()
            {
                // Make sure that the control is enabled
                if (!IsEnabledMenu)
                {
                    throw new ElementNotEnabledException();
                }

                SetFocus();

                // sending enter key to the currently selected item
                ExpandViaEnter();
            }

            // Adds this element to the selection
            void ISelectionItemProvider.AddToSelection()
            {
                throw new InvalidOperationException(SR.Get(SRID.DoesNotSupportMultipleSelection));
            }

            // Removes this element from the selection
            void ISelectionItemProvider.RemoveFromSelection()
            {
                throw new InvalidOperationException(SR.Get(SRID.DoesNotSupportMultipleSelection));
            }

            // True if this element is part of the the selection
            bool ISelectionItemProvider.IsSelected
            {
                get
                {
                    return IsChecked() ? true : false;
                }
            }

            // Returns the container for this element
            IRawElementProviderSimple ISelectionItemProvider.SelectionContainer
            {
                get
                {
                    return GetParent();
                }
            }

            #endregion SelectionItem Pattern

            #region IToggleProvider

            void IToggleProvider.Toggle()
            {
                // Make sure that the control is enabled
                if (!IsEnabledMenu)
                {
                    throw new ElementNotEnabledException();
                }

                SetFocus();

                // sending enter key to the currently selected item
                ExpandViaEnter();
            }

            ToggleState IToggleProvider.ToggleState
            {
                get
                {
                    return IsChecked() ? ToggleState.On : ToggleState.Off;
                }
            }

            #endregion IToggleProvider

            //------------------------------------------------------
            //
            //  Protected Methods
            //
            //------------------------------------------------------

            #region Protected Methods

            // This routine is only called on elements belonging to an hwnd that has the focus.
            protected override bool IsFocused()
            {
                return Misc.IsBitSet(UnsafeNativeMethods.GetMenuState(_hmenu, _item, NativeMethods.MF_BYPOSITION), NativeMethods.MF_HILITE);
            }

            #endregion

            // ------------------------------------------------------
            //
            // Private Methods
            //
            // ------------------------------------------------------

            #region Private Methods

            private bool IsChecked()
            {
                return Misc.IsBitSet(UnsafeNativeMethods.GetMenuState(_hmenu, _item, NativeMethods.MF_BYPOSITION), NativeMethods.MF_CHECKED);
            }

            private bool IsRadioCheck()
            {
                NativeMethods.MENUITEMINFO menuItemInfo = new NativeMethods.MENUITEMINFO();
                menuItemInfo.cbSize = Marshal.SizeOf(menuItemInfo.GetType());
                menuItemInfo.fMask = NativeMethods.MIIM_FTYPE | NativeMethods.MIIM_SUBMENU | NativeMethods.MIIM_STATE;

                if (!Misc.GetMenuItemInfo(_hmenu, _item, true, ref menuItemInfo))
                {
                    return false;
                }

                return (Misc.IsBitSet(menuItemInfo.fType, NativeMethods.MFT_RADIOCHECK) && menuItemInfo.hbmpChecked == IntPtr.Zero);
            }

            // Also needed for SetFocus Keyboard navigation
            private static bool IsSeparator (IntPtr hmenu, int position)
            {
                NativeMethods.MENUITEMINFO menuItemInfo = new NativeMethods.MENUITEMINFO ();
                menuItemInfo.cbSize = Marshal.SizeOf (menuItemInfo.GetType ());
                menuItemInfo.fMask = NativeMethods.MIIM_FTYPE | NativeMethods.MIIM_SUBMENU | NativeMethods.MIIM_STATE;

                if (!Misc.GetMenuItemInfo(hmenu, position, true, ref menuItemInfo))
                {
                    return false;
                }

                return (Misc.IsBitSet(menuItemInfo.fType, NativeMethods.MF_SEPARATOR) ||
                        Misc.IsBitSet(menuItemInfo.fType, NativeMethods.MF_MENUBARBREAK) ||
                        Misc.IsBitSet(menuItemInfo.fType, NativeMethods.MF_MENUBREAK));
            }

            //Gets the localized keywords accelerators
            private string[] GetKeywordsAccelerators()
            {
                return new string[] {
                    SR.Get(SRID.KeyHome),
                    SR.Get(SRID.KeyEnd),
                    SR.Get(SRID.KeyDel),
                    SR.Get(SRID.KeyDelete),
                    SR.Get(SRID.KeyIns),
                    SR.Get(SRID.KeyInsert),
                    SR.Get(SRID.KeyPageUp),
                    SR.Get(SRID.KeyPageDown),
                    SR.Get(SRID.KeyEsc),
                    SR.Get(SRID.KeyScrLk),
                    SR.Get(SRID.KeyPause),
                    SR.Get(SRID.KeySysRq),
                    SR.Get(SRID.KeyPrtScn),
                    SR.Get(SRID.KeyTab),
                    SR.Get(SRID.KeyHelp),
                };
            }

            // Retrieve type of menu item
            private MenuItemType GetMenuItemType ()
            {
                if (_menuType == WindowsMenu.MenuType.System)
                {
                    return MenuItemType.SubMenu;
                }

                NativeMethods.MENUITEMINFO menuItemInfo = new NativeMethods.MENUITEMINFO();
                menuItemInfo.cbSize = Marshal.SizeOf(menuItemInfo.GetType());
                menuItemInfo.fMask = NativeMethods.MIIM_FTYPE | NativeMethods.MIIM_SUBMENU | NativeMethods.MIIM_STATE;

                if (Misc.GetMenuItemInfo(_hmenu, _item, true, ref menuItemInfo))
                {
                    if (menuItemInfo.hSubMenu != IntPtr.Zero)
                    {
                        return MenuItemType.SubMenu;
                    }

                    if (Misc.IsBitSet(menuItemInfo.fType, NativeMethods.MF_SEPARATOR) ||
                        Misc.IsBitSet(menuItemInfo.fType, NativeMethods.MF_MENUBARBREAK) ||
                        Misc.IsBitSet(menuItemInfo.fType, NativeMethods.MF_MENUBREAK))
                    {
                        return MenuItemType.Spacer;
                    }
                }

                return MenuItemType.Command; // Everything else
            }

            // Detect if a submenu is collapsed (not expanded).
            private bool IsSubmenuCollapsed()
            {
                // Not a submenu
                if (_type != MenuItemType.SubMenu)
                {
                    return false;
                }

                if (IsInSystemMenuMode())
                {
                    IntPtr hwndSubMenu = Misc.FindWindowEx(IntPtr.Zero, IntPtr.Zero, MenuClassName, null);
                    return (hwndSubMenu == IntPtr.Zero || !SafeNativeMethods.IsWindowVisible(hwndSubMenu));
                }
                else
                {
                    IntPtr submenu = UnsafeNativeMethods.GetSubMenu(_hmenu, _item);
                    return (IntPtr.Zero == WindowsMenu.WindowFromSubmenu(submenu));
                }
            }

            // Expand menu that is a child of the top-level menuItem
            private bool ExpandTopLevelMenu ()
            {
                // Put us in menu mode and expand the Menu via the Enter key
                if (SetFocus())
                {
                    ExpandViaEnter();
                    return WaitForPopupMenu(true);
                }

                return false;
            }

            // Expand menu that is a child of the top-level menuItem
            private bool CollapseTopLevelMenu ()
            {
                SendAltKey ();
                if (WaitForPopupMenu(false))
                {
                    // Wait for the Highlighted menu to be gone
                    // this avoids odd timing issue
                    return WaitForNoFocus();
                }

                return false;
            }

            // Expand the submenu
            private bool ExpandSubmenu ()
            {
                // Since IsSubmenuCollapsed returns true for null or invisible menus, this code
                // should return false if the window is already invisible.
                 if (!SafeNativeMethods.IsWindowVisible (_hwnd))
                 {
                     return false;
                 }

                // In order to expand the submenu we will send a hotkey to the menuItem that shows it
                // But before we can do this we need to make sure that there are no other submenus
                // open that can interfere with us.
                // If there any other submenu open we need to collapse them before sending a hotkey
                // collaps all the submenus (if any) that go after us
                if (CollapseSubmenusAfter ())
                {
                    char hotKey = HotKeyCheckNoDups;

                    if ('\0' != hotKey)
                    {
                        short convert = UnsafeNativeMethods.VkKeyScan (hotKey);

                        if (-1 != convert)
                        {
                            short vk = convert;

                            Input.SendKeyboardInputVK(vk, true);
                            Input.SendKeyboardInputVK(vk, false);
                            return WaitForPopupMenu (true);
                        }
                    }

                    // Bad dev did not put any hot-keys
                    // Navigate to the menu item and send enter key
                    if (KeyBoardNavigate ())
                    {
                        ExpandViaEnter ();
                        return WaitForPopupMenu (true);
                    }
                }

                return false;
            }

            // Collapse the submenu
            private bool CollapseSubmenu ()
            {
                // Since IsSubmenuCollapsed returns true for invisible menus, this code
                // can return true if the window is already invisible.
                if (!SafeNativeMethods.IsWindowVisible(_hwnd))
                {
                    return false;
                }

                // In order to collapse the submenu we will send an ESC
                // But before we can do this we need to make sure that there are no other submenus
                // open that can interfere with us.
                // If there any other submenu open we need to collapse them before sending our ESC
                if (CollapseSubmenusAfter ())
                {
                    return WaitForPopupMenu (false);
                }

                return false;
            }

            // Collapses all the submenus that follow _hMenu in the hierarchy
            private bool CollapseSubmenusAfter ()
            {
                // Submenus that were brough after us, are located before us in top-level windows hierarchy
                int afterUsCounter = 0;

                for (IntPtr hwndSubMenu = Misc.FindWindowEx(IntPtr.Zero, IntPtr.Zero, WindowsMenu.MenuClassName, null);
                     hwndSubMenu != _hwnd;
                     hwndSubMenu = Misc.FindWindowEx(IntPtr.Zero, hwndSubMenu, WindowsMenu.MenuClassName, null))
                {
                    // At this point all that left is to bail out
                    if (hwndSubMenu == IntPtr.Zero)
                    {
                        return false;
                    }

                    if (SafeNativeMethods.IsWindowVisible (hwndSubMenu))
                    {
                        afterUsCounter++;
                    }
                }

                // now collapse menus that follow after us
                while (afterUsCounter > 0)
                {
                    SingleCollapse ();
                    afterUsCounter--;
                }

                return true;
            }

            // expands or collapses the systemmenupop
            private bool ExpandCollapseSystem(bool fExpand)
            {
                if (fExpand)
                {
                    Misc.PostMessage(_hwnd, NativeMethods.WM_SYSCOMMAND, (IntPtr)NativeMethods.SC_KEYMENU, (IntPtr)Convert.ToInt32(' '));
                }
                else
                {
                    SendAltKey();
                }

                // Wait for a few millisecond for this operation to be completed
                return WaitForMenuMode(fExpand);
            }

            private MenuItem FirstOrLastChild (bool returnFirstChild)
            {
                // Return first or last menuItem on the menu that this menuItem will drop
                if (_type == MenuItemType.SubMenu && IsEnabledMenu)
                {
                    IntPtr hSubmenu = UnsafeNativeMethods.GetSubMenu(_hmenu, _item);
                    if (hSubmenu == IntPtr.Zero)
                    {
                        return null;
                    }
                    IntPtr hwndSubmenu = WindowsMenu.WindowFromSubmenu(hSubmenu);

                    if (hwndSubmenu != IntPtr.Zero)
                    {
                        WindowsMenu.MenuType type = WindowsMenu.GetSubMenuType(hwndSubmenu, hSubmenu);
                        WindowsMenu parent = new WindowsMenu(hwndSubmenu, null, hSubmenu, type, 0);
                        int childIndex = (returnFirstChild) ? 0 : Misc.GetMenuItemCount(hSubmenu) - 1;

                        return new MenuItem(hwndSubmenu, parent, childIndex, hSubmenu, type);
                    }
                    return null;
                }

                return null;
            }

            // Put us in menu mode and expand the Menu via the Enter key
            // Wait for a few milliseconds for the menu to be expanded before
            // exiting
            private bool WaitForPopupMenu (bool fShow)
            {
                UInt32 dwTicks = SafeNativeMethods.GetTickCount (), dwDelta = 0;

                // Get out through a timer
                while (true)
                {
                    // Wait for a few milliseconds for this operation to be completed
                    System.Threading.Thread.Sleep(10);

                    // Wait until at least one child is there
                    MenuItem firstMenu = FirstOrLastChild (true);

                    // Wait until the action has been completed
                    // If fShow == true, also make sure last child is present.
                    // (This makes it more certain that the menu is done with initialization.)
                    if ((fShow && firstMenu != null && FirstOrLastChild(false) != null
                            || !fShow && firstMenu == null)
                        || (dwDelta = SubtractTicks (SafeNativeMethods.GetTickCount (), dwTicks)) >= WindowsMenu.TimeOut)
                    {
                        return dwDelta < WindowsMenu.TimeOut;
                    }
                }
            }

            // Wait for the focus to be gone or a time out before proceeding
            internal bool WaitForNoFocus()
            {
                // Wait for a few millisecond for this operation to be completed
                UInt32 dwTicks = SafeNativeMethods.GetTickCount();

                // Wait until the action has been completed
                do
                {
                    int item = 0;
                    int cItems = Misc.GetMenuItemCount(_hmenu);

                    // Loops on all the MenuItems
                    for (item = 0; item < cItems && !Misc.IsBitSet(UnsafeNativeMethods.GetMenuState(_hmenu, item, NativeMethods.MF_BYPOSITION), NativeMethods.MF_HILITE); item++)
                    {
                    }

                    if (item == cItems)
                    {
                        return true;
                    }
                    // Check if we run out of time
                } while (SubtractTicks(SafeNativeMethods.GetTickCount(), dwTicks) <= WindowsMenu.TimeOut);

                return false;
            }

            // Last can wrap around, make it a ulong if this happens
            private static UInt32 SubtractTicks (UInt32 last, UInt32 first)
            {
                if (last < first)
                {
                    return (UInt32) ((ulong) last + (ulong) UInt32.MaxValue + 1 - first);
                }

                return last - first;
            }

            // Make sure that our window is an active one
            // If not try to make it active
            // Assumtpion: window is shown
            private bool ReadyForInput ()
            {
                if (!SafeNativeMethods.IsWindowVisible (_hwnd))
                {
                    throw new ElementNotAvailableException ();
                }

                if (_menuType == WindowsMenu.MenuType.Submenu || _menuType == WindowsMenu.MenuType.Context || _menuType == WindowsMenu.MenuType.SystemPopup)
                {
                    // if menu's type one of the above we do have the right of input
                    // otheriwce window would not be visible
                    return true;
                }

                NativeMethods.GUITHREADINFO gui;

                if (Misc.ProxyGetGUIThreadInfo(0, out gui) && _hwnd == gui.hwndActive)
                {
                    // Remark: ProxyGetClassName() does not return the actual classname.  Instead
                    // it returns the underlying classname and hence the compare is done with "ListBox" and
                    // not "ComboLBox".
                    if (gui.hwndCapture != IntPtr.Zero && Misc.ProxyGetClassName(gui.hwndCapture) == "ListBox")
                    {
                        // If a combobox's listbox already has the capture, release it to make menu ready for input.
                        // To release the combobox send an escape key sequence.
                        SingleCollapse();
                    }

                    return true;
                }

                // try to set focus
                return Misc.SetFocus(_hwnd);
            }

            private static bool WaitForMenuMode (bool fInMenuMode)
            {
                // Wait for a few millisecond for this operation to be completed
                UInt32 dwTicks = SafeNativeMethods.GetTickCount();
                UInt32 dwDelta = 0;

                // Wait until the action has been completed
                while (Misc.InMenuMode() != fInMenuMode && (dwDelta = SubtractTicks(SafeNativeMethods.GetTickCount(), dwTicks)) < WindowsMenu.TimeOut)
                {
                    // Sleep the shortest amount of time possible while still guaranteeing that some sleep occurs
                    System.Threading.Thread.Sleep(1);
                }

                return dwDelta < WindowsMenu.TimeOut;
            }

            // Set or Remove the application in Menu mode
            private static bool MenuMode(bool fSet)
            {
                if (Misc.InMenuMode() == fSet)
                    return true;

                SendAltKey();

                // Wait until the action has been completed
                return WaitForMenuMode(fSet);
            }

            // This method navigates to our menuItem
            // using down key
            // Note: This algorithm is simplier and less prone to the bugs
            // than the one where we would get the currently higlighted item
            // than calculate the delta (taking Spacer into account) and the direction
            // and only than move. The fact that down key will go from the last item to the
            // first item allows us to do this. We could of also use the up key only
            private bool KeyBoardNavigate ()
            {
                int cMoves;

                if (CalculateKeyboardMoves (out cMoves))
                {
                    Key key;

                    // For Menu Bar, simulates keyboard right arrow move to get to the item
                    if (this._menuType == MenuType.Toplevel)
                    {
                        key = cMoves > 0 ? Key.Right : Key.Left;
                    }
                    else
                    {
                        key = cMoves > 0 ? Key.Down : Key.Up;
                    }

                    // This math is used a few times throughout the proxy code
                    // Very simple primitives are used. It might be worth reimplementing
                    // them in the proxy dll.
                    cMoves = Math.Abs (cMoves);
                    for (int i = 0; i < cMoves; i++)
                    {
                        Input.SendKeyboardInput (key, true);
                        Input.SendKeyboardInput (key, false);

                        // Sleep for a millisecond to let the target process the Keypresses
                        System.Threading.Thread.Sleep(1);
                    }

                    // Make sure that the our hwnd has the focus
                    // Wait for a few millisecond for this operation to be completed
                    UInt32 dwTicks = SafeNativeMethods.GetTickCount ();
                    UInt32 dwDelta = 0;

                    // Wait until the action has been completed
                    while (!Misc.IsBitSet(UnsafeNativeMethods.GetMenuState(_hmenu, _item, NativeMethods.MF_BYPOSITION), NativeMethods.MF_HILITE) &&
                           (dwDelta = SubtractTicks (SafeNativeMethods.GetTickCount (), dwTicks)) <= WindowsMenu.TimeOut)
                    {
                        // Sleep the shortest amount of time possible while still guaranteeing that some sleep occurs
                        System.Threading.Thread.Sleep(1);
                    }

                    return dwDelta <= WindowsMenu.TimeOut;
                }

                return false;
            }

            // This method calculates the number of right keys
            // that we need to send in order to move to the
            // needed menu item
            // NOTE: call this method only for the top-level menu
            private bool CalculateKeyboardMoves (out int cMoves)
            {
                // Figure out how far away we are from the hilited item
                int c = Misc.GetMenuItemCount(_hmenu);
                int rMoves = 0;
                bool pd = false;

                cMoves = 0;

                for (int i = 0; i < c; i++)
                {
                    if (!IsSeparator(_hmenu, i))
                        rMoves++;

                    if (Misc.IsBitSet(UnsafeNativeMethods.GetMenuState(_hmenu, i, NativeMethods.MF_BYPOSITION), NativeMethods.MF_HILITE))
                    {
                        cMoves = -rMoves;
                        if( pd )
                            break;
                        rMoves = 0;
                        pd = true;
                    }

                    if( i == _item )
                    {
                        cMoves = rMoves;
                        if( pd )
                            break;
                        rMoves = 0;
                        pd = true;
                    }
                }

                return true;
            }

            // detect if given menuItem is a system menuItem
            // Either _menuType is System
            // or in case of maximized MDI - 1st item (on TopLevel) has a submenu and a bitmap associated with it
            private bool IsSystemMenuItem ()
            {
                if (_menuType == WindowsMenu.MenuType.System)
                {
                    return true;
                }

                // MDI case. The system menu is the first item.
                if (_item == 0 && _menuType == WindowsMenu.MenuType.Toplevel &&
                    (IntPtr.Zero != UnsafeNativeMethods.GetSubMenu (_hmenu, 0)) &&
                    Misc.IsBitSet(UnsafeNativeMethods.GetMenuState(_hmenu, 0, NativeMethods.MF_BYPOSITION), NativeMethods.MF_BITMAP))
                {
                    return true;
                }

                return false;
            }

            // Collapses the last open submenu is the hierarchy
            private static void SingleCollapse ()
            {
                Input.SendKeyboardInput (Key.Escape, true);
                Input.SendKeyboardInput (Key.Escape, false);
            }

            // Expands the menu by the means of sending an enter key
            // NOTE: the needed menuItem should be highlighted
            private static void ExpandViaEnter ()
            {
                Input.SendKeyboardInput (Key.Return, true);
                Input.SendKeyboardInput (Key.Return, false);
            }

            // This method has multiple purposes depending on situation at which it is called
            // It can be used to do a full menu collapse
            // It can be used to take us off the menu mode
            // It can be used to put us back into the menu mode
            private static void SendAltKey ()
            {
                Input.SendKeyboardInput (Key.LeftAlt, true);
                Input.SendKeyboardInput (Key.LeftAlt, false);
            }

            // Search in a menu if the menu item string finishes with an accelerator
            // of the form Ctrl+Am Control+Shift+K.
            private static string AccelatorKeyCtrl(string sKeyword, string sCanonicalsKeyword, string menuText, string menuRawText, out int pos)
            {
                int cMenuChars = menuText.Length;
                char ch;

                // Try to find the keyword
               // Eg: Ctrl or Control; 4 == "ctrl".Length; 2 '+' or ' ' + one char
                int cKeyChars = sKeyword.Length;
                if ((pos = menuText.LastIndexOf(sKeyword, StringComparison.Ordinal)) >= 0 && pos + cKeyChars + 2 <= cMenuChars)
                {
                    ch = menuText [pos + cKeyChars];
                    if (ch == '+' || ch == ' ')
                    {
                        // Found a combination "Ctrl+letter"
                        if (pos + cKeyChars + 2 == cMenuChars)
                        {
                            // UperCase the letter, case Ctr+A
                            return string.Format(CultureInfo.CurrentCulture, "{0}{1}{2}", sCanonicalsKeyword, menuText.Substring(pos + cKeyChars + 1, cMenuChars - (pos + cKeyChars + 2)), Char.ToUpper(menuText[cMenuChars - 1], CultureInfo.InvariantCulture));
                        }
                        else
                        {
                            // Take the remaining string from the Keyword
                            // Case Alt+Enter
                            return sCanonicalsKeyword + menuRawText.Substring(pos + cKeyChars + 1, cMenuChars - (pos + cKeyChars + 1));
                        }
                    }
                }
                return null;
            }

            // Search in a menu if the menu item string finishes with an accelerator
            // of the form Fxx (F5, F12, etc)
            private static string AccelatorFxx (string menuText)
            {
                int cChars = menuText.Length;
                int pos;

                // Get the function key number
                for (pos = cChars - 1; pos > 0 && cChars - pos <= 2 && Char.IsDigit (menuText [pos]); pos--)
                {
                }

                // Check that it is the form Fxx
                if (pos < cChars - 1 && pos > 0 && menuText [pos] == 'f')
                {
                    int iKey = int.Parse(menuText.Substring(pos + 1, cChars - (pos + 1)), CultureInfo.InvariantCulture);
                    if (iKey > 0 && iKey <= 12)
                    {
                        return "F" + iKey.ToString(CultureInfo.CurrentCulture);
                    }
                }
                return null;
            }

            // Backward walk to skip over char that are space in menu
            private static int SkipMenuSpaceChar(string s, int iStart)
            {
                char ch;

                // Spaces for Menus are ' ', '\t', '\a'
                while (iStart - 1 >= 0 && ((ch = s[iStart - 1]) == ' ' || ch == '\t' || ch == '\a'))
                {
                    iStart--;
                }

                return iStart;
            }

            private void GetItemId(ref string itemId)
            {
                int result = UnsafeNativeMethods.GetMenuItemID(_hmenu, _item);

                if (result > 0)
                {
                    itemId = "Item " + result.ToString(CultureInfo.CurrentCulture);
                }
                else if (result == -1)
                {
                    // since the "Application-defined 16-bit value that identifies the menu item", i.e.
                    // the MENUITEMINFO.wID, changes from instance to instance, I am using the position as
                    // the ID of the menu items.
                    itemId = "Item " + (_item + 1).ToString(CultureInfo.CurrentCulture);
                }
            }

            #endregion

            // ------------------------------------------------------
            //
            // Private Properties
            //
            // ------------------------------------------------------

            #region Private Properties

            // This method gets the menu item string of an menu.  First gets the length of the string then
            // allocates resources to accomodate the length of the string.
            private string Text
            {
                get
                {
                    // Get the length. passing the length of zero returns the length
                    int length = UnsafeNativeMethods.GetMenuString(_hmenu, _item, IntPtr.Zero, 0, NativeMethods.MF_BYPOSITION);

                    // if the item has zero length, exit gracefully
                    if (length > 0)
                    {
                        // allocate resources for the string
                        StringBuilder strbldr = new StringBuilder(length + 1);

                        // Send the message...
                        if (UnsafeNativeMethods.GetMenuString(_hmenu, _item, strbldr, length + 1, NativeMethods.MF_BYPOSITION) == length)
                        {
                            // assign output parameters
                            return strbldr.ToString();
                        }
                    }
                    else
                    {
                        NativeMethods.MENUITEMINFO menuItemInfo = new NativeMethods.MENUITEMINFO();
                        menuItemInfo.cbSize = Marshal.SizeOf(menuItemInfo.GetType());
                        menuItemInfo.fMask = NativeMethods.MIIM_TYPE | NativeMethods.MIIM_STATE | NativeMethods.MIIM_DATA | NativeMethods.MIIM_ID;
                        menuItemInfo.cch = 0;

                        if (Misc.GetMenuItemInfo(_hmenu, _item, true, ref menuItemInfo))
                        {
                            if (Misc.IsBitSet(menuItemInfo.fType, NativeMethods.MF_SEPARATOR) ||
                                Misc.IsBitSet(menuItemInfo.fType, NativeMethods.MF_MENUBARBREAK) ||
                                Misc.IsBitSet(menuItemInfo.fType, NativeMethods.MF_MENUBREAK))
                            {
                                return SR.Get(SRID.LocalizedNameWindowsMenuSeparator);
                            }
                            else if (Misc.IsBitSet(menuItemInfo.fType, NativeMethods.MF_OWNERDRAW))
                            {
                                // If it's owner-draw, check if it supports the 'dwData is ptr to MSAA data' workaround.
                                return TryMSAAMenuWorkAround(menuItemInfo.dwItemData);
                            }
                        }
                    }

                    return "";
                }
            }

            private unsafe string TryMSAAMenuWorkAround(IntPtr dwItemData)
            {
                if (_hwnd == IntPtr.Zero)
                {
                    return "";
                }

                // Open that process so we can read its memory...
                using (SafeProcessHandle hProcess = new SafeProcessHandle(_hwnd))
                {
                    if (hProcess.IsInvalid)
                    {
                        return "";
                    }

                    // Treat dwItemData as an address, and try to read a MSAAMENUINFO struct from there...
                    MSAAMENUINFO msaaMenuInfo = new MSAAMENUINFO();
                    int readSize = Marshal.SizeOf(msaaMenuInfo.GetType());
                    IntPtr count;

                    if (!Misc.ReadProcessMemory(hProcess, dwItemData, new IntPtr(&msaaMenuInfo), new IntPtr(readSize), out count))
                    {
                        return "";
                    }
                    // Check signature...
                    if (msaaMenuInfo.dwMSAASignature != MSAA_MENU_SIG)
                    {
                        return "";
                    }
                    // Very large values of cchWText can lead to overflows in the length calculation below, and/or
                    // large allocations; to avoid this, bail if cchWText reports a length greater than 4k - which
                    // should be more than sufficient for any menu item.
                    if (msaaMenuInfo.cchWText > 4096)
                    {
                        return "";
                    }

                    // Work out len of UNICODE string to copy (+1 for terminating NUL)
                    readSize = (msaaMenuInfo.cchWText + 1) * sizeof(char);

                    char* text = stackalloc char[readSize];

                    // Do the copy...
                    if (Misc.ReadProcessMemory(hProcess, msaaMenuInfo.pszWText, new IntPtr(text), new IntPtr(readSize), out count))
                    {
                        string menuText = new string(text);

                        int nullTermination = menuText.IndexOf('\0');

                        if (-1 != nullTermination)
                        {
                            // We need to strip null terminated char and everything behind it from the str
                            menuText = menuText.Remove(nullTermination, readSize - nullTermination);
                        }
                        return menuText;
                    }
                }

                return "";
            }

            private bool IsEnabledMenu
            {
                get
                {
                    if (_menuType == WindowsMenu.MenuType.System)
                    {
                        return SafeNativeMethods.IsWindowEnabled(_hwnd);
                    }

                    // get the menu states
                    int state = UnsafeNativeMethods.GetMenuState(_hmenu, _item, NativeMethods.MF_BYPOSITION);

                    // check if the menu state contains `the disabled or grayed state
                    return !(Misc.IsBitSet(state, NativeMethods.MF_DISABLED) | Misc.IsBitSet(state, NativeMethods.MF_GRAYED));
                }
            }

            private MenuItem PreviousMenuItem
            {
                get
                {
                    // return our prev hierarchical sibling of type WindowsMenuItem
                    if (_item > 0)
                    {
                        return new MenuItem(_hwnd, _parent, _item - 1, _hmenu, _menuType);
                    }
                    return null;
                }
            }

            private MenuItem NextMenuItem
            {
                get
                {
                    // return our next hierarchical sibling of type WindowsMenuItem
                    int nextItem = _item + 1;

                    if (nextItem < Misc.GetMenuItemCount(_hmenu))
                    {
                        return new MenuItem(_hwnd, _parent, nextItem, _hmenu, _menuType);
                    }
                    return null;
                }
            }

            // retrieve a hotkey char if any
            private char HotKey
            {
                get
                {
                    string name = Text;
                    int hotKeyStart = name.IndexOf ('&');

                    if (hotKeyStart == -1)
                    {
                        return '\0';
                    }

                    System.Diagnostics.Debug.Assert (name.Length > hotKeyStart + 1, "Unexpected end of string");
                    return name[hotKeyStart + 1];
                }
            }

            // retrieve a hotkey char if any.
            // Checks if there is an another menu prior to this menu item with the same accelerator
            private char HotKeyCheckNoDups
            {
                get
                {
                    char chHotKey = HotKey;

                    if (chHotKey != '\0')
                    {
                        for (int i = 0; i < _item; i++)
                        {
                            if (new MenuItem (_hwnd, this._parent, i, _hmenu, _menuType).HotKey == chHotKey)
                                return '\0';
                        }
                    }
                    return chHotKey;
                }
            }

            private string AcceleratorKey
            {
                get
                {
                    string menuRawText = Text;

                    if (string.IsNullOrEmpty(menuRawText))
                    {
                        return null;
                    }

                    menuRawText = Misc.StripMnemonic(Text);

                    // A tab, '\t', is usually used to separate the menu string from the accelerator.  If a
                    // tab is found assume that the acceleraor is after the tab.
                    int pos = menuRawText.IndexOf('\t');
                    if (pos > 0)
                    {
                        return menuRawText.Remove(0, pos + 1);
                    }

                    // !!! Be caution modifying this code, it must miror the code for the Name Property

                    // Try to look for a combination Ctrl or Alt + something
                    string keyCtrl = SR.Get(SRID.KeyCtrl);
                    string keyControl = SR.Get(SRID.KeyControl);
                    string keyAlt = SR.Get(SRID.KeyAlt);
                    string keyShift = SR.Get(SRID.KeyShift);
                    string keyWin = SR.Get(SRID.KeyWinKey);

                    string menuText = menuRawText.ToLower(CultureInfo.InvariantCulture);
                    string accelerator;

                    if ((accelerator = AccelatorKeyCtrl(keyCtrl.ToLower(CultureInfo.InvariantCulture), keyCtrl + " + ", menuText, menuRawText, out pos)) != null ||
                        (accelerator = AccelatorKeyCtrl(keyControl.ToLower(CultureInfo.InvariantCulture), keyCtrl + " + ", menuText, menuRawText, out pos)) != null ||
                        (accelerator = AccelatorKeyCtrl(keyAlt.ToLower(CultureInfo.InvariantCulture), keyAlt + " + ", menuText, menuRawText, out pos)) != null ||
                        (accelerator = AccelatorKeyCtrl(keyShift.ToLower(CultureInfo.InvariantCulture), keyShift + " + ", menuText, menuRawText, out pos)) != null ||
                        (accelerator = AccelatorKeyCtrl(keyWin.ToLower(CultureInfo.InvariantCulture), keyWin + " + ", menuText, menuRawText, out pos)) != null)
                    {
                        return accelerator;
                    }

                    // Try to look for a Fxx
                    accelerator = AccelatorFxx(menuText);
                    if (!string.IsNullOrEmpty(accelerator))
                    {
                        return accelerator;
                    }

                    // Look for a bunch of Predefined keyword
                    string[] keywordsAccelerators = GetKeywordsAccelerators();

                    for (int i = 0; i < keywordsAccelerators.Length; i++)
                    {
                        pos = menuText.LastIndexOf(keywordsAccelerators[i], StringComparison.OrdinalIgnoreCase);
                        if (pos > 0 && pos + keywordsAccelerators[i].Length == menuText.Length && (menuText[pos - 1] == '\a' || menuText[pos - 1] == '\t'))
                        {
                            return keywordsAccelerators[i];
                        }
                    }

                    return null;
                }
            }


            #endregion

            // ------------------------------------------------------
            //
            // Private Fields and Types Declaration
            //
            // ------------------------------------------------------

            #region Private Fields

            private enum MenuItemType
            {
                Spacer,
                Command,
                SubMenu
            }


            // The following structure is used for accessibility.  Accessibility tools
            // use it to get a descriptive string out of an owner-draw menu.  This
            // stuff will probably be put in a system header someday.
            private const int MSAA_MENU_SIG = unchecked((int)0xAA0DF00D);

            // Menu's dwItemData should point to one of these structs:
            // (or can point to an app-defined struct containing this as the first
            // member)
            [StructLayout(LayoutKind.Sequential)]
            private struct MSAAMENUINFO
            {
                internal int dwMSAASignature; // Must be MSAA_MENU_SIG
                internal int cchWText;        // Length of text in chars
                internal IntPtr pszWText;        // NUL-terminated text, in Unicode
            }

            private MenuItemType _type;
            private IntPtr _hmenu;
            internal WindowsMenu.MenuType _menuType;

            #endregion
        }

        #endregion

        // ------------------------------------------------------
        //
        //  DestroyedMenuItem Internal Class
        //
        //------------------------------------------------------

        #region DestroyedMenuItem

        // Represents a menu item whose container hwnd has been destroyed as is the case with InvokedEvent
        internal class DestroyedMenuItem : ProxyFragment, IInvokeProvider
        {
            // ------------------------------------------------------
            //
            // Constructors
            //
            // ------------------------------------------------------

            #region Constructors

            // Constructor used only for the Invoke event.  At this point in the Invoked event, hwnd is destroyed and there
            // is no reliable way to get the parent.  Most properties and methods for this object will throw InvalidOperation.
            internal DestroyedMenuItem(IntPtr hwnd, int item, IntPtr hwndParent)
                : base(hwnd, null, item)
            {
                _fNonClientAreaElement = true;
                _cControlType = ControlType.MenuItem;
                _item = item;

                _sAutomationId = "Item " + (item).ToString(CultureInfo.CurrentCulture);

                // This is used only to return a HostRawElementProvider for this menu item
                _hwndParent = hwndParent;
            }

            #endregion


            #region ProxySimple Interface

            internal override IRawElementProviderSimple HostRawElementProvider
            {
                get
                {
                    // Although we have the hwnd that contains this menu item that hwnd has been destroyed
                    // but go ahead and use it for the fragment root so events can be scoped to it.
                    return AutomationInteropProvider.HostProviderFromHandle(_hwndParent);
                }
            }

            // This might be a good idea for RuntimeId for menu items (base, hwnd menubar, item) in general.
            // Commenting out for now because UIA core doesn't give this provider a chance to provide RuntimeId.
            //internal override int [] GetRuntimeId()
            //{
            //    return new int[] { 1, unchecked((int)(long)_hwnd), _item };
            //}

            // Returns a pattern interface if supported.
            internal override object GetPatternProvider(AutomationPattern iid)
            {
                // even though this element is dead, return 'this' so events are raised properly
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
                    return Rect.Empty;
                }
            }

            internal override bool SetFocus()
            {
                throw new ElementNotAvailableException();
            }

            internal override object GetElementProperty (AutomationProperty idProp)
            {
                // Expose enough properties to identify this as a particular menu item
                if (idProp == AutomationElement.ControlTypeProperty)
                {
                    return ControlType.MenuItem.Id;
                }

                if (idProp == AutomationElement.AutomationIdProperty)
                {
                    return _sAutomationId.Length > 0 ? _sAutomationId : null;
                }

                // This property is used by UIAutomationCore when raising events
                if (idProp == AutomationElement.NativeWindowHandleProperty)
                {
                    return _hwnd;
                }

                return AutomationElement.NotSupported;
            }

            #endregion

            #region Invoke Pattern

            void IInvokeProvider.Invoke()
            {
                throw new ElementNotAvailableException();
            }

            #endregion


            #region ProxyFragment Interface
            //
            // Throw ElementNotAvailable for any ProxyFragment implementation
            // since this element is quite dead.
            //
            internal override ProxySimple GetNextSibling (ProxySimple child)
            {
                throw new ElementNotAvailableException();
            }

            internal override ProxySimple GetPreviousSibling (ProxySimple child)
            {
                throw new ElementNotAvailableException();
            }

            internal override ProxySimple GetFirstChild ()
            {
                throw new ElementNotAvailableException();
            }

            internal override ProxySimple GetLastChild ()
            {
                throw new ElementNotAvailableException();
            }

            internal override ProxySimple ElementProviderFromPoint (int x, int y)
            {
                throw new ElementNotAvailableException();
            }

            #endregion ProxyFragment Interface

            // ------------------------------------------------------
            //
            // Private Fields
            //
            // ------------------------------------------------------

            #region Private Fields

            IntPtr _hwndParent;

            #endregion
        }

        #endregion
    }
}
