// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// Description: Wraps some of IAccessible to support getting basic properties
//              and default action
//


// PRESHARP: In order to avoid generating warnings about unkown message numbers and unknown pragmas.
#pragma warning disable 1634, 1691

using System;
using System.Diagnostics;
using System.Collections;
using System.Globalization;
using System.Threading;
using System.Windows.Automation;
using System.Windows;
using Accessibility;
using System.Text;
using System.Runtime.InteropServices;
using MS.Win32;

namespace MS.Internal.AutomationProxies
{
    // values return from IAccessible.get_accRole.
    internal enum AccessibleRole: int
    {
        TitleBar = 0x1,
        MenuBar = 0x2,
        ScrollBar = 0x3,
        Grip = 0x4,
        Sound = 0x5,
        Cursor = 0x6,
        Caret = 0x7,
        Alert = 0x8,
        Window = 0x9,
        Client = 0xa,
        MenuPopup = 0xb,
        MenuItem = 0xc,
        Tooltip = 0xd,
        Application = 0xe,
        Document = 0xf,
        Pane = 0x10,
        Chart = 0x11,
        Dialog = 0x12,
        Border = 0x13,
        Grouping = 0x14,
        Separator = 0x15,
        ToolBar = 0x16,
        StatusBar = 0x17,
        Table = 0x18,
        ColumnHeader = 0x19,
        RowHeader = 0x1a,
        Column = 0x1b,
        Row = 0x1c,
        Cell = 0x1d,
        Link = 0x1e,
        HelpBalloon = 0x1f,
        Character = 0x20,
        List = 0x21,
        ListItem = 0x22,
        Outline = 0x23,
        OutlineItem = 0x24,
        PageTab = 0x25,
        PropertyPage = 0x26,
        Indicator = 0x27,
        Graphic = 0x28,
        StaticText = 0x29,
        Text = 0x2a,
        PushButton = 0x2b,
        CheckButton = 0x2c,
        RadioButton = 0x2d,
        Combobox = 0x2e,
        DropList = 0x2f,
        ProgressBar = 0x30,
        Dial = 0x31,
        HotKeyField = 0x32,
        Slider = 0x33,
        SpinButton = 0x34,
        Diagram = 0x35,
        Animation = 0x36,
        Equation = 0x37,
        ButtonDropDown = 0x38,
        ButtonMenu = 0x39,
        ButtonDropDownGrid = 0x3a,
        Whitespace = 0x3b,
        PageTabList = 0x3c,
        Clock = 0x3d,
        SplitButton = 0x3e,
        IpAddress = 0x3f,
        OutlineButton = 0x40,
    }

    // values returned from IAccessible.get_accState.
    [Flags]
    internal enum AccessibleState: int
    {
        Normal =            0x00000000,
        Unavailable =       0x00000001,
        Selected =          0x00000002,
        Focused =           0x00000004,
        Pressed =           0x00000008,
        Checked =           0x00000010,
        Mixed =             0x00000020,
        ReadOnly =          0x00000040,
        HotTracked =        0x00000080,
        Default =           0x00000100,
        Expanded =          0x00000200,
        Collapsed =         0x00000400,
        Busy =              0x00000800,
        Floating =          0x00001000,
        Marqueed =          0x00002000,
        Animated =          0x00004000,
        Invisible =         0x00008000,
        Offscreen =         0x00010000,
        Sizeable =          0x00020000,
        Moveable =          0x00040000,
        SelfVoicing =       0x00080000,
        Focusable =         0x00100000,
        Selectable =        0x00200000,
        Linked =            0x00400000,
        Traversed =         0x00800000,
        Multiselectable =   0x01000000,
        ExtSelectable =     0x02000000,
        AlertLow =          0x04000000,
        AlertMedium =       0x08000000,
        AlertHigh =         0x10000000,
        Protected =         0x20000000,
    }

    internal class Accessible
    {
        //------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------
 
        #region Constructors

        // CONSIDER - When we have a split dll consider adding a pInvoke call that returns the commonly
        // used properties batched up.  Need to see if there are common sets of properties that are
        // gotten.  VPT suggests that one pInvoke that makes multiple COM calls call may be more performant
        // than making the multiple COM calls from managed code.

        // called by Wrap to create a node Accessible that manages its children
        private Accessible(IAccessible acc, int idChild)
        {
            Debug.Assert(acc != null, "null IAccessible");
            _acc = acc;
            _idChild = idChild;
            _accessibleChildrenIndex = -1;
        }

