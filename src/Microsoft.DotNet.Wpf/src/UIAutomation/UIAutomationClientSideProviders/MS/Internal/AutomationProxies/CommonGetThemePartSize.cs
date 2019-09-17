// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Security;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;
using Microsoft.Win32.SafeHandles;
using MS.Win32;

namespace MS.Internal.AutomationProxies
{
    class ThemePart: IDisposable
    {
        // ------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------

        #region Constructors

        internal ThemePart (IntPtr hwnd, string sClass)
        {
            if (Environment.OSVersion.Version.Major >= 5)
            {
                _hTheme = new SafeThemeHandle(OpenThemeData(hwnd, sClass), false);
            }
            else
            {
                _hTheme = new SafeThemeHandle(IntPtr.Zero, false);
            }
        }

        public void Dispose ()
        {
            _hTheme.Dispose();
        }

        #endregion

        //------------------------------------------------------
        //
        //  Internal Methods
        //
        //------------------------------------------------------

        #region Internal Methods

        // Retreive the dimension of of UI element
        internal NativeMethods.SIZE Size (int iPartId, int iStateId)
        {
            bool fSuccess = false;
            // Default is (0, 0)
            NativeMethods.SIZE size = new NativeMethods.SIZE(0, 0);

            if (!_hTheme.IsInvalid)
            {
                unsafe
                {
                    fSuccess = GetThemePartSize(_hTheme, IntPtr.Zero, iPartId, iStateId, IntPtr.Zero, (int)THEMESIZE.TS_TRUE, &size) == IntPtr.Zero;
                }

            }

            // Falls back uses GetSystemMetrics
            if (!fSuccess)
            {
                // dangerous construct, the iPartId might collide. 
                // When entering an entry, make sure that the ID is not previously used.
                // If it is the case, then an extra parameter needs to be added to this method
                switch (iPartId)
                {
                    case (int) STATUSPARTS.SP_GRIPPER:
                        size.cx = UnsafeNativeMethods.GetSystemMetrics (NativeMethods.SM_CXSIZE);
                        size.cy = UnsafeNativeMethods.GetSystemMetrics (NativeMethods.SM_CXSIZE);
                        break;

                    case (int)SCROLLBARPARTS.SBP_SIZEBOX:
                        size.cx = UnsafeNativeMethods.GetSystemMetrics(NativeMethods.SM_CXVSCROLL);
                        size.cy = UnsafeNativeMethods.GetSystemMetrics(NativeMethods.SM_CYHSCROLL);
                        break;

                    case (int)SCROLLBARPARTS.SBP_ARROWBTN:
                        size.cx = UnsafeNativeMethods.GetSystemMetrics(NativeMethods.SM_CXHSCROLL);
                        size.cy = UnsafeNativeMethods.GetSystemMetrics(NativeMethods.SM_CYVSCROLL);
                        break;

                    case (int) WINDOWPARTS.WP_MINBUTTON :
                    case (int) WINDOWPARTS.WP_MAXBUTTON :
                    case (int) WINDOWPARTS.WP_CLOSEBUTTON :
                    case (int) WINDOWPARTS.WP_HELPBUTTON :
                        size.cx = UnsafeNativeMethods.GetSystemMetrics (NativeMethods.SM_CXSIZE);
                        size.cy = UnsafeNativeMethods.GetSystemMetrics (NativeMethods.SM_CXSIZE);
                        break;

                    default:
                        System.Diagnostics.Debug.Assert (false, "Unsupport Type");
                        break;
                }
            }

            return size;
        }

        #endregion

        //------------------------------------------------------
        //
        //  Internal Fields
        //
        //------------------------------------------------------

        #region Internal Fields

