// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// Description: UIAutomation Toolbar Proxy

using System;
using System.Collections;
using System.Runtime.InteropServices;
using System.ComponentModel;
using System.Globalization;
using System.Text;
using System.Windows.Automation;
using System.Windows.Automation.Provider;
using System.Windows;
using MS.Win32;

namespace MS.Internal.AutomationProxies
{
    // Toolbar proxy
    class WindowsToolbar: ProxyHwnd
    {
        // ------------------------------------------------------
        //
        // Constructors
        //
        // ------------------------------------------------------

        #region Constructors

        protected WindowsToolbar (IntPtr hwnd, ProxyFragment parent, int item)
            : base( hwnd, parent, item )
        {
            // Set the control type string to return properly the properties.
            _cControlType = ControlType.ToolBar;
            _fIsContent = false;

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

        internal static IRawElementProviderSimple Create(IntPtr hwnd, int idChild)
        {
            WindowsToolbar wtv = null;

            // By calling Accessible.CreateNativeFromEvent() instead of AccessibleObjectFromWindow() only controls with a native
            // implementation of IAccessible will be found.  OleAcc will not create a IAccessible proxy, since
            // Accessible.CreateNativeFromEvent() by passes OleAcc by sending a WM_GETOBJECT directly to the control and creating
            // IAccessible from the return, if it can.
            Accessible acc = Accessible.CreateNativeFromEvent(hwnd, NativeMethods.OBJID_CLIENT, NativeMethods.CHILD_SELF);
            if (acc != null)
            {
                AccessibleRole role = acc.Role;
                if (role == AccessibleRole.MenuBar || role == AccessibleRole.MenuPopup)
                {
                    wtv = new WindowsToolbarAsMenu(hwnd, null, 0, acc);
                }
            }

            if( wtv == null)
            {
                wtv = new WindowsToolbar(hwnd, null, 0);
            }

            return idChild == 0 ? wtv : wtv.CreateToolbarItem (idChild - 1);
        }

        // Static Create method called by the event tracker system
        internal static void RaiseEvents (IntPtr hwnd, int eventId, object idProp, int idObject, int idChild)
        {
            if (idObject != NativeMethods.OBJID_VSCROLL && idObject != NativeMethods.OBJID_HSCROLL)
            {
                ProxySimple proxySimple;
                try
                {
                    proxySimple = (ProxySimple)Create(hwnd, idChild);
                }
                catch(Win32Exception)
                {
                    // With the toolbar on the classic Taskbar, we receive two EventObjectDestroy's.  The first
                    // for the button/application that is going away and the second for an unknown button.
                    // The second unknown button fails the Create() with nonconsistant errors.  So just ignore
                    // EventObjectDestroy's that causes Win32Exceptions in the Create().
                    if (eventId == NativeMethods.EventObjectDestroy && idProp == AutomationElement.StructureChangedEvent)
                    {
                        proxySimple = null;
                    }
                    else
                    {
                        throw;
                    }
                }
                // Ends up calling CreateToolbarItem which can return null
                if (proxySimple != null)
                {
                    proxySimple.DispatchEvents(eventId, idProp, idObject, idChild);
                }
            }
        }

        #endregion Proxy Create

        //------------------------------------------------------
        //
        //  Patterns Implementation
        //
        //------------------------------------------------------

        #region ProxyFragment Interface

        // Returns the next sibling element in the raw hierarchy.
        // Peripheral controls have always negative values.
        // Returns null if no next child.
        internal override ProxySimple GetNextSibling (ProxySimple child)
        {
            ProxySimple toolbarItem = null;
            int count = Count;

            // Next for an item that does not exist in the list
            if (child._item >= count)
            {
                throw new ElementNotAvailableException ();
            }

            // If the index of the next node would be out of range...
            for (int item = child._item + 1; item >= 0 && item < count; item++)
            {
                // This may fail if the toolbar item is hidden
                if ((toolbarItem = CreateToolbarItem (item)) != null)
                {
                    break;
                }
            }

            return toolbarItem;
        }

        // Returns the previous sibling element in the raw hierarchy.
        // Peripheral controls have always negative values.
        // Returns null is no previous.
        internal override ProxySimple GetPreviousSibling (ProxySimple child)
        {
            ProxySimple toolbarItem = null;
            int count = Count;

            // Next for an item that does not exist in the list
            if (child._item >= count)
            {
                throw new ElementNotAvailableException ();
            }

            // If the index of the prev node would be out of range...
            for (int item = child._item - 1; item >= 0 && item < count; item--)
            {
                // This may fail if the toolbar item is hidden
                if ((toolbarItem = CreateToolbarItem (item)) != null)
                {
                    break;
                }
            }

            return toolbarItem;
        }

        // Returns the first child element in the raw hierarchy.
        internal override ProxySimple GetFirstChild ()
        {
            ProxySimple toolbarItem = null;

            // If the index of the next node would be out of range...
            for (int item = 0, count = Count; item < count; item++)
            {
                // This may fail if the toolbar item is hidden
                if ((toolbarItem = CreateToolbarItem (item)) != null)
                {
                    break;
                }
            }

            return toolbarItem;
        }

        // Returns the last child element in the raw hierarchy.
        internal override ProxySimple GetLastChild ()
        {
            ProxySimple toolbarItem = null;

            // If the index of the prev node would be out of range...
            for (int item = Count - 1; item >= 0; item--)
            {
                // This may fail if the toolbar item is hidden
                if ((toolbarItem = CreateToolbarItem (item)) != null)
                {
                    break;
                }
            }

            return toolbarItem;
        }

        // Returns a Proxy element corresponding to the specified screen coordinates.
        internal override ProxySimple ElementProviderFromPoint (int x, int y)
        {
            for (int item = 0, count = Count; item <= count; item++)
            {
                NativeMethods.Win32Rect rc = new NativeMethods.Win32Rect (ToolbarItem.GetBoundingRectangle (_hwnd, item));

                if (Misc.PtInRect(ref rc, x, y))
                {
                    return CreateToolbarItem (item);
                }
            }

            return base.ElementProviderFromPoint (x, y);
        }

        #endregion

        #region ProxySimple Interface

        // Returns an item corresponding to the focused element (if there is one), or null otherwise.
        internal override ProxySimple GetFocus ()
        {
            int focusIndex = Misc.ProxySendMessageInt(_hwnd, NativeMethods.TB_GETHOTITEM, IntPtr.Zero, IntPtr.Zero);

            if (focusIndex >= 0)
            {
                Accessible acc = Accessible.CreateNativeFromEvent(_hwnd, NativeMethods.OBJID_CLIENT, NativeMethods.CHILD_SELF);
                if (acc != null)
                {
                    AccessibleRole role = acc.Role;
                    if (role == AccessibleRole.MenuBar || role == AccessibleRole.MenuPopup)
                    {
                        return new WindowsToolbarAsMenu(_hwnd, this, focusIndex, acc);
                    }
                }

                return new WindowsToolbar(_hwnd, this, focusIndex);
            }
            else
            {
                return null;
            }
        }

        //Gets the localized name
        internal override string LocalizedName
        {
            get
            {
                // Outlook Express is now setting their toolbars with names, via SetWindowText(), so
                // try to get the name from the windows text.
                string name = Misc.ProxyGetText(_hwnd);
                if (string.IsNullOrEmpty(name))
                {
                    // if WM_GETTEXT failed try to get the name from the IAccessible object.
                    name = GetAccessibleName(NativeMethods.CHILD_SELF);
                }
                // If still no name return null.
                return string.IsNullOrEmpty(name) ? null : name;
            }
        }

        #endregion

        // ------------------------------------------------------
        //
        // Internal Methods
        //
        // ------------------------------------------------------        

        #region Internal Methods

        // Create a WindowsToolbar instance.  Needs to be internal because
        // ApplicationWindow pattern needs to call this so needs to be internal
        internal ProxySimple CreateToolbarItem (int item)
        {
            NativeMethods.TBBUTTON tbb = new NativeMethods.TBBUTTON ();

            // During the FocusChanged WinEvent (EVENT_OBJECT_FOCUS),
            // some "ToolbarWindow32" children report an item ID (child id)
            // of 0x80000001, 0x80000002, etc. instead of 1, 2, etc.
            // However, when created as children of the parent toolbar,
            // these same items are assigned IDs of 1, 2, etc.
            // Therefore, map negative item IDs of the form 0x80000001,
            // 0x80000002, etc. to 1, 2, etc.
            item = (int)(~0x80000000 & (uint)item);

            if (!XSendMessage.GetItem(_hwnd, item, ref tbb))
            {
                // If failed to get button infromation the button must not exist, so return null.
                return null;
            }

            if (Misc.ProxySendMessageInt(_hwnd, NativeMethods.TB_ISBUTTONHIDDEN, new IntPtr(tbb.idCommand), IntPtr.Zero) == 0)
            {
                Accessible acc = Accessible.CreateNativeFromEvent(_hwnd, NativeMethods.OBJID_CLIENT, item + 1);
                if (acc != null)
                {
                    if (acc.Role == AccessibleRole.MenuItem)
                    {
                        return new ToolbarItemAsMenuItem(_hwnd, this, item, tbb.idCommand, acc);
                    }
                }

                return new ToolbarItem(_hwnd, this, item, tbb.idCommand);
            }
            return null;
        }

        #endregion

        // ------------------------------------------------------
        //
        // Private Methods
        //
        // ------------------------------------------------------        

        #region Private Methods

        private int Count
        {
            get
            {
                return Misc.ProxySendMessageInt(_hwnd, NativeMethods.TB_BUTTONCOUNT, IntPtr.Zero, IntPtr.Zero);
            }
        }

        #endregion
    }

