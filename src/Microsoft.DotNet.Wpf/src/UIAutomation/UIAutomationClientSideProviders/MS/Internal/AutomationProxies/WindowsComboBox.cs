// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// Description: Win32 Combobox proxy

using System;
using System.Globalization;
using System.Text;
using System.ComponentModel;
using System.Windows.Automation;
using System.Windows.Automation.Provider;
using System.Windows;
using System.Runtime.InteropServices;
using MS.Win32;
using NativeMethodsSetLastError = MS.Internal.UIAutomationClientSideProviders.NativeMethodsSetLastError;

namespace MS.Internal.AutomationProxies
{
    // Combobox Logical Tree and Patterns (NOTE: Edit is removed, by Design)
    //
    //  Combo box  (Commands, ExpandCollapse, Selection, Text, Value)
    //    --List box  (Selection, Scroll, Text)  [container]
    //        -----List item (Selection Item, Text)
    //        -----List item (Selection Item, Text)
    //        -----List item (Selection Item, Text)
    //    --DropDownButton peripheral  (Invoke)
    //
    // NOTE: We will reparent the List portion of the combo (ComboLBox) by
    //       1. We will provide a proxy for List portion (actually we will reuse WindowsListBox for it)
    //       2. List Host will refer to the ComboLBox hwnd
    //       3. Providing an ability to navigate from Combo to List and returtning Combo as
    //          List's parent

    // Combobox proxy
    class WindowsComboBox : ProxyHwnd, IValueProvider, IExpandCollapseProvider
    {

        //------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------

        #region Constructor

        WindowsComboBox (IntPtr hwnd, ProxyFragment parent, IntPtr hwndEx, int item)
            : base(hwnd, parent, item)
        {
            _cControlType = ControlType.ComboBox;
            _hwndEx = hwndEx;
            _comboType = GetComboType ();
            _fIsKeyboardFocusable = true;
            _createOnEvent = new WinEventTracker.ProxyRaiseEvents (RaiseEvents);
        }


        #endregion Constructor

        #region Proxy Create

        // Static Create method called by UIAutomation to create this proxy.
        // returns null if unsuccessful</returns>
        internal static IRawElementProviderSimple Create(IntPtr hwnd, int idChild, int idObject)
        {
            return Create(hwnd, idChild);
        }

        internal static IRawElementProviderSimple Create(IntPtr hwnd, int idChild)
        {
            // Something is wrong if idChild is not zero
            if (idChild != 0)
            {
                System.Diagnostics.Debug.Assert (idChild == 0, "Invalid Child Id, idChild != 0");
                throw new ArgumentOutOfRangeException("idChild", idChild, SR.Get(SRID.ShouldBeZero));
            }

            return new WindowsComboBox(hwnd, null, HostedByComboEx(hwnd), idChild);
        }

        #endregion

        //------------------------------------------------------
        //
        //  Pattern Implementation
        //
        //------------------------------------------------------

        #region ProxySimple Interface

        // Returns a pattern interface if supported.
        internal override object GetPatternProvider (AutomationPattern iid)
        {
            if (iid == ValuePattern.Pattern && IsEditableCombo())
            {
                return this;
            }
            else if (iid == ExpandCollapsePattern.Pattern && !IsSimpleCombo ())
            {
                return this;
            }
            else if (iid == SelectionPattern.Pattern)
            {
                // delegate work to the WindowsListBox implementation of ISelection
                ProxySimple list = CreateListBox ();

                if (list != null)
                {
                    ISelectionProvider selection = list.GetPatternProvider (iid) as ISelectionProvider;

                    if (selection != null)
                    {
                        return selection;
                    }
                }
            }

            return null;
        }

