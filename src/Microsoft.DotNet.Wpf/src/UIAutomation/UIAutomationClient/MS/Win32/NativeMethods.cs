// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//***************************************************************************
// HOW TO USE THIS FILE
//
// If you need access to a Win32 API that is not exposed, simply uncomment
// it in one of the following files:
// 
//
// Only uncomment what you need to avoid code bloat.
//
// DO NOT adjust the visibility of anything in these files.  They are marked
// internal on pupose.
//***************************************************************************

using System;
using System.Runtime.InteropServices;

namespace MS.Win32
{
    internal static class NativeMethods
    {
        public const int MAX_PATH   = 260;

        // Dialog Codes
        internal const int WM_GETDLGCODE = 0x0087;
        internal const int DLGC_STATIC = 0x0100;
        
        [StructLayout(LayoutKind.Sequential)]
        public struct HWND
        {
            public IntPtr h;

            public static HWND Cast(IntPtr h)
            {
                HWND hTemp = new HWND();
                hTemp.h = h;
                return hTemp;
            }

            public static implicit operator IntPtr(HWND h)
            {
                return h.h;
            }

            public static HWND NULL
            {
                get
                {
                    HWND hTemp = new HWND();
                    hTemp.h = IntPtr.Zero;
                    return hTemp;
                }
            }

            public static bool operator==(HWND hl, HWND hr)
            {
                return hl.h == hr.h;
            }

            public static bool operator!=(HWND hl, HWND hr)
            {
                return hl.h != hr.h;
            }

            override public bool Equals(object oCompare)
            {
                HWND hr = Cast((HWND)oCompare);
                return h == hr.h;
            }

            public override int GetHashCode()
            {
                return (int) h;
            }
        }


        [StructLayout(LayoutKind.Sequential)]
        public struct RECT
        {
            public int left;
            public int top;
            public int right;
            public int bottom;

            public RECT(int left, int top, int right, int bottom)
            {
                this.left   = left;
                this.top    = top;
                this.right  = right;
                this.bottom = bottom;
            }

            public bool IsEmpty
            {
                get
                {
                    return left >= right || top >= bottom;
                }
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct POINT
        {
            public int x;
            public int y;

            public POINT(int x, int y)
            {
                this.x  = x;
                this.y  = y;
            }
        }

        // WinEvent specific consts and delegates

        public const int EVENT_MIN = 0;
        public const int EVENT_MAX = 0x7FFFFFFF;

        public const int EVENT_SYSTEM_MENUSTART = 0x0004;
        public const int EVENT_SYSTEM_MENUEND = 0x0005;
        public const int EVENT_SYSTEM_MENUPOPUPSTART = 0x0006;
        public const int EVENT_SYSTEM_MENUPOPUPEND = 0x0007;
        public const int EVENT_SYSTEM_CAPTURESTART = 0x0008;
        public const int EVENT_SYSTEM_CAPTUREEND = 0x0009;
        public const int EVENT_SYSTEM_SWITCHSTART = 0x0014;
        public const int EVENT_SYSTEM_SWITCHEND = 0x0015;

        public const int EVENT_OBJECT_CREATE = 0x8000;
        public const int EVENT_OBJECT_DESTROY = 0x8001;
        public const int EVENT_OBJECT_SHOW = 0x8002;
        public const int EVENT_OBJECT_HIDE = 0x8003;
        public const int EVENT_OBJECT_FOCUS = 0x8005;
        public const int EVENT_OBJECT_STATECHANGE = 0x800A;
        public const int EVENT_OBJECT_LOCATIONCHANGE = 0x800B;

        public const int WINEVENT_OUTOFCONTEXT = 0x0000;
        public const int WINEVENT_SKIPOWNTHREAD = 0x0001;
        public const int WINEVENT_SKIPOWNPROCESS = 0x0002;
        public const int WINEVENT_INCONTEXT = 0x0004;

        // WinEvent fired when new Avalon UI is created
        public const int EventObjectUIFragmentCreate = 0x6FFFFFFF;

        // the delegate passed to USER for receiving a WinEvent
        public delegate void WinEventProcDef(int winEventHook, int eventId, IntPtr hwnd, int idObject, int idChild, int eventThread, uint eventTime);
    }
}