    // ------------------------------------------------------
    //
    // ToolbarItem Classes
    //
    // ------------------------------------------------------

    #region ToolbarItem

    // Proxy for each button in a toolbar
    class ToolbarItem : ProxySimple, IInvokeProvider, IToggleProvider
    {
        // ------------------------------------------------------
        //
        // Constructors
        //
        // ------------------------------------------------------

        #region Constructors

        internal ToolbarItem(IntPtr hwnd, ProxyFragment parent, int item, int idCommand)
            : base(hwnd, parent, item)
        {
            _idCommand = idCommand;

            NativeMethods.TBBUTTON tbb = new NativeMethods.TBBUTTON();
            int buttonStyle = 0;
            if (XSendMessage.GetItem(_hwnd, _item, ref tbb))
            {
                buttonStyle = tbb.fsStyle;
            }

            // Set the strings to return properly the properties.
            bool hasImageList = Misc.ProxySendMessageInt(_hwnd, NativeMethods.TB_GETIMAGELIST, IntPtr.Zero, IntPtr.Zero) != 0;
            int exStyle = Misc.ProxySendMessageInt(_hwnd, NativeMethods.TB_GETEXTENDEDSTYLE, IntPtr.Zero, IntPtr.Zero);

            _isToggleButton = false;
            _cControlType = ControlType.Button;

            // If a separator, say so
            if (Misc.IsBitSet(buttonStyle, NativeMethods.BTNS_SEP))
                _cControlType = ControlType.Separator;
            else if (Misc.IsBitSet(buttonStyle, NativeMethods.BTNS_CHECK))
            {
                // Special case for task list - they use the checked style, but only for visuals...
                IntPtr hwndParent = Misc.GetParent(_hwnd);
                if(Misc.GetClassName(hwndParent) != "MSTaskSwWClass")
                {
                    _isToggleButton = true;
                }
            }
            else if (Misc.IsBitSet(buttonStyle, NativeMethods.BTNS_DROPDOWN)
                  && Misc.IsBitSet(exStyle, NativeMethods.TBSTYLE_EX_DRAWDDARROWS))
            {
                // if its a drop down and it has an arrow its a split button
                _cControlType = ControlType.SplitButton;
            }
            else if (!hasImageList || tbb.iBitmap == NativeMethods.I_IMAGENONE)
            {
                // Text-only, no bitmap, so it's effectively a menu item.
                // (eg. as used in MMC)
                _cControlType = ControlType.MenuItem;
            }
 
             _fIsContent = _cControlType != ControlType.Separator;

            // The Start Menu's "Shut Down" and "Log Off" buttons are toolbar items.  They need to have the 
            // KeyboardFocusable property be set to true.
            _fIsKeyboardFocusable = (bool)parent.GetElementProperty(AutomationElement.IsKeyboardFocusableProperty);

            GetItemId(ref _sAutomationId);
        }