        internal enum WINDOWPARTS
        {
            WP_CAPTION = 1,
            WP_SMALLCAPTION = 2,
            WP_MINCAPTION = 3,
            WP_SMALLMINCAPTION = 4,
            WP_MAXCAPTION = 5,
            WP_SMALLMAXCAPTION = 6,
            WP_FRAMELEFT = 7,
            WP_FRAMERIGHT = 8,
            WP_FRAMEBOTTOM = 9,
            WP_SMALLFRAMELEFT = 10,
            WP_SMALLFRAMERIGHT = 11,
            WP_SMALLFRAMEBOTTOM = 12,
            WP_SYSBUTTON = 13,
            WP_MDISYSBUTTON = 14,
            WP_MINBUTTON = 15,
            WP_MDIMINBUTTON = 16,
            WP_MAXBUTTON = 17,
            WP_CLOSEBUTTON = 18,
            WP_SMALLCLOSEBUTTON = 19,
            WP_MDICLOSEBUTTON = 20,
            WP_RESTOREBUTTON = 21,
            WP_MDIRESTOREBUTTON = 22,
            WP_HELPBUTTON = 23,
            WP_MDIHELPBUTTON = 24,
            WP_HORZSCROLL = 25,
            WP_HORZTHUMB = 26,
            WP_VERTSCROLL = 27,
            WP_VERTTHUMB = 28,
            WP_DIALOG = 29,
            WP_CAPTIONSIZINGTEMPLATE = 30,
            WP_SMALLCAPTIONSIZINGTEMPLATE = 31,
            WP_FRAMELEFTSIZINGTEMPLATE = 32,
            WP_SMALLFRAMELEFTSIZINGTEMPLATE = 33,
            WP_FRAMERIGHTSIZINGTEMPLATE = 34,
            WP_SMALLFRAMERIGHTSIZINGTEMPLATE = 35,
            WP_FRAMEBOTTOMSIZINGTEMPLATE = 36,
            WP_SMALLFRAMEBOTTOMSIZINGTEMPLATE = 37,
        };
        //
        internal enum MINBUTTONSTATES
        {
            MINBS_NORMAL = 1,
            MINBS_HOT = 2,
            MINBS_PUSHED = 3,
            MINBS_DISABLED = 4,
        };

        internal enum SCROLLBARPARTS
        {
            SBP_ARROWBTN = 1,
            SBP_THUMBBTNHORZ = 2,
            SBP_THUMBBTNVERT = 3,
            SBP_LOWERTRACKHORZ = 4,
            SBP_UPPERTRACKHORZ = 5,
            SBP_LOWERTRACKVERT = 6,
            SBP_UPPERTRACKVERT = 7,
            SBP_GRIPPERHORZ = 8,
            SBP_GRIPPERVERT = 9,
            SBP_SIZEBOX = 10,
        };

        internal enum STATUSPARTS
        {
            SP_PANE = 1,
            SP_GRIPPERPANE = 2,
            SP_GRIPPER = 3,
        };

        enum THEMESIZE
        {
            TS_MIN,             // minimum size
            TS_TRUE,            // size without stretching
            TS_DRAW,            // size that theme mgr will use to draw part
        };

        #endregion

        //------------------------------------------------------
        //
        //  Private Methods
        //
        //------------------------------------------------------

        #region Private Methods

        [DllImport ("UxTheme.dll", CharSet = CharSet.Auto)]
        private static unsafe extern IntPtr GetThemePartSize(SafeThemeHandle hTheme, IntPtr hdc, int iPartId, int iStateId, IntPtr prc, int eSize, NativeMethods.SIZE* psz);

        [DllImport ("UxTheme.dll", CharSet = CharSet.Auto)]
        private static extern IntPtr OpenThemeData(IntPtr hwnd, [MarshalAs(UnmanagedType.LPWStr)]string s);

        #endregion

        //------------------------------------------------------
        //
        //  Private Fields
        //
        //------------------------------------------------------

        #region Private Fields

        // cached Value. Will never be initialize elsewhere if OS == Win98
        private SafeThemeHandle _hTheme;

        #endregion

    }
}