        // Gets the bounding rectangle for this element
        internal override Rect BoundingRectangle
        {
            get
            {
                NativeMethods.Win32Rect rcCombo = new NativeMethods.Win32Rect (base.BoundingRectangle);

                if (GetDroppedState (_hwnd))
                {
                    // NOTE: Do not use CB_GETDROPPEDCONTROLRECT
                    // it will not produce the correct rect
                    NativeMethods.COMBOBOXINFO cbInfo = new NativeMethods.COMBOBOXINFO(NativeMethods.comboboxInfoSize);

                    if (GetComboInfo(_hwnd, ref cbInfo))
                    {
                        NativeMethods.Win32Rect rcList = NativeMethods.Win32Rect.Empty;

                        if (!Misc.GetWindowRect(cbInfo.hwndList, ref rcList))
                        {
                            return Rect.Empty;
                        }
                        if (!Misc.UnionRect(out rcCombo, ref rcCombo, ref rcList))
                        {
                            return Rect.Empty;
                        }
                    }
                }

                return rcCombo.ToRect(Misc.IsControlRTL(_hwnd));
            }
        }

        // Process all the Logical and Raw Element Properties
        internal override object GetElementProperty (AutomationProperty idProp)
        {
            if (idProp == AutomationElement.NameProperty)
            {
                if (Misc.GetClassName(_hwnd).Equals("Internet Explorer_TridentCmboBx"))
                {
                    object result = base.GetElementProperty(idProp);
                    // Return an empty string instead of null to prevent the default HWND proxy
                    // from using the window text as the name - since the window text for this owner-
                    // draw trident combo is garbage.
                    return result == null ? "" : (string)result;
                }
            }
            // EventManager.DispatchEvent() genericaly uses GetElementProperty()
            // to get properties during a property change event.  Proccess ExpandCollapseStateProperty
            // so the ExpandCollapseStateProperty Change Event can get the correct state.
            else if (idProp == ExpandCollapsePattern.ExpandCollapseStateProperty)
            {
                return ((IExpandCollapseProvider)this).ExpandCollapseState;
            }

            return base.GetElementProperty (idProp);
        }

        internal override ProxySimple [] GetEmbeddedFragmentRoots ()
        {
            // Only applies to drop-down lists, which reparent the list, need to do this...
            if (IsSimpleCombo())
                return null;

            // Because we are moving the listbox portion of the combo from the being a child of the
            // desktop to a child of the combo,  We have to tell automation the that this is one of our
            // children so the listbox will get advised of events added/removed.
            return new ProxySimple[] { CreateListBox() };
        }

        //Gets the localized name
        internal override string LocalizedName
        {
            get
            {
                // In the case of ComboBoxEx32, this element's _hwnd will be the
                // HWND of the ComboBox which is a child of ComboBoxEx the lable that names
                // Combobox however is a sibling of ComboBoxEx32, hence we need to handle this case here
                if (IsComboBoxEx32())
                {
                    return Misc.GetControlName(Misc.GetLabelhwnd(_hwndEx), true);
                }
                return null;
            }
        }

        #endregion ProxySimple Interface

        #region ProxyFragment Interface

        // Returns the next sibling element in the raw hierarchy.
        // Peripheral controls have always negative values.
        // Returns null if no next child
        internal override ProxySimple GetNextSibling (ProxySimple child)
        {
            if (child._item == (int) ComboChildren.List && !IsSimpleCombo ())
            {
                return CreateComboButton ();
            }

            return null;
        }

        // Returns the previous sibling element in the raw hierarchy.
        // Peripheral controls have always negative values.
        // Returns null is no previous
        internal override ProxySimple GetPreviousSibling (ProxySimple child)
        {
            if (child._item == (int) ComboChildren.Button)
            {
                return CreateListBox ();
            }

            return null;
        }

        // Returns the first child element in the raw hierarchy.
        internal override ProxySimple GetFirstChild ()
        {
            // List portion is a first child of the combo-box
            return CreateListBox ();
        }

        // Returns the last child element in the raw hierarchy.
        internal override ProxySimple GetLastChild ()
        {
            if (IsSimpleCombo ())
            {
                // there is no DropDown button
                return CreateListBox ();
            }

            return CreateComboButton ();
        }

