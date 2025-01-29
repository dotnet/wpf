// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#pragma warning disable 649 // Disable CS0649: "field is never assigned to"

using System.Runtime.InteropServices;
using System;
using System.Text;
#if !DRT && !UIAUTOMATIONTYPES
using MS.Internal.Interop;
using MS.Utility;
#endif
// The SecurityHelper class differs between assemblies and could not actually be
//  shared, so it is duplicated across namespaces to prevent name collision.
#if WINDOWS_BASE
using MS.Internal.WindowsBase;
#elif PRESENTATION_CORE
    using MS.Internal.PresentationCore;
#elif PRESENTATIONFRAMEWORK
    using MS.Internal.PresentationFramework;
#elif UIAUTOMATIONTYPES
    using MS.Internal.UIAutomationTypes;
#elif DRT
    using MS.Internal.Drt;
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
                this.eM11 = this.eM22 = 1; // identity matrix.
            }

            public XFORM( float em11, float em12, float em21, float em22, float edx, float edy )
            {
                this.eM11 = em11;
                this.eM12 = em12;
                this.eM21 = em21;
                this.eM22 = em22;
                this.eDx  = edx;
                this.eDy  = edy;
            }

            public XFORM( float[] elements )
            {
                this.eM11 = elements[0];
                this.eM12 = elements[1];
                this.eM21 = elements[2];
                this.eM22 = elements[3];
                this.eDx  = elements[4];
                this.eDy  = elements[5];
            }

            public override string ToString()
            {
                return String.Format(System.Globalization.CultureInfo.CurrentCulture,"[{0}, {1}, {2}, {3}, {4}, {5}]", this.eM11, this.eM12, this.eM21, this.eM22, this.eDx, this.eDy );
            }

            public override bool Equals( object obj )
            {
                if (obj is not XFORM xform)
                {
                    return false;
                }

                return this.eM11 == xform.eM11 &&
                       this.eM12 == xform.eM12 &&
                       this.eM21 == xform.eM21 &&
                       this.eM22 == xform.eM22 &&
                       this.eDx  == xform.eDx  &&
                       this.eDy  == xform.eDy;
            }

            public override int GetHashCode()
            {
                return this.ToString().GetHashCode();
            }
}
#endif

        public static IntPtr InvalidIntPtr = (IntPtr)(-1);
        public static IntPtr LPSTR_TEXTCALLBACK = (IntPtr)(-1);
        public static HandleRef NullHandleRef = new HandleRef(null, IntPtr.Zero);

        public const int ERROR = 0;

        public const int BITMAPINFO_MAX_COLORSIZE = 256;

        public const int
            PAGE_READWRITE = 0x04,
            FILE_MAP_READ = 0x0004;

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
        cmb4 = 0x0473,
        CS_DBLCLKS = 0x0008,
        CS_DROPSHADOW = 0x00020000,
        CS_SAVEBITS = 0x0800,
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
        CLSCTX_INPROC_SERVER    = 0x1,
        CLSCTX_LOCAL_SERVER     = 0x4,
        CW_USEDEFAULT = (unchecked((int)0x80000000)),
        CWP_SKIPINVISIBLE = 0x0001,
        COLOR_WINDOW = 5,
        CB_ERR = (-1),
        CBN_SELCHANGE = 1,
        CBN_DBLCLK = 2,
        CBN_EDITCHANGE = 5,
        CBN_EDITUPDATE = 6,
        CBN_DROPDOWN = 7,
        CBN_CLOSEUP  = 8,
        CBN_SELENDOK = 9,
        CBS_SIMPLE = 0x0001,
        CBS_DROPDOWN = 0x0002,
        CBS_DROPDOWNLIST = 0x0003,
        CBS_OWNERDRAWFIXED = 0x0010,
        CBS_OWNERDRAWVARIABLE = 0x0020,
        CBS_AUTOHSCROLL = 0x0040,
        CBS_HASSTRINGS = 0x0200,
        CBS_NOINTEGRALHEIGHT = 0x0400,
        CB_GETEDITSEL = 0x0140,
        CB_LIMITTEXT = 0x0141,
        CB_SETEDITSEL = 0x0142,
        CB_ADDSTRING = 0x0143,
        CB_DELETESTRING = 0x0144,
        CB_GETCURSEL = 0x0147,
        CB_GETLBTEXT = 0x0148,
        CB_GETLBTEXTLEN = 0x0149,
        CB_INSERTSTRING = 0x014A,
        CB_RESETCONTENT = 0x014B,
        CB_FINDSTRING = 0x014C,
        CB_SETCURSEL = 0x014E,
        CB_SHOWDROPDOWN = 0x014F,
        CB_GETITEMDATA = 0x0150,
        CB_SETITEMHEIGHT = 0x0153,
        CB_GETITEMHEIGHT = 0x0154,
        CB_GETDROPPEDSTATE = 0x0157,
        CB_FINDSTRINGEXACT = 0x0158,
        CB_SETDROPPEDWIDTH = 0x0160,
        CDRF_DODEFAULT = 0x00000000,
        CDRF_NEWFONT = 0x00000002,
        CDRF_SKIPDEFAULT = 0x00000004,
        CDRF_NOTIFYPOSTPAINT = 0x00000010,
        CDRF_NOTIFYITEMDRAW = 0x00000020,
        CDRF_NOTIFYSUBITEMDRAW = CDRF_NOTIFYITEMDRAW,
        CDDS_PREPAINT = 0x00000001,
        CDDS_POSTPAINT = 0x00000002,
        CDDS_ITEM = 0x00010000,
        CDDS_SUBITEM = 0x00020000,
        CDDS_ITEMPREPAINT = (0x00010000|0x00000001),
        CDDS_ITEMPOSTPAINT = (0x00010000|0x00000002),
        CDIS_SELECTED = 0x0001,
        CDIS_GRAYED = 0x0002,
        CDIS_DISABLED = 0x0004,
        CDIS_CHECKED = 0x0008,
        CDIS_FOCUS = 0x0010,
        CDIS_DEFAULT = 0x0020,
        CDIS_HOT = 0x0040,
        CDIS_MARKED = 0x0080,
        CDIS_INDETERMINATE = 0x0100,
        CDIS_SHOWKEYBOARDCUES = 0x0200,
        CLR_NONE = unchecked((int)0xFFFFFFFF),
        CLR_DEFAULT = unchecked((int)0xFF000000),
        CCM_SETVERSION = (0x2000+0x7),
        CCM_GETVERSION = (0x2000+0x8),
        CCS_NORESIZE = 0x00000004,
        CCS_NOPARENTALIGN = 0x00000008,
        CCS_NODIVIDER = 0x00000040,
        CBEM_INSERTITEMA = (0x0400+1),
        CBEM_GETITEMA = (0x0400+4),
        CBEM_SETITEMA = (0x0400+5),
        CBEM_INSERTITEMW = (0x0400+11),
        CBEM_SETITEMW = (0x0400+12),
        CBEM_GETITEMW = (0x0400+13),
        CBEN_ENDEDITA = ((0-800)-5),
        CBEN_ENDEDITW = ((0-800)-6),
        CONNECT_E_NOCONNECTION = unchecked((int)0x80040200),
        CONNECT_E_CANNOTCONNECT = unchecked((int)0x80040202),
        CTRLINFO_EATS_RETURN    = 1,
        CTRLINFO_EATS_ESCAPE    = 2,
        CSIDL_DESKTOP                    = 0x0000,        // <desktop>
        CSIDL_INTERNET                   = 0x0001,        // Internet Explorer (icon on desktop)
        CSIDL_PROGRAMS                   = 0x0002,        // Start Menu\Programs
        CSIDL_PERSONAL                   = 0x0005,        // My Documents
        CSIDL_FAVORITES                  = 0x0006,        // <user name>\Favorites
        CSIDL_STARTUP                    = 0x0007,        // Start Menu\Programs\Startup
        CSIDL_RECENT                     = 0x0008,        // <user name>\Recent
        CSIDL_SENDTO                     = 0x0009,        // <user name>\SendTo
        CSIDL_STARTMENU                  = 0x000b,        // <user name>\Start Menu
        CSIDL_DESKTOPDIRECTORY           = 0x0010,        // <user name>\Desktop
        CSIDL_TEMPLATES                  = 0x0015,
        CSIDL_APPDATA                    = 0x001a,        // <user name>\Application Data
        CSIDL_LOCAL_APPDATA              = 0x001c,        // <user name>\Local Settings\Applicaiton Data (non roaming)
        CSIDL_INTERNET_CACHE             = 0x0020,
        CSIDL_COOKIES                    = 0x0021,
        CSIDL_HISTORY                    = 0x0022,
        CSIDL_COMMON_APPDATA             = 0x0023,        // All Users\Application Data
        CSIDL_SYSTEM                     = 0x0025,        // GetSystemDirectory()
        CSIDL_PROGRAM_FILES              = 0x0026,        // C:\Program Files
        CSIDL_PROGRAM_FILES_COMMON       = 0x002b;        // C:\Program Files\Common

        public const int DUPLICATE = 0x06,
        DISPID_VALUE = 0,
        DISPID_UNKNOWN = (-1),
        DISPID_PROPERTYPUT = (-3),
        DISPATCH_METHOD = 0x1,
        DISPATCH_PROPERTYGET = 0x2,
        DISPATCH_PROPERTYPUT = 0x4,
        DISPATCH_PROPERTYPUTREF = 0x8,
        DV_E_DVASPECT = unchecked((int)0x8004006B),
        DEFAULT_GUI_FONT = 17,
        DIB_RGB_COLORS = 0,
        DRAGDROP_E_NOTREGISTERED = unchecked((int)0x80040100),
        DRAGDROP_E_ALREADYREGISTERED = unchecked((int)0x80040101),
        DUPLICATE_SAME_ACCESS = 0x00000002,
        DFC_CAPTION = 1,
        DFC_MENU = 2,
        DFC_SCROLL = 3,
        DFC_BUTTON = 4,
        DFCS_CAPTIONCLOSE = 0x0000,
        DFCS_CAPTIONMIN = 0x0001,
        DFCS_CAPTIONMAX = 0x0002,
        DFCS_CAPTIONRESTORE = 0x0003,
        DFCS_CAPTIONHELP = 0x0004,
        DFCS_MENUARROW = 0x0000,
        DFCS_MENUCHECK = 0x0001,
        DFCS_MENUBULLET = 0x0002,
        DFCS_SCROLLUP = 0x0000,
        DFCS_SCROLLDOWN = 0x0001,
        DFCS_SCROLLLEFT = 0x0002,
        DFCS_SCROLLRIGHT = 0x0003,
        DFCS_SCROLLCOMBOBOX = 0x0005,
        DFCS_BUTTONCHECK = 0x0000,
        DFCS_BUTTONRADIO = 0x0004,
        DFCS_BUTTON3STATE = 0x0008,
        DFCS_BUTTONPUSH = 0x0010,
        DFCS_INACTIVE = 0x0100,
        DFCS_PUSHED = 0x0200,
        DFCS_CHECKED = 0x0400,
        DFCS_FLAT = 0x4000,
        DT_LEFT = 0x00000000,
        DT_RIGHT = 0x00000002,
        DT_VCENTER = 0x00000004,
        DT_SINGLELINE = 0x00000020,
        DT_NOCLIP = 0x00000100,
        DT_CALCRECT = 0x00000400,
        DT_NOPREFIX = 0x00000800,
        DT_EDITCONTROL = 0x00002000,
        DT_EXPANDTABS  = 0x00000040,
        DT_END_ELLIPSIS = 0x00008000,
        DT_RTLREADING = 0x00020000,
        DT_WORDBREAK = 0x00000010,
        DCX_WINDOW = 0x00000001,
        DCX_CACHE = 0x00000002,
        DCX_LOCKWINDOWUPDATE = 0x00000400,
        DI_NORMAL = 0x0003,
        DLGC_WANTARROWS = 0x0001,
        DLGC_WANTTAB = 0x0002,
        DLGC_WANTALLKEYS = 0x0004,
        DLGC_WANTCHARS = 0x0080,
        DTM_GETSYSTEMTIME = (0x1000+1),
        DTM_SETSYSTEMTIME = (0x1000+2),
        DTM_SETRANGE = (0x1000+4),
        DTM_SETFORMATA = (0x1000+5),
        DTM_SETFORMATW = (0x1000+50),
        DTM_SETMCCOLOR = (0x1000+6),
        DTM_SETMCFONT = (0x1000+9),
        DTS_UPDOWN = 0x0001,
        DTS_SHOWNONE = 0x0002,
        DTS_LONGDATEFORMAT = 0x0004,
        DTS_TIMEFORMAT = 0x0009,
        DTS_RIGHTALIGN = 0x0020,
        DTN_DATETIMECHANGE = ((0-760)+1),
        DTN_USERSTRINGA = ((0-760)+2),
        DTN_USERSTRINGW = ((0-760)+15),
        DTN_WMKEYDOWNA = ((0-760)+3),
        DTN_WMKEYDOWNW = ((0-760)+16),
        DTN_FORMATA = ((0-760)+4),
        DTN_FORMATW = ((0-760)+17),
        DTN_FORMATQUERYA = ((0-760)+5),
        DTN_FORMATQUERYW = ((0-760)+18),
        DTN_DROPDOWN = ((0-760)+6),
        DTN_CLOSEUP = ((0-760)+7),
        DVASPECT_CONTENT   = 1,
        DVASPECT_TRANSPARENT = 32,
        DVASPECT_OPAQUE    = 16;

        public const int E_NOTIMPL = unchecked((int)0x80004001),
        E_OUTOFMEMORY = unchecked((int)0x8007000E),
        E_INVALIDARG = unchecked((int)0x80070057),
        E_NOINTERFACE = unchecked((int)0x80004002),
        E_FAIL = unchecked((int)0x80004005),
        E_ABORT = unchecked((int)0x80004004),
        E_ACCESSDENIED = unchecked((int)0x80070005),
        E_UNEXPECTED = unchecked((int)0x8000FFFF),
        INET_E_DEFAULT_ACTION = unchecked((int)0x800C0011),
        ETO_OPAQUE = 0x0002,
        ETO_CLIPPED = 0x0004,
        EMR_POLYTEXTOUTA = 96,
        EMR_POLYTEXTOUTW = 97,
        EDGE_RAISED = (0x0001|0x0004),
        EDGE_SUNKEN = (0x0002|0x0008),
        EDGE_ETCHED = (0x0002|0x0004),
        EDGE_BUMP = (0x0001|0x0008),
        ES_LEFT = 0x0000,
        ES_CENTER = 0x0001,
        ES_RIGHT = 0x0002,
        ES_MULTILINE = 0x0004,
        ES_UPPERCASE = 0x0008,
        ES_LOWERCASE = 0x0010,
        ES_AUTOVSCROLL = 0x0040,
        ES_AUTOHSCROLL = 0x0080,
        ES_NOHIDESEL = 0x0100,
        ES_READONLY = 0x0800,
        ES_PASSWORD = 0x0020,
        EN_CHANGE = 0x0300,
        EN_UPDATE = 0x0400,
        EN_HSCROLL = 0x0601,
        EN_VSCROLL = 0x0602,
        EN_ALIGN_LTR_EC = 0x0700,
        EN_ALIGN_RTL_EC = 0x0701,
        EC_LEFTMARGIN = 0x0001,
        EC_RIGHTMARGIN = 0x0002,
        EM_GETSEL = 0x00B0,
        EM_SETSEL = 0x00B1,
        EM_SCROLL = 0x00B5,
        EM_SCROLLCARET = 0x00B7,
        EM_GETMODIFY = 0x00B8,
        EM_SETMODIFY = 0x00B9,
        EM_GETLINECOUNT = 0x00BA,
        EM_REPLACESEL = 0x00C2,
        EM_GETLINE = 0x00C4,
        EM_LIMITTEXT = 0x00C5,
        EM_CANUNDO = 0x00C6,
        EM_UNDO = 0x00C7,
        EM_SETPASSWORDCHAR = 0x00CC,
        EM_GETPASSWORDCHAR = 0x00D2,
        EM_EMPTYUNDOBUFFER = 0x00CD,
        EM_SETREADONLY = 0x00CF,
        EM_SETMARGINS = 0x00D3,
        EM_POSFROMCHAR = 0x00D6,
        EM_CHARFROMPOS = 0x00D7,
        EM_LINEFROMCHAR = 0x00C9,
        EM_LINEINDEX = 0x00BB;

        public const int FNERR_SUBCLASSFAILURE = 0x3001,
        FNERR_INVALIDFILENAME = 0x3002,
        FNERR_BUFFERTOOSMALL = 0x3003;

        public const int GMEM_MOVEABLE = 0x0002,
        GMEM_ZEROINIT = 0x0040,
        GMEM_DDESHARE = 0x2000,
        GCL_WNDPROC = (-24),
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
        GMR_VISIBLE = 0,
        GMR_DAYSTATE = 1,
        GDI_ERROR = (unchecked((int)0xFFFFFFFF)),
        GDTR_MIN = 0x0001,
        GDTR_MAX = 0x0002,
        GDT_VALID = 0,
        GDT_NONE = 1,
        GA_PARENT = 1,
        GA_ROOT   = 2;

        // ImmGetCompostionString index.
        public const int
        GCS_COMPREADSTR       = 0x0001,
        GCS_COMPREADATTR      = 0x0002,
        GCS_COMPREADCLAUSE    = 0x0004,
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
        IMN_CLOSESTATUSWINDOW           = 0x0001,
        IMN_OPENSTATUSWINDOW            = 0x0002,
        IMN_CHANGECANDIDATE             = 0x0003,
        IMN_CLOSECANDIDATE              = 0x0004,
        IMN_OPENCANDIDATE               = 0x0005,
        IMN_SETCONVERSIONMODE           = 0x0006,
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
        IME_CMODE_RESERVED          = unchecked((int)0xF0000000),

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
        HC_ACTION = 0,
        HC_GETNEXT = 1,
        HC_SKIP = 2,
        HTNOWHERE = 0,
        HTCLIENT = 1,
        HTBOTTOM = 15,
        HTTRANSPARENT = (-1),
        HTBOTTOMLEFT = 16,
        HTBOTTOMRIGHT = 17,
        HELPINFO_WINDOW = 0x0001,
        HCF_HIGHCONTRASTON = 0x00000001,
        HDI_ORDER = 0x0080,
        HDI_WIDTH = 0x0001,
        HDM_GETITEMCOUNT = (0x1200+0),
        HDM_INSERTITEMA = (0x1200+1),
        HDM_INSERTITEMW = (0x1200+10),
        HDM_GETITEMA = (0x1200+3),
        HDM_GETITEMW = (0x1200+11),
        HDM_SETITEMA = (0x1200+4),
        HDM_SETITEMW = (0x1200+12),
        HDN_ITEMCHANGINGA = ((0-300)-0),
        HDN_ITEMCHANGINGW = ((0-300)-20),
        HDN_ITEMCHANGEDA = ((0-300)-1),
        HDN_ITEMCHANGEDW = ((0-300)-21),
        HDN_ITEMCLICKA = ((0-300)-2),
        HDN_ITEMCLICKW = ((0-300)-22),
        HDN_ITEMDBLCLICKA = ((0-300)-3),
        HDN_ITEMDBLCLICKW = ((0-300)-23),
        HDN_DIVIDERDBLCLICKA = ((0-300)-5),
        HDN_DIVIDERDBLCLICKW = ((0-300)-25),
        HDN_BEGINTDRAG = ((0-300)-10),
        HDN_BEGINTRACKA = ((0-300)-6),
        HDN_BEGINTRACKW = ((0-300)-26),
        HDN_ENDDRAG = ((0-300)-11),
        HDN_ENDTRACKA = ((0-300)-7),
        HDN_ENDTRACKW = ((0-300)-27),
        HDN_TRACKA = ((0-300)-8),
        HDN_TRACKW = ((0-300)-28),
        HDN_GETDISPINFOA = ((0-300)-9),
        HDN_GETDISPINFOW = ((0-300)-29);
        // HOVER_DEFAULT = Do not use this value ever! It crashes entire servers.

        public static HandleRef HWND_TOP = new HandleRef(null, (IntPtr)0);
        public static HandleRef HWND_BOTTOM = new HandleRef(null, (IntPtr)1);
        public static HandleRef HWND_TOPMOST = new HandleRef(null, new IntPtr(-1));
        public static HandleRef HWND_NOTOPMOST = new HandleRef(null, new IntPtr(-2));
        // NOTE:  NativeMethodsOther.cs defines the following
        //public static IntPtr HWND_MESSAGE = new IntPtr(-3);

        public const int INPLACE_E_NOTOOLSPACE = unchecked((int)0x800401A1),
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
        IMAGE_ICON = 1,
        IMAGE_CURSOR = 2,
        ICC_LISTVIEW_CLASSES = 0x00000001,
        ICC_TREEVIEW_CLASSES = 0x00000002,
        ICC_BAR_CLASSES = 0x00000004,
        ICC_TAB_CLASSES = 0x00000008,
        ICC_PROGRESS_CLASS = 0x00000020,
        ICC_DATE_CLASSES = 0x00000100,
        ILC_MASK = 0x0001,
        ILC_COLOR = 0x0000,
        ILC_COLOR4 = 0x0004,
        ILC_COLOR8 = 0x0008,
        ILC_COLOR16 = 0x0010,
        ILC_COLOR24 = 0x0018,
        ILC_COLOR32 = 0x0020,
        ILC_MIRROR = 0x00002000,
        ILD_NORMAL = 0x0000,
        ILD_TRANSPARENT = 0x0001,
        ILD_MASK = 0x0010,
        ILD_ROP = 0x0040,

        // ImageList
        //
        ILP_NORMAL = 0,
        ILP_DOWNLEVEL = 1,
        ILS_NORMAL = 0x0,
        ILS_GLOW = 0x1,
        ILS_SHADOW = 0x2,
        ILS_SATURATE = 0x4,
        ILS_ALPHA = 0x8;

        public const int CSC_NAVIGATEFORWARD = 0x00000001,
        CSC_NAVIGATEBACK = 0x00000002;

        public const int STG_E_CANTSAVE = unchecked((int)0x80030103);

        public const int LOGPIXELSX = 88,
        LOGPIXELSY = 90,
        LB_ERR = (-1),
        LB_ERRSPACE = (-2),
        LBN_SELCHANGE = 1,
        LBN_DBLCLK = 2,
        LB_ADDSTRING = 0x0180,
        LB_INSERTSTRING = 0x0181,
        LB_DELETESTRING = 0x0182,
        LB_RESETCONTENT = 0x0184,
        LB_SETSEL = 0x0185,
        LB_SETCURSEL = 0x0186,
        LB_GETSEL = 0x0187,
        LB_GETCARETINDEX = 0x019F,
        LB_GETCURSEL = 0x0188,
        LB_GETTEXT = 0x0189,
        LB_GETTEXTLEN = 0x018A,
        LB_GETTOPINDEX = 0x018E,
        LB_FINDSTRING = 0x018F,
        LB_GETSELCOUNT = 0x0190,
        LB_GETSELITEMS = 0x0191,
        LB_SETTABSTOPS = 0x0192,
        LB_SETHORIZONTALEXTENT = 0x0194,
        LB_SETCOLUMNWIDTH = 0x0195,
        LB_SETTOPINDEX = 0x0197,
        LB_GETITEMRECT = 0x0198,
        LB_SETITEMHEIGHT = 0x01A0,
        LB_GETITEMHEIGHT = 0x01A1,
        LB_FINDSTRINGEXACT = 0x01A2,
        LB_ITEMFROMPOINT = 0x01A9,
        LB_SETLOCALE = 0x01A5;

        public const int LWA_ALPHA = 0x00000002;

        public const int MEMBERID_NIL = (-1),
        MAX_PATH = 260,
        MA_ACTIVATE = 0x0001,
        MA_ACTIVATEANDEAT = 0x0002,
        MA_NOACTIVATE = 0x0003,
        MA_NOACTIVATEANDEAT = 0x0004,
        MM_TEXT = 1,
        MM_ANISOTROPIC = 8,
        MK_LBUTTON = 0x0001,
        MK_RBUTTON = 0x0002,
        MK_SHIFT = 0x0004,
        MK_CONTROL = 0x0008,
        MK_MBUTTON = 0x0010,
        MNC_EXECUTE = 2,
        MNC_SELECT = 3,
        MIIM_STATE = 0x00000001,
        MIIM_ID = 0x00000002,
        MIIM_SUBMENU = 0x00000004,
        MIIM_TYPE = 0x00000010,
        MIIM_DATA = 0x00000020,
        MIIM_STRING = 0x00000040,
        MIIM_BITMAP = 0x00000080,
        MIIM_FTYPE = 0x00000100,
        MB_OK = 0x00000000,
        MF_BYCOMMAND = 0x00000000,
        MF_BYPOSITION = 0x00000400,
        MF_ENABLED = 0x00000000,
        MF_GRAYED = 0x00000001,
        MF_POPUP = 0x00000010,
        MF_SYSMENU = 0x00002000,
        MFS_DISABLED = 0x00000003,
            MFT_MENUBREAK = 0x00000040,
        MFT_SEPARATOR = 0x00000800,
        MFT_RIGHTORDER = 0x00002000,
        MFT_RIGHTJUSTIFY = 0x00004000,
        MDIS_ALLCHILDSTYLES = 0x0001,
        MDITILE_VERTICAL = 0x0000,
        MDITILE_HORIZONTAL = 0x0001,
        MDITILE_SKIPDISABLED = 0x0002,
        MCM_SETMAXSELCOUNT = (0x1000+4),
        MCM_SETSELRANGE = (0x1000+6),
        MCM_GETMONTHRANGE = (0x1000+7),
        MCM_GETMINREQRECT = (0x1000+9),
        MCM_SETCOLOR = (0x1000+10),
        MCM_SETTODAY = (0x1000+12),
        MCM_GETTODAY = (0x1000+13),
        MCM_HITTEST = (0x1000+14),
        MCM_SETFIRSTDAYOFWEEK = (0x1000+15),
        MCM_SETRANGE = (0x1000+18),
        MCM_SETMONTHDELTA = (0x1000+20),
        MCM_GETMAXTODAYWIDTH = (0x1000+21),
        MCHT_TITLE = 0x00010000,
        MCHT_CALENDAR = 0x00020000,
        MCHT_TODAYLINK = 0x00030000,
        MCHT_TITLEBK = (0x00010000),
        MCHT_TITLEMONTH = (0x00010000|0x0001),
        MCHT_TITLEYEAR = (0x00010000|0x0002),
        MCHT_TITLEBTNNEXT = (0x00010000|0x01000000|0x0003),
        MCHT_TITLEBTNPREV = (0x00010000|0x02000000|0x0003),
        MCHT_CALENDARBK = (0x00020000),
        MCHT_CALENDARDATE = (0x00020000|0x0001),
        MCHT_CALENDARDATENEXT = ((0x00020000|0x0001)|0x01000000),
        MCHT_CALENDARDATEPREV = ((0x00020000|0x0001)|0x02000000),
        MCHT_CALENDARDAY = (0x00020000|0x0002),
        MCHT_CALENDARWEEKNUM = (0x00020000|0x0003),
        MCSC_TEXT = 1,
        MCSC_TITLEBK = 2,
        MCSC_TITLETEXT = 3,
        MCSC_MONTHBK = 4,
        MCSC_TRAILINGTEXT = 5,
        MCN_SELCHANGE = ((0-750)+1),
        MCN_GETDAYSTATE = ((0-750)+3),
        MCN_SELECT = ((0-750)+4),
        MCS_DAYSTATE = 0x0001,
        MCS_MULTISELECT = 0x0002,
        MCS_WEEKNUMBERS = 0x0004,
        MCS_NOTODAYCIRCLE = 0x0008,
        MCS_NOTODAY = 0x0010,
        MSAA_MENU_SIG = (unchecked((int) 0xAA0DF00D));

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

        public const int OFN_READONLY = 0x00000001,
        OFN_OVERWRITEPROMPT = 0x00000002,
        OFN_HIDEREADONLY = 0x00000004,
        OFN_NOCHANGEDIR = 0x00000008,
        //OFN_SHOWHELP = 0x00000010,
        OFN_ENABLEHOOK = 0x00000020,
        OFN_NOVALIDATE = 0x00000100,
        OFN_ALLOWMULTISELECT = 0x00000200,
        OFN_PATHMUSTEXIST = 0x00000800,
        OFN_FILEMUSTEXIST = 0x00001000,
        OFN_CREATEPROMPT = 0x00002000,
        OFN_EXPLORER = 0x00080000,
        OFN_NODEREFERENCELINKS = 0x00100000,
        OFN_ENABLESIZING = 0x00800000,
        OFN_USESHELLITEM = 0x01000000;

        public const int PDERR_SETUPFAILURE = 0x1001,
        PDERR_PARSEFAILURE = 0x1002,
        PDERR_RETDEFFAILURE = 0x1003,
        PDERR_LOADDRVFAILURE = 0x1004,
        PDERR_GETDEVMODEFAIL = 0x1005,
        PDERR_INITFAILURE = 0x1006,
        PDERR_NODEVICES = 0x1007,
        PDERR_NODEFAULTPRN = 0x1008,
        PDERR_DNDMMISMATCH = 0x1009,
        PDERR_CREATEICFAILURE = 0x100A,
        PDERR_PRINTERNOTFOUND = 0x100B,
        PDERR_DEFAULTDIFFERENT = 0x100C,
        PD_ALLPAGES = 0x00000000,
        PD_SELECTION = 0x00000001,
        PD_PAGENUMS = 0x00000002,
        PD_NOSELECTION = 0x00000004,
        PD_NOPAGENUMS = 0x00000008,
        PD_COLLATE = 0x00000010,
        PD_PRINTTOFILE = 0x00000020,
        PD_PRINTSETUP = 0x00000040,
        PD_NOWARNING = 0x00000080,
        PD_RETURNDC = 0x00000100,
        PD_RETURNIC = 0x00000200,
        PD_RETURNDEFAULT = 0x00000400,
        PD_SHOWHELP = 0x00000800,
        PD_ENABLEPRINTHOOK = 0x00001000,
        PD_ENABLESETUPHOOK = 0x00002000,
        PD_ENABLEPRINTTEMPLATE = 0x00004000,
        PD_ENABLESETUPTEMPLATE = 0x00008000,
        PD_ENABLEPRINTTEMPLATEHANDLE = 0x00010000,
        PD_ENABLESETUPTEMPLATEHANDLE = 0x00020000,
        PD_USEDEVMODECOPIES = 0x00040000,
        PD_USEDEVMODECOPIESANDCOLLATE = 0x00040000,
        PD_DISABLEPRINTTOFILE = 0x00080000,
        PD_HIDEPRINTTOFILE = 0x00100000,
        PD_NONETWORKBUTTON = 0x00200000,
        PD_CURRENTPAGE = 0x00400000,
        PD_NOCURRENTPAGE = 0x00800000,
        PD_EXCLUSIONFLAGS = 0x01000000,
        PD_USELARGETEMPLATE = 0x10000000,
        PSD_MINMARGINS = 0x00000001,
        PSD_MARGINS = 0x00000002,
        PSD_INHUNDREDTHSOFMILLIMETERS = 0x00000008,
        PSD_DISABLEMARGINS = 0x00000010,
        PSD_DISABLEPRINTER = 0x00000020,
        PSD_DISABLEORIENTATION = 0x00000100,
        PSD_DISABLEPAPER = 0x00000200,
        PSD_SHOWHELP = 0x00000800,
        PSD_ENABLEPAGESETUPHOOK = 0x00002000,
        PSD_NONETWORKBUTTON = 0x00200000,
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
        PBM_SETRANGE = (0x0400+1),
        PBM_SETPOS = (0x0400+2),
        PBM_SETSTEP = (0x0400+4),
        PBM_SETRANGE32 = (0x0400+6),
        PBM_SETBARCOLOR = (0x0400+9),
        PBM_SETBKCOLOR  = (0x2000 +1),
        PSM_SETTITLEA = (0x0400+111),
        PSM_SETTITLEW = (0x0400+120),
        PSM_SETFINISHTEXTA = (0x0400+115),
        PSM_SETFINISHTEXTW = (0x0400+121),
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

        public const int stc4 = 0x0443,
        SHGFP_TYPE_CURRENT = 0,
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
        SB_HORZ = 0,
        SB_VERT = 1,
        SB_CTL = 2,
        SB_LINEUP = 0,
        SB_LINELEFT = 0,
        SB_LINEDOWN = 1,
        SB_LINERIGHT = 1,
        SB_PAGEUP = 2,
        SB_PAGELEFT = 2,
        SB_PAGEDOWN = 3,
        SB_PAGERIGHT = 3,
        SB_THUMBPOSITION = 4,
        SB_THUMBTRACK = 5,
        SB_LEFT = 6,
        SB_RIGHT = 7,
        SB_ENDSCROLL = 8,
        SB_TOP = 6,
        SB_BOTTOM = 7,
        SIZE_MAXIMIZED = 2,
        ESB_ENABLE_BOTH = 0x0000,
        ESB_DISABLE_BOTH =0x0003,
        SORT_DEFAULT =0x0,
        SUBLANG_DEFAULT = 0x01,
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

        public const int MB_ICONHAND = 0x000010,
        MB_ICONQUESTION = 0x000020,
        MB_ICONEXCLAMATION = 0x000030,
        MB_ICONASTERISK = 0x000040;

        public const int SW_SCROLLCHILDREN = 0x0001,
        SW_INVALIDATE = 0x0002,
        SW_ERASE = 0x0004,
        SW_SMOOTHSCROLL =   0x0010,
        SC_SIZE = 0xF000,
        SC_MINIMIZE = 0xF020,
        SC_MAXIMIZE = 0xF030,
        SC_CLOSE = 0xF060,
        SC_KEYMENU = 0xF100,
        SC_RESTORE = 0xF120,
        SC_MOVE    = 0xF010,
        SS_LEFT = 0x00000000,
        SS_CENTER = 0x00000001,
        SS_RIGHT = 0x00000002,
        SS_OWNERDRAW = 0x0000000D,
        SS_NOPREFIX = 0x00000080,
        SS_SUNKEN = 0x00001000,
        SBS_HORZ = 0x0000,
        SBS_VERT = 0x0001,
        SIF_RANGE = 0x0001,
        SIF_PAGE = 0x0002,
        SIF_POS = 0x0004,
        SIF_TRACKPOS = 0x0010,
        SIF_ALL = (0x0001|0x0002|0x0004|0x0010),
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
        SBARS_SIZEGRIP = 0x0100,
        SB_SETTEXTA = (0x0400+1),
        SB_SETTEXTW = (0x0400+11),
        SB_GETTEXTA = (0x0400+2),
        SB_GETTEXTW = (0x0400+13),
        SB_GETTEXTLENGTHA = (0x0400+3),
        SB_GETTEXTLENGTHW = (0x0400+12),
        SB_SETPARTS = (0x0400+4),
        SB_SIMPLE = (0x0400+9),
        SB_GETRECT = (0x0400+10),
        SB_SETICON = (0x0400+15),
        SB_SETTIPTEXTA = (0x0400+16),
        SB_SETTIPTEXTW = (0x0400+17),
        SB_GETTIPTEXTA = (0x0400+18),
        SB_GETTIPTEXTW = (0x0400+19),
        SBT_OWNERDRAW = 0x1000,
        SBT_NOBORDERS = 0x0100,
        SBT_POPOUT = 0x0200,
        SBT_RTLREADING = 0x0400,
        SRCCOPY = 0x00CC0020,
        SRCAND             = 0x008800C6, /* dest = source AND dest          */
        SRCPAINT           = 0x00EE0086, /* dest = source OR dest           */
        NOTSRCCOPY         = 0x00330008, /* dest = (NOT source)             */
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

        public const int S_OK =      0x00000000;
        public const int S_FALSE =   0x00000001;

        public static bool Succeeded(int hr) {
            return(hr >= 0);
        }

        public static bool Failed(int hr) {
            return(hr < 0);
        }

        public const int TRANSPARENT = 1,
        OPAQUE = 2,
        TME_HOVER = 0x00000001,
        TME_LEAVE = 0x00000002,
        TPM_LEFTBUTTON = 0x0000,
        TPM_RIGHTBUTTON = 0x0002,
        TPM_LEFTALIGN = 0x0000,
        TPM_RIGHTALIGN = 0x0008,
        TPM_VERTICAL = 0x0040,
        TV_FIRST = 0x1100,
        TBSTATE_CHECKED = 0x01,
        TBSTATE_ENABLED = 0x04,
        TBSTATE_HIDDEN = 0x08,
        TBSTATE_INDETERMINATE = 0x10,
        TBSTYLE_BUTTON = 0x00,
        TBSTYLE_SEP = 0x01,
        TBSTYLE_CHECK = 0x02,
        TBSTYLE_DROPDOWN = 0x08,
        TBSTYLE_TOOLTIPS = 0x0100,
        TBSTYLE_FLAT = 0x0800,
        TBSTYLE_LIST = 0x1000,
        TBSTYLE_EX_DRAWDDARROWS = 0x00000001,
        TB_ENABLEBUTTON = (0x0400+1),
        TB_ISBUTTONCHECKED = (0x0400+10),
        TB_ISBUTTONINDETERMINATE = (0x0400+13),
        TB_ADDBUTTONSA = (0x0400+20),
        TB_ADDBUTTONSW = (0x0400+68),
        TB_INSERTBUTTONA = (0x0400+21),
        TB_INSERTBUTTONW = (0x0400+67),
        TB_DELETEBUTTON = (0x0400+22),
        TB_GETBUTTON = (0x0400+23),
        TB_SAVERESTOREA = (0x0400+26),
        TB_SAVERESTOREW = (0x0400+76),
        TB_ADDSTRINGA = (0x0400+28),
        TB_ADDSTRINGW = (0x0400+77),
        TB_BUTTONSTRUCTSIZE = (0x0400+30),
        TB_SETBUTTONSIZE = (0x0400+31),
        TB_AUTOSIZE = (0x0400+33),
        TB_GETROWS = (0x0400+40),
        TB_GETBUTTONTEXTA = (0x0400+45),
        TB_GETBUTTONTEXTW = (0x0400+75),
        TB_SETIMAGELIST = (0x0400+48),
        TB_GETRECT = (0x0400+51),
        TB_GETBUTTONSIZE = (0x0400+58),
        TB_GETBUTTONINFOW = (0x0400+63),
        TB_SETBUTTONINFOW = (0x0400+64),
        TB_GETBUTTONINFOA = (0x0400+65),
        TB_SETBUTTONINFOA = (0x0400+66),
        TB_MAPACCELERATORA = (0x0400+78),
        TB_SETEXTENDEDSTYLE = (0x0400+84),
        TB_MAPACCELERATORW = (0x0400+90),
        TB_GETTOOLTIPS     = (0x0400 + 35),
        TB_SETTOOLTIPS     = (0x0400 + 36),
        TBIF_IMAGE = 0x00000001,
        TBIF_TEXT = 0x00000002,
        TBIF_STATE = 0x00000004,
        TBIF_STYLE = 0x00000008,
        TBIF_COMMAND = 0x00000020,
        TBIF_SIZE = 0x00000040,
        TBN_GETBUTTONINFOA = ((0-700)-0),
        TBN_GETBUTTONINFOW = ((0-700)-20),
        TBN_QUERYINSERT = ((0-700)-6),
        TBN_DROPDOWN = ((0-700)-10),
        TBN_HOTITEMCHANGE = ((0-700)-13),
        TBN_GETDISPINFOA = ((0-700)-16),
        TBN_GETDISPINFOW = ((0-700)-17),
        TBN_GETINFOTIPA = ((0-700)-18),
        TBN_GETINFOTIPW = ((0-700)-19),
        TTS_ALWAYSTIP = 0x01,
        TTS_NOPREFIX            =0x02,
        TTS_NOANIMATE           =0x10,
        TTS_NOFADE              =0x20,
        TTS_BALLOON             =0x40,
        //TTI_NONE                =0,
        //TTI_INFO                =1,
        TTI_WARNING             =2,
        //TTI_ERROR               =3,
        TTF_IDISHWND = 0x0001,
        TTF_RTLREADING = 0x0004,
        TTF_TRACK = 0x0020,
        TTF_CENTERTIP = 0x0002,
        TTF_SUBCLASS = 0x0010,
        TTF_TRANSPARENT = 0x0100,
        TTF_ABSOLUTE   =  0x0080,
        TTDT_AUTOMATIC = 0,
        TTDT_RESHOW = 1,
        TTDT_AUTOPOP = 2,
        TTDT_INITIAL = 3,
        TTM_TRACKACTIVATE = (0x0400+17),
        TTM_TRACKPOSITION = (0x0400+18),
        TTM_ACTIVATE = (0x0400+1),
        TTM_POP = (0x0400 + 28),
        TTM_ADJUSTRECT = (0x400 + 31),
        TTM_SETDELAYTIME = (0x0400+3),
