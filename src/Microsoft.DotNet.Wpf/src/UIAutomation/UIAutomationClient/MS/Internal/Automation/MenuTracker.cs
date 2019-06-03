// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// Description: Class used to track context menus appearing 

using System;
using System.Text;
using System.Windows.Automation;
using System.Diagnostics;
using MS.Win32;

namespace MS.Internal.Automation
{
    internal delegate void MenuHandler( AutomationElement rawEl, bool menuHasOpened );

    // MenuOpened - Class used to track context menus appearing
    internal class MenuTracker : WinEventWrap
    {
        //------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------
 
        #region Constructors

        internal MenuTracker(MenuHandler newHandler)
            : base(new int[] {NativeMethods.EVENT_SYSTEM_MENUPOPUPSTART, NativeMethods.EVENT_SYSTEM_MENUPOPUPEND})
        {
            AddCallback(newHandler);
        }

        #endregion Constructors


        //------------------------------------------------------
        //
        //  Internal Methods
        //
        //------------------------------------------------------
 
        #region Internal Methods

        internal override void WinEventProc(int eventId, IntPtr hwnd, int idObject, int idChild, uint eventTime)
        {
            AutomationElement rawEl = null;
            bool menuHasOpened = eventId == NativeMethods.EVENT_SYSTEM_MENUPOPUPSTART;

            // Only create a raw element wrapper if the menu has popped up.  Send a
            // null element for menu closed (the element isn't available anymore).
            // If there is any requirement to provide information on what
            // menu element has closed, client code can track menu opened and rely
            // on the following menu closed event to be that menu element.
            if( menuHasOpened )
            {
                // Ignore if this is a bogus hwnd (shouldn't happen)
                if( hwnd == IntPtr.Zero )
                    return;

                NativeMethods.HWND nativeHwnd = NativeMethods.HWND.Cast( hwnd );
                if( !SafeNativeMethods.IsWindow( nativeHwnd ) )
                    return;

                // Filter... send events for visible hwnds only
                if( !SafeNativeMethods.IsWindowVisible( nativeHwnd ) )
                    return;

                rawEl = AutomationElement.FromHandle( hwnd );
            }

            // Do callback.  This handler is called due to a WinEvent on the client.  The handler
            // is going to hand off the work of calling out to the client code to another thread
            // via a queue so it is safe to do this callback w/in the lock.
            object [] handlers = GetHandlers();
            Debug.Assert(handlers.Length <= 1, "handlers.Length");
            if( handlers.Length > 0 )
                ( ( MenuHandler )handlers[0] )( rawEl, menuHasOpened );
        }

        #endregion Internal Methods
    }
}
