// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// Description: Win32 ListView Item proxy
//


// PRESHARP: In order to avoid generating warnings about unknown message numbers and unknown pragmas.
#pragma warning disable 1634, 1691

using System;
using System.ComponentModel;
using System.Windows.Automation;
using System.Windows.Automation.Provider;
using System.Runtime.InteropServices;
using System.Windows;
using Accessibility;
using MS.Win32;

namespace MS.Internal.AutomationProxies
{
    internal class ListViewItem : ProxyFragment, IInvokeProvider,
                    ISelectionItemProvider, IValueProvider, IGridItemProvider,
                    IScrollItemProvider
    {
        // ------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------

        #region Constructors

        internal ListViewItem (IntPtr hwnd, ProxyFragment parent, int item)
            : base (hwnd, parent, item)
        {
            // Set the strings to return properly the properties.
            if (WindowsListView.IsDetailMode(hwnd))
            {
                _cControlType = ControlType.DataItem;
            }
            else
            {
                _cControlType = ControlType.ListItem;
            }
            _fHasPersistentID = false;
            _fIsKeyboardFocusable = true;
            _isComctrlV6OnOsVerV6orHigher = Misc.IsComctrlV6OnOsVerV6orHigher(hwnd);
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
            if (iid == InvokePattern.Pattern && WindowsListView.ListViewInvokable(_hwnd))
            {
                return this;
            }

            if (iid == SelectionItemPattern.Pattern)
            {
                return this;
            }

            if (iid == ValuePattern.Pattern && WindowsListView.ListViewEditable (_hwnd))
            {
                return this;
            }

            if (iid == GridItemPattern.Pattern && IsImplementingGrid (_hwnd))
            {
                return this;
            }

            if (iid == TogglePattern.Pattern && IsItemWithCheckbox(_hwnd, _item))
            {
                return CreateListViewItemCheckbox();
            }

            if (iid == ScrollItemPattern.Pattern && WindowScroll.IsScrollable(_hwnd))
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
                NativeMethods.Win32Rect itemRectangle = NativeMethods.Win32Rect.Empty;
                Rect boundingRectangle = Rect.Empty;

                // If we have a listview that could support group view we need to use a diferent method
                // to get the builnding rect.  If anything fails trying this way just fall back to the old way
                // of doing things.
                if (_isComctrlV6OnOsVerV6orHigher)
                {
                    itemRectangle.left = NativeMethods.LVIR_BOUNDS;

                    WindowsListViewGroup windowsListViewGroup = _parent as WindowsListViewGroup;
                    if (windowsListViewGroup != null)
                    {
                        // The group might have been destroyed by an event 
                        WindowsListView._groupsCollection.EnsureCreation (_hwnd);

                        GroupManager manager = WindowsListView._groupsCollection[_hwnd];
                        int [] groupIds = manager.GetGroupIds ();

                        // find the index for this group
                        int index;
                        for (index = 0; index < groupIds.Length && windowsListViewGroup.ID != groupIds[index]; index++);

                        if (index >= groupIds.Length)
                        {
                            return boundingRectangle;
                        }
                        
                        NativeMethods.LVITEMINDEX ii = new NativeMethods.LVITEMINDEX();
                        ii.iGroup = index;
                        ii.iItem = _item;

                        unsafe
                        {
                            if (XSendMessage.XSend(_hwnd, NativeMethods.LVM_GETITEMINDEXRECT, new IntPtr(&ii), new IntPtr(&itemRectangle), Marshal.SizeOf(ii.GetType()), Marshal.SizeOf(itemRectangle.GetType())))
                            {
                                if (Misc.MapWindowPoints(_hwnd, IntPtr.Zero, ref itemRectangle, 2))
                                {
                                    // Don't need to normalize, GetItemRect returns absolute coordinates.
                                    boundingRectangle = itemRectangle.ToRect(false);
                                    return boundingRectangle;
                                }
                            }
                        }
                    }
                }
                
                // If this node represents the container itself...
                if (WindowsListView.GetItemRect(_hwnd, _item, NativeMethods.LVIR_BOUNDS, out itemRectangle))
                {
                    // Don't need to normalize, GetItemRect returns absolute coordinates.
                    boundingRectangle = itemRectangle.ToRect(false);
                }
                
                return boundingRectangle;
            }
        }

