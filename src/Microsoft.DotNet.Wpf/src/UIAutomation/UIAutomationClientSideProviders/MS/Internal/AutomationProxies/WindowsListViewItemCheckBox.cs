// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
//
//
// Description: Win32 ListViewItemCheckbox proxy
//


using System;
using System.ComponentModel;
using System.Windows.Automation;
using System.Windows.Automation.Provider;
using System.Windows;
using System.Runtime.InteropServices;
using MS.Win32;

namespace MS.Internal.AutomationProxies
{
    class ListViewItemCheckbox: ProxySimple, IToggleProvider
    {
        // ------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------

        #region Constructors

        internal ListViewItemCheckbox (IntPtr hwnd, ProxyFragment parent, int item, int checkbox) :
            base (hwnd, parent, checkbox)
        {
            _cControlType = ControlType.CheckBox;
            _listviewItem = item;
        }

        #endregion Constructors

        //------------------------------------------------------
        //
        //  Patterns Implementation
        //
        //------------------------------------------------------

        #region ProxySimple Interface

        // Returns a pattern interface if supported.
        internal override object GetPatternProvider (AutomationPattern iid)
        {
            if (iid == TogglePattern.Pattern)
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
                // Don't need to normalize, ListViewCheckBoxRect returns absolute coordinates.
                return ListViewCheckBoxRect(_hwnd, _listviewItem).ToRect(false);
            }
        }

        // Process all the Element Properties
        internal override object GetElementProperty(AutomationProperty idProp)
        {
            if (idProp == AutomationElement.IsOffscreenProperty)
            {
                Rect parentRect = GetParent().BoundingRectangle;
                NativeMethods.Win32Rect itemRect = ListViewCheckBoxRect(_hwnd, _listviewItem);
                if (itemRect.IsEmpty || parentRect.IsEmpty)
                {
                    return true;
                }

                if (Misc.MapWindowPoints(_hwnd, IntPtr.Zero, ref itemRect, 2) && !Misc.IsItemVisible(ref parentRect, ref itemRect))
                {
                    return true;
                }
            }
            // EventManager.DispatchEvent() genericaly uses GetElementProperty()
            // to get properties during a property change event.  Proccess ToggleStateProperty
            // so the ToggleStateProperty Change Event can get the correct state.
            else if (idProp == TogglePattern.ToggleStateProperty)
            {
                return ((IToggleProvider)this).ToggleState;
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
                string name = ListViewItem.GetText(_hwnd, _listviewItem, 0);
                return name.Length < Misc.MaxLengthNameProperty ? name : name.Substring(0, Misc.MaxLengthNameProperty);

            }
        }

        #endregion ProxySimple Interface

        #region IToggleProvider

        void IToggleProvider.Toggle()
        {
            // check or uncheck the checkbox
            Toggle();
        }

        ToggleState IToggleProvider.ToggleState
        {
            get
            {
                return GetToggleState();
            }
        }

        #endregion IToggleProvider

        //------------------------------------------------------
        //
        //  Internal Methods
        //
        //------------------------------------------------------

        #region Internal Methods

        // retrieve bounding rectangle of the listview checkbox
        internal static NativeMethods.Win32Rect ListViewCheckBoxRect (IntPtr hwnd, int item)
        {
            //  Rare special case
            if (WindowsListView.FullRowSelect (hwnd) && WindowsListView.IsDetailMode (hwnd))
            {
                // Get listview window rect
                NativeMethods.Win32Rect controlRectangle = NativeMethods.Win32Rect.Empty;

                if (!Misc.GetWindowRect(hwnd, ref controlRectangle))
                {
                    return NativeMethods.Win32Rect.Empty;
                }

                // BOUNDS == SELECTBOUNDS, hence cannot rely on them
                // will rely on the ICON or LABEL
                // Try icon first since it is the closest to the checkbox
                NativeMethods.Win32Rect rc;

                if ((WindowsListView.GetItemRect(hwnd, item, NativeMethods.LVIR_ICON, out rc) && rc.left != rc.right) || (WindowsListView.GetItemRect(hwnd, item, NativeMethods.LVIR_LABEL, out rc) && rc.left != rc.right))
                {
                    int right = controlRectangle.left + (rc.left - controlRectangle.left);

                    return new NativeMethods.Win32Rect (controlRectangle.left, rc.top, right, rc.bottom);
                }
            }
            else
            {
                // Very common, simple case
                NativeMethods.Win32Rect wholeItem;

                if (!WindowsListView.GetItemRect(hwnd, item, NativeMethods.LVIR_BOUNDS, out wholeItem))
                {
                    return NativeMethods.Win32Rect.Empty;
                }

                NativeMethods.Win32Rect selectable;

                if (!WindowsListView.GetItemRect(hwnd, item, NativeMethods.LVIR_SELECTBOUNDS, out selectable))
                {
                    return NativeMethods.Win32Rect.Empty;
                }

                if(Misc.IsControlRTL(hwnd))
                {
                    return new NativeMethods.Win32Rect (selectable.right, wholeItem.top, wholeItem.right, wholeItem.bottom);
                }
                else
                {
                    return new NativeMethods.Win32Rect (wholeItem.left, wholeItem.top, selectable.left, wholeItem.bottom);
                }
            }

            return NativeMethods.Win32Rect.Empty;
        }
        
        #endregion Internal Methods
                
        //------------------------------------------------------
        //
        //  Private Methods
        //
        //------------------------------------------------------

        #region Private Methods
        
        // retrieve current ToggleState
        private ToggleState GetToggleState ()
        {
            ListViewItem.CheckState current = (ListViewItem.CheckState) WindowsListView.GetCheckedState (_hwnd, _listviewItem);

            switch (current)
            {
                case ListViewItem.CheckState.NoCheckbox :
                    {
                        throw new InvalidOperationException(SR.Get(SRID.OperationCannotBePerformed));
                    }

                case ListViewItem.CheckState.Checked :
                    {
                        return ToggleState.On;
                    }

                case ListViewItem.CheckState.Unchecked :
                    {
                        return ToggleState.Off;
                    }
            }

            // developer defined custom values which cannot be interpret outside of the app's scope
            return ToggleState.Indeterminate;
        }
        
        private void Toggle()
        {
            // Make sure that the control is enabled
            if (!SafeNativeMethods.IsWindowEnabled(_hwnd))
            {
                throw new ElementNotEnabledException();
            }

            Misc.SetFocus(_hwnd);

            NativeMethods.Win32Rect rc = ListViewCheckBoxRect(_hwnd, _listviewItem);
            NativeMethods.Win32Point pt = new NativeMethods.Win32Point((rc.left + rc.right) / 2, (rc.top + rc.bottom) / 2);

            if (Misc.MapWindowPoints(IntPtr.Zero, _hwnd, ref pt, 1))
            {
                // "click" on the checkbox
                Misc.ProxySendMessage(_hwnd, NativeMethods.WM_LBUTTONDOWN, (IntPtr)NativeMethods.MK_LBUTTON, NativeMethods.Util.MAKELPARAM(pt.x, pt.y));
                Misc.ProxySendMessage(_hwnd, NativeMethods.WM_LBUTTONUP, IntPtr.Zero, NativeMethods.Util.MAKELPARAM(pt.x, pt.y));
            }
        }

        #endregion Private Methods
        
        //------------------------------------------------------
        //
        //  Private Fields
        //
        //------------------------------------------------------

        #region Private Fields

        private int _listviewItem;

        #endregion Private Fields
    }
}
