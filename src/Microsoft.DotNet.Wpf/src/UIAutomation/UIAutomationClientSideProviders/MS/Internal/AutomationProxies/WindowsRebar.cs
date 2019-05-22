// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// Description: HWND-based Rebar Proxy

using System;
using System.Collections;
using System.Text;
using System.Runtime.InteropServices;
using System.ComponentModel;
using System.Windows.Automation;
using System.Windows.Automation.Provider;
using System.Windows;
using MS.Win32;

namespace MS.Internal.AutomationProxies
{
    class WindowsRebar: ProxyHwnd, IRawElementProviderHwndOverride
    {
        // ------------------------------------------------------
        //
        // Constructors
        //
        // ------------------------------------------------------

        #region Constructors

        WindowsRebar (IntPtr hwnd, ProxyFragment parent, int item)
            : base( hwnd, parent, item )
        {
            _sType = SR.Get(SRID.LocalizedControlTypeRebar);
            _fIsContent = false;

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

            return new WindowsRebar(hwnd, null, idChild);
        }

        // Static Create method called by the event tracker system
        internal static void RaiseEvents (IntPtr hwnd, int eventId, object idProp, int idObject, int idChild)
        {
            if (idObject != NativeMethods.OBJID_VSCROLL && idObject != NativeMethods.OBJID_HSCROLL)
            {
                WindowsRebar wtv = new WindowsRebar (hwnd, null, -1);
                wtv.DispatchEvents (eventId, idProp, idObject, idChild);
            }
        }

        #endregion

        //------------------------------------------------------
        //
        //  Patterns Implementation
        //
        //------------------------------------------------------

        #region ProxyFragment Interface

        // Returns the next sibling element in the raw hierarchy.
        // Peripheral controls have always negative values.
        // Returns null if no next child
        internal override ProxySimple GetNextSibling (ProxySimple child)
        {
            int item = child._item;

            // If the index of the next node would be out of range...
            if (item >= 0 && (item + 1) < Count)
            {
                // return a node to represent the requested item.
                return CreateRebarItem (item + 1);
            }
            return null;
        }

        // Returns the previous sibling element in the raw hierarchy.
        // Peripheral controls have always negative values.
        // Returns null is no previous
        internal override ProxySimple GetPreviousSibling (ProxySimple child)
        {
            // If the index of the previous node would be out of range...
            int item = child._item;
            if (item > 0 && item < Count)
            {
                return CreateRebarItem (item - 1);
            }
            return null;
        }

        // Returns the first child element in the raw hierarchy.
        internal override ProxySimple GetFirstChild ()
        {
            return Count > 0 ? CreateRebarItem (0) : null;
        }

        // Returns the last child element in the raw hierarchy.
        internal override ProxySimple GetLastChild ()
        {
            int count = Count;
            return count > 0 ? CreateRebarItem (count - 1) : null;
        }

        // Returns a Proxy element corresponding to the specified screen coordinates.
        internal override ProxySimple ElementProviderFromPoint (int x, int y)
        {
            int x1 = x;
            int y1 = y;
            NativeMethods.Win32Rect rebarRect = new NativeMethods.Win32Rect ();
            if (!Misc.GetWindowRect(_hwnd, ref rebarRect))
            {
                return null;
            }

            if (x >= rebarRect.left && x <= rebarRect.right &&
            y >= rebarRect.top && y <= rebarRect.bottom)
            {
                x = x - rebarRect.left;
                y = y - rebarRect.top;

                NativeMethods.Win32Point pt = new NativeMethods.Win32Point (x, y);

                int BandID = getRebarBandIDFromPoint (pt);

                if (-1 != BandID)
                {
                    return CreateRebarItem (BandID).ElementProviderFromPoint (x1, y1);
                }

            }
            return null;
        }

        #endregion

        #region IRawElementProviderHwndOverride Interface 

        //------------------------------------------------------
        //
        //  Interface IRawElementProviderHwndOverride
        //
        //------------------------------------------------------
        IRawElementProviderSimple IRawElementProviderHwndOverride.GetOverrideProviderForHwnd (IntPtr hwnd)
        {
            // return the appropriate placeholder for the given hwnd...
            // loop over all the band to find it.
        
            for (RebarBandItem band = (RebarBandItem) GetFirstChild (); band != null; band = (RebarBandItem) GetNextSibling (band))
            {
                if (band.HwndBand == hwnd)
                {
                    return new RebarBandChildOverrideProxy (hwnd, band, band._item);
                }
            }

            // Should never get here
            return null;
        }

        #endregion IRawElementProviderHwndOverride Interface 

        // ------------------------------------------------------
        //
        // Private Methods
        //
        // ------------------------------------------------------        

        #region Private Methods

        // Creates a list item RawElementBase Item
        private RebarBandItem CreateRebarItem (int index)
        {
            return new RebarBandItem (_hwnd, this, index);
        }