#if !DRT && !UIAUTOMATIONTYPES
        TTM_SETTITLEA           =((int)WindowMessage.WM_USER + 32),  // wParam = TTI_*, lParam = char* szTitle
        TTM_SETTITLEW           =((int)WindowMessage.WM_USER + 33), // wParam = TTI_*, lParam = wchar* szTitle
#endif
        TTM_ADDTOOLA = (0x0400+4),
        TTM_ADDTOOLW = (0x0400+50),
        TTM_DELTOOLA = (0x0400+5),
        TTM_DELTOOLW = (0x0400+51),
        TTM_NEWTOOLRECTA = (0x0400+6),
        TTM_NEWTOOLRECTW = (0x0400+52),
        TTM_RELAYEVENT = (0x0400+7),
        TTM_GETTIPBKCOLOR = (0x0400+22),
        TTM_SETTIPBKCOLOR =  (0x0400 + 19),
        TTM_SETTIPTEXTCOLOR  = (0x0400 + 20),
        TTM_GETTIPTEXTCOLOR = (0x0400+23),
        TTM_GETTOOLINFOA = (0x0400+8),
        TTM_GETTOOLINFOW = (0x0400+53),
        TTM_SETTOOLINFOA = (0x0400+9),
        TTM_SETTOOLINFOW = (0x0400+54),
        TTM_HITTESTA = (0x0400+10),
        TTM_HITTESTW = (0x0400+55),
        TTM_GETTEXTA = (0x0400+11),
        TTM_GETTEXTW = (0x0400+56),
        TTM_UPDATE = (0x0400+29),
        TTM_UPDATETIPTEXTA = (0x0400+12),
        TTM_UPDATETIPTEXTW = (0x0400+57),
        TTM_ENUMTOOLSA = (0x0400+14),
        TTM_ENUMTOOLSW = (0x0400+58),
        TTM_GETCURRENTTOOLA = (0x0400+15),
        TTM_GETCURRENTTOOLW = (0x0400+59),
        TTM_WINDOWFROMPOINT = (0x0400+16),
        TTM_GETDELAYTIME = (0x0400+21),
        TTM_SETMAXTIPWIDTH = (0x0400+24),
        TTN_GETDISPINFOA = ((0-520)-0),
        TTN_GETDISPINFOW = ((0-520)-10),
        TTN_SHOW = ((0-520)-1),
        TTN_POP = ((0-520)-2),
        TTN_NEEDTEXTA = ((0-520)-0),
        TTN_NEEDTEXTW = ((0-520)-10),
        TBS_AUTOTICKS = 0x0001,
        TBS_VERT = 0x0002,
        TBS_TOP = 0x0004,
        TBS_BOTTOM = 0x0000,
        TBS_BOTH = 0x0008,
        TBS_NOTICKS = 0x0010,
        TBM_GETPOS = (0x0400),
        TBM_SETTIC = (0x0400+4),
        TBM_SETPOS = (0x0400+5),
        TBM_SETRANGE = (0x0400+6),
        TBM_SETRANGEMIN = (0x0400+7),
        TBM_SETRANGEMAX = (0x0400+8),
        TBM_SETTICFREQ = (0x0400+20),
        TBM_SETPAGESIZE = (0x0400+21),
        TBM_SETLINESIZE = (0x0400+23),
        TB_LINEUP = 0,
        TB_LINEDOWN = 1,
        TB_PAGEUP = 2,
        TB_PAGEDOWN = 3,
        TB_THUMBPOSITION = 4,
        TB_THUMBTRACK = 5,
        TB_TOP = 6,
        TB_BOTTOM = 7,
        TB_ENDTRACK = 8,
        TVS_HASBUTTONS = 0x0001,
        TVS_HASLINES = 0x0002,
        TVS_LINESATROOT = 0x0004,
        TVS_EDITLABELS = 0x0008,
        TVS_SHOWSELALWAYS = 0x0020,
        TVS_RTLREADING = 0x0040,
        TVS_CHECKBOXES = 0x0100,
        TVS_TRACKSELECT = 0x0200,
        TVS_FULLROWSELECT = 0x1000,
        TVS_NONEVENHEIGHT = 0x4000,
        TVS_INFOTIP = 0x0800,
        TVS_NOTOOLTIPS = 0x0080,
        TVIF_TEXT = 0x0001,
        TVIF_IMAGE = 0x0002,
        TVIF_PARAM = 0x0004,
        TVIF_STATE = 0x0008,
        TVIF_HANDLE = 0x0010,
        TVIF_SELECTEDIMAGE = 0x0020,
        TVIS_SELECTED = 0x0002,
        TVIS_EXPANDED = 0x0020,
        TVIS_EXPANDEDONCE = 0x0040,
        TVIS_STATEIMAGEMASK = 0xF000,
        TVI_ROOT = (unchecked((int)0xFFFF0000)),
        TVI_FIRST = (unchecked((int)0xFFFF0001)),
        TVM_INSERTITEMA = (0x1100+0),
        TVM_INSERTITEMW = (0x1100+50),
        TVM_DELETEITEM = (0x1100+1),
        TVM_EXPAND = (0x1100+2),
        TVE_COLLAPSE = 0x0001,
        TVE_EXPAND = 0x0002,
        TVM_GETITEMRECT = (0x1100+4),
        TVM_GETINDENT = (0x1100+6),
        TVM_SETINDENT = (0x1100+7),
        TVM_SETIMAGELIST = (0x1100+9),
        TVM_GETNEXTITEM = (0x1100+10),
        TVGN_NEXT = 0x0001,
        TVGN_PREVIOUS = 0x0002,
        TVGN_FIRSTVISIBLE = 0x0005,
        TVGN_NEXTVISIBLE = 0x0006,
        TVGN_PREVIOUSVISIBLE = 0x0007,
        TVGN_CARET = 0x0009,
        TVM_SELECTITEM = (0x1100+11),
        TVM_GETITEMA = (0x1100+12),
        TVM_GETITEMW = (0x1100+62),
        TVM_SETITEMA = (0x1100+13),
        TVM_SETITEMW = (0x1100+63),
        TVM_EDITLABELA = (0x1100+14),
        TVM_EDITLABELW = (0x1100+65),
        TVM_GETEDITCONTROL = (0x1100+15),
        TVM_GETVISIBLECOUNT = (0x1100+16),
        TVM_HITTEST = (0x1100+17),
        TVM_ENSUREVISIBLE = (0x1100+20),
        TVM_ENDEDITLABELNOW = (0x1100+22),
        TVM_GETISEARCHSTRINGA = (0x1100+23),
        TVM_GETISEARCHSTRINGW = (0x1100+64),
        TVM_SETITEMHEIGHT = (0x1100+27),
        TVM_GETITEMHEIGHT = (0x1100+28),
        TVN_SELCHANGINGA = ((0-400)-1),
        TVN_SELCHANGINGW = ((0-400)-50),
        TVN_GETINFOTIPA  = ((0-400)-13),
        TVN_GETINFOTIPW  = ((0-400)-14),
        TVN_SELCHANGEDA = ((0-400)-2),
        TVN_SELCHANGEDW = ((0-400)-51),
        TVC_UNKNOWN = 0x0000,
        TVC_BYMOUSE = 0x0001,
        TVC_BYKEYBOARD = 0x0002,
        TVN_GETDISPINFOA = ((0-400)-3),
        TVN_GETDISPINFOW = ((0-400)-52),
        TVN_SETDISPINFOA = ((0-400)-4),
        TVN_SETDISPINFOW = ((0-400)-53),
        TVN_ITEMEXPANDINGA = ((0-400)-5),
        TVN_ITEMEXPANDINGW = ((0-400)-54),
        TVN_ITEMEXPANDEDA = ((0-400)-6),
        TVN_ITEMEXPANDEDW = ((0-400)-55),
        TVN_BEGINDRAGA = ((0-400)-7),
        TVN_BEGINDRAGW = ((0-400)-56),
        TVN_BEGINRDRAGA = ((0-400)-8),
        TVN_BEGINRDRAGW = ((0-400)-57),
        TVN_BEGINLABELEDITA = ((0-400)-10),
        TVN_BEGINLABELEDITW = ((0-400)-59),
        TVN_ENDLABELEDITA = ((0-400)-11),
        TVN_ENDLABELEDITW = ((0-400)-60),
        TCS_BOTTOM = 0x0002,
        TCS_RIGHT = 0x0002,
        TCS_FLATBUTTONS = 0x0008,
        TCS_HOTTRACK = 0x0040,
        TCS_VERTICAL = 0x0080,
        TCS_TABS = 0x0000,
        TCS_BUTTONS = 0x0100,
        TCS_MULTILINE = 0x0200,
        TCS_RIGHTJUSTIFY = 0x0000,
        TCS_FIXEDWIDTH = 0x0400,
        TCS_RAGGEDRIGHT = 0x0800,
        TCS_OWNERDRAWFIXED = 0x2000,
        TCS_TOOLTIPS = 0x4000,
        TCM_SETIMAGELIST = (0x1300+3),
        TCIF_TEXT = 0x0001,
        TCIF_IMAGE = 0x0002,
        TCM_GETITEMA = (0x1300+5),
        TCM_GETITEMW = (0x1300+60),
        TCM_SETITEMA = (0x1300+6),
        TCM_SETITEMW = (0x1300+61),
        TCM_INSERTITEMA = (0x1300+7),
        TCM_INSERTITEMW = (0x1300+62),
        TCM_DELETEITEM = (0x1300+8),
        TCM_DELETEALLITEMS = (0x1300+9),
        TCM_GETITEMRECT = (0x1300+10),
        TCM_GETCURSEL = (0x1300+11),
        TCM_SETCURSEL = (0x1300+12),
        TCM_ADJUSTRECT = (0x1300+40),
        TCM_SETITEMSIZE = (0x1300+41),
        TCM_SETPADDING = (0x1300+43),
        TCM_GETROWCOUNT = (0x1300+44),
        TCM_GETTOOLTIPS = (0x1300+45),
        TCM_SETTOOLTIPS = (0x1300+46),
        TCN_SELCHANGE = ((0-550)-1),
        TCN_SELCHANGING = ((0-550)-2),
        TBSTYLE_WRAPPABLE = 0x0200,
        TVM_SETBKCOLOR = (TV_FIRST + 29),
        TVM_SETTEXTCOLOR = (TV_FIRST + 30),
        TYMED_NULL = 0,
        TVM_GETLINECOLOR  = (TV_FIRST + 41),
        TVM_SETLINECOLOR  = (TV_FIRST + 40),
        TVM_SETTOOLTIPS   = (TV_FIRST + 24),
        TVSIL_STATE   =          2,
        TVM_SORTCHILDRENCB = (TV_FIRST + 21);

        public const int
        UIS_SET        = 1,
        UIS_CLEAR      = 2,
        UIS_INITIALIZE = 3,
        UISF_HIDEFOCUS = 0x1,
        UISF_HIDEACCEL = 0x2,
        UISF_ACTIVE    = 0x4;

        public const int VK_TAB = 0x09;
        public const int VK_SHIFT = 0x10;
        public const int VK_CONTROL = 0x11;
        public const int VK_MENU = 0x12;

        public const int WH_JOURNALPLAYBACK = 1,
        WH_GETMESSAGE = 3,
        WH_MOUSE = 7,
        WSF_VISIBLE = 0x0001,
        WA_INACTIVE = 0,
        WA_ACTIVE = 1,
        WA_CLICKACTIVE = 2;

        public const int WHEEL_DELTA = 120,
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

        public const int XBUTTON1    =  0x0001;
        public const int XBUTTON2    =  0x0002;

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
        public struct MARGINS {
            public int cxLeftWidth;
            public int cxRightWidth;
            public int cyTopHeight;
            public int cyBottomHeight;
        }

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
        public class LOGFONT {
            public LOGFONT() {
            }
            public LOGFONT( LOGFONT lf )
            {
                ArgumentNullException.ThrowIfNull(lf);

                this.lfHeight           = lf.lfHeight;
                this.lfWidth            = lf.lfWidth;
                this.lfEscapement       = lf.lfEscapement;
                this.lfOrientation      = lf.lfOrientation;
                this.lfWeight           = lf.lfWeight;
                this.lfItalic           = lf.lfItalic;
                this.lfUnderline        = lf.lfUnderline;
                this.lfStrikeOut        = lf.lfStrikeOut;
                this.lfCharSet          = lf.lfCharSet;
                this.lfOutPrecision     = lf.lfOutPrecision;
                this.lfClipPrecision    = lf.lfClipPrecision;
                this.lfQuality          = lf.lfQuality;
                this.lfPitchAndFamily   = lf.lfPitchAndFamily;
                this.lfFaceName         = lf.lfFaceName;
            }
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
            public string   lfFaceName;
        }

        [StructLayout(LayoutKind.Sequential, CharSet=CharSet.Auto)]
        public class MENUITEMINFO_T
        {
            public int cbSize = SizeOf();
            public int fMask = 0;
            public int fType = 0;
            public int fState = 0;
            public int wID = 0;
            public IntPtr hSubMenu = IntPtr.Zero;
            public IntPtr hbmpChecked = IntPtr.Zero;
            public IntPtr hbmpUnchecked = IntPtr.Zero;
            public int dwItemData = 0;
            public string dwTypeData = null;
            public int cch = 0;
            
            private static int SizeOf()
            {
                return Marshal.SizeOf(typeof(MENUITEMINFO_T));
            }
        }

        public delegate bool EnumThreadWindowsCallback(IntPtr hWnd, IntPtr lParam);

        [StructLayout(LayoutKind.Sequential)]
        internal class OFNOTIFY
        {
            // hdr was a by-value NMHDR structure
            internal IntPtr hdr_hwndFrom;
            internal IntPtr hdr_idFrom;
            internal int hdr_code;

            internal IntPtr lpOFN;
            internal IntPtr pszFile;
        }

        [StructLayout(LayoutKind.Sequential, CharSet=CharSet.Unicode)]
        public class OPENFILENAME_I
        {
            public int      lStructSize = SizeOf(); //ndirect.DllLib.sizeOf(this);
            public IntPtr   hwndOwner;
            public IntPtr   hInstance;
            public string   lpstrFilter;   // use embedded nulls to separate filters
            public IntPtr   lpstrCustomFilter;
            public int      nMaxCustFilter;
            public int      nFilterIndex;
            public IntPtr   lpstrFile;
            public int      nMaxFile = NativeMethods.MAX_PATH;
            public IntPtr   lpstrFileTitle;
            public int      nMaxFileTitle = NativeMethods.MAX_PATH;
            public string   lpstrInitialDir;
            public string   lpstrTitle;
            public int      Flags;
            public short    nFileOffset;
            public short    nFileExtension;
            public string   lpstrDefExt;
            public IntPtr   lCustData;
            public WndProc  lpfnHook;
            public string   lpTemplateName;
            public IntPtr   pvReserved;
            public int      dwReserved;
            public int      FlagsEx;
            
            private static int SizeOf()
            {
                return Marshal.SizeOf(typeof(OPENFILENAME_I));
            }        
        }

        // constants related to the OPENFILENAME structure and file open/save dialogs
        public const int CDN_FIRST          = unchecked((int)(0U-601U));

        public const int CDN_INITDONE       = (CDN_FIRST - 0x0000);
        public const int CDN_SELCHANGE      = (CDN_FIRST - 0x0001);
        public const int CDN_SHAREVIOLATION = (CDN_FIRST - 0x0003);
        public const int CDN_FILEOK         = (CDN_FIRST - 0x0005);