        // Here we are re-implementing AccessibleObjectFromEvent with a critical difference.
        // AccessibleObjectFromEvent, via AccessibleObjectFromWindow, will default to a standard implementation
        // of IAccessible if the window doesn't have a native implementation.
        // However we only want to succeed constructing the object if the window has a native implementation.
        internal static Accessible CreateNativeFromEvent(IntPtr hwnd, int idObject, int idChild)
        {
            // On Vista, pass PID as wParam - allows credUI scenario to work (OLEACC needs to know our PID to
            // DuplicateHandle back to this process.)
            IntPtr wParam = IntPtr.Zero;
            if(Environment.OSVersion.Version.Major >= 6)
                wParam = new IntPtr(UnsafeNativeMethods.GetCurrentProcessId());

            // send the window a WM_GETOBJECT message requesting the specific object id.
            IntPtr lResult = Misc.ProxySendMessage(hwnd, NativeMethods.WM_GETOBJECT, wParam, new IntPtr(idObject));
            if (lResult == IntPtr.Zero)
            {
                return null;
            }

            // unwrap the pointer that was returned
            IAccessible acc = null;
            int hr = NativeMethods.S_FALSE;

            try
            {
                hr = UnsafeNativeMethods.ObjectFromLresult(lResult, ref UnsafeNativeMethods.IID_IAccessible, wParam, ref acc);
            }
            catch (InvalidCastException)
            {
                // CLR remoting appears to be interfering in cases where the remote IAccessible is a Winforms control -
                // the object we get back is a __Transparent proxy, and casting that to IAccessible fails with an exception
                // (which is caught and ignored here).
                // One way around this is to use AccessibleObjectFromWindow - that returns IAccessible instead of IUnknown - 
                // in effect it does the case in unmanaged code, and seems to avoid this issue. Other winforms code in the
                // proxies uses this approach. AccessibleObjectFromWindow will return an OLEACC proxy if one is available,
                // however, so we can't use it here, as this code only wants to deal with native IAccessibles.
                return null;
            }

            // ObjectFromLresult returns an IAccessible from the remote process. If that impl is managed, however, then
            // the local CLR will set up a CLR-Remoting-based connection instead and bypass COM - this causes problems
            // because CLR Remoting typically isn't initalized, so calls fail with a RemotingException: "This remoting
            // proxy has no channel sink which means either the server has no registered server channels that are listening,
            // or this application has no suitable client channel to talk to the server." The local CLR
            // detects that the remote object is a managed impl by QI'ing for a specifc interface (IManagedObject?).
            // We can prevent this from happening by dropping the IAccessible we've gotten back from ObjectFromLresult,
            // and instead use AccessibleObjectFromWindow: AOFW wraps the real IAccessible in a DynamicAnnotation wrapper
            // that passes through all IAccessible (and related interface) calls - but it doesn't pass through the
            // IManagedObject interface, so the CLR treats it as a COM object, and continues to use plain COM to access it,
            // avoiding the CLR Remoting issues.
            //
            // In effect, we're sending WM_GETOBJECT to the HWND to see if there's a native impl there, using ObjectFromLresult
            // just to free that object, and then using AccessibleObjectFromWindow on the window to get the IAccessible
            // that we actually use. (Can't just use AccessibleObjectFromWindow from the start, since that would return
            // an oleacc proxy for hwnds that don't support IAccessible natively, and we only care about actual IAccessible
            // impls here.)
            //
            // We used to do use AOFW below only if the acc we got back above was a managed object (checked using
            // !Marshal.IsComObject()) - that only protects us from managed IAccessibles we get back directly; we could
            // still hit the above issue if we get back a remote unmanaged impl that then returns a maanged impl via
            // navigation (Media Center does this). So we now use AOFW all the time.
            if(hr == NativeMethods.S_OK && acc != null)
            {
                object obj = null;
                hr = UnsafeNativeMethods.AccessibleObjectFromWindow(hwnd, idObject, ref UnsafeNativeMethods.IID_IUnknown, ref obj);
                acc = obj as IAccessible;
            }

            if (hr != NativeMethods.S_OK || acc == null)
            {
                return null;
            }

            // This takes care of calling get_accChild, if necessary...
            return AccessibleFromObject(idChild, acc);
        }

#if DEBUG
        /*
        // strictly for debugging purposes:
        public override string ToString()
        {
            try
            {
                return string.Format( "{0} \"{1}\" {2} {3}", RoleText, Name, _idChild, Window );
            }
            catch (Exception e)
            {
                return e.Message;
            }
        }

        [DllImport( "oleacc.dll" )]
        internal static extern int GetRoleText( int dwRole, StringBuilder lpszRole, int cchRoleMax );

        private string RoleText
        {
            get
            {
                const int cch = 64;
                StringBuilder sb = new StringBuilder( cch );
                int len = GetRoleText( (int)Role, sb, cch );
                return sb.ToString().Substring( 0, len );
            }
        }
        */
#endif

        #endregion Constructors


        //------------------------------------------------------
        //
        //  Internal Methods
        //
        //------------------------------------------------------
 
        #region Internal Methods

        // returns a new Accessible for the IAccessible, or null if IAccessible is null
        internal static Accessible Wrap(IAccessible acc)
        {
            return Wrap(acc, NativeMethods.CHILD_SELF);
        }

        // returns a new Accessible for the IAccessible + child id, or null if IAccessible is null
        internal static Accessible Wrap(IAccessible acc, int idChild)
        {
            return acc != null ? new Accessible(acc, idChild) : null;
        }

        internal IAccessible IAccessible   { get { return _acc; } }
        internal int    ChildCount         { get { return GetChildCount(_acc); } }
        internal int    ChildId            { get { return _idChild; } }
        internal string Description        { get { return GetDescription(_acc, _idChild); } }
        internal string KeyboardShortcut   { get { return GetKeyboardShortcut(_acc, _idChild); } }
        internal string Name               { get { return GetName(_acc, _idChild); } }
        internal string DefaultAction      { get { return GetDefaultAction(_acc, _idChild); } }
        internal AccessibleRole Role { get { return GetRole(_acc, _idChild); } }
        internal bool   IsPassword         { get { return HasState(AccessibleState.Protected); } }
        internal bool   IsSelected         { get { return HasState(AccessibleState.Selected); } }
        internal bool   IsMultiSelectable  { get { return HasState(AccessibleState.Multiselectable); } }
        internal bool   IsIndeterminate    { get { return HasState(AccessibleState.Mixed); } }
        internal bool   IsChecked          { get { return HasState(AccessibleState.Checked); } }
        internal bool   IsReadOnly         { get { return HasState(AccessibleState.ReadOnly); } }
        internal bool   IsEnabled          { get { return ! HasState(AccessibleState.Unavailable); } }
        internal bool   IsFocused          { get { return HasState(AccessibleState.Focused); } }
        internal bool   IsOffScreen        { get { return HasState(AccessibleState.Offscreen); } }

        internal Accessible FirstChild      
        { 
            get 
            {
                return _idChild == NativeMethods.CHILD_SELF ? GetChildAt(_acc, null, 0) : null;
            }
        }

