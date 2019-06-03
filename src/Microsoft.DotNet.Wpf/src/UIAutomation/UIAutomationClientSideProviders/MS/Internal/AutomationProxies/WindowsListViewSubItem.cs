// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// Description: Win32 ListViewSubItem proxy
//


using System;
using System.ComponentModel;
using System.Windows.Automation;
using System.Windows.Automation.Provider;
using System.Windows;
using MS.Win32;
using Accessibility;

namespace MS.Internal.AutomationProxies
{
    internal class ListViewSubItem: ProxySimple, IGridItemProvider, ITableItemProvider, IValueProvider
    {
        // ------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------

        #region Constructors

        internal ListViewSubItem (IntPtr hwnd, ProxyFragment parent, int item, int itemParent)
            : base (hwnd, parent, item)
        {
            // Is used to discriminate between items in a collection.
            _itemParent = itemParent;

            _cControlType = WindowsListView.ListViewEditable(hwnd) ? ControlType.Edit : ControlType.Text;
        }

        #endregion Constructos

        //------------------------------------------------------
        //
        //  Patterns Implementation
        //
        //------------------------------------------------------

        #region ProxySimple Interface

        // Returns a pattern interface if supported.
        internal override object GetPatternProvider (AutomationPattern iid)
        {
            if (iid == GridItemPattern.Pattern)
            {
                return this;
            }

            // Only the first element is editable
            if (iid == ValuePattern.Pattern && _item == 0 && WindowsListView.ListViewEditable (_hwnd))
            {
                return this;
            }

            if (iid == TableItemPattern.Pattern)
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
                NativeMethods.Win32Rect itemRectangle;

                // NOTE: 1st(column 0) subitem is a special one, since it is fake, Win32's LV does not
                // have a subitem 0 in report mode
                int lvir = (_item == 0) ? NativeMethods.LVIR_SELECTBOUNDS : NativeMethods.LVIR_BOUNDS;

                if (!WindowsListView.GetSubItemRect (_hwnd, _itemParent, _item, lvir, out itemRectangle))
                {
                    return Rect.Empty;
                }

                // Special case: LV is full row select, with more than 1 column and we are looking at the first item.
                // Only IconViews and DetailViews are processed here.  TileViews will be processed as a
                // ListViewItem.  The DetailView is the only view that is in a row/column layout with its data.
                if (WindowsListView.FullRowSelect(_hwnd) && !WindowsListView.HasJustifyColumnsExStyle(_hwnd) &&
                    !WindowsListView.IsIconView(_hwnd) && _item == 0 && 1 < ListViewItem.GetSubItemCount(_hwnd))
                {
                    NativeMethods.Win32Rect itemRectangle1;

                    if (!WindowsListView.GetSubItemRect(_hwnd, _itemParent, 1, NativeMethods.LVIR_BOUNDS, out itemRectangle1))
                    {
                        return Rect.Empty;
                    }
                    
                    // Derived values from the adjacent subitems are conditional based on RTL
                    if (Misc.IsControlRTL(_hwnd))
                    {
                        itemRectangle.left = itemRectangle1.right;
                    }
                    else 
                    {
                        itemRectangle.right = itemRectangle1.left;
                    }

                    // take checkbox into account
                    if (ListViewItem.IsItemWithCheckbox (_hwnd, _itemParent))
                    {
                        NativeMethods.Win32Rect checkboxRectangle = ListViewItemCheckbox.ListViewCheckBoxRect (_hwnd, _itemParent);

                        // Derived values from the adjacent subitems are conditional based on RTL
                        if (Misc.IsControlRTL(_hwnd))
                        {
                            itemRectangle.right -= (checkboxRectangle.right - checkboxRectangle.left);
                        }
                        else
                        {
                            itemRectangle.left += (checkboxRectangle.right - checkboxRectangle.left);
                        }
                    }
                }

                // Don't need to normalize, GetSubItemRect returns absolute coordinates.
                return itemRectangle.ToRect(false);
            }
        }

        // Process all the Logical and Raw Element Properties
        internal override object GetElementProperty(AutomationProperty idProp)
        {
            if (idProp == AutomationElement.IsOffscreenProperty)
            {
                Rect parentRect = GetParent().GetParent().BoundingRectangle;
                Rect itemRect = BoundingRectangle;
                if (itemRect.IsEmpty || parentRect.IsEmpty)
                {
                    return true;
                }

                // Need to check if this item is visible on the whole control not just its immediate parent.
                if (!Misc.IsItemVisible(ref parentRect, ref itemRect))
                {
                    return true;
                }
            }
            else if (idProp == AutomationElement.HasKeyboardFocusProperty)
            {
                IAccessible acc = AccessibleObject;
                // The items are zero based, i.e. the first listview item is item 0.  The
                // zero item in MSAA is self, so need to add one to the item to get the 
                // correct Accessible child.
                AccessibleRole role = Accessible.GetRole(acc, _itemParent + 1);

                // ListView Iaccessible knows when its really a menu item
                if (role == AccessibleRole.MenuItem)
                {
                    // Use the IsFocused of the Subitem instead the the one in ProxySimple
                    // When ListViews are used for menus they don't get focus
                    // so the check for "does this hwnd have focus" fails
                    return IsFocused ();
                }

                // If we are in a SysListView32 and that list view is in the Start Menu search column
                // real focus can stay on the edit box, while a virtual focus navigates the list
                // If this is the case, only check IsFocused, don't do the GetGUIThreadInfo check.
                IntPtr ancestor = _hwnd;
                IntPtr desktop = UnsafeNativeMethods.GetDesktopWindow();

                while (ancestor != IntPtr.Zero && ancestor != desktop)
                {
                    if (Misc.GetClassName(ancestor) == "Desktop Search Open View")
                    {
                        return IsFocused();
                    }
                    ancestor = Misc.GetParent(ancestor);
                }
            }

            return base.GetElementProperty(idProp);
        }


