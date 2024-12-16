// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma warning disable 649 // Disable CS0649: "field is never assigned to"

using System.Runtime.InteropServices;
using System;
using System.Text;
using Windows.Win32;

#if !DRT && !UIAUTOMATIONTYPES
using MS.Internal.Interop;
using MS.Utility;
#endif

// The SecurityHelper class differs between assemblies and could not actually be
//  shared, so it is duplicated across namespaces to prevent name collision.
#if WINDOWS_BASE
using MS.Internal.WindowsBase;
#elif UIAUTOMATIONTYPES
using MS.Internal.UIAutomationTypes;
#else
#error Attempt to use a class (duplicated across multiple namespaces) from an unknown assembly.
#endif

namespace MS.Win32
{
    internal partial class NativeMethods {
 #if !FRAMEWORK_NATIVEMETHODS
        [StructLayout(LayoutKind.Sequential, CharSet=CharSet.Auto)]
        public class XFORM {
            public float eM11;
            public float eM12;
            public float eM21;
            public float eM22;
            public float eDx;
            public float eDy;

            public XFORM()
            {
                eM11 = eM22 = 1; // identity matrix.
            }

            public XFORM( float em11, float em12, float em21, float em22, float edx, float edy )
            {
                eM11 = em11;
                eM12 = em12;
                eM21 = em21;
                eM22 = em22;
                eDx  = edx;
                eDy  = edy;
            }

            public XFORM( float[] elements )
            {
                eM11 = elements[0];
                eM12 = elements[1];
                eM21 = elements[2];
                eM22 = elements[3];
                eDx  = elements[4];
                eDy  = elements[5];
            }

            public override string ToString()
            {
                return String.Format(System.Globalization.CultureInfo.CurrentCulture,"[{0}, {1}, {2}, {3}, {4}, {5}]", eM11, eM12, eM21, eM22, eDx, eDy );
            }

            public override bool Equals( object obj )
            {
                XFORM xform = obj as XFORM;

                if( xform == null )
                {
                    return false;
                }

                return eM11 == xform.eM11 &&
                       eM12 == xform.eM12 &&
                       eM21 == xform.eM21 &&
                       eM22 == xform.eM22 &&
                       eDx  == xform.eDx  &&
                       eDy  == xform.eDy;
            }

            public override int GetHashCode()
            {
                return ToString().GetHashCode();
            }
}
#endif

        public static HandleRef NullHandleRef = new HandleRef(null, IntPtr.Zero);

        public const int APPCOMMAND_BROWSER_BACKWARD       = 1;
        public const int APPCOMMAND_BROWSER_FORWARD        = 2;
        public const int APPCOMMAND_BROWSER_REFRESH        = 3;
        public const int APPCOMMAND_BROWSER_STOP           = 4;
        public const int APPCOMMAND_BROWSER_SEARCH         = 5;
        public const int APPCOMMAND_BROWSER_FAVORITES      = 6;
        public const int APPCOMMAND_BROWSER_HOME           = 7;
        public const int APPCOMMAND_VOLUME_MUTE            = 8;
        public const int APPCOMMAND_VOLUME_DOWN            = 9;
        public const int APPCOMMAND_VOLUME_UP              = 10;
        public const int APPCOMMAND_MEDIA_NEXTTRACK        = 11;
        public const int APPCOMMAND_MEDIA_PREVIOUSTRACK    = 12;
        public const int APPCOMMAND_MEDIA_STOP             = 13;
        public const int APPCOMMAND_MEDIA_PLAY_PAUSE       = 14;
        public const int APPCOMMAND_LAUNCH_MAIL            = 15;
        public const int APPCOMMAND_LAUNCH_MEDIA_SELECT    = 16;
        public const int APPCOMMAND_LAUNCH_APP1            = 17;
        public const int APPCOMMAND_LAUNCH_APP2            = 18;
        public const int APPCOMMAND_BASS_DOWN              = 19;
        public const int APPCOMMAND_BASS_BOOST             = 20;
        public const int APPCOMMAND_BASS_UP                = 21;
        public const int APPCOMMAND_TREBLE_DOWN            = 22;
        public const int APPCOMMAND_TREBLE_UP              = 23;
        public const int APPCOMMAND_MICROPHONE_VOLUME_MUTE = 24;
        public const int APPCOMMAND_MICROPHONE_VOLUME_DOWN = 25;
        public const int APPCOMMAND_MICROPHONE_VOLUME_UP   = 26;
        public const int APPCOMMAND_HELP                   = 27;
        public const int APPCOMMAND_FIND                   = 28;
        public const int APPCOMMAND_NEW                    = 29;
        public const int APPCOMMAND_OPEN                   = 30;
        public const int APPCOMMAND_CLOSE                  = 31;
        public const int APPCOMMAND_SAVE                   = 32;
        public const int APPCOMMAND_PRINT                  = 33;
        public const int APPCOMMAND_UNDO                   = 34;
        public const int APPCOMMAND_REDO                   = 35;
        public const int APPCOMMAND_COPY                   = 36;
        public const int APPCOMMAND_CUT                    = 37;
        public const int APPCOMMAND_PASTE                  = 38;
        public const int APPCOMMAND_REPLY_TO_MAIL          = 39;
        public const int APPCOMMAND_FORWARD_MAIL           = 40;
        public const int APPCOMMAND_SEND_MAIL              = 41;
        public const int APPCOMMAND_SPELL_CHECK            = 42;
        public const int APPCOMMAND_DICTATE_OR_COMMAND_CONTROL_TOGGLE    = 43;
        public const int APPCOMMAND_MIC_ON_OFF_TOGGLE      = 44;
        public const int APPCOMMAND_CORRECTION_LIST        = 45;
        public const int APPCOMMAND_MEDIA_PLAY             = 46;
        public const int APPCOMMAND_MEDIA_PAUSE            = 47;
        public const int APPCOMMAND_MEDIA_RECORD           = 48;
        public const int APPCOMMAND_MEDIA_FAST_FORWARD     = 49;
        public const int APPCOMMAND_MEDIA_REWIND           = 50;
        public const int APPCOMMAND_MEDIA_CHANNEL_UP       = 51;
        public const int APPCOMMAND_MEDIA_CHANNEL_DOWN     = 52;
        public const int FAPPCOMMAND_MOUSE = 0x8000;
        public const int FAPPCOMMAND_KEY   = 0;
        public const int FAPPCOMMAND_OEM   = 0x1000;
        public const int FAPPCOMMAND_MASK  = 0xF000;

        public const int BI_RGB = 0;
        public const int BITSPIXEL = 12;

        public const int
        CF_TEXT = 1,
        CF_BITMAP = 2,
        CF_METAFILEPICT = 3,
        CF_SYLK = 4,
        CF_DIF = 5,
        CF_TIFF = 6,
        CF_OEMTEXT = 7,
        CF_DIB = 8,
        CF_PALETTE = 9,
        CF_PENDATA = 10,
        CF_RIFF = 11,
        CF_WAVE = 12,
        CF_UNICODETEXT = 13,
        CF_ENHMETAFILE = 14,
        CF_HDROP = 15,
        CF_LOCALE = 16,
        CW_USEDEFAULT = (unchecked((int)0x80000000));

        public const int
        DISPID_UNKNOWN = (-1),
        DISPATCH_METHOD = 0x1,
        DEFAULT_GUI_FONT = 17,
        DIB_RGB_COLORS = 0,
        DVASPECT_CONTENT = 1;

        public const int GMEM_MOVEABLE = 0x0002,
        GMEM_ZEROINIT = 0x0040,
        GMEM_DDESHARE = 0x2000,
        GWL_WNDPROC = (-4),
        GWL_HWNDPARENT = (-8),
        GWL_STYLE = (-16),
        GWL_EXSTYLE = (-20),
        GWL_ID = (-12),
        GW_HWNDFIRST = 0,
        GW_HWNDLAST = 1,
        GW_HWNDNEXT = 2,
        GW_HWNDPREV = 3,
        GW_OWNER = 4,
        GW_CHILD = 5,
        GA_PARENT = 1,
        GA_ROOT = 2;

        // ImmGetCompostionString index.
        public const int
        GCS_COMPSTR           = 0x0008,
        GCS_COMPATTR          = 0x0010,
        GCS_COMPCLAUSE        = 0x0020,
        GCS_CURSORPOS         = 0x0080,
        GCS_DELTASTART        = 0x0100,
        GCS_RESULTREADSTR     = 0x0200,
        GCS_RESULTREADCLAUSE  = 0x0400,
        GCS_RESULTSTR         = 0x0800,
        GCS_RESULTCLAUSE      = 0x1000,

        // attribute for COMPOSITIONSTRING Structure
        ATTR_INPUT               = 0x00,
        ATTR_TARGET_CONVERTED    = 0x01,
        ATTR_CONVERTED           = 0x02,
        ATTR_TARGET_NOTCONVERTED = 0x03,
        ATTR_INPUT_ERROR         = 0x04,
        ATTR_FIXEDCONVERTED      = 0x05,

        // dwAction for ImmNotifyIME
        NI_COMPOSITIONSTR = 0x0015,

        // wParam of report message WM_IME_NOTIFY
        IMN_OPENCANDIDATE               = 0x0005,
        IMN_SETSENTENCEMODE             = 0x0007,
        IMN_SETOPENSTATUS               = 0x0008,
        IMN_SETCANDIDATEPOS             = 0x0009,
        IMN_SETCOMPOSITIONFONT          = 0x000A,
        IMN_SETCOMPOSITIONWINDOW        = 0x000B,
        IMN_SETSTATUSWINDOWPOS          = 0x000C,
        IMN_GUIDELINE                   = 0x000D,
        IMN_PRIVATE                     = 0x000E,

