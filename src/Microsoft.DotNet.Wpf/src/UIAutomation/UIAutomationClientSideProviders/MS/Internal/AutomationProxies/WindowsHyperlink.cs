// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// Description: Windows Hyperlink Proxy

using System;
using System.Runtime.InteropServices;
using System.ComponentModel;
using System.Text;
using System.Collections;
using System.Windows.Automation;
using System.Windows.Automation.Provider;
using System.Windows;
using MS.Win32;

namespace MS.Internal.AutomationProxies
{
    // Implementation of the Hyperlink (SysLink) proxy.
    class WindowsHyperlink: ProxyHwnd
    {
        // ------------------------------------------------------
        //
        // Constructors
        //
        // ------------------------------------------------------

        #region Constructors
        
        WindowsHyperlink (IntPtr hwnd, ProxyFragment parent, int item)
            : base( hwnd, parent, item)
        {
            // Set the strings to return properly the properties.
            _cControlType = ControlType.Hyperlink;

            // support for events
            _createOnEvent = new WinEventTracker.ProxyRaiseEvents(RaiseEvents);
        }

        #endregion Constructors

        #region Proxy Create

        // Static Create method called by UIAutomation to create this proxy.
        // <returns null if unsuccessful
        internal static IRawElementProviderSimple Create(IntPtr hwnd, int idChild, int idObject)
        {
            return Create(hwnd, idChild);
        }

        private static IRawElementProviderSimple Create(IntPtr hwnd, int idChild)
        {
            // (WindowsHyperlink UIA proxy isn't handling child elements in the SysLink control)
            // Consider falling back to the native MSAA provider to support child elements of 
            // the SysLink control or implementing the code here to return back the child element.  
            // Narrator may need to be modified to work properly - to avoid reading the link text
            // twice due to receiving focus change events for the outer text (which includes the 
            // link text) and a focus change for the link itself. 
            return new WindowsHyperlink(hwnd, null, idChild);
        }

        // Static Create method called by the event tracker system
        internal static void RaiseEvents(IntPtr hwnd, int eventId, object idProp, int idObject, int idChild)
        {
            if (idObject != NativeMethods.OBJID_VSCROLL && idObject != NativeMethods.OBJID_HSCROLL)
            {
                ProxySimple wtv = new WindowsHyperlink(hwnd, null, idChild);

                if (idProp == InvokePattern.InvokedEvent)
                {
                    wtv = new WindowsHyperlinkItem(hwnd, (ProxyFragment)wtv, idChild);
                }

                wtv.DispatchEvents(eventId, idProp, idObject, idChild);
            }
        }

        #endregion Proxy Create

        // ------------------------------------------------------
        //
        // Patterns Implementation
        //
        // ------------------------------------------------------

        #region ProxySimple Interface

        // Gets the bounding rectangle for this element
        internal override Rect BoundingRectangle
        {
            get
            {
                // ...otherwise, we need to determine the bounding rectangle
                // for this node.
                //
                // How do I do that, given that there is no API
                // to get the bounding rect for the embedded links?  In
                // fact, the individual links could flow across lines of
                // text, so either their "bounding rect" could include parts
                // of the screen outside the link itself, or they actually
                // have several bouding rects.
                //
                // Right now we just return the bounding rect of the whole SysLink hwnd.
                //
                /*
                BoundingRectangle = NativeMethods.Win32Rect.Empty;
                return AutomationElement.NotSupported;
                */
                return base.BoundingRectangle;
            }
        }

        //Gets the localized name
        internal override string LocalizedName
        {
            get
            {
                // ...then just return the window text...
                return Misc.StripMnemonic(RemoveHTMLAnchorTag(Misc.ProxyGetText(_hwnd)));
            }
        }

        #endregion ProxySimple Interface

        #region ProxyHwnd Interface

        // Builds a list of Win32 WinEvents to process a UIAutomation Event.
        // Param name="idEvent", UIAuotmation event
        // Param name="cEvent"out, number of winevent set in the array
        // Returns an array of Events to Set. The number of valid entries in this array pass back in cEvent
        protected override WinEventTracker.EvtIdProperty[] EventToWinEvent(AutomationEvent idEvent, out int cEvent)
        {
            if (idEvent == InvokePattern.InvokedEvent)
            {
                cEvent = 1;
                return new WinEventTracker.EvtIdProperty[1] { new WinEventTracker.EvtIdProperty(NativeMethods.EventSystemCaptureEnd, idEvent) };
            }

            return base.EventToWinEvent(idEvent, out cEvent);
        }