        internal Accessible LastChild
        {
            get
            {
                return _idChild == NativeMethods.CHILD_SELF ? GetChildAt(_acc, null, Accessible.GetChildCount(_acc) - 1) : null;
            }
        }

        internal Accessible NextSibling(Accessible parent)
        {
            Debug.Assert(parent != null);

            // if this object doesn't yet have an index into parent's children find it
            object[] children = null; // if we need to get children to find an index; re-use them
            if (_accessibleChildrenIndex == -1)
            {
                children = SetAccessibleChildrenIndexAndGetChildren(parent._acc);

                // if unable to find this child (broken IAccessible impl?) bail (should we throw here?)
                if (_accessibleChildrenIndex == -1)
                {
                    Debug.Assert(false);
                    return null;
                }
            }

            Accessible rval = null;
            if (_accessibleChildrenIndex + 1 < Accessible.GetChildCount(parent._acc))
            {
                rval = GetChildAt(parent._acc, children, _accessibleChildrenIndex + 1);
            }

            return rval;
        }

        internal Accessible PreviousSibling(Accessible parent)
        {
            Debug.Assert(parent != null);

            // if this object doesn't yet have an index into parent's children find it
            object[] children = null; // if we need to get children to find an index; re-use them
            if (_accessibleChildrenIndex == -1)
            {
                children = SetAccessibleChildrenIndexAndGetChildren(parent._acc);

                // if unable to find this child (broken IAccessible impl?) bail (
                if (_accessibleChildrenIndex == -1)
                {
                    Debug.Assert(false);
                    return null;
                }
            }

            Accessible rval = null;
            if (_accessibleChildrenIndex - 1 >= 0)
            {
                rval = GetChildAt(parent._acc, children, _accessibleChildrenIndex - 1);
            }

            return rval;
        }

        internal Accessible Parent
        { 
            get 
            {
                // null parents happen! I have seen it with windowless media player controls
                // embedded in an IE web page, and with entries in Outlook 2003's SUPERGRID table view.
                // we have to ensure that we fail gracefully in that case.

                // review: I think it might be better to throw an exception here than return a bogus value.

                IAccessible rval;
                if (_idChild != NativeMethods.CHILD_SELF)
                {
                    rval = _acc; // parent is managing this child
                }
                else
                {
                    try
                    {
                        rval = (IAccessible)_acc.accParent;
                    }
                    catch (Exception e)
                    {
                        if (HandleIAccessibleException(e))
                        {
                            // PerSharp/PreFast will flag this as a warning, 6503/56503: Property get methods should not throw exceptions.
                            // We are communicate with the underlying control to get the information.  
                            // The control may not be able to give us the information we need.
                            // Throw the correct exception to communicate the failure.
#pragma warning suppress 6503
                            throw;
                        }
                        return null;
                    }
                }
                return Wrap(rval);
            }
        }

        internal int AccessibleChildrenIndex(Accessible parent)
        {
            // if this is the first time we are called then compute the value and cache it.
            if (_accessibleChildrenIndex < 0)
            {
                SetAccessibleChildrenIndexAndGetChildren(parent._acc);
            }
            return _accessibleChildrenIndex;
        }

        internal bool IsAvailableToUser
        {
            get
            {
                AccessibleState state = State;

                // From MSDN:
                // STATE_SYSTEM_INVISIBLE means the object is programmatically hidden. For example, menu items 
                // are programmatically hidden until a user activates the menu. Because objects with this 
                // state are not available to users, client applications should not communicate information 
                // about the object to users. However, if client applications find an object with this state, 
                // they should check to see if STATE_SYSTEM_OFFSCREEN is also set. If this second state is 
                // defined, then clients can communicate the information about the object to users.
                //
                // We're not dealing with menus [in this version of NativeMsaaProxy] so won't worry about them
                // here.  To "clean up" the tree we'll skip over IAccessibles that have the above states.  
                // May revisit per user feedback.
                if (Accessible.HasState(state, AccessibleState.Invisible) && !Accessible.HasState(state, AccessibleState.Offscreen))
                    return false;

                return true;
            }
        }

        internal bool InSameHwnd(IntPtr hwnd)
        {
            bool inSameHwnd = Window != IntPtr.Zero && Window == hwnd;
            return inSameHwnd;
        }

        // returns true if accessible object should be exposed to UIA clients.
        // accessible objects are exposed UIA if they are visible, not offscreen, and do not correspond to a child window.
        internal bool IsExposedToUIA
        {
            get
            {
                bool rval = false;
                // if the accessible object is "available"...
                if (IsAvailableToUser)
                {
                    // ... and is not a child window...
                    // This isn't sufficient to eliminate redundancies for Office. for example, the Word 2003 Font dialog
                    // has several child windows. However, the children in the IAccessible tree rooted at the dialog that correspond
                    // to the child windows don't have role == ROLE_SYSTEM_WINDOW. Instead they have ROLE_SYSTEM_TEXT etc. and are
                    // indistinguishable from windowless children that don't have a corresponding hwnd. Therefore those children are
                    // duplicated in the tree since the host hwnd provider is supplying children for the child hwndsand the native msaa
                    // provider is supplying children for the IAccessibles.
                    AccessibleRole role = Role;
                    if (role != AccessibleRole.Window)
                    {
                        // ... and is not a child window that is trident ...
                        // (special case since trident doesn't have a ROLE_SYSTEM_WINDOW object for its "Internet Explorer_Server" window.
                        if (role != AccessibleRole.Client || Description != "MSAAHTML Registered Handler")
                        {
                            // then it is visible 
                            rval = true;
                        }
                    }
                }
                return rval;
            }
        }