        // dwIndex for ImmNotifyIME/NI_COMPOSITIONSTR
        CPS_COMPLETE = 0x01,
        CPS_CANCEL   = 0x04,

        // dwStyle for CANDIDATEFORM
        CFS_DEFAULT                     = 0x0000,
        CFS_RECT                        = 0x0001,
        CFS_POINT                       = 0x0002,
        CFS_FORCE_POSITION              = 0x0020,
        CFS_CANDIDATEPOS                = 0x0040,
        CFS_EXCLUDE                     = 0x0080,

        // bit field for conversion mode
        IME_CMODE_ALPHANUMERIC          = 0x0000,
        IME_CMODE_NATIVE                = 0x0001,
        IME_CMODE_CHINESE               = 0x0001,  // IME_CMODE_NATIVE,
        IME_CMODE_HANGEUL               = 0x0001,  // IME_CMODE_NATIVE,
        IME_CMODE_HANGUL                = 0x0001,  // IME_CMODE_NATIVE,
        IME_CMODE_JAPANESE              = 0x0001,  // IME_CMODE_NATIVE,
        IME_CMODE_KATAKANA              = 0x0002,  // only effect under IME_CMODE_NATIVE
        IME_CMODE_LANGUAGE              = 0x0003,
        IME_CMODE_FULLSHAPE             = 0x0008,
        IME_CMODE_ROMAN                 = 0x0010,
        IME_CMODE_CHARCODE              = 0x0020,
        IME_CMODE_HANJACONVERT          = 0x0040,
        IME_CMODE_SOFTKBD               = 0x0080,
        IME_CMODE_NOCONVERSION          = 0x0100,
        IME_CMODE_EUDC                  = 0x0200,
        IME_CMODE_SYMBOL                = 0x0400,
        IME_CMODE_FIXED                 = 0x0800,

        // bit field for sentence mode
        IME_SMODE_NONE                  = 0x0000,
        IME_SMODE_PLAURALCLAUSE         = 0x0001,
        IME_SMODE_SINGLECONVERT         = 0x0002,
        IME_SMODE_AUTOMATIC             = 0x0004,
        IME_SMODE_PHRASEPREDICT         = 0x0008,
        IME_SMODE_CONVERSATION          = 0x0010,
        IME_SMODE_RESERVED          = 0x0000F000,

        IME_CAND_UNKNOWN                = 0x0000,
        IME_CAND_READ                   = 0x0001,
        IME_CAND_CODE                   = 0x0002,
        IME_CAND_MEANING                = 0x0003,
        IME_CAND_RADICAL                = 0x0004,
        IME_CAND_STROKE                 = 0x0005,

        IMR_COMPOSITIONWINDOW           = 0x0001,
        IMR_CANDIDATEWINDOW             = 0x0002,
        IMR_COMPOSITIONFONT             = 0x0003,
        IMR_RECONVERTSTRING             = 0x0004,
        IMR_CONFIRMRECONVERTSTRING      = 0x0005,
        IMR_QUERYCHARPOSITION           = 0x0006,
        IMR_DOCUMENTFEED                = 0x0007,

        IME_CONFIG_GENERAL              = 1,
        IME_CONFIG_REGISTERWORD         = 2,
        IME_CONFIG_SELECTDICTIONARY     = 3,

        IGP_GETIMEVERSION               = (-4),
        IGP_PROPERTY                    = 0x00000004,
        IGP_CONVERSION                  = 0x00000008,
        IGP_SENTENCE                    = 0x0000000c,
        IGP_UI                          = 0x00000010,
        IGP_SETCOMPSTR                  = 0x00000014,
        IGP_SELECT                      = 0x00000018,

        IME_PROP_AT_CARET               = 0x00010000,
        IME_PROP_SPECIAL_UI             = 0x00020000,
        IME_PROP_CANDLIST_START_FROM_1  = 0x00040000,
        IME_PROP_UNICODE                = 0x00080000,
        IME_PROP_COMPLETE_ON_UNSELECT   = 0x00100000;

        // CANDIDATEFORM structures
        [StructLayout(LayoutKind.Sequential)]
        public struct CANDIDATEFORM
        {
            public int    dwIndex;
            public int    dwStyle;
            public POINT  ptCurrentPos;
            public RECT   rcArea;
        }

        // COMPOSITIONFORM structures
        [StructLayout(LayoutKind.Sequential)]
        public struct COMPOSITIONFORM
        {
            public int    dwStyle;
            public POINT  ptCurrentPos;
            public RECT   rcArea;
        }

        // RECONVERTSTRING structures
        [StructLayout(LayoutKind.Sequential)]
        public struct RECONVERTSTRING
        {
            public int dwSize;
            public int dwVersion;
            public int dwStrLen;
            public int dwStrOffset;
            public int dwCompStrLen;
            public int dwCompStrOffset;
            public int dwTargetStrLen;
            public int dwTargetStrOffset;
        }

        // REGISTERWORD structures
        [StructLayout(LayoutKind.Sequential, CharSet=CharSet.Auto)]
        public struct REGISTERWORD
        {
            public string lpReading;
            public string lpWord;
        }

        public const int
        HTCLIENT = 1,
        HTBOTTOMLEFT = 16,
        HTBOTTOMRIGHT = 17,
        HCF_HIGHCONTRASTON = 0x00000001;

        // HOVER_DEFAULT = Do not use this value ever! It crashes entire servers.

        public static HandleRef HWND_TOP = new HandleRef(null, (IntPtr)0);
        public static HandleRef HWND_BOTTOM = new HandleRef(null, (IntPtr)1);
        public static HandleRef HWND_TOPMOST = new HandleRef(null, new IntPtr(-1));
        public static HandleRef HWND_NOTOPMOST = new HandleRef(null, new IntPtr(-2));
        // NOTE:  NativeMethodsOther.cs defines the following
        //public static IntPtr HWND_MESSAGE = new IntPtr(-3);

        public const int
        ICON_SMALL = 0,
        ICON_BIG = 1,
        IDC_ARROW = 32512,
        IDC_IBEAM = 32513,
        IDC_WAIT = 32514,
        IDC_CROSS = 32515,
        IDC_SIZEALL = 32646,
        IDC_SIZENWSE = 32642,
        IDC_SIZENESW = 32643,
        IDC_SIZEWE = 32644,
        IDC_SIZENS = 32645,
        IDC_UPARROW = 32516,
        IDC_NO = 32648,
        IDC_APPSTARTING = 32650,
        IDC_HELP = 32651,
        IMAGE_CURSOR = 2;

        public const int CSC_NAVIGATEFORWARD = 0x00000001,
        CSC_NAVIGATEBACK = 0x00000002;

        public const int STG_E_CANTSAVE = unchecked((int)0x80030103);

        public const int LOGPIXELSX = 88,
        LOGPIXELSY = 90;

        public const int LWA_ALPHA = 0x00000002;

        public const int MEMBERID_NIL = (-1),
        MAX_PATH = 260,
        MA_NOACTIVATE = 0x0003,
        MK_LBUTTON = 0x0001,
        MK_RBUTTON = 0x0002,
        MK_SHIFT = 0x0004,
        MK_CONTROL = 0x0008;

        //ActiveX related defines
        public const int
        OLECONTF_EMBEDDINGS = 0x1,
        OLECONTF_LINKS = 0x2,
        OLECONTF_OTHERS = 0x4,
        OLECONTF_ONLYUSER = 0x8,
        OLECONTF_ONLYIFRUNNING = 0x10,
        OLEMISC_RECOMPOSEONRESIZE = 0x00000001,
        OLEMISC_INSIDEOUT = 0x00000080,
        OLEMISC_ACTIVATEWHENVISIBLE = 0x0000100,
        OLEMISC_ACTSLIKEBUTTON = 0x00001000,
        OLEMISC_SETCLIENTSITEFIRST = 0x00020000,
        OLEIVERB_PRIMARY = 0,
        OLEIVERB_SHOW = -1,
        OLEIVERB_HIDE = -3,
        OLEIVERB_UIACTIVATE = -4,
        OLEIVERB_INPLACEACTIVATE = -5,
        OLEIVERB_DISCARDUNDOSTATE= -6,
        OLEIVERB_PROPERTIES = -7,
        XFORMCOORDS_POSITION = 0x1,
        XFORMCOORDS_SIZE = 0x2,
        XFORMCOORDS_HIMETRICTOCONTAINER = 0x4,
        XFORMCOORDS_CONTAINERTOHIMETRIC = 0x8;

        public const int
        PS_SOLID = 0,
        PS_DOT = 2,
        PLANES = 14,
        PRF_CHECKVISIBLE = 0x00000001,
        PRF_NONCLIENT = 0x00000002,
        PRF_CLIENT = 0x00000004,
        PRF_ERASEBKGND = 0x00000008,
        PRF_CHILDREN = 0x00000010,
        PM_NOREMOVE = 0x0000,
        PM_REMOVE = 0x0001,
        PM_NOYIELD = 0x0002,
        PATCOPY = 0x00F00021,
        PATINVERT = 0x005A0049;

        public const int QS_KEY = 0x0001,
        QS_MOUSEMOVE = 0x0002,
        QS_MOUSEBUTTON = 0x0004,
        QS_POSTMESSAGE = 0x0008,
        QS_TIMER = 0x0010,
        QS_PAINT = 0x0020,
        QS_SENDMESSAGE = 0x0040,
        QS_HOTKEY = 0x0080,
        QS_ALLPOSTMESSAGE = 0x0100,
        QS_MOUSE = QS_MOUSEMOVE | QS_MOUSEBUTTON,
        QS_INPUT = QS_MOUSE | QS_KEY,
        QS_ALLEVENTS = QS_INPUT | QS_POSTMESSAGE | QS_TIMER | QS_PAINT | QS_HOTKEY,
        QS_ALLINPUT = QS_INPUT | QS_POSTMESSAGE | QS_TIMER | QS_PAINT | QS_HOTKEY | QS_SENDMESSAGE;

