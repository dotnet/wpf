// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// Description: HWND-based Alt-Tab (Task Switch) Window Proxy

// PRESHARP: In order to avoid generating warnings about unknown message numbers and unknown pragmas.
#pragma warning disable 1634, 1691

using System;
using System.Collections;
using System.Globalization;
using System.Text;
using System.ComponentModel;
using Accessibility;
using System.Windows.Automation.Provider;
using System.Windows.Automation;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using MS.Win32;

namespace MS.Internal.AutomationProxies
{
    // This class represents the Alt-Tab (task switch) window.
    class WindowsAltTab : ProxyHwnd
    {
        // ------------------------------------------------------
        //
        // Constructors
        //
        // ------------------------------------------------------

        #region Constructors

        internal WindowsAltTab(IntPtr hwnd, ProxyFragment parent, int item)
            : base(hwnd, parent, item)
        {
            _fIsKeyboardFocusable = true;
            _cControlType = ControlType.List;

            // support for events
            _createOnEvent = new WinEventTracker.ProxyRaiseEvents(RaiseEvents);

            GetAltTabInfo(_hwnd, 0, ref _altTabInfo, null);
        }

        #endregion

        #region Proxy Create

        // Static Create method called by UIAutomation to create this proxy.
        // returns null if unsuccessful
        internal static IRawElementProviderSimple Create(IntPtr hwnd, int idChild, int idObject)
        {
            return Create(hwnd, idChild);
        }

        internal static IRawElementProviderSimple Create(IntPtr hwnd, int idChild)
        {
            IRawElementProviderSimple rawElementProviderSimple = null;
            try
            {
                ProxyFragment parent = null;

                WindowsAltTab altTab = new WindowsAltTab(hwnd, parent, 0);
                if (idChild == 0)
                {
                    rawElementProviderSimple = altTab;
                }
                else
                {
                    rawElementProviderSimple = altTab.CreateAltTabItem(idChild - 1);
                }
            }
            catch (ElementNotAvailableException)
            {
                rawElementProviderSimple = null;
            }

            return rawElementProviderSimple;
        }

        // Static Create method called by the event tracker system
        internal static void RaiseEvents (IntPtr hwnd, int eventId, object idProp, int idObject, int idChild)
        {
            if (idObject != NativeMethods.OBJID_VSCROLL && idObject != NativeMethods.OBJID_HSCROLL)
            {
                ProxySimple el = (ProxyHwnd) WindowsAltTab.Create(hwnd, 0);
                if (el != null)
                {
                    el.DispatchEvents(eventId, idProp, idObject, idChild);
                }
            }
        }

        #endregion

        //------------------------------------------------------
        //
        //  ProxyFragment Overrides
        //
        //------------------------------------------------------

        #region ProxyFragment Overrides

        // Returns the next sibling element in the raw hierarchy.
        internal override ProxySimple GetNextSibling(ProxySimple child)
        {
            return CreateAltTabItem(child._item + 1);
        }

        // Returns the previous sibling element in the raw hierarchy.
        internal override ProxySimple GetPreviousSibling(ProxySimple child)
        {
            return CreateAltTabItem(child._item - 1);
        }

        // Returns the first child element in the raw hierarchy.
        internal override ProxySimple GetFirstChild()
        {
            return CreateAltTabItem(0);
        }

        // Returns the last child element in the raw hierarchy.
        internal override ProxySimple GetLastChild()
        {
            return CreateAltTabItem(Count - 1);
        }

        // Returns a Proxy element corresponding to the specified screen coordinates.
        internal override ProxySimple ElementProviderFromPoint(int x, int y)
        {
            ProxySimple proxyElement = null;

            // Convert screen to client coords.
            NativeMethods.Win32Point pt = new NativeMethods.Win32Point(x, y);
            if (Misc.MapWindowPoints(System.IntPtr.Zero, _hwnd, ref pt, 1))
            {
                // GetClientRect
                NativeMethods.Win32Rect clientRect = new NativeMethods.Win32Rect();
                if(Misc.GetClientRect(_hwnd, ref clientRect))
                {
                    if (Misc.PtInRect(ref clientRect, pt.x, pt.y))
                    {
                        int column = (pt.x - _altTabInfo.ptStart.x) / _altTabInfo.cxItem;
                        int row = (pt.y - _altTabInfo.ptStart.y) / _altTabInfo.cyItem;
                        if (column >= 0 && column < Columns && row >= 0 && row < Rows)
                        {
                            proxyElement = CreateAltTabItem(ItemIndex(row, column));
                        }
                    }
                }
            }
            if (proxyElement == null)
            {
                proxyElement = base.ElementProviderFromPoint(x, y);
            }


            return proxyElement;
        }

        // Returns an item corresponding to the focused element (if there is one), 
        // or null otherwise.
        internal override ProxySimple GetFocus()
        {
            ProxySimple focus = this;
            int focusIndex = FocusIndex;
            if (focusIndex >= 0)
            {
                focus = CreateAltTabItem(focusIndex);
            }
            return focus;
        }

        #endregion


        // ------------------------------------------------------
        //
        // Protected Methods
        //
        // ------------------------------------------------------

        #region Protected Methods
        #endregion

        //------------------------------------------------------
        //
        //  Private Methods
        //
        //------------------------------------------------------

        #region Private Methods

        // Creates an alt-tab item.
        private ProxySimple CreateAltTabItem(int item)
        {
            ProxySimple altTabItem = null;
            if (item >= 0 && item < this.Count)
            {
                altTabItem = new WindowsAltTabItem(_hwnd, this, item);
            }
            return altTabItem;
        }

