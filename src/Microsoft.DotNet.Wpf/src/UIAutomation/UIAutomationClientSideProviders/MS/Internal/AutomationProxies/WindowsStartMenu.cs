// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// Description: Implementation of a provider for the Classic Start Menu

using System;
using System.Windows.Automation.Provider;

namespace MS.Internal.AutomationProxies
{
    internal class WindowsStartMenu : ProxyHwnd, IRawElementProviderSimple
    {
        //------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------

        #region Constructors
        public WindowsStartMenu(IntPtr hwnd, ProxyHwnd parent, int item)
            : base( hwnd, parent, item)
        {
            _sAutomationId = "StartMenu";
        }
        #endregion Constructors

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

            return new WindowsStartMenu(hwnd, null, 0);
        }

        #endregion

    }
   
}