        #endregion

        // ------------------------------------------------------
        //
        // Patterns Implementation
        //
        // ------------------------------------------------------

        #region ProxySimple Interface

        // Returns a pattern interface if supported.
        internal override object GetPatternProvider(AutomationPattern iid)
        {
            // Check if button is a separator
            if (_cControlType == ControlType.Separator)
            {
                return null;
            }

            // Check if button is hidden
            if (Misc.ProxySendMessageInt(_hwnd, NativeMethods.TB_ISBUTTONHIDDEN, new IntPtr(_idCommand), IntPtr.Zero) != 0)
            {
                return null;
            }

            if (iid == InvokePattern.Pattern && !_isToggleButton)
            {
                // button is enabled and not hidden and not a separator
                return this;
            }
            else if (iid == TogglePattern.Pattern && _isToggleButton)
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
                return GetBoundingRectangle(_hwnd, _item);
            }
        }

        // Process all the Logical and Raw Element Properties
        internal override object GetElementProperty(AutomationProperty idProp)
        {
            if (idProp == AutomationElement.AccessKeyProperty)
            {
                return Misc.AccessKey(Text);
            }
            else if (idProp == AutomationElement.IsControlElementProperty)
            {
                return _cControlType != ControlType.Separator;
            }
            else if (idProp == AutomationElement.IsEnabledProperty)
            {
                return Misc.ProxySendMessageInt(_hwnd, NativeMethods.TB_ISBUTTONENABLED, new IntPtr(_idCommand), IntPtr.Zero) != 0;
            }

            return base.GetElementProperty(idProp);
        }

