// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// Description: Windows Container Proxy

using System;
using System.Windows;
using System.Collections;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Windows.Automation;
using System.Windows.Automation.Provider;
using System.Text;
using MS.Win32;


namespace MS.Internal.AutomationProxies
{
    class WindowsContainer : ProxyHwnd, IRawElementProviderHwndOverride
    {
        // ------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------

        #region Constructors

        public WindowsContainer (IntPtr hwnd, ProxyHwnd parent, int item)
            : base( hwnd, parent, item)
        {
            string className = Misc.ProxyGetClassName(hwnd);
            if (!string.IsNullOrEmpty(className))
            {
                if (className.Equals("#32770"))
                {
                    _sType = SR.Get(SRID.LocalizedControlTypeDialog);
                }
                else if (className.IndexOf("AfxControlBar", StringComparison.Ordinal) != -1)
                {
                    _sType = SR.Get(SRID.LocalizedControlTypeContainer);
                }
            }

            _fIsContent = IsTopLevelWindow();
            _fIsKeyboardFocusable = true;
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
                System.Diagnostics.Debug.Assert(idChild == 0, "Invalid Child Id, idChild != 0");
                throw new ArgumentOutOfRangeException("idChild", idChild, SR.Get(SRID.ShouldBeZero));
            }

            return new WindowsContainer(hwnd, null, 0);
        }

        #endregion

        //------------------------------------------------------
        //
        //  Patterns Implementation
        //
        //------------------------------------------------------

        #region ProxySimple Interface

        // Process all the Logical and Raw Element Properties
        internal override object GetElementProperty(AutomationProperty idProp)
        {
            if (idProp == AutomationElement.IsControlElementProperty)
            {
                return IsTopLevelWindow();
            }

            return base.GetElementProperty(idProp);
        }

        #endregion

        //------------------------------------------------------
        //
        //  Interface IRawElementProviderHwndOverride
        //
        //------------------------------------------------------
        #region IRawElementProviderHwndOverride Interface

        IRawElementProviderSimple IRawElementProviderHwndOverride.GetOverrideProviderForHwnd(IntPtr hwnd)
        {
            // return the appropriate placeholder for the given hwnd...
            // loop over all the tabs to find it.

            IntPtr hwndTab;
            int item;

            if (IsTabPage(hwnd, out hwndTab, out item))
            {
                WindowsTab wTab = new WindowsTab(hwndTab, null, 0);
                return new WindowsTabChildOverrideProxy(hwnd, wTab.CreateTabItem(item), item);
            }

            return null;
        }

        #endregion IRawElementProviderHwndOverride Interface

        //------------------------------------------------------
        //
        //  Private Methods
        //
        //------------------------------------------------------

        #region Private Methods

        private bool IsTopLevelWindow()
        {
            int style = WindowStyle;

            // WS_OVERLAPPED and WS_POPUP indicate a top level window.
            // WS_OVERLAPPED constant is 0, it does not make a good mask.  But all
            // WS_OVERLAPPED windows MUST have a caption so use WS_CAPTION instead.
            return Misc.IsBitSet(style, NativeMethods.WS_CAPTION) ||
                Misc.IsBitSet(style, NativeMethods.WS_POPUP);
        }

        private bool HasTabPageStyle(IntPtr hwnd)
        {
            int style = Misc.GetWindowStyle(hwnd);
            int exstyle = Misc.GetWindowExStyle(hwnd);

            return Misc.IsBitSet(style, NativeMethods.DS_CONTROL) &&
                   Misc.IsBitSet(style, NativeMethods.WS_CHILD) &&
                   Misc.IsBitSet(exstyle, NativeMethods.WS_EX_CONTROLPARENT);
        }

        private bool IsTabPage(IntPtr hwnd, out IntPtr hwndTab, out int item)
        {
            hwndTab = IntPtr.Zero;
            item = -1;

            if (!SafeNativeMethods.IsWindowVisible(hwnd))
            {
                return false;
            }

            try
            {
                if (!HasTabPageStyle(hwnd))
                {
                    return false;
                }
            }
            catch (ElementNotAvailableException)
            {
                // if the received an ElementNotAvailableException this hwnd can not be a
                // tab page so return false.
                return false;
            }

            string dlgName = Misc.ProxyGetText(hwnd);
            // if the dialog does not have a title there is no way to match to a tab item.
            if (string.IsNullOrEmpty(dlgName))
            {
                return false;
            }

            IntPtr hwndParent = Misc.GetParent(hwnd);
            if (hwndParent == IntPtr.Zero)
            {
                return false;
            }

            hwndTab = Misc.FindWindowEx(hwndParent, IntPtr.Zero, "SysTabControl32", null);
            // if the tab control is invisible then the tab control is not there.
            if (hwndTab == IntPtr.Zero || !SafeNativeMethods.IsWindowVisible(hwndTab))
            {
                return false;
            }

            item = WindowsTabItem.GetCurrentSelectedItem(hwndTab);
            return dlgName.Equals(WindowsTabItem.GetName(hwndTab, item, true));
        }

        #endregion

    }
}