        public const int RDW_INVALIDATE = 0x0001;
        public const int RDW_ALLCHILDREN = 0x0080;

        public const int
        STGM_READ = 0x00000000,
        STGM_WRITE = 0x00000001,
        STGM_READWRITE = 0x00000002,
        STGM_SHARE_EXCLUSIVE = 0x00000010,
        STGM_CREATE = 0x00001000,
        STGM_TRANSACTED = 0x00010000,
        STGM_CONVERT = 0x00020000,
        STGM_DELETEONRELEASE    = 0x04000000,

        STGTY_STORAGE      = 1,
        STGTY_STREAM       = 2,
        STGTY_LOCKBYTES    = 3,
        STGTY_PROPERTY     = 4,

        STARTF_USESHOWWINDOW = 0x00000001,
        SIZE_MAXIMIZED = 2,
        SW_HIDE = 0,
        SW_NORMAL = 1,
        SW_SHOWMINIMIZED = 2,
        SW_SHOWMAXIMIZED = 3,
        SW_MAXIMIZE = 3,
        SW_SHOWNOACTIVATE = 4,
        SW_SHOW = 5,
        SW_MINIMIZE = 6,
        SW_SHOWMINNOACTIVE = 7,
        SW_SHOWNA = 8,
        SW_RESTORE = 9,
        SW_MAX = 10,
        SWP_NOSIZE = 0x0001,
        SWP_NOMOVE = 0x0002,
        SWP_NOZORDER = 0x0004,
        SWP_NOACTIVATE = 0x0010,
        SWP_SHOWWINDOW = 0x0040,
        SWP_HIDEWINDOW = 0x0080,
        SWP_DRAWFRAME = 0x0020;

        public const int
        SC_SIZE = 0xF000,
        SC_MINIMIZE = 0xF020,
        SC_MAXIMIZE = 0xF030,
        SC_CLOSE = 0xF060,
        SC_KEYMENU = 0xF100,
        SC_RESTORE = 0xF120,
        SC_MOVE = 0xF010,
        SPI_GETFONTSMOOTHING = 0x004A,
        SPI_GETDROPSHADOW = 0x1024,
        SPI_GETFLATMENU =   0x1022,
        SPI_GETFONTSMOOTHINGTYPE = 0x200A,
        SPI_GETFONTSMOOTHINGCONTRAST = 0x200C,
        SPI_ICONHORIZONTALSPACING =  0x000D,
        SPI_ICONVERTICALSPACING =   0x0018,
        SPI_GETICONMETRICS =        0x002D,
        SPI_GETICONTITLEWRAP =      0x0019,
        SPI_GETICONTITLELOGFONT =   0x001F,
        SPI_GETKEYBOARDCUES =       0x100A,
        SPI_GETKEYBOARDDELAY =      0x0016,
        SPI_GETKEYBOARDPREF =       0x0044,
        SPI_GETKEYBOARDSPEED =      0x000A,
        SPI_GETMOUSEHOVERWIDTH =    0x0062,
        SPI_GETMOUSEHOVERHEIGHT =   0x0064,
        SPI_GETMOUSEHOVERTIME =     0x0066,
        SPI_GETMOUSESPEED =         0x0070,
        SPI_GETMENUDROPALIGNMENT =  0x001B,
        SPI_GETMENUFADE =           0x1012,
        SPI_GETMENUSHOWDELAY =      0x006A,
        SPI_GETCOMBOBOXANIMATION =  0x1004,
        SPI_GETCLIENTAREAANIMATION = 0x1042,
        SPI_GETGRADIENTCAPTIONS =   0x1008,
        SPI_GETHOTTRACKING =        0x100E,
        SPI_GETLISTBOXSMOOTHSCROLLING =  0x1006,
        SPI_GETMENUANIMATION    =   0x1002,
        SPI_GETSELECTIONFADE =      0x1014,
        SPI_GETTOOLTIPANIMATION =   0x1016,
        SPI_GETUIEFFECTS =          0x103E,
        SPI_GETACTIVEWINDOWTRACKING =       0x1000,
        SPI_GETACTIVEWNDTRKTIMEOUT  =       0x2002,
        SPI_GETANIMATION =          0x0048,
        SPI_GETBORDER  =            0x0005,
        SPI_GETCARETWIDTH =         0x2006,
        SPI_GETMOUSEVANISH =        0x1020,
        SPI_GETDRAGFULLWINDOWS = 38,
        SPI_GETNONCLIENTMETRICS = 41,
        SPI_GETWORKAREA = 48,
        SPI_GETHIGHCONTRAST = 66,
        SPI_GETDEFAULTINPUTLANG = 89,
        SPI_GETSNAPTODEFBUTTON = 95,
        SPI_GETWHEELSCROLLLINES = 104,
        STATFLAG_DEFAULT = 0x0,
        STATFLAG_NONAME = 0x1,
        STATFLAG_NOOPEN = 0x2,
        STGC_DEFAULT = 0x0,
        STGC_OVERWRITE = 0x1,
        STGC_ONLYIFCURRENT = 0x2,
        STGC_DANGEROUSLYCOMMITMERELYTODISKCACHE = 0x4,
        STREAM_SEEK_SET = 0x0,
        STREAM_SEEK_CUR = 0x1,
        STREAM_SEEK_END = 0x2;

        public const int TME_LEAVE = 0x00000002;

        public const int VK_TAB = 0x09;
        public const int VK_SHIFT = 0x10;
        public const int VK_CONTROL = 0x11;
        public const int VK_MENU = 0x12;

        public const int
        WA_INACTIVE = 0,
        WA_ACTIVE = 1,
        WA_CLICKACTIVE = 2;

        public const int
#if !DRT && !UIAUTOMATIONTYPES
        WM_REFLECT = (int)WindowMessage.WM_USER + 0x1C00,
        WM_CHOOSEFONT_GETLOGFONT = (int)WindowMessage.WM_USER +1,
#endif
        WS_OVERLAPPED = 0x00000000,
        WS_POPUP = unchecked((int)0x80000000),
        WS_CHILD = 0x40000000,
        WS_MINIMIZE = 0x20000000,
        WS_VISIBLE = 0x10000000,
        WS_DISABLED = 0x08000000,
        WS_CLIPSIBLINGS = 0x04000000,
        WS_CLIPCHILDREN = 0x02000000,
        WS_MAXIMIZE = 0x01000000,
        WS_CAPTION = 0x00C00000,
        WS_BORDER = 0x00800000,
        WS_DLGFRAME = 0x00400000,
        WS_VSCROLL = 0x00200000,
        WS_HSCROLL = 0x00100000,
        WS_SYSMENU = 0x00080000,
        WS_THICKFRAME = 0x00040000,
        WS_TABSTOP = 0x00010000,
        WS_MINIMIZEBOX = 0x00020000,
        WS_MAXIMIZEBOX = 0x00010000,
        WS_EX_DLGMODALFRAME = 0x00000001,
        WS_EX_TRANSPARENT = 0x00000020,
        WS_EX_MDICHILD = 0x00000040,
        WS_EX_TOOLWINDOW = 0x00000080,
        WS_EX_WINDOWEDGE = 0x00000100,
        WS_EX_CLIENTEDGE = 0x00000200,
        WS_EX_CONTEXTHELP = 0x00000400,
        WS_EX_RIGHT = 0x00001000,
        WS_EX_LEFT = 0x00000000,
        WS_EX_RTLREADING = 0x00002000,
        WS_EX_LEFTSCROLLBAR = 0x00004000,
        WS_EX_CONTROLPARENT = 0x00010000,
        WS_EX_STATICEDGE = 0x00020000,
        WS_EX_APPWINDOW = 0x00040000,
        WS_EX_LAYERED = 0x00080000,
        WS_EX_TOPMOST = 0x00000008,
        WS_EX_LAYOUTRTL = 0x00400000,
        WS_EX_NOINHERITLAYOUT = 0x00100000,
        WS_EX_COMPOSITED = 0x02000000,
        WPF_SETMINPOSITION = 0x0001,
        WPF_RESTORETOMAXIMIZED = 0x0002;

        public const int WHITE_BRUSH = 0x00000000;
        public const int NULL_BRUSH = 5;

        public static int SignedHIWORD(int n)
        {
            int i = (int)(short)((n >> 16) & 0xffff);
            return i;
        }

        public static int SignedLOWORD(int n)
        {
            int i = (int)(short)(n & 0xFFFF);
            return i;
        }

        [StructLayout(LayoutKind.Sequential,CharSet=CharSet.Auto, Pack=4)]
        public class MONITORINFOEX {
            internal int     cbSize = SizeOf();
            internal RECT    rcMonitor = new RECT();
            internal RECT    rcWork = new RECT();
            internal int     dwFlags = 0;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst=32)]
            internal char[]  szDevice = new char[32];
            