        //Gets the controls help text
        internal override string HelpText
        {
            get
            {
                return GetItemToolTipText();
            }
        }

        //Gets the localized name
        internal override string LocalizedName
        {
            get
            {
                // It does not look like Winforms support the AccessibleName for standard toolbar items.
                // If ToolStrips are going to be supported by this proxy, will need to added code to check
                // if this is winforms toolbar button and use the AccessibleName if it is set.

                return Misc.StripMnemonic(Text);
            }
        }

        // This method will set the hot item since toolbar buttons can't have focus
        internal override bool SetFocus()
        {
            // Get current focus...
            WindowsToolbar toolbar = (WindowsToolbar)_parent;
            ToolbarItem focused = toolbar.GetFocus() as ToolbarItem;

            // ... check for no current focus or currently focused item is not the one we want...
            if (focused == null || _item != focused._item)
            {
                //... set the focus
                /*
                                    // should this go to parent window?
                                    if ( NativeMethods.DefWindowProc( _hwnd, NativeMethods.WM_SETFOCUS, IntPtr.Zero, IntPtr.Zero ) != IntPtr.Zero )
                                    {
                                        return false;
                                    };
                */
                Misc.ProxySendMessage(_hwnd, NativeMethods.TB_SETHOTITEM, new IntPtr(_item), IntPtr.Zero);
            }

            return true;
        }

        #endregion

        #region Invoke Pattern

        // Press a toolbar button
        void IInvokeProvider.Invoke()
        {
            Invoke();
        }

        #endregion

        #region IToggleProvider

        void IToggleProvider.Toggle()
        {
            Invoke();
        }

        ToggleState IToggleProvider.ToggleState
        {
            get
            {
                return ToggleState;
            }
        }

        #endregion IToggleProvider

        //------------------------------------------------------
        //
        //  Internal Methods
        //
        //------------------------------------------------------

        #region Internal Methods

        // Returns the bounding rectangle of the control.
        internal static Rect GetBoundingRectangle(IntPtr hwnd, int item)
        {
            return XSendMessage.GetItemRect(hwnd, NativeMethods.TB_GETITEMRECT, item);
        }

        #endregion

        //------------------------------------------------------
        //
        //  Protected Methods
        //
        //------------------------------------------------------

        #region Protected Methods