        internal AccessibleState State
        { 
            get 
            {
                try
                {
                    return (AccessibleState)_acc.get_accState(_idChild);
                }
                catch (Exception e)
                {
                    if (HandleIAccessibleException(e))
                    {
                        // PerSharp/PreFast will flag this as a warning, 6503/56503: Property get methods should not throw exceptions.
                        // We are communicate with the underlying control to get the information.  
                        // The control may not be able to give us the information we need.
                        // Throw the correct exception to communicate the failure.
#pragma warning suppress 6503
                        throw;
                    }
                    return AccessibleState.Unavailable;
                }
            } 
        }

        internal string Value
        {
            get
            {
                try
                {
                    string value = FixBstr(_acc.get_accValue(_idChild));
                    // PerSharp/PreFast will flag this as warning 6507/56507: Prefer 'string.IsNullOrEmpty(value)' over checks for null and/or emptiness.
                    // Need to convert nulls into an empty string, so need to just test for a null.
                    // Therefore we can not use IsNullOrEmpty() here, suppress the warning.
#pragma warning suppress 6507
                    return value != null ? value : "";
                }
                catch (Exception e)
                {
                    if (HandleIAccessibleException(e))
                    {
                        // PerSharp/PreFast will flag this as a warning, 6503/56503: Property get methods should not throw exceptions.
                        // We are communicate with the underlying control to get the information.  
                        // The control may not be able to give us the information we need.
                        // Throw the correct exception to communicate the failure.
#pragma warning suppress 6503
                        throw;
                    }
                    return "";
                }
            }
            set
            {
                try
                {
                    _acc.set_accValue(_idChild, value);
                }
                catch (Exception e)
                {
                    if (HandleIAccessibleException(e))
                    {
                        throw;
                    }

                    throw new InvalidOperationException(SR.Get(SRID.OperationCannotBePerformed), e);
                }
            }
        }

        // Return the Rect that bounds this element in screen coordinates
        internal Rect Location
        {
            get
            {
                NativeMethods.Win32Rect rcW32 = GetLocation(_acc, _idChild);
                return rcW32.ToRect(false);
            }
        }

        internal static Accessible GetFullAccessibleChildByIndex(Accessible accParent, int index)
        {
            int childCount = 0; 
            object[] accChildren = Accessible.GetAccessibleChildren(accParent.IAccessible, out childCount);

            if (accChildren != null && 0 <= index && index < accChildren.Length)
            {
                object child = accChildren[index];
                IAccessible accChild = child as IAccessible;
                if (accChild != null)
                {
                    return Accessible.Wrap(accChild);
                }
                else if (child is int)
                {
                    int idChild = (int)child;
                    return Accessible.Wrap(accParent.IAccessible, idChild);
                }
            }

            return null;
        }

        internal static AccessibleRole GetRole(IAccessible acc, int idChild)
        {
            AccessibleRole rval;
            try
            {
                object role = acc.get_accRole(idChild);

                // get_accRole can return a non-int! for example, Outlook 2003 SUPERGRID entries
                // can return the string "Table View". so we return an int if we got one otherwise
                // we convert it to the generic "client" role.
                rval = (role is int) ? (AccessibleRole)(int)role : AccessibleRole.Client;
            }
            catch (Exception e)
            {
                if (HandleIAccessibleException(e))
                {
                    throw;
                }
                rval = AccessibleRole.Client;
            }
            return rval;
        }

        // Get the selected children in a container
        internal Accessible [] GetSelection()
        {
            object obj = null;
            try
            {
                obj = _acc.accSelection;
            }
            catch (Exception e)
            {
                if (HandleIAccessibleException(e))
                {
                    throw;
                }
                obj = null;
            }

            if (obj == null)
            {
                return null;
            }

            Accessible [] children = null;
            if (obj is int)
            {
                children = new Accessible[1];
                children[0] = AccessibleFromObject(obj, _acc);
            }
            else if (obj is object)
            {
                children = new Accessible[1];
                children[0] = AccessibleFromObject(obj, _acc);
            }
            else if (obj is object [])
            {
                object [] objs = (object [])obj;
                children = new Accessible[objs.Length];
                for (int i=0;i<objs.Length;i++)
                {
                    children[i] = AccessibleFromObject(objs[i], _acc);
                }
            }

            return children;
        }

        // Get the focused Accessible
        internal Accessible GetFocus()
        {
            object scan;
            try
            {
                // Why does accFocus always return null? Is it because this
                // isn't the active window?
                scan = _acc.accFocus;
            }
            catch (Exception e)
            {
                // Narrator crashed with this exception and the call stack was pointing to this particular method.
                // Looks like some implementation of IAccessible->accFocus sometimes returns NullReferenceException. 
                // Until it can be identified/fixed, ignore this NullReferenceException and return gracefully.
                if (!(e is NullReferenceException) && HandleIAccessibleException(e))
                {
                    throw;
                }
                return null;
            }

            if (scan == null)
            {
                return this;
            }

            // Expect to get back either an IAccessible...
            if (scan is IAccessible)
            {
                return Wrap((IAccessible)scan);
            }
            // ... Or a ChildID (may be this element or a ChildID)...
            else if (scan is Int32)
            {
                int childId = (int)scan;
                if (childId == NativeMethods.CHILD_SELF)
                {
                    return this;
                }
                else
                {
                    return Wrap(_acc, childId);
                }
            }
            else
            {
                Debug.Assert( false, "Need to handle Accessible.accFocus case!" );
                return null;
            }
        }

        internal bool HasState(AccessibleState testState)
        {
            return HasState(State, testState);
        }

        internal static bool HasState(AccessibleState state, AccessibleState testState)
        {
            return Misc.IsBitSet((int)state, (int)testState);
        }