        //Gets the controls help text
        internal override string HelpText
        {
            get
            {
                // Getting tooltips from Win32 APIs is currently broken.  See WinOS Bugs #1454943.
                //string helpText = WindowsListView.GetItemToolTipText(_hwnd);
                //if (string.IsNullOrEmpty(helpText))
                string helpText = "";
                {
                    // port the msaa impl for this from .\oleacc\listview.cpp
                    // failed to get HelpText from tooltips so try MSAA accHelp
                    IAccessible acc = AccessibleObject;
                    if (acc != null)
                    {
                        // in ListViewItem class _item is idChild - 1
                        Accessible accItem = Accessible.Wrap(acc, _item + 1);
                        if (accItem != null)
                        {
                            helpText = accItem.HelpText;
                        }
                    }
                }

                return string.IsNullOrEmpty(helpText) ? null : helpText;
            }
        }

        //Gets the localized name
        internal override string LocalizedName
        {
            get
            {
                string text = GetText(_hwnd, _item, 0);
                if (string.IsNullOrEmpty(text))
                {
                    IAccessible acc = AccessibleObject;
                    if (acc != null)
                    {
                        // in ListViewItem class _item is idChild - 1
                        Accessible accItem = Accessible.Wrap(acc, _item + 1);
                        if (accItem != null)
                        {
                            text = accItem.Name;
                        }
                    }
                }

                return text;
            }
        }

        // Sets the focus to this item.
        internal override bool SetFocus ()
        {
            // Set the item's state to focused.
            return WindowsListView.SetItemFocused (_hwnd, _item);
        }

        internal override object GetElementProperty(AutomationProperty propertyId)
        {
            if (propertyId == AutomationElement.ClickablePointProperty)
            {
                // Special case ClickablePoint - for details view, we need to
                // return a point on the first item. (The default impl in proxy simple
                // excludes the children when looking for a point.)
                NativeMethods.Win32Point clickPoint;
                if(GetListviewitemClickablePoint (out clickPoint))
                {
                    return new double[] { clickPoint.x, clickPoint.y };
                }
            }
            // EventManager.DispatchEvent() genericaly uses GetElementProperty()
            // to get properties during a property change event.  Proccess ToggleStateProperty
            // so the ToggleStateProperty Change Event can get the correct state.
            else if (propertyId == TogglePattern.ToggleStateProperty
                        && IsItemWithCheckbox(_hwnd, _item))
            {
                IToggleProvider listViewItemCheckbox =
                    (IToggleProvider)CreateListViewItemCheckbox();
                if (listViewItemCheckbox != null)
                {
                    return listViewItemCheckbox.ToggleState;
                }
            }

            return base.GetElementProperty(propertyId);
        }

        #endregion

        #region ProxyFragment Interface

        // Returns the next sibling element in the raw hierarchy.
        // Peripheral controls have always negative values.
        // Returns null if no next child
        internal override ProxySimple GetNextSibling (ProxySimple child)
        {
            int item = child._item;

            if (WindowsListView.IsDetailMode (_hwnd))
            {
                item++;
                int countCol = GetSubItemCount (_hwnd);
                if (item >= 0 && item < countCol)
                {
                    return CreateListViewSubItem (item);
                }
            }

            return null;
        }

        // Returns the previous sibling element in the raw hierarchy.
        // Peripheral controls have always negative values.
        // Returns null is no previous
        internal override ProxySimple GetPreviousSibling (ProxySimple child)
        {
            int item = child._item;

            if (IsItemWithCheckbox(_hwnd, _item) && item == 0)
            {
                return CreateListViewItemCheckbox();
            }
            else if (WindowsListView.IsDetailMode (_hwnd))
            {
                int countCol = GetSubItemCount (_hwnd);
                if (item > 0 && item < countCol)
                {
                    return CreateListViewSubItem(item - 1);
                }
            }

            return null;
        }

        // Returns the first child element in the raw hierarchy.
        internal override ProxySimple GetFirstChild ()
        {
            if (IsItemWithCheckbox(_hwnd, _item))
            {
                return CreateListViewItemCheckbox();
            }
            else if (WindowsListView.IsDetailMode(_hwnd))
            {
                int countCol = GetSubItemCount (_hwnd);
                if (countCol > 0)
                {
                    return CreateListViewSubItem (0);
                }
            }

            return null;
        }