        // This routine is only called on elements belonging to an hwnd that has the focus.
        protected override bool IsFocused()
        {
            return Misc.ProxySendMessageInt(_hwnd, NativeMethods.TB_GETHOTITEM, IntPtr.Zero, IntPtr.Zero) == _item;
        }

        protected bool IsSeparator()
        {
            return _cControlType == ControlType.Separator;
        }

        #endregion

        // ------------------------------------------------------
        //
        // Private methods
        //
        // ------------------------------------------------------

        #region Private Methods

        private string Text
        {
            get
            {
                int len = Misc.ProxySendMessageInt(_hwnd, NativeMethods.TB_GETBUTTONTEXT, new IntPtr(_idCommand), IntPtr.Zero);
                if (len > 0)
                {
                    return XSendMessage.GetItemText(_hwnd, NativeMethods.TB_GETBUTTONTEXT, _idCommand, len);
                }
                else
                {
                    // If there is no button text then try getting accName from MSAA.  MSAA has 1 based ChildIDs
                    // so add 1. As a last resort return the tooltip if there is one (may be long).
                    string name  = GetAccessibleName(_item + 1);
                    if (!string.IsNullOrEmpty(name))
                    {
                        return name;
                    }
                    else
                    {
                        return GetItemToolTipText();
                    }
                }
            }
        }

        private void GetItemId(ref string itemId)
        {
            NativeMethods.TBBUTTON tbb = new NativeMethods.TBBUTTON();

            if (XSendMessage.GetItem(_hwnd, _item, ref tbb))
            {
                if (tbb.idCommand > 0)
                {
                    itemId = "Item " + tbb.idCommand.ToString(CultureInfo.CurrentCulture);
                }
            }
        }

        private string GetItemToolTipText()
        {
            IntPtr hwndToolTip = Misc.ProxySendMessage(_hwnd, NativeMethods.TB_GETTOOLTIPS, IntPtr.Zero, IntPtr.Zero);
            return Misc.GetItemToolTipText(_hwnd, hwndToolTip, _idCommand);
        }

        private void Invoke()
        {
            // Make sure that the toolbar is enabled, and that the toolbar button is enabled.
            if (!SafeNativeMethods.IsWindowEnabled(_hwnd)
                || Misc.ProxySendMessageInt(_hwnd, NativeMethods.TB_ISBUTTONENABLED, new IntPtr(_idCommand), IntPtr.Zero) == 0)
            {
                throw new ElementNotEnabledException();
            }

            // Check that button can be clicked (button not hidden)
            // This state could change anytime so success is not guaranteed
            if (Misc.ProxySendMessageInt(_hwnd, NativeMethods.TB_ISBUTTONHIDDEN, new IntPtr(_idCommand), IntPtr.Zero) != 0)
            {
                throw new InvalidOperationException(SR.Get(SRID.OperationCannotBePerformed));
            }

            // Click the center of the button
            // TB_CHECKBUTTON and TB_PRESSBUTTON messages are not used as they will not trigger proper notifications
            // Need to check that this button is visible to the mouse
            Rect boundingRectangle = BoundingRectangle;

            if (boundingRectangle.IsEmpty)
            {
                throw new InvalidOperationException(SR.Get(SRID.OperationCannotBePerformed));
            }

            // make sure this item is active
            SetFocus();

            Misc.MouseClick(((int)boundingRectangle.Left + (int)boundingRectangle.Right) / 2, ((int)boundingRectangle.Top + (int)boundingRectangle.Bottom) / 2);
        }

        private ToggleState ToggleState
        {
            get
            {
                ToggleState icsState = ToggleState.Indeterminate;

                if (Misc.ProxySendMessageInt(_hwnd, NativeMethods.TB_ISBUTTONCHECKED, new IntPtr(_idCommand), IntPtr.Zero) == 0)
                {
                    icsState = ToggleState.Off;
                }
                else
                {
                    icsState = ToggleState.On;
                }

                return icsState;
            }
        }

        #endregion

        // ------------------------------------------------------
        //
        // Protected Fields
        //
        // ------------------------------------------------------

        #region Protected Fields

        // Command identifier for toolbar buttons
        protected int _idCommand;
        private bool _isToggleButton;

        #endregion
    }

    #endregion
}

