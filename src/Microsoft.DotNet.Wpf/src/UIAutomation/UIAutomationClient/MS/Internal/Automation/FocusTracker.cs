// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// Description: Class that tracks Win32 focus changes

using System;
using System.Windows.Automation;
using System.Windows.Automation.Provider;
using System.Diagnostics;
using MS.Win32;

namespace MS.Internal.Automation
{
    // Class that tracks Win32 focus changes
    internal class FocusTracker : WinEventWrap
    {
        //------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------
 
        #region Constructors

        // Ctor - provide the WinEvent identifiers used to track focus changes
        internal FocusTracker()
            : base(_eventIds) 
        {
            // Intentionally not setting the callback for the base WinEventWrap since the WinEventProc override
            // in this class calls RaiseEventInThisClientOnly to actually raise the event to the client.
        }

        #endregion Constructors


        //------------------------------------------------------
        //
        //  Internal Methods
        //
        //------------------------------------------------------
 
        #region Internal Methods

        // WinEventProc - override to process WinEvents
        internal override void WinEventProc(int eventId, IntPtr hwnd, int idObject, int idChild, uint eventTime)
        {
            if (hwnd != IntPtr.Zero)
            {
                switch(eventId)
                {
                    case NativeMethods.EVENT_OBJECT_FOCUS:          OnEventObjectFocus(eventId, hwnd, idObject, idChild, eventTime); break;
                    case NativeMethods.EVENT_SYSTEM_MENUSTART:      OnEventSystemMenuStart(eventId, hwnd, idObject, idChild, eventTime); break;
                    case NativeMethods.EVENT_SYSTEM_MENUEND:        OnEventSystemMenuEnd(eventId, hwnd, idObject, idChild, eventTime); break;
                    case NativeMethods.EVENT_SYSTEM_SWITCHSTART:    OnEventSystemMenuStart(eventId, hwnd, idObject, idChild, eventTime); break;
                    case NativeMethods.EVENT_SYSTEM_SWITCHEND:      OnEventSystemMenuEnd(eventId, hwnd, idObject, idChild, eventTime); break;
                    case NativeMethods.EVENT_OBJECT_DESTROY:        OnEventObjectDestroy(eventId, hwnd, idObject, idChild, eventTime); break;
                    case NativeMethods.EVENT_SYSTEM_MENUPOPUPSTART: OnEventSystemMenuPopupStart(eventId, hwnd, idObject, idChild, eventTime); break;
                    case NativeMethods.EVENT_SYSTEM_CAPTURESTART:   OnEventSystemCaptureStart(eventId, hwnd, idObject, idChild, eventTime); break;
                    case NativeMethods.EVENT_SYSTEM_CAPTUREEND:     OnEventSystemCaptureEnd(eventId, hwnd, idObject, idChild, eventTime); break;
                }
            }
        }

        #endregion Internal Methods


        //------------------------------------------------------
        //
        //  Private Methods
        //
        //------------------------------------------------------
 
        #region Private Methods

        // HandleFocusChange - Called when a WinEvent we're listening to indicates the
        // focus has changed.  This is where the callback to the client is queued.
        private void HandleFocusChange(IntPtr hwnd, Accessible acc, int idObject, int idChild, uint eventTime)
        {
            // If there is an hwnd then notify of a focus change
            if (hwnd != IntPtr.Zero)
            {
                Debug.Assert(acc != null, "HandleFocusChange got hwnd and null IAccessible");

                // Create an event args and get the source logical element
                AutomationFocusChangedEventArgs e = new InternalAutomationFocusChangedEventArgs(idObject, idChild, eventTime);
                AutomationElement srcEl = GetFocusedElementFromWinEvent(hwnd, idObject, idChild);
                if (srcEl == null)
                {
                    // Don't raise focus change events for UI that is gone.  This has been seen when toolbar menus are
                    // being manipulated (e.g. mnu.SubMenu("File").MenuItems("Close").Click() in MITA).  We should be
                    // seeing another focus change soon and with that event we can re-establish focus. 
                    return;
                }

                // Check that item is actually focused
                // Don't do this for statics - the controls in the color picker are statics, and
                // they get focus, but OLEACC assumes statics don't get focus, so never sets the
                // focus bit. So, for now, assume statics that send focus do actually have focus.
                if (!Accessible.IsStatic(hwnd) && !Accessible.IsComboDropdown(hwnd))
                {
                    // instead of depending on oleacc to see if something has focus ask provider
                    if (!(bool)srcEl.GetCurrentPropertyValue(AutomationElement.HasKeyboardFocusProperty))
                    {
                        return;
                    }
                }
                
                // Do notifies
                ClientEventManager.RaiseEventInThisClientOnly(AutomationElement.AutomationFocusChangedEvent, srcEl, e);
            }

            // Keep track of where we are right now (may be unknown/null)
            _accCurrent = acc;
        }