        // Returns a Proxy element corresponding to the specified screen coordinates.
        internal override ProxySimple ElementProviderFromPoint (int x, int y)
        {
            NativeMethods.COMBOBOXINFO cbInfo = new NativeMethods.COMBOBOXINFO(NativeMethods.comboboxInfoSize);

            if (GetComboInfo(_hwnd, ref cbInfo))
            {
                // check for button
                if (Misc.MapWindowPoints(_hwnd, IntPtr.Zero, ref cbInfo.rcButton, 2))
                {
                    if (Misc.PtInRect(ref cbInfo.rcButton, x, y))
                    {
                        return new WindowsComboButton(_hwnd, this, (int)ComboChildren.Button);
                    }
                }

                // check for list
                if (SafeNativeMethods.IsWindowVisible(cbInfo.hwndList))
                {
                    NativeMethods.Win32Rect rcList = NativeMethods.Win32Rect.Empty;

                    if (Misc.GetWindowRect(cbInfo.hwndList, ref rcList) &&
                        Misc.PtInRect(ref rcList, x, y))
                    {
                        ProxySimple el = CreateListBox(cbInfo.hwndList);

                        return ((WindowsListBox) el).ElementProviderFromPoint (x, y);
                    }
                }
            }

            return this;
        }

        #endregion

        #region ProxyHwnd Interface

        // override the default implementation so we can handle the WinEvents that are send to the edit
        // portion of ComboBox (Combo proxy will hide edit portion from the user, but will take care of all
        // the features/functionality that Edit presents) and some(show, hide) events that are send to the List portion of combo
        // In both cases this events will be presented to the user as Combo's LE events
        internal override void AdviseEventAdded (AutomationEvent eventId, AutomationProperty [] aidProps)
        {
            // call the base class implementation first to add combo-specific things and register combo specific callback
            base.AdviseEventAdded (eventId, aidProps);

            NativeMethods.COMBOBOXINFO cbInfo = new NativeMethods.COMBOBOXINFO(NativeMethods.comboboxInfoSize);

            if (GetComboInfo(_hwnd, ref cbInfo))
            {
                if (eventId == AutomationElement.AutomationPropertyChangedEvent)
                {
                    // ComboBoxEx32 controls with the style CBS_DROPDOWNLIST are still editable.
                    if (cbInfo.hwndItem != IntPtr.Zero && IsEditableCombo())
                    {
                        // subscribe to edit-specific notifications, that would be presented as combo le event
                        // ValueAsString, ValueAsObject, IsReadOnly
                        // create array containing events that user is interested in
                        WinEventTracker.EvtIdProperty [] editPortionEvents;
                        int counter;

                        CreateEditPortionEvents (out editPortionEvents, out counter, aidProps);
                        if ( counter > 0 )
                        {
                            WinEventTracker.AddToNotificationList( cbInfo.hwndItem, new WinEventTracker.ProxyRaiseEvents( EditPortionEvents ), editPortionEvents, counter );
                        }
                    }
                }

                // Need to also advise the list portions of the combobox so that it can raise events.
                if (cbInfo.hwndList != IntPtr.Zero)
                {
                    WindowsListBox listbox = new WindowsListBox(cbInfo.hwndList, this, 0, true);
                    listbox.AdviseEventAdded(eventId, aidProps);
                }
            }
        }

