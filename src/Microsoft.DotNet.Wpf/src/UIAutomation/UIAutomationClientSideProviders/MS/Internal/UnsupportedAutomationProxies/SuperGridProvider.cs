// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// Description: Super Grid Provider.
//
//              Reason for this non-standard stuff. The Office SUPERGRID is really just has a single IAccessible, 
//              which reflects the currently selected item. While the winevents it fires do have non-zero childids, 
//              it appears that we can just ignore those and always use CHILDID_SELF, since that gets the currently selected item.
// 
//              ... or at least that's what playing with inspect/accevent/accsnap appears to indicate. We may discover 
//              otherwise when we actually start using the proxy.
//
//              So -- we have created this provider that will return the Automation Name Property 
//              as Accessible.Value and the ItemType Property as Accessible.Name
//

using System;
using System.Collections;
using System.Text;
using System.Windows.Automation;
using System.Windows.Automation.Provider;
using System.Windows;
using System.Runtime.InteropServices;
using System.ComponentModel;
using MS.Internal.AutomationProxies;
using MS.Win32;

namespace MS.Internal.UnsupportedAutomationProxies
{
    // SuperGrid provider
    class SuperGridProvider : ProxyHwnd
    {
        // ------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------

        #region Constructors

        // Contructor for SuperGrid provider class.
        internal SuperGridProvider(IntPtr hwnd, ProxyFragment parent, Accessible acc)
            : base( hwnd, parent, 0)
        {
            _fIsKeyboardFocusable = true;
            _acc = acc;

            _cControlType = ControlType.Custom;
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

            // Get Accessible
            Accessible acc = null;
            if (Accessible.AccessibleObjectFromWindow(hwnd, NativeMethods.OBJID_CLIENT, ref acc) != NativeMethods.S_OK)
                acc = null;

            return new SuperGridProvider(hwnd, null, acc);
        }

        #endregion

        //------------------------------------------------------
        //
        //  Patterns Implementation
        //
        //------------------------------------------------------

        #region ProxySimple Interface

        // Process all the Logical and Raw Element Properties
        internal override object GetElementProperty (AutomationProperty idProp)
        {
            if (_acc != null)
            {
                if (idProp == AutomationElement.NameProperty)
                    return _acc.Value;
                else if (idProp == AutomationElement.ItemTypeProperty)
                    return _acc.Name;
            }

            return base.GetElementProperty (idProp);
        }

        #endregion

      
        // ------------------------------------------------------
        //
        // Private Methods
        //
        // ------------------------------------------------------

        #region Private Methods


        #endregion

        // ------------------------------------------------------
        //
        // Private Fields
        //
        // ------------------------------------------------------

        #region Private Fields

        private Accessible _acc;  // Accessible is used for SuperGrid.

        #endregion

    }
}
