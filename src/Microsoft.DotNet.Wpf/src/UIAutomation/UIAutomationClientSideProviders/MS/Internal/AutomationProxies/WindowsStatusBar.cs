// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// Description: Windows Status Proxy

using System;
using System.Collections;
using System.Text;
using System.Windows.Automation;
using System.Windows.Automation.Provider;
using System.ComponentModel;
using System.Windows;
using System.Globalization;
using MS.Win32;

namespace MS.Internal.AutomationProxies
{
    class WindowsStatusBar : ProxyHwnd, IGridProvider, IRawElementProviderHwndOverride
    {
        // ------------------------------------------------------
        //
        // Constructors
        //
        // ------------------------------------------------------

        #region Constructors

        internal WindowsStatusBar(IntPtr hwnd, ProxyFragment parent, int item, Accessible acc)
            : base(hwnd, parent, item)
        {
            _acc = acc;

            _cControlType = ControlType.StatusBar;

            _fHasGrip = StatusBarGrip.HasGrip(hwnd);

            _sAutomationId = "StatusBar"; // This string is a non-localizable string

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

        private static IRawElementProviderSimple Create(IntPtr hwnd, int idChild)
        {
            // Something is wrong if idChild is not zero 
            if (idChild != 0)
            {
                System.Diagnostics.Debug.Assert (idChild == 0, "Invalid Child Id, idChild != 0");
                throw new ArgumentOutOfRangeException("idChild", idChild, SR.Get(SRID.ShouldBeZero));
            }

            return new WindowsStatusBar(hwnd, null, idChild, null);
        }

        // Static Create method called by the event tracker system
        internal static void RaiseEvents (IntPtr hwnd, int eventId, object idProp, int idObject, int idChild)
        {
            if (idObject != NativeMethods.OBJID_VSCROLL && idObject != NativeMethods.OBJID_HSCROLL)
            {
                bool isWinforms = WindowsFormsHelper.IsWindowsFormsControl(hwnd);
                ProxySimple el = isWinforms ? (ProxySimple)WindowsFormsHelper.Create(hwnd, 0, idObject) : (ProxySimple)Create(hwnd, 0);
                if (el == null)
                {
                    // WindowsFormsHelper may return null if the MSAA Role for this hwnd isn't handled
                    return;
                }

                if (idChild > 0)
                {
                    if (eventId == NativeMethods.EventObjectNameChange && idChild == 1)
                    {
                        // Need to let the overall control process this event also.
                        el.DispatchEvents(eventId, idProp, idObject, idChild);
                    }

                    el = ((WindowsStatusBar)el).CreateStatusBarPane(idChild - 1);
                }

                el.DispatchEvents(eventId, idProp, idObject, idChild);
            }
        }

        internal ProxySimple CreateStatusBarPane (int index)
        {
            // Use the Accessible object if this is a Winforms control.  Only Winforms StatusBars
            // can have children.
            Accessible accChild = null;
            if (_acc != null)
            {
                // OLEACC's Win32 proxy does use a 1, 2, 3... scheme, but the Winforms
                // controls in some cases supply their own children, using a different scheme.
                // Using the "ByIndex" approach avoids having to know what the underlying
                // object's idChild scheme is.
                accChild = Accessible.GetFullAccessibleChildByIndex(_acc, index);
                if (accChild != null && accChild.Role != AccessibleRole.PushButton)
                {
                    // WinForms toolbars have full IAccessibles for their children, but 
                    // return the overall hwnd; treat those same as regular items.
                    // We only want to special-case actual child hwnds for overriding.
                    IntPtr hwndChild = accChild.Window;
                    if (hwndChild == IntPtr.Zero || hwndChild == _hwnd)
                    {
                        hwndChild = GetChildHwnd(_hwnd, accChild.Location);
                    }

                    if(hwndChild != IntPtr.Zero && hwndChild != _hwnd)
                    {
                        // We have an actual child hwnd.
                        return new WindowsStatusBarPaneChildOverrideProxy(hwndChild, this, index);
                    }
                }
            }
            return new WindowsStatusBarPane(_hwnd, this, index, accChild);
        }

        #endregion Proxy Create

        // ------------------------------------------------------
        //
        // Patterns Implementation
        //
        // ------------------------------------------------------

        #region ProxySimple Interface

        // Returns a pattern interface if supported.
        internal override object GetPatternProvider (AutomationPattern iid)
        {
            return iid == GridPattern.Pattern ? this : null;
        }

        //Gets the localized name
        internal override string LocalizedName
        {
            get
            {
                return Misc.ProxyGetText(_hwnd);
            }
        }

        #endregion ElementProvider

        #region ProxyFragment Interface

        // Returns the next sibling element in the raw hierarchy.
        // Peripheral controls have always negative values.
        // Returns null if no next child
        internal override ProxySimple GetNextSibling (ProxySimple child)
        {
            int item = child._item;
            int count = Count;

            // Next for an item that does not exist in the list
            if (item >= count)
            {
                throw new ElementNotAvailableException ();
            }

            // The grip is the last item. Exit when we see it.
            if (item == GripItemID)
            {
                return null;
            }

            // Eventually add the Grip as the last element in the list
            return item + 1 < count ? CreateStatusBarPane(item + 1) : (_fHasGrip ? StatusBarGrip.Create(_hwnd, this, -1) : null);
        }

        // Returns the previous sibling element in the raw hierarchy.
        // Peripheral controls have always negative values.
        // Returns null is no previous
        internal override ProxySimple GetPreviousSibling (ProxySimple child)
        {
            int item = child._item;
            int count = Count;

            // Next for an item that does not exist in the list
            if (item >= count)
            {
                throw new ElementNotAvailableException ();
            }

            // Grip is the last in the list but has a negative number
            // Fake a new item number that is last in the list
            if (item == GripItemID)
            {
                item = count;
            }

            return item > 0 && (item - 1) < Count ? CreateStatusBarPane (item - 1) : null;
        }

        // Returns the first child element in the raw hierarchy.
        internal override ProxySimple GetFirstChild ()
        {
            // Grip is the last Element
            return Count > 0 ? CreateStatusBarPane(0) : (_fHasGrip ? StatusBarGrip.Create(_hwnd, this, GripItemID) : null);
        }

        // Returns the last child element in the raw hierarchy.
        internal override ProxySimple GetLastChild ()
        {
            // Grip is the last Element
            if (_fHasGrip)
            {
                return StatusBarGrip.Create(_hwnd, this, GripItemID);
            }

            int count = Count;
            return count > 0 ? CreateStatusBarPane (count - 1): null;
        }

        // Returns a Proxy element corresponding to the specified screen coordinates.
        internal override ProxySimple ElementProviderFromPoint (int x, int y)
        {
            // Loop through all the panes
            for (int item = 0, count = Count; item < count; item++)
            {
                NativeMethods.Win32Rect rc = new NativeMethods.Win32Rect (WindowsStatusBarPane.GetBoundingRectangle (_hwnd, item));

                if (Misc.PtInRect(ref rc, x, y))
                {
                    return CreateStatusBarPane(item);
                }
            }

            // Try the Grip
            if (_fHasGrip)
            {
                NativeMethods.Win32Rect rc = StatusBarGrip.GetBoundingRectangle (_hwnd);
                if (Misc.PtInRect(ref rc, x, y))
                {
                    ProxySimple grip = StatusBarGrip.Create(_hwnd, this, -1);
                    return (ProxySimple)(grip != null ? grip : this);
                }
            }
            return this;
        }

        #endregion

        #region Grid Pattern

        // Obtain the AutomationElement at an zero based absolute position in the grid.
        // Where 0,0 is top left
        IRawElementProviderSimple IGridProvider.GetItem(int row, int column)
        {
            // NOTE: Status bar has only 1 row
            if (row != 0)
            {
                throw new ArgumentOutOfRangeException("row", row, SR.Get(SRID.GridRowOutOfRange));
            }

            if (column < 0 || column >= Count)
            {
                throw new ArgumentOutOfRangeException("column", column, SR.Get(SRID.GridColumnOutOfRange));
            }

            return CreateStatusBarPane(column);
        }

        int IGridProvider.RowCount
        {
            get
            {
                return 1;
            }
        }

        int IGridProvider.ColumnCount
        {
            get
            {
                return Count;
            }
        }

        #endregion Grid Pattern

        #region IRawElementProviderHwndOverride Interface

        //------------------------------------------------------
        //
        //  Interface IRawElementProviderHwndOverride
        //
        //------------------------------------------------------
        IRawElementProviderSimple IRawElementProviderHwndOverride.GetOverrideProviderForHwnd(IntPtr hwnd)
        {
            // return the appropriate placeholder for the given hwnd...
            // loop over all the band to find it.

            // Only Winforms StatusBars can have children.
            if (_acc != null)
            {
                Accessible accChild = _acc.FirstChild;
                IntPtr hwndChild = IntPtr.Zero;

                for (int i = 0; accChild != null; i++, accChild = accChild.NextSibling(_acc))
                {
                    hwndChild = accChild.Window;
                    if (hwndChild == IntPtr.Zero)
                    {
                        hwndChild = GetChildHwnd(_hwnd, accChild.Location);
                    }
                    if (hwndChild == hwnd)
                    {
                        return new WindowsStatusBarPaneChildOverrideProxy(hwnd, this, i);
                    }
                }
            }

            return null;
        }

        #endregion IRawElementProviderHwndOverride Interface

        // ------------------------------------------------------
        //
        // Private Methods
        //
        // ------------------------------------------------------        

        #region Private Methods

        // Returns the number of Status Bar Panes
        private int Count
        {
            get
            {
                if (_acc == null)
                {
                    return Misc.ProxySendMessageInt(_hwnd, NativeMethods.SB_GETPARTS, IntPtr.Zero, IntPtr.Zero);
                }
                else
                {
                    return _acc.ChildCount;
                }
            }
        }

        unsafe static private IntPtr GetChildHwnd(IntPtr hwnd, Rect rc)
        {
            UnsafeNativeMethods.ENUMCHILDWINDOWFROMRECT info = new UnsafeNativeMethods.ENUMCHILDWINDOWFROMRECT();

            info.hwnd = IntPtr.Zero;
            info.rc.left = (int)rc.Left;
            info.rc.top = (int)rc.Top;
            info.rc.right = (int)rc.Right;
            info.rc.bottom = (int)rc.Bottom;

            Misc.EnumChildWindows(hwnd, new NativeMethods.EnumChildrenCallbackVoid(FindChildFromRect), (void*)&info);

            return info.hwnd;
        }

        unsafe static private bool FindChildFromRect(IntPtr hwnd, void* lParam)
        {
            NativeMethods.Win32Rect rc = NativeMethods.Win32Rect.Empty;
            if (!Misc.GetClientRectInScreenCoordinates(hwnd, ref rc))
            {
                return true;
            }

            UnsafeNativeMethods.ENUMCHILDWINDOWFROMRECT * info = (UnsafeNativeMethods.ENUMCHILDWINDOWFROMRECT *)lParam;

            if (rc.left == info->rc.left && rc.top == info->rc.top && rc.right == info->rc.right && rc.bottom == info->rc.bottom)
            {
                info->hwnd = hwnd;
                return false;
            }

            return true;
        }

        #endregion

        // ------------------------------------------------------
        //
        // Private Fields
        //
        // ------------------------------------------------------

        #region Private Fields

        // Status bar with a Grip Style
        private bool _fHasGrip;
        private Accessible _acc;   // Accessible is used for WinForms controls.

        // Item ID for the grip. Must be negative as it is a peripheral element
        private const int GripItemID = -1;

        private const int SBARS_SIZEGRIP = 0x0100;

        #endregion

        // ------------------------------------------------------
        //
        // WindowsStatusBarPane Private Class
        //
        // ------------------------------------------------------

        #region WindowsStatusBarPane 

        class WindowsStatusBarPane : ProxySimple, IGridItemProvider, IValueProvider
        {

            // ------------------------------------------------------
            //
            //  Constructors
            //
            //------------------------------------------------------

            #region Constructors

            internal WindowsStatusBarPane (IntPtr hwnd, ProxyFragment parent, int item, Accessible acc)
            : base (hwnd, parent, item)
            {
                _acc = acc;

                _cControlType = ControlType.Edit;
                _sAutomationId = "StatusBar.Pane" + item.ToString(CultureInfo.CurrentCulture); // This string is a non-localizable string
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
                if (iid == GridItemPattern.Pattern)
                {
                    return this;
                }
                else if (iid == ValuePattern.Pattern)
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
                    return GetBoundingRectangle (_hwnd, _item);
                }
            }

            // Gets the localized name
            internal override string LocalizedName
            {
                get
                {
                    return Text;
                }
            }

            #endregion ProxySimple Interface

            #region Grid Pattern

            int IGridItemProvider.Row
            {
                get
                {
                    return 0;
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
                    return _parent;
                }
            }

            #endregion Grid Pattern

            #region Value Pattern

            // Sets the text of the edit.
            void IValueProvider.SetValue(string str)
            {
                // This is a read only element.
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

            // Read only status
            bool IValueProvider.IsReadOnly
            {
                get
                {
                    return true;
                }
            }

            #endregion

            //------------------------------------------------------
            //
            //  Internal Methods
            //
            //------------------------------------------------------

            #region Internal Methods

            // Retrieves the bounding rectangle of the Status Bar Pane.
            static internal Rect GetBoundingRectangle (IntPtr hwnd, int item)
            {
                if( !WindowsFormsHelper.IsWindowsFormsControl(hwnd))
                {
                    return XSendMessage.GetItemRect(hwnd, NativeMethods.SB_GETRECT, item);
                }
                else
                {
                    Accessible acc = null;
                    if (Accessible.AccessibleObjectFromWindow(hwnd, NativeMethods.OBJID_CLIENT, ref acc) != NativeMethods.S_OK || acc == null)
                    {
                        return Rect.Empty;
                    }
                    else
                    {
                        // OLEACC's Win32 proxy does use a 1, 2, 3... scheme, but the Winforms
                        // controls in some cases supply their own children, using a different scheme.
                        // Using the "ByIndex" approach avoids having to know what the underlying
                        // object's idChild scheme is.
                        acc = Accessible.GetFullAccessibleChildByIndex(acc, item);
                        if (acc == null)
                        {
                            return Rect.Empty;
                        }
                        else
                        {
                            return acc.Location;
                        }
                    }
                }
            }

            //Gets the localized name
            internal string Text
            {
                get
                {
                    if (_acc == null)
                    {
                        // Get the length of the string
                        int retValue = Misc.ProxySendMessageInt(_hwnd, NativeMethods.SB_GETTEXTLENGTHW, new IntPtr(_item), IntPtr.Zero);

                        // The low word specifies the length, in characters, of the text.
                        // The high word specifies the type of operation used to draw the text.
                        int len = NativeMethods.Util.LOWORD(retValue);
                        return XSendMessage.GetItemText(_hwnd, NativeMethods.SB_GETTEXTW, _item, len);
                    }
                    else
                    {
                        return _acc.Name;
                    }
                }
            }

            #endregion

            // ------------------------------------------------------
            //
            // Private Fields
            //
            // ------------------------------------------------------

            #region Private Fields

            private Accessible _acc; // Accessible is used for WinForms controls.

            #endregion

        }
        #endregion

        // ------------------------------------------------------
        //
        //  WindowsStatusBarPaneChildOverrideProxy Private Class
        //
        //------------------------------------------------------

        #region WindowsStatusBarPaneChildOverrideProxy

        class WindowsStatusBarPaneChildOverrideProxy : ProxyHwnd, IGridItemProvider
        {
            // ------------------------------------------------------
            //
            // Constructors
            //
            // ------------------------------------------------------

            #region Constructors

            internal WindowsStatusBarPaneChildOverrideProxy(IntPtr hwnd, ProxyFragment parent, int item)
                : base(hwnd, parent, item)
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

            // Returns a pattern interface if supported.
            internal override object GetPatternProvider(AutomationPattern iid)
            {
                if (iid == GridItemPattern.Pattern)
                {
                    return this;
                }
                return null;
            }

            internal override object GetElementProperty(AutomationProperty idProp)
            {
                // No property should be handled by the override proxy
                // Overrides the ProxySimple implementation.
                return null;
            }

            #endregion

            #region Grid Pattern

            int IGridItemProvider.Row
            {
                get
                {
                    return 0;
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
                    return _parent;
                }
            }

            #endregion Grid Pattern
        }

        #endregion
        // ------------------------------------------------------
        //
        // StatusBarGrip Private Class
        //
        // ------------------------------------------------------

        #region StatusBarGrip

        class StatusBarGrip: ProxyFragment
        {
            // ------------------------------------------------------
            //
            //  Constructors
            //
            //------------------------------------------------------

            #region Constructors

            public StatusBarGrip (IntPtr hwnd, ProxyHwnd parent, int item)
                : base( hwnd, parent, item)
            {
                _sType = SR.Get(SRID.LocalizedControlTypeGrip);
                _sAutomationId = "StatusBar.Grip"; // This string is a non-localizable string
            }

            #endregion

            //------------------------------------------------------
            //
            //  Patterns Implementation
            //
            //------------------------------------------------------

            #region ProxySimple Interface

            // Gets the bounding rectangle for this element
            internal override Rect BoundingRectangle
            {
                get
                {
                    // Don't need to normalize, GetBoundingRectangle returns absolute coordinates.
                    return GetBoundingRectangle(_hwnd).ToRect(false);
                }
            }

            #endregion

            //------------------------------------------------------
            //
            //  Internal Methods
            //
            //------------------------------------------------------

            #region Internal Methods

            // Gets the bounding rectangle for this element
            internal static NativeMethods.Win32Rect GetBoundingRectangle (IntPtr hwnd)
            {
                if (!HasGrip(hwnd))
                {
                    return NativeMethods.Win32Rect.Empty;
                }

                NativeMethods.Win32Rect client = new NativeMethods.Win32Rect();
                if (!Misc.GetClientRectInScreenCoordinates(hwnd, ref client))
                {
                    return NativeMethods.Win32Rect.Empty;
                }

                // Get the Size for the Gripper
                // The size can change at any time so the value cannot be cached
                NativeMethods.SIZE sizeGrip = WindowsGrip.GetGripSize(hwnd, true);

                if (Misc.IsLayoutRTL(hwnd))
                {
                    // Right to left mirroring style
                    return new NativeMethods.Win32Rect(client.left, client.bottom - sizeGrip.cy, client.left + sizeGrip.cx, client.bottom);
                }
                else
                {
                    return new NativeMethods.Win32Rect(client.right - sizeGrip.cx, client.bottom - sizeGrip.cy, client.right, client.bottom);
                }
            }

            internal static StatusBarGrip Create(IntPtr hwnd, ProxyHwnd parent, int item)
            {
                if(HasGrip(hwnd))
                {
                    return new StatusBarGrip(hwnd, parent, item);
                }

                return null;
            }

            #endregion

            #region Private Methods
            internal static bool HasGrip(IntPtr hwnd)
            {
                int style = Misc.GetWindowStyle(hwnd);
                return Misc.IsBitSet(style, SBARS_SIZEGRIP) || WindowsGrip.IsGripPresent(hwnd, true);
            }
            #endregion
        }

        #endregion
    }
}