        #endregion ProxyHwnd Interface

        #region ProxyFragment Interface

        // Returns the next sibling element in the raw hierarchy.
        // Peripheral controls have always negative values.
        // Returns null if no next child.
        internal override ProxySimple GetNextSibling (ProxySimple child)
        {
            return GetLinkItem (child._item + 1) ? CreateHyperlinkItem (_linkItem, child._item + 1) : null;
        }

        // Returns the previous sibling element in the raw hierarchy.
        // Peripheral controls have always negative values.
        // Returns null is no previous.
        internal override ProxySimple GetPreviousSibling (ProxySimple child)
        {
            return GetLinkItem (child._item - 1) ? CreateHyperlinkItem (_linkItem, child._item - 1) : null;
        }

        // Returns the first child element in the raw hierarchy.
        internal override ProxySimple GetFirstChild ()
        {
            return GetLinkItem (0) ? CreateHyperlinkItem (_linkItem, 0) : null;
        }

        // Returns the last child element in the raw hierarchy.
        internal override ProxySimple GetLastChild ()
        {
            //
            // Is there a better way to do this?
            //
            // Keep trying to find elements until we fail,
            // then assume that the prior one must have been the last.
            //
            int iLastItem = -2;
            UnsafeNativeMethods.LITEM linkItemLast;

            do
            {
                linkItemLast = _linkItem;
                iLastItem++;
            } while (GetLinkItem (iLastItem + 1));

            return iLastItem < 0 ? CreateHyperlinkItem (linkItemLast, iLastItem + 1) : null;
        }

        // Returns a Proxy element corresponding to the specified
        // screen coordinates.
        internal override ProxySimple ElementProviderFromPoint (int x, int y)
        {
            //
            // Send the link an LM_HITTEST message.
            //
            // Allocate a local hit test info struct.
            UnsafeNativeMethods.LHITTESTINFO HitTestInfo = new UnsafeNativeMethods.LHITTESTINFO();

            // Fill in the coordinates that we want to check.
            HitTestInfo.pt.x = x;
            HitTestInfo.pt.y = y;

            // Convert screen coordinates to client coordinates.
            if (!Misc.MapWindowPoints(IntPtr.Zero, _hwnd, ref HitTestInfo.pt, 1))
            {
                base.ElementProviderFromPoint(x, y);
            }

            // Fill in index and state info.
            HitTestInfo.item.mask = NativeMethods.LIF_ITEMINDEX | NativeMethods.LIF_STATE;
            HitTestInfo.item.iLink = 0;
            HitTestInfo.item.stateMask = NativeMethods.LIS_ENABLED;
            HitTestInfo.item.state = 0;

            bool bGetItemResult;
            unsafe
            {
                // Send the LM_HITTEST message.
                bGetItemResult = XSendMessage.XSend(_hwnd, NativeMethods.LM_HITTEST, IntPtr.Zero, new IntPtr(&HitTestInfo), Marshal.SizeOf(HitTestInfo.GetType()));
            }

            if (bGetItemResult == true && HitTestInfo.item.iLink >= 0 && GetLinkItem (HitTestInfo.item.iLink))
            {
                return CreateHyperlinkItem (_linkItem, HitTestInfo.item.iLink);
            }

            return base.ElementProviderFromPoint (x, y);
        }

        // Returns an item corresponding to the focused element (if there is one), 
        // or null otherwise.
        internal override ProxySimple GetFocus ()
        {
            //
            // Is there a better way to do this?
            //
            // Keep trying to find elements until we fail,
            //
            for (int iCurrentItem = 0; GetLinkItem(iCurrentItem); iCurrentItem++)
            {
                // If the item was focused...
                if (Misc.IsBitSet(_linkItem.state, NativeMethods.LIS_FOCUSED))
                {
                    return CreateHyperlinkItem(_linkItem, iCurrentItem);
                }
            }

            return null;
        }

