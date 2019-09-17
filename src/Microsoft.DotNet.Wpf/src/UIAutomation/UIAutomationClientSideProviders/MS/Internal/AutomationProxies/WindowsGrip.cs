// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// Description: Windows Button Proxy

using System;
using System.Collections;
using System.Text;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Automation.Provider;
using System.ComponentModel;
using MS.Win32;


namespace MS.Internal.AutomationProxies
{
    class WindowsGrip: ProxyFragment
    {
        // ------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------

        #region Constructors

        public WindowsGrip (IntPtr hwnd, ProxyHwnd parent, int item)
            : base( hwnd, parent, item)
        {
            _sType = SR.Get(SRID.LocalizedControlTypeGrip);
            _sAutomationId = "Window.Grip"; // This string is a non-localizable string
        }

        #endregion

        //------------------------------------------------------
        //
        //  Patterns Implementation
        //
        //------------------------------------------------------

        #region ProxySimple Interface

        /// <summary>
        /// Gets the bounding rectangle for this element
        /// </summary>
        internal override Rect BoundingRectangle
        {
            get
            {
                if (IsGripPresent(_hwnd, false))
                {
                    NativeMethods.Win32Rect client = new NativeMethods.Win32Rect();
                    if (Misc.GetClientRectInScreenCoordinates(_hwnd, ref client))
                    {
                        NativeMethods.SIZE sizeGrip = GetGripSize(_hwnd, false);

                        if (Misc.IsLayoutRTL(_hwnd))
                        {
                            return new Rect(client.left - sizeGrip.cx, client.bottom, sizeGrip.cx, sizeGrip.cy);
                        }
                        else
                        {
                            return new Rect(client.right, client.bottom, sizeGrip.cx, sizeGrip.cy);
                        }
                    }
                }

                return Rect.Empty;
            }
        }

        #endregion


        //------------------------------------------------------
        //
        //  Internal Methods
        //
        //------------------------------------------------------

        #region Internal Methods

        static internal bool IsGripPresent(IntPtr hwnd, bool onStatusBar)
        {
            NativeMethods.Win32Rect client = new NativeMethods.Win32Rect();
            if (!Misc.GetClientRectInScreenCoordinates(hwnd, ref client))
            {
                return false;
            }

            // According to the documentation of GetClientRect, the left and top members are zero.  So if
            // they are negitive the control must be minimized, therefore the grip is not present.
            if (client.left < 0 && client.top < 0 )
            {
                return false;
            }

            NativeMethods.SIZE sizeGrip = GetGripSize(hwnd, onStatusBar);
            if (!onStatusBar)
            {
                // When not on a status bar the grip should be out side of the client area.
                sizeGrip.cx *= -1;
                sizeGrip.cy *= -1;
            }

            if (Misc.IsLayoutRTL(hwnd))
            {
                int x = client.left + (int)(sizeGrip.cx / 2);
                int y = client.bottom - (int)(sizeGrip.cy / 2);
                int hit = Misc.ProxySendMessageInt(hwnd, NativeMethods.WM_NCHITTEST, IntPtr.Zero, NativeMethods.Util.MAKELPARAM(x, y));

                return hit == NativeMethods.HTBOTTOMLEFT;
            }
            else
            {
                int x = client.right - (int)(sizeGrip.cx / 2);
                int y = client.bottom - (int)(sizeGrip.cy / 2);
                int hit = Misc.ProxySendMessageInt(hwnd, NativeMethods.WM_NCHITTEST, IntPtr.Zero, NativeMethods.Util.MAKELPARAM(x, y));

                return hit == NativeMethods.HTBOTTOMRIGHT;
            }
        }

        internal static NativeMethods.SIZE GetGripSize(IntPtr hwnd, bool onStatusBar)
        {
            using (ThemePart themePart = new ThemePart(hwnd, onStatusBar ? "STATUS" : "SCROLLBAR"))
            {
                return themePart.Size(onStatusBar ? (int)ThemePart.STATUSPARTS.SP_GRIPPER : (int)ThemePart.SCROLLBARPARTS.SBP_SIZEBOX, 0);
            }
        }

        #endregion
    }
}
