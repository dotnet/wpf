// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// Description: Tooltip Proxy

// PRESHARP: In order to avoid generating warnings about unkown message numbers and unknown pragmas.
#pragma warning disable 1634, 1691

using System;
using System.Globalization;
using System.Text;
using System.Windows.Automation;
using System.Windows.Automation.Provider;
using System.Windows;
using System.Runtime.InteropServices;
using System.ComponentModel;
using MS.Win32;

namespace MS.Internal.AutomationProxies
{
    // Class definition for the WindowsTooltip proxy. 
    class WindowsTooltip : ProxyHwnd
    {
        // ------------------------------------------------------
        //
        // Constructors
        //
        // ------------------------------------------------------

        #region Constructors

        // Contructor for the tooltip proxy class.
        WindowsTooltip (IntPtr hwnd, ProxyFragment parent, int item)
            : base( hwnd, parent, item)
        {
            // Set the control type string to return properly the properties.
            _cControlType = ControlType.ToolTip;

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

            return new WindowsTooltip(hwnd, null, idChild);
        }

        // Static create method called by the event tracker system.
        // WinEvents are raised only when a notification has been set for a 
        // specific item.
        internal static void RaiseEvents (IntPtr hwnd, int eventId, object idProp, int idObject, int idChild)
        {
            if (idObject != NativeMethods.OBJID_VSCROLL && idObject != NativeMethods.OBJID_HSCROLL)
            {
                WindowsTooltip wtv = new WindowsTooltip(hwnd, null, 0);
                wtv.DispatchEvents(eventId, idProp, idObject, idChild);
            }
        }

        #endregion Proxy Create 

        #region ProxyHwnd Interface

        internal override void AdviseEventAdded( AutomationEvent eventId, AutomationProperty[] aidProps )
        {
            base.AdviseEventAdded( eventId, aidProps );

            // If the framework is advising for ToolTipOpenedEvent then go ahead and raise this event now 
            // since the WinEvent we would listen to (usually EVENT_OBJECT_SHOW) has already occurrred
            // (it is why Advise is being called).  No other action is necessary because when this ToolTip
            // goes away, AdviseEventRemoved is going to be called.  In other words, this proxy gets
            // created when the ToolTip opens and thrown away when it closes so no need to keep state
            // that we want to listen for more SHOWS or CREATES - there's always one for any one instance.
            if( eventId == AutomationElement.ToolTipOpenedEvent )
            {
                AutomationEventArgs e = new AutomationEventArgs(AutomationElement.ToolTipOpenedEvent);
                AutomationInteropProvider.RaiseAutomationEvent(AutomationElement.ToolTipOpenedEvent, this, e);
            }
            else if( eventId == AutomationElement.ToolTipClosedEvent )
            {
                // subscribe to ToolTip specific events, keeping track of how many times the event has been added
                WinEventTracker.AddToNotificationList( IntPtr.Zero, new WinEventTracker.ProxyRaiseEvents( OnToolTipEvents ), _toolTipEventIds, _toolTipEventIds.Length );
                _listenerCount++;
            }
        }

        internal override void AdviseEventRemoved( AutomationEvent eventId, AutomationProperty[] aidProps )
        {
            base.AdviseEventRemoved(eventId, aidProps);

            // For now, ToolTips only raise ToolTip-specific events when they close
            if( eventId != AutomationElement.ToolTipClosedEvent )
                return;

            if( _listenerCount > 0 )
            {
                // decrement the event counter
                --_listenerCount;
                WinEventTracker.RemoveToNotificationList( IntPtr.Zero, _toolTipEventIds, new WinEventTracker.ProxyRaiseEvents( OnToolTipEvents ), _toolTipEventIds.Length );
            }
        }

        #endregion ProxyHwnd Interface

        //------------------------------------------------------
        //
        //  Patterns Implementation
        //
        //------------------------------------------------------

        #region ProxySimple Interface

        //Gets the localized name
        internal override string LocalizedName
        {
            get
            {
                return GetText();
            }
        }

        #endregion

        //------------------------------------------------------
        //
        //  Private Methods
        //
        //------------------------------------------------------

        #region Private Methods

