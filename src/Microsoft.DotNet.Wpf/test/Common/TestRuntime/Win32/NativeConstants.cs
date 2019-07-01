// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.IO;
using System.Text;
using System.Security;
using System.Threading;
using System.Runtime.InteropServices;


namespace Microsoft.Test.Win32
{
    /// <summary>
    /// 
    /// </summary>
    static class Win32ExceptionWrapper
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="apiName"></param>
        /// <returns></returns>
        public static Exception ExternalException(string apiName)
        {
            return ExternalException(apiName, Marshal.GetLastWin32Error());
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="apiName"></param>
        /// <param name="errorCodeOrHresult"></param>
        /// <returns></returns>
        public static Exception ExternalException(string apiName, int errorCodeOrHresult)
        {
            return new ExternalException("Native call to Win32 API ('" + apiName + "') failed ! \r\n\tError code : " + errorCodeOrHresult + " (0x" + errorCodeOrHresult.ToString("X") + ")\r\n\tError message : " + Win32ExceptionWrapper.GetStringForErrorCode(errorCodeOrHresult));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="herror"></param>
        /// <returns></returns>
        public static string GetStringForErrorCode(int herror)
        {
            StringBuilder retVal = new StringBuilder();
            int dwFlags = NativeConstants.FORMAT_MESSAGE_FROM_SYSTEM |
                NativeConstants.FORMAT_MESSAGE_ALLOCATE_BUFFER |
                NativeConstants.FORMAT_MESSAGE_IGNORE_INSERTS;

            if (IntFormatMessage(dwFlags, IntPtr.Zero, herror, 0, ref retVal, 0, IntPtr.Zero) == 0)
            {
                // call failed, not a major issue, just return the error code
                return "( Unable to retrive string associated with this error code)";
            }
            return retVal.ToString();
        }

        #region Internal Imports

        internal const string KERNEL32DLL = "kernel32.dll";

        [DllImport(KERNEL32DLL, EntryPoint = "FormatMessage", SetLastError = true, CharSet = CharSet.Ansi)]
        internal static extern int IntFormatMessage(int dwFlags, IntPtr lpSource, int dwMessageId, int dwLanguageId, ref StringBuilder lpBuffer, int nSize, IntPtr va_list);

        #endregion
    }


    /// <summary>
    /// "Type" of Windows native constant.
    /// </summary>
    public enum NativeConstantType
    {
        /// <summary>WM constants</summary>
        WM,
        /// <summary>SW constants</summary>
        SW,
        /// <summary>WS constants</summary>
        WS,
        /// <summary>HT constants</summary>
        HT,
        /// <summary>SWP constants</summary>
        SWP,
        /// <summary>HWND constants</summary>
        HWND,
        /// <summary>WA constants</summary>
        WA
    };

    /// <summary>
    /// Contains Native Win32 Constants 
    /// </summary>
    public class NativeConstants
    {
        /// <summary>
        /// Initializes the NativeConstant-to-String maps.
        /// </summary>
        static NativeConstants()
        {
            //
            // Initialize constant maps for all native constant "types".
            //

            _CreateMap<int>(NativeConstantType.WM, "WM_");
            _CreateMap<int>(NativeConstantType.SW, "SW_");
            _CreateMap<int>(NativeConstantType.WS, "WS_");
            _CreateMap<int>(NativeConstantType.HT, "HT");
            _CreateMap<int>(NativeConstantType.SWP, "SWP_");
            _CreateMap<IntPtr>(NativeConstantType.HWND, "HWND_");
            _CreateMap<IntPtr>(NativeConstantType.WA, "WA_");

            _PopulateMaps();
        }

#pragma warning disable 3003
        /// <summary>
        /// Used for arguments to LoadLibraryEx
        /// </summary>
        public const uint LOAD_LIBRARY_AS_DATAFILE = 0x00000002;
#pragma warning restore 3003
        /// <summary>
        /// 
        /// </summary>
        public const int OPTION_ADDEXTENSION = unchecked(unchecked((int)0x80000000));

        /// <summary>
        /// Max Path for string on Shell File Dialog Windows
        /// </summary>
        public const int MAX_PATH = 260;

        /// <summary>
        /// Accelerator constants
        /// </summary>
        public const int
            FVIRTKEY = 1,
            FNOINVERT = 0x02,
            FSHIFT = 0x04,
            FCONTROL = 0x08,
            FALT = 0x10;


        /// <summary>
        /// 
        /// </summary>
        public const int 
            FORMAT_MESSAGE_ALLOCATE_BUFFER = 0x00000100,
            FORMAT_MESSAGE_IGNORE_INSERTS = 0x00000200,
            FORMAT_MESSAGE_FROM_STRING = 0x00000400,
            FORMAT_MESSAGE_FROM_HMODULE = 0x00000800,
            FORMAT_MESSAGE_FROM_SYSTEM = 0x00001000,
            FORMAT_MESSAGE_ARGUMENT_ARRAY = 0x00002000,
            FORMAT_MESSAGE_MAX_WIDTH_MASK = 0x000000FF;


        /// <summary>
        /// Display Constants.
        /// </summary>
        public  const int CCHDEVICENAME = 32,        
            DISP_CHANGE_SUCCESSFUL = 0,
            DISP_CHANGE_RESTART = 1,
            DISP_CHANGE_FAILED = -1,
            DISP_CHANGE_BADMODE = -2,
            DISP_CHANGE_NOTUPDATED = -3,
            DISP_CHANGE_BADFLAGS = -4,
            DISP_CHANGE_BADPARAM = -5,
            DISP_CHANGE_BADDUALVIEW = -6,
            CDS_UPDATEREGISTRY = 0x00000001,
            CDS_TEST = 0x00000002,
            CDS_FULLSCREEN = 0x00000004,
            CDS_GLOBAL = 0x00000008,
            CDS_SET_PRIMARY = 0x00000010,
            CDS_VIDEOPARAMETERS = 0x00000020,
            CDS_RESET = 0x40000000,
            CDS_NORESET = 0x10000000,
            ENUM_CURRENT_SETTINGS = -1,
            ENUM_REGISTRY_SETTINGS = -2,
            DISPLAY_DEVICE_ATTACHED_TO_DESKTOP = 0x00000001,
            DISPLAY_DEVICE_MULTI_DRIVER = 0x00000002,
            DISPLAY_DEVICE_PRIMARY_DEVICE = 0x00000004,
            DISPLAY_DEVICE_MIRRORING_DRIVER = 0x00000008,
            DISPLAY_DEVICE_VGA_COMPATIBLE = 0x00000010,
            DISPLAY_DEVICE_REMOVABLE = 0x00000020,
            DISPLAY_DEVICE_MODESPRUNED = 0x08000000,
            DISPLAY_DEVICE_REMOTE = 0x04000000,
            DISPLAY_DEVICE_DISCONNECT = 0x02000000,
            DISPLAY_DEVICE_TS_COMPATIBLE = 0x00200000,
            MONITOR_DEFAULTTONULL = 0x00000000,
            MONITOR_DEFAULTTOPRIMARY = 0x00000001,
            MONITOR_DEFAULTTONEAREST = 0x00000002;



        /// <summary>
        /// WS constants for window creation
        /// </summary>
        public const int
            WS_OVERLAPPED = 0x00000000,
            WS_POPUP = (int)-2147483648, // 0x80000000
            WS_CHILD = 0x40000000,
            WS_MINIMIZE = 0x20000000,
            WS_VISIBLE = 0x10000000,
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
            WS_MINIMIZEBOX = 0x00020000,
            WS_MAXIMIZEBOX = 0x00010000,
            WS_OVERLAPPEDWINDOW =
            (WS_OVERLAPPED | WS_CAPTION | WS_SYSMENU |
            WS_THICKFRAME | WS_MINIMIZEBOX | WS_MAXIMIZEBOX),
            WS_EX_DLGMODALFRAME = 0x00000001,
            WS_EX_NOPARENTNOTIFY = 0x00000004,
            WS_EX_TOPMOST = 0x00000008,
            WS_EX_ACCEPTFILES = 0x00000010,
            WS_EX_TRANSPARENT = 0x00000020,
            WS_EX_MDICHILD = 0x00000040,
            WS_EX_TOOLWINDOW = 0x00000080,
            WS_EX_WINDOWEDGE = 0x00000100,
            WS_EX_CLIENTEDGE = 0x00000200,
            WS_EX_CONTEXTHELP = 0x00000400,
            WS_EX_RIGHT = 0x00001000,
            WS_EX_LEFT = 0x00000000,
            WS_EX_RTLREADING = 0x00002000,
            WS_EX_LTRREADING = 0x00000000,
            WS_EX_LEFTSCROLLBAR = 0x00004000,
            WS_EX_RIGHTSCROLLBAR = 0x00000000,
            WS_EX_CONTROLPARENT = 0x00010000,
            WS_EX_STATICEDGE = 0x00020000,
            WS_EX_APPWINDOW = 0x00040000,
            WS_EX_OVERLAPPEDWINDOW = WS_EX_WINDOWEDGE | WS_EX_CLIENTEDGE,
            WS_EX_PALETTEWINDOW = WS_EX_WINDOWEDGE | WS_EX_TOOLWINDOW | WS_EX_TOPMOST,
            WS_EX_LAYERED = 0x00080000,
            WS_EX_NOINHERITLAYOUT = 0x00100000, // Disable inheritence of mirroring by children
            WS_EX_LAYOUTRTL = 0x00400000, // Right to left mirroring
            WS_EX_COMPOSITED = 0x02000000,
            WS_EX_NOACTIVATE = 0x08000000,
            CW_USEDEFAULT = 0x8000;

        /// <summary>
        /// GMMP constants
        /// </summary>
        public const int
            GMMP_USE_DISPLAY_POINTS = 1,
            GMMP_USE_HIGH_RESOLUTION_POINTS = 2;

        /// <summary>
        /// HWND constants
        /// </summary>
        public static IntPtr
            HWND_TOP = new IntPtr(0),
            HWND_BOTTOM = new IntPtr(1),
            HWND_TOPMOST = new IntPtr(-1),
            HWND_NOTOPMOST = new IntPtr(-2),
            HWND_MESSAGE = new IntPtr(-3);


        /// <summary>
        /// WA constants for WM_ACTIVATE state value
        /// </summary>
        public static IntPtr
            WA_INACTIVE = new IntPtr(0),
            WA_ACTIVE = new IntPtr(1),
            WA_CLICKACTIVE = new IntPtr(2);

        /// <summary>
        /// Win32 constants
        /// </summary>
        public const int BN_CLICKED = 0x0000;

        /// <summary>
        /// WM constants
        /// </summary>
        public const int
            WM_NULL = 0x0000,
            WM_CREATE = 0x0001,
            WM_DESTROY = 0x0002,
            WM_MOVE = 0x0003,
            WM_SIZE = 0x0005,
            WM_ACTIVATE = 0x0006,
            WM_SETFOCUS = 0x0007,
            WM_KILLFOCUS = 0x0008,
            WM_CLOSE = 0x0010,
            WM_ENABLE = 0x000A,
            WM_SETREDRAW = 0x000B,
            WM_SETTEXT = 0x000C,
            WM_GETTEXT = 0x000D,
            WM_GETTEXTLENGTH = 0x000E,
            WM_PAINT = 0x000F,
            WM_QUIT = 0x0012,
            WM_ERASEBKGND = 0x0014,
            WM_SHOWWINDOW = 0x0018,
            WM_DEVMODECHANGE = 0x001B,
            WM_ACTIVATEAPP = 0x001C,
            WM_FONTCHANGE = 0x001D,
            WM_TIMECHANGE = 0x001E,
            WM_CANCELMODE = 0x001F,
            WM_SETCURSOR = 0x0020,
            WM_MOUSEACTIVATE = 0x0021,
            WM_CHILDACTIVATE = 0x0022,
            WM_QUEUESYNC = 0x0023,
            WM_WINDOWPOSCHANGING = 0x0046,
            WM_WINDOWPOSCHANGED = 0x0047,
            WM_NOTIFY = 0x004E,
            WM_CONTEXTMENU = 0x007B,
            WM_STYLECHANGING = 0x007C,
            WM_STYLECHANGED = 0x007D,
            WM_DISPLAYCHANGE = 0x007E,
            WM_GETICON = 0x007F,
            WM_SETICON = 0x0080,
            WM_NCCREATE = 0x0081,
            WM_NCDESTROY = 0x0082,
            WM_NCCALCSIZE = 0x0083,
            WM_NCHITTEST = 0x0084,
            WM_NCPAINT = 0x0085,
            WM_NCACTIVATE = 0x0086,
            WM_GETDLGCODE = 0x0087,
            WM_SYNCPAINT = 0x0088,
            WM_CHAR = 0x0102,
            WM_DEADCHAR = 0x0103,
            WM_SYSCHAR = 0x0106,
            WM_SYSDEADCHAR = 0x0107,
            WM_COMMAND = 0x0111,
            WM_SYSCOMMAND = 0x0112,
            WM_KEYDOWN = 0x0100,
            WM_KEYUP = 0x0101,
            WM_SYSKEYDOWN = 0x0104,
            WM_SYSKEYUP = 0x0105,
            WM_CTLCOLORMSGBOX = 0x0132,
            WM_CTLCOLOREDIT = 0x0133,
            WM_CTLCOLORLISTBOX = 0x0134,
            WM_CTLCOLORBTN = 0x0135,
            WM_CTLCOLORDLG = 0x0136,
            WM_CTLCOLORSCROLLBAR = 0x0137,
            WM_CTLCOLORSTATIC = 0x0138,
            WM_MOUSEMOVE = 0x0200,
            WM_LBUTTONDOWN = 0x0201,
            WM_LBUTTONUP = 0x0202,
            WM_LBUTTONDBLCLK = 0x0203,
            WM_RBUTTONDOWN = 0x0204,
            WM_RBUTTONUP = 0x0205,
            WM_RBUTTONDBLCLK = 0x0206,
            WM_MBUTTONDOWN = 0x0207,
            WM_MBUTTONUP = 0x0208,
            WM_MBUTTONDBLCLK = 0x0209,
            WM_NEXTMENU = 0x0213,
            WM_SIZING = 0x0214,
            WM_CAPTURECHANGED = 0x0215,
            WM_MOVING = 0x0216,
            WM_MOUSEHOVER = 0x02A1,
            WM_MOUSELEAVE = 0x02A3,
            WM_DWMCOMPOSITIONCHANGED = 0x031E;

        /// <summary>
        /// HT constants for WM_NCHITTEST, WM_SETCURSOR
        /// </summary>
        public const int
            HTERROR = (-2),
            HTTRANSPARENT = (-1),
            HTNOWHERE = 0,
            HTCLIENT = 1,
            HTCAPTION = 2,
            HTSYSMENU = 3,
            HTGROWBOX = 4,
            HTSIZE = HTGROWBOX,
            HTMENU = 5,
            HTHSCROLL = 6,
            HTVSCROLL = 7,
            HTMINBUTTON = 8,
            HTMAXBUTTON = 9,
            HTLEFT = 10,
            HTRIGHT = 11,
            HTTOP = 12,
            HTTOPLEFT = 13,
            HTTOPRIGHT = 14,
            HTBOTTOM = 15,
            HTBOTTOMLEFT = 16,
            HTBOTTOMRIGHT = 17,
            HTBORDER = 18,
            HTREDUCE = HTMINBUTTON,
            HTZOOM = HTMAXBUTTON,
            HTSIZEFIRST = HTLEFT,
            HTSIZELAST = HTBOTTOMRIGHT,
            HTOBJECT = 19,
            HTCLOSE = 20,
            HTHELP = 21;


        /// <summary>
        /// SWP constants for SetWindowPos 
        /// </summary>
        public const int
            SWP_NOSIZE = 0x0001,
            SWP_NOMOVE = 0x0002,
            SWP_NOZORDER = 0x0004,
            SWP_NOACTIVATE = 0x0010,
            SWP_SHOWWINDOW = 0x0040,
            SWP_HIDEWINDOW = 0x0080,
            SWP_DRAWFRAME = 0x0020;

        /// <summary>
        /// SW constants for ShowWindow 
        /// </summary>
        public const int
            SW_HIDE = 0,
            SW_SHOWNORMAL = 1,
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
            SW_SHOWDEFAULT = 10,
            SW_FORCEMINIMIZE = 11,
            SW_MAX = 11;

        /// <summary>
        /// KEYEVENTF constants
        /// </summary>
        public const int
            KEYEVENTF_EXTENDEDKEY = 0x0001,
            KEYEVENTF_KEYUP = 0x0002,
            KEYEVENTF_UNICODE = 0x0004,
            KEYEVENTF_SCANCODE = 0x0008;

        /// <summary>
        /// MOUSEEVENTF constants
        /// </summary>
        public const int
            MOUSEEVENTF_MOVE = 0x0001,
            MOUSEEVENTF_LEFTDOWN = 0x0002,
            MOUSEEVENTF_LEFTUP = 0x0004,
            MOUSEEVENTF_RIGHTDOWN = 0x0008,
            MOUSEEVENTF_RIGHTUP = 0x0010,
            MOUSEEVENTF_MIDDLEDOWN = 0x0020,
            MOUSEEVENTF_MIDDLEUP = 0x0040,
            MOUSEEVENTF_XDOWN = 0x0080,
            MOUSEEVENTF_XUP = 0x0100,
            MOUSEEVENTF_WHEEL = 0x00800,
            MOUSEEVENTF_VIRTUALDESK = 0x04000,
            MOUSEEVENTF_ABSOLUTE = 0x08000,
            MOUSEEVENTF_ACTUAL = 0x10000;

        /// <summary>
        /// WM_MOUSEWHEEL constant
        /// </summary>
        public const int WHEEL_DELTA = 120;

        /// <summary>
        /// INPUT constants
        /// </summary>
        public const int
            INPUT_MOUSE = 0,
            INPUT_KEYBOARD = 1;


        /// <summary>
        /// SM constants for GetSystemMetrics
        /// </summary>
        public const int
            MONITORINFOF_PRIMARY = 0x00000001,
            SM_CXSCREEN = 0,
            SM_CYSCREEN = 1,
            SM_CXVSCROLL = 2,
            SM_CYHSCROLL = 3,
            SM_CYCAPTION = 4,
            SM_CXBORDER = 5,
            SM_CYBORDER = 6,
            SM_CYVTHUMB = 9,
            SM_CXHTHUMB = 10,
            SM_CXICON = 11,
            SM_CYICON = 12,
            SM_CXCURSOR = 13,
            SM_CYCURSOR = 14,
            SM_CYMENU = 15,
            SM_CYKANJIWINDOW = 18,
            SM_MOUSEPRESENT = 19,
            SM_CYVSCROLL = 20,
            SM_CXHSCROLL = 21,
            SM_DEBUG = 22,
            SM_SWAPBUTTON = 23,
            SM_CXMIN = 28,
            SM_CYMIN = 29,
            SM_CXSIZE = 30,
            SM_CYSIZE = 31,
            SM_CXFRAME = 32,
            SM_CYFRAME = 33,
            SM_CXMINTRACK = 34,
            SM_CYMINTRACK = 35,
            SM_CXDOUBLECLK = 36,
            SM_CYDOUBLECLK = 37,
            SM_CXICONSPACING = 38,
            SM_CYICONSPACING = 39,
            SM_MENUDROPALIGNMENT = 40,
            SM_PENWINDOWS = 41,
            SM_DBCSENABLED = 42,
            SM_CMOUSEBUTTONS = 43,
            SM_CXFIXEDFRAME = 7,
            SM_CYFIXEDFRAME = 8,
            SM_SECURE = 44,
            SM_CXEDGE = 45,
            SM_CYEDGE = 46,
            SM_CXMINSPACING = 47,
            SM_CYMINSPACING = 48,
            SM_CXSMICON = 49,
            SM_CYSMICON = 50,
            SM_CYSMCAPTION = 51,
            SM_CXSMSIZE = 52,
            SM_CYSMSIZE = 53,
            SM_CXMENUSIZE = 54,
            SM_CYMENUSIZE = 55,
            SM_ARRANGE = 56,
            SM_CXMINIMIZED = 57,
            SM_CYMINIMIZED = 58,
            SM_CXMAXTRACK = 59,
            SM_CYMAXTRACK = 60,
            SM_CXMAXIMIZED = 61,
            SM_CYMAXIMIZED = 62,
            SM_NETWORK = 63,
            SM_CLEANBOOT = 67,
            SM_CXDRAG = 68,
            SM_CYDRAG = 69,
            SM_SHOWSOUNDS = 70,
            SM_CXMENUCHECK = 71,
            SM_CYMENUCHECK = 72,
            SM_MIDEASTENABLED = 74,
            SM_MOUSEWHEELPRESENT = 75,
            SM_XVIRTUALSCREEN = 76,
            SM_YVIRTUALSCREEN = 77,
            SM_CXVIRTUALSCREEN = 78,
            SM_CYVIRTUALSCREEN = 79,
            SM_CMONITORS = 80,
            SM_SAMEDISPLAYFORMAT = 81,
            SM_REMOTESESSION = 0x1000;


        /// <summary>
        /// ID constants for dialogs
        /// </summary>
        public const int
            IDOK = 1,
            IDCANCEL = 2,
            IDABORT = 3,
            IDRETRY = 4,
            IDIGNORE = 5,
            IDYES = 6,
            IDNO = 7,
            IDCLOSE = 8,
            IDHELP = 9,
            IDTRYAGAIN = 10,
            IDCONTINUE = 11;

        /// <summary>
        /// IDC constants for cursors
        /// </summary>
        public const int
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
            IDC_HAND = 32649,
            IDC_APPSTARTING = 32650,
            IDC_HELP = 32651,
            IDC_PEN = IDC_ARROW + 119, // PenCursor
            IDC_SCROLLWE = IDC_ARROW + 141, // ScrollWECursor
            IDC_SCROLLSE = IDC_ARROW + 150, // ScrollSECursor
            IDC_ARROWCD = IDC_ARROW + 151, // ArrowCDCursor
            IDC_SCROLLALL = 0x7f8e,
            IDC_SCROLLE = 0x7f92,
            IDC_SCROLLN = 0x7f8f,
            IDC_SCROLLNE = 0x7f94,
            IDC_SCROLLNS = 0x7f8c,
            IDC_SCROLLNW = 0x7f93,
            IDC_SCROLLS = 0x7f90,
            IDC_SCROLLSW = 0x7f95,
            IDC_SCROLLW = 0x7f91;

        /// <summary>
        /// Constant for Mil Windows
        /// </summary>
        public const int CS_MIL = 0x00040000;

        /// <summary>
        /// Constant for Mil Windows
        /// </summary>
        public const string WC_TREEVIEW = "SysTreeView32";

        /// <summary>
        /// Win32 constants for Win32 Treeview
        /// </summary>
        public const int
            TVS_HASLINES = 0x0002,
            TVS_LINESATROOT = 0x0004,
            TVS_HASBUTTONS = 0x0001,
            TVM_SETTOOLTIPS = (0x1100 + 24),
            TVM_INSERTITEMW = (0x1100 + 50),
            TVI_ROOT = (unchecked((int)0xFFFF0000)),
            TVI_FIRST = (unchecked((int)0xFFFF0001)),
            TVIF_TEXT = 0x0001;

        /// <summary>
        /// Win32 constants for Win32 Treeview
        /// </summary>
        public const int
            TVN_FIRST = (0 - 400),
            TVN_SELCHANGING = ((0 - 400) - 50),
            TVN_SELCHANGED = ((0 - 400) - 51);


        /// <summary>
        /// Win32 constants
        /// </summary>
        public const int
            TPM_LEFTBUTTON = 0x0000,
            TPM_RIGHTBUTTON = 0x0002,
            TPM_LEFTALIGN = 0x0000,
            TPM_CENTERALIGN = 0x0004,
            TPM_RIGHTALIGN = 0x0008,
            TPM_RECURSE = 0x0001,
            TPM_HORPOSANIMATION = 0x0400,
            TPM_HORNEGANIMATION = 0x0800,
            TPM_VERPOSANIMATION = 0x1000,
            TPM_VERNEGANIMATION = 0x2000,
            TPM_NOANIMATION = 0x4000,
            TPM_LAYOUTRTL = 0x8000;

        /// <summary>
        /// Win32 constants
        /// </summary>
        public const int COLOR_WINDOW = 5;

        /// <summary>
        /// Win32 constants
        /// </summary>
        public const int LOGPIXELSX = 88;

        /// <summary>
        /// Win32 constants
        /// </summary>
        public const int LOGPIXELSY = 90;

        /// <summary>
        /// Win32 constants
        /// </summary>
        public const int GWL_WNDPROC = (-4);
        public const int GWL_STYLE = -16;
        public const int GWL_EXSTYLE = -20;

        /// <summary>
        /// VK constants
        /// </summary>
        public const int
            VK_LBUTTON = 0x01,
            VK_RBUTTON = 0x02,
            VK_CANCEL = 0x03,
            VK_MBUTTON = 0x04,
            VK_XBUTTON1 = 0x05,
            VK_XBUTTON2 = 0x06,
            VK_BACK = 0x08,
            VK_TAB = 0x09,
            VK_CLEAR = 0x0C,
            VK_RETURN = 0x0D,
            VK_SHIFT = 0x10,
            VK_CONTROL = 0x11,
            VK_MENU = 0x12,
            VK_PAUSE = 0x13,
            VK_CAPITAL = 0x14,
            VK_KANA = 0x15,
            VK_HANGEUL = 0x15,
            VK_HANGUL = 0x15,
            VK_JUNJA = 0x17,
            VK_FINAL = 0x18,
            VK_HANJA = 0x19,
            VK_KANJI = 0x19,
            VK_ESCAPE = 0x1B,
            VK_CONVERT = 0x1C,
            VK_NONCONVERT = 0x1D,
            VK_ACCEPT = 0x1E,
            VK_MODECHANGE = 0x1F,
            VK_SPACE = 0x20,
            VK_PRIOR = 0x21,
            VK_NEXT = 0x22,
            VK_END = 0x23,
            VK_HOME = 0x24,
            VK_LEFT = 0x25,
            VK_UP = 0x26,
            VK_RIGHT = 0x27,
            VK_DOWN = 0x28,
            VK_SELECT = 0x29,
            VK_PRINT = 0x2A,
            VK_EXECUTE = 0x2B,
            VK_SNAPSHOT = 0x2C,
            VK_INSERT = 0x2D,
            VK_DELETE = 0x2E,
            VK_HELP = 0x2F,
            VK_0 = 0x30,
            VK_1 = 0x31,
            VK_2 = 0x32,
            VK_3 = 0x33,
            VK_4 = 0x34,
            VK_5 = 0x35,
            VK_6 = 0x36,
            VK_7 = 0x37,
            VK_8 = 0x38,
            VK_9 = 0x39,
            VK_A = 0x41,
            VK_B = 0x42,
            VK_C = 0x43,
            VK_D = 0x44,
            VK_E = 0x45,
            VK_F = 0x46,
            VK_G = 0x47,
            VK_H = 0x48,
            VK_I = 0x49,
            VK_J = 0x4A,
            VK_K = 0x4B,
            VK_L = 0x4C,
            VK_M = 0x4D,
            VK_N = 0x4E,
            VK_O = 0x4F,
            VK_P = 0x50,
            VK_Q = 0x51,
            VK_R = 0x52,
            VK_S = 0x53,
            VK_T = 0x54,
            VK_U = 0x55,
            VK_V = 0x56,
            VK_W = 0x57,
            VK_X = 0x58,
            VK_Y = 0x59,
            VK_Z = 0x5A,
            VK_LWIN = 0x5B,
            VK_RWIN = 0x5C,
            VK_APPS = 0x5D,
            VK_POWER = 0x5E,
            VK_SLEEP = 0x5F,
            VK_NUMPAD0 = 0x60,
            VK_NUMPAD1 = 0x61,
            VK_NUMPAD2 = 0x62,
            VK_NUMPAD3 = 0x63,
            VK_NUMPAD4 = 0x64,
            VK_NUMPAD5 = 0x65,
            VK_NUMPAD6 = 0x66,
            VK_NUMPAD7 = 0x67,
            VK_NUMPAD8 = 0x68,
            VK_NUMPAD9 = 0x69,
            VK_MULTIPLY = 0x6A,
            VK_ADD = 0x6B,
            VK_SEPARATOR = 0x6C,
            VK_SUBTRACT = 0x6D,
            VK_DECIMAL = 0x6E,
            VK_DIVIDE = 0x6F,
            VK_F1 = 0x70,
            VK_F2 = 0x71,
            VK_F3 = 0x72,
            VK_F4 = 0x73,
            VK_F5 = 0x74,
            VK_F6 = 0x75,
            VK_F7 = 0x76,
            VK_F8 = 0x77,
            VK_F9 = 0x78,
            VK_F10 = 0x79,
            VK_F11 = 0x7A,
            VK_F12 = 0x7B,
            VK_F13 = 0x7C,
            VK_F14 = 0x7D,
            VK_F15 = 0x7E,
            VK_F16 = 0x7F,
            VK_F17 = 0x80,
            VK_F18 = 0x81,
            VK_F19 = 0x82,
            VK_F20 = 0x83,
            VK_F21 = 0x84,
            VK_F22 = 0x85,
            VK_F23 = 0x86,
            VK_F24 = 0x87,
            VK_NUMLOCK = 0x90,
            VK_SCROLL = 0x91,
            VK_LSHIFT = 0xA0,
            VK_RSHIFT = 0xA1,
            VK_LCONTROL = 0xA2,
            VK_RCONTROL = 0xA3,
            VK_LMENU = 0xA4,
            VK_RMENU = 0xA5,
            VK_BROWSER_BACK = 0xA6,
            VK_BROWSER_FORWARD = 0xA7,
            VK_BROWSER_REFRESH = 0xA8,
            VK_BROWSER_STOP = 0xA9,
            VK_BROWSER_SEARCH = 0xAA,
            VK_BROWSER_FAVORITES = 0xAB,
            VK_BROWSER_HOME = 0xAC,
            VK_VOLUME_MUTE = 0xAD,
            VK_VOLUME_DOWN = 0xAE,
            VK_VOLUME_UP = 0xAF,
            VK_MEDIA_NEXT_TRACK = 0xB0,
            VK_MEDIA_PREV_TRACK = 0xB1,
            VK_MEDIA_STOP = 0xB2,
            VK_MEDIA_PLAY_PAUSE = 0xB3,
            VK_LAUNCH_MAIL = 0xB4,
            VK_LAUNCH_MEDIA_SELECT = 0xB5,
            VK_LAUNCH_APP1 = 0xB6,
            VK_LAUNCH_APP2 = 0xB7,
            VK_OEM_1 = 0xBA,
            VK_OEM_PLUS = 0xBB,
            VK_OEM_COMMA = 0xBC,
            VK_OEM_MINUS = 0xBD,
            VK_OEM_PERIOD = 0xBE,
            VK_OEM_2 = 0xBF,
            VK_OEM_3 = 0xC0,
            VK_OEM_4 = 0xDB,
            VK_OEM_5 = 0xDC,
            VK_OEM_6 = 0xDD,
            VK_OEM_7 = 0xDE,
            VK_OEM_8 = 0xDF,
            VK_OEM_AX = 0xE1,
            VK_OEM_102 = 0xE2,
            VK_ICO_HELP = 0xE3,
            VK_ICO_00 = 0xE4,
            VK_PROCESSKEY = 0xE5,
            VK_PACKET = 0xE7,
            VK_OEM_RESET = 0xE9,
            VK_OEM_JUMP = 0xEA,
            VK_OEM_PA1 = 0xEB,
            VK_OEM_PA2 = 0xEC,
            VK_OEM_PA3 = 0xED,
            VK_OEM_WSCTRL = 0xEE,
            VK_OEM_CUSEL = 0xEF,
            VK_OEM_ATTN = 0xF0,
            VK_OEM_FINISH = 0xF1,
            VK_OEM_COPY = 0xF2,
            VK_OEM_AUTO = 0xF3,
            VK_OEM_ENLW = 0xF4,
            VK_OEM_BACKTAB = 0xF5,
            VK_ATTN = 0xF6,
            VK_CRSEL = 0xF7,
            VK_EXSEL = 0xF8,
            VK_EREOF = 0xF9,
            VK_PLAY = 0xFA,
            VK_ZOOM = 0xFB,
            VK_NONAME = 0xFC,
            VK_PA1 = 0xFD,
            VK_OEM_CLEAR = 0xFE;



        ///<summary>
        /// Options for Common Dialogs
        ///</summary>
        public const int
            OFN_READONLY = 0x00000001,
            OFN_OVERWRITEPROMPT = 0x00000002,
            OFN_HIDEREADONLY = 0x00000004,
            OFN_NOCHANGEDIR = 0x00000008,
            OFN_SHOWHELP = 0x00000010,
            OFN_ENABLEHOOK = 0x00000020,
            OFN_ENABLETEMPLATE = 0x00000040,
            OFN_ENABLETEMPLATEHANDLE = 0x00000080,
            OFN_NOVALIDATE = 0x00000100,
            OFN_ALLOWMULTISELECT = 0x00000200,
            OFN_EXTENSIONDIFFERENT = 0x00000400,
            OFN_PATHMUSTEXIST = 0x00000800,
            OFN_FILEMUSTEXIST = 0x00001000,
            OFN_CREATEPROMPT = 0x00002000,
            OFN_SHAREAWARE = 0x00004000,
            OFN_NOREADONLYRETURN = 0x00008000,
            OFN_NOTESTFILECREATE = 0x00010000,
            OFN_NONETWORKBUTTON = 0x00020000,
            OFN_NOLONGNAMES = 0x00040000,     // force no long names for 4.x modules
            OFN_EXPLORER = 0x00080000,     // new look commdlg
            OFN_NODEREFERENCELINKS = 0x00100000,
            OFN_LONGNAMES = 0x00200000,    // force long names for 3.x modules
            OFN_ENABLEINCLUDENOTIFY = 0x00400000,     // send include message to callback
            OFN_ENABLESIZING = 0x00800000,
            OFN_DONTADDTORECENT = 0x02000000,
            OFN_FORCESHOWHIDDEN = 0x10000000,    // Show All files including System and hidden files
            OFN_EX_NOPLACESBAR = 0x00000001,
            OFN_USESHELLITEM = 0x01000000;

        ///<summary>
        /// Options for MsgWaitForMultipleObjectsEx mask
        ///</summary>
        public const int
            QS_KEY = 0x0001,
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
            QS_ALLINPUT = QS_INPUT | QS_POSTMESSAGE | QS_TIMER | QS_PAINT | QS_HOTKEY | QS_SENDMESSAGE,
            QS_EVENT = 0x2000;

        ///<summary>
        /// MWMO constants
        ///</summary>
        public const int
            MWMO_WAITALL = 0x0001,
            MWMO_ALERTABLE = 0x0002,
            MWMO_INPUTAVAILABLE = 0x0004;

        ///<summary>
        /// Win32 error codes
        ///</summary>
        public const int
            ERROR_FILE_NOT_FOUND = 2,
            ERROR_PATH_NOT_FOUND = 3,
            ERROR_ACCESS_DENIED = 5,
            ERROR_INVALID_DRIVE = 15,
            ERROR_SHARING_VIOLATION = 32,
            ERROR_FILE_EXISTS = 80,
            ERROR_INVALID_PARAMETER = 87,
            ERROR_FILENAME_EXCED_RANGE = 206,
            ERROR_OPERATION_ABORTED = 995;

        /// <summary>
        /// dwTypeBitMask constants for Kernel32.VerSetConditionMask
        /// </summary>
        [CLSCompliant(false)]
        public const uint
            VER_BUILDNUMBER      = 0x0000004,
            VER_MAJORVERSION     = 0x0000002,
            VER_MINORVERSION     = 0x0000001,
            VER_PLATFORMID       = 0x0000008,
            VER_PRODUCT_TYPE     = 0x0000080,
            VER_SERVICEPACKMAJOR = 0x0000020,
            VER_SERVICEPACKMINOR = 0x0000010,
            VER_SUITENAME        = 0x0000040;

        /// <summary>
        /// product type values for NtDll.RtlVerifyVersionInfo
        /// </summary>
        public const byte 
            VER_NT_WORKSTATION       = 0x0000001,
            VER_NT_DOMAIN_CONTROLLER = 0x0000002,
            VER_NT_SERVER            = 0x0000003;

        /// <summary>
        /// dwConditionMask constants to support Kernel32.VerSetConditionMask
        /// </summary>
        public const byte 
            VER_EQUAL         = 1,
            VER_GREATER       = 2,
            VER_GREATER_EQUAL = 3,
            VER_LESS          = 4,
            VER_LESS_EQUAL    = 5,
            VER_AND           = 6,
            VER_OR            = 7;

        /// <summary>
        /// _WIN32_WINNT constants to identify different OS platforms
        /// </summary>
        [CLSCompliant(false)] 
        public const UInt16 
            _WIN32_WINNT_NT4      = 0x0400,
            _WIN32_WINNT_WIN2K    = 0x0500,
            _WIN32_WINNT_WINXP    = 0x0501,
            _WIN32_WINNT_WS03     = 0x0502,

            _WIN32_WINNT_VISTA    = 0x0600,
            _WIN32_WINNT_WS08     = 0x0600,

            _WIN32_WINNT_WIN7     = 0x0601,
            _WIN32_WINNT_WIN8     = 0x0602,
            _WIN32_WINNT_WINBLUE  = 0x0603,
            _WIN32_WINNT_WIN10    = 0x0A00;

        [CLSCompliant(false)]
        public const uint STATUS_SUCCESS = 0x0;
        
        /// <summary>
        /// Converts a Windows constant integer value to a human-readable
        /// string, e.g. 0x0007 to WM_SETFOCUS.
        /// </summary>
        /// <param name="value">The value to convert to a string.</param>
        /// <param name="constantType">The native constant "type".</param>
        public static string ConvertToString(int value, NativeConstantType constantType)
        {
            return ConvertToString<int>(value, constantType);
        }

        /// <summary>
        /// Converts a Windows constant integer value to a human-readable
        /// string, e.g. 0x0007 to WM_SETFOCUS.
        /// </summary>
        /// <typeparam name="T">The clr type of the native constant.</typeparam>
        /// <param name="value">The value to convert to a string.</param>
        /// <param name="constantType">The native constant "type".</param>
        /// <returns>A string representation of the native constant value.</returns>
        public static string ConvertToString<T>(T value, NativeConstantType constantType)
        {
            Type type = typeof(T);

            // Get the collection of maps for the given type.
            // Return the int value as string if the collection doesn't exist.
            Dictionary<NativeConstantType, NativeConstantMap<T>> mapCollection = null;
            if (_maps.ContainsKey(type))
            {
                mapCollection = (Dictionary<NativeConstantType, NativeConstantMap<T>>)_maps[type];
            }
            else
            {
                return value.ToString();
            }

            // Get the map for the constant type.
            // Return the int value as string if the collection doesn't exist.
            Dictionary<T, string> map = null;
            if (mapCollection.ContainsKey(constantType))
            {
                map = mapCollection[constantType];
            }
            else
            {
                return value.ToString();
            }

            // Return the mapped string.
            // Return the int value as string if the collection doesn't exist.
            if (map.ContainsKey(value))
            {
                return map[value];
            }
            else
            {
                return value.ToString();
            }
        }

        // Creates a new Constant-to-String map for the given NativeConstantType.
        // This may be called just once for each NativeConstantType.
        private static void _CreateMap<T>(NativeConstantType constantType, string prefix)
        {
            
            Dictionary<NativeConstantType, NativeConstantMap<T>> mapCollection = null;
            Type type = typeof(T);

            if (!_maps.ContainsKey(type))
            {
                mapCollection = new Dictionary<NativeConstantType, NativeConstantMap<T>>();
                _maps.Add(type, mapCollection);
            }
            else
            {
                mapCollection = (Dictionary<NativeConstantType, NativeConstantMap<T>>)_maps[type];
            }

            if (mapCollection.ContainsKey(constantType))
            {
                throw new ArgumentException("A map for constant type '" + constantType.ToString() + "' has already been created.");
            }

            // Add new NativeConstantMap to map collection.
            mapCollection.Add(constantType, new NativeConstantMap<T>(prefix));
        }

        // Populates all Constant-to-String maps by reflecting
        // on the NativeConstants fields.
        private static void _PopulateMaps()
        {
            Type type = typeof(NativeConstants);
            MethodInfo method = type.GetMethod("_AddToMap", BindingFlags.NonPublic | BindingFlags.Static);
            MethodInfo genMethod = null;

            //
            // Add each field name-value pair to the appropriate
            // Constant-to-String map if possible. If the field is not
            // a known native constant "type", the _AddToMap() method 
            // will fail silently.
            //

            FieldInfo[] fieldInfos = type.GetFields();

            for (int i = 0; i < fieldInfos.Length; i++)
            {
                FieldInfo field = fieldInfos[i];

                genMethod = method.MakeGenericMethod(field.FieldType);

                genMethod.Invoke(
                    null, //Object obj,
                    BindingFlags.Static | BindingFlags.NonPublic, //BindingFlags invokeAttr,
                    null, //Binder binder,
                    new object[] { field }, //Object[] parameters,
                    System.Globalization.CultureInfo.InvariantCulture //CultureInfo culture
                );
            }
        }

        // Add an entry to a Constant-to-String map for the given field.
        private static void _AddToMap<T>(FieldInfo field)
        {
            // Check that the field's type is the same as the generic type.
            if (field.FieldType != typeof(T))
            {
                throw new ArgumentException("info", "The field type '" + field.FieldType.ToString() + "' is not the expected type '" + typeof(T).ToString() + "'.");
            }

            // Get the collection of maps for the given
            // generic type.
            Dictionary<NativeConstantType, NativeConstantMap<T>>.ValueCollection maps = null;
            Type type = typeof(T);

            if (!_maps.ContainsKey(type))
            {
                return;
            }

            Dictionary<NativeConstantType, NativeConstantMap<T>>
                mapCollection = (Dictionary<NativeConstantType, NativeConstantMap<T>>)_maps[type];

            maps = mapCollection.Values as Dictionary<NativeConstantType, NativeConstantMap<T>>.ValueCollection;

            // Walk the maps looking for the matching NativeConstantType prefix.
            // If a map is found and the field's value hasn't been mapped yet,
            // add it to the map.
            foreach (NativeConstantMap<T> map in maps)
            {
                T val = (T)field.GetValue(null);
                if (field.Name.StartsWith(map.Prefix) && !map.ContainsKey(val))
                {
                    map.Add(val, field.Name);
                    break;
                }
            }

            return;
        }

        // Extends a Dictionary for Constant-to-String mappings.
        // Adds a Prefix property to indicate the native constant "type"
        // for which the map exists.
        private class NativeConstantMap<T> : Dictionary<T, string>
        {
            private string _prefix;

            public NativeConstantMap(string prefix)
            {
                _prefix = prefix;
            }

            public string Prefix
            {
                get { return _prefix; }
            }
        }

        private static Dictionary<Type, object> _maps = new Dictionary<Type, object>();
    }
}