        // Returns the last child element in the raw hierarchy.
        internal override ProxySimple GetLastChild ()
        {
            if (WindowsListView.IsDetailMode (_hwnd))
            {
                int countCol = GetSubItemCount (_hwnd);

                if (countCol > 0)
                {
                    return CreateListViewSubItem (countCol - 1);
                }
            }

            if (IsItemWithCheckbox(_hwnd, _item))
            {
                return CreateListViewItemCheckbox();
            }

            return null;
        }

        // Returns a Proxy element corresponding to the specified screen coordinates.
        internal override ProxySimple ElementProviderFromPoint (int x, int y)
        {
            // Test for the checkbox first
            if (IsItemWithCheckbox (_hwnd, _item))
            {
                NativeMethods.Win32Rect checkboxRectangle = ListViewItemCheckbox.ListViewCheckBoxRect (_hwnd, _item);
                if (!checkboxRectangle.IsEmpty && Misc.PtInRect(ref checkboxRectangle, x, y))
                {
                    // listviewitem checkbox
                    return new ListViewItemCheckbox (_hwnd, this, _item, _checkbox);
                }
            }

            if (WindowsListView.IsDetailMode (_hwnd))
            {
                // test for subitem (returns either subitem or lvitem)
                return ListViewSubItem.ElementProviderFromPoint (_hwnd, this, _item, x, y);
            }

            return this;

        }
        
        // Returns an item corresponding to the focused element (if there is one), or null otherwise.
        internal override ProxySimple GetFocus ()
        {
            if (_isComctrlV6OnOsVerV6orHigher)
            {
                int columns = ListViewItem.GetSubItemCount (_hwnd);
                if (columns > 0)
                {
                    int column = (int)Misc.ProxySendMessage(_hwnd, NativeMethods.LVM_GETFOCUSEDCOLUMN, IntPtr.Zero, IntPtr.Zero);
                    if (column >= 0)
                    {
                        return CreateListViewSubItem (column);
                    }
                }
            }
            
            return this;
        }

        #endregion

        #region SelectionItem Pattern

        // Selects this element
        void ISelectionItemProvider.Select ()
        {
            // Make sure that the control is enabled
            if (!SafeNativeMethods.IsWindowEnabled(_hwnd))
            {
                throw new ElementNotEnabledException();
            }

            // simple case: object already selected - only works for single selection element
            if (!WindowsListView.MultiSelected (_hwnd) && WindowsListView.IsItemSelected (_hwnd, _item))
            {
                return;
            }

            // Unselect all items.
            WindowsListView.UnselectAll (_hwnd);

            // Select the specified item.
            if (!WindowsListView.SelectItem(_hwnd, _item))
            {
                throw new InvalidOperationException(SR.Get(SRID.OperationCannotBePerformed));
            }
        }

        // Adds this element to the selection
        void ISelectionItemProvider.AddToSelection ()
        {
            // Make sure that the control is enabled
            if (!SafeNativeMethods.IsWindowEnabled(_hwnd))
            {
                throw new ElementNotEnabledException();
            }

            // simple case: object already selected
            if (WindowsListView.IsItemSelected (_hwnd, _item))
            {
                return;
            }

            // object does not support multi-selection
            if (!WindowsListView.MultiSelected(_hwnd))
            {
                IRawElementProviderSimple container = ((ISelectionItemProvider)this).SelectionContainer;
                bool selectionRequired = container != null ? ((ISelectionProvider)container).IsSelectionRequired : true;

                // For single selection containers that IsSelectionRequired == false and nothing is selected
                // an AddToSelection is valid.
                if (selectionRequired || WindowsListView.GetSelectedItemCount(_hwnd) > 0)
                {
                    throw new InvalidOperationException(SR.Get(SRID.DoesNotSupportMultipleSelection));
                }
            }

            // At this point we know: Item either supports multiple selection or nothing
            // is selected in the list
            // Try to select an item
            if (!WindowsListView.SelectItem(_hwnd, _item))
            {
                throw new InvalidOperationException(SR.Get(SRID.OperationCannotBePerformed));
            }
        }

