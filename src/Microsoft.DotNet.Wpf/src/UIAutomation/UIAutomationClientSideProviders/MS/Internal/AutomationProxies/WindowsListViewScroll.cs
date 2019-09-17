// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// Description: Class to provide scroll bars for listview
//


using System;
using System.Runtime.InteropServices;
using System.ComponentModel;
using System.Windows.Automation;
using System.Windows.Automation.Provider;
using System.Windows;
using MS.Win32;

namespace MS.Internal.AutomationProxies
{

        // The default implementation for scroll bars uses SB_THUMBTRACK and SB_THUMBPOSITION for SetValue
        // This does not work with listview so the Scrollbar is overloaded with a derived version that
        // uses LVM_SCROLL messages instead
        class WindowsListViewScrollBar: WindowsScrollBar, IRangeValueProvider
        {

            // ------------------------------------------------------
            //
            //  Constructors
            //
            //------------------------------------------------------

            #region Constructors

            internal WindowsListViewScrollBar(IntPtr hwnd, ProxyFragment parent, int item, int sbFlag)
                : base( hwnd, parent, item, sbFlag){}

            #endregion Constructors

            //------------------------------------------------------
            //
            //  Patterns Implementation
            //
            //------------------------------------------------------

            #region RangeValue Pattern

            void IRangeValueProvider.SetValue(double val)
            {
                // Check if the window is disabled
                if (!SafeNativeMethods.IsWindowEnabled (_hwnd))
                {
                    throw new ElementNotEnabledException();
                }

                NativeMethods.ScrollInfo si = new NativeMethods.ScrollInfo ();
                si.fMask = NativeMethods.SIF_ALL;
                si.cbSize = Marshal.SizeOf (si.GetType ());

                if (!Misc.GetScrollInfo(_hwnd, _sbFlag, ref si))
                {
                    return;
                }

                int pos = (int)val;
                // Throw if val is greater than the maximum or less than the minimum.
                // See remarks for WindowsScrollBar.GetScrollValue(ScrollBarInfo.MaximumPosition)
                // regarding this calculation of the allowed maximum.
                if (pos > si.nMax - si.nPage + (si.nPage > 0 ? 1 : 0))
                {
                    throw new ArgumentOutOfRangeException("value", val, SR.Get(SRID.RangeValueMax));
                }
                else if (pos < si.nMin)
                {
                    throw new ArgumentOutOfRangeException("value", val, SR.Get(SRID.RangeValueMin));
                }

                // LVM_SCROLL does not work in mode Report, use SetScrollPos instead
                bool isVerticalScroll = IsScrollBarVertical(_hwnd, _sbFlag);
                if (isVerticalScroll && WindowsListView.InReportView (_hwnd))
                {
                    Misc.SetScrollPos(_hwnd, _sbFlag, pos, true);
                    return;
                }

                // get the "full size" of the list-view
                int size = WindowsListView.ApproximateViewRect (_hwnd);

                // delta between current and user-requested position in pixels
                // since the cPelsAll contains the dimension in pels for all items + the 2 pels of the border
                // the operation below does a trunc on purpose
                int dx = 0, dy = 0;
                if (!isVerticalScroll)
                {
                    int cPelsAll = NativeMethods.Util.LOWORD (size);

                    dx = (int)((pos - si.nPos) * ((double)cPelsAll / (si.nMax + 1 - si.nMin)));
                }
                else
                {
                    int cPelsAll = NativeMethods.Util.HIWORD (size);

                    dy = (int)((pos - si.nPos) * ((double)cPelsAll / (si.nMax + 1 - si.nMin)));
                }

                if (WindowsListView.Scroll (_hwnd, (IntPtr) dx, (IntPtr) dy))
                {
                    // Check the result again, on occasion the result will be different than the value
                    // we previously got. It's unsure why this is, but for now we'll just reissue the
                    // scroll command with the new delta.
                    if (!Misc.GetScrollInfo(_hwnd, _sbFlag, ref si))
                    {
                        return;
                    }

                    if (si.nPos != pos)
                    {
                        if (!isVerticalScroll)
                        {
                            int cPelsAll = NativeMethods.Util.LOWORD (size);

                            dx = (pos - si.nPos) * (cPelsAll / (si.nMax + 1 - si.nMin));
                        }
                        else
                        {
                            int cPelsAll = NativeMethods.Util.HIWORD (size);

                            dy = (pos - si.nPos) * (cPelsAll / (si.nMax + 1 - si.nMin));
                        }

                        WindowsListView.Scroll (_hwnd, (IntPtr) dx, (IntPtr) dy);
                    }
                }
            }

            #endregion Value Pattern
    }
}
