// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//***************************************************************************
// DO NOT adjust the visibility of anything in these files.  They are marked
// internal on purpose.
//***************************************************************************   

namespace MS.Win32
{
    using System;

    internal static class NativeMethods
    {
        public static IntPtr HWND_TOP = IntPtr.Zero;

        public const int LOGPIXELSX = 88;
        public const int LOGPIXELSY = 90;

        public const int SWP_NOSIZE = 1;
        public const int SWP_NOMOVE = 2;

        public const int UIS_SET = 1;
        public const int UIS_INITIALIZE = 3;
        public const int UISF_HIDEFOCUS = 0x1;
        public const int UISF_HIDEACCEL = 0x2;

        public const int WM_CREATE = 0x0001;
        public const int WM_MOVE = 0x0003;
        public const int WM_SIZE = 0x0005;
        public const int WM_SETFOCUS = 0x0007;
        public const int WM_KILLFOCUS = 0x0008;
        public const int WM_SETREDRAW = 0x000B;
        public const int WM_WINDOWPOSCHANGING = 0x0046;
        public const int WM_WINDOWPOSCHANGED = 0x0047;
        public const int WM_ACTIVATEAPP = 0x001C;
        public const int WM_MOUSEACTIVATE = 0x0021;
        public const int WM_CHILDACTIVATE = 0x0022;
        public const int WM_GETOBJECT = 0x003D;

        public const int WM_KEYDOWN = 0x0100;
        public const int WM_KEYUP = 0x0101;
        public const int WM_CHAR = 0x0102;
        public const int WM_DEADCHAR = 0x0103;
        public const int WM_SYSKEYDOWN = 0x0104;
        public const int WM_SYSKEYUP = 0x0105;
        public const int WM_SYSCHAR = 0x0106;
        public const int WM_SYSDEADCHAR = 0x0107;
        public const int WM_UPDATEUISTATE = 0x0128;

        public const int WM_PARENTNOTIFY = 0x0210;
        public const int WM_USER = 0x0400;
        public const int WM_REFLECT = WM_USER + 0x1C00;

        public const int WM_INPUTLANGCHANGE = 0x0051;
        public const int WM_IME_NOTIFY = 0x0282;
        public const int IMN_SETCONVERSIONMODE = 0x0006;
        public const int IMN_SETOPENSTATUS     = 0x0008;
    }
}
