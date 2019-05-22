// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// Description: HWND-based ListBox Proxy
//


using System;
using System.Collections;
using System.Text;
using System.Runtime.InteropServices;
using System.ComponentModel;
using System.Windows.Automation;
using System.Windows.Automation.Provider;
using System.Windows;
using Accessibility;
using MS.Win32;

namespace MS.Internal.AutomationProxies
{
    // This class represents ListBox and ListBox with check buttons.
    class WindowsListBox: ProxyHwnd, ISelectionProvider
    {
        // ------------------------------------------------------
        //
        // Constructors
        //
        // ------------------------------------------------------

        #region Constructors

        internal WindowsListBox (IntPtr hwnd, ProxyFragment parent, int item, bool parentedByCombo)
           : base(hwnd, parent, item)
        {
            // Set the strings to return properly the properties.
            _parentedByCombo = parentedByCombo;
            _fIsKeyboardFocusable = true;
            _cControlType = ControlType.List;
            _fIsContent = !_parentedByCombo;

            if (parentedByCombo)
            {
                _sAutomationId = "ListBox"; // This string is a non-localizable string
            }

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
            bool parentedByCombo = false;
            ProxyFragment parent = null;
            int item = 0;

            try
            {
                int style = Misc.GetWindowStyle(hwnd);
                // If can not get windows style the hwnd is bad so do not create a proxy for it.
                if (style == 0)
                {
                    return null;
                }

                if (Misc.IsBitSet(style, NativeMethods.LBS_COMBOBOX))
                {
                    // List portion of combo box
                    NativeMethods.COMBOBOXINFO cbInfo = new NativeMethods.COMBOBOXINFO(NativeMethods.comboboxInfoSize);

                    if (WindowsComboBox.GetComboInfo(hwnd, ref cbInfo) && (cbInfo.hwndCombo != IntPtr.Zero))
                    {
                        parent = (ProxyFragment)WindowsComboBox.Create(cbInfo.hwndCombo, 0);
                        parentedByCombo = true;
                        item = (int)WindowsComboBox.ComboChildren.List;
                    }
                }
            }
            catch (ElementNotAvailableException)
            {
                return null;
            }

            WindowsListBox listbox = new WindowsListBox (hwnd, parent, item, parentedByCombo);

            if (idChild == 0)
            {
                return listbox;
            }
            else
            {
                return listbox.CreateListboxItem(idChild - 1);
            }
        }