        // Removes this element from the selection
        void ISelectionItemProvider.RemoveFromSelection ()
        {
            // Make sure that the control is enabled
            if (!SafeNativeMethods.IsWindowEnabled(_hwnd))
            {
                throw new ElementNotEnabledException();
            }

            // simple case: item is not selected
            if (!WindowsListView.IsItemSelected (_hwnd, _item))
            {
                return;
            }

            // object does not support multi-selection
            if (!WindowsListView.MultiSelected (_hwnd))
            {
                IRawElementProviderSimple container = ((ISelectionItemProvider)this).SelectionContainer;
                bool selectionRequired = container != null ? ((ISelectionProvider)container).IsSelectionRequired : true;

                // For single selection containers that IsSelectionRequired == false a
                // RemoveFromSelection is valid.
                if (selectionRequired)
                {
                    throw new InvalidOperationException(SR.Get(SRID.SelectionRequired));
                }
            }

            // try to unselect the item
            if (!WindowsListView.UnSelectItem(_hwnd, _item))
            {
                throw new InvalidOperationException(SR.Get(SRID.OperationCannotBePerformed));
            }
        }

        // True if this element is part of the the selection
        bool ISelectionItemProvider.IsSelected
        {
            get
            {
                return WindowsListView.IsItemSelected (_hwnd, _item);
            }
        }

        // Returns the container for this element
        IRawElementProviderSimple ISelectionItemProvider.SelectionContainer
        {
            get
            {
                // SelectionContainer is always a listview item
                // However it is possible that between listview item and the listview
                // we have a Group, if this is the case skip it.
                ProxyFragment parent = _parent;

                while (!(parent is WindowsListView))
                {
                    System.Diagnostics.Debug.Assert (parent != null, "Hit null while looking for the SelectionContainer");
                    parent = parent._parent;
                }

                return parent;
            }
        }

        #endregion SelectionItem Pattern

        #region  InvokePattern

        // Same as clicking on a list item.
        void IInvokeProvider.Invoke ()
        {
            // Make sure that the control is enabled
            if (!SafeNativeMethods.IsWindowEnabled(_hwnd))
            {
                throw new ElementNotEnabledException();
            }

            if (WindowsListView.Scrollable (_hwnd))
            {
                // ensure item vertical visibility
                WindowsListView.EnsureVisible (_hwnd, _item, true);
            }

            NativeMethods.Win32Point clickPoint;

            // try to obtaine the clickable point
            if (!GetListviewitemClickablePoint (out clickPoint))
            {
                throw new InvalidOperationException(SR.Get(SRID.OperationCannotBePerformed));
            }

            Click(clickPoint);
        }

        #endregion Invoke Pattern

        #region Value Pattern

        void IValueProvider.SetValue (string val)
        {
            // Make sure that the control is enabled
            if (!SafeNativeMethods.IsWindowEnabled(_hwnd))
            {
                throw new ElementNotEnabledException();
            }

            SetValue(val, _hwnd, _item);
        }

        // Request to get the value that this UI element is representing as a string
        string IValueProvider.Value
        {
            get
            {
                return ListViewItem.GetText (_hwnd, _item, 0);
            }
        }

        // Read only status
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
                if (WindowsListView.IsGroupViewEnabled (_hwnd))
                {
                    return GetItemRowPositionInGroup ();
                }

                // Calculation depends on snakeness of the column
                if (WindowsListView.IsListMode (_hwnd))
                {
                    int itemCount = WindowsListView.GetItemCount (_hwnd);
                    int rowCount = WindowsListView.GetRowCountListMode (_hwnd, itemCount);
                    int column = _item / rowCount;

                    return (_item - (column * rowCount));
                }

                int columnCount = WindowsListView.GetColumnCountOtherModes (_hwnd);