        internal override void AdviseEventRemoved (AutomationEvent eventId, AutomationProperty [] aidProps)
        {
            // remove combo-related events
            base.AdviseEventRemoved (eventId, aidProps);

            NativeMethods.COMBOBOXINFO cbInfo = new NativeMethods.COMBOBOXINFO(NativeMethods.comboboxInfoSize);

            if (GetComboInfo(_hwnd, ref cbInfo))
            {
                // remove edit and list specific events that got remapped into combo's events
                if (eventId == AutomationElement.AutomationPropertyChangedEvent)
                {
                    // ComboBoxEx32 controls with the style CBS_DROPDOWNLIST are still editable.
                    if (cbInfo.hwndItem != IntPtr.Zero && IsEditableCombo())
                    {
                        // un-subscribe from edit-specific notifications
                        // ValueAsString, ValueAsObject, IsReadOnly
                        // create array containing events from which user wants to unsubscribe
                        WinEventTracker.EvtIdProperty [] editPortionEvents;
                        int counter;

                        CreateEditPortionEvents (out editPortionEvents, out counter, aidProps);
                        if ( counter > 0 )
                        {
                            WinEventTracker.RemoveToNotificationList( cbInfo.hwndItem, editPortionEvents, null, counter );
                        }
                    }
                }

                // Need to also remove the advise from the list portions of the combobox.
                if (cbInfo.hwndList != IntPtr.Zero)
                {
                    WindowsListBox listbox = new WindowsListBox(cbInfo.hwndList, this, 0, true);
                    listbox.AdviseEventRemoved(eventId, aidProps);
                }
            }
        }

        #endregion

        #region Value Pattern

        // Sets the text of the edit part of the Combo
        void IValueProvider.SetValue (string str)
        {
            // Ensure that the window and all its parents are enabled.
            Misc.CheckEnabled(_hwnd);

            // piggy-back on win32editbox proxy
            NativeMethods.COMBOBOXINFO cbInfo = new NativeMethods.COMBOBOXINFO(NativeMethods.comboboxInfoSize);

            if (GetComboInfo(_hwnd, ref cbInfo) && SafeNativeMethods.IsWindowVisible(cbInfo.hwndItem))
            {
                WindowsEditBox editBox = new WindowsEditBox(cbInfo.hwndItem, null, -1);
                IValueProvider valueProvider = (IValueProvider) editBox.GetPatternProvider (ValuePattern.Pattern);

                // try to set user-provided text
                valueProvider.SetValue (str);

                // Let the parent know that the value has change, to allow the parent to do any processing it needs
                // to do on value change.
                IntPtr hwndParent = Misc.GetParent(_hwnd);
                if (hwndParent != IntPtr.Zero)
                {
                    int id = Misc.GetWindowId(_hwnd);
                    IntPtr wParam = new IntPtr(NativeMethods.Util.MAKELONG(id, NativeMethods.CBN_EDITUPDATE));

                    Misc.ProxySendMessage(hwndParent, NativeMethods.WM_COMMAND, wParam, _hwnd);
                }

                return;
            }

            throw new InvalidOperationException(SR.Get(SRID.OperationCannotBePerformed));
        }

        // Request to get the value that this UI element is representing as a string
        string IValueProvider.Value
        {
            get
            {
                return Text;
            }
        }

        bool IValueProvider.IsReadOnly
        {
            get
            {
                if (!SafeNativeMethods.IsWindowEnabled(_hwnd))
                {
                    return true;
                }

                // ComboBoxEx32 controls with the style CBS_DROPDOWNLIST are still editable.
                if (IsComboBoxEx32())
                {
                    return false;
                }
                else
                {
                    return IsDropDownListCombo();
                }
            }
        }


        #endregion ValuePattern

        #region ExpandCollapse Pattern

        void IExpandCollapseProvider.Expand ()
        {
            // Ensure that the window and all its parents are enabled.
            Misc.CheckEnabled(_hwnd);

            if (GetDroppedState (_hwnd))
            {
                // list portion is already visible
                return;
            }

            Expand (_hwnd);
        }

        void IExpandCollapseProvider.Collapse ()
        {
            // Ensure that the window and all its parents are enabled.
            Misc.CheckEnabled(_hwnd);

            if (!GetDroppedState (_hwnd))
            {
                // list portion is already collapsed
                return;
            }

            Collapse(_hwnd);
        }

        ExpandCollapseState IExpandCollapseProvider.ExpandCollapseState
        {
            get
            {
                return (GetDroppedState (_hwnd)) ? ExpandCollapseState.Expanded : ExpandCollapseState.Collapsed;
            }
        }