        // Static Create method called by the event tracker system
        // WinEvents are one throwns because items exist. so it makes sense to create the item and
        // check for details afterward.
        internal static void RaiseEvents (IntPtr hwnd, int eventId, object idProp, int idObject, int idChild)
        {
            // NOTE: List may be a portion of the Combobox
            // Use WindowsListBox.Create in order to set-up a correct parenthood chain

            switch (idObject)
            {
                case NativeMethods.OBJID_WINDOW:
                    RaiseEventsOnWindow(hwnd, eventId, idProp, idObject, idChild);
                    break;

                case NativeMethods.OBJID_CLIENT:
                    RaiseEventsOnClient(hwnd, eventId, idProp, idObject, idChild);
                    break;

                case NativeMethods.OBJID_VSCROLL :
                case NativeMethods.OBJID_HSCROLL :
                    break;

                default :
                    ProxySimple el = (ProxyHwnd)WindowsListBox.Create(hwnd, 0);
                    if (el != null)
                    {
                        el.DispatchEvents(eventId, idProp, idObject, idChild);
                    }
                    break;
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
        internal override object GetPatternProvider (AutomationPattern iid)
        {
            // selection is always supported
            if (iid == SelectionPattern.Pattern)
            {
                return this;
            }
            // the scroll pattern is only supported when the list is scrollable.
            else if (iid == ScrollPattern.Pattern && WindowScroll.HasScrollableStyle(_hwnd))
            {
                // delegate work to the NonClientArea implementation of IScrollProvider
                IScrollProvider scroll = NonClientArea.Create(_hwnd, 0) as IScrollProvider;

                if (scroll != null)
                {
                    return scroll;
                }
            }


            return null;
        }

        // Process all the Element Properties
        internal override object GetElementProperty(AutomationProperty idProp)
        {
            if (idProp == AutomationElement.IsControlElementProperty)
            {
                return IsParentedByCombo() || SafeNativeMethods.IsWindowVisible(_hwnd);
            }
            else if (idProp == AutomationElement.IsOffscreenProperty)
            {
                if (IsParentedByCombo())
                {
                    // Since the bounding rectangle of a collapsed listbox protion of a combo-box is still 
                    // on the virtal desktop needs to check to make sure the listbox protion is
                    // expanded, i.e. visible.
                    if (!SafeNativeMethods.IsWindowVisible(_hwnd))
                    {
                        return true;
                    }
                }
            }

            return base.GetElementProperty(idProp);
        }

        #endregion

        #region ProxyFragment Interface

        // ------------------------------------------------------
        //
        // RawElementProvider interface implementation
        //
        // ------------------------------------------------------

        // Returns the next sibling element in the raw hierarchy.
        // Peripheral controls have always negative values.
        internal override ProxySimple GetNextSibling (ProxySimple child)
        {
            int item = child._item;
            int count = Length;

            // Next for an item that does not exist in the list
            if (item >= count)
            {
                throw new ElementNotAvailableException ();
            }

            if (item >= 0 && (item + 1) < count)
            {
                return CreateListboxItem (item + 1);
            }
            else
            {
                return base.GetNextSibling (child);
            }
        }

        // Returns the previous sibling element in the raw hierarchy.
        // Peripheral controls have always negative values.
        internal override ProxySimple GetPreviousSibling (ProxySimple child)
        {
            // start with the scrollbars
            ProxySimple ret = base.GetPreviousSibling (child);

            if (ret != null)
            {
                return ret;
            }

            // then try out the items
            int item = child._item;
            int count = Length;

            // Next for an item that does not exist in the list
            if (item >= count)
            {
                throw new ElementNotAvailableException ();
            }

            if (item > 0 && (item) < count)
            {
                return CreateListboxItem (item - 1);
            }
            else
            {
                return item != 0 && count > 0 ? CreateListboxItem (count - 1) : null;
            }
        }

        // Returns the first child element in the raw hierarchy.
        internal override ProxySimple GetFirstChild ()
        {
            if (Length > 0)
            {
                return CreateListboxItem (0);
            }

            // no content go for the scrollbars
            return base.GetFirstChild ();
        }

        // Returns the last child element in the raw hierarchy.
        internal override ProxySimple GetLastChild ()
        {
            // start with the scrollbars
            ProxySimple ret = base.GetFirstChild ();

            if (ret != null)
            {
                return ret;
            }

            int count = Length;

            return count > 0 ? CreateListboxItem (count - 1) : null;
        }

        // Returns a Proxy element corresponding to the specified screen coordinates.
        internal override ProxySimple ElementProviderFromPoint (int x, int y)
        {
            NativeMethods.Win32Rect listboxrect = new NativeMethods.Win32Rect ();

            Misc.GetClientRectInScreenCoordinates(_hwnd, ref listboxrect);
            if (Misc.PtInRect(ref listboxrect, x, y))
            {
                int ret = Misc.ProxySendMessageInt(_hwnd, NativeMethods.LB_ITEMFROMPOINT, IntPtr.Zero, NativeMethods.Util.MAKELPARAM(x - listboxrect.left, y - listboxrect.top));
                if (NativeMethods.Util.HIWORD(ret) == 0)
                {
                    int index = NativeMethods.Util.LOWORD(ret);
                    return CreateListboxItem(index);
                }
            }

            return base.ElementProviderFromPoint (x, y);
        }

        // Returns an item corresponding to the focused element (if there is one), 
        // or null otherwise.
        internal override ProxySimple GetFocus ()
        {
            int index = Misc.ProxySendMessageInt(_hwnd, NativeMethods.LB_GETCARETINDEX, IntPtr.Zero, IntPtr.Zero);

            if (index != NativeMethods.LB_ERR)
            {
                return CreateListboxItem(index);
            }

            return this;
        }

        #endregion

        #region Selection Pattern

        // ------------------------------------------------------
        //
        // ISelectionProvider interface implementation
        //
        // ------------------------------------------------------

        // Returns an enumerator over the current selection.
        IRawElementProviderSimple[] ISelectionProvider.GetSelection()
        {
            int count = Length;
            int countSelection = IsMultipleSelection() ? GetSelectionCount() : 1;

            if (count <= 0 || countSelection <= 0)
            {
                return null;
            }

            IRawElementProviderSimple[] selection = new IRawElementProviderSimple[countSelection];

            int index = 0;
            for (int itemPos = 0; itemPos < count; itemPos++)
            {
                if (ListboxItem.IsSelected(_hwnd, itemPos))
                {
                    selection[index] = CreateListboxItem(itemPos);
                    index++;
                }
            }

            if (index == 0)
            {
                return null;
            }

            return selection;
        }

        // Returns whether the control supports multiple selection.
        bool ISelectionProvider.CanSelectMultiple
        {
            get
            {
                return IsMultipleSelection ();
            }
        }

        // Returns whether the control requires a minimum of one
        // selected element at all times.
        bool ISelectionProvider.IsSelectionRequired
        {
            // If ListBox supports multipleselection - this property always returns false
            // since user can unselect everything using the Ctrl + Click
            // This property is dynamic in the case of single-selected listbox.
            // This should be documented, user should not cached this value (single-selection lb)
            get
            {
                if (IsMultipleSelection ())
                {
                    return false;
                }

                return Misc.ProxySendMessageInt(_hwnd, NativeMethods.LB_GETCURSEL, IntPtr.Zero, IntPtr.Zero) >= 0;
            }
        }

        #endregion

        // ------------------------------------------------------
        //
        // Protected Methods
        //
        // ------------------------------------------------------

        #region Protected Methods

        // Picks a WinEvent to track for a UIA property
        protected override int[] PropertyToWinEvent(AutomationProperty idProp)
        {
            // Upon creation, a single selection Listbox can have no selection to start with
            // however once an item has been selection, the selection cannot be removed.
            // The notification handler is set based on the type of Listbox (single selection) 
            // and if nothing is selected.
            if (idProp == SelectionPattern.IsSelectionRequiredProperty && !IsMultipleSelection() && !HasSelection())
            {
                return new int[] { NativeMethods.EventObjectSelection };
            }

            return base.PropertyToWinEvent(idProp);
        }

        #endregion

        //------------------------------------------------------
        //
        //  Private Methods
        //
        //------------------------------------------------------

        #region Private Method

        // Creates a list item RawElementBase Item
        private ProxySimple CreateListboxItem (int index)
        {
            return new ListboxItem (_hwnd, this, index);
        }

        // Return the number of items (non peripheral) in the listbox.
        private int Length
        {
            get
            {
                return Misc.ProxySendMessageInt(_hwnd, NativeMethods.LB_GETCOUNT, IntPtr.Zero, IntPtr.Zero);
            }
        }

        private bool IsParentedByCombo ()
        {
            return _parentedByCombo;
        }

        private static void RaiseEventsOnClient(IntPtr hwnd, int eventId, object idProp, int idObject, int idChild)
        {
            ProxySimple el = null;

            WindowsListBox wlb = (WindowsListBox)WindowsListBox.Create(hwnd, 0);

            // Upon creation, a single selection Listbox can have no selection to start with
            // however once an item has been selection, the selection cannot be removed.
            // This WinEvent can only be received once, on the first selection.
            // Once the notification is received the notification handler is removed to not get it a 
            // second time.
            if ((eventId == NativeMethods.EventObjectSelection || eventId == NativeMethods.EventObjectSelectionAdd) && (idProp as AutomationProperty) == SelectionPattern.IsSelectionRequiredProperty)
            {
                // This array must be kept in sync with the array in PropertyToWinEvent
                WinEventTracker.EvtIdProperty[] aEvtIdProperties = new WinEventTracker.EvtIdProperty[] { new WinEventTracker.EvtIdProperty(NativeMethods.EventObjectSelection, SelectionPattern.IsSelectionRequiredProperty) };

                WinEventTracker.RemoveToNotificationList(hwnd, aEvtIdProperties, null, aEvtIdProperties.Length);
                el = wlb;
            }
            else if (eventId == NativeMethods.EventObjectSelection || eventId == NativeMethods.EventObjectSelectionRemove || eventId == NativeMethods.EventObjectSelectionAdd)
            {
                bool isMultipleSelection = wlb.IsMultipleSelection();

                // User should send SelectionAdd for a Multiselect listbox but it sends instead
                // Selection. The code below fixes the bug in User 
                if (eventId == NativeMethods.EventObjectSelection && isMultipleSelection && wlb.HasOtherSelections(idChild - 1))
                {
                    eventId = NativeMethods.EventObjectSelectionAdd;
                }

                // The spec says a ElementSelectionEvent should be fired when action causes only one 
                // selection.
                if ((eventId == NativeMethods.EventObjectSelectionRemove || eventId == NativeMethods.EventObjectSelectionAdd) &&
                    isMultipleSelection && wlb.GetSelectionCount() == 1)
                {
                    // The net result of the user action is that there is only one item selected in the
                    // listbox, so change the event to an EventObjectSelected.
                    idProp = SelectionItemPattern.ElementSelectedEvent;
                    eventId = NativeMethods.EventObjectSelection;

                    // Now need to find what item is selected.
                    int selection = wlb.GetOtherSelection(idChild - 1);
                    if (selection != NativeMethods.LB_ERR)
                    {
                        idChild = selection;
                    }
                }

                el = wlb.CreateListboxItem(idChild - 1);
            }
            else
            {
                el = wlb;
            }

            // Special case for logical element change for listbox item
            if ((idProp as AutomationEvent) == AutomationElement.StructureChangedEvent &&
                (eventId == NativeMethods.EventObjectDestroy || eventId == NativeMethods.EventObjectCreate))
            {
                // Since children are referenced by position in the tree, addition and removal
                // of items leads to different results when asking properties for the same element
                // On removal, item + 1 is now item!
                // Use Children Invalidated to let the client knows that all the cached 
                AutomationInteropProvider.RaiseStructureChangedEvent( wlb, new StructureChangedEventArgs( StructureChangeType.ChildrenInvalidated, wlb.MakeRuntimeId() ) );
                return;
            }

            if (el != null)
            {
                el.DispatchEvents(eventId, idProp, idObject, idChild);
            }
        }

        private static void RaiseEventsOnWindow(IntPtr hwnd, int eventId, object idProp, int idObject, int idChild)
        {
            AutomationProperty automationProperty = idProp as AutomationProperty;
            ProxySimple el = null;

            if ((eventId == NativeMethods.EventObjectShow || eventId == NativeMethods.EventObjectHide) &&
                (automationProperty != null && automationProperty == ExpandCollapsePattern.ExpandCollapseStateProperty))
            {
                if (Misc.IsBitSet(Misc.GetWindowStyle(hwnd), NativeMethods.LBS_COMBOBOX))
                {
                    // List portion of combo: We'll hit it in the case when user hovers over it
                    NativeMethods.COMBOBOXINFO cbInfo = new NativeMethods.COMBOBOXINFO(NativeMethods.comboboxInfoSize);

                    if (WindowsComboBox.GetComboInfo(hwnd, ref cbInfo) && (cbInfo.hwndCombo != IntPtr.Zero))
                    {
                        WindowsComboBox cb = (WindowsComboBox)WindowsComboBox.Create(cbInfo.hwndCombo, 0);

                        if (!cb.IsSimpleCombo())
                        {
                            el = cb;
                        }
                    }
                }
            }

            if (el != null)
            {
                el.DispatchEvents(eventId, idProp, idObject, idChild);
            }
        }

        #region Selection Pattern Helpers

        private int GetOtherSelection(int skipItem)
        {
            for (int i = 0, count = Length; i < count; i++)
            {
                if (i != skipItem && ListboxItem.IsSelected(_hwnd, i))
                {
                    // Win32 listbox items are 0 based, UIAutomation listbox items are 1 based.
                    return i + 1;
                }
            }

            return NativeMethods.LB_ERR;
        }

        private int GetSelectionCount()
        {
            int result = Misc.ProxySendMessageInt(_hwnd, NativeMethods.LB_GETSELCOUNT, IntPtr.Zero, IntPtr.Zero);
            return result != NativeMethods.LB_ERR ? result : 0;
        }

        // Detect if there any selections in
        // This is used by the WindowsListBoxItem class
        // returns true if any items are selected
        private bool HasSelection ()
        {
            int i, count;

            for (i = 0, count = Length; i < count && !ListboxItem.IsSelected (_hwnd, i); i++)
                ;

            return (i < count);
        }

        // Detect if listbox has any element except skipItem selected
        // This is used by the WindowsListBoxItem class
        // returns true if any items
        private bool HasOtherSelections (int skipItem)
        {
            for (int i = 0, count = Length; i < count; i++)
            {
                if (i != skipItem && ListboxItem.IsSelected (_hwnd, i))
                {
                    return true;
                }
            }

            return false;
        }

        // Clears all elements in the multiple-selection  listbox
        // This is used by the WindowsListBoxItem class
        // returns true if operation succeeded
        private bool ClearAll ()
        {
            // clear all possible only in the multi-select case
            System.Diagnostics.Debug.Assert (IsMultipleSelection (), "Calling ClearAll on single-selected listbox");

            return Misc.ProxySendMessageInt(_hwnd, NativeMethods.LB_SETSEL, IntPtr.Zero, new IntPtr(-1)) != NativeMethods.LB_ERR;
        }

        // Check the LBS_MULTIPLESEL or/and LBS_EXTENDEDSEL for multiple selection
        // This is used by WindowListBoxItem class
        // returns true if multiple selection is supported
        private bool IsMultipleSelection ()
        {
            return (0 != (WindowStyle & (NativeMethods.LBS_MULTIPLESEL | NativeMethods.LBS_EXTENDEDSEL)));
        }

        private bool IsWinFormCheckedListBox()
        {
            if (WindowsFormsHelper.IsWindowsFormsControl(_hwnd))
            {
                return ((WindowStyle & NativeMethods.LBS_OWNERDRAWFIXED) == NativeMethods.LBS_OWNERDRAWFIXED) &&
                    ((WindowStyle & NativeMethods.LBS_WANTKEYBOARDINPUT) == NativeMethods.LBS_WANTKEYBOARDINPUT);
            }
            return false;
        }


        #endregion

        #endregion

        //------------------------------------------------------
        //
        //  Private Fields
        //
        //------------------------------------------------------

        #region Private Fields

        private bool _parentedByCombo;

        #endregion

        // ------------------------------------------------------
        //
        //  ListboxItem Private Class
        //
        //------------------------------------------------------

        #region ListBoxItem

        // Summary description for WindowsListboxItem.
        class ListboxItem : ProxySimple, ISelectionItemProvider, IScrollItemProvider, IToggleProvider
        {
            // ------------------------------------------------------
            //
            // Constructors
            //
            // ------------------------------------------------------

            #region Constructors

            // Constructor.
            internal ListboxItem (IntPtr hwnd, ProxyFragment parent, int item)
            : base (hwnd, parent, item)
            {
                // Set the strings to return properly the properties.
                _cControlType = ControlType.ListItem;
                _fHasPersistentID = false;
                _fIsKeyboardFocusable = true;
                _listBox = (WindowsListBox) parent;
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
                if (iid == SelectionItemPattern.Pattern)
                {
                    return this;
                }
                else if (iid == ScrollItemPattern.Pattern && WindowScroll.IsScrollable(_hwnd))
                {
                    return this;
                }
                else if (_listBox.IsWinFormCheckedListBox() && iid == TogglePattern.Pattern)
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
                    // Don't need to normalize, LB_GETITEMRECT returns absolute coordinates.
                    return BoundingRect().ToRect(false);                    
                }
            }

            //Gets the localized name
            // Returns the text of ListBox item. Check value is not included.
            internal override string LocalizedName
            {
                get
                {
                    // NOTE: List can be a part of the combobox. Many times
                    // sending LB_ message will work, however many apps have ownerdraw combos,
                    // in this case only CB_ type message will work
                    if (((WindowsListBox)_parent).IsParentedByCombo())
                    {
                        WindowsComboBox cb = (WindowsComboBox)_parent._parent;
                        return cb.GetListItemText(_item);
                    }


                    int iTextLen = Misc.ProxySendMessageInt(_hwnd, NativeMethods.LB_GETTEXTLEN, new IntPtr(_item), IntPtr.Zero);

                    if (iTextLen != 0)
                    {
                        if (Misc.GetClassName(_hwnd).Equals("Internet Explorer_TridentLstBox"))
                        {
                            // The Trident listbox is a superclassed standard listbox.
                            // Trident listboxes are owner draw that does not have the hasstring style set.
                            // All the control contains is the owner draw data and not text.  Private
                            // messages were added to retrieve the owner draw data as text.  The new messages
                            // are used just like the normally LB_GETTEXT and CB_GETTEXT messages.
                            return XSendMessage.GetItemText(_hwnd, NativeMethods.WM_USER + NativeMethods.LB_GETTEXT, _item, iTextLen);
                        }
                        else
                        {
                            string text = Misc.GetUnsafeText(_hwnd, NativeMethods.LB_GETTEXT, new IntPtr(_item), iTextLen);
                            // The application engineer has most likely hidden associated information in the 
                            // listbox item's text with a tab, '\t'.  If this is the case remove that hidden 
                            // information.
                            int iPos = text.IndexOf('\t');
                            if (iPos > 0)
                            {
                                text = text.Substring(0, iPos);
                            }

                            return text;
                        }
                    }

                    return "";
                }
            }

            #endregion

            #region SelectionItem Pattern

            // Selects this element
            void ISelectionItemProvider.Select ()
            {
                // Check that control can be interacted with.
                // This state could change anytime
                if (!SafeNativeMethods.IsWindowEnabled(_hwnd))
                {
                    throw new ElementNotEnabledException();
                }

                // get needed info about lbItem
                bool multipleSelected = _listBox.IsMultipleSelection ();
                bool itemSelected = IsSelected (_hwnd, _item);
                bool parentedByCombo = _listBox.IsParentedByCombo();

                if (multipleSelected)
                {
                    if (_listBox.HasOtherSelections(_item))
                    {   // some other elements are selected
                        // unselect all. It is just simplier to unselect all
                        // and re-select us, than unselect each element one by one
                        _listBox.ClearAll ();
                    }
                    else if (itemSelected)
                    {
                        // multi-selected lbItem with
                        // only 1 selected element - us
                        return;
                    }
                }
                else if (itemSelected)
                {
                    // if it is a combo, then always perform the selection otherwise the listbox won't disappear
                    if (!parentedByCombo)
                    {
                        // single-selection and we selected already
                        return;
                    }
                }

                if (parentedByCombo)
                {
                    // if this is a combo and the listbox is not displayed the selection will not stick, so 
                    // display the listbox before doing the select.
                    if (((IExpandCollapseProvider)_listBox._parent).ExpandCollapseState == ExpandCollapseState.Collapsed)
                    {
                        ((IExpandCollapseProvider)_listBox._parent).Expand();
                    }
                }

                // do the selection
                if (!Select(multipleSelected))
                {
                    throw new InvalidOperationException(SR.Get(SRID.OperationCannotBePerformed));
                }
            }

            // Adds this element to the selection
            void ISelectionItemProvider.AddToSelection ()
            {
                // Check that control can be interacted with.
                // This state could change anytime
                if (!SafeNativeMethods.IsWindowEnabled(_hwnd))
                {
                    throw new ElementNotEnabledException();
                }

                // check if item already selected
                if (ListboxItem.IsSelected (_hwnd, _item) && !_listBox.IsParentedByCombo())
                {
                    // if it is a combo, then always perform the selection otherwise the listbox won't disappear
                    return;
                }

                bool multipleSelection = _listBox.IsMultipleSelection();

                // object does not support multi-selection
                if (!multipleSelection)
                {
                    IRawElementProviderSimple container = ((ISelectionItemProvider)this).SelectionContainer;
                    bool selectionRequired = container != null ? ((ISelectionProvider)container).IsSelectionRequired : true;

                    // For single selection containers that IsSelectionRequired == false and nothing is selected
                    // an AddToSelection is valid.
                    if (selectionRequired || _listBox.HasSelection())
                    {
                        throw new InvalidOperationException(SR.Get(SRID.DoesNotSupportMultipleSelection));
                    }
                }

                if (_listBox.IsParentedByCombo())
                {
                    // if this is a combo and the listbox is not displayed the selection will not stick, so 
                    // display the listbox before doing the select.
                    if (((IExpandCollapseProvider)_listBox._parent).ExpandCollapseState == ExpandCollapseState.Collapsed)
                    {
                        ((IExpandCollapseProvider)_listBox._parent).Expand();
                    }
                }

                // At this point we know: Item either supports multiple selection or nothing
                // is selected in the list
                // Try to select an item
                if (!Select(multipleSelection))
                {
                    throw new InvalidOperationException(SR.Get(SRID.OperationCannotBePerformed));
                }
            }

            // Removes this element from the selection
            void ISelectionItemProvider.RemoveFromSelection ()
            {
                // Check that control can be interacted with.
                // This state could change anytime
                if (!SafeNativeMethods.IsWindowEnabled(_hwnd))
                {
                    throw new ElementNotEnabledException();
                }

                if (!IsSelected(_hwnd, _item))
                {
                    // simple case, item is not selected
                    return;
                }

                // object does not support multi-selection
                if (!_listBox.IsMultipleSelection())
                {
                    // single-selected lb - user cannot remove the selection using keyboard and mouse
                    // At this point we know that item is selected, lb is single-selected hence
                    // RemoveFromSelection is not possible
                    throw new InvalidOperationException(SR.Get(SRID.SelectionRequired));
                }

                if (!UnSelect(_hwnd, _item))
                {
                    throw new InvalidOperationException(SR.Get(SRID.OperationCannotBePerformed));
                }
            }

            // True if this element is part of the the selection
            bool ISelectionItemProvider.IsSelected
            {
                get
                {
                    return ListboxItem.IsSelected (_hwnd, _item);
                }
            }

            // Returns the container for this element
            IRawElementProviderSimple ISelectionItemProvider.SelectionContainer
            {
                get
                {
                    System.Diagnostics.Debug.Assert (_parent is WindowsListBox, "Invalid Parent for a Listbox Item");
                    return _parent;
                }
            }

            #endregion SelectionItem Pattern

            #region ScrollItem Pattern

            void IScrollItemProvider.ScrollIntoView ()
            {
                if (_listBox._parentedByCombo && !SafeNativeMethods.IsWindowVisible(_hwnd))
                {
                    throw new InvalidOperationException(SR.Get(SRID.OperationCannotBePerformed));
                }

                if (!WindowScroll.IsScrollable(_hwnd))
                {
                    throw new InvalidOperationException(SR.Get(SRID.OperationCannotBePerformed));
                }

                // It is assumed that in a listbox an item will always be smaller than the scrolling area
                Misc.ProxySendMessage(_hwnd, NativeMethods.LB_SETTOPINDEX, new IntPtr(_item), IntPtr.Zero);
            }

            #endregion ScrollItem Pattern

            #region IToggleProvider

            void IToggleProvider.Toggle()
            {
                // Check that button can be clicked
                // This state could change anytime
                if (!SafeNativeMethods.IsWindowEnabled(_hwnd))
                {
                    throw new ElementNotEnabledException();
                }
                if (!SafeNativeMethods.IsWindowVisible(_hwnd))
                {
                    throw new InvalidOperationException(SR.Get(SRID.OperationCannotBePerformed));
                }

                Toggle();
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

            #region Selection Pattern Helpers

            // Detects if an item is selected
            internal static bool IsSelected (IntPtr hwnd, int item)
            {
                return Misc.ProxySendMessageInt(hwnd, NativeMethods.LB_GETSEL, new IntPtr(item), IntPtr.Zero) > 0;
            }

            internal static bool Select (IntPtr hwnd, int item, bool fMultipleSelection)
            {
                int SendMessageResult = 0;

                if (fMultipleSelection)
                {
                    SendMessageResult = Misc.ProxySendMessageInt(hwnd, NativeMethods.LB_SETSEL, new IntPtr(1), new IntPtr(item));
                }
                else
                {
                    SendMessageResult = Misc.ProxySendMessageInt(hwnd, NativeMethods.LB_SETCURSEL, new IntPtr(item), IntPtr.Zero);
                }

                return NativeMethods.LB_ERR != SendMessageResult;
            }

            #endregion

            #region Focus Helper

            // Returns an item corresponding to the focused element (if there is one), or null otherwise.
            internal override bool SetFocus ()
            {
                if (_listBox.IsMultipleSelection ())
                {
                    return Misc.ProxySendMessageInt(_hwnd, NativeMethods.LB_SETCARETINDEX, new IntPtr(_item), new IntPtr(0)) != NativeMethods.LB_ERR;
                }
                else
                {
                    return Select (_hwnd, _item, false);
                }
            }

            #endregion

            #endregion

            //------------------------------------------------------
            //
            //  Protected Methods
            //
            //------------------------------------------------------

            #region Protected Methods

            // This routine is only called on elements belonging to an hwnd that has the focus.
            protected override bool IsFocused ()
            {
                if (_listBox.IsMultipleSelection ())
                {
                    return Misc.ProxySendMessageInt(_hwnd, NativeMethods.LB_GETCARETINDEX, IntPtr.Zero, IntPtr.Zero) == _item;
                }
                else
                {
                    return Misc.ProxySendMessageInt(_hwnd, NativeMethods.LB_GETSEL, new IntPtr(_item), IntPtr.Zero) >= 0;
                }
            }

            #endregion

            //------------------------------------------------------
            //
            //  Private Methods
            //
            //------------------------------------------------------

            #region Private Methods

            private NativeMethods.Win32Rect BoundingRect()
            {
                NativeMethods.Win32Rect itemRect = new NativeMethods.Win32Rect();
                Misc.ProxySendMessage(_hwnd, NativeMethods.LB_GETITEMRECT, new IntPtr(_item), ref itemRect);
                return Misc.MapWindowPoints(_hwnd, IntPtr.Zero, ref itemRect, 2) ? itemRect : NativeMethods.Win32Rect.Empty;
            }

            // Process all the Toggle Properties
            private ToggleState ToggleState
            {
                get
                {
                    ToggleState icsState = ToggleState.Indeterminate;

                    // Special handling for forms
                    if (_windowsForms == WindowsFormsHelper.FormControlState.Undeterminate)
                    {
                        _windowsForms = WindowsFormsHelper.GetControlState(_hwnd);
                    }

                    if (_windowsForms == WindowsFormsHelper.FormControlState.True)
                    {
                        int childrenReturned;
                        object[] accChildren = Accessible.GetAccessibleChildren(this.AccessibleObject, out childrenReturned);
                        IAccessible accChild = (IAccessible)accChildren[_item];

                        if (((int)accChild.get_accState(NativeMethods.CHILD_SELF) & NativeMethods.STATE_SYSTEM_CHECKED) == NativeMethods.STATE_SYSTEM_CHECKED)
                        {
                            icsState = ToggleState.On;
                        }
                        else if (((int)accChild.get_accState(NativeMethods.CHILD_SELF) & NativeMethods.STATE_SYSTEM_MIXED) == NativeMethods.STATE_SYSTEM_MIXED)
                        {
                            icsState = ToggleState.Indeterminate;
                        }
                        else
                        {
                            icsState = ToggleState.Off;
                        }
                    }
                    return icsState;
                }
            }

            #region Selection Pattern Helpers

            private void Toggle()
            {
                // Convoluted way fake a mouse action
                NativeMethods.Win32Point pt = new NativeMethods.Win32Point();
                if (GetClickablePoint(out pt, false))
                {
                    // Mouse method is used here because following methods fail:
                    //     -BM_CLICK message doesn't work with all buttons (e.g. Start)
                    //     -WM_MOUSEACTIVATE + WM_LBUTTONDOWN + WM_LBUTTONUP messages don't work with all buttons
                    //     -WM_KEYDOWN + WM_KEYUP messages for space bar
                    //     -SendKeyboardInput for space bar
                    // See prior versions of this file for alternative code.
                    //
                    Misc.MouseClick(pt.x, pt.y);
                }
            }

            private bool Select(bool fMultipleSelection)
            {
                int sendMessageResult = 0;
                bool success = true;

                if (!((WindowsListBox)_parent).IsParentedByCombo())
                {
                    if (fMultipleSelection)
                    {
                        sendMessageResult =
                            Misc.ProxySendMessageInt(_hwnd, NativeMethods.LB_SETSEL, new IntPtr(1), new IntPtr(_item));
                    }
                    else
                    {
                        sendMessageResult =
                            Misc.ProxySendMessageInt(_hwnd, NativeMethods.LB_SETCURSEL, new IntPtr(_item), IntPtr.Zero);
                    }

                    success = (NativeMethods.LB_ERR != sendMessageResult);
                    if (success)
                    {
                        // Whether multiple selection or not, send LBN_SELCHANGE.
                        // This is normally sent when a user presses an arrow key, but
                        // NOT when LB_SETCURSEL is sent programmatically.  We need to
                        // mimic the action of a user, including all side effects.
                        int listBoxStyle = Misc.GetWindowStyle(_hwnd);
                        if (Misc.IsBitSet(listBoxStyle, NativeMethods.LBS_NOTIFY))
                        {
                            // Get the child ID of the listbox in its parent hwnd.
                            int idListBox = Misc.GetWindowId(_hwnd);
                            IntPtr wParam =
                                new IntPtr(NativeMethods.Util.MAKELONG(
                                                idListBox, NativeMethods.LBN_SELCHANGE));
                            IntPtr hwndListBoxParent = Misc.GetParent(_hwnd);
                            // The return value indicates whether the WM_COMMAND was processed,
                            // which is irrelevant, so ignore the return value here.
                            Misc.ProxySendMessageInt(
                                hwndListBoxParent, NativeMethods.WM_COMMAND, wParam, _hwnd);
                        }
                    }
                }
                else
                {
                    ProxyFragment combo = (WindowsComboBox)_parent._parent;
                    sendMessageResult =
                        Misc.ProxySendMessageInt(_hwnd, NativeMethods.LB_SETCURSEL, new IntPtr(_item), IntPtr.Zero);
                    success = (NativeMethods.LB_ERR != sendMessageResult);
                    if (success)
                    {
                        int id = Misc.GetWindowId(_hwnd);
                        IntPtr wParam = new IntPtr(NativeMethods.Util.MAKELONG(id, NativeMethods.LBN_SELCHANGE));
                        // The return value indicates whether the WM_COMMAND was processed,
                        // which is irrelevant, so ignore the return value here.
                        Misc.ProxySendMessageInt(combo._hwnd, NativeMethods.WM_COMMAND, wParam, _hwnd);
                    }
                }

                return success;
            }

            // This method should be called only on multi-selected listbox
            private bool UnSelect (IntPtr hwnd, int item)
            {
                // Even though listbox item can be unselected in the single-selected lb programmaticaly
                // user cannot do it using the keyboard and mouse hence we will not permit the "removal" of selection in the
                // single-selected listbox
                //  ProxySendMessage(hwnd, NativeMethods.LB_SETCURSEL, new IntPtr (-1), IntPtr.Zero, ref SendMessageResult );
                System.Diagnostics.Debug.Assert (_listBox.IsMultipleSelection (), "Calling UnSelect on single-selected listbox");

                return Misc.ProxySendMessageInt(hwnd, NativeMethods.LB_SETSEL, IntPtr.Zero, new IntPtr(item)) != NativeMethods.LB_ERR;
            }

            #endregion

            #endregion

            //------------------------------------------------------
            //
            //  Private Fields
            //
            //------------------------------------------------------

            #region Private Fields

            private WindowsListBox _listBox;

            #endregion

        }

        #endregion

    }
}