                return _item / columnCount;
            }
        }

        int IGridItemProvider.Column
        {
            get
            {
                if (WindowsListView.IsGroupViewEnabled (_hwnd))
                {
                    return GetItemColumnPositionInGroup ();
                }

                // Calculation depends on snakeness of the column
                if (WindowsListView.IsListMode (_hwnd))
                {
                    int itemCount = WindowsListView.GetItemCount (_hwnd);
                    int rowCount = WindowsListView.GetRowCountListMode (_hwnd, itemCount);

                    return _item / rowCount;
                }

                int columnCount = WindowsListView.GetColumnCountOtherModes (_hwnd);
                int row = _item / columnCount;

                return (_item - (row * columnCount));
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
                return _parent;
            }
        }

        #endregion GridItemPattern

        #region ScrollItem Pattern

        void IScrollItemProvider.ScrollIntoView()
        {
            // Make sure that the control is enabled
            if (!SafeNativeMethods.IsWindowEnabled(_hwnd))
            {
                throw new ElementNotEnabledException();
            }

            // Currently this ignores the alignToTop, as there is no easy way to set something to the bottom of a listbox
            if (!WindowsListView.Scrollable(_hwnd))
            {
                throw new InvalidOperationException(SR.Get(SRID.OperationCannotBePerformed));
            }

            // ensure item vertical visibility
            WindowsListView.EnsureVisible(_hwnd, _item, false);
        }

        #endregion ScrollItem Pattern

        //------------------------------------------------------
        //
        //  Internal Methods
        //
        //------------------------------------------------------

        #region Internal Methods

        internal static int GetSubItemCount (IntPtr hwnd)
        {
            // Subitems are only available in details mode.
            if(WindowsListView.IsDetailMode(hwnd))
            {
                IntPtr hwndHeader = WindowsListView.ListViewGetHeader (hwnd);

                if (hwndHeader == IntPtr.Zero)
                {
                    return 0;
                }

                return WindowsListView.HeaderItemCount (hwndHeader);
            }

            return -1;
        }

        // retrieves listview item/subitem text
        internal static string GetText (IntPtr hwnd, int item, int subitem)
        {
            NativeMethods.LVITEM lvitem = new NativeMethods.LVITEM ();

            lvitem.mask = NativeMethods.LVIF_TEXT;
            lvitem.iItem = item;
            lvitem.iSubItem = subitem;
            return WindowsListView.GetItemText (hwnd, lvitem);
        }

        // detect if given listviewitem has a checkbox
        internal static bool IsItemWithCheckbox (IntPtr hwnd, int item)
        {
            if (!WindowsListView.CheckBoxes (hwnd))
            {
                return false;
            }

            // this listview supports checkbox, detect if
            // current item has it
            return (CheckState.NoCheckbox != (CheckState) WindowsListView.GetCheckedState (hwnd, item));
        }

        // retrieve an id of the group to which this lvitem belongs
        // valid only if lv has groups enabled
        static internal int GetGroupID (IntPtr hwnd, int lvItem)
        {
            System.Diagnostics.Debug.Assert (WindowsListView.IsGroupViewEnabled (hwnd), "GetGroupID: called when lv does not have groups");

            NativeMethods.LVITEM_V6 item = new NativeMethods.LVITEM_V6 ();
            item.mask = NativeMethods.LVIF_GROUPID;
            item.iItem = lvItem;

            if (XSendMessage.GetItem(hwnd, ref item))
            {
                return item.iGroupID;
            }

            return -1;
        }

        internal static void SetValue (string val, IntPtr hwnd, int item)
        {
            // PerSharp/PreFast will flag this as warning 6507/56507: Prefer 'string.IsNullOrEmpty(val)' over checks for null and/or emptiness.
            // An empty strings is valued here, while a null string is not.
            // Therefore we can not use IsNullOrEmpty() here, suppress the warning.
#pragma warning suppress 6507
            if (val == null)
            {
                throw new ArgumentNullException ("val");
            }

            if (!WindowsListView.ListViewEditable (hwnd))
            {
                throw new InvalidOperationException(SR.Get(SRID.ValueReadonly));
            }

            Misc.SetFocus(hwnd);

            // set focus to the item
            WindowsListView.SetItemFocused (hwnd, item);

            // retrieve edit window
            IntPtr hwndEdit = WindowsListView.ListViewEditLabel (hwnd, item);

            if (IntPtr.Zero == hwndEdit)
            {
                throw new InvalidOperationException(SR.Get(SRID.OperationCannotBePerformed));
            }

            // re-use editbox proxy to do the job
            // Note: we will pass null and -1 for some of the parameters
            // this is ok, since the only thing that matters to us is an ability to set the text
            // and these parameters irrelevant for our task
            WindowsEditBox editBox = new WindowsEditBox (hwndEdit, null, -1);

            // get value pattern
            IValueProvider valueProvider = editBox.GetPatternProvider (ValuePattern.Pattern) as IValueProvider;

            // try to set user-provided text
            bool setValueSucceeded = false;
            try
            {
                valueProvider.SetValue (val);
                setValueSucceeded = true;
            }
            finally
            {
                // even if there is a problem doing SetValue need to exit
                // editing mode (e.g. cleanup from editing mode).
                FinishEditing(setValueSucceeded, hwnd, hwndEdit);
            }

            // Need to give some time for the control to do all its processing.
            bool wasTextSet = false;
            for (int i = 0; i < 10; i++)
            {
                System.Threading.Thread.Sleep(1);

                // Now see if the item really got set.
                if (val.Equals(ListViewItem.GetText(hwnd, item, 0)))
                {
                    wasTextSet = true;
                    break;
                }
            }
            if (!wasTextSet)
            {
                throw new InvalidOperationException(SR.Get(SRID.OperationCannotBePerformed));
            }

        }

        #endregion Internal Methods

        //------------------------------------------------------
        //
        //  Internal Fields
        //
        //------------------------------------------------------

        #region Internal Fields

        // Represent the state of the checkbox, for lvitem with checkbox
        internal enum CheckState: int
        {
            NoCheckbox = -1,
            Unchecked = 0,
            Checked = 1
        }

        #endregion Internal Fields

        //------------------------------------------------------
        //
        //  Protected Methods
        //
        //------------------------------------------------------

        #region Protected Methods

        // This routine is only called on elements belonging to an hwnd that has the focus.
        protected override bool IsFocused ()
        {
            return WindowsListView.IsItemFocused (_hwnd, _item);
        }

        // To test if a list view item is offscreen, we need to
        // take into account the fact that it may be obscured by
        // the list view header.
        internal override bool IsOffscreen()
        {
            IntPtr hwndHeader = WindowsListView.ListViewGetHeader(_hwnd);
            if (hwndHeader != IntPtr.Zero)
            {
                NativeMethods.Win32Rect listViewRect = new NativeMethods.Win32Rect();
                NativeMethods.Win32Rect headerRect = new NativeMethods.Win32Rect();
                if (Misc.GetWindowRect(hwndHeader, ref headerRect)
                    && Misc.GetClientRectInScreenCoordinates(_hwnd, ref listViewRect))
                {
                    // Remove the listview header rect.
                    listViewRect.top = headerRect.bottom;

                    NativeMethods.Win32Rect itemRect =
                        new NativeMethods.Win32Rect(BoundingRectangle);
                    if (!listViewRect.IsEmpty
                        && !Misc.IsItemVisible(ref listViewRect, ref itemRect))
                    {
                        return true;
                    }
                }
            }

            return false;
        }


        #endregion

        //------------------------------------------------------
        //
        //  Private Methods
        //
        //------------------------------------------------------

        #region Private Methods

        private ProxySimple CreateListViewSubItem (int index)
        {
            return new ListViewSubItem (_hwnd, this, index, _item);
        }

        private ProxySimple CreateListViewItemCheckbox ()
        {
            return new ListViewItemCheckbox (_hwnd, this, _item, _checkbox);
        }

        // detect if this listviewitem needs to support GridItem pattern
        static private bool IsImplementingGrid (IntPtr hwnd)
        {
            // in the detail mode, GridItem will be implemented on the subitem
            // and not item
            if (WindowsListView.IsDetailMode (hwnd))
            {
                return false;
            }

            return (WindowsListView.IsListMode (hwnd) || WindowsListView.ListViewAutoArrange (hwnd));
        }

        // Obtain clickable point on the listviewitem
        // in the case when one doesnot exist return false
        private bool GetListviewitemClickablePoint (out NativeMethods.Win32Point clickPoint)
        {
            // When this method is called, lv was already scrolled vertically
            // hence item is visible veritcally
            clickPoint.x = clickPoint.y = 0;

            NativeMethods.Win32Rect itemRectangle;

            // Obtain rectangle
            if (!WindowsListView.GetItemRect(_hwnd, _item, NativeMethods.LVIR_LABEL, out itemRectangle))
            {
                return false;
            }

            if (WindowsListView.IsDetailMode (_hwnd) && !WindowsListView.FullRowSelect (_hwnd))
            {
                // LVS_REPORT - possible that we may need to scroll horizontaly
                // Need to implement Bidi?
                NativeMethods.Win32Point pt = new NativeMethods.Win32Point (itemRectangle.left, 0);

                if (!Misc.MapWindowPoints(IntPtr.Zero, _hwnd, ref pt, 1))
                {
                    return false;
                }

                // In client coordinates, hence negative indicates that item is to the left of lv client area
                if (pt.x < 0)
                {
                    ((IScrollItemProvider)this).ScrollIntoView();

                    if (!WindowsListView.GetItemRect(_hwnd, _item, NativeMethods.LVIR_LABEL, out itemRectangle))
                    {
                        return false;
                    }
                }
            }

            clickPoint.x = Math.Min ((itemRectangle.left + 5), (itemRectangle.left + itemRectangle.right) / 2);
            clickPoint.y = (itemRectangle.top + itemRectangle.bottom) / 2;
            return true;
        }

        private void Click (NativeMethods.Win32Point clickPoint)
        {
            Misc.MouseClick(clickPoint.x, clickPoint.y, !WindowsListView.ListViewSingleClickActivate(_hwnd));
        }

        // send an enter key to the control
        // hence finishing label editting
        // Assumption: control has focus
        private static void FinishEditing (bool setValueSucceeded, IntPtr hwnd, IntPtr hwndEdit)
        {
            // If editing was successful exit editing mode using Return key otherwise Esc out
            IntPtr key = (IntPtr)((setValueSucceeded) ? NativeMethods.VK_RETURN : NativeMethods.VK_ESCAPE);

            // get the scankey
            int scanCode = SafeNativeMethods.MapVirtualKey (NativeMethods.VK_RETURN, 0);

            scanCode <<= 16;

            IntPtr keyUpLParam = new IntPtr (scanCode + (1 << 31) + (1 << 30));

            // send keyboard message
            Misc.ProxySendMessage(hwndEdit, NativeMethods.WM_KEYDOWN, key, new IntPtr(scanCode));
            Misc.ProxySendMessage(hwnd, NativeMethods.WM_KEYUP, key, keyUpLParam);
        }

        // retrieve lvitem column position in the Grid
        // To be called only when Group is enabled
        private int GetItemColumnPositionInGroup ()
        {
            // get id of the group to which this item belongs
            int groupID = GetGroupID (_hwnd, _item);

            if (groupID != -1)
            {
                // get position of this lvitem within the array of all items in the group
                GroupManager.GroupInfo groupInfo = WindowsListViewGroup.GetGroupInfo (_hwnd, groupID);

                if (groupInfo)
                {
                    int position = groupInfo.IndexOf (_item); //Array.IndexOf(groupInfo._items, _item);

                    if (position != -1)
                    {
                        // number of columns in the grid
                        int columnCount = WindowsListViewGroup.GetColumnCountExternal (_hwnd, groupID);

                        // item's row position
                        int itemRowPosition = position / columnCount;

                        // item's column position
                        return (position - (itemRowPosition * columnCount));
                    }
                }
            }

            return -1;
        }

        // retrieve lvitem row position in the Grid
        // To be called only when Group is enabled
        private int GetItemRowPositionInGroup ()
        {
            // get id of the group to which this item belongs
            int groupID = GetGroupID (_hwnd, _item);

            if (groupID != -1)
            {
                // get position of this lvitem within the array of all items in the group
                GroupManager.GroupInfo groupInfo = WindowsListViewGroup.GetGroupInfo (_hwnd, groupID);

                if (groupInfo)
                {
                    int position = groupInfo.IndexOf (_item);

                    if (position != -1)
                    {
                        // number of columns in the grid
                        int columnCount = WindowsListViewGroup.GetColumnCountExternal (_hwnd, groupID);

                        // calculate the row position of the item
                        return position / columnCount;
                    }
                }
            }

            return -1;
        }

        #endregion Private Methods

        //------------------------------------------------------
        //
        //  Private Fields
        //
        //------------------------------------------------------

        #region Private Fields

        private const int _checkbox = -1;
        bool _isComctrlV6OnOsVerV6orHigher;
        #endregion Private Fields
    }
}