        #endregion ExpandCollapse Pattern

        //------------------------------------------------------
        //
        //  Internal Methods
        //
        //------------------------------------------------------

        #region Internal Methods

        internal bool IsSimpleCombo ()
        {
            return (NativeMethods.CBS_SIMPLE == _comboType);
        }

        // no edit portion....
        internal bool IsDropDownListCombo ()
        {
            return (NativeMethods.CBS_DROPDOWNLIST == _comboType);
        }

        // get text of the listboxitem (external use)
        internal string GetListItemText (int index)
        {
            IntPtr hwndToAsk = IsComboBoxEx32() ? _hwndEx : _hwnd;

            return SpecialText (hwndToAsk, index);
        }

        // Detect if our combobox is hosted inside of comboex
        // This is important to know becuase:
        // Real styles will be provided by comboex
        // comboex supplies the edit
        static internal IntPtr HostedByComboEx (IntPtr hwnd)
        {
            IntPtr hwndEx = NativeMethodsSetLastError.GetAncestor (hwnd, NativeMethods.GA_PARENT);

            if ((IntPtr.Zero != hwndEx) && IsComboEx (hwndEx))
            {
                return hwndEx;
            }

            return IntPtr.Zero;
        }

        // Wrapper on top of Win32's GetComboInfo
        static internal bool GetComboInfo(IntPtr hwnd, ref NativeMethods.COMBOBOXINFO cbInfo)
        {
            bool result = Misc.GetComboBoxInfo(hwnd, ref cbInfo);

            if (result)
            {
                // some combo boxes do not have an edit portion
                // instead they return combo hwnd in the item
                // to make our life easier  set hwndItem to IntPtr.Zero
                if (cbInfo.hwndItem == cbInfo.hwndCombo)
                {
                    cbInfo.hwndItem = IntPtr.Zero;
                }

                // Possible that Combo is hosted by ComboboxEx32
                // hence GetComboBoxInfo did not provide us with edit.
                // We should try to detect it ourselves
                if (cbInfo.hwndItem == IntPtr.Zero && IsComboEx (NativeMethodsSetLastError.GetAncestor (hwnd, NativeMethods.GA_PARENT)))
                {
                    cbInfo.hwndItem = Misc.FindWindowEx(hwnd, IntPtr.Zero, "EDIT", null);
                    if (cbInfo.hwndItem != IntPtr.Zero)
                    {
                        result = Misc.GetWindowRect(cbInfo.hwndItem, ref cbInfo.rcItem);
                        if( result)
                        {
                            result = Misc.MapWindowPoints(_hwndDesktop, hwnd, ref cbInfo.rcItem, 2);
                        }

                        if (!result)
                        {
                            cbInfo.rcItem = NativeMethods.Win32Rect.Empty;
                        }
                    }
                }
            }

            return result;
        }
        // determin if the list portion of combo is dropped
        static internal bool GetDroppedState (IntPtr hwnd)
        {
            return Misc.ProxySendMessageInt(hwnd, NativeMethods.CB_GETDROPPEDSTATE, IntPtr.Zero, IntPtr.Zero) != 0;
        }
        // expand the list portion
        static internal void Expand (IntPtr hwnd)
        {
            IntPtr hwndFocused = Misc.GetFocusedWindow();

            NativeMethods.COMBOBOXINFO cbInfo = new NativeMethods.COMBOBOXINFO(NativeMethods.comboboxInfoSize);
            GetComboInfo(hwnd, ref cbInfo);

            // if the combobox does not already have focus, set the focus.
            if (hwndFocused != hwnd && hwndFocused != cbInfo.hwndCombo && hwndFocused != cbInfo.hwndItem && hwndFocused != cbInfo.hwndList)
            {
                Misc.SetFocus(hwnd);
            }
            Misc.ProxySendMessage(hwnd, NativeMethods.CB_SHOWDROPDOWN, new IntPtr(1), IntPtr.Zero);
        }
        // collapse the list portion
        static internal void Collapse (IntPtr hwnd)
        {
            Misc.ProxySendMessage(hwnd, NativeMethods.CB_SHOWDROPDOWN, new IntPtr(0), IntPtr.Zero);
        }