        // Return an elements help text
        internal string HelpText
        {
            get 
            {
                try
                {
                    return FixBstr(_acc.get_accHelp(_idChild));
                }
                catch (Exception e)
                {
                    if (HandleIAccessibleException(e))
                    {
                        // PerSharp/PreFast will flag this as a warning, 6503/56503: Property get methods should not throw exceptions.
                        // We are communicate with the underlying control to get the information.  
                        // The control may not be able to give us the information we need.
                        // Throw the correct exception to communicate the failure.
#pragma warning suppress 6503
                        throw;
                    }
                    return "";
                }
            }
        }

        // returns null if (x, y) are outside of object, 
        // or 'this' if (x, y) are on this object but not a child,
        // or a child if the point is on a child.
        internal Accessible HitTest(int x, int y)
        {
            // this shouldn't be called on "simple" objects.
            Debug.Assert(_idChild == NativeMethods.CHILD_SELF);

            object scan = null;
            try
            {
                scan = _acc.accHitTest(x, y);
            }
            catch (Exception e)
            {
                if (HandleIAccessibleException(e))
                {
                    throw;
                }
                scan = null;
            }

            Accessible rval;
            if (scan == null)
            {
                // point is not on this object or one of its children
                rval = null;
            }
            else if (scan is int)
            {
                // point is on child or self. If self then return 'this'
                int idChild = (int)scan;
                if (idChild == NativeMethods.CHILD_SELF)
                {
                    rval = this;
                }
                else
                {
                    // point is on a child object that has its own IAccessible.

                    // NOTE: we don't call IAccessible.get_accChild to see if idChild has it's own IAccessible.
                    // We assume that if the child has an IAccessible then the IAccessible will be returned.
                    // This is the same assumption that AccessibleObjectFromPoint makes.
                    rval = Wrap(_acc, idChild);
                }
            }
            else if (scan is IAccessible)
            {
                rval = Wrap((IAccessible)scan, NativeMethods.CHILD_SELF);
            }
            else
            {
                // if we get here, there is a problem here!!
                Debug.Assert(false);
                rval = null;
            }

            return rval;
        }

        // While accDoDefaultAction is supposed to return immediately, some implementations 
        // block the return. For example, if clicking a link displays a dialog, some 
        // implementations will block the return until the dialog is dismissed.
        internal void DoDefaultAction()
        {
            try
            {
                _acc.accDoDefaultAction(_idChild);
            }
            catch (Exception e)
            {
                if (HandleIAccessibleException(e))
                {
                    throw;
                }
            }
        }

        // Simulate moving the focus 
        internal void SetFocus()
        {
            Select(NativeMethods.SELFLAG_TAKEFOCUS);
        }

        // Click to select
        internal void SelectTakeFocusTakeSelection()
        {
            Select(NativeMethods.SELFLAG_TAKEFOCUS|NativeMethods.SELFLAG_TAKESELECTION);
        }

        // Ctrl + Click to add to selection
        internal void SelectTakeFocusAddToSelection()
        {
            // Do implementations check that the item is not already selected?
            if (! IsSelected)
                Select(NativeMethods.SELFLAG_TAKEFOCUS|NativeMethods.SELFLAG_ADDSELECTION);
        }

        // Ctrl + Click on an already selected item to remove from selection
        internal void SelectTakeFocusRemoveFromSelection()
        {
            // Do implementations already check that the item is selected?
            if (IsSelected)
                Select(NativeMethods.SELFLAG_TAKEFOCUS|NativeMethods.SELFLAG_REMOVESELECTION);
        }

        // compare 2 Accessible objects
        internal static bool Compare(Accessible acc1, Accessible acc2)
        {
            return acc1==acc2 || acc1.Compare(acc2._acc, acc2._idChild);
        }

        internal IntPtr Window
        {
            get
            {
                if (_hwnd == IntPtr.Zero)
                {
                    try
                    {
                        int result = UnsafeNativeMethods.WindowFromAccessibleObject(_acc, ref _hwnd);
                        if ( result != NativeMethods.S_OK)
                        {
                            _hwnd = IntPtr.Zero;
                        }
                    }
                    catch (Exception e)
                    {
                        if (HandleIAccessibleException(e))
                        {
                            // PerSharp/PreFast will flag this as a warning, 6503/56503: Property get methods should not throw exceptions.
                            // We are communicate with the underlying control to get the information.  
                            // The control may not be able to give us the information we need.
                            // Throw the correct exception to communicate the failure.
#pragma warning suppress 6503
                            throw;
                        }

                        _hwnd = IntPtr.Zero;
                    }
                }
                return _hwnd;
            }
        }

        #region IAccessible Utility Methods from ProxySimple

        // Overload for IAccessibles, much user friendly.
        internal static int AccessibleObjectFromWindow(IntPtr hwnd, int idObject, ref Accessible acc)
        {
            IAccessible accObject = null;
            acc = null;

            try
            {
                object obj = null;
                int hr = UnsafeNativeMethods.AccessibleObjectFromWindow(hwnd, idObject, ref UnsafeNativeMethods.IID_IUnknown, ref obj);

                accObject = obj as IAccessible;

                if (hr != NativeMethods.S_OK || accObject == null)
                {
                    return NativeMethods.S_FALSE;
                }

                acc = Accessible.Wrap(accObject);
                return hr;
            }
            catch (Exception e)
            {
                if (HandleIAccessibleException(e))
                {
                    throw;
                }

                return NativeMethods.S_FALSE;
            }
        }

        // Wrapper for AccessibleChildren API in oleacc.
        // Param "accessibleObject" is the IAccessible interface of the parent
        // Param "childrenReturned" is an out param for number of children returned
        // Returns an array of IAccessible interfaces and/or childID's representing the children
        internal static object[] GetAccessibleChildren(IAccessible accessibleObject, out int childrenReturned)
        {
            int childCount;
            object[] aChildren = null;