        private static void OnToolTipEvents( IntPtr hwnd, int eventId, object idProp, int idObject, int idChild )
        {
            if (idObject != NativeMethods.OBJID_WINDOW)
            {
                return;
            }

            if (!IsToolTip(hwnd))
            {
                return;
            }

            // Raise ToolTipClosedEvent on OBJECT_HIDE WinEvents.  Not raising the event for EVENT_OBJECT_DESTROY
            // because to do this means having to change code downstream from RaiseAutomationEvent to accept a
            // null src.  (Client-side proxies that raise events end up going through server-side
            // code) would be a good time to fix this issue (may then be able to pass null src).  Most tool tips
            // currently get created, then shown and hidden, and are destroyed when the app exits so the impact
            // here should be minimal since the ToolTip is probaby not showing when the app exits.
            if( eventId == NativeMethods.EVENT_OBJECT_HIDE /*|| eventId == NativeMethods.EVENT_OBJECT_DESTROY*/ )
            {
                WindowsTooltip wtv = new WindowsTooltip( hwnd, null, 0 );
                AutomationEventArgs e = new AutomationEventArgs( AutomationElement.ToolTipClosedEvent );
                AutomationInteropProvider.RaiseAutomationEvent( AutomationElement.ToolTipClosedEvent, wtv, e );
            }
        }

        private static bool IsToolTip( IntPtr hwnd )
        {
            // If we can't determine this is a ToolTip then just return false
            if (!UnsafeNativeMethods.IsWindow(hwnd))
            {
                return false;
            }

            string className = Misc.ProxyGetClassName(hwnd);

            return String.Compare(className, "tooltips_class32", StringComparison.OrdinalIgnoreCase) == 0 ||
                String.Compare(className, CLASS_TITLEBAR_TOOLTIP, StringComparison.OrdinalIgnoreCase) == 0 ||
                String.Compare(className, "VBBubble", StringComparison.OrdinalIgnoreCase) == 0;
        }

        private string GetText()
        {
            string className = Misc.ProxyGetClassName(_hwnd);

            if (String.Compare(className, CLASS_TITLEBAR_TOOLTIP, StringComparison.OrdinalIgnoreCase) == 0)
            {
                return GetTitleBarToolTipText();
            }
            else if (String.Compare(className, "VBBubble", StringComparison.OrdinalIgnoreCase) == 0)
            {
                // The WM_GETTEXT should work for VBBubble.  It seems that the string being returned is having
                // a problem with Unicode covertion and therefore trunk'ing the string after the first character.
            }

            // For tooltips_class32 WM_GETTEXT works fine at getting the text off of the tooltip.
            return Misc.ProxyGetText(_hwnd);
        }

        // Tooltips for titlebar parts requires figuring out what titlebar part the mouse is over and returning
        // a string defined in this dll that represents the part.  The hittesting technique is sensitive to the
        // desktop theme and composition.  The following method uses one technique for composition and another
        // for all other cases.  Fix for WinOS Bug will allow using the technique 
        // used in GetTitleBarToolTipTextForDWMEnabled on Vista regardless of themes.
        private string GetTitleBarToolTipText()
        {
            if (System.Environment.OSVersion.Version.Major >= 6)
            {
                int isDWMEnabled = 0; // DWM is not enabled
                try
                {
#pragma warning suppress 56031 // No need to check return value; failure means it isn't enabled
                    UnsafeNativeMethods.DwmIsCompositionEnabled(out isDWMEnabled);
                }
                catch (DllNotFoundException)
                {
                    // The API is not available so we can't be under the DWM
                    // simply ignore the exception
                }

                // Using new APIs in Vista to figure out where the cursor is give more
                // accurate results when composition is enabled.
                if (isDWMEnabled != 0)
                    return GetTitleBarToolTipTextForDWMEnabled();
            }

            return GetTitleBarToolTipTextHitTest();
        }

