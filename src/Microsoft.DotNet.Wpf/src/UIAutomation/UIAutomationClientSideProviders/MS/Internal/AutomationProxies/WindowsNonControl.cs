// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// Description: A Windows Proxy to set IsContent and IsControl to false.
//  By setting both IsContent and IsControl to false this will hide these
//  controls from the Content view of the Automation Tree.

using System;
using System.Windows.Automation;
using System.Windows.Automation.Provider;

namespace MS.Internal.AutomationProxies
{
    internal class WindowsNonControl: ProxyHwnd
    {
        // ------------------------------------------------------
        //
        // Constructors
        //
        // ------------------------------------------------------

        #region Constructors

        private WindowsNonControl(IntPtr hwnd, ProxyFragment parent, int item)
            : base(hwnd, parent, item)
        {
            _fIsContent = false;
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
            ArgumentOutOfRangeException.ThrowIfNotEqual(idChild, 0);

            return new WindowsNonControl(hwnd, null, idChild);
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
            if (idProp == AutomationElement.IsControlElementProperty)
            {
                return false;
            }

            return base.GetElementProperty (idProp);
        }

        #endregion
    }
}