        private int Count
        {
            get
            {
                return Misc.ProxySendMessageInt(_hwnd, NativeMethods.RB_GETBANDCOUNT, IntPtr.Zero, IntPtr.Zero);
            }
        }

        private unsafe int getRebarBandIDFromPoint (NativeMethods.Win32Point pt)
        {
            NativeMethods.RB_HITTESTINFO rbHitTestInfo = new NativeMethods.RB_HITTESTINFO ();
            rbHitTestInfo.pt = pt;
            rbHitTestInfo.uFlags = 0;
            rbHitTestInfo.iBand = 0;

            return XSendMessage.XSendGetIndex(_hwnd, NativeMethods.RB_HITTEST, IntPtr.Zero, new IntPtr(&rbHitTestInfo), Marshal.SizeOf(rbHitTestInfo.GetType()));
        }

        #endregion

        //------------------------------------------------------
        //
        //  Private Fields
        //
        //------------------------------------------------------

        #region Private Fields

        private enum CommonControlStyles
        {
//                CCS_TOP = 0x00000001,
//                CCS_NOMOVEY = 0x00000002,
//                CCS_BOTTOM = 0x00000003,
//                CCS_NORESIZE = 0x00000004,
//                CCS_NOPARENTALIGN = 0x00000008,
//                CCS_ADJUSTABLE = 0x00000020,
//                CCS_NODIVIDER = 0x00000040,
            CCS_VERT = 0x00000080,
//                CCS_LEFT = (CCS_VERT | CCS_TOP),
//                CCS_RIGHT = (CCS_VERT | CCS_BOTTOM),
//                CCS_NOMOVEX = (CCS_VERT | CCS_NOMOVEY)
        }

        private const int RBBIM_CHILD = 0x10;

        #endregion

        // ------------------------------------------------------
        //
        //  RebarBandItem Private Class
        //
        //------------------------------------------------------

        #region RebarBandItem

        class RebarBandItem: ProxyFragment, IInvokeProvider
        {
            // ------------------------------------------------------
            //
            // Constructors
            //
            // ------------------------------------------------------

            #region Constructors

            internal RebarBandItem (IntPtr hwnd, ProxyFragment parent, int item)
            : base (hwnd, parent, item)
            {
                // Set the strings to return properly the properties.
                _sType = SR.Get(SRID.LocalizedControlTypeRebarBand);
                _fIsContent = false;
            }

            #endregion

            //------------------------------------------------------
            //
            //  Patterns Implementation
            //
            //------------------------------------------------------

            #region ProxySimple

            // Gets the bounding rectangle for this element
            internal override Rect BoundingRectangle
            {
                get
                {
                    return GetBoundingRectangle (_hwnd, _item);
                }
            }

            //Gets the controls help text
            internal override string HelpText
            {
                get
                {
                    IntPtr hwndToolTip = Misc.ProxySendMessage(_hwnd, NativeMethods.RB_GETTOOLTIPS, IntPtr.Zero, IntPtr.Zero);
                    return Misc.GetItemToolTipText(_hwnd, hwndToolTip, _item);
                }
            }

            //Gets the localized name
            internal override string LocalizedName
            {
                get
                {
                    return SR.Get(SRID.LocalizedNameWindowsReBarBandItem);
                }
            }

            #endregion

            #region ProxyFragment Interface

            // Returns the next sibling element in the raw hierarchy.
            // Peripheral controls have always negative values.
            // Returns null if no next child
            internal override ProxySimple GetNextSibling (ProxySimple child)
            {
                return null;
            }

            // Returns the previous sibling element in the raw hierarchy.
            // Peripheral controls have always negative values.
            // Returns null is no previous
            internal override ProxySimple GetPreviousSibling (ProxySimple child)
            {
                return null;
            }

            // Returns the first child element in the raw hierarchy.
            internal override ProxySimple GetFirstChild ()
            {
                IntPtr hwndBand = HwndBand;

                if (hwndBand != IntPtr.Zero)
                {
                    return new RebarBandChildOverrideProxy (HwndBand, this, _item);
                }
                return null;
            }

            // Returns the last child element in the raw hierarchy.
            internal override ProxySimple GetLastChild ()
            {
                // By construction, a rebar band can only have one children
                return GetFirstChild ();
            }

            // Returns a Proxy element corresponding to the specified screen coordinates.
            internal override ProxySimple ElementProviderFromPoint (int x, int y)
            {
                IntPtr hwndBand = HwndBand;

                if (hwndBand != IntPtr.Zero && Misc.PtInWindowRect(hwndBand, x, y))
                {
                    return null;
                }

                return this;
            }

            internal override object GetElementProperty(AutomationProperty idProp)
            {
                if (idProp == AutomationElement.IsControlElementProperty)
                {
                    //
                    // The Rebar band should not be in the control view.
                    // 
                    return false;
                }

                return base.GetElementProperty(idProp);
            }