            try
            {
                childCount = accessibleObject.accChildCount;
                childrenReturned = 0;
                if (childCount > 0)
                {
                    aChildren = new object[childCount];

                    // Get the raw children because accNavigate doesn't work
                    if (UnsafeNativeMethods.AccessibleChildren(accessibleObject, 0, childCount, aChildren, out childrenReturned) == NativeMethods.E_INVALIDARG)
                    {
                        System.Diagnostics.Debug.Assert(false, "Call to AccessibleChildren() returned E_INVALIDARG.");
                        throw new ElementNotAvailableException();
                    }
                }

                return aChildren;
            }
            catch (Exception e)
            {
                if (HandleIAccessibleException(e))
                {
                    throw;
                }
                throw new ElementNotAvailableException();
            }
        }

        #endregion

        #endregion Internal Methods


        //------------------------------------------------------
        //
        //  Private Methods
        //
        //------------------------------------------------------
 
        #region Private Methods

        // Returns a child by index
        private static Accessible GetChildAt(IAccessible parent, object [] children, int index)
        {
            // note: calling AccessibleChildren on Trident and asking for a single child can
            // result in a Fatal Execution Engine Error (79697ADA)(80121506).
            // Perhaps the implementation ignores the cChildren param?
            // To avoid the problem we always ask for all of the children and just
            // use the specific one we are interested in.

            if (children == null)
            {
                children = GetChildren(parent);
            }
            if (children == null)
            {
                return null;
            }

            // Paranoia that between calls accChildCount returns different counts
            if (index >= children.Length)
                index = children.Length - 1; 

            Accessible nav = AccessibleFromObject(children[index], parent);
            if (nav != null)
            {
                nav._accessibleChildrenIndex = index;
            }

            return nav;
        }

        // Find _acc's index among its siblings with optimization to return children collection
        // if that would be needed later by the caller
        private object [] SetAccessibleChildrenIndexAndGetChildren(IAccessible parent)
        {
            // this is only called if the index hasn't been set yet.
            Debug.Assert(_accessibleChildrenIndex < 0);

            object [] children = GetChildren(parent);
            if (children == null)
            {
                return null;   // unlikely to happen but...
            }

            // Try to figure out which child in this array '_acc' is

            for (int i=0;i<children.Length;i++)
            {
                // First get the IAccessible and ChildID
                int idChild;
                IAccessible acc;
                IAccessibleFromObject(children[i], parent, out acc, out idChild);
                if (acc == null)
                    continue;   // unlikely to happen but...

                // If this child compares to the member _acc we've [probably] got its index
                if (Compare(acc, idChild))
                {
                    _accessibleChildrenIndex = i;
                    break;
                }
            }

            return children;
        }

        // Compare this Accessible to an IAccessible and return true if they match else false
        private bool Compare(IAccessible acc, int idChild)
        {
            // first try child id's...
            if (_idChild != idChild)
            {
                return false;
            }

            // then try location...
            NativeMethods.Win32Rect rect1 = GetLocation(_acc, _idChild);
            NativeMethods.Win32Rect rect2 = GetLocation(acc, idChild);
            if (rect1.left != rect2.left)
            {
                return false;
            }
            if (rect1.top != rect2.top)
            {
                return false;
            }
            if (rect1.right != rect2.right)
            {
                return false;
            }
            if (rect1.bottom != rect2.bottom)
            {
                return false;
            }

            // then try to match on Name...
            string name1 = Name;
            string name2 = GetName(acc, idChild);
            if (name1 != name2)
            {
                return false;
            }

            // last try Role...
            if (Role != GetRole(acc, idChild))
            {
                return false;
            }

            // need to handle the case rect1.Empty && name1 == "" && role1 == cell/table/client/etc...?
            return true;
        }

        // Return an Accessible wrapper for an object that may be a full IAccessible or a ChildID of 'parent'
        private static Accessible AccessibleFromObject(object o, IAccessible parent)
        {
            int idChild;
            IAccessible acc;

            IAccessibleFromObject(o, parent, out acc, out idChild);
            return Wrap(acc, idChild);
        }

        // Gets an IAccessible and idChild for an object that may be a full IAccessible or a ChildID of 'parent'
        private static void IAccessibleFromObject(object obj, IAccessible parent, out IAccessible acc, out int idChild)
        {
            idChild = 0;
            acc = obj as IAccessible;

            // first see if o is a full IAccessible object
            if (acc != null)
            {
                idChild = NativeMethods.CHILD_SELF;
            }
            else if (obj is int)
            {
                // call get_accChild to check if the object has its own IAccessible...
                object test = null;
                try
                {
                    test = parent.get_accChild((int)obj);
                }
                catch (Exception e)
                {
                    // Some impls of get_accChild return inappropriate error codes (eg. Trident
                    // used to return E_INVALIDCAST; others returned E_FAIL). To be more robust,
                    // do what the MSAA tools (eg. inspect etc) do and ignore the error, using
                    // the VT_I4 to access the child instead. If there really is an error, we'll
                    // hit it when we try to access the child itself.
                    // Some impls (MediaPlayer) return E_POINTER, which translates
                    // to NullReferenceException; ignore that also.
                    if (Misc.IsCriticalException(e) && ! (e is NullReferenceException))
                    {
                        throw;
                    }
                }

                if (test is IAccessible)
                {
                    acc = (IAccessible)test;
                    idChild = NativeMethods.CHILD_SELF;
                }
                else
                {
                    // it is a ChildID and parent is handling
                    acc = parent;
                    idChild = (int)obj;
                }
            }
        }

        private void Select(int selFlags)
        {
            try
            {
                _acc.accSelect(selFlags, _idChild);
            }
            catch (Exception e)
            {
                if (HandleIAccessibleException(e))
                {
                    throw;
                }
                throw new InvalidOperationException(SR.Get(SRID.OperationCannotBePerformed), e);
            }
        }

        private static object [] GetChildren(IAccessible parent)
        {
            // review: I think it might be better to throw an exception here than return a bogus value.