        #endregion ProxyFragment Interface

        // ------------------------------------------------------
        //
        // Private Methods
        //
        // ------------------------------------------------------

        #region Private Methods

        // Copy creator for a link or link item.
        private ProxySimple CreateHyperlinkItem(UnsafeNativeMethods.LITEM linkItem, int index)
        {
            return new WindowsHyperlinkItem(_hwnd, this, index);
        }

        private bool GetLinkItem (int item)
        {
            if (item < 0)
            {
                return false;
            }

            // Set the members about which we care.
            _linkItem.mask = NativeMethods.LIF_ITEMINDEX | NativeMethods.LIF_STATE;
            _linkItem.iLink = item;
            _linkItem.state = 0;
            _linkItem.stateMask = NativeMethods.LIS_ENABLED;

            unsafe
            {
                fixed (UnsafeNativeMethods.LITEM* pLinkItem = &_linkItem)
                {
                    return XSendMessage.XSend(_hwnd, NativeMethods.LM_GETITEM, IntPtr.Zero, new IntPtr(pLinkItem), sizeof(UnsafeNativeMethods.LITEM));
                }
            }
        }

        private string RemoveHTMLAnchorTag(string text)
        {
            // If there are no anchor tag then it's ok just return it
            if (text.IndexOf("<A", StringComparison.OrdinalIgnoreCase) < 0)
            {
                return text;
            }

            char[] ach = text.ToCharArray();
            bool inAnchorMode = false;
            int dest = 0;

            for (int source = 0; source < ach.Length; source++)
            {
                if (!inAnchorMode)
                {
                    if (source + 1 < ach.Length && ach[source] == '<' && (ach[source + 1] == 'A' || ach[source + 1] == 'a'))
                    {
                        inAnchorMode = true;
                    }
                    else if (source + 2 < ach.Length && ach[source] == '<' && ach[source + 1] == '/' && (ach[source + 2] == 'A' || ach[source + 2] == 'a'))
                    {
                        inAnchorMode = true;
                    }
                    else
                    {
                        ach[dest++] = ach[source];
                    }
                }
                else if (ach[source] == '>')
                {
                    inAnchorMode = false;
                }
            }

            return new string(ach, 0, dest);
        }

        #endregion Private Methods

        // ------------------------------------------------------
        //
        // Private Fields
        //
        // ------------------------------------------------------

        #region Private Fields

        // Temporary variable used all over the proxy
        private UnsafeNativeMethods.LITEM _linkItem;

        #endregion Private Fields
    }

    // ------------------------------------------------------
    //
    //  WindowsHyperlinkItem Class
    //
    //------------------------------------------------------

    // Implementation of the PAW WindowsHyperlinkItem (SysLink) proxy.
    class WindowsHyperlinkItem : ProxySimple, IInvokeProvider
    {
        // ------------------------------------------------------
        //
        // Constructors
        //
        // ------------------------------------------------------

        #region Constructors

        // Constructor.
        internal WindowsHyperlinkItem(IntPtr hwnd, ProxyFragment parent, int item)
            : base( hwnd, parent, item)
        {
            // Set the strings to return properly the properties.
            _cControlType = ControlType.Hyperlink;
        }

        #endregion Constructors

        //------------------------------------------------------
        //
        //  Patterns Implementation
        //
        //------------------------------------------------------

        #region ProxySimple Interface

        // Returns a pattern interface if supported.
        internal override object GetPatternProvider(AutomationPattern iid)
        {
            return iid == InvokePattern.Pattern ? this : null;
        }

        // Sets the focus to this item.
        internal override bool SetFocus()
        {
            //
            // Send the link an LM_SETITEM message.
            //
            // Allocate a local LITEM struct.
            UnsafeNativeMethods.LITEM linkItem = new UnsafeNativeMethods.LITEM();

            // Fill in the coordinates about which we care.
            linkItem.mask = NativeMethods.LIF_ITEMINDEX | NativeMethods.LIF_STATE;
            linkItem.iLink = _item;
            linkItem.stateMask = NativeMethods.LIS_FOCUSED;
            linkItem.state = NativeMethods.LIS_FOCUSED;

            unsafe
            {
                // Send the LM_SETITEM message.
                return XSendMessage.XSend(_hwnd, NativeMethods.LM_SETITEM, IntPtr.Zero, new IntPtr(&linkItem), Marshal.SizeOf(linkItem.GetType()));
            }
        }