        // For Vista getting the part of the titlebar that a tooltip belongs to is more
        // reliable across themes
        private string GetTitleBarToolTipTextForDWMEnabled()
        {
            // The mouse is over the titlebar item so get that point on the screen
            NativeMethods.Win32Point pt = new NativeMethods.Win32Point();
            if (!Misc.GetCursorPos(ref pt))
            {
                return "";
            }

            // Find the titlebar hwnd
            IntPtr hwnd = UnsafeNativeMethods.WindowFromPhysicalPoint(pt.x, pt.y);
            if (hwnd == IntPtr.Zero)
            {
                return "";
            }

            // Get the rects for each titlbar part
            Rect[] rects = Misc.GetTitlebarRects(hwnd);

            // Look from back to front - front is entire titlebar rect
            int scan;
            for (scan = rects.Length - 1; scan >= 0; scan--)
            {
                // Not using Misc.PtInRect because including the bounding pixels all the way around gives
                // better results; tooltips may appear when the mouse is one or two pixels outside of the
                // bounding rect so even this technique may miss.
                if (pt.x >= rects[scan].Left && pt.x <= rects[scan].Right && pt.y >= rects[scan].Top && pt.y <= rects[scan].Bottom)
                {
                    break;
                }
            }

            switch (scan)
            {
                case NativeMethods.INDEX_TITLEBAR_MINBUTTON:
                    if (Misc.IsBitSet(WindowStyle, NativeMethods.WS_MINIMIZE))
                        return SR.Get(SRID.LocalizedNameWindowsTitleBarButtonRestore);
                    else
                        return SR.Get(SRID.LocalizedNameWindowsTitleBarButtonMinimize);

                case NativeMethods.INDEX_TITLEBAR_HELPBUTTON:
                    return SR.Get(SRID.LocalizedNameWindowsTitleBarButtonContextHelp);

                case NativeMethods.INDEX_TITLEBAR_MAXBUTTON:
                    if (Misc.IsBitSet(WindowStyle, NativeMethods.WS_MAXIMIZE))
                        return SR.Get(SRID.LocalizedNameWindowsTitleBarButtonRestore);
                    else
                        return SR.Get(SRID.LocalizedNameWindowsTitleBarButtonMaximize);

                case NativeMethods.INDEX_TITLEBAR_CLOSEBUTTON:
                    return SR.Get(SRID.LocalizedNameWindowsTitleBarButtonClose);

                case NativeMethods.INDEX_TITLEBAR_SELF:
                    return Misc.ProxyGetText(hwnd);

                default:
                    return "";
            }
        }

        private string GetTitleBarToolTipTextHitTest()
        {
            NativeMethods.Win32Point pt = new NativeMethods.Win32Point();
            if (!Misc.GetCursorPos(ref pt))
            {
                return "";
            }

            IntPtr hwnd = UnsafeNativeMethods.WindowFromPhysicalPoint(pt.x, pt.y);
            if (hwnd == IntPtr.Zero)
            {
                return "";
            }

            int hit = Misc.ProxySendMessageInt(hwnd, NativeMethods.WM_NCHITTEST, IntPtr.Zero, NativeMethods.Util.MAKELPARAM(pt.x, pt.y));

            switch (hit)
            {
                case NativeMethods.HTMINBUTTON:
                    if (Misc.IsBitSet(Misc.GetWindowStyle(hwnd), NativeMethods.WS_MINIMIZE))
                        return SR.Get(SRID.LocalizedNameWindowsTitleBarButtonRestore);
                    else
                        return SR.Get(SRID.LocalizedNameWindowsTitleBarButtonMinimize);

                case NativeMethods.HTMAXBUTTON:
                    if (Misc.IsBitSet(Misc.GetWindowStyle(hwnd), NativeMethods.WS_MAXIMIZE))
                        return SR.Get(SRID.LocalizedNameWindowsTitleBarButtonRestore);
                    else
                        return SR.Get(SRID.LocalizedNameWindowsTitleBarButtonMaximize);

                case NativeMethods.HTCLOSE:
                case NativeMethods.HTMDICLOSE:
                    return SR.Get(SRID.LocalizedNameWindowsTitleBarButtonClose);

                case NativeMethods.HTHELP:
                    return SR.Get(SRID.LocalizedNameWindowsTitleBarButtonContextHelp);

                case NativeMethods.HTMDIMINBUTTON:
                    return SR.Get(SRID.LocalizedNameWindowsTitleBarButtonMinimize);

                case NativeMethods.HTMDIMAXBUTTON:
                    return SR.Get(SRID.LocalizedNameWindowsTitleBarButtonMaximize);

                case NativeMethods.HTCAPTION:
                    return Misc.ProxyGetText(hwnd);

                default:
                    break;
            }

            return "";
        }
        #endregion Private Methods

        // ------------------------------------------------------
        //
        // Private Fields and Types Declaration
        //
        // ------------------------------------------------------

        #region Private Fields

        private readonly static WinEventTracker.EvtIdProperty[] _toolTipEventIds = new WinEventTracker.EvtIdProperty[] 
        {
            new WinEventTracker.EvtIdProperty(NativeMethods.EVENT_OBJECT_HIDE, 0), 
            //see comment in OnToolTipEvents
            //new WinEventTracker.EvtIdProperty(NativeMethods.EVENT_OBJECT_DESTROY, 0)
        };
        private static int _listenerCount;

        private const string CLASS_TITLEBAR_TOOLTIP = "#32774";

        #endregion Private Fields
    }
}