            private static int SizeOf()
            {
                return Marshal.SizeOf(typeof(MONITORINFOEX));
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        public class TRACKMOUSEEVENT {
                public int      cbSize = SizeOf();
                public int      dwFlags = 0;
                public IntPtr   hwndTrack = IntPtr.Zero;
                public int      dwHoverTime = 100; // Never set this to field ZERO, or to HOVER_DEFAULT, ever!
            
            private static int SizeOf()
            {
                return Marshal.SizeOf(typeof(TRACKMOUSEEVENT));
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct POINT {
            public int x;
            public int y;

            public POINT(int x, int y) {
                this.x = x;
                this.y = y;
            }
#if DEBUG
            public override string ToString() {
                return "{x=" + x + ", y=" + y + "}";
            }
#endif
        }

        public delegate IntPtr WndProc(IntPtr hWnd, Int32 msg, IntPtr wParam, IntPtr lParam);

        [StructLayout(LayoutKind.Sequential)]
        public class NONCLIENTMETRICS {
            public int cbSize = SizeOf();
            public int iBorderWidth = 0;
            public int iScrollWidth = 0;
            public int iScrollHeight = 0;
            public int iCaptionWidth = 0;
            public int iCaptionHeight = 0;
            [MarshalAs(UnmanagedType.Struct)]
            public LOGFONT lfCaptionFont = null;
            public int iSmCaptionWidth = 0;
            public int iSmCaptionHeight = 0;
            [MarshalAs(UnmanagedType.Struct)]
            public LOGFONT lfSmCaptionFont = null;
            public int iMenuWidth = 0;
            public int iMenuHeight = 0;
            [MarshalAs(UnmanagedType.Struct)]
            public LOGFONT lfMenuFont = null;
            [MarshalAs(UnmanagedType.Struct)]
            public LOGFONT lfStatusFont = null;
            [MarshalAs(UnmanagedType.Struct)]
            public LOGFONT lfMessageFont = null;
            
            private static int SizeOf()
            {
                return Marshal.SizeOf(typeof(NONCLIENTMETRICS));
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        public class ICONMETRICS {
            public int      cbSize = SizeOf();
            public int      iHorzSpacing = 0;
            public int      iVertSpacing = 0;
            public int      iTitleWrap = 0;
            [MarshalAs(UnmanagedType.Struct)]
            public LOGFONT  lfFont = null;
            
            private static int SizeOf()
            {
                return Marshal.SizeOf(typeof(ICONMETRICS));
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct PAINTSTRUCT {
            public IntPtr   hdc;
            public bool     fErase;
            // rcPaint was a by-value RECT structure
            public int      rcPaint_left;
            public int      rcPaint_top;
            public int      rcPaint_right;
            public int      rcPaint_bottom;
            public bool     fRestore;
            public bool     fIncUpdate;
            public int      reserved1;
            public int      reserved2;
            public int      reserved3;
            public int      reserved4;
            public int      reserved5;
            public int      reserved6;
            public int      reserved7;
            public int      reserved8;
        }

#if FRAMEWORK_NATIVEMETHODS || CORE_NATIVEMETHODS || BASE_NATIVEMETHODS || DRT_SEE_NATIVEMETHODS || UIAUTOMATIONTYPES

        [StructLayout(LayoutKind.Sequential)]
        public class SIZE {
            public int cx;
            public int cy;

            public SIZE()
            {
            }

            public SIZE(int cx, int cy) {
                this.cx = cx;
                this.cy = cy;
            }
}

#endif

        [StructLayout(LayoutKind.Sequential)]
        public struct WINDOWPLACEMENT {
            public int  length;
            public int  flags;
            public int  showCmd;
            // ptMinPosition was a by-value POINT structure
            public int  ptMinPosition_x;
            public int  ptMinPosition_y;
            // ptMaxPosition was a by-value POINT structure
            public int  ptMaxPosition_x;
            public int  ptMaxPosition_y;
            // rcNormalPosition was a by-value RECT structure
            public int  rcNormalPosition_left;
            public int  rcNormalPosition_top;
            public int  rcNormalPosition_right;
            public int  rcNormalPosition_bottom;
        }

        [StructLayout(LayoutKind.Sequential)]
        public class BITMAP {
            public int bmType = 0;
            public int bmWidth = 0;
            public int bmHeight = 0;
            public int bmWidthBytes = 0;
            public short bmPlanes = 0;
            public short bmBitsPixel = 0;
            public int bmBits = 0;
        }

        [StructLayout(LayoutKind.Sequential, CharSet=CharSet.Unicode)]
        public class LOGFONT
        {
            public int lfHeight;
            public int lfWidth;
            public int lfEscapement;
            public int lfOrientation;
            public int lfWeight;
            public byte lfItalic;
            public byte lfUnderline;
            public byte lfStrikeOut;
            public byte lfCharSet;
            public byte lfOutPrecision;
            public byte lfClipPrecision;
            public byte lfQuality;
            public byte lfPitchAndFamily;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst=32)]
            public string lfFaceName;
        }

        public delegate bool EnumThreadWindowsCallback(IntPtr hWnd, IntPtr lParam);

        [StructLayout(LayoutKind.Sequential)]
        public struct STYLESTRUCT {
            public int styleOld;
            public int styleNew;
        }

        [StructLayout(LayoutKind.Sequential)]
        public class STATSTG
        {
            [MarshalAs(UnmanagedType.LPWStr)]
            public string pwcsName = null;

            public int type = 0;
            [MarshalAs(UnmanagedType.I8)]
            public long cbSize = 0;
            [MarshalAs(UnmanagedType.I8)]
            public long mtime = 0;
            [MarshalAs(UnmanagedType.I8)]
            public long ctime = 0;
            [MarshalAs(UnmanagedType.I8)]
            public long atime = 0;
            [MarshalAs(UnmanagedType.I4)]
            public int grfMode = 0;
            [MarshalAs(UnmanagedType.I4)]
            public int grfLocksSupported = 0;

            public int clsid_data1 = 0;
            [MarshalAs(UnmanagedType.I2)]
            public short clsid_data2 = 0;
            [MarshalAs(UnmanagedType.I2)]
            public short clsid_data3 = 0;
            [MarshalAs(UnmanagedType.U1)]
            public byte clsid_b0 = 0;
            [MarshalAs(UnmanagedType.U1)]
            public byte clsid_b1 = 0;
            [MarshalAs(UnmanagedType.U1)]
            public byte clsid_b2 = 0;
            [MarshalAs(UnmanagedType.U1)]
            public byte clsid_b3 = 0;
            [MarshalAs(UnmanagedType.U1)]
            public byte clsid_b4 = 0;
            [MarshalAs(UnmanagedType.U1)]
            public byte clsid_b5 = 0;
            [MarshalAs(UnmanagedType.U1)]
            public byte clsid_b6 = 0;
            [MarshalAs(UnmanagedType.U1)]
            public byte clsid_b7 = 0;
            [MarshalAs(UnmanagedType.I4)]
            public int grfStateBits = 0;
            [MarshalAs(UnmanagedType.I4)]
            public int reserved = 0;
        }

        [Serializable]
        [StructLayout(LayoutKind.Sequential)]
        public class COMRECT {
            public int left;
            public int top;
            public int right;
            public int bottom;

            public COMRECT(int x, int y, int right, int bottom)
            {
                left = x;
                top = y;
                this.right = right;
                this.bottom = bottom;
            }

            public COMRECT(RECT rect) {
                left = rect.left;
                top = rect.top;
                bottom = rect.bottom;
                right = rect.right;
            }

            public void CopyTo(COMRECT destRect) {
                destRect.left = left;
                destRect.right = right;
                destRect.top = top;
                destRect.bottom = bottom;
            }

            public bool IsEmpty { get { return left == right && top == bottom; } }

            public override string ToString() {
                return "Left = " + left + " Top " + top + " Right = " + right + " Bottom = " + bottom;
            }
        }

        [StructLayout(LayoutKind.Sequential)/*leftover(noAutoOffset)*/]
        public sealed class tagOleMenuGroupWidths {
            [MarshalAs(UnmanagedType.ByValArray, SizeConst=6)/*leftover(offset=0, widths)*/]
            public int[] widths = new int[6];
        }

        [StructLayout(LayoutKind.Sequential)/*leftover(noAutoOffset)*/]
        public struct POINTF
        {
            public float x;
            public float y;
        }

        [StructLayout(LayoutKind.Sequential)/*leftover(noAutoOffset)*/]
        public sealed class OLEINPLACEFRAMEINFO
        {
            [MarshalAs(UnmanagedType.U4)/*leftover(offset=0, cb)*/]
            public uint cb;

            public bool fMDIApp;
            public IntPtr hwndFrame;
            public IntPtr hAccel;

            [MarshalAs(UnmanagedType.U4)/*leftover(offset=16, cAccelEntries)*/]
            public uint cAccelEntries;
        }

        [StructLayout(LayoutKind.Sequential)/*leftover(noAutoOffset)*/]
        public sealed class tagOLEVERB
        {
            public int lVerb = 0;

            [MarshalAs(UnmanagedType.LPWStr)] // leftover(offset=4, customMarshal="UniStringMarshaller", lpszVerbName)
            public string lpszVerbName = null;

            [MarshalAs(UnmanagedType.U4)] // leftover(offset=8, fuFlags)
            public uint fuFlags = 0;

            [MarshalAs(UnmanagedType.U4)] // leftover(offset=12, grfAttribs)
            public uint grfAttribs = 0;
        }

        [StructLayout(LayoutKind.Sequential)/*leftover(noAutoOffset)*/]
        public sealed class tagLOGPALETTE
        {
            [MarshalAs(UnmanagedType.U2)] // leftover(offset=0, palVersion)
            public ushort palVersion = 0;

            [MarshalAs(UnmanagedType.U2)] // leftover(offset=2, palNumEntries)
            public ushort palNumEntries = 0;
        }

        [StructLayout(LayoutKind.Sequential)]
        public sealed class VARIANT {
            [MarshalAs(UnmanagedType.I2)]
            public short vt;
            [MarshalAs(UnmanagedType.I2)]
            public short reserved1;
            [MarshalAs(UnmanagedType.I2)]
            public short reserved2;
            [MarshalAs(UnmanagedType.I2)]
            public short reserved3;

            public IntPtr data1;

            public IntPtr data2;

            public bool Byref{
                get{
                    return 0 != (vt & (int)tagVT.VT_BYREF);
                }
            }

            public void Clear() {
                if ((vt == (int)tagVT.VT_UNKNOWN || vt == (int)tagVT.VT_DISPATCH) && data1 != IntPtr.Zero) {
                    Marshal.Release(data1);
                }

                if (vt == (int)tagVT.VT_BSTR && data1 != IntPtr.Zero) {
                    SysFreeString(data1);
                }

                data1 = data2 = IntPtr.Zero;
                vt = (int)tagVT.VT_EMPTY;
            }

            ~VARIANT() {
                Clear();
            }

            public void SuppressFinalize()
            {
                // Called if this VARIANT is returned to the caller in native world which is supposed to call
                // VariantClear().
                // GC does not have to clear it.
                GC.SuppressFinalize(this);
            }

            [DllImport(ExternDll.Oleaut32,CharSet=CharSet.Auto)]
            private static extern IntPtr SysAllocString([In, MarshalAs(UnmanagedType.LPWStr)]string s);

            [DllImport(ExternDll.Oleaut32,CharSet=CharSet.Auto)]
            private static extern void SysFreeString(IntPtr pbstr);
            public void SetLong(long lVal) {
                data1 = (IntPtr)(lVal & 0xFFFFFFFF);
                data2 = (IntPtr)((lVal >> 32) & 0xFFFFFFFF);
            }

            public IntPtr ToCoTaskMemPtr() {
                IntPtr mem = Marshal.AllocCoTaskMem(16);
                Marshal.WriteInt16(mem, vt);
                Marshal.WriteInt16(mem, 2, reserved1);
                Marshal.WriteInt16(mem, 4, reserved2);
                Marshal.WriteInt16(mem, 6, reserved3);
                Marshal.WriteInt32(mem, 8, (int) data1);
                Marshal.WriteInt32(mem, 12, (int) data2);
                return mem;
            }

            public object ToObject() {
                IntPtr val = data1;
                long longVal;

                int vtType = (int)(vt & (short)tagVT.VT_TYPEMASK);

                switch (vtType) {
                case (int)tagVT.VT_EMPTY:
                    return null;
                case (int)tagVT.VT_NULL:
                    return Convert.DBNull;

                case (int)tagVT.VT_I1:
                    if (Byref) {
                        val = (IntPtr) Marshal.ReadByte(val);
                    }
                    return (SByte) (0xFF & (SByte) val);

                case (int)tagVT.VT_UI1:
                    if (Byref) {
                        val = (IntPtr) Marshal.ReadByte(val);
                    }

                    return (byte) (0xFF & (byte) val);

                case (int)tagVT.VT_I2:
                    if (Byref) {
                        val = (IntPtr) Marshal.ReadInt16(val);
                    }
                    return (short)(0xFFFF & (short) val);

                case (int)tagVT.VT_UI2:
                    if (Byref) {
                        val = (IntPtr) Marshal.ReadInt16(val);
                    }
                    return (UInt16)(0xFFFF & (UInt16) val);

                case (int)tagVT.VT_I4:
                case (int)tagVT.VT_INT:
                    if (Byref) {
                        val = (IntPtr) Marshal.ReadInt32(val);
                    }
                    return (int)val;

                case (int)tagVT.VT_UI4:
                case (int)tagVT.VT_UINT:
                    if (Byref) {
                        val = (IntPtr) Marshal.ReadInt32(val);
                    }
                    return (UInt32)val;

                case (int)tagVT.VT_I8:
                case (int)tagVT.VT_UI8:
                    if (Byref) {
                        longVal = Marshal.ReadInt64(val);
                    }
                    else {
                        longVal = ((uint)data1 & 0xffffffff) | ((uint)data2 << 32);
                    }

                    if (vt == (int)tagVT.VT_I8) {
                        return (long)longVal;
                    }
                    else {
                        return (UInt64)longVal;
                    }
                }

                if (Byref) {
                    val = GetRefInt(val);
                }

                switch (vtType) {
                case (int)tagVT.VT_R4:
                case (int)tagVT.VT_R8:

                    // can I use unsafe here?
                    throw new FormatException(/*SR.GetString(SR.CannotConvertIntToFloat)*/);

                case (int)tagVT.VT_CY:
                    // internally currency is 8-byte int scaled by 10,000
                    longVal = ((uint)data1 & 0xffffffff) | ((uint)data2 << 32);
                    return new Decimal(longVal);
                case (int)tagVT.VT_DATE:
                    throw new FormatException(/*SR.GetString(SR.CannotConvertDoubleToDate)*/);

                case (int)tagVT.VT_BSTR:
                case (int)tagVT.VT_LPWSTR:
                    return Marshal.PtrToStringUni(val);

                case (int)tagVT.VT_LPSTR:
                    return Marshal.PtrToStringAnsi(val);

                case (int)tagVT.VT_DISPATCH:
                case (int)tagVT.VT_UNKNOWN:
                    {
                        return Marshal.GetObjectForIUnknown(val);
                    }

                case (int)tagVT.VT_HRESULT:
                    return val;

                case (int)tagVT.VT_DECIMAL:
                    longVal = ((uint)data1 & 0xffffffff) | ((uint)data2 << 32);
                    return new Decimal(longVal);

                case (int)tagVT.VT_BOOL:
                    return (val != IntPtr.Zero);

                case (int)tagVT.VT_VARIANT:
                    VARIANT varStruct = Marshal.PtrToStructure<VARIANT>(val);
                    return varStruct.ToObject();
                case (int)tagVT.VT_CLSID:
                    //Debug.Fail("PtrToStructure will not work with System.Guid...");
                    Guid guid = Marshal.PtrToStructure<Guid>(val);
                    return guid;

                case (int)tagVT.VT_FILETIME:
                    longVal = ((uint)data1 & 0xffffffff) | ((uint)data2 << 32);
                    return new DateTime(longVal);

                case (int)tagVT.VT_ARRAY:
                    //gSAFEARRAY sa = (tagSAFEARRAY)Marshal.PtrToStructure(val), typeof(tagSAFEARRAY));
                    //return GetArrayFromSafeArray(sa);

                case (int)tagVT.VT_USERDEFINED:
                case (int)tagVT.VT_VOID:
                case (int)tagVT.VT_PTR:
                case (int)tagVT.VT_SAFEARRAY:
                case (int)tagVT.VT_CARRAY:

                case (int)tagVT.VT_RECORD:
                case (int)tagVT.VT_BLOB:
                case (int)tagVT.VT_STREAM:
                case (int)tagVT.VT_STORAGE:
                case (int)tagVT.VT_STREAMED_OBJECT:
                case (int)tagVT.VT_STORED_OBJECT:
                case (int)tagVT.VT_BLOB_OBJECT:
                case (int)tagVT.VT_CF:
                case (int)tagVT.VT_BSTR_BLOB:
                case (int)tagVT.VT_VECTOR:
                case (int)tagVT.VT_BYREF:
                    //case (int)tagVT.VT_RESERVED:
                default:
                    return null;
            }
            }
            private static IntPtr GetRefInt(IntPtr value) {
                return Marshal.ReadIntPtr(value);
            }
        }

        public enum  tagVT {
            VT_EMPTY = 0,
            VT_NULL = 1,
            VT_I2 = 2,
            VT_I4 = 3,
            VT_R4 = 4,
            VT_R8 = 5,
            VT_CY = 6,
            VT_DATE = 7,
            VT_BSTR = 8,
            VT_DISPATCH = 9,
            VT_ERROR = 10,
            VT_BOOL = 11,
            VT_VARIANT = 12,
            VT_UNKNOWN = 13,
            VT_DECIMAL = 14,
            VT_I1 = 16,
            VT_UI1 = 17,
            VT_UI2 = 18,
            VT_UI4 = 19,
            VT_I8 = 20,
            VT_UI8 = 21,
            VT_INT = 22,
            VT_UINT = 23,
            VT_VOID = 24,
            VT_HRESULT = 25,
            VT_PTR = 26,
            VT_SAFEARRAY = 27,
            VT_CARRAY = 28,
            VT_USERDEFINED = 29,
            VT_LPSTR = 30,
            VT_LPWSTR = 31,
            VT_RECORD = 36,
            VT_FILETIME = 64,
            VT_BLOB = 65,
            VT_STREAM = 66,
            VT_STORAGE = 67,
            VT_STREAMED_OBJECT = 68,
            VT_STORED_OBJECT = 69,
            VT_BLOB_OBJECT = 70,
            VT_CF = 71,
            VT_CLSID = 72,
            VT_BSTR_BLOB = 4095,
            VT_VECTOR = 4096,
            VT_ARRAY = 8192,
            VT_BYREF = 16384,
            VT_RESERVED = 32768,
            VT_ILLEGAL = 65535,
            VT_ILLEGALMASKED = 4095,
            VT_TYPEMASK = 4095
        }

        public delegate void TimerProc(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);

        [StructLayout(LayoutKind.Sequential, CharSet=CharSet.Auto)]
        public struct HIGHCONTRAST_I {
            public int cbSize;
            public int dwFlags;
            public IntPtr lpszDefaultScheme;
        }

        [StructLayout(LayoutKind.Sequential)/*leftover(noAutoOffset)*/]
        public sealed class DISPPARAMS
        {
          public IntPtr rgvarg;
          public IntPtr rgdispidNamedArgs;
          [MarshalAs(UnmanagedType.U4)/*leftover(offset=8, cArgs)*/]
          public uint cArgs;
          [MarshalAs(UnmanagedType.U4)/*leftover(offset=12, cNamedArgs)*/]
          public uint cNamedArgs;
        }

        [StructLayout(LayoutKind.Sequential)]
        public class EXCEPINFO {
            [MarshalAs(UnmanagedType.U2)]
            public ushort wCode;
            [MarshalAs(UnmanagedType.U2)]
            public ushort wReserved;
            [MarshalAs(UnmanagedType.BStr)]
            public string bstrSource;
            [MarshalAs(UnmanagedType.BStr)]
            public string bstrDescription;
            [MarshalAs(UnmanagedType.BStr)]
            public string bstrHelpFile;
            [MarshalAs(UnmanagedType.U4)]
            public uint dwHelpContext;

            public IntPtr pvReserved;

            public IntPtr pfnDeferredFillIn;
            [MarshalAs(UnmanagedType.I4)]
            public int scode;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct WINDOWPOS {
            public IntPtr hwnd;
            public IntPtr hwndInsertAfter;
            public int x;
            public int y;
            public int cx;
            public int cy;
            public int flags;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct MINMAXINFO {
            public POINT ptReserved;
            public POINT ptMaxSize;
            public POINT ptMaxPosition;
            public POINT ptMinTrackSize;
            public POINT ptMaxTrackSize;
        }

        internal abstract class CharBuffer
        {
            internal static CharBuffer CreateBuffer(int size)
            {
                if (Marshal.SystemDefaultCharSize == 1)
                {
                    return new AnsiCharBuffer(size);
                }
                return new UnicodeCharBuffer(size);
            }

            internal abstract IntPtr AllocCoTaskMem();
            internal abstract string GetString();
            internal abstract void PutCoTaskMem(IntPtr ptr);
            internal abstract void PutString(string s);
            internal abstract int Length{get;}
        }

        internal class AnsiCharBuffer : CharBuffer
        {
            internal byte[] buffer;
            internal int offset;

            internal AnsiCharBuffer(int size)
            {
                buffer = new byte[size];
            }

            internal override int Length
            {
                get { return buffer.Length; }
            }

            internal override IntPtr AllocCoTaskMem()
            {
                IntPtr result = Marshal.AllocCoTaskMem(buffer.Length);
                Marshal.Copy(buffer, 0, result, buffer.Length);

                return result;
            }

            internal override string GetString()
            {
                int i = offset;
                while (i < buffer.Length && buffer[i] != 0)
                    i++;

                string result = Encoding.Default.GetString(buffer, offset, i - offset);

                if (i < buffer.Length)
                    i++;

                offset = i;

                return result;
            }

            internal override void PutCoTaskMem(IntPtr ptr)
            {
                Marshal.Copy(ptr, buffer, 0, buffer.Length);
                offset = 0;
            }

            internal override void PutString(string s)
            {
                byte[] bytes = Encoding.Default.GetBytes(s);
                int count = Math.Min(bytes.Length, buffer.Length - offset);

                Array.Copy(bytes, 0, buffer, offset, count);

                offset += count;
                if (offset < buffer.Length)
                    buffer[offset++] = 0;
            }
        }

        internal class UnicodeCharBuffer : CharBuffer
        {
            internal char[] buffer;
            internal int offset;

            internal UnicodeCharBuffer(int size)
            {
                buffer = new char[size];
            }

            internal override int Length
            {
                get { return buffer.Length; }
            }

            internal override IntPtr AllocCoTaskMem()
            {
                IntPtr result = Marshal.AllocCoTaskMem(buffer.Length * 2);
                Marshal.Copy(buffer, 0, result, buffer.Length);
                return result;
            }

            internal override String GetString()
            {
                int i = offset;

                while (i < buffer.Length && buffer[i] != 0)
                    i++;

                string result = new string(buffer, offset, i - offset);

                if (i < buffer.Length)
                    i++;

                offset = i;
                return result;
            }

            internal override void PutCoTaskMem(IntPtr ptr)
            {
                Marshal.Copy(ptr, buffer, 0, buffer.Length);
                offset = 0;
            }

            internal override void PutString(string s)
            {
                int count = Math.Min(s.Length, buffer.Length - offset);

                s.CopyTo(0, buffer, offset, count);
                offset += count;

                if (offset < buffer.Length)
                    buffer[offset++] = (char)0;
            }
        }

        public static class CommonHandles
        {
            /// <devdoc>
            ///     handle type for cursors.
            /// </devdoc>
            public static readonly int Cursor       = HandleCollector.RegisterType("Cursor", 20, 500);

            /// <devdoc>
            ///     Handle type for GDI objects.
            /// </devdoc>
            public static readonly int GDI          = HandleCollector.RegisterType("GDI", 50, 500);

            /// <devdoc>
            ///     Handle type for HDC's that count against the Win98 limit of five DC's.  HDC's
            ///     which are not scarce, such as HDC's for bitmaps, are counted as GDIHANDLE's.
            /// </devdoc>
            public static readonly int HDC          = HandleCollector.RegisterType("HDC", 100, 2); // wait for 2 dc's before collecting

            /// <devdoc>
            ///     Handle type for icons.
            /// </devdoc>
            public static readonly int Icon         = HandleCollector.RegisterType("Icon", 20, 500);

            /// <devdoc>
            ///     Handle type for kernel objects.
            /// </devdoc>
            public static readonly int Kernel       = HandleCollector.RegisterType("Kernel", 0, 1000);
        }

        public const int PBT_APMPOWERSTATUSCHANGE = 0x000A;

        [StructLayout(LayoutKind.Sequential)]
        public struct SYSTEM_POWER_STATUS {
            public byte     ACLineStatus;
            public byte     BatteryFlag;
            public byte     BatteryLifePercent;
            public byte     Reserved1;
            public int      BatteryLifeTime;
            public int      BatteryFullLifeTime;
        }

        // WinEvent
        public const int EVENT_SYSTEM_MOVESIZEEND = 0x000B;
        public const int EVENT_OBJECT_STATECHANGE = 0x800A;
        public const int EVENT_OBJECT_FOCUS = 0x8005;
        public const int OBJID_CLIENT = unchecked(unchecked((int)0xFFFFFFFC));
        public const int WINEVENT_OUTOFCONTEXT = 0x0000;

        // the delegate passed to USER for receiving a WinEvent
        internal delegate void WinEventProcDef(int winEventHook, int eventId, IntPtr hwnd, int idObject, int idChild, int eventThread, int eventTime);

        #region WebBrowser Related Definitions
        /// <summary>
        /// Specifies the ReadyState of the WebBrowser control.
        /// Returned by the IWebBrowser2.ReadyState property.
        /// </summary>
        public enum WebBrowserReadyState
        {
            UnInitialized = 0,
            Loading = 1,
            Loaded = 2,
            Interactive = 3,
            Complete = 4
        }
        #endregion WebBrowser Related Definitions

        [StructLayout(LayoutKind.Sequential)]
        public struct RAWINPUTDEVICELIST
        {
            public IntPtr hDevice;
            public uint   dwType;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct RID_DEVICE_INFO_MOUSE
        {
            public uint  dwId;
            public uint  dwNumberOfButtons;
            public uint  dwSampleRate;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct RID_DEVICE_INFO_KEYBOARD
        {
            public uint  dwType;
            public uint  dwSubType;
            public uint  dwKeyboardMode;
            public uint  dwNumberOfFunctionKeys;
            public uint  dwNumberOfIndicators;
            public uint  dwNumberOfKeysTotal;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct RID_DEVICE_INFO_HID
        {
            public uint  dwVendorId;
            public uint  dwProductId;
            public uint  dwVersionNumber;
            public ushort usUsagePage;
            public ushort usUsage;
        }

        [StructLayout(LayoutKind.Explicit)]
        public struct RID_DEVICE_INFO
        {
            [FieldOffset(0)]
            public uint                cbSize;
            [FieldOffset(4)]
            public uint                dwType;
            [FieldOffset(8)]
            public RID_DEVICE_INFO_MOUSE mouse;
            [FieldOffset(8)]
            public RID_DEVICE_INFO_KEYBOARD keyboard;
            [FieldOffset(8)]
            public RID_DEVICE_INFO_HID hid;
        }

        public const uint RIDI_DEVICEINFO = 0x2000000b;
        public const uint RIM_TYPEHID = 2;
        public const ushort HID_USAGE_PAGE_DIGITIZER = 0x0D;
        public const ushort HID_USAGE_DIGITIZER_DIGITIZER = 1;
        public const ushort HID_USAGE_DIGITIZER_PEN = 2;
        public const ushort HID_USAGE_DIGITIZER_LIGHTPEN = 3;
        public const ushort HID_USAGE_DIGITIZER_TOUCHSCREEN = 4;

        [StructLayout(LayoutKind.Sequential)]
        public struct BLENDFUNCTION
        {
            public byte BlendOp;
            public byte BlendFlags;
            public byte SourceConstantAlpha;
            public byte AlphaFormat;
        }

        public const int AC_SRC_OVER  = 0x00000000;
        public const int ULW_ALPHA    = 0x00000002;

        /// <summary>
        /// Contains values that indicate the type of session information to retrieve 
        /// in a call to the WTSQuerySessionInformation function.
        /// </summary>
        public enum WTS_INFO_CLASS
        {
            /// <summary>
            /// A null-terminated string that contains the name of the initial program that Remote Desktop Services runs when the user logs on.
            /// </summary>
            WTSInitialProgram = 0,
            /// <summary>
            /// A null-terminated string that contains the published name of the application that the session is running.
            /// </summary>
            /// <remarks>
            /// Windows Server 2008 R2, Windows 7, Windows Server 2008 and Windows Vista:  This value is not supported
            /// </remarks>
            WTSApplicationName = 1,
            /// <summary>
            /// A null-terminated string that contains the default directory used when launching the initial program.
            /// </summary>
            WTSWorkingDirectory = 2,
            /// <summary>
            /// This value is not used.
            /// </summary>
            WTSOEMId = 3,
            /// <summary>
            /// A ULONG value that contains the session identifier.
            /// </summary>
            WTSSessionId = 4,
            /// <summary>
            /// A null-terminated string that contains the name of the user associated with the session.
            /// </summary>
            WTSUserName = 5,
            /// <summary>
            /// A null-terminated string that contains the name of the Remote Desktop Services session.
            /// </summary>
            /// <remarks>
            /// Despite its name, specifying this type does not return the window station name. Rather, it returns 
            /// the name of the Remote Desktop Services session. Each Remote Desktop Services session is associated 
            /// with an interactive window station. Because the only supported window station name for an interactive 
            /// window station is "WinSta0", each session is associated with its own "WinSta0" window station. For more 
            /// information, <see cref="https://msdn.microsoft.com/en-us/library/ms687096(v=vs.85).aspx">Window Stations</see>
            /// </remarks>
            WTSWinStationName = 6,
            /// <summary>
            /// A null-terminated string that contains the name of the domain to which the logged-on user belongs.
            /// </summary>
            WTSDomainName = 7,
            /// <summary>
            /// The session's current connection state. For more information, <see cref="WTS_CONNECTSTATE_CLASS"/> 
            /// </summary>
            WTSConnectState = 8,
            /// <summary>
            /// A ULONG value that contains the build number of the client.
            /// </summary>
            WTSClientBuildNumber = 9,
            /// <summary>
            /// A null-terminated string that contains the name of the client.
            /// </summary>
            WTSClientName = 10,
            /// <summary>
            /// A null-terminated string that contains the directory in which the client 
            /// is installed.
            /// </summary>
            WTSClientDirectory = 11,
            /// <summary>
            /// A USHORT client-specific product identifier.
            /// </summary>
            WTSClientProductId = 12,
            /// <summary>
            /// A ULONG value that contains a client-specific hardware identifier. This option 
            /// is reserved 
            /// for future use. 
            /// WTSQuerySessionInformation will always return a value of 0.
            /// </summary>
            WTSClientHardwareId = 13,
            /// <summary>
            /// The network type and network address of the client. For more information, 
            /// see WTS_CLIENT_ADDRESS.
            /// The IP address is offset by two bytes from the start of the Address member of the 
            /// WTS_CLIENT_ADDRESS structure.
            /// </summary>
            WTSClientAddress = 14,
            /// <summary>
            /// Information about the display resolution of the client. For more information, 
            /// see WTS_CLIENT_DISPLAY.
            /// </summary>
            WTSClientDisplay = 15,
            /// <summary>
            /// A USHORT value that specifies information about the protocol type for the session. 
            /// This is one of the following values:
            /// 0 : The console session.
            /// 1 : This value is retained for legacy purposes.
            /// 2 : The RDP protocol.
            /// </summary>
            WTSClientProtocolType = 16,
            /// <summary>
            /// This value returns FALSE. If you call GetLastError to get extended error information, 
            /// GetLastError returns ERROR_NOT_SUPPORTED.
            /// </summary>
            /// <remarks>Windows Server 2008 and Windows Vista:  This value is not used.</remarks>
            WTSIdleTime = 17,
            /// <summary>
            /// This value returns FALSE. If you call GetLastError to get extended error information, 
            /// GetLastError returns ERROR_NOT_SUPPORTED.
            /// </summary>
            /// <remarks>Windows Server 2008 and Windows Vista:  This value is not used.</remarks>
            WTSLogonTime = 18,
            /// <summary>
            /// This value returns FALSE. If you call GetLastError to get extended error information, 
            /// GetLastError returns ERROR_NOT_SUPPORTED.
            /// </summary>
            /// <remarks>Windows Server 2008 and Windows Vista:  This value is not used.</remarks>
            WTSIncomingBytes = 19,
            /// <summary>
            /// This value returns FALSE. If you call GetLastError to get extended error information, 
            /// GetLastError returns ERROR_NOT_SUPPORTED.
            /// </summary>
            /// <remarks>Windows Server 2008 and Windows Vista:  This value is not used.</remarks>
            WTSOutgoingBytes = 20,
            /// <summary>
            /// This value returns FALSE. If you call GetLastError to get extended error information, 
            /// GetLastError returns ERROR_NOT_SUPPORTED.
            /// </summary>
            /// <remarks>Windows Server 2008 and Windows Vista:  This value is not used.</remarks>
            WTSIncomingFrames = 21,
            /// <summary>
            /// This value returns FALSE. If you call GetLastError to get extended error information, 
            /// GetLastError returns ERROR_NOT_SUPPORTED.
            /// </summary>
            /// <remarks>Windows Server 2008 and Windows Vista:  This value is not used.</remarks>
            WTSOutgoingFrames = 22,
            /// <summary>
            /// Information about a Remote Desktop Connection (RDC) client. For more information, 
            /// see WTSCLIENT.
            /// </summary>
            WTSClientInfo = 23,
            /// <summary>
            /// Information about a client session on a RD Session Host server. For more information, 
            /// see WTSINFO.
            /// </summary>
            WTSSessionInfo = 24,
            /// <summary>
            /// Extended information about a session on a RD Session Host server. For more information, 
            /// see WTSINFOEX.
            /// </summary>
            /// <remarks>Windows Server 2008 and Windows Vista:  This value is not supported.</remarks>
            WTSSessionInfoEx = 25,
            /// <summary>
            /// A WTSCONFIGINFO structure that contains information about the configuration of a RD 
            /// Session Host server.
            /// </summary>
            /// <remarks>Windows Server 2008 and Windows Vista:  This value is not supported.</remarks>
            WTSConfigInfo = 26,
            /// <summary>
            /// This value is not supported.
            /// </summary>
            WTSValidationInfo = 27,
            /// <summary>
            /// A WTS_SESSION_ADDRESS structure that contains the IPv4 address assigned to the session. 
            /// If the session does not have a virtual IP address, the WTSQuerySessionInformation function 
            /// returns ERROR_NOT_SUPPORTED.
            /// </summary>
            /// <remarks>Windows Server 2008 and Windows Vista:  This value is not supported.</remarks>
            WTSSessionAddressV4 = 28,
            /// <summary>
            /// Determines whether the current session is a remote session. The WTSQuerySessionInformation 
            /// function returns a value of TRUE to indicate that the current session is a remote session, 
            /// and FALSE to indicate that the current session is a local session. This value can only be 
            /// used for the local machine, so the hServer parameter of the WTSQuerySessionInformation 
            /// function must contain WTS_CURRENT_SERVER_HANDLE.
            /// </summary>
            /// <remarks>Windows Server 2008 and Windows Vista:  This value is not supported.</remarks>
            WTSIsRemoteSession = 29
        }

        /// <summary>
        /// Specifies the connection state of a Remote Desktop Services session.
        /// </summary>;
        /// <remarks>
        /// Only WTSActive represents a fully connected user session. All other
        /// states represent a disconnected user session.
        /// </remarks>
        public enum WTS_CONNECTSTATE_CLASS
        {
            /// <summary>
            /// A user is logged on to the WinStation.
            /// </summary>
            WTSActive = 0,
            /// <summary>
            /// The WinStation is connected to the client.
            /// </summary>
            WTSConnected = 1,
            /// <summary>
            /// The WinStation is in the process of connecting to the client.
            /// </summary>
            WTSConnectQuery = 2,
            /// <summary>
            /// The WinStation is shadowing another WinStation.
            /// </summary>
            WTSShadow = 3,
            /// <summary>
            /// The WinStation is active but the client is disconnected.
            /// </summary>
            WTSDisconnected = 4,
            /// <summary>
            /// The WinStation is waiting for a client to connect.
            /// </summary>
            WTSIdle = 5,
            /// <summary>
            /// The WinStation is listening for a connection. A listener session waits for requests for 
            /// new client connections. 
            /// No user is logged on a listener session. A listener session cannot be reset, shadowed, or 
            /// changed to a regular client session.
            /// </summary>
            WTSListen = 6,
            /// <summary>
            /// The WinStation is being reset.
            /// </summary>
            WTSReset = 7,
            /// <summary>
            /// The WinStation is down due to an error.
            /// </summary>
            WTSDown = 8,
            /// <summary>
            /// The WinStation is initializing.
            /// </summary>
            WTSInit = 9
        }

        /// <summary>
        /// Specifies the current server
        /// </summary>
        public static readonly IntPtr WTS_CURRENT_SERVER_HANDLE = IntPtr.Zero;

        /// <summary>
        /// Specifies the current session (SessionId)
        /// </summary>
        public const int WTS_CURRENT_SESSION = -1;

        /// <summary>
        /// Equivalent to Win32 PROCESS_DPI_AWARENESS
        /// Identifies dots per inch (dpi) awareness values. DPI awareness
        /// indicates how much scaling work an application performs for DPI
        /// versus how much is done by the system.
        /// </summary>
        /// <remarks>
        /// Currently, the DPI awareness is defined on an individual thread, 
        /// window or process level and is indicated by the DPI_AWARENESS (
        /// which is very similar to, but slightly different from, 
        /// PROCESS_DPI_AWARENESS) returned by functions like 
        /// GetThreadDpiAwareness or GetWindowDpiAwareness. 
        /// 
        /// The old recommendation was to define DPI awareness level in the
        /// application manifest using the setting <i>dpiAware</i>. Now that
        /// DPI awareness is tied to threads and windows instead of an entire
        /// application, the new setting <i>dpiAwareness</i> is introduced, 
        /// which will overridden any <i>dpiAware</i> entries in the application
        /// manifest. 
        /// 
        /// While it is still recommended to use the manifest, the DPI awareness 
        /// can be changed while the app is running by using 
        /// SetThreadDpiAwarenessContext. Windows within the application that have
        /// DPI_AWARENESS_PER_MONITOR_AWARE are the responsibility of the application
        /// to update by keeping track of WM_DPICHANGED.
        /// </remarks>
        internal enum PROCESS_DPI_AWARENESS
        {
            /// <summary>
            /// PROCESS_DPI_UNAWARE
            /// This app does not scale for DPI changes and is 
            /// always assumed to have a scale factor of 100% (96 DPI). It 
            /// will be automatically scaled by the system on any other DPI
            /// setting.
            /// </summary>
            PROCESS_DPI_UNAWARE = 0,

            /// <summary>
            /// PROCESS_SYSTEM_DPI_AWARE
            /// The app does not scale for DPI changes. It will query for the DPI once
            /// and use that value for the lifetime of the app. If the DPI changes, 
            /// the app will not adjust to the new DPI value. It will be automatically scaled
            /// up or down by the system when the DPI changes from the system value
            /// </summary>
            PROCESS_SYSTEM_DPI_AWARE = 1,

            /// <summary>
            /// PROCESS_PER_MONITOR_DPI_AWARE
            /// This app checks for the DPI when it is created and adjusts the scale factor
            /// whenever the DPI changes. These applications are not automatically scaled
            /// by the system.
            /// </summary>
            PROCESS_PER_MONITOR_DPI_AWARE = 2,
        }

        /// <summary>
        /// Identifies the dots per inch (dpi) setting for a thread, process
        /// or window
        /// </summary>
        /// <remarks>DPI_AWARENESS  enumeration</remarks>
        internal enum DPI_AWARENESS : int
        {
            /// <summary>
            /// Invalid DPI awareness. This is an invalid DPI awareness value
            /// </summary>
            /// <remarks>DPI_AWARENESS_INVALID</remarks>
            DPI_AWARENESS_INVALID = -1,

            /// <summary>
            /// DPI unaware. This process does not scale for DPI changes and 
            /// is always assumed to have a scale factor of 100% (96 DPI). 
            /// It will be automatically scaled by the system on any other DPI setting.
            /// </summary>
            /// <remarks>DPI_AWARENESS_UNAWARE</remarks>
            DPI_AWARENESS_UNAWARE = 0,

            /// <summary>
            /// System DPI aware. This process does not scale for DPI changes. 
            /// It will query for the DPI once and use that value for the lifetime 
            /// of the process. If the DPI changes, the process will not adjust to 
            /// the new DPI value. It will be automatically scaled up or down by 
            /// the system when the DPI changes from the system value.
            /// </summary>
            /// <remarks>DPI_AWARENESS_SYSTEM_AWARE</remarks>
            DPI_AWARENESS_SYSTEM_AWARE = 1,

            /// <summary>
            /// Per monitor DPI aware. This process checks for the DPI when it is 
            /// created and adjusts the scale factor whenever the DPI changes.
            /// These processes are not automatically scaled by the system.
            /// </summary>
            /// <remarks>DPI_AWARENESS_PER_MONITOR_AWARE</remarks>
            DPI_AWARENESS_PER_MONITOR_AWARE = 2
        }

        /// <summary>
        /// Identifies the DPI hosting behavior for a window. This behavior allows windows
        /// created in the thread to host child windows with a different DPI_AWARENESS_CONTEXT
        /// </summary>
        internal enum DPI_HOSTING_BEHAVIOR : int
        {
            /// <summary>
            /// Invalid DPI hosting behavior. This usually occurs if the
            /// previous SetThreadDpiHostingBehavior call used an invalid parameter.
            /// </summary>
            DPI_HOSTING_BEHAVIOR_INVALID = -1,

            /// <summary>
            /// Default DPI hosting behavior. The associated window behaves as normal,
            /// and cannot create or re-parent child windows with a different DPI_AWARENESS_CONTEXT.
            /// </summary>
            DPI_HOSTING_BEHAVIOR_DEFAULT = 0,

            /// <summary>
            /// Mixed DPI hosting behavior. This enables the creation and re-parenting of child
            /// windows with different DPI_AWARENESS_CONTEXT. These child windows will be independently scaled by the OS.
            /// </summary>
            DPI_HOSTING_BEHAVIOR_MIXED = 1
        }

#if !DRT && !UIAUTOMATIONTYPES

        /// <summary>
        /// DPI unaware. This windows do not scale for DPI changes and
        /// is always assumed to have a scale factor of 100% (96 DPI).
        /// </summary>
        /// <remarks>
        /// <code><![CDATA[#define DPI_AWARENESS_CONTEXT_UNAWARE              ((DPI_AWARENESS_CONTEXT)-1)]]></code>
        /// </remarks>
        internal static readonly DpiAwarenessContextHandle DPI_AWARENESS_CONTEXT_UNAWARE = DpiAwarenessContextHandle.DPI_AWARENESS_CONTEXT_UNAWARE;

        /// <summary>
        /// System DPI aware. This window does not scale for DPI changes. It will 
        /// query for the DPI once and use that value for the lifetime of the
        /// process. If the DPI changes, the process will not adjust to the new DPI
        /// value.
        /// </summary>
        /// <remarks>
        /// <code><![CDATA[#define DPI_AWARENESS_CONTEXT_SYSTEM_AWARE         ((DPI_AWARENESS_CONTEXT)-2)]]></code>
        /// </remarks>
        internal static readonly DpiAwarenessContextHandle DPI_AWARENESS_CONTEXT_SYSTEM_AWARE = DpiAwarenessContextHandle.DPI_AWARENESS_CONTEXT_SYSTEM_AWARE;

        /// <summary>
        /// Per monitor DPI aware. This window checks for the DPI when it is created
        /// and adjusts the scale factor whenever the DPI changes. These processes
        /// are not automatically scaled by the system.
        /// </summary>
        /// <remarks>
        /// <code><![CDATA[#define DPI_AWARENESS_CONTEXT_PER_MONITOR_AWARE    ((DPI_AWARENESS_CONTEXT)-3)]]></code>
        /// </remarks>
        internal static readonly DpiAwarenessContextHandle DPI_AWARENESS_CONTEXT_PER_MONITOR_AWARE = DpiAwarenessContextHandle.DPI_AWARENESS_CONTEXT_PER_MONITOR_AWARE;

        /// <summary>
        /// Also known as Per Monitor v2, this is an advancement over the original
        /// per-monitor DPI awareness mode, which enables applications to access new DPI
        /// related scaling behaviors on a per top-level window basis. Per Monitor v2 was
        /// made available in the Creators Update of Windows 10 (v1703). The additional 
        /// behaviors available are as follows:
        /// - Child window DPI change notifications
        /// - Automatic scaling of non-client area
        /// - Scaling of Win32 menus
        /// - Dialog scaling
        /// - Improved scaling of ComCtl32 controls
        /// - Improved theming behavior
        /// </summary>
        /// <remarks>
        /// <code><![CDATA[#define DPI_AWARENESS_CONTEXT_PER_MONITOR_AWARE_V2 ((DPI_AWARENESS_CONTEXT)-4)]]></code>
        /// </remarks>
        internal static readonly DpiAwarenessContextHandle DPI_AWARENESS_CONTEXT_PER_MONITOR_AWARE_V2 = DpiAwarenessContextHandle.DPI_AWARENESS_CONTEXT_PER_MONITOR_AWARE_V2;

#endif

        /// <summary>
        /// A MonitorEnumProc function is an application-defined callback function that
        /// is called by the EnumDisplayMonitors function.
        /// A value of type MONITORENUMPROC is a pointer to a MonitorEnumProc function.
        /// </summary>
        /// <param name="hMonitor">A handle to the display monitor. This value will always be non-NULL.</param>
        /// <param name="hdcMonitor">
        /// A handle to a device context.
        /// 
        /// The device context has color attributes that are appropriate for the display monitor identified
        /// by hMonitor.The clipping area of the device context is set to the intersection of the visible
        /// region of the device context identified by the hdc parameter of EnumDisplayMonitors, the
        /// rectangle pointed to by the lprcClip parameter of EnumDisplayMonitors, and the display monitor
        /// rectangle.
        /// 
        /// This value is NULL if the hdc parameter of EnumDisplayMonitors was NULL.
        /// </param>
        /// <param name="lprcMonitor">
        /// A pointer to a RECT structure.
        /// 
        /// If hdcMonitor is non-NULL, this rectangle is the intersection of the clipping area of the device
        /// context identified by hdcMonitor and the display monitor rectangle.The rectangle coordinates are
        /// device-context coordinates.
        /// 
        /// If hdcMonitor is NULL, this rectangle is the display monitor rectangle. The rectangle coordinates
        /// are virtual-screen coordinates.
        /// </param>
        /// <param name="dwData">
        /// Application-defined data that EnumDisplayMonitors passes directly to
        /// the enumeration function.
        /// </param>
        /// <returns>
        /// To continue the enumeration, return true.
        /// To stop the enumeration, return false.
        /// </returns>
        [return: MarshalAs(UnmanagedType.Bool)]
        internal delegate bool MonitorEnumProc(IntPtr hMonitor, IntPtr hdcMonitor, ref RECT lprcMonitor, IntPtr dwData);

        /// <summary>
        /// Identifies the dots per inch (dpi) setting for a monitor.
        /// </summary>
        internal enum MONITOR_DPI_TYPE : int
        {
            /// <summary>
            /// The effective DPI. This value should be used when determining the
            /// correct scale factor for scaling UI elements. This incorporates the
            /// scale factor set by the user for this specific display.
            /// </summary>
            MDT_EFFECTIVE_DPI = 0,

            /// <summary>
            /// The angular DPI. This DPI ensures rendering at a compliant angular
            /// resolution on the screen. This does not include the scale factor set
            /// by the user for this specific display.
            /// </summary>
            MDT_ANGULAR_DPI = 1,

            /// <summary>
            /// The raw DPI. This value is the linear DPI of the screen as measured
            /// on the screen itself. Use this value when you want to read the pixel
            /// density and not the recommended scaling setting. This does not include
            /// the scale factor set by the user for this specific display and is not
            /// guaranteed to be a supported DPI value.
            /// </summary>
            MDT_RAW_DPI = 2,

            /// <summary>
            /// The default DPI setting for a monitor is MDT_EFFECTIVE_DPI.
            /// </summary>
            MDT_DEFAULT = MDT_EFFECTIVE_DPI
        }
    }
}