            // Get parent's child count
            int count = GetChildCount(parent);
            if (count <= 0)
            {
                return null;
            }

            // Create an array big enough to get all children of 'parent'
            object [] children = new object[count];

            int hr = 0;
            int actualCount;
            try
            {
                hr = UnsafeNativeMethods.AccessibleChildren(parent, 0, count, children, out actualCount);
            }
            catch (Exception e)
            {
                if (HandleIAccessibleException(e))
                {
                    throw;
                }

                // want to know when we get an exception we haven't seen before.
                Debug.WriteLine(string.Format(CultureInfo.CurrentCulture, "Unexpected exception thrown for AccessibleChildren: {0}", e));
                return null;
            }

            // if the MSAA call failed then throw a not available exception.
            if (hr != 0)
            {
                // want to know when we get an exception we haven't seen before.
                Debug.WriteLine(string.Format(CultureInfo.CurrentCulture, "Unexpected hresult from AccessibleChildren: {0} count is {1} and actualCount is {2}", hr, count, actualCount));
                return null;
            }

            return children;
        }

        private static int GetChildCount(IAccessible acc)
        {
            // review: I think it might be better to throw an exception here than return a bogus value.
            try
            {
                return acc.accChildCount;
            }
            catch (Exception e)
            {
                if (HandleIAccessibleException(e))
                {
                    throw;
                }
                return 0;
            }
        }

        private static string GetDescription(IAccessible acc, int idChild)
        {
            try
            {
                return FixBstr(acc.get_accDescription(idChild));
            }
            catch (Exception e)
            {
                if (HandleIAccessibleException(e))
                {
                    throw;
                }
                return "";
            }
        }

        private static string GetDefaultAction(IAccessible acc, int idChild)
        {
            try
            {
                return FixBstr(acc.get_accDefaultAction(idChild));
            }
            catch (Exception e)
            {
                if (HandleIAccessibleException(e))
                {
                    throw;
                }
                return "";
            }
        }

        private static string GetKeyboardShortcut(IAccessible acc, int idChild)
        {
            try
            {
                return FixBstr(acc.get_accKeyboardShortcut(idChild));
            }
            catch (Exception e)
            {
                if (HandleIAccessibleException(e))
                {
                    throw;
                }

                return "";
            }
        }

        private static string GetName(IAccessible acc, int idChild)
        {
            try
            {
                return FixBstr(acc.get_accName(idChild));
            }
            catch (Exception e)
            {
                if (HandleIAccessibleException(e))
                {
                    throw;
                }
                return "";
            }
        }

        internal static NativeMethods.Win32Rect GetLocation(IAccessible acc, int idChild)
        {
            // Should convert to use Rect since accLocation wants that and GetLoction gets casted to Rect
            NativeMethods.Win32Rect rect = NativeMethods.Win32Rect.Empty;
            try
            {
                acc.accLocation(out rect.left, out rect.top, out rect.right/*width*/, out rect.bottom/*height*/, idChild );
            }
            catch (Exception e)
            {
                // Some impls (eg Media Center) return odd error codes;
                // so treat all but critical ones as a 'soft' failure (return empty rect)
                // Some impls (MediaPlayer) return E_POINTER, which translates
                // to NullReferenceException; ignore that also.
                if (Misc.IsCriticalException(e) && ! (e is NullReferenceException))
                {
                    throw;
                }
                return NativeMethods.Win32Rect.Empty;
            }

            rect.right += rect.left;    // convert width to right
            rect.bottom += rect.top;    // convert height to bottom
            return rect;
        }
        