        #endregion Internal Methods

        //------------------------------------------------------
        //
        //  Internal Fields
        //
        //------------------------------------------------------

        #region Internal Fields

        // enum describing children of the combobox
        internal enum ComboChildren: int
        {
            Button = -1,
            List = 2
        }

        internal const string Combobox = "ComboBox";

        #endregion Internal Fields

        //------------------------------------------------------
        //
        //  Private Methods
        //
        //------------------------------------------------------

        #region Private Methods

        // retrieve combobox text
        private string Text
        {
            get
            {
                if (IsDropDownListCombo() && IsComboBoxEx32())
                {
                    // special case ComboboxEx droplist
                    string text = SpecialText(_hwndEx, -1);
                    if (!string.IsNullOrEmpty(text))
                    {
                        return text;
                    }
                }

                // all other cases
                return Misc.ProxyGetText(IsComboBoxEx32() ? _hwndEx : _hwnd);
            }
        }

        private int GetComboType()
        {
            IntPtr hwnd = IsComboBoxEx32() ? _hwndEx : _hwnd;

            return (Misc.GetWindowStyle(hwnd) & NativeMethods.CBS_COMBOTYPEMASK);
        }

        // create combo button
        private ProxySimple CreateComboButton ()
        {
            NativeMethods.COMBOBOXINFO cbInfo = new NativeMethods.COMBOBOXINFO(NativeMethods.comboboxInfoSize);

            if (GetComboInfo(_hwnd, ref cbInfo) && cbInfo.stateButton != NativeMethods.STATE_SYSTEM_INVISIBLE)
            {
                return new WindowsComboButton (_hwnd, this, (int) ComboChildren.Button);
            }

            return null;
        }

        // create list portion of combo box
        private ProxySimple CreateListBox()
        {
            NativeMethods.COMBOBOXINFO cbInfo = new NativeMethods.COMBOBOXINFO(NativeMethods.comboboxInfoSize);
            if (GetComboInfo(_hwnd, ref cbInfo) && (IntPtr.Zero != cbInfo.hwndList))
            {
                return new WindowsListBox(cbInfo.hwndList, this, (int)ComboChildren.List, true);
            }
            return null;
        }

        // create listbox from known hwnd
        private ProxySimple CreateListBox (IntPtr hwndList)
        {
            return new WindowsListBox(hwndList, this, (int)ComboChildren.List, true);
        }

        // detect if passed int window corresponds to the comboex
        static private bool IsComboEx (IntPtr hwndEx)
        {
            if (hwndEx == IntPtr.Zero)
            {
                return false;
            }

            return (0 == String.Compare(Misc.GetClassName(hwndEx), ComboboxEx32, StringComparison.OrdinalIgnoreCase));
        }

        private bool IsEditableCombo()
        {
            // Standard Combo Box controls with the style CBS_DROPDOWNLIST has the edit control replaced by a
            // static text item to displays the current selection in the list box.  This is not true of
            // ComboBoxEx32 controls with the style CBS_DROPDOWNLIST.
            return !IsDropDownListCombo() || IsComboBoxEx32();
        }

