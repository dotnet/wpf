// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// Description:
//              The SecurityMgrSite is an implementation of Urlmon's IInternetSecurityMgrSite. 
//
//              It is primarily used to supply an hwnd to be modal to- when a ProcessUrlAction call
//              is required to show UI. 

using System;
using MS.Win32;
using System.Runtime.InteropServices;
using System.Windows;
using System.Security;
using MS.Internal.AppModel;

namespace MS.Internal
{
    internal class SecurityMgrSite : NativeMethods.IInternetSecurityMgrSite
    {
        internal SecurityMgrSite()
        {
        }

        ///<SecurityNote> 
        ///     Critical calls unsecure handle. 
        ///     This function should only be called by Urlmon. 
        ///
        ///     Which is un-managed code - ergo also critical. 
        ///</SecurityNote> 
        [SecurityCritical]
        public void GetWindow( /* [out] */ ref IntPtr phwnd)
        {
            phwnd = IntPtr.Zero;

            if (Application.Current != null)
            {
                Window curWindow = Application.Current.MainWindow;

#if NETFX
                Invariant.Assert( Application.Current.BrowserCallbackServices == null || ( curWindow is RootBrowserWindow )); 
#endif

                if (curWindow != null)
                {
                    phwnd = curWindow.CriticalHandle;
                }
            }
        }

        public void EnableModeless( /* [in] */ bool fEnable)
        {
        }
    }
}