            #endregion

            #region InvokeInteropPattern

            void IInvokeProvider.Invoke ()
            {
                // Make sure that the control is enabled
                if (!SafeNativeMethods.IsWindowEnabled(_hwnd))
                {
                    throw new ElementNotEnabledException();
                }

                Misc.PostMessage(_hwnd, NativeMethods.RB_PUSHCHEVRON, (IntPtr)_item, IntPtr.Zero);
            }

            #endregion

            //------------------------------------------------------
            //
            //  Internal Methods
            //
            //------------------------------------------------------

            #region Internal Methods

            // Returns the bounding rectangle of the control.
            internal static Rect GetBoundingRectangle (IntPtr hwnd, int item)
            {
                NativeMethods.Win32Rect rectW32 = NativeMethods.Win32Rect.Empty;

                unsafe
                {
                    if (!XSendMessage.XSend(hwnd, NativeMethods.RB_GETRECT, new IntPtr(item), new IntPtr(&rectW32), Marshal.SizeOf(rectW32.GetType()), XSendMessage.ErrorValue.Zero))
                    {
                        return Rect.Empty;
                    }
                }

                if (!Misc.MapWindowPoints(hwnd, IntPtr.Zero, ref rectW32, 2))
                {
                    return Rect.Empty;
                }

                // Work around a bug in the common control. Swap the X and Y value for vertical
                // rebar bands 
                if (Misc.IsBitSet(Misc.GetWindowStyle(hwnd), (int)CommonControlStyles.CCS_VERT))
                {
                    return new Rect (rectW32.left, rectW32.top, rectW32.bottom - rectW32.top, rectW32.right - rectW32.left);
                }
                else
                {
                    return rectW32.ToRect(Misc.IsControlRTL(hwnd));
                }
            }

            internal IntPtr HwndBand
            {
                get
                {
                    if (_hwndBand == IntPtr.Zero)
                    {
                        NativeMethods.REBARBANDINFO rebarBandInfo = new NativeMethods.REBARBANDINFO();
                        rebarBandInfo.fMask = RBBIM_CHILD;

                        unsafe
                        {
                            if (XSendMessage.XSend(_hwnd, NativeMethods.RB_GETBANDINFOA, new IntPtr(_item), new IntPtr(&rebarBandInfo), Marshal.SizeOf(rebarBandInfo.GetType()), XSendMessage.ErrorValue.Zero))
                            {
                                _hwndBand = rebarBandInfo.hwndChild;
                            }
                        }
                    }
                    return _hwndBand;
                }
            }

            #endregion

            //------------------------------------------------------
            //
            //  Private Fields
            //
            //------------------------------------------------------

            #region Private Fields

            IntPtr _hwndBand = IntPtr.Zero;

            #endregion

        }

        #endregion

        // ------------------------------------------------------
        //
        //  RebarBandChildOverrideProxy Private Class
        //
        //------------------------------------------------------

        #region RebarBandChildOverrideProxy

        class RebarBandChildOverrideProxy: ProxyHwnd
        {
            // ------------------------------------------------------
            //
            // Constructors
            //
            // ------------------------------------------------------

            #region Constructors

            internal RebarBandChildOverrideProxy (IntPtr hwnd, ProxyFragment parent, int item)
            : base (hwnd, parent, item)
            {
                _fIsContent = false;
            }

            #endregion

            //------------------------------------------------------
            //
            //  Patterns Implementation
            //
            //------------------------------------------------------

            #region ProxySimple Interface

            internal override ProviderOptions ProviderOptions
            {
                get
                {
                    return base.ProviderOptions | ProviderOptions.OverrideProvider;
                }
            }

            // Process all the Logical and Raw Element Properties
            internal override object GetElementProperty (AutomationProperty idProp)
            {
                if (idProp == AutomationElement.IsControlElementProperty)
                {
                    //
                    // The panes under the rebar band should not be in the control view.
                    //

                    // In IE6, the rebar band HWND tree was only one level deep:
                    //
                    // rebar / rebar band / rebar item (Toolbar32)
                    //
                    // In IE7, the HWND tree is the same but the rebar item is 
                    // a window acting as another container, for instance:
                    //
                    // rebar / rebar band / rebar item (FavBandClass) / children (Toolbar32)
                    //

                    // Hide windows that are intermediate containers from the control view
                    Accessible accThis = Accessible.Wrap(this.AccessibleObject);
                    if ((accThis != null) && (accThis.ChildCount == 1))
                    {
                        Accessible accWind = accThis.FirstChild;
                        if ((accWind != null) && (accWind.Role == AccessibleRole.Window))
                        {
                            return false;
                        }
                    }
                }

                // No property should be handled by the override proxy
                // Overrides the ProxySimple implementation.
                return null;
            }

            #endregion
        }

        #endregion
    }
}