        // Retrieve the text of the list portion of Combo.
        // Or Text of the edit portion of ComboBoxEx32 (path -1 as index)
        // Use CB_XXX instead of LB_XXX, since CB_XXX will give us back text in ownerdrawn combo
        static private string SpecialText (IntPtr hwnd, int index)
        {
            if (index == -1)
            {
                // get the selected element
                index = Misc.ProxySendMessageInt(hwnd, NativeMethods.CB_GETCURSEL, IntPtr.Zero, IntPtr.Zero);
                if (index == -1)
                {
                    return "";
                }
            }

            int len = Misc.ProxySendMessageInt(hwnd, NativeMethods.CB_GETLBTEXTLEN, new IntPtr(index), IntPtr.Zero);

            if (len < 1)
            {
                return "";
            }

            if (Misc.GetClassName(hwnd).Equals("Internet Explorer_TridentCmboBx"))
            {
                // The Trident listbox is a superclassed standard listbox.
                // Trident listboxes are owner draw that does not have the hasstring style set.
                // All the control contains is the owner draw data and not text.  Private
                // messages were added to retrieve the owner draw data as text.  The new messages
                // are used just like the normally LB_GETTEXT and CB_GETTEXT messages.
                return XSendMessage.GetItemText(hwnd, NativeMethods.WM_USER + NativeMethods.CB_GETLBTEXT, index, len);
            }
            else
            {
                return Misc.GetUnsafeText(hwnd, NativeMethods.CB_GETLBTEXT, new IntPtr(index), len);
            }
        }

        // Combo-specific events
        static private void RaiseEvents (IntPtr hwnd, int eventId, object idProp, int idObject, int idChild)
        {
            // ------------------------------------------------------/////////////////////////////////////
            //
            // Depending of the type of Combobox we will get different WinEvents
            //
            // Simple: No events
            //
            // DropDown: OBJ_STATECHANGE: for the dropbutton button (idChild == 2)
            // NOTE: that OBJECT_STATECHANGE will only be generated when user uses button to show the list
            // if user uses the button to hide the list, event will not be generated
            //
            // DropDownList: OBJ_STATECHANGE (same as above)
            //             : OBJECT_VALUECHANGE - when using the keyboard to navigate between list children
            //                                  - no need to handle it here, ListBox proxy will take care of that
            //
            // ------------------------------------------------------//////////////////////////////////////
            ProxySimple el = null;

            if (idProp is AutomationProperty && idProp == ExpandCollapsePattern.ExpandCollapseStateProperty)
            {
                // expand/collapse events are handled in WindowsListBox with the ComboLBox hwnd.
                // so just return here so we don't fire extraneous events
                return;
            }

            switch (idObject)
            {
                case NativeMethods.OBJID_CLIENT :
                    {
                        if (eventId == NativeMethods.EventObjectStateChange && idChild == 2)
                        {
                            // event came for combobutton
                            // We will be getting 2 OBJECT_STATECHANGED event
                            // one with button state pressed and another normal
                            // both indicate the same invoke event
                            // hence second event is a duplicate of the first one and we need to filter it out
                            NativeMethods.COMBOBOXINFO cbInfo = new NativeMethods.COMBOBOXINFO(NativeMethods.comboboxInfoSize);

                            if (WindowsComboBox.GetComboInfo(hwnd, ref cbInfo) && Misc.IsBitSet(NativeMethods.STATE_SYSTEM_PRESSED, cbInfo.stateButton))
                            {
                                // The event could be for both the button and the combo
                                WindowsComboBox cb = (WindowsComboBox) Create (hwnd, 0);
                                cb.DispatchEvents (eventId, idProp, idObject, idChild);

                                el = cb.CreateComboButton ();
                                el.DispatchEvents (eventId, idProp, idObject, idChild);
                                return;
                            }
                        }
                        el = (ProxySimple) Create (hwnd, 0);
                        break;
                    }

            }

            if (el != null)
            {
                el.DispatchEvents (eventId, idProp, idObject, idChild);
            }
        }

        // Handles combo's edit portion specific events
        // as combo-specific events
        private static void EditPortionEvents (IntPtr hwnd, int eventId, object idProp, int idObject, int idChild)
        {
            // Get hwnd of the combo-box
            IntPtr hwndCombo = NativeMethodsSetLastError.GetAncestor (hwnd, NativeMethods.GA_PARENT);

            if (hwndCombo != IntPtr.Zero)
            {
                ProxySimple el = (ProxySimple) Create (hwndCombo, 0);

                el.DispatchEvents (eventId, idProp, idObject, idChild);
            }
        }