        // Get the 0-based index of an item from its row and column.
        private int ItemIndex(int row, int column)
        {
            return row * Columns + column;
        }

        // Get the index of the currently focused item (-1 if none).
        private int FocusIndex
        {
            get
            {
                int focusItem = -1;
                UnsafeNativeMethods.ALTTABINFO altTabInfo =
                    new UnsafeNativeMethods.ALTTABINFO();
                if (GetAltTabInfo(_hwnd, -1, ref altTabInfo, null))
                {
                    focusItem =
                        altTabInfo.iRowFocus * altTabInfo.cColumns + altTabInfo.iColFocus;
                }
                return focusItem;
            }
        }

        #endregion

        //------------------------------------------------------
        //
        //  Internal Methods
        //
        //------------------------------------------------------

        #region Internal Methods

        // To get itemText, set item to the item index (or -1 otherwise).
        // Returns true for success.
        internal static bool
            GetAltTabInfo(IntPtr hwnd, int item,
                            ref UnsafeNativeMethods.ALTTABINFO altTabInfo,
                            StringBuilder itemText)
        {
            altTabInfo.cbSize = (uint)Marshal.SizeOf(typeof(UnsafeNativeMethods.ALTTABINFO));
            uint cchItemText = 0;
            if (itemText != null)
            {
                cchItemText = (uint)itemText.Capacity;
            }
            bool result =
                UnsafeNativeMethods.GetAltTabInfo(
                    hwnd, item, ref altTabInfo, itemText, (uint)cchItemText);

            int lastWin32Error = Marshal.GetLastWin32Error();
            if (!result)
            {
                Misc.ThrowWin32ExceptionsIfError(lastWin32Error);
            }
            return result;
        }

        #endregion

        //------------------------------------------------------
        //
        //  Private Fields
        //
        //------------------------------------------------------

        #region Private Fields

        private const int MaxItemNameLength = 1000;

        // Note this is only used for "stable" state;
        // iRow/ColFocus should always be fetched "fresh".
        private UnsafeNativeMethods.ALTTABINFO _altTabInfo =
            new UnsafeNativeMethods.ALTTABINFO();

        private int Rows
        {
            get
            {
                return _altTabInfo.cRows;
            }
        }

        private int Columns
        {
            get
            {
                return _altTabInfo.cColumns;
            }
        }

        private int Count
        {
            get
            {
                return _altTabInfo.cItems;
            }
        }

        #endregion

        // ------------------------------------------------------
        //
        //  WindowsAltTabItem Private Class
        //
        //------------------------------------------------------

        #region WindowsAltTabItem

        // Proxy class for an entry in the Alt-Tab window, representing
        // a single running program.
        class WindowsAltTabItem : ProxySimple
        {
            // ------------------------------------------------------
            //
            // Constructors
            //
            // ------------------------------------------------------

            #region Constructors

            // Constructor.
            internal WindowsAltTabItem(IntPtr hwnd, WindowsAltTab parent, int item)
                : base(hwnd, parent, item)
            {
                _cControlType = ControlType.ListItem;
                _fIsKeyboardFocusable = true;
                _altTab = parent;
            }

            #endregion

            //------------------------------------------------------
            //
            //  Patterns Implementation
            //
            //------------------------------------------------------

            #region ProxySimple Interface

            // Gets the bounding rectangle for this element.
            internal override Rect BoundingRectangle
            {
                get
                {
                    return GetBoundingRect().ToRect(Misc.IsControlRTL(_hwnd));
                }
            }

            // Returns the text of the alt-tab item.
            internal override string LocalizedName
            {
                get
                {
                    UnsafeNativeMethods.ALTTABINFO altTabInfo =
                        new UnsafeNativeMethods.ALTTABINFO();
                    String localizedName = String.Empty;
                    StringBuilder itemText = new StringBuilder(WindowsAltTab.MaxItemNameLength);
                    if (WindowsAltTab.GetAltTabInfo(_hwnd, _item, ref altTabInfo, itemText))
                    {
                        localizedName = itemText.ToString();
                    }
                    return localizedName;
                }
            }

            #endregion

            //------------------------------------------------------
            //
            //  Private Methods
            //
            //------------------------------------------------------

            #region Private Methods

            private NativeMethods.Win32Rect GetBoundingRect()
            {
                int columns = this._altTab._altTabInfo.cColumns;
                int cxItem = this._altTab._altTabInfo.cxItem;
                int cyItem = this._altTab._altTabInfo.cyItem;
                int row = _item / columns;
                int column = _item % columns;
                NativeMethods.Win32Point ptStart = this._altTab._altTabInfo.ptStart;

                int left = ptStart.x + column * cxItem;
                int top = ptStart.y + row * cyItem;
                NativeMethods.Win32Rect itemRect =
                    new NativeMethods.Win32Rect(left, top, left + cxItem, top + cyItem);

                if(!Misc.MapWindowPoints(_hwnd, IntPtr.Zero, ref itemRect, 2))
                {
                    // MapWindowPoints() failed.
                    itemRect = NativeMethods.Win32Rect.Empty;
                }
                return itemRect;
            }

            #endregion

            //------------------------------------------------------
            //
            //  Private Fields
            //
            //------------------------------------------------------
            #region Private Fields

            private WindowsAltTab _altTab;

            #endregion
        }

        #endregion
    }
}
