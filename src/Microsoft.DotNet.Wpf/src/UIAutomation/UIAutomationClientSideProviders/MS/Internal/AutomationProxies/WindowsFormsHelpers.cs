// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// Description:
//      This proxy is the base class for all proxies that support Windows
//      Forms controls.
//      All generic Windows Forms functionality goes here.
//

using System;
using System.Text;
using System.Collections;
using System.Windows.Automation;
using System.Windows.Automation.Provider;
using System.Runtime.InteropServices;
using MS.Win32;

namespace MS.Internal.AutomationProxies
{
    // Helper static class used by the Win32 proxies to get Winforms information
    static class WindowsFormsHelper
    {
        #region Proxy Create

        // Static Create method called by UIAutomation to create proxies for Winforms controls.
        // returns null if unsuccessful
        internal static IRawElementProviderSimple Create(IntPtr hwnd, int idChild, int idObject)
        {
            // Currently there is an issue with CLR remoting that causes Accessible.CreateNativeFromEvent() to fail
            // for Winforms controls.  Until that is resolved use AccessibleObjectFromWindow() instead.  It will
            // return a Native IAccessble and not a OleAcc implementaion.  Winforms does provide a Native IAccessible.

            Accessible acc = null;
            if (Accessible.AccessibleObjectFromWindow(hwnd, idObject, ref acc) != NativeMethods.S_OK || acc == null)
            {
                return null;
            }

            switch (acc.Role)
            {
                // ============================================================
                // WinformsSpinner controls are not identifiable by classname or 
                // other simple properties.  The following case calls the 
                // WinformsSpinner constructor which in turn tries to establish 
                // the class of the control as a fact or returns null.
                case AccessibleRole.Combobox:
                    return WinformsSpinner.Create( hwnd, idChild, idObject );
                // ============================================================

                case AccessibleRole.SpinButton:
                    return WindowsUpDown.Create( hwnd, idChild, idObject );

                case AccessibleRole.Grouping:
                    return new WindowsButton(hwnd, null, WindowsButton.ButtonType.GroupBox, Misc.GetWindowStyle(hwnd) & NativeMethods.BS_TYPEMASK, acc);

                case AccessibleRole.StatusBar:
                    WindowsStatusBar sb = new WindowsStatusBar(hwnd, null, 0, acc);
                    if (sb == null)
                    {
                        return null;
                    }
                    return idChild == NativeMethods.CHILD_SELF ? sb : sb.CreateStatusBarPane(idChild);

                default:
                    break;
            }

            return null;
        }

        // Static Create method called by UIAutomation to create a Button proxy for Winforms Buttons.
        // returns null if unsuccessful
        internal static IRawElementProviderSimple CreateButton(IntPtr hwnd)
        {
            // Currently there is an issue with CLR remoting that causes Accessible.CreateNativeFromEvent() to fail
            // for Winforms controls.  Until that is resolved use AccessibleObjectFromWindow() instead.  It will
            // return a Native IAccessble and not a OleAcc implementaion.  Winforms does provide a Native IAccessible.

            Accessible acc = null;
            if (Accessible.AccessibleObjectFromWindow(hwnd, NativeMethods.OBJID_CLIENT, ref acc) != NativeMethods.S_OK || acc == null)
            {
                return null;
            }

            switch (acc.Role)
            {
                case AccessibleRole.CheckButton:
                    return new WindowsButton(hwnd, null, WindowsButton.ButtonType.CheckBox, Misc.GetWindowStyle(hwnd) & NativeMethods.BS_TYPEMASK, acc);

                case AccessibleRole.Grouping:
                    return new WindowsButton(hwnd, null, WindowsButton.ButtonType.GroupBox, Misc.GetWindowStyle(hwnd) & NativeMethods.BS_TYPEMASK, acc);

                case AccessibleRole.PushButton:
                    return new WindowsButton(hwnd, null, WindowsButton.ButtonType.PushButton, Misc.GetWindowStyle(hwnd) & NativeMethods.BS_TYPEMASK, acc);

                case AccessibleRole.RadioButton:
                    return new WindowsButton(hwnd, null, WindowsButton.ButtonType.RadioButton, Misc.GetWindowStyle(hwnd) & NativeMethods.BS_TYPEMASK, acc);

                default:
                    break;
            }

            return null;
        }

        #endregion

        #region Internal Methods

        // ------------------------------------------------------
        //
        // Internal Methods
        //
        // ------------------------------------------------------

        // Checks if an hwnd is a winform.
        // Returns True/False if the hwnd is a winform.
        static internal FormControlState GetControlState(IntPtr hwnd)
        {
            return IsWindowsFormsControl(hwnd) ? FormControlState.True : FormControlState.False;
        }

        // Checks if an a class name is a winform classname.
        // Returns True/False if the classname is a winform.
        static internal bool IsWindowsFormsControl(string className)
        {
            return className.IndexOf(_WindowsFormsClassName, StringComparison.OrdinalIgnoreCase) > -1;
        }

        // Checks if an a class name is a winform classname.
        // Returns True/False if the classname is a winform.
        static internal bool IsWindowsFormsControl(IntPtr hwnd)
        {
            return IsWindowsFormsControl(Misc.GetClassName(hwnd));
        }

        static internal bool IsWindowsFormsControl(IntPtr hwnd, ref FormControlState state)
        {
            if (state == FormControlState.Undeterminate)
            {
                state = GetControlState(hwnd);
            }

            return state == FormControlState.True ? true : false;
        }

        // The control name is the only real "Persistent" ID in Windows Forms
        static internal string WindowsFormsID(IntPtr hwnd)
        {
            return GetControlName(hwnd);
        }

        // Extract the internal Name property of the Windows Forms control using
        // the WM_GETCONTROLNAME message.
        static internal string GetControlName(IntPtr hwnd)
        {
            string winFormsID = "";
            if (XSendMessage.XSend(hwnd, WM_GETCONTROLNAME, new IntPtr(Misc.MaxLengthNameProperty), ref winFormsID, Misc.MaxLengthNameProperty))
            {
                return winFormsID;
            }
            return null;
        }

        #endregion

        #region Internal Fields

        // ------------------------------------------------------
        //
        // Internal Fields
        //
        // ------------------------------------------------------

        // The different states for figurating out if an hwnd is a winform.
        // Underminate implies value not set yet. Must call GetControlState,
        // otherwise the state is cached.
        internal enum FormControlState
        {
            Undeterminate,
            False,
            True,
        }

        #endregion

        #region Private Fields

        // ------------------------------------------------------
        //
        // Private Fields
        //
        // ------------------------------------------------------

        // Use this string to determine if this is a Windows Forms control or not
        private const string _WindowsFormsClassName = "windowsforms";

        // Private Message to know the underlying name for a control
        private static int WM_GETCONTROLNAME = Misc.RegisterWindowMessage("WM_GETCONTROLNAME");

        #endregion
    }
}