        // Return an array that contains combo's edit portion specific events
        // These events will be remapped as combo box events
        private static void CreateEditPortionEvents (out WinEventTracker.EvtIdProperty [] editPortionEvents, out int counter, AutomationProperty [] aidProps)
        {
            // count how many events to pass back for the edit part of combo
            int c = 0;
            foreach ( AutomationProperty p in aidProps )
            {
                if ( p == ValuePattern.ValueProperty || p == ValuePattern.IsReadOnlyProperty )
                {
                    c++;
                }
            }

            if (c == 0)
            {
                editPortionEvents = null;
                counter = 0;
                return;
            }

            // allocate array with the number of events from above
            editPortionEvents = new WinEventTracker.EvtIdProperty[c];

            c = 0;
            foreach ( AutomationProperty p in aidProps )
            {
                if ( p == ValuePattern.ValueProperty || p == ValuePattern.IsReadOnlyProperty )
                {
                    editPortionEvents[c]._idProp = p;
                    editPortionEvents[c]._evtId = (p == ValuePattern.ValueProperty) ? NativeMethods.EventObjectValueChange : NativeMethods.EventObjectStateChange;
                    c++;
                }
            }

            counter = c;
        }

        // When _hwndEx is not IntPtr.Zero the control is a ComboBoxEx32 control.
        private bool IsComboBoxEx32()
        {
            return _hwndEx != IntPtr.Zero;
        }

        #endregion Private Methods

        //------------------------------------------------------
        //
        //  Private Fields
        //
        //------------------------------------------------------

        #region Private fields

        // HWND of ComboBoxEx32 (if exist)
        private IntPtr _hwndEx;

        private int _comboType;

        private const string ComboboxEx32 = "ComboBoxEx32";

        #endregion Private fields

        //------------------------------------------------------
        //
        //  WindowsComboButton Private Class
        //
        //------------------------------------------------------

        #region WindowsComboButton

        // Proxy for ComboBox button
        class WindowsComboButton: ProxySimple, IInvokeProvider
        {
            //------------------------------------------------------
            //
            //  Constructors
            //
            //------------------------------------------------------

            #region Constructor

            internal WindowsComboButton (IntPtr hwnd, ProxyFragment parent, int item)
            : base(hwnd, parent, item)
            {
                _cControlType = ControlType.Button;
                _sAutomationId = "DropDown"; // This string is a non-localizable string
            }

            #endregion Constructor

            //------------------------------------------------------
            //
            //  Pattern Implementation
            //
            //------------------------------------------------------

            #region ProxySimple Interface

            // Returns a pattern interface if supported.
            internal override object GetPatternProvider (AutomationPattern iid)
            {
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
                    NativeMethods.COMBOBOXINFO cbInfo = new NativeMethods.COMBOBOXINFO(NativeMethods.comboboxInfoSize);

                    if (!WindowsComboBox.GetComboInfo(_hwnd, ref cbInfo))
                    {
                        return Rect.Empty;
                    }
                    if (!Misc.MapWindowPoints(_hwnd, IntPtr.Zero, ref cbInfo.rcButton, 2))
                    {
                        return Rect.Empty;
                    }

                    // Don't need to normalize, MapWindowPoints returns absolute coordinates.
                    return cbInfo.rcButton.ToRect(false);
                }
            }

            //Gets the localized name
            internal override string LocalizedName
            {
                get
                {
                    return SR.Get(SRID.LocalizedNameWindowsComboButton);
                }
            }

            #endregion ProxySimple Interface

            #region Invoke Pattern

            // Same effect as a click on the drop down button
            void IInvokeProvider.Invoke ()
            {
                // Ensure that the window and all its parents are enabled.
                Misc.CheckEnabled(_hwnd);

                if (!WindowsComboBox.GetDroppedState (_hwnd))
                {
                    WindowsComboBox.Expand (_hwnd);
                }
                else
                {
                    WindowsComboBox.Collapse (_hwnd);
                }
            }

            #endregion Invoke Pattern
        }

        #endregion

    }
}