        //Gets the controls help text
        internal override string HelpText
        {
            get
            {
                return WindowsListView.GetItemToolTipText(_hwnd);
            }
        }

        //Gets the localized name
        internal override string LocalizedName
        {
            get
            {
                string name = ListViewItem.GetText(_hwnd, _itemParent, _item);
                return name.Length < Misc.MaxLengthNameProperty ? name : name.Substring(0, Misc.MaxLengthNameProperty);
            }
        }

        // Sets the focus to this item.
        internal override bool SetFocus()
        {
            // Set the item's state to focused.
            return WindowsListView.SetItemFocused (_hwnd, this._itemParent);
        }

        #endregion ProxySimple Interface

        #region Value Pattern

        void IValueProvider.SetValue (string val)
        {
            // Make sure that the control is enabled
            if (!SafeNativeMethods.IsWindowEnabled(_hwnd))
            {
                throw new ElementNotEnabledException();
            }

            ListViewItem.SetValue (val, _hwnd, _itemParent);
        }

        // Request to get the value that this UI element is representing as a string
        string IValueProvider.Value
        {
            get
            {
                return ListViewItem.GetText (_hwnd, _itemParent, _item);
            }
        }

        bool IValueProvider.IsReadOnly
        {
            get
            {
                return !WindowsListView.ListViewEditable (_hwnd);
            }
        }


        #endregion ValuePattern

        #region GridItemPattern

        int IGridItemProvider.Row
        {
            get
            {
                if (!WindowsListView.IsGroupViewEnabled (_hwnd))
                {
                    return _itemParent;
                }

                // we're in the group mode:
                // In order to detect the item's row...find the location
                // of this item in the array of group items, location will indicate the raw
                int groupID = ListViewItem.GetGroupID (_hwnd, _itemParent);

                if (groupID != -1)
                {
                    GroupManager.GroupInfo groupInfo = WindowsListViewGroup.GetGroupInfo (_hwnd, groupID);

                    if (groupInfo)
                    {
                        int row = groupInfo.IndexOf (_itemParent);

                        if (row >= 0)
                        {
                            return row;
                        }
                    }
                }

                return -1;
            }
        }

        int IGridItemProvider.Column
        {
            get
            {
                return _item;
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
                // ContainingGrid would be either Group or the ListView
                // For both cases we need to skip our immediate parent
                // which is ListViewItem => meaning ContainingGrid is defined as parent of the parent                
                return _parent._parent;
            }
        }

        #endregion GridItemPattern

        #region TableItemPattern

        IRawElementProviderSimple [] ITableItemProvider.GetRowHeaderItems ()
        {
            return null;
        }

        IRawElementProviderSimple [] ITableItemProvider.GetColumnHeaderItems ()
        {
            IntPtr hwndHeader = WindowsListView.ListViewGetHeader (_hwnd);
            if (SafeNativeMethods.IsWindowVisible (hwndHeader))
            {

                WindowsSysHeader header = (WindowsSysHeader) WindowsSysHeader.Create (hwndHeader, 0);
                return new IRawElementProviderSimple [] { new WindowsSysHeader.HeaderItem (hwndHeader, header, _item) };
            }
            return null;
        }

        #endregion TableItemPattern

        //------------------------------------------------------
        //
        //  Internal Methods
        //
        //------------------------------------------------------

        #region Internal Methods

        internal static ProxySimple ElementProviderFromPoint (IntPtr hwnd, ProxyFragment parent, int item, int x, int y)
        {
            NativeMethods.LVHITTESTINFO_INTERNAL hitTest = WindowsListView.SubitemHitTest (hwnd, item, new NativeMethods.Win32Point (x, y));

            if (hitTest.iSubItem >= 0)
            {
                return new ListViewSubItem (hwnd, parent, hitTest.iSubItem, item);
            }

            // subitems do not exist
            return parent;
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
            if (Misc.IsComctrlV6OnOsVerV6orHigher(_hwnd))
            {
                int column = (int)Misc.ProxySendMessage(_hwnd, NativeMethods.LVM_GETFOCUSEDCOLUMN, IntPtr.Zero, IntPtr.Zero);
                return column == _item;
            }

            return WindowsListView.IsItemFocused (_hwnd, _itemParent);
        }


        #endregion

        //------------------------------------------------------
        //
        //  Private Fields
        //
        //------------------------------------------------------

        #region Private Fields

        // The item in the listview. _item is the SubItem
        private int _itemParent;

        #endregion Private Fields
    }
}