        // We need to treat MSAA's FOCUS winevents differently depending on the OBJID -
        // OBJID_CLIENT gets routed to the proxies; _MENU and _SYSMENU get speical treatment.
        private AutomationElement GetFocusedElementFromWinEvent(IntPtr hwnd, int idObject, int idChild)
        {
            try
            {
                IRawElementProviderSimple provider = null;
                // These are the only object types that oleacc proxies allow to take focus.
                // (Native IAccessibles can send focus for other custom OBJID valus, but those are no use
                // to us.)
                // Try and get providers for them ourself - if we don't get anything, then
                // defer to core to get the element for the HWND itself.
                if (idObject == UnsafeNativeMethods.OBJID_CLIENT)
                {
                    // regular focus - pass it off to a proxy...
                    provider = ProxyManager.ProxyProviderFromHwnd(NativeMethods.HWND.Cast(hwnd), idChild, UnsafeNativeMethods.OBJID_CLIENT);
                }
                else if (idObject == UnsafeNativeMethods.OBJID_MENU)
                {
                    // menubar focus - see if there's a menubar pseudo-proxy registered...
                    ClientSideProviderFactoryCallback factory = ProxyManager.NonClientMenuBarProxyFactory;
                    if (factory != null)
                    {
                        provider = factory(hwnd, idChild, idObject);
                    }
                }
                else if (idObject == UnsafeNativeMethods.OBJID_SYSMENU)
                {
                    // system menu box focus - see if there's a sysmenu pseudo-proxy registered...
                    ClientSideProviderFactoryCallback factory = ProxyManager.NonClientSysMenuProxyFactory;
                    if (factory != null)
                    {
                        provider = factory(hwnd, idChild, idObject);
                    }
                }
                else if (idObject <= 0)
                {
                    return null;
                }
                else
                {
                    // This covers OBJID_CLIENT and custom OBJID cases.
                    // Pass it to the proxy manager: most proxies will just handle OBJID_CLIENT,
                    // but the MSAA proxy can potentally handle other OBJID values.
                    provider = ProxyManager.ProxyProviderFromHwnd(NativeMethods.HWND.Cast(hwnd), idChild, idObject);
                }

                if(provider != null)
                {
                    // Ask the fragment root if any of its children really have the focus
                    IRawElementProviderFragmentRoot fragment = provider as IRawElementProviderFragmentRoot;
                    if (fragment != null)
                    {
                        // if we get back something that is different than what we started with and its not null
                        // use that instead.  This is here to get the subset link in the listview but could be usefull
                        // for listview subitems as well.
                        IRawElementProviderSimple realFocus = fragment.GetFocus();
                        if(realFocus != null && !Object.ReferenceEquals(realFocus, provider))
                        {
                            provider = realFocus;
                        }
                    }

                    SafeNodeHandle hnode = UiaCoreApi.UiaNodeFromProvider(provider);
                    return AutomationElement.Wrap(hnode);
                }
                else
                {
                    // Didn't find a proxy to handle this hwnd - pass off to core...
                    return AutomationElement.FromHandle(hwnd);
                }
            }
            catch( Exception e )
            {
                if( Misc.IsCriticalException( e ) )
                    throw;

                return null;
            }
        }

        // OnEventObjectFocus - process an EventObjectFocus WinEvent.
        private void OnEventObjectFocus(int eventId, IntPtr hwnd, int idObject, int idChild, uint eventTime)
        {
            Accessible acc = Accessible.Create(hwnd, idObject, idChild);
            if (acc == null)
            {
                return;
            }

            // Keep track of last focused non-menu item, so we can restore focus when we leave menu mode
            if (!_fInMenu)
            {
                _accLastBeforeMenu = acc;
                _hwndLastBeforeMenu = hwnd;
                _idLastObject = idObject;
                _idLastChild = idChild;
            }

            HandleFocusChange(hwnd, acc, idObject, idChild, eventTime);
        }

        // OnEventSystemMenuStart - process an EventSystemMenuStart WinEvent.
        private void OnEventSystemMenuStart(int eventId, IntPtr hwnd, int idObject, int idChild, uint eventTime)
        {
            // No immediate effect on focus - we expect to get a FOCUS event after this. 
            _fInMenu = true;
        }

        // OnEventSystemMenuEnd - process an EventSystemMenuEnd WinEvent.
        private void OnEventSystemMenuEnd(int eventId, IntPtr hwnd, int idObject, int idChild, uint eventTime)
        {
            // Restore focus to where it was before the menu appeared
            if (_fInMenu)
            {
                _fInMenu = false;

                if (_accLastBeforeMenu != null)
                {
                    HandleFocusChange(_hwndLastBeforeMenu, _accLastBeforeMenu, _idLastObject, _idLastChild, eventTime);
                }
            }
        }