        //Gets the localized name
        internal override string LocalizedName
        {
            get
            {
                // Cannot get the associated with each each hyperlink (within <A></A>)
                return "";
            }
        }

        #endregion ProxySimple Interface

        #region Invoke Pattern

        // Same as clicking on an hyperlink
        void IInvokeProvider.Invoke()
        {
            // Check that button can be clicked.
            //
            // This state could change anytime.
            //

            // Make sure that the control is enabled
            if (!SafeNativeMethods.IsWindowEnabled(_hwnd))
            {
                throw new ElementNotEnabledException();
            }

            if (!SafeNativeMethods.IsWindowVisible(_hwnd))
            {
                throw new InvalidOperationException(SR.Get(SRID.OperationCannotBePerformed));
            }

            //
            // Get the bounding rect for the window.
            //
            NativeMethods.Win32Rect BoundingRect = NativeMethods.Win32Rect.Empty;
            if (!Misc.GetWindowRect(_hwnd, ref BoundingRect))
            {
                return;
            }

            //
            // All we really need here are the height and the width,
            // so we don't even need to translate between screen
            // and client coordinates.
            //
            int width = BoundingRect.right - BoundingRect.left;
            int height = BoundingRect.bottom - BoundingRect.top;

            //
            // Determine the point to click.
            //
            // We do this by scanning over the window's client
            // region hit-testing until we find a point that
            // corresponds to our link.  We start out by scanning
            // at a resolution of 10x10 pixels, starting from the
            // window's (1,1) coordinate, then reducing the resolution
            // if needed.  Thus the idea here is to scan quickly if
            // possible, but more thoroughly if absolutely necessary.
            //
            // Note:
            // I intentionally started scanning points starting from
            // (1,1) rather than from (0,0) just in case clicking the
            // border of a control is different than clicking inside
            // the border.
            //
            for (int Resolution = 10; Resolution > 0; --Resolution)
            {
                for (int x = 1; x <= width; x += Resolution)
                {
                    for (int y = 1; y <= height; y += Resolution)
                    {
                        //
                        // Send the link an LM_HITTEST message.
                        //
                        // Allocate a local hit test info struct.
                        UnsafeNativeMethods.LHITTESTINFO HitTestInfo = new UnsafeNativeMethods.LHITTESTINFO();

                        // Fill in the coordinates that we want to check.
                        HitTestInfo.pt.x = x;
                        HitTestInfo.pt.y = y;

                        // Fill in index and state info.
                        HitTestInfo.item.mask = NativeMethods.LIF_ITEMINDEX | NativeMethods.LIF_STATE;
                        HitTestInfo.item.iLink = 0;
                        HitTestInfo.item.stateMask = NativeMethods.LIS_ENABLED;
                        HitTestInfo.item.state = 0;

                        bool bGetItemResult;
                        unsafe
                        {
                            // Send the LM_HITTEST message.
                            bGetItemResult = XSendMessage.XSend(_hwnd, NativeMethods.LM_HITTEST, IntPtr.Zero, new IntPtr(&HitTestInfo), Marshal.SizeOf(HitTestInfo.GetType()));
                        }

                        if (bGetItemResult == true && HitTestInfo.item.iLink == _item)
                        {
                            //
                            // N.B. [SEdmison]:
                            // This multiplication is essentially just
                            // a left shift by one word's width; in
                            // Win32 I'd just use my trusty MAKELONG macro,
                            // but C# doesn't give me that option.
                            //
                            Misc.ProxySendMessage(_hwnd, NativeMethods.WM_LBUTTONDOWN, IntPtr.Zero, NativeMethods.Util.MAKELPARAM(x, y));
                            Misc.ProxySendMessage(_hwnd, NativeMethods.WM_LBUTTONUP, IntPtr.Zero, NativeMethods.Util.MAKELPARAM(x, y));
                            return;
                        }
                    }
                }
            }
         }

        #endregion
    }
}