        // converts the exception into a more appropriate one and throws it,
        // or returns false indicating the caller should assume a default result
        // or returns true indicating the caller should rethrow the exception.
        private static bool HandleIAccessibleException(Exception e)
        {
            if (e is OutOfMemoryException)
            {
                // Some OLEACC proxies produce out-of-memory for non-critical reasons:
                // notably, the treeview proxy will raise this if the target HWND no longer exists,
                // GetWindowThreadProcessID fails and it therefore won't be able to allocate shared
                // memory in the target process, so it incorrectly assumes OOM.
                // (Need to check this before Misc.IsCriticalException, since it includes OOM as critical.)
                throw new ElementNotAvailableException(e);
            }

            if (e is NullReferenceException)
            {
                // Media Player and some other badly-implemented IAccessibles can return the correponding 
                // COM error code (E_POINTER).  This does not actually indicate a null dereference in this 
                // process.
                throw new ElementNotAvailableException(e);
            }

            if (Misc.IsCriticalException(e))
            {
                return true;
            }

            COMException comException = e as COMException;

            if (e is NotImplementedException)
            {
                // just return on E_NOTIMPL errors
                return false;
            }
            else if (comException != null)
            {
                // convert certain COM exceptions to ElementNotAvailable exceptions.
                // these occur when the underlying UI elements disappear but we are still
                // holding pointers to them, like when Trident navigates to a new page.
                int errorCode = comException.ErrorCode;

                switch (errorCode)
                {
                    case NativeMethods.RPC_E_SERVERFAULT: // The server threw an exception.
                    case NativeMethods.RPC_E_DISCONNECTED: // The object invoked has disconnected from its clients.
                    case NativeMethods.RPC_E_UNAVAILABLE: // The server has disappeared
                    case NativeMethods.DISP_E_BADINDEX: // Index out of Range (Usually means Children have disappeared)
                    case NativeMethods.E_INTERFACEUNKNOWN: // The interface is unknown, usually because things have changed.
                    case NativeMethods.E_UNKNOWNWORDERROR: // An unknown Error code thrown by Word being closed while a search is running
                    case NativeMethods.RPC_E_SYS_CALL_FAILED: // System call failed during RPC.
                        throw new ElementNotAvailableException(e);

                    case NativeMethods.E_FAIL:
                        // An unknown or generic error occurred; treat as a not-impl. (Other methods on the object
                        // may still work, so don't treat as ElementNotAvailable.)
                    case NativeMethods.E_MEMBERNOTFOUND:
                        // The object does not support the requested property or action. For example,
                        // a push button returns this value if you request its Value property, since
                        // it does not have a Value property.
                    case NativeMethods.E_NOTIMPL:
                        // just return on E_NOTIMPL errors
                        return false;

                    case NativeMethods.E_OUTOFMEMORY:
                        // Some OLEACC proxies produce out-of-memory for non-critical reasons:
                        // notably, the treeview proxy will raise this if the target HWND no longer exists,
                        // GetWindowThreadProcessID fails and it therefore won't be able to allocate shared
                        // memory in the target process, so it incorrectly assumes OOM.
                        throw new ElementNotAvailableException(e);
                        
                    case NativeMethods.E_INVALIDARG:
                        // One or more arguments were invalid. This error occurs when the caller attempts to identify
                        // a child object using an identifier that the server does not recognize. This error also results
                        // when a client attempts to identify a child object within an object that has no children.
                        throw new ArgumentException(SR.Get(SRID.InvalidParameter));

                    case NativeMethods.E_ACCESSDENIED:
                        // This is returned when you call get_accValue to get the value of a password control.
                        throw new UnauthorizedAccessException();

                    case NativeMethods.E_UNEXPECTED:
                        // An IAccessible server has been released unexpectedly but still has pending events.
                        // If the current execution context is inside one of these event handlers it must be 
                        // abandoned.
                        throw new ElementNotAvailableException(e);

                    default:
                        // we want to know when we get an exception we haven't seen before
                        Debug.Assert(false, string.Format(CultureInfo.CurrentCulture, "MsaaNativeProvider: IAccessible threw a COMException: {0}", e.Message));
                        break;
                }
            }
            else if (e is InvalidCastException)
            {
                // sometimes Trident throws InvalidCastExceptions on elements from obsolete pages
                throw new ElementNotAvailableException(e);
            }
            else if (e is BadImageFormatException)
            {
                // This control/window mostly has went away or is in the process of shutting down.
                throw new ElementNotAvailableException(e);
            }
            else
            {
                // we want to know when we get an exception we haven't seen before
                Debug.Assert(false, string.Format(CultureInfo.CurrentCulture, "Unexpected IAccessible exception: {0}", e));
            }

            // rethrow the exception
            return true;
        }

        // IAccessibles that we get from Winforms apps in partial trust return failure
        // code for some methods - notably accNavigate and accChild. The operation will
        // succeed, however, if we first navigate up to the parent, and then back down
        // to the 'original' IAccessible - this navigation step seems to add or remove some
        // wrapper object in the winforms impl that is otherwise blocking these operations.
        //
        // Since modifying the Winforms code to fix it isn't a viable option (it's already
        // released, so we need a way to work with the existing code anyway; and this error
        // appears to be a side-effect of other security-related code in the Winforms impl
        // which Winforms really do not want to modify), we use this workaround.
        static IAccessible WashPartialTrustWinformsAccessible(IAccessible old)
        {
            // Basic alg: get the parent, get all its children, then check each
            // one looking for the one that corresponds to the same element as the
            // start IAccessible. (Use Role/Location to do this check.)
            NativeMethods.Win32Rect ownLoc = GetLocation(old, NativeMethods.CHILD_SELF);
            AccessibleRole ownRole = GetRole(old, NativeMethods.CHILD_SELF);

            IAccessible accParent = (IAccessible)old.accParent;
           

            int childCount;
            object[] rawChildren = Accessible.GetAccessibleChildren(accParent, out childCount);

            for (int i = 0; i < childCount; i++)
            {
                // Only looking for full IAccessible children here - so skip idChild entries....
                IAccessible accChild = rawChildren[i] as IAccessible;
                if( accChild == null )
                    continue;

                // Use Role+Location to compare the child IAccessible with the original one...
                AccessibleRole role = GetRole(accChild, NativeMethods.CHILD_SELF);
                if (role != ownRole)
                    continue;

                NativeMethods.Win32Rect loc = GetLocation(accChild, NativeMethods.CHILD_SELF);
                if( loc.left != ownLoc.left
                 || loc.top != ownLoc.top
                 || loc.right != ownLoc.right
                 || loc.bottom != ownLoc.bottom)
                    continue;

                return accChild;
            }
            return null;
        }

        // Some MSAA imples return BSTRs that contain embedded NULs. Unmanaged code (eg inspect etc) stops
        // at the first null, but managed strings allow embedded NULs, so use the whole string. This causes
        // problems, since (1) the remainder of the string after the NUL is likely garbage, and (2) if the
        // string is appended to and then passed to unmanaged code at some later stage, the unmanaged code
        // will stop at the first NUL, and miss the remainder of the string.
        // This code fixes the issue by truncating the string at the first NUL.
        private static string FixBstr(string bstr)
        {
            if (bstr == null)
                return null;
            int nulIndex = bstr.IndexOf('\0');
            if (nulIndex == -1)
                return bstr;
            return bstr.Substring(0, nulIndex);
        }
        #endregion Private Methods

        //------------------------------------------------------
        //
        //  Private Fields
        //
        //------------------------------------------------------
 
        #region Private Fields

        private enum NavDir
        {
            FirstChild,
            NextSibling,
            PrevSibling,
            LastChild,
            Parent
        }

        private IAccessible _acc;   // a full IAccessible object or an IAccessible parent that is managing a ChildID
        private int _idChild;       // this is ChildID which is the ID a server gives this child (not related to child order!)
        private int _accessibleChildrenIndex;    // this is how many children to skip over when calling AccessibleChildren

        private IntPtr _hwnd;

        #endregion Private Fields
    }
}