        // OnEventObjectDestroy - process an EventObjectDestroy WinEvent.
        private void OnEventObjectDestroy(int eventId, IntPtr hwnd, int idObject, int idChild, uint eventTime)
        {
            // Check if still alive. Ignore caret destroys - we're only interesed in 'real' objects here...
            if (idObject != UnsafeNativeMethods.OBJID_CARET && _accCurrent != null)
            {
                bool fDead = false;

                try
                {
                    int dwState = _accCurrent.State;
                    IntPtr hwndCur = _accCurrent.Window;
                    if (hwndCur == IntPtr.Zero || !SafeNativeMethods.IsWindow(NativeMethods.HWND.Cast(hwndCur)))
                    {
                        fDead = true;
                    }
                }
                catch( Exception e )
                {
                    if( Misc.IsCriticalException( e ) )
                        throw;

                    fDead = true;
                }

                if (fDead)
                {
                    // It's dead...
                    HandleFocusChange(IntPtr.Zero, null, 0, 0, eventTime);
                }
            }
        }

        // OnEventSystemMenuPopupStart - process an EventSystemMenuPopupStart WinEvent.
        private void OnEventSystemMenuPopupStart(int eventId, IntPtr hwnd, int idObject, int idChild, uint eventTime)
        {
            Accessible acc = Accessible.Create(hwnd, idObject, idChild);
            if( acc == null )
                return;

            HandleFocusChange(hwnd, acc, idObject, idChild, eventTime);
        }

        // OnEventSystemCaptureStart - process an EventSystemCaptureStart WinEvent.
        private void OnEventSystemCaptureStart(int eventId, IntPtr hwnd, int idObject, int idChild, uint eventTime)
        {
            // Deal only with Combolbox dropdowns...
            if (Accessible.IsComboDropdown(hwnd))
            {
                // Need to get id of focused item...
                try
                {
                    IntPtr i = Misc.SendMessageTimeout(NativeMethods.HWND.Cast(hwnd), UnsafeNativeMethods.LB_GETCURSEL, IntPtr.Zero, IntPtr.Zero);
                    Accessible acc = Accessible.Create(hwnd, UnsafeNativeMethods.OBJID_CLIENT, i.ToInt32() + 1);
                    if (acc == null)
                        return;

                    HandleFocusChange(hwnd, acc, idObject, idChild, eventTime);
                }
                catch (TimeoutException)
                {
                    // Ignore
                }
            }
        }

        // OnEventSystemCaptureEnd - process an EventSystemCaptureEnd WinEvent.
        private void OnEventSystemCaptureEnd(int eventId, IntPtr hwnd, int idObject, int idChild, uint eventTime)
        {
            // Deal only with Combolbox dropdowns...
            if (Accessible.IsComboDropdown(hwnd))
            {
                SafeNativeMethods.GUITHREADINFO guiThreadInfo = new SafeNativeMethods.GUITHREADINFO();

                if (!Misc.GetGUIThreadInfo(0, ref guiThreadInfo))
                {
                    return;
                }

                Accessible acc = Accessible.Create(guiThreadInfo.hwndFocus, UnsafeNativeMethods.OBJID_CLIENT, 0);
                if (acc == null)
                    return;

                HandleFocusChange(hwnd, acc, idObject, idChild, eventTime);
            }
        }

        #endregion Private Methods


        //------------------------------------------------------
        //
        //  Private Fields
        //
        //------------------------------------------------------
 
        #region Private Fields

        private static int [] _eventIds = new int [] {
                NativeMethods.EVENT_OBJECT_FOCUS,
                NativeMethods.EVENT_SYSTEM_MENUSTART,
                NativeMethods.EVENT_SYSTEM_MENUPOPUPSTART,
                NativeMethods.EVENT_SYSTEM_MENUEND,
                NativeMethods.EVENT_OBJECT_DESTROY,
                NativeMethods.EVENT_SYSTEM_CAPTURESTART,
                NativeMethods.EVENT_SYSTEM_CAPTUREEND,
                NativeMethods.EVENT_SYSTEM_SWITCHSTART,
                NativeMethods.EVENT_SYSTEM_SWITCHEND
            };

        private Accessible _accCurrent;            // the IAccessible currently being handled
        private Accessible _accLastBeforeMenu;     // the last IAccessible before a menu got focus
        private IntPtr     _hwndLastBeforeMenu;    // the last hwnd before a menu got focus
        private int        _idLastObject;          // the last idObject before a menu got focus
        private int        _idLastChild;           // the last idChild before a menu got focus
        private bool       _fInMenu;               // true if there's a menu up

        #endregion Private Fields
    }
}