#if !DRT && !UIAUTOMATIONTYPES
        public const int CDM_FIRST          = (int)WindowMessage.WM_USER + 100;

        public const int CDM_GETSPEC        = (CDM_FIRST + 0x0000);
        public const int CDM_GETFILEPATH    = (CDM_FIRST + 0x0001);
#endif

        public const int DWL_MSGRESULT = 0;

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
                this.left = x;
                this.top = y;
                this.right = right;
                this.bottom = bottom;
            }

            public COMRECT(RECT rect) {
                this.left = rect.left;
                this.top = rect.top;
                this.bottom = rect.bottom;
                this.right = rect.right;
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

        [StructLayout(LayoutKind.Sequential)/*leftover(noAutoOffset)*/]
        public sealed class tagCONTROLINFO
        {
            [MarshalAs(UnmanagedType.U4)/*leftover(offset=0, cb)*/]
            public uint cb = (uint)SizeOf();

            public IntPtr hAccel;

            [MarshalAs(UnmanagedType.U2)/*leftover(offset=8, cAccel)*/]
            public ushort cAccel;

            [MarshalAs(UnmanagedType.U4)/*leftover(offset=10, dwFlags)*/]
            public uint dwFlags;
            
            private static int SizeOf()
            {
                return Marshal.SizeOf(typeof(tagCONTROLINFO));
            }
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
                if ((this.vt == (int)tagVT.VT_UNKNOWN || this.vt == (int)tagVT.VT_DISPATCH) && this.data1 != IntPtr.Zero) {
                    Marshal.Release(this.data1);
                }

                if (this.vt == (int)tagVT.VT_BSTR && this.data1 != IntPtr.Zero) {
                    SysFreeString(this.data1);
                }

                this.data1 = this.data2 = IntPtr.Zero;
                this.vt = (int)tagVT.VT_EMPTY;
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

                int vtType = (int)(this.vt & (short)tagVT.VT_TYPEMASK);

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

        public static class CommonHandles {
            static CommonHandles() {
            }

            /// <devdoc>
            ///     Handle type for accelerator tables.
            /// </devdoc>
            public static readonly int Accelerator  = HandleCollector.RegisterType("Accelerator", 80, 50);

            /// <devdoc>
            ///     handle type for cursors.
            /// </devdoc>
            public static readonly int Cursor       = HandleCollector.RegisterType("Cursor", 20, 500);

            /// <devdoc>
            ///     Handle type for enhanced metafiles.
            /// </devdoc>
            public static readonly int EMF          = HandleCollector.RegisterType("EnhancedMetaFile", 20, 500);

            /// <devdoc>
            ///     Handle type for file find handles.
            /// </devdoc>
            public static readonly int Find         = HandleCollector.RegisterType("Find", 0, 1000);

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

            /// <devdoc>
            ///     Handle type for files.
            /// </devdoc>
            public static readonly int Menu         = HandleCollector.RegisterType("Menu", 30, 1000);

            /// <devdoc>
            ///     Handle type for windows.
            /// </devdoc>
            public static readonly int Window       = HandleCollector.RegisterType("Window", 5, 1000);
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
        public const int EVENT_SYSTEM_MOVESIZESTART =      0x000A;
        public const int EVENT_SYSTEM_MOVESIZEEND =        0x000B;
        public const int EVENT_OBJECT_STATECHANGE = 0x800A;
        public const int EVENT_OBJECT_FOCUS = 0x8005;
        public const int OBJID_CLIENT            = unchecked(unchecked((int)0xFFFFFFFC));
        public const int WINEVENT_OUTOFCONTEXT =           0x0000;

        // the delegate passed to USER for receiving a WinEvent
        internal delegate void WinEventProcDef (int winEventHook, int eventId, IntPtr hwnd, int idObject, int idChild, int eventThread, int eventTime);

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

        public const uint   RIDI_DEVICEINFO = 0x2000000b;
        public const uint   RIM_TYPEHID = 2;
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
        public const int ULW_COLORKEY = 0x00000001;
        public const int ULW_ALPHA    = 0x00000002;
        public const int ULW_OPAQUE   = 0x00000004;

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

